namespace HayChoriYPaty
{
    /// <summary>Attempt-local presentation clock. Never settles or mutates a match.</summary>
    public sealed class StreetVictoryTransition
    {
        public const float Duration = 3f;
        public const float SmokeStart = .92f;
        public const float SmokeFullCoverage = 2.05f;
        public const float SmokeRelease = 2.16f;
        public const float ChulengoRevealStart = 2.08f;
        private object attempt;
        private bool sawPlaying, handledWin, started;
        private float startedAt;

        public void Observe(object currentAttempt, bool playing, bool won, float unscaledNow)
        {
            if (!ReferenceEquals(attempt, currentAttempt) || (!playing && !won))
            {
                attempt = currentAttempt;
                sawPlaying = handledWin = started = false;
            }
            if (currentAttempt == null) return;
            if (playing) sawPlaying = true;
            if (won && sawPlaying && !handledWin)
            {
                handledWin = started = true;
                startedAt = unscaledNow;
            }
        }

        public float Elapsed(float unscaledNow) => !started ? Duration
            : System.Math.Max(0f, System.Math.Min(Duration, unscaledNow - startedAt));
        public bool Active(float unscaledNow) => started && unscaledNow - startedAt < Duration;
        public void Skip() { started = false; }

        public static float SmokeCoverageAt(float elapsed)
        {
            float rise = Smooth((elapsed - SmokeStart) / (SmokeFullCoverage - SmokeStart));
            float clear = Smooth((elapsed - SmokeRelease) / (Duration - SmokeRelease));
            return rise * (1f - clear);
        }

        private static float Smooth(float value)
        {
            value = System.Math.Max(0f, System.Math.Min(1f, value));
            return value * value * (3f - 2f * value);
        }
    }
}
