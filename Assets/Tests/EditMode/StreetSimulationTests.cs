using System;
using System.Collections;
using System.Collections.Generic;
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
        private static readonly Dictionary<object, int> PostStartStaffSetup = new Dictionary<object, int>();

        [SetUp]
        public void SetUp()
        {
            simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            PostStartStaffSetup.Clear();
        }

        private object Make(int coins = 0, int staff = 1, int level = 0, object balance = null)
        {
            if (balance == null) balance = Activator.CreateInstance(balanceType);
            object sim = Activator.CreateInstance(simType, new object[] { balance, level, 5f, coins, staff, 0 });
            if (staff > 1) PostStartStaffSetup[sim] = staff;
            if (coins > 0) Set(sim, "Coins", coins); // Synthetic fixture balance, not resumed save data.
            return sim;
        }
        private static object Get(object instance, string property)
        {
            Type type = instance.GetType();
            PropertyInfo member = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (member != null) return member.GetValue(instance, null);
            FieldInfo field = type.GetField(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(instance);
            throw new ArgumentException("No readable property or field named " + property + " on " + type.FullName);
        }
        private static object Call(object instance, string method, params object[] args)
        {
            MethodInfo target = null;
            foreach (MethodInfo candidate in instance.GetType().GetMethods(Instance))
                if (candidate.Name == method && candidate.GetParameters().Length == args.Length) { target = candidate; break; }
            return target.Invoke(instance, args);
        }
        private static void Start(object sim)
        {
            Call(sim, "StartRound");
            if (!PostStartStaffSetup.TryGetValue(sim, out int count)) return;
            PostStartStaffSetup.Remove(sim);
            Set(sim, "Coins", 10000);
            while ((int)Get(sim, "StaffCount") < count && (bool)Call(sim, "TryHire", Get(sim, "NextHireRole"))) { }
            Set(sim, "Coins", 0);
        }
        private static void Step(object sim, float seconds) { Call(sim, "Step", seconds); }
        private static bool Spawn(object sim, int product, int quantity) { return (bool)Call(sim, "SpawnCustomer", product, quantity); }
        private object NewBalance() { return Activator.CreateInstance(balanceType); }
        private void Tune(object balance, string field, object value) { balanceType.GetField(field, Instance).SetValue(balance, value); }
        private IList Customers(object sim) { return (IList)Get(sim, "Customers"); }


        private static Type Workstations => Type.GetType("HayChoriYPaty.StreetWorkstationLayout, Assembly-CSharp", true);
        private static Type KitchenLayout => Type.GetType("HayChoriYPaty.StreetKitchenLayout, Assembly-CSharp", true);
        private static float WorkerServiceY => (float)Type.GetType("HayChoriYPaty.StreetSceneLayout, Assembly-CSharp", true)
            .GetField("WorkerServiceY", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static Rect WorkBounds(string name) => (Rect)Workstations.GetProperty(name).GetValue(null, null);
        private static Rect ProductBounds(int product, bool premium = false) => (Rect)(premium
            ? Workstations.GetMethod("BoundsForProduct", new[] { typeof(int), typeof(bool) }).Invoke(null, new object[] { product, true })
            : Workstations.GetMethod("BoundsForProduct", new[] { typeof(int) }).Invoke(null, new object[] { product }));
        private static Vector2 PickupPosition(int product, bool premium = false) => (Vector2)(premium
            ? Workstations.GetMethod("PickupPosition", new[] { typeof(int), typeof(bool) }).Invoke(null, new object[] { product, true })
            : Workstations.GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(null, new object[] { product }));
        private static Rect WorkerBounds(string method, Vector2 position) => (Rect)Workstations.GetMethod(method, new[] { typeof(Vector2) }).Invoke(null, new object[] { position });
        private static Rect KitchenFootprint(int product) => (Rect)KitchenLayout.GetMethod("FootprintForProduct").Invoke(null, new object[] { product });
        private static Rect KitchenGrillBounds(int product) => (Rect)KitchenLayout.GetMethod("GrillBoundsForProduct").Invoke(null, new object[] { product });
        private static Vector2 KitchenPickupPosition(int product) => (Vector2)KitchenLayout.GetMethod("PickupPosition").Invoke(null, new object[] { product });
        private static Vector2[] KitchenApproachRoute(int product) => (Vector2[])KitchenLayout.GetMethod("ApproachRoute").Invoke(null, new object[] { product });
        private static Rect KitchenWorkerFootBounds(Vector2 position) => new Rect(position.x - 18f, position.y - 12f, 36f, 12f);

        [TestCase(0)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void StandardWorkstationPickupPresentationKeepsLocalReachDepthOnTallPortrait(int product)
        {
            const float scale = 1.25f;
            Rect prop = ProductBounds(product);
            Vector2 feet = (Vector2)Workstations.GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(null, new object[] { product });
            float offset = (float)Workstations.GetMethod("WorkerPresentationOffset").Invoke(null, new object[] { feet, product, scale, false });
            float drawnFeet = (feet.y - 98f) * scale + 98f + offset;
            Assert.AreEqual(feet.y - prop.y, drawnFeet - prop.y * scale, .001f, "Reach height relative to station top remains fixed");
            Assert.AreEqual(0f, Workstations.GetMethod("WorkerPresentationOffset").Invoke(null, new object[] { feet, product, 1f, false }));
            Rect body = new Rect(feet.x - 45, drawnFeet - 98, 90, 99.5f);
            foreach (string name in new[] { "GrillBounds", "FoodTableBounds", "CocaBounds", "BeerBounds", "FernetBounds" })
            {
                Rect other = WorkBounds(name); if (other == prop) continue; other.y *= scale;
                Assert.IsFalse(body.Overlaps(other));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void StandardWorkstationsKeepFlorestaSizeAndSeparatedAccessibleProps(int level)
        {
            Assert.AreEqual(new Vector2(282, 94), WorkBounds("GrillBounds").size);
            Assert.AreEqual(new Vector2(196, 98), WorkBounds("FoodTableBounds").size);
            object sim = Make(level: level);
            var props = new System.Collections.Generic.List<Rect> { WorkBounds("GrillBounds"), WorkBounds("FoodTableBounds") };
            foreach (int p in new[] { 4, 5, 6 }) if ((bool)Call(sim, "IsProductAvailable", p)) props.Add(ProductBounds(p));
            foreach (Rect prop in props)
            {
                Assert.GreaterOrEqual(prop.xMin, 0); Assert.LessOrEqual(prop.xMax, 540);
                Assert.GreaterOrEqual(prop.yMin, 400); Assert.Less(prop.yMax, 710);
            }
            for (int i = 0; i < props.Count; i++)
            for (int j = i + 1; j < props.Count; j++)
                Assert.IsTrue((bool)Workstations.GetMethod("HasStationClearance").Invoke(null, new object[] { props[i], props[j] }));
            for (int i = 0; i < (int)Get(sim, "ProductCount"); i++)
            {
                int p = (int)Call(sim, "GetAvailableProduct", i);
                Vector2 pickup = (Vector2)Workstations.GetMethod("PickupPosition", new[] { typeof(int) }).Invoke(null, new object[] { p });
                foreach (Rect prop in props) Assert.IsFalse(WorkerBounds("WorkerFootBounds", pickup).Overlaps(prop));
            }
            Rect fitted = (Rect)Workstations.GetMethod("FitArtwork").Invoke(null, new object[] { WorkBounds("GrillBounds"), new Vector2(2170, 725) });
            Assert.AreEqual(2170f / 725f, fitted.width / fitted.height, .0001f, "Never stretch the artwork");
            Assert.That(fitted.width, Is.InRange(281f, 282f)); Assert.AreEqual(94f, fitted.height, .001f);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void StandardWorkstationRoutesReachEveryProductFromEveryCounterWithoutCrossingProps(int level)
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "customerArrivalSeconds", 1000f);
            Tune(balance, "customerPatienceSeconds", 10000f);
            object catalog = Make(level: level, balance: balance);
            foreach (int product in AvailableProducts(catalog))
            for (int column = 0; column < 7; column++)
            {
                object sim = Make(staff: level == 0 ? 1 : 2, level: level, balance: balance); Start(sim);
                object requiredRole = Type.GetType("HayChoriYPaty.StreetSpecialties, Assembly-CSharp", true)
                    .GetMethod("GetRequiredWorkerRole", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { product });
                if ((int)Call(sim, "WorkerCount", requiredRole) == 0)
                {
                    Set(sim, "Coins", 10000);
                    Assert.IsTrue((bool)Call(sim, "TryHire", requiredRole), "Hire the catalog-required worker for product " + product);
                    Set(sim, "Coins", 0);
                }
                Assert.IsTrue(Spawn(sim, product, 2)); object client = Customers(sim)[0];
                Vector2 target = (Vector2)Get(client, "Target"); target.x = 58 + column * 70;
                Set(client, "Target", target); SetWaiting(client);
                client.GetType().GetField("Slot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, column);
                var props = new System.Collections.Generic.List<Rect>();
                foreach (int visibleProduct in AvailableProducts(sim))
                {
                    Rect footprint = KitchenFootprint(visibleProduct);
                    if (!props.Contains(footprint)) props.Add(footprint);
                    if (visibleProduct < 4)
                    {
                        Rect grill = KitchenGrillBounds(visibleProduct);
                        if (!props.Contains(grill)) props.Add(grill);
                    }
                }
                bool picked = false, carried = false, handed = false;
                Vector2[] expectedRoute = KitchenApproachRoute(product);
                for (int frame = 0; frame < 3000 && (int)Get(sim, "Delivered") < 2; frame++)
                {
                    Step(sim, .01f);
                    foreach (object worker in (IList)Get(sim, "Workers"))
                    {
                        int p = (int)Get(worker, "Product"); if (p < 0) continue;
                        Vector2 feet = (Vector2)Get(worker, "Position"); string state = Get(worker, "State").ToString();
                        foreach (Rect prop in props)
                        {
                            Assert.IsFalse(KitchenWorkerFootBounds(feet).Overlaps(prop),
                                "Worker product " + p + " feet at " + feet + " crossed prop " + prop + " in " + state);
                        }
                        if (state == "Pickup") { picked = true; Assert.AreEqual(KitchenPickupPosition(product), feet); }
                        if (state == "ToCounter")
                        {
                            carried = true;
                            int reverseStage = (int)Get(worker, "StationRouteStage");
                            if (reverseStage >= 0)
                                Assert.AreEqual(expectedRoute[reverseStage], Get(worker, "Target"), "Return trip follows the outbound route in reverse.");
                        }
                        if (state == "Handoff")
                        {
                            handed = true;
                            Assert.AreEqual(WorkerServiceY, feet.y, "Every real handoff remains at the player side of the service edge.");
                            Assert.AreEqual(target.x, feet.x, "Every handoff remains aligned to its front-row customer.");
                        }
                    }
                }
                Assert.IsTrue(picked && carried && handed);
                Assert.AreEqual(2, Get(sim, "Delivered")); Assert.AreEqual(10, Get(sim, "Coins"));
                Assert.AreEqual(0, Get(client, "Reserved"));
                foreach (object worker in (IList)Get(sim, "Workers")) Assert.AreEqual("Idle", Get(worker, "State").ToString());
            }
        }

        [Test]
        public void Level3IsLiniersVelezAndOffersOnlyChoriPatyAndCoca()
        {
            object sim = Make(level: 2);
            Assert.AreEqual("Liniers - Velez Sarsfield", ((string[])simType.GetField("LevelNames", BindingFlags.Public | BindingFlags.Static).GetValue(null))[2]);
            Assert.AreEqual(3, Get(sim, "ProductCount"));
            CollectionAssert.AreEqual(new[] { 0, 1, 4 }, new[] { Call(sim, "GetAvailableProduct", 0), Call(sim, "GetAvailableProduct", 1), Call(sim, "GetAvailableProduct", 2) });
            Assert.IsTrue((bool)Call(sim, "IsProductAvailable", 0));
            Assert.IsTrue((bool)Call(sim, "IsProductAvailable", 1));
            Assert.IsTrue((bool)Call(sim, "IsProductAvailable", 4));
            foreach (int product in new[] { 2, 3, 5, 6 }) Assert.IsFalse((bool)Call(sim, "IsProductAvailable", product));
            Assert.AreEqual(65, Get(sim, "Goal"));
            Assert.AreEqual(240f, Get(sim, "TimeRemaining"));
        }

        [Test]
        public void VelezCustomersCanOrderEveryNonemptyOneTwoOrThreeProductCombination()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 7);
            object sim = Make(level: 2, balance: balance);
            var customers = new System.Collections.Generic.List<object>();
            int[] available = { 0, 1, 4 };
            for (int mask = 1; mask < 8; mask++)
            {
                var products = new System.Collections.Generic.List<int>();
                var quantities = new System.Collections.Generic.List<int>();
                for (int bit = 0; bit < available.Length; bit++)
                    if ((mask & (1 << bit)) != 0) { products.Add(available[bit]); quantities.Add(0); }
                Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", products.ToArray(), quantities.ToArray()));
                object customer = Customers(sim)[Customers(sim).Count - 1];
                Assert.AreEqual(products.Count, Get(customer, "OrderLineCount"));
                for (int lineIndex = 0; lineIndex < products.Count; lineIndex++)
                {
                    object line = Call(customer, "GetOrderLine", lineIndex);
                    Assert.AreEqual(products[lineIndex], Get(line, "Product"), "Original catalog order is preserved");
                    Assert.AreEqual(1, Get(line, "Remaining"), "Invalid zero quantities are clamped, never shown as real lines");
                    Assert.That((int)Get(line, "Remaining"), Is.InRange(1, 4));
                }
                customers.Add(customer);
            }
            Assert.AreEqual(7, customers.Count);
        }

        [Test]
        public void VelezAutomaticOrdersUseAllSevenSubsetsAndIndependentOneToFourQuantities()
        {
            var variants = new System.Collections.Generic.HashSet<int>();
            for (int seed = 0; seed < 160; seed++)
            {
                object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "randomSeed", seed);
                Tune(balance, "workerSpeed", 1f); Tune(balance, "customerSpeed", 1f);
                object sim = Make(level: 2, balance: balance); Start(sim); Step(sim, .05f);
                Assert.AreEqual(1, Customers(sim).Count);
                object customer = Customers(sim)[0];
                int count = (int)Get(customer, "OrderLineCount");
                Assert.That(count, Is.InRange(1, 3));
                int mask = 0;
                for (int i = 0; i < count; i++)
                {
                    object line = Call(customer, "GetOrderLine", i);
                    int product = (int)Get(line, "Product");
                    Assert.IsTrue(product == 0 || product == 1 || product == 4);
                    Assert.That((int)Get(line, "Remaining"), Is.InRange(1, 4));
                    mask |= product == 0 ? 1 : product == 1 ? 2 : 4;
                }
                variants.Add(mask);
            }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7 }, variants);
        }

        [Test]
        public void VelezParrilleroOwnsBothFoodLinesAndCocacoleroWorksInParallelOnCoca()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1);
            Tune(balance, "workerSpeed", 100000f); Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(staff: 2, level: 2, balance: balance); Start(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 1, 4 }, new[] { 2, 1, 2 }));
            object customer = Customers(sim)[0]; SetWaiting(customer);
            IList workers = (IList)Get(sim, "Workers");
            object parrillero = workers[0], cocacolero = workers[1];
            Assert.AreEqual("Parrillero", Get(parrillero, "Role").ToString());
            Assert.AreEqual("Cocacolero", Get(cocacolero, "Role").ToString());
            HiddenCall(sim, "Assign", parrillero);
            HiddenCall(sim, "Assign", cocacolero);

            int parrilleroId = (int)Get(parrillero, "Id");
            Assert.AreEqual(Get(customer, "Id"), Get(parrillero, "CustomerId"));
            Assert.AreEqual(Get(customer, "Id"), Get(cocacolero, "CustomerId"));
            Assert.AreEqual(0, Get(parrillero, "Product"));
            Assert.AreEqual(4, Get(cocacolero, "Product"));
            Assert.AreEqual(parrilleroId, Get(Call(customer, "GetOrderLine", 0), "OwnerWorkerId"));
            Assert.AreEqual(parrilleroId, Get(Call(customer, "GetOrderLine", 1), "OwnerWorkerId"));
            Assert.AreEqual(Get(cocacolero, "Id"), Get(Call(customer, "GetOrderLine", 2), "OwnerWorkerId"));
            Assert.AreEqual(0, Get(Call(customer, "GetOrderLine", 1), "Reserved"), "Paty is owned by the Parrillero but waits while its Chori trip completes.");

            for (int i = 0; i < 800 && (int)Get(sim, "Delivered") < 5; i++)
            {
                Step(sim, .02f);
                if ((int)Get(sim, "Delivered") < 5)
                {
                    Assert.IsTrue(Customers(sim).Contains(customer), "The mixed customer must wait for both specialties.");
                    Assert.AreNotEqual("Leaving", Get(customer, "State").ToString());
                }
            }
            Assert.AreEqual(5, Get(sim, "Delivered"));
            Assert.AreEqual(2, Get(sim, "ChoriDelivered"));
            Assert.AreEqual(2, Get(sim, "CocaDelivered"));
            for (int i = 0; i < 3; i++) Assert.AreEqual(0, Get(Call(customer, "GetOrderLine", i), "Remaining"));
            Assert.AreEqual(25, Get(sim, "Coins"));
            Step(sim, .35f); // Receiving finishes only after the final unit; then the customer begins leaving.
            Assert.AreEqual("Leaving", Get(customer, "State").ToString());
        }

        [Test]
        public void VelezThreeProductBubbleRevealsTheHiddenThirdLineWithoutZeroQuantities()
        {
            object customer = SpawnOrder(Make(level: 2), new[] { 0, 1, 4 }, new[] { 3, 2, 1 });
            CollectionAssert.AreEqual(new[] { 0, 1 }, VisibleProducts(customer));
            Assert.AreEqual(1, Get(customer, "HiddenOrderLineCount"));

            Set(Call(customer, "GetOrderLine", 0), "Remaining", 0);
            CollectionAssert.AreEqual(new[] { 1, 4 }, VisibleProducts(customer));
            Assert.AreEqual(0, Get(customer, "HiddenOrderLineCount"));
            for (int i = 0; i < 2; i++)
                Assert.Greater((int)Get(Call(customer, "GetVisibleOrderLine", i), "Remaining"), 0);
        }

        [Test]
        public void VelezTwoWorkersOfSameRoleNeverOwnTheSameCustomer()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 2);
            object sim = Make(staff: 4, level: 2, balance: balance); Start(sim);
            IList workers = (IList)Get(sim, "Workers");
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 1, 4 }, new[] { 2, 1, 2 }));
            object first = Customers(sim)[0];
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 1, 4 }, new[] { 1, 2, 1 }));
            object second = Customers(sim)[1];
            SetWaiting(first); SetWaiting(second);
            object parrilleroA = workers[0], parrilleroB = workers[2];
            HiddenCall(sim, "Assign", parrilleroA); HiddenCall(sim, "Assign", parrilleroB);
            Assert.AreEqual(Get(first, "Id"), Get(parrilleroA, "CustomerId"), "The older eligible first-row customer has priority.");
            Assert.AreEqual(Get(second, "Id"), Get(parrilleroB, "CustomerId"), "A second Parrillero must skip the owned customer and take the next eligible one.");
            HiddenCall(sim, "CancelAssignment", parrilleroA); HiddenCall(sim, "CancelAssignment", parrilleroB);
            object cocaA = workers[1], cocaB = workers[3];
            HiddenCall(sim, "Assign", cocaA); HiddenCall(sim, "Assign", cocaB);
            Assert.AreNotEqual(Get(cocaA, "CustomerId"), Get(cocaB, "CustomerId"));
        }

        [Test]
        public void VelezParrilleroDoesNotServeTheRearRowWhenOnlyFoodTicketIsBehind()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 8);
            object sim = Make(staff: 2, level: 2, balance: balance); Start(sim);
            for (int i = 0; i < 7; i++)
            {
                Assert.IsTrue(Spawn(sim, 4, 1), "The older front-row tickets need Coca only.");
                SetWaiting(Customers(sim)[Customers(sim).Count - 1]);
            }
            Assert.IsTrue(Spawn(sim, 0, 1));
            object rearChori = Customers(sim)[Customers(sim).Count - 1];
            Assert.AreEqual(7, Get(rearChori, "Slot"));
            SetWaiting(rearChori);

            object parrillero = ((IList)Get(sim, "Workers"))[0];
            HiddenCall(sim, "Assign", parrillero);
            Assert.AreEqual("Idle", Get(parrillero, "State").ToString());
            Assert.AreEqual(0, Get(Call(rearChori, "GetOrderLine", 0), "Reserved"));
            Assert.AreEqual(0, Get(Call(rearChori, "GetOrderLine", 0), "OwnerWorkerId"));
        }

        [Test]
        public void VelezSpecialistsNeverClaimProductsFromTheOtherRole()
        {
            object sim = Make(staff: 2, level: 2); Start(sim);
            IList workers = (IList)Get(sim, "Workers");
            object parrillero = workers[0], cocacolero = workers[1];
            object cocaOnly = SpawnOrder(sim, new[] { 4 }, new[] { 1 }); SetWaiting(cocaOnly);
            HiddenCall(sim, "Assign", parrillero);
            Assert.AreEqual("Idle", Get(parrillero, "State").ToString());
            Assert.AreEqual(0, Get(cocaOnly, "Reserved"));

            object patySim = Make(staff: 2, level: 2); Start(patySim);
            object patyOnly = SpawnOrder(patySim, new[] { 1 }, new[] { 1 }); SetWaiting(patyOnly);
            object patyCocacolero = ((IList)Get(patySim, "Workers"))[1];
            HiddenCall(patySim, "Assign", patyCocacolero);
            Assert.AreEqual("Idle", Get(patyCocacolero, "State").ToString());
            Assert.AreEqual(0, Get(patyOnly, "Reserved"));
        }

        [Test]
        public void VelezTimeoutReleasesAllParrilleroOwnershipWithoutCoinsOrDelivery()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "customerPatienceSeconds", .25f);
            object sim = Make(level: 2, balance: balance); Start(sim);
            object customer = SpawnOrder(sim, new[] { 0, 1, 4 }, new[] { 2, 2, 1 }); SetWaiting(customer);
            object worker = ((IList)Get(sim, "Workers"))[0]; HiddenCall(sim, "Assign", worker);
            for (int i = 0; i < 2; i++) Assert.AreEqual(Get(worker, "Id"), Get(Call(customer, "GetOrderLine", i), "OwnerWorkerId"));
            Assert.AreEqual(1, Get(Call(customer, "GetOrderLine", 0), "Reserved"));
            SetField(customer, "Patience", 0f); HiddenCall(sim, "AdvanceCustomers", .01f);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(0, Get(Call(customer, "GetOrderLine", i), "OwnerWorkerId"));
                Assert.AreEqual(0, Get(Call(customer, "GetOrderLine", i), "Reserved"));
            }
            Assert.AreEqual(0, Get(sim, "Delivered")); Assert.AreEqual(0, Get(sim, "Coins")); Assert.AreEqual(0, Get(sim, "CoinsEarned"));
            HiddenCall(sim, "Deliver", worker);
            Assert.AreEqual(0, Get(sim, "Delivered")); Assert.AreEqual(0, Get(sim, "Coins"));
        }

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
            object sim = Make(level: 4, balance: balance); Start(sim); Assert.IsTrue(Spawn(sim, 0, 999));
            for (int i = 0; i < 200; i++) Step(sim, 0.1f);
            Assert.LessOrEqual((int)Get(sim, "Delivered"), 999);
            IList customers = Customers(sim);
            Assert.AreEqual(999 - (int)Get(sim, "Delivered"), customers.Count == 0 ? 0 : (int)Get(customers[0], "Remaining"));
        }

        [Test]
        public void MultipleWorkersNeverReserveMoreUnitsThanRemain()
        {
            object balance = NewBalance(); Tune(balance,"levelGoals",new[]{1,1,1,1,1}); Tune(balance, "maxCustomers", 1);
            object sim = Make(staff: 5, level: 1, balance: balance); Start(sim); Spawn(sim, 0, 1);
            object customer = Customers(sim)[0]; Step(sim, 8f);
            // The full turn may serve later arrivals; verify this original one-unit order.
            Assert.AreEqual(0, Get(customer, "Remaining"));
            Assert.AreEqual(0, Get(customer, "Reserved"));
        }

        [Test]
        public void ChicagoParrillerosStayOnThePlayerSideOfTheCounterForBothProducts()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 1); Tune(balance, "workerSpeed", 100000f);
            Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(level: 1, balance: balance);
            Start(sim);
            Set(sim, "Coins", 1000);
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("Cocacolero")));
            IList workers = (IList)Get(sim, "Workers");
            float handoffY = (float)simType.GetField("ChicagoCounterServiceY", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Assert.AreEqual(WorkerServiceY, handoffY);
            foreach (object worker in workers)
                Assert.AreEqual(handoffY, ((Vector2)Get(worker, "Position")).y, "Chicago staff start in front of, not inside, the counter");

            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1, 4, 1));
            bool sawHandoff = false;
            for (int frame = 0; frame < 1000 && (int)Get(sim, "Delivered") < 2; frame++)
            {
                Step(sim, .02f);
                foreach (object worker in workers)
                {
                    Vector2 position = (Vector2)Get(worker, "Position");
                    Assert.That(position.y, Is.GreaterThanOrEqualTo(handoffY - .001f), "A worker must not cross the counter service boundary");
                    if (Get(worker, "State").ToString() == "Handoff")
                    {
                        Assert.AreEqual(handoffY, position.y);
                        sawHandoff = true;
                    }
                }
            }
            Assert.IsTrue(sawHandoff);
            Assert.AreEqual(2, Get(sim, "Delivered"));
            Assert.AreEqual(1, Get(sim, "ChoriDelivered"));
            Assert.AreEqual(1, Get(sim, "CocaDelivered"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryLevelConstructionSelectionAndStartUseFreshAttemptBaseline(int level)
        {
            object balance = NewBalance();
            object sim = Activator.CreateInstance(simType, new object[] { balance, level, 5f, 123, 4, 7 });
            AssertFreshAttempt(sim);
            Start(sim);
            AssertFreshAttempt(sim);
            object ready = Make(level: 0, balance: balance);
            Assert.IsTrue((bool)Call(ready, "SelectLevel", level));
            AssertFreshAttempt(ready);
        }

        private void AssertFreshAttempt(object sim)
        {
            Assert.AreEqual(1, Get(sim, "ParrilleroCount"));
            Assert.AreEqual(0, Get(sim, "CocacoleroCount"));
            Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
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
        public void ZeroPriceOverrideIsIgnoredAndEveryDeliveryEarnsFixedFiveCoins()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "levelProductCounts", new[] { 1, 1, 5, 6, 7 });
            object sim = Make(level: 1, balance: balance); Call(sim, "SetPrice", 0f);
            Assert.AreEqual(5f, Get(sim, "Price"));
            Start(sim); Spawn(sim, 0, 1); Step(sim, 8f);
            Assert.Greater((int)Get(sim, "Delivered"), 0);
            Assert.AreEqual((int)Get(sim, "Delivered") * 5, Get(sim, "Coins"));
            Assert.AreEqual((int)Get(sim, "Delivered") * 5, Get(sim, "CoinsEarned"));
        }

        [Test]
        public void WorkerKeepsOneCustomerUntilItsWholeProductPartIsComplete()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 2);
            Tune(balance, "workerSpeed", 100000f); Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(level: 4, balance: balance); Start(sim);
            Spawn(sim, 0, 999); Spawn(sim, 1, 1);
            IList customers = Customers(sim); object firstCustomer = customers[0], secondCustomer = customers[1];
            SetWaiting(firstCustomer); SetWaiting(secondCustomer);
            object worker = ((IList)Get(sim, "Workers"))[0];
            for (int i = 0; i < 60; i++)
            {
                Step(sim, .05f);
                if ((int)Get(firstCustomer, "Remaining") > 0 && Get(worker, "State").ToString() != "Idle")
                    Assert.AreEqual(Get(firstCustomer, "Id"), Get(worker, "CustomerId"), "The assigned worker cannot switch after a single unit.");
            }
            Assert.Less((int)Get(firstCustomer, "Remaining"), 999);
            Assert.Greater((int)Get(firstCustomer, "Remaining"), 0);
            Assert.AreEqual(1, Get(secondCustomer, "Remaining"), "FIFO keeps the second client waiting until the first ticket is done.");
        }

        [Test]
        public void ChicagoBothSpecialistsReachNewSidePickupsAndReturnWithoutCrossingProps()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1);
            Tune(balance, "customerArrivalSeconds", 1000f);
            object sim = Make(staff: 2, level: 1, balance: balance); Start(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 2, 4, 2));
            var props = new System.Collections.Generic.List<Rect>();
            foreach (int product in AvailableProducts(sim))
            {
                Rect footprint = KitchenFootprint(product);
                if (!props.Contains(footprint)) props.Add(footprint);
                if (product < 4)
                {
                    Rect grill = KitchenGrillBounds(product);
                    if (!props.Contains(grill)) props.Add(grill);
                }
            }
            bool[] picked = new bool[2], carried = new bool[2], handed = new bool[2];
            for (int frame = 0; frame < 3000 && (int)Get(sim, "Delivered") < 4; frame++)
            {
                Step(sim, .01f);
                foreach (object worker in (IList)Get(sim, "Workers"))
                {
                    int role = Get(worker, "Role").ToString() == "Cocacolero" ? 1 : 0;
                    Vector2 position = (Vector2)Get(worker, "Position");
                    foreach (Rect prop in props)
                    {
                        Assert.IsFalse(KitchenWorkerFootBounds(position).Overlaps(prop), "Worker feet crossed a kitchen prop: " + Get(worker, "State"));
                    }
                    string state = Get(worker, "State").ToString();
                    if (state == "Pickup")
                    {
                        picked[role] = true;
                        Assert.AreEqual(KitchenPickupPosition(role == 0 ? 0 : 4), position);
                        Assert.AreEqual(role == 0 ? 0 : 4, Get(worker, "Product"));
                    }
                    if (state == "ToCounter") carried[role] = true;
                    if (state == "Handoff") { handed[role] = true; Assert.AreEqual(WorkerServiceY, position.y); }
                }
            }
            Assert.AreEqual(4, Get(sim, "Delivered")); Assert.AreEqual(20, Get(sim, "Coins"));
            CollectionAssert.AreEqual(new[] { true, true }, picked);
            CollectionAssert.AreEqual(new[] { true, true }, carried);
            CollectionAssert.AreEqual(new[] { true, true }, handed);
        }

        private object Role(string name) { return Enum.Parse(Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true), name); }

