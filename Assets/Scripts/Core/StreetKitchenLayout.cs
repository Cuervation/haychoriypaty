using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Catalog-derived kitchen geometry shared by rendering, pickup points, and worker routes.</summary>
    public sealed class StreetKitchenLayout
    {
        public const float SourceWidth = 760f;
        public const float Scale = StreetSceneLayout.Width / SourceWidth;
        public const float TableY = 520f;
        public const float BarrelY = 520f;
        public const float WorkerLaneY = 450f;
        public const float TablePickupY = 556f;
        public const float GrillPickupY = 608f;
        public const float BarrelPickupY = 508f;
        public const float TableWidth = 196f * Scale, TableHeight = 98f * Scale;
        public const float BarrelWidth = 60f * Scale, BarrelHeight = 117.6f * Scale;
        public const float StandardGrillWidth = 352f * Scale, StandardGrillHeight = 117.3333f * Scale;
        private const float GrillBottomLimit = 714f;

        private readonly bool hasNormal, hasPremium, hasFernet, hasCoca, hasBeer;
        private readonly Rect normalTable, premiumTable, fernetTable, cocaBarrel, beerBarrel;
        private readonly Rect normalGrill, premiumGrill;

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
                + (hasCoca ? 60f : 0f) + (hasBeer ? 60f : 0f));
            float gap = count > 0 ? (SourceWidth - totalWidth) / (count + 1) : 0f;
            float x = gap;
            normal = PlaceUpper(ref x, gap, hasNormal, 196f);
            premium = PlaceUpper(ref x, gap, hasPremium, 196f);
            fernet = PlaceUpper(ref x, gap, hasFernet, 196f);
            coca = PlaceUpper(ref x, gap, hasCoca, 60f);
            beer = PlaceUpper(ref x, gap, hasBeer, 60f);
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
            float grillY = 620f;
            float nativeWidth = 352f;
            if (hasNormal && hasPremium)
            {
                normal = new Rect(14f * Scale, grillY, StandardGrillWidth, StandardGrillHeight);
                premium = new Rect(394f * Scale, grillY, StandardGrillWidth, StandardGrillHeight);
                return;
            }
            bool active = hasNormal || hasPremium;
            if (!active) { normal = premium = Rect.zero; return; }

            float upperBottom = TableY;
            if (hasNormal) upperBottom = Mathf.Max(upperBottom, normalTable.yMax);
            if (hasPremium) upperBottom = Mathf.Max(upperBottom, premiumTable.yMax);
            if (hasFernet) upperBottom = Mathf.Max(upperBottom, fernetTable.yMax);
            if (hasCoca) upperBottom = Mathf.Max(upperBottom, cocaBarrel.yMax);
            if (hasBeer) upperBottom = Mathf.Max(upperBottom, beerBarrel.yMax);
            grillY = Mathf.Max(600f, upperBottom + 12f);
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

        public Vector2 GrillSlotPosition(int product, int slot, bool companionAvailable, int capacity)
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
                    width = usableWidth * .60f; columns = 4;
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

        public Vector2 BarrelSlotPosition(int product, int slot)
        {
            Rect r = BoundsForProduct(product);
            int col = Mathf.Max(0, slot) % 3, row = Mathf.Max(0, slot) / 3;
            return new Vector2(r.x + r.width * (.22f + .28f * col), r.y + r.height * .16f + row * 2.1f);
        }
    }
}
