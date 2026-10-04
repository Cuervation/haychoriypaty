using System.Collections;
using System.Reflection;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    // Exercise the actual StreetView pointer path while keeping runtime in Assembly-CSharp.
    public sealed class StreetPointerTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private GameObject root;
        private Component game;
        private Type gameType;
        private Mouse mouse;
        private bool hadSavedProgress;
        private string previousSavedProgress;
        private float previousTimeScale;
        private bool previousBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousBackground = Application.runInBackground;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            hadSavedProgress = PlayerPrefs.HasKey(key);
            previousSavedProgress = hadSavedProgress ? PlayerPrefs.GetString(key) : null;
            SaveSeed();

            mouse = InputSystem.AddDevice<Mouse>();
            root = new GameObject("Street pointer test (temporary)");
            game = root.AddComponent(gameType);
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            root.AddComponent(viewType);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            if (hadSavedProgress) PlayerPrefs.SetString(key, previousSavedProgress);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Time.timeScale = previousTimeScale;
            Application.runInBackground = previousBackground;
            yield return null;
        }

        private void SaveSeed()
        {
            Type dataType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            object data = System.Activator.CreateInstance(dataType);
            dataType.GetField("unlockedLevel").SetValue(data, 1);
            dataType.GetField("price").SetValue(data, 5f);
            dataType.GetField("coins").SetValue(data, 40);
            dataType.GetField("staff").SetValue(data, 1);
            dataType.GetField("speed").SetValue(data, 0);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private object Get(string name) { return gameType.GetProperty(name, PublicInstance).GetValue(game, null); }
        private string Phase { get { return Get("Sim").GetType().GetProperty("Phase", PublicInstance).GetValue(Get("Sim"), null).ToString(); } }
        private object Sim { get { return Get("Sim"); } }
        private void Invoke(object target, string method, params object[] args) { target.GetType().GetMethod(method, PublicInstance).Invoke(target, args); }
        private void Tune(string field, object value) { Get("Balance").GetType().GetField(field, PublicInstance).SetValue(Get("Balance"), value); }

        private Vector2 Pixel(float x, float y)
        {
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            return new Vector2(viewport.x + x * scale, Screen.height - (viewport.y + y * scale));
        }

        private IEnumerator Click(float x, float y)
        {
            Vector2 p = Pixel(x, y);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p });
            yield return null; yield return null;
        }

        private IEnumerator DragPrice(float startX, float endX)
        {
            Vector2 from = Pixel(startX, 502), to = Pixel(endX, 502);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = to }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = to });
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator MouseDragSetsPriceAndStartBeginsRound()
        {
            Invoke(game, "SelectLevel", 1);
            Assert.AreEqual("Ready", Phase);
            yield return DragPrice(89, 451);
            Assert.AreEqual(60f, Get("Sim").GetType().GetProperty("Price", PublicInstance).GetValue(Sim, null));
            yield return DragPrice(451, 89);
            Assert.AreEqual(0f, Get("Sim").GetType().GetProperty("Price", PublicInstance).GetValue(Sim, null));
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase);
        }

        [UnityTest]
        public IEnumerator ProductTabsSelectIndependentPriceWithoutCoveringHud()
        {
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            ((Behaviour)game).enabled = false;
            PlayerPrefs.SetString(key, "{\"version\":1,\"unlockedLevel\":1,\"price\":5,\"coins\":40,\"staff\":1,\"speed\":0}");
            ((Behaviour)game).enabled = true;
            Invoke(game, "SelectLevel", 1);
            yield return null;
            yield return Click(154, 288);
            yield return DragPrice(89, 451);
            MethodInfo price = Sim.GetType().GetMethod("GetProductPrice", PublicInstance);
            Assert.AreEqual(60f, price.Invoke(Sim, new object[] { 1 }));
            Assert.AreEqual(5f, price.Invoke(Sim, new object[] { 0 }));
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            Rect tab = (Rect)viewType.GetMethod("ProductButton", StaticPrivate).Invoke(null, new object[] { 1 });
            Assert.IsFalse(tab.Overlaps(new Rect(24, 644, 498, 27)));
        }

        [UnityTest]
        public IEnumerator MouseCookAndSpeedPurchasesChargeExactlyOnce()
        {
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase);
            ((Behaviour)game).enabled = false; // The view still receives input; the round clock stays fixed.

            yield return Click(369, 769);
            Assert.AreEqual(2, Get("Sim").GetType().GetProperty("StaffCount", PublicInstance).GetValue(Sim, null));
            Assert.AreEqual(15, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));

            yield return Click(171, 769);
            Assert.AreEqual(1, Get("Sim").GetType().GetProperty("SpeedLevel", PublicInstance).GetValue(Sim, null));
            Assert.AreEqual(10, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));
        }

        private IEnumerator Tap(Touchscreen screen, float x, float y, int id)
        {
            Vector2 p = Pixel(x, y);
            InputSystem.QueueStateEvent(screen, new TouchState
                { touchId = id, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(screen, new TouchState
                { touchId = id, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator PrimaryTouchCanStartAndPurchaseThroughActualView()
        {
            Touchscreen screen = InputSystem.AddDevice<Touchscreen>();
            try
            {
                yield return Tap(screen, 270, 585, 1);
                Assert.AreEqual("Playing", Phase);
                ((Behaviour)game).enabled = false;
                yield return Tap(screen, 369, 769, 2);
                Assert.AreEqual(2, Get("Sim").GetType().GetProperty("StaffCount", PublicInstance).GetValue(Sim, null));
                Assert.AreEqual(15, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));
            }
            finally { InputSystem.RemoveDevice(screen); }
        }

        [UnityTest]
        public IEnumerator ReplayReturnsToReadyWithoutStartingAutomatically()
        {
            Tune("levelDurations", new[] { 0.05f, 210f, 240f, 270f, 300f });
            yield return Click(270, 585);
            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual("Lost", Phase);
            yield return Click(270, 564);
            Assert.AreEqual("Ready", Phase);
        }


        [UnityTest]
        public IEnumerator FlorestaIgnoresSliderDragButStartStillWorks()
        {
            Assert.IsFalse((bool)Sim.GetType().GetProperty("CanEditPrices").GetValue(Sim, null));
            yield return DragPrice(89, 451);
            Assert.AreEqual(5f, Sim.GetType().GetProperty("Price").GetValue(Sim, null));
            yield return DragPrice(451, 89);
            Assert.AreEqual(5f, Sim.GetType().GetProperty("Price").GetValue(Sim, null));
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase);
        }

        [UnityTest]
        public IEnumerator FlorestaLoadFixesSavedPriceWithoutResettingOtherProgress()
        {
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            ((Behaviour)game).enabled = false;
            PlayerPrefs.SetString(key, "{\"version\":1,\"unlockedLevel\":1,\"price\":60,\"prices\":[60,17,9,11,3,6,5],\"coins\":123,\"staff\":3,\"speed\":4}");
            ((Behaviour)game).enabled = true; yield return null;
            Assert.AreEqual(5f, Sim.GetType().GetProperty("Price").GetValue(Sim, null));
            Assert.AreEqual(123, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            Assert.AreEqual(3, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
            Assert.AreEqual(4, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
            Assert.That((float)Sim.GetType().GetProperty("WorkRate").GetValue(Sim, null), Is.EqualTo(1.4f).Within(.0001f));
            Assert.AreEqual(17f, Sim.GetType().GetMethod("GetProductPrice").Invoke(Sim, new object[] { 1 }));
            Invoke(game, "SelectLevel", 1);
            Assert.IsTrue((bool)Sim.GetType().GetProperty("CanEditPrices").GetValue(Sim, null));
        }


        private Rect Viewport(Vector2 size, Rect safe)
        {
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            return (Rect)viewType.GetMethod("CanvasViewport", StaticPrivate).Invoke(null, new object[] { size, safe });
        }

        [Test]
        public void StaleSimulatorSafeAreaFallsBackToCurrentScreenBounds()
        {
            Rect v=Viewport(new Vector2(1080,1920),new Rect(0,0,960,2566));Assert.AreEqual(new Rect(0,0,1080,1920),v);
        }

        [TestCase(1080, 1920, 0, 0, 1080, 1920)]
        [TestCase(1080, 2400, 0, 80, 1080, 2220)]
        [TestCase(2400, 1080, 80, 0, 2240, 1080)]
        public void SafeViewportKeepsPortraitDesignInsideSafeArea(float width, float height, float x, float y, float safeWidth, float safeHeight)
        {
            Rect viewport = Viewport(new Vector2(width, height), new Rect(x, y, safeWidth, safeHeight));
            Assert.That(viewport.width / viewport.height, Is.EqualTo(540f / 960f).Within(0.0001f));
            Assert.That(viewport.xMin, Is.GreaterThanOrEqualTo(x));
            Assert.That(viewport.xMax, Is.LessThanOrEqualTo(x + safeWidth));
        }
    }
}
