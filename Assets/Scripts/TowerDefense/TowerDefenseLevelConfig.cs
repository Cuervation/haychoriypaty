using System;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Serpentine = the original two-rail serpentine; HorizontalSingleFence = one straight fence with the work area below;
    /// CShapedSingleFence = one fence shaped like a "C" open to the left (top run to the right, a right connector down and a
    /// bottom run back to the left) with the work area in the pocket the fence encloses and the fans walking outside it.
    /// </summary>
    public enum TowerDefenseRouteKind { Serpentine = 0, HorizontalSingleFence = 1, CShapedSingleFence = 2 }

    public enum TowerDefenseStationKind
    {
        NormalGrill = 0, PremiumGrill = 1, CocaBarrel = 2, FernetTable = 3, BeerBarrel = 4,
        NormalTable = 5, PremiumTable = 6
    }

    [Serializable]
    public struct TowerDefenseStartingStation
    {
        public TowerDefenseStationKind Kind;
        [Tooltip("Position in logical pixels relative to the lower-left corner of WorkBounds.")]
        public Vector2 PositionFromWorkBoundsMin;
        [Tooltip("When true, use the authored worker access point (shared by stations used by the same role when appropriate).")]
        public bool HasAccessPosition;
        [Tooltip("Worker access point in logical pixels relative to the lower-left corner of WorkBounds.")]
        public Vector2 AccessPositionFromWorkBoundsMin;
    }

    /// <summary>
    /// One step of a level's difficulty ramp. It applies to every fan whose spawn index is at least <see cref="FromFan"/> and
    /// below the next tier's FromFan. Orders roll their quantity per product line and their chance of a second product here.
    /// </summary>
    [Serializable]
    public struct TowerDefenseDifficultyTier
    {
        [Tooltip("Index (0-based) of the first fan of the wave that uses this tier.")]
        [Min(0)] public int FromFan;
        [Tooltip("Multiplier on the level's fan speed for fans born in this tier (already-born fans keep theirs).")]
        [Min(0.1f)] public float SpeedMultiplier;
        [Tooltip("Percent chance that a product line asks for 2 units.")]
        [Range(0, 100)] public int Quantity2Percent;
        [Tooltip("Percent chance that a product line asks for 3 units (rolled after 5 and 4, before 2; the rest ask for 1).")]
        [Range(0, 100)] public int Quantity3Percent;
        [Tooltip("Percent chance that a product line asks for 4 units (2026-11-26; 0 in levels that never ask for more than 3).")]
        [Range(0, 100)] public int Quantity4Percent;
        [Tooltip("Percent chance that a product line asks for 5 units (rolled first; the five quantity chances add up to at most 100).")]
        [Range(0, 100)] public int Quantity5Percent;
        [Tooltip("Percent chance that the order has a second product.")]
        [Range(0, 100)] public int SecondProductPercent;
        [Tooltip("Percent chance, once it has two, that the order adds a third different product (2026-11-26; 0 keeps orders at two).")]
        [Range(0, 100)] public int ThirdProductPercent;
        [Tooltip("Percent chance, once it has three, that the order adds a fourth different product.")]
        [Range(0, 100)] public int FourthProductPercent;
    }

    /// <summary>Serializable configuration for the Tower Defense runtime attached to a campaign level.</summary>
    [CreateAssetMenu(menuName = "Hay Chori y Paty/Tower Defense/Level Config", fileName = "TD-Camioneros-PlazaDeMayo")]
    public sealed class TowerDefenseLevelConfig : ScriptableObject
    {
        public const string CamionerosLevelId = "tower-defense.camioneros.plaza-de-mayo.level-1";
        public const string LosRedondosLevelId = "tower-defense.los-redondos.tandil.level-1";
        public const string LaRengaLevelId = "tower-defense.la-renga.autodromo-buenos-aires.level-1";
        public const string PeregrinacionLujanLevelId = "tower-defense.peregrinacion-lujan.moreno.level-1";
        public const int CamionerosCampaignLevelId = 9;
        public const int LosRedondosCampaignLevelId = 10;
        public const int LaRengaCampaignLevelId = 13;
        public const int PeregrinacionLujanCampaignLevelId = 14;
        public const int CommonEconomicBalanceLevelId = 9;
        public const int FansToServe = 60;
        public const int CamionerosFansToServe = 120;
        public const float DefaultFanSpeed = 0.1575f;
        public const float CamionerosFanSpeed = 0.20475f;
        public const float DefaultMinimumEntrySpacingLogical = 58f;
        public const float CamionerosMinimumEntrySpacingLogical = 96f;

        public static string ResourcesPathForCampaignLevel(int campaignLevelId)
        {
            switch (campaignLevelId)
            {
                case CamionerosCampaignLevelId: return "TowerDefense/Camioneros-PlazaDeMayo";
                case LosRedondosCampaignLevelId: return "TowerDefense/LosRedondos-Tandil";
                case LaRengaCampaignLevelId: return "TowerDefense/LaRenga-AutodromoBuenosAires";
                case PeregrinacionLujanCampaignLevelId: return "TowerDefense/Peregrinacion-LujanMoreno";
                default: return null;
            }
        }

        public static int TotalFanCountForCampaignLevel(int campaignLevelId)
        {
            string path = ResourcesPathForCampaignLevel(campaignLevelId);
            if (string.IsNullOrEmpty(path)) return FansToServe;
            TowerDefenseLevelConfig levelConfig = Resources.Load<TowerDefenseLevelConfig>(path);
            if (levelConfig != null) return levelConfig.TotalFanCount;
            return campaignLevelId == CamionerosCampaignLevelId ? CamionerosFansToServe : FansToServe;
        }

        [SerializeField] private string modeId = "tower-defense";
        [SerializeField] private string levelId = CamionerosLevelId;
        [SerializeField] private string displayName = "Camioneros / Plaza de Mayo";
        [SerializeField, Min(0)] private int sourceCampaignLevelId = 9;
        [Tooltip("Optional campaign balance row used only for Tower Defense prices. Campaign identity/progression still uses SourceCampaignLevelId.")]
        [SerializeField, Min(0)] private int economicBalanceCampaignLevelId = CommonEconomicBalanceLevelId;
        [SerializeField] private bool useCampaignEconomyPricing;
        [Tooltip("Optional: price the workers hired in this level with the hire prices and growth of a regular campaign level (e.g. 3 = Ferro). -1 = off. Speed upgrades keep their own price.")]
        [SerializeField] private int hirePricingCampaignLevelId = -1;
        [Tooltip("Per-role first price that replaces the campaign one (0 = keep it); it grows with each purchase like the campaign prices.")]
        [SerializeField] private int[] hireBaseCostOverridesByRole = new int[0];
        [SerializeField] private string backgroundResource = "street-background-camioneros-v1";
        [SerializeField] private TowerDefenseRouteKind routeKind = TowerDefenseRouteKind.Serpentine;
        [SerializeField] private bool freeConstruction;
        [SerializeField] private bool equipmentPlacementAllowed = true;
        [SerializeField] private TowerDefenseRouteGeometryConfig route = new TowerDefenseRouteGeometryConfig
        {
            RowCount = 3,
            SegmentLengths = new[] { 3.8f, 3.8f, 3.8f },
            CorridorWidth = 0.9f,
            RowSpacing = 1.8f,
            TurnRadius = 0.68f,
            PlayableBounds = new Rect(-3.3f, -4.7f, 6.6f, 9.4f),
            TurnSegments = 16
        };
        [SerializeField] private GameObject straightFencePrefab;
        [SerializeField] private GameObject turnFencePrefab;
        [SerializeField] private GameObject terminalFencePrefab;
        [SerializeField] private GameObject directionSignPrefab;
        [SerializeField] private Sprite directionArrowSprite;
        [SerializeField] private Sprite verticalFencePanelSprite;
        [SerializeField] private int[] availableProducts = { 0, 4 };
        [SerializeField, Min(1)] private int totalFanCount = CamionerosFansToServe;
        [Tooltip("Multiplier applied to Tower Defense fan movement; other campaign levels are unaffected.")]
        [SerializeField, Min(0.005f)] private float fanSpeed = CamionerosFanSpeed;
        [Tooltip("Minimum route progress, in logical pixels, before another fan may enter. This keeps arrivals visibly staggered.")]
        [SerializeField, Min(1f)] private float minimumEntrySpacingLogical = DefaultMinimumEntrySpacingLogical;
        [SerializeField, Min(0.01f)] private float serviceRange = 0.8f;
        [SerializeField] private Vector2 firstWorkZoneOffset;
        [SerializeField] private Vector2 secondWorkZoneOffset;
        [SerializeField, Min(0)] private int startingCoins = 500;
        [SerializeField, Min(0.1f)] private float fanSpawnIntervalSeconds = 1f;
        [SerializeField, Min(0)] private int stationCost = 80;
        [Tooltip("Most workers of each role the player may hire in a free-construction level. 0 = no limit (only coins limit it).")]
        [SerializeField, Min(0)] private int maximumWorkersPerRole = 3;
        [Tooltip("Optional difficulty ramp by spawn index (ascending FromFan). Empty = the level's fixed difficulty.")]
        [SerializeField] private TowerDefenseDifficultyTier[] difficultyTiers = Array.Empty<TowerDefenseDifficultyTier>();
        [SerializeField] private int[] freeStationCounts = { 1, 1, 1, 1, 1, 1, 1 };
        [Tooltip("Optional physical stations placed at level start. Positions are logical pixels from the lower-left of WorkBounds.")]
        [SerializeField] private TowerDefenseStartingStation[] startingStations = Array.Empty<TowerDefenseStartingStation>();
        [SerializeField] private int[] hireCostsByRole = { 60, 60, 60, 60, 60 };
        [Tooltip("Explicit role-local starting crew for free-construction levels. This is level config, never imported from a saved campaign roster.")]
        [SerializeField] private int[] startingWorkersByRole = new int[5];
        [Tooltip("Draws the Defense world mirrored left-right on screen (a C that opens to the right). The game logic and geometry are unchanged.")]
        [SerializeField] private bool mirrorHorizontally;
        [Tooltip("Racing's active kitchen (2026-11-24): the Parrilleros cook on the grill and the Mozos hand the sandwiches over, with all seven products, like the previous campaign level.")]
        [SerializeField] private bool activeKitchen;

        public string ModeId => string.IsNullOrWhiteSpace(modeId) ? "tower-defense" : modeId;
        public string LevelId => string.IsNullOrWhiteSpace(levelId) ? CamionerosLevelId : levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Camioneros / Plaza de Mayo" : displayName;
        /// <summary>Visual/campaign identity whose stable ID owns progression and persistence.</summary>
        public int SourceCampaignLevelId => sourceCampaignLevelId;
        public int EconomicBalanceCampaignLevelId => Mathf.Clamp(economicBalanceCampaignLevelId, 0, StreetSimulation.LevelNames.Length - 1);
        public bool UseCampaignEconomyPricing => useCampaignEconomyPricing;
        /// <summary>Regular campaign level whose worker prices this level copies, or -1.</summary>
        /// <summary>Own first price for this role (Los Redondos' Fernetero: 120, 2026-11-26), or 0 to keep the campaign price.</summary>
        public int HireBaseCostOverride(StreetWorkerRole role)
        {
            int index = (int)role;
            return hireBaseCostOverridesByRole != null && index >= 0 && index < hireBaseCostOverridesByRole.Length ? Mathf.Max(0, hireBaseCostOverridesByRole[index]) : 0;
        }
        public int HirePricingCampaignLevelId => hirePricingCampaignLevelId < 0 ? -1 : Mathf.Min(hirePricingCampaignLevelId, StreetSimulation.LevelNames.Length - 1);
        public string BackgroundResource => backgroundResource;
        public TowerDefenseRouteKind RouteKind => routeKind;
        public bool FreeConstruction => freeConstruction;
        public bool EquipmentPlacementAllowed => equipmentPlacementAllowed;
        public int StartingCoins => Mathf.Max(0, startingCoins);
        public float FanSpawnIntervalSeconds => Mathf.Max(0.1f, fanSpawnIntervalSeconds);
        public int StationCost => Mathf.Max(0, stationCost);
        public int MaximumWorkersPerRole => maximumWorkersPerRole <= 0 ? int.MaxValue : maximumWorkersPerRole;
        public bool HasDifficultyRamp => difficultyTiers != null && difficultyTiers.Length > 0;
        /// <summary>The tier that governs the fan with this 0-based spawn index (the last one whose FromFan is not above it).</summary>
        public TowerDefenseDifficultyTier DifficultyTierForFan(int spawnIndex)
        {
            if (!HasDifficultyRamp) return new TowerDefenseDifficultyTier { SpeedMultiplier = 1f };
            TowerDefenseDifficultyTier tier = difficultyTiers[0];
            for (int i = 1; i < difficultyTiers.Length; i++)
            {
                if (difficultyTiers[i].FromFan > spawnIndex) break;
                tier = difficultyTiers[i];
            }
            if (tier.SpeedMultiplier <= 0f) tier.SpeedMultiplier = 1f;
            return tier;
        }
        public int HireCostForRole(StreetWorkerRole role)
        {
            int index = (int)role;
            return hireCostsByRole != null && index >= 0 && index < hireCostsByRole.Length ? Mathf.Max(0, hireCostsByRole[index]) : 60;
        }
        public int FreeStationCount(TowerDefenseStationKind kind)
        {
            int index = (int)kind;
            return freeStationCounts != null && index >= 0 && index < freeStationCounts.Length ? Mathf.Max(0, freeStationCounts[index]) : 1;
        }
        public TowerDefenseStartingStation[] StartingStations => startingStations == null
            ? Array.Empty<TowerDefenseStartingStation>() : (TowerDefenseStartingStation[])startingStations.Clone();
        public TowerDefenseRouteGeometryConfig Route => route;
        public GameObject StraightFencePrefab => straightFencePrefab;
        public GameObject TurnFencePrefab => turnFencePrefab;
        public GameObject TerminalFencePrefab => terminalFencePrefab;
        public GameObject DirectionSignPrefab => directionSignPrefab;
        public Sprite DirectionArrowSprite => directionArrowSprite;
        public Sprite VerticalFencePanelSprite => verticalFencePanelSprite;
        public int[] AvailableProducts => availableProducts == null ? Array.Empty<int>() : (int[])availableProducts.Clone();
        public float FanSpeed => Mathf.Max(0f, fanSpeed);
        public int TotalFanCount => Mathf.Max(1, totalFanCount);
        public float MinimumEntrySpacingLogical => Mathf.Max(1f, minimumEntrySpacingLogical);
        public float ServiceRange => Mathf.Max(0.01f, serviceRange);
        public Vector2 FirstWorkZoneOffset => firstWorkZoneOffset;
        public Vector2 SecondWorkZoneOffset => secondWorkZoneOffset;
        /// <summary>The world is drawn mirrored left-right on screen (2026-11-24: Los Redondos plays La Renga's C inverted).</summary>
        public bool MirrorHorizontally => mirrorHorizontally;
        /// <summary>Racing's active kitchen: cooks cook on the grill and Mozos deliver (Boca's seven-station layout); not a passive Defense kitchen.</summary>
        public bool ActiveKitchen => activeKitchen;
        /// <summary>
        /// La Renga's dynamics: its C-shaped fence, kitchen (Ferro's with narrow grills), crew behavior and difficulty ramp.
        /// Los Redondos copies them on the C-shaped fence (2026-11-24, a pedido).
        /// </summary>
        public bool UsesLaRengaDynamics => sourceCampaignLevelId == LaRengaCampaignLevelId ||
            (sourceCampaignLevelId == LosRedondosCampaignLevelId && routeKind == TowerDefenseRouteKind.CShapedSingleFence);
        public int[] StartingWorkersByRole => startingWorkersByRole == null
            ? new int[5] : (int[])startingWorkersByRole.Clone();

        /// <summary>Used on a runtime clone to fit the same level to a responsive camera viewport.</summary>
        public void ConfigureRoute(TowerDefenseRouteGeometryConfig runtimeRoute)
        {
            if (runtimeRoute == null) throw new ArgumentNullException(nameof(runtimeRoute));
            route = new TowerDefenseRouteGeometryConfig
            {
                RowCount = runtimeRoute.RowCount,
                RouteKind = runtimeRoute.RouteKind,
                SegmentLengths = runtimeRoute.SegmentLengths == null ? Array.Empty<float>() : (float[])runtimeRoute.SegmentLengths.Clone(),
                CorridorWidth = runtimeRoute.CorridorWidth,
                HorizontalCorridorWidth = runtimeRoute.HorizontalCorridorWidth,
                RowSpacing = runtimeRoute.RowSpacing,
                TurnRadius = runtimeRoute.TurnRadius,
                PlayableBounds = runtimeRoute.PlayableBounds,
                WorkBounds = runtimeRoute.WorkBounds,
                TurnSegments = runtimeRoute.TurnSegments,
                FanEntryExtensionWorldUnits = runtimeRoute.FanEntryExtensionWorldUnits,
                BottomLaneExtra = runtimeRoute.BottomLaneExtra
            };
        }

        /// <summary>Finds a service point halfway between two adjacent rows, outside the fan corridor.</summary>
        public Vector2 WorkZonePosition(TowerDefenseRouteGeometryResult geometry, int betweenRows)
        {
            if (geometry == null || !geometry.IsValid || route == null || betweenRows < 0 || betweenRows >= route.RowCount - 1 ||
                geometry.Centerline == null || geometry.Centerline.Length < 2) return Vector2.zero;

            int rowStride = route.TurnSegments + 1;
            int firstIndex = betweenRows * rowStride;
            int secondIndex = (betweenRows + 1) * rowStride;
            if (firstIndex + 1 >= geometry.Centerline.Length || secondIndex + 1 >= geometry.Centerline.Length) return Vector2.zero;

            Vector2 firstMidpoint = (geometry.Centerline[firstIndex] + geometry.Centerline[firstIndex + 1]) * 0.5f;
            Vector2 secondMidpoint = (geometry.Centerline[secondIndex] + geometry.Centerline[secondIndex + 1]) * 0.5f;
            Vector2 configuredOffset = betweenRows == 0 ? firstWorkZoneOffset : secondWorkZoneOffset;
            return (firstMidpoint + secondMidpoint) * 0.5f + configuredOffset;
        }
    }
}
