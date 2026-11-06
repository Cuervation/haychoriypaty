using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int HubAmbientActivityMultiplier = 3;
        private static float HubAmbientOpacity(float baseline) => Mathf.Clamp01(baseline * HubAmbientActivityMultiplier);
        private static float HubAmbientInterval(float baseline) => baseline / HubAmbientActivityMultiplier;
        private HubAmbientOverlay hubAmbientOverlay;

        private void DrawHubAmbientEffects()
        {
            if (hubAmbientOverlay == null) hubAmbientOverlay = new HubAmbientOverlay(this);
            hubAmbientOverlay.Draw();
        }

        partial void ReleaseHubAmbientEffectsResources();

        partial void ReleaseHubAmbientEffectsResources()
        {
            if (hubAmbientOverlay != null) hubAmbientOverlay.Dispose();
            hubAmbientOverlay = null;
        }

        // These image-space anchors are intentionally local overlays; they never alter the artwork or hitboxes.
        // IDs are stable for the focused EditMode mapping test below.
        internal static Rect HubAmbientLayoutBounds(int element, Vector2 screenSize, Rect safeArea)
        {
            Rect source = HubAmbientSourceBounds(element);
            return source.width <= 0f ? Rect.zero : HubArtworkRectToLayout(source, HubArtworkSize, screenSize, safeArea);
        }

        private static Rect HubAmbientSourceBounds(int element)
        {
            switch (element)
            {
                case 0: return new Rect(244f, 152f, 206f, 248f); // Upper open sky for falling leaves.
                case 1: return new Rect(211f, 688f, 77f, 91f);   // Existing grill haze, right of the cook.
                case 2: return new Rect(24f, 790f, 164f, 48f);   // Existing visible coals only.
                case 3: return new Rect(515f, 463f, 55f, 52f);   // Ranking star, not its lettering.
                case 4: return new Rect(126f, 870f, 273f, 86f);  // Interior of the existing JUGAR sign.
                default: return Rect.zero;
            }
        }

        private sealed class HubAmbientOverlay
        {
            private const int LeafRouteCount = 5;
            private const int LeafCount = LeafRouteCount * HubAmbientActivityMultiplier;
            private const int SmokeCount = 3 * HubAmbientActivityMultiplier;
            private const int SmokeShapeCount = 3;
            private const int SparkCount = 2;
            private readonly StreetView owner;
            private readonly System.Random random = new System.Random();
            private readonly FallingLeaf[] leaves = new FallingLeaf[LeafCount];
            private readonly SmokePuff[] smoke = new SmokePuff[SmokeCount];
            private readonly SparkFlight[] sparks = new SparkFlight[SparkCount];
            private readonly float[] emberPhases = new float[6];
            private readonly Vector2[] emberPoints = {
                new Vector2(39f, 808f), new Vector2(67f, 816f), new Vector2(96f, 806f),
                new Vector2(123f, 817f), new Vector2(151f, 807f), new Vector2(177f, 815f)
            };
            private readonly Texture2D[] leafTextures = new Texture2D[4];
            private readonly Texture2D[] smokeTextures = new Texture2D[SmokeShapeCount];
            private Texture2D softDisc;
            private Texture2D buttonSweep;
            private float animationTime;
            private int lastUpdatedFrame = -1;
            private float nextRankingShineAt;
            private float rankingShineStartedAt = -1f;
            private float rankingShineDuration;
            private float nextPlaySweepAt;
            private float playSweepStartedAt = -1f;
            private float playSweepDuration;
            private bool resourcesCreated;

            private struct FallingLeaf
            {
                public bool active;
                public float nextStartAt, startAt, duration;
                public float x, startY, endY, drift, sway, phase, angle, spin, size;
                public int textureIndex, routeIndex;
                public bool hasSpawned;
            }

            private struct SmokePuff
            {
                public bool active;
                public float nextStartAt, startAt, duration;
                public float x, startY, rise, drift, size, aspect, sway, phase, swayFrequency;
                public int shapeIndex;
            }

            private struct SparkFlight
            {
                public bool active;
                public float nextStartAt, startAt, duration;
                public float x, y, drift, rise, angle;
            }

            private struct LeafRoute
            {
                public Rect source, destination;
                public LeafRoute(Rect source, Rect destination) { this.source = source; this.destination = destination; }
            }

            private static readonly LeafRoute[] LeafRoutes = {
                new LeafRoute(new Rect(270f, 158f, 168f, 67f), new Rect(270f, 292f, 168f, 42f)), // Open sky.
                new LeafRoute(new Rect(64f, 142f, 184f, 146f), new Rect(275f, 398f, 192f, 182f)), // Left stadium tree.
                new LeafRoute(new Rect(706f, 258f, 128f, 160f), new Rect(534f, 492f, 190f, 202f)), // Right stadium tree.
                new LeafRoute(new Rect(18f, 802f, 132f, 190f), new Rect(184f, 1038f, 176f, 226f)), // Lower-left shop tree.
                new LeafRoute(new Rect(704f, 868f, 132f, 190f), new Rect(506f, 1115f, 194f, 230f)) // Lower-right tree.
            };

            public HubAmbientOverlay(StreetView owner)
            {
                this.owner = owner;
                for (int i = 0; i < leaves.Length; i++)
                {
                    FallingLeaf leaf = new FallingLeaf
                    {
                        nextStartAt = i < LeafRouteCount ? 0f : (i - LeafRouteCount) * .12f,
                        routeIndex = i % LeafRouteCount
                    };
                    leaves[i] = leaf;
                }
                for (int i = 0; i < smoke.Length; i++)
                {
                    SmokePuff puff = new SmokePuff { nextStartAt = i * HubAmbientInterval(.75f) };
                    smoke[i] = puff;
                }
                for (int i = 0; i < sparks.Length; i++) sparks[i] = new SparkFlight { nextStartAt = .3f + i * .8f };
                for (int i = 0; i < emberPhases.Length; i++) emberPhases[i] = Range(0f, Mathf.PI * 2f);
                nextRankingShineAt = HubAmbientInterval(Range(6f, 11f));
                nextPlaySweepAt = HubAmbientInterval(Range(7f, 12f));
            }

            public void Draw()
            {
                Event current = Event.current;
                if (current == null || current.type != EventType.Repaint) return;
                AdvanceClockOncePerFrame();
                EnsureResources();

                Color previousColor = GUI.color;
                try
                {
                    DrawLeaves();
                    DrawSmoke();
                    DrawEmbers();
                    DrawSparks();
                    // Sign attention is handled by the actual pulsing touch targets.
                    // No glitter or sweeps over the cleaned sign artwork.
                }
                finally { GUI.color = previousColor; }
            }

            private void AdvanceClockOncePerFrame()
            {
                if (lastUpdatedFrame == Time.frameCount) return;
                if (lastUpdatedFrame >= 0) animationTime += Mathf.Clamp(Time.unscaledDeltaTime, 0f, .1f);
                lastUpdatedFrame = Time.frameCount;
            }

            private float Range(float minimum, float maximum) => minimum + (float)random.NextDouble() * (maximum - minimum);

            private void EnsureResources()
            {
                if (resourcesCreated) return;
                leafTextures[0] = CreateLeafTexture(new Color(.93f, .72f, .16f), new Color(1f, .91f, .49f));
                leafTextures[1] = CreateLeafTexture(new Color(.64f, .78f, .22f), new Color(.86f, .93f, .48f));
                leafTextures[2] = CreateLeafTexture(new Color(.85f, .56f, .12f), new Color(1f, .82f, .38f));
                leafTextures[3] = CreateLeafTexture(new Color(.45f, .68f, .24f), new Color(.75f, .87f, .40f));
                for (int i = 0; i < smokeTextures.Length; i++) smokeTextures[i] = CreateSmokeTexture(i);
                softDisc = CreateSoftDisc();
                buttonSweep = CreateButtonSweep();
                resourcesCreated = true;
            }

            private static Texture2D NewRuntimeTexture(int width, int height, string name)
            {
                return new Texture2D(width, height, TextureFormat.RGBA32, false, true)
                {
                    name = name,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            private static Texture2D CreateLeafTexture(Color fill, Color vein)
            {
                const int size = 24;
                Texture2D texture = NewRuntimeTexture(size, size, "Hub ambient leaf overlay");
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    float v = y / (float)(size - 1);
                    float halfWidth = Mathf.Sin(Mathf.PI * v) * .39f;
                    float center = .5f + .08f * Mathf.Sin(v * Mathf.PI);
                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)(size - 1);
                        float distance = Mathf.Abs(u - center);
                        float edge = Mathf.Clamp01((halfWidth - distance) * size * 2.2f);
                        float alpha = edge * Mathf.Clamp01(Mathf.Min(v, 1f - v) * 10f + .12f);
                        if (alpha <= 0f) continue;
                        bool midrib = distance < .028f && v > .12f && v < .88f;
                        Color color = midrib ? vein : fill;
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                return texture;
            }

            private static Texture2D CreateSmokeTexture(int variant)
            {
                const int size = 64;
                Texture2D texture = NewRuntimeTexture(size, size, "Hub ambient smoke silhouette " + variant);
                Color[] pixels = new Color[size * size];
                float offset = (variant - 1) * .12f;
                float lowerX = .48f + offset * .28f;
                float middleX = .43f - offset;
                float upperX = .57f + offset * .8f;
                float lowerWidth = .27f + variant * .018f;
                float middleWidth = .23f - variant * .012f;
                float upperWidth = .18f + variant * .014f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size;
                    float v = (y + .5f) / size;
                    float lower = GaussianLobe(u, v, lowerX, .72f, lowerWidth, .25f);
                    float middle = GaussianLobe(u, v, middleX, .48f, middleWidth, .23f);
                    float upper = GaussianLobe(u, v, upperX, .25f, upperWidth, .19f);
                    float alpha = Mathf.Clamp01(Mathf.Max(lower, Mathf.Max(middle, upper)) * .86f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                return texture;
            }

            private static float GaussianLobe(float x, float y, float centerX, float centerY, float radiusX, float radiusY)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                return Mathf.Exp(-2.7f * (dx * dx + dy * dy));
            }

            private static Texture2D CreateSoftDisc()
            {
                const int size = 32;
                Texture2D texture = NewRuntimeTexture(size, size, "Hub ambient soft glow");
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2f - 1f;
                    float dy = (y + .5f) / size * 2f - 1f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2.4f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                return texture;
            }

            private static Texture2D CreateButtonSweep()
            {
                const int width = 48, height = 112;
                Texture2D texture = NewRuntimeTexture(width, height, "Hub JUGAR subtle sweep");
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    float center = .46f + .34f * (v - .5f);
                    float distance = Mathf.Abs(u - center);
                    float band = Mathf.Clamp01(1f - distance / .30f);
                    float edge = Mathf.Sin(Mathf.PI * v);
                    float alpha = band * edge * HubAmbientOpacity(.25f);
                    pixels[y * width + x] = new Color(1f, .91f, .67f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                return texture;
            }

            private Rect LayoutBounds(Rect source)
            {
                Rect logical = HubArtworkRectToLayout(source, HubArtworkSize,
                    new Vector2(Screen.width, Screen.height), Screen.safeArea);
                return owner.LayoutRect(logical);
            }

            private void DrawSourceTexture(Texture2D texture, Rect source, Color tint)
            {
                if (texture == null) return;
                Rect bounds = LayoutBounds(source);
                if (bounds.width <= 0f || bounds.height <= 0f) return;
                GUI.color = tint;
                GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
            }

            private void DrawLeaves()
            {
                for (int i = 0; i < leaves.Length; i++)
                {
                    FallingLeaf leaf = leaves[i];
                    if (!leaf.active)
                    {
                        if (animationTime < leaf.nextStartAt) continue;
                        if (leaf.hasSpawned) leaf.routeIndex = random.Next(0, LeafRoutes.Length);
                        leaf.hasSpawned = true;
                        LeafRoute route = LeafRoutes[leaf.routeIndex];
                        leaf.active = true;
                        leaf.startAt = animationTime;
                        leaf.duration = Range(6.5f, 9.0f);
                        leaf.x = Range(route.source.xMin, route.source.xMax);
                        leaf.startY = Range(route.source.yMin, route.source.yMax);
                        leaf.endY = Range(route.destination.yMin, route.destination.yMax);
                        float landingX = Range(route.destination.xMin, route.destination.xMax);
                        leaf.drift = landingX - leaf.x;
                        leaf.sway = Range(7f, 15f);
                        leaf.phase = Range(0f, Mathf.PI * 2f);
                        leaf.angle = Range(-100f, 100f);
                        leaf.spin = Range(-95f, 95f);
                        leaf.size = Range(1.2f, 1.6f);
                        // Seed the first wave mid-flight: entering the hub must never look like a still image.
                        if (animationTime < .1f) leaf.startAt -= leaf.duration * Range(.14f, .32f);
                        leaf.textureIndex = random.Next(0, leafTextures.Length);
                    }

                    float progress = (animationTime - leaf.startAt) / leaf.duration;
                    if (progress >= 1f)
                    {
                        leaf.active = false;
                        leaf.nextStartAt = animationTime + HubAmbientInterval(Range(2.5f, 5.5f));
                        leaves[i] = leaf;
                        continue;
                    }

                    float eased = progress * progress * (3f - 2f * progress);
                    float x = leaf.x + leaf.drift * progress + Mathf.Sin(leaf.phase + progress * 5.8f) * leaf.sway;
                    float y = Mathf.Lerp(leaf.startY, leaf.endY, eased);
                    float size = leaf.size * Mathf.Lerp(.76f, 1f, Mathf.Sin(progress * Mathf.PI));
                    float alpha = Mathf.Sin(progress * Mathf.PI) * .84f;
                    Rect source = new Rect(x - 7f * size, y - 10f * size, 14f * size, 20f * size);
                    Rect bounds = LayoutBounds(source);
                    if (bounds.width > 0f && bounds.height > 0f && leafTextures[leaf.textureIndex] != null)
                    {
                        Matrix4x4 previousMatrix = GUI.matrix;
                        try
                        {
                            GUIUtility.RotateAroundPivot(leaf.angle + leaf.spin * progress, bounds.center);
                            GUI.color = new Color(1f, 1f, 1f, alpha);
                            GUI.DrawTexture(bounds, leafTextures[leaf.textureIndex], ScaleMode.StretchToFill, true);
                        }
                        finally { GUI.matrix = previousMatrix; }
                    }
                    leaves[i] = leaf;
                }
            }

            private void DrawSmoke()
            {
                for (int i = 0; i < smoke.Length; i++)
                {
                    SmokePuff puff = smoke[i];
                    if (!puff.active)
                    {
                        if (animationTime < puff.nextStartAt) continue;
                        puff.active = true;
                        puff.startAt = animationTime;
                        puff.duration = Range(2.6f, 4.2f);
                        puff.x = Range(223f, 271f);
                        puff.startY = Range(736f, 766f);
                        puff.rise = Range(48f, 82f);
                        puff.drift = Range(-22f, 22f);
                        puff.size = Range(43f, 62f);
                        puff.aspect = Range(.58f, .88f);
                        puff.sway = Range(6f, 16f);
                        puff.phase = Range(0f, Mathf.PI * 2f);
                        puff.swayFrequency = Range(2.8f, 5.8f);
                        puff.shapeIndex = random.Next(0, smokeTextures.Length);
                        if (animationTime < .1f) puff.startAt -= puff.duration * .4f;
                    }

                    float progress = (animationTime - puff.startAt) / puff.duration;
                    if (progress >= 1f)
                    {
                        puff.active = false;
                        puff.nextStartAt = animationTime + HubAmbientInterval(Range(.35f, 1.0f));
                        smoke[i] = puff;
                        continue;
                    }

                    float alpha = Mathf.Sin(progress * Mathf.PI) * HubAmbientOpacity(.15f);
                    float width = puff.size * Mathf.Lerp(.74f, 1.25f, progress);
                    float height = puff.size * puff.aspect * Mathf.Lerp(.88f, 1.32f, progress);
                    float y = puff.startY - puff.rise * progress;
                    float x = puff.x + puff.drift * progress + Mathf.Sin(progress * puff.swayFrequency + puff.phase) * puff.sway * Mathf.Sin(progress * Mathf.PI);
                    DrawSourceTexture(smokeTextures[puff.shapeIndex], new Rect(x - width * .5f, y - height * .5f, width, height),
                        new Color(.88f, .84f, .77f, alpha));
                    smoke[i] = puff;
                }
            }

            private void DrawEmbers()
            {
                for (int i = 0; i < emberPoints.Length; i++)
                {
                    float pulse = .5f + .5f * Mathf.Sin(animationTime * (2.2f + i * .19f) + emberPhases[i]);
                    float alpha = HubAmbientOpacity(.045f + pulse * .055f);
                    float size = 14f + pulse * 9f;
                    Vector2 point = emberPoints[i];
                    DrawSourceTexture(softDisc, new Rect(point.x - size * .5f, point.y - size * .5f, size, size),
                        new Color(1f, .30f + pulse * .18f, .045f, alpha));
                }
            }

            private void DrawSparks()
            {
                for (int i = 0; i < sparks.Length; i++)
                {
                    SparkFlight spark = sparks[i];
                    if (!spark.active)
                    {
                        if (animationTime < spark.nextStartAt) continue;
                        spark.active = true;
                        spark.startAt = animationTime;
                        spark.duration = Range(.34f, .52f);
                        spark.x = Range(42f, 174f);
                        spark.y = Range(795f, 817f);
                        spark.drift = Range(-9f, 9f);
                        spark.rise = Range(12f, 23f);
                        spark.angle = Range(-26f, 26f);
                    }

                    float progress = (animationTime - spark.startAt) / spark.duration;
                    if (progress >= 1f)
                    {
                        spark.active = false;
                        spark.nextStartAt = animationTime + HubAmbientInterval(Range(3.0f, 5.6f));
                        sparks[i] = spark;
                        continue;
                    }

                    float alpha = Mathf.Sin(progress * Mathf.PI) * .7f;
                    float x = spark.x + spark.drift * progress;
                    float y = spark.y - spark.rise * progress;
                    DrawSourceTexture(softDisc, new Rect(x - 4f, y - 4f, 8f, 8f), new Color(1f, .56f, .10f, alpha));
                    Matrix4x4 previous = GUI.matrix;
                    try
                    {
                        Rect ray = LayoutBounds(new Rect(x - .75f, y - 8.5f, 1.5f, 14f));
                        GUIUtility.RotateAroundPivot(spark.angle, ray.center);
                        GUI.color = new Color(1f, .78f, .30f, alpha * .76f);
                        GUI.DrawTexture(ray, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                    }
                    finally { GUI.matrix = previous; }
                    sparks[i] = spark;
                }
            }

            private void DrawRankingShine()
            {
                if (rankingShineStartedAt < 0f && animationTime >= nextRankingShineAt)
                {
                    rankingShineStartedAt = animationTime;
                    rankingShineDuration = Range(.55f, .80f);
                    nextRankingShineAt = animationTime + HubAmbientInterval(Range(6f, 11f));
                }
                if (rankingShineStartedAt < 0f) return;
                float progress = (animationTime - rankingShineStartedAt) / rankingShineDuration;
                if (progress >= 1f) { rankingShineStartedAt = -1f; return; }
                float alpha = Mathf.Sin(progress * Mathf.PI) * HubAmbientOpacity(.28f);
                DrawSourceTexture(softDisc, new Rect(519f, 467f, 46f, 44f), new Color(1f, .79f, .25f, alpha));
                float glint = Mathf.Sin(progress * Mathf.PI) * HubAmbientOpacity(.42f);
                DrawSourceTexture(Texture2D.whiteTexture, new Rect(537f, 471f, 1.5f, 29f), new Color(1f, .91f, .57f, glint));
            }

            private void DrawPlaySweep()
            {
                if (playSweepStartedAt < 0f && animationTime >= nextPlaySweepAt)
                {
                    playSweepStartedAt = animationTime;
                    playSweepDuration = Range(1.0f, 1.3f);
                    nextPlaySweepAt = animationTime + HubAmbientInterval(Range(7f, 12f));
                }
                if (playSweepStartedAt < 0f) return;
                float progress = (animationTime - playSweepStartedAt) / playSweepDuration;
                if (progress >= 1f) { playSweepStartedAt = -1f; return; }
                float alpha = Mathf.Sin(progress * Mathf.PI) * .52f;
                float centerX = Mathf.Lerp(153f, 375f, progress);
                DrawSourceTexture(buttonSweep, new Rect(centerX - 17f, 872f, 34f, 72f), new Color(1f, 1f, 1f, alpha));
            }

            public void Dispose()
            {
                for (int i = 0; i < leafTextures.Length; i++) DestroyTexture(leafTextures[i]);
                for (int i = 0; i < smokeTextures.Length; i++) DestroyTexture(smokeTextures[i]);
                DestroyTexture(softDisc);
                DestroyTexture(buttonSweep);
                Array.Clear(leafTextures, 0, leafTextures.Length);
                Array.Clear(smokeTextures, 0, smokeTextures.Length);
                softDisc = null;
                buttonSweep = null;
                resourcesCreated = false;
            }

            private static void DestroyTexture(Texture2D texture)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }

            public static int LeafRouteCountForTesting => LeafRoutes.Length;
            public static Vector2 LeafRouteStartForTesting(int route) => LeafRoutes[route].source.center;
            public static Vector2 LeafRouteEndForTesting(int route) => LeafRoutes[route].destination.center;
            public static int SmokeShapeCountForTesting => SmokeShapeCount;
            public static int SparkCountForTesting => SparkCount;
        }

#if UNITY_EDITOR
        internal static int HubAmbientLeafRouteCountForTesting => HubAmbientOverlay.LeafRouteCountForTesting;
        internal static Vector2 HubAmbientLeafRouteStartForTesting(int route) => HubAmbientOverlay.LeafRouteStartForTesting(route);
        internal static Vector2 HubAmbientLeafRouteEndForTesting(int route) => HubAmbientOverlay.LeafRouteEndForTesting(route);
        internal static int HubAmbientSmokeShapeCountForTesting => HubAmbientOverlay.SmokeShapeCountForTesting;
        internal static int HubAmbientSparkCountForTesting => HubAmbientOverlay.SparkCountForTesting;
#endif
    }
}
