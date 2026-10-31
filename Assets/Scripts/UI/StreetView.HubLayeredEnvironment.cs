using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const string HubLayeredResourceRoot = "StreetHubEnvironment/";
        private const int HubLayeredCanvasWidth = 841;
        private const int HubLayeredCanvasHeight = 1870;
        private HubLayeredEnvironmentRenderer hubLayeredEnvironmentRenderer;
#if UNITY_EDITOR
        private bool bypassHubLayeredEnvironmentForTesting;
#endif

        private void DrawHubLayeredEnvironment()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
#if UNITY_EDITOR
            if (bypassHubLayeredEnvironmentForTesting) return;
#endif
            if (hubLayeredEnvironmentRenderer == null) hubLayeredEnvironmentRenderer = new HubLayeredEnvironmentRenderer(this);
            hubLayeredEnvironmentRenderer.Draw();
        }

        partial void ReleaseHubLayeredEnvironmentResources();

        partial void ReleaseHubLayeredEnvironmentResources()
        {
            if (hubLayeredEnvironmentRenderer != null) hubLayeredEnvironmentRenderer.Dispose();
            hubLayeredEnvironmentRenderer = null;
        }

        // Source-space bounds are centralized so generated layers can be adjusted without touching renderer logic.
        internal static readonly Rect[] HubLayeredFlagBounds = {
            new Rect(337, 404, 52, 46), new Rect(393, 343, 58, 70), new Rect(486, 302, 90, 86),
            new Rect(600, 293, 68, 64), new Rect(698, 297, 17, 40)
        };
        internal static readonly Rect[] HubLayeredShirtBounds = { new Rect(590, 850, 167, 156) };
        internal static readonly Rect[] HubLayeredAwningBounds = {
            new Rect(0, 514, 373, 136), new Rect(67, 1250, 371, 128)
        };
        // Normalized UV rectangles use top-origin source pixel bounds, independent of target canvas coordinates.
        internal static readonly Rect[] HubLayeredFlagSourceUv = {
            SourceUv(222,468,288,554,841,1870), SourceUv(291,439,356,535,841,1870),
            SourceUv(359,411,439,519,841,1870), SourceUv(475,368,582,479,841,1870),
            SourceUv(626,369,730,481,841,1870)
        };
        internal static readonly Rect[] HubLayeredShirtSourceUv = { SourceUv(356,578,896,1130,928,1695) };
        internal static readonly Rect[] HubLayeredAwningSourceUv = {
            SourceUv(0,505,530,707,841,1870), SourceUv(62,1243,520,1406,841,1870)
        };

        private static Rect HubLayeredArtworkScreenRect(Rect art, Vector2 screenSize)
        {
            Rect backdrop = HubArtworkScreenBounds(HubArtworkSize, screenSize);
            float scale = backdrop.width / HubLayeredCanvasWidth;
            return new Rect(backdrop.x + art.x * scale, backdrop.y + art.y * scale,
                art.width * scale, art.height * scale);
        }

        private static Rect SourceUv(float x0, float y0, float x1, float y1, float width, float height)
            => new Rect(x0 / width, y0 / height, (x1 - x0) / width, (y1 - y0) / height);

        private sealed class HubLayeredEnvironmentRenderer
        {
            private const int ClothSegments = 12;
            private const float FlagAmplitude = 2.2f;
            private const float ShirtAmplitude = 1.2f;
            private const float AwningAmplitude = .75f;
            private readonly StreetView owner;
            private readonly float[] flagPhases = { 0f, 1.1f, 2.3f, 3.7f, 4.8f };
            private readonly float[] shirtPhases = { .7f };
            private readonly float[] awningPhases = { 1.6f, 3.1f };
            private Texture2D cleanPlate, flags, shirts, awnings, softDisc, birdSilhouette;
            private float elapsed;
            private int lastFrame = -1;
            private bool loadAttempted;
            private readonly Texture2D[] cleanPatches = new Texture2D[HubLayeredFlagBounds.Length + HubLayeredShirtBounds.Length + HubLayeredAwningBounds.Length];

            public HubLayeredEnvironmentRenderer(StreetView owner) { this.owner = owner; }

            public void Draw()
            {
                AdvanceClock();
                EnsureResources();
                Matrix4x4 oldMatrix = GUI.matrix;
                Color oldColor = GUI.color;
                try
                {
                    // Full-bleed artwork is in physical screen pixels, not fixed-height UI units.
                    // LayoutRect scales only y, leaving heights short on tall screens.
                    GUI.matrix = Matrix4x4.identity;
                    // Never expose a moved opaque cutout over the original: every replacement is gated by its clean plate.
                    if (cleanPlate != null)
                    {
                        DrawClothGroup(flags, HubLayeredFlagBounds, HubLayeredFlagSourceUv, flagPhases, FlagAmplitude, 0, false);
                        DrawClothGroup(shirts, HubLayeredShirtBounds, HubLayeredShirtSourceUv, shirtPhases, ShirtAmplitude, HubLayeredFlagBounds.Length, false);
                        DrawClothGroup(awnings, HubLayeredAwningBounds, HubLayeredAwningSourceUv, awningPhases, AwningAmplitude,
                            HubLayeredFlagBounds.Length + HubLayeredShirtBounds.Length, true);
                    }
                    DrawCloudShadows();
                    DrawDistantBirds();
                    DrawWarmShopPulse();
                }
                finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
            }

            private void AdvanceClock()
            {
                if (lastFrame == Time.frameCount) return;
                if (lastFrame >= 0) elapsed += Mathf.Clamp(Time.unscaledDeltaTime, 0f, .1f);
                lastFrame = Time.frameCount;
            }

            private void EnsureResources()
            {
                if (loadAttempted) return;
                loadAttempted = true;
                cleanPlate = Resources.Load<Texture2D>(HubLayeredResourceRoot + "hub-environment-clean-v1");
                flags = Resources.Load<Texture2D>(HubLayeredResourceRoot + "hub-environment-flags-v1");
                shirts = Resources.Load<Texture2D>(HubLayeredResourceRoot + "hub-environment-shirts-v1");
                awnings = Resources.Load<Texture2D>(HubLayeredResourceRoot + "hub-environment-awnings-v1");
                int patch = 0;
                for (int i = 0; i < HubLayeredFlagBounds.Length; i++) cleanPatches[patch++] = CreateFeatheredPatch(HubLayeredFlagBounds[i]);
                for (int i = 0; i < HubLayeredShirtBounds.Length; i++) cleanPatches[patch++] = CreateFeatheredPatch(HubLayeredShirtBounds[i]);
                for (int i = 0; i < HubLayeredAwningBounds.Length; i++) cleanPatches[patch++] = CreateFeatheredPatch(HubLayeredAwningBounds[i]);
                softDisc = CreateSoftDisc();
                birdSilhouette = CreateBirdSilhouette();
            }

            private void DrawClothGroup(Texture2D foreground, Rect[] bounds, Rect[] sourceUvs, float[] phases, float amplitude, int patchOffset, bool pinnedRoof)
            {
                if (foreground == null) return; // Missing layer disables only this group; untouched master remains visible.
                for (int i = 0; i < bounds.Length; i++)
                {
                    if (cleanPatches[patchOffset + i] == null) continue;
                    Rect art = bounds[i];
                    DrawCleanPatch(cleanPatches[patchOffset + i], art);
                    if (pinnedRoof) DrawPinnedAwning(foreground, art, sourceUvs[i], phases[i], amplitude);
                    else DrawClothSlices(foreground, art, sourceUvs[i], phases[i], amplitude);
                }
            }

            private void DrawPinnedAwning(Texture2D texture, Rect art, Rect sourceUv, float phase, float amplitude)
            {
                // Keep the canopy in one continuous draw to avoid sampling seams; a tiny pivoted sway anchors the roof corner.
                Rect layout = HubLayeredArtworkScreenRect(art, new Vector2(Screen.width, Screen.height));
                Matrix4x4 previous = GUI.matrix;
                Vector2 pinnedCorner = new Vector2(layout.xMin, layout.yMin);
                float angle = Mathf.Sin(elapsed * 1.7f + phase) * amplitude * .22f;
                GUI.matrix = Matrix4x4.TRS(pinnedCorner, Quaternion.Euler(0f, 0f, angle), Vector3.one) *
                             Matrix4x4.TRS(-pinnedCorner, Quaternion.identity, Vector3.one) * previous;
                DrawTextureRegion(texture, art, sourceUv, new Rect(0f, 0f, 1f, 1f));
                GUI.matrix = previous;
            }

            private void DrawClothSlices(Texture2D texture, Rect art, Rect sourceUv, float phase, float amplitude)
            {
                for (int s = 0; s < ClothSegments; s++)
                {
                    float x0 = s / (float)ClothSegments, x1 = (s + 1) / (float)ClothSegments;
                    float localFlex = Mathf.Sin(x0 * Mathf.PI) * Mathf.Sin(elapsed * 2.1f + phase + x0 * 5.2f);
                    float nextFlex = Mathf.Sin(x1 * Mathf.PI) * Mathf.Sin(elapsed * 2.1f + phase + x1 * 5.2f);
                    float y = (localFlex + nextFlex) * .5f * amplitude;
                    Rect piece = new Rect(art.x + art.width * x0, art.y + y, art.width / ClothSegments + .25f, art.height);
                    DrawTextureRegion(texture, piece, sourceUv, new Rect(x0, 0f, 1f / ClothSegments, 1f));
                }
            }

            // Aligned source patch with normalized source UV; dimensions of generated images are intentionally irrelevant.
            private void DrawAlignedPatch(Texture2D texture, Rect art, Rect normalizedPatch)
            {
                if (texture == null) return;
                Rect layout = HubLayeredArtworkScreenRect(art, new Vector2(Screen.width, Screen.height));
                if (layout.width <= 0f || layout.height <= 0f) return;
                Rect uv = new Rect(art.x / HubLayeredCanvasWidth + normalizedPatch.x * art.width / HubLayeredCanvasWidth,
                    1f - (art.y + art.height) / HubLayeredCanvasHeight + normalizedPatch.y * art.height / HubLayeredCanvasHeight,
                    normalizedPatch.width * art.width / HubLayeredCanvasWidth,
                    normalizedPatch.height * art.height / HubLayeredCanvasHeight);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(layout, texture, uv, true);
            }

            private void DrawTextureRegion(Texture2D texture, Rect destinationArt, Rect sourceUv, Rect normalizedPatch)
            {
                if (texture == null) return;
                Rect layout = HubLayeredArtworkScreenRect(destinationArt, new Vector2(Screen.width, Screen.height));
                if (layout.width <= 0f || layout.height <= 0f) return;
                Rect uv = HubLayeredNormalizedPatchToTextureUv(sourceUv, normalizedPatch);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(layout, texture, uv, true);
            }

            internal static Rect HubLayeredNormalizedPatchToTextureUv(Rect sourceUvTopOrigin, Rect normalizedPatch)
            {
                return new Rect(sourceUvTopOrigin.x + normalizedPatch.x * sourceUvTopOrigin.width,
                    1f - (sourceUvTopOrigin.y + (normalizedPatch.y + normalizedPatch.height) * sourceUvTopOrigin.height),
                    normalizedPatch.width * sourceUvTopOrigin.width,
                    normalizedPatch.height * sourceUvTopOrigin.height);
            }

            private void DrawAlignedPatch(Texture2D texture, Rect art) => DrawAlignedPatch(texture, art, new Rect(0f, 0f, 1f, 1f));

            private void DrawCloudShadows()
            {
                if (softDisc == null) return;
                float travel = Mathf.Repeat(elapsed * 4f, 500f);
                DrawSoftOval(new Rect(260f + travel, 990f, 120f, 24f), .06f);
                DrawSoftOval(new Rect(450f + Mathf.Repeat(travel + 170f, 300f), 1390f, 135f, 24f), .06f);
            }

            private void DrawDistantBirds()
            {
                if (birdSilhouette == null) return;
                float cycle = Mathf.Repeat(elapsed, 18f) / 18f;
                for (int i = 0; i < 3; i++)
                {
                    float t = Mathf.Repeat(cycle + i / 3f, 1f);
                    float x = Mathf.Lerp(-20f, 850f, t);
                    float y = 237f + i * 23f + Mathf.Sin(t * Mathf.PI * 2f) * 5f;
                    float wingHeight = 10f + Mathf.Sin(elapsed * 5f + i * 1.7f) * 2.5f;
                    Rect screen = HubLayeredArtworkScreenRect(new Rect(x - 13f, y - wingHeight * .5f, 26f, wingHeight),
                        new Vector2(Screen.width, Screen.height));
                    GUI.color = new Color(.17f, .22f, .29f, .72f);
                    GUI.DrawTexture(screen, birdSilhouette, ScaleMode.StretchToFill, true);
                }
            }

            // Code-native gull silhouette: curved tapered wings and body, cached once (not line dashes).
            private static Texture2D CreateBirdSilhouette()
            {
                const int width = 96, height = 40;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                { name = "Hub distant bird silhouette", filterMode = FilterMode.Bilinear,
                  wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    float u = (x + .5f) / width * 2f - 1f;
                    float v = (y + .5f) / height;
                    float wingCurve = .24f + .48f * Mathf.Sin(Mathf.Abs(u) * Mathf.PI * .82f);
                    float thickness = .025f + .065f * (1f - Mathf.Abs(u));
                    float wing = Mathf.Clamp01((thickness - Mathf.Abs(v - wingCurve)) * height);
                    float body = Mathf.Clamp01((1f - (u * u / .008f + (v - .24f) * (v - .24f) / .045f)) * 4f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Max(wing, body));
                }
                texture.SetPixels(pixels); texture.Apply(false, true); return texture;
            }

            private void DrawWarmShopPulse()
            {
                if (softDisc == null) return;
                float pulse = .05f + (Mathf.Sin(elapsed * .72f) + 1f) * .018f;
                DrawWarmGlow(new Rect(137f, 1370f, 19f, 25f), pulse);
                DrawWarmGlow(new Rect(174f, 1372f, 20f, 24f), pulse * .85f);
            }

            private void DrawWarmGlow(Rect art, float alpha)
            {
                Rect layout = HubLayeredArtworkScreenRect(art, new Vector2(Screen.width, Screen.height));
                GUI.color = new Color(1f, .56f, .15f, alpha);
                GUI.DrawTexture(layout, softDisc, ScaleMode.StretchToFill, true);
            }

            private void DrawSoftOval(Rect art, float alpha)
            {
                Rect layout = HubLayeredArtworkScreenRect(art, new Vector2(Screen.width, Screen.height));
                GUI.color = new Color(.22f, .25f, .31f, alpha);
                GUI.DrawTexture(layout, softDisc, ScaleMode.StretchToFill, true);
            }

            private static Texture2D CreateSoftDisc()
            {
                const int size = 32;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                { name = "Hub layered soft ambient disc", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float dx = (x - 15.5f) / 15.5f, dy = (y - 15.5f) / 15.5f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)));
                }
                tex.SetPixels(pixels); tex.Apply(false, true); return tex;
            }

            private Texture2D CreateFeatheredPatch(Rect bounds)
            {
                if (cleanPlate == null || !cleanPlate.isReadable) return null;
                const float expansion = 4f;
                float scaleX = cleanPlate.width / (float)HubLayeredCanvasWidth;
                float scaleY = cleanPlate.height / (float)HubLayeredCanvasHeight;
                int x = Mathf.Clamp(Mathf.FloorToInt((bounds.x - expansion) * scaleX), 0, cleanPlate.width - 1);
                int yTop = Mathf.Clamp(Mathf.FloorToInt((bounds.y - expansion) * scaleY), 0, cleanPlate.height - 1);
                int xEnd = Mathf.Clamp(Mathf.CeilToInt((bounds.xMax + expansion) * scaleX), x + 1, cleanPlate.width);
                int yEnd = Mathf.Clamp(Mathf.CeilToInt((bounds.yMax + expansion) * scaleY), yTop + 1, cleanPlate.height);
                int width = xEnd - x, height = yEnd - yTop;
                Rect expandedBounds = new Rect(x / scaleX, yTop / scaleY, width / scaleX, height / scaleY);
                Color[] pixels = cleanPlate.GetPixels(x, cleanPlate.height - yEnd, width, height);
                const int feather = 2;
                for (int py = 0; py < height; py++) for (int px = 0; px < width; px++)
                {
                    float edge = Mathf.Min(Mathf.Min(px, width - 1 - px), Mathf.Min(py, height - 1 - py));
                    int index = py * width + px;
                    Color color = pixels[index]; color.a *= Mathf.Clamp01(edge / feather); pixels[index] = color;
                }
                var patch = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                { name = "Hub layered clean feather patch", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                patch.SetPixels(pixels); patch.Apply(false, true);
                patch.name = "Hub layered clean feather patch " + Mathf.RoundToInt(expandedBounds.x) + "," + Mathf.RoundToInt(expandedBounds.y);
                return patch;
            }

            private void DrawCleanPatch(Texture2D patch, Rect art)
            {
                if (patch == null) return;
                Rect expandedArt = new Rect(art.x - 4f, art.y - 4f, art.width + 8f, art.height + 8f);
                Rect layout = HubLayeredArtworkScreenRect(expandedArt, new Vector2(Screen.width, Screen.height));
                GUI.color = Color.white;
                GUI.DrawTexture(layout, patch, ScaleMode.StretchToFill, true);
            }

            public void Dispose()
            {
                if (softDisc != null) Object.Destroy(softDisc);
                if (birdSilhouette != null) Object.Destroy(birdSilhouette);
                for (int i = 0; i < cleanPatches.Length; i++) if (cleanPatches[i] != null) Object.Destroy(cleanPatches[i]);
                System.Array.Clear(cleanPatches, 0, cleanPatches.Length);
                softDisc = birdSilhouette = null; cleanPlate = flags = shirts = awnings = null; loadAttempted = false;
            }

#if UNITY_EDITOR
            public void SetElapsedForTesting(float value) { elapsed = Mathf.Max(0f, value); lastFrame = -1; }
            public static Vector2 ClothOffsetForTesting(float normalizedX, float time, float phase, float amplitude)
            {
                float flex = Mathf.Sin(Mathf.Clamp01(normalizedX) * Mathf.PI) * Mathf.Sin(time * 2.1f + phase + normalizedX * 5.2f);
                return new Vector2(normalizedX, flex * amplitude);
            }
            public static Vector2 CloudSampleForTesting(float time, int index)
                => new Vector2(index == 0 ? 260f + Mathf.Repeat(time * 4f, 500f) : 450f + Mathf.Repeat(time * 4f + 170f, 300f), index == 0 ? 990f : 1390f);
            public static Vector2 BirdSampleForTesting(float time, int index)
            {
                float t = Mathf.Repeat(Mathf.Repeat(time, 18f) / 18f + index / 3f, 1f);
                return new Vector2(Mathf.Lerp(-20f, 850f, t), 237f + index * 23f + Mathf.Sin(t * Mathf.PI * 2f) * 5f);
            }
            public static float LightPulseForTesting(float time) => .05f + (Mathf.Sin(time * .72f) + 1f) * .018f;
            public bool ResourcesReadyForTesting => cleanPlate != null && flags != null && shirts != null && awnings != null;
#endif
        }

