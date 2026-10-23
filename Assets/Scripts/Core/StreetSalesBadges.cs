using System;

namespace HayChoriYPaty
{
    /// <summary>Fixed achievement catalog; earned state is saved alongside Firebase-backed delivery totals.</summary>
    public static class StreetSalesBadges
    {
        public const int ProductCount = 7, TierCount = 3, BadgeCount = ProductCount * TierCount;
        public static long Threshold(int tier)
        {
            switch (tier) { case 0: return 500L; case 1: return 2500L; case 2: return 5000L;
                default: throw new ArgumentOutOfRangeException(nameof(tier)); }
        }
        public static int EarnedCount(long sales)
        {
            int count = 0;
            for (int tier = 0; tier < TierCount; tier++) if (sales >= Threshold(tier)) count++;
            return count;
        }
        public static bool IsEarned(long sales, int tier) => sales >= Threshold(tier);
    }
}
