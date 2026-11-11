using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Huracán regressions use reflection to preserve the test asmdef's runtime boundary.</summary>
    public sealed class StreetBonusTrackTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private Type simType, balanceType, stateType, gameType, saveType, ticketType, matchType, roleType, phaseType;
        private readonly List<GameObject> hosts = new List<GameObject>();
        private bool oldEditorInitialization, oldStartupGrant;
        private string latestJson;
        private object lastMatch;

        [SetUp]
        public void SetUp()
        {
            simType = Runtime("StreetSimulation");
            balanceType = Runtime("StreetBalance");
            stateType = Runtime("StreetBonusTrackState");
            gameType = Runtime("StreetGame");
            saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            ticketType = Runtime("StreetTicketData");
            matchType = Runtime("StreetMatchRecord");
            roleType = Runtime("StreetWorkerRole");
            phaseType = Runtime("RoundPhase");
            latestJson = null;
            lastMatch = null;
            FieldInfo editorInitialization = gameType.GetField("AllowEditorTestInitialization", AnyStatic);
            oldEditorInitialization = (bool)editorInitialization.GetValue(null);
            editorInitialization.SetValue(null, false);
            FieldInfo startupGrant = gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", AnyStatic);
            oldStartupGrant = (bool)startupGrant.GetValue(null);
            startupGrant.SetValue(null, false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in hosts) if (host != null) UnityEngine.Object.DestroyImmediate(host);
            hosts.Clear();
            gameType.GetField("AllowEditorTestInitialization", AnyStatic).SetValue(null, oldEditorInitialization);
            gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", AnyStatic).SetValue(null, oldStartupGrant);
        }

        [Test]
        public void ReorganizedCampaignOrderKeepsAllStableIdsAndBothDefenseModes()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 9, 2, 12, 3, 11, 10, 13, 14, 4, 5, 6, 7, 8 },
                (int[])simType.GetField("CampaignOrder", BindingFlags.Public | BindingFlags.Static).GetValue(null));
            Assert.AreEqual(15, ((string[])simType.GetField("LevelNames", BindingFlags.Public | BindingFlags.Static).GetValue(null)).Length);
            Assert.AreEqual(9, CallStatic(simType, "CampaignLevelAt", 2));
            Assert.AreEqual(12, CallStatic(simType, "NextCampaignLevelId", 2));
            Assert.AreEqual(11, CallStatic(simType, "NextCampaignLevelId", 3));
            Assert.AreEqual(10, CallStatic(simType, "NextCampaignLevelId", 11));
            Assert.AreEqual(13, CallStatic(simType, "NextCampaignLevelId", 10));
            Assert.AreEqual(4, CallStatic(simType, "NextCampaignLevelId", 13));
            Assert.AreEqual(6, CallStatic(simType, "NextCampaignLevelId", 5));
            Assert.AreEqual(3, CallStatic(simType, "NextCampaignLevelId", 12));
            Assert.AreEqual(-1, CallStatic(simType, "NextCampaignLevelId", 8));
            Assert.IsTrue((bool)CallStatic(simType, "IsTowerDefenseLevel", 9));
            Assert.IsTrue((bool)CallStatic(simType, "IsTowerDefenseLevel", 10));
            Assert.IsTrue((bool)CallStatic(simType, "IsTowerDefenseLevel", 13));
            Assert.IsFalse((bool)CallStatic(simType, "IsTowerDefenseLevel", 11));
        }

        [Test]
        public void LegacyV4RacingProgressAlsoUnlocksHuracanWithoutMovingTheFrontier()
        {
            object legacy = Activator.CreateInstance(saveType);
            Put(legacy, "version", 4);
            Put(legacy, "unlockedLevel", 7);
            object migrated = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { legacy });

            Assert.AreEqual(7, Read(migrated, "version"));
            Assert.AreEqual(7, Read(migrated, "unlockedLevel"));
            Assert.IsTrue((bool)Read(migrated, "legacyRacingUnlocked"));
            Assert.IsTrue((bool)Read(migrated, "huracanUnlocked"));
            Assert.IsFalse((bool)Read(migrated, "racingUnlockedByHuracan"));
            Assert.NotNull(Read(migrated, "bonusTrack"));
        }

        [Test]
        public void LegacyV5UnlocksArgentinosOnlyWithAnActualLosRedondosWin()
        {
            object frontierOnly = Activator.CreateInstance(saveType);
            Put(frontierOnly, "version", 5);
            Put(frontierOnly, "unlockedLevel", 10);
            object migratedFrontierOnly = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { frontierOnly });
            Assert.AreEqual(7, Read(migratedFrontierOnly, "version"));
            Assert.IsFalse((bool)Read(migratedFrontierOnly, "argentinosUnlocked"),
                "The legacy frontier means Los Redondos is available; it does not prove a win there.");
            Assert.AreEqual(10, Read(migratedFrontierOnly, "unlockedLevel"));

            object wonSave = Activator.CreateInstance(saveType);
            Put(wonSave, "version", 5);
            Put(wonSave, "unlockedLevel", 10);
            object statistics = Activator.CreateInstance(Runtime("StreetPlayerStatistics"));
            Put(statistics, "winsByLevel", new long[12]);
            ((long[])Read(statistics, "winsByLevel"))[10] = 1;
            Put(wonSave, "statistics", statistics);
            object migratedWonSave = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { wonSave });
            Assert.IsTrue((bool)Read(migratedWonSave, "argentinosUnlocked"));
            Assert.AreEqual(10, Read(migratedWonSave, "unlockedLevel"), "Legacy stable-ID frontier stays capped at Los Redondos.");
        }

        [Test]
        public void SaveV6MigrationPreservesExactLegacyIdUnlocksAndSeparatesTraditionalLosRedondosRecords()
        {
            object legacy = Activator.CreateInstance(saveType);
            Put(legacy, "version", 6);
            Put(legacy, "unlockedLevel", 2);
            object statistics = Activator.CreateInstance(Runtime("StreetPlayerStatistics"));
            long[] wins = new long[13]; wins[10] = 3;
            Put(statistics, "winsByLevel", wins);
            Put(statistics, "lossesByLevel", new long[13]);
            Put(statistics, "bestGrossCentsByLevel", new long[13]);
            Put(statistics, "bestUnitsSoldByLevel", new long[13]);
            Put(statistics, "bestWinTimeByLevel", new float[13]);
            Put(legacy, "statistics", statistics);

            object migrated = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { legacy });
            bool[] unlocked = (bool[])Read(migrated, "unlockedLevelIds");
            Assert.AreEqual(7, Read(migrated, "version"));
            CollectionAssert.AreEqual(new[] { true, true, true, false, false, false, false, false, false, false, false, false, false }, unlocked);
            object migratedStats = Read(migrated, "statistics");
            Assert.AreEqual(3L, Read(migratedStats, "legacyTraditionalLosRedondosWins"));
            Assert.AreEqual(0L, ((long[])Read(migratedStats, "winsByLevel"))[10]);

            object migratedAgain = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { migrated });
            CollectionAssert.AreEqual(unlocked, (bool[])Read(migratedAgain, "unlockedLevelIds"));
            Assert.AreEqual(3L, Read(Read(migratedAgain, "statistics"), "legacyTraditionalLosRedondosWins"));
        }

        [Test]
        public void ArgentinosUsesAnIndependentUnlockBitAndBocaIsTheCampaignTerminal()
        {
            object save = Activator.CreateInstance(saveType);
            Put(save, "version", 6);
            Put(save, "unlockedLevel", 10);
            Put(save, "argentinosUnlocked", false);
            object tickets = Activator.CreateInstance(ticketType);
            Put(tickets, "migrationApplied", true);
            Put(save, "tickets", tickets);
            object game = CreateGame(JsonUtility.ToJson(save));

            Assert.IsTrue((bool)Call(game, "IsLevelUnlocked", 10));
            Assert.IsFalse((bool)Call(game, "IsLevelUnlocked", 12));
            object liveSave = Read(game, "save");
            ((bool[])Read(liveSave, "unlockedLevelIds"))[12] = true;
            Assert.IsTrue((bool)Call(game, "IsLevelUnlocked", 12));
            Assert.AreEqual(10, Read(liveSave, "unlockedLevel"), "Unlocking ID12 must not move the legacy stable-ID frontier.");
            Assert.AreEqual(3, CallStatic(simType, "NextCampaignLevelId", 12));
            Assert.AreEqual(-1, CallStatic(simType, "NextCampaignLevelId", 8));
        }

        [Test]
        public void BonusStartsWithFreeTeamAndPausesAtFiveExactChoiceDeadlines()
        {
            object sim = NewSim(11);
            Assert.IsTrue((bool)Call(sim, "StartBonusRound", 4097));
            Assert.AreEqual(2, Read(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Read(sim, "CocacoleroCount"));
            Assert.AreEqual(1, Read(sim, "MozoCount"));
            Assert.AreEqual(0, Read(sim, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, Read(sim, "FerneteroCount"));
            Assert.AreEqual(0, Read(sim, "Coins"));
            Assert.IsFalse((bool)Read(sim, "GoalReached"));

            for (int choice = 0; choice < 5; choice++)
            {
                float deadline = (choice + 1) * 20f;
                AdvanceTo(sim, deadline);
                object state = Read(sim, "BonusTrack");
                Assert.AreEqual(deadline, (float)Read(sim, "Elapsed"), .0001f, "Choice " + (choice + 1) + " must land on its exact deadline.");
                Assert.IsTrue((bool)Read(state, "SelectionPending"));
                Assert.AreEqual(3, ((ICollection)Read(state, "CurrentCards")).Count);
                Assert.AreEqual(choice == 2, (bool)Read(state, "IsSuperBonusPending"));
                Call(sim, "Step", 6f);
                Assert.AreEqual(deadline, (float)Read(sim, "Elapsed"), .0001f, "Effective time must stop while the card is open.");
                Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                state = Read(sim, "BonusTrack");
                if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                Assert.IsFalse((bool)Read(state, "SelectionPending"));
                Assert.AreEqual(choice + 1, Read(state, "ChoiceCount"));
            }

            AdvanceTo(sim, 120f);
            Assert.AreEqual("Won", Read(sim, "Phase").ToString());
            Assert.AreEqual(120f, (float)Read(sim, "Elapsed"), .0001f);
        }

        [Test]
        public void BonusChoiceCardsSitBelowHuracanCaptionAbovePennantsAndHitTheirWholeCard()
        {
            object sim = NewSim(11);
            Assert.IsTrue((bool)Call(sim, "StartBonusRound", 4097));
            AdvanceTo(sim, 20f);
            Assert.IsTrue((bool)Read(Read(sim, "BonusTrack"), "SelectionPending"));

            Type viewType = Runtime("StreetView");
            MethodInfo build = viewType.GetMethod("BuildResponsiveFooterLayout", AnyStatic);
            Assert.NotNull(build);
            const float height = 1360f; // 610×1536 portrait viewport expressed in the 540-wide logical canvas.
            float verticalScale = height / 960f;
            object footer = build.Invoke(null, new object[] { sim, height, false, false });
            Rect caption = (Rect)Read(footer, "LevelBadge");
            Rect pennants = (Rect)Read(footer, "Pennants");
            Rect[] buttons = (Rect[])Read(footer, "BonusChoiceBounds");
            Assert.AreEqual(3, buttons.Length);
            Assert.Greater(pennants.height, 1f, "Huracán keeps a visible banderín strip at the supplied tall portrait height.");

            for (int i = 0; i < buttons.Length; i++)
            {
                Assert.Greater(buttons[i].yMin, caption.yMax * verticalScale,
                    "Choice cards must be below the club/neighborhood caption.");
                Assert.Less(buttons[i].yMax, pennants.yMin,
                    "Choice cards must stay above the pennants.");
                Assert.AreEqual(156f, buttons[i].width, .01f, "The whole upgrade card is tappable; there is no smaller ELEGIR button.");
                Assert.AreEqual(120f, buttons[i].height, .01f);
                float expectedCardCenter = (540f - (3f * 156f + 2f * 9f)) * .5f + i * 165f + 78f;
                Assert.AreEqual(expectedCardCenter, buttons[i].center.x, .01f,
                    "Each footer choice card stays in its matching column.");
            }

            GameObject viewHost = new GameObject("Huracán bonus choice pointer geometry test");
            viewHost.SetActive(false);
            hosts.Add(viewHost);
            Component game = viewHost.AddComponent(gameType);
            gameType.GetField("sim", PrivateInstance).SetValue(game, sim);
            gameType.GetField("playerActive", PrivateInstance).SetValue(game, true);
            Component view = viewHost.AddComponent(viewType);
            viewType.GetField("game", PrivateInstance).SetValue(view, game);
            viewType.GetField("logicalCanvasHeight", PrivateInstance).SetValue(view, height);
            viewType.GetField("layoutVerticalScale", PrivateInstance).SetValue(view, verticalScale);
            MethodInfo hitAction = viewType.GetMethod("HitAction", PrivateInstance);
            stateType.GetField("aguanteReady", AnyInstance).SetValue(Read(sim, "BonusTrack"), true);
            Vector2 aguantePointer = new Vector2(449f, 97f / verticalScale);
            Assert.AreEqual(0, (int)hitAction.Invoke(view, new object[] { aguantePointer }),
                "Aguante must not compete with the pending one-card choice.");
            for (int i = 0; i < buttons.Length; i++)
            {
                Vector2[] samples = {
                    buttons[i].center,
                    new Vector2(buttons[i].xMin + 1f, buttons[i].yMin + 1f),
                    new Vector2(buttons[i].xMax - 1f, buttons[i].yMax - 1f)
                };
                for (int sample = 0; sample < samples.Length; sample++)
                {
                    Vector2 pointer = new Vector2(samples[sample].x, samples[sample].y / verticalScale);
                    Assert.AreEqual(5000 + i, (int)hitAction.Invoke(view, new object[] { pointer }),
                        "The whole visible footer card must select that card, without a smaller secondary button.");
                }
            }
        }

        [Test]
        public void EmergencyRoleChoicesAlsoUseDirectFooterCardsAndMatchingHitActions()
        {
            object sim = null;
            int emergencyCardIndex = -1;
            for (int seed = 1; seed <= 512 && emergencyCardIndex < 0; seed++)
            {
                object candidate = NewSim(11);
                Assert.IsTrue((bool)Call(candidate, "StartBonusRound", seed));
                AdvanceTo(candidate, 20f);
                IList cards = (IList)Read(Read(candidate, "BonusTrack"), "CurrentCards");
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i].ToString() == "RefuerzoDeEmergencia")
                    {
                        sim = candidate;
                        emergencyCardIndex = i;
                        break;
                    }
            }
            Assert.NotNull(sim, "The deterministic offer search should find Refuerzo de Emergencia.");
            Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", emergencyCardIndex));
            object state = Read(sim, "BonusTrack");
            Assert.IsTrue((bool)Read(state, "EmergencyRoleSelectionPending"));

            const float height = 1360f;
            float verticalScale = height / 960f;
            Type viewType = Runtime("StreetView");
            object footer = viewType.GetMethod("BuildResponsiveFooterLayout", AnyStatic)
                .Invoke(null, new object[] { sim, height, false, false });
            Rect[] cardsBounds = (Rect[])Read(footer, "BonusChoiceBounds");
            IList roles = (IList)Read(state, "EmergencyRoles");
            Assert.AreEqual(roles.Count, cardsBounds.Length);

            GameObject viewHost = new GameObject("Huracán inline emergency role pointer test");
            viewHost.SetActive(false);
            hosts.Add(viewHost);
            Component game = viewHost.AddComponent(gameType);
            gameType.GetField("sim", PrivateInstance).SetValue(game, sim);
            gameType.GetField("playerActive", PrivateInstance).SetValue(game, true);
            Component view = viewHost.AddComponent(viewType);
            viewType.GetField("game", PrivateInstance).SetValue(view, game);
            viewType.GetField("logicalCanvasHeight", PrivateInstance).SetValue(view, height);
            viewType.GetField("layoutVerticalScale", PrivateInstance).SetValue(view, verticalScale);
            MethodInfo hitAction = viewType.GetMethod("HitAction", PrivateInstance);
            for (int i = 0; i < cardsBounds.Length; i++)
            {
                Vector2 pointer = new Vector2(cardsBounds[i].center.x, cardsBounds[i].center.y / verticalScale);
                Assert.AreEqual(5020 + i, (int)hitAction.Invoke(view, new object[] { pointer }),
                    "The role card itself selects the role in that column.");
            }
        }

        [Test]
        public void AguanteRequiresTenDeliveredUnitsAndExpiresWithoutPermanentSpeedChange()
        {
            object bonus = Activator.CreateInstance(stateType, new object[] { 81 });
            for (int i = 0; i < 9; i++) Call(bonus, "RecordDeliveredUnit");
            Assert.IsFalse((bool)Read(bonus, "AguanteReady"));
            Assert.AreEqual(900, Read(bonus, "Score"));
            Call(bonus, "RecordDeliveredUnit");
            Assert.IsTrue((bool)Read(bonus, "AguanteReady"));
            Assert.IsTrue((bool)Call(bonus, "ActivateAguante"));
            object parrillero = Enum.Parse(roleType, "Parrillero");
            object mozo = Enum.Parse(roleType, "Mozo");
            Assert.AreEqual(1.25f, (float)Call(bonus, "ProductionMultiplier", parrillero), .0001f);
            Assert.AreEqual(1.25f, (float)Call(bonus, "MovementMultiplier", mozo), .0001f);
            Call(bonus, "Advance", 8f);
            Assert.IsFalse((bool)Read(bonus, "AguanteActive"));
            Assert.AreEqual(1f, (float)Call(bonus, "ProductionMultiplier", parrillero), .0001f);
            Assert.AreEqual(0f, (float)Read(bonus, "AguanteProgress"));
        }

        [Test]
        public void OnlyRealSimulationHandoffsAddBonusPointsAndNeverAddCurrency()
        {
            object sim = NewSim(11);
            Assert.IsTrue((bool)Call(sim, "StartBonusRound", 1234));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
            int unitEvents = 0;
            EventInfo delivered = simType.GetEvent("ProductUnitDelivered", PublicInstance);
            Action handler = () => unitEvents++;
            delivered.AddEventHandler(sim, handler);

            object state = Read(sim, "BonusTrack");
            for (int guard = 0; guard < 3000 && (int)Read(state, "Deliveries") == 0; guard++)
            {
                if ((bool)Read(state, "SelectionPending"))
                {
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                    state = Read(sim, "BonusTrack");
                    if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                        Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                }
                else Call(sim, "Step", .05f);
            }

            int deliveries = (int)Read(state, "Deliveries");
            int completed = (int)Read(state, "CompletedOrders");
            Assert.Greater(deliveries, 0);
            Assert.AreEqual(deliveries, unitEvents);
            Assert.GreaterOrEqual((int)Read(state, "Score"), 100 * deliveries + 150 * completed);
            Assert.AreEqual(0, Read(sim, "Coins"));
            Assert.AreEqual(0, Read(sim, "CoinsEarned"));
            Assert.AreEqual(0, Read(sim, "CustomersLost"));
        }

        [Test]
        public void BonusTicketGrantIsIdempotentAndThreeStarExtraIsOneTime()
        {
            object save = Activator.CreateInstance(saveType);
            Put(save, "version", 5);
            Put(save, "unlockedLevel", 4);
            Put(save, "huracanUnlocked", true);
            object tickets = Activator.CreateInstance(ticketType);
            Put(tickets, "migrationApplied", true);
            Put(save, "tickets", tickets);
            object game = CreateGame(JsonUtility.ToJson(save));

            Assert.AreEqual(0L, Read(game, "TicketBalance"));
            Assert.IsTrue((bool)Call(game, "SelectLevel", 11));
            Assert.IsTrue((bool)Call(game, "TryStartRound"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 3000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, Read(game, "TicketBalance"));
            Assert.AreEqual(1, Read(lastMatch, "bonusTickets"));
            Assert.IsFalse((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, Read(game, "TicketBalance"), "Repeating the terminal callback cannot award a second ticket.");

            Assert.IsTrue((bool)Call(game, "Retry"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 10000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(3L, Read(game, "TicketBalance"), "The second run earns one regular ticket and the first-time three-star extra.");
            Assert.AreEqual(2, Read(lastMatch, "bonusTickets"));
            Assert.IsTrue((bool)Read(Read(game, "BonusTrackProgress"), "threeStarTicketAwarded"));
            Assert.IsTrue((bool)Call(game, "IsLevelUnlocked", 5));

            Assert.IsTrue((bool)Call(game, "Retry"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 10000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(4L, Read(game, "TicketBalance"), "Later three-star runs earn only the repeatable regular ticket.");
            Assert.AreEqual(1, Read(Read(game, "BonusTrackProgress"), "lastTickets"));
            Assert.IsNotEmpty(latestJson);
        }

        [Test]
        public void BonusMatchAndStaleOutboxUseTheCommittedSnapshotInventory()
        {
            int[] stock = { 3, 0, 5, 7, 8, 9, 10, 11, 12, 13 };
            object save = Activator.CreateInstance(saveType);
            Put(save, "version", 5);
            Put(save, "unlockedLevel", 4);
            Put(save, "huracanUnlocked", true);
            object tickets = Activator.CreateInstance(ticketType);
            Put(tickets, "migrationApplied", true);
            Put(save, "tickets", tickets);
            object market = Activator.CreateInstance(Runtime("StreetMarketData"));
            Put(market, "stockUnits", stock);
            Put(market, "kitGranted", true);
            Put(market, "initializedUnlockedLevel", 4);
            Put(save, "market", market);

            object game = CreateGame(JsonUtility.ToJson(save));
            Assert.IsTrue((bool)Call(game, "SelectLevel", 11));
            Assert.IsTrue((bool)Call(game, "TryStartRound"));
            object match = gameType.GetMethod("BuildBonusMatchRecord", AnyInstance)
                .Invoke(game, new[] { Read(Read(game, "Sim"), "BonusTrack") });

            var expected = Array.ConvertAll(stock, value => (long)value);
            CollectionAssert.AreEqual((ICollection)expected, (ICollection)Read(match, "stockRemaining"),
                "Huracán does not consume Mercado stock; its match must retain the current inventory.");

            object committedSnapshot = JsonUtility.FromJson(latestJson, saveType);
            var queuedFields = new Dictionary<string, object> { { "stockRemaining", new long[10] } };
            Type repositoryType = Type.GetType("HayChoriYPaty.Cloud.FirestorePlayerRepository, Assembly-CSharp", true);
            MethodInfo bindInventory = repositoryType.GetMethod("BindMatchInventoryToSnapshot", AnyStatic);
            Assert.NotNull(bindInventory);
            bindInventory.Invoke(null, new[] { queuedFields, committedSnapshot });

            var expectedFirestore = Array.ConvertAll(stock, value => (object)(long)value);
            CollectionAssert.AreEqual(expectedFirestore, (ICollection)queuedFields["stockRemaining"],
                "Retrying an older queued match must project stockRemaining from its immutable snapshot.");
        }

        private object CreateGame(string json)
        {
            var host = new GameObject("Huracán bonus-track isolated test");
            host.SetActive(false);
            hosts.Add(host);
            Component game = host.AddComponent(gameType);
            host.SetActive(true);
            Type callbackType = typeof(Func<,,>).MakeGenericType(typeof(string), matchType, typeof(bool));
            MethodInfo callbackMethod = GetType().GetMethod("Persist", PrivateInstance).MakeGenericMethod(matchType);
            Delegate persist = callbackMethod.CreateDelegate(callbackType, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", json, persist));
            return game;
        }

        private bool Persist<T>(string json, T match)
        {
            latestJson = json;
            if (match != null) lastMatch = match;
            return true;
        }

        private object NewSim(int level) => Activator.CreateInstance(simType,
            new object[] { Activator.CreateInstance(balanceType), level, 5f, 0, 1, 0 });

        private void AdvanceTo(object sim, float deadline)
        {
            for (int guard = 0; guard < 32 && (string)Read(sim, "Phase").ToString() == "Playing" && (float)Read(sim, "Elapsed") + .0001f < deadline; guard++)
            {
                float remaining = deadline - (float)Read(sim, "Elapsed");
                Call(sim, "Step", Mathf.Min(10f, remaining));
                if ((bool)Read(Read(sim, "BonusTrack"), "SelectionPending")) return;
            }
        }

        private void CompleteRound(object sim)
        {
            object state = Read(sim, "BonusTrack");
            for (int guard = 0; guard < 3000 && Read(sim, "Phase").ToString() == "Playing"; guard++)
            {
                if ((bool)Read(state, "SelectionPending"))
                {
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                    state = Read(sim, "BonusTrack");
                    if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                        Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                }
                else Call(sim, "Step", .25f);
            }
            Assert.AreEqual("Won", Read(sim, "Phase").ToString());
        }

        private void SetScore(object state, int score) => stateType.GetField("score", AnyInstance).SetValue(state, score);

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);

        private static object Read(object instance, string name)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance), "Cannot read " + name);
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, AnyInstance);
            if (property != null) return property.GetValue(instance, null);
            FieldInfo field = type.GetField(name, AnyInstance);
            if (field != null) return field.GetValue(instance);
            throw new ArgumentException("No readable member " + name + " on " + type.FullName);
        }

        private static void Put(object instance, string name, object value) => instance.GetType().GetField(name, AnyInstance).SetValue(instance, value);

        private static object Call(object instance, string name, params object[] args)
        {
            foreach (MethodInfo method in instance.GetType().GetMethods(AnyInstance))
            {
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool compatible = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { compatible = false; break; }
                if (compatible) return method.Invoke(instance, args);
            }
            throw new MissingMethodException(instance.GetType().FullName, name);
        }

        private static object CallStatic(Type type, string name, params object[] args) => type.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);

        private static object First(object sequence)
        {
            foreach (object value in (IEnumerable)sequence) return value;
            throw new InvalidOperationException("Expected at least one role option.");
        }
    }
}
