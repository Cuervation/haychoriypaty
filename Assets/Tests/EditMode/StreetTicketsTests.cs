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
        private object persistedMatch;
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
            persistedMatch = null;
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
        private static object InvokeAny(object instance, string name, params object[] args) =>
            instance.GetType().GetMethod(name, AnyInstance).Invoke(instance, args);
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
            persistedMatch = match;
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
        public void LegacyLosRedondosAccessNowSelectsTowerDefenseInsteadOfTraditionalMode()
        {
            object game = NewGame(Json(NewLegacySave(10, null, Tickets(0))));
            Assert.IsFalse((bool)Invoke(game, "IsLevelUnlocked", 12), "Access to Los Redondos alone does not unlock Argentinos.");
            SelectLevel(game, 10);
            Type simulationType = Runtime("StreetSimulation");
            Assert.IsTrue((bool)simulationType.GetMethod("IsTowerDefenseLevel", PublicStatic).Invoke(null, new object[] { 10 }));
            Assert.IsFalse((bool)Invoke(game, "TryStartRound"), "Legacy ID10 must not be reinterpreted as a traditional round.");
            Assert.IsTrue((bool)Invoke(game, "TryStartTowerDefenseRound"));
            object sim = Read(game, "Sim");
            Assert.AreEqual(500, Read(sim, "Coins"), "Both defense campaign identities use the isolated common opening grant.");
            Assert.AreEqual(2, Read(sim, "ProductCount"));
        }

        [Test]
        public void LegacyPendingTraditionalLosRedondosWinDoesNotUnlockItsNewCampaignSuccessor()
        {
            object pending = Activator.CreateInstance(matchType);
            Put(pending, "id", "legacy-redondos-result"); Put(pending, "attemptId", "legacy-redondos-attempt");
            Put(pending, "level", 10); Put(pending, "won", true);
            object legacy = NewLegacySave(2, null, Tickets(0), null, pending);
            object game = NewGame(Json(legacy));

            Assert.IsTrue((bool)Invoke(game, "IsLevelUnlocked", 12), "The historical win keeps its old Argentinos unlock meaning.");
            Assert.IsFalse((bool)Invoke(game, "IsLevelUnlocked", 4));
            Assert.IsTrue((bool)Invoke(game, "Save"));
            Assert.IsFalse((bool)Invoke(game, "IsLevelUnlocked", 4), "Persisting a legacy ID10 win must not reinterpret it as a TD ID10 campaign transition.");
        }

        [Test]
        public void ResolvingMigratedLegacyLosRedondosVictoryPreservesHistoryBucketAndOutboxIdentity()
        {
            object pendingVictory = Activator.CreateInstance(matchType);
            Put(pendingVictory, "id", "legacy-redondos-pending"); Put(pendingVictory, "attemptId", "legacy-redondos-attempt");
            Put(pendingVictory, "level", 10); Put(pendingVictory, "won", true); Put(pendingVictory, "elapsedSeconds", 77f);
            Put(pendingVictory, "salesByProduct", new long[7]); Put(pendingVictory, "stockConsumed", new long[10]);
            Put(pendingVictory, "stockRemaining", new long[10]); Put(pendingVictory, "hiresByRole", new long[5]);
            object legacy = NewLegacySave(2, null, Tickets(0), pendingVictory);
            Put(legacy, "version", 6);
            object oldStats = Field(legacy, "statistics");
            long[] wins = new long[13]; wins[10] = 2; Put(oldStats, "winsByLevel", wins);
            float[] times = new float[13]; times[10] = 90f; Put(oldStats, "bestWinTimeByLevel", times);
            object ticketLedger = Field(legacy, "tickets"); Put(ticketLedger, "migrationApplied", true);
            object oldMarket = Field(legacy, "market");
            object result = Activator.CreateInstance(Runtime("StreetMarketResult"));
            Put(result, "won", true); Put(result, "level", 10); Put(result, "chulengoManaged", true);
            Put(result, "chulengoPending", true); Put(result, "chulengoBaseRewardCents", 10000L);
            Put(result, "recognizedSalesCents", 5000L); Put(result, "bonusCents", 5000L); Put(result, "earnedCents", 10000L);
            Put(oldMarket, "lastResult", result);

            object game = NewGame(Json(legacy));
            object migratedStats = Field(Field(game, "save"), "statistics");
            Assert.AreEqual(2L, Field(migratedStats, "legacyTraditionalLosRedondosWins"));
            Assert.AreEqual(0L, ((long[])Field(migratedStats, "winsByLevel"))[10]);
            Assert.IsFalse((bool)Invoke(game, "IsLevelUnlocked", 4));

            Assert.IsTrue((bool)Invoke(game, "TryCollectVictoryReward"));
            Assert.AreEqual(3L, Field(migratedStats, "legacyTraditionalLosRedondosWins"));
            Assert.AreEqual(0L, ((long[])Field(migratedStats, "winsByLevel"))[10], "The old traditional result must not be counted in the new TD slot.");
            Assert.IsFalse((bool)Invoke(game, "IsLevelUnlocked", 4), "The old pending victory must not unlock the new ID10 successor.");
            Assert.IsNotNull(persistedMatch, "The existing result is committed rather than dropped from the outbox.");
            Assert.AreEqual("legacy-redondos-attempt", Field(persistedMatch, "attemptId"));
            Assert.AreEqual(10, Field(persistedMatch, "level"));
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
        public void TicketEligibilityUsesExplicitUnlocksAndStableLevelIdentityAfterReordering()
        {
            object save = NewLegacySave(3, null, Tickets(9));
            Put(save, "version", 7);
            bool[] unlocked = new bool[13]; unlocked[0] = true; unlocked[2] = true;
            unlocked[5] = true; unlocked[12] = true;
            Put(save, "unlockedLevelIds", unlocked);
            object game = NewGame(Json(save));
            object parrillero = Role(roleType, "Parrillero");

            SelectLevel(game, 12);
            Assert.IsTrue((bool)Invoke(game, "CanRedeemTicketWorker", parrillero), "Explicitly unlocked Argentinos keeps its legacy ID>=5 ticket eligibility.");
            SelectLevel(game, 5);
            Assert.IsTrue((bool)Invoke(game, "CanRedeemTicketWorker", parrillero), "Racing remains eligible after the campaign reorder.");
            SelectLevel(game, 2);
            Assert.IsFalse((bool)Invoke(game, "CanRedeemTicketWorker", parrillero), "A newly earlier stable ID below 5 does not gain ticket eligibility from its visual position.");
        }

        [Test]
        public void CamionerosTowerDefenseUsesCampaignId9SettlementAndAdvancesByCampaignOrderExactlyOnce()
        {
            Type simulationType = Runtime("StreetSimulation");
            int[] order = (int[])simulationType.GetField("CampaignOrder", PublicStatic).GetValue(null);
            Assert.AreEqual(13, order.Length);
            CollectionAssert.AreEqual(new[] { 0, 1, 9, 2, 12, 3, 11, 10, 4, 5, 6, 7, 8 }, order);

            long[] reservedForOtherLevels = { 1, 1, 0, 0, 0 };
            object game = NewGame(Json(NewLegacySave(9, null, Tickets(4, 0, reservedForOtherLevels))));
            Assert.IsTrue((bool)Invoke(game, "SelectLevel", 9));
            object parrilleroRole = Role(roleType, "Parrillero");
            Assert.IsFalse((bool)Invoke(game, "CanRedeemTicketWorker", parrilleroRole), "Level 9 cannot take a new ticket redemption.");
            Assert.IsFalse((bool)Invoke(game, "TryRedeemTicketWorker", parrilleroRole, Invoke(game, "TicketRedemptionOperationId", parrilleroRole)));
            Assert.IsFalse((bool)Invoke(game, "TryStartRound"), "The traditional Camioneros entry point must be unavailable.");
            Assert.IsTrue((bool)Invoke(game, "TryStartTowerDefenseRound"));

            object sim = Read(game, "Sim");
            Assert.AreEqual(9, Read(sim, "LevelIndex"));
            Assert.IsTrue((bool)Read(sim, "IsExternalCampaignRuntime"));
            Assert.AreEqual(2, Read(sim, "ProductCount"), "Camioneros exposes only Chori and Coca in the campaign product surface.");
            Assert.AreEqual(0, Invoke(sim, "GetAvailableProduct", 0));
            Assert.AreEqual(4, Invoke(sim, "GetAvailableProduct", 1));
            Assert.AreEqual(0, Invoke(sim, "GetProductGoal", 0));
            Assert.AreEqual(0, Invoke(sim, "GetProductGoal", 4));
            Assert.AreEqual(0, Invoke(sim, "WorkerCount", parrilleroRole), "TD clears the ordinary campaign baseline crew.");
            Assert.AreEqual(0, Invoke(sim, "WorkerCount", Role(roleType, "Cocacolero")));
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", parrilleroRole), "TD leaves reserved ticket workers untouched.");
            Assert.AreEqual(1L, Invoke(game, "PendingTicketWorkers", Role(roleType, "Cocacolero")));
            float initialElapsed = (float)Read(sim, "Elapsed");
            Invoke(sim, "Step", 2f);
            Assert.AreEqual(initialElapsed, (float)Read(sim, "Elapsed"), "The traditional simulation cannot step under TD.");

            int[] deliveries = new int[7];
            deliveries[0] = 1; // One actual sale is enough; obsolete campaign quotas do not gate TD victory.
            int[] workers = (int[])Read(sim, "WorkerCountsByRole");
            Assert.IsTrue((bool)InvokeAny(sim, "ImportTowerDefenseState", 240f, 600, 250, 1, workers,
                deliveries, 8, 7, true, true));
            Assert.IsTrue((bool)Invoke(game, "ProcessRoundResult"), "TD must settle through the existing Market/Chulengo path.");
            Assert.IsTrue((bool)Read(game, "HasPendingChulengo"));
            Assert.AreEqual(10, Read(game, "UnlockedLevel"));
            long ticketsBeforeChoice = (long)Read(game, "TicketBalance");

            Assert.IsTrue((bool)Invoke(game, "TryCollectVictoryReward"));
            Assert.AreEqual(ticketsBeforeChoice + 1, (long)Read(game, "TicketBalance"));
            Assert.AreEqual(1L, ((long[])Field(Field(Field(game, "save"), "statistics"), "winsByLevel"))[9]);
            Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"), "Retrying the terminal result cannot create a second match or ticket.");
            Assert.AreEqual(ticketsBeforeChoice + 1, (long)Read(game, "TicketBalance"));
            Assert.IsTrue((bool)Invoke(game, "NextLevel"));
            Assert.AreEqual(2, Read(Read(game, "Sim"), "LevelIndex"), "The successor follows the reordered campaign rather than numeric stable-ID order.");
        }

        [Test]
        public void ExitAtTowerDefenseCompletionSettlesVictoryInsteadOfAbandoning()
        {
            object game = NewGame(Json(NewLegacySave(9, null, Tickets(4))));
            Assert.IsTrue((bool)Invoke(game, "SelectLevel", 9));
            Assert.IsTrue((bool)Invoke(game, "TryStartTowerDefenseRound"));
            object campaign = Read(game, "Sim");

            Type configType = Runtime("TowerDefenseLevelConfig");
            object config = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo", configType);
            Assert.IsNotNull(config);
            object route = Read(config, "Route");
            Type geometryType = Runtime("TowerDefenseRouteGeometry");
            object geometry = geometryType.GetMethod("Generate", PublicStatic).Invoke(null, new[] { route });
            Assert.IsTrue((bool)Read(geometry, "IsValid"));
            Vector2 first = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)0 });
            Vector2 second = (Vector2)configType.GetMethod("WorkZonePosition", PublicInstance).Invoke(config, new[] { geometry, (object)1 });
            object mode = Activator.CreateInstance(Runtime("TowerDefenseGameMode"), new object[]
            {
                config, Read(game, "Balance"), route, 0.02f, first, second,
                Read(campaign, "WorkerCountsByRole"), Read(campaign, "TicketWorkerCountsByRole"), Read(campaign, "Coins")
            });
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(game, "TryHireTowerDefense", mode, Role(roleType, "Cocacolero"), 0));
            for (int n = 0; n < 10; n++)
                Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {4}, new[] {1}));
            for (int n = 0; n < 10; n++) InvokeAny(mode, "DeliverWithinServiceRanges");
            Put(mode, "elapsed", 500f); // No inherited 380-second terminal or history clamp.
            Invoke(mode, "Step", .5f);
            Assert.IsTrue((bool)Read(mode, "HasWon"));
            Assert.AreEqual(10, Read(mode, "ServedFans"));
            Assert.AreEqual("Finished", Read(mode, "Phase").ToString());

            Assert.IsTrue((bool)Invoke(game, "ExitTowerDefenseRound", mode));
            Assert.Greater((float)Read(campaign, "Elapsed"), 380f);
            Assert.AreEqual(0f, Read(campaign, "TimeRemaining"));
            Assert.IsTrue((bool)Read(game, "HasPendingChulengo"), "A terminal victory must use the common victory settlement, not the abandoned-attempt path.");
            Assert.IsTrue((bool)Invoke(game, "TryCollectVictoryReward"));
            object stats = Field(Field(game, "save"), "statistics");
            Assert.AreEqual(1L, ((long[])Field(stats, "winsByLevel"))[9]);
            Assert.AreEqual(0L, Field(stats, "abandons"));
            Invoke(mode, "Stop");
        }

        [TestCase("CompleteTowerDefenseRound")]
        [TestCase("ExitTowerDefenseRound")]
        [TestCase("OnApplicationPause")]
        public void FirstDefenseEscapeSettlesLossNotAbandonmentExactlyOnce(string entry)
        {
            object game = NewGame(Json(NewLegacySave(9, null, Tickets(4))));
            Assert.IsTrue((bool)Invoke(game, "SelectLevel", 9));
            Assert.IsTrue((bool)Invoke(game, "TryStartTowerDefenseRound"));
            object config = Resources.Load("TowerDefense/Camioneros-PlazaDeMayo", Runtime("TowerDefenseLevelConfig"));
            object route = Read(config, "Route");
            object geometry = Runtime("TowerDefenseRouteGeometry").GetMethod("Generate", PublicStatic).Invoke(null, new[] {route});
            Vector2 first = (Vector2)Invoke(config, "WorkZonePosition", geometry, 0);
            Vector2 second = (Vector2)Invoke(config, "WorkZonePosition", geometry, 1);
            object mode = Activator.CreateInstance(Runtime("TowerDefenseGameMode"), new object[]
                {config, Read(game, "Balance"), route, .02f, first, second});
            Invoke(mode, "Begin");
            Assert.IsTrue((bool)Invoke(game, "AttachTowerDefenseRuntime", mode));
            Assert.IsTrue((bool)Invoke(mode, "TrySpawnFan", new[] {4}, new[] {1}));
            object fan = ((System.Collections.IList)Read(mode, "Fans"))[0];
            object follower = Read(fan, "RouteFollower");
            Invoke(follower, "Advance", (float)Read(follower, "TotalDistance") / (float)Read(follower, "Speed") + .1f);
            Invoke(mode, "Step", .01f);
            Assert.IsTrue((bool)Read(mode, "HasLost"));
            if (entry == "OnApplicationPause") InvokeAny(game, entry, true);
            else Assert.IsTrue((bool)Invoke(game, entry, mode));
            object stats = Field(Field(game, "save"), "statistics");
            Assert.AreEqual("Lost", Read(Read(game, "Sim"), "Phase").ToString());
            Assert.AreEqual(1L, ((long[])Field(stats, "lossesByLevel"))[9]);
            Assert.AreEqual(0L, Field(stats, "abandons"));
            Assert.AreEqual(9, Read(game, "UnlockedLevel"));
            Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, ((long[])Field(stats, "lossesByLevel"))[9]);
            Invoke(mode, "Stop");
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
                if ((bool)Runtime("StreetSimulation").GetMethod("IsTowerDefenseLevel", PublicStatic).Invoke(null, new object[] { level }))
                    continue; // Both TD identities use the separate entry point and start without ticket workers.
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
                bool startsWithMozo = level == 5 || level == 6 || level == 8 || level == 10;
                Assert.AreEqual(startsWithMozo ? 1 : 0, Invoke(sim, "WorkerCount", Role(roleType, "Mozo")),
                    "Level index " + level + " must match its configured free Mozo baseline.");
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
        public void V6SnapshotValidationAcceptsTicketsAndRejectsAnInconsistentWallet()
        {
            Type repository = Type.GetType("HayChoriYPaty.Cloud.PlayerLocalRepository, Assembly-CSharp", true);
            MethodInfo validate = repository.GetMethod("ValidateSnapshot", PublicStatic);
            object valid = NewLegacySave(10, null, Tickets(3));
            Put(valid, "version", 6);
            validate.Invoke(null, new object[] { Json(valid) });
            object invalid = Tickets(3);
            Put(invalid, "totalSpent", 2L);
            Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { Json(NewLegacySave(10, null, invalid)) }));
        }
    }
}
