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
        private const string SheetPath = "Assets/Art/Street/Resources/street-parrillero.png";
        private const string DiagonalSheetPath = "Assets/Art/Street/Resources/street-parrillero-diagonal-v1.png";
        private static Type View { get { return Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true); } }

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
        public void ParrilleroWalkSelectionCyclesOppositeDiagonalAndCardinalStrides()
        {
            MethodInfo select = View.GetMethod("SelectParrilleroWalkFrame", BindingFlags.Static | BindingFlags.NonPublic);
            Vector2[] directions = {
                new Vector2(-100, 100), new Vector2(100, 100),
                new Vector2(-100, -100), new Vector2(100, -100)
            };
            for (int i = 0; i < directions.Length; i++)
            {
                Assert.AreEqual(16 + i * 2, (int)select.Invoke(null, new object[] { directions[i], 0f }));
                Assert.AreEqual(17 + i * 2, (int)select.Invoke(null, new object[] { directions[i], .13f }));
            }

            Assert.AreEqual(1, (int)select.Invoke(null, new object[] { Vector2.up, 0f }));
            Assert.AreEqual(2, (int)select.Invoke(null, new object[] { Vector2.up, .13f }));
            Assert.AreEqual(5, (int)select.Invoke(null, new object[] { Vector2.down, 0f }));
            Assert.AreEqual(6, (int)select.Invoke(null, new object[] { Vector2.down, .13f }));
            Assert.AreEqual(9, (int)select.Invoke(null, new object[] { Vector2.right, 0f }));
            Assert.AreEqual(10, (int)select.Invoke(null, new object[] { Vector2.right, .13f }));
            Assert.AreEqual(1, (int)select.Invoke(null, new object[] { new Vector2(20, 100), 0f }), "Near-vertical travel stays on the front walk pair");
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
            Rect[] cards = new Rect[5];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = (Rect)cardBounds.Invoke(null, new object[] { i });
                Assert.GreaterOrEqual(cards[i].xMin, 0f); Assert.LessOrEqual(cards[i].xMax, 540f);
                Assert.GreaterOrEqual(cards[i].yMin, 0f); Assert.LessOrEqual(cards[i].yMax, 960f);
                for (int j = 0; j < i; j++) Assert.IsFalse(cards[i].Overlaps(cards[j]), "Level cards must have separate touch targets");
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
        public void ChicagoLocationLoadsItsMuralCrestBottleAndBlueBarrelWithSidePickupLayout()
        {
            foreach (string name in new[] { "street-background-chicago-v1", "street-new-chicago-crest-v1", "street-coca-bottle-v1", "street-beverage-barrel-v1" })
                Assert.NotNull(Resources.Load<Texture2D>(name), "Missing Nueva Chicago art: " + name);
            Assert.AreEqual("street-new-chicago-crest-v1", View.GetField("ChicagoCrestResource", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue());
            Texture2D bottle = Resources.Load<Texture2D>("street-coca-bottle-v1");
            Texture2D barrel = Resources.Load<Texture2D>("street-beverage-barrel-v1");
            Assert.AreEqual(1024, bottle.width); Assert.AreEqual(1536, bottle.height);
            Assert.AreEqual(1024, barrel.width); Assert.AreEqual(1536, barrel.height);
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
            Assert.IsFalse(table.Overlaps(grill)); Assert.IsFalse(barrelBounds.Overlaps(grill));
            Assert.Less(table.xMax, grill.xMin); Assert.Greater(barrelBounds.xMin, grill.xMax);
            Type simulation = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Vector2 chori = (Vector2)simulation.GetMethod("StationPositionForLevel").Invoke(null, new object[] { 0, 1, 2 });
            Vector2 coca = (Vector2)simulation.GetMethod("StationPositionForLevel").Invoke(null, new object[] { 4, 1, 2 });
            Assert.IsTrue(table.Contains(chori)); Assert.IsTrue(barrelBounds.Contains(coca));
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
            Assert.LessOrEqual(r.yMax, 644, "Grill must stop before money/time HUD");
            if (products == 1) Assert.AreEqual(282f, r.width);
            else if (products <= 4) Assert.AreEqual(494f, r.width);
            else Assert.LessOrEqual(r.xMax, 305, "Leave beverage slots free");
            Rect speed = (Rect)View.GetField("Speed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect hire = (Rect)View.GetField("HireParrillero", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsFalse(r.Overlaps(speed)); Assert.IsFalse(r.Overlaps(hire));
            Type sim = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Vector2 anchor = (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 0 });
            Assert.AreEqual(new Vector2(205, 585), anchor, "Choripán pickups now happen beside the grill at the loaded trestle table");
            Assert.AreEqual(new Vector2(128, 578), (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 1 }),
                "Other unlocked food stations keep their original pickup positions");
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
            Assert.IsTrue(table.Contains(pickup));
            // Every possible counter destination and the initial spawn use this lane.
            for (int column = 0; column < 7; column++)
            for (int sample = 0; sample <= 100; sample++)
            {
                float t = sample / 100f;
                Assert.IsFalse(grill.Contains(Vector2.Lerp(new Vector2(58 + 70 * column, 400), approach, t)));
                Assert.IsFalse(grill.Contains(Vector2.Lerp(new Vector2(433, 430), approach, t)));
                Assert.IsFalse(grill.Contains(Vector2.Lerp(approach, pickup, t)));
            }
            Assert.AreEqual(new Vector2(220, 665), (Vector2)sim.GetMethod("StationPositionForProductCount")
                .Invoke(null, new object[] { 0, 3 }), "Later multi-product pickup positions remain unchanged");
        }

        [Test]
        public void EdgeHudFillsPhysicalScreenAndKeepsNumbersOutsideCenteredCameraGap()
        {
            Rect physical = (Rect)View.GetMethod("ScreenBounds", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Vector2(1220, 2712) });
            Assert.AreEqual(new Rect(0, 0, 1220, 2712), physical);
            Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(0f, bar.yMin);
            Rect gap = (Rect)View.GetField("CutoutHudGap", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (string name in new[] { "CutoutHudCoins", "CutoutHudTime", "CutoutHudSales" })
            {
                Rect field = (Rect)View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.IsFalse(field.Overlaps(gap));
                Assert.IsTrue(bar.Contains(field.min)); Assert.IsTrue(bar.Contains(field.max));
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

        [Test]
        public void UnifiedTopHudFitsAboveRearOrdersAndSeparatesCounters()
        {
            Rect bar = (Rect)View.GetField("HudBar", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.GreaterOrEqual(bar.xMin, 0); Assert.LessOrEqual(bar.xMax, 540);
            Assert.AreEqual(0f, bar.yMin); Assert.AreEqual(68f, bar.height, "Double the former 34-pixel HUD height");
            Rect[] counters = new[] { "HudCoins", "HudTime", "HudSales" }
                .Select(name => (Rect)View.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).ToArray();
            for (int i = 0; i < counters.Length; i++)
            {
                Assert.IsTrue(bar.Contains(counters[i].min));
                Assert.IsTrue(bar.Contains(counters[i].max));
                for (int j = i + 1; j < counters.Length; j++) Assert.IsFalse(counters[i].Overlaps(counters[j]));
            }
            Rect chori = (Rect)View.GetField("HudChoriSales", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect coca = (Rect)View.GetField("HudCocaSales", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsFalse(chori.Overlaps(coca), "Chicago goals have independent readable rows");
            foreach (Rect count in new[] { chori, coca })
            {
                Assert.IsTrue(bar.Contains(count.min)); Assert.IsTrue(bar.Contains(count.max));
                Assert.IsFalse(count.Overlaps(counters[0])); Assert.IsFalse(count.Overlaps(counters[1]));
            }
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
            Texture2D cutout = (Texture2D)View.GetMethod("CreateHudBarTexture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { true });
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
    }
}
