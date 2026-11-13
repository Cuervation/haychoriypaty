using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int PilgrimSelectFirst = 7910, PilgrimPlace = 7920;
        private const int PilgrimCardsPerPage = 6;
        private static readonly StreetWorkerRole[] PilgrimWorkerCardRoles =
        {
            StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium,
            StreetWorkerRole.Cocacolero, StreetWorkerRole.Fernetero, StreetWorkerRole.Mozo
        };
        // Index zero is the immediate speed upgrade; the remaining cards hire real staff.
        // Like Boca's footer, Luján has no category switcher or equipment catalogue.
        private int pilgrimSelection = -1;
        private Vector2 pilgrimPlacementPoint;
        private bool IsPilgrimConstruction => towerDefenseMode != null && towerDefenseMode.FreeConstruction;

        private void ResetPilgrimPlacement()
        { pilgrimSelection = -1; pilgrimPlacementPoint = Vector2.zero; }

        private int PilgrimItemCount => PilgrimWorkerCardRoles.Length + 1;
        private Rect PilgrimCardBounds(int visibleIndex) => new Rect(16f + visibleIndex % 3 * 170f,
            logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 80f + visibleIndex / 3 * 74f, 158f, 70f);

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

        private bool CanSelectPilgrimWorker(int cardIndex)
        {
            if (!TowerDefensePlacementInputAllowed || cardIndex <= 0 || cardIndex >= PilgrimItemCount) return false;
            int hireCost = towerDefenseMode.HireCost(PilgrimWorkerCardRoles[cardIndex - 1]);
            return hireCost > 0 && hireCost <= towerDefenseMode.Coins;
        }

        private bool CanUpgradePilgrimSpeed => TowerDefensePlacementInputAllowed && towerDefenseMode.SpeedUpgradeCost > 0 &&
            towerDefenseMode.Coins >= towerDefenseMode.SpeedUpgradeCost;

        private int HitPilgrimAction(Vector2 logical)
        {
            if (!TowerDefensePlacementInputAllowed) return 0;
            for (int cardIndex = 0; cardIndex < PilgrimCardsPerPage; cardIndex++)
            {
                if (!PilgrimCardBounds(cardIndex).Contains(logical)) continue;
                if (cardIndex == 0) return CanUpgradePilgrimSpeed ? TowerDefenseSpeedAction : 0;
                if (pilgrimSelection == cardIndex || CanSelectPilgrimWorker(cardIndex))
                    return PilgrimSelectFirst + cardIndex;
                return 0;
            }
            if (pilgrimSelection > 0 && PilgrimWorldToLogical(towerDefenseMode.WorkBounds).Contains(logical))
            { pilgrimPlacementPoint = PilgrimLogicalToWorld(logical); return PilgrimPlace; }
            return 0;
        }

        private bool HandlePilgrimAction(int action)
        {
            if (action == TowerDefenseSpeedAction)
            {
                if (!CanUpgradePilgrimSpeed) return true;
                towerDefenseError = game.TryUpgradeTowerDefenseSpeed(towerDefenseMode)
                    ? null : "No se pudo guardar la mejora de velocidad.";
                return true;
            }
            if (action >= PilgrimSelectFirst && action < PilgrimSelectFirst + PilgrimCardsPerPage)
            {
                if (!TowerDefensePlacementInputAllowed) return true;
                int selectedCard = action - PilgrimSelectFirst;
                if (selectedCard <= 0) return true;
                if (selectedCard != pilgrimSelection && !CanSelectPilgrimWorker(selectedCard)) return true;
                pilgrimSelection = selectedCard == pilgrimSelection ? -1 : selectedCard;
                pilgrimPlacementPoint = towerDefenseMode.WorkBounds.center;
                towerDefenseError = null;
                return true;
            }
            if (action != PilgrimPlace) return false;
            if (!TowerDefensePlacementInputAllowed || pilgrimSelection <= 0 || pilgrimSelection >= PilgrimItemCount) return true;

            StreetWorkerRole role = PilgrimWorkerCardRoles[pilgrimSelection - 1];
            TowerDefensePlacementResult preview = towerDefenseMode.PreviewWorkerPlacement(role, pilgrimPlacementPoint);
            if (!preview.IsValid || !CanSelectPilgrimWorker(pilgrimSelection))
            {
                towerDefenseError = preview.IsValid ? "No alcanza el saldo para sumar este puesto." : preview.Reason;
                return true;
            }

            pilgrimSelection = -1; // Consume selection before durable command; repeated release cannot hire twice.
            bool saved = game.TryHireTowerDefenseAt(towerDefenseMode, role, pilgrimPlacementPoint);
            towerDefenseError = saved ? null : "No se pudo guardar. La partida quedó en pausa.";
            if (!saved) towerDefenseMode.Pause();
            return true;
        }

        private void DrawPilgrimHud(float height)
        {
            DrawTowerDefenseCampaignHud();
            float top = height - TowerDefenseResponsiveLayout.FooterReserve;
            DrawFooterBluePanel(LayoutRect(new Rect(0, top, W, height - top)), layoutVerticalScale);
            if (towerDefenseCrest != null) DrawLevelCrest(new Rect(8, top + 3, 29, 33), towerDefenseCrest);
            const string levelTitle = "PEREGRINACIÓN LUJÁN";
            int previousTitleSize = towerDefenseTitle.fontSize;
            towerDefenseTitle.fontSize = FitUpgradeStatusFontSize(towerDefenseTitle, levelTitle, W - 84f);
            DrawTowerDefenseLabel(new Rect(42, top + 1, W - 84, 26), levelTitle, towerDefenseTitle);
            towerDefenseTitle.fontSize = previousTitleSize;
            DrawTowerDefenseLabel(new Rect(42, top + 26, W - 84, 18), "MORENO", towerDefenseSmallLabel);

            string hint = !string.IsNullOrEmpty(towerDefenseError) ? towerDefenseError
                : pilgrimSelection > 0 ? "TOCÁ EL ÁREA PARA SUMARLO" : string.Empty;
            if (!string.IsNullOrEmpty(hint))
            {
                int hintSize = towerDefenseSmallLabel.fontSize;
                towerDefenseSmallLabel.fontSize = FitUpgradeStatusFontSize(towerDefenseSmallLabel, hint, W - 162f);
                DrawTowerDefenseLabel(new Rect(42, top + 49, W - 150, 18), hint, towerDefenseSmallLabel);
                towerDefenseSmallLabel.fontSize = hintSize;
            }

            bool active = TowerDefensePlacementInputAllowed;
            for (int cardIndex = 0; cardIndex < PilgrimCardsPerPage; cardIndex++)
            {
                Rect card = PilgrimCardBounds(cardIndex);
                if (cardIndex == 0)
                {
                    int speedCost = towerDefenseMode.SpeedUpgradeCost;
                    DrawPilgrimSpeedCard(card, speedCost, towerDefenseMode.SpeedLevel,
                        active && speedCost > 0 && speedCost <= towerDefenseMode.Coins);
                    continue;
                }

                StreetWorkerRole role = PilgrimWorkerCardRoles[cardIndex - 1];
                int cost = towerDefenseMode.HireCost(role);
                DrawPilgrimWorkerCard(card, PilgrimSelectFirst + cardIndex, role, cost,
                    towerDefenseMode.WorkersCount(role), active && (pilgrimSelection == cardIndex || cost > 0 && cost <= towerDefenseMode.Coins),
                    pilgrimSelection == cardIndex);
            }

            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused)
            {
                DrawTowerDefensePanel(new Rect(75, height * .43f, W - 150, 80), new Color(.015f, .04f, .08f, .92f));
                DrawTowerDefenseLabel(new Rect(85, height * .44f, W - 170, 55), "PARTIDA EN PAUSA", towerDefenseTitle);
            }
        }

        private void DrawPilgrimPlacementOverlay()
        {
            if (!TowerDefensePlacementInputAllowed || pilgrimSelection <= 0) return;
            Rect area = PilgrimWorldToLogical(towerDefenseMode.WorkBounds);
            FillRect(area, new Color(.10f, .55f, .80f, .08f));
            if (Event.current != null && area.Contains(Event.current.mousePosition))
                pilgrimPlacementPoint = PilgrimLogicalToWorld(Event.current.mousePosition);
            StreetWorkerRole role = PilgrimWorkerCardRoles[pilgrimSelection - 1];
            TowerDefensePlacementResult result = towerDefenseMode.PreviewWorkerPlacement(role, pilgrimPlacementPoint);
            Rect preview = PilgrimWorldToLogical(result.Bounds);
            Color color = result.IsValid && CanSelectPilgrimWorker(pilgrimSelection)
                ? new Color(.12f, .9f, .30f, .28f) : new Color(.95f, .12f, .12f, .28f);
            FillRect(preview, color);
            color.a = 1;
            DrawTowerDefensePlacementBorder(preview, color);
        }

        private void DrawPilgrimWorkerCard(Rect bounds, int action, StreetWorkerRole role, int cost,
            int count, bool available, bool selected)
        {
            DrawPilgrimCardBackground(bounds, available, selected);
            Texture2D art = null;
            Rect source = default;
            switch (role)
            {
                case StreetWorkerRole.Parrillero: art = parrilleroIcon; source = new Rect(22, 6, 1269, 1188); break;
                case StreetWorkerRole.ParrilleroPremium: art = premiumWorker; source = ParrilleroPoses[0]; break;
                case StreetWorkerRole.Cocacolero:
                    int level = TowerDefenseLevelConfig.CommonEconomicBalanceLevelId;
                    art = CocacoleroArtForLevel(level); source = CocacoleroPoseForLevel(level, 0); break;
                case StreetWorkerRole.Fernetero:
                    art = fernetWorker;
                    source = art != null ? MatchedCocacoleroPose(0, new Vector2(art.width, art.height)) : default;
                    break;
                case StreetWorkerRole.Mozo: art = mozoWorker; source = ParrilleroPoses[0]; break;
            }
            if (art != null) DrawUpgradeOvalContent(bounds, art, source);
            Rect content = new Rect(bounds.x + 57, bounds.y + 3, bounds.width - 64, bounds.height - 6);
            string title = RoleCaption(role).Replace('\n', ' ');
            DrawPilgrimCardLabel(new Rect(content.x, content.y, content.width, 17), title, upgradeTitle, new Color(.98f, .99f, 1f), 11);
            string status = selected ? "COLOCAR" : "EQUIPO " + count;
            DrawPilgrimCardLabel(new Rect(content.x, content.y + 17, content.width, 16), status, upgradePercent, new Color(.55f, .90f, 1f), 11);
            Item(new Rect(content.x, bounds.y + 48, 18, 18), 10);
            string price = cost > 0 ? cost.ToString() : "MAX";
            DrawPilgrimCardLabel(new Rect(content.x + 20, bounds.y + 43, content.width - 20, 23), price, upgradePrice,
                new Color(.98f, .99f, 1f), 20);
            DrawTowerDefenseClickTarget(bounds, action, available);
        }

        private void DrawPilgrimSpeedCard(Rect bounds, int cost, int speedLevel, bool available)
        {
            DrawPilgrimCardBackground(bounds, available, false);
            if (speedArrows != null) DrawUpgradeOvalContent(bounds, speedArrows, SpeedArrowBounds);
            Rect content = new Rect(bounds.x + 57, bounds.y + 3, bounds.width - 64, bounds.height - 6);
            DrawPilgrimCardLabel(new Rect(content.x, content.y, content.width, 17), "VELOCIDAD", upgradeTitle,
                new Color(.98f, .99f, 1f), 11);
            string multiplier = (1f + speedLevel * .1f).ToString("0.00").Replace('.', ',');
            DrawPilgrimCardLabel(new Rect(content.x, content.y + 17, content.width, 16), "×" + multiplier + "  +10%",
                upgradePercent, new Color(.55f, .90f, 1f), 11);
            if (cost > 0) Item(new Rect(content.x, bounds.y + 48, 18, 18), 10);
            DrawPilgrimCardLabel(new Rect(content.x + 20, bounds.y + 43, content.width - 20, 23), cost > 0 ? cost.ToString() : "MAX",
                upgradePrice, new Color(.98f, .99f, 1f), 20);
            DrawTowerDefenseClickTarget(bounds, TowerDefenseSpeedAction, available);
        }

        private void DrawPilgrimCardBackground(Rect bounds, bool available, bool selected)
        {
            Color previous = GUI.color;
            GUI.color = selected ? new Color(.70f, .84f, 1f, 1f) : available ? Color.white : new Color(.57f, .57f, .57f, 1f);
            Texture2D texture = GetGlossyUpgradeCardTexture(bounds);
            if (texture != null) GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
            else FillRect(bounds, new Color(.025f, .14f, .40f, 1f));
            GUI.color = previous;
            if (selected) DrawTowerDefensePlacementBorder(bounds, new Color(1f, .86f, .18f, 1f));
            else if (available) DrawAffordableUpgradeBorder(bounds);
        }

        private void DrawPilgrimCardLabel(Rect bounds, string value, GUIStyle style, Color color, int maximumFontSize)
        {
            int previous = style.fontSize;
            style.fontSize = Mathf.Min(previous, maximumFontSize);
            style.fontSize = FitUpgradeStatusFontSize(style, value, bounds.width);
            UpgradeOutline(bounds, value, style, color);
            style.fontSize = previous;
        }
    }
}
