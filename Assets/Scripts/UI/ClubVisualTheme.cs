using System;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Club palette keyed by the actual runtime level name, not by scene copies or a second level order.
    /// Add one entry here when a club is added to StreetSimulation.LevelNames.
    /// </summary>
    public sealed class ClubVisualTheme
    {
        private static readonly ClubVisualTheme[] ConfiguredThemes =
        {
            new ClubVisualTheme("Floresta / All Boys", new Color32(245, 242, 234, 255), new Color32(0, 0, 0, 255)),
            new ClubVisualTheme("Nueva Chicago", new Color32(0, 122, 61, 255), new Color32(0, 0, 0, 255)),
            new ClubVisualTheme("Liniers - Velez Sarsfield", new Color32(8, 48, 132, 255), new Color32(245, 242, 234, 255)),
            new ClubVisualTheme("Ferro Carril Oeste", new Color32(0, 105, 63, 255), new Color32(245, 242, 234, 255)),
            new ClubVisualTheme("Independiente de Avellaneda", new Color32(211, 29, 39, 255), new Color32(245, 242, 234, 255))
        };

        private static readonly ClubVisualTheme UnknownTheme =
            new ClubVisualTheme("Equipo sin configurar", new Color32(235, 230, 218, 255), new Color32(45, 43, 40, 255));

        public string ClubName { get; private set; }
        public Color PrimaryColor { get; private set; }
        public Color SecondaryColor { get; private set; }
        public Color? AccentColor { get; private set; }

        private ClubVisualTheme(string clubName, Color primary, Color secondary, Color? accent = null)
        {
            ClubName = clubName;
            PrimaryColor = primary;
            SecondaryColor = secondary;
            AccentColor = accent;
        }

        public static ClubVisualTheme ForLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= StreetSimulation.LevelNames.Length)
                return UnknownTheme;

            return ForClub(StreetSimulation.LevelNames[levelIndex]);
        }

        public static ClubVisualTheme ForClub(string clubName)
        {
            for (int i = 0; i < ConfiguredThemes.Length; i++)
            {
                if (string.Equals(ConfiguredThemes[i].ClubName, clubName, StringComparison.OrdinalIgnoreCase))
                    return ConfiguredThemes[i];
            }

            return UnknownTheme;
        }

        public Color GetPennantColor(int pennantIndex)
        {
            if (AccentColor.HasValue && pennantIndex >= 0 && pennantIndex % 6 == 4)
                return AccentColor.Value;

            return Mathf.Abs(pennantIndex) % 2 == 0 ? PrimaryColor : SecondaryColor;
        }
    }

    /// <summary>Creates the small shared footer pennant row once per active club palette.</summary>
    public static class ClubPennantRenderer
    {
        public const int PennantCount = 20;
        public const int TextureWidth = 1080;
        public const int TextureHeight = 160;
        public const float LogicalHeight = 80f;

        private static readonly Color32 Outline = new Color32(31, 25, 21, 255);
        private static readonly Color32 RopeHighlight = new Color32(206, 184, 151, 255);

        public static Texture2D CreateTexture(ClubVisualTheme theme, bool keepReadable = false)
        {
            if (theme == null) theme = ClubVisualTheme.ForLevel(-1);

            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, false)
            {
                name = "Club Pennants - " + theme.ClubName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[TextureWidth * TextureHeight];
            Vector2 ropeStart = new Vector2(0f, RopeY(0f));
            for (int i = 1; i <= TextureWidth; i++)
            {
                float t = i / (float)TextureWidth;
                Vector2 next = new Vector2(i, RopeY(t));
                DrawLine(pixels, ropeStart, next, Outline, 5f);
                DrawLine(pixels, ropeStart + new Vector2(0f, -1.5f), next + new Vector2(0f, -1.5f), RopeHighlight, 1.2f);
                ropeStart = next;
            }

            float pitch = TextureWidth / (float)PennantCount;
            for (int i = 0; i < PennantCount; i++)
            {
                float centerX = (i + .5f) * pitch;
                float leftX = centerX - pitch * .47f;
                float rightX = centerX + pitch * .47f;
                var left = new Vector2(leftX, RopeY(leftX / TextureWidth));
                var right = new Vector2(rightX, RopeY(rightX / TextureWidth));
                var tip = new Vector2(centerX, TextureHeight - 2f);
                Color fill = theme.GetPennantColor(i);

                FillTriangle(pixels, left, right, tip, fill);
                DrawLine(pixels, left, right, Outline, 4f);
                DrawLine(pixels, right, tip, Outline, 4f);
                DrawLine(pixels, tip, left, Outline, 4f);

                // A narrow shaded fold keeps the simple generated row in the game's inked, fabric-like style.
                Vector2 foldStart = Vector2.Lerp(left, right, .30f);
                Vector2 foldEnd = Vector2.Lerp(tip, Vector2.Lerp(left, right, .30f), .18f);
                Color fold = Color.Lerp(fill, Outline, .28f);
                fold.a = .65f;
                DrawLine(pixels, foldStart, foldEnd, fold, 2f);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }

        private static float RopeY(float t)
        {
            // The same shallow, down-curving cord profile as the existing All Boys footer artwork.
            return 9f + 67f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }

        private static void FillTriangle(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c, Color fill)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, TextureWidth - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, TextureWidth - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, TextureHeight - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, TextureHeight - 1);
            float height = Mathf.Max(1f, maxY - minY);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + .5f, y + .5f);
                if (!InsideTriangle(p, a, b, c)) continue;

                float depth = Mathf.Clamp01((y - minY) / height);
                float highlight = Mathf.Lerp(.11f, 0f, depth);
                Color shaded = Color.Lerp(fill, Color.white, highlight);
                SetPixel(pixels, x, y, shaded);
            }
        }

        private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }

        private static void DrawLine(Color32[] pixels, Vector2 a, Vector2 b, Color color, float thickness)
        {
            float distance = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            float radius = Mathf.Max(.5f, thickness * .5f);
            Color32 color32 = color;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, i / (float)steps);
                int cx = Mathf.RoundToInt(point.x), cy = Mathf.RoundToInt(point.y);
                int extent = Mathf.CeilToInt(radius);
                for (int y = cy - extent; y <= cy + extent; y++)
                for (int x = cx - extent; x <= cx + extent; x++)
                {
                    if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight) continue;
                    float dx = x - point.x, dy = y - point.y;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    BlendPixel(pixels, x, y, color32);
                }
            }
        }

        private static void SetPixel(Color32[] pixels, int x, int yFromTop, Color color)
        {
            if (x < 0 || x >= TextureWidth || yFromTop < 0 || yFromTop >= TextureHeight) return;
            Color32 value = color;
            pixels[(TextureHeight - 1 - yFromTop) * TextureWidth + x] = value;
        }

        private static void BlendPixel(Color32[] pixels, int x, int yFromTop, Color32 source)
        {
            int index = (TextureHeight - 1 - yFromTop) * TextureWidth + x;
            Color32 destination = pixels[index];
            float alpha = source.a / 255f;
            float inverse = 1f - alpha;
            pixels[index] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * alpha + destination.r * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * alpha + destination.g * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * alpha + destination.b * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.a + destination.a * inverse), 0, 255));
        }
    }
}
