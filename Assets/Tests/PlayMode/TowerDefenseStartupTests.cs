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
    public sealed class TowerDefenseStartupTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        static readonly Type GameType = RuntimeType("StreetGame");
        static readonly Type ViewType = RuntimeType("StreetView");
        static readonly Type SimType = RuntimeType("StreetSimulation");
        static readonly FieldInfo Gate = GameType.GetField("AllowEditorTestInitialization", S);
        static readonly int[] CampaignOrder = (int[])SimType.GetField("CampaignOrder", S).GetValue(null);
        static readonly int FirstAction = (int)ViewType.GetField("LevelSelectFirstAction", S).GetRawConstantValue();
        Component game, view;
        Scene scene;
        PortraitGameView portrait;
        bool previousGate, previousBackground;
        float previousTime;
        string originalProgress;
        int snapshotsWithActiveAttempt, matchWrites;

        object Sim => Get(game, "Sim");
        object Save => Get(game, "save");
        object MarketData => Get(Get(game, "Market"), "Data");
        long AttemptCount => Convert.ToInt64(Get(Get(Save, "statistics"), "attemptsStarted"));
        string Phase => Get(Sim, "Phase").ToString();

        static Type RuntimeType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static void Set(object target, string name, object value) => target.GetType().GetField(name, I).SetValue(target, value);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(I)
            .Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);

        bool Persist<T>(string snapshot, T match)
        {
            if (snapshot.Contains("\"attemptActive\":true")) snapshotsWithActiveAttempt++;
            if ((object)match != null) matchWrites++;
            return true;
        }

        [UnitySetUp]
        public IEnumerator Mount()
        {
            previousGate = (bool)Gate.GetValue(null);
            previousBackground = Application.runInBackground;
            previousTime = Time.timeScale;
            originalProgress = PlayerPrefs.GetString(ProgressKey, null);
            Gate.SetValue(null, false);
            Application.runInBackground = true;
            Time.timeScale = 0f;
            Assert.IsNull(RuntimeType("Cloud.FirebasePlayerSession").GetProperty("Instance", S).GetValue(null));
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByName("Main");
            game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(GameType, true)).Single();
            view = game.GetComponent(ViewType);
            var matchType = RuntimeType("StreetMatchRecord");
            var delegateType = typeof(Func<,,>).MakeGenericType(typeof(string), matchType, typeof(bool));
            var persist = GetType().GetMethod(nameof(Persist), I).MakeGenericMethod(matchType).CreateDelegate(delegateType, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", null, persist));
            float deadline = Time.realtimeSinceStartup + 85f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady"));
            foreach (string flag in new[] { "introActive", "hubActive", "marketActive", "rankingActive" })
                Set(view, flag, false);
            var unlocked = (bool[])Get(Save, "unlockedLevelIds");
            for (int index = 0; index < unlocked.Length; index++) unlocked[index] = true;
        }

        [UnityTearDown]
        public IEnumerator Clean()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            portrait?.End();
            Gate.SetValue(null, previousGate);
            Application.runInBackground = previousBackground;
            Time.timeScale = previousTime;
            Assert.AreEqual(originalProgress, PlayerPrefs.GetString(ProgressKey, null), "The real save must not change.");
        }

        IEnumerator SetSize(int width, int height)
        {
            portrait?.End();
            portrait = new PortraitGameView(width, height);
            portrait.Begin();
            yield return portrait.WaitUntilApplied(9f);
            Assert.AreEqual(new Vector2Int(width, height), new Vector2Int(Screen.width, Screen.height));
        }

        void EnterFromSelector(int level)
        {
            Set(view, "levelSelectActive", true);
            Call(view, "EnterSelectedLevel", FirstAction + Array.IndexOf(CampaignOrder, level), level);
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator UnbuildableDefenseDoesNotLockTheSelectorOrOpenAnAttempt()
        {
            yield return SetSize(768, 1024);
            const int level = 10;
            var configType = RuntimeType("TowerDefenseLevelConfig");
            var resource = (string)configType.GetMethod("ResourcesPathForCampaignLevel", S)
                .Invoke(null, new object[] { level });
            var config = Resources.Load(resource, configType);
            var stations = (Array)Get(config, "startingStations");
            // Duplicate a station in the loaded object for this synchronous call only. Neither
            // the normal layout nor the compact retry can honestly build this kitchen.
            var duplicate = (Array)stations.Clone();
            duplicate.SetValue(duplicate.GetValue(0), 1);
            long attemptedBefore = AttemptCount;
            int snapshotsBefore = snapshotsWithActiveAttempt, matchesBefore = matchWrites;
            try
            {
                Set(config, "startingStations", duplicate);
                EnterFromSelector(level);
            }
            finally { Set(config, "startingStations", stations); }

            Assert.AreEqual(level, Get(game, "SelectedLevel"));
            Assert.AreEqual("Ready", Phase);
            Assert.AreEqual(true, Get(view, "levelSelectActive"));
            Assert.AreEqual(false, Get(view, "towerDefenseActive"));
            StringAssert.Contains("Starting stations overlap", (string)Get(view, "towerDefenseError"));
            Assert.IsNull(Get(view, "towerDefenseMode"));
            Assert.IsNull(Get(view, "towerDefenseRuntimeConfig"));
            Assert.AreEqual(false, Get(MarketData, "attemptActive"));
            Assert.IsTrue(string.IsNullOrEmpty((string)Get(Save, "activeAttemptId")));
            Assert.AreEqual(attemptedBefore, AttemptCount);
            Assert.AreEqual(snapshotsBefore, snapshotsWithActiveAttempt);
            Assert.AreEqual(matchesBefore, matchWrites);
            EnterFromSelector(0); // No recovery reset between these selections.
            Assert.AreEqual(0, Get(game, "SelectedLevel"));
            Assert.AreEqual(false, Get(view, "levelSelectActive"));
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator CompactDefenseOpensAndServesAtEveryPreviouslyRejectedSize()
        {
            foreach (var (width, height, failed) in new[]
            {
                (320, 480, new[] { 10, 13, 14 }), (800, 1280, new[] { 10, 13 }),
                (768, 1024, new[] { 10, 13, 14 }), (600, 720, new[] { 9, 10, 13, 14 }),
                (600, 600, new[] { 9, 10, 13, 14 })
            })
            {
                yield return SetSize(width, height);
                foreach (int level in new[] { 9, 10, 13, 14 })
                {
                    string key = CaptureKey(width, height, level);
                    long attempts = AttemptCount;
                    EnterFromSelector(level);
                    Assert.AreEqual(true, Get(view, "towerDefenseActive"), key + ": " + Get(view, "towerDefenseError"));
                    Assert.AreEqual("Playing", Phase, key);
                    Assert.AreEqual(attempts + 1L, AttemptCount, key);
                    var mode = Get(view, "towerDefenseMode");
                    var compact = (Rect?)Get(view, "towerDefenseCompactViewport");
                    Assert.AreEqual(failed.Contains(level), compact.HasValue, key);
                    if (compact.HasValue)
                    {
                        Rect rect = compact.Value;
                        Assert.AreEqual(width * .5f, rect.center.x, .1f, key);
                        Assert.AreEqual(height * (540f / 960f), rect.width, .1f, key);
                        Rect card = (Rect)Call(view, "PilgrimCardBounds", 1);
                        float scale = rect.width / 540f;
                        Vector2 center = new Vector2(rect.x + card.center.x * scale,
                            height - rect.y - card.center.y * scale);
                        int before = (int)Get(mode, "Coins");
                        Tap(new Vector2(rect.xMin * .5f, center.y));
                        Assert.AreEqual(before, Get(mode, "Coins"), key + ": side margin must not hit a card");
                        Tap(center);
                        Assert.Less((int)Get(mode, "Coins"), before, key + ": tap must hire through the visible card");
                    }
                    RunServiceSample(mode, level);
                    Assert.Greater((int)Get(mode, "TotalDelivered"), 0, key);
                    yield return Capture(key + "-play", width, height, "QA-Responsive-2/compact");
                    Assert.IsTrue((bool)Call(game, "ExitTowerDefenseRound", mode), key);
                    Call(view, "EndTowerDefenseRun");
                    Assert.IsNull(Get(view, "towerDefenseCompactViewport"), key);
                    Debug.Log("QA2 compact matrix: " + key + "; fallback=" + compact.HasValue + "; deliveries=" + Get(mode, "TotalDelivered"));
                }
            }
        }

        [UnityTest, Timeout(160000)]
        public IEnumerator ResizeAnOpenDefenseRoundKeepsTheSameAttemptAndProgress()
        {
            const int level = 13;
            yield return SetSize(360, 640);
            EnterFromSelector(level);
            var mode = Get(view, "towerDefenseMode");
            Assert.IsNotNull(mode);
            RunServiceSample(mode, level);
            int delivered = (int)Get(mode, "TotalDelivered");
            Assert.Greater(delivered, 0);
            int coins = (int)Get(mode, "Coins");
            string attempt = (string)Get(Save, "activeAttemptId");
            Assert.IsFalse(string.IsNullOrEmpty(attempt));
            float elapsed = (float)Get(mode, "Elapsed");
            yield return SetSize(768, 1024);
            yield return null;
            Assert.IsNotNull(Get(view, "towerDefenseCompactViewport"), Get(view, "towerDefenseError")?.ToString());
            Assert.AreSame(mode, Get(view, "towerDefenseMode"));
            Assert.AreEqual(attempt, Get(Save, "activeAttemptId"));
            Assert.AreEqual("Playing", Get(mode, "Phase").ToString());
            Assert.AreEqual(elapsed, Get(mode, "Elapsed"));
            Assert.AreEqual(delivered, Get(mode, "TotalDelivered"));
            Assert.AreEqual(coins, Get(mode, "Coins"));
            yield return SetSize(360, 640);
            yield return null;
            Assert.IsNull(Get(view, "towerDefenseCompactViewport"), Get(view, "towerDefenseError")?.ToString());
            Assert.AreSame(mode, Get(view, "towerDefenseMode"));
            Assert.AreEqual(attempt, Get(Save, "activeAttemptId"));
            Assert.AreEqual(delivered, Get(mode, "TotalDelivered"));
        }

        void Tap(Vector2 position)
        {
            Call(view, "HandlePointer", position, true, false);
            Call(view, "HandlePointer", position, false, true);
        }

        static string CaptureKey(int width, int height, int level) =>
            width + "x" + height + "-p" + (Array.IndexOf(CampaignOrder, level) + 1).ToString("00") + "-id" + level;

        void RunServiceSample(object mode, int level)
        {
            int[] roles = level == 9 ? new[] { 0, 1, 0, 1, 0, 1 }
                : level == 14 ? new[] { 1, 2, 3, 4, 0, 1, 2, 4, 3 } : new[] { 0, 1, 2, 0, 1, 2, 0, 1 };
            var roleType = RuntimeType("StreetWorkerRole");
            int next = 0;
            for (int tick = 0; tick < 102 && Get(mode, "Phase").ToString() == "Playing"; tick++)
            {
                if (next < roles.Length && (bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleType, roles[next]))) next++;
                Call(mode, "Step", .25f);
            }
        }

        static IEnumerator Capture(string key, int width, int height, string folder)
        {
            yield return null; yield return null; yield return null;
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(capture, key);
                Assert.AreEqual(new Vector2Int(width, height), new Vector2Int(capture.width, capture.height), key);
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance", folder));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, key + ".jpg"), capture.EncodeToJPG(94));
            }
            finally { UnityEngine.Object.Destroy(capture); }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AllFourDefenseVenuesStillOpenAtTheNineProtectedSizes()
        {
            foreach (var (width, height) in new[] { (320, 568), (360, 640), (360, 720), (390, 844), (360, 800),
                         (610, 1356), (360, 840), (360, 864), (1080, 1920) })
            {
                yield return SetSize(width, height);
                foreach (int level in CampaignOrder.Where(id => id == 9 || id == 10 || id == 13 || id == 14))
                {
                    string key = CaptureKey(width, height, level);
                    long attemptedBefore = AttemptCount;
                    EnterFromSelector(level);
                    Assert.AreEqual(true, Get(view, "towerDefenseActive"), key);
                    Assert.AreEqual("Playing", Phase, key);
                    Assert.AreEqual(true, Get(MarketData, "attemptActive"), key);
                    Assert.AreEqual(attemptedBefore + 1L, AttemptCount, key);
                    var mode = Get(view, "towerDefenseMode");
                    Assert.AreEqual(Get(Sim, "Coins"), Get(mode, "Coins"), key);

                    Assert.IsNull(Get(view, "towerDefenseCompactViewport"), key);
                    RunServiceSample(mode, level);
                    yield return Capture(key + "-play", width, height, "QA-Responsive-2/protected");
                    Assert.IsTrue((bool)Call(game, "ExitTowerDefenseRound", mode), key);
                    Call(view, "EndTowerDefenseRun");
                    Debug.Log("QA2 protected: " + key + "; deliveries=" + Get(mode, "TotalDelivered"));
                }
            }
        }
    }
}
#endif
