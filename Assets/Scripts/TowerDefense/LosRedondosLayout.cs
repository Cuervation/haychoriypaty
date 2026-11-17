using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Single source of truth for the Los Redondos (ID10) Tower Defense layout, in logical
    /// canvas pixels (x right, y down, 540 px wide). Route rails, navigation, fences,
    /// hiring areas, stations and crews are all derived from this one result, so no
    /// second system can place the same objects with its own constants.
    /// </summary>
    public sealed class LosRedondosLayoutResult
    {
        public bool IsValid => string.IsNullOrEmpty(Error);
        public string Error { get; internal set; }
        /// <summary>Area between the mural sidewalk and the footer, in route-geometry coordinates.</summary>
        public Rect Playfield { get; internal set; }
        /// <summary>Envelope of both fence rails (outer rails of the vertical connectors included).</summary>
        public Rect RouteEnvelope { get; internal set; }
        /// <summary>Right-side strip reserved for workers, tables and barrels.</summary>
        public Rect WorkColumn { get; internal set; }
        /// <summary>Four stacked sectors: Parrillero 0, Cocacolero 0, Parrillero 1, Cocacolero 1.</summary>
        public Rect[] Sectors { get; internal set; }
        public float[] RowCenterY { get; internal set; }
        public float RowStartX { get; internal set; }
        public float RowEndX { get; internal set; }
        public float RowPitch { get; internal set; }
        public float HorizontalCorridor { get; internal set; }
        public float VerticalCorridor { get; internal set; }
        public float SegmentLength => RowEndX - RowStartX;
    }

    public static class LosRedondosLayout
    {
        public const int RowCount = 5;
        public const int SectorCount = 4;
        // Horizontal proportions of the 540 px canvas width (user design, 2026-11-16).
        public const float RouteStartFraction = .03f;
        public const float RouteEndFraction = .62f;
        // The previous ID10 worker strip was 30% of the width; the new one is 15% wider.
        public const float PreviousWorkStripFraction = .30f;
        public const float WorkStripGrowth = 1.15f;
        public const float WorkAreaWidthFraction = PreviousWorkStripFraction * WorkStripGrowth;
        public const float WorkAreaStartFraction = 1f - WorkAreaWidthFraction;
        // Same corridors as Camioneros: 96 px vertical connectors and 80% for the runs.
        public const float VerticalCorridorLogical = TowerDefenseResponsiveLayout.CorridorWidthLogical;
        public const float HorizontalCorridorLogical = TowerDefenseResponsiveLayout.CorridorWidthLogical * .8f;
        // fence-panel-v1 (2172x724 px) is drawn 120 px long, i.e. its renderer is 40 px tall
        // centered on the rail (opaque pixels reach 15 px above and 18 px below the line).
        public const float FenceArtHalfThicknessLogical = 20f;
        // Minimum free space between the rails of two consecutive rows (U-turn tongue).
        public const float MinimumRailGapLogical = 12f;
        // Taller screens keep the rows compact: the gap between the rails of two rows never
        // exceeds one fence's art (40 px), so the single fence drawn on that gap's midline
        // covers both rails and the rows are separated by one barrier, not two.
        public const float MaximumRowPitchLogical = HorizontalCorridorLogical + FenceArtHalfThicknessLogical * 2f;
        public const float WorkColumnRightInsetLogical = 2f;

        /// <summary>Areas in sector order: role/zone 0..3 = P0, C0, P1, C1.</summary>
        public static int SectorIndex(StreetWorkerRole role, int zone) =>
            Mathf.Clamp(zone, 0, 1) * 2 + (role == StreetWorkerRole.Cocacolero ? 1 : 0);

        /// <summary>World rect of a sector (0 = top) inside the world-space work column.</summary>
        public static Rect WorldSector(Rect workColumnWorld, int sector)
        {
            float height = workColumnWorld.height / SectorCount;
            int index = Mathf.Clamp(sector, 0, SectorCount - 1);
            return new Rect(workColumnWorld.xMin, workColumnWorld.yMax - (index + 1) * height, workColumnWorld.width, height);
        }

        public static LosRedondosLayoutResult Compute(float logicalWidth, float logicalHeight)
        {
            var result = new LosRedondosLayoutResult();
            if (logicalWidth <= 0f || logicalHeight <= 0f || float.IsNaN(logicalWidth) || float.IsNaN(logicalHeight))
            {
                result.Error = "Los Redondos layout needs a positive logical canvas.";
                return result;
            }

            // The world overlay is drawn VisualStageLift px higher than route coordinates,
            // so the visible sidewalk edge (RouteTopReserve - lift) and the footer top
            // (height - FooterReserve) map to these geometry coordinates.
            float playTop = TowerDefenseResponsiveLayout.RouteTopReserve;
            float playBottom = logicalHeight - TowerDefenseResponsiveLayout.FooterReserve + TowerDefenseResponsiveLayout.VisualStageLift;
            result.Playfield = Rect.MinMaxRect(0f, playTop, logicalWidth, playBottom);

            float horizontalCorridor = HorizontalCorridorLogical;
            float verticalCorridor = VerticalCorridorLogical;
            float available = playBottom - playTop - FenceArtHalfThicknessLogical * 2f;
            float pitch = Mathf.Min(MaximumRowPitchLogical, (available - horizontalCorridor) / (RowCount - 1));
            if (pitch - horizontalCorridor < MinimumRailGapLogical)
            {
                float required = (horizontalCorridor + MinimumRailGapLogical) * (RowCount - 1) +
                    horizontalCorridor + FenceArtHalfThicknessLogical * 2f;
                result.Error = "Los Redondos needs " + required + " px between sidewalk and footer for five rows; only " +
                    (playBottom - playTop) + " px are available.";
                return result;
            }

            float routeHeight = pitch * (RowCount - 1) + horizontalCorridor;
            float routeTop = playTop + FenceArtHalfThicknessLogical + (available - routeHeight) * .5f;
            float routeLeft = logicalWidth * RouteStartFraction;
            // 62% is the design target; never let a vertical rail's art reach the worker strip.
            float routeRight = Mathf.Min(logicalWidth * RouteEndFraction,
                logicalWidth * WorkAreaStartFraction - FenceArtHalfThicknessLogical);
            result.RouteEnvelope = Rect.MinMaxRect(routeLeft, routeTop, routeRight, routeTop + routeHeight);
            result.RowStartX = routeLeft + verticalCorridor * .5f;
            result.RowEndX = routeRight - verticalCorridor * .5f;
            if (result.SegmentLength <= verticalCorridor)
            {
                result.Error = "Los Redondos route runs are shorter than one connector at this width.";
                return result;
            }

            result.RowPitch = pitch;
            result.HorizontalCorridor = horizontalCorridor;
            result.VerticalCorridor = verticalCorridor;
            result.RowCenterY = new float[RowCount];
            for (int row = 0; row < RowCount; row++)
                result.RowCenterY[row] = routeTop + horizontalCorridor * .5f + row * pitch;

            // The column spans the same height as the fenced route (art included), so each
            // sector stays beside the two runs it serves on every portrait aspect ratio.
            Rect column = Rect.MinMaxRect(logicalWidth * WorkAreaStartFraction,
                routeTop - FenceArtHalfThicknessLogical, logicalWidth - WorkColumnRightInsetLogical,
                routeTop + routeHeight + FenceArtHalfThicknessLogical);
            result.WorkColumn = column;
            var sectors = new Rect[SectorCount];
            float sectorHeight = column.height / SectorCount;
            for (int i = 0; i < SectorCount; i++)
                sectors[i] = new Rect(column.xMin, column.yMin + i * sectorHeight, column.width, sectorHeight);
            result.Sectors = sectors;
            return result;
        }
    }
}
