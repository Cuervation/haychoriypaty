using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Persistence and payout regressions for the post-victory Chulengo choice.</summary>
    public sealed class StreetChulengoTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;

        private Type gameType, matchType;
        private GameObject host;
        private bool oldEditorInitialization;
        private string savedJson;
        private object capturedMatch;
        private bool persistSucceeds = true;
        private int persistCalls, failAtCall = -1, successfulMatchWrites;

        [SetUp]
        public void SetUp()
        {
            gameType = Runtime("StreetGame");
            matchType = Runtime("StreetMatchRecord");
            savedJson = null;
            capturedMatch = null;
            persistSucceeds = true;
            persistCalls = 0;
            failAtCall = -1;
            successfulMatchWrites = 0;
            oldEditorInitialization = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            gameType.GetField("AllowEditorTestInitialization", StaticPublic).SetValue(null, false);
            host = new GameObject("Chulengo acceptance fixture");
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            gameType.GetField("AllowEditorTestInitialization", StaticPublic).SetValue(null, oldEditorInitialization);
        }

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Read(object instance, string name) => instance.GetType().GetProperty(name, PublicInstance).GetValue(instance, null);
        private static object Field(object instance, string name) => instance.GetType().GetField(name, AnyInstance).GetValue(instance);
        private static void Put(object instance, string name, object value) => instance.GetType().GetField(name, AnyInstance).SetValue(instance, value);
        private static object Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, PublicInstance).Invoke(instance, args);

        private Delegate PersistDelegate()
        {
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), matchType, typeof(bool));
            MethodInfo generic = GetType().GetMethod(nameof(Persist), PrivateInstance).MakeGenericMethod(matchType);
            return Delegate.CreateDelegate(callback, this, generic);
        }

        private bool Persist<T>(string json, T match)
        {
            persistCalls++;
            bool success = persistSucceeds && persistCalls != failAtCall;
            if (success)
            {
                savedJson = json;
                if (match != null) { capturedMatch = match; successfulMatchWrites++; }
            }
            return success;
        }

        private static bool IsAbsentMatch(object match) => (bool)Runtime("StreetMatchRecord")
            .GetMethod("IsAbsent", BindingFlags.Static | BindingFlags.Public).Invoke(null, new[] { match });

        private object NewGame(string json = null)
        {
            object game = host.AddComponent(gameType);
            Assert.IsTrue((bool)gameType.GetMethod("ActivatePlayer", PublicInstance).Invoke(game, new object[] { json, PersistDelegate() }));
            return game;
        }

        // Fixture-only transition: this does not claim to simulate a real player victory.
        private void ForgeWonPhase(object game)
        {
            Assert.IsTrue((bool)Invoke(game, "TryStartRound"), "Start a normal, market-backed attempt before injecting the terminal phase.");
            object sim = Read(game, "Sim");
            Type phaseType = Runtime("RoundPhase");
            sim.GetType().GetProperty("Phase", AnyInstance).SetValue(sim, Enum.Parse(phaseType, "Won"), null);
            sim.GetType().GetProperty("Coins", AnyInstance).SetValue(sim, 10, null);
            sim.GetType().GetProperty("CoinsEarned", AnyInstance).SetValue(sim, 10, null);
            Assert.IsTrue((bool)Invoke(game, "ProcessRoundResult"), "Persist pending choice, not a final match.");
        }

        [Test]
        public void SevenEqualSectorsHaveSpecifiedMultipliersAndClampedBoundaries()
        {
            Type chulengo = Runtime("StreetChulengo");
            Assert.AreEqual(7, chulengo.GetField("SectorCount", StaticPublic).GetValue(null));
            MethodInfo multiplier = chulengo.GetMethod("MultiplierNumerator", StaticPublic);
            CollectionAssert.AreEqual(new[] { 0, 3, 2, 4, 2, 3, 0 }, new[] {
                (int)multiplier.Invoke(null, new object[] { 0 }), (int)multiplier.Invoke(null, new object[] { 1 }),
                (int)multiplier.Invoke(null, new object[] { 2 }), (int)multiplier.Invoke(null, new object[] { 3 }),
                (int)multiplier.Invoke(null, new object[] { 4 }), (int)multiplier.Invoke(null, new object[] { 5 }),
                (int)multiplier.Invoke(null, new object[] { 6 }) });
            MethodInfo at = chulengo.GetMethod("SectorAtPosition", StaticPublic);
            Assert.AreEqual(0, at.Invoke(null, new object[] { 0f }));
            Assert.AreEqual(0, at.Invoke(null, new object[] { 1f / 7f - .0001f }));
            Assert.AreEqual(1, at.Invoke(null, new object[] { 1f / 7f + .0001f }));
            Assert.AreEqual(6, at.Invoke(null, new object[] { 1f }));
            Assert.AreEqual(0, at.Invoke(null, new object[] { -1f }));
            Assert.AreEqual(6, at.Invoke(null, new object[] { 2f }));
            Assert.AreEqual(-1, at.Invoke(null, new object[] { float.NaN }));
            MethodInfo scale = chulengo.GetMethod("ScaleCents", StaticPublic);
            Assert.AreEqual(151L, scale.Invoke(null, new object[] { 101L, 3 }), "Odd cent values use deterministic floor rounding.");
            Assert.AreEqual(202L, scale.Invoke(null, new object[] { 101L, 4 }));
            Assert.Throws<OverflowException>(() => scale.Invoke(null, new object[] { long.MaxValue, 4 }));
        }

        [Test]
        public void NeedleCompletesRoundTripAtTwiceItsPreviousSpeed()
        {
            Type chulengo = Runtime("StreetChulengo");
            MethodInfo atTime = chulengo.GetMethod("PositionAtTime", StaticPublic);

            Assert.AreEqual(0f, atTime.Invoke(null, new object[] { 0f }));
            Assert.AreEqual(.5f, (float)atTime.Invoke(null, new object[] { .7f }), .0001f,
                "The needle reaches the center in half the old time.");
            Assert.AreEqual(1f, (float)atTime.Invoke(null, new object[] { 1.4f }), .0001f,
                "The left-to-right sweep takes 1.4 seconds instead of 2.8.");
            Assert.AreEqual(.5f, (float)atTime.Invoke(null, new object[] { 2.1f }), .0001f);
            Assert.AreEqual(0f, (float)atTime.Invoke(null, new object[] { 2.8f }), .0001f,
                "The full left-right-left cycle is 2.8 seconds, exactly half its former duration.");
        }

        [Test]
        public void VictoryEscrowsRewardUntilCollectAndCannotAdvanceOrBuyDuringChoice()
        {
            object game = NewGame();
            object marketBeforeWin = Read(game, "Market");
            Put(Read(marketBeforeWin, "Data"), "balanceCents", 1_000_000L);
            ForgeWonPhase(game);
            Assert.IsTrue((bool)Read(game, "HasPendingChulengo"));
            Assert.IsTrue((bool)Read(game, "ShowChulengoResult"));
            Assert.IsNotNull(savedJson);
            Assert.IsNull(capturedMatch, "No finalized match may be emitted before the choice.");
            object save = JsonUtility.FromJson(savedJson, gameType.GetNestedType("SaveData", BindingFlags.Public));
            Assert.IsTrue(Field(save, "pendingVictoryMatch") != null);
            Assert.IsTrue(IsAbsentMatch(Field(save, "pendingMatch")), "No final outbox record may exist before choice.");
            object pendingStatistics = Field(save, "statistics");
            Assert.AreEqual(0L, Field(pendingStatistics, "wins"));
            Assert.AreEqual(0L, Field(pendingStatistics, "attemptsFinished"));
            long baseReward = (long)Read(game, "ChulengoBaseRewardCents");
            Assert.Greater(baseReward, 0L);
            object market = Read(game, "Market");
            long balance = (long)Read(market, "BalanceCents");
            Assert.AreEqual(1_000_000L, balance, "Escrowing this turn's reward must leave prior bank untouched.");
            Assert.IsFalse((bool)Read(game, "CanStartRound"));
            Assert.IsFalse((bool)Invoke(game, "TryStartRound"));
            Assert.IsFalse((bool)Invoke(game, "SelectLevel", 0));
            Assert.IsFalse((bool)Invoke(game, "TryBuyIngredient", 0, 1, 999L));

            Assert.IsTrue((bool)Invoke(game, "TryCollectVictoryReward"));
            Assert.IsFalse((bool)Read(game, "HasPendingChulengo"));
            Assert.IsNotNull(capturedMatch);
            Assert.AreEqual(baseReward, Read(game, "ChulengoFinalRewardCents"));
            Assert.IsFalse((bool)Invoke(game, "TryCollectVictoryReward"), "A victory may be claimed once only.");
            Assert.IsFalse((bool)Invoke(game, "TryMultiplyVictoryReward", 3));
        }

        [Test]
        public void EachSectorFreezesExactOutcomeAndZeroLosesOnlyTurnReward()
        {
            int[] numerators = { 0, 3, 2, 4, 2, 3, 0 };
            foreach (int sector in new[] { 0, 1, 2, 3, 4, 5, 6 })
            {
                GameObject isolated = new GameObject("Chulengo sector " + sector);
                object game = isolated.AddComponent(gameType);
                Assert.IsTrue((bool)gameType.GetMethod("ActivatePlayer", PublicInstance).Invoke(game,
                    new object[] { null, PersistDelegate() }));
                Put(Read(Read(game, "Market"), "Data"), "balanceCents", 1_000_000L);
                ForgeWonPhase(game);
                long baseReward = (long)Read(game, "ChulengoBaseRewardCents");
                object market = Read(game, "Market");
                long oldBank = (long)Read(market, "BalanceCents");
                Assert.IsTrue((bool)Invoke(game, "TryMultiplyVictoryReward", sector));
                Assert.AreEqual(sector, Read(game, "ChulengoSelectedSector"));
                Assert.AreEqual(baseReward * numerators[sector] / 2L, Read(game, "ChulengoFinalRewardCents"));
                Assert.IsFalse((bool)Invoke(game, "TryMultiplyVictoryReward", (sector + 1) % 7));
                Assert.IsFalse((bool)Invoke(game, "TryCollectVictoryReward"));
                Assert.AreEqual(oldBank + baseReward * numerators[sector] / 2L, Read(market, "BalanceCents"));
                object statistics = Field(Field(game, "save"), "statistics");
                Assert.AreEqual(1L, (long)Field(statistics, "wins"), "The won match is recorded once after the choice.");
                Assert.AreEqual(1L, ((long[])Field(statistics, "winsByLevel"))[0]);
                if (sector == 0)
                {
                    string zeroSave = savedJson;
                    long zeroBank = (long)Read(market, "BalanceCents");
                    GameObject reopenedHost = new GameObject("Chulengo zero result recovery fixture");
                    object reopened = reopenedHost.AddComponent(gameType);
                    Assert.IsTrue((bool)gameType.GetMethod("ActivatePlayer", PublicInstance).Invoke(reopened,
                        new object[] { zeroSave, PersistDelegate() }));
                    Assert.AreEqual(zeroBank, Read(Read(reopened, "Market"), "BalanceCents"), "Legacy zero-payout repair must not refund an intentionally lost Chulengo turn reward.");
                    Assert.IsFalse((bool)Read(reopened, "HasPendingChulengo"));
                    Assert.AreEqual(1L, (long)Field(Field(reopened, "save"), "statistics").GetType()
                        .GetField("wins", AnyInstance).GetValue(Field(Field(reopened, "save"), "statistics")));
                    UnityEngine.Object.DestroyImmediate(reopenedHost);
                }
                UnityEngine.Object.DestroyImmediate(isolated);
            }
        }

        [Test]
        public void ExactNeedlePositionIsFrozenInsteadOfSnappingToSectorCenter()
        {
            object game = NewGame();
            ForgeWonPhase(game);
            const float position = .467f;
            Assert.IsTrue((bool)Invoke(game, "TryMultiplyVictoryRewardAtPosition", position));
            Assert.AreEqual(3, Read(game, "ChulengoSelectedSector"));
            Assert.AreEqual(position, (float)Read(game, "ChulengoSelectedPosition"), .000001f);
            string snapshot = savedJson;
            UnityEngine.Object.DestroyImmediate(host);
            host = new GameObject("Chulengo precise needle reload");
            object reloaded = NewGame(snapshot);
            Assert.AreEqual(position, (float)Read(reloaded, "ChulengoSelectedPosition"), .000001f);
            Assert.IsFalse((bool)Invoke(reloaded, "TryMultiplyVictoryRewardAtPosition", .9f));
        }

        [Test]
        public void FailedFinalPersistenceRetriesSameLockedChoiceOnly()
        {
            object game = NewGame();
            ForgeWonPhase(game);
            failAtCall = persistCalls + 2; // Lock save succeeds; only the final claim commit fails.
            Assert.IsFalse((bool)Invoke(game, "TryMultiplyVictoryReward", 3));
            Assert.IsTrue((bool)Read(game, "ChulengoChoiceLocked"));
            Assert.AreEqual(3, Read(game, "ChulengoSelectedSector"));
            long frozen = (long)Read(game, "ChulengoFinalRewardCents");
            Assert.IsFalse((bool)Invoke(game, "TryMultiplyVictoryReward", 0));
            Assert.AreEqual(frozen, Read(game, "ChulengoFinalRewardCents"));
            Assert.AreEqual(0, successfulMatchWrites, "Rejected final persistence must not acknowledge a match.");
            failAtCall = -1;
            Assert.IsTrue((bool)Invoke(game, "TryMultiplyVictoryReward", 3));
            Assert.IsFalse((bool)Read(game, "HasPendingChulengo"));
            Assert.IsNotNull(capturedMatch);
            Assert.AreEqual(1, successfulMatchWrites, "Retry may acknowledge the final match only once.");
        }

        [Test]
        public void PendingChoiceSurvivesReloadAndLatestLevelHasNoNextLevel()
        {
            object game = NewGame();
            ForgeWonPhase(game);
            string pending = savedJson;
            UnityEngine.Object.DestroyImmediate(host);
            host = new GameObject("Chulengo reload fixture");
            object reloaded = NewGame(pending);
            Assert.IsTrue((bool)Read(reloaded, "ShowChulengoResult"));
            Assert.IsFalse((bool)Read(reloaded, "CanStartRound"));
            Assert.IsFalse((bool)Invoke(reloaded, "TryStartRound"));
            Assert.IsTrue((bool)Invoke(reloaded, "TryCollectVictoryReward"));
            Assert.IsFalse((bool)Read(reloaded, "HasPendingChulengo"));

            UnityEngine.Object.DestroyImmediate(host);
            host = new GameObject("Chulengo latest level fixture");
            object latest = NewGame();
            object save = Field(latest, "save");
            Put(save, "unlockedLevel", 10);
            Assert.IsTrue((bool)Invoke(latest, "SelectLevel", 10));
            object latestData = Read(Read(latest, "Market"), "Data");
            // Test-only inventory fixture satisfies the last level's shared-recipe stock gate.
            Put(latestData, "stockUnits", new int[] { 10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000 });
            ForgeWonPhase(latest);
            Assert.AreEqual(10, Read(latest, "SelectedLevel"));
            Assert.IsFalse((bool)Invoke(latest, "NextLevel"));
            Assert.IsTrue((bool)Invoke(latest, "TryCollectVictoryReward"));
        }
    }
}
