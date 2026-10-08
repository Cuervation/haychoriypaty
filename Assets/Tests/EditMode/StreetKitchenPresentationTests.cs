using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Focused contracts for the native prefab/resources and responsive route projection.</summary>
    public sealed class StreetKitchenPresentationTests
    {
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);

        private static object Simulation(int level)
        {
            object balance = Activator.CreateInstance(Runtime("StreetBalance"));
            return Activator.CreateInstance(Runtime("StreetSimulation"), new[] { balance, (object)level, 5f, 0, 1, 0 });
        }

        private static object Layout(object simulation) => Get(simulation, "KitchenLayout");
        private static object FullLayout() => Layout(Simulation(4));

        private static object Call(object target, string name, params object[] args)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (candidate.Name != name || candidate.GetParameters().Length != args.Length) continue;
                if (method != null) throw new AmbiguousMatchException(target.GetType().Name + "." + name);
                method = candidate;
            }
            if (method == null) throw new MissingMethodException(target.GetType().Name + "." + name);
            return method.Invoke(target, args);
        }

        private static object Get(object target, string name) => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        private static Rect RectProperty(object layout, string name) => (Rect)Runtime("StreetKitchenLayout").GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(layout);
        private static float LayoutConstant(string name) => (float)Runtime("StreetKitchenLayout").GetField(name, BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
        private static float SceneWidth() => (float)Runtime("StreetSceneLayout").GetField("Width", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static float WorkerServiceY() => (float)Runtime("StreetSceneLayout").GetField("WorkerServiceY", BindingFlags.Public | BindingFlags.Static).GetValue(null);

        [Test]
        public void AllElevenCatalogsExposeOnlyRealStockForTheirSevenProductProfile()
        {
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level);
                object kitchen = Get(sim, "Kitchen");
                var units = (System.Collections.IEnumerable)Get(kitchen, "Units");
                var ids = new HashSet<int>();
                int[] capacities = new int[7], grills = new int[7];
                for (int p = 0; p < 7; p++)
                {
                    capacities[p] = (int)Call(kitchen, "TableCapacity", p);
                    grills[p] = (int)Call(kitchen, "GrillCapacity", p);
                    bool available = (bool)Call(sim, "IsProductAvailable", p);
                    Assert.AreEqual(available, capacities[p] > 0 || grills[p] > 0, "L" + level + " product " + p);
                    if (p < 4) Assert.AreEqual(available, grills[p] > 0, "L" + level + " grill product " + p);
                }
                foreach (object unit in units)
                {
                    int id = (int)Get(unit, "Id"), product = (int)Get(unit, "Product");
                    Assert.IsTrue(ids.Add(id), "Duplicate food unit ID in level " + level);
                    Assert.That(product, Is.InRange(0, 6));
                    Assert.IsTrue((bool)Call(sim, "IsProductAvailable", product), "Locked product in level " + level);
                }
                for (int p = 0; p < 7; p++)
                {
                    int listedTable = 0, listedGrill = 0;
                    foreach (object unit in units)
                    {
                        if ((int)Get(unit, "Product") != p) continue;
                        string location = Get(unit, "Location").ToString();
                        if (location == "Table") listedTable++;
                        if (location == "Grill") listedGrill++;
                    }
                    Assert.AreEqual(capacities[p], listedTable, "L" + level + " initial table count for product " + p);
                    Assert.AreEqual(grills[p], listedGrill, "L" + level + " initial grill count for product " + p);
                }
                Assert.AreEqual(18, grills[0] + grills[1], "Normal grill has 18 real meat units when available.");
                if (grills[2] + grills[3] > 0) Assert.AreEqual(7, grills[2] + grills[3], "Premium grill has seven real meat units.");
            }
        }

        [Test]
        public void ActiveStationsReflowAcrossTheWidthWithConsistentSizesAndSafeGrillRows()
        {
            float width = SceneWidth();
            Rect? tableSize = null, barrelSize = null;
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim);
                bool normal = (bool)Call(sim, "IsProductAvailable", 0) || (bool)Call(sim, "IsProductAvailable", 1);
                bool premium = (bool)Call(sim, "IsProductAvailable", 2) || (bool)Call(sim, "IsProductAvailable", 3);
                bool fernet = (bool)Call(sim, "IsProductAvailable", 5);
                bool coca = (bool)Call(sim, "IsProductAvailable", 4);
                bool beer = (bool)Call(sim, "IsProductAvailable", 6);
                Assert.AreEqual(normal, Get(layout, "HasNormalGrill"));
                Assert.AreEqual(premium, Get(layout, "HasPremiumGrill"));
                Assert.AreEqual(normal, Get(layout, "HasNormalTable"));
                Assert.AreEqual(premium, Get(layout, "HasPremiumTable"));
                Assert.AreEqual(fernet, Get(layout, "HasFernetTable"));
                Assert.AreEqual(coca, Get(layout, "HasCocaBarrel"));
                Assert.AreEqual(beer, Get(layout, "HasBeerBarrel"));

                var top = new List<Rect>();
                if (normal) top.Add(RectProperty(layout, "NormalTableBounds"));
                if (premium) top.Add(RectProperty(layout, "PremiumTableBounds"));
                if (fernet) top.Add(RectProperty(layout, "FernetTableBounds"));
                if (coca) top.Add(RectProperty(layout, "CocaBarrelBounds"));
                if (beer) top.Add(RectProperty(layout, "BeerBarrelBounds"));
                float totalWidth = 0f;
                foreach (Rect item in top) totalWidth += item.width;
                float gap = (width - totalWidth) / (top.Count + 1);
                Assert.AreEqual(gap, top[0].xMin, .02f, "First active prep station should use the responsive row margin.");
                for (int i = 0; i < top.Count; i++)
                {
                    Assert.AreEqual(LayoutConstant("TableY"), top[i].y, .02f, "All active prep stations share the raised upper row.");
                    Assert.Greater(top[i].width, 0f);
                    if (i > 0) Assert.AreEqual(gap, top[i].xMin - top[i - 1].xMax, .02f, "Hidden stations must not leave row holes.");
                    if (top[i].width > 100f)
                    {
                        if (!tableSize.HasValue) tableSize = top[i];
                        Assert.AreEqual(tableSize.Value.size, top[i].size, "All active tables share one visual size.");
                    }
                    else
                    {
                        if (!barrelSize.HasValue) barrelSize = top[i];
                        Assert.AreEqual(barrelSize.Value.size, top[i].size, "All active barrels share one visual size.");
                    }
                }
                Assert.AreEqual(gap, width - top[top.Count - 1].xMax, .02f);

                var grills = new List<Rect>();
                if (normal) grills.Add(RectProperty(layout, "NormalGrillBounds"));
                if (premium) grills.Add(RectProperty(layout, "PremiumGrillBounds"));
                Assert.Greater(grills[0].width, 0f);
                foreach (Rect grill in grills) Assert.AreEqual(3f, grill.width / grill.height, .02f, "Grill sprites retain their authored aspect ratio.");
                if (grills.Count == 1)
                {
                    Assert.AreEqual(width * .5f, grills[0].center.x, .02f, "One active grill is centered, not stranded in its old left/right slot.");
                    Assert.Greater(grills[0].width, LayoutConstant("StandardGrillWidth"), "A lone grill grows into available width without covering the bottom UI band.");
                }
                else
                {
                    Assert.AreEqual(grills[0].width, grills[1].width, .02f);
                    Assert.AreEqual(grills[0].height, grills[1].height, .02f);
                    Assert.AreEqual(grills[0].y, grills[1].y, .02f);
                    Assert.Less(grills[0].xMax, grills[1].xMin);
                    Assert.LessOrEqual(grills[0].xMin, 10f);
                    Assert.GreaterOrEqual(grills[1].xMax, width - 10f);
                }
                foreach (Rect grill in grills)
                    Assert.LessOrEqual(grill.yMax, LayoutConstant("GrillBottomLimit") + .02f,
                        "Every grill must remain completely on the tiled playfield above the lower UI field.");
                foreach (Rect item in top) foreach (Rect grill in grills) Assert.IsFalse(item.Overlaps(grill));
            }
            Assert.AreEqual(196f * (width / (float)LayoutConstant("SourceWidth")), tableSize.Value.width, .02f);
            Assert.AreEqual(98f * (width / (float)LayoutConstant("SourceWidth")), tableSize.Value.height, .02f);
            Assert.AreEqual(72f * (width / (float)LayoutConstant("SourceWidth")), barrelSize.Value.width, .02f);
            Assert.AreEqual(117.6f * (width / (float)LayoutConstant("SourceWidth")), barrelSize.Value.height, .02f);
        }

        [Test]
        public void GrillMeatVisualBoundsStayInsideTheGrillForEveryLevel()
        {
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim), kitchen = Get(sim, "Kitchen");
                for (int product = 0; product < 4; product++)
                {
                    int capacity = (int)Call(kitchen, "GrillCapacity", product);
                    if (capacity <= 0) continue;
                    int companion = product % 2 == 0 ? product + 1 : product - 1;
                    bool companionAvailable = (bool)Call(sim, "IsProductAvailable", companion);
                    Rect grill = RectProperty(layout, product < 2 ? "NormalGrillBounds" : "PremiumGrillBounds");
                    Vector2 size = (Vector2)Call(layout, "GrillMeatSize", product, companionAvailable);
                    for (int slot = 0; slot < capacity; slot++)
                    {
                        Vector2 position = (Vector2)Call(layout, "GrillSlotPosition", product, slot, companionAvailable, capacity);
                        Rect meat = new Rect(position - size * .5f, size);
                        string context = "Level " + level + ", product " + product + ", slot " + slot;
                        float grateTop = grill.y + grill.height * .115f;
                        float grateBottom = grill.y + grill.height * .46f;
                        Assert.GreaterOrEqual(meat.yMin, grateTop - .02f, context + " behind the grate");
                        Assert.LessOrEqual(meat.yMax, grateBottom + .02f, context + " past the front grate edge");
                        float topDepth = Mathf.Clamp01((meat.yMin - grateTop) / (grateBottom - grateTop));
                        float bottomDepth = Mathf.Clamp01((meat.yMax - grateTop) / (grateBottom - grateTop));
                        float leftAtTop = Mathf.Lerp(.145f, .035f, topDepth);
                        float leftAtBottom = Mathf.Lerp(.145f, .035f, bottomDepth);
                        float rightAtTop = Mathf.Lerp(.855f, .965f, topDepth);
                        float rightAtBottom = Mathf.Lerp(.855f, .965f, bottomDepth);
                        float grateLeft = grill.x + grill.width * Mathf.Max(leftAtTop, leftAtBottom);
                        float grateRight = grill.x + grill.width * Mathf.Min(rightAtTop, rightAtBottom);
                        Assert.GreaterOrEqual(meat.xMin, grateLeft - .02f, context + " outside the tapered left grate edge");
                        Assert.LessOrEqual(meat.xMax, grateRight + .02f, context + " outside the tapered right grate edge");
                    }
                }
            }
        }

        [Test]
        public void BarrelDrinkSlotsScatterAcrossTheOpeningWithMixedStableAngles()
        {
            object layout = Layout(Simulation(4));
            Rect bounds = RectProperty(layout, "CocaBarrelBounds");
            var positions = new HashSet<Vector2>();
            var angles = new HashSet<float>();
            for (int slot = 0; slot < 12; slot++)
            {
                Vector2 position = (Vector2)Call(layout, "BarrelSlotPosition", 4, slot);
                float normalizedY = (position.y - bounds.y) / bounds.height;
                float normalizedX = (position.x - bounds.x) / bounds.width;
                Assert.That(normalizedX, Is.InRange(.15f, .86f));
                Assert.That(normalizedY, Is.InRange(.04f, .19f));
                positions.Add(position);
                angles.Add((float)Call(layout, "BarrelSlotRotation", slot));
            }
            Assert.AreEqual(12, positions.Count, "All twelve real inventory slots need their own visual position.");
            Assert.AreEqual(12, angles.Count, "Each drink receives a deterministic but distinct tossed angle.");
            Assert.IsTrue(angles.Contains(106f), "Some drinks should lie diagonally/sideways rather than all standing upright.");
            Assert.IsTrue(angles.Contains(-72f));
        }

        [Test]
        public void EveryCatalogRouteUsesItsDynamicStationBoundsAndClearsActiveBases()
        {
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level), layout = Layout(sim);
                var allBases = new List<Rect>();
                if ((bool)Get(layout, "HasNormalTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 0));
                if ((bool)Get(layout, "HasPremiumTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 2));
                if ((bool)Get(layout, "HasCocaBarrel")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 4));
                if ((bool)Get(layout, "HasFernetTable")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 5));
                if ((bool)Get(layout, "HasBeerBarrel")) allBases.Add((Rect)Call(layout, "FootprintForProduct", 6));
                if ((bool)Get(layout, "HasNormalGrill")) allBases.Add(RectProperty(layout, "NormalGrillBounds"));
                if ((bool)Get(layout, "HasPremiumGrill")) allBases.Add(RectProperty(layout, "PremiumGrillBounds"));

                for (int product = 0; product < 7; product++)
                {
                    if (!(bool)Call(sim, "IsProductAvailable", product)) continue;
                    Vector2[] route = (Vector2[])Call(layout, "ApproachRoute", product);
                    Assert.GreaterOrEqual(route.Length, 2);
                    Assert.AreEqual((Vector2)Call(layout, "PickupPosition", product), route[route.Length - 1]);
                    for (int column = 0; column < 7; column++)
                    {
                        Vector2 from = new Vector2(58 + column * 70, WorkerServiceY());
                        foreach (Vector2 to in route)
                        {
                            for (int step = 0; step <= 100; step++)
                            {
                                Vector2 feet = Vector2.Lerp(from, to, step / 100f);
                                Rect footBounds = new Rect(feet.x - 18f, feet.y - 12f, 36f, 12f);
                                foreach (Rect baseRect in allBases)
                                    Assert.IsFalse(footBounds.Overlaps(baseRect), "L" + level + " P" + product + " feet " + footBounds + " base " + baseRect);
                            }
                            from = to;
                        }
                    }
                }
            }
        }

        [Test]
        public void SelectingAndAdvancingLevelRefreshesTheCatalogDrivenKitchenLayout()
        {
            object sim = Simulation(0);
            Assert.IsFalse((bool)Get(Layout(sim), "HasCocaBarrel"));
            Assert.IsTrue((bool)Call(sim, "SelectLevel", 2));
            object selectedLayout = Layout(sim);
            Assert.IsTrue((bool)Get(selectedLayout, "HasCocaBarrel"));
            Assert.IsFalse((bool)Get(selectedLayout, "HasPremiumGrill"));

            PropertyInfo phase = Runtime("StreetSimulation").GetProperty("Phase", BindingFlags.Public | BindingFlags.Instance);
            phase.GetSetMethod(true).Invoke(sim, new[] { Enum.Parse(phase.PropertyType, "Won") });
            Assert.IsTrue((bool)Call(sim, "NextLevel"));
            Assert.AreEqual(3, Get(sim, "LevelIndex"));
            object nextLayout = Layout(sim);
            Assert.IsTrue((bool)Get(nextLayout, "HasPremiumGrill"));
            Assert.IsTrue((bool)Get(nextLayout, "HasBeerBarrel"));
            Assert.IsFalse((bool)Get(nextLayout, "HasFernetTable"));
        }

        [Test]
        public void TableAndGrillSlotsDoNotMixTheTwoMeatFamilies()
        {
            object layout = FullLayout();
            AssertSlotsSeparated(layout, 0, 1, 24, new Vector2(20f, 14f), new Vector2(20f, 14f));
            AssertSlotsSeparated(layout, 2, 3, 18, new Vector2(20f, 14f), new Vector2(20f, 14f));
            AssertSlotsSeparated(layout, 0, 1, 18, new Vector2(36.4f, 14f), new Vector2(34f, 20f), true);
            AssertSlotsSeparated(layout, 2, 3, 7, new Vector2(34f, 38f), new Vector2(90f, 14f), true);
        }

        private static void AssertSlotsSeparated(object layout, int first, int second, int count, Vector2 firstSize, Vector2 secondSize, bool grill = false)
        {
            var firstRects = new List<Rect>();
            var secondRects = new List<Rect>();
            for (int i = 0; i < count; i++)
            {
                Vector2 a = grill ? (Vector2)Call(layout, "GrillSlotPosition", first, i, true, first < 2 ? 12 : 4)
                    : (Vector2)Call(layout, "TableSlotPosition", first, i, true, count);
                Vector2 b = grill ? (Vector2)Call(layout, "GrillSlotPosition", second, i, true, second < 2 ? 6 : 3)
                    : (Vector2)Call(layout, "TableSlotPosition", second, i, true, count);
                if (grill)
                {
                    firstSize = (Vector2)Call(layout, "GrillMeatSize", first, true);
                    secondSize = (Vector2)Call(layout, "GrillMeatSize", second, true);
                }
                int firstLimit = grill ? (first == 0 ? 12 : first == 1 ? 6 : first == 2 ? 4 : 3) : count;
                int secondLimit = grill ? (second == 0 ? 12 : second == 1 ? 6 : second == 2 ? 4 : 3) : count;
                if (i < firstLimit) firstRects.Add(new Rect(a - firstSize * .5f, firstSize));
                if (i < secondLimit) secondRects.Add(new Rect(b - secondSize * .5f, secondSize));
            }
            foreach (Rect a in firstRects) foreach (Rect b in secondRects)
                Assert.IsFalse(a.Overlaps(b), "Cross-family grill/table slots overlap for products " + first + "/" + second + ": " + a + " vs " + b);
        }

        [Test]
        public void FourProductionSubstatesProjectToThreeWarmVisualStates()
        {
            Type stateType = Runtime("StreetFoodState");
            MethodInfo visualState = Runtime("StreetKitchenRenderer").GetMethod("VisualStateName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(visualState);
            Func<string, string> project = state => (string)visualState.Invoke(null, new[] { Enum.Parse(stateType, state) });
            Assert.AreEqual("Raw", project("Raw"));
            Assert.AreEqual("Raw", project("Cooking"), "Cooking remains a timer stage, not a fourth visual state.");
            Assert.AreEqual("Cooked", project("Cooked"));
            Assert.AreEqual("Passed", project("Burned"));
            MethodInfo spriteState = Runtime("StreetKitchenRenderer").GetMethod("ArtStateName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(spriteState);
            Assert.AreEqual("Cooked", spriteState.Invoke(null, new object[] { "Passed" }),
                "Passed must reuse warm cooked art, not the black-char frame.");
        }

        [Test]
        public void IndependentResourcesKeepCookingSourceFramesAndSeparateStationPrefabs()
        {
            string[] meat = { "meat-chori", "meat-paty", "meat-bondiola", "meat-vacio" };
            string[] states = { "Raw", "Cooking", "Cooked", "Burned" };
            foreach (string name in meat)
            {
                string path = "Assets/Resources/ModularKitchen/Art/" + name + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "Original sprite aspect must not be resampled to a different power-of-two ratio.");
                Assert.IsTrue(importer.mipmapEnabled, "High resolution food minification needs mipmaps to avoid sparkle.");
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
                Sprite[] sprites = Resources.LoadAll<Sprite>("ModularKitchen/Sprites/" + name);
                Assert.AreEqual(4, sprites.Length, name);
                Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                Assert.IsTrue(ImageConversion.LoadImage(decoded, File.ReadAllBytes(path)), path);
                int cell = decoded.width / 4;
                Assert.AreEqual(decoded.width, sprites[0].texture.width);
                Assert.AreEqual(decoded.height, sprites[0].texture.height);
                for (int state = 0; state < 4; state++)
                {
                    string expectedName = name + "_" + states[state];
                    Sprite found = Array.Find(sprites, s => s.name == expectedName);
                    Assert.NotNull(found, expectedName);
                    Assert.Greater(found.rect.width, cell * .5f, expectedName + " should use most of its authored cell.");
                    Assert.Less(found.rect.width, cell, expectedName + " should trim alpha margin without altering source PNG.");
                    string spriteAssetPath = "Assets/Resources/ModularKitchen/Sprites/" + name + "/" + expectedName + ".asset";
                    Assert.AreSame(found, AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath), spriteAssetPath);
                    Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(spriteAssetPath), spriteAssetPath);
                    Assert.IsTrue(ContainsOpaquePixel(decoded, state * cell, (state + 1) * cell), expectedName);
                }
                UnityEngine.Object.DestroyImmediate(decoded);
            }

            AssertPrefab("Station_NormalGrill", "empty-grill", 1);
            AssertPrefab("Station_PremiumGrill", "empty-grill", 1);
            AssertPrefab("Station_NormalTable", "empty-table", 1);
            AssertPrefab("Station_PremiumTable", "empty-table", 1);
            AssertPrefab("Station_FernetTable", "empty-table", 1);
            AssertPrefab("Station_BeerBarrel", "blue-barrel", 2);
            AssertPrefab("Station_CocaBarrel", "blue-barrel", 2);
            string[] singles = { "empty-grill", "empty-table", "blue-barrel", "sandwich-chori", "sandwich-paty", "sandwich-bondiola", "sandwich-vacio", "coca", "beer", "fernet", "ice", "smoke", "heat" };
            foreach (string name in singles)
            {
                string path = "Assets/Resources/ModularKitchen/Art/" + name + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "Original sprite aspect must not be resampled to a different power-of-two ratio.");
                Assert.IsTrue(importer.mipmapEnabled, "High resolution food minification needs mipmaps to avoid sparkle.");
                Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
                Assert.AreEqual(1, Resources.LoadAll<Sprite>("ModularKitchen/Sprites/" + name).Length, name);
            }
            foreach (string name in new[] { "Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio", "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio", "Drink_Coca", "Drink_Beer", "Drink_Fernet", "Prop_Ice", "Effect_Smoke", "Effect_Heat" })
            {
                GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + name);
                Assert.NotNull(prefab, name);
                SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.Greater(renderers.Length, 0, name);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Assert.NotNull(renderers[i].sprite, name + " missing sprite.");
                    Assert.NotNull(renderers[i].sharedMaterial, name + " missing default sprite material.");
                    Assert.NotNull(renderers[i].sharedMaterial.shader, name + " missing sprite shader.");
                    Assert.IsTrue(renderers[i].sharedMaterial.shader.isSupported, name + " sprite shader is unsupported.");
                }
            }
        }

        private static bool ContainsOpaquePixel(Texture2D texture, int xMin, int xMax)
        {
            for (int y = 0; y < texture.height; y++)
            for (int x = xMin; x < xMax; x++) if (texture.GetPixel(x, y).a > .05f) return true;
            return false;
        }

        private static void AssertPrefab(string prefabName, string spriteName, int expectedRendererCount)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefabName);
            Assert.NotNull(prefab, prefabName);
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(expectedRendererCount, renderers.Length, prefabName + " must have independent station artwork only.");
            Assert.AreEqual(spriteName, renderers[0].sprite.name, prefabName);
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.NotNull(renderers[i].sharedMaterial, prefabName + " missing default sprite material.");
                Assert.NotNull(renderers[i].sharedMaterial.shader, prefabName + " missing sprite shader.");
                Assert.IsTrue(renderers[i].sharedMaterial.shader.isSupported, prefabName + " sprite shader is unsupported by the active render pipeline.");
            }
        }
    }
}
