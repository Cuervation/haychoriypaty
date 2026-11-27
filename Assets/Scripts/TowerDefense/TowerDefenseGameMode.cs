using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum TowerDefenseRunPhase { Ready, Playing, Paused, Finished }

    /// <summary>Receipt of one consumed unit, never a request to create inventory or revenue.</summary>
    public readonly struct TowerDefenseDelivery
    {
        public readonly int UnitId, Product, WorkerId, FanId, Zone;
        public readonly Vector2 Source, Target;
        public readonly float Elapsed;
        public TowerDefenseDelivery(int unitId, int product, int workerId, int fanId, int zone,
            Vector2 source, Vector2 target, float elapsed)
        {
            UnitId = unitId; Product = product; WorkerId = workerId; FanId = fanId; Zone = zone;
            Source = source; Target = target; Elapsed = elapsed;
        }
    }

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
        internal int ReservedByWorker;
        public int Id { get; }
        public TowerDefenseFanRouteFollower RouteFollower { get; internal set; }
        public int AppearanceId { get; internal set; }
        public CamionerosFanProfile Profile { get; internal set; }
        /// <summary>Difficulty-ramp speed multiplier fixed when the fan is born (1 when the level has no ramp).</summary>
        public float SpeedMultiplier { get; internal set; } = 1f;
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
            AppearanceId = id;
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
        public float ServeAnimationSeconds { get; internal set; }
        public float DeliveryCooldownSeconds { get; internal set; }
        public Vector2 LastServiceTarget { get; internal set; }

        internal int TargetZone;
        internal Vector2[] TransferRoute;
        internal int TransferIndex;
        public bool IsCooking { get; internal set; }
        public int CurrentFoodId => ServiceUnitId > 0 ? ServiceUnitId : CookUnitId;
        internal int CookUnitId;
        /// <summary>Seconds left standing at the table before putting the cooked sandwich down; negative while it has not started.</summary>
        internal float CookHandoffSeconds = -1f;
        /// <summary>Seconds a cook holding a cooked sandwich has found no walkable way to any handoff point (2026-11-26 stuck-cook fix).</summary>
        internal float CookHandoffBlockedSeconds;
        internal int NextCookProduct;
        internal int ServiceUnitId;
        internal int PendingFanId;
        internal int PendingProduct = -1;
        internal int ServiceStage;
        // Nueva Chicago's grab and hand-over dwell (StreetSimulation Pickup/Handoff states).
        internal bool ServiceDwelling;
        internal float ServiceDwellSeconds;
        internal Vector2 PostPosition;
        internal float WaitJitter = float.NaN; // C levels: where in the waiting band this stop falls (-1..1), re-rolled after every job
        internal bool ReturningToPost;

        internal TowerDefenseWorkerState(int id, StreetWorkerRole role, int zone, Vector2 position)
        {
            Id = id;
            Role = role;
            ServiceZone = zone;
            TargetZone = zone;
            Position = position;
            PostPosition = position;
            TransferRoute = Array.Empty<Vector2>();
        }
    }

    /// <summary>
    /// Tower Defense-only runtime coordinator. It owns fans, orders, route delivery and a
    /// dedicated StreetKitchenProduction instance; StreetGame bridges its metrics into campaign
    /// progression, settlement and persistence.
    /// </summary>
    public sealed partial class TowerDefenseGameMode
    {
        public const int MaxWorkersPerRolePerZone = 3;
        public const float CampaignStepSliceSeconds = 0.05f;
        public const float FanDepartureDurationSeconds = 0.42f;
        public const float DeliveryIntervalSeconds = 0.62f;
        public const float DeliveryCueDurationSeconds = DeliveryIntervalSeconds;
        public const float WorkerVisualHeightLogical = 94f;
        public const float WorkerVisualPivotY = 0.02f;
        // Los Redondos sector composition (full-size Camioneros art): the station sits on
        // the sector floor and its crew stands behind it with feet hidden at this fraction
        // of the station height, like the Camioneros counter.
        public const float LosRedondosStationFloorInsetLogical = 1f;
        public const float LosRedondosCrewFeetStationFraction = .38f;
        public const float LosRedondosSectorSideInsetLogical = 2f;
        public const float WorkerFenceClearanceLogical = 8f;
        public const float StationOuterClearanceLogical = 2f;
        public const float WorkerStationClearanceLogical = 4f;
        public const float ParrilleroVisualWidthLogical = 83f;
        public const float CocacoleroVisualWidthLogical = 94f;
        public const float TableStationVerticalInsetLogical = 8f;
        public const float BarrelStationVerticalInsetLogical = 0f;
        public const float StationRenderYOffsetLogical = 18f;
        private const float ParallelWorkerSpacingLogical = 84f;
        private const float ParallelCocacoleroSpacingLogical = 96f;
        private const float CamionerosCrewVisualOverlapLogical = 10f;
        // The Parrillero tight mesh has the source-frame aspect ratio (266x303) at this height.
        // Subtract ten logical pixels so each adjacent worker silhouette overlaps slightly.
        private const float CamionerosParrilleroSpacingLogical = WorkerVisualHeightLogical * 266f / 303f - CamionerosCrewVisualOverlapLogical;
        // Unity's SpriteMeshType.Tight bounds for matched Cocacolero frame 0 are 68.10366 logical px
        // at the 94 px render height; use this actual mesh width to calculate crew overlap.
        private const float CamionerosCocacoleroStandingWidthLogical = 68.10366f;
        private const float CamionerosCocacoleroSpacingLogical = CamionerosCocacoleroStandingWidthLogical - CamionerosCrewVisualOverlapLogical;
        private const float WorkerRoleRowSeparationLogical = 110f;
        private const float CompactStationSpacingLogical = 140f;
        private const float CamionerosWorkerRowOffsetLogical = 8f;
        private static readonly int[] OrderProductIds = { 0, 4 };
        private const int MaximumSpeedLevel = 9;
        private static TowerDefenseGameMode activeInstance;
        private static bool campaignStepSuspended;

        private readonly TowerDefenseLevelConfig config;
        private readonly StreetBalance sourceBalance;
        private readonly System.Random random;
        private readonly List<TowerDefenseFanState> fans = new List<TowerDefenseFanState>();
        private readonly List<TowerDefenseFanState> serviceOrder = new List<TowerDefenseFanState>();
        private readonly HashSet<int> servicedWorkers = new HashSet<int>();
        private readonly List<TowerDefenseWorkerState> workers = new List<TowerDefenseWorkerState>();
        private readonly int[] deliveredByProduct = new int[7];
        private readonly int[] availableProducts;
        private readonly int[] workerCounts = new int[5];
        private int[] startingCrewCounts = new int[5];
        private readonly long[] ticketWorkersByRole = new long[5];
        private readonly long[] hiresByRole = new long[5];
        private readonly Vector2[] serviceZones = new Vector2[2];
        private float worldUnitsPerLogicalPixel;
        private float elapsed;
        private float nextFanSpawnAt;
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
        private Vector2[] fanRouteWaypoints;

        public static bool OwnsRuntime => activeInstance != null;
        public static bool CampaignStepSuspended => campaignStepSuspended;
        public static void SetCampaignStepSuspended(bool suspended) => campaignStepSuspended = suspended;
        public string ModeId => config.ModeId;
        public string LevelId => config.LevelId;
        public string DisplayName => config.DisplayName;
        public bool FreeConstruction => config.FreeConstruction;
        public bool LastReflowSucceeded { get; private set; } = true;
        public TowerDefenseRouteKind RouteKind => config.RouteKind;
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
        public event Action<TowerDefenseDelivery> DeliveryCompleted;

        /// <summary>Returns the configured, passable service point for one of the first level's work areas.</summary>
        public Vector2 WorkZone(int zone) => zone >= 0 && zone < serviceZones.Length ? serviceZones[zone] : Vector2.zero;

        private bool UsesSplitWorkerRows
        {
            get
            {
                float corridor = route.HorizontalCorridorWidth > 0f ? route.HorizontalCorridorWidth : route.CorridorWidth;
                float clearHeightLogical = Mathf.Max(0f, route.RowSpacing - corridor) /
                    Mathf.Max(0.0001f, worldUnitsPerLogicalPixel);
                return clearHeightLogical >= WorkerVisualHeightLogical + WorkerRoleRowSeparationLogical +
                    WorkerFenceClearanceLogical * 2f;
            }
        }

        public float CurrentStationSpacingLogical => config.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
            ? Mathf.Abs(StationVisualPosition(StreetWorkerRole.Parrillero, 0).x -
                StationVisualPosition(StreetWorkerRole.Cocacolero, 0).x) / Mathf.Max(.0001f, worldUnitsPerLogicalPixel)
            : config.SourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId
                ? Vector2.Distance(StationPosition(StreetWorkerRole.Parrillero, 0),
                    StationPosition(StreetWorkerRole.Cocacolero, 0)) / Mathf.Max(.0001f, worldUnitsPerLogicalPixel)
            : UsesSplitWorkerRows
                ? Mathf.Abs(StationPosition(StreetWorkerRole.Parrillero, 0).x -
                    StationPosition(StreetWorkerRole.Cocacolero, 0).x) / Mathf.Max(.0001f, worldUnitsPerLogicalPixel)
                : CompactStationSpacingLogical;

        // Stable base station/service anchors for the two player-selected work areas.
        // The presenter may apply a level-specific visual offset through StationVisualPosition;
        // gameplay still uses these anchors for the shared non-Camioneros layouts and metrics.
        public Vector2 StationPosition(StreetWorkerRole role, int zone)
        {
            Vector2 center = WorkZone(zone);
            if (IsLosRedondos)
                return LosRedondosStationCenter(role, zone);
            if (!UsesSplitWorkerRows)
                return center + Vector2.right * ((role == StreetWorkerRole.Parrillero ? -.5f : .5f) *
                    CompactStationSpacingLogical * worldUnitsPerLogicalPixel);

            Vector2 area = WorkAreaHorizontalBounds(zone);
            float width = role == StreetWorkerRole.Parrillero
                ? StreetKitchenLayout.TableWidth : StreetKitchenLayout.BarrelWidth;
            float x = role == StreetWorkerRole.Parrillero
                ? area.x + (StationOuterClearanceLogical + width * .5f) * worldUnitsPerLogicalPixel
                : area.y - (StationOuterClearanceLogical + width * .5f) * worldUnitsPerLogicalPixel;

            float clearHalfHeight = ClearHalfHeightLogical();
            float stationHalfHeight = role == StreetWorkerRole.Parrillero
                ? StreetKitchenLayout.SandwichTableHeight * .5f : StreetKitchenLayout.BarrelHeight * .5f;
            // The presentation renderer lowers each station root by 18 logical pixels.
            // Put the table near the upper rail and the taller barrel near the lower rail,
            // leaving the two worker rows free while preserving each station's exact size.
            float renderedStationY = role == StreetWorkerRole.Parrillero
                ? center.y + (clearHalfHeight - stationHalfHeight - TableStationVerticalInsetLogical) * worldUnitsPerLogicalPixel
                : center.y - (clearHalfHeight - stationHalfHeight - BarrelStationVerticalInsetLogical) * worldUnitsPerLogicalPixel;
            float anchorY = renderedStationY + StationRenderYOffsetLogical * worldUnitsPerLogicalPixel;
            return new Vector2(x, anchorY);
        }

        /// <summary>
        /// Visual-only Camioneros placement. Both stations share one depth line; each station
        /// is centered on its own crew, with the two groups pushed to opposite safe edges.
        /// </summary>
        public Vector2 StationVisualPosition(StreetWorkerRole role, int zone)
        {
            Vector2 anchor = StationPosition(role, zone);
            // The presenter lowers every station root by StationRenderYOffsetLogical.
            if (IsLosRedondos)
                return anchor + Vector2.up * StationRenderYOffsetLogical * worldUnitsPerLogicalPixel;
            if (!IsCamionerosCrew(role, zone)) return anchor;

            Vector2 groupBounds = CamionerosWorkerGroupCenterBoundsX(role, zone);
            if (groupBounds.y < groupBounds.x) return anchor;
            float x = role == StreetWorkerRole.Parrillero ? groupBounds.x : groupBounds.y;
            // The renderer subtracts StationRenderYOffsetLogical, so this places both
            // furniture roots on the centerline of the same work area.
            float y = WorkZone(zone).y + StationRenderYOffsetLogical * worldUnitsPerLogicalPixel;
            return new Vector2(x, y);
        }

        // The five-run serpentine with right-hand bays; the horizontal kitchen version of the level has none of it.
        private bool IsLosRedondos => config.SourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId && !config.FreeConstruction;

        /// <summary>
        /// Los Redondos sector for a role/zone (P0, C0, P1, C1 from top to bottom), taken from
        /// the same right-side work column that the route layout validated. Other levels
        /// have no right-side sectors and return an empty rect.
        /// </summary>
        public Rect RightBayBounds(StreetWorkerRole role, int zone) => IsLosRedondos
            ? LosRedondosLayout.WorldSector(route.WorkBounds, LosRedondosLayout.SectorIndex(role, zone))
            : Rect.zero;

        /// <summary>World footprint of a Los Redondos table/barrel at its full Camioneros size.</summary>
        public Rect LosRedondosStationBounds(StreetWorkerRole role, int zone)
        {
            Vector2 center = LosRedondosStationCenter(role, zone);
            float width = (role == StreetWorkerRole.Parrillero ? StreetKitchenLayout.TableWidth : StreetKitchenLayout.BarrelWidth) *
                worldUnitsPerLogicalPixel;
            float height = LosRedondosStationHeightLogical(role) * worldUnitsPerLogicalPixel;
            return new Rect(center.x - width * .5f, center.y - height * .5f, width, height);
        }

        /// <summary>World footprint of a full-size (94 px) worker standing at its feet position.</summary>
        public Rect LosRedondosWorkerBounds(TowerDefenseWorkerState worker)
        {
            float width = (worker.Role == StreetWorkerRole.Parrillero
                ? WorkerVisualHeightLogical * 266f / 303f : CamionerosCocacoleroStandingWidthLogical) * worldUnitsPerLogicalPixel;
            float height = WorkerVisualHeightLogical * worldUnitsPerLogicalPixel;
            return new Rect(worker.Position.x - width * .5f, worker.Position.y - height * WorkerVisualPivotY, width, height);
        }

        /// <summary>
        /// Audits the live ID10 placement: the shared route validator plus every station and
        /// worker inside its own right-side sector, clear of the fenced route (and its art),
        /// and never overlapping another sector's objects or another station. Empty = valid.
        /// </summary>
        public string[] AuditLosRedondosLayout()
        {
            var issues = new List<string>();
            if (!IsLosRedondos) return issues.ToArray();
            float px = worldUnitsPerLogicalPixel;
            TowerDefenseRouteLayoutReport report = TowerDefenseResponsiveLayout.ValidateLosRedondos(route, routeGeometry, px);
            for (int i = 0; i < report.Issues.Count; i++) issues.Add(report.Issues[i].ToString());
            float art = LosRedondosLayout.FenceArtHalfThicknessLogical * px;
            Rect routeArt = Rect.MinMaxRect(route.PlayableBounds.xMin - art, route.PlayableBounds.yMin - art,
                route.PlayableBounds.xMax + art, route.PlayableBounds.yMax + art);
            float tol = .01f * px;
            var rects = new List<Rect>();
            var sectorOf = new List<int>();
            var labels = new List<string>();
            for (int zone = 0; zone < 2; zone++)
            for (int r = 0; r < 2; r++)
            {
                StreetWorkerRole role = r == 0 ? StreetWorkerRole.Parrillero : StreetWorkerRole.Cocacolero;
                rects.Add(LosRedondosStationBounds(role, zone));
                sectorOf.Add(LosRedondosLayout.SectorIndex(role, zone));
                labels.Add((role == StreetWorkerRole.Parrillero ? "mesa " : "barril ") + "zona " + zone);
            }
            for (int i = 0; i < workers.Count; i++)
            {
                rects.Add(LosRedondosWorkerBounds(workers[i]));
                sectorOf.Add(LosRedondosLayout.SectorIndex(workers[i].Role, workers[i].ServiceZone));
                labels.Add(workers[i].Role + " #" + workers[i].Id + " zona " + workers[i].ServiceZone);
            }
            for (int i = 0; i < rects.Count; i++)
            {
                Rect sector = LosRedondosLayout.WorldSector(route.WorkBounds, sectorOf[i]);
                if (rects[i].xMin < sector.xMin - tol || rects[i].xMax > sector.xMax + tol ||
                    rects[i].yMin < sector.yMin - tol || rects[i].yMax > sector.yMax + tol)
                    issues.Add(labels[i] + " " + rects[i] + " sale de su sector " + sectorOf[i] + " " + sector);
                if (RectsOverlap(rects[i], routeArt, tol))
                    issues.Add(labels[i] + " " + rects[i] + " invade el recorrido vallado " + routeArt);
                for (int j = i + 1; j < rects.Count; j++)
                {
                    bool bothStations = i < 4 && j < 4;
                    if ((bothStations || sectorOf[i] != sectorOf[j]) && RectsOverlap(rects[i], rects[j], tol))
                        issues.Add(labels[i] + " se superpone con " + labels[j] + " en " + rects[i].center);
                }
            }
            return issues.ToArray();
        }

        private static bool RectsOverlap(Rect a, Rect b, float tol) =>
            a.xMin < b.xMax - tol && a.xMax > b.xMin + tol && a.yMin < b.yMax - tol && a.yMax > b.yMin + tol;

        private static float LosRedondosStationHeightLogical(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero ? StreetKitchenLayout.SandwichTableHeight : StreetKitchenLayout.BarrelHeight;

        private Vector2 LosRedondosStationCenter(StreetWorkerRole role, int zone)
        {
            Rect sector = RightBayBounds(role, zone);
            float bottom = sector.yMin + LosRedondosStationFloorInsetLogical * worldUnitsPerLogicalPixel;
            return new Vector2(sector.center.x,
                bottom + LosRedondosStationHeightLogical(role) * .5f * worldUnitsPerLogicalPixel);
        }

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
            fanRouteWaypoints = BuildFanRouteWaypoints(routeGeometry, route.FanEntryExtensionWorldUnits);

            serviceZones[0] = firstServiceZone;
            serviceZones[1] = secondServiceZone;
            serviceRange = Mathf.Max(config.ServiceRange, route.RowSpacing * 0.5f + worldUnitsPerLogicalPixel * 4f);
            availableProducts = config.AvailableProducts.Length > 0 ? config.AvailableProducts : (int[])OrderProductIds.Clone();

            int sourceLevel = Mathf.Clamp(config.SourceCampaignLevelId, 0, StreetSimulation.LevelNames.Length - 1);
            int[] configuredStartingCrew = config.StartingWorkersByRole;
            for (int role = 0; role < workerCounts.Length; role++)
            {
                // Never import campaign/ticket workers into TD. A level may explicitly author a
                // starting crew; Luján's explicit roster is authored by its level config.
                workerCounts[role] = config.FreeConstruction && configuredStartingCrew != null && role < configuredStartingCrew.Length
                    ? Mathf.Clamp(configuredStartingCrew[role], 0, config.MaximumWorkersPerRole) : 0;
                ticketWorkersByRole[role] = 0L;
            }
            startingCrewCounts = (int[])workerCounts.Clone();
            coins = startingCoins >= 0 ? startingCoins : TowerDefenseEconomyProfile.StartingCoins;
            // Nueva Chicago's kitchen is the traditional one: nobody cooks, the table is always full of sandwiches and the
            // Parrillero only serves them. Every other Tower Defense level (and Luján) keeps active cooks.
            Kitchen = new StreetKitchenProduction(sourceBalance.kitchen, availableProducts, null, !IsPassiveKitchenLevel, IsPassiveKitchenLevel, !config.FreeConstruction, false, NarrowGrills);
            random = new System.Random(sourceBalance.randomSeed + sourceLevel * 97 + config.LevelId.GetHashCode());
            InitializeStartingStations();
            BuildWorkers();
            WarmPathLattices();
        }

        public void Begin()
        {
            if (Phase != TowerDefenseRunPhase.Ready) return;
            activeInstance = this;
            campaignStepSuspended = true;
            WarmPathLattices(); // a no-op when the screen layout did not change since construction
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
            if (config.FreeConstruction)
            {
                if (!IsPlaceableRole(role) || !IsRoleAvailable(role)) return 0;
                // At the per-role cap (Camioneros, Peregrinación: 3) there is nothing left to buy: cost 0 makes the card read "MAX"
                // and stay unavailable instead of showing a price that can no longer be paid (2026-11-26, a pedido).
                if (WorkersCount(role) >= config.MaximumWorkersPerRole) return 0;
                if (config.UseCampaignEconomyPricing)
                    return StreetSimulation.CalculateHireCost(sourceBalance, config.EconomicBalanceCampaignLevelId,
                        role, workerCounts[(int)role], 0L);
                if (config.HirePricingCampaignLevelId >= 0)
                {
                    // La Renga copies Ferro's worker prices and their growth (x1, x2.5, x5, ...): the crew it starts with is free and only
                    // the workers bought count, plus the one Parrillero that Ferro itself starts with.
                    int bought = Math.Max(0, workerCounts[(int)role] - startingCrewCounts[(int)role]);
                    // A role with its own first price (Los Redondos' Fernetero: 120, 2026-11-26) keeps the same growth from that price.
                    int ownBase = config.HireBaseCostOverride(role);
                    if (ownBase > 0) return StreetSimulation.ComputeHireCost(ownBase, bought + 1);
                    int ferroStartingCrew = role == StreetWorkerRole.Parrillero ? 1 : 0;
                    return StreetSimulation.CalculateHireCost(sourceBalance, config.HirePricingCampaignLevelId, role, bought + ferroStartingCrew, 0L);
                }
                return config.HireCostForRole(role);
            }
            if (!IsPlaceableRole(role) || !IsRoleAvailable(role)) return 0;
            return TowerDefenseEconomyProfile.CalculateHireCost(role,
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
            if (config.FreeConstruction)
                return Phase == TowerDefenseRunPhase.Playing && zone == 0 && IsPlaceableRole(role) &&
                    WorkersCount(role) < config.MaximumWorkersPerRole && HasStationForRole(role) &&
                    HireCost(role) > 0 && coins >= HireCost(role);
            return Phase == TowerDefenseRunPhase.Playing && zone >= 0 && zone < serviceZones.Length &&
                IsPlaceableRole(role) && WorkersInZone(role, zone) < MaxWorkersPerRolePerZone &&
                HireCost(role) > 0 && coins >= HireCost(role);
        }

        private bool IsPlaceableRole(StreetWorkerRole role) => config.FreeConstruction
            ? (int)role >= (int)StreetWorkerRole.Parrillero && (int)role <= (int)StreetWorkerRole.Mozo
            : role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.Cocacolero;

        public bool TryHire(StreetWorkerRole role, int zone)
        {
            if (config.FreeConstruction) return false;
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
            : config.FreeConstruction && config.UseCampaignEconomyPricing
                ? StreetSimulation.CalculateSpeedCost(sourceBalance, config.EconomicBalanceCampaignLevelId, speedLevel)
                : config.FreeConstruction && config.HirePricingCampaignLevelId >= 0
                    ? StreetSimulation.CalculateSpeedCost(sourceBalance, config.HirePricingCampaignLevelId, speedLevel) // La Renga: Ferro's speed prices
                    : TowerDefenseEconomyProfile.CalculateSpeedCost(speedLevel);

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
                products.Length > (config.FreeConstruction ? 7 : OrderProductIds.Length) || products.Length != quantities.Length) return false;
            int[] safeProducts = new int[products.Length];
            int[] safeQuantities = new int[products.Length];
            for (int i = 0; i < products.Length; i++)
            {
                if (!ContainsProduct(products[i]) || quantities[i] < 1 || quantities[i] > 5) return false;
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
            LastReflowSucceeded = false;
            if (config.FreeConstruction && !IsBocaKitchenLevel && !FreeLayoutFitsReflow(runtimeRoute.WorkBounds, logicalPixelScale))
            { Pause(); return; }
            Rect previousWorkBounds = route.WorkBounds;
            float previousPixelScale = worldUnitsPerLogicalPixel;
            var oldStations = config.FreeConstruction ? new List<TowerDefensePlacedStation>(placedStations) : null;
            var oldWorkerPositions = new Vector2[workers.Count];
            var oldWorkerPosts = new Vector2[workers.Count];
            for (int i = 0; i < workers.Count; i++) { oldWorkerPositions[i] = workers[i].Position; oldWorkerPosts[i] = workers[i].PostPosition; }
            float[] normalized = new float[fans.Count];
            for (int i = 0; i < fans.Count; i++) normalized[i] = fans[i].RouteFollower.TotalDistance <= 0f ? 0f :
                fans[i].RouteFollower.Progress / fans[i].RouteFollower.TotalDistance;
            Vector2[] nextWaypoints = BuildFanRouteWaypoints(nextGeometry, runtimeRoute.FanEntryExtensionWorldUnits);
            float[] profileProgress = UsesCamionerosProfiles ? CamionerosReflowProgress(normalized, nextWaypoints, logicalPixelScale) : null;
            if (UsesCamionerosProfiles && profileProgress == null) { Pause(); return; }
            route = CloneRoute(runtimeRoute);
            routeGeometry = nextGeometry;
            fanRouteWaypoints = BuildFanRouteWaypoints(routeGeometry, route.FanEntryExtensionWorldUnits);
            serviceZones[0] = firstServiceZone;
            serviceZones[1] = secondServiceZone;
            // Followers and worker offsets below must be rebuilt using the new viewport scale.
            worldUnitsPerLogicalPixel = logicalPixelScale;
            serviceRange = Mathf.Max(config.ServiceRange, route.RowSpacing * 0.5f + logicalPixelScale * 4f);
            if (config.FreeConstruction)
            {
                placedStations.Clear();
                for (int i = 0; i < oldStations.Count; i++)
                {
                    TowerDefensePlacedStation old = oldStations[i];
                    float ratio = logicalPixelScale / Mathf.Max(.001f, previousPixelScale);
                    Vector2 center = route.WorkBounds.min + (old.Bounds.center - previousWorkBounds.min) * ratio;
                    Vector2 size = old.Bounds.size * ratio;
                    Rect bounds = new Rect(center - size * .5f, size);
                    Vector2 work = route.WorkBounds.min + (old.WorkPosition - previousWorkBounds.min) * ratio;
                    Vector2 pickup = new Vector2(center.x, bounds.yMin);
                    placedStations.Add(new TowerDefensePlacedStation(old.Id, old.Kind, bounds, work, pickup));
                }
                if (IsBocaKitchenLevel) ReflowBocaKitchen();
                for (int i = 0; i < workers.Count; i++)
                {
                    TowerDefenseWorkerState worker = workers[i];
                    Vector2 old = oldWorkerPositions[i];
                    float workerRatio = logicalPixelScale / Mathf.Max(.001f, previousPixelScale);
                    worker.Position = route.WorkBounds.min + (old - previousWorkBounds.min) * workerRatio;
                    worker.PostPosition = route.WorkBounds.min + (oldWorkerPosts[i] - previousWorkBounds.min) * workerRatio;
                    worker.ReturningToPost = false;
                    worker.TransferRoute = Array.Empty<Vector2>(); worker.TransferIndex = 0; worker.IsMoving = false;
                    worker.IsCooking = false;
                    // Existing carried/cooking units and service claims survive a purely visual resize.
                }
                if (IsBocaKitchenLevel) AssignAllRolePosts();
            }
            for (int i = 0; i < fans.Count; i++)
            {
                TowerDefenseFanState fan = fans[i];
                var follower = new TowerDefenseFanRouteFollower(fan.Id, fanRouteWaypoints,
                    FanSpeedForProfile(fan.Profile, logicalPixelScale) * fan.SpeedMultiplier);
                float progress = profileProgress == null ? normalized[i] * follower.TotalDistance : profileProgress[i];
                if (follower.Speed > 0f && progress > 0f)
                    follower.Advance(progress / follower.Speed);
                fan.RouteFollower = follower;
            }
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                // A placed crew member remains in the area selected by the player after reflow.
                worker.TargetZone = worker.ServiceZone;
                if (!config.FreeConstruction)
                    worker.Position = ZoneWorkerPosition(worker.Role, worker.ServiceZone,
                        CountPriorWorkers(worker.Role, worker.ServiceZone, worker), WorkersInZone(worker.Role, worker.ServiceZone));
                worker.TransferRoute = Array.Empty<Vector2>();
                worker.TransferIndex = 0;
                worker.IsMoving = false;
            }
            LastReflowSucceeded = true;
        }

        private void StepSlice(float deltaTime)
        {
            elapsed += deltaTime; // Audit only: Tower Defense has no round deadline.
            float cookRate = 1f + speedLevel * 0.06f;
            float beverageRate = 1f + speedLevel * Mathf.Max(0.01f, sourceBalance.speedIncrease);
            bool freeBuild = config.FreeConstruction;
            bool normalCook = workerCounts[(int)StreetWorkerRole.Parrillero] > 0 && (!freeBuild || HasStation(TowerDefenseStationKind.NormalGrill));
            bool premiumCook = workerCounts[(int)StreetWorkerRole.ParrilleroPremium] > 0 && (!freeBuild || HasStation(TowerDefenseStationKind.PremiumGrill));
            bool cocaWorker = workerCounts[(int)StreetWorkerRole.Cocacolero] > 0;
            bool fernetero = workerCounts[(int)StreetWorkerRole.Fernetero] > 0 && (!freeBuild || HasStation(TowerDefenseStationKind.FernetTable));
            if (freeBuild) cocaWorker &= HasStation(TowerDefenseStationKind.CocaBarrel) && (HasStation(TowerDefenseStationKind.BeerBarrel) || !ContainsProduct(6));
            Kitchen.Advance(deltaTime, cookRate, beverageRate, normalCook, premiumCook, cocaWorker, fernetero);
            if (freeBuild) AdvanceFreeConstructionCooks(deltaTime, cookRate);
            else AdvanceActiveCooks(deltaTime, cookRate);
            AdvanceFans(deltaTime);
            if (Phase != TowerDefenseRunPhase.Playing) return;
            AdvanceWorkers(deltaTime);
            DeliverWithinServiceRanges();
            if (Phase != TowerDefenseRunPhase.Playing) return;
            if (!stopSpawning)
            {
                if (fans.Count < TotalFanCount && HasEntrySpacing() && (!freeBuild || elapsed >= nextFanSpawnAt)) SpawnNextFan();
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
            if (UsesCamionerosProfiles)
            {
                float headway = NextCamionerosProfile.HeadwayLogical * worldUnitsPerLogicalPixel;
                for (int i = 0; i < fans.Count; i++)
                    if (!fans[i].IsDeparting && fans[i].RouteFollower.Progress < headway) return false;
                return true;
            }
            float minimumSpacing = config.MinimumEntrySpacingLogical * worldUnitsPerLogicalPixel;
            for (int i = 0; i < fans.Count; i++)
                if (fans[i].RouteFollower.Progress < minimumSpacing) return false;
            return true;
        }

        private void SpawnNextFan()
        {
            if (stopSpawning || spawnedFans >= TotalFanCount || fans.Count >= TotalFanCount) return;
            if (UsesCamionerosProfiles) { SpawnCamionerosOrder(); return; }
            if (config.FreeConstruction && config.HasDifficultyRamp)
            {
                // Difficulty ramp (La Renga, Camioneros and, since 2026-11-26, Los Redondos with Racing's seven products, whose orders
                // used to skip the ramp and only took its speed): the same product rotation as the smaller menu below, but each fan's
                // quantities and its extra products come from the tier of its spawn index (its speed is applied in CreateFan).
                int index = spawnedFans;
                TowerDefenseDifficultyTier tier = config.DifficultyTierForFan(index);
                int[] pool = availableProducts;
                int product = index < 6 ? pool[index % 2 == 0 ? 0 : pool.Length - 1]
                    : index < 18 ? pool[(index - 6) % pool.Length] : pool[random.Next(pool.Length)];
                if (tier.SecondProductPercent > 0 && pool.Length > 1 && random.Next(100) < tier.SecondProductPercent)
                {
                    int second = product;
                    while (second == product) second = pool[random.Next(pool.Length)];
                    var products = new List<int> { product, second };
                    // Up to four different products (Los Redondos, 2026-11-26): each extra one is rolled only once the previous one was added.
                    if (tier.ThirdProductPercent > 0 && pool.Length > 2 && random.Next(100) < tier.ThirdProductPercent)
                    {
                        products.Add(RollDistinctProduct(pool, products));
                        if (tier.FourthProductPercent > 0 && pool.Length > 3 && random.Next(100) < tier.FourthProductPercent)
                            products.Add(RollDistinctProduct(pool, products));
                    }
                    var quantities = new int[products.Count];
                    for (int i = 0; i < quantities.Length; i++) quantities[i] = RollTierQuantity(tier);
                    CreateFan(products.ToArray(), quantities);
                }
                else CreateFan(new[] { product }, new[] { RollTierQuantity(tier) });
                nextFanSpawnAt = elapsed + config.FanSpawnIntervalSeconds;
                return;
            }
            if (config.FreeConstruction && availableProducts.Length < 7)
            {
                // A smaller menu (Camioneros: sandwich and Coca): single-unit orders drawn only from what the level sells.
                int index = spawnedFans;
                int[] pool = availableProducts;
                int product = index < 6 ? pool[index % 2 == 0 ? 0 : pool.Length - 1]
                    : index < 18 ? pool[(index - 6) % pool.Length] : pool[random.Next(pool.Length)];
                if (index >= 18 && pool.Length > 1 && random.Next(4) == 0)
                {
                    int second = product;
                    while (second == product) second = pool[random.Next(pool.Length)];
                    CreateFan(new[] { product, second }, new[] { 1, 1 });
                }
                else CreateFan(new[] { product }, new[] { 1 });
                nextFanSpawnAt = elapsed + config.FanSpawnIntervalSeconds;
                return;
            }
            if (config.FreeConstruction)
            {
                int index = spawnedFans;
                int product;
                if (index < 6) product = index % 2 == 0 ? 0 : 4;
                else if (index < 18)
                {
                    int[] early = { 0, 1, 4, 6 };
                    product = early[(index - 6) % early.Length];
                }
                else product = random.Next(7);
                if (index >= 18 && random.Next(4) == 0)
                {
                    int second = product;
                    while (second == product) second = random.Next(7);
                    CreateFan(new[] { product, second }, new[] { 1, 1 });
                }
                else CreateFan(new[] { product }, new[] { 1 });
                nextFanSpawnAt = elapsed + config.FanSpawnIntervalSeconds;
                return;
            }
            if (random.Next(2) == 0)
                CreateFan(new[] { 0, 4 }, new[] { random.Next(1, 5), random.Next(1, 5) });
            else
                CreateFan(new[] { random.Next(2) == 0 ? 0 : 4 }, new[] { random.Next(1, 5) });
        }

        private int RollDistinctProduct(int[] pool, List<int> taken)
        {
            int candidate = taken[0];
            while (taken.Contains(candidate)) candidate = pool[random.Next(pool.Length)];
            return candidate;
        }

        private int RollTierQuantity(TowerDefenseDifficultyTier tier)
        {
            // One roll over 5, 4, 3 and 2 units in that order (4 and 5 since 2026-11-26, Camioneros); the rest ask for 1.
            int roll = random.Next(100), threshold = tier.Quantity5Percent;
            if (roll < threshold) return 5;
            if (roll < (threshold += tier.Quantity4Percent)) return 4;
            if (roll < (threshold += tier.Quantity3Percent)) return 3;
            return roll < threshold + tier.Quantity2Percent ? 2 : 1;
        }

        private bool CreateFan(int[] products, int[] quantities)
        {
            if (stopSpawning || spawnedFans >= TotalFanCount || fans.Count >= TotalFanCount ||
                routeGeometry == null || routeGeometry.Centerline.Length < 2) return false;
            int id = nextFanId++;
            int appearance = UsesCamionerosProfiles ? CamionerosFanProfile.AppearanceForSpawn(spawnedFans) : id;
            CamionerosFanProfile profile = UsesCamionerosProfiles ? CamionerosFanProfile.ForAppearance(appearance) : null;
            float rampSpeed = config.HasDifficultyRamp ? config.DifficultyTierForFan(spawnedFans).SpeedMultiplier : 1f;
            var follower = new TowerDefenseFanRouteFollower(id, fanRouteWaypoints,
                FanSpeedForProfile(profile, worldUnitsPerLogicalPixel) * rampSpeed);
            fans.Add(new TowerDefenseFanState(id, follower, products, quantities)
                { AppearanceId = appearance, Profile = profile, SpeedMultiplier = rampSpeed });
            spawnedFans++;
            if (spawnedFans >= TotalFanCount) stopSpawning = true;
            return true;
        }

        private void AdvanceFans(float deltaTime)
        {
            if (UsesCamionerosProfiles) { AdvanceCamionerosFans(deltaTime); return; }
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
            servicedWorkers.Clear();
            if (config.FreeConstruction)
            {
                DeliverFreeConstructionOrders();
                return;
            }
            // Arbitrate shared ready stock by fan urgency BEFORE worker hire order.
            // Otherwise an upper-area cook can consume the last unit for a rear fan
            // while a lower-area cook eligible for the exit-near fan receives nothing.
            for (int f = 0; f < serviceOrder.Count; f++)
            {
                TowerDefenseFanState fan = serviceOrder[f];
                if (fan.OrderComplete) continue;
                for (int w = 0; w < workers.Count; w++)
                {
                    if (fan.OrderComplete) break;
                    TowerDefenseWorkerState worker = workers[w];
                    if (worker.IsMoving || worker.DeliveryCooldownSeconds > 0f || servicedWorkers.Contains(worker.Id) ||
                        !IsAdjacentToRoleArea(fan, worker.Role, worker.ServiceZone)) continue;
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
                            worker.DeliveryCooldownSeconds = DeliveryIntervalSeconds;
                            worker.ServeAnimationSeconds = DeliveryCueDurationSeconds;
                            worker.LastServiceTarget = fan.RouteFollower.Position;
                            Vector2 deliverySource = config.FreeConstruction ? worker.Position : StationPosition(worker.Role, worker.ServiceZone);
                            if (!config.FreeConstruction && config.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId)
                                deliverySource = StationVisualPosition(worker.Role, worker.ServiceZone) -
                                    Vector2.up * StationRenderYOffsetLogical * worldUnitsPerLogicalPixel;
                            DeliveryCompleted?.Invoke(new TowerDefenseDelivery(unit.Id, order.Product, worker.Id, fan.Id,
                                worker.ServiceZone, deliverySource,
                                fan.RouteFollower.Position, elapsed));
                        }
                        servicedWorkers.Add(worker.Id); // Still one physical unit per worker per pass.
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

        // Kept as the original two-argument reflection/test hook for the shared-zone modes.
        private bool IsAdjacentToArea(TowerDefenseFanState fan, int zone) =>
            IsAdjacentToRoleArea(fan, StreetWorkerRole.Parrillero, zone);

        private bool IsAdjacentToRoleArea(TowerDefenseFanState fan, StreetWorkerRole role, int zone)
        {
            if (config.FreeConstruction) return zone == 0 && !fan.RouteFollower.HasExited && FanIsServiceable(fan);
            if (zone < 0 || zone >= serviceZones.Length || fan.RouteFollower.HasExited) return false;
            // Los Redondos has four right-side sectors; sector k serves runs k and k+1 and
            // their connector, exactly as a Camioneros area serves its two bordering runs.
            int areaIndex = IsLosRedondos ? LosRedondosLayout.SectorIndex(role, zone) : zone;
            // Each shared area borders its upper/lower horizontal rows and their
            // own connecting turn. The next connector and third row are not adjacent.
            int stride = route.TurnSegments + 1;
            int start = areaIndex * stride, end = (areaIndex + 1) * stride + 1;
            Vector2[] points = routeGeometry.Centerline;
            if (end >= points.Length) return false;
            float from = 0f, to = 0f;
            for (int i = 1; i <= end; i++)
            {
                float length = Vector2.Distance(points[i - 1], points[i]);
                to += length;
                if (i <= start) from += length;
            }
            float progress = Mathf.Max(0f, fan.RouteFollower.Progress - route.FanEntryExtensionWorldUnits);
            return progress >= from && progress <= to;
        }

        internal static Vector2[] BuildFanRouteWaypoints(TowerDefenseRouteGeometryResult geometry, float entryExtension)
        {
            Vector2[] centerline = geometry == null ? null : geometry.Centerline;
            if (centerline == null || centerline.Length < 2) return Array.Empty<Vector2>();
            if (!IsFinite(entryExtension) || entryExtension <= 0f) return centerline;

            var expanded = new Vector2[centerline.Length + 1];
            expanded[0] = centerline[0] - Vector2.right * entryExtension;
            Array.Copy(centerline, 0, expanded, 1, centerline.Length);
            return expanded;
        }

        private void AdvanceWorkers(float deltaTime)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                worker.AnimationTime += deltaTime;
                worker.ServeAnimationSeconds = Mathf.Max(0f, worker.ServeAnimationSeconds - deltaTime);
                worker.DeliveryCooldownSeconds = Mathf.Max(0f, worker.DeliveryCooldownSeconds - deltaTime);
                worker.ServiceDwellSeconds = Mathf.Max(0f, worker.ServiceDwellSeconds - deltaTime);
                if (!worker.IsMoving)
                {
                    if (config.FreeConstruction && worker.ServiceStage > 0) AdvanceFreeConstructionServiceStage(worker);
                    else if (config.FreeConstruction && worker.ServiceStage == 0 && worker.ServeAnimationSeconds <= 0f) QueueWorkerToPost(worker);
                    continue;
                }
                float movement = (config.FreeConstruction ? FreeWorkerSpeed(worker.Role) : Mathf.Max(0f, sourceBalance.workerSpeed) * worldUnitsPerLogicalPixel) * deltaTime;
                while (movement > 0f && worker.TransferIndex < worker.TransferRoute.Length)
                {
                    Vector2 target = worker.TransferRoute[worker.TransferIndex];
                    float distance = Vector2.Distance(worker.Position, target);
                    if (distance <= movement || distance <= 0.0001f)
                    {
                        if (config.FreeConstruction && !FreeSegmentClear(worker.Position, target, worker.Role, worker, Rect.zero, false)) { movement = 0f; break; }
                        worker.Position = target;
                        worker.TravelDistance += distance;
                        movement -= distance;
                        worker.TransferIndex++;
                    }
                    else
                    {
                        Vector2 direction = (target - worker.Position).normalized;
                        Vector2 nextPosition = worker.Position + direction * movement;
                        if (config.FreeConstruction && !FreeSegmentClear(worker.Position, nextPosition, worker.Role, worker, Rect.zero, false)) { movement = 0f; break; }
                        worker.Position = nextPosition;
                        worker.TravelDistance += movement;
                        movement = 0f;
                    }
                }
                if (worker.TransferIndex >= worker.TransferRoute.Length)
                {
                    worker.ServiceZone = worker.TargetZone;
                    worker.IsMoving = false;
                    worker.ReturningToPost = false;
                    worker.TransferRoute = Array.Empty<Vector2>();
                    if (config.FreeConstruction && worker.ServiceStage > 0) AdvanceFreeConstructionServiceStage(worker);
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

        private Vector2 WorkAreaHorizontalBounds(int zone)
        {
            int safeZone = Mathf.Clamp(zone, 0, serviceZones.Length - 1);
            float pixel = Mathf.Max(.0001f, worldUnitsPerLogicalPixel);
            // The serialized bounds already inset the viewport by 16 logical pixels.
            // Restore 12 of those pixels to use the real interior up to a small 4px edge margin.
            float outerLeft = route.PlayableBounds.xMin - 12f * pixel;
            float outerRight = route.PlayableBounds.xMax + 12f * pixel;
            int rowStride = Mathf.Max(1, route.TurnSegments + 1);
            int turnIndex = safeZone * rowStride + 1;
            if (routeGeometry == null || routeGeometry.Centerline == null || turnIndex >= routeGeometry.Centerline.Length)
                return new Vector2(outerLeft, outerRight);

            Vector2 turn = routeGeometry.Centerline[turnIndex];
            float innerWall = turn.x + (safeZone % 2 == 0 ? -.5f : .5f) * route.CorridorWidth;
            return safeZone % 2 == 0
                ? new Vector2(outerLeft, innerWall - WorkerFenceClearanceLogical * pixel)
                : new Vector2(innerWall + WorkerFenceClearanceLogical * pixel, outerRight);
        }

        private float ClearHalfHeightLogical()
        {
            float horizontalCorridor = route.HorizontalCorridorWidth > 0f
                ? route.HorizontalCorridorWidth : route.CorridorWidth;
            return Mathf.Max(0f, route.RowSpacing - horizontalCorridor) /
                (2f * Mathf.Max(.0001f, worldUnitsPerLogicalPixel));
        }

        private float WorkerRowAnchorLogical(StreetWorkerRole role)
        {
            float halfHeight = ClearHalfHeightLogical();
            float visualMinY = -WorkerVisualHeightLogical * WorkerVisualPivotY;
            float visualMaxY = WorkerVisualHeightLogical * (1f - WorkerVisualPivotY);
            float lowerMin = -halfHeight + WorkerFenceClearanceLogical - visualMinY;
            float lowerMax = halfHeight - WorkerFenceClearanceLogical - WorkerRoleRowSeparationLogical - visualMaxY;
            float lowerAnchor = (lowerMin + lowerMax) * .5f;
            return role == StreetWorkerRole.Parrillero ? lowerAnchor + WorkerRoleRowSeparationLogical : lowerAnchor;
        }

        private float CamionerosWorkerRowAnchorLogical()
        {
            float halfHeight = ClearHalfHeightLogical();
            float visualMinY = -WorkerVisualHeightLogical * WorkerVisualPivotY;
            float visualMaxY = WorkerVisualHeightLogical * (1f - WorkerVisualPivotY);
            float minimum = -halfHeight + WorkerFenceClearanceLogical - visualMinY;
            float maximum = halfHeight - WorkerFenceClearanceLogical - visualMaxY;
            if (maximum < minimum) return 0f;
            return Mathf.Clamp(CamionerosWorkerRowOffsetLogical, minimum, maximum);
        }

        private bool IsCamionerosCrew(StreetWorkerRole role, int zone) =>
            config.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId &&
            zone >= 0 && zone < serviceZones.Length &&
            (role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.Cocacolero);

        private float CamionerosWorkerGroupCenterX(StreetWorkerRole role, int zone, float fallback)
        {
            if (!IsCamionerosCrew(role, zone)) return fallback;
            Vector2 bounds = CamionerosWorkerGroupCenterBoundsX(role, zone);
            if (bounds.y < bounds.x) return fallback;
            return Mathf.Clamp(StationVisualPosition(role, zone).x, bounds.x, bounds.y);
        }

        private Vector2 CamionerosWorkerGroupCenterBoundsX(StreetWorkerRole role, int zone)
        {
            Vector2 bounds = WorkAreaHorizontalBounds(zone);
            float visualWidth = role == StreetWorkerRole.Parrillero
                ? ParrilleroVisualWidthLogical : CocacoleroVisualWidthLogical;
            float spacing = role == StreetWorkerRole.Parrillero
                ? CamionerosParrilleroSpacingLogical : CamionerosCocacoleroSpacingLogical;
            float halfGroup = spacing + visualWidth * .5f;
            float safeHalf = WorkerFenceClearanceLogical + halfGroup;
            float minimum = bounds.x + safeHalf * worldUnitsPerLogicalPixel;
            float maximum = bounds.y - safeHalf * worldUnitsPerLogicalPixel;
            return new Vector2(minimum, maximum);
        }

        private void BuildWorkers()
        {
            workers.Clear();
            for (int roleIndex = 0; roleIndex < workerCounts.Length; roleIndex++)
                for (int i = 0; i < workerCounts[roleIndex]; i++) AddWorker((StreetWorkerRole)roleIndex, ChooseInitialZone((StreetWorkerRole)roleIndex));
            if (config.FreeConstruction && IsBocaKitchenLevel)
                for (int roleIndex = 0; roleIndex < workerCounts.Length; roleIndex++) AssignRolePosts((StreetWorkerRole)roleIndex, true);
        }

        private int ChooseInitialZone(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.Mozo ? 0 : 1;

        private void AddWorker(StreetWorkerRole role, int zone)
        {
            int index = CountPriorWorkers(role, zone);
            int crewCount = index + 1;
            workers.Add(new TowerDefenseWorkerState(nextWorkerId++, role, zone,
                ZoneWorkerPosition(role, zone, index, crewCount)));
            // 2026-11-24: the C-shaped free-construction Camioneros waits at campaign spots. Teleporting the whole role to zone slots on every hire moved
            // a worker that was mid-job without clearing its route, which then crossed the table and stayed stuck with a fan reserved forever.
            if (IsCamionerosCrew(role, zone) && !IsPassiveKitchenLevel) RepositionCamionerosCrew(role, zone);
            else if (IsLosRedondos)
                RepositionLosRedondosCrew(role, zone);
        }

        private void RepositionLosRedondosCrew(StreetWorkerRole role, int zone)
        {
            int crewCount = WorkersInZone(role, zone);
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != role || worker.ServiceZone != zone) continue;
                worker.Position = ZoneWorkerPosition(role, zone,
                    CountPriorWorkers(role, zone, worker), crewCount);
            }
        }

        private void RepositionCamionerosCrew(StreetWorkerRole role, int zone)
        {
            int crewCount = WorkersInZone(role, zone);
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != role || worker.ServiceZone != zone) continue;
                worker.Position = ZoneWorkerPosition(role, zone,
                    CountPriorWorkers(role, zone, worker), crewCount);
            }
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

        // Traditional (passive) kitchens have no Mozo: the cook of each specialty serves its own products.
        private StreetWorkerRole GetDeliveryRole(int product) =>
            product >= 0 && product <= 3
                ? (IsPassiveKitchenLevel ? StreetSpecialties.GetRequiredWorkerRole(product)
                    : config.FreeConstruction ? StreetWorkerRole.Mozo : StreetWorkerRole.Parrillero)
                : StreetSpecialties.GetRequiredWorkerRole(product);

        private int CountPriorWorkers(StreetWorkerRole role)
        {
            int count = 0;
            for (int i = 0; i < workers.Count; i++) if (workers[i].Role == role) count++;
            return count;
        }

        private Vector2 ZoneWorkerPosition(StreetWorkerRole role, int zone, int roleIndex, int crewCount)
        {
            if (config.FreeConstruction)
            {
                if (placedStations.Count > 0)
                {
                    Vector2 stationAccess = IsBocaKitchenLevel ? RolePost(role, roleIndex, crewCount) : RoleStationAccess(role);
                    if (IsFinite(stationAccess.x) && IsFinite(stationAccess.y) &&
                        (IsBocaKitchenLevel || FreePositionClear(stationAccess, role, null, Rect.zero))) return stationAccess;
                }
                int startingCrewSize = 0;
                for (int i = 0; i < workerCounts.Length; i++) startingCrewSize += workerCounts[i];
                // Keep configured starters in one separated corridor. When the level has
                // opening furniture, this places them just behind it; otherwise preserve the
                // lower staging corridor used by free-construction fixtures.
                float spacing = 102f * worldUnitsPerLogicalPixel;
                float offset = (workers.Count - (startingCrewSize - 1) * .5f) * spacing;
                float startRowLogical = placedStations.Count > 0 ? 190f : 110f;
                float preferredY = WorkBounds.min.y + startRowLogical * worldUnitsPerLogicalPixel;
                float maximumFeetY = WorkBounds.yMax - ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) * worldUnitsPerLogicalPixel;
                float workerY = Mathf.Clamp(preferredY, WorkBounds.min.y, maximumFeetY);
                return new Vector2(WorkBounds.center.x + offset, workerY);
            }
            if (IsCamionerosCrew(role, zone))
            {
                float spacing = role == StreetWorkerRole.Parrillero
                    ? CamionerosParrilleroSpacingLogical : CamionerosCocacoleroSpacingLogical;
                float slotOffset = CamionerosWorkerSlotOffsetLogical(roleIndex, crewCount, spacing);
                float centerX = CamionerosWorkerGroupCenterX(role, zone, WorkZone(zone).x);
                return new Vector2(centerX + slotOffset * worldUnitsPerLogicalPixel,
                    WorkZone(zone).y + CamionerosWorkerRowAnchorLogical() * worldUnitsPerLogicalPixel);
            }
            if (IsLosRedondos)
            {
                // Full-size crew (Camioneros widths/spacing) centered behind its station,
                // compressed only as needed to stay inside the 34.5% sector.
                Rect sector = RightBayBounds(role, zone);
                float sectorWidth = sector.width / Mathf.Max(.0001f, worldUnitsPerLogicalPixel) - LosRedondosSectorSideInsetLogical * 2f;
                float workerWidth = role == StreetWorkerRole.Parrillero
                    ? WorkerVisualHeightLogical * 266f / 303f : CamionerosCocacoleroStandingWidthLogical;
                float preferred = role == StreetWorkerRole.Parrillero
                    ? CamionerosParrilleroSpacingLogical : CamionerosCocacoleroSpacingLogical;
                float spacing = crewCount > 1
                    ? Mathf.Min(preferred, Mathf.Max(0f, (sectorWidth - workerWidth) / (crewCount - 1))) : 0f;
                float centeredIndex = roleIndex - (crewCount - 1) * .5f;
                float stationBottom = sector.yMin + LosRedondosStationFloorInsetLogical * worldUnitsPerLogicalPixel;
                float feetY = stationBottom + LosRedondosStationHeightLogical(role) * LosRedondosCrewFeetStationFraction *
                    worldUnitsPerLogicalPixel;
                return new Vector2(sector.center.x + centeredIndex * spacing * worldUnitsPerLogicalPixel, feetY);
            }
            if (UsesSplitWorkerRows)
            {
                Vector2 table = StationPosition(StreetWorkerRole.Parrillero, zone);
                Vector2 barrel = StationPosition(StreetWorkerRole.Cocacolero, zone);
                float visualWidth = role == StreetWorkerRole.Parrillero
                    ? ParrilleroVisualWidthLogical : CocacoleroVisualWidthLogical;
                float workerSpacing = role == StreetWorkerRole.Parrillero
                    ? ParallelWorkerSpacingLogical : ParallelCocacoleroSpacingLogical;
                float groupHalfWidth = workerSpacing + visualWidth * .5f;
                float rowCenterX = role == StreetWorkerRole.Parrillero
                    ? table.x + (StreetKitchenLayout.TableWidth * .5f + WorkerStationClearanceLogical + groupHalfWidth) * worldUnitsPerLogicalPixel
                    : barrel.x - (StreetKitchenLayout.BarrelWidth * .5f + WorkerStationClearanceLogical + groupHalfWidth) * worldUnitsPerLogicalPixel;
                float lateralLogical = (roleIndex - 1) * workerSpacing;
                float rowAnchor = WorkerRowAnchorLogical(role);
                return new Vector2(rowCenterX + lateralLogical * worldUnitsPerLogicalPixel,
                    WorkZone(zone).y + rowAnchor * worldUnitsPerLogicalPixel);
            }

            Vector2 basePoint = StationPosition(role, Mathf.Clamp(zone, 0, serviceZones.Length - 1));
            // Full-size handoff poses are up to 77 logical pixels wide; keep same-role
            // workers at least 84 pixels apart so simultaneous servings remain visible.
            float lateral = (roleIndex == 0 ? 0f : roleIndex == 1 ? -ParallelWorkerSpacingLogical : ParallelWorkerSpacingLogical)
                * worldUnitsPerLogicalPixel;
            float depth = -((roleIndex / 3) * worldUnitsPerLogicalPixel * 22f);
            return basePoint + new Vector2(lateral, depth);
        }

        private static float CamionerosWorkerSlotOffsetLogical(int roleIndex, int crewCount, float spacing)
        {
            if (roleIndex <= 0 || crewCount <= 1) return 0f;
            // The first hire stays centered on the station; later hires take the left,
            // then right slot as the group fills out.
            return roleIndex == 1 ? -spacing : spacing;
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
                RouteKind = source.RouteKind,
                SegmentLengths = source.SegmentLengths == null ? Array.Empty<float>() : (float[])source.SegmentLengths.Clone(),
                CorridorWidth = source.CorridorWidth,
                HorizontalCorridorWidth = source.HorizontalCorridorWidth,
                RowSpacing = source.RowSpacing,
                TurnRadius = source.TurnRadius,
                PlayableBounds = source.PlayableBounds,
                WorkBounds = source.WorkBounds,
                TurnSegments = source.TurnSegments,
                FanEntryExtensionWorldUnits = source.FanEntryExtensionWorldUnits,
                BottomLaneExtra = source.BottomLaneExtra,
                KitchenVerticalScale = source.KitchenVerticalScale
            };
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
