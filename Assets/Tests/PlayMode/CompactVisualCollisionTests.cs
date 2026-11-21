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
    public sealed class CompactVisualCollisionTests
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
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethods(I).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);
        static object Static(Type type, string name, params object[] args) =>
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

        IEnumerator PlayVenue(int level)
        {
            Assert.IsTrue((bool)Call(game, "SelectLevel", level));
            Set(Get(Market, "Data"), "balanceCents", 10000000L);
            if (!(bool)Get(game, "CanStartRound")) Assert.IsTrue((bool)Call(game, "TryBuyMissingIngredients"));
            Assert.IsTrue((bool)Call(game, "TryStartRound"));
            object sim = Get(game, "Sim");
            Type role = Runtime("StreetWorkerRole");
            for (int tick = 0; tick < 136 && Get(sim, "Phase").ToString() == "Playing"; tick++)
            {
                if (tick % 6 == 0)
                {
                    for (int r = 0; r < 5; r++)
                    {
                        object rr = Enum.ToObject(role, r);
                        if ((int)Call(sim, "WorkerCount", rr) < 3) Call(game, "TryHire", rr);
                    }
                    Call(game, "TryUpgradeSpeed");
                }
                Call(sim, "Step", .25f);
            }
            yield return null; yield return null; yield return null;
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(screenshot);
                Assert.AreEqual(new Vector2Int(Screen.width, Screen.height), new Vector2Int(screenshot.width, screenshot.height));
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/QA-Responsive-456/frames"));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), screenshot.EncodeToJPG(94));
            }
            finally { UnityEngine.Object.Destroy(screenshot); }
        }

        void AssertOrdersClear(bool compact, string key)
        {
            object sim = Get(game, "Sim");
            int count = (int)Get(sim, "ProductCount");
            var products = Enumerable.Range(0, count).Select(slot => (int)Call(sim, "GetAvailableProduct", slot)).ToArray();
            object[] hudLayoutArguments = { Rect.zero, products, count, Rect.zero };
            Rect band = (Rect)Static(ViewType, "CompactHudQuotaBand", hudLayoutArguments);
            Rect status = (Rect)hudLayoutArguments[3];
            Assert.AreEqual(compact, band.width > 0f, key);
            float vertical = (float)Get(view, "layoutVerticalScale");
            float counter = (float)Call(view, "CustomerCounterTopY");
            if (compact)
            {
                Vector2 advancing = (Vector2)Call(view, "CustomerViewPosition", new Vector2(80f, 324f - 1.5f * 56f + .01f));
                Rect rawLimit = (Rect)Static(ViewType, "OrderBubbleBounds", advancing, 3, 28f);
                Rect first = (Rect)Static(ViewType, "OrderBubbleLayoutBounds", rawLimit, vertical);
                Assert.That(band.yMax <= first.yMin || band.yMin >= counter, key + ": space even while the queue advances");
            }
            int visible = 0;
            foreach (object customer in (IEnumerable)Get(sim, "Customers"))
            {
                string state = Get(customer, "State").ToString();
                if (state == "Entering" || state == "Leaving") continue;
                Vector2 position = (Vector2)Get(customer, "Position");
                int row = Mathf.Max(0, Mathf.RoundToInt((324f - position.y) / 56f));
                int lines = (int)Get(customer, "PendingOrderLineCount");
                if (row >= 2 || lines <= 0) continue;
                Rect raw = (Rect)Static(ViewType, "OrderBubbleBounds", Call(view, "CustomerViewPosition", position), lines, 28f);
                Rect drawn = (Rect)Static(ViewType, "OrderBubbleLayoutBounds", raw, vertical);
                drawn.yMax = Mathf.Min(drawn.yMax, counter);
                visible++;
                if (status.width > 0f) Assert.IsFalse(status.Overlaps(drawn), key + ": compact status");
                for (int slot = 0; slot < count; slot++)
                {
                    Rect chip = (Rect)Static(ViewType, "HudQuotaDisplayBounds", slot, products, count, Rect.zero, band);
                    Assert.IsTrue(chip.width > 0 && chip.height > 0, key);
                    Assert.IsFalse(chip.Overlaps(drawn), key + " bubble=" + drawn + "; quota=" + chip);
                }
            }
            Assert.Greater(visible, 0, key);
        }

        void AssertClubCrestClear(int level, bool protectedSize, string key)
        {
            var sim = Get(game, "Sim");
            var footer = Call(view, "CurrentResponsiveFooter", sim, (float)Get(view, "logicalCanvasHeight"), 0f);
            Rect badge = (Rect)Get(footer, "LevelBadge");
            var crest = (Texture2D)Call(view, "LevelCrestForLevel", level);
            Assert.IsNotNull(crest);
            GUIStyle title = (GUIStyle)Get(view, "levelTitle");
            string[] names = (string[])Runtime("StreetSimulation").GetField("LevelDisplayNames", S).GetValue(null);
            float width = ((Rect)Call(view, "LevelCrestSizedSlot", new Rect(0, 0, 32f, 32f), crest)).width;
            float textWidth = title.CalcSize(new GUIContent(names[level].ToUpperInvariant())).x + 8f;
            Rect[] layout = (Rect[])Static(ViewType, "LevelBadgeLayoutForWidth", badge, textWidth, width);
            Rect old = (Rect)Call(view, "LayoutRect", layout[0]);
            Rect slot = (Rect)Call(view, "LevelBadgeCrestDisplaySlot", level, old, crest);
            if (protectedSize)
            {
                Assert.AreEqual(old, slot, key); // Preserve the existing composition, including sub-pixel edge rounding.
                return;
            }
            var visible = (Rect)Call(view, "LevelCrestVisibleBounds", slot, crest, Rect.zero);
            foreach (Rect card in (Rect[])Get(footer, "UpgradeBounds"))
            {
                Rect drawn = (Rect)Call(view, "LayoutRect",
                    Static(ViewType, "ScaleCardBounds", card, (float)Get(view, "layoutVerticalScale")));
                Assert.IsFalse(visible.Overlaps(drawn), key + ": crest=" + visible + "; card=" + drawn);
            }
        }

        IEnumerator OpenChulengoAndAssert(bool shortHeight, string key)
        {
            object sim = Get(game, "Sim");
            Set(sim, "<Phase>k__BackingField", Enum.Parse(Get(sim, "Phase").GetType(), "Won"));
            yield return null;
            float until = Time.realtimeSinceStartup + 5f;
            while ((bool)Get(view, "VictoryTransitionActive") && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue((bool)Get(game, "HasPendingChulengo"), key);
            float h = (float)Get(view, "logicalCanvasHeight");
            Rect frame = (Rect)Static(ViewType, "VictoryPopupBounds", h, true);
            int preferred = Mathf.RoundToInt(20f * frame.width / 516f);
            bool compact = h < 958f && preferred < 12;
            if (shortHeight) Assert.IsTrue(compact, key);
            if (h >= 958f) Assert.IsFalse(compact, key);
            Rect label = (Rect)Static(ViewType, "ChulengoPrizeCaptionBounds", frame, compact);
            var style = new GUIStyle((GUIStyle)Get(view, "menuTitle"));
            style.fontSize = (int)Static(ViewType, "FitVictoryFontWithMinimum", style, "PREMIO DEL TURNO", preferred, label, compact ? 1 : 12);
            if (shortHeight) Assert.GreaterOrEqual(style.fontSize, 8, key + ": visible caption size");
            Vector2 needed = style.CalcSize(new GUIContent("PREMIO DEL TURNO"));
            Assert.LessOrEqual(needed.x, label.width, key + ": measured width at actual font size");
            Assert.LessOrEqual(needed.y, label.height, key + ": measured height at actual font size");
            Rect collect = (Rect)Call(view, "ChulengoCollectBounds");
            Rect multiply = (Rect)Call(view, "ChulengoMultiplyBounds");
            Assert.That(collect.yMax <= h && multiply.yMax <= h && !collect.Overlaps(multiply), key);
            yield return Capture(key + "-chulengo");
            Assert.IsTrue((bool)Call(game, "TryCollectVictoryReward"));
        }

        [UnityTest, Timeout(250000)]
        public IEnumerator CompactOrdersPrizeAndRacingShieldStayClear()
        {
            foreach (var (width, height) in new[] { (768, 1024), (600, 720), (600, 600) })
            {
                yield return Size(width, height);
                foreach (int level in new[] { 5, 8 })
                {
                    string key = width + "x" + height + "-p" + (Array.IndexOf(Order, level) + 1).ToString("00") + "-id" + level;
                    yield return PlayVenue(level);
                    AssertOrdersClear(true, key);
                    AssertClubCrestClear(level, false, key);
                    yield return Capture(key + "-play");
                    if (level == 8 && width == height) yield return OpenChulengoAndAssert(true, key);
                    Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                    Debug.Log("QA456 compact: " + key);
                }
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ProtectedViewsKeepTheExistingQuotaPrizeAndRacingGeometry()
        {
            foreach (var (width, height) in new[] { (320, 568), (360, 640), (360, 720), (390, 844), (360, 800),
                (610, 1356), (360, 840), (360, 864), (1080, 1920) })
            {
                yield return Size(width, height);
                foreach (int level in new[] { 5, 8 })
                {
                    string key = width + "x" + height + "-p" + (Array.IndexOf(Order, level) + 1).ToString("00") + "-id" + level;
                    yield return PlayVenue(level);
                    AssertOrdersClear(false, key);
                    AssertClubCrestClear(level, true, key);
                    yield return Capture(key + "-play");
                    if (level == 8) yield return OpenChulengoAndAssert(false, key);
                    Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                    Debug.Log("QA456 protected: " + key);
                }
            }
        }
    }
}
#endif
