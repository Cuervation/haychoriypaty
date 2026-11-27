#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
                // 2026-11-24: La Renga starts with one Parrillero and 40 coins; fund the rest of the six-person crew.
                Set(mode, "coins", 100000);
                foreach (int role in new[] { 2, 0, 1, 2, 1 })
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
                        // The campaign crew body sorts as one SortingGroup at the feet depth.
                        var rider = (SortingGroup)Get(workerViews[Get(workers[index], "Id")], "Group");
                        Assert.IsNotNull(rider);
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

        // 2026-11-24 (a pedido, from a phone capture): hiring the Camioneros crew in quick succession used to teleport a worker that was mid-job (its route then
        // crossed the table and it stood still with a fan reserved) and left new hires inside the barrels' keep-out strip, where no route can start.
        [UnityTest, Timeout(300000)]
        public IEnumerator CamionerosCrewHiredInQuickSuccessionNeverGetsStuck()
        {
            yield return SetSize(575, 1280);
            EnterFromSelector(9);
            object mode = Get(view, "towerDefenseMode");
            Set(mode, "coins", 100000);
            int[] roles = { 0, 1, 0, 1, 0, 1 };
            int hired = 0;
            var lastKey = new Dictionary<int, string>(); var since = new Dictionary<int, float>();
            float t = 0f, longestStuck = 0f;
            for (int tick = 0; tick < 9000 && Get(mode, "Phase").ToString() == "Playing"; tick++)
            {
                t += .05f;
                if (hired < roles.Length && Call(game, "TryHireTowerDefenseAtPost", mode, Role(roles[hired])) is bool ok && ok) hired++;
                if (tick > 400 && tick % 160 == 0) Call(game, "TryUpgradeTowerDefenseSpeed", mode);
                Call(mode, "Step", .05f);
                foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                {
                    int id = (int)Get(worker, "Id");
                    string key = Get(worker, "Position") + "|" + Get(worker, "ServiceStage") + "|" + Get(worker, "PendingFanId") + "|" + Get(worker, "IsMoving") + "|" + Get(worker, "ServiceDwelling");
                    if (!lastKey.TryGetValue(id, out string previous) || previous != key) { lastKey[id] = key; since[id] = t; continue; }
                    if ((int)Get(worker, "ServiceStage") != 0) longestStuck = Mathf.Max(longestStuck, t - since[id]);
                }
                if (tick % 20 == 0) yield return null;
            }
            Assert.AreEqual(roles.Length, hired);
            Assert.Less(longestStuck, 8f, "A worker with a job never stands still for long.");
            // 2026-11-26: the user kept Camioneros' harder orders (4 and 5 units) even though this maximum crew no longer wins
            // (it served 79-87/120); the test keeps checking that nobody gets stuck and that the round ends, not the result.
            Assert.AreEqual("Finished", Get(mode, "Phase").ToString(), "served " + Get(mode, "ServedFans") + "/120 in " + t.ToString("0.0") + " s");
            Assert.Greater((int)Get(mode, "ServedFans"), 0);
            Return(mode);
        }

        // 2026-11-26 (a pedido): the time the app spent in the background is played on return, a few seconds per frame.
        [UnityTest, Timeout(120000)]
        public IEnumerator TimeAwayInTheBackgroundIsPlayedWhenTheAppReturns()
        {
            yield return SetSize(575, 1280);
            EnterFromSelector(9);
            object mode = Get(view, "towerDefenseMode");
            Assert.AreEqual("Playing", Get(mode, "Phase").ToString());
            float before = (float)Get(mode, "Elapsed");
            // As OnApplicationPause(true) then (false) after 25 s away, without the save the real pause makes.
            Set(game, "backgroundSinceUtc", (DateTime?)DateTime.UtcNow.AddSeconds(-25));
            Call(game, "RecordBackgroundTime", false);
            Assert.AreEqual(25f, (float)Get(game, "PendingBackgroundCatchUpSeconds"), .5f, "the time away is owed to the running round");
            for (int frame = 0; frame < 6; frame++) yield return null;
            Assert.AreEqual(0f, (float)Get(game, "PendingBackgroundCatchUpSeconds"), "played within a few frames (10 s per frame)");
            Assert.GreaterOrEqual((float)Get(mode, "Elapsed") - before, 24.5f, "the clock and the fans advanced by the time away");
            Assert.Greater((int)Get(mode, "SpawnedFans"), 0, "fans kept arriving while away");

            // A paused round owes nothing.
            Call(mode, "Pause");
            Set(game, "backgroundSinceUtc", (DateTime?)DateTime.UtcNow.AddSeconds(-25));
            Call(game, "RecordBackgroundTime", false);
            Assert.AreEqual(0f, (float)Get(game, "PendingBackgroundCatchUpSeconds"));
            Return(mode);
        }

        // 2026-11-26 (a pedido, before publishing): every screen of the game, each one captured at the ten aspect ratios of the publishing matrix
        // (one folder per screen), for manual review. Screens are set up once and the Game view is resized under them.
        [UnityTest, Timeout(1800000), Explicit("Captures to Logs/Acceptance/FullGame-20261126")]
        public IEnumerator EveryScreenAcrossTheResolutionMatrixCapture()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/FullGame-20261126"));
            var sizes = new[] {
                new Vector2Int(720, 1280), new Vector2Int(1080, 2160), new Vector2Int(1080, 2340), new Vector2Int(1080, 2400), new Vector2Int(1220, 2712),
                new Vector2Int(1080, 2520), new Vector2Int(800, 1280), new Vector2Int(768, 1024), new Vector2Int(904, 2316), new Vector2Int(1768, 2208) };
            string[] flags = { "introActive", "settingsActive", "marketActive", "badgesActive", "rankingActive", "levelSelectActive", "profileActive", "hubActive" };
            void Show(params string[] on) { foreach (string f in flags) Set(view, f, false); foreach (string f in on) Set(view, f, true); Set(view, "pressedAction", 0); }
            IEnumerator AllSizes(string screen, Action beforeEach = null)
            {
                string folder = Path.Combine(root, screen);
                Directory.CreateDirectory(folder);
                foreach (Vector2Int size in sizes)
                {
                    yield return SetSize(size.x, size.y);
                    beforeEach?.Invoke();
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    yield return new WaitForEndOfFrame();
                    Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder, size.x + "x" + size.y + ".png"), capture.EncodeToPNG());
                    UnityEngine.Object.Destroy(capture);
                }
            }
            object Market() => Get(game, "Market");
            bool StartLevel(int level)
            {
                if (!(bool)Call(game, "SelectLevel", level)) return false;
                Set(Get(Market(), "Data"), "balanceCents", 10000000L);
                if (!(bool)Get(game, "CanStartRound")) Call(game, "TryBuyMissingIngredients");
                return (bool)Call(game, "TryStartRound");
            }
            void StepSim(float seconds) { object sim = Get(game, "Sim"); for (float t = 0f; t < seconds; t += .05f) Call(sim, "Step", .05f); }

            bool onlyVictory = File.Exists(Path.Combine(root, "skip-menus")); // drop this file to recapture only the levels at the end
            if (!onlyVictory) {
            // Menus.
            yield return AllSizes("01-portada", () => { Show("introActive"); Set(view, "splashClockStarted", false); });
            Show("hubActive"); yield return AllSizes("02-barrio");
            Show("hubActive", "settingsActive"); yield return AllSizes("03-ajustes");
            Show("levelSelectActive"); Set(view, "levelSelectScroll", 0f); yield return AllSizes("04-selector-arriba");
            yield return AllSizes("05-selector-abajo", () => Set(view, "levelSelectScroll", 100000f));
            Show("hubActive"); Call(view, "DispatchAction", 201);
            Set(view, "marketTab", 0); yield return AllSizes("06-mercado");
            Set(view, "marketTab", 1); yield return AllSizes("07-mercado-pestana-2");
            Show("hubActive");

            // A normal level before and during play, then lost (riot).
            Show(); Assert.IsTrue((bool)Call(game, "SelectLevel", 5)); yield return AllSizes("08-nivel-listo");
            Assert.IsTrue(StartLevel(5), "level 5 starts"); StepSim(25f); yield return AllSizes("09-nivel-jugando");
            object playing = Get(game, "Sim");
            float duration = (float)Get(playing, "LevelDuration");
            playing.GetType().GetField("<Elapsed>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(playing, duration - .1f);
            StepSim(.3f);
            Assert.AreEqual("Lost", Get(playing, "Phase").ToString());
            yield return AllSizes("10-derrota-disturbio");
            Call(view, "DispatchAction", 15);

            // Huracán's bonus track.
            Show(); if (StartLevel(11)) { StepSim(8f); yield return AllSizes("11-huracan"); }
            }
            Call(game, "ReturnToLevels"); // leave Huracán's bonus round so the next levels can start

            // The two Defense levels not covered by the earlier capture.
            Show("levelSelectActive"); EnterFromSelector(13);
            object renga = Get(view, "towerDefenseMode");
            if (renga != null) { for (int i = 0; i < 16; i++) Call(renga, "Step", .5f); yield return AllSizes("12-la-renga"); Return(renga); }
            Show("levelSelectActive"); EnterFromSelector(14);
            object lujan = Get(view, "towerDefenseMode");
            if (lujan != null) { for (int i = 0; i < 16; i++) Call(lujan, "Step", .5f); yield return AllSizes("13-peregrinacion"); Return(lujan); }

            // A won normal level: the victory transition, then the result.
            Show(); Assert.IsTrue(StartLevel(5), "level 5 starts for the victory"); StepSim(5f);
            object winning = Get(game, "Sim");
            System.Array delivered = (System.Array)winning.GetType().GetField("deliveredByProduct", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(winning);
            for (int product = 0; product < delivered.Length; product++)
                delivered.SetValue(Convert.ChangeType(winning.GetType().GetMethod("GetProductGoal", new[] { typeof(int) }).Invoke(winning, new object[] { product }), delivered.GetType().GetElementType()), product);
            StepSim(.1f);
            for (int frame = 0; frame < 30; frame++) yield return null;
            yield return AllSizes("14-victoria");
            float until = Time.realtimeSinceStartup + 15f;
            while (!(bool)Get(game, "ShowChulengoResult") && Time.realtimeSinceStartup < until) yield return null;
            yield return AllSizes("15-resultado");
        }

        // 2026-11-27 (a pedido): Huracán plays as a normal level between Ferro and Independiente: Market stock, quotas, clock, passive kitchen.
        [UnityTest, Timeout(180000)]
        public IEnumerator HuracanPlaysAsANormalQuotaLevel()
        {
            yield return SetSize(610, 1356);
            Assert.IsTrue((bool)Call(game, "SelectLevel", 11));
            object market = Get(game, "Market");
            Set(Get(market, "Data"), "balanceCents", 10000000L);
            if (!(bool)Get(game, "CanStartRound")) Assert.IsTrue((bool)Call(game, "TryBuyMissingIngredients"));
            Assert.IsTrue((bool)Call(game, "TryStartRound"), "Huracán starts with Market stock like any level");
            object sim = Get(game, "Sim");
            Assert.IsFalse((bool)Get(sim, "IsBonusTrack"));
            Assert.IsFalse((bool)Get(sim, "UsesActiveCooks"), "passive kitchen like Ferro and Independiente");
            Assert.AreEqual(6, (int)Get(sim, "ProductCount"));
            Assert.AreEqual(225f, (float)Get(sim, "TimeRemaining"), .5f);
            int goals = 0;
            foreach (int product in new[] { 0, 1, 2, 3, 4, 6 }) goals += (int)sim.GetType().GetMethod("GetProductGoal", new[] { typeof(int) }).Invoke(sim, new object[] { product });
            Assert.AreEqual(677, goals);
            for (float t = 0f; t < 20f; t += .05f) Call(sim, "Step", .05f);
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString());
            Assert.Greater((float)Get(sim, "Elapsed"), 19f);
            yield return Capture("huracan-nivel-normal");
            Call(game, "ReturnToLevels");
        }

        // 2026-11-26: recheck of the two screens fixed in the full review (settings panel, Huracán's HUD bar) at the ten aspect ratios.
        [UnityTest, Timeout(600000), Explicit("Captures to Logs/Acceptance/FullGame-20261126/recheck")]
        public IEnumerator SettingsAndHuracanRecheckCapture()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/FullGame-20261126/recheck"));
            Directory.CreateDirectory(root);
            var sizes = new[] {
                new Vector2Int(720, 1280), new Vector2Int(1080, 2160), new Vector2Int(1080, 2340), new Vector2Int(1080, 2400), new Vector2Int(1220, 2712),
                new Vector2Int(1080, 2520), new Vector2Int(800, 1280), new Vector2Int(768, 1024), new Vector2Int(904, 2316), new Vector2Int(1768, 2208) };
            string[] flags = { "introActive", "settingsActive", "marketActive", "badgesActive", "rankingActive", "levelSelectActive", "profileActive", "hubActive" };
            IEnumerator Shots(string prefix)
            {
                foreach (Vector2Int size in sizes)
                {
                    yield return SetSize(size.x, size.y);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    yield return new WaitForEndOfFrame();
                    Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(root, prefix + "-" + size.x + "x" + size.y + ".png"), capture.EncodeToPNG());
                    UnityEngine.Object.Destroy(capture);
                }
            }
            foreach (string f in flags) Set(view, f, false);
            Set(view, "hubActive", true); Set(view, "settingsActive", true);
            yield return Shots("ajustes");
            foreach (string f in flags) Set(view, f, false);
            Assert.IsTrue((bool)Call(game, "SelectLevel", 11));
            Assert.IsTrue((bool)Call(game, "TryStartRound"));
            object sim = Get(game, "Sim");
            for (float t = 0f; t < 8f; t += .05f) Call(sim, "Step", .05f);
            yield return Shots("huracan");
            Call(game, "ReturnToLevels");
        }

        // 2026-11-26 diagnosis (user report on La Renga: several workers stood still and a fan escaped with its order). Plays the audit's crew at
        // 540x960 and at the Motorola's 610x1356 and writes every worker and pending fan every second, plus the moment a fan escapes.
        [UnityTest, Timeout(600000), Explicit("Diagnostic trace to Logs/Acceptance/RengaStall-20261126")]
        public IEnumerator LaRengaCrewStallTrace()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RengaStall-20261126"));
            Directory.CreateDirectory(directory);
            MethodInfo deliveryRole = RuntimeType("TowerDefenseGameMode").GetMethod("GetDeliveryRole", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var (width, height) in new[] { (540, 960), (610, 1356) })
            {
                yield return SetSize(width, height);
                object mode = OpenLaRenga();
                int[] hires = { 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2 };
                int hired = 0;
                var log = new System.Text.StringBuilder();
                var idleWithWork = new Dictionary<string, float>(); var longest = new Dictionary<string, float>();
                float t = 0f;
                for (int tick = 0; tick < 24000 && Get(mode, "Phase").ToString() == "Playing"; tick++)
                {
                    t += .05f;
                    if (hired < hires.Length && (bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(hires[hired]))) hired++;
                    if (tick > 320 && tick % 128 == 0) Call(game, "TryUpgradeTowerDefenseSpeed", mode);
                    Call(mode, "Step", .05f);
                    // Roles with an idle worker while an unreserved fan wants one of their products.
                    var wanted = new HashSet<string>();
                    foreach (object fan in (IEnumerable)Get(mode, "Fans"))
                    {
                        if ((bool)Get(fan, "OrderComplete") || (int)Get(fan, "ReservedByWorker") != 0) continue;
                        foreach (object line in (IEnumerable)Get(fan, "Order"))
                            if ((int)Get(line, "Remaining") > 0) wanted.Add(deliveryRole.Invoke(mode, new object[] { (int)Get(line, "Product") }).ToString());
                    }
                    var idleRoles = new HashSet<string>();
                    foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                        if (!(bool)Get(worker, "IsMoving") && (int)Get(worker, "ServiceStage") == 0) idleRoles.Add(Get(worker, "Role").ToString());
                    foreach (string role in new[] { "Parrillero", "Cocacolero", "ParrilleroPremium" })
                    {
                        float streak = idleRoles.Contains(role) && wanted.Contains(role) ? (idleWithWork.TryGetValue(role, out float s0) ? s0 : 0f) + .05f : 0f;
                        idleWithWork[role] = streak;
                        longest[role] = Mathf.Max(longest.TryGetValue(role, out float l0) ? l0 : 0f, streak);
                    }
                    bool snapshot = tick % 20 == 0 || Get(mode, "Phase").ToString() != "Playing";
                    if (!snapshot) continue;
                    log.Append("t=").Append(t.ToString("0.0")).Append(" served=").Append(Get(mode, "ServedFans")).Append(" coins=").Append(Get(mode, "Coins")).Append('\n');
                    foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                        log.Append("  W").Append(Get(worker, "Id")).Append(' ').Append(Get(worker, "Role")).Append(" pos=").Append(((Vector2)Get(worker, "Position")).ToString("F2"))
                            .Append(" post=").Append(((Vector2)Get(worker, "PostPosition")).ToString("F2")).Append(" mov=").Append(Get(worker, "IsMoving"))
                            .Append(" stage=").Append(Get(worker, "ServiceStage")).Append(" fan=").Append(Get(worker, "PendingFanId")).Append(" prod=").Append(Get(worker, "PendingProduct"))
                            .Append(" dwell=").Append(Get(worker, "ServiceDwelling")).Append(" cd=").Append(((float)Get(worker, "DeliveryCooldownSeconds")).ToString("0.00")).Append('\n');
                    foreach (object fan in (IEnumerable)Get(mode, "Fans"))
                    {
                        if ((bool)Get(fan, "OrderComplete")) continue;
                        object follower = Get(fan, "RouteFollower");
                        log.Append("  F").Append(Get(fan, "Id")).Append(" res=").Append(Get(fan, "ReservedByWorker")).Append(" prog=")
                            .Append(((float)Get(follower, "Progress")).ToString("0.00")).Append('/').Append(((float)Get(follower, "TotalDistance")).ToString("0.00")).Append(" order=");
                        foreach (object line in (IEnumerable)Get(fan, "Order")) log.Append(Get(line, "Product")).Append('x').Append(Get(line, "Remaining")).Append(' ');
                        log.Append('\n');
                    }
                    if (tick % 400 == 0) yield return null;
                }
                log.Append("END ").Append(width).Append('x').Append(height).Append(" phase=").Append(Get(mode, "Phase")).Append(" won=").Append(Get(mode, "HasWon"))
                    .Append(" served=").Append(Get(mode, "ServedFans")).Append(" t=").Append(t.ToString("0.0")).Append(" hired=").Append(hired);
                foreach (var pair in longest) log.Append(" longestIdleWithWork[").Append(pair.Key).Append("]=").Append(pair.Value.ToString("0.0"));
                File.WriteAllText(Path.Combine(directory, "trace-" + width + "x" + height + ".txt"), log.ToString());
                Return(mode);
            }
        }

        // 2026-11-26 (a pedido: "analizá que no tenga ningún problema visual en ninguna de las resoluciones"): every screen changed this
        // session at 16:9, 20:9, the Motorola's 1220x2712 and a 3:4 tablet, for manual review.
        [UnityTest, Timeout(600000), Explicit("Captures to Logs/Acceptance/Resolutions-20261126")]
        public IEnumerator ChangedScreensAcrossResolutionsCapture()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/Resolutions-20261126"));
            Directory.CreateDirectory(folder);
            object save = Get(game, "save");
            object statistics = save.GetType().GetField("statistics").GetValue(save);
            statistics.GetType().GetField("deliveredByProduct").SetValue(statistics, new long[] { 5400, 2600, 820, 120, 3100, 0, 510 });
            Set(statistics, "attemptsFinished", 184L); Set(statistics, "wins", 97L); Set(statistics, "customersAttended", 9321L);
            statistics.GetType().GetMethod("Normalize").Invoke(statistics, null);
            Set(view, "profilePreviewNickname", "Cuervation"); Set(view, "profilePreviewEmail", "hincha.ejemplo@gmail.com"); Set(view, "profilePreviewSignedIn", true);
            Type snapshotType = RuntimeType("Cloud.LeaderboardSnapshot"), entryType = RuntimeType("Cloud.LeaderboardEntry");
            object snapshot = Activator.CreateInstance(snapshotType);
            string[] nicks = { "Morcipan_10", "La Chimi", "Tano Parrilla", "Fernetero", "Doña Provoleta", "El Colo Asado", "Bondiolero", "Vacío_Feliz", "Mollejita", "Pancho.LP" };
            IList top = (IList)Get(snapshot, "top");
            for (int i = 0; i < nicks.Length; i++)
            {
                object e = Activator.CreateInstance(entryType);
                Set(e, "uid", "u" + i); Set(e, "nickname", nicks[i]); Set(e, "points", 1890L - i * 120L); Set(e, "wonLevels", 14L - i);
                top.Add(e);
            }
            object me = Activator.CreateInstance(entryType);
            Set(me, "uid", "me"); Set(me, "nickname", "Cuervation"); Set(me, "points", 393L); Set(me, "wonLevels", 4L);
            Set(snapshot, "me", me); Set(snapshot, "myRank", 27L);
            Type rankingState = view.GetType().GetNestedType("RankingState", BindingFlags.NonPublic);
            string[] flags = { "introActive", "settingsActive", "marketActive", "badgesActive", "rankingActive", "levelSelectActive", "profileActive", "hubActive" };
            IEnumerator Shot(string name)
            {
                for (int frame = 0; frame < 4; frame++) yield return null;
                yield return new WaitForEndOfFrame();
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture);
            }
            void Show(string flag) { foreach (string f in flags) Set(view, f, false); Set(view, flag, true); Set(view, "pressedAction", 0); }
            // Publishing matrix (2026-11-26): 16:9, 18:9, 19.5:9, 20:9 (and the Motorola), 21:9, 16:10 and 3:4 tablets, a folded phone's tall cover
            // screen and an unfolded phone's near-square inner screen.
            var sizes = new[] {
                new Vector2Int(720, 1280), new Vector2Int(1080, 2160), new Vector2Int(1080, 2340), new Vector2Int(1080, 2400), new Vector2Int(1220, 2712),
                new Vector2Int(1080, 2520), new Vector2Int(800, 1280), new Vector2Int(768, 1024), new Vector2Int(904, 2316), new Vector2Int(1768, 2208) };
            foreach (var size in sizes)
            {
                yield return SetSize(size.x, size.y);
                string tag = size.x + "x" + size.y;
                Show("hubActive"); yield return Shot(tag + "-01-hub");
                Show("profileActive"); Set(view, "profileEditing", false); Set(view, "profileConfirmSignOut", false); yield return Shot(tag + "-02-perfil");
                Set(view, "profileEditing", true); Set(view, "profileNicknameInput", "Cuervation 10"); yield return Shot(tag + "-03-perfil-editando");
                Set(view, "profileEditing", false); Set(view, "profileConfirmSignOut", true); yield return Shot(tag + "-04-perfil-cerrar-sesion");
                Set(view, "profileConfirmSignOut", false);
                Show("badgesActive"); Set(view, "badgesScroll", 0f); yield return Shot(tag + "-05-insignias-arriba");
                Set(view, "badgesScroll", 100000f); yield return Shot(tag + "-06-insignias-final");
                Show("rankingActive"); Set(view, "rankingSnapshot", snapshot); Set(view, "rankingState", Enum.ToObject(rankingState, 2)); yield return Shot(tag + "-07-ranking");
                Show("levelSelectActive");
                EnterFromSelector(10);
                object redondos = Get(view, "towerDefenseMode");
                Assert.IsNotNull(redondos, Get(view, "towerDefenseError")?.ToString());
                Assert.IsTrue((bool)Call(redondos, "TrySpawnFan", new[] { 0, 2, 4, 6 }, new[] { 5, 4, 5, 4 }));
                for (int i = 0; i < 12; i++) Call(redondos, "Step", .5f);
                yield return Shot(tag + "-08-redondos");
                Return(redondos);
                Show("levelSelectActive");
                EnterFromSelector(9);
                object camioneros = Get(view, "towerDefenseMode");
                Assert.IsNotNull(camioneros, Get(view, "towerDefenseError")?.ToString());
                Assert.IsTrue((bool)Call(camioneros, "TrySpawnFan", new[] { 0, 4 }, new[] { 5, 4 }));
                for (int i = 0; i < 8; i++) Call(camioneros, "Step", .5f);
                yield return Shot(tag + "-09-camioneros");
                Return(camioneros);
            }
        }

        // 2026-11-26 (user report on Los Redondos): with the user's crew a Parrillero stood forever by the barrels holding a cooked sandwich
        // (its handoff point had no walkable way) and the Mozos stood idle while fans asking for choris on the table walked past (one fan
        // ahead waiting for an uncooked sandwich held the whole role). Neither may last more than a moment now.
        [UnityTest, Timeout(300000)]
        public IEnumerator LosRedondosCooksNeverHoldASandwichAndMozosServeWhatIsOnTheTable()
        {
            yield return SetSize(610, 1356);
            EnterFromSelector(10);
            object mode = Get(view, "towerDefenseMode");
            Set(mode, "coins", 100000);
            int[] roles = { 0, 0, 1, 3, 4, 4, 4 };
            int hired = 0;
            object kitchen = Get(mode, "Kitchen");
            MethodInfo available = kitchen.GetType().GetMethod("AvailableTableCount");
            var holding = new Dictionary<int, float>();
            float t = 0f, longestHold = 0f, mozoStall = 0f, longestMozoStall = 0f;
            for (int tick = 0; tick < 6000 && Get(mode, "Phase").ToString() == "Playing"; tick++)
            {
                t += .05f;
                if (hired < roles.Length && Call(game, "TryHireTowerDefenseAtPost", mode, Role(roles[hired])) is bool ok && ok) hired++;
                Call(mode, "Step", .05f);
                int idleMozos = 0;
                foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                {
                    int id = (int)Get(worker, "Id"), cookUnit = (int)Get(worker, "CookUnitId");
                    bool carrying = false;
                    if (cookUnit > 0 && !(bool)Get(worker, "IsMoving"))
                        foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
                            if ((int)Get(unit, "Id") == cookUnit && Get(unit, "Location").ToString() == "Carried") carrying = true;
                    holding[id] = carrying ? (holding.TryGetValue(id, out float held) ? held : 0f) + .05f : 0f;
                    longestHold = Mathf.Max(longestHold, holding[id]);
                    if (Get(worker, "Role").ToString() == "Mozo" && !(bool)Get(worker, "IsMoving") && (int)Get(worker, "ServiceStage") == 0) idleMozos++;
                }
                // A fan well inside the fence asks for a sandwich that is on the table and no fan ahead is waiting for that same sandwich.
                bool servable = false;
                var fans = new List<object>();
                foreach (object fan in (IEnumerable)Get(mode, "Fans")) if (!(bool)Get(fan, "OrderComplete")) fans.Add(fan);
                fans.Sort((a, b) => ((float)Get(Get(b, "RouteFollower"), "Progress")).CompareTo((float)Get(Get(a, "RouteFollower"), "Progress")));
                var waitedAhead = new HashSet<int>();
                foreach (object fan in fans)
                {
                    float progress = (float)Get(Get(fan, "RouteFollower"), "Progress");
                    bool reserved = (int)Get(fan, "ReservedByWorker") != 0;
                    foreach (object line in (IEnumerable)Get(fan, "Order"))
                    {
                        int product = (int)Get(line, "Product");
                        if ((int)Get(line, "Remaining") <= 0 || product >= 4) continue;
                        if (!reserved && progress > 2.5f && !waitedAhead.Contains(product) && (int)available.Invoke(kitchen, new object[] { product, 0 }) > 0) servable = true;
                        waitedAhead.Add(product);
                    }
                }
                mozoStall = idleMozos >= 2 && servable ? mozoStall + .05f : 0f;
                longestMozoStall = Mathf.Max(longestMozoStall, mozoStall);
                if (tick % 200 == 0) yield return null;
            }
            Assert.AreEqual(7, hired);
            Assert.Less(longestHold, 3f, "a cook never stands still holding a cooked sandwich");
            Assert.Less(longestMozoStall, 3f, "Mozos do not stand idle while a fan asks for what is on the table");
            Return(mode);
        }

        // 2026-11-26 diagnosis (user report on Los Redondos): a Parrillero stood stuck by the barrels and the Mozos stood idle while fans
        // with orders walked past. Plays the user's crew (3 P, 1 Pr, 1 C, 1 F, 4 M) and writes a trace of every worker.
        [UnityTest, Timeout(300000), Explicit("Diagnostic trace to Logs/Acceptance/RedondosStall-20261126")]
        public IEnumerator LosRedondosCrewStallTrace()
        {
            yield return SetSize(610, 1356);
            EnterFromSelector(10);
            object mode = Get(view, "towerDefenseMode");
            Set(mode, "coins", 100000);
            int[] roles = { 0, 0, 1, 3, 4, 4, 4 };
            int hired = 0;
            object kitchen = Get(mode, "Kitchen");
            var log = new System.Text.StringBuilder();
            float t = 0f;
            for (int tick = 0; tick < 6000 && Get(mode, "Phase").ToString() == "Playing"; tick++)
            {
                t += .05f;
                if (hired < roles.Length && Call(game, "TryHireTowerDefenseAtPost", mode, Role(roles[hired])) is bool ok && ok) hired++;
                Call(mode, "Step", .05f);
                if (tick % 40 != 0) continue;
                log.Append("t=").Append(t.ToString("0.0")).Append(" served=").Append(Get(mode, "ServedFans")).Append(" table=");
                for (int product = 0; product < 7; product++)
                    log.Append((int)kitchen.GetType().GetMethod("AvailableTableCount").Invoke(kitchen, new object[] { product, 0 })).Append(product < 6 ? "," : "");
                log.Append('\n');
                foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                {
                    log.Append("  W").Append(Get(worker, "Id")).Append(' ').Append(Get(worker, "Role")).Append(" pos=").Append(((Vector2)Get(worker, "Position")).ToString("F2"))
                        .Append(" post=").Append(((Vector2)Get(worker, "PostPosition")).ToString("F2"))
                        .Append(" mov=").Append(Get(worker, "IsMoving")).Append(" cook=").Append(Get(worker, "IsCooking")).Append(" stage=").Append(Get(worker, "ServiceStage"))
                        .Append(" fan=").Append(Get(worker, "PendingFanId")).Append(" prod=").Append(Get(worker, "PendingProduct")).Append(" cookUnit=").Append(Get(worker, "CookUnitId"))
                        .Append(" handoff=").Append(Get(worker, "CookHandoffSeconds"));
                int cookUnit = (int)Get(worker, "CookUnitId");
                if (cookUnit > 0 && !(bool)Get(worker, "IsMoving") && !(bool)Get(worker, "IsCooking"))
                {
                    foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
                        if ((int)Get(unit, "Id") == cookUnit) log.Append(" unit=").Append(Get(unit, "State")).Append('@').Append(Get(unit, "Location")).Append(" slot=").Append(Get(unit, "Slot")).Append(" p=").Append(Get(unit, "Progress"));
                    MethodInfo handoffOf = mode.GetType().GetMethod("CookTableHandoffPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo findStation = mode.GetType().GetMethod("FindPlacedStation", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool premium = Get(worker, "Role").ToString() == "ParrilleroPremium";
                    object table = findStation.Invoke(mode, new[] { Enum.Parse(RuntimeType("TowerDefenseStationKind"), premium ? "PremiumTable" : "NormalTable") });
                    Vector2 handoffAt = (Vector2)handoffOf.Invoke(mode, new[] { worker, table });
                    log.Append(" handoffAt=").Append(handoffAt.ToString("F2"));
                    MethodInfo path = mode.GetType().GetMethod("TryBuildWorkerPath", BindingFlags.Instance | BindingFlags.NonPublic);
                    object[] pathArgs = { worker, Get(worker, "Position"), handoffAt, Rect.zero, null, Role(0) };
                    if (!float.IsNaN(handoffAt.x)) log.Append(" path=").Append(path.Invoke(mode, pathArgs));
                }
                log.Append('\n');
                }
                foreach (object fan in (IEnumerable)Get(mode, "Fans"))
                {
                    if ((bool)Get(fan, "OrderComplete")) continue;
                    log.Append("  F").Append(Get(fan, "Id")).Append(" res=").Append(Get(fan, "ReservedByWorker")).Append(" prog=")
                        .Append(((float)Get(Get(fan, "RouteFollower"), "Progress")).ToString("0.00")).Append('/').Append(((float)Get(Get(fan, "RouteFollower"), "TotalDistance")).ToString("0.00")).Append(" order=");
                    foreach (object line in (IEnumerable)Get(fan, "Order")) log.Append(Get(line, "Product")).Append('x').Append(Get(line, "Remaining")).Append(' ');
                    log.Append('\n');
                }
                if (tick % 400 == 0) yield return null;
            }
            log.Append("END phase=").Append(Get(mode, "Phase")).Append(" served=").Append(Get(mode, "ServedFans")).Append(" t=").Append(t);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RedondosStall-20261126"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "trace.txt"), log.ToString());
            Return(mode);
        }

        // 2026-11-26 (a pedido): Defense orders of four different products show four rows in a taller bubble.
        [UnityTest, Timeout(120000)]
        public IEnumerator LosRedondosFourProductOrderShowsFourRows()
        {
            yield return SetSize(575, 1280);
            EnterFromSelector(10);
            object mode = Get(view, "towerDefenseMode");
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 0, 2, 4, 6 }, new[] { 5, 4, 5, 4 }));
            for (int i = 0; i < 6; i++) Call(mode, "Step", 1f);
            MethodInfo bounds = view.GetType().GetMethod("TowerDefenseOrderBubbleBounds", BindingFlags.NonPublic | BindingFlags.Static);
            Rect two = (Rect)bounds.Invoke(null, new object[] { new Vector2(200f, 400f), 2, 10 });
            Rect four = (Rect)bounds.Invoke(null, new object[] { new Vector2(200f, 400f), 4, 10 });
            Assert.AreEqual(two.yMax, four.yMax, .01f, "the tail stays in place");
            Assert.Greater(four.height, two.height + 30f, "two more rows");
            yield return Capture("redondos-pedido-4-productos");
            Return(mode);
        }

        // 2026-11-24 (a pedido): Los Redondos plays La Renga's C-shaped fence inverted, with La Renga's kitchen, crew and difficulty.
        [UnityTest, Timeout(120000)]
        public IEnumerator LosRedondosOpensOnTheInvertedCWithLaRengasKitchenCrewAndEconomy()
        {
            yield return SetSize(575, 1280);
            EnterFromSelector(10);
            Assert.AreEqual(true, Get(view, "towerDefenseActive"), Get(view, "towerDefenseError")?.ToString());
            object mode = Get(view, "towerDefenseMode");
            object config = Get(view, "towerDefenseSourceConfig");
            Assert.IsTrue((bool)Get(mode, "IsCShapedLevel"), "La Renga's C-shaped single fence.");
            Assert.IsTrue((bool)Get(config, "MirrorHorizontally"), "Drawn inverted.");
            Assert.IsTrue((bool)Get(config, "UsesLaRengaDynamics"));
            Assert.IsTrue((bool)Get(config, "ActiveKitchen"), "Racing's active kitchen (2026-11-24).");
            Assert.IsFalse((bool)Get(mode, "IsPassiveKitchenLevel"), "Parrilleros cook and Mozos deliver.");
            Assert.AreEqual(40, (int)Get(mode, "Coins"));
            Assert.AreEqual(240, (int)Get(mode, "TotalFanCount"));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, (int[])Get(config, "AvailableProducts"), "Racing's seven products.");
            CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1 }, (int[])Get(mode, "WorkerCountsByRole"), "Racing's crew: a Parrillero, a Premium and a Mozo.");
            Assert.AreEqual(7, ((ICollection)Get(mode, "PlacedStations")).Count, "Racing's seven stations.");
            Assert.IsTrue((bool)Get(config, "HasDifficultyRamp"));
            // 2026-11-26 (a pedido): the first Fernetero costs 120 (it was Racing's 200) and the next ones grow from there.
            Assert.AreEqual(120, (int)Call(mode, "HireCost", Role(3)), "first Fernetero");
            // 2026-11-26 (a pedido): the Premium cooks in its own outfit, not with the Parrillero's white-apron poses.
            var cookPoses = (UnityEngine.Sprite[])Call(view, "CreateTowerDefenseHandoffFrames", Role(0));
            var premiumPoses = (UnityEngine.Sprite[])Call(view, "CreateTowerDefenseHandoffFrames", Role(2));
            Assert.AreEqual(cookPoses.Length, premiumPoses.Length, "the Premium has the same cooking poses");
            Assert.Greater(premiumPoses.Length, 0);
            Assert.AreNotSame(cookPoses[0].texture, premiumPoses[0].texture, "recolored, not the Parrillero's apron");
            yield return Capture("redondos-invertida-apertura");
            Return(mode);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LaRengaIdleCrewWaitsBetweenFenceAndTablesFacingTheFenceAndChantsWhenItRunsOutOfWork()
        {
            yield return SetSize(610, 1356);
            object mode = OpenLaRenga();
            Set(mode, "coins", 100000);
            foreach (int role in new[] { 0, 0, 2, 1, 1, 1 })
                Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(role)));
            float tablesTop = ((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                .Where(station => Get(station, "Kind").ToString().EndsWith("Table")).Max(station => ((Rect)Get(station, "Bounds")).yMax);
            for (int tick = 0; tick < 24; tick++) Call(mode, "Step", .25f);
            yield return null; yield return null;
            var workerViews = (IDictionary)Get(Get(view, "towerDefensePresentation"), "workerViews");
            int waiting = 0;
            foreach (object worker in (IEnumerable)Get(mode, "Workers"))
            {
                if (Get(worker, "Role").ToString().StartsWith("Parrillero") == false || (bool)Get(worker, "IsMoving")) continue;
                Assert.Greater(((Vector2)Get(worker, "Position")).y, tablesTop, "A waiting cook stays above the tables, between them and the fence.");
                Assert.AreEqual("Up", Get(workerViews[Get(worker, "Id")], "Facing").ToString(), "A waiting cook looks at the first fence.");
                waiting++;
            }
            Assert.GreaterOrEqual(waiting, 3);

            // Run until each kind of worker has been away working and settled again: the chant starts once and lasts about a second.
            var starts = (IDictionary)Get(view, "crewChantStart");
            var roleOf = new Dictionary<int, string>();
            foreach (object worker in (IEnumerable)Get(mode, "Workers")) roleOf[(int)Get(worker, "Id")] = Get(worker, "Role").ToString();
            var chanted = new HashSet<string>();
            for (int tick = 0; tick < 4000 && chanted.Count < 3; tick++)
            {
                Call(mode, "Step", .1f);
                if (tick % 2 == 0) yield return null;
                foreach (DictionaryEntry entry in starts)
                {
                    string role = roleOf[(int)entry.Key];
                    if (chanted.Contains(role) || (float)Get(mode, "Elapsed") - (float)entry.Value > .35f) continue;
                    chanted.Add(role);
                    yield return Capture("dialogo-" + role);
                }
            }
            CollectionAssert.AreEquivalent(new[] { "Parrillero", "ParrilleroPremium", "Cocacolero" }, chanted, "Each kind of worker calls out its line once it has settled.");
            Return(mode);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LosRedondosShowsSixtyDegreeArtOnTheVerticalFence()
        {
            yield return SetSize(575, 1280);
            EnterFromSelector(10);
            Assert.IsTrue((bool)Get(view, "towerDefenseActive"), Get(view, "towerDefenseError")?.ToString());
            object mode = Get(view, "towerDefenseMode");
            var generator = (Component)Get(Get(view, "towerDefensePresentation"), "fenceGenerator");
            var panels = generator.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => renderer.name.StartsWith("Fence rail ", StringComparison.Ordinal)).ToArray();
            Assert.IsTrue(panels.Any(renderer => Mathf.Abs(renderer.transform.right.y) > .9f));
            Assert.IsTrue(panels.Any(renderer => Mathf.Abs(renderer.transform.right.x) > .9f));
            foreach (SpriteRenderer panel in panels)
                Assert.AreEqual(Mathf.Abs(panel.transform.right.y) > .9f ? "fence-panel-overhead-60-v1" : "fence-panel-v1",
                    panel.sprite.texture.name, panel.name);
            for (int i = 0; i < 16; i++) Call(mode, "Step", .25f);
            yield return Capture("los-redondos-valla-vertical-60-575x1280");
            Return(mode);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LosRedondosAndLujanShowCrewChantsAtTheirExistingPosts()
        {
            yield return SetSize(575, 1280);
            foreach (int level in new[] { 10, 14 })
            {
                EnterFromSelector(level);
                Assert.IsTrue((bool)Get(view, "towerDefenseActive"), Get(view, "towerDefenseError")?.ToString());
                object mode = Get(view, "towerDefenseMode");
                Assert.IsTrue((bool)Call(mode, "ContainsProduct", 3), "The dialogue should advertise Vacío in both levels.");

                Set(mode, "coins", 100000);
                var workers = (IEnumerable)Get(mode, "Workers");
                foreach (string role in Enum.GetNames(RuntimeType("StreetWorkerRole")))
                {
                    if (!workers.Cast<object>().Any(w => Get(w, "Role").ToString() == role))
                    {
                        object workerRole = Enum.Parse(RuntimeType("StreetWorkerRole"), role);
                        Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, workerRole));
                    }
                }

                var premium = workers.Cast<object>().First(w => Get(w, "Role").ToString() == "ParrilleroPremium");
                var fernetero = workers.Cast<object>().First(w => Get(w, "Role").ToString() == "Fernetero");
                // Freeze time in the fixture. Place each worker at the actual assigned idle point
                // and drive the presentation with an explicit busy → idle transition.
                foreach (object worker in workers)
                {
                    worker.GetType().GetProperty("Position", I).SetValue(worker, Call(mode, "WaitSpot", worker));
                    worker.GetType().GetProperty("IsMoving", I).SetValue(worker, false);
                }
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.AreSame(mode, Get(view, "crewChantMode"), "The view must process crew chants in the active match.");
                var starts = (IDictionary)Get(view, "crewChantStart");
                Assert.IsEmpty(starts, "Newly hired or initially settled workers do not call out.");
                var workerViews = (IDictionary)Get(Get(view, "towerDefensePresentation"), "workerViews");
                foreach (object worker in workers)
                {
                    object workerView = workerViews[Get(worker, "Id")];
                    Assert.AreEqual("Up", Get(workerView, "Facing").ToString(), $"Level {level}, {Get(worker, "Role")}");
                    if (level == 14)
                        Assert.AreSame(Get(workerView, "RearIdleFrame"), ((SpriteRenderer)Get(workerView, "Renderer")).sprite);
                }

                Set(premium, "CookUnitId", 1);
                Set(fernetero, "ServiceStage", 1);
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.IsEmpty(starts, "A cook with assigned food and a worker serving stay silent.");

                Set(premium, "CookUnitId", 0);
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.IsTrue(starts.Contains(Get(premium, "Id")));
                Assert.IsFalse(starts.Contains(Get(fernetero, "Id")));
                float premiumStart = (float)starts[Get(premium, "Id")];
                yield return Capture($"crew-chant-level-{level}-premium");

                Set(mode, "elapsed", premiumStart + 2f);
                Set(fernetero, "ServiceStage", 0);
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.AreEqual(premiumStart + 2f, starts[Get(fernetero, "Id")]);
                Assert.AreEqual(premiumStart, starts[Get(premium, "Id")], "A worker still waiting must not restart its chant.");
                yield return Capture($"crew-chant-level-{level}-fernet");
                Return(mode);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LosRedondosAndLujanMozoLooksAtTheMissingSandwichTable()
        {
            yield return SetSize(575, 1280);
            foreach (int level in new[] { 10, 14 })
            {
                EnterFromSelector(level);
                Assert.IsTrue((bool)Get(view, "towerDefenseActive"), Get(view, "towerDefenseError")?.ToString());
                object mode = Get(view, "towerDefenseMode");
                Set(mode, "coins", 100000);
                var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>();
                if (!workers.Any(w => Get(w, "Role").ToString() == "Mozo"))
                    Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(4)));
                object mozo = workers.Single(w => Get(w, "Role").ToString() == "Mozo");
                Vector2 wait = (Vector2)Call(mode, "WaitSpot", mozo);
                mozo.GetType().GetProperty("Position", I).SetValue(mozo, wait);
                mozo.GetType().GetProperty("IsMoving", I).SetValue(mozo, false);
                object presentation = Get(view, "towerDefensePresentation");
                var workerViews = (IDictionary)Get(presentation, "workerViews");

                Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 3 }, new[] { 1 }));
                object fan = ((IEnumerable)Get(mode, "Fans")).Cast<object>().Single();
                object follower = Get(fan, "RouteFollower");
                Call(follower, "Advance", (float)Get(follower, "TotalDistance") * .18f / (float)Get(follower, "Speed"));
                Call(mode, "DeliverWithinServiceRanges");
                Call(presentation, "Sync", mode);
                var args = new[] { mozo, (object)Vector2.zero };
                Assert.IsTrue((bool)Call(mode, "TryGetMozoWaitingSandwichTable", args));
                object table = ((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                    .Single(station => Get(station, "Kind").ToString() == "PremiumTable");
                Vector2 target = ((Rect)Get(table, "Bounds")).center;
                Assert.AreEqual(target, args[1], $"Level {level}: missing Vacío targets the Premium table.");
                Assert.AreEqual(wait, Get(mozo, "Position"), "The cue only turns the Mozo; it must not move him.");
                Vector2 delta = target - wait;
                Type facing = RuntimeType("StreetFacing");
                object expectedFacing = RuntimeType("StreetWorkerAnimation").GetMethod("ResolveFacing", S)
                    .Invoke(null, new[] { (object)new Vector2(delta.x, -delta.y), Enum.Parse(facing, "Up") });
                object workerView = workerViews[Get(mozo, "Id")];
                Assert.AreEqual(expectedFacing, Get(workerView, "Facing"));
                var cue = (SpriteRenderer)Get(workerView, "MozoAngryCue");
                Assert.IsTrue(cue != null && cue.enabled);
                Assert.Greater(cue.transform.position.y, wait.y);
                yield return Capture($"mozo-missing-vacio-{level}");

                Set(fan, "ReservedByWorker", int.MaxValue);
                Call(mode, "DeliverWithinServiceRanges");
                Call(presentation, "Sync", mode);
                Assert.IsFalse(cue.enabled, "An already assigned order must not leave an idle Mozo angry.");
                Assert.AreEqual("Up", Get(workerView, "Facing").ToString(), "Without the cue, look toward the first fence again.");
                Return(mode);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator CamionerosWorkersCostWhatNuevaChicagoChargesAndGrowTheSameWay()
        {
            yield return SetSize(610, 1356);
            EnterFromSelector(9);
            object mode = Get(view, "towerDefenseMode");
            Set(mode, "coins", 100000);
            // 2026-11-23 (a pedido): the previous normal level's prices (Nueva Chicago base 20/20) and its growth (x1, x2.5, x5, ...).
            int[] expectedPrice = { 20, 20, 50, 50, 100, 100 };
            int[] roles = { 0, 1, 0, 1, 0, 1 };
            for (int i = 0; i < roles.Length; i++)
            {
                Assert.AreEqual(expectedPrice[i], (int)Call(mode, "HireCost", Role(roles[i])), "hire " + i + " role " + roles[i]);
                Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(roles[i])), "hire " + i + " role " + roles[i] + " coins " + Get(mode, "Coins"));
                for (int tick = 0; tick < 80; tick++) Call(mode, "Step", .25f); // one at a time: the strip jams if several cooks are hired in the same instant
            }
            // 2026-11-26 (a pedido): at the cap of 3 per role the card reads "MAX" (cost 0) and nothing more can be bought.
            foreach (int role in new[] { 0, 1 })
            {
                Assert.AreEqual(0, (int)Call(mode, "HireCost", Role(role)), "role " + role + " at its cap shows MAX");
                Assert.IsFalse((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Role(role)), "role " + role + " cannot be hired past 3");
            }
            Return(mode);
        }

        [UnityTest, Timeout(220000)]
        public IEnumerator LaRengaRealWaveKeepsTheAuditedOutcomeAndDeliveryCount()
        {
            foreach (var (width, height, expectedWins, expectedFans, expectedUnits) in new[]
                     // Fans at half speed (fanSpeed 0.1875) sandwiches taken from behind the table and campaign waiting spots (2026-11-21); the previous audit at 0.375 gave 145 and a 66/77 loss.
                     // 2026-11-23: with the difficulty ramp (speed x1.00..x1.40, bigger and double orders) the same scripted crew still wins 120/120 but delivers 220 units.
                     // 2026-11-23 (a pedido): 200 fans and the ramp up to x1.72. The old eight-hire crew now loses at 150/200 (334 units); the crew below hires up to 24
                     // staff as coins allow (the per-role cap is gone), wins 200/200 and delivers 514 units.
                     // 2026-11-23 (a pedido): La Renga starts with 0 coins, one Parrillero and one Cocacolero and prices workers like Ferro (30/30/35, x2.5, x5, ...).
                     // The same scripted crew now loses at 178/200 (444 units) in 540x960 but still wins 200/200 (514 units) in the taller 610x1356; no scripted strategy of the 540x960 sweep won (best 180/200).
                     // 2026-11-24 (a pedido): ramp 'E' (orders of 3 units and second products from early on) plus two extra tiers of 20 -> 240 fans, x1.80 and x1.88. With 40 coins and no starting Cocacolero the same scripted crew
                     // now LOSES: 212/240 (900 units) in 540x960 and 120/240 (366 units) in 610x1356 (the Parrillero strip jams; one scripted strategy only, not a balance certificate).
                     // 2026-11-23 (a pedido): La Renga pays Ferro's prices for the speed upgrade (20, 35, 50, ...). 2026-11-24 (a pedido): it starts with 0 coins again; the same crew still wins 200/200 (514 units) in both sizes.
                     { (540, 960, false, 211, 897), (610, 1356, false, 120, 366) })
            {
                yield return SetSize(width, height);
                object mode = OpenLaRenga();
                int[] hires = { 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2 };
                int hired = 0;
                int checks = 0;
                var stations = ((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                    .Select(station => (Rect)Get(station, "Bounds")).ToArray();
                for (int tick = 0; tick < 6000 && Get(mode, "Phase").ToString() == "Playing"; tick++)
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
                Debug.Log("Renga real wave observed: " + width + "x" + height + "; won=" + Get(mode, "HasWon") + "; served=" +
                    Get(mode, "ServedFans") + "; deliveries=" + Get(mode, "TotalDelivered"));
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
