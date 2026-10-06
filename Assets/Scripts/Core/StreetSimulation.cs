using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    [Serializable]
    public sealed class StreetBalance
    {
        [Range(1, 21)] public int maxCustomers = 21;
        public int randomSeed = 1337;
        [Range(.1f,4f)] public float priceSensitivity=2.5f;
        [Min(1)] public int maxStaff = 5;
        public int[] hireCosts = { 200, 500, 1200, 2800 };
        public int[] speedUpgradeCosts = { 25, 40, 65, 100, 160, 250, 400, 640, 1000 };
        [Tooltip("Discounted tutorial-level costs; later locations keep the shared tables above.")]
        public int[] florestaHireCosts = { 15, 30, 60, 100 };
        [Tooltip("Discounted tutorial-level costs; later locations keep the shared tables above.")]
        public int[] florestaSpeedUpgradeCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        [Min(.01f)] public float speedIncrease = .10f;
        [Min(0.1f)] public float customerArrivalSeconds = 0.16f;
        [Min(0.1f)] public float workerSpeed = 300f;
        [Min(10f)] public float customerSpeed = 300f;
        public float[] levelDemandMultipliers = { 1f, 1.1f, 1.2f, 1.35f, 1.5f };
        [Min(0.01f)] public float pickupSeconds = 0.28f;
        [Min(0.1f)] public float customerPatienceSeconds = 180f;
        [Min(0f)] public float minPrice = 0f;
        [Min(0f)] public float maxPrice = 60f;
        public float[] productPriceMultipliers = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        [Min(1)] public int minOrderQuantity = 999;
        [Min(1)] public int maxOrderQuantity = 999;
        [Min(0f)] public float initialPrice = 5f;
        [Min(0f)] public float deliveryPatienceRefreshSeconds = 180f;
        public int[] levelProductCounts = { 1, 2, 5, 6, 7 };
        public int[] levelGoals = { 200, 200, 65, 85, 110 };
        public float[] levelDurations = { 120f, 180f, 240f, 270f, 300f };
        [Tooltip("Optional deadline-only Floresta trial. Default false: win immediately at the unit goal.")]
        public bool florestaFinishAtDeadline = false;
    }

    public enum StreetCustomerState { Entering, Waiting, Receiving, Leaving, Advancing }
    public enum StreetWorkerState { Idle, ToStation, Pickup, ToCounter, Handoff }

    [Serializable]
    public sealed class StreetCustomer
    {
        public int Id { get; internal set; }
        public int Product { get; internal set; }
        public int Remaining { get; internal set; }
        public int SecondaryProduct { get; internal set; } = -1;
        public int SecondaryRemaining { get; internal set; }
        public int Reserved { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public StreetCustomerState State { get; internal set; }
        public float PatienceFraction { get; internal set; }
        public float AnimationTime { get; internal set; }
        internal float Patience;
        internal float ReceiveRemaining;
        internal int Slot;
        internal int ReservedPrimary;
        internal int ReservedSecondary;
    }

    [Serializable]
    public sealed class StreetWorker
    {
        public int Id { get; internal set; }
        public int Product { get; internal set; }
        public int CustomerId { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public StreetWorkerState State { get; internal set; }
        public float AnimationTime { get; internal set; }
        internal float Delay;
        internal bool UsingStationApproach;
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
        public const float FlorestaChoriPrice = 5f;
        public const float FrontQueueY = 324f, QueueRowSpacing = 56f;
        // Keep the full Parrillero sprite below the Nueva Chicago counter fascia (bottom ≈ y384).
        public const float ChicagoCounterServiceY = 485f;
        private const int QueueColumns = 7, QueueSlots = 21;
        public static readonly string[] ProductNames = { "Chori", "Paty", "Bondiola", "Vacío", "Coca 600 ml", "Fernet", "Cerveza" };
        public static readonly string[] LevelNames = { "Floresta / All Boys", "Nueva Chicago", "Argentinos Juniors", "Vélez", "Ferro" };
        private static readonly int[] DefaultProductCounts = { 1, 2, 5, 6, 7 };
        private static readonly int[] DefaultGoals = { 200, 200, 65, 85, 110 };
        private static readonly int[] DefaultHireCosts = { 200, 500, 1200, 2800 };
        private static readonly int[] DefaultSpeedCosts = { 25, 40, 65, 100, 160, 250, 400, 640, 1000 };
        private static readonly int[] DefaultFlorestaHireCosts = { 15, 30, 60, 100 };
        private static readonly int[] DefaultFlorestaSpeedCosts = { 5, 10, 15, 20, 30, 45, 65, 90, 125 };
        private static readonly float[] DefaultDurations = { 120f, 180f, 240f, 270f, 300f };
        private readonly StreetBalance balance;
        private readonly List<StreetCustomer> customers = new List<StreetCustomer>();
        private readonly List<StreetWorker> workers = new List<StreetWorker>();
        private readonly List<StreetSale> sales = new List<StreetSale>();
        private float arrival;
        private int nextCustomer = 1, nextWorker = 1, nextSale = 1, assignmentAfterId, roundFirstOrder = 1;
        private float price;
        private readonly float[] productPrices = new float[7];
        private System.Random random;

        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public int LevelIndex { get; private set; }
        public float Price { get { return price; } }
        public int Coins { get; private set; }
        public int CoinsEarned { get; private set; }
        public int Delivered { get; private set; }
        public int ChoriDelivered { get; private set; }
        public int CocaDelivered { get; private set; }
        public int Goal { get { return LevelValue(balance.levelGoals, LevelIndex, DefaultGoals); } }
        public bool GoalReached
        {
            get
            {
                return LevelIndex == 1
                    ? ChoriDelivered >= Goal && CocaDelivered >= Goal
                    : Delivered >= Goal;
            }
        }
        public float TimeRemaining { get { return Mathf.Max(0f, LevelValue(balance.levelDurations, LevelIndex, DefaultDurations) - Elapsed); } }
        public int StaffCount { get { return workers.Count; } }
        public int SpeedLevel { get; private set; }
        public bool CanEditPrices { get { return LevelIndex != 0; } }
        private int[] HireCosts
        {
            get
            {
                if (!CanEditPrices)
                    return balance.florestaHireCosts != null && balance.florestaHireCosts.Length > 0 ? balance.florestaHireCosts : DefaultFlorestaHireCosts;
                return balance.hireCosts != null && balance.hireCosts.Length > 0 ? balance.hireCosts : DefaultHireCosts;
            }
        }
        private int[] SpeedCosts
        {
            get
            {
                if (!CanEditPrices)
                    return balance.florestaSpeedUpgradeCosts != null && balance.florestaSpeedUpgradeCosts.Length > 0 ? balance.florestaSpeedUpgradeCosts : DefaultFlorestaSpeedCosts;
                return balance.speedUpgradeCosts != null && balance.speedUpgradeCosts.Length > 0 ? balance.speedUpgradeCosts : DefaultSpeedCosts;
            }
        }
        public int MaxStaffCount { get { return Mathf.Clamp(balance.maxStaff, 1, HireCosts.Length + 1); } }
        public int MaxSpeedLevel { get { return SpeedCosts.Length; } }
        public int HireCost { get { return StaffCount < MaxStaffCount ? Mathf.Max(1, HireCosts[StaffCount - 1]) : 0; } }
        public int SpeedCost { get { return SpeedLevel < MaxSpeedLevel ? Mathf.Max(1, SpeedCosts[SpeedLevel]) : 0; } }
        public bool CanHire { get { return (Phase == RoundPhase.Ready || Phase == RoundPhase.Playing) && StaffCount < MaxStaffCount && Coins >= HireCost; } }
        public bool CanUpgradeSpeed { get { return (Phase == RoundPhase.Ready || Phase == RoundPhase.Playing) && SpeedLevel < MaxSpeedLevel && Coins >= SpeedCost; } }
        public float WorkRate { get { return 1f + SpeedLevel * balance.speedIncrease; } }
        public IReadOnlyList<StreetCustomer> Customers { get { return customers; } }
        public IReadOnlyList<StreetWorker> Workers { get { return workers; } }
        public IReadOnlyList<StreetSale> Sales { get { return sales; } }
        public float Elapsed { get; private set; }
        public float DemandFraction
        {
            get
            {
                float total = 0;
                for (int slot = 0; slot < ProductCount; slot++) total += productPrices[GetAvailableProduct(slot)];
                return Mathf.Clamp01((balance.maxPrice-total/ProductCount)/Mathf.Max(0.01f,balance.maxPrice-balance.minPrice));
            }
        }
        public int ProductCount { get { return Mathf.Clamp(LevelValue(balance.levelProductCounts, LevelIndex, DefaultProductCounts),1,7); } }

        public int GetAvailableProduct(int slot)
        {
            if (slot < 0 || slot >= ProductCount) return -1;
            // Preserve the seven-product save/index order while level two swaps Paty for bottled Coca.
            return LevelIndex == 1 && slot == 1 ? 4 : slot;
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
            random = new System.Random(this.balance.randomSeed + LevelIndex * 97);
            this.price = Mathf.Clamp(price, this.balance.minPrice, this.balance.maxPrice);
            for(int i=0;i<7;i++) productPrices[i]=Mathf.Clamp(this.price*ProductMultiplier(i),this.balance.minPrice,this.balance.maxPrice);
            this.price=productPrices[0];
            if (!CanEditPrices) SetProductPrice(0, FlorestaChoriPrice);
            Coins = Mathf.Max(0, coins); SpeedLevel = Mathf.Clamp(speed, 0, MaxSpeedLevel);
            staff = Mathf.Clamp(staff, 1, MaxStaffCount);
            if (!CanEditPrices) ResetFlorestaTeam();
            else for (int i = 0; i < staff; i++) AddWorker();
        }

        public void StartRound()
        {
            customers.Clear(); sales.Clear();
            Coins = 0; CoinsEarned = 0; // Both totals belong only to this attempt.
            // A fresh/replayed Floresta turn never inherits hired staff or speed upgrades.
            if (!CanEditPrices) ResetFlorestaTeam();
            foreach (StreetWorker worker in workers) ResetWorker(worker);
            Elapsed = 0f; Delivered = 0; ChoriDelivered = 0; CocaDelivered = 0;
            arrival = 0f; assignmentAfterId = 0; roundFirstOrder = 1; Phase = RoundPhase.Playing;
        }
        public void SetPrice(float value) { SetProductPrice(0,value); }
        public float GetProductPrice(int product) { return productPrices[Mathf.Clamp(product,0,6)]; }
        public void SetProductPrice(int product,float value) { if(product<0||product>=7)return; productPrices[product]=!CanEditPrices && product==0 ? FlorestaChoriPrice : Mathf.Clamp(value,balance.minPrice,balance.maxPrice); if(product==0)price=productPrices[0]; }
        public bool TryHire()
        {
            if (!CanHire) return false;
            Coins -= HireCost; AddWorker(); return true;
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
            Coins = 0; CoinsEarned = 0;
            if (!CanEditPrices) SetProductPrice(0, FlorestaChoriPrice);
            return true;
        }
        public bool NextLevel()
        {
            if (Phase != RoundPhase.Won || LevelIndex + 1 >= LevelNames.Length) return false;
            LevelIndex++;
            Coins = 0; CoinsEarned = 0;
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
            return SpawnCustomer(product, quantity, -1, 0);
        }

        public bool SpawnCustomer(int product, int quantity, int secondaryProduct, int secondaryQuantity)
        {
            if (customers.Count >= Mathf.Clamp(balance.maxCustomers, 1, 21) || !IsProductAvailable(product)) return false;
            if (secondaryProduct >= 0 && (!IsProductAvailable(secondaryProduct) || secondaryProduct == product)) return false;
            int slot = FindQueueTailSlot();
            if (slot < 0) return false;
            Vector2 target = QueuePosition(slot);
            bool left = (nextCustomer & 1) == 0;
            int maxQuantity = LevelIndex <= 1 ? 4 : 999;
            customers.Add(new StreetCustomer { Id = nextCustomer++, Product = product,
                Remaining = Mathf.Clamp(quantity, 1, maxQuantity), SecondaryProduct = secondaryProduct,
                SecondaryRemaining = secondaryProduct >= 0 ? Mathf.Clamp(secondaryQuantity, 1, maxQuantity) : 0,
                Position = new Vector2(left ? -40 : 580, target.y),
                Target = target, State = StreetCustomerState.Entering, Patience = Mathf.Max(0.1f, balance.customerPatienceSeconds), PatienceFraction = 1f,
                Slot = slot });
            return true;
        }

        public static Vector2 StationPosition(int product)
        {
            int index = Mathf.Clamp(product, 0, 6);
            // Choripán pickups happen at the trestle serving table beside the grill.
            return index == 0 ? new Vector2(205, 585) : new Vector2(58 + 70 * index, 578);
        }

        public static Vector2 StationPositionForLevel(int product, int levelIndex, int productCount)
        {
            if (levelIndex == 1)
            {
                if (product == 0) return new Vector2(105, 660);
                if (product == 4) return new Vector2(465, 635);
            }
            return StationPositionForProductCount(product, productCount);
        }

        // Route along the clear upper lane, then down beside the table.
        // Never cross the Floresta hot grill on either pickup or return travel.
        public static Vector2 StationApproachPoint() => new Vector2(205, 510);
        public static Vector2 StationApproachPointForLevel(int product, int levelIndex) =>
            levelIndex == 1 ? (product == 4 ? new Vector2(415, 510) : new Vector2(125, 510)) : StationApproachPoint();
        public static Vector2 StationPositionForProductCount(int product, int productCount) =>
            product == 0 && productCount > 1 ? new Vector2(220, 665) : StationPosition(product);
        public static float CounterHandoffYForLevel(int levelIndex) => levelIndex == 1 ? ChicagoCounterServiceY : 400f;
        private Vector2 WorkerHomePosition => new Vector2(433f, CounterHandoffYForLevel(LevelIndex));
        private Vector2 CounterHandoffPosition(StreetCustomer customer) =>
            new Vector2(customer.Target.x, CounterHandoffYForLevel(LevelIndex));
        private bool UsesSideTableRoute(StreetWorker worker) =>
            (LevelIndex == 0 && worker.Product == 0) ||
            (LevelIndex == 1 && (worker.Product == 0 || worker.Product == 4));
        private Vector2 StationPositionForWorker(StreetWorker worker) =>
            StationPositionForLevel(worker.Product, LevelIndex, ProductCount);
        private Vector2 StationApproachForWorker(StreetWorker worker) =>
            StationApproachPointForLevel(worker.Product, LevelIndex);

        private void StepSlice(float dt)
        {
            float duration = LevelValue(balance.levelDurations, LevelIndex, DefaultDurations);
            Elapsed = Mathf.Min(duration, Elapsed + dt);
            if (Elapsed >= duration) { Phase = GoalReached ? RoundPhase.Won : RoundPhase.Lost; return; }
            arrival -= dt;
            if (arrival <= 0f && customers.Count < DemandCapacity())
            {
                float demandFactor = .55f / Mathf.Pow(Mathf.Max(.08f,DemandFraction),Mathf.Max(.1f,balance.priceSensitivity));
                float pressure=balance.levelDemandMultipliers!=null&&LevelIndex<balance.levelDemandMultipliers.Length?Mathf.Max(.1f,balance.levelDemandMultipliers[LevelIndex]):1f;
                if (LevelIndex == 1)
                {
                    int variant = random.Next(0, 3);
                    int choriQuantity = random.Next(1, 5);
                    if (variant == 0) SpawnCustomer(0, choriQuantity);
                    else if (variant == 1) SpawnCustomer(4, random.Next(1, 5));
                    else SpawnCustomer(0, choriQuantity, 4, random.Next(1, 5));
                }
                else
                {
                    int product = ChooseProduct();
                    int quantity;
                    if (LevelIndex == 0) quantity = random.Next(1, 5);
                    else
                    {
                        int minimum = Mathf.Clamp(balance.minOrderQuantity, 1, 999);
                        int maximum = Mathf.Clamp(balance.maxOrderQuantity, minimum, 999);
                        quantity = random.Next(minimum, maximum + 1);
                        float appetite = Mathf.Clamp01((balance.maxPrice - GetProductPrice(product)) / Mathf.Max(.01f, balance.maxPrice - balance.minPrice));
                        if (appetite < .8f) quantity = Mathf.Max(1, Mathf.RoundToInt(quantity * Mathf.Pow(appetite, 4)));
                        else if (roundFirstOrder == 1) quantity = 999;
                    }
                    SpawnCustomer(product, quantity);
                }
                roundFirstOrder = 0;
                arrival = Mathf.Max(0.05f, balance.customerArrivalSeconds * demandFactor / pressure);
            }
            AdvanceCustomers(dt);
            AdvanceWorkers(dt);
            for (int i = sales.Count - 1; i >= 0; i--) { sales[i].Age += dt; if (sales[i].Age > 3f) sales.RemoveAt(i); }
            if (GoalReached && !(LevelIndex == 0 && balance.florestaFinishAtDeadline)) Phase = RoundPhase.Won;
        }

        private int DemandCapacity()
        {
            int cap=Mathf.Clamp(balance.maxCustomers,1,21);
            return DemandFraction>=.8f?cap:Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1,cap,DemandFraction*DemandFraction)),1,cap);
        }
        private int ChooseProduct()
        {
            float total=0;
            for(int slot=0;slot<ProductCount;slot++)total+=PriceWeight(GetAvailableProduct(slot));
            float roll=(float)random.NextDouble()*total;
            for(int slot=0;slot<ProductCount;slot++){int p=GetAvailableProduct(slot);roll-=PriceWeight(p);if(roll<=0)return p;}
            return GetAvailableProduct(ProductCount-1);
        }
        private float PriceWeight(int p)
        {
            float f=Mathf.Clamp01((balance.maxPrice-GetProductPrice(p))/Mathf.Max(.01f,balance.maxPrice-balance.minPrice));
            return Mathf.Max(.01f,Mathf.Pow(f,Mathf.Max(.1f,balance.priceSensitivity)));
        }

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

        private void AdvanceWorkers(float dt)
        {
            foreach (StreetWorker w in workers)
            {
                w.AnimationTime += dt;
                if (w.State == StreetWorkerState.Idle) { Assign(w); continue; }
                if (!IsAtCounter(w.Customer) || !customers.Contains(w.Customer)) { CancelAssignment(w); continue; }
                if (w.State == StreetWorkerState.ToStation)
                {
                    w.Position = Move(w.Position, w.Target, balance.workerSpeed * WorkRate, dt);
                    if (w.Position == w.Target)
                    {
                        if (w.UsingStationApproach)
                        {
                            w.UsingStationApproach = false;
                            w.Target = StationPositionForWorker(w);
                        }
                        else { w.State = StreetWorkerState.Pickup; w.Delay = balance.pickupSeconds / WorkRate; }
                    }
                }
                else if (w.State == StreetWorkerState.Pickup)
                {
                    w.Delay -= dt;
                    if (w.Delay <= 0f)
                    {
                        w.State = StreetWorkerState.ToCounter;
                        w.UsingStationApproach = UsesSideTableRoute(w);
                        w.Target = w.UsingStationApproach ? StationApproachForWorker(w) : CounterHandoffPosition(w.Customer);
                    }
                }
                else if (w.State == StreetWorkerState.ToCounter)
                {
                    w.Position = Move(w.Position, w.Target, balance.workerSpeed * WorkRate, dt);
                    if (w.Position == w.Target)
                    {
                        if (w.UsingStationApproach)
                        {
                            w.UsingStationApproach = false;
                            w.Target = CounterHandoffPosition(w.Customer);
                        }
                        else { w.State = StreetWorkerState.Handoff; w.Delay = balance.pickupSeconds / WorkRate; }
                    }
                }
                else if (w.State == StreetWorkerState.Handoff)
                {
                    w.Delay -= dt;
                    if (w.Delay <= 0f)
                    {
                        Deliver(w);
                        // Stop at the goal handoff, before another worker can sell in this slice.
                        if (LevelIndex <= 1 && GoalReached && !(LevelIndex == 0 && balance.florestaFinishAtDeadline))
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
            int start = 0;
            while (start < customers.Count && customers[start].Id <= assignmentAfterId) start++;
            for (int offset = 0; offset < customers.Count; offset++)
            {
                int i = (start + offset) % customers.Count;
                StreetCustomer c = customers[i];
                if (c.State != StreetCustomerState.Waiting || !IsAtCounter(c)) continue;
                int product = NextProductToServe(c);
                if (product < 0) continue;
                worker.Product = product;
                c.Reserved++;
                if (product == c.Product) c.ReservedPrimary++; else c.ReservedSecondary++;
                worker.Customer = c; worker.CustomerId = c.Id; assignmentAfterId = c.Id;
                worker.UsingStationApproach = UsesSideTableRoute(worker);
                worker.Target = worker.UsingStationApproach ? StationApproachForWorker(worker) : StationPositionForWorker(worker);
                worker.State = StreetWorkerState.ToStation; return;
            }
        }

        private static int NextProductToServe(StreetCustomer c)
        {
            if (c.Remaining > 0)
                return c.Remaining - c.ReservedPrimary > 0 ? c.Product : -1;
            if (c.SecondaryProduct >= 0 && c.SecondaryRemaining > 0)
                return c.SecondaryRemaining - c.ReservedSecondary > 0 ? c.SecondaryProduct : -1;
            return -1;
        }

        private static bool HasOutstandingItems(StreetCustomer c) =>
            c.Remaining > 0 || c.SecondaryRemaining > 0 || c.Reserved > 0;

        private void Deliver(StreetWorker worker)
        {
            StreetCustomer c = worker.Customer;
            bool primary = c != null && worker.Product == c.Product;
            bool secondary = c != null && worker.Product == c.SecondaryProduct;
            if (IsAtCounter(c) && customers.Contains(c) && c.Reserved > 0 &&
                ((primary && c.ReservedPrimary > 0 && c.Remaining > 0) ||
                 (secondary && c.ReservedSecondary > 0 && c.SecondaryRemaining > 0)))
            {
                c.Reserved--;
                if (primary) { c.ReservedPrimary--; c.Remaining--; }
                else { c.ReservedSecondary--; c.SecondaryRemaining--; }
                c.State = StreetCustomerState.Receiving; c.ReceiveRemaining=.3f;
                Delivered++;
                if (worker.Product == 0) ChoriDelivered++;
                if (LevelIndex == 1 && worker.Product == 4) CocaDelivered++;
                int amount = Mathf.Max(0, Mathf.RoundToInt(GetProductPrice(worker.Product)));
                Coins += amount; CoinsEarned += amount; sales.Add(new StreetSale { Id = nextSale++, Amount = amount, Position = new Vector2(c.Target.x, 400) });
                c.Patience = Mathf.Max(0.1f, balance.deliveryPatienceRefreshSeconds);
                c.PatienceFraction = Mathf.Clamp01(c.Patience / Mathf.Max(0.1f, balance.customerPatienceSeconds));
                if (sales.Count > 40) sales.RemoveAt(0);
                // Receiving pose lasts .3 seconds; departure/next assignment is advanced above.
            }
            else { CancelAssignment(worker); return; }
            ResetWorker(worker);
        }

        private void RemoveCustomer(StreetCustomer c)
        {
            int column = c.Slot >= 0 ? c.Slot % QueueColumns : -1;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Customer == c) CancelAssignment(workers[i]);
            customers.Remove(c);
            if (column >= 0) CompactQueue(column);
        }
        private float ProductMultiplier(int product)
        {
            return balance.productPriceMultipliers != null && product < balance.productPriceMultipliers.Length
                ? Mathf.Max(0f, balance.productPriceMultipliers[product]) : 1f;
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
        private void CancelAssignment(StreetWorker worker)
        {
            if (worker.Customer != null && worker.Customer.Reserved > 0)
            {
                worker.Customer.Reserved--;
                if (worker.Product == worker.Customer.Product && worker.Customer.ReservedPrimary > 0) worker.Customer.ReservedPrimary--;
                else if (worker.Product == worker.Customer.SecondaryProduct && worker.Customer.ReservedSecondary > 0) worker.Customer.ReservedSecondary--;
            }
            ResetWorker(worker);
        }
        private static int LevelValue(int[] values, int index, int[] fallback) { return values != null && index < values.Length ? Mathf.Max(1, values[index]) : fallback[index]; }
        private static float LevelValue(float[] values, int index, float[] fallback) { return values != null && index < values.Length ? Mathf.Max(.01f, values[index]) : fallback[index]; }
        private void ResetFlorestaTeam()
        {
            workers.Clear();
            nextWorker = 1;
            SpeedLevel = 0; // Zero purchased upgrades means the displayed speed is x1.00.
            AddWorker();
        }
        private void AddWorker() { workers.Add(new StreetWorker { Id = nextWorker++, Position = WorkerHomePosition, Target = WorkerHomePosition, State = StreetWorkerState.Idle }); }
        private void ResetWorker(StreetWorker w) { w.Customer = null; w.CustomerId = 0; w.State = StreetWorkerState.Idle; w.UsingStationApproach = false; w.Target = WorkerHomePosition; }
        private static Vector2 Move(Vector2 from, Vector2 to, float speed, float dt) { return Vector2.MoveTowards(from, to, speed * dt); }
    }
}
