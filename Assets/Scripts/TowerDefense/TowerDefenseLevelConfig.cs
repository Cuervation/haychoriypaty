using System;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Serializable configuration for the Tower Defense runtime attached to a campaign level.</summary>
    [CreateAssetMenu(menuName = "Hay Chori y Paty/Tower Defense/Level Config", fileName = "TD-Camioneros-PlazaDeMayo")]
    public sealed class TowerDefenseLevelConfig : ScriptableObject
    {
        public const string CamionerosLevelId = "tower-defense.camioneros.plaza-de-mayo.level-1";
        public const int CamionerosFansToServe = 10;

        [SerializeField] private string modeId = "tower-defense";
        [SerializeField] private string levelId = CamionerosLevelId;
        [SerializeField] private string displayName = "Camioneros / Plaza de Mayo";
        [SerializeField, Min(0)] private int sourceCampaignLevelId = 9;
        [SerializeField] private string backgroundResource = "street-background-camioneros-v1";
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
        [Tooltip("Multiplier applied to Tower Defense fan movement; other campaign levels are unaffected.")]
        [SerializeField, Min(0.05f)] private float fanSpeed = 0.45f;
        [SerializeField, Min(0.01f)] private float serviceRange = 0.8f;
        [SerializeField] private Vector2 firstWorkZoneOffset;
        [SerializeField] private Vector2 secondWorkZoneOffset;
        [Tooltip("Defense starts without workers; retained for serialized layout compatibility.")]
        [SerializeField] private int[] startingWorkersByRole = new int[5];

        public string ModeId => string.IsNullOrWhiteSpace(modeId) ? "tower-defense" : modeId;
        public string LevelId => string.IsNullOrWhiteSpace(levelId) ? CamionerosLevelId : levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Camioneros / Plaza de Mayo" : displayName;
        /// <summary>Campaign level whose current balance drives this runtime and whose stable ID owns progression.</summary>
        public int SourceCampaignLevelId => sourceCampaignLevelId;
        public string BackgroundResource => backgroundResource;
        public TowerDefenseRouteGeometryConfig Route => route;
        public GameObject StraightFencePrefab => straightFencePrefab;
        public GameObject TurnFencePrefab => turnFencePrefab;
        public GameObject TerminalFencePrefab => terminalFencePrefab;
        public GameObject DirectionSignPrefab => directionSignPrefab;
        public Sprite DirectionArrowSprite => directionArrowSprite;
        public int[] AvailableProducts => availableProducts == null ? Array.Empty<int>() : (int[])availableProducts.Clone();
        public float FanSpeed => Mathf.Max(0f, fanSpeed);
        public int TotalFanCount => CamionerosFansToServe;
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
                SegmentLengths = runtimeRoute.SegmentLengths == null ? Array.Empty<float>() : (float[])runtimeRoute.SegmentLengths.Clone(),
                CorridorWidth = runtimeRoute.CorridorWidth,
                HorizontalCorridorWidth = runtimeRoute.HorizontalCorridorWidth,
                RowSpacing = runtimeRoute.RowSpacing,
                TurnRadius = runtimeRoute.TurnRadius,
                PlayableBounds = runtimeRoute.PlayableBounds,
                TurnSegments = runtimeRoute.TurnSegments
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
