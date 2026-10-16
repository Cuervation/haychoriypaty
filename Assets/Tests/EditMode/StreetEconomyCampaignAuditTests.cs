using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// One fresh, cash-only campaign through the production StreetGame lifecycle. It deliberately
    /// fails at the first unwinnable/underfunded transition rather than manufacturing a grant.
    /// </summary>
    public sealed class StreetEconomyCampaignAuditTests
    {
        private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        private string savedProgress;
        private bool hadProgress;
        private GameObject host;
        private object game;

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object value, string name)
        {
            PropertyInfo p = value.GetType().GetProperty(name, Public);
            if (p != null) return p.GetValue(value, null);
            FieldInfo f = value.GetType().GetField(name, AnyInstance);
            if (f != null) return f.GetValue(value);
            throw new MissingMemberException(value.GetType().FullName, name);
        }
        private static object Call(object value, string name, params object[] args)
        {
            foreach (MethodInfo m in value.GetType().GetMethods(Public))
            {
                if (m.Name != name || m.GetParameters().Length != args.Length) continue;
                ParameterInfo[] ps = m.GetParameters();
                bool match = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null && !ps[i].ParameterType.IsInstanceOfType(args[i])) { match = false; break; }
                if (match) return m.Invoke(value, args);
            }
            throw new MissingMethodException(value.GetType().FullName, name);
        }

        [SetUp]
        public void SetUp()
        {
            hadProgress = PlayerPrefs.HasKey(ProgressKey);
            savedProgress = hadProgress ? PlayerPrefs.GetString(ProgressKey) : null;
            PlayerPrefs.DeleteKey(ProgressKey); // Isolated Editor test key only; never touches a device save.
            game = CreateGame("Isolated fresh economy campaign");
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (hadProgress) PlayerPrefs.SetString(ProgressKey, savedProgress);
            else PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
        }

        [Explicit("Manual balance benchmark: current policies fail Level 10; this is not a regression guarantee or proof of impossibility.")]
        [TestCase(1.2f, TestName = "FreshCampaign_Mozo3_SpeedFactor1_2")]
        [TestCase(1.5f, TestName = "FreshCampaign_Mozo3_SpeedFactor1_5")]
        [TestCase(2.0f, TestName = "FreshCampaign_Mozo3_SpeedFactor2")]
        public void FreshSaveCampaignUsesEarnedCashAndRealHandoffsFromLevelOneToEleven(float speedValueMultiplier)
        {
            const float mozoWeight = 3f;
            TestContext.WriteLine("Adaptive marginal-load campaign policy; Mozo weight=3, speed valuation factor=" + speedValueMultiplier.ToString("F1"));
            Assert.AreEqual(0, Get(game, "UnlockedLevel"), "Campaign must start from a genuinely new save.");
            Assert.IsTrue((bool)Call(game, "TryStartRound"), "Fresh Level 1 starter kit must permit the first attempt.");
            var summaries = new List<string>();
            int expectedLevel = 0;
            bool reachedFinal = false;
            long cashBeforeRestock = (long)Get(Get(game, "Market"), "BalanceCents");
            long purchasesBeforeRestock = (long)Get(Get(Get(game, "Market"), "Data"), "cumulativePurchaseSpendCents");

            while (expectedLevel < 11)
            {
                object sim = Get(game, "Sim");
                object market = Get(game, "Market");
                Assert.AreEqual(expectedLevel, Get(sim, "LevelIndex"));
                Assert.IsTrue((bool)Get(market, "Data").GetType().GetField("attemptActive").GetValue(Get(market, "Data")));
                long purchasesBefore = (long)Get(Get(market, "Data"), "cumulativePurchaseSpendCents");
                long earnedBefore = (long)Get(market, "BalanceCents");
                int salesBefore = (int)Get(sim, "Delivered");
                int openingAttemptCoins = (int)Get(sim, "Coins");
                int upgradeSpend = 0, hireSpend = 0;

                for (int tick = 0; tick < 1600 && Get(sim, "Phase").ToString() == "Playing"; tick++)
                {
                    Call(sim, "Step", .25f);
                    if ((tick & 1) == 0) continue; // Player policy acts every 0.5 seconds.
                    var spend = ApplyAffordableFiveRolePolicy(game, sim, mozoWeight, speedValueMultiplier);
                    hireSpend += spend.Item1;
                    upgradeSpend += spend.Item2;
                }

                string phase = Get(sim, "Phase").ToString();
                int elapsedSales = (int)Get(sim, "Delivered") - salesBefore;
                float elapsed = (float)Get(sim, "Elapsed");
                Assert.AreEqual(openingAttemptCoins + (int)Get(sim, "CoinsEarned") - hireSpend - upgradeSpend, (int)Get(sim, "Coins"),
                    "Attempt-cash conservation for L" + (expectedLevel + 1) + ": opening coins + real sales - paid hires - speed upgrades = leftover.");
                Assert.IsTrue((bool)Call(game, "ProcessRoundResult"), "Terminal result must settle and persist via StreetGame.");
                market = Get(game, "Market");
                object data = Get(market, "Data");
                object result = Get(data, "lastResult");
                long purchaseSpend = (long)Get(data, "cumulativePurchaseSpendCents") - purchasesBefore;
                long restockSpend = (long)Get(data, "cumulativePurchaseSpendCents") - purchasesBeforeRestock;
                long balanceAfter = (long)Get(market, "BalanceCents");
                string row = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "L{0}: {1}, {2:F1}s, handoffs={3}, hires=${4:F2}, speed=${5:F2}, restock=${6:F2}, market ${7:F2} - ${8:F2} restock -> ${9:F2} before result -> ${10:F2} after result, gross=${11:F2}, recognized 50%=${12:F2}, leftover=${13:F2}, 2x leftover=${14:F2}, progression=${15:F2}, earned=${16:F2}",
                    expectedLevel + 1, phase, elapsed, elapsedSales, (float)hireSpend, (float)upgradeSpend,
                    restockSpend / 100f, cashBeforeRestock / 100f, restockSpend / 100f,
                    earnedBefore / 100f, balanceAfter / 100f,
                    Convert.ToInt64(Get(result, "grossSalesCents")) / 100f,
                    Convert.ToInt64(Get(result, "recognizedSalesCents")) / 100f,
                    Convert.ToInt64(Get(result, "leftoverCents")) / 100f,
                    Convert.ToInt64(Get(result, "bonusCents")) / 100f,
                    Convert.ToInt64(Get(result, "progressionBonusCents")) / 100f,
                    Convert.ToInt64(Get(result, "earnedCents")) / 100f);
                Assert.AreEqual(cashBeforeRestock - restockSpend + Convert.ToInt64(Get(result, "earnedCents")), balanceAfter,
                    "Mercado cash conservation for L" + (expectedLevel + 1) + ": opening money - actual ingredient purchases + earned rewards = closing money.");
                Assert.AreEqual(Convert.ToInt64(Get(result, "recognizedSalesCents")) + Convert.ToInt64(Get(result, "bonusCents")) + Convert.ToInt64(Get(result, "progressionBonusCents")),
                    Convert.ToInt64(Get(result, "earnedCents")), "All posted Mercado income must reconcile to its three disclosed sources.");
                Assert.AreEqual(purchaseSpend, 0L, "Ingredient restock happens before an attempt, not while the round is running.");
                summaries.Add(row);
                TestContext.WriteLine(row);
                TestContext.WriteLine("  team: parrilleros=" + Get(sim, "ParrilleroCount") + ", premium=" + Get(sim, "ParrilleroPremiumCount") +
                    ", cocacoleros=" + Get(sim, "CocacoleroCount") + ", ferneteros=" + Get(sim, "FerneteroCount") +
                    ", mozos=" + Get(sim, "MozoCount") + ", speed=" + Get(sim, "WorkRate"));

                if (phase != "Won")
                {
                    TestContext.WriteLine("Campaign stopped honestly: a loss does not unlock or skip this level.");
                    for (int product = 0; product < 7; product++)
                        if ((bool)Call(sim, "IsProductAvailable", product))
                            TestContext.WriteLine("  product " + product + ": sold=" + Call(sim, "GetProductDelivered", product) +
                                ", remaining=" + Call(sim, "GetProductRemaining", product));
                    Assert.Fail("Fresh sequential campaign did not win Level " + (expectedLevel + 1) + ".\n" + string.Join("\n", summaries));
                }
                if (expectedLevel == 10) { reachedFinal = true; break; }

                expectedLevel++;
                Assert.AreEqual(expectedLevel, Get(game, "UnlockedLevel"), "Only a real win unlocks the next level.");
                Assert.IsTrue((bool)Call(game, "NextLevel"), "A won level must transition through the production lifecycle.");
                // Reload the actual serialized StreetGame save between levels, as on app navigation/restart.
                UnityEngine.Object.DestroyImmediate(host);
                game = CreateGame("Reloaded campaign level " + (expectedLevel + 1));
                Assert.AreEqual(expectedLevel, Get(game, "UnlockedLevel"), "Reload must preserve the earned unlock before level selection.");
                Assert.IsTrue((bool)Call(game, "SelectLevel", expectedLevel), "Choose only the next actually unlocked level through production navigation.");
                Assert.AreEqual(expectedLevel, Get(game, "SelectedLevel"));
                sim = Get(game, "Sim");
                market = Get(game, "Market");
                cashBeforeRestock = (long)Get(market, "BalanceCents");
                purchasesBeforeRestock = (long)Get(Get(market, "Data"), "cumulativePurchaseSpendCents");
                int[] required = (int[])Call(market, "RequiredUnits", sim);
                long expectedRestockCost = (long)Call(market, "MinimumPurchaseCostCents", sim);
                for (int ingredient = 0; ingredient < required.Length; ingredient++)
                {
                    int missing = (int)Call(market, "MissingPacks", sim, ingredient);
                    if (missing == 0) continue;
                    // Buy exactly the real catalog's missing packs, never a grant or synthetic kit.
                    Assert.IsTrue((bool)Call(game, "TryBuyIngredient", ingredient, missing, (long)Get(market, "NextPurchaseId")),
                        "Unable to purchase exact missing stock for L" + (expectedLevel + 1) + ", ingredient " + ingredient + ". Cash=" + Get(market, "BalanceCents"));
                }
                Assert.AreEqual(expectedRestockCost, (long)Get(Get(market, "Data"), "cumulativePurchaseSpendCents") - purchasesBeforeRestock,
                    "The campaign must buy the exact minimum missing stock and no extra packs.");
                Assert.IsTrue((bool)Call(game, "TryStartRound"), "Fully paid and stocked next level must start.");
            }
            Assert.IsTrue(reachedFinal, "A completed campaign must include level 11.");
        }

        internal static Tuple<int, int> ApplyAffordableFiveRolePolicy(object game, object sim, float mozoWeight, float speedValueMultiplier)
        {
            Type roleType = Runtime("StreetWorkerRole");
            Type specialties = Runtime("StreetSpecialties");
            MethodInfo productionRole = specialties.GetMethod("GetProductionRole", BindingFlags.Public | BindingFlags.Static);
            MethodInfo deliveryRole = specialties.GetMethod("GetDeliveryRole", BindingFlags.Public | BindingFlags.Static);
            string[] roleNames = { "Parrillero", "Cocacolero", "ParrilleroPremium", "Fernetero", "Mozo" };
            object[] roles = Array.ConvertAll(roleNames, name => Enum.Parse(roleType, name));
            bool activeCooks = (bool)Get(sim, "UsesActiveCooks");
            var demand = new float[roles.Length];
            var tableLoad = new int[roles.Length];
            object kitchen = Get(sim, "Kitchen");

            for (int slot = 0; slot < (int)Get(sim, "ProductCount"); slot++)
            {
                int product = (int)Call(sim, "GetAvailableProduct", slot);
                int remaining = (int)Call(sim, "GetProductRemaining", product);
                object producer = productionRole.Invoke(null, new object[] { product });
                object delivery = deliveryRole.Invoke(null, new object[] { product, activeCooks });
                for (int i = 0; i < roles.Length; i++)
                {
                    if (roles[i].Equals(producer))
                    {
                        demand[i] += remaining;
                        tableLoad[i] += (int)Call(kitchen, "TableCount", product);
                    }
                    // Active sandwiches impose distinct cooking and Mozo handoff tasks. Other
                    // products share the same responsible role, so their demand is counted once.
                    if (!delivery.Equals(producer) && roles[i].Equals(delivery)) demand[i] += remaining;
                }
            }

            // Staff missing roles before marginal upgrades; save for the best weighted work/$.
            int missing = -1; float missingScore = -1f;
            for (int i = 0; i < roles.Length; i++)
            {
                object role = roles[i];
                if (!(bool)Call(sim, "HasRole", role) || (int)Call(sim, "WorkerCount", role) != 0 || demand[i] <= 0) continue;
                int cost = (int)Call(sim, "HireCostForRole", role);
                if (cost <= 0) continue;
                float score = demand[i] * (i == 4 ? mozoWeight : 1f) / cost;
                if (score > missingScore) { missing = i; missingScore = score; }
            }
            if (missing >= 0)
            {
                object role = roles[missing]; int cost = (int)Call(sim, "HireCostForRole", role);
                return (bool)Call(sim, "CanHireRole", role) && (bool)Call(game, "TryHire", role)
                    ? Tuple.Create(cost, 0) : Tuple.Create(0, 0);
            }

            // Compare extra-worker marginal workload reduction with the global +10% speed upgrade.
            // Restore the previously measured aggregate-table backpressure threshold for active cooks.
            int candidate = -1; float bestStaffScore = -1f;
            for (int i = 0; i < roles.Length; i++)
            {
                object role = roles[i]; int workers = (int)Call(sim, "WorkerCount", role);
                if (!(bool)Call(sim, "HasRole", role) || demand[i] <= 0) continue;
                if (activeCooks && (i == 0 || i == 2) && tableLoad[i] > 4) continue;
                int cost = (int)Call(sim, "HireCostForRole", role);
                if (cost <= 0) continue;
                float weight = i == 4 ? mozoWeight : 1f;
                float reduction = demand[i] * weight / (Math.Max(1, workers) * (workers + 1f));
                float score = reduction / cost;
                if (score > bestStaffScore) { candidate = i; bestStaffScore = score; }
            }

            float rate = (float)Get(sim, "WorkRate");
            object balance = sim.GetType().GetField("balance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sim);
            float speedIncrease = (float)Get(balance, "speedIncrease");
            float totalWeightedWork = 0f;
            for (int i = 0; i < roles.Length; i++)
            {
                int workers = (int)Call(sim, "WorkerCount", roles[i]);
                float weight = i == 4 ? mozoWeight : 1f;
                totalWeightedWork += demand[i] * weight / Math.Max(1, workers);
            }
            int speedCost = (int)Get(sim, "SpeedCost");
            float speedBenefit = totalWeightedWork * (1f / rate - 1f / (rate + speedIncrease));
            bool speedAvailable = (int)Get(sim, "SpeedLevel") < (int)Get(sim, "MaxSpeedLevel");
            // CanUpgradeSpeed also requires sufficient cash, so use the cap for planning and keep
            // the score positive while saving; only an actually capped level is unavailable.
            float speedScore = speedAvailable && speedCost > 0 ? speedBenefit * speedValueMultiplier / speedCost : -1f;

            if (candidate >= 0 && bestStaffScore > speedScore)
            {
                object role = roles[candidate]; int cost = (int)Call(sim, "HireCostForRole", role);
                if (!(bool)Call(sim, "CanHireRole", role)) return Tuple.Create(0, 0); // Save toward the top-value hire.
                return (bool)Call(game, "TryHire", role) ? Tuple.Create(cost, 0) : Tuple.Create(0, 0);
            }
            if ((bool)Get(sim, "CanUpgradeSpeed") && speedScore >= bestStaffScore &&
                (bool)Call(game, "TryUpgradeSpeed")) return Tuple.Create(0, speedCost);
            return Tuple.Create(0, 0);
        }

        private object CreateGame(string name)
        {
            host = new GameObject(name);
            Component component = host.AddComponent(Runtime("StreetGame"));
            component.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(component, null);
            DisablePresentationAudio(component);
            return component;
        }

        private static void DisablePresentationAudio(Component game)
        {
            Behaviour audioCue = game.GetComponent(Runtime("OrderDeliverySound")) as Behaviour;
            if (audioCue != null)
            {
                Call(audioCue, "Initialize", (object)null); // Explicitly detach; EditMode doesn't reliably dispatch OnDisable.
                audioCue.enabled = false; // This audit does not need audio playback or EditMode Awake.
            }
        }
    }
}
