using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum TowerDefenseFencePieceKind { Straight, Turn, Terminal, DirectionSign, ShortStraight }

    /// <summary>
    /// Constructs two independent rows of small SpriteRenderer pieces and continuous
    /// EdgeCollider2D route rails from the route's offset polylines. Optional visual
    /// endpoint fitting snaps entry/exit panels to camera edges without moving the route.
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
        [SerializeField, Min(0.05f)] private float shortPanelWorldLength = 0.33f;
        [SerializeField, Range(0f, 0.2f)] private float panelOverlap = 0.035f;
        [SerializeField, Min(0.01f)] private float colliderThickness = 0.12f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private int presentationLayer = 5;
        [SerializeField] private bool generateOnAwake;

        private TowerDefenseRouteGeometryResult latestGeometry;
        private string latestError;
        private bool hasVisualHorizontalBounds;
        private float visualLeftEdge, visualRightEdge;
        private Sprite visiblePanelSprite;
        private Sprite visiblePanelSource;

        public TowerDefenseLevelConfig LevelConfig { get => levelConfig; set => levelConfig = value; }
        public Sprite FencePanelSprite { get => fencePanelSprite; set => fencePanelSprite = value; }
        public TowerDefenseRouteGeometryResult LatestGeometry => latestGeometry;
        public string LatestError => latestError;
        public int GeneratedPieceCount { get; private set; }
        public int PresentationLayer { get => presentationLayer; set => presentationLayer = Mathf.Clamp(value, 0, 31); }
        public int SortingOrder { get => sortingOrder; set => sortingOrder = value; }

        /// <summary>Size the existing artwork in canvas units while keeping route geometry unchanged.</summary>
        public void SetLogicalPanelSize(float worldUnitsPerLogicalPixel)
        {
            if (worldUnitsPerLogicalPixel <= 0f || float.IsNaN(worldUnitsPerLogicalPixel) || float.IsInfinity(worldUnitsPerLogicalPixel)) return;
            panelWorldLength = 120f * worldUnitsPerLogicalPixel;
            shortPanelWorldLength = 60f * worldUnitsPerLogicalPixel;
            // The existing sprite has transparent side margins; overlap frames so their
            // visible rails touch on straight segments instead of spacing by empty pixels.
            panelOverlap = panelWorldLength * 0.42f;
        }

        /// <summary>Extend the generated fence panels to the camera's horizontal edges.</summary>
        public bool SetVisualHorizontalBounds(float worldLeft, float worldRight)
        {
            if (float.IsNaN(worldLeft) || float.IsInfinity(worldLeft) ||
                float.IsNaN(worldRight) || float.IsInfinity(worldRight) || worldRight <= worldLeft)
            {
                hasVisualHorizontalBounds = false;
                return false;
            }

            visualLeftEdge = worldLeft;
            visualRightEdge = worldRight;
            hasVisualHorizontalBounds = true;
            return true;
        }

        public void ClearVisualHorizontalBounds()
        {
            hasVisualHorizontalBounds = false;
        }

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

            PrepareVisiblePanelSprite();
            if (generatedRoot == null)
            {
                var root = new GameObject("Tower Defense — modular double fence");
                generatedRoot = root.transform;
                generatedRoot.SetParent(transform, false);
            }
            SetLayerRecursively(generatedRoot.gameObject, presentationLayer);
            ClearGeneratedChildren();
            BuildRail(FitVisualRailToHorizontalBounds(latestGeometry.LeftBoundary), 0);
            BuildRail(FitVisualRailToHorizontalBounds(latestGeometry.RightBoundary), 1);
            // Simulation/collision keep using the original route; only fence art snaps
            // to the screen edges, so screen framing cannot change follower waypoints.
            BuildEdge("Fence rail 0 collider", latestGeometry.LeftBoundary);
            BuildEdge("Fence rail 1 collider", latestGeometry.RightBoundary);
            BuildDirectionSigns(latestGeometry);
            return GeneratedPieceCount > 0;
        }

        private Vector2[] FitVisualRailToHorizontalBounds(Vector2[] source)
        {
            if (!hasVisualHorizontalBounds || source == null || source.Length < 2) return source;
            Vector2[] extended = (Vector2[])source.Clone();
            Vector2 firstTangent = FirstTangent(source);
            Vector2 lastTangent = LastTangent(source);

            // Pin visible endpoints exactly to camera bounds even when the authored
            // route already extends beyond them. Also supports a reversed route.
            if (firstTangent.x > 0.5f)
                extended[0].x = visualLeftEdge;
            else if (firstTangent.x < -0.5f)
                extended[0].x = visualRightEdge;

            int last = extended.Length - 1;
            if (lastTangent.x > 0.5f)
                extended[last].x = visualRightEdge;
            else if (lastTangent.x < -0.5f)
                extended[last].x = visualLeftEdge;

            return extended;
        }

        private Sprite RenderPanelSprite => visiblePanelSprite != null ? visiblePanelSprite : fencePanelSprite;

        private void PrepareVisiblePanelSprite()
        {
            if (visiblePanelSource == fencePanelSprite) return;
            if (visiblePanelSprite != null)
            {
                if (Application.isPlaying) Destroy(visiblePanelSprite); else DestroyImmediate(visiblePanelSprite);
                visiblePanelSprite = null;
            }
            visiblePanelSource = fencePanelSprite;
            if (fencePanelSprite == null || fencePanelSprite.name != "fence-panel-v1") return;
            // This shipped sprite's transparent side margins are 421/2172 and
            // 423/2172. Trim only those margins so visible rails meet at corners;
            // the underlying artwork and its height remain unchanged.
            Rect rect = fencePanelSprite.rect;
            rect.x += rect.width * (421f / 2172f);
            rect.width *= 1328f / 2172f;
            visiblePanelSprite = Sprite.Create(fencePanelSprite.texture, rect, new Vector2(.5f, .5f),
                fencePanelSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            visiblePanelSprite.name = "Tower Defense visible fence panel";
            visiblePanelSprite.hideFlags = HideFlags.DontSave;
        }

        private void OnDestroy()
        {
            if (visiblePanelSprite == null) return;
            if (Application.isPlaying) Destroy(visiblePanelSprite); else DestroyImmediate(visiblePanelSprite);
        }

        private void BuildRail(Vector2[] points, int railIndex)
        {
            float totalLength = PolylineLength(points);
            if (points == null || points.Length < 2 || totalLength <= 0f) return;

            List<Vector2> corners = CollapseCollinearPoints(points);
            int count = 0;
            for (int legIndex = 1; legIndex < corners.Count; legIndex++)
            {
                Vector2 start = corners[legIndex - 1];
                Vector2 end = corners[legIndex];
                Vector2 delta = end - start;
                float legLength = delta.magnitude;
                if (legLength <= 0.00001f) continue;

                Vector2 tangent = delta / legLength;
                bool verticalLeg = Mathf.Abs(tangent.y) > Mathf.Abs(tangent.x);
                // Vertical connectors and any short residual run use a half-length
                // module so a full panel cannot protrude across the turn.
                bool croppedPanel = visiblePanelSprite != null;
                float visibleFraction = croppedPanel ? 1328f / 2172f : 1f;
                float longLength = panelWorldLength * visibleFraction;
                float shortLength = shortPanelWorldLength * visibleFraction;
                bool useShortPanel = (!croppedPanel && verticalLeg) || legLength < (croppedPanel
                    ? longLength : Mathf.Max(0.05f, panelWorldLength - panelOverlap));
                float moduleLength = useShortPanel ? shortLength : longLength;
                // A run shorter than its preferred module must never spill over a
                // corner into the neighboring lane. Uniformly scale that one piece
                // to fit the straight boundary segment instead of leaving a partial
                // fence across the open passage.
                moduleLength = Mathf.Min(moduleLength, legLength);
                float moduleOverlap = croppedPanel ? moduleLength * .02f : panelWorldLength > 0.0001f
                    ? panelOverlap * (moduleLength / panelWorldLength)
                    : 0f;
                float visiblePitch = Mathf.Max(0.05f, moduleLength - moduleOverlap);
                int panelCount = legLength <= moduleLength + 0.00001f
                    ? 1
                    : Mathf.Max(2, Mathf.CeilToInt((legLength - moduleLength) / visiblePitch) + 1);
                // Anchor the first and last frames to this leg's corners. Interior
                // panels may overlap one another, but none can extend into the next
                // horizontal/vertical run and partially obstruct its passage.
                float spacing = panelCount > 1 ? (legLength - moduleLength) / (panelCount - 1) : 0f;
                float moduleScale = moduleLength / Mathf.Max(0.0001f, RenderPanelSprite.bounds.size.x);
                TowerDefenseFencePieceKind kind = useShortPanel
                    ? TowerDefenseFencePieceKind.ShortStraight
                    : TowerDefenseFencePieceKind.Straight;

                // Fit each leg independently: the visible panel rows begin/end on
                // the corner and keep regular spacing, without long-frame stubs.
                for (int panelIndex = 0; panelIndex < panelCount; panelIndex++)
                {
                    Vector2 position = start + tangent * (moduleLength * 0.5f + spacing * panelIndex);
                    CreatePiece("Fence rail " + railIndex + " segment " + count.ToString("D3"),
                        position, tangent, moduleScale, railIndex, count, kind);
                    count++;
                }
            }

            // Explicit end pieces communicate where the continuous rail terminates at entry/exit.
            float longScale = panelWorldLength * (visiblePanelSprite != null ? 1328f / 2172f : 1f) / Mathf.Max(0.0001f, RenderPanelSprite.bounds.size.x);
            CreatePiece("Fence rail " + railIndex + " terminal entry", points[0], FirstTangent(points),
                longScale, railIndex, count, TowerDefenseFencePieceKind.Terminal);
            CreatePiece("Fence rail " + railIndex + " terminal exit", points[points.Length - 1], LastTangent(points),
                longScale, railIndex, count + 1, TowerDefenseFencePieceKind.Terminal);
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
            pieceObject.transform.rotation = GetFencePanelRotation(tangent);

            var renderer = pieceObject.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = pieceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RenderPanelSprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder + Mathf.RoundToInt(-position.y * 12f);

            var collider = pieceObject.GetComponent<BoxCollider2D>();
            if (collider == null) collider = pieceObject.AddComponent<BoxCollider2D>();
            // Enlarge the artwork, not the collision thickness of the existing route.
            collider.size = new Vector2(RenderPanelSprite.bounds.size.x, colliderThickness / uniformScale);

            var metadata = pieceObject.GetComponent<TowerDefenseFencePiece>();
            if (metadata == null) metadata = pieceObject.AddComponent<TowerDefenseFencePiece>();
            metadata.Initialize(kind, railIndex, pieceIndex);
            GeneratedPieceCount++;
        }

        /// <summary>
        /// Keep the asymmetric fence artwork upright in screen space instead of
        /// rotating it with the direction of travel. Leftward horizontal runs
        /// otherwise flip the support feet above the rail, while alternating
        /// vertical runs swap them between sides.
        /// </summary>
        private static Quaternion GetFencePanelRotation(Vector2 tangent)
        {
            if (tangent.sqrMagnitude <= 0.000001f) return Quaternion.identity;
            float angle = Mathf.Abs(tangent.x) >= Mathf.Abs(tangent.y) ? 0f : 90f;
            return Quaternion.AngleAxis(angle, Vector3.forward);
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

        private static List<Vector2> CollapseCollinearPoints(Vector2[] points)
        {
            var corners = new List<Vector2>();
            if (points == null || points.Length == 0) return corners;

            corners.Add(points[0]);
            Vector2 direction = Vector2.zero;
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                if (delta.sqrMagnitude <= 0.000001f) continue;

                Vector2 nextDirection = delta.normalized;
                if (direction.sqrMagnitude > 0.000001f)
                {
                    float cross = direction.x * nextDirection.y - direction.y * nextDirection.x;
                    float dot = Vector2.Dot(direction, nextDirection);
                    if (Mathf.Abs(cross) > 0.001f || dot < 0.999f)
                    {
                        Vector2 corner = points[i - 1];
                        if ((corner - corners[corners.Count - 1]).sqrMagnitude > 0.000001f)
                            corners.Add(corner);
                    }
                }

                direction = nextDirection;
            }

            Vector2 end = points[points.Length - 1];
            if ((end - corners[corners.Count - 1]).sqrMagnitude > 0.000001f)
                corners.Add(end);
            return corners;
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
