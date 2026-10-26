using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private static readonly Color SelectorInk = new Color(.0015f, .006f, .023f);
        private readonly System.Collections.Generic.Dictionary<int, Texture2D> selectorCardSurfaces =
            new System.Collections.Generic.Dictionary<int, Texture2D>();
        private Texture2D selectorAvailableBadge, selectorLockedBadge, selectorHeadingRay;
        private GUIStyle selectorText;
        private Font selectorFont;

        private static Rect LevelSelectorViewportBounds(float canvasHeight)
        {
            Rect rect = LevelSelectorViewport;
            rect.height = Mathf.Max(150f, canvasHeight - rect.y - 100f);
            return rect;
        }
        private static Rect LevelSelectorBackBounds(float canvasHeight)
        {
            Rect rect = LevelSelectBack;
            rect.y = canvasHeight - 90f;
            return rect;
        }
        private int HitLevelSelectorAction(Vector2 point)
        {
            if (MarketSelectorBounds().Contains(point)) return MarketCloseAction;
            if (LevelSelectorBackBounds(logicalCanvasHeight).Contains(point)) return LevelSelectorBackAction;
            if (!LevelSelectorViewportBounds(logicalCanvasHeight).Contains(point)) return 0;
            for (int i = 0; i < StreetSimulation.LevelNames.Length; i++)
            {
                Rect card = LevelSelectCardBounds(i);
                card.y -= levelSelectScroll;
                if (i <= game.UnlockedLevel && card.Contains(point)) return LevelSelectFirstAction + i;
            }
            return 0;
        }

        private void DisposeLevelSelectorPresentation()
        {
            foreach (Texture2D texture in selectorCardSurfaces.Values)
                if (texture != null) Destroy(texture);
            selectorCardSurfaces.Clear();
            if (selectorAvailableBadge != null) Destroy(selectorAvailableBadge);
            if (selectorLockedBadge != null) Destroy(selectorLockedBadge);
            if (selectorHeadingRay != null) Destroy(selectorHeadingRay);
            selectorAvailableBadge = selectorLockedBadge = selectorHeadingRay = null;
            selectorText = null;
        }

        // A selector is not a frozen gameplay screen: it has its own continuous original street,
        // with no counter, kitchen footer, blue clear color, or pennants behind the cards.
        private void DrawLevelSelectorBackdrop()
        {
            if (muralArt == null) return;
            Matrix4x4 matrix = GUI.matrix;
            Color color = GUI.color;
            try
            {
                float scale = Screen.width / W;
                float height = Screen.height / scale;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                GUI.color = Color.white;
                Rect pavement = new Rect(0, 744, 840, 452);
                float tileHeight = pavement.height * W / pavement.width;
                for (float y = 0; y < height; y += tileHeight)
                    DrawSelectorRaw(new Rect(0, y, W, tileHeight), muralArt, pavement);
                DrawSelectorRaw(new Rect(0, 0, W, 161f), muralArt,
                    new Rect(0, 0, muralArt.width, 280f));
                GUI.color = new Color(.08f, .055f, .035f, .08f);
                GUI.DrawTexture(new Rect(0, 0, W, height), Texture2D.whiteTexture);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }

        private static void DrawSelectorRaw(Rect destination, Texture2D texture, Rect source)
        {
            GUI.DrawTextureWithTexCoords(destination, texture,
                new Rect(source.x / texture.width, 1f - source.yMax / texture.height,
                    source.width / texture.width, source.height / texture.height), true);
        }

        private void SelectorLabel(Rect bounds, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            if (selectorFont == null) selectorFont = Resources.Load<Font>("Google/GoogleSans-Medium");
            if (selectorText == null)
                selectorText = new GUIStyle(GUI.skin.label) {
                    font = selectorFont, fontStyle = FontStyle.Bold, wordWrap = false,
                    padding = new RectOffset(), margin = new RectOffset() };
            selectorText.fontSize = size;
            selectorText.alignment = alignment;
            selectorText.normal.textColor = color;
            float measured = selectorText.CalcSize(new GUIContent(value)).x;
            if (measured > bounds.width)
                selectorText.fontSize = Mathf.Max(10, Mathf.FloorToInt(size * bounds.width / measured));
            GUI.Label(LayoutRect(bounds), value, selectorText);
        }

        private void DrawLevelSelectorHeading()
        {
            int prior = menuTitle.fontSize;
            menuTitle.fontSize = 48;
            OutlineLabel(new Rect(LevelSelectTitle.x + 1f, LevelSelectTitle.y + 3f,
                LevelSelectTitle.width, LevelSelectTitle.height), "ELEGÍ TU CANCHA",
                menuTitle, new Color(.21f, .12f, .045f), 3f);
            OutlineLabel(LevelSelectTitle, "ELEGÍ TU CANCHA", menuTitle,
                new Color(1f, .97f, .86f), 2.5f);
            menuTitle.fontSize = prior;
            DrawSelectorHeadingAccents();
        }

        private void DrawSelectorHeadingAccents()
        {
            if (selectorHeadingRay == null)
            {
                const int width=880, height=140;
                var pixels = new Color[width*height];
                for (int y=0;y<height;y++)
                for (int x=0;x<width;x++)
                {
                    Vector2 point=new Vector2((x+.5f)*.5f,(height-y-.5f)*.5f);
                    float d=Mathf.Min(
                        Mathf.Min(SelectorSegmentDistance(point,new Vector2(12,16),new Vector2(20,24)),
                            SelectorSegmentDistance(point,new Vector2(9,45),new Vector2(20,47))),
                        Mathf.Min(SelectorSegmentDistance(point,new Vector2(408,16),new Vector2(400,24)),
                            SelectorSegmentDistance(point,new Vector2(411,45),new Vector2(400,47))));
                    Color color=new Color(.15f,.075f,.025f,Mathf.Clamp01(4.5f-d));
                    pixels[y*width+x]=Over(color,new Color(.12f,.48f,.025f),Mathf.Clamp01(2.8f-d));
                }
                selectorHeadingRay=SelectorTexture(width,height,pixels,"Selector green heading accents");
            }
            GUI.DrawTexture(LayoutRect(new Rect(50,84,440,70)),selectorHeadingRay,ScaleMode.StretchToFill,true);
        }

        private void DrawLevelSelectorCardPresentation(int index, Rect baseBounds)
        {
            bool unlocked = index <= game.UnlockedLevel;
            Rect bounds = ScaleCardBounds(baseBounds, Mathf.Min(1f, layoutVerticalScale));
            int key = index * 2 + (unlocked ? 1 : 0);
            Texture2D surface;
            if (!selectorCardSurfaces.TryGetValue(key, out surface))
            {
                surface = CreateLevelSelectorCardSurface(index, unlocked);
                selectorCardSurfaces.Add(key, surface);
            }
            GUI.DrawTexture(LayoutRect(bounds), surface, ScaleMode.StretchToFill, true);
            float scale = bounds.width / 244f;
            Texture2D crest = LevelCrestForLevel(index);
            if (crest != null)
                DrawLevelCrest(LayoutRect(new Rect(bounds.x + 11f * scale, bounds.y + 10f * scale,
                    30f * scale, 34f * scale)), crest);

            Rect caption = new Rect(bounds.x + 24f * scale, bounds.y + 129f * scale,
                bounds.width - 34f * scale, 24f * scale);
            SelectorLabel(caption, "NIVEL " + (index + 1), Mathf.RoundToInt(22f * scale), SelectorInk);
            string name = ClubVisualTheme.ForLevel(index).ClubName.ToUpperInvariant();
            if (index == 9) name = name.Split('/')[0].Trim();
            SelectorLabel(new Rect(caption.x, bounds.y + 155f * scale, caption.width, 20f * scale),
                name, Mathf.RoundToInt(15f * scale), SelectorInk);

            float badgeWidth = unlocked ? 102f : 114f;
            Rect badge = new Rect(bounds.xMax - (badgeWidth + 9f) * scale,
                bounds.y + 8f * scale, badgeWidth * scale, 27f * scale);
            Texture2D badgeSurface = unlocked ? selectorAvailableBadge : selectorLockedBadge;
            if (badgeSurface == null)
            {
                badgeSurface = CreateLevelSelectorStatusSurface(unlocked);
                if (unlocked) selectorAvailableBadge = badgeSurface; else selectorLockedBadge = badgeSurface;
            }
            GUI.DrawTexture(LayoutRect(badge), badgeSurface, ScaleMode.StretchToFill, true);
            if (!unlocked)
                Item(new Rect(badge.x + 6f * scale, badge.y + 4f * scale, 16f * scale, 19f * scale), 17);
            SelectorLabel(new Rect(badge.x + (unlocked ? 7f : 26f) * scale, badge.y,
                (unlocked ? 76f : 82f) * scale, badge.height),
                unlocked ? "DISPONIBLE" : "BLOQUEADO", Mathf.RoundToInt(13f * scale),
                Color.white, TextAnchor.MiddleCenter);

            if (unlocked && GUI.Button(LayoutRect(bounds), "", invisible))
                NativeAction(LevelSelectFirstAction + index);
        }

        private Texture2D CreateLevelSelectorCardSurface(int index, bool unlocked)
        {
            const int width = 488, height = 368;
            Texture2D artwork;
            Rect source;
            if (index == 0) { artwork = muralArt; source = AllBoysSelectorMuralSource; }
            else if (index == 1) { artwork = chicagoBackground; source = ChicagoSelectorMuralSource; }
            else if (index >= 2 && index <= 4)
            {
                string resource = index == 2 ? VelezBackgroundResource : index == 3 ? FerroBackgroundResource : IndependienteBackgroundResource;
                artwork = Resources.Load<Texture2D>(resource);
                float top = index == 2 ? 48f : index == 3 ? 215f : 120f;
                source = new Rect(0, top, artwork.width, artwork.width * 122f / 236f);
            }
            else
            {
                ClubVisualTheme theme = ClubVisualTheme.ForLevel(index);
                artwork = Resources.Load<Texture2D>(theme.MuralResource);
                Rect uv = theme.MuralSource;
                source = new Rect(uv.x * artwork.width, uv.y * artwork.height,
                    uv.width * artwork.width, uv.height * artwork.height);
            }
            // Crop a real source region to fill the thumbnail, without stretching edge pixels.
            float aspect = 236f / 122f;
            if (source.width / source.height > aspect)
            {
                float cropWidth = source.height * aspect;
                source = new Rect(source.center.x - cropWidth * .5f, source.y, cropWidth, source.height);
            }
            else
            {
                float cropHeight = source.width / aspect;
                source = new Rect(source.x, source.center.y - cropHeight * .5f, source.width, cropHeight);
            }
            Texture2D readable = null;
            Color32[] artPixels = null;
            if (artwork != null)
            {
                if (artwork.isReadable) artPixels = artwork.GetPixels32();
                else
                {
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture temporary = RenderTexture.GetTemporary(artwork.width, artwork.height, 0, RenderTextureFormat.ARGB32);
                    try
                    {
                        Graphics.Blit(artwork, temporary);
                        RenderTexture.active = temporary;
                        readable = new Texture2D(artwork.width, artwork.height, TextureFormat.RGBA32, false);
                        readable.ReadPixels(new Rect(0, 0, artwork.width, artwork.height), 0, 0);
                        readable.Apply();
                        artPixels = readable.GetPixels32();
                    }
                    finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary); }
                }
            }
            var pixels = new Color[width * height];
            Color accent = ClubVisualTheme.ForLevel(index).PrimaryColor;
            if (accent.grayscale > .84f) accent = ClubVisualTheme.ForLevel(index).SecondaryColor;
            Rect outer = new Rect(0, 0, 244, 180), inner = new Rect(2, 2, 240, 176);
            Rect face = new Rect(3.5f, 3.5f, 237, 173);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * .5f, py = (height - y - .5f) * .5f;
                Color c = new Color(.12f, .075f, .025f,
                    .32f * RoundedCoverage(px, py, new Rect(0, 4, 244, 180), 13f));
                c = Over(c, new Color(.84f, .71f, .41f), RoundedCoverage(px, py, outer, 12f));
                c = Over(c, new Color(.48f, .43f, .31f), RoundedCoverage(px, py, inner, 10f));
                Color cream = Color.Lerp(new Color(1f, .975f, .88f), new Color(.98f, .94f, .81f),
                    Mathf.Clamp01((py - 126f) / 54f));
                c = Over(c, cream, RoundedCoverage(px, py, face, 9f));
                float mask = RoundedCoverage(px, py, face, 9f);
                if (py < 126f && artPixels != null)
                {
                    float sx = source.x + Mathf.Clamp01((px - 4f) / 236f) * source.width;
                    float sy = source.y + Mathf.Clamp01((py - 4f) / 122f) * source.height;
                    Color sample = SelectorSample(artPixels, artwork.width, artwork.height,
                        sx, artwork.height - 1f - sy);
                    if (!unlocked) sample = Color.Lerp(sample, new Color(.19f, .24f, .31f), .14f);
                    c = Over(c, sample, mask);
                }
                else if (py >= 126f)
                {
                    if (!unlocked) c = Over(c, new Color(.72f, .75f, .78f), mask * .55f);
                    if (px < 14f) c = Over(c, accent,
                        mask * RoundedCoverage(px, py, new Rect(4, 126, 10, 51), 4f));
                }
                pixels[y * width + x] = c;
            }
            if (readable != null) Destroy(readable);
            return SelectorTexture(width, height, pixels, "Original mural rounded selector card");
        }

        private static Color SelectorSample(Color32[] pixels, int width, int height, float x, float y)
        {
            x = Mathf.Clamp(x, 0, width - 1); y = Mathf.Clamp(y, 0, height - 1);
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, width - 1), y1 = Mathf.Min(y0 + 1, height - 1);
            return Color.Lerp(Color.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], x - x0),
                Color.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], x - x0), y - y0);
        }

        private static Texture2D CreateLevelSelectorStatusSurface(bool unlocked)
        {
            int width = unlocked ? 204 : 228;
            const int height = 54;
            float w = width * .5f;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * .5f, py = (height - y - .5f) * .5f;
                Color c = new Color(.12f, .20f, .11f, .3f * RoundedCoverage(px, py, new Rect(0, 2, w, 25), 9));
                c = Over(c, new Color(.98f, 1f, .91f), RoundedCoverage(px, py, new Rect(0, 0, w, 25), 9));
                Color fill = unlocked
                    ? Color.Lerp(new Color(.01f, .44f, .015f), new Color(.0f, .25f, .005f), py / 25f)
                    : Color.Lerp(new Color(.31f, .40f, .49f), new Color(.15f, .22f, .30f), py / 25f);
                c = Over(c, fill, RoundedCoverage(px, py, new Rect(2, 2, w - 4, 21), 7));
                if (unlocked)
                {
                    Vector2 point = new Vector2(px, py);
                    float circle = Mathf.Clamp01(8.5f - Vector2.Distance(point, new Vector2(w - 14f, 12.5f)));
                    c = Over(c, Color.white, circle);
                    float d = Mathf.Min(SelectorSegmentDistance(point, new Vector2(w - 18, 12), new Vector2(w - 15, 15)),
                        SelectorSegmentDistance(point, new Vector2(w - 15, 15), new Vector2(w - 10, 9)));
                    c = Over(c, new Color(.02f, .49f, .07f), circle * Mathf.Clamp01(2f - d));
                }
                pixels[y * width + x] = c;
            }
            return SelectorTexture(width, height, pixels, "Rounded selector status badge");
        }

        private static float SelectorSegmentDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 line = to - from;
            return Vector2.Distance(point, from + line * Mathf.Clamp01(Vector2.Dot(point - from, line) / line.sqrMagnitude));
        }

        private static Texture2D SelectorTexture(int width, int height, Color[] pixels, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
    }
}
