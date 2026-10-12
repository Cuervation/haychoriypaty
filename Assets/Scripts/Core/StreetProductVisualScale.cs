using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Shared render-only size targets for products; never changes stock, recipes, or order data.</summary>
    internal static class StreetProductVisualScale
    {
        private const float BarrelDrinkScale = 1.35f;
        private const float Coca600ReferenceHeightCm = 24f; // Approximate PET reference; packaging varies by market.
        private const float Beer473HeightCm = 15.7f;
        private const float Fernet800CupHeightCm = 14.3f;
        private const float Coca600BarrelHeight = 15f * BarrelDrinkScale;
        private const float Fernet1LBottleHeightRatio = 1.31f;
        private const float Coca25LBottleHeightRatio = 1.40f;

        internal static float ShelfBottleHeightFraction(bool fernetBottle)
        {
            float ratio = fernetBottle ? Fernet1LBottleHeightRatio : Coca25LBottleHeightRatio;
            return Coca600BarrelHeight * ratio / StreetKitchenLayout.TableHeight;
        }

        internal static Vector2 ServingSize(int product, bool carried)
        {
            float carryScale = carried ? 1.1f : 1f;
            switch (product)
            {
                case 0: return new Vector2(22f, 10f) * carryScale; // Chori on a long roll.
                case 1: return new Vector2(20f, 14f) * carryScale; // Patty roll is shorter and thicker.
                case 2: return new Vector2(20f, 12f) * carryScale;
                case 3: return new Vector2(21f, 12f) * carryScale;
                case 4: return new Vector2(8f, 15f) * BarrelDrinkScale; // 600 ml Coca bottle.
                case 5: return new Vector2(12f, Coca600BarrelHeight * Fernet800CupHeightCm / Coca600ReferenceHeightCm);
                case 6: return new Vector2(8f, 15f) * BarrelDrinkScale * (Beer473HeightCm / Coca600ReferenceHeightCm);
                default: return Vector2.zero;
            }
        }

        internal static Vector2 GrillMeatSize(int product)
        {
            // Fixed per product across catalogs: an absent companion must not make a cut change scale.
            switch (product)
            {
                case 0: return new Vector2(28f, 11f); // Long, narrow chorizo.
                case 1: return new Vector2(17f, 12f); // Round patty.
                case 2: return new Vector2(16f, 16f); // Compact bondiola medallion.
                case 3: return new Vector2(24f, 11f); // Wider, flatter vacío steak.
                default: return Vector2.zero;
            }
        }
    }
}
