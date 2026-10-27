using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    // Drive the real StreetView pointer path, including its action-22 Mozo card.
    public sealed class StreetActiveCooksPointerTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private GameObject root;
        private Component game, view;
        private Type gameType, viewType, roleType;
        private Mouse mouse;
        private bool hadSavedProgress;
        private string previousSavedProgress;
        private float previousTimeScale;
        private bool previousBackground;
        private InputSettings.BackgroundBehavior previousInputBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousBackground = Application.runInBackground;
            previousInputBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Time.timeScale = 0f;
            Application.runInBackground = true;
            gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            roleType = Type.GetType("HayChoriYPaty.StreetWorkerRole, Assembly-CSharp", true);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            hadSavedProgress = PlayerPrefs.HasKey(key);
            previousSavedProgress = hadSavedProgress ? PlayerPrefs.GetString(key) : null;
            SeedProgress();
            mouse = InputSystem.AddDevice<Mouse>();
            root = new GameObject("Active cooks pointer test (temporary)");
            game = root.AddComponent(gameType);
            view = root.AddComponent(viewType);
            viewType.GetField("introActive", PrivateInstance).SetValue(view, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = previousInputBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            yield return null;
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            if (hadSavedProgress) PlayerPrefs.SetString(key, previousSavedProgress);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Time.timeScale = previousTimeScale;
            Application.runInBackground = previousBackground;
            yield return null;
        }

        private void SeedProgress()
        {
            Type dataType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object data = Activator.CreateInstance(dataType);
            dataType.GetField("unlockedLevel").SetValue(data, 10);
            dataType.GetField("price").SetValue(data, 5f);
            dataType.GetField("coins").SetValue(data, 500);
            dataType.GetField("staff").SetValue(data, 1);
            dataType.GetField("speed").SetValue(data, 0);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private object Get(string name) => gameType.GetProperty(name, PublicInstance).GetValue(game, null);
        private object Sim => Get("Sim");
        private static object GetProperty(object target, string name) => target.GetType().GetProperty(name, PublicInstance).GetValue(target, null);
        private static MethodInfo Method(Type type, string name, int arity, BindingFlags flags)
        {
            foreach (MethodInfo candidate in type.GetMethods(flags))
                if (candidate.Name == name && candidate.GetParameters().Length == arity) return candidate;
            throw new MissingMethodException(type.FullName, name);
        }
        private object CallGame(string method, params object[] args) => Method(gameType, method, args.Length, PublicInstance).Invoke(game, args);
        private void SetSim(string property, object value) => Sim.GetType().GetProperty(property, PublicInstance).SetValue(Sim, value, null);
        private static object Call(object target, string method, params object[] args)
            => Method(target.GetType(), method, args.Length, PublicInstance).Invoke(target, args);
        private static object EnumValue(Type type, string name) => Enum.Parse(type, name);
        private void ActivateTestGameWithoutPersisting()
        {
            if (Sim != null) return;
            MethodInfo activate = gameType.GetMethod("ActivatePlayer", PublicInstance);
            Type callbackType = activate.GetParameters()[1].ParameterType;
            ParameterExpression[] parameters = Array.ConvertAll(callbackType.GetMethod("Invoke").GetParameters(),
                parameter => Expression.Parameter(parameter.ParameterType, parameter.Name));
            Delegate callback = Expression.Lambda(callbackType, Expression.Constant(true), parameters).Compile();
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            string savedJson = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            Assert.IsTrue((bool)activate.Invoke(game, new object[] { savedJson, callback }),
                "The temporary fixture must activate from its snapshot without writing PlayerPrefs.");
        }

        private void StockSelectedLevel()
        {
            object market = Get("Market");
            object data = GetProperty(market, "Data");
            FieldInfo balanceCents = data.GetType().GetField("balanceCents");
            balanceCents.SetValue(data, Math.Max((long)balanceCents.GetValue(data), 1000000000L));
            for (int id = 0; id < 10; id++)
                if ((int)Method(market.GetType(), "MissingPacks", 2, PublicInstance).Invoke(market, new[] { Sim, (object)id }) > 0)
                    Assert.IsTrue((bool)CallGame("BuyMissingIngredient", id));
        }

        private void StartLevel(int level)
        {
            Assert.IsTrue((bool)CallGame("SelectLevel", level));
            StockSelectedLevel();
            Assert.IsTrue((bool)CallGame("TryStartRound"));
            SetSim("Coins", 10000);
        }

        private Rect MozoCardBounds()
        {
            return (Rect)viewType.GetMethod("ResponsiveUpgradeCardBounds", PrivateInstance)
                .Invoke(view, new[] { Sim, (object)22 });
        }

        private Vector2 Pixel(float x, float y)
        {
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            float logicalHeight = (float)viewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            float verticalScale = logicalHeight / 960f;
            return new Vector2(viewport.x + x * scale, Screen.height - (viewport.y + y * verticalScale * scale));
        }

        private IEnumerator Click(Rect bounds)
        {
            Vector2 p = Pixel(bounds.center.x, bounds.center.y);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p });
            yield return null; yield return null;
        }

        private IEnumerator Tap(Touchscreen screen, Rect bounds, int id)
        {
            Vector2 p = Pixel(bounds.center.x, bounds.center.y);
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
            yield return null; yield return null;
        }

        private int CookingWorkerCount(out bool allMidProgress)
        {
            int count = 0;
            allMidProgress = true;
            foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
            {
                if (GetProperty(worker, "State").ToString() != "Cooking") continue;
                count++;
                float progress = Convert.ToSingle(GetProperty(worker, "CookingProgress"));
                allMidProgress &= progress >= .1f && progress <= .85f;
            }
            return count;
        }

        private IEnumerator WaitForCookingWorkers(int expected, int maxTicks, string label)
        {
            bool ready = false;
            for (int i = 0; i < maxTicks; i++)
            {
                Call(Sim, "Step", .05f);
                bool midProgress;
                if (CookingWorkerCount(out midProgress) == expected && midProgress)
                { ready = true; break; }
                yield return null;
            }
            Assert.IsTrue(ready, label + " must show " + expected + " distinct workers in the real Cooking state.");
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        private static void SetWorkerProperty(object worker, string name, object value)
        {
            PropertyInfo property = worker.GetType().GetProperty(name, PublicInstance);
            Assert.IsNotNull(property, "Missing worker property " + name);
            Assert.IsNotNull(property.GetSetMethod(true), "Missing worker setter " + name);
            property.GetSetMethod(true).Invoke(worker, new[] { value });
        }

        private static void SaveCookingGameViewCapture(string name)
        {
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.IsNotNull(capture, "Unity did not return a Game View capture for " + name + ".");
            string path = Path.Combine(Path.GetTempPath(), "hay-chori-" + name + ".png");
            File.WriteAllBytes(path, capture.EncodeToPNG());
            Debug.Log("Parrillero cooking Game View capture saved to " + path + " (" + capture.width + "x" + capture.height + ")");
            UnityEngine.Object.Destroy(capture);
        }

        private void AddReadyTableSandwich(object kitchen, int product, int slot)
        {
            Type state = Type.GetType("HayChoriYPaty.StreetFoodState, Assembly-CSharp", true);
            Type location = Type.GetType("HayChoriYPaty.StreetFoodLocation, Assembly-CSharp", true);
            MethodInfo add = Method(kitchen.GetType(), "Add", 4, PrivateInstance);
            object unit = add.Invoke(kitchen, new[] { (object)product, EnumValue(state, "Cooked"), EnumValue(location, "Table"), slot });
            Assert.IsNotNull(unit, "Visual fixture must seed real prepared units through the production inventory.");
        }

        private static void ClearFixtureFood(object kitchen)
        {
            ((System.Collections.IList)GetProperty(kitchen, "Units")).Clear();
        }

        private int ConfigureVisualCookersForRingCapture(int normalCount, int premiumCount, float cycleProgress)
        {
            object layout = GetProperty(Sim, "KitchenLayout");
            int normalIndex = 0, premiumIndex = 0, active = 0;
            foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
            {
                string role = GetProperty(worker, "Role").ToString();
                bool premium = role == "ParrilleroPremium";
                if (!premium && role != "Parrillero") continue;
                int index = premium ? premiumIndex++ : normalIndex++;
                int requested = premium ? premiumCount : normalCount;
                int teamCount = requested;
                bool cooking = index < requested;
                SetWorkerProperty(worker, "State", EnumValue(worker.GetType().Assembly.GetType("HayChoriYPaty.StreetWorkerState"), "Idle"));
                SetWorkerProperty(worker, "VisualCooking", cooking);
                SetWorkerProperty(worker, "VisualCookingProduct", cooking ? (premium ? 2 : 0) : -1);
                SetWorkerProperty(worker, "CookingProgress", cooking ? .57f : 0f);
                SetWorkerProperty(worker, "CookingCycleProgress", cycleProgress);
                if (!cooking) continue;

                int product = premium ? 2 : 0;
                Vector2 post = (Vector2)Call(layout, "GrillWorkPosition", product, index, teamCount);
                SetWorkerProperty(worker, "CookStationIndex", index);
                SetWorkerProperty(worker, "Position", post);
                SetWorkerProperty(worker, "Target", post);
                SetWorkerProperty(worker, "StepDistance", 0f);
                active++;
            }
            return active;
        }

        private void AssertCookingRingCentersDoNotOverlap(int expected)
        {
            IList centers = (IList)viewType.GetField("cookingRingCenters", PrivateInstance).GetValue(view);
            Assert.AreEqual(expected, centers.Count, "Each visible cooking worker must have one rendered progress ring.");
            for (int i = 0; i < centers.Count; i++)
            for (int j = i + 1; j < centers.Count; j++)
                Assert.GreaterOrEqual(((Vector2)centers[i] - (Vector2)centers[j]).sqrMagnitude,
                    28f * 28f - .01f, "Cooking rings must not overlap one another.");
        }

#if UNITY_EDITOR
        private IEnumerator CaptureSandwichGameViewSize(int width, int height, string name)
        {
            Assembly editorAssembly = Type.GetType("UnityEditor.EditorWindow, UnityEditor", true).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type windowType = editorAssembly.GetType("UnityEditor.EditorWindow");
            MethodInfo getWindow = windowType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "GetWindow" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));
            Assert.IsNotNull(getWindow);
            object window = getWindow.Invoke(null, new object[] { gameViewType });
            PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo selectedField = gameViewType.GetField("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int previous = selected != null ? Convert.ToInt32(selected.GetValue(window, null)) : Convert.ToInt32(selectedField.GetValue(window));
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            object registry = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(null, null);
            object groupKind = sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(registry, null);
            object group = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(registry, new[] { groupKind });
            MethodInfo countMethod = group.GetType().GetMethod("GetTotalCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getSize = group.GetType().GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = Convert.ToInt32(countMethod.Invoke(group, null)), target = -1, addedIndex = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                int w = Convert.ToInt32(size.GetType().GetProperty("width").GetValue(size, null));
                int h = Convert.ToInt32(size.GetType().GetProperty("height").GetValue(size, null));
                PropertyInfo typeProperty = size.GetType().GetProperty("type");
                if (w == width && h == height && typeProperty != null && typeProperty.GetValue(size, null).ToString() == "FixedResolution") { target = i; break; }
            }
            if (target < 0)
            {
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                object kind = Enum.Parse(editorAssembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
                object newSize = Activator.CreateInstance(sizeType, new[] { kind, (object)width, height, "Temporary sandwich QA " + width + "x" + height });
                group.GetType().GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(group, new[] { newSize });
                target = Convert.ToInt32(countMethod.Invoke(group, null)) - 1;
                addedIndex = target;
            }
            try
            {
                if (selected != null) selected.SetValue(window, target, null); else selectedField.SetValue(window, target);
                yield return null; yield return null;
                Assert.AreEqual(width, Screen.width);
                Assert.AreEqual(height, Screen.height);
                yield return new WaitForEndOfFrame();
                Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
                Assert.IsNotNull(capture);
                string path = Path.Combine(Path.GetTempPath(), "hay-chori-sandwich-" + name + "-" + width + "x" + height + ".png");
                File.WriteAllBytes(path, capture.EncodeToPNG());
                Debug.Log("Sandwich Game View capture saved to " + path + " (" + capture.width + "x" + capture.height + ")");
                UnityEngine.Object.Destroy(capture);
            }
            finally
            {
                int restore = Mathf.Clamp(previous, 0, count - 1);
                if (selected != null) selected.SetValue(window, restore, null); else selectedField.SetValue(window, restore);
                if (addedIndex >= 0)
                    group.GetType().GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(group, new object[] { addedIndex });
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureSandwichTableRowsAndMozoCarryAtPortraitSizes()
        {
            ActivateTestGameWithoutPersisting();
            StartLevel(5);
            object kitchen = GetProperty(Sim, "Kitchen");
            ClearFixtureFood(kitchen);
            for (int product = 0; product < 4; product++) AddReadyTableSandwich(kitchen, product, 0);
            yield return CaptureSandwichGameViewSize(540, 960, "after-few-products");

            for (int product = 0; product < 4; product++)
                for (int slot = 1; slot < 4; slot++) AddReadyTableSandwich(kitchen, product, slot);
            yield return CaptureSandwichGameViewSize(540, 960, "after-two-rows-compact");
            yield return CaptureSandwichGameViewSize(540, 1200, "after-two-rows-tall");

            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "Mozo")));
            Assert.IsTrue((bool)Call(Sim, "SpawnCustomer", 0, 1));
            bool carrying = false;
            for (int tick = 0; tick < 160 && !carrying; tick++)
            {
                Call(Sim, "Step", .05f);
                foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
                    if (GetProperty(worker, "Role").ToString() == "Mozo" && Convert.ToInt32(GetProperty(worker, "CarriedItemId")) > 0)
                    { carrying = true; break; }
                if (!carrying) yield return null;
            }
            Assert.IsTrue(carrying, "The capture needs a real Mozo carrying a real sandwich unit.");
            yield return CaptureSandwichGameViewSize(540, 1200, "after-mozo-carrying");
        }
