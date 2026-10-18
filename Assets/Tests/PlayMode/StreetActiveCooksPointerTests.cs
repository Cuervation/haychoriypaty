using System;
using System.Collections;
using System.IO;
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
#endif

        [UnityTest]
        public IEnumerator RacingMozoCardReceivesOneMouseClickAndChargesOnce()
        {
            StartLevel(5);
            Rect card = MozoCardBounds();
            Assert.Greater(card.width, 0f);
            int before = (int)GetProperty(Sim, "Coins");
            int cost = (int)GetProperty(Sim, "MozoHireCost");
            yield return Click(card);
            Assert.AreEqual(1, GetProperty(Sim, "MozoCount"));
            Assert.AreEqual(before - cost, GetProperty(Sim, "Coins"), "One pointer-up action charges exactly once.");
            Assert.AreEqual(0, GetProperty(Sim, "CocacoleroCount"));
            Assert.AreEqual(0, GetProperty(Sim, "ParrilleroPremiumCount"));
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
