using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class LaRengaVisualTests
    {
        const BindingFlags S = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const BindingFlags I = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static Type T(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static object Get(object o, string name) => o.GetType().GetProperty(name, I).GetValue(o);
        static object Fans(int id) => Get(T("ClubVisualTheme").GetMethod("ForLevel", S).Invoke(null, new object[] {id}), "Fans");

        [Test]
        public void LaRengaOwnsItsPanoramicMuralCrestAndTwelvePairedDirectionalIdentities()
        {
            object theme = T("ClubVisualTheme").GetMethod("ForLevel", S).Invoke(null, new object[] {13});
            string[] names = (string[])T("StreetSimulation").GetField("LevelNames", S).GetValue(null);
            Assert.AreEqual(names[13], Get(theme, "ClubName"), "Theme keys must match the stable catalog exactly.");
            Assert.AreEqual("La Renga – Autódromo Bs. As.", Get(theme, "ClubName"));
            Assert.AreEqual("street-background-larenga-autodromo-v1", Get(theme, "MuralResource"));
            Texture2D mural = Resources.Load<Texture2D>((string)Get(theme, "MuralResource"));
            Assert.NotNull(mural); Assert.That(mural.width / (float)mural.height, Is.EqualTo(3f).Within(.02f));
            string[] crests = (string[])T("StreetView").GetField("LevelCrestResourceNames", S).GetValue(null);
            Assert.AreEqual(15, crests.Length); Assert.AreEqual("street-larenga-crest-v1", crests[13]);
            Assert.NotNull(Resources.Load<Texture2D>(crests[13]));
            object fans = Fans(13);
            Assert.AreEqual(12, Get(fans, "VariantCount")); Assert.AreEqual(2, Get(fans, "WalkingPoseCount"));
            Assert.IsTrue((bool)fans.GetType().GetMethod("IsComplete").Invoke(fans, new object[] {true}));
            foreach (string resource in new[] {(string)Get(fans, "FrontFans"), (string)Get(fans, "WalkingFans"), (string)Get(fans, "DownwardWalkingFans")})
            {
                StringAssert.StartsWith("street-larenga-", resource);
                Texture2D atlas = Resources.Load<Texture2D>(resource); Assert.NotNull(atlas);
                Assert.AreEqual(1536, atlas.width); Assert.AreEqual(1024, atlas.height);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas));
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha()); Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                var decoded = new Texture2D(2, 2);
                try
                {
                    decoded.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(atlas)));
                    Color32[] pixels = decoded.GetPixels32();
                    Assert.Greater(pixels.Count(p => p.a < 10) / (float)pixels.Length, .35f);
                    for (int id = 0; id < 12; id++)
                    {
                        Rect a = (Rect)fans.GetType().GetMethod("WalkingAnimationSourceRect").Invoke(fans, new object[] {1536, 1024, id, 0});
                        Rect b = (Rect)fans.GetType().GetMethod("WalkingAnimationSourceRect").Invoke(fans, new object[] {1536, 1024, id, 1});
                        Assert.AreEqual(256f, a.width); Assert.AreEqual(256f, a.height);
                        Assert.AreEqual(a.x + 256f, b.x); Assert.AreEqual(a.y, b.y);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(decoded); }
            }
            Assert.AreEqual("street-ferro-fans-walk-v2", Get(Fans(9), "WalkingFans"));
            Assert.AreEqual("street-losredondos-fans-walk-v1", Get(Fans(10), "WalkingFans"));
        }

        [Test]
        public void LaRengaRendererUsesRealLateralDownwardAndReceivingPosesWithoutForeignFallback()
        {
            var root = new GameObject("La Renga isolated atlas check");
            try
            {
                Type type = T("TowerDefenseWorldPresentation");
                Component renderer = root.AddComponent(type); type.GetField("visualLevelId", I).SetValue(renderer, 13);
                MethodInfo spriteMethod = type.GetMethod("FanSpriteForFacing", I);
                MethodInfo gait = type.GetMethod("FanWalkingFrameIndex", S);
                for (int id = 0; id < 12; id++)
                {
                    foreach (bool down in new[] {false, true})
                    {
                        Sprite a = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 0, down});
                        Sprite b = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 1, down});
                        Assert.AreEqual(down ? "street-larenga-fans-walk-down-v1" : "street-larenga-fans-walk-v1", a.texture.name);
                        Assert.AreSame(a.texture, b.texture); Assert.AreNotEqual(a.rect, b.rect);
                    }
                    Sprite receiving = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, true, 0, false});
                    Assert.AreEqual("street-larenga-fans-front-v1", receiving.texture.name);
                }
                Assert.AreEqual(0, gait.Invoke(null, new object[] {13, 0, 0f, .02f}));
                Assert.AreEqual(1, gait.Invoke(null, new object[] {13, 0, .361f, .02f}));
                Assert.AreEqual(0, gait.Invoke(null, new object[] {13, 0, .721f, .02f}));
                Assert.AreEqual(2, gait.Invoke(null, new object[] {9, 0, .721f, .02f}));
                Assert.AreEqual(1, gait.Invoke(null, new object[] {10, 0, .361f, .02f}), "Los Redondos alternates stride and passing poses.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // 2026-11-26: every Tower Defense level's walking fans alternate at least two leg poses while they advance.
        // Los Redondos (ID10) switches between its stride atlas and the derived passing atlas; Peregrinación (ID14)
        // alternates the two authored columns of its paired walk atlas although it has no downward atlas.
        [Test]
        public void LosRedondosAndPeregrinacionWalkingFansAlternateTwoDifferentLegPoses()
        {
            var root = new GameObject("TD gait atlas check");
            try
            {
                Type type = T("TowerDefenseWorldPresentation");
                MethodInfo gait = type.GetMethod("FanWalkingFrameIndex", S);
                MethodInfo spriteMethod = type.GetMethod("FanSpriteForFacing", I);
                Component renderer = root.AddComponent(type);

                type.GetField("visualLevelId", I).SetValue(renderer, 10);
                Texture2D stride = Resources.Load<Texture2D>("street-losredondos-fans-walk-v1");
                Texture2D passing = Resources.Load<Texture2D>("street-losredondos-fans-walk-passing-v1");
                Assert.NotNull(stride); Assert.NotNull(passing);
                Assert.AreEqual(stride.width, passing.width); Assert.AreEqual(stride.height, passing.height);
                type.GetField("losRedondosPassingWalkingFans", I).SetValue(renderer, passing);
                Assert.AreEqual(0, gait.Invoke(null, new object[] {10, 0, 0f, .01f}));
                Assert.AreEqual(1, gait.Invoke(null, new object[] {10, 0, .18f, .01f}));
                Assert.AreEqual(0, gait.Invoke(null, new object[] {10, 0, .36f, .01f}));
                Assert.AreEqual(1, gait.Invoke(null, new object[] {10, 1, 0f, .01f}), "Neighbouring fans start in opposite poses.");
                for (int id = 0; id < 12; id++)
                {
                    Sprite a = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 0, false});
                    Sprite b = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 1, true});
                    Assert.AreEqual("street-losredondos-fans-walk-v1", a.texture.name);
                    Assert.AreEqual("street-losredondos-fans-walk-passing-v1", b.texture.name);
                    Assert.AreEqual(a.rect, b.rect, "Both poses use the same identity cell.");
                }

                type.GetField("visualLevelId", I).SetValue(renderer, 14);
                Assert.AreEqual(0, gait.Invoke(null, new object[] {14, 0, 0f, .01f}));
                Assert.AreEqual(1, gait.Invoke(null, new object[] {14, 0, .18f, .01f}));
                for (int id = 0; id < 12; id++)
                {
                    Sprite a = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 0, false});
                    Sprite b = (Sprite)spriteMethod.Invoke(renderer, new object[] {id, false, 1, false});
                    Assert.AreEqual("street-peregrinacion-fans-walk-v1", a.texture.name);
                    Assert.AreSame(a.texture, b.texture);
                    Assert.AreNotEqual(a.rect, b.rect, "Peregrinación must alternate the two columns of its paired walk atlas.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // 2026-11-23: the "TECH SEGURIDAD" guard that patrols La Renga's top sidewalk (decorative; TowerDefenseWorldPresentation.SecurityGuard).
        [Test]
        public void SecurityGuardAtlasHasTheFourSupporterBasedFramesAndTransparentCorners()
        {
            Texture2D atlas = Resources.Load<Texture2D>("street-security-guard-v1");
            Assert.NotNull(atlas);
            Assert.AreEqual(1024, atlas.width); Assert.AreEqual(256, atlas.height); // 1 x 4 cells of 256 px: walk A, walk B, front A, front B (the bald supporter in uniform)
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas));
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            // Reading pixels needs a readable copy: render through a temporary RenderTexture.
            var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var readable = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(atlas, target); RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0); readable.Apply();
                for (int cell = 0; cell < 4; cell++)
                {
                    int x = cell * 256, y = 0;
                    Assert.AreEqual(0f, readable.GetPixel(x + 2, y + 2).a, .01f, "cell " + cell + " must keep a transparent corner");
                    int opaque = 0;
                    for (int px = 0; px < 256; px += 8) for (int py = 0; py < 256; py += 8) if (readable.GetPixel(x + px, y + py).a > .9f) opaque++;
                    Assert.Greater(opaque, 150, "cell " + cell + " must contain the guard");
                }
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(readable); }
        }
    }
}
