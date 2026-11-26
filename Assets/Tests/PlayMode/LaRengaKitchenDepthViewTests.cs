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
        public IEnumerator CamionerosCrewHiredInQuickSuccessionNeverGetsStuckAndServesEveryFan()
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
            Assert.AreEqual("Finished", Get(mode, "Phase").ToString());
            Assert.IsTrue((bool)Get(mode, "HasWon"), "Every fan is served.");
            Assert.AreEqual(120, Get(mode, "ServedFans"));
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
