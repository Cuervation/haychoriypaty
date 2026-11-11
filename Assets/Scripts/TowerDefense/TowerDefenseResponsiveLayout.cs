using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Responsive route bounds and screen-to-world mapping for the portrait safe-area canvas.</summary>
    public static class TowerDefenseResponsiveLayout
    {
        public const float RouteTopReserve = 240f;
        public const float FooterReserve = 240f;
        // Shared visual frame width used to anchor follower entry to the viewport edge.
        public const float FanFrameWidthLogical = 78f;
        // The 96px corridor leaves a visible margin around the fan frame.
        public const float CorridorWidthLogical = 96f;
        // Display-only clearance for the full-height fence artwork above the footer.
        public const float VisualStageLift = 24f;

        public static bool TryCreate(TowerDefenseLevelConfig source, Camera camera, Rect viewport,
            float logicalWidth, float logicalHeight, out TowerDefenseLevelConfig runtimeConfig,
            out TowerDefenseRouteGeometryConfig route, out float worldUnitsPerLogicalPixel,
            out Vector2 firstServiceZone, out Vector2 secondServiceZone, out string error)
        {
            runtimeConfig = null;
            route = null;
            worldUnitsPerLogicalPixel = 0f;
            firstServiceZone = secondServiceZone = Vector2.zero;
            error = null;
            if (source == null || camera == null || viewport.width <= 0f || logicalWidth <= 0f || logicalHeight <= 0f)
            {
                error = "Tower Defense needs its level config, active camera and a non-empty portrait viewport.";
                return false;
            }

            TowerDefenseRouteGeometryConfig authored = source.Route;
            if (authored == null || (authored.RouteKind == TowerDefenseRouteKind.HorizontalSingleFence
                ? authored.RowCount != 1 : authored.RowCount < 3 || authored.RowCount > 9 || authored.RowCount % 2 == 0))
            {
                error = "The editable route has an invalid row count for its route kind.";
                return false;
            }

            float canvasScale = viewport.width / logicalWidth;
            Vector2 origin = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, Vector2.zero);
            Vector2 onePixel = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, Vector2.right);
            worldUnitsPerLogicalPixel = Vector2.Distance(origin, onePixel);
            if (worldUnitsPerLogicalPixel <= 0f || float.IsNaN(worldUnitsPerLogicalPixel) || float.IsInfinity(worldUnitsPerLogicalPixel))
            {
                error = "The active camera does not produce a finite screen-to-world scale.";
                return false;
            }

            if (authored.RouteKind == TowerDefenseRouteKind.HorizontalSingleFence)
                return TryCreateHorizontal(source, camera, viewport, logicalWidth, logicalHeight, canvasScale,
                    worldUnitsPerLogicalPixel, out runtimeConfig, out route, out firstServiceZone, out secondServiceZone, out error);

            const float horizontalMargin = 16f;
            // The campaign's three runs sit below the mural and above its full footer.
            // Larger generator-only configurations keep their historic compact reservations.
            float topReserve = authored.RowCount == 3 ? RouteTopReserve : 170f;
            float footerReserve = authored.RowCount == 3 ? FooterReserve : 180f;
            float corridorLogical = Mathf.Min(CorridorWidthLogical,
                Mathf.Max(76f, (logicalHeight - footerReserve - topReserve) / 3f - 1f));
            float logicalBottom = logicalHeight - footerReserve;
            float maximumSpacing = (logicalBottom - topReserve - corridorLogical) / (authored.RowCount - 1f);
            if (maximumSpacing <= corridorLogical)
            {
                error = "The Tower Defense route does not fit the available vertical safe area without scaling its art.";
                return false;
            }

            float rowSpacingLogical = Mathf.Min(300f, maximumSpacing);
            float routeHeight = (authored.RowCount - 1) * rowSpacingLogical + corridorLogical;
            float top = Mathf.Max(topReserve, logicalBottom - routeHeight);
            Rect logicalBounds = new Rect(horizontalMargin, top, logicalWidth - horizontalMargin * 2f, routeHeight);
            Vector2 bottomLeft = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(logicalBounds.xMin, logicalBounds.yMax));
            Vector2 topRight = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(logicalBounds.xMax, logicalBounds.yMin));
            Rect worldBounds = Rect.MinMaxRect(Mathf.Min(bottomLeft.x, topRight.x), Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.x, topRight.x), Mathf.Max(bottomLeft.y, topRight.y));

            float segmentLogical = logicalWidth - horizontalMargin * 2f - corridorLogical - 12f;
            if (segmentLogical <= 0f)
            {
                error = "The route cannot fit a horizontal segment and two modular turns at this screen width.";
                return false;
            }
            var lengths = new float[authored.RowCount];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = segmentLogical * worldUnitsPerLogicalPixel;
            route = new TowerDefenseRouteGeometryConfig
            {
                RowCount = authored.RowCount,
                SegmentLengths = lengths,
                CorridorWidth = corridorLogical * worldUnitsPerLogicalPixel,
                HorizontalCorridorWidth = corridorLogical * .8f * worldUnitsPerLogicalPixel,
                RowSpacing = rowSpacingLogical * worldUnitsPerLogicalPixel,
                TurnRadius = authored.TurnRadius,
                PlayableBounds = worldBounds,
                TurnSegments = authored.TurnSegments
            };

            runtimeConfig = Object.Instantiate(source);
            runtimeConfig.name = source.name + " (runtime viewport)";
            TowerDefenseRouteGeometryResult geometry = TowerDefenseRouteGeometry.Generate(route);
            if (geometry == null || !geometry.IsValid)
            {
                error = geometry == null ? "Route generator returned no layout." : string.Join("; ", geometry.Validation.Errors);
                Object.Destroy(runtimeConfig);
                runtimeConfig = null;
                return false;
            }

            float entryCenterOffsetLogical = source.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? -FanFrameWidthLogical * .5f
                : FanFrameWidthLogical * .5f;
            Vector2 screenEntry = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(entryCenterOffsetLogical, logicalHeight * .5f));
            route.FanEntryExtensionWorldUnits = Mathf.Max(0f, geometry.Centerline[0].x - screenEntry.x);
            runtimeConfig.ConfigureRoute(route);

            firstServiceZone = runtimeConfig.WorkZonePosition(geometry, 0);
            secondServiceZone = runtimeConfig.WorkZonePosition(geometry, Mathf.Min(1, route.RowCount - 2));
            return true;
        }

        private static bool TryCreateHorizontal(TowerDefenseLevelConfig source, Camera camera, Rect viewport,
            float logicalWidth, float logicalHeight, float canvasScale, float pixel,
            out TowerDefenseLevelConfig runtimeConfig, out TowerDefenseRouteGeometryConfig route,
            out Vector2 firstServiceZone, out Vector2 secondServiceZone, out string error)
        {
            runtimeConfig = null;
            route = null;
            firstServiceZone = secondServiceZone = Vector2.zero;
            error = null;
            const float margin = 16f;
            const float corridorLogical = 88f;
            const float fenceClearanceLogical = 8f;
            float footer = FooterReserve;
            float routeY = RouteTopReserve + corridorLogical * .5f;
            float workTop = routeY + corridorLogical * .5f + fenceClearanceLogical;
            float workBottom = logicalHeight - footer - margin;
            if (logicalWidth <= margin * 2f + corridorLogical || workBottom - workTop < 160f)
            {
                error = "Horizontal single-fence layout needs a usable lower work area above the footer.";
                return false;
            }

            float entryX = -FanFrameWidthLogical * .5f;
            float exitX = logicalWidth + FanFrameWidthLogical * .5f;
            Vector2 routeTopLeft = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(entryX - corridorLogical * .5f, routeY - corridorLogical * .5f));
            Vector2 routeBottomRight = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(exitX + corridorLogical * .5f, routeY + corridorLogical * .5f));
            Rect worldRouteBounds = Rect.MinMaxRect(Mathf.Min(routeTopLeft.x, routeBottomRight.x), Mathf.Min(routeTopLeft.y, routeBottomRight.y),
                Mathf.Max(routeTopLeft.x, routeBottomRight.x), Mathf.Max(routeTopLeft.y, routeBottomRight.y));
            Vector2 workTopLeft = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(margin, workTop));
            Vector2 workBottomRight = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(logicalWidth - margin, workBottom));
            Rect workBounds = Rect.MinMaxRect(Mathf.Min(workTopLeft.x, workBottomRight.x), Mathf.Min(workTopLeft.y, workBottomRight.y),
                Mathf.Max(workTopLeft.x, workBottomRight.x), Mathf.Max(workTopLeft.y, workBottomRight.y));

            float runLogical = exitX - entryX;
            route = new TowerDefenseRouteGeometryConfig
            {
                RouteKind = TowerDefenseRouteKind.HorizontalSingleFence,
                RowCount = 1,
                SegmentLengths = new[] { runLogical * pixel },
                CorridorWidth = corridorLogical * pixel,
                HorizontalCorridorWidth = corridorLogical * pixel,
                RowSpacing = corridorLogical * 2f * pixel,
                TurnRadius = source.Route.TurnRadius,
                PlayableBounds = worldRouteBounds,
                WorkBounds = workBounds,
                TurnSegments = Mathf.Clamp(source.Route.TurnSegments, 4, 128)
            };
            runtimeConfig = UnityEngine.Object.Instantiate(source);
            runtimeConfig.name = source.name + " (runtime horizontal viewport)";
            TowerDefenseRouteGeometryResult geometry = TowerDefenseRouteGeometry.Generate(route);
            if (geometry == null || !geometry.IsValid)
            {
                error = geometry == null ? "Route generator returned no layout." : string.Join("; ", geometry.Validation.Errors);
                UnityEngine.Object.Destroy(runtimeConfig);
                runtimeConfig = null;
                return false;
            }
            Vector2 entry = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(entryX, routeY));
            route.FanEntryExtensionWorldUnits = Mathf.Max(0f, geometry.Centerline[0].x - entry.x);
            runtimeConfig.ConfigureRoute(route);
            firstServiceZone = secondServiceZone = workBounds.center;
            return true;
        }

        public static Vector2 LogicalToWorld(Camera camera, Rect viewport, float logicalWidth,
            float canvasScale, Vector2 logicalPosition)
        {
            float guiX = viewport.x + logicalPosition.x * canvasScale;
            float guiY = viewport.y + logicalPosition.y * canvasScale;
            float pixelY = camera.pixelHeight - guiY;
            float depth = Vector3.Dot(Vector3.zero - camera.transform.position, camera.transform.forward);
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(guiX, pixelY, depth));
            return new Vector2(world.x, world.y);
        }
    }
}
