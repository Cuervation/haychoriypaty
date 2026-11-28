using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        // Original single-stroke lettering, rasterized once to a shared grainy atlas.
        // The sign art stays empty; all editable names, prices and countdown digits use this ink.
        private sealed class RoundPriceBoardChalk : IDisposable
        {
            private const int Columns = 8, GlyphWidth = 44, GlyphHeight = 68;
            private const float Density = 4f;
            private const string Alphabet = "ABCDEFHILNOPRSTUVYZÍ0123456789$-";
            // Coordinates are in chalk units; negative values lift the chalk between strokes.
            private static readonly sbyte[][] Strokes =
            {
                new sbyte[] { 0,14, 4,2, 5,3, 8,14, -1, 2,10, 7,9 }, // A
                new sbyte[] { 1,14, 1,3, 5,2, 7,3, 7,5, 5,7, 1,8, 5,7, 8,9, 8,11, 6,13, 1,14 },
                new sbyte[] { 8,3, 6,2, 3,3, 1,6, 0,10, 2,13, 5,14, 8,12 },
                new sbyte[] { 1,14, 1,3, 4,2, 7,4, 8,7, 8,10, 6,13, 1,14 },
                new sbyte[] { 8,2, 1,3, 1,14, 8,13, -1, 1,8, 7,7 },
                new sbyte[] { 1,14, 1,3, 8,2, -1, 1,8, 7,7 },
                new sbyte[] { 1,2, 1,14, -1, 8,2, 7,14, -1, 1,8, 7,7 },
                new sbyte[] { 2,3, 7,2, -1, 5,3, 4,14, -1, 1,14, 7,13 },
                new sbyte[] { 2,2, 1,14, 8,13 },
                new sbyte[] { 0,14, 1,2, 7,14, 8,2 },
                new sbyte[] { 5,2, 2,3, 0,7, 0,11, 2,14, 5,14, 7,12, 8,8, 8,5, 7,3, 5,2 },
                new sbyte[] { 1,14, 1,3, 5,2, 7,3, 8,5, 7,7, 5,8, 1,8 },
                new sbyte[] { 1,14, 1,3, 5,2, 7,3, 8,5, 7,7, 5,8, 1,8, -1, 4,8, 8,14 },
                new sbyte[] { 8,3, 5,2, 2,3, 1,5, 3,7, 6,8, 8,10, 7,12, 4,14, 1,13 },
                new sbyte[] { 0,3, 8,2, -1, 4,3, 4,14 },
                new sbyte[] { 1,2, 0,10, 1,13, 4,14, 7,12, 8,2 },
                new sbyte[] { 0,2, 2,9, 4,14, 8,2 },
                new sbyte[] { 0,2, 4,8, 8,2, -1, 4,8, 3,14 },
                new sbyte[] { 1,3, 8,2, 0,14, 8,13 },
                new sbyte[] { 2,4, 7,4, -1, 5,4, 4,14, -1, 1,14, 7,13, -1, 5,2, 7,0 },
                new sbyte[] { 5,2, 2,3, 1,6, 0,11, 2,14, 5,14, 7,11, 8,6, 7,3, 5,2 },
                new sbyte[] { 1,6, 5,2, 4,14, -1, 1,14, 8,14 },
                new sbyte[] { 0,5, 2,3, 5,2, 7,3, 8,5, 6,8, 1,12, 0,14, 8,13 },
                new sbyte[] { 1,4, 4,2, 7,2, 8,4, 7,6, 4,8, 7,8, 8,10, 7,12, 5,14, 2,14, 0,12 },
                new sbyte[] { 5,2, 0,10, 8,9, -1, 7,3, 6,14 },
                new sbyte[] { 8,2, 2,3, 1,8, 5,7, 7,8, 8,10, 7,12, 4,14, 1,13 },
                new sbyte[] { 7,2, 4,3, 2,6, 0,10, 1,13, 4,14, 7,12, 8,9, 7,7, 4,7, 1,9 },
                new sbyte[] { 0,3, 8,2, 5,8, 2,14, -1, 2,8, 7,8 },
                new sbyte[] { 5,2, 7,2, 8,4, 6,7, 3,8, 1,10, 0,12, 2,14, 5,14, 8,12, 8,10, 6,8, 3,7, 1,5, 2,3, 5,2 },
                new sbyte[] { 7,8, 4,9, 1,8, 0,6, 1,4, 4,2, 7,3, 8,6, 7,10, 5,13, 2,14 },
                new sbyte[] { 8,4, 5,3, 2,4, 1,6, 3,8, 7,9, 8,11, 5,13, 1,12, -1, 5,1, 4,15 },
                new sbyte[] { 0,9, 2,7, 5,6, 7,6, 9,7 }
            };

            private readonly Texture2D atlas, shadow;

            public RoundPriceBoardChalk()
            {
                int width = Columns * GlyphWidth;
                int height = Mathf.CeilToInt(Alphabet.Length / (float)Columns) * GlyphHeight;
                var pixels = new Color32[width * height];
                for (int glyph = 0; glyph < Alphabet.Length; glyph++)
                {
                    var stroke = Strokes[glyph];
                    for (int y = 0; y < GlyphHeight; y++)
                    for (int x = 0; x < GlyphWidth; x++)
                    {
                        Vector2 sample = new Vector2((x + .5f) / Density - 1.25f, (y + .5f) / Density);
                        sample.x -= (.46f - sample.y * .052f) * (glyph % 4 - 1);
                        float dist = float.PositiveInfinity;
                        Vector2 from = default;
                        bool penDown = false;
                        for (int i = 0; i < stroke.Length;)
                        {
                            if (stroke[i] < 0) { penDown = false; i++; continue; }
                            var to = new Vector2(stroke[i++], stroke[i++]);
                            if (penDown)
                            {
                                Vector2 line = to - from;
                                Vector2 closest = from + line * Mathf.Clamp01(Vector2.Dot(sample - from, line) / line.sqrMagnitude);
                                dist = Mathf.Min(dist, (sample - closest).sqrMagnitude);
                            }
                            from = to;
                            penDown = true;
                        }

                        float speckle = Grain(x, y, glyph);
                        float coverage = Mathf.Clamp01((.84f + speckle * .26f - Mathf.Sqrt(dist)) * Density);
                        float opacity = coverage * (.48f + .5f * speckle);
                        if (speckle < .10f) opacity *= .27f;
                        int atlasX = glyph % Columns * GlyphWidth + x;
                        int atlasY = glyph / Columns * GlyphHeight + GlyphHeight - 1 - y;
                        pixels[atlasY * width + atlasX] = new Color32(255, 255, 255, (byte)(opacity * 255f));
                    }
                }
                atlas = MakeTexture("Pizarra/Tiza", width, height, pixels);

                const int shadowSize = 48;
                var shade = new Color32[shadowSize * shadowSize];
                for (int y = 0; y < shadowSize; y++)
                for (int x = 0; x < shadowSize; x++)
                {
                    var p = new Vector2(2f * (x + .5f) / shadowSize - 1f, 2f * (y + .5f) / shadowSize - 1f);
                    float alpha = Mathf.Clamp01(1f - p.sqrMagnitude);
                    shade[y * shadowSize + x] = new Color32(30, 28, 29, (byte)(100f * alpha * alpha));
                }
                shadow = MakeTexture("Pizarra/Sombra", shadowSize, shadowSize, shade);
            }

            private static float Grain(int x, int y, int glyph)
            {
                unchecked
                {
                    uint n = (uint)x * 374761393u + (uint)y * 668265263u + (uint)glyph * 2246822519u;
                    n = (n ^ (n >> 13)) * 1274126177u;
                    return ((n ^ (n >> 16)) & 255) / 255f;
                }
            }

            private static Texture2D MakeTexture(string name, int width, int height, Color32[] pixels)
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                return texture;
            }

            private void DrawGlyph(int glyph, Rect bounds)
            {
                if (glyph < 0) return;
                float w = (float)GlyphWidth / atlas.width, h = (float)GlyphHeight / atlas.height;
                GUI.DrawTextureWithTexCoords(bounds, atlas, new Rect((glyph % Columns) * w, (glyph / Columns) * h, w, h), true);
            }

            public void Write(string letters, float x, float y, float height, TextAnchor align)
            {
                float width = height * GlyphWidth / GlyphHeight;
                float step = width * .91f;
                float length = width + step * (letters.Length - 1);
                if (align == TextAnchor.MiddleCenter) x -= length * .5f;
                else if (align == TextAnchor.MiddleRight) x -= length;
                for (int i = 0; i < letters.Length; i++)
                {
                    float offset = ((i * 9 + letters[i]) % 5 - 2) * height * .022f;
                    DrawGlyph(Alphabet.IndexOf(letters[i]), new Rect(x + i * step, y + offset, width, height));
                }
            }

            public void Rule(Rect bounds) => DrawGlyph(Alphabet.Length - 1, bounds);
            public void DrawShadow(Rect bounds) => GUI.DrawTexture(bounds, shadow);
            public void Dispose()
            {
                UnityEngine.Object.Destroy(atlas);
                UnityEngine.Object.Destroy(shadow);
            }
        }
    }
}
