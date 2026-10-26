using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// Full player-loop stress cases: purchase missing stock, enter a real StreetSimulation,
    /// let actual customers/orders/reservations/handoffs run, invest in staff/upgrades, settle,
    /// and replay losses without replenishing stock for free.
    /// </summary>
    public sealed class StreetCompletePlayerStrategyTests
    {
        private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly string[] PurchasePlans = { "forward_bulk", "reverse_bulk", "cheapest_bulk", "forward_single", "reverse_single" };
        private static readonly string[] StaffPlans = { "adaptive_bottleneck_D", "balanced_speed_C" };
        private static readonly int[] Cadences = { 1, 2, 4 };
        private static readonly int[] Seeds = { 17, 407, 1337, 2026, 8675309 };

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object value, string property) => value.GetType().GetProperty(property, Public).GetValue(value, null);
        private static object Call(object value, string name, params object[] args)
        {
            foreach (MethodInfo method in value.GetType().GetMethods(Public))
                if (method.Name == name && method.GetParameters().Length == args.Length) return method.Invoke(value, args);
            throw new MissingMethodException(value.GetType().FullName, name);
        }

        private static object NewSimulation(object balance, int level) => Activator.CreateInstance(Runtime("StreetSimulation"),
            new[] { balance, (object)level, 5f, 0, 1, 0 });
        private static object NewMarket(object data, object balance) => Activator.CreateInstance(Runtime("StreetMarket"), new[] { data, balance });

        public static IEnumerable<TestCaseData> CompletePlayerStrategies()
        {
            int profile = 0;
            for (int purchase = 0; purchase < PurchasePlans.Length; purchase++)
            for (int staff = 0; staff < StaffPlans.Length; staff++)
            for (int cadence = 0; cadence < Cadences.Length; cadence++, profile++)
            for (int seed = 0; seed < Seeds.Length; seed++)
            {
                int caseIndex = profile * Seeds.Length + seed;
                int level = caseIndex % 11;
                int carryPercent = (profile % 5) * 20;
                yield return new TestCaseData(level, purchase, staff, cadence, seed, carryPercent)
                    .SetName("L" + (level + 1).ToString("00") + "_" + PurchasePlans[purchase] + "_" + StaffPlans[staff] +
                        "_checkEvery" + Cadences[cadence] + "Ticks_seed" + Seeds[seed] + "_carry" + carryPercent + "pct");
            }
        }

        [TestCaseSource(nameof(CompletePlayerStrategies))]
        public void PlayerBuysHiresDeliversSettlesAndCanReplay(int level, int purchasePlan, int staffPlan, int cadence, int seedIndex, int carryPercent)
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            balance.GetType().GetField("randomSeed").SetValue(balance, Seeds[seedIndex]);
            object sim = NewSimulation(balance, level);
            object data = Activator.CreateInstance(Runtime("StreetMarketData"));
            int[] required = (int[])Call(NewMarket(data, balance), "RequiredUnits", sim);
            int[] stock = new int[10];
            int[] packUnits = (int[])Runtime("StreetIngredientCatalog").GetField("PackUnits").GetValue(null);
            for (int id = 0; id < stock.Length; id++)
            {
                int carried = required[id] * carryPercent / 100;
                stock[id] = carried / packUnits[id] * packUnits[id];
            }
            data.GetType().GetField("stockUnits").SetValue(data, stock);
            data.GetType().GetField("kitGranted").SetValue(data, true);
            data.GetType().GetField("initializedUnlockedLevel").SetValue(data, level);
            object market = NewMarket(data, balance);

            long exactRestock = (long)Call(market, "MinimumPurchaseCostCents", sim);
            Assert.Greater(exactRestock, 0L, "Each scenario must exercise real purchases, not a no-op market.");
            data.GetType().GetField("balanceCents").SetValue(data, exactRestock);
            market = NewMarket(data, balance);
            ExecutePurchases(market, sim, balance, level, purchasePlan);
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim), "All recipe stock must be bought before play.");
            Assert.AreEqual(0L, Get(market, "BalanceCents"), "Exact-cost shopping should debit the full wallet and never go below zero.");

            // Exercise saved-state roundtrip after shopping and before the game starts.
            object restoredData = JsonUtility.FromJson(JsonUtility.ToJson(Get(market, "Data")), Runtime("StreetMarketData"));
            market = NewMarket(restoredData, balance);
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim), "Purchased inventory must survive save/reload.");

            var fixture = new StreetSimulationTests();
            fixture.SetUp();
            MethodInfo playerActions = typeof(StreetSimulationTests).GetMethod("ApplyStrategyPurchases", Private);
            Assert.IsNotNull(playerActions, "Use the same in-game hire/upgrade tactics already exercised by the simulation suite.");

            int[] beforeRound = (int[])((int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data"))).Clone();
            object primary = NewSimulation(balance, level);
            Assert.IsTrue((bool)Call(market, "BeginAttempt", primary));
            Call(primary, "BindIngredientStock", market);
            Call(primary, "StartRound");
            string firstPolicy = staffPlan == 0 ? "D" : "C";
            RunLiveRound(primary, fixture, playerActions, firstPolicy, Cadences[cadence]);
            string firstPhase = Get(primary, "Phase").ToString();
            Assert.IsTrue(firstPhase == "Won" || firstPhase == "Lost", "A complete real round must reach a terminal result.");
            object firstResult = Call(market, "SettleAttempt", primary);
            Assert.IsNotNull(firstResult);
            Assert.IsFalse((bool)Get(market, "Data").GetType().GetField("attemptActive").GetValue(Get(market, "Data")));

            object finalSim = primary;
            int replayCount = 0;
            if (firstPhase == "Lost")
            {
                CollectionAssert.AreEqual(beforeRound, (int[])Get(market, "Data").GetType().GetField("stockUnits").GetValue(Get(market, "Data")),
                    "A loss must restore the exact purchased inventory before replay.");
                object retry = NewSimulation(balance, level);
                Assert.IsTrue((bool)Call(market, "BeginAttempt", retry), "A loss may replay using restored stock, without a free kit.");
                Call(retry, "BindIngredientStock", market);
                Call(retry, "StartRound");
                RunLiveRound(retry, fixture, playerActions, "D", 1);
                Assert.IsTrue(Get(retry, "Phase").ToString() == "Won" || Get(retry, "Phase").ToString() == "Lost");
                finalSim = retry;
                replayCount = 1;
                Call(market, "SettleAttempt", retry);
            }

            string finalPhase = Get(finalSim, "Phase").ToString();
            Assert.IsTrue(finalPhase == "Won" || finalPhase == "Lost");
            object settledData = Get(market, "Data");
            object lastResult = settledData.GetType().GetField("lastResult").GetValue(settledData);
            Assert.AreEqual(finalPhase == "Won", lastResult.GetType().GetField("won").GetValue(lastResult));
            if (finalPhase == "Won")
            {
                Assert.IsTrue((bool)Get(finalSim, "GoalReached"), "A victory must satisfy every product objective, not merely the aggregate.");
                int[] deliveryConsumption = ConsumptionForDeliveries(finalSim);
                CollectionAssert.AreEqual(deliveryConsumption, (int[])lastResult.GetType().GetField("consumedUnits").GetValue(lastResult));
                CollectionAssert.AreEqual((int[])settledData.GetType().GetField("stockUnits").GetValue(settledData),
                    (int[])lastResult.GetType().GetField("remainingUnits").GetValue(lastResult));
                Assert.Greater((int)Get(finalSim, "CoinsEarned"), 0, "Real deliveries must create sales income.");
                Assert.Greater((int)Get(finalSim, "StaffCount"), 1, "A winning strategy must actually invest in at least one worker.");
            }
            else
            {
                CollectionAssert.AreEqual(beforeRound, (int[])settledData.GetType().GetField("stockUnits").GetValue(settledData),
                    "Losses restore the purchased stock exactly; no kit is granted during replay.");
                Assert.IsFalse(((bool[])settledData.GetType().GetField("bonusPaidByLevel").GetValue(settledData))[level],
                    "A loss cannot mark the one-time victory bonus as paid.");
            }
            Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), 0L);
            long settledBalance = (long)Get(market, "BalanceCents");
            Call(market, "SettleAttempt", finalSim);
            Assert.AreEqual(settledBalance, Get(market, "BalanceCents"), "Settling the same attempt twice must never pay twice.");

            TestContext.WriteLine("PLAYER LOOP " + Get(market, "LastResult").GetType().GetField("level").GetValue(Get(market, "LastResult")) +
                " strategy=" + PurchasePlans[purchasePlan] + "/" + StaffPlans[staffPlan] + "/cadence" + Cadences[cadence] +
                "/seed" + Seeds[seedIndex] + " carry=" + carryPercent + "% phase=" + firstPhase + " retry=" + replayCount + " final=" + finalPhase +
                " goals=" + GoalSummary(finalSim) + " delivered=" + TotalDelivered(finalSim) + " sales=$" + Get(finalSim, "CoinsEarned") +
                " upgradesStaff=" + Get(finalSim, "StaffCount") + " marketBalance=$" + ((long)Get(market, "BalanceCents") / 100f).ToString("F2"));
        }

        [Test]
        public void LevelNumbersFollowNonLinearSelectionOrder()
        {
            int[] order = { 9, 2, 7, 0, 10, 3, 5, 1, 8, 4, 6, 9, 3, 10 };
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object sim = NewSimulation(balance, 0);
            foreach (int level in order)
            {
                Assert.IsTrue((bool)Call(sim, "SelectLevel", level), "Ready-state selection should permit the assigned unlocked level.");
                Assert.AreEqual(level, Get(sim, "LevelIndex"));
                int goalSum = 0;
                for (int product = 0; product < 7; product++)
                {
                    int target = (int)Call(sim, "GetProductGoal", product);
                    int remaining = (int)Call(sim, "GetProductRemaining", product);
                    Assert.GreaterOrEqual(target, 0);
                    Assert.AreEqual(target, remaining, "A newly selected level must show the full actual target for every product.");
                    goalSum += target;
                }
                Assert.AreEqual(goalSum, Get(sim, "Goal"), "Aggregate target must equal visible per-product objectives.");
                Assert.IsFalse((bool)Get(sim, "GoalReached"), "No selected fresh level can display a false completed objective.");
            }
            TestContext.WriteLine("Non-linear READY selection sequence validated: " + string.Join(" -> ", Array.ConvertAll(order, x => (x + 1).ToString("00"))));
        }

        private static void ExecutePurchases(object market, object sim, object balance, int unlockedLevel, int plan)
        {
            var ids = new List<int>();
            for (int id = 0; id < 10; id++) if ((int)Call(market, "MissingPacks", sim, id) > 0) ids.Add(id);
            if (plan == 1) ids.Reverse();
            if (plan == 2) ids.Sort((a, b) => ((int)Call(market, "PackPriceCents", a)).CompareTo((int)Call(market, "PackPriceCents", b)));
            long op = (long)Get(market, "NextPurchaseId");
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                int packs = (int)Call(market, "MissingPacks", sim, id);
                bool singlePack = plan >= 3;
                int count = singlePack ? packs : 1;
                int perTransaction = singlePack ? 1 : packs;
                for (int p = 0; p < count; p++)
                {
                    Assert.IsTrue((bool)Call(market, "TryBuy", sim, id, perTransaction, unlockedLevel, op++),
                        "A player purchase covered by the exact wallet must succeed. Ingredient=" + id);
                }
            }
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim));
        }

        private static void RunLiveRound(object sim, object fixture, MethodInfo actions, string staffPolicy, int cadenceTicks)
        {
            for (int tick = 0; tick < 1500 && Get(sim, "Phase").ToString() == "Playing"; tick++)
            {
                Call(sim, "Step", .25f);
                if ((tick + 1) % cadenceTicks == 0) actions.Invoke(fixture, new[] { sim, staffPolicy });
            }
        }

        private static int[] ConsumptionForDeliveries(object sim)
        {
            int[] consumed = new int[10];
            bool activeCooks = (bool)Get(sim, "UsesActiveCooks");
            int levelIndex = (int)Get(sim, "LevelIndex");
            for (int slot = 0; slot < (int)Get(sim, "ProductCount"); slot++)
            {
                int product = (int)Call(sim, "GetAvailableProduct", slot);
                if ((activeCooks && product < 4) || (levelIndex == 2 && product < 4) || product == 4 || product == 6)
                    continue; // Active/Vélez sandwiches and inexhaustible barrels bypass Market ingredients.
                int goal = (int)Call(sim, "GetProductGoal", product);
                int actual = (int)Call(sim, "GetProductDelivered", product);
                Assert.GreaterOrEqual(actual, goal, "A win completes every required quota; genuine bonus sales may exceed it.");
                MethodInfo recipe = Runtime("StreetIngredientCatalog").GetMethod("Recipe", BindingFlags.Public | BindingFlags.Static);
                foreach (int ingredient in (int[])recipe.Invoke(null, new object[] { product })) consumed[ingredient] += actual;
            }
            return consumed;
        }

        private static int TotalDelivered(object sim)
        {
            int total = 0;
            for (int slot = 0; slot < (int)Get(sim, "ProductCount"); slot++) total += (int)Call(sim, "GetProductDelivered", (int)Call(sim, "GetAvailableProduct", slot));
            return total;
        }

        private static string GoalSummary(object sim)
        {
            var values = new List<string>();
            for (int slot = 0; slot < (int)Get(sim, "ProductCount"); slot++)
            {
                int p = (int)Call(sim, "GetAvailableProduct", slot);
                values.Add(p + ":" + Call(sim, "GetProductDelivered", p) + "/" + Call(sim, "GetProductGoal", p));
            }
            return string.Join(",", values.ToArray());
        }
    }
}
