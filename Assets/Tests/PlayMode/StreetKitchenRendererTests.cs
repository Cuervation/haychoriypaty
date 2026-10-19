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
        private static object CallExact(object value, string name, Type[] parameterTypes, params object[] args) =>
            value.GetType().GetMethod(name, Instance, null, parameterTypes, null).Invoke(value, args);

        [TestCase(0, false, 31.2f, 16.8f)]
        [TestCase(1, false, 24f, 16.8f)]
        [TestCase(2, false, 26.4f, 18.48f)]
        [TestCase(3, false, 26.4f, 18.48f)]
        [TestCase(0, true, 39f, 22.5f)]
        [TestCase(1, true, 30f, 22.5f)]
        [TestCase(2, true, 33f, 24.75f)]
        [TestCase(3, true, 33f, 24.75f)]
        [TestCase(4, false, 9f, 20f)]
        [TestCase(5, false, 18f, 28f)]
        public void SandwichesUseLargerAspectPreservingDisplayTargetsWithoutScalingDrinks(
            int product, bool carried, float expectedWidth, float expectedHeight)
        {
            MethodInfo method = GameType("StreetKitchenRenderer").GetMethod("ServingSize", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2 size = (Vector2)method.Invoke(null, new object[] { product, carried });
            Assert.AreEqual(expectedWidth, size.x, .0001f);
            Assert.AreEqual(expectedHeight, size.y, .0001f);
        }

        [UnityTest]
        public IEnumerator ActiveGrillsStayVisuallyFullWithoutCreatingFoodAndRealCookOwnsItsSlot()
        {
            var root = new GameObject("Active grill ambient meat projection test");
            try
            {
                Component renderer = root.AddComponent(GameType("StreetKitchenRenderer"));
                IDictionary ambient = (IDictionary)renderer.GetType()
                    .GetField("ambientGrillMeat", Instance).GetValue(renderer);
                IDictionary foodObjects = (IDictionary)renderer.GetType()
                    .GetField("foodObjects", Instance).GetValue(renderer);
                object balance = Activator.CreateInstance(GameType("StreetBalance"));

                object early = Activator.CreateInstance(GameType("StreetSimulation"),
                    new object[] { balance, 4, 5f, 0, 1, 0 });
                int earlyUnitCount = ((IList)Get(Get(early, "Kitchen"), "Units")).Count;
                Call(renderer, "Prepare", early, 1f, 960f);
                Assert.IsFalse((bool)Get(early, "UsesActiveCooks"));
                Assert.AreEqual(0, ambient.Count, "Levels 1–5 keep their original real-meat grill pipeline.");
                Assert.AreEqual(earlyUnitCount, ((IList)Get(Get(early, "Kitchen"), "Units")).Count);
                Assert.GreaterOrEqual(foodObjects.Count, 25, "Earlier levels still render real grill units.");

                object active = null;
                for (int level = 5; level <= 10; level++)
                {
                    active = Activator.CreateInstance(GameType("StreetSimulation"),
                        new object[] { balance, level, 5f, 0, 1, 0 });
                    object kitchen = Get(active, "Kitchen");
                    int unitCount = ((IList)Get(kitchen, "Units")).Count;
                    int coins = (int)Get(active, "Coins");
                    Call(renderer, "Prepare", active, 1f, 960f);

                    Assert.IsTrue((bool)Get(active, "UsesActiveCooks"));
                    Assert.AreEqual(25, ambient.Count, "Racing through level 11 has 18 normal + 7 Premium slots.");
                    Assert.AreEqual(12, (int)Call(kitchen, "GrillCapacity", 0));
                    Assert.AreEqual(6, (int)Call(kitchen, "GrillCapacity", 1));
                    Assert.AreEqual(4, (int)Call(kitchen, "GrillCapacity", 2));
                    Assert.AreEqual(3, (int)Call(kitchen, "GrillCapacity", 3));

                    bool hasRaw = false, hasCooking = false, hasCooked = false;
                    for (int product = 0; product < 4; product++)
                    {
                        int capacity = (int)Call(kitchen, "GrillCapacity", product);
                        bool companion = (bool)Call(active, "IsProductAvailable", product ^ 1);
                        for (int slot = 0; slot < capacity; slot++)
                        {
                            int key = product * 100 + slot;
                            Assert.IsTrue(ambient.Contains(key), "Every available physical grill slot has one pooled visual.");
                            object visual = ambient[key];
                            var go = (GameObject)visual.GetType().GetField("Object", Instance).GetValue(visual);
                            var spriteRenderer = (SpriteRenderer)visual.GetType().GetField("Renderer", Instance).GetValue(visual);
                            Assert.IsTrue(go.activeSelf, "Unused active-cook slots stay visually filled.");
                            Vector2 point = (Vector2)CallExact(Get(active, "KitchenLayout"), "GrillSlotPosition",
                                new[] { typeof(int), typeof(int), typeof(bool), typeof(int) }, product, slot, companion, capacity);
                            Assert.AreEqual((point.x - 270f) * .01f, go.transform.localPosition.x, .0001f);
                            Assert.AreEqual((480f - point.y) * .01f, go.transform.localPosition.y, .0001f);
                            string spriteName = spriteRenderer.sprite.name;
                            hasRaw |= spriteName.EndsWith("_Raw", StringComparison.OrdinalIgnoreCase);
                            hasCooking |= spriteName.EndsWith("_Cooking", StringComparison.OrdinalIgnoreCase);
                            hasCooked |= spriteName.EndsWith("_Cooked", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    Assert.IsTrue(hasRaw && hasCooking && hasCooked,
                        "Ambient positions begin in a desynchronized mix of raw, cooking, and cooked art.");
                    Assert.AreEqual(unitCount, ((IList)Get(kitchen, "Units")).Count,
                        "Preparing and updating decorative visuals cannot create StreetFoodUnit inventory.");
                    Assert.AreEqual(coins, (int)Get(active, "Coins"), "Ambient visuals never change currency.");

                    int activeAmbientCount = 0;
                    foreach (DictionaryEntry entry in ambient)
                    {
                        object visual = entry.Value;
                        GameObject go = (GameObject)visual.GetType().GetField("Object", Instance).GetValue(visual);
                        if (go.activeSelf) activeAmbientCount++;
                    }
                    Assert.AreEqual(25, activeAmbientCount,
                        "Each active cook level shows every available grill slot when no real unit occupies it.");
                    yield return null;
                }

                var initialStates = new string[ambient.Count];
                int stateIndex = 0;
                foreach (DictionaryEntry entry in ambient)
                {
                    object visual = entry.Value;
                    SpriteRenderer sr = (SpriteRenderer)visual.GetType().GetField("Renderer", Instance).GetValue(visual);
                    initialStates[stateIndex++] = sr.sprite.name;
                }
                yield return new WaitForSecondsRealtime(2.3f);
                Call(renderer, "Prepare", active, 1f, 960f);
                int changedStates = 0;
                float earliestNextState = float.MaxValue, latestNextState = float.MinValue;
                stateIndex = 0;
                foreach (DictionaryEntry entry in ambient)
                {
                    object visual = entry.Value;
                    SpriteRenderer sr = (SpriteRenderer)visual.GetType().GetField("Renderer", Instance).GetValue(visual);
                    if (initialStates[stateIndex++] != sr.sprite.name) changedStates++;
                    float nextState = (float)visual.GetType().GetField("NextStateAt", Instance).GetValue(visual);
                    earliestNextState = Mathf.Min(earliestNextState, nextState);
                    latestNextState = Mathf.Max(latestNextState, nextState);
                }
                Assert.Greater(changedStates, 0, "The independent visual cycles advance without a gameplay tick.");
                Assert.Greater(latestNextState - earliestNextState, .05f,
                    "Per-slot state deadlines remain staggered instead of synchronized.");

                object noStock = Activator.CreateInstance(GameType("StreetSimulation"),
                    new object[] { balance, 5, 5f, 0, 1, 0 });
                object marketData = Activator.CreateInstance(GameType("StreetMarketData"));
                object market = Activator.CreateInstance(GameType("StreetMarket"), new object[] { marketData, balance });
                Call(noStock, "BindIngredientStock", market);
                Call(noStock, "StartRound");
                object noStockKitchen = Get(noStock, "Kitchen");
                Assert.IsFalse((bool)Call(noStockKitchen, "CanReserveCook", 0));
                int noStockUnits = ((IList)Get(noStockKitchen, "Units")).Count;
                Call(renderer, "Prepare", noStock, 1f, 960f);
                Assert.AreEqual(25, ambient.Count, "No ingredient stock does not make the grill look empty.");
                Assert.AreEqual(noStockUnits, ((IList)Get(noStockKitchen, "Units")).Count);
                yield return null;

                object fullTable = Activator.CreateInstance(GameType("StreetSimulation"),
                    new object[] { balance, 5, 5f, 0, 1, 0 });
                object fullTableKitchen = Get(fullTable, "Kitchen");
                int tableCapacity = (int)Call(fullTableKitchen, "TableCapacity", 0);
                Type foodState = GameType("StreetFoodState"), foodLocation = GameType("StreetFoodLocation");
                for (int slot = 0; slot < tableCapacity; slot++)
                    Call(fullTableKitchen, "Add", 0, Enum.Parse(foodState, "Cooked"),
                        Enum.Parse(foodLocation, "Table"), slot);
                Assert.IsFalse((bool)Call(fullTableKitchen, "CanReserveCook", 0));
                int fullTableUnits = ((IList)Get(fullTableKitchen, "Units")).Count;
                Call(renderer, "Prepare", fullTable, 1f, 960f);
                Assert.AreEqual(25, ambient.Count, "Full sandwich tables still leave ambient grill meat visible.");
                Assert.AreEqual(fullTableUnits, ((IList)Get(fullTableKitchen, "Units")).Count);
                yield return null;

                object realSimulation = Activator.CreateInstance(GameType("StreetSimulation"),
                    new object[] { balance, 10, 5f, 0, 1, 0 });
                object realKitchen = Get(realSimulation, "Kitchen");
                int baselineUnits = ((IList)Get(realKitchen, "Units")).Count;
                int baselineTableFood = (int)Call(realKitchen, "TableCount", 0);
                int workerId = (int)Get(((IList)Get(realSimulation, "Workers"))[0], "Id");
                Call(renderer, "Prepare", realSimulation, 1f, 960f);
                object realUnit = CallExact(realKitchen, "ReserveCook",
                    new[] { typeof(int), typeof(int), typeof(int) }, 0, workerId, 0);
                Assert.IsNotNull(realUnit);
                int realId = (int)Get(realUnit, "Id");
                Call(renderer, "Prepare", realSimulation, 1f, 960f);
                object realVisual = ambient[0];
                GameObject ambientObject = (GameObject)realVisual.GetType().GetField("Object", Instance).GetValue(realVisual);
                Assert.IsFalse(ambientObject.activeSelf, "The real meat exclusively occupies its physical slot.");
                Assert.IsTrue(foodObjects.Contains(realId));
                Assert.IsTrue(((GameObject)foodObjects[realId]).activeSelf);
                Assert.AreEqual(baselineUnits + 1, ((IList)Get(realKitchen, "Units")).Count);
                Assert.AreEqual(baselineTableFood, (int)Call(realKitchen, "TableCount", 0));
                Assert.AreEqual(0, (int)Get(realSimulation, "Coins"));

                Assert.IsTrue((bool)Call(realKitchen, "AdvanceCook", realId, workerId, 100f, 1f));
                Assert.AreEqual(1f, (float)Get(realUnit, "Progress"), .0001f,
                    "Renderer synchronization does not reset real cook progress.");
                Assert.IsTrue((bool)Call(realKitchen, "TakeCooked", realId, workerId));
                Call(renderer, "Prepare", realSimulation, 1f, 960f);
                Assert.IsTrue(ambientObject.activeSelf, "The ambient piece returns as soon as the real unit leaves the grill.");
                Assert.IsTrue(foodObjects.Contains(realId), "The actual carried sandwich remains rendered.");
                Assert.IsTrue((bool)Call(realKitchen, "Consume", realId, 0, workerId));
                Call(renderer, "Prepare", realSimulation, 1f, 960f);
                Assert.IsTrue(ambientObject.activeSelf);
                Assert.IsFalse(foodObjects.Contains(realId));
                Assert.AreEqual(baselineUnits, ((IList)Get(realKitchen, "Units")).Count);
                Assert.AreEqual(baselineTableFood, (int)Call(realKitchen, "TableCount", 0));
                Assert.AreEqual(0, (int)Get(realSimulation, "Coins"));
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
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
                    Assert.IsTrue(bounds.Contains(position), "Each initial meat unit must remain within its active grill bounds.");
                    float expectedY = (height * .5f - position.y * verticalScale) * .01f;
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
                Assert.AreEqual((height * .5f - (feet.y * verticalScale - 23.5f)) * .01f, carried.transform.localPosition.y, .0001f,
                    "The fallback carried item is drawn lower at the worker's hand height.");
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
        public IEnumerator FernetTableDisplayKeepsTwoRowsSupportedAndProportional()
        {
            var root = new GameObject("Fernet tabletop bottle display test");
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
                Call(renderer, "EnsureFernetTableDisplay", station);

                Transform display = station.transform.Find("FernetBottleDisplay");
                Assert.IsNotNull(display);
                Assert.AreEqual(8, display.childCount, "The tabletop display uses four bottles in each of two rows.");
                float tableHeight = table.sprite.bounds.size.y;
                float groupRight = table.sprite.bounds.min.x + table.sprite.bounds.size.x * .64f;
                int[] cocaByRow = new int[2], fernetByRow = new int[2];
                float[] rearY = { float.NaN, float.NaN };
                for (int i = 0; i < display.childCount; i++)
                {
                    Transform bottle = display.GetChild(i);
                    SpriteRenderer bottleRenderer = bottle.GetComponent<SpriteRenderer>();
                    bool fernet = bottle.name.StartsWith("FernetBrancaBottle");
                    int row = bottle.name.Contains("Row0_") ? 0 : 1;
                    Assert.IsTrue(row == 0 || bottle.name.Contains("Row1_"), "Every stock bottle belongs to a known tabletop row.");
                    if (fernet) fernetByRow[row]++; else cocaByRow[row]++;
                    float expectedHeight = tableHeight * (fernet ? .25f * 3f * .93f : .333f * 3f * .76f * .95f);
                    float height = bottleRenderer.sprite.bounds.size.y * bottle.localScale.y;
                    Assert.AreEqual(expectedHeight, height, .001f, "Fernet and Coca keep their requested relative visual sizes.");
                    Assert.AreEqual(bottle.localScale.x, bottle.localScale.y, .0001f,
                        "Bottle sprites must preserve their original aspect ratios.");
                    Vector2 bottleSize = bottleRenderer.sprite.bounds.size * bottle.localScale.x;
                    Assert.GreaterOrEqual(bottle.localPosition.x - bottleSize.x * .5f, table.sprite.bounds.min.x - .001f);
                    Assert.LessOrEqual(bottle.localPosition.x + bottleSize.x * .5f, groupRight + .001f,
                        "Stock bottles must remain in the left tabletop zone.");
                    float depthFraction = row == 0 ? .03f : .32f;
                    float tabletopY = table.sprite.bounds.max.y - tableHeight * depthFraction;
                    float visibleInset = fernet ? 0f : bottleRenderer.sprite.bounds.size.y * .009f * bottle.localScale.y;
                    Assert.AreEqual(tabletopY - visibleInset, bottle.localPosition.y - height * .5f, .001f,
                        "Each row's bottle bases must sit on its tabletop depth plane.");
                    rearY[row] = bottle.localPosition.y;
                    for (int other = 0; other < i; other++)
                    {
                        Transform previous = display.GetChild(other);
                        if (!previous.name.Contains("Row" + row + "_")) continue;
                        SpriteRenderer previousRenderer = previous.GetComponent<SpriteRenderer>();
                        float separation = Mathf.Abs(bottle.localPosition.x - previous.localPosition.x);
                        float required = (bottleSize.x + previousRenderer.sprite.bounds.size.x * previous.localScale.x) * .5f;
                        Assert.GreaterOrEqual(separation + .001f, required, "Bottles in each row must not overlap horizontally.");
                    }
                }
                for (int row = 0; row < 2; row++)
                {
                    Assert.AreEqual(2, cocaByRow[row], "Each row has two Coca bottles.");
                    Assert.AreEqual(2, fernetByRow[row], "Each row has two Fernet bottles.");
                }
                Assert.Greater(rearY[0], rearY[1], "The rear row is staggered behind the front row.");
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
                    float baseScale = Mathf.Min(8f * .01f / sourceSize.x, 15f * .01f / sourceSize.y);
                    Vector2 expectedSize = sourceSize * (baseScale * 1.35f * 1.08f * 100f);
                    Vector2 renderedSize = sourceSize * (view.transform.localScale.x * 100f);
                    Assert.AreEqual(expectedSize.x, renderedSize.x, .01f, "Barrel Coca/beer should be 35% larger while preserving each sprite's aspect ratio.");
                    Assert.AreEqual(expectedSize.y, renderedSize.y, .01f, "Barrel Coca/beer should be 35% larger while preserving each sprite's aspect ratio.");
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
