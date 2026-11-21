using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Pantalla de victoria, Chulengo y recibo del mercado. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private const float ChulengoPendingButtonGap = 25f;
        private const float ChulengoPendingButtonSpacing = 8f;
        private const float ChulengoPendingButtonBottomInset = 16f;
        private const float ChulengoPendingButtonHeightMultiplier = 1.30f;
        private static readonly Color ChulengoPendingButtonTextColor = Color.black;
        private static float ChulengoPendingButtonHeight(float canvasHeight) =>
            52f * ChulengoPendingButtonHeightMultiplier * Mathf.Clamp(canvasHeight / 1200f, .65f, 1f);

        public static Rect ChulengoPendingCollectBoundsForFrame(Rect frame, float canvasHeight)
        {
            float width = (frame.width - ChulengoPendingButtonSpacing) * .5f;
            return new Rect(frame.x, frame.yMax + ChulengoPendingButtonGap, width,
                ChulengoPendingButtonHeight(canvasHeight));
        }

        public static Rect ChulengoPendingMultiplyBoundsForFrame(Rect frame, float canvasHeight)
        {
            Rect collect = ChulengoPendingCollectBoundsForFrame(frame, canvasHeight);
            return new Rect(collect.xMax + ChulengoPendingButtonSpacing, collect.y, collect.width, collect.height);
        }

        public static Rect ChulengoFinalCollectBoundsForFrame(Rect frame) => VictorySlot(frame, VictoryExit);

        private static Rect VictoryPopupBounds(float canvasHeight, bool chulengo = false)
        {
            float topHudReserve = chulengo ? 224f : 110f;
            const float screenInset = 16f;
            float resultActionReserve = chulengo
                ? ChulengoPendingButtonGap + ChulengoPendingButtonHeight(canvasHeight)
                : 72f;
            float resultButtonGap = chulengo ? ChulengoPendingButtonBottomInset : 8f;
            // Chulengo reserves the shared horizontal action row below the popup and keeps the status HUD above the crest.
            // In the square stress view the usual top padding leaves the portrait popup too small for the prize text.
            if (chulengo && canvasHeight < 600f)
                topHudReserve = HudQuotaGridBounds(TopHudCutoutBounds(new Vector2(Screen.width, Screen.height),
                    Screen.cutouts)).yMax + 10f;
            float availableHeight = Mathf.Max(1f, canvasHeight - topHudReserve - screenInset - resultActionReserve - resultButtonGap);
            float horizontalInset = chulengo ? 32f : 24f;
            float scale = Mathf.Min((W - horizontalInset) / VictoryPopupArtSize.x, availableHeight / VictoryPopupArtSize.y);
            Vector2 size = VictoryPopupArtSize * scale;
            float y = topHudReserve + screenInset + (availableHeight - size.y) * .5f;
            return new Rect((W - size.x) * .5f, y, size.x, size.y);
        }
        private static Rect VictorySlot(Rect frame, Rect normalized) => new Rect(
            frame.x + normalized.x * frame.width, frame.y + normalized.y * frame.height,
            normalized.width * frame.width, normalized.height * frame.height);

        private Texture2D ClubColoredVictoryPopup(int levelIndex)
        {
            if (victoryPopup == null) return null;
            if (clubColoredVictoryPopupLevel == levelIndex)
                return clubColoredVictoryPopup != null ? clubColoredVictoryPopup : victoryPopup;

            if (clubColoredVictoryPopup != null) Destroy(clubColoredVictoryPopup);
            clubColoredVictoryPopup = CreateClubColoredVictoryPopup(victoryPopup, ClubVisualTheme.ForLevel(levelIndex));
            clubColoredVictoryPopupLevel = levelIndex;
            return clubColoredVictoryPopup != null ? clubColoredVictoryPopup : victoryPopup;
        }

        private static Texture2D CreateClubColoredVictoryPopup(Texture2D source, ClubVisualTheme theme, bool keepReadable = false)
        {
            if (source == null || theme == null || !source.isReadable) return null;

            Color32[] pixels;
            try { pixels = source.GetPixels32(); }
            catch (UnityException) { return null; }

            int width = source.width, height = source.height;
            Color32 primary = theme.PrimaryColor, secondary = theme.SecondaryColor;
            int changed = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!IsChulengoFlagPixel(x, y, width, height)) continue;
                int index = y * width + x;
                Color32 sourcePixel = pixels[index];
                if (sourcePixel.a < 48) continue;

                int min = Mathf.Min(sourcePixel.r, Mathf.Min(sourcePixel.g, sourcePixel.b));
                int max = Mathf.Max(sourcePixel.r, Mathf.Max(sourcePixel.g, sourcePixel.b));
                // The poles and gold finials are warm-colored, while the two cloth flags are grayscale.
                if (max - min > 48) continue;

                float luminance = (sourcePixel.r * .299f + sourcePixel.g * .587f + sourcePixel.b * .114f) / 255f;
                Color32 band = ChulengoFlagBandColor(luminance, primary, secondary);
                float shade = luminance < .5f
                    ? Mathf.Lerp(.72f, .96f, Mathf.Clamp01(luminance * 2f))
                    : Mathf.Lerp(.90f, 1f, Mathf.Clamp01((luminance - .5f) * 2f));
                pixels[index] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.r * shade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.g * shade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.b * shade), 0, 255), sourcePixel.a);
                changed++;
            }

            if (changed == 0) return null;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Chulengo victory popup - " + theme.ClubName,
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };
            result.SetPixels32(pixels);
            result.Apply(false, !keepReadable);
            return result;
        }

        private static bool IsChulengoFlagPixel(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0) return false;
            float u = (x + .5f) / width, v = (y + .5f) / height;
            if (v < .87f || v > .992f) return false;
            return u >= .15f && u <= .305f || u >= .695f && u <= .85f;
        }

        private static Color32 ChulengoFlagBandColor(float luminance, Color32 primary, Color32 secondary)
        {
            float primaryLuminance = primary.r * .299f + primary.g * .587f + primary.b * .114f;
            float secondaryLuminance = secondary.r * .299f + secondary.g * .587f + secondary.b * .114f;
            bool primaryIsDarker = primaryLuminance <= secondaryLuminance;
            bool sourceIsLightStripe = luminance >= .5f;
            if (Mathf.Abs(primaryLuminance - secondaryLuminance) < 8f)
                return sourceIsLightStripe ? secondary : primary;
            return sourceIsLightStripe == primaryIsDarker ? secondary : primary;
        }
        private Rect ChulengoMultiplyBounds()
        {
            bool pendingHorizontalActions = game != null && game.HasPendingChulengo && !game.ChulengoChoiceLocked;
            Rect frame = VictoryPopupBounds(logicalCanvasHeight, true);
            return pendingHorizontalActions
                ? ChulengoPendingMultiplyBoundsForFrame(frame, logicalCanvasHeight)
                : ChulengoMultiplyBoundsForFrame(frame);
        }
        // Public for focused geometry checks; pointer and visible control share this normalized frame.
        public static Rect ChulengoMultiplyBoundsForFrame(Rect frame) => VictorySlot(frame, new Rect(.20f, .935f, .60f, .095f));
        private static readonly string[] ChulengoLabels = { "×0", "×1,5", "×1", "×2", "×1", "×1,5", "×0" };
        private static readonly Color[] ChulengoColors = {
            new Color(.76f, .12f, .055f), new Color(.94f, .78f, .15f), new Color(.12f, .46f, .82f),
            new Color(.96f, .64f, .08f), new Color(.12f, .46f, .82f), new Color(.94f, .78f, .15f),
            new Color(.76f, .12f, .055f)
        };
        private static Texture2D CreateChulengoMeterTexture()
        {
            const int width = 1152, height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo forged ember meter", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color clear = new Color(0, 0, 0, 0);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            const float cx = 576f, cy = 30f, outerX = 554f, outerY = 326f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = x + .5f - cx, dy = y + .5f - cy;
                    if (dy < 0f) continue;
                    float outerRadius = Mathf.Sqrt(dx * dx / (outerX * outerX) + dy * dy / (outerY * outerY));
                    if (outerRadius > 1f) continue;

                    Color pixel;
                    if (outerRadius < .695f)
                    {
                        // The transparent inner bowl reveals the authored coal-and-choripan art beneath the live sectors.
                        pixel = clear;
                    }
                    else if (outerRadius < .735f || outerRadius > .947f)
                    {
                        float shine = .5f + .5f * Mathf.Sin(dy * .047f + dx * .011f);
                        if (outerRadius > .947f)
                        {
                            // A dark forged outer rim reads like the charcoal steel bezel in the reference.
                            float highlight = Mathf.Clamp01(.14f + shine * .32f);
                            pixel = Color.Lerp(new Color(.055f, .052f, .048f), new Color(.50f, .42f, .29f), highlight);
                        }
                        else
                        {
                            float rim = Mathf.InverseLerp(.695f, .735f, outerRadius);
                            pixel = Color.Lerp(new Color(.12f, .09f, .075f), new Color(.83f, .68f, .38f), Mathf.Clamp01(.23f + shine * .62f));
                            if (rim > .55f && (x % 19 == 0 || y % 19 == 0)) pixel = Color.Lerp(pixel, new Color(.98f, .84f, .56f), .34f);
                        }
                    }
                    else
                    {
                        // Equal angular slices keep the visible pointer angle identical to gameplay's sector math.
                        float angle = Mathf.Atan2(dy, dx);
                        float position = Mathf.Clamp01(1f - angle / Mathf.PI);
                        int sector = Mathf.Min(6, Mathf.FloorToInt(position * 7f));
                        float edge = Mathf.Min(position * 7f - sector, sector + 1f - position * 7f);
                        if (edge < .022f) pixel = new Color(.08f, .045f, .025f);
                        else
                        {
                            float radialLight = .72f + .24f * Mathf.Sin((outerRadius - .735f) / .212f * Mathf.PI);
                            float grainNoise = Mathf.PerlinNoise(x * .018f, y * .033f);
                            pixel = ChulengoColors[sector] * Mathf.Clamp(radialLight + (grainNoise - .5f) * .15f, .58f, 1.08f);
                            if (outerRadius > .91f) pixel = Color.Lerp(pixel, new Color(.13f, .07f, .04f), Mathf.InverseLerp(.91f, .947f, outerRadius) * .7f);
                        }
                    }

                    // Six small brass rivets lock the two forged rims together.
                    float angleAtRivet = Mathf.Atan2(dy / outerY, dx / outerX);
                    float rivetPosition = Mathf.Clamp01(1f - angleAtRivet / Mathf.PI);
                    int rivet = Mathf.RoundToInt(rivetPosition * 6f);
                    float rivetAngle = Mathf.PI * (1f - rivet / 6f);
                    float boltX = cx + Mathf.Cos(rivetAngle) * outerX * .972f;
                    float boltY = cy + Mathf.Sin(rivetAngle) * outerY * .972f;
                    float bolt = Mathf.Sqrt((x + .5f - boltX) * (x + .5f - boltX) + (y + .5f - boltY) * (y + .5f - boltY));
                    if (bolt < 13f && outerRadius >= .695f) pixel = bolt < 8.1f ? new Color(.95f, .67f, .19f) : new Color(.10f, .065f, .04f);
                    pixels[y * width + x] = pixel;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private const int ChulengoNeedleThicknessScale = 3;
        private static float ChulengoNeedleHalfWidthAt(float t)
        {
            const float currentBaseHalfWidth = 18f + 1.2f;
            const float currentTipHalfWidth = 1.2f;
            return Mathf.Lerp(currentBaseHalfWidth * ChulengoNeedleThicknessScale, currentTipHalfWidth, Mathf.Clamp01(t));
        }

        private static float ChulengoNeedleDisplayHeight(float meterHeight) =>
            Mathf.Max(10f, meterHeight * .16f) * ChulengoNeedleThicknessScale;

        private static Texture2D CreateChulengoNeedleTexture()
        {
            const int width = 512, height = 64 * ChulengoNeedleThicknessScale, tip = 494;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo red pointer", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float t = Mathf.Clamp01((x - 8f) / (tip - 8f));
                    float half = ChulengoNeedleHalfWidthAt(t);
                    float distance = Mathf.Abs(y + .5f - height * .5f);
                    if (x < 8 || x > tip || distance > half + 2f) pixels[y * width + x] = new Color(0, 0, 0, 0);
                    else if (distance > half) pixels[y * width + x] = new Color(.09f, .035f, .025f, 1f);
                    else if (distance < half * .26f) pixels[y * width + x] = new Color(1f, .55f, .36f, 1f);
                    else pixels[y * width + x] = new Color(.83f, .08f, .045f, 1f);
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static Texture2D CreateChulengoHubTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Chulengo steel pivot", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f - size * .5f) / (size * .5f), dy = (y + .5f - size * .5f) / (size * .5f);
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color pixel = new Color(0, 0, 0, 0);
                    if (radius <= 1f)
                    {
                        if (radius > .84f) pixel = new Color(.10f, .065f, .035f);
                        else if (radius > .68f) pixel = Color.Lerp(new Color(.95f, .71f, .23f), new Color(.44f, .24f, .055f), radius);
                        else
                        {
                            float light = Mathf.Clamp01(.62f + .28f * (-dy) + .16f * Mathf.Sin((dx + dy) * 8f));
                            pixel = Color.Lerp(new Color(.20f, .23f, .25f), new Color(.88f, .91f, .90f), light);
                        }
                    }
                    pixels[y * size + x] = pixel;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static Texture2D CreateChulengoParchmentTexture()
        {
            const int width = 512, height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo textured parchment panel", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    float cornerX = Mathf.Max(0f, 16f - Mathf.Min(x, width - 1 - x));
                    float cornerY = Mathf.Max(0f, 16f - Mathf.Min(y, height - 1 - y));
                    bool roundedCorner = cornerX * cornerX + cornerY * cornerY > 16f * 16f;
                    if (roundedCorner) { pixels[y * width + x] = new Color(0, 0, 0, 0); continue; }
                    float mottling = Mathf.PerlinNoise(x * .026f, y * .031f);
                    float cloudy = Mathf.PerlinNoise(x * .008f, y * .012f);
                    float fineGrain = Mathf.PerlinNoise(x * .14f, y * .16f);
                    float fibers = Mathf.Sin(x * 1.71f + y * .91f) * Mathf.Sin(x * .17f - y * .27f) * .038f;
                    float shade = Mathf.Clamp01(.56f + (mottling - .5f) * .84f + (cloudy - .5f) * .92f + (fineGrain - .5f) * .22f + fibers);
                    Color paper = Color.Lerp(new Color(.76f, .53f, .28f), new Color(1f, .95f, .78f), shade);
                    if (fineGrain > .78f && mottling < .48f) paper = Color.Lerp(paper, new Color(.64f, .37f, .18f), .17f);
                    if (edge < 3f) paper = new Color(.20f, .075f, .025f);
                    else if (edge < 6f) paper = new Color(.71f, .39f, .12f);
                    else if (edge < 9f) paper = new Color(.99f, .84f, .54f);
                    pixels[y * width + x] = paper;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static bool ChulengoTicketShapeContains(float x, float y, float inset)
        {
            float span = 1f - inset * 2f;
            x = (x - inset) / span;
            y = (y - inset) / span;
            if (x < 0f || x > 1f || y < 0f || y > 1f) return false;

            const float corner = .075f;
            if (y < corner && (x < corner - y || x > 1f - corner + y)) return false;
            if (y > 1f - corner && (x < corner - (1f - y) || x > 1f - corner + (1f - y))) return false;

            float notchProgress = Mathf.Clamp01(1f - Mathf.Abs(y - .5f) / .09f);
            float notch = .065f * Mathf.Sqrt(Mathf.Max(0f, 1f - (1f - notchProgress) * (1f - notchProgress)));
            return x >= notch && x <= 1f - notch;
        }

        private static Vector2[] ChulengoTicketStarPoints(float outerRadius)
        {
            var points = new Vector2[10];
            float innerRadius = outerRadius * .38f;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.PI * .5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? outerRadius : innerRadius;
                points[i] = new Vector2(.5f + Mathf.Cos(angle) * radius, .5f + Mathf.Sin(angle) * radius);
            }
            return points;
        }

        private static bool ChulengoTicketPolygonContains(float x, float y, Vector2[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[i], b = points[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static Texture2D CreateChulengoTicketTexture()
        {
            // A runtime vector-style coupon keeps the yellow stub, red perforations and blue star crisp at every UI scale.
            const int width = 512, height = 320;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Chulengo illustrated ticket badge",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            Color32 navy = new Color32(9, 24, 58, 255);
            Color32 gold = new Color32(255, 166, 0, 255);
            Color32 yellow = new Color32(255, 207, 23, 255);
            Color32 face = new Color32(255, 226, 89, 255);
            Color32 red = new Color32(204, 37, 37, 255);
            Color32 starBlue = new Color32(20, 78, 194, 255);
            Color32 starHighlight = new Color32(43, 113, 232, 255);
            Vector2[] starOutline = ChulengoTicketStarPoints(.29f);
            Vector2[] starFace = ChulengoTicketStarPoints(.26f);

            for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float x = (px + .5f) / width;
                float y = (py + .5f) / height;
                Color32 pixel = new Color32(0, 0, 0, 0);
                if (ChulengoTicketShapeContains(x, y, 0f)) pixel = navy;
                if (ChulengoTicketShapeContains(x, y, .022f)) pixel = gold;
                if (ChulengoTicketShapeContains(x, y, .047f)) pixel = yellow;
                if (ChulengoTicketShapeContains(x, y, .075f)) pixel = face;

                bool horizontalDash = (y >= .145f && y <= .17f || y >= .83f && y <= .855f) && x >= .13f && x <= .87f &&
                    Mathf.Repeat((x - .13f) / .072f, 1f) < .58f;
                bool verticalDash = (x >= .13f && x <= .155f || x >= .845f && x <= .87f) && y >= .19f && y <= .81f &&
                    Mathf.Repeat((y - .19f) / .072f, 1f) < .58f;
                if ((horizontalDash || verticalDash) && pixel.a > 0) pixel = red;

                if (ChulengoTicketPolygonContains(x, y, starOutline)) pixel = navy;
                if (ChulengoTicketPolygonContains(x, y, starFace))
                    pixel = y > .5f ? starHighlight : starBlue;
                pixels[py * width + px] = pixel;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Rect FitChulengoMeter(Rect container)
        {
            const float aspect = 3f;
            float height = Mathf.Min(container.height, container.width / aspect);
            float width = height * aspect;
            return new Rect(container.center.x - width * .5f, container.center.y - height * .5f, width, height);
        }
        private void DrawChulengoMeter(Rect container, bool locked, int selectedSector, int labelFontSize)
        {
            Rect bounds = FitChulengoMeter(container);
            float pivotY = bounds.yMax - bounds.height * (30f / 384f);
            if (chulengoBowl != null)
            {
                float bowlWidth = bounds.width * .69f;
                float bowlHeight = bounds.height * .69f;
                Rect bowl = new Rect(bounds.center.x - bowlWidth * .5f, pivotY - bowlHeight, bowlWidth, bowlHeight);
                GUI.DrawTexture(bowl, chulengoBowl, ScaleMode.StretchToFill, true);
            }
            if (chulengoMeter != null) GUI.DrawTexture(bounds, chulengoMeter, ScaleMode.StretchToFill, true);
            if (!locked || selectedSector >= 0)
            {
                float position = locked ? game.ChulengoSelectedPosition : StreetChulengo.PositionAtTime(Time.unscaledTime);
                Vector2 center = new Vector2(bounds.center.x, bounds.yMax - bounds.height * (30f / 384f));
                float rotation = 180f * position - 180f;
                Color old = GUI.color;
                GUI.color = Color.white;
                Matrix4x4 matrix = GUI.matrix;
                try
                {
                    // Rotate in this local canvas before the outer viewport scale is applied.
                    // The artwork's pointed ray is anchored at the exact saved needle position.
                    GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(center.x, center.y, 0f))
                        * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation))
                        * Matrix4x4.Translate(new Vector3(-center.x, -center.y, 0f));
                    // Follow the elliptical rim so the pointer reaches the same visual distance
                    // at the center and at either end of the seven-sector arc.
                    float angle = Mathf.PI * (1f - position);
                    float outerX = bounds.width * (554f / 1152f);
                    float outerY = bounds.height * (326f / 384f);
                    float length = .96f / Mathf.Sqrt(Mathf.Cos(angle) * Mathf.Cos(angle) / (outerX * outerX)
                        + Mathf.Sin(angle) * Mathf.Sin(angle) / (outerY * outerY));
                    // Triple canvas and display thickness together to widen the base without enlarging the tip.
                    float needleHeight = ChulengoNeedleDisplayHeight(bounds.height);
                    Rect needle = new Rect(center.x, center.y - needleHeight * .5f, length, needleHeight);
                    if (chulengoNeedle != null) GUI.DrawTexture(needle, chulengoNeedle, ScaleMode.StretchToFill, true);
                }
                finally { GUI.matrix = matrix; GUI.color = old; }
            }
            Vector2 pivot = new Vector2(bounds.center.x, pivotY);
            float hubSize = Mathf.Max(28f, bounds.height * .31f);
            if (chulengoHub != null)
                GUI.DrawTexture(new Rect(pivot.x - hubSize * .5f, pivot.y - hubSize * .5f, hubSize, hubSize), chulengoHub, ScaleMode.StretchToFill, true);
            for (int i = 0; i < ChulengoLabels.Length; i++)
            {
                float position = (i + .5f) / ChulengoLabels.Length;
                float angle = Mathf.PI * (1f - position);
                float outerX = bounds.width * (554f / 1152f), outerY = bounds.height * (326f / 384f);
                float rayLength = .84f / Mathf.Sqrt(Mathf.Cos(angle) * Mathf.Cos(angle) / (outerX * outerX)
                    + Mathf.Sin(angle) * Mathf.Sin(angle) / (outerY * outerY));
                Vector2 label = new Vector2(pivot.x + rayLength * Mathf.Cos(angle), pivot.y - rayLength * Mathf.Sin(angle));
                VictoryText(new Rect(label.x - 36f, label.y - 21f, 72f, 42f), ChulengoLabels[i], labelFontSize, new Color(1f, .97f, .86f), TextAnchor.MiddleCenter);
            }
        }
        private Rect VictoryExitBounds() => VictorySlot(VictoryPopupBounds(logicalCanvasHeight, game != null && game.ShowChulengoResult), VictoryExit);
        private static string FormatVictoryTime(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        private static int FitVictoryFont(GUIStyle style, string value, int preferred, Rect bounds) =>
            FitVictoryFontWithMinimum(style, value, preferred, bounds, 12);

        private static int FitVictoryFontWithMinimum(GUIStyle style, string value, int preferred, Rect bounds, int minimum)
        {
            int original = style.fontSize;
            style.fontSize = Mathf.Max(1, preferred);
            Vector2 needed = style.CalcSize(new GUIContent(value));
            style.fontSize = original;
            float scale = Mathf.Min(1f, Mathf.Max(1f, bounds.width - (minimum < 12 ? 4f : 8f)) / Mathf.Max(1f, needed.x),
                Mathf.Max(1f, bounds.height - (minimum < 12 ? 2f : 6f)) / Mathf.Max(1f, needed.y));
            return Mathf.Max(minimum, Mathf.FloorToInt(preferred * scale));
        }
        private void VictoryText(Rect bounds, string value, int preferred, Color fill, TextAnchor alignment) =>
            VictoryText(bounds, value, preferred, fill, alignment, new Color(.08f, .035f, .018f, .98f));

        private void VictoryText(Rect bounds, string value, int preferred, Color fill, TextAnchor alignment, Color outline, int minimum = 12)
        {
            // Keep popup/cover styles isolated and avoid inherited white hover on static summary labels.
            int fontSize = FitVictoryFontWithMinimum(menuTitle, value, preferred, bounds, minimum);
            var captionStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize, fill);
            captionStyle.alignment = alignment;
            var borderStyle = CreateStandardButtonLabelStyle(captionStyle, fontSize, outline);
            float offset = preferred >= 32 ? 2f : .65f;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x * offset, bounds.y + y * offset, bounds.width, bounds.height), value, borderStyle);
            Label(bounds, value, captionStyle);
        }

        private static string FormatChulengoMoney(long cents)
        {
            long whole = cents / 100L;
            string grouped = whole.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
            string amount = "$" + grouped;
            long remainder = cents % 100L;
            return remainder == 0L ? amount : amount + "," + remainder.ToString("00");
        }

        private static Rect ChulengoTitleLineBoundsForFrame(Rect frame, bool secondLine) =>
            VictorySlot(frame, secondLine ? ChulengoTitleSecondLineSlot : ChulengoTitleFirstLineSlot);

        private static Rect ChulengoPrizeAmountBoundsForFrame(Rect frame) => VictorySlot(frame, ChulengoPrizeAmountSlot);
        private static Rect ChulengoPrizeCaptionBounds(Rect frame, bool compact) => VictorySlot(frame,
            // The compact caption is above the coin illustration, not under its top-left edge.
            compact ? new Rect(.385f, .381f, .435f, .061f) : ChulengoPrizeLabelSlot);
        private static Rect ChulengoTicketCardBoundsForFrame(Rect frame) => VictorySlot(frame, ChulengoTicketCardSlot);
        private static Rect ChulengoTicketCaptionBoundsForFrame(Rect frame) => VictorySlot(frame, ChulengoTicketCaptionSlot);

        private void DrawChulengoTitleRay(Rect frame, Vector2 normalizedCenter, float angle, Color color)
        {
            float width = frame.width * .034f;
            float height = frame.width * .008f;
            Vector2 center = new Vector2(frame.x + normalizedCenter.x * frame.width,
                frame.y + normalizedCenter.y * frame.height);
            Rect ray = new Rect(center.x - width * .5f, center.y - height * .5f, width, height);
            Matrix4x4 previousMatrix = GUI.matrix;
            Vector3 pivot = new Vector3(center.x, center.y, 0f);
            try
            {
                GUI.matrix = previousMatrix * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle)) *
                    Matrix4x4.Translate(-pivot);
                FillRect(ray, color);
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void DrawChulengoTitle(Rect frame, float fontScale)
        {
            Color accent = new Color(1f, .76f, .08f);
            // Small burst marks echo the selected comic lettering without touching the crest or iron plates.
            DrawChulengoTitleRay(frame, new Vector2(.105f, .232f), -24f, accent);
            DrawChulengoTitleRay(frame, new Vector2(.135f, .205f), -10f, accent);
            DrawChulengoTitleRay(frame, new Vector2(.105f, .264f), 16f, accent);
            DrawChulengoTitleRay(frame, new Vector2(.895f, .232f), 24f, accent);
            DrawChulengoTitleRay(frame, new Vector2(.865f, .205f), 10f, accent);
            DrawChulengoTitleRay(frame, new Vector2(.895f, .264f), -16f, accent);

            VictoryText(ChulengoTitleLineBoundsForFrame(frame, false), ChulengoVictoryTitleFirstLine,
                Mathf.RoundToInt(75f * fontScale), Color.white, TextAnchor.MiddleCenter, ChulengoTitleOutline);
            VictoryText(ChulengoTitleLineBoundsForFrame(frame, true), ChulengoVictoryTitleSecondLine,
                Mathf.RoundToInt(70f * fontScale), new Color(1f, .78f, .12f), TextAnchor.MiddleCenter, ChulengoTitleOutline);
        }

        private void DrawChulengoTicketBadge(Rect frame, float fontScale)
        {
            // The reference sticker is a tilted, die-cut yellow coupon rather than a flat icon tile.
            DrawChulengoTitleRay(frame, new Vector2(.969f, .379f), 44f, new Color(1f, .76f, .08f));
            DrawChulengoTitleRay(frame, new Vector2(.984f, .401f), 8f, new Color(1f, .76f, .08f));
            DrawChulengoTitleRay(frame, new Vector2(.949f, .365f), 66f, new Color(1f, .76f, .08f));

            Rect card = ChulengoTicketCardBoundsForFrame(frame);
            Vector2 pivot = card.center;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                GUI.matrix = previousMatrix * Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0f)) *
                    Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -14f)) *
                    Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0f));
                if (chulengoTicketTexture != null)
                {
                    Color shadow = new Color(.025f, .045f, .12f, .48f);
                    GUI.color = shadow;
                    GUI.DrawTexture(LayoutRect(new Rect(card.x + 2f, card.y + 3f, card.width, card.height)),
                        chulengoTicketTexture, ScaleMode.StretchToFill, true);
                    GUI.color = Color.white;
                    GUI.DrawTexture(LayoutRect(card), chulengoTicketTexture, ScaleMode.StretchToFill, true);
                }

                Rect caption = ChulengoTicketCaptionBoundsForFrame(frame);
                VictoryText(caption, ChulengoTicketCaption, Mathf.RoundToInt(33f * fontScale), Color.white,
                    TextAnchor.MiddleCenter, ChulengoTitleOutline);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }

        private void DrawChulengoPaper(Rect frame)
        {
            // Keep the original bolt plates visible while expanding the authored parchment into the reference's reward-card hierarchy.
            // The legacy popup has a baked green CTA at the bottom; extend the paper behind the live replacement action to hide its exposed edge.
            Rect paper = VictorySlot(frame, new Rect(.07f, .31f, .86f, .674f));
            if (chulengoParchment != null) GUI.DrawTexture(paper, chulengoParchment, ScaleMode.StretchToFill, true);
            else FillRect(paper, new Color(.98f, .89f, .68f));
        }

        private void DrawChulengoHero(Rect frame, bool pending, bool lockedRetry, int sector)
        {
            Color cream = new Color(1f, .94f, .78f);
            float fontScale = frame.width / 516f;
            VictoryText(VictorySlot(frame, new Rect(.39f, .315f, .54f, .086f)), "CHULENGO",
                Mathf.RoundToInt(82f * fontScale), cream, TextAnchor.MiddleCenter);

            Rect cook = VictorySlot(frame, new Rect(.075f, .345f, .31f, .255f));
            if (parrilleroIcon != null) GUI.DrawTexture(cook, parrilleroIcon, ScaleMode.ScaleToFit, true);

            string label;
            long reward;
            if (pending)
            {
                label = "PREMIO DEL TURNO";
                reward = game.ChulengoBaseRewardCents;
            }
            else if (lockedRetry && sector >= 0)
            {
                label = "RESULTADO FIJO: " + ChulengoLabels[Mathf.Clamp(sector, 0, 6)];
                reward = game.ChulengoFinalRewardCents;
            }
            else if (lockedRetry)
            {
                label = "COBRO PENDIENTE";
                reward = game.ChulengoBaseRewardCents;
            }
            else if (sector >= 0)
            {
                label = "CHULENGO " + ChulengoLabels[Mathf.Clamp(sector, 0, 6)];
                reward = game.ChulengoFinalRewardCents;
            }
            else
            {
                label = "PREMIO COBRADO";
                reward = game.ChulengoFinalRewardCents;
            }

            Color resultColor = sector == 0 || sector == 6 ? new Color(.75f, .075f, .035f) : cream;
            if (pending || lockedRetry && sector < 0) resultColor = cream;
            int preferredLabelSize = Mathf.RoundToInt(20f * fontScale);
            bool compactLabel = logicalCanvasHeight < 958f && preferredLabelSize < 12;
            VictoryText(ChulengoPrizeCaptionBounds(frame, compactLabel), label,
                preferredLabelSize, resultColor, TextAnchor.MiddleCenter,
                new Color(.08f, .035f, .018f, .98f), compactLabel ? 1 : 12);
            if (gameCoin != null)
            {
                GUI.DrawTexture(VictorySlot(frame, new Rect(.385f, .435f, .075f, .075f)), gameCoin, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(VictorySlot(frame, new Rect(.405f, .415f, .085f, .088f)), gameCoin, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(VictorySlot(frame, new Rect(.425f, .445f, .075f, .077f)), gameCoin, ScaleMode.ScaleToFit, true);
            }
            VictoryText(ChulengoPrizeAmountBoundsForFrame(frame), FormatChulengoMoney(reward),
                Mathf.RoundToInt(80f * fontScale), sector == 0 || sector == 6 ? new Color(.75f, .075f, .035f) : new Color(1f, .69f, .14f), TextAnchor.MiddleCenter);
        }

        private void DrawChulengoFooter(Rect frame, bool pending, bool lockedRetry, int sector)
        {
            float fontScale = frame.width / 516f;
            Color brown = new Color(.20f, .055f, .02f);
            if (pending) return;
            // The pending claim stays uncluttered: prize, wheel and actions communicate the choice.
            if (lockedRetry)
            {
                VictoryText(VictorySlot(frame, new Rect(.15f, .75f, .70f, .043f)),
                    sector < 0 ? "EL COBRO SE REINTENTA SIN PERDER EL PREMIO" : "RESULTADO GUARDADO · NO SE VUELVE A MULTIPLICAR",
                    Mathf.RoundToInt(13f * fontScale), new Color(.68f, .10f, .035f), TextAnchor.MiddleCenter);
            }
            else
            {
                VictoryText(VictorySlot(frame, new Rect(.15f, .745f, .70f, .040f)),
                    "PREMIO FINAL: " + FormatMarketMoney(game.ChulengoFinalRewardCents),
                    Mathf.RoundToInt(19f * fontScale), brown, TextAnchor.MiddleCenter);
                if (game.TicketRewardAppliedForCurrentVictory)
                    VictoryText(VictorySlot(frame, new Rect(.10f, .790f, .80f, .035f)),
                        "¡GANASTE 1 TICKET! · SALDO: " + game.TicketBalance,
                        Mathf.RoundToInt(17f * fontScale), new Color(.08f, .42f, .17f), TextAnchor.MiddleCenter);
            }
        }
        private static readonly string[] ProductShortNames = { "CHO", "PAT", "BON", "VAC", "COC", "FER", "CER" };

        private static string FormatQuotaSummary(StreetSimulation sim, bool pendingOnly)
        {
            if (StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex))
                return pendingOnly ? "UN HINCHA LLEGÓ A LA SALIDA"
                    : TowerDefenseLevelConfig.TotalFanCountForCampaignLevel(sim.LevelIndex) + " HINCHAS ATENDIDOS";
            var entries = new System.Collections.Generic.List<string>(sim.ProductCount);
            for (int slot = 0; slot < sim.ProductCount; slot++)
            {
                int product = sim.GetAvailableProduct(slot);
                int remaining = sim.GetProductRemaining(product);
                if (pendingOnly)
                {
                    if (remaining > 0) entries.Add(ProductShortNames[product] + " " + remaining);
                }
                else entries.Add(ProductShortNames[product] + " " + sim.GetProductDelivered(product) + "/" + sim.GetProductGoal(product));
            }
            if (entries.Count == 0) return pendingOnly ? "Todas las metas completas" : "—";
            var lines = new System.Collections.Generic.List<string>(2);
            for (int i = 0; i < entries.Count; i += 4)
                lines.Add(string.Join("  ·  ", entries.GetRange(i, Mathf.Min(4, entries.Count - i)).ToArray()));
            return string.Join("\n", lines.ToArray());
        }

        private void ResultPanel()
        {
            if (game.IsBonusTrack)
            {
                DrawBonusTrackResultPanel();
                return;
            }
            // Physical-screen dimmer covers the HUD and safe-area insets too.
            DrawIntroScreen(Texture2D.whiteTexture, new Color(0f, 0f, 0f, .63f), ScaleMode.StretchToFill);
            // Keep the status HUD crisp above the dimmed playfield, as in the in-game reference.
            if (game.ShowChulengoResult) DrawCounters(game.Sim);
            Rect frame = VictoryPopupBounds(logicalCanvasHeight, game.ShowChulengoResult);
            float previousVertical = layoutVerticalScale;
            try
            {
                // Uniform popup coordinates: never stretch its artwork, type or hit area vertically.
                layoutVerticalScale = 1f;
                Texture2D popup = game.ShowChulengoResult ? ClubColoredVictoryPopup(game.Sim.LevelIndex) : victoryPopup;
                if (popup != null) GUI.DrawTexture(frame, popup, ScaleMode.StretchToFill, true);
                else
                {
                    FillRect(frame, new Color(.19f, .07f, .025f));
                    FillRect(VictorySlot(frame, new Rect(.07f, .35f, .86f, .47f)), new Color(1f, .91f, .71f));
                    FillRect(VictorySlot(frame, VictoryExit), new Color(.015f, .06f, .18f));
                }
                float fontScale = frame.width / 516f;
                Color gold = new Color(1f, .82f, .22f), brown = new Color(.18f, .055f, .018f);

                bool victory = game.Sim.Phase == RoundPhase.Won;
                if (game.ShowChulengoResult)
                {
                    bool lockedRetry = game.HasPendingChulengo && game.ChulengoChoiceLocked;
                    bool pending = game.HasPendingChulengo && !lockedRetry;
                    int sector = game.ChulengoSelectedSector;
                    DrawChulengoPaper(frame);
                    DrawChulengoTitle(frame, fontScale);
                    DrawChulengoHero(frame, pending, lockedRetry, sector);
                    DrawChulengoMeter(VictorySlot(frame, new Rect(.0625f, .50f, .875f, .245f)),
                        !pending, sector, Mathf.RoundToInt(29f * fontScale));
                    DrawChulengoTicketBadge(frame, fontScale);
                    DrawChulengoFooter(frame, pending, lockedRetry, sector);
                    if (pending)
                    {
                        DrawStandardButton(ChulengoCollectBounds(), ChulengoCollectAction,
                            "COBRAR " + FormatChulengoMoney(game.ChulengoBaseRewardCents),
                            true, null, null, null, chulengoCollectButton, ChulengoPendingButtonTextColor);
                        DrawStandardButton(ChulengoMultiplyBounds(), ChulengoMultiplyAction, "MULTIPLICAR",
                            true, null, null, null, chulengoMultiplyButton, ChulengoPendingButtonTextColor);
                    }
                    else DrawStandardButton(ChulengoCollectBounds(), ChulengoCollectAction,
                        "COBRAR " + FormatChulengoMoney(game.ChulengoFinalRewardCents), true,
                        null, null, null, chulengoCollectButton, ChulengoPendingButtonTextColor);
                    return;
                }
                VictoryText(VictorySlot(frame, new Rect(.095f, .149f, .81f, .189f)),
                    StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex) ? "¡DEFENSA\nCOMPLETADA!" : "¡TURNO\nCOMPLETADO!", Mathf.RoundToInt(50f * fontScale), gold, TextAnchor.MiddleCenter);
                VictoryText(VictorySlot(frame, new Rect(.10f, .345f, .80f, .055f)),
                    StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex) ? "PEDIDOS COMPLETOS" : victory ? "VENTAS COMPLETADAS" : "FALTAN", Mathf.RoundToInt(23f * fontScale), brown, TextAnchor.MiddleCenter);
                string sales = FormatQuotaSummary(game.Sim, !victory);
                VictoryText(VictorySlot(frame, new Rect(.09f, .398f, .82f, .15f)), sales,
                    Mathf.RoundToInt(17f * fontScale), gold, TextAnchor.MiddleCenter);
                DrawMarketReceipt(VictorySlot(frame, new Rect(.10f, .55f, .80f, .27f)));
                bool canAdvance=game.CanAdvanceAfterResult;
                DrawStandardButton(VictorySlot(frame, VictoryExit), VictoryExitAction, canAdvance?"SIGUIENTE PARTIDO":"ÚLTIMO NIVEL", canAdvance);
                DrawStandardButton(ResultLevelsBounds(), ResultLevelsAction, "NIVELES");
            }
            finally { layoutVerticalScale = previousVertical; }
        }
        private void DrawMarketReceipt(Rect bounds)
        {
            StreetMarketResult r=game.Market!=null?game.Market.LastResult:null;
            if(r==null)return;
            string line1="VENTAS "+FormatMarketMoney(r.grossSalesCents)+" → 50% "+FormatMarketMoney(r.halfSalesCents);
            string line2=r.won?"BONO SOBRANTE: 2 × "+FormatMarketMoney(r.leftoverCents)+" = "+FormatMarketMoney(r.bonusCents):"DERROTA: sin bono por sobrante";
            string line3="VENTAS RECONOCIDAS: "+FormatMarketMoney(r.recognizedSalesCents);
            string line4="REPOSICIÓN: "+FormatMarketMoney(r.progressionBonusCents);
            string line5="INGRESO NUEVO: "+FormatMarketMoney(r.earnedCents);
            string line6="SALDO ACTUAL: "+FormatMarketMoney(r.totalBalanceCents);
            string line7=game.TicketRewardAppliedForCurrentVictory?"¡GANASTE 1 TICKET! · SALDO: "+game.TicketBalance:"";
            string[] lines={line1,line2,line4,line3,line5,line6,line7};
            int oldSize=small.fontSize;
            TextAnchor oldAlignment=small.alignment;
            bool oldWordWrap=small.wordWrap;
            float row=bounds.height/lines.Length;
            Color oldColor=small.normal.textColor;
            small.alignment=TextAnchor.MiddleLeft;
            small.wordWrap=false;
            float textX=bounds.x+bounds.width*.22f;
            float textWidth=bounds.width*.76f;
            for(int i=0;i<lines.Length;i++)
            {
                Rect lineBounds=new Rect(textX,bounds.y+row*i,textWidth,row);
                small.fontSize=13;
                while(small.fontSize>10 && small.CalcSize(new GUIContent(lines[i])).x>lineBounds.width) small.fontSize--;
                small.normal.textColor = i == 6 && !string.IsNullOrEmpty(lines[i]) ? new Color(.08f,.42f,.17f) : new Color(.24f,.10f,.035f);
                Label(lineBounds,lines[i],small);
            }
            small.normal.textColor=oldColor;
            small.alignment=oldAlignment;
            small.wordWrap=oldWordWrap;
            small.fontSize=oldSize;
        }
    }
}
