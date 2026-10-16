using System;

namespace HayChoriYPaty
{
    /// <summary>Bounded domain counters; this type contains no storage or cloud dependencies.</summary>
    [Serializable]
    public sealed class StreetPlayerStatistics
    {
        public int schemaVersion = 1;
        public string instrumentationStartedAtUtc;
        public bool legacyCoverageIncomplete;
        public long knownLegacyIngredientSpendCents, knownLegacyPurchaseUnits;
        public long[] purchasedUnitsByIngredient = new long[10];
        public long[] purchaseSpendCentsByIngredient = new long[10];
        public long[] consumedUnitsByIngredient = new long[10];
        public long[] deliveredByProduct = new long[7];
        public long[] paidHiresByRole = new long[5];
        public long[] hireSpendCentsByRole = new long[5];
        public long grossSalesCents, totalMarketRewardsCents, ingredientSpendTotalCents;
        public long hireSpendTotalCents, speedSpendTotalCents, initialFreeCoinsCents, initialFreeMarketBalanceCents;
        public long speedUpgrades, speedSpendCents, attemptsStarted, attemptsFinished, wins, losses, abandons;
        public long customersAttended, customersLost, ordersCompleted, completedProducts;
        public long[] winsByLevel = new long[11], lossesByLevel = new long[11];
        public float[] bestWinTimeByLevel = new float[11];
        public long[] bestGrossCentsByLevel = new long[11], bestUnitsSoldByLevel = new long[11];
        // Current result retry token; the cloud outbox owns the durable history of acknowledgements.
        public string lastAppliedAttemptId;
        // Recovery token for a live-attempt checkpoint; this never represents a finished result.
        public string lastRecoveredAttemptId;

        public void Normalize()
        {
            purchasedUnitsByIngredient = Normalize(purchasedUnitsByIngredient, 10);
            purchaseSpendCentsByIngredient = Normalize(purchaseSpendCentsByIngredient, 10);
            consumedUnitsByIngredient = Normalize(consumedUnitsByIngredient, 10);
            deliveredByProduct = Normalize(deliveredByProduct, 7);
            paidHiresByRole = Normalize(paidHiresByRole, 5);
            hireSpendCentsByRole = Normalize(hireSpendCentsByRole, 5);
            winsByLevel = Normalize(winsByLevel, 11); lossesByLevel = Normalize(lossesByLevel, 11);
            bestWinTimeByLevel = Normalize(bestWinTimeByLevel, 11);
            bestGrossCentsByLevel = Normalize(bestGrossCentsByLevel, 11);
            bestUnitsSoldByLevel = Normalize(bestUnitsSoldByLevel, 11);
            speedUpgrades = Math.Max(0, speedUpgrades); speedSpendCents = Math.Max(0, speedSpendCents);
            grossSalesCents = Math.Max(0, grossSalesCents); totalMarketRewardsCents = Math.Max(0, totalMarketRewardsCents);
            ingredientSpendTotalCents = Math.Max(0, ingredientSpendTotalCents);
            hireSpendTotalCents = Math.Max(0, hireSpendTotalCents); speedSpendTotalCents = Math.Max(0, speedSpendTotalCents);
            initialFreeCoinsCents = Math.Max(0, initialFreeCoinsCents); initialFreeMarketBalanceCents = Math.Max(0, initialFreeMarketBalanceCents);
            attemptsStarted = Math.Max(0, attemptsStarted); attemptsFinished = Math.Max(0, attemptsFinished);
            wins = Math.Max(0, wins); losses = Math.Max(0, losses); abandons = Math.Max(0, abandons);
            customersAttended = Math.Max(0, customersAttended); customersLost = Math.Max(0, customersLost);
            ordersCompleted = Math.Max(0, ordersCompleted); completedProducts = Math.Max(0, completedProducts);
        }

        public bool HasApplied(string attemptId)
        {
            return !string.IsNullOrEmpty(attemptId) && lastAppliedAttemptId == attemptId;
        }

