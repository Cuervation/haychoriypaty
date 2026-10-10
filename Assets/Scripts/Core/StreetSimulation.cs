using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    [Serializable]
    public sealed class StreetUpgradeCostProfile
    {
        public string profileId = "TEST_ECONOMY_PROFILE";
        [Tooltip("Shared hire curve used by every role unless its optional role profile override selects another profile.")]
        public int[] hireCosts = { 15, 30, 60, 100 };
        public int[] speedUpgradeCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
    }


    [Serializable]
    public sealed class StreetLevelBalance
    {
        [Tooltip("Mandatory targets keyed by stable product ID 0..6; zero only for products not in this level.")]
        public int[] goalsByProductId = new int[7];
        [Tooltip("First paid hire price by StreetWorkerRole enum; zero means the role is not available.")]
        public int[] hireBaseCostsByRole = new int[4];
        [Min(0.1f)] public float speedCostMultiplier = 1f;
        [Min(0)] public int startingCoins;

        public StreetLevelBalance() { }
        public StreetLevelBalance(int[] goals, int[] hireBaseCosts, float speedMultiplier, int startingCoins = 0)
        {
            goalsByProductId = goals; hireBaseCostsByRole = hireBaseCosts;
            speedCostMultiplier = speedMultiplier; this.startingCoins = Mathf.Max(0, startingCoins);
        }
    }

    [Serializable]
    public sealed class StreetBalance
    {
        [Range(1, 21)] public int maxCustomers = 21;
        public int randomSeed = 1337;
        [Range(.1f,4f)] public float priceSensitivity=2.5f;
        [HideInInspector, Tooltip("Legacy serialized limit retained for save/scene compatibility; worker hiring is unlimited when the role is enabled.")]
        public int maxStaff = 5;
        [HideInInspector, Tooltip("Legacy serialized role caps retained for scene compatibility; ignored by runtime hiring.")]
        public int maxParrilleros;
        [HideInInspector] public int maxCocacoleros;
        [HideInInspector] public int maxPremiumParrilleros;
        [HideInInspector] public int maxFerneteros;
        [Tooltip("All current clubs temporarily use the same profile; add profiles and remap levels for future club-specific economy.")]
        public StreetUpgradeCostProfile[] upgradeCostProfiles = { new StreetUpgradeCostProfile() };
        [Tooltip("Profile index used per level; all current levels share profile 0.")]
        public int[] levelUpgradeCostProfileIds = { 0, 0, 0, 0, 0 };
        [Tooltip("Optional hire-profile override by role enum value; -1 inherits that level's shared profile.")]
        public int[] roleHireCostProfileIds = { -1, -1, -1, -1 };
        [HideInInspector, Tooltip("Legacy serialized fields retained only for scene compatibility; runtime costs come from upgradeCostProfiles.")]
        public int[] hireCosts = { 15, 30, 60, 100 };
        [HideInInspector, Tooltip("Legacy serialized fields retained only for scene compatibility; runtime costs come from upgradeCostProfiles.")]
        public int[] speedUpgradeCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        [HideInInspector, Tooltip("Legacy serialized field retained only for scene compatibility; runtime costs come from upgradeCostProfiles.")]
        public int[] florestaHireCosts = { 15, 30, 60, 100 };
        [HideInInspector, Tooltip("Legacy serialized field retained only for scene compatibility; runtime costs come from upgradeCostProfiles.")]
        public int[] florestaSpeedUpgradeCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        [Min(.01f)] public float speedIncrease = .10f;
        [Min(0.1f)] public float customerArrivalSeconds = 0.16f;
        [Min(0.1f)] public float workerSpeed = 300f;
        [Min(10f)] public float customerSpeed = 300f;
        public float[] levelDemandMultipliers = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.65f, 1.815f, 1.9965f, 2.19615f, 2.415765f, 2.6573415f };
        [Min(0.01f)] public float pickupSeconds = 0.28f;
        [Min(0.1f)] public float customerPatienceSeconds = 180f;
        [Min(0f)] public float minPrice = 0f;
        [Min(0f)] public float maxPrice = 60f;
        public float[] productPriceMultipliers = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        public StreetKitchenBalance kitchen = new StreetKitchenBalance();
        [Min(1)] public int minOrderQuantity = 999;
        [Min(1)] public int maxOrderQuantity = 999;
        [Min(0f)] public float initialPrice = 5f;
        [Min(0f)] public float deliveryPatienceRefreshSeconds = 180f;
        public int[] levelProductCounts = { 1, 2, 3, 5, 7, 7, 7, 7, 7, 7, 7 };
        [Tooltip("Comma-separated stable product IDs, in each level's display/order priority.")]
        public string[] levelProductIds = { "0", "0,4", "0,1,4", "0,1,2,4,6", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5" };
        [HideInInspector, Tooltip("Legacy aggregate goals retained for serialized-scene compatibility; individual levelBalances are authoritative.")]
        public int[] levelGoals = { 200, 200, 65, 85, 110, 121, 133, 146, 161, 177, 195 };
        public float[] levelDurations = { 120f, 180f, 180f, 210f, 240f, 255f, 270f, 295f, 300f, 300f, 300f };
        [Tooltip("Official per-level goals, role hire base costs, speed-cost multiplier, and opening attempt balance.")]
        public StreetLevelBalance[] levelBalances = CreateOfficialLevelBalances();

        private static StreetLevelBalance[] CreateOfficialLevelBalances()
        {
            return new[]
            {
                new StreetLevelBalance(new[] {200,0,0,0,0,0,0}, new[] {15,0,0,0}, 1f),
                new StreetLevelBalance(new[] {200,0,0,0,200,0,0}, new[] {20,20,0,0}, 1.5f),
                new StreetLevelBalance(new[] {180,120,0,0,150,0,0}, new[] {25,25,0,0}, 2.25f),
                new StreetLevelBalance(new[] {185,125,60,0,155,0,75}, new[] {30,30,35,0}, 3.25f),
                new StreetLevelBalance(new[] {190,130,70,65,160,65,80}, new[] {35,35,45,40}, 4.50f),
                new StreetLevelBalance(new[] {210,145,80,75,175,75,90}, new[] {40,40,50,45}, 6.00f),
                new StreetLevelBalance(new[] {230,165,100,85,195,80,105}, new[] {45,45,55,50}, 8.00f),
                new StreetLevelBalance(new[] {250,180,110,95,210,90,115}, new[] {50,50,65,55}, 10.50f),
                new StreetLevelBalance(new[] {275,195,120,110,225,100,125}, new[] {55,55,70,65}, 13.50f, 50),
                new StreetLevelBalance(new[] {310,220,135,125,255,110,145}, new[] {60,60,80,70}, 17.00f, 150),
                new StreetLevelBalance(new[] {345,245,150,140,285,120,165}, new[] {70,70,90,80}, 21.00f, 200)
            };
        }

        public StreetLevelBalance GetLevelBalance(int levelIndex)
        {
            return levelBalances != null && levelIndex >= 0 && levelIndex < levelBalances.Length && levelBalances[levelIndex] != null
                ? levelBalances[levelIndex] : CreateOfficialLevelBalances()[Mathf.Clamp(levelIndex, 0, 10)];
        }

        public int GetProductGoal(int levelIndex, int productId)
        {
            if (productId < 0 || productId >= 7) return 0;
            StreetLevelBalance row = GetLevelBalance(levelIndex);
            return row.goalsByProductId != null && productId < row.goalsByProductId.Length && row.goalsByProductId[productId] > 0
                ? row.goalsByProductId[productId] : CreateOfficialLevelBalances()[Mathf.Clamp(levelIndex, 0, 10)].goalsByProductId[productId];
        }

        public int GetHireBaseCost(int levelIndex, StreetWorkerRole role)
        {
            int roleIndex = (int)role;
            StreetLevelBalance row = GetLevelBalance(levelIndex);
            return row.hireBaseCostsByRole != null && roleIndex >= 0 && roleIndex < row.hireBaseCostsByRole.Length
                ? Mathf.Max(0, row.hireBaseCostsByRole[roleIndex])
                : CreateOfficialLevelBalances()[Mathf.Clamp(levelIndex, 0, 10)].hireBaseCostsByRole[roleIndex];
        }
        public int GetStartingCoins(int levelIndex)
        {
            return Mathf.Max(0, GetLevelBalance(levelIndex).startingCoins);
        }
        [Tooltip("Optional deadline-only Floresta trial. Default false: win immediately at the unit goal.")]
        public bool florestaFinishAtDeadline = false;
        private static readonly StreetUpgradeCostProfile DefaultUpgradeCostProfile = new StreetUpgradeCostProfile();

        public StreetUpgradeCostProfile GetUpgradeCostProfile(int levelIndex)
        {
            return GetProfile(levelUpgradeCostProfileIds != null && levelIndex >= 0 && levelIndex < levelUpgradeCostProfileIds.Length
                ? levelUpgradeCostProfileIds[levelIndex] : 0);
        }

        public StreetUpgradeCostProfile GetHireCostProfile(int levelIndex, StreetWorkerRole role)
        {
            int roleIndex = (int)role;
            int profileIndex = roleHireCostProfileIds != null && roleIndex >= 0 && roleIndex < roleHireCostProfileIds.Length
                ? roleHireCostProfileIds[roleIndex] : -1;
            return GetProfile(profileIndex >= 0 ? profileIndex :
                levelUpgradeCostProfileIds != null && levelIndex >= 0 && levelIndex < levelUpgradeCostProfileIds.Length
                    ? levelUpgradeCostProfileIds[levelIndex] : 0);
        }

        private StreetUpgradeCostProfile GetProfile(int profileIndex)
        {
            if (upgradeCostProfiles == null || upgradeCostProfiles.Length == 0) return DefaultUpgradeCostProfile;
            if (profileIndex < 0 || profileIndex >= upgradeCostProfiles.Length) profileIndex = 0;
            return upgradeCostProfiles[profileIndex] ?? DefaultUpgradeCostProfile;
        }
    }

    public enum StreetCustomerState { Entering, Waiting, Receiving, Leaving, Advancing }
    public enum StreetWorkerState { Idle, ToStation, Pickup, ToCounter, Handoff }
    public enum StreetWorkerRole { Parrillero = 0, Cocacolero = 1, ParrilleroPremium = 2, Fernetero = 3 }

    [Serializable]
    public sealed class StreetOrderLine
    {
        [SerializeField] private int product;
        [SerializeField] private int remaining;
        [SerializeField] private int reserved;
        [SerializeField] private int ownerWorkerId;

        public int Product { get { return product; } internal set { product = value; } }
        public int Remaining { get { return remaining; } internal set { remaining = Mathf.Max(0, value); } }
        public int Reserved { get { return reserved; } internal set { reserved = Mathf.Max(0, value); } }
        public int OwnerWorkerId { get { return ownerWorkerId; } internal set { ownerWorkerId = Mathf.Max(0, value); } }

        internal StreetOrderLine(int product, int remaining)
        {
            this.product = product;
            this.remaining = Mathf.Max(0, remaining);
        }
    }

    [Serializable]
    public sealed class StreetCustomer
    {
        public const int MaxOrderProducts = 5;
        public const int MaxVisibleOrderProducts = 2;
        [SerializeField] private List<StreetOrderLine> orderLines = new List<StreetOrderLine>(MaxOrderProducts);

        public int Id { get; internal set; }
        public int Product { get { return LegacyLine(0)?.Product ?? -1; } internal set { EnsureLegacyLine(0, value).Product = value; } }
        public int Remaining { get { return LegacyLine(0)?.Remaining ?? 0; } internal set { EnsureLegacyLine(0, Product).Remaining = Mathf.Max(0, value); } }
        public int SecondaryProduct { get { return LegacyLine(1)?.Product ?? -1; } internal set { if (value < 0) { if (orderLines.Count > 1) orderLines.RemoveAt(1); } else EnsureLegacyLine(1, value).Product = value; } }
        public int SecondaryRemaining { get { return LegacyLine(1)?.Remaining ?? 0; } internal set { if (orderLines.Count > 1) orderLines[1].Remaining = Mathf.Max(0, value); else if (value > 0) EnsureLegacyLine(1, -1).Remaining = value; } }
        public int Reserved { get { int total = 0; for (int i = 0; i < orderLines.Count; i++) total += orderLines[i].Reserved; return total; } }
        public int ReservedPrimary { get { return LegacyLine(0)?.Reserved ?? 0; } internal set { if (orderLines.Count > 0) orderLines[0].Reserved = value; } }
        public int ReservedSecondary { get { return LegacyLine(1)?.Reserved ?? 0; } internal set { if (orderLines.Count > 1) orderLines[1].Reserved = value; } }
        public IReadOnlyList<StreetOrderLine> OrderLines { get { return orderLines; } }
        public int OrderLineCount { get { return orderLines.Count; } }
        public int PendingOrderLineCount { get { int count = 0; for (int i = 0; i < orderLines.Count; i++) if (orderLines[i].Remaining > 0) count++; return count; } }
        public int HiddenOrderLineCount { get { return Mathf.Max(0, PendingOrderLineCount - MaxVisibleOrderProducts); } }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public StreetCustomerState State { get; internal set; }
        public float PatienceFraction { get; internal set; }
        public float AnimationTime { get; internal set; }
        internal float Patience;
        internal float ReceiveRemaining;
        internal int Slot;

        public StreetOrderLine GetOrderLine(int index) => index >= 0 && index < orderLines.Count ? orderLines[index] : null;
        public StreetOrderLine GetPendingOrderLine(int pendingIndex)
        {
            if (pendingIndex < 0) return null;
            for (int i = 0; i < orderLines.Count; i++)
                if (orderLines[i].Remaining > 0 && pendingIndex-- == 0) return orderLines[i];
            return null;
        }
        public StreetOrderLine GetVisibleOrderLine(int visibleIndex) =>
            visibleIndex >= 0 && visibleIndex < MaxVisibleOrderProducts ? GetPendingOrderLine(visibleIndex) : null;
        internal StreetOrderLine FindOrderLine(int product)
        {
            for (int i = 0; i < orderLines.Count; i++) if (orderLines[i].Product == product) return orderLines[i];
            return null;
        }
        internal void SetOrder(int[] products, int[] quantities)
        {
            orderLines.Clear();
            for (int i = 0; i < products.Length; i++) orderLines.Add(new StreetOrderLine(products[i], quantities[i]));
        }
        private StreetOrderLine LegacyLine(int index) => index >= 0 && index < orderLines.Count ? orderLines[index] : null;
        private StreetOrderLine EnsureLegacyLine(int index, int product)
        {
            while (orderLines.Count <= index) orderLines.Add(new StreetOrderLine(-1, 0));
            if (product >= 0) orderLines[index].Product = product;
            return orderLines[index];
        }
    }

    [Serializable]
    public sealed class StreetWorker
    {
        public int Id { get; internal set; }
        public int Product { get; internal set; }
        public StreetWorkerRole Role { get; internal set; }
        public int CustomerId { get; internal set; }
        public int CarriedItemId { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public StreetWorkerState State { get; internal set; }
        public float AnimationTime { get; internal set; }
        public float PickupProgress { get; internal set; }
        // Visual telemetry only; movement math and service timing are unchanged.
        public float TravelDistance { get; internal set; }
        public float StepDistance { get; internal set; }
        public Vector2 FacingVector { get; internal set; }
        internal float Delay;
        internal float PickupDuration;
        internal Vector2[] Route;
        internal int RouteIndex;
        internal StreetCustomer Customer;
    }

    [Serializable]
    public sealed class StreetSale
    {
        public int Id { get; internal set; }
        public Vector2 Position { get; internal set; }
        public int Amount { get; internal set; }
        public float Age { get; internal set; }
    }

    /// <summary>Deterministic, view-independent street-service simulation.</summary>
    public sealed class StreetSimulation
    {
        public const float FixedProductPrice = 5f; // Legacy common/chori price retained for compatibility.
        public const float FlorestaChoriPrice = FixedProductPrice; // Legacy name retained for compatibility.
        private static readonly float[] OfficialProductPrices = { FixedProductPrice, FixedProductPrice, 10f, 12f, FixedProductPrice, 12f, 7f };
        // Preserve the former uniform-$5 demand model; selling prices must not influence ticket frequency or product selection.
        private const float DemandReferencePrice = FixedProductPrice;
        public const float FrontQueueY = 324f, QueueRowSpacing = 56f;
        // Legacy public name retained for callers; all levels use the shared scene service line.
        public const float ChicagoCounterServiceY = StreetSceneLayout.WorkerServiceY;
        private const int QueueColumns = 7, QueueSlots = 21;
        public static readonly string[] ProductNames = { "Chori", "Paty", "Bondiola", "Vacío", "Coca 600 ml", "Fernet con Coca 1 L", "Cerveza en lata" };
        public static readonly string[] LevelNames = { "Floresta / All Boys", "Nueva Chicago", "Liniers - Velez Sarsfield", "Ferro Carril Oeste", "Independiente de Avellaneda", "Racing Club / Avellaneda", "San Lorenzo / Boedo", "River Plate / Núñez", "Boca Juniors / La Boca", "Sindicato de Camioneros / Plaza de Mayo", "Los Redondos / Tandil" };
        private static readonly int[] DefaultProductCounts = { 1, 2, 3, 5, 7, 7, 7, 7, 7, 7, 7 };
        private static readonly int[][] DefaultLevelProducts = {
            new[] { 0 }, new[] { 0, 4 }, new[] { 0, 1, 4 },
            new[] { 0, 1, 2, 4, 6 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
            new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
            new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }
        };
        private static readonly float[] DefaultDemandMultipliers = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.65f, 1.815f, 1.9965f, 2.19615f, 2.415765f, 2.6573415f };
        private static readonly int[] DefaultSpeedCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        private static readonly float[] DefaultDurations = { 120f, 180f, 180f, 210f, 240f, 255f, 270f, 295f, 300f, 300f, 300f };
        private const int MaxHireCost = int.MaxValue - int.MaxValue % 5;
        private readonly StreetBalance balance;
        private readonly int[][] availableProductsByLevel;
        private readonly List<StreetCustomer> customers = new List<StreetCustomer>();
        private readonly List<StreetWorker> workers = new List<StreetWorker>();
        private readonly List<StreetSale> sales = new List<StreetSale>();
        private float arrival;
        private int nextCustomer = 1, nextWorker = 1, nextSale = 1;
        private readonly int[] deliveredByProduct = new int[7];
        private float price;
        private System.Random random;

        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public int LevelIndex { get; private set; }
        public float Price { get { return price; } }
        public int Coins { get; private set; }
        public int CoinsEarned { get; private set; }
        public int Delivered { get; private set; }
        public int ChoriDelivered { get { return GetProductDelivered(0); } }
        public int CocaDelivered { get { return GetProductDelivered(4); } }
        // Aggregate target is retained for summaries only; it is never a victory predicate.
        public int Goal
        {
            get { int total = 0; for (int slot = 0; slot < ProductCount; slot++) total += GetProductGoal(GetAvailableProduct(slot)); return total; }
        }
        public bool GoalReached
        {
            get
            {
                if (ProductCount <= 0) return false;
                for (int slot = 0; slot < ProductCount; slot++)
                {
                    int product = GetAvailableProduct(slot);
                    if (GetProductGoal(product) <= 0 || GetProductDelivered(product) < GetProductGoal(product)) return false;
                }
                return true;
            }
        }
        public int GetProductGoal(int productId) => IsProductAvailable(productId) ? balance.GetProductGoal(LevelIndex, productId) : 0;
        public int GetProductDelivered(int productId) => productId >= 0 && productId < deliveredByProduct.Length ? deliveredByProduct[productId] : 0;
        public int GetProductRemaining(int productId) => Mathf.Max(0, GetProductGoal(productId) - GetProductDelivered(productId));
        public bool IsProductGoalComplete(int productId) => GetProductGoal(productId) > 0 && GetProductRemaining(productId) == 0;
        public float TimeRemaining { get { return Mathf.Max(0f, LevelValue(balance.levelDurations, LevelIndex, DefaultDurations) - Elapsed); } }
        public int StaffCount { get { return workers.Count; } }
        public StreetWorkerRole NextHireRole
        {
            get
            {
                StreetWorkerRole[] roles = { StreetWorkerRole.Parrillero, StreetWorkerRole.Cocacolero,
                    StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Fernetero };
                StreetWorkerRole best = StreetWorkerRole.Parrillero;
                int lowestCount = int.MaxValue;
                foreach (StreetWorkerRole role in roles)
                {
                    if (!HasRole(role)) continue;
                    int count = WorkerCount(role);
                    if (count < lowestCount) { lowestCount = count; best = role; }
                }
                return best;
            }
        }
        public int ParrilleroCount { get { return WorkerCount(StreetWorkerRole.Parrillero); } }
        public int CocacoleroCount { get { return WorkerCount(StreetWorkerRole.Cocacolero); } }
        public int ParrilleroPremiumCount { get { return WorkerCount(StreetWorkerRole.ParrilleroPremium); } }
        public int FerneteroCount { get { return WorkerCount(StreetWorkerRole.Fernetero); } }
        public bool HasCocacolero { get { return HasRole(StreetWorkerRole.Cocacolero); } }
        public bool HasParrilleroPremium { get { return HasRole(StreetWorkerRole.ParrilleroPremium); } }
        public bool HasFernetero { get { return HasRole(StreetWorkerRole.Fernetero); } }
        public int ParrilleroHireCost { get { return HireCostForRole(StreetWorkerRole.Parrillero); } }
        public int CocacoleroHireCost { get { return HireCostForRole(StreetWorkerRole.Cocacolero); } }
        public int ParrilleroPremiumHireCost { get { return HireCostForRole(StreetWorkerRole.ParrilleroPremium); } }
        public int FerneteroHireCost { get { return HireCostForRole(StreetWorkerRole.Fernetero); } }
        public bool CanHireParrillero { get { return CanHireRole(StreetWorkerRole.Parrillero); } }
        public bool CanHireCocacolero { get { return CanHireRole(StreetWorkerRole.Cocacolero); } }
        public bool CanHireParrilleroPremium { get { return CanHireRole(StreetWorkerRole.ParrilleroPremium); } }
        public bool CanHireFernetero { get { return CanHireRole(StreetWorkerRole.Fernetero); } }
        public bool HasRole(StreetWorkerRole role) { return StreetSpecialties.CatalogHasRole(availableProductsByLevel[LevelIndex], role); }
        public int WorkerCount(StreetWorkerRole role) { int count = 0; foreach (var worker in workers) if (worker.Role == role) count++; return count; }
        public int MaxWorkersForRole(StreetWorkerRole role)
        {
            // This legacy query now reports the representable count range, not a gameplay cap.
            return HasRole(role) ? int.MaxValue : 0;
        }
        public int HireCostForRole(StreetWorkerRole role)
        {
            if (!HasRole(role)) return 0;
            long paidNumber = role == StreetWorkerRole.Parrillero
                ? Math.Max(1, WorkerCount(role)) // The opening Parrillero is free.
                : (long)WorkerCount(role) + 1;
            int baseCost = balance.GetHireBaseCost(LevelIndex, role);
            if (baseCost <= 0) return 0;

            // Preserve x1, x2.5 and x5, then extend the fourth-tier x9 curve with
            // n(n+1)/2-1. Work with exact integer ratios, and saturate only at the
            // largest positive $5 multiple representable by Coins (not a hire cap).
            return ComputeHireCost(baseCost, paidNumber);
        }
        private static int ComputeHireCost(int baseCost, long paidNumber)
        {
            if (baseCost <= 0 || paidNumber <= 0) return 0;
            long factorNumerator;
            long denominator;
            if (paidNumber == 1) { factorNumerator = 1; denominator = 1; }
            else if (paidNumber == 2) { factorNumerator = 5; denominator = 2; }
            else if (paidNumber == 3) { factorNumerator = 5; denominator = 1; }
            else
            {
                if (paidNumber == long.MaxValue) return MaxHireCost;
                long left = paidNumber, right = paidNumber + 1;
                if ((left & 1L) == 0) left /= 2; else right /= 2;
                if (left > long.MaxValue / right) return MaxHireCost;
                factorNumerator = left * right - 1;
                denominator = 1;
            }

            long allowedNumerator = (long)MaxHireCost * denominator;
            if (factorNumerator > allowedNumerator / baseCost) return MaxHireCost;
            long numerator = factorNumerator * baseCost;
            long priceStep = denominator * 5;
            long rounded = ((numerator + priceStep - 1) / priceStep) * 5;
            return (int)Math.Min(rounded, MaxHireCost);
        }
        public bool CanHireRole(StreetWorkerRole role)
        {
            int hireCost = HireCostForRole(role);
            return (Phase == RoundPhase.Ready || Phase == RoundPhase.Playing) &&
                   HasRole(role) && hireCost > 0 && Coins >= hireCost;
        }
        public int SpeedLevel { get; private set; }
        public bool CanEditPrices { get { return false; } }
        public int MaxStaffCount { get { return MaxWorkersForRole(StreetWorkerRole.Parrillero); } }
        public int MaxSpeedLevel { get { return DefaultSpeedCosts.Length; } }
        public int HireCost { get { return ParrilleroHireCost; } }
        public int SpeedCost
        {
            get
            {
                if (SpeedLevel >= MaxSpeedLevel) return 0;
                float multiplier = balance.GetLevelBalance(LevelIndex).speedCostMultiplier;
                int raw = Mathf.CeilToInt(DefaultSpeedCosts[SpeedLevel] * Mathf.Max(0.1f, multiplier));
                return Mathf.CeilToInt(raw / 5f) * 5;
            }
        }
        public bool CanHire { get { return CanHireParrillero; } }
        public bool CanUpgradeSpeed { get { return (Phase == RoundPhase.Ready || Phase == RoundPhase.Playing) && SpeedLevel < MaxSpeedLevel && Coins >= SpeedCost; } }
        public float WorkRate { get { return 1f + SpeedLevel * balance.speedIncrease; } }
        public IReadOnlyList<StreetCustomer> Customers { get { return customers; } }
        public IReadOnlyList<StreetWorker> Workers { get { return workers; } }
        public IReadOnlyList<StreetSale> Sales { get { return sales; } }
        public StreetKitchenProduction Kitchen { get; private set; }
        public StreetKitchenLayout KitchenLayout { get; private set; }
        public float Elapsed { get; private set; }
        public float DemandFraction
        {
            get
            {
                float total = 0;
                for (int slot = 0; slot < ProductCount; slot++) total += DemandReferencePrice;
                return Mathf.Clamp01((balance.maxPrice-total/ProductCount)/Mathf.Max(0.01f,balance.maxPrice-balance.minPrice));
            }
        }
        public int ProductCount { get { return availableProductsByLevel[LevelIndex].Length; } }

        public int GetAvailableProduct(int slot)
        {
            return slot >= 0 && slot < ProductCount ? availableProductsByLevel[LevelIndex][slot] : -1;
        }

        public bool HasSpecialtyWorkers { get { return StreetSpecialties.CatalogHasNonDefaultRole(availableProductsByLevel[LevelIndex]); } }

        private static int[][] BuildAvailableProducts(StreetBalance value)
        {
            var result = new int[LevelNames.Length][];
            for (int level = 0; level < result.Length; level++)
            {
                int[] parsed = ParseProductIds(value.levelProductIds != null && level < value.levelProductIds.Length
                    ? value.levelProductIds[level] : null);
                if (parsed == null) parsed = (int[])DefaultLevelProducts[level].Clone();
                result[level] = parsed;
            }
            return result;
        }

        private static int[] ParseProductIds(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string[] tokens = value.Split(',');
            if (tokens.Length < 1 || tokens.Length > StreetCustomer.MaxOrderProducts && tokens.Length != 7) return null;
            var products = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                int product;
                if (!int.TryParse(tokens[i].Trim(), out product) || product < 0 || product >= ProductNames.Length) return null;
                for (int prior = 0; prior < i; prior++) if (products[prior] == product) return null;
                products[i] = product;
            }
            return products;
        }

        public bool IsProductAvailable(int product)
        {
            for (int slot = 0; slot < ProductCount; slot++)
                if (GetAvailableProduct(slot) == product) return true;
            return false;
        }

        public StreetSimulation(StreetBalance balance, int level = 0, float price = 5f, int coins = 0, int staff = 1, int speed = 0)
        {
            this.balance = balance ?? new StreetBalance();
            LevelIndex = Mathf.Clamp(level, 0, LevelNames.Length - 1);
            availableProductsByLevel = BuildAvailableProducts(this.balance);
            RefreshKitchenLayout();
            random = new System.Random(this.balance.randomSeed + LevelIndex * 97);
            // Keep the legacy aggregate price field for compatibility; the official ID table owns unit prices.
            this.price = FixedProductPrice;
            // Constructor arguments remain for compatibility with saved callers, but every
            // construction creates a new attempt baseline; purchases are never resumed.
            Coins = Mathf.Max(0, coins); // Preserve the existing injected-balance API; actual attempt entry clears coins.
            ResetTeamAndSpeed();
        }

        private void RefreshKitchenLayout()
        {
            KitchenLayout = new StreetKitchenLayout(availableProductsByLevel[LevelIndex]);
        }

        public void StartRound()
        {
            customers.Clear(); sales.Clear();
            Coins = balance.GetStartingCoins(LevelIndex); CoinsEarned = 0; // Opening balance is a configured attempt grant, not earned revenue.
            // Every round entry is a fresh attempt, regardless of selected club.
            ResetTeamAndSpeed();
            foreach (StreetWorker worker in workers) ResetWorker(worker);
            Elapsed = 0f; Delivered = 0; Array.Clear(deliveredByProduct, 0, deliveredByProduct.Length);
            arrival = 0f; Phase = RoundPhase.Playing;
        }
        public void SetPrice(float value) { SetProductPrice(0,value); }
        public float GetProductPrice(int product) { return OfficialProductPrices[Mathf.Clamp(product,0,OfficialProductPrices.Length - 1)]; }
        public void SetProductPrice(int product, float value)
        {
            // Retain the legacy setter for saved callers; official prices are immutable and ID-based.
            if (product < 0 || product >= OfficialProductPrices.Length) return;
            price = FixedProductPrice; // Legacy aggregate price represents Chori (ID 0).
        }
        public bool TryHire()
        {
            return TryHire(StreetWorkerRole.Parrillero);
        }
        public bool TryHire(StreetWorkerRole role)
        {
            if (!CanHireRole(role)) return false;
            Coins -= HireCostForRole(role); AddWorker(role); return true;
        }
        public void RestoreWorkerCounts(int parrilleros, int cocacoleros)
        {
            // Legacy explicit Ready-state composition helper; StreetGame never applies save purchases through it.
            if (Phase != RoundPhase.Ready) return;
            workers.Clear(); nextWorker = 1;
            for (int i = 0; i < Math.Max(1, parrilleros); i++) AddWorker(StreetWorkerRole.Parrillero);
            if (HasRole(StreetWorkerRole.Cocacolero))
                for (int i = 0; i < Math.Max(0, cocacoleros); i++) AddWorker(StreetWorkerRole.Cocacolero);
        }
        public bool TryUpgradeSpeed()
        {
            if (!CanUpgradeSpeed) return false;
            Coins -= SpeedCost; SpeedLevel++; return true;
        }
        public bool SelectLevel(int level)
        {
            if (Phase != RoundPhase.Ready || level < 0 || level >= LevelNames.Length) return false;
            LevelIndex = level;
            RefreshKitchenLayout();
            ResetTeamAndSpeed();
            Coins = balance.GetStartingCoins(LevelIndex); CoinsEarned = 0;
            return true;
        }
        public bool NextLevel()
        {
            if (Phase != RoundPhase.Won || LevelIndex + 1 >= LevelNames.Length) return false;
            LevelIndex++;
            RefreshKitchenLayout();
            ResetTeamAndSpeed();
            Coins = balance.GetStartingCoins(LevelIndex); CoinsEarned = 0;
            Phase = RoundPhase.Ready; return true;
        }

        public void Step(float delta)
        {
            if (Phase != RoundPhase.Playing || delta <= 0f) return;
            float remaining = Mathf.Min(delta, 10f);
            while (remaining > 0f && Phase == RoundPhase.Playing)
            {
                float dt = Mathf.Min(0.05f, remaining); remaining -= dt;
                StepSlice(dt);
            }
        }

        public bool SpawnCustomer(int product, int quantity)
        {
            return SpawnCustomerWithProducts(new[] { product }, new[] { quantity });
        }

        public bool SpawnCustomer(int product, int quantity, int secondaryProduct, int secondaryQuantity)
        {
            return secondaryProduct < 0
                ? SpawnCustomerWithProducts(new[] { product }, new[] { quantity })
                : SpawnCustomerWithProducts(new[] { product, secondaryProduct }, new[] { quantity, secondaryQuantity });
        }

        public bool SpawnCustomerWithProducts(int[] products, int[] quantities)
        {
            if (customers.Count >= Mathf.Clamp(balance.maxCustomers, 1, 21) || products == null || quantities == null ||
                products.Length < 1 || products.Length > StreetCustomer.MaxOrderProducts || products.Length != quantities.Length) return false;
            int maxQuantity = LevelIndex <= 2 ? 4 : 999;
            int[] boundedQuantities = new int[quantities.Length];
            for (int i = 0; i < products.Length; i++)
            {
                if (!IsProductAvailable(products[i])) return false;
                for (int prior = 0; prior < i; prior++) if (products[prior] == products[i]) return false;
                boundedQuantities[i] = Mathf.Clamp(quantities[i], 1, maxQuantity);
            }
            int slot = FindQueueTailSlot();
            if (slot < 0) return false;
            Vector2 target = QueuePosition(slot);
            bool left = (nextCustomer & 1) == 0;
            var customer = new StreetCustomer { Id = nextCustomer++, Position = new Vector2(left ? -40 : 580, target.y),
                Target = target, State = StreetCustomerState.Entering, Patience = Mathf.Max(0.1f, balance.customerPatienceSeconds),
                PatienceFraction = 1f, Slot = slot };
            customer.SetOrder(products, boundedQuantities);
            customers.Add(customer);
            return true;
        }

        public static Vector2 StationPosition(int product) => StreetWorkstationLayout.PickupPosition(Mathf.Clamp(product, 0, 6));
        public static Vector2 StationPositionForLevel(int product, int levelIndex, int productCount) => StationPosition(product);
        public static Vector2 StationApproachPoint() => StreetWorkstationLayout.ApproachPosition(0);
        public static Vector2 StationApproachPointForLevel(int product, int levelIndex) => StreetWorkstationLayout.ApproachPosition(product);
        public static Vector2 StationPositionForProductCount(int product, int productCount) => StationPosition(product);
        public static float CounterHandoffYForLevel(int levelIndex) => StreetSceneLayout.WorkerServiceY;
        private Vector2 WorkerHomePosition => new Vector2(433f, StreetSceneLayout.WorkerServiceY);
        private Vector2 CounterHandoffPosition(StreetCustomer customer) =>
            new Vector2(customer.Target.x, StreetSceneLayout.WorkerServiceY);
        private void StepSlice(float dt)
        {
            Kitchen.Advance(dt, WorkRate, ParrilleroCount > 0, ParrilleroPremiumCount > 0,
                CocacoleroCount > 0, FerneteroCount > 0);
            float duration = LevelDuration;
            Elapsed = Mathf.Min(duration, Elapsed + dt);
            if (Elapsed >= duration) { Phase = GoalReached ? RoundPhase.Won : RoundPhase.Lost; return; }
            arrival -= dt;
            if (arrival <= 0f && customers.Count < DemandCapacity())
            {
                float pressure = balance.levelDemandMultipliers != null && LevelIndex < balance.levelDemandMultipliers.Length
                    ? Mathf.Max(.1f, balance.levelDemandMultipliers[LevelIndex]) : DefaultDemandMultipliers[LevelIndex];
                // Keep the established arrival curve; the reference price is fixed, so payouts cannot affect demand.
                float demandFactor = .55f / Mathf.Pow(Mathf.Max(.08f, DemandFraction), Mathf.Max(.1f, balance.priceSensitivity));
                SpawnQuotaBalancedOrder();
                arrival = Mathf.Max(.05f, balance.customerArrivalSeconds * demandFactor / pressure);
            }
            AdvanceCustomers(dt);
            AdvanceWorkers(dt);
            for (int i = sales.Count - 1; i >= 0; i--) { sales[i].Age += dt; if (sales[i].Age > 3f) sales.RemoveAt(i); }
            if (GoalReached) Phase = RoundPhase.Won;
        }

        private int DemandCapacity()
        {
            int cap=Mathf.Clamp(balance.maxCustomers,1,21);
            return DemandFraction>=.8f?cap:Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1,cap,DemandFraction*DemandFraction)),1,cap);
        }
        private void SpawnQuotaBalancedOrder()
        {
            var eligible = new List<int>(ProductCount);
            var weights = new List<float>(ProductCount);
            for (int slot = 0; slot < ProductCount; slot++)
            {
                int product = GetAvailableProduct(slot);
                eligible.Add(product);
                weights.Add(ProductDemandWeight(product));
            }
            if (eligible.Count == 0) return;

            int maxOrderSize = Mathf.Min(StreetCustomer.MaxOrderProducts, eligible.Count);
            int orderWeightTotal = 0;
            for (int size = 1; size <= maxOrderSize; size++) orderWeightTotal += maxOrderSize + 2 - size;
            int orderRoll = random.Next(orderWeightTotal), orderSize = 1;
            for (int size = 1; size <= maxOrderSize; size++)
            {
                orderRoll -= maxOrderSize + 2 - size;
                if (orderRoll < 0) { orderSize = size; break; }
            }
            int[] products = new int[orderSize], quantities = new int[orderSize];
            var pool = new List<int>(eligible);
            var poolWeights = new List<float>(weights);
            for (int i = 0; i < orderSize; i++)
            {
                int selected = WeightedProductIndex(poolWeights);
                products[i] = pool[selected];
                quantities[i] = random.Next(1, 5); // Preserve the existing 1–4 units per product.
                pool.RemoveAt(selected); poolWeights.RemoveAt(selected);
            }
            // Sort each product together with its quantity to preserve stable catalog order.
            for (int i = 0; i < orderSize; i++)
            for (int j = i + 1; j < orderSize; j++)
                if (products[j] < products[i]) { int p = products[i]; products[i] = products[j]; products[j] = p; int q = quantities[i]; quantities[i] = quantities[j]; quantities[j] = q; }
            SpawnCustomerWithProducts(products, quantities);
        }

        private int WeightedProductIndex(List<float> weights)
        {
            float total = 0f; for (int i = 0; i < weights.Count; i++) total += Mathf.Max(.01f, weights[i]);
            float roll = (float)random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++) { roll -= Mathf.Max(.01f, weights[i]); if (roll <= 0f) return i; }
            return weights.Count - 1;
        }

        private float ProductDemandWeight(int product)
        {
            int goal = GetProductGoal(product);
            if (goal <= 0) return .01f;
            int remaining = GetProductRemaining(product);
            if (remaining <= 0) return .10f; // Completed goods stay available for genuine extra sales.
            int alreadyRequested = 0;
            for (int i = 0; i < customers.Count; i++)
            {
                StreetCustomer customer = customers[i];
                if (customer.State == StreetCustomerState.Leaving) continue;
                StreetOrderLine line = customer.FindOrderLine(product);
                if (line != null) alreadyRequested += line.Remaining;
            }
            int unrequested = Mathf.Max(0, remaining - alreadyRequested);
            float timeFraction = Mathf.Clamp01(TimeRemaining / Mathf.Max(1f, LevelDuration));
            float relativePace = Mathf.Clamp((remaining / (float)goal) / Mathf.Max(.05f, timeFraction), .25f, 4f);
            float quotaNeed = Mathf.Pow(unrequested / (float)goal, 1.25f);
            return .08f + quotaNeed * relativePace;
        }

        private float LevelDuration => LevelValue(balance.levelDurations, LevelIndex, DefaultDurations);

        private void AdvanceCustomers(float dt)
        {
            for (int i = customers.Count - 1; i >= 0; i--)
            {
                StreetCustomer c = customers[i]; c.AnimationTime += dt;
                if (c.State == StreetCustomerState.Entering || c.State == StreetCustomerState.Advancing ||
                    (c.State == StreetCustomerState.Receiving && c.Position != c.Target))
                {
                    c.Position = Move(c.Position, c.Target, balance.customerSpeed, dt);
                    if (c.Position == c.Target && c.State != StreetCustomerState.Receiving) c.State = StreetCustomerState.Waiting;
                }
                if (c.State == StreetCustomerState.Waiting || c.State == StreetCustomerState.Advancing)
                { c.Patience -= dt; c.PatienceFraction = Mathf.Clamp01(c.Patience / Mathf.Max(0.1f, balance.customerPatienceSeconds)); }
                if (c.Patience <= 0f && c.State != StreetCustomerState.Leaving) SetLeaving(c);
                if (c.State == StreetCustomerState.Leaving) { c.Position = Move(c.Position, c.Target, 180f, dt); if (c.Position == c.Target) RemoveCustomer(c); }
                if(c.State==StreetCustomerState.Receiving) { c.ReceiveRemaining-=dt; if(c.ReceiveRemaining<=0f) { if(!HasOutstandingItems(c))SetLeaving(c);else c.State=c.Position==c.Target?StreetCustomerState.Waiting:StreetCustomerState.Advancing; } }
            }
        }

        private void MoveWorker(StreetWorker worker, float dt)
        {
            Vector2 previous = worker.Position;
            worker.Position = Move(previous, worker.Target, balance.workerSpeed * WorkRate, dt);
            Vector2 delta = worker.Position - previous;
            worker.StepDistance = delta.magnitude;
            worker.TravelDistance += worker.StepDistance;
            if (worker.StepDistance > .0001f) worker.FacingVector = delta;
        }

        private void AdvanceWorkers(float dt)
        {
            foreach (StreetWorker w in workers)
            {
                w.AnimationTime += dt;
                w.StepDistance = 0f;
                if (w.State == StreetWorkerState.Idle) { Assign(w); continue; }
                if (!IsAtCounter(w.Customer) || !customers.Contains(w.Customer)) { CancelAssignment(w); continue; }
                if (w.State == StreetWorkerState.ToStation)
                {
                    MoveWorker(w, dt);
                    if (w.Position == w.Target && AdvanceWorkerRoute(w))
                    {
                        w.State = StreetWorkerState.Pickup;
                        w.Delay = balance.pickupSeconds / WorkRate;
                        w.PickupDuration = w.Delay;
                        w.PickupProgress = 0f;
                    }
                }
                else if (w.State == StreetWorkerState.Pickup)
                {
                    w.Delay -= dt;
                    w.PickupProgress = 1f - Mathf.Clamp01(w.Delay / Mathf.Max(.001f, w.PickupDuration));
                    if (w.Delay <= 0f)
                    {
                        StreetFoodUnit item = Kitchen.TryTake(w.Product, w.Role, w.Id);
                        if (item == null)
                        {
                            w.Delay = balance.pickupSeconds / WorkRate;
                            w.PickupDuration = w.Delay;
                            w.PickupProgress = 0f;
                            continue;
                        }
                        w.CarriedItemId = item.Id;
                        w.State = StreetWorkerState.ToCounter;
                        if (!SetWorkerRoute(w, CounterHandoffPosition(w.Customer))) { CancelAssignment(w); continue; }
                    }
                }
                else if (w.State == StreetWorkerState.ToCounter)
                {
                    MoveWorker(w, dt);
                    if (w.Position == w.Target && AdvanceWorkerRoute(w))
                    { w.State = StreetWorkerState.Handoff; w.Delay = balance.pickupSeconds / WorkRate; }
                }
                else if (w.State == StreetWorkerState.Handoff)
                {
                    w.Delay -= dt;
                    if (w.Delay <= 0f)
                    {
                        Deliver(w);
                        // Stop at the goal handoff, before another worker can sell in this slice.
                        if (GoalReached)
                        {
                            Phase = RoundPhase.Won;
                            return;
                        }
                    }
                }
            }
        }

        private void Assign(StreetWorker worker)
        {
            // customers is append-ordered; walking it directly preserves FIFO priority instead of rotating workers.
            for (int i = 0; i < customers.Count; i++)
            {
                StreetCustomer customer = customers[i];
                if (customer.State != StreetCustomerState.Waiting || !IsAtCounter(customer)) continue;
                StreetOrderLine line = NextOrderLineToServe(customer, worker);
                if (line == null || line.OwnerWorkerId != 0) continue;
                ClaimAndDispatch(worker, customer, line);
                return;
            }
        }

        private StreetOrderLine NextOrderLineToServe(StreetCustomer customer, StreetWorker worker)
        {
            if (HasSpecialtyWorkers)
            {
                for (int i = 0; i < customer.OrderLineCount; i++)
                {
                    StreetOrderLine line = customer.GetOrderLine(i);
                    if (line.Remaining > 0 && IsWorkerResponsibleFor(worker, line.Product) &&
                        line.OwnerWorkerId != 0 && line.OwnerWorkerId != worker.Id) return null;
                }
                for (int i = 0; i < customer.OrderLineCount; i++)
                {
                    StreetOrderLine line = customer.GetOrderLine(i);
                    if (!IsWorkerResponsibleFor(worker, line.Product)) continue;
                    if (line.Remaining > line.Reserved && (line.OwnerWorkerId == 0 || line.OwnerWorkerId == worker.Id)) return line;
                }
                return null;
            }

            // Floresta keeps its generalist worker and ordered ticket completion.
            for (int i = 0; i < customer.OrderLineCount; i++)
            {
                StreetOrderLine line = customer.GetOrderLine(i);
                if (line.Remaining <= 0) continue;
                if (line.Remaining <= line.Reserved || (line.OwnerWorkerId != 0 && line.OwnerWorkerId != worker.Id)) return null;
                return line;
            }
            return null;
        }

        private bool IsWorkerResponsibleFor(StreetWorker worker, int product)
        {
            return StreetSpecialties.IsResponsible(worker.Role, product);
        }

        private void ClaimAndDispatch(StreetWorker worker, StreetCustomer customer, StreetOrderLine line)
        {
            line.OwnerWorkerId = worker.Id;
            if (HasSpecialtyWorkers)
            {
                // The owner reserves their complete food/drink share while moving one unit at a time.
                for (int i = 0; i < customer.OrderLineCount; i++)
                {
                    StreetOrderLine owned = customer.GetOrderLine(i);
                    if (owned.Remaining > 0 && IsWorkerResponsibleFor(worker, owned.Product)) owned.OwnerWorkerId = worker.Id;
                }
            }
            line.Reserved++;
            worker.Product = line.Product;
            worker.CarriedItemId = 0;
            worker.Customer = customer;
            worker.CustomerId = customer.Id;
            DispatchToStation(worker);
        }

        private void DispatchToStation(StreetWorker worker)
        {
            worker.PickupProgress = 0f;
            worker.PickupDuration = 0f;
            if (!SetWorkerRoute(worker, KitchenLayout.PickupPosition(worker.Product))) { CancelAssignment(worker); return; }
            worker.State = StreetWorkerState.ToStation;
        }

        private bool SetWorkerRoute(StreetWorker worker, Vector2 destination)
        {
            worker.Route = KitchenLayout.FindShortestSafeRoute(worker.Position, destination);
            if (worker.Route == null)
            {
                worker.RouteIndex = 0;
                worker.Target = worker.Position;
                return false;
            }
            worker.RouteIndex = 0;
            worker.Target = worker.Route.Length > 0 ? worker.Route[0] : destination;
            return true;
        }

        private static bool AdvanceWorkerRoute(StreetWorker worker)
        {
            worker.RouteIndex++;
            if (worker.Route != null && worker.RouteIndex < worker.Route.Length)
            {
                worker.Target = worker.Route[worker.RouteIndex];
                return false;
            }
            worker.Route = null;
            worker.RouteIndex = 0;
            return true;
        }

        private static bool HasOutstandingItems(StreetCustomer customer) =>
            customer.PendingOrderLineCount > 0 || customer.Reserved > 0;

        private void Deliver(StreetWorker worker)
        {
            StreetCustomer customer = worker.Customer;
            StreetOrderLine line = customer != null ? customer.FindOrderLine(worker.Product) : null;
            bool correctSpecialty = IsWorkerResponsibleFor(worker, worker.Product);
            if (IsAtCounter(customer) && customers.Contains(customer) && correctSpecialty && line != null &&
                line.OwnerWorkerId == worker.Id && line.Reserved > 0 && line.Remaining > 0 &&
                worker.CarriedItemId > 0 && Kitchen.Consume(worker.CarriedItemId, worker.Product, worker.Id))
            {
                worker.CarriedItemId = 0;
                line.Reserved--;
                line.Remaining--;
                customer.State = StreetCustomerState.Receiving; customer.ReceiveRemaining = .3f;
                Delivered++;
                deliveredByProduct[worker.Product]++;
                int amount = Mathf.Max(0, Mathf.RoundToInt(GetProductPrice(worker.Product)));
                Coins += amount; CoinsEarned += amount; sales.Add(new StreetSale { Id = nextSale++, Amount = amount, Position = new Vector2(customer.Target.x, 400) });
                customer.Patience = Mathf.Max(0.1f, balance.deliveryPatienceRefreshSeconds);
                customer.PatienceFraction = Mathf.Clamp01(customer.Patience / Mathf.Max(0.1f, balance.customerPatienceSeconds));
                if (sales.Count > 40) sales.RemoveAt(0);

                if (line.Remaining > 0)
                {
                    // The same owner reserves and carries the next unit; nobody else can take this line.
                    line.Reserved++;
                    DispatchToStation(worker);
                    return;
                }

                if (HasSpecialtyWorkers)
                {
                    // Keep the assigned client until every line for this worker's whole specialty is complete.
                    StreetOrderLine nextPart = NextOrderLineToServe(customer, worker);
                    if (nextPart != null)
                    {
                        ClaimAndDispatch(worker, customer, nextPart);
                        return;
                    }
                    ReleaseOwnedLines(customer, worker.Id);
                }
                else
                {
                    line.OwnerWorkerId = 0;
                    StreetOrderLine next = NextOrderLineToServe(customer, worker);
                    if (next != null)
                    {
                        ClaimAndDispatch(worker, customer, next);
                        return;
                    }
                }
                ResetWorker(worker);
            }
            else { CancelAssignment(worker); return; }
        }

        private void RemoveCustomer(StreetCustomer c)
        {
            int column = c.Slot >= 0 ? c.Slot % QueueColumns : -1;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Customer == c) CancelAssignment(workers[i]);
            customers.Remove(c);
            if (column >= 0) CompactQueue(column);
        }
        private void SetLeaving(StreetCustomer c)
        {
            if (c.State == StreetCustomerState.Leaving) return;
            int column = c.Slot % QueueColumns;
            c.State = StreetCustomerState.Leaving; c.Target = new Vector2(c.Position.x < 270 ? -50 : 590, c.Position.y);
            c.Slot = -1; // No longer in the queue, but still counted until the exit walk ends.
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Customer == c) CancelAssignment(workers[i]);
            CompactQueue(column);
        }
        private void CompactQueue(int column)
        {
            int nextSlot = column;
            // Front-to-back traversal preserves the same-lane order of existing customers.
            for (int slot = column; slot < QueueSlots; slot += QueueColumns)
            for (int i = 0; i < customers.Count; i++)
            {
                StreetCustomer c = customers[i];
                if (c.Slot != slot || c.State == StreetCustomerState.Leaving) continue;
                if (c.Slot != nextSlot)
                {
                    for (int w = 0; w < workers.Count; w++)
                        if (workers[w].Customer == c) CancelAssignment(workers[w]);
                    c.Slot = nextSlot;
                    c.Target = QueuePosition(nextSlot);
                    if (c.State == StreetCustomerState.Waiting) c.State = StreetCustomerState.Advancing;
                }
                nextSlot += QueueColumns;
                break;
            }
        }
        private int FindQueueTailSlot()
        {
            int max = Mathf.Clamp(balance.maxCustomers, 1, QueueSlots), result = -1;
            for (int column = 0; column < QueueColumns; column++)
            {
                int tail = column;
                for (int i = 0; i < customers.Count; i++)
                {
                    int slot = customers[i].Slot;
                    if (slot >= 0 && slot % QueueColumns == column) tail = Mathf.Max(tail, slot + QueueColumns);
                }
                // Only append behind existing people; never reuse a hole in front of them.
                if (tail < max && (result < 0 || tail < result)) result = tail;
            }
            return result;
        }
        private static Vector2 QueuePosition(int slot)
        {
            return new Vector2(58 + 70 * (slot % QueueColumns), FrontQueueY - QueueRowSpacing * (slot / QueueColumns));
        }
        private static bool IsAtCounter(StreetCustomer c)
        {
            return c != null && c.Slot >= 0 && c.Slot < QueueColumns && c.Position == c.Target &&
                   (c.State == StreetCustomerState.Waiting || c.State == StreetCustomerState.Receiving);
        }
        private static void ReleaseOwnedLines(StreetCustomer customer, int workerId)
        {
            if (customer == null || workerId <= 0) return;
            for (int i = 0; i < customer.OrderLineCount; i++)
            {
                StreetOrderLine line = customer.GetOrderLine(i);
                if (line.OwnerWorkerId == workerId) line.OwnerWorkerId = 0;
            }
        }

        private void CancelAssignment(StreetWorker worker)
        {
            if (worker.CarriedItemId > 0) Kitchen.ReturnCarried(worker.CarriedItemId, worker.Id);
            if (worker.Customer != null)
            {
                for (int i = 0; i < worker.Customer.OrderLineCount; i++)
                {
                    StreetOrderLine line = worker.Customer.GetOrderLine(i);
                    if (line.OwnerWorkerId != worker.Id) continue;
                    // Only the active trip owns a reserved unit; other owned food lines are waiting their turn.
                    if (line.Product == worker.Product && line.Reserved > 0) line.Reserved--;
                    line.OwnerWorkerId = 0;
                }
            }
            ResetWorker(worker);
        }
        private static int LevelValue(int[] values, int index, int[] fallback) { return values != null && index < values.Length ? Mathf.Max(1, values[index]) : fallback[index]; }
        private static float LevelValue(float[] values, int index, float[] fallback) { return values != null && index < values.Length ? Mathf.Max(.01f, values[index]) : fallback[index]; }
        private void ResetTeamAndSpeed()
        {
            Kitchen = new StreetKitchenProduction(balance.kitchen, availableProductsByLevel[LevelIndex]);
            workers.Clear();
            nextWorker = 1;
            SpeedLevel = 0; // Zero purchased upgrades means the displayed speed is x1.00.
            AddWorker();
        }
        private void AddWorker() { AddWorker(StreetWorkerRole.Parrillero); }
        private void AddWorker(StreetWorkerRole role)
        {
            workers.Add(new StreetWorker { Id = nextWorker++, Role = role, Product = -1,
                Position = WorkerHomePosition, Target = WorkerHomePosition, State = StreetWorkerState.Idle });
        }
        private void ResetWorker(StreetWorker w) { w.StepDistance = 0f; w.PickupProgress = 0f; w.PickupDuration = 0f; w.Customer = null; w.CustomerId = 0; w.Product = -1; w.CarriedItemId = 0; w.State = StreetWorkerState.Idle; w.Route = null; w.RouteIndex = 0; w.Target = w.Position; }
        private static Vector2 Move(Vector2 from, Vector2 to, float speed, float dt) { return Vector2.MoveTowards(from, to, speed * dt); }
    }
}
