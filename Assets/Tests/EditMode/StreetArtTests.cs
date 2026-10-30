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
            Texture2D header = Resources.Load<Texture2D>("street-river-header-extension-v1");
            Assert.NotNull(header);
            Assert.AreEqual(2172, header.width); Assert.AreEqual(274, header.height);
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

        [TestCase("market-bread-common-v1", 1862, 845)]
        [TestCase("market-bread-burger-v1", 1728, 910)]
        public void MarketBreadArtKeepsFullSilhouetteWithTransparentMargins(string resource, int width, int height)
        {
            const string folder = "Assets/Resources/ModularKitchen/Art/";
            Texture2D art = Resources.Load<Texture2D>("ModularKitchen/Art/" + resource);
            Assert.NotNull(art);
            Assert.AreEqual(width, art.width); Assert.AreEqual(height, art.height);
            var importer = (TextureImporter)AssetImporter.GetAtPath(folder + resource + ".png");
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            var decoded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(decoded.LoadImage(System.IO.File.ReadAllBytes(folder + resource + ".png")));
                Color32[] pixels = decoded.GetPixels32();
                Assert.Greater(pixels.Count(p => p.a > 240), pixels.Length / 5, "A visible complete bread must be present.");
                for (int x = 0; x < width; x++)
                {
                    Assert.LessOrEqual(pixels[x].a, 10, "Bottom edge must not cut through bread.");
                    Assert.LessOrEqual(pixels[(height - 1) * width + x].a, 10, "Top edge must not cut through bread.");
                }
                for (int y = 0; y < height; y++)
                {
                    Assert.LessOrEqual(pixels[y * width].a, 10, "Left edge must not cut through bread.");
                    Assert.LessOrEqual(pixels[y * width + width - 1].a, 10, "Right edge must not cut through bread.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }

        [TestCase(96f)] [TestCase(104f)]
        public void MarketIngredientIconHasBalancedPaddingInsideRow(float faceHeight)
        {
            var row = new Rect(18f, 200f, 495f, faceHeight + 72f);
            Rect icon = (Rect)View.GetMethod("MarketIngredientIconBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { row, faceHeight });
            Assert.GreaterOrEqual(icon.yMin - row.y, 12f);
            Assert.AreEqual(icon.yMin - row.y, row.y + faceHeight - icon.yMax, .001f);
            Assert.GreaterOrEqual(icon.xMin, row.x + 12f);
            Assert.Less(icon.xMax, row.x + 100f, "Icon must not overlap ingredient name or purchase controls.");
        }

        [TestCase(1862f, 845f)] [TestCase(1728f, 910f)]
        [TestCase(32f, 64f)] [TestCase(64f, 64f)] [TestCase(128f, 32f)]
        public void MarketIngredientIconFitsWithoutCropOrDistortion(float width, float height)
        {
            var bounds = new Rect(32f, 42f, 78f, 62f);
            Rect fit = (Rect)View.GetMethod("FitMarketIngredientIcon", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bounds, new Vector2(width, height) });
            Assert.GreaterOrEqual(fit.xMin, bounds.xMin - .001f); Assert.LessOrEqual(fit.xMax, bounds.xMax + .001f);
            Assert.GreaterOrEqual(fit.yMin, bounds.yMin - .001f); Assert.LessOrEqual(fit.yMax, bounds.yMax + .001f);
            Assert.AreEqual(bounds.center, fit.center);
            Assert.AreEqual(width / height, fit.width / fit.height, .001f);
            Assert.IsTrue(Mathf.Abs(fit.width - bounds.width) < .001f || Mathf.Abs(fit.height - bounds.height) < .001f);
        }

        [TestCase(0f, 20f)] [TestCase(20f, 0f)]
        public void MarketIngredientIconRejectsEmptySource(float width, float height)
        {
            Assert.AreEqual(Rect.zero, View.GetMethod("FitMarketIngredientIcon", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Rect(0, 0, 78, 78), new Vector2(width, height) }));
        }

        [TestCase("BONDIOLA", 28, 144f, 27f)]
        [TestCase("$143.309,00", 38, 220f, 44f)]
        [TestCase("NECESITÁS", 13, 70f, 17f)]
        [TestCase("1234567890", 28, 80f, 30f)]
        public void MarketTextFontFitsBothItsWidthAndHeight(string value, int preferred, float width, float height)
        {
            Font font = Resources.Load<Font>("Menu/LuckiestGuy-Regular");
            Assert.IsNotNull(font);
            var style = new GUIStyle { font = font, fontSize = preferred, wordWrap = false };
            int fitted = (int)View.GetMethod("FitMarketTextFontSize", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { style, value, preferred, new Vector2(width, height) });
            Assert.GreaterOrEqual(fitted, 8, "Market labels must remain legible after fitting, not collapse to microscopic text.");
            style.fontSize = fitted;
            Vector2 measured = style.CalcSize(new GUIContent(value));
            Assert.LessOrEqual(measured.x, width - 2f, "Text must fit horizontally without wrapping or truncation.");
            Assert.LessOrEqual(measured.y, height - 2f, "Text must fit vertically within its row.");
        }

        [TestCase(4, "ModularKitchen/Art/meat-bondiola-whole-v1_Raw")]
        [TestCase(5, "ModularKitchen/Art/meat-vacio-whole-v1_Raw")]
        public void MarketUsesCurrentRawWholeMeatIllustration(int ingredientId, string resource)
        {
            string selected = (string)View.GetMethod("MarketFreshMeatResource", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { ingredientId });
            Assert.AreEqual(resource, selected, "The market row should use the latest prepared raw-meat illustration.");
            Texture2D art = Resources.Load<Texture2D>(selected);
            Assert.IsNotNull(art, "Current Mercado ingredient art must be present in Resources: " + selected);
            Rect fit = (Rect)View.GetMethod("FitMarketIngredientIcon", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Rect(0f, 0f, 78f, 78f), new Vector2(art.width, art.height) });
            Assert.AreEqual(new Vector2(39f, 39f), fit.center);
            Assert.LessOrEqual(fit.width, 78f);
            Assert.LessOrEqual(fit.height, 78f);
        }

        [TestCase(60f, 40f, 50f, 80f, true)]
        [TestCase(40f, 40f, 50f, 80f, false)]
        [TestCase(110f, 40f, 50f, 80f, false)]
        public void MarketOnlyDisplaysWholeRowsWithinViewport(float rowY, float rowHeight, float viewportY,
            float viewportHeight, bool expectedVisible)
        {
            bool visible = (bool)View.GetMethod("MarketFullyVisibleInViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Rect(0f, rowY, 100f, rowHeight), new Rect(0f, viewportY, 100f, viewportHeight) });
            Assert.AreEqual(expectedVisible, visible, "A partially clipped ingredient card must be hidden, never draw cut text/numbers.");
        }

        [TestCase("meat-chori/meat-chori_Raw")]
        [TestCase("sandwich-chori/sandwich-chori")]
        [TestCase("meat-paty/meat-paty_Raw")]
        [TestCase("sandwich-paty/sandwich-paty")]
        [TestCase("meat-bondiola/meat-bondiola_Raw")]
        [TestCase("meat-vacio/meat-vacio_Raw")]
        [TestCase("coca/coca")]
        [TestCase("beer/beer")]
        public void MarketIngredientSpriteSourceKeepsEntireSprite(string resource)
        {
            Sprite sprite = Resources.Load<Sprite>("ModularKitchen/Sprites/" + resource);
            Assert.NotNull(sprite);
            Rect source = (Rect)View.GetMethod("MarketIngredientSpriteSource", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { sprite });
            Vector2 scale = resource.StartsWith("meat-", StringComparison.Ordinal)
                ? new Vector2(sprite.texture.width / 2172f, sprite.texture.height / 724f) : Vector2.one;
            Assert.AreEqual(Vector2.Scale(sprite.rect.size, scale), source.size, "Keep the whole sprite after import scaling.");
            Assert.AreEqual(sprite.rect.x * scale.x, source.x);
            Assert.AreEqual(sprite.texture.height - sprite.rect.yMax * scale.y, source.y, .001f);
            Debug.Log(resource + " imported atlas " + sprite.texture.width + "x" + sprite.texture.height);
        }

        [TestCase(2172, 724)] [TestCase(2048, 683)]
        public void MarketMeatAtlasKeepsNormalizedRegionAfterImportResize(int width, int height)
        {
            var texture = new Texture2D(width, height);
            Sprite sprite = Sprite.Create(texture, new Rect(8, 221, 532, 293), Vector2.one * .5f);
            sprite.name = "meat-vacio_Raw";
            try
            {
                Rect source = (Rect)View.GetMethod("MarketIngredientSpriteSource", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { sprite });
                Assert.AreEqual(8f / 2172f, source.x / width, .00001f);
                Assert.AreEqual((724f - 221f - 293f) / 724f, source.y / height, .00001f);
                Assert.AreEqual(532f / 2172f, source.width / width, .00001f);
                Assert.AreEqual(293f / 724f, source.height / height, .00001f);
                Assert.Less(source.xMax / width, .25f, "Exclude the neighboring cooking-state cell.");
            }
            finally { UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
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
        [TestCase("COMPRAR\nFALTANTES", 142f, 46f)]
        [TestCase("$143.309,00", 180f, 46f)]
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
            Vector2 measured = style.CalcSize(new GUIContent(caption));
            Assert.LessOrEqual(measured.x, width - 24f * height / 92f + 1f);
            Assert.LessOrEqual(measured.y, height - 8f * height / 92f + 1f, "Button labels, including two-line captions, must not clip vertically.");
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

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        public void OnlyFirstTwoQueueRowsShowOrderBubbles(int visualRow, bool expected)
        {
            bool actual = (bool)View.GetMethod("ShouldDrawOrderBubble", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { visualRow });
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GlossyBlueGameplayFooterCreatesReusableTexture()
        {
            Texture2D footer = (Texture2D)View.GetMethod("CreateGameplayFooterTexture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
            try
            {
                Assert.NotNull(footer);
                Assert.AreEqual(256, footer.width);
                Assert.AreEqual(256, footer.height);
                Assert.AreEqual(TextureWrapMode.Clamp, footer.wrapMode);
                Assert.AreEqual(FilterMode.Bilinear, footer.filterMode);
                StringAssert.Contains("blue gameplay footer", footer.name);
            }
            finally { UnityEngine.Object.DestroyImmediate(footer); }
        }

        [Test]
        public void GlossyUpgradeCardsMatchTheCyanMedallionReferenceWithoutChangingBounds()
        {
            Rect card = new Rect(24f, 740f, 156f, 100f);
            Rect medallion = (Rect)View.GetMethod("UpgradeOvalBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { card });
            Assert.AreEqual(medallion.width, medallion.height, .01f, "The avatar plate is circular, not an oval.");
            Assert.GreaterOrEqual(medallion.width, 64f);
            Rect title = (Rect)View.GetMethod("UpgradeTitleBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { card });
            Rect status = (Rect)View.GetMethod("UpgradeStatusBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { card });
            Rect coin = (Rect)View.GetMethod("UpgradeCoinBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { card });
            Rect price = (Rect)View.GetMethod("UpgradePriceTextBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { card });
            Assert.GreaterOrEqual(title.xMin, medallion.xMax + 5f);
            Assert.GreaterOrEqual(status.yMin, title.yMin + title.height);
            Assert.GreaterOrEqual(coin.yMin, status.yMax + 5f);
            Assert.GreaterOrEqual(price.xMin, coin.xMax + 1f);
            foreach (Rect content in new[] { medallion, title, status, coin, price })
            {
                Assert.IsTrue(card.Contains(content.min));
                Assert.IsTrue(card.Contains(content.max - Vector2.one * .01f));
            }

            Texture2D texture = (Texture2D)View.GetMethod("CreateGlossyUpgradeCardTexture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 312, 200, true });
            try
            {
                Assert.AreEqual(312, texture.width); Assert.AreEqual(200, texture.height);
                Assert.AreEqual(0f, texture.GetPixel(0, 0).a, .01f, "Rounded glossy corners remain transparent.");
                Color face = texture.GetPixel(290, 100);
                Color medallionPixel = texture.GetPixel(80, 100);
                Assert.Greater(face.a, .9f); Assert.Greater(medallionPixel.a, .9f);
                Assert.Greater(medallionPixel.b, medallionPixel.r, "The medallion uses the deeper blue inset.");
                Assert.Greater(face.b, face.r, "The card face remains in the cyan/blue glossy family.");
                StringAssert.Contains("circular medallion", texture.name);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [TestCase(312, 200, 10, 3, 8, 6)]
        [TestCase(468, 240, 14, 4, 9, 7)]
        public void AffordableUpgradeBorderHasTwoWhiteRingsWithoutChangingCardFace(
            int cardWidth, int cardHeight, int padding, int outerX, int innerX, int gapX)
        {
            Texture2D texture = (Texture2D)View.GetMethod("CreateAffordableUpgradeBorderTexture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { cardWidth, cardHeight, true });
            try
            {
                Assert.AreEqual(cardWidth + padding * 2, texture.width);
                Assert.AreEqual(cardHeight + padding * 2, texture.height);
                Assert.That(texture.GetPixel(0, 0).a, Is.LessThan(.01f), "The extra rounded border canvas stays transparent at corners.");
                int y = padding + cardHeight / 2;
                Color outer = texture.GetPixel(outerX, y);
                Color inner = texture.GetPixel(innerX, y);
                Color gap = texture.GetPixel(gapX, y);
                Assert.Greater(outer.a, .9f, "The outer white ring is visible.");
                Assert.Greater(inner.a, .9f, "The second white ring is visible.");
                Assert.Less(gap.a, .05f, "The two rings remain visually distinct.");
                Assert.Greater(outer.r, .9f); Assert.Greater(outer.g, .9f); Assert.Greater(outer.b, .9f);
                Assert.Greater(inner.r, .9f); Assert.Greater(inner.g, .9f); Assert.Greater(inner.b, .9f);
                Assert.Less(texture.GetPixel(padding + cardWidth / 2, y).a, .01f,
                    "The transparent overlay leaves the card's contents unobstructed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void RoundedMarketPanelsHaveTransparentCornersAndSolidBlueFrame()
        {
            foreach (Vector2Int size in new[] { new Vector2Int(1032, 200), new Vector2Int(208, 136) })
            {
                var texture = (Texture2D)View.GetMethod("CreateRoundedMarketPanel", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { size.x, size.y, new Color(.8f,.9f,1f) });
                try {
                    Assert.That(texture.GetPixel(0,0).a, Is.LessThan(.01f));
                    Assert.That(texture.GetPixel(0,size.y-1).a, Is.LessThan(.01f));
                    Assert.That(texture.GetPixel(size.x/2,size.y/2).a, Is.GreaterThan(.99f));
                    StringAssert.Contains("Rounded", texture.name);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void AdaptivePennantsRemainTransparentOverGlossyBlueFooter()
        {
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            object theme = themes.GetMethod("ForLevel").Invoke(null, new object[] { 0 });
            Texture2D flags = (Texture2D)View.GetMethod("CreateFooterPennants", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { theme, 80f, true });
            try
            {
                Assert.NotNull(flags);
                Assert.AreEqual(0, flags.GetPixel(flags.width - 1, flags.height - 1).a,
                    "Keep pennant decoration transparent outside its fabric over the blue footer.");
                Assert.IsTrue(flags.GetPixels32().Any(pixel => pixel.a > 0),
                    "The existing pennants themselves must still render.");
            }
            finally { UnityEngine.Object.DestroyImmediate(flags); }
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
            Assert.AreEqual(bubble.center.x, more.center.x);
        }

        [TestCase(0, "")]
        [TestCase(1, "•••")]
        [TestCase(2, "•••")]
        [TestCase(3, "•••")]
        public void HiddenOrderIndicatorShowsDotsOnlyWhenProductsAreHidden(int hiddenLines, string expected)
        {
            string actual = (string)View.GetMethod("HiddenOrderIndicatorText", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { hiddenLines });
            Assert.AreEqual(expected, actual);
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
                Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
            Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
                Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
                Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
                Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format, "Android uses the established ETC2 override; source textures remain uncompressed.");
            }

            float duration = (float)View.GetField("IntroDuration", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.GreaterOrEqual(duration, 3.9f);
            Assert.LessOrEqual(duration, 4.1f);
            Rect progress = (Rect)View.GetMethod("IntroProgressBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Assert.LessOrEqual(progress.width, 108f);
            Assert.AreEqual(95f, progress.width, .001f);
            Assert.AreEqual(28f, progress.height, .001f);
            Assert.AreEqual(882f, progress.y, .001f);
            MethodInfo progressValue = View.GetMethod("IntroProgressValue", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(progressValue);
            Assert.AreEqual(0f, (float)progressValue.Invoke(null, new object[] { 0f }), .001f);
            Assert.AreEqual(.5f, (float)progressValue.Invoke(null, new object[] { 2f }), .001f);
            Assert.AreEqual(1f, (float)progressValue.Invoke(null, new object[] { 4f }), .001f);
            MethodInfo pop = View.GetMethod("IntroLogoPop", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(pop);
            Assert.AreEqual(0f, (float)pop.Invoke(null, new object[] { 0f }), .001f);
            Assert.Greater((float)pop.Invoke(null, new object[] { .22f }), 1.2f);
            Assert.AreEqual(1f, (float)pop.Invoke(null, new object[] { .53f }), .001f);
            Rect logoBounds = (Rect)View.GetMethod("IntroLogoBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 1f });
            Assert.AreEqual(new Rect(135f, 320f, 270f, 260f), logoBounds);
            MethodInfo status = View.GetMethod("IntroStatus", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.AreEqual("PRENDIENDO LAS BRASAS...", (string)status.Invoke(null, new object[] { 0f }));
            Assert.AreEqual("PRENDIENDO LAS BRASAS...", (string)status.Invoke(null, new object[] { 1.349f }));
            Assert.AreEqual("LA HINCHADA ESTÁ LLEGANDO...", (string)status.Invoke(null, new object[] { 1.35f }));
            Assert.AreEqual("LA HINCHADA ESTÁ LLEGANDO...", (string)status.Invoke(null, new object[] { 2.699f }));
            Assert.AreEqual("¡QUE EMPIECE LA FIESTA EN EL BARRIO!", (string)status.Invoke(null, new object[] { 2.7f }));
            Assert.AreEqual("¡QUE EMPIECE LA FIESTA EN EL BARRIO!", (string)status.Invoke(null, new object[] { 3.999f }));
            Font statusFont = Resources.Load<Font>("Google/GoogleSans-Medium");
            Assert.NotNull(statusFont);
            int statusFontSize = (int)View.GetField("IntroStatusFontSize", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Assert.AreEqual(14, statusFontSize);
            var statusStyle = new GUIStyle { font = statusFont, fontSize = statusFontSize, fontStyle = FontStyle.Bold, wordWrap = false };
            Assert.LessOrEqual(statusStyle.CalcSize(new GUIContent("¡QUE EMPIECE LA FIESTA EN EL BARRIO!")).x, 524f);
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
            MethodInfo backdropBounds = View.GetMethod("IntroBackdropBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(backdropBounds);
            Rect animated = (Rect)backdropBounds.Invoke(null, new object[] { new Vector2(width, height), 1.028f });
            Assert.LessOrEqual(animated.xMin, 0f);
            Assert.LessOrEqual(animated.yMin, 0f);
            Assert.GreaterOrEqual(animated.xMax, width);
            Assert.GreaterOrEqual(animated.yMax, height);
        }

        [TestCase(320, 568)]
        [TestCase(360, 640)]
        [TestCase(360, 800)]
        [TestCase(375, 812)]
        [TestCase(390, 844)]
        [TestCase(414, 896)]
        [TestCase(540, 960)]
        [TestCase(575, 1280)]
        [TestCase(720, 1280)]
        [TestCase(720, 1600)]
        [TestCase(1080, 1920)]
        [TestCase(1080, 2160)]
        [TestCase(1080, 2340)]
        [TestCase(1080, 2400)]
        [TestCase(1080, 2560)]
        [TestCase(1200, 2670)]
        [TestCase(1440, 2960)]
        [TestCase(1536, 2048)]
        [TestCase(2048, 2732)]
        public void HubArtworkFillsPortraitScreensAndHotspotsStayVisible(int width, int height)
        {
            const float artWidth = 841f, artHeight = 1870f;
            Vector2 screen = new Vector2(width, height);
            Rect safe = new Rect(24, 41, width - 48, height - 117); // asymmetric bottom/top insets
            MethodInfo map = View.GetMethod("HubArtworkRectToLayout", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo artworkBounds = View.GetMethod("HubArtworkScreenBounds", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo canvasViewport = View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic);
            Rect viewport = (Rect)canvasViewport.Invoke(null, new object[] { screen, safe });
            Rect artwork = (Rect)artworkBounds.Invoke(null, new object[] { new Vector2(artWidth, artHeight), screen });
            MethodInfo settingsBounds = View.GetMethod("HubSettingsButtonScreenBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect settings = (Rect)settingsBounds.Invoke(null, new object[] { screen, safe });
            MethodInfo topMusicBounds = View.GetMethod("HubTopMusicButtonScreenBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect topMusic = (Rect)topMusicBounds.Invoke(null, new object[] { screen, safe });
            Rect topMusicLayout = (Rect)View.GetMethod("HubTopMusicButtonLayoutBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { screen, safe });
            Rect account = (Rect)View.GetMethod("HubAccountButtonScreenBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { screen });
            Rect settingsPanel = (Rect)View.GetField("HubSettingsPanelBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect musicControl = (Rect)View.GetField("HubSettingsMusicButtonBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect backControl = (Rect)View.GetField("HubSettingsBackButtonBounds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            Assert.GreaterOrEqual(settings.xMin, viewport.xMin - .02f);
            Assert.LessOrEqual(settings.xMax, viewport.xMax + .02f);
            Assert.GreaterOrEqual(settings.yMin, viewport.yMin - .02f);
            Assert.LessOrEqual(settings.yMax, viewport.yMax + .02f);
            Assert.GreaterOrEqual(settings.width, 44f, "Settings must keep a touch target wider than 44 physical pixels.");
            Assert.GreaterOrEqual(settings.height, 44f, "Settings must keep a touch target taller than 44 physical pixels.");
            Assert.IsFalse(settings.Overlaps(account),
                "Settings must not overlap the existing CUENTA control.");
            Assert.GreaterOrEqual(topMusic.xMin, viewport.xMin - .02f);
            Assert.LessOrEqual(topMusic.xMax, viewport.xMax + .02f);
            Assert.GreaterOrEqual(topMusic.yMin, viewport.yMin - .02f);
            Assert.LessOrEqual(topMusic.yMax, viewport.yMax + .02f);
            Assert.GreaterOrEqual(topMusic.width, 44f, "Top music must keep a 44px touch target at " + width + "x" + height + ".");
            Assert.GreaterOrEqual(topMusic.height, 44f, "Top music must keep a 44px touch target at " + width + "x" + height + ".");
            Assert.LessOrEqual(topMusic.xMax, account.xMin + .02f,
                "The sound control must sit left of CUENTA at " + width + "x" + height + ".");
            Assert.IsFalse(topMusic.Overlaps(account), "Top music must not cover CUENTA at " + width + "x" + height + ".");
            Assert.IsFalse(topMusic.Overlaps(settings), "The two top icon controls must not overlap.");
            Assert.AreEqual(topMusic.xMin, viewport.xMin + topMusicLayout.xMin * viewport.width / 540f, .02f);
            Assert.AreEqual(topMusic.xMax, viewport.xMin + topMusicLayout.xMax * viewport.width / 540f, .02f);
            Assert.AreEqual(topMusic.yMin, viewport.yMin + topMusicLayout.yMin * viewport.height / 960f, .02f);
            Assert.AreEqual(topMusic.yMax, viewport.yMin + topMusicLayout.yMax * viewport.height / 960f, .02f);
            Assert.GreaterOrEqual(settingsPanel.xMin, 0f); Assert.LessOrEqual(settingsPanel.xMax, 540f);
            Assert.GreaterOrEqual(settingsPanel.yMin, 0f); Assert.LessOrEqual(settingsPanel.yMax, 960f);
            Assert.GreaterOrEqual(musicControl.yMin, settingsPanel.yMin);
            Assert.LessOrEqual(musicControl.yMax, settingsPanel.yMax);
            Assert.GreaterOrEqual(backControl.yMin, settingsPanel.yMin);
            Assert.LessOrEqual(backControl.yMax, settingsPanel.yMax);
            Assert.GreaterOrEqual(musicControl.height * viewport.height / 960f, 44f,
                "Music control must keep a 44px target at " + width + "x" + height + ".");
            Assert.GreaterOrEqual(backControl.height * viewport.height / 960f, 44f,
                "Back control must keep a 44px target at " + width + "x" + height + ".");

            // The same image covers the full physical screen with aspect-preserving cover crop.
            Assert.LessOrEqual(artwork.xMin, .02f);
            Assert.GreaterOrEqual(artwork.xMax, width - .02f);
            Assert.LessOrEqual(artwork.yMin, .02f);
            Assert.GreaterOrEqual(artwork.yMax, height - .02f);
            Assert.AreEqual(artWidth / artHeight, artwork.width / artwork.height, .001f);

            int accountAction = (int)View.GetField("HubAccountAction", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            Rect accountSource = (Rect)View.GetMethod("HubArtActionSourceRect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { accountAction });
            Assert.AreEqual(Rect.zero, accountSource, "The hub no longer has an independent account button or hotspot.");

            foreach (Rect source in new[] { new Rect(467, 448, 288, 143),
                new Rect(100, 859, 315, 155), new Rect(94, 1238, 317, 164), new Rect(479, 1236, 344, 168) })
            {
                Rect hit = (Rect)map.Invoke(null, new object[] { source, new Vector2(artWidth, artHeight), screen, safe });
                Assert.Greater(hit.width, 0f);
                Assert.Greater(hit.height, 0f);
                Assert.GreaterOrEqual(hit.xMin, 0f); Assert.LessOrEqual(hit.xMax, 540f);
                Assert.GreaterOrEqual(hit.yMin, 0f); Assert.LessOrEqual(hit.yMax, 960f);

                // The painted action can meet a notch/edge inset; only its safe-area intersection
                // is tappable, while still retaining a non-empty target at every tested size.
                float artScale = artwork.width / artWidth;
                Rect artworkOnScreen = new Rect(artwork.x + source.x * artScale, artwork.y + source.y * artScale,
                    source.width * artScale, source.height * artScale);
                Assert.IsFalse(settings.Overlaps(artworkOnScreen),
                    "Settings must not cover a hub hotspot at " + width + "x" + height + ".");
                Assert.IsFalse(topMusic.Overlaps(artworkOnScreen),
                    "Top music must not cover a hub hotspot at " + width + "x" + height + ".");
                Rect tappableOnScreen = Rect.MinMaxRect(
                    Mathf.Max(artworkOnScreen.xMin, viewport.xMin),
                    Mathf.Max(artworkOnScreen.yMin, viewport.yMin),
                    Mathf.Min(artworkOnScreen.xMax, viewport.xMax),
                    Mathf.Min(artworkOnScreen.yMax, viewport.yMax));
                Assert.Greater(tappableOnScreen.width, 0f);
                Assert.Greater(tappableOnScreen.height, 0f);
                Assert.AreEqual(tappableOnScreen.xMin, viewport.xMin + hit.xMin * viewport.width / 540f, .02f);
                Assert.AreEqual(tappableOnScreen.xMax, viewport.xMin + hit.xMax * viewport.width / 540f, .02f);
                Assert.AreEqual(tappableOnScreen.yMin, viewport.yMin + hit.yMin * viewport.height / 960f, .02f);
                Assert.AreEqual(tappableOnScreen.yMax, viewport.yMin + hit.yMax * viewport.height / 960f, .02f);
            }
        }

        [TestCase("street-background-open-street-v4", 940, 1673, false)]
        [TestCase("street-hub-stadium-v1", 841, 1870, false)]
        [TestCase("street-victory-popup-wood-v1", 1122, 1402, true)]
        [TestCase("street-mural-real-v5", 940, 1673, false)]
        [TestCase("street-coin-gold-v2", 1254, 1254, true)]
        [TestCase("street-allboys-crest", 320, 320, true)]
        [TestCase("street-independiente-crest-v1", 960, 997, true)]
        [TestCase("street-ferro-crest-v1", 256, 256, true)]
        [TestCase("street-san-lorenzo-crest-v1", 1500, 1500, true)]
        [TestCase("street-river-crest-v1", 1200, 1487, true)]
        [TestCase("street-boca-crest-v1", 400, 400, true)]
        [TestCase("street-camioneros-crest-v1", 713, 485, false)]
        [TestCase("street-los-redondos-crest-v1", 300, 300, false)]
        public void NewStreetAssetsAndGoldCoinKeepNativeResolution(string name, int width, int height, bool alpha)
        {
            Texture2D texture = Resources.Load<Texture2D>(name); Assert.NotNull(texture);
            Assert.AreEqual(width, texture.width); Assert.AreEqual(height, texture.height);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + name + ".png");
            Assert.NotNull(importer); Assert.AreEqual(alpha, importer.DoesSourceTextureHaveAlpha());
            Assert.IsFalse(importer.mipmapEnabled); Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden);
            TextureImporterFormat expectedAndroidFormat = Array.IndexOf(new[] {
                "street-background-open-street-v4", "street-hub-stadium-v1", "street-victory-popup-wood-v1",
                "street-mural-real-v5", "street-coin-gold-v2", "street-allboys-crest"
            }, name) >= 0 ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.RGBA32;
            Assert.AreEqual(expectedAndroidFormat, android.format);
            Assert.GreaterOrEqual(android.maxTextureSize, Math.Max(width, height));
        }

        [Test]
        public void LevelCrestManifestMatchesAllLevelsAndLoadsUserSuppliedCrests()
        {
            string[] resources = (string[])View.GetField("LevelCrestResourceNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Type simulation = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            string[] levels = (string[])simulation.GetField("LevelDisplayNames").GetValue(null);
            string[] expected = {
                "street-allboys-crest", "street-new-chicago-crest-v1", "street-velez-crest-v1",
                "street-ferro-crest-v1", "street-independiente-crest-v1", "street-racing-crest-v1",
                "street-san-lorenzo-crest-v1", "street-river-crest-v1", "street-boca-crest-v1",
                "street-camioneros-crest-v1", "street-los-redondos-crest-v1"
            };
            Assert.AreEqual(levels.Length, resources.Length, "The crest manifest must stay aligned with the level catalog");
            Assert.AreEqual(expected.Length, resources.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], resources[i], "Crest order must match the level catalog at index " + i);
                Assert.NotNull(Resources.Load<Texture2D>(resources[i]), "Crest resource must resolve: " + resources[i]);
            }
            Assert.AreEqual("Ferro / Caballito", levels[3]);
            Assert.AreEqual("Independiente / Avellaneda", levels[4]);
            Assert.AreEqual("Racing Club / Avellaneda", levels[5]);
        }

        [Test]
        public void RacingClubCrestCutoutPreservesWhiteDetailsAndNativeArtworkBounds()
        {
            const string resource = "street-racing-crest-v1";
            const string path = "Assets/Art/Street/Resources/street-racing-crest-v1.png";
            Texture2D crest = Resources.Load<Texture2D>(resource);
            Assert.NotNull(crest);
            Assert.AreEqual(310, crest.width); Assert.AreEqual(382, crest.height);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.NotNull(importer);
            Assert.IsFalse(importer.DoesSourceTextureHaveAlpha(), "The supplied clipboard PNG has the checkerboard baked in");
            Assert.IsTrue(importer.isReadable, "Runtime cutout needs readable source pixels");
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterFormat.RGBA32, importer.GetPlatformTextureSettings("Android").format);

            var createCutout = View.GetMethod("CreateLevelCrestCutout", BindingFlags.Static | BindingFlags.NonPublic);
            Texture2D cutout = (Texture2D)createCutout.Invoke(null, new object[] { crest });
            Assert.NotNull(cutout);
            try
            {
                Assert.AreEqual(0f, cutout.GetPixel(0, 0).a, .01f, "Exterior checkerboard should become transparent");
                Color stripe = cutout.GetPixel(87, 230);
                Assert.AreEqual(1f, stripe.a, .01f, "White stripe inside the blue shield must stay opaque");
                Assert.Greater(stripe.r, .9f, "White stripe color must be preserved");
                Assert.Greater(cutout.GetPixel(123, 190).b, .6f, "Blue field should remain intact");
            }
            finally { UnityEngine.Object.DestroyImmediate(cutout); }
        }

        [TestCase("street-camioneros-crest-v1")]
        [TestCase("street-los-redondos-crest-v1")]
        public void OpaqueUserCrestCutoutsRemoveOnlyExteriorLightBackground(string resource)
        {
            Texture2D source = Resources.Load<Texture2D>(resource);
            Assert.NotNull(source);
            Texture2D cutout = (Texture2D)View.GetMethod("CreateLevelCrestCutout", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { source });
            Assert.NotNull(cutout);
            try
            {
                Assert.AreEqual(0f, cutout.GetPixel(0, 0).a, .01f, "Exterior white background should become transparent");
                int opaquePixels = 0;
                foreach (Color pixel in cutout.GetPixels()) if (pixel.a > .95f) opaquePixels++;
                Assert.Greater(opaquePixels, source.width * source.height / 5, "The crest artwork must remain visible");
            }
            finally { UnityEngine.Object.DestroyImmediate(cutout); }
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
                float extra = Mathf.Max(0f, (r.height - bounds.height) * .5f);
                Assert.GreaterOrEqual(r.yMin, bounds.yMin - extra); Assert.LessOrEqual(r.yMax, bounds.yMax + extra);
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

        [TestCase(20f)]
        [TestCase(22f)]
        [TestCase(26f)]
        public void LevelBadgeCrestsUseAllBoysReferenceSizeAcrossFooterHeights(float badgeHeight)
        {
            Rect bounds = new Rect(45f, 100f, 450f, badgeHeight);
            Rect[] layout = (Rect[])View.GetMethod("LevelBadgeLayout", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bounds, 240f, true });
            Assert.AreEqual(32f, layout[0].width, .01f);
            Assert.AreEqual(32f, layout[0].height, .01f);
            Assert.AreEqual(bounds.center.y, layout[0].center.y, .01f);
            Assert.IsFalse(layout[0].Overlaps(layout[1]));
        }

        [Test]
        public void SanLorenzoReplacementKeepsSuppliedAlphaResolutionAndGuid()
        {
            const string path = "Assets/Art/Street/Resources/street-san-lorenzo-crest-v1.png";
            var texture = Resources.Load<Texture2D>("street-san-lorenzo-crest-v1");
            Assert.AreEqual(1500, texture.width); Assert.AreEqual(1500, texture.height);
            Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath(path)).DoesSourceTextureHaveAlpha());
            Assert.AreEqual("fb885176735e4c06b11b5a4bb4fd6612", AssetDatabase.AssetPathToGUID(path));
        }

        [Test]
        public void CrestVisibleUvIgnoresTransparentPaddingWithoutDistortingArt()
        {
            var texture = new Texture2D(10, 10, TextureFormat.RGBA32, false);
            var pixels = new Color32[100];
            for (int y = 2; y < 8; y++)
            for (int x = 3; x < 7; x++) pixels[y * 10 + x] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels); texture.Apply();
            try
            {
                Rect uv = (Rect)View.GetMethod("LevelCrestVisibleUv", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { texture });
                Assert.That(uv.x, Is.EqualTo(.3f).Within(.001f));
                Assert.That(uv.y, Is.EqualTo(.2f).Within(.001f));
                Assert.That(uv.width, Is.EqualTo(.4f).Within(.001f));
                Assert.That(uv.height, Is.EqualTo(.6f).Within(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void AllElevenCrestsHaveEqualVisibleBodyHeightWithoutDistortion()
        {
            string[] resources = (string[])View.GetField("LevelCrestResourceNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            MethodInfo measure = View.GetMethod("LevelCrestVisibleUv", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo fit = View.GetMethod("FitLevelCrest", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < resources.Length; i++)
            {
                Texture2D source = Resources.Load<Texture2D>(resources[i]);
                Texture2D cutout = null;
                try
                {
                    if (i == 5 || i == 9 || i == 10)
                        cutout = (Texture2D)View.GetMethod("CreateLevelCrestCutout", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { source });
                    Texture2D texture = cutout != null ? cutout : source;
                    Rect uv = (Rect)measure.Invoke(null, new object[] { texture });
                    Assert.Greater(uv.width, 0); Assert.Greater(uv.height, 0);
                    float aspect = texture.width * uv.width / (texture.height * uv.height);
                    Rect body = (Rect)View.GetMethod("LevelCrestBodyUv", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { i, uv });
                    foreach (Rect slot in new[] { new Rect(0,0,32,32), new Rect(0,0,30,34), new Rect(0,0,76,76) })
                    {
                        Rect visible = (Rect)fit.Invoke(null, new object[] { slot, new Vector2(texture.width, texture.height), uv, body });
                        Assert.That(visible.width / visible.height, Is.EqualTo(aspect).Within(.001f), resources[i]);
                        float scale = visible.height / (texture.height * uv.height);
                        Assert.That(texture.height * body.height * scale, Is.EqualTo(slot.height).Within(.001f), resources[i]);
                        float bodyTop = visible.y + texture.height * (uv.yMax - body.yMax) * scale;
                        Assert.That(bodyTop, Is.EqualTo(slot.yMin).Within(.001f), resources[i]);
                        Assert.That(visible.center.x, Is.EqualTo(slot.center.x).Within(.001f), resources[i]);
                        if (i == 2) Assert.Greater(visible.height, slot.height, "Vélez star must not shrink the shield body");
                        else Assert.That(visible.height, Is.EqualTo(slot.height).Within(.001f), resources[i]);
                    }
                }
                finally { if (cutout != null) UnityEngine.Object.DestroyImmediate(cutout); }
            }
        }

        [TestCase(23f)]
        [TestCase(32f)]
        [TestCase(48f)]
        public void UniformBodyHeightBadgeReservesActualCrestWidth(float crestWidth)
        {
            Rect bounds = new Rect(45, 100, 450, 26);
            Rect[] layout = (Rect[])View.GetMethod("LevelBadgeLayoutForWidth", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bounds, 600f, crestWidth });
            Assert.That(layout[0].height, Is.EqualTo(32f));
            Assert.That(layout[0].width, Is.EqualTo(crestWidth));
            Assert.That(layout[1].xMin - layout[0].xMax, Is.EqualTo(8f).Within(.001f));
            Assert.That(layout[0].xMin, Is.GreaterThanOrEqualTo(bounds.xMin - .001f));
            Assert.That(layout[1].xMax, Is.LessThanOrEqualTo(bounds.xMax + .001f));
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
                Assert.GreaterOrEqual(cards[i].yMin, 0f);
                for (int j = i / 2 * 2; j < i; j++)
                    Assert.IsFalse(cards[i].Overlaps(cards[j]), "Cards in the same grid row must have separate touch targets");
            }

            Rect viewport = (Rect)View.GetField("LevelSelectorViewport", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect back = (Rect)View.GetField("LevelSelectBack", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.LessOrEqual(viewport.yMax, back.yMin - 8f,
                "Scrollable level cards must stop before the fixed VOLVER button");
            Assert.That(cards[0].width, Is.EqualTo(244f));
            Assert.That(cards[0].height, Is.EqualTo(184f));
            Assert.That(cards[1].xMin - cards[0].xMax, Is.EqualTo(12f));
            Assert.That(cards[2].yMin - cards[0].yMax, Is.EqualTo(12f));
            Assert.Less(cards[6].yMin, viewport.yMax, "A partial fourth row makes the scroll discoverable");
            MethodInfo maxScroll = View.GetMethod("LevelSelectorMaxScroll", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.Greater((float)maxScroll.Invoke(null, new object[] { 960f }), 0f,
                "All eleven levels exceed the viewport and must be reachable by scrolling");
            Assert.Greater(cards[10].yMax, 960f, "Campaign tail belongs below the initial viewport");

            Assert.AreEqual("street-mural-real-v5", View.GetField("MuralResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.AreEqual("street-background-chicago-v1", View.GetField("ChicagoBackgroundResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Assert.NotNull(Resources.Load<Texture2D>("street-mural-real-v5"), "Floresta card uses the All Boys mural");
            Assert.NotNull(Resources.Load<Texture2D>("street-allboys-crest"));
            Assert.NotNull(Resources.Load<Texture2D>("street-background-chicago-v1"), "Nueva Chicago card uses its own mural scene");
            Assert.NotNull(Resources.Load<Texture2D>("street-new-chicago-crest-v1"));
        }

        [TestCase(960f)]
        [TestCase(1200f)]
        [TestCase(1356f)]
        public void SelectorTallViewportExtendsWithoutStretchingCards(float canvasHeight)
        {
            Rect viewport = (Rect)View.GetMethod("LevelSelectorViewportBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight });
            Rect back = (Rect)View.GetMethod("LevelSelectorBackBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight });
            Assert.That(viewport.yMax, Is.EqualTo(canvasHeight - 100f));
            Assert.That(back.yMin - viewport.yMax, Is.EqualTo(10f));
            Assert.That(back.yMax, Is.LessThan(canvasHeight));
            Rect card = (Rect)View.GetMethod("LevelSelectCardBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 10 });
            float scroll = (float)View.GetMethod("LevelSelectorMaxScroll", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight });
            Assert.That(card.yMax - scroll, Is.LessThanOrEqualTo(viewport.yMax + .01f));
            Assert.That(card.size, Is.EqualTo(new Vector2(244f, 184f)), "Faces and text retain width-based proportions");
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
        public void SpecialistUpgradePortraitUsesChicagoStyleAtEveryLevel()
        {
            MethodInfo sourceLevel = View.GetMethod("SpecialistUpgradePortraitSourceLevel", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo matchedStyle = View.GetMethod("UsesMatchedCocacoleroArt", BindingFlags.Static | BindingFlags.NonPublic);
            string[] levels = (string[])Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true)
                .GetField("LevelNames").GetValue(null);
            for (int level = 0; level < levels.Length; level++)
                Assert.AreEqual(1, sourceLevel.Invoke(null, new object[] { level }), "Upgrade card art should use the same matched atlas at every level");
            Assert.IsTrue((bool)matchedStyle.Invoke(null, new object[] { 1 }), "The shared card portrait style must use the Chicago matched atlas");
            Assert.AreEqual(0, sourceLevel.Invoke(null, new object[] { -1 }), "Invalid levels retain the legacy fallback");
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
            Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, importer.GetPlatformTextureSettings("Android").format);
        }

        [Test]
        public void CocaBarrelReachSpriteImportsAsTransparentWorkerArt()
        {
            const string resource = "street-cocacolero-barrel-reach-v1";
            const string path = "Assets/Art/Street/Resources/street-cocacolero-barrel-reach-v1.png";
            Texture2D pose = Resources.Load<Texture2D>(resource);
            Assert.NotNull(pose);
            Assert.AreEqual(1247, pose.width); Assert.AreEqual(1261, pose.height);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.NotNull(importer);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
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
            Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, chicagoGrillImporter.GetPlatformTextureSettings("Android").format);
            foreach (string name in new[] { "street-new-chicago-crest-v1", "street-coca-bottle-v1", "street-beverage-barrel-v1" })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + name + ".png");
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, importer.GetPlatformTextureSettings("Android").format);
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
            Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
            Rect viewport = (Rect)View.GetMethod("LevelSelectorViewportBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 960f });
            Assert.Less(drawnLastCardBottom, viewport.yMax * verticalScale,
                "The first three selector rows remain inside the scroll viewport.");

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
            MethodInfo adaptiveCreate = renderer.GetMethod("CreateAdaptiveTexture", BindingFlags.Static | BindingFlags.Public);
            Texture2D expanded = (Texture2D)adaptiveCreate.Invoke(null, new object[] { theme, 120f, true, true });
            try
            {
                Assert.AreEqual(255, opaque.GetPixel(opaque.width - 1, opaque.height - 1).a * 255f, .01f);
                Assert.AreEqual(0, transparent.GetPixel(transparent.width - 1, transparent.height - 1).a, .01f);
                Assert.AreEqual(240, expanded.height, "Adaptive pennant geometry is authored at the footer's logical height.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(opaque);
                UnityEngine.Object.DestroyImmediate(transparent);
                UnityEngine.Object.DestroyImmediate(expanded);
            }
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
            foreach (string name in new[] { "HudCoins", "HudTime" })
            {
                Rect field = (Rect)View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.IsTrue(bar.Contains(field.min)); Assert.IsTrue(bar.Contains(field.max));
            }
            Rect sales = (Rect)View.GetField("HudSales", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.GreaterOrEqual(sales.yMin, bar.yMax, "Product counters overlay the mural below the coins/time strip.");
            Assert.IsFalse(bar.Contains(sales.min));
            Rect punchHoleViewport = (Rect)viewportMethod.Invoke(null, new object[] {
                new Vector2(1440, 3088), new Rect(0, 0, 1440, 2999) });
            Assert.AreEqual(2999f, punchHoleViewport.height);
            foreach (string name in new[] { "CutoutHudCoins", "CutoutHudTime", "CutoutHudSales" })
                Assert.IsNull(View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic), name + " must not draw an alternate HUD");
        }

        [Test]
        public void TopCameraCutoutGetsAnActualHudGapAndQuotaColumnsStayBelowTheStatusStrip()
        {
            MethodInfo gapMethod = View.GetMethod("TopHudCutoutBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect gap = (Rect)gapMethod.Invoke(null, new object[] {
                new Vector2(1080, 2340), new[] { new Rect(480, 2260, 120, 80) } });
            Assert.AreEqual(232f, gap.xMin, .01f);
            Assert.AreEqual(308f, gap.xMax, .01f);
            Assert.AreEqual(34f, gap.height);

            int[] products = { 0, 1, 2, 3, 4, 6, 5 };
            MethodInfo cellsMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int[]), typeof(int), typeof(Rect) }, null);
            MethodInfo barHeightMethod = View.GetMethod("HudBarHeightFor", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Rect) }, null);
            Rect[] cells = new Rect[7];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = (Rect)cellsMethod.Invoke(null, new object[] { i, products, products.Length, gap });
                Assert.IsFalse(cells[i].Overlaps(gap), "Quota chip " + i + " crosses the hardware camera channel");
                Assert.GreaterOrEqual(cells[i].yMin, (float)barHeightMethod.Invoke(null, new object[] { gap }));
                int group = i < 4 ? 0 : 1;
                int groupIndex = i < 4 ? i : i - 4;
                float expectedX = group == 0
                    ? 8f + groupIndex % 2 * 117f
                    : 310f + groupIndex % 2 * 117f;
                Assert.AreEqual(expectedX, cells[i].xMin, .01f,
                    "Each two-column category group stays on its side of the screen.");
                for (int j = 0; j < i; j++) Assert.IsFalse(cells[i].Overlaps(cells[j]));
            }
            Rect[] statusCells = new Rect[2];
            MethodInfo statusMethod = View.GetMethod("HudStatusCellBounds", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < statusCells.Length; i++)
            {
                statusCells[i] = (Rect)statusMethod.Invoke(null, new object[] { i, gap });
                Assert.IsFalse(statusCells[i].Overlaps(gap), "Status card " + i + " crosses the hardware camera channel");
                for (int j = 0; j < i; j++) Assert.IsFalse(statusCells[i].Overlaps(statusCells[j]));
            }
            Assert.AreEqual(1.2f, statusCells[0].yMin, .01f, "Coins remain at the top edge.");
            Assert.AreEqual(1.2f, statusCells[1].yMin, .01f, "Both cards stay level while shrinking around the notch.");
            Assert.Less(statusCells[0].width,
                ((Rect)statusMethod.Invoke(null, new object[] { 0, Rect.zero })).width,
                "A centered cutout reduces both equal card widths.");
            Assert.Greater(statusCells[1].xMin, gap.xMax);
            Assert.IsNull(View.GetField("HudLevel", BindingFlags.Static | BindingFlags.NonPublic), "The level badge is removed.");
            float cutoutHeight = (float)barHeightMethod.Invoke(null, new object[] { gap });
            Assert.AreEqual(39.36f, cutoutHeight, .01f, "A centered camera island does not expand the status strip when both cards fit beside it.");
            Assert.Less(cutoutHeight, 81.6f, "Quota cards no longer inflate the full-width header.");
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

        [TestCase("Playing", 5, true)]
        [TestCase("Ready", 2, true)]
        [TestCase("Ready", 3, true)]
        [TestCase("Ready", 4, false)]
        [TestCase("Won", 5, false)]
        [TestCase("Lost", 5, false)]
        public void UpgradeCardsAreDrawnOnlyWhilePlayingOrInCompactReady(string phase, int catalogActionCount, bool expected)
        {
            Type phaseType = Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp", true);
            object phaseValue = Enum.Parse(phaseType, phase);
            MethodInfo shouldDraw = View.GetMethod("ShouldDrawUpgradeCards", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(shouldDraw);
            Assert.AreEqual(expected, shouldDraw.Invoke(null, new[] { phaseValue, (object)catalogActionCount }));
        }

        [TestCase(320, 568)]
        [TestCase(360, 640)]
        [TestCase(360, 800)]
        [TestCase(393, 852)]
        [TestCase(412, 915)]
        [TestCase(720, 1600)]
        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(1170, 2532)]
        [TestCase(1290, 2796)]
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
            string[] levelNames = (string[])simulationType.GetField("LevelNames").GetValue(null);
            ConstructorInfo simulationConstructor = simulationType.GetConstructor(new[] {
                balanceType, typeof(int), typeof(float), typeof(int), typeof(int), typeof(int)
            });
            MethodInfo build = View.GetMethod("BuildResponsiveFooterLayout", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo scaleCard = View.GetMethod("ScaleCardBounds", BindingFlags.Static | BindingFlags.NonPublic);
            for (int level = 0; level < levelNames.Length; level++)
            {
                object sim = simulationConstructor.Invoke(new object[] { Activator.CreateInstance(balanceType), level, 5f, 0, 1, 0 });
                object kitchen = simulationType.GetProperty("KitchenLayout").GetValue(sim);
                float stageBottom = (float)kitchenLayout.GetProperty("PlayfieldBottom").GetValue(kitchen) * verticalScale;
                object footer = build.Invoke(null, new object[] { sim, canvasHeight, true, false });
                Type footerType = footer.GetType();
                Rect[] raw = (Rect[])footerType.GetField("UpgradeBounds").GetValue(footer);
                int[] actions = (int[])View.GetMethod("CatalogUpgradeActions", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { sim });
                Assert.AreEqual(actions.Length, raw.Length, "Every Level " + level + " catalog action remains represented.");
                Rect badge = (Rect)footerType.GetField("LevelBadge").GetValue(footer);
                Rect drawnBadge = new Rect(badge.x, badge.y * verticalScale, badge.width, badge.height);
                Rect flags = (Rect)footerType.GetField("Pennants").GetValue(footer);
                float expectedFlags = ExpectedResponsivePennantHeight(level, actions.Length, false, canvasHeight, stageBottom, 0f);
                Assert.AreEqual(expectedFlags, flags.height, .2f, "Level " + level + " reserves only real free pennant space.");
                if (flags.height > 1f)
                {
                    Assert.AreEqual(canvasHeight, flags.yMax, .1f, "Level " + level + " pennants end flush with the safe footer edge.");
                    Assert.GreaterOrEqual(flags.yMin, stageBottom - .1f);
                }
                Rect pennantArt = (Rect)View.GetMethod("FooterPennantRenderBounds", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { flags, canvasHeight, 0f });
                if (flags.height > 1f) Assert.AreEqual(flags, pennantArt, "Visible pennant slots retain their exact bounds.");
                else Assert.AreEqual(Rect.zero, pennantArt, "No pennant fallback may be painted behind cards.");
                float compactScale = Mathf.Min(1f, verticalScale);
                float footerBottom = flags.height > 1f ? flags.yMin - 2f * compactScale : canvasHeight - 4f * compactScale;
                Rect drawnButtons = FooterButtonVerticalBounds(raw, verticalScale, Rect.zero);
                float buttonAreaTop = drawnBadge.yMax + 6f * compactScale;
                float expectedButtonTop = buttonAreaTop + Mathf.Max(0f,
                    (footerBottom - buttonAreaTop - drawnButtons.height) * .5f);
                float expectedBadgeTop = Mathf.Min(stageBottom + 4f * compactScale,
                    footerBottom - (drawnBadge.height + 6f * compactScale + drawnButtons.height));
                Assert.AreEqual(expectedBadgeTop, drawnBadge.yMin, .2f,
                    "Level " + level + " name/neighborhood begins at the top of the safe footer.");
                Assert.AreEqual(expectedButtonTop, drawnButtons.yMin, .2f,
                    "Level " + level + " upgrade block is vertically centered below its caption.");
                for (int i = 0; i < raw.Length; i++)
                {
                    Rect card = (Rect)scaleCard.Invoke(null, new object[] { raw[i], verticalScale });
                    card.y *= verticalScale;
                    Assert.GreaterOrEqual(card.xMin, 0f); Assert.LessOrEqual(card.xMax, 540f);
                    Assert.GreaterOrEqual(card.yMin, stageBottom - .1f, "Level " + level + " card entered the playfield.");
                    Assert.LessOrEqual(card.yMax, canvasHeight + .1f, "Level " + level + " card is clipped.");
                    Assert.GreaterOrEqual(card.yMin, drawnBadge.yMax - .1f);
                    Assert.IsFalse(card.Overlaps(flags), "Level " + level + " card overlaps the reserved pennant band.");
                    for (int j = 0; j < i; j++)
                    {
                        Rect other = (Rect)scaleCard.Invoke(null, new object[] { raw[j], verticalScale });
                        other.y *= verticalScale;
                        Assert.IsFalse(card.Overlaps(other), "Level " + level + " upgrade cards overlap.");
                    }
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
                float expectedReadyFlags = ExpectedResponsivePennantHeight(level, raw.Length <= 3 ? raw.Length : 0,
                    true, canvasHeight, stageBottom, 0f);
                Assert.AreEqual(expectedReadyFlags, readyFlags.height, .2f);
                Assert.IsFalse(drawnSelector.Overlaps(readyFlags));
                Rect[] readyCards = raw.Length <= 3 ? (Rect[])ready.GetType().GetField("UpgradeBounds").GetValue(ready) : new Rect[0];
                Rect drawnReadyButtons = FooterButtonVerticalBounds(readyCards, verticalScale, selector);
                float readyFooterBottom = readyFlags.height > 1f
                    ? readyFlags.yMin - 2f * compactScale : canvasHeight - 4f * compactScale;
                float readyButtonAreaTop = drawnReadyBadge.yMax + 6f * compactScale;
                float expectedReadyButtonTop = readyButtonAreaTop + Mathf.Max(0f,
                    (readyFooterBottom - readyButtonAreaTop - drawnReadyButtons.height) * .5f);
                float expectedReadyBadgeTop = Mathf.Min(stageBottom + 4f * compactScale,
                    readyFooterBottom - (drawnReadyBadge.height + 6f * compactScale + drawnReadyButtons.height));
                Assert.AreEqual(expectedReadyBadgeTop, drawnReadyBadge.yMin, .2f,
                    "Ready keeps the location label at the top of the footer.");
                Assert.AreEqual(expectedReadyButtonTop, drawnReadyButtons.yMin, .2f,
                    "Ready buttons are centered below the location label.");
                Rect readyPennantArt = (Rect)View.GetMethod("FooterPennantRenderBounds", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { readyFlags, canvasHeight, 0f });
                if (readyFlags.height > 1f) Assert.AreEqual(readyFlags, readyPennantArt);
                else Assert.AreEqual(Rect.zero, readyPennantArt);

                object noControls = build.Invoke(null, new object[] { sim, canvasHeight, false, false });
                Rect noControlFlags = (Rect)noControls.GetType().GetField("Pennants").GetValue(noControls);
                float expectedNoControlFlags = ExpectedResponsivePennantHeight(level, 0, false, canvasHeight, stageBottom, 0f);
                Assert.AreEqual(expectedNoControlFlags, noControlFlags.height, .2f);
                Rect noControlPennantArt = (Rect)View.GetMethod("FooterPennantRenderBounds", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { noControlFlags, canvasHeight, 0f });
                if (noControlFlags.height > 1f) Assert.AreEqual(noControlFlags, noControlPennantArt);
                else Assert.AreEqual(Rect.zero, noControlPennantArt);
            }
        }

        private static Rect FooterButtonVerticalBounds(Rect[] cards, float verticalScale, Rect selector)
        {
            float compactScale = Mathf.Min(1f, verticalScale);
            float top = selector.height > 0f ? selector.y * verticalScale : float.PositiveInfinity;
            float bottom = selector.height > 0f ? selector.y * verticalScale + selector.height : float.NegativeInfinity;
            for (int i = 0; i < cards.Length; i++)
            {
                float cardTop = cards[i].y * verticalScale;
                top = Mathf.Min(top, cardTop);
                bottom = Mathf.Max(bottom, cardTop + cards[i].height * compactScale);
            }
            return float.IsPositiveInfinity(top) ? Rect.zero : Rect.MinMaxRect(0f, top, 1f, bottom);
        }

        private static float ExpectedResponsivePennantHeight(int levelIndex, int count, bool ready, float canvasHeight, float stageBottom, float bottomBleed)
        {
            if (levelIndex >= 5) return 0f;
            float compact = Mathf.Min(1f, canvasHeight / 960f);
            int rows = count == 0 ? 0 : count <= 3 ? 1 : 2;
            float cardHeight = (count >= 5 ? 100f : count == 4 ? 104f : 120f) * compact;
            float rowGap = rows > 1 ? 8f * compact : 0f;
            float badgeHeight = (count >= 5 ? 20f : count == 4 ? 22f : 26f) * compact;
            float sectionGap = rows > 0 || ready ? 6f * compact : 0f;
            float selectorHeight = ready ? 44f * compact : 0f;
            float selectorGap = ready && rows > 0 ? 6f * compact : 0f;
            float panelHeight = badgeHeight + sectionGap + rows * cardHeight + Mathf.Max(0, rows - 1) * rowGap
                + selectorGap + selectorHeight;
            float free = canvasHeight + bottomBleed - stageBottom - 4f * compact - panelHeight - 2f * compact;
            return Mathf.Min(80f * compact, Mathf.Max(0f, free));
        }

        [Test]
        public void PennantsUseTheDecorativeBottomInsetWithoutMovingFooterControls()
        {
            Vector2 screen = new Vector2(1080f, 2400f);
            Rect safe = new Rect(0f, 80f, 1080f, 2260f);
            Rect viewport = (Rect)View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { screen, safe });
            float canvasHeight = (float)View.GetMethod("CanvasLogicalHeight", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { viewport });
            float bottomBleed = (float)View.GetMethod("SafeAreaBottomInsetLogical", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { viewport, screen.y });
            Assert.AreEqual(40f, bottomBleed, .01f);

            Type simType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            ConstructorInfo constructor = simType.GetConstructor(new[] {
                balanceType, typeof(int), typeof(float), typeof(int), typeof(int), typeof(int)
            });
            object sim = constructor.Invoke(new object[] { Activator.CreateInstance(balanceType), 10, 5f, 0, 1, 0 });
            object footer = View.GetMethod("BuildResponsiveFooterLayoutWithBottomBleed", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { sim, canvasHeight, true, false, bottomBleed });
            Rect[] cards = (Rect[])footer.GetType().GetField("UpgradeBounds").GetValue(footer);
            Rect pennants = (Rect)footer.GetType().GetField("Pennants").GetValue(footer);
            if (pennants.height > 1f)
            {
                Assert.AreEqual(canvasHeight + bottomBleed, pennants.yMax, .01f);
                Assert.Greater(pennants.height, 1f, "The safe inset leaves a real, non-overlapping pennant band.");
                foreach (Rect card in cards) Assert.GreaterOrEqual(pennants.yMin, card.yMax - .01f);
            }
            else Assert.AreEqual(0f, pennants.height, .01f,
                "The Racing+ layout hides the original pennant slot without moving its controls.");
            Rect pennantArt = (Rect)View.GetMethod("FooterPennantRenderBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { pennants, canvasHeight, bottomBleed });
            Assert.AreEqual(pennants.height > 1f ? pennants : Rect.zero, pennantArt,
                "The safe inset extends only a reserved pennant band; an empty slot never paints behind cards.");
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
            Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
            Rect coinBounds = (Rect)View.GetMethod("UpgradeCoinBounds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { speed });
            Assert.AreEqual(speed.xMax - 8f - (coinBounds.xMax + 2f), price.width, .01f);
            Assert.Greater(price.width, 80f, "The right-side price capsule keeps four-digit costs legible.");
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
        public void StatusHudRemainsCompactWhileProductCountersOverlayTheMural()
        {
Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect grid = (Rect)View.GetField("HudQuotaGrid", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect coins = (Rect)View.GetField("HudCoins", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect clock = (Rect)View.GetField("HudTime", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect cell0 = (Rect)View.GetMethod("HudStatusCellBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 0, Rect.zero });
            Rect cell1 = (Rect)View.GetMethod("HudStatusCellBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 1, Rect.zero });
            Assert.AreEqual(0f, bar.yMin); Assert.AreEqual(39.36f, bar.height, .01f);
            Assert.AreEqual(8f, cell0.xMin, .01f); Assert.AreEqual(532f, cell1.xMax, .01f);
            Assert.AreEqual(cell0.width, cell1.width, .01f, "Coins and time share the main strip equally.");
            Assert.AreEqual(6f, cell1.xMin - cell0.xMax, .01f);
            Assert.AreEqual(34.56f, cell0.height, .01f, "The primary status strip is 20% taller.");
            Assert.AreEqual(8f, grid.x, .01f); Assert.AreEqual(41.76f, grid.y, .01f);
            Assert.AreEqual(524f, grid.width, .01f); Assert.AreEqual(77.28f, grid.height, .01f);
            Assert.IsTrue(bar.Contains(coins.min)); Assert.IsTrue(bar.Contains(coins.max));
            Assert.IsTrue(bar.Contains(clock.min)); Assert.IsTrue(bar.Contains(clock.max));
            Assert.IsFalse(coins.Overlaps(clock)); Assert.IsFalse(bar.Contains(grid.min));
            Assert.IsFalse(coins.Overlaps(grid)); Assert.IsFalse(clock.Overlaps(grid));
            Assert.IsNull(View.GetField("HudLevel", BindingFlags.Static | BindingFlags.NonPublic));
            Assert.AreEqual(207f, clock.width, .01f); Assert.AreEqual(34.56f, clock.height, .01f);
            int[] products = { 0, 1, 2, 3, 4, 6, 5 };
            MethodInfo cellMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int[]), typeof(int), typeof(Rect) }, null);
            Rect[] cells = new Rect[7];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = (Rect)cellMethod.Invoke(null, new object[] { i, products, products.Length, Rect.zero });
                Assert.GreaterOrEqual(cells[i].xMin, grid.xMin - .01f);
                Assert.LessOrEqual(cells[i].xMax, grid.xMax + .01f);
                Assert.GreaterOrEqual(cells[i].yMin, grid.yMin - .01f);
                Assert.LessOrEqual(cells[i].yMax, grid.yMax + .01f);
                float expectedX = i < 4 ? 8f + i % 2 * 117f : 310f + (i - 4) % 2 * 117f;
                Assert.AreEqual(expectedX, cells[i].xMin, .01f);
                Assert.GreaterOrEqual(cells[i].yMin, bar.yMax);
                Assert.AreEqual(105f, cells[i].width, .01f);
                for (int j = 0; j < i; j++) Assert.IsFalse(cells[i].Overlaps(cells[j]));
            }
            foreach (string name in new[] { "CutoutHudCoins", "CutoutHudTime", "CutoutHudSales" })
                Assert.IsNull(View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic), name + " is not a supported HUD layout");
            const BindingFlags counterFlags = BindingFlags.Static | BindingFlags.NonPublic;
            int sharedCounterFontSize = (int)View.GetField("HudCounterFontSize", counterFlags).GetRawConstantValue();
            Assert.AreEqual(35, sharedCounterFontSize, "Keep the largest current HUD counter size.");
            foreach (string counter in new[] { "HudClockFontSize", "HudStatusValueFontSize", "HudQuotaCounterFontSize" })
                Assert.AreEqual(sharedCounterFontSize,
                    (int)View.GetField(counter, counterFlags).GetRawConstantValue(), counter + " shares the common size.");
            Assert.AreEqual(28.8f, (float)View.GetField("HudStatusIconSize", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue(), .001f);
        }

        [Test]
        public void HeaderHasOnlyEqualCoinAndClockCardsAndRemovesLevelBadge()
        {
            MethodInfo cellMethod = View.GetMethod("HudStatusCellBounds", BindingFlags.Static | BindingFlags.NonPublic);
            Rect coins = (Rect)cellMethod.Invoke(null, new object[] { 0, Rect.zero });
            Rect clock = (Rect)cellMethod.Invoke(null, new object[] { 1, Rect.zero });
            Assert.AreEqual(coins.width, clock.width, .01f);
            Assert.AreEqual(6f, clock.xMin - coins.xMax, .01f);
            Assert.IsNull(View.GetField("HudLevel", BindingFlags.Static | BindingFlags.NonPublic));
            Assert.AreEqual(Rect.zero, (Rect)cellMethod.Invoke(null, new object[] { 2, Rect.zero }));
            Assert.AreEqual(1.2f, (float)View.GetField("HudStatusHeightScale", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue(), .001f);
        }

        [Test]
        public void ObjectiveChipsUseTranslucentCoolColors()
        {
            MethodInfo colorMethod = View.GetMethod("HudQuotaCardColor", BindingFlags.Static | BindingFlags.NonPublic);
            Color active = (Color)colorMethod.Invoke(null, new object[] { false });
            Color complete = (Color)colorMethod.Invoke(null, new object[] { true });
            Assert.AreEqual(.65f, active.a, .001f, "The active fill is 65% opaque.");
            Assert.AreEqual(.65f, complete.a, .001f, "The completed fill is also 65% opaque.");
            Assert.Greater(active.b, active.r, "The active charcoal resource card contrasts with the warm wooden header.");
            Assert.Less(active.r, .1f); Assert.Less(active.g, .15f); Assert.Less(active.b, .3f);
            Assert.AreNotEqual(active, complete);
        }

        [TestCase(0, new[] { 0 })]
        [TestCase(1, new[] { 0, 4 })]
        [TestCase(2, new[] { 0, 1, 4 })]
        [TestCase(3, new[] { 0, 1, 2, 4, 6 })]
        [TestCase(4, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(5, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(6, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(7, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(8, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(9, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        [TestCase(10, new[] { 0, 1, 2, 3, 4, 6, 5 })]
        public void HudQuotaCardsGroupSolidsLeftAndLiquidsRightForEveryLevel(int level, int[] products)
        {
            Type simulationType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            object simulation = Activator.CreateInstance(simulationType, new object[] {
                Activator.CreateInstance(balanceType), level, 5f, 0, 1, 0
            });
            Assert.AreEqual(products.Length, simulationType.GetProperty("ProductCount").GetValue(simulation, null));
            for (int i = 0; i < products.Length; i++)
                Assert.AreEqual(products[i], simulationType.GetMethod("GetAvailableProduct").Invoke(simulation, new object[] { i }),
                    "The test follows the official level catalog.");
            MethodInfo cellMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int[]), typeof(int), typeof(Rect) }, null);
            MethodInfo gridMethod = View.GetMethod("HudQuotaGridBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Rect), typeof(int[]), typeof(int) }, null);
            Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect grid = (Rect)gridMethod.Invoke(null, new object[] { Rect.zero, products, products.Length });
            Rect[] cards = new Rect[products.Length];
            int[] currentIndexesByColumn = new int[2];
            for (int i = 0; i < products.Length; i++)
            {
                cards[i] = (Rect)cellMethod.Invoke(null, new object[] { i, products, products.Length, Rect.zero });
                int column = products[i] < 4 ? 0 : 1;
                int groupIndex = currentIndexesByColumn[column]++;
                float expectedX = column == 0
                    ? 8f + groupIndex % 2 * 117f
                    : 310f + groupIndex % 2 * 117f;
                Assert.AreEqual(expectedX, cards[i].xMin, .01f,
                    "Solid SKUs stay in the left group; drink SKUs stay in the right group.");
                Assert.GreaterOrEqual(cards[i].yMin, bar.yMax, "The count overlays scenery just below the status strip.");
                Assert.AreEqual(105f, cards[i].width, .01f, "Each sales counter is 25% wider while staying compact enough for the longest delivered/goal value.");
                Assert.AreEqual(37.44f, cards[i].height, .02f, "Each sales counter is 25% taller than its previous 29.952-unit size.");
                Assert.GreaterOrEqual(cards[i].xMin, grid.xMin - .01f);
                Assert.LessOrEqual(cards[i].xMax, grid.xMax + .01f);
                Assert.GreaterOrEqual(cards[i].yMin, grid.yMin - .01f);
                Assert.LessOrEqual(cards[i].yMax, grid.yMax + .01f);
                Assert.AreEqual(groupIndex / 2, Mathf.RoundToInt((cards[i].yMin - (bar.yMax + 2.4f)) / 39.84f),
                    "Catalog order flows left-to-right, then top-to-bottom, in each category group.");
                for (int j = 0; j < i; j++) Assert.IsFalse(cards[i].Overlaps(cards[j]));
            }
        }

        [TestCase(540f, 960f, 0f)]
        [TestCase(575f, 1280f, 0f)]
        [TestCase(540f, 1200f, 0f)]
        [TestCase(1080f, 2400f, 50f)]
        public void HudQuotaGroupsClearEveryRearRowOrderBubbleAtPortraitSizes(float screenWidth, float screenHeight, float topInset)
        {
            Vector2 screenSize = new Vector2(screenWidth, screenHeight);
            Rect safeArea = new Rect(0f, 0f, screenWidth, screenHeight - topInset);
            Rect viewport = (Rect)View.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { screenSize, safeArea });
            float verticalScale = (float)View.GetMethod("CanvasLogicalHeight", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { viewport }) / 960f;
            Type sceneLayout = Type.GetType("HayChoriYPaty.StreetSceneLayout, Assembly-CSharp", true);
            float counterTop = (float)sceneLayout.GetMethod("CounterTopInViewport").Invoke(null,
                new object[] { screenWidth, screenHeight, viewport });
            float customerOffset = (float)sceneLayout.GetMethod("CustomerOffset").Invoke(null,
                new object[] { counterTop, verticalScale });
            float screenScale = viewport.width / 540f;
            float viewportTopInLogicalUnits = viewport.y / screenScale;
            int[] products = { 0, 1, 2, 3, 4, 6, 5 };
            Rect[] cutouts = screenWidth == 1080f
                ? new[] { new Rect(478f, 2320f, 124f, 80f) }
                : Array.Empty<Rect>();
            Rect gap = (Rect)View.GetMethod("TopHudCutoutBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { screenSize, cutouts });
            MethodInfo cell = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int[]), typeof(int), typeof(Rect) }, null);
            Rect[] cards = new Rect[products.Length];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = (Rect)cell.Invoke(null, new object[] { i, products, products.Length, gap });

            MethodInfo queuePosition = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true)
                .GetMethod("QueuePosition", BindingFlags.Static | BindingFlags.NonPublic);
            for (int slot = 7; slot < 14; slot++)
            {
                Vector2 anchor = (Vector2)queuePosition.Invoke(null, new object[] { slot });
                anchor.y += customerOffset;
                Rect logicalBubble = Bubble(anchor, 2);
                float contentScale = OrderContentScale(logicalBubble.height, verticalScale);
                Rect bubble = OrderRect("OrderBubbleLayoutBounds", logicalBubble, verticalScale);
                bubble.y += viewportTopInLogicalUnits;
                for (int i = 0; i < cards.Length; i++)
                    Assert.IsFalse(cards[i].Overlaps(bubble),
                        "A rear-row order bubble overlaps quota " + i + " at " + screenWidth + "x" + screenHeight + ".");
                Assert.Greater(contentScale, 0f);
            }
        }

        [TestCase(540f, 960f)]
        [TestCase(575f, 1280f)]
        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1220f, 2712f)]
        public void HudQuotaStacksStayInBoundsAcrossPortraitResolutions(float screenWidth, float screenHeight)
        {
            Rect[] cutouts = new Rect[0];
            if (screenWidth == 1080f && screenHeight == 2400f)
                cutouts = new[] { new Rect(478f, 2320f, 124f, 80f) };
            else if (screenWidth == 1220f)
                cutouts = new[] { new Rect(534f, 2660f, 152f, 52f) };
            Rect gap = (Rect)View.GetMethod("TopHudCutoutBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Vector2(screenWidth, screenHeight), cutouts });
            int[] products = { 0, 1, 2, 3, 4, 6, 5 };
            MethodInfo cellMethod = View.GetMethod("HudQuotaCellBounds", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int[]), typeof(int), typeof(Rect) }, null);
            float scale = screenWidth / 540f;
            float logicalHeight = screenHeight / scale;
            float barHeight = (float)View.GetMethod("HudBarHeightFor", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Rect) }, null).Invoke(null, new object[] { gap });
            Assert.LessOrEqual(barHeight * scale, screenHeight);
            for (int i = 0; i < products.Length; i++)
            {
                Rect card = (Rect)cellMethod.Invoke(null, new object[] { i, products, products.Length, gap });
                Assert.GreaterOrEqual(card.xMin, 0f); Assert.LessOrEqual(card.xMax, 540f);
                Assert.GreaterOrEqual(card.yMin, barHeight);
                Assert.LessOrEqual(card.yMax, logicalHeight, "Counters stay on-screen at " + screenWidth + "x" + screenHeight);
                Assert.IsFalse(card.Overlaps(gap), "No counter crosses the top cutout at " + screenWidth + "x" + screenHeight);
            }
        }

        [Test]
        public void HudQuotaCountersCenterTextWhileProductIconsStayAtTheLeftInset()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            TextAnchor alignment = (TextAnchor)View.GetField("HudQuotaCounterTextAlignment", flags).GetRawConstantValue();
            float inset = (float)View.GetField("HudQuotaCardInset", flags).GetRawConstantValue();
            float rightInset = (float)View.GetField("HudQuotaCounterRightInset", flags).GetRawConstantValue();
            Assert.AreEqual(TextAnchor.MiddleCenter, alignment);
            Assert.AreEqual(1.5f, inset, .01f, "The icon inset scales with the enlarged card.");
            Assert.AreEqual(6f, rightInset, .01f, "Keep a small but visible gap after the centered count area.");
            Rect card = new Rect(0, 0, 100, 30);
            Rect icon = new Rect(inset, 2, 24, 24);
            Rect counter = (Rect)View.GetMethod("HudQuotaCounterBounds", flags)
                .Invoke(null, new object[] { card, icon });
            Assert.Greater(counter.xMin, icon.xMax, "Text remains separated from the icon.");
            Assert.AreEqual(card.xMax - rightInset, counter.xMax, .01f, "The count's text area retains right padding.");
            Assert.AreEqual(card.center.y, counter.center.y, .01f, "The count's text area is vertically centered in the chip.");
        }

        [Test]
        public void HudQuotaCountersShareOneFitFontSizeAcrossAllValues()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo fitFontSize = View.GetMethod("HudNumberFitFontSize", flags);
            var style = new GUIStyle(EditorStyles.label) { fontSize = 35, wordWrap = false, padding = new RectOffset(0, 0, 0, 0) };
            Rect counter = new Rect(0, 0, 61.8f, 33.2f);
            string[] values = { "40/210", "40/145", "40/80", "32/75", "70/175", "43/90", "41/75" };
            int sharedFontSize = 35;
            foreach (string value in values)
                sharedFontSize = Math.Min(sharedFontSize,
                    (int)fitFontSize.Invoke(null, new object[] { style, counter, value, 35 }));
            Assert.Less(sharedFontSize, 35, "The common quota size must fit within the text cell.");
            foreach (string value in values)
                Assert.AreEqual(sharedFontSize,
                    (int)fitFontSize.Invoke(null, new object[] { style, counter, value, sharedFontSize }),
                    value + " is rendered at the same shared quota-counter font size.");
        }

        [Test]
        public void AllElevenVisibleLevelCaptionsContainTheOfficialTeamAndNeighborhood()
        {
            Type simulation = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            string[] names = (string[])simulation.GetField("LevelDisplayNames").GetValue(null);
            CollectionAssert.AreEqual(new[] {
                "All Boys / Floresta", "Nueva Chicago / Mataderos", "Vélez Sarsfield / Liniers",
                "Ferro / Caballito", "Independiente / Avellaneda", "Racing Club / Avellaneda",
                "San Lorenzo / Boedo", "River Plate / Núñez", "Boca Juniors / La Boca",
                "Sindicato de Camioneros / Plaza de Mayo", "Los Redondos / Tandil"
            }, names);
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            MethodInfo forLevel = themes.GetMethod("ForLevel", BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < names.Length; i++)
                Assert.AreNotEqual("Equipo sin configurar", themes.GetProperty("ClubName").GetValue(forLevel.Invoke(null, new object[] { i })));
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
                Assert.AreEqual(1080, texture.width); Assert.AreEqual(79, texture.height);
                Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
                Assert.IsTrue(texture.name.Contains("gameplay HUD"));
                Assert.AreEqual(HideFlags.HideAndDontSave, texture.hideFlags);
                // Runtime textures deliberately discard their CPU copy after upload.
                Assert.IsFalse(texture.isReadable); Assert.IsFalse(cutout.isReadable);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(cutout); }
        }

        [Test]
        public void HudCutoutKeepsTheHudBackgroundInsteadOfRevealingScenery()
        {
            MethodInfo create = View.GetMethod("CreateHudBarTexture", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Rect), typeof(bool) }, null);
            Assert.IsNotNull(create);
            var gap = new Rect(232f, 0f, 70f, 34f);
            Texture2D texture = (Texture2D)create.Invoke(null, new object[] { gap, true });
            try
            {
                Color underCamera = texture.GetPixel(Mathf.RoundToInt(267f * 2f), texture.height - Mathf.RoundToInt(17f * 2f) - 1);
                Assert.Greater(underCamera.a, .99f, "The notch channel keeps HUD fill; only the physical camera covers it.");
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [TestCase(700f)]
        [TestCase(960f)]
        [TestCase(1200f)]
        public void VictoryPopupAndExitUseOneUniformPortraitFrame(float canvasHeight)
        {
            Rect frame = (Rect)View.GetMethod("VictoryPopupBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight, false });
            Rect exitSlot = (Rect)View.GetField("VictoryExit", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect exit = (Rect)View.GetMethod("VictorySlot", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { frame, exitSlot });
            Assert.GreaterOrEqual(frame.xMin, 0f); Assert.LessOrEqual(frame.xMax, 540f);
            Assert.GreaterOrEqual(frame.yMin, 16f); Assert.LessOrEqual(frame.yMax, canvasHeight - 16f);
            Assert.AreEqual(1122f / 1402f, frame.width / frame.height, .001f);
            Assert.IsTrue(frame.Contains(exit.min)); Assert.IsTrue(frame.Contains(exit.max));
            Assert.Greater(exit.yMin, frame.center.y, "Only the green bottom button is interactive");
        }

        [TestCase(700f)]
        [TestCase(960f)]
        [TestCase(1200f)]
        public void ChulengoVictoryButtonsStayStackedWithoutOverlap(float canvasHeight)
        {
            Rect frame = (Rect)View.GetMethod("VictoryPopupBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasHeight, true });
            Rect multiply = (Rect)View.GetMethod("ChulengoMultiplyBoundsForFrame", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { frame });
            Rect collect = (Rect)View.GetMethod("ChulengoCollectBoundsForFrame", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { frame, canvasHeight });
            Rect levels = (Rect)View.GetMethod("ChulengoLevelsBoundsForFrame", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { frame, canvasHeight });

            Assert.Greater(multiply.yMax, frame.yMax, "The main action intentionally overlaps the wood's lower edge.");
            Assert.GreaterOrEqual(collect.yMin, multiply.yMax + 7f);
            Assert.GreaterOrEqual(levels.yMin, collect.yMax + 13f);
            Assert.LessOrEqual(levels.yMax, canvasHeight - 4f);
        }

        [TestCase(700f)]
        [TestCase(960f)]
        [TestCase(1200f)]
        public void ChulengoVictoryReferenceHeadlineAndTicketFitThePopup(float canvasHeight)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            Assert.AreEqual("MISIÓN", View.GetField("ChulengoVictoryTitleFirstLine", flags).GetValue(null));
            Assert.AreEqual("COMPLETADA", View.GetField("ChulengoVictoryTitleSecondLine", flags).GetValue(null));
            Assert.AreEqual("+1 TICKET", View.GetField("ChulengoTicketCaption", flags).GetValue(null));
            Assert.AreEqual(Color.white, View.GetField("ChulengoZeroWarningFill", flags).GetValue(null));

            Rect frame = (Rect)View.GetMethod("VictoryPopupBounds", flags)
                .Invoke(null, new object[] { canvasHeight, true });
            Rect firstLine = (Rect)View.GetMethod("ChulengoTitleLineBoundsForFrame", flags)
                .Invoke(null, new object[] { frame, false });
            Rect secondLine = (Rect)View.GetMethod("ChulengoTitleLineBoundsForFrame", flags)
                .Invoke(null, new object[] { frame, true });
            Rect amount = (Rect)View.GetMethod("ChulengoPrizeAmountBoundsForFrame", flags).Invoke(null, new object[] { frame });
            Rect ticket = (Rect)View.GetMethod("ChulengoTicketCardBoundsForFrame", flags).Invoke(null, new object[] { frame });
            Rect ticketCaption = (Rect)View.GetMethod("ChulengoTicketCaptionBoundsForFrame", flags).Invoke(null, new object[] { frame });
            Rect meterContainer = (Rect)View.GetMethod("VictorySlot", flags)
                .Invoke(null, new object[] { frame, new Rect(.0625f, .50f, .875f, .245f) });
            Rect meter = (Rect)View.GetMethod("FitChulengoMeter", flags).Invoke(null, new object[] { meterContainer });

            Assert.IsTrue(frame.Contains(firstLine.min) && frame.Contains(firstLine.max));
            Assert.IsTrue(frame.Contains(secondLine.min) && frame.Contains(secondLine.max));
            Assert.IsFalse(firstLine.Overlaps(secondLine), "The two-line comic headline keeps independent text slots.");
            Assert.IsTrue(frame.Contains(ticket.min) && frame.Contains(ticket.max));
            Assert.IsTrue(frame.Contains(ticketCaption.min) && frame.Contains(ticketCaption.max));
            Assert.IsFalse(amount.Overlaps(ticket), "The ticket badge sits beside, not over, the payout.");
            Assert.IsFalse(amount.Overlaps(ticketCaption), "The ticket caption stays legible beside the payout.");
            Assert.LessOrEqual(ticketCaption.yMax, meter.yMin,
                "The ticket badge ends before the visible gauge begins.");
        }

        [Test]
        public void ChulengoTicketArtworkUsesYellowStubRedPerforationAndBlueStar()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var texture = (Texture2D)View.GetMethod("CreateChulengoTicketTexture", flags).Invoke(null, null);
            Assert.IsNotNull(texture);
            try
            {
                Assert.AreEqual(512, texture.width);
                Assert.AreEqual(320, texture.height);
                Assert.Less(texture.GetPixel(0, 0).a, .01f, "The die-cut ticket stays transparent outside its silhouette.");
                Color blueStar = texture.GetPixel(texture.width / 2, texture.height / 2);
                Assert.Greater(blueStar.b, blueStar.r * 3f, "The coupon's central symbol is the reference's blue star.");
                Color redPerforation = texture.GetPixel(Mathf.RoundToInt(texture.width * .22f),
                    Mathf.RoundToInt(texture.height * .16f));
                Assert.Greater(redPerforation.r, redPerforation.g * 2f, "The inset perforation line is red.");
                Color yellowFace = texture.GetPixel(Mathf.RoundToInt(texture.width * .22f),
                    Mathf.RoundToInt(texture.height * .23f));
                Assert.Greater(yellowFace.r, .9f);
                Assert.Greater(yellowFace.g, .55f);
                Assert.Greater(yellowFace.r, yellowFace.b * 1.7f, "The ticket face is yellow, not a blue square.");
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void ChulengoFlagsUseTheActiveLevelPrimaryAndSecondaryColors()
        {
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            MethodInfo mapBand = View.GetMethod("ChulengoFlagBandColor", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(mapBand);
            Type simulation = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            int levelCount = ((string[])simulation.GetField("LevelNames").GetValue(null)).Length;
            for (int level = 0; level < levelCount; level++)
            {
                object theme = themes.GetMethod("ForLevel").Invoke(null, new object[] { level });
                Color32 primary = (Color32)(Color)themes.GetProperty("PrimaryColor").GetValue(theme);
                Color32 secondary = (Color32)(Color)themes.GetProperty("SecondaryColor").GetValue(theme);
                float primaryLuminance = primary.r * .299f + primary.g * .587f + primary.b * .114f;
                float secondaryLuminance = secondary.r * .299f + secondary.g * .587f + secondary.b * .114f;
                bool mapPrimaryToDark = Mathf.Abs(primaryLuminance - secondaryLuminance) < 8f || primaryLuminance < secondaryLuminance;
                Color32 expectedDark = mapPrimaryToDark ? primary : secondary;
                Color32 expectedLight = mapPrimaryToDark ? secondary : primary;
                Assert.AreEqual(expectedDark, mapBand.Invoke(null, new object[] { .05f, primary, secondary }), "dark stripe, level " + level);
                Assert.AreEqual(expectedLight, mapBand.Invoke(null, new object[] { .96f, primary, secondary }), "light stripe, level " + level);
            }
        }

        [Test]
        public void ChulengoPopupColorizesOnlyTheTwoFlagClothAreas()
        {
            const string resource = "street-victory-popup-wood-v1";
            Texture2D source = Resources.Load<Texture2D>(resource);
            Assert.NotNull(source);
            Assert.IsTrue(source.isReadable, "The runtime recolor requires the imported authored texture to be CPU-readable.");
            Type themes = Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true);
            object racing = themes.GetMethod("ForLevel").Invoke(null, new object[] { 5 });
            MethodInfo create = View.GetMethod("CreateClubColoredVictoryPopup", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(create);
            Texture2D colored = (Texture2D)create.Invoke(null, new[] { (object)source, racing, true });
            Assert.NotNull(colored);
            try
            {
                Color32[] original = source.GetPixels32();
                Color32[] result = colored.GetPixels32();
                int changedFlags = 0, changedElsewhere = 0, racingBluePixels = 0;
                for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                {
                    int index = y * source.width + x;
                    Color32 a = original[index], b = result[index];
                    bool changed = a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;
                    bool flagRegion = (y + .5f) / source.height >= .87f && (y + .5f) / source.height <= .992f &&
                        ((x + .5f) / source.width >= .15f && (x + .5f) / source.width <= .305f ||
                         (x + .5f) / source.width >= .695f && (x + .5f) / source.width <= .85f);
                    if (flagRegion)
                    {
                        if (changed) changedFlags++;
                        if (b.b > b.r + 24 && b.g > b.r + 18) racingBluePixels++;
                    }
                    else if (changed) changedElsewhere++;
                }
                Assert.Greater(changedFlags, 1000, "Both authored flag fabrics should be recolored.");
                Assert.Greater(racingBluePixels, 1000, "Racing's sky-blue stripe should be visible in the flags.");
                Assert.AreEqual(0, changedElsewhere, "The board, crest, poles and all other artwork remain pixel-identical.");
            }
            finally { UnityEngine.Object.DestroyImmediate(colored); }
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
            int[] delivered = (int[])simType.GetField("deliveredByProduct", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sim);
            delivered[5] = (int)simType.GetMethod("GetProductGoal").Invoke(sim, new object[] { 5 });
            complete = (string)summary.Invoke(null, new object[] { sim, false });
            pending = (string)summary.Invoke(null, new object[] { sim, true });
            foreach (string code in new[] { "CHO", "PAT", "BON", "VAC", "COC", "FER", "CER" })
                Assert.IsTrue(complete.Contains(code + " "), "Complete result lists every catalog product.");
            foreach (string code in new[] { "CHO", "PAT", "BON", "VAC", "COC", "CER" })
                Assert.IsTrue(pending.Contains(code + " "), "Defeat result lists each incomplete product.");
            Assert.IsFalse(pending.Contains("FER "), "Defeat result omits the completed Fernet quota.");
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
                Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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

        [TestCase("food-table-plank.png", "ModularKitchen/Art/food-table-plank", "4c6fac694e5d7c0b364a1eac649f0d9357e84e3bb04ba7a45e00d8eca2e31b29", "1ecc3f9a7984f7921b9f46c6e0b7d704ab45e059e39f47fb3e34f68af3b96a9b", "ba31aede125064fd8857b2227d07992e")]
        [TestCase("food-table-trestle.png", "ModularKitchen/Art/food-table-trestle", "1f69a2f12cdb975ede6a118684c6c79dd10cbccf6bd04b5f46f55dd49512f315", "f89ecb575450c341147ad3364d67afe7207d44b128a2ee3c8ba7a273981badbc", "00ac52b88645b4b9d943757fe4beefcf")]
        [TestCase("street-ready-sandwiches-table-v1.png", "street-ready-sandwiches-table-v1", "c799a8a7345a827ee0c2b792ec7a99e47dcebbb3190cd06a551a7d04563217f9", "15539eb9ad4c4a1770df5cdc62dd9b6c63ea60977b9c994a34c864ae3197bf22", "4d07362ad5b74f948a14cb751a35874c")]
        [TestCase("street-serving-table-v1.png", "street-serving-table-v1", "a44bc943193943c549fe792d6dc90141508bdc940c893e0d0cd1c4807c9da4d1", "5ed655df74573ab57419b321da35a4c3143166b314c84212fcffdd96b84210d4", "262c70b1a7b14d359409cefc28e1de95")]
        public void LegacyFoodArtIsPreservedByteForByteButNotShippedInResources(
            string filename, string resource, string pngHash, string metaHash, string guid)
        {
            string path = "Assets/Art/Editor/LegacyKitchen/" + filename;
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                Assert.AreEqual(pngHash, BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant());
                Assert.AreEqual(metaHash, BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path + ".meta"))).Replace("-", "").ToLowerInvariant());
            }
            Assert.IsNull(Resources.Load<Texture2D>(resource), "Archived source art must not be packaged or loaded at runtime.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AndroidLegacyArtCleanupTouchesOnlyKnownGeneratedFiles(bool legacyPresent)
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hcyp-staging-test-" + Guid.NewGuid().ToString("N"));
            string gradle = System.IO.Path.Combine(root, "Library/Bee/Android/Prj/IL2CPP/Gradle");
            string[] folders = {
                "unityLibrary/src/main/assets/bin/Data",
                "unityLibrary/build/intermediates/assets/debug/mergeDebugAssets/bin/Data",
                "launcher/build/intermediates/assets/debug/mergeDebugAssets/bin/Data"
            };
            string[] guids = { "ba31aede125064fd8857b2227d07992e", "00ac52b88645b4b9d943757fe4beefcf",
                "4d07362ad5b74f948a14cb751a35874c", "262c70b1a7b14d359409cefc28e1de95" };
            string source = System.IO.Path.Combine(root, "Assets", guids[0]);
            try
            {
                foreach (string folder in folders)
                {
                    string data = System.IO.Path.Combine(gradle, folder);
                    System.IO.Directory.CreateDirectory(data);
                    if (legacyPresent)
                        foreach (string guid in guids) System.IO.File.WriteAllText(System.IO.Path.Combine(data, guid), "obsolete copy");
                    System.IO.File.WriteAllText(System.IO.Path.Combine(data, "keep-live-texture"), "live");
                }
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(source));
                System.IO.File.WriteAllText(source, "original source");
                foreach (string variant in new[] { "Debug", "Release" })
                {
                    string cache = System.IO.Path.Combine(gradle, "launcher/build/intermediates/incremental/package" + variant);
                    string apkFolder = System.IO.Path.Combine(gradle, "launcher/build/outputs/apk/" + variant.ToLowerInvariant());
                    System.IO.Directory.CreateDirectory(cache);
                    System.IO.Directory.CreateDirectory(apkFolder);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(cache, "state"), "cache");
                    System.IO.File.WriteAllText(System.IO.Path.Combine(apkFolder, "launcher-" + variant.ToLowerInvariant() + ".apk"), "APK");
                }
                Type cleanup = Type.GetType("HayChoriYPaty.Editor.AndroidLegacyArtStagingCleanup, Assembly-CSharp-Editor", true);
                cleanup.GetMethod("RemoveArchivedCopies").Invoke(null, new object[] { root });
                foreach (string folder in folders)
                {
                    string data = System.IO.Path.Combine(gradle, folder);
                    foreach (string guid in guids) Assert.IsFalse(System.IO.File.Exists(System.IO.Path.Combine(data, guid)));
                    Assert.AreEqual("live", System.IO.File.ReadAllText(System.IO.Path.Combine(data, "keep-live-texture")));
                }
                foreach (string variant in new[] { "Debug", "Release" })
                {
                    string cache = System.IO.Path.Combine(gradle, "launcher/build/intermediates/incremental/package" + variant);
                    string apk = System.IO.Path.Combine(gradle, "launcher/build/outputs/apk/" + variant.ToLowerInvariant(), "launcher-" + variant.ToLowerInvariant() + ".apk");
                    Assert.AreEqual(!legacyPresent, System.IO.Directory.Exists(cache));
                    Assert.AreEqual(!legacyPresent, System.IO.File.Exists(apk));
                }
                Assert.AreEqual("original source", System.IO.File.ReadAllText(source));
            }
            finally { if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true); }
        }

        [Test]
        public void LiveTablesUseTheApprovedTextureAndNoArchivedFoodArtwork()
        {
            foreach (string station in new[] { "Station_NormalTable", "Station_PremiumTable" })
            {
                string path = "Assets/Resources/ModularKitchen/Prefabs/" + station + ".prefab";
                string[] dependencies = AssetDatabase.GetDependencies(path, true);
                CollectionAssert.Contains(dependencies, "Assets/Resources/ModularKitchen/Art/food-table-folding-v2.png");
                Assert.IsFalse(dependencies.Any(x => x.StartsWith("Assets/Art/Editor/LegacyKitchen/", StringComparison.Ordinal)));
                Assert.NotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/" + station));
            }
            Assert.NotNull(Resources.Load<Texture2D>("ModularKitchen/Art/empty-table"), "Fernet art remains unchanged.");
            Assert.IsNull(View.GetField("readySandwichesTable", BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNull(View.GetField("servingTable", BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNull(View.GetMethod("DrawServingTable", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        [TestCase(4)]
        [TestCase(6)]
        public void BarrelDrinkWalkingRootDoesNotReceivePresentationSpeedOffsets(int product)
        {
            Type simulationType = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Type balanceType = Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp", true);
            Type workerType = Type.GetType("HayChoriYPaty.StreetWorker, Assembly-CSharp", true);
            Type roleType = Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true);
            Type stateType = Type.GetType("HayChoriYPaty.StreetWorkerState, Assembly-CSharp", true);
            object simulation = Activator.CreateInstance(simulationType,
                new object[] { Activator.CreateInstance(balanceType), 3, 5f, 0, 1, 0 });
            object worker = Activator.CreateInstance(workerType);
            SetProjectionMember(worker, "Role", Enum.Parse(roleType, "Cocacolero"));
            SetProjectionMember(worker, "Product", product);
            object layout = simulationType.GetProperty("KitchenLayout").GetValue(simulation);
            Vector2 pickup = (Vector2)layout.GetType().GetMethod("PickupPosition", new[] { typeof(int) })
                .Invoke(layout, new object[] { product });
            Rect barrel = (Rect)layout.GetType().GetMethod("BoundsForProduct", new[] { typeof(int) })
                .Invoke(layout, new object[] { product });
            Assert.That(barrel.width, Is.GreaterThan(0f), "Both test product IDs must resolve to real barrel geometry.");
            float fullBodyLift = Mathf.Max(0f, barrel.height - 24f);
            Type projectionType = Type.GetType("HayChoriYPaty.StreetWorkerProjection, Assembly-CSharp", true);
            MethodInfo presentationPosition = projectionType.GetMethod("PresentationPosition", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo bodyLift = projectionType.GetMethod("BarrelDrinkPickupBodyLift", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(presentationPosition);
            Assert.NotNull(bodyLift);
            Func<Vector2> presented = () => (Vector2)presentationPosition.Invoke(null, new[] { simulation, worker });
            Func<float> lifted = () => (float)bodyLift.Invoke(null, new[] { simulation, worker });

            SetProjectionMember(worker, "State", Enum.Parse(stateType, "ToStation"));
            SetProjectionMember(worker, "Route", new[] { pickup });
            SetProjectionMember(worker, "RouteDistance", 96f);
            float[] remaining = { 96f, 48f, 36f, 24f, 12f, 0f };
            foreach (float distance in remaining)
            {
                Vector2 logical = pickup + Vector2.left * distance;
                SetProjectionMember(worker, "Position", logical);
                Assert.That(Vector2.Distance(presented(), logical), Is.LessThan(.01f),
                    "The rendered root must track the complete simulation position throughout the Coca/beer approach.");
                Assert.That(lifted(), Is.EqualTo(0f).Within(.01f), "No pickup-body lift may run while walking.");
            }

            SetProjectionMember(worker, "State", Enum.Parse(stateType, "ToCounter"));
            SetProjectionMember(worker, "Route", new[] { pickup + Vector2.right * 64f });
            SetProjectionMember(worker, "RouteDistance", 64f);
            float[] travelled = { 0f, 12f, 24f, 36f, 48f, 64f };
            foreach (float distance in travelled)
            {
                Vector2 logical = pickup + Vector2.right * distance;
                SetProjectionMember(worker, "Position", logical);
                Assert.That(Vector2.Distance(presented(), logical), Is.LessThan(.01f),
                    "The root must stay on the complete real departure path instead of adding/subtracting vertical travel.");
                Assert.That(lifted(), Is.EqualTo(0f).Within(.01f));
            }

            SetProjectionMember(worker, "State", Enum.Parse(stateType, "Pickup"));
            SetProjectionMember(worker, "Position", pickup);
            SetProjectionMember(worker, "Route", System.Array.Empty<Vector2>());
            foreach (float progress in new[] { 0f, .5f, 1f })
            {
                SetProjectionMember(worker, "PickupProgress", progress);
                Assert.That(Vector2.Distance(presented(), pickup), Is.LessThan(.01f),
                    "Even while reaching, the worker's ground/root stays planted at the pickup anchor.");
                Assert.That(lifted(), Is.EqualTo(fullBodyLift * Mathf.Sin(progress * Mathf.PI)).Within(.01f),
                    "Only the stationary reach pose moves the upper body; feet and gameplay position remain fixed.");
            }
        }

        private static void SetProjectionMember(object instance, string name, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = instance.GetType().GetProperty(name, flags);
            if (property != null)
            {
                property.GetSetMethod(true).Invoke(instance, new[] { value });
                return;
            }
            instance.GetType().GetField(name, flags).SetValue(instance, value);
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
            Assert.NotNull(Resources.Load<GameObject>("ModularKitchen/Prefabs/Station_NormalTable"), "The current modular table supplies the tabletop in every club.");

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
                "street-grill-four-zones-v1", "street-beer-barrel-v1", "street-fernet-table-v1", "street-coca-bottle-v1"
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
                Assert.IsTrue(android.overridden); Assert.AreEqual(TextureImporterFormat.ETC2_RGBA8, android.format);
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
