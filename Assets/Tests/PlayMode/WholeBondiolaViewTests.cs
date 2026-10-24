using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class WholeBondiolaViewTests
    {
        const BindingFlags P = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Type T(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static object Get(object target, string property) => target.GetType().GetProperty(property, P).GetValue(target);
        static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, P).Invoke(target, args);
        GameObject root; Component game, view; string key, previous; bool hadSave;
#if UNITY_EDITOR
        UnityEditor.EditorWindow window; PropertyInfo selected; object group; int previousSize, addedSize;
        void SetPortrait()
        {
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            window = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            selected = window.GetType().GetProperty("selectedSizeIndex", P); previousSize = (int)selected.GetValue(window);
            Type registryType = editor.GetType("UnityEditor.GameViewSizes");
            object registry = registryType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
            object groupKind = registryType.GetProperty("currentGroupType", P).GetValue(registry);
            group = registryType.GetMethod("GetGroup", P).Invoke(registry, new[] { groupKind });
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"), new object[] {
                Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), 1080, 1920, "Temporary whole Bondiola QA" });
            group.GetType().GetMethod("AddCustomSize", P).Invoke(group, new[] { size });
            addedSize = (int)group.GetType().GetMethod("GetTotalCount", P).Invoke(group, null) - 1;
            selected.SetValue(window, addedSize); window.Repaint();
        }
#endif
        [UnitySetUp] public IEnumerator SetUp()
        {
            key = (string)T("StreetGame").GetField("ProgressKey").GetValue(null);
            hadSave = PlayerPrefs.HasKey(key); previous = hadSave ? PlayerPrefs.GetString(key) : null;
            object seed = Activator.CreateInstance(T("StreetGame").GetNestedType("SaveData", BindingFlags.Public));
            seed.GetType().GetField("version").SetValue(seed, 4); seed.GetType().GetField("unlockedLevel").SetValue(seed, 10);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(seed)); PlayerPrefs.Save();
#if UNITY_EDITOR
            SetPortrait();
#endif
            root = new GameObject("Whole Bondiola GameView QA");
            game = root.AddComponent(T("StreetGame")); view = root.AddComponent(T("StreetView"));
            yield return null;
            ((Behaviour)game).enabled = false; // Freeze domain updates; only render controlled test states.
            foreach (string field in new[] { "introActive", "hubActive", "marketActive", "levelSelectActive" })
                T("StreetView").GetField(field, P).SetValue(view, false);
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            yield return null;
            if (hadSave) PlayerPrefs.SetString(key, previous); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
#if UNITY_EDITOR
            if (group != null)
            {
                selected.SetValue(window, previousSize);
                group.GetType().GetMethod("RemoveCustomSize", P).Invoke(group, new object[] { addedSize });
            }
