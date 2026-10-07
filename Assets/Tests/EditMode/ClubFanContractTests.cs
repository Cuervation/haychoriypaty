using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class ClubFanContractTests
    {
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object Get(object o, string name) => o.GetType().GetProperty(name).GetValue(o);
        private static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name).Invoke(o, args);

        [Test]
        public void EveryRegisteredLevelHasCompleteMatchedFrontWalkAndTwoRiotFrames()
        {
            string[] names = (string[])Runtime("StreetSimulation").GetField("LevelNames").GetValue(null);
            int[] expected = { 9, 8, 9, 12, 12, 12, 12, 12, 12, 12, 12 };
            Assert.AreEqual(expected.Length, names.Length, "New levels need an explicit expected wardrobe count and visual DoD review.");
            for (int level = 0; level < names.Length; level++)
            {
                object theme = Runtime("ClubVisualTheme").GetMethod("ForLevel").Invoke(null, new object[] { level });
                object fans = Get(theme, "Fans");
                Assert.NotNull(fans, names[level]);
                Assert.IsTrue((bool)Call(fans, "IsComplete", true), names[level]);
                Assert.AreEqual(expected[level], Get(fans, "VariantCount"), names[level]);
                Assert.AreEqual(2, Get(fans, "RiotPoses"), names[level]);
                Assert.AreNotEqual("street-riot-fans-v1", Get(fans, "RiotFans"), names[level]);
                Texture2D front = (Texture2D)Call(fans, "LoadFrontFans");
                Texture2D walk = (Texture2D)Call(fans, "LoadWalkingFans");
                Texture2D riot = (Texture2D)Call(fans, "LoadRiotFans");
                var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Street/Resources/" + Get(fans, "RiotFans") + ".png");
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, names[level] + ": do not stretch atlas aspect during import");
                var cells = new HashSet<Rect>();
                for (int id = 0; id < expected[level]; id++)
                {
                    Assert.AreEqual(id, Call(fans, "GetVariant", id + expected[level]));
                    Assert.AreEqual(id, Call(fans, "GetVariant", id - expected[level]));
                    Rect f = (Rect)Call(fans, "FrontSourceRect", front.width, front.height, id);
                    Rect w = (Rect)Call(fans, "WalkingSourceRect", walk.width, walk.height, id);
                    Rect a = (Rect)Call(fans, "RiotSourceRect", riot.width, riot.height, id, 0);
                    Rect b = (Rect)Call(fans, "RiotSourceRect", riot.width, riot.height, id, 1);
                    foreach (Rect r in new[] { f, w, a, b }) Assert.Greater(r.width * r.height, 0);
                    Assert.That(f.y / front.height, Is.EqualTo(w.y / walk.height).Within(.001f), names[level]);
                    Assert.AreEqual(a.y, b.y);
                    Assert.AreEqual(a.xMax, b.x);
                    Assert.IsTrue(cells.Add(a)); Assert.IsTrue(cells.Add(b));
                    Assert.That(a.xMin, Is.GreaterThanOrEqualTo(0));
                    Assert.That(b.xMax, Is.LessThanOrEqualTo(riot.width + .01f));
                    Assert.That(a.yMax, Is.LessThanOrEqualTo(riot.height + .01f));
                    Assert.AreEqual(a, Call(fans, "RiotSourceRect", riot.width, riot.height, id + expected[level], 0));
                }
            }
        }

        [Test]
        public void IncompleteGenericAndOnePoseDefinitionsCannotPassTheContract()
        {
            Type layoutType = Runtime("FanAtlasLayout"), kindType = Runtime("FanAtlasLayoutKind");
            object grid = Activator.CreateInstance(layoutType, 3, 4, Enum.Parse(kindType, "Grid"), false);
            object paired = Activator.CreateInstance(layoutType, 6, 4, Enum.Parse(kindType, "PairedColumns"), false);
            foreach (string riot in new[] { null, "", "street-riot-fans-v1" })
            {
                object d = Activator.CreateInstance(Runtime("ClubFanDefinition"), "front", "walk", riot, 12, 3, grid, grid, paired, 2, 3);
                Assert.IsFalse((bool)Call(d, "IsComplete", false));
            }
            foreach (object[] args in new[] {
                new object[] { "front", "walk", "riot", 13, 3, grid, grid, paired, 2, 3 },
                new object[] { "front", "walk", "riot", 12, 3, grid, grid, paired, 1, 3 },
                new object[] { "front", "walk", "riot", 12, 3, grid, grid, grid, 2, 3 }
            }) Assert.IsFalse((bool)Call(Activator.CreateInstance(Runtime("ClubFanDefinition"), args), "IsComplete", false));
        }

        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(10)]
        public void NewRiotBodyHeightAndShoesMatchTheSameNormalVariant(int level)
        {
            object theme = Runtime("ClubVisualTheme").GetMethod("ForLevel").Invoke(null, new object[] { level });
            object fans = Get(theme, "Fans");
            var front = new Texture2D(2, 2); var riot = new Texture2D(2, 2);
            try
            {
                front.LoadImage(System.IO.File.ReadAllBytes(Application.dataPath + "/Art/Street/Resources/" + Get(fans, "FrontFans") + ".png"));
                riot.LoadImage(System.IO.File.ReadAllBytes(Application.dataPath + "/Art/Street/Resources/" + Get(fans, "RiotFans") + ".png"));
                for (int id = 0; id < (int)Get(fans, "VariantCount"); id++)
                {
                    Rect f = (Rect)Call(fans, "FrontSourceRect", front.width, front.height, id);
                    Vector2 body = BodyAndFootPadding(front, f);
                    float normalScale = Mathf.Min(78f / f.width, 76f / f.height);
                    for (int pose = 0; pose < 2; pose++)
                    {
                        Rect r = (Rect)Call(fans, "RiotSourceRect", riot.width, riot.height, id, pose);
                        Vector2 riotBody = BodyAndFootPadding(riot, r);
                        Rect draw = (Rect)Call(fans, "RiotDrawBounds", new Rect(0, 0, 88, 88), id, pose);
                        float riotScale = Mathf.Min(draw.width / r.width, draw.height / r.height);
                        Assert.That(riotBody.x * riotScale, Is.EqualTo(body.x * normalScale).Within(.1f), "Body height, not stick or transparent cell height");
                        Assert.That(draw.yMax - riotBody.y * riotScale, Is.EqualTo(88f - body.y * normalScale).Within(.1f), "Same shoes baseline");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(front); UnityEngine.Object.DestroyImmediate(riot); }
        }

        private static Vector2 BodyAndFootPadding(Texture2D texture, Rect frame)
        {
            Color32[] pixels = texture.GetPixels32();
            int top = (int)frame.height, bottom = 0;
            for (int y = 0; y < (int)frame.height; y++)
            {
                int central = 0;
                for (int x = 0; x < (int)frame.width; x++)
                {
                    int ix = (int)frame.x + x, iy = texture.height - 1 - ((int)frame.y + y);
                    if (pixels[iy * texture.width + ix].a < 128) continue;
                    bottom = Mathf.Max(bottom, y);
                    if (x > frame.width * .4f && x < frame.width * .6f) central++;
                }
                // Ignore narrow raised sticks; the central head/body has a substantial opaque span.
                if (central > frame.width * .14f) top = Mathf.Min(top, y);
            }
            return new Vector2(bottom - top + 1, frame.height - 1 - bottom);
        }

        [TestCase("street-independiente-riot-fans-v3")]
        [TestCase("street-racing-riot-fans-v2")]
        [TestCase("street-sanlorenzo-riot-fans-v2")]
        [TestCase("street-river-riot-fans-v2")]
        [TestCase("street-boca-riot-fans-v2")]
        [TestCase("street-losredondos-riot-fans-v1")]
        public void NewRiotAtlasKeepsEveryPoseInsideItsOwnCell(string name)
        {
            var t = new Texture2D(2, 2);
            try
            {
                t.LoadImage(System.IO.File.ReadAllBytes(Application.dataPath + "/Art/Street/Resources/" + name + ".png"));
                Assert.AreEqual(1536, t.width); Assert.AreEqual(1024, t.height);
                var pixels = t.GetPixels32();
                int clear = 0;
                foreach (Color32 p in pixels) if (p.a < 10) clear++;
                Assert.Greater(clear / (float)pixels.Length, .4f);
                for (int y = 255; y < 1023; y += 256)
                    for (int x = 0; x < 1536; x++)
                    {
                        Assert.Less(pixels[y * 1536 + x].a, 128, name + " row boundary");
                        Assert.Less(pixels[(y + 1) * 1536 + x].a, 128, name + " row boundary");
                    }
                for (int x = 255; x < 1535; x += 256)
                    for (int y = 0; y < 1024; y++)
                        Assert.Less(pixels[y * 1536 + x].a, 128, name + " column boundary");
            }
            finally { UnityEngine.Object.DestroyImmediate(t); }
        }
    }
}
