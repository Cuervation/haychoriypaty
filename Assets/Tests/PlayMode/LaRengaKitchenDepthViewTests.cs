#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class LaRengaKitchenDepthViewTests
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

        object Sim => Get(game, "Sim");
        object Save => Get(game, "save");

        static Type RuntimeType(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        static object Get(object target, string name)
        {
            var property = target.GetType().GetProperty(name, I);
            return property != null ? property.GetValue(target) : target.GetType().GetField(name, I).GetValue(target);
        }
        static void Set(object target, string name, object value) => target.GetType().GetField(name, I).SetValue(target, value);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(I)
            .Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);

        bool Persist<T>(string snapshot, T match) => true;

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

        object Role(int id) => Enum.ToObject(RuntimeType("StreetWorkerRole"), id);
        object OpenLaRenga()
        {
            EnterFromSelector(13);
            Assert.AreEqual(true, Get(view, "towerDefenseActive"), Get(view, "towerDefenseError")?.ToString());
            var mode = Get(view, "towerDefenseMode");
            Assert.IsNotNull(mode);
            return mode;
        }

        void Return(object mode)
        {
            Assert.IsTrue((bool)Call(game, "ExitTowerDefenseRound", mode));
            Call(view, "EndTowerDefenseRun");
        }

        static IEnumerator Capture(string key)
        {
            yield return null; yield return null; yield return null;
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.IsNotNull(capture);
                Assert.AreEqual(new Vector2Int(Screen.width, Screen.height), new Vector2Int(capture.width, capture.height));
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/QA-LaRenga-Depth/frames"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, key + ".jpg"), capture.EncodeToJPG(94));
            }
            finally { UnityEngine.Object.Destroy(capture); }
        }

        [UnityTest, Timeout(220000)]
        public IEnumerator LaRengaWorkerFeetOrderAgainstEveryFurnishedStationAndItsStock()
        {
            foreach (var (width, height) in new[] { (540, 960), (610, 1356), (768, 1024) })
            {
                yield return SetSize(width, height);
                object mode = OpenLaRenga();
                foreach (int role in new[] { 0, 1, 2, 0, 1, 2 })
                    Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(role)));
                object presentation = Get(view, "towerDefensePresentation");
                var stationViews = (IDictionary)Get(presentation, "pilgrimStations");
                var workerViews = (IDictionary)Get(presentation, "workerViews");
                var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>().ToArray();
                var stations = ((IEnumerable)Get(mode, "PlacedStations")).Cast<object>().ToArray();
                Assert.AreEqual(6, stations.Length);
                Assert.AreEqual(6, workers.Length);
                float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
                for (int index = 0; index < stations.Length; index++)
                {
                    object station = stations[index];
                    var stockRoot = (GameObject)stationViews[Get(station, "Id")];
                    var group = stockRoot.GetComponent<SortingGroup>();
                    string kind = Get(station, "Kind").ToString();
                    Rect footprint = (Rect)Get(station, "Bounds");
                    Assert.IsNotNull(group, kind);
                    Assert.AreEqual(32 + Mathf.RoundToInt(-footprint.yMin * 12f), group.sortingOrder, kind);
                    int furnitureOrder = stockRoot.transform.GetChild(0).GetComponentsInChildren<SpriteRenderer>().Max(r => r.sortingOrder);
                    SpriteRenderer[] stock = stockRoot.GetComponentsInChildren<SpriteRenderer>()
                        .Where(r => r.gameObject.name.StartsWith("Physical kitchen unit", StringComparison.Ordinal) ||
                            r.gameObject.name.Contains("ambient grill")).ToArray();
                    Assert.IsNotEmpty(stock, kind + ": resident stock shares the furniture group");
                    Assert.Greater(stock.Min(r => r.sortingOrder), furnitureOrder, kind);
                }

                foreach (bool front in new[] { true, false })
                {
                    // Explicit visual probes. Real path/collision behavior is covered separately.
                    for (int index = 0; index < stations.Length; index++)
                    {
                        Rect footprint = (Rect)Get(stations[index], "Bounds");
                        workers[index].GetType().GetProperty("Position", I).SetValue(workers[index],
                            new Vector2(footprint.center.x, footprint.yMin + (front ? -4f : 18f) * pixel));
                    }
                    Call(presentation, "Sync", mode);
                    for (int index = 0; index < stations.Length; index++)
                    {
                        var group = ((GameObject)stationViews[Get(stations[index], "Id")]).GetComponent<SortingGroup>();
                        var rider = (SpriteRenderer)Get(workerViews[Get(workers[index], "Id")], "Renderer");
                        string kind = Get(stations[index], "Kind").ToString();
                        if (front) Assert.Greater(rider.sortingOrder, group.sortingOrder, kind);
                        else Assert.Less(rider.sortingOrder, group.sortingOrder, kind);
                    }
                    yield return Capture(width + "x" + height + (front ? "-sonda-delante" : "-sonda-detras"));
                }
                Return(mode);
                Debug.Log("Renga depth fixture: " + width + "x" + height + "; six furniture groups, front/back and stock.");
            }
        }

        [UnityTest, Timeout(220000)]
        public IEnumerator LaRengaRealWaveKeepsTheAuditedOutcomeAndDeliveryCount()
        {
            foreach (var (width, height, expectedWins, expectedFans, expectedUnits) in new[]
                     { (540, 960, true, 120, 145), (610, 1356, false, 66, 77) })
            {
                yield return SetSize(width, height);
                object mode = OpenLaRenga();
                int[] hires = { 0, 1, 2, 0, 1, 2, 0, 1 };
                int hired = 0;
                int checks = 0;
                var stations = ((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                    .Select(station => (Rect)Get(station, "Bounds")).ToArray();
                for (int tick = 0; tick < 2400 && Get(mode, "Phase").ToString() == "Playing"; tick++)
                {
                    if (hired < hires.Length && (bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(hires[hired]))) hired++;
                    if (tick > 80 && tick % 32 == 0) Call(game, "TryUpgradeTowerDefenseSpeed", mode);
                    Call(mode, "Step", .25f);
                    foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                    {
                        Rect feet = (Rect)Call(mode, "WorkerFeet", Get(worker, "Position"));
                        foreach (Rect footprint in stations)
                        {
                            Assert.IsFalse(feet.Overlaps(footprint), "The fix is visual; staff must not walk over furniture.");
                            checks++;
                        }
                    }
                    if (tick == 100 || tick == 180)
                        yield return Capture(width + "x" + height + "-ola-" + tick);
                }
                Assert.Greater(checks, 0);
                Assert.AreEqual("Finished", Get(mode, "Phase").ToString());
                Assert.AreEqual(expectedWins, Get(mode, "HasWon"), width + "x" + height);
                Assert.AreEqual(expectedFans, Get(mode, "ServedFans"), width + "x" + height);
                Assert.AreEqual(expectedUnits, Get(mode, "TotalDelivered"), width + "x" + height);
                Debug.Log("Renga real wave: " + width + "x" + height + "; won=" + expectedWins + "; served=" + expectedFans +
                    "; deliveries=" + expectedUnits + "; collision checks=" + checks);
                yield return null;
                if ((bool)Get(game, "HasPendingChulengo"))
                    Assert.IsTrue((bool)Call(game, "TryCollectVictoryReward"));
                Assert.IsTrue((bool)Call(game, "ReturnToLevels"));
                Call(view, "EndTowerDefenseRun");
            }
        }
    }
}
#endif
