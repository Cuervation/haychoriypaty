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
            Assert.AreEqual(14, crests.Length); Assert.AreEqual("street-larenga-crest-v1", crests[13]);
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
                Assert.AreEqual(0, gait.Invoke(null, new object[] {10, 0, .721f, .02f}));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
