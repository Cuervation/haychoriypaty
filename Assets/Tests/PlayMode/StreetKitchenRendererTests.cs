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

        [TestCase(0, false, 22f, 10f)]
        [TestCase(1, false, 20f, 14f)]
        [TestCase(2, false, 20f, 12f)]
        [TestCase(3, false, 21f, 12f)]
        [TestCase(0, true, 24.2f, 11f)]
        [TestCase(1, true, 22f, 15.4f)]
        [TestCase(2, true, 22f, 13.2f)]
        [TestCase(3, true, 23.1f, 13.2f)]
        [TestCase(4, false, 10.8f, 20.25f)]
        [TestCase(5, false, 12f, 12.065625f)]
        [TestCase(6, false, 7.065f, 13.246875f)]
        public void ProductTargetsRespectPackageAndServingRatios(
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
        public IEnumerator FernetTableShelfUsesProportionalBottleSizesWithoutOverlap()
        {
            var root = new GameObject("Fernet bottle shelf sizing test");
            try
            {
                Component renderer = root.AddComponent(GameType("StreetKitchenRenderer"));
                var station = new GameObject("Station_FernetTable");
                station.transform.SetParent(root.transform, false);
                Sprite tableSprite = Resources.LoadAll<Sprite>("ModularKitchen/Sprites/empty-table")[0];
                SpriteRenderer table = station.AddComponent<SpriteRenderer>();
                table.sprite = tableSprite;

                Texture2D fernetTexture = Resources.Load<Texture2D>("ModularKitchen/Art/fernet-bottle-1l-v1");
                Assert.IsNotNull(fernetTexture);
                Sprite fernetSprite = Sprite.Create(fernetTexture, new Rect(0, 0, fernetTexture.width, fernetTexture.height),
                    new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                renderer.GetType().GetField("fernetBottleSprite", Instance).SetValue(renderer, fernetSprite);
                Call(renderer, "LoadOptionalFernetSprites");
                Assert.IsNotNull(renderer.GetType().GetField("coca25ShelfSprite", Instance).GetValue(renderer));
                Call(renderer, "EnsureFernetBottleShelf", station);

                Transform shelf = station.transform.Find("FernetBottleShelf");
                Assert.IsNotNull(shelf);
                Assert.AreEqual(6, shelf.childCount);
                float tableHeight = table.sprite.bounds.size.y;
                Type scaleType = GameType("StreetProductVisualScale");
                MethodInfo shelfFraction = scaleType.GetMethod("ShelfBottleHeightFraction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                float fernetFraction = (float)shelfFraction.Invoke(null, new object[] { true });
                float cocaFraction = (float)shelfFraction.Invoke(null, new object[] { false });
                for (int i = 0; i < shelf.childCount; i++)
                {
                    Transform bottle = shelf.GetChild(i);
                    SpriteRenderer bottleRenderer = bottle.GetComponent<SpriteRenderer>();
                    float expectedHeight = tableHeight * (i < 3 ? fernetFraction : cocaFraction);
                    Assert.AreEqual(expectedHeight, bottleRenderer.sprite.bounds.size.y * bottle.localScale.y, .001f,
                        "Fernet 1 L and Coca 2.5 L shelf bottles should scale to different, larger proportions.");
                    Assert.AreEqual(bottle.localScale.x, bottle.localScale.y, .0001f,
                        "Bottle sprites must preserve their original aspect ratios.");
                    Vector2 bottleSize = bottleRenderer.sprite.bounds.size * bottle.localScale.x;
                    Assert.GreaterOrEqual(bottle.localPosition.x - bottleSize.x * .5f, table.sprite.bounds.min.x - .001f,
                        "Enlarged bottles should remain within the table width.");
                    Assert.LessOrEqual(bottle.localPosition.x + bottleSize.x * .5f, table.sprite.bounds.max.x + .001f,
                        "Enlarged bottles should remain within the table width.");
                    Assert.GreaterOrEqual(bottle.localPosition.y - bottleSize.y * .5f, table.sprite.bounds.min.y - .001f,
                        "Enlarged bottles should remain within the table height.");
                    Assert.LessOrEqual(bottle.localPosition.y + bottleSize.y * .5f, table.sprite.bounds.max.y + .001f,
                        "Enlarged bottles should remain within the table height.");
                    for (int other = 0; other < i; other++)
                    {
                        Transform previous = shelf.GetChild(other);
                        SpriteRenderer previousRenderer = previous.GetComponent<SpriteRenderer>();
                        float separation = Mathf.Abs(bottle.localPosition.x - previous.localPosition.x);
                        float required = (bottleSize.x + previousRenderer.sprite.bounds.size.x * previous.localScale.x) * .5f;
                        Assert.GreaterOrEqual(separation + .001f, required, "Shelf bottles should not overlap.");
                    }
                }
                float fernetHeight = shelf.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size.y * shelf.GetChild(0).localScale.y;
                float cocaHeight = shelf.GetChild(3).GetComponent<SpriteRenderer>().sprite.bounds.size.y * shelf.GetChild(3).localScale.y;
                Assert.Greater(fernetHeight, tableHeight * .16f * 1.5f, "The 1 L Fernet bottle should be visibly larger than before.");
                Assert.Greater(cocaHeight, tableHeight * .16f * 1.5f, "The 2.5 L Coca bottle should be visibly larger than before.");
                Assert.AreEqual(1.40f / 1.31f, cocaHeight / fernetHeight, .01f, "Shelf scale must distinguish the real package heights, not their volume ratio.");
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
                var propsRoot = (GameObject)renderer.GetType().GetField("propsRoot", Instance).GetValue(renderer);
                foreach (string barrel in new[] { "Coca", "Beer" })
                {
                    Transform station = propsRoot.transform.Find("station:Station_" + barrel + "Barrel");
                    Assert.IsNotNull(station, barrel + " barrel station should render once.");
                    SpriteRenderer stationSprite = station.GetComponentInChildren<SpriteRenderer>();
                    Assert.IsNotNull(stationSprite);
                    Rect stationBounds = (Rect)Get(layout, barrel + "BarrelBounds");
                    Assert.AreEqual(stationBounds.width * .01f, stationSprite.bounds.size.x, .0001f,
                        barrel + " barrel sprite width should match the expanded hit/route footprint.");
                    Assert.AreEqual(stationBounds.height * .01f, stationSprite.bounds.size.y, .0001f,
                        barrel + " barrel sprite height should remain unchanged.");
                }
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
                    SpriteRenderer drinkSprite = view.GetComponentInChildren<SpriteRenderer>(true);
                    Assert.IsNotNull(drinkSprite);
                    Vector2 sourceSize = drinkSprite.sprite.bounds.size;
                    Vector2 target = (Vector2)GameType("StreetKitchenRenderer").GetMethod("ServingSize", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { product, false });
                    float fitScale = Mathf.Min(target.x * .01f / sourceSize.x, target.y * .01f / sourceSize.y);
                    Vector2 expectedSize = sourceSize * (fitScale * 100f);
                    Vector2 renderedSize = sourceSize * (view.transform.localScale.x * 100f);
                    Assert.AreEqual(expectedSize.x, renderedSize.x, .01f, "Barrel items should use the shared package scale and preserve sprite aspect ratio.");
                    Assert.AreEqual(expectedSize.y, renderedSize.y, .01f, "Barrel items should use the shared package scale and preserve sprite aspect ratio.");
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
