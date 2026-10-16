using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetMarketIntegrationTests
    {
        private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object value, string name) => value.GetType().GetProperty(name, Public).GetValue(value);
        private static object Call(object value, string name, params object[] args)
        {
            foreach (MethodInfo method in value.GetType().GetMethods(Public))
                if (method.Name == name && method.GetParameters().Length == args.Length) return method.Invoke(value, args);
            throw new MissingMethodException(name);
        }
        private static void Set(object value, string name, object data) => value.GetType()
            .GetField("<" + name + ">k__BackingField", Private).SetValue(value, data);
        private static object NewSim(object balance, int level) => Activator.CreateInstance(Runtime("StreetSimulation"), new[] { balance, (object)level, 5f, 0, 1, 0 });
        private static object NewMarket(object balance, object data = null) => Activator.CreateInstance(Runtime("StreetMarket"),
            new[] { data ?? Activator.CreateInstance(Runtime("StreetMarketData")), balance });

        [Test]
        public void KitchenReservesButOnlyValidPhysicalConsumeDebitsOnce()
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object sim = NewSim(balance, 0), market = NewMarket(balance);
            Call(market, "Initialize", 0); Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market); Call(sim, "StartRound");
            Assert.AreEqual(200, Call(market, "Units", 0), "Prepared mise-en-place is only reserved, not spent.");
            object kitchen = Get(sim, "Kitchen"), role = Enum.Parse(Runtime("StreetWorkerRole"), "Parrillero");
            object item = Call(kitchen, "TryTake", 0, role, 1);
            Assert.IsNotNull(item); int id = (int)Get(item, "Id");
            Assert.IsFalse((bool)Call(kitchen, "Consume", id, 0, 2));
            Assert.AreEqual(200, Call(market, "Units", 0));
            Assert.IsTrue((bool)Call(kitchen, "Consume", id, 0, 1));
            Assert.IsFalse((bool)Call(kitchen, "Consume", id, 0, 1));
            Assert.AreEqual(199, Call(market, "Units", 0)); Assert.AreEqual(199, Call(market, "Units", 1));
            Call(market, "AbortAttempt");
            Assert.AreEqual(200, Call(market, "Units", 0)); Assert.AreEqual(200, Call(market, "Units", 1));
        }

        [Test]
        public void ActualHandoffsAndAdaptivePurchasesFundAllElevenFirstUnlockTransitions()
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object market = NewMarket(balance); Call(market, "Initialize", 0);
            var strategy = new StreetSimulationTests(); strategy.SetUp();
            MethodInfo purchase = typeof(StreetSimulationTests).GetMethod("ApplyStrategyPurchases", Private);
            int completed = 0;
            for (int level = 0; level < 11; level++)
            {
                object sim = NewSim(balance, level);
                long needed = (long)Call(market, "MinimumPurchaseCostCents", sim);
                long available = (long)Get(market, "BalanceCents");
                Assert.GreaterOrEqual(available, needed, "First-unlock reward must fund exact missing stock for level " + (level + 1));
                for (int ingredient = 0; ingredient < 10; ingredient++)
                {
                    int missing = (int)Call(market, "MissingPacks", sim, ingredient);
                    if (missing > 0) Assert.IsTrue((bool)Call(market, "TryBuy", sim, ingredient, missing, level,
                        (long)Get(market, "NextPurchaseId")), "Cash-only purchase L" + (level + 1) + " ingredient " + ingredient);
                }
                int[] before = (int[])((int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data"))).Clone();
                Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
                Call(sim, "BindIngredientStock", market); Call(sim, "StartRound");
                for (int tick = 0; tick < 1300 && Get(sim, "Phase").ToString() == "Playing"; tick++)
                {
                    Call(sim, "Step", .25f); purchase.Invoke(strategy, new[] { sim, "D" });
                }
                int[] consumed = new int[10];
                for (int product = 0; product < 7; product++)
                {
                    int delivered = (int)Call(sim, "GetProductDelivered", product);
                    int[] recipe = (int[])Runtime("StreetIngredientCatalog").GetMethod("Recipe").Invoke(null, new object[] { product });
                    foreach (int ingredient in recipe) consumed[ingredient] += delivered;
                }
                for (int i = 0; i < 10; i++) Assert.AreEqual(before[i] - consumed[i], Call(market, "Units", i), "Real consumption L" + (level + 1) + " ingredient " + i);
                TestContext.WriteLine("L" + (level + 1) + ": " + Get(sim, "Phase") + ", t=" + Get(sim, "Elapsed") + ", sales=" + Get(sim, "CoinsEarned"));
                Assert.AreEqual("Won", Get(sim, "Phase").ToString(), "Cash-stocked actual level " + (level + 1));
                Assert.IsNotNull(Call(market, "SettleAttempt", sim));
                Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), 0L);
                if (level + 1 < 11)
                {
                    object next = NewSim(balance, level + 1);
                    long nextCost = (long)Call(market, "MinimumPurchaseCostCents", next);
                    long priorCash = (long)Get(market, "BalanceCents");
                    long bonus = (long)Call(market, "GrantFirstUnlockStockBonus", next, true);
                    Assert.AreEqual(Math.Max(0L, nextCost - priorCash), bonus, "Only the exact next-level stock shortfall is granted.");
                    Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), nextCost);
                    TestContext.WriteLine("Transition to L" + (level + 2) + ": stock=" + (nextCost / 100f).ToString("F2") +
                        ", reward=" + (bonus / 100f).ToString("F2") + ", wallet=" + ((long)Get(market, "BalanceCents") / 100f).ToString("F2"));
                }
                object persisted = JsonUtility.FromJson(JsonUtility.ToJson(Get(market, "Data")), Runtime("StreetMarketData"));
                market = NewMarket(balance, persisted); completed++;
            }
            Assert.AreEqual(11, completed, "The cash-only player campaign must reach and complete every level.");
        }

        [Test]
        public void ExistingLevelTwoSaveRecoversOnlyItsUnpaidMinimumStockTransitionCost()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                string migrated = "{\"version\":4,\"unlockedLevel\":1,\"price\":5,\"market\":{" +
                    "\"balanceCents\":0,\"cumulativePurchaseSpendCents\":0," +
                    "\"stockUnits\":[0,235,0,0,0,0,85,0,0,0]," +
                    "\"highestRecognizedSalesByLevel\":[50000],\"bonusPaidByLevel\":[true]," +
                    "\"lastResult\":{" +
                    "\"won\":true,\"level\":0,\"halfSalesCents\":50000," +
                    "\"recognizedSalesCents\":0,\"bonusCents\":0,\"earnedCents\":0,\"totalBalanceCents\":0}," +
                    "\"kitGranted\":true,\"initializedUnlockedLevel\":1}}";
                PlayerPrefs.SetString(key, migrated); PlayerPrefs.Save();
                root = new GameObject("Legacy Level 2 reward repair test"); Component game = root.AddComponent(gameType);
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                object market = Get(game, "Market");
                Assert.AreEqual(58750L, Get(market, "BalanceCents"));
                Assert.AreEqual(0, Call(market, "Units", 0)); Assert.AreEqual(235, Call(market, "Units", 1)); Assert.AreEqual(85, Call(market, "Units", 6));
                Assert.IsTrue(PlayerPrefs.GetString(key).Contains("legacyTransitionRepairApplied\":true"));

                // Reopening the save must keep the one-time amount and never grant it again.
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                market = Get(game, "Market"); Assert.AreEqual(58750L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(game, "SelectLevel", 1));
                Assert.AreEqual(58750L, Call(market, "MinimumPurchaseCostCents", Get(game, "Sim")));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 0, 200, 1L));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 6, 115, 2L));
                Assert.AreEqual(0L, Get(Get(game, "Market"), "BalanceCents"));
                Assert.IsTrue((bool)Call(Get(game, "Market"), "HasRequiredStock", Get(game, "Sim")));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void WatermarkedLevelOneWinRepairsUnpaidBalanceBeforeSameSessionLevelTwoMarket()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                // A migrated save can already have Level 1's sales watermark and one-time bonus
                // marked paid. Completing it in the same app session suppresses normal earnings.
                string migrated = "{\"version\":4,\"unlockedLevel\":0,\"price\":5,\"market\":{" +
                    "\"balanceCents\":0,\"cumulativePurchaseSpendCents\":0," +
                    "\"stockUnits\":[200,200,0,0,0,0,85,0,0,0]," +
                    "\"highestRecognizedSalesByLevel\":[50000],\"bonusPaidByLevel\":[true]," +
                    "\"kitGranted\":true,\"initializedUnlockedLevel\":0}}";
                PlayerPrefs.SetString(key, migrated); PlayerPrefs.Save();
                root = new GameObject("Same-session unpaid Level 1 transition repair test");
                Component game = root.AddComponent(gameType); gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                Assert.IsTrue((bool)Call(game, "TryStartRound"));

                object sim = Get(game, "Sim"), market = Get(game, "Market");
                object data = Get(market, "Data");
                int[] stock = (int[])data.GetType().GetField("stockUnits").GetValue(data);
                stock[0] = 0; stock[1] = 0; // Consumed by the completed Level 1 match; retain 85 Coca.
                Set(sim, "CoinsEarned", 1000);
                Set(sim, "Coins", 0);
                Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Won"));

                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                market = Get(game, "Market");
                Assert.AreEqual(1, Get(game, "UnlockedLevel"));
                Assert.AreEqual(78750L, Get(market, "BalanceCents"), "Exact remaining Level 2 stock cost must be available before Mercado is shown.");
                data = Get(market, "Data");
                Assert.IsTrue((bool)data.GetType().GetField("legacyTransitionRepairApplied").GetValue(data));

                // Reload and navigate as the game does; the transition amount must persist once.
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                market = Get(game, "Market"); Assert.AreEqual(78750L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(game, "SelectLevel", 1));
                Assert.AreEqual(78750L, Call(market, "MinimumPurchaseCostCents", Get(game, "Sim")));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 0, 200, 1L));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 1, 200, 2L));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 6, 115, 3L));
                market = Get(game, "Market");
                Assert.AreEqual(0L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(market, "HasRequiredStock", Get(game, "Sim")));
                Assert.IsTrue((bool)Get(game, "CanStartRound"));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void LegacyZeroPayoutSaveWithPriorPurchasesGetsOneIdempotentRestockRecovery()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                string migrated = "{\"version\":4,\"unlockedLevel\":10,\"price\":5,\"market\":{" +
                    "\"balanceCents\":0,\"cumulativePurchaseSpendCents\":78750," +
                    "\"stockUnits\":[0,235,0,0,0,0,85,0,0,0]," +
                    "\"highestRecognizedSalesByLevel\":[50000],\"bonusPaidByLevel\":[true]," +
                    "\"lastResult\":{" +
                    "\"won\":true,\"level\":0,\"grossSalesCents\":100000,\"halfSalesCents\":50000," +
                    "\"recognizedSalesCents\":0,\"bonusCents\":0,\"earnedCents\":0,\"totalBalanceCents\":0}," +
                    "\"kitGranted\":true,\"initializedUnlockedLevel\":10,\"legacyTransitionRepairApplied\":true}}";
                PlayerPrefs.SetString(key, migrated); PlayerPrefs.Save();
                root = new GameObject("Legacy zero-payout balance recovery test");
                Component game = root.AddComponent(gameType); gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                object market = Get(game, "Market"), data = Get(market, "Data"), result = Get(market, "LastResult");
                Assert.AreEqual(58750L, Get(market, "BalanceCents"), "Only the actual Level 2 stock shortage is restored.");
                Assert.AreEqual(78750L, data.GetType().GetField("cumulativePurchaseSpendCents").GetValue(data));
                Assert.AreEqual(0, Call(market, "Units", 0)); Assert.AreEqual(235, Call(market, "Units", 1)); Assert.AreEqual(85, Call(market, "Units", 6));
                Assert.IsTrue((bool)data.GetType().GetField("legacyZeroBalanceRecoveryApplied").GetValue(data));
                Assert.AreEqual(58750L, result.GetType().GetField("progressionBonusCents").GetValue(result));

                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                market = Get(game, "Market"); Assert.AreEqual(58750L, Get(market, "BalanceCents"), "Reload must not pay recovery twice.");
                Assert.IsTrue((bool)Call(game, "SelectLevel", 1));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 0, 200, 1L));
                Assert.IsTrue((bool)Call(game, "TryBuyIngredient", 6, 115, 2L));
                market = Get(game, "Market");
                Assert.AreEqual(0L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(market, "HasRequiredStock", Get(game, "Sim")));
                Assert.IsTrue((bool)Get(game, "CanStartRound"));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void FirstUnlockRestockRewardDoesNotPayAgainOnReplay()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
                root = new GameObject("First-unlock reward and replay test");
                Component game = root.AddComponent(gameType); gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                object sim = Get(game, "Sim"), market = Get(game, "Market"), balance = Get(game, "Balance");
                int[] stock = (int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data"));
                stock[0] = 0; stock[1] = 0;
                Set(sim, "CoinsEarned", 1000); Set(sim, "Coins", 0);
                Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Won"));
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                market = Get(game, "Market");
                object next = NewSim(balance, 1);
                long nextCost = (long)Call(market, "MinimumPurchaseCostCents", next);
                Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), nextCost);
                long firstUnlockBalance = (long)Get(market, "BalanceCents");
                Assert.Greater((long)Get(market, "LastResult").GetType().GetField("progressionBonusCents").GetValue(Get(market, "LastResult")), 0L);

                Assert.IsTrue((bool)Call(game, "SelectLevel", 0));
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                sim = Get(game, "Sim"); market = Get(game, "Market");
                stock = (int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data"));
                stock[0] = 0; stock[1] = 0;
                Set(sim, "CoinsEarned", 1000); Set(sim, "Coins", 0);
                Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Won"));
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                Assert.AreEqual(firstUnlockBalance, Get(Get(game, "Market"), "BalanceCents"), "Replaying an already unlocked level cannot claim the progression reward again.");
                Assert.AreEqual(0L, Get(Get(game, "Market"), "LastResult").GetType().GetField("progressionBonusCents").GetValue(Get(Get(game, "Market"), "LastResult")));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void VersionFourSaveIgnoresFormerDebtFieldAndWritesCashOnly()
        {
            Type gameType = Runtime("StreetGame");
            string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                string legacy = "{\"version\":4,\"unlockedLevel\":0,\"price\":5,\"market\":{\"balanceCents\":1234,\"debtCents\":987654,\"stockUnits\":[200,200,0,0,0,0,0,0,0,0],\"kitGranted\":true,\"initializedUnlockedLevel\":0}}";
                PlayerPrefs.SetString(key, legacy); PlayerPrefs.Save();
                root = new GameObject("Legacy debt save test");
                Component game = root.AddComponent(gameType);
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                object market = Get(game, "Market");
                Assert.AreEqual(1234L, Get(market, "BalanceCents"));
                Assert.IsFalse(PlayerPrefs.GetString(key).Contains("debtCents"), "Saving should drop the legacy-only field.");
                Assert.AreEqual(1234L, Get(Get(game, "Market"), "BalanceCents"));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void ExistingPersistedInventoryIsNeverReplacedByTheNewGameKit()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                // Observed quantities are persisted inventory, even if an older save lacks the kit marker.
                PlayerPrefs.SetString(key, "{\"version\":4,\"unlockedLevel\":0,\"price\":5,\"market\":{\"stockUnits\":[145,435,0,0,0,0,0,0,0,0],\"kitGranted\":false,\"initializedUnlockedLevel\":-1}}");
                PlayerPrefs.Save(); root = new GameObject("Existing stock preservation test");
                Component game = root.AddComponent(gameType); gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                object market = Get(game, "Market");
                Assert.AreEqual(145, Call(market, "Units", 0)); Assert.AreEqual(435, Call(market, "Units", 1));
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                market = Get(game, "Market");
                Assert.AreEqual(145, Call(market, "Units", 0), "Reopening must preserve saved stock.");
                Assert.AreEqual(435, Call(market, "Units", 1), "Reopening must not add the new-game kit.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void LevelOneStartGrantsOnlyMissingStockBeforeSnapshotAndKeepsItOnLoss()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                PlayerPrefs.SetString(key, "{\"version\":4,\"unlockedLevel\":0,\"price\":5,\"market\":{\"stockUnits\":[145,435,0,0,0,0,0,0,0,0],\"kitGranted\":true,\"initializedUnlockedLevel\":0}}");
                PlayerPrefs.Save(); root = new GameObject("Level one starter stock floor test");
                Component game = root.AddComponent(gameType); gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                object market = Get(game, "Market");
                Assert.AreEqual(145, Call(market, "Units", 0), "Loading alone must not mutate persisted inventory.");
                Assert.AreEqual(435, Call(market, "Units", 1));
                Assert.IsTrue((bool)Get(game, "CanStartRound"), "The level-one button should allow its safe top-up.");
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                market = Get(game, "Market"); Assert.AreEqual(200, Call(market, "Units", 0)); Assert.AreEqual(435, Call(market, "Units", 1));
                int[] snapshot = (int[])market.GetType().GetProperty("Data", Public).GetValue(market).GetType().GetField("attemptStock").GetValue(market.GetType().GetProperty("Data", Public).GetValue(market));
                Assert.AreEqual(200, snapshot[0]); Assert.AreEqual(435, snapshot[1]);
                object sim = Get(game, "Sim"); Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Lost"));
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                Assert.AreEqual(200, Call(Get(game, "Market"), "Units", 0)); Assert.AreEqual(435, Call(Get(game, "Market"), "Units", 1));
                Assert.IsTrue((bool)Call(game, "SelectLevel", 0));
                market = Get(game, "Market"); int[] current = (int[])market.GetType().GetProperty("Data", Public).GetValue(market).GetType().GetField("stockUnits").GetValue(market.GetType().GetProperty("Data", Public).GetValue(market));
                current[0] = 199; current[1] = 100; gameType.GetMethod("Save", Private).Invoke(game, null);
                Assert.IsTrue((bool)Call(game, "TryStartRound"), "A partial shortage must be granted before the next first-level attempt.");
                Assert.AreEqual(200, Call(Get(game, "Market"), "Units", 0)); Assert.AreEqual(200, Call(Get(game, "Market"), "Units", 1));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void LifecyclePersistsKitRecoversAbandonmentAndSettlesLossOnce()
        {
            Type gameType = Runtime("StreetGame"); string key = (string)gameType.GetField("ProgressKey").GetValue(null);
            bool had = PlayerPrefs.HasKey(key); string prior = had ? PlayerPrefs.GetString(key) : null;
            GameObject root = null;
            try
            {
                PlayerPrefs.DeleteKey(key);
                root = new GameObject("Market lifecycle temporary test"); Component game = root.AddComponent(gameType);
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                Component sound = root.GetComponent(Runtime("OrderDeliverySound"));
                if (sound != null) sound.GetType().GetMethod("OnDisable", Private).Invoke(sound, null); // No audible/AudioSource lifecycle in this EditMode fixture.
                object market = Get(game, "Market"); Assert.AreEqual(200, Call(market, "Units", 0)); Assert.AreEqual(200, Call(market, "Units", 1));
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                object sim = Get(game, "Sim"); for (int i = 0; i < 60; i++) Call(sim, "Step", .25f);
                Assert.Greater((int)Get(sim, "Delivered"), 0);
                Assert.AreEqual(200 - (int)Get(sim, "Delivered"), Call(market, "Units", 0));
                gameType.GetMethod("OnApplicationPause", Private).Invoke(game, new object[] { true });
                gameType.GetMethod("OnEnable", Private).Invoke(game, null); // Simulated process recovery of an active snapshot.
                if (sound != null) sound.GetType().GetMethod("OnDisable", Private).Invoke(sound, null);
                market = Get(game, "Market"); Assert.AreEqual(200, Call(market, "Units", 0)); Assert.AreEqual(200, Call(market, "Units", 1));
                Assert.AreEqual(0L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(game, "TryStartRound")); sim = Get(game, "Sim");
                for (int i = 0; i < 60; i++) Call(sim, "Step", .25f);
                int gross = (int)Get(sim, "CoinsEarned"); Assert.Greater(gross, 0);
                Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Lost"));
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                Assert.AreEqual((long)gross * 50L, Get(market, "BalanceCents")); Assert.AreEqual(200, Call(market, "Units", 0)); Assert.AreEqual(200, Call(market, "Units", 1));
                Assert.IsFalse((bool)Call(game, "ProcessRoundResult")); Assert.IsFalse((bool)Call(game, "Retry"));
                Assert.AreEqual(0, Get(game, "UnlockedLevel")); Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                Assert.AreEqual("Ready", Get(Get(game, "Sim"), "Phase").ToString());
                ((int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data")))[0] = 0;
                gameType.GetMethod("Save", Private).Invoke(game, null);
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                Assert.AreEqual(0, Call(Get(game, "Market"), "Units", 0), "Loading a save alone must not top up stock.");
                Assert.IsTrue((bool)Get(game, "CanStartRound"));
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                Assert.AreEqual(200, Call(Get(game, "Market"), "Units", 0), "The missing starter amount is granted on level entry.");
                Assert.AreEqual(200, Call(Get(game, "Market"), "Units", 1));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (had) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
