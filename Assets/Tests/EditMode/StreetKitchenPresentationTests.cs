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

        private static object Static(string name, params object[] args)
        {
            MethodInfo method = Runtime("StreetKitchenLayout").GetMethod(name, BindingFlags.Public | BindingFlags.Static, null,
                Array.ConvertAll(args, x => x.GetType()), null);
            if (method == null) throw new MissingMethodException("StreetKitchenLayout." + name);
            return method.Invoke(null, args);
        }

        private static object Call(object target, string name, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (method == null) throw new MissingMethodException(target.GetType().Name + "." + name);
            return method.Invoke(target, args);
        }

        private static object Get(object target, string name) => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        private static Rect RectProperty(string name) => (Rect)Runtime("StreetKitchenLayout").GetProperty(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
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
        public void FiveOrderedStationsAndTwoGrillsRetainTheirAspectAndSafeRows()
        {
            Rect beer = RectProperty("BeerBarrelBounds"), coca = RectProperty("CocaBarrelBounds");
            Rect normal = RectProperty("NormalTableBounds"), premium = RectProperty("PremiumTableBounds"), fernet = RectProperty("FernetTableBounds");
            Rect normalGrill = RectProperty("NormalGrillBounds"), premiumGrill = RectProperty("PremiumGrillBounds");
            Assert.Less(beer.xMax, coca.xMin); Assert.Less(coca.xMax, normal.xMin);
            Assert.Less(normal.xMax, premium.xMin); Assert.Less(premium.xMax, fernet.xMin);
            Assert.AreEqual(normal.size, premium.size); Assert.AreEqual(normal.size, fernet.size);
            Assert.AreEqual(normalGrill.width, premiumGrill.width); Assert.AreEqual(normalGrill.height, premiumGrill.height);
            Assert.AreEqual(520f, normal.y); Assert.AreEqual(620f, normalGrill.y);
            Assert.AreEqual(30f, normalGrill.y - normal.yMax, .5f);
            Assert.Less(normalGrill.yMax, 716f, "Kitchen must not cover the upgrade/status band.");
            Assert.GreaterOrEqual(normal.y - WorkerServiceY(), 120f);
            Rect[] furniture = { beer, coca, normal, premium, fernet, normalGrill, premiumGrill };
            for (int i = 0; i < furniture.Length; i++)
                for (int j = 0; j < i; j++) Assert.IsFalse(furniture[i].Overlaps(furniture[j]), i + " overlaps " + j);
        }

        [Test]
        public void EveryCatalogRouteKeepsTheWholeFootRectangleClearOfVisibleStationBases()
        {
            Rect[] allBases = {
                (Rect)Static("FootprintForProduct", 0), (Rect)Static("FootprintForProduct", 2),
                (Rect)Static("FootprintForProduct", 4), (Rect)Static("FootprintForProduct", 5),
                (Rect)Static("FootprintForProduct", 6), RectProperty("NormalGrillBounds"), RectProperty("PremiumGrillBounds")
            };
            for (int level = 0; level < 11; level++)
            {
                object sim = Simulation(level);
                for (int product = 0; product < 7; product++)
                {
                    if (!(bool)Call(sim, "IsProductAvailable", product)) continue;
                    Vector2[] route = (Vector2[])Static("ApproachRoute", product);
                    Assert.GreaterOrEqual(route.Length, 2);
                    Assert.AreEqual((Vector2)Static("PickupPosition", product), route[route.Length - 1]);
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
        public void TableAndGrillSlotsDoNotMixTheTwoMeatFamilies()
        {
            AssertSlotsSeparated(0, 1, 24, new Vector2(20f, 14f), new Vector2(20f, 14f));
            AssertSlotsSeparated(2, 3, 18, new Vector2(20f, 14f), new Vector2(20f, 14f));
            AssertSlotsSeparated(0, 1, 18, new Vector2(28f, 14f), new Vector2(34f, 20f), true);
            AssertSlotsSeparated(2, 3, 7, new Vector2(34f, 38f), new Vector2(90f, 14f), true);
        }

        private static void AssertSlotsSeparated(int first, int second, int count, Vector2 firstSize, Vector2 secondSize, bool grill = false)
        {
            var firstRects = new List<Rect>();
            var secondRects = new List<Rect>();
            for (int i = 0; i < count; i++)
            {
                Vector2 a = grill ? (Vector2)Static("GrillSlotPosition", first, i, true, first < 2 ? 12 : 4)
                    : (Vector2)Static("TableSlotPosition", first, i, true, count);
                Vector2 b = grill ? (Vector2)Static("GrillSlotPosition", second, i, true, second < 2 ? 6 : 3)
                    : (Vector2)Static("TableSlotPosition", second, i, true, count);
                int firstLimit = grill ? (first == 0 ? 12 : first == 1 ? 6 : first == 2 ? 4 : 3) : count;
                int secondLimit = grill ? (second == 0 ? 12 : second == 1 ? 6 : second == 2 ? 4 : 3) : count;
                if (i < firstLimit) firstRects.Add(new Rect(a - firstSize * .5f, firstSize));
                if (i < secondLimit) secondRects.Add(new Rect(b - secondSize * .5f, secondSize));
            }
            foreach (Rect a in firstRects) foreach (Rect b in secondRects) Assert.IsFalse(a.Overlaps(b), "Cross-family slots overlap.");
        }

        [Test]
        public void IndependentResourcesContainFourTrimmedCookingFramesAndSeparateStationPrefabs()
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
