using UnityEngine;
using System;

namespace HayChoriYPaty
{
    /// <summary>Bounded, snapshot-owned ticket wallet and one-attempt worker reservations.</summary>
    [Serializable]
    public sealed class StreetTicketData
    {
        public int schemaVersion = 1;
        public long balance, totalEarned, totalSpent;
        public long[] pendingWorkersByRole = new long[5];
        public bool migrationApplied;
        public string lastAwardedAttemptId, lastRedeemOperationId;
        public int lastRedeemRole = -1;

        public bool IsValid()
        {
            if (schemaVersion != 1 || balance < 0 || totalEarned < 0 || totalSpent < 0 ||
                totalSpent > totalEarned || balance != totalEarned - totalSpent || totalSpent % 3 != 0 ||
                pendingWorkersByRole == null || pendingWorkersByRole.Length != 5) return false;
            for (int i = 0; i < pendingWorkersByRole.Length; i++) if (pendingWorkersByRole[i] < 0) return false;
            return true;
        }
    }

    /// <summary>Unity lifecycle and isolated persistence wrapper for StreetSimulation.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetGame : MonoBehaviour
    {
        public const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        private const string SaveKey = ProgressKey;
        [SerializeField] private StreetBalance balance = new StreetBalance();
        [SerializeField, Range(0, 12)] private int selectedLevel;
        private StreetSimulation sim;
        private OrderDeliverySound deliverySound;
        private SaveData save;
        private bool resultSaved;
        private bool resumedChulengoResult;
        private StreetMarket market;
        private TowerDefenseGameMode towerDefenseMode;
        private bool playerActive;
        private bool suspensionRequested;
        private Func<string, StreetMatchRecord, bool> persist;
        private string activeAttemptId;
        private long attemptHireSpendCents, attemptSpeedSpendCents;
        private readonly long[] attemptHiresByRole = new long[5];
        private const long TemporaryStartupTicketGrant = 30L;
        private static bool temporaryStartupTicketGrantAppliedThisProcess;
#if UNITY_EDITOR
        /// <summary>Test-only opt-in to the pre-cloud PlayerPrefs boot path.</summary>
        public static bool AllowEditorTestInitialization;
#endif

        [System.Serializable]
        public sealed class SaveData
        {
            public int version = 7;
            public int unlockedLevel;
            // `unlockedLevel` remains the original stable-ID frontier; campaign order is separate.
            public bool huracanUnlocked, legacyRacingUnlocked, racingUnlockedByHuracan, argentinosUnlocked;
            // Explicit, stable-ID unlock state for the reorganized campaign. Never infer this set from a max ID.
            public bool[] unlockedLevelIds = new bool[13];
            // One-time repair for pre-v7 market snapshots, expressed in the old stable-ID frontier.
            public int legacyProgressionRepairLevel = -1;
            // Legacy ID10 wins/records are historical traditional Los Redondos, before ID10 became Tower Defense.
            public bool hasLegacyTraditionalLosRedondosResult;
            public string legacyTraditionalLosRedondosPendingAttemptId;
            public float price = 5f;
            public float[] prices;
            public int coins;
            public int staff = 1;
            public int speed;
            public int workerLevel = -1;
            public int parrilleros = -1;
            public int cocacoleros;
            public int premiumParrilleros;
            public int ferneteros;
            public int mozos;
            public StreetMarketData market;
            public StreetTicketData tickets;
            public StreetPlayerStatistics statistics = new StreetPlayerStatistics();
            public StreetBonusTrackProfile bonusTrack = new StreetBonusTrackProfile();
            public string activeAttemptId;
            // Checkpoint for cumulative metrics of a live attempt. It is snapshot-only, never history/outbox.
            public StreetMatchRecord activeAttemptSummary;
            public StreetMatchRecord pendingMatch;
            public StreetMatchRecord pendingVictoryMatch;
        }

        public StreetSimulation Sim { get { return playerActive ? sim : null; } }
        public StreetBalance Balance { get { return balance; } }
        public int SelectedLevel { get { return selectedLevel; } }
        public int UnlockedLevel { get { return save == null ? 0 : save.unlockedLevel; } }
        public bool IsBonusTrack => playerActive && sim != null && sim.IsBonusTrack;
        public bool IsTowerDefenseCampaignRun => playerActive && sim != null && sim.IsExternalCampaignRuntime;
        public bool CanAdvanceAfterResult
        {
            get
            {
                if (!playerActive || sim == null || (sim.Phase != RoundPhase.Won && !resumedChulengoResult)) return false;
                int next = StreetSimulation.NextCampaignLevelId(sim.LevelIndex);
                return next >= 0 && IsLevelUnlocked(next);
            }
        }
        public bool IsLevelUnlocked(int levelId)
        {
            if (save == null || levelId < 0 || levelId >= StreetSimulation.LevelNames.Length) return false;
            if (save.version >= 7 && save.unlockedLevelIds != null && save.unlockedLevelIds.Length == StreetSimulation.LevelNames.Length)
                return save.unlockedLevelIds[levelId];
            if (levelId == StreetSimulation.HuracanBonusLevelId)
                return save.huracanUnlocked || save.legacyRacingUnlocked;
            if (levelId == StreetSimulation.ArgentinosJuniorsLevelId)
                return save.argentinosUnlocked;
            if (levelId == 5)
                return save.legacyRacingUnlocked || save.racingUnlockedByHuracan || save.unlockedLevel >= 6;
            return levelId <= save.unlockedLevel;
        }
        private int FirstUnlockedCampaignLevel()
        {
            for (int position = 0; position < StreetSimulation.CampaignOrder.Length; position++)
            {
                int id = StreetSimulation.CampaignLevelAt(position);
                if (IsLevelUnlocked(id)) return id;
            }
            return 0;
        }
        public bool IsIngredientUnlocked(int ingredientId) => playerActive && market != null && save != null &&
            market.IsIngredientUnlocked(ingredientId, save.unlockedLevelIds, save.unlockedLevel);
        public bool HasWonLevel(int levelIndex)
        {
            if (save == null || save.statistics == null || levelIndex < 0 || levelIndex >= StreetSimulation.LevelNames.Length ||
                save.statistics.winsByLevel == null || levelIndex >= save.statistics.winsByLevel.Length) return false;
            return save.statistics.winsByLevel[levelIndex] > 0;
        }
        /// <summary>Consolidated real deliveries, already persisted in the UID's v4 cloud snapshot.
        /// Pending Chulengo/live-attempt metrics are not added a second time.</summary>
        public long GetCumulativeProductSales(int product)
        {
            var totals = playerActive ? save?.statistics?.deliveredByProduct : null;
            return product < 0 || product >= StreetSalesBadges.ProductCount || totals == null || product >= totals.Length
                ? 0L : Math.Max(0L, totals[product]);
        }
        public bool HasProductSalesBadge(int product, int tier)
        {
            var badges = playerActive ? save?.statistics?.earnedProductSalesBadges : null;
            if (product < 0 || product >= StreetSalesBadges.ProductCount || tier < 0 || tier >= StreetSalesBadges.TierCount || badges == null) return false;
            int index = product * StreetSalesBadges.TierCount + tier;
            return index < badges.Length && badges[index];
        }
        public bool HasIncompleteSalesHistory => playerActive && save?.statistics != null && save.statistics.legacyCoverageIncomplete;
        public StreetMarket Market => playerActive ? market : null;
        public StreetBonusTrackProfile BonusTrackProgress => playerActive ? save?.bonusTrack : null;
        public long TicketBalance => playerActive && save?.tickets != null ? save.tickets.balance : 0L;
        public long TicketTotalEarned => playerActive && save?.tickets != null ? save.tickets.totalEarned : 0L;
        public long TicketTotalSpent => playerActive && save?.tickets != null ? save.tickets.totalSpent : 0L;
        public bool TicketExtrasUnlocked => playerActive && IsLevelUnlocked(5);
        public string TicketRedemptionOperationId(StreetWorkerRole role)
        {
            int index = (int)role;
            return save?.tickets != null && index >= 0 && index < 5
                ? FormatTicketRedemptionOperationId(save.tickets.totalSpent / 3L, index) : null;
        }
        public long PendingTicketWorkers(StreetWorkerRole role)
        {
            int index = (int)role;
            return playerActive && save?.tickets?.pendingWorkersByRole != null && index >= 0 && index < 5
                ? save.tickets.pendingWorkersByRole[index] : 0L;
        }
        public bool TicketRewardAppliedForCurrentVictory => playerActive && sim != null && sim.Phase == RoundPhase.Won &&
            save?.tickets != null && save.statistics != null && !string.IsNullOrEmpty(save.tickets.lastAwardedAttemptId) &&
            save.tickets.lastAwardedAttemptId == save.statistics.lastAppliedAttemptId;
        public bool IsPlayerActive => playerActive;
        public string ActiveSaveJson => save == null ? null : JsonUtility.ToJson(save);
        public bool HasPendingChulengo => save != null && save.pendingVictoryMatch != null;
        public bool ShowChulengoResult => HasPendingChulengo || (playerActive && !IsBonusTrack && (resumedChulengoResult ||
            (sim != null && sim.Phase == RoundPhase.Won && market?.LastResult != null && market.LastResult.chulengoManaged)));
        public bool ChulengoChoiceLocked => market?.LastResult != null && market.LastResult.chulengoChoiceLocked;
        public int ChulengoSelectedSector => market?.LastResult == null ? -1 : market.LastResult.chulengoSector;
        public float ChulengoSelectedPosition => market?.LastResult != null && market.LastResult.chulengoHasNeedlePosition
            ? market.LastResult.chulengoNeedlePosition : (ChulengoSelectedSector + .5f) / StreetChulengo.SectorCount;
        public long ChulengoBaseRewardCents => market?.LastResult == null ? 0L : market.LastResult.chulengoBaseRewardCents;
        public long ChulengoFinalRewardCents => market?.LastResult == null ? 0L : market.LastResult.earnedCents;
        public bool CanStartRound => !HasPendingChulengo && !resumedChulengoResult && playerActive && sim != null && sim.Phase == RoundPhase.Ready &&
            (sim.IsBonusTrack || (market != null && !market.Data.attemptActive && market.CanMeetRequiredStockWithLevelOneFloor(sim)));

        private void OnEnable()
        {
            playerActive = false;
#if UNITY_EDITOR
            if (AllowEditorTestInitialization)
                ActivatePlayer(PlayerPrefs.HasKey(SaveKey) ? PlayerPrefs.GetString(SaveKey) : null,
                    (json, match) => { PlayerPrefs.SetString(SaveKey, json); PlayerPrefs.Save(); return true; });
#endif
        }

        /// <summary>Enables the game only after the caller has authorized an owner-specific save.</summary>
        public bool ActivatePlayer(string savedJson, Func<string, StreetMatchRecord, bool> saveDelegate)
        {
            if (playerActive || suspensionRequested) return false;
            if (balance == null) balance = new StreetBalance();
            bool hasPriorSave = savedJson != null;
            SaveData loaded;
            if (hasPriorSave)
            {
                if (!TryLoad(savedJson, out loaded)) return false; // Invalid data is never treated as a new profile.
            }
            else loaded = DefaultSave();
            persist = saveDelegate;
            save = loaded;
            bool missingStatistics = save.statistics == null || string.IsNullOrEmpty(save.statistics.instrumentationStartedAtUtc);
            if (save.statistics == null) save.statistics = new StreetPlayerStatistics();
            save.statistics.Normalize();
            if (save.bonusTrack == null) save.bonusTrack = new StreetBonusTrackProfile();
            save.bonusTrack.Normalize();
            if (!InitializeTicketData(save)) return false;
            if (missingStatistics)
            {
                save.statistics.instrumentationStartedAtUtc = DateTime.UtcNow.ToString("o");
                save.statistics.legacyCoverageIncomplete = hasPriorSave;
                if (hasPriorSave && save.market != null)
                {
                    save.statistics.knownLegacyIngredientSpendCents = Math.Max(0, save.market.cumulativePurchaseSpendCents);
                    save.statistics.knownLegacyPurchaseUnits = Math.Max(0, save.market.cumulativePurchaseUnits);
                    save.statistics.ingredientSpendTotalCents = Math.Max(save.statistics.ingredientSpendTotalCents,
                        save.statistics.knownLegacyIngredientSpendCents);
                }
            }
            activeAttemptId = save.activeAttemptId;
            towerDefenseMode = null;
            bool hasPersistedMarket = save.market != null;
            market = new StreetMarket(save.market, balance);
            save.market = market.Data;
            if (!hasPriorSave) market.InitializeNewGameKit();
            else if (hasPersistedMarket) market.InitializeExistingSave(save.unlockedLevel);
            else market.Initialize(save.unlockedLevel); // One-time compatibility migration for pre-Mercado saves.
            if (!hasPriorSave) save.statistics.initialFreeMarketBalanceCents = market.BalanceCents;
            bool hadInterruptedAttempt = market.Data.attemptActive;
            market.RecoverAbandonedAttempt();
            StreetMatchRecord recoveredAttempt = null;
            if (hadInterruptedAttempt && hasPersistedMarket && save.market != null && !save.market.attemptActive &&
                save.activeAttemptSummary != null && !string.IsNullOrEmpty(save.activeAttemptSummary.attemptId) &&
                save.activeAttemptSummary.attemptId == save.activeAttemptId)
            {
                recoveredAttempt = save.activeAttemptSummary;
                save.statistics.ApplyRecoveredAttemptMetrics(recoveredAttempt);
                save.activeAttemptSummary = null;
                save.activeAttemptId = null;
                activeAttemptId = null;
            }
            market.RecoverLegacyVictoryPayout();
            market.RecoverLegacyZeroPayoutTransition();
            resumedChulengoResult = save.pendingVictoryMatch != null;
            if (resumedChulengoResult && market.LastResult != null) selectedLevel = market.LastResult.level;
            else if (!market.Data.attemptActive && market.HasPreparationPermitPurchases &&
                market.Data.preparationLevelId >= 0 && market.Data.preparationLevelId < StreetSimulation.LevelNames.Length &&
                IsLevelUnlocked(market.Data.preparationLevelId))
                selectedLevel = market.Data.preparationLevelId;
            selectedLevel = Mathf.Clamp(selectedLevel, 0, StreetSimulation.LevelNames.Length - 1);
            if (!IsLevelUnlocked(selectedLevel)) selectedLevel = FirstUnlockedCampaignLevel();
            // A Bonus Track interrupted by app closure has no resumable simulation; discard only
            // its volatile attempt token. It never consumed stock or issued a ticket.
            if (!market.Data.attemptActive && selectedLevel == StreetSimulation.HuracanBonusLevelId &&
                !string.IsNullOrEmpty(save.activeAttemptId) && save.pendingMatch == null)
            {
                activeAttemptId = save.activeAttemptId = null;
            }
            sim = new StreetSimulation(balance, selectedLevel, save.price);
            if(save.prices!=null && save.prices.Length==7)for(int i=0;i<7;i++)sim.SetProductPrice(i,save.prices[i]);
            if (save.legacyProgressionRepairLevel > 0 && save.legacyProgressionRepairLevel < StreetSimulation.LevelNames.Length &&
                !market.Data.attemptActive)
            {
                // Preserve the bounded pre-v7 recovery once, using its old stable-ID frontier semantics.
                int legacyNextLevel = save.legacyProgressionRepairLevel;
                var nextUnlockedSimulation = new StreetSimulation(balance, legacyNextLevel, save.price);
                market.ReconcileLegacyProgressionBalance(nextUnlockedSimulation, legacyNextLevel);
                save.legacyProgressionRepairLevel = -1;
            }
            if (!market.Data.attemptActive && !resumedChulengoResult) market.SelectPreparationLevel(selectedLevel);
            InitializeDeliverySound();
            bool startupTicketGrantApplied = ShouldGrantTemporaryStartupTickets() &&
                TryApplyTemporaryStartupTicketGrant(save.tickets);
            resultSaved = false;
            playerActive = true;
            if (!Save())
            {
                if (startupTicketGrantApplied) UndoTemporaryStartupTicketGrant(save.tickets);
                playerActive = false; sim = null; market = null; return false;
            }
            return true;
        }

        private static bool ShouldGrantTemporaryStartupTickets() => !Application.isEditor && Debug.isDebugBuild;

        private static bool TryApplyTemporaryStartupTicketGrant(StreetTicketData tickets)
        {
            if (temporaryStartupTicketGrantAppliedThisProcess || tickets == null || !tickets.IsValid() ||
                tickets.balance > long.MaxValue - TemporaryStartupTicketGrant ||
                tickets.totalEarned > long.MaxValue - TemporaryStartupTicketGrant) return false;

            tickets.balance += TemporaryStartupTicketGrant;
            tickets.totalEarned += TemporaryStartupTicketGrant;
            temporaryStartupTicketGrantAppliedThisProcess = true;
            return true;
        }

        private static void UndoTemporaryStartupTicketGrant(StreetTicketData tickets)
        {
            if (tickets == null || !temporaryStartupTicketGrantAppliedThisProcess) return;
            tickets.balance -= TemporaryStartupTicketGrant;
            tickets.totalEarned -= TemporaryStartupTicketGrant;
            temporaryStartupTicketGrantAppliedThisProcess = false;
        }

        private void Update()
        {
            if (!playerActive || sim == null || TowerDefenseGameMode.CampaignStepSuspended) return;
            sim.Step(Time.deltaTime);
            ProcessRoundResult();
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused || (!playerActive && !suspensionRequested)) return;
            if (playerActive && sim != null && sim.IsExternalCampaignRuntime)
            {
                if (towerDefenseMode != null && towerDefenseMode.Phase == TowerDefenseRunPhase.Finished &&
                    (towerDefenseMode.HasWon || towerDefenseMode.HasLost))
                    CompleteTowerDefenseRound(towerDefenseMode);
                else if (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost) ProcessRoundResult();
                else if (towerDefenseMode != null) PersistTowerDefenseProgress(towerDefenseMode);
                else Save();
                if (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost) ProcessRoundResult();
                return;
            }
            if (!TowerDefenseGameMode.CampaignStepSuspended)
            {
                if (playerActive) ProcessRoundResult();
                Save();
            }
        }
        private void OnApplicationQuit() { if ((sim != null && sim.IsExternalCampaignRuntime) || !TowerDefenseGameMode.CampaignStepSuspended) FinishOrAbort(); }
        private void OnDisable() { if ((sim != null && sim.IsExternalCampaignRuntime) || !TowerDefenseGameMode.CampaignStepSuspended) FinishOrAbort(); }

