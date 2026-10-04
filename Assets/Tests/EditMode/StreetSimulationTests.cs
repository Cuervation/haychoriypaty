using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Runtime remains in Assembly-CSharp; the test asmdef intentionally binds by reflection.
    public sealed class StreetSimulationTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public;
        private Type simType, balanceType;

        [SetUp]
        public void SetUp()
        {
            simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
        }

        private object Make(int coins = 0, int staff = 1, int level = 0, object balance = null)
        {
            if (balance == null) balance = Activator.CreateInstance(balanceType);
            return Activator.CreateInstance(simType, new object[] { balance, level, 5f, coins, staff, 0 });
        }
        private static object Get(object instance, string property) { return instance.GetType().GetProperty(property, Instance).GetValue(instance, null); }
        private static object Call(object instance, string method, params object[] args) { return instance.GetType().GetMethod(method, Instance).Invoke(instance, args); }
        private static void Start(object sim) { Call(sim, "StartRound"); }
        private static void Step(object sim, float seconds) { Call(sim, "Step", seconds); }
        private static bool Spawn(object sim, int product, int quantity) { return (bool)Call(sim, "SpawnCustomer", product, quantity); }
        private object NewBalance() { return Activator.CreateInstance(balanceType); }
        private void Tune(object balance, string field, object value) { balanceType.GetField(field, Instance).SetValue(balance, value); }
        private IList Customers(object sim) { return (IList)Get(sim, "Customers"); }

        [Test]
        public void StartsPausedAndWorkerOnlyDeliversAfterTravel()
        {
            object sim = Make();
            Spawn(sim, 0, 2); Start(sim); Step(sim, 0.2f);
            Assert.AreEqual(0, Get(sim, "Delivered"));
            Spawn(sim, 0, 2); Step(sim, 5f);
            Assert.Greater((int)Get(sim, "Delivered"), 0);
        }

        [Test]
        public void LargeOrdersAreServedOneUnitAtATimeWithoutOverserving()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1);
            object sim = Make(level: 1, balance: balance); Start(sim); Assert.IsTrue(Spawn(sim, 0, 999));
            for (int i = 0; i < 200; i++) Step(sim, 0.1f);
            Assert.LessOrEqual((int)Get(sim, "Delivered"), 999);
            IList customers = Customers(sim);
            Assert.AreEqual(999 - (int)Get(sim, "Delivered"), customers.Count == 0 ? 0 : (int)Get(customers[0], "Remaining"));
        }

        [Test]
        public void MultipleWorkersNeverReserveMoreUnitsThanRemain()
        {
            object balance = NewBalance(); Tune(balance,"levelGoals",new[]{1,1,1,1,1}); Tune(balance, "maxCustomers", 1);
            object sim = Make(staff: 8, balance: balance); Start(sim); Spawn(sim, 0, 1);
            object customer = Customers(sim)[0]; Step(sim, 8f);
            // The full turn may serve later arrivals; verify this original one-unit order.
            Assert.AreEqual(0, Get(customer, "Remaining"));
            Assert.AreEqual(0, Get(customer, "Reserved"));
        }

        [Test]
        public void ExpiredCustomerCancelsReservationsBeforeHandoff()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "customerPatienceSeconds", 0.25f);
            object sim = Make(balance: balance); Start(sim); Spawn(sim, 0, 3); object expired=Customers(sim)[0]; Step(sim, 5f);
            Assert.AreEqual(0, Get(sim, "Delivered"));
            Assert.AreEqual(0, Get(expired, "Reserved"));
        }

        [Test]
        public void ZeroPriceCompletesServiceButEarnsZeroCoins()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "levelProductCounts", new[] { 1, 1, 5, 6, 7 });
            object sim = Make(level: 1, balance: balance); Call(sim, "SetPrice", 0f); Start(sim); Spawn(sim, 0, 1); Step(sim, 8f);
            Assert.Greater((int)Get(sim, "Delivered"), 0); Assert.AreEqual(0, Get(sim, "Coins"));
        }

        [Test]
        public void RoundRobinPreventsMassOrderFromStarvingNextProduct()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 2);
            object sim = Make(level: 1, balance: balance); Start(sim);
            Spawn(sim, 0, 999); Spawn(sim, 1, 1);
            object secondProduct = Customers(sim)[1];
            Step(sim, 8f);
            Assert.AreEqual(0, Get(secondProduct, "Remaining"));
        }

        [Test]
        public void TwentyOneCrowdUsesDistinctReusableSlots()
        {
            object balance = NewBalance(); object sim = Make(balance: balance);
            for (int i = 0; i < 21; i++) Assert.IsTrue(Spawn(sim, 0, 999));
            Assert.IsFalse(Spawn(sim, 0, 1));
            IList customers = Customers(sim);
            Assert.AreEqual(21, customers.Count);
            for (int i = 0; i < customers.Count; i++)
                for (int j = i + 1; j < customers.Count; j++)
                    Assert.AreNotEqual(Get(customers[i], "Target"), Get(customers[j], "Target"));
        }

        [Test]
        public void FreedCrowdSlotCanBeOccupiedByALaterCustomer()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "customerPatienceSeconds", 0.1f);
            object sim = Make(balance: balance); Start(sim); Spawn(sim, 0, 999);
            object originalTarget = Get(Customers(sim)[0], "Target");
            Step(sim, 12f);
            IList customers = Customers(sim);
            bool foundReused = false;
            for (int i = 0; i < customers.Count; i++)
                if ((int)Get(customers[i], "Id") > 1 && Get(customers[i], "Target").Equals(originalTarget)) foundReused = true;
            Assert.IsTrue(foundReused);
        }

        [Test]
        public void PurchasesCostCoinsAndAreAvailableDuringPlay()
        {
            object sim = Make(coins: 40); Start(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire")); Assert.AreEqual(2, Get(sim, "StaffCount"));
            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed")); Assert.AreEqual(1, Get(sim, "SpeedLevel"));
            Assert.AreEqual(10, Get(sim, "Coins"));
        }

        [Test]
        public void WinUnlocksNextLevelAndCreatesReadyNextRound()
        {
            object balance = NewBalance(); Tune(balance, "levelGoals", new[] { 1, 2, 3, 4, 5 });
            object sim = Make(level: 1, balance: balance); Start(sim); Spawn(sim, 0, 1);
            for (int i = 0; i < 100 && Get(sim, "Phase").ToString() == "Playing"; i++) Step(sim, 0.1f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.IsTrue((bool)Call(sim, "NextLevel"));
            Assert.AreEqual("Ready", Get(sim, "Phase").ToString()); Assert.AreEqual(2, Get(sim, "LevelIndex"));
        }

        [Test]
        public void SavePayloadJsonRoundTripsWithoutAccessingPlayerPrefs()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object payload = Activator.CreateInstance(saveType);
            saveType.GetField("unlockedLevel").SetValue(payload, 2);
            saveType.GetField("coins").SetValue(payload, 17);
            saveType.GetField("price").SetValue(payload, 4f);
            saveType.GetField("prices").SetValue(payload, new[]{4f,7f,9f,11f,3f,6f,5f});
            string json = JsonUtility.ToJson(payload);
            object restored = JsonUtility.FromJson(json, saveType);
            Assert.AreEqual(2, saveType.GetField("unlockedLevel").GetValue(restored));
            Assert.AreEqual(17, saveType.GetField("coins").GetValue(restored));
            Assert.AreEqual(4f, saveType.GetField("price").GetValue(restored));
            Assert.AreEqual(7f,((float[])saveType.GetField("prices").GetValue(restored))[1]);
        }

        [Test]
        public void ProductCatalogRemainsSevenAndPriceUsesConfigurableRange()
        {
            object balance = NewBalance(); Tune(balance, "levelProductCounts", new[] { 1, 1, 5, 6, 7 });
            object sim = Make(level: 1, balance: balance);
            Call(sim, "SetPrice", 70f); Assert.AreEqual(60f, Get(sim, "Price")); Assert.AreEqual(0f, Get(sim, "DemandFraction"));
            Call(sim, "SetPrice", -1f); Assert.AreEqual(0f, Get(sim, "Price")); Assert.AreEqual(1f, Get(sim, "DemandFraction"));
            Assert.AreEqual(7, ((string[])simType.GetField("ProductNames", BindingFlags.Public | BindingFlags.Static).GetValue(null)).Length);
        }

        [Test]
        public void PerProductPricesSurviveRoundTripAndDetermineTheirOwnRevenue()
        {
            object balance=NewBalance();Tune(balance,"maxCustomers",1);Tune(balance,"levelGoals",new[]{1,1,1,1,1});
            object sim=Make(level:1,balance:balance);Call(sim,"SetProductPrice",1,17f);
            Start(sim);Spawn(sim,1,1);Step(sim,8f);
            Assert.AreEqual(17,Get(sim,"Coins"));Assert.AreEqual(17f,Call(sim,"GetProductPrice",1));
        }
        [Test]
        public void OwnedRandomIgnoresUnityGlobalRandomAndCatalogConfigIsClamped()
        {
            object b=NewBalance();Tune(b,"levelProductCounts",new[]{99,99,99,99,99});
            object a=Make(balance:b), c=Make(balance:b);Start(a);Start(c);
            UnityEngine.Random.InitState(111);Step(a,.3f);UnityEngine.Random.InitState(222);Step(c,.3f);
            Assert.AreEqual(7,Get(a,"ProductCount"));Assert.AreEqual(Get(Customers(a)[0],"Product"),Get(Customers(c)[0],"Product"));
        }

        [Test]
        public void HighPriceReducesActualArrivalsAndBulkQuantity()
        {
            object cheap=Make(level:1),expensive=Make(level:1);for(int p=0;p<3;p++)Call(expensive,"SetProductPrice",p,60f);Start(cheap);Start(expensive);
            Step(cheap,3f);Step(expensive,3f);
            Assert.Greater(Customers(cheap).Count,Customers(expensive).Count);
            Assert.Greater((int)Get(Customers(cheap)[0],"Remaining"),900);
            if(Customers(expensive).Count>0)Assert.LessOrEqual((int)Get(Customers(expensive)[0],"Remaining"),1);
        }

        [Test]
        public void FlorestaServesPastGoalAndWinsOnlyAtThreeMinuteDeadline()
        {
            object sim = Make(coins: 1000, staff: 8);
            for (int i = 0; i < 7; i++) Call(sim, "TryUpgradeSpeed");
            Start(sim);
            for (int i = 0; i < 3; i++) Step(sim, 10f);
            Assert.Greater((int)Get(sim, "Delivered"), (int)Get(sim, "Goal"));
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
            int coins = (int)Get(sim, "Coins"); Step(sim, 10f);
            Assert.Greater((int)Get(sim, "Coins"), coins);
            for (int i = 0; i < 13; i++) Step(sim, 10f);
            Step(sim, 9f);
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
            Assert.Greater((float)Get(sim, "TimeRemaining"), 0f);
            Step(sim, 2f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.AreEqual(180f, Get(sim, "Elapsed"));
            Assert.AreEqual(0f, Get(sim, "TimeRemaining"));
            int earned = (int)Get(sim, "Coins"); Step(sim, 5f);
            Assert.AreEqual(180f, Get(sim, "Elapsed"));
            Assert.AreEqual(earned, Get(sim, "Coins"));
        }

        [Test]
        public void FlorestaLosesAtDeadlineWhenGoalWasNotMet()
        {
            object b = NewBalance(); Tune(b, "levelDurations", new[] { .5f, 210f, 240f, 270f, 300f });
            object sim = Make(balance: b); Start(sim); Step(sim, 2f);
            Assert.AreEqual("Lost", Get(sim, "Phase").ToString());
            Assert.AreEqual(.5f, Get(sim, "Elapsed"));
            Assert.AreEqual(0f, Get(sim, "TimeRemaining"));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void LaterLevelsStillWinEarly(int level)
        {
            object b = NewBalance(); Tune(b, "levelGoals", new[] { 1, 1, 1, 1, 1 });
            object sim = Make(level: level, balance: b); Start(sim); Spawn(sim, 0, 1); Step(sim, 8f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.Greater((float)Get(sim, "TimeRemaining"), 0f);
        }


        [Test]
        public void FlorestaNormalizesOldPriceAndKeepsProgressWhenSwitchingLevels()
        {
            object sim = Activator.CreateInstance(simType, new object[] { NewBalance(), 0, 60f, 123, 3, 4 });
            Assert.AreEqual(5f, Get(sim, "Price"));
            Assert.IsFalse((bool)Get(sim, "CanEditPrices"));
            Call(sim, "SetPrice", 0f); Call(sim, "SetProductPrice", 0, 60f);
            Assert.AreEqual(5f, Call(sim, "GetProductPrice", 0));
            Assert.AreEqual(123, Get(sim, "Coins")); Assert.AreEqual(3, Get(sim, "StaffCount"));
            Assert.AreEqual(4, Get(sim, "SpeedLevel"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.4f).Within(.0001f));
            Call(sim, "SelectLevel", 1); Call(sim, "SetPrice", 17f);
            Assert.IsTrue((bool)Get(sim, "CanEditPrices")); Assert.AreEqual(17f, Get(sim, "Price"));
            Call(sim, "SelectLevel", 0); Assert.AreEqual(5f, Get(sim, "Price"));
        }

        [Test]
        public void FlorestaRepeatPurchasesHaveFixedCostsAndTenPercentBaseSteps()
        {
            object sim = Make(coins: 100); Start(sim);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(25, Get(sim, "HireCost")); Assert.IsTrue((bool)Call(sim, "TryHire"));
                Assert.AreEqual(5, Get(sim, "SpeedCost")); Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
                Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1f + .1f * (i + 1)).Within(.0001f));
            }
            Assert.AreEqual(40, Get(sim, "Coins")); Assert.AreEqual(3, Get(sim, "StaffCount"));
            Assert.AreEqual(25, Get(sim, "HireCost")); Assert.AreEqual(5, Get(sim, "SpeedCost"));
        }

        [Test]
        public void FlorestaInsufficientFundsDoNotChangePurchases()
        {
            object speed = Make(coins: 4); Assert.IsFalse((bool)Call(speed, "TryUpgradeSpeed"));
            Assert.AreEqual(4, Get(speed, "Coins")); Assert.AreEqual(0, Get(speed, "SpeedLevel"));
            object hire = Make(coins: 24); Assert.IsFalse((bool)Call(hire, "TryHire"));
            Assert.AreEqual(24, Get(hire, "Coins")); Assert.AreEqual(1, Get(hire, "StaffCount"));
        }

        [Test]
        public void LaterLevelPurchaseBalanceRemainsUnchanged()
        {
            object sim = Make(coins: 100, staff: 3, level: 1);
            Call(sim, "TryUpgradeSpeed"); Call(sim, "TryUpgradeSpeed");
            Assert.AreEqual(45, Get(sim, "HireCost")); Assert.AreEqual(15, Get(sim, "SpeedCost"));
            Assert.AreEqual(85, Get(sim, "Coins")); Assert.AreEqual(1.5f, Get(sim, "WorkRate"));
        }

        [Test]
        public void FlorestaAutomaticOrdersCoverOneThroughFourIncludingFirstArrival()
        {
            object sim = Make(); Start(sim);
            var ids = new System.Collections.Generic.HashSet<int>();
            var quantities = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 100; i++)
            {
                Step(sim, .05f);
                foreach (object customer in Customers(sim))
                    if (ids.Add((int)Get(customer, "Id")))
                    {
                        int count = (int)Get(customer, "Remaining");
                        Assert.That(count, Is.InRange(1, 4)); Assert.AreEqual(0, Get(customer, "Product"));
                        quantities.Add(count);
                    }
            }
            Assert.GreaterOrEqual(ids.Count, 21);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, quantities);
        }

        [Test]
        public void FlorestaExplicitSpawnCannotCreateZeroOrMassOrders()
        {
            object sim = Make(); Start(sim);
            Spawn(sim, 0, 0); Spawn(sim, 0, 999);
            Assert.AreEqual(1, Get(Customers(sim)[0], "Remaining"));
            Assert.AreEqual(4, Get(Customers(sim)[1], "Remaining"));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CompletedFlorestaOrderEarnsExactAmountLeavesAndIsReplaced(int quantity)
        {
            object b = NewBalance(); Tune(b, "maxCustomers", 1);
            object sim = Make(balance: b); Start(sim); Spawn(sim, 0, quantity);
            object original = Customers(sim)[0]; object target = Get(original, "Target");
            for (int i = 0; i < 1000 && (int)Get(original, "Remaining") > 0; i++) Step(sim, .05f);
            Assert.AreEqual(0, Get(original, "Remaining")); Assert.AreEqual(0, Get(original, "Reserved"));
            Assert.AreEqual(quantity, Get(sim, "Delivered")); Assert.AreEqual(quantity * 5, Get(sim, "Coins"));
            for (int i = 0; i < 100 && Customers(sim).Contains(original); i++) Step(sim, .05f);
            Assert.AreEqual("Leaving", Get(original, "State").ToString()); Assert.IsFalse(Customers(sim).Contains(original));
            Step(sim, .05f); Assert.AreEqual(1, Customers(sim).Count);
            object replacement = Customers(sim)[0];
            Assert.Greater((int)Get(replacement, "Id"), (int)Get(original, "Id"));
            Assert.AreEqual(target, Get(replacement, "Target"));
            Assert.That((int)Get(replacement, "Remaining"), Is.InRange(1, 4));
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
        }


        [Test]
        public void OnlyPlayingRoundAdvancesClock()
        {
            object sim = Make(); Step(sim, 1f); Assert.AreEqual(0f, Get(sim, "Elapsed"));
            Start(sim); Step(sim, 1f); Assert.Greater((float)Get(sim, "Elapsed"), 0f);
        }
    }
}
