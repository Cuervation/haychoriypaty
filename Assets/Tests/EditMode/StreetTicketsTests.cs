using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Ticket wallet, terminal reward, migration and one-round worker integration regressions.</summary>
    public sealed class StreetTicketsTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private Type gameType, saveType, marketDataType, ticketDataType, matchType, roleType;
        private bool oldEditorInitialization, oldTemporaryStartupTicketGrantApplied, persistSucceeds = true;
        private string persistedJson;
        private readonly List<GameObject> hosts = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            gameType = Runtime("StreetGame");
            saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            marketDataType = Runtime("StreetMarketData");
            ticketDataType = Runtime("StreetTicketData");
            matchType = Runtime("StreetMatchRecord");
            roleType = Runtime("StreetWorkerRole");
            oldEditorInitialization = (bool)gameType.GetField("AllowEditorTestInitialization", PublicStatic).GetValue(null);
            gameType.GetField("AllowEditorTestInitialization", PublicStatic).SetValue(null, false);
            FieldInfo startupGrant = gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", PrivateStatic);
            oldTemporaryStartupTicketGrantApplied = (bool)startupGrant.GetValue(null);
            startupGrant.SetValue(null, false);
            persistSucceeds = true;
            persistedJson = null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in hosts) if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            hosts.Clear();
            gameType.GetField("AllowEditorTestInitialization", PublicStatic).SetValue(null, oldEditorInitialization);
            gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", PrivateStatic)
                .SetValue(null, oldTemporaryStartupTicketGrantApplied);
        }

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Read(object instance, string name) => instance.GetType().GetProperty(name, PublicInstance).GetValue(instance, null);
        private static object Field(object instance, string name) => instance.GetType().GetField(name, AnyInstance).GetValue(instance);
        private static void Put(object instance, string name, object value) => instance.GetType().GetField(name, AnyInstance).SetValue(instance, value);
        private static object Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, PublicInstance).Invoke(instance, args);
        private object InvokePrivateStatic(string name, params object[] args) => gameType.GetMethod(name, PrivateStatic).Invoke(null, args);
        private static object Role(Type type, string name) => Enum.Parse(type, name);

        private Delegate PersistDelegate()
        {
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), matchType, typeof(bool));
            MethodInfo method = GetType().GetMethod(nameof(Persist), PrivateInstance).MakeGenericMethod(matchType);
            return Delegate.CreateDelegate(callback, this, method);
        }

        private bool Persist<T>(string json, T match)
        {
            if (!persistSucceeds) return false;
            persistedJson = json;
            return true;
        }

        private object NewGame(string json = null)
        {
            GameObject host = new GameObject("Tickets test fixture");
            hosts.Add(host);
            Component instance = host.AddComponent(gameType);
            Assert.IsTrue((bool)Invoke(instance, "ActivatePlayer", json, PersistDelegate()));
            return instance;
        }

        private object NewLegacySave(int unlockedLevel, long[] wins = null, object tickets = null, object pendingVictory = null, object pendingMatch = null)
        {
            object data = Activator.CreateInstance(saveType);
            Put(data, "version", 4);
            Put(data, "unlockedLevel", unlockedLevel);
            object market = Activator.CreateInstance(marketDataType);
            Put(market, "balanceCents", 1_000_000L);
            int[] stock = new int[10];
            for (int i = 0; i < stock.Length; i++) stock[i] = 10000;
            Put(market, "stockUnits", stock);
            Put(data, "market", market);
            if (tickets != null) Put(data, "tickets", tickets);
            if (pendingVictory != null) Put(data, "pendingVictoryMatch", pendingVictory);
            if (pendingMatch != null) Put(data, "pendingMatch", pendingMatch);
            object stats = Field(data, "statistics");
            if (wins != null) Put(stats, "winsByLevel", wins);
            return data;
        }

        private object Tickets(long balance, long spent = 0, long[] pending = null)
        {
            object data = Activator.CreateInstance(ticketDataType);
            Put(data, "balance", balance);
            Put(data, "totalEarned", balance + spent);
            Put(data, "totalSpent", spent);
            Put(data, "migrationApplied", true);
            if (pending != null) Put(data, "pendingWorkersByRole", pending);
            return data;
        }

        private static string Json(object value) => JsonUtility.ToJson(value);

        [Test]
        public void TemporaryDevelopmentStartupGrantAddsThirtyOnlyOnceAndPreservesTheTicketLedger()
        {
            object tickets = Tickets(7, 3);

            Assert.IsFalse((bool)InvokePrivateStatic("ShouldGrantTemporaryStartupTickets"),
                "The temporary grant must stay off in the Unity Editor, even if Editor diagnostics are enabled.");
            Assert.IsTrue((bool)InvokePrivateStatic("TryApplyTemporaryStartupTicketGrant", tickets));
            Assert.AreEqual(37L, Field(tickets, "balance"));
            Assert.AreEqual(40L, Field(tickets, "totalEarned"));
            Assert.AreEqual(3L, Field(tickets, "totalSpent"));
            Assert.IsTrue((bool)Invoke(tickets, "IsValid"));

            Assert.IsFalse((bool)InvokePrivateStatic("TryApplyTemporaryStartupTicketGrant", tickets),
                "Further player activations in the same process must not stack another grant.");
            Assert.AreEqual(37L, Field(tickets, "balance"));
            Assert.AreEqual(40L, Field(tickets, "totalEarned"));
        }

        [Test]
        public void TemporaryDevelopmentStartupGrantCanRollbackAfterPersistenceFailureAndRejectOverflow()
        {
            object tickets = Tickets(4, 3);
            Assert.IsTrue((bool)InvokePrivateStatic("TryApplyTemporaryStartupTicketGrant", tickets));
            InvokePrivateStatic("UndoTemporaryStartupTicketGrant", tickets);
            Assert.AreEqual(4L, Field(tickets, "balance"));
            Assert.AreEqual(7L, Field(tickets, "totalEarned"));
            Assert.IsTrue((bool)Invoke(tickets, "IsValid"));
            Assert.IsTrue((bool)InvokePrivateStatic("TryApplyTemporaryStartupTicketGrant", tickets),
                "A failed startup save must leave the process grant available for retry.");

            gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", PrivateStatic).SetValue(null, false);
            object nearOverflow = Tickets(long.MaxValue - 1, 0);
            Assert.IsFalse((bool)InvokePrivateStatic("TryApplyTemporaryStartupTicketGrant", nearOverflow));
            Assert.AreEqual(long.MaxValue - 1, Field(nearOverflow, "balance"));
            Assert.AreEqual(long.MaxValue - 1, Field(nearOverflow, "totalEarned"));
            Assert.IsTrue((bool)Invoke(nearOverflow, "IsValid"));
        }

        private void SelectLevel(object game, int level)
        {
            Assert.IsTrue((bool)Invoke(game, "SelectLevel", level), "The fixture unlocks every campaign level.");
            FillSelectedLevelStock(game);
        }

        private void FillSelectedLevelStock(object game)
        {
            object sim = Read(game, "Sim"), market = Read(game, "Market"), data = Read(market, "Data");
            int[] need = (int[])Invoke(market, "RequiredUnits", sim);
            int[] stock = (int[])Field(data, "stockUnits");
            for (int i = 0; i < stock.Length; i++) stock[i] = Math.Max(stock[i], i < need.Length ? need[i] : 0);
            Put(data, "balanceCents", Math.Max(1_000_000L, (long)Field(data, "balanceCents")));
        }

        private void ForgeTerminalResult(object game, string phase)
        {
            object sim = Read(game, "Sim");
            PropertyInfo phaseProperty = sim.GetType().GetProperty("Phase", AnyInstance);
            phaseProperty.GetSetMethod(true).Invoke(sim, new[] { Enum.Parse(Runtime("RoundPhase"), phase) });
            sim.GetType().GetProperty("Coins", AnyInstance).GetSetMethod(true).Invoke(sim, new object[] { 10 });
            sim.GetType().GetProperty("CoinsEarned", AnyInstance).GetSetMethod(true).Invoke(sim, new object[] { 10 });
            Assert.IsTrue((bool)Invoke(game, "ProcessRoundResult"));
        }

        [Test]
        public void ThreeSameLevelWinsAwardThreeTicketsRegardlessOfChulengoChoiceAndResultRetry()
        {
            object game = NewGame();
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) SelectLevel(game, 0);
                Assert.IsTrue((bool)Invoke(game, "TryStartRound"));
                ForgeTerminalResult(game, "Won");
                bool claimed = i == 0 ? (bool)Invoke(game, "TryCollectVictoryReward")
                    : (bool)Invoke(game, "TryMultiplyVictoryReward", i == 1 ? 3 : 6);
                Assert.IsTrue(claimed);
                Assert.AreEqual(i + 1L, Read(game, "TicketBalance"));
                Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"), "A finalized result does not settle twice.");
                Assert.AreEqual(i + 1L, Read(game, "TicketBalance"));
            }
            Assert.AreEqual(3L, Read(game, "TicketTotalEarned"));
            Assert.AreEqual(0L, Read(game, "TicketTotalSpent"));
        }

        [Test]
        public void LossDoesNotAwardTicketsAndRepeatedCloseDoesNotChangeWallet()
        {
            object game = NewGame();
            Assert.IsTrue((bool)Invoke(game, "TryStartRound"));
            ForgeTerminalResult(game, "Lost");
            Assert.AreEqual(0L, Read(game, "TicketBalance"));
            Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"));
            Assert.AreEqual(0L, Read(game, "TicketTotalEarned"));
        }

        [Test]
        public void LegacyUnlockAndRecordedRepeatsMigrateOnceWithoutCountingPendingFirstUnlockVictory()
        {
            long[] wins = new long[11]; wins[0] = 2; wins[1] = 2;
            object first = NewGame(Json(NewLegacySave(5, wins)));
            Assert.AreEqual(7L, Read(first, "TicketBalance"), "Five inferred first wins plus two recorded repeats.");
            string migrated = persistedJson;
            Assert.IsNotNull(migrated);
            object restarted = NewGame(migrated);
            Assert.AreEqual(7L, Read(restarted, "TicketBalance"), "The persisted migration marker prevents a second grant.");

            object pending = Activator.CreateInstance(matchType);
            Put(pending, "id", "pending-first-win"); Put(pending, "attemptId", "pending-first-win");
            Put(pending, "level", 4); Put(pending, "won", true);
            object legacyPendingGame = NewGame(Json(NewLegacySave(5, new long[11], null, pending)));
            Assert.AreEqual(4L, Read(legacyPendingGame, "TicketBalance"),
                "The current unfinalized first-unlock victory is excluded until its Chulengo result is committed.");
        }

        [Test]
        public void ThreeTicketsRedeemExactlyOneWorkerAndRepeatOperationIdIsIdempotent()
        {
            object tickets = Tickets(6);
            object game = NewGame(Json(NewLegacySave(10, null, tickets)));
            SelectLevel(game, 5);
            object role = Role(roleType, "Parrillero");
            string firstOperation = (string)Invoke(game, "TicketRedemptionOperationId", role);
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, firstOperation));
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, firstOperation));
            Assert.AreEqual(3L, Read(game, "TicketBalance"));
            Assert.AreEqual(3L, Read(game, "TicketTotalSpent"));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", role));
            object stats = Field(Field(game, "save"), "statistics");
            Assert.AreEqual(0L, ((long[])Field(stats, "paidHiresByRole"))[0]);
        }

        [Test]
        public void ReplayingAnOlderRedemptionIdAfterANewerCanjeDoesNotSpendAgain()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(9))));
            SelectLevel(game, 5);
            object parrillero = Role(roleType, "Parrillero");
            object mozo = Role(roleType, "Mozo");
            string olderId = (string)Invoke(game, "TicketRedemptionOperationId", parrillero);
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", parrillero, olderId));
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", mozo, Invoke(game, "TicketRedemptionOperationId", mozo)));
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", parrillero, olderId));
            Assert.AreEqual(3L, Read(game, "TicketBalance"));
            Assert.AreEqual(6L, Read(game, "TicketTotalSpent"));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", parrillero));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", mozo));
        }

        [Test]
        public void TwoTicketsAreInsufficientAndRacingMustBeUnlockedAndSelected()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(2))));
            SelectLevel(game, 5);
            object role = Role(roleType, "Mozo");
            Assert.IsFalse((bool)Invoke(game, "CanRedeemTicketWorker", role));
            Assert.IsFalse((bool)Invoke(game, "TryRedeemTicketWorker", role, Invoke(game, "TicketRedemptionOperationId", role)));
            Assert.AreEqual(2L, Read(game, "TicketBalance"));

            object locked = NewGame(Json(NewLegacySave(4, null, Tickets(3))));
            Assert.IsFalse((bool)Read(locked, "TicketExtrasUnlocked"));
            Assert.IsFalse((bool)Invoke(locked, "CanRedeemTicketWorker", role));
        }

        [Test]
        public void TwoRedemptionsSpendSixTicketsAndPendingReservationsSurviveReload()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(6))));
            SelectLevel(game, 5);
            object role = Role(roleType, "Fernetero");
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, Invoke(game, "TicketRedemptionOperationId", role)));
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, Invoke(game, "TicketRedemptionOperationId", role)));
            Assert.AreEqual(0L, Read(game, "TicketBalance"));
            Assert.AreEqual(6L, Read(game, "TicketTotalSpent"));
            Assert.AreEqual(2L, Invoke(game, "PendingTicketWorkers", role));
            object restarted = NewGame(persistedJson);
            Assert.AreEqual(0L, Read(restarted, "TicketBalance"));
            Assert.AreEqual(2L, Invoke(restarted, "PendingTicketWorkers", role));
        }

        [Test]
        public void FiveTicketsRedeemWorkerForRacingAndKeepTwoTicketsAfterStart()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(5))));
            SelectLevel(game, 5);
            object role = Role(roleType, "Parrillero");
            object sim = Read(game, "Sim");
            int baselineWorkers = (int)Invoke(sim, "WorkerCount", role);
            string operationId = (string)Invoke(game, "TicketRedemptionOperationId", role);

            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, operationId));
            Assert.AreEqual(2L, Read(game, "TicketBalance"));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", role));
            Assert.IsTrue((bool)Invoke(game, "TryStartRound"));

            Assert.AreEqual(baselineWorkers + 1, Invoke(sim, "WorkerCount", role),
                "The redeemed Parrillero joins the normal Racing starting roster.");
            Assert.AreEqual(2L, Read(game, "TicketBalance"), "The two unspent tickets remain in the wallet.");
            Assert.AreEqual(3L, Read(game, "TicketTotalSpent"));
            Assert.AreEqual(0L, Invoke(game, "PendingTicketWorkers", role));
            Assert.IsNotNull(persistedJson, "The eligible attempt and reservation consumption are durably saved.");
        }

        [Test]
        public void FailedRedemptionAndFailedRoundStartDoNotPartiallySpendOrConsumeTickets()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(3))));
            SelectLevel(game, 5);
            object role = Role(roleType, "Cocacolero");
            string operationId = (string)Invoke(game, "TicketRedemptionOperationId", role);
            persistSucceeds = false;
            Assert.IsFalse((bool)Invoke(game, "TryRedeemTicketWorker", role, operationId));
            Assert.AreEqual(3L, Read(game, "TicketBalance"));
            Assert.AreEqual(0L, Read(game, "TicketTotalSpent"));
            Assert.AreEqual(0L, Invoke(game, "PendingTicketWorkers", role));

            persistSucceeds = true;
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, operationId));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", role));
            persistSucceeds = false;
            Assert.IsFalse((bool)Invoke(game, "TryStartRound"));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", role));
            Assert.AreEqual(0L, Read(game, "TicketBalance"), "The earlier canje remains spent while its reserved worker is restored after failed start.");
            Assert.AreEqual("Ready", Read(Read(game, "Sim"), "Phase").ToString());
        }

        [Test]
        public void EligibleStartsAddTicketWorkersAfterEveryLevelBaselineAndLeavePaidCostsUnchanged()
        {
            for (int level = 5; level <= 10; level++)
            {
                long[] pending = new long[5]; pending[0] = 1;
                object game = NewGame(Json(NewLegacySave(10, null, Tickets(0, 3, pending))));
                SelectLevel(game, level);
                object sim = Read(game, "Sim");
                int[] costs = new int[5];
                for (int role = 0; role < costs.Length; role++) costs[role] = (int)Invoke(sim, "HireCostForRole", Enum.ToObject(roleType, role));
                Assert.IsTrue((bool)Invoke(game, "TryStartRound"), "Eligible campaign level index " + level);
                int baselineParrilleros = level == 6 ? 2 : 1;
                Assert.AreEqual(baselineParrilleros + 1, Invoke(sim, "WorkerCount", Role(roleType, "Parrillero")));
                Assert.AreEqual(level == 5 ? 1 : 0, Invoke(sim, "WorkerCount", Role(roleType, "ParrilleroPremium")));
                Assert.AreEqual(level == 5 || level == 6 || level == 8 ? 1 : 0, Invoke(sim, "WorkerCount", Role(roleType, "Mozo")));
                for (int role = 0; role < costs.Length; role++)
                    Assert.AreEqual(costs[role], (int)Invoke(sim, "HireCostForRole", Enum.ToObject(roleType, role)), "Ticket worker must not raise paid tier at level " + level + ", role " + role);
                Assert.AreEqual(0L, Invoke(game, "PendingTicketWorkers", Role(roleType, "Parrillero")));
            }
        }

        [Test]
        public void ALevelBelowRacingKeepsPreviouslyRedeemedWorkerPending()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(3))));
            SelectLevel(game, 5);
            object role = Role(roleType, "ParrilleroPremium");
            Assert.IsTrue((bool)Invoke(game, "TryRedeemTicketWorker", role, Invoke(game, "TicketRedemptionOperationId", role)));
            SelectLevel(game, 4);
            Assert.IsTrue((bool)Invoke(game, "TryStartRound"));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", role));
            Assert.AreEqual(3L, Read(game, "TicketTotalSpent"));
        }

        [Test]
        public void V4SnapshotValidationAcceptsTicketsAndRejectsAnInconsistentWallet()
        {
            Type repository = Type.GetType("HayChoriYPaty.Cloud.PlayerLocalRepository, Assembly-CSharp", true);
            MethodInfo validate = repository.GetMethod("ValidateSnapshot", PublicStatic);
            validate.Invoke(null, new object[] { Json(NewLegacySave(10, null, Tickets(3))) });
            object invalid = Tickets(3);
            Put(invalid, "totalSpent", 2L);
            Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { Json(NewLegacySave(10, null, invalid)) }));
        }
    }
}
