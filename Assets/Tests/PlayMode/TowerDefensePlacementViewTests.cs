#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    /// <summary>Real TD UI with a disposable in-memory campaign profile, never PlayerPrefs/cloud.</summary>
    public sealed class TowerDefensePlacementViewTests
    {
        const BindingFlags I = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags S = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        GameObject root, cameraRoot;
        Component game, view;
        Camera camera;
        Type gt, vt;
        object mode;
        bool failSaves, previousGate;
        int savedCalls;
        float previousTime;
        FieldInfo editorGate;
        UnityEditor.EditorWindow gameWindow;
        PropertyInfo selectedSize;
        object sizeGroup;
        int previousSize, addedSize = -1;
        public static string CaptureDirectory => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Logs/Acceptance/TowerDefensePlacement/TenFans20261106"));

        static Type T(string n) => Type.GetType("HayChoriYPaty." + n + ", Assembly-CSharp", true);
        static object Get(object o, string n)
        {
            PropertyInfo p = o.GetType().GetProperty(n, I);
            return p != null ? p.GetValue(o) : o.GetType().GetField(n, I).GetValue(o);
        }
        static void Set(object o, string n, object v) => o.GetType().GetField(n, I).SetValue(o, v);
        static object Call(object o, string n, params object[] args)
        {
            try { return o.GetType().GetMethod(n, I).Invoke(o, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException; }
        }
        bool Persist<TMatch>(string json, TMatch match)
        {
            savedCalls++;
            return !failSaves;
        }
        void SetPortrait()
        {
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            gameWindow = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            selectedSize = gameWindow.GetType().GetProperty("selectedSizeIndex", I);
            previousSize = (int)selectedSize.GetValue(gameWindow);
            Type registryType = editor.GetType("UnityEditor.GameViewSizes");
            object registry = registryType.GetProperty("instance", S).GetValue(null);
            object groupKind = registryType.GetProperty("currentGroupType", I).GetValue(registry);
            sizeGroup = registryType.GetMethod("GetGroup", I).Invoke(registry, new[] {groupKind});
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"),
                new object[] {Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"),
                    540, 960, "TD placement isolated QA"});
            sizeGroup.GetType().GetMethod("AddCustomSize", I).Invoke(sizeGroup, new[] {size});
            addedSize = (int)sizeGroup.GetType().GetMethod("GetTotalCount", I).Invoke(sizeGroup, null) - 1;
            selectedSize.SetValue(gameWindow, addedSize); gameWindow.Repaint();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTime = Time.timeScale;
            gt = T("StreetGame"); vt = T("StreetView");
            editorGate = gt.GetField("AllowEditorTestInitialization", S);
            previousGate = (bool)editorGate.GetValue(null); editorGate.SetValue(null, false);
            Type firebase = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            Assert.IsNull(firebase.GetProperty("Instance", S).GetValue(null), "Cloud must be suppressed by isolated test setup.");
            SetPortrait(); yield return null; yield return null;
            cameraRoot = new GameObject("TD placement test camera"); cameraRoot.tag = "MainCamera";
            camera = cameraRoot.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = Color.black;
            Assert.AreSame(camera, Camera.main);
            root = new GameObject("TD placement memory-only campaign"); root.SetActive(false);
            game = root.AddComponent(gt); root.SetActive(true);
            object seed = Activator.CreateInstance(gt.GetNestedType("SaveData", BindingFlags.Public));
            Set(seed, "version", 5); Set(seed, "unlockedLevel", 10);
            Type match = T("StreetMatchRecord");
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), match, typeof(bool));
            Delegate persist = GetType().GetMethod("Persist", I).MakeGenericMethod(match).CreateDelegate(callback, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", JsonUtility.ToJson(seed), persist));
            Assert.IsTrue((bool)Call(game, "SelectLevel", 9));
            // No legacy quota stock is injected: this finite Defense run must start on its own rules.
            view = root.AddComponent(vt);
            Set(view, "introActive", false); Set(view, "hubActive", false);
            Set(view, "levelSelectActive", false); Set(view, "marketActive", false);
            float deadline = Time.realtimeSinceStartup + 90f;
            while (!(bool)Get(view, "viewResourcesReady") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)Get(view, "viewResourcesReady"));
            Set(view, "introActive", false);
            Time.timeScale = 0f;
            Assert.IsTrue((bool)Call(view, "StartTowerDefenseRun"), (string)Get(view, "towerDefenseError"));
            mode = Get(view, "towerDefenseMode");
            Assert.AreEqual(0, ((ICollection)Get(mode, "Workers")).Count);
            Assert.IsTrue((bool)Get(game, "IsPlayerActive"));
            yield return null; yield return null;
        }

        Rect[] Cards() => (Rect[])vt.GetMethod("TowerDefenseHireBounds", S).Invoke(null, new object[] {960f});
        Rect[] Areas() => (Rect[])Call(view, "TowerDefensePlacementAreaBounds", new Rect(0, 0, 540, 960));
        int Selected => (int)Get(view, "towerDefenseSelectedRole");
        int Coins => (int)Get(mode, "Coins");
        object Role(string role) => Enum.Parse(T("StreetWorkerRole"), role);
        int Count(string role, int zone) => (int)Call(mode, "WorkersInZone", Role(role), zone);
        IEnumerator Tap(Vector2 logical)
        {
            Vector2 pixel = new Vector2(logical.x, 960f - logical.y);
            Call(view, "HandlePointer", pixel, true, false);
            Call(view, "HandlePointer", pixel, false, true);
            yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.AreEqual(540, image.width); Assert.AreEqual(960, image.height);
            Directory.CreateDirectory(CaptureDirectory);
            File.WriteAllBytes(Path.Combine(CaptureDirectory, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }

        [UnityTest]
        public IEnumerator SelectionShadesBothAreasWithoutSpendingThenPlacesInChosenAreaOnce()
        {
            for (int n = 0; n < 9; n++)
            {
                Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {0, 4}, new[] {1, 1}));
                IList fans = (IList)Get(mode, "Fans"); object follower = Get(fans[fans.Count - 1], "RouteFollower");
                Call(follower, "Advance", (float)Get(follower, "TotalDistance") * (.03f + n * .104f) / (float)Get(follower, "Speed"));
            }
            yield return Capture("initial-empty-workers");
            Rect[] cards = Cards(); Rect[] areas = Areas();
            Assert.AreEqual(2, areas.Length);
            Assert.Greater(areas[0].height, 40f); Assert.Greater(areas[1].height, 40f);
            Assert.IsFalse(areas[0].Overlaps(areas[1]));
            int coins = Coins, saves = savedCalls;
            yield return Tap(cards[1].center);
            Assert.AreEqual(1, Selected); Assert.AreEqual(coins, Coins); Assert.AreEqual(saves, savedCalls);
            Assert.AreEqual(7830, (int)Call(view, "HitTowerDefenseAction", areas[0].center));
            Assert.AreEqual(7831, (int)Call(view, "HitTowerDefenseAction", areas[1].center));
            yield return Capture("cocacolero-selected-shaded-areas");
            yield return Tap(cards[1].center); Assert.AreEqual(-1, Selected);
            yield return Tap(cards[0].center); Assert.AreEqual(0, Selected);
            yield return Tap(cards[1].center); Assert.AreEqual(1, Selected);
            Assert.AreEqual(coins, Coins); Assert.AreEqual(saves, savedCalls);
            int cost = (int)Call(mode, "HireCost", Role("Cocacolero"));
            yield return Tap(areas[1].center);
            Assert.AreEqual(-1, Selected); Assert.AreEqual(coins - cost, Coins);
            Assert.AreEqual(1, Count("Cocacolero", 1)); Assert.AreEqual(0, Count("Cocacolero", 0));
            Assert.AreEqual(saves + 1, savedCalls);
            yield return Tap(areas[1].center);
            Assert.AreEqual(coins - cost, Coins); Assert.AreEqual(1, Count("Cocacolero", 1));
            yield return Capture("cocacolero-placed-lower-area");
        }

        [UnityTest]
        public IEnumerator FullAreaRejectsWithoutSpendAndFailedSaveCannotRepeatPlacement()
        {
            Set(mode, "coins", 100000);
            Rect[] cards = Cards(); Rect[] areas = Areas();
            foreach (int card in new[] {0, 1})
            {
                for (int count = 0; count < 3; count++)
                {
                    yield return Tap(cards[card].center); yield return Tap(areas[0].center);
                }
            }
            Assert.AreEqual(3, Count("Parrillero", 0)); Assert.AreEqual(3, Count("Cocacolero", 0));
            int coins = Coins, saves = savedCalls;
            yield return Tap(cards[0].center); yield return Tap(areas[0].center);
            Assert.AreEqual(0, Selected); Assert.AreEqual(coins, Coins); Assert.AreEqual(saves, savedCalls);
            yield return Capture("full-area-rejected");
            yield return Tap(areas[1].center);
            Assert.AreEqual(1, Count("Parrillero", 1)); Assert.AreEqual(-1, Selected);
            failSaves = true;
            yield return Tap(cards[1].center); yield return Tap(areas[1].center);
            Assert.IsFalse((bool)Get(game, "IsPlayerActive")); Assert.AreEqual(-1, Selected);
            Assert.AreEqual(1, Count("Cocacolero", 1));
            coins = Coins; saves = savedCalls;
            yield return Tap(cards[1].center); yield return Tap(areas[1].center);
            Assert.AreEqual(coins, Coins); Assert.AreEqual(saves, savedCalls);
            Assert.AreEqual(1, Count("Cocacolero", 1));
        }

        [UnityTest]
        public IEnumerator CompleteRequestFadesExistingFanThenRemovesItWithoutWinningAnIncompleteWave()
        {
            Rect[] cards = Cards(), areas = Areas();
            yield return Tap(cards[1].center); yield return Tap(areas[0].center);
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {0, 4}, new[] {1, 1}));
            object fan = ((IList)Get(mode, "Fans"))[0], follower = Get(fan, "RouteFollower");
            Call(follower, "Advance", (float)Get(follower, "TotalDistance") * .48f / (float)Get(follower, "Speed"));
            Call(mode, "DeliverWithinServiceRanges");
            Assert.AreEqual(1, Get(fan, "PendingUnits"));
            Assert.IsFalse((bool)Get(fan, "IsDeparting"), "A partial order must stay on the path.");
            yield return Capture("partial-order-coins-only-hud");

            yield return Tap(cards[0].center); yield return Tap(areas[0].center);
            object worker = ((IList)Get(mode, "Workers"))[1];
            int workerId = (int)Get(worker, "Id");
            object kitchen = Get(mode, "Kitchen");
            object unit = kitchen.GetType().GetMethod("ReserveCook", new[] {typeof(int), typeof(int)})
                .Invoke(kitchen, new object[] {0, workerId});
            Assert.IsNotNull(unit);
            int unitId = (int)Get(unit, "Id");
            Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", unitId, workerId, 100f, 1f));
            Assert.IsTrue((bool)Call(kitchen, "TakeCooked", unitId, workerId));
            Assert.IsTrue((bool)Call(kitchen, "DepositCooked", unitId, workerId));
            Call(mode, "DeliverWithinServiceRanges");
            Assert.IsTrue((bool)Get(fan, "IsDeparting")); Assert.AreEqual(0, Get(fan, "PendingUnits"));
            Vector2 position = (Vector2)Get(follower, "Position");
            follower.GetType().GetProperty("Speed", I).SetValue(follower, 100f);
            object presentation = Get(view, "towerDefensePresentation");
            Call(presentation, "Sync", mode);
            IDictionary fanViews = (IDictionary)Get(presentation, "activeFans");
            SpriteRenderer renderer = (SpriteRenderer)Get(fanViews[(int)Get(fan, "Id")], "Renderer");
            float scale = renderer.transform.localScale.x;
            yield return Capture("completed-order-animation-start");
            Call(mode, "AdvanceFans", .21f); Call(presentation, "Sync", mode);
            Assert.AreEqual(position, Get(follower, "Position"), "Completed supporter must not keep walking during departure.");
            Assert.That((float)Get(fan, "DepartureProgress"), Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(renderer.color.a, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.Less(renderer.transform.localScale.x, scale);
            Assert.Greater(renderer.transform.position.y, position.y);
            Assert.IsFalse((bool)Get(mode, "HasWon"));
            yield return Capture("completed-order-animation-midpoint");
            Call(mode, "AdvanceFans", .30f); Call(presentation, "Sync", mode);
            Assert.AreEqual(0, ((IList)Get(mode, "Fans")).Count);
            Assert.AreEqual(0, fanViews.Count);
            Assert.IsFalse((bool)Get(mode, "HasWon"), "Empty scene is not victory until all ten fans were served.");
            Assert.AreEqual(1, Get(mode, "SpawnedFans")); Assert.AreEqual(1, Get(mode, "ServedFans"));
            yield return Capture("completed-order-removed-wave-not-finished");
        }

        [UnityTest]
        public IEnumerator FirstEscapedFanSettlesDefenseLossAndDoesNotRestoreLegacyHud()
        {
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {4}, new[] {1}));
            object fan = ((IList)Get(mode, "Fans"))[0], follower = Get(fan, "RouteFollower");
            Call(follower, "Advance", (float)Get(follower, "TotalDistance") / (float)Get(follower, "Speed") + 1f);
            Call(mode, "Step", .01f);
            Assert.IsTrue((bool)Get(mode, "HasLost"));
            Call(view, "UpdateTowerDefenseFrame");
            Assert.IsFalse((bool)Get(view, "towerDefenseActive"));
            Assert.AreEqual("Lost", Get(Get(game, "Sim"), "Phase").ToString());
            yield return null; yield return null;
            Assert.AreEqual(0, Get(view, "hudQuotaProductCount"));
            yield return Capture("first-escape-defense-loss");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            failSaves = false;
            if (view != null) Call(view, "EndTowerDefenseRun");
            if (root != null) UnityEngine.Object.Destroy(root);
            if (cameraRoot != null) UnityEngine.Object.Destroy(cameraRoot);
            Time.timeScale = previousTime;
            yield return null;
            if (gameWindow != null && selectedSize != null) { selectedSize.SetValue(gameWindow, previousSize); gameWindow.Repaint(); }
            if (sizeGroup != null && addedSize >= 0) sizeGroup.GetType().GetMethod("RemoveCustomSize", I).Invoke(sizeGroup, new object[] {addedSize});
            if (editorGate != null) editorGate.SetValue(null, previousGate);
        }
    }
}

#endif
