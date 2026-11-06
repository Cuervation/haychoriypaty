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
            private const int AwningHorizontalSegments = 12;
            private const int AwningVerticalSegments = 2;
            private const int BirdCount = 4;
            private const float FlagAmplitude = 7.5f;
            private const float ShirtAmplitude = 5.5f;
            private const float AwningAmplitude = 7f;
            private readonly StreetView owner;
            private readonly System.Random random = new System.Random();
            private readonly float[] flagPhases = { 0f, 1.1f, 2.3f, 3.7f, 4.8f };
            private readonly float[] shirtPhases = { .7f };
            private readonly float[] awningPhases = { 1.6f, 3.1f };
            private readonly float[] flagFrequencies = { 1.72f, 2.04f, 2.36f, 1.84f, 2.2f };
            private readonly float[] shirtFrequencies = { 1.93f };
            private readonly float[] awningFrequencies = { 1.12f, 1.38f };
            private readonly BirdFlight[] birds = new BirdFlight[BirdCount];
            private Texture2D cleanPlate, flags, shirts, awnings, softDisc, birdSilhouette;
            private float elapsed;
            private int lastFrame = -1;
            private bool loadAttempted;
            private readonly Texture2D[] cleanPatches = new Texture2D[HubLayeredFlagBounds.Length + HubLayeredShirtBounds.Length + HubLayeredAwningBounds.Length];

            private struct BirdFlight
            {
                public bool active;
                public float nextStartAt, startAt, duration;
                public float startX, endX, baseY, arc, wave, phase, scale, wingSpeed;
            }

            public HubLayeredEnvironmentRenderer(StreetView owner)
            {
                this.owner = owner;
                for (int i = 0; i < birds.Length; i++)
                {
                    BirdFlight bird = new BirdFlight { nextStartAt = i == 0 ? .35f : i * 1.65f + Range(0f, .9f) };
                    birds[i] = bird;
                }
            }

            private float Range(float minimum, float maximum) => minimum + (float)random.NextDouble() * (maximum - minimum);

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
                        DrawClothGroup(flags, HubLayeredFlagBounds, HubLayeredFlagSourceUv, flagPhases, flagFrequencies, FlagAmplitude, 0);
                        DrawClothGroup(shirts, HubLayeredShirtBounds, HubLayeredShirtSourceUv, shirtPhases, shirtFrequencies, ShirtAmplitude, HubLayeredFlagBounds.Length);
                        DrawAwningGroup();
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

            private void DrawClothGroup(Texture2D foreground, Rect[] bounds, Rect[] sourceUvs, float[] phases, float[] frequencies, float amplitude, int patchOffset)
            {
                if (foreground == null) return; // Missing layer disables only this group; untouched master remains visible.
                for (int i = 0; i < bounds.Length; i++)
                {
                    if (cleanPatches[patchOffset + i] == null) continue;
                    Rect art = bounds[i];
                    DrawCleanPatch(cleanPatches[patchOffset + i], art);
                    DrawClothSlices(foreground, art, sourceUvs[i], phases[i], frequencies[i], amplitude);
                }
            }

            private void DrawAwningGroup()
            {
                if (awnings == null) return;
                int patchOffset = HubLayeredFlagBounds.Length + HubLayeredShirtBounds.Length;
                for (int i = 0; i < HubLayeredAwningBounds.Length; i++)
                {
                    if (cleanPatches[patchOffset + i] == null) continue;
                    Rect art = HubLayeredAwningBounds[i];
                    DrawCleanPatch(cleanPatches[patchOffset + i], art);
                    DrawPinnedAwningWaves(awnings, art, HubLayeredAwningSourceUv[i], awningPhases[i], awningFrequencies[i]);
                }
            }

            private void DrawPinnedAwningWaves(Texture2D texture, Rect art, Rect sourceUv, float phase, float frequency)
            {
                // The roof seam and both side edges stay pinned; only the lower cloth ripples in a small, coherent wave.
                for (int x = 0; x < AwningHorizontalSegments; x++)
                {
                    float x0 = x / (float)AwningHorizontalSegments;
                    float x1 = (x + 1) / (float)AwningHorizontalSegments;
                    float centerX = (x0 + x1) * .5f;
                    for (int y = 0; y < AwningVerticalSegments; y++)
                    {
                        float y0 = y / (float)AwningVerticalSegments;
                        float y1 = (y + 1) / (float)AwningVerticalSegments;
                        float weight = y0; // 0 at the attached top edge, gradually stronger toward the scalloped hem.
                        float flex = AwningOffset(centerX, weight, elapsed, phase + y * .31f, frequency);
                        if (x == 0 || x == AwningHorizontalSegments - 1) flex = 0f;
                        float overlap = AwningAmplitude / art.height + .005f;
                        float sampleY0 = Mathf.Max(0f, y0 - overlap);
                        float sampleY1 = Mathf.Min(1f, y1 + overlap);
                        Rect piece = new Rect(art.x + art.width * x0, art.y + art.height * sampleY0 + flex,
                            art.width * (x1 - x0) + .35f, art.height * (sampleY1 - sampleY0));
                        DrawTextureRegion(texture, piece, sourceUv, new Rect(x0, sampleY0, x1 - x0, sampleY1 - sampleY0));
                    }
                }
            }

            private void DrawClothSlices(Texture2D texture, Rect art, Rect sourceUv, float phase, float frequency, float amplitude)
            {
                for (int s = 0; s < ClothSegments; s++)
                {
                    float x0 = s / (float)ClothSegments, x1 = (s + 1) / (float)ClothSegments;
                    float localFlex = Mathf.Sin(x0 * Mathf.PI) * ClothWave(x0, phase, frequency);
                    float nextFlex = Mathf.Sin(x1 * Mathf.PI) * ClothWave(x1, phase, frequency);
                    float y = (localFlex + nextFlex) * .5f * amplitude;
                    Rect piece = new Rect(art.x + art.width * x0, art.y + y, art.width / ClothSegments + .25f, art.height);
                    DrawTextureRegion(texture, piece, sourceUv, new Rect(x0, 0f, 1f / ClothSegments, 1f));
                }
            }

            private float ClothWave(float x, float phase, float frequency) => ClothWave(x, phase, frequency, elapsed);

            private static float ClothWave(float x, float phase, float frequency, float time)
                => Mathf.Sin(time * frequency + phase + x * 5.2f) * .82f +
                   Mathf.Sin(time * frequency * .61f + phase * .73f + x * 2.9f) * .18f;

            private static float AwningOffset(float x, float normalizedHeight, float time, float phase, float frequency)
                => Mathf.Sin(Mathf.Clamp01(x) * Mathf.PI) * Mathf.Clamp01(normalizedHeight) *
                   ClothWave(Mathf.Clamp01(x), phase, frequency, time) * AwningAmplitude;

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
                Vector2 first = CloudPosition(elapsed, 0);
                Vector2 second = CloudPosition(elapsed, 1);
                DrawCloudShadow(first, 286f, 82f, .045f);
                DrawSoftOval(new Rect(first.x + 52f, first.y + 9f, 192f, 65f), .032f);
                DrawCloudShadow(second, 248f, 76f, .041f);
                DrawSoftOval(new Rect(second.x - 44f, second.y + 12f, 174f, 58f), .029f);
            }

            private void DrawCloudShadow(Vector2 center, float width, float height, float alpha)
                => DrawSoftOval(new Rect(center.x - width * .5f, center.y - height * .5f, width, height), alpha);

            private static Vector2 CloudPosition(float time, int index)
            {
                float speed = index == 0 ? 27f : 20f;
                float offset = index == 0 ? 0f : 485f;
                float baseY = index == 0 ? 1020f : 1255f;
                float amplitude = index == 0 ? 150f : 125f;
                float verticalFrequency = index == 0 ? .38f : .31f;
                float phase = index * 2.1f;
                float x = Mathf.Repeat(time * speed + offset, 1120f) - 140f;
                float y = baseY + Mathf.Sin(time * verticalFrequency + phase) * amplitude;
                return new Vector2(x, y);
            }

            private void DrawDistantBirds()
            {
                if (birdSilhouette == null) return;
                for (int i = 0; i < birds.Length; i++)
                {
                    BirdFlight bird = birds[i];
                    if (!bird.active)
                    {
                        if (elapsed < bird.nextStartAt) continue;
                        bird = StartBirdFlight();
                        bird.startAt = elapsed;
                    }

                    float progress = (elapsed - bird.startAt) / bird.duration;
                    if (progress >= 1f)
                    {
                        bird.active = false;
                        bird.nextStartAt = elapsed + Range(1.25f, 4.25f);
                        birds[i] = bird;
                        continue;
                    }

                    Vector2 position = EvaluateBirdPosition(progress, bird.startX, bird.endX, bird.baseY, bird.arc, bird.wave, bird.phase);
                    float edgeFade = Mathf.Min(Mathf.Clamp01(progress / .12f), Mathf.Clamp01((1f - progress) / .14f));
                    float wingHeight = (10f + Mathf.Sin(elapsed * bird.wingSpeed + bird.phase) * 2.8f) * bird.scale;
                    float width = 34f * bird.scale;
                    Rect screen = HubLayeredArtworkScreenRect(new Rect(position.x - width * .5f, position.y - wingHeight * .5f, width, wingHeight),
                        new Vector2(Screen.width, Screen.height));
                    GUI.color = new Color(.17f, .22f, .29f, .76f * edgeFade);
                    GUI.DrawTexture(screen, birdSilhouette, ScaleMode.StretchToFill, true);
                    birds[i] = bird;
                }
            }

            private BirdFlight StartBirdFlight()
            {
                bool leftToRight = random.Next(0, 2) == 0;
                return new BirdFlight
                {
                    active = true,
                    duration = Range(7.5f, 13.5f),
                    startX = leftToRight ? -36f : 877f,
                    endX = leftToRight ? 877f : -36f,
                    baseY = Range(175f, 365f),
                    arc = Range(-24f, 24f),
                    wave = Range(6f, 18f),
                    phase = Range(0f, Mathf.PI * 2f),
                    scale = Range(.66f, 1.12f),
                    wingSpeed = Range(4.2f, 7.2f)
                };
            }

            private static Vector2 EvaluateBirdPosition(float progress, float startX, float endX, float baseY, float arc, float wave, float phase)
            {
                float t = Mathf.Clamp01(progress);
                float x = Mathf.Lerp(startX, endX, t);
                float y = baseY + Mathf.Sin(t * Mathf.PI) * arc + Mathf.Sin(t * Mathf.PI * 2f + phase) * wave;
                return new Vector2(x, y);
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
                float pulse = .105f + (.5f + .5f * Mathf.Sin(elapsed * .82f)) * .075f;
                DrawWarmGlow(new Rect(132f, 1359f, 39f, 52f), pulse);
                DrawWarmGlow(new Rect(174f, 1363f, 37f, 49f), pulse * .82f);
                float reflection = .048f + (.5f + .5f * Mathf.Sin(elapsed * .56f + 1.7f)) * .026f;
                DrawWarmGlow(new Rect(102f, 1401f, 130f, 37f), reflection);
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
                return ClothOffsetForTesting(normalizedX, time, phase, amplitude, 2.1f);
            }
            public static Vector2 ClothOffsetForTesting(float normalizedX, float time, float phase, float amplitude, float frequency)
            {
                float x = Mathf.Clamp01(normalizedX);
                float wave = Mathf.Sin(time * frequency + phase + x * 5.2f) * .82f +
                             Mathf.Sin(time * frequency * .61f + phase * .73f + x * 2.9f) * .18f;
                return new Vector2(x, Mathf.Sin(x * Mathf.PI) * wave * amplitude);
            }
            public static Vector2 CloudSampleForTesting(float time, int index)
                => CloudPosition(time, index);
            public static Vector2 BirdSampleForTesting(float time, int index)
            {
                float duration = 8f + index * 1.45f;
                float phase = Mathf.Repeat(time + index * 2.65f, duration + 2.5f);
                float progress = Mathf.Clamp01(phase / duration);
                float startX = index % 2 == 0 ? -36f : 877f;
                float endX = index % 2 == 0 ? 877f : -36f;
                return EvaluateBirdPosition(progress, startX, endX, 185f + index * 39f,
                    -18f + index * 8f, 7f + index * 2f, index * 1.37f);
            }
            public static float LightPulseForTesting(float time) => .105f + (.5f + .5f * Mathf.Sin(time * .82f)) * .075f;
            public static int BirdCountForTesting => BirdCount;
            public static float FlagAmplitudeForTesting => FlagAmplitude;
            public static float ShirtAmplitudeForTesting => ShirtAmplitude;
            public static float AwningAmplitudeForTesting => AwningAmplitude;
            public static float AwningOffsetForTesting(float x, float y, float time, float phase, float frequency)
                => AwningOffset(x, y, time, phase, frequency);
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
        internal static int HubLayeredBirdCountForTesting => HubLayeredEnvironmentRenderer.BirdCountForTesting;
        internal static float HubLayeredFlagAmplitudeForTesting => HubLayeredEnvironmentRenderer.FlagAmplitudeForTesting;
        internal static float HubLayeredShirtAmplitudeForTesting => HubLayeredEnvironmentRenderer.ShirtAmplitudeForTesting;
        internal static float HubLayeredAwningAmplitudeForTesting => HubLayeredEnvironmentRenderer.AwningAmplitudeForTesting;
        internal static float HubLayeredAwningOffsetForTesting(float x, float y, float time, float phase, float frequency)
            => HubLayeredEnvironmentRenderer.AwningOffsetForTesting(x, y, time, phase, frequency);
        internal bool HubLayeredEnvironmentResourcesReadyForTesting => hubLayeredEnvironmentRenderer != null && hubLayeredEnvironmentRenderer.ResourcesReadyForTesting;
#endif
    }
}
