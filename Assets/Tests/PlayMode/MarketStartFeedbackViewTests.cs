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
    public sealed class MarketStartFeedbackViewTests
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
        static int Action(string name) => (int)ViewType.GetField(name, S).GetRawConstantValue();
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethods(I).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, I).SetValue(target, value);
        int writes, rejectAt, rejected;
        string tutorialKey;
        bool hadTutorialKey;
        int previousTutorial;

        bool Persist<T>(string snapshot, T match)
        {
            if (++writes != rejectAt) return true;
            rejected++;
            return false;
        }

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
            var callback = GetType().GetMethod(nameof(Persist), I).MakeGenericMethod(match)
                .CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(string), match, typeof(bool)), this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", null, callback));
            float end = Time.realtimeSinceStartup + 85f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady"));
            foreach (string flag in new[] { "introActive", "hubActive", "levelSelectActive", "rankingActive" })
                Set(view, flag, false);
            var ids = (bool[])Get(Get(game, "save"), "unlockedLevelIds");
            for (int index = 0; index < ids.Length; index++) ids[index] = true;
            // Exercise retry from the Market, rather than Chicago's first-start tutorial.
            tutorialKey = (string)ViewType.GetField("LevelTwoCocacoleroTutorialCompletedKey", S).GetRawConstantValue();
            hadTutorialKey = PlayerPrefs.HasKey(tutorialKey);
            previousTutorial = PlayerPrefs.GetInt(tutorialKey, 0);
            PlayerPrefs.SetInt(tutorialKey, 1);
        }

        [UnityTearDown]
        public IEnumerator Clean()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            portrait?.End();
            if (tutorialKey != null)
            {
                if (hadTutorialKey) PlayerPrefs.SetInt(tutorialKey, previousTutorial);
                else PlayerPrefs.DeleteKey(tutorialKey);
            }
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

        object Sim => Get(game, "Sim");
        object MarketData => Get(Market, "Data");
        long AttemptCount => Convert.ToInt64(Get(Get(Get(game, "save"), "statistics"), "attemptsStarted"));
        string Phase => Get(Sim, "Phase").ToString();
        void Act(string action) => Call(view, "DispatchAction", Action(action));

        IEnumerator OpenChicago()
        {
            Array.Clear((int[])Get(MarketData, "stockUnits"), 0, ((int[])Get(MarketData, "stockUnits")).Length);
            Set(MarketData, "balanceCents", 10000000L);
            Set(view, "levelSelectActive", true);
            Call(view, "EnterSelectedLevel", Action("LevelSelectFirstAction") + Array.IndexOf(Order, 1), 1);
            Assert.AreEqual(1, Get(Sim, "LevelIndex"));
            Assert.AreEqual(true, Get(view, "marketActive"));
            Assert.AreEqual("Ready", Phase);
            yield return null; yield return null;
        }

        void TapPlay()
        {
            var local = (Rect)Call(view, "MarketPlayBounds");
            float fit = (float)Get(view, "MarketFitScale"), left = (float)Get(view, "MarketFitLeft");
            float scale = Screen.width / 540f;
            var pixel = new Vector2((left + local.center.x * fit) * scale, Screen.height - local.center.y * fit * scale);
            Call(view, "HandlePointer", pixel, true, false);
            Call(view, "HandlePointer", pixel, false, true);
        }

        IEnumerator WaitForShutter()
        {
            Assert.IsTrue((bool)Get(view, "RewardShutterActive"), "Start must run while the real navigation shutter covers the screen.");
            float end = Time.realtimeSinceStartup + 8f;
            while ((bool)Get(view, "RewardShutterActive") && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse((bool)Get(view, "RewardShutterActive"));
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(screenshot);
                Assert.AreEqual(new Vector2Int(Screen.width, Screen.height), new Vector2Int(screenshot.width, screenshot.height));
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/QA-Market-Start-Feedback/frames"));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), screenshot.EncodeToJPG(94));
            }
            finally { UnityEngine.Object.Destroy(screenshot); }
        }

        [UnityTest, Timeout(160000)]
        public IEnumerator RealShortageStaysStockGatedWithoutAnAttemptOrAWrite()
        {
            yield return Size(540, 960);
            yield return OpenChicago();
            Assert.IsFalse((bool)Get(game, "CanStartRound"));
            Assert.Greater((long)Call(Market, "MinimumPurchaseCostCents", Sim), 0L);
            int before = writes;
            TapPlay();
            Assert.AreEqual("Falta stock para iniciar", Get(view, "marketMessage"));
            Assert.AreEqual("Ready", Phase);
            Assert.AreEqual(true, Get(view, "marketActive"));
            Assert.AreEqual(before, writes);
            Assert.AreEqual(false, Get(MarketData, "attemptActive"));
            yield return Capture("540x960-faltante-real");
        }

        [UnityTest, Timeout(250000)]
        public IEnumerator ChicagoReportsRejectedStartWriteAndRetriesWithoutRepurchase()
        {
            foreach (var (width, height) in new[] { (540, 960), (610, 1356), (768, 1024) })
            {
                string key = width + "x" + height;
                yield return Size(width, height);
                yield return OpenChicago();
                Set(view, "marketLastPurchaseTime", -10f);
                Act("MarketMissingAction");
                Assert.IsTrue((bool)Get(game, "CanStartRound"), key);
                Assert.AreEqual(0L, Call(Market, "MinimumPurchaseCostCents", Sim), key);
                long balance = (long)Get(Market, "BalanceCents");
                var stock = (int[])((int[])Get(MarketData, "stockUnits")).Clone();
                long attempts = AttemptCount;
                int count = rejected;
                rejectAt = writes + 1;

                TapPlay();
                yield return WaitForShutter();
                Assert.AreEqual(count + 1, rejected, key);
                Assert.AreEqual("No se pudo guardar el inicio. Volvé a intentar.", Get(view, "marketMessage"), key);
                Assert.AreEqual("Ready", Phase, key);
                Assert.AreEqual(true, Get(view, "marketActive"), key);
                Assert.AreEqual(false, Get(MarketData, "attemptActive"), key);
                Assert.IsTrue(string.IsNullOrEmpty((string)Get(Get(game, "save"), "activeAttemptId")), key);
                Assert.AreEqual(attempts, AttemptCount, key);
                Assert.AreEqual(balance, Get(Market, "BalanceCents"), key);
                CollectionAssert.AreEqual(stock, (int[])Get(MarketData, "stockUnits"), key);
                Assert.IsTrue((bool)Get(game, "CanStartRound"), key);
                Assert.AreEqual("", Call(view, "MarketShortageMessage"), key);
                yield return Capture(key + "-error-de-guardado");

                // Same stock and same button; no restock action between the failed and successful starts.
                TapPlay();
                yield return WaitForShutter();
                Assert.AreEqual("Playing", Phase, key);
                Assert.AreEqual(false, Get(view, "marketActive"), key);
                Assert.AreEqual("", Get(view, "marketMessage"), key);
                Assert.AreEqual(attempts + 1L, AttemptCount, key);
                Assert.AreEqual(balance, Get(Market, "BalanceCents"), key);
                Assert.AreEqual(true, Get(MarketData, "attemptActive"), key);
                yield return Capture(key + "-reintento-en-partida");
                Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                Debug.Log("Mercado reintento: " + key + "; 1 rechazo, sin recompra, 1 intento registrado.");
            }
        }
    }
}
#endif
