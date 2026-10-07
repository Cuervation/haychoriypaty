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
        private Component view;
        private Type gameType;
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
            // Synthetic events must not depend on editor/Game-view focus.
            // Do not replace InputSettings: InputManager destroys temporary defaults.
            previousInputBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
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
            view = root.AddComponent(viewType);
            // Existing pointer checks isolate post-intro controls; the dedicated test below exercises startup.
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
            dataType.GetField("coins").SetValue(data, 500);
            dataType.GetField("staff").SetValue(data, 1);
            dataType.GetField("speed").SetValue(data, 0);
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private object Get(string name) { return gameType.GetProperty(name, PublicInstance).GetValue(game, null); }
        private string Phase { get { return Get("Sim").GetType().GetProperty("Phase", PublicInstance).GetValue(Get("Sim"), null).ToString(); } }
        private object Sim { get { return Get("Sim"); } }
        private void SetSim(string property, object value) { Sim.GetType().GetProperty(property, PublicInstance).SetValue(Sim, value, null); }
        private void Invoke(object target, string method, params object[] args) { target.GetType().GetMethod(method, PublicInstance).Invoke(target, args); }
        private void Tune(string field, object value) { Get("Balance").GetType().GetField(field, PublicInstance).SetValue(Get("Balance"), value); }

        private Vector2 Pixel(float x, float y)
        {
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            float logicalHeight = (float)viewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            float verticalScale = logicalHeight / 960f;
            return new Vector2(viewport.x + x * scale, Screen.height - (viewport.y + y * verticalScale * scale));
        }

        private IEnumerator Click(float x, float y)
        {
            Vector2 p = Pixel(x, y);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = p });
            yield return null; yield return null;
        }

        private void StartChicagoForHiring()
        {
            Invoke(game,"SelectLevel",1); Invoke(game,"StartRound");
            game.GetType().GetProperty("Sim",PublicInstance).GetValue(game,null);
            SetSim("Coins",10000); Time.timeScale=0;
        }
        [UnityTest]
        public IEnumerator ClickingParrilleroDoesNotHireCocacolero()
        {
            StartChicagoForHiring(); yield return Click(270,770);
            Assert.AreEqual(2,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null));
            Assert.AreEqual(0,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
            Assert.AreEqual(9800,Sim.GetType().GetProperty("Coins").GetValue(Sim,null));
        }
        [UnityTest]
        public IEnumerator ClickingCocacoleroDoesNotHireParrillero()
        {
            StartChicagoForHiring(); yield return Click(438,770);
            Assert.AreEqual(1,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null));
            Assert.AreEqual(1,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
            Assert.AreEqual(9800,Sim.GetType().GetProperty("Coins").GetValue(Sim,null));
        }
        [UnityTest]
        public IEnumerator UpgradeGapsAndEdgesNeverDispatchAdjacentRole()
        {
            StartChicagoForHiring();
            foreach(float x in new[]{180f,186f,348f,354f}) yield return Click(x,770);
            Assert.AreEqual(1,Sim.GetType().GetProperty("StaffCount").GetValue(Sim,null)); Assert.AreEqual(10000,Sim.GetType().GetProperty("Coins").GetValue(Sim,null));
            yield return Click(192,770); yield return Click(360,770);
            Assert.AreEqual(2,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null)); Assert.AreEqual(1,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
            yield return Click(179,770); Assert.AreEqual(1,Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim,null));
        }
        [UnityTest]
        public IEnumerator ChicagoTouchPurchasesEachSpecialtyExactlyOnce()
        {
            StartChicagoForHiring();
            Touchscreen screen=InputSystem.AddDevice<Touchscreen>();
            try
            {
                yield return Tap(screen,270,770,101); yield return Tap(screen,438,770,102);
                Assert.AreEqual(2,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null));
                Assert.AreEqual(1,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
                Assert.AreEqual(9600,Sim.GetType().GetProperty("Coins").GetValue(Sim,null));
                yield return Tap(screen,186,770,103); yield return Tap(screen,354,770,104);
                Assert.AreEqual(9600,Sim.GetType().GetProperty("Coins").GetValue(Sim,null));
            }
            finally { InputSystem.RemoveDevice(screen); }
        }

        [UnityTest]
        public IEnumerator IndependentCompositionSurvivesWrapperReloadAndRetry()
        {
            StartChicagoForHiring(); yield return Click(270,770); yield return Click(270,770); yield return Click(438,770);
            gameType.GetMethod("OnEnable",PrivateInstance).Invoke(game,null);
            Assert.AreEqual(3,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null)); Assert.AreEqual(1,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
            Invoke(game,"StartRound"); for(int i=0;i<19;i++) Invoke(Sim,"Step",10f);
            Assert.AreEqual("Lost",Phase); Invoke(game,"Retry");
            Assert.AreEqual(3,Sim.GetType().GetProperty("ParrilleroCount").GetValue(Sim,null)); Assert.AreEqual(1,Sim.GetType().GetProperty("CocacoleroCount").GetValue(Sim,null));
        }

        [Test]
        public void IntroUsesSelectedIntegratedCoverWithoutDuplicateParrillero()
        {
            Type type = view.GetType();
            Assert.AreSame(Resources.Load<Texture2D>("street-parrillero-icon"), type.GetField("parrilleroIcon", PrivateInstance).GetValue(view),
                "The original game/hire portrait remains unchanged");
            Assert.AreSame(Resources.Load<Texture2D>("street-cover-user-v5"), type.GetField("coverArt", PrivateInstance).GetValue(view));
            Assert.AreSame(Resources.Load<Texture2D>("street-logo"), type.GetField("titleLogo", PrivateInstance).GetValue(view));
            Assert.IsNull(type.GetField("coverParrillero", PrivateInstance), "The integrated scene must not add the old cutout");
        }

        [UnityTest]
        public IEnumerator StartupIntroBlocksActionsAndUsesUnscaledTime()
        {
            Type viewType = view.GetType();
            viewType.GetField("introActive", PrivateInstance).SetValue(view, true);
            viewType.GetField("introStarted", PrivateInstance).SetValue(view, Time.unscaledTime);
            viewType.GetMethod("DispatchAction", PrivateInstance).Invoke(view, new object[] { 1 });
            Assert.AreEqual("Ready", Phase, "Native and bridge actions must not start behind the cover");
            viewType.GetMethod("DispatchAction", PrivateInstance).Invoke(view, new object[] { 6 });
            yield return Click(270, 750);
            Assert.AreEqual("Ready", Phase, "Jugar must remain unavailable during the timed presentation");

            Assert.GreaterOrEqual((float)viewType.GetField("CoverHoldDuration", StaticPrivate).GetRawConstantValue(), 4f);
            Assert.GreaterOrEqual((float)viewType.GetField("LogoHoldDuration", StaticPrivate).GetRawConstantValue(), 4f);
            Time.timeScale = 0f;
            float duration = (float)viewType.GetField("IntroDuration", StaticPrivate).GetRawConstantValue();
            yield return new WaitForSecondsRealtime(duration + .1f);
            Assert.IsTrue((bool)viewType.GetField("introActive", PrivateInstance).GetValue(view), "The menu stays open until Jugar");
            Assert.AreEqual("Ready", Phase, "Waiting at the menu must not start a timed round");
            viewType.GetMethod("DispatchAction", PrivateInstance).Invoke(view, new object[] { 1 });
            Assert.AreEqual("Ready", Phase, "Hidden gameplay Start stays blocked even when the menu is available");
            Time.timeScale = 1f;
            yield return Click(270, 750);
            Assert.IsFalse((bool)viewType.GetField("introActive", PrivateInstance).GetValue(view));
            Assert.IsTrue((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view), "Jugar opens the location selector");
            Assert.AreEqual("Ready", Phase, "Opening the selector must not start the round");
            yield return Click(142, 229);
            Assert.IsFalse((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view));
            Assert.AreEqual("Playing", Phase, "Selecting the unlocked first location starts Floresta");
        }

        [UnityTest]
        public IEnumerator LockedLevelNeedsPreviousWinAndVictorySalirReturnsToSelector()
        {
            Type viewType = view.GetType();
            object save = gameType.GetField("save", PrivateInstance).GetValue(game);
            save.GetType().GetField("unlockedLevel", PublicInstance).SetValue(save, 0);
            viewType.GetField("introActive", PrivateInstance).SetValue(view, false);
            viewType.GetField("levelSelectActive", PrivateInstance).SetValue(view, true);
            MethodInfo dispatch = viewType.GetMethod("DispatchAction", PrivateInstance);

            dispatch.Invoke(view, new object[] { 31 }); // Level 2 remains locked before a Level-1 win.
            Assert.IsTrue((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view));
            Assert.AreEqual(0, Get("SelectedLevel"));
            dispatch.Invoke(view, new object[] { 30 });
            Assert.AreEqual("Playing", Phase);

            SetSim("Phase", Enum.Parse(Sim.GetType().GetProperty("Phase", PublicInstance).PropertyType, "Won"));
            yield return null; // StreetGame persists the win and unlocks the next level during Update.
            dispatch.Invoke(view, new object[] { 9 }); // The victory popup's only Salir action.
            Assert.AreEqual("Ready", Phase);
            Assert.IsTrue((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view));
            Assert.AreEqual(1, Get("UnlockedLevel"), "Winning Floresta unlocks only Nueva Chicago");

            dispatch.Invoke(view, new object[] { 31 });
            Assert.IsFalse((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view));
            Assert.AreEqual("Playing", Phase, "Every unlocked level starts directly without a price setup screen");
            Assert.AreEqual(1, Get("SelectedLevel"));
            Assert.IsFalse((bool)Sim.GetType().GetProperty("CanEditPrices", PublicInstance).GetValue(Sim, null));
            Assert.AreEqual(5f, Sim.GetType().GetMethod("GetProductPrice", PublicInstance).Invoke(Sim, new object[] { 4 }));
        }

        [UnityTest]
        public IEnumerator VictoryPopupTouchBoundsReturnToSelectorAndBlockBackgroundUpgrades()
        {
            Type viewType = view.GetType();
            viewType.GetField("introActive", PrivateInstance).SetValue(view, false);
            viewType.GetField("levelSelectActive", PrivateInstance).SetValue(view, false);
            Invoke(game, "StartRound");
            SetSim("Phase", Enum.Parse(Sim.GetType().GetProperty("Phase", PublicInstance).PropertyType, "Won"));
            yield return null;
            Rect viewport = (Rect)viewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float canvasHeight = (float)viewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            float verticalScale = canvasHeight / 960f;
            viewType.GetField("logicalCanvasHeight", PrivateInstance).SetValue(view, canvasHeight);
            viewType.GetField("layoutVerticalScale", PrivateInstance).SetValue(view, verticalScale);
            Rect exit = (Rect)viewType.GetMethod("VictoryExitBounds", PrivateInstance).Invoke(view, null);
            MethodInfo hit = viewType.GetMethod("HitAction", PrivateInstance);
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(35, 715) }), "The modal blocks the upgrade behind it");
            Assert.AreEqual(9, hit.Invoke(view, new object[] { new Vector2(exit.center.x, exit.center.y / verticalScale) }));
            yield return Click(exit.center.x, exit.center.y / verticalScale);
            Assert.AreEqual("Ready", Phase);
            Assert.IsTrue((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view));
        }

        [UnityTest]
        public IEnumerator PriceAreaHasNoHitTargetAndReadyLevelSelectionStartsImmediately()
        {
            Invoke(game, "SelectLevel", 1);
            Type viewType = view.GetType();
            viewType.GetField("introActive", PrivateInstance).SetValue(view, false);
            viewType.GetField("levelSelectActive", PrivateInstance).SetValue(view, false);
            MethodInfo hit = viewType.GetMethod("HitAction", PrivateInstance);
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(270, 502) }), "Former slider location is inert");
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(154, 288) }), "Former product-tab location is inert");
            yield return Click(270, 585); // Starting directly remains supported after reload/retry.
            Assert.AreEqual("Playing", Phase);

            SetSim("Phase", Enum.Parse(Sim.GetType().GetProperty("Phase", PublicInstance).PropertyType, "Ready"));
            viewType.GetMethod("DispatchAction", PrivateInstance).Invoke(view, new object[] { 11 });
            Assert.AreEqual("Playing", Phase, "Choosing an unlocked level from Ready starts it without an intermediate panel");
        }

        [UnityTest]
        public IEnumerator LegacySavedPricesNormalizeAndPricePanelControlsAreGone()
        {
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            ((Behaviour)game).enabled = false;
            PlayerPrefs.SetString(key, "{\"version\":2,\"unlockedLevel\":1,\"price\":60,\"prices\":[60,17,9,11,3,6,5],\"coins\":40,\"staff\":1,\"speed\":0}");
            ((Behaviour)game).enabled = true;
            Invoke(game, "SelectLevel", 1);
            yield return null;
            MethodInfo price = Sim.GetType().GetMethod("GetProductPrice", PublicInstance);
            for (int product = 0; product < 7; product++) Assert.AreEqual(5f, price.Invoke(Sim, new object[] { product }));
            Assert.IsFalse((bool)Sim.GetType().GetProperty("CanEditPrices", PublicInstance).GetValue(Sim, null));
            Type viewType = view.GetType(); MethodInfo hit = viewType.GetMethod("HitAction", PrivateInstance);
            viewType.GetField("introActive", PrivateInstance).SetValue(view, false);
            viewType.GetField("levelSelectActive", PrivateInstance).SetValue(view, false);
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(270, 502) }));
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(154, 288) }));
        }

        [UnityTest]
        public IEnumerator MouseCookAndSpeedPurchasesChargeExactlyOnce()
        {
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase);
            ((Behaviour)game).enabled = false; // The view still receives input; the round clock stays fixed.
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            SetSim("Coins", 500); // Simulated income for the purchase-only fixture, not a starting balance.

            yield return Click(395, 770);
            Assert.AreEqual(2, Get("Sim").GetType().GetProperty("StaffCount", PublicInstance).GetValue(Sim, null));
            Assert.AreEqual(485, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));

            yield return Click(145, 770);
            Assert.AreEqual(1, Get("Sim").GetType().GetProperty("SpeedLevel", PublicInstance).GetValue(Sim, null));
            Assert.AreEqual(480, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));
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
                Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
                SetSim("Coins", 500);
                yield return Tap(screen, 395, 770, 2);
                Assert.AreEqual(2, Get("Sim").GetType().GetProperty("StaffCount", PublicInstance).GetValue(Sim, null));
                Assert.AreEqual(485, Get("Sim").GetType().GetProperty("Coins", PublicInstance).GetValue(Sim, null));
            }
            finally { InputSystem.RemoveDevice(screen); }
        }

        [UnityTest]
        public IEnumerator UpgradeCardsUpdateNextCostsAndStopAtMaximum()
        {
            yield return Click(270, 585);
            ((Behaviour)game).enabled = false; SetSim("Coins", 10000);
            int[] hires = { 15, 30, 60, 100 };
            int[] speeds = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
            int remaining = 10000;
            for (int i = 0; i < hires.Length; i++)
            {
                Assert.AreEqual(hires[i], Sim.GetType().GetProperty("HireCost").GetValue(Sim, null));
                yield return Click(395, 770); remaining -= hires[i];
                Assert.AreEqual(i + 2, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
                Assert.AreEqual(remaining, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            }
            for (int i = 0; i < speeds.Length; i++)
            {
                Assert.AreEqual(speeds[i], Sim.GetType().GetProperty("SpeedCost").GetValue(Sim, null));
                yield return Click(145, 770); remaining -= speeds[i];
                Assert.AreEqual(i + 1, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
                Assert.AreEqual(remaining, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            }
            Assert.AreEqual(0, Sim.GetType().GetProperty("HireCost").GetValue(Sim, null));
            Assert.AreEqual(0, Sim.GetType().GetProperty("SpeedCost").GetValue(Sim, null));
            yield return Click(395, 770); yield return Click(145, 770);
            Assert.AreEqual(5, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
            Assert.AreEqual(9, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
            Assert.AreEqual(9390, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
        }

        [UnityTest]
        public IEnumerator RiotReturnOpensLevelSelectorWithoutStartingAutomaticallyAndDiscardsCoins()
        {
            Tune("levelDurations", new[] { 0.05f, 210f, 240f, 270f, 300f });
            yield return Click(270, 585);
            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual("Lost", Phase);
            Type viewType = view.GetType();
            Texture2D riot = Resources.Load<Texture2D>("street-riot-environment-v1");
            Assert.NotNull(riot, "The staged timeout sequence requires a broken-stall environment plate");
            Assert.AreSame(riot, viewType.GetField("riotBackdrop", PrivateInstance).GetValue(view));
            Texture2D riotFans = Resources.Load<Texture2D>("street-riot-fans-v1");
            Assert.NotNull(riotFans, "The timeout sequence requires live angry-fan animation frames");
            Assert.AreEqual(1774, riotFans.width); Assert.AreEqual(887, riotFans.height);
            Assert.AreSame(riotFans, viewType.GetField("riotFanAtlas", PrivateInstance).GetValue(view));
            MethodInfo poseIndex = viewType.GetMethod("RiotFanPoseIndex", StaticPrivate);
            int raisedPose = (int)poseIndex.Invoke(null, new object[] { .02f, 0 });
            int swingingPose = (int)poseIndex.Invoke(null, new object[] { .19f, 0 });
            Assert.AreNotEqual(raisedPose, swingingPose, "Waiting fans must alternate real raised-stick and swing poses");
            Assert.IsTrue((bool)viewType.GetField("riotScreenActive", PrivateInstance).GetValue(view),
                "The riot scene must be active in the actual StreetView loss screen");
            MethodInfo debris = viewType.GetMethod("RiotDebrisPosition", StaticPrivate);
            Vector2 first = (Vector2)debris.Invoke(null, new object[] { .22f, 3 });
            Vector2 second = (Vector2)debris.Invoke(null, new object[] { .37f, 3 });
            Assert.Greater(Vector2.Distance(first, second), 1f, "Unscaled riot debris should move while the round is Lost");
            SetSim("Coins", 321);
            yield return Click(270, 891);
            Assert.AreEqual("Ready", Phase);
            Assert.IsTrue((bool)viewType.GetField("levelSelectActive", PrivateInstance).GetValue(view),
                "Volver should return to the unlocked level selector without starting another round");
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
        }


        [UnityTest]
        public IEnumerator EveryLevelReadyHasNoPriceTargetAndJugarStartsRound()
        {
            Assert.IsFalse((bool)Sim.GetType().GetProperty("CanEditPrices").GetValue(Sim, null));
            MethodInfo hit = view.GetType().GetMethod("HitAction", PrivateInstance);
            Assert.AreEqual(0, hit.Invoke(view, new object[] { new Vector2(270, 502) }));
            Invoke(game, "SetPrice", 0f);
            Assert.AreEqual(5f, Sim.GetType().GetProperty("Price").GetValue(Sim, null));
            yield return Click(270, 585);
            Assert.AreEqual("Playing", Phase);
        }

        [UnityTest]
        public IEnumerator LegacyPricesNormalizeAndFlorestaResetsTeamButPreservesProgress()
        {
            string key = (string)gameType.GetField("ProgressKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            ((Behaviour)game).enabled = false;
            PlayerPrefs.SetString(key, "{\"version\":2,\"unlockedLevel\":1,\"price\":60,\"prices\":[60,17,9,11,3,6,5],\"coins\":123,\"staff\":3,\"speed\":4}");
            ((Behaviour)game).enabled = true; yield return null;
            Assert.AreEqual(5f, Sim.GetType().GetProperty("Price").GetValue(Sim, null));
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            Assert.AreEqual(1, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
            Assert.AreEqual(0, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
            Assert.That((float)Sim.GetType().GetProperty("WorkRate").GetValue(Sim, null), Is.EqualTo(1f).Within(.0001f));
            for (int product = 0; product < 7; product++) Assert.AreEqual(5f, Sim.GetType().GetMethod("GetProductPrice").Invoke(Sim, new object[] { product }));
            Invoke(game, "SelectLevel", 1);
            Assert.IsFalse((bool)Sim.GetType().GetProperty("CanEditPrices").GetValue(Sim, null));
        }


        [UnityTest]
        public IEnumerator LaterLevelTransitionsAndReloadDiscardCoinsButPreserveProgress()
        {
            ((Behaviour)game).enabled = false; // Keep lifecycle commands deterministic without automatic steps.
            Invoke(game, "SelectLevel", 1);
            Invoke(game, "SetProductPrice", 1, 17f);
            Invoke(game, "StartRound");
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            SetSim("Coins", 500);
            Invoke(game, "StartRound"); // Duplicate Start during play must not erase current income.
            Assert.AreEqual(500, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            Invoke(game, "TryHire"); Invoke(game, "TryUpgradeSpeed");
            Assert.Greater((int)Sim.GetType().GetProperty("Coins").GetValue(Sim, null), 0);

            SetSim("Phase", Enum.Parse(Sim.GetType().GetProperty("Phase").PropertyType, "Won"));
            Invoke(game, "NextLevel");
            Assert.AreEqual("Ready", Phase);
            Assert.AreEqual(2, Get("SelectedLevel")); Assert.AreEqual(2, Get("UnlockedLevel"));
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            Assert.AreEqual(1, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
            Assert.AreEqual(0, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
            Assert.AreEqual(1f, Sim.GetType().GetProperty("WorkRate").GetValue(Sim, null));
            Assert.AreEqual(15, Sim.GetType().GetProperty("HireCost").GetValue(Sim, null));
            Assert.AreEqual(5, Sim.GetType().GetProperty("SpeedCost").GetValue(Sim, null));

            Invoke(game, "StartRound"); SetSim("Coins", 234);
            SetSim("Phase", Enum.Parse(Sim.GetType().GetProperty("Phase").PropertyType, "Lost"));
            Invoke(game, "Retry");
            Assert.AreEqual("Ready", Phase);
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));

            // An older/nonzero serialized round balance must not return on application reload.
            SetSim("Coins", 789);
            gameType.GetMethod("Save", PrivateInstance).Invoke(game, null);
            ((Behaviour)game).enabled = true; yield return null;
            Assert.AreEqual(0, Sim.GetType().GetProperty("Coins").GetValue(Sim, null));
            Assert.AreEqual(2, Get("UnlockedLevel"));
            Assert.AreEqual(1, Sim.GetType().GetProperty("StaffCount").GetValue(Sim, null));
            Assert.AreEqual(0, Sim.GetType().GetProperty("SpeedLevel").GetValue(Sim, null));
            Assert.AreEqual(5f, Sim.GetType().GetMethod("GetProductPrice").Invoke(Sim, new object[] { 1 }));
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
        public void SafeViewportFillsAvailableDisplayWithoutLetterbox(float width, float height, float x, float y, float safeWidth, float safeHeight)
        {
            Rect viewport = Viewport(new Vector2(width, height), new Rect(x, y, safeWidth, safeHeight));
            Assert.AreEqual(new Rect(x, height - y - safeHeight, safeWidth, safeHeight), viewport);
        }

        [Test]
        public void TallPortraitAddsVisibleVerticalPlayAreaWithoutChangingUniformSpriteScale()
        {
            Rect viewport = Viewport(new Vector2(1220, 2712), new Rect(0, 0, 1220, 2712));
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            float logicalHeight = (float)viewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            Assert.That(logicalHeight, Is.EqualTo(1200.3934f).Within(0.1f));
            Assert.That(logicalHeight, Is.GreaterThan(960f));
            Assert.That(viewport.width / 540f, Is.EqualTo(viewport.height / logicalHeight).Within(0.0001f));
        }

        [Test]
        public void FullScreenPointerTransformStillHitsResponsiveUpgradeButton()
        {
            Vector2 screenSize = new Vector2(1220, 2712);
            Rect safe = new Rect(0, 0, screenSize.x, screenSize.y);
            Rect viewport = Viewport(screenSize, safe);
            Type viewType = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            const float designX = 145f, designY = 770f; // Center of the Parrilla Criolla speed card.
            float logicalHeight = (float)viewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            float scale = viewport.width / 540f;
            Vector2 pixel = new Vector2(viewport.x + designX * scale,
                screenSize.y - (viewport.y + designY * logicalHeight / 960f * scale));
            Vector2 layout = (Vector2)viewType.GetMethod("ScreenToLayoutPoint", StaticPrivate)
                .Invoke(null, new object[] { pixel, screenSize, viewport });
            Assert.That(layout.x, Is.EqualTo(designX).Within(.001f));
            Assert.That(layout.y, Is.EqualTo(designY).Within(.001f));
        }
        [UnityTest]
        public IEnumerator StandardReadyButtonsKeepCoverFontPressedStateAndLockedNavigation()
        {
            Type viewType = view.GetType();
            Assert.NotNull(viewType.GetMethod("DrawStandardButton", PrivateInstance));
            var style = (GUIStyle)viewType.GetField("menuTitle", PrivateInstance).GetValue(view);
            Assert.NotNull(style);
            Assert.AreSame(Resources.Load<Font>("Menu/LuckiestGuy-Regular"), style.font);
            Texture2D shared = (Texture2D)viewType.GetField("menuButton", PrivateInstance).GetValue(view);
            Assert.NotNull(shared);
            yield return Click(405f, 930f); // Level 5 is locked in this fixture.
            Assert.AreEqual("Ready", Phase); Assert.AreEqual(0, Get("SelectedLevel"));
            Assert.AreSame(shared, viewType.GetField("menuButton", PrivateInstance).GetValue(view));
            Vector2 start = Pixel(270f, 584f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            Assert.AreEqual(1, viewType.GetField("pressedAction", PrivateInstance).GetValue(view));
            Assert.AreEqual("Ready", Phase, "Press state must not dispatch before release.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start });
            yield return null; yield return null;
            Assert.AreEqual("Playing", Phase);
            Assert.AreEqual(0, viewType.GetField("pressedAction", PrivateInstance).GetValue(view));
        }

    }
}