public void HireParrilleroIncreasesOnlyParrilleroCount()
        {
            var sim = Make(level: 1); Start(sim); Set(sim,"Coins",10000);
            Assert.IsTrue((bool)Call(sim,"TryHire",Role("Parrillero")));
            Assert.AreEqual(2,Get(sim,"ParrilleroCount")); Assert.AreEqual(0,Get(sim,"CocacoleroCount"));
            Assert.AreEqual(9985,Get(sim,"Coins"));
        }
public void HireCocacoleroIncreasesOnlyCocacoleroCount()
        {
            var sim = Make(level: 1); Start(sim); Set(sim,"Coins",10000);
            Assert.IsTrue((bool)Call(sim,"TryHire",Role("Cocacolero")));
            Assert.AreEqual(1,Get(sim,"ParrilleroCount")); Assert.AreEqual(1,Get(sim,"CocacoleroCount"));
            Assert.AreEqual(9985,Get(sim,"Coins"));
            Assert.AreEqual(30,Get(sim,"CocacoleroHireCost"));
        }
public void ParrilleroAndCocacoleroUseTheSameCurveWithRoleLocalTiers()
        {
            var sim = Make(level: 1); Start(sim); Set(sim,"Coins",10000);
            Call(sim,"TryHire",Role("Parrillero"));
            Assert.AreEqual(30,Get(sim,"ParrilleroHireCost")); Assert.AreEqual(15,Get(sim,"CocacoleroHireCost"));
            Call(sim,"TryHire",Role("Cocacolero"));
            Assert.AreEqual(30,Get(sim,"CocacoleroHireCost"));
            Call(sim,"TryHire",Role("Cocacolero"));
            Assert.AreEqual(60,Get(sim,"CocacoleroHireCost")); Assert.AreEqual(30,Get(sim,"ParrilleroHireCost"));
            Set(sim,"Coins",59); Assert.IsFalse((bool)Call(sim,"TryHire",Role("Cocacolero")));
            Assert.AreEqual(59,Get(sim,"Coins")); Assert.AreEqual(2,Get(sim,"CocacoleroCount"));
        }
