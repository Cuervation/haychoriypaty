using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class WholeBondiolaArtTests
    {
        const string Resource = "ModularKitchen/Art/meat-bondiola-whole-v1_";
        const string VacioResource = "ModularKitchen/Art/meat-vacio-whole-v1_";
        static Type RendererType => Type.GetType("HayChoriYPaty.StreetKitchenRenderer, Assembly-CSharp", true);
        const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase("Raw")] [TestCase("Cooking")] [TestCase("Cooked")]
        public void WholeBondiolaStateKeepsFullHorizontalSpriteAndTransparentMargins(string state)
        {
            Sprite sprite = Resources.Load<Sprite>(Resource + state);
            Assert.NotNull(sprite);
            Assert.AreEqual(new Rect(0, 0, sprite.texture.width, sprite.texture.height), sprite.rect);
            Assert.AreEqual(sprite.rect.size * .5f, sprite.pivot);
            Assert.That(sprite.rect.width / sprite.rect.height, Is.InRange(2f, 2.2f));
            string path = "Assets/Resources/" + Resource + state + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.IsTrue(importer.alphaIsTransparency); Assert.IsTrue(importer.mipmapEnabled);
            var decoded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(path)));
                Color32[] pixels = decoded.GetPixels32(); int width = decoded.width, height = decoded.height, opaque = 0;
                for (int i = 0; i < pixels.Length; i++) if (pixels[i].a > 240) opaque++;
                Assert.Greater(opaque, pixels.Length / 3);
                for (int x = 0; x < width; x++)
                {
                    Assert.LessOrEqual(pixels[x].a, 10); Assert.LessOrEqual(pixels[(height - 1) * width + x].a, 10);
                }
                for (int y = 0; y < height; y++)
                {
                    Assert.LessOrEqual(pixels[y * width].a, 10); Assert.LessOrEqual(pixels[y * width + width - 1].a, 10);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }

        [Test]
        public void CookedWholeBondiolaIsTheExactUserApprovedImage()
        {
            CollectionAssert.AreEqual(File.ReadAllBytes("ConceptArt/Bondiola/bondiola-entera-cocida-propuesta-v1.png"),
                File.ReadAllBytes("Assets/Resources/" + Resource + "Cooked.png"));
        }

        [TestCase("Raw", "Raw")] [TestCase("Cooking", "Cooking")]
        [TestCase("Cooked", "Cooked")] [TestCase("Passed", "Cooked")]
        public void BondiolaSelectorUsesWholeArtAndKeepsPassedWarm(string requested, string expected)
        {
            var root = new GameObject("Whole Bondiola selector regression");
            try
            {
                Component renderer = root.AddComponent(RendererType);
                MethodInfo method = RendererType.GetMethod("StateSprite", P);
                var sprite = (Sprite)method.Invoke(renderer, new object[] { "meat-bondiola", requested });
                Assert.AreSame(Resources.Load<Sprite>(Resource + expected), sprite);
                Assert.AreSame(sprite, method.Invoke(renderer, new object[] { "meat-bondiola", requested }), "Use cached imported sprites, not recreated runtime copies.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(2, "Raw", "Raw")] [TestCase(2, "Cooking", "Cooking")]
        [TestCase(2, "Cooked", "Cooked")] [TestCase(2, "Burned", "Passed")]
        [TestCase(3, "Raw", "Raw")] [TestCase(3, "Cooking", "Cooking")]
        [TestCase(3, "Cooked", "Cooked")] [TestCase(3, "Burned", "Passed")]
        [TestCase(0, "Cooking", "Raw")] [TestCase(1, "Cooking", "Raw")]
        public void WholeBondiolaCookingArtIsAProductLocalVisualException(int product, string state, string expected)
        {
            Type foodState = Type.GetType("HayChoriYPaty.StreetFoodState, Assembly-CSharp", true);
            Assert.AreEqual(expected, RendererType.GetMethod("VisualStateNameForProduct", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { product, Enum.Parse(foodState, state) }));
        }

        [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(10)]
        public void SixWholeBondiolasFillTheirGrillAreaWithoutTouchingVacio(int level)
        {
            Type st = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            object sim = Activator.CreateInstance(st, new object[] { Activator.CreateInstance(Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true)), level, 5f, 0, 1, 0 });
            object layout = st.GetProperty("KitchenLayout").GetValue(sim), kitchen = st.GetProperty("Kitchen").GetValue(sim);
            Assert.AreEqual(6, kitchen.GetType().GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 2 }));
            bool companion = (bool)st.GetMethod("IsProductAvailable").Invoke(sim, new object[] { 3 });
            Type lt = layout.GetType();
            Vector2 size = (Vector2)lt.GetMethod("GrillMeatSize").Invoke(layout, new object[] { 2, companion });
            Assert.AreEqual(2.1f, size.x / size.y, .001f);
            Assert.Greater(size.y, 13.7f, "Bondiolas are maximized while fitting a close two-row grid.");
            MethodInfo slot = lt.GetMethod("GrillSlotPosition", new[] { typeof(int), typeof(int), typeof(bool), typeof(int) });
            Rect grill = (Rect)lt.GetMethod("GrillBoundsForProduct").Invoke(layout, new object[] { 2 });
            var points = new Vector2[6]; var footprints = new Rect[6];
            int columns = companion ? 3 : 6, rows = companion ? 2 : 1;
            for (int i = 0; i < 6; i++)
            {
                points[i] = (Vector2)slot.Invoke(layout, new object[] { 2, i, companion, 6 });
                footprints[i] = new Rect(points[i] - size * .5f, size);
                Assert.GreaterOrEqual(footprints[i].yMin, grill.y + grill.height * .115f - .001f);
                Assert.LessOrEqual(footprints[i].yMax, grill.y + grill.height * .46f + .001f);
                Assert.GreaterOrEqual(footprints[i].xMin, grill.x + grill.width * .035f - .001f);
                Assert.LessOrEqual(footprints[i].xMax, grill.x + grill.width * .965f + .001f);
                for (int j = 0; j < i; j++) Assert.IsFalse(footprints[i].Overlaps(footprints[j]), $"Slots {i}/{j}: {points[i]} versus {points[j]}, size {size}");
                if (companion)
                {
                    Assert.LessOrEqual(footprints[i].xMax, grill.x + grill.width * .5f + .001f, "Bondiola stays in its half.");
                    Vector2 vacio = (Vector2)lt.GetMethod("GrillMeatSize").Invoke(layout, new object[] { 3, true });
                    for (int v = 0; v < 4; v++)
                    {
                        Vector2 p = (Vector2)slot.Invoke(layout, new object[] { 3, v, true, 4 });
                        Assert.IsFalse(footprints[i].Overlaps(new Rect(p - vacio * .5f, vacio)), "Do not invade Vacío.");
                    }
                }
            }
            for (int row = 0; row < rows; row++)
            {
                int first = row * columns, last = first + columns - 1;
                Assert.Less(points[first].x, points[last].x, "Each row fills the usable width.");
                for (int col = 1; col < columns; col++)
                {
                    float gap = points[first + col].x - points[first + col - 1].x - size.x;
                    Assert.That(gap, Is.InRange(-.001f, 8f), "Neighboring Bondiolas nearly touch without overlap.");
                }
                if (row < rows - 1)
                {
                    Assert.Less(points[first].y, points[first + columns].y, "Rows follow rear-to-front order.");
                    float gap = points[first + columns].y - points[first].y - size.y;
                    Assert.That(gap, Is.InRange(-.001f, 2f), "The two rows leave little depth empty.");
                }
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void WholeMeatCookingSlotsPreservePreparedStockAndFillBothPremiumHalves(bool companion)
        {
            Type pt = Type.GetType("HayChoriYPaty.StreetKitchenProduction, Assembly-CSharp", true);
            object kitchen = Activator.CreateInstance(pt, new object[] {
                Activator.CreateInstance(Type.GetType("HayChoriYPaty.StreetKitchenBalance, Assembly-CSharp", true)), companion ? new[] { 2, 3 } : new[] { 2 } });
            Assert.AreEqual(6, pt.GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 2 }));
            Assert.AreEqual(companion ? 4 : 0, pt.GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 3 }));
            Assert.AreEqual(48, pt.GetMethod("TableCapacity").Invoke(kitchen, new object[] { 2 }));
            Assert.AreEqual(companion ? 18 : 36, pt.GetMethod("TableCount").Invoke(kitchen, new object[] { 2 }));
            var slots = new System.Collections.Generic.HashSet<int>();
            foreach (object unit in (System.Collections.IEnumerable)pt.GetProperty("Units").GetValue(kitchen))
            {
                Type ut = unit.GetType();
                if ((int)ut.GetProperty("Product").GetValue(unit) != 2 || ut.GetProperty("Location").GetValue(unit).ToString() != "Grill") continue;
                int slot = (int)ut.GetProperty("Slot").GetValue(unit);
                Assert.That(slot, Is.InRange(0, 5)); Assert.IsTrue(slots.Add(slot));
            }
            Assert.AreEqual(6, slots.Count, "Exactly six real meats; none hidden, duplicated or invented.");
        }

        [TestCase("Raw")] [TestCase("Cooking")] [TestCase("Cooked")]
        public void WholeVacioStatesImportAsTransparentSingleSprites(string state)
        {
            Sprite sprite = Resources.Load<Sprite>(VacioResource + state);
            Assert.NotNull(sprite);
            Assert.AreEqual(new Rect(0, 0, sprite.texture.width, sprite.texture.height), sprite.rect);
            Assert.AreEqual(sprite.rect.size * .5f, sprite.pivot);
            Assert.That(sprite.rect.width / sprite.rect.height, Is.InRange(2.5f, 2.6f));
            string path = "Assets/Resources/" + VacioResource + state + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.IsTrue(importer.alphaIsTransparency); Assert.IsTrue(importer.mipmapEnabled);
            var decoded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(path)));
                Color32[] pixels = decoded.GetPixels32(); int width = decoded.width, height = decoded.height, opaque = 0;
                for (int i = 0; i < pixels.Length; i++) if (pixels[i].a > 240) opaque++;
                Assert.Greater(opaque, pixels.Length / 3);
                for (int x = 0; x < width; x++)
                {
                    Assert.LessOrEqual(pixels[x].a, 10); Assert.LessOrEqual(pixels[(height - 1) * width + x].a, 10);
                }
                for (int y = 0; y < height; y++)
                {
                    Assert.LessOrEqual(pixels[y * width].a, 10); Assert.LessOrEqual(pixels[y * width + width - 1].a, 10);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }

        [TestCase("Raw", "Raw")] [TestCase("Cooking", "Cooking")]
        [TestCase("Cooked", "Cooked")] [TestCase("Passed", "Cooked")]
        public void VacioSelectorUsesWholeCutArtAndKeepsPassedWarm(string requested, string expected)
        {
            var root = new GameObject("Whole Vacio selector regression");
            try
            {
                Component renderer = root.AddComponent(RendererType);
                MethodInfo method = RendererType.GetMethod("StateSprite", P);
                var sprite = (Sprite)method.Invoke(renderer, new object[] { "meat-vacio", requested });
                Assert.AreSame(Resources.Load<Sprite>(VacioResource + expected), sprite);
                Assert.AreSame(sprite, method.Invoke(renderer, new object[] { "meat-vacio", requested }), "Use cached imported sprites, not recreated runtime copies.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(5)] [TestCase(10)]
        public void FourWholeVaciosFillTheRightHalfInTwoColumnsAndTwoRows(int level)
        {
            Type st = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            object sim = Activator.CreateInstance(st, new object[] { Activator.CreateInstance(Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true)), level, 5f, 0, 1, 0 });
            object layout = st.GetProperty("KitchenLayout").GetValue(sim), kitchen = st.GetProperty("Kitchen").GetValue(sim);
            Assert.AreEqual(4, kitchen.GetType().GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 3 }));
            bool companion = (bool)st.GetMethod("IsProductAvailable").Invoke(sim, new object[] { 2 });
            Assert.IsTrue(companion);
            Type lt = layout.GetType();
            Vector2 size = (Vector2)lt.GetMethod("GrillMeatSize").Invoke(layout, new object[] { 3, true });
            Vector2 bondiolaSize = (Vector2)lt.GetMethod("GrillMeatSize").Invoke(layout, new object[] { 2, true });
            Assert.AreEqual(size.y * 3.4f, size.x, .001f, "The broad Vacío cut uses most of its half-width.");
            Assert.AreEqual(bondiolaSize.y * 1.10f, size.y, .001f, "Vacío is enlarged 10% in depth.");
            MethodInfo slot = lt.GetMethod("GrillSlotPosition", new[] { typeof(int), typeof(int), typeof(bool), typeof(int) });
            Rect grill = (Rect)lt.GetMethod("GrillBoundsForProduct").Invoke(layout, new object[] { 3 });
            var points = new Vector2[4]; var footprints = new Rect[4];
            for (int i = 0; i < 4; i++)
            {
                points[i] = (Vector2)slot.Invoke(layout, new object[] { 3, i, true, 4 });
                footprints[i] = new Rect(points[i] - size * .5f, size);
                Assert.GreaterOrEqual(footprints[i].yMin, grill.y + grill.height * .115f - .001f);
                Assert.LessOrEqual(footprints[i].yMax, grill.y + grill.height * .46f + .001f);
                Assert.GreaterOrEqual(footprints[i].xMin, grill.x + grill.width * .5f - .001f, "Vacío stays on its half.");
                for (int j = 0; j < i; j++)
                {
                    float xGap = Mathf.Abs(points[i].x - points[j].x) - size.x;
                    float yGap = Mathf.Abs(points[i].y - points[j].y) - size.y;
                    if (i / 2 == j / 2 && Mathf.Abs(i - j) == 1)
                        Assert.That(xGap, Is.InRange(.99f, 1.01f), $"Adjacent Vacíos stay closely spaced: slots {i}/{j}, xGap={xGap}.");
                    if (i / 2 != j / 2 && i % 2 == j % 2)
                        Assert.That(yGap, Is.InRange(-1.7f, 2f), $"Matching columns across depth rows stay nearly touching: slots {i}/{j}, yGap={yGap}.");
                }
            }
            for (int row = 0; row < 2; row++)
            {
                int first = row * 2;
                Assert.Less(points[first].x, points[first + 1].x, "Each row uses the full Vacío half-width.");
                for (int col = 1; col < 2; col++)
                {
                    float horizontalGap = points[first + col].x - points[first + col - 1].x - size.x;
                    Assert.That(horizontalGap, Is.InRange(.99f, 1.01f), $"Vacíos keep the requested close 1-unit gap: gap={horizontalGap}.");
                }
                if (row < 1)
                {
                    float verticalGap = points[first + 2].y - points[first].y - size.y;
                    Assert.That(verticalGap, Is.InRange(-1.7f, 2f), "The two rows nearly fill grill depth.");
                }
            }
        }

        [Test]
        public void SoloVacioRetainsSevenCookingSlots()
        {
            Type pt = Type.GetType("HayChoriYPaty.StreetKitchenProduction, Assembly-CSharp", true);
            object kitchen = Activator.CreateInstance(pt, new object[] {
                Activator.CreateInstance(Type.GetType("HayChoriYPaty.StreetKitchenBalance, Assembly-CSharp", true)), new[] { 3 } });
            Assert.AreEqual(7, pt.GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 3 }));
            Assert.AreEqual(0, pt.GetMethod("GrillCapacity").Invoke(kitchen, new object[] { 2 }));
        }

        [TestCase("meat-chori")] [TestCase("meat-paty")]
        public void OtherMeatSelectorsKeepTheirOriginalSprites(string resource)
        {
            var root = new GameObject("Other meat selector regression");
            try
            {
                Component renderer = root.AddComponent(RendererType);
                foreach (string state in new[] { "Raw", "Cooking", "Cooked" })
                {
                    Sprite sprite = (Sprite)RendererType.GetMethod("StateSprite", P).Invoke(renderer, new object[] { resource, state });
                    Assert.AreSame(Resources.Load<Sprite>("ModularKitchen/Sprites/" + resource + "/" + resource + "_" + state), sprite);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
