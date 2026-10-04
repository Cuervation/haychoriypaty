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
        [Min(1)] public int maxStaff = 8;
        [Min(1)] public int baseHireCost = 15;
        [Min(1)] public int baseSpeedCost = 5;
        [Min(1)] public int florestaHireCost = 25;
        [Min(1)] public int florestaSpeedCost = 5;
        [Min(.01f)] public float florestaSpeedIncrease = .10f;
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
        public int[] levelProductCounts = { 1, 3, 5, 6, 7 };
        public int[] levelGoals = { 24, 40, 65, 85, 110 };
        public float[] levelDurations = { 180f, 210f, 240f, 270f, 300f };
        [Tooltip("Floresta keeps serving for the full turn; evaluate its sales goal at the deadline.")]
        public bool florestaFinishAtDeadline = true;
    }

    public enum StreetCustomerState { Entering, Waiting, Receiving, Leaving }
    public enum StreetWorkerState { Idle, ToStation, Pickup, ToCounter, Handoff }

    [Serializable]
    public sealed class StreetCustomer
    {
        public int Id { get; internal set; }
        public int Product { get; internal set; }
        public int Remaining { get; internal set; }
        public int Reserved { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public StreetCustomerState State { get; internal set; }
        public float PatienceFraction { get; internal set; }
        public float AnimationTime { get; internal set; }
        internal float Patience;
        internal float ReceiveRemaining;
        internal int Slot;
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
        public static readonly string[] ProductNames = { "Chori", "Paty", "Bondiola", "Vacío", "Coca", "Fernet", "Cerveza" };
        public static readonly string[] LevelNames = { "Floresta / All Boys", "Nueva Chicago", "Argentinos Juniors", "Vélez", "Ferro" };
        private static readonly int[] DefaultProductCounts = { 1, 3, 5, 6, 7 };
        private static readonly int[] DefaultGoals = { 24, 40, 65, 85, 110 };
        private static readonly float[] DefaultDurations = { 180f, 210f, 240f, 270f, 300f };
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
        public int Delivered { get; private set; }
        public int Goal { get { return LevelValue(balance.levelGoals, LevelIndex, DefaultGoals); } }
        public float TimeRemaining { get { return Mathf.Max(0f, LevelValue(balance.levelDurations, LevelIndex, DefaultDurations) - Elapsed); } }
        public int StaffCount { get { return workers.Count; } }
        public int SpeedLevel { get; private set; }
        public bool CanEditPrices { get { return LevelIndex != 0; } }
        public int HireCost { get { return CanEditPrices ? balance.baseHireCost * StaffCount : balance.florestaHireCost; } }
        public int SpeedCost { get { return CanEditPrices ? balance.baseSpeedCost * (SpeedLevel + 1) : balance.florestaSpeedCost; } }
        public float WorkRate { get { return 1f + SpeedLevel * (CanEditPrices ? .25f : balance.florestaSpeedIncrease); } }
        public IReadOnlyList<StreetCustomer> Customers { get { return customers; } }
        public IReadOnlyList<StreetWorker> Workers { get { return workers; } }
        public IReadOnlyList<StreetSale> Sales { get { return sales; } }
        public float Elapsed { get; private set; }
        public float DemandFraction { get { float total = 0; for(int i=0;i<ProductCount;i++) total+=productPrices[i]; return Mathf.Clamp01((balance.maxPrice-total/ProductCount)/Mathf.Max(0.01f,balance.maxPrice-balance.minPrice)); } }
        public int ProductCount { get { return Mathf.Clamp(LevelValue(balance.levelProductCounts, LevelIndex, DefaultProductCounts),1,7); } }

        public StreetSimulation(StreetBalance balance, int level = 0, float price = 5f, int coins = 0, int staff = 1, int speed = 0)
        {
            this.balance = balance ?? new StreetBalance();
            LevelIndex = Mathf.Clamp(level, 0, LevelNames.Length - 1);
            random = new System.Random(this.balance.randomSeed + LevelIndex * 97);
            this.price = Mathf.Clamp(price, this.balance.minPrice, this.balance.maxPrice);
            for(int i=0;i<7;i++) productPrices[i]=Mathf.Clamp(this.price*ProductMultiplier(i),this.balance.minPrice,this.balance.maxPrice);
            this.price=productPrices[0];
            if (!CanEditPrices) SetProductPrice(0, FlorestaChoriPrice);
            Coins = Mathf.Max(0, coins); SpeedLevel = Mathf.Max(0, speed);
            staff = Mathf.Clamp(staff, 1, Mathf.Max(1, this.balance.maxStaff));
            for (int i = 0; i < staff; i++) AddWorker();
        }

        public void StartRound()
        {
            customers.Clear(); sales.Clear();
            foreach (StreetWorker worker in workers) ResetWorker(worker);
            Elapsed = 0f; Delivered = 0; arrival = 0f; assignmentAfterId = 0; roundFirstOrder = 1; Phase = RoundPhase.Playing;
        }
        public void SetPrice(float value) { SetProductPrice(0,value); }
        public float GetProductPrice(int product) { return productPrices[Mathf.Clamp(product,0,6)]; }
        public void SetProductPrice(int product,float value) { if(product<0||product>=7)return; productPrices[product]=!CanEditPrices && product==0 ? FlorestaChoriPrice : Mathf.Clamp(value,balance.minPrice,balance.maxPrice); if(product==0)price=productPrices[0]; }
        public bool TryHire()
        {
            if ((Phase != RoundPhase.Ready && Phase != RoundPhase.Playing) || StaffCount >= balance.maxStaff || Coins < HireCost) return false;
            Coins -= HireCost; AddWorker(); return true;
        }
        public bool TryUpgradeSpeed()
        {
            if ((Phase != RoundPhase.Ready && Phase != RoundPhase.Playing) || Coins < SpeedCost) return false;
            Coins -= SpeedCost; SpeedLevel++; return true;
        }
        public bool SelectLevel(int level)
        {
            if (Phase != RoundPhase.Ready || level < 0 || level >= LevelNames.Length) return false;
            LevelIndex = level;
            if (!CanEditPrices) SetProductPrice(0, FlorestaChoriPrice);
            return true;
        }
        public bool NextLevel()
        {
            if (Phase != RoundPhase.Won || LevelIndex + 1 >= LevelNames.Length) return false;
            LevelIndex++;
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
            if (customers.Count >= Mathf.Clamp(balance.maxCustomers, 1, 21) || product < 0 || product >= ProductCount) return false;
            int slot = FindFreeSlot();
            if (slot < 0) return false;
            int row = slot / 7, col = slot % 7;
            Vector2 target = new Vector2(58 + 70 * col, 310 - 70 * row);
            bool left = (nextCustomer & 1) == 0;
            customers.Add(new StreetCustomer { Id = nextCustomer++, Product = product,
                Remaining = Mathf.Clamp(quantity, 1, CanEditPrices ? 999 : 4), Position = new Vector2(left ? -40 : 580, target.y),
                Target = target, State = StreetCustomerState.Entering, Patience = Mathf.Max(0.1f, balance.customerPatienceSeconds), PatienceFraction = 1f,
                Slot = slot });
            return true;
        }

        public static Vector2 StationPosition(int product) { return new Vector2(58 + 70 * Mathf.Clamp(product, 0, 6), 578); }

        private void StepSlice(float dt)
        {
            float duration = LevelValue(balance.levelDurations, LevelIndex, DefaultDurations);
            Elapsed = Mathf.Min(duration, Elapsed + dt);
            if (Elapsed >= duration) { Phase = Delivered >= Goal ? RoundPhase.Won : RoundPhase.Lost; return; }
            arrival -= dt;
            if (arrival <= 0f && customers.Count < DemandCapacity())
            {
                float demandFactor = .55f / Mathf.Pow(Mathf.Max(.08f,DemandFraction),Mathf.Max(.1f,balance.priceSensitivity));
                float pressure=balance.levelDemandMultipliers!=null&&LevelIndex<balance.levelDemandMultipliers.Length?Mathf.Max(.1f,balance.levelDemandMultipliers[LevelIndex]):1f;
                int product = ChooseProduct();
                int quantity;
                if (!CanEditPrices) quantity = random.Next(1, 5);
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
                roundFirstOrder = 0;
                arrival = Mathf.Max(0.05f, balance.customerArrivalSeconds * demandFactor / pressure);
            }
            AdvanceCustomers(dt);
            AdvanceWorkers(dt);
            for (int i = sales.Count - 1; i >= 0; i--) { sales[i].Age += dt; if (sales[i].Age > 3f) sales.RemoveAt(i); }
            if (Delivered >= Goal && !(LevelIndex == 0 && balance.florestaFinishAtDeadline)) Phase = RoundPhase.Won;
        }

        private int DemandCapacity()
        {
            int cap=Mathf.Clamp(balance.maxCustomers,1,21);
            return DemandFraction>=.8f?cap:Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1,cap,DemandFraction*DemandFraction)),1,cap);
        }
        private int ChooseProduct()
        {
            float total=0;
            for(int p=0;p<ProductCount;p++)total+=PriceWeight(p);
            float roll=(float)random.NextDouble()*total;
            for(int p=0;p<ProductCount;p++){roll-=PriceWeight(p);if(roll<=0)return p;}
            return ProductCount-1;
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
                if (c.State == StreetCustomerState.Entering) { c.Position = Move(c.Position, c.Target, balance.customerSpeed, dt); if (c.Position == c.Target) c.State = StreetCustomerState.Waiting; }
                if (c.State == StreetCustomerState.Waiting) { c.Patience -= dt; c.PatienceFraction = Mathf.Clamp01(c.Patience / Mathf.Max(0.1f, balance.customerPatienceSeconds)); }
                if (c.Patience <= 0f && c.State != StreetCustomerState.Leaving) SetLeaving(c);
                if (c.State == StreetCustomerState.Leaving) { c.Position = Move(c.Position, c.Target, 180f, dt); if (c.Position == c.Target) RemoveCustomer(c); }
                if(c.State==StreetCustomerState.Receiving) { c.ReceiveRemaining-=dt; if(c.ReceiveRemaining<=0f) { if(c.Remaining<=0&&c.Reserved<=0)SetLeaving(c);else if(c.Remaining>0)c.State=StreetCustomerState.Waiting; } }
            }
        }

        private void AdvanceWorkers(float dt)
        {
            foreach (StreetWorker w in workers)
            {
                w.AnimationTime += dt;
                if (w.State == StreetWorkerState.Idle) { Assign(w); continue; }
                if (w.State == StreetWorkerState.ToStation)
                {
                    w.Position = Move(w.Position, w.Target, balance.workerSpeed * WorkRate, dt);
                    if (w.Position == w.Target) { w.State = StreetWorkerState.Pickup; w.Delay = balance.pickupSeconds / WorkRate; }
                }
                else if (w.State == StreetWorkerState.Pickup)
                {
                    w.Delay -= dt;
                    if (w.Delay <= 0f) { w.State = StreetWorkerState.ToCounter; w.Target = new Vector2(w.Customer.Target.x, 400); }
                }
                else if (w.State == StreetWorkerState.ToCounter)
                {
                    w.Position = Move(w.Position, w.Target, balance.workerSpeed * WorkRate, dt);
                    if (w.Position == w.Target) { w.State = StreetWorkerState.Handoff; w.Delay = balance.pickupSeconds / WorkRate; }
                }
                else if (w.State == StreetWorkerState.Handoff)
                {
                    w.Delay -= dt;
                    if (w.Delay <= 0f) Deliver(w);
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
                if (c.State != StreetCustomerState.Waiting || c.Remaining - c.Reserved <= 0) continue;
                c.Reserved++; worker.Customer = c; worker.CustomerId = c.Id; worker.Product = c.Product; assignmentAfterId = c.Id;
                worker.Target = StationPosition(c.Product); worker.State = StreetWorkerState.ToStation; return;
            }
        }

        private void Deliver(StreetWorker worker)
        {
            StreetCustomer c = worker.Customer;
            if (c != null && customers.Contains(c) && c.State != StreetCustomerState.Leaving && c.Reserved > 0 && c.Remaining > 0)
            {
                c.Reserved--; c.Remaining--; c.State = StreetCustomerState.Receiving; c.ReceiveRemaining=.3f;
                Delivered++; int amount = Mathf.Max(0, Mathf.RoundToInt(GetProductPrice(c.Product)));
                Coins += amount; sales.Add(new StreetSale { Id = nextSale++, Amount = amount, Position = new Vector2(c.Target.x, 400) });
                c.Patience = Mathf.Max(0.1f, balance.deliveryPatienceRefreshSeconds);
                c.PatienceFraction = Mathf.Clamp01(c.Patience / Mathf.Max(0.1f, balance.customerPatienceSeconds));
                if (sales.Count > 40) sales.RemoveAt(0);
                // Receiving pose lasts .3 seconds; departure/next assignment is advanced above.
            }
            ResetWorker(worker);
        }

        private void RemoveCustomer(StreetCustomer c)
        {
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Customer == c) { if (c.Reserved > 0) c.Reserved--; ResetWorker(workers[i]); }
            customers.Remove(c);
        }
        private float ProductMultiplier(int product)
        {
            return balance.productPriceMultipliers != null && product < balance.productPriceMultipliers.Length
                ? Mathf.Max(0f, balance.productPriceMultipliers[product]) : 1f;
        }
        private void SetLeaving(StreetCustomer c)
        {
            c.State = StreetCustomerState.Leaving; c.Target = new Vector2(c.Position.x < 270 ? -50 : 590, c.Position.y);
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Customer == c) { if (c.Reserved > 0) c.Reserved--; ResetWorker(workers[i]); }
        }
        private int FindFreeSlot()
        {
            int max = Mathf.Clamp(balance.maxCustomers, 1, 21);
            for (int slot = 0; slot < max; slot++)
            {
                bool used = false;
                for (int i = 0; i < customers.Count; i++) if (customers[i].Slot == slot) { used = true; break; }
                if (!used) return slot;
            }
            return -1;
        }
        private static int LevelValue(int[] values, int index, int[] fallback) { return values != null && index < values.Length ? Mathf.Max(1, values[index]) : fallback[index]; }
        private static float LevelValue(float[] values, int index, float[] fallback) { return values != null && index < values.Length ? Mathf.Max(.01f, values[index]) : fallback[index]; }
        private void AddWorker() { workers.Add(new StreetWorker { Id = nextWorker++, Position = new Vector2(433, 430), Target = new Vector2(433, 430), State = StreetWorkerState.Idle }); }
        private static void ResetWorker(StreetWorker w) { w.Customer = null; w.CustomerId = 0; w.State = StreetWorkerState.Idle; w.Target = new Vector2(433, 430); }
        private static Vector2 Move(Vector2 from, Vector2 to, float speed, float dt) { return Vector2.MoveTowards(from, to, speed * dt); }
    }
}
