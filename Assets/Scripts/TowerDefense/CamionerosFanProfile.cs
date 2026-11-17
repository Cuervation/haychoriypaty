using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum CamionerosFanKind { Deportivo, Casual, Abrigado }

    /// <summary>Authored behavior for a look, independent of identity, animation and deliveries.</summary>
    public sealed class CamionerosFanProfile
    {
        public CamionerosFanKind Kind { get; }
        public float SpeedMultiplier { get; }
        public float FreeGapLogical { get; }
        // Camioneros square atlas cells fit to the 76px render height, hence a 76px footprint.
        // The 78px entry reference is not the fitted sprite width. Gaps are NOT center distances.
        public float HeadwayLogical => StreetSceneLayout.CustomerHeight + FreeGapLogical;
        public int MinQuantity { get; }
        public int MaxQuantity { get; }
        private CamionerosFanProfile(CamionerosFanKind kind, float speed, float gap, int min, int max)
        { Kind = kind; SpeedMultiplier = speed; FreeGapLogical = gap; MinQuantity = min; MaxQuantity = max; }

        public static readonly CamionerosFanProfile Deportivo = new CamionerosFanProfile(CamionerosFanKind.Deportivo, 1.15f, 10f, 1, 1);
        public static readonly CamionerosFanProfile Casual = new CamionerosFanProfile(CamionerosFanKind.Casual, 1f, 20f, 1, 3);
        public static readonly CamionerosFanProfile Abrigado = new CamionerosFanProfile(CamionerosFanKind.Abrigado, .85f, 30f, 2, 4);
        // Zero-based atlas cells, left-to-right/top-to-bottom. Do not change the source art.
        private static readonly int[][] Appearances = { new[] { 0, 1, 4, 8 }, new[] { 3, 5, 9, 11 }, new[] { 2, 6, 7, 10 } };
        // After 12 casuals, each nine arrivals has 3 of each kind, at most 2 sports in a row.
        private static readonly int[] MixedWave = { 0, 0, 1, 2, 2, 1, 1, 0, 2 };

        // Legacy Grid4 source rects are bottom-row-first after their GUI/Sprite conversion.
        // Profiles use the numbered sheet the player sees (top-row-first). Opt in only ID9.
        public static int AtlasIdForAppearance(int appearance)
        {
            int variant = ((appearance % 12) + 12) % 12;
            return (3 - variant / 3) * 3 + variant % 3;
        }

        public static CamionerosFanProfile ForAppearance(int appearance)
        {
            int variant = ((appearance % 12) + 12) % 12;
            if (variant == 0 || variant == 1 || variant == 4 || variant == 8) return Deportivo;
            if (variant == 3 || variant == 5 || variant == 9 || variant == 11) return Casual;
            return Abrigado;
        }

        public static int AppearanceForSpawn(int index)
        {
            index = Mathf.Max(0, index);
            if (index < 12) return Appearances[1][index % 4];
            int mixed = index - 12, kind = MixedWave[mixed % MixedWave.Length];
            int prior = (mixed / MixedWave.Length) * 3;
            for (int i = 0; i < mixed % MixedWave.Length; i++) if (MixedWave[i] == kind) prior++;
            return Appearances[kind][prior % 4];
        }
    }

    public sealed partial class TowerDefenseGameMode
    {
        private bool UsesCamionerosProfiles => config.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId && !config.FreeConstruction;
        private CamionerosFanProfile NextCamionerosProfile => CamionerosFanProfile.ForAppearance(CamionerosFanProfile.AppearanceForSpawn(spawnedFans));
        private float FanSpeedForProfile(CamionerosFanProfile profile, float scale) =>
            sourceBalance.customerSpeed * config.FanSpeed * scale * (profile == null ? 1f : profile.SpeedMultiplier);

        private void SpawnCamionerosOrder()
        {
            CamionerosFanProfile profile = NextCamionerosProfile;
            if (random.Next(2) == 0)
                CreateFan(new[] { 0, 4 }, new[] { random.Next(profile.MinQuantity, profile.MaxQuantity + 1), random.Next(profile.MinQuantity, profile.MaxQuantity + 1) });
            else
                CreateFan(new[] { random.Next(2) == 0 ? 0 : 4 }, new[] { random.Next(profile.MinQuantity, profile.MaxQuantity + 1) });
        }

        private void AdvanceCamionerosFans(float dt)
        {
            // Oldest first: each follower sees the leader's NEW progress this slice.
            // Never reorder identities, use Euclidean shortcuts across the S, or permanently lower Speed.
            TowerDefenseFanState leader = null;
            for (int i = 0; i < fans.Count; i++)
            {
                TowerDefenseFanState fan = fans[i];
                fan.AnimationTime += dt;
                fan.ReceiveAnimationSeconds = Mathf.Max(0f, fan.ReceiveAnimationSeconds - dt);
                fan.IsReceiving = fan.ReceiveAnimationSeconds > 0f;
                if (fan.IsDeparting)
                {
                    fan.DepartureProgress = Mathf.Clamp01(fan.DepartureProgress + dt / FanDepartureDurationSeconds);
                    if (fan.DepartureProgress >= 1f)
                    { fans.RemoveAt(i--); servedFans++; UpdateCompletionState(); }
                    continue; // Served supporters fade out of the lane and no longer block it.
                }
                float moveTime = dt;
                if (leader != null && fan.RouteFollower.Speed > 0f)
                {
                    float room = leader.RouteFollower.Progress - fan.Profile.HeadwayLogical * worldUnitsPerLogicalPixel - fan.RouteFollower.Progress;
                    moveTime = Mathf.Min(dt, Mathf.Max(0f, room) / fan.RouteFollower.Speed);
                    // Arc length alone can let square frames overlap around a tight bend.
                    // Check adjacent rendered footprints too, without comparing unrelated S rows.
                    if (!HasCamionerosVisualClearance(leader, fan, moveTime))
                    {
                        float low = 0f, high = moveTime;
                        for (int sample = 0; sample < 12; sample++)
                        {
                            float middle = (low + high) * .5f;
                            if (HasCamionerosVisualClearance(leader, fan, middle)) low = middle;
                            else high = middle;
                        }
                        moveTime = low;
                    }
                }
                fan.RouteFollower.Advance(moveTime);
                if (!fan.RouteFollower.HasExited) { leader = fan; continue; }
                EscapedFans++;
                fans.RemoveAt(i--);
                if (!fan.OrderComplete)
                { HasLost = true; stopSpawning = true; Phase = TowerDefenseRunPhase.Finished; return; }
            }
        }

        private bool HasCamionerosVisualClearance(TowerDefenseFanState leader, TowerDefenseFanState fan, float moveTime)
        {
            Vector2 position = fan.RouteFollower.PositionAtProgress(fan.RouteFollower.Progress + fan.RouteFollower.Speed * moveTime);
            Vector2 delta = leader.RouteFollower.Position - position;
            float clearance = fan.Profile.HeadwayLogical * worldUnitsPerLogicalPixel;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) >= clearance;
        }

        // Preserve normalized progress where possible, but never compress a live lane below its headway.
        // Reject a viewport too short to hold the existing lane rather than overlapping or losing fans.
        private float[] CamionerosReflowProgress(float[] normalized, Vector2[] nextWaypoints, float scale)
        {
            float total = 0f;
            for (int i = 1; i < nextWaypoints.Length; i++) total += Vector2.Distance(nextWaypoints[i - 1], nextWaypoints[i]);
            var sampler = new TowerDefenseFanRouteFollower(0, nextWaypoints, 0f);
            var result = new float[fans.Count];
            int lastLive = -1;
            for (int i = 0; i < fans.Count; i++)
            {
                result[i] = normalized[i] * total;
                if (fans[i].IsDeparting) continue;
                result[i] = Mathf.Min(result[i], total - .001f * scale);
                if (lastLive >= 0)
                {
                    float clearance = fans[i].Profile.HeadwayLogical * scale;
                    result[i] = Mathf.Min(result[i], result[lastLive] - clearance);
                    if (result[i] < 0f) return null;
                    Vector2 leaderPosition = sampler.PositionAtProgress(result[lastLive]);
                    Vector2 delta = leaderPosition - sampler.PositionAtProgress(result[i]);
                    if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < clearance)
                    {
                        delta = leaderPosition - sampler.PositionAtProgress(0f);
                        if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < clearance) return null;
                        float low = 0f, high = result[i];
                        for (int sample = 0; sample < 18; sample++)
                        {
                            float middle = (low + high) * .5f;
                            delta = leaderPosition - sampler.PositionAtProgress(middle);
                            if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) >= clearance) low = middle;
                            else high = middle;
                        }
                        result[i] = low;
                    }
                }
                lastLive = i;
            }
            return result;
        }
    }
}
