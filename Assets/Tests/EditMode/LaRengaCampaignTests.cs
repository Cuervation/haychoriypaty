using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class LaRengaCampaignTests
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

        private static Type RuntimeType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static void Set(object instance, string name, object value) => instance.GetType().GetField(name).SetValue(instance, value);
        private static object Get(object instance, string name) => instance.GetType().GetField(name).GetValue(instance);
        private static object ReadProperty(object instance, string name) => instance.GetType().GetProperty(name, AnyInstance).GetValue(instance, null);
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<GameObject> hosts = new List<GameObject>();
        private int persistedMatchCount;
        private object durableRepository;
        private static void SetField(object instance, string name, object value) => instance.GetType().GetField(name, AnyInstance).SetValue(instance, value);
        private static object Field(object instance, string name) => instance.GetType().GetField(name, AnyInstance).GetValue(instance);
        private static object Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(instance, args);
        private static object InvokeAny(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, AnyInstance).Invoke(instance, args);
        private bool Persist<T>(string json, T match)
        {
            if (match != null) persistedMatchCount++;
            return durableRepository == null || (bool)RepoType.GetMethod("Commit").Invoke(durableRepository, new object[] { json, match });
        }
        private Delegate PersistDelegate(Type recordType)
        {
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), recordType, typeof(bool));
            MethodInfo generic = GetType().GetMethod(nameof(Persist), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(recordType);
            return Delegate.CreateDelegate(callback, this, generic);
        }
        private object NewGame(string json)
        {
            Type type = RuntimeType("StreetGame");
            var host = new GameObject("La Renga campaign test"); hosts.Add(host);
            object game = host.AddComponent(type);
            Assert.IsTrue((bool)Invoke(game, "ActivatePlayer", json, PersistDelegate(RuntimeType("StreetMatchRecord"))));
            return game;
        }
        private static Type RepoType => Type.GetType("HayChoriYPaty.Cloud.PlayerLocalRepository, Assembly-CSharp", true);
        private static object NewRepository(string root, string uid) => Activator.CreateInstance(RepoType, root, uid);
        private static object RepoState(object repo) => RepoType.GetProperty("State").GetValue(repo, null);
        private static IList Pending(object repo) => (IList)Field(RepoState(repo), "pending");
        private static object OperationField(object operation, string name) => operation.GetType().GetField(name).GetValue(operation);
        private static bool Commit(object repo, string snapshot) => (bool)RepoType.GetMethod("Commit").Invoke(repo, new object[] { snapshot, null });
        private static string HashSnapshot(string snapshot) => (string)RepoType.GetMethod("Hash", PublicStatic).Invoke(null, new object[] { snapshot });
        private static void ValidateSnapshot(string json)
        {
            try { RepoType.GetMethod("ValidateSnapshot", PublicStatic).Invoke(null, new object[] { json }); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); }
        }
        [TearDown] public void TearDown()
        {
            durableRepository = null;
            foreach (var host in hosts) if (host != null) UnityEngine.Object.DestroyImmediate(host);
            hosts.Clear();
        }

        [Test]
        public void CampaignOrderAppendsLaRengaBeforeUnchangedTraditionalIds()
        {
            Type simulation = RuntimeType("StreetSimulation");
            Assert.AreEqual(new[] { 0, 1, 9, 2, 12, 3, 11, 10, 13, 14, 4, 5, 6, 7, 8 },
                simulation.GetField("CampaignOrder", PublicStatic).GetValue(null));
            Assert.AreEqual(15, ((string[])simulation.GetField("LevelNames", PublicStatic).GetValue(null)).Length);
            Assert.AreEqual(13, simulation.GetField("LaRengaTowerDefenseCampaignLevelId", PublicStatic).GetRawConstantValue());
            Assert.IsTrue((bool)simulation.GetMethod("IsTowerDefenseLevel", PublicStatic).Invoke(null, new object[] { 13 }));
        }

        [Test]
        public void V7MigrationCopiesOldBitsAndUnlocksLaRengaForLaterCampaignAccessOrRedondosVictory()
        {
            Type game = RuntimeType("StreetGame");
            Type saveType = game.GetNestedType("SaveData", BindingFlags.Public);
            Type statsType = RuntimeType("StreetPlayerStatistics");
            MethodInfo migrate = game.GetMethod("MigrateSaveData", NonPublicStatic);

            var later = Activator.CreateInstance(saveType);
            Set(later, "version", 7);
            var priorBits = new bool[13]; priorBits[0] = true; priorBits[4] = true; priorBits[10] = true;
            Set(later, "unlockedLevelIds", priorBits);
            var stats = Activator.CreateInstance(statsType); Set(later, "statistics", stats);
            object migrated = migrate.Invoke(null, new[] { later });
            Assert.AreEqual(9, Get(migrated, "version"));
            var expanded = (bool[])Get(migrated, "unlockedLevelIds");
            Assert.AreEqual(15, expanded.Length);
            Assert.IsTrue(expanded[4]); Assert.IsTrue(expanded[10]); Assert.IsTrue(expanded[13]);
            Assert.IsTrue(ReferenceEquals(migrated, migrate.Invoke(null, new[] { migrated })), "Migration must be idempotent.");

            var won = Activator.CreateInstance(saveType);
            Set(won, "version", 7);
            var noLaterBits = new bool[13]; noLaterBits[0] = true; Set(won, "unlockedLevelIds", noLaterBits);
            var wonStats = Activator.CreateInstance(statsType); var wins = new long[13]; wins[10] = 1;
            Set(wonStats, "winsByLevel", wins); Set(won, "statistics", wonStats);
            migrated = migrate.Invoke(null, new[] { won });
            Assert.IsTrue(((bool[])Get(migrated, "unlockedLevelIds"))[13]);
        }

        [Test]
        public void LegacyPendingTraditionalRedondosResultDoesNotUnlockLaRenga()
        {
            Type game = RuntimeType("StreetGame");
            Type saveType = game.GetNestedType("SaveData", BindingFlags.Public);
            object save = Activator.CreateInstance(saveType);
            Set(save, "version", 7);
            var bits = new bool[13]; bits[0] = true; Set(save, "unlockedLevelIds", bits);
            Set(save, "hasLegacyTraditionalLosRedondosResult", true);
            Set(save, "legacyTraditionalLosRedondosPendingAttemptId", "legacy-token");
            Type recordType = RuntimeType("StreetMatchRecord");
            object pending = Activator.CreateInstance(recordType);
            Set(pending, "level", 10); Set(pending, "won", true); Set(pending, "attemptId", "legacy-token");
            Set(save, "pendingVictoryMatch", pending);
            object migrated = game.GetMethod("MigrateSaveData", NonPublicStatic).Invoke(null, new[] { save });
            Assert.IsFalse(((bool[])Get(migrated, "unlockedLevelIds"))[13]);
        }

        [Test]
        public void LocalSnapshotValidatorAcceptsV7AndV8CampaignArrayShapesWithoutChangingEnvelope()
        {
            Assert.DoesNotThrow(() => ValidateSnapshot("{\"version\":7,\"unlockedLevel\":0,\"unlockedLevelIds\":[true,false,false,false,false,false,false,false,false,false,false,false,false]}"));
            Assert.DoesNotThrow(() => ValidateSnapshot("{\"version\":8,\"unlockedLevel\":0,\"unlockedLevelIds\":[true,false,false,false,false,false,false,false,false,false,false,false,false,false]}"));
            Assert.Throws<InvalidDataException>(() => ValidateSnapshot("{\"version\":7,\"unlockedLevel\":0,\"unlockedLevelIds\":[true,false,false,false,false,false,false,false,false,false,false,false,false,false]}"));
        }

        private static string NewCampaignSave(int unlockedId)
        {
            Type gameType = RuntimeType("StreetGame");
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object save = Activator.CreateInstance(saveType);
            Set(save, "version", 8); Set(save, "unlockedLevel", 10);
            var unlocked = new bool[14]; unlocked[0] = true; unlocked[unlockedId] = true;
            Set(save, "unlockedLevelIds", unlocked);
            Type ticketsType = RuntimeType("StreetTicketData");
            object tickets = Activator.CreateInstance(ticketsType);
            Set(tickets, "migrationApplied", true); Set(tickets, "balance", 0L);
            Set(tickets, "totalEarned", 0L); Set(tickets, "totalSpent", 0L); Set(save, "tickets", tickets);
            Type marketType = RuntimeType("StreetMarketData");
            object market = Activator.CreateInstance(marketType);
            Set(market, "balanceCents", 1_000_000L); Set(market, "stockUnits", new int[10]);
            Set(market, "kitGranted", true); Set(market, "initializedUnlockedLevel", 10); Set(save, "market", market);
            return JsonUtility.ToJson(save);
        }

        private void WinDefenseRound(object game, int levelId)
        {
            Assert.IsTrue((bool)Invoke(game, "SelectLevel", levelId));
            Assert.IsTrue((bool)Invoke(game, "TryStartTowerDefenseRound"));
            object sim = ReadProperty(game, "Sim");
            int[] deliveries = new int[7]; deliveries[0] = 1;
            int[] workers = (int[])Get(sim, "WorkerCountsByRole");
            Assert.IsTrue((bool)InvokeAny(sim, "ImportTowerDefenseState", 240f, 600, 250, 1, workers,
                deliveries, 8, 7, true, true));
            Assert.IsTrue((bool)Invoke(game, "ProcessRoundResult"));
            Assert.IsTrue((bool)Invoke(game, "TryCollectVictoryReward"));
        }

        [Test]
        public void CompletingRedondosThenLaRengaUnlocksSuccessorsAndRetriesCannotDuplicateRewards()
        {
            object game = NewGame(NewCampaignSave(10));
            Assert.AreEqual(0L, ReadProperty(game, "TicketBalance"));
            WinDefenseRound(game, 10);
            Assert.IsTrue((bool)Invoke(game, "IsLevelUnlocked", 13), "A settled ID10 victory must unlock ID13.");
            Assert.IsFalse((bool)Invoke(game, "TryCollectVictoryReward"));
            Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, ReadProperty(game, "TicketBalance"));
            Assert.AreEqual(1, persistedMatchCount);
            Assert.AreEqual(1L, ((long[])Field(Field(Field(game, "save"), "statistics"), "winsByLevel"))[10]);

            WinDefenseRound(game, 13);
            Assert.IsTrue((bool)Invoke(game, "IsLevelUnlocked", 14), "A settled ID13 victory must unlock Peregrinación ID14.");
            Assert.IsFalse((bool)Invoke(game, "TryCollectVictoryReward"));
            Assert.IsFalse((bool)Invoke(game, "ProcessRoundResult"));
            Assert.AreEqual(2L, ReadProperty(game, "TicketBalance"));
            Assert.AreEqual(2, persistedMatchCount);
            var wins = (long[])Field(Field(Field(game, "save"), "statistics"), "winsByLevel");
            Assert.AreEqual(1L, wins[10]); Assert.AreEqual(1L, wins[13]);
        }

        [Test]
        public void V7MigrationPreservesProgressAndEconomyAndDoesNotInferUnlockFromLegacyFrontierAlone()
        {
            Type gameType = RuntimeType("StreetGame");
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object save = Activator.CreateInstance(saveType);
            Set(save, "version", 7); Set(save, "unlockedLevel", 10); Set(save, "coins", 731); Set(save, "staff", 4);
            Set(save, "speed", 3); Set(save, "price", 8.5f); Set(save, "prices", new[] { 8f, 9f, 10f, 11f, 12f, 13f, 14f });
            var prior = new bool[13]; prior[0] = true; prior[3] = true; Set(save, "unlockedLevelIds", prior);
            object market = Activator.CreateInstance(RuntimeType("StreetMarketData"));
            Set(market, "balanceCents", 987654L); Set(market, "stockUnits", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var sales = new long[13]; sales[3] = 654321L; Set(market, "highestRecognizedSalesByLevel", sales);
            var bonuses = new bool[13]; bonuses[3] = true; Set(market, "bonusPaidByLevel", bonuses); Set(save, "market", market);
            object tickets = Activator.CreateInstance(RuntimeType("StreetTicketData"));
            Set(tickets, "balance", 14L); Set(tickets, "totalEarned", 20L); Set(tickets, "totalSpent", 6L);
            Set(tickets, "pendingWorkersByRole", new long[] { 1, 2, 0, 0, 0 }); Set(tickets, "migrationApplied", true); Set(save, "tickets", tickets);
            object stats = Activator.CreateInstance(RuntimeType("StreetPlayerStatistics"));
            var oldWins = new long[13]; oldWins[3] = 2; Set(stats, "winsByLevel", oldWins);
            var oldLosses = new long[13]; oldLosses[3] = 4; Set(stats, "lossesByLevel", oldLosses);
            var oldTimes = new float[13]; oldTimes[3] = 45.5f; Set(stats, "bestWinTimeByLevel", oldTimes);
            var oldGross = new long[13]; oldGross[3] = 12345L; Set(stats, "bestGrossCentsByLevel", oldGross);
            var oldUnits = new long[13]; oldUnits[3] = 77; Set(stats, "bestUnitsSoldByLevel", oldUnits); Set(stats, "wins", 12L);
            Set(save, "statistics", stats);

            MethodInfo migrate = gameType.GetMethod("MigrateSaveData", NonPublicStatic);
            object migrated = migrate.Invoke(null, new[] { save });
            var unlocked = (bool[])Get(migrated, "unlockedLevelIds");
            Assert.IsTrue(unlocked[0]); Assert.IsTrue(unlocked[3]); Assert.IsFalse(unlocked[13], "Legacy frontier=10 alone is not a valid ID10 TD win.");
            var copiedBits = new bool[13]; Array.Copy(unlocked, copiedBits, copiedBits.Length);
            CollectionAssert.AreEqual(prior, copiedBits, "Every v7 stable-ID unlock bit must survive unchanged.");
            Assert.AreEqual(10, Get(migrated, "unlockedLevel"));
            Assert.AreEqual(731, Get(migrated, "coins")); Assert.AreEqual(4, Get(migrated, "staff")); Assert.AreEqual(3, Get(migrated, "speed"));
            Assert.AreEqual(8.5f, Get(migrated, "price")); CollectionAssert.AreEqual(new[] { 8f, 9f, 10f, 11f, 12f, 13f, 14f }, (float[])Get(migrated, "prices"));
            object migratedMarket = Get(migrated, "market"); Assert.AreEqual(987654L, Get(migratedMarket, "balanceCents"));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, (int[])Get(migratedMarket, "stockUnits"));
            object migratedTickets = Get(migrated, "tickets"); Assert.AreEqual(14L, Get(migratedTickets, "balance")); Assert.AreEqual(20L, Get(migratedTickets, "totalEarned"));
            CollectionAssert.AreEqual(new long[] { 1, 2, 0, 0, 0 }, (long[])Get(migratedTickets, "pendingWorkersByRole"));

            object migratedStats = Get(migrated, "statistics");
            InvokeAny(migratedStats, "Normalize");
            Assert.AreEqual(15, ((long[])Get(migratedStats, "winsByLevel")).Length);
            Assert.AreEqual(2L, ((long[])Get(migratedStats, "winsByLevel"))[3]); Assert.AreEqual(12L, Get(migratedStats, "wins"));
            Assert.AreEqual(4L, ((long[])Get(migratedStats, "lossesByLevel"))[3]); Assert.AreEqual(45.5f, ((float[])Get(migratedStats, "bestWinTimeByLevel"))[3]);
            Assert.AreEqual(12345L, ((long[])Get(migratedStats, "bestGrossCentsByLevel"))[3]); Assert.AreEqual(77L, ((long[])Get(migratedStats, "bestUnitsSoldByLevel"))[3]);
            var marketRuntime = Activator.CreateInstance(RuntimeType("StreetMarket"), new[] { migratedMarket, Activator.CreateInstance(RuntimeType("StreetBalance")) });
            migratedMarket = ReadProperty(marketRuntime, "Data"); Set(migrated, "market", migratedMarket);
            Assert.AreEqual(15, ((long[])Get(migratedMarket, "highestRecognizedSalesByLevel")).Length);
            Assert.AreEqual(654321L, ((long[])Get(migratedMarket, "highestRecognizedSalesByLevel"))[3]);
            Assert.IsTrue(((bool[])Get(migratedMarket, "bonusPaidByLevel"))[3]);
            string once = JsonUtility.ToJson(migrated);
            object repeated = migrate.Invoke(null, new[] { migrated });
            Assert.AreSame(migrated, repeated); Assert.AreEqual(once, JsonUtility.ToJson(repeated));
        }

        [Test]
        public void V7OutboxOperationIsByteStableAcrossV8LiveSnapshotAndRestart()
        {
            string root = Path.Combine(Path.GetTempPath(), "hcyp-la-renga-outbox-" + Guid.NewGuid().ToString("N"));
            try
            {
                const string oldSnapshot = "{\"version\":7,\"unlockedLevel\":0,\"unlockedLevelIds\":[true,false,false,false,false,false,false,false,false,false,false,false,false]}";
                object repo = NewRepository(root, "campaign-upgrade-uid");
                Assert.IsTrue(Commit(repo, oldSnapshot));
                object original = Pending(repo)[0];
                string originalId = (string)OperationField(original, "id");
                string originalBytes = (string)OperationField(original, "snapshot");
                string originalHash = (string)OperationField(original, "payloadHash");
                long originalRevision = (long)OperationField(original, "expectedRevision");
                durableRepository = repo;
                object liveGame = NewGame(oldSnapshot); // ActivatePlayer loads v7 and commits its migrated v8 state.
                Assert.NotNull(liveGame);
                string upgraded = (string)OperationField(Pending(repo)[1], "snapshot");
                Assert.IsTrue(upgraded.Contains("\"version\":9"));
                durableRepository = null;
                Assert.AreEqual(2, Pending(repo).Count);
                object oldOperation = Pending(repo)[0];
                Assert.AreEqual(originalId, OperationField(oldOperation, "id")); Assert.AreEqual(originalBytes, OperationField(oldOperation, "snapshot"));
                Assert.AreEqual(originalHash, OperationField(oldOperation, "payloadHash")); Assert.AreEqual(originalRevision, OperationField(oldOperation, "expectedRevision"));
                Assert.AreEqual(HashSnapshot(originalBytes), originalHash);
                repo = NewRepository(root, "campaign-upgrade-uid");
                oldOperation = Pending(repo)[0];
                Assert.AreEqual(originalId, OperationField(oldOperation, "id")); Assert.AreEqual(originalBytes, OperationField(oldOperation, "snapshot"));
                Assert.AreEqual(originalHash, OperationField(oldOperation, "payloadHash")); Assert.AreEqual(originalRevision, OperationField(oldOperation, "expectedRevision"));
                Assert.AreEqual(1L, OperationField(Pending(repo)[1], "expectedRevision"));
                Assert.AreEqual(upgraded, OperationField(Pending(repo)[1], "snapshot"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void LaRengaConfigUsesSharedTwoProductTowerDefenseDefaults()
        {
            string path = RuntimeType("TowerDefenseLevelConfig").GetMethod("ResourcesPathForCampaignLevel", PublicStatic)
                .Invoke(null, new object[] { 13 }) as string;
            Assert.AreEqual("TowerDefense/LaRenga-AutodromoBuenosAires", path);
            Type configType = RuntimeType("TowerDefenseLevelConfig");
            var config = Resources.Load(path, configType);
            Assert.NotNull(config);
            Assert.AreEqual(13, configType.GetProperty("SourceCampaignLevelId").GetValue(config, null));
            Assert.AreEqual(60, configType.GetProperty("TotalFanCount").GetValue(config, null));
            Assert.AreEqual(0.1575f, configType.GetProperty("FanSpeed").GetValue(config, null));
            Assert.AreEqual(96f, configType.GetProperty("MinimumEntrySpacingLogical").GetValue(config, null));
            CollectionAssert.AreEqual(new[] { 0, 4 }, (int[])configType.GetProperty("AvailableProducts").GetValue(config, null));
        }
    }
}
