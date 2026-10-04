using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Inspector tuning for the first Floresta slice; values are intentionally data, not rules.</summary>
    [System.Serializable]
    public sealed class GameBalance
    {
        [Min(1f)] public float roundDurationSeconds = 150f;
        [Min(1)] public int customersToWin = 12;
        [Min(0.5f)] public float arrivalIntervalSeconds = 5.2f;
        [Min(1f)] public float customerPatienceSeconds = 46f;
        [Min(0f)] public float crowdPressurePerSecond = 0.06f;
        [Min(0f)] public float crowdDeparturePenalty = 15f;
        [Range(0f, 100f)] public float startingCrowdPatience = 100f;
        [Min(0f)] public int startingCoins = 16;
        [Min(0f)] public int hireCost = 30;
        [Min(0f)] public int speedUpgradeCost = 24;
        [Min(0f)] public int coinsPerOrder = 18;
        [Range(1, 4)] public int grillCapacity = 1;
        [Range(2, 12)] public int maxQueueCapacity = 6;
        [Min(0.5f)] public float choriCookSeconds = 8f;
        [Min(0.1f)] public float condimentSeconds = 1.4f;
        [Min(0.1f)] public float handoffSeconds = 1.1f;
        [Min(0f)] public float helperCookBonus = 0.35f;
        [Min(0f)] public float speedUpgradeCookBonus = 0.2f;
        [Min(0f)] public float speedUpgradeHandoffBonus = 0.45f;
        [Range(0, 5)] public int maxSpeedUpgrades = 3;
    }
}
