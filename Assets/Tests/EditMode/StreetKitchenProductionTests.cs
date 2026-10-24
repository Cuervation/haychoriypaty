using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HayChoriYPaty;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetKitchenProductionTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private Type productionType, balanceType, stateType, locationType, roleType;

        // The test asmdef cannot reference predefined Assembly-CSharp directly.
        // Proxy the runtime interface just like the fixture reflects its other domain types.
        private sealed class EmptyIngredientStock : System.Runtime.Remoting.Proxies.RealProxy
        {
            public int CanPrepareCalls, ReserveCalls, ConsumeCalls;
            public EmptyIngredientStock() : base(Type.GetType("HayChoriYPaty.IStreetKitchenStock, Assembly-CSharp", true)) { }
            public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
            {
                var call = (System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
                if (call.MethodName == "CanPrepareFood") CanPrepareCalls++;
                if (call.MethodName == "TryReserveFood") ReserveCalls++;
                if (call.MethodName == "ConsumeReservedFood") ConsumeCalls++;
                return new System.Runtime.Remoting.Messaging.ReturnMessage(
                    call.MethodName == "ReleaseFood" ? null : (object)false, null, 0, call.LogicalCallContext, call);
            }
        }

        [SetUp]
        public void SetUp()
        {
            productionType = Type.GetType("HayChoriYPaty.StreetKitchenProduction, Assembly-CSharp", true);
            balanceType = Type.GetType("HayChoriYPaty.StreetKitchenBalance, Assembly-CSharp", true);
            stateType = Type.GetType("HayChoriYPaty.StreetFoodState, Assembly-CSharp", true);
            locationType = Type.GetType("HayChoriYPaty.StreetFoodLocation, Assembly-CSharp", true);
            roleType = Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true);
        }

        private object Make(int[] products, Action<object> configure = null)
        {
            object balance = Activator.CreateInstance(balanceType);
            configure?.Invoke(balance);
            return Activator.CreateInstance(productionType, new object[] { balance, products });
        }
        private object MakeActive(int[] products)
        {
            object balance = Activator.CreateInstance(balanceType);
            return Activator.CreateInstance(productionType, new object[] { balance, products, null, true });
        }
        private static object Get(object value, string name) => value.GetType().GetProperty(name, PublicInstance).GetValue(value);
        private static object Call(object value, string name, params object[] args)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in value.GetType().GetMethods(PublicInstance))
            {
                if (candidate.Name != name || candidate.GetParameters().Length != args.Length) continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                bool matches = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == null ? parameters[i].ParameterType.IsValueType
                        : !parameters[i].ParameterType.IsInstanceOfType(args[i])) { matches = false; break; }
                if (!matches) continue;
                if (method != null) throw new AmbiguousMatchException(value.GetType().Name + "." + name);
                method = candidate;
            }
            if (method == null) throw new MissingMethodException(value.GetType().Name + "." + name);
            return method.Invoke(value, args);
        }
        private static object EnumValue(Type type, string name) => Enum.Parse(type, name);
        private static IList Units(object kitchen) => (IList)Get(kitchen, "Units");
        private static object Unit(object kitchen, int product, string state, string location)
        {
            foreach (object unit in Units(kitchen))
                if ((int)Get(unit, "Product") == product && Get(unit, "State").ToString() == state && Get(unit, "Location").ToString() == location) return unit;
            return null;
        }

        [Test]
        public void InitialStockAndGrillSlotsFollowAvailableCatalog()
        {
            object early = Make(new[] { 0 });
            Assert.AreEqual(48, (int)Call(early, "TableCount", 0));
            Assert.AreEqual(48, (int)Call(early, "TableCapacity", 0), "A solo normal product retains all48 actual table units.");
            Assert.AreEqual(18, (int)Call(early, "GrillCapacity", 0));
            Assert.AreEqual(0, (int)Call(early, "GrillCapacity", 1));
            object full = Make(new[] { 0, 1, 2, 3, 4, 5, 6 });
            Assert.AreEqual(24, (int)Call(full, "TableCount", 0));
            Assert.AreEqual(24, (int)Call(full, "TableCount", 1));
            Assert.AreEqual(18, (int)Call(full, "TableCount", 2));
            Assert.AreEqual(18, (int)Call(full, "TableCount", 3));
            for (int product = 0; product < 4; product++)
                Assert.AreEqual(48, (int)Call(full, "TableCapacity", product), "Each paired sandwich has its own 48-slot limit.");
            Assert.AreEqual(12, (int)Call(full, "TableCount", 4));
            Assert.AreEqual(12, (int)Call(full, "TableCount", 6));
            Assert.AreEqual(12, (int)Call(full, "GrillCapacity", 0));
            Assert.AreEqual(6, (int)Call(full, "GrillCapacity", 1));
            Assert.AreEqual(6, (int)Call(full, "GrillCapacity", 2));
            Assert.AreEqual(4, (int)Call(full, "GrillCapacity", 3));
            Assert.IsNotNull(Unit(full, 0, "Raw", "Grill"));
            Assert.IsNotNull(Unit(full, 2, "Raw", "Grill"));
        }

        [Test]
        public void ActiveCooksGiveEachSandwich48SlotsAndStopUntilThatProductsSlotIsFreed()
        {
            object kitchen = MakeActive(new[] { 0, 1, 2, 3 });
            for (int product = 0; product < 4; product++)
            {
                Assert.AreEqual(48, (int)Call(kitchen, "TableCapacity", product), "Product " + product + " owns 48 slots independently.");
                for (int i = 0; i < 48; i++)
                {
                    int worker = 1000 + product * 100 + i;
                    object unit = Call(kitchen, "ReserveCook", product, worker);
                    Assert.IsNotNull(unit);
                    int id = (int)Get(unit, "Id");
                    Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", id, worker, 2f, 1f));
                    Assert.IsTrue((bool)Call(kitchen, "TakeCooked", id, worker));
                    Assert.IsTrue((bool)Call(kitchen, "DepositCooked", id, worker));
                }
                Assert.AreEqual(48, (int)Call(kitchen, "TableCount", product));
                Assert.IsFalse((bool)Call(kitchen, "CanReserveCook", product), "A 49th " + product + " unit must not start.");
                Assert.IsNull(Call(kitchen, "ReserveCook", product, 8000 + product));

                int waiter = 9000 + product;
                Assert.IsNotNull(Call(kitchen, "ReserveTableUnit", product, waiter));
                object carried = Call(kitchen, "TryTake", product, EnumValue(roleType, "Mozo"), waiter);
                Assert.IsNotNull(carried);
                Assert.IsTrue((bool)Call(kitchen, "Consume", Get(carried, "Id"), product, waiter));
                Assert.IsTrue((bool)Call(kitchen, "CanReserveCook", product), "Selling one unit reopens exactly one product slot.");
                int replacementWorker = 10000 + product;
                object replacement = Call(kitchen, "ReserveCook", product, replacementWorker);
                Assert.IsNotNull(replacement);
                int replacementId = (int)Get(replacement, "Id");
                Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", replacementId, replacementWorker, 2f, 1f));
                Assert.IsTrue((bool)Call(kitchen, "TakeCooked", replacementId, replacementWorker));
                Assert.IsTrue((bool)Call(kitchen, "DepositCooked", replacementId, replacementWorker));
                Assert.AreEqual(48, (int)Call(kitchen, "TableCount", product));
            }
        }

        [Test]
        public void LockedDrinkBarrelsAreNeverStockedAcrossAllElevenCatalogs()
        {
            int[][] catalogs =
            {
                new[] { 0 }, new[] { 0, 4 }, new[] { 0, 1, 4 },
                new[] { 0, 1, 2, 4, 6 },
                new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
                new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
                new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
                new[] { 0, 1, 2, 3, 4, 6, 5 }
            };
            foreach (int[] catalog in catalogs)
            {
                object kitchen = Make(catalog);
                bool hasCoca = Array.IndexOf(catalog, 4) >= 0, hasBeer = Array.IndexOf(catalog, 6) >= 0;
                Assert.AreEqual(hasCoca ? 12 : 0, (int)Call(kitchen, "TableCount", 4));
                Assert.AreEqual(hasBeer ? 12 : 0, (int)Call(kitchen, "TableCount", 6));
                Assert.AreEqual(hasCoca ? 12 : 0, (int)Call(kitchen, "TableCapacity", 4));
                Assert.AreEqual(hasBeer ? 12 : 0, (int)Call(kitchen, "TableCapacity", 6));
            }
        }

        [Test]
        public void PickupIsIndividualRoleGatedAndConsumableOnlyWithMatchingWorker()
        {
            object kitchen = Make(new[] { 0, 1, 2 });
            object wrongRole = EnumValue(roleType, "ParrilleroPremium");
            object normalRole = EnumValue(roleType, "Parrillero");
            Assert.IsNull(Call(kitchen, "TryTake", 0, wrongRole, 2));
            object taken = Call(kitchen, "TryTake", 0, normalRole, 7);
            Assert.IsNotNull(taken);
            int id = (int)Get(taken, "Id");
            Assert.AreEqual("Carried", Get(taken, "Location").ToString());
            Assert.IsFalse((bool)Call(kitchen, "Consume", id, 0, 8));
            Assert.IsTrue((bool)Call(kitchen, "Consume", id, 0, 7));
            Assert.IsNull(Call(kitchen, "Find", id));
            Assert.AreEqual(23, (int)Call(kitchen, "TableCount", 0));
        }

        [Test]
        public void CarriedFoodReturnsToItsRealStockOnCancellation()
        {
            object kitchen = Make(new[] { 0 });
            object item = Call(kitchen, "TryTake", 0, EnumValue(roleType, "Parrillero"), 3);
            int id = (int)Get(item, "Id");
            Call(kitchen, "ReturnCarried", id, 3);
            Assert.AreEqual("Table", Get(Call(kitchen, "Find", id), "Location").ToString());
            Assert.AreEqual(48, (int)Call(kitchen, "TableCount", 0));
        }

        [TestCase(false)] [TestCase(true)]
        public void CocaAndBeerBarrelsStayFullAndServePastEmptyMarketStock(bool activeCooks)
        {
            object balance = Activator.CreateInstance(balanceType);
            var emptyStock = new EmptyIngredientStock();
            object kitchen = Activator.CreateInstance(productionType,
                new object[] { balance, new[] { 4, 6 }, emptyStock.GetTransparentProxy(), activeCooks });
            object cocaRole = EnumValue(roleType, "Cocacolero");

            foreach (int product in new[] { 4, 6 })
            {
                Assert.AreEqual(12, (int)Call(kitchen, "TableCapacity", product));
                Assert.AreEqual(12, (int)Call(kitchen, "TableCount", product));
                Assert.IsTrue((bool)Call(kitchen, "HasSupply", product), "An enabled barrel is never sold out.");

                for (int delivery = 0; delivery < 24; delivery++)
                {
                    int workerId = 1000 + product * 100 + delivery;
                    object item = Call(kitchen, "TryTake", product, cocaRole, workerId);
                    Assert.IsNotNull(item, "An empty Mercado cannot block barrel item " + delivery + " for product " + product + ".");
                    Assert.AreEqual("Carried", Get(item, "Location").ToString());
                    Assert.AreEqual(12, (int)Call(kitchen, "TableCount", product), "Pickup immediately refills the same barrel slot.");
                    Assert.IsTrue((bool)Call(kitchen, "Consume", (int)Get(item, "Id"), product, workerId));
                    Assert.AreEqual(12, (int)Call(kitchen, "TableCount", product), "Serving does not reduce the barrel's visible stock.");
                    Assert.IsTrue((bool)Call(kitchen, "HasSupply", product));
                }
            }

            Assert.AreEqual(0, emptyStock.CanPrepareCalls, "Unlimited barrels do not consult finite ingredient stock.");
            Assert.AreEqual(0, emptyStock.ReserveCalls, "Unlimited barrels never reserve Mercado units.");
            Assert.AreEqual(0, emptyStock.ConsumeCalls, "Serving Coca/beer never consumes Mercado units.");
        }

        [Test]
        public void UnlimitedBarrelsKeepRoleOwnershipCancellationAndFernetStockRules()
        {
            var empty = new EmptyIngredientStock();
            object kitchen = Activator.CreateInstance(productionType, new object[] {
                Activator.CreateInstance(balanceType), new[] { 4, 5, 6 }, empty.GetTransparentProxy() });
            Assert.AreEqual(0, Call(kitchen, "TableCount", 5), "Fernet remains ingredient-backed.");
            Assert.IsFalse((bool)Call(kitchen, "HasSupply", 5));
            foreach (int product in new[] { 4, 6 })
            {
                Assert.IsNull(Call(kitchen, "TryTake", product, EnumValue(roleType, "Parrillero"), 1));
                object item = Call(kitchen, "TryTake", product, EnumValue(roleType, "Cocacolero"), 2);
                int id = (int)Get(item, "Id");
                Assert.IsFalse((bool)Call(kitchen, "Consume", id, product, 3), "Only the actual carrier can hand it off.");
                Assert.IsTrue((bool)Call(kitchen, "Consume", id, product, 2));
                Assert.IsFalse((bool)Call(kitchen, "Consume", id, product, 2), "No duplicate consumption/sale.");
                object cancelled = Call(kitchen, "TryTake", product, EnumValue(roleType, "Cocacolero"), 4);
                Call(kitchen, "ReturnCarried", (int)Get(cancelled, "Id"), 4);
                Assert.AreEqual(12, Call(kitchen, "TableCount", product), "Cancellation cannot grow the barrel beyond its12slots.");
            }
        }

        [Test]
        public void RealMarketWithZeroDrinkStockStillAllowsRepeatedPhysicalDrinkSales()
        {
            Type sb = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            Type st = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type mt = Type.GetType("HayChoriYPaty.StreetMarket, Assembly-CSharp", true);
            Type md = Type.GetType("HayChoriYPaty.StreetMarketData, Assembly-CSharp", true);
            object balance = Activator.CreateInstance(sb);
            sb.GetField("maxCustomers").SetValue(balance, 2);
            sb.GetField("customerArrivalSeconds").SetValue(balance, 10000f);
            object sim = Activator.CreateInstance(st, new object[] { balance, 4, 5f, 0, 1, 0 });
            object data = Activator.CreateInstance(md);
            object market = Activator.CreateInstance(mt, new[] { data, balance });
            int[] required = (int[])Call(market, "RequiredUnits", sim);
            md.GetField("stockUnits").SetValue(data, (int[])required.Clone());
            Call(sim, "BindIngredientStock", market);
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim), "The existing pre-round stock gate remains intact.");
            Call(sim, "StartRound");
            st.GetField("arrival", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sim, 10000f); // Isolate the two scripted drink orders, including the initial arrival.
            int[] stock = (int[])md.GetField("stockUnits").GetValue(data); stock[6] = stock[7] = 0;
            st.GetProperty("Coins").SetValue(sim, 100000);
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Cocacolero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", EnumValue(roleType, "Cocacolero")));
            st.GetProperty("Coins").SetValue(sim, 0);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 4, 24));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 6, 24));
            Assert.AreEqual(0, Get(sim, "Delivered")); Assert.AreEqual(0, Get(sim, "CoinsEarned"));
            for (int i = 0; i < 4000 && ((int)Call(sim, "GetProductDelivered", 4) < 24 || (int)Call(sim, "GetProductDelivered", 6) < 24); i++)
                Call(sim, "Step", .05f);
            Assert.AreEqual(24, Call(sim, "GetProductDelivered", 4));
            Assert.AreEqual(24, Call(sim, "GetProductDelivered", 6));
            Assert.AreEqual(48, Get(sim, "Delivered"));
            int expectedRevenue = 24 * ((int)(float)Call(sim, "GetProductPrice", 4) + (int)(float)Call(sim, "GetProductPrice", 6));
            Assert.AreEqual(expectedRevenue, Get(sim, "CoinsEarned"), "Only actual handoffs generate the unchanged sale prices.");
            Assert.AreEqual(0, Call(market, "Units", 6)); Assert.AreEqual(0, Call(market, "Units", 7));
            Assert.AreEqual(12, Call(Get(sim, "Kitchen"), "TableCount", 4));
            Assert.AreEqual(12, Call(Get(sim, "Kitchen"), "TableCount", 6));
        }

        [Test]
        public void GrillMeatAdvancesThroughCookedAndBurnedThenRefillsWithoutServingBurned()
        {
            object kitchen = Make(new[] { 0 }, balance =>
            {
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 48);
                balanceType.GetField("cookSeconds", PublicInstance).SetValue(balance, 1f);
                balanceType.GetField("burnAfterSeconds", PublicInstance).SetValue(balance, 2f);
                balanceType.GetField("discardBurnedAfterSeconds", PublicInstance).SetValue(balance, 1f);
            });
            object parrillero = EnumValue(roleType, "Parrillero");
            Assert.AreEqual(48, (int)Call(kitchen, "TableCount", 0), "All 48 physical product slots start full while grill meat burns.");
            Call(kitchen, "Advance", 1f, 1f, true, false, false, false);
            Assert.IsNotNull(Unit(kitchen, 0, "Cooked", "Grill"));
            Call(kitchen, "Advance", 2f, 1f, true, false, false, false);
            Assert.IsNotNull(Unit(kitchen, 0, "Burned", "Grill"));
            object edible = Call(kitchen, "TryTake", 0, parrillero, 1);
            Assert.IsNotNull(edible);
            Assert.AreEqual("Cooked", Get(edible, "State").ToString());
            Assert.IsTrue((bool)Call(kitchen, "Consume", (int)Get(edible, "Id"), 0, 1));
            int remainingPreparedStock = 0;
            while ((edible = Call(kitchen, "TryTake", 0, parrillero, 1)) != null)
            {
                remainingPreparedStock++;
                Assert.IsTrue((bool)Call(kitchen, "Consume", (int)Get(edible, "Id"), 0, 1));
            }
            Assert.AreEqual(47, remainingPreparedStock, "Drain all other prepared units before verifying that burned grill meat is not edible.");
            Assert.AreEqual(0, (int)Call(kitchen, "TableCount", 0));
            Call(kitchen, "Advance", 1f, 1f, true, false, false, false);
            Assert.IsNull(Unit(kitchen, 0, "Burned", "Grill"));
            Assert.IsNotNull(Unit(kitchen, 0, "Raw", "Grill"));
        }

        [Test]
        public void GrillRefillsVacatedSlotWhenCookedMeatMovesToTable()
        {
            object kitchen = Make(new[] { 0 }, balance =>
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 48));
            Call(kitchen, "TryTake", 0, EnumValue(roleType, "Parrillero"), 1);
            Call(kitchen, "Advance", 4f, 1f, true, false, false, false);
            Assert.AreEqual(48, (int)Call(kitchen, "TableCount", 0));
            Assert.AreEqual(18, CountAt(kitchen, 0, "Grill"));
            Assert.AreEqual(1, CountStateAt(kitchen, 0, "Raw", "Grill"));
            AssertUniqueGrillSlots(kitchen, 0);

            Call(kitchen, "TryTake", 0, EnumValue(roleType, "Parrillero"), 2);
            Call(kitchen, "Advance", .05f, 1f, true, false, false, false);
            Assert.AreEqual(18, CountAt(kitchen, 0, "Grill"));
            Assert.AreEqual(1, CountStateAt(kitchen, 0, "Raw", "Grill"));
            Assert.AreEqual(1, CountStateAt(kitchen, 0, "Cooking", "Grill"));
            AssertUniqueGrillSlots(kitchen, 0);
        }

        private static int CountAt(object kitchen, int product, string location)
        {
            int count = 0;
            foreach (object unit in Units(kitchen)) if ((int)Get(unit, "Product") == product && Get(unit, "Location").ToString() == location) count++;
            return count;
        }
        private static int CountStateAt(object kitchen, int product, string state, string location)
        {
            int count = 0;
            foreach (object unit in Units(kitchen)) if ((int)Get(unit, "Product") == product && Get(unit, "State").ToString() == state && Get(unit, "Location").ToString() == location) count++;
            return count;
        }
        private static void AssertUniqueGrillSlots(object kitchen, int product)
        {
            var slots = new HashSet<int>();
            foreach (object unit in Units(kitchen))
                if ((int)Get(unit, "Product") == product && Get(unit, "Location").ToString() == "Grill")
                    Assert.IsTrue(slots.Add((int)Get(unit, "Slot")), "Duplicate grill slot detected.");
            Assert.AreEqual(18, slots.Count);
        }
    }
}