#endif

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator CaptureOneToFourRealCooksAllPhasesAndTransition()
        {
            ActivateTestGameWithoutPersisting();
            StartLevel(5);
            SetSim("Coins", 100000);
            for (int product = 0; product < 4; product++)
                Assert.IsTrue((bool)Call(Sim, "SpawnCustomer", product, 50), "Seed demand for grill product " + product + ".");

            yield return WaitForCookingWorkers(1, 160, "One-cook capture");
            object firstCook = null;
            foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
                if (GetProperty(worker, "State").ToString() == "Cooking") { firstCook = worker; break; }
            Assert.IsNotNull(firstCook, "One real cook must be available for phase captures.");
            int firstCookId = Convert.ToInt32(GetProperty(firstCook, "Id"));
            for (int frame = 0; frame < 4; frame++)
            {
                int step = (frame - (firstCookId & 3) + 4) & 3;
                SetWorkerProperty(firstCook, "CookingCycleProgress", (step + .1f) / 8f);
                yield return null;
                yield return new WaitForEndOfFrame();
                SaveCookingGameViewCapture("parrillero-one-cook-frame-" + frame);
            }

            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "Parrillero")));
            yield return WaitForCookingWorkers(2, 80, "Two-cook capture");
            SaveCookingGameViewCapture("parrillero-two-cooks");

            SetSim("Coins", 100000);
            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            yield return WaitForCookingWorkers(3, 80, "Normal and Premium three-cook capture");
            SaveCookingGameViewCapture("parrillero-three-cooks");
            Call(Sim, "Step", .25f);
            yield return null;
            yield return new WaitForEndOfFrame();
            SaveCookingGameViewCapture("parrillero-three-cooks-next-phase");

            SetSim("Coins", 100000);
            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            yield return WaitForCookingWorkers(4, 20, "Four-cook capture");
            SaveCookingGameViewCapture("parrillero-four-cooks");

            bool sawCarry = false;
            for (int i = 0; i < 60 && !sawCarry; i++)
            {
                Call(Sim, "Step", .05f);
                foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
                    sawCarry |= Convert.ToInt32(GetProperty(worker, "CarriedItemId")) > 0 &&
                                GetProperty(worker, "State").ToString() == "ToTable";
                if (!sawCarry) yield return null;
            }
            Assert.IsTrue(sawCarry, "A real cooking unit must transition to the worker's ToTable carry state.");
            yield return null;
            yield return new WaitForEndOfFrame();
            SaveCookingGameViewCapture("parrillero-cooking-to-carry");
        }

        [UnityTest]
        public IEnumerator CaptureCookingRingsForBothRolesAndFourCookTeamsAtResponsivePortraitSizes()
        {
            ActivateTestGameWithoutPersisting();
            StartLevel(5);
            SetSim("Coins", 1000000);
            for (int i = 1; i < 4; i++)
                Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "Parrillero")), "Four normal cooks are required for the capture set.");
            for (int i = 0; i < 4; i++)
                Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")), "Four Premium cooks are required for the capture set.");

            Assert.AreEqual(1, ConfigureVisualCookersForRingCapture(1, 0, .31f));
            yield return CaptureSandwichGameViewSize(540, 960, "cooking-ring-single-normal");
            AssertCookingRingCentersDoNotOverlap(1);

            Assert.AreEqual(1, ConfigureVisualCookersForRingCapture(0, 1, .31f));
            yield return CaptureSandwichGameViewSize(540, 960, "cooking-ring-single-premium");
            AssertCookingRingCentersDoNotOverlap(1);

            Assert.AreEqual(4, ConfigureVisualCookersForRingCapture(4, 0, .31f));
            yield return CaptureSandwichGameViewSize(540, 960, "cooking-ring-four-normal-reference");
            AssertCookingRingCentersDoNotOverlap(4);
            yield return CaptureSandwichGameViewSize(540, 1200, "cooking-ring-four-normal-tall");
            AssertCookingRingCentersDoNotOverlap(4);

            Assert.AreEqual(4, ConfigureVisualCookersForRingCapture(0, 4, .31f));
            yield return CaptureSandwichGameViewSize(540, 960, "cooking-ring-four-premium-reference");
            AssertCookingRingCentersDoNotOverlap(4);
            yield return CaptureSandwichGameViewSize(540, 1200, "cooking-ring-four-premium-tall");
            AssertCookingRingCentersDoNotOverlap(4);

            Assert.AreEqual(4, ConfigureVisualCookersForRingCapture(2, 2, .31f));
            yield return CaptureSandwichGameViewSize(540, 960, "cooking-ring-mixed-reference");
            AssertCookingRingCentersDoNotOverlap(4);
            yield return CaptureSandwichGameViewSize(540, 1200, "cooking-ring-mixed-tall");
            AssertCookingRingCentersDoNotOverlap(4);
        }

        [UnityTest]
        public IEnumerator CaptureAllFourAlternatingVisualRecipesWithoutStock()
        {
            ActivateTestGameWithoutPersisting();
            StartLevel(5);
            SetSim("Coins", 100000);
            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "Parrillero")));
            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));
            Assert.IsTrue((bool)Call(Sim, "TryHire", EnumValue(roleType, "ParrilleroPremium")));

            object market = Get("Market");
            object marketData = GetProperty(market, "Data");
            int[] stock = (int[])marketData.GetType().GetField("stockUnits").GetValue(marketData);
            Array.Clear(stock, 0, stock.Length);

            var capturedProducts = new System.Collections.Generic.HashSet<int>();
            for (int tick = 0; tick < 2400 && capturedProducts.Count < 4; tick++)
            {
                Call(Sim, "Step", .05f);
                yield return null;
                foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
                {
                    if (!(bool)GetProperty(worker, "VisualCooking")) continue;
                    int product = Convert.ToInt32(GetProperty(worker, "VisualCookingProduct"));
                    if (product < 0 || !capturedProducts.Add(product)) continue;
                    Assert.AreEqual(0, GetProperty(worker, "CookingItemId"), "A no-stock visual turn cannot reserve a real item.");
                    Assert.AreEqual(0, GetProperty(worker, "CarriedItemId"), "A no-stock visual turn cannot carry a fake item.");
                    yield return new WaitForEndOfFrame();
                    SaveCookingGameViewCapture("fixed-alternation-no-stock-product-" + product);
                }
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, capturedProducts,
                "Normal and Premium cooks must keep displaying all enabled recipes when their stock is empty.");
            CollectionAssert.AreEqual(new int[stock.Length], stock, "Visual cooking must not consume or create stock.");
            object kitchen = GetProperty(Sim, "Kitchen");
            foreach (object unit in (System.Collections.IEnumerable)GetProperty(kitchen, "Units"))
                Assert.GreaterOrEqual(Convert.ToInt32(GetProperty(unit, "Product")), 4,
                    "Visual cooking must not add transportable sandwiches to the real inventory.");

            bool capturedFourAtOnce = false;
            for (int tick = 0; tick < 400 && !capturedFourAtOnce; tick++)
            {
                Call(Sim, "Step", .05f);
                yield return null;
                int visualCooks = 0;
                foreach (object worker in (System.Collections.IEnumerable)GetProperty(Sim, "Workers"))
                    if ((bool)GetProperty(worker, "VisualCooking")) visualCooks++;
                if (visualCooks < 4) continue;
                yield return new WaitForEndOfFrame();
                SaveCookingGameViewCapture("fixed-alternation-no-stock-four-cooks");
                capturedFourAtOnce = true;
            }
            Assert.IsTrue(capturedFourAtOnce, "All four specialty cooks should visibly animate together without stock.");
        }
