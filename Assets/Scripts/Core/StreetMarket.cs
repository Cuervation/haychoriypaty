using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    [Serializable]
    public sealed class StreetMarketBalance
    {
        public int[] packPricesCents = { 150, 100, 150, 100, 400, 500, 250, 350, 1600, 800 };
        public long startingBalanceCents;
    }

    /// <summary>Stable ingredient and seven-product recipe authority.</summary>
    public static class StreetIngredientCatalog
    {
        public static readonly string[] Names = { "Chorizo", "Pan común", "Paty", "Pan Paty", "Bondiola", "Vacío", "Coca", "Cerveza", "Fernet", "Coca grande (2,25 L)" };
        public static readonly int[] PackUnits = { 1, 1, 1, 1, 1, 1, 1, 1, 4, 4 };
        private static readonly int[][] Recipes = { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4, 1 }, new[] { 5, 1 }, new[] { 6 }, new[] { 8, 9 }, new[] { 7 } };
        public static int[] Recipe(int product) { return product >= 0 && product < Recipes.Length ? (int[])Recipes[product].Clone() : new int[0]; }
    }

    [Serializable]
    public sealed class StreetMarketResult
    {
        public bool won;
        public int level;
        public long grossSalesCents, leftoverCents, halfSalesCents, recognizedSalesCents, bonusCents, progressionBonusCents, earnedCents, totalBalanceCents;
        public int[] consumedUnits, remainingUnits;
    }

    [Serializable]
    public sealed class StreetMarketData
    {
        public long balanceCents, nextPurchaseId = 1, lastPurchaseId, cumulativePurchaseUnits, cumulativePurchaseSpendCents;
        public long nextAttemptId = 1, attemptId, settledAttemptId;
        public int attemptLevel = -1, initializedUnlockedLevel = -1;
        public int[] stockUnits = new int[10], attemptStock;
        public long[] highestRecognizedSalesByLevel = new long[11];
        public bool[] bonusPaidByLevel = new bool[11];
        public bool attemptActive, kitGranted, legacyTransitionRepairApplied, legacyZeroBalanceRecoveryApplied;
        public int victoryPayoutVersion; // Additive v4 compatibility: latest receipt reconciled once.
        public StreetMarketResult lastResult;
    }

    public interface IStreetKitchenStock
    {
        bool CanPrepareFood(int product);
        bool TryReserveFood(int foodId, int product);
        bool ConsumeReservedFood(int foodId, int product);
        void ReleaseFood(int foodId);
    }

    /// <summary>Persistent cent-exact market economy embedded in the StreetGame save.</summary>
    public sealed class StreetMarket : IStreetKitchenStock
    {
        public const int LevelOneStarterStockFloor = 200;
        private sealed class Reservation { public int product; public int[] recipe; }
        public StreetMarketData Data { get; private set; }
        public StreetBalance Balance { get; private set; }
        public long BalanceCents { get { return Data.balanceCents; } }
        public long NextPurchaseId { get { return Data.nextPurchaseId; } }
        public StreetMarketResult LastResult { get { return Data.lastResult; } }
        private readonly Dictionary<int, Reservation> reservations = new Dictionary<int, Reservation>();
        private readonly int[] attemptConsumed = new int[10];
        private readonly int[] consumedProducts = new int[7];
        private StreetSimulation attemptSimulation;
        private StreetBalance unlockCacheBalance;
        private int unlockCacheLevel = -1;
        private bool[] unlockedIngredients = new bool[10];
        private static readonly int[] DefaultPackPrices = { 150, 100, 150, 100, 400, 500, 250, 350, 1600, 800 };

        public StreetMarket(StreetMarketData data, StreetBalance balance)
        {
            Data = data ?? new StreetMarketData(); Balance = balance ?? new StreetBalance();
            Data.stockUnits = Normalize(Data.stockUnits, 10);
            Data.highestRecognizedSalesByLevel = Normalize(Data.highestRecognizedSalesByLevel, 11);
            Data.bonusPaidByLevel = Normalize(Data.bonusPaidByLevel, 11);
            Data.attemptStock = Data.attemptStock == null ? null : Normalize(Data.attemptStock, 10);
            Data.balanceCents = Math.Max(0L, Data.balanceCents);
            Data.cumulativePurchaseUnits = Math.Max(0L, Data.cumulativePurchaseUnits);
            Data.cumulativePurchaseSpendCents = Math.Max(0L, Data.cumulativePurchaseSpendCents);
            Data.nextPurchaseId = Math.Max(1L, Data.nextPurchaseId); Data.nextAttemptId = Math.Max(1L, Data.nextAttemptId);
            if (Data.lastResult != null && Data.lastResult.earnedCents == 0)
                Data.lastResult.earnedCents = Math.Max(0L, Data.lastResult.recognizedSalesCents) + Math.Max(0L, Data.lastResult.bonusCents) + Math.Max(0L, Data.lastResult.progressionBonusCents);
        }

        public int Units(int id) { return id >= 0 && id < 10 ? Data.stockUnits[id] : 0; }
        public int PackPriceCents(int id)
        {
            int[] prices = Balance.market == null ? null : Balance.market.packPricesCents;
            if (id < 0 || id >= 10) return 0;
            return prices != null && id < prices.Length ? Math.Max(0, prices[id]) : DefaultPackPrices[id];
        }

        public bool IsIngredientUnlocked(int id, int unlockedLevel)
        {
            if (id < 0 || id >= 10 || unlockedLevel < 0) return false;
            int last = Math.Min(unlockedLevel, StreetSimulation.LevelNames.Length - 1);
            if (unlockCacheBalance != Balance || unlockCacheLevel != last)
            {
                Array.Clear(unlockedIngredients, 0, unlockedIngredients.Length);
                for (int level = 0; level <= last; level++)
                {
                    StreetSimulation sim = new StreetSimulation(Balance, level, 5f);
                    for (int slot = 0; slot < sim.ProductCount; slot++)
                        foreach (int ingredient in StreetIngredientCatalog.Recipe(sim.GetAvailableProduct(slot))) unlockedIngredients[ingredient] = true;
                }
                unlockCacheBalance = Balance; unlockCacheLevel = last;
            }
            return unlockedIngredients[id];
        }

        public int[] RequiredUnits(StreetSimulation sim)
        {
            int[] result = new int[10];
            if (sim == null) return result;
            for (int slot = 0; slot < sim.ProductCount; slot++)
            {
                int product = sim.GetAvailableProduct(slot), goal = Math.Max(0, sim.GetProductGoal(product));
                foreach (int ingredient in StreetIngredientCatalog.Recipe(product)) result[ingredient] = SafeUnitAdd(result[ingredient], goal);
            }
            return result;
        }

        public int MissingUnits(StreetSimulation sim, int id) { return id < 0 || id >= 10 ? 0 : Math.Max(0, RequiredUnits(sim)[id] - Units(id)); }
        public int MissingPacks(StreetSimulation sim, int id)
        {
            int missing = MissingUnits(sim, id);
            return missing == 0 ? 0 : (int)Math.Min(int.MaxValue, ((long)missing + StreetIngredientCatalog.PackUnits[id] - 1L) / StreetIngredientCatalog.PackUnits[id]);
        }
        public bool HasRequiredStock(StreetSimulation sim)
        {
            int[] required = RequiredUnits(sim);
            for (int i = 0; i < 10; i++) if (Units(i) < required[i]) return false;
            return true;
        }

        public bool CanMeetRequiredStockWithLevelOneFloor(StreetSimulation sim)
        {
            if (sim == null) return false;
            int[] required = RequiredUnits(sim);
            int[] levelOneRecipe = sim.LevelIndex == 0 ? StreetIngredientCatalog.Recipe(0) : new int[0];
            for (int i = 0; i < 10; i++)
            {
                int available = Units(i);
                if (Array.IndexOf(levelOneRecipe, i) >= 0) available = Math.Max(available, LevelOneStarterStockFloor);
                if (available < required[i]) return false;
            }
            return true;
        }

        public bool EnsureLevelOneStartingStock(StreetSimulation sim)
        {
            if (sim == null || sim.LevelIndex != 0) return false;
            bool changed = false;
            foreach (int ingredient in StreetIngredientCatalog.Recipe(0))
            {
                if (Data.stockUnits[ingredient] >= LevelOneStarterStockFloor) continue;
                Data.stockUnits[ingredient] = LevelOneStarterStockFloor;
                changed = true;
            }
            return changed;
        }
        public long MinimumPurchaseCostCents(StreetSimulation sim)
        {
            long sum = 0;
            for (int i = 0; i < 10; i++) if (!TryAdd(sum, (long)MissingPacks(sim, i) * PackPriceCents(i), out sum)) return long.MaxValue;
            return sum;
        }

        /// <summary>First migration grants the level-one kit. Older progress gets its high-level kit once and reward watermarks.</summary>
        public void Initialize(int unlockedLevel)
        {
            unlockedLevel = Math.Max(0, Math.Min(StreetSimulation.LevelNames.Length - 1, unlockedLevel));
            if (Data.kitGranted && Data.initializedUnlockedLevel >= 0) return;
            if (BalanceCents == 0 && Balance.market != null) Data.balanceCents = Math.Max(0L, Balance.market.startingBalanceCents);
            int kitLevel = unlockedLevel > 0 ? unlockedLevel : 0;
            int[] kit = RequiredUnits(new StreetSimulation(Balance, kitLevel, 5f));
            if (!Data.kitGranted)
            {
                for (int i = 0; i < 10; i++) Data.stockUnits[i] = Math.Max(Data.stockUnits[i], kit[i]);
                Data.kitGranted = true;
            }
            if (unlockedLevel > 0)
                for (int level = 0; level < unlockedLevel; level++)
                {
                    StreetSimulation prior = new StreetSimulation(Balance, level, 5f);
                    long gross = EstimatedGrossSales(prior), reward = gross / 2;
                    Data.highestRecognizedSalesByLevel[level] = Math.Max(Data.highestRecognizedSalesByLevel[level], reward);
                    Data.bonusPaidByLevel[level] = true;
                }
            Data.initializedUnlockedLevel = unlockedLevel;
        }

        /// <summary>Grants the calculated opening kit only when StreetGame has no prior save.</summary>
        public void InitializeNewGameKit()
        {
            if (Data.kitGranted && Data.initializedUnlockedLevel >= 0) return;
            InitializeStartingBalance();
            int[] required = RequiredUnits(new StreetSimulation(Balance, 0, 5f));
            for (int i = 0; i < Data.stockUnits.Length; i++)
                Data.stockUnits[i] = Math.Max(Data.stockUnits[i], required[i]);
            Data.kitGranted = true;
            Data.initializedUnlockedLevel = 0;
        }

        /// <summary>Marks an existing persisted inventory initialized without topping it up or replacing it.</summary>
        public void InitializeExistingSave(int unlockedLevel)
        {
            unlockedLevel = Math.Max(0, Math.Min(StreetSimulation.LevelNames.Length - 1, unlockedLevel));
            if (Data.kitGranted && Data.initializedUnlockedLevel >= 0) return;
            InitializeStartingBalance();
            if (unlockedLevel > 0)
                for (int level = 0; level < unlockedLevel; level++)
                {
                    StreetSimulation prior = new StreetSimulation(Balance, level, 5f);
                    long reward = EstimatedGrossSales(prior) / 2;
                    Data.highestRecognizedSalesByLevel[level] = Math.Max(Data.highestRecognizedSalesByLevel[level], reward);
                    Data.bonusPaidByLevel[level] = true;
                }
            Data.kitGranted = true;
            Data.initializedUnlockedLevel = unlockedLevel;
        }

        /// <summary>
        /// Repairs a one-time legacy progression dead end when a completed level was watermarked as paid
        /// during migration but its Mercado balance was never credited. This is a bounded transition grant,
        /// not financing: it equals only the current missing-stock cost and is permanently marked in the save.
        /// </summary>
        public long ReconcileLegacyProgressionBalance(StreetSimulation nextLevelSimulation, int unlockedLevel)
        {
            if (Data.legacyTransitionRepairApplied || nextLevelSimulation == null || unlockedLevel <= 0 ||
                unlockedLevel >= StreetSimulation.LevelNames.Length || nextLevelSimulation.LevelIndex != unlockedLevel ||
                Data.attemptActive || BalanceCents != 0 || Data.cumulativePurchaseSpendCents != 0) return 0L;

            int completedLevel = unlockedLevel - 1;
            if (completedLevel < 0 || completedLevel >= Data.highestRecognizedSalesByLevel.Length ||
                !Data.bonusPaidByLevel[completedLevel]) return 0L;

            StreetSimulation completedSimulation = new StreetSimulation(Balance, completedLevel, 5f);
            long expectedRecognizedSales = EstimatedGrossSales(completedSimulation) / 2L;
            if (expectedRecognizedSales <= 0 || Data.highestRecognizedSalesByLevel[completedLevel] < expectedRecognizedSales) return 0L;

            StreetMarketResult previousResult = Data.lastResult;
            if (previousResult != null &&
                (previousResult.level != completedLevel || !previousResult.won || previousResult.earnedCents != 0 ||
                 previousResult.totalBalanceCents != 0 || previousResult.halfSalesCents <= 0)) return 0L;

            long missingCost = MinimumPurchaseCostCents(nextLevelSimulation);
            if (missingCost < 0 || missingCost == long.MaxValue) return 0L;

            // Set even when there is no shortage: this migration repair is never a recurring allowance.
            Data.legacyTransitionRepairApplied = true;
            if (missingCost == 0) return 0L;
            Data.balanceCents = missingCost;
            RecordProgressionBonus(missingCost);
            return missingCost;
        }

        /// <summary>One-time reward on a first level unlock that ensures the next match's minimum stock is affordable.</summary>
        public long GrantFirstUnlockStockBonus(StreetSimulation nextLevelSimulation, bool firstTimeUnlock)
        {
            if (!firstTimeUnlock || nextLevelSimulation == null || Data.attemptActive ||
                nextLevelSimulation.LevelIndex <= 0 || nextLevelSimulation.LevelIndex >= StreetSimulation.LevelNames.Length) return 0L;
            long required = MinimumPurchaseCostCents(nextLevelSimulation);
            if (required <= BalanceCents || required == long.MaxValue) return 0L;
            long bonus = required - BalanceCents;
            Data.balanceCents += bonus;
            RecordProgressionBonus(bonus);
            return bonus;
        }

        /// <summary>
        /// One-time compatibility recovery for v4 saves already stuck after the earlier migration repair
        /// was consumed. It requires a persisted successful zero-payout result and a zero wallet.
        /// </summary>
        public long RecoverLegacyZeroPayoutTransition()
        {
            StreetMarketResult result = Data.lastResult;
            if (Data.legacyZeroBalanceRecoveryApplied || !Data.legacyTransitionRepairApplied || Data.attemptActive ||
                BalanceCents != 0 || result == null || !result.won || result.earnedCents != 0 ||
                result.totalBalanceCents != 0 || result.halfSalesCents <= 0 || result.recognizedSalesCents != 0 || result.bonusCents != 0 ||
                result.level < 0 || result.level >= Data.highestRecognizedSalesByLevel.Length ||
                !Data.bonusPaidByLevel[result.level] || Data.highestRecognizedSalesByLevel[result.level] < result.halfSalesCents) return 0L;

            int nextLevel = result.level + 1;
            if (nextLevel >= StreetSimulation.LevelNames.Length) return 0L;
            long missingCost = MinimumPurchaseCostCents(new StreetSimulation(Balance, nextLevel, 5f));
            if (missingCost <= 0 || missingCost == long.MaxValue) return 0L;

            Data.legacyZeroBalanceRecoveryApplied = true;
            Data.balanceCents = missingCost;
            RecordProgressionBonus(missingCost);
            return missingCost;
        }

        /// <summary>
        /// Reconcile only the latest persisted, settled victory when upgrading the old per-level
        /// payout policy. Credit the recorded formula minus what its receipt already paid, once.
        /// Inventory, purchases, unlocks and prior restocking awards are not rewritten.
        /// </summary>
        public long RecoverLegacyVictoryPayout()
        {
            if (Data.victoryPayoutVersion >= 1 || Data.attemptActive) return 0L;
            StreetMarketResult result = Data.lastResult;
            if (result == null || !result.won) { Data.victoryPayoutVersion = 1; return 0L; }
            if (Data.attemptId <= 0 || Data.settledAttemptId != Data.attemptId ||
                result.level != Data.attemptLevel || result.level < 0 || result.level >= 11 ||
                result.grossSalesCents < 0 || result.leftoverCents < 0 || result.leftoverCents > long.MaxValue / 2 ||
                result.halfSalesCents != result.grossSalesCents / 2 ||
                result.recognizedSalesCents < 0 || result.recognizedSalesCents > result.halfSalesCents ||
                result.bonusCents < 0 || result.bonusCents > result.leftoverCents * 2 || result.progressionBonusCents < 0) return 0L;

            long previouslyPaid, recordedEarned, fullReward, nextBalance, nextEarned;
            if (!TryAdd(result.recognizedSalesCents, result.bonusCents, out previouslyPaid) ||
                !TryAdd(previouslyPaid, result.progressionBonusCents, out recordedEarned) || recordedEarned != result.earnedCents ||
                !TryAdd(result.halfSalesCents, result.leftoverCents * 2, out fullReward)) return 0L;
            long missing = fullReward - previouslyPaid;
            if (!TryAdd(Data.balanceCents, missing, out nextBalance) ||
                !TryAdd(result.earnedCents, missing, out nextEarned)) return 0L;

            Data.balanceCents = nextBalance;
            result.recognizedSalesCents = result.halfSalesCents;
            result.bonusCents = result.leftoverCents * 2;
            result.earnedCents = nextEarned;
            result.totalBalanceCents = nextBalance;
            Data.highestRecognizedSalesByLevel[result.level] = Math.Max(Data.highestRecognizedSalesByLevel[result.level], result.halfSalesCents);
            Data.bonusPaidByLevel[result.level] = true;
            Data.victoryPayoutVersion = 1;
            return missing;
        }

        private void RecordProgressionBonus(long amount)
        {
            if (amount <= 0 || Data.lastResult == null || !Data.lastResult.won) return;
            Data.lastResult.progressionBonusCents += amount;
            Data.lastResult.earnedCents += amount;
            Data.lastResult.totalBalanceCents = Data.balanceCents;
        }

        private void InitializeStartingBalance()
        {
            if (BalanceCents == 0 && Balance.market != null)
                Data.balanceCents = Math.Max(0L, Balance.market.startingBalanceCents);
        }

        public bool RecoverAbandonedAttempt()
        {
            if (!Data.attemptActive) return false;
            RestoreSnapshot(); Data.attemptActive = false; reservations.Clear(); attemptSimulation = null; return true;
        }
        private void RestoreSnapshot() { if (Data.attemptStock != null) Array.Copy(Data.attemptStock, Data.stockUnits, 10); }

        public bool TryBuy(StreetSimulation sim, int id, int packs, int unlocked, long operationId)
        {
            if (sim == null || sim.Phase != RoundPhase.Ready || Data.attemptActive || id < 0 || id >= 10 || packs <= 0 ||
                !IsIngredientUnlocked(id, unlocked) || operationId <= Data.lastPurchaseId || operationId != Data.nextPurchaseId) return false;
            long unitCount = (long)packs * StreetIngredientCatalog.PackUnits[id];
            long cost = (long)packs * PackPriceCents(id);
            if (unitCount > int.MaxValue - Units(id) || cost < 0 || cost > long.MaxValue - Data.cumulativePurchaseSpendCents ||
                unitCount > long.MaxValue - Data.cumulativePurchaseUnits) return false;
            long cash = Data.balanceCents;
            if (cost > cash || operationId == long.MaxValue) return false;
            Data.balanceCents = cash - cost; Data.stockUnits[id] += (int)unitCount;
            Data.lastPurchaseId = operationId; Data.nextPurchaseId = operationId + 1;
            Data.cumulativePurchaseUnits += unitCount; Data.cumulativePurchaseSpendCents += cost;
            return true;
        }

        public bool BeginAttempt(StreetSimulation sim)
        {
            if (sim == null || sim.Phase != RoundPhase.Ready || Data.attemptActive || !HasRequiredStock(sim) || Data.nextAttemptId == long.MaxValue) return false;
            Data.attemptId = Data.nextAttemptId++; Data.attemptLevel = sim.LevelIndex; Data.attemptStock = (int[])Data.stockUnits.Clone(); Data.attemptActive = true;
            attemptSimulation = sim; Array.Clear(attemptConsumed, 0, 10); Array.Clear(consumedProducts, 0, 7); reservations.Clear(); return true;
        }

        public StreetMarketResult SettleAttempt(StreetSimulation sim)
        {
            if (sim == null || !Data.attemptActive || sim != attemptSimulation || sim.LevelIndex != Data.attemptLevel ||
                (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost) || Data.settledAttemptId == Data.attemptId) return LastResult;
            bool won = sim.Phase == RoundPhase.Won;
            int level = Data.attemptLevel;
            long gross = (long)Math.Max(0, sim.CoinsEarned) * 100L;
            long half = gross / 2L;
            long watermark = Data.highestRecognizedSalesByLevel[level];
            // Every real victory earns its full reward. Only the persisted attempt ID deduplicates it.
            // Keep the existing incremental-sales policy on defeats, which have no leftover bonus.
            long recognized = won ? half : Math.Max(0L, half - watermark);
            long bonus = won ? (long)Math.Max(0, sim.Coins) * 200L : 0L;
            long earned = recognized + bonus;
            if (Data.balanceCents > long.MaxValue - earned) return null;
            if (!won) RestoreSnapshot();
            if (won) Data.bonusPaidByLevel[level] = true;
            Data.highestRecognizedSalesByLevel[level] = Math.Max(watermark, half);
            Data.balanceCents += earned;
            Data.lastResult = new StreetMarketResult { won = won, level = level, grossSalesCents = gross, leftoverCents = (long)Math.Max(0, sim.Coins) * 100L,
                halfSalesCents = half, recognizedSalesCents = recognized, bonusCents = bonus, earnedCents = earned,
                totalBalanceCents = Data.balanceCents, consumedUnits = (int[])attemptConsumed.Clone(), remainingUnits = (int[])Data.stockUnits.Clone() };
            Data.settledAttemptId = Data.attemptId; Data.attemptActive = false; reservations.Clear(); attemptSimulation = null; return Data.lastResult;
        }

        public void AbortAttempt() { if (Data.attemptActive) RestoreSnapshot(); Data.attemptActive = false; reservations.Clear(); attemptSimulation = null; }

        public bool TryReserveFood(int foodId, int product)
        {
            if (foodId <= 0 || reservations.ContainsKey(foodId) || !CanPrepareFood(product)) return false;
            reservations[foodId] = new Reservation { product = product, recipe = StreetIngredientCatalog.Recipe(product) }; return true;
        }

        public bool CanPrepareFood(int product)
        {
            if (!Data.attemptActive || attemptSimulation == null || product < 0 || product > 6 || !attemptSimulation.IsProductAvailable(product)) return false;
            int[] recipe = StreetIngredientCatalog.Recipe(product), protectedByOthers = new int[10];
            // Existing reservations consume physical stock regardless of product.
            foreach (var pair in reservations) AddQuota(protectedByOthers, pair.Value.product, 1);
            // Preserve all outstanding quotas for other products, not merely already reserved stock.
            for (int p = 0; p < 7; p++) if (p != product)
            {
                int remaining = Math.Max(0, attemptSimulation.GetProductRemaining(p) - ReservedFor(p));
                AddQuota(protectedByOthers, p, remaining);
            }
            foreach (int ingredient in recipe) if (Units(ingredient) - protectedByOthers[ingredient] <= 0) return false;
            return true;
        }

        public bool ConsumeReservedFood(int foodId, int product)
        {
            Reservation reservation;
            if (!Data.attemptActive || !reservations.TryGetValue(foodId, out reservation) || reservation.product != product) return false;
            foreach (int ingredient in reservation.recipe) if (Units(ingredient) <= 0) return false;
            foreach (int ingredient in reservation.recipe) { Data.stockUnits[ingredient]--; attemptConsumed[ingredient]++; }
            consumedProducts[product]++; reservations.Remove(foodId); return true;
        }
        public void ReleaseFood(int foodId) { reservations.Remove(foodId); }
        private int ReservedFor(int product) { int count=0; foreach(var r in reservations.Values) if(r.product==product) count++; return count; }
        private static void AddQuota(int[] target,int product,int portions)
        { foreach(int ingredient in StreetIngredientCatalog.Recipe(product)) target[ingredient]=SafeUnitAdd(target[ingredient],portions); }
        private static long EstimatedGrossSales(StreetSimulation sim)
        { long gross=0; for(int slot=0;slot<sim.ProductCount;slot++){int p=sim.GetAvailableProduct(slot); gross+=(long)sim.GetProductGoal(p)*(long)(sim.GetProductPrice(p)*100f); } return gross; }
        private static int[] Normalize(int[] source,int length)
        { int[] result=new int[length]; if(source!=null)Array.Copy(source,result,Math.Min(source.Length,length)); for(int i=0;i<length;i++)result[i]=Math.Max(0,result[i]); return result; }
        private static long[] Normalize(long[] source,int length)
        { long[] result=new long[length]; if(source!=null)Array.Copy(source,result,Math.Min(source.Length,length)); for(int i=0;i<length;i++)result[i]=Math.Max(0L,result[i]); return result; }
        private static bool[] Normalize(bool[] source,int length)
        { bool[] result=new bool[length]; if(source!=null)Array.Copy(source,result,Math.Min(source.Length,length)); return result; }
        private static bool TryAdd(long a,long b,out long value) { if(b>0&&a>long.MaxValue-b){value=0;return false;} value=a+b;return true; }
        private static int SafeUnitAdd(int a,int b) { long sum=(long)a+b;return sum>int.MaxValue?int.MaxValue:(int)Math.Max(0L,sum); }
    }
}
