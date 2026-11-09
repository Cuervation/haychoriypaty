using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Frozen shared economy for the two Tower Defense campaign levels. It intentionally does
    /// not read campaign rows: legacy balance edits must not silently rebalance this mode.
    /// </summary>
    public static class TowerDefenseEconomyProfile
    {
        public const int StartingCoins = 500;
        public const int ParrilleroBaseHireCost = 60;
        public const int CocacoleroBaseHireCost = 60;
        public const float SpeedCostMultiplier = 17f;
        public const int BalanceLevelId = TowerDefenseLevelConfig.CommonEconomicBalanceLevelId;
        private static readonly StreetBalance FrozenBalance = BuildBalanceSnapshot();

        /// <summary>
        /// Creates an isolated balance snapshot for legacy shared cost formulas. Every call
        /// returns independent mutable data, so neither campaign rows nor another run can mutate it.
        /// </summary>
        public static StreetBalance CreateBalanceSnapshot()
        {
            return BuildBalanceSnapshot();
        }

        private static StreetBalance BuildBalanceSnapshot()
        {
            var balance = new StreetBalance();
            var balances = balance.levelBalances == null ? new StreetLevelBalance[13] : (StreetLevelBalance[])balance.levelBalances.Clone();
            if (balances.Length <= BalanceLevelId)
            {
                var expanded = new StreetLevelBalance[BalanceLevelId + 1];
                System.Array.Copy(balances, expanded, balances.Length);
                balances = expanded;
            }

            balances[BalanceLevelId] = new StreetLevelBalance(
                new[] { 310, 220, 135, 125, 255, 110, 145 },
                new[] { ParrilleroBaseHireCost, CocacoleroBaseHireCost, 80, 70, 70 },
                SpeedCostMultiplier,
                StartingCoins);
            balance.levelBalances = balances;
            return balance;
        }

        public static int CalculateHireCost(StreetWorkerRole role, int workerCount, long ticketWorkers = 0)
        {
            return StreetSimulation.CalculateHireCost(FrozenBalance, BalanceLevelId, role, workerCount, ticketWorkers);
        }

        public static int CalculateSpeedCost(int speedLevel)
        {
            return StreetSimulation.CalculateSpeedCost(FrozenBalance, BalanceLevelId, speedLevel);
        }
    }
}
