using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum TowerDefenseRunPhase { Ready, Playing, Paused, Finished }

    [Serializable]
    public sealed class TowerDefenseOrderLine
    {
        [SerializeField] private int product;
        [SerializeField] private int remaining;
        public int Product => product;
        public int Remaining => remaining;
        public bool IsComplete => remaining <= 0;

        public TowerDefenseOrderLine(int productId, int quantity)
        {
            product = productId;
            remaining = Mathf.Max(0, quantity);
        }

        internal bool RecordDelivery()
        {
            if (remaining <= 0) return false;
            remaining--;
            return true;
        }
    }

    /// <summary>One identified supporter and their per-product pending order as they travel the route.</summary>
    public sealed class TowerDefenseFanState
    {
        internal float ReceiveAnimationSeconds;
        public int Id { get; }
        public TowerDefenseFanRouteFollower RouteFollower { get; internal set; }
        public IReadOnlyList<TowerDefenseOrderLine> Order => order;
        public float AnimationTime { get; internal set; }
        public bool IsReceiving { get; internal set; }
        public bool OrderComplete
        {
            get { for (int i = 0; i < order.Count; i++) if (!order[i].IsComplete) return false; return true; }
        }
        public int PendingUnits
        {
            get { int total = 0; for (int i = 0; i < order.Count; i++) total += order[i].Remaining; return total; }
        }

        private readonly List<TowerDefenseOrderLine> order;

        internal TowerDefenseFanState(int id, TowerDefenseFanRouteFollower follower, int[] products, int[] quantities)
        {
            Id = id;
            RouteFollower = follower;
            order = new List<TowerDefenseOrderLine>(products.Length);
            for (int i = 0; i < products.Length; i++) order.Add(new TowerDefenseOrderLine(products[i], quantities[i]));
        }

        internal bool NeedsProduct(int product)
        {
            for (int i = 0; i < order.Count; i++) if (order[i].Product == product && order[i].Remaining > 0) return true;
            return false;
        }

        internal bool RecordDelivery(int product)
        {
            for (int i = 0; i < order.Count; i++)
                if (order[i].Product == product) return order[i].RecordDelivery();
            return false;
        }
    }

    /// <summary>Logical workforce member assigned to one of the independent service zones.</summary>
    public sealed class TowerDefenseWorkerState
    {
        public int Id { get; }
        public StreetWorkerRole Role { get; }
        public Vector2 Position { get; internal set; }
        public int ServiceZone { get; internal set; }
        public bool IsMoving { get; internal set; }
        public float AnimationTime { get; internal set; }
        public float TravelDistance { get; internal set; }

        internal int TargetZone;
        internal Vector2[] TransferRoute;
        internal int TransferIndex;
        internal int CookUnitId;
        internal int NextCookProduct;

        internal TowerDefenseWorkerState(int id, StreetWorkerRole role, int zone, Vector2 position)
        {
            Id = id;
            Role = role;
            ServiceZone = zone;
            TargetZone = zone;
            Position = position;
            TransferRoute = Array.Empty<Vector2>();
        }
    }

    /// <summary>
    /// Tower Defense-only coordinator. It owns fans, orders, staffing, route delivery and an
    /// isolated StreetKitchenProduction instance; it reads campaign balance values but never
    /// calls campaign progression, settlement, selection or persistence.
    /// </summary>
    public sealed class TowerDefenseGameMode
    {
        public const float CampaignStepSliceSeconds = 0.05f;
        private const int CampaignOrderMaximumLineCount = 3;
        private const int CampaignOrderMaximumLineQuantity = 4;
        private const int MaximumSpeedLevel = 9;
        private static TowerDefenseGameMode activeInstance;
        private static bool campaignStepSuspended;

        private readonly TowerDefenseLevelConfig config;
        private readonly StreetBalance sourceBalance;
        private readonly System.Random random;
        private readonly List<TowerDefenseFanState> fans = new List<TowerDefenseFanState>();
        private readonly List<TowerDefenseWorkerState> workers = new List<TowerDefenseWorkerState>();
        private readonly int[] deliveredByProduct = new int[7];
        private readonly int[] goalByProduct = new int[7];
        private readonly int[] availableProducts;
        private readonly int[] workerCounts = new int[5];
        private readonly Vector2[] serviceZones = new Vector2[2];
        private float worldUnitsPerLogicalPixel;
        private float elapsed, spawnTimer;
        private int nextFanId = 1, nextWorkerId = 1, speedLevel;
        private int coins;
        private bool stopSpawning;
        private float serviceRange;
        private TowerDefenseRouteGeometryResult routeGeometry;
        private TowerDefenseRouteGeometryConfig route;

        public static bool OwnsRuntime => activeInstance != null;
        public static bool CampaignStepSuspended => campaignStepSuspended;
        public static void SetCampaignStepSuspended(bool suspended) => campaignStepSuspended = suspended;
        public string ModeId => config.ModeId;
        public string LevelId => config.LevelId;
        public string DisplayName => config.DisplayName;
        public TowerDefenseRouteGeometryConfig RouteConfig => route;
        public TowerDefenseRouteGeometryResult RouteGeometry => routeGeometry;
        public TowerDefenseRunPhase Phase { get; private set; } = TowerDefenseRunPhase.Ready;
        public StreetKitchenProduction Kitchen { get; }
        public IReadOnlyList<TowerDefenseFanState> Fans => fans;
        public IReadOnlyList<TowerDefenseWorkerState> Workers => workers;
        public int WorkersCount(StreetWorkerRole role)
        {
            int count = 0;
            for (int i = 0; i < workers.Count; i++) if (workers[i].Role == role) count++;
            return count;
        }
        public int[] DeliveredByProduct => (int[])deliveredByProduct.Clone();
        public int[] GoalByProduct => (int[])goalByProduct.Clone();
        public int Coins => coins;
        public int SpeedLevel => speedLevel;
        public int MaximumFans => Mathf.Min(config.MaximumFans, Mathf.Clamp(sourceBalance.maxCustomers, 1, 28));
        public int ActiveFanCount => fans.Count;
        public float Elapsed => elapsed;
        public float DurationSeconds { get; }
        public float TimeRemaining => Mathf.Max(0f, DurationSeconds - elapsed);
        public float ServiceRange => serviceRange;
        public bool HasReachedAllGoals { get; private set; }
        public bool TimedOut { get; private set; }
        public int CompletedOrdersAtExit { get; private set; }
        public int IncompleteOrdersAtExit { get; private set; }
        public int TotalDelivered { get; private set; }
        public float WorldUnitsPerLogicalPixel => worldUnitsPerLogicalPixel;

        /// <summary>Returns the configured, passable service point for one of the first level's work areas.</summary>
        public Vector2 WorkZone(int zone) => zone >= 0 && zone < serviceZones.Length ? serviceZones[zone] : Vector2.zero;

        public TowerDefenseGameMode(TowerDefenseLevelConfig levelConfig, StreetBalance campaignBalance,
            TowerDefenseRouteGeometryConfig runtimeRoute, float logicalPixelScale, Vector2 firstServiceZone, Vector2 secondServiceZone)
        {
            config = levelConfig != null ? levelConfig : throw new ArgumentNullException(nameof(levelConfig));
            sourceBalance = campaignBalance != null ? campaignBalance : throw new ArgumentNullException(nameof(campaignBalance));
            if (!IsFinite(logicalPixelScale) || logicalPixelScale <= 0f) throw new ArgumentOutOfRangeException(nameof(logicalPixelScale));
            worldUnitsPerLogicalPixel = logicalPixelScale;
            route = runtimeRoute != null ? CloneRoute(runtimeRoute) : throw new ArgumentNullException(nameof(runtimeRoute));
            routeGeometry = TowerDefenseRouteGeometry.Generate(route);
            if (routeGeometry == null || !routeGeometry.IsValid)
                throw new ArgumentException(routeGeometry == null ? "Tower Defense route generation returned no geometry." : string.Join("; ", routeGeometry.Validation.Errors), nameof(runtimeRoute));

            serviceZones[0] = firstServiceZone;
            serviceZones[1] = secondServiceZone;
            serviceRange = Mathf.Max(config.ServiceRange, route.RowSpacing * 0.5f + worldUnitsPerLogicalPixel * 4f);
            availableProducts = config.AvailableProducts;
            if (availableProducts.Length == 0) throw new ArgumentException("The Tower Defense level must enable at least one product.", nameof(levelConfig));

            int sourceLevel = Mathf.Clamp(config.SourceCampaignLevelId, 0, StreetSimulation.LevelNames.Length - 1);
            DurationSeconds = sourceBalance.levelDurations != null && sourceLevel < sourceBalance.levelDurations.Length
                ? Mathf.Max(1f, sourceBalance.levelDurations[sourceLevel]) : 380f;
            for (int p = 0; p < goalByProduct.Length; p++) goalByProduct[p] = sourceBalance.GetProductGoal(sourceLevel, p);
            int[] starting = config.StartingWorkersByRole;
            for (int role = 0; role < workerCounts.Length; role++) workerCounts[role] = role < starting.Length ? Mathf.Max(0, starting[role]) : 0;
            coins = sourceBalance.GetStartingCoins(sourceLevel);
            Kitchen = new StreetKitchenProduction(sourceBalance.kitchen, availableProducts, null, true, false);
            random = new System.Random(sourceBalance.randomSeed + sourceLevel * 97 + config.LevelId.GetHashCode());
            BuildWorkers();
        }

        public void Begin()
        {
            if (Phase != TowerDefenseRunPhase.Ready) return;
            activeInstance = this;
            campaignStepSuspended = true;
            Phase = TowerDefenseRunPhase.Playing;
        }

        public void Pause()
        {
            if (Phase == TowerDefenseRunPhase.Playing) Phase = TowerDefenseRunPhase.Paused;
        }

        public void Resume()
        {
            if (Phase == TowerDefenseRunPhase.Paused) Phase = TowerDefenseRunPhase.Playing;
        }

        /// <summary>Leave only Tower Defense runtime; the campaign simulation/save remains untouched.</summary>
        public void Stop()
        {
            if (activeInstance == this) activeInstance = null;
            campaignStepSuspended = false;
            if (Phase != TowerDefenseRunPhase.Finished) Phase = TowerDefenseRunPhase.Finished;
        }

        public bool IsRoleAvailable(StreetWorkerRole role)
        {
            if (role == StreetWorkerRole.Mozo)
            {
                for (int i = 0; i < availableProducts.Length; i++)
                    if (availableProducts[i] >= 0 && availableProducts[i] <= 3) return true;
                return false;
            }
            return StreetSpecialties.CatalogHasRole(availableProducts, role);
        }

        public int HireCost(StreetWorkerRole role)
        {
            if (!IsRoleAvailable(role)) return 0;
            return StreetSimulation.CalculateHireCost(sourceBalance, config.SourceCampaignLevelId, role,
                workerCounts[(int)role]);
        }

        public bool TryHire(StreetWorkerRole role)
        {
            int index = (int)role;
            if (Phase != TowerDefenseRunPhase.Playing || index < 0 || index >= workerCounts.Length || !IsRoleAvailable(role)) return false;
            int cost = HireCost(role);
            if (cost <= 0 || coins < cost || workerCounts[index] >= int.MaxValue - workers.Count) return false;
            coins -= cost;
            workerCounts[index]++;
            AddWorker(role, ChooseInitialZone(role));
            return true;
        }

        public int SpeedUpgradeCost => speedLevel >= MaximumSpeedLevel ? 0
            : StreetSimulation.CalculateSpeedCost(sourceBalance, config.SourceCampaignLevelId, speedLevel);

        public bool TryUpgradeSpeed()
        {
            if (Phase != TowerDefenseRunPhase.Playing || speedLevel >= MaximumSpeedLevel || coins < SpeedUpgradeCost || SpeedUpgradeCost <= 0) return false;
            coins -= SpeedUpgradeCost;
            speedLevel++;
            return true;
        }

        /// <summary>Step is split to match the fixed-size slices used by StreetSimulation.</summary>
        public void Step(float deltaTime)
        {
            if (Phase != TowerDefenseRunPhase.Playing || !IsFinite(deltaTime) || deltaTime <= 0f) return;
            float remaining = Mathf.Min(deltaTime, 1f);
            while (remaining > 0f && Phase == TowerDefenseRunPhase.Playing)
            {
                float step = Mathf.Min(CampaignStepSliceSeconds, remaining);
                StepSlice(step);
                remaining -= step;
            }
        }

        /// <summary>Deterministic order injection for focused domain tests and authored levels.</summary>
        public bool TrySpawnFan(int[] products, int[] quantities)
        {
            if (Phase != TowerDefenseRunPhase.Playing || stopSpawning || fans.Count >= MaximumFans ||
                products == null || quantities == null || products.Length < 1 ||
                products.Length > CampaignOrderMaximumLineCount || products.Length != quantities.Length) return false;
            int[] safeProducts = new int[products.Length];
            int[] safeQuantities = new int[products.Length];
            for (int i = 0; i < products.Length; i++)
            {
                if (!ContainsProduct(products[i]) || quantities[i] < 1 || quantities[i] > CampaignOrderMaximumLineQuantity) return false;
                for (int prior = 0; prior < i; prior++) if (products[prior] == products[i]) return false;
                safeProducts[i] = products[i];
                safeQuantities[i] = quantities[i];
            }
            return CreateFan(safeProducts, safeQuantities);
        }

        /// <summary>Rebuild world-space layout after a viewport/aspect change while preserving every fan's normalized progress and order.</summary>
        public void Reflow(TowerDefenseRouteGeometryConfig runtimeRoute, float logicalPixelScale,
            Vector2 firstServiceZone, Vector2 secondServiceZone)
        {
            if (runtimeRoute == null || !IsFinite(logicalPixelScale) || logicalPixelScale <= 0f) return;
            TowerDefenseRouteGeometryResult nextGeometry = TowerDefenseRouteGeometry.Generate(runtimeRoute);
            if (nextGeometry == null || !nextGeometry.IsValid) return;
            float[] normalized = new float[fans.Count];
            for (int i = 0; i < fans.Count; i++) normalized[i] = fans[i].RouteFollower.TotalDistance <= 0f ? 0f :
                fans[i].RouteFollower.Progress / fans[i].RouteFollower.TotalDistance;
            route = CloneRoute(runtimeRoute);
            routeGeometry = nextGeometry;
            serviceZones[0] = firstServiceZone;
            serviceZones[1] = secondServiceZone;
            // Followers and worker offsets below must be rebuilt using the new viewport scale.
            worldUnitsPerLogicalPixel = logicalPixelScale;
            serviceRange = Mathf.Max(config.ServiceRange, route.RowSpacing * 0.5f + logicalPixelScale * 4f);
            for (int i = 0; i < fans.Count; i++)
            {
                TowerDefenseFanState fan = fans[i];
                var follower = new TowerDefenseFanRouteFollower(fan.Id, routeGeometry.Centerline,
                    sourceBalance.customerSpeed * config.FanSpeed * logicalPixelScale);
                if (follower.Speed > 0f && normalized[i] > 0f)
                    follower.Advance(normalized[i] * follower.TotalDistance / follower.Speed);
                fan.RouteFollower = follower;
            }
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                worker.ServiceZone = ChooseInitialZone(worker.Role);
                worker.TargetZone = worker.ServiceZone;
                worker.Position = ZoneWorkerPosition(worker.Role, worker.ServiceZone, CountPriorWorkers(worker.Role));
                worker.TransferRoute = Array.Empty<Vector2>();
                worker.TransferIndex = 0;
                worker.IsMoving = false;
            }
        }

        private void StepSlice(float deltaTime)
        {
            if (!stopSpawning) elapsed = Mathf.Min(DurationSeconds, elapsed + deltaTime);
            if (elapsed >= DurationSeconds - 0.0001f) TimedOut = true;
            UpdateCompletionState();
            float cookRate = 1f + speedLevel * 0.06f;
            float beverageRate = 1f + speedLevel * Mathf.Max(0.01f, sourceBalance.speedIncrease);
            Kitchen.Advance(deltaTime, cookRate, beverageRate,
                workerCounts[(int)StreetWorkerRole.Parrillero] > 0,
                workerCounts[(int)StreetWorkerRole.ParrilleroPremium] > 0,
                workerCounts[(int)StreetWorkerRole.Cocacolero] > 0,
                workerCounts[(int)StreetWorkerRole.Fernetero] > 0);
            AdvanceActiveCooks(deltaTime, cookRate);
            AdvanceFans(deltaTime);
            AdvanceWorkers(deltaTime);
            DeliverWithinServiceRanges();
            UpdateCompletionState();
            if (!stopSpawning)
            {
                spawnTimer -= deltaTime;
                if (spawnTimer <= 0f && fans.Count < MaximumFans && HasEntrySpacing())
                {
                    SpawnQuotaBalancedFan();
                    spawnTimer = OfficialSpawnInterval();
                }
            }
            if (stopSpawning && fans.Count == 0) Phase = TowerDefenseRunPhase.Finished;
        }

        private void UpdateCompletionState()
        {
            HasReachedAllGoals = true;
            for (int i = 0; i < goalByProduct.Length; i++)
                if (goalByProduct[i] > 0 && deliveredByProduct[i] < goalByProduct[i]) { HasReachedAllGoals = false; break; }
            if (HasReachedAllGoals || TimedOut) stopSpawning = true;
        }

        private float OfficialSpawnInterval()
        {
            int sourceLevel = Mathf.Clamp(config.SourceCampaignLevelId, 0, StreetSimulation.LevelNames.Length - 1);
            float pressure = sourceBalance.levelDemandMultipliers != null && sourceLevel < sourceBalance.levelDemandMultipliers.Length
                ? Mathf.Max(0.1f, sourceBalance.levelDemandMultipliers[sourceLevel]) : 1f;
            float demandFraction = Mathf.Clamp01((sourceBalance.maxPrice - StreetSimulation.FixedProductPrice) /
                Mathf.Max(0.01f, sourceBalance.maxPrice - sourceBalance.minPrice));
            float demandFactor = 0.55f / Mathf.Pow(Mathf.Max(0.08f, demandFraction), Mathf.Max(0.1f, sourceBalance.priceSensitivity));
            return Mathf.Max(0.05f, sourceBalance.customerArrivalSeconds * demandFactor / pressure * config.FanSpawnScale);
        }

        private bool HasEntrySpacing()
        {
            float minimumSpacing = 58f * worldUnitsPerLogicalPixel;
            for (int i = 0; i < fans.Count; i++)
                if (fans[i].RouteFollower.Progress < minimumSpacing) return false;
            return true;
        }

        private void SpawnQuotaBalancedFan()
        {
            if (availableProducts.Length == 0 || fans.Count >= MaximumFans || stopSpawning) return;
            int lineCountLimit = Mathf.Min(CampaignOrderMaximumLineCount, availableProducts.Length);
            int totalWeight = 0;
            for (int size = 1; size <= lineCountLimit; size++) totalWeight += lineCountLimit + 2 - size;
            int roll = random.Next(totalWeight);
            int lineCount = 1;
            for (int size = 1; size <= lineCountLimit; size++)
            {
                roll -= lineCountLimit + 2 - size;
                if (roll < 0) { lineCount = size; break; }
            }
            var pool = new List<int>(availableProducts);
            var poolWeights = new List<float>(pool.Count);
            for (int i = 0; i < pool.Count; i++) poolWeights.Add(ProductDemandWeight(pool[i]));
            var products = new int[lineCount];
            var quantities = new int[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                int selected = WeightedProductIndex(poolWeights);
                products[i] = pool[selected];
                quantities[i] = random.Next(1, CampaignOrderMaximumLineQuantity + 1);
                pool.RemoveAt(selected);
                poolWeights.RemoveAt(selected);
            }
            SortOrderPairs(products, quantities);
            CreateFan(products, quantities);
        }

        private int WeightedProductIndex(List<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0.01f, weights[i]);
            float roll = (float)random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Mathf.Max(0.01f, weights[i]);
                if (roll <= 0f) return i;
            }
            return weights.Count - 1;
        }

        private float ProductDemandWeight(int product)
        {
            if (product < 0 || product >= goalByProduct.Length) return 0.01f;
            int goal = goalByProduct[product];
            if (goal <= 0) return 0.01f;
            int remaining = Mathf.Max(0, goal - deliveredByProduct[product]);
            if (remaining <= 0) return 0.10f; // Completed products stay available for genuine extra sales.

            int alreadyRequested = 0;
            for (int fanIndex = 0; fanIndex < fans.Count; fanIndex++)
            {
                IReadOnlyList<TowerDefenseOrderLine> order = fans[fanIndex].Order;
                for (int lineIndex = 0; lineIndex < order.Count; lineIndex++)
                    if (order[lineIndex].Product == product) alreadyRequested += order[lineIndex].Remaining;
            }
            int unrequested = Mathf.Max(0, remaining - alreadyRequested);
            float timeFraction = Mathf.Clamp01(TimeRemaining / Mathf.Max(1f, DurationSeconds));
            float relativePace = Mathf.Clamp((remaining / (float)goal) / Mathf.Max(0.05f, timeFraction), 0.25f, 4f);
            float quotaNeed = Mathf.Pow(unrequested / (float)goal, 1.25f);
            return 0.08f + quotaNeed * relativePace;
        }

        private static void SortOrderPairs(int[] products, int[] quantities)
        {
            // Keep each quantity attached to the product it was sampled for.
            for (int i = 1; i < products.Length; i++)
            {
                int product = products[i];
                int quantity = quantities[i];
                int j = i - 1;
                while (j >= 0 && products[j] > product)
                {
                    products[j + 1] = products[j];
                    quantities[j + 1] = quantities[j];
                    j--;
                }
                products[j + 1] = product;
                quantities[j + 1] = quantity;
            }
        }

        private bool CreateFan(int[] products, int[] quantities)
        {
            if (routeGeometry == null || routeGeometry.Centerline.Length < 2) return false;
            int id = nextFanId++;
            var follower = new TowerDefenseFanRouteFollower(id, routeGeometry.Centerline,
                sourceBalance.customerSpeed * config.FanSpeed * worldUnitsPerLogicalPixel);
            fans.Add(new TowerDefenseFanState(id, follower, products, quantities));
            return true;
        }

        private void AdvanceFans(float deltaTime)
        {
            for (int i = fans.Count - 1; i >= 0; i--)
            {
                TowerDefenseFanState fan = fans[i];
                fan.AnimationTime += deltaTime;
                fan.ReceiveAnimationSeconds = Mathf.Max(0f, fan.ReceiveAnimationSeconds - deltaTime);
                fan.IsReceiving = fan.ReceiveAnimationSeconds > 0f;
                fan.RouteFollower.Advance(deltaTime);
                if (!fan.RouteFollower.HasExited) continue;
                if (fan.OrderComplete) CompletedOrdersAtExit++;
                else IncompleteOrdersAtExit++;
                fans.RemoveAt(i);
            }
        }

        private void AdvanceActiveCooks(float deltaTime, float cookRate)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != StreetWorkerRole.Parrillero && worker.Role != StreetWorkerRole.ParrilleroPremium) continue;
                if (worker.CookUnitId <= 0)
                {
                    int firstProduct = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
                    int selected = firstProduct + worker.NextCookProduct;
                    StreetFoodUnit unit = Kitchen.ReserveCook(selected, worker.Id);
                    if (unit == null) unit = Kitchen.ReserveCook(firstProduct + 1 - worker.NextCookProduct, worker.Id);
                    if (unit == null) continue;
                    worker.CookUnitId = unit.Id;
                    worker.NextCookProduct = 1 - worker.NextCookProduct;
                }
                if (!Kitchen.AdvanceCook(worker.CookUnitId, worker.Id, deltaTime, cookRate)) continue;
                if (Kitchen.TakeCooked(worker.CookUnitId, worker.Id) && Kitchen.DepositCooked(worker.CookUnitId, worker.Id))
                    worker.CookUnitId = 0;
            }
        }

        private void DeliverWithinServiceRanges()
        {
            for (int f = 0; f < fans.Count; f++)
            {
                TowerDefenseFanState fan = fans[f];
                for (int zone = 0; zone < serviceZones.Length; zone++)
                {
                    if (Vector2.Distance(fan.RouteFollower.Position, serviceZones[zone]) > serviceRange) continue;
                    for (int line = 0; line < fan.Order.Count; line++)
                    {
                        TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                        if (order.Remaining <= 0) continue;
                        StreetWorkerRole role = StreetSpecialties.GetDeliveryRole(order.Product, true);
                        TowerDefenseWorkerState worker = FindWorkerInZone(role, zone);
                        if (worker == null)
                        {
                            RequestWorkerForZone(role, zone);
                            continue;
                        }
                        if (order.Product < 4 && worker.Role == StreetWorkerRole.Mozo)
                            Kitchen.ReserveTableUnit(order.Product, worker.Id);
                        StreetFoodUnit unit = Kitchen.TryTake(order.Product, role, worker.Id);
                        if (unit == null) { Kitchen.ReleaseTableReservation(worker.Id); continue; }
                        if (!Kitchen.Consume(unit.Id, order.Product, worker.Id))
                        {
                            Kitchen.ReturnCarried(unit.Id, worker.Id);
                            Kitchen.ReleaseTableReservation(worker.Id);
                            continue;
                        }
                        if (fan.RecordDelivery(order.Product))
                        {
                            fan.IsReceiving = true;
                            fan.ReceiveAnimationSeconds = 0.35f;
                            deliveredByProduct[order.Product]++;
                            TotalDelivered++;
                            coins = coins > int.MaxValue - Mathf.RoundToInt(StreetSimulation.OfficialProductPrice(order.Product))
                                ? int.MaxValue : coins + Mathf.RoundToInt(StreetSimulation.OfficialProductPrice(order.Product));
                        }
                        break; // One physical unit per worker per service pass; next substep can carry another.
                    }
                }
            }
        }

        private void AdvanceWorkers(float deltaTime)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                worker.AnimationTime += deltaTime;
                if (!worker.IsMoving) continue;
                float movement = Mathf.Max(0f, sourceBalance.workerSpeed) * worldUnitsPerLogicalPixel * deltaTime;
                while (movement > 0f && worker.TransferIndex < worker.TransferRoute.Length)
                {
                    Vector2 target = worker.TransferRoute[worker.TransferIndex];
                    float distance = Vector2.Distance(worker.Position, target);
                    if (distance <= movement || distance <= 0.0001f)
                    {
                        worker.Position = target;
                        worker.TravelDistance += distance;
                        movement -= distance;
                        worker.TransferIndex++;
                    }
                    else
                    {
                        Vector2 direction = (target - worker.Position).normalized;
                        worker.Position += direction * movement;
                        worker.TravelDistance += movement;
                        movement = 0f;
                    }
                }
                if (worker.TransferIndex >= worker.TransferRoute.Length)
                {
                    worker.ServiceZone = worker.TargetZone;
                    worker.IsMoving = false;
                    worker.TransferRoute = Array.Empty<Vector2>();
                }
            }

            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != StreetWorkerRole.Mozo && worker.Role != StreetWorkerRole.Cocacolero && worker.Role != StreetWorkerRole.Fernetero) continue;
                int target = BestServiceZone(worker.Role);
                if (target >= 0 && target != worker.TargetZone) RequestWorkerForZone(worker.Role, target);
            }
        }

        private int BestServiceZone(StreetWorkerRole role)
        {
            int best = -1, bestScore = 0;
            for (int zone = 0; zone < serviceZones.Length; zone++)
            {
                int score = 0;
                for (int f = 0; f < fans.Count; f++)
                {
                    TowerDefenseFanState fan = fans[f];
                    if (Vector2.Distance(fan.RouteFollower.Position, serviceZones[zone]) > serviceRange + 0.4f) continue;
                    for (int line = 0; line < fan.Order.Count; line++)
                    {
                        TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                        if (order.Remaining > 0 && StreetSpecialties.GetDeliveryRole(order.Product, true) == role)
                            score += order.Remaining;
                    }
                }
                if (score > bestScore) { best = zone; bestScore = score; }
            }
            return best;
        }

        private TowerDefenseWorkerState FindWorkerInZone(StreetWorkerRole role, int zone)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role == role && !worker.IsMoving && worker.ServiceZone == zone) return worker;
            }
            return null;
        }

        private void RequestWorkerForZone(StreetWorkerRole role, int zone)
        {
            if (zone < 0 || zone >= serviceZones.Length) return;
            TowerDefenseWorkerState chosen = null;
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != role) continue;
                if (worker.IsMoving) return;
                chosen = worker;
                break;
            }
            if (chosen == null || chosen.ServiceZone == zone) return;
            float gateX = FindLeftGateX();
            Vector2 currentGate = new Vector2(gateX, chosen.Position.y);
            Vector2 targetGate = new Vector2(gateX, serviceZones[zone].y);
            chosen.TargetZone = zone;
            chosen.TransferRoute = new[] { currentGate, targetGate, ZoneWorkerPosition(role, zone, CountPriorWorkers(role)) };
            chosen.TransferIndex = 0;
            chosen.IsMoving = true;
        }

        private float FindLeftGateX()
        {
            float minimum = float.PositiveInfinity;
            IncludeMinimumX(routeGeometry.LeftBoundary, ref minimum);
            IncludeMinimumX(routeGeometry.RightBoundary, ref minimum);
            return minimum - worldUnitsPerLogicalPixel * 5f;
        }

        private static void IncludeMinimumX(Vector2[] points, ref float minimum)
        {
            if (points == null) return;
            for (int i = 0; i < points.Length; i++) if (points[i].x < minimum) minimum = points[i].x;
        }

        private void BuildWorkers()
        {
            workers.Clear();
            for (int roleIndex = 0; roleIndex < workerCounts.Length; roleIndex++)
                for (int i = 0; i < workerCounts[roleIndex]; i++) AddWorker((StreetWorkerRole)roleIndex, ChooseInitialZone((StreetWorkerRole)roleIndex));
        }

        private int ChooseInitialZone(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.Mozo ? 0 : 1;

        private void AddWorker(StreetWorkerRole role, int zone)
        {
            int index = CountPriorWorkers(role);
            workers.Add(new TowerDefenseWorkerState(nextWorkerId++, role, zone, ZoneWorkerPosition(role, zone, index)));
        }

        private int CountPriorWorkers(StreetWorkerRole role)
        {
            int count = 0;
            for (int i = 0; i < workers.Count; i++) if (workers[i].Role == role) count++;
            return count;
        }

        private Vector2 ZoneWorkerPosition(StreetWorkerRole role, int zone, int roleIndex)
        {
            Vector2 basePoint = serviceZones[Mathf.Clamp(zone, 0, serviceZones.Length - 1)];
            float lateral = ((roleIndex % 3) - 1) * worldUnitsPerLogicalPixel * 32f;
            float depth = -((roleIndex / 3) * worldUnitsPerLogicalPixel * 22f);
            return basePoint + new Vector2(lateral, depth);
        }

        private bool ContainsProduct(int product)
        {
            for (int i = 0; i < availableProducts.Length; i++) if (availableProducts[i] == product) return true;
            return false;
        }

        private static TowerDefenseRouteGeometryConfig CloneRoute(TowerDefenseRouteGeometryConfig source)
        {
            return new TowerDefenseRouteGeometryConfig
            {
                RowCount = source.RowCount,
                SegmentLengths = source.SegmentLengths == null ? Array.Empty<float>() : (float[])source.SegmentLengths.Clone(),
                CorridorWidth = source.CorridorWidth,
                RowSpacing = source.RowSpacing,
                TurnRadius = source.TurnRadius,
                PlayableBounds = source.PlayableBounds,
                TurnSegments = source.TurnSegments
            };
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
