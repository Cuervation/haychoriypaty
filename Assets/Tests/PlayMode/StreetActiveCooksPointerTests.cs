using System;
using System.Collections;
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
        private Type gameType, viewType;
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
