using UnityEngine;

namespace HayChoriYPaty
{
    // Arte procedural de la vitrina de insignias (2026-11-26, a pedido; referencia: estantes de trofeos de un juego móvil,
    // llevado a la estética del juego). Cada insignia es una estatuilla del producto "fundida" en bronce, plata u oro a partir
    // de su propio ícono (street-items / botella de Coca), sobre un pedestal con placa; las que faltan son siluetas pálidas.
    // Se genera una sola vez (a 2x, junto al arte del ranking) y se cachea; no agrega assets al proyecto.
    public sealed partial class StreetView
    {
        private const int BadgeStatuetteBox = 84;
        private static readonly Color BadgeBronze = new Color(.80f, .43f, .16f), BadgeSilver = new Color(.70f, .76f, .86f), BadgeGold = new Color(1f, .75f, .12f);
        private static Color BadgeMetal(int tier) => tier == 0 ? BadgeBronze : tier == 1 ? BadgeSilver : BadgeGold;

        // Product silhouette sampled once from its icon: luminance and alpha, top-down, plus a dilated alpha for the outline.
        private sealed class BadgeShape { public int width, height; public float[] lum, alpha, halo; }
        private readonly BadgeShape[] badgeShapes = new BadgeShape[StreetSalesBadges.ProductCount];

        private bool BadgeShapesReady
        {
            get { for (int i = 0; i < badgeShapes.Length; i++) if (badgeShapes[i] == null) return false; return true; }
        }

        // Reads the icon atlases outside OnGUI (a Blit during a GUI repaint could disturb the GUI target).
        private void EnsureBadgeShapes()
        {
            if (BadgeShapesReady || items == null) return;
            Color32[] atlas = null, bottle = null;
            for (int product = 0; product < badgeShapes.Length; product++)
            {
                if (badgeShapes[product] != null) continue;
                bool useBottle = product == 4 && cocaBottle != null;
                Texture2D source = useBottle ? cocaBottle : items;
                Color32[] pixels = useBottle ? (bottle ?? (bottle = ReadBadgeSource(source))) : (atlas ?? (atlas = ReadBadgeSource(source)));
                if (pixels == null) return;
                Rect src = useBottle ? new Rect(0, 0, source.width, source.height) : Items[product];
                badgeShapes[product] = SampleBadgeShape(pixels, source.width, source.height, src);
            }
        }

