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
        public bool IsDeparting { get; internal set; }
        public float DepartureProgress { get; internal set; }
        public bool HasReceivedDelivery { get; internal set; }
        public bool CompletionCounted { get; internal set; }
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
    /// Tower Defense-only runtime coordinator. It owns fans, orders, route delivery and a
    /// dedicated StreetKitchenProduction instance; StreetGame bridges its metrics into campaign
    /// progression, settlement and persistence.
    /// </summary>
    public sealed class TowerDefenseGameMode
    {
        public const int MaxWorkersPerRolePerZone = 3;
        public const float CampaignStepSliceSeconds = 0.05f;
        public const float FanDepartureDurationSeconds = 0.42f;
        private static readonly int[] OrderProductIds = { 0, 4 };
        private const int MaximumSpeedLevel = 9;
        private static TowerDefenseGameMode activeInstance;
        private static bool campaignStepSuspended;

        private readonly TowerDefenseLevelConfig config;
        private readonly StreetBalance sourceBalance;
        private readonly System.Random random;
        private readonly List<TowerDefenseFanState> fans = new List<TowerDefenseFanState>();
        private readonly List<TowerDefenseFanState> serviceOrder = new List<TowerDefenseFanState>();
        private readonly List<TowerDefenseWorkerState> workers = new List<TowerDefenseWorkerState>();
        private readonly int[] deliveredByProduct = new int[7];
        private readonly int[] availableProducts;
        private readonly int[] workerCounts = new int[5];
        private readonly long[] ticketWorkersByRole = new long[5];
        private readonly long[] hiresByRole = new long[5];
        private readonly Vector2[] serviceZones = new Vector2[2];
        private float worldUnitsPerLogicalPixel;
        private float elapsed;
        private int nextFanId = 1, nextWorkerId = 1, speedLevel, spawnedFans, servedFans;
        private int coins;
        private int coinsEarned;
        private int customersAttended;
        private int ordersCompleted;
        private long hireSpendCents, speedSpendCents;
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
        public int[] AvailableProducts => (int[])availableProducts.Clone();
        public int[] DeliveredByProduct => (int[])deliveredByProduct.Clone();
        public int Coins => coins;
        public int CoinsEarned => coinsEarned;
        public int SpeedLevel => speedLevel;
        public int CustomersAttended => customersAttended;
        public int OrdersCompleted => ordersCompleted;
        public long HireSpendCents => hireSpendCents;
        public long SpeedSpendCents => speedSpendCents;
        public long[] HiresByRole => (long[])hiresByRole.Clone();
        public int[] WorkerCountsByRole => (int[])workerCounts.Clone();
        public int ActiveFanCount => fans.Count;
        public int SpawnedFans => spawnedFans;
        public int ServedFans => servedFans;
        public int TotalFanCount => config.TotalFanCount;
        public float Elapsed => elapsed;
        public float ServiceRange => serviceRange;
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }
        public int EscapedFans { get; private set; }
        public int TotalDelivered { get; private set; }
        public float WorldUnitsPerLogicalPixel => worldUnitsPerLogicalPixel;

        /// <summary>Returns the configured, passable service point for one of the first level's work areas.</summary>
        public Vector2 WorkZone(int zone) => zone >= 0 && zone < serviceZones.Length ? serviceZones[zone] : Vector2.zero;

        public TowerDefenseGameMode(TowerDefenseLevelConfig levelConfig, StreetBalance campaignBalance,
            TowerDefenseRouteGeometryConfig runtimeRoute, float logicalPixelScale, Vector2 firstServiceZone, Vector2 secondServiceZone)
            : this(levelConfig, campaignBalance, runtimeRoute, logicalPixelScale, firstServiceZone, secondServiceZone, null, null, -1)
        {
        }

        public TowerDefenseGameMode(TowerDefenseLevelConfig levelConfig, StreetBalance campaignBalance,
            TowerDefenseRouteGeometryConfig runtimeRoute, float logicalPixelScale, Vector2 firstServiceZone, Vector2 secondServiceZone,
            int[] campaignWorkerCounts, long[] campaignTicketWorkersByRole, int startingCoins)
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
            availableProducts = (int[])OrderProductIds.Clone();

            int sourceLevel = Mathf.Clamp(config.SourceCampaignLevelId, 0, StreetSimulation.LevelNames.Length - 1);
            for (int role = 0; role < workerCounts.Length; role++)
            {
                // Camioneros TD starts empty by contract. Keep constructor inputs for call-site
                // compatibility, but never import campaign/ticket workers into this runtime.
                workerCounts[role] = 0;
                ticketWorkersByRole[role] = 0L;
            }
            coins = startingCoins >= 0 ? startingCoins : sourceBalance.GetStartingCoins(sourceLevel);
            Kitchen = new StreetKitchenProduction(sourceBalance.kitchen, availableProducts, null, true, false, true);
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

        /// <summary>Dispose the volatile runtime; campaign persistence and progression are handled by StreetGame.</summary>
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
            if (!IsPlaceableRole(role) || !IsRoleAvailable(role)) return 0;
            return StreetSimulation.CalculateHireCost(sourceBalance, config.SourceCampaignLevelId, role,
                workerCounts[(int)role], ticketWorkersByRole[(int)role]);
        }

        public int WorkersInZone(StreetWorkerRole role, int zone)
        {
            if (zone < 0 || zone >= serviceZones.Length) return 0;
            int count = 0;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Role == role && workers[i].ServiceZone == zone) count++;
            return count;
        }

        public bool CanPlaceWorker(StreetWorkerRole role, int zone)
        {
            return Phase == TowerDefenseRunPhase.Playing && zone >= 0 && zone < serviceZones.Length &&
                IsPlaceableRole(role) && WorkersInZone(role, zone) < MaxWorkersPerRolePerZone &&
                HireCost(role) > 0 && coins >= HireCost(role);
        }

        private static bool IsPlaceableRole(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.Cocacolero;

        public bool TryHire(StreetWorkerRole role, int zone)
        {
            int index = (int)role;
            if (!CanPlaceWorker(role, zone) || index < 0 || index >= workerCounts.Length || workerCounts[index] >= int.MaxValue - workers.Count) return false;
            int cost = HireCost(role);
            coins -= cost;
            hireSpendCents = AddCents(hireSpendCents, (long)cost * 100L);
            hiresByRole[index] = hiresByRole[index] == long.MaxValue ? long.MaxValue : hiresByRole[index] + 1L;
            workerCounts[index]++;
            AddWorker(role, zone);
            return true;
        }

        public int SpeedUpgradeCost => speedLevel >= MaximumSpeedLevel ? 0
            : StreetSimulation.CalculateSpeedCost(sourceBalance, config.SourceCampaignLevelId, speedLevel);

        public bool TryUpgradeSpeed()
        {
            if (Phase != TowerDefenseRunPhase.Playing || speedLevel >= MaximumSpeedLevel || coins < SpeedUpgradeCost || SpeedUpgradeCost <= 0) return false;
            speedSpendCents = AddCents(speedSpendCents, (long)SpeedUpgradeCost * 100L);
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
            if (Phase != TowerDefenseRunPhase.Playing || stopSpawning || spawnedFans >= TotalFanCount || fans.Count >= TotalFanCount ||
                products == null || quantities == null || products.Length < 1 ||
                products.Length > OrderProductIds.Length || products.Length != quantities.Length) return false;
            int[] safeProducts = new int[products.Length];
            int[] safeQuantities = new int[products.Length];
            for (int i = 0; i < products.Length; i++)
            {
                if (!ContainsProduct(products[i]) || quantities[i] < 1 || quantities[i] > 4) return false;
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
                // A placed crew member remains in the area selected by the player after reflow.
                worker.TargetZone = worker.ServiceZone;
                worker.Position = ZoneWorkerPosition(worker.Role, worker.ServiceZone, CountPriorWorkers(worker.Role, worker.ServiceZone, worker));
                worker.TransferRoute = Array.Empty<Vector2>();
                worker.TransferIndex = 0;
                worker.IsMoving = false;
            }
        }

        private void StepSlice(float deltaTime)
        {
            elapsed += deltaTime; // Audit only: Tower Defense has no round deadline.
            float cookRate = 1f + speedLevel * 0.06f;
            float beverageRate = 1f + speedLevel * Mathf.Max(0.01f, sourceBalance.speedIncrease);
            Kitchen.Advance(deltaTime, cookRate, beverageRate,
                workerCounts[(int)StreetWorkerRole.Parrillero] > 0,
                workerCounts[(int)StreetWorkerRole.ParrilleroPremium] > 0,
                workerCounts[(int)StreetWorkerRole.Cocacolero] > 0,
                workerCounts[(int)StreetWorkerRole.Fernetero] > 0);
            AdvanceActiveCooks(deltaTime, cookRate);
            AdvanceFans(deltaTime);
            if (Phase != TowerDefenseRunPhase.Playing) return;
            AdvanceWorkers(deltaTime);
            DeliverWithinServiceRanges();
            if (Phase != TowerDefenseRunPhase.Playing) return;
            if (!stopSpawning)
            {
                if (fans.Count < TotalFanCount && HasEntrySpacing()) SpawnNextFan();
            }
            UpdateCompletionState();
        }

        private void UpdateCompletionState()
        {
            if (HasWon || HasLost) return;
            if (spawnedFans == TotalFanCount && servedFans == TotalFanCount && fans.Count == 0)
            {
                stopSpawning = true;
                HasWon = true;
                Phase = TowerDefenseRunPhase.Finished;
            }
        }

        private bool HasEntrySpacing()
        {
            float minimumSpacing = 58f * worldUnitsPerLogicalPixel;
            for (int i = 0; i < fans.Count; i++)
                if (fans[i].RouteFollower.Progress < minimumSpacing) return false;
            return true;
        }

        private void SpawnNextFan()
        {
            if (stopSpawning || spawnedFans >= TotalFanCount || fans.Count >= TotalFanCount) return;
            if (random.Next(2) == 0)
                CreateFan(new[] { 0, 4 }, new[] { random.Next(1, 5), random.Next(1, 5) });
            else
                CreateFan(new[] { random.Next(2) == 0 ? 0 : 4 }, new[] { random.Next(1, 5) });
        }

        private bool CreateFan(int[] products, int[] quantities)
        {
            if (stopSpawning || spawnedFans >= TotalFanCount || fans.Count >= TotalFanCount ||
                routeGeometry == null || routeGeometry.Centerline.Length < 2) return false;
            int id = nextFanId++;
            var follower = new TowerDefenseFanRouteFollower(id, routeGeometry.Centerline,
                sourceBalance.customerSpeed * config.FanSpeed * worldUnitsPerLogicalPixel);
            fans.Add(new TowerDefenseFanState(id, follower, products, quantities));
            spawnedFans++;
            if (spawnedFans >= TotalFanCount) stopSpawning = true;
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
                if (fan.IsDeparting)
                {
                    fan.DepartureProgress = Mathf.Clamp01(fan.DepartureProgress + deltaTime / FanDepartureDurationSeconds);
                    if (fan.DepartureProgress >= 1f)
                    {
                        fans.RemoveAt(i);
                        servedFans++;
                        UpdateCompletionState();
                    }
                    continue;
                }
                fan.RouteFollower.Advance(deltaTime);
                if (!fan.RouteFollower.HasExited) continue;
                EscapedFans++;
                fans.RemoveAt(i);
                if (!fan.OrderComplete)
                {
                    HasLost = true;
                    stopSpawning = true;
                    Phase = TowerDefenseRunPhase.Finished;
                    return;
                }
            }
        }

        private void AdvanceActiveCooks(float deltaTime, float cookRate)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != StreetWorkerRole.Parrillero) continue;
                if (worker.CookUnitId <= 0)
                {
                    StreetFoodUnit unit = Kitchen.ReserveCook(0, worker.Id);
                    if (unit == null) continue;
                    worker.CookUnitId = unit.Id;
                }
                if (!Kitchen.AdvanceCook(worker.CookUnitId, worker.Id, deltaTime, cookRate)) continue;
                if (Kitchen.TakeCooked(worker.CookUnitId, worker.Id) && Kitchen.DepositCooked(worker.CookUnitId, worker.Id))
                    worker.CookUnitId = 0;
            }
        }

        // Route progress, not list order or straight-line distance, determines urgency.
        // Use a scratch list so targeting does not reorder renderer/customer identities.
        private void DeliverWithinServiceRanges()
        {
            serviceOrder.Clear();
            serviceOrder.AddRange(fans);
            serviceOrder.Sort(CompareServicePriority);
            for (int w = 0; w < workers.Count; w++)
            {
                TowerDefenseWorkerState worker = workers[w];
                if (worker.IsMoving) continue;
                bool delivered = false;
                for (int f = 0; f < serviceOrder.Count && !delivered; f++)
                {
                    TowerDefenseFanState fan = serviceOrder[f];
                    if (fan.OrderComplete || !IsAdjacentToArea(fan, worker.ServiceZone)) continue;
                    for (int line = 0; line < fan.Order.Count; line++)
                    {
                        TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                        if (order.Remaining <= 0 || GetDeliveryRole(order.Product) != worker.Role) continue;
                        StreetFoodUnit unit = Kitchen.TryTake(order.Product, worker.Role, worker.Id);
                        if (unit == null) continue;
                        if (!Kitchen.Consume(unit.Id, order.Product, worker.Id))
                        {
                            Kitchen.ReturnCarried(unit.Id, worker.Id);
                            continue;
                        }
                        if (fan.RecordDelivery(order.Product))
                        {
                            if (!fan.HasReceivedDelivery)
                            {
                                fan.HasReceivedDelivery = true;
                                if (customersAttended < int.MaxValue) customersAttended++;
                            }
                            fan.IsReceiving = true;
                            fan.ReceiveAnimationSeconds = 0.35f;
                            deliveredByProduct[order.Product]++;
                            TotalDelivered++;
                            int sale = Mathf.RoundToInt(StreetSimulation.OfficialProductPrice(order.Product));
                            coins = coins > int.MaxValue - sale ? int.MaxValue : coins + sale;
                            coinsEarned = coinsEarned > int.MaxValue - sale ? int.MaxValue : coinsEarned + sale;
                            if (fan.OrderComplete && !fan.CompletionCounted)
                            {
                                fan.CompletionCounted = true;
                                if (ordersCompleted < int.MaxValue) ordersCompleted++;
                                fan.IsDeparting = true;
                                fan.DepartureProgress = 0f;
                            }
                        }
                        delivered = true; // One physical unit per worker per service pass.
                        break;
                    }
                }
            }
        }

        private static int CompareServicePriority(TowerDefenseFanState a, TowerDefenseFanState b)
        {
            int progress = b.RouteFollower.Progress.CompareTo(a.RouteFollower.Progress);
            return progress != 0 ? progress : a.Id.CompareTo(b.Id);
        }

        private bool IsAdjacentToArea(TowerDefenseFanState fan, int zone)
        {
            if (zone < 0 || zone >= serviceZones.Length || fan.RouteFollower.HasExited) return false;
            // Each shared area borders its upper/lower horizontal rows and their
            // own connecting turn. The next connector and third row are not adjacent.
            int stride = route.TurnSegments + 1;
            int start = zone * stride, end = (zone + 1) * stride + 1;
            Vector2[] points = routeGeometry.Centerline;
            if (end >= points.Length) return false;
            float from = 0f, to = 0f;
            for (int i = 1; i <= end; i++)
            {
                float length = Vector2.Distance(points[i - 1], points[i]);
                to += length;
                if (i <= start) from += length;
            }
            float progress = fan.RouteFollower.Progress;
            return progress >= from && progress <= to;
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
            int index = CountPriorWorkers(role, zone);
            workers.Add(new TowerDefenseWorkerState(nextWorkerId++, role, zone, ZoneWorkerPosition(role, zone, index)));
        }

        private int CountPriorWorkers(StreetWorkerRole role, int zone, TowerDefenseWorkerState current = null)
        {
            int count = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker == current) break;
                if (worker.Role == role && worker.ServiceZone == zone) count++;
            }
            return count;
        }

        private static StreetWorkerRole GetDeliveryRole(int product) =>
            product >= 0 && product <= 3 ? StreetWorkerRole.Parrillero : StreetSpecialties.GetRequiredWorkerRole(product);

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

        private static long AddCents(long total, long amount) => amount > 0 && total > long.MaxValue - amount
            ? long.MaxValue : Math.Max(0L, total) + Math.Max(0L, amount);

        private static TowerDefenseRouteGeometryConfig CloneRoute(TowerDefenseRouteGeometryConfig source)
        {
            return new TowerDefenseRouteGeometryConfig
            {
                RowCount = source.RowCount,
                SegmentLengths = source.SegmentLengths == null ? Array.Empty<float>() : (float[])source.SegmentLengths.Clone(),
                CorridorWidth = source.CorridorWidth,
                HorizontalCorridorWidth = source.HorizontalCorridorWidth,
                RowSpacing = source.RowSpacing,
                TurnRadius = source.TurnRadius,
                PlayableBounds = source.PlayableBounds,
                TurnSegments = source.TurnSegments
            };
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
