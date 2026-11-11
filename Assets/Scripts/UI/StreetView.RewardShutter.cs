using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        // Timing and motion match the approved preview, without its demonstration holds.
        private const float RewardShutterTravel = .54f;
        private const float RewardShutterHold = .60f;
        private float rewardShutterStarted = -1f;
        private bool rewardShutterCommitted;
        private Texture2D rewardShutterPanel;
        private bool RewardShutterActive => rewardShutterStarted >= 0f;

        private void StartRewardShutter()
        {
            if (RewardShutterActive) return;
            if (rewardShutterPanel == null)
                rewardShutterPanel = Resources.Load<Texture2D>("street-reward-shutter-v1");
            rewardShutterCommitted = false;
            rewardShutterStarted = Time.unscaledTime;
            pressedAction = 0;
        }

        private void UpdateRewardShutter()
        {
            if (!RewardShutterActive) return;
            float elapsed = Time.unscaledTime - rewardShutterStarted;
            if (!rewardShutterCommitted && elapsed >= RewardShutterTravel)
            {
                // The real durable settlement remains the sole source of the reward.
                rewardShutterCommitted = true;
                if (game != null) game.TryCollectVictoryReward();
            }
            if (elapsed >= RewardShutterTravel * 2f + RewardShutterHold)
                rewardShutterStarted = -1f;
        }

        public static float RewardShutterProgress(float elapsed)
        {
            if (elapsed < RewardShutterTravel)
                return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / RewardShutterTravel));
            if (elapsed < RewardShutterTravel + RewardShutterHold) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((elapsed - RewardShutterTravel - RewardShutterHold) / RewardShutterTravel));
        }

        private void DrawRewardShutter()
        {
            if (!RewardShutterActive || Event.current.type != EventType.Repaint) return;
            float elapsed = Time.unscaledTime - rewardShutterStarted;
            float progress = RewardShutterProgress(elapsed);
            float width = Screen.width, height = Screen.height, half = height * .5f;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
            Texture2D panel = rewardShutterPanel != null ? rewardShutterPanel : Texture2D.whiteTexture;
            if (rewardShutterPanel == null) GUI.color = new Color(.13f, .36f, .56f);
            GUI.DrawTexture(new Rect(0f, -half + progress * half, width, half), panel, ScaleMode.StretchToFill, true);
            // Flip vertically around the screen center, retaining the original riveted leading edge.
            GUI.matrix = Matrix4x4.TRS(new Vector3(0f, height, 0f), Quaternion.identity, new Vector3(1f, -1f, 1f));
            GUI.DrawTexture(new Rect(0f, -half + progress * half, width, half), panel, ScaleMode.StretchToFill, true);
            GUI.matrix = Matrix4x4.identity;
            if (titleLogo != null && progress > .52f)
            {
                float alpha = Mathf.Clamp01((progress - .52f) / .32f);
                float scale = .88f + .12f * Mathf.Clamp01((progress - .52f) / .48f);
                float size = Mathf.Min(width * (196f / 432f), height * .38f) * scale;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect((width - size) * .5f, (height - size) * .5f, size, size), titleLogo, ScaleMode.ScaleToFit, true);
            }
            GUI.color = color; GUI.matrix = matrix;
        }
    }
}
