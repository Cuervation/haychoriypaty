using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum TowerDefenseFencePieceKind { Straight, Turn, Terminal, DirectionSign }

    /// <summary>
    /// Constructs two independent rows of small SpriteRenderer pieces and matching
    /// continuous EdgeCollider2D rails from the route's offset polylines.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerDefenseModularFenceGenerator : MonoBehaviour
    {
        [SerializeField] private TowerDefenseLevelConfig levelConfig;
        [SerializeField] private Sprite fencePanelSprite;
        [SerializeField] private Sprite directionArrowSprite;
        [SerializeField] private GameObject straightPiecePrefab;
        [SerializeField] private GameObject turnPiecePrefab;
        [SerializeField] private GameObject terminalPiecePrefab;
        [SerializeField] private GameObject directionSignPrefab;
        [SerializeField] private Transform generatedRoot;
        [SerializeField, Min(0.05f)] private float panelWorldLength = 0.66f;
        [SerializeField, Range(0f, 0.2f)] private float panelOverlap = 0.035f;
        [SerializeField, Min(0.01f)] private float colliderThickness = 0.12f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private int presentationLayer = 5;
        [SerializeField] private bool generateOnAwake;

        private TowerDefenseRouteGeometryResult latestGeometry;
        private string latestError;

        public TowerDefenseLevelConfig LevelConfig { get => levelConfig; set => levelConfig = value; }
        public Sprite FencePanelSprite { get => fencePanelSprite; set => fencePanelSprite = value; }
        public TowerDefenseRouteGeometryResult LatestGeometry => latestGeometry;
        public string LatestError => latestError;
        public int GeneratedPieceCount { get; private set; }
        public int PresentationLayer { get => presentationLayer; set => presentationLayer = Mathf.Clamp(value, 0, 31); }

        private void Awake()
        {
            if (fencePanelSprite == null) fencePanelSprite = Resources.Load<Sprite>("fence-panel-v1");
            if (levelConfig != null)
            {
                if (directionArrowSprite == null) directionArrowSprite = levelConfig.DirectionArrowSprite;
                if (straightPiecePrefab == null) straightPiecePrefab = levelConfig.StraightFencePrefab;
                if (turnPiecePrefab == null) turnPiecePrefab = levelConfig.TurnFencePrefab;
                if (terminalPiecePrefab == null) terminalPiecePrefab = levelConfig.TerminalFencePrefab;
                if (directionSignPrefab == null) directionSignPrefab = levelConfig.DirectionSignPrefab;
            }
            if (directionArrowSprite == null) directionArrowSprite = Resources.Load<Sprite>("fence-direction-arrow-v1");
            if (straightPiecePrefab == null) straightPiecePrefab = Resources.Load<GameObject>("TowerDefense/FenceStraight");
            if (turnPiecePrefab == null) turnPiecePrefab = Resources.Load<GameObject>("TowerDefense/FenceTurn");
            if (terminalPiecePrefab == null) terminalPiecePrefab = Resources.Load<GameObject>("TowerDefense/FenceTerminal");
            if (directionSignPrefab == null) directionSignPrefab = Resources.Load<GameObject>("TowerDefense/FenceDirectionSign");
            if (generateOnAwake) Generate();
        }

        /// <summary>Rebuilds only this component's generated children, never scene or campaign objects.</summary>
        public bool Generate()
        {
            latestError = null;
            GeneratedPieceCount = 0;
            if (levelConfig == null)
            {
                latestError = "A TowerDefenseLevelConfig must be assigned.";
                return false;
            }
            if (fencePanelSprite == null)
            {
                latestError = "A modular fence-panel Sprite must be assigned or loaded from Resources/fence-panel-v1.";
                return false;
            }

            latestGeometry = TowerDefenseRouteGeometry.Generate(levelConfig.Route);
            if (latestGeometry == null || !latestGeometry.IsValid)
            {
                latestError = latestGeometry == null ? "Route generation returned no geometry."
                    : string.Join("; ", latestGeometry.Validation.Errors);
                return false;
            }

            if (generatedRoot == null)
            {
                var root = new GameObject("Tower Defense — modular double fence");
                generatedRoot = root.transform;
                generatedRoot.SetParent(transform, false);
            }
            SetLayerRecursively(generatedRoot.gameObject, presentationLayer);
            ClearGeneratedChildren();
            float uniformScale = panelWorldLength / Mathf.Max(0.0001f, fencePanelSprite.bounds.size.x);
            BuildRail(latestGeometry.LeftBoundary, 0, uniformScale);
            BuildRail(latestGeometry.RightBoundary, 1, uniformScale);
            BuildEdge("Fence rail 0 collider", latestGeometry.LeftBoundary);
            BuildEdge("Fence rail 1 collider", latestGeometry.RightBoundary);
            BuildDirectionSigns(latestGeometry);
            return GeneratedPieceCount > 0;
        }

        private void BuildRail(Vector2[] points, int railIndex, float uniformScale)
        {
            float totalLength = PolylineLength(points);
            if (points == null || points.Length < 2 || totalLength <= 0f) return;

            float pieceWidth = panelWorldLength;
            float pitch = Mathf.Max(0.05f, pieceWidth - panelOverlap);
            int count = Mathf.Max(1, Mathf.CeilToInt(totalLength / pitch));
            float centeredPitch = count > 1 ? (totalLength - pieceWidth) / (count - 1) : totalLength;
            centeredPitch = Mathf.Clamp(centeredPitch, pitch * 0.72f, pitch);
            float firstDistance = Mathf.Max(0f, (totalLength - centeredPitch * (count - 1)) * 0.5f);

            for (int i = 0; i < count; i++)
            {
                float distance = Mathf.Min(totalLength, firstDistance + centeredPitch * i);
                Vector2 position, tangent;
                SamplePolyline(points, distance, out position, out tangent);
                TowerDefenseFencePieceKind kind = IsNearTurn(distance, points, panelWorldLength * 0.42f)
                    ? TowerDefenseFencePieceKind.Turn : TowerDefenseFencePieceKind.Straight;
                CreatePiece("Fence rail " + railIndex + " segment " + i.ToString("D3"),
                    position, tangent, uniformScale, railIndex, i, kind);
            }

            // Explicit end pieces communicate where the continuous rail terminates at entry/exit.
            CreatePiece("Fence rail " + railIndex + " terminal entry", points[0], FirstTangent(points),
                uniformScale, railIndex, count, TowerDefenseFencePieceKind.Terminal);
            CreatePiece("Fence rail " + railIndex + " terminal exit", points[points.Length - 1], LastTangent(points),
                uniformScale, railIndex, count + 1, TowerDefenseFencePieceKind.Terminal);
        }

        private void CreatePiece(string objectName, Vector2 position, Vector2 tangent, float uniformScale,
            int railIndex, int pieceIndex, TowerDefenseFencePieceKind kind)
        {
            GameObject prefab = kind == TowerDefenseFencePieceKind.Turn ? turnPiecePrefab
                : kind == TowerDefenseFencePieceKind.Terminal ? terminalPiecePrefab : straightPiecePrefab;
            var pieceObject = prefab != null ? Instantiate(prefab, generatedRoot, false) : new GameObject(objectName);
            pieceObject.name = objectName;
            pieceObject.transform.SetParent(generatedRoot, false);
            SetLayerRecursively(pieceObject, presentationLayer);
            pieceObject.transform.position = new Vector3(position.x, position.y, 0f);
            pieceObject.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            if (tangent.sqrMagnitude > 0.000001f)
                pieceObject.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg, Vector3.forward);

            var renderer = pieceObject.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = pieceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = fencePanelSprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            var collider = pieceObject.GetComponent<BoxCollider2D>();
            if (collider == null) collider = pieceObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(fencePanelSprite.bounds.size.x,
                Mathf.Max(colliderThickness / uniformScale, fencePanelSprite.bounds.size.y * 0.28f));

            var metadata = pieceObject.GetComponent<TowerDefenseFencePiece>();
            if (metadata == null) metadata = pieceObject.AddComponent<TowerDefenseFencePiece>();
            metadata.Initialize(kind, railIndex, pieceIndex);
            GeneratedPieceCount++;
        }

        private void BuildDirectionSigns(TowerDefenseRouteGeometryResult geometry)
        {
            if (directionArrowSprite == null || directionSignPrefab == null || geometry.Centerline == null) return;
            TowerDefenseRouteGeometryConfig route = levelConfig.Route;
            int stride = route.TurnSegments + 1;
            for (int turn = 0; turn < route.RowCount - 1; turn++)
            {
                int arcMiddle = turn * stride + 1 + route.TurnSegments / 2;
                if (arcMiddle < 0 || arcMiddle >= geometry.Centerline.Length) continue;
                Vector2 point = geometry.Centerline[arcMiddle];
                Vector2 tangent = geometry.Centerline[Mathf.Min(arcMiddle + 1, geometry.Centerline.Length - 1)] -
                    geometry.Centerline[Mathf.Max(arcMiddle - 1, 0)];
                if (tangent.sqrMagnitude < 0.000001f) tangent = Vector2.down;
                CreateDirectionSign("Route direction sign " + turn,
                    point + Perpendicular(tangent.normalized) * (route.CorridorWidth * 0.85f), tangent);
            }
        }

        private void CreateDirectionSign(string objectName, Vector2 position, Vector2 direction)
        {
            var sign = Instantiate(directionSignPrefab, generatedRoot, false);
            sign.name = objectName;
            SetLayerRecursively(sign, presentationLayer);
            sign.transform.position = new Vector3(position.x, position.y, 0f);
            sign.transform.localScale = Vector3.one * 0.42f;
            sign.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);
            var renderer = sign.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = sign.AddComponent<SpriteRenderer>();
            renderer.sprite = directionArrowSprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder + 2;
            var collider = sign.GetComponent<Collider2D>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            var metadata = sign.GetComponent<TowerDefenseFencePiece>();
            if (metadata == null) metadata = sign.AddComponent<TowerDefenseFencePiece>();
            metadata.Initialize(TowerDefenseFencePieceKind.DirectionSign, -1, GeneratedPieceCount);
            GeneratedPieceCount++;
        }

        private void BuildEdge(string objectName, Vector2[] worldPoints)
        {
            var collisionObject = new GameObject(objectName);
            collisionObject.layer = presentationLayer;
            collisionObject.transform.SetParent(generatedRoot, false);
            var edge = collisionObject.AddComponent<EdgeCollider2D>();
            var localPoints = new Vector2[worldPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++)
                localPoints[i] = generatedRoot.InverseTransformPoint(new Vector3(worldPoints[i].x, worldPoints[i].y, 0f));
            edge.points = localPoints;
            edge.edgeRadius = colliderThickness * 0.5f;
        }

        private void ClearGeneratedChildren()
        {
            if (generatedRoot == null) return;
            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = generatedRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static bool IsNearTurn(float distance, Vector2[] points, float lookAroundDistance)
        {
            if (points == null || points.Length < 3) return false;
            Vector2 before, beforeTangent, after, afterTangent;
            SamplePolyline(points, Mathf.Max(0f, distance - lookAroundDistance), out before, out beforeTangent);
            SamplePolyline(points, distance + lookAroundDistance, out after, out afterTangent);
            return Vector2.Dot(beforeTangent, afterTangent) < 0.995f;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null) return;
            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }

        private static float PolylineLength(Vector2[] points)
        {
            if (points == null || points.Length < 2) return 0f;
            float length = 0f;
            for (int i = 1; i < points.Length; i++) length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }

        private static void SamplePolyline(Vector2[] points, float distance, out Vector2 position, out Vector2 tangent)
        {
            float remaining = distance;
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 segment = points[i] - points[i - 1];
                float segmentLength = segment.magnitude;
                if (segmentLength <= 0.00001f) continue;
                tangent = segment / segmentLength;
                if (remaining <= segmentLength)
                {
                    position = points[i - 1] + tangent * remaining;
                    return;
                }
                remaining -= segmentLength;
            }
            tangent = LastTangent(points);
            position = points[points.Length - 1];
        }

        private static Vector2 FirstTangent(Vector2[] points)
        {
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                if (delta.sqrMagnitude > 0.000001f) return delta.normalized;
            }
            return Vector2.right;
        }

        private static Vector2 LastTangent(Vector2[] points)
        {
            for (int i = points.Length - 1; i > 0; i--)
            {
                Vector2 delta = points[i] - points[i - 1];
                if (delta.sqrMagnitude > 0.000001f) return delta.normalized;
            }
            return Vector2.right;
        }

        private void OnDrawGizmosSelected()
        {
            TowerDefenseRouteGeometryResult geometry = latestGeometry;
            if (geometry == null && levelConfig != null) geometry = TowerDefenseRouteGeometry.Generate(levelConfig.Route);
            if (geometry == null) return;
            DrawPolyline(geometry.Centerline, Color.yellow);
            DrawPolyline(geometry.LeftBoundary, new Color(0.2f, 0.9f, 1f));
            DrawPolyline(geometry.RightBoundary, new Color(1f, 0.3f, 0.6f));
            if (geometry.IsValid && levelConfig != null && levelConfig.Route.RowCount == 3)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(levelConfig.WorkZonePosition(geometry, 0), 0.18f);
                Gizmos.DrawWireSphere(levelConfig.WorkZonePosition(geometry, 1), 0.18f);
            }
        }

        private static void DrawPolyline(Vector2[] points, Color color)
        {
            if (points == null || points.Length < 2) return;
            Gizmos.color = color;
            for (int i = 1; i < points.Length; i++) Gizmos.DrawLine(points[i - 1], points[i]);
        }

        private static Vector2 Perpendicular(Vector2 direction) => new Vector2(-direction.y, direction.x);
    }
}
