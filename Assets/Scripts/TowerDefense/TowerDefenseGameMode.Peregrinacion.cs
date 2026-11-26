using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public readonly struct TowerDefensePlacementResult
    {
        public readonly bool IsValid;
        public readonly string Reason;
        public readonly Rect Bounds;
        public readonly Vector2 Anchor;
        public TowerDefensePlacementResult(bool isValid, string reason, Rect bounds, Vector2 anchor)
        { IsValid = isValid; Reason = reason ?? string.Empty; Bounds = bounds; Anchor = anchor; }
    }

    public sealed class TowerDefensePlacedStation
    {
        public int Id { get; }
        public TowerDefenseStationKind Kind { get; }
        public Rect Bounds { get; }
        public Vector2 WorkPosition { get; }
        public Vector2 PickupPosition { get; }
        internal TowerDefensePlacedStation(int id, TowerDefenseStationKind kind, Rect bounds, Vector2 work, Vector2 pickup)
        { Id = id; Kind = kind; Bounds = bounds; WorkPosition = work; PickupPosition = pickup; }
    }

    /// <summary>Free-build domain additions used only by the opt-in horizontal campaign level.</summary>
    public sealed partial class TowerDefenseGameMode
    {
        public const float ConstructionGridLogical = 32f;
        public const float ConstructionFurnitureClearanceLogical = 12f;
        // La Renga's cooks climb the 23 px corridor between the premium table and the barrels, so their collision feet are narrower there.
        private float FootHalfWidthLogical => IsCShapedLevel ? CShapedFootHalfWidthLogical : BocaFootHalfWidthLogical;
        private const float CShapedFootHalfWidthLogical = 9f;
        private const float BocaFootHalfWidthLogical = 18f, BocaFootDepthLogical = 12f, BocaTableBaseDepthLogical = 20f;
        // La Renga: gap between the far edge of a solid table and the feet of the worker taking a sandwich from behind it.
        private const float TableBackPickupClearanceLogical = 2f;
        private const int BocaPostSlots = 4;
        // Workers lean on the fence while serving: feet stay below the rail, the head rises above it, so the
        // order is handed over from close by instead of across the whole yard.
        private const float ServiceLeanLogical = 56f;
        // The kitchen starts this far from the pocket's left edge.
        private const float CShapedKitchenSideMarginLogical = 12f, CShapedKitchenLeftLogical = 12f, CShapedKitchenDropLogical = 40f;
        // Visible body width of a worker in Boca's own pixels (StreetKitchenLayout.GrillWorkPosition reserves 30 px per side).
        private const float BocaBodyWidthPx = 60f;
        // Boca's counter row pieces overlap by this many of its own pixels (tables/barrels are 1.2 px wider than their step).
        private const float BocaPieceOverlapPx = 1.2f;
        public const float ConstructionWorkerWidthLogical = ParrilleroVisualWidthLogical;
        public const float ConstructionWorkerHeightLogical = WorkerVisualHeightLogical;
        private const float BocaKitchenWorkerLaneLogical = WorkerVisualHeightLogical + 6f;
        private static readonly StreetKitchenLayout BocaKitchenLayoutStatic =
            new StreetKitchenLayout(new[] { 0, 1, 2, 3, 4, 5, 6 }, true);
        // Nueva Chicago's kitchen: one grill, one sandwich table and the Coca barrel (traditional layout, products 0 and 4).
        private static readonly StreetKitchenLayout ChicagoKitchenLayoutStatic =
            new StreetKitchenLayout(new[] { 0, 4 }, false) { SideBarrelPickup = true }; // drinks from the barrel's side, like La Renga (the C-shaped fence)
        // Ferro's kitchen: normal and premium grill and table, Coca and beer barrels (products 0, 1, 2, 4 and 6).
        private static readonly StreetKitchenLayout FerroKitchenLayoutStatic =
            new StreetKitchenLayout(new[] { 0, 1, 2, 4, 6 }, false);
                // La Renga's kitchen is Ferro's with two columns less of food on its grills (2 per product instead of 3).
        private static readonly StreetKitchenLayout RengaKitchenLayoutStatic =
            new StreetKitchenLayout(new[] { 0, 1, 2, 4, 6 }, false, true);
        // Los Redondos' active kitchen is Racing's seven stations with La Renga's narrow grills and tables and its barrel stack (2026-11-25).
        private static readonly StreetKitchenLayout RedondosKitchenLayoutStatic =
            new StreetKitchenLayout(new[] { 0, 1, 2, 3, 4, 5, 6 }, true, true, true);
        internal bool NarrowGrills => config.UsesLaRengaDynamics;
        /// <summary>Drinks are taken from the barrel's right side, facing it (La Renga and, on the same C-shaped fence, Camioneros).</summary>
        internal bool BarrelsFromTheSide => BocaKitchenLayout.BarrelsFromTheSide;
        internal static StreetKitchenLayout LujanBocaKitchenVisualLayout => BocaKitchenLayoutStatic;

        /// <summary>The campaign kitchen a kitchen-derived level copies: Chicago's for Camioneros, Boca's for Luján.</summary>
        internal static StreetKitchenLayout KitchenLayoutForLevel(int sourceCampaignLevelId) =>
            sourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId ? ChicagoKitchenLayoutStatic
            : sourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId ? FerroKitchenLayoutStatic
            : sourceCampaignLevelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId ? RengaKitchenLayoutStatic
            : BocaKitchenLayoutStatic;
        // Los Redondos on La Renga's C-shaped fence (2026-11-24) cooks in La Renga's kitchen; the footer still copies Ferro's (FooterSourceLevelId).
        // 2026-11-24: with the active kitchen it is Racing's (the same seven-station layout as Luján's).
        private StreetKitchenLayout BocaKitchenLayout => config.ActiveKitchen ? (IsCShapedLevel ? RedondosKitchenLayoutStatic : BocaKitchenLayoutStatic) : KitchenLayoutForLevel(config.UsesLaRengaDynamics
            ? TowerDefenseLevelConfig.LaRengaCampaignLevelId : config.SourceCampaignLevelId);
        internal StreetKitchenLayout SourceKitchenLayout => BocaKitchenLayout;
        private readonly List<TowerDefensePlacedStation> placedStations = new List<TowerDefensePlacedStation>();
        private long equipmentSpendCents;
        private int nextStationId = 1;

        public Rect WorkBounds => config.FreeConstruction ? route.WorkBounds : Rect.zero;
        public bool EquipmentPlacementAllowed => config.EquipmentPlacementAllowed;
        public IReadOnlyList<TowerDefensePlacedStation> PlacedStations => placedStations;
        public long EquipmentSpendCents => equipmentSpendCents;

        public int StationCost(TowerDefenseStationKind kind)
        {
            if (!config.FreeConstruction || !Enum.IsDefined(typeof(TowerDefenseStationKind), kind)) return 0;
            if (!config.EquipmentPlacementAllowed) return -1;
            int alreadyPlaced = 0;
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == kind) alreadyPlaced++;
            return alreadyPlaced < config.FreeStationCount(kind) ? 0 : config.StationCost;
        }

        public bool HasStation(TowerDefenseStationKind kind)
        {
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == kind) return true;
            return false;
        }

        private void InitializeStartingStations()
        {
            if (!config.FreeConstruction) return;
            TowerDefenseStartingStation[] starting = config.StartingStations;
            for (int i = 0; i < starting.Length; i++)
            {
                Vector2 position = IsBocaKitchenLevel ? BocaStationCenter(starting[i].Kind)
                    : WorkBounds.min + starting[i].PositionFromWorkBoundsMin * worldUnitsPerLogicalPixel;
                if (!Enum.IsDefined(typeof(TowerDefenseStationKind), starting[i].Kind) ||
                    !IsFinite(position.x) || !IsFinite(position.y))
                    throw new ArgumentException("Invalid starting station entry for " + config.LevelId + ".", nameof(config));

                Rect bounds = StationFootprint(starting[i].Kind, position, false);
                if (!Contains(WorkBounds, bounds))
                    throw new ArgumentException("Starting station is outside the work area for " + config.LevelId + " (" +
                        starting[i].Kind + ").", nameof(config));
                for (int existing = 0; existing < placedStations.Count; existing++)
                {
                    // The pre-installed Boca kitchen uses a tightly packed countertop row.
                    // Keep real footprints disjoint, but don't apply the extra build clearance
                    // intended for player-placed furniture in other construction levels.
                    Rect occupied = IsBocaKitchenLevel ? bounds
                        : Expand(bounds, ConstructionFurnitureClearanceLogical * worldUnitsPerLogicalPixel);
                    if (Overlaps(occupied, placedStations[existing].Bounds))
                        throw new ArgumentException("Starting stations overlap for " + config.LevelId + " (" +
                            starting[i].Kind + ").", nameof(config));
                }

                // Authored opening stations are installed, not bought. Resolve access only after the
                // complete furniture layout exists so an early entry cannot block a later station.
                placedStations.Add(new TowerDefensePlacedStation(nextStationId++, starting[i].Kind, bounds,
                    new Vector2(float.NaN, float.NaN), StationPickupPosition(starting[i].Kind, bounds)));
            }

            // Resolve opening access points after all furniture bounds exist. A role may share
            // an access point across its paired stations (Coca/beer, normal/premium tables),
            // so one worker does not need an impossible seventh idle slot in the narrow work area.
            for (int i = 0; i < placedStations.Count; i++)
            {
                TowerDefensePlacedStation current = placedStations[i];
                Vector2 access = new Vector2(float.NaN, float.NaN);
                StreetWorkerRole accessRole = StationWorkerRole(current.Kind);
                TowerDefenseStartingStation authored = starting[i];
                if (IsBocaKitchenLevel)
                {
                    // Boca's StreetKitchenLayout decides where each role works: cooks on the grill's
                    // table-side edge, Mozo/Fernetero behind their table, Cocacolero at the barrel back edge.
                    Vector2 bocaAccess = BocaStationAccess(current);
                    if (!IsStationAccessClear(current.Kind, bocaAccess, current.Bounds, Rect.zero))
                    {
                        Rect body = WorkerBounds(bocaAccess, StationWorkerRole(current.Kind)), feet = WorkerFeet(bocaAccess);
                        string hit = "";
                        foreach (var other in placedStations)
                            if (Overlaps(feet, StationSolidRect(other.Kind, other.Bounds))) hit += " feetHit=" + other.Kind;
                        throw new ArgumentException("Boca-derived starting-station access is blocked for " + current.Kind + " at " + bocaAccess +
                            " insideWork=" + Contains(WorkBounds, body) + " work=" + WorkBounds + " body=" + body + hit + ".", nameof(config));
                    }
                    placedStations[i] = new TowerDefensePlacedStation(current.Id, current.Kind, current.Bounds,
                        bocaAccess, current.PickupPosition);
                    continue;
                }
                Vector2 authoredAccess = authored.HasAccessPosition
                    ? WorkBounds.min + authored.AccessPositionFromWorkBoundsMin * worldUnitsPerLogicalPixel
                    : new Vector2(float.NaN, float.NaN);
                for (int prior = 0; prior < i; prior++)
                {
                    TowerDefensePlacedStation existing = placedStations[prior];
                    if (StationWorkerRole(existing.Kind) != accessRole) continue;
                    access = existing.WorkPosition;
                    if (authored.HasAccessPosition)
                    {
                        if (Vector2.Distance(authoredAccess, access) > worldUnitsPerLogicalPixel * .1f)
                            throw new ArgumentException("Stations operated by the same role must share one authored access point for " +
                                current.Kind + ".", nameof(config));
                    }
                    break;
                }
                if (!IsFinite(access.x) || !IsFinite(access.y))
                {
                    if (authored.HasAccessPosition)
                    {
                        access = authoredAccess;
                        if (!IsStationAccessClear(current.Kind, access, current.Bounds, Rect.zero))
                            throw new ArgumentException("Authored starting-station access is blocked for " + current.Kind + ".", nameof(config));
                    }
                    else
                    {
                        access = FindStationAccessAnchor(current.Kind, current.Bounds);
                    }
                }
                if (!IsFinite(access.x) || !IsFinite(access.y))
                    throw new ArgumentException("Starting station has no reachable worker access for " + current.Kind + ".", nameof(config));
                placedStations[i] = new TowerDefensePlacedStation(current.Id, current.Kind, current.Bounds,
                    access, current.PickupPosition);
            }

            // Check the real production handoffs with the worker envelopes that use them. Not
            // every role needs a separate idle point at every station: the cook hands food to a
            // table access point that the Mozo also uses, and drink stations share one Cocacolero.
            TowerDefensePlacedStation normalGrill = FindPlacedStation(TowerDefenseStationKind.NormalGrill);
            TowerDefensePlacedStation normalTable = FindPlacedStation(TowerDefenseStationKind.NormalTable);
            TowerDefensePlacedStation premiumGrill = FindPlacedStation(TowerDefenseStationKind.PremiumGrill);
            TowerDefensePlacedStation premiumTable = FindPlacedStation(TowerDefenseStationKind.PremiumTable);
            TowerDefensePlacedStation cocaBarrel = FindPlacedStation(TowerDefenseStationKind.CocaBarrel);
            TowerDefensePlacedStation beerBarrel = FindPlacedStation(TowerDefenseStationKind.BeerBarrel);
            // Free-construction configs may deliberately start without a complete kitchen.
            // Validate a production handoff only when both ends of that route are authored.
            Vector2 HandoffTarget(TowerDefensePlacedStation table) =>
                IsBocaKitchenLevel ? TableDepositPosition(table, 0) : table.WorkPosition;
            if (normalGrill != null && normalTable != null &&
                    !TryBuildWorkerPath(null, normalGrill.WorkPosition, HandoffTarget(normalTable), Rect.zero, out _, StreetWorkerRole.Parrillero) ||
                premiumGrill != null && premiumTable != null &&
                    !TryBuildWorkerPath(null, premiumGrill.WorkPosition, HandoffTarget(premiumTable), Rect.zero, out _, StreetWorkerRole.ParrilleroPremium) ||
                cocaBarrel != null && beerBarrel != null &&
                    !TryBuildWorkerPath(null, cocaBarrel.WorkPosition, beerBarrel.WorkPosition, Rect.zero, out _, StreetWorkerRole.Cocacolero))
                throw new ArgumentException("Starting stations are not connected by their role's worker path for " + config.LevelId + "." +
                    (normalGrill != null && normalTable != null ? " grillWork=" + normalGrill.WorkPosition + " tableDeposit=" + HandoffTarget(normalTable) +
                        " grillBounds=" + normalGrill.Bounds + " tableBounds=" + normalTable.Bounds + " work=" + WorkBounds + " area=" + WorkerArea() +
                        " startClear=" + FreePositionClear(normalGrill.WorkPosition, StreetWorkerRole.Parrillero, null, Rect.zero, false) +
                        " targetClear=" + FreePositionClear(HandoffTarget(normalTable), StreetWorkerRole.Parrillero, null, Rect.zero, false) +
                        " startBody=" + WorkerBounds(normalGrill.WorkPosition) + " targetBody=" + WorkerBounds(HandoffTarget(normalTable)) +
                        " startFeet=" + WorkerFeet(normalGrill.WorkPosition) + " grillSolid=" + StationSolidRect(normalGrill.Kind, normalGrill.Bounds) +
                        " tableSolid=" + StationSolidRect(normalTable.Kind, normalTable.Bounds) : ""), nameof(config));
        }

        // After a viewport change the whole kitchen is mapped again from Boca's layout (the new screen height changes
        // Boca's vertical stretch), instead of just scaling the old positions.
        private void ReflowBocaKitchen()
        {
            for (int i = 0; i < placedStations.Count; i++)
            {
                TowerDefensePlacedStation old = placedStations[i];
                Rect bounds = StationFootprint(old.Kind, BocaStationCenter(old.Kind), false);
                var updated = new TowerDefensePlacedStation(old.Id, old.Kind, bounds, new Vector2(float.NaN, float.NaN),
                    StationPickupPosition(old.Kind, bounds));
                placedStations[i] = updated;
                placedStations[i] = new TowerDefensePlacedStation(old.Id, old.Kind, bounds, BocaStationAccess(updated), updated.PickupPosition);
            }
        }

        private void AssignAllRolePosts()
        {
            foreach (StreetWorkerRole role in Enum.GetValues(typeof(StreetWorkerRole))) AssignRolePosts(role, true);
        }

        // Luján copies Boca's kitchen and the horizontal Camioneros copies Nueva Chicago's: both derive their furniture,
        // worker posts, footer and work area from a campaign kitchen layout.
        private bool IsBocaKitchenLevel => config.SourceCampaignLevelId == TowerDefenseLevelConfig.PeregrinacionLujanCampaignLevelId
            || IsPassiveKitchenLevel || IsCShapedLevel;
        // La Renga: Huracán's kitchen inside the pocket of a "C"-shaped fence; the fans walk around the outside of it.
        internal bool IsCShapedLevel => config.FreeConstruction && config.RouteKind == TowerDefenseRouteKind.CShapedSingleFence;
        // Kitchen-derived horizontal levels that copy a traditional (passive) campaign kitchen: Camioneros (Nueva Chicago's)
        // and Los Redondos (Ferro's), plus La Renga on its C-shaped fence (Huracán's kitchen layout, the same two products as
        // Camioneros). Nobody cooks, the tables are always full and each worker only serves.
        internal bool IsPassiveKitchenLevel => (config.SourceCampaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId ||
            config.SourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId ||
            config.SourceCampaignLevelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId)
            && config.FreeConstruction && !config.ActiveKitchen && (config.RouteKind == TowerDefenseRouteKind.HorizontalSingleFence || IsCShapedLevel);

        private Vector2 BocaStationAccess(TowerDefensePlacedStation station)
        {
            if (IsCShapedLevel && (station.Kind == TowerDefenseStationKind.NormalTable || station.Kind == TowerDefenseStationKind.PremiumTable ||
                                   station.Kind == TowerDefenseStationKind.FernetTable))
            {
                // As in the campaign kitchens, sandwiches are taken from the far side of the table (the grill is on the near
                // side): the worker stands just behind the solid table, which hides the legs, and reaches over it.
                var behind = new Vector2(station.Bounds.center.x, station.Bounds.yMax + TableBackPickupClearanceLogical * worldUnitsPerLogicalPixel);
                return ClampPostInsideWork(behind, StreetWorkerRole.Parrillero);
            }
            StreetKitchenLayout layout = BocaKitchenLayout;
            Vector2 boca;
            switch (station.Kind)
            {
                case TowerDefenseStationKind.NormalGrill: boca = layout.GrillWorkPosition(0, 0, 1); break;
                case TowerDefenseStationKind.PremiumGrill: boca = layout.GrillWorkPosition(2, 0, 1); break;
                case TowerDefenseStationKind.NormalTable: boca = layout.PickupPosition(0); break;
                case TowerDefenseStationKind.PremiumTable: boca = layout.PickupPosition(2); break;
                case TowerDefenseStationKind.FernetTable: boca = layout.PickupPosition(5); break;
                case TowerDefenseStationKind.CocaBarrel: boca = layout.PickupPosition(4); break;
                case TowerDefenseStationKind.BeerBarrel: boca = layout.PickupPosition(6); break;
                default: return new Vector2(float.NaN, float.NaN);
            }
            return ClampPostInsideWork(BocaPointToWorld(boca), StationWorkerRole(station.Kind));
        }

        // Boca draws its y coordinates stretched by the screen's vertical scale (positions move, sizes do not).
        // Luján maps Boca's layout with the same rule so every piece sits where it sits in Boca on the same screen.
        private float BocaVerticalScale => route.KitchenVerticalScale > .01f ? route.KitchenVerticalScale : 1f;

        // Bottom edge (logical px, y down) of the lowest piece of a campaign kitchen for a given vertical scale: the grills.
        internal static float BocaStationsBottomLogical(StreetKitchenLayout layout, float verticalScale)
        {
            float bottom = float.NegativeInfinity;
            foreach (Rect station in KitchenPieces(layout))
                bottom = Mathf.Max(bottom, station.yMax * verticalScale);
            return bottom;
        }

        private static Rect[] KitchenPieces(StreetKitchenLayout layout)
        {
            var pieces = new List<Rect>();
            Rect[] all =
            {
                layout.NormalGrillBounds, layout.PremiumGrillBounds, layout.NormalTableBounds, layout.PremiumTableBounds,
                layout.FernetTableBounds, layout.CocaBarrelBounds, layout.BeerBarrelBounds
            };
            for (int i = 0; i < all.Length; i++) if (all[i].width > 0f && all[i].height > 0f) pieces.Add(all[i]);
            return pieces.ToArray();
        }

        // Nueva Chicago's worker timing (StreetSimulation): walking speed and the grab / hand-over dwell scale with the
        // level's speed upgrade, the grab and the hand-over each last pickupSeconds / workRate (0.28 s at base speed),
        // nothing waits between deliveries and the customer takes 0.3 s to receive. Luján keeps its own timing.
        private const float ChicagoReceiveSeconds = .3f;
        private float ChicagoWorkRate(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium
                ? 1f + speedLevel * .06f : 1f + speedLevel * Mathf.Max(.01f, sourceBalance.speedIncrease);
        private float FreeWorkerSpeed(StreetWorkerRole role) =>
            Mathf.Max(0f, sourceBalance.workerSpeed) * worldUnitsPerLogicalPixel * (IsPassiveKitchenLevel ? ChicagoWorkRate(role) : 1f);
        internal float ChicagoDwellSeconds(StreetWorkerRole role) =>
            sourceBalance.pickupSeconds / Mathf.Max(.01f, ChicagoWorkRate(role));
        /// <summary>How long a delivered product takes to reach the customer (the hand-over cue).</summary>
        public float DeliveryCueSeconds => IsPassiveKitchenLevel ? ChicagoReceiveSeconds : DeliveryCueDurationSeconds;

        // A point in Boca's layout coordinates (y down, stretched by the vertical scale) -> world space.
        private Vector2 BocaPointToWorld(Vector2 boca)
        {
            float scale = BocaKitchenFitScale() * worldUnitsPerLogicalPixel;
            float bottom = BocaStationsBottomLogical(BocaKitchenLayout, BocaVerticalScale);
            return new Vector2(KitchenWorldX(boca.x, scale),
                KitchenWorldBottomY(scale, bottom) + (bottom - boca.y * BocaVerticalScale) * scale);
        }

        // The campaign kitchens span the work area from side to side, centered on it. La Renga's pocket is wider and taller than
        // Huracán's kitchen: the kitchen keeps its exact size and distances and sits in the middle of the pocket.
        private float KitchenWorldX(float bocaX, float scale) => IsCShapedLevel
            // La Renga's kitchen is anchored to the left of the pocket, CShapedKitchenSideMarginLogical from its edge.
            // 2026-11-25 (a pedido): Los Redondos' world is drawn mirrored, so its kitchen is laid out reflected: on screen the grills and tables keep
            // La Renga's order (normal left, premium right). The three tables go against the screen's right margin and the barrels stand to their left (the layout puts the
            // barrels first), which leaves the free lane on the fence side. The world is mirrored, so the screen's right margin is the world's left one.
            ? ReflectKitchen
                ? WorkBounds.xMin + CShapedKitchenSideMarginLogical * worldUnitsPerLogicalPixel + (BocaKitchenSourceBounds().xMax - bocaX) * scale
                : WorkBounds.xMin + CShapedKitchenLeftLogical * worldUnitsPerLogicalPixel + (bocaX - BocaKitchenSourceBounds().xMin) * scale
            : WorkBounds.center.x + (bocaX - StreetSceneLayout.Width * .5f) * scale;

        /// <summary>The kitchen's left-right order is reversed in world space (the mirrored world flips it back on screen).</summary>
        internal bool ReflectKitchen => config.MirrorHorizontally && IsCShapedLevel;
        private float KitchenWorldBottomY(float scale, float bottomLogical)
        {
            if (!IsCShapedLevel) return WorkBounds.yMin;
            Rect source = BocaKitchenSourceBounds();
            float height = (bottomLogical - source.yMin * BocaVerticalScale) * scale;
            // Centered, unless the pocket is so short that the Cocacolero's head would leave the worker area over the top rail.
            float free = WorkBounds.height - height;
            float headroom = Mathf.Max(0f, ConstructionWorkerHeightLogical - ServiceLeanLogical) * worldUnitsPerLogicalPixel;
            // The beer barrel stands on the Coca barrel and reaches high: the kitchen sits lower so a worker (feet over the barrel, head
            // inside the lean allowance) can still walk over it between the top lane and the right side.
            return WorkBounds.yMin + Mathf.Max(0f, Mathf.Min(free * .5f, free - headroom) - CShapedKitchenDropLogical * worldUnitsPerLogicalPixel);
        }

        // A Boca piece -> world space rectangle. Boca stretches its kitchen art vertically with the screen height
        // (centers and heights alike), so the piece spans y*vs .. (y+h)*vs.
        private Rect BocaRectToWorld(Rect boca)
        {
            float scale = BocaKitchenFitScale() * worldUnitsPerLogicalPixel;
            float bottom = BocaStationsBottomLogical(BocaKitchenLayout, BocaVerticalScale);
            float x = KitchenWorldX(ReflectKitchen ? boca.xMax : boca.xMin, scale);
            float y = KitchenWorldBottomY(scale, bottom) + (bottom - boca.yMax * BocaVerticalScale) * scale;
            return new Rect(x, y, boca.width * scale, boca.height * BocaVerticalScale * scale);
        }

        /// <summary>Boca's vertical stretch (screen height / 960) used by Luján's kitchen art.</summary>
        public float KitchenVerticalScale => BocaVerticalScale;

        private Vector2 BocaStationCenter(TowerDefenseStationKind kind)
        {
            Rect boca = BocaKitchenSourceBounds(kind);
            return boca.width <= 0f ? new Vector2(float.NaN, float.NaN) : BocaRectToWorld(boca).center;
        }

        private Vector2 ClampPostInsideWork(Vector2 post, StreetWorkerRole role)
        {
            float half = (role == StreetWorkerRole.Cocacolero ? 94f : ConstructionWorkerWidthLogical) * .5f * worldUnitsPerLogicalPixel;
            post.x = Mathf.Clamp(post.x, WorkBounds.xMin + half, WorkBounds.xMax - half);
            return post;
        }

        // Cooks hand sandwiches over from the apron below the table with Boca's DepositPosition(product, index, 4) rule:
        // the slot never depends on how many cooks there are. Two columns around the table center and the third and
        // fourth cook one row further down. Boca spaces them 24 px / 30 px for a 60 px body; Luján draws a larger body,
        // so the same proportion of the body is kept (otherwise the two cooks overlapped).
        private Vector2 TableDepositPosition(TowerDefensePlacedStation table, int index)
        {
            Rect source = BocaKitchenSourceBounds(table.Kind);
            float bodyScale = ConstructionWorkerWidthLogical / BocaBodyWidthPx;
            index = Mathf.Clamp(index, 0, BocaPostSlots - 1);
            int column = index % 2, row = index / 2;
            var boca = new Vector2(source.center.x + (column == 0 ? -24f : 24f) * bodyScale, source.yMax + 14f + row * 30f * bodyScale);
            // 2026-11-25: only where the grill is stacked right under the table (La Renga's kitchen). Los Redondos' active kitchen keeps the campaign's 70 px
            // between table and grill, so its cooks walk up to the table's front edge (Boca's apron) to hand the sandwich over instead of "delivering" it
            // 16 px sideways from their own post, 100 px away from the table.
            if (IsCShapedLevel && !config.ActiveKitchen)
            {
                // The apron below the table is the grill itself here, so the cooks hand over from the strip between the rows:
                // two beside each other and a third in the middle.
                float stripY = BocaKitchenLayout.GrillWorkPosition(table.Kind == TowerDefenseStationKind.PremiumTable ? 2 : 0, 0, 1).y;
                boca = new Vector2(source.center.x + (index == 0 ? -24f : index == 1 ? 24f : 0f) * bodyScale, stripY);
            }
            // A table at the screen edge would push the outer slot out of the work area, where no path reaches it.
            return ClampPostInsideWork(BocaPointToWorld(boca), StreetWorkerRole.Parrillero);
        }

        // Team members of one role stand where Boca's GrillWorkPosition / PickupPosition put them.
        private Vector2 RolePost(StreetWorkerRole role, int index, int count)
        {
            if (RoleStation(role) == null) return new Vector2(float.NaN, float.NaN);
            count = Mathf.Clamp(count, 1, BocaPostSlots);
            index = Mathf.Clamp(index, 0, count - 1);
            StreetKitchenLayout layout = BocaKitchenLayout;
            Vector2 boca;
            switch (role)
            {
                case StreetWorkerRole.Parrillero: boca = layout.GrillWorkPosition(0, index, count); break;
                case StreetWorkerRole.ParrilleroPremium: boca = layout.GrillWorkPosition(2, index, count); break;
                case StreetWorkerRole.Cocacolero: boca = layout.PickupPosition(4, index, count); break;
                case StreetWorkerRole.Fernetero: boca = layout.PickupPosition(5, index, count); break;
                case StreetWorkerRole.Mozo: boca = layout.PickupPosition(HasStation(TowerDefenseStationKind.NormalTable) ? 0 : 2, index, count); break;
                default: return new Vector2(float.NaN, float.NaN);
            }
            if (CooksAtVisibleGrillBack && IsCookRole(role)) boca.y += layout.GrillVisibleTopInset(role == StreetWorkerRole.Parrillero ? 0 : 2);
            return ClampPostInsideWork(BocaPointToWorld(boca), role);
        }

        // 2026-11-26 (a pedido): Los Redondos' cooks stand right against the back of their own grill. The narrow grill art leaves a transparent band over
        // the grate (GrillVisibleTopInset); their post goes down by that band and the band is not solid (StationSolidRect), so the feet meet the grate.
        // The grill's access point (and the path lattice aligned to it) does not move. La Renga's passive kitchen keeps its 4 px strip.
        private bool CooksAtVisibleGrillBack => IsCShapedLevel && config.ActiveKitchen;

        private void AssignRolePosts(StreetWorkerRole role, bool snap)
        {
            int count = 0;
            for (int i = 0; i < workers.Count; i++) if (workers[i].Role == role) count++;
            int index = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != role) continue;
                Vector2 post = RolePost(role, index++, count);
                if (!IsFinite(post.x) || !IsFinite(post.y)) continue;
                worker.PostPosition = post;
                if (snap) worker.Position = post;
                else if ((!IsCookRole(role) || IsPassiveKitchenLevel) && !worker.IsMoving && worker.ServiceStage == 0) QueueWorkerToPost(worker);
            }
        }

        private static bool IsCookRole(StreetWorkerRole role) =>
            role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium;

        private Rect WorkerArea()
        {
            if (!IsBocaKitchenLevel) return WorkBounds;
            Rect area = WorkBounds;
            return new Rect(area.xMin, area.yMin, area.width, area.height + ServiceLeanLogical * worldUnitsPerLogicalPixel);
        }

        // Nueva Chicago stacks its grill 4 px under its table, so a cook stands on that thin strip: its feet are shallower there.
        // When the C-shaped pocket shrinks the kitchen to fit (Ferro is wider than the pocket), that strip shrinks with it,
        // so the feet depth shrinks at least as fast to keep the cook's feet off the barrel base above it.
        // 2026-11-24: this was recomputed (rebuilding the whole kitchen's pieces twice) on every WorkerFeet call, and a single path-clearance test calls it ~26 times
        // while a path search makes hundreds of such tests: the late-match stutter. It only depends on the screen scale and the work area, so it is kept.
        private float footDepthCache = float.NaN, footDepthPixel;
        private Rect footDepthBounds;
        private float FootDepthLogical
        {
            get
            {
                if (!IsPassiveKitchenLevel) return BocaFootDepthLogical;
                if (!IsCShapedLevel) return ChicagoFootDepthLogical;
                if (!float.IsNaN(footDepthCache) && footDepthPixel == worldUnitsPerLogicalPixel && footDepthBounds == WorkBounds) return footDepthCache;
                float fit = BocaKitchenFitScale();
                footDepthCache = ChicagoFootDepthLogical * fit * fit;
                footDepthPixel = worldUnitsPerLogicalPixel; footDepthBounds = WorkBounds;
                return footDepthCache;
            }
        }
        private const float ChicagoFootDepthLogical = 2f, ChicagoGrillOpenBackLogical = 30f;

        // Boca collision: only the feet touch the solid base (table front strip, barrel, grill).
        private Rect WorkerFeet(Vector2 position)
        {
            float pixel = worldUnitsPerLogicalPixel, epsilon = .05f * pixel;
            float half = FootHalfWidthLogical;
            return new Rect(position.x - half * pixel + epsilon, position.y + epsilon,
                half * 2f * pixel - epsilon * 2f, FootDepthLogical * pixel - epsilon * 2f);
        }

        private Rect StationSolidRect(TowerDefenseStationKind kind, Rect bounds)
        {
            // La Renga: nobody walks over anything in the kitchen. Grills, tables and barrels are solid for the feet over their whole
            // footprint; the cooks work from the thin strip between the table row and the grill row.
            if (CooksAtVisibleGrillBack && (kind == TowerDefenseStationKind.NormalGrill || kind == TowerDefenseStationKind.PremiumGrill))
            {
                float band = BocaKitchenLayout.GrillVisibleTopInset(kind == TowerDefenseStationKind.NormalGrill ? 0 : 2) *
                    BocaKitchenFitScale() * BocaVerticalScale * worldUnitsPerLogicalPixel;
                return new Rect(bounds.xMin, bounds.yMin, bounds.width, Mathf.Max(0f, bounds.height - band));
            }
            if (IsCShapedLevel) return bounds;
            if (IsPassiveKitchenLevel && (kind == TowerDefenseStationKind.NormalGrill || kind == TowerDefenseStationKind.PremiumGrill))
            {
                // Chicago's cooks hand over and stand over the back of the grill: only its front is solid.
                float open = Mathf.Min(bounds.height * .5f, ChicagoGrillOpenBackLogical * BocaKitchenFitScale() * BocaVerticalScale * worldUnitsPerLogicalPixel);
                return new Rect(bounds.xMin, bounds.yMin, bounds.width, bounds.height - open);
            }
            // Ferro's tables stand almost wall to wall (27 px gaps) and the traditional workers reach the pickup point inside the
            // table's own rows: tables are not an obstacle for the passive kitchen's crew.
            if (IsPassiveKitchenLevel && (kind == TowerDefenseStationKind.NormalTable || kind == TowerDefenseStationKind.PremiumTable))
                return Rect.zero;
            if (!IsBocaKitchenLevel || kind != TowerDefenseStationKind.NormalTable && kind != TowerDefenseStationKind.PremiumTable &&
                kind != TowerDefenseStationKind.FernetTable) return bounds;
            float depth = Mathf.Min(bounds.height, BocaTableBaseDepthLogical * BocaKitchenFitScale() * BocaVerticalScale * worldUnitsPerLogicalPixel);
            return new Rect(bounds.xMin, bounds.yMin, bounds.width, depth);
        }

        private bool WorkerHitsStation(Vector2 position, Rect body, TowerDefensePlacedStation station) =>
            IsBocaKitchenLevel ? Overlaps(WorkerFeet(position), StationSolidRect(station.Kind, station.Bounds))
                : Overlaps(body, station.Bounds);

        private Rect BocaKitchenSourceBounds()
        {
            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity, yMax = float.NegativeInfinity;
            foreach (Rect piece in KitchenPieces(BocaKitchenLayout))
            {
                xMin = Mathf.Min(xMin, piece.xMin);
                yMin = Mathf.Min(yMin, piece.yMin);
                xMax = Mathf.Max(xMax, piece.xMax);
                yMax = Mathf.Max(yMax, piece.yMax);
            }
            // Barrels first (Los Redondos): the Cocacolero's side lane is on the barrels' left, so the span includes it (otherwise the post would be pushed into the barrel).
            if (BocaKitchenLayout.BarrelsBeforeTables && BocaKitchenLayout.HasCocaBarrel)
                xMin = Mathf.Min(xMin, BocaKitchenLayout.CocaBarrelBounds.xMin - StreetKitchenLayout.NarrowBarrelSideStandoff - ConstructionWorkerWidthLogical * .5f);
            // La Renga's narrow layout keeps the span it had before the beer barrel went on top of the Coca barrel.
            if (BocaKitchenLayout.KitchenSpanTop > 0f) yMin = Mathf.Max(yMin, BocaKitchenLayout.KitchenSpanTop);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        internal Rect BocaKitchenSourceBounds(TowerDefenseStationKind kind)
        {
            switch (kind)
            {
                case TowerDefenseStationKind.NormalGrill: return BocaKitchenLayout.NormalGrillBounds;
                case TowerDefenseStationKind.PremiumGrill: return BocaKitchenLayout.PremiumGrillBounds;
                case TowerDefenseStationKind.NormalTable: return BocaKitchenLayout.NormalTableBounds;
                case TowerDefenseStationKind.PremiumTable: return BocaKitchenLayout.PremiumTableBounds;
                case TowerDefenseStationKind.FernetTable: return BocaKitchenLayout.FernetTableBounds;
                case TowerDefenseStationKind.CocaBarrel: return BocaKitchenLayout.CocaBarrelBounds;
                case TowerDefenseStationKind.BeerBarrel: return BocaKitchenLayout.BeerBarrelBounds;
                default: return Rect.zero;
            }
        }

        internal float BocaKitchenFitScale()
        {
            Rect source = BocaKitchenSourceBounds();
            float pixel = Mathf.Max(.0001f, worldUnitsPerLogicalPixel);
            float availableWidth = WorkBounds.width / pixel;
            // La Renga's counter would otherwise touch the screen edge and the right fence: keep a small margin on each side.
            if (IsCShapedLevel) availableWidth -= CShapedKitchenLeftLogical + CShapedKitchenSideMarginLogical;
            // Preserve one worker-height service lane behind the furniture before fitting
            // the same Boca station proportions into the smaller TD workfield.
            float availableHeight = WorkBounds.height / pixel - BocaKitchenWorkerLaneLogical;
            // La Renga's crew stands behind the furniture with their heads over the top rail (the worker area leans over it).
            if (IsCShapedLevel) availableHeight += ServiceLeanLogical;
            return Mathf.Min(1f, Mathf.Min(availableWidth / source.width, availableHeight / (source.height * BocaVerticalScale)));
        }

        public TowerDefensePlacementResult PreviewStationPlacement(TowerDefenseStationKind kind, Vector2 position)
        {
            Rect bounds = StationFootprint(kind, position);
            if (!config.FreeConstruction) return InvalidPlacement("Este nivel no habilita construcción libre.", bounds);
            if (!config.EquipmentPlacementAllowed)
                return InvalidPlacement("El equipamiento de este nivel ya está instalado y no se puede comprar ni colocar.", bounds);
            if (!Enum.IsDefined(typeof(TowerDefenseStationKind), kind)) return InvalidPlacement("Estación desconocida.", bounds);
            if (!IsFinite(position.x) || !IsFinite(position.y)) return InvalidPlacement("La posición no es válida.", bounds);
            if (!Contains(WorkBounds, bounds)) return InvalidPlacement("La estación debe quedar dentro del área libre, bajo la valla y fuera del HUD.", bounds);
            Rect padded = Expand(bounds, ConstructionFurnitureClearanceLogical * worldUnitsPerLogicalPixel);
            for (int i = 0; i < placedStations.Count; i++)
                if (Overlaps(padded, placedStations[i].Bounds)) return InvalidPlacement("La estación se superpone con otro mueble.", bounds);
            for (int i = 0; i < workers.Count; i++)
                if (Overlaps(padded, WorkerBounds(workers[i].Position, workers[i].Role))) return InvalidPlacement("La estación se superpone con un trabajador.", bounds);
            if (!HasConnectedStationAccess(kind, bounds)) return InvalidPlacement("La estación debe conservar un paso transitable desde las otras instalaciones.", bounds);
            return new TowerDefensePlacementResult(true, string.Empty, bounds, bounds.center);
        }

        public bool TryPlaceStation(TowerDefenseStationKind kind, Vector2 position, out TowerDefensePlacedStation placed)
        {
            placed = null;
            if (Phase != TowerDefenseRunPhase.Playing || !config.EquipmentPlacementAllowed) return false;
            TowerDefensePlacementResult preview = PreviewStationPlacement(kind, position);
            if (!preview.IsValid) return false;
            int cost = StationCost(kind);
            if (cost < 0 || coins < cost) return false;
            coins -= cost;
            if (cost > 0)
                equipmentSpendCents = AddCents(equipmentSpendCents, (long)cost * 100L);
            Rect bounds = preview.Bounds;
            placed = CreatePlacedStation(kind, bounds);
            placedStations.Add(placed);
            // A new obstacle can change a previous station's accessible side.
            for (int i=0;i<placedStations.Count;i++)
            {
                var old=placedStations[i]; Vector2 access=FindStationAccessAnchor(old.Kind,old.Bounds);
                placedStations[i]=new TowerDefensePlacedStation(old.Id,old.Kind,old.Bounds,access,old.PickupPosition);
            }
            placed=placedStations[placedStations.Count-1];
            return true;
        }

        private TowerDefensePlacedStation CreatePlacedStation(TowerDefenseStationKind kind, Rect bounds)
        {
            Vector2 work = FindStationAccessAnchor(kind, bounds);
            return new TowerDefensePlacedStation(nextStationId++, kind, bounds, work, StationPickupPosition(kind, bounds));
        }

        private Vector2 StationPickupPosition(TowerDefenseStationKind kind, Rect bounds) =>
            kind == TowerDefenseStationKind.CocaBarrel || kind == TowerDefenseStationKind.BeerBarrel
                ? new Vector2(bounds.center.x, bounds.yMin)
                : new Vector2(bounds.center.x, bounds.yMin - 18f * worldUnitsPerLogicalPixel);

        private static StreetWorkerRole StationWorkerRole(TowerDefenseStationKind kind)
        {
            switch (kind)
            {
                case TowerDefenseStationKind.NormalGrill: return StreetWorkerRole.Parrillero;
                case TowerDefenseStationKind.PremiumGrill: return StreetWorkerRole.ParrilleroPremium;
                case TowerDefenseStationKind.CocaBarrel:
                case TowerDefenseStationKind.BeerBarrel: return StreetWorkerRole.Cocacolero;
                case TowerDefenseStationKind.FernetTable: return StreetWorkerRole.Fernetero;
                case TowerDefenseStationKind.NormalTable:
                case TowerDefenseStationKind.PremiumTable: return StreetWorkerRole.Mozo;
                default: return StreetWorkerRole.Parrillero;
            }
        }

        public TowerDefensePlacementResult PreviewWorkerPlacement(StreetWorkerRole role, Vector2 position)
        {
            position = Snap(position);
            Rect bounds = WorkerBounds(position, role);
            if (!config.FreeConstruction) return InvalidPlacement("Este nivel no habilita contratación por posición.", bounds);
            if (!IsPlaceableRole(role) || !IsRoleAvailable(role)) return InvalidPlacement("El rol no está disponible en este nivel.", bounds);
            if (!Contains(WorkBounds, bounds)) return InvalidPlacement("El trabajador debe permanecer debajo de la valla y fuera del HUD.", bounds);
            if (!CanPlaceWorker(role, 0)) return InvalidPlacement("Falta una estación, monedas o cupo para este rol.", bounds);
            for (int i = 0; i < workers.Count; i++)
                if (Overlaps(Expand(bounds, 4f * worldUnitsPerLogicalPixel), WorkerBounds(workers[i].Position, workers[i].Role)))
                    return InvalidPlacement("Los trabajadores no pueden superponerse.", bounds);
            for (int i = 0; i < placedStations.Count; i++)
                if (Overlaps(bounds, placedStations[i].Bounds)) return InvalidPlacement("El trabajador se superpone con un mueble.", bounds);
            if (!TryBuildWorkerPath(null, position, RoleStationAccess(role), Rect.zero, out _, role))
                return InvalidPlacement("No hay un recorrido libre hasta la estación asignada.", bounds);
            return new TowerDefensePlacementResult(true, string.Empty, bounds, position);
        }

        public bool CanHireAt(StreetWorkerRole role, Vector2 position) => PreviewWorkerPlacement(role, position).IsValid;

        public bool TryHireAt(StreetWorkerRole role, Vector2 position)
        {
            if (Phase != TowerDefenseRunPhase.Playing || !CanHireAt(role, position)) return false;
            int roleIndex = (int)role;
            int cost = HireCost(role);
            if (roleIndex < 0 || roleIndex >= workerCounts.Length || cost <= 0 || coins < cost) return false;
            ChargeHire(roleIndex, cost);
            AddWorker(role, 0);
            TowerDefenseWorkerState worker = workers[workers.Count - 1];
            worker.Position = Snap(position);
            if (!QueueWorkerToStation(worker))
            {
                workers.RemoveAt(workers.Count - 1);
                workerCounts[roleIndex]--;
                coins += cost;
                hireSpendCents -= (long)cost * 100L;
                hiresByRole[roleIndex]--;
                return false;
            }
            return true;
        }

        private void ChargeHire(int roleIndex, int cost)
        {
            coins -= cost;
            hireSpendCents = AddCents(hireSpendCents, (long)cost * 100L);
            hiresByRole[roleIndex] = hiresByRole[roleIndex] == long.MaxValue ? long.MaxValue : hiresByRole[roleIndex] + 1L;
            workerCounts[roleIndex]++;
        }

        public bool CanHireAtPost(StreetWorkerRole role) =>
            config.FreeConstruction && Phase == TowerDefenseRunPhase.Playing && IsPlaceableRole(role) && IsRoleAvailable(role) &&
            CanPlaceWorker(role, 0) && RoleStation(role) != null;

        /// <summary>One-tap hire: the worker appears at its role's post and starts working, as in Boca.</summary>
        public bool TryHireAtPost(StreetWorkerRole role)
        {
            if (!CanHireAtPost(role)) return false;
            int roleIndex = (int)role;
            int cost = HireCost(role);
            if (roleIndex < 0 || roleIndex >= workerCounts.Length || cost <= 0 || coins < cost) return false;
            ChargeHire(roleIndex, cost);
            AddWorker(role, 0);
            if (IsBocaKitchenLevel) AssignRolePosts(role, false);
            if (UsesWaitingBand(workers[workers.Count - 1]))
            {
                // As in the campaign, a new hire appears at its own waiting spot.
                TowerDefenseWorkerState hired = workers[workers.Count - 1];
                Vector2 home = CampaignWaitSpot(hired);
                // 2026-11-24: ignore co-workers here (routes and walking do too): a spot briefly occupied by someone passing used to leave the hire at the
                // slot AddWorker gave it, inside the keep-out strip under the barrels, from where no route can be planned and it never received an order.
                if (FreePositionClear(home, hired.Role, hired, Rect.zero, false))
                {
                    hired.Position = home;
                    hired.IsMoving = false; hired.ReturningToPost = false;
                    hired.TransferRoute = Array.Empty<Vector2>(); hired.TransferIndex = 0;
                }
            }
            return true;
        }

        // Where an idle worker waits: the passive campaign-kitchen levels (Camioneros, Los Redondos, La Renga) wait like the
        // campaign (CampaignWaitSpot); every other level waits at the post.
        private Vector2 WaitSpot(TowerDefenseWorkerState worker) =>
            UsesWaitingBand(worker) ? CampaignWaitSpot(worker) : worker.PostPosition;

        // The passive crews wait in the open band between the fence and the tables. On the C-shaped fence the active kitchen's Mozos do too (2026-11-24):
        // their Boca post, in front of the tables, is inside the solid furniture of the C, from where no route can start.
        // 2026-11-25: the Fernetero too: its Boca post is the middle of the Fernet table, which is solid on the C, so it never left it (no route can start there).
        private bool UsesWaitingBand(TowerDefenseWorkerState worker) =>
            IsPassiveKitchenLevel || (IsCShapedLevel && (worker.Role == StreetWorkerRole.Mozo || worker.Role == StreetWorkerRole.Fernetero));

        // The passive Defense kitchens wait like the traditional passive kitchens (StreetSimulation.WorkerWaitPosition): each worker owns a fixed
        // spot by hire order, in rows along the service line next to the fence (the top run here, the counter there), each
        // row shifted half a column, and stands there until its next order. Columns and rows are one body apart so the
        // waiting crew never blocks itself; a spot on furniture is skipped (behind a table, the stacks hide the body, as in the campaign).
        private const float WaitingBandStackHeightLogical = 130f;
        private const float WaitingBandMarginFraction = .3f, WaitingBandColumnSpacing = .75f;
        private readonly System.Random waitRandom = new System.Random(1311);

        // 2026-11-24: a worker's waiting spot only depends on its place in the crew, the furniture, the screen scale and its random offset, so it is kept
        // instead of rebuilt (three lists and a scan of every column) for every idle worker on every simulation step and every frame of the dialogues.
        private readonly struct WaitSpotEntry
        {
            public readonly Vector2 Spot; public readonly int Order; public readonly long Signature; public readonly float Pixel, Jitter;
            public WaitSpotEntry(Vector2 spot, int order, long signature, float pixel, float jitter) { Spot = spot; Order = order; Signature = signature; Pixel = pixel; Jitter = jitter; }
        }
        private readonly Dictionary<int, WaitSpotEntry> waitSpotCache = new Dictionary<int, WaitSpotEntry>();

        internal Vector2 CampaignWaitSpot(TowerDefenseWorkerState worker)
        {
            int order = workers.IndexOf(worker);
            long signature = PathLatticeSignature();
            if (!float.IsNaN(worker.WaitJitter) && waitSpotCache.TryGetValue(worker.Id, out WaitSpotEntry cached) && cached.Order == order &&
                cached.Signature == signature && cached.Pixel == worldUnitsPerLogicalPixel && cached.Jitter == worker.WaitJitter) return cached.Spot;
            Vector2 spot = CampaignWaitSpotUncached(worker, order);
            waitSpotCache[worker.Id] = new WaitSpotEntry(spot, order, signature, worldUnitsPerLogicalPixel, worker.WaitJitter);
            return spot;
        }

        private Vector2 CampaignWaitSpotUncached(TowerDefenseWorkerState worker, int order)
        {
            if (order < 0 || routeGeometry.Centerline == null || routeGeometry.Centerline.Length == 0) return worker.PostPosition;
            float pixel = worldUnitsPerLogicalPixel;
            float column = CocacoleroVisualWidthLogical * pixel, row = ConstructionWorkerHeightLogical * pixel;
            float serviceY = GetFanServiceAnchor(routeGeometry.Centerline[0]).y;
            // La Renga's C (2026-11-23, a pedido): the crew waits in the open band between the fence and the sandwich tables, never beside
            // the tables or behind their stacks, so the band above each table (stack height) and everything below the tables' top edge is off limits.
            bool band = IsCShapedLevel;
            float tablesTop = float.NegativeInfinity;
            var stackZones = new List<Rect>();
            if (band)
                for (int i = 0; i < placedStations.Count; i++)
                {
                    TowerDefensePlacedStation station = placedStations[i];
                    if (station.Kind != TowerDefenseStationKind.NormalTable && station.Kind != TowerDefenseStationKind.PremiumTable) continue;
                    Rect b = station.Bounds;
                    tablesTop = Mathf.Max(tablesTop, b.yMax);
                    stackZones.Add(new Rect(b.xMin - column * .5f, b.yMin, b.width + column, b.height + WaitingBandStackHeightLogical * pixel));
                }
            if (band && serviceY - tablesTop > row * .5f)
            {
                // The crew waits halfway between the fence and the tables (a pedido), one column each so nobody overlaps; every stop lands
                // up to WaitingBandMarginFraction of that distance above or below the middle, at random. Columns are 3/4 of a body apart (shoulder to shoulder).
                float middle = (serviceY + tablesTop) * .5f, span = serviceY - tablesTop;
                if (float.IsNaN(worker.WaitJitter)) worker.WaitJitter = (float)(waitRandom.NextDouble() * 2.0 - 1.0);
                var openColumns = new List<Vector2>();
                var stackedColumns = new List<Vector2>();
                float step = column * WaitingBandColumnSpacing;
                for (int slot = 0; ; slot++)
                {
                    float x = WorkBounds.xMin + column * .5f + step * slot;
                    if (x > WorkBounds.xMax - column * .5f) break;
                    var spot = new Vector2(x, middle);
                    if (!FreePositionClear(spot, StreetWorkerRole.Cocacolero, worker, Rect.zero, false)) continue;
                    // Columns in front of a table's stack come last: there the body is partly hidden behind the sandwiches.
                    (stackZones.Exists(zone => zone.Contains(spot)) ? stackedColumns : openColumns).Add(spot);
                }
                openColumns.AddRange(stackedColumns);
                if (order < openColumns.Count)
                {
                    Vector2 chosen = openColumns[order];
                    var jittered = new Vector2(chosen.x, middle + worker.WaitJitter * WaitingBandMarginFraction * span);
                    return FreePositionClear(jittered, StreetWorkerRole.Cocacolero, worker, Rect.zero, false) ? jittered : chosen;
                }
                order -= openColumns.Count; // the rest wait in the old rows
            }
            int found = 0;
            for (int line = 0; line < 4; line++)
            {
                float y = serviceY - line * row;
                for (int slot = 0; ; slot++)
                {
                    float x = WorkBounds.xMin + column * (.5f + slot + ((line & 1) == 1 ? .5f : 0f));
                    if (x > WorkBounds.xMax - column * .5f) break;
                    var spot = new Vector2(x, y);
                    if (!FreePositionClear(spot, StreetWorkerRole.Cocacolero, worker, Rect.zero, false)) continue;
                    if (found++ == order) return spot;
                }
            }
            return worker.PostPosition;
        }

        private void QueueWorkerToPost(TowerDefenseWorkerState worker)
        {
            if (!config.FreeConstruction || (IsCookRole(worker.Role) && !IsPassiveKitchenLevel) ||
                !IsFinite(worker.PostPosition.x) || !IsFinite(worker.PostPosition.y)) return;
            Vector2 spot = WaitSpot(worker);
            if (WorkerAt(worker, spot)) return;
            // 2026-11-24: an idle worker standing where no route can start (furniture or the barrel keep-out strip) would never get an order: put it on its spot.
            if (UsesWaitingBand(worker) && !worker.IsMoving && !FreePositionClear(worker.Position, worker.Role, worker, Rect.zero, false) &&
                FreePositionClear(spot, worker.Role, worker, Rect.zero, false)) { worker.Position = spot; return; }
            if (QueueFreeWorkerPath(worker, spot)) worker.ReturningToPost = worker.IsMoving;
        }

        private bool HasStationForRole(StreetWorkerRole role)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero: return HasStation(TowerDefenseStationKind.NormalGrill) && HasStation(TowerDefenseStationKind.NormalTable);
                case StreetWorkerRole.ParrilleroPremium: return HasStation(TowerDefenseStationKind.PremiumGrill) && HasStation(TowerDefenseStationKind.PremiumTable);
                case StreetWorkerRole.Cocacolero: return HasStation(TowerDefenseStationKind.CocaBarrel) && (HasStation(TowerDefenseStationKind.BeerBarrel) || !ContainsProduct(6));
                case StreetWorkerRole.Fernetero: return HasStation(TowerDefenseStationKind.FernetTable);
                case StreetWorkerRole.Mozo: return HasStation(TowerDefenseStationKind.NormalTable) || HasStation(TowerDefenseStationKind.PremiumTable);
                default: return false;
            }
        }

        private void AdvanceFreeConstructionCooks(float deltaTime, float cookRate)
        {
            // Nueva Chicago's passive kitchen: the table is always full, so nobody walks to the grill to cook.
            if (!Kitchen.ActiveCooks) { foreach (TowerDefenseWorkerState idle in workers) idle.IsCooking = false; return; }
            foreach (TowerDefenseWorkerState worker in workers)
            {
                worker.IsCooking = false;
                if (worker.IsMoving || (worker.Role != StreetWorkerRole.Parrillero && worker.Role != StreetWorkerRole.ParrilleroPremium)) continue;

                var grillKind = worker.Role == StreetWorkerRole.Parrillero ? TowerDefenseStationKind.NormalGrill : TowerDefenseStationKind.PremiumGrill;
                var tableKind = worker.Role == StreetWorkerRole.Parrillero ? TowerDefenseStationKind.NormalTable : TowerDefenseStationKind.PremiumTable;
                TowerDefensePlacedStation grill = FindPlacedStation(grillKind), table = FindPlacedStation(tableKind);
                if (grill == null || table == null) continue;
                StreetFoodUnit carried = null;
                foreach (var unit in Kitchen.Units) if (unit.Id == worker.CookUnitId) { carried = unit; break; }
                if (carried != null && carried.Location == StreetFoodLocation.Carried)
                {
                    // Keep the table's Mozo pickup point free while the cook carries a
                    // real sandwich to its own nearby handoff point.
                    Vector2 handoff = CookTableHandoffPosition(worker, table);
                    if (!IsFinite(handoff.x) || !IsFinite(handoff.y)) continue;
                    if (!WorkerAt(worker, handoff)) { worker.CookHandoffSeconds = -1f; QueueFreeWorkerPath(worker, handoff); continue; }
                    // The campaign cook stops at the table for pickupSeconds / work rate before putting the sandwich down (StreetWorkerState.Deposit).
                    // Only on the C-shaped fence (Los Redondos): Luján's active kitchen keeps its own timing.
                    if (worker.CookHandoffSeconds < 0f) worker.CookHandoffSeconds = IsCShapedLevel ? ChicagoDwellSeconds(worker.Role) : 0f;
                    worker.CookHandoffSeconds -= deltaTime;
                    if (worker.CookHandoffSeconds > 0f) continue;
                    worker.CookHandoffSeconds = -1f;
                    if (Kitchen.DepositCooked(carried.Id, worker.Id)) worker.CookUnitId = 0;
                    continue;
                }
                Vector2 post = IsFinite(worker.PostPosition.x) && IsFinite(worker.PostPosition.y) ? worker.PostPosition : grill.WorkPosition;
                if (!WorkerAt(worker, post)) { QueueFreeWorkerPath(worker, post); continue; }
                int first = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
                if (carried == null)
                {
                    worker.CookUnitId = 0;
                    int preferred = worker.NextCookProduct >= first && worker.NextCookProduct < first + 2 ? worker.NextCookProduct : first;
                    for (int offset = 0; offset < 2; offset++)
                    {
                        int product = first + (preferred - first + offset) % 2;
                        if (!ContainsProduct(product)) continue;
                        carried = Kitchen.ReserveCook(product, worker.Id);
                        if (carried == null) continue;
                        worker.CookUnitId = carried.Id; worker.NextCookProduct = product == first ? first + 1 : first;
                        break;
                    }
                }
                if (carried == null) continue;
                worker.IsCooking = true;
                if (Kitchen.AdvanceCook(carried.Id, worker.Id, deltaTime, cookRate) && Kitchen.TakeCooked(carried.Id, worker.Id))
                {
                    worker.IsCooking = false;
                    Vector2 handoff = CookTableHandoffPosition(worker, table);
                    if (IsFinite(handoff.x) && IsFinite(handoff.y)) QueueFreeWorkerPath(worker, handoff);
                }
            }
        }

        private Vector2 CookTableHandoffPosition(TowerDefenseWorkerState worker, TowerDefensePlacedStation table)
        {
            if (worker == null || table == null) return new Vector2(float.NaN, float.NaN);
            if (IsBocaKitchenLevel)
            {
                int index = 0, count = 0;
                for (int i = 0; i < workers.Count; i++)
                {
                    if (workers[i].Role != worker.Role) continue;
                    if (workers[i] == worker) index = count;
                    count++;
                }
                // A slot beside a table at the screen edge could leave the work area (the cook then never delivered and
                // stood idle holding the sandwich), so keep the whole body inside.
                return ClampPostInsideWork(TableDepositPosition(table, index), worker.Role);
            }
            TowerDefenseStationKind grillKind = worker.Role == StreetWorkerRole.Parrillero
                ? TowerDefenseStationKind.NormalGrill : TowerDefenseStationKind.PremiumGrill;
            TowerDefensePlacedStation grill = FindPlacedStation(grillKind);
            if (grill == null || !IsFinite(table.WorkPosition.x) || !IsFinite(grill.WorkPosition.x))
                return new Vector2(float.NaN, float.NaN);

            Vector2 direction = grill.WorkPosition - table.WorkPosition;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) direction = new Vector2(Mathf.Sign(direction.x), 0f);
            else direction = new Vector2(0f, Mathf.Sign(direction.y));
            if (direction.sqrMagnitude < .5f) direction = Vector2.right;

            float pixel = worldUnitsPerLogicalPixel;
            float halfWidth = (worker.Role == StreetWorkerRole.Cocacolero ? 94f : ConstructionWorkerWidthLogical) * .5f;
            float minimumOffset = halfWidth * 2f + 8f;
            // The closest clear spot is preferred, keeping the cook visibly at the table
            // edge while leaving its shared Mozo access unobstructed.
            for (float offset = minimumOffset; offset <= minimumOffset + 256f; offset += 16f)
            {
                Vector2 candidate = table.WorkPosition + direction * (offset * pixel);
                if (!Contains(WorkBounds, WorkerBounds(candidate, worker.Role))) continue;
                if (!FreePositionClear(candidate, worker.Role, worker, Rect.zero)) continue;
                if (!TryBuildWorkerPath(worker, worker.Position, candidate, Rect.zero, out _)) continue;
                return candidate;
            }
            return new Vector2(float.NaN, float.NaN);
        }

        private TowerDefensePlacedStation FindPlacedStation(TowerDefenseStationKind kind)
        { foreach (var station in placedStations) if (station.Kind == kind) return station; return null; }
        private bool WorkerAt(TowerDefenseWorkerState worker, Vector2 target) =>
            IsFinite(target.x) && IsFinite(target.y) && Vector2.Distance(worker.Position, target) <= worldUnitsPerLogicalPixel * .1f;
        private bool QueueFreeWorkerPath(TowerDefenseWorkerState worker, Vector2 target)
        {
            if (!TryBuildWorkerPath(worker, worker.Position, target, Rect.zero, out Vector2[] path)) return false;
            worker.TransferRoute = path; worker.TransferIndex = 0; worker.IsMoving = path.Length > 0; worker.TargetZone = 0;
            return true;
        }

        private void DeliverFreeConstructionOrders()
        {
            UpdateFreeConstructionFanInterceptions();
            // Same-role hold-back: a role never serves a pilgrim while one further ahead still waits for that role
            // (even without stock yet), so a worker does not jump the queue. Other roles keep working in parallel,
            // and a pilgrim nobody of the role can reach in time stops holding the line.
            var heldRoles = new HashSet<StreetWorkerRole>();
            for (int f = 0; f < serviceOrder.Count; f++)
            {
                TowerDefenseFanState fan = serviceOrder[f];
                if (fan.OrderComplete || fan.RouteFollower.HasExited || fan.ReservedByWorker != 0) continue;
                for (int line = 0; line < fan.Order.Count; line++)
                {
                    TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                    if (order.Remaining <= 0) continue;
                    StreetWorkerRole role = GetDeliveryRole(order.Product);
                    if (heldRoles.Contains(role)) continue;
                    if (order.Product < 4 && Kitchen.AvailableTableCount(order.Product) <= 0) continue;
                    TowerDefenseWorkerState worker = FindWorkerForFreeService(role, fan, order.Product);
                    if (worker == null || !HasStationForRole(role)) continue;
                    if (order.Product < 4 && Kitchen.ActiveCooks && Kitchen.ReserveTableUnit(order.Product, worker.Id) == null) continue;
                    worker.PendingFanId = fan.Id; worker.PendingProduct = order.Product; worker.ServiceStage = 1; worker.ReturningToPost = false;
                    fan.ReservedByWorker = worker.Id;
                    if (!QueueWorkerToPickup(worker)) ReleaseFreeService(worker, fan);
                    break;
                }
                if (fan.ReservedByWorker != 0) continue;
                for (int line = 0; line < fan.Order.Count; line++)
                {
                    TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                    if (order.Remaining <= 0) continue;
                    StreetWorkerRole role = GetDeliveryRole(order.Product);
                    if (!heldRoles.Contains(role) && IsFanReachableForRole(fan, role, order.Product)) heldRoles.Add(role);
                }
            }
        }

        // True when some worker of the role could still bring this product to the pilgrim before it leaves.
        private bool IsFanReachableForRole(TowerDefenseFanState fan, StreetWorkerRole role, int product)
        {
            TowerDefensePlacedStation station = FindPlacedStation(StationForProduct(product));
            if (station == null) return false;
            float workerSpeed = Mathf.Max(.001f, FreeWorkerSpeed(role));
            float fanSpeed = fan.RouteFollower.Speed;
            float timeToExit = fanSpeed <= .0001f ? 0f :
                (fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / fanSpeed;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i].Role == role && TryEstimateFreeServiceEta(workers[i], station, fan, workerSpeed, timeToExit, out _)) return true;
            return false;
        }

        private void UpdateFreeConstructionFanInterceptions()
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.ServiceStage != 2 || worker.ServiceUnitId <= 0) continue;
                float workerSpeed = Mathf.Max(.001f, FreeWorkerSpeed(worker.Role));
                TowerDefenseFanState fan = FindFan(worker.PendingFanId);
                float timeToExit = fan == null || fan.RouteFollower.Speed <= .0001f
                    ? 0f : (fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / fan.RouteFollower.Speed;
                if (fan == null || fan.OrderComplete || fan.RouteFollower.HasExited || timeToExit <= 0f)
                {
                    ReleaseFreeService(worker, fan);
                    continue;
                }

                // Passive kitchen levels: once in place beside the fan the worker stands still and hands the order over, as at a
                // campaign counter, instead of hopping a few pixels after the walking fan (it looked like a shake).
                if (IsPassiveKitchenLevel && !worker.IsMoving && WorkerInHandOverRange(worker, fan)) continue;
                float eta = RemainingWorkerPathDistance(worker) / workerSpeed;
                if (eta + .12f >= timeToExit)
                {
                    ReleaseFreeService(worker, fan);
                    continue;
                }

                Vector2 target = GetPredictedFanServiceAnchor(fan, eta);
                Vector2 previousTarget = worker.IsMoving && worker.TransferRoute.Length > 0
                    ? worker.TransferRoute[worker.TransferRoute.Length - 1] : worker.Position;
                if (Vector2.Distance(previousTarget, target) < 4f * worldUnitsPerLogicalPixel) continue;
                if (!TryBuildWorkerPath(worker, worker.Position, target, Rect.zero, out Vector2[] path))
                {
                    ReleaseFreeService(worker, fan);
                    continue;
                }
                worker.TransferRoute = path;
                worker.TransferIndex = 0;
                worker.IsMoving = path.Length > 0;
                worker.TargetZone = 0;
            }
        }

        private float RemainingWorkerPathDistance(TowerDefenseWorkerState worker)
        {
            float distance = 0f;
            Vector2 from = worker.Position;
            for (int i = worker.TransferIndex; i < worker.TransferRoute.Length; i++)
            {
                distance += Vector2.Distance(from, worker.TransferRoute[i]);
                from = worker.TransferRoute[i];
            }
            return distance;
        }

        private static float PathDistance(Vector2 start, Vector2[] path)
        {
            float distance = 0f;
            Vector2 from = start;
            for (int i = 0; i < path.Length; i++)
            {
                distance += Vector2.Distance(from, path[i]);
                from = path[i];
            }
            return distance;
        }

        private TowerDefenseWorkerState FindWorkerForFreeService(StreetWorkerRole role, TowerDefenseFanState fan, int product)
        {
            TowerDefensePlacedStation station = FindPlacedStation(StationForProduct(product));
            if (station == null) return null;
            float workerSpeed = Mathf.Max(.001f, FreeWorkerSpeed(role));
            float fanSpeed = fan.RouteFollower.Speed;
            float timeToExit = fanSpeed <= .0001f ? 0f :
                (fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / fanSpeed;
            TowerDefenseWorkerState best = null;
            float bestEta = float.PositiveInfinity;
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role != role || (worker.IsMoving && !worker.ReturningToPost) || worker.ServiceStage != 0 || worker.DeliveryCooldownSeconds > 0f) continue;
                if (!TryEstimateFreeServiceEta(worker, station, fan, workerSpeed, timeToExit, out float eta) || eta >= bestEta) continue;
                best = worker;
                bestEta = eta;
            }
            return best;
        }

        // The walk to a station only depends on where the worker stands and on the furniture, so it is kept while an idle worker stays on its spot.
        private readonly Dictionary<(int, int, int, int, int, int, long), float> pickupDistanceCache = new Dictionary<(int, int, int, int, int, int, long), float>();

        private bool TryGetPickupDistance(TowerDefenseWorkerState worker, TowerDefensePlacedStation station, out float distance)
        {
            var key = (worker.Id, station.Id, Mathf.RoundToInt(worker.Position.x * 1000f), Mathf.RoundToInt(worker.Position.y * 1000f),
                Mathf.RoundToInt(station.WorkPosition.x * 1000f), Mathf.RoundToInt(station.WorkPosition.y * 1000f), PathLatticeSignature());
            if (pickupDistanceCache.TryGetValue(key, out float cached)) { distance = cached; return cached >= 0f; }
            bool ok = TryBuildWorkerPath(worker, worker.Position, station.WorkPosition, Rect.zero, out Vector2[] pickupPath);
            distance = ok ? PathDistance(worker.Position, pickupPath) : -1f;
            if (pickupDistanceCache.Count > 4096) pickupDistanceCache.Clear();
            pickupDistanceCache[key] = distance;
            return ok;
        }

        private bool TryEstimateFreeServiceEta(TowerDefenseWorkerState worker, TowerDefensePlacedStation station,
            TowerDefenseFanState fan, float workerSpeed, float timeToExit, out float eta)
        {
            eta = float.PositiveInfinity;
            // 2026-11-24: the walk to the station cannot be shorter than the straight line, so a fan that leaves before even that is covered is out of reach
            // without searching a single path (late fans near the exit used to cost ~5 path searches per worker, every step).
            if (Vector2.Distance(worker.Position, station.WorkPosition) / workerSpeed + .12f >= timeToExit) return false;
            if (!TryGetPickupDistance(worker, station, out float pickupDistance)) return false;
            float pickupEta = pickupDistance / workerSpeed;
            Vector2 target = GetFanServiceAnchor(fan);
            float deliveryEta = 0f;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (!TryBuildWorkerPath(worker, station.WorkPosition, target, Rect.zero, out Vector2[] deliveryPath)) return false;
                deliveryEta = PathDistance(station.WorkPosition, deliveryPath) / workerSpeed;
                eta = pickupEta + deliveryEta;
                if (eta + .12f >= timeToExit) return false;
                target = GetPredictedFanServiceAnchor(fan, eta);
            }
            if (!TryBuildWorkerPath(worker, station.WorkPosition, target, Rect.zero, out Vector2[] finalPath)) return false;
            deliveryEta = PathDistance(station.WorkPosition, finalPath) / workerSpeed;
            eta = pickupEta + deliveryEta;
            return eta + .12f < timeToExit;
        }

        private bool QueueWorkerToPickup(TowerDefenseWorkerState worker)
        {
            TowerDefenseStationKind kind = StationForProduct(worker.PendingProduct);
            TowerDefensePlacedStation station = null;
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == kind) { station = placedStations[i]; break; }
            if (station == null || !TryBuildWorkerPath(worker, worker.Position, station.WorkPosition, Rect.zero, out Vector2[] path)) return false;
            worker.TargetZone = 0; worker.TransferRoute = path; worker.TransferIndex = 0; worker.IsMoving = path.Length > 0;
            if (!worker.IsMoving) AdvanceFreeConstructionServiceStage(worker);
            return true;
        }

        private void AdvanceFreeConstructionServiceStage(TowerDefenseWorkerState worker)
        {
            if (worker == null || worker.ServiceStage <= 0) return;
            TowerDefenseFanState fan = FindFan(worker.PendingFanId);
            if (fan == null || fan.OrderComplete || !fan.NeedsProduct(worker.PendingProduct) || fan.RouteFollower.HasExited)
            { ReleaseFreeService(worker, fan); return; }
            if (worker.ServiceStage == 1 || worker.ServiceStage == 3)
            {
                var source = FindPlacedStation(StationForProduct(worker.PendingProduct));
                if (source == null) { ReleaseFreeService(worker,fan); return; }
                if (!WorkerAt(worker,source.WorkPosition)) { QueueFreeWorkerPath(worker,source.WorkPosition); return; }
                if (IsPassiveKitchenLevel)
                {
                    // Chicago's worker reaches the station, then spends pickupSeconds grabbing the product.
                    if (!worker.ServiceDwelling) { worker.ServiceDwelling = true; worker.ServiceDwellSeconds = ChicagoDwellSeconds(worker.Role); return; }
                    if (worker.ServiceDwellSeconds > 0f) return;
                    worker.ServiceDwelling = false;
                }
                StreetFoodUnit unit;
                if (worker.PendingProduct < 4 && Kitchen.ActiveCooks)
                {
                    Kitchen.ReserveTableUnit(worker.PendingProduct, worker.Id);
                    unit = Kitchen.TryTake(worker.PendingProduct, worker.Role, worker.Id);
                }
                else unit = Kitchen.TryTake(worker.PendingProduct, worker.Role, worker.Id);
                if (unit == null) { worker.ServiceStage = 3; return; }
                worker.ServiceUnitId = unit.Id; worker.ServiceStage = 2;
                if (!TryBuildFreeConstructionInterceptPath(worker, fan, out Vector2[] path))
                {
                    ReleaseFreeService(worker, fan); return;
                }
                worker.TransferRoute = path; worker.TransferIndex = 0; worker.IsMoving = path.Length > 0;
                if (!worker.IsMoving) CompleteFreeConstructionDelivery(worker);
                return;
            }
            if (worker.ServiceStage == 2) CompleteFreeConstructionDelivery(worker);
        }

        private void CompleteFreeConstructionDelivery(TowerDefenseWorkerState worker)
        {
            TowerDefenseFanState fan = FindFan(worker.PendingFanId);
            int product = worker.PendingProduct, unitId = worker.ServiceUnitId;
            if (fan == null || fan.RouteFollower.HasExited)
            { ReleaseFreeService(worker, fan); return; }
            if (!WorkerInHandOverRange(worker, fan))
            {
                TryQueueFreeConstructionIntercept(worker, fan);
                return;
            }
            if (IsPassiveKitchenLevel && fan != null && fan.NeedsProduct(product))
            {
                // ...and spends pickupSeconds handing it over before the sale is made.
                if (!worker.ServiceDwelling) { worker.ServiceDwelling = true; worker.ServiceDwellSeconds = ChicagoDwellSeconds(worker.Role); return; }
                if (worker.ServiceDwellSeconds > 0f) return;
                worker.ServiceDwelling = false;
            }
            if (fan == null || !fan.NeedsProduct(product) || !Kitchen.Consume(unitId, product, worker.Id))
            {
                ReleaseFreeService(worker, fan); return;
            }
            worker.ServiceUnitId = 0;
            if (fan.RecordDelivery(product))
            {
                if (!fan.HasReceivedDelivery) { fan.HasReceivedDelivery = true; if (customersAttended < int.MaxValue) customersAttended++; }
                fan.IsReceiving = true; fan.ReceiveAnimationSeconds = DeliveryCueSeconds;
                deliveredByProduct[product]++; TotalDelivered++;
                int sale = Mathf.RoundToInt(StreetSimulation.OfficialProductPrice(product));
                coins = coins > int.MaxValue - sale ? int.MaxValue : coins + sale;
                coinsEarned = coinsEarned > int.MaxValue - sale ? int.MaxValue : coinsEarned + sale;
                if (fan.OrderComplete && !fan.CompletionCounted)
                { fan.CompletionCounted = true; if (ordersCompleted < int.MaxValue) ordersCompleted++; fan.IsDeparting = true; fan.DepartureProgress = 0f; }
                worker.DeliveryCooldownSeconds = IsPassiveKitchenLevel ? 0f : DeliveryIntervalSeconds; worker.ServeAnimationSeconds = DeliveryCueSeconds;
                worker.LastServiceTarget = fan.RouteFollower.Position;
                DeliveryCompleted?.Invoke(new TowerDefenseDelivery(unitId, product, worker.Id, fan.Id, 0, worker.Position, fan.RouteFollower.Position, elapsed));
            }
            ReleaseFreeService(worker, fan);
        }

        // The worker stands on the fan's side of the lane and the fan is beside it: the order can be handed over.
        private bool WorkerInHandOverRange(TowerDefenseWorkerState worker, TowerDefenseFanState fan)
        {
            Vector2 serviceAnchor = GetFanServiceAnchor(fan);
            bool rightRun = ServiceLane(fan.RouteFollower.Position) == 1;
            float acrossLane = rightRun ? Mathf.Abs(worker.Position.x - serviceAnchor.x) : Mathf.Abs(worker.Position.y - serviceAnchor.y);
            float alongLane = rightRun ? Mathf.Abs(worker.Position.y - fan.RouteFollower.Position.y)
                : Mathf.Abs(worker.Position.x - fan.RouteFollower.Position.x);
            return acrossLane <= 2f * worldUnitsPerLogicalPixel && alongLane <= 64f * worldUnitsPerLogicalPixel;
        }

        private void ReleaseFreeService(TowerDefenseWorkerState worker, TowerDefenseFanState fan)
        {
            if (fan != null && fan.ReservedByWorker == worker.Id) fan.ReservedByWorker = 0;
            if (worker.ServiceUnitId > 0) Kitchen.ReturnCarried(worker.ServiceUnitId, worker.Id);
            Kitchen.ReleaseTableReservation(worker.Id);
            worker.PendingFanId = 0; worker.PendingProduct = -1; worker.ServiceStage = 0; worker.ServiceUnitId = 0;
            worker.WaitJitter = float.NaN; // a new stop: a new random spot in the waiting band
            worker.ServiceDwelling = false; worker.ServiceDwellSeconds = 0f;
            worker.TransferRoute = Array.Empty<Vector2>(); worker.TransferIndex = 0; worker.IsMoving = false;
            worker.ReturningToPost = false;
            // After a hand-over the worker stays put until the product reaches the pilgrim's hand.
            if (worker.ServeAnimationSeconds <= 0f) QueueWorkerToPost(worker);
        }

        private TowerDefenseFanState FindFan(int id)
        { for (int i = 0; i < fans.Count; i++) if (fans[i].Id == id) return fans[i]; return null; }

        private Vector2 GetFanServiceAnchor(TowerDefenseFanState fan)
        {
            return GetFanServiceAnchor(fan.RouteFollower.Position);
        }

        // On the C-shaped fence the kitchen sits right next to the entry and the exit: a fan is only served once it is fully on
        // screen, otherwise it would be handed its order the moment it appears (or while it leaves) and never walk the fence.
        private bool FanIsServiceable(TowerDefenseFanState fan) =>
            !IsCShapedLevel || fan.RouteFollower.Position.x >= WorkBounds.xMin + TowerDefenseResponsiveLayout.FanFrameWidthLogical * .5f * worldUnitsPerLogicalPixel;

        // Which side of the fence a fan is served from: 0 top run, 1 right run, 2 bottom run (only the "C" fence has more than one).
        private int ServiceLane(Vector2 fanPosition)
        {
            if (!IsCShapedLevel) return 0;
            Vector2[] center = routeGeometry.Centerline;
            float dTop = Mathf.Abs(fanPosition.y - center[0].y), dBottom = Mathf.Abs(fanPosition.y - center[center.Length - 1].y),
                dRight = Mathf.Abs(fanPosition.x - center[1].x);
            return dRight < dTop && dRight < dBottom ? 1 : dBottom < dTop ? 2 : 0;
        }

        private Vector2 GetFanServiceAnchor(Vector2 fanPosition)
        {
            float pixel = worldUnitsPerLogicalPixel;
            float lean = IsBocaKitchenLevel ? ServiceLeanLogical : 0f;
            int lane = ServiceLane(fanPosition);
            if (lane != 0)
            {
                float halfWidth = CocacoleroVisualWidthLogical * .5f * pixel;
                float feet = ConstructionWorkerHeightLogical * WorkerVisualPivotY * pixel;
                float topFeet = WorkBounds.yMax - ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) * pixel;
                if (lane == 1)
                    // Right run: the worker stands against the inside of the right rail, level with the fan.
                    return new Vector2(WorkBounds.xMax - halfWidth, Mathf.Clamp(fanPosition.y, WorkBounds.yMin + feet, topFeet));
                // Bottom run: feet right above the bottom rail, level with the fan.
                return new Vector2(Mathf.Clamp(fanPosition.x, WorkBounds.xMin + halfWidth, WorkBounds.xMax - halfWidth), WorkBounds.yMin + feet);
            }
            float y = routeGeometry.RightBoundary[0].y - (ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) + 8f - lean) * pixel;
            float half = CocacoleroVisualWidthLogical * .5f * pixel;
            return new Vector2(Mathf.Clamp(fanPosition.x, WorkBounds.xMin + half, WorkBounds.xMax - half), y);
        }

        private Vector2 GetPredictedFanServiceAnchor(TowerDefenseFanState fan, float secondsAhead)
        {
            float speed = fan.RouteFollower.Speed;
            float timeToExit = speed <= .0001f ? 0f :
                Mathf.Max(0f, fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / speed;
            float predictionTime = Mathf.Clamp(secondsAhead, 0f, timeToExit);
            Vector2 predictedPosition = fan.RouteFollower.Position +
                fan.RouteFollower.Facing * (speed * predictionTime);
            return GetFanServiceAnchor(predictedPosition);
        }

        private bool TryBuildFreeConstructionInterceptPath(TowerDefenseWorkerState worker, TowerDefenseFanState fan,
            out Vector2[] path)
        {
            path = Array.Empty<Vector2>();
            float workerSpeed = Mathf.Max(.001f, FreeWorkerSpeed(worker.Role));
            float fanSpeed = fan.RouteFollower.Speed;
            if (fanSpeed <= .0001f) return false;
            Vector2 target = GetFanServiceAnchor(fan);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (!TryBuildWorkerPath(worker, worker.Position, target, Rect.zero, out path)) return false;
                float eta = PathDistance(worker.Position, path) / workerSpeed;
                float timeToExit = (fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / fanSpeed;
                if (eta + .12f >= timeToExit) return false;
                target = GetPredictedFanServiceAnchor(fan, eta);
            }
            if (!TryBuildWorkerPath(worker, worker.Position, target, Rect.zero, out path)) return false;
            float finalEta = PathDistance(worker.Position, path) / workerSpeed;
            float finalTimeToExit = (fan.RouteFollower.TotalDistance - fan.RouteFollower.Progress) / fanSpeed;
            return finalEta + .12f < finalTimeToExit;
        }

        private bool TryQueueFreeConstructionIntercept(TowerDefenseWorkerState worker, TowerDefenseFanState fan)
        {
            if (!TryBuildFreeConstructionInterceptPath(worker, fan, out Vector2[] path))
            {
                ReleaseFreeService(worker, fan);
                return false;
            }
            worker.TransferRoute = path;
            worker.TransferIndex = 0;
            worker.IsMoving = path.Length > 0;
            worker.TargetZone = 0;
            // Already on the service lane but the pilgrim is not beside the worker yet: wait in place.
            // AdvanceWorkers retries the hand-over next slice; recursing here overflowed the stack.
            return true;
        }

        internal static TowerDefenseStationKind StationForProduct(int product)
        {
            if (product < 2) return TowerDefenseStationKind.NormalTable;
            if (product < 4) return TowerDefenseStationKind.PremiumTable;
            if (product == 4) return TowerDefenseStationKind.CocaBarrel;
            if (product == 5) return TowerDefenseStationKind.FernetTable;
            return TowerDefenseStationKind.BeerBarrel;
        }

        private bool FreeLayoutFitsReflow(Rect nextBounds, float nextPixel)
        {
            float ratio=nextPixel/worldUnitsPerLogicalPixel;
            if(!IsFinite(ratio)||ratio<=0f) return false;
            foreach(var station in placedStations)
            {
                Rect mapped=new Rect(nextBounds.min+(station.Bounds.position-WorkBounds.min)*ratio,station.Bounds.size*ratio);
                Vector2 access=nextBounds.min+(station.WorkPosition-WorkBounds.min)*ratio;
                if(!Contains(nextBounds,mapped)||!IsFinite(access.x)||!IsFinite(access.y)) return false;
            }
            foreach(var worker in workers)
            {
                Rect body=WorkerBounds(worker.Position,worker.Role);
                Rect mapped=new Rect(nextBounds.min+(body.position-WorkBounds.min)*ratio,body.size*ratio);
                if(!Contains(nextBounds,mapped)) return false;
            }
            return true;
        }

        private bool CanWorkerOccupyPosition(TowerDefenseWorkerState moving, Vector2 position)
        {
            Rect body = WorkerBounds(position, moving.Role);
            if (!Contains(WorkBounds, body)) return false;
            for (int i = 0; i < placedStations.Count; i++) if (Overlaps(body, placedStations[i].Bounds)) return false;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i] != moving && Overlaps(body, WorkerBounds(workers[i].Position, workers[i].Role))) return false;
            return true;
        }

        private Rect StationFootprint(TowerDefenseStationKind kind, Vector2 position) =>
            StationFootprint(kind, position, true);

        private Rect StationFootprint(TowerDefenseStationKind kind, Vector2 position, bool snapToGrid)
        {
            Vector2 sizeLogical;
            if (IsBocaKitchenLevel)
            {
                Rect boca = BocaKitchenSourceBounds(kind);
                if (boca.width <= 0f || boca.height <= 0f) return Rect.zero;
                sizeLogical = boca.size * BocaKitchenFitScale();
                sizeLogical.y *= BocaVerticalScale;
                // StreetKitchenLayout's five-piece Boca row overlaps its transparent art by BocaPieceOverlapPx.
                // Trim exactly that overlap (plus a hair) so the collision bounds abut instead of overlapping,
                // keeping the same edge-to-edge lineup and distances as Boca.
                if (kind != TowerDefenseStationKind.NormalGrill && kind != TowerDefenseStationKind.PremiumGrill)
                    sizeLogical.x = Mathf.Max(1f, sizeLogical.x - (BocaPieceOverlapPx * BocaKitchenFitScale() + .05f));
            }
            else
            {
                switch (kind)
                {
                    case TowerDefenseStationKind.NormalGrill:
                    case TowerDefenseStationKind.PremiumGrill: sizeLogical = new Vector2(160f, 60f); break;
                    case TowerDefenseStationKind.NormalTable:
                    case TowerDefenseStationKind.PremiumTable: sizeLogical = new Vector2(160f, 58f); break;
                    case TowerDefenseStationKind.FernetTable: sizeLogical = new Vector2(88f, 68f); break;
                    case TowerDefenseStationKind.CocaBarrel:
                    case TowerDefenseStationKind.BeerBarrel: sizeLogical = new Vector2(72f, 68f); break;
                    default: return Rect.zero;
                }
            }
            Vector2 center = snapToGrid ? Snap(position) : position;
            Vector2 size = sizeLogical * worldUnitsPerLogicalPixel;
            return new Rect(center - size * .5f, size);
        }

        private Vector2 Snap(Vector2 position)
        {
            float spacing = ConstructionGridLogical * worldUnitsPerLogicalPixel;
            Vector2 origin = WorkBounds.min;
            return origin + new Vector2(Mathf.Round((position.x - origin.x) / spacing),
                Mathf.Round((position.y - origin.y) / spacing)) * spacing;
        }

        private Rect WorkerBounds(Vector2 position, StreetWorkerRole role = StreetWorkerRole.Parrillero)
        {
            float widthLogical = role == StreetWorkerRole.Cocacolero ? 94f : ConstructionWorkerWidthLogical;
            float width = widthLogical * worldUnitsPerLogicalPixel;
            float height = ConstructionWorkerHeightLogical * worldUnitsPerLogicalPixel;
            return new Rect(position.x - width * .5f,
                position.y - height * WorkerVisualPivotY, width, height);
        }

        private Vector2 FindStationAccessAnchor(TowerDefenseStationKind kind, Rect bounds, Rect extraObstacle = default)
        {
            float pixel = worldUnitsPerLogicalPixel;
            StreetWorkerRole accessRole = StationWorkerRole(kind);
            float workerWidth = accessRole == StreetWorkerRole.Cocacolero ? 94f : ConstructionWorkerWidthLogical;
            float halfWorker = workerWidth * .5f * pixel;
            float side = 8f * pixel;
            float maxFeetY = WorkBounds.yMax - ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) * pixel;
            float preferredY = Mathf.Min(bounds.center.y, maxFeetY);
            Vector2[] candidates =
            {
                new Vector2(bounds.xMax + halfWorker + side, preferredY),
                new Vector2(bounds.xMin - halfWorker - side, preferredY),
                new Vector2(bounds.center.x, bounds.yMax + 4f * pixel),
                new Vector2(bounds.center.x, bounds.yMin - ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) * pixel - 8f * pixel)
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (IsStationAccessClear(kind, candidates[i], bounds, extraObstacle)) return candidates[i];
            }

            // A dense authored starter layout can lack a worker-width side gap beside one
            // particular prefab. Use the nearest open tile in the connected service aisle.
            Vector2 best = new Vector2(float.NaN, float.NaN);
            float bestDistance = float.PositiveInfinity;
            float step = 16f * pixel;
            for (float y = WorkBounds.yMin + ConstructionWorkerHeightLogical * WorkerVisualPivotY * pixel;
                y <= WorkBounds.yMax - ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) * pixel; y += step)
                for (float x = WorkBounds.xMin + halfWorker; x <= WorkBounds.xMax - halfWorker; x += step)
                {
                    Vector2 candidate = new Vector2(x, y);
                    if (!IsStationAccessClear(kind, candidate, bounds, extraObstacle)) continue;
                    float distance = (candidate - bounds.center).sqrMagnitude;
                    if (distance >= bestDistance) continue;
                    best = candidate;
                    bestDistance = distance;
                }
            if (IsFinite(best.x)) return best;
            return new Vector2(float.NaN, float.NaN);
        }

        private bool IsStationAccessClear(TowerDefenseStationKind kind, Vector2 position, Rect bounds, Rect extraObstacle)
        {
            StreetWorkerRole role = StationWorkerRole(kind);
            Rect worker = WorkerBounds(position, role);
            if (IsBocaKitchenLevel)
            {
                // La Renga's cooks and the Cocacolero stand behind their furniture with their heads over the top rail, as Luján's lean over the fence.
                if (!Contains(IsCShapedLevel ? WorkerArea() : WorkBounds, worker) || Overlaps(worker, extraObstacle)) return false;
                Rect feet = WorkerFeet(position);
                foreach (var station in placedStations)
                    if (Overlaps(feet, StationSolidRect(station.Kind, station.Bounds))) return false;
                return !Overlaps(feet, StationSolidRect(kind, bounds));
            }
            if (!Contains(WorkBounds, worker) || Overlaps(worker, bounds) || Overlaps(worker, extraObstacle)) return false;
            for (int i = 0; i < placedStations.Count; i++)
            {
                TowerDefensePlacedStation station = placedStations[i];
                if (Overlaps(worker, station.Bounds)) return false;
                if (station.Bounds == bounds || !IsFinite(station.WorkPosition.x) || !IsFinite(station.WorkPosition.y)) continue;
                if (Overlaps(worker, WorkerBounds(station.WorkPosition, StationWorkerRole(station.Kind)))) return false;
            }
            return true;
        }

        private TowerDefensePlacedStation RoleStation(StreetWorkerRole role)
        {
            TowerDefenseStationKind primary;
            switch (role)
            {
                case StreetWorkerRole.Parrillero: primary = TowerDefenseStationKind.NormalGrill; break;
                case StreetWorkerRole.ParrilleroPremium: primary = TowerDefenseStationKind.PremiumGrill; break;
                case StreetWorkerRole.Cocacolero: primary = TowerDefenseStationKind.CocaBarrel; break;
                case StreetWorkerRole.Fernetero: primary = TowerDefenseStationKind.FernetTable; break;
                case StreetWorkerRole.Mozo: primary = HasStation(TowerDefenseStationKind.NormalTable)
                    ? TowerDefenseStationKind.NormalTable : TowerDefenseStationKind.PremiumTable; break;
                default: return null;
            }
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == primary) return placedStations[i];
            if (role == StreetWorkerRole.Cocacolero)
                for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == TowerDefenseStationKind.BeerBarrel) return placedStations[i];
            return null;
        }

        private Vector2 RoleStationAccess(StreetWorkerRole role)
        {
            TowerDefensePlacedStation station = RoleStation(role);
            return station == null ? Vector2.zero : station.WorkPosition;
        }

        private bool QueueWorkerToStation(TowerDefenseWorkerState worker)
        {
            Vector2 target = RoleStationAccess(worker.Role);
            if (!IsFinite(target.x) || !IsFinite(target.y) || !TryBuildWorkerPath(worker, worker.Position, target, Rect.zero, out Vector2[] path)) return false;
            worker.TargetZone = 0;
            worker.PostPosition = target;
            worker.TransferRoute = path;
            worker.TransferIndex = 0;
            worker.IsMoving = path.Length > 0;
            if (!worker.IsMoving) worker.Position = target;
            return true;
        }

        // Resting/placement spots never overlap a co-worker. Walking follows Boca's active-cook rule:
        // routes avoid furniture only, so co-workers may cross briefly instead of walling off the
        // single service lane under the fence (a parked Parrillero used to strand the Mozo).
        // La Renga: a cook never walks the strip between the grills and the barrels. To get past the barrels it climbs the corridor between the
        // table and the barrels and goes behind (above) the beer barrel. The area is the strip right under the barrels (a few px wider than them on each side).
        // Cooks and drink workers alike: nobody walks the strip under the barrels (drinks are taken from the barrels' right side).
        private static bool UsesBarrelKeepOut(StreetWorkerRole role) =>
            IsCookRole(role) || role == StreetWorkerRole.Cocacolero || role == StreetWorkerRole.Fernetero;

        private Rect CookKeepOut()
        {
            if (!IsCShapedLevel) return Rect.zero;
            bool any = false; Rect union = Rect.zero;
            foreach (var station in placedStations)
            {
                if (station.Kind != TowerDefenseStationKind.CocaBarrel && station.Kind != TowerDefenseStationKind.BeerBarrel) continue;
                union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, station.Bounds.xMin), Mathf.Min(union.yMin, station.Bounds.yMin),
                    Mathf.Max(union.xMax, station.Bounds.xMax), Mathf.Max(union.yMax, station.Bounds.yMax)) : station.Bounds;
                any = true;
            }
            TowerDefensePlacedStation grill = FindPlacedStation(TowerDefenseStationKind.NormalGrill);
            if (!any || grill == null) return Rect.zero;
            float pixel = worldUnitsPerLogicalPixel;
            return Rect.MinMaxRect(union.xMin - 4f * pixel, grill.Bounds.yMax - pixel, union.xMax + 4f * pixel, union.yMin + pixel);
        }

        // 2026-11-24: exact segment/rectangle test (Liang-Barsky). It used to walk the segment in steps of a quarter of the thinnest side; the barrels' keep-out
        // strip is ~0.06 units thick, so a 3-unit segment cost ~200 point tests, and every path search runs hundreds of these checks (the late-match stutter).
        private static bool SegmentCrosses(Rect area, Vector2 from, Vector2 to)
        {
            if (area.width <= 0f || area.height <= 0f) return false;
            float t0 = 0f, t1 = 1f, dx = to.x - from.x, dy = to.y - from.y;
            return ClipSegment(-dx, from.x - area.xMin, ref t0, ref t1) && ClipSegment(dx, area.xMax - from.x, ref t0, ref t1) &&
                ClipSegment(-dy, from.y - area.yMin, ref t0, ref t1) && ClipSegment(dy, area.yMax - from.y, ref t0, ref t1);
        }

        private static bool ClipSegment(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-9f) return q >= 0f;
            float r = q / p;
            if (p < 0f) { if (r > t1) return false; if (r > t0) t0 = r; }
            else { if (r < t0) return false; if (r < t1) t1 = r; }
            return true;
        }

        private bool FreePositionClear(Vector2 position, StreetWorkerRole role, TowerDefenseWorkerState ignore, Rect extra,
            bool avoidWorkers = true)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y)) return false;
            Rect body = WorkerBounds(position, role);
            if (!Contains(WorkerArea(), body) || Overlaps(body, extra)) return false;
            if (UsesBarrelKeepOut(role) && CookKeepOut().Contains(position)) return false;
            foreach (var station in placedStations) if (WorkerHitsStation(position, body, station)) return false;
            if (avoidWorkers)
                foreach (var worker in workers) if (worker != ignore && Overlaps(body, WorkerBounds(worker.Position, worker.Role))) return false;
            return true;
        }
        private bool FreeSegmentClear(Vector2 from, Vector2 to, StreetWorkerRole role, TowerDefenseWorkerState ignore, Rect extra,
            bool avoidWorkers = true)
        {
            if (!FreePositionClear(from, role, ignore, extra, avoidWorkers) || !FreePositionClear(to, role, ignore, extra, avoidWorkers)) return false;
            if (UsesBarrelKeepOut(role) && SegmentCrosses(CookKeepOut(), from, to)) return false;
            Rect a = WorkerBounds(from, role), b = WorkerBounds(to, role);
            Rect swept = Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin), Mathf.Min(a.yMin,b.yMin), Mathf.Max(a.xMax,b.xMax), Mathf.Max(a.yMax,b.yMax));
            if (Overlaps(swept, extra)) return false;
            if (IsBocaKitchenLevel)
            {
                Rect feetA = WorkerFeet(from), feetB = WorkerFeet(to);
                Rect sweptFeet = Rect.MinMaxRect(Mathf.Min(feetA.xMin, feetB.xMin), Mathf.Min(feetA.yMin, feetB.yMin),
                    Mathf.Max(feetA.xMax, feetB.xMax), Mathf.Max(feetA.yMax, feetB.yMax));
                foreach (var station in placedStations)
                    if (Overlaps(sweptFeet, StationSolidRect(station.Kind, station.Bounds))) return false;
            }
            else foreach (var station in placedStations) if (Overlaps(swept, station.Bounds)) return false;
            if (avoidWorkers)
                foreach (var worker in workers) if (worker != ignore && Overlaps(swept, WorkerBounds(worker.Position, worker.Role))) return false;
            return true;
        }
        // The waypoint lattice only depends on the stations and the worker's body, not on where the other workers are (paths
        // ignore them), so it is built once per role and reused until a station changes. Searching it used to recompute every
        // node of the grid on every call, which made a crew that cannot reach its targets cost hundreds of milliseconds a frame.
        private sealed class PathLattice
        {
            public Vector2[] Points; public bool[] Open; public sbyte[] Edges; public int Columns, Rows; public float Step; public long Signature;
        }
        private readonly Dictionary<StreetWorkerRole, PathLattice> pathLattices = new Dictionary<StreetWorkerRole, PathLattice>();
        private static readonly int[] LatticeDx = { -1, 1, 0, 0, -1, 1, -1, 1 }, LatticeDy = { 0, 0, -1, 1, -1, -1, 1, 1 };

        private long PathLatticeSignature()
        {
            unchecked
            {
                long hash = 17 + placedStations.Count;
                foreach (var station in placedStations)
                {
                    Rect b = station.Bounds;
                    hash = hash * 31 + (int)station.Kind;
                    hash = hash * 31 + Mathf.RoundToInt(b.xMin * 1000f); hash = hash * 31 + Mathf.RoundToInt(b.yMin * 1000f);
                    hash = hash * 31 + Mathf.RoundToInt(b.width * 1000f); hash = hash * 31 + Mathf.RoundToInt(b.height * 1000f);
                }
                return hash;
            }
        }

        private PathLattice GetPathLattice(StreetWorkerRole role, TowerDefenseWorkerState movingWorker)
        {
            long signature = PathLatticeSignature();
            if (pathLattices.TryGetValue(role, out PathLattice cached) && cached.Signature == signature) return cached;
            float step = (config.FreeConstruction ? ConstructionGridLogical * .5f : ConstructionGridLogical) * worldUnitsPerLogicalPixel;
            // La Renga's cooks work from a strip a few pixels tall between two solid rows. One row of waypoints is laid exactly on
            // it, otherwise no node falls inside the strip and a cook standing there could never reach anything.
            float originY = WorkBounds.yMin;
            if (IsCShapedLevel)
            {
                TowerDefensePlacedStation grill = FindPlacedStation(TowerDefenseStationKind.NormalGrill);
                if (grill != null && IsFinite(grill.WorkPosition.y)) originY = WorkBounds.yMin + Mathf.Repeat(grill.WorkPosition.y - WorkBounds.yMin, step);
            }
            // Same for the corridor between the premium table and the barrels: a column of waypoints runs along its centre line.
            float originX = WorkBounds.xMin;
            if (IsCShapedLevel)
            {
                TowerDefensePlacedStation table = FindPlacedStation(TowerDefenseStationKind.PremiumTable), barrel = FindPlacedStation(TowerDefenseStationKind.CocaBarrel);
                if (table != null && barrel != null)
                    originX = WorkBounds.xMin + Mathf.Repeat((table.Bounds.xMax + barrel.Bounds.xMin) * .5f - WorkBounds.xMin, step);
            }
            int columns = Mathf.Max(1, Mathf.FloorToInt((WorkBounds.xMax - originX) / step) + 1);
            int rows = Mathf.Max(1, Mathf.FloorToInt((WorkBounds.yMax - originY) / step) + 1), total = columns * rows;
            var lattice = new PathLattice
            {
                Points = new Vector2[total], Open = new bool[total], Edges = new sbyte[total * 8], Columns = columns, Rows = rows, Step = step, Signature = signature
            };
            for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            {
                int id = y * columns + x;
                lattice.Points[id] = new Vector2(originX + x * step, originY + y * step);
                lattice.Open[id] = FreePositionClear(lattice.Points[id], role, movingWorker, Rect.zero, false);
            }
            pathLattices[role] = lattice;
            return lattice;
        }

        // 2026-11-24: build each role's waypoint lattice, and every edge it can walk, before the run starts. They used to be built on the first
        // job, costing a 50-115 ms hitch at the start of the match (the BFS below visits and caches the whole connected pocket).
        private void WarmPathLattices()
        {
            if (!config.FreeConstruction) return;
            foreach (StreetWorkerRole role in new[] { StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Cocacolero })
                UnreachableWorkerNodes(role);
        }

        /// <summary>Waypoints a worker of this role could stand on but never reach from the grill post: 0 when the pocket is connected.</summary>
        internal int UnreachableWorkerNodes(StreetWorkerRole role)
        {
            PathLattice lattice = GetPathLattice(role, null);
            TowerDefensePlacedStation grill = FindPlacedStation(TowerDefenseStationKind.NormalGrill);
            if (grill == null) return 0;
            int start = -1; float best = float.MaxValue;
            for (int i = 0; i < lattice.Points.Length; i++)
            {
                float d = Vector2.Distance(lattice.Points[i], grill.WorkPosition);
                if (lattice.Open[i] && d < best) { best = d; start = i; }
            }
            if (start < 0) return lattice.Points.Length;
            var seen = new bool[lattice.Points.Length]; var queue = new Queue<int>(); queue.Enqueue(start); seen[start] = true;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int direction = 0; direction < 8; direction++)
                {
                    int nx = current % lattice.Columns + LatticeDx[direction], ny = current / lattice.Columns + LatticeDy[direction];
                    if (nx < 0 || nx >= lattice.Columns || ny < 0 || ny >= lattice.Rows) continue;
                    int next = ny * lattice.Columns + nx;
                    if (seen[next] || !LatticeEdgeClear(lattice, current, direction, role, null)) continue;
                    seen[next] = true; queue.Enqueue(next);
                }
            }
            int unreachable = 0;
            for (int i = 0; i < seen.Length; i++) if (lattice.Open[i] && !seen[i]) unreachable++;
            return unreachable;
        }

        private bool LatticeEdgeClear(PathLattice lattice, int from, int direction, StreetWorkerRole role, TowerDefenseWorkerState movingWorker)
        {
            int x = from % lattice.Columns + LatticeDx[direction], y = from / lattice.Columns + LatticeDy[direction];
            if (x < 0 || x >= lattice.Columns || y < 0 || y >= lattice.Rows) return false;
            int to = y * lattice.Columns + x;
            if (!lattice.Open[to]) return false;
            int slot = from * 8 + direction;
            if (lattice.Edges[slot] == 0)
                lattice.Edges[slot] = (sbyte)(FreeSegmentClear(lattice.Points[from], lattice.Points[to], role, movingWorker, Rect.zero, false) ? 1 : -1);
            return lattice.Edges[slot] > 0;
        }

        private bool TryBuildWorkerPath(TowerDefenseWorkerState movingWorker, Vector2 start, Vector2 target,
            Rect extraObstacle, out Vector2[] path, StreetWorkerRole pathRole = StreetWorkerRole.Parrillero)
        {
            path = Array.Empty<Vector2>();
            StreetWorkerRole role = movingWorker == null ? pathRole : movingWorker.Role;
            if (!FreePositionClear(start, role, movingWorker, extraObstacle, false) || !FreePositionClear(target, role, movingWorker, extraObstacle, false)) return false;
            if (Vector2.Distance(start, target) <= worldUnitsPerLogicalPixel * .1f) return true;
            if (FreeSegmentClear(start,target,role,movingWorker,extraObstacle,false)) { path = new[] {target}; return true; }
            // A 32px placement grid can bridge over a narrow but passable alley underneath
            // furniture; route waypoints therefore use half that spacing for free-build levels.
            if (extraObstacle != Rect.zero) return TryBuildWorkerPathUncached(movingWorker, start, target, extraObstacle, out path, role);
            PathLattice lattice = GetPathLattice(role, movingWorker);
            int total = lattice.Points.Length;
            var previous = new int[total];
            for (int i = 0; i < total; i++) previous[i] = -2;
            var queue = new Queue<int>();
            float attachDistance = lattice.Step * 1.5f;
            for (int id = 0; id < total; id++)
                if (lattice.Open[id] && Vector2.Distance(start, lattice.Points[id]) <= attachDistance &&
                    FreeSegmentClear(start, lattice.Points[id], role, movingWorker, Rect.zero, false))
                { previous[id] = -1; queue.Enqueue(id); }
            if (queue.Count == 0) return false;
            int reached = -1;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (Vector2.Distance(lattice.Points[current], target) <= attachDistance &&
                    FreeSegmentClear(lattice.Points[current], target, role, movingWorker, Rect.zero, false)) { reached = current; break; }
                for (int direction = 0; direction < 8; direction++)
                {
                    int next = (current / lattice.Columns + LatticeDy[direction]) * lattice.Columns + current % lattice.Columns + LatticeDx[direction];
                    int nx = current % lattice.Columns + LatticeDx[direction], ny = current / lattice.Columns + LatticeDy[direction];
                    if (nx < 0 || nx >= lattice.Columns || ny < 0 || ny >= lattice.Rows || previous[next] != -2) continue;
                    if (!LatticeEdgeClear(lattice, current, direction, role, movingWorker)) continue;
                    previous[next] = current; queue.Enqueue(next);
                }
            }
            if (reached < 0) return false;
            var reverse = new List<Vector2>();
            for (int node = reached; node >= 0; node = previous[node]) reverse.Add(lattice.Points[node]);
            reverse.Reverse(); reverse.Add(target);
            path = SmoothWorkerPath(start, reverse, role, movingWorker, extraObstacle); return true;
        }

        private bool TryBuildWorkerPathUncached(TowerDefenseWorkerState movingWorker, Vector2 start, Vector2 target,
            Rect extraObstacle, out Vector2[] path, StreetWorkerRole role)
        {
            path = Array.Empty<Vector2>();
            float step = (config.FreeConstruction ? ConstructionGridLogical * .5f : ConstructionGridLogical) * worldUnitsPerLogicalPixel;
            int columns = Mathf.Max(1, Mathf.FloorToInt(WorkBounds.width / step) + 1);
            int rows = Mathf.Max(1, Mathf.FloorToInt(WorkBounds.height / step) + 1), total = columns * rows;
            var points = new Vector2[total]; var open = new bool[total]; var previous = new int[total];
            var queue = new Queue<int>();
            float attachDistance = step * 1.5f;
            for (int y=0;y<rows;y++) for(int x=0;x<columns;x++)
            {
                int id=y*columns+x; points[id]=WorkBounds.min+new Vector2(x*step,y*step); previous[id]=-2;
                open[id]=FreePositionClear(points[id],role,movingWorker,extraObstacle,false);
                if (open[id] && Vector2.Distance(start,points[id]) <= attachDistance && FreeSegmentClear(start,points[id],role,movingWorker,extraObstacle,false))
                { previous[id]=-1; queue.Enqueue(id); }
            }
            int reached=-1;
            while(queue.Count>0)
            {
                int current=queue.Dequeue(), x=current%columns, y=current/columns;
                if(Vector2.Distance(points[current],target)<=attachDistance && FreeSegmentClear(points[current],target,role,movingWorker,extraObstacle,false)) { reached=current; break; }
                Enqueue(x-1,y,current); Enqueue(x+1,y,current); Enqueue(x,y-1,current); Enqueue(x,y+1,current);
                Enqueue(x-1,y-1,current); Enqueue(x+1,y-1,current); Enqueue(x-1,y+1,current); Enqueue(x+1,y+1,current);
            }
            if(reached<0) return false;
            var reverse=new List<Vector2>();
            for(int node=reached;node>=0;node=previous[node]) reverse.Add(points[node]);
            reverse.Reverse(); reverse.Add(target);
            path=SmoothWorkerPath(start,reverse,role,movingWorker,extraObstacle); return true;
            void Enqueue(int x,int y,int from)
            {
                if(x<0||x>=columns||y<0||y>=rows) return;
                int next=y*columns+x;
                if(!open[next]||previous[next]!=-2||!FreeSegmentClear(points[from],points[next],role,movingWorker,extraObstacle,false)) return;
                previous[next]=from; queue.Enqueue(next);
            }
        }

        // Workers walk straight lines like in Boca: drop every waypoint that a clear direct segment can skip,
        // so the grid search never leaves a staircase of axis-aligned legs.
        private Vector2[] SmoothWorkerPath(Vector2 start, List<Vector2> waypoints, StreetWorkerRole role,
            TowerDefenseWorkerState movingWorker, Rect extraObstacle)
        {
            var smooth = new List<Vector2>();
            Vector2 anchor = start;
            int index = 0;
            while (index < waypoints.Count)
            {
                int furthest = index;
                for (int candidate = waypoints.Count - 1; candidate > index; candidate--)
                    if (FreeSegmentClear(anchor, waypoints[candidate], role, movingWorker, extraObstacle, false)) { furthest = candidate; break; }
                smooth.Add(waypoints[furthest]);
                anchor = waypoints[furthest];
                index = furthest + 1;
            }
            return smooth.ToArray();
        }

        private bool HasConnectedStationAccess(TowerDefenseStationKind newKind, Rect newBounds)
        {
            var anchors = new List<Vector2>();
            foreach(var station in placedStations)
            {
                Vector2 point=FindStationAccessAnchor(station.Kind,station.Bounds,newBounds);
                if(!IsFinite(point.x)) return false; anchors.Add(point);
            }
            Vector2 added=FindStationAccessAnchor(newKind,newBounds,newBounds);
            if(!IsFinite(added.x)) return false; anchors.Add(added);
            // Use a genuinely clear access point, not an arbitrary occupied area center.
            foreach(Vector2 point in anchors)
                if(!TryBuildWorkerPath(null,added,point,newBounds,out _)) return false;
            return true;
        }

        private static TowerDefensePlacementResult InvalidPlacement(string reason, Rect bounds) =>
            new TowerDefensePlacementResult(false, reason, bounds, bounds.center);
        private static Rect Expand(Rect rect, float amount) => new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        // The fence service line sits exactly on WorkBounds' top edge on tall screens; tolerate float rounding there.
        private const float ContainmentTolerance = 1e-4f;
        private static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - ContainmentTolerance && inner.xMax <= outer.xMax + ContainmentTolerance &&
            inner.yMin >= outer.yMin - ContainmentTolerance && inner.yMax <= outer.yMax + ContainmentTolerance;
        // An empty rectangle (such as Rect.zero, used for "no extra obstacle") overlaps nothing, even when a body straddles the origin.
        private static bool Overlaps(Rect a, Rect b) => a.width > 0f && a.height > 0f && b.width > 0f && b.height > 0f &&
            a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
    }
}
