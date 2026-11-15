using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Botones estándar y su arte. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void DrawStandardButton(Rect bounds, int action, string caption, bool enabled = true,
            System.Action activated = null, Texture2D leadingIcon = null, GUIContent buttonContent = null,
            Texture2D backgroundOverride = null, Color? textColorOverride = null, float fontScale = 1f)
        {
            bool interactive = enabled && GUI.enabled;
            bool pressed = interactive && pressedAction == action;
            Rect hitBounds = LayoutRect(bounds);
            Rect drawn = StandardButtonVisualBounds(hitBounds, pressed);
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.color = StandardButtonTint(interactive, pressed);
                DrawStandardButtonBackground(drawn, backgroundOverride);
                GUI.color = Color.white;
                float iconInset = leadingIcon != null ? drawn.height * .64f : 0f;
                Rect fitBounds = bounds;
                fitBounds.width = Mathf.Max(1f, fitBounds.width - iconInset * 2f);
                int fontSize = FitStandardButtonFontSize(menuTitle, caption, fitBounds, fontScale);
                Color captionColor = textColorOverride ?? (interactive ? Color.white : new Color(.80f, .87f, .94f));
                Color borderColor = textColorOverride ?? new Color(.035f, .075f, .16f);
                var captionStyle = CachedStandardButtonLabelStyle(menuTitle, fontSize, captionColor);
                var borderStyle = CachedStandardButtonLabelStyle(menuTitle, fontSize, borderColor);
                float textScale = bounds.height / 92f;
                Rect letters = new Rect(drawn.x + iconInset, drawn.y - 2f * textScale,
                    drawn.width - iconInset * 2f, drawn.height - 8f * textScale);
                if (leadingIcon != null)
                {
                    float iconSize = Mathf.Min(drawn.height * .54f, drawn.width * .12f);
                    float sideInset = drawn.width * .075f;
                    GUI.DrawTexture(new Rect(drawn.x + sideInset, drawn.center.y - iconSize * .5f,
                        iconSize, iconSize), leadingIcon, ScaleMode.ScaleToFit, true);
                }
                if (!textColorOverride.HasValue)
                {
                    float outline = 2.2f * captionStyle.fontSize / 38f;
                    for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                        if (x != 0 || y != 0)
                            GUI.Label(new Rect(letters.x + x * outline, letters.y + y * outline,
                                letters.width, letters.height), caption, borderStyle);
                }
                GUI.Label(letters, caption, captionStyle);
                GUI.enabled = interactive;
                if (GUI.Button(hitBounds, buttonContent ?? GUIContent.none, invisible) && !SuppressLevelSelectorNativeTap && !SuppressMarketNativeTap)
                { if (activated != null) activated(); else NativeAction(action); }
            }
            finally
            {
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        private static Rect StandardButtonVisualBounds(Rect bounds, bool pressed)
        {
            float scale = pressed ? .97f : 1f;
            return new Rect(bounds.center.x - bounds.width * scale * .5f,
                bounds.center.y - bounds.height * scale * .5f, bounds.width * scale, bounds.height * scale);
        }

        private static Color StandardButtonTint(bool enabled, bool pressed) =>
            !enabled ? new Color(.50f, .62f, .75f) : pressed ? new Color(.78f, .88f, 1f) : Color.white;

        private const int StandardButtonCacheLimit = 256;
        private static readonly System.Collections.Generic.Dictionary<(GUIStyle, int, Color), GUIStyle> standardButtonLabelStyleCache =
            new System.Collections.Generic.Dictionary<(GUIStyle, int, Color), GUIStyle>();
        private static readonly System.Collections.Generic.Dictionary<(GUIStyle, string, float, float, float), int> standardButtonFontSizeCache =
            new System.Collections.Generic.Dictionary<(GUIStyle, string, float, float, float), int>();

        // Shared read-only label style for per-frame button text; callers must not mutate it.
        private static GUIStyle CachedStandardButtonLabelStyle(GUIStyle source, int fontSize, Color color)
        {
            var key = (source, fontSize, color);
            if (standardButtonLabelStyleCache.TryGetValue(key, out GUIStyle cached) && cached != null) return cached;
            if (standardButtonLabelStyleCache.Count >= StandardButtonCacheLimit) standardButtonLabelStyleCache.Clear();
            GUIStyle created = CreateStandardButtonLabelStyle(source, fontSize, color);
            standardButtonLabelStyleCache[key] = created;
            return created;
        }

        private static GUIStyle CreateStandardButtonLabelStyle(GUIStyle source, int fontSize, Color color)
        {
            var style = new GUIStyle(source) { fontSize = fontSize };
            // GUI.Label also inherits skin hover colors; they must not erase our custom outline.
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            return style;
        }

        // GUIStyle.CalcSize is costly on mobile and the same short strings are measured every frame
        // (order quantities, HUD counters, upgrade prices). Cache by style, font size and text.
        private static readonly System.Collections.Generic.Dictionary<(GUIStyle, int, string), Vector2> textSizeCache =
            new System.Collections.Generic.Dictionary<(GUIStyle, int, string), Vector2>();
        private static Vector2 CachedCalcSize(GUIStyle style, string value)
        {
            var key = (style, style.fontSize, value);
            if (textSizeCache.TryGetValue(key, out Vector2 size)) return size;
            if (textSizeCache.Count >= 1024) textSizeCache.Clear();
            size = style.CalcSize(new GUIContent(value));
            textSizeCache[key] = size;
            return size;
        }
        private static int FitStandardButtonFontSize(GUIStyle style, string caption, Rect bounds, float fontScale = 1f)
        {
            var key = (style, caption, bounds.width, bounds.height, fontScale);
            if (standardButtonFontSizeCache.TryGetValue(key, out int cachedSize)) return cachedSize;
            if (standardButtonFontSizeCache.Count >= StandardButtonCacheLimit) standardButtonFontSizeCache.Clear();
            int fitted = FitStandardButtonFontSizeUncached(style, caption, bounds, fontScale);
            standardButtonFontSizeCache[key] = fitted;
            return fitted;
        }

        private static int FitStandardButtonFontSizeUncached(GUIStyle style, string caption, Rect bounds, float fontScale)
        {
            int original = style.fontSize;
            int preferred = Mathf.Max(11, Mathf.RoundToInt(38f * bounds.height / 92f * Mathf.Max(1f, fontScale)));
            float width = Mathf.Max(1f, bounds.width - 24f * bounds.height / 92f);
            float height = Mathf.Max(1f, bounds.height - 8f * bounds.height / 92f);
            try
            {
                for (int candidate = preferred; candidate > 11; candidate--)
                {
                    style.fontSize = candidate;
                    Vector2 measured = style.CalcSize(new GUIContent(caption));
                    if (measured.x <= width && measured.y <= height) return candidate;
                }
                return 11;
            }
            finally { style.fontSize = original; }
        }

        private static Rect StandardButtonSliceBounds(Rect bounds, int slice)
        {
            float cap = Mathf.Min(bounds.height * .5f, bounds.width * .5f);
            return slice == 0 ? new Rect(bounds.x, bounds.y, cap, bounds.height)
                : slice == 1 ? new Rect(bounds.x + cap, bounds.y, bounds.width - cap * 2f, bounds.height)
                : new Rect(bounds.xMax - cap, bounds.y, cap, bounds.height);
        }

        private void DrawStandardButtonBackground(Rect bounds, Texture2D backgroundOverride = null)
        {
            Texture2D background = backgroundOverride != null ? backgroundOverride : menuButton;
            if (background == null) return;
            // The original cover aspect uses its original exact texture mapping.
            if (Mathf.Abs(bounds.width / bounds.height - background.width / (float)background.height) < .03f)
            {
                GUI.DrawTexture(bounds, background, ScaleMode.StretchToFill, true);
                return;
            }
            // Resize only the straight middle; end caps, gloss, rim and shadow retain their proportions.
            for (int slice = 0; slice < 3; slice++)
            {
                Rect dest = StandardButtonSliceBounds(bounds, slice);
                Rect source = StandardButtonSliceBounds(new Rect(0, 0, background.width, background.height), slice);
                if (dest.width <= 0f) continue;
                // Fractional portrait transforms can round adjacent IMGUI rectangles apart.
                // Overlap their internal edges by less than one physical pixel, extending matching UVs.
                float seam = .75f / Mathf.Max(.01f, Mathf.Abs(GUI.matrix.m00));
                float sourceSeam = seam * source.width / dest.width;
                if (slice > 0) { dest.xMin -= seam; source.xMin -= sourceSeam; }
                if (slice < 2) { dest.xMax += seam; source.xMax += sourceSeam; }
                DrawRaw(dest, background, source, true, false);
            }
        }

        private static Texture2D CreateMenuButton()
        {
            return CreateMenuButton(new Color(.73f, .96f, 1f), new Color(.025f, .12f, .57f),
                new Color(.2f, .81f, 1f), new Color(.02f, .22f, .98f), "Original glossy blue startup button");
        }

        private static Texture2D CreateMenuButton(Color rimTop, Color rimBottom, Color faceTop, Color faceBottom, string buttonName)
        {
            const int width = 512, height = 164;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = buttonName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect shadow = new Rect(8, 13, 496, 148), rim = new Rect(6, 3, 500, 149), face = new Rect(11, 7, 490, 137);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / face.height);
                Color color = new Color(.015f, .06f, .18f, .6f * RoundedCoverage(px, py, shadow, 70));
                color = Over(color, Color.Lerp(rimTop, rimBottom, t), RoundedCoverage(px, py, rim, 72));
                float faceAlpha = RoundedCoverage(px, py, face, 67);
                color = Over(color, Color.Lerp(faceTop, faceBottom, t), faceAlpha);
                float gloss = .48f * (1f - IntroEase((t - .02f) / .48f));
                color = Over(color, Color.white, faceAlpha * gloss);
                float dx = (px - 47f) / 13f, dy = (py - 25f) / 5f;
                color = Over(color, Color.white, .78f * faceAlpha * Mathf.Clamp01(1f - dx * dx - dy * dy));
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
        private static Texture2D CreateGoogleButtonSurface()
        {
            const int width = 512, height = 164;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "White Google startup button capsule",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect shadow = new Rect(8, 13, 496, 148), rim = new Rect(6, 3, 500, 149), face = new Rect(11, 7, 490, 137);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / face.height);
                Color color = new Color(.08f, .08f, .09f, .16f * RoundedCoverage(px, py, shadow, 70));
                color = Over(color, new Color(.79f, .80f, .82f), RoundedCoverage(px, py, rim, 72));
                float faceAlpha = RoundedCoverage(px, py, face, 67);
                color = Over(color, Color.Lerp(new Color(1f, 1f, 1f), new Color(.965f, .965f, .965f), t), faceAlpha);
                float gloss = .16f * (1f - IntroEase((t - .02f) / .48f));
                color = Over(color, Color.white, faceAlpha * gloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float RoundedCoverage(float x, float y, Rect r, float radius)
        {
            float dx = Mathf.Abs(x - r.center.x) - (r.width * .5f - radius);
            float dy = Mathf.Abs(y - r.center.y) - (r.height * .5f - radius);
            float distance = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude
                             + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
            return Mathf.Clamp01(.5f - distance);
        }
        private static Color Over(Color below, Color above, float coverage)
        {
            float alpha = above.a * coverage, remaining = below.a * (1f - alpha), total = alpha + remaining;
            if (total <= 0f) return Color.clear;
            return new Color((above.r * alpha + below.r * remaining) / total,
                             (above.g * alpha + below.g * remaining) / total,
                             (above.b * alpha + below.b * remaining) / total, total);
        }
    }
}
