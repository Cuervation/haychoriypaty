using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetHubViewTests
    {
        const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject root, accountRoot;
        Component game, view;
        Type gt, vt;
        string key, previous;
        bool hadSave;
        string tutorialKey;
        bool hadTutorialMarker;
        int previousTutorialMarker;
        float oldScale;
        int gameViewWidth = 1080, gameViewHeight = 1920;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameWindow;
        PropertyInfo selectedSize;
        object sizeGroup;
        int previousSize, addedSize;
        void SetPortraitView()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("HCYP_GAMEVIEW_WIDTH"), out int requestedWidth) && requestedWidth >= 320)
                gameViewWidth = requestedWidth;
            if (int.TryParse(Environment.GetEnvironmentVariable("HCYP_GAMEVIEW_HEIGHT"), out int requestedHeight) && requestedHeight >= 480)
                gameViewHeight = requestedHeight;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            gameWindow = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            selectedSize = gameWindow.GetType().GetProperty("selectedSizeIndex", flags);
            previousSize = (int)selectedSize.GetValue(gameWindow);
            Type registryType = editor.GetType("UnityEditor.GameViewSizes");
            object registry = registryType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
            object groupKind = registryType.GetProperty("currentGroupType", flags).GetValue(registry);
            sizeGroup = registryType.GetMethod("GetGroup", flags).Invoke(registry, new[] { groupKind });
            Type kind = editor.GetType("UnityEditor.GameViewSizeType");
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"), new object[] { Enum.Parse(kind, "FixedResolution"), gameViewWidth, gameViewHeight, "Temporary hub QA" });
            sizeGroup.GetType().GetMethod("AddCustomSize", flags).Invoke(sizeGroup, new[] { size });
            addedSize = (int)sizeGroup.GetType().GetMethod("GetTotalCount", flags).Invoke(sizeGroup, null) - 1;
            selectedSize.SetValue(gameWindow, addedSize); gameWindow.Repaint();
        }
