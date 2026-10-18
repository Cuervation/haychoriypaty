using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Runtime stays in Assembly-CSharp; these focused domain tests bind by reflection.
    public sealed class StreetActiveCooksTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private Type productionType, balanceType, simulationType, simulationBalanceType, roleType, marketType, marketDataType;

        [SetUp]
        public void SetUp()
        {
            productionType = Type.GetType("HayChoriYPaty.StreetKitchenProduction, Assembly-CSharp", true);
            balanceType = Type.GetType("HayChoriYPaty.StreetKitchenBalance, Assembly-CSharp", true);
            simulationType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            simulationBalanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            roleType = Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true);
            marketType = Type.GetType("HayChoriYPaty.StreetMarket, Assembly-CSharp", true);
            marketDataType = Type.GetType("HayChoriYPaty.StreetMarketData, Assembly-CSharp", true);
        }

        private object Make(int[] products, bool active, Action<object> configure = null)
        {
            object balance = Activator.CreateInstance(balanceType);
            configure?.Invoke(balance);
            return Activator.CreateInstance(productionType, new object[] { balance, products, null, active });
        }

        private static object Get(object value, string name) => value.GetType().GetProperty(name, PublicInstance).GetValue(value);
        private static object Call(object value, string name, params object[] args)
        {
            MethodInfo[] candidates = value.GetType().GetMethods(PublicInstance);
            foreach (MethodInfo candidate in candidates)
            {
                if (candidate.Name != name) continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool matches = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { matches = false; break; }
                if (matches) return candidate.Invoke(value, args);
            }
            throw new MissingMethodException(value.GetType().FullName, name);
        }
        private static IList Units(object kitchen) => (IList)Get(kitchen, "Units");
        private static IList Workers(object sim) => (IList)Get(sim, "Workers");
        private static object EnumValue(Type type, string name) => Enum.Parse(type, name);

        private object MakeActiveSimulation(int level, int startingCoins)
        {
            object balance = MakeActiveBalance(level, startingCoins);
            return Activator.CreateInstance(simulationType, new object[] { balance, level, 5f, 0, 1, 0 });
        }

        private object MakeActiveBalance(int level, int startingCoins)
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            simulationBalanceType.GetField("maxCustomers", PublicInstance).SetValue(balance, 9);
            simulationBalanceType.GetField("customerArrivalSeconds", PublicInstance).SetValue(balance, 10000f);
            simulationBalanceType.GetField("workerSpeed", PublicInstance).SetValue(balance, 900f);
            simulationBalanceType.GetField("pickupSeconds", PublicInstance).SetValue(balance, .05f);
            Array levels = (Array)simulationBalanceType.GetField("levelBalances", PublicInstance).GetValue(balance);
            object row = levels.GetValue(level);
            row.GetType().GetField("startingCoins", PublicInstance).SetValue(row, startingCoins);
            row.GetType().GetField("goalsByProductId", PublicInstance).SetValue(row, new[] { 1000, 1000, 1000, 1000, 1000, 1000, 1000 });
            float[] durations = (float[])simulationBalanceType.GetField("levelDurations", PublicInstance).GetValue(balance);
            durations[level] = 1000f;
            return balance;
        }

        private static void SetArrivalFarFuture(object sim)
        {
            sim.GetType().GetField("arrival", PrivateInstance).SetValue(sim, 10000f);
        }

        private static void Step(object sim, int ticks)
        {
            for (int i = 0; i < ticks; i++) Call(sim, "Step", .05f);
        }

        [Test]
        public void ActiveSandwichTablesStartEmptyAndLegacyProductionRetainsStock()
        {
            int[] sandwiches = { 0, 1, 2, 3 };
            object legacy = Make(sandwiches, false);
            object active = Make(new[] { 0, 1, 2, 3, 4 }, true);
            for (int i = 0; i < sandwiches.Length; i++)
            {
                Assert.Greater((int)Call(legacy, "TableCount", sandwiches[i]), 0, "Legacy/table-first mode is preserved.");
                Assert.AreEqual(0, (int)Call(active, "TableCount", sandwiches[i]), "Active cooks must produce each sandwich unit.");
            }
            Assert.Greater((int)Call(active, "TableCount", 4), 0, "Beverage stock remains on its existing path.");
        }

        [Test]
        public void ActiveCookReservesUniqueRawUnitAndProgressScalesWithWorkRate()
        {
            object kitchen = Make(new[] { 0 }, true);
            object first = Call(kitchen, "ReserveCook", 0, 1);
            object second = Call(kitchen, "ReserveCook", 0, 2);
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            int firstId = (int)Get(first, "Id"), secondId = (int)Get(second, "Id");
            Assert.AreNotEqual(firstId, secondId);
            Assert.AreEqual("Raw", Get(first, "State").ToString(), "Dispatch reserves one identifiable raw grill unit; cooking starts on arrival.");

            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", firstId, 1, 1f, 1f));
            Assert.That((float)Get(first, "Progress"), Is.EqualTo(.5f).Within(.001f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", firstId, 1, .5f, 2f));
            Assert.That((float)Get(first, "Progress"), Is.EqualTo(1f).Within(.001f));
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", firstId, 99, 1f, 1f), "Only the owning worker advances its cook.");
        }

        [Test]
        public void ActiveCookDefaultDurationIsTwoSeconds()
        {
            object balance = Activator.CreateInstance(balanceType);
            float duration = (float)balanceType.GetField("activeCookSeconds", PublicInstance).GetValue(balance);
            Assert.That(duration, Is.EqualTo(2f).Within(.0001f));

            object kitchen = Make(new[] { 0 }, true);
            object unit = Call(kitchen, "ReserveCook", 0, 5);
            int id = (int)Get(unit, "Id");
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", id, 5, 1.99f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, 5, .01f, 1f));
            Assert.AreEqual("Cooked", Get(unit, "State").ToString());
        }

        [Test]
        public void CookedUnitMustBeClaimedAndDepositedIntoItsReservedTableSlot()
        {
            object kitchen = Make(new[] { 0 }, true);
            object unit = Call(kitchen, "ReserveCook", 0, 7);
            int id = (int)Get(unit, "Id");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 7), "An in-flight production item already reserves its destination capacity.");
            Assert.IsFalse((bool)Call(kitchen, "AdvanceCook", id, 8, .5f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, 7, 2f, 1f));
            Assert.IsFalse((bool)Call(kitchen, "TakeCooked", id, 8));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, 7));
            Assert.AreEqual("Carried", Get(unit, "Location").ToString());
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, 7));
            Assert.AreEqual("Table", Get(unit, "Location").ToString());
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
        }

        [Test]
        public void TableCapacityAndWorkerReservationsPreventOverbookingAndReleaseCleanly()
        {
            object kitchen = Make(new[] { 0 }, true, balance =>
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 1));
            object unit = Call(kitchen, "ReserveCook", 0, 11);
            int id = (int)Get(unit, "Id");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 11), "The in-flight item owns its destination slot directly.");
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 12), "In-flight items consume finite table capacity.");
            Call(kitchen, "CancelCook", 11);
            Call(kitchen, "ReleaseTableReservation", 11);
            Assert.AreEqual(0, (int)Call(kitchen, "TableCount", 0));
            object replacement = Call(kitchen, "ReserveCook", 0, 12);
            Assert.IsNotNull(replacement);
            Assert.AreNotEqual(id, (int)Get(replacement, "Id"), "Canceled item identity is never reused.");
        }

        [Test]
        public void TableReservationsAreExclusiveAndCannotClaimMissingStock()
        {
            object kitchen = Make(new[] { 0 }, true);
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 1), "No phantom unit may be reserved.");
            object unit = Call(kitchen, "ReserveCook", 0, 1);
            int id = (int)Get(unit, "Id");
            Call(kitchen, "AdvanceCook", id, 1, 2f, 1f);
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, 1));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, 1));
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", 0, 3));
            Assert.IsNull(Call(kitchen, "ReserveTableUnit", 0, 4));
            Assert.IsFalse((bool)Call(kitchen, "TakeCooked", id, 4));
        }

        [Test]
        public void RealMarketIngredientsAreReleasedOnCookCancellationAndConsumedOnlyAtSale()
        {
            object balance = MakeActiveBalance(5, 1000);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(marketDataType);
            int[] stocked = new int[10];
            for (int i = 0; i < stocked.Length; i++) stocked[i] = 10000;
            marketDataType.GetField("stockUnits", PublicInstance).SetValue(data, stocked);
            object market = Activator.CreateInstance(marketType, new[] { data, balance });
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
            Call(sim, "BindIngredientStock", market);
            Call(sim, "StartRound");
            object kitchen = Get(sim, "Kitchen");
            int chorizoBefore = (int)Call(market, "Units", 0), breadBefore = (int)Call(market, "Units", 1);

            object cancelled = Call(kitchen, "ReserveCook", 0, 1);
            Assert.IsNotNull(cancelled);
            int cancelledId = (int)Get(cancelled, "Id");
            Call(kitchen, "AdvanceCook", cancelledId, 1, .25f, 1f);
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Reservation cannot spend ingredients.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));
            Call(kitchen, "CancelCook", 1);
            Assert.IsNull(Call(kitchen, "Find", cancelledId));
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Cancelled cooking releases its reserved recipe.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));

            object cooked = Call(kitchen, "ReserveCook", 0, 2);
            int cookedId = (int)Get(cooked, "Id");
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", cookedId, 2, 2f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", cookedId, 2));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", cookedId, 2));
            Assert.AreEqual(chorizoBefore, Call(market, "Units", 0), "Depositing edible food does not consume its ingredient reservation.");
            Assert.AreEqual(breadBefore, Call(market, "Units", 1));
            Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", 0, 3));
            Assert.IsNotNull(Call(kitchen, "TryTake", 0, EnumValue(roleType, "Mozo"), 3));
            Assert.IsTrue((bool)Call(kitchen, "Consume", cookedId, 0, 3));
            Assert.AreEqual(chorizoBefore - 1, Call(market, "Units", 0));
            Assert.AreEqual(breadBefore - 1, Call(market, "Units", 1));
        }

        [Test]
        public void BeverageRoleStockBehaviorRemainsAvailableInActiveCookMode()
        {
            object kitchen = Make(new[] { 4, 5, 6 }, true);
            Assert.IsNotNull(Call(kitchen, "TryTake", 4, EnumValue(roleType, "Cocacolero"), 21));
            Assert.IsNotNull(Call(kitchen, "TryTake", 5, EnumValue(roleType, "Fernetero"), 22));
            Assert.IsNotNull(Call(kitchen, "TryTake", 6, EnumValue(roleType, "Cocacolero"), 23));
        }

        [Test]
        public void ActiveCookGateStartsAtSixthLevelAndDoesNotChangeEarlierMode()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            for (int level = 0; level < 11; level++)
            {
                object sim = Activator.CreateInstance(simulationType, new object[] { balance, level, 5f, 0, 1, 0 });
                Assert.AreEqual(level >= 5, (bool)Get(sim, "UsesActiveCooks"), "Unexpected active cook mode at level index " + level);
                Assert.AreEqual(0, Get(sim, "MozoCount"), "A fresh team never receives a free Mozo.");
            }
            object early = Activator.CreateInstance(simulationType, new object[] { balance, 4, 5f, 0, 1, 0 });
            object active = Activator.CreateInstance(simulationType, new object[] { balance, 5, 5f, 0, 1, 0 });
            Assert.AreEqual(0, Get(early, "MozoCount"));
            Assert.AreEqual(0, Get(active, "MozoCount"), "Mozo is never granted automatically.");
            Assert.IsFalse((bool)Get(early, "HasMozo"));
            Assert.IsTrue((bool)Get(active, "HasMozo"));
        }

        [Test]
        public void MozoHiringIsPlayOnlyAndUsesTheExistingIncreasingCostCurve()
        {
            object balance = Activator.CreateInstance(simulationBalanceType);
            Array levels = (Array)simulationBalanceType.GetField("levelBalances", PublicInstance).GetValue(balance);
            object river = levels.GetValue(7);
            river.GetType().GetField("startingCoins", PublicInstance).SetValue(river, 1000);
            object sim = Activator.CreateInstance(simulationType, new object[] { balance, 7, 5f, 10000, 1, 0 });
            object mozo = EnumValue(roleType, "Mozo");
            Assert.AreEqual(0, Get(sim, "MozoCount"));
            Assert.IsFalse((bool)Get(sim, "CanHireMozo"), "Role purchases are offered only during play.");
            int firstCost = (int)Get(sim, "MozoHireCost");
            Assert.AreEqual(60, firstCost, "River is the $60 first-hire reference.");
            Call(sim, "StartRound");
            Assert.IsTrue((bool)Get(sim, "CanHireMozo"));
            int coinsBeforeHire = (int)Get(sim, "Coins");
            Assert.GreaterOrEqual(coinsBeforeHire, firstCost);
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo));
            Assert.AreEqual(1, Get(sim, "MozoCount"));
            Assert.AreEqual(coinsBeforeHire - firstCost, Get(sim, "Coins"));
            int secondCost = (int)Get(sim, "MozoHireCost");
            Assert.Greater(secondCost, firstCost);
            Assert.IsTrue((bool)Call(sim, "TryHire", mozo), "Mozo hiring remains unlimited while affordable.");
            Assert.AreEqual(2, Get(sim, "MozoCount"));
        }

        [Test]
        public void LegacySaveWithoutMozoFieldDefaultsToNoMozo()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object restored = UnityEngine.JsonUtility.FromJson("{\"unlockedLevel\":7,\"coins\":25,\"price\":5}", saveType);
            Assert.AreEqual(0, saveType.GetField("mozos").GetValue(restored));
        }

        [Test]
        public void CookTravelsCooksCarriesAndDepositsButCannotSellUntilMozoIsHired()
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
            object kitchen = Get(sim, "Kitchen");
            object kitchenLayout = Get(sim, "KitchenLayout");

            bool sawGrillTravel = false, sawPartialCook = false, sawCarriedUnit = false;
            for (int i = 0; i < 600 && (int)Call(Get(sim, "Kitchen"), "TableCount", 0) == 0; i++)
            {
                Call(sim, "Step", .05f);
                foreach (object worker in Workers(sim))
                {
                    string state = Get(worker, "State").ToString();
                    if (state == "ToGrill") sawGrillTravel = true;
                    if (state == "Cooking")
                    {
                        int stationIndex = 0, stationCount = 0;
                        foreach (object candidate in Workers(sim))
                        {
                            if (Get(candidate, "Role").ToString() != Get(worker, "Role").ToString()) continue;
                            if (Equals(Get(candidate, "Id"), Get(worker, "Id"))) stationIndex = stationCount;
                            stationCount++;
                        }
                        Vector2 work = (Vector2)Call(kitchenLayout, "GrillWorkPosition", Get(worker, "Product"), stationIndex, stationCount);
                        Assert.AreEqual(work, Get(worker, "Position"), "Parrillero cooks at their own separated team station.");
                    }
                    if (state == "Cooking" && (float)Get(worker, "CookingProgress") > 0f && (float)Get(worker, "CookingProgress") < 1f)
                        sawPartialCook = true;
                    if (state == "ToTable" && (int)Get(worker, "CarriedItemId") > 0) sawCarriedUnit = true;
                }
            }
            Assert.IsTrue(sawGrillTravel, "Cook must physically reach the grill through its route.");
            Assert.IsTrue(sawPartialCook, "Cooking progress must be a real partial fraction.");
            Assert.IsTrue(sawCarriedUnit, "The finished identified unit must be carried to the table.");
            Assert.AreEqual(1, (int)Call(Get(sim, "Kitchen"), "TableCount", 0));
            Assert.AreEqual(0, Get(sim, "Delivered"), "Active cooks never deliver a sandwich directly.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            for (int i = 0; i < 600 && (int)Get(sim, "Delivered") == 0; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(1, Get(sim, "Delivered"), "Mozo must fetch the deposited food and complete the real customer handoff.");
            Assert.AreEqual(1, Call(sim, "GetProductDelivered", 0));
        }

        [Test]
        public void MozoPickupAnchorsFollowEachProductAndActualTableSlot()
        {
            object sim = MakeActiveSimulation(5, 1000);
            object layout = Get(sim, "KitchenLayout");
            object kitchen = Get(sim, "Kitchen");
            Vector2 home = new Vector2(433f, 400f);
            const float workerHalfWidth = 45f;
            float sceneWidth = 540f;

            for (int product = 0; product < 4; product++)
            {
                bool companionAvailable = (bool)Call(sim, "IsProductAvailable", product ^ 1);
                int capacity = (int)Call(kitchen, "TableCapacity", product);
                Vector2 standard = (Vector2)Call(layout, "PickupPosition", product);
                Vector2 firstItem = (Vector2)Call(layout, "TableSlotPosition", product, 0, companionAvailable, capacity);
                HashSet<int> slots = new HashSet<int> { 0, 1, 3, capacity / 2, capacity - 1 };

                foreach (int slot in slots)
                {
                    Vector2 item = (Vector2)Call(layout, "TableSlotPosition", product, slot, companionAvailable, capacity);
                    Vector2 target = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companionAvailable, capacity);
                    Assert.AreEqual(Mathf.Clamp(item.x, workerHalfWidth, sceneWidth - workerHalfWidth), target.x,
                        "Product " + product + " slot " + slot + " keeps the full Mozo on screen while following the food's X position.");
                    Assert.AreEqual(standard.y + item.y - firstItem.y, target.y, .001f,
                        "Product " + product + " slot " + slot + " follows its row/depth on the same reachable table front.");
                    Vector2[] route = (Vector2[])Call(layout, "FindShortestSafeRoute", home, target);
                    Assert.IsNotNull(route, "Product " + product + " slot " + slot + " must remain reachable.");
                    Assert.Greater(route.Length, 0);
                    Assert.AreEqual(target, route[route.Length - 1]);
                }
                Assert.AreNotEqual(standard,
                    (Vector2)Call(layout, "TableSlotPickupPosition", product, 0, companionAvailable, capacity),
                    "Mozo no longer uses the shared center point for product " + product + ".");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MozoWalksToTheReservedSandwichSlotForEachProduct(int product)
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            if (product >= 2)
                Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1));

            object kitchen = Get(sim, "Kitchen");
            for (int i = 0; i < 600 && (int)Call(kitchen, "TableCount", product) == 0; i++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", product), "The requested product must be physically prepared first.");

            object unit = null;
            foreach (object candidate in Units(kitchen))
                if ((int)Get(candidate, "Product") == product && Get(candidate, "Location").ToString() == "Table" &&
                    Get(candidate, "State").ToString() == "Cooked") { unit = candidate; break; }
            Assert.IsNotNull(unit, "The target must be derived from a real cooked unit.");
            int slot = (int)Get(unit, "Slot");
            object layout = Get(sim, "KitchenLayout");
            bool companionAvailable = (bool)Call(sim, "IsProductAvailable", product ^ 1);
            int capacity = (int)Call(kitchen, "TableCapacity", product);
            Vector2 expected = (Vector2)Call(layout, "TableSlotPickupPosition", product, slot, companionAvailable, capacity);
            Assert.AreNotEqual((Vector2)Call(layout, "PickupPosition", product), expected,
                "Each of Chori, Paty, Bondiola, and Vacío must have a product-specific table destination.");

            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            object mozo = null;
            foreach (object worker in Workers(sim))
                if (Get(worker, "Role").ToString() == "Mozo") { mozo = worker; break; }
            Assert.IsNotNull(mozo);

            bool reachedReservedSlot = false;
            for (int i = 0; i < 300 && !reachedReservedSlot; i++)
            {
                Call(sim, "Step", .05f);
                reachedReservedSlot = Vector2.Distance((Vector2)Get(mozo, "Position"), expected) < .001f;
            }
            Assert.IsTrue(reachedReservedSlot, "The Mozo must physically reach product " + product + " slot " + slot + ".");

            for (int i = 0; i < 600 && (int)Call(sim, "GetProductDelivered", product) == 0; i++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(1, (int)Call(sim, "GetProductDelivered", product),
                "Correct slot routing must still complete the real handoff.");
        }

        [Test]
        public void ConcurrentCooksKeepDistinctIdentifiedUnitsAndPerWorkerProgress()
        {
            object sim = MakeActiveSimulation(5, 1000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));

            for (int i = 0; i < 400; i++)
            {
                Call(sim, "Step", .05f);
                var liveCookIds = new HashSet<int>();
                var cookingPositions = new List<Vector2>();
                int progressing = 0;
                foreach (object worker in Workers(sim))
                    if (Get(worker, "State").ToString() == "Cooking" && (float)Get(worker, "CookingProgress") > 0f && (int)Get(worker, "CookingItemId") > 0)
                    {
                        progressing++;
                        Assert.IsTrue(liveCookIds.Add((int)Get(worker, "CookingItemId")), "Two cooks cannot own the same physical unit.");
                        cookingPositions.Add((Vector2)Get(worker, "Position"));
                    }
                if (progressing >= 2)
                {
                    Assert.AreEqual(2, liveCookIds.Count);
                    Assert.Greater(Vector2.Distance(cookingPositions[0], cookingPositions[1]), 120f,
                        "Two Parrilleros cooking at once must occupy separate grill-side stations.");
                    return;
                }
            }
            Assert.Fail("Two cooks never made simultaneous, independently progressing reservations; route/capacity may be blocking.");
        }

        [Test]
        public void FourSandwichRolesAndMixedBeveragesAllCompleteRealDeliveriesWithoutRouteBlocking()
        {
            object sim = MakeActiveSimulation(5, 3000);
            Call(sim, "StartRound");
            SetArrivalFarFuture(sim);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Cocacolero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Fernetero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Mozo")));
            for (int product = 0; product < 7; product++)
                Assert.IsTrue((bool)Call(sim, "SpawnCustomer", product, 1), "Expected valid level-6 order product " + product);

            for (int i = 0; i < 1200 && (int)Get(sim, "Delivered") < 7; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(7, Get(sim, "Delivered"), "Safe worker routes should make every sandwich and beverage handoff reachable.");
            for (int product = 0; product < 7; product++) Assert.AreEqual(1, Call(sim, "GetProductDelivered", product), "Incorrect count for product " + product);
            Assert.AreEqual(0, Get(sim, "SpeedLevel"), "A fresh attempt remains at x1.00 until speed is purchased.");
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(1, Get(sim, "ParrilleroCount"), "A fresh attempt starts with exactly one Parrillero.");
        }
    }
}