public void ParrilleroAndCocacoleroMaxAreIndependent()
        {
            var sim = Make(level: 1); Start(sim); Set(sim,"Coins",50000);
            for(int i=0;i<4;i++) Assert.IsTrue((bool)Call(sim,"TryHire",Role("Parrillero")));
            Assert.AreEqual(0,Get(sim,"ParrilleroHireCost")); Assert.IsFalse((bool)Get(sim,"CanHireParrillero"));
            Assert.IsTrue((bool)Get(sim,"CanHireCocacolero"));
            int[] cocaCosts = { 15, 30, 60, 100 };
            for(int i=0;i<cocaCosts.Length;i++)
            {
                Assert.AreEqual(cocaCosts[i],Get(sim,"CocacoleroHireCost"));
                Assert.IsTrue((bool)Call(sim,"TryHire",Role("Cocacolero")));
            }
            Assert.AreEqual(5,Get(sim,"ParrilleroCount")); Assert.AreEqual(4,Get(sim,"CocacoleroCount"));
            Assert.AreEqual(5,Call(sim,"MaxWorkersForRole", Role("Cocacolero")));
            Assert.AreEqual(0,Get(sim,"CocacoleroHireCost")); Assert.IsFalse((bool)Get(sim,"CanHireCocacolero"));
        }
        [Test]
        public void RoleCapsAndPostStartHiringPreserveIndependentComposition()
        {
            var balance=NewBalance(); Tune(balance,"maxParrilleros",2); Tune(balance,"maxCocacoleros",3);
            var sim=Make(level:1,balance:balance);
            Start(sim); Set(sim,"Coins",10000);
            for (int i=0;i<10;i++) Call(sim,"TryHire",Role(i < 2 ? "Parrillero" : "Cocacolero"));
            Assert.AreEqual(2,Get(sim,"ParrilleroCount")); Assert.AreEqual(3,Get(sim,"CocacoleroCount"));
            var floresta=Make(coins:10000); Assert.IsFalse((bool)Call(floresta,"TryHire",Role("Cocacolero")));
        }

        [Test]
        public void ChicagoParrilleroCompletesFourChorisBeforeMovingToSecondAndThirdCustomer()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 3); Tune(balance, "workerSpeed", 100000f);
            Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(level: 1, balance: balance); Start(sim);
            Assert.IsTrue(Spawn(sim, 0, 4)); Assert.IsTrue(Spawn(sim, 0, 2)); Assert.IsTrue(Spawn(sim, 0, 1));
            IList customers = Customers(sim); object first = customers[0], second = customers[1], third = customers[2];
            foreach (object customer in customers) SetWaiting(customer);
            object worker = ((IList)Get(sim, "Workers"))[0];
            for (int i = 0; i < 1200 && (int)Get(sim, "Delivered") < 7; i++)
            {
                Step(sim, .02f);
                if ((int)Get(first, "Remaining") > 0 && Get(worker, "State").ToString() != "Idle")
                    Assert.AreEqual(Get(first, "Id"), Get(worker, "CustomerId"), "Parrillero must carry all four units for the first customer.");
                if ((int)Get(second, "Remaining") > 0 && (int)Get(first, "Remaining") > 0)
                    Assert.AreEqual(0, Get(second, "Reserved"), "A later client cannot receive a service reservation while the oldest one's part is incomplete.");
            }
            Assert.AreEqual(0, Get(first, "Remaining")); Assert.AreEqual(0, Get(second, "Remaining")); Assert.AreEqual(0, Get(third, "Remaining"));
            Assert.AreEqual(7, Get(sim, "ChoriDelivered")); Assert.AreEqual(7, Get(sim, "Delivered"));
        }

        [Test]
        public void ChicagoCocacoleroCompletesFourCocasBeforeMovingToNextCustomer()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 2); Tune(balance, "workerSpeed", 100000f);
            Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(staff: 2, level: 1, balance: balance); Start(sim);
            Assert.IsTrue(Spawn(sim, 4, 4)); Assert.IsTrue(Spawn(sim, 4, 2));
            IList customers = Customers(sim); object first = customers[0], second = customers[1];
            SetWaiting(first); SetWaiting(second);
            object cocaWorker = null;
            foreach (object worker in (IList)Get(sim, "Workers"))
                if (Get(worker, "Role").ToString() == "Cocacolero") cocaWorker = worker;
            Assert.NotNull(cocaWorker);
            for (int i = 0; i < 1200 && (int)Get(first, "Remaining") > 0; i++)
            {
                Step(sim, .02f);
                if ((int)Get(first, "Remaining") > 0 && Get(cocaWorker, "State").ToString() != "Idle")
                    Assert.AreEqual(Get(first, "Id"), Get(cocaWorker, "CustomerId"), "Cocacolero must complete the first Coca order before taking the next client.");
                Assert.AreEqual(2, Get(second, "Remaining"), "Second Coca order waits for the first customer's four Coca deliveries.");
            }
            Assert.AreEqual(0, Get(first, "Remaining")); Assert.AreEqual(4, Get(sim, "CocaDelivered"));
            for (int i = 0; i < 1200 && (int)Get(second, "Remaining") > 0; i++) Step(sim, .02f);
            Assert.AreEqual(0, Get(second, "Remaining")); Assert.AreEqual(6, Get(sim, "CocaDelivered"));
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
            object sim = Make(); Start(sim); Set(sim, "Coins", 500); // Simulated mid-round income.
            Assert.IsTrue((bool)Call(sim, "TryHire")); Assert.AreEqual(2, Get(sim, "StaffCount"));
            Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed")); Assert.AreEqual(1, Get(sim, "SpeedLevel"));
            Assert.AreEqual(480, Get(sim, "Coins"));
        }

        [Test]
        public void EarnedCoinsCountActualIncomeNotRemainingBalanceAndResetOnNewAttempt()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1);
            Tune(balance, "workerSpeed", 100000f); Tune(balance, "customerSpeed", 100000f);
            Tune(balance, "pickupSeconds", .01f);
            object sim = Make(balance: balance); Start(sim); Assert.IsTrue(Spawn(sim, 0, 4));
            for (int i = 0; i < 300 && (int)Get(sim, "Delivered") < 4; i++) Step(sim, .05f);
            Assert.AreEqual(4, Get(sim, "Delivered"));
            Assert.AreEqual(20, Get(sim, "CoinsEarned")); Assert.AreEqual(20, Get(sim, "Coins"));
            Assert.IsTrue((bool)Call(sim, "TryHire")); Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.AreEqual(0, Get(sim, "Coins")); Assert.AreEqual(20, Get(sim, "CoinsEarned"));
            Start(sim);
            Assert.AreEqual(0, Get(sim, "CoinsEarned")); Assert.AreEqual(0, Get(sim, "Coins"));
        }

        [Test]
        public void WinUnlocksNextLevelAndCreatesReadyNextRound()
        {
            object balance = NewBalance(); Tune(balance, "levelGoals", new[] { 1, 1, 3, 4, 5 });
            object sim = Make(level: 1, balance: balance); Start(sim);
            Set(sim, "Coins", 500);
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("Cocacolero"))); Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.AreEqual(2, Get(sim, "StaffCount")); Assert.AreEqual(1, Get(sim, "SpeedLevel"));
            Spawn(sim, 0, 1); Spawn(sim, 4, 1);
            for (int i = 0; i < 100 && Get(sim, "Phase").ToString() == "Playing"; i++) Step(sim, 0.1f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.Greater((int)Get(sim, "Coins"), 0);
            Assert.IsTrue((bool)Call(sim, "NextLevel"));
            Assert.AreEqual("Ready", Get(sim, "Phase").ToString()); Assert.AreEqual(2, Get(sim, "LevelIndex"));
            Assert.AreEqual(0, Get(sim, "Coins"));
            Assert.AreEqual(1, Get(sim, "StaffCount")); Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.AreEqual(1f, Get(sim, "WorkRate"));
            Assert.AreEqual(15, Get(sim, "HireCost")); Assert.AreEqual(5, Get(sim, "SpeedCost"));
            IList workers = (IList)Get(sim, "Workers");
            Assert.AreEqual("Parrillero", Get(workers[0], "Role").ToString());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryLevelStartAndRestartDiscardCoinsButStillEarnIncome(int level)
        {
            object sim = Make(coins: 123, level: level); Start(sim);
            Assert.AreEqual(0, Get(sim, "Coins"));
            Spawn(sim, 0, 1); Step(sim, 8f);
            Assert.Greater((int)Get(sim, "Coins"), 0, "Real handoffs still earn coins within the attempt");
            Start(sim);
            Assert.AreEqual(0, Get(sim, "Coins"));
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
        public void LegacyInflatedSaveResetsStartingTeamAndSpeedButPreservesProgress()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object legacy = Activator.CreateInstance(saveType);
            saveType.GetField("version").SetValue(legacy, 1);
            saveType.GetField("staff").SetValue(legacy, 8);
            saveType.GetField("speed").SetValue(legacy, 65);
            saveType.GetField("unlockedLevel").SetValue(legacy, 2);
            saveType.GetField("coins").SetValue(legacy, 42);
            saveType.GetField("price").SetValue(legacy, 17f);
            saveType.GetField("prices").SetValue(legacy, new[] { 5f, 17f, 18f, 19f, 5f, 8f, 9f });

            object migrated = gameType.GetMethod("MigrateSaveData", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new[] { legacy });
            Assert.AreEqual(3, saveType.GetField("version").GetValue(migrated));
            Assert.AreEqual(1, saveType.GetField("staff").GetValue(migrated));
            Assert.AreEqual(0, saveType.GetField("speed").GetValue(migrated));
            Assert.AreEqual(2, saveType.GetField("unlockedLevel").GetValue(migrated));
            Assert.AreEqual(42, saveType.GetField("coins").GetValue(migrated));
            Assert.AreEqual(17f, saveType.GetField("price").GetValue(migrated));
            Assert.AreEqual(17f, ((float[])saveType.GetField("prices").GetValue(migrated))[1]);
        }


        [Test]
        public void SevenProductCatalogUsesFixedFiveAcrossEveryLevelAndRejectsEdits()
        {
            object balance = NewBalance(); Tune(balance, "levelProductCounts", new[] { 1, 1, 5, 6, 7 });
            for (int level = 0; level < 5; level++)
            {
                object sim = Make(level: level, balance: balance);
                Assert.IsFalse((bool)Get(sim, "CanEditPrices"));
                Assert.AreEqual(5f, Get(sim, "Price"));
                for (int product = 0; product < 7; product++)
                {
                    Call(sim, "SetProductPrice", product, 0f);
                    Call(sim, "SetProductPrice", product, 60f);
                    Assert.AreEqual(5f, Call(sim, "GetProductPrice", product), "Level " + level + ", product " + product);
                }
                Call(sim, "SetPrice", -1f);
                Assert.AreEqual(5f, Get(sim, "Price"));
            }
            Assert.AreEqual(7, ((string[])simType.GetField("ProductNames", BindingFlags.Public | BindingFlags.Static).GetValue(null)).Length);
        }

        [Test]
        public void LegacyPerProductPriceEditCannotChangeFixedRevenue()
        {
            object balance=NewBalance();Tune(balance,"maxCustomers",1);Tune(balance,"levelGoals",new[]{1,1,1,1,1});
            object sim=Make(level:2,balance:balance);Call(sim,"SetProductPrice",1,17f);
            Start(sim);Spawn(sim,1,1);Step(sim,8f);
            Assert.AreEqual(5,Get(sim,"Coins"));Assert.AreEqual(5,Get(sim,"CoinsEarned"));Assert.AreEqual(5f,Call(sim,"GetProductPrice",1));
        }
        [Test]
        public void OwnedRandomIgnoresUnityGlobalRandomAndCatalogConfigIsClamped()
        {
            object b=NewBalance();Tune(b,"levelProductCounts",new[]{99,99,99,99,99});
            object a=Make(level:4,balance:b), c=Make(level:4,balance:b);Start(a);Start(c);
            UnityEngine.Random.InitState(111);Step(a,.3f);UnityEngine.Random.InitState(222);Step(c,.3f);
            Assert.AreEqual(7,Get(a,"ProductCount"));Assert.AreEqual(Get(Customers(a)[0],"Product"),Get(Customers(c)[0],"Product"));
        }

        [Test]
        public void LegacyPriceEditsDoNotChangeDemandOrGeneratedOrders()
        {
            object fixedPrices=Make(level:3), legacyEdited=Make(level:3);
            for(int product=0;product<7;product++)Call(legacyEdited,"SetProductPrice",product,60f);
            Assert.AreEqual(Get(fixedPrices,"DemandFraction"),Get(legacyEdited,"DemandFraction"));
            Start(fixedPrices);Start(legacyEdited);Step(fixedPrices,3f);Step(legacyEdited,3f);
            Assert.AreEqual(Customers(fixedPrices).Count,Customers(legacyEdited).Count);
            for(int i=0;i<Customers(fixedPrices).Count;i++)
            {
                Assert.AreEqual(Get(Customers(fixedPrices)[i],"Product"),Get(Customers(legacyEdited)[i],"Product"));
                Assert.AreEqual(Get(Customers(fixedPrices)[i],"Remaining"),Get(Customers(legacyEdited)[i],"Remaining"));
            }
        }

        [Test]
        public void ChicagoKeepsSevenStableProductIdsButOffersOnlyChoriAndBottleCoca()
        {
            object sim = Make(level: 1);
            Assert.AreEqual(2, Get(sim, "ProductCount"));
            Assert.IsTrue((bool)Call(sim, "IsProductAvailable", 0));
            Assert.IsFalse((bool)Call(sim, "IsProductAvailable", 1));
            Assert.IsTrue((bool)Call(sim, "IsProductAvailable", 4));
            Assert.AreEqual(0, Call(sim, "GetAvailableProduct", 0));
            Assert.AreEqual(4, Call(sim, "GetAvailableProduct", 1));
            Assert.IsFalse(Spawn(sim, 1, 1));
            Assert.IsTrue(Spawn(sim, 4, 4));
            Assert.AreEqual("Coca 600 ml", ((string[])simType.GetField("ProductNames", BindingFlags.Public | BindingFlags.Static).GetValue(null))[4]);
        }

        [Test]
        public void ChicagoAutomaticOrdersIncludeThreeVariantsWithIndependentOneToFourQuantities()
        {
            object balance = NewBalance();
            Tune(balance, "customerArrivalSeconds", .1f);
            Tune(balance, "customerSpeed", 100000f);
            Tune(balance, "workerSpeed", 1f);
            Tune(balance, "customerPatienceSeconds", 1000f);
            object sim = Make(level: 1, balance: balance);
            Start(sim); Step(sim, 4f);
            bool choriOnly = false, cocaOnly = false, combined = false;
            foreach (object customer in Customers(sim))
            {
                int product = (int)Get(customer, "Product"), quantity = (int)Get(customer, "Remaining");
                int secondProduct = (int)Get(customer, "SecondaryProduct"), secondQuantity = (int)Get(customer, "SecondaryRemaining");
                Assert.That(quantity, Is.InRange(1, 4)); Assert.IsTrue(product == 0 || product == 4);
                if (secondProduct < 0) { choriOnly |= product == 0; cocaOnly |= product == 4; }
                else
                {
                    combined = true;
                    Assert.AreEqual(0, product); Assert.AreEqual(4, secondProduct);
                    Assert.That(secondQuantity, Is.InRange(1, 4));
                }
            }
            Assert.IsTrue(choriOnly); Assert.IsTrue(cocaOnly); Assert.IsTrue(combined);
        }

        [Test]
        public void ChicagoSpecialistsServeTheirOwnMixedOrderPartsInParallelAndCustomerWaitsForBoth()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 1); Tune(balance, "levelGoals", new[] { 200, 100, 65, 85, 110 });
            Tune(balance, "workerSpeed", 100000f); Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(staff: 2, level: 1, balance: balance);
            Call(sim, "SetProductPrice", 0, 5f); Call(sim, "SetProductPrice", 4, 7f);
            Start(sim); Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 2, 4, 3));
            object order = Customers(sim)[0]; SetWaiting(order);
            IList workers = (IList)Get(sim, "Workers");
            foreach (object worker in workers) HiddenCall(sim, "Assign", worker);
            Assert.AreEqual(Get(order, "Id"), Get(workers[0], "CustomerId"));
            Assert.AreEqual(Get(order, "Id"), Get(workers[1], "CustomerId"));
            Assert.AreNotEqual(Get(workers[0], "Product"), Get(workers[1], "Product"));
            Assert.AreEqual(Get(workers[0], "Id"), Get(Call(order, "GetOrderLine", 0), "OwnerWorkerId"));
            Assert.AreEqual(Get(workers[1], "Id"), Get(Call(order, "GetOrderLine", 1), "OwnerWorkerId"));
            bool cocaBeforeChoriDone = false;
            for (int i = 0; i < 1000 && (int)Get(sim, "Delivered") < 5; i++)
            {
                Step(sim, .02f);
                if ((int)Get(order, "Remaining") > 0 && (int)Get(order, "SecondaryRemaining") < 3) cocaBeforeChoriDone = true;
                Assert.AreNotEqual("Leaving", Get(order, "State").ToString(), "Mixed customer cannot leave with either half pending.");
            }
            Assert.IsTrue(cocaBeforeChoriDone, "The Coca specialist starts the same mixed customer before chori is complete.");
            Assert.AreEqual(5, Get(sim, "Delivered"));
            Assert.AreEqual(2, Get(sim, "ChoriDelivered")); Assert.AreEqual(3, Get(sim, "CocaDelivered"));
            Assert.AreEqual(0, Get(order, "Remaining")); Assert.AreEqual(0, Get(order, "SecondaryRemaining"));
            Assert.AreEqual(25, Get(sim, "Coins")); Assert.AreEqual(25, Get(sim, "CoinsEarned")); Assert.AreEqual(0, Get(order, "Reserved"));
            Step(sim, .5f);
            Assert.AreEqual("Leaving", Get(order, "State").ToString());
        }

        [Test]
        public void ChicagoVictoryRequiresBothProductTargetsAndRoundRestartClearsThem()
        {
            object defaults = Make(level: 1);
            Assert.AreEqual(200, Get(defaults, "Goal"), "The Chicago per-product target is 200 by default");
            Assert.AreEqual(180f, Get(defaults, "TimeRemaining"), "Nueva Chicago has a three-minute timer by default");
            Assert.AreEqual(5f, Call(defaults, "GetProductPrice", 0), "Chori starts at $5");
            Assert.AreEqual(5f, Call(defaults, "GetProductPrice", 4), "Bottled Coca starts at $5");
            Assert.IsFalse((bool)Get(defaults, "CanEditPrices"), "Product prices are fixed in every location");

            object balance = NewBalance(); Tune(balance, "levelGoals", new[] { 200, 2, 65, 85, 110 });
            object sim = Make(level: 1, balance: balance); Start(sim);
            Set(sim, "ChoriDelivered", 2);
            Assert.IsFalse((bool)Get(sim, "GoalReached"), "Reaching only the chori target cannot satisfy the Coca target");
            Set(sim, "CocaDelivered", 2);
            Assert.IsTrue((bool)Get(sim, "GoalReached"), "Both per-product targets are required");

            Call(sim, "StartRound");
            Assert.AreEqual(0, Get(sim, "ChoriDelivered")); Assert.AreEqual(0, Get(sim, "CocaDelivered"));
            Assert.IsFalse((bool)Get(sim, "GoalReached"));
        }

        [Test]
        public void FlorestaWinsAtExactlyTwoHundredRealSalesBeforeDeadlineAndFreezes()
        {
            object b = NewBalance();
            // Accelerate travel only to exercise the real handoff path, not production pacing.
            Tune(b, "workerSpeed", 100000f); Tune(b, "customerSpeed", 100000f); Tune(b, "pickupSeconds", .01f);
            object sim = Make(balance: b); Start(sim); Set(sim, "Coins", 10000);
            Assert.AreEqual(200, Get(sim, "Goal")); Assert.AreEqual(120f, Get(sim, "TimeRemaining"));
            for (int i = 0; i < 4; i++) Assert.IsTrue((bool)Call(sim, "TryHire"));
            for (int i = 0; i < 9; i++) Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed"));
            int opening = (int)Get(sim, "Coins");
            for (int i = 0; i < 3600 && Get(sim, "Phase").ToString() == "Playing"; i++) Step(sim, .05f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString()); Assert.AreEqual(200, Get(sim, "Delivered"));
            Assert.Greater((float)Get(sim, "TimeRemaining"), 0f); Assert.AreEqual(opening + 1000, Get(sim, "Coins"));
            float elapsed = (float)Get(sim, "Elapsed"); Step(sim, 10f);
            Assert.AreEqual(elapsed, Get(sim, "Elapsed")); Assert.AreEqual(200, Get(sim, "Delivered"));
            Assert.AreEqual(opening + 1000, Get(sim, "Coins"));
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
            // Chicago needs one worker per specialty; limit arrivals and speed up both routes for determinism.
            if (level == 1)
            {
                Tune(b, "maxCustomers", 2); Tune(b, "workerSpeed", 100000f);
                Tune(b, "customerSpeed", 100000f); Tune(b, "pickupSeconds", .01f);
            }
            object sim = Make(staff: level == 1 ? 2 : 1, level: level, balance: b); Start(sim); Spawn(sim, 0, 1);
            if (level == 1) Spawn(sim, 4, 1);
            Step(sim, 8f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.Greater((float)Get(sim, "TimeRemaining"), 0f);
        }


        [Test]
        public void AllLevelsNormalizeLegacyPricesAndKeepFlorestaResetRules()
        {
            object sim = Activator.CreateInstance(simType, new object[] { NewBalance(), 0, 60f, 123, 3, 4 });
            Assert.AreEqual(5f, Get(sim, "Price"));
            Assert.IsFalse((bool)Get(sim, "CanEditPrices"));
            Call(sim, "SetPrice", 0f); Call(sim, "SetProductPrice", 0, 60f);
            Assert.AreEqual(5f, Call(sim, "GetProductPrice", 0));
            Assert.AreEqual(123, Get(sim, "Coins")); Assert.AreEqual(1, Get(sim, "StaffCount"));
            Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1f).Within(.0001f));
            Call(sim, "SelectLevel", 1); Call(sim, "SetPrice", 17f);
            Assert.AreEqual(0, Get(sim, "Coins"));
            Assert.IsFalse((bool)Get(sim, "CanEditPrices")); Assert.AreEqual(5f, Get(sim, "Price"));
            Call(sim, "SelectLevel", 0); Assert.AreEqual(5f, Get(sim, "Price"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryLevelUsesExactUpgradeTablesAndStopsAtLastTier(int level)
        {
            object sim = Make(level: level); Start(sim); Set(sim, "Coins", 10000);
            int[] speedCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
            int[] hireCosts = { 15, 30, 60, 100 };
            int remaining = 10000;
            for (int i = 0; i < speedCosts.Length; i++)
            {
                Assert.AreEqual(speedCosts[i], Get(sim, "SpeedCost"));
                Set(sim, "Coins", speedCosts[i] - 1);
                Assert.IsFalse((bool)Call(sim, "TryUpgradeSpeed"));
                Assert.AreEqual(i, Get(sim, "SpeedLevel")); Assert.AreEqual(speedCosts[i] - 1, Get(sim, "Coins"));
                Set(sim, "Coins", remaining);
                Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed")); remaining -= speedCosts[i];
                Assert.AreEqual(remaining, Get(sim, "Coins"));
                Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1f + .1f * (i + 1)).Within(.0001f));
            }
            Assert.AreEqual(9, Get(sim, "SpeedLevel")); Assert.AreEqual(0, Get(sim, "SpeedCost"));
            Assert.IsFalse((bool)Get(sim, "CanUpgradeSpeed")); Assert.IsFalse((bool)Call(sim, "TryUpgradeSpeed"));
            Assert.AreEqual(remaining, Get(sim, "Coins"));
            for (int i = 0; i < hireCosts.Length; i++)
            {
                Assert.AreEqual(hireCosts[i], Get(sim, "HireCost"));
                Set(sim, "Coins", hireCosts[i] - 1);
                Assert.IsFalse((bool)Call(sim, "TryHire"));
                Assert.AreEqual(i + 1, Get(sim, "StaffCount")); Assert.AreEqual(hireCosts[i] - 1, Get(sim, "Coins"));
                Set(sim, "Coins", remaining);
                Assert.IsTrue((bool)Call(sim, "TryHire")); remaining -= hireCosts[i];
                Assert.AreEqual(i + 2, Get(sim, "StaffCount")); Assert.AreEqual(remaining, Get(sim, "Coins"));
            }
            Assert.AreEqual(5, Get(sim, "StaffCount")); Assert.AreEqual(0, Get(sim, "HireCost"));
            Assert.IsFalse((bool)Get(sim, "CanHire")); Assert.IsFalse((bool)Call(sim, "TryHire"));
            Assert.AreEqual(remaining, Get(sim, "Coins"));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CocacoleroUsesTheSharedHireCurveFromZeroToMax(int level)
        {
            object sim = Make(level: level, coins: 1000); Start(sim);
            int[] costs = { 15, 30, 60, 100 };
            int remaining = 1000;
            for (int i = 0; i < costs.Length; i++)
            {
                Assert.AreEqual(costs[i], Get(sim, "CocacoleroHireCost"));
                Set(sim, "Coins", costs[i] - 1);
                Assert.IsFalse((bool)Call(sim, "TryHire", Role("Cocacolero")));
                Set(sim, "Coins", remaining);
                Assert.IsTrue((bool)Call(sim, "TryHire", Role("Cocacolero")));
                remaining -= costs[i];
                Assert.AreEqual(i + 1, Get(sim, "CocacoleroCount"));
                Assert.AreEqual(remaining, Get(sim, "Coins"));
            }
            Assert.AreEqual(0, Get(sim, "CocacoleroHireCost"));
            Assert.IsFalse((bool)Get(sim, "CanHireCocacolero"));
            Assert.AreEqual(4, Get(sim, "CocacoleroCount"));
            Assert.AreEqual(5, Get(sim, "StaffCount"));
            int coinsAtMax = (int)Get(sim, "Coins");
            Assert.IsFalse((bool)Call(sim, "TryHire", Role("Cocacolero")));
            Assert.AreEqual(coinsAtMax, Get(sim, "Coins"));
            Assert.AreEqual(5, Call(sim, "MaxWorkersForRole", Role("Cocacolero")));
        }

[Test]
        public void LevelsSelectUpgradeProfilesWithoutReadingLegacyTables()
        {
            object balance = NewBalance();
            Type profileType = Type.GetType("HayChoriYPaty.StreetUpgradeCostProfile, Assembly-CSharp", true);
            Array defaults = (Array)balanceType.GetField("upgradeCostProfiles", Instance).GetValue(balance);
            object custom = Activator.CreateInstance(profileType);
            profileType.GetField("profileId", Instance).SetValue(custom, "CUSTOM_LEVEL_PROFILE");
            profileType.GetField("hireCosts", Instance).SetValue(custom, new[] { 11, 22, 33, 44 });
            profileType.GetField("speedUpgradeCosts", Instance).SetValue(custom, new[] { 7, 14, 21, 28, 35, 42, 49, 56, 63 });
            Array profiles = Array.CreateInstance(profileType, 2);
            profiles.SetValue(defaults.GetValue(0), 0); profiles.SetValue(custom, 1);
            Tune(balance, "upgradeCostProfiles", profiles);
            Tune(balance, "levelUpgradeCostProfileIds", new[] { 0, 0, 1, 0, 0 });
            Tune(balance, "hireCosts", new[] { 200, 500, 1200, 2800 });
            Tune(balance, "speedUpgradeCosts", new[] { 25, 40, 65, 100, 160, 250, 400, 640, 1000 });

            object allBoys = Make(level: 0, balance: balance);
            object velez = Make(level: 2, balance: balance);
            object chicago = Make(level: 1, balance: balance);
            Assert.AreEqual(15, Get(allBoys, "HireCost")); Assert.AreEqual(5, Get(allBoys, "SpeedCost"));
            Assert.AreEqual(11, Get(velez, "HireCost")); Assert.AreEqual(7, Get(velez, "SpeedCost"));
            Assert.AreEqual(15, Get(chicago, "HireCost")); Assert.AreEqual(5, Get(chicago, "SpeedCost"));
        }



        [Test]
        public void MissingTablesUseDefaultsAndOlderPurchasesClampToLastTier()
        {
            object b = NewBalance();
            Tune(b, "hireCosts", new int[0]); Tune(b, "speedUpgradeCosts", null);
            Tune(b, "florestaHireCosts", new int[0]); Tune(b, "florestaSpeedUpgradeCosts", null);
            Tune(b, "levelGoals", null);
            object fresh = Make(balance: b);
            Assert.AreEqual(200, Get(fresh, "Goal")); Assert.AreEqual(15, Get(fresh, "HireCost"));
            Assert.AreEqual(5, Get(fresh, "SpeedCost"));
            object laterDefaults = Make(level: 1, balance: b);
            Assert.AreEqual(15, Get(laterDefaults, "HireCost")); Assert.AreEqual(5, Get(laterDefaults, "SpeedCost"));
            object restored = Activator.CreateInstance(simType, new object[] { b, 1, 5f, 10000, 8, 65 });
            Assert.AreEqual(1, Get(restored, "StaffCount")); Assert.AreEqual(0, Get(restored, "SpeedLevel"));
            Assert.AreEqual(1f, Get(restored, "WorkRate"));
            Assert.AreEqual(1, Get(restored, "ParrilleroCount")); Assert.AreEqual(0, Get(restored, "CocacoleroCount"));
            Assert.AreEqual(10000, Get(restored, "Coins"), "The explicit test-balance API is unchanged");
            Start(restored); Assert.AreEqual(0, Get(restored, "Coins"));
        }

        [Test]
        public void FlorestaInsufficientFundsDoNotChangePurchases()
        {
            object speed = Make(coins: 4); Assert.IsFalse((bool)Call(speed, "TryUpgradeSpeed"));
            Assert.AreEqual(4, Get(speed, "Coins")); Assert.AreEqual(0, Get(speed, "SpeedLevel"));
            object hire = Make(coins: 14); Assert.IsFalse((bool)Call(hire, "TryHire"));
            Assert.AreEqual(14, Get(hire, "Coins")); Assert.AreEqual(1, Get(hire, "StaffCount"));
        }

public void LaterLevelPurchasesResumeAtTheNextTableTier()
        {
            object sim = Make(level: 1);
            Start(sim); Set(sim, "Coins", 100);
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("Cocacolero")));
            Call(sim, "TryUpgradeSpeed"); Call(sim, "TryUpgradeSpeed");
            Assert.AreEqual(30, Get(sim, "HireCost")); Assert.AreEqual(30, Get(sim, "CocacoleroHireCost")); Assert.AreEqual(15, Get(sim, "SpeedCost"));
            Assert.AreEqual(70, Get(sim, "Coins")); Assert.That((float)Get(sim, "WorkRate"), Is.EqualTo(1.2f).Within(.0001f));
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
        public void OneProductShowsOneRow()
        {
            object sim = Make(level: 0);
            object customer = SpawnOrder(sim, new[] { 0 }, new[] { 4 });
            Assert.AreEqual(1, Get(customer, "PendingOrderLineCount"));
            Assert.AreEqual(0, Get(Call(customer, "GetVisibleOrderLine", 0), "Product"));
            Assert.IsNull(Call(customer, "GetVisibleOrderLine", 1));
            Assert.AreEqual(0, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void TwoProductsShowTwoRows()
        {
            object sim = Make(level: 1);
            object customer = SpawnOrder(sim, new[] { 0, 4 }, new[] { 4, 2 });
            CollectionAssert.AreEqual(new[] { 0, 4 }, VisibleProducts(customer));
            Assert.AreEqual(0, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void ThreeProductsShowTwoRowsAndPlusOne()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2 }, new[] { 4, 2, 1 });
            CollectionAssert.AreEqual(new[] { 0, 1 }, VisibleProducts(customer));
            Assert.AreEqual(1, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void FourProductOrdersKeepTwoRowsAndHideTwo()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3 }, new[] { 4, 2, 1, 3 });
            CollectionAssert.AreEqual(new[] { 0, 1 }, VisibleProducts(customer));
            Assert.AreEqual(2, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void FiveProductsShowTwoRowsAndPlusThree()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3, 4 }, new[] { 4, 2, 1, 3, 2 });
            CollectionAssert.AreEqual(new[] { 0, 1 }, VisibleProducts(customer));
            Assert.AreEqual(3, Get(customer, "HiddenOrderLineCount"));
            Assert.AreEqual(5, Get(customer, "OrderLineCount"), "The display window must not truncate the complete order.");
        }

        [Test]
        public void ZeroQuantityProductIsHidden()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2 }, new[] { 4, 2, 1 });
            Set(Call(customer, "GetOrderLine", 0), "Remaining", 0);
            Assert.AreEqual(2, Get(customer, "PendingOrderLineCount"));
            CollectionAssert.AreEqual(new[] { 1, 2 }, VisibleProducts(customer));
            Assert.AreEqual(0, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void CompletedVisibleProductRevealsNextHiddenProduct()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3 }, new[] { 4, 2, 1, 3 });
            Set(Call(customer, "GetOrderLine", 0), "Remaining", 0);
            CollectionAssert.AreEqual(new[] { 1, 2 }, VisibleProducts(customer));
            Assert.AreEqual(1, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void PlusMoreCountUpdatesCorrectly()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3, 4 }, new[] { 4, 2, 1, 3, 2 });
            Assert.AreEqual(3, Get(customer, "HiddenOrderLineCount"));
            Set(Call(customer, "GetOrderLine", 0), "Remaining", 0);
            Assert.AreEqual(2, Get(customer, "HiddenOrderLineCount"));
            Set(Call(customer, "GetOrderLine", 1), "Remaining", 0);
            Assert.AreEqual(1, Get(customer, "HiddenOrderLineCount"));
            Set(Call(customer, "GetOrderLine", 2), "Remaining", 0);
            Assert.AreEqual(0, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void ProductOrderIsPreserved()
        {
            object customer = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3, 4 }, new[] { 4, 2, 1, 3, 2 });
            Set(Call(customer, "GetOrderLine", 1), "Remaining", 0);
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4 }, PendingProducts(customer));
            CollectionAssert.AreEqual(new[] { 0, 2 }, VisibleProducts(customer));
            Assert.AreEqual(2, Get(customer, "HiddenOrderLineCount"));
        }

        [Test]
        public void SameLogicWorksAcrossDifferentLevels()
        {
            object floresta = SpawnOrder(Make(level: 0), new[] { 0 }, new[] { 2 });
            object chicago = SpawnOrder(Make(level: 1), new[] { 4, 0 }, new[] { 1, 3 });
            object laterClub = SpawnOrder(Make(level: 4), new[] { 0, 1, 2, 3, 4 }, new[] { 1, 2, 3, 4, 5 });
            Assert.AreEqual(1, Get(floresta, "PendingOrderLineCount"));
            CollectionAssert.AreEqual(new[] { 4, 0 }, VisibleProducts(chicago));
            CollectionAssert.AreEqual(new[] { 0, 1 }, VisibleProducts(laterClub));
            Assert.AreEqual(3, Get(laterClub, "HiddenOrderLineCount"));
        }

        [Test]
        public void FiveProductOrderKeepsFoodPriorityWhileSpecialistsWorkConcurrentlyAndLeavesNormally()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 1); Tune(balance, "workerSpeed", 100000f);
            Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(staff: 3, level: 4, balance: balance); Start(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 1, 2, 3, 4 }, new[] { 1, 1, 1, 1, 1 }));
            object customer = Customers(sim)[0];
            for (int i = 0; i < 1500 && (int)Get(sim, "Delivered") < 5; i++)
            {
                Step(sim, .05f);
                // Each griller retains catalog order within its own part; Coca progresses independently.
                foreach (int[] orderedLines in new[] { new[] { 0, 1 }, new[] { 2, 3 } })
                    if ((int)Get(Call(customer, "GetOrderLine", orderedLines[0]), "Remaining") > 0)
                        Assert.AreEqual(1, Get(Call(customer, "GetOrderLine", orderedLines[1]), "Remaining"),
                            "A worker must complete its first assigned food line before the next one; other roles run in parallel.");
            }
            Assert.AreEqual(5, Get(sim, "Delivered"));
            Assert.AreEqual(25, Get(sim, "Coins"), "Each of the five actual handoffs earns its unchanged $5 price.");
            for (int line = 0; line < 5; line++) Assert.AreEqual(0, Get(Call(customer, "GetOrderLine", line), "Remaining"));
            Assert.AreEqual(0, Get(customer, "Reserved"));
            for (int i = 0; i < 200 && Customers(sim).Contains(customer); i++) Step(sim, .05f);
            Assert.AreEqual("Leaving", Get(customer, "State").ToString());
            Assert.IsFalse(Customers(sim).Contains(customer), "A completed five-line order still exits through the normal queue flow.");
        }

        [Test]
        public void ExistingOneProductOrdersStillWork()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 1); Tune(balance, "workerSpeed", 100000f);
            Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(level: 0, balance: balance); Start(sim);
            Assert.IsTrue(Spawn(sim, 0, 1)); object customer = Customers(sim)[0];
            for (int i = 0; i < 100 && (int)Get(customer, "Remaining") > 0; i++) Step(sim, .05f);
            Assert.AreEqual(0, Get(customer, "Remaining"));
            Assert.AreEqual(1, Get(sim, "Delivered")); Assert.AreEqual(5, Get(sim, "Coins"));
            Assert.AreEqual(0, Get(customer, "Reserved"));
        }

        [Test]
        public void ChicagoWorkersStayAssignedUntilTheirSpecialtyPartIsComplete()
        {
            object balance = NewBalance();
            Tune(balance, "maxCustomers", 2); Tune(balance, "levelGoals", new[] { 200, 100, 65, 85, 110 });
            Tune(balance, "workerSpeed", 100000f); Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
            object sim = Make(staff: 2, level: 1, balance: balance); Start(sim);
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 4 }, new[] { 2, 1 }));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0 }, new[] { 1 }));
            object first = Customers(sim)[0], second = Customers(sim)[1]; SetWaiting(first); SetWaiting(second);
            IList workers = (IList)Get(sim, "Workers");
            foreach (object worker in workers) HiddenCall(sim, "Assign", worker);
            Assert.AreEqual(Get(first, "Id"), Get(workers[0], "CustomerId"));
            Assert.AreEqual(Get(first, "Id"), Get(workers[1], "CustomerId"));
            Assert.AreEqual(0, Get(workers[0], "Product")); Assert.AreEqual(4, Get(workers[1], "Product"));
            for (int i = 0; i < 500 && (int)Get(sim, "Delivered") < 3; i++) Step(sim, .02f);
            Assert.AreEqual(3, Get(sim, "Delivered"));
            Assert.AreEqual(0, Get(Call(first, "GetOrderLine", 0), "Remaining"));
            Assert.AreEqual(0, Get(Call(first, "GetOrderLine", 1), "Remaining"));
            Assert.AreEqual(1, Get(second, "Remaining"), "Neither specialty worker may take the next customer before completing its current part.");
            Assert.AreEqual(15, Get(sim, "Coins")); Assert.AreEqual(0, Get(first, "Reserved"));
        }

        [Test]
        public void TwoParrillerosOrTwoCocacolerosNeverOwnTheSameCustomerPart()
        {
            object balance = NewBalance();
            object sim = Make(staff: 4, level: 1, balance: balance); Start(sim);
            IList workers = (IList)Get(sim, "Workers");
            StreetWorkerRoleForTest(workers[0], "Parrillero"); StreetWorkerRoleForTest(workers[1], "Parrillero");
            StreetWorkerRoleForTest(workers[2], "Cocacolero"); StreetWorkerRoleForTest(workers[3], "Cocacolero");
            object first = SpawnOrder(sim, new[] { 0, 4 }, new[] { 2, 2 });
            Assert.NotNull(SpawnOrder(sim, new[] { 0, 4 }, new[] { 2, 2 }));
            IList customers = Customers(sim); object second = customers[1];
            SetWaiting(first); SetWaiting(second);
            HiddenCall(sim, "Assign", workers[0]); HiddenCall(sim, "Assign", workers[1]);
            Assert.AreNotEqual(Get(workers[0], "CustomerId"), Get(workers[1], "CustomerId"));
            Assert.AreNotEqual(Get(workers[0], "Id"), Get(workers[1], "Id"));
            HiddenCall(sim, "CancelAssignment", workers[0]); HiddenCall(sim, "CancelAssignment", workers[1]);
            StreetWorkerRoleForTest(workers[0], "Cocacolero"); StreetWorkerRoleForTest(workers[1], "Cocacolero");
            HiddenCall(sim, "Assign", workers[0]); HiddenCall(sim, "Assign", workers[1]);
            Assert.AreNotEqual(Get(workers[0], "CustomerId"), Get(workers[1], "CustomerId"));
            Assert.AreEqual(Get(first, "Id"), Get(workers[0], "CustomerId"));
            Assert.AreEqual(Get(second, "Id"), Get(workers[1], "CustomerId"));
        }

        [Test]
        public void WorkerStaysIdleWithoutAnEligibleSpecialtyAndNeverServesRearRow()
        {
            object sim = Make(staff: 2, level: 1); Start(sim);
            IList workers = (IList)Get(sim, "Workers");
            object cocaOnly = SpawnOrder(sim, new[] { 4 }, new[] { 1 }); SetWaiting(cocaOnly);
            StreetWorkerRoleForTest(workers[0], "Parrillero"); HiddenCall(sim, "Assign", workers[0]);
            Assert.AreEqual("Idle", Get(workers[0], "State").ToString());
            Assert.AreEqual(0, Get(cocaOnly, "Reserved"));

            object crowded = Make(staff: 2, level: 1); Start(crowded);
            IList crowd = Customers(crowded);
            for (int i = 0; i < 7; i++)
            {
                Assert.IsTrue(Spawn(crowded, 4, 1)); SetWaiting(crowd[crowd.Count - 1]);
            }
            Assert.IsTrue(Spawn(crowded, 0, 1)); object rearChori = crowd[crowd.Count - 1]; SetWaiting(rearChori);
            IList crowdWorkers = (IList)Get(crowded, "Workers");
            HiddenCall(crowded, "Assign", crowdWorkers[0]);
            HiddenCall(crowded, "Assign", crowdWorkers[1]);
            Assert.AreNotEqual("Idle", Get(crowdWorkers[1], "State").ToString(), "Cocacolero serves the oldest eligible front-row client.");
            Assert.IsNull(Field(crowdWorkers[0], "Customer"), "Parrillero must not skip seven Coca fans to reach the chori fan in the back row.");
            Assert.AreEqual(0, Get(rearChori, "Reserved"));
        }

        [Test]
        public void ExpiredSpecialtyAssignmentReleasesOwnerWithoutDeliveryOrCoins()
        {
            object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "customerPatienceSeconds", .25f);
            object sim = Make(level: 1, balance: balance); Start(sim);
            object customer = SpawnOrder(sim, new[] { 0, 4 }, new[] { 2, 1 }); SetWaiting(customer);
            object worker = ((IList)Get(sim, "Workers"))[0]; HiddenCall(sim, "Assign", worker);
            object chori = Call(customer, "GetOrderLine", 0);
            Assert.AreEqual(Get(worker, "Id"), Get(chori, "OwnerWorkerId")); Assert.AreEqual(1, Get(chori, "Reserved"));
            SetField(customer, "Patience", 0f); HiddenCall(sim, "AdvanceCustomers", .01f);
            Assert.AreEqual(0, Get(chori, "OwnerWorkerId")); Assert.AreEqual(0, Get(chori, "Reserved"));
            State(worker, "Handoff"); SetField(worker, "Delay", 0f); HiddenCall(sim, "Deliver", worker);
            Assert.AreEqual(0, Get(sim, "Delivered")); Assert.AreEqual(0, Get(sim, "Coins")); Assert.AreEqual(0, Get(sim, "CoinsEarned"));
        }

        [Test]
        public void FerroAndIndependienteExposeCumulativeProductsInRequestedDisplayOrder()
        {
            string[] names = (string[])simType.GetField("LevelNames", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Assert.AreEqual("Ferro Carril Oeste", names[3]);
            Assert.AreEqual("Independiente de Avellaneda", names[4]);
            object ferro = Make(level: 3), independiente = Make(level: 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 4, 6 }, AvailableProducts(ferro));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 6, 5 }, AvailableProducts(independiente));
            Assert.AreEqual(85, Get(ferro, "Goal")); Assert.AreEqual(270f, Get(ferro, "TimeRemaining"));
            Assert.AreEqual(110, Get(independiente, "Goal")); Assert.AreEqual(300f, Get(independiente, "TimeRemaining"));
        }

        [Test]
        public void FerroAcceptsBondiolaBeerAndMixedTicketsButKeepsVacioAndFernetLocked()
        {
            object ferro = Make(level: 3);
            Assert.IsTrue((bool)Call(ferro, "SpawnCustomerWithProducts", new[] { 0, 1, 2, 4, 6 }, new[] { 1, 2, 3, 1, 2 }));
            Assert.IsFalse((bool)Call(ferro, "SpawnCustomerWithProducts", new[] { 3 }, new[] { 1 }), "Vacío unlocks only in Independiente.");
            Assert.IsFalse((bool)Call(ferro, "SpawnCustomerWithProducts", new[] { 5 }, new[] { 1 }), "Fernet unlocks only in Independiente.");
        }

        [Test]
        public void IndependienteUnlocksVacioAndFernetAndStillLimitsEachOrderToFiveTypes()
        {
            object independiente = Make(level: 4);
            Assert.IsTrue((bool)Call(independiente, "SpawnCustomerWithProducts", new[] { 0, 2, 3, 6, 5 }, new[] { 2, 1, 3, 2, 1 }));
            Assert.IsFalse((bool)Call(independiente, "SpawnCustomerWithProducts", new[] { 0, 1, 2, 3, 4, 6 }, new[] { 1, 1, 1, 1, 1, 1 }));
            Assert.AreEqual(5, Get(Customers(independiente)[0], "OrderLineCount"));
        }

        [Test]
        public void FerroAndIndependienteMixedTicketsUseEveryRequiredSpecialtyAndComplete()
        {
            foreach (int level in new[] { 3, 4 })
            {
                object balance = NewBalance();
                Tune(balance, "maxCustomers", 1); Tune(balance, "workerSpeed", 100000f);
                Tune(balance, "customerSpeed", 100000f); Tune(balance, "pickupSeconds", .01f);
                int[] products = level == 3 ? new[] { 0, 1, 2, 4, 6 } : new[] { 0, 2, 3, 6, 5 };
                int[] quantities = { 1, 2, 1, 2, 1 };
                int expectedTotal = 7;
                int staff = level == 3 ? 3 : 4;
                object sim = Make(staff: staff, level: level, balance: balance); Start(sim);
                object customer = SpawnOrder(sim, products, quantities); SetWaiting(customer);
                IList workers = (IList)Get(sim, "Workers");
                var byRole = new Dictionary<string, object>();
                foreach (object worker in workers) byRole[Get(worker, "Role").ToString()] = worker;
                string[] neededRoles = level == 3
                    ? new[] { "Parrillero", "ParrilleroPremium", "Cocacolero" }
                    : new[] { "Parrillero", "ParrilleroPremium", "Cocacolero", "Fernetero" };
                foreach (string role in neededRoles)
                {
                    Assert.IsTrue(byRole.ContainsKey(role), role + " is available from this level's product catalog.");
                    HiddenCall(sim, "Assign", byRole[role]);
                    Assert.AreEqual(Get(customer, "Id"), Get(byRole[role], "CustomerId"), role + " claims its portion of the mixed ticket.");
                }
                for (int lineIndex = 0; lineIndex < products.Length; lineIndex++)
                {
                    string role = products[lineIndex] <= 1 ? "Parrillero" :
                        products[lineIndex] <= 3 ? "ParrilleroPremium" :
                        products[lineIndex] == 5 ? "Fernetero" : "Cocacolero";
                    Assert.AreEqual(Get(byRole[role], "Id"), Get(Call(customer, "GetOrderLine", lineIndex), "OwnerWorkerId"),
                        "Product " + products[lineIndex] + " must be owned by its only responsible role.");
                }
                for (int frame = 0; frame < 1800 && (int)Get(sim, "Delivered") < expectedTotal; frame++) Step(sim, .02f);
                Assert.AreEqual(expectedTotal, Get(sim, "Delivered"));
                Assert.AreEqual(expectedTotal * 5, Get(sim, "Coins"));
                Assert.AreEqual(0, Get(customer, "PendingOrderLineCount")); Assert.AreEqual(0, Get(customer, "Reserved"));
                for (int i = 0; i < products.Length; i++)
                {
                    object line = Call(customer, "GetOrderLine", i);
                    Assert.AreEqual(products[i], Get(line, "Product")); Assert.AreEqual(0, Get(line, "Remaining"));
                    Assert.AreEqual(0, Get(line, "Reserved")); Assert.AreEqual(0, Get(line, "OwnerWorkerId"));
                }
                Step(sim, .35f);
                Assert.AreEqual("Leaving", Get(customer, "State").ToString(), "The customer leaves after every role's order lines finish.");
            }
        }

        [Test]
        public void FerroAndIndependienteSpecialistsKeepFIFOAndNeverShareTheirOwnRolePart()
        {
            foreach (int level in new[] { 3, 4 })
            {
                object balance = NewBalance(); Tune(balance, "maxCustomers", 2);
                object sim = Make(staff: 5, level: level, balance: balance); Start(sim);
                object first = SpawnOrder(sim, new[] { 0, 4 }, new[] { 2, 1 });
                int[] secondProducts = level == 3 ? new[] { 2, 6 } : new[] { 2, 5, 6 };
                var secondQuantities = new int[secondProducts.Length];
                for (int quantityIndex = 0; quantityIndex < secondQuantities.Length; quantityIndex++) secondQuantities[quantityIndex] = 1;
                Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", secondProducts, secondQuantities));
                object second = Customers(sim)[1]; SetWaiting(first); SetWaiting(second);
                IList workers = (IList)Get(sim, "Workers");
                StreetWorkerRoleForTest(workers[0], "Parrillero");
                StreetWorkerRoleForTest(workers[1], "Cocacolero");
                StreetWorkerRoleForTest(workers[2], "Cocacolero");
                StreetWorkerRoleForTest(workers[3], "ParrilleroPremium");
                StreetWorkerRoleForTest(workers[4], level == 3 ? "Parrillero" : "Fernetero");
                for (int i = 0; i < workers.Count; i++) HiddenCall(sim, "Assign", workers[i]);
                Assert.AreEqual(Get(first, "Id"), Get(workers[0], "CustomerId"));
                Assert.AreEqual(Get(first, "Id"), Get(workers[1], "CustomerId"), "The first Cocacolero owns the oldest Coca line.");
                Assert.AreEqual(Get(second, "Id"), Get(workers[2], "CustomerId"), "A second Cocacolero skips the owned first ticket and handles the next Coca line.");
                Assert.AreEqual(Get(second, "Id"), Get(workers[3], "CustomerId"));
                if (level == 3) Assert.AreEqual("Idle", Get(workers[4], "State").ToString());
                else Assert.AreEqual(Get(second, "Id"), Get(workers[4], "CustomerId"));
                Assert.AreNotEqual(Get(workers[1], "CustomerId"), Get(workers[2], "CustomerId"));
            }
        }

        [Test]
        public void FerroAndIndependienteRandomOrdersUseOnlyUnlockedProductsAndOneToFiveTypes()
        {
            foreach (int level in new[] { 3, 4 })
            {
                int[] expected = level == 3 ? new[] { 0, 1, 2, 4, 6 } : new[] { 0, 1, 2, 3, 4, 6, 5 };
                for (int seed = 0; seed < 50; seed++)
                {
                    object balance = NewBalance(); Tune(balance, "maxCustomers", 1); Tune(balance, "randomSeed", seed);
                    object sim = Make(level: level, balance: balance); Start(sim); Step(sim, .05f);
                    object customer = Customers(sim)[0];
                    int count = (int)Get(customer, "OrderLineCount"); Assert.That(count, Is.InRange(1, 5));
                    var seen = new System.Collections.Generic.HashSet<int>();
                    for (int lineIndex = 0; lineIndex < count; lineIndex++)
                    {
                        object line = Call(customer, "GetOrderLine", lineIndex);
                        int product = (int)Get(line, "Product");
                        CollectionAssert.Contains(expected, product); Assert.IsTrue(seen.Add(product), "No automatic order repeats a product type.");
                        Assert.That((int)Get(line, "Remaining"), Is.InRange(1, 4));
                    }
                }
            }
        }

        [Test]
        public void FourZoneKitchenStationsAreAlignedAndPickupsStayBesideTheirVisibleAssets()
        {
            MethodInfo station = simType.GetMethod("StationPositionForLevel", BindingFlags.Static | BindingFlags.Public);
            for (int level = 0; level < 5; level++)
            for (int product = 0; product < 4; product++)
                Assert.AreEqual(new Vector2(235, 575), station.Invoke(null, new object[] { product, level, 7 }));
            Assert.AreEqual(new Vector2(435, 565), station.Invoke(null, new object[] { 4, 3, 5 }));
            Assert.AreEqual(new Vector2(395, 565), station.Invoke(null, new object[] { 6, 3, 5 }));
            Assert.AreEqual(new Vector2(155, 500), station.Invoke(null, new object[] { 5, 4, 7 }));
        }

        private int[] AvailableProducts(object sim)
        {
            int count = (int)Get(sim, "ProductCount"); var ids = new int[count];
            for (int i = 0; i < count; i++) ids[i] = (int)Call(sim, "GetAvailableProduct", i);
            return ids;
        }

        private void SetWaiting(object customer) { Set(customer, "Position", Get(customer, "Target")); State(customer, "Waiting"); }
        private void StreetWorkerRoleForTest(object worker, string role) { Set(worker, "Role", System.Enum.Parse(Get(worker, "Role").GetType(), role)); }

        private object SpawnOrder(object sim, int[] products, int[] quantities)
        {
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", products, quantities));
            return Customers(sim)[0];
        }
        private static int[] VisibleProducts(object customer)
        {
            var products = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 2; i++)
            {
                object line = Call(customer, "GetVisibleOrderLine", i);
                if (line != null) products.Add((int)Get(line, "Product"));
            }
            return products.ToArray();
        }
        private static int[] PendingProducts(object customer)
        {
            var products = new System.Collections.Generic.List<int>();
            int count = (int)Get(customer, "PendingOrderLineCount");
            for (int i = 0; i < count; i++) products.Add((int)Get(Call(customer, "GetPendingOrderLine", i), "Product"));
            return products.ToArray();
        }

        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object obj, string property, object value) { obj.GetType().GetProperty(property, Instance).SetValue(obj, value, null); }
        private static object Field(object obj, string name) { return obj.GetType().GetField(name, Hidden).GetValue(obj); }
        private static void SetField(object obj, string name, object value) { obj.GetType().GetField(name, Hidden).SetValue(obj, value); }
        private static object HiddenCall(object obj, string method, params object[] args) { return obj.GetType().GetMethod(method, Hidden).Invoke(obj, args); }
        private static void State(object obj, string state) { Set(obj, "State", Enum.Parse(obj.GetType().GetProperty("State").PropertyType, state)); }
        private object FullSettledQueue(int coins = 0)
        {
            object sim = Make(); Start(sim); Set(sim, "Coins", coins);
            for (int i = 0; i < 21; i++) Assert.IsTrue(Spawn(sim, 0, 4));
            foreach (object c in Customers(sim)) { Set(c, "Position", Get(c, "Target")); State(c, "Waiting"); }
            return sim;
        }

        [Test]
        public void RestartResetsFirstLevelTeamSpeedAndCoins()
        {
            object sim = Make(); Start(sim); Set(sim, "Coins", 1000);
            for (int i = 0; i < 2; i++) { Call(sim, "TryHire"); Call(sim, "TryUpgradeSpeed"); }
            Assert.AreEqual(3, Get(sim, "StaffCount")); Assert.AreEqual(2, Get(sim, "SpeedLevel"));
            Start(sim);
            Assert.AreEqual(1, Get(sim, "StaffCount")); Assert.AreEqual(0, Get(sim, "SpeedLevel"));
            Assert.AreEqual(1f, Get(sim, "WorkRate")); Assert.AreEqual(0, Get(sim, "Coins"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DepartingFrontAdvancesExistingLaneAndAdmitsNewcomerOnlyAtTail(bool expires)
        {
            object sim = FullSettledQueue(); IList queue = Customers(sim);
            object front = queue[0], second = queue[7], third = queue[14];
            Vector2 secondFrom = (Vector2)Get(second, "Position"), thirdFrom = (Vector2)Get(third, "Position");
            int secondId = (int)Get(second, "Id"), thirdId = (int)Get(third, "Id");
            if (expires) { SetField(front, "Patience", 0f); HiddenCall(sim, "AdvanceCustomers", .01f); }
            else { Set(front, "Remaining", 0); State(front, "Receiving"); SetField(front, "ReceiveRemaining", 0f); HiddenCall(sim, "AdvanceCustomers", .01f); }
            Assert.AreEqual("Leaving", Get(front, "State").ToString()); Assert.AreEqual(-1, Field(front, "Slot"));
            Assert.AreEqual(0, Field(second, "Slot")); Assert.AreEqual(7, Field(third, "Slot"));
            Assert.AreEqual("Advancing", Get(second, "State").ToString()); Assert.AreEqual("Advancing", Get(third, "State").ToString());
            Assert.AreEqual(secondFrom, Get(second, "Position")); Assert.AreEqual(thirdFrom, Get(third, "Position"));
            Assert.AreEqual(secondId, Get(second, "Id")); Assert.AreEqual(thirdId, Get(third, "Id"));
            Assert.AreEqual(4, Get(second, "Remaining")); Assert.AreEqual(4, Get(third, "Remaining"));
            Assert.IsFalse(Spawn(sim, 0, 1), "Leaver counts toward capacity until offscreen");
            HiddenCall(sim, "RemoveCustomer", front); Assert.IsTrue(Spawn(sim, 0, 1));
            object newcomer = queue[queue.Count - 1]; Assert.AreEqual(14, Field(newcomer, "Slot"));
            Assert.Greater((int)Get(newcomer, "Id"), thirdId);
            HiddenCall(sim, "AdvanceCustomers", .05f);
            Assert.Greater(((Vector2)Get(second, "Position")).y, secondFrom.y);
            Assert.Less(((Vector2)Get(second, "Position")).y, ((Vector2)Get(second, "Target")).y);
            Assert.AreEqual(0, Get(sim, "Delivered")); Assert.AreEqual(0, Get(sim, "Coins"));
        }

        [Test]
        public void WorkersReserveOnlySettledFrontCustomersNeverMovingOrRearFans()
        {
            object sim = FullSettledQueue(10000);
            for (int i = 0; i < 4; i++) Assert.IsTrue((bool)Call(sim, "TryHire"));
            object moving = Customers(sim)[0]; State(moving, "Advancing"); Set(moving, "Position", new Vector2(58, 280));
            foreach (object worker in (IList)Get(sim, "Workers"))
            {
                HiddenCall(sim, "Assign", worker); object c = Field(worker, "Customer");
                Assert.NotNull(c); Assert.That((int)Field(c, "Slot"), Is.InRange(1, 6));
                Assert.AreEqual(Get(c, "Target"), Get(c, "Position")); Assert.AreEqual("Waiting", Get(c, "State").ToString());
            }
            Assert.AreEqual(0, Get(moving, "Reserved"));
            foreach (object c in Customers(sim)) if ((int)Field(c, "Slot") >= 7) Assert.AreEqual(0, Get(c, "Reserved"));
        }

        [Test]
        public void ConcurrentHandoffsStopAtExactTwoHundredthSaleInSameSlice()
        {
            object sim = FullSettledQueue(10000);
            for (int i = 0; i < 4; i++) Assert.IsTrue((bool)Call(sim, "TryHire"));
            object kitchen = Get(sim, "Kitchen");
            foreach (object worker in (IList)Get(sim, "Workers"))
            {
                HiddenCall(sim, "Assign", worker);
                int product = (int)Get(worker, "Product"), workerId = (int)Get(worker, "Id");
                object physicalItem = Call(kitchen, "TryTake", product, Get(worker, "Role"), workerId);
                Assert.NotNull(physicalItem, "Every synthetic handoff must own a distinct real cooked unit.");
                PropertyInfo carried = worker.GetType().GetProperty("CarriedItemId", BindingFlags.Instance | BindingFlags.Public);
                carried.GetSetMethod(true).Invoke(worker, new[] { Get(physicalItem, "Id") });
                State(worker, "Handoff"); SetField(worker, "Delay", 0f);
            }
            Set(sim, "Delivered", 199); int coins = (int)Get(sim, "Coins");
            Step(sim, .05f);
            Assert.AreEqual(200, Get(sim, "Delivered")); Assert.AreEqual(coins + 5, Get(sim, "Coins"));
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            int remaining = 0; foreach (object c in Customers(sim)) remaining += (int)Get(c, "Remaining");
            Assert.AreEqual(83, remaining, "Exactly one of five queued handoffs should commit");
        }

        [Test]
        public void ConcurrentQueueMaintainsUniqueSlotsAndExactReservationsAndIncome()
        {
            object sim = Make(); Start(sim); Set(sim, "Coins", 10000);
            for (int i = 0; i < 4; i++) Assert.IsTrue((bool)Call(sim, "TryHire"));
            int opening = (int)Get(sim, "Coins");
            for (int i = 0; i < 1800; i++)
            {
                Step(sim, .05f); var slots = new System.Collections.Generic.HashSet<int>();
                foreach (object c in Customers(sim))
                {
                    int slot = (int)Field(c, "Slot"); if (slot >= 0) Assert.IsTrue(slots.Add(slot));
                    Assert.That((int)Get(c, "Reserved"), Is.InRange(0, (int)Get(c, "Remaining")));
                    if (slot >= 7 || Get(c, "State").ToString() == "Advancing") Assert.AreEqual(0, Get(c, "Reserved"));
                }
                Assert.AreEqual(opening + (int)Get(sim, "Delivered") * 5, Get(sim, "Coins"));
            }
            Assert.Greater((int)Get(sim, "Delivered"), 0);
        }

        [Test]
        public void NewLevelsHaveStableNamesCatalogGoalsDurationAndDemand()
        {
            string[] expectedNames = { "Racing Club / Avellaneda", "San Lorenzo / Boedo", "River Plate / Núñez", "Boca Juniors / La Boca", "Sindicato de Camioneros / Plaza de Mayo", "Los Redondos / Tandil" };
            int[] expectedGoals = { 121, 133, 146, 161, 177, 195 };
            float[] expectedDemand = { 1.65f, 1.815f, 1.9965f, 2.19615f, 2.415765f, 2.6573415f };
            string[] names = (string[])simType.GetField("LevelNames", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object balance = NewBalance();
            float[] demand = (float[])balanceType.GetField("levelDemandMultipliers", Instance).GetValue(balance);
            for (int i = 0; i < expectedNames.Length; i++)
            {
                int level = i + 5;
                object sim = Make(level: level, balance: balance);
                Assert.AreEqual(expectedNames[i], names[level]);
                Assert.AreEqual(expectedGoals[i], Get(sim, "Goal"));
                Assert.AreEqual(300f, Get(sim, "TimeRemaining"));
                Assert.AreEqual(7, Get(sim, "ProductCount"));
                int[] stableOrder = { 0, 1, 2, 3, 4, 6, 5 };
                for (int slot = 0; slot < stableOrder.Length; slot++) Assert.AreEqual(stableOrder[slot], Call(sim, "GetAvailableProduct", slot));
                Assert.AreEqual(expectedDemand[i], demand[level], 0.00001f);
            }
        }

        [Test]
        public void FinalLevelCanActuallyWinAndSavePayloadSupportsUnlock()
        {
            object balance = NewBalance();
            int[] goals = (int[])balanceType.GetField("levelGoals", Instance).GetValue(balance);
            goals[10] = 1;
            object sim = Make(level: 10, balance: balance);
            Start(sim);
            Assert.IsTrue(Spawn(sim, 0, 1));
            for (int i = 0; i < 400 && Get(sim, "Phase").ToString() == "Playing"; i++) Step(sim, 0.05f);
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            Type saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object payload = Activator.CreateInstance(saveType);
            saveType.GetField("unlockedLevel").SetValue(payload, 10);
            object restored = JsonUtility.FromJson(JsonUtility.ToJson(payload), saveType);
            Assert.AreEqual(10, saveType.GetField("unlockedLevel").GetValue(restored));
        }

        [TestCase(5, 121)] [TestCase(6, 133)] [TestCase(7, 146)]
        [TestCase(8, 161)] [TestCase(9, 177)] [TestCase(10, 195)]
        public void LegacyFiveEntryBalanceFallsBackSafelyForNewLevels(int level, int goal)
        {
            object balance = NewBalance();
            Tune(balance, "levelGoals", new[] { 200, 200, 65, 85, 110 });
            Tune(balance, "levelDurations", new[] { 120f, 180f, 240f, 270f, 300f });
            Tune(balance, "levelDemandMultipliers", new[] { 1f, 1.1f, 1.2f, 1.35f, 1.5f });
            Tune(balance, "levelProductIds", new[] { "0", "0,4", "0,1,4", "0,1,2,4,6", "0,1,2,3,4,6,5" });
            object sim = Make(level: level, balance: balance);
            Assert.AreEqual(goal, Get(sim, "Goal"));
            Assert.AreEqual(300f, Get(sim, "TimeRemaining"));
            Assert.AreEqual(7, Get(sim, "ProductCount"));
            Start(sim); Assert.DoesNotThrow(() => Step(sim, 1f));
        }

        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void NewLevelCanWinWithActualGoalAndEarnedUpgradeIncome(int level)
        {
            object sim = Make(level: level); Start(sim);
            int goal = (int)Get(sim, "Goal");
            for (int i = 0; i < 6000 && Get(sim, "Phase").ToString() == "Playing"; i++)
            {
                Step(sim, .05f);
                bool changed = true; int safety = 0;
                while (changed && safety++ < 20)
                {
                    changed = false;
                    foreach (string roleName in new[] { "Parrillero", "ParrilleroPremium", "Cocacolero", "Fernetero" })
                    {
                        object role = Role(roleName);
                        if ((bool)Call(sim, "CanHireRole", role)) { Call(sim, "TryHire", role); changed = true; }
                    }
                    if ((bool)Get(sim, "CanUpgradeSpeed")) { Call(sim, "TryUpgradeSpeed"); changed = true; }
                }
            }
            TestContext.WriteLine("Level=" + level + ", phase=" + Get(sim, "Phase") + ", delivered=" + Get(sim, "Delivered") + ", elapsed=" + Get(sim, "Elapsed"));
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.AreEqual(goal, Get(sim, "Delivered"));
            Assert.Less((float)Get(sim, "Elapsed"), 300f);
            bool hasNext = (bool)Call(sim, "NextLevel");
            Assert.AreEqual(level < 10, hasNext);
            if (hasNext) Assert.AreEqual(level + 1, Get(sim, "LevelIndex"));
        }

        [Test]
        public void OnlyPlayingRoundAdvancesClock()
        {
            object sim = Make(); Step(sim, 1f); Assert.AreEqual(0f, Get(sim, "Elapsed"));
            Start(sim); Step(sim, 1f); Assert.Greater((float)Get(sim, "Elapsed"), 0f);
        }

        [Test]
        public void FlorestaCanPassByBuyingUpgradesWithEarnedIncomeBeforeDeadline()
        {
            object sim = Make(); Start(sim);
            Assert.AreEqual(15, Get(sim, "HireCost")); Assert.AreEqual(5, Get(sim, "SpeedCost"));
            for (int i = 0; i < 3600 && Get(sim, "Phase").ToString() == "Playing"; i++)
            {
                Step(sim, .05f);
                bool changed = true; int safety = 0;
                while (changed && safety++ < 20)
                {
                    changed = false;
                    if ((bool)Get(sim, "CanHire")) { Assert.IsTrue((bool)Call(sim, "TryHire")); changed = true; }
                    if ((bool)Get(sim, "CanUpgradeSpeed")) { Assert.IsTrue((bool)Call(sim, "TryUpgradeSpeed")); changed = true; }
                }
            }
            TestContext.WriteLine("Floresta purchase playthrough: phase=" + Get(sim, "Phase") +
                ", sold=" + Get(sim, "Delivered") + ", time=" + ((float)Get(sim, "Elapsed")).ToString("F1") +
                "s, helpers=" + Get(sim, "StaffCount") + ", speed=" + Get(sim, "WorkRate") +
                ", balance=$" + Get(sim, "Coins"));
            Assert.AreEqual("Won", Get(sim, "Phase").ToString());
            Assert.AreEqual(200, Get(sim, "Delivered"));
            Assert.Less((float)Get(sim, "Elapsed"), 120f);
            Assert.AreEqual(5, Get(sim, "StaffCount")); Assert.AreEqual(9, Get(sim, "SpeedLevel"));
            Assert.AreEqual(390, Get(sim, "Coins"));
        }

    }
}
