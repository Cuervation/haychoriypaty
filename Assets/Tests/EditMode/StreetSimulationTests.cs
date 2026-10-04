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
            object sim = Make(balance: balance); Start(sim); Assert.IsTrue(Spawn(sim, 0, 999));
            for (int i = 0; i < 200; i++) Step(sim, 0.1f);
            Assert.LessOrEqual((int)Get(sim, "Delivered"), 999);
            IList customers = Customers(sim);
            Assert.AreEqual(999 - (int)Get(sim, "Delivered"), customers.Count == 0 ? 0 : (int)Get(customers[0], "Remaining"));
        }

        [Test]
        public void MultipleWorkersNeverReserveMoreUnitsThanRemain()
        {
            object balance = NewBalance(); Tune(balance,"levelGoals",new[]{1,1,1,1,1}); Tune(balance, "maxCustomers", 1);
            object sim = Make(staff: 8, balance: balance); Start(sim); Spawn(sim, 0, 1); Step(sim, 8f);
            IList customers = Customers(sim);
            if (customers.Count > 0)
            {
                object customer = customers[0];
                Assert.LessOrEqual((int)Get(customer, "Reserved"), (int)Get(customer, "Remaining"));
            }
            Assert.LessOrEqual((int)Get(sim, "Delivered"), 1);
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
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1);
            object sim = Make(balance: balance); Call(sim, "SetPrice", 0f); Start(sim); Spawn(sim, 0, 1); Step(sim, 8f);
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
            Assert.AreEqual(20, Get(sim, "Coins"));
        }

        [Test]
        public void WinUnlocksNextLevelAndCreatesReadyNextRound()
        {
            object balance = NewBalance(); Tune(balance, "levelGoals", new[] { 1, 2, 3, 4, 5 });
            object sim = Make(balance: balance); Start(sim); Spawn(sim, 0, 1);
            for (int i = 0; i < 100 && Get(sim, "Phase").ToString() == "Playing"; i++) Step(sim, 0.1f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.IsTrue((bool)Call(sim, "NextLevel"));
            Assert.AreEqual("Ready", Get(sim, "Phase").ToString()); Assert.AreEqual(1, Get(sim, "LevelIndex"));
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
            object sim = Make();
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
            object cheap=Make(),expensive=Make();Call(expensive,"SetPrice",60f);Start(cheap);Start(expensive);
            Step(cheap,3f);Step(expensive,3f);
            Assert.Greater(Customers(cheap).Count,Customers(expensive).Count);
            Assert.Greater((int)Get(Customers(cheap)[0],"Remaining"),900);
            if(Customers(expensive).Count>0)Assert.LessOrEqual((int)Get(Customers(expensive)[0],"Remaining"),1);
        }

        [Test]
        public void OnlyPlayingRoundAdvancesClock()
        {
            object sim = Make(); Step(sim, 1f); Assert.AreEqual(0f, Get(sim, "Elapsed"));
            Start(sim); Step(sim, 1f); Assert.Greater((float)Get(sim, "Elapsed"), 0f);
        }
    }
}
