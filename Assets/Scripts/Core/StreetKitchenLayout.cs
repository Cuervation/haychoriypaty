using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Catalog-derived kitchen geometry shared by rendering, pickup points, and worker routes.</summary>
    public sealed class StreetKitchenLayout
    {
        public const float SourceWidth = 760f;
        public const float Scale = StreetSceneLayout.Width / SourceWidth;
        public const float TableY = 466f;
        public const float BarrelY = 466f;
        public const float WorkerLaneY = 450f;
        public const float TablePickupY = 502f;
        public const float GrillPickupY = 554f;
        public const float BarrelPickupY = 454f;
        public const float TableWidth = 196f * Scale, TableHeight = 98f * Scale;
        public const float BarrelAuthoredWidth = 72f, BarrelAuthoredHeight = 117.6f;
        public const float BarrelWidth = BarrelAuthoredWidth * Scale, BarrelHeight = BarrelAuthoredHeight * Scale;
        public const float StandardGrillWidth = 352f * Scale, StandardGrillHeight = 117.3333f * Scale;
        public const float GrillBottomLimit = 660f;
        private const float GrillSurfaceTop = .115f, GrillSurfaceBottom = .46f;
        private const float GrillBackLeft = .145f, GrillBackRight = .855f;
        private const float GrillFrontLeft = .035f, GrillFrontRight = .965f;

        private readonly bool hasNormal, hasPremium, hasFernet, hasCoca, hasBeer;
        private readonly Rect normalTable, premiumTable, fernetTable, cocaBarrel, beerBarrel;
        private readonly Rect normalGrill, premiumGrill;
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
            // Keep both standard grills below the active prep row and entirely above the lower UI field.
            float grillY = upperBottom + 12f;
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

        /// <summary>Walk via the clear upper lane, then approach the service edge without crossing a station base.</summary>
        public Vector2[] ApproachRoute(int product)
        {
            Vector2 pickup = PickupPosition(product);
            float approachY = product < 4 || product == 5 ? TablePickupY : BarrelPickupY;
            Vector2 lane = new Vector2(pickup.x, WorkerLaneY);
            Vector2 approach = new Vector2(pickup.x, approachY);
            var result = new List<Vector2> { new Vector2(StreetSceneLayout.Width * .5f, WorkerLaneY) };
            if (Mathf.Abs(result[0].x - lane.x) > 1f) result.Add(lane);
            if (Mathf.Abs(lane.y - approach.y) > 1f) result.Add(approach);
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
