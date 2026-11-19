#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Assert.AreEqual(requestedLevelId == 14 ? 2 : 0, ((ICollection)Get(mode, "Workers")).Count,
                "Only Peregrinación has an explicitly configured Boca-like crew (Parrillero + Mozo) at start.");
            if (requestedLevelId == 14)
            {
                CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 1 }, (int[])Get(mode, "WorkerCountsByRole"));
                IList initialWorkers = (IList)Get(mode, "Workers");
                Assert.AreEqual("Parrillero", Get(initialWorkers[0], "Role").ToString());
                Assert.AreEqual("Mozo", Get(initialWorkers[1], "Role").ToString());
                IList openingStations = (IList)Get(mode, "PlacedStations");
                Assert.AreEqual(7, openingStations.Count, "Luján opens with one physical copy of every station.");
                CollectionAssert.AreEqual(new[] { "NormalGrill", "PremiumGrill", "CocaBarrel", "FernetTable", "BeerBarrel", "NormalTable", "PremiumTable" },
                    new[] { Get(openingStations[0], "Kind").ToString(), Get(openingStations[1], "Kind").ToString(),
                        Get(openingStations[2], "Kind").ToString(), Get(openingStations[3], "Kind").ToString(),
                        Get(openingStations[4], "Kind").ToString(), Get(openingStations[5], "Kind").ToString(),
                        Get(openingStations[6], "Kind").ToString() });
            }
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
            string directory = requestedLevelId == 10 && name.StartsWith("los-redondos-", StringComparison.Ordinal)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/TowerDefenseLosRedondosFiveRuns20261116"))
                : requestedLevelId >= 13 ? Path.Combine(CaptureDirectory, "Level"+requestedLevelId) : CaptureDirectory;
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator CamionerosOutfitProfilesRenderTheSameLookWhileWalkingAndReceiving()
        {
            // Projection-only fixture: retain one real instance of each profile without advancing an entire wave.
            for (int i = 0; i < 17; i++) Call(mode, "SpawnNextFan");
            IList live = (IList)Get(mode, "Fans");
            var retained = new List<object>(); var kinds = new HashSet<string>();
            foreach (object fan in live)
                if (kinds.Add(Get(Get(fan, "Profile"), "Kind").ToString())) retained.Add(fan);
            Assert.AreEqual(3, retained.Count);
            for (int i = live.Count - 1; i >= 0; i--) if (!retained.Contains(live[i])) live.RemoveAt(i);
            for (int i = 0; i < live.Count; i++)
            {
                object follower = Get(live[i], "RouteFollower");
                float scale = (float)Get(mode, "WorldUnitsPerLogicalPixel");
                Call(follower, "Advance", (360f - i * 125f) * scale / (float)Get(follower, "Speed"));
            }
            Call(mode, "Pause");
            object presentation = Get(view, "towerDefensePresentation");
            Call(presentation, "SyncFans", mode);
            IDictionary visuals = (IDictionary)Get(presentation, "activeFans");
            foreach (object fan in live)
            {
                object visual = visuals[Get(fan, "Id")];
                SpriteRenderer renderer = (SpriteRenderer)Get(visual, "Renderer");
                StringAssert.Contains(":" + Get(fan, "AppearanceId") + " walk", renderer.sprite.name);
                int appearance = (int)Get(fan, "AppearanceId");
                Assert.AreEqual((3 - appearance / 3) * renderer.sprite.texture.height / 4f, renderer.sprite.rect.y,
                    "Appearance must select the actual numbered top-down sheet row, not merely name the sprite correctly.");
                Set(fan, "<IsReceiving>k__BackingField", true);
            }
            Call(presentation, "SyncFans", mode);
            foreach (object fan in live)
            {
                SpriteRenderer renderer = (SpriteRenderer)Get(visuals[Get(fan, "Id")], "Renderer");
                StringAssert.Contains(":" + Get(fan, "AppearanceId") + " front", renderer.sprite.name);
                Set(fan, "<IsReceiving>k__BackingField", false);
            }
            Call(presentation, "SyncFans", mode);
            yield return CaptureCamionerosTutorialStep("outfit-profiles-live");
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
            CollectionAssert.AreEqual(new[] {"Para el chori,\ncontratá al Parrillero.", "Para la Coca,\ncontratá al Cocacolero."}, new[] { headlines[0], headlines[1] });
            CollectionAssert.AreEqual(new[]
            {
                "Tocá Parrillero y elegí\ncualquiera de las dos áreas.",
                "Tocá Cocacolero y elegí\ncualquiera de las dos áreas."
            }, new[] { explanations[0], explanations[1] });
            Assert.AreEqual(5, headlines.Length);
            Assert.AreEqual(5, explanations.Length);
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
            for (int step = 2; step < 5; step++)
            {
                Assert.AreEqual(step, Get(view, "towerDefenseTutorialStep"));
                Assert.AreEqual("Paused", Get(mode, "Phase").ToString());
                Assert.AreEqual(0, ((ICollection)Get(mode, "Fans")).Count);
                StringAssert.Contains(step == 2 ? "10 px" : step == 3 ? "20 px" : "30 px", explanations[step]);
                yield return CaptureCamionerosTutorialStep("profile-" + step);
                Call(view, "HandleTowerDefenseAction", 300);
            }
            Assert.IsFalse((bool)Get(view, "towerDefenseTutorialActive"));
            Assert.AreEqual("Playing", Get(mode, "Phase").ToString());
            Assert.AreEqual(1, PlayerPrefs.GetInt(camionerosTutorialCompletedKey, 0),
                "Completion is saved only after the actual run resumes successfully.");
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; it no longer shows the two-area tutorial.")]
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
            const float stationScale = 1f; // every Defense level, ID10 included, uses full-size stations
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
                            -(point.y - normalTableBounds.center.y)) * pixel * stationScale;
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
                        barrel.sprite.bounds.size.x * station.transform.lossyScale.x / pixel / stationScale, .001f,
                        "TD Coca barrel must render at the regular level's exact logical width.");
                    Assert.AreEqual((float)StaticValue(layoutType, "BarrelHeight"),
                        barrel.sprite.bounds.size.y * station.transform.lossyScale.y / pixel / stationScale, .001f,
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator CaseAPlacedParrilleroProducesAndServesChoriFromPreparedTable()
        {
            yield return RealSingleRoleService(0, 0, 0, "case-a-chori");
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator CaseBPlacedCocacoleroServesOnlyCocaFromLoadedBarrel()
        {
            yield return RealSingleRoleService(1, 4, 1, "case-b-coca");
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Pendiente de rediseno: test PlayMode desfasado respecto del flujo/layout actual de StreetView (hub, selector por posicion de campana, tutoriales, mercado). Fallo previo a la limpieza; StreetView.cs no fue modificado por las fases 1-2b. Ver Docs/tests-playmode-pendientes.md.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Pendiente de rediseno: test PlayMode desfasado respecto del flujo/layout actual de StreetView (hub, selector por posicion de campana, tutoriales, mercado). Fallo previo a la limpieza; StreetView.cs no fue modificado por las fases 1-2b. Ver Docs/tests-playmode-pendientes.md.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator AutomaticMixedOneHundredTwentyFanWaveCanWinWithCoinHiresAndNormalMovementForCamioneros()
        {
            yield return RunAutomaticMixedFanWave(9, 120);
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator CamionerosPreviousAreaWidthProvidesRedondosBaseline()
        {
            Assert.AreEqual(9, requestedLevelId);
            Rect[] areas = Areas();
            Assert.AreEqual(2, areas.Length);
            TestContext.WriteLine("Prior Camioneros area widths at 540x960: " + areas[0].width + ", " + areas[1].width);
            Assert.That(areas[0].width, Is.EqualTo(404f).Within(.5f));
            Assert.That(areas[1].width, Is.EqualTo(404f).Within(.5f));
            yield return null;
        }

        [UnityTest]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public IEnumerator LosRedondosEntryTopLeftOneFiveRunRouteAndNoDuplicatedLayout()
        {
            Assert.AreEqual(10, requestedLevelId);
            object route = Get(mode, "RouteConfig");
            object geometry = Get(mode, "RouteGeometry");
            Assert.AreEqual(5, Get(route, "RowCount"));
            Vector2[] centerline = (Vector2[])Get(geometry, "Centerline");
            Assert.Greater(centerline[1].x, centerline[0].x, "The first run goes right from the upper-left entry.");
            Assert.That(centerline[0].y, Is.EqualTo(centerline.Max(point => point.y)).Within(.001f));
            Assert.That(centerline[centerline.Length - 1].y, Is.EqualTo(centerline.Min(point => point.y)).Within(.001f),
                "The exit is the end of the bottom run.");
            Assert.Greater(centerline[centerline.Length - 1].x, centerline[centerline.Length - 2].x, "The last run goes right.");
            string[] audit = (string[])Call(mode, "AuditLosRedondosLayout");
            Assert.IsEmpty(audit, string.Join("\n", audit));

            // Exactly one presentation, one fence generator and one route: no earlier layout survives.
            Type generatorType = T("TowerDefenseModularFenceGenerator");
            UnityEngine.Object[] generators = UnityEngine.Object.FindObjectsByType(generatorType, FindObjectsSortMode.None);
            Assert.AreEqual(1, generators.Length, "Only one fence generator may exist in the scene.");
            object fenceGeometry = Get(generators[0], "LatestGeometry");
            Assert.AreSame(Get(Get(generators[0], "LevelConfig"), "Route").GetType(), route.GetType());
            CollectionAssert.AreEqual(centerline, (Vector2[])Get(fenceGeometry, "Centerline"),
                "Fences and navigation are generated from the same centerline.");
            CollectionAssert.AreEqual((Vector2[])Get(geometry, "LeftBoundary"), (Vector2[])Get(fenceGeometry, "LeftBoundary"));
            CollectionAssert.AreEqual((Vector2[])Get(geometry, "RightBoundary"), (Vector2[])Get(fenceGeometry, "RightBoundary"));
            Assert.AreEqual(2, UnityEngine.Object.FindObjectsByType<EdgeCollider2D>(FindObjectsSortMode.None).Length);
            object presentation = Get(view, "towerDefensePresentation");
            Assert.AreEqual(4, ((IList)Get(presentation, "stationObjects")).Count, "One table and one barrel per area, once.");
            int roots = 0;
            foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (candidate.transform.parent == null && candidate.name.StartsWith("Tower Defense —", StringComparison.Ordinal)) roots++;
            Assert.AreEqual(1, roots, "A single Tower Defense scene-object root.");

            // No fence art reaches the worker column; the visual entry extends to the left screen edge.
            Rect work = (Rect)Get(route, "WorkBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Type pieceType = T("TowerDefenseFencePiece");
            int pieces = 0;
            foreach (Component piece in UnityEngine.Object.FindObjectsByType(pieceType, FindObjectsSortMode.None))
            {
                SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null) continue;
                pieces++;
                Assert.Less(renderer.bounds.max.x, work.xMin + .5f * pixel,
                    piece.name + " fence art must stay left of the worker column.");
            }
            Assert.Greater(pieces, 40);

            Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 0 }, new[] { 1 }));
            Set(mode, "stopSpawning", true);
            object follower = Get(((IList)Get(mode, "Fans"))[0], "RouteFollower");
            Vector2 entry = (Vector2)Get(follower, "Position");
            Assert.That(camera.WorldToScreenPoint(entry).x, Is.EqualTo(39f).Within(.5f), "Fans enter at the left edge.");
            Assert.That(entry.y, Is.EqualTo(centerline[0].y).Within(.001f), "Fans enter on the top run.");
            Call(presentation, "SyncFans", mode);
            yield return Capture("los-redondos-five-runs-entry");
        }

        [UnityTest]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public IEnumerator LosRedondosFiveRunsRightSectorsFullCrewsAndActualDeliveries()
        {
            Assert.AreEqual(10, requestedLevelId);
            object route = Get(mode, "RouteConfig");
            Rect envelope = (Rect)Get(route, "PlayableBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            foreach (int role in new[] { 0, 1 })
            foreach (int zone in new[] { 0, 1 })
            {
                yield return Tap(Cards()[role].center);
                Rect[] areas = Areas();
                Assert.AreEqual(2, areas.Length);
                Assert.GreaterOrEqual(areas[zone].xMin, 540f * .655f - .5f, "Hiring areas are only on the right strip.");
                yield return Tap(areas[zone].center);
                Assert.AreEqual(1, Count(role == 0 ? "Parrillero" : "Cocacolero", zone));
            }
            Set(mode, "coins", 100000);
            foreach (int role in new[] { 0, 1 })
            foreach (int zone in new[] { 0, 1 })
                for (int extra = 0; extra < 2; extra++)
                    Assert.IsTrue((bool)Call(mode, "TryHire", Enum.ToObject(T("StreetWorkerRole"), role), zone));
            Assert.AreEqual(12, ((IList)Get(mode, "Workers")).Count);
            yield return null;
            string[] audit = (string[])Call(mode, "AuditLosRedondosLayout");
            Assert.IsEmpty(audit, string.Join("\n", audit));

            // Real rendered bounds: workers and stations inside their sector, right of the route, stations apart.
            object presentation = Get(view, "towerDefensePresentation");
            IDictionary renderedWorkers = (IDictionary)Get(presentation, "workerViews");
            float tolerance = 1.5f * pixel;
            foreach (object worker in (IList)Get(mode, "Workers"))
            {
                SpriteRenderer renderer = (SpriteRenderer)Get(renderedWorkers[(int)Get(worker, "Id")], "Renderer");
                Rect sector = (Rect)Call(mode, "RightBayBounds", Get(worker, "Role"), Get(worker, "ServiceZone"));
                Bounds b = renderer.bounds;
                Assert.That(b.size.y / pixel, Is.EqualTo(94f).Within(.2f), "Workers keep the Camioneros size.");
                Assert.IsTrue(b.min.x >= sector.xMin - tolerance && b.max.x <= sector.xMax + tolerance &&
                    b.min.y >= sector.yMin - tolerance && b.max.y <= sector.yMax + tolerance,
                    Get(worker, "Role") + " " + b + " outside sector " + sector);
                Assert.Greater(b.min.x, envelope.xMax, "No worker on the route side.");
            }
            IList stations = (IList)Get(presentation, "stationObjects");
            var stationBounds = new List<Bounds>();
            foreach (GameObject station in stations)
            {
                SpriteRenderer[] parts = station.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(parts);
                Bounds b = parts[0].bounds;
                foreach (SpriteRenderer part in parts) b.Encapsulate(part.bounds);
                bool table = station.name.EndsWith("Station_NormalTable");
                int zone = station.name.Contains("zone 1") ? 1 : 0;
                Rect sector = (Rect)Call(mode, "RightBayBounds", Role(table ? "Parrillero" : "Cocacolero"), zone);
                TestContext.WriteLine(station.name + " bounds " + b + " sector " + sector);
                Assert.IsTrue(b.min.x >= sector.xMin - tolerance && b.max.x <= sector.xMax + tolerance &&
                    b.min.y >= sector.yMin - tolerance && b.max.y <= sector.yMax + tolerance,
                    station.name + " " + b + " outside sector " + sector);
                foreach (Bounds other in stationBounds)
                    Assert.IsFalse(b.Intersects(other) && b.min.y < other.max.y - tolerance && other.min.y < b.max.y - tolerance,
                        station.name + " overlaps another station.");
                stationBounds.Add(b);
            }

            // Stage one fan on every run and capture the full layout.
            int stride = (int)Get(route, "TurnSegments") + 1;
            Vector2[] centerline = (Vector2[])Get(Get(mode, "RouteGeometry"), "Centerline");
            float prefix = (float)Get(route, "FanEntryExtensionWorldUnits");
            float[] speeds = new float[5];
            for (int row = 0; row < 5; row++)
            {
                int first = row * stride;
                for (int j = row == 0 ? 1 : (row - 1) * stride + 1; j <= first; j++)
                    prefix += Vector2.Distance(centerline[j - 1], centerline[j]);
                float rowLength = Vector2.Distance(centerline[first], centerline[first + 1]);
                Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 0, 4 }, new[] { 4, 4 }));
                IList fans = (IList)Get(mode, "Fans");
                object follower = Get(fans[fans.Count - 1], "RouteFollower");
                speeds[row] = (float)Get(follower, "Speed");
                Call(follower, "Advance", (prefix + rowLength * .5f) / speeds[row]);
                follower.GetType().GetProperty("Speed", I).SetValue(follower, 0f);
            }
            Set(mode, "stopSpawning", true);
            yield return Capture("los-redondos-five-runs-full-crew");
            IDictionary renderedFans = (IDictionary)Get(presentation, "activeFans");
            int visibleFans = 0;
            foreach (DictionaryEntry pair in renderedFans)
            {
                SpriteRenderer fanRenderer = (SpriteRenderer)Get(pair.Value, "Renderer");
                if (fanRenderer == null || fanRenderer.sprite == null) continue;
                visibleFans++;
                Assert.Less(fanRenderer.bounds.max.x, ((Rect)Get(route, "WorkBounds")).xMin, "Fans never enter the worker strip.");
            }
            Assert.AreEqual(5, visibleFans);

            // Release the staged fans and wait for real deliveries from all four sectors.
            IList staged = (IList)Get(mode, "Fans");
            for (int i = 0; i < staged.Count; i++)
            {
                object follower = Get(staged[i], "RouteFollower");
                follower.GetType().GetProperty("Speed", I).SetValue(follower, speeds[i]);
            }
            bool[,] served = new bool[2, 2];
            for (int tick = 0; tick < 1600; tick++)
            {
                Call(mode, "Step", .05f);
                foreach (object receipt in receipts)
                {
                    int product = (int)Get(receipt, "Product"), zone = (int)Get(receipt, "Zone");
                    if (zone >= 0 && zone < 2) served[product == 0 ? 0 : 1, zone] = true;
                }
                if (served[0, 0] && served[0, 1] && served[1, 0] && served[1, 1]) break;
                if (tick % 20 == 0) yield return null;
            }
            yield return Capture("los-redondos-five-runs-deliveries");
            for (int role = 0; role < 2; role++)
                for (int zone = 0; zone < 2; zone++)
                    Assert.IsTrue(served[role, zone], "Expected actual delivery from role=" + role + ", zone=" + zone);
        }

        [UnityTest]
        [Ignore("Los Redondos (ID10) is now a horizontal Ferro kitchen level; this test covered its former five-run serpentine route and right-hand bays.")]
        public IEnumerator AutomaticMixedSixtyFanWaveCanWinWithCoinHiresAndNormalMovementForLosRedondos()
        {
            yield return RunAutomaticMixedFanWave(10, 60);
        }

        [UnityTest]
        [Ignore("La Renga (ID13) is now a C-shaped single-fence level with Huracán's kitchen and crew; this test covered its former two-area serpentine design.")]
        public IEnumerator LaRengaAutomaticSixtyFansCanWinWithNormalMovementAndCoinHires()
        {
            Assert.IsFalse((bool)Get(view, "towerDefenseTutorialActive"));
            yield return RunAutomaticMixedFanWave(13, 60);
        }

        [UnityTest]
        [Ignore("La Renga (ID13) is now a C-shaped single-fence level with Huracán's kitchen and crew; this test covered its former two-area serpentine design.")]
        public IEnumerator LaRengaMixedOrderUsesPhysicalProductsAndCommonCampaignProjection()
        {
            yield return CaseCMixedOrderUsesRealProductionCorrectRolesReceiptsAndCampaignProjection();
        }

        [UnityTest]
        [Ignore("La Renga (ID13) is now a C-shaped single-fence level with Huracán's kitchen and crew; this test covered its former two-area serpentine design.")]
        public IEnumerator LaRengaWithoutWorkersLosesAtTheFirstPendingEscape()
        {
            yield return AutomaticWaveWithoutWorkersLosesWithNoGhostDeliveries();
        }

        [UnityTest]
        [Ignore("Pendiente de rediseno: test PlayMode desfasado respecto del flujo/layout actual de StreetView (hub, selector por posicion de campana, tutoriales, mercado). Fallo previo a la limpieza; StreetView.cs no fue modificado por las fases 1-2b. Ver Docs/tests-playmode-pendientes.md.")]
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

        private void BuildPilgrimStationsAndInitialCrew()
        {
            Assert.AreEqual(14, requestedLevelId);
            Rect bounds = (Rect)Get(mode, "WorkBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Type roleKind = T("StreetWorkerRole");
            Assert.AreEqual(7, ((IList)Get(mode, "PlacedStations")).Count,
                "The game starts with every station installed; the test must not buy or place equipment.");
            Assert.IsFalse((bool)Get(mode, "EquipmentPlacementAllowed"));
            var hired=requestedLevelId==14 ? new[]{true,false,false,false,false} : new bool[5];
            int count=requestedLevelId==14 ? 1 : 0;
            for(int tick=0;tick<600 && count<5;tick++)
            {
                if(tick%20==0)
                // The initial Parrillero is already hired. Add the Fernetero before
                // filling the shared lower aisle with more crews.
                foreach(int role in new[]{0,4,3,1,2})
                {
                    if(hired[role])continue;
                    if(!(bool)Call(game,"TryHireTowerDefenseAtPost",mode,Enum.ToObject(roleKind,role)))continue;
                    hired[role]=true;count++;
                }
                if(count<5)Call(mode,"Step",.05f);
            }
            string missingRoles = string.Join(",", Enumerable.Range(0, 5).Where(role => !hired[role])
                .Select(role => Enum.GetName(roleKind, role)));
            string missingDiagnostics = string.Empty;
            if (!string.IsNullOrEmpty(missingRoles))
            {
                int missingRoleIndex = Enumerable.Range(0, 5).First(role => !hired[role]);
                object missingRole = Enum.ToObject(roleKind, missingRoleIndex);
                string lastPlacementReason = string.Empty;
                bool anyLegalPlacement = false;
                var placementReasons = new System.Collections.Generic.HashSet<string>();
                for (int y = 64; y <= bounds.height / pixel - 40 && !anyLegalPlacement; y += 32)
                for (int x = 64; x < bounds.width / pixel - 40 && !anyLegalPlacement; x += 32)
                {
                    Vector2 point = bounds.min + new Vector2(x, y) * pixel;
                    object preview = Call(mode, "PreviewWorkerPlacement", missingRole, point);
                    anyLegalPlacement = (bool)Get(preview, "IsValid");
                    lastPlacementReason = (string)Get(preview, "Reason");
                    if (!anyLegalPlacement) placementReasons.Add(lastPlacementReason);
                }
                missingDiagnostics = "; role=" + missingRole + ", coins=" + Get(mode, "Coins") +
                    ", cost=" + Call(mode, "HireCost", missingRole) + ", available=" + Call(mode, "IsRoleAvailable", missingRole) +
                    ", anyLegalPlacement=" + anyLegalPlacement + ", reasons=" + string.Join(" | ", placementReasons);
            }
            Assert.AreEqual(5,((IList)Get(mode,"Workers")).Count,
                "A legal progressive strategy must afford and reach all five roles. Missing: " + missingRoles + missingDiagnostics);
            Assert.AreEqual(7,((IList)Get(mode,"PlacedStations")).Count);
        }

        [UnityTest]
        [Ignore("Pendiente de rediseno: test PlayMode desfasado respecto del flujo/layout actual de StreetView (hub, selector por posicion de campana, tutoriales, mercado). Fallo previo a la limpieza; StreetView.cs no fue modificado por las fases 1-2b. Ver Docs/tests-playmode-pendientes.md.")]
        public IEnumerator PeregrinacionNaturalSixtyWithSevenProductsUsesLegalConstructionAndCanWin()
        {
            BuildPilgrimStationsAndInitialCrew();
            int[] delivered;
            string escapedOrder = "none";
            for (int tick=0;tick<24000 && Get(mode,"Phase").ToString()=="Playing";tick++)
            {
                object[] before = ((IEnumerable)Get(mode, "Fans")).Cast<object>().ToArray();
                Call(mode,"Step",.05f);
                if ((bool)Get(mode, "HasLost"))
                {
                    int[] remainingIds = ((IEnumerable)Get(mode, "Fans")).Cast<object>().Select(f => (int)Get(f, "Id")).ToArray();
                    object escaped = before.FirstOrDefault(f => !remainingIds.Contains((int)Get(f, "Id")) && !(bool)Get(f, "OrderComplete"));
                    if (escaped != null)
                    {
                        string lines = string.Join(",", ((IEnumerable)Get(escaped, "Order")).Cast<object>()
                            .Select(line => Get(line, "Product") + ":" + Get(line, "Remaining")).ToArray());
                        escapedOrder = "id=" + Get(escaped, "Id") + " order=" + lines;
                    }
                }
                if (tick % 120 == 0) yield return null;
            }
            string deliveredSummary = string.Join(",", (int[])Get(mode, "DeliveredByProduct"));
            string activeFanSummary = string.Join(" | ", ((IEnumerable)Get(mode, "Fans")).Cast<object>().Select(f =>
                "id=" + Get(f, "Id") + " progress=" + Get(Get(f, "RouteFollower"), "Progress") + " order=" +
                string.Join(",", ((IEnumerable)Get(f, "Order")).Cast<object>()
                    .Select(line => Get(line, "Product") + ":" + Get(line, "Remaining")).ToArray())).ToArray());
            string workerSummary = string.Join(" | ", ((IEnumerable)Get(mode, "Workers")).Cast<object>().Select(w =>
                Get(w, "Role") + "@" + Get(w, "Position") + " moving=" + Get(w, "IsMoving") +
                " stage=" + Get(w, "ServiceStage") + " pending=" + Get(w, "PendingProduct") +
                " cookUnit=" + Get(w, "CookUnitId")).ToArray());
            Assert.IsTrue((bool)Get(mode,"HasWon"),"Natural full wave must win without teleport, cash or speed changes. " +
                "phase=" + Get(mode, "Phase") + ", spawned=" + Get(mode, "SpawnedFans") + ", served=" + Get(mode, "ServedFans") +
                ", escaped=" + Get(mode, "EscapedFans") + ", active=" + Get(mode, "ActiveFanCount") +
                ", delivered=" + Get(mode, "TotalDelivered") + ", byProduct=" + deliveredSummary + ", coins=" + Get(mode, "Coins") +
                ", escapedFan=" + escapedOrder + ", activeOrders=" + activeFanSummary + ", workers=" + workerSummary);
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
        public IEnumerator PeregrinacionStarterMozoWalksToThePilgrimAndDeliversTheSandwich()
        {
            object mozo = ((IEnumerable)Get(mode, "Workers")).Cast<object>()
                .FirstOrDefault(w => Get(w, "Role").ToString() == "Mozo");
            Assert.IsNotNull(mozo, "Luján must open with a Mozo, as Boca does.");
            int mozoId = (int)Get(mozo, "Id");
            float pixel = (float)Get(mode, "worldUnitsPerLogicalPixel");
            int coinsBefore = (int)Get(mode, "Coins");
            Vector2 sandwichTable = (Vector2)Get(((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                .First(st => Get(st, "Kind").ToString() == "NormalTable"), "WorkPosition");
            // Only Chori pilgrims in this focused check (no Cocacolero is hired), injected on the real screen route.
            Set(mode, "nextFanSpawnAt", float.MaxValue);
            if (((ICollection)Get(mode, "Fans")).Count == 0) Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 0 }, new[] { 1 }));

            for (int expected = 1; expected <= 2; expected++)
            {
                if (expected == 2)
                {
                    // Second pilgrim is already far along the route: the Mozo has to pick up at the
                    // table and walk out along the fence to reach it before the exit.
                    for (int tick = 0; tick < 1200 && ((ICollection)Get(mode, "Fans")).Count > 0; tick++)
                    { Call(mode, "Step", .05f); if (tick % 40 == 0) yield return null; }
                    Assert.AreEqual(0, ((ICollection)Get(mode, "Fans")).Count, "The served first pilgrim leaves the route.");
                    Assert.IsTrue((bool)Call(mode, "TrySpawnFan", new[] { 0 }, new[] { 1 }));
                    object follower = Get(((IList)Get(mode, "Fans"))[0], "RouteFollower");
                    Call(follower, "Advance", (float)Get(follower, "TotalDistance") * .6f / (float)Get(follower, "Speed"));
                }
                float travelBefore = (float)Get(mozo, "TravelDistance");
                float closestToTable = float.PositiveInfinity;
                string lastDiagnostic = DescribeLujanService(mode, mozo);
                object receipt = null;
                for (int tick = 0; tick < 1200 && receipt == null; tick++)
                {
                    Call(mode, "Step", .05f);
                    closestToTable = Mathf.Min(closestToTable, Vector2.Distance((Vector2)Get(mozo, "Position"), sandwichTable));
                    if ((bool)Get(mode, "HasLost")) Assert.Fail("Pilgrim " + expected + " escaped without the Mozo delivering. " + lastDiagnostic);
                    if (tick % 10 == 0) lastDiagnostic = DescribeLujanService(mode, mozo);
                    object[] mine = receipts.Cast<object>().Where(r => (int)Get(r, "WorkerId") == mozoId).ToArray();
                    if (mine.Length >= expected) receipt = mine[expected - 1];
                    if (tick % 40 == 0) yield return null;
                }
                Assert.IsNotNull(receipt, "The starter Mozo must deliver pilgrim " + expected + "'s Chori within 60 simulated seconds.");
                Assert.AreEqual(0, Get(receipt, "Product"));
                Vector2 handOver = (Vector2)Get(receipt, "Source");
                Vector2 pilgrim = (Vector2)Get(receipt, "Target");
                Assert.LessOrEqual(Mathf.Abs(handOver.x - pilgrim.x), 64f * pixel,
                    "The Mozo hands the order over beside pilgrim " + expected + ", not from the kitchen.");
                Assert.Less(handOver.y, pilgrim.y, "The Mozo stays in the worker area below the fence.");
                Assert.LessOrEqual(closestToTable, 2f * pixel, "The Mozo picked the sandwich up at the table, not remotely.");
                if (expected == 2)
                {
                    float walked = ((float)Get(mozo, "TravelDistance") - travelBefore) / pixel;
                    Assert.Greater(walked, 60f, "The Mozo walked from the table out to the distant pilgrim. handOver=" +
                        handOver + " table=" + sandwichTable + " pilgrim=" + pilgrim + " walkedPx=" + walked);
                }
            }
            int choriPrice = Mathf.RoundToInt((float)T("StreetSimulation").GetMethod("OfficialProductPrice", S).Invoke(null, new object[] { 0 }));
            Assert.AreEqual(2, receipts.Count, "Exactly the two Chori orders were delivered.");
            Assert.AreEqual(coinsBefore + choriPrice * 2, (int)Get(mode, "Coins"), "Each sale is charged exactly once.");

            Vector2 post = (Vector2)Get(mozo, "PostPosition");
            for (int tick = 0; tick < 400 && Vector2.Distance((Vector2)Get(mozo, "Position"), post) > .5f * pixel; tick++)
            { Call(mode, "Step", .05f); if (tick % 40 == 0) yield return null; }
            Assert.LessOrEqual(Vector2.Distance((Vector2)Get(mozo, "Position"), post), .5f * pixel,
                "After delivering, the Mozo walks back to its post at the tables.");
            Assert.AreEqual(sandwichTable.y, post.y, .5f * pixel, "The Mozo's post is on the service lane next to the tables.");
        }

        [UnityTest]
        public IEnumerator PeregrinacionFullRosterKeepsRestingWorkersApartCapture()
        {
            // Tall phone proportion: the work area grows toward the fence, the kitchen stays at the bottom.
            yield return CaptureAtResolution("peregrinacion-full-roster-phone-before-610x1356", 610, 1356, false);
            float pixel = (float)Get(mode, "worldUnitsPerLogicalPixel");
            IList workers = (IList)Get(mode, "Workers");
            Type roleKind = T("StreetWorkerRole");
            foreach (int role in new[] { 1, 2, 3, 0, 4 })
                Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role)), "hire " + role);
            Assert.AreEqual(7, workers.Count);
            Set(mode, "nextFanSpawnAt", float.MaxValue);
            Call(mode, "Step", .05f);
            Call(Get(view, "towerDefensePresentation"), "Sync", mode);
            var report = new System.Text.StringBuilder();
            Rect wb = (Rect)Get(mode, "WorkBounds");
            report.Append("WB y=" + Mathf.RoundToInt(wb.yMin / pixel) + ".." + Mathf.RoundToInt(wb.yMax / pixel) + " | ");
            foreach (object w in workers)
                report.Append(Get(w, "Role")).Append("@post").Append(Mathf.RoundToInt(((Vector2)Get(w, "PostPosition")).x / pixel)).Append(',')
                    .Append(Mathf.RoundToInt(((Vector2)Get(w, "PostPosition")).y / pixel)).Append(' ');
            Debug.Log("ROSTER " + report);
            yield return CaptureAtResolution("peregrinacion-full-roster-phone-610x1356", 610, 1356, false);
            for (int i = 0; i < workers.Count; i++)
                for (int j = i + 1; j < workers.Count; j++)
                {
                    Vector2 a = (Vector2)Get(workers[i], "PostPosition"), b = (Vector2)Get(workers[j], "PostPosition");
                    Assert.GreaterOrEqual(Vector2.Distance(a, b), 55f * pixel, "Worker posts overlap: " + report);
                }
        }

        // Records a real Luján run at phone proportion for human review; run it by name only.
        [UnityTest]
        [Explicit("Records gameplay frames to Logs/Acceptance/PeregrinacionVideo-20261117")]
        public IEnumerator PeregrinacionGameplayVideoFrames()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/PeregrinacionVideo-20261117/frames"));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            yield return CaptureAtResolution("peregrinacion-video-setup-610x1356", 610, 1356, false);
            Time.captureDeltaTime = 1f / 20f;
            object presentation = Get(view, "towerDefensePresentation");
            Type roleKind = T("StreetWorkerRole");
            // Hires happen on a schedule so the posts can be seen filling up: (frame, role).
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 1 }, { 25, 0 }, { 45, 4 }, { 65, 3 } };
            int frames = 1000, written = 0, loggedReceipts = 0;
            try
            {
                for (int frame = 0; frame < frames && Get(mode, "Phase").ToString() == "Playing"; frame++)
                {
                    if (hires.TryGetValue(frame, out int role))
                        Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role));
                    Call(mode, "Step", .05f); // one 0.05s step per captured frame: the video plays at 1x
                    if (receipts.Count > loggedReceipts) { Debug.Log("HANDOVER frame=" + written + " product=" + Get(receipts[receipts.Count - 1], "Product")); loggedReceipts = receipts.Count; }
                    Call(presentation, "Sync", mode);
                    yield return new WaitForEndOfFrame();
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(directory, "f" + written.ToString("00000") + ".jpg"), image.EncodeToJPG(88));
                    UnityEngine.Object.Destroy(image);
                    written++;
                }
            }
            finally { Time.captureDeltaTime = 0f; }
            Debug.Log("VIDEO frames=" + written + " phase=" + Get(mode, "Phase") + " delivered=" + Get(mode, "TotalDelivered") + " coins=" + Get(mode, "Coins"));
            Assert.Greater(written, 100);
        }

        // Records a real Camioneros run at phone proportion for human review; run it by name only.
        [UnityTest]
        [Explicit("Records gameplay frames to Logs/Acceptance/CamionerosVideo-20261118")]
        public IEnumerator CamionerosGameplayVideoFrames()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/CamionerosVideo-20261118/frames"));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            yield return CaptureAtResolution("camioneros-video-setup-610x1356", 610, 1356, false);
            Time.captureDeltaTime = 1f / 20f;
            object presentation = Get(view, "towerDefensePresentation");
            Type roleKind = T("StreetWorkerRole");
            // Hires happen on a schedule so the posts can be seen filling up: (frame, role).
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 0 }, { 25, 1 }, { 45, 0 } };
            int frames = 1000, written = 0, loggedReceipts = 0;
            try
            {
                for (int frame = 0; frame < frames && Get(mode, "Phase").ToString() == "Playing"; frame++)
                {
                    if (hires.TryGetValue(frame, out int role))
                        Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role));
                    Call(mode, "Step", .05f); // one 0.05s step per captured frame: the video plays at 1x
                    if (receipts.Count > loggedReceipts) { Debug.Log("HANDOVER frame=" + written + " product=" + Get(receipts[receipts.Count - 1], "Product")); loggedReceipts = receipts.Count; }
                    Call(presentation, "Sync", mode);
                    yield return new WaitForEndOfFrame();
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(directory, "f" + written.ToString("00000") + ".jpg"), image.EncodeToJPG(88));
                    UnityEngine.Object.Destroy(image);
                    written++;
                }
            }
            finally { Time.captureDeltaTime = 0f; }
            Debug.Log("VIDEO frames=" + written + " phase=" + Get(mode, "Phase") + " delivered=" + Get(mode, "TotalDelivered") + " coins=" + Get(mode, "Coins"));
            Assert.Greater(written, 100);
        }

        // A worker never starts serving a pilgrim while one further ahead still waits for that same role.
        [UnityTest]
        public IEnumerator PeregrinacionSameRoleNeverServesBehindAPilgrimStillWaiting()
        {
            Type roleKind = T("StreetWorkerRole");
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 1 }, { 25, 0 }, { 45, 4 }, { 65, 3 } };
            var reservedBefore = new System.Collections.Generic.HashSet<int>();
            int assignments = 0;
            for (int frame = 0; frame < 900 && Get(mode, "Phase").ToString() == "Playing"; frame++)
            {
                if (hires.TryGetValue(frame, out int role))
                    Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role)));
                Call(mode, "Step", .05f);
                if (frame % 100 == 0) yield return null;
                var fans = ((IEnumerable)Get(mode, "Fans")).Cast<object>().ToList();
                var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>().ToList();
                foreach (object fan in fans)
                {
                    int reservedBy = (int)Get(fan, "ReservedByWorker");
                    int fanId = (int)Get(fan, "Id");
                    if (reservedBy == 0) { reservedBefore.Remove(fanId); continue; }
                    if (!reservedBefore.Add(fanId)) continue;
                    object worker = workers.FirstOrDefault(w => (int)Get(w, "Id") == reservedBy);
                    if (worker == null) continue;
                    assignments++;
                    int serving = (int)Get(worker, "PendingProduct");
                    object servingRole = Call(mode, "GetDeliveryRole", serving);
                    float progress = (float)Get(Get(fan, "RouteFollower"), "Progress");
                    foreach (object ahead in fans)
                    {
                        if ((int)Get(ahead, "Id") == fanId || (bool)Get(Get(ahead, "RouteFollower"), "HasExited") ||
                            (float)Get(Get(ahead, "RouteFollower"), "Progress") <= progress ||
                            (int)Get(ahead, "ReservedByWorker") != 0) continue; // already being served by another worker
                        foreach (object line in (IEnumerable)Get(ahead, "Order"))
                        {
                            if ((int)Get(line, "Remaining") <= 0) continue;
                            int product = (int)Get(line, "Product");
                            if (!Equals(Call(mode, "GetDeliveryRole", product), servingRole)) continue;
                            Assert.IsFalse((bool)Call(mode, "IsFanReachableForRole", ahead, servingRole, product),
                                "Worker " + reservedBy + " (" + servingRole + ") started serving fan " + fanId + " while fan " +
                                Get(ahead, "Id") + " ahead still waited for product " + product + " at frame " + frame);
                        }
                    }
                }
            }
            Assert.Greater(assignments, 5);
        }

        // Regression: the outer deposit slot of two cooks fell outside the work area, so the starter cook stood idle
        // holding a sandwich for most of the run while its partner did all the cooking.
        [UnityTest]
        public IEnumerator PeregrinacionEveryCookKeepsCookingWithATeamMate()
        {
            Type roleKind = T("StreetWorkerRole");
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 1 }, { 25, 0 }, { 45, 4 }, { 65, 3 } };
            var cookingSteps = new System.Collections.Generic.Dictionary<int, int>();
            int steps = 0;
            for (int frame = 0; frame < 900 && Get(mode, "Phase").ToString() == "Playing"; frame++, steps++)
            {
                if (hires.TryGetValue(frame, out int role))
                    Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role)));
                Call(mode, "Step", .05f);
                if (frame % 100 == 0) yield return null;
                foreach (object w in (IEnumerable)Get(mode, "Workers"))
                {
                    string r = Get(w, "Role").ToString();
                    if ((r != "Parrillero" && r != "ParrilleroPremium") || !(bool)Get(w, "IsCooking")) continue;
                    int id = (int)Get(w, "Id");
                    cookingSteps[id] = cookingSteps.TryGetValue(id, out int n) ? n + 1 : 1;
                }
            }
            int cooks = ((IEnumerable)Get(mode, "Workers")).Cast<object>().Count(w => Get(w, "Role").ToString() == "Parrillero");
            Assert.GreaterOrEqual(cooks, 2, "The run hires a second Parrillero.");
            foreach (object w in ((IEnumerable)Get(mode, "Workers")).Cast<object>().Where(x => Get(x, "Role").ToString() == "Parrillero"))
            {
                cookingSteps.TryGetValue((int)Get(w, "Id"), out int cooked);
                Assert.Greater(cooked, steps / 4, "Parrillero " + Get(w, "Id") + " cooked only " + cooked + " of " + steps + " steps.");
            }
        }

        // Cook hand-over slots must equal Boca's DepositPosition(product, index, 4), mapped onto Luján's table.
        [UnityTest]
        public IEnumerator PeregrinacionCookDepositSlotsMatchBocaLayout()
        {
            yield return null;
            Type kindType = T("TowerDefenseStationKind");
            object table = Call(mode, "FindPlacedStation", Enum.Parse(kindType, "NormalTable"));
            Rect bounds = (Rect)Get(table, "Bounds");
            Rect source = (Rect)mode.GetType().GetMethod("BocaKitchenSourceBounds", I, null, new[] { kindType }, null)
                .Invoke(mode, new[] { Enum.Parse(kindType, "NormalTable") });
            float scale = (float)Call(mode, "BocaKitchenFitScale") * (float)Get(mode, "WorldUnitsPerLogicalPixel");
            object layout = mode.GetType().GetProperty("LujanBocaKitchenVisualLayout", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public).GetValue(null);
            for (int index = 0; index < 4; index++)
            {
                Vector2 boca = (Vector2)layout.GetType().GetMethod("DepositPosition", new[] { typeof(int), typeof(int), typeof(int) })
                    .Invoke(layout, new object[] { 0, index, 4 });
                // Same proportions as Boca, scaled by how much larger the drawn body is than Boca's 60 px body.
                float body = (float)Get(mode, "WorldUnitsPerLogicalPixel") * (float)mode.GetType().GetField("ConstructionWorkerWidthLogical", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
                float k = body / (60f * scale);
                var expected = new Vector2(bounds.center.x + (boca.x - source.center.x) * scale * k,
                    bounds.yMin - (14f + (boca.y - (source.yMax + 14f)) * k) * scale);
                var actual = (Vector2)Call(mode, "TableDepositPosition", table, index);
                float half = body * .5f; // the outer slot is kept inside the work area
                Rect work = (Rect)Get(mode, "WorkBounds");
                expected.x = Mathf.Clamp(expected.x, work.xMin + half, work.xMax - half);
                Assert.AreEqual(expected.x, actual.x, .03f, "x of deposit slot " + index);
                Assert.AreEqual(expected.y, actual.y, .03f, "y of deposit slot " + index);
            }
        }

        // The kitchen must be Boca's own layout at (almost) its own size: same sizes, same distances, one uniform scale.
        [UnityTest]
        public IEnumerator PeregrinacionKitchenKeepsBocaSizeAndDistances()
        {
            yield return null;
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            float fit = (float)Call(mode, "BocaKitchenFitScale");
            Assert.GreaterOrEqual(fit, .99f, "The kitchen should be drawn at Boca's size.");
            Rect work = (Rect)Get(mode, "WorkBounds");
            Type kindType = T("TowerDefenseStationKind");
            Type modeType = mode.GetType();
            object layout = modeType.GetProperty("LujanBocaKitchenVisualLayout", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public).GetValue(null);
            string[] props = { "NormalGrillBounds", "PremiumGrillBounds", "NormalTableBounds", "PremiumTableBounds", "FernetTableBounds", "CocaBarrelBounds", "BeerBarrelBounds" };
            string[] kinds = { "NormalGrill", "PremiumGrill", "NormalTable", "PremiumTable", "FernetTable", "CocaBarrel", "BeerBarrel" };
            var bocaRects = props.Select(n => (Rect)layout.GetType().GetProperty(n).GetValue(layout)).ToArray();
            float left = bocaRects.Min(r => r.xMin), bottom = bocaRects.Max(r => r.yMax);
            float scale = fit * pixel;
            for (int i = 0; i < kinds.Length; i++)
            {
                Rect b = (Rect)Get(Call(mode, "FindPlacedStation", Enum.Parse(kindType, kinds[i])), "Bounds");
                Rect boca = bocaRects[i];
                Assert.AreEqual(work.xMin + (boca.center.x - left) * scale, b.center.x, .005f, kinds[i] + " x relative to Boca");
                Assert.AreEqual(work.yMin + (bottom - boca.center.y) * scale, b.center.y, .005f, kinds[i] + " y relative to Boca");
                Assert.AreEqual(boca.height * scale, b.height, .005f, kinds[i] + " height");
                Assert.AreEqual(boca.width * scale, b.width, 2.2f * scale, kinds[i] + " width only loses Boca's 1.2 px overlap");
            }
        }

        // On a tall phone screen Boca stretches its kitchen's y positions and heights with the screen height and starts
        // its footer at PlayfieldBottom * stretch. Luján must put every piece and its footer at those same screen rows.
        [UnityTest]
        public IEnumerator PeregrinacionKitchenAndFooterSitAtBocaScreenRows()
        {
            yield return CaptureAtResolution("peregrinacion-boca-screen-rows-610x1356", 610, 1356, false);
            float logicalHeight = (float)view.GetType().GetField("logicalCanvasHeight", I).GetValue(view);
            float stretch = logicalHeight / 960f;
            Assert.Greater(stretch, 1.2f, "A tall phone must exercise Boca's vertical stretch.");
            Type kindType = T("TowerDefenseStationKind");
            object layout = mode.GetType().GetProperty("LujanBocaKitchenVisualLayout", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public).GetValue(null);
            string[] props = { "NormalGrillBounds", "PremiumGrillBounds", "NormalTableBounds", "PremiumTableBounds", "FernetTableBounds", "CocaBarrelBounds", "BeerBarrelBounds" };
            string[] kinds = { "NormalGrill", "PremiumGrill", "NormalTable", "PremiumTable", "FernetTable", "CocaBarrel", "BeerBarrel" };
            MethodInfo toLogical = view.GetType().GetMethod("PilgrimWorldToLogical", I);
            for (int i = 0; i < kinds.Length; i++)
            {
                Rect boca = (Rect)layout.GetType().GetProperty(props[i]).GetValue(layout);
                Rect world = (Rect)Get(Call(mode, "FindPlacedStation", Enum.Parse(kindType, kinds[i])), "Bounds");
                Rect onScreen = (Rect)toLogical.Invoke(view, new object[] { world });
                Assert.AreEqual(boca.center.x, onScreen.center.x, 1.5f, kinds[i] + " column on screen");
                Assert.AreEqual((boca.y + boca.height * .5f) * stretch, onScreen.center.y, 1.5f, kinds[i] + " row on screen");
                Assert.AreEqual(boca.height * stretch, onScreen.height, 1.5f, kinds[i] + " height on screen");
            }
            float bocaFooterTop = (float)T("StreetKitchenLayout").GetField("ActivePlayfieldBottom").GetRawConstantValue() * stretch;
            Assert.AreEqual(bocaFooterTop, (float)CallStatic(T("TowerDefenseResponsiveLayout"), "PilgrimFooterTop", logicalHeight, 14), .01f);
        }

        // Camioneros is a horizontal-fence kitchen level that copies Nueva Chicago's kitchen and footer.
        [UnityTest]
        public IEnumerator CamionerosChicagoKitchenStartsWithThreeStationsAndTheChicagoFooter()
        {
            Assert.AreEqual(9, requestedLevelId);
            Assert.IsTrue((bool)Get(mode, "FreeConstruction"), "Camioneros must be a free-construction (kitchen) level.");
            Assert.IsTrue((bool)Get(mode, "IsPassiveKitchenLevel"), "Camioneros must use Nueva Chicago's kitchen.");
            // Like Nueva Chicago: nobody cooks and the sandwich table is always full; the Parrillero only serves.
            object kitchen = Get(mode, "Kitchen");
            Assert.IsFalse((bool)Get(kitchen, "ActiveCooks"), "Chicago's kitchen is the traditional, passive one.");
            Assert.GreaterOrEqual((int)Call(kitchen, "AvailableTableCount", 0, 0), 12, "The sandwich table starts full.");
            Assert.AreEqual(3, ((IList)Get(mode, "PlacedStations")).Count);
            CollectionAssert.AreEquivalent(new[] { "NormalGrill", "NormalTable", "CocaBarrel" },
                ((IList)Get(mode, "PlacedStations")).Cast<object>().Select(st => Get(st, "Kind").ToString()).ToArray());
            Type roleKind = T("StreetWorkerRole");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 0)), "Hire a Parrillero at its post.");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 1)), "Hire a Cocacolero at its post.");
            yield return CaptureAtResolution("camioneros-chicago-kitchen-610x1356", 610, 1356, false);
            Assert.AreEqual(3, Get(view, "PilgrimItemCount"), "Velocidad, Parrillero and Cocacolero: Chicago's three cards.");
        }

        // On a tall phone Camioneros' footer and its three stations must sit at the screen rows of Nueva Chicago's.
        [UnityTest]
        public IEnumerator CamionerosKitchenAndFooterSitAtChicagoScreenRows()
        {
            yield return CaptureAtResolution("camioneros-chicago-screen-rows-610x1356", 610, 1356, false);
            float logicalHeight = (float)view.GetType().GetField("logicalCanvasHeight", I).GetValue(view);
            float stretch = logicalHeight / 960f;
            Assert.Greater(stretch, 1.2f, "A tall phone must exercise the vertical stretch.");
            Type kindType = T("TowerDefenseStationKind");
            object layout = mode.GetType().GetProperty("SourceKitchenLayout", I).GetValue(mode);
            var pieces = new System.Collections.Generic.Dictionary<string, string>
                { { "NormalGrill", "NormalGrillBounds" }, { "NormalTable", "NormalTableBounds" }, { "CocaBarrel", "CocaBarrelBounds" } };
            MethodInfo toLogical = view.GetType().GetMethod("PilgrimWorldToLogical", I);
            foreach (var piece in pieces)
            {
                Rect chicago = (Rect)layout.GetType().GetProperty(piece.Value).GetValue(layout);
                Rect world = (Rect)Get(Call(mode, "FindPlacedStation", Enum.Parse(kindType, piece.Key)), "Bounds");
                Rect onScreen = (Rect)toLogical.Invoke(view, new object[] { world });
                Assert.AreEqual(chicago.center.x, onScreen.center.x, 1.5f, piece.Key + " column on screen");
                Assert.AreEqual((chicago.y + chicago.height * .5f) * stretch, onScreen.center.y, 1.5f, piece.Key + " row on screen");
                Assert.AreEqual(chicago.height * stretch, onScreen.height, 1.5f, piece.Key + " height on screen");
            }
            float chicagoFooterTop = (float)layout.GetType().GetProperty("PlayfieldBottom").GetValue(layout) * stretch;
            Assert.AreEqual(665f * stretch, chicagoFooterTop, .01f, "Chicago's playfield bottom is row 665.");
            Assert.AreEqual(chicagoFooterTop, (float)CallStatic(T("TowerDefenseResponsiveLayout"), "PilgrimFooterTop", logicalHeight, 9), .01f);
            Rect first = (Rect)Call(view, "PilgrimCardBounds", 0);
            Assert.AreEqual(120f, first.height, "Chicago's three cards are 156x120.");
            Assert.AreEqual(156f, first.width);
            Assert.AreEqual(Rect.zero, (Rect)Call(view, "PilgrimCardBounds", 3), "Chicago's footer has only three cards.");
        }

        // The Parrillero cooks and also hands the sandwich to the pilgrim at the fence; the Cocacolero brings the Coca.
        [UnityTest]
        public IEnumerator CamionerosParrilleroAndCocacoleroDeliverOnlyWhatTheKitchenSells()
        {
            Type roleKind = T("StreetWorkerRole");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 0)));
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 1)));
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 0)));
            for (int frame = 0; frame < 900 && Get(mode, "Phase").ToString() == "Playing"; frame++)
            {
                Call(mode, "Step", .05f);
                if (frame % 100 == 0) yield return null;
            }
            Assert.GreaterOrEqual(receipts.Count, 4, "The crew must serve real orders.");
            Assert.IsFalse(((IEnumerable)Get(mode, "Workers")).Cast<object>().Any(w => (bool)Get(w, "IsCooking")), "Chicago's Parrilleros never cook.");
            Assert.GreaterOrEqual((int)Call(Get(mode, "Kitchen"), "AvailableTableCount", 0, 0), 12, "The table stays full.");
            var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>().ToList();
            foreach (object receipt in receipts)
            {
                int product = (int)Get(receipt, "Product");
                Assert.IsTrue(product == 0 || product == 4, "Camioneros sells only sandwich (0) and Coca (4): " + product);
                object worker = workers.First(w => (int)Get(w, "Id") == (int)Get(receipt, "WorkerId"));
                Assert.AreEqual(product == 0 ? "Parrillero" : "Cocacolero", Get(worker, "Role").ToString(),
                    "The Parrillero serves sandwiches and the Cocacolero serves Coca.");
            }
        }

        // Chicago's worker timing: the grab and the hand-over each take pickupSeconds, nothing waits between deliveries.
        [UnityTest]
        public IEnumerator CamionerosWorkersFollowChicagosGrabAndHandOverTiming()
        {
            Type roleKind = T("StreetWorkerRole");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 1)));
            object balance = Get(game, "Balance");
            float pickup = (float)balance.GetType().GetField("pickupSeconds").GetValue(balance);
            Assert.AreEqual(.28f, pickup, .001f);
            object coca = ((IEnumerable)Get(mode, "Workers")).Cast<object>().First(w => Get(w, "Role").ToString() == "Cocacolero");
            float assigned = -1f, moving = -1f, delivered = -1f;
            for (int frame = 0; frame < 600 && delivered < 0f; frame++)
            {
                Call(mode, "Step", .02f);
                if (frame % 100 == 0) yield return null;
                float t = frame * .02f;
                // The dwell starts when the worker has reached the barrel (it waits half way, so it first walks there).
                if (assigned < 0f && (int)Get(coca, "ServiceStage") == 1 && !(bool)Get(coca, "IsMoving")) assigned = t;
                if (assigned >= 0f && moving < 0f && (bool)Get(coca, "IsMoving") && (int)Get(coca, "ServiceStage") == 2) moving = t;
                if (assigned >= 0f && moving >= 0f && receipts.Count > 0) delivered = t;
            }
            Assert.GreaterOrEqual(assigned, 0f, "The Cocacolero must be assigned an order.");
            Assert.AreEqual(pickup, moving - assigned, .05f, "The grab at the barrel lasts pickupSeconds (0.28 s), like Chicago.");
            Assert.GreaterOrEqual(delivered, moving, "A delivery must follow.");
            Assert.AreEqual(0f, (float)Get(coca, "DeliveryCooldownSeconds"), .001f, "Chicago has no wait between deliveries.");
        }

        // Camioneros' idle delivery crew waits in the middle band between its post and the fence, each at its own spot.
        [UnityTest]
        public IEnumerator CamionerosIdleWorkersWaitHalfWayBetweenPostAndFence()
        {
            Type roleKind = T("StreetWorkerRole");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 0)));
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 1)));
            mode.GetType().GetField("stopSpawning", I).SetValue(mode, true); // no pilgrims: the crew has nothing to deliver
            float laneY = ((Vector2)mode.GetType().GetMethod("GetFanServiceAnchor", I, null, new[] { typeof(Vector2) }, null).Invoke(mode, new object[] { Vector2.zero })).y;
            var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>().ToList();
            bool bothResting = false;
            for (int frame = 0; frame < 1200 && !bothResting; frame++)
            {
                Call(mode, "Step", .05f);
                if (frame % 100 == 0) yield return null;
                bothResting = frame > 20 && workers.All(w => (int)Get(w, "ServiceStage") == 0 && !(bool)Get(w, "IsMoving") &&
                    (float)Get(w, "ServeAnimationSeconds") <= 0f);
            }
            Assert.IsTrue(bothResting, "At some point both workers must be idle and resting.");
            var xs = new System.Collections.Generic.List<float>();
            foreach (object worker in workers)
            {
                float postY = ((Vector2)worker.GetType().GetField("PostPosition", I).GetValue(worker)).y;
                Vector2 position = (Vector2)Get(worker, "Position");
                Assert.Greater(position.y, postY + .1f, Get(worker, "Role") + " must have left its post toward the fence.");
                Assert.Less(position.y, laneY - .1f, Get(worker, "Role") + " must wait before the fence line.");
                xs.Add(position.x);
            }
            Assert.Greater(Mathf.Abs(xs[0] - xs[1]), .3f, "Each idle worker waits at its own spot.");
        }

        // Los Redondos is a horizontal-fence kitchen level that copies Ferro's kitchen and footer.
        [UnityTest]
        public IEnumerator LosRedondosFerroKitchenStartsWithSixStationsAndTheFerroFooter()
        {
            Assert.AreEqual(10, requestedLevelId);
            Assert.IsTrue((bool)Get(mode, "FreeConstruction"));
            Assert.IsTrue((bool)Get(mode, "IsPassiveKitchenLevel"), "Los Redondos uses Ferro's passive kitchen.");
            object kitchen = Get(mode, "Kitchen");
            Assert.IsFalse((bool)Get(kitchen, "ActiveCooks"));
            foreach (int product in new[] { 0, 1, 2 })
                Assert.GreaterOrEqual((int)Call(kitchen, "AvailableTableCount", product, 0), 12, "Ferro's sandwich tables start full: product " + product);
            CollectionAssert.AreEquivalent(new[] { "NormalGrill", "PremiumGrill", "NormalTable", "PremiumTable", "CocaBarrel", "BeerBarrel" },
                ((IList)Get(mode, "PlacedStations")).Cast<object>().Select(st => Get(st, "Kind").ToString()).ToArray());
            Type roleKind = T("StreetWorkerRole");
            foreach (int role in new[] { 0, 2, 1 })
                Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role)), "Hire role " + role);
            yield return CaptureAtResolution("losredondos-ferro-kitchen-610x1356", 610, 1356, false);
            Assert.AreEqual(4, Get(view, "PilgrimItemCount"), "Velocidad, Parrillero, Parrillero Premium and Cocacolero: Ferro's four cards.");
        }

        [UnityTest]
        public IEnumerator LosRedondosKitchenAndFooterSitAtFerroScreenRows()
        {
            yield return CaptureAtResolution("losredondos-ferro-screen-rows-610x1356", 610, 1356, false);
            float logicalHeight = (float)view.GetType().GetField("logicalCanvasHeight", I).GetValue(view);
            float stretch = logicalHeight / 960f;
            Type kindType = T("TowerDefenseStationKind");
            object layout = mode.GetType().GetProperty("SourceKitchenLayout", I).GetValue(mode);
            var pieces = new System.Collections.Generic.Dictionary<string, string>
            {
                { "NormalGrill", "NormalGrillBounds" }, { "PremiumGrill", "PremiumGrillBounds" }, { "NormalTable", "NormalTableBounds" },
                { "PremiumTable", "PremiumTableBounds" }, { "CocaBarrel", "CocaBarrelBounds" }, { "BeerBarrel", "BeerBarrelBounds" }
            };
            MethodInfo toLogical = view.GetType().GetMethod("PilgrimWorldToLogical", I);
            foreach (var piece in pieces)
            {
                Rect ferro = (Rect)layout.GetType().GetProperty(piece.Value).GetValue(layout);
                Rect world = (Rect)Get(Call(mode, "FindPlacedStation", Enum.Parse(kindType, piece.Key)), "Bounds");
                Rect onScreen = (Rect)toLogical.Invoke(view, new object[] { world });
                Assert.AreEqual(ferro.center.x, onScreen.center.x, 1.5f, piece.Key + " column on screen");
                Assert.AreEqual((ferro.y + ferro.height * .5f) * stretch, onScreen.center.y, 1.5f, piece.Key + " row on screen");
                Assert.AreEqual(ferro.height * stretch, onScreen.height, 1.5f, piece.Key + " height on screen");
            }
            Assert.AreEqual(665f * stretch, (float)CallStatic(T("TowerDefenseResponsiveLayout"), "PilgrimFooterTop", logicalHeight, 10), .01f);
            Rect first = (Rect)Call(view, "PilgrimCardBounds", 0);
            Assert.AreEqual(104f, first.height, "Ferro's four cards are 234x104.");
            Assert.AreEqual(234f, first.width);
            Assert.AreEqual(Rect.zero, (Rect)Call(view, "PilgrimCardBounds", 4), "Ferro's footer has only four cards.");
        }

        [UnityTest]
        public IEnumerator LosRedondosCrewDeliversEachProductWithItsOwnRole()
        {
            Type roleKind = T("StreetWorkerRole");
            foreach (int role in new[] { 0, 2, 1, 1 })
                Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role)));
            for (int frame = 0; frame < 1500 && Get(mode, "Phase").ToString() == "Playing"; frame++)
            {
                Call(mode, "Step", .05f);
                if (frame % 100 == 0) yield return null;
            }
            string pending = string.Join(";", ((IEnumerable)Get(mode, "Fans")).Cast<object>().Select(f => "fan" + Get(f, "Id") + ":" +
                string.Join(",", ((IEnumerable)Get(f, "Order")).Cast<object>().Where(l => (int)Get(l, "Remaining") > 0).Select(l => "p" + Get(l, "Product") + "x" + Get(l, "Remaining")))));
            Assert.GreaterOrEqual(receipts.Count, 6, "The crew must serve real orders. phase=" + Get(mode, "Phase") + " escaped=" + Get(mode, "EscapedFans") +
                " delivered=" + Get(mode, "TotalDelivered") + " spawned=" + Get(mode, "SpawnedFans") + " pending=" + pending);
            var workers = ((IEnumerable)Get(mode, "Workers")).Cast<object>().ToList();
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (object receipt in receipts)
            {
                int product = (int)Get(receipt, "Product");
                seen.Add(product);
                Assert.Contains(product, new[] { 0, 1, 2, 4, 6 }, "Ferro's menu: " + product);
                object worker = workers.First(w => (int)Get(w, "Id") == (int)Get(receipt, "WorkerId"));
                string expected = product <= 1 ? "Parrillero" : product == 2 ? "ParrilleroPremium" : "Cocacolero";
                Assert.AreEqual(expected, Get(worker, "Role").ToString(), "Product " + product + " is served by its specialty.");
            }
            Assert.GreaterOrEqual(seen.Count, 3, "More than one product must have been served.");
        }

        [UnityTest]
        [Explicit("Records a real Los Redondos run to Logs/Acceptance/RedondosVideo-20261118")]
        public IEnumerator LosRedondosGameplayVideoFrames()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RedondosVideo-20261118/frames"));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            yield return CaptureAtResolution("losredondos-video-setup-610x1356", 610, 1356, false);
            Time.captureDeltaTime = 1f / 20f;
            object presentation = Get(view, "towerDefensePresentation");
            Type roleKind = T("StreetWorkerRole");
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 0 }, { 25, 1 }, { 45, 2 }, { 65, 0 } };
            int written = 0;
            try
            {
                for (int frame = 0; frame < 1000 && Get(mode, "Phase").ToString() == "Playing"; frame++)
                {
                    if (hires.TryGetValue(frame, out int role)) Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role));
                    Call(mode, "Step", .05f);
                    Call(presentation, "Sync", mode);
                    yield return new WaitForEndOfFrame();
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(directory, "f" + written.ToString("00000") + ".jpg"), image.EncodeToJPG(88));
                    UnityEngine.Object.Destroy(image);
                    written++;
                }
            }
            finally { Time.captureDeltaTime = 0f; }
            Assert.Greater(written, 100);
        }

        [UnityTest]
        [Explicit("Captures the empty stage (La Renga mural, sidewalk, Chicago footer; no fences, workers, tables or fans) to Logs/Acceptance/RengaEmptyStage-20261118")]
        public IEnumerator CamionerosFooterWithRengaMuralEmptyStageCapture()
        {
            // Level 9 supplies the Chicago footer; the La Renga mural replaces the backdrop, framed exactly as La Renga's (0,0,1,1) source.
            Texture2D renga = Resources.Load<Texture2D>("street-background-larenga-autodromo-v1");
            Assert.NotNull(renga);
            const int width = 2172, height = 1724, bandTop = 172;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            Texture2D composite = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix(); GL.LoadPixelMatrix(0, width, height, 0);
                Graphics.DrawTexture(new Rect(0, bandTop, width, renga.height), renga);
                GL.PopMatrix();
                composite.ReadPixels(new Rect(0, 0, width, height), 0, 0); composite.Apply();
            }
            finally { RenderTexture.active = previousActive; RenderTexture.ReleaseTemporary(target); }
            Set(view, "towerDefenseBackdrop", composite);
            Set(view, "towerDefensePresentation", null);
            yield return CaptureAtResolution("renga-empty-stage-610x1356", 610, 1356);
            Debug.Log("RENGA EMPTY STAGE captured in " + Path.Combine(CaptureDirectory, "Level09"));
        }

        [UnityTest]
        public IEnumerator LaRengaCShapedLayoutCopiesFerroKitchenAndFooter()
        {
            yield return CaptureAtResolution("larenga-c-layout-610x1356", 610, 1356);
            Assert.AreEqual("CShapedSingleFence", Get(mode, "RouteKind").ToString());
            Assert.IsTrue((bool)Get(mode, "FreeConstruction"));
            Rect work = (Rect)Get(mode, "WorkBounds");
            float pixel = (float)Get(mode, "WorldUnitsPerLogicalPixel");
            Type layoutType = T("StreetKitchenLayout");
            // Ferro's kitchen with La Renga's narrow grills (two columns less of food, grills shrunk to hold it).
            object ferro = Activator.CreateInstance(layoutType, new object[] { new[] { 0, 1, 2, 4, 6 }, false, true });
            var source = new System.Collections.Generic.Dictionary<string, Rect>
            {
                { "NormalGrill", (Rect)Get(ferro, "NormalGrillBounds") }, { "PremiumGrill", (Rect)Get(ferro, "PremiumGrillBounds") },
                { "NormalTable", (Rect)Get(ferro, "NormalTableBounds") }, { "PremiumTable", (Rect)Get(ferro, "PremiumTableBounds") },
                { "CocaBarrel", (Rect)Get(ferro, "CocaBarrelBounds") }, { "BeerBarrel", (Rect)Get(ferro, "BeerBarrelBounds") }
            };
            var placed = new System.Collections.Generic.Dictionary<string, Rect>();
            foreach (object station in (IEnumerable)Get(mode, "PlacedStations"))
                placed[Get(station, "Kind").ToString()] = (Rect)Get(station, "Bounds");
            CollectionAssert.AreEquivalent(source.Keys, placed.Keys, "La Renga installs exactly Ferro's six pieces.");
            // The C's pocket is narrower than Ferro's 520 px counter, so the whole kitchen shrinks uniformly to fit:
            // Ferro's sizes and x distances times that single factor.
            float fit = (float)Call(mode, "BocaKitchenFitScale");
            Assert.LessOrEqual(fit, 1f); Assert.Greater(fit, .8f, "the kitchen is at most slightly smaller than Ferro's (full size once the tables are halved)");
            foreach (var pair in source)
                Assert.AreEqual(pair.Value.width * fit, placed[pair.Key].width / pixel, 1.5f, pair.Key + " width");
            Rect srcTable = source["NormalTable"], table = placed["NormalTable"];
            foreach (var pair in source)
                Assert.AreEqual((pair.Value.xMin - srcTable.xMin) * fit, (placed[pair.Key].xMin - table.xMin) / pixel, 1.5f, pair.Key + " x distance to the table");
            Assert.Greater(table.yMin, placed["NormalGrill"].yMin, "Ferro's grills are the front row, under the tables and barrels.");
            float left = float.MaxValue, right = float.MinValue;
            foreach (Rect r in placed.Values) { left = Mathf.Min(left, r.xMin); right = Mathf.Max(right, r.xMax); }
            Assert.AreEqual(work.xMin + 12f * pixel, left, 1.5f * pixel, "the kitchen starts 12 logical px from the pocket's left edge");
            // The halved tables pull the barrels in: a worker can walk along the right fence past the last barrel. (The premium
            // grill still reaches further right than the barrels, so the strip beside the grills is the narrower one.)
            Debug.Log("LARENGA vertical: pocket top - Coca top = " + ((work.yMax - placed["CocaBarrel"].yMax) / pixel) + " px; barrel height = " + (placed["CocaBarrel"].height / pixel) + " px; pocket height = " + (work.height / pixel) + " px; kitchen bottom - pocket bottom = " + ((placed["NormalGrill"].yMin - work.yMin) / pixel) + " px");
            Debug.Log("LARENGA left margin (logical px from the pocket left edge): normalTable=" + ((placed["NormalTable"].xMin - work.xMin) / pixel)+ " premiumTable=" + ((placed["PremiumTable"].xMin - work.xMin) / pixel) + " normalGrill=" + ((placed["NormalGrill"].xMin - work.xMin) / pixel)+ " | table width=" + (placed["NormalTable"].width / pixel) + " | pocket left=" + work.xMin + " pixel=" + pixel);
            float barrelsRight = Mathf.Max(placed["CocaBarrel"].xMax, placed["BeerBarrel"].xMax);
            float stripBesideBarrels = (work.xMax - barrelsRight) / pixel, stripBesideGrills = (work.xMax - right) / pixel;
            Debug.Log("LARENGA strip right of the barrels = " + stripBesideBarrels + " / right of the grills = " + stripBesideGrills + " (logical px)");
            Assert.GreaterOrEqual(stripBesideBarrels, 28f, "a free strip along the right fence beside the barrels");
            // Like the other horizontal kitchen levels: nobody starts on the crew and the kitchen is passive.
            Assert.AreEqual(0, ((ICollection)Get(mode, "Workers")).Count);
            Assert.IsTrue((bool)Get(mode, "IsPassiveKitchenLevel"), "La Renga plays like the other horizontal kitchen levels.");
            // The footer is Ferro's / Los Redondos': the same top as ID10 on the same screen.
            Type layoutHelper = T("TowerDefenseResponsiveLayout");
            float canvas = (float)Get(view, "logicalCanvasHeight");
            Assert.AreEqual((float)layoutHelper.GetMethod("PilgrimFooterTop").Invoke(null, new object[] { canvas, 10 }),
                (float)layoutHelper.GetMethod("PilgrimFooterTop").Invoke(null, new object[] { canvas, 13 }), .001f);
        }

        [UnityTest]
        public IEnumerator LaRengaWorkersNeverStepOnAnyKitchenPiece()
        {
            yield return null;
            Type roleKind = T("StreetWorkerRole");
            object[] hires = { 0, 1, 0, 1, 0, 1 };
            int[] hireFrames = { 1, 2, 3, 4, 5, 6 }; // the whole crew from the start, so the run lasts
            int violations = 0, checks = 0, hired = 0;
            string first = "";
            for (int frame = 0; frame < 1800 && Get(mode, "Phase").ToString() == "Playing"; frame++)
            {
                if (hired < hireFrames.Length && frame == hireFrames[hired])
                    Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, hires[hired++]));
                Call(mode, "Step", .05f);
                foreach (object worker in (IEnumerable)Get(mode, "Workers"))
                {
                    Vector2 position = (Vector2)Get(worker, "Position");
                    Rect feet = (Rect)Call(mode, "WorkerFeet", position);
                    foreach (object station in (IEnumerable)Get(mode, "PlacedStations"))
                    {
                        checks++;
                        if (!feet.Overlaps((Rect)Get(station, "Bounds"))) continue;
                        violations++;
                        if (first.Length == 0)
                            first = Get(worker, "Role") + " feet " + feet + " over " + Get(station, "Kind") + " " + Get(station, "Bounds") + " at frame " + frame;
                    }
                }
            }
            Debug.Log("LARENGA solid kitchen: checks=" + checks + " violations=" + violations + " phase=" + Get(mode, "Phase") + " served=" + Get(mode, "ServedFans") + " " + first);
            Assert.AreEqual(0, violations, "No worker may walk over a table, grill or barrel. First: " + first);
            Assert.Greater((int)Get(mode, "ServedFans"), 0, "the crew still serves fans");
        }

        [UnityTest]
        [Explicit("Captures La Renga's C stage without workers: with its kitchen and without it, for layout mock-ups")]
        public IEnumerator LaRengaKitchenOnlyAndBaseCaptures()
        {
            object presentation = Get(view, "towerDefensePresentation");
            ((IList)mode.GetType().GetField("workers", I).GetValue(mode)).Clear();
            Call(presentation, "Sync", mode);
            yield return CaptureAtResolution("larenga-kitchen-only-610x1356", 610, 1356, false);
            ((IList)mode.GetType().GetField("placedStations", I).GetValue(mode)).Clear();
            Call(presentation, "ClearPilgrimStations"); // Sync only adds station views; the base capture needs them gone.
            Call(presentation, "Sync", mode);
            yield return CaptureAtResolution("larenga-base-610x1356", 610, 1356, false);
        }

        [UnityTest]
        [Explicit("Records the first 75 s of a La Renga run to Logs/Acceptance/RengaCShape-20261118/frames")]
        public IEnumerator LaRengaGameplayVideoFrames()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RengaCShape-20261118/frames"));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            yield return CaptureAtResolution("larenga-video-setup-610x1356", 610, 1356, false);
            Time.captureDeltaTime = 1f / 20f;
            object presentation = Get(view, "towerDefensePresentation");
            Type roleKind = T("StreetWorkerRole");
            var hires = new System.Collections.Generic.Dictionary<int, int> { { 5, 0 }, { 25, 1 }, { 45, 0 }, { 65, 1 }, { 85, 0 }, { 105, 1 } };
            int written = 0;
            try
            {
                for (int frame = 0; frame < 1500 && Get(mode, "Phase").ToString() == "Playing"; frame++)
                {
                    if (hires.TryGetValue(frame, out int role)) Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, role));
                    Call(mode, "Step", .05f);
                    Call(presentation, "Sync", mode);
                    yield return new WaitForEndOfFrame();
                    Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(directory, "f" + written.ToString("00000") + ".jpg"), image.EncodeToJPG(88));
                    UnityEngine.Object.Destroy(image);
                    written++;
                }
            }
            finally { Time.captureDeltaTime = 0f; }
            Assert.Greater(written, 100);
        }

        [UnityTest]
        [Explicit("Captures La Renga's C-shaped layout to Logs/Acceptance/RengaCShape-20261118")]
        public IEnumerator LaRengaCShapedLayoutCaptures()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RengaCShape-20261118"));
            Directory.CreateDirectory(directory);
            yield return CaptureAtResolution("larenga-c-setup-610x1356", 610, 1356, false);
            object presentation = Get(view, "towerDefensePresentation");
            Type roleKind = T("StreetWorkerRole");
            Time.captureDeltaTime = 1f / 20f;
            int shot = 0;
            try
            {
                for (int frame = 0; frame < 1200 && Get(mode, "Phase").ToString() == "Playing"; frame++)
                {
                    if (frame >= 1 && frame <= 6) Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, (frame + 1) % 2));
                    Call(mode, "Step", .05f);
                    Call(presentation, "Sync", mode);
                    if (frame == 30 || frame == 80 || frame == 130 || frame == 200)
                    {
                        yield return new WaitForEndOfFrame();
                        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(Path.Combine(directory, "larenga-c-play-" + shot++ + ".png"), image.EncodeToPNG());
                        UnityEngine.Object.Destroy(image);
                    }
                }
            }
            finally { Time.captureDeltaTime = 0f; }
            Debug.Log("LARENGA C captures=" + shot + " phase=" + Get(mode, "Phase") + " served=" + Get(mode, "ServedFans") + " delivered=" + Get(mode, "TotalDelivered"));
        }

        // Balance sweep for the horizontal kitchen levels: fan speed x crew size, each over a whole run (no rendering).
        static readonly object[] BalanceCases = BuildBalanceCases();
        static object[] BuildBalanceCases()
        {
            var cases = new System.Collections.Generic.List<object>();
            foreach (float speed in new[] { .054f, .075f, .1f, .13f })
                foreach (int cap in new[] { 2, 3, 4, 6, 9 }) cases.Add(new object[] { speed, cap });
            return cases.ToArray();
        }

        [UnityTest]
        [Explicit("Appends to Logs/Acceptance/RedondosVideo-20261118/balance-sweep.txt")]
        public IEnumerator LosRedondosBalanceCase([ValueSource(nameof(BalanceSpeeds))] float speed, [ValueSource(nameof(BalanceSpacings))] float spacing, [ValueSource(nameof(BalanceCaps))] int cap)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RedondosVideo-20261118/balance-sweep.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Type roleKind = T("StreetWorkerRole");
            int[] order = { 0, 1, 2, 0, 1, 2, 0, 1, 2 };
            object cfg = mode.GetType().GetField("config", I).GetValue(mode); // the runtime clone, never the asset
            cfg.GetType().GetField("fanSpeed", I).SetValue(cfg, speed);
            cfg.GetType().GetField("minimumEntrySpacingLogical", I).SetValue(cfg, spacing);
            int next = 0, frames = 0;
            for (; frames < 20 * 60 * 25 && Get(mode, "Phase").ToString() == "Playing"; frames++)
            {
                if (frames % 20 == 0 && next < cap && (bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, order[next]))) next++;
                Call(mode, "Step", .05f);
                if (frames % 400 == 0) yield return null;
            }
            string line = "speed=" + speed + " spacing=" + spacing + " hires=" + cap + " -> won=" + Get(mode, "HasWon") + " minutes=" + (frames * .05f / 60f).ToString("0.0") +
                " served=" + Get(mode, "ServedFans") + "/" + Get(mode, "TotalFanCount") + " escaped=" + Get(mode, "EscapedFans") + " coins=" + Get(mode, "Coins");
            File.AppendAllText(path, line + "\n");
            Assert.Pass(line);
        }
        [UnityTest]
        [Explicit("Appends to Logs/Acceptance/RengaCShape-20261118/balance-sweep.txt")]
        public IEnumerator LaRengaBalanceCase([ValueSource(nameof(RengaSpeeds))] float speed, [ValueSource(nameof(RengaSpacings))] float spacing,
            [ValueSource(nameof(RengaHires))] int hires)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Acceptance/RengaCShape-20261118/balance-sweep.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Type roleKind = T("StreetWorkerRole");
            int[] order = { 0, 1, 0, 1, 0, 1 }; // the hire cards: Parrillero and Cocacolero, up to three of each
            object cfg = mode.GetType().GetField("config", I).GetValue(mode); // the runtime clone, never the asset
            cfg.GetType().GetField("fanSpeed", I).SetValue(cfg, speed);
            cfg.GetType().GetField("minimumEntrySpacingLogical", I).SetValue(cfg, spacing);
            if (RengaFans > 0) cfg.GetType().GetField("totalFanCount", I).SetValue(cfg, RengaFans);
            int next = 0, frames = 0;
            for (; frames < 20 * 60 * 25 && Get(mode, "Phase").ToString() == "Playing"; frames++)
            {
                if (frames % 20 == 0 && next < hires && (bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, order[next]))) next++;
                Call(mode, "Step", .05f);
                if (frames % 400 == 0) yield return null;
            }
            string line = "fans=" + RengaFans + " speed=" + speed + " spacing=" + spacing + " hires=" + hires + " -> won=" + Get(mode, "HasWon") + " minutes=" + (frames * .05f / 60f).ToString("0.0") +
                " served=" + Get(mode, "ServedFans") + "/" + Get(mode, "TotalFanCount") + " escaped=" + Get(mode, "EscapedFans") + " coins=" + Get(mode, "Coins");
            File.AppendAllText(path, line + "\n");
            Assert.Pass(line);
        }
        static int RengaFans = 120;
        static readonly float[] RengaSpeeds = { .375f, .39f, .41f };
        static readonly float[] RengaSpacings = { 58f };
        static readonly int[] RengaHires = { 2, 3, 4, 5, 6 };

        static readonly float[] BalanceSpeeds = { .1575f };
        static readonly float[] BalanceSpacings = { 58f };
        static readonly int[] BalanceCaps = { 2, 3, 4, 5, 6, 9 };

        [UnityTest]
        public IEnumerator PeregrinacionNaturalWaveIsServedAtPhoneProportion()
        {
            yield return CaptureAtResolution("peregrinacion-natural-phone-setup", 610, 1356, false);
            Type roleKind = T("StreetWorkerRole");
            Assert.IsTrue((bool)Call(game, "TryHireTowerDefenseAtPost", mode, Enum.ToObject(roleKind, 1)));
            object mozo = ((IEnumerable)Get(mode, "Workers")).Cast<object>().First(w => Get(w, "Role").ToString() == "Mozo");
            string diagnostic = "";
            for (int tick = 0; tick < 1600 && (int)Get(mode, "TotalDelivered") < 2 && Get(mode, "Phase").ToString() == "Playing"; tick++)
            {
                Call(mode, "Step", .05f);
                if (tick % 100 == 0) diagnostic = DescribeLujanService(mode, mozo);
            // Every spot a worker can stand on must be reachable from the grill post: a pocket cut in two (for instance by the stacked
            // barrels leaving no room to walk over them) left the cooks unable to leave their strip and retrying every frame.
            foreach (int role in new[] { 0, 1 })
                Assert.AreEqual(0, (int)Call(mode, "UnreachableWorkerNodes", Enum.ToObject(roleKind, role)), "role " + role + " reaches the whole pocket");
                if (tick % 40 == 0) yield return null;
            }
            Assert.GreaterOrEqual((int)Get(mode, "TotalDelivered"), 2, "Natural pilgrims must be served. " + diagnostic +
                " phase=" + Get(mode, "Phase") + " lost=" + Get(mode, "HasLost") + " elapsed=" + Get(mode, "Elapsed"));
        }

        [UnityTest]
        public IEnumerator PeregrinacionHireCardsHireAtTheRolePostWithOneTap()
        {
            float pixel = (float)Get(mode, "worldUnitsPerLogicalPixel");
            IList workers = (IList)Get(mode, "Workers");
            Assert.AreEqual(2, workers.Count);
            int coins = Coins;
            int cocaCost = (int)Call(mode, "HireCost", Role("Cocacolero"));
            Vector2 barrelAccess = (Vector2)Get(((IEnumerable)Get(mode, "PlacedStations")).Cast<object>()
                .First(st => Get(st, "Kind").ToString() == "CocaBarrel"), "WorkPosition");

            yield return Tap(((Rect)Call(view, "PilgrimCardBounds", 3)).center); // Cocacolero: a single tap hires.
            Assert.AreEqual(3, workers.Count, "One tap on the role card hires without choosing a spot.");
            Assert.AreEqual(coins - cocaCost, Coins);
            object coca = workers[2];
            Assert.AreEqual("Cocacolero", Get(coca, "Role").ToString());
            Assert.LessOrEqual(Vector2.Distance((Vector2)Get(coca, "Position"), barrelAccess), .5f * pixel,
                "The Cocacolero appears at the barrels' post.");
            Assert.IsFalse((bool)Get(coca, "IsMoving"), "It starts at its post instead of walking in from a tapped spot.");

            yield return Tap(((Rect)Call(view, "PilgrimCardBounds", 1)).center); // second Parrillero
            Assert.AreEqual(4, workers.Count);
            Vector2 first = (Vector2)Get(workers[0], "Position"), second = (Vector2)Get(workers[3], "Position");
            Assert.AreEqual("Parrillero", Get(workers[3], "Role").ToString());
            Assert.GreaterOrEqual(Vector2.Distance(first, second), 55f * pixel,
                "The second Parrillero takes its own post (lane or a row behind) instead of stacking on the first.");
            Assert.GreaterOrEqual(second.y, first.y - .5f * pixel, "Extra posts go toward the fence, never into the furniture.");

            // Both cooks work from their own posts: neither stays idle behind the other.
            Set(mode, "nextFanSpawnAt", float.MaxValue);
            var cooked = new System.Collections.Generic.HashSet<int>();
            for (int tick = 0; tick < 400 && cooked.Count < 2; tick++)
            {
                Call(mode, "Step", .05f);
                foreach (object w in workers)
                    if (Get(w, "Role").ToString() == "Parrillero" && (bool)Get(w, "IsCooking")) cooked.Add((int)Get(w, "Id"));
            }
            Assert.AreEqual(2, cooked.Count, "Both Parrilleros cook at their posts.");
        }

        static string DescribeLujanService(object mode, object mozo)
        {
            object kitchen = Get(mode, "Kitchen");
            var sb = new System.Text.StringBuilder();
            sb.Append("t=").Append(Get(mode, "Elapsed")).Append(" table0=").Append(Call(kitchen, "AvailableTableCount", 0, 0));
            sb.Append(" mozo=").Append(Get(mozo, "Position")).Append(" stage=").Append(Get(mozo, "ServiceStage"))
                .Append(" moving=").Append(Get(mozo, "IsMoving")).Append(" cd=").Append(Get(mozo, "DeliveryCooldownSeconds"));
            foreach (object fan in (IEnumerable)Get(mode, "Fans"))
            {
                object follower = Get(fan, "RouteFollower");
                sb.Append(" | fan").Append(Get(fan, "Id")).Append("@").Append(Get(follower, "Position"))
                    .Append(" prog=").Append(Get(follower, "Progress")).Append("/").Append(Get(follower, "TotalDistance"))
                    .Append(" res=").Append(Get(fan, "ReservedByWorker"));
                MethodInfo find = mode.GetType().GetMethod("FindWorkerForFreeService", I);
                object role = Get(mozo, "Role");
                sb.Append(" candidate=").Append(find.Invoke(mode, new object[] { role, fan, 0 }) != null);
                MethodInfo anchor = mode.GetType().GetMethod("GetFanServiceAnchor", I, null, new[] { fan.GetType() }, null);
                sb.Append(" anchor=").Append(anchor.Invoke(mode, new[] { fan }));
            }
            sb.Append(" workBounds=").Append(Get(mode, "WorkBounds"));
            return sb.ToString();
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
        public IEnumerator PeregrinacionBocaWorkerGridWithoutCategoryTabsCaptureAtPortrait()
        {
            Assert.AreEqual(2, ((IList)Get(mode, "Workers")).Count);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 1 }, (int[])Get(mode, "WorkerCountsByRole"));
            Assert.AreEqual("Parrillero", Get(((IList)Get(mode, "Workers"))[0], "Role").ToString());
            Assert.AreEqual("Mozo", Get(((IList)Get(mode, "Workers"))[1], "Role").ToString());
            Assert.AreEqual(6, Get(view, "PilgrimItemCount"), "Speed plus five worker roles fit Boca's 3x2 block.");
            Rect first = (Rect)Call(view, "PilgrimCardBounds", 0);
            Rect third = (Rect)Call(view, "PilgrimCardBounds", 2);
            Rect fourth = (Rect)Call(view, "PilgrimCardBounds", 3);
            Rect sixth = (Rect)Call(view, "PilgrimCardBounds", 5);
            Assert.AreEqual(100f, first.height, "Luján's cards are Boca's 156x100 cards.");
            Assert.AreEqual(156f, first.width);
            Assert.That(third.y, Is.EqualTo(first.y).Within(.01f));
            Assert.That(fourth.y, Is.GreaterThan(first.y));
            Assert.That(sixth.x, Is.GreaterThan(fourth.x));
            IList stations=(IList)Get(mode,"PlacedStations");Assert.AreEqual(7,stations.Count);
            CollectionAssert.AreEqual(new[]{"NormalGrill","PremiumGrill","CocaBarrel","FernetTable","BeerBarrel","NormalTable","PremiumTable"},
                Enumerable.Range(0,stations.Count).Select(i=>Get(stations[i],"Kind").ToString()).ToArray());
            object presentation=Get(view,"towerDefensePresentation");Call(presentation,"Sync",mode);
            GameObject presentationRoot=(GameObject)Get(presentation,"presentationRoot");
            Assert.IsNotNull(presentationRoot,"The Tower Defense world presentation must own its scene roots.");
            int visibleFurniture=0;
            foreach(Transform child in presentationRoot.GetComponentsInChildren<Transform>(true))
                if(child.name.StartsWith("Peregrinación placed ",StringComparison.Ordinal))visibleFurniture++;
            Assert.AreEqual(7,visibleFurniture,"These must be actual rendered furniture roots, not only model entries or menu cards.");
            foreach (object station in stations)
            {
                string kind = Get(station, "Kind").ToString();
                if (kind != "NormalTable" && kind != "PremiumTable") continue;
                int id = (int)Get(station, "Id");
                Transform rootForTable = presentationRoot.transform.Find("Peregrinación placed " + kind + " " + id);
                Assert.IsNotNull(rootForTable);
                Transform furniture = rootForTable.Find(kind == "NormalTable" ? "Station_NormalTable" : "Station_PremiumTable");
                Assert.IsNotNull(furniture, kind + " must use Boca's same board-and-trestle sandwich-table prefab.");
                Assert.IsNotNull(furniture.Find("Tabletop"));
                Assert.IsNotNull(furniture.Find("Trestle_Left"));
                Assert.IsNotNull(furniture.Find("Trestle_Right"));
                Assert.IsNull(rootForTable.Find("Station_FernetTable"), "Do not substitute the Fernet counter for a sandwich table.");
            }

            object fernetStationModel = stations.Cast<object>().First(station => Get(station, "Kind").ToString() == "FernetTable");
            Transform fernetStationRoot = presentationRoot.transform.Find("Peregrinación placed FernetTable " + Get(fernetStationModel, "Id"));
            Assert.IsNotNull(fernetStationRoot);
            Transform fernetStation = fernetStationRoot.Find("Station_FernetTable");
            Assert.IsNotNull(fernetStation);
            Transform fernetBottleDisplay = fernetStation.Find("FernetBottleDisplay");
            Assert.IsNotNull(fernetBottleDisplay, "The Fernet table must copy Boca's two display rows of Coca/Fernet bottles.");
            Assert.AreEqual(8, fernetBottleDisplay.GetComponentsInChildren<SpriteRenderer>(true)
                .Count(renderer => renderer.name.StartsWith("CocaColaBottle_Row", StringComparison.Ordinal)
                    || renderer.name.StartsWith("FernetBrancaBottle_Row", StringComparison.Ordinal)));
            Assert.IsNull(presentationRoot.transform.Find("Station product label"),
                "Do not add oversized floating drink labels that Boca does not use.");

            int[] tableUnits = new int[7], grillUnits = new int[7];
            object kitchen = Get(mode, "Kitchen");
            foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
            {
                int product = (int)Get(unit, "Product");
                string location = Get(unit, "Location").ToString();
                if (location == "Table") tableUnits[product]++;
                if (location == "Grill") grillUnits[product]++;
            }
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 12, 45, 12 }, tableUnits,
                "The Boca opening stocks Fernet/Coca/beer, with all sandwich tables empty.");
            CollectionAssert.AreEqual(new int[7], grillUnits,
                "Boca's full grill art is visual-only and must not seed extra inventory.");
            int ambientGrillMeat = 0;
            foreach (Transform child in presentationRoot.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Peregrinación Boca ambient grill P", StringComparison.Ordinal)) ambientGrillMeat++;
            int expectedAmbientGrillMeat = 0;
            for (int product = 0; product < 4; product++) expectedAmbientGrillMeat += (int)Call(kitchen, "GrillCapacity", product);
            Assert.AreEqual(expectedAmbientGrillMeat, ambientGrillMeat,
                "Every free Boca grill slot has its render-only meat displayed at start.");
            int fernetUnitId = -1;
            int cocaUnitId = -1;
            int beerUnitId = -1;
            foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
            {
                if (Get(unit, "Location").ToString() != "Table") continue;
                int product = (int)Get(unit, "Product");
                if (product == 5 && fernetUnitId < 0) fernetUnitId = (int)Get(unit, "Id");
                else if (product == 4 && cocaUnitId < 0) cocaUnitId = (int)Get(unit, "Id");
                else if (product == 6 && beerUnitId < 0) beerUnitId = (int)Get(unit, "Id");
            }
            foreach (var expectedVisual in new[]
                {
                    new { UnitId = fernetUnitId, Sprite = "fernet-cup-800cc-v1" },
                    new { UnitId = cocaUnitId, Sprite = "coca" },
                    new { UnitId = beerUnitId, Sprite = "beer" }
                })
            {
                Assert.Greater(expectedVisual.UnitId, 0);
                Transform item = presentationRoot.transform.Find("Physical kitchen unit " + expectedVisual.UnitId);
                Assert.IsNotNull(item);
                Assert.AreEqual(expectedVisual.Sprite, item.GetComponent<SpriteRenderer>().sprite.name,
                    "Physical Luján stock must use the same cup/bottle sprite as Boca.");
            }
            SpriteRenderer fernetTableRenderer = fernetStation.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(fernetTableRenderer);
            int fernetBottleDisplayFrontOrder = fernetBottleDisplay.GetComponentsInChildren<SpriteRenderer>(true)
                .Max(renderer => renderer.sortingOrder);
            int renderedFernetCups = 0;
            foreach (object unit in (IEnumerable)Get(kitchen, "Units"))
            {
                if ((int)Get(unit, "Product") != 5 || Get(unit, "Location").ToString() != "Table") continue;
                int id = (int)Get(unit, "Id");
                int slot = (int)Get(unit, "Slot");
                Transform cup = presentationRoot.transform.Find("Physical kitchen unit " + id);
                Assert.IsNotNull(cup);
                SpriteRenderer cupRenderer = cup.GetComponent<SpriteRenderer>();
                Bounds tableVisualBounds = fernetTableRenderer.bounds;
                Bounds cupVisualBounds = cupRenderer.bounds;
                Assert.IsTrue(tableVisualBounds.Intersects(cupVisualBounds),
                    "Every Boca-positioned Fernet cup must overlap the tabletop artwork. Cup " + id
                    + " bounds=" + cupVisualBounds + " table=" + tableVisualBounds + ".");
                Assert.GreaterOrEqual(cupVisualBounds.min.x, tableVisualBounds.min.x - .01f,
                    "A Fernet cup must not spill off the table's left edge.");
                Assert.LessOrEqual(cupVisualBounds.max.x, tableVisualBounds.max.x + .01f,
                    "A Fernet cup must not spill off the table's right edge.");
                Assert.That(cupVisualBounds.min.y,
                    Is.InRange(tableVisualBounds.min.y - .01f, tableVisualBounds.max.y + .01f),
                    "The cup base must rest within the tabletop artwork, not float above or below it.");
                Assert.AreEqual(fernetTableRenderer.sortingOrder + 10 + slot, cupRenderer.sortingOrder,
                    "Fernet cups must use Boca's foreground-on-station sorting band.");
                Assert.Greater(cupRenderer.sortingOrder, fernetBottleDisplayFrontOrder,
                    "Prepared cups must render over the Fernet counter and its bottle display.");
                renderedFernetCups++;
            }
            Assert.AreEqual(45, renderedFernetCups, "All initial Fernet servings must be visible on the Fernet table.");
            Assert.IsFalse((bool)Get(mode, "EquipmentPlacementAllowed"));
            Assert.IsNull(vt.GetField("pilgrimEquipmentTab", S),
                "Luján must not keep a hidden equipment tab or alternate screen.");
            Assert.IsNull(vt.GetMethod("PilgrimTabBounds", S),
                "The Luján footer must not expose category tabs.");
            // Luján's footer is Boca's: it starts at Boca's footer top and its caption badge sits where Boca's does.
            float canvasHeight = (float)Get(view, "logicalCanvasHeight");
            float top = (float)CallStatic(T("TowerDefenseResponsiveLayout"), "PilgrimFooterTop", canvasHeight, 14);
            object pilgrimFooter = view.GetType().GetMethod("PilgrimFooter", I).Invoke(view, new object[] { canvasHeight });
            Rect badgeLayout = (Rect)pilgrimFooter.GetType().GetField("LevelBadge").GetValue(pilgrimFooter);
            float badgeY = (badgeLayout.y + badgeLayout.height * .5f) * (canvasHeight / 960f);
            Assert.AreSame(Get(view, "menuFont"), ((GUIStyle)Get(view, "towerDefenseTitle")).font,
                "Luján's title must use the same Luckiest Guy font as the standard level badge.");
            Assert.AreEqual(0, Call(view, "HitPilgrimAction", new Vector2(460f, badgeY)),
                "The removed pause button must not leave an invisible touch target.");
            Assert.AreEqual(0, Call(view, "HitPilgrimAction", new Vector2(508f, badgeY)),
                "The removed back button must not leave an invisible touch target.");
            foreach (Vector2 oldTabCenter in new[] { new Vector2(98f, badgeY), new Vector2(272f, badgeY), new Vector2(445f, badgeY) })
                Assert.AreEqual(0, Call(view, "HitPilgrimAction", oldTabCenter),
                    "Former Trabajadores/Equipamiento/Estaciones tab positions must no longer activate a control.");
            yield return CaptureAtResolution("peregrinacion-boca-exact-opening-state-540x960", 540, 960);

            int openingCoins = (int)Get(mode, "Coins");
            yield return Tap(((Rect)Call(view, "PilgrimCardBounds", 0)).center);
            Assert.AreEqual(1, Get(mode, "SpeedLevel"), "The Boca-style speed card is an active upgrade control.");
            Assert.AreEqual(openingCoins - 70, Get(mode, "Coins"), "The first Boca-priced speed upgrade costs70.");
        }

        [UnityTest]
        [Ignore("Pendiente de rediseno: test PlayMode desfasado respecto del flujo/layout actual de StreetView (hub, selector por posicion de campana, tutoriales, mercado). Fallo previo a la limpieza; StreetView.cs no fue modificado por las fases 1-2b. Ver Docs/tests-playmode-pendientes.md.")]
        public IEnumerator PeregrinacionGameViewSingleFenceFreeConstructionAtThreeResolutions()
        {
            BuildPilgrimStationsAndInitialCrew();
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
        public IEnumerator PeregrinacionStationsStartInstalledAndCannotBuyOrPlace()
        {
            IList initial=(IList)Get(mode,"PlacedStations");Assert.AreEqual(7,initial.Count);
            int coins=(int)Get(mode,"Coins");long equipmentSpend=(long)Get(mode,"EquipmentSpendCents");
            Assert.IsFalse((bool)Get(mode,"EquipmentPlacementAllowed"),"There is no equipment-placement flow on Luján.");
            Vector2 work=((Rect)Get(mode,"WorkBounds")).center;
            Vector2 logical=((Rect)Call(view,"PilgrimWorldToLogical",new Rect(work,Vector2.zero))).position;
            yield return Tap(logical);
            Assert.AreEqual(7,((IList)Get(mode,"PlacedStations")).Count);
            Assert.AreEqual(coins,Get(mode,"Coins"));
            Assert.AreEqual(equipmentSpend,Get(mode,"EquipmentSpendCents"));
            Assert.AreEqual(-1,Call(mode,"StationCost",Enum.ToObject(T("TowerDefenseStationKind"),0)));
        }

        [UnityTest]
        public IEnumerator PeregrinacionOpeningStationsArePresentAndPauseStopsSimulation()
        {
            Assert.AreEqual(7,((IList)Get(mode,"PlacedStations")).Count);
            Call(mode,"Pause");float elapsed=(float)Get(mode,"Elapsed");Call(mode,"Step",20f);
            Assert.AreEqual(elapsed,Get(mode,"Elapsed"));Call(mode,"Resume");
            Assert.AreEqual("Playing",Get(mode,"Phase").ToString());
            yield return null;
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
            // ID10 now has distinct service windows: staff both Coca bays instead of
            // assuming the old upper shared Coca area covers the whole route.
            int[][] opening = campaignLevelId == 10
                ? new[] {new[] {0, 0}, new[] {1, 0}, new[] {0, 1}, new[] {1, 1}}
                : new[] {new[] {0, 0}, new[] {1, 0}, new[] {0, 1}, new[] {0, 1}};
            foreach (int[] placement in opening)
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator ThreeParrillerosAndThreeCocacolerosFitInsideTheFirstAreaInTallPortrait()
        {
            yield return PlaceAndCaptureThreePlusThreeWorkers(0);
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
        public IEnumerator ThreeParrillerosAndThreeCocacolerosFitInsideTheSecondAreaInTallPortrait()
        {
            yield return PlaceAndCaptureThreePlusThreeWorkers(1);
        }

        [UnityTest]
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
        [Ignore("Camioneros (ID9) is now a horizontal Nueva Chicago kitchen level; this test covered its former two-area serpentine design.")]
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
