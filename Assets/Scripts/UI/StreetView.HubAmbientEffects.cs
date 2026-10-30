using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
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
            private const int LeafCount = 4;
            private const int SmokeCount = 3;
            private readonly StreetView owner;
            private readonly System.Random random = new System.Random();
            private readonly FallingLeaf[] leaves = new FallingLeaf[LeafCount];
            private readonly SmokePuff[] smoke = new SmokePuff[SmokeCount];
            private readonly float[] emberPhases = new float[4];
            private readonly Vector2[] emberPoints = { new Vector2(45f, 808f), new Vector2(78f, 815f), new Vector2(121f, 808f), new Vector2(157f, 818f) };
            private readonly Texture2D[] leafTextures = new Texture2D[4];
            private Texture2D softDisc;
            private Texture2D buttonSweep;
            private float animationTime;
            private int lastUpdatedFrame = -1;
            private float nextSparkAt = 2.8f;
            private float sparkStartedAt = -1f;
            private float sparkDuration;
            private float sparkX;
            private float sparkY;
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
                public int textureIndex;
            }

            private struct SmokePuff
            {
                public bool active;
                public float nextStartAt, startAt, duration;
                public float x, startY, rise, drift, size;
            }

            public HubAmbientOverlay(StreetView owner)
            {
                this.owner = owner;
                for (int i = 0; i < leaves.Length; i++)
                {
                    FallingLeaf leaf = new FallingLeaf { nextStartAt = i * 1.9f + Range(0f, .9f) };
                    leaves[i] = leaf;
                }
                for (int i = 0; i < smoke.Length; i++)
                {
                    SmokePuff puff = new SmokePuff { nextStartAt = i * .75f + Range(0f, .25f) };
                    smoke[i] = puff;
                }
                for (int i = 0; i < emberPhases.Length; i++) emberPhases[i] = Range(0f, Mathf.PI * 2f);
                nextRankingShineAt = Range(6f, 11f);
                nextPlaySweepAt = Range(7f, 12f);
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
                    DrawSpark();
                    DrawRankingShine();
                    DrawPlaySweep();
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
                    float alpha = band * edge * .25f;
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
                        leaf.active = true;
                        leaf.startAt = animationTime;
                        leaf.duration = Range(6.5f, 9.0f);
                        leaf.x = Range(270f, 438f);
                        leaf.startY = Range(158f, 225f);
                        leaf.endY = Range(292f, 322f);
                        leaf.drift = Range(-28f, 28f);
                        leaf.sway = Range(7f, 15f);
                        leaf.phase = Range(0f, Mathf.PI * 2f);
                        leaf.angle = Range(-100f, 100f);
                        leaf.spin = Range(-95f, 95f);
                        leaf.size = Range(.90f, 1.30f);
                        leaf.textureIndex = random.Next(0, leafTextures.Length);
                    }

                    float progress = (animationTime - leaf.startAt) / leaf.duration;
                    if (progress >= 1f)
                    {
                        leaf.active = false;
                        leaf.nextStartAt = animationTime + Range(2.5f, 5.5f);
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
                        puff.duration = Range(2.4f, 3.2f);
                        puff.x = Range(223f, 271f);
                        puff.startY = Range(736f, 766f);
                        puff.rise = Range(30f, 48f);
                        puff.drift = Range(-9f, 9f);
                        puff.size = Range(28f, 40f);
                    }

                    float progress = (animationTime - puff.startAt) / puff.duration;
                    if (progress >= 1f)
                    {
                        puff.active = false;
                        puff.nextStartAt = animationTime + Range(.35f, 1.0f);
                        smoke[i] = puff;
                        continue;
                    }

                    float alpha = Mathf.Sin(progress * Mathf.PI) * .135f;
                    float width = puff.size * Mathf.Lerp(.70f, 1.22f, progress);
                    float y = puff.startY - puff.rise * progress;
                    float x = puff.x + puff.drift * progress + Mathf.Sin(progress * 4.8f + i) * 4f;
                    DrawSourceTexture(softDisc, new Rect(x - width * .5f, y - width * .5f, width, width),
                        new Color(.88f, .84f, .77f, alpha));
                    smoke[i] = puff;
                }
            }

            private void DrawEmbers()
            {
                for (int i = 0; i < emberPoints.Length; i++)
                {
                    float pulse = .5f + .5f * Mathf.Sin(animationTime * (2.2f + i * .19f) + emberPhases[i]);
                    float alpha = .04f + pulse * .05f;
                    float size = 12f + pulse * 7f;
                    Vector2 point = emberPoints[i];
                    DrawSourceTexture(softDisc, new Rect(point.x - size * .5f, point.y - size * .5f, size, size),
                        new Color(1f, .30f + pulse * .18f, .045f, alpha));
                }
            }

            private void DrawSpark()
            {
                if (sparkStartedAt < 0f && animationTime >= nextSparkAt)
                {
                    sparkStartedAt = animationTime;
                    sparkDuration = Range(.36f, .52f);
                    sparkX = Range(68f, 145f);
                    sparkY = Range(795f, 807f);
                    nextSparkAt = animationTime + Range(3.0f, 5.6f);
                }
                if (sparkStartedAt < 0f) return;
                float progress = (animationTime - sparkStartedAt) / sparkDuration;
                if (progress >= 1f) { sparkStartedAt = -1f; return; }
                float alpha = Mathf.Sin(progress * Mathf.PI) * .55f;
                float y = sparkY - progress * 13f;
                DrawSourceTexture(softDisc, new Rect(sparkX - 3.5f, y - 3.5f, 7f, 7f), new Color(1f, .56f, .10f, alpha));
                DrawSourceTexture(Texture2D.whiteTexture, new Rect(sparkX - .8f, y - 4.5f, 1.6f, 9f), new Color(1f, .72f, .24f, alpha * .60f));
            }

            private void DrawRankingShine()
            {
                if (rankingShineStartedAt < 0f && animationTime >= nextRankingShineAt)
                {
                    rankingShineStartedAt = animationTime;
                    rankingShineDuration = Range(.55f, .80f);
                    nextRankingShineAt = animationTime + Range(6f, 11f);
                }
                if (rankingShineStartedAt < 0f) return;
                float progress = (animationTime - rankingShineStartedAt) / rankingShineDuration;
                if (progress >= 1f) { rankingShineStartedAt = -1f; return; }
                float alpha = Mathf.Sin(progress * Mathf.PI) * .28f;
                DrawSourceTexture(softDisc, new Rect(519f, 467f, 46f, 44f), new Color(1f, .79f, .25f, alpha));
                float glint = Mathf.Sin(progress * Mathf.PI) * .42f;
                DrawSourceTexture(Texture2D.whiteTexture, new Rect(537f, 471f, 1.5f, 29f), new Color(1f, .91f, .57f, glint));
            }

            private void DrawPlaySweep()
            {
                if (playSweepStartedAt < 0f && animationTime >= nextPlaySweepAt)
                {
                    playSweepStartedAt = animationTime;
                    playSweepDuration = Range(1.0f, 1.3f);
                    nextPlaySweepAt = animationTime + Range(7f, 12f);
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
                DestroyTexture(softDisc);
                DestroyTexture(buttonSweep);
                Array.Clear(leafTextures, 0, leafTextures.Length);
                softDisc = null;
                buttonSweep = null;
                resourcesCreated = false;
            }

            private static void DestroyTexture(Texture2D texture)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
