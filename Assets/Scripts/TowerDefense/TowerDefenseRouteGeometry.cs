using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Serializable dimensions for a horizontal, alternating serpentine route.
    /// SegmentLengths contains one straight length per row. The first row runs
    /// left-to-right; every following row reverses direction. Rows descend from
    /// the highest Y lane to the lowest. Row spacing is
    /// measured between centerlines, TurnRadius is the lateral bulge of each cubic
    /// U-turn, and PlayableBounds is expressed in the same
    /// Unity world-space units as the returned points.
    /// </summary>
    [Serializable]
    public sealed class TowerDefenseRouteGeometryConfig
    {
        [Min(3)] public int RowCount = 3;
        public float[] SegmentLengths = { 8f, 8f, 8f };
        [Min(0.01f)] public float CorridorWidth = 2f;
        [Min(0.01f)] public float RowSpacing = 3f;
        [Min(0.01f)] public float TurnRadius = 1.1f;
        public Rect PlayableBounds = new Rect(-10f, -10f, 20f, 20f);
        [Range(4, 128)] public int TurnSegments = 12;
    }

    /// <summary>A public, inspectable report of route configuration/layout errors.</summary>
    public sealed class TowerDefenseRouteValidationResult
    {
        private readonly string[] errors;

        internal TowerDefenseRouteValidationResult(List<string> messages)
        {
            errors = messages == null ? new string[0] : messages.ToArray();
        }

        public bool IsValid => errors.Length == 0;

        /// <summary>Validation messages; empty only when the route fits its contract and bounds.</summary>
        public string[] Errors => (string[])errors.Clone();
    }

    /// <summary>
    /// Ordered centerline samples and the two independent polylines that flank it.
    /// Invalid configurations return empty geometry; a geometrically complete but
    /// out-of-bounds route retains its points so callers can inspect the failed layout.
    /// </summary>
    public sealed class TowerDefenseRouteGeometryResult
    {
        internal TowerDefenseRouteGeometryResult(
            TowerDefenseRouteValidationResult validation,
            Vector2[] centerline,
            Vector2[] leftBoundary,
            Vector2[] rightBoundary)
        {
            Validation = validation;
            Centerline = centerline ?? new Vector2[0];
            LeftBoundary = leftBoundary ?? new Vector2[0];
            RightBoundary = rightBoundary ?? new Vector2[0];
        }

        public TowerDefenseRouteValidationResult Validation { get; }
        public bool IsValid => Validation != null && Validation.IsValid;
        public Vector2[] Centerline { get; }
        public Vector2[] LeftBoundary { get; }
        public Vector2[] RightBoundary { get; }
    }

    /// <summary>
    /// Builds a continuous horizontal serpentine with sampled cubic U-turns.
    /// The route is translated as a whole to center its fence envelope in the
    /// playable rectangle; segment lengths are never scaled or silently shortened.
    /// </summary>
    public static class TowerDefenseRouteGeometry
    {
        private const float BoundsTolerance = 0.0001f;
        private const int MinimumTurnSegments = 4;
        private const int MaximumTurnSegments = 128;

        /// <summary>
        /// Checks both configuration rules and the final fence envelope against the
        /// configured bounds. Calls Generate so this result is based on the exact
        /// same sampled centerline and offsets returned to the caller.
        /// </summary>
        public static TowerDefenseRouteValidationResult Validate(TowerDefenseRouteGeometryConfig config)
        {
            return Generate(config).Validation;
        }

        /// <summary>
        /// Creates the ordered route and two offset fence rails. Supported row counts
        /// are odd counts from 3 through 9. Row zero starts at the highest Y lane and travels right;
        /// each U-turn advances one row spacing downward and reverses horizontal travel.
        /// With the supported odd row counts, the final row also travels right.
        /// </summary>
        public static TowerDefenseRouteGeometryResult Generate(TowerDefenseRouteGeometryConfig config)
        {
            List<string> errors = ValidateConfiguration(config);
            if (errors.Count > 0)
                return EmptyResult(errors);

            var centerline = new List<Vector2>();
            float firstY = config.PlayableBounds.center.y + (config.RowCount - 1) * config.RowSpacing * 0.5f;
            float currentX = 0f;
            float currentY = firstY;
            float turnRadius = config.TurnRadius;

            centerline.Add(new Vector2(currentX, currentY));
            for (int row = 0; row < config.RowCount; row++)
            {
                float direction = row % 2 == 0 ? 1f : -1f;
                currentX += direction * config.SegmentLengths[row];
                centerline.Add(new Vector2(currentX, currentY));

                if (row == config.RowCount - 1)
                    continue;

                float nextY = currentY - config.RowSpacing;
                Vector2 start = new Vector2(currentX, currentY);
                Vector2 control1 = new Vector2(currentX + direction * turnRadius, currentY);
                Vector2 control2 = new Vector2(currentX + direction * turnRadius, nextY);
                Vector2 end = new Vector2(currentX, nextY);
                for (int sample = 1; sample <= config.TurnSegments; sample++)
                {
                    float t = sample / (float)config.TurnSegments;
                    float inverse = 1f - t;
                    Vector2 point = inverse * inverse * inverse * start +
                        3f * inverse * inverse * t * control1 +
                        3f * inverse * t * t * control2 +
                        t * t * t * end;
                    centerline.Add(sample == config.TurnSegments ? end : point);
                }

                currentY = nextY;
            }

            Vector2[] centerlinePoints = centerline.ToArray();
            Vector2[] left = OffsetPolyline(centerlinePoints, config.CorridorWidth * 0.5f);
            Vector2[] right = OffsetPolyline(centerlinePoints, -config.CorridorWidth * 0.5f);

            // Center by translation only. This preserves every configured segment length.
            float minimumX = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;
            IncludeX(left, ref minimumX, ref maximumX);
            IncludeX(right, ref minimumX, ref maximumX);
            float translateX = config.PlayableBounds.center.x - (minimumX + maximumX) * 0.5f;
            TranslateX(centerlinePoints, translateX);
            TranslateX(left, translateX);
            TranslateX(right, translateX);

            AddBoundsErrorIfNeeded(config.PlayableBounds, centerlinePoints, left, right, errors);
            return new TowerDefenseRouteGeometryResult(
                new TowerDefenseRouteValidationResult(errors), centerlinePoints, left, right);
        }

        private static TowerDefenseRouteGeometryResult EmptyResult(List<string> errors)
        {
            return new TowerDefenseRouteGeometryResult(
                new TowerDefenseRouteValidationResult(errors), new Vector2[0], new Vector2[0], new Vector2[0]);
        }

        private static List<string> ValidateConfiguration(TowerDefenseRouteGeometryConfig config)
        {
            var errors = new List<string>();
            if (config == null)
            {
                errors.Add("Route configuration is null.");
                return errors;
            }

            if (config.RowCount < 3 || config.RowCount > 9 || config.RowCount % 2 == 0)
                errors.Add("RowCount must be an odd number from 3 through 9.");

            if (!IsFinite(config.CorridorWidth) || config.CorridorWidth <= 0f)
                errors.Add("CorridorWidth must be a finite value greater than zero.");

            if (!IsFinite(config.RowSpacing) || config.RowSpacing <= 0f)
                errors.Add("RowSpacing must be a finite value greater than zero.");
            else if (IsFinite(config.CorridorWidth) && config.CorridorWidth > 0f && config.RowSpacing <= config.CorridorWidth)
                errors.Add("RowSpacing must be greater than CorridorWidth so adjacent fence rails do not overlap at U-turns.");

            if (!IsFinite(config.TurnRadius) || config.TurnRadius <= 0f)
                errors.Add("TurnRadius must be a finite value greater than zero.");
            else if (IsFinite(config.CorridorWidth) && config.CorridorWidth > 0f && config.TurnRadius <= config.CorridorWidth * 0.5f)
                errors.Add("TurnRadius must exceed half the CorridorWidth to keep inner fence rails from folding across one another.");
            else if (IsFinite(config.RowSpacing) && config.RowSpacing > 0f && config.TurnRadius > config.RowSpacing * 0.5f)
                errors.Add("TurnRadius must not exceed half the RowSpacing.");

            if (config.SegmentLengths == null || config.SegmentLengths.Length != config.RowCount)
            {
                errors.Add("SegmentLengths must contain exactly one horizontal length per row.");
            }
            else
            {
                for (int i = 0; i < config.SegmentLengths.Length; i++)
                    if (!IsFinite(config.SegmentLengths[i]) || config.SegmentLengths[i] <= 0f)
                        errors.Add("SegmentLengths[" + i + "] must be a finite value greater than zero.");
            }

            if (config.TurnSegments < MinimumTurnSegments || config.TurnSegments > MaximumTurnSegments)
                errors.Add("TurnSegments must be between " + MinimumTurnSegments + " and " + MaximumTurnSegments + ".");

            Rect bounds = config.PlayableBounds;
            if (!IsFinite(bounds.x) || !IsFinite(bounds.y) || !IsFinite(bounds.width) || !IsFinite(bounds.height) ||
                !IsFinite(bounds.xMin) || !IsFinite(bounds.xMax) || !IsFinite(bounds.yMin) || !IsFinite(bounds.yMax) ||
                !IsFinite(bounds.center.x) || !IsFinite(bounds.center.y) ||
                bounds.width <= 0f || bounds.height <= 0f)
                errors.Add("PlayableBounds must have finite coordinates and positive width and height.");

            return errors;
        }

        private static Vector2[] OffsetPolyline(Vector2[] points, float distance)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                result[i] = points[i] + OffsetAt(points, i, distance);
            return result;
        }

        private static Vector2 OffsetAt(Vector2[] points, int index, float distance)
        {
            Vector2 incoming = index > 0 ? (points[index] - points[index - 1]).normalized : Vector2.zero;
            Vector2 outgoing = index < points.Length - 1 ? (points[index + 1] - points[index]).normalized : Vector2.zero;
            if (index == 0)
                return Perpendicular(outgoing) * distance;
            if (index == points.Length - 1)
                return Perpendicular(incoming) * distance;

            Vector2 incomingNormal = Perpendicular(incoming);
            Vector2 outgoingNormal = Perpendicular(outgoing);
            Vector2 miter = incomingNormal + outgoingNormal;
            if (miter.sqrMagnitude < 0.000001f)
                return outgoingNormal * distance;

            miter.Normalize();
            float projection = Vector2.Dot(miter, outgoingNormal);
            if (projection <= 0.0001f)
                return outgoingNormal * distance;

            return miter * (distance / projection);
        }

        private static Vector2 Perpendicular(Vector2 direction)
        {
            return new Vector2(-direction.y, direction.x);
        }

        private static void IncludeX(Vector2[] points, ref float minimum, ref float maximum)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].x < minimum) minimum = points[i].x;
                if (points[i].x > maximum) maximum = points[i].x;
            }
        }

        private static void TranslateX(Vector2[] points, float amount)
        {
            for (int i = 0; i < points.Length; i++)
                points[i].x += amount;
        }

        private static void AddBoundsErrorIfNeeded(Rect bounds, Vector2[] centerline, Vector2[] left, Vector2[] right, List<string> errors)
        {
            if (ContainsNonFinitePoint(centerline) || ContainsNonFinitePoint(left) || ContainsNonFinitePoint(right))
            {
                errors.Add("Generated centerline or fence rail contains a non-finite coordinate.");
                return;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            IncludeBounds(centerline, ref minX, ref maxX, ref minY, ref maxY);
            IncludeBounds(left, ref minX, ref maxX, ref minY, ref maxY);
            IncludeBounds(right, ref minX, ref maxX, ref minY, ref maxY);

            if (!IsFinite(minX) || !IsFinite(maxX) || !IsFinite(minY) || !IsFinite(maxY))
            {
                errors.Add("Generated centerline or fence rail contains a non-finite coordinate.");
                return;
            }

            if (minX < bounds.xMin - BoundsTolerance || maxX > bounds.xMax + BoundsTolerance ||
                minY < bounds.yMin - BoundsTolerance || maxY > bounds.yMax + BoundsTolerance)
            {
                errors.Add("Generated centerline or fence rail extends outside PlayableBounds; increase the bounds or adjust the configured lengths, corridor width, and row spacing. Segment lengths were not reduced.");
            }
        }

        private static bool ContainsNonFinitePoint(Vector2[] points)
        {
            for (int i = 0; i < points.Length; i++)
                if (!IsFinite(points[i].x) || !IsFinite(points[i].y))
                    return true;
            return false;
        }

        private static void IncludeBounds(Vector2[] points, ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[i];
                if (!IsFinite(point.x) || !IsFinite(point.y))
                    continue;
                if (point.x < minX) minX = point.x;
                if (point.x > maxX) maxX = point.x;
                if (point.y < minY) minY = point.y;
                if (point.y > maxY) maxY = point.y;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
