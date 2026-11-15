using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Pantalla de introducción. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private static Rect IntroScreenRect(Vector2 size)
        {
            return new Rect(0, 0, Mathf.Max(0, size.x), Mathf.Max(0, size.y));
        }
        private static Rect IntroBackdropBounds(Vector2 screenSize, float scale)
        {
            float width = Mathf.Max(0f, screenSize.x * scale);
            float height = Mathf.Max(0f, screenSize.y * scale);
            return new Rect((screenSize.x - width) * .5f, (screenSize.y - height) * .5f, width, height);
        }
        private static void DrawIntroScreen(Texture2D texture, Color tint, ScaleMode scaleMode)
        {
            DrawIntroScreen(texture, tint, scaleMode, IntroScreenRect(new Vector2(Screen.width, Screen.height)));
        }
        private static void DrawIntroScreen(Texture2D texture, Color tint, ScaleMode scaleMode, Rect screenBounds)
        {
            if (texture == null) return;
            // Decorative art fills physical pixels; foreground/input keep the safe-area canvas.
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.color = tint;
                GUI.DrawTexture(screenBounds, texture, scaleMode, true);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static Rect IntroProgressBounds() => new Rect((W - 95f) * .5f, 882f, 95f, 28f);
        private static float IntroProgressValue(float elapsed) => Mathf.Clamp01(elapsed / IntroDuration);
        // GUIStyle.fontSize is an integer: 19 × 0.75 = 14.25, rounded to 14.
        private const int IntroStatusFontSize = 14;
        private GUIStyle IntroStatusStyle()
        {
            if (introStatusStyle != null) return introStatusStyle;
            introStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = IntroStatusFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
            if (googleSansFont != null) introStatusStyle.font = googleSansFont;
            introStatusStyle.normal.textColor = Color.white;
            return introStatusStyle;
        }
        private static float IntroLogoPop(float t)
        {
            if (t < .10f) return 0f;
            if (t < .22f) return Mathf.Lerp(.55f, 1.22f, Mathf.SmoothStep(0f, 1f, (t - .10f) / .12f));
            if (t < .40f) return Mathf.Lerp(1.22f, .97f, Mathf.SmoothStep(0f, 1f, (t - .22f) / .18f));
            return Mathf.Lerp(.97f, 1f, Mathf.SmoothStep(0f, 1f, (t - .40f) / .13f));
        }
        private static string IntroStatus(float elapsed) =>
            elapsed < 1.35f ? "PRENDIENDO LAS BRASAS..." :
            elapsed < 2.7f ? "LA HINCHADA ESTÁ LLEGANDO..." : "¡QUE EMPIECE LA FIESTA EN EL BARRIO!";
        private static Rect IntroLogoBounds(float scale)
        {
            float width = 270f * scale, height = 260f * scale;
            return new Rect((W - width) * .5f, 450f - height * .5f, width, height);
        }
        private void PrepareIntroLogoShines()
        {
            if ((introLogoShines != null && introLogoShines.Length > 0) || titleLogo == null) return;
            const int size = 256, frames = 24;
            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Texture2D alphaCopy = null;
            Color32[] source;
            try
            {
                Graphics.Blit(titleLogo, rt);
                RenderTexture.active = rt;
                alphaCopy = new Texture2D(size, size, TextureFormat.RGBA32, false);
                alphaCopy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                alphaCopy.Apply(false, false);
                source = alphaCopy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                if (alphaCopy != null) Destroy(alphaCopy);
            }
            introLogoShines = new Texture2D[frames];
            for (int k = 0; k < frames; k++)
            {
                Color[] pixels = new Color[size * size];
                float center = Mathf.Lerp(-.25f, 1.5f, k / (float)(frames - 1));
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs((x / (float)size + y / (float)size * .25f) - center);
                    float shine = Mathf.Pow(Mathf.Clamp01(1f - d / .13f), 1.4f) * .62f;
                    pixels[y * size + x] = new Color(1f, 1f, .92f, shine * source[y * size + x].a / 255f);
                }
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Startup logo shine " + k,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                introLogoShines[k] = texture;
            }
        }
        private void DrawIntroLogo(float elapsed)
        {
            float scale = IntroLogoPop(elapsed);
            if (scale <= 0f || titleLogo == null) return;
            Rect bounds = IntroLogoBounds(scale);
            GUI.DrawTexture(LayoutRect(bounds), titleLogo, ScaleMode.ScaleToFit, true);
            PrepareIntroLogoShines();
            float sweep = elapsed >= 2.65f ? elapsed - 2.65f : elapsed - 1.05f;
            if (sweep >= 0f && sweep < .65f && introLogoShines != null && introLogoShines.Length > 0)
            {
                int frame = Mathf.Clamp(Mathf.FloorToInt(sweep / .65f * introLogoShines.Length), 0, introLogoShines.Length - 1);
                if (introLogoShines[frame] != null)
                    GUI.DrawTexture(LayoutRect(bounds), introLogoShines[frame], ScaleMode.ScaleToFit, true);
            }
        }
        private void DrawIntroProgress(float elapsed)
        {
            const int width = 95, height = 28;
            if (introPreviewBar == null)
                introPreviewBar = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Startup cyan animated capsule",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
            int key = Mathf.FloorToInt(elapsed * 30f);
            if (key != introPreviewBarKey)
            {
                introPreviewBarKey = key;
                Color[] pixels = new Color[width * height];
                float progress = Mathf.Clamp01(elapsed / 3.7f);
                if (!viewResourcesReady)
                {
                    progress = Mathf.Min(.85f, progress * .85f);
                    if (SplashComplete) progress = .85f + .15f * startupLoadProgress;
                }
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float px = x + .5f, py = y + .5f;
                    Color color = Color.clear;
                    float coverage = RoundedCoverage(px, py, new Rect(0, 2, 95, 26), 13);
                    color = Color.Lerp(color, new Color(.09f, .025f, .15f, .85f), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(2, 0, 91, 25), 12.5f);
                    color = Color.Lerp(color, new Color(.11f, .11f, .27f, 1), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(4, 2, 87, 21), 10.5f);
                    color = Color.Lerp(color, new Color(.94f, .99f, 1f, 1), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(7, 5, 81, 15), 7.5f);
                    color = Color.Lerp(color, new Color(.025f, .33f, .50f, 1), coverage);
                    if (px < 7 + 81 * progress)
                    {
                        float stripe = ((px + py * .65f - key * 1.2f) % 23f + 23f) % 23f;
                        Color fill = Color.Lerp(new Color(.04f, .63f, .89f, 1), new Color(.18f, .89f, 1f, 1), 1f - py / 28f);
                        if (stripe < 7f) fill = Color.Lerp(fill, new Color(.6f, .96f, 1f, 1), .3f);
                        color = Color.Lerp(color, fill, coverage);
                    }
                    pixels[(height - 1 - y) * width + x] = color;
                }
                introPreviewBar.SetPixels(pixels);
                introPreviewBar.Apply(false, false);
            }
            GUI.DrawTexture(LayoutRect(IntroProgressBounds()), introPreviewBar);
        }
        private void DrawIntro(float elapsed)
        {
            DrawIntroScreen(coverArt, Color.white, ScaleMode.ScaleAndCrop);
            DrawIntroLogo(elapsed);
            GUIStyle status = IntroStatusStyle();
            string message = IntroStatus(elapsed);
            Rect letters = new Rect(8f, 851f, W - 16f, 30f);
            status.normal.textColor = Color.black;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(letters.x + x * 1.5f, letters.y + y * 1.5f, letters.width, letters.height), message, status);
            status.normal.textColor = Color.white;
            Label(letters, message, status);
            DrawIntroProgress(elapsed);
            GUI.color = Color.white;
        }
    }
}
