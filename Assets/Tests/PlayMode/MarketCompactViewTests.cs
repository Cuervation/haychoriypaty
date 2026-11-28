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
    public sealed class MarketCompactViewTests
    {
        const BindingFlags I = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags S = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static readonly Type GameType = Runtime("StreetGame");
        static readonly Type ViewType = Runtime("StreetView");
        static readonly FieldInfo Gate = GameType.GetField("AllowEditorTestInitialization", S);
        static readonly int[] Order = (int[])Runtime("StreetSimulation").GetField("CampaignOrder", S).GetValue(null);

        Scene scene;
        Component game, view;
        PortraitGameView portrait;
        bool gate, background;
        float time;
        string progress;
        object Market => Get(game, "Market");
        float Fit => (float)Get(view, "MarketFitScale");
        static int Action(string name) => (int)ViewType.GetField(name, S).GetRawConstantValue();
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethods(I).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);
        static object RectAt(Type type, string name, params object[] args) =>
            type.GetMethods(S).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(null, args);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, I).SetValue(target, value);
        static bool Persist<T>(string snapshot, T match) => true;

        [UnitySetUp]
        public IEnumerator Mount()
        {
            progress = PlayerPrefs.GetString(ProgressKey, null);
            gate = (bool)Gate.GetValue(null); background = Application.runInBackground; time = Time.timeScale;
            Gate.SetValue(null, false);
            Application.runInBackground = true;
            Time.timeScale = 0f;
            Assert.IsNull(Runtime("Cloud.FirebasePlayerSession").GetProperty("Instance", S).GetValue(null));
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByName("Main");
            game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(GameType, true)).Single();
            view = game.GetComponent(ViewType);
            var match = Runtime("StreetMatchRecord");
            var callback = GetType().GetMethod(nameof(Persist), S).MakeGenericMethod(match)
                .CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(string), match, typeof(bool)));
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", null, callback));
            float end = Time.realtimeSinceStartup + 85f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady"));
            foreach (string flag in new[] { "introActive", "hubActive", "levelSelectActive", "rankingActive" })
                Set(view, flag, false);
            var ids = (bool[])Get(Get(game, "save"), "unlockedLevelIds");
            for (int index = 0; index < ids.Length; index++) ids[index] = true;
            Set(Get(game, "save"), "unlockedLevel", 10);
        }

        [UnityTearDown]
        public IEnumerator Clean()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            portrait?.End();
            Gate.SetValue(null, gate);
            Application.runInBackground = background; Time.timeScale = time;
            Assert.AreEqual(progress, PlayerPrefs.GetString(ProgressKey, null));
        }

        IEnumerator Size(int width, int height)
        {
            portrait?.End();
            portrait = new PortraitGameView(width, height);
            portrait.Begin();
            yield return portrait.WaitUntilApplied(9f);
            Assert.AreEqual(new Vector2Int(width, height), new Vector2Int(Screen.width, Screen.height));
        }

        IEnumerator OpenMarket(int level)
        {
            Assert.IsTrue((bool)Call(game, "SelectLevel", level));
            var data = Get(Market, "Data");
            var stock = (int[])Get(data, "stockUnits");
            Array.Clear(stock, 0, stock.Length);
            Set(data, "balanceCents", 10000000L);
            Set(view, "marketActive", true);
            Set(view, "marketTab", 0);
            Set(view, "marketScroll", 0f);
            Set(view, "marketExpandedIngredient", -1);
            Set(view, "marketOtherExpanded", false);
            Set(view, "marketMessage", "");
            Set(view, "marketLastPurchaseTime", -10f);
            yield return null; yield return null;
        }

        Vector2 ToPixel(Vector2 local)
        {
            float rawScale = Screen.width / 540f;
            return new Vector2(((float)Get(view, "MarketFitLeft") + local.x * Fit) * rawScale,
                Screen.height - local.y * Fit * rawScale);
        }

        void Tap(Rect local)
        {
            Vector2 point = ToPixel(local.center);
            Call(view, "HandlePointer", point, true, false);
            Call(view, "HandlePointer", point, false, true);
        }

        IEnumerator Capture(string folder, string name)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(screenshot);
                Assert.AreEqual(new Vector2Int(Screen.width, Screen.height), new Vector2Int(screenshot.width, screenshot.height));
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/QA-Responsive-3", folder));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), screenshot.EncodeToJPG(94));
            }
            finally { UnityEngine.Object.Destroy(screenshot); }
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator CompactMarketPurchasesThroughVisibleRowAndKeepsTabsAndFooterReachable()
        {
            foreach (var (width, height, level) in new[] { (768, 1024, 3), (768, 1024, 8), (600, 720, 8), (600, 600, 8) })
            {
                string key = width + "x" + height + "-id" + level;
                yield return Size(width, height);
                yield return OpenMarket(level);
                Assert.Less(Fit, 1f, key);
                Rect body = (Rect)Call(view, "MarketLocalBodyBounds");
                Rect footer = (Rect)RectAt(ViewType, "MarketEconomyFooterBounds", Get(view, "MarketContentCanvasHeight"));
                Rect row = (Rect)Call(view, "MarketRowBounds", 0);
                Assert.Less(body.yMax, footer.yMin, key);
                Assert.GreaterOrEqual(row.yMin, body.yMin, key);
                Assert.LessOrEqual(row.yMax, body.yMax, key);
                int id = (int)Call(view, "MarketExpandedId");
                long balance = (long)Get(Market, "BalanceCents");
                int stock = (int)Call(Market, "Units", id);
                int[] pending = (int[])Get(view, "marketPendingPacks");
                Array.Clear(pending, 0, pending.Length);
                Assert.IsFalse((bool)Get(game, "CanStartRound"));
                Rect plus = (Rect)RectAt(ViewType, "MarketPurchaseControlBounds", row, Action("MarketPlusAction"));
                Rect buy = (Rect)RectAt(ViewType, "MarketPurchaseControlBounds", row, Action("MarketBuyAction"));
                Tap(plus);
                Assert.AreEqual(1, pending[id], key);
                yield return Capture("compact", key + "-ingredientes");
                Tap(buy);
                Assert.AreEqual(balance - Convert.ToInt64(Call(Market, "PackPriceCents", id)), Get(Market, "BalanceCents"), key);
                Assert.Greater((int)Call(Market, "Units", id), stock, key);
                Assert.AreEqual(0, pending[id], key);
                Assert.IsTrue((bool)Get(view, "marketActive"), key);

                Tap((Rect)Call(view, "MarketTabBounds", 1));
                Assert.AreEqual(1, Get(view, "marketTab"), key);
                Tap((Rect)Call(view, "MarketTabBounds", 2));
                Assert.AreEqual(2, Get(view, "marketTab"), key);
                Tap((Rect)Call(view, "MarketTabBounds", 0));
                Assert.AreEqual(0, Get(view, "marketTab"), key);

                Vector2 center = ToPixel(body.center);
                Call(view, "HandleMarketTouchScroll", center, true, true, false);
                Call(view, "HandleMarketTouchScroll", center + Vector2.up * 65f, false, true, false);
                Call(view, "HandleMarketTouchScroll", center + Vector2.up * 65f, false, false, true);
                Assert.Greater((float)Get(view, "marketScroll"), 0f, key);
                yield return new WaitForSecondsRealtime(.27f); // Existing purchase debounce.
                Rect buyMissing = (Rect)RectAt(ViewType, "MarketBuyMissingBounds", Get(view, "MarketContentCanvasHeight"));
                Tap(buyMissing);
                Assert.IsTrue((bool)Get(game, "CanStartRound"), key + ": fixed footer buys the remaining stock");
                Debug.Log("QA3 market compact: " + key + "; purchase, tabs, scroll, buy-missing");
            }
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator MarketLayoutUnchangedAtTheNineProtectedSizes()
        {
            foreach (var (width, height) in new[] { (320, 568), (360, 640), (360, 720), (390, 844), (360, 800),
                (610, 1356), (360, 840), (360, 864), (1080, 1920) })
            {
                yield return Size(width, height);
                foreach (int level in new[] { 3, 8 })
                {
                    yield return OpenMarket(level);
                    Assert.AreEqual(1f, Fit);
                    Rect body = (Rect)Call(view, "MarketBodyBounds");
                    float originalHeight = (float)Get(view, "logicalCanvasHeight");
                    Assert.AreEqual(Mathf.Max(140f, originalHeight - 184f - body.y), body.height, .01f);
                    var name = width + "x" + height + "-p" + (Array.IndexOf(Order, level) + 1).ToString("00") + "-id" + level + "-market";
                    yield return Capture("protected", name);
                }
            }
        }
    }
}
#endif
