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
        public int[] hireBaseCostsByRole = new int[5];
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
        public int[] roleHireCostProfileIds = { -1, -1, -1, -1, -1 };
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
        [Min(0f)] public float minPrice = 0f;
        [Min(0f)] public float maxPrice = 60f;
        public float[] productPriceMultipliers = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        public StreetKitchenBalance kitchen = new StreetKitchenBalance();
        public StreetMarketBalance market = new StreetMarketBalance();
        [Min(1)] public int minOrderQuantity = 999;
        [Min(1)] public int maxOrderQuantity = 999;
        [Min(0f)] public float initialPrice = 5f;
        public int[] levelProductCounts = { 1, 2, 3, 5, 7, 7, 7, 7, 7, 7, 7 };
        [Tooltip("Comma-separated stable product IDs, in each level's display/order priority.")]
        public string[] levelProductIds = { "0", "0,4", "0,1,4", "0,1,2,4,6", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5", "0,1,2,3,4,6,5" };
        [HideInInspector, Tooltip("Legacy aggregate goals retained for serialized-scene compatibility; individual levelBalances are authoritative.")]
        public int[] levelGoals = { 200, 200, 65, 85, 110, 121, 133, 146, 161, 177, 195 };
        public float[] levelDurations = { 120f, 150f, 180f, 210f, 240f, 260f, 270f, 295f, 300f, 300f, 315f };
        [Tooltip("Official per-level goals, role hire base costs, speed-cost multiplier, and opening attempt balance.")]
        public StreetLevelBalance[] levelBalances = CreateOfficialLevelBalances();

        private static StreetLevelBalance[] CreateOfficialLevelBalances()
        {
            return new[]
            {
                new StreetLevelBalance(new[] {200,0,0,0,0,0,0}, new[] {15,0,0,0}, 1f),
                new StreetLevelBalance(new[] {200,0,0,0,200,0,0}, new[] {20,20,0,0}, 1.5f, 10),
                new StreetLevelBalance(new[] {180,120,0,0,150,0,0}, new[] {25,25,0,0}, 2.25f),
                new StreetLevelBalance(new[] {185,125,60,0,155,0,75}, new[] {30,30,35,0}, 3.25f),
                new StreetLevelBalance(new[] {190,130,70,65,160,65,80}, new[] {35,35,45,40}, 4.50f),
                new StreetLevelBalance(new[] {210,145,80,75,175,75,90}, new[] {40,40,50,200,50}, 6.00f, 250),
                new StreetLevelBalance(new[] {230,165,100,85,195,80,105}, new[] {45,45,55,50,55}, 8.00f, 250),
                new StreetLevelBalance(new[] {250,180,110,95,210,90,115}, new[] {50,50,65,55,60}, 10.50f, 250),
                new StreetLevelBalance(new[] {275,195,120,110,225,100,125}, new[] {55,55,70,65,65}, 13.50f, 250),
                new StreetLevelBalance(new[] {310,220,135,125,255,110,145}, new[] {60,60,80,70,70}, 17.00f, 500),
                new StreetLevelBalance(new[] {345,245,150,140,285,120,165}, new[] {70,70,90,80,80}, 21.00f, 2000)
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
            if (roleIndex < 0 || roleIndex > 4) return 0;
            if (role == StreetWorkerRole.Mozo && levelIndex < 5) return 0;
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
    public enum StreetWorkerState { Idle, ToStation, Pickup, ToCounter, Handoff, ToGrill, Cooking, ToTable, Deposit, ToCookQueue }
    public enum StreetWorkerRole { Parrillero = 0, Cocacolero = 1, ParrilleroPremium = 2, Fernetero = 3, Mozo = 4 }

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
        public float AnimationTime { get; internal set; }
        internal float ReceiveRemaining;
        internal int Slot;
        internal bool DeliverySoundPlayed;
        internal bool WasServed;

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
        public Vector2 WaitPosition { get; internal set; }
        public StreetWorkerState State { get; internal set; }
        public float AnimationTime { get; internal set; }
        public float PickupProgress { get; internal set; }
        public float CookingProgress { get; internal set; }
        public float CookingCycleProgress { get; internal set; }
        public bool VisualCooking { get; internal set; }
        public int VisualCookingProduct { get; internal set; } = -1;
        public int CookingItemId { get; internal set; }
        // Stable logical anchor retained through grill approach, cooking, and table departure.
        internal Vector2 CookingWorkPosition;
        // Keeps an already-visible grill contact continuous when resuming real cooking.
        internal float GrillContactBlendStart;
        // A cook only owns a physical grill station while its index is non-negative.
        public int CookStationIndex { get; internal set; } = -1;
        internal long CookQueueTicket = long.MaxValue;
        // Visual telemetry only; movement math and service timing are unchanged.
        public float TravelDistance { get; internal set; }
        public float StepDistance { get; internal set; }
        // Presentation telemetry only: lets station-facing visual corrections follow route progress linearly.
        public float RouteDistance { get; internal set; }
        public Vector2 FacingVector { get; internal set; }
        internal float Delay;
        internal float PickupDuration;
        internal Vector2[] Route;
        internal int RouteIndex;
        internal StreetCustomer Customer;
        internal bool ReturningToWaitPosition;
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
        // Player-facing captions consistently show the club/team first and its neighborhood second.
        // LevelNames remain stable theme keys consumed by ClubVisualTheme.
        public static readonly string[] LevelDisplayNames = {
            "All Boys / Floresta", "Nueva Chicago / Mataderos", "Vélez Sarsfield / Liniers",
            "Ferro / Caballito", "Independiente / Avellaneda", "Racing Club / Avellaneda",
            "San Lorenzo / Boedo", "River Plate / Núñez", "Boca Juniors / La Boca",
            "Sindicato de Camioneros / Plaza de Mayo", "Los Redondos / Tandil"
        };
        private static readonly int[] DefaultProductCounts = { 1, 2, 3, 5, 7, 7, 7, 7, 7, 7, 7 };
        private static readonly int[][] DefaultLevelProducts = {
            new[] { 0 }, new[] { 0, 4 }, new[] { 0, 1, 4 },
            new[] { 0, 1, 2, 4, 6 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
            new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 },
            new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }, new[] { 0, 1, 2, 3, 4, 6, 5 }
        };
        private static readonly float[] DefaultDemandMultipliers = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.65f, 1.815f, 1.9965f, 2.19615f, 2.415765f, 2.6573415f };
        private static readonly int[] DefaultSpeedCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        private static readonly float[] DefaultDurations = { 120f, 150f, 180f, 210f, 240f, 260f, 270f, 295f, 300f, 300f, 315f };
        private const int MaxHireCost = int.MaxValue - int.MaxValue % 5;
        private readonly StreetBalance balance;
        private readonly int[][] availableProductsByLevel;
        private readonly List<StreetCustomer> customers = new List<StreetCustomer>();
        private readonly List<StreetWorker> workers = new List<StreetWorker>();
        private readonly List<StreetSale> sales = new List<StreetSale>();
        private float arrival;
        private int nextCustomer = 1, nextWorker = 1, nextSale = 1;
        private readonly int[] deliveredByProduct = new int[7];
        private int customersAttended, ordersCompleted;
        private float price;
        private System.Random random;
        private IStreetKitchenStock ingredientStock;

        public event Action OrderCompleted;

        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public int LevelIndex { get; private set; }
        public float Price { get { return price; } }
        public int Coins { get; private set; }
        public int CoinsEarned { get; private set; }
        public int Delivered { get; private set; }
        public int CustomersAttended => customersAttended;
        // Kept for statistics/save compatibility; wait-time losses are no longer part of gameplay.
        public int CustomersLost => 0;
        public int OrdersCompleted => ordersCompleted;
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
                    StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Fernetero, StreetWorkerRole.Mozo };
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
        public bool UsesActiveCooks => LevelIndex >= 5;
        public int MozoCount => WorkerCount(StreetWorkerRole.Mozo);
        public bool HasMozo => HasRole(StreetWorkerRole.Mozo);
        public int MozoHireCost => HireCostForRole(StreetWorkerRole.Mozo);
        public bool CanHireMozo => CanHireRole(StreetWorkerRole.Mozo);
        public bool HasRole(StreetWorkerRole role) { return role == StreetWorkerRole.Mozo
            ? UsesActiveCooks && (IsProductAvailable(0) || IsProductAvailable(1) || IsProductAvailable(2) || IsProductAvailable(3)) : StreetSpecialties.CatalogHasRole(availableProductsByLevel[LevelIndex], role); }
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
                : role == StreetWorkerRole.ParrilleroPremium && LevelIndex == 5
                    ? Math.Max(1, WorkerCount(role)) // Racing's opening Premium Parrillero is free.
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
                   HasRole(role) && (role != StreetWorkerRole.Mozo || Phase == RoundPhase.Playing) && hireCost > 0 && Coins >= hireCost;
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
        public float CookWorkRate { get { return 1f + SpeedLevel * 0.06f; } }
        private float WorkRateFor(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium
                ? CookWorkRate : WorkRate;
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
            KitchenLayout = new StreetKitchenLayout(availableProductsByLevel[LevelIndex], UsesActiveCooks);
        }

        public void StartRound()
        {
            customers.Clear(); sales.Clear();
            Coins = balance.GetStartingCoins(LevelIndex); CoinsEarned = 0; // Opening balance is a configured attempt grant, not earned revenue.
            Elapsed = 0f; Delivered = 0; customersAttended = ordersCompleted = 0;
            Array.Clear(deliveredByProduct, 0, deliveredByProduct.Length);
            // Every round entry is a fresh attempt, regardless of selected club.
            ResetTeamAndSpeed();
            foreach (StreetWorker worker in workers) ResetWorker(worker);
            arrival = 0f; Phase = RoundPhase.Playing;
        }
        public void BindIngredientStock(IStreetKitchenStock stock)
        {
            if (Phase != RoundPhase.Ready) throw new InvalidOperationException("Bind ingredients before starting the match.");
            ingredientStock = stock;
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
            workers.Clear(); nextWorker = 1; nextCookQueueTicket = 0;
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
            customersAttended = ordersCompleted = 0;
            Array.Clear(deliveredByProduct, 0, deliveredByProduct.Length); Delivered = 0;
            return true;
        }
        public bool NextLevel()
        {
            if (Phase != RoundPhase.Won || LevelIndex + 1 >= LevelNames.Length) return false;
            LevelIndex++;
            RefreshKitchenLayout();
            ResetTeamAndSpeed();
            Coins = balance.GetStartingCoins(LevelIndex); CoinsEarned = 0;
            customersAttended = ordersCompleted = 0;
            Array.Clear(deliveredByProduct, 0, deliveredByProduct.Length); Delivered = 0;
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
                Target = target, State = StreetCustomerState.Entering, Slot = slot };
            customer.SetOrder(products, boundedQuantities);
            customers.Add(customer);
            return true;
        }

        public static Vector2 StationPosition(int product) => StreetWorkstationLayout.PickupPosition(Mathf.Clamp(product, 0, 6));
        public static Vector2 StationPositionForLevel(int product, int levelIndex, int productCount) => StationPosition(product);
        public static Vector2 StationApproachPoint() => StreetWorkstationLayout.ApproachPosition(0);
        public static Vector2 StationApproachPointForLevel(int product, int levelIndex) => StreetWorkstationLayout.ApproachPosition(product);
        public static Vector2 StationPositionForProductCount(int product, int productCount) => StationPosition(product);
        public static float CounterHandoffYForLevel(int levelIndex) => Mathf.Lerp(FrontQueueY, StreetSceneLayout.WorkerServiceY, .5f);
        private Vector2 WorkerWaitPosition(int workerId)
        {
            // Stagger rows across the counter-side aisle: six columns fit without clipping,
            // and the half-column offset keeps neighboring rows clear of each other's feet.
            const int columns = 6;
            const float columnSpacing = 78f, rowSpacing = 28f;
            int index = Mathf.Max(0, workerId - 1);
            int column = index % columns;
            int row = index / columns;
            float x = 45f + column * columnSpacing + ((row & 1) == 1 ? columnSpacing * .5f : 0f);
            x = Mathf.Clamp(x, StreetWorkstationLayout.WorkerHalfWidth, StreetSceneLayout.Width - StreetWorkstationLayout.WorkerHalfWidth);
            float y = StreetSceneLayout.WorkerServiceY + row * rowSpacing;
            return new Vector2(x, y);
        }

        private static Vector2 CounterHandoffPosition(StreetCustomer customer)
        {
            // Keep the customer's exact column, but halve the old vertical service gap.
            // Canvas Y grows downward, so this leaves the worker between the street edge and customer.
            return new Vector2(customer.Target.x,
                Mathf.Lerp(customer.Target.y, StreetSceneLayout.WorkerServiceY, .5f));
        }

        private static void FaceCustomerAtHandoff(StreetWorker worker)
        {
            if (worker == null || worker.Customer == null) return;
            Vector2 towardCustomer = worker.Customer.Target - worker.Position;
            if (towardCustomer.sqrMagnitude > .0001f) worker.FacingVector = towardCustomer;
        }

        private int WorkerAccessIndex(StreetWorker worker, int accessCount)
        {
            int index = 0;
            foreach (StreetWorker other in workers)
                if (other != worker && other.Role == worker.Role && other.Id < worker.Id) index++;
            return index % Mathf.Max(1, accessCount);
        }

        private Vector2 PickupPositionForWorker(StreetWorker worker)
        {
            // In levels 2 and 3 the Coca barrel is exposed on its own; keep every Cocacolero
            // on the centered approach instead of spreading access points sideways.
            if (worker.Product == 4 && (LevelIndex == 1 || LevelIndex == 2))
                return KitchenLayout.PickupPosition(worker.Product);
            return KitchenLayout.PickupPosition(worker.Product, WorkerAccessIndex(worker, 3), 3);
        }
        private void StepSlice(float dt)
        {
            Kitchen.Advance(dt, CookWorkRate, WorkRate, ParrilleroCount > 0, ParrilleroPremiumCount > 0,
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
                // A sold-out extra ticket must not block FIFO lanes behind it. Preserve its pending
                // quantities and normal departure/cancellation; never count it as a completed order.
                if (c.State == StreetCustomerState.Waiting && IsAtCounter(c) && OnlySoldOutExtrasRemain(c)) SetLeaving(c);
                if (c.State == StreetCustomerState.Leaving) { c.Position = Move(c.Position, c.Target, 180f, dt); if (c.Position == c.Target) RemoveCustomer(c); }
                if(c.State==StreetCustomerState.Receiving) { c.ReceiveRemaining-=dt; if(c.ReceiveRemaining<=0f) { if(!HasOutstandingItems(c))SetLeaving(c);else c.State=c.Position==c.Target?StreetCustomerState.Waiting:StreetCustomerState.Advancing; } }
            }
        }

        private bool OnlySoldOutExtrasRemain(StreetCustomer customer)
        {
            if (customer.Reserved > 0 || customer.PendingOrderLineCount == 0) return false;
            for (int i = 0; i < customer.OrderLineCount; i++)
            {
                StreetOrderLine line = customer.GetOrderLine(i);
                if (line.Remaining > 0 && (!IsProductGoalComplete(line.Product) || Kitchen.HasSupply(line.Product))) return false;
            }
            return true; // Unlimited/standalone kitchens always report supply, preserving their rules.
        }

        private void MoveWorker(StreetWorker worker, float dt)
        {
            Vector2 previous = worker.Position;
            float speed = balance.workerSpeed * WorkRateFor(worker.Role);
            worker.Position = Move(previous, worker.Target, speed, dt);
            Vector2 delta = worker.Position - previous;
            worker.StepDistance = delta.magnitude;
            worker.TravelDistance += worker.StepDistance;
            if (worker.StepDistance > .0001f) worker.FacingVector = delta;
        }

        private static bool IsWorkerTravelling(StreetWorker worker) => worker.ReturningToWaitPosition ||
            worker.State == StreetWorkerState.ToStation || worker.State == StreetWorkerState.ToCounter ||
            worker.State == StreetWorkerState.ToGrill || worker.State == StreetWorkerState.ToTable ||
            worker.State == StreetWorkerState.ToCookQueue;

        private void AdvanceIdleWorker(StreetWorker worker, float dt)
        {
            if (worker.Position == worker.WaitPosition)
            {
                worker.ReturningToWaitPosition = false;
                worker.Target = worker.WaitPosition;
                worker.Route = null;
                worker.RouteIndex = 0;
                return;
            }
            if (!worker.ReturningToWaitPosition)
            {
                if (!SetWorkerRoute(worker, worker.WaitPosition)) return;
                worker.ReturningToWaitPosition = true;
            }
            MoveWorker(worker, dt);
            if (worker.Position == worker.Target && AdvanceWorkerRoute(worker))
            {
                worker.Target = worker.WaitPosition;
                worker.ReturningToWaitPosition = false;
            }
        }

        private void AdvanceWorkers(float dt)
        {
            if (UsesActiveCooks) AssignAvailableCookStations();
            foreach (StreetWorker w in workers)
            {
                w.AnimationTime += dt;
                w.StepDistance = 0f;
                float workRate = WorkRateFor(w.Role);
                if (UsesActiveCooks && (w.Role == StreetWorkerRole.Parrillero || w.Role == StreetWorkerRole.ParrilleroPremium))
                { AdvanceCookWorker(w, dt); continue; }
                if (w.State == StreetWorkerState.Idle)
                {
                    Assign(w);
                    if (w.State == StreetWorkerState.Idle) AdvanceIdleWorker(w, dt);
                    continue;
                }
                if (!IsAtCounter(w.Customer) || !customers.Contains(w.Customer)) { CancelAssignment(w); continue; }
                if (!RefreshMozoPickupRoute(w)) continue;
                if (w.State == StreetWorkerState.ToStation)
                {
                    MoveWorker(w, dt);
                    if (w.Position == w.Target && AdvanceWorkerRoute(w))
                    {
                        w.State = StreetWorkerState.Pickup;
                        w.Delay = balance.pickupSeconds / workRate;
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
                            if (!Kitchen.HasSupply(w.Product)) { CancelAssignment(w); continue; }
                            w.Delay = balance.pickupSeconds / workRate;
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
                    {
                        // Stop first, turn toward the assigned customer, then use the existing handoff dwell.
                        FaceCustomerAtHandoff(w);
                        w.State = StreetWorkerState.Handoff;
                        w.Delay = balance.pickupSeconds / workRate;
                    }
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

        // Each specialty owns one shared cursor so real and visual cook turns use the same
        // deterministic alternation, independent of goals, stock, orders, or worker count.
        private int nextNormalCookProduct;
        private int nextPremiumCookProduct = 2;
        private const int MaxCookStationsPerGrill = StreetKitchenLayout.MaxCookStationsPerGrill;
        private long nextCookQueueTicket;

        private int CookStationCount(StreetWorkerRole role) => Mathf.Min(MaxCookStationsPerGrill, WorkerCount(role));

        private void AssignAvailableCookStations()
        {
            AssignAvailableCookStations(StreetWorkerRole.Parrillero);
            AssignAvailableCookStations(StreetWorkerRole.ParrilleroPremium);
        }

        private void AssignAvailableCookStations(StreetWorkerRole role)
        {
            int occupied = 0;
            foreach (StreetWorker worker in workers)
                if (worker.Role == role && worker.CookStationIndex >= 0 && worker.CookStationIndex < MaxCookStationsPerGrill)
                    occupied |= 1 << worker.CookStationIndex;

            for (int station = 0; station < MaxCookStationsPerGrill; station++)
            {
                int stationBit = 1 << station;
                if ((occupied & stationBit) != 0) continue;
                StreetWorker next = null;
                foreach (StreetWorker worker in workers)
                    if (worker.Role == role && worker.CookStationIndex < 0 && worker.State == StreetWorkerState.Idle &&
                        (next == null || worker.CookQueueTicket < next.CookQueueTicket)) next = worker;
                if (next == null) break;
                next.CookStationIndex = station;
                next.CookQueueTicket = long.MaxValue;
                occupied |= stationBit;
            }
        }

        private int CookQueueIndex(StreetWorker worker, out int queueCount)
        {
            int index = 0;
            queueCount = 0;
            foreach (StreetWorker candidate in workers)
            {
                if (candidate.Role != worker.Role || candidate.CookStationIndex >= 0 ||
                    (candidate.State != StreetWorkerState.Idle && candidate.State != StreetWorkerState.ToCookQueue))
                    continue;
                queueCount++;
                if (candidate.CookQueueTicket < worker.CookQueueTicket) index++;
            }
            return index;
        }

        private void EnqueueCookWorker(StreetWorker worker)
        {
            worker.CookStationIndex = -1;
            worker.CookQueueTicket = nextCookQueueTicket++;
        }

        private void ReleaseCookStation(StreetWorker worker)
        {
            worker.CookStationIndex = -1;
            worker.CookQueueTicket = long.MaxValue;
        }

        private void CookWorkerStation(StreetWorker worker, out int stationIndex, out int stationCount)
        {
            stationIndex = worker.CookStationIndex;
            stationCount = CookStationCount(worker.Role);
            if (stationCount == 0) stationCount = 1;
        }

        private int NearestFreeGrillSlot(StreetWorker worker, int product, Vector2 referencePosition)
        {
            int capacity = Kitchen.GrillCapacity(product);
            bool companionAvailable = IsProductAvailable(product ^ 1);
            const float minimumWorkerSeparation = 84f;
            int nearestSlot = -1;
            float nearestDistance = float.PositiveInfinity;
            int fallbackSlot = -1;
            float fallbackDistance = float.PositiveInfinity;
            float fallbackSeparation = -1f;
            for (int slot = 0; slot < capacity; slot++)
            {
                if (!Kitchen.IsGrillSlotFree(product, slot)) continue;
                Vector2 foodPosition = KitchenLayout.GrillSlotPosition(product, slot, companionAvailable, capacity);
                Vector2 workPosition = KitchenLayout.GrillSlotWorkPosition(product, foodPosition);
                float distance = (foodPosition - referencePosition).sqrMagnitude;
                float minimumSeparation = float.PositiveInfinity;
                foreach (StreetWorker other in workers)
                {
                    if (other == worker || other.CookingItemId <= 0 ||
                        (other.State != StreetWorkerState.ToGrill && other.State != StreetWorkerState.Cooking)) continue;
                    StreetFoodUnit otherUnit = Kitchen.Find(other.CookingItemId);
                    if (otherUnit == null || otherUnit.Product < 0 || otherUnit.Product > 3) continue;
                    int otherCapacity = Kitchen.GrillCapacity(otherUnit.Product);
                    bool otherCompanion = IsProductAvailable(otherUnit.Product ^ 1);
                    Vector2 otherFood = KitchenLayout.GrillSlotPosition(otherUnit.Product, otherUnit.Slot, otherCompanion, otherCapacity);
                    Vector2 otherWork = KitchenLayout.GrillSlotWorkPosition(otherUnit.Product, otherFood);
                    minimumSeparation = Mathf.Min(minimumSeparation, Vector2.Distance(workPosition, otherWork));
                }

                bool separationTied = minimumSeparation == fallbackSeparation ||
                    Mathf.Abs(minimumSeparation - fallbackSeparation) <= .001f;
                if (fallbackSlot < 0 || minimumSeparation > fallbackSeparation + .001f ||
                    (separationTied && distance < fallbackDistance))
                {
                    fallbackSeparation = minimumSeparation;
                    fallbackDistance = distance;
                    fallbackSlot = slot;
                }
                if (minimumSeparation >= minimumWorkerSeparation && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = slot;
                }
            }
            return nearestSlot >= 0 ? nearestSlot : fallbackSlot;
        }

        private void AdvanceCookWorker(StreetWorker worker, float dt)
        {
            if (worker.State == StreetWorkerState.ToCookQueue)
            {
                MoveWorker(worker, dt);
                if (worker.Position == worker.Target && AdvanceWorkerRoute(worker))
                {
                    worker.State = StreetWorkerState.Idle;
                    worker.Target = worker.Position;
                }
                return;
            }
            if (worker.State == StreetWorkerState.Idle)
            {
                if (worker.CookStationIndex < 0)
                {
                    int queueIndex = CookQueueIndex(worker, out int queueCount);
                    Vector2 waitingPoint = KitchenLayout.CookQueuePosition(worker.Role == StreetWorkerRole.Parrillero ? 0 : 2,
                        queueIndex, queueCount);
                    if (worker.Position != waitingPoint && SetWorkerRoute(worker, waitingPoint))
                        worker.State = StreetWorkerState.ToCookQueue;
                    return;
                }

                CookWorkerStation(worker, out int assignedStation, out int stationCount);
                int first = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
                Vector2 stationPoint = KitchenLayout.GrillWorkPosition(first, assignedStation, stationCount);

                // A visual turn keeps its assigned recipe. Only physical slot capacity can prevent
                // a real unit; objective progress and ingredient stock never gate active cooking.
                if (worker.VisualCooking)
                {
                    AdvanceVisualCookWorker(worker, dt);
                    return;
                }

                int chosen = worker.VisualCookingProduct;
                if (chosen < 0)
                {
                    chosen = SelectNextCookProduct(first);
                    worker.VisualCookingProduct = chosen;
                }
                bool routeFailed = false;
                if (chosen >= 0 && TryStartRealCook(worker, chosen, stationPoint, false, out routeFailed)) return;
                if (chosen >= 0 && routeFailed) return;

                // Keep the scheduled product while approaching the grill so arrival cannot
                // allocate a second turn or silently replace the one that was blocked.
                if (worker.Position != stationPoint)
                {
                    worker.CookingWorkPosition = Vector2.zero;
                    worker.GrillContactBlendStart = 0f;
                    if (SetWorkerRoute(worker, stationPoint)) worker.State = StreetWorkerState.ToGrill;
                    return;
                }
                AdvanceVisualCookWorker(worker, dt);
                return;
            }
            if (worker.State == StreetWorkerState.ToGrill)
            {
                MoveWorker(worker, dt);
                if (worker.Position == worker.Target && AdvanceWorkerRoute(worker))
                {
                    if (worker.CookingItemId > 0) worker.State = StreetWorkerState.Cooking;
                    else
                    {
                        worker.State = StreetWorkerState.Idle;
                        worker.Target = worker.Position;
                        AdvanceVisualCookWorker(worker, 0f);
                    }
                }
            }
            else if (worker.State == StreetWorkerState.Cooking)
            {
                float cookStep = dt * CookWorkRate / Mathf.Max(.01f, Kitchen.Balance.activeCookSeconds);
                worker.CookingCycleProgress = Mathf.Repeat(worker.CookingCycleProgress + cookStep, 1f);
                bool complete = Kitchen.AdvanceCook(worker.CookingItemId, worker.Id, dt, CookWorkRate);
                StreetFoodUnit unit = Kitchen.Find(worker.CookingItemId);
                worker.CookingProgress = unit == null ? 0f : unit.Progress;
                if (!complete) return;
                if (!Kitchen.TakeCooked(worker.CookingItemId, worker.Id)) return;
                worker.CarriedItemId = worker.CookingItemId;
                if (!SetWorkerRoute(worker, KitchenLayout.DepositPosition(worker.Product,
                    WorkerAccessIndex(worker, StreetKitchenLayout.MaxCookStationsPerGrill), StreetKitchenLayout.MaxCookStationsPerGrill)))
                { Kitchen.CancelCook(worker.Id); EnqueueCookWorker(worker); ResetWorker(worker); return; }
                // A post becomes available as soon as the cook leaves to deliver the unit.
                ReleaseCookStation(worker);
                worker.State = StreetWorkerState.ToTable;
            }
            else if (worker.State == StreetWorkerState.ToTable)
            {
                MoveWorker(worker, dt);
                if (worker.Position == worker.Target && AdvanceWorkerRoute(worker))
                { worker.State = StreetWorkerState.Deposit; worker.Delay = balance.pickupSeconds / CookWorkRate; }
            }
            else if (worker.State == StreetWorkerState.Deposit)
            {
                worker.Delay -= dt;
                if (worker.Delay > 0f) return;
                if (!Kitchen.DepositCooked(worker.CarriedItemId, worker.Id)) return;
                ResetWorker(worker);
                EnqueueCookWorker(worker);
            }
        }

        private void AdvanceVisualCookWorker(StreetWorker worker, float dt)
        {
            // Cosmetic cycles contain no food ID, inventory operation, ingredient reservation,
            // capacity reservation, or delivery route.
            if (worker.CookingItemId > 0 || worker.CarriedItemId > 0) return;
            int first = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
            if (worker.CookStationIndex < 0) return;
            CookWorkerStation(worker, out int stationIndex, out int stationCount);
            Vector2 grillPosition = KitchenLayout.GrillWorkPosition(first, stationIndex, stationCount);
            if (worker.Position != grillPosition)
            {
                // Preserve a pending/current visual turn and its phase if a station anchor moves.
                if (SetWorkerRoute(worker, grillPosition)) worker.State = StreetWorkerState.ToGrill;
                return;
            }

            if (worker.VisualCookingProduct < 0)
            {
                int newlyEligible = SelectNextCookProduct(first);
                if (newlyEligible >= 0)
                {
                    if (TryStartRealCook(worker, newlyEligible, grillPosition, true, out bool routeFailed)) return;
                    if (routeFailed) return;
                    // A concurrent cook may have claimed the last reservation after selection.
                    worker.VisualCookingProduct = SelectNextCookProduct(first);
                }
            }
            // If both tables are full, keep the Parrillero visibly busy with a generic
            // simulated cycle (product -1) that cannot create or represent real inventory.
            if (!worker.VisualCooking)
            {
                worker.VisualCooking = true;
                worker.CookingProgress = 0f;
            }

            float cookStep = dt * CookWorkRate / Mathf.Max(.01f, Kitchen.Balance.activeCookSeconds);
            float nextProgress = worker.CookingProgress + cookStep;
            worker.CookingCycleProgress = Mathf.Repeat(worker.CookingCycleProgress + cookStep, 1f);
            if (nextProgress >= 1f)
            {
                int nextProduct = SelectNextCookProduct(first);
                // Assign the next eligible global turn once at the cycle boundary. Capacity
                // reservations are rechecked by TryStartRealCook; if another cook just took
                // the final slot, reselect rather than showing a capped product cosmetically.
                if (nextProduct >= 0)
                {
                    if (TryStartRealCook(worker, nextProduct, grillPosition, true, out bool routeFailed)) return;
                    if (routeFailed) return;
                    nextProduct = SelectNextCookProduct(first);
                }
                worker.VisualCookingProduct = nextProduct;
            }
            worker.CookingProgress = Mathf.Repeat(nextProgress, 1f);
        }

        private bool TryStartRealCook(StreetWorker worker, int product, Vector2 stationPoint,
            bool wasVisuallyCooking, out bool routeFailed)
        {
            routeFailed = false;
            if (!IsProductAvailable(product) || !Kitchen.CanReserveCook(product)) return false;

            int preferredSlot = NearestFreeGrillSlot(worker, product, stationPoint);
            StreetFoodUnit unit = Kitchen.ReserveCook(product, worker.Id, preferredSlot);
            if (unit == null) return false;
            worker.Product = product;
            worker.CookingItemId = unit.Id;
            worker.CookingProgress = 0f;
            worker.VisualCooking = false;
            worker.VisualCookingProduct = -1;
            Vector2 foodPosition = KitchenLayout.GrillSlotPosition(product, unit.Slot,
                IsProductAvailable(product ^ 1), Kitchen.GrillCapacity(product));
            Vector2 cookingPosition = KitchenLayout.GrillSlotWorkPosition(product, foodPosition);
            worker.CookingWorkPosition = cookingPosition;
            worker.GrillContactBlendStart = wasVisuallyCooking || worker.Position == stationPoint ? 1f : 0f;
            if (!SetWorkerRoute(worker, cookingPosition))
            {
                Kitchen.CancelCook(worker.Id);
                ResetWorker(worker);
                routeFailed = true;
                return false;
            }
            worker.State = StreetWorkerState.ToGrill;
            if (worker.Position == worker.Target && AdvanceWorkerRoute(worker))
                worker.State = StreetWorkerState.Cooking;
            return true;
        }

        private int SelectNextCookProduct(int firstProduct)
        {
            int secondProduct = firstProduct + 1;
            bool firstAvailable = IsProductAvailable(firstProduct) && Kitchen.CanReserveCook(firstProduct);
            bool secondAvailable = IsProductAvailable(secondProduct) && Kitchen.CanReserveCook(secondProduct);
            int preferred = firstProduct == 0 ? nextNormalCookProduct : nextPremiumCookProduct;
            bool preferredAvailable = preferred == firstProduct ? firstAvailable : secondAvailable;
            bool alternateAvailable = preferred == firstProduct ? secondAvailable : firstAvailable;
            int selected = preferredAvailable ? preferred
                : alternateAvailable ? (preferred == firstProduct ? secondProduct : firstProduct) : -1;
            if (selected < 0) return -1;

            // When one product is full, keep its place in the shared sequence so it resumes
            // on the next turn after a Mozo frees table capacity.
            if (firstProduct == 0) nextNormalCookProduct = selected == firstProduct ? secondProduct : firstProduct;
            else nextPremiumCookProduct = selected == firstProduct ? secondProduct : firstProduct;
            return selected;
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
                    if (line.Remaining > line.Reserved && Kitchen.HasSupply(line.Product) &&
                        (worker.Role != StreetWorkerRole.Mozo || Kitchen.AvailableTableCount(line.Product, worker.Id) > 0) &&
                        (line.OwnerWorkerId == 0 || line.OwnerWorkerId == worker.Id)) return line;
                }
                return null;
            }

            // Floresta keeps its generalist worker and ordered ticket completion.
            for (int i = 0; i < customer.OrderLineCount; i++)
            {
                StreetOrderLine line = customer.GetOrderLine(i);
                if (line.Remaining <= 0) continue;
                if (!Kitchen.HasSupply(line.Product)) continue;
                if (line.Remaining <= line.Reserved || (line.OwnerWorkerId != 0 && line.OwnerWorkerId != worker.Id)) return null;
                return line;
            }
            return null;
        }

        private bool IsWorkerResponsibleFor(StreetWorker worker, int product)
        {
            return StreetSpecialties.IsDeliveryResponsible(worker.Role, product, UsesActiveCooks);
        }

        private void ClaimAndDispatch(StreetWorker worker, StreetCustomer customer, StreetOrderLine line)
        {
            if (worker.Role == StreetWorkerRole.Mozo && Kitchen.ReserveTableUnit(line.Product, worker.Id) == null) return;
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
            worker.ReturningToWaitPosition = false;
            worker.PickupProgress = 0f;
            worker.PickupDuration = 0f;
            Vector2 pickup = PickupPositionForWorker(worker);
            if (worker.Role == StreetWorkerRole.Mozo)
            {
                StreetFoodUnit reserved = Kitchen.ReserveTableUnit(worker.Product, worker.Id);
                if (reserved == null) { CancelAssignment(worker); return; }
                if (worker.Product < 4)
                {
                    bool companionAvailable = IsProductAvailable(worker.Product ^ 1);
                    pickup = KitchenLayout.TableSlotPickupPosition(worker.Product, reserved.Slot,
                        companionAvailable, Kitchen.TableCapacity(worker.Product));
                }
            }
            if (!SetWorkerRoute(worker, pickup)) { CancelAssignment(worker); return; }
            worker.State = StreetWorkerState.ToStation;
        }

        private bool RefreshMozoPickupRoute(StreetWorker worker)
        {
            if (!UsesActiveCooks || worker.Role != StreetWorkerRole.Mozo || worker.Product < 0 || worker.Product > 3 ||
                (worker.State != StreetWorkerState.ToStation && worker.State != StreetWorkerState.Pickup)) return true;

            StreetFoodUnit reserved = Kitchen.ReserveTableUnit(worker.Product, worker.Id);
            if (reserved == null) { CancelAssignment(worker); return false; }
            Vector2 destination = KitchenLayout.TableSlotPickupPosition(worker.Product, reserved.Slot,
                IsProductAvailable(worker.Product ^ 1), Kitchen.TableCapacity(worker.Product));
            Vector2 currentDestination = worker.Route != null && worker.Route.Length > 0
                ? worker.Route[worker.Route.Length - 1] : worker.Target;
            if ((destination - currentDestination).sqrMagnitude <= .0001f) return true;
            if (!SetWorkerRoute(worker, destination)) { CancelAssignment(worker); return false; }
            if (worker.State == StreetWorkerState.Pickup)
            {
                // A compacted unit moved while this Mozo was reaching for it. Re-approach
                // the same claimed ID instead of completing pickup at its former coordinate.
                worker.State = StreetWorkerState.ToStation;
                worker.Delay = 0f;
                worker.PickupDuration = 0f;
                worker.PickupProgress = 0f;
            }
            return true;
        }

        private bool SetWorkerRoute(StreetWorker worker, Vector2 destination)
        {
            worker.Route = KitchenLayout.FindShortestSafeRoute(worker.Position, destination);
            if (worker.Route == null)
            {
                worker.RouteIndex = 0;
                worker.RouteDistance = 0f;
                worker.Target = worker.Position;
                return false;
            }
            worker.RouteIndex = 0;
            worker.RouteDistance = RouteDistance(worker.Position, worker.Route, destination);
            worker.Target = worker.Route.Length > 0 ? worker.Route[0] : destination;
            return true;
        }

        private static float RouteDistance(Vector2 start, Vector2[] route, Vector2 destination)
        {
            if (route == null || route.Length == 0) return Vector2.Distance(start, destination);
            float distance = 0f;
            Vector2 previous = start;
            for (int i = 0; i < route.Length; i++)
            {
                distance += Vector2.Distance(previous, route[i]);
                previous = route[i];
            }
            return distance;
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
            worker.RouteDistance = 0f;
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
                if (!customer.WasServed) { customer.WasServed = true; customersAttended++; }
                int amount = Mathf.Max(0, Mathf.RoundToInt(GetProductPrice(worker.Product)));
                Coins += amount; CoinsEarned += amount; sales.Add(new StreetSale { Id = nextSale++, Amount = amount, Position = worker.Position });
                if (sales.Count > 40) sales.RemoveAt(0);

                if (line.Remaining > 0 && Kitchen.HasSupply(worker.Product))
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
                if (!customer.DeliverySoundPlayed && customer.PendingOrderLineCount == 0 && customer.Reserved == 0)
                {
                    customer.DeliverySoundPlayed = true;
                    ordersCompleted++;
                    OrderCompleted?.Invoke();
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
            Kitchen.ReleaseTableReservation(worker.Id);
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
            nextNormalCookProduct = 0;
            nextPremiumCookProduct = 2;
            nextCookQueueTicket = 0;
            Kitchen = new StreetKitchenProduction(balance.kitchen, availableProductsByLevel[LevelIndex], ingredientStock, UsesActiveCooks, !UsesActiveCooks && LevelIndex == 2);
            workers.Clear();
            nextWorker = 1;
            SpeedLevel = 0; // Zero purchased upgrades means the displayed speed is x1.00.
            AddWorker();
            if (LevelIndex == 5 && HasRole(StreetWorkerRole.ParrilleroPremium)) AddWorker(StreetWorkerRole.ParrilleroPremium);
        }
        private void AddWorker() { AddWorker(StreetWorkerRole.Parrillero); }
        private void AddWorker(StreetWorkerRole role)
        {
            // Production stays on the grill side of the original prep row; delivery stays on
            // its counter side. Do not move furniture or route cooks through solid table bases.
            bool grillWorker = role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium;
            int stationIndex = -1, stationCount = 1;
            long queueTicket = long.MaxValue;
            if (UsesActiveCooks && grillWorker)
            {
                stationCount = Mathf.Min(MaxCookStationsPerGrill, WorkerCount(role) + 1);
                bool[] occupied = new bool[MaxCookStationsPerGrill];
                foreach (StreetWorker existing in workers)
                    if (existing.Role == role && existing.CookStationIndex >= 0 && existing.CookStationIndex < occupied.Length)
                        occupied[existing.CookStationIndex] = true;
                for (int i = 0; i < occupied.Length; i++) if (!occupied[i]) { stationIndex = i; break; }
                if (stationIndex < 0) queueTicket = nextCookQueueTicket++;
            }
            Vector2 home = WorkerWaitPosition(nextWorker);
            if (UsesActiveCooks && grillWorker)
            {
                int product = role == StreetWorkerRole.Parrillero ? 0 : 2;
                if (stationIndex >= 0) home = KitchenLayout.GrillWorkPosition(product, stationIndex, stationCount);
                else
                {
                    int queueIndex = 0, queueCount = 1;
                    foreach (StreetWorker existing in workers)
                    {
                        if (existing.Role != role || existing.CookStationIndex >= 0 ||
                            (existing.State != StreetWorkerState.Idle && existing.State != StreetWorkerState.ToCookQueue)) continue;
                        queueCount++;
                        if (existing.CookQueueTicket < queueTicket) queueIndex++;
                    }
                    home = KitchenLayout.CookQueuePosition(product, queueIndex, queueCount);
                }
            }
            workers.Add(new StreetWorker { Id = nextWorker++, Role = role, Product = -1,
                Position = home, Target = home, WaitPosition = home, State = StreetWorkerState.Idle,
                CookStationIndex = stationIndex, CookQueueTicket = queueTicket });
        }
        private void ResetWorker(StreetWorker w) { w.CookingProgress = 0f; w.VisualCooking = false; w.VisualCookingProduct = -1; w.CookingItemId = 0; w.CookingWorkPosition = Vector2.zero; w.GrillContactBlendStart = 0f; w.StepDistance = 0f; w.RouteDistance = 0f; w.PickupProgress = 0f; w.PickupDuration = 0f; w.Customer = null; w.CustomerId = 0; w.Product = -1; w.CarriedItemId = 0; w.State = StreetWorkerState.Idle; w.Route = null; w.RouteIndex = 0; w.ReturningToWaitPosition = false; w.Target = w.Position; }
        private static Vector2 Move(Vector2 from, Vector2 to, float speed, float dt) { return Vector2.MoveTowards(from, to, speed * dt); }
    }
}
