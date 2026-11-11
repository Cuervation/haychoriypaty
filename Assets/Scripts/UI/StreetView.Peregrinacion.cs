using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int PilgrimTabWorkers = 7900, PilgrimTabEquipment = 7901, PilgrimNextPage = 7902;
        private const int PilgrimSelectFirst = 7910, PilgrimPlace = 7920;
        private bool pilgrimEquipmentTab;
        private int pilgrimPage, pilgrimSelection = -1;
        private Vector2 pilgrimPlacementPoint;
        private static readonly string[] PilgrimEquipmentNames = { "PARRILLA", "PARRILLA PREMIUM", "COCA", "FERNET", "CERVEZA", "MESA CHORI/PATY", "MESA PREMIUM" };
        private bool IsPilgrimConstruction => towerDefenseMode != null && towerDefenseMode.FreeConstruction;

        private void ResetPilgrimPlacement()
        { pilgrimSelection = -1; pilgrimPage = 0; pilgrimEquipmentTab = false; pilgrimPlacementPoint = Vector2.zero; }

        private int PilgrimItemCount => pilgrimEquipmentTab ? 7 : 5;
        private Rect PilgrimCardBounds(int visibleIndex) => new Rect(16f + visibleIndex * 170f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 80f, 158f, 120f);
        private Rect PilgrimTabBounds(int tab) => new Rect(16f + tab * 174f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 41f, 164f, 33f);
        private Rect PilgrimNextBounds() => new Rect(366f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 41f, 158f, 33f);
        private Rect PilgrimPauseBounds() => new Rect(W - 102f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 5f, 44f, 31f);
        private Rect PilgrimExitBounds() => new Rect(W - 52f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 5f, 40f, 31f);

        private Vector2 PilgrimLogicalToWorld(Vector2 logical)
        {
            Camera camera = Camera.main;
            Rect viewport = CanvasViewport(TowerDefenseRenderSize(camera), Screen.safeArea);
            // The overlay is displayed lifted by this amount; invert that visual transform for input.
            logical.y += TowerDefenseResponsiveLayout.VisualStageLift;
            return TowerDefenseResponsiveLayout.LogicalToWorld(camera, viewport, W, viewport.width / W, logical);
        }
        private Rect PilgrimWorldToLogical(Rect world)
        {
            Camera camera = Camera.main;
            Rect viewport = CanvasViewport(TowerDefenseRenderSize(camera), Screen.safeArea);
            float scale = viewport.width / W;
            Vector3 a = camera.WorldToScreenPoint(new Vector3(world.xMin, world.yMax, 0));
            Vector3 b = camera.WorldToScreenPoint(new Vector3(world.xMax, world.yMin, 0));
            return Rect.MinMaxRect((a.x - viewport.x) / scale,
                (camera.pixelHeight - a.y - viewport.y) / scale - TowerDefenseResponsiveLayout.VisualStageLift,
                (b.x - viewport.x) / scale,
                (camera.pixelHeight - b.y - viewport.y) / scale - TowerDefenseResponsiveLayout.VisualStageLift);
        }
        private bool CanSelectPilgrimItem(int item)
        {
            if (!TowerDefensePlacementInputAllowed || item < 0 || item >= PilgrimItemCount) return false;
            int cost = pilgrimEquipmentTab ? towerDefenseMode.StationCost((TowerDefenseStationKind)item)
                : towerDefenseMode.HireCost((StreetWorkerRole)item);
            return cost >= (pilgrimEquipmentTab ? 0 : 1) && cost <= towerDefenseMode.Coins;
        }
        private int HitPilgrimAction(Vector2 logical)
        {
            if (PilgrimExitBounds().Contains(logical)) return TowerDefenseExitAction;
            if (PilgrimPauseBounds().Contains(logical)) return TowerDefensePauseAction;
            if (!TowerDefensePlacementInputAllowed) return 0;
            if (PilgrimTabBounds(0).Contains(logical)) return PilgrimTabWorkers;
            if (PilgrimTabBounds(1).Contains(logical)) return PilgrimTabEquipment;
            if (PilgrimNextBounds().Contains(logical)) return PilgrimNextPage;
            for (int i = 0; i < 3; i++)
                if (pilgrimPage * 3 + i < PilgrimItemCount && PilgrimCardBounds(i).Contains(logical) &&
                    (pilgrimSelection == pilgrimPage * 3 + i || CanSelectPilgrimItem(pilgrimPage * 3 + i))) return PilgrimSelectFirst + i;
            if (pilgrimSelection >= 0 && PilgrimWorldToLogical(towerDefenseMode.WorkBounds).Contains(logical))
            { pilgrimPlacementPoint = PilgrimLogicalToWorld(logical); return PilgrimPlace; }
            return 0;
        }
        private bool HandlePilgrimAction(int action)
        {
            if (action == PilgrimTabWorkers || action == PilgrimTabEquipment)
            { pilgrimEquipmentTab = action == PilgrimTabEquipment; pilgrimSelection = -1; pilgrimPage = 0; return true; }
            if (action == PilgrimNextPage)
            { pilgrimPage = (pilgrimPage + 1) % ((PilgrimItemCount + 2) / 3); pilgrimSelection = -1; return true; }
            if (action >= PilgrimSelectFirst && action < PilgrimSelectFirst + 3)
            {
                if (!TowerDefensePlacementInputAllowed) return true;
                int selected = pilgrimPage * 3 + action - PilgrimSelectFirst;
                if (selected != pilgrimSelection && !CanSelectPilgrimItem(selected)) return true;
                pilgrimSelection = selected == pilgrimSelection ? -1 : selected;
                pilgrimPlacementPoint = towerDefenseMode.WorkBounds.center;
                towerDefenseError = null; return true;
            }
            if (action != PilgrimPlace) return false;
            if (!TowerDefensePlacementInputAllowed || pilgrimSelection < 0 || pilgrimSelection >= PilgrimItemCount) return true;
            int choice = pilgrimSelection;
            TowerDefensePlacementResult preview = pilgrimEquipmentTab
                ? towerDefenseMode.PreviewStationPlacement((TowerDefenseStationKind)choice, pilgrimPlacementPoint)
                : towerDefenseMode.PreviewWorkerPlacement((StreetWorkerRole)choice, pilgrimPlacementPoint);
            if (!preview.IsValid || !CanSelectPilgrimItem(choice)) { towerDefenseError = preview.IsValid ? "No alcanza el saldo para colocarlo." : preview.Reason; return true; }
            pilgrimSelection = -1; // Consume selection before durable command; repeated release cannot purchase twice.
            bool saved = pilgrimEquipmentTab
                ? game.TryPlaceTowerDefenseStation(towerDefenseMode, (TowerDefenseStationKind)choice, pilgrimPlacementPoint)
                : game.TryHireTowerDefenseAt(towerDefenseMode, (StreetWorkerRole)choice, pilgrimPlacementPoint);
            towerDefenseError = saved ? null : "No se pudo colocar o guardar. La partida quedó en pausa.";
            if (!saved) towerDefenseMode.Pause();
            return true;
        }
        private void DrawPilgrimHud(float height)
        {
            DrawTowerDefenseCampaignHud();
            float top = height - TowerDefenseResponsiveLayout.FooterReserve;
            DrawFooterBluePanel(LayoutRect(new Rect(0, top, W, height - top)), layoutVerticalScale);
            if (towerDefenseCrest != null) DrawLevelCrest(new Rect(8, top + 3, 29, 33), towerDefenseCrest);
            DrawTowerDefenseLabel(new Rect(42, top + 4, W - 150, 30), "PEREGRINACIÓN LUJÁN", towerDefenseSmallLabel);
            DrawStandardButton(PilgrimPauseBounds(), TowerDefensePauseAction, towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "▶" : "II");
            DrawStandardButton(PilgrimExitBounds(), TowerDefenseExitAction, "↩");
            DrawStandardButton(PilgrimTabBounds(0), PilgrimTabWorkers, "TRABAJADORES");
            DrawStandardButton(PilgrimTabBounds(1), PilgrimTabEquipment, "EQUIPAMIENTO");
            DrawStandardButton(PilgrimNextBounds(), PilgrimNextPage, pilgrimPage == (PilgrimItemCount - 1) / 3 ? "‹  INICIO" : "MÁS  ›");
            bool active = TowerDefensePlacementInputAllowed;
            for (int i = 0; i < 3; i++)
            {
                int item = pilgrimPage * 3 + i;
                if (item >= PilgrimItemCount) break;
                Rect card = PilgrimCardBounds(i);
                if (!pilgrimEquipmentTab)
                {
                    var role = (StreetWorkerRole)item;
                    int cost = towerDefenseMode.HireCost(role);
                    DrawTowerDefenseHireCard(card, PilgrimSelectFirst + i, role, cost,
                        towerDefenseMode.WorkersCount(role), active && cost > 0 && cost <= towerDefenseMode.Coins,
                        pilgrimSelection == item);
                }
                else
                {
                    FillRect(card, new Color(.025f, .14f, .40f));
                    var tex = GetGlossyUpgradeCardTexture(new Rect(0, 0, card.width, card.height));
                    if (tex != null) GUI.DrawTexture(card, tex, ScaleMode.StretchToFill, true);
                    if (pilgrimSelection == item) DrawTowerDefensePlacementBorder(card, Color.yellow);
                    DrawTowerDefenseLabel(new Rect(card.x + 4, card.y + 4, card.width - 8, 36), PilgrimEquipmentNames[item], towerDefenseSmallLabel);
                    int product = item == 0 || item == 5 ? 0 : item == 1 || item == 6 ? 2 : item == 2 ? 4 : item == 3 ? 5 : 6;
                    DrawProductIcon(new Rect(card.center.x - 22, card.y + 42, 44, 35), product, false);
                    int cost = towerDefenseMode.StationCost((TowerDefenseStationKind)item);
                    DrawTowerDefenseLabel(new Rect(card.x + 3, card.yMax - 34, card.width - 6, 31), cost == 0 ? "PRIMERA GRATIS" : "$ " + cost, towerDefenseSmallLabel);
                    DrawTowerDefenseClickTarget(card, PilgrimSelectFirst + i, active && cost <= towerDefenseMode.Coins);
                }
            }
            string hint = !string.IsNullOrEmpty(towerDefenseError) ? towerDefenseError : pilgrimSelection >= 0
                ? "TOCÁ UNA POSICIÓN LIBRE BAJO LA VALLA" : "ATENDIDOS " + towerDefenseMode.ServedFans + "/" + towerDefenseMode.TotalFanCount;
            DrawTowerDefenseLabel(new Rect(12, top + 204, W - 24, 33), hint, towerDefenseSmallLabel);
            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused)
            {
                DrawTowerDefensePanel(new Rect(75, height * .43f, W - 150, 80), new Color(.015f, .04f, .08f, .92f));
                DrawTowerDefenseLabel(new Rect(85, height * .44f, W - 170, 55), "PARTIDA EN PAUSA", towerDefenseTitle);
            }
        }
        private void DrawPilgrimPlacementOverlay()
        {
            if (!TowerDefensePlacementInputAllowed || pilgrimSelection < 0) return;
            Rect area = PilgrimWorldToLogical(towerDefenseMode.WorkBounds);
            FillRect(area, new Color(.10f, .55f, .80f, .08f));
            if (Event.current != null && area.Contains(Event.current.mousePosition))
                pilgrimPlacementPoint = PilgrimLogicalToWorld(Event.current.mousePosition);
            var result = pilgrimEquipmentTab
                ? towerDefenseMode.PreviewStationPlacement((TowerDefenseStationKind)pilgrimSelection, pilgrimPlacementPoint)
                : towerDefenseMode.PreviewWorkerPlacement((StreetWorkerRole)pilgrimSelection, pilgrimPlacementPoint);
            Rect preview = PilgrimWorldToLogical(result.Bounds);
            Color color = result.IsValid && CanSelectPilgrimItem(pilgrimSelection) ? new Color(.12f, .9f, .30f, .28f) : new Color(.95f, .12f, .12f, .28f);
            FillRect(preview, color); color.a = 1;
            DrawTowerDefensePlacementBorder(preview, color);
        }
    }
}
