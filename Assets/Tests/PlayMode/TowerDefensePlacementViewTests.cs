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
        readonly ArrayList receipts = new ArrayList();
        float previousTime;
        FieldInfo editorGate;
        UnityEditor.EditorWindow gameWindow;
        PropertyInfo selectedSize;
        object sizeGroup;
        int previousSize, addedSize = -1;
        public static string CaptureDirectory => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Logs/Acceptance/TowerDefenseReadyStations20261106"));

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
            receipts.Clear();
            EventInfo deliveryEvent = mode.GetType().GetEvent("DeliveryCompleted");
            Type receiptType = deliveryEvent.EventHandlerType.GetGenericArguments()[0];
            Delegate handler = GetType().GetMethod("RecordReceipt", I).MakeGenericMethod(receiptType)
                .CreateDelegate(deliveryEvent.EventHandlerType, this);
            deliveryEvent.AddEventHandler(mode, handler);
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

        void RecordReceipt<TReceipt>(TReceipt receipt)
        {
            object item = receipt;
            int id = (int)Get(item, "UnitId");
            foreach (object prior in receipts) Assert.AreNotEqual(id, Get(prior, "UnitId"), "A physical unit cannot be delivered twice.");
            Assert.IsNull(Call(Get(mode, "Kitchen"), "Find", id), "Receipt is emitted only after the unit is physically consumed.");
            receipts.Add(item);
        }

        object HeldFan(int[] products, int[] quantities, float progress)
        {
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", products, quantities));
            IList fans = (IList)Get(mode, "Fans");
            object fan = fans[fans.Count - 1], follower = Get(fan, "RouteFollower");
            Call(follower, "Advance", (float)Get(follower, "TotalDistance") * progress / (float)Get(follower, "Speed"));
            // A held QA target isolates actual production/service; no product is injected.
            follower.GetType().GetProperty("Speed", I).SetValue(follower, 0f);
            return fan;
        }

        void AssertPreparedStations()
        {
            object presentation = Get(view, "towerDefensePresentation");
            IList stations = (IList)Get(presentation, "stationObjects");
            Assert.AreEqual(4, stations.Count);
            int tables = 0, barrels = 0;
            foreach (GameObject station in stations)
            {
                bool table = station.name.EndsWith("Station_NormalTable");
                if (table) tables++; else { barrels++; Assert.IsTrue(station.name.EndsWith("Station_CocaBarrel")); }
                int products = 0;
                foreach (SpriteRenderer renderer in station.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Assert.IsNotNull(renderer.sprite);
                    string sprite = renderer.sprite.name.ToLowerInvariant();
                    Assert.IsFalse(sprite.Contains("grill") || sprite.Contains("parrilla") || sprite.Contains("smoke") || sprite.Contains("heat"));
                    if (renderer.name.StartsWith(table ? "Prepared Chori display" : "Loaded Coca display"))
                    {
                        products++;
                        string prefab = table ? "Sandwich_Chori" : "Drink_Coca";
                        Assert.AreSame(Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefab).GetComponent<SpriteRenderer>().sprite, renderer.sprite);
                    }
                }
                Assert.AreEqual(table ? 12 : 6, products, "Prepared products/loaded barrel are visible from frame zero.");
                if (!table) Assert.IsNotNull(station.transform.Find("Ice"));
            }
            Assert.AreEqual(2, tables); Assert.AreEqual(2, barrels);
        }

        IEnumerator RealSingleRoleService(int card, int product, int zone, string name)
        {
            AssertPreparedStations();
            object wanted = HeldFan(new[] {product}, new[] {1}, zone == 0 ? .12f : .85f);
            object other = HeldFan(new[] {product == 0 ? 4 : 0}, new[] {1}, zone == 0 ? .23f : .92f);
            Set(mode, "stopSpawning", true);
            int initial = Coins, cost = (int)Call(mode, "HireCost", Role(card == 0 ? "Parrillero" : "Cocacolero"));
            yield return Capture(name + "-before-hire");
            for (int n = 0; n < 5; n++) Call(mode, "Step", .05f);
            Assert.AreEqual(0, Get(mode, "TotalDelivered")); Assert.AreEqual(0, receipts.Count);
            yield return Tap(Cards()[card].center); yield return Tap(Areas()[zone].center);
            Assert.AreEqual(initial - cost, Coins);
            Assert.AreEqual(1, Count(card == 0 ? "Parrillero" : "Cocacolero", zone));
            object worker = ((IList)Get(mode, "Workers"))[0];
            Vector2 start = (Vector2)Get(worker, "Position");
            for (int n = 0; n < 120 && (int)Get(mode, "TotalDelivered") == 0; n++)
            {
                Call(mode, "Step", .05f); yield return null;
            }
            Assert.AreEqual(1, Get(mode, "TotalDelivered"));
            Assert.AreEqual(1, receipts.Count); Assert.AreEqual(product, Get(receipts[0], "Product"));
            Assert.AreEqual(Get(wanted, "Id"), Get(receipts[0], "FanId"));
            Assert.AreEqual(zone, Get(receipts[0], "Zone"));
            Assert.AreEqual(0, Get(wanted, "PendingUnits")); Assert.IsTrue((bool)Get(wanted, "IsDeparting"));
            Assert.AreEqual(1, Get(other, "PendingUnits"), "A worker cannot serve the other role's product.");
            Assert.AreEqual(5, Get(mode, "CoinsEarned")); Assert.AreEqual(initial - cost + 5, Coins);
            Assert.AreEqual(1, Get(mode, "OrdersCompleted")); Assert.AreEqual(start, Get(worker, "Position"));
            Call(mode, "Step", .12f);
            object presentation = Get(view, "towerDefensePresentation"); Call(presentation, "Sync", mode);
            IList cues = (IList)Get(presentation, "deliveryCues");
            Assert.AreEqual(1, cues.Count); Assert.IsNotNull(((SpriteRenderer)Get(cues[0], "Renderer")).sprite);
            yield return Capture(name + "-physical-delivery");
            for (int n = 0; n < 20; n++) Call(mode, "Step", .05f);
            Call(presentation, "Sync", mode);
            Assert.AreEqual(0, ((IList)Get(presentation, "deliveryCues")).Count);
            Assert.AreEqual(1, Get(mode, "TotalDelivered")); Assert.AreEqual(1, receipts.Count);
            Assert.AreEqual(1, Get(mode, "ServedFans")); Assert.IsFalse((bool)Get(mode, "HasWon"));
            Assert.IsTrue((bool)Call(game, "PersistTowerDefenseProgress", mode));
            object sim = Get(game, "Sim");
            Assert.AreEqual(1, Get(sim, "Delivered")); Assert.AreEqual(5, Get(sim, "CoinsEarned"));
            yield return Capture(name + "-completed-no-phantom-sales");
        }

        [UnityTest]
        public IEnumerator CaseAPlacedParrilleroProducesAndServesChoriFromPreparedTable()
        {
            yield return RealSingleRoleService(0, 0, 0, "case-a-chori");
        }

        [UnityTest]
        public IEnumerator CaseBPlacedCocacoleroServesOnlyCocaFromLoadedBarrel()
        {
            yield return RealSingleRoleService(1, 4, 1, "case-b-coca");
        }

        [UnityTest]
        public IEnumerator CaseCMixedOrderUsesRealProductionCorrectRolesReceiptsAndCampaignProjection()
        {
            AssertPreparedStations();
            object fan = HeldFan(new[] {0, 4}, new[] {2, 2}, .48f);
            Set(mode, "stopSpawning", true);
            yield return Tap(Cards()[0].center); yield return Tap(Areas()[0].center);
            yield return Tap(Cards()[1].center); yield return Tap(Areas()[1].center);
            int hiredCoins = Coins;
            var positions = new ArrayList();
            foreach (object worker in (IList)Get(mode, "Workers")) positions.Add(Get(worker, "Position"));
            for (int n = 0; n < 20 && ((int[])Get(mode, "DeliveredByProduct"))[4] < 2; n++)
            { Call(mode, "Step", .05f); yield return null; }
            Assert.AreEqual(2, ((int[])Get(mode, "DeliveredByProduct"))[4]);
            Assert.AreEqual(0, ((int[])Get(mode, "DeliveredByProduct"))[0]);
            Assert.AreEqual(2, Get(fan, "PendingUnits")); Assert.IsFalse((bool)Get(fan, "IsDeparting"));
            yield return Capture("case-c-partial-coca-keeps-chori-order");
            for (int n = 0; n < 240 && (int)Get(mode, "TotalDelivered") < 4; n++)
            { Call(mode, "Step", .05f); yield return null; }
            Assert.AreEqual(4, receipts.Count); Assert.AreEqual(4, Get(mode, "TotalDelivered"));
            CollectionAssert.AreEqual(new[] {2, 0, 0, 0, 2, 0, 0}, (int[])Get(mode, "DeliveredByProduct"));
            Assert.AreEqual(20, Get(mode, "CoinsEarned")); Assert.AreEqual(hiredCoins + 20, Coins);
            Assert.AreEqual(1, Get(mode, "CustomersAttended")); Assert.AreEqual(1, Get(mode, "OrdersCompleted"));
            Assert.AreEqual(0, Get(fan, "PendingUnits")); Assert.IsTrue((bool)Get(fan, "IsDeparting"));
            foreach (object receipt in receipts)
            {
                int product = (int)Get(receipt, "Product"), id = (int)Get(receipt, "WorkerId");
                object worker = ((IList)Get(mode, "Workers"))[id - 1];
                Assert.AreEqual(product == 0 ? "Parrillero" : "Cocacolero", Get(worker, "Role").ToString());
                Assert.AreEqual(product == 0 ? 0 : 1, Get(receipt, "Zone"));
                Assert.AreEqual(Get(fan, "Id"), Get(receipt, "FanId"));
            }
            Call(mode, "Step", .12f);
            yield return Capture("case-c-mixed-completion-visible-chori-handoff");
            Call(mode, "Step", .5f);
            Assert.AreEqual(1, Get(mode, "ServedFans")); Assert.AreEqual(0, ((IList)Get(mode, "Fans")).Count);
            Assert.AreEqual(4, receipts.Count);
            for (int i = 0; i < positions.Count; i++) Assert.AreEqual(positions[i], Get(((IList)Get(mode, "Workers"))[i], "Position"));
            Assert.IsTrue((bool)Call(game, "PersistTowerDefenseProgress", mode));
            object sim = Get(game, "Sim");
            Assert.AreEqual(4, Get(sim, "Delivered")); Assert.AreEqual(20, Get(sim, "CoinsEarned"));
            Assert.AreEqual(1, Get(sim, "OrdersCompleted"));
            yield return Capture("case-c-completed-stations-and-staff");
        }

        [UnityTest]
        public IEnumerator HandoffCuesAnimatePauseAndRecycleWithoutCreatingSales()
        {
            AssertPreparedStations();
            HeldFan(new[] {0, 4}, new[] {1, 1}, .48f);
            Set(mode, "stopSpawning", true);
            yield return Tap(Cards()[0].center); yield return Tap(Areas()[0].center);
            yield return Tap(Cards()[1].center); yield return Tap(Areas()[1].center);
            object presentation = Get(view, "towerDefensePresentation");
            GameObject firstCue = null;
            foreach (int product in new[] {4, 0})
            {
                int expected = product == 4 ? 1 : 2;
                for (int n = 0; n < 160 && receipts.Count < expected; n++)
                { Call(mode, "Step", .05f); yield return null; }
                Assert.AreEqual(expected, receipts.Count);
                Call(presentation, "Sync", mode);
                IList cues = (IList)Get(presentation, "deliveryCues"); Assert.AreEqual(1, cues.Count);
                GameObject cue = (GameObject)Get(cues[0], "GameObject");
                if (firstCue == null) firstCue = cue; else Assert.AreSame(firstCue, cue, "Reuse the expired visual, never a new inventory item.");
                Vector3 start = cue.transform.position;
                for (int frame = 0; frame < 6; frame++)
                {
                    if (frame > 0) Call(mode, "Step", .05f);
                    Call(presentation, "Sync", mode);
                    if (frame == 2)
                    {
                        Assert.AreNotEqual(start, cue.transform.position, "Actual product cue must move toward its recipient.");
                        object worker = ((IList)Get(mode, "Workers"))[product == 0 ? 0 : 1];
                        IDictionary staff = (IDictionary)Get(presentation, "workerViews");
                        GameObject workerObject = (GameObject)Get(staff[(int)Get(worker, "Id")], "GameObject");
                        Assert.Greater(Quaternion.Angle(Quaternion.identity, workerObject.transform.localRotation), 1f, "A real handoff has a service gesture.");
                        Vector3 held = cue.transform.position; float elapsed = (float)Get(mode, "Elapsed");
                        Call(mode, "Pause"); Call(mode, "Step", 1f); Call(presentation, "Sync", mode);
                        Assert.AreEqual(elapsed, Get(mode, "Elapsed")); Assert.AreEqual(held, cue.transform.position);
                        Call(mode, "Resume");
                    }
                    Assert.AreEqual(expected, receipts.Count);
                    yield return Capture("handoff-" + (product == 0 ? "chori" : "coca") + "-frame-" + frame.ToString("00"));
                }
                Call(mode, "Step", .1f); Call(presentation, "Sync", mode);
                Assert.AreEqual(0, ((IList)Get(presentation, "deliveryCues")).Count);
            }
            Call(mode, "Step", .5f); Call(presentation, "Sync", mode);
            Assert.AreEqual(1, Get(mode, "ServedFans")); Assert.AreEqual(2, Get(mode, "TotalDelivered"));
            Assert.AreEqual(10, Get(mode, "CoinsEarned")); Assert.AreEqual(2, receipts.Count);
            Assert.AreEqual(1, ((ICollection)Get(presentation, "pooledDeliveryCues")).Count);
        }

        [UnityTest]
        public IEnumerator AutomaticMixedTenFanWaveCanWinWithCoinHiresAndNormalMovement()
        {
            AssertPreparedStations();
            // Legal player strategy: one Coca covers the upper/shared path; prioritize
            // Chori throughput and reinvest actual receipts, never inject money/stock.
            foreach (int[] placement in new[] {new[] {0, 0}, new[] {1, 0}, new[] {0, 1}, new[] {0, 1}})
            { yield return Tap(Cards()[placement[0]].center); yield return Tap(Areas()[placement[1]].center); }
            Assert.AreEqual(4, ((IList)Get(mode, "Workers")).Count);
            int afterHires = Coins, additionalSpend = 0, requested = 0;
            var seen = new System.Collections.Generic.HashSet<int>();
            bool captured = false;
            for (int n = 0; n < 3600 && Get(mode, "Phase").ToString() == "Playing"; n++)
            {
                int cooks = Count("Parrillero", 0) + Count("Parrillero", 1);
                int cost = (int)Call(mode, "HireCost", Role("Parrillero"));
                if (cooks < 6 && Coins >= cost)
                {
                    int before = Coins, zone = Count("Parrillero", 1) < 3 ? 1 : 0;
                    yield return Tap(Cards()[0].center); yield return Tap(Areas()[zone].center);
                    Assert.AreEqual(cooks + 1, Count("Parrillero", 0) + Count("Parrillero", 1));
                    additionalSpend += before - Coins;
                }
                Call(mode, "Step", .05f);
                foreach (object fan in (IList)Get(mode, "Fans"))
                    if (seen.Add((int)Get(fan, "Id"))) requested += (int)Get(fan, "PendingUnits");
                if (!captured && (float)Get(mode, "Elapsed") >= 10f)
                { captured = true; yield return Capture("automatic-wave-ten-seconds-natural-route"); }
                if (n % 100 == 0) yield return null;
            }
            string outcome = "elapsed=" + Get(mode, "Elapsed") + ", requested=" + requested + ", delivered=" + Get(mode, "TotalDelivered") +
                ", served=" + Get(mode, "ServedFans") + ", escaped=" + Get(mode, "EscapedFans") + ", coins=" + Coins;
            TestContext.WriteLine(outcome);
            if (!(bool)Get(mode, "HasWon")) yield return Capture("automatic-wave-strategy-failure");
            Assert.IsTrue((bool)Get(mode, "HasWon"), "Natural finite wave with legal adaptive coin hires: " + outcome);
            Assert.IsFalse((bool)Get(mode, "HasLost")); Assert.AreEqual(10, seen.Count);
            Assert.AreEqual(10, Get(mode, "SpawnedFans")); Assert.AreEqual(10, Get(mode, "ServedFans"));
            Assert.AreEqual(10, Get(mode, "OrdersCompleted")); Assert.AreEqual(0, Get(mode, "EscapedFans"));
            Assert.AreEqual(requested, Get(mode, "TotalDelivered")); Assert.AreEqual(requested, receipts.Count);
            Assert.AreEqual(requested * 5, Get(mode, "CoinsEarned")); Assert.AreEqual(afterHires - additionalSpend + requested * 5, Coins);
            Call(view, "UpdateTowerDefenseFrame");
            Assert.AreEqual("Won", Get(Get(game, "Sim"), "Phase").ToString());
            TestContext.WriteLine("Natural wave: requested/delivered=" + requested + ", earned=" + requested * 5 + ", final coins=" + Coins);
            yield return Capture("automatic-wave-won-common-campaign-result");
        }

        [UnityTest]
        public IEnumerator AutomaticWaveWithoutWorkersLosesWithNoGhostDeliveries()
        {
            AssertPreparedStations();
            for (int n = 0; n < 3600 && Get(mode, "Phase").ToString() == "Playing"; n++)
            { Call(mode, "Step", .05f); if (n % 100 == 0) yield return null; }
            Assert.IsTrue((bool)Get(mode, "HasLost")); Assert.IsFalse((bool)Get(mode, "HasWon"));
            Assert.AreEqual(1, Get(mode, "EscapedFans")); Assert.AreEqual(0, Get(mode, "TotalDelivered"));
            Assert.AreEqual(0, receipts.Count); Assert.AreEqual(500, Coins); Assert.AreEqual(0, Get(mode, "CoinsEarned"));
            Call(view, "UpdateTowerDefenseFrame");
            Assert.AreEqual("Lost", Get(Get(game, "Sim"), "Phase").ToString());
            yield return Capture("automatic-wave-first-escape-no-workers-no-ghost-sales");
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
