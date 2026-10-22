using System;

namespace HayChoriYPaty
{
    /// <summary>Deterministic seven-sector victory reward; no random or wall-clock payout selection.</summary>
    public static class StreetChulengo
    {
        public const int SectorCount = 7;
        private static readonly int[] Numerators = { 0, 3, 2, 4, 2, 3, 0 };
        public static int MultiplierNumerator(int sector)
        {
            if (sector < 0 || sector >= SectorCount) throw new ArgumentOutOfRangeException(nameof(sector));
            return Numerators[sector];
        }
        public static int SectorAtPosition(float position)
        {
            if (float.IsNaN(position) || float.IsInfinity(position)) return -1;
            return Math.Min(SectorCount - 1, (int)(Math.Max(0f, Math.Min(1f, position)) * SectorCount));
        }
        public static float PositionAtTime(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return 0f;
            double phase = Math.Max(0d, seconds) % 2.8d / 1.4d;
            return (float)(phase <= 1d ? phase : 2d - phase);
        }
        public static long ScaleCents(long cents, int numerator)
        {
            if (cents < 0 || numerator < 0 || numerator > 4) throw new ArgumentOutOfRangeException();
            return checked((cents / 2L) * numerator + (cents % 2L) * numerator / 2L);
        }
    }
}
