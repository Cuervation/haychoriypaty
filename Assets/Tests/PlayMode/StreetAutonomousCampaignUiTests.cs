using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Isolated UI-driven campaign pilot. Test money is injected only into this temporary
    /// StreetGame instance; every decision is sent through Input System and StreetView.Update.
    /// </summary>
    public sealed class StreetAutonomousCampaignUiTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags StaticField = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Logs/Acceptance/AutonomousCampaign-20261019"));
        private static readonly string ModeBLevel6CheckpointPath = Path.Combine(OutputDirectory, "mode-b-level6-authentic-checkpoint.json");
        private static Type GameType => Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
        private static Type ViewType => Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
        private static Type SimulationType => Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true);

        // FirebasePlayerSession.Install runs at BeforeSceneLoad. Set its existing Editor-only
        // guard earlier, before any test scene loads; SetUp then turns the PlayerPrefs path off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SkipFirebaseForIsolatedTestPlayer()
        {
            GameType.GetField("AllowEditorTestInitialization", StaticField).SetValue(null, true);
            Debug.Log("Autonomous campaign test bootstrap: Firebase disabled before scene load.");
        }

        private GameObject root;
        private Component game;
        private Component view;
        private Type firebaseSessionType;
        private Mouse mouse;
        private AudioListener addedAudioListener;
        private bool previousEditorPersistence;
        private float previousTimeScale;
        private float matchTimeScale;
        private bool realEconomyMode;
        private bool resumeFromLevel6Checkpoint;
        private bool previousBackground;
        private InputSettings.BackgroundBehavior previousInputBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        private readonly List<string> memorySnapshots = new List<string>();
        private readonly List<string> decisionSequence = new List<string>();
        private readonly List<CampaignObservation> observations = new List<CampaignObservation>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            FieldInfo editorGate = GameType.GetField("AllowEditorTestInitialization", StaticField);
            previousEditorPersistence = (bool)editorGate.GetValue(null);
            // Critical: adding StreetGame while this is false cannot read or write PlayerPrefs.
            editorGate.SetValue(null, false);
            firebaseSessionType = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            Assert.IsNull(firebaseSessionType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null, null),
                "Firebase bootstrap was not suppressed; refuse to create a test player.");

            previousTimeScale = Time.timeScale;
            previousBackground = Application.runInBackground;
            previousInputBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Time.timeScale = 1f;
            Application.runInBackground = true;

            Directory.CreateDirectory(OutputDirectory);
            memorySnapshots.Clear();
            decisionSequence.Clear();
            observations.Clear();
            mouse = InputSystem.AddDevice<Mouse>();
            root = new GameObject("Isolated autonomous UI campaign (temporary)");
            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
                addedAudioListener = root.AddComponent<AudioListener>();
            game = root.AddComponent(GameType);
            realEconomyMode = TestContext.CurrentContext.Test.Name.StartsWith("ModeB_", StringComparison.Ordinal);
            resumeFromLevel6Checkpoint = TestContext.CurrentContext.Test.Name.StartsWith("ModeB_ResumeFromLevel6Checkpoint_", StringComparison.Ordinal);
            if (resumeFromLevel6Checkpoint)
                Assert.IsTrue(File.Exists(ModeBLevel6CheckpointPath), "Resume requires a checkpoint written after a genuine Level 5 UI victory.");
            object balance = Get(game, "Balance");
            Array levels = (Array)balance.GetType().GetField("levelBalances", PublicInstance).GetValue(balance);
            if (!realEconomyMode)
                foreach (object level in levels)
                    level.GetType().GetField("startingCoins", PublicInstance).SetValue(level, 50_000);
            string initialSave = !realEconomyMode ? CreateTestSaveJson()
                : resumeFromLevel6Checkpoint ? File.ReadAllText(ModeBLevel6CheckpointPath) : null;
            Assert.IsTrue((bool)GameType.GetMethod("ActivatePlayer", PublicInstance).Invoke(game,
                new object[] { initialSave, CreateMemoryPersistenceDelegate() }),
                "Temporary in-memory player failed to initialize.");
            if (resumeFromLevel6Checkpoint)
                Assert.AreEqual(5, Get(game, "UnlockedLevel"), "Resume must use the Racing unlock earned by the previous real UI campaign.");
            Assert.IsNull(firebaseSessionType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null, null),
                "No Firebase session may exist during the test.");
            view = root.AddComponent(ViewType);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Keep the production PlayerPrefs boot gate off until this test owner is destroyed.
            GameType.GetField("AllowEditorTestInitialization", StaticField).SetValue(null, false);
            if (root != null) UnityEngine.Object.Destroy(root);
            if (addedAudioListener != null) UnityEngine.Object.Destroy(addedAudioListener);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = previousInputBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            yield return null; // StreetGame.OnDisable persists only into the in-memory delegate.
            Time.timeScale = previousTimeScale;
            Application.runInBackground = previousBackground;
            GameType.GetField("AllowEditorTestInitialization", StaticField).SetValue(null, previousEditorPersistence);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Level01_AllBoys_RealIntroAndPlayControls_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(0);
        }

        [UnityTest]
        public IEnumerator Level02_NuevaChicago_MarketAndSpecialistUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(1);
        }

        [UnityTest]
        public IEnumerator Level03_Velez_MarketAndMultiProductUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(2);
        }

        [UnityTest]
        public IEnumerator Level04_Ferro_MarketAndMultiRoleUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(3);
        }

        [UnityTest]
        public IEnumerator Level05_Independiente_MarketAndFerneteroUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(4);
        }

        [UnityTest]
        public IEnumerator Level06_Racing_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(5);
        }

        [UnityTest]
        public IEnumerator Level07_SanLorenzo_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(6);
        }

        [UnityTest]
        public IEnumerator Level08_River_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(7);
        }

        [UnityTest]
        public IEnumerator Level09_Boca_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(8);
        }

        [UnityTest]
        public IEnumerator Level10_Camioneros_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(9);
        }

        [UnityTest]
        public IEnumerator Level11_LosRedondos_MarketAndMozoUi_InMemoryOnly()
        {
            yield return RunUnrestrictedAttempt(10);
        }

        [UnityTest]
        public IEnumerator Level10_Camioneros_FocusedDrinksAndService_Attempt02()
        {
            yield return RunUnrestrictedAttempt(9, 2);
        }

        [UnityTest]
        public IEnumerator Level11_LosRedondos_FocusedDrinksAndService_Attempt02()
        {
            yield return RunUnrestrictedAttempt(10, 2);
        }

        [UnityTest]
        public IEnumerator Level10_Camioneros_MaximumSupportTeam_Attempt03()
        {
            yield return RunUnrestrictedAttempt(9, 3);
        }

        [UnityTest]
        public IEnumerator Level11_LosRedondos_MaximumSupportTeam_Attempt03()
        {
            yield return RunUnrestrictedAttempt(10, 3);
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Level10_Camioneros_NativeGameTimeControl_Attempt04()
        {
            yield return RunUnrestrictedAttempt(9, 4);
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Level11_LosRedondos_NativeGameTimeControl_Attempt04()
        {
            yield return RunUnrestrictedAttempt(10, 4);
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Level11_LosRedondos_MoreCooks_NativeGameTimeControl_Attempt05()
        {
            yield return RunUnrestrictedAttempt(10, 5);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_FreshSave_RealEconomyProgression_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(false);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_CreateLevel6Checkpoint_FromFreshSave_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(false, true);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt08_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt09_NormalAndCocaFocus_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 9);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt10_OneNormalAfterServiceAndSpeedReserve_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 10);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt11_FiveNormalThreeMozos_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 11);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt12_BalancedOverflowNormal_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 12);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt13_BalancedOverflowNormalAfterRoster_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 13);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt14_ThreeMozosForSpeedNine_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 14);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt15_OneFerneteroSpeedNine_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 15);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt16_ReplayBestOpportunisticRoster_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 16);
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator ModeB_ResumeFromLevel6Checkpoint_Attempt17_OneFernFiveNormal_ActualUiOnly()
        {
            yield return RunRealEconomyProgression(true, false, 17);
        }

        private IEnumerator RunUnrestrictedAttempt(int levelIndex, int attempt = 1)
        {
            string prefix = "L" + (levelIndex + 1).ToString("00") + "-A-attempt" + attempt.ToString("00") + "-";
            string strategy;
            int extraParrilleros, extraCocacoleros, extraPremium, extraFerneteros, extraMozos;
            if (levelIndex == 0)
            {
                strategy = "A01: hire five additional Parrilleros first, then nine cook-speed upgrades";
                extraParrilleros = 5; extraCocacoleros = extraPremium = extraFerneteros = extraMozos = 0;
            }
            else if (levelIndex == 1)
            {
                strategy = "A02: max speed first, then a balanced three-Parrillero/three-Cocacolero team";
                extraParrilleros = 2; extraCocacoleros = 3; extraPremium = extraFerneteros = extraMozos = 0;
            }
            else if (levelIndex == 2)
            {
                strategy = "A03: food-first hiring for the larger Chori/Paty demand, then two Cocacoleros and speed";
                extraParrilleros = 5; extraCocacoleros = 2; extraPremium = extraFerneteros = extraMozos = 0;
            }
            else if (levelIndex == 3)
            {
                strategy = "A04: drinks/Premium specialists first, then Parrilleros and speed";
                extraParrilleros = 3; extraCocacoleros = extraPremium = 3; extraFerneteros = extraMozos = 0;
            }
            else if (levelIndex == 4)
            {
                strategy = "A05: four-role balanced specialist team, then speed";
                extraParrilleros = 3; extraCocacoleros = 3; extraPremium = 4; extraFerneteros = 3; extraMozos = 0;
            }
            else
            {
                strategy = "A06: four-slot Normal/Premium grills, three drink specialists and three Mozos, speed first";
                extraParrilleros = 3; extraCocacoleros = 3; extraPremium = 4; extraFerneteros = 3; extraMozos = 3;
            }
            if (attempt == 2 && levelIndex >= 9)
            {
                strategy = "A07: speed first; retain four cooks per grill, expand Cocacoleros and Mozos to five, reduce Ferneteros to one";
                extraParrilleros = 3; extraPremium = 4; extraCocacoleros = 5; extraFerneteros = 1; extraMozos = 5;
            }
            else if ((attempt == 3 || attempt == 4) && levelIndex >= 9)
            {
                strategy = attempt == 4
                    ? "A09: same maximum-support team as A08, validated at native Time.timeScale=1"
                    : "A08: speed first; four cooks per grill, eight Cocacoleros and eight Mozos, one Fernetero";
                extraParrilleros = 3; extraPremium = 4; extraCocacoleros = 8; extraFerneteros = 1; extraMozos = 8;
            }
            else if (attempt == 5 && levelIndex == 10)
            {
                strategy = "A10: test an eight-Normal/eight-Premium cook queue at native Time.timeScale=1; retain max drink/Mozo support";
                extraParrilleros = 7; extraPremium = 8; extraCocacoleros = 8; extraFerneteros = 1; extraMozos = 8;
            }

            yield return WaitForMenu();
            yield return Click(new Rect(126f, 704f, 288f, 92f).center); // Genuine intro JUGAR.
            if (levelIndex >= 6) yield return Click(GetStaticRectField("LevelSelectNext").center);
            if (levelIndex == 0)
                yield return Click(InvokeStaticRectNoArgs("MarketSelectorBounds").center); // Level 1 stock inspection.
            else
                yield return Click(InvokeStaticRect("LevelSelectCardBounds", levelIndex).center);

            object sim = Get(game, "Sim"), market = Get(game, "Market");
            Assert.AreEqual(levelIndex, Get(sim, "LevelIndex"), "The UI card must select the requested club.");
            Assert.IsTrue((bool)Get(view, "marketActive"), "The selected level must be in its authentic Mercado screen.");
            long marketCentsStarting = (long)Get(market, "BalanceCents");
            object marketData = Get(market, "Data");
            long purchaseSpendBefore = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData);
            string marketShot = prefix + "market.png";
            yield return Capture(marketShot);
            long missingCost = (long)Call(market, "MinimumPurchaseCostCents", sim);
            if (missingCost > 0)
            {
                Assert.LessOrEqual(missingCost, (long)Get(market, "BalanceCents"), "The isolated Mercado budget should cover real catalog prices.");
                yield return ClickMarket(InvokeMarketBuyMissingBounds().center);
                decisionSequence.Add("market:buy-missing:" + missingCost.ToString());
                yield return new WaitForSecondsRealtime(.35f); // Honor the real market's purchase debounce.
            }
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim), "All objective ingredients must come from the authentic kit or Mercado.");
            Assert.IsTrue((bool)Get(game, "CanStartRound"), "Market stock gate should open only after required stock exists.");
            if (missingCost > 0) yield return Capture(prefix + "market-restocked.png");
            yield return ClickMarket(InvokeViewRectNoArgs("MarketPlayBounds").center);
            Assert.AreEqual("Playing", Get(sim, "Phase").ToString(), "JUGAR PARTIDO must start the real match through the UI.");
            int staffCoinsStarting = (int)Get(sim, "Coins"); // StartRound applies the real per-attempt opening balance here.
            yield return Capture(prefix + "start.png");

            // Purchase order varies by level to isolate demand/specialty hypotheses.
            bool speedFirst = (levelIndex == 1) || (attempt >= 2 && levelIndex >= 9);
            if (speedFirst) yield return BuySpeedThroughUi(sim, 9);
            if (levelIndex == 0)
            {
                yield return HireRoleThroughUi(sim, 2, "CanHireParrillero", extraParrilleros);
            }
            else if (levelIndex == 1)
            {
                yield return HireRoleThroughUi(sim, 2, "CanHireParrillero", extraParrilleros);
                yield return HireRoleThroughUi(sim, 16, "CanHireCocacolero", extraCocacoleros);
            }
            else if (levelIndex == 2)
            {
                yield return HireRoleThroughUi(sim, 2, "CanHireParrillero", extraParrilleros);
                yield return HireRoleThroughUi(sim, 16, "CanHireCocacolero", extraCocacoleros);
            }
            else if (levelIndex == 3)
            {
                yield return HireRoleThroughUi(sim, 16, "CanHireCocacolero", extraCocacoleros);
                yield return HireRoleThroughUi(sim, 20, "CanHireParrilleroPremium", extraPremium);
                yield return HireRoleThroughUi(sim, 2, "CanHireParrillero", extraParrilleros);
            }
            else
            {
                yield return HireRoleThroughUi(sim, 2, "CanHireParrillero", extraParrilleros);
                yield return HireRoleThroughUi(sim, 20, "CanHireParrilleroPremium", extraPremium);
                yield return HireRoleThroughUi(sim, 16, "CanHireCocacolero", extraCocacoleros);
                yield return HireRoleThroughUi(sim, 21, "CanHireFernetero", extraFerneteros);
                if (levelIndex >= 5) yield return HireRoleThroughUi(sim, 22, "CanHireMozo", extraMozos);
            }
            if (!speedFirst && levelIndex != 1) yield return BuySpeedThroughUi(sim, 9);
            yield return Capture(prefix + "workers.png");

            // The test advances game-time normally at 8x only to shorten wall-clock waiting.
            // No goals, game-time durations, worker clocks, prices, production or customer rules change.
            matchTimeScale = TestContext.CurrentContext.Test.Name.IndexOf("NativeGameTimeControl", StringComparison.OrdinalIgnoreCase) >= 0 ? 1f : 8f;
            Time.timeScale = matchTimeScale;
            float activityDeadline = Time.realtimeSinceStartup + 40f / Mathf.Max(0.1f, matchTimeScale) + 10f;
            float nextObservation = 30f;
            while ((float)Get(sim, "Elapsed") < 30f && Get(sim, "Phase").ToString() == "Playing" &&
                   Time.realtimeSinceStartup < activityDeadline)
            {
                float elapsed = (float)Get(sim, "Elapsed");
                if (elapsed >= nextObservation) { observations.Add(CaptureObservation(sim)); nextObservation += 30f; }
                yield return null;
            }
            yield return Capture(prefix + "activity.png");
            yield return new WaitForSecondsRealtime(.25f);
            yield return Capture(prefix + "grill-workers.png");
            float resultDeadline = Time.realtimeSinceStartup + (float)Get(sim, "TimeRemaining") / Mathf.Max(0.1f, matchTimeScale) + 30f;
            while (Get(sim, "Phase").ToString() == "Playing" && Time.realtimeSinceStartup < resultDeadline)
            {
                float elapsed = (float)Get(sim, "Elapsed");
                if (elapsed >= nextObservation) { observations.Add(CaptureObservation(sim)); nextObservation += 30f; }
                yield return null;
            }
            Assert.AreNotEqual("Playing", Get(sim, "Phase").ToString(), "The match must reach its real win/loss state before timing out.");
            yield return Capture(prefix + "result.png");

            var report = BuildReport(levelIndex, attempt, strategy, prefix, marketShot, staffCoinsStarting,
                marketCentsStarting, purchaseSpendBefore, matchTimeScale);
            WriteProgress(report);
            TestContext.WriteLine(JsonUtility.ToJson(report, true));
            Assert.Greater(memorySnapshots.Count, 0, "Game save callbacks should remain in the memory-only store.");
        }

        private IEnumerator RunRealEconomyProgression(bool resumeFromLevel6, bool stopAfterLevel6Checkpoint = false, int resumeAttempt = 8)
        {
            Assert.IsTrue(realEconomyMode, "Mode B uses only a fresh save or a genuine campaign checkpoint.");
            Time.timeScale = 1f;
            Assert.AreEqual(resumeFromLevel6 ? 5 : 0, Get(game, "UnlockedLevel"), "Only a real UI victory may unlock a level.");
            if (!resumeFromLevel6)
                Assert.AreEqual(0L, Get(game, "Market") == null ? -1L : Get(Get(game, "Market"), "BalanceCents"),
                    "Mode B must begin with the project's real new-game Mercado balance.");

            yield return WaitForMenu();
            yield return Click(new Rect(126f, 704f, 288f, 92f).center); // Real intro JUGAR.
            if (resumeFromLevel6)
            {
                Assert.IsTrue((bool)Get(view, "levelSelectActive"), "The saved campaign must open the genuine selector.");
                yield return Click(InvokeStaticRect("LevelSelectCardBounds", 5).center); // Actual unlocked Racing card.
                Assert.AreEqual(5, Get(game, "SelectedLevel"), "Racing must be selected through its visible card.");
            }
            else
                yield return Click(InvokeStaticRectNoArgs("MarketSelectorBounds").center); // Real Mercado access for All Boys.

            int levelIndex = resumeFromLevel6 ? 5 : 0;
            bool stopCampaign = false;
            while (levelIndex < 11 && !stopCampaign)
            {
                bool levelWon = false;
                int attempt = resumeFromLevel6 && levelIndex == 5 ? resumeAttempt : 1;
                int maximumAttemptsForLevel = resumeFromLevel6 && levelIndex == 5 ? resumeAttempt : 7;
                while (attempt <= maximumAttemptsForLevel && !levelWon && !stopCampaign)
                {
                    decisionSequence.Clear();
                    observations.Clear();
                    object sim = Get(game, "Sim"), market = Get(game, "Market");
                    string prefix = "L" + (levelIndex + 1).ToString("00") + "-B-attempt" + attempt.ToString("00") + "-";
                    string strategy = attempt == 1
                        ? "B: speed-first; then food and enabled specialists when affordable"
                        : attempt == 2
                            ? "B retry 2: normal staffing before speed, then enabled specialists; earned staff coins only"
                            : attempt == 3
                                ? levelIndex == 0 || levelIndex == 2
                                    ? "B retry 3: expand Parrilleros first; retain two Cocacoleros for Vélez; earned staff coins only"
                                    : "B retry 3: expand enabled worker roles before speed; earned staff coins only"
                                : attempt == 4
                                    ? "B retry 4: Mozo-first, then Normal/Premium cooks and drinks; hold speed until role targets are met"
                                    : attempt == 5
                                        ? "B retry 5: Coca-first then Mozo/cooks; hold speed until role targets are met"
                                        : attempt == 6
                                        ? "B retry 6: cook-first then Mozo/drinks; hold speed until role targets are met"
                                        : attempt == 7
                                            ? "B retry 7: service baseline, early speed, then expand affordable roles"
                                            : attempt == 8
                                                ? "B retry 8: resume authentic checkpoint, service baseline and early speed"
                                                : attempt == 9
                                                    ? "B retry 9: keep full Premium/service coverage; add Normal and Coca capacity before speed"
                                                    : attempt == 10
                                                        ? "B retry 10: match balanced service/speed baseline; add one Normal only with the next speed upgrade reserved"
                                                        : attempt == 11
                                                            ? "B retry 11: early speed 3; prioritize five Normal over a fourth Mozo; preserve Premium/Coca/Fernet coverage"
                                                            : attempt == 12
                                                                ? "B retry 12: balanced roster with an overflow Normal opportunistically bought before the final Mozo"
                                                                : attempt == 13
                                                                    ? "B retry 13: complete all balanced role targets and speed before hiring one overflow Normal"
                                                                    : attempt == 14
                                                                        ? "B retry 14: keep three Mozos and spend the saved fourth-Mozo cost on speed level 9"
                                                                        : attempt == 15
                                                                            ? "B retry 15: keep four Mozos and other specialists; skip second Fernetero to fund speed level 9"
                                                                            : attempt == 16
                                                                                ? "B retry 16: replay retry 12's opportunistic role purchase timing to verify the best result is reproducible"
                                                                                : "B retry 17: keep four Mozos and target five Normal, four Premium/Coca, one Fernetero; spend remaining staff coins on speed";
                    string marketShot = "";
                    long marketCentsStarting = (long)Get(market, "BalanceCents");
                    object marketData = Get(market, "Data");
                    long purchaseSpendBefore = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData);

                    if (Get(sim, "Phase").ToString() == "Lost")
                    {
                        yield return Click(InvokeViewRectNoArgs("RiotResultButtonBounds").center);
                        Assert.IsTrue((bool)Get(view, "levelSelectActive"), "Loss must return through the real result UI to levels.");
                        if (levelIndex >= 6) yield return Click(GetStaticRectField("LevelSelectNext").center);
                        yield return Click(InvokeStaticRect("LevelSelectCardBounds", levelIndex).center);
                        sim = Get(game, "Sim"); market = Get(game, "Market");
                        Assert.AreEqual(levelIndex, Get(sim, "LevelIndex"), "Retry must select the same unlocked level through its card.");
                        marketCentsStarting = (long)Get(market, "BalanceCents");
                        marketData = Get(market, "Data");
                        purchaseSpendBefore = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData);
                        if (levelIndex == 0) marketShot = "L01-B-attempt01-market.png"; // All Boys starts directly; retry has no separate Mercado screen.
                    }

                    if (Get(sim, "Phase").ToString() == "Ready")
                    {
                        Assert.IsTrue((bool)Get(view, "marketActive"), "The authentic Mercado must appear before this level starts.");
                        marketCentsStarting = (long)Get(market, "BalanceCents");
                        marketData = Get(market, "Data");
                        purchaseSpendBefore = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData);
                        marketShot = prefix + "market.png";
                        yield return Capture(marketShot);
                        long missingCost = (long)Call(market, "MinimumPurchaseCostCents", sim);
                        long balanceCents = (long)Get(market, "BalanceCents");
                        if (missingCost > balanceCents)
                        {
                            LevelAttemptReport blocked = BuildMarketBlockedReport(levelIndex, attempt, strategy, prefix,
                                marketShot, marketCentsStarting, purchaseSpendBefore, missingCost - balanceCents);
                            WriteProgress(blocked);
                            TestContext.WriteLine("Mode B stopped at cash-only Mercado gate: " + JsonUtility.ToJson(blocked, true));
                            stopCampaign = true;
                            break;
                        }
                        if (missingCost > 0)
                        {
                            yield return ClickMarket(InvokeMarketBuyMissingBounds().center);
                            decisionSequence.Add("market:buy-missing:" + missingCost.ToString());
                            yield return new WaitForSecondsRealtime(.35f);
                            yield return Capture(prefix + "market-restocked.png");
                        }
                        if (!(bool)Get(game, "CanStartRound"))
                        {
                            LevelAttemptReport blocked = BuildMarketBlockedReport(levelIndex, attempt, strategy, prefix,
                                marketShot, marketCentsStarting, purchaseSpendBefore, 0);
                            blocked.result = "BlockedByStockGate";
                            WriteProgress(blocked);
                            TestContext.WriteLine("Mode B stock gate remained closed after authorized-price Mercado purchase: " + JsonUtility.ToJson(blocked, true));
                            stopCampaign = true;
                            break;
                        }
                        yield return ClickMarket(InvokeViewRectNoArgs("MarketPlayBounds").center);
                    }

                    Assert.AreEqual("Playing", Get(sim, "Phase").ToString(), "Only the actual JUGAR PARTIDO interface may start a round.");
                    int staffCoinsStarting = (int)Get(sim, "Coins");
                    yield return Capture(prefix + "start.png");

                    float gameStart = (float)Get(sim, "Elapsed");
                    float wallStart = Time.realtimeSinceStartup;
                    float nextDecision = gameStart + 1f;
                    float nextObservation = 30f;
                    bool capturedActivity = false;
                    float resultDeadline = wallStart + (float)Get(sim, "TimeRemaining") + 120f;
                    while (Get(sim, "Phase").ToString() == "Playing" && Time.realtimeSinceStartup < resultDeadline)
                    {
                        float elapsed = (float)Get(sim, "Elapsed");
                        if (elapsed >= nextDecision)
                        {
                            int action = ChooseRealEconomyUiAction(sim, levelIndex, attempt);
                            if (action != 0)
                            {
                                int oldSpeed = (int)Get(sim, "SpeedLevel");
                                int oldCount = WorkerCount(sim, action);
                                decisionSequence.Add("at-game-second:" + elapsed.ToString("0.0") + ":" + RoleName(action));
                                yield return Click(InvokeViewRect("CatalogUpgradeTouchBounds", action).center);
                                Assert.IsTrue(action == 3 ? (int)Get(sim, "SpeedLevel") > oldSpeed : WorkerCount(sim, action) > oldCount,
                                    "Mode B purchase must succeed only through its visible live-game button.");
                            }
                            nextDecision = elapsed + (attempt >= 4 ? 1f : 6f);
                        }
                        if (elapsed >= nextObservation)
                        {
                            observations.Add(CaptureObservation(sim));
                            nextObservation += 30f;
                        }
                        if (!capturedActivity && elapsed >= 30f)
                        {
                            yield return Capture(prefix + "activity.png");
                            yield return Capture(prefix + "grill-workers.png");
                            capturedActivity = true;
                        }
                        yield return null;
                    }
                    Assert.AreNotEqual("Playing", Get(sim, "Phase").ToString(), "Native-time Mode B must reach the official result before its bounded safety deadline.");
                    if (!capturedActivity)
                    {
                        yield return Capture(prefix + "activity.png");
                        yield return Capture(prefix + "grill-workers.png");
                    }
                    yield return Capture(prefix + "result.png");
                    LevelAttemptReport report = BuildReport(levelIndex, attempt, strategy, prefix, marketShot,
                        staffCoinsStarting, marketCentsStarting, purchaseSpendBefore, 1f);
                    WriteProgress(report);
                    TestContext.WriteLine(JsonUtility.ToJson(report, true));

                    if (Get(sim, "Phase").ToString() == "Won")
                    {
                        levelWon = true;
                        if (levelIndex < 10)
                        {
                            yield return Click(InvokeViewRectNoArgs("VictoryExitBounds").center);
                            Assert.IsTrue((bool)Get(view, "marketActive"), "SIGUIENTE PARTIDO must advance only after a real victory and open the next Mercado.");
                            if (realEconomyMode && levelIndex == 4)
                            {
                                WriteAuthenticLevel6Checkpoint();
                                if (stopAfterLevel6Checkpoint) stopCampaign = true;
                            }
                        }
                    }
                    else if (attempt < maximumAttemptsForLevel)
                    {
                        yield return Click(InvokeViewRectNoArgs("RiotResultButtonBounds").center);
                        Assert.IsTrue((bool)Get(view, "levelSelectActive"), "A real defeat must use the game's level-return control before retry.");
                        if (levelIndex >= 6) yield return Click(GetStaticRectField("LevelSelectNext").center);
                        yield return Click(InvokeStaticRect("LevelSelectCardBounds", levelIndex).center);
                        attempt++;
                    }
                    else
                    {
                        stopCampaign = true; // Unlocks stay earned only; never force the next level open.
                    }
                }
                if (!stopCampaign && levelWon) levelIndex++;
                else if (!levelWon) stopCampaign = true;
            }
            Assert.Greater(memorySnapshots.Count, 0, "The campaign must persist only through the memory-only test delegate.");
        }

        private int ChooseRealEconomyUiAction(object sim, int levelIndex, int attempt)
        {
            if (attempt >= 17) return ChooseOneFernFiveNormalRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 16) return ChooseOpportunisticOverflowNormalRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 15) return ChooseOneFerneteroSpeedNineRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 14) return ChooseThreeMozoSpeedFocusRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 12) return ChooseBalancedOverflowNormalRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 11) return ChooseFiveNormalThreeMozoRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 10) return ChooseOneNormalWithSpeedReserveRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 9) return ChooseNormalAndCocaFocusRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 7) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);
            if (attempt >= 4) return ChooseExpandedRealEconomyUiAction(sim, levelIndex, attempt);
            bool staffFirst = attempt > 1;
            if (!staffFirst && (int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int normalTarget = attempt >= 3
                ? (levelIndex <= 2 ? 6 : (levelIndex >= 5 ? 6 : 4))
                : (levelIndex >= 5 ? 4 : 2);
            if ((int)Get(sim, "ParrilleroCount") < normalTarget && (bool)Get(sim, "CanHireParrillero")) return 2;
            bool wantsPremium = (int)Call(sim, "GetProductGoal", 1) > 0 || (int)Call(sim, "GetProductGoal", 2) > 0 || (int)Call(sim, "GetProductGoal", 3) > 0;
            int premiumTarget = attempt >= 3 ? 4 : 2;
            if (wantsPremium && (int)Get(sim, "ParrilleroPremiumCount") < premiumTarget && (bool)Get(sim, "CanHireParrilleroPremium")) return 20;
            bool wantsCoca = (int)Call(sim, "GetProductGoal", 4) > 0 || (int)Call(sim, "GetProductGoal", 6) > 0;
            int cocaTarget = attempt >= 3 && levelIndex != 2 ? 4 : 2;
            if (wantsCoca && (int)Get(sim, "CocacoleroCount") < cocaTarget && (bool)Get(sim, "CanHireCocacolero")) return 16;
            int ferneteroTarget = attempt >= 3 ? 2 : 1;
            if ((int)Call(sim, "GetProductGoal", 5) > 0 && (int)Get(sim, "FerneteroCount") < ferneteroTarget && (bool)Get(sim, "CanHireFernetero")) return 21;
            int mozoTarget = attempt >= 3 ? 4 : 2;
            if (levelIndex >= 5 && (int)Get(sim, "MozoCount") < mozoTarget && (bool)Get(sim, "CanHireMozo")) return 22;
            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseOneFernFiveNormalRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Trade the second Fernetero for one extra Normal cook, while retaining four
            // Mozos and every other specialist. This tests whether Racing's largest shortages
            // (Chori/Paty) improve without reducing the service team.
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 2, 16, 20, 21 }; // Four Mozos, five Normal, then full remaining specialists.
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 2 ? 5 : action == 21 ? 1 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseOpportunisticOverflowNormalRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Reproduce retry 12's measured ordering exactly: when a higher-priority role is
            // unaffordable, continue to the next affordable role rather than waiting. This
            // intentionally permits the fifth Normal to precede the fourth Mozo and verifies
            // whether the former best real-economy outcome is reproducible.
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 16, 2, 20, 21 };
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            if (WorkerCount(sim, 2) < 5 && (bool)Get(sim, "CanHireParrillero")) return 2;
            return 0;
        }

        private int ChooseFiveNormalThreeMozoRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Keep the genuine opening service baseline and three early speed upgrades, then
            // trade the fourth Mozo for one more Normal cook. Preserve all other specialists.
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 2, 20, 16, 21, 22 }; // Normal first, then Premium/Coca/Fernet, 3 Mozos max.
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 2 ? 5 : action == 20 || action == 16 ? 4 : action == 21 ? 2 : 3;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (rosterComplete && (int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseBalancedOverflowNormalRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Repeat the strongest measured balanced lineup. Only spend residual earned coins
            // on a fifth queued Normal cook after the normal speed-upgrade path is exhausted.
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 16, 2, 20, 21 };
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!rosterComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            if (WorkerCount(sim, 2) < 5 && (bool)Get(sim, "CanHireParrillero")) return 2;
            return 0;
        }

        private int ChooseOneFerneteroSpeedNineRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Fernet was 74/75 at the best balanced baseline; skip its second hire ($115)
            // while keeping four Mozos, then attempt the ninth speed upgrade ($750).
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 16, 2, 20, 21 };
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 21 ? 1 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!rosterComplete) return 0;
            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseThreeMozoSpeedFocusRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Trade the fourth Mozo's $450 late-tier cost for the ninth, $750 speed upgrade.
            // Keep the balanced four-post cook/drink coverage otherwise unchanged.
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 16, 2, 20, 21 };
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 22 ? 3 : action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!rosterComplete) return 0;
            if ((int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseOneNormalWithSpeedReserveRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            // Reuse the proven retry-7/8 purchase sequence. Only after the same service roster
            // and early speed are established may one extra Normal cook use surplus cash, and
            // only when the next speed upgrade remains affordable after that hire.
            int nextAction = ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);
            bool baselineComplete = WorkerCount(sim, 22) >= 4 && WorkerCount(sim, 16) >= 4 &&
                WorkerCount(sim, 2) >= 4 && WorkerCount(sim, 20) >= 4 && WorkerCount(sim, 21) >= 2;
            if (!baselineComplete || WorkerCount(sim, 2) >= 5 || !(bool)Get(sim, "CanHireParrillero"))
                return nextAction;

            int speedLevel = (int)Get(sim, "SpeedLevel");
            if (speedLevel >= 9) return 2;
            if (!(bool)Get(sim, "CanUpgradeSpeed")) return nextAction;
            int reserve = (int)Get(sim, "ParrilleroHireCost") + (int)Get(sim, "SpeedCost");
            return (int)Get(sim, "Coins") >= reserve ? 2 : nextAction;
        }

        private int ChooseNormalAndCocaFocusRealEconomyUiAction(object sim, int levelIndex)
        {
            if (levelIndex != 5) return ChooseEarlySpeedRealEconomyUiAction(sim, levelIndex);

            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 2, 16, 20, 21 }; // Mozo, Normal, Coca, Premium, Fernet.
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 2 ? 6 : action == 16 ? 5 : action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (rosterComplete && (int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private int ChooseEarlySpeedRealEconomyUiAction(object sim, int levelIndex)
        {
            int[] baselineRoles = { 22, 2, 20, 16, 21 };
            int[] baselineTargets = { 1, 2, 1, 1, 1 };
            bool baselineComplete = true;
            for (int i = 0; i < baselineRoles.Length; i++)
            {
                int action = baselineRoles[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action) || WorkerCount(sim, action) >= baselineTargets[i]) continue;
                baselineComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (!baselineComplete) return 0;

            if ((int)Get(sim, "SpeedLevel") < 3 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            int[] expansionOrder = { 22, 16, 2, 20, 21 };
            bool rosterComplete = true;
            for (int i = 0; i < expansionOrder.Length; i++)
            {
                int action = expansionOrder[i];
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            if (rosterComplete && (int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private void WriteAuthenticLevel6Checkpoint()
        {
            Assert.IsTrue(realEconomyMode, "Only Mode B can create a resumable checkpoint.");
            Assert.AreEqual(5, Get(game, "UnlockedLevel"));
            Assert.Greater(memorySnapshots.Count, 0, "Checkpoint must come from the in-memory persistence delegate.");
            string json = memorySnapshots[memorySnapshots.Count - 1];
            Type saveType = GameType.GetNestedType("SaveData", BindingFlags.Public);
            object save = JsonUtility.FromJson(json, saveType);
            Assert.AreEqual(5, saveType.GetField("unlockedLevel", PublicInstance).GetValue(save));
            object market = saveType.GetField("market", PublicInstance).GetValue(save);
            Assert.IsNotNull(market);
            Assert.IsFalse((bool)market.GetType().GetField("attemptActive", PublicInstance).GetValue(market),
                "A checkpoint cannot restore a round in progress.");
            File.WriteAllText(ModeBLevel6CheckpointPath, json);
        }

        private int ChooseExpandedRealEconomyUiAction(object sim, int levelIndex, int attempt)
        {
            bool rosterComplete = true;
            for (int slot = 0; slot < 5; slot++)
            {
                int action = ExpandedRetryRole(attempt, slot);
                if (!RealEconomyRoleRequired(sim, levelIndex, action)) continue;
                int target = action == 21 ? 2 : 4;
                if (WorkerCount(sim, action) >= target) continue;
                rosterComplete = false;
                if ((bool)Get(sim, CanHireProperty(action))) return action;
            }
            // Keep earned cash available for staff while a planned role is not yet affordable.
            // Buy speed only after this policy's complete, four-post/specialist roster is in place.
            if (rosterComplete && (int)Get(sim, "SpeedLevel") < 9 && (bool)Get(sim, "CanUpgradeSpeed")) return 3;
            return 0;
        }

        private static int ExpandedRetryRole(int attempt, int slot)
        {
            if (attempt == 4)
            {
                switch (slot) { case 0: return 22; case 1: return 2; case 2: return 20; case 3: return 16; default: return 21; }
            }
            if (attempt == 5)
            {
                switch (slot) { case 0: return 16; case 1: return 22; case 2: return 2; case 3: return 20; default: return 21; }
            }
            switch (slot) { case 0: return 20; case 1: return 2; case 2: return 22; case 3: return 16; default: return 21; }
        }

        private bool RealEconomyRoleRequired(object sim, int levelIndex, int action)
        {
            if (action == 2) return (int)Call(sim, "GetProductGoal", 0) > 0 || (int)Call(sim, "GetProductGoal", 1) > 0;
            if (action == 20) return (int)Call(sim, "GetProductGoal", 2) > 0 || (int)Call(sim, "GetProductGoal", 3) > 0;
            if (action == 16) return (int)Call(sim, "GetProductGoal", 4) > 0 || (int)Call(sim, "GetProductGoal", 6) > 0;
            if (action == 21) return (int)Call(sim, "GetProductGoal", 5) > 0;
            return action == 22 && levelIndex >= 5;
        }

        private static string CanHireProperty(int action)
        {
            if (action == 2) return "CanHireParrillero";
            if (action == 16) return "CanHireCocacolero";
            if (action == 20) return "CanHireParrilleroPremium";
            if (action == 21) return "CanHireFernetero";
            if (action == 22) return "CanHireMozo";
            throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown UI hiring action.");
        }

        private int WorkerCount(object sim, int action)
        {
            if (action == 2) return (int)Get(sim, "ParrilleroCount");
            if (action == 16) return (int)Get(sim, "CocacoleroCount");
            if (action == 20) return (int)Get(sim, "ParrilleroPremiumCount");
            if (action == 21) return (int)Get(sim, "FerneteroCount");
            if (action == 22) return (int)Get(sim, "MozoCount");
            return 0;
        }

        private IEnumerator HireRoleThroughUi(object sim, int action, string canHireProperty, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue((bool)Get(sim, canHireProperty), "Test balance should cover role action " + action + " hire " + (i + 1));
                decisionSequence.Add("hire:" + RoleName(action) + "#" + (i + 1).ToString());
                yield return Click(InvokeViewRect("CatalogUpgradeTouchBounds", action).center);
            }
        }

        private static string RoleName(int action)
        {
            if (action == 2) return "Parrillero";
            if (action == 16) return "Cocacolero";
            if (action == 20) return "ParrilleroPremium";
            if (action == 21) return "Fernetero";
            if (action == 22) return "Mozo";
            return "action-" + action.ToString();
        }

        private IEnumerator BuySpeedThroughUi(object sim, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue((bool)Get(sim, "CanUpgradeSpeed"), "Test balance should cover speed upgrade " + (i + 1));
                decisionSequence.Add("speed-level:" + Get(sim, "SpeedLevel").ToString() + "->" + (i + 1).ToString());
                yield return Click(InvokeViewRect("CatalogUpgradeTouchBounds", 3).center);
            }
        }

        private Delegate CreateMemoryPersistenceDelegate()
        {
            ParameterInfo[] parameters = GameType.GetMethod("ActivatePlayer", PublicInstance).GetParameters()[1]
                .ParameterType.GetMethod("Invoke").GetParameters();
            ParameterExpression json = Expression.Parameter(parameters[0].ParameterType, "json");
            ParameterExpression match = Expression.Parameter(parameters[1].ParameterType, "match");
            MethodInfo add = typeof(List<string>).GetMethod("Add");
            BlockExpression body = Expression.Block(
                Expression.Call(Expression.Constant(memorySnapshots), add, json),
                Expression.Constant(true));
            return Expression.Lambda(GameType.GetMethod("ActivatePlayer", PublicInstance).GetParameters()[1].ParameterType,
                body, json, match).Compile();
        }

        private static string CreateTestSaveJson()
        {
            Type saveType = GameType.GetNestedType("SaveData", BindingFlags.Public);
            object save = Activator.CreateInstance(saveType);
            saveType.GetField("version", PublicInstance).SetValue(save, 4);
            saveType.GetField("unlockedLevel", PublicInstance).SetValue(save, 10); // Test-only access; no real progression is loaded.
            FieldInfo marketField = saveType.GetField("market", PublicInstance);
            object market = Activator.CreateInstance(marketField.FieldType);
            market.GetType().GetField("balanceCents", PublicInstance).SetValue(market, 5_000_000L);
            market.GetType().GetField("kitGranted", PublicInstance).SetValue(market, true);
            market.GetType().GetField("initializedUnlockedLevel", PublicInstance).SetValue(market, 10);
            int[] openingStock = new int[10];
            openingStock[0] = 200; // Authentic Level 1 starter kit; all other stock must be bought in Mercado.
            openingStock[1] = 200;
            market.GetType().GetField("stockUnits", PublicInstance).SetValue(market, openingStock);
            marketField.SetValue(save, market);
            return JsonUtility.ToJson(save);
        }

        private static object Get(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, PublicInstance);
            if (property != null) return property.GetValue(target, null);
            FieldInfo field = target.GetType().GetField(name, PublicInstance | BindingFlags.NonPublic);
            if (field == null) throw new MissingMemberException(target.GetType().FullName, name);
            return field.GetValue(target);
        }

        private static object Call(object target, string name, params object[] args)
        {
            return target.GetType().GetMethod(name, PublicInstance).Invoke(target, args);
        }

        private IEnumerator WaitForMenu()
        {
            PropertyInfo property = ViewType.GetProperty("MenuAvailable", PrivateInstance);
            Assert.NotNull(property, "Intro readiness property moved; update the test locator rather than bypassing the UI.");
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!(bool)property.GetValue(view, null) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue((bool)property.GetValue(view, null), "Timed out waiting for the genuine intro animation/menu.");
        }

        private IEnumerator Click(Vector2 logicalPoint)
        {
            yield return ClickPixels(Pixel(logicalPoint.x, logicalPoint.y));
        }

        private IEnumerator ClickMarket(Vector2 marketPoint)
        {
            Rect viewport = (Rect)ViewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            float logicalHeight = (float)ViewType.GetMethod("CanvasLogicalHeight", StaticPrivate)
                .Invoke(null, new object[] { viewport });
            float verticalScale = logicalHeight / 960f;
            // Mercado hitboxes are already expressed in the full responsive canvas height;
            // unlike 960-base gameplay controls, do not apply verticalScale before rasterizing.
            Vector2 pixel = new Vector2(viewport.x + marketPoint.x * scale,
                Screen.height - (viewport.y + marketPoint.y * scale));
            Vector2 recovered = (Vector2)ViewType.GetMethod("ScreenToLayoutPoint", StaticPrivate)
                .Invoke(null, new object[] { pixel, new Vector2(Screen.width, Screen.height), viewport });
            Assert.AreEqual(marketPoint.x, recovered.x, .02f, "Mercado mouse X must round-trip through StreetView screen mapping.");
            Assert.AreEqual(marketPoint.y, recovered.y * verticalScale, .02f,
                "Mercado uses full-height hitboxes; the real pointer must map back after StreetView's verticalScale conversion.");
            TestContext.WriteLine("Market UI click at " + Screen.width + "x" + Screen.height +
                " logical=" + marketPoint + " pixel=" + pixel + " roundtrip=" + recovered);
            yield return ClickPixels(pixel);
        }

        private IEnumerator ClickPixels(Vector2 pixel)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel });
            yield return null;
            yield return null;
        }

        private Vector2 Pixel(float x, float y)
        {
            Rect viewport = (Rect)ViewType.GetMethod("CanvasViewport", StaticPrivate)
                .Invoke(null, new object[] { new Vector2(Screen.width, Screen.height), Screen.safeArea });
            float scale = viewport.width / 540f;
            float logicalHeight = (float)ViewType.GetMethod("CanvasLogicalHeight", StaticPrivate).Invoke(null, new object[] { viewport });
            float verticalScale = logicalHeight / 960f;
            return new Vector2(viewport.x + x * scale,
                Screen.height - (viewport.y + y * verticalScale * scale));
        }

        private Rect InvokeViewRect(string method, int argument)
        {
            MethodInfo info = ViewType.GetMethod(method, PrivateInstance);
            Assert.NotNull(info, "UI hit target locator missing: " + method);
            return (Rect)info.Invoke(view, new object[] { argument });
        }

        private static Rect InvokeStaticRect(string method, int argument)
        {
            MethodInfo info = ViewType.GetMethod(method, StaticPrivate);
            Assert.NotNull(info, "UI hit target locator missing: " + method);
            return (Rect)info.Invoke(null, new object[] { argument });
        }

        private Rect InvokeViewRectNoArgs(string method)
        {
            MethodInfo info = ViewType.GetMethod(method, PrivateInstance);
            Assert.NotNull(info, "UI hit target locator missing: " + method);
            return (Rect)info.Invoke(view, null);
        }

        private static Rect InvokeStaticRectNoArgs(string method)
        {
            MethodInfo info = ViewType.GetMethod(method, StaticPrivate);
            Assert.NotNull(info, "UI hit target locator missing: " + method);
            return (Rect)info.Invoke(null, null);
        }

        private static Rect GetStaticRectField(string name)
        {
            FieldInfo info = ViewType.GetField(name, StaticPrivate);
            Assert.NotNull(info, "UI hit target locator missing: " + name);
            return (Rect)info.GetValue(null);
        }

        private Rect InvokeMarketBuyMissingBounds()
        {
            float logicalHeight = (float)Get(view, "logicalCanvasHeight");
            Assert.Greater(logicalHeight, 0f, "Wait for responsive UI geometry before clicking Mercado.");
            MethodInfo info = ViewType.GetMethod("MarketBuyMissingBounds", StaticPrivate);
            Assert.NotNull(info, "Market purchase button locator missing.");
            return (Rect)info.Invoke(null, new object[] { logicalHeight });
        }

        private CampaignObservation CaptureObservation(object sim)
        {
            int[] delivered = new int[7];
            for (int product = 0; product < 7; product++) delivered[product] = (int)Call(sim, "GetProductDelivered", product);
            return new CampaignObservation
            {
                elapsedGameSeconds = (float)Get(sim, "Elapsed"),
                remainingGameSeconds = (float)Get(sim, "TimeRemaining"),
                deliveredByProduct = delivered,
                staffCoins = (int)Get(sim, "Coins"),
                workerStates = WorkerStateSummary(sim)
            };
        }

        private LevelAttemptReport BuildReport(int levelIndex, int attempt, string strategy, string prefix,
            string marketShot, int startingStaffCoins, long startingMarketCents, long purchaseSpendBefore,
            float playedAtTimeScale)
        {
            object sim = Get(game, "Sim"), market = Get(game, "Market"), balance = Get(game, "Balance");
            object marketData = Get(market, "Data");
            object settlement = Get(market, "LastResult");
            int[] goals = new int[7], delivered = new int[7];
            for (int product = 0; product < 7; product++)
            {
                goals[product] = (int)Call(sim, "GetProductGoal", product);
                delivered[product] = (int)Call(sim, "GetProductDelivered", product);
            }
            int[] closingStock = (int[])Get(settlement, "remainingUnits");
            int[] consumedStock = (int[])Get(settlement, "consumedUnits");
            object[] names = (object[])SimulationType.GetField("LevelDisplayNames", StaticField).GetValue(null);
            var screenshots = new List<string>();
            AddCaptureIfPresent(screenshots, marketShot);
            AddCaptureIfPresent(screenshots, prefix + "market-restocked.png");
            AddCaptureIfPresent(screenshots, prefix + "start.png");
            if (!realEconomyMode) AddCaptureIfPresent(screenshots, prefix + "workers.png");
            AddCaptureIfPresent(screenshots, prefix + "activity.png");
            AddCaptureIfPresent(screenshots, prefix + "grill-workers.png");
            AddCaptureIfPresent(screenshots, prefix + "result.png");
            return new LevelAttemptReport
            {
                mode = realEconomyMode ? "B-real-economy" : "A-unrestricted-test-only",
                captureMode = Application.isBatchMode ? "batchmode-no-gameview-capture" : "gameview-png", matchStarted = true,
                level = levelIndex + 1, attempt = attempt, club = names[levelIndex].ToString(),
                strategy = strategy, unityVersion = Application.unityVersion, seed = (int)Get(balance, "randomSeed"),
                gameTimeScale = playedAtTimeScale,
                result = Get(sim, "Phase").ToString(), elapsedGameSeconds = (float)Get(sim, "Elapsed"),
                remainingGameSeconds = (float)Get(sim, "TimeRemaining"), goalsByProduct = goals, deliveredByProduct = delivered,
                closingIngredientStock = (int[])closingStock.Clone(), consumedIngredientUnits = (int[])consumedStock.Clone(),
                parrilleros = (int)Get(sim, "ParrilleroCount"),
                cocacoleros = (int)Get(sim, "CocacoleroCount"), premiumParrilleros = (int)Get(sim, "ParrilleroPremiumCount"),
                ferneteros = (int)Get(sim, "FerneteroCount"), mozos = (int)Get(sim, "MozoCount"), speedLevel = (int)Get(sim, "SpeedLevel"),
                staffCoinsStarting = startingStaffCoins, staffCoinsEarned = (int)Get(sim, "CoinsEarned"),
                staffCoinsRemaining = (int)Get(sim, "Coins"),
                staffCoinsSpent = startingStaffCoins + (int)Get(sim, "CoinsEarned") - (int)Get(sim, "Coins"),
                marketCentsStarting = startingMarketCents,
                marketCentsSpent = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData) - purchaseSpendBefore,
                marketCentsRemaining = (long)Get(market, "BalanceCents"), customersLost = (int)Get(sim, "CustomersLost"),
                customersAttended = Convert.ToInt64(Get(sim, "CustomersAttended")),
                ordersCompleted = Convert.ToInt64(Get(sim, "OrdersCompleted")),
                marketGrossSalesCents = Convert.ToInt64(Get(settlement, "grossSalesCents")),
                marketEarnedCents = Convert.ToInt64(Get(settlement, "earnedCents")),
                actionSequence = decisionSequence.ToArray(), progressObservations = observations.ToArray(),
                screenshots = screenshots.ToArray(), workerStates = WorkerStateSummary(sim),
                persistenceSnapshotsInMemory = memorySnapshots.Count
            };
        }

        private static void AddCaptureIfPresent(List<string> screenshots, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(OutputDirectory, fileName)))
                screenshots.Add(fileName);
        }

        private LevelAttemptReport BuildMarketBlockedReport(int levelIndex, int attempt, string strategy, string prefix,
            string marketShot, long startingMarketCents, long purchaseSpendBefore, long shortfallCents)
        {
            object sim = Get(game, "Sim"), market = Get(game, "Market"), marketData = Get(market, "Data");
            object[] names = (object[])SimulationType.GetField("LevelDisplayNames", StaticField).GetValue(null);
            int[] goals = new int[7], delivered = new int[7], remainingStock = new int[10];
            for (int product = 0; product < 7; product++)
            {
                goals[product] = (int)Call(sim, "GetProductGoal", product);
                delivered[product] = (int)Call(sim, "GetProductDelivered", product);
            }
            for (int ingredient = 0; ingredient < 10; ingredient++)
                remainingStock[ingredient] = (int)Call(market, "Units", ingredient);
            long spent = (long)marketData.GetType().GetField("cumulativePurchaseSpendCents", PublicInstance).GetValue(marketData) - purchaseSpendBefore;
            return new LevelAttemptReport
            {
                mode = "B-real-economy", captureMode = Application.isBatchMode ? "batchmode-no-gameview-capture" : "gameview-png",
                matchStarted = false, level = levelIndex + 1, attempt = attempt,
                club = names[levelIndex].ToString(), strategy = strategy, unityVersion = Application.unityVersion,
                seed = (int)Get(Get(game, "Balance"), "randomSeed"), result = "BlockedBeforeStart",
                elapsedGameSeconds = 0f, remainingGameSeconds = (float)Get(sim, "TimeRemaining"),
                goalsByProduct = goals, deliveredByProduct = delivered,
                closingIngredientStock = remainingStock, consumedIngredientUnits = new int[10],
                parrilleros = (int)Get(sim, "ParrilleroCount"), cocacoleros = (int)Get(sim, "CocacoleroCount"),
                premiumParrilleros = (int)Get(sim, "ParrilleroPremiumCount"), ferneteros = (int)Get(sim, "FerneteroCount"),
                mozos = (int)Get(sim, "MozoCount"), speedLevel = (int)Get(sim, "SpeedLevel"),
                staffCoinsStarting = 0, staffCoinsRemaining = (int)Get(sim, "Coins"), staffCoinsSpent = 0,
                marketCentsStarting = startingMarketCents, marketCentsSpent = spent,
                marketCentsRemaining = (long)Get(market, "BalanceCents"), marketShortfallCents = shortfallCents,
                customersLost = 0, customersAttended = 0, ordersCompleted = 0,
                marketGrossSalesCents = 0, marketEarnedCents = 0,
                actionSequence = decisionSequence.ToArray(), progressObservations = observations.ToArray(),
                screenshots = new[] { marketShot }, workerStates = WorkerStateSummary(sim),
                persistenceSnapshotsInMemory = memorySnapshots.Count, gameTimeScale = 1f
            };
        }

        private static string WorkerStateSummary(object sim)
        {
            var counts = new Dictionary<string, int>();
            foreach (object worker in (IEnumerable)Get(sim, "Workers"))
            {
                string state = "P" + Get(worker, "Product").ToString() + ":" + Get(worker, "State").ToString();
                counts[state] = counts.TryGetValue(state, out int count) ? count + 1 : 1;
            }
            var summary = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts) summary.Add(pair.Key + ":" + pair.Value);
            return string.Join(";", summary.ToArray());
        }

        private static void WriteProgress(LevelAttemptReport report)
        {
            string modeCode = report.mode == "B-real-economy" ? "B" : "A";
            string attemptCode = "attempt" + Math.Max(1, report.attempt).ToString("00");
            string baseName = "L" + report.level.ToString("00") + "-" + modeCode + "-" + attemptCode;
            File.WriteAllText(Path.Combine(OutputDirectory, baseName + ".json"), JsonUtility.ToJson(report, true));
            WriteCsvProgress();
            WriteMarkdownProgress();
        }

        private static void WriteCsvProgress()
        {
            string csvPath = Path.Combine(OutputDirectory, "attempts.csv");
            var csvLines = new List<string>();
            csvLines.Add("mode,level,attempt,club,strategy,seed,result,matchStarted,elapsedGameSeconds,remainingGameSeconds,goalsByProduct,deliveredByProduct,parrilleros,cocacoleros,premiumParrilleros,ferneteros,mozos,speedLevel,staffCoinsStarting,staffCoinsSpent,staffCoinsRemaining,marketCentsStarting,marketCentsSpent,marketCentsRemaining,marketShortfallCents,marketGrossSalesCents,marketEarnedCents,customersAttended,customersLost,ordersCompleted,consumedIngredientUnits,closingIngredientStock,workerStates,actionSequence");
            var reports = new List<LevelAttemptReport>();
            foreach (string file in Directory.GetFiles(OutputDirectory, "L*-*-attempt*.json"))
            {
                try { reports.Add(JsonUtility.FromJson<LevelAttemptReport>(File.ReadAllText(file))); }
                catch { /* Ignore partial data from an interrupted run. */ }
            }
            reports.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : a.attempt.CompareTo(b.attempt));
            foreach (LevelAttemptReport item in reports)
            {
                csvLines.Add(string.Join(",", new[]
                {
                    item.mode, item.level.ToString(), "attempt" + Math.Max(1, item.attempt).ToString("00"), Csv(item.club), Csv(item.strategy),
                    item.seed.ToString(), item.result, (item.matchStarted || item.result == "Won" || item.result == "Lost").ToString(),
                    item.elapsedGameSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.remainingGameSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    Csv(string.Join(";", item.goalsByProduct ?? new int[0])), Csv(string.Join(";", item.deliveredByProduct ?? new int[0])),
                    item.parrilleros.ToString(), item.cocacoleros.ToString(), item.premiumParrilleros.ToString(), item.ferneteros.ToString(),
                    item.mozos.ToString(), item.speedLevel.ToString(), item.staffCoinsStarting.ToString(), item.staffCoinsSpent.ToString(),
                    item.staffCoinsRemaining.ToString(), item.marketCentsStarting.ToString(), item.marketCentsSpent.ToString(),
                    item.marketCentsRemaining.ToString(), item.marketShortfallCents.ToString(), item.marketGrossSalesCents.ToString(), item.marketEarnedCents.ToString(),
                    item.customersAttended.ToString(), item.customersLost.ToString(), item.ordersCompleted.ToString(),
                    Csv(string.Join(";", item.consumedIngredientUnits ?? new int[0])), Csv(string.Join(";", item.closingIngredientStock ?? new int[0])),
                    Csv(item.workerStates), Csv(string.Join(";", item.actionSequence ?? new string[0]))
                }));
            }
            File.WriteAllLines(csvPath, csvLines.ToArray());
        }

        private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

        private static void WriteMarkdownProgress()
        {
            var reports = new List<LevelAttemptReport>();
            foreach (string file in Directory.GetFiles(OutputDirectory, "L*-*-attempt*.json"))
            {
                try { reports.Add(JsonUtility.FromJson<LevelAttemptReport>(File.ReadAllText(file))); }
                catch { /* Ignore incomplete JSON from an interrupted test run. */ }
            }
            reports.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : a.attempt.CompareTo(b.attempt));
            var text = new System.Text.StringBuilder();
            text.AppendLine("# Campaña autónoma de Hay Chori y Paty — avance");
            text.AppendLine();
            text.AppendLine("Evidencia PlayMode real mediante menú, selector, Mercado, botones y eventos de mouse en `StreetView.Update`. El guardado se mantiene en memoria; Firebase y PlayerPrefs no intervienen. Modalidad A financia solo su fixture aislada con $50.000 para contratación y $50.000 para Mercado. Modalidad B usa save fresco, saldos/costos reales y Time.timeScale=1; nunca fuerza desbloqueos ni compra si no alcanza el dinero. La mayoría de intentos A usan Time.timeScale=8 para explorar una estrategia; los controles nativos de L10/L11 usan 1. Precios, stock inicial, producción, metas y duración oficial no cambian.");
            text.AppendLine();
            text.AppendLine("| Nivel | Intentos A | Victorias A | Derrotas A | Estrategia/resultado | Estado B |");
            text.AppendLine("|---|---:|---:|---:|---|---|");
            object[] clubs = (object[])SimulationType.GetField("LevelDisplayNames", StaticField).GetValue(null);
            for (int level = 1; level <= 11; level++)
            {
                var matches = new List<LevelAttemptReport>();
                foreach (LevelAttemptReport item in reports)
                    if (item.level == level && item.mode == "A-unrestricted-test-only") matches.Add(item);
                int wins = 0, losses = 0;
                foreach (LevelAttemptReport item in matches) { if (item.result == "Won") wins++; if (item.result == "Lost") losses++; }
                string best = matches.Count == 0 ? "—" : matches[matches.Count - 1].strategy + " — " + matches[matches.Count - 1].result;
                LevelAttemptReport b = null;
                foreach (LevelAttemptReport item in reports)
                    if (item.level == level && item.mode == "B-real-economy" && (b == null || item.attempt >= b.attempt)) b = item;
                string modeB = b == null ? "Pendiente" : b.result == "BlockedBeforeStart" || b.result == "BlockedByStockGate"
                    ? "Bloqueado antes del partido (faltan " + (b.marketShortfallCents / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ")"
                    : b.result + " · intento " + b.attempt;
                text.AppendLine("| " + level + " · " + clubs[level - 1] + " | " + matches.Count + " | " + wins + " | " + losses + " | " + best + " | " + modeB + " |");
            }
            text.AppendLine();
            text.AppendLine("## Cobertura / límites");
            text.AppendLine("- JSON/CSV conserva objetivos/entregas por ID, seed, equipo, mejora, saldos, compras, ventas/bono, déficit de mercado, stock consumido/restante, clientes atendidos/perdidos, pedidos completados, secuencia de acciones, trazas y Time.timeScale.");
            text.AppendLine("- Las derrotas son resultados válidos; no se cuenta una simulación interna como partida UI.");
            text.AppendLine("- Cobertura A actual es una exploración inicial de estrategias, no una búsqueda exhaustiva de 30. B avanza por victorias reales y puede reanudarse desde un checkpoint generado solo por una victoria UI auténtica; no altera la economía real.");
            File.WriteAllText(Path.Combine(OutputDirectory, "progress.md"), text.ToString());
        }

        private IEnumerator Capture(string fileName)
        {
            if (Application.isBatchMode)
            {
                // Batchmode supports the real pointer/update path but exposes no native Game View
                // framebuffer to ScreenCapture. Keep structured outcome evidence, not fake PNG references.
                yield return null;
                yield return null;
                yield break;
            }

            yield return new WaitForEndOfFrame();
            string path = Path.Combine(OutputDirectory, fileName);
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(File.Exists(path), "Unity Game View capture was not written: " + path);
        }

        [Serializable]
        private sealed class LevelAttemptReport
        {
            public string mode, captureMode, club, strategy, unityVersion, result, workerStates;
            public int level, attempt, seed, parrilleros, cocacoleros, premiumParrilleros, ferneteros, mozos, speedLevel;
            public bool matchStarted;
            public float gameTimeScale;
            public int staffCoinsStarting, staffCoinsEarned, staffCoinsRemaining, staffCoinsSpent, customersLost, persistenceSnapshotsInMemory;
            public long customersAttended, ordersCompleted, marketGrossSalesCents, marketEarnedCents;
            public long marketCentsStarting, marketCentsSpent, marketCentsRemaining, marketShortfallCents;
            public float elapsedGameSeconds, remainingGameSeconds;
            public int[] goalsByProduct, deliveredByProduct, consumedIngredientUnits, closingIngredientStock;
            public string[] screenshots, actionSequence;
            public CampaignObservation[] progressObservations;
        }

        [Serializable]
        private sealed class CampaignObservation
        {
            public float elapsedGameSeconds, remainingGameSeconds;
            public int staffCoins;
            public int[] deliveredByProduct;
            public string workerStates;
        }
    }
}
