using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetKitchenRendererTests
    {
        private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Type GameType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object value, string name) => value.GetType().GetProperty(name, Instance).GetValue(value);
        private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Instance).Invoke(value, args);

        [TestCase(0, false, 39f, 21f)]
        [TestCase(1, false, 30f, 21f)]
        [TestCase(2, false, 30f, 21f)]
        [TestCase(3, false, 30f, 21f)]
        [TestCase(0, true, 39f, 22.5f)]
        [TestCase(1, true, 30f, 22.5f)]
        [TestCase(2, true, 30f, 22.5f)]
        [TestCase(3, true, 30f, 22.5f)]
        [TestCase(4, false, 9f, 20f)]
        [TestCase(5, false, 18f, 32f)]
        public void SandwichesUseLargerAspectPreservingDisplayTargetsWithoutScalingDrinks(
            int product, bool carried, float expectedWidth, float expectedHeight)
        {
            MethodInfo method = GameType("StreetKitchenRenderer").GetMethod("ServingSize", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2 size = (Vector2)method.Invoke(null, new object[] { product, carried });
            Assert.AreEqual(expectedWidth, size.x, .0001f);
            Assert.AreEqual(expectedHeight, size.y, .0001f);
        }

        [UnityTest]
        public IEnumerator TallProjectionKeepsMeatOnItsGrillAndConsumesTheIndividualCarriedObject()
        {
            var root = new GameObject("Modular kitchen projection test");
            try
            {
                object balance = Activator.CreateInstance(GameType("StreetBalance"));
                object sim = Activator.CreateInstance(GameType("StreetSimulation"), new object[] { balance, 4, 5f, 0, 1, 0 });
                object kitchen = Get(sim, "Kitchen");
                Component renderer = root.AddComponent(GameType("StreetKitchenRenderer"));
                const float verticalScale = 1.25f, height = 1200f;
                Call(renderer, "Prepare", sim, verticalScale, height);
                Assert.IsTrue((bool)Get(renderer, "IsReady"));
                var objects = (IDictionary)renderer.GetType().GetField("foodObjects", Instance).GetValue(renderer);
                Type layoutType = GameType("StreetKitchenLayout");
                object layout = Get(sim, "KitchenLayout");
                foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
                {
                    if (Get(unit, "Location").ToString() != "Grill") continue;
                    int product = (int)Get(unit, "Product"), slot = (int)Get(unit, "Slot");
                    int capacity = (int)Call(kitchen, "GrillCapacity", product);
                    MethodInfo slots = layoutType.GetMethod("GrillSlotPosition", new[] { typeof(int), typeof(int), typeof(bool), typeof(int) });
                    Vector2 position = (Vector2)slots.Invoke(layout, new object[] { product, slot, true, capacity });
                    Rect bounds = (Rect)layoutType.GetMethod("GrillBoundsForProduct").Invoke(layout, new object[] { product });
                    float expectedY = (height * .5f - (bounds.y * verticalScale + position.y - bounds.y)) * .01f;
                    var go = (GameObject)objects[(int)Get(unit, "Id")];
                    Assert.AreEqual(expectedY, go.transform.localPosition.y, .0001f, "Meat must use its grill anchor, not the preparation-table anchor.");
                    Assert.AreEqual((position.x - 270f) * .01f, go.transform.localPosition.x, .0001f);
                }

                object worker = ((IList)Get(sim, "Workers"))[0];
                int workerId = (int)Get(worker, "Id");
                object normal = Enum.Parse(GameType("StreetWorkerRole"), "Parrillero");
                object item = Call(kitchen, "TryTake", 0, normal, workerId);
                Assert.NotNull(item);
                int id = (int)Get(item, "Id");
                worker.GetType().GetProperty("CarriedItemId", Instance).GetSetMethod(true).Invoke(worker, new object[] { id });
                Call(renderer, "Prepare", sim, verticalScale, height);
                var carried = (GameObject)objects[id];
                Vector2 feet = (Vector2)Get(worker, "Position");
                Assert.AreEqual((feet.x - 9.5f - 270f) * .01f, carried.transform.localPosition.x, .0001f);
                Assert.AreEqual((height * .5f - (feet.y * verticalScale - 31.5f)) * .01f, carried.transform.localPosition.y, .0001f);
                Assert.AreEqual(31, carried.layer);
                Assert.IsTrue((bool)Call(kitchen, "Consume", id, 0, workerId));
                Call(renderer, "Prepare", sim, verticalScale, height);
                Assert.IsFalse(objects.Contains(id), "One consumed unit removes its corresponding native object.");
                yield return null;
                Assert.IsTrue(carried == null);
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullCatalogBarrelDrinksRenderAsTossedIndividualObjects()
        {
            var root = new GameObject("Barrel drink projection test");
            try
            {
                object balance = Activator.CreateInstance(GameType("StreetBalance"));
                object sim = Activator.CreateInstance(GameType("StreetSimulation"), new object[] { balance, 4, 5f, 0, 1, 0 });
                Component renderer = root.AddComponent(GameType("StreetKitchenRenderer"));
                Call(renderer, "Prepare", sim, 1f, 960f);
                Assert.IsTrue((bool)Get(renderer, "IsReady"));
                var objects = (IDictionary)renderer.GetType().GetField("foodObjects", Instance).GetValue(renderer);
                object kitchen = Get(sim, "Kitchen");
                object layout = Get(sim, "KitchenLayout");
                int coca = 0, beer = 0;
                foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
                {
                    int product = (int)Get(unit, "Product");
                    if ((product != 4 && product != 6) || Get(unit, "Location").ToString() != "Table") continue;
                    if (product == 4) coca++; else beer++;
                    int slot = (int)Get(unit, "Slot"), id = (int)Get(unit, "Id");
                    GameObject view = (GameObject)objects[id];
                    Vector2 point = (Vector2)Call(layout, "BarrelSlotPosition", product, slot);
                    Rect bounds = (Rect)Call(layout, "BoundsForProduct", product);
                    Assert.AreEqual((point.x - 270f) * .01f, view.transform.localPosition.x, .0001f);
                    Assert.AreEqual((480f - point.y) * .01f, view.transform.localPosition.y, .0001f);
                    float angle = (float)Call(layout, "BarrelSlotRotation", slot);
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(angle, view.transform.localEulerAngles.z)), Is.LessThan(.01f));
                    Assert.GreaterOrEqual(point.x, bounds.xMin);
                    Assert.LessOrEqual(point.x, bounds.xMax);
                }
                Assert.AreEqual(12, coca);
                Assert.AreEqual(12, beer);
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }
    }
}