#endif
        object Field(string name) => vt.GetField(name, P).GetValue(view);
        void Dispatch(int action) => vt.GetMethod("DispatchAction", P).Invoke(view, new object[] { action });
        void ClickAction(int action)
        {
            Rect bounds = (Rect)vt.GetMethod("HubArtworkActionBounds", P).Invoke(view, new object[] { action });
            Assert.Greater(bounds.width, 0);
            Assert.AreEqual(action, vt.GetMethod("HitAction", P).Invoke(view, new object[] { bounds.center }));
            Rect vp = (Rect)vt.GetMethod("CanvasViewport", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            Vector2 pixel = new Vector2(vp.x + bounds.center.x * vp.width / 540f,
                Screen.height - (vp.y + bounds.center.y * vp.height / 960f));
            MethodInfo pointer = vt.GetMethod("HandlePointer", P);
            pointer.Invoke(view, new object[] { pixel, true, false });
            pointer.Invoke(view, new object[] { pixel, false, true });
        }
        IEnumerator Capture(string name)
        {
            string folder = Environment.GetEnvironmentVariable("HCYP_CAPTURE_DIR");
            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(Path.GetTempPath(), "hcyp-street-hub-view-tests");
            Directory.CreateDirectory(folder);
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.NotNull(capture);
            Assert.AreEqual(gameViewWidth, capture.width); Assert.AreEqual(gameViewHeight, capture.height);
            string resolutionName = name.Replace("1080x1920", gameViewWidth + "x" + gameViewHeight);
            File.WriteAllBytes(Path.Combine(folder, resolutionName + ".png"), capture.EncodeToPNG());
            UnityEngine.Object.Destroy(capture);
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            gt = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            vt = Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
            tutorialKey = (string)vt.GetField("LevelOneTutorialCompletedKey",
                BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            hadTutorialMarker = PlayerPrefs.HasKey(tutorialKey);
            previousTutorialMarker = hadTutorialMarker ? PlayerPrefs.GetInt(tutorialKey) : 0;
            key = (string)gt.GetField("ProgressKey").GetValue(null);
            hadSave = PlayerPrefs.HasKey(key); previous = hadSave ? PlayerPrefs.GetString(key) : null;
            object seed = Activator.CreateInstance(gt.GetNestedType("SaveData", BindingFlags.Public));
            seed.GetType().GetField("version").SetValue(seed, 4);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(seed)); PlayerPrefs.Save();
            oldScale = Time.timeScale; Time.timeScale = 1;
#if UNITY_EDITOR
            SetPortraitView();
            yield return null; yield return null;
            Assert.AreEqual(gameViewWidth, Screen.width); Assert.AreEqual(gameViewHeight, Screen.height);
#endif
            root = new GameObject("Temporary hub integration test");
            root.AddComponent<Camera>(); root.AddComponent<AudioListener>();
            game = root.AddComponent(gt); view = root.AddComponent(vt);
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (accountRoot != null) UnityEngine.Object.Destroy(accountRoot);
            yield return null;
            if (hadSave) PlayerPrefs.SetString(key, previous); else PlayerPrefs.DeleteKey(key);
            if (hadTutorialMarker) PlayerPrefs.SetInt(tutorialKey, previousTutorialMarker);
            else PlayerPrefs.DeleteKey(tutorialKey);
            PlayerPrefs.Save(); Time.timeScale = oldScale;
#if UNITY_EDITOR
            if (sizeGroup != null)
            {
                selectedSize.SetValue(gameWindow, previousSize);
                sizeGroup.GetType().GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(sizeGroup, new object[] { addedSize });
            }
#endif
        }
        [Test] public void StartupDoesNotDefineTheUnrequestedGenericLoadingScreen()
        {
            Assert.IsNull(vt.GetMethod("DrawStartupLoadingFallback", P),
                "Startup should not draw the separate generic PREPARANDO EL BARRIO screen.");
        }

        [UnityTest] public IEnumerator AmbientOverlaysRenderInTheFullHubGameView()
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while (!(bool)Field("viewResourcesReady") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)Field("viewResourcesReady"), "Hub artwork must be loaded before the capture.");
            Assert.NotNull(Field("hubBackdrop"), "The existing hub illustration remains the full-screen background.");

            vt.GetField("introActive", P).SetValue(view, false);
            vt.GetField("hubActive", P).SetValue(view, true);
            vt.GetField("settingsActive", P).SetValue(view, false);
            vt.GetField("marketActive", P).SetValue(view, false);
            vt.GetField("badgesActive", P).SetValue(view, false);
            vt.GetField("rankingActive", P).SetValue(view, false);

            yield return null;
            yield return Capture("hub-ambient-initial-playmode-1080x1920");
            yield return new WaitForSecondsRealtime(18f);
            Assert.IsTrue((bool)Field("hubActive"), "Ambient effects remain limited to the visible hub.");
            object overlay = Field("hubAmbientOverlay");
            Assert.NotNull(overlay, "The overlay renderer should exist only after the hub has been drawn.");
            FieldInfo animationClock = overlay.GetType().GetField("animationTime", P);
            Assert.NotNull(animationClock);
            Assert.Greater((float)animationClock.GetValue(overlay), 15f, "The ambient cycles advance while the hub is visible.");
            yield return Capture("hub-ambient-cycle-playmode-1080x1920");
        }

        [UnityTest] public IEnumerator NeighborhoodCharactersStayStillDuringAShortHubVisit()
        {
            float loadDeadline = Time.realtimeSinceStartup + 30f;
            while (!(bool)Field("viewResourcesReady") && Time.realtimeSinceStartup < loadDeadline) yield return null;
            Assert.IsTrue((bool)Field("viewResourcesReady"), "Hub artwork must load before the static character view is checked.");

            vt.GetField("introActive", P).SetValue(view, false);
            vt.GetField("hubActive", P).SetValue(view, true);
            vt.GetField("settingsActive", P).SetValue(view, false);
            vt.GetField("marketActive", P).SetValue(view, false);
            vt.GetField("badgesActive", P).SetValue(view, false);
            vt.GetField("rankingActive", P).SetValue(view, false);
            Assert.IsNull(Field("hubCharacterScene"), "The paused character rig must not load or replace the still artwork.");
            yield return Capture("hub-characters-static-initial-playmode-1080x1920");

            yield return new WaitForSecondsRealtime(6.5f);
            Assert.IsTrue((bool)Field("hubActive"), "The hub remains visible for the whole observation window.");
            Assert.IsNull(Field("hubCharacterScene"), "No character rig or movement should start during the visit.");
            yield return Capture("hub-characters-static-after-6s-playmode-1080x1920");
        }

        [UnityTest] public IEnumerator StartupShowsTheRealCoverWhileOtherAssetsLoad()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Field("coverArt") == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.NotNull(Field("coverArt"), "The startup cover should become available during the first resource batch.");
            Assert.IsFalse((bool)Field("startupArtReady"),
                "The cover-only interval should occur while the remaining first-batch assets are loading.");
            yield return Capture("startup-cover-during-resource-load-1080x1920");
        }

        [UnityTest] public IEnumerator StartupPresentationAndHubRenderBeforeFirebaseActivatesThePlayer()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!(bool)Field("viewResourcesReady") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)Field("viewResourcesReady"), "Startup art should finish loading.");
            Assert.IsTrue((bool)gt.GetProperty("IsPlayerActive").GetValue(game), "The test fixture starts with its local test profile.");
            Assert.IsTrue((bool)gt.GetMethod("SuspendPlayer").Invoke(game, null), "Simulate no authenticated profile.");
            Assert.IsFalse((bool)gt.GetProperty("IsPlayerActive").GetValue(game));

            float duration = (float)vt.GetField("IntroDuration", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            vt.GetField("introActive", P).SetValue(view, true);
            vt.GetField("hubActive", P).SetValue(view, false);
            vt.GetField("splashClockStarted", P).SetValue(view, true);
            vt.GetField("introStarted", P).SetValue(view, Time.unscaledTime - .25f);
            vt.GetMethod("Update", P).Invoke(view, null);
            Assert.IsTrue((bool)Field("introActive"), "The startup presentation remains visible without a player profile.");
            yield return Capture("startup-intro-before-auth-1080x1920");

            vt.GetField("introStarted", P).SetValue(view, Time.unscaledTime - duration - .1f);
            vt.GetMethod("Update", P).Invoke(view, null);
            Assert.IsFalse((bool)Field("introActive"));
            Assert.IsTrue((bool)Field("hubActive"), "The hub must appear before Firebase login.");
            Assert.IsFalse((bool)gt.GetProperty("IsPlayerActive").GetValue(game));
            Assert.IsFalse((bool)vt.GetProperty("HubAccountActionRendered").GetValue(view),
                "No separate account button is shown on the hub.");
            yield return Capture("hub-before-auth-1080x1920");
        }

        [Test] public void LoginRequiredHubActionOpensAccountAndReadyJugarOpensSelector()
        {
            Type sessionType = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            accountRoot = new GameObject("Temporary Firebase account panel test");
            Component session = accountRoot.AddComponent(sessionType);
            ((Behaviour)session).enabled = false; // Keep BootstrapAsync from contacting Firebase in a UI unit test.
            sessionType.GetProperty("Instance").GetSetMethod(true).Invoke(null, new[] { session });
            PropertyInfo state = sessionType.GetProperty("State");
            state.GetSetMethod(true).Invoke(session, new[] { Enum.Parse(state.PropertyType, "LoginRequired") });
            vt.GetField("introActive", P).SetValue(view, false);
            vt.GetField("hubActive", P).SetValue(view, true);
            vt.GetField("levelSelectActive", P).SetValue(view, false);
            FieldInfo editorGate = gt.GetField("AllowEditorTestInitialization", BindingFlags.Public | BindingFlags.Static);
            bool previousEditorGate = (bool)editorGate.GetValue(null);
            editorGate.SetValue(null, false);
            string musicKey = (string)vt.GetField("HubMusicEnabledPlayerPrefKey",
                BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            bool hadMusicPreference = PlayerPrefs.HasKey(musicKey);
            int previousMusicPreference = PlayerPrefs.GetInt(musicKey, 1);
            try
            {
            PlayerPrefs.SetInt(musicKey, 1);
            int topMusicAction = (int)vt.GetField("HubTopMusicAction",
                BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            Dispatch(topMusicAction); // The icon toggles locally without opening the settings panel or Firebase gate.
            Assert.IsFalse((bool)vt.GetMethod("HubMusicEnabled", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null));
            Assert.IsFalse((bool)Field("settingsActive"));
            Assert.IsFalse((bool)sessionType.GetProperty("AccountMenuOpen").GetValue(session));
            Dispatch(topMusicAction);
            Assert.IsTrue((bool)vt.GetMethod("HubMusicEnabled", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null));

            Dispatch(207); // Settings are local and available before Firebase login.
            Assert.IsTrue((bool)Field("settingsActive"));
            Assert.IsFalse((bool)sessionType.GetProperty("AccountMenuOpen").GetValue(session));
            Dispatch(208);
            Assert.IsFalse((bool)vt.GetMethod("HubMusicEnabled", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null));
            Dispatch(209);
            Assert.IsFalse((bool)Field("settingsActive"));

            Dispatch(200); // Jugar while signed out should direct the player to Cuenta, not silently fail.
            Assert.IsTrue((bool)sessionType.GetProperty("AccountMenuOpen").GetValue(session));
            Assert.IsTrue((bool)Field("hubActive"));
            Assert.IsFalse((bool)Field("levelSelectActive"));

            state.GetSetMethod(true).Invoke(session, new[] { Enum.Parse(state.PropertyType, "Ready") });
            sessionType.GetField("game", P).SetValue(session, game);
            sessionType.GetField("accountOpen", P).SetValue(session, true);
            sessionType.GetMethod("ContinueToGameAfterGoogleSignIn", P).Invoke(session, null);
            Assert.IsFalse((bool)sessionType.GetProperty("AccountMenuOpen").GetValue(session));
            Assert.IsTrue((bool)Field("hubActive"), "Finishing sign-in leaves the player on the hub.");

            Dispatch(200);
            Assert.IsTrue((bool)Field("levelSelectActive"), "Jugar must work after the session becomes Ready.");
            Assert.IsFalse((bool)Field("hubActive"));
            object sim = gt.GetProperty("Sim").GetValue(game);
            Assert.AreEqual("Ready", sim.GetType().GetProperty("Phase").GetValue(sim).ToString());
            }
            finally
            {
                if (hadMusicPreference) PlayerPrefs.SetInt(musicKey, previousMusicPreference);
                else PlayerPrefs.DeleteKey(musicKey);
                PlayerPrefs.Save();
                editorGate.SetValue(null, previousEditorGate);
            }
        }

        [UnityTest] public IEnumerator AutomaticIntroThenHubPointerDestinationsAndBack()
        {
            Assert.IsTrue((bool)Field("introActive"));
            object sim = gt.GetProperty("Sim").GetValue(game);
            PropertyInfo remaining = sim.GetType().GetProperty("TimeRemaining");
            float before = (float)remaining.GetValue(sim);
            yield return new WaitForSecondsRealtime(.20f);
            yield return Capture("intro-pop-playmode-1080x1920");
            yield return new WaitForSecondsRealtime(.52f);
            yield return Capture("intro-settled-playmode-1080x1920");
            yield return new WaitForSecondsRealtime(2.25f);
            yield return Capture("intro-finale-playmode-1080x1920");
            float deadline = Time.realtimeSinceStartup + 7f;
            while ((bool)Field("introActive") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse((bool)Field("introActive"), "Authenticated splash must advance automatically.");
            Assert.IsTrue((bool)Field("hubActive"));
            Assert.AreEqual(before, (float)remaining.GetValue(sim), .01f, "Intro cannot consume round time.");
            yield return Capture("hub-playmode-1080x1920");
            Assert.IsFalse((bool)vt.GetProperty("HubAccountActionRendered").GetValue(view),
                "No separate account button is shown on the hub.");
            Dispatch(207);
            Assert.IsTrue((bool)Field("settingsActive"));
            yield return Capture("hub-settings-1080x1920");
            Dispatch(209);
            Assert.IsFalse((bool)Field("settingsActive"));
            ClickAction(202); Assert.IsTrue((bool)Field("badgesActive"));
            object save = gt.GetField("save", P).GetValue(game);
            object statistics = save.GetType().GetField("statistics").GetValue(save);
            statistics.GetType().GetField("deliveredByProduct").SetValue(statistics, new long[] { 499, 500, 2499, 2500, 4999, 5000, 0 });
            statistics.GetType().GetMethod("Normalize").Invoke(statistics, null);
            MethodInfo rows = vt.GetMethod("SalesBadgeRowBounds", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (float height in new[] { 900f, 960f, 1200f })
            for (int product = 0; product < 7; product++)
            {
                Rect row = (Rect)rows.Invoke(null, new object[] { product, height });
                Assert.GreaterOrEqual(row.yMin, 160); Assert.Less(row.yMax, height - 109f);
                Assert.AreEqual(500f, row.width);
            }
            yield return Capture("badges-playmode-1080x1920");
            Dispatch(205); Assert.IsTrue((bool)Field("hubActive"));
            ClickAction(203); Assert.IsTrue((bool)Field("rankingActive"));
            yield return Capture("ranking-playmode-1080x1920");
            Dispatch(206); Assert.IsTrue((bool)Field("hubActive"));
            ClickAction(201); Assert.IsTrue((bool)Field("marketActive"));
            Dispatch(70); Assert.IsFalse((bool)Field("marketActive"));
            ClickAction(200); Assert.IsTrue((bool)Field("levelSelectActive"));
            Assert.IsFalse((bool)Field("hubActive"));
        }
    }
}
