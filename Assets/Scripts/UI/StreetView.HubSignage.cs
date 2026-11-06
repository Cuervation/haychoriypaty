using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        // The cleaned full master remains authoritative. Animate its own local
        // pixels, never the older sign cutouts or painted/runtime sparkle overlays.
        internal static int HubSignageAnimationCount => HubArtworkActions.Length;
#if UNITY_EDITOR
        private float hubSignageAnimationTimeForTesting = -1f;
#endif
        private static Rect HubSignagePatchSourceBounds(int action)
        {
            switch (action)
            {
                case HubRankingAction: return new Rect(479f, 440f, 250f, 143f);
                case HubPlayAction: return new Rect(112f, 827f, 242f, 133f);
                case HubMarketAction: return new Rect(78f, 1194f, 315f, 151f);
                case HubBadgesAction: return new Rect(585f, 827f, 256f, 162f);
                default: return Rect.zero;
            }
        }

        // Preserve the canonical artwork anchors used by the hub layout contract.
        internal static Rect HubSignageArtworkSourceBounds(int signIndex) =>
            HubArtActionSourceRect(signIndex == 0 ? HubMarketAction : signIndex == 1 ? HubBadgesAction : 0);

        internal static float HubSignagePulseScale(int action, float elapsed, bool pressed)
        {
            int index = System.Array.IndexOf(HubArtworkActions, action);
            if (index < 0) return 1f;
            if (pressed) return .95f;
            float pulse = .5f - .5f * Mathf.Cos(elapsed * (Mathf.PI * 2f / 2.8f) + index * Mathf.PI * .5f);
            return 1f + .08f * pulse;
        }

        internal static Rect HubSignagePulseBounds(int action, float elapsed, bool pressed)
        {
            Rect source = HubSignagePatchSourceBounds(action);
            float scale = HubSignagePulseScale(action, elapsed, pressed);
            return new Rect(source.center.x - source.width * scale * .5f,
                source.center.y - source.height * scale * .5f, source.width * scale, source.height * scale);
        }

        // The complete animated envelope is a stable target: never chase a moving
        // button, and a press/release remains valid at either end of its pulse.
        internal static Rect HubSignageTouchSourceBounds(int action)
        {
            Rect source = HubSignagePatchSourceBounds(action);
            return new Rect(source.center.x - source.width * 1.08f * .5f,
                source.center.y - source.height * 1.08f * .5f, source.width * 1.08f, source.height * 1.08f);
        }

        private void DrawHubSignage()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || hubBackdrop == null) return;
            float elapsed = Time.unscaledTime;
#if UNITY_EDITOR
            if (hubSignageAnimationTimeForTesting >= 0f) elapsed = hubSignageAnimationTimeForTesting;
#endif
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.color = Color.white;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                foreach (int action in HubArtworkActions)
                {
                    Rect source = HubSignagePatchSourceBounds(action);
                    Rect uv = new Rect(source.x / HubLayeredCanvasWidth,
                        1f - source.yMax / HubLayeredCanvasHeight,
                        source.width / HubLayeredCanvasWidth, source.height / HubLayeredCanvasHeight);
                    Rect animated = HubSignagePulseBounds(action, elapsed, pressedAction == action);
                    GUI.DrawTextureWithTexCoords(HubLayeredArtworkScreenRect(animated, screenSize), hubBackdrop, uv, true);
                }
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
        }

        partial void ReleaseHubSignageResources();
        partial void ReleaseHubSignageResources() { } // UV animation allocates no textures/materials.
    }
}
