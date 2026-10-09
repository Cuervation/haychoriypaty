using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Catalog-derived kitchen geometry shared by rendering, pickup points, and worker routes.</summary>
    public sealed class StreetKitchenLayout
    {
        public const float SourceWidth = 760f;
        public const float Scale = StreetSceneLayout.Width / SourceWidth;
        public const float TiledPlayfieldBottom = 665f;
        public const float GrillBottomClearance = 5f;
        public const float GrillVerticalGap = 4f;
        public const float TableWidth = 196f * Scale, TableHeight = 98f * Scale;
        public const float BarrelAuthoredWidth = 72f, BarrelAuthoredHeight = 117.6f;
        public const float BarrelWidth = BarrelAuthoredWidth * Scale, BarrelHeight = BarrelAuthoredHeight * Scale;
        public const float StandardGrillWidth = 352f * Scale, StandardGrillHeight = 117.3333f * Scale;
        public const float TableY = TiledPlayfieldBottom - GrillBottomClearance - StandardGrillHeight - TableHeight - GrillVerticalGap;
        // Align taller barrels to table bottoms so the complete row shares its grill clearance.
        public const float BarrelY = TableY + TableHeight - BarrelHeight;
        public const float WorkerLaneY = 450f;
        public const float TablePickupY = TableY + 36f;
        public const float GrillPickupY = TableY + TableHeight + GrillVerticalGap + 14.4f;
        public const float BarrelPickupY = BarrelY - 12f;
        public const float GrillBottomLimit = TiledPlayfieldBottom - GrillBottomClearance;
        private const float GrillSurfaceTop = .115f, GrillSurfaceBottom = .46f;
        private const float GrillBackLeft = .145f, GrillBackRight = .855f;
        private const float GrillFrontLeft = .035f, GrillFrontRight = .965f;

        private readonly bool hasNormal, hasPremium, hasFernet, hasCoca, hasBeer;
        private readonly Rect normalTable, premiumTable, fernetTable, cocaBarrel, beerBarrel;
        private readonly Rect normalGrill, premiumGrill;
        // Route obstacles describe the actual solid bases expanded by the worker's 36x12 foot collider.
        private readonly List<Rect> routeObstacles = new List<Rect>();
        private readonly List<Vector2> routeVertices = new List<Vector2>();
        private const float RouteClearance = 0f;
        private const float RouteVertexOffset = .05f;
        private static readonly Vector2[] BarrelDrinkScatter =
        {
            new Vector2(.18f, .055f), new Vector2(.31f, .085f), new Vector2(.44f, .05f),
            new Vector2(.57f, .095f), new Vector2(.70f, .06f), new Vector2(.82f, .085f),
            new Vector2(.22f, .15f), new Vector2(.35f, .18f), new Vector2(.48f, .145f),
            new Vector2(.61f, .18f), new Vector2(.74f, .15f), new Vector2(.84f, .17f)
        };
        private static readonly float[] BarrelDrinkRotations = { -18f, 64f, 6f, -72f, 28f, 95f, -38f, 12f, 72f, -5f, 106f, 43f };

        public bool HasNormalGrill { get { return hasNormal; } }
        public bool HasPremiumGrill { get { return hasPremium; } }
        public bool HasNormalTable { get { return hasNormal; } }
        public bool HasPremiumTable { get { return hasPremium; } }
        public bool HasFernetTable { get { return hasFernet; } }
        public bool HasCocaBarrel { get { return hasCoca; } }
        public bool HasBeerBarrel { get { return hasBeer; } }
        public Rect NormalGrillBounds { get { return normalGrill; } }
        public Rect PremiumGrillBounds { get { return premiumGrill; } }
        public Rect NormalTableBounds { get { return normalTable; } }
        public Rect PremiumTableBounds { get { return premiumTable; } }
        public Rect FernetTableBounds { get { return fernetTable; } }
        public Rect BeerBarrelBounds { get { return beerBarrel; } }
        public Rect CocaBarrelBounds { get { return cocaBarrel; } }

        public StreetKitchenLayout(int[] availableProducts)
        {
            bool[] available = new bool[7];
            if (availableProducts != null)
                for (int i = 0; i < availableProducts.Length; i++)
                    if (availableProducts[i] >= 0 && availableProducts[i] < available.Length) available[availableProducts[i]] = true;

            hasNormal = available[0] || available[1];
            hasPremium = available[2] || available[3];
            hasCoca = available[4];
            hasFernet = available[5];
            hasBeer = available[6];
            Rect normal, premium, fernet, coca, beer;
            PackUpperRow(out normal, out premium, out fernet, out coca, out beer);
            normalTable = normal; premiumTable = premium; fernetTable = fernet; cocaBarrel = coca; beerBarrel = beer;
            Rect normalG, premiumG;
            PackGrills(out normalG, out premiumG);
            normalGrill = normalG; premiumGrill = premiumG;
            BuildRouteGeometry(available);
        }

        private void PackUpperRow(out Rect normal, out Rect premium, out Rect fernet, out Rect coca, out Rect beer)
        {
            int count = (hasNormal ? 1 : 0) + (hasPremium ? 1 : 0) + (hasFernet ? 1 : 0) + (hasCoca ? 1 : 0) + (hasBeer ? 1 : 0);
            float totalWidth = ((hasNormal ? 196f : 0f) + (hasPremium ? 196f : 0f) + (hasFernet ? 196f : 0f)
                + (hasCoca ? BarrelAuthoredWidth : 0f) + (hasBeer ? BarrelAuthoredWidth : 0f));
            float gap = count > 0 ? (SourceWidth - totalWidth) / (count + 1) : 0f;
            float x = gap;
            normal = PlaceUpper(ref x, gap, hasNormal, 196f);
            premium = PlaceUpper(ref x, gap, hasPremium, 196f);
            fernet = PlaceUpper(ref x, gap, hasFernet, 196f);
            coca = PlaceUpper(ref x, gap, hasCoca, BarrelAuthoredWidth);
            beer = PlaceUpper(ref x, gap, hasBeer, BarrelAuthoredWidth);
        }

        private static Rect PlaceUpper(ref float x, float gap, bool active, float nativeWidth)
        {
            if (!active) return Rect.zero;
            float scaledWidth = nativeWidth * Scale;
            Rect result = new Rect(x * Scale, nativeWidth == 196f ? TableY : BarrelY, scaledWidth, nativeWidth == 196f ? TableHeight : BarrelHeight);
            x += nativeWidth + gap;
            return result;
        }

        private void PackGrills(out Rect normal, out Rect premium)
        {
            bool active = hasNormal || hasPremium;
            if (!active) { normal = premium = Rect.zero; return; }

            float upperBottom = TableY;
            if (hasNormal) upperBottom = Mathf.Max(upperBottom, normalTable.yMax);
            if (hasPremium) upperBottom = Mathf.Max(upperBottom, premiumTable.yMax);
            if (hasFernet) upperBottom = Mathf.Max(upperBottom, fernetTable.yMax);
            if (hasCoca) upperBottom = Mathf.Max(upperBottom, cocaBarrel.yMax);
            if (hasBeer) upperBottom = Mathf.Max(upperBottom, beerBarrel.yMax);
            // Keep the entire station block between the requested four-point upper and two-point lower margins.
            float grillY = upperBottom + GrillVerticalGap;
            float nativeWidth = 352f;
            if (hasNormal && hasPremium)
            {
                normal = new Rect(14f * Scale, grillY, StandardGrillWidth, StandardGrillHeight);
                premium = new Rect(394f * Scale, grillY, StandardGrillWidth, StandardGrillHeight);
                return;
            }
            float availableHeight = Mathf.Max(0f, GrillBottomLimit - grillY);
            nativeWidth = Mathf.Min(SourceWidth - 28f, availableHeight / Scale * 3f);
            float width = nativeWidth * Scale;
            float height = width / 3f;
            float x = (StreetSceneLayout.Width - width) * .5f;
            Rect only = new Rect(x, grillY, width, height);
            if (hasNormal) { normal = only; premium = Rect.zero; }
            else { normal = Rect.zero; premium = only; }
        }

        public Rect BoundsForProduct(int product)
        {
            switch (product)
            {
                case 0: case 1: return normalTable;
                case 2: case 3: return premiumTable;
                case 4: return cocaBarrel;
                case 5: return fernetTable;
                case 6: return beerBarrel;
                default: return Rect.zero;
            }
        }

        public Rect GrillBoundsForProduct(int product) => product < 2 ? normalGrill : product < 4 ? premiumGrill : Rect.zero;

        /// <summary>Solid base/front collision only. The tabletop/food overhang remains reachable.</summary>
        public Rect FootprintForProduct(int product)
        {
            Rect station = BoundsForProduct(product);
            if (station.width <= 0f) return Rect.zero;
            if (product == 0 || product == 1 || product == 2 || product == 3 || product == 5)
                return new Rect(station.x, station.yMax - 20f * Scale, station.width, 20f * Scale);
            return station;
        }

        public Vector2 PickupPosition(int product)
        {
            Rect r = BoundsForProduct(product);
            float y = product < 4 || product == 5 ? TablePickupY : BarrelPickupY;
            float safeX = Mathf.Clamp(r.center.x, StreetWorkstationLayout.WorkerHalfWidth, StreetSceneLayout.Width - StreetWorkstationLayout.WorkerHalfWidth);
            return new Vector2(safeX, y);
        }

        /// <summary>Shortest safe route from the initial counter position to this product's pickup.</summary>
        public Vector2[] ApproachRoute(int product)
        {
            return FindShortestSafeRoute(new Vector2(433f, StreetSceneLayout.WorkerServiceY), PickupPosition(product));
        }

        /// <summary>Returns waypoints excluding start and including destination; return paths are queried independently.</summary>
        public Vector2[] FindShortestSafeRoute(Vector2 start, Vector2 destination)
        {
            if (start == destination) return new Vector2[0];
            if (IsRouteSegmentClear(start, destination)) return new[] { destination };

            var points = new List<Vector2>(routeVertices.Count + 2) { start };
            points.AddRange(routeVertices);
            points.Add(destination);
            int last = points.Count - 1;
            var distances = new float[points.Count];
            var previous = new int[points.Count];
            var visited = new bool[points.Count];
            for (int i = 0; i < distances.Length; i++) { distances[i] = float.PositiveInfinity; previous[i] = -1; }
            distances[0] = 0f;

            for (int iteration = 0; iteration < points.Count; iteration++)
            {
                int current = -1;
                float best = float.PositiveInfinity;
                for (int i = 0; i < points.Count; i++)
                    if (!visited[i] && distances[i] < best) { current = i; best = distances[i]; }
                if (current < 0 || current == last) break;
                visited[current] = true;
                for (int next = 1; next < points.Count; next++)
                {
                    if (visited[next] || next == current || !IsRouteSegmentClear(points[current], points[next])) continue;
                    float candidate = best + Vector2.Distance(points[current], points[next]);
                    if (candidate + .0001f < distances[next]) { distances[next] = candidate; previous[next] = current; }
                }
            }

            if (previous[last] < 0) return null;
            var reversed = new List<Vector2>();
            for (int at = last; at > 0; at = previous[at])
            {
                if (at < 0) return null;
                reversed.Add(points[at]);
            }
            reversed.Reverse();
            return SimplifyRoute(reversed);
        }

        /// <summary>Checks the complete swept feet-center segment against all active solid bases.</summary>
        public bool IsRouteSegmentClear(Vector2 start, Vector2 end)
        {
            float halfWidth = StreetWorkstationLayout.FootHalfWidth;
            if (start.x < halfWidth || start.x > StreetSceneLayout.Width - halfWidth ||
                end.x < halfWidth || end.x > StreetSceneLayout.Width - halfWidth ||
                start.y < 12f || start.y > StreetSceneLayout.Height ||
                end.y < 12f || end.y > StreetSceneLayout.Height) return false;
            for (int i = 0; i < routeObstacles.Count; i++)
                if (SegmentEntersOpenRect(start, end, routeObstacles[i])) return false;
            return true;
        }

        private void BuildRouteGeometry(bool[] available)
        {
            var solidBases = new List<Rect>();
            for (int product = 0; product < available.Length; product++)
            {
                if (!available[product]) continue;
                Rect footprint = FootprintForProduct(product);
                if (footprint.width > 0f && !solidBases.Contains(footprint)) solidBases.Add(footprint);
            }
            if (hasNormal && normalGrill.width > 0f) solidBases.Add(normalGrill);
            if (hasPremium && premiumGrill.width > 0f) solidBases.Add(premiumGrill);
            for (int i = 0; i < solidBases.Count; i++)
            {
                Rect solid = solidBases[i];
                routeObstacles.Add(new Rect(
                    solid.xMin - StreetWorkstationLayout.FootHalfWidth - RouteClearance,
                    solid.yMin - RouteClearance,
                    solid.width + StreetWorkstationLayout.FootHalfWidth * 2f + RouteClearance * 2f,
                    solid.height + 12f + RouteClearance * 2f));
            }
            BuildVisibilityVertices();
        }

        // A shortest path around axis-aligned solids only turns at exposed convex vertices of their union.
        private void BuildVisibilityVertices()
        {
            var xs = new List<float>();
            var ys = new List<float>();
            for (int i = 0; i < routeObstacles.Count; i++)
            {
                AddUnique(xs, routeObstacles[i].xMin); AddUnique(xs, routeObstacles[i].xMax);
                AddUnique(ys, routeObstacles[i].yMin); AddUnique(ys, routeObstacles[i].yMax);
            }
            xs.Sort(); ys.Sort();
            const float sample = .01f;
            for (int xi = 0; xi < xs.Count; xi++)
            for (int yi = 0; yi < ys.Count; yi++)
            {
                float x = xs[xi], y = ys[yi];
                int occupied = 0, blockedX = 0, blockedY = 0;
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    if (IsInsideObstacle(new Vector2(x + sx * sample, y + sy * sample)))
                    { occupied++; blockedX = sx; blockedY = sy; }
                if (occupied != 1) continue;
                Vector2 vertex = new Vector2(x - blockedX * RouteVertexOffset, y - blockedY * RouteVertexOffset);
                if (!IsRoutePointClear(vertex)) continue;
                bool duplicate = false;
                for (int i = 0; i < routeVertices.Count; i++)
                    if ((routeVertices[i] - vertex).sqrMagnitude < .0001f) { duplicate = true; break; }
                if (!duplicate) routeVertices.Add(vertex);
            }
        }

        private static void AddUnique(List<float> values, float value)
        {
            for (int i = 0; i < values.Count; i++) if (Mathf.Abs(values[i] - value) < .001f) return;
            values.Add(value);
        }

        private bool IsInsideObstacle(Vector2 point)
        {
            for (int i = 0; i < routeObstacles.Count; i++)
                if (point.x > routeObstacles[i].xMin && point.x < routeObstacles[i].xMax &&
                    point.y > routeObstacles[i].yMin && point.y < routeObstacles[i].yMax) return true;
            return false;
        }

        private bool IsRoutePointClear(Vector2 point) =>
            point.x >= StreetWorkstationLayout.FootHalfWidth && point.x <= StreetSceneLayout.Width - StreetWorkstationLayout.FootHalfWidth &&
            point.y >= 12f && point.y <= StreetSceneLayout.Height && !IsInsideObstacle(point);

        private static bool SegmentEntersOpenRect(Vector2 start, Vector2 end, Rect rect)
        {
            float minT = 0f, maxT = 1f;
            if (!ClipSegmentAxis(start.x, end.x - start.x, rect.xMin, rect.xMax, ref minT, ref maxT) ||
                !ClipSegmentAxis(start.y, end.y - start.y, rect.yMin, rect.yMax, ref minT, ref maxT)) return false;
            return maxT - minT > .000001f && maxT > 0f && minT < 1f;
        }

        private static bool ClipSegmentAxis(float origin, float delta, float min, float max, ref float minT, ref float maxT)
        {
            if (Mathf.Abs(delta) < .000001f) return origin > min && origin < max;
            float first = (min - origin) / delta, second = (max - origin) / delta;
            if (first > second) { float swap = first; first = second; second = swap; }
            minT = Mathf.Max(minT, first); maxT = Mathf.Min(maxT, second);
            return maxT > minT;
        }

        private static Vector2[] SimplifyRoute(List<Vector2> route)
        {
            if (route.Count < 3) return route.ToArray();
            var result = new List<Vector2>(route.Count);
            for (int i = 0; i < route.Count; i++)
            {
                while (result.Count >= 2)
                {
                    Vector2 a = result[result.Count - 2], b = result[result.Count - 1], c = route[i];
                    if (Mathf.Abs((b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x)) > .001f) break;
                    result.RemoveAt(result.Count - 1);
                }
                result.Add(route[i]);
            }
            return result.ToArray();
        }

        public Vector2 TableSlotPosition(int product, int slot) => TableSlotPosition(product, slot, true, product == 5 ? 45 : 24);

        public Vector2 TableSlotPosition(int product, int slot, bool companionAvailable, int capacity)
        {
            Rect r = BoundsForProduct(product);
            int index = Mathf.Max(0, slot);
            float woodTopDepth = r.height * .35f;
            if (product == 5)
            {
                int item = index % 15, layer = index / 15;
                int col = item % 5, row = item / 5;
                float x = r.x + r.width * (.10f + .20f * col);
                float baseY = r.y + woodTopDepth * (.28f + .28f * row);
                float dx = layer == 1 ? (col % 2 == 0 ? 2f : -2f) : layer == 2 ? (col % 2 == 0 ? -2f : 2f) : 0f;
                float dy = layer * 1.5f;
                return new Vector2(x + dx, baseY - 16f + dy);
            }

            int columns = companionAvailable ? 3 : 6;
            float left = r.x + 5f * Scale;
            float usableWidth = r.width - 10f * Scale;
            if (companionAvailable)
            {
                usableWidth *= .5f;
                if (product == 1 || product == 3) left += usableWidth + 5f * Scale;
            }
            int perLayer = companionAvailable ? (product < 2 ? 12 : 9) : Mathf.Max(columns, capacity / 2);
            int rows = Mathf.Max(1, Mathf.CeilToInt(perLayer / (float)columns));
            int layerIndex = index / perLayer, itemIndex = index % perLayer;
            int itemCol = itemIndex % columns, itemRow = itemIndex / columns;
            float xStep = usableWidth / columns;
            float xPosition = left + xStep * (itemCol + .5f);
            float rowBase = woodTopDepth * (.15f + .75f * (itemRow + .5f) / rows);
            float layerX = layerIndex == 0 ? 0f : (product % 2 == 0 ? 1.5f : -1.5f) * layerIndex;
            float layerY = layerIndex * 1.75f;
            return new Vector2(xPosition + layerX, r.y + rowBase - 7f + layerY);
        }

        public Vector2 GrillSlotPosition(int product, int slot) => GrillSlotPosition(product, slot, true, product < 2 ? 12 : 4);

        /// <summary>Visual meat footprint adapted to the actual surface of the scaled grill.</summary>
        public Vector2 GrillMeatSize(int product, bool companionAvailable)
        {
            Rect grill = GrillBoundsForProduct(product);
            if (product == 0 && !companionAvailable)
            {
                float width = Mathf.Min(62f, Mathf.Max(36.4f, grill.width * .12f));
                return new Vector2(width, width * 14f / 36.4f);
            }
            if (product == 2)
            {
                float height = Mathf.Min(38f, grill.height * (GrillSurfaceBottom - GrillSurfaceTop));
                return new Vector2(34f * height / 38f, height);
            }
            if (product == 3 && companionAvailable)
            {
                float width = Mathf.Min(90f, grill.width * .27f);
                return new Vector2(width, 14f * width / 90f);
            }
            return product == 0 ? new Vector2(36.4f, 14f)
                : product == 1 ? new Vector2(34f, 20f)
                : product == 2 ? new Vector2(34f, 38f)
                : new Vector2(90f, 14f);
        }

        public Vector2 GrillSlotPosition(int product, int slot, bool companionAvailable, int capacity)
        {
            Rect r = GrillBoundsForProduct(product);
            int index = Mathf.Max(0, slot);
            Vector2 meatSize = GrillMeatSize(product, companionAvailable);
            int columns, rows, col, row;
            if (companionAvailable && product < 2)
            {
                columns = 3;
                rows = product == 0 ? 4 : 2;
                col = index % columns; row = (index / columns) % rows;
            }
            else if (companionAvailable && product >= 2)
            {
                if (product == 2)
                {
                    columns = 4; rows = 1;
                }
                else { columns = 1; rows = Mathf.Max(1, capacity); }
                col = index % columns; row = index / columns;
            }
            else
            {
                columns = product == 3 ? Mathf.Min(2, Mathf.Max(1, capacity))
                    : product == 2 ? Mathf.Max(1, capacity) : 6;
                rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, capacity) / (float)columns));
                col = index % columns; row = index / columns;
            }

            float surfaceTop = r.y + r.height * GrillSurfaceTop;
            float surfaceBottom = r.y + r.height * GrillSurfaceBottom;
            float firstY = surfaceTop + meatSize.y * .5f;
            float lastY = surfaceBottom - meatSize.y * .5f;
            float y = rows <= 1 || lastY <= firstY ? (firstY + lastY) * .5f
                : Mathf.Lerp(firstY, lastY, row / (float)(rows - 1));

            // The rendered grill is perspective-trapezoidal: narrow at the rear and wider at the front.
            // Use the narrower edge across the meat's full depth so no slot can hang over the grate sides.
            float topT = Mathf.Clamp01((y - meatSize.y * .5f - surfaceTop) / (surfaceBottom - surfaceTop));
            float bottomT = Mathf.Clamp01((y + meatSize.y * .5f - surfaceTop) / (surfaceBottom - surfaceTop));
            float leftEdge = r.x + r.width * Mathf.Max(LeftEdgeAt(topT), LeftEdgeAt(bottomT));
            float rightEdge = r.x + r.width * Mathf.Min(RightEdgeAt(topT), RightEdgeAt(bottomT));
            float zoneStart = companionAvailable
                ? product == 1 ? .5f : product == 3 ? .6f : 0f
                : 0f;
            float zoneEnd = companionAvailable
                ? product == 0 ? .5f : product == 2 ? .6f : 1f
                : 1f;
            float zonePadding = companionAvailable && (product == 0 || product == 1 || product == 2 || product == 3) ? 1f : 0f;
            float zoneLeft = Mathf.Lerp(leftEdge, rightEdge, zoneStart) + meatSize.x * .5f
                + (companionAvailable && (product == 1 || product == 3) ? zonePadding : 0f);
            float zoneRight = Mathf.Lerp(leftEdge, rightEdge, zoneEnd) - meatSize.x * .5f
                - (companionAvailable && (product == 0 || product == 2) ? zonePadding : 0f);
            float x = columns <= 1 || zoneRight <= zoneLeft ? (zoneLeft + zoneRight) * .5f
                : Mathf.Lerp(zoneLeft, zoneRight, col / (float)(columns - 1));
            return new Vector2(x, y);
        }

        private static float LeftEdgeAt(float depth) => Mathf.Lerp(GrillBackLeft, GrillFrontLeft, depth);
        private static float RightEdgeAt(float depth) => Mathf.Lerp(GrillBackRight, GrillFrontRight, depth);

        public Vector2 BarrelSlotPosition(int product, int slot)
        {
            Rect r = BoundsForProduct(product);
            Vector2 normalized = BarrelDrinkScatter[Mathf.Max(0, slot) % BarrelDrinkScatter.Length];
            return new Vector2(r.x + r.width * normalized.x, r.y + r.height * normalized.y);
        }

        /// <summary>Stable loose-pile orientation for each real beverage slot; never changes inventory.</summary>
        public float BarrelSlotRotation(int slot) => BarrelDrinkRotations[Mathf.Max(0, slot) % BarrelDrinkRotations.Length];
    }
}
