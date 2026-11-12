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
    /// <summary>Real TD UI with an in-memory campaign profile, restored tutorial preference, and no cloud.</summary>
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
        bool hadCamionerosTutorialCompletion;
        int previousCamionerosTutorialCompletion;
        string camionerosTutorialCompletedKey;
        readonly ArrayList receipts = new ArrayList();
        float previousTime;
        FieldInfo editorGate;
        UnityEditor.EditorWindow gameWindow;
        PropertyInfo selectedSize;
        object sizeGroup;
        int previousSize, addedSize = -1;
        readonly ArrayList temporarySizes = new ArrayList();
        int requestedLevelId = 9;
        public static string CaptureDirectory => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Logs/Acceptance/TowerDefenseReadyStations20261106"));

        static Type T(string n) => Type.GetType("HayChoriYPaty." + n + ", Assembly-CSharp", true);
        static object Get(object o, string n)
        {
            PropertyInfo p = o.GetType().GetProperty(n, I);
            return p != null ? p.GetValue(o) : o.GetType().GetField(n, I).GetValue(o);
        }
        static object StaticValue(Type type, string n)
        {
            PropertyInfo p = type.GetProperty(n, S);
            return p != null ? p.GetValue(null) : type.GetField(n, S).GetValue(null);
        }
        static void Set(object o, string n, object v) => o.GetType().GetField(n, I).SetValue(o, v);
        static object Call(object o, string n, params object[] args)
        {
            try { return o.GetType().GetMethod(n, I).Invoke(o, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException; }
        }
        static object CallStatic(Type type, string n, params object[] args)
        {
            try { return type.GetMethod(n, S).Invoke(null, args); }
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
            requestedLevelId = TestContext.CurrentContext.Test.Name.IndexOf("Peregrinacion", StringComparison.OrdinalIgnoreCase) >= 0 ? 14
                : TestContext.CurrentContext.Test.Name.IndexOf("LaRenga", StringComparison.OrdinalIgnoreCase) >= 0 ? 13
                : TestContext.CurrentContext.Test.Name.IndexOf("LosRedondos", StringComparison.OrdinalIgnoreCase) >= 0 ? 10 : 9;
            previousTime = Time.timeScale;
            gt = T("StreetGame"); vt = T("StreetView");
            camionerosTutorialCompletedKey = (string)vt.GetField("CamionerosTutorialCompletedKey", S).GetRawConstantValue();
            hadCamionerosTutorialCompletion = PlayerPrefs.HasKey(camionerosTutorialCompletedKey);
            previousCamionerosTutorialCompletion = hadCamionerosTutorialCompletion
                ? PlayerPrefs.GetInt(camionerosTutorialCompletedKey) : 0;
            bool isTutorialCase = TestContext.CurrentContext.Test.Name.IndexOf("CamionerosTutorial", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isTutorialCase) PlayerPrefs.DeleteKey(camionerosTutorialCompletedKey);
            else PlayerPrefs.SetInt(camionerosTutorialCompletedKey, 1);
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
            Set(seed, "version", 5); Set(seed, "unlockedLevel", 12);
            Type match = T("StreetMatchRecord");
            Type callback = typeof(Func<,,>).MakeGenericType(typeof(string), match, typeof(bool));
            Delegate persist = GetType().GetMethod("Persist", I).MakeGenericMethod(match).CreateDelegate(callback, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", JsonUtility.ToJson(seed), persist));
            Assert.IsTrue((bool)Call(game, "SelectLevel", requestedLevelId));
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
            string directory = requestedLevelId >= 13 ? Path.Combine(CaptureDirectory, "Level"+requestedLevelId) : CaptureDirectory;
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }

        IEnumerator CaptureCamionerosTutorialStep(string step)
        {
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.AreEqual(540, image.width); Assert.AreEqual(960, image.height);
                File.WriteAllBytes("/private/tmp/hcyp-camioneros-tutorial-" + step + ".png", image.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(image); }
        }

        [UnityTest]
        public IEnumerator CamionerosTutorialExplainsExistingHireCardsAndKeepsDefensePausedUntilContinue()
        {
            Assert.IsTrue((bool)Get(view, "towerDefenseTutorialActive"));
            Assert.AreEqual("Paused", Get(mode, "Phase").ToString(),
                "The Defense route must not advance while the first-time tutorial is open.");
            Assert.AreEqual(0, ((ICollection)Get(mode, "Fans")).Count,
                "No fan can enter before the tutorial is continued.");

            Rect[] cards = Cards();
            string[] headlines = (string[])vt.GetField("CamionerosTutorialHeadlines", S).GetValue(null);
            string[] explanations = (string[])vt.GetField("CamionerosTutorialExplanations", S).GetValue(null);
            CollectionAssert.AreEqual(new[] {"Para el chori,\ncontratá al Parrillero.", "Para la Coca,\ncontratá al Cocacolero."}, headlines);
            CollectionAssert.AreEqual(new[]
            {
                "Tocá Parrillero y elegí\ncualquiera de las dos áreas.",
                "Tocá Cocacolero y elegí\ncualquiera de las dos áreas."
            }, explanations);
            Rect firstTarget = (Rect)Call(view, "CamionerosTutorialTargetBounds");
            Assert.AreEqual(cards[0], firstTarget, "The first callout targets the existing Parrillero card.");
            Assert.AreEqual(300, (int)Call(view, "HitTowerDefenseAction",
                ((Rect)Call(view, "CamionerosTutorialPrimaryBounds")).center));
            yield return CaptureCamionerosTutorialStep("parrillero");

            Call(view, "HandleTowerDefenseAction", 300);
            Assert.AreEqual(1, Get(view, "towerDefenseTutorialStep"));
            Assert.AreEqual("Paused", Get(mode, "Phase").ToString());
            Rect secondTarget = (Rect)Call(view, "CamionerosTutorialTargetBounds");
            Assert.AreEqual(cards[1], secondTarget, "The second callout targets the existing Cocacolero card.");
            yield return CaptureCamionerosTutorialStep("cocacolero");

            Call(view, "HandleTowerDefenseAction", 300);
            Assert.IsFalse((bool)Get(view, "towerDefenseTutorialActive"));
            Assert.AreEqual("Playing", Get(mode, "Phase").ToString());
            Assert.AreEqual(1, PlayerPrefs.GetInt(camionerosTutorialCompletedKey, 0),
                "Completion is saved only after the actual run resumes successfully.");
        }

        [UnityTest]
        public IEnumerator CamionerosTutorialCanBeSkippedIntoTheSameDefenseRun()
        {
            Assert.IsTrue((bool)Get(view, "towerDefenseTutorialActive"));
            Assert.AreEqual("Paused", Get(mode, "Phase").ToString());
            Call(view, "HandleTowerDefenseAction", 301);
            Assert.IsFalse((bool)Get(view, "towerDefenseTutorialActive"));
            Assert.AreEqual("Playing", Get(mode, "Phase").ToString());
            Assert.AreEqual(1, PlayerPrefs.GetInt(camionerosTutorialCompletedKey, 0));
            yield return null;
        }

        IEnumerator CaptureAtResolution(string name, int width, int height, bool restoreResolution = true)
        {
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            gameWindow = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            Type registryType = editor.GetType("UnityEditor.GameViewSizes");
            object registry = registryType.GetProperty("instance", S).GetValue(null);
            object groupKind = registryType.GetProperty("currentGroupType", I).GetValue(registry);
            sizeGroup = registryType.GetMethod("GetGroup", I).Invoke(registry, new[] {groupKind});
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"),
                new object[] {Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"),
                    width, height, "TD responsive capture " + requestedLevelId + " " + width + "x" + height});
            sizeGroup.GetType().GetMethod("AddCustomSize", I).Invoke(sizeGroup, new[] {size});
            int temporarySize = (int)sizeGroup.GetType().GetMethod("GetTotalCount", I).Invoke(sizeGroup, null) - 1;
            temporarySizes.Add(temporarySize);
            selectedSize.SetValue(gameWindow, temporarySize);
            gameWindow.Repaint();
            try
            {
                yield return null; yield return null; yield return new WaitForEndOfFrame();
                Call(view, "UpdateTowerDefenseFrame");
                yield return null; yield return new WaitForEndOfFrame();
                Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    Assert.AreEqual(width, image.width, "Actual Game View capture width for " + name);
                    Assert.AreEqual(height, image.height, "Actual Game View capture height for " + name);
                    string directory = Path.Combine(CaptureDirectory, "Level" + requestedLevelId.ToString("00"));
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
                }
                finally { UnityEngine.Object.Destroy(image); }
            }
            finally
            {
                if (restoreResolution)
                {
                    sizeGroup.GetType().GetMethod("RemoveCustomSize", I).Invoke(sizeGroup, new object[] {temporarySize});
                    temporarySizes.Remove(temporarySize);
                    selectedSize.SetValue(gameWindow, addedSize);
                    gameWindow.Repaint();
                }
            }
            if (restoreResolution)
            {
                yield return null;
                Call(view, "UpdateTowerDefenseFrame");
                yield return null;
            }
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
            Type simulationType = T("StreetSimulation");
            object normalLevel = Activator.CreateInstance(simulationType, new[] { Get(game, "Balance"), (object)0, 5f, 0, 1, 0 });
            int fullyLoadedLevelOneTable = (int)Call(Get(normalLevel, "Kitchen"), "TableCount", 0);
            Type layoutType = T("StreetKitchenLayout");
            object normalLayout = Activator.CreateInstance(layoutType, new object[] { new[] { 0 }, false });
            Rect normalTableBounds = (Rect)Get(normalLayout, "NormalTableBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
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
                Assert.AreEqual(table ? fullyLoadedLevelOneTable : 6, products,
                    "Prepared products/loaded barrel are visible from frame zero; the Chori display matches the complete Level 1 table.");
                if (table)
                {
                    Assert.AreEqual(48, fullyLoadedLevelOneTable, "The reference is the actual Level 1 opening table, not a guessed display count.");
                    Assert.AreEqual(fullyLoadedLevelOneTable, Call(Get(mode, "Kitchen"), "TableCapacity", 0));
                    for (int slot = 0; slot < fullyLoadedLevelOneTable; slot++)
                    {
                        Transform display = station.transform.Find("Prepared Chori display " + slot.ToString("00"));
                        Assert.IsNotNull(display, "Every Level 1 table slot is rendered exactly once.");
                        Vector2 point = (Vector2)layoutType.GetMethod("TableSlotPosition", I, null,
                            new[] { typeof(int), typeof(int), typeof(bool), typeof(int) }, null)
                            .Invoke(normalLayout, new object[] { 0, slot, false, fullyLoadedLevelOneTable });
                        Vector2 expectedOffset = new Vector2(point.x - normalTableBounds.center.x,
                            -(point.y - normalTableBounds.center.y)) * pixel;
                        Vector2 actualOffset = (Vector2)display.position - (Vector2)station.transform.position;
                        Assert.Less(Vector2.Distance(expectedOffset, actualOffset), .0001f,
                            "TD preserves the regular Level 1 table's exact slot placement.");
                        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(display.localEulerAngles.z,
                            (float)CallStatic(layoutType, "TableSlotRotation", 0, slot, false))), .001f,
                            "TD uses the same stable scatter/rotation as the regular table.");
                    }
                }
                if (!table)
                {
                    Assert.IsNotNull(station.transform.Find("Ice"));
                    SpriteRenderer barrel = station.GetComponent<SpriteRenderer>();
                    Assert.IsNotNull(barrel);
                    Assert.IsNotNull(barrel.sprite);
                    Assert.AreEqual((float)StaticValue(layoutType, "BarrelWidth"),
                        barrel.sprite.bounds.size.x * station.transform.lossyScale.x / pixel, .001f,
                        "TD Coca barrel must render at the regular level's exact logical width.");
                    Assert.AreEqual((float)StaticValue(layoutType, "BarrelHeight"),
                        barrel.sprite.bounds.size.y * station.transform.lossyScale.y / pixel, .001f,
                        "TD Coca barrel must render at the regular level's exact logical height.");
                }
            }
            Assert.AreEqual(2, tables); Assert.AreEqual(2, barrels);
        }

        GameObject Station(int zone, string prefab)
        {
            IList stations = (IList)Get(Get(view, "towerDefensePresentation"), "stationObjects");
            string expected = "TD zone " + zone + " " + prefab;
            foreach (GameObject station in stations)
                if (station != null && station.name == expected) return station;
            Assert.Fail("Missing Tower Defense station " + expected);
            return null;
        }

        int ActiveLoadedCocaDisplays(int zone)
        {
            Transform barrel = Station(zone, "Station_CocaBarrel").transform;
            int active = 0;
            for (int i = 0; i < barrel.childCount; i++)
            {
                Transform child = barrel.GetChild(i);
                if (child.name.StartsWith("Loaded Coca display") && child.gameObject.activeSelf) active++;
            }
            return active;
        }

        int ActivePreparedChoriDisplays(int zone)
        {
            Transform table = Station(zone, "Station_NormalTable").transform;
            int active = 0;
            for (int i = 0; i < table.childCount; i++)
            {
                Transform child = table.GetChild(i);
                if (child.name.StartsWith("Prepared Chori display") && child.gameObject.activeSelf) active++;
            }
            return active;
        }

        Transform FirstActiveLoadedCocaDisplay(int zone)
        {
            Transform barrel = Station(zone, "Station_CocaBarrel").transform;
            for (int i = 0; i < barrel.childCount; i++)
            {
                Transform child = barrel.GetChild(i);
                if (child.name.StartsWith("Loaded Coca display") && child.gameObject.activeSelf) return child;
            }
            Assert.Fail("No visible Coca bottle is available in zone " + zone);
            return null;
        }

        Transform FirstActivePreparedChoriDisplay(int zone)
        {
            Transform table = Station(zone, "Station_NormalTable").transform;
            for (int i = 0; i < table.childCount; i++)
            {
                Transform child = table.GetChild(i);
                if (child.name.StartsWith("Prepared Chori display") && child.gameObject.activeSelf) return child;
            }
            Assert.Fail("No visible Chori serving is available in zone " + zone);
            return null;
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
        public IEnumerator OneCocacoleroSerializesCocaHandoffsToOneFanAtATime()
        {
            AssertPreparedStations();
            object firstFan = HeldFan(new[] {4}, new[] {1}, .12f);
            object secondFan = HeldFan(new[] {4}, new[] {1}, .12f);
            Set(mode, "stopSpawning", true);
            yield return Tap(Cards()[1].center); yield return Tap(Areas()[0].center);

            object presentation = Get(view, "towerDefensePresentation");
            for (int n = 0; n < 40 && receipts.Count < 1; n++)
            {
                Call(mode, "Step", .05f);
                Call(presentation, "Sync", mode);
                yield return null;
            }
            Assert.AreEqual(1, receipts.Count);
            Assert.AreEqual(1, Get(mode, "TotalDelivered"));
            Assert.AreEqual(0, Get(firstFan, "PendingUnits"));
            Assert.AreEqual(1, Get(secondFan, "PendingUnits"));
            float firstDeliveryAt = (float)Get(receipts[0], "Elapsed");
            IList cues = (IList)Get(presentation, "deliveryCues");
            Assert.AreEqual(1, cues.Count, "One real Coca unit starts one visible handoff cue.");
            yield return Capture("single-cocacolero-serial-handoff");

            for (int n = 0; n < 10; n++)
            {
                Call(mode, "Step", .05f);
                Call(presentation, "Sync", mode);
                yield return null;
                Assert.AreEqual(1, receipts.Count,
                    "A single worker must not start another handoff during the 0.62-second service animation.");
                Assert.LessOrEqual(((IList)Get(presentation, "deliveryCues")).Count, 1,
                    "The same Cocacolero must never stack Coca flight cues.");
            }

            for (int n = 0; n < 6 && receipts.Count < 2; n++)
            {
                Call(mode, "Step", .05f);
                Call(presentation, "Sync", mode);
                yield return null;
            }
            Assert.AreEqual(2, receipts.Count, "After the cooldown, the same worker should continue serving the next waiting fan.");
            Assert.AreNotEqual(Get(receipts[0], "FanId"), Get(receipts[1], "FanId"));
            float gap = (float)Get(receipts[1], "Elapsed") - firstDeliveryAt;
            Assert.That(gap, Is.GreaterThanOrEqualTo((float)StaticValue(T("TowerDefenseGameMode"), "DeliveryCueDurationSeconds")));
            Assert.LessOrEqual(((IList)Get(presentation, "deliveryCues")).Count, 1);
        }

        [UnityTest]
        public IEnumerator MultipleParrillerosInOneAreaShowDistinctParallelHandoffs()
        {
            AssertPreparedStations();
            HeldFan(new[] {0}, new[] {1}, .12f);
            HeldFan(new[] {0}, new[] {1}, .12f);
            Set(mode, "stopSpawning", true);
            yield return Tap(Cards()[0].center); yield return Tap(Areas()[0].center);
            yield return Tap(Cards()[0].center); yield return Tap(Areas()[0].center);

            IList workers = (IList)Get(mode, "Workers");
            Assert.AreEqual(2, workers.Count);
            Assert.AreEqual("Parrillero", Get(workers[0], "Role").ToString());
            Assert.AreEqual("Parrillero", Get(workers[1], "Role").ToString());
            Assert.AreEqual(0, Get(workers[0], "ServiceZone"));
            Assert.AreEqual(0, Get(workers[1], "ServiceZone"));

            object presentation = Get(view, "towerDefensePresentation");
            for (int n = 0; n < 120 && receipts.Count < 2; n++)
            {
                Call(mode, "Step", .05f);
                Call(presentation, "Sync", mode);
                yield return null;
            }
            Assert.AreEqual(2, receipts.Count, "Both physical Chori units should be delivered by the two ready Parrilleros.");
            Assert.AreNotEqual(Get(receipts[0], "WorkerId"), Get(receipts[1], "WorkerId"),
                "The two distinct workers, not one worker twice, must perform the handoffs.");

            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Vector2 firstPosition = (Vector2)Get(workers[0], "Position"), secondPosition = (Vector2)Get(workers[1], "Position");
            Assert.That(Mathf.Abs(firstPosition.x - secondPosition.x) / pixel, Is.GreaterThanOrEqualTo(84f));
            IDictionary staff = (IDictionary)Get(presentation, "workerViews");
            SpriteRenderer[] renderers = new SpriteRenderer[2];
            for (int i = 0; i < workers.Count; i++)
            {
                object worker = workers[i];
                Assert.Greater((float)Get(worker, "ServeAnimationSeconds"), 0f,
                    "Both workers must show the handoff animation in the same frame.");
                Assert.IsFalse((bool)Get(worker, "IsMoving"), "The contract keeps each Parrillero inside the selected work area.");
                object workerView = staff[(int)Get(worker, "Id")];
                renderers[i] = (SpriteRenderer)Get(workerView, "Renderer");
                Sprite[] handoffFrames = (Sprite[])Get(workerView, "HandoffFrames");
                CollectionAssert.Contains(handoffFrames, renderers[i].sprite,
                    "Each visible worker must use its approved Parrillero handoff sprite.");
            }
            Assert.IsFalse(renderers[0].bounds.Intersects(renderers[1].bounds),
                "Same-area Parrillero handoff sprites must not cover one another.");
            yield return Capture("two-parrilleros-area-0-parallel-handoff");
        }

        [UnityTest]
        public IEnumerator HandoffCuesAnimatePauseAndRecycleWithoutCreatingSales()
        {
            AssertPreparedStations();
            HeldFan(new[] {0, 4}, new[] {1, 1}, .48f);
            Set(mode, "stopSpawning", true);
            object presentation = Get(view, "towerDefensePresentation");
            GameObject firstCue = null;
            foreach (int product in new[] {4, 0})
            {
                int zone = product == 4 ? 1 : 0;
                int roleCard = product == 4 ? 1 : 0;
                yield return Tap(Cards()[roleCard].center); yield return Tap(Areas()[zone].center);
                int expected = product == 4 ? 1 : 2;
                Transform sourceDisplay = product == 4 ? FirstActiveLoadedCocaDisplay(zone) : FirstActivePreparedChoriDisplay(zone);
                Vector3 sourcePosition = sourceDisplay.position;
                int initialSourceCount = product == 4 ? ActiveLoadedCocaDisplays(zone) : ActivePreparedChoriDisplays(zone);
                for (int n = 0; n < 160 && receipts.Count < expected; n++)
                { Call(mode, "Step", .05f); yield return null; }
                Assert.AreEqual(expected, receipts.Count);
                Call(presentation, "Sync", mode);
                IList cues = (IList)Get(presentation, "deliveryCues"); Assert.AreEqual(1, cues.Count);
                GameObject cue = (GameObject)Get(cues[0], "GameObject");
                int remainingSourceCount = product == 4 ? ActiveLoadedCocaDisplays(zone) : ActivePreparedChoriDisplays(zone);
                Assert.AreEqual(initialSourceCount - 1, remainingSourceCount,
                    "Exactly the serving being handed off disappears from its cosmetic station display.");
                Assert.IsFalse(sourceDisplay.gameObject.activeSelf);
                Assert.AreEqual(product, Get(Get(cues[0], "Receipt"), "Product"));
                Assert.Less(Vector3.Distance(sourcePosition, cue.transform.position), .0001f,
                    "The one visible delivery cue starts at the source serving, not at an invented duplicate.");
                if (product == 4)
                {
                    Assert.AreEqual(6, ActiveLoadedCocaDisplays(0), "A delivery in one area must not hide the other area's display.");
                    Assert.IsFalse(((SpriteRenderer)Get(cues[0], "Renderer")).enabled,
                        "Cocacolero's existing bottle-in-hand pose represents the pickup; the separate Coca cue appears at release.");
                }
                if (firstCue == null) firstCue = cue; else Assert.AreSame(firstCue, cue, "Reuse the expired visual, never a new inventory item.");
                Vector3 start = cue.transform.position;
                IList workers = (IList)Get(mode, "Workers");
                object deliveryWorker = workers[product == 4 ? 0 : 1];
                IDictionary staff = (IDictionary)Get(presentation, "workerViews");
                object workerView = staff[(int)Get(deliveryWorker, "Id")];
                SpriteRenderer workerRenderer = (SpriteRenderer)Get(workerView, "Renderer");
                Sprite[] baseFrames = (Sprite[])Get(workerView, "Frames");
                Sprite[] handoffFrames = (Sprite[])Get(workerView, "HandoffFrames");
                Assert.IsNotNull(handoffFrames); Assert.Greater(handoffFrames.Length, 0);
                Texture2D expectedHandoffArt = Resources.Load<Texture2D>(product == 4
                    ? "street-cocacolero-barrel-reach-v1" : "street-parrillero-cooking-arms-v1");
                Assert.IsNotNull(expectedHandoffArt);
                Assert.AreSame(expectedHandoffArt, handoffFrames[0].texture,
                    "The throw uses the already-approved role artwork, not a mismatched replacement sprite.");
                Assert.AreNotSame(baseFrames[0], workerRenderer.sprite,
                    "The serving worker switches to an existing authored reach/hand-off sprite.");
                GameObject workerObject = (GameObject)Get(workerView, "GameObject");
                float maxGestureAngle = 0f;
                for (int frame = 0; frame < 6; frame++)
                {
                    if (frame > 0) Call(mode, "Step", .05f);
                    Call(presentation, "Sync", mode);
                    maxGestureAngle = Mathf.Max(maxGestureAngle,
                        Quaternion.Angle(Quaternion.identity, workerObject.transform.localRotation));
                    if (frame == 2)
                    {
                        Assert.AreNotEqual(start, cue.transform.position, "Actual product cue must move toward its recipient.");
                        Sprite heldSprite = workerRenderer.sprite;
                        Vector3 held = cue.transform.position; float elapsed = (float)Get(mode, "Elapsed");
                        Call(mode, "Pause"); Call(mode, "Step", 1f); Call(presentation, "Sync", mode);
                        Assert.AreEqual(elapsed, Get(mode, "Elapsed")); Assert.AreEqual(held, cue.transform.position);
                        Assert.AreSame(heldSprite, workerRenderer.sprite, "Pause freezes the worker pose as well as the product.");
                        Call(mode, "Resume");
                    }
                    Assert.AreEqual(expected, receipts.Count);
                    yield return Capture("handoff-" + (product == 0 ? "chori" : "coca") + "-frame-" + frame.ToString("00"));
                }
                Assert.Greater(maxGestureAngle, 1f, "A real handoff has a service gesture during its animation.");
                Call(mode, "Step", .21f); Call(presentation, "Sync", mode);
                Assert.IsTrue(((SpriteRenderer)Get(cues[0], "Renderer")).enabled,
                    "The physical product appears at the worker's hand when the throw begins.");
                Assert.Greater(Mathf.Abs(cue.transform.localEulerAngles.z), 1f,
                    "The released product spins through a visible throwing arc.");
                Assert.AreNotEqual(start, cue.transform.position);
                Vector2 target = (Vector2)Get(Get(cues[0], "Receipt"), "Target") + Vector2.up * 28f *
                    (float)Get(mode, "WorldUnitsPerLogicalPixel");
                Assert.Less(Vector2.Distance((Vector2)cue.transform.position, target),
                    Vector2.Distance((Vector2)start, target), "The launched unit travels toward the exact receipt recipient.");
                yield return Capture("handoff-" + (product == 0 ? "chori" : "coca") + "-release-throw");
                Call(mode, "Step", .25f); Call(presentation, "Sync", mode);
                Assert.AreEqual(0, ((IList)Get(presentation, "deliveryCues")).Count);
                Assert.AreEqual(initialSourceCount, product == 4 ? ActiveLoadedCocaDisplays(zone) : ActivePreparedChoriDisplays(zone),
                    "The cosmetic product display returns after the real unit's visual handoff ends.");
                Assert.IsTrue(sourceDisplay.gameObject.activeSelf);
            }
            Call(mode, "Step", .5f); Call(presentation, "Sync", mode);
            Assert.AreEqual(1, Get(mode, "ServedFans")); Assert.AreEqual(2, Get(mode, "TotalDelivered"));
            Assert.AreEqual(10, Get(mode, "CoinsEarned")); Assert.AreEqual(2, receipts.Count);
            Assert.AreEqual(1, ((ICollection)Get(presentation, "pooledDeliveryCues")).Count);
        }

        [UnityTest]
        public IEnumerator CamionerosAdvancingFanCyclesThroughTheThreeLoadedWalkingSprites()
        {
            Assert.AreEqual(9, requestedLevelId);
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {4}, new[] {1}));
            object fan = ((IList)Get(mode, "Fans"))[0];
            object follower = Get(fan, "RouteFollower");
            object presentation = Get(view, "towerDefensePresentation");
            int fanId = (int)Get(fan, "Id");
            Call(presentation, "Sync", mode);
            IDictionary fanViews = (IDictionary)Get(presentation, "activeFans");
            SpriteRenderer renderer = (SpriteRenderer)Get(fanViews[fanId], "Renderer");
            string[] frameTextures =
            {
                "street-ferro-fans-walk-v2",
                "street-ferro-fans-walk-reverse-v1",
                "street-ferro-fans-walk-passing-v1"
            };
            int initialPose = fanId % frameTextures.Length;
            Assert.AreEqual(frameTextures[initialPose], renderer.sprite.texture.name,
                "The initial rendered atlas must honor the fan-ID phase offset.");
            yield return Capture("camioneros-walking-pose-0");

            float speed = (float)Get(follower, "Speed");
            float distance = 18f * (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Assert.Greater(speed, 0f);
            for (int stride = 1; stride <= frameTextures.Length; stride++)
            {
                Call(follower, "Advance", (distance + .0001f) / speed);
                Call(presentation, "Sync", mode);
                string expected = frameTextures[(initialPose + stride) % frameTextures.Length];
                Assert.AreEqual(expected, renderer.sprite.texture.name,
                    "The rendered supporter must cycle through all authored atlases as route progress advances.");
                yield return Capture("camioneros-walking-pose-" + stride);
            }
        }

        [UnityTest]
        public IEnumerator CamionerosFanUsesFrontFacingThreePoseGaitOnDownwardRouteSegments()
        {
            Assert.AreEqual(9, requestedLevelId);
            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {4}, new[] {1}));
            object fan = ((IList)Get(mode, "Fans"))[0];
            object follower = Get(fan, "RouteFollower");
            object presentation = Get(view, "towerDefensePresentation");
            int fanId = (int)Get(fan, "Id");
            float speed = (float)Get(follower, "Speed");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Type presentationType = T("TowerDefenseWorldPresentation");
            Assert.Greater(speed, 0f);
            Assert.Greater(pixel, 0f);

            // Advance along the configured real route until the first downward leg.
            int steps = 0;
            while (!(bool)CallStatic(presentationType, "IsWalkingDownwards", (Vector2)Get(follower, "Facing")) && steps < 10000)
            {
                Call(follower, "Advance", pixel / speed);
                steps++;
            }
            Assert.IsTrue((bool)CallStatic(presentationType, "IsWalkingDownwards", (Vector2)Get(follower, "Facing")),
                "The configured Camioneros path must contain a downward connector.");

            string[] downTextures =
            {
                "street-ferro-fans-walk-down-v1",
                "street-ferro-fans-walk-down-reverse-v1",
                "street-ferro-fans-walk-down-passing-v1"
            };
            IDictionary fanViews = (IDictionary)Get(presentation, "activeFans");
            SpriteRenderer renderer = (SpriteRenderer)Get(fanViews[fanId], "Renderer");
            Call(presentation, "Sync", mode);
            int pose = (int)CallStatic(presentationType, "FanWalkingFrameIndex", 9, fanId,
                (float)Get(follower, "Progress"), pixel);
            Assert.AreEqual(downTextures[pose], renderer.sprite.texture.name,
                "A descending route segment must render the front-facing atlas, not the side-facing one.");
            for (int phase = 0; phase < downTextures.Length; phase++)
            {
                Sprite selected = (Sprite)Call(presentation, "FanSpriteForFacing", fanId, false, phase, true);
                Assert.AreEqual(downTextures[phase], selected.texture.name,
                    "Each gait phase must resolve to its matching front-facing downward atlas.");
            }
            yield return Capture("camioneros-walking-down-pose-0");
        }

        [UnityTest]
        public IEnumerator CamionerosOrderBubbleEntersWithTheFanInsteadOfAppearingAtTheEdge()
        {
            Assert.AreEqual(9, requestedLevelId);
            Call(mode, "Step", .05f);
            IList fans = (IList)Get(mode, "Fans");
            Assert.AreEqual(1, fans.Count, "The first supporter appears gradually from the left entrance.");
            Set(mode, "stopSpawning", true);
            object fan = fans[0];
            object follower = Get(fan, "RouteFollower");
            object presentation = Get(view, "towerDefensePresentation");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            float speed = (float)Get(follower, "Speed");
            Assert.Greater(pixel, 0f);
            Assert.Greater(speed, 0f);

            // Capture two points in the 78px entrance reveal. The bubble is rendered
            // through the same viewport clip as its world-space fan, above its head.
            Call(follower, "Advance", 18f * pixel / speed);
            Call(presentation, "Sync", mode);
            yield return Capture("camioneros-entry-reveal-18px-fan-and-order-bubble");

            Call(follower, "Advance", 21f * pixel / speed);
            Call(presentation, "Sync", mode);
            yield return Capture("camioneros-entry-reveal-39px-fan-and-order-bubble");

            Call(follower, "Advance", 21f * pixel / speed);
            Call(presentation, "Sync", mode);
            yield return Capture("camioneros-entry-reveal-60px-fan-and-order-bubble");
        }

        [UnityTest]
        public IEnumerator AutomaticMixedOneHundredTwentyFanWaveCanWinWithCoinHiresAndNormalMovementForCamioneros()
        {
            yield return RunAutomaticMixedFanWave(9, 120);
        }

        [UnityTest]
        public IEnumerator AutomaticMixedSixtyFanWaveCanWinWithCoinHiresAndNormalMovementForLosRedondos()
        {
            yield return RunAutomaticMixedFanWave(10, 60);
        }

        [UnityTest]
        public IEnumerator LaRengaAutomaticSixtyFansCanWinWithNormalMovementAndCoinHires()
        {
            Assert.IsFalse((bool)Get(view, "towerDefenseTutorialActive"));
            yield return RunAutomaticMixedFanWave(13, 60);
        }

        [UnityTest]
        public IEnumerator LaRengaMixedOrderUsesPhysicalProductsAndCommonCampaignProjection()
        {
            yield return CaseCMixedOrderUsesRealProductionCorrectRolesReceiptsAndCampaignProjection();
        }

        [UnityTest]
        public IEnumerator LaRengaWithoutWorkersLosesAtTheFirstPendingEscape()
        {
            yield return AutomaticWaveWithoutWorkersLosesWithNoGhostDeliveries();
        }

        [UnityTest]
        public IEnumerator LaRengaGameViewShowsBothStaffedAreasFansAndPhysicalThrowsAtThreeResolutions()
        {
            Assert.AreEqual(13, requestedLevelId); AssertPreparedStations();
            Set(mode, "stopSpawning", true);
            for (int n = 0; n < 9; n++) HeldFan(new[] {0, 4}, new[] {4, 4}, .05f + n * .105f);
            foreach (int zone in new[] {0, 1})
                foreach (int role in new[] {0, 1})
                { yield return Tap(Cards()[role].center); yield return Tap(Areas()[zone].center); }
            Assert.AreEqual(4, ((IList)Get(mode, "Workers")).Count);
            bool chori = false, coca = false;
            for (int n = 0; n < 400 && !chori; n++)
            {
                Call(mode, "Step", .05f);
                foreach (object receipt in receipts)
                { chori |= (int)Get(receipt, "Product") == 0; coca |= (int)Get(receipt, "Product") == 4; }
                if (n % 10 == 0) yield return null;
            }
            Assert.IsTrue(chori); Assert.IsTrue(coca);
            Call(mode, "Step", .36f); // Advance a real receipt into its airborne throw phase.
            yield return null;
            // Held QA targets isolate visual production/handoffs; the separate sixty-fan
            // natural-wave test proves route movement and the finite win condition.
            yield return CaptureAtResolution("la-renga-service-540x960", 540, 960);
            yield return CaptureAtResolution("la-renga-service-1080x1920", 1080, 1920);
            yield return CaptureAtResolution("la-renga-service-720x1280", 720, 1280);
        }

        private void BuildPilgrimEquipmentAndInitialCrew()
        {
            Assert.AreEqual(14, requestedLevelId);
            Rect bounds = (Rect)Get(mode, "WorkBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Type stationKind = T("TowerDefenseStationKind"), roleKind = T("StreetWorkerRole");
            // Legal purchases only; no cash injection, follower-speed change or held QA targets.
            Vector2[] locations = {new Vector2(128,64),new Vector2(384,64),new Vector2(256,320),
                new Vector2(64,320),new Vector2(448,320),new Vector2(128,160),new Vector2(384,160)};
            // Place all seven separate fixtures. A blocked access is a real test failure, not waived.
            for (int i=0;i<7;i++)
            {
                Vector2 point=bounds.min+locations[i]*pixel;
                Assert.IsTrue((bool)Call(game,"TryPlaceTowerDefenseStation",mode,Enum.ToObject(stationKind,i),point),"Equipment "+i);
            }
            var hired=new bool[5]; int count=0;
            for(int tick=0;tick<600 && count<5;tick++)
            {
                if(tick%20==0)
                foreach(int role in new[]{0,4,1,2,3})
                {
                    if(hired[role])continue;
                    foreach(int y in new[]{192,224,160,256,128,96,64})
                    {
                        for(int x=64;x<bounds.width/pixel-40;x+=32)
                        {
                            Vector2 point=bounds.min+new Vector2(x,y)*pixel;
                            if(!(bool)Call(game,"TryHireTowerDefenseAt",mode,Enum.ToObject(roleKind,role),point))continue;
                            hired[role]=true;count++;break;
                        }
                        if(hired[role])break;
                    }
                }
                if(count<5)Call(mode,"Step",.05f);
            }
            Assert.AreEqual(5,((IList)Get(mode,"Workers")).Count,"A legal progressive strategy must afford and reach all five roles.");
            Assert.AreEqual(7,((IList)Get(mode,"PlacedStations")).Count);
        }

        [UnityTest]
        public IEnumerator PeregrinacionNaturalSixtyWithSevenProductsUsesLegalConstructionAndCanWin()
        {
            BuildPilgrimEquipmentAndInitialCrew();
            int[] delivered;
            for (int tick=0;tick<24000 && Get(mode,"Phase").ToString()=="Playing";tick++)
            {
                Call(mode,"Step",.05f);
                if (tick % 120 == 0) yield return null;
            }
            Assert.IsTrue((bool)Get(mode,"HasWon"),"Natural full wave must win without teleport, cash or speed changes.");
            Assert.AreEqual(60,Get(mode,"SpawnedFans")); Assert.AreEqual(60,Get(mode,"ServedFans"));
            delivered=(int[])Get(mode,"DeliveredByProduct");
            for (int product=0;product<7;product++) Assert.Greater(delivered[product],0,"Real deliveries product " + product);
            long spend=(long)Get(mode,"HireSpendCents")+(long)Get(mode,"EquipmentSpendCents");
            Assert.AreEqual(1200L+(int)Get(mode,"CoinsEarned")-spend/100,(long)(int)Get(mode,"Coins"));
            Call(view,"UpdateTowerDefenseFrame");
            Assert.AreEqual("Won",Get(Get(game,"Sim"),"Phase").ToString());
            Assert.IsTrue((bool)Call(game,"IsLevelUnlocked",4));
        }

        [UnityTest]
        public IEnumerator PeregrinacionMorenoBackdropLoadsAndRendersInUnityGameView()
        {
            const string expectedBackground = "street-background-peregrinacion-moreno-v4";
            Assert.AreEqual(expectedBackground, Get(Get(view, "towerDefenseSourceConfig"), "BackgroundResource"));
            Assert.AreEqual(expectedBackground, ((Texture2D)Get(view, "towerDefenseBackdrop")).name);

            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] {0}, new[] {1}));
            object fan = ((IList)Get(mode, "Fans"))[0];
            object follower = Get(fan, "RouteFollower");
            float distance = (float)Get(follower, "TotalDistance");
            float speed = (float)Get(follower, "Speed");
            Call(follower, "Advance", distance * .4f / speed);
            Call(Get(view, "towerDefensePresentation"), "Sync", mode);

            yield return CaptureAtResolution("peregrinacion-moreno-v4-live-gameview-540x960", 540, 960);
        }

        [UnityTest]
        public IEnumerator PeregrinacionGameViewSingleFenceFreeConstructionAtThreeResolutions()
        {
            BuildPilgrimEquipmentAndInitialCrew();
            for (int tick=0;tick<1600 && receipts.Count==0;tick++)
            { Call(mode,"Step",.05f); if (tick%50==0) yield return null; }
            Assert.Greater(receipts.Count,0,"Capture needs real production and delivery.");
            Call(mode,"Step",.36f);
            object presentation=Get(view,"towerDefensePresentation");Call(presentation,"Sync",mode);
            var component=(Component)presentation;
            Assert.AreEqual(1,component.GetComponentsInChildren<EdgeCollider2D>().Length,"Exactly one physical rail collider.");
            yield return CaptureAtResolution("peregrinacion-horizontal-540x960",540,960);
            yield return CaptureAtResolution("peregrinacion-horizontal-1080x1920",1080,1920);
            yield return CaptureAtResolution("peregrinacion-horizontal-720x1280",720,1280);
        }

        [UnityTest]
        public IEnumerator PeregrinacionPointerPreviewPlacesFurnitureAndRejectsOverlapWithoutCharge()
        {
            yield return Tap(((Rect)Call(view,"PilgrimTabBounds",1)).center);
            yield return Tap(((Rect)Call(view,"PilgrimCardBounds",0)).center);
            Assert.AreEqual(0,Get(view,"pilgrimSelection"));
            Rect work=(Rect)Get(mode,"WorkBounds");float pixel=(float)Get(mode,"WorldUnitsPerLogicalPixel");
            Vector2 world=work.min+new Vector2(128,64)*pixel;
            Vector2 logical=((Rect)Call(view,"PilgrimWorldToLogical",new Rect(world,Vector2.zero))).position;
            yield return Tap(logical);
            Assert.AreEqual(1,((IList)Get(mode,"PlacedStations")).Count);Assert.AreEqual(-1,Get(view,"pilgrimSelection"));
            int coins=(int)Get(mode,"Coins");
            yield return Tap(((Rect)Call(view,"PilgrimCardBounds",0)).center);yield return Tap(logical);
            Assert.AreEqual(1,((IList)Get(mode,"PlacedStations")).Count);Assert.AreEqual(coins,Get(mode,"Coins"));
            Assert.IsNotEmpty((string)Get(view,"towerDefenseError"));
        }

        [UnityTest]
        public IEnumerator PeregrinacionWithoutWorkersLosesAndPauseStopsSimulation()
        {
            Call(mode,"Pause");float elapsed=(float)Get(mode,"Elapsed");Call(mode,"Step",20f);
            Assert.AreEqual(elapsed,Get(mode,"Elapsed"));Call(mode,"Resume");
            for (int tick=0;tick<1800 && !(bool)Get(mode,"HasLost");tick++)
            { Call(mode,"Step",.05f);if(tick%100==0)yield return null; }
            Assert.IsTrue((bool)Get(mode,"HasLost"));Assert.AreEqual(0,Get(mode,"TotalDelivered"));
        }

        private IEnumerator RunAutomaticMixedFanWave(int campaignLevelId, int expectedFanCount)
        {
            Assert.AreEqual(campaignLevelId, requestedLevelId);
            object config = Get(view, "towerDefenseSourceConfig");
            Assert.AreEqual(campaignLevelId, Get(config, "SourceCampaignLevelId"));
            Assert.AreEqual(9, Get(config, "EconomicBalanceCampaignLevelId"));
            string expectedWalkArt = campaignLevelId == 13 ? "street-larenga-fans-walk-v1"
                : campaignLevelId == 10 ? "street-losredondos-fans-walk-v1" : "street-ferro-fans-walk-v2";
            Assert.AreEqual(expectedWalkArt, ((Sprite)Call(Get(view, "towerDefensePresentation"), "FanSprite", 1, false, 0)).texture.name);
            AssertPreparedStations();
            // Legal player strategy: one Coca covers the upper/shared path; prioritize
            // Chori throughput and reinvest actual receipts, never inject money/stock.
            foreach (int[] placement in new[] {new[] {0, 0}, new[] {1, 0}, new[] {0, 1}, new[] {0, 1}})
            { yield return Tap(Cards()[placement[0]].center); yield return Tap(Areas()[placement[1]].center); }
            Assert.AreEqual(4, ((IList)Get(mode, "Workers")).Count);
            int afterHires = Coins, additionalSpend = 0, requested = 0;
            var seen = new System.Collections.Generic.HashSet<int>();
            bool captured = false;
            for (int n = 0; n < 7200 && Get(mode, "Phase").ToString() == "Playing"; n++)
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
                {
                    captured = true;
                    yield return CaptureAtResolution("tower-defense-540x960", 540, 960);
                    yield return CaptureAtResolution("tower-defense-1080x1920", 1080, 1920);
                    yield return CaptureAtResolution("tower-defense-720x1280", 720, 1280);
                }
                if (n % 100 == 0) yield return null;
            }
            string outcome = "elapsed=" + Get(mode, "Elapsed") + ", requested=" + requested + ", delivered=" + Get(mode, "TotalDelivered") +
                ", served=" + Get(mode, "ServedFans") + ", escaped=" + Get(mode, "EscapedFans") + ", coins=" + Coins;
            TestContext.WriteLine(outcome);
            if (!(bool)Get(mode, "HasWon")) yield return Capture("automatic-wave-strategy-failure");
            Assert.IsTrue((bool)Get(mode, "HasWon"), "Natural finite wave with legal adaptive coin hires: " + outcome);
            Assert.IsFalse((bool)Get(mode, "HasLost")); Assert.AreEqual(expectedFanCount, seen.Count);
            Assert.AreEqual(expectedFanCount, Get(mode, "SpawnedFans")); Assert.AreEqual(expectedFanCount, Get(mode, "ServedFans"));
            Assert.AreEqual(expectedFanCount, Get(mode, "OrdersCompleted")); Assert.AreEqual(0, Get(mode, "EscapedFans"));
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
        public IEnumerator ThreeParrillerosAndThreeCocacolerosFitInsideTheFirstAreaInTallPortrait()
        {
            yield return PlaceAndCaptureThreePlusThreeWorkers(0);
        }

        [UnityTest]
        public IEnumerator ThreeParrillerosAndThreeCocacolerosFitInsideTheSecondAreaInTallPortrait()
        {
            yield return PlaceAndCaptureThreePlusThreeWorkers(1);
        }

        [UnityTest]
        public IEnumerator ThreeParrillerosAndThreeCocacolerosInBothAreasRenderOnSharedLines()
        {
            Set(mode, "coins", 10000);
            Rect[] cards = Cards(), areas = Areas();
            for (int zone = 0; zone < 2; zone++)
                for (int role = 0; role < 2; role++)
                    for (int i = 0; i < 3; i++)
                    {
                        yield return Tap(cards[role].center);
                        yield return Tap(areas[zone].center);
                    }

            for (int zone = 0; zone < 2; zone++)
            {
                Assert.AreEqual(3, Count("Parrillero", zone));
                Assert.AreEqual(3, Count("Cocacolero", zone));
            }

            yield return CaptureAtResolution("workers-3-plus-3-both-areas-576x1280", 576, 1280);
            for (int zone = 0; zone < 2; zone++)
            {
                AssertBothCamionerosCrewsShareLineInsideRails(zone);
            }

        }

        void AssertBothCamionerosCrewsShareLineInsideRails(int zone)
        {
            Vector2 center = (Vector2)Call(mode, "WorkZone", new object[] {zone});
            object route = Get(mode, "RouteConfig");
            float rowSpacing = (float)route.GetType().GetField("RowSpacing", I).GetValue(route);
            float corridor = (float)route.GetType().GetField("HorizontalCorridorWidth", I).GetValue(route);
            float halfClearHeight = (rowSpacing - corridor) * .5f;
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            IDictionary workerViews = (IDictionary)Get(Get(view, "towerDefensePresentation"), "workerViews");
            IList workers = (IList)Get(mode, "Workers");
            Bounds[][] crewBounds = { new Bounds[3], new Bounds[3] };
            int[] crewCounts = new int[2];
            float row = float.NaN;
            int count = 0;

            for (int i = 0; i < workers.Count; i++)
            {
                object worker = workers[i];
                if ((int)Get(worker, "ServiceZone") != zone) continue;
                Vector2 position = (Vector2)Get(worker, "Position");
                if (float.IsNaN(row)) row = position.y;
                Assert.AreEqual(row, position.y, .0001f,
                    "Both roles in area " + zone + " share the same horizontal line.");
                SpriteRenderer renderer = (SpriteRenderer)Get(workerViews[(int)Get(worker, "Id")], "Renderer");
                Assert.GreaterOrEqual(renderer.bounds.min.y, center.y - halfClearHeight - .0001f,
                    "Area " + zone + " workers remain above the lower fence rail.");
                Assert.LessOrEqual(renderer.bounds.max.y, center.y + halfClearHeight + .0001f,
                    "Area " + zone + " workers remain below the upper fence rail.");
                int crewIndex = Get(worker, "Role").ToString() == "Parrillero" ? 0 : 1;
                crewBounds[crewIndex][crewCounts[crewIndex]++] = renderer.bounds;
                count++;
            }

            Assert.AreEqual(6, count, "Both three-person teams are present in area " + zone + ".");
            Assert.AreEqual(Station(zone, "Station_NormalTable").transform.position.y,
                Station(zone, "Station_CocaBarrel").transform.position.y, .0001f,
                "Both stations in area " + zone + " share the worker line.");
            for (int crew = 0; crew < 2; crew++)
            {
                Assert.AreEqual(-10f * pixel, crewBounds[crew][0].min.x - crewBounds[crew][1].max.x, .00025f,
                    "The left and center rendered workers overlap by ten logical pixels.");
                Assert.AreEqual(-10f * pixel, crewBounds[crew][2].min.x - crewBounds[crew][0].max.x, .00025f,
                    "The center and right rendered workers overlap by ten logical pixels.");
            }
        }

        IEnumerator PlaceAndCaptureThreePlusThreeWorkers(int zone)
        {
            Set(mode, "coins", 10000);
            Rect[] cards = Cards(), areas = Areas();
            for (int role = 0; role < 2; role++)
                for (int i = 0; i < 3; i++)
                {
                    yield return Tap(cards[role].center);
                    yield return Tap(areas[zone].center);
                }

            Assert.AreEqual(3, Count("Parrillero", zone));
            Assert.AreEqual(3, Count("Cocacolero", zone));
            yield return CaptureAtResolution("workers-3-plus-3-area-" + zone + "-576x1280", 576, 1280, false);
            AssertTallPortraitWorkersAndStationsStayInsideArea(zone);
        }

        void AssertTallPortraitWorkersAndStationsStayInsideArea(int zone)
        {
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Vector2 horizontal = (Vector2)Call(mode, "WorkAreaHorizontalBounds", new object[] {zone});
            Vector2 center = (Vector2)Call(mode, "WorkZone", zone);
            object route = Get(mode, "RouteConfig");
            float rowSpacing = (float)route.GetType().GetField("RowSpacing", I).GetValue(route);
            float horizontalCorridor = (float)route.GetType().GetField("HorizontalCorridorWidth", I).GetValue(route);
            float halfClearHeight = (rowSpacing - horizontalCorridor) * .5f;
            float fenceClearance = (float)T("TowerDefenseGameMode").GetField("WorkerFenceClearanceLogical", S).GetRawConstantValue() * pixel;
            IDictionary workerViews = (IDictionary)Get(Get(view, "towerDefensePresentation"), "workerViews");
            IList workers = (IList)Get(mode, "Workers");
            GameObject table = Station(zone, "Station_NormalTable");
            GameObject barrel = Station(zone, "Station_CocaBarrel");
            int tableMinimumSortingOrder = MinimumSortingOrder(table);
            int barrelMinimumSortingOrder = MinimumSortingOrder(barrel);
            float parrilleroCenterSum = 0f, cocacoleroCenterSum = 0f;
            float sharedWorkerRowY = float.NaN;
            int parrilleroCount = 0, cocacoleroCount = 0;
            Bounds[] parrilleroBounds = new Bounds[3], cocacoleroBounds = new Bounds[3];

            for (int i = 0; i < workers.Count; i++)
            {
                object worker = workers[i];
                if ((int)Get(worker, "ServiceZone") != zone) continue;
                int id = (int)Get(worker, "Id");
                object workerView = workerViews[id];
                SpriteRenderer renderer = (SpriteRenderer)Get(workerView, "Renderer");
                Bounds bounds = renderer.bounds;
                Rect envelope = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
                Assert.GreaterOrEqual(envelope.xMin, horizontal.x - .0001f,
                    "The rendered sprite stays within the outer fence edge.");
                Assert.LessOrEqual(envelope.xMax, horizontal.y + .0001f,
                    "The rendered sprite stays within the inner U-turn edge.");
                Assert.GreaterOrEqual(envelope.yMin, center.y - halfClearHeight + fenceClearance - .0001f,
                    "The full rendered sprite clears the lower rail.");
                Assert.LessOrEqual(envelope.yMax, center.y + halfClearHeight - fenceClearance + .0001f,
                    "Area " + zone + ": the full rendered sprite clears the upper rail.");
                bool cocacolero = Get(worker, "Role").ToString() == "Cocacolero";
                int stationMinimumSortingOrder = cocacolero
                    ? barrelMinimumSortingOrder : tableMinimumSortingOrder;
                Assert.Less(renderer.sortingOrder, stationMinimumSortingOrder,
                    "The assigned station must draw in front of its worker; silhouette overlap with the counter is intentional.");
                Vector2 workerPosition = (Vector2)Get(worker, "Position");
                if (float.IsNaN(sharedWorkerRowY)) sharedWorkerRowY = workerPosition.y;
                Assert.AreEqual(sharedWorkerRowY, workerPosition.y, .0001f,
                    "Both teams share the same horizontal/depth line in this work area.");
                if (cocacolero)
                {
                    float spacing = bounds.size.x / pixel - 10f;
                    float side = cocacoleroCount == 0 ? 0f : cocacoleroCount == 1 ? -spacing : spacing;
                    Assert.AreEqual(barrel.transform.position.x + side * pixel, workerPosition.x, .001f,
                        "The first Cocacolero stays centered; later hires take the left then right slot.");
                    cocacoleroBounds[cocacoleroCount] = bounds;
                    cocacoleroCenterSum += workerPosition.x;
                    cocacoleroCount++;
                }
                else
                {
                    float spacing = bounds.size.x / pixel - 10f;
                    float side = parrilleroCount == 0 ? 0f : parrilleroCount == 1 ? -spacing : spacing;
                    Assert.AreEqual(table.transform.position.x + side * pixel, workerPosition.x, .001f,
                        "The first Parrillero stays centered; later hires take the left then right slot.");
                    parrilleroBounds[parrilleroCount] = bounds;
                    parrilleroCenterSum += workerPosition.x;
                    parrilleroCount++;
                }
            }
            Assert.AreEqual(3, parrilleroCount);
            Assert.AreEqual(3, cocacoleroCount);
            Assert.AreEqual(table.transform.position.y, barrel.transform.position.y, .0001f,
                "The table and barrel are centered on the same depth line.");
            Assert.AreEqual(-10f * pixel, parrilleroBounds[0].min.x - parrilleroBounds[1].max.x, .00025f,
                "The left and center rendered Parrillero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, parrilleroBounds[2].min.x - parrilleroBounds[0].max.x, .00025f,
                "The center and right rendered Parrillero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, cocacoleroBounds[0].min.x - cocacoleroBounds[1].max.x, .00025f,
                "The left and center rendered Cocacolero bounds overlap by ten logical pixels.");
            Assert.AreEqual(-10f * pixel, cocacoleroBounds[2].min.x - cocacoleroBounds[0].max.x, .00025f,
                "The center and right rendered Cocacolero bounds overlap by ten logical pixels.");
            float centeringTolerance = 20f * pixel;
            Assert.LessOrEqual(Mathf.Abs(parrilleroCenterSum / parrilleroCount - table.transform.position.x),
                centeringTolerance, "The Parrillero trio stays centered behind its table.");
            Assert.LessOrEqual(Mathf.Abs(cocacoleroCenterSum / cocacoleroCount - barrel.transform.position.x),
                centeringTolerance, "The Cocacolero trio stays centered behind the Coca barrel.");
            float tableWidth = (float)T("StreetKitchenLayout").GetField("TableWidth", S).GetRawConstantValue();
            float barrelWidth = (float)T("StreetKitchenLayout").GetField("BarrelWidth", S).GetRawConstantValue();
            Assert.GreaterOrEqual(barrel.transform.position.x - table.transform.position.x -
                (tableWidth + barrelWidth) * pixel * .5f, 23.5f * pixel - .0001f,
                "The full-size table and barrel remain separately readable despite the shared staff line.");
        }

        int MinimumSortingOrder(GameObject station)
        {
            int minimum = int.MaxValue;
            foreach (SpriteRenderer renderer in station.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer != null && renderer.enabled && renderer.sprite != null)
                    minimum = Mathf.Min(minimum, renderer.sortingOrder);
            Assert.AreNotEqual(int.MaxValue, minimum, station.name + " has no visible SpriteRenderer.");
            return minimum;
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
            if (sizeGroup != null)
            {
                for (int i = temporarySizes.Count - 1; i >= 0; i--)
                    sizeGroup.GetType().GetMethod("RemoveCustomSize", I).Invoke(sizeGroup, new[] {temporarySizes[i]});
                temporarySizes.Clear();
            }
            if (gameWindow != null && selectedSize != null) { selectedSize.SetValue(gameWindow, previousSize); gameWindow.Repaint(); }
            if (sizeGroup != null && addedSize >= 0) sizeGroup.GetType().GetMethod("RemoveCustomSize", I).Invoke(sizeGroup, new object[] {addedSize});
            if (hadCamionerosTutorialCompletion) PlayerPrefs.SetInt(camionerosTutorialCompletedKey, previousCamionerosTutorialCompletion);
            else PlayerPrefs.DeleteKey(camionerosTutorialCompletedKey);
            PlayerPrefs.Save();
            if (editorGate != null) editorGate.SetValue(null, previousGate);
        }
    }
}

#endif