        /// <summary>Applies one settled result exactly once. The record and aggregates are serialized together.</summary>
        public bool ApplyMatch(StreetMatchRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.attemptId) || HasApplied(record.attemptId)) return false;
            Normalize();
            int level = Clamp(record.level, 0, 10);
            long units = 0;
            for (int i = 0; i < 7; i++) { long n = At(record.salesByProduct, i); deliveredByProduct[i] = Add(deliveredByProduct[i], n); units = Add(units, n); }
            for (int i = 0; i < 10; i++) consumedUnitsByIngredient[i] = Add(consumedUnitsByIngredient[i], At(record.stockConsumed, i));
            grossSalesCents = Add(grossSalesCents, record.grossSalesCents);
            totalMarketRewardsCents = Add(totalMarketRewardsCents, record.rewardCents);
            initialFreeCoinsCents = Add(initialFreeCoinsCents, record.initialCoinsCents);
            customersAttended = Add(customersAttended, record.customersAttended);
            customersLost = Add(customersLost, record.customersLost);
            ordersCompleted = Add(ordersCompleted, record.ordersCompleted);
            completedProducts = Add(completedProducts, units);
            if (record.abandoned) abandons = Add(abandons, 1);
            else attemptsFinished = Add(attemptsFinished, 1);
            int newRecords = 0;
            if (!record.abandoned && record.grossSalesCents > bestGrossCentsByLevel[level]) newRecords++;
            if (!record.abandoned && units > bestUnitsSoldByLevel[level]) newRecords++;
            if (!record.abandoned && record.grossSalesCents > bestGrossCentsByLevel[level]) bestGrossCentsByLevel[level] = record.grossSalesCents;
            if (!record.abandoned && units > bestUnitsSoldByLevel[level]) bestUnitsSoldByLevel[level] = units;
            if (!record.abandoned && record.won)
            {
                wins = Add(wins, 1); winsByLevel[level] = Add(winsByLevel[level], 1);
                if (bestWinTimeByLevel[level] <= 0 || record.elapsedSeconds < bestWinTimeByLevel[level]) { bestWinTimeByLevel[level] = record.elapsedSeconds; newRecords++; }
            }
            else if (!record.abandoned) { losses = Add(losses, 1); lossesByLevel[level] = Add(lossesByLevel[level], 1); }
            record.recordsReached = newRecords;
            lastAppliedAttemptId = record.attemptId;
            return true;
        }

        /// <summary>Recovers only actual live-attempt telemetry after stock rollback, never an outcome.</summary>
        public bool ApplyRecoveredAttemptMetrics(StreetMatchRecord summary)
        {
            if (summary == null || string.IsNullOrEmpty(summary.attemptId) ||
                lastRecoveredAttemptId == summary.attemptId || HasApplied(summary.attemptId)) return false;
            Normalize();
            long units = 0;
            for (int i = 0; i < 7; i++)
            {
                long delivered = At(summary.salesByProduct, i);
                deliveredByProduct[i] = Add(deliveredByProduct[i], delivered);
                units = Add(units, delivered);
            }
            for (int i = 0; i < 10; i++)
                consumedUnitsByIngredient[i] = Add(consumedUnitsByIngredient[i], At(summary.stockConsumed, i));
            grossSalesCents = Add(grossSalesCents, summary.grossSalesCents);
            customersAttended = Add(customersAttended, summary.customersAttended);
            customersLost = Add(customersLost, summary.customersLost);
            ordersCompleted = Add(ordersCompleted, summary.ordersCompleted);
            completedProducts = Add(completedProducts, units);
            lastRecoveredAttemptId = summary.attemptId;
            return true;
        }

        private static long[] Normalize(long[] values, int length)
        {
            var result = new long[length]; if (values != null) Array.Copy(values, result, Math.Min(values.Length, length));
            for (int i = 0; i < length; i++) result[i] = Math.Max(0, result[i]); return result;
        }
        private static float[] Normalize(float[] values, int length)
        { var result = new float[length]; if (values != null) Array.Copy(values, result, Math.Min(values.Length, length)); return result; }
        private static long At(long[] values, int i) => values != null && i < values.Length ? Math.Max(0, values[i]) : 0;
        private static long Add(long a, long b) => b > 0 && a > long.MaxValue - b ? long.MaxValue : a + Math.Max(0, b);
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    }

    [Serializable]
    public sealed class StreetMatchRecord
    {
        public static bool IsAbsent(StreetMatchRecord record) => record == null ||
            (string.IsNullOrEmpty(record.id) && string.IsNullOrEmpty(record.attemptId));

        public string id, attemptId, utc;
        public int level;
        public string club, result;
        public bool won, abandoned;
        public float elapsedSeconds;
        public long[] salesByProduct = new long[7];
        public long grossSalesCents, hireSpendCents, speedSpendCents, initialCoinsCents, leftoverCents;
        public long halfSalesRewardCents, leftoverBonusCents, progressionBonusCents, rewardCents;
        public long[] stockConsumed = new long[10], stockRemaining = new long[10];
        public long[] hiresByRole = new long[5];
        public long customersAttended, customersLost, ordersCompleted;
        public int recordsReached;
    }
}
