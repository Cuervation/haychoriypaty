using UnityEngine;
using System;

namespace HayChoriYPaty
{
    /// <summary>Unity lifecycle and isolated persistence wrapper for StreetSimulation.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetGame : MonoBehaviour
    {
        public const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        private const string SaveKey = ProgressKey;
        [SerializeField] private StreetBalance balance = new StreetBalance();
        [SerializeField, Range(0, 10)] private int selectedLevel;
        private StreetSimulation sim;
        private OrderDeliverySound deliverySound;
        private SaveData save;
        private bool resultSaved;
        private StreetMarket market;
        private bool playerActive;
        private bool suspensionRequested;
        private Func<string, StreetMatchRecord, bool> persist;
        private string activeAttemptId;
        private long attemptHireSpendCents, attemptSpeedSpendCents;
        private readonly long[] attemptHiresByRole = new long[5];
#if UNITY_EDITOR
        /// <summary>Test-only opt-in to the pre-cloud PlayerPrefs boot path.</summary>
        public static bool AllowEditorTestInitialization;
#endif

        [System.Serializable]
        public sealed class SaveData
        {
            public int version = 4;
            public int unlockedLevel;
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
            public StreetPlayerStatistics statistics = new StreetPlayerStatistics();
            public string activeAttemptId;
            // Checkpoint for cumulative metrics of a live attempt. It is snapshot-only, never history/outbox.
            public StreetMatchRecord activeAttemptSummary;
            public StreetMatchRecord pendingMatch;
        }

        public StreetSimulation Sim { get { return playerActive ? sim : null; } }
        public StreetBalance Balance { get { return balance; } }
        public int SelectedLevel { get { return selectedLevel; } }
        public int UnlockedLevel { get { return save == null ? 0 : save.unlockedLevel; } }
        public StreetMarket Market => playerActive ? market : null;
        public bool IsPlayerActive => playerActive;
        public string ActiveSaveJson => save == null ? null : JsonUtility.ToJson(save);
        public bool CanStartRound => playerActive && sim != null && sim.Phase == RoundPhase.Ready &&
            market != null && !market.Data.attemptActive && market.CanMeetRequiredStockWithLevelOneFloor(sim);

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
            selectedLevel = Mathf.Clamp(selectedLevel, 0, save.unlockedLevel);
            sim = new StreetSimulation(balance, selectedLevel, save.price);
            if(save.prices!=null && save.prices.Length==7)for(int i=0;i<7;i++)sim.SetProductPrice(i,save.prices[i]);
            if (save.unlockedLevel > 0)
            {
                // Some older saves entered Mercado with completed-level rewards already watermarked,
                // but no Mercado cash existed to pay them. Reconcile only the next unlocked level once.
                var nextUnlockedSimulation = new StreetSimulation(balance, save.unlockedLevel, save.price);
                market.ReconcileLegacyProgressionBalance(nextUnlockedSimulation, save.unlockedLevel);
            }
            InitializeDeliverySound();
            resultSaved = false;
            playerActive = true;
            if (!Save()) { playerActive = false; sim = null; market = null; return false; }
            return true;
        }

        private void Update()
        {
            if (!playerActive || sim == null) return;
            sim.Step(Time.deltaTime);
            ProcessRoundResult();
        }
        private void OnApplicationPause(bool paused) { if (paused && (playerActive || suspensionRequested)) { if (playerActive) ProcessRoundResult(); Save(); } }
        private void OnApplicationQuit() { FinishOrAbort(); }
        private void OnDisable() { FinishOrAbort(); }

        private void FinishOrAbort()
        {
            if (!playerActive || sim == null || market == null) return;
            ProcessRoundResult();
            if (sim.Phase == RoundPhase.Playing) market.AbortAttempt();
            Save();
        }

