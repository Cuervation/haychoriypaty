using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Serializable dimensions for a horizontal, alternating serpentine route.
    /// SegmentLengths contains one straight length per row. The first row runs
    /// left-to-right; every following row reverses direction. Rows descend from
    /// the highest Y lane to the lowest. Row spacing is measured between
    /// centerlines. The legacy TurnRadius field is retained for serialized-config
    /// compatibility but ignored by the orthogonal route; TurnSegments controls
    /// subdivisions of each straight vertical connector. PlayableBounds is expressed in the same
    /// Unity world-space units as the returned points.
    /// </summary>
    [Serializable]
    public sealed class TowerDefenseRouteGeometryConfig
    {
        [Min(3)] public int RowCount = 3;
        public float[] SegmentLengths = { 8f, 8f, 8f };
        [Min(0.01f)] public float CorridorWidth = 2f;
        [Tooltip("Optional horizontal-run width; zero uses CorridorWidth.")]
        [Min(0f)] public float HorizontalCorridorWidth;
        [Min(0.01f)] public float RowSpacing = 3f;
        [Tooltip("Legacy serialized field; orthogonal straight connectors do not use a turn radius.")]
        [Min(0f)] public float TurnRadius = 1.1f;
        public Rect PlayableBounds = new Rect(-10f, -10f, 20f, 20f);
        [Tooltip("Number of samples along each straight vertical connector; retained to preserve route-row indexing.")]
        [Range(4, 128)] public int TurnSegments = 12;
        [NonSerialized] public float FanEntryExtensionWorldUnits;
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
    /// Builds a continuous orthogonal horizontal serpentine with straight vertical connectors.
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
                Vector2 end = new Vector2(currentX, nextY);
                for (int sample = 1; sample <= config.TurnSegments; sample++)
                {
                    float t = sample / (float)config.TurnSegments;
                    // Keep each connector on one vertical line; samples exist only
                    // for the existing route-row indexing contract.
                    centerline.Add(sample == config.TurnSegments ? end : Vector2.Lerp(start, end, t));
                }

                currentY = nextY;
            }

            Vector2[] centerlinePoints = centerline.ToArray();
            float horizontalWidth = config.HorizontalCorridorWidth > 0f ? config.HorizontalCorridorWidth : config.CorridorWidth;
            Vector2[] left = OffsetPolyline(centerlinePoints, config.CorridorWidth * 0.5f, horizontalWidth * 0.5f);
            Vector2[] right = OffsetPolyline(centerlinePoints, -config.CorridorWidth * 0.5f, -horizontalWidth * 0.5f);

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

            if (!IsFinite(config.HorizontalCorridorWidth) || config.HorizontalCorridorWidth < 0f)
                errors.Add("HorizontalCorridorWidth must be finite and nonnegative (zero uses CorridorWidth).");
            if (config.HorizontalCorridorWidth > 0f && config.HorizontalCorridorWidth >= config.RowSpacing)
                errors.Add("RowSpacing must be greater than HorizontalCorridorWidth.");

            if (!IsFinite(config.RowSpacing) || config.RowSpacing <= 0f)
                errors.Add("RowSpacing must be a finite value greater than zero.");
            else if (IsFinite(config.CorridorWidth) && config.CorridorWidth > 0f && config.RowSpacing <= config.CorridorWidth)
                errors.Add("RowSpacing must be greater than CorridorWidth so adjacent fence rails do not overlap at U-turns.");

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

        private static Vector2[] OffsetPolyline(Vector2[] points, float verticalDistance, float horizontalDistance)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 incoming = i > 0 ? (points[i] - points[i - 1]).normalized : Vector2.zero;
                Vector2 outgoing = i < points.Length - 1 ? (points[i + 1] - points[i]).normalized : Vector2.zero;
                Vector2 a = OffsetNormal(incoming, verticalDistance, horizontalDistance);
                Vector2 b = OffsetNormal(outgoing, verticalDistance, horizontalDistance);
                if (i == 0) result[i] = points[i] + b;
                else if (i == points.Length - 1) result[i] = points[i] + a;
                else if (Mathf.Abs(incoming.x * outgoing.y - incoming.y * outgoing.x) < .001f)
                    result[i] = points[i] + b;
                else result[i] = points[i] + a + b;
            }

            // Centerline turn samples can lie above the offset corner when the
            // corridor is wider than a single sample. Redistribute the samples
            // between the actual corner intersections so rails never double back.
            for (int start = 0; start < points.Length - 1;)
            {
                Vector2 delta = points[start + 1] - points[start];
                if (Mathf.Abs(delta.x) > .0001f || Mathf.Abs(delta.y) < .0001f) { start++; continue; }
                int end = start + 1;
                while (end < points.Length - 1 &&
                       Mathf.Abs(points[end + 1].x - points[end].x) < .0001f &&
                       Mathf.Abs(points[end + 1].y - points[end].y) > .0001f) end++;
                Vector2 first = result[start], last = result[end];
                for (int i = start + 1; i < end; i++)
                    result[i] = Vector2.Lerp(first, last, (i - start) / (float)(end - start));
                start = end;
            }
            return result;
        }

        private static Vector2 OffsetNormal(Vector2 direction, float verticalDistance, float horizontalDistance)
        {
            return Perpendicular(direction) * (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? horizontalDistance : verticalDistance);
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
