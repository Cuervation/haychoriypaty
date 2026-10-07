using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Shared canvas-space station geometry. Dimensions measured from live Floresta;
    /// placement and side approaches never depend on product counts, hiring or upgrades.</summary>
    public static class StreetWorkstationLayout
    {
        public static readonly Vector2 ReferenceGrillSize = new Vector2(282, 94);
        public static readonly Vector2 ReferenceTableSize = new Vector2(196, 98);
        public const float MinimumStationSpacing = 6f;
        public const float WorkerHalfWidth = 45f, WorkerHeight = 98f, FootHalfWidth = 18f;
        public static Rect GrillBounds => new Rect(242, 577, ReferenceGrillSize.x, ReferenceGrillSize.y);
        public static Rect FoodTableBounds => new Rect(16, 540, ReferenceTableSize.x, ReferenceTableSize.y);
        public static Rect CocaBounds => new Rect(524f - 80f * 2f / 3f, 488, 80f * 2f / 3f, 80);
        // Same barrel height, its own authored aspect ratio (1134/1387), outside the shared 90px corridor.
        public static Rect BeerBounds => new Rect(364f - 80f * 1134f / 1387f, 488, 80f * 1134f / 1387f, 80);
        public static Rect FernetBounds => new Rect(16, 434, 94, 66);
        public static Vector2 KitchenEntry => new Vector2(433, 485);

        public static Rect BoundsForProduct(int product) =>
            product < 4 ? FoodTableBounds : product == 4 ? CocaBounds : product == 5 ? FernetBounds : BeerBounds;
        public static Vector2 PickupPosition(int product) =>
            product < 4 ? new Vector2(235, 575) : product == 4 ? new Vector2(435, 565)
            : product == 5 ? new Vector2(155, 500) : new Vector2(395, 565);
        public static Vector2 ApproachPosition(int product) =>
            product < 4 ? new Vector2(235, 480) : product == 5 ? new Vector2(155, 415)
            : new Vector2(product == 4 ? 435 : 395, 450);
        public static bool PickupReachesRight(int product) => BoundsForProduct(product).center.x > PickupPosition(product).x;
        public static Rect WorkerFootBounds(Vector2 feet) => new Rect(feet.x - FootHalfWidth, feet.y - 12, FootHalfWidth * 2, 12);
        // Keep the same local hand-to-prop height when portrait anchors spread apart.
        // Fade only the near-station visual correction; counter/handoff and simulation routes remain unchanged.
        public static float WorkerPresentationOffset(Vector2 feet, int product, float verticalScale)
        {
            if (product < 0) return 0f;
            Vector2 pickup = PickupPosition(product), approach = ApproachPosition(product);
            float weight = Mathf.Clamp01(1f - Vector2.Distance(feet, pickup) / Mathf.Max(1f, Vector2.Distance(approach, pickup)));
            return (pickup.y - BoundsForProduct(product).y - WorkerHeight) * (1f - verticalScale) * weight;
        }
        public static Rect WorkerVisualBounds(Vector2 feet) => new Rect(feet.x - WorkerHalfWidth, feet.y - WorkerHeight, WorkerHalfWidth * 2, WorkerHeight + 1.5f);

        // Shared occupancy check for current/future placement changes: both rendered rectangles, not centers.
        public static bool HasStationClearance(Rect a, Rect b)
        {
            return a.xMax + MinimumStationSpacing <= b.xMin || b.xMax + MinimumStationSpacing <= a.xMin
                || a.yMax + MinimumStationSpacing <= b.yMin || b.yMax + MinimumStationSpacing <= a.yMin;
        }
        public static Rect FitArtwork(Rect bounds, Vector2 sourceSize)
        {
            float scale = Mathf.Min(bounds.width / sourceSize.x, bounds.height / sourceSize.y);
            Vector2 size = sourceSize * scale;
            return new Rect(bounds.center.x - size.x * .5f, bounds.yMax - size.y, size.x, size.y);
        }
    }
}
