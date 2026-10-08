using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Responsive native-kitchen geometry in StreetView logical canvas units.
    /// The 760-unit source row is fitted uniformly into the existing 540-wide world;
    /// y anchors remain canvas anchors and only their vertical presentation transform changes.</summary>
    public static class StreetKitchenLayout
    {
        public const float SourceWidth = 760f;
        public const float Scale = StreetSceneLayout.Width / SourceWidth;
        public const float TableY = 520f;
        public const float GrillY = 620f;
        public const float BarrelY = 520f;
        public const float WorkerLaneY = 450f;
        public const float TablePickupY = 556f;
        public const float GrillPickupY = 608f;
        public const float BarrelPickupY = 508f;
        public const float TableWidth = 196f * Scale, TableHeight = 98f * Scale;
        public const float GrillWidth = 352f * Scale, GrillHeight = 117.3333f * Scale;
        public const float BarrelWidth = 50f * Scale, BarrelHeight = 98f * Scale;

        // Preserve five-slot ordering even when a product is not unlocked. Locked stations remain empty.
        private static readonly Rect Beer = new Rect(14f * Scale, BarrelY, BarrelWidth, BarrelHeight);
        private static readonly Rect Coca = new Rect(76f * Scale, BarrelY, BarrelWidth, BarrelHeight);
        private static readonly Rect NormalTable = new Rect(138f * Scale, TableY, TableWidth, TableHeight);
        private static readonly Rect PremiumTable = new Rect(346f * Scale, TableY, TableWidth, TableHeight);
        private static readonly Rect FernetTable = new Rect(554f * Scale, TableY, TableWidth, TableHeight);
        private static readonly Rect NormalGrill = new Rect(14f * Scale, GrillY, GrillWidth, GrillHeight);
        private static readonly Rect PremiumGrill = new Rect(394f * Scale, GrillY, GrillWidth, GrillHeight);

        public static Rect BoundsForProduct(int product)
        {
            switch (product)
            {
                case 0: case 1: return NormalTable;
                case 2: case 3: return PremiumTable;
                case 4: return Coca;
                case 5: return FernetTable;
                case 6: return Beer;
                default: return Rect.zero;
            }
        }

        public static Rect GrillBoundsForProduct(int product) => product < 2 ? NormalGrill
            : product < 4 ? PremiumGrill : Rect.zero;
        public static Rect NormalGrillBounds => NormalGrill;
        public static Rect PremiumGrillBounds => PremiumGrill;
        public static Rect NormalTableBounds => NormalTable;
        public static Rect PremiumTableBounds => PremiumTable;
        public static Rect FernetTableBounds => FernetTable;
        public static Rect BeerBarrelBounds => Beer;
        public static Rect CocaBarrelBounds => Coca;

        /// <summary>Solid base/front collision only. The tabletop/food overhang remains reachable.</summary>
        public static Rect FootprintForProduct(int product)
        {
            Rect station = BoundsForProduct(product);
            if (product == 0 || product == 1 || product == 2 || product == 3 || product == 5)
                return new Rect(station.x, station.yMax - 20f * Scale, station.width, 20f * Scale);
            return station;
        }

        public static Vector2 PickupPosition(int product)
        {
            Rect r = BoundsForProduct(product);
            float y = product < 4 || product == 5 ? TablePickupY : BarrelPickupY;
            float safeX = Mathf.Clamp(r.center.x, StreetWorkstationLayout.WorkerHalfWidth, StreetSceneLayout.Width - StreetWorkstationLayout.WorkerHalfWidth);
            return new Vector2(safeX, y);
        }

        /// <summary>Walk via the clear upper lane, then approach the service edge without crossing a station base.</summary>
        public static Vector2[] ApproachRoute(int product)
        {
            Vector2 pickup = PickupPosition(product);
            float approachY = product < 4 || product == 5 ? TablePickupY : BarrelPickupY;
            Vector2 lane = new Vector2(pickup.x, WorkerLaneY);
            Vector2 approach = new Vector2(pickup.x, approachY);
            // Keep paths out of the narrow inter-station gaps: travel in the common upper lane,
            // then descend at the station center to the pickup edge.
            var result = new List<Vector2> { new Vector2(StreetSceneLayout.Width * .5f, WorkerLaneY) };
            if (Mathf.Abs(result[0].x - lane.x) > 1f) result.Add(lane);
            if (Mathf.Abs(lane.y - approach.y) > 1f) result.Add(approach);
            return result.ToArray();
        }

        public static Vector2 TableSlotPosition(int product, int slot) => TableSlotPosition(product, slot, true, product == 5 ? 45 : 24);

        public static Vector2 TableSlotPosition(int product, int slot, bool companionAvailable, int capacity)
        {
            Rect r = BoundsForProduct(product);
            int index = Mathf.Max(0, slot);
            float woodTopDepth = r.height * .35f;
            if (product == 5)
            {
                // 15 large glasses, repeated in three subtle product layers.
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

        public static Vector2 GrillSlotPosition(int product, int slot) => GrillSlotPosition(product, slot, true, product < 2 ? 12 : 4);

        public static Vector2 GrillSlotPosition(int product, int slot, bool companionAvailable, int capacity)
        {
            Rect r = GrillBoundsForProduct(product);
            int index = Mathf.Max(0, slot);
            float margin = 8f * Scale;
            float usableWidth = r.width - 2f * margin;
            float grateTop = r.y + r.height * .08f;
            float grateHeight = r.height * .46f;
            int columns, rows, col, row;
            float left = r.x + margin, width = usableWidth;
            if (companionAvailable && product < 2)
            {
                width = usableWidth * .5f;
                if (product == 1) left += width;
                columns = 3;
                rows = product == 0 ? 4 : 2;
                col = index % columns; row = (index / columns) % rows;
                return new Vector2(left + width * (col + .5f) / columns, grateTop + grateHeight * (row + .5f) / rows);
            }
            if (companionAvailable)
            {
                if (product == 2)
                {
                    width = usableWidth * .60f; columns = 4; rows = 1;
                    col = index % columns;
                    return new Vector2(left + width * (col + .5f) / columns, grateTop + grateHeight * .48f);
                }
                left += usableWidth * .60f; width = usableWidth * .40f; columns = 1; rows = 3;
                row = index % rows;
                return new Vector2(left + width * .5f, grateTop + grateHeight * (row + .5f) / rows);
            }

            if (product == 3) { columns = 3; rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, capacity) / (float)columns)); }
            else { columns = product < 2 ? 6 : 7; rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, capacity) / (float)columns)); }
            col = index % columns; row = index / columns;
            return new Vector2(left + width * (col + .5f) / columns, grateTop + grateHeight * (row + .5f) / rows);
        }

        public static Vector2 BarrelSlotPosition(int product, int slot)
        {
            Rect r = BoundsForProduct(product);
            int col = Mathf.Max(0, slot) % 3, row = Mathf.Max(0, slot) / 3;
            return new Vector2(r.x + r.width * (.22f + .28f * col), r.y + r.height * .16f + row * 2.1f);
        }
    }
}
