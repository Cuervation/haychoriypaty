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
        // Stable product-major index: productId * 3 + tier (500/2500/5000).
        // Additive v1 field; obtained medals are durable, not just a render-time projection.
        public bool[] earnedProductSalesBadges = new bool[StreetSalesBadges.BadgeCount];
        public long[] paidHiresByRole = new long[5];
        public long[] hireSpendCentsByRole = new long[5];
        public long grossSalesCents, totalMarketRewardsCents, ingredientSpendTotalCents;
        public long hireSpendTotalCents, speedSpendTotalCents, initialFreeCoinsCents, initialFreeMarketBalanceCents;
        public long speedUpgrades, speedSpendCents, attemptsStarted, attemptsFinished, wins, losses, abandons;
        public long customersAttended, customersLost, ordersCompleted, completedProducts;
        public long[] winsByLevel = new long[13], lossesByLevel = new long[13];
        public float[] bestWinTimeByLevel = new float[13];
        public long[] bestGrossCentsByLevel = new long[13], bestUnitsSoldByLevel = new long[13];
        // Stable ID10 used to be traditional Los Redondos. Keep its historic level-specific records
        // separate now that the same campaign ID is a Tower Defense mode.
        public long legacyTraditionalLosRedondosWins, legacyTraditionalLosRedondosLosses;
        public float legacyTraditionalLosRedondosBestWinTime;
        public long legacyTraditionalLosRedondosBestGrossCents, legacyTraditionalLosRedondosBestUnitsSold;
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
            AwardProductSalesBadges();
            paidHiresByRole = Normalize(paidHiresByRole, 5);
            hireSpendCentsByRole = Normalize(hireSpendCentsByRole, 5);
            winsByLevel = Normalize(winsByLevel, 13); lossesByLevel = Normalize(lossesByLevel, 13);
            bestWinTimeByLevel = Normalize(bestWinTimeByLevel, 13);
            bestGrossCentsByLevel = Normalize(bestGrossCentsByLevel, 13);
            bestUnitsSoldByLevel = Normalize(bestUnitsSoldByLevel, 13);
            legacyTraditionalLosRedondosWins = Math.Max(0, legacyTraditionalLosRedondosWins);
            legacyTraditionalLosRedondosLosses = Math.Max(0, legacyTraditionalLosRedondosLosses);
            legacyTraditionalLosRedondosBestWinTime = Math.Max(0, legacyTraditionalLosRedondosBestWinTime);
            legacyTraditionalLosRedondosBestGrossCents = Math.Max(0, legacyTraditionalLosRedondosBestGrossCents);
            legacyTraditionalLosRedondosBestUnitsSold = Math.Max(0, legacyTraditionalLosRedondosBestUnitsSold);
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

        /// <summary>Idempotently separates old traditional ID10 records from the new TD level-10 bucket.</summary>
        public void ArchiveLegacyTraditionalLosRedondosRecords()
        {
            Normalize();
            const int legacyId = 10;
            legacyTraditionalLosRedondosWins = Math.Max(legacyTraditionalLosRedondosWins, winsByLevel[legacyId]);
            legacyTraditionalLosRedondosLosses = Math.Max(legacyTraditionalLosRedondosLosses, lossesByLevel[legacyId]);
            float archivedBest = bestWinTimeByLevel[legacyId];
            if (archivedBest > 0f && (legacyTraditionalLosRedondosBestWinTime <= 0f || archivedBest < legacyTraditionalLosRedondosBestWinTime))
                legacyTraditionalLosRedondosBestWinTime = archivedBest;
            legacyTraditionalLosRedondosBestGrossCents = Math.Max(legacyTraditionalLosRedondosBestGrossCents, bestGrossCentsByLevel[legacyId]);
            legacyTraditionalLosRedondosBestUnitsSold = Math.Max(legacyTraditionalLosRedondosBestUnitsSold, bestUnitsSoldByLevel[legacyId]);
            winsByLevel[legacyId] = lossesByLevel[legacyId] = 0;
            bestWinTimeByLevel[legacyId] = 0;
            bestGrossCentsByLevel[legacyId] = bestUnitsSoldByLevel[legacyId] = 0;
        }

        public bool HasApplied(string attemptId)
        {
            return !string.IsNullOrEmpty(attemptId) && lastAppliedAttemptId == attemptId;
        }

        /// <summary>Applies one settled result exactly once. The record and aggregates are serialized together.</summary>
        public bool ApplyMatch(StreetMatchRecord record)
        {
            return ApplyMatchInternal(record, false);
        }

        public bool ApplyLegacyTraditionalLosRedondosMatch(StreetMatchRecord record)
        {
            return record != null && record.level == 10 && ApplyMatchInternal(record, true);
        }

        private bool ApplyMatchInternal(StreetMatchRecord record, bool legacyTraditionalLosRedondos)
        {
            if (record == null || string.IsNullOrEmpty(record.attemptId) || HasApplied(record.attemptId)) return false;
            Normalize();
            int level = Clamp(record.level, 0, 12);
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
            long priorGross = legacyTraditionalLosRedondos ? legacyTraditionalLosRedondosBestGrossCents : bestGrossCentsByLevel[level];
            long priorUnits = legacyTraditionalLosRedondos ? legacyTraditionalLosRedondosBestUnitsSold : bestUnitsSoldByLevel[level];
            if (!record.abandoned && record.grossSalesCents > priorGross) newRecords++;
            if (!record.abandoned && units > priorUnits) newRecords++;
            if (!record.abandoned && record.grossSalesCents > priorGross)
            {
                if (legacyTraditionalLosRedondos) legacyTraditionalLosRedondosBestGrossCents = record.grossSalesCents;
                else bestGrossCentsByLevel[level] = record.grossSalesCents;
            }
            if (!record.abandoned && units > priorUnits)
            {
                if (legacyTraditionalLosRedondos) legacyTraditionalLosRedondosBestUnitsSold = units;
                else bestUnitsSoldByLevel[level] = units;
            }
            if (!record.abandoned && record.won)
            {
                wins = Add(wins, 1);
                if (legacyTraditionalLosRedondos) legacyTraditionalLosRedondosWins = Add(legacyTraditionalLosRedondosWins, 1);
                else winsByLevel[level] = Add(winsByLevel[level], 1);
                float priorBest = legacyTraditionalLosRedondos ? legacyTraditionalLosRedondosBestWinTime : bestWinTimeByLevel[level];
                if (priorBest <= 0 || record.elapsedSeconds < priorBest)
                {
                    if (legacyTraditionalLosRedondos) legacyTraditionalLosRedondosBestWinTime = record.elapsedSeconds;
                    else bestWinTimeByLevel[level] = record.elapsedSeconds;
                    newRecords++;
                }
            }
            else if (!record.abandoned)
            {
                losses = Add(losses, 1);
                if (legacyTraditionalLosRedondos) legacyTraditionalLosRedondosLosses = Add(legacyTraditionalLosRedondosLosses, 1);
                else lossesByLevel[level] = Add(lossesByLevel[level], 1);
            }
            record.recordsReached = newRecords;
            AwardProductSalesBadges();
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
            AwardProductSalesBadges();
            lastRecoveredAttemptId = summary.attemptId;
            return true;
        }

        private void AwardProductSalesBadges()
        {
            if (earnedProductSalesBadges == null || earnedProductSalesBadges.Length != StreetSalesBadges.BadgeCount)
            {
                var previous = earnedProductSalesBadges;
                earnedProductSalesBadges = new bool[StreetSalesBadges.BadgeCount];
                if (previous != null) Array.Copy(previous, earnedProductSalesBadges, Math.Min(previous.Length, earnedProductSalesBadges.Length));
            }
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
            for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                if (StreetSalesBadges.IsEarned(At(deliveredByProduct, product), tier))
                    earnedProductSalesBadges[product * StreetSalesBadges.TierCount + tier] = true;
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
        public int bonusScore, bonusStars, bonusDeliveries, bonusCompletedOrders, bonusTickets;
        public string bonusUpgrades = string.Empty;
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
