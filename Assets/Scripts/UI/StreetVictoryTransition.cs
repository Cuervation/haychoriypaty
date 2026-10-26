namespace HayChoriYPaty
{
    /// <summary>Attempt-local presentation clock. Never settles or mutates a match.</summary>
    public sealed class StreetVictoryTransition
    {
        public const float Duration = 3f;
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
    }
}