        private static Color32[] ReadBadgeSource(Texture2D source)
        {
            if (source == null) return null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture staging = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, staging);
                RenderTexture.active = staging;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(false);
                return copy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(staging);
                if (Application.isPlaying) Destroy(copy); else DestroyImmediate(copy);
            }
        }

        private static BadgeShape SampleBadgeShape(Color32[] pixels, int texWidth, int texHeight, Rect src)
        {
            int box = BadgeStatuetteBox * RankingArtScale, pad = 3 * RankingArtScale;
            float scale = Mathf.Min((box - 2f * pad) / src.width, (box - 2f * pad) / src.height);
            int w = Mathf.CeilToInt(src.width * scale) + 2 * pad, h = Mathf.CeilToInt(src.height * scale) + 2 * pad;
            var shape = new BadgeShape { width = w, height = h, lum = new float[w * h], alpha = new float[w * h], halo = new float[w * h] };
            for (int oy = 0; oy < h; oy++)
            for (int ox = 0; ox < w; ox++)
            {
                // Box filter over the source pixels this output pixel covers (source rects use a top-left origin).
                float x0 = src.x + (ox - pad) / scale, x1 = x0 + 1f / scale, y0 = src.y + (oy - pad) / scale, y1 = y0 + 1f / scale;
                x0 = Mathf.Max(x0, src.x); x1 = Mathf.Min(x1, src.xMax); y0 = Mathf.Max(y0, src.y); y1 = Mathf.Min(y1, src.yMax);
                if (x1 <= x0 || y1 <= y0) continue;
                float weight = 0f, alpha = 0f, lum = 0f;
                for (int iy = Mathf.FloorToInt(y0); iy < Mathf.CeilToInt(y1); iy++)
                for (int ix = Mathf.FloorToInt(x0); ix < Mathf.CeilToInt(x1); ix++)
                {
                    if (ix < 0 || iy < 0 || ix >= texWidth || iy >= texHeight) continue;
                    float cover = (Mathf.Min(ix + 1f, x1) - Mathf.Max(ix, x0)) * (Mathf.Min(iy + 1f, y1) - Mathf.Max(iy, y0));
                    if (cover <= 0f) continue;
                    Color32 c = pixels[(texHeight - 1 - iy) * texWidth + ix];
                    float a = c.a / 255f;
                    weight += cover; alpha += cover * a;
                    lum += cover * a * (.30f * c.r + .59f * c.g + .11f * c.b) / 255f;
                }
                if (weight <= 0f) continue;
                int index = oy * w + ox;
                shape.alpha[index] = alpha / weight;
                shape.lum[index] = alpha > 0f ? lum / alpha : 0f;
            }
            int radius = 2 * RankingArtScale;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float best = 0f;
                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius + 1) continue;
                    int sx = x + dx, sy = y + dy;
                    if (sx < 0 || sy < 0 || sx >= w || sy >= h) continue;
                    best = Mathf.Max(best, shape.alpha[sy * w + sx]);
                }
                shape.halo[y * w + x] = best;
            }
            return shape;
        }

        // Cast-metal statuette of the product (earned) or its pale plaster silhouette (still to win).
        private Texture2D BadgeStatuette(int product, int tier, bool earned)
        {
            BadgeShape shape = product >= 0 && product < badgeShapes.Length ? badgeShapes[product] : null;
            if (shape == null) return null;
            string key = "badge-statuette" + product + "-" + tier + (earned ? "" : "-ghost");
            if (rankingArt.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            int w = shape.width, h = shape.height;
            Color metal = BadgeMetal(tier);
            Color dark = new Color(metal.r * .22f, metal.g * .17f, metal.b * .15f, 1f), mid = new Color(metal.r * .80f, metal.g * .78f, metal.b * .78f, 1f);
            Color light = Color.Lerp(metal, Color.white, .45f);
            Color edge = earned ? new Color(metal.r * .20f, metal.g * .13f, metal.b * .08f, 1f) : new Color(.70f, .62f, .52f, .55f);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                Color c = new Color(edge.r, edge.g, edge.b, edge.a * shape.halo[index]);
                float a = shape.alpha[index];
                if (a > 0f)
                {
                    float t = Mathf.Clamp01((shape.lum[index] - .08f) / .80f);
                    t = Mathf.Pow(t * t * (3f - 2f * t), earned ? 1.45f : 1f);
                    Color body;
                    if (earned)
                    {
                        body = t < .45f ? Color.Lerp(dark, mid, t / .45f) : t < .8f ? Color.Lerp(mid, metal, (t - .45f) / .35f) : Color.Lerp(metal, light, (t - .8f) / .2f);
                        // Polished highlight: a soft diagonal streak and a lit top edge, like cast metal under a spotlight.
                        float u = (float)x / w, v = (float)y / h;
                        float streak = Mathf.Exp(-Mathf.Pow((u * .78f + v * .52f - .40f) / .06f, 2f)) * .62f;
                        float top = Mathf.Clamp01(1f - v * 3.2f) * .12f;
                        body = Color.Lerp(body, Color.white, Mathf.Clamp01(streak + top));
                        body.a = a;
                    }
                    else
                    {
                        body = Color.Lerp(new Color(.88f, .83f, .75f), new Color(1f, .985f, .95f), .30f + .70f * t);
                        body.a = a * .80f;
                    }
                    c = Over(c, body, 1f);
                }
                pixels[(h - 1 - y) * w + x] = c;
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) {
                name = "Badge " + key, hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(false, true);
            rankingArt[key] = texture;
            return texture;
        }

        // Pedestal: a moulded cap, a column with room for the plaque and a wide base; walnut, slate or navy with metal trim.
        private void DrawBadgePedestal(Rect bounds, int tier, bool earned)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            Color stone = !earned ? new Color(.95f, .91f, .84f) : tier == 0 ? new Color(.44f, .24f, .11f)
                : tier == 1 ? new Color(.30f, .33f, .40f) : new Color(.07f, .13f, .34f);
            Color trim = earned ? BadgeMetal(tier) : new Color(1f, .98f, .93f);
            Color line = earned ? new Color(.12f, .05f, .01f) : new Color(.72f, .64f, .54f);
            float opacity = earned ? 1f : .78f;
            Texture2D t = RankingTexture("badge-pedestal" + tier + (earned ? "" : "-ghost") + w + "x" + h, w, h, (x, y) =>
            {
                Rect cap = new Rect(w * .06f, 0f, w * .88f, 9f), column = new Rect(w * .15f, 7f, w * .70f, h - 22f), foot = new Rect(0f, h - 16f, w, 15f);
                Color c = Color.clear;
                foreach (Rect part in new[] { column, foot, cap })
                {
                    Rect outline = new Rect(part.x - 1.2f, part.y - 1.2f, part.width + 2.4f, part.height + 2.4f);
                    c = Over(c, new Color(line.r, line.g, line.b, opacity), RoundedCoverage(x, y, outline, 4.2f));
                    float coverage = RoundedCoverage(x, y, part, 3f);
                    float v = Mathf.Clamp01((y - part.y) / part.height), side = Mathf.Abs((x - part.center.x) / (part.width * .5f));
                    bool metalPart = part.height < 10f;
                    Color face = metalPart ? Color.Lerp(Color.Lerp(trim, Color.white, .45f), trim * .78f, v)
                        : Color.Lerp(Color.Lerp(stone, Color.white, .22f), stone * .80f, v);
                    face = Color.Lerp(face, face * .70f, Mathf.Pow(Mathf.Clamp01(side), 5f));
                    if (!metalPart && y - part.y < 2.2f) face = Color.Lerp(trim, Color.white, .35f);
                    face.a = opacity;
                    c = Over(c, face, coverage);
                }
                return c;
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }

        // Warm sunburst behind the gold statuettes.
        private Texture2D BadgeGlow() => RankingTexture("badge-glow", 120, 120, (x, y) =>
        {
            Vector2 d = new Vector2(x - 60f, y - 60f);
            float r = d.magnitude / 60f, angle = Mathf.Atan2(d.y, d.x);
            float rays = .55f + .45f * Mathf.SmoothStep(0f, 1f, .5f + .5f * Mathf.Sin(angle * 12f));
            float a = Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r) * rays * .95f + Mathf.Clamp01(1f - r * 2.0f) * .55f;
            return new Color(1f, .90f, .45f, a);
        });

        // Wooden shelf: lit top face, darker front edge and a soft shadow below.
        private void DrawBadgeShelf(Rect bounds)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            Texture2D t = RankingTexture("badge-shelf" + w + "x" + h, w, h, (x, y) =>
            {
                Rect shadow = new Rect(4f, 6f, w - 8f, h - 6f), board = new Rect(0f, 0f, w, h - 6f);
                Color c = new Color(.10f, .04f, .0f, .35f * RoundedCoverage(x, y, shadow, 5f));
                c = Over(c, new Color(.20f, .09f, .02f), RoundedCoverage(x, y, board, 4f));
                Rect top = new Rect(1.5f, 1.5f, w - 3f, 5.5f), front = new Rect(1.5f, 7f, w - 3f, h - 14.5f);
                float grain = .04f * Mathf.Sin(x * .21f + Mathf.Sin(x * .047f) * 3f);
                c = Over(c, Color.Lerp(new Color(.93f, .70f, .42f), new Color(.80f, .55f, .29f), (y - top.y) / top.height) + new Color(grain, grain, grain, 0f), RoundedCoverage(x, y, top, 2f));
                c = Over(c, Color.Lerp(new Color(.62f, .37f, .17f), new Color(.45f, .25f, .10f), (y - front.y) / Mathf.Max(1f, front.height)) + new Color(grain, grain, grain, 0f), RoundedCoverage(x, y, front, 2f));
                return c;
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }

        // Display case for one product: dark-wood frame and a warm back wall with soft wainscot stripes and a top light.
        private void DrawBadgeCase(Rect bounds)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            Texture2D t = RankingTexture("badge-case" + w + "x" + h, w, h, (x, y) =>
            {
                Rect shadow = new Rect(0, 4, w, h - 4), rim = new Rect(0, 0, w, h - 4), face = new Rect(4, 4, w - 8, h - 12);
                Color c = new Color(.05f, .02f, .0f, .45f * RoundedCoverage(x, y, shadow, 16));
                c = Over(c, new Color(.20f, .09f, .02f), RoundedCoverage(x, y, rim, 16));
                float coverage = RoundedCoverage(x, y, face, 12);
                float v = Mathf.Clamp01((y - face.y) / face.height);
                Color wall = Color.Lerp(new Color(1f, .96f, .86f), new Color(.89f, .79f, .59f), v);
                float stripe = .5f + .5f * Mathf.Cos((x - face.x) / 26f * Mathf.PI * 2f);
                wall = Color.Lerp(wall, wall * .955f, Mathf.SmoothStep(.3f, .9f, stripe));
                Vector2 spot = new Vector2((x - w * .5f) / (w * .55f), (y - face.y) / (face.height * .7f));
                wall = Color.Lerp(wall, new Color(1f, .99f, .94f), .28f * Mathf.Clamp01(1f - spot.magnitude));
                // Inner shadow under the top frame.
                wall = Color.Lerp(wall, new Color(.55f, .38f, .20f), .30f * Mathf.Clamp01(1f - (y - face.y) / 9f));
                wall.a = 1f;
                return Over(c, wall, coverage);
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }
    }
}
