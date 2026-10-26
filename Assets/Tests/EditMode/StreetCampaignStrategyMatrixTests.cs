using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// Cash-only campaign stress matrix: 10 playable transitions × 5 wallet boundaries × 3 purchase tactics = 150 cases.
    /// Exercises the production market domain without changing gameplay code or player save data.
    /// </summary>
    public sealed class StreetCampaignStrategyMatrixTests
    {
        private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
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
        private static int[] Stock(object market)
        {
            object data = Get(market, "Data");
            return (int[])data.GetType().GetField("stockUnits").GetValue(data);
        }

        private static readonly string[] FundingNames = { "zero", "one-cheapest-pack-short", "one-cent-short", "exact", "one-pack-buffer" };
        private static readonly string[] TacticNames = { "catalog-forward-bulk", "catalog-reverse-bulk", "cheapest-pack-first" };

        public static IEnumerable<TestCaseData> RestockCases()
        {
            for (int level = 1; level < 11; level++)
                for (int funding = 0; funding < FundingNames.Length; funding++)
                    for (int tactic = 0; tactic < TacticNames.Length; tactic++)
                        yield return new TestCaseData(level, funding, tactic)
                            .SetName("L" + (level + 1).ToString("00") + "_" + FundingNames[funding] + "_" + TacticNames[tactic]);
        }

        [TestCaseSource(nameof(RestockCases))]
        public void RestockAndStockGateRemainCorrect(int level, int fundingMode, int tactic)
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object sim = NewSimulation(balance, level);
            object data = Activator.CreateInstance(Runtime("StreetMarketData"));
            int[] required = (int[])Call(NewMarket(data, balance), "RequiredUnits", sim);
            int[] seededStock = new int[10];
            for (int ingredient = 0; ingredient < seededStock.Length; ingredient++) seededStock[ingredient] = required[ingredient] / 2;
            data.GetType().GetField("stockUnits").SetValue(data, seededStock);
            data.GetType().GetField("kitGranted").SetValue(data, true);
            data.GetType().GetField("initializedUnlockedLevel").SetValue(data, level);

            object market = NewMarket(data, balance);
            long completionCost = (long)Call(market, "MinimumPurchaseCostCents", sim);
            Assert.Greater(completionCost, 0L, "Every transition from L2 through L11 must have a positive remaining-stock cost.");
            int cheapestPack = int.MaxValue;
            for (int id = 0; id < 10; id++)
                if ((int)Call(market, "MissingPacks", sim, id) > 0)
                    cheapestPack = Math.Min(cheapestPack, (int)Call(market, "PackPriceCents", id));
            Assert.Less(cheapestPack, int.MaxValue);

            long openingCash = fundingMode == 0 ? 0L :
                fundingMode == 1 ? completionCost - cheapestPack :
                fundingMode == 2 ? completionCost - 1L :
                fundingMode == 3 ? completionCost : completionCost + cheapestPack;
            data.GetType().GetField("balanceCents").SetValue(data, openingCash);
            List<int> order = new List<int>();
            for (int id = 0; id < 10; id++) if ((int)Call(market, "MissingPacks", sim, id) > 0) order.Add(id);
            if (tactic == 1) order.Reverse();
            if (tactic == 2) order.Sort((a, b) => ((int)Call(market, "PackPriceCents", a)).CompareTo((int)Call(market, "PackPriceCents", b)));

            bool rejectedTransactionObserved = false;
            long purchaseIdBefore = (long)Get(market, "NextPurchaseId");
            if (tactic < 2)
            {
                foreach (int id in order)
                {
                    int packs = (int)Call(market, "MissingPacks", sim, id);
                    if (packs > 0) AttemptPurchase(market, sim, level, id, packs, ref rejectedTransactionObserved);
                }
            }
            else
            {
                foreach (int id in order)
                {
                    int packs = (int)Call(market, "MissingPacks", sim, id);
                    for (int pack = 0; pack < packs; pack++)
                    {
                        if (!AttemptPurchase(market, sim, level, id, 1, ref rejectedTransactionObserved)) break;
                    }
                }
            }

            bool funded = openingCash >= completionCost;
            bool stocked = (bool)Call(market, "HasRequiredStock", sim);
            Assert.AreEqual(funded, stocked, "Funding threshold must match actual catalog/recipe requirements; " + Context(level, fundingMode, tactic));
            Assert.AreEqual(funded, (bool)Call(market, "BeginAttempt", sim), "Unstocked levels must remain blocked; fully funded levels must be startable. " + Context(level, fundingMode, tactic));
            Assert.GreaterOrEqual((long)Get(market, "BalanceCents"), 0L);
            if (!funded) Assert.IsTrue(rejectedTransactionObserved, "A cash-short path should exercise an atomic rejected purchase. " + Context(level, fundingMode, tactic));
            object currentData = Get(market, "Data");
            long purchaseCount = (long)Get(market, "NextPurchaseId") - purchaseIdBefore;
            Assert.AreEqual(purchaseIdBefore + purchaseCount, (long)Get(market, "NextPurchaseId"));
            Assert.AreEqual((long)currentData.GetType().GetField("lastPurchaseId").GetValue(currentData) + 1L,
                (long)Get(market, "NextPurchaseId"), "Only successful purchases advance the monotonic operation ID.");

            if (funded)
            {
                int[] snapshot = (int[])Stock(market).Clone();
                int product = level == 2 ? (int)Call(sim, "GetAvailableProduct", 2) : (int)Call(sim, "GetAvailableProduct", 0);
                int foodId = 100000 + level * 100 + fundingMode * 3 + tactic;
                Assert.IsTrue((bool)Call(market, "TryReserveFood", foodId, product));
                Assert.IsTrue((bool)Call(market, "ConsumeReservedFood", foodId, product));
                Call(market, "AbortAttempt");
                CollectionAssert.AreEqual(snapshot, Stock(market), "Abort must restore exact stock after a partial physical consume.");
            }
            else Assert.IsFalse((bool)Call(market, "BeginAttempt", sim));

            string json = JsonUtility.ToJson(Get(market, "Data"));
            object restored = JsonUtility.FromJson(json, Runtime("StreetMarketData"));
            object reloaded = NewMarket(restored, balance);
            Assert.AreEqual(Get(market, "BalanceCents"), Get(reloaded, "BalanceCents"), "Save/reload preserves available cash.");
            CollectionAssert.AreEqual(Stock(market), Stock(reloaded), "Save/reload preserves all ten ingredient stocks.");
        }

        private static bool AttemptPurchase(object market, object sim, int unlocked, int id, int packs, ref bool rejectedObserved)
        {
            long cashBefore = (long)Get(market, "BalanceCents");
            int stockBefore = (int)Call(market, "Units", id);
            long op = (long)Get(market, "NextPurchaseId");
            long price = (long)packs * (int)Call(market, "PackPriceCents", id);
            bool purchased = (bool)Call(market, "TryBuy", sim, id, packs, unlocked, op);
            if (purchased)
            {
                Assert.AreEqual(cashBefore - price, (long)Get(market, "BalanceCents"));
                int[] packUnits = (int[])Runtime("StreetIngredientCatalog").GetField("PackUnits").GetValue(null);
                Assert.AreEqual(stockBefore + packs * packUnits[id], Call(market, "Units", id));
            }
            else
            {
                rejectedObserved = true;
                Assert.Greater(price, cashBefore, "Transactions may be rejected only for the cash boundary exercised by this matrix.");
                Assert.AreEqual(cashBefore, Get(market, "BalanceCents"), "Rejected purchases must not debit cash.");
                Assert.AreEqual(stockBefore, Call(market, "Units", id), "Rejected purchases must not change inventory.");
            }
            return purchased;
        }

        private static string Context(int level, int funding, int tactic) =>
            "L" + (level + 1) + ", " + FundingNames[funding] + ", " + TacticNames[tactic];

        [Test]
        public void FreshLevelOneVictoryMustFundLevelTwoMinimumRestock()
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            object data = Activator.CreateInstance(Runtime("StreetMarketData"));
            object market = NewMarket(data, balance);
            Call(market, "InitializeNewGameKit");
            object sim = NewSimulation(balance, 0);
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim), "Fresh Level 1 should start from its opening kit.");
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            var fixture = new StreetSimulationTests();
            fixture.SetUp();
            MethodInfo adaptivePurchases = typeof(StreetSimulationTests).GetMethod("ApplyStrategyPurchases", Private);
            Assert.IsNotNull(adaptivePurchases);
            for (int tick = 0; tick < 1300 && Get(sim, "Phase").ToString() == "Playing"; tick++)
            {
                Call(sim, "Step", .25f);
                adaptivePurchases.Invoke(fixture, new[] { sim, "D" });
            }
            Assert.AreEqual("Won", Get(sim, "Phase").ToString(), "Use an actual simulation victory, not a mocked settlement.");
            Call(market, "SettleAttempt", sim);
            object nextLevel = NewSimulation(balance, 1);
            long missingStockCost = (long)Call(market, "MinimumPurchaseCostCents", nextLevel);
            long availableBalance = (long)Get(market, "BalanceCents");
            Assert.GreaterOrEqual(availableBalance, missingStockCost,
                "Fresh L1 win must fund L2 restock without a loan. Available cents=" + availableBalance + "; required cents=" + missingStockCost + ".");
        }
    }
}
