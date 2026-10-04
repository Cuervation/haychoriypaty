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
    public sealed class FlorestaPointerTests
    {
        private GameObject root;
        private Component game;
        private Type type;
        private Mouse mouse;
        private float previousTimeScale;
        private bool previousBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousBackground = Application.runInBackground;
            Time.timeScale = 1f; Application.runInBackground = true;
            mouse = InputSystem.AddDevice<Mouse>();
            root = new GameObject("Floresta pointer test (temporary)");
            type = Type.GetType("HayChoriYPaty.GameController, Assembly-CSharp", true);
            game = root.AddComponent(type);
            object balance = Get("Balance");
            balance.GetType().GetField("startingCoins").SetValue(balance, 100);
            ((Behaviour)game).enabled = false; ((Behaviour)game).enabled = true;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(root);
            InputSystem.RemoveDevice(mouse);
            Time.timeScale = previousTimeScale; Application.runInBackground = previousBackground;
            yield return null;
        }

        private object Get(string name) { return type.GetProperty(name).GetValue(game, null); }
        private string Phase { get { return Get("Phase").ToString(); } }
        private Vector2 Pixel(float x, float y)
        {
            Type view = Type.GetType("HayChoriYPaty.PrototypeView, Assembly-CSharp", true);
            Rect viewport = (Rect)view.GetMethod("CanvasViewport", BindingFlags.NonPublic | BindingFlags.Static)
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

        [UnityTest]
        public IEnumerator MouseButtonsStartHireUpgradeAndReplayWithInputSystemOnly()
        {
            Assert.AreEqual("Ready", Phase);
            yield return Click(270, 597);
            Assert.AreEqual("Playing", Phase, "Real queued mouse input must activate Start.");
            yield return Click(140, 890);
            Assert.AreEqual(2, Get("StaffCount")); Assert.AreEqual(70, Get("Coins"));
            yield return Click(395, 890);
            Assert.AreEqual(1, Get("SpeedUpgrades")); Assert.AreEqual(46, Get("Coins"));
            object balance = Get("Balance");
            balance.GetType().GetField("roundDurationSeconds").SetValue(balance, 0.05f);
            yield return new WaitForSeconds(0.15f); Assert.AreEqual("Lost", Phase);
            balance.GetType().GetField("roundDurationSeconds").SetValue(balance, 150f);
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase); Assert.AreEqual(1, Get("StaffCount"));
            Assert.AreEqual(100, Get("Coins"));
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
        public IEnumerator PrimaryTouchStartsHiresAndUpgrades()
        {
            Touchscreen screen = InputSystem.AddDevice<Touchscreen>();
            try
            {
                yield return Tap(screen, 270, 597, 1); Assert.AreEqual("Playing", Phase);
                yield return Tap(screen, 140, 890, 2);
                Assert.AreEqual(2, Get("StaffCount")); Assert.AreEqual(70, Get("Coins"));
                yield return Tap(screen, 395, 890, 3);
                Assert.AreEqual(1, Get("SpeedUpgrades")); Assert.AreEqual(46, Get("Coins"));
            }
            finally { InputSystem.RemoveDevice(screen); }
        }

        [UnityTest]
        public IEnumerator NativeGuiNotificationDoesNotRepeatInputSystemPurchase()
        {
            yield return Click(270, 597);
            Type viewType = Type.GetType("HayChoriYPaty.PrototypeView, Assembly-CSharp", true);
            Component view = root.GetComponent(viewType);
            MethodInfo notify = viewType.GetMethod("HandleGuiAction", BindingFlags.NonPublic | BindingFlags.Instance);
            notify.Invoke(view, new object[] { 3 });
            Assert.AreEqual(0, Get("SpeedUpgrades")); Assert.AreEqual(100, Get("Coins"));
            yield return Click(395, 890);
            notify.Invoke(view, new object[] { 3 });
            Assert.AreEqual(1, Get("SpeedUpgrades")); Assert.AreEqual(76, Get("Coins"));
        }

        [UnityTest]
        public IEnumerator OutsideClickAndDragIntoStartDoNotActivateIt()
        {
            yield return Click(10, 290); Assert.AreEqual("Ready", Phase);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = Pixel(10, 290) }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = Pixel(270, 597) });
            yield return null; yield return null;
            Assert.AreEqual("Ready", Phase);
        }
    }
}