#endif
        }
        [UnityTest] public IEnumerator WholePremiumCutsFillBothHalvesAndUseRealCookingStates()
        {
            foreach (int level in new[] { 3, 5 })
            {
                object previousSim = Get(game, "Sim");
                PropertyInfo phase = previousSim.GetType().GetProperty("Phase", P);
                phase.SetValue(previousSim, Enum.Parse(phase.PropertyType, "Ready"));
                Assert.IsTrue((bool)Call(game, "SelectLevel", level));
                object sim = Get(game, "Sim"), kitchen = Get(sim, "Kitchen");
                IList units = (IList)Get(kitchen, "Units");
                ((IList)kitchen.GetType().GetField("units", P).GetValue(kitchen)).Clear();
                if (level == 3)
                {
                    string[] states = { "Raw", "Cooking", "Cooked", "Burned", "Raw", "Raw" };
                    MethodInfo add = kitchen.GetType().GetMethod("Add", P);
                    for (int slot = 0; slot < 6; slot++) add.Invoke(kitchen, new object[] {
                        2, Enum.Parse(T("StreetFoodState"), states[slot]), Enum.Parse(T("StreetFoodLocation"), "Grill"), slot });
                }
                int before = units.Count, coins = (int)Get(sim, "Coins");
                sim.GetType().GetProperty("Phase", P).SetValue(sim, Enum.Parse(sim.GetType().GetProperty("Phase", P).PropertyType, "Playing"));
                // Keep the controlled scene stable across complete render frames. Direct WaitForEndOfFrame
                // is rejected by UnityTest's batchmode coroutine wrapper; the previous completed frame is valid here.
                yield return null; yield return null; yield return null;
                Component renderer = root.GetComponent(T("StreetKitchenRenderer")); Assert.NotNull(renderer);
                Assert.IsTrue((bool)Get(renderer, "IsReady"));
                IDictionary food = (IDictionary)renderer.GetType().GetField("foodRenderers", P).GetValue(renderer);
                IDictionary ambient = (IDictionary)renderer.GetType().GetField("ambientGrillMeat", P).GetValue(renderer);
                int wholeCount = 0, vacioCount = 0;
                if (level == 3)
                {
                    foreach (object unit in units)
                    {
                        SpriteRenderer sr = (SpriteRenderer)food[(int)Get(unit, "Id")];
                        string state = Get(unit, "State").ToString();
                        string expected = state == "Burned" ? "Cooked" : state;
                        Assert.AreSame(Resources.Load<Sprite>("ModularKitchen/Art/meat-bondiola-whole-v1_" + expected), sr.sprite);
                        Assert.AreEqual(state == "Burned" ? new Color(.94f, .78f, .60f, 1f) : Color.white, sr.color);
                    }
                    foreach (SpriteRenderer sr in food.Values)
                    {
                        Assert.That(sr.sprite.name, Does.StartWith("meat-bondiola-whole-v1_")); wholeCount++;
                    }
                }
                else
                {
                    foreach (object item in ambient.Values)
                    {
                        int product = (int)item.GetType().GetField("Product", P).GetValue(item);
                        var go = (GameObject)item.GetType().GetField("Object", P).GetValue(item);
                        var sr = (SpriteRenderer)item.GetType().GetField("Renderer", P).GetValue(item);
                        Assert.IsTrue(go.activeSelf);
                        if (product == 2)
                        {
                            Assert.That(sr.sprite.name, Does.StartWith("meat-bondiola-whole-v1_")); wholeCount++;
                        }
                        else if (product == 3)
                        {
                            Assert.That(sr.sprite.name, Does.StartWith("meat-vacio-whole-v1_")); vacioCount++;
                            Vector2 source = sr.sprite.bounds.size;
                            float renderedAspect = source.x * go.transform.localScale.x / (source.y * go.transform.localScale.y);
                            Assert.AreEqual(3.4f, renderedAspect, .02f, "Shared-grill Vacío is widened horizontally to fill its four-piece layout.");
                        }
                    }
                }
                Assert.AreEqual(6, wholeCount, "All six whole Bondiola cooking positions render in both catalog layouts.");
                Assert.AreEqual(6, Call(kitchen, "GrillCapacity", 2));
                if (level == 5)
                {
                    Assert.AreEqual(4, vacioCount, "All four whole Vacío cooking positions fill the right half of the shared grill.");
                    Assert.AreEqual(4, Call(kitchen, "GrillCapacity", 3));
                    foreach (string state in new[] { "Raw", "Cooking", "Cooked" })
                        Assert.NotNull(Resources.Load<Sprite>("ModularKitchen/Art/meat-vacio-whole-v1_" + state));
                }
                Assert.AreEqual(before, units.Count, "Decorative filling cannot create inventory."); Assert.AreEqual(coins, Get(sim, "Coins"));
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                Assert.AreEqual(1080, capture.width); Assert.AreEqual(1920, capture.height);
                string evidenceDir = Path.Combine(Application.dataPath, "../Logs/Acceptance/VacioFillHalf-20261024");
                Directory.CreateDirectory(evidenceDir);
                string path = Path.Combine(evidenceDir, "premium-grill-level-" + (level + 1) + "-1080x1920.png");
                File.WriteAllBytes(path, capture.EncodeToPNG()); UnityEngine.Object.Destroy(capture);
                Debug.Log("Whole premium grill full GameView capture: " + path);
                // Native projection-only detail for visual comparison; the full GameView above is retained.
                RenderTexture target = (RenderTexture)Get(renderer, "BackgroundTexture");
                Rect grill = (Rect)Get(Get(sim, "KitchenLayout"), "PremiumGrillBounds");
                int x = Mathf.Max(0, Mathf.FloorToInt(grill.xMin * 2f) - 8);
                int y = Mathf.Max(0, Mathf.FloorToInt(target.height - grill.yMax * 2f) - 8);
                int width = Mathf.Min(target.width - x, Mathf.CeilToInt(grill.width * 2f) + 16);
                int height = Mathf.Min(target.height - y, Mathf.CeilToInt(grill.height * 2f) + 16);
                RenderTexture previousTarget = RenderTexture.active;
                var detail = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = target;
                    detail.ReadPixels(new Rect(x, y, width, height), 0, 0); detail.Apply();
                    string detailPath = Path.Combine(evidenceDir, "premium-grill-detail-level-" + (level + 1) + ".png");
                    File.WriteAllBytes(detailPath, detail.EncodeToPNG());
                    Debug.Log("Whole premium grill native camera detail: " + detailPath);
                }
                finally { RenderTexture.active = previousTarget; UnityEngine.Object.Destroy(detail); }

            }
        }
    }
}