#if UNITY_EDITOR
        internal void SetHubLayeredEnvironmentElapsedForTesting(float elapsed)
        {
            if (hubLayeredEnvironmentRenderer == null) hubLayeredEnvironmentRenderer = new HubLayeredEnvironmentRenderer(this);
            hubLayeredEnvironmentRenderer.SetElapsedForTesting(elapsed);
        }

        internal void SetHubLayeredEnvironmentBypassedForTesting(bool bypass)
        {
            bypassHubLayeredEnvironmentForTesting = bypass;
        }

        internal static Vector2 HubLayeredClothOffsetForTesting(float x, float time, float phase, float amplitude)
            => HubLayeredEnvironmentRenderer.ClothOffsetForTesting(x, time, phase, amplitude);
        internal static Vector2 HubLayeredCloudSampleForTesting(float time, int index) => HubLayeredEnvironmentRenderer.CloudSampleForTesting(time, index);
        internal static Vector2 HubLayeredBirdSampleForTesting(float time, int index) => HubLayeredEnvironmentRenderer.BirdSampleForTesting(time, index);
        internal static float HubLayeredLightPulseForTesting(float time) => HubLayeredEnvironmentRenderer.LightPulseForTesting(time);
        internal bool HubLayeredEnvironmentResourcesReadyForTesting => hubLayeredEnvironmentRenderer != null && hubLayeredEnvironmentRenderer.ResourcesReadyForTesting;
#endif
    }
}
