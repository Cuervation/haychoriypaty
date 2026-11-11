using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum TowerDefenseRouteKind { Serpentine = 0, HorizontalSingleFence = 1 }

    public enum TowerDefenseStationKind
    {
        NormalGrill = 0, PremiumGrill = 1, CocaBarrel = 2, FernetTable = 3, BeerBarrel = 4,
        NormalTable = 5, PremiumTable = 6
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
        [SerializeField] private string backgroundResource = "street-background-camioneros-v1";
        [SerializeField] private TowerDefenseRouteKind routeKind = TowerDefenseRouteKind.Serpentine;
        [SerializeField] private bool freeConstruction;
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
        [SerializeField, Min(1)] private int maximumWorkersPerRole = 3;
        [SerializeField] private int[] freeStationCounts = { 1, 1, 1, 1, 1, 1, 1 };
        [SerializeField] private int[] hireCostsByRole = { 60, 60, 60, 60, 60 };
        [Tooltip("Defense starts without workers; retained for serialized layout compatibility.")]
        [SerializeField] private int[] startingWorkersByRole = new int[5];

        public string ModeId => string.IsNullOrWhiteSpace(modeId) ? "tower-defense" : modeId;
        public string LevelId => string.IsNullOrWhiteSpace(levelId) ? CamionerosLevelId : levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Camioneros / Plaza de Mayo" : displayName;
        /// <summary>Visual/campaign identity whose stable ID owns progression and persistence.</summary>
        public int SourceCampaignLevelId => sourceCampaignLevelId;
        public string BackgroundResource => backgroundResource;
        public TowerDefenseRouteKind RouteKind => routeKind;
        public bool FreeConstruction => freeConstruction;
        public int StartingCoins => Mathf.Max(0, startingCoins);
        public float FanSpawnIntervalSeconds => Mathf.Max(0.1f, fanSpawnIntervalSeconds);
        public int StationCost => Mathf.Max(0, stationCost);
        public int MaximumWorkersPerRole => Mathf.Max(1, maximumWorkersPerRole);
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
        public TowerDefenseRouteGeometryConfig Route => route;
        public GameObject StraightFencePrefab => straightFencePrefab;
        public GameObject TurnFencePrefab => turnFencePrefab;
        public GameObject TerminalFencePrefab => terminalFencePrefab;
        public GameObject DirectionSignPrefab => directionSignPrefab;
        public Sprite DirectionArrowSprite => directionArrowSprite;
        public int[] AvailableProducts => availableProducts == null ? Array.Empty<int>() : (int[])availableProducts.Clone();
        public float FanSpeed => Mathf.Max(0f, fanSpeed);
        public int TotalFanCount => Mathf.Max(1, totalFanCount);
        public float MinimumEntrySpacingLogical => Mathf.Max(1f, minimumEntrySpacingLogical);
        /// <summary>Legacy source-row compatibility metadata; runtime TD costs use the frozen profile.</summary>
        public int EconomicBalanceCampaignLevelId => CommonEconomicBalanceLevelId;
        public float ServiceRange => Mathf.Max(0.01f, serviceRange);
        public Vector2 FirstWorkZoneOffset => firstWorkZoneOffset;
        public Vector2 SecondWorkZoneOffset => secondWorkZoneOffset;
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
                FanEntryExtensionWorldUnits = runtimeRoute.FanEntryExtensionWorldUnits
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
