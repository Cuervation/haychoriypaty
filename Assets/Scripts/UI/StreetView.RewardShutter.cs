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
        private bool rewardShutterOpenSounded;
        private AudioSource rewardShutterAudio;
        private AudioClip rewardShutterCloseClip, rewardShutterSlamClip, rewardShutterOpenClip, rewardCoinsClip, rewardCoinsBrightClip;
        private bool RewardShutterActive => rewardShutterStarted >= 0f;
        // What a shutter hides: run once the panels fully cover the screen (null = the Chulengo reward claim).
        private System.Action rewardShutterWhenCovered;

        public const int ClaimSoundNone = 0, ClaimSoundCoins = 1, ClaimSoundBright = 2;
        private const float RewardShutterSoundVolume = .5f, RewardShutterSlamVolume = .7f, RewardCoinsVolume = .6f;

        /// <summary>Closes the shutter over the screen, runs <paramref name="whenCovered"/> once it is fully shut and opens it again.
        /// Without an action it is the reward claim's shutter.</summary>
        private void StartRewardShutter(System.Action whenCovered = null)
        {
            if (RewardShutterActive) return;
            rewardShutterWhenCovered = whenCovered;
            if (rewardShutterPanel == null)
                rewardShutterPanel = Resources.Load<Texture2D>("street-reward-shutter-v1");
            rewardShutterCommitted = false;
            rewardShutterOpenSounded = false;
            rewardShutterStarted = Time.unscaledTime;
            pressedAction = 0;
            PlayRewardShutterSound(ref rewardShutterCloseClip, "Audio/reward-shutter-close", RewardShutterSoundVolume);
        }

        /// <summary>Coins only when the claim pays: base or x1 claims are plain, x1.5/x2 are bright, a zero sector is silent.</summary>
        public static int RewardClaimSoundKind(bool hasPendingChulengo, bool choiceLocked, int selectedSector)
        {
            if (!hasPendingChulengo) return ClaimSoundNone;
            if (!choiceLocked || selectedSector < 0) return ClaimSoundCoins;
            int numerator = StreetChulengo.MultiplierNumerator(selectedSector);
            return numerator == 0 ? ClaimSoundNone : numerator > 2 ? ClaimSoundBright : ClaimSoundCoins;
        }

        private void PlayRewardShutterSound(ref AudioClip clip, string resourcePath, float volume)
        {
            if (clip == null) clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null) return;
            if (rewardShutterAudio == null)
            {
                Transform child = transform.Find("RewardShutterAudio");
                GameObject audioObject = child != null ? child.gameObject : new GameObject("RewardShutterAudio");
                if (child == null) audioObject.transform.SetParent(transform, false);
                rewardShutterAudio = audioObject.GetComponent<AudioSource>();
                if (rewardShutterAudio == null) rewardShutterAudio = audioObject.AddComponent<AudioSource>();
                rewardShutterAudio.playOnAwake = false;
                rewardShutterAudio.loop = false;
                rewardShutterAudio.spatialBlend = 0f;
            }
            rewardShutterAudio.PlayOneShot(clip, volume);
        }

        private void UpdateRewardShutter()
        {
            if (!RewardShutterActive) return;
            float elapsed = Time.unscaledTime - rewardShutterStarted;
            if (!rewardShutterCommitted && elapsed >= RewardShutterTravel)
            {
                // Commit only after the original shutter fully covers the claim screen.
                rewardShutterCommitted = true;
                PlayRewardShutterSound(ref rewardShutterSlamClip, "Audio/reward-shutter-slam", RewardShutterSlamVolume);
                if (rewardShutterWhenCovered != null)
                {
                    System.Action action = rewardShutterWhenCovered;
                    rewardShutterWhenCovered = null;
                    action();
                    return;
                }
                if (game == null) return;

                int soundKind = RewardClaimSoundKind(game.HasPendingChulengo, game.ChulengoChoiceLocked, game.ChulengoSelectedSector);
                bool claimed = !game.HasPendingChulengo ||
                    (game.ChulengoChoiceLocked && game.ChulengoSelectedSector >= 0
                        ? game.TryCollectSelectedVictoryReward()
                        : game.TryCollectVictoryReward());
                if (claimed && soundKind == ClaimSoundBright)
                    PlayRewardShutterSound(ref rewardCoinsBrightClip, "Audio/reward-claim-coins-bright", RewardCoinsVolume);
                else if (claimed && soundKind == ClaimSoundCoins)
                    PlayRewardShutterSound(ref rewardCoinsClip, "Audio/reward-claim-coins", RewardCoinsVolume);
                if (claimed && game.ReturnToLevels())
                {
                    marketActive = false;
                    levelSelectActive = true;
                    levelSelectScroll = 0f;
                }
            }
            if (!rewardShutterOpenSounded && elapsed >= RewardShutterTravel + RewardShutterHold)
            {
                rewardShutterOpenSounded = true;
                PlayRewardShutterSound(ref rewardShutterOpenClip, "Audio/reward-shutter-open", RewardShutterSoundVolume);
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
