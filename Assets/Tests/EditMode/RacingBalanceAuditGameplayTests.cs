using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    // EditMode guardrails for the effective Racing fixture; full legal campaign outcomes are
    // measured separately by the real StreetGame/StreetMarket campaign runner.
    [Category("RacingAudit")]
    public sealed class RacingBalanceAuditGameplayTests
    {
        static readonly Type Runner = Type.GetType("GameplayBalanceAudit, Assembly-CSharp-Editor", true);
        static object Invoke(string method, params object[] args) => Runner.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        static object Field(object instance, string name) => instance.GetType().GetField(name).GetValue(instance);
        static object Effective()
        {
            object balance = Invoke("LoadEffective");
            Runner.GetField("effective", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, balance);
            return balance;
        }
        static object Plan(string name, int mode = 9)
        {
            object plan = Activator.CreateInstance(Runner.GetNestedType("Plan"));
            plan.GetType().GetField("name").SetValue(plan, name);
            plan.GetType().GetField("mode").SetValue(plan, mode);
            return plan;
        }

        [Test]
        public void RacingCandidatePoliciesExplicitlyStaffMozos()
        {
            var found = new System.Collections.Generic.HashSet<string>();
            foreach (object plan in (IEnumerable)Invoke("ExplorationPlans"))
            {
                string name = (string)Field(plan, "name");
                if (name != "balanced" && name != "adaptive_lag") continue;
                Assert.Greater((float)Field(plan, "mozo"), 0f, name + " must budget explicit Mozo capacity at Racing.");
                found.Add(name);
            }
            CollectionAssert.AreEquivalent(new[] { "balanced", "adaptive_lag" }, found);
        }

        [Test]
        public void RacingFreeMozoCanSellButCannotWinWithoutPaidUpgrades()
        {
            Effective();
            object run = Invoke("Isolated", 5, 101, Plan("no_purchases"), "current");
            CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1 }, (int[])Field(run, "team"));
            Assert.AreEqual("conditional_minimum_stock", Field(run, "mode"));
            Assert.AreEqual("Lost", Field(run, "phase"));
            int[] delivered = (int[])Field(run, "delivered");
            int[] produced = (int[])Field(run, "produced");
            Assert.Greater(produced[0] + produced[1], 0, "The no-purchase run must exercise real active cooking.");
            Assert.Greater(delivered[0] + delivered[1] + delivered[2] + delivered[3], 0,
                "The free starter Mozo must hand off real sandwiches without a paid hire.");
            Assert.AreEqual(250, Field(run, "initialCoins"));
            Assert.AreEqual((int)Field(run, "initialCoins") + (int)Field(run, "earned") - (int)Field(run, "spent"), Field(run, "cash"));
        }

        [Test]
        public void RacingPrestocksCanReachWonOnlyAfterRealPerProductDeliveries()
        {
            Effective();
            object plan = Plan("balanced", 0);
            foreach (FieldInfo field in plan.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (field.FieldType == typeof(float)) field.SetValue(plan, 1f);
            object run = Invoke("Isolated", 5, 1016, plan, "current");
            Assert.AreEqual("conditional_minimum_stock", Field(run, "mode"));
            Assert.AreEqual("Won", Field(run, "phase"));
            Assert.IsTrue((bool)Field(run, "won"));
            int[] goals = (int[])Field(run, "goals"), sold = (int[])Field(run, "delivered"), initial = (int[])Field(run, "initialTable"), deposited = (int[])Field(run, "deposited");
            for (int product = 0; product < 7; product++)
            {
                Assert.GreaterOrEqual(sold[product], goals[product]);
                Assert.LessOrEqual(sold[product], initial[product] + deposited[product], "Product " + product + " cannot be sold without table stock/deposit.");
            }
            float[] visual = (float[])Field(run, "roleVisual");
            Assert.AreEqual(0f, visual[0], "Normal active cooks cannot add visual-only sandwiches.");
            Assert.AreEqual(0f, visual[2], "Premium active cooks cannot add visual-only sandwiches.");
            Assert.AreEqual((int)Field(run, "initialCoins") + (int)Field(run, "earned") - (int)Field(run, "spent"), Field(run, "cash"));
        }

        [Test]
        public void TestedThirteenWorkerCapacityTeamWinsOnlyInExplicitTheoreticalFixture()
        {
            Effective();
            object run = Invoke("Capacity", 5, 501, new[] { 4, 2, 3, 1, 3 }, 9);
            Assert.AreEqual("capacity_preconfigured_unlimited_stock", Field(run, "mode"));
            Assert.IsTrue((bool)Field(run, "won"));
            CollectionAssert.AreEqual(new[] { 4, 2, 3, 1, 3 }, (int[])Field(run, "team"));
            Assert.AreEqual(9, Field(run, "speed"));
            int[] goals = (int[])Field(run, "goals"), sold = (int[])Field(run, "delivered");
            for (int product = 0; product < 7; product++) Assert.GreaterOrEqual(sold[product], goals[product]);
        }

        [Test]
        public void SevereOverstaffingFillsFoodTablesAndStopsFoodSales()
        {
            Effective();
            object run = Invoke("Capacity", 5, 501, new[] { 12, 6, 12, 6, 12 }, 9);
            Assert.AreEqual("capacity_preconfigured_unlimited_stock", Field(run, "mode"));
            Assert.IsFalse((bool)Field(run, "won"));
            int[] sold = (int[])Field(run, "delivered");
            int[] initial = (int[])Field(run, "initialTable");
            int[] deposited = (int[])Field(run, "deposited");
            float[] tableFull = (float[])Field(run, "tableFullSeconds");
            for (int product = 0; product < 4; product++)
            {
                Assert.LessOrEqual(sold[product], initial[product] + deposited[product],
                    "Overstaffing may sell legitimately prepared food, but cannot count unsupplied sales.");
                Assert.Greater(tableFull[product], 200f, "Food table " + product + " should expose the physical capacity bottleneck.");
            }
        }
    }
}
