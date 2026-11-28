using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Responsive route bounds and screen-to-world mapping for the portrait safe-area canvas.</summary>
    public static class TowerDefenseResponsiveLayout
    {
        public const float RouteTopReserve = 240f;
        public const float FooterReserve = 240f;

        /// <summary>
        /// Top of a kitchen-derived level's footer: the same place as its source campaign level's (Boca's for Luján,
        /// Nueva Chicago's for Camioneros): the kitchen field bottom, stretched with the screen height.
        /// </summary>
        public static float PilgrimFooterTop(float logicalHeight, int sourceCampaignLevelId) =>
            TowerDefenseGameMode.KitchenLayoutForLevel(FooterSourceLevelId(sourceCampaignLevelId)).PlayfieldBottom * logicalHeight / StreetSceneLayout.Height;

        /// <summary>The campaign level whose footer a Tower Defense level copies. La Renga keeps Camioneros' (Nueva Chicago's) footer.</summary>
        public static int FooterSourceLevelId(int sourceCampaignLevelId) =>
            sourceCampaignLevelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId ? TowerDefenseLevelConfig.LosRedondosCampaignLevelId : sourceCampaignLevelId;
        // Shared visual frame width used to anchor follower entry to the viewport edge.
        public const float FanFrameWidthLogical = 78f;
        // The 96px corridor leaves a visible margin around the fan frame.
        public const float CorridorWidthLogical = 96f;
        // Display-only clearance for the full-height fence artwork above the footer.
        public const float VisualStageLift = 24f;
        // Full fan body height shared by Camioneros and Los Redondos.
        public const float LosRedondosFanBodyLogical = 76f;

        /// <summary>Only offer a narrower view below the shortest validated phone (320×568).</summary>
        public static bool TryCompactViewport(Rect safeViewport, out Rect viewport)
        {
            viewport = safeViewport;
            const float shortestProtectedHeight = 958f; // 320×568 is 958.5 logical pixels high.
            if (safeViewport.width <= 0f || safeViewport.height <= 0f ||
                safeViewport.height * StreetSceneLayout.Width >= safeViewport.width * shortestProtectedHeight)
                return false;
            float width = safeViewport.height * StreetSceneLayout.Width / StreetSceneLayout.Height;
            viewport = new Rect(safeViewport.center.x - width * .5f, safeViewport.y, width, safeViewport.height);
            return true;
        }

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
            if (authored == null || (authored.RouteKind != TowerDefenseRouteKind.Serpentine
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

            if (authored.RouteKind == TowerDefenseRouteKind.CShapedSingleFence)
                return TryCreateCShaped(source, camera, viewport, logicalWidth, logicalHeight, canvasScale,
                    worldUnitsPerLogicalPixel, out runtimeConfig, out route, out firstServiceZone, out secondServiceZone, out error);
            if (authored.RouteKind == TowerDefenseRouteKind.HorizontalSingleFence)
                return TryCreateHorizontal(source, camera, viewport, logicalWidth, logicalHeight, canvasScale,
                    worldUnitsPerLogicalPixel, out runtimeConfig, out route, out firstServiceZone, out secondServiceZone, out error);
            if (source.SourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId)
                return TryCreateLosRedondos(source, camera, viewport, logicalWidth, logicalHeight, canvasScale,
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

        /// <summary>
        /// Los Redondos ID10: one continuous five-run serpentine on the left 3%..62% of the
        /// width and four worker sectors on the right 34.5%. Route, rails, navigation and
        /// work areas all come from <see cref="LosRedondosLayout"/>; the result is rejected
        /// with the concrete defect if the shared validator finds any problem.
        /// </summary>
        private static bool TryCreateLosRedondos(TowerDefenseLevelConfig source, Camera camera, Rect viewport,
            float logicalWidth, float logicalHeight, float canvasScale, float pixel,
            out TowerDefenseLevelConfig runtimeConfig, out TowerDefenseRouteGeometryConfig route,
            out Vector2 firstServiceZone, out Vector2 secondServiceZone, out string error)
        {
            runtimeConfig = null;
            route = null;
            firstServiceZone = secondServiceZone = Vector2.zero;
            error = null;
            if (source.Route.RowCount != LosRedondosLayout.RowCount)
            {
                error = "Los Redondos must author exactly " + LosRedondosLayout.RowCount + " route rows.";
                return false;
            }
            LosRedondosLayoutResult layout = LosRedondosLayout.Compute(logicalWidth, logicalHeight);
            if (!layout.IsValid)
            {
                error = layout.Error;
                return false;
            }

            var lengths = new float[LosRedondosLayout.RowCount];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = layout.SegmentLength * pixel;
            route = new TowerDefenseRouteGeometryConfig
            {
                RouteKind = TowerDefenseRouteKind.Serpentine,
                RowCount = LosRedondosLayout.RowCount,
                SegmentLengths = lengths,
                CorridorWidth = layout.VerticalCorridor * pixel,
                HorizontalCorridorWidth = layout.HorizontalCorridor * pixel,
                RowSpacing = layout.RowPitch * pixel,
                TurnRadius = source.Route.TurnRadius,
                PlayableBounds = LogicalRectToWorld(camera, viewport, logicalWidth, canvasScale, layout.RouteEnvelope),
                // Serpentine routes ignore WorkBounds; ID10 uses it for its right-side worker column.
                WorkBounds = LogicalRectToWorld(camera, viewport, logicalWidth, canvasScale, layout.WorkColumn),
                TurnSegments = source.Route.TurnSegments
            };
            TowerDefenseRouteGeometryResult geometry = TowerDefenseRouteGeometry.Generate(route);
            if (geometry == null || !geometry.IsValid)
            {
                error = geometry == null ? "Route generator returned no layout." : string.Join("; ", geometry.Validation.Errors);
                return false;
            }

            // Supporters enter flush with the left screen edge, as before for ID10.
            Vector2 screenEntry = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(FanFrameWidthLogical * .5f, logicalHeight * .5f));
            route.FanEntryExtensionWorldUnits = Mathf.Max(0f, geometry.Centerline[0].x - screenEntry.x);

            TowerDefenseRouteLayoutReport report = ValidateLosRedondos(route, geometry, pixel);
            if (!report.IsValid)
            {
                error = "Los Redondos route layout is invalid: " + report;
                return false;
            }

            runtimeConfig = Object.Instantiate(source);
            runtimeConfig.name = source.name + " (runtime viewport)";
            runtimeConfig.ConfigureRoute(route);
            Rect firstPair = Rect.MinMaxRect(LosRedondosLayout.WorldSector(route.WorkBounds, 1).xMin,
                LosRedondosLayout.WorldSector(route.WorkBounds, 1).yMin, route.WorkBounds.xMax,
                LosRedondosLayout.WorldSector(route.WorkBounds, 0).yMax);
            Rect secondPair = Rect.MinMaxRect(LosRedondosLayout.WorldSector(route.WorkBounds, 3).xMin,
                LosRedondosLayout.WorldSector(route.WorkBounds, 3).yMin, route.WorkBounds.xMax,
                LosRedondosLayout.WorldSector(route.WorkBounds, 2).yMax);
            firstServiceZone = firstPair.center;
            secondServiceZone = secondPair.center;
            return true;
        }

        /// <summary>Runs the shared route validator against ID10's exact route, navigation and four sectors.</summary>
        public static TowerDefenseRouteLayoutReport ValidateLosRedondos(TowerDefenseRouteGeometryConfig route,
            TowerDefenseRouteGeometryResult geometry, float pixel)
        {
            var sectors = new Rect[LosRedondosLayout.SectorCount];
            for (int i = 0; i < sectors.Length; i++) sectors[i] = LosRedondosLayout.WorldSector(route.WorkBounds, i);
            var clearance = new TowerDefenseRouteClearance
            {
                FanWidth = FanFrameWidthLogical * pixel,
                FanDepth = LosRedondosFanBodyLogical * pixel,
                FenceHalfThickness = LosRedondosLayout.FenceArtHalfThicknessLogical * pixel,
                Tolerance = .01f * pixel
            };
            return TowerDefenseRouteLayoutValidator.Validate(geometry, route, clearance, sectors,
                TowerDefenseGameMode.BuildFanRouteWaypoints(geometry, route.FanEntryExtensionWorldUnits));
        }

        private static Rect LogicalRectToWorld(Camera camera, Rect viewport, float logicalWidth, float canvasScale, Rect logical)
        {
            Vector2 a = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(logical.xMin, logical.yMin));
            Vector2 b = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(logical.xMax, logical.yMax));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
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
            // The fence stays where the 88 px corridor put it; the narrower corridor walks the pilgrims right
            // behind it so workers hand the order over instead of reaching across a wide yard.
            const float fenceOffsetLogical = 88f;
            const float corridorLogical = 24f;
            const float fenceClearanceLogical = 8f;
            float routeY = RouteTopReserve + fenceOffsetLogical - corridorLogical * .5f;
            float workTop = RouteTopReserve + fenceOffsetLogical + fenceClearanceLogical;
            // The kitchen is Boca's own: its footer starts where Boca's does and the furniture sits at Boca's
            // (vertically stretched) positions, so the work area ends at the bottom of Boca's lowest piece, the grills.
            float kitchenVerticalScale = logicalHeight / StreetSceneLayout.Height;
            // The stage is drawn VisualStageLift logical px above the coordinates it is computed from (the route and
            // mural already account for it), so add it back to make the grills end exactly where Boca's end.
            float workBottom = TowerDefenseGameMode.BocaStationsBottomLogical(
                TowerDefenseGameMode.KitchenLayoutForLevel(source.SourceCampaignLevelId), kitchenVerticalScale) + VisualStageLift;
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
            // Boca's kitchen row spans the whole 540 px canvas, so the work area has no side margin: only that keeps
            // the furniture at Boca's size. The bottom margin above the footer stays.
            // Boca's row overhangs the 540 px canvas by 1.2 px on each side, so the work area does too.
            const float workSideMarginLogical = -1.2f;
            Vector2 workTopLeft = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(workSideMarginLogical, workTop));
            Vector2 workBottomRight = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(logicalWidth - workSideMarginLogical, workBottom));
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
                KitchenVerticalScale = kitchenVerticalScale,
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

        // The "C" fence (La Renga): the top run sits 42.5 px under the sidewalk curb, the right run stays 65.7 px (74 screen px)
        // inside the screen edge and the bottom run sits 46.8 logical px above the footer (stage lift included); the work pocket is the rectangle the fence encloses, and the fans
        // walk around the outside. Everything is in the same logical coordinates as the horizontal route (stage lift included).
        private const float CShapedTopFenceOffset = 42.5f, CShapedRightFenceInset = 65.7f, CShapedBottomFenceAboveFooter = 38.83f,
            CShapedCorridor = 24f, CShapedPocketClearance = 8f,
            // The downward run of the line walks this much further right than the corridor's own centre (the fence stays put),
            // so the fans no longer stand on the right rail.
            CShapedRightLaneShift = 31f,
            // The bottom run walks this much further below the bottom fence (the fence stays put): the fans pass under it, not on it.
            CShapedBottomLaneExtra = 38f;

        private static bool TryCreateCShaped(TowerDefenseLevelConfig source, Camera camera, Rect viewport,
            float logicalWidth, float logicalHeight, float canvasScale, float pixel,
            out TowerDefenseLevelConfig runtimeConfig, out TowerDefenseRouteGeometryConfig route,
            out Vector2 firstServiceZone, out Vector2 secondServiceZone, out string error)
        {
            runtimeConfig = null;
            route = null;
            firstServiceZone = secondServiceZone = Vector2.zero;
            error = null;
            float topFence = RouteTopReserve + CShapedTopFenceOffset;
            // Los Redondos' active kitchen (2026-11-24) copies Racing's/Boca's footer, which is taller than Ferro's.
            float bottomFence = PilgrimFooterTop(logicalHeight, source.ActiveKitchen ? TowerDefenseLevelConfig.PeregrinacionLujanCampaignLevelId : source.SourceCampaignLevelId) - CShapedBottomFenceAboveFooter;
            float rightFence = logicalWidth - CShapedRightFenceInset;
            if (bottomFence - topFence < 300f || rightFence < logicalWidth * .5f)
            {
                error = "The C-shaped fence needs a usable pocket between the mural and the footer.";
                return false;
            }
            float half = CShapedCorridor * .5f;
            float verticalCorridor = CShapedCorridor + 2f * CShapedRightLaneShift, verticalHalf = verticalCorridor * .5f;
            float topCenter = topFence - half, bottomCenter = bottomFence + half + CShapedBottomLaneExtra, rightCenter = rightFence + verticalHalf;
            float entryX = -FanFrameWidthLogical * .5f;
            Vector2 a = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(entryX - verticalHalf, topCenter - half)); // symmetric about the run (same half width as the right end), so its left end lands exactly on entryX
            Vector2 b = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(rightCenter + verticalHalf, bottomCenter + half));
            Rect playable = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            // The work pocket: from the screen's left edge to just inside the right rail, between the top and bottom rails.
            Vector2 workA = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(0f, topFence + CShapedPocketClearance));
            Vector2 workB = LogicalToWorld(camera, viewport, logicalWidth, canvasScale,
                new Vector2(rightFence - CShapedPocketClearance, bottomFence - CShapedPocketClearance));
            Rect workBounds = Rect.MinMaxRect(Mathf.Min(workA.x, workB.x), Mathf.Min(workA.y, workB.y),
                Mathf.Max(workA.x, workB.x), Mathf.Max(workA.y, workB.y));
            route = new TowerDefenseRouteGeometryConfig
            {
                RouteKind = TowerDefenseRouteKind.CShapedSingleFence,
                RowCount = 1,
                SegmentLengths = new[] { (rightCenter - entryX) * pixel },
                CorridorWidth = verticalCorridor * pixel,
                HorizontalCorridorWidth = CShapedCorridor * pixel,
                BottomLaneExtra = CShapedBottomLaneExtra * pixel,
                RowSpacing = (bottomCenter - topCenter) * pixel,
                TurnRadius = source.Route.TurnRadius,
                PlayableBounds = playable,
                WorkBounds = workBounds,
                KitchenVerticalScale = logicalHeight / StreetSceneLayout.Height,
                TurnSegments = Mathf.Clamp(source.Route.TurnSegments, 4, 128)
            };
            runtimeConfig = UnityEngine.Object.Instantiate(source);
            runtimeConfig.name = source.name + " (runtime C-shaped viewport)";
            TowerDefenseRouteGeometryResult geometry = TowerDefenseRouteGeometry.Generate(route);
            if (geometry == null || !geometry.IsValid)
            {
                error = geometry == null ? "Route generator returned no layout." : string.Join("; ", geometry.Validation.Errors);
                UnityEngine.Object.Destroy(runtimeConfig);
                runtimeConfig = null;
                return false;
            }
            Vector2 entry = LogicalToWorld(camera, viewport, logicalWidth, canvasScale, new Vector2(entryX, topCenter));
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
