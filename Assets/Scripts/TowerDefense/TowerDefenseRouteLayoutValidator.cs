using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>One concrete layout defect: which check failed, where on the route, and the world point.</summary>
    public sealed class TowerDefenseRouteLayoutIssue
    {
        internal TowerDefenseRouteLayoutIssue(string check, string segment, Vector2 point, string message)
        {
            Check = check;
            Segment = segment;
            Point = point;
            Message = message;
        }

        public string Check { get; }
        /// <summary>Human-readable route part, e.g. "tramo 3" (horizontal run) or "giro 2" (connector).</summary>
        public string Segment { get; }
        public Vector2 Point { get; }
        public string Message { get; }
        public override string ToString() =>
            "[" + Check + "] " + Segment + " @ (" + Point.x.ToString("0.###") + ", " + Point.y.ToString("0.###") + "): " + Message;
    }

    public sealed class TowerDefenseRouteLayoutReport
    {
        private readonly List<TowerDefenseRouteLayoutIssue> issues = new List<TowerDefenseRouteLayoutIssue>();
        public IReadOnlyList<TowerDefenseRouteLayoutIssue> Issues => issues;
        public bool IsValid => issues.Count == 0;
        internal void Add(string check, string segment, Vector2 point, string message) =>
            issues.Add(new TowerDefenseRouteLayoutIssue(check, segment, point, message));

        public override string ToString()
        {
            if (issues.Count == 0) return "Route layout valid.";
            var builder = new StringBuilder();
            for (int i = 0; i < issues.Count; i++)
            {
                if (i > 0) builder.Append("; ");
                builder.Append(issues[i]);
            }
            return builder.ToString();
        }
    }

    /// <summary>Sizes (world units) that the route must admit and keep clear.</summary>
    public struct TowerDefenseRouteClearance
    {
        /// <summary>Widest fan footprint crossing a vertical connector (frame width).</summary>
        public float FanWidth;
        /// <summary>Fan body extent that must fit between the rails of a horizontal run.</summary>
        public float FanDepth;
        /// <summary>Half of the drawn fence-panel thickness around each rail line.</summary>
        public float FenceHalfThickness;
        public float Tolerance;
    }

    /// <summary>
    /// Reusable geometric validator for serpentine routes. It checks the real centerline,
    /// both rails and the follower waypoints instead of trusting that waypoints are connected:
    /// entry/exit openings, run/connector sequence and continuity, clearance from every rail,
    /// usable corridor width, accidental intersections, reserved work areas and that
    /// navigation coincides with the fenced corridor.
    /// </summary>
    public static class TowerDefenseRouteLayoutValidator
    {
        public static TowerDefenseRouteLayoutReport Validate(TowerDefenseRouteGeometryResult geometry,
            TowerDefenseRouteGeometryConfig route, TowerDefenseRouteClearance clearance,
            IReadOnlyList<Rect> reservedAreas, IReadOnlyList<Vector2> navigationWaypoints)
        {
            var report = new TowerDefenseRouteLayoutReport();
            float tol = clearance.Tolerance > 0f ? clearance.Tolerance : 0.0001f;
            if (geometry == null || route == null)
            {
                report.Add("existencia", "ruta", Vector2.zero, "No route geometry/config was supplied.");
                return report;
            }
            if (!geometry.IsValid)
                foreach (string error in geometry.Validation.Errors)
                    report.Add("generador", "ruta", Vector2.zero, error);

            Vector2[] c = geometry.Centerline, left = geometry.LeftBoundary, right = geometry.RightBoundary;
            if (c == null || c.Length < 2 || left == null || right == null || left.Length != c.Length || right.Length != c.Length)
            {
                report.Add("conexión", "ruta", Vector2.zero, "Centerline and rails must be non-empty polylines with one rail point per centerline point.");
                return report;
            }

            float horizontalCorridor = route.HorizontalCorridorWidth > 0f ? route.HorizontalCorridorWidth : route.CorridorWidth;
            float verticalCorridor = route.CorridorWidth;

            // 1-2, 6. Sequence of runs/connectors, continuity and directions.
            List<int> corners = CornerIndices(c, tol);
            int legCount = corners.Count - 1;
            int expectedLegs = route.RowCount * 2 - 1;
            if (legCount != expectedLegs)
                report.Add("tramos", "ruta", c[0], "Expected " + route.RowCount + " runs and " + (route.RowCount - 1) +
                    " connectors (" + expectedLegs + " legs) but found " + legCount + ".");
            for (int leg = 0; leg < legCount; leg++)
            {
                Vector2 a = c[corners[leg]], b = c[corners[leg + 1]];
                Vector2 d = b - a;
                bool horizontal = leg % 2 == 0;
                string name = LegName(leg);
                if (horizontal)
                {
                    float expected = (leg / 2) % 2 == 0 ? 1f : -1f;
                    if (Mathf.Abs(d.y) > tol || Mathf.Sign(d.x) != expected || Mathf.Abs(d.x) <= tol)
                        report.Add("tramos", name, a, "Run must be horizontal and travel " + (expected > 0f ? "left→right" : "right→left") + ".");
                }
                else if (Mathf.Abs(d.x) > tol || d.y >= -tol)
                    report.Add("giros", name, a, "Connector must be a straight descending segment.");
                for (int i = corners[leg] + 1; i <= corners[leg + 1]; i++)
                    if (!IsFinite(c[i]) || !IsFinite(left[i]) || !IsFinite(right[i]))
                        report.Add("conexión", name, c[i - 1], "Non-finite point disconnects the route.");
            }

            // Entry (top-left, first run) and exit (bottom row, last run going right).
            if (legCount > 0)
            {
                float topY = float.NegativeInfinity, bottomY = float.PositiveInfinity;
                for (int i = 0; i < c.Length; i++) { topY = Mathf.Max(topY, c[i].y); bottomY = Mathf.Min(bottomY, c[i].y); }
                if (Mathf.Abs(c[0].y - topY) > tol)
                    report.Add("entrada", "tramo 1", c[0], "Entry is not on the top row.");
                if (Mathf.Abs(c[c.Length - 1].y - bottomY) > tol || c[c.Length - 1].x <= c[corners[legCount - 1]].x)
                    report.Add("salida", LegName(legCount - 1), c[c.Length - 1], "Exit must end the bottom row travelling right.");
                CheckMouthIsOpen(report, "entrada", "tramo 1", left, right, 0, tol);
                CheckMouthIsOpen(report, "salida", LegName(legCount - 1), left, right, c.Length - 1, tol);
            }

            // 7, 3. Usable width of every run and connector (rail-to-rail distance).
            for (int leg = 0; leg < legCount; leg++)
            {
                int mid = (corners[leg] + corners[leg + 1]) / 2;
                bool horizontal = leg % 2 == 0;
                if (mid == corners[leg]) mid = corners[leg] + (corners[leg + 1] > corners[leg] ? 1 : 0);
                float width = horizontal ? Mathf.Abs(left[mid].y - right[mid].y) : Mathf.Abs(left[mid].x - right[mid].x);
                float required = horizontal ? clearance.FanDepth : clearance.FanWidth;
                if (width + tol < required)
                    report.Add(horizontal ? "ancho útil" : "giros", LegName(leg), c[mid],
                        "Corridor is " + width.ToString("0.###") + " wide; fans need " + required.ToString("0.###") + ".");
                float expected = horizontal ? horizontalCorridor : verticalCorridor;
                if (Mathf.Abs(width - expected) > tol * 10f)
                    report.Add("navegación=vallas", LegName(leg), c[mid], "Rails are not at the configured corridor width around the path.");
            }

            // 4, 9. Every point of the path keeps half a corridor from both rails; no leg ends against a fence.
            float minimumHalf = Mathf.Min(horizontalCorridor, verticalCorridor) * .5f;
            for (int leg = 0; leg < legCount; leg++)
            {
                Vector2 a = c[corners[leg]], b = c[corners[leg + 1]];
                int samples = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b) / Mathf.Max(tol * 100f, minimumHalf * .1f)));
                for (int s = 0; s <= samples; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)samples);
                    float distance = Mathf.Min(DistanceToPolyline(p, left), DistanceToPolyline(p, right));
                    if (distance + tol < minimumHalf)
                    {
                        report.Add("bloqueo", LegName(leg), p, "Path runs into a fence (" + distance.ToString("0.###") +
                            " from a rail, needs " + minimumHalf.ToString("0.###") + ").");
                        break;
                    }
                }
            }

            // 5. Accidental intersections: rails never cross each other or themselves; the path never crosses itself.
            CheckCrossings(report, "intersección", "riel izquierdo×riel derecho", left, right, false);
            CheckCrossings(report, "intersección", "riel izquierdo", left, left, true);
            CheckCrossings(report, "intersección", "riel derecho", right, right, true);
            CheckCrossings(report, "intersección", "eje", c, c, true);

            // 8. Reserved work areas are outside the corridor and the drawn fence art.
            if (reservedAreas != null)
                for (int area = 0; area < reservedAreas.Count; area++)
                {
                    Rect reserved = reservedAreas[area];
                    for (int leg = 0; leg < legCount; leg++)
                    {
                        Rect corridor = LegCorridor(c[corners[leg]], c[corners[leg + 1]],
                            leg % 2 == 0 ? horizontalCorridor : verticalCorridor, clearance.FenceHalfThickness);
                        if (Overlaps(reserved, corridor, tol))
                        {
                            report.Add("zona de trabajo", LegName(leg), reserved.center,
                                "Reserved area " + area + " overlaps the fenced corridor " + corridor + ".");
                        }
                    }
                    for (int other = area + 1; other < reservedAreas.Count; other++)
                        if (Overlaps(reserved, reservedAreas[other], tol))
                            report.Add("zona de trabajo", "sector " + area, reserved.center, "Overlaps sector " + other + ".");
                }

            // 9. Navigation waypoints are the centerline (plus an optional collinear entry approach).
            if (navigationWaypoints != null)
            {
                int offset = navigationWaypoints.Count - c.Length;
                if (offset < 0 || offset > 1)
                    report.Add("navegación=vallas", "ruta", c[0], "Navigation has " + navigationWaypoints.Count +
                        " waypoints for a " + c.Length + "-point centerline.");
                else
                {
                    if (offset == 1)
                    {
                        Vector2 approach = navigationWaypoints[0];
                        if (Mathf.Abs(approach.y - c[0].y) > tol || approach.x > c[0].x + tol)
                            report.Add("navegación=vallas", "entrada", approach, "Entry approach is not collinear with the first run.");
                    }
                    for (int i = 0; i < c.Length; i++)
                        if ((navigationWaypoints[i + offset] - c[i]).sqrMagnitude > tol * tol)
                        {
                            report.Add("navegación=vallas", LegNameForPoint(corners, i), navigationWaypoints[i + offset],
                                "Waypoint " + (i + offset) + " differs from the fenced centerline point " + c[i] + ".");
                            break;
                        }
                }
            }
            return report;
        }

        private static void CheckMouthIsOpen(TowerDefenseRouteLayoutReport report, string check, string segment,
            Vector2[] left, Vector2[] right, int index, float tol)
        {
            Vector2 a = left[index], b = right[index];
            // Shrink slightly so the rails that legitimately end at the mouth do not count.
            Vector2 shrinkA = Vector2.Lerp(a, b, .02f), shrinkB = Vector2.Lerp(a, b, .98f);
            if (CrossesPolyline(shrinkA, shrinkB, left) || CrossesPolyline(shrinkA, shrinkB, right))
                report.Add(check, segment, (a + b) * .5f, "A fence closes the route opening.");
        }

        private static void CheckCrossings(TowerDefenseRouteLayoutReport report, string check, string name,
            Vector2[] first, Vector2[] second, bool same)
        {
            for (int i = 1; i < first.Length; i++)
            {
                if ((first[i] - first[i - 1]).sqrMagnitude < 1e-12f) continue;
                for (int j = same ? i + 2 : 1; j < second.Length; j++)
                {
                    if ((second[j] - second[j - 1]).sqrMagnitude < 1e-12f) continue;
                    if (SegmentsIntersect(first[i - 1], first[i], second[j - 1], second[j]))
                    {
                        report.Add(check, name, first[i - 1], "Segments " + (i - 1) + " and " + (j - 1) + " intersect.");
                        return;
                    }
                }
            }
        }

        private static List<int> CornerIndices(Vector2[] points, float tol)
        {
            var corners = new List<int> { 0 };
            Vector2 direction = Vector2.zero;
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                if (delta.sqrMagnitude <= tol * tol) continue;
                Vector2 next = delta.normalized;
                if (direction != Vector2.zero && Vector2.Dot(direction, next) < .999f) corners.Add(i - 1);
                direction = next;
            }
            if (corners[corners.Count - 1] != points.Length - 1) corners.Add(points.Length - 1);
            return corners;
        }

        private static string LegName(int leg) => leg % 2 == 0 ? "tramo " + (leg / 2 + 1) : "giro " + (leg / 2 + 1);

        private static string LegNameForPoint(List<int> corners, int index)
        {
            for (int leg = 0; leg < corners.Count - 1; leg++)
                if (index <= corners[leg + 1]) return LegName(leg);
            return "ruta";
        }

        private static Rect LegCorridor(Vector2 a, Vector2 b, float corridor, float fenceHalf)
        {
            float half = corridor * .5f + fenceHalf;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - half, Mathf.Min(a.y, b.y) - half,
                Mathf.Max(a.x, b.x) + half, Mathf.Max(a.y, b.y) + half);
        }

        private static bool Overlaps(Rect a, Rect b, float tol) =>
            a.xMin < b.xMax - tol && a.xMax > b.xMin + tol && a.yMin < b.yMax - tol && a.yMax > b.yMin + tol;

        private static float DistanceToPolyline(Vector2 p, Vector2[] polyline)
        {
            float best = float.PositiveInfinity;
            for (int i = 1; i < polyline.Length; i++)
            {
                Vector2 a = polyline[i - 1], ab = polyline[i] - a;
                float t = ab.sqrMagnitude < 1e-12f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        private static bool CrossesPolyline(Vector2 a, Vector2 b, Vector2[] polyline)
        {
            for (int i = 1; i < polyline.Length; i++)
                if (SegmentsIntersect(a, b, polyline[i - 1], polyline[i])) return true;
            return false;
        }

        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(q2 - q1, p1 - q1), d2 = Cross(q2 - q1, p2 - q1);
            float d3 = Cross(p2 - p1, q1 - p1), d4 = Cross(p2 - p1, q2 - p1);
            const float eps = 1e-9f;
            if (((d1 > eps && d2 < -eps) || (d1 < -eps && d2 > eps)) && ((d3 > eps && d4 < -eps) || (d3 < -eps && d4 > eps)))
                return true;
            return (Mathf.Abs(d1) <= eps && OnSegment(q1, q2, p1)) || (Mathf.Abs(d2) <= eps && OnSegment(q1, q2, p2)) ||
                   (Mathf.Abs(d3) <= eps && OnSegment(p1, p2, q1)) || (Mathf.Abs(d4) <= eps && OnSegment(p1, p2, q2));
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool OnSegment(Vector2 a, Vector2 b, Vector2 p) =>
            p.x >= Mathf.Min(a.x, b.x) - 1e-6f && p.x <= Mathf.Max(a.x, b.x) + 1e-6f &&
            p.y >= Mathf.Min(a.y, b.y) - 1e-6f && p.y <= Mathf.Max(a.y, b.y) + 1e-6f;

        private static bool IsFinite(Vector2 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
    }
}
