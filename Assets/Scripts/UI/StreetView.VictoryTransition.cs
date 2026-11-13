using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private readonly StreetVictoryTransition victoryTransition = new StreetVictoryTransition();
        private bool victorySkipHeld;
        private Texture2D victorySmoke, victorySmokeField, victoryGlow;
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
            const int size = 96;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f, v = (y + .5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float alpha;
                if (glow) alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                else
                {
                    float broad = Mathf.PerlinNoise(u * 3.4f + 2.17f, v * 3.4f + 8.31f);
                    float detail = Mathf.PerlinNoise(u * 7.2f + 5.43f, v * 7.2f + 1.91f);
                    float edge = .77f + (broad - .5f) * .36f + (detail - .5f) * .18f;
                    alpha = (1f - VictorySmooth((r - edge) / .18f)) * (.80f + broad * .20f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = glow ? "Victory ember glow" : "Victory white smoke puff", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateVictorySmokeField()
        {
            const int width = 128, height = 256;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = (x + .5f) / width, v = (y + .5f) / height;
                float broad = Mathf.PerlinNoise(u * 3.2f + 4.3f, v * 3.2f + 1.7f);
                float middle = Mathf.PerlinNoise(u * 7.6f + 2.8f, v * 6.4f + 9.1f);
                float fine = Mathf.PerlinNoise(u * 13.1f + 8.2f, v * 12.4f + 3.6f);
                float turbulence = broad * .52f + middle * .31f + fine * .17f;
                float alpha = Mathf.Clamp01(.22f + turbulence * .92f);
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "Victory white smoke turbulence", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }

        private void DisposeVictoryTransitionTextures()
        {
            if (victorySmoke != null) Destroy(victorySmoke);
            if (victorySmokeField != null) Destroy(victorySmokeField);
            if (victoryGlow != null) Destroy(victoryGlow);
            victorySmoke = victorySmokeField = victoryGlow = null;
        }
        private void VictoryQuad(Rect rect, Color color, Texture2D texture = null)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, texture != null ? texture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        }

        private void DrawVictorySmokePuff(Matrix4x4 baseMatrix, float x, float y, float width, float height,
            float rotation, float alpha, bool silver)
        {
            GUI.matrix = baseMatrix * Matrix4x4.Translate(new Vector3(x, y, 0f))
                * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation));
            float shadeAlpha = alpha * (silver ? .88f : .72f);
            VictoryQuad(new Rect(-width * .56f - 9f, -height * .52f + 9f, width * 1.12f, height * 1.04f),
                new Color(.58f, .66f, .78f, shadeAlpha), victorySmoke);
            VictoryQuad(new Rect(-width * .46f, -height * .46f, width * .92f, height * .92f),
                new Color(.98f, .99f, 1f, alpha * .82f), victorySmoke);
            GUI.matrix = baseMatrix;
        }

        private void DrawVictorySmoke(float elapsed, float height, Matrix4x4 baseMatrix)
        {
            float coverage = StreetVictoryTransition.SmokeCoverageAt(elapsed);
            if (coverage <= .001f) return;
            if (victorySmoke == null) victorySmoke = CreateVictoryRadial(false);
            if (victorySmokeField == null) victorySmokeField = CreateVictorySmokeField();
            if (victoryGlow == null) victoryGlow = CreateVictoryRadial(true);

            // A cool white veil plus turbulent texture fills the whole viewport at the transition's peak.
            VictoryQuad(new Rect(0f, 0f, W, height), new Color(.88f, .93f, .98f, coverage * .10f));
            GUI.color = new Color(.96f, .98f, 1f, coverage * .14f);
            float fieldOffset = elapsed * .075f;
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, W, height), victorySmokeField,
                new Rect(0f, fieldOffset, 1f, 1f), true);
            GUI.matrix = baseMatrix;

            for (int i = 0; i < 32; i++)
            {
                float delay = VictoryNoise(i + 401) * .34f;
                float grow = VictoryEase((elapsed - StreetVictoryTransition.SmokeStart - delay) / 1.05f);
                if (grow <= .001f) continue;
                int column = i % 4, row = i / 4;
                float targetX = W * (column + .5f) / 4f + (VictoryNoise(i + 501) - .5f) * 68f;
                float targetY = height * (row + .5f) / 8f + (VictoryNoise(i + 601) - .5f) * 82f;
                float sourceX = W * .5f + (VictoryNoise(i + 701) - .5f) * W * .30f;
                float sourceY = height + 110f + VictoryNoise(i + 801) * 160f;
                float drift = Mathf.Sin(elapsed * (1.2f + VictoryNoise(i + 901) * .8f) + i) * 18f;
                float x = Mathf.Lerp(sourceX, targetX, grow) + drift;
                float y = Mathf.Lerp(sourceY, targetY, grow) - grow * 30f;
                float width = Mathf.Lerp(118f + VictoryNoise(i + 1001) * 70f,
                    235f + VictoryNoise(i + 1101) * 150f, grow);
                float puffHeight = Mathf.Lerp(155f + VictoryNoise(i + 1201) * 60f,
                    220f + VictoryNoise(i + 1301) * 125f, grow);
                float puffAlpha = coverage * VictorySmooth((elapsed - StreetVictoryTransition.SmokeStart - delay) / .2f)
                    * (.62f + VictoryNoise(i + 1401) * .30f);
                float rotation = Mathf.Sin(elapsed * .8f + i * .7f) * 17f + (VictoryNoise(i + 1501) - .5f) * 18f;
                DrawVictorySmokePuff(baseMatrix, x, y, width, puffHeight, rotation, puffAlpha, i % 3 == 0);
            }

            // A few stretched, curling wisps keep the cloud visibly rising rather than reading as a screen tint.
            for (int i = 0; i < 9; i++)
            {
                float delay = .08f + VictoryNoise(i + 1601) * .25f;
                float rise = VictoryEase((elapsed - StreetVictoryTransition.SmokeStart - delay) / 1.18f);
                if (rise <= .001f) continue;
                float targetX = VictoryNoise(i + 1701) * (W * 1.18f) - W * .09f;
                float x = Mathf.Lerp(W * .5f + (VictoryNoise(i + 1801) - .5f) * 70f, targetX, rise)
                    + Mathf.Sin(elapsed * 1.4f + i) * 25f;
                float y = Mathf.Lerp(height + 80f, VictoryNoise(i + 1901) * height, rise);
                float width = Mathf.Lerp(150f, 300f + VictoryNoise(i + 2001) * 220f, rise);
                float puffHeight = 70f + VictoryNoise(i + 2101) * 86f;
                float rotation = Mathf.Sin(elapsed * 1.1f + i * .9f) * 28f;
                float alpha = coverage * .55f * VictorySmooth((elapsed - StreetVictoryTransition.SmokeStart - delay) / .2f);
                DrawVictorySmokePuff(baseMatrix, x, y, width, puffHeight, rotation, alpha, true);
            }

            // Keep only a handful of warm sparks close to the grill; the smoke itself is white/silver.
            float emberFade = coverage * (1f - VictorySmooth((elapsed - 1.48f) / .48f));
            VictoryQuad(new Rect(W * .5f - 116f, height * .83f - 78f, 232f, 232f),
                new Color(1f, .63f, .28f, emberFade * .18f), victoryGlow);
            for (int i = 0; i < 15; i++)
            {
                float age = elapsed - StreetVictoryTransition.SmokeStart - VictoryNoise(i + 2201) * .24f;
                if (age < 0f || age > .92f) continue;
                float x = W * .5f + (VictoryNoise(i + 2301) - .5f) * 280f + age * (VictoryNoise(i + 2401) - .5f) * 40f;
                float y = height * .88f - age * (80f + VictoryNoise(i + 2501) * 190f);
                float alpha = emberFade * Mathf.Clamp01(1f - age / .92f);
                float radius = 5f + VictoryNoise(i + 2601) * 7f;
                VictoryQuad(new Rect(x - radius * .5f, y - radius, radius, radius * 2f),
                    new Color(1f, .69f, .31f, alpha * .85f), victoryGlow);
            }
        }

        private void DrawVictoryTransition()
        {
            float t = victoryTransition.Elapsed(Time.unscaledTime), height = logicalCanvasHeight;
            Color previous = GUI.color; Matrix4x4 matrix = GUI.matrix;
            float vertical = layoutVerticalScale; bool enabled = GUI.enabled;
            layoutVerticalScale = 1f;
            try
            {
                if (t >= StreetVictoryTransition.ChulengoRevealStart)
                {
                    // Reveal the real modal, not a screenshot or duplicate reward simulation.
                    GUI.enabled = false;
                    float reveal = VictoryEase((t - StreetVictoryTransition.ChulengoRevealStart) / .66f);
                    GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(0, 30f * (1f - reveal), 0))
                        * Matrix4x4.Translate(new Vector3(W * .5f, height * .5f, 0f))
                        * Matrix4x4.Scale(new Vector3(.97f + .03f * reveal, .97f + .03f * reveal, 1f))
                        * Matrix4x4.Translate(new Vector3(-W * .5f, -height * .5f, 0f));
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
                DrawVictorySmoke(t, height, matrix);
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