#endif

        [UnityTest]
        public IEnumerator RacingFirstPaidMozoCardReceivesOneMouseClickAndChargesOnce()
        {
            StartLevel(5);
            Rect card = MozoCardBounds();
            Assert.Greater(card.width, 0f);
            Assert.AreEqual(1, GetProperty(Sim, "MozoCount"), "Racing starts with one free Mozo.");
            int before = (int)GetProperty(Sim, "Coins");
            int cost = (int)GetProperty(Sim, "MozoHireCost");
            Assert.AreEqual(50, cost, "The first paid Mozo must remain at its $50 base price.");
            yield return Click(card);
            Assert.AreEqual(2, GetProperty(Sim, "MozoCount"));
            Assert.AreEqual(before - cost, GetProperty(Sim, "Coins"), "One pointer-up action charges exactly once.");
            Assert.AreEqual(0, GetProperty(Sim, "CocacoleroCount"));
            Assert.AreEqual(1, GetProperty(Sim, "ParrilleroPremiumCount"), "Preserve Racing's pre-existing free Premium worker.");
            Assert.AreEqual(0, GetProperty(Sim, "FerneteroCount"));
        }

        [UnityTest]
        public IEnumerator RiverMozoCardReceivesOneTouchAndChargesOnce()
        {
            StartLevel(7);
            Rect card = MozoCardBounds();
            Assert.Greater(card.width, 0f);
            int before = (int)GetProperty(Sim, "Coins");
            int cost = (int)GetProperty(Sim, "MozoHireCost");
            Touchscreen screen = InputSystem.AddDevice<Touchscreen>();
            try { yield return Tap(screen, card, 41); }
            finally { InputSystem.RemoveDevice(screen); }
            Assert.AreEqual(1, GetProperty(Sim, "MozoCount"));
            Assert.AreEqual(before - cost, GetProperty(Sim, "Coins"), "One touch-up action charges exactly once.");
            Assert.AreEqual(0, GetProperty(Sim, "CocacoleroCount"));
            Assert.AreEqual(0, GetProperty(Sim, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, GetProperty(Sim, "FerneteroCount"));
        }

        [UnityTest]
        public IEnumerator MozoCardAndHiringAreUnavailableBeforeRacing()
        {
            StartLevel(4);
            Assert.AreEqual(Rect.zero, MozoCardBounds());
            Assert.IsFalse((bool)CallGame("TryHireMozo"));
            Assert.AreEqual(0, GetProperty(Sim, "MozoCount"));
            Assert.AreEqual(0, GetProperty(Sim, "CocacoleroCount"));
            Assert.AreEqual(0, GetProperty(Sim, "ParrilleroPremiumCount"));
            yield return null;
        }
    }
}
