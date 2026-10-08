using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetKitchenProductionTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private Type productionType, balanceType, stateType, locationType, roleType;

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
        private static object Get(object value, string name) => value.GetType().GetProperty(name, PublicInstance).GetValue(value);
        private static object Call(object value, string name, params object[] args)
        {
            MethodInfo method = value.GetType().GetMethod(name, PublicInstance);
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
            Assert.AreEqual(18, (int)Call(early, "GrillCapacity", 0));
            Assert.AreEqual(0, (int)Call(early, "GrillCapacity", 1));
            object full = Make(new[] { 0, 1, 2, 3, 4, 5, 6 });
            Assert.AreEqual(24, (int)Call(full, "TableCount", 0));
            Assert.AreEqual(24, (int)Call(full, "TableCount", 1));
            Assert.AreEqual(12, (int)Call(full, "TableCount", 4));
            Assert.AreEqual(12, (int)Call(full, "TableCount", 6));
            Assert.AreEqual(12, (int)Call(full, "GrillCapacity", 0));
            Assert.AreEqual(6, (int)Call(full, "GrillCapacity", 1));
            Assert.AreEqual(4, (int)Call(full, "GrillCapacity", 2));
            Assert.AreEqual(3, (int)Call(full, "GrillCapacity", 3));
            Assert.IsNotNull(Unit(full, 0, "Raw", "Grill"));
            Assert.IsNotNull(Unit(full, 2, "Raw", "Grill"));
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

        [Test]
        public void EmptyDrinkBarrelRefillsOnlyWhenItsResponsibleRoleIsHired()
        {
            object kitchen = Make(new[] { 4 });
            for (int worker = 1; worker <= 12; worker++) Call(kitchen, "TryTake", 4, EnumValue(roleType, "Cocacolero"), worker);
            Call(kitchen, "Advance", 1f, 1f, false, false, false, false);
            Assert.AreEqual(0, (int)Call(kitchen, "TableCount", 4));
            Call(kitchen, "Advance", .28f, 1f, false, false, true, false);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 4));
        }

        [Test]
        public void GrillMeatAdvancesThroughCookedAndBurnedThenRefillsWithoutServingBurned()
        {
            object kitchen = Make(new[] { 0 }, balance =>
            {
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 1);
                balanceType.GetField("cookSeconds", PublicInstance).SetValue(balance, 1f);
                balanceType.GetField("burnAfterSeconds", PublicInstance).SetValue(balance, 2f);
                balanceType.GetField("discardBurnedAfterSeconds", PublicInstance).SetValue(balance, 1f);
            });
            object parrillero = EnumValue(roleType, "Parrillero");
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0), "Prepared stock keeps the table full while grill meat burns.");
            Call(kitchen, "Advance", 1f, 1f, true, false, false, false);
            Assert.IsNotNull(Unit(kitchen, 0, "Cooked", "Grill"));
            Call(kitchen, "Advance", 2f, 1f, true, false, false, false);
            Assert.IsNotNull(Unit(kitchen, 0, "Burned", "Grill"));
            object edible = Call(kitchen, "TryTake", 0, parrillero, 1);
            Assert.IsNotNull(edible);
            Assert.AreEqual("Cooked", Get(edible, "State").ToString());
            Assert.IsTrue((bool)Call(kitchen, "Consume", (int)Get(edible, "Id"), 0, 1));
            Assert.IsNull(Call(kitchen, "TryTake", 0, parrillero, 1), "Burned grill items must not replace missing table stock.");
            Call(kitchen, "Advance", 1f, 1f, true, false, false, false);
            Assert.IsNull(Unit(kitchen, 0, "Burned", "Grill"));
            Assert.IsNotNull(Unit(kitchen, 0, "Raw", "Grill"));
        }

        [Test]
        public void GrillRefillsVacatedSlotWhenCookedMeatMovesToTable()
        {
            object kitchen = Make(new[] { 0 }, balance =>
                balanceType.GetField("normalTableCapacity", PublicInstance).SetValue(balance, 1));
            Call(kitchen, "TryTake", 0, EnumValue(roleType, "Parrillero"), 1);
            Call(kitchen, "Advance", 4f, 1f, true, false, false, false);
            Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0));
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
