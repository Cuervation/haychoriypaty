#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class AuditSelectorAndSaleViewTests
    {
        const BindingFlags I = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags S = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        const float CanvasWidth = 540f;
        static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static readonly Type GameType = Runtime("StreetGame");
        static readonly Type ViewType = Runtime("StreetView");
        static readonly FieldInfo Gate = GameType.GetField("AllowEditorTestInitialization", S);
        static readonly int[] Order = (int[])Runtime("StreetSimulation").GetField("CampaignOrder", S).GetValue(null);

        Scene scene;
        Component game, view;
        PortraitGameView portrait;
        bool previousGate, previousBackground;
        float previousTime;
        string progress;
        bool rejectNextAttempt;
        int rejected, successfulWrites;
        object Sim => Get(game, "Sim");
        object Save => Get(game, "save");
        object Market => Get(game, "Market");
        object MarketData => Get(Market, "Data");
        long Attempts => Convert.ToInt64(Get(Get(Save, "statistics"), "attemptsStarted"));
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethods(I).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
        static object ViewStatic(string name, params object[] args) =>
            ViewType.GetMethods(S).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(null, args);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, I).SetValue(target, value);
        bool Persist<T>(string snapshot, T match)
        {
            if (rejectNextAttempt && snapshot.Contains("\"attemptActive\":true"))
            {
                rejectNextAttempt = false;
                rejected++;
                return false;
            }
            successfulWrites++;
            return true;
        }

        [UnitySetUp]
        public IEnumerator Mount()
        {
            progress = PlayerPrefs.GetString(ProgressKey, null);
            previousGate = (bool)Gate.GetValue(null); previousBackground = Application.runInBackground; previousTime = Time.timeScale;
            Gate.SetValue(null, false); Application.runInBackground = true; Time.timeScale = 0f;
            Assert.IsNull(Runtime("Cloud.FirebasePlayerSession").GetProperty("Instance", S).GetValue(null));
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByName("Main");
            game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(GameType, true)).Single();
            view = game.GetComponent(ViewType);
            var match = Runtime("StreetMatchRecord");
            var callback = GetType().GetMethod(nameof(Persist), I).MakeGenericMethod(match)
                .CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(string), match, typeof(bool)), this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", null, callback));
            float end = Time.realtimeSinceStartup + 85f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady"));
            foreach (string flag in new[] { "introActive", "hubActive", "levelSelectActive", "marketActive", "rankingActive" })
                Set(view, flag, false);
            var ids = (bool[])Get(Save, "unlockedLevelIds");
            for (int index = 0; index < ids.Length; index++) ids[index] = true;
        }

        [UnityTearDown]
        public IEnumerator Clean()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            portrait?.End();
            Gate.SetValue(null, previousGate); Application.runInBackground = previousBackground; Time.timeScale = previousTime;
            Assert.AreEqual(progress, PlayerPrefs.GetString(ProgressKey, null));
        }

        IEnumerator Size(int width, int height)
        {
            portrait?.End();
            portrait = new PortraitGameView(width, height);
            portrait.Begin();
            yield return portrait.WaitUntilApplied(9f);
            Assert.AreEqual(new Vector2Int(width, height), new Vector2Int(Screen.width, Screen.height));
            yield return null;
        }

        void Tap(Vector2 local)
        {
            Rect viewport = (Rect)ViewStatic("CanvasViewport", new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float scale = viewport.width / CanvasWidth;
            var pixel = new Vector2(viewport.x + local.x * scale, Screen.height - viewport.y - local.y * scale);
            Call(view, "HandlePointer", pixel, true, false);
            Call(view, "HandlePointer", pixel, false, true);
        }

        IEnumerator WaitForShutter()
        {
            Assert.IsTrue((bool)Get(view, "RewardShutterActive"), "The visible selector card must start the real shutter.");
            float end = Time.realtimeSinceStartup + 8f;
            while ((bool)Get(view, "RewardShutterActive") && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse((bool)Get(view, "RewardShutterActive"));
            yield return null;
        }

        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(image);
                Assert.AreEqual(new Vector2Int(Screen.width, Screen.height), new Vector2Int(image.width, image.height));
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/QA-Selector-And-Sale/frames"));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), image.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(image); }
        }

        [UnityTest, Timeout(210000)]
        public IEnumerator CamionerosShowsRejectedAttemptSaveOnSelectorAndRetriesByTouch()
        {
            foreach (var (width, height) in new[] { (540, 960), (610, 1356), (600, 600) })
            {
                yield return Size(width, height);
                string key = width + "x" + height;
                Set(view, "levelSelectActive", true);
                Set(view, "levelSelectScroll", 0f);
                Rect card = (Rect)ViewStatic("LevelSelectCardBounds", Array.IndexOf(Order, 9));
                var stock = (int[])((int[])Get(MarketData, "stockUnits")).Clone();
                long balance = (long)Get(Market, "BalanceCents"), attempts = Attempts;
                int rejects = rejected, writes = successfulWrites;
                rejectNextAttempt = true;

                Tap(new Vector2(card.center.x, card.yMin + 42f));
                yield return WaitForShutter();
                Assert.AreEqual(rejects + 1, rejected, key);
                Assert.AreEqual(writes + 1, successfulWrites, "Level selection must save successfully first.");
                Assert.AreEqual(9, Get(game, "SelectedLevel"), key);
                Assert.AreEqual("Ready", Get(Sim, "Phase").ToString(), key);
                Assert.AreEqual(true, Get(view, "levelSelectActive"), key);
                Assert.AreEqual(false, Get(view, "towerDefenseActive"), key);
                Assert.IsNull(Get(view, "towerDefenseMode"), key);
                Assert.IsFalse((bool)Get(MarketData, "attemptActive"), key);
                Assert.IsTrue(string.IsNullOrEmpty((string)Get(Save, "activeAttemptId")), key);
                Assert.AreEqual(attempts, Attempts, key);
                Assert.AreEqual(balance, Get(Market, "BalanceCents"), key);
                CollectionAssert.AreEqual(stock, (int[])Get(MarketData, "stockUnits"), key);
                Assert.AreEqual("No se pudo guardar el inicio. Volvé a intentar.", Get(view, "levelSelectorStartError"), key);
                yield return Capture(key + "-camioneros-fallo-visible");

                Tap(new Vector2(card.center.x, card.yMin + 42f));
                yield return WaitForShutter();
                Assert.AreEqual("Playing", Get(Sim, "Phase").ToString(), key);
                Assert.AreEqual(false, Get(view, "levelSelectActive"), key);
                Assert.AreEqual(true, Get(view, "towerDefenseActive"), key);
                Assert.IsTrue(string.IsNullOrEmpty((string)Get(view, "levelSelectorStartError")), key);
                Assert.AreEqual(attempts + 1, Attempts, key);
                Assert.AreEqual(balance, Get(Market, "BalanceCents"), key);
                yield return Capture(key + "-camioneros-reintento");
                Assert.IsTrue((bool)Call(game, "ExitTowerDefenseRound", Get(view, "towerDefenseMode")), key);
                Call(view, "EndTowerDefenseRun");
            }
        }

        [UnityTest, Timeout(210000)]
        public IEnumerator NaturalSaleStaysAwayFromPendingOrdersAtTraditionalSizes()
        {
            foreach (var (width, height, level) in new[] { (540, 960, 0), (610, 1356, 2), (600, 600, 0) })
            {
                yield return Size(width, height);
                string key = width + "x" + height + "-id" + level;
                Assert.IsTrue((bool)Call(game, "SelectLevel", level), key);
                Set(MarketData, "balanceCents", 10000000L);
                if (!(bool)Get(game, "CanStartRound")) Assert.IsTrue((bool)Call(game, "TryBuyMissingIngredients"), key);
                Assert.IsTrue((bool)Call(game, "TryStartRound"), key);
                yield return null;
                float vertical = (float)Get(view, "layoutVerticalScale");
                float top = (float)Get(view, "projectedCounterTopY");
                float canvasHeight = (float)Get(view, "logicalCanvasHeight");
                Rect viewport = (Rect)ViewStatic("CanvasViewport", new Vector2(width, height), Screen.safeArea);
                float clearTop = (float)Call(view, "SaleFeedbackTop", viewport);
                var step = Sim.GetType().GetMethod("Step", I);
                object simulation = Sim;
                bool found = false;
                bool expectOldCollision = width != height; // The square case guards against a new overlap.
                for (int i = 0; i < 1100 && !found; i++)
                {
                    step.Invoke(simulation, new object[] { .05f });
                    // The old trajectory actually touches a still-visible order in this captured frame.
                    var customers = ((IEnumerable)Get(simulation, "Customers")).Cast<object>().Where(c =>
                    {
                        var position = (Vector2)Get(c, "Position");
                        string state = Get(c, "State").ToString();
                        return (int)Get(c, "PendingOrderLineCount") > 0 && state != "Entering" && state != "Leaving" &&
                            (bool)ViewStatic("ShouldDrawOrderBubble", Mathf.Clamp(
                                Mathf.RoundToInt(((float)Runtime("StreetSimulation").GetField("FrontQueueY", S).GetRawConstantValue() - position.y) /
                                    (float)Runtime("StreetSimulation").GetField("QueueRowSpacing", S).GetRawConstantValue()), 0, 2));
                    }).ToArray();
                    if (customers.Length < 5) continue;
                    var sales = ((IEnumerable)Get(simulation, "Sales")).Cast<object>().Where(s => (float)Get(s, "Age") < 1.45f).ToArray();
                    if (sales.Length == 0) continue;
                    var bubbles = customers.Select(c =>
                    {
                        Vector2 p = (Vector2)Call(view, "CustomerViewPosition", Get(c, "Position"));
                        Rect b = (Rect)ViewStatic("OrderBubbleBounds", p, Get(c, "PendingOrderLineCount"), 24f);
                        return (Rect)ViewStatic("OrderBubbleLayoutBounds", b, vertical);
                    }).ToArray();
                    foreach (object sale in sales)
                    {
                        Vector2 p = (Vector2)Get(sale, "Position");
                        float age = (float)Get(sale, "Age");
                        var old = new Rect(p.x - 27f, (p.y - 86f - age * 33f) * vertical, 72f, 25f);
                        found |= !expectOldCollision || bubbles.Any(b => b.Overlaps(old));
                        Rect marker = (Rect)ViewStatic("SaleFeedbackBounds", p, age, clearTop, vertical);
                        var finalVisible = (Rect)ViewStatic("SaleFeedbackBounds", p, 2.19f, clearTop, vertical);
                        Assert.Greater(marker.yMin, top, key);
                        Assert.Greater(finalVisible.yMin, clearTop, key);
                        if (width == height) Assert.GreaterOrEqual(finalVisible.yMin, top + 30f, key + ": compact HUD uses the counter face.");
                        Assert.LessOrEqual(marker.yMax, canvasHeight, key);
                        Assert.GreaterOrEqual(marker.xMin, 0f, key);
                        Assert.LessOrEqual(marker.xMax, CanvasWidth, key);
                        Assert.IsFalse(bubbles.Any(b => b.Overlaps(marker)), key);
                    }
                }
                Assert.IsTrue(found, key + ": expected a real sale with active orders" +
                    (expectOldCollision ? " and a collision in the old positions." : "."));
                Assert.Greater((int)Get(simulation, "Delivered"), 0, key);
                Assert.Greater((int)Get(simulation, "CoinsEarned"), 0, key);
                yield return Capture(key + "-pedidos-y-moneda");
                Assert.IsTrue((bool)Call(game, "ReturnToLevels"), key);
            }
        }
    }
}
#endif
