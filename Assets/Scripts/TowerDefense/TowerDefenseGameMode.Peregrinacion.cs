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
        public const float ConstructionWorkerWidthLogical = ParrilleroVisualWidthLogical;
        public const float ConstructionWorkerHeightLogical = WorkerVisualHeightLogical;
        private readonly List<TowerDefensePlacedStation> placedStations = new List<TowerDefensePlacedStation>();
        private long equipmentSpendCents;
        private int nextStationId = 1;

        public Rect WorkBounds => config.FreeConstruction ? route.WorkBounds : Rect.zero;
        public IReadOnlyList<TowerDefensePlacedStation> PlacedStations => placedStations;
        public long EquipmentSpendCents => equipmentSpendCents;

        public int StationCost(TowerDefenseStationKind kind)
        {
            if (!config.FreeConstruction || !Enum.IsDefined(typeof(TowerDefenseStationKind), kind)) return 0;
            int alreadyPlaced = 0;
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == kind) alreadyPlaced++;
            return alreadyPlaced < config.FreeStationCount(kind) ? 0 : config.StationCost;
        }

        public bool HasStation(TowerDefenseStationKind kind)
        {
            for (int i = 0; i < placedStations.Count; i++) if (placedStations[i].Kind == kind) return true;
            return false;
        }

        public TowerDefensePlacementResult PreviewStationPlacement(TowerDefenseStationKind kind, Vector2 position)
        {
            Rect bounds = StationFootprint(kind, position);
            if (!config.FreeConstruction) return InvalidPlacement("Este nivel no habilita construcción libre.", bounds);
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
            if (Phase != TowerDefenseRunPhase.Playing) return false;
            TowerDefensePlacementResult preview = PreviewStationPlacement(kind, position);
            if (!preview.IsValid) return false;
            int cost = StationCost(kind);
            if (cost < 0 || coins < cost) return false;
            coins -= cost;
            if (cost > 0)
                equipmentSpendCents = AddCents(equipmentSpendCents, (long)cost * 100L);
            Rect bounds = preview.Bounds;
            Vector2 work = FindStationAccessAnchor(kind, bounds);
            Vector2 pickup = kind == TowerDefenseStationKind.CocaBarrel || kind == TowerDefenseStationKind.BeerBarrel
                ? new Vector2(bounds.center.x, bounds.yMin)
                : new Vector2(bounds.center.x, bounds.yMin - 18f * worldUnitsPerLogicalPixel);
            placed = new TowerDefensePlacedStation(nextStationId++, kind, bounds, work, pickup);
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
            coins -= cost;
            hireSpendCents = AddCents(hireSpendCents, (long)cost * 100L);
            hiresByRole[roleIndex] = hiresByRole[roleIndex] == long.MaxValue ? long.MaxValue : hiresByRole[roleIndex] + 1L;
            workerCounts[roleIndex]++;
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

        private bool HasStationForRole(StreetWorkerRole role)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero: return HasStation(TowerDefenseStationKind.NormalGrill) && HasStation(TowerDefenseStationKind.NormalTable);
                case StreetWorkerRole.ParrilleroPremium: return HasStation(TowerDefenseStationKind.PremiumGrill) && HasStation(TowerDefenseStationKind.PremiumTable);
                case StreetWorkerRole.Cocacolero: return HasStation(TowerDefenseStationKind.CocaBarrel) && HasStation(TowerDefenseStationKind.BeerBarrel);
                case StreetWorkerRole.Fernetero: return HasStation(TowerDefenseStationKind.FernetTable);
                case StreetWorkerRole.Mozo: return HasStation(TowerDefenseStationKind.NormalTable) || HasStation(TowerDefenseStationKind.PremiumTable);
                default: return false;
            }
        }

        private void AdvanceFreeConstructionCooks(float deltaTime, float cookRate)
        {
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
                    // A cooked unit stays attached to its real carrier until the table is reached.
                    if (!WorkerAt(worker, table.WorkPosition)) { QueueFreeWorkerPath(worker, table.WorkPosition); continue; }
                    if (Kitchen.DepositCooked(carried.Id, worker.Id)) worker.CookUnitId = 0;
                    continue;
                }
                if (!WorkerAt(worker, grill.WorkPosition)) { QueueFreeWorkerPath(worker, grill.WorkPosition); continue; }
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
                    QueueFreeWorkerPath(worker, table.WorkPosition);
                }
            }
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
            for (int f = 0; f < serviceOrder.Count; f++)
            {
                TowerDefenseFanState fan = serviceOrder[f];
                if (fan.OrderComplete || fan.RouteFollower.HasExited || fan.ReservedByWorker != 0) continue;
                for (int line = 0; line < fan.Order.Count; line++)
                {
                    TowerDefenseOrderLine order = (TowerDefenseOrderLine)fan.Order[line];
                    if (order.Remaining <= 0) continue;
                    StreetWorkerRole role = GetDeliveryRole(order.Product);
                    TowerDefenseWorkerState worker = FindWorkerForFreeService(role);
                    if (worker == null || !HasStationForRole(role)) continue;
                    worker.PendingFanId = fan.Id; worker.PendingProduct = order.Product; worker.ServiceStage = 1;
                    fan.ReservedByWorker = worker.Id;
                    if (!QueueWorkerToPickup(worker)) ReleaseFreeService(worker, fan);
                    break;
                }
            }
        }

        private TowerDefenseWorkerState FindWorkerForFreeService(StreetWorkerRole role)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                TowerDefenseWorkerState worker = workers[i];
                if (worker.Role == role && !worker.IsMoving && worker.ServiceStage == 0 && worker.DeliveryCooldownSeconds <= 0f) return worker;
            }
            return null;
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
                StreetFoodUnit unit;
                if (worker.PendingProduct < 4)
                {
                    Kitchen.ReserveTableUnit(worker.PendingProduct, worker.Id);
                    unit = Kitchen.TryTake(worker.PendingProduct, worker.Role, worker.Id);
                }
                else unit = Kitchen.TryTake(worker.PendingProduct, worker.Role, worker.Id);
                if (unit == null) { worker.ServiceStage = 3; return; }
                worker.ServiceUnitId = unit.Id; worker.ServiceStage = 2;
                if (!TryBuildWorkerPath(worker, worker.Position, GetFanServiceAnchor(fan), Rect.zero, out Vector2[] path))
                {
                    Kitchen.ReturnCarried(unit.Id, worker.Id); ReleaseFreeService(worker, fan); return;
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
            if (fan != null && Mathf.Abs(worker.Position.y - GetFanServiceAnchor(fan).y) > 2f * worldUnitsPerLogicalPixel)
            { QueueFreeWorkerPath(worker,GetFanServiceAnchor(fan)); return; }
            if (fan != null && !fan.RouteFollower.HasExited &&
                Mathf.Abs(worker.Position.x - fan.RouteFollower.Position.x) > 64f * worldUnitsPerLogicalPixel)
            {
                QueueFreeWorkerPath(worker, GetFanServiceAnchor(fan));
                return;
            }
            if (fan == null || !fan.NeedsProduct(product) || !Kitchen.Consume(unitId, product, worker.Id))
            {
                if (unitId > 0) Kitchen.ReturnCarried(unitId, worker.Id);
                ReleaseFreeService(worker, fan); return;
            }
            if (fan.RecordDelivery(product))
            {
                if (!fan.HasReceivedDelivery) { fan.HasReceivedDelivery = true; if (customersAttended < int.MaxValue) customersAttended++; }
                fan.IsReceiving = true; fan.ReceiveAnimationSeconds = .35f;
                deliveredByProduct[product]++; TotalDelivered++;
                int sale = Mathf.RoundToInt(StreetSimulation.OfficialProductPrice(product));
                coins = coins > int.MaxValue - sale ? int.MaxValue : coins + sale;
                coinsEarned = coinsEarned > int.MaxValue - sale ? int.MaxValue : coinsEarned + sale;
                if (fan.OrderComplete && !fan.CompletionCounted)
                { fan.CompletionCounted = true; if (ordersCompleted < int.MaxValue) ordersCompleted++; fan.IsDeparting = true; fan.DepartureProgress = 0f; }
                worker.DeliveryCooldownSeconds = DeliveryIntervalSeconds; worker.ServeAnimationSeconds = DeliveryCueDurationSeconds;
                worker.LastServiceTarget = fan.RouteFollower.Position;
                DeliveryCompleted?.Invoke(new TowerDefenseDelivery(unitId, product, worker.Id, fan.Id, 0, worker.Position, fan.RouteFollower.Position, elapsed));
            }
            ReleaseFreeService(worker, fan);
        }

        private void ReleaseFreeService(TowerDefenseWorkerState worker, TowerDefenseFanState fan)
        {
            if (fan != null && fan.ReservedByWorker == worker.Id) fan.ReservedByWorker = 0;
            worker.PendingFanId = 0; worker.PendingProduct = -1; worker.ServiceStage = 0; worker.ServiceUnitId = 0;
        }

        private TowerDefenseFanState FindFan(int id)
        { for (int i = 0; i < fans.Count; i++) if (fans[i].Id == id) return fans[i]; return null; }

        private Vector2 GetFanServiceAnchor(TowerDefenseFanState fan)
        {
            float pixel = worldUnitsPerLogicalPixel;
            float y = routeGeometry.RightBoundary[0].y - (ConstructionWorkerHeightLogical * (1f - WorkerVisualPivotY) + 8f) * pixel;
            float half = CocacoleroVisualWidthLogical * .5f * pixel;
            return new Vector2(Mathf.Clamp(fan.RouteFollower.Position.x, WorkBounds.xMin + half, WorkBounds.xMax - half), y);
        }

        private static TowerDefenseStationKind StationForProduct(int product)
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

        private Rect StationFootprint(TowerDefenseStationKind kind, Vector2 position)
        {
            Vector2 sizeLogical;
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
            Vector2 center = Snap(position);
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
            float workerWidth = kind == TowerDefenseStationKind.CocaBarrel || kind == TowerDefenseStationKind.BeerBarrel ? 94f : ConstructionWorkerWidthLogical;
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
                StreetWorkerRole accessRole = kind == TowerDefenseStationKind.CocaBarrel || kind == TowerDefenseStationKind.BeerBarrel
                    ? StreetWorkerRole.Cocacolero : StreetWorkerRole.Parrillero;
                Rect worker = WorkerBounds(candidates[i], accessRole);
                if (!Contains(WorkBounds, worker) || Overlaps(worker, bounds) || Overlaps(worker, extraObstacle)) continue;
                bool clear = true;
                for (int s = 0; clear && s < placedStations.Count; s++) clear = !Overlaps(worker, placedStations[s].Bounds);
                if (clear) return candidates[i];
            }
            return new Vector2(float.NaN, float.NaN);
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
            worker.TransferRoute = path;
            worker.TransferIndex = 0;
            worker.IsMoving = path.Length > 0;
            if (!worker.IsMoving) worker.Position = target;
            return true;
        }

        private bool FreePositionClear(Vector2 position, StreetWorkerRole role, TowerDefenseWorkerState ignore, Rect extra)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y)) return false;
            Rect body = WorkerBounds(position, role);
            if (!Contains(WorkBounds, body) || Overlaps(body, extra)) return false;
            foreach (var station in placedStations) if (Overlaps(body, station.Bounds)) return false;
            foreach (var worker in workers) if (worker != ignore && Overlaps(body, WorkerBounds(worker.Position, worker.Role))) return false;
            return true;
        }
        private bool FreeSegmentClear(Vector2 from, Vector2 to, StreetWorkerRole role, TowerDefenseWorkerState ignore, Rect extra)
        {
            if (!FreePositionClear(from, role, ignore, extra) || !FreePositionClear(to, role, ignore, extra)) return false;
            Rect a = WorkerBounds(from, role), b = WorkerBounds(to, role);
            Rect swept = Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin), Mathf.Min(a.yMin,b.yMin), Mathf.Max(a.xMax,b.xMax), Mathf.Max(a.yMax,b.yMax));
            if (Overlaps(swept, extra)) return false;
            foreach (var station in placedStations) if (Overlaps(swept, station.Bounds)) return false;
            foreach (var worker in workers) if (worker != ignore && Overlaps(swept, WorkerBounds(worker.Position, worker.Role))) return false;
            return true;
        }
        private bool TryBuildWorkerPath(TowerDefenseWorkerState movingWorker, Vector2 start, Vector2 target,
            Rect extraObstacle, out Vector2[] path, StreetWorkerRole pathRole = StreetWorkerRole.Parrillero)
        {
            path = Array.Empty<Vector2>();
            StreetWorkerRole role = movingWorker == null ? pathRole : movingWorker.Role;
            if (!FreePositionClear(start, role, movingWorker, extraObstacle) || !FreePositionClear(target, role, movingWorker, extraObstacle)) return false;
            if (Vector2.Distance(start, target) <= worldUnitsPerLogicalPixel * .1f) return true;
            if (FreeSegmentClear(start,target,role,movingWorker,extraObstacle)) { path = new[] {target}; return true; }
            float step = ConstructionGridLogical * worldUnitsPerLogicalPixel;
            int columns = Mathf.Max(1, Mathf.FloorToInt(WorkBounds.width / step) + 1);
            int rows = Mathf.Max(1, Mathf.FloorToInt(WorkBounds.height / step) + 1), total = columns * rows;
            var points = new Vector2[total]; var open = new bool[total]; var previous = new int[total];
            var queue = new Queue<int>();
            float attachDistance = step * 1.5f;
            for (int y=0;y<rows;y++) for(int x=0;x<columns;x++)
            {
                int id=y*columns+x; points[id]=WorkBounds.min+new Vector2(x*step,y*step); previous[id]=-2;
                open[id]=FreePositionClear(points[id],role,movingWorker,extraObstacle);
                if (open[id] && Vector2.Distance(start,points[id]) <= attachDistance && FreeSegmentClear(start,points[id],role,movingWorker,extraObstacle))
                { previous[id]=-1; queue.Enqueue(id); }
            }
            int reached=-1;
            while(queue.Count>0)
            {
                int current=queue.Dequeue(), x=current%columns, y=current/columns;
                if(Vector2.Distance(points[current],target)<=attachDistance && FreeSegmentClear(points[current],target,role,movingWorker,extraObstacle)) { reached=current; break; }
                Enqueue(x-1,y,current); Enqueue(x+1,y,current); Enqueue(x,y-1,current); Enqueue(x,y+1,current);
            }
            if(reached<0) return false;
            var reverse=new List<Vector2>();
            for(int node=reached;node>=0;node=previous[node]) reverse.Add(points[node]);
            reverse.Reverse(); reverse.Add(target); path=reverse.ToArray(); return true;
            void Enqueue(int x,int y,int from)
            {
                if(x<0||x>=columns||y<0||y>=rows) return;
                int next=y*columns+x;
                if(!open[next]||previous[next]!=-2||!FreeSegmentClear(points[from],points[next],role,movingWorker,extraObstacle)) return;
                previous[next]=from; queue.Enqueue(next);
            }
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
        private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        private static bool Overlaps(Rect a, Rect b) => a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
    }
}
