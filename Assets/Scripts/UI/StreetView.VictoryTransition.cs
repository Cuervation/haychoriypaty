using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private readonly StreetVictoryTransition victoryTransition = new StreetVictoryTransition();
        private bool victorySkipHeld;
        private Texture2D victorySmoke, victoryGlow;
        private bool VictoryTransitionActive => victoryTransition.Active(Time.unscaledTime);

        private void ObserveVictoryTransition()
        {
            var sim = game != null && game.IsPlayerActive ? game.Sim : null;
            victoryTransition.Observe(sim, sim != null && sim.Phase == RoundPhase.Playing,
                sim != null && sim.Phase == RoundPhase.Won, Time.unscaledTime);
            if (sim == null || sim.Phase == RoundPhase.Ready || sim.Phase == RoundPhase.Lost)
                victorySkipHeld = false;
        }

        private bool ConsumeVictoryTransitionPointer(bool down, bool up)
        {
            if (!VictoryTransitionActive && !victorySkipHeld) return false;
            pressedAction = 0;
            if (down) victorySkipHeld = true;
            if (up) { victoryTransition.Skip(); victorySkipHeld = false; }
            return true; // The release that skips must never reach COBRAR/MULTIPLICAR.
        }

        private static float VictorySmooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
        private static float VictoryEase(float value) => 1f - Mathf.Pow(1f - Mathf.Clamp01(value), 3f);
        private static float VictoryNoise(int seed) => Mathf.Repeat(Mathf.Sin(seed * 87.71f + 19.13f) * 43758.5453f, 1f);

        private static Texture2D CreateVictoryRadial(bool glow)
        {
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f)) / (size * .5f);
                float alpha = glow ? Mathf.Pow(Mathf.Clamp01(1f - r), 2f) : 1f - VictorySmooth((r - .45f) / .55f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = glow ? "Victory ember glow" : "Victory smoke puff", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }
        private void DisposeVictoryTransitionTextures()
        {
            if (victorySmoke != null) Destroy(victorySmoke);
            if (victoryGlow != null) Destroy(victoryGlow);
            victorySmoke = victoryGlow = null;
        }
        private void VictoryQuad(Rect rect, Color color, Texture2D texture = null)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, texture != null ? texture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        }

        private void DrawVictoryTransition()
        {
            float t = victoryTransition.Elapsed(Time.unscaledTime), height = logicalCanvasHeight;
            Color previous = GUI.color; Matrix4x4 matrix = GUI.matrix;
            float vertical = layoutVerticalScale; bool enabled = GUI.enabled;
            layoutVerticalScale = 1f;
            try
            {
                if (t >= 1.45f)
                {
                    // Reveal the real modal, not a screenshot or duplicate reward simulation.
                    GUI.enabled = false;
                    GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(0, 24f * (1f - VictoryEase((t - 1.45f) / .8f)), 0));
                    ResultPanel();
                    GUI.matrix = matrix; GUI.enabled = enabled;
                }
                else VictoryQuad(new Rect(0, 0, W, height), new Color(.02f, .07f, .12f, .39f * VictorySmooth((t - .1f) / .4f)));

                float a = VictorySmooth((t - .15f) / .14f) * (1f - VictorySmooth((t - 1.05f) / .26f));
                if (a > 0)
                {
                    float centerY = height * .375f;
                    float pop = 1f + .1f * Mathf.Sin(Mathf.Clamp01((t - .15f) / .4f) * Mathf.PI);
                    GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(W * .5f, centerY, 0))
                        * Matrix4x4.Scale(new Vector3(pop, pop, 1)) * Matrix4x4.Translate(new Vector3(-W * .5f, -centerY, 0));
                    GUI.color = new Color(1, 1, 1, a);
                    VictoryText(new Rect(18, centerY - 75, W - 36, 72), "¡PARTIDO", 62, new Color(1f, .96f, .8f), TextAnchor.MiddleCenter);
                    VictoryText(new Rect(18, centerY - 12, W - 36, 80), "GANADO!", 70, new Color(1f, .86f, .4f), TextAnchor.MiddleCenter);
                    GUI.matrix = matrix;
                }
                for (int i = 0; i < 35 && t > .2f && t < 2.85f; i++)
                {
                    float age = t - .2f - VictoryNoise(i + 2) * .45f;
                    if (age < 0) continue;
                    float life = Mathf.Clamp01(age / 2.2f);
                    float x = 270 + (VictoryNoise(i + 32) - .5f) * 490 * life;
                    float y = height * .448f - (300 + VictoryNoise(i + 83) * 150) * life + 800 * life * life;
                    float alpha = (1f - life) * .9f;
                    if (i % 4 == 0 && gameCoin != null)
                    {
                        float size = 20 + VictoryNoise(i + 96) * 8;
                        VictoryQuad(new Rect(x - size / 2, y - size / 2, size, size), new Color(1, 1, 1, alpha), gameCoin);
                    }
                    else
                    {
                        Color color = i % 3 == 0 ? new Color(.46f, .84f, 1f, alpha)
                            : i % 3 == 1 ? new Color(1f, .8f, .29f, alpha) : new Color(1f, .98f, .87f, alpha);
                        GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(x, y, 0))
                            * Matrix4x4.Rotate(Quaternion.Euler(0, 0, age * (VictoryNoise(i + 90) * 340 - 170)));
                        VictoryQuad(new Rect(-3, -6, 6, 12), color);
                        GUI.matrix = matrix;
                    }
                }
                if (t > 1f && t < 2.7f)
                {
                    if (victorySmoke == null) victorySmoke = CreateVictoryRadial(false);
                    if (victoryGlow == null) victoryGlow = CreateVictoryRadial(true);
                    float cy = height + 150 - (height + 480) * VictoryEase((t - 1) / 1.3f);
                    float fade = VictorySmooth((t - 1) / .18f) * (1 - VictorySmooth((t - 2.22f) / .48f));
                    VictoryQuad(new Rect(-180, cy - 420, 900, 900), new Color(1f, .48f, .08f, fade * .85f), victoryGlow);
                    for (int i = 0; i < 28; i++)
                    {
                        float x = VictoryNoise(i + 100) * 720 - 90, y = cy + (VictoryNoise(i + 200) - .5f) * 270;
                        float radius = 85 + VictoryNoise(i + 300) * 90;
                        VictoryQuad(new Rect(x - radius, y - radius, radius * 2, radius * 2), new Color(.30f, .28f, .25f, fade * .84f), victorySmoke);
                    }
                    for (int i = 0; i < 48; i++)
                    {
                        float age = t - 1 - VictoryNoise(i + 800) * .35f;
                        if (age < 0) continue;
                        float x = 270 + (VictoryNoise(i + 900) - .5f) * 620 * age;
                        float y = height * .74f - (250 + VictoryNoise(i + 1000) * 550) * age;
                        float alpha = fade * Mathf.Clamp01(1 - age / 1.6f);
                        VictoryQuad(new Rect(x - 12, y - 12, 26, 30), new Color(1f, .30f, .04f, alpha), victoryGlow);
                        VictoryQuad(new Rect(x, y, 2 + VictoryNoise(i + 1200) * 2, 5 + VictoryNoise(i + 1100) * 7), new Color(1f, .8f, .35f, alpha));
                    }
                }
                GUI.color = Color.white;
                // Legacy input fallback. The production Input System consumes complete skip gestures in Update.
#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
                if (GUI.Button(new Rect(0, 0, W, height), GUIContent.none, invisible))
                { victoryTransition.Skip(); pressedAction = 0; victorySkipHeld = false; }
#endif
            }
            finally { GUI.matrix = matrix; GUI.color = previous; GUI.enabled = enabled; layoutVerticalScale = vertical; }
        }
    }
}