        public bool ProcessRoundResult()
        {
            if ((!playerActive && !suspensionRequested) || sim == null || save == null || resultSaved ||
                (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost)) return false;
            StreetMarketResult receipt = market?.SettleAttempt(sim);
            if (market != null && market.Data.attemptActive) return false; // Refuse progression if settlement was rejected.
            if (receipt == null) return false;
            int previousUnlocked = save.unlockedLevel;
            if (sim.Phase == RoundPhase.Won)
            {
                int nextLevel = sim.LevelIndex + 1;
                if (market != null && nextLevel < StreetSimulation.LevelNames.Length)
                {
                    // Keep bounded legacy recovery for older receipts; current victories pay in full.
                    // The normal first-unlock grant covers only the remaining next-stock deficit.
                    var nextLevelSimulation = new StreetSimulation(balance, nextLevel, sim.Price);
                    market.ReconcileLegacyProgressionBalance(nextLevelSimulation, nextLevel);
                    market.GrantFirstUnlockStockBonus(nextLevelSimulation, nextLevel > previousUnlocked);
                }
                save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(StreetSimulation.LevelNames.Length - 1, sim.LevelIndex + 1));
            }
            if (save.pendingMatch == null)
            {
                save.pendingMatch = BuildMatchRecord(receipt);
                save.statistics.ApplyMatch(save.pendingMatch);
            }
            resultSaved = SaveWithMatch(save.pendingMatch);
            if (!resultSaved) save.unlockedLevel = previousUnlocked;
            else { ResetAttemptMetrics(); activeAttemptId = null; save.activeAttemptId = null; }
            return resultSaved;
        }

        private bool EnsureResultSaved() => resultSaved ||
            (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost) || ProcessRoundResult();

        public bool SelectLevel(int level)
        {
            if (!playerActive || sim == null || sim.Phase == RoundPhase.Playing || level < 0 ||
                level >= StreetSimulation.LevelNames.Length || level > UnlockedLevel) return false;
            if (!EnsureResultSaved()) return false;
            selectedLevel = level;
            ResetAttemptMetrics();
            sim = NewReadySimulation(level); resultSaved = false;
            InitializeDeliverySound(); Save(); return true;
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
            if (!playerActive || !CanStartRound) return false;
            string previousMarket = JsonUtility.ToJson(market.Data);
            market.EnsureLevelOneStartingStock(sim);
            if (!market.BeginAttempt(sim))
            {
                RestoreMarketAfterFailedStart(previousMarket);
                return false;
            }
            activeAttemptId = Guid.NewGuid().ToString("N");
            save.activeAttemptId = activeAttemptId;
            save.statistics.attemptsStarted++;
            // Persist the top-up and full pre-attempt snapshot before any delivery can consume stock.
            if (!Save())
            {
                save.statistics.attemptsStarted--;
                save.activeAttemptId = null; activeAttemptId = null;
                market.AbortAttempt();
                RestoreMarketAfterFailedStart(previousMarket);
                return false;
            }
            sim.BindIngredientStock(market);
            resultSaved = false; sim.StartRound(); Save(); return true;
        }

        private void RestoreMarketAfterFailedStart(string serializedMarket)
        {
            market = new StreetMarket(JsonUtility.FromJson<StreetMarketData>(serializedMarket), balance);
            save.market = market.Data;
        }
        public bool TryBuyIngredient(int ingredient, int packs, long operationId = 0)
        {
            if (!playerActive || market == null || sim == null || sim.Phase != RoundPhase.Ready || UnlockedLevel == 0) return false;
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
            if (!playerActive || sim == null || sim.Phase != RoundPhase.Won || sim.LevelIndex + 1 >= StreetSimulation.LevelNames.Length) return false;
            if (!EnsureResultSaved()) return false;
            selectedLevel = sim.LevelIndex + 1; save.unlockedLevel = Mathf.Max(save.unlockedLevel, selectedLevel);
            ResetAttemptMetrics();
            sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }
        public bool Retry()
        {
            return false; // Retained API for old callers, not a retry mechanic or navigation action.
        }
        public bool ReturnToLevels()
        {
            if (!playerActive || sim == null || market == null) return false;
            if (!EnsureResultSaved()) return false;
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
            else market.AbortAttempt();
            ResetAttemptMetrics();
            resultSaved = false; sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }

        /// <summary>Saves the authorized owner's state, then drops all live gameplay references.</summary>
        public bool SuspendPlayer()
        {
            if (!playerActive && !suspensionRequested) return true;
            if (sim != null && (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost) && !EnsureResultSaved())
            { playerActive = false; suspensionRequested = true; return false; }
            // Block simulation and all public gameplay commands even if durable save must be retried.
            playerActive = false; suspensionRequested = true;
            if (!Save()) return false;
            if (deliverySound != null) deliverySound.Initialize(null);
            sim = null; market = null; persist = null; activeAttemptId = null;
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
                if (!System.Text.RegularExpressions.Regex.IsMatch(json, "^\\s*\\{\\s*\\\"version\\\"\\s*:\\s*[1-4](?:\\s*[,}])")) return false;
                data = MigrateSaveData(JsonUtility.FromJson<SaveData>(json));
                // Unity serializes inline null DTOs as empty objects; never interpret them as results.
                if (data != null && StreetMatchRecord.IsAbsent(data.pendingMatch)) data.pendingMatch = null;
                if (data != null && StreetMatchRecord.IsAbsent(data.activeAttemptSummary)) data.activeAttemptSummary = null;
                if (data == null || data.version != 4) { data = null; return false; }
                data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 0, StreetSimulation.LevelNames.Length - 1);
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
            return data;
        }


        private SaveData DefaultSave() { return new SaveData { price = balance.initialPrice }; }

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
                initialCoinsCents = (long)balance.GetStartingCoins(level) * 100L,
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
                initialCoinsCents = (long)balance.GetStartingCoins(level) * 100L,
                leftoverCents = (long)Math.Max(0, sim.Coins) * 100L,
                customersAttended = sim.CustomersAttended, customersLost = sim.CustomersLost, ordersCompleted = sim.OrdersCompleted,
                salesByProduct = new long[7], stockConsumed = new long[10], stockRemaining = new long[10], hiresByRole = (long[])attemptHiresByRole.Clone()
            };
            for (int product = 0; product < 7; product++)
            {
                long delivered = sim.GetProductDelivered(product);
                record.salesByProduct[product] = delivered;
                foreach (int ingredient in StreetIngredientCatalog.Recipe(product))
                    record.stockConsumed[ingredient] += delivered;
            }
            for (int i = 0; i < 10; i++) record.stockRemaining[i] = market.Units(i);
            return record;
        }

        private static long At(int[] values, int index) => values != null && index >= 0 && index < values.Length ? Math.Max(0, values[index]) : 0;

        private bool Save() => SaveWithMatch(null);

        private bool SaveWithMatch(StreetMatchRecord match)
        {
            if (sim == null || save == null || (!playerActive && !suspensionRequested)) return false;
            if(save.prices==null || save.prices.Length!=7)save.prices=new float[7];
            for(int i=0;i<7;i++)save.prices[i]=sim.GetProductPrice(i);
            save.price = sim.Price; save.coins = sim.Coins; save.staff = sim.StaffCount; save.speed = sim.SpeedLevel;
            save.workerLevel = sim.LevelIndex; save.parrilleros = sim.ParrilleroCount; save.cocacoleros = sim.CocacoleroCount;
            save.premiumParrilleros = sim.ParrilleroPremiumCount; save.ferneteros = sim.FerneteroCount; save.mozos = sim.MozoCount;
            save.unlockedLevel = Mathf.Max(save.unlockedLevel, sim.LevelIndex);
            save.market = market?.Data ?? save.market;
            if (save.pendingMatch != null && save.pendingMatch.won)
                save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(StreetSimulation.LevelNames.Length - 1, save.pendingMatch.level + 1));
            StreetMatchRecord pendingToRestore = null;
            StreetMatchRecord summaryToRestore = null;
            string activeAttemptToRestore = null;
            try
            {
                StreetMatchRecord outputMatch = match ?? save.pendingMatch;
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