        private void FinishOrAbort()
        {
            if (!playerActive || sim == null || market == null) return;
            if (sim.IsExternalCampaignRuntime)
            {
                // Preserve the attempt snapshot; the existing activation recovery rolls stock back
                // and applies this checkpoint once instead of inventing TD resume state.
                if (towerDefenseMode != null && towerDefenseMode.Phase == TowerDefenseRunPhase.Finished &&
                    (towerDefenseMode.HasWon || towerDefenseMode.HasLost))
                    CompleteTowerDefenseRound(towerDefenseMode);
                else if (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost) ProcessRoundResult();
                else if (towerDefenseMode != null) PersistTowerDefenseProgress(towerDefenseMode);
                else Save();
                return;
            }
            ProcessRoundResult();
            if (sim.IsBonusTrack && sim.Phase == RoundPhase.Playing)
            {
                activeAttemptId = null;
                if (save != null) save.activeAttemptId = null;
                ResetAttemptMetrics();
                Save();
                return;
            }
            if (sim.Phase == RoundPhase.Playing) market.AbortAttempt();
            Save();
        }

        public bool ProcessRoundResult()
        {
            if (save != null && save.pendingVictoryMatch != null)
            {
                // Persist the pending victory once; a chosen outcome can only retry without changing it.
                return resultSaved || (resultSaved = Save());
            }
            if (sim != null && sim.IsBonusTrack) return ProcessBonusTrackResult();
            if ((!playerActive && !suspensionRequested) || sim == null || save == null || resultSaved ||
                (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost)) return false;
            StreetMarketResult receipt = market?.SettleAttempt(sim);
            if (market != null && market.Data.attemptActive) return false; // Refuse progression if settlement was rejected.
            if (receipt == null) return false;
            int previousUnlocked = save.unlockedLevel;
            bool[] previousCampaignUnlocks = (bool[])save.unlockedLevelIds.Clone();
            bool previousHuracanUnlocked = save.huracanUnlocked;
            bool previousArgentinosUnlocked = save.argentinosUnlocked;
            bool previousLegacyRacingUnlocked = save.legacyRacingUnlocked;
            if (sim.Phase == RoundPhase.Won)
            {
                int nextLevel = StreetSimulation.NextCampaignLevelId(sim.LevelIndex);
                bool firstTimeUnlock = nextLevel >= 0 && !IsLevelUnlocked(nextLevel);
                if (nextLevel >= 0) SetCampaignLevelUnlocked(nextLevel);
                save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(10, sim.LevelIndex + 1));
                if (market != null && nextLevel >= 0 && nextLevel != StreetSimulation.HuracanBonusLevelId)
                {
                    var nextLevelSimulation = new StreetSimulation(balance, nextLevel, sim.Price);
                    market.GrantFirstUnlockStockBonus(nextLevelSimulation, firstTimeUnlock);
                }
            }
            if (receipt.won && !receipt.chulengoManaged)
            {
                StreetMatchRecord victoryMatch = BuildMatchRecord(receipt);
                if (!market.PrepareChulengo())
                {
                    RestoreCampaignProgress(previousUnlocked, previousCampaignUnlocks, previousHuracanUnlocked,
                        previousArgentinosUnlocked, previousLegacyRacingUnlocked);
                    return false;
                }
                save.pendingVictoryMatch = victoryMatch;
                save.activeAttemptSummary = null;
                resultSaved = Save(); // No final match/history/statistics before the player chooses.
                if (!resultSaved)
                {
                    RestoreCampaignProgress(previousUnlocked, previousCampaignUnlocks, previousHuracanUnlocked,
                        previousArgentinosUnlocked, previousLegacyRacingUnlocked);
                }
                return resultSaved;
            }
            if (save.pendingMatch == null) save.pendingMatch = BuildMatchRecord(receipt);
            if (save.pendingMatch.won && !EnsureVictoryTicketAwarded(save.pendingMatch))
            {
                RestoreCampaignProgress(previousUnlocked, previousCampaignUnlocks, previousHuracanUnlocked,
                    previousArgentinosUnlocked, previousLegacyRacingUnlocked);
                return false;
            }
            ApplyCampaignMatchStatistics(save.pendingMatch);
            resultSaved = SaveWithMatch(save.pendingMatch);
            if (!resultSaved)
            {
                RestoreCampaignProgress(previousUnlocked, previousCampaignUnlocks, previousHuracanUnlocked,
                    previousArgentinosUnlocked, previousLegacyRacingUnlocked);
            }
            else { ResetAttemptMetrics(); activeAttemptId = null; save.activeAttemptId = null; }
            return resultSaved;
        }

        private bool ProcessBonusTrackResult()
        {
            if ((!playerActive && !suspensionRequested) || sim == null || save == null || save.bonusTrack == null || resultSaved ||
                sim.Phase != RoundPhase.Won) return false;
            StreetBonusTrackState attempt = sim.BonusTrack;
            if (attempt == null) return false;
            StreetMatchRecord record = save.pendingMatch;
            if (record == null || record.level != StreetSimulation.HuracanBonusLevelId || record.attemptId != activeAttemptId)
            {
                record = BuildBonusMatchRecord(attempt);
                save.pendingMatch = record;
            }

            StreetBonusTrackProfile progress = save.bonusTrack;
            int previousUnlocked = save.unlockedLevel;
            bool[] previousCampaignUnlocks = (bool[])save.unlockedLevelIds.Clone();
            bool previousHuracanUnlocked = save.huracanUnlocked;
            bool previousLegacyRacingUnlocked = save.legacyRacingUnlocked;
            if (progress.lastAttemptId != record.attemptId)
            {
                progress.previousBestScore = progress.bestScore;
                progress.lastAttemptId = record.attemptId;
                progress.attemptsFinished++;
                progress.lastScore = record.bonusScore;
                progress.lastStars = record.bonusStars;
                progress.lastDeliveries = record.bonusDeliveries;
                progress.lastOrders = record.bonusCompletedOrders;
                progress.bestScore = Math.Max(progress.bestScore, record.bonusScore);
                progress.bestStars = Math.Max(progress.bestStars, record.bonusStars);
                progress.bestDeliveries = Math.Max(progress.bestDeliveries, record.bonusDeliveries);
                progress.bestOrders = Math.Max(progress.bestOrders, record.bonusCompletedOrders);
            }

            if (record.bonusStars > 0)
            {
                int nextLevel = StreetSimulation.NextCampaignLevelId(StreetSimulation.HuracanBonusLevelId);
                if (nextLevel >= 0) SetCampaignLevelUnlocked(nextLevel);
                if (!EnsureVictoryTicketAwarded(record))
                {
                    save.unlockedLevel = previousUnlocked;
                    save.unlockedLevelIds = previousCampaignUnlocks;
                    save.huracanUnlocked = previousHuracanUnlocked;
                    save.legacyRacingUnlocked = previousLegacyRacingUnlocked;
                    return false;
                }
                if (record.bonusTickets <= 0) record.bonusTickets = 1;
                if (record.bonusStars == 3 && !progress.threeStarTicketAwarded &&
                    save.tickets.balance < long.MaxValue && save.tickets.totalEarned < long.MaxValue)
                {
                    save.tickets.balance++;
                    save.tickets.totalEarned++;
                    progress.threeStarTicketAwarded = true;
                    record.bonusTickets = Math.Max(2, record.bonusTickets);
                }
            }
            else record.bonusTickets = 0;
            progress.lastTickets = record.bonusTickets;

            if (!save.statistics.HasApplied(record.attemptId)) ApplyCampaignMatchStatistics(record);
            resultSaved = SaveWithMatch(record);
            if (!resultSaved)
            {
                save.unlockedLevel = previousUnlocked;
                save.unlockedLevelIds = previousCampaignUnlocks;
                save.huracanUnlocked = previousHuracanUnlocked;
                save.legacyRacingUnlocked = previousLegacyRacingUnlocked;
                return false;
            }
            ResetAttemptMetrics();
            activeAttemptId = null;
            save.activeAttemptId = null;
            return true;
        }

        private void SetCampaignLevelUnlocked(int levelId)
        {
            if (save?.unlockedLevelIds == null || levelId < 0 || levelId >= save.unlockedLevelIds.Length) return;
            save.unlockedLevelIds[levelId] = true;
            if (levelId == StreetSimulation.HuracanBonusLevelId) save.huracanUnlocked = true;
            else if (levelId == StreetSimulation.ArgentinosJuniorsLevelId) save.argentinosUnlocked = true;
            else if (levelId == 5) save.legacyRacingUnlocked = true;
        }

        private void RestoreCampaignProgress(int legacyFrontier, bool[] unlocks, bool huracan, bool argentinos, bool racing)
        {
            save.unlockedLevel = legacyFrontier;
            save.unlockedLevelIds = unlocks;
            save.huracanUnlocked = huracan;
            save.argentinosUnlocked = argentinos;
            save.legacyRacingUnlocked = racing;
        }

        private void ApplyCampaignMatchStatistics(StreetMatchRecord record)
        {
            if (IsLegacyTraditionalLosRedondosPending(record)) save.statistics.ApplyLegacyTraditionalLosRedondosMatch(record);
            else save.statistics.ApplyMatch(record);
        }

        private bool IsLegacyTraditionalLosRedondosPending(StreetMatchRecord match)
        {
            return save != null && save.hasLegacyTraditionalLosRedondosResult && match != null && match.level ==
                StreetSimulation.LosRedondosTowerDefenseCampaignLevelId &&
                !string.IsNullOrEmpty(save.legacyTraditionalLosRedondosPendingAttemptId) &&
                match.attemptId == save.legacyTraditionalLosRedondosPendingAttemptId;
        }

        public bool TryCollectVictoryReward() => playerActive && ResolveVictoryChoice(-1);
        public bool TryMultiplyVictoryReward(int sector)
        {
            if (!playerActive || sector < 0 || sector >= StreetChulengo.SectorCount) return false;
            return ResolveVictoryChoice(sector);
        }

        public bool TryMultiplyVictoryRewardAtPosition(float position)
        {
            if (!playerActive || float.IsNaN(position) || float.IsInfinity(position) || position < 0f || position > 1f) return false;
            return ResolveVictoryChoice(StreetChulengo.SectorAtPosition(position), position);
        }

        private bool ResolveVictoryChoice(int sector, float exactPosition = -1f)
        {
            if ((!playerActive && !suspensionRequested) || !HasPendingChulengo || market == null || market.LastResult == null) return false;
            StreetMarketResult receipt = market.LastResult;
            // A failed durable write cannot become a second spin: retry only the frozen choice.
            if (receipt.chulengoChoiceLocked && receipt.chulengoSector != sector) return false;
            if (!receipt.chulengoChoiceLocked)
            {
                if (!market.LockChulengoChoice(sector)) return false;
                if (sector >= 0 && exactPosition >= 0f)
                {
                    receipt.chulengoHasNeedlePosition = true;
                    receipt.chulengoNeedlePosition = exactPosition;
                }
            }
            if (receipt.chulengoPending && !Save()) return false; // durable outcome before credit
            if (!market.ResolveChulengo()) return false;
            StreetMatchRecord record = save.pendingVictoryMatch;
            record.halfSalesRewardCents = receipt.recognizedSalesCents;
            record.leftoverBonusCents = receipt.bonusCents;
            record.progressionBonusCents = receipt.progressionBonusCents;
            record.rewardCents = receipt.earnedCents;
            save.pendingMatch = record;
            if (!EnsureVictoryTicketAwarded(record)) return false;
            ApplyCampaignMatchStatistics(record);
            save.pendingVictoryMatch = null;
            resultSaved = SaveWithMatch(record);
            if (!resultSaved) { save.pendingVictoryMatch = record; return false; }
            ResetAttemptMetrics(); activeAttemptId = null; save.activeAttemptId = null;
            return true;
        }

        private bool EnsureResultSaved() => !HasPendingChulengo && (resultSaved ||
            (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost) || ProcessRoundResult());

        public bool SelectLevel(int level)
        {
            if (!playerActive || sim == null || sim.Phase == RoundPhase.Playing || level < 0 ||
                level >= StreetSimulation.LevelNames.Length || !IsLevelUnlocked(level)) return false;
            if (!EnsureResultSaved()) return false;
            string previousSave = JsonUtility.ToJson(save);
            StreetSimulation previousSimulation = sim;
            bool previousHadMarketResult = market.Data.lastResult != null;
            int previousSelectedLevel = selectedLevel;
            bool previousResultSaved = resultSaved, previousResumedChulengoResult = resumedChulengoResult;
            resumedChulengoResult = false;
            selectedLevel = level;
            if (!market.Data.attemptActive) market.SelectPreparationLevel(level);
            ResetAttemptMetrics();
            sim = NewReadySimulation(level); resultSaved = false;
            InitializeDeliverySound();
            if (Save()) return true;

            // A preparation change clears its old permits only when the new selection is durable.
            save = JsonUtility.FromJson<SaveData>(previousSave);
            if (StreetMatchRecord.IsAbsent(save.pendingMatch)) save.pendingMatch = null;
            if (StreetMatchRecord.IsAbsent(save.pendingVictoryMatch)) save.pendingVictoryMatch = null;
            if (StreetMatchRecord.IsAbsent(save.activeAttemptSummary)) save.activeAttemptSummary = null;
            if (!previousHadMarketResult && save.market != null) save.market.lastResult = null;
            market = new StreetMarket(save.market, balance);
            sim = previousSimulation;
            selectedLevel = previousSelectedLevel;
            resultSaved = previousResultSaved;
            resumedChulengoResult = previousResumedChulengoResult;
            InitializeDeliverySound();
            return false;
        }
        public void SetPrice(float value)
        {
            if (!playerActive || sim == null || sim.Phase != RoundPhase.Ready) return;
            sim.SetPrice(value);
        }
        public void SetProductPrice(int product,float value) { if(playerActive && sim!=null && sim.Phase==RoundPhase.Ready)sim.SetProductPrice(product,value); }
        public void StartRound() { TryStartRound(); }
        public bool TryStartRound()
        {
            // Both stable TD IDs are owned by the external mode, never the traditional simulation.
            if (sim == null || StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex)) return false;
            return TryStartRoundCore(false);
        }

        public bool TryStartTowerDefenseRound()
        {
            return sim != null && StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex) &&
                IsLevelUnlocked(sim.LevelIndex) && TryStartRoundCore(true);
        }

        private bool TryStartRoundCore(bool externalTowerDefense)
        {
            if (!playerActive || !CanStartRound) return false;
            if (sim.IsBonusTrack) return TryStartBonusRound();
            long[] ticketGrants = new long[5];
            bool eligibleLevel = sim.LevelIndex >= 5 && !externalTowerDefense;
            if (eligibleLevel) Array.Copy(save.tickets.pendingWorkersByRole, ticketGrants, ticketGrants.Length);
            if (!sim.CanStartRoundWithTicketWorkers(ticketGrants)) return false;

            string previousMarket = JsonUtility.ToJson(market.Data);
            string previousTickets = JsonUtility.ToJson(save.tickets);
            market.EnsureLevelOneStartingStock(sim);
            if (!market.BeginAttempt(sim))
            {
                RestoreMarketAfterFailedStart(previousMarket);
                return false;
            }
            market.ConsumePreparationPermits(sim.LevelIndex);
            if (eligibleLevel)
                for (int role = 0; role < ticketGrants.Length; role++)
                    save.tickets.pendingWorkersByRole[role] -= ticketGrants[role];
            activeAttemptId = Guid.NewGuid().ToString("N");
            save.activeAttemptId = activeAttemptId;
            save.statistics.attemptsStarted++;
            // The same durable snapshot confirms the attempt and consumes eligible reservations.
            if (!Save())
            {
                save.statistics.attemptsStarted--;
                save.activeAttemptId = null; activeAttemptId = null;
                save.tickets = JsonUtility.FromJson<StreetTicketData>(previousTickets);
                market.AbortAttempt();
                RestoreMarketAfterFailedStart(previousMarket);
                return false;
            }
            sim.BindIngredientStock(market);
            resultSaved = false;
            if (!sim.StartRound(ticketGrants) || (externalTowerDefense && !sim.BeginExternalCampaignRuntime()))
            {
                // Preflight and StartRound share the same role/count checks; this is a defensive recovery path.
                save.statistics.attemptsStarted--;
                save.activeAttemptId = null; activeAttemptId = null;
                save.tickets = JsonUtility.FromJson<StreetTicketData>(previousTickets);
                market.AbortAttempt();
                RestoreMarketAfterFailedStart(previousMarket);
                if (!Save()) FreezeAfterUnpersistedMutation();
                return false;
            }
            if (!Save())
            {
                FreezeAfterUnpersistedMutation();
                return false;
            }
            return true;
        }

        public bool AttachTowerDefenseRuntime(TowerDefenseGameMode mode)
        {
            if (!playerActive || sim == null || !sim.IsExternalCampaignRuntime || mode == null) return false;
            towerDefenseMode = mode;
            return PersistTowerDefenseProgress(mode);
        }

        public void DetachTowerDefenseRuntime(TowerDefenseGameMode mode)
        {
            if (ReferenceEquals(towerDefenseMode, mode)) towerDefenseMode = null;
        }

        public bool PersistTowerDefenseProgress(TowerDefenseGameMode mode)
        {
            if (!playerActive || sim == null || mode == null || !sim.IsExternalCampaignRuntime ||
                sim.Phase != RoundPhase.Playing || !ImportTowerDefenseState(mode, false, false)) return false;
            if (Save()) return true;
            FreezeAfterUnpersistedMutation();
            return false;
        }

        public bool CompleteTowerDefenseRound(TowerDefenseGameMode mode)
        {
            if (!playerActive || sim == null || mode == null || !sim.IsExternalCampaignRuntime ||
                mode.Phase != TowerDefenseRunPhase.Finished || (!mode.HasWon && !mode.HasLost)) return false;
            if (sim.Phase == RoundPhase.Playing && !ImportTowerDefenseState(mode, true, mode.HasWon)) return false;
            bool saved = ProcessRoundResult();
            if (saved) towerDefenseMode = null;
            return saved;
        }

        public bool ExitTowerDefenseRound(TowerDefenseGameMode mode)
        {
            if (!playerActive || sim == null || mode == null || !sim.IsExternalCampaignRuntime) return false;
            // A terminal TD run must never be converted into a user-abandoned result merely
            // because the exit action wins the same-frame race with UpdateTowerDefenseFrame.
            if (mode.Phase == TowerDefenseRunPhase.Finished && (mode.HasWon || mode.HasLost))
                return CompleteTowerDefenseRound(mode);
            if (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost)
            {
                bool resultSaved = ProcessRoundResult();
                if (resultSaved) towerDefenseMode = null;
                return resultSaved;
            }
            if (sim.Phase != RoundPhase.Playing || !ImportTowerDefenseState(mode, false, false)) return false;
            bool saved = ReturnToLevels();
            if (saved) towerDefenseMode = null;
            return saved;
        }

        private bool ImportTowerDefenseState(TowerDefenseGameMode mode, bool terminal, bool won)
        {
            int[] hires = mode.HiresByRole == null ? new int[0] : Array.ConvertAll(mode.HiresByRole, value => (int)Math.Min(int.MaxValue, Math.Max(0L, value)));
            int[] delivered = mode.DeliveredByProduct;
            if (!sim.ImportTowerDefenseState(mode.Elapsed, mode.Coins, mode.CoinsEarned, mode.SpeedLevel,
                    mode.WorkerCountsByRole, delivered, mode.CustomersAttended, mode.OrdersCompleted, terminal, won)) return false;
            attemptHireSpendCents = mode.HireSpendCents;
            attemptSpeedSpendCents = mode.SpeedSpendCents;
            Array.Clear(attemptHiresByRole, 0, attemptHiresByRole.Length);
            for (int role = 0; role < Math.Min(hires.Length, attemptHiresByRole.Length); role++)
                attemptHiresByRole[role] = hires[role];
            return true;
        }

        public bool TryHireTowerDefense(TowerDefenseGameMode mode, StreetWorkerRole role, int zone)
        {
            int index = (int)role;
            if (!playerActive || sim == null || mode == null || !sim.IsExternalCampaignRuntime || index < 0 || index >= 5) return false;
            if (!mode.CanPlaceWorker(role, zone)) return false;
            int cost = mode.HireCost(role);
            if (cost <= 0 || !mode.TryHire(role, zone)) return false;
            long costCents = (long)cost * 100L;
            save.statistics.paidHiresByRole[index]++;
            save.statistics.hireSpendCentsByRole[index] += costCents;
            save.statistics.hireSpendTotalCents += costCents;
            if (!PersistTowerDefenseProgress(mode)) return false;
            return true;
        }

        public bool TryUpgradeTowerDefenseSpeed(TowerDefenseGameMode mode)
        {
            if (!playerActive || sim == null || mode == null || !sim.IsExternalCampaignRuntime) return false;
            int cost = mode.SpeedUpgradeCost;
            if (cost <= 0 || !mode.TryUpgradeSpeed()) return false;
            long costCents = (long)cost * 100L;
            save.statistics.speedUpgrades++;
            save.statistics.speedSpendCents += costCents;
            save.statistics.speedSpendTotalCents += costCents;
            if (!PersistTowerDefenseProgress(mode)) return false;
            return true;
        }

        private bool TryStartBonusRound()
        {
            if (!playerActive || sim == null || !sim.IsBonusTrack || !CanStartRound || save.bonusTrack == null) return false;
            string previousMarket = JsonUtility.ToJson(market.Data);
            market.ConsumePreparationPermits(sim.LevelIndex);
            string previousAttempt = save.activeAttemptId;
            long previousStarted = save.statistics.attemptsStarted;
            long previousBonusStarted = save.bonusTrack.attemptsStarted;
            activeAttemptId = Guid.NewGuid().ToString("N");
            save.activeAttemptId = activeAttemptId;
            save.statistics.attemptsStarted++;
            save.bonusTrack.attemptsStarted++;
            int seed = balance.randomSeed ^ (int)(save.bonusTrack.attemptsStarted * 7919L);
            if (!Save())
            {
                save.statistics.attemptsStarted = previousStarted;
                save.bonusTrack.attemptsStarted = previousBonusStarted;
                save.activeAttemptId = activeAttemptId = previousAttempt;
                RestoreMarketAfterFailedStart(previousMarket);
                return false;
            }
            resultSaved = false;
            if (!sim.StartBonusRound(seed))
            {
                save.statistics.attemptsStarted = previousStarted;
                save.bonusTrack.attemptsStarted = previousBonusStarted;
                save.activeAttemptId = activeAttemptId = previousAttempt;
                RestoreMarketAfterFailedStart(previousMarket);
                if (!Save()) FreezeAfterUnpersistedMutation();
                return false;
            }
            Save();
            return true;
        }

        public bool ChooseBonusCard(int index) => playerActive && sim != null && sim.ChooseBonusCard(index);
        public bool ChooseBonusEmergencyRole(StreetWorkerRole role) => playerActive && sim != null && sim.ChooseBonusEmergencyRole(role);
        public bool ActivateBonusAguante() => playerActive && sim != null && sim.ActivateBonusAguante();

        public bool CanRedeemTicketWorker(StreetWorkerRole role)
        {
            int index = (int)role;
            return playerActive && save?.tickets != null && market != null && sim != null &&
                IsLevelUnlocked(sim.LevelIndex) && sim.LevelIndex >= 5 && !sim.IsBonusTrack && sim.Phase == RoundPhase.Ready &&
                !HasPendingChulengo && !resumedChulengoResult && !market.Data.attemptActive &&
                !StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex) &&
                index >= 0 && index < 5 && sim.HasRole(role) && save.tickets.balance >= 3 &&
                save.tickets.totalSpent <= long.MaxValue - 3 && save.tickets.pendingWorkersByRole[index] < long.MaxValue;
        }

        public bool TryRedeemTicketWorker(StreetWorkerRole role, string operationId)
        {
            int index = (int)role;
            if (string.IsNullOrEmpty(operationId) || operationId.Length > 64 || index < 0 || index >= 5 || save?.tickets == null ||
                !TryParseTicketRedemptionOperationId(operationId, out long sequence, out int operationRole) || operationRole != index) return false;
            long nextSequence = save.tickets.totalSpent / 3L;
            // Sequences are contiguous because every successful redemption costs exactly three.
            // This recognizes any delayed replay without retaining an unbounded operation-ID list.
            if (sequence < nextSequence) return true;
            if (sequence != nextSequence || !CanRedeemTicketWorker(role)) return false;
            string before = JsonUtility.ToJson(save.tickets);
            save.tickets.balance -= 3;
            save.tickets.totalSpent += 3;
            save.tickets.pendingWorkersByRole[index]++;
            save.tickets.lastRedeemOperationId = operationId;
            save.tickets.lastRedeemRole = index;
            if (Save()) return true;
            save.tickets = JsonUtility.FromJson<StreetTicketData>(before);
            return false;
        }

        private static string FormatTicketRedemptionOperationId(long sequence, int role) =>
            "ticket-redeem/" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" +
            role.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static bool TryParseTicketRedemptionOperationId(string operationId, out long sequence, out int role)
        {
            sequence = -1; role = -1;
            string[] fields = operationId.Split('/');
            return fields.Length == 3 && fields[0] == "ticket-redeem" &&
                long.TryParse(fields[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out sequence) && sequence >= 0 &&
                int.TryParse(fields[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out role) && role >= 0 && role < 5;
        }

        private void RestoreMarketAfterFailedStart(string serializedMarket)
        {
            market = new StreetMarket(JsonUtility.FromJson<StreetMarketData>(serializedMarket), balance);
            save.market = market.Data;
        }
        public bool TryBuyIngredient(int ingredient, int packs, long operationId = 0)
        {
            if (!playerActive || HasPendingChulengo || resumedChulengoResult || market == null || sim == null || sim.Phase != RoundPhase.Ready || UnlockedLevel == 0) return false;
            string before = JsonUtility.ToJson(market.Data);
            if (!market.TryBuy(sim, ingredient, packs, UnlockedLevel,
                operationId == 0 ? market.NextPurchaseId : operationId)) return false;
            save.statistics.purchasedUnitsByIngredient[ingredient] += (long)packs * StreetIngredientCatalog.PackUnits[ingredient];
            long purchaseCost = (long)packs * market.PackPriceCents(ingredient);
            save.statistics.purchaseSpendCentsByIngredient[ingredient] += purchaseCost;
            save.statistics.ingredientSpendTotalCents += purchaseCost;
            if (Save()) return true;
            save.statistics.purchasedUnitsByIngredient[ingredient] -= (long)packs * StreetIngredientCatalog.PackUnits[ingredient];
            save.statistics.purchaseSpendCentsByIngredient[ingredient] -= purchaseCost;
            save.statistics.ingredientSpendTotalCents -= purchaseCost;
            market = new StreetMarket(JsonUtility.FromJson<StreetMarketData>(before), balance);
            save.market = market.Data;
            return false;
        }
        public bool IsPreparationPermitPurchased(int itemId) => playerActive && market != null && sim != null &&
            market.IsPreparationPermitPurchased(itemId, sim.LevelIndex);

        public bool TryBuyPreparationPermit(int itemId)
        {
            if (!playerActive || HasPendingChulengo || resumedChulengoResult || market == null || sim == null ||
                sim.Phase != RoundPhase.Ready || market.Data.attemptActive ||
                market.Data.preparationLevelId != sim.LevelIndex) return false;
            string before = JsonUtility.ToJson(market.Data);
            if (!market.TryBuyPreparationPermit(itemId, sim.LevelIndex)) return false;
            save.market = market.Data;
            if (Save()) return true;
            RestoreMarketAfterFailedStart(before);
            return false;
        }
        public bool BuyMissingIngredient(int ingredient) => market != null &&
            TryBuyIngredient(ingredient, market.MissingPacks(sim, ingredient));
        public bool TryHire() { return TryHireParrillero(); }
        public bool TryHireParrillero() { return HireRole(StreetWorkerRole.Parrillero); }
        public bool TryHireCocacolero() { return HireRole(StreetWorkerRole.Cocacolero); }
        public bool TryHireParrilleroPremium() { return HireRole(StreetWorkerRole.ParrilleroPremium); }
        public bool TryHireFernetero() { return HireRole(StreetWorkerRole.Fernetero); }
        public bool TryHireMozo() { return HireRole(StreetWorkerRole.Mozo); }
        public bool TryHire(StreetWorkerRole role) { return HireRole(role); }
        private bool HireRole(StreetWorkerRole role)
        {
            if (!playerActive || sim == null) return false;
            int cost = sim.HireCostForRole(role); if (!sim.TryHire(role)) return false;
            save.statistics.paidHiresByRole[(int)role]++;
            long costCents = (long)cost * 100L;
            save.statistics.hireSpendCentsByRole[(int)role] += costCents;
            save.statistics.hireSpendTotalCents += costCents;
            attemptHiresByRole[(int)role]++;
            attemptHireSpendCents += costCents;
            if (Save()) return true;
            save.statistics.paidHiresByRole[(int)role]--;
            save.statistics.hireSpendCentsByRole[(int)role] -= costCents;
            save.statistics.hireSpendTotalCents -= costCents;
            attemptHiresByRole[(int)role]--;
            attemptHireSpendCents -= costCents;
            FreezeAfterUnpersistedMutation();
            return false;
        }
        public bool TryUpgradeSpeed()
        {
            if (!playerActive || sim == null) return false;
            int cost = sim.SpeedCost; if (!sim.TryUpgradeSpeed()) return false;
            long costCents = (long)cost * 100L;
            save.statistics.speedUpgrades++; save.statistics.speedSpendCents += costCents;
            save.statistics.speedSpendTotalCents += costCents;
            attemptSpeedSpendCents += costCents;
            if (Save()) return true;
            save.statistics.speedUpgrades--; save.statistics.speedSpendCents -= costCents;
            save.statistics.speedSpendTotalCents -= costCents; attemptSpeedSpendCents -= costCents;
            FreezeAfterUnpersistedMutation();
            return false;
        }
        public bool NextLevel()
        {
            if (!playerActive || sim == null || (sim.Phase != RoundPhase.Won && !resumedChulengoResult)) return false;
            if (!EnsureResultSaved()) return false;
            int next = StreetSimulation.NextCampaignLevelId(sim.LevelIndex);
            if (next < 0 || !IsLevelUnlocked(next)) return false;
            resumedChulengoResult = false;
            selectedLevel = next;
            if (!market.Data.attemptActive) market.SelectPreparationLevel(next);
            ResetAttemptMetrics();
            sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }
        public bool Retry()
        {
            if (!playerActive || sim == null || !sim.IsBonusTrack ||
                (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost) || !EnsureResultSaved()) return false;
            resultSaved = false;
            ResetAttemptMetrics();
            sim = NewReadySimulation(StreetSimulation.HuracanBonusLevelId);
            InitializeDeliverySound();
            return TryStartBonusRound();
        }
        public bool ReturnToLevels()
        {
            if (!playerActive || sim == null || (!sim.IsBonusTrack && market == null)) return false;
            if (!EnsureResultSaved()) return false;
            if (sim.IsBonusTrack && sim.Phase == RoundPhase.Playing)
            {
                // Abandonment records no result or reward; the round owns no market stock.
                activeAttemptId = null;
                save.activeAttemptId = null;
                ResetAttemptMetrics();
                resultSaved = false;
                sim = NewReadySimulation(selectedLevel);
                InitializeDeliverySound();
                return Save();
            }
            if (sim.Phase == RoundPhase.Playing && market.Data.attemptActive)
            {
                StreetMatchRecord abandoned = BuildAbandonedMatchRecord();
                market.AbortAttempt(); // Keep the existing policy: restore the round's stock snapshot.
                for (int i = 0; i < 10; i++) abandoned.stockRemaining[i] = market.Units(i);
                save.pendingMatch = abandoned;
                save.statistics.ApplyMatch(abandoned);
                if (!SaveWithMatch(abandoned)) { FreezeAfterUnpersistedMutation(); return false; }
                ResetAttemptMetrics(); activeAttemptId = null; save.activeAttemptId = null;
            }
            else market?.AbortAttempt();
            ResetAttemptMetrics();
            resumedChulengoResult = false;
            resultSaved = false; sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }

        /// <summary>Saves the authorized owner's state, then drops all live gameplay references.</summary>
        public bool SuspendPlayer()
        {
            if (!playerActive && !suspensionRequested) return true;
            if (playerActive && sim != null && sim.IsExternalCampaignRuntime && towerDefenseMode != null &&
                !PersistTowerDefenseProgress(towerDefenseMode)) return false;
            if (!HasPendingChulengo && sim != null && (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost) && !EnsureResultSaved())
            { playerActive = false; suspensionRequested = true; return false; }
            // Block simulation and all public gameplay commands even if durable save must be retried.
            playerActive = false; suspensionRequested = true;
            if (!Save()) return false;
            if (deliverySound != null) deliverySound.Initialize(null);
            sim = null; market = null; persist = null; activeAttemptId = null; towerDefenseMode = null;
            suspensionRequested = false;
            return true;
        }

        private void InitializeDeliverySound()
        {
            if (deliverySound == null)
            {
                deliverySound = GetComponent<OrderDeliverySound>();
                if (deliverySound == null) deliverySound = gameObject.AddComponent<OrderDeliverySound>();
            }
            deliverySound.Initialize(sim);
        }

        private StreetSimulation NewReadySimulation(int level)
        {
            var next = new StreetSimulation(balance, level, sim.Price);
            for(int i=0;i<7;i++)next.SetProductPrice(i,sim.GetProductPrice(i));return next;
        }

        private void ResetAttemptMetrics()
        {
            attemptHireSpendCents = attemptSpeedSpendCents = 0;
            Array.Clear(attemptHiresByRole, 0, attemptHiresByRole.Length);
        }

        private bool TryLoad(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) return false;
            try
            {
                // JsonUtility fills omitted fields with defaults; require a real legacy version marker.
                if (!System.Text.RegularExpressions.Regex.IsMatch(json, "^\\s*\\{\\s*\\\"version\\\"\\s*:\\s*[1-7](?:\\s*[,}])")) return false;
                data = MigrateSaveData(JsonUtility.FromJson<SaveData>(json));
                // Unity serializes inline null DTOs as empty objects; never interpret them as results.
                if (data != null && StreetMatchRecord.IsAbsent(data.pendingMatch)) data.pendingMatch = null;
                if (data != null && StreetMatchRecord.IsAbsent(data.pendingVictoryMatch)) data.pendingVictoryMatch = null;
                if (data != null && StreetMatchRecord.IsAbsent(data.activeAttemptSummary)) data.activeAttemptSummary = null;
                if (data == null || data.version != 7 || data.unlockedLevelIds == null ||
                    data.unlockedLevelIds.Length != StreetSimulation.LevelNames.Length ||
                    !data.unlockedLevelIds[0] ||
                    (data.tickets != null && !data.tickets.IsValid())) { data = null; return false; }
                data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 0, 10);
                data.price = Mathf.Clamp(data.price, balance.minPrice, balance.maxPrice);
                data.staff = Mathf.Max(1, data.staff);
                data.speed = Mathf.Max(0, data.speed);
                return true;
            }
            catch { data = null; return false; }
        }

        private static SaveData MigrateSaveData(SaveData data)
        {
            if (data == null) return null;
            if (data.version == 1)
            {
                // Development started from an inflated save; retain its established migration.
                data.staff = 1;
                data.speed = 0;
                data.version = 2;
            }
            if (data.version == 2)
            {
                // Old saves only recorded the first two roles. Composition is never restored,
                // but retain their progress fields and initialize new role counters safely.
                data.premiumParrilleros = 0;
                data.ferneteros = 0;
                data.version = 3;
            }
            if (data.version == 3) data.version = 4; // Market.Initialize performs the one-time stock migration.
            if (data.version == 4)
            {
                // Preserve legacy Racing and later-level access while the visible campaign gains Huracán.
                data.legacyRacingUnlocked = data.unlockedLevel >= 5;
                data.huracanUnlocked = data.unlockedLevel >= 5;
                data.racingUnlockedByHuracan = false;
                if (data.bonusTrack == null) data.bonusTrack = new StreetBonusTrackProfile();
                data.version = 5;
            }
            if (data.version == 5)
            {
                // Do not infer a completed ID10 from unlockedLevel==10: that only proves access.
                // Unlock ID12 only when legacy data contains an actual ID10 victory record.
                bool wonArgentinosPredecessor = data.statistics != null && data.statistics.winsByLevel != null &&
                    data.statistics.winsByLevel.Length > 10 && data.statistics.winsByLevel[10] > 0;
                wonArgentinosPredecessor |= data.pendingMatch != null && data.pendingMatch.level == 10 && data.pendingMatch.won;
                wonArgentinosPredecessor |= data.pendingVictoryMatch != null && data.pendingVictoryMatch.level == 10 && data.pendingVictoryMatch.won;
                data.argentinosUnlocked = wonArgentinosPredecessor;
                data.version = 6;
            }
            if (data.version == 6)
            {
                // Freeze the exact legacy stable-ID access set; do not fill a prefix of the new visual order.
                var legacyAccess = new bool[StreetSimulation.LevelNames.Length];
                legacyAccess[0] = true;
                for (int id = 1; id <= 10; id++) legacyAccess[id] = id <= Mathf.Clamp(data.unlockedLevel, 0, 10);
                legacyAccess[5] |= data.legacyRacingUnlocked || data.racingUnlockedByHuracan || data.unlockedLevel >= 6;
                legacyAccess[StreetSimulation.HuracanBonusLevelId] = data.huracanUnlocked || data.legacyRacingUnlocked;
                legacyAccess[StreetSimulation.ArgentinosJuniorsLevelId] = data.argentinosUnlocked;
                data.unlockedLevelIds = legacyAccess;
                data.legacyProgressionRepairLevel = data.unlockedLevel;
                if (data.statistics != null) data.statistics.ArchiveLegacyTraditionalLosRedondosRecords();

                // The old terminal Redondos record remains historical. Keep a pending token so v7
                // result persistence cannot apply the new campaign successor to that legacy win.
                if ((data.pendingMatch != null && data.pendingMatch.level == StreetSimulation.LosRedondosTowerDefenseCampaignLevelId) ||
                    (data.pendingVictoryMatch != null && data.pendingVictoryMatch.level == StreetSimulation.LosRedondosTowerDefenseCampaignLevelId))
                {
                    data.hasLegacyTraditionalLosRedondosResult = true;
                    var legacyRecord = data.pendingVictoryMatch != null ? data.pendingVictoryMatch : data.pendingMatch;
                    data.legacyTraditionalLosRedondosPendingAttemptId = legacyRecord == null || string.IsNullOrEmpty(legacyRecord.attemptId)
                        ? null : legacyRecord.attemptId;
                }
                data.version = 7;
            }
            return data;
        }


        private bool InitializeTicketData(SaveData data)
        {
            if (data == null) return false;
            if (data.tickets == null) data.tickets = new StreetTicketData();
            if (!data.tickets.IsValid()) return false;
            if (!data.tickets.migrationApplied)
            {
                long migrated = InferLegacyTicketWins(data);
                long room = Math.Min(long.MaxValue - data.tickets.balance, long.MaxValue - data.tickets.totalEarned);
                migrated = Math.Min(migrated, room);
                data.tickets.balance += migrated;
                data.tickets.totalEarned += migrated;
                // A pending old-style terminal record is already reflected in win statistics and migration.
                if (data.pendingMatch != null && data.pendingMatch.won && !string.IsNullOrEmpty(data.pendingMatch.attemptId))
                    data.tickets.lastAwardedAttemptId = data.pendingMatch.attemptId;
                data.tickets.migrationApplied = true;
            }
            return data.tickets.IsValid();
        }

        private static long InferLegacyTicketWins(SaveData data)
        {
            if (data == null) return 0L;
            long impliedFirstWins = Math.Max(0, Math.Min(StreetSimulation.LevelNames.Length, data.unlockedLevel));
            long[] wins = data.statistics != null ? data.statistics.winsByLevel : null;
            StreetMatchRecord pending = data.pendingVictoryMatch;
            int pendingLevel = pending == null ? -1 : pending.level;
            if (pending != null && pending.won && pendingLevel == impliedFirstWins - 1 &&
                At(wins, pendingLevel) == 0 && impliedFirstWins > 0)
                impliedFirstWins--; // ProcessRoundResult raised the unlock before the Chulengo result was finalized.

            long total = impliedFirstWins;
            for (int level = 0; level < StreetSimulation.LevelNames.Length; level++)
            {
                long recordedWins = At(wins, level);
                long additional = level < impliedFirstWins ? Math.Max(0L, recordedWins - 1L) : recordedWins;
                total = additional > 0 && total > long.MaxValue - additional ? long.MaxValue : total + additional;
            }
            return total;
        }

        private bool EnsureVictoryTicketAwarded(StreetMatchRecord record)
        {
            if (record == null || !record.won || record.abandoned || string.IsNullOrEmpty(record.attemptId) || save?.tickets == null) return false;
            if (save.tickets.lastAwardedAttemptId == record.attemptId) return true;
            if (save.tickets.balance == long.MaxValue || save.tickets.totalEarned == long.MaxValue) return false;
            save.tickets.balance++;
            save.tickets.totalEarned++;
            save.tickets.lastAwardedAttemptId = record.attemptId;
            return true;
        }

        private SaveData DefaultSave()
        {
            var data = new SaveData { price = balance.initialPrice };
            data.unlockedLevelIds[0] = true;
            return data;
        }

        private StreetMatchRecord BuildMatchRecord(StreetMarketResult receipt)
        {
            int level = Mathf.Clamp(sim.LevelIndex, 0, StreetSimulation.LevelNames.Length - 1);
            string attemptId = string.IsNullOrEmpty(activeAttemptId) ? save.activeAttemptId : activeAttemptId;
            if (string.IsNullOrEmpty(attemptId))
            {
                attemptId = Guid.NewGuid().ToString("N");
                activeAttemptId = save.activeAttemptId = attemptId;
            }
            var record = new StreetMatchRecord {
                id = attemptId, attemptId = attemptId,
                utc = DateTime.UtcNow.ToString("o"), level = level, club = StreetSimulation.LevelDisplayNames[level],
                won = receipt.won, result = receipt.won ? "won" : "lost", elapsedSeconds = sim.Elapsed, grossSalesCents = receipt.grossSalesCents,
                hireSpendCents = attemptHireSpendCents, speedSpendCents = attemptSpeedSpendCents,
                initialCoinsCents = (long)StreetSimulation.GetStartingCoinsForLevel(balance, level) * 100L,
                leftoverCents = receipt.leftoverCents, halfSalesRewardCents = receipt.recognizedSalesCents,
                leftoverBonusCents = receipt.bonusCents, progressionBonusCents = receipt.progressionBonusCents,
                rewardCents = receipt.earnedCents,
                customersAttended = sim.CustomersAttended, customersLost = sim.CustomersLost, ordersCompleted = sim.OrdersCompleted
            };
            for (int i = 0; i < 7; i++) record.salesByProduct[i] = sim.GetProductDelivered(i);
            for (int i = 0; i < 10; i++) { record.stockConsumed[i] = At(receipt.consumedUnits, i); record.stockRemaining[i] = At(receipt.remainingUnits, i); }
            record.hiresByRole = (long[])attemptHiresByRole.Clone();
            return record;
        }

        private StreetMatchRecord BuildBonusMatchRecord(StreetBonusTrackState attempt)
        {
            string attemptId = string.IsNullOrEmpty(activeAttemptId) ? save.activeAttemptId : activeAttemptId;
            if (string.IsNullOrEmpty(attemptId)) attemptId = activeAttemptId = save.activeAttemptId = Guid.NewGuid().ToString("N");
            var record = new StreetMatchRecord
            {
                id = attemptId, attemptId = attemptId, utc = DateTime.UtcNow.ToString("o"),
                level = StreetSimulation.HuracanBonusLevelId, club = StreetSimulation.LevelDisplayNames[StreetSimulation.HuracanBonusLevelId],
                won = attempt.Stars > 0, result = attempt.Stars > 0 ? "won" : "lost", elapsedSeconds = sim.Elapsed,
                grossSalesCents = 0, rewardCents = 0, bonusScore = attempt.Score, bonusStars = attempt.Stars,
                bonusDeliveries = attempt.Deliveries, bonusCompletedOrders = attempt.CompletedOrders, bonusTickets = attempt.TicketsAwardedThisRun,
                bonusUpgrades = string.Join(" | ", new System.Collections.Generic.List<string>(attempt.ChoiceHistory).ToArray()),
                customersAttended = sim.CustomersAttended, customersLost = 0, ordersCompleted = attempt.CompletedOrders,
                salesByProduct = new long[7], stockConsumed = new long[10], stockRemaining = new long[10], hiresByRole = new long[5]
            };
            for (int i = 0; i < 7; i++) record.salesByProduct[i] = sim.GetProductDelivered(i);
            for (int i = 0; i < 10; i++) record.stockRemaining[i] = market?.Units(i) ?? 0;
            return record;
        }

        private StreetMatchRecord BuildAbandonedMatchRecord()
        {
            int level = Mathf.Clamp(sim.LevelIndex, 0, StreetSimulation.LevelNames.Length - 1);
            string attemptId = string.IsNullOrEmpty(activeAttemptId) ? save.activeAttemptId : activeAttemptId;
            if (string.IsNullOrEmpty(attemptId)) attemptId = activeAttemptId = save.activeAttemptId = Guid.NewGuid().ToString("N");
            var record = new StreetMatchRecord {
                id = attemptId, attemptId = attemptId, utc = DateTime.UtcNow.ToString("o"),
                level = level, club = StreetSimulation.LevelDisplayNames[level], result = "abandoned", abandoned = true,
                elapsedSeconds = sim.Elapsed, grossSalesCents = (long)Math.Max(0, sim.CoinsEarned) * 100L,
                hireSpendCents = attemptHireSpendCents, speedSpendCents = attemptSpeedSpendCents,
                initialCoinsCents = (long)StreetSimulation.GetStartingCoinsForLevel(balance, level) * 100L,
                leftoverCents = (long)Math.Max(0, sim.Coins) * 100L,
                customersAttended = sim.CustomersAttended, customersLost = sim.CustomersLost, ordersCompleted = sim.OrdersCompleted,
                salesByProduct = new long[7], stockConsumed = new long[10], stockRemaining = new long[10], hiresByRole = (long[])attemptHiresByRole.Clone()
            };
            for (int product = 0; product < 7; product++)
            {
                long delivered = sim.GetProductDelivered(product);
                record.salesByProduct[product] = delivered;
                if (!sim.IsExternalCampaignRuntime && !(sim.UsesActiveCooks && product < 4))
                    foreach (int ingredient in StreetIngredientCatalog.Recipe(product))
                        record.stockConsumed[ingredient] += delivered;
            }
            if (sim.IsExternalCampaignRuntime)
            {
                int[] actual = market.AttemptConsumedUnits;
                for (int i = 0; i < 10; i++) record.stockConsumed[i] = At(actual, i);
            }
            for (int i = 0; i < 10; i++) record.stockRemaining[i] = market.Units(i);
            return record;
        }

        private static long At(int[] values, int index) => values != null && index >= 0 && index < values.Length ? Math.Max(0, values[index]) : 0;
        private static long At(long[] values, int index) => values != null && index >= 0 && index < values.Length ? Math.Max(0L, values[index]) : 0L;

        private bool Save()
        {
            if (save != null && save.pendingVictoryMatch != null && market?.LastResult != null &&
                market.LastResult.chulengoChoiceLocked && !market.LastResult.chulengoPending)
            {
                // Final-credit write failed earlier: retry full atomic finalization on shutdown/pause.
                return ResolveVictoryChoice(market.LastResult.chulengoSector);
            }
            return SaveWithMatch(null);
        }

        private bool SaveWithMatch(StreetMatchRecord match)
        {
            if (sim == null || save == null || (!playerActive && !suspensionRequested)) return false;
            if (!sim.IsBonusTrack)
            {
                if(save.prices==null || save.prices.Length!=7)save.prices=new float[7];
                for(int i=0;i<7;i++)save.prices[i]=sim.GetProductPrice(i);
                save.price = sim.Price; save.coins = sim.Coins; save.staff = sim.StaffCount; save.speed = sim.SpeedLevel;
                save.workerLevel = sim.LevelIndex; save.parrilleros = sim.ParrilleroCount; save.cocacoleros = sim.CocacoleroCount;
                save.premiumParrilleros = sim.ParrilleroPremiumCount; save.ferneteros = sim.FerneteroCount; save.mozos = sim.MozoCount;
                save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(10, sim.LevelIndex));
            }
            save.market = market?.Data ?? save.market;
            if (save.pendingMatch != null && save.pendingMatch.won)
            {
                StreetMatchRecord pending = save.pendingMatch;
                if (!IsLegacyTraditionalLosRedondosPending(pending))
                {
                    if (pending.level != StreetSimulation.HuracanBonusLevelId || pending.bonusStars > 0)
                    {
                        int next = StreetSimulation.NextCampaignLevelId(pending.level);
                        if (next >= 0) SetCampaignLevelUnlocked(next);
                    }
                    save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(10, pending.level + 1));
                }
            }
            StreetMatchRecord pendingToRestore = null;
            StreetMatchRecord summaryToRestore = null;
            string activeAttemptToRestore = null;
            try
            {
                StreetMatchRecord outputMatch = match ?? (HasPendingChulengo ? null : save.pendingMatch);
                if (outputMatch != null && ReferenceEquals(save.pendingMatch, outputMatch))
                {
                    // Outbox owns the match separately; the committed v4 snapshot must not requeue it.
                    pendingToRestore = save.pendingMatch;
                    save.pendingMatch = null;
                }
                if (outputMatch != null)
                {
                    // Terminal record owns these metrics now; never persist the checkpoint beside it.
                    summaryToRestore = save.activeAttemptSummary;
                    save.activeAttemptSummary = null;
                }
                else if (sim.Phase == RoundPhase.Playing && market != null && market.Data.attemptActive)
                {
                    // Checkpoint only on existing persistence events (pause, purchases, hires, etc.).
                    save.activeAttemptSummary = BuildActiveAttemptSummary();
                }
                if (outputMatch != null && save.activeAttemptId == outputMatch.attemptId)
                {
                    activeAttemptToRestore = save.activeAttemptId;
                    save.activeAttemptId = null;
                }
                string json = JsonUtility.ToJson(save);
                bool ok = persist != null && persist(json, outputMatch);
#if UNITY_EDITOR
                if (persist == null && AllowEditorTestInitialization)
                { PlayerPrefs.SetString(SaveKey, json); PlayerPrefs.Save(); ok = true; }
#endif
                if (ok && outputMatch != null)
                {
                    resultSaved = true;
                    if (pendingToRestore != null) save.pendingMatch = null;
                    if (activeAttemptId == outputMatch.attemptId)
                    { ResetAttemptMetrics(); activeAttemptId = null; save.activeAttemptId = null; }
                }
                else if (pendingToRestore != null) save.pendingMatch = pendingToRestore;
                if (!ok && summaryToRestore != null) save.activeAttemptSummary = summaryToRestore;
                if (!ok && activeAttemptToRestore != null) save.activeAttemptId = activeAttemptToRestore;
                return ok;
            }
            catch (System.Exception error)
            {
                if (save.pendingMatch == null && pendingToRestore != null) save.pendingMatch = pendingToRestore;
                else if (save.pendingMatch == null && match != null) save.pendingMatch = match;
                if (save.activeAttemptId == null && activeAttemptToRestore != null) save.activeAttemptId = activeAttemptToRestore;
                if (save.activeAttemptSummary == null && summaryToRestore != null) save.activeAttemptSummary = summaryToRestore;
                Debug.LogError("No se pudo guardar Mercado y progreso: " + error.Message);
                return false;
            }
        }

        private StreetMatchRecord BuildActiveAttemptSummary()
        {
            StreetMatchRecord summary = BuildAbandonedMatchRecord();
            summary.result = "incomplete";
            summary.abandoned = false;
            summary.won = false;
            summary.rewardCents = 0;
            summary.halfSalesRewardCents = 0;
            summary.leftoverBonusCents = 0;
            summary.progressionBonusCents = 0;
            summary.recordsReached = 0;
            return summary;
        }

        private void FreezeAfterUnpersistedMutation()
        {
            // The simulation mutation cannot be acknowledged. Freeze all commands and retain it for retry.
            playerActive = false;
            suspensionRequested = true;
        }
    }
}
