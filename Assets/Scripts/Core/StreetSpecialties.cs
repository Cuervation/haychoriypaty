using System;

namespace HayChoriYPaty
{
    /// <summary>Authoritative product-to-worker and product-to-station rules.</summary>
    public static class StreetSpecialties
    {
        public enum Station { NormalGrill, PremiumGrill, CocaBarrel, FernetTable, BeerBarrel }

        public static StreetWorkerRole GetRequiredWorkerRole(int productId)
        {
            switch (productId)
            {
                case 0: case 1: return StreetWorkerRole.Parrillero;
                case 2: case 3: return StreetWorkerRole.ParrilleroPremium;
                case 4: case 6: return StreetWorkerRole.Cocacolero;
                case 5: return StreetWorkerRole.Fernetero;
                default: throw new ArgumentOutOfRangeException(nameof(productId), productId, "Unknown street product ID.");
            }
        }

        public static Station GetStation(int productId)
        {
            switch (productId)
            {
                case 0: case 1: return Station.NormalGrill;
                case 2: case 3: return Station.PremiumGrill;
                case 4: return Station.CocaBarrel;
                case 5: return Station.FernetTable;
                case 6: return Station.BeerBarrel;
                default: throw new ArgumentOutOfRangeException(nameof(productId), productId, "Unknown street product ID.");
            }
        }

        public static bool IsResponsible(StreetWorkerRole role, int productId)
        {
            return productId >= 0 && productId <= 6 && GetRequiredWorkerRole(productId) == role;
        }

        public static bool CatalogHasRole(int[] productIds, StreetWorkerRole role)
        {
            if (productIds == null) return false;
            for (int i = 0; i < productIds.Length; i++)
                if (IsResponsible(role, productIds[i])) return true;
            return false;
        }

        public static bool CatalogHasNonDefaultRole(int[] productIds)
        {
            if (productIds == null) return false;
            for (int i = 0; i < productIds.Length; i++)
                if (GetRequiredWorkerRole(productIds[i]) != StreetWorkerRole.Parrillero) return true;
            return false;
        }
    }
}
