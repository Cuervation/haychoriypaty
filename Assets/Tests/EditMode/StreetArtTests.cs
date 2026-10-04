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
            foreach (string path in new[] { SheetPath, "Assets/Art/Street/Resources/street-parrillero-icon.png" })
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
        }

        [Test]
        public void LargeStreetGrillPreservesAlphaAndOriginalResolution()
        {
            const string path = "Assets/Art/Street/Resources/street-parrilla-large.png";
            Texture2D texture = Resources.Load<Texture2D>("street-parrilla-large");
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
            Assert.NotNull(Resources.Load<Texture2D>("street-items"), "Keep original fallback and drinks");
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
            if (products <= 4) Assert.AreEqual(494f, r.width);
            else Assert.LessOrEqual(r.xMax, 305, "Leave beverage slots free");
            Rect speed = (Rect)View.GetField("Speed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Rect hire = (Rect)View.GetField("HireParrillero", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsFalse(r.Overlaps(speed)); Assert.IsFalse(r.Overlaps(hire));
            Type sim = Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);
            Vector2 anchor = (Vector2)sim.GetMethod("StationPosition").Invoke(null, new object[] { 0 });
            Assert.AreEqual(new Vector2(58, 578), anchor, "Visual-only change must keep pickup path");
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

