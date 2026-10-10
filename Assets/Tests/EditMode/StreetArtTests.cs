using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetArtTests
    {
        [Test]
        public void RiverVisualThemeOwnsIndexSevenInTheOfficialCatalog()
        {
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            object theme = themes.GetMethod("ForLevel").Invoke(null, new object[] { 7 });
            Assert.AreEqual("River Plate / Núñez", themes.GetProperty("ClubName").GetValue(theme));
            Assert.AreEqual("street-mural-river-master-v1", themes.GetProperty("MuralResource").GetValue(theme));
            Assert.NotNull(Resources.Load<Texture2D>("street-mural-river-master-v1"));
            string[] names = (string[])Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true)
                .GetField("LevelNames").GetValue(null);
            Assert.AreEqual(11, names.Length);
            Assert.AreEqual("River Plate / Núñez", names[7]);
            Assert.AreEqual("Boca Juniors / La Boca", themes.GetProperty("ClubName").GetValue(
                themes.GetMethod("ForLevel").Invoke(null, new object[] { 8 })));
        }

        [TestCase(false)] [TestCase(true)]
        public void RiverAtlasesKeepTwelveOutfitsAcrossAllPosePairs(bool riot)
        {
            string resource = riot ? "street-river-riot-fans-v1" : "street-river-fans-paired-v1";
            Texture2D atlas = Resources.Load<Texture2D>(resource);
            Assert.NotNull(atlas);
            Assert.AreEqual(1536, atlas.width); Assert.AreEqual(1024, atlas.height);
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + resource + ".png");
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsFalse(importer.mipmapEnabled); Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            var decoded = new Texture2D(2, 2);
            try
            {
                decoded.LoadImage(System.IO.File.ReadAllBytes(Application.dataPath + "/Art/Street/Resources/" + resource + ".png"));
                Color32[] pixels = decoded.GetPixels32();
                Assert.Greater(pixels.Count(p => p.a < 10) / (float)pixels.Length, .40f,
                    "Transparent gutters must not contain the image generator's translucent background haze");
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
            MethodInfo map = View.GetMethod("RiverAtlasVariant", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo source = View.GetMethod("RacingFanSourceRect", BindingFlags.Static | BindingFlags.NonPublic);
            var seen = new System.Collections.Generic.HashSet<Rect>();
            for (int variant = 0; variant < 12; variant++)
            {
                int mapped = (int)map.Invoke(null, new object[] { variant });
                for (int pose = 0; pose < 2; pose++)
                {
                    Rect cell = (Rect)source.Invoke(null, new object[] { atlas, mapped, pose == 1 });
                    Assert.AreEqual((variant % 3 * 2 + pose) * 256f, cell.x);
                    Assert.AreEqual(variant / 3 * 256f, cell.y, "Retail provenance rows read from top to bottom");
                    Assert.AreEqual(new Vector2(256, 256), cell.size);
                    Assert.IsTrue(seen.Add(cell), "Each paired pose has its own complete-body cell");
                }
                Assert.AreEqual(mapped, map.Invoke(null, new object[] { variant + 12 }));
            }
            Assert.AreEqual(map.Invoke(null, new object[] { 11 }), map.Invoke(null, new object[] { -1 }));
        }

        private const string SheetPath = "Assets/Art/Street/Resources/street-parrillero.png";
        private const string DiagonalSheetPath = "Assets/Art/Street/Resources/street-parrillero-diagonal-v1.png";
        private static Type View { get { return Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true); } }

        [TestCase(960f)]
        [TestCase(1200f)]
        public void MasterSceneCounterAndCrowdMatchRootPlateForEveryClub(float canvasHeight)
        {
            Type layout = Type.GetType("HayChoriYPaty.StreetSceneLayout, Assembly-CSharp", true);
            Rect plate = (Rect)layout.GetMethod("BackgroundBounds").Invoke(null, new object[] { canvasHeight });
            Rect counter = (Rect)layout.GetMethod("CounterBounds").Invoke(null, new object[] { canvasHeight });
            Rect source = (Rect)layout.GetField("CounterSource").GetValue(null);
            Vector2 size = (Vector2)layout.GetField("ReferenceBackdropSize").GetValue(null);
            Assert.AreEqual(plate.y + source.y / size.y * plate.height, counter.y, .001f);
            Assert.AreEqual(plate.y + source.yMax / size.y * plate.height, counter.yMax, .001f);
            Assert.AreEqual(plate.width, counter.width, .001f);
            MethodInfo edge = View.GetMethod("CounterSurfaceY", BindingFlags.Static | BindingFlags.NonPublic);
            for (int level = 0; level < 11; level++)
            foreach (Vector2 replacementSize in new[] { new Vector2(940, 1673), new Vector2(1536, 2730), new Vector2(887, 1772) })
                Assert.AreEqual(counter.y, (float)edge.Invoke(null, new object[] { canvasHeight, replacementSize, level }), .001f,
                    "Club art dimensions must never move the counter or crowd");
            Assert.Less(counter.yMax, 400f * canvasHeight / 960f, "Workers' feet stay on the operator side");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void ClubMuralAndAtmosphereConfigureArtWithoutDefiningPerspective(int level)
        {
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            object theme = themes.GetMethod("ForLevel").Invoke(null, new object[] { level });
            string resource = (string)themes.GetProperty("MuralResource").GetValue(theme);
            Assert.IsNotEmpty(resource);
            Texture2D art = Resources.Load<Texture2D>(resource);
            Assert.IsNotNull(art, resource);
            Rect crop = (Rect)themes.GetProperty("MuralSource").GetValue(theme);
            Assert.Greater(crop.width, 0f); Assert.Greater(crop.height, 0f);
            Assert.GreaterOrEqual(crop.xMin, 0f); Assert.GreaterOrEqual(crop.yMin, 0f);
            Assert.LessOrEqual(crop.xMax, 1f); Assert.LessOrEqual(crop.yMax, 1f);
            Color tint = (Color)themes.GetProperty("SceneryTint").GetValue(theme);
            Assert.GreaterOrEqual(Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b)), .88f,
                "Atmosphere cannot make the playing scenery dark");
            Assert.AreEqual(1f, tint.a);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StandardButtonLabelStatesDoNotInheritSkinHoverOrMutateSource(bool outline)
        {
            var source = new GUIStyle { font = Resources.Load<Font>("Menu/LuckiestGuy-Regular"), fontSize = 38 };
            source.normal.textColor = Color.red;
            source.hover.textColor = Color.yellow;
            Color fill = outline ? new Color(.035f, .075f, .16f) : Color.white;
            var method = View.GetMethod("CreateStandardButtonLabelStyle", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (GUIStyle)method.Invoke(null, new object[] { source, 27, fill });
            Assert.AreSame(source.font, result.font);
            Assert.AreEqual(27, result.fontSize);
            Assert.AreEqual(fill, result.normal.textColor);
            Assert.AreEqual(fill, result.hover.textColor);
            Assert.AreEqual(fill, result.active.textColor);
            Assert.AreEqual(fill, result.focused.textColor);
            Assert.AreEqual(38, source.fontSize);
            Assert.AreEqual(Color.red, source.normal.textColor);
            Assert.AreEqual(Color.yellow, source.hover.textColor);
        }

        [Test]
        public void StandardButtonReusesExactCoverTextureFactoryAndBundledFont()
        {
            Texture2D texture = (Texture2D)View.GetMethod("CreateMenuButton", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            try
            {
                Assert.AreEqual(512, texture.width); Assert.AreEqual(164, texture.height);
                Assert.AreEqual("Original glossy blue startup button", texture.name);
                Assert.AreEqual(HideFlags.HideAndDontSave, texture.hideFlags);
                Assert.AreEqual("", AssetDatabase.GetAssetPath(texture), "No duplicated button asset is created.");
                Assert.NotNull(Resources.Load<Font>("Menu/LuckiestGuy-Regular"));
                Assert.NotNull(View.GetMethod("DrawStandardButton", BindingFlags.NonPublic | BindingFlags.Instance));
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void StandardButtonStatesRetainCoverPaletteAndCoherentDisabledTint()
        {
            MethodInfo method = View.GetMethod("StandardButtonTint", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual(Color.white, (Color)method.Invoke(null, new object[] { true, false }));
            Assert.AreEqual(new Color(.78f, .88f, 1f), (Color)method.Invoke(null, new object[] { true, true }));
            Color disabled = (Color)method.Invoke(null, new object[] { false, false });
            Assert.AreEqual(disabled, (Color)method.Invoke(null, new object[] { false, true }));
            Assert.Less(disabled.r, disabled.b, "The disabled button stays in the cover's blue family, not generic Unity gray.");
        }

        [TestCase(288f, 92f)]
        [TestCase(364f, 66f)]
        [TestCase(57f, 35f)]
        [TestCase(320f, 98f)]
        public void StandardButtonSlicesKeepRoundEndCapsAndCoverEverySize(float width, float height)
        {
            Rect bounds = new Rect(120f, 800f, width, height);
            MethodInfo method = View.GetMethod("StandardButtonSliceBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect left = (Rect)method.Invoke(null, new object[] { bounds, 0 });
            Rect middle = (Rect)method.Invoke(null, new object[] { bounds, 1 });
            Rect right = (Rect)method.Invoke(null, new object[] { bounds, 2 });
            Assert.AreEqual(height * .5f, left.width); Assert.AreEqual(left.width, right.width);
            Assert.AreEqual(left.xMax, middle.xMin); Assert.AreEqual(middle.xMax, right.xMin);
            Assert.AreEqual(bounds.xMin, left.xMin); Assert.AreEqual(bounds.xMax, right.xMax);
            Assert.AreEqual(bounds.width, left.width + middle.width + right.width, .01f);
        }

        [TestCase("JUGAR", 288f, 92f)]
        [TestCase("VOLVER", 364f, 66f)]
        [TestCase("1", 57f, 35f)]
        [TestCase("CONTINUAR", 156f, 60f)]
        public void StandardButtonFontFitsWithoutFallbackOrLeakingStyle(string caption, float width, float height)
        {
            Font font = Resources.Load<Font>("Menu/LuckiestGuy-Regular");
            var style = new GUIStyle { font = font, fontSize = 38, wordWrap = false };
            Rect bounds = new Rect(0, 0, width, height);
            int size = (int)View.GetMethod("FitStandardButtonFontSize", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { style, caption, bounds });
            Assert.AreSame(font, style.font); Assert.AreEqual(38, style.fontSize);
            Assert.GreaterOrEqual(size, 11);
            if (caption == "JUGAR") Assert.AreEqual(38, size, "Keep the original cover lettering unchanged.");
            style.fontSize = size;
            Assert.LessOrEqual(style.CalcSize(new GUIContent(caption)).x, width - 24f * height / 92f + 1f);
        }

        [TestCase(1f)]
        [TestCase(1.25f)]
        [TestCase(1.42f)]
        public void StandardButtonPressedKeepsCenterInsideUnchangedHitBounds(float verticalScale)
        {
            Rect hit = new Rect(88f, 858f * verticalScale, 364f, 66f);
            MethodInfo method = View.GetMethod("StandardButtonVisualBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect normal = (Rect)method.Invoke(null, new object[] { hit, false });
            Rect pressed = (Rect)method.Invoke(null, new object[] { hit, true });
            Assert.AreEqual(hit, normal); Assert.AreEqual(hit.center, pressed.center);
            Assert.AreEqual(hit.width * .97f, pressed.width, .01f); Assert.AreEqual(hit.height * .97f, pressed.height, .01f);
            Assert.GreaterOrEqual(pressed.xMin, hit.xMin); Assert.LessOrEqual(pressed.xMax, hit.xMax);
            Assert.GreaterOrEqual(pressed.yMin, hit.yMin); Assert.LessOrEqual(pressed.yMax, hit.yMax);
        }

        [Test]
        public void SingleRemainingProductIsCentered()
        {
            Rect bubble = Bubble(new Vector2(58f, 324f), 1);
            Rect row = OrderRect("OrderLineBounds", bubble, 0, 1, 1f);
            Assert.AreEqual(62f, bubble.width); Assert.AreEqual(49f, bubble.height);
            Assert.AreEqual(bubble.y + (bubble.height - 8f) * .5f, row.center.y, .01f);
            Assert.AreEqual(58f, bubble.center.x);
            Assert.AreEqual(256f, bubble.yMax);
            Assert.GreaterOrEqual(row.xMin, bubble.xMin); Assert.LessOrEqual(row.xMax, bubble.xMax);
        }

        [Test]
        public void OrderBubbleTwoRowsAndMoreFooterFitInsideAdaptiveCard()
        {
            Rect bubble = Bubble(new Vector2(270f, 324f), 4);
            Rect first = OrderRect("OrderLineBounds", bubble, 0, 2, 1f);
            Rect second = OrderRect("OrderLineBounds", bubble, 1, 2, 1f);
            Rect more = OrderRect("OrderMoreBounds", bubble, 1f);
            Assert.AreEqual(62f, bubble.width); Assert.AreEqual(88f, bubble.height);
            Assert.LessOrEqual(first.yMax, second.yMin); Assert.LessOrEqual(second.yMax, more.yMin);
            Assert.GreaterOrEqual(first.xMin, bubble.xMin); Assert.LessOrEqual(first.xMax, bubble.xMax);
            Assert.GreaterOrEqual(second.xMin, bubble.xMin); Assert.LessOrEqual(second.xMax, bubble.xMax);
            Assert.LessOrEqual(more.yMax, bubble.yMax - 8f);
            Assert.LessOrEqual(more.xMax, bubble.xMax);
        }

        [TestCase(1, 49f)]
        [TestCase(2, 74f)]
        [TestCase(3, 88f)]
        [TestCase(4, 88f)]
        [TestCase(5, 88f)]
        public void OrderBubbleAdaptsWithoutScalingIconsOrQuantities(int pendingLines, float height)
        {
            Rect bubble = Bubble(new Vector2(270f, 324f), pendingLines);
            Assert.AreEqual(height, bubble.height);
            Assert.AreEqual(270f, bubble.center.x); Assert.AreEqual(256f, bubble.yMax);
            int rows = Mathf.Min(2, pendingLines);
            for (int i = 0; i < rows; i++)
            {
                Rect row = OrderRect("OrderLineBounds", bubble, i, rows, 1f);
                Rect icon = OrderRect("OrderIconBounds", row, 1f);
                Rect quantity = OrderRect("OrderQuantityBounds", row, 1f);
                Assert.AreEqual(new Vector2(26f, 23f), icon.size);
                Assert.AreEqual(new Vector2(20f, 25f), quantity.size);
                Assert.GreaterOrEqual(icon.xMin, bubble.xMin); Assert.LessOrEqual(quantity.xMax, bubble.xMax);
                Assert.GreaterOrEqual(icon.yMin, bubble.yMin); Assert.LessOrEqual(quantity.yMax, bubble.yMax - 8f);
            }
        }

        [Test]
        public void OrderBubbleWidthFollowsContentWithoutMovingAnchor()
        {
            Rect compact = Bubble(new Vector2(270f, 324f), 1);
            Rect wide = (Rect)View.GetMethod("OrderBubbleBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Vector2(270f, 324f), 1, 40f });
            Assert.Greater(wide.width, compact.width);
            Assert.AreEqual(compact.center.x, wide.center.x); Assert.AreEqual(compact.yMax, wide.yMax);
            Assert.LessOrEqual(wide.width, 70f, "Keep the existing seven-column envelope for legacy large quantities.");
        }

        [TestCase(.75f)]
        [TestCase(1f)]
        [TestCase(1.25f)]
        [TestCase(1.42f)]
        public void OrderBubblesFitQueuePitchAndRetainCustomerTail(float verticalScale)
        {
            for (int pendingLines = 1; pendingLines <= 4; pendingLines++)
            {
                Rect logical = Bubble(new Vector2(270f, 324f), pendingLines);
                Rect drawn = OrderRect("OrderBubbleLayoutBounds", logical, verticalScale);
                float contentScale = OrderContentScale(logical.height, verticalScale);
                Assert.AreEqual(logical.yMax * verticalScale, drawn.yMax, .01f);
                Assert.AreEqual(logical.center.x, drawn.center.x);
                Assert.AreEqual(logical.height * contentScale, drawn.height, .01f);
                Assert.LessOrEqual(drawn.height, 56f * verticalScale + .01f);
                int rows = Mathf.Min(2, pendingLines);
                Rect previous = default;
                for (int i = 0; i < rows; i++)
                {
                    Rect row = OrderRect("OrderLineBounds", drawn, i, rows, contentScale);
                    Rect icon = OrderRect("OrderIconBounds", row, contentScale);
                    Rect quantity = OrderRect("OrderQuantityBounds", row, contentScale);
                    Assert.GreaterOrEqual(row.yMin, drawn.yMin); Assert.LessOrEqual(row.yMax, drawn.yMax);
                    Assert.GreaterOrEqual(icon.yMin, drawn.yMin); Assert.LessOrEqual(quantity.yMax, drawn.yMax);
                    if (i > 0) Assert.LessOrEqual(previous.yMax, row.yMin);
                    previous = row;
                }
                if (pendingLines > rows)
                {
                    Rect more = OrderRect("OrderMoreBounds", drawn, contentScale);
                    Assert.GreaterOrEqual(more.yMin, drawn.yMin); Assert.LessOrEqual(more.yMax, drawn.yMax);
                    Assert.LessOrEqual(previous.yMax, more.yMin);
                }
            }
        }

        private static float OrderContentScale(float bubbleHeight, float verticalScale) =>
            (float)View.GetMethod("OrderBubbleContentScale", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bubbleHeight, verticalScale });

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void OrderBubbleNineSlicesKeepCornersFixedAndCoverFrame(int pendingLines)
        {
            Rect bubble = Bubble(new Vector2(270f, 324f), pendingLines);
            float area = 0f;
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            {
                Rect slice = OrderRect("OrderBubbleSlice", bubble, column, row, 20f, 15f);
                Assert.Greater(slice.width, 0f); Assert.Greater(slice.height, 0f);
                Assert.GreaterOrEqual(slice.xMin, bubble.xMin); Assert.LessOrEqual(slice.xMax, bubble.xMax);
                Assert.GreaterOrEqual(slice.yMin, bubble.yMin); Assert.LessOrEqual(slice.yMax, bubble.yMax);
                if (column != 1) Assert.AreEqual(20f, slice.width);
                if (row != 1) Assert.AreEqual(15f, slice.height);
                area += slice.width * slice.height;
            }
            Assert.AreEqual(bubble.width * bubble.height, area, .01f);
        }

        private static Rect Bubble(Vector2 position, int pendingLines) =>
            (Rect)View.GetMethod("OrderBubbleBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { position, pendingLines, 20f });

        private static Rect OrderRect(string method, params object[] args) =>
            (Rect)View.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);

        [Test]
        public void ParrilleroHasSixteenNamedSpritesAndReviewedBounds()
        {
            Texture2D texture = Resources.Load<Texture2D>("street-parrillero");
            Assert.NotNull(texture);
            Assert.AreEqual(1315, texture.width);
            Assert.AreEqual(1197, texture.height);
            Sprite[] sprites = Resources.LoadAll<Sprite>("street-parrillero");
            Assert.AreEqual(16, sprites.Length);
            Assert.AreEqual(16, sprites.Select(s => s.name).Distinct().Count());
            Assert.IsTrue(sprites.All(s => s.name.StartsWith("parrillero-")));
            Rect[] bounds = (Rect[])View.GetField("ParrilleroPoses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(16, bounds.Length);
            foreach (Rect b in bounds)
            {
                Assert.Greater(b.width, 0); Assert.Greater(b.height, 0);
                Assert.GreaterOrEqual(b.xMin, 0); Assert.GreaterOrEqual(b.yMin, 0);
                Assert.LessOrEqual(b.xMax, texture.width); Assert.LessOrEqual(b.yMax, texture.height);
                Rect imported = new Rect(b.x, texture.height - b.yMax, b.width, b.height);
                Assert.IsTrue(sprites.Any(s => s.rect == imported), "Renderer rect must match an imported sprite");
            }
        }

        [Test]
        public void ParrilleroTexturesPreserveAlphaAndOriginalResolution()
        {
            foreach (string path in new[] { SheetPath, "Assets/Art/Street/Resources/street-parrillero-icon.png", "Assets/Art/Street/Resources/street-cover-parrillero-full-v1.png" })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.NotNull(importer);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsTrue(importer.alphaIsTransparency);
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden);
                Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
                Assert.GreaterOrEqual(android.maxTextureSize, 1315);
            }
            Texture2D icon = Resources.Load<Texture2D>("street-parrillero-icon");
            Assert.NotNull(icon); Assert.AreEqual(1315, icon.width); Assert.AreEqual(1196, icon.height);
            Texture2D coverHero = Resources.Load<Texture2D>("street-cover-parrillero-full-v1");
            Assert.NotNull(coverHero); Assert.AreEqual(1024, coverHero.width); Assert.AreEqual(1536, coverHero.height);
        }

        [Test]
        public void ParrilleroDiagonalSheetLoadsEightReviewedFramesAtOriginalResolution()
        {
            Texture2D texture = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
            Assert.NotNull(texture);
            Assert.AreEqual(1536, texture.width);
            Assert.AreEqual(1024, texture.height);
            Sprite[] importedSprite = Resources.LoadAll<Sprite>("street-parrillero-diagonal-v1");
            Assert.AreEqual(1, importedSprite.Length);
            Assert.AreEqual(new Rect(0, 0, 1536, 1024), importedSprite[0].rect);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(DiagonalSheetPath);
            Assert.NotNull(importer);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden);
            Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            Assert.GreaterOrEqual(android.maxTextureSize, 1536);

            Rect[] bounds = (Rect[])View.GetField("ParrilleroDiagonalPoses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(8, bounds.Length);
            foreach (Rect b in bounds)
            {
                Assert.Greater(b.width, 0); Assert.Greater(b.height, 0);
                Assert.GreaterOrEqual(b.xMin, 0); Assert.GreaterOrEqual(b.yMin, 0);
                Assert.LessOrEqual(b.xMax, texture.width); Assert.LessOrEqual(b.yMax, texture.height);
            }
        }

        [Test]
        public void ParrilleroWalkSelectionUsesDistanceDrivenGaitPhases()
        {
            MethodInfo select = View.GetMethod("SelectParrilleroWalkFrame", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2[] directions = {
                new Vector2(-100, 100), new Vector2(100, 100),
                new Vector2(-100, -100), new Vector2(100, -100)
            };
            for (int i = 0; i < directions.Length; i++)
            {
                Assert.AreEqual(16 + i * 2, (int)select.Invoke(null, new object[] { directions[i], 0 }));
                Assert.AreEqual(17 + i * 2, (int)select.Invoke(null, new object[] { directions[i], 1 }));
            }

            Assert.AreEqual(1, (int)select.Invoke(null, new object[] { Vector2.up, 0 }));
            Assert.AreEqual(2, (int)select.Invoke(null, new object[] { Vector2.up, 1 }));
            Assert.AreEqual(5, (int)select.Invoke(null, new object[] { Vector2.down, 0 }));
            Assert.AreEqual(6, (int)select.Invoke(null, new object[] { Vector2.down, 1 }));
            Assert.AreEqual(9, (int)select.Invoke(null, new object[] { Vector2.right, 0 }));
            Assert.AreEqual(10, (int)select.Invoke(null, new object[] { Vector2.right, 1 }));
            Assert.AreEqual(1, (int)select.Invoke(null, new object[] { new Vector2(20, 100), 0 }), "Near-vertical travel stays on the front walk pair");
        }

        [Test]
        public void FirstLevelFansWearNineIntegratedAllBoysOutfitsInFrontAndWalkingPoses()
        {
            const string frontResource = "street-allboys-fans-front-v1";
            const string walkResource = "street-allboys-fans-walk-v1";
            const string frontPath = "Assets/Art/Street/Resources/street-allboys-fans-front-v1.png";
            const string walkPath = "Assets/Art/Street/Resources/street-allboys-fans-walk-v1.png";
            Texture2D front = Resources.Load<Texture2D>(frontResource);
            Texture2D walk = Resources.Load<Texture2D>(walkResource);
            Assert.NotNull(front, "Nine complete front-facing All Boys outfit sprites must be bundled");
            Assert.NotNull(walk, "Matching profile-walking All Boys outfits must be bundled");
            foreach (Texture2D atlas in new[] { front, walk })
            {
                Assert.AreEqual(1254, atlas.width);
                Assert.AreEqual(1254, atlas.height);
            }
            foreach (string path in new[] { frontPath, walkPath })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.NotNull(importer);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsTrue(importer.alphaIsTransparency);
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden);
                Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            }

            int count = (int)View.GetField("AllBoysFanVariantCount", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.AreEqual(9, count, "Only nine neutral/monochrome wardrobe styles are used for Floresta");
            MethodInfo variant = View.GetMethod("GetAllBoysFanVariant", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo select = View.GetMethod("GetAllBoysFanFrame", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo sourceRect = View.GetMethod("GetAllBoysFanSourceRect", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(variant); Assert.NotNull(select); Assert.NotNull(sourceRect);
            CollectionAssert.AreEqual(Enumerable.Range(0, 9).ToArray(), Enumerable.Range(0, 9)
                .Select(id => (int)variant.Invoke(null, new object[] { id })).ToArray());
            CollectionAssert.AreEqual(Enumerable.Range(0, 9).ToArray(), Enumerable.Range(0, 9)
                .Select(id => (int)select.Invoke(null, new object[] { id, false })).ToArray());
            CollectionAssert.AreEqual(Enumerable.Range(9, 9).ToArray(), Enumerable.Range(0, 9)
                .Select(id => (int)select.Invoke(null, new object[] { id, true })).ToArray());
            Assert.AreEqual(8, (int)variant.Invoke(null, new object[] { -1 }), "Negative customer IDs must wrap consistently");
            Assert.AreEqual(new Rect(0, 836, 418, 418), (Rect)sourceRect.Invoke(null, new object[] { front, 0 }));
            Assert.AreEqual(new Rect(836, 0, 418, 418), (Rect)sourceRect.Invoke(null, new object[] { walk, 8 }));

            Assert.IsNull(View.GetField("fanWardrobe", BindingFlags.Instance | BindingFlags.NonPublic),
                "Level 1 outfits are complete full-body sprites, not shirt overlays");
            Assert.IsNull(View.GetMethod("DrawFanWardrobe", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.NotNull(View.GetMethod("DrawAllBoysFan", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.NotNull(View.GetMethod("DrawChicagoFan", BindingFlags.Instance | BindingFlags.NonPublic),
                "Nueva Chicago uses its own integrated full-body outfit sprites");
        }

        [Test]
        public void NewChicagoFansUseEightIntegratedGreenBlackWhiteOutfitsAcrossAllPoses()
        {
            const string frontResource = "street-chicago-fans-front-v1";
            const string walkResource = "street-chicago-fans-walk-v1";
            const string riotResource = "street-chicago-riot-fans-v1";
            const string resourceRoot = "Assets/Art/Street/Resources/";
            Texture2D front = Resources.Load<Texture2D>(frontResource);
            Texture2D walk = Resources.Load<Texture2D>(walkResource);
            Texture2D riot = Resources.Load<Texture2D>(riotResource);
            Assert.NotNull(front, "Eight complete front-facing Chicago outfits must be bundled");
            Assert.NotNull(walk, "Matching side-walking Chicago outfit sprites must be bundled");
            Assert.NotNull(riot, "The timeout animation must keep the Chicago apparel integrated too");
            foreach (Texture2D atlas in new[] { front, walk })
            {
                Assert.AreEqual(1254, atlas.width);
                Assert.AreEqual(1254, atlas.height);
            }
            Assert.AreEqual(1774, riot.width);
            Assert.AreEqual(887, riot.height);

            foreach (string filename in new[] { frontResource, walkResource, riotResource })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(resourceRoot + filename + ".png");
                Assert.NotNull(importer);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsTrue(importer.alphaIsTransparency);
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden);
                Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            }

            int variantCount = (int)View.GetField("ChicagoFanVariantCount", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            MethodInfo select = View.GetMethod("GetChicagoFanVariant", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo sourceRect = View.GetMethod("GetChicagoFanSourceRect", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual(8, variantCount);
            CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), Enumerable.Range(0, 8)
                .Select(id => (int)select.Invoke(null, new object[] { id })).ToArray());
            Assert.AreEqual(7, (int)select.Invoke(null, new object[] { -1 }), "Negative customer IDs wrap consistently");
            Assert.AreEqual(0, (int)select.Invoke(null, new object[] { 8 }), "Eight outfits cycle deterministically");
            Assert.AreEqual(new Rect(0, 836, 418, 418), (Rect)sourceRect.Invoke(null, new object[] { front, 0 }));
            Assert.AreEqual(new Rect(418, 0, 418, 418), (Rect)sourceRect.Invoke(null, new object[] { walk, 7 }));

            MethodInfo riotSource = View.GetMethod("RiotFanFrameSource", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual(new Rect(0, 0, riot.width / 4f, 493),
                (Rect)riotSource.Invoke(null, new object[] { riot, 0, 0 }));
            Assert.AreEqual(new Rect(0, 493, riot.width / 4f, 394),
                (Rect)riotSource.Invoke(null, new object[] { riot, 0, 1 }));

            Assert.IsNull(View.GetField("chicagoWardrobeSheet", BindingFlags.Instance | BindingFlags.NonPublic),
                "Chicago apparel is drawn as part of the full character sprite, never a separate garment overlay");
            Assert.IsNull(View.GetMethod("DrawChicagoWardrobe", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(View.GetMethod("GetChicagoWardrobeBounds", BindingFlags.Static | BindingFlags.NonPublic));
            Assert.NotNull(View.GetMethod("DrawChicagoFan", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [Test]
        public void StartupCoverAndTitleLogoAreReadyForAndroidSplash()
        {
            string coverName = (string)View.GetField("IntroBackdropResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Texture2D cover = Resources.Load<Texture2D>(coverName);
            Texture2D logo = Resources.Load<Texture2D>("street-logo");
            Assert.NotNull(cover);
            Assert.NotNull(logo);
            Assert.AreEqual(941, cover.width);
            Assert.AreEqual(1672, cover.height);
            Assert.AreEqual(1274, logo.width);
            Assert.AreEqual(1235, logo.height);

            foreach (var pair in new[] {
                new { Path = "Assets/Art/Street/Resources/" + coverName + ".png", Texture = cover, Alpha = false },
                new { Path = "Assets/Art/Street/Resources/street-logo.png", Texture = logo, Alpha = true }
            })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(pair.Path);
                Assert.NotNull(importer);
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.AreEqual(pair.Alpha, importer.DoesSourceTextureHaveAlpha());
                if (pair.Alpha) Assert.IsTrue(importer.alphaIsTransparency);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden);
                Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            }

            float duration = (float)View.GetField("IntroDuration", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.GreaterOrEqual(duration, 8f);
            Assert.LessOrEqual(duration, 9f);
            Assert.NotNull(Resources.Load<Font>("Menu/LuckiestGuy-Regular"));
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(1200, 2670)]
        [TestCase(2400, 1080)]
        public void IntroBackdropCoversEveryScreenPixelWithoutSafeAreaLetterboxing(int width, int height)
        {
            Rect screen = (Rect)View.GetMethod("IntroScreenRect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Vector2(width, height) });
            Assert.AreEqual(new Rect(0, 0, width, height), screen);
        }

        [TestCase("street-background-open-street-v4", 940, 1673, false)]
        [TestCase("street-victory-popup-wood-v1", 1122, 1402, true)]
        [TestCase("street-mural-real-v5", 940, 1673, false)]
        [TestCase("street-coin-gold-v2", 1254, 1254, true)]
        [TestCase("street-allboys-crest", 320, 320, true)]
        public void NewStreetBackgroundAndGoldCoinKeepNativeResolution(string name, int width, int height, bool alpha)
        {
            Texture2D texture = Resources.Load<Texture2D>(name); Assert.NotNull(texture);
            Assert.AreEqual(width, texture.width); Assert.AreEqual(height, texture.height);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + name + ".png");
            Assert.NotNull(importer); Assert.AreEqual(alpha, importer.DoesSourceTextureHaveAlpha());
            Assert.IsFalse(importer.mipmapEnabled); Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            Assert.GreaterOrEqual(android.maxTextureSize, Math.Max(width, height));
        }

        [TestCase("PlayingLevelBadge", true, 300f)]
        [TestCase("ReadyLevelBadge", true, 300f)]
        [TestCase("PlayingLevelBadge", false, 600f)]
        public void LevelBadgeCentersIconAndCaptionWithoutCoveringControls(string field, bool includeCrest, float width)
        {
            Rect bounds = (Rect)View.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect[] layout = (Rect[])View.GetMethod("LevelBadgeLayout", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bounds, width, includeCrest });
            Assert.GreaterOrEqual(bounds.yMin, 884, "Keep team stats unobstructed");
            foreach (Rect r in layout)
            {
                Assert.GreaterOrEqual(r.xMin, bounds.xMin); Assert.LessOrEqual(r.xMax, bounds.xMax);
                Assert.GreaterOrEqual(r.yMin, bounds.yMin); Assert.LessOrEqual(r.yMax, bounds.yMax);
                foreach (string control in new[] { "Speed", "HireParrillero" })
                    Assert.IsFalse(r.Overlaps((Rect)View.GetField(control, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)));
            }
            Assert.IsFalse(layout[0].Overlaps(layout[1]));
            Assert.AreEqual(bounds.center.x, (layout[0].xMin + layout[1].xMax) * .5f, .01f);
            if (field == "ReadyLevelBadge")
                for (int i = 0; i < 5; i++)
                    Assert.IsFalse(bounds.Overlaps((Rect)View.GetMethod("LevelButton", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { i })));
            Assert.AreEqual(includeCrest, layout[0].width > 0f);
        }

        [Test]
        public void LevelSelectorCardsFitPortraitCanvasAndUseTeamMuralResources()
        {
            MethodInfo cardBounds = View.GetMethod("LevelSelectCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect[] cards = new Rect[11];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = (Rect)cardBounds.Invoke(null, new object[] { i });
                Assert.GreaterOrEqual(cards[i].xMin, 0f); Assert.LessOrEqual(cards[i].xMax, 540f);
                Assert.GreaterOrEqual(cards[i].yMin, 0f); Assert.LessOrEqual(cards[i].yMax, 960f);
                Assert.LessOrEqual(cards[i].yMax, 652f, "Cards must leave room for pagination/back");
                for (int j = i / 6 * 6; j < i; j++) Assert.IsFalse(cards[i].Overlaps(cards[j]), "Same-page cards must have separate touch targets");
            }

            Assert.AreEqual("street-mural-real-v5", View.GetField("MuralResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.AreEqual("street-background-chicago-v1", View.GetField("ChicagoBackgroundResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.NotNull(Resources.Load<Texture2D>("street-mural-real-v5"), "Floresta card uses the All Boys mural");
            Assert.NotNull(Resources.Load<Texture2D>("street-allboys-crest"));
            Assert.NotNull(Resources.Load<Texture2D>("street-background-chicago-v1"), "Nueva Chicago card uses its own mural scene");
            Assert.NotNull(Resources.Load<Texture2D>("street-new-chicago-crest-v1"));
        }

        [Test]
        public void FlorestaBadgeHasOriginalCrestAndBundledComicFont()
        {
            Assert.AreEqual("street-allboys-crest", View.GetField("AllBoysCrestResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.NotNull(Resources.Load<Texture2D>("street-allboys-crest"));
            Assert.NotNull(Resources.Load<Font>("Menu/LuckiestGuy-Regular"));
        }

        [Test]
        public void MatchedCocacoleroPosesContainOnlyTheirAuthoredRows()
        {
            Texture2D atlas = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
            Assert.NotNull(atlas);
            Rect[] poses = (Rect[])View.GetField("MatchedCocacoleroPoses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(16, poses.Length);
            int[] rows = { 0, 350, 670, 963, 1261 };
            for (int i = 0; i < poses.Length; i++)
            {
                Assert.GreaterOrEqual(poses[i].yMin, rows[i / 4]);
                Assert.LessOrEqual(poses[i].yMax, rows[i / 4 + 1], "Never include neighboring feet or clip this pose's feet");
                Assert.Greater(poses[i].height, 240f);
                Assert.GreaterOrEqual(poses[i].xMin, 0f);
                Assert.LessOrEqual(poses[i].xMax, 1247f);
                Rect imported = (Rect)View.GetMethod("MatchedCocacoleroPose", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { i, new Vector2(atlas.width, atlas.height) });
                Assert.LessOrEqual(imported.xMax, atlas.width);
                Assert.AreEqual(poses[i].yMin / 1261f, imported.yMin / atlas.height, .0001f,
                    "Importer downscaling must not change the source UV row");
            }
        }

        [Test]
        public void ChicagoCocacoleroAtlasImportsAllSixteenRedApronPoses()
        {
            const string resource = "street-cocacolero-v1";
            const string path = "Assets/Art/Street/Resources/street-cocacolero-v1.png";
            Texture2D atlas = Resources.Load<Texture2D>(resource);
            Assert.NotNull(atlas);
            Assert.AreEqual(1247, atlas.width); Assert.AreEqual(1261, atlas.height);
            Rect[] poses = (Rect[])View.GetField("CocacoleroPoses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(16, poses.Length, "Idle, directional strides, pickup, handoff, and shared extra poses stay indexed like Parrillero.");
            foreach (Rect pose in poses)
            {
                Assert.GreaterOrEqual(pose.xMin, 0); Assert.GreaterOrEqual(pose.yMin, 0);
                Assert.LessOrEqual(pose.xMax, atlas.width); Assert.LessOrEqual(pose.yMax, atlas.height);
                Assert.Greater(pose.width, 0); Assert.Greater(pose.height, 0);
            }
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsFalse(importer.mipmapEnabled); Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.AreEqual(TextureImporterFormat.RGBA32, importer.GetPlatformTextureSettings("Android").format);
        }

        [Test]
        public void ChicagoLocationLoadsItsMuralCrestBottleAndBlueBarrelWithSidePickupLayout()
        {
            foreach (string name in new[] { "street-background-chicago-v1", "street-new-chicago-crest-v1", "street-coca-bottle-v1", "street-beverage-barrel-v1" })
                Assert.NotNull(Resources.Load<Texture2D>(name), "Missing Nueva Chicago art: " + name);
            Assert.AreEqual("street-new-chicago-crest-v1", View.GetField("ChicagoCrestResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Texture2D bottle = Resources.Load<Texture2D>("street-coca-bottle-v1");
            Texture2D barrel = Resources.Load<Texture2D>("street-beverage-barrel-v1");
            Assert.AreEqual(1024, bottle.width); Assert.AreEqual(1536, bottle.height);
            Assert.AreEqual(1024, barrel.width); Assert.AreEqual(1536, barrel.height);
            Texture2D chicagoGrill = Resources.Load<Texture2D>("street-parrilla-large-v4");
            Assert.NotNull(chicagoGrill, "Chicago needs its four-row grill artwork");
            Assert.AreEqual(2170, chicagoGrill.width); Assert.AreEqual(725, chicagoGrill.height);
            TextureImporter chicagoGrillImporter = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/street-parrilla-large-v4.png");
            Assert.NotNull(chicagoGrillImporter); Assert.IsTrue(chicagoGrillImporter.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(chicagoGrillImporter.alphaIsTransparency); Assert.IsFalse(chicagoGrillImporter.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, chicagoGrillImporter.textureCompression);
            Assert.AreEqual(TextureImporterNPOTScale.None, chicagoGrillImporter.npotScale);
            Assert.AreEqual(TextureImporterFormat.RGBA32, chicagoGrillImporter.GetPlatformTextureSettings("Android").format);
            foreach (string name in new[] { "street-new-chicago-crest-v1", "street-coca-bottle-v1", "street-beverage-barrel-v1" })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + name + ".png");
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.AreEqual(TextureImporterFormat.RGBA32, importer.GetPlatformTextureSettings("Android").format);
            }

            Rect grill = (Rect)View.GetMethod("GrillRectForLevel", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int) }, null)
                .Invoke(null, new object[] { 2, 1 });
            Rect table = (Rect)View.GetMethod("ServingTableBoundsForLevel", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int) }, null)
                .Invoke(null, new object[] { 2, 1 });
            Rect barrelBounds = (Rect)View.GetField("ChicagoBarrelRect", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(new Rect(242, 577, 282, 94), grill);
            Assert.AreEqual(new Rect(16, 540, 196, 98), table);
            Assert.AreEqual(new Rect(524f - 80f * 2f / 3f, 488, 80f * 2f / 3f, 80), barrelBounds);
            Assert.IsFalse(table.Overlaps(grill)); Assert.IsFalse(barrelBounds.Overlaps(grill));
            Assert.Less(grill.yMax, 710f, "Fixed-size stations stay above the unchanged HUD");
            Type simulation = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Assert.AreEqual(new Vector2(235, 575), simulation.GetMethod("StationPositionForLevel").Invoke(null, new object[] { 0, 1, 2 }));
            Assert.AreEqual(new Vector2(435, 565), simulation.GetMethod("StationPositionForLevel").Invoke(null, new object[] { 4, 1, 2 }));
            Rect velezBarrel = (Rect)View.GetField("VelezBarrelRect", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(barrelBounds, velezBarrel, "Barrel proportions no longer change with the club");
        }

        [TestCase(false, 9, 100f, false, true)]
        [TestCase(false, 10, 1f, false, true)]
        [TestCase(false, 9, -100f, false, false)]
        [TestCase(true, 9, -1f, false, true)]
        [TestCase(true, 10, 100f, false, false)]
        [TestCase(false, 7, 0f, true, true)]
        [TestCase(true, 7, 0f, true, true)]
        public void ChicagoWorkerFacingMatchesSideReachAndNearTargetTravel(bool coca, int frame, float x, bool pickup, bool expected)
        {
            MethodInfo method = View.GetMethod("ChicagoWorkerFlip", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual(expected, method.Invoke(null, new object[] { coca, frame, new Vector2(x, 0), pickup }));
        }

        [Test]
        public void ChicagoCocaWalkingAndReachFramesUseAuthoredRowsWithoutIdleOrHairFragments()
        {
            Rect[] poses = (Rect[])View.GetField("ChicagoCocacoleroPoses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Texture2D atlas = Resources.Load<Texture2D>("street-cocacolero-v1");
            Assert.AreEqual(16, poses.Length);
            foreach (Rect pose in poses)
            {
                Assert.Greater(pose.width, 0); Assert.Greater(pose.height, 0);
                Assert.GreaterOrEqual(pose.xMin, 0); Assert.GreaterOrEqual(pose.yMin, 0);
                Assert.LessOrEqual(pose.xMax, atlas.width); Assert.LessOrEqual(pose.yMax, atlas.height);
            }
            Assert.AreEqual(new Rect(103, 6, 165, 291), poses[0], "Idle is the real front idle, not the bottom-row carry frame");
            Assert.Less(poses[1].y, 315); Assert.Less(poses[2].y, 315, "Front strides come from the first row");
            Assert.AreEqual(315f, poses[5].y); Assert.AreEqual(315f, poses[6].y, "Back strides come from the second row");
            Assert.AreEqual(630f, poses[9].y); Assert.AreEqual(630f, poses[10].y, "Side strides come from the third row");
            Assert.AreEqual(new Rect(984, 315, 215, 302), poses[7], "Use the existing extended side reach beside the left barrel");
            Assert.Less(poses[9].yMax, 931f); Assert.Less(poses[10].yMax, 931f, "Walking must not include a bottom-row hair fragment");
            Assert.AreEqual(890f, poses[11].yMax, "Low pickup excludes the unrelated hair fragment at y931");
        }

        [Test]
        public void RealMuralLayerOnlyCoversFarWallAndPreservesBaseBackground()
        {
            string name = (string)View.GetField("MuralResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.AreEqual("street-mural-real-v5", name);
            Texture2D mural = Resources.Load<Texture2D>(name);
            Assert.NotNull(mural);
            Assert.NotNull(Resources.Load<Texture2D>("street-background-open-street-v4"));
            Rect source = (Rect)View.GetField("MuralSource", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect target = (Rect)View.GetField("MuralBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(new Rect(0, 0, 940, 280), source);
            Assert.LessOrEqual(source.xMax, mural.width); Assert.LessOrEqual(source.yMax, mural.height);
            Assert.AreEqual(new Rect(0, 0, 540, 118), target);
            Assert.Less(target.yMax, 120f, "Cover the full wall band without extending into the broad street");
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID("Assets/Art/Street/Resources/" + name + ".png"),
                AssetDatabase.AssetPathToGUID("Assets/Art/Street/Resources/street-background-open-street-v4.png"));
        }

        [Test]
        public void PanFrancesStreetGrillPreservesAlphaAndOriginalResolution()
        {
            const string path = "Assets/Art/Street/Resources/street-parrilla-large-v2.png";
            Texture2D texture = Resources.Load<Texture2D>("street-parrilla-large-v2");
            Assert.NotNull(texture);
            Assert.AreEqual(2172, texture.width); Assert.AreEqual(724, texture.height);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency); Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            Assert.GreaterOrEqual(android.maxTextureSize, texture.width);
            Assert.NotNull(Resources.Load<Texture2D>("street-parrilla-large"), "Retain the previous grill resource");
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(path), AssetDatabase.AssetPathToGUID("Assets/Art/Street/Resources/street-parrilla-large.png"));
            Assert.NotNull(Resources.Load<Texture2D>("street-items"), "Keep original fallback and drinks");
        }

        [Test]
        public void ChoripanIsNotDuplicatedAsFloatingStationBadge()
        {
            MethodInfo shouldDraw = View.GetMethod("ShouldDrawStationProductBadge", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(shouldDraw);
            Assert.IsFalse((bool)shouldDraw.Invoke(null, new object[] { 0 }));
            Assert.IsTrue((bool)shouldDraw.Invoke(null, new object[] { 1 }));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SharedGrillIsLargeAndClearOfHudAndButtons(int products)
        {
            Rect r = (Rect)View.GetMethod("GrillRect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { products });
            Assert.Greater(r.width, 4 * 66f);
            Assert.GreaterOrEqual(r.xMin, 0); Assert.LessOrEqual(r.xMax, 540);
            Assert.LessOrEqual(r.yMax, 704, "Grill must stop before the upgrade HUD");
            Assert.AreEqual(new Vector2(282, 94), r.size, "Floresta dimensions remain fixed at every product count");
            Rect speed = (Rect)View.GetField("Speed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect hire = (Rect)View.GetField("HireParrillero", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsFalse(r.Overlaps(speed)); Assert.IsFalse(r.Overlaps(hire));
            Type sim = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Vector2 anchor = (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 0 });
            Assert.AreEqual(new Vector2(235, 575), anchor, "Choripán pickups now happen beside the grill at the loaded trestle table");
            Assert.AreEqual(new Vector2(235, 575), (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 1 }),
                "Finished foods are fetched from the standard table, never the hot grate");
        }

        [Test]
        public void FlorestaTableIsBesideGrillAndBothTravelLegsStayClear()
        {
            Rect grill = (Rect)View.GetMethod("GrillRect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 1 });
            Rect table = (Rect)View.GetField("ServingTableRect", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsFalse(table.Overlaps(grill));
            Assert.Less(table.xMax, grill.xMin);
            Type sim = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Vector2 approach = (Vector2)sim.GetMethod("StationApproachPoint").Invoke(null, null);
            Vector2 pickup = (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 0 });
            Assert.Less(approach.y, grill.yMin);
            Assert.Less(pickup.x, grill.xMin);
            Assert.IsFalse(table.Contains(pickup));
            Assert.Less(table.xMax, pickup.x);
            // Every possible counter destination and the initial spawn use this lane.
            for (int column = 0; column < 7; column++)
            for (int sample = 0; sample <= 100; sample++)
            {
                float t = sample / 100f;
                Assert.IsFalse(grill.Contains(Vector2.Lerp(new Vector2(58 + 70 * column, 400), approach, t)));
                Assert.IsFalse(grill.Contains(Vector2.Lerp(new Vector2(433, 430), approach, t)));
                Assert.IsFalse(grill.Contains(Vector2.Lerp(approach, pickup, t)));
            }
            Assert.AreEqual(new Vector2(235, 575), (Vector2)sim.GetMethod("StationPositionForProductCount")
                .Invoke(null, new object[] { 0, 3 }), "Product counts must not resize/reposition the standard pickup");
        }

        [Test]
        public void TabletSelectorCardsAndPurchaseCardsFitRowsAndKeepTheirVisibleBounds()
        {
            const float verticalScale = .75f;
            MethodInfo cardBounds = View.GetMethod("LevelSelectCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo scaleCard = View.GetMethod("ScaleCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect previous = default;
            for (int i = 0; i < 6; i++)
            {
                Rect logical = (Rect)cardBounds.Invoke(null, new object[] { i });
                Rect responsive = (Rect)scaleCard.Invoke(null, new object[] { logical, verticalScale });
                Rect drawn = new Rect(responsive.x, responsive.y * verticalScale, responsive.width, responsive.height);
                Assert.AreEqual(logical.y * verticalScale, drawn.y, .01f);
                Assert.AreEqual(logical.height * verticalScale, drawn.height, .01f);
                Assert.AreEqual(logical.height, responsive.height / verticalScale, .01f,
                    "Inverse pointer conversion must exactly recover the visible card height.");
                if (i > 0 && i % 2 == 0)
                    Assert.LessOrEqual(previous.yMax, drawn.yMin, "Tablet card rows cannot overlap.");
                previous = drawn;
            }
            Rect lastCard = (Rect)scaleCard.Invoke(null, new object[] { cardBounds.Invoke(null, new object[] { 4 }), verticalScale });
            float drawnLastCardBottom = (lastCard.y + lastCard.height) * verticalScale;
            Assert.Less(drawnLastCardBottom, 666f * verticalScale, "Selector pagination remains outside the last row.");

            MethodInfo catalogBounds = View.GetMethod("CatalogUpgradeCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Type simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            object balance = Activator.CreateInstance(balanceType, true);
            Type[] signature = { balanceType, typeof(int), typeof(float), typeof(int), typeof(int), typeof(int) };
            object sim = simType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, signature, null).Invoke(new object[] { balance, 4, 5f, 0, 1, 0 });
            int[] actions = (int[])View.GetMethod("CatalogUpgradeActions", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { sim });
            for (int i = 0; i < actions.Length; i++)
            {
                Rect logical = (Rect)catalogBounds.Invoke(null, new[] { sim, (object)actions[i] });
                Rect responsive = (Rect)scaleCard.Invoke(null, new object[] { logical, verticalScale });
                Assert.AreEqual(logical.height, responsive.height / verticalScale, .01f);
            }
        }

        [Test]
        public void ThemedMuralsImportAtTheirAuthoredNonPowerOfTwoDimensions()
        {
            string[] names = { "street-mural-velez-master-v1", "street-mural-ferro-master-v1",
                "street-mural-independiente-master-v1", "street-background-sanlorenzo-v1", "street-background-boca-v1",
                "street-background-camioneros-v1", "street-background-losredondos-v1" };
            Vector2Int[] dimensions = { new Vector2Int(2116, 743), new Vector2Int(1898, 829), new Vector2Int(2054, 766),
                new Vector2Int(887, 1774), new Vector2Int(887, 1774), new Vector2Int(941, 1672), new Vector2Int(941, 1672) };
            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/Art/Street/Resources/" + names[i] + ".png";
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.NotNull(importer, path);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, path);
                Texture2D texture = Resources.Load<Texture2D>(names[i]);
                Assert.NotNull(texture, path);
                Assert.AreEqual(dimensions[i].x, texture.width, path);
                Assert.AreEqual(dimensions[i].y, texture.height, path);
            }
        }

        [Test]
        public void ThemedPennantOverlayMasksRootRowWithoutChangingTransparentDefault()
        {
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            Type renderer = Type.GetType("HayChoriYPaty.ClubPennantRenderer, Assembly-CSharp", true);
            object theme = themes.GetMethod("ForLevel").Invoke(null, new object[] { 1 });
            MethodInfo create = renderer.GetMethod("CreateTexture", BindingFlags.Static | BindingFlags.Public);
            Texture2D opaque = (Texture2D)create.Invoke(null, new[] { theme, (object)true, true });
            Texture2D transparent = (Texture2D)create.Invoke(null, new[] { theme, (object)true, false });
            try
            {
                Assert.AreEqual(255, opaque.GetPixel(opaque.width - 1, opaque.height - 1).a * 255f, .01f);
                Assert.AreEqual(0, transparent.GetPixel(transparent.width - 1, transparent.height - 1).a, .01f);
            }
            finally { UnityEngine.Object.DestroyImmediate(opaque); UnityEngine.Object.DestroyImmediate(transparent); }
        }

        [Test]
        public void HudAnchorsAtPhysicalTopWhileInteractiveCanvasKeepsTheSafeViewport()
        {
            MethodInfo viewportMethod = View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic);
            Rect viewport = (Rect)viewportMethod.Invoke(null, new object[] {
                new Vector2(1200, 2640), new Rect(0, 102, 1200, 2397) });
            Assert.AreEqual(new Rect(0, 141, 1200, 2397), viewport);
            float logicalHeight = (float)View.GetMethod("CanvasLogicalHeight", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { viewport });
            Assert.AreEqual(1078.65f, logicalHeight, .1f);
            Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(0f, bar.yMin, "The read-only HUD is drawn at the physical screen top, separately from safe controls");
            foreach (string name in new[] { "HudCoins", "HudTime", "HudSales" })
            {
                Rect field = (Rect)View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.IsTrue(bar.Contains(field.min)); Assert.IsTrue(bar.Contains(field.max));
            }
            Rect punchHoleViewport = (Rect)viewportMethod.Invoke(null, new object[] {
                new Vector2(1440, 3088), new Rect(0, 0, 1440, 2999) });
            Assert.AreEqual(2999f, punchHoleViewport.height);
            foreach (string name in new[] { "CutoutHudCoins", "CutoutHudTime", "CutoutHudSales" })
                Assert.IsNull(View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic), name + " must not draw an alternate HUD");
        }

        [Test]
        public void TopCameraCutoutGetsAnActualHudGapAndSevenQuotaCellsReflowAroundIt()
        {
            MethodInfo gapMethod = View.GetMethod("TopHudCutoutBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect gap = (Rect)gapMethod.Invoke(null, new object[] {
                new Vector2(1080, 2340), new[] { new Rect(480, 2260, 120, 80) } });
            Assert.AreEqual(232f, gap.xMin, .01f);
            Assert.AreEqual(308f, gap.xMax, .01f);
            Assert.AreEqual(34f, gap.height);

            MethodInfo cellsMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(Rect) }, null);
            Rect[] cells = new Rect[7];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = (Rect)cellsMethod.Invoke(null, new object[] { i, gap });
                Assert.IsFalse(cells[i].Overlaps(gap), "Quota chip " + i + " crosses the hardware camera channel");
                for (int j = 0; j < i; j++) Assert.IsFalse(cells[i].Overlaps(cells[j]));
            }
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2340)]
        [TestCase(1080, 2400)]
        [TestCase(1440, 3200)]
        [TestCase(1536, 2048)]
        public void OneBakedCounterProjectsIntoEverySafeViewportWithoutADuplicate(int width, int height)
        {
            Vector2 size = new Vector2(width, height);
            Rect safe = new Rect(0f, height * .02f, width, height * .96f);
            Rect viewport = (Rect)View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { size, safe });
            Type scene = Type.GetType("HayChoriYPaty.StreetSceneLayout, Assembly-CSharp", true);
            float actual = (float)scene.GetMethod("CounterTopInViewport").Invoke(null, new object[] { width, height, viewport });
            float logicalScreenHeight = height * 540f / width;
            float fullScale = width / 540f;
            float expected = ((float)scene.GetMethod("CounterTop").Invoke(null, new object[] { logicalScreenHeight }) * fullScale
                - viewport.y) / (viewport.width / 540f);
            Assert.AreEqual(expected, actual, .01f);
            Assert.IsNull(View.GetMethod("DrawClubCounter", BindingFlags.Instance | BindingFlags.NonPublic),
                "The backdrop owns the only counter rendering; actors are clipped against its projection.");
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2340)]
        [TestCase(1080, 2400)]
        [TestCase(1440, 3200)]
        [TestCase(1536, 2048)]
        public void ResponsiveFooterFitsAllElevenLevelsAndKeepsButtonsFlagsAndStageSeparate(int width, int height)
        {
            Vector2 size = new Vector2(width, height);
            Rect safe = new Rect(0f, height * .02f, width, height * .96f);
            Rect viewport = (Rect)View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { size, safe });
            float canvasHeight = (float)View.GetMethod("CanvasLogicalHeight", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { viewport });
            float verticalScale = canvasHeight / 960f;
            Type kitchenLayout = Type.GetType("HayChoriYPaty.StreetKitchenLayout, Assembly-CSharp", true);
            Type simulationType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            float playfieldBottom = (float)kitchenLayout.GetField("TiledPlayfieldBottom").GetRawConstantValue();
            float stageBottom = playfieldBottom * verticalScale;
            string[] levelNames = (string[])simulationType.GetField("LevelNames").GetValue(null);
            ConstructorInfo simulationConstructor = simulationType.GetConstructor(new[] {
                balanceType, typeof(int), typeof(float), typeof(int), typeof(int), typeof(int)
            });
            MethodInfo build = View.GetMethod("BuildResponsiveFooterLayout", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo scaleCard = View.GetMethod("ScaleCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            for (int level = 0; level < levelNames.Length; level++)
            {
                object balance = Activator.CreateInstance(balanceType);
                object sim = simulationConstructor.Invoke(new object[] { balance, level, 5f, 0, 1, 0 });
                object footer = build.Invoke(null, new object[] { sim, canvasHeight, true, false });
                Type footerType = footer.GetType();
                Rect[] raw = (Rect[])footerType.GetField("UpgradeBounds").GetValue(footer);
                int[] actions = (int[])View.GetMethod("CatalogUpgradeActions", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { sim });
                Assert.AreEqual(actions.Length, raw.Length, "Every Level " + level + " catalog action remains represented.");
                Rect badge = (Rect)footerType.GetField("LevelBadge").GetValue(footer);
                Rect drawnBadge = new Rect(badge.x, badge.y * verticalScale, badge.width, badge.height);
                Rect flags = (Rect)footerType.GetField("Pennants").GetValue(footer);
                Assert.GreaterOrEqual(drawnBadge.yMin, stageBottom - .1f, "Level " + level + " footer entered the playfield.");
                for (int i = 0; i < raw.Length; i++)
                {
                    Rect card = (Rect)scaleCard.Invoke(null, new object[] { raw[i], verticalScale });
                    card.y *= verticalScale;
                    Assert.GreaterOrEqual(card.xMin, 0f); Assert.LessOrEqual(card.xMax, 540f);
                    Assert.GreaterOrEqual(card.yMin, stageBottom - .1f, "Level " + level + " card entered the playfield.");
                    Assert.LessOrEqual(card.yMax, canvasHeight + .1f, "Level " + level + " card is clipped.");
                    Assert.GreaterOrEqual(card.yMin, drawnBadge.yMax - .1f);
                    Assert.GreaterOrEqual(flags.yMin, card.yMax - .1f, "Level " + level + " decoration overlaps a card.");
                    for (int j = 0; j < i; j++)
                    {
                        Rect other = (Rect)scaleCard.Invoke(null, new object[] { raw[j], verticalScale });
                        other.y *= verticalScale;
                        Assert.IsFalse(card.Overlaps(other), "Level " + level + " upgrade cards overlap.");
                    }
                }
                Assert.GreaterOrEqual(flags.yMin, stageBottom - .1f);
                Assert.LessOrEqual(flags.yMax, canvasHeight + .1f);
                for (int i = 0; i < raw.Length; i++)
                {
                    Rect card = (Rect)scaleCard.Invoke(null, new object[] { raw[i], verticalScale });
                    card.y *= verticalScale;
                    Assert.IsFalse(card.Overlaps(flags), "Level " + level + " upgrade card " + card + " overlaps decoration " + flags + ".");
                }

                object ready = build.Invoke(null, new object[] { sim, canvasHeight, raw.Length <= 3, true });
                Rect selector = (Rect)ready.GetType().GetField("ReadySelector").GetValue(ready);
                Rect drawnSelector = new Rect(selector.x, selector.y * verticalScale, selector.width, selector.height);
                Assert.GreaterOrEqual(drawnSelector.yMin, stageBottom - .1f);
                Assert.LessOrEqual(drawnSelector.yMax, canvasHeight + .1f);
                Rect readyBadge = (Rect)ready.GetType().GetField("LevelBadge").GetValue(ready);
                Rect drawnReadyBadge = new Rect(readyBadge.x, readyBadge.y * verticalScale, readyBadge.width, readyBadge.height);
                Assert.IsFalse(drawnSelector.Overlaps(drawnReadyBadge));
                Rect readyFlags = (Rect)ready.GetType().GetField("Pennants").GetValue(ready);
                Assert.GreaterOrEqual(readyFlags.yMin, drawnSelector.yMax - .1f);
                Assert.LessOrEqual(readyFlags.yMax, canvasHeight + .1f);
            }
        }

        [Test]
        public void SimulationQueueAnchorsRemainUnchangedByCounterOcclusion()
        {
            Type sim = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            var queue = sim.GetMethod("QueuePosition", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2 front = (Vector2)queue.Invoke(null, new object[] { 0 });
            Vector2 back = (Vector2)queue.Invoke(null, new object[] { 14 });
            Assert.AreEqual(new Vector2(58, 324), front);
            Assert.AreEqual(new Vector2(58, 212), back);
            Assert.GreaterOrEqual(back.y - 132, 80, "Rear bubbles leave most of the 90-high wall visible");
        }

        [TestCase(0, 960f)]
        [TestCase(1, 960f)]
        [TestCase(2, 960f)]
        [TestCase(3, 960f)]
        [TestCase(4, 960f)]
        [TestCase(0, 1200f)]
        [TestCase(1, 1200f)]
        public void CrowdViewHidesFrontLegsAtEachBackdropCounterWithoutChangingQueue(int level, float canvasHeight)
        {
            float verticalScale = canvasHeight / 960f;
            float counterTop = (float)View.GetMethod("CounterSurfaceY", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight, new Vector2(940, 1673), level });
            float offset = (float)View.GetMethod("CustomerViewOffsetY", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { counterTop, verticalScale });
            float hiddenLegs = (float)View.GetField("CustomerHiddenLegHeight", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            float bodyTop = (324f + offset - 76f) * verticalScale;
            float feet = bodyTop + 76f;
            Assert.AreEqual(counterTop + hiddenLegs, feet, .01f);
            Assert.Less(bodyTop, counterTop, "Face and upper body remain above the counter clipping edge");
            Assert.GreaterOrEqual(hiddenLegs, 28f, "Legs remain occluded even during small waiting hops");
            float rearBubbleTop = (212f + offset - (level == 1 ? 151f : 132f)) * verticalScale;
            Assert.Greater(rearBubbleTop, 68f, "The enlarged physical-edge HUD stays clear of rear orders");
        }

        [TestCase("street-upgrade-wood-v1", 1510, 1041)]
        [TestCase("street-speed-arrows-v1", 1402, 1122)]
        public void ParrillaCriollaUpgradeArtworkImportsAtFullResolutionWithAlpha(string resource, int width, int height)
        {
            Texture2D texture = Resources.Load<Texture2D>(resource);
            Assert.NotNull(texture, "Missing upgrade artwork " + resource);
            Assert.AreEqual(width, texture.width);
            Assert.AreEqual(height, texture.height);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + resource + ".png");
            Assert.NotNull(importer);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden);
            Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            Assert.GreaterOrEqual(android.maxTextureSize, Math.Max(width, height));
        }

        [Test]
        public void ParrillaCriollaUpgradeCardsFitCanvasKeepSeparateTargetsAndFourDigitPrices()
        {
            Rect speed = (Rect)View.GetField("Speed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect hire = (Rect)View.GetField("HireParrillero", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.GreaterOrEqual(speed.xMin, 0); Assert.LessOrEqual(speed.xMax, 540);
            Assert.GreaterOrEqual(hire.xMin, 0); Assert.LessOrEqual(hire.xMax, 540);
            Assert.IsFalse(speed.Overlaps(hire), "Cards must retain independent full-card touch targets");
            Assert.GreaterOrEqual(speed.yMin, 644, "Cards remain below the grill");
            Assert.LessOrEqual(Mathf.Max(speed.yMax, hire.yMax), 858, "Leave the worker/speed footer clear");

            Rect wood = (Rect)View.GetField("UpgradeWoodBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect arrows = (Rect)View.GetField("SpeedArrowBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Texture2D woodTexture = Resources.Load<Texture2D>("street-upgrade-wood-v1");
            Texture2D arrowTexture = Resources.Load<Texture2D>("street-speed-arrows-v1");
            foreach (var entry in new[] { new { Bounds = wood, Texture = woodTexture }, new { Bounds = arrows, Texture = arrowTexture } })
            {
                Assert.NotNull(entry.Texture);
                Assert.Greater(entry.Bounds.width, 0); Assert.Greater(entry.Bounds.height, 0);
                Assert.GreaterOrEqual(entry.Bounds.xMin, 0); Assert.GreaterOrEqual(entry.Bounds.yMin, 0);
                Assert.LessOrEqual(entry.Bounds.xMax, entry.Texture.width);
                Assert.LessOrEqual(entry.Bounds.yMax, entry.Texture.height);
            }

            Rect price = (Rect)View.GetMethod("UpgradePriceTextBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { speed });
            Assert.AreEqual(speed.width - 145, price.width);
            Font font = Resources.Load<Font>("Menu/LuckiestGuy-Regular");
            Assert.NotNull(font);
            var style = new GUIStyle { font = font, fontSize = 27, wordWrap = false };
            MethodInfo fit = View.GetMethod("FitUpgradePriceFontSize", BindingFlags.Static | BindingFlags.NonPublic);
            int fitted = (int)fit.Invoke(null, new object[] { style, "2800", price.width });
            style.fontSize = fitted;
            Assert.GreaterOrEqual(fitted, 16);
            Assert.LessOrEqual(style.CalcSize(new GUIContent("2800")).x, price.width,
                "The largest current hire cost must fit on the brass plaque");
        }

        private Rect SpecialtyCard(int action)
        {
            return (Rect)View.GetMethod("UpgradeCardBounds",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{action,true});
        }
        [Test]
        public void ChicagoShowsThreeUpgradeButtons()
        {
            Rect[] cards={SpecialtyCard(3),SpecialtyCard(2),SpecialtyCard(16)};
            Assert.AreEqual(12,cards[1].xMin-cards[0].xMax); Assert.AreEqual(12,cards[2].xMin-cards[1].xMax);
            var style=new GUIStyle{font=Resources.Load<Font>("Menu/LuckiestGuy-Regular"),fontSize=24,wordWrap=false};
            foreach(Rect card in cards)
            {
                Assert.AreEqual(710,card.y); Assert.GreaterOrEqual(card.xMin,0); Assert.LessOrEqual(card.xMax,540);
                foreach(string method in new[]{"UpgradeTitleBounds","UpgradeStatusBounds","UpgradeCoinBounds","UpgradePriceTextBounds"})
                {
                    Rect slot=(Rect)View.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{card});
                    Assert.IsTrue(card.Contains(slot.min)); Assert.IsTrue(card.Contains(slot.max-Vector2.one*.01f));
                }
                Rect price=(Rect)View.GetMethod("UpgradePriceTextBounds",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{card});
                foreach(string value in new[]{"2800","MAX"})
                {
                    style.fontSize=24;
                    int size=(int)View.GetMethod("FitUpgradePriceFontSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{style,value,price.width});
                    style.fontSize=size; Assert.LessOrEqual(style.CalcSize(new GUIContent(value)).x,price.width);
                }
            }
        }
        [TestCase(1f)] [TestCase(1.2504f)]
        public void UpgradeTouchTargetsMatchAnchoredArtOnTallScreens(float vertical)
        {
            foreach(int action in new[]{3,2,16})
            {
                Rect art=SpecialtyCard(action);
                Rect touch=(Rect)View.GetMethod("UpgradeTouchBounds",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{action,true,vertical});
                Assert.AreEqual(art.y*vertical,touch.y*vertical); Assert.AreEqual(art.height,touch.height*vertical,.001f);
                Assert.IsTrue(touch.Contains(new Vector2(touch.center.x,touch.y+119f/vertical)));
                Assert.IsFalse(touch.Contains(new Vector2(touch.center.x,touch.y+121f/vertical)));
            }
        }
        [Test] public void ParrilleroButtonIsVisible(){Assert.AreEqual(new Rect(192,710,156,120),SpecialtyCard(2));}
        [Test] public void CocacoleroButtonIsVisible(){Assert.AreEqual(new Rect(360,710,156,120),SpecialtyCard(16));}
        [Test] public void SpeedButtonIsVisible(){Assert.AreEqual(new Rect(24,710,156,120),SpecialtyCard(3));}

        [Test]
        public void UnifiedTopHudFitsAboveRearOrdersAndSeparatesCounters()
        {
Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect grid = (Rect)View.GetField("HudQuotaGrid", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect coins = (Rect)View.GetField("HudCoins", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect clock = (Rect)View.GetField("HudTime", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(0f, bar.yMin); Assert.AreEqual(68f, bar.height);
            Assert.AreEqual(new Rect(204, 1, 334, 66), grid);
            Assert.IsTrue(bar.Contains(coins.min)); Assert.IsTrue(bar.Contains(coins.max));
            Assert.IsTrue(bar.Contains(clock.min)); Assert.IsTrue(bar.Contains(clock.max));
            Assert.IsFalse(coins.Overlaps(clock)); Assert.IsFalse(coins.Overlaps(grid)); Assert.IsFalse(clock.Overlaps(grid));
            Assert.AreEqual(new Vector2(82, 48), clock.size);
            Assert.That(Mathf.Abs(clock.center.y - bar.center.y), Is.LessThanOrEqualTo(1f));
            MethodInfo cellMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int) }, null);
            Rect[] cells = new Rect[7];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = (Rect)cellMethod.Invoke(null, new object[] { i });
                Assert.IsTrue(grid.Contains(cells[i].min)); Assert.IsTrue(grid.Contains(cells[i].max));
                for (int j = 0; j < i; j++) Assert.IsFalse(cells[i].Overlaps(cells[j]));
            }
            foreach (string name in new[] { "CutoutHudCoins", "CutoutHudTime", "CutoutHudSales" })
                Assert.IsNull(View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic), name + " is not a supported HUD layout");
            Assert.AreEqual(48, (int)View.GetField("HudClockFontSize", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
        }

        [TestCase(900f, 500f)]
        [TestCase(512f, 512f)]
        [TestCase(100f, 600f)]
        public void LargerHudIconsFitTheirSlotWithoutStretchingAndRemainCentered(float width, float height)
        {
            Rect slot = new Rect(338, 10, 43, 45);
            Rect fitted = (Rect)View.GetMethod("HudIconBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { slot, new Vector2(width, height) });
            Assert.AreEqual(slot.center.x, fitted.center.x, .01f); Assert.AreEqual(slot.center.y, fitted.center.y, .01f);
            Assert.LessOrEqual(fitted.width, slot.width); Assert.LessOrEqual(fitted.height, slot.height);
            Assert.AreEqual(width / height, fitted.width / fitted.height, .001f);
        }

        [TestCase(180f, "3:00")]
        [TestCase(120f, "2:00")]
        [TestCase(151f, "2:31")]
        [TestCase(59.1f, "1:00")]
        [TestCase(.1f, "0:01")]
        [TestCase(0f, "0:00")]
        [TestCase(-.1f, "0:00")]
        public void HudCountdownUsesSimulationTimeRoundedUpAndNeverNegative(float seconds, string expected)
        {
            var format = View.GetMethod("FormatRemainingTime", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual(expected, format.Invoke(null, new object[] { seconds }));
        }

        [Test]
        public void UnifiedHudRuntimeTextureIsCachedSizedAndDisposable()
        {
            var create = View.GetMethod("CreateHudBar", BindingFlags.Static | BindingFlags.NonPublic);
            Texture2D texture = (Texture2D)create.Invoke(null, null);
            Texture2D cutout = (Texture2D)View.GetMethod("CreateHudBarTexture", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(bool) }, null).Invoke(null, new object[] { true });
            try
            {
                Assert.AreEqual(1080, texture.width); Assert.AreEqual(136, texture.height);
                Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
                Assert.IsTrue(texture.name.Contains("clock"));
                Assert.AreEqual(HideFlags.HideAndDontSave, texture.hideFlags);
                // Runtime textures deliberately discard their CPU copy after upload.
                Assert.IsFalse(texture.isReadable); Assert.IsFalse(cutout.isReadable);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(cutout); }
        }

        [TestCase(700f)]
        [TestCase(960f)]
        [TestCase(1200f)]
        public void VictoryPopupAndExitUseOneUniformPortraitFrame(float canvasHeight)
        {
            Rect frame = (Rect)View.GetMethod("VictoryPopupBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight });
            Rect exitSlot = (Rect)View.GetField("VictoryExit", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect exit = (Rect)View.GetMethod("VictorySlot", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { frame, exitSlot });
            Assert.GreaterOrEqual(frame.xMin, 0f); Assert.LessOrEqual(frame.xMax, 540f);
            Assert.GreaterOrEqual(frame.yMin, 16f); Assert.LessOrEqual(frame.yMax, canvasHeight - 16f);
            Assert.AreEqual(1122f / 1402f, frame.width / frame.height, .001f);
            Assert.IsTrue(frame.Contains(exit.min)); Assert.IsTrue(frame.Contains(exit.max));
            Assert.Greater(exit.yMin, frame.center.y, "Only the green bottom button is interactive");
        }

        [TestCase("200/200", 40)]
        [TestCase("2147483647", 42)]
        [TestCase("CHORI 200/200\nCOCA 200/200", 27)]
        public void VictoryLiveValuesFitTheirSlotsWithoutBakingNumbers(string value, int preferred)
        {
            var style = new GUIStyle { font = Resources.Load<Font>("Menu/LuckiestGuy-Regular"), fontSize = 38, wordWrap = false };
            Rect bounds = new Rect(0, 0, 190, 68);
            int size = (int)View.GetMethod("FitVictoryFont", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { style, value, preferred, bounds });
            Assert.AreEqual(38, style.fontSize, "Fitting must not mutate the shared style");
            style.fontSize = size;
            Assert.LessOrEqual(style.CalcSize(new GUIContent(value)).x, bounds.width - 4f);
            Assert.LessOrEqual(style.CalcSize(new GUIContent(value)).y, bounds.height - 2f);
        }

        [Test]
        public void SevenQuotaResultsFitCompactlyAndDefeatListsOnlyPendingProducts()
        {
            Type simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            object sim = Activator.CreateInstance(simType, new object[] { Activator.CreateInstance(balanceType), 4, 5f, 0, 1, 0 });
            MethodInfo summary = View.GetMethod("FormatQuotaSummary", BindingFlags.Static | BindingFlags.NonPublic);
            string complete = (string)summary.Invoke(null, new object[] { sim, false });
            string pending = (string)summary.Invoke(null, new object[] { sim, true });
            Assert.AreEqual(2, complete.Split('\n').Length);
            foreach (string name in new[] { "CHORI", "PATY", "BOND", "VACÍO", "COCA", "FERNET", "CERVEZA" })
            {
                Assert.IsTrue(complete.Contains(name));
                Assert.IsTrue(pending.Contains(name));
            }
        }

        [TestCase(21f, "00:21")]
        [TestCase(120f, "02:00")]
        [TestCase(0f, "00:00")]
        [TestCase(-1f, "00:00")]
        public void VictoryRemainingTimeUsesTwoDigitMinutes(float seconds, string expected)
        {
            Assert.AreEqual(expected, View.GetMethod("FormatVictoryTime", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { seconds }));
        }

        [Test]
        public void VisibleRoleIsParrilleroAndExistingFansRemainAvailable()
        {
            Assert.AreEqual("Parrillero", View.GetField("ParrilleroLabel", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.NotNull(Resources.Load<Texture2D>("street-characters"));
            Rect[] fans = (Rect[])View.GetField("People", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(16, fans.Length);
            Assert.AreEqual(new Rect(49, 631, 236, 306), fans[8]);
            Assert.NotNull(Resources.Load<Texture2D>("street-items"));
        }

        [Test]
        public void VelezLevelLoadsItsMuralCrestFoodAndIntegratedTeamWardrobes()
        {
            Texture2D background = Resources.Load<Texture2D>("street-background-velez-v1");
            Texture2D crest = Resources.Load<Texture2D>("street-velez-crest-v1");
            Texture2D front = Resources.Load<Texture2D>("street-velez-fans-front-v1");
            Texture2D walk = Resources.Load<Texture2D>("street-velez-fans-walk-v1");
            Texture2D riot = Resources.Load<Texture2D>("street-velez-riot-fans-v1");
            Texture2D grill = Resources.Load<Texture2D>("street-parrilla-velez-v1");
            Assert.NotNull(background); Assert.NotNull(crest); Assert.NotNull(front); Assert.NotNull(walk); Assert.NotNull(riot); Assert.NotNull(grill);
            Assert.AreEqual(941, background.width); Assert.AreEqual(1672, background.height);
            Assert.AreEqual(1254, front.width); Assert.AreEqual(1254, front.height);
            Assert.AreEqual(front.width, walk.width); Assert.AreEqual(front.height, walk.height);
            Assert.AreEqual(1536, riot.width); Assert.AreEqual(1024, riot.height);
            Assert.AreEqual(2172, grill.width); Assert.AreEqual(724, grill.height);

            foreach (string path in new[] {
                "Assets/Art/Street/Resources/street-velez-fans-front-v1.png",
                "Assets/Art/Street/Resources/street-velez-fans-walk-v1.png",
                "Assets/Art/Street/Resources/street-velez-riot-fans-v1.png",
                "Assets/Art/Street/Resources/street-parrilla-velez-v1.png",
                "Assets/Art/Street/Resources/street-velez-crest-v1.png"
            })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.NotNull(importer); Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsTrue(importer.alphaIsTransparency); Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            }

            int count = (int)View.GetField("VelezFanVariantCount", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.AreEqual(9, count);
            MethodInfo source = View.GetMethod("VelezFanSourceRect", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo riotSource = View.GetMethod("AllBoysRiotFanFrameSource", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(source); Assert.NotNull(riotSource);
            for (int variant = 0; variant < 9; variant++)
            {
                Rect idle = (Rect)source.Invoke(null, new object[] { front, variant });
                Rect walking = (Rect)source.Invoke(null, new object[] { walk, variant });
                Assert.AreEqual(variant % 3, Mathf.RoundToInt(idle.x / (front.width / 3f)));
                Assert.AreEqual(variant / 3, Mathf.RoundToInt((front.height - idle.yMax) / (front.height / 3f)));
                Assert.AreEqual(idle.width, walking.width); Assert.AreEqual(idle.height, walking.height);
                Rect angry = (Rect)riotSource.Invoke(null, new object[] { riot, variant, 0 });
                Rect swing = (Rect)riotSource.Invoke(null, new object[] { riot, variant, 1 });
                Assert.AreEqual(angry.width, swing.width); Assert.AreEqual(angry.height, swing.height);
                Assert.AreEqual(angry.y, swing.y);
            }
            Assert.NotNull(Resources.Load<Texture2D>("street-items"), "The existing distinct Paty sandwich icon is reused.");
        }

        [Test]
        public void VelezKitchenKeepsStandardStationsAndHidesFoodZoneCaptions()
        {
            Type layout = Type.GetType("HayChoriYPaty.StreetWorkstationLayout, Assembly-CSharp", true);
            Rect table = (Rect)layout.GetProperty("FoodTableBounds", BindingFlags.Static | BindingFlags.Public).GetValue(null, null);
            Rect grill = (Rect)View.GetMethod("GrillRectForLevel", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 3, 2 });
            Rect velezTable = (Rect)View.GetMethod("ServingTableBoundsForLevel", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 3, 2 });
            Rect barrel = (Rect)layout.GetProperty("CocaBounds", BindingFlags.Static | BindingFlags.Public).GetValue(null, null);

            Assert.AreEqual(new Vector2(196, 98), table.size);
            Assert.AreEqual(new Vector2(282, 94), grill.size);
            Assert.AreEqual(table, velezTable, "Vélez uses the same always-present ready-product table frame as the other levels.");
            Assert.AreEqual(new Vector2(80f * 2f / 3f, 80), barrel.size);
            Assert.NotNull(Resources.Load<Texture2D>("street-serving-table-v1"), "The shared tabletop art is available to DrawServingTable.");

            MethodInfo label = View.GetMethod("StationLabelForProduct", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(label);
            for (int food = 0; food < 4; food++)
                Assert.AreEqual(string.Empty, label.Invoke(null, new object[] { food }), "Food zones must not add CHORI/PATY floor captions.");
            Assert.AreEqual("COCA 600 ml", label.Invoke(null, new object[] { 4 }), "Keep the existing drink-station label.");
        }

        [Test]
        public void FerroAndIndependienteHaveDistinctBackgroundsMatchedFullBodyCrowdAndStationArt()
        {
            string[] resources = {
                "street-background-ferro-v1", "street-ferro-fans-front-v1", "street-ferro-fans-walk-v1", "street-ferro-riot-fans-v1",
                "street-background-independiente-v1", "street-independiente-fans-front-v1", "street-independiente-fans-walk-v1", "street-independiente-riot-fans-v1",
                "street-grill-four-zones-v1", "street-ready-sandwiches-table-v1", "street-beer-barrel-v1", "street-fernet-table-v1", "street-coca-bottle-v1"
            };
            foreach (string resource in resources) Assert.NotNull(Resources.Load<Texture2D>(resource), "Missing Resources asset: " + resource);

            foreach (string path in new[] {
                "Assets/Art/Street/Resources/street-ferro-fans-front-v1.png", "Assets/Art/Street/Resources/street-ferro-fans-walk-v1.png",
                "Assets/Art/Street/Resources/street-ferro-riot-fans-v1.png", "Assets/Art/Street/Resources/street-independiente-fans-front-v1.png",
                "Assets/Art/Street/Resources/street-independiente-fans-walk-v1.png", "Assets/Art/Street/Resources/street-independiente-riot-fans-v1.png"
            })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.NotNull(importer, "Unity must import the team outfit atlas: " + path);
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha()); Assert.IsTrue(importer.alphaIsTransparency);
                Assert.IsFalse(importer.mipmapEnabled); Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.RGBA32, android.format);
            }

            Texture2D ferroFront = Resources.Load<Texture2D>("street-ferro-fans-front-v1");
            Texture2D ferroWalk = Resources.Load<Texture2D>("street-ferro-fans-walk-v1");
            Texture2D ferroRiot = Resources.Load<Texture2D>("street-ferro-riot-fans-v1");
            Texture2D rojoFront = Resources.Load<Texture2D>("street-independiente-fans-front-v1");
            Texture2D rojoWalk = Resources.Load<Texture2D>("street-independiente-fans-walk-v1");
            Texture2D rojoRiot = Resources.Load<Texture2D>("street-independiente-riot-fans-v1");
            MethodInfo teamSource = View.GetMethod("TeamFanSourceRect", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo riotSource = View.GetMethod("TeamRiotFanFrameSource", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(teamSource); Assert.NotNull(riotSource);
            foreach (Texture2D[] atlases in new[] { new[] { ferroFront, ferroWalk, ferroRiot }, new[] { rojoFront, rojoWalk, rojoRiot } })
            for (int customerId = 0; customerId < 9; customerId++)
            {
                Rect front = (Rect)teamSource.Invoke(null, new object[] { atlases[0], customerId });
                Rect walking = (Rect)teamSource.Invoke(null, new object[] { atlases[1], customerId });
                Rect riotPose = (Rect)riotSource.Invoke(null, new object[] { atlases[2], customerId, 0 });
                Rect riotSwing = (Rect)riotSource.Invoke(null, new object[] { atlases[2], customerId, 1 });
                Assert.AreEqual(front.width, walking.width); Assert.AreEqual(front.height, walking.height);
                Assert.AreEqual(riotPose.y, riotSwing.y); Assert.AreEqual(riotPose.height, riotSwing.height);
                Assert.AreEqual(Mathf.RoundToInt(front.x / front.width), Mathf.RoundToInt(riotPose.x / riotPose.width) / 2,
                    "Normal, walking and riot renderers must choose the same apparel column for one customer.");
                Assert.AreEqual(Mathf.RoundToInt(front.y / front.height), Mathf.RoundToInt(riotPose.y / riotPose.height),
                    "Normal, walking and riot renderers must choose the same apparel row for one customer.");
            }
            Assert.AreEqual(9, (int)View.GetField("TeamFanVariantCount", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
        }
    }
}
