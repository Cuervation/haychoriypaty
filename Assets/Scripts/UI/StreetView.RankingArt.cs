using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    // Arte procedural del Ranking de la hinchada (2026-11-25, a pedido): tarjeta de podio celeste con rayos,
    // pedestales metálicos, medallones con laureles, estrella dorada y filas redondeadas con bloque de posición.
    // Se genera una sola vez por tamaño (a 2x) y se cachea; no agrega assets al proyecto.
    public sealed partial class StreetView
    {
        private enum RankingRowKind { Normal, Gold, Silver, Bronze, Mine, Free }
        private const int RankingArtScale = 2;
        private static readonly Dictionary<string, Texture2D> rankingArt = new Dictionary<string, Texture2D>();
        private GUIStyle rankingOutlineStyle;
        private GUIStyle rankingOutlineSource;

        private static Color RankingMetal(int place) => place == 0 ? RankingGold : place == 1 ? RankingSilver : RankingBronze;

        private static Texture2D RankingTexture(string key, int width, int height, System.Func<float, float, Color> pixel)
        {
            if (rankingArt.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            int pw = Mathf.Max(2, width * RankingArtScale), ph = Mathf.Max(2, height * RankingArtScale);
            var texture = new Texture2D(pw, ph, TextureFormat.RGBA32, false) {
                name = "Ranking " + key, hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[pw * ph];
            for (int y = 0; y < ph; y++)
            for (int x = 0; x < pw; x++)
                // Logical coordinates with y growing downwards, like the IMGUI layout.
                pixels[y * pw + x] = pixel((x + .5f) / RankingArtScale, height - (y + .5f) / RankingArtScale);
            texture.SetPixels(pixels); texture.Apply(false, true);
            rankingArt[key] = texture;
            return texture;
        }

        private void RankingDraw(Rect bounds, Texture2D texture, Color tint)
        {
            Color previous = GUI.color;
            GUI.color = new Color(previous.r * tint.r, previous.g * tint.g, previous.b * tint.b, previous.a * tint.a);
            GUI.DrawTexture(LayoutRect(bounds), texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        // White rounded rectangle, tinted when drawn (pills, chips, badges).
        private void RankingPill(Rect bounds, Color color, float radius = -1f)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            float r = radius < 0f ? h * .5f : radius;
            Texture2D t = RankingTexture("pill" + w + "x" + h + "r" + r, w, h,
                (x, y) => new Color(1f, 1f, 1f, RoundedCoverage(x, y, new Rect(0, 0, w, h), r)));
            RankingDraw(new Rect(bounds.x, bounds.y, w, h), t, color);
        }

        private static readonly Color RankingCardTop = new Color(.05f, .20f, .52f), RankingCardBottom = new Color(.33f, .70f, .96f);

        // Podium card: navy rim, cyan bevel and an Argentine-sky gradient with soft sun rays behind the champion.
        private void DrawRankingPodiumCard(Rect bounds)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            Texture2D t = RankingTexture("card" + w + "x" + h, w, h, (x, y) =>
            {
                Rect shadow = new Rect(0, 4, w, h - 4), rim = new Rect(0, 0, w, h - 4);
                Rect cyan = new Rect(3, 3, w - 6, h - 10), face = new Rect(6, 6, w - 12, h - 16);
                Color c = new Color(0f, .02f, .08f, .55f * RoundedCoverage(x, y, shadow, 20));
                c = Over(c, MarketNavy, RoundedCoverage(x, y, rim, 20));
                c = Over(c, MarketCyan, RoundedCoverage(x, y, cyan, 17));
                float coverage = RoundedCoverage(x, y, face, 14);
                float t01 = Mathf.Clamp01((y - face.y) / face.height);
                Color sky = Color.Lerp(RankingCardTop, RankingCardBottom, t01);
                Vector2 sun = new Vector2(w * .5f, h * .30f), d = new Vector2(x, y) - sun;
                float angle = Mathf.Atan2(d.y, d.x), dist = d.magnitude;
                float ray = Mathf.SmoothStep(0f, 1f, .5f + .5f * Mathf.Sin(angle * 14f)) * Mathf.Clamp01(1f - dist / (w * .62f));
                sky = Color.Lerp(sky, new Color(.80f, .93f, 1f), .16f * ray);
                sky = Color.Lerp(sky, new Color(1f, .93f, .62f), .30f * Mathf.Clamp01(1f - dist / (w * .26f)));
                // Stadium-floor band at the bottom so the pedestals stand on something.
                if (y > face.yMax - 22f) sky = Color.Lerp(sky, new Color(.03f, .13f, .36f), .55f);
                c = Over(c, sky, coverage);
                float gloss = .16f * Mathf.Clamp01(1f - (y - face.y) / (face.height * .22f));
                c = Over(c, Color.white, coverage * gloss);
                return c;
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }

        // Metal pedestal with rounded top corners, a lit top step and darker sides.
        private void DrawRankingPedestal(Rect bounds, int place)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height);
            Color metal = RankingMetal(place);
            Texture2D t = RankingTexture("pedestal" + place + "_" + w + "x" + h, w, h, (x, y) =>
            {
                Rect outer = new Rect(0, 0, w, h + 12), inner = new Rect(2, 2, w - 4, h + 12);
                Color c = Over(Color.clear, new Color(.16f, .07f, .02f), RoundedCoverage(x, y, outer, 10));
                float coverage = RoundedCoverage(x, y, inner, 8);
                float vy = Mathf.Clamp01(y / h), side = Mathf.Abs(x / w - .5f) * 2f;
                Color body = Color.Lerp(Color.Lerp(metal, Color.white, .30f), metal * .72f, vy);
                body = Color.Lerp(body, metal * .55f, Mathf.Pow(side, 4f) * .7f);
                if (y < 12f) body = Color.Lerp(metal, Color.white, .55f - y * .02f);
                else if (y < 14f) body = metal * .62f;
                body.a = 1f;
                return Over(c, body, coverage);
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }

        // Round avatar disc with shading; tinted per player.
        private Texture2D RankingDisc() => RankingTexture("disc", 64, 64, (x, y) =>
        {
            Vector2 d = new Vector2(x - 32f, y - 32f);
            float coverage = Mathf.Clamp01(32f - d.magnitude);
            float shade = Mathf.Lerp(1.08f, .78f, Mathf.Clamp01(y / 64f));
            float gloss = Mathf.Clamp01(1f - new Vector2((x - 30f) / 22f, (y - 18f) / 12f).magnitude) * .35f;
            float v = Mathf.Min(1f, shade + gloss);
            return new Color(v, v, v, coverage);
        });

        // Laurel wings around a medallion (white, tinted with the medal metal).
        private Texture2D RankingLaurel() => RankingTexture("laurel", 120, 84, (x, y) =>
        {
            // Units of the medallion diameter (60 px), origin on the medallion centre, y up.
            float ux = (x - 60f) / 60f, uy = (44f - y) / 60f, alpha = 0f, light = 0f;
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? 1f : -1f;
                for (int k = 0; k < 6; k++)
                {
                    float phi = (-72f + k * 23f) * Mathf.Deg2Rad;
                    Vector2 centre = new Vector2(side * .64f * Mathf.Cos(phi), .64f * Mathf.Sin(phi));
                    float tilt = phi + Mathf.PI * .5f + .45f;
                    Vector2 along = new Vector2(side * Mathf.Cos(tilt), Mathf.Sin(tilt));
                    Vector2 across = new Vector2(-along.y, along.x);
                    Vector2 p = new Vector2(ux, uy) - centre;
                    float a = .15f - k * .006f, b = .058f;
                    float u = Vector2.Dot(p, along) / a, v = Vector2.Dot(p, across) / b;
                    float e = Mathf.Sqrt(u * u + v * v);
                    float cov = Mathf.Clamp01((1f - e) * b * 60f * RankingArtScale * .5f + .5f);
                    if (cov > alpha) { alpha = cov; light = 1f - .35f * Mathf.Abs(v) - .15f * (u > 0 ? 0f : 1f); }
                }
            }
            return new Color(light, light, light, alpha);
        });

        // Five-point golden star with a dark outline.
        private Texture2D RankingStar() => RankingTexture("star", 32, 32, (x, y) =>
        {
            float outer = RankingStarCoverage(x, y, 16f, 16.8f, 15.6f), inner = RankingStarCoverage(x, y, 16f, 16.8f, 11.8f);
            Color c = Over(Color.clear, new Color(.24f, .10f, .02f), outer);
            Color gold = Color.Lerp(new Color(1f, .95f, .55f), new Color(.96f, .58f, .08f), Mathf.Clamp01((y - 4f) / 24f));
            return Over(c, gold, inner);
        });

        private static float RankingStarCoverage(float x, float y, float cx, float cy, float radius)
        {
            int hits = 0;
            for (int sy = 0; sy < 3; sy++)
            for (int sx = 0; sx < 3; sx++)
                if (RankingInsideStar(x - .5f + (sx + .5f) / 3f - cx, y - .5f + (sy + .5f) / 3f - cy, radius)) hits++;
            return hits / 9f;
        }

        private static bool RankingInsideStar(float x, float y, float radius)
        {
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                Vector2 a = RankingStarVertex(i, radius), b = RankingStarVertex(j, radius);
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        private static Vector2 RankingStarVertex(int index, float radius)
        {
            float r = index % 2 == 0 ? radius : radius * .45f, angle = -Mathf.PI * .5f + index * Mathf.PI / 5f;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        private void DrawRankingStar(Rect bounds) => GUI.DrawTexture(LayoutRect(bounds), RankingStar(), ScaleMode.ScaleToFit, true);

        // List row: drop shadow, dark-wood rim, cream (or green for the player) face and a coloured rank block on the left.
        private void DrawRankingRowBase(Rect bounds, RankingRowKind kind, float blockWidth)
        {
            int w = Mathf.CeilToInt(bounds.width), h = Mathf.CeilToInt(bounds.height), bw = Mathf.CeilToInt(blockWidth);
            Color block, faceTop, faceBottom;
            switch (kind)
            {
                case RankingRowKind.Gold: block = new Color(.95f, .66f, .10f); break;
                case RankingRowKind.Silver: block = new Color(.58f, .66f, .76f); break;
                case RankingRowKind.Bronze: block = new Color(.74f, .40f, .16f); break;
                case RankingRowKind.Mine: block = new Color(.22f, .62f, .16f); break;
                case RankingRowKind.Free: block = new Color(.45f, .36f, .26f); break;
                default: block = new Color(.16f, .52f, .90f); break;
            }
            if (kind == RankingRowKind.Mine) { faceTop = new Color(.86f, 1f, .62f); faceBottom = new Color(.60f, .86f, .34f); }
            else if (kind == RankingRowKind.Free) { faceTop = new Color(.40f, .30f, .21f); faceBottom = new Color(.33f, .24f, .16f); }
            else { faceTop = new Color(1f, .96f, .84f); faceBottom = new Color(.92f, .83f, .62f); }
            Texture2D t = RankingTexture("row" + kind + w + "x" + h + "b" + bw, w, h, (x, y) =>
            {
                Rect shadow = new Rect(0, 3, w, h - 3), rim = new Rect(0, 0, w, h - 3), face = new Rect(3, 3, w - 6, h - 9);
                Color c = new Color(.05f, .02f, .0f, .45f * RoundedCoverage(x, y, shadow, 12));
                c = Over(c, kind == RankingRowKind.Mine ? new Color(.10f, .30f, .06f) : new Color(.20f, .09f, .02f), RoundedCoverage(x, y, rim, 12));
                float coverage = RoundedCoverage(x, y, face, 9);
                float t01 = Mathf.Clamp01((y - face.y) / face.height);
                Color fill = x < bw ? Color.Lerp(Color.Lerp(block, Color.white, .25f), block * .80f, t01) : Color.Lerp(faceTop, faceBottom, t01);
                fill.a = 1f;
                if (x >= bw && x < bw + 2f) fill = new Color(.20f, .09f, .02f, 1f) * .9f;
                c = Over(c, fill, coverage);
                float gloss = .28f * Mathf.Clamp01(1f - (y - face.y) / (face.height * .36f));
                return Over(c, Color.white, coverage * gloss);
            });
            GUI.DrawTexture(LayoutRect(new Rect(bounds.x, bounds.y, w, h)), t, ScaleMode.StretchToFill, true);
        }

        private static readonly Color[] RankingAvatarColors = {
            new Color(.86f, .18f, .16f), new Color(.15f, .38f, .86f), new Color(.12f, .58f, .28f), new Color(.56f, .24f, .70f),
            new Color(.96f, .55f, .10f), new Color(.08f, .60f, .70f), new Color(.80f, .14f, .44f), new Color(.45f, .30f, .16f) };

        private static Color RankingAvatarColor(string uid)
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(uid)) foreach (char ch in uid) hash = unchecked(hash * 31 + ch);
            return RankingAvatarColors[(hash & int.MaxValue) % RankingAvatarColors.Length];
        }

        private static string RankingInitial(LeaderboardEntryView entry) =>
            entry.empty || string.IsNullOrEmpty(entry.nickname) ? "?" : entry.nickname.Trim().Substring(0, 1).ToUpperInvariant();

        // Medallion: metal ring, shaded disc in the player's colour and the apodo's initial in outlined letters.
        private void DrawRankingMedallion(Rect bounds, Color ring, LeaderboardEntryView entry, bool laurels)
        {
            float s = bounds.width;
            if (laurels) RankingDraw(new Rect(bounds.center.x - s, bounds.center.y - s * 44f / 60f, s * 2f, s * 84f / 60f), RankingLaurel(), ring);
            Texture2D disc = RankingDisc();
            RankingDraw(new Rect(bounds.x - 2, bounds.y - 1, s + 4, s + 4), disc, new Color(.10f, .04f, .01f, .9f));
            RankingDraw(bounds, disc, ring);
            float inset = Mathf.Max(3f, s * .09f);
            Rect inner = new Rect(bounds.x + inset, bounds.y + inset, s - inset * 2f, s - inset * 2f);
            RankingDraw(inner, disc, entry.empty ? new Color(.30f, .24f, .18f) : RankingAvatarColor(entry.uid));
            RankingOutlined(new Rect(inner.x, inner.y + inner.height * .02f, inner.width, inner.height), RankingInitial(entry),
                Mathf.RoundToInt(inner.height * .62f), entry.empty ? new Color(.75f, .66f, .52f) : Color.white, new Color(.10f, .04f, .01f));
        }

        // Text with the dark outline the standard buttons use.
        private void RankingOutlined(Rect bounds, string value, int size, Color fill, Color outline,
            TextAnchor alignment = TextAnchor.MiddleCenter, float thicknessScale = 1f)
        {
            if (menuTitle == null) return;
            if (rankingOutlineStyle == null || rankingOutlineSource != menuTitle)
            {
                rankingOutlineSource = menuTitle;
                rankingOutlineStyle = new GUIStyle(menuTitle) { padding = new RectOffset(), margin = new RectOffset(), wordWrap = false };
            }
            GUIStyle style = rankingOutlineStyle;
            style.alignment = alignment;
            style.fontSize = FitMarketTextFontSize(style, value, size, new Vector2(bounds.width - 4f, bounds.height));
            float o = Mathf.Max(1.2f, style.fontSize * .075f) * thicknessScale;
            style.normal.textColor = style.hover.textColor = outline;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    GUI.Label(LayoutRect(new Rect(bounds.x + x * o, bounds.y + y * o, bounds.width, bounds.height)), value, style);
            style.normal.textColor = style.hover.textColor = fill;
            GUI.Label(LayoutRect(bounds), value, style);
        }

        // Lightweight view over a leaderboard row so empty podium/list slots draw with the same code.
        private struct LeaderboardEntryView
        {
            public bool empty;
            public string uid, nickname;
            public long points, wonLevels;
            public static LeaderboardEntryView From(Cloud.LeaderboardEntry entry) => entry == null
                ? new LeaderboardEntryView { empty = true }
                : new LeaderboardEntryView { uid = entry.uid, nickname = entry.nickname, points = entry.points, wonLevels = entry.wonLevels };
        }
    }
}
