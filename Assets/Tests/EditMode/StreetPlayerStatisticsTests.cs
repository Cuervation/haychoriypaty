using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Runtime types live in predefined Assembly-CSharp; keep this asmdef suite reflection-only.</summary>
    public sealed class StreetPlayerStatisticsTests
    {
        private static bool AbsentRecord(object value) => value == null ||
            (string.IsNullOrEmpty((string)ReadField(value, "id")) && string.IsNullOrEmpty((string)ReadField(value, "attemptId")));
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;
        private Type statisticsType, recordType, gameType, saveDataType, simulationType;
        private Func<string, object, bool> persistHandler;

        [SetUp]
        public void SetUp()
        {
            statisticsType = RuntimeType("StreetPlayerStatistics");
            recordType = RuntimeType("StreetMatchRecord");
            gameType = RuntimeType("StreetGame");
            saveDataType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            simulationType = RuntimeType("StreetSimulation");
        }

        private static Type RuntimeType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static void SetEditorTestInitialization(Type type, bool value) => type.GetField("AllowEditorTestInitialization", StaticPublic).SetValue(null, value);
        private static object ReadProperty(object instance, string name) => instance.GetType().GetProperty(name, PublicInstance).GetValue(instance, null);
        private static object ReadField(object instance, string name) => instance.GetType().GetField(name, PublicInstance | NonPublicInstance).GetValue(instance);
        private static void WriteField(object instance, string name, object value) => instance.GetType().GetField(name, PublicInstance | NonPublicInstance).SetValue(instance, value);
        private static object Invoke(object instance, string method, params object[] args) => instance.GetType().GetMethod(method, PublicInstance).Invoke(instance, args);
        private static object Deserialize(Type type, string json) => JsonUtility.FromJson(json, type);

        private Delegate PersistDelegate(Type targetType)
        {
            Type callbackType = typeof(Func<,,>).MakeGenericType(typeof(string), targetType, typeof(bool));
            MethodInfo generic = GetType().GetMethod(nameof(Persist), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(targetType);
            return Delegate.CreateDelegate(callbackType, this, generic);
        }

        private bool Persist<T>(string json, T record) => persistHandler(json, record);

        private object NewGame(GameObject gameObject) => gameObject.AddComponent(gameType);

        private bool Activate(object game, string json, Func<string, object, bool> handler)
        {
            persistHandler = handler;
            return (bool)gameType.GetMethod("ActivatePlayer", PublicInstance).Invoke(game, new object[] { json, PersistDelegate(recordType) });
        }

        private object NewStatistics() => Activator.CreateInstance(statisticsType);
        private object NewRecord() => Activator.CreateInstance(recordType);
        private bool Apply(object statistics, object record) => (bool)statisticsType.GetMethod("ApplyMatch", PublicInstance).Invoke(statistics, new[] { record });
        private bool ApplyRecovered(object statistics, object summary) =>
            (bool)statisticsType.GetMethod("ApplyRecoveredAttemptMetrics", PublicInstance).Invoke(statistics, new[] { summary });
        private static long[] LongArray(object instance, string field) => (long[])ReadField(instance, field);

        [Test]
        public void MatchCountersUseFixedCatalogSizesAndApplyAttemptOnce()
        {
            object statistics = NewStatistics();
            object record = NewRecord();
            WriteField(record, "id", "record-id"); WriteField(record, "attemptId", "attempt-id");
            WriteField(record, "level", 4); WriteField(record, "won", true);
            WriteField(record, "elapsedSeconds", 42f); WriteField(record, "grossSalesCents", 12000L);
            WriteField(record, "salesByProduct", new long[] { 2, 1, 0, 0, 0, 0, 0 });
            WriteField(record, "stockConsumed", new long[10]); WriteField(record, "stockRemaining", new long[10]);
            WriteField(record, "hiresByRole", new long[5]);
            WriteField(record, "customersAttended", 3L); WriteField(record, "customersLost", 1L); WriteField(record, "ordersCompleted", 2L);
            Assert.IsTrue(Apply(statistics, record));
            Assert.IsFalse(Apply(statistics, record), "A retry for the current attempt must not double counters.");
            Assert.AreEqual(2, LongArray(statistics, "deliveredByProduct")[0]);
            Assert.AreEqual(1, LongArray(statistics, "deliveredByProduct")[1]);
            Assert.AreEqual(1L, ReadField(statistics, "wins"));
            Assert.AreEqual(1, LongArray(statistics, "winsByLevel")[4]);
            Assert.AreEqual(42f, ((float[])ReadField(statistics, "bestWinTimeByLevel"))[4]);
            Assert.AreEqual(3L, ReadField(statistics, "customersAttended"));
            Assert.AreEqual(1L, ReadField(statistics, "customersLost"));
            Assert.AreEqual(2L, ReadField(statistics, "ordersCompleted"));
            Assert.AreEqual(7, LongArray(statistics, "deliveredByProduct").Length);
            Assert.AreEqual(10, LongArray(statistics, "consumedUnitsByIngredient").Length);
            Assert.AreEqual(5, LongArray(statistics, "paidHiresByRole").Length);
            Assert.AreEqual(11, LongArray(statistics, "winsByLevel").Length);
        }

        [Test]
        public void MatchAggregatesSalesRewardsAndInitialCoinsWithoutDoubleCountingHires()
        {
            object statistics = NewStatistics();
            WriteField(statistics, "paidHiresByRole", new long[] { 1, 0, 0, 0, 0 });
            object record = NewRecord();
            WriteField(record, "attemptId", "attempt-ledger"); WriteField(record, "won", true);
            WriteField(record, "grossSalesCents", 12345L); WriteField(record, "rewardCents", 4000L);
            WriteField(record, "initialCoinsCents", 2500L); WriteField(record, "hiresByRole", new long[] { 1, 0, 0, 0, 0 });
            WriteField(record, "salesByProduct", new long[7]); WriteField(record, "stockConsumed", new long[10]);
            Assert.IsTrue(Apply(statistics, record));
            Assert.AreEqual(12345L, ReadField(statistics, "grossSalesCents"));
            Assert.AreEqual(4000L, ReadField(statistics, "totalMarketRewardsCents"));
            Assert.AreEqual(2500L, ReadField(statistics, "initialFreeCoinsCents"));
            Assert.AreEqual(1L, LongArray(statistics, "paidHiresByRole")[0], "The hire is counted at purchase, not again from its receipt.");
        }

        [Test]
        public void AbandonedMatchIsNotCountedAsFinishedWinOrLoss()
        {
            object statistics = NewStatistics();
            object record = NewRecord();
            WriteField(record, "id", "attempt-abandoned"); WriteField(record, "attemptId", "attempt-abandoned");
            WriteField(record, "abandoned", true); WriteField(record, "result", "abandoned");
            WriteField(record, "grossSalesCents", 500L); WriteField(record, "salesByProduct", new long[7]);
            WriteField(record, "stockConsumed", new long[10]);
            Assert.IsTrue(Apply(statistics, record));
            Assert.AreEqual(1L, ReadField(statistics, "abandons"));
            Assert.AreEqual(0L, ReadField(statistics, "attemptsFinished"));
            Assert.AreEqual(0L, ReadField(statistics, "wins"));
            Assert.AreEqual(0L, ReadField(statistics, "losses"));
            Assert.AreEqual(500L, ReadField(statistics, "grossSalesCents"));
            Assert.AreEqual(0L, ReadField(statistics, "totalMarketRewardsCents"));
        }

        [Test]
        public void RecoveredAttemptTelemetryIsDeduplicatedWithoutInventingAnOutcome()
        {
            object statistics = NewStatistics();
            object summary = NewRecord();
            WriteField(summary, "attemptId", "recovered-attempt");
            WriteField(summary, "result", "incomplete"); WriteField(summary, "abandoned", false);
            WriteField(summary, "salesByProduct", new long[] { 2, 1, 0, 0, 0, 0, 0 });
            WriteField(summary, "stockConsumed", new long[] { 2, 2, 1, 1, 0, 0, 0, 0, 0, 0 });
            WriteField(summary, "grossSalesCents", 875L); WriteField(summary, "customersAttended", 2L);
            WriteField(summary, "customersLost", 1L); WriteField(summary, "ordersCompleted", 1L);
            WriteField(summary, "rewardCents", 900L);
            Assert.IsTrue(ApplyRecovered(statistics, summary));
            Assert.IsFalse(ApplyRecovered(statistics, summary), "A replay of a recovered checkpoint must not duplicate metrics.");
            Assert.AreEqual(3L, ReadField(statistics, "completedProducts"));
            Assert.AreEqual(875L, ReadField(statistics, "grossSalesCents"));
            Assert.AreEqual(2L, ReadField(statistics, "customersAttended"));
            Assert.AreEqual(1L, ReadField(statistics, "customersLost"));
            Assert.AreEqual(1L, ReadField(statistics, "ordersCompleted"));
            Assert.AreEqual(0L, ReadField(statistics, "attemptsFinished"));
            Assert.AreEqual(0L, ReadField(statistics, "wins"));
            Assert.AreEqual(0L, ReadField(statistics, "losses"));
            Assert.AreEqual(0L, ReadField(statistics, "abandons"));
            Assert.AreEqual(0L, ReadField(statistics, "totalMarketRewardsCents"));
        }

        [Test]
        public void LoadingActiveAttemptCheckpointKeepsRealMetricsButDoesNotCreateResult()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("active-attempt-recovery-test");
            var recoveredGo = new GameObject("active-attempt-recovery-target");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object sourceGame = NewGame(go);
                Assert.IsTrue(Activate(sourceGame, null, (json, match) => true));
                object sourceSave = ReadField(sourceGame, "save");
                object sourceMarket = ReadProperty(sourceGame, "Market");
                object marketData = ReadProperty(sourceMarket, "Data");
                int[] stock = (int[])ReadField(marketData, "stockUnits");
                WriteField(marketData, "attemptStock", (int[])stock.Clone());
                WriteField(marketData, "attemptActive", true);
                WriteField(sourceSave, "activeAttemptId", "crashed-attempt");
                object summary = NewRecord();
                WriteField(summary, "attemptId", "crashed-attempt"); WriteField(summary, "id", "crashed-attempt");
                WriteField(summary, "result", "incomplete"); WriteField(summary, "abandoned", false);
                WriteField(summary, "salesByProduct", new long[] { 3, 1, 0, 0, 0, 0, 0 });
                WriteField(summary, "stockConsumed", new long[] { 3, 3, 1, 1, 0, 0, 0, 0, 0, 0 });
                WriteField(summary, "grossSalesCents", 1125L); WriteField(summary, "customersAttended", 2L);
                WriteField(sourceSave, "activeAttemptSummary", summary);
                string interruptedJson = JsonUtility.ToJson(sourceSave);

                object recoveredGame = NewGame(recoveredGo);
                string persistedJson = null;
                Assert.IsTrue(Activate(recoveredGame, interruptedJson, (json, match) => { persistedJson = json; return true; }));
                object recoveredSave = Deserialize(saveDataType, persistedJson);
                object statistics = ReadField(recoveredSave, "statistics");
                Assert.AreEqual(4L, ReadField(statistics, "completedProducts"));
                Assert.AreEqual(1125L, ReadField(statistics, "grossSalesCents"));
                Assert.AreEqual(0L, ReadField(statistics, "attemptsFinished"));
                Assert.AreEqual(0L, ReadField(statistics, "wins"));
                Assert.AreEqual(0L, ReadField(statistics, "losses"));
                Assert.AreEqual(0L, ReadField(statistics, "abandons"));
                Assert.IsTrue(AbsentRecord(ReadField(recoveredSave, "activeAttemptSummary")));
                Assert.IsTrue(string.IsNullOrEmpty((string)ReadField(recoveredSave, "activeAttemptId")));
                Assert.IsFalse((bool)ReadField(ReadField(recoveredSave, "market"), "attemptActive"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(recoveredGo);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void SaveAcknowledgesMatchSeparatelyAndDoesNotSerializeItForRequeue()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("cloud-match-outbox-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go);
                string capturedJson = null; object capturedMatch = null; bool allowWrite = true;
                Assert.IsTrue(Activate(game, null, (json, match) => { capturedJson = json; capturedMatch = match; return allowWrite; }));
                allowWrite = false;
                object matchRecord = NewRecord();
                WriteField(matchRecord, "id", "attempt-stable"); WriteField(matchRecord, "attemptId", "attempt-stable");
                WriteField(matchRecord, "won", true); WriteField(matchRecord, "level", 0);
                object save = ReadField(game, "save");
                WriteField(save, "pendingMatch", matchRecord); WriteField(save, "activeAttemptId", "attempt-stable");
                object summary = NewRecord();
                WriteField(summary, "attemptId", "attempt-stable"); WriteField(summary, "result", "incomplete");
                WriteField(save, "activeAttemptSummary", summary);
                MethodInfo saveMethod = gameType.GetMethod("SaveWithMatch", NonPublicInstance);
                Assert.IsFalse((bool)saveMethod.Invoke(game, new object[] { null }));
                Assert.AreSame(matchRecord, ReadField(save, "pendingMatch"), "Failed commit retains the exact retry record.");
                allowWrite = true;
                Assert.IsTrue((bool)saveMethod.Invoke(game, new object[] { null }));
                Assert.AreSame(matchRecord, capturedMatch);
                Assert.AreEqual("attempt-stable", ReadField(capturedMatch, "id"));
                object committed = Deserialize(saveDataType, capturedJson);
                Assert.IsTrue(AbsentRecord(ReadField(committed, "pendingMatch")));
                Assert.IsTrue(string.IsNullOrEmpty((string)ReadField(committed, "activeAttemptId")));
                Assert.IsTrue(AbsentRecord(ReadField(committed, "activeAttemptSummary")), "Terminal snapshot must not retain private in-flight telemetry.");
                Assert.IsTrue(string.IsNullOrEmpty((string)ReadField(ReadField(committed, "statistics"), "lastRecoveredAttemptId")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void ExplicitReturnDuringRoundPersistsAbandonedAttemptAndRestoresStock()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("abandoned-match-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go); object capturedMatch = null;
                Assert.IsTrue(Activate(game, null, (json, match) => { if (match != null) capturedMatch = match; return true; }));
                if (!(bool)Invoke(game, "TryStartRound")) Assert.Ignore("Default market does not provide the level-one start floor.");
                Assert.IsTrue((bool)Invoke(game, "ReturnToLevels"));
                Assert.IsNotNull(capturedMatch);
                Assert.AreEqual("abandoned", ReadField(capturedMatch, "result"));
                Assert.IsTrue((bool)ReadField(capturedMatch, "abandoned"));
                Assert.AreEqual(ReadField(capturedMatch, "attemptId"), ReadField(capturedMatch, "id"));
                object saved = Deserialize(saveDataType, (string)ReadProperty(game, "ActiveSaveJson"));
                Assert.AreEqual(1L, ReadField(ReadField(saved, "statistics"), "abandons"));
                object market = ReadProperty(game, "Market");
                object marketData = ReadProperty(market, "Data");
                Assert.IsFalse((bool)ReadField(marketData, "attemptActive"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void FailedHireSaveIsNotConfirmedAndFreezesFurtherCommands()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("failed-hire-save-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go); bool failWrites = false;
                Assert.IsTrue(Activate(game, null, (json, match) => !failWrites));
                object simulation = ReadProperty(game, "Sim");
                int cost = (int)ReadProperty(simulation, "HireCost");
                simulationType.GetField("<Coins>k__BackingField", NonPublicInstance).SetValue(simulation, cost + 100);
                failWrites = true;
                Assert.IsFalse((bool)Invoke(game, "TryHireParrillero"));
                Assert.IsFalse((bool)ReadProperty(game, "IsPlayerActive"));
                Assert.IsNull(ReadProperty(game, "Sim"));
                object saved = Deserialize(saveDataType, (string)ReadProperty(game, "ActiveSaveJson"));
                Assert.AreEqual(0L, LongArray(ReadField(saved, "statistics"), "paidHiresByRole")[0]);
                Assert.IsFalse((bool)Invoke(game, "TryHireParrillero"), "Failed persistence must block subsequent economic commands.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void LegacyStatisticsNormalizeMissingAndOversizedArraysWithoutInventingHistory()
        {
            object statistics = NewStatistics();
            WriteField(statistics, "deliveredByProduct", new long[] { 7, 2 });
            WriteField(statistics, "winsByLevel", null);
            statisticsType.GetMethod("Normalize", PublicInstance).Invoke(statistics, null);
            Assert.AreEqual(7L, LongArray(statistics, "deliveredByProduct")[0]);
            Assert.AreEqual(2L, LongArray(statistics, "deliveredByProduct")[1]);
            Assert.AreEqual(0L, LongArray(statistics, "deliveredByProduct")[2]);
            Assert.AreEqual(11, LongArray(statistics, "winsByLevel").Length);
            Assert.AreEqual(0L, LongArray(statistics, "winsByLevel")[0]);
        }

        [Test]
        public void JsonUtilityEmptyOptionalRecordsNeverEmitPhantomResultsAfterReload()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var firstGo = new GameObject("empty-result-first"); var secondGo = new GameObject("empty-result-reload");
            try
            {
                SetEditorTestInitialization(gameType, false);
                string saved = null; object result = null;
                Assert.IsTrue(Activate(NewGame(firstGo), null, (json, match) => { saved = json; return true; }));
                Assert.IsTrue(Activate(NewGame(secondGo), saved, (json, match) => { result = match; return true; }));
                Assert.IsNull(result, "Inline DTO nulls reconstructed by Unity must not enter history/outbox.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstGo); UnityEngine.Object.DestroyImmediate(secondGo);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void ProductSalesBadgesUseInclusiveIndependentCumulativeThresholds(int product)
        {
            Type catalog = RuntimeType("StreetSalesBadges");
            var go = new GameObject("sales-badge-boundary-test");
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go); Assert.IsTrue(Activate(game, null, (json, record) => true));
                long[] totals = LongArray(ReadField(ReadField(game, "save"), "statistics"), "deliveredByProduct");
                long[] boundaries = { 499L, 500L, 2499L, 2500L, 4999L, 5000L, long.MaxValue };
                int[] expected = { 0, 1, 1, 2, 2, 3, 3 };
                for (int n = 0; n < boundaries.Length; n++)
                {
                    totals[product] = boundaries[n];
                    for (int p = 0; p < 7; p++)
                    {
                        long value = (long)Invoke(game, "GetCumulativeProductSales", p);
                        Assert.AreEqual(p == product ? boundaries[n] : 0L, value);
                        Assert.AreEqual(p == product ? expected[n] : 0,
                            catalog.GetMethod("EarnedCount").Invoke(null, new object[] { value }));
                    }
                }
                totals[product] = -1; Assert.AreEqual(0L, Invoke(game, "GetCumulativeProductSales", product));
                Assert.AreEqual(0L, Invoke(game, "GetCumulativeProductSales", -1));
                Assert.AreEqual(0L, Invoke(game, "GetCumulativeProductSales", 7));
                Assert.AreEqual(500L, catalog.GetMethod("Threshold").Invoke(null, new object[] { 0 }));
                Assert.AreEqual(2500L, catalog.GetMethod("Threshold").Invoke(null, new object[] { 1 }));
                Assert.AreEqual(5000L, catalog.GetMethod("Threshold").Invoke(null, new object[] { 2 }));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); SetEditorTestInitialization(gameType, prior); }
        }

        [Test]
        public void EarnedSalesBadgesAreDurableAndAwardedOnceFromSettledAndRecoveredSales()
        {
            object stats = NewStatistics();
            var totals = LongArray(stats, "deliveredByProduct"); totals[0] = 499;
            statisticsType.GetMethod("Normalize").Invoke(stats, null);
            Assert.IsFalse(((bool[])ReadField(stats, "earnedProductSalesBadges"))[0]);
            object record = NewRecord(); WriteField(record, "attemptId", "badge-terminal");
            WriteField(record, "salesByProduct", new long[] { 1, 0, 0, 0, 0, 0, 0 });
            Assert.IsTrue(Apply(stats, record)); Assert.IsFalse(Apply(stats, record));
            Assert.IsTrue(((bool[])ReadField(stats, "earnedProductSalesBadges"))[0]);
            Assert.AreEqual(500L, LongArray(stats, "deliveredByProduct")[0]);
            object checkpoint = NewRecord(); WriteField(checkpoint, "attemptId", "badge-checkpoint");
            WriteField(checkpoint, "salesByProduct", new long[] { 0, 0, 0, 0, 0, 0, 5000 });
            Assert.IsTrue(ApplyRecovered(stats, checkpoint)); Assert.IsFalse(ApplyRecovered(stats, checkpoint));
            bool[] flags = (bool[])ReadField(stats, "earnedProductSalesBadges");
            Assert.AreEqual(21, flags.Length);
            Assert.IsTrue(flags[18]); Assert.IsTrue(flags[19]); Assert.IsTrue(flags[20]);
            Assert.IsFalse(flags[3], "A Chori medal never awards a Paty medal.");
            object restored = Deserialize(statisticsType, JsonUtility.ToJson(stats));
            LongArray(restored, "deliveredByProduct")[0] = 0;
            statisticsType.GetMethod("Normalize").Invoke(restored, null);
            Assert.IsTrue(((bool[])ReadField(restored, "earnedProductSalesBadges"))[0], "Persisted achievements stay obtained, not recomputed away.");
            object legacy = Deserialize(statisticsType, "{\"deliveredByProduct\":[5000]}");
            statisticsType.GetMethod("Normalize").Invoke(legacy, null);
            bool[] migrated = (bool[])ReadField(legacy, "earnedProductSalesBadges");
            Assert.IsTrue(migrated[0]); Assert.IsTrue(migrated[1]); Assert.IsTrue(migrated[2]);
            for (int i = 3; i < 21; i++) Assert.IsFalse(migrated[i]);
        }

        [Test]
        public void ProductSalesBadgesSurviveUidSnapshotOutboxAckAndReactivation()
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sales-badge-uid-" + Guid.NewGuid().ToString("N"));
            Type repository = Type.GetType("HayChoriYPaty.Cloud.PlayerLocalRepository, Assembly-CSharp", true);
            dynamic repo = Activator.CreateInstance(repository, root, "badge-owner");
            var go = new GameObject("sales-badge-persistence-test");
            var reload = new GameObject("sales-badge-restored-test");
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            try
            {
                SetEditorTestInitialization(gameType, false);
                object seed = Activator.CreateInstance(saveDataType);
                object stats = ReadField(seed, "statistics");
                long[] expected = { 499L, 500L, 2499L, 2500L, 4999L, 5000L, 16000L };
                WriteField(stats, "deliveredByProduct", expected);
                WriteField(stats, "instrumentationStartedAtUtc", "2026-10-23T00:00:00Z");
                object game = NewGame(go);
                Assert.IsTrue(Activate(game, JsonUtility.ToJson(seed), (json, record) => repo.Commit(json, (dynamic)record)));
                repo = Activator.CreateInstance(repository, root, "badge-owner");
                string snapshot = repo.State.snapshot;
                Assert.That(snapshot, Does.Contain("earnedProductSalesBadges"), "Obtained flags themselves must be in the Firebase-bound JSON, not only sales totals.");
                Assert.AreEqual(snapshot, (string)repo.State.pending[0].snapshot, "Cloud outbox transports the full badge source snapshot.");
                string id = repo.State.pending[0].id;
                Assert.IsTrue(repo.Commit(snapshot, null)); Assert.AreEqual(1, (int)repo.State.pending.Count, "Reads/retries cannot create duplicate badge operations.");
                repo.Acknowledge(id, 1L); repo = Activator.CreateInstance(repository, root, "badge-owner");
                Assert.AreEqual(0, (int)repo.State.pending.Count);
                object restored = NewGame(reload);
                Assert.IsTrue(Activate(restored, (string)repo.State.snapshot, (json, record) => true));
                for (int p = 0; p < 7; p++)
                {
                    Assert.AreEqual(expected[p], Invoke(restored, "GetCumulativeProductSales", p));
                    for (int tier = 0; tier < 3; tier++)
                        Assert.AreEqual(expected[p] >= (tier == 0 ? 500 : tier == 1 ? 2500 : 5000), Invoke(restored, "HasProductSalesBadge", p, tier));
                }
                Assert.IsFalse((bool)Invoke(restored, "HasProductSalesBadge", -1, 0));
                Assert.IsFalse((bool)Invoke(restored, "HasProductSalesBadge", 0, 3));
                dynamic other = Activator.CreateInstance(repository, root, "other-owner");
                Assert.IsNull((string)other.State.snapshot, "Another UID cannot inherit this collection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(reload);
                SetEditorTestInitialization(gameType, prior);
                if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
            }
        }

        [Test]
        public void PerClubBadgeProgressCanReadExistingWinsIncludingTheFinalLevel()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("per-club-badge-progress-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go);
                Assert.IsTrue(Activate(game, null, (json, match) => true));
                Assert.IsFalse((bool)Invoke(game, "HasWonLevel", 10), "A new profile has no final-club badge.");
                object save = ReadField(game, "save");
                object statistics = ReadField(save, "statistics");
                LongArray(statistics, "winsByLevel")[10] = 1;
                Assert.IsTrue((bool)Invoke(game, "HasWonLevel", 10), "The existing per-level win statistic grants the final-club badge.");
                Assert.IsFalse((bool)Invoke(game, "HasWonLevel", -1));
                Assert.IsFalse((bool)Invoke(game, "HasWonLevel", 11));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void StreetGameWaitsForAuthorizedJsonAndFailsClosedOnInvalidSave()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("cloud-gate-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go);
                Assert.IsFalse((bool)ReadProperty(game, "IsPlayerActive"));
                Assert.IsNull(ReadProperty(game, "Sim"));
                Assert.IsFalse((bool)Invoke(game, "TryStartRound"));
                Assert.IsFalse(Activate(game, "not-json", (json, match) => true));
                Assert.IsFalse((bool)ReadProperty(game, "IsPlayerActive"));
                string written = null;
                Assert.IsTrue(Activate(game, null, (json, match) => { written = json; return true; }));
                Assert.IsTrue((bool)ReadProperty(game, "IsPlayerActive"));
                StringAssert.Contains("\"version\":4", written);
                Assert.IsTrue((bool)Invoke(game, "SuspendPlayer"));
                Assert.IsFalse((bool)ReadProperty(game, "IsPlayerActive"));
                Assert.IsNull(ReadProperty(game, "Sim"));
                Assert.IsNull(ReadProperty(game, "Market"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }

        [Test]
        public void FailedSuspendFreezesCommandsAndKeepsStateForRetry()
        {
            bool prior = (bool)gameType.GetField("AllowEditorTestInitialization", StaticPublic).GetValue(null);
            var go = new GameObject("cloud-suspend-retry-test");
            try
            {
                SetEditorTestInitialization(gameType, false);
                object game = NewGame(go); bool fail = false;
                Assert.IsTrue(Activate(game, null, (json, match) => !fail));
                object priorSimulation = ReadField(game, "sim");
                fail = true;
                Assert.IsFalse((bool)Invoke(game, "SuspendPlayer"));
                Assert.IsFalse((bool)ReadProperty(game, "IsPlayerActive"));
                Assert.IsNull(ReadProperty(game, "Sim"), "Do not expose gameplay references after access is frozen.");
                Assert.AreSame(priorSimulation, ReadField(game, "sim"), "Retain state internally until persistence succeeds.");
                Assert.IsFalse((bool)Invoke(game, "TryStartRound"));
                Assert.IsFalse((bool)Invoke(game, "TryHireParrillero"));
                fail = false;
                Assert.IsTrue((bool)Invoke(game, "SuspendPlayer"));
                Assert.IsNull(ReadProperty(game, "Sim"));
                Assert.IsNull(ReadProperty(game, "Market"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                SetEditorTestInitialization(gameType, prior);
            }
        }
    }
}
