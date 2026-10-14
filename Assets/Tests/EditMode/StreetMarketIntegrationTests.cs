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
        public void ActualHandoffsAndAdaptivePurchasesNeverSpendBeyondBalance()
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
                if (needed > available)
                {
                    bool checkedReject = false;
                    while (!(bool)Call(market, "HasRequiredStock", sim) && !checkedReject)
                    {
                        bool purchased = false;
                        for (int ingredient = 0; ingredient < 10; ingredient++)
                        {
                            int packs = (int)Call(market, "MissingPacks", sim, ingredient);
                            if (packs <= 0) continue;
                            long itemCost = (long)packs * (int)Call(market, "PackPriceCents", ingredient);
                            long currentCash = (long)Get(market, "BalanceCents");
                            long operation = (long)Get(market, "NextPurchaseId");
                            int stockBefore = (int)Call(market, "Units", ingredient);
                            if (itemCost > currentCash)
                            {
                                Assert.IsFalse((bool)Call(market, "TryBuy", sim, ingredient, packs, level, operation));
                                Assert.AreEqual(currentCash, Get(market, "BalanceCents"));
                                Assert.AreEqual(stockBefore, Call(market, "Units", ingredient));
                                checkedReject = true;
                                break;
                            }
                            Assert.IsTrue((bool)Call(market, "TryBuy", sim, ingredient, packs, level, operation));
                            Assert.LessOrEqual((long)Get(market, "BalanceCents"), currentCash);
                            Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), 0L);
                            purchased = true;
                            break;
                        }
                        if (!purchased && !checkedReject) break;
                    }
                    Assert.IsTrue(checkedReject, "The first pack beyond the remaining balance must be rejected.");
                    Assert.IsFalse((bool)Call(market, "BeginAttempt", sim), "Unstocked L" + (level + 1) + " must remain gated.");
                    TestContext.WriteLine("ECONOMIC BLOCK: level " + (level + 1) + " needs $" + (needed / 100L) + ", available at entry $" + (available / 100L) + ". Cash-only purchases stop at $" + ((long)Get(market, "BalanceCents") / 100L) + " and the match remains gated.");
                    break;
                }
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
                object persisted = JsonUtility.FromJson(JsonUtility.ToJson(Get(market, "Data")), Runtime("StreetMarketData"));
                market = NewMarket(balance, persisted); completed++;
            }
            Assert.GreaterOrEqual(completed, 1, "At least the starting level must complete from its one-time stock kit.");
            TestContext.WriteLine("Cash-funded levels completed: " + completed + "/11");
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
                object market = Get(game, "Market"); Assert.AreEqual(200, Call(market, "Units", 0));
                Assert.IsTrue((bool)Call(game, "TryStartRound"));
                object sim = Get(game, "Sim"); for (int i = 0; i < 60; i++) Call(sim, "Step", .25f);
                Assert.Greater((int)Get(sim, "Delivered"), 0);
                Assert.AreEqual(200 - (int)Get(sim, "Delivered"), Call(market, "Units", 0));
                gameType.GetMethod("OnApplicationPause", Private).Invoke(game, new object[] { true });
                gameType.GetMethod("OnEnable", Private).Invoke(game, null); // Simulated process recovery of an active snapshot.
                if (sound != null) sound.GetType().GetMethod("OnDisable", Private).Invoke(sound, null);
                market = Get(game, "Market"); Assert.AreEqual(200, Call(market, "Units", 0));
                Assert.AreEqual(0L, Get(market, "BalanceCents"));
                Assert.IsTrue((bool)Call(game, "TryStartRound")); sim = Get(game, "Sim");
                for (int i = 0; i < 60; i++) Call(sim, "Step", .25f);
                int gross = (int)Get(sim, "CoinsEarned"); Assert.Greater(gross, 0);
                Set(sim, "Phase", Enum.Parse(Runtime("RoundPhase"), "Lost"));
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
                Assert.AreEqual((long)gross * 50L, Get(market, "BalanceCents")); Assert.AreEqual(200, Call(market, "Units", 0));
                Assert.IsFalse((bool)Call(game, "ProcessRoundResult")); Assert.IsFalse((bool)Call(game, "Retry"));
                Assert.AreEqual(0, Get(game, "UnlockedLevel")); Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                Assert.AreEqual("Ready", Get(Get(game, "Sim"), "Phase").ToString());
                ((int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data")))[0] = 0;
                gameType.GetMethod("Save", Private).Invoke(game, null);
                gameType.GetMethod("OnEnable", Private).Invoke(game, null);
                Assert.AreEqual(0, Call(Get(game, "Market"), "Units", 0), "Reload must not repeat the kit.");
                Assert.IsFalse((bool)Get(game, "CanStartRound"));
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
