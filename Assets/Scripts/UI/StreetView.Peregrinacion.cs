using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int PilgrimTabWorkers = 7900, PilgrimTabEquipment = 7901, PilgrimNextPage = 7902;
        private const int PilgrimSelectFirst = 7910, PilgrimPlace = 7920;
        private const int PilgrimCardsPerPage = 6;
        private static readonly StreetWorkerRole[] PilgrimWorkerCardRoles =
        {
            StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Cocacolero,
            StreetWorkerRole.Fernetero, StreetWorkerRole.Mozo
        };
        private bool pilgrimEquipmentTab;
        private int pilgrimPage, pilgrimSelection = -1;
        private Vector2 pilgrimPlacementPoint;
        private static readonly string[] PilgrimEquipmentNames = { "PARRILLA", "PARRILLA PREMIUM", "COCA", "FERNET", "CERVEZA", "MESA CHORI/PATY", "MESA PREMIUM" };
        private static readonly string[] PilgrimEquipmentPrefabs =
        {
            "Station_NormalGrill", "Station_PremiumGrill", "Station_CocaBarrel", "Station_FernetTable",
            "Station_BeerBarrel", "Station_NormalTable", "Station_PremiumTable"
        };
        private readonly Sprite[] pilgrimEquipmentSprites = new Sprite[7];
        private bool pilgrimEquipmentSpritesLoaded;
        private bool IsPilgrimConstruction => towerDefenseMode != null && towerDefenseMode.FreeConstruction;

        private void ResetPilgrimPlacement()
        { pilgrimSelection = -1; pilgrimPage = 0; pilgrimEquipmentTab = false; pilgrimPlacementPoint = Vector2.zero; }

        private int PilgrimItemCount => pilgrimEquipmentTab ? 7 : PilgrimWorkerCardRoles.Length + 1;
        private int PilgrimPageCount => Mathf.Max(1, (PilgrimItemCount + PilgrimCardsPerPage - 1) / PilgrimCardsPerPage);
        private Rect PilgrimCardBounds(int visibleIndex)
        {
            // The seventh equipment item occupies the center of its otherwise-empty page.
            int column = pilgrimEquipmentTab && pilgrimPage == 1 && visibleIndex == 0 && PilgrimItemCount == 7
                ? 1 : visibleIndex % 3;
            return new Rect(16f + column * 170f,
                logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve + 80f + visibleIndex / 3 * 74f, 158f, 70f);
        }
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
            if (pilgrimEquipmentTab)
            {
                return towerDefenseMode.EquipmentPlacementAllowed;
            }
            if (item == 0) return false; // Speed buys immediately; it is not a placement mode.
            int hireCost = towerDefenseMode.HireCost(PilgrimWorkerCardRoles[item - 1]);
            return hireCost > 0 && hireCost <= towerDefenseMode.Coins;
        }
        private bool CanUpgradePilgrimSpeed => TowerDefensePlacementInputAllowed && towerDefenseMode.SpeedUpgradeCost > 0 &&
            towerDefenseMode.Coins >= towerDefenseMode.SpeedUpgradeCost;
        private int HitPilgrimAction(Vector2 logical)
        {
            if (PilgrimExitBounds().Contains(logical)) return TowerDefenseExitAction;
            if (PilgrimPauseBounds().Contains(logical)) return TowerDefensePauseAction;
            if (!TowerDefensePlacementInputAllowed) return 0;
            if (PilgrimTabBounds(0).Contains(logical)) return PilgrimTabWorkers;
            if (PilgrimTabBounds(1).Contains(logical)) return PilgrimTabEquipment;
            if (PilgrimNextBounds().Contains(logical)) return pilgrimEquipmentTab ? PilgrimNextPage : PilgrimTabEquipment;
            for (int i = 0; i < PilgrimCardsPerPage; i++)
            {
                int item = pilgrimPage * PilgrimCardsPerPage + i;
                if (item >= PilgrimItemCount || !PilgrimCardBounds(i).Contains(logical)) continue;
                if (!pilgrimEquipmentTab && item == 0) return CanUpgradePilgrimSpeed ? TowerDefenseSpeedAction : 0;
                if (pilgrimSelection == item || CanSelectPilgrimItem(item)) return PilgrimSelectFirst + i;
            }
            if (pilgrimSelection >= 0 && PilgrimWorldToLogical(towerDefenseMode.WorkBounds).Contains(logical))
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
            if (action == PilgrimTabWorkers || action == PilgrimTabEquipment)
            { pilgrimEquipmentTab = action == PilgrimTabEquipment; pilgrimSelection = -1; pilgrimPage = 0; return true; }
            if (action == PilgrimNextPage)
            { pilgrimPage = (pilgrimPage + 1) % PilgrimPageCount; pilgrimSelection = -1; return true; }
            if (action >= PilgrimSelectFirst && action < PilgrimSelectFirst + PilgrimCardsPerPage)
            {
                if (!TowerDefensePlacementInputAllowed) return true;
                int selected = pilgrimPage * PilgrimCardsPerPage + action - PilgrimSelectFirst;
                if (pilgrimEquipmentTab && !towerDefenseMode.EquipmentPlacementAllowed) return true;
                if (!pilgrimEquipmentTab && selected == 0) return true;
                if (selected != pilgrimSelection && !CanSelectPilgrimItem(selected)) return true;
                pilgrimSelection = selected == pilgrimSelection ? -1 : selected;
                pilgrimPlacementPoint = towerDefenseMode.WorkBounds.center;
                towerDefenseError = null; return true;
            }
            if (action != PilgrimPlace) return false;
            if (!TowerDefensePlacementInputAllowed || pilgrimSelection < 0 || pilgrimSelection >= PilgrimItemCount) return true;
            if (pilgrimEquipmentTab && !towerDefenseMode.EquipmentPlacementAllowed)
            { pilgrimSelection = -1; return true; }
            int choice = pilgrimSelection;
            TowerDefensePlacementResult preview = pilgrimEquipmentTab
                ? towerDefenseMode.PreviewStationPlacement((TowerDefenseStationKind)choice, pilgrimPlacementPoint)
                : towerDefenseMode.PreviewWorkerPlacement(PilgrimWorkerCardRoles[choice - 1], pilgrimPlacementPoint);
            if (!preview.IsValid || !CanSelectPilgrimItem(choice)) { towerDefenseError = preview.IsValid ? "No alcanza el saldo para colocarlo." : preview.Reason; return true; }
            pilgrimSelection = -1; // Consume selection before durable command; repeated release cannot purchase twice.
            bool saved = pilgrimEquipmentTab
                ? game.TryPlaceTowerDefenseStation(towerDefenseMode, (TowerDefenseStationKind)choice, pilgrimPlacementPoint)
                : game.TryHireTowerDefenseAt(towerDefenseMode, PilgrimWorkerCardRoles[choice - 1], pilgrimPlacementPoint);
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
            DrawTowerDefenseLabel(new Rect(42, top + 1, W - 150, 23), "PEREGRINACIÓN LUJÁN", towerDefenseSmallLabel);
            DrawStandardButton(PilgrimPauseBounds(), TowerDefensePauseAction, towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "▶" : "II");
            DrawStandardButton(PilgrimExitBounds(), TowerDefenseExitAction, "↩");
            string hint = !string.IsNullOrEmpty(towerDefenseError) ? towerDefenseError : pilgrimSelection >= 0
                ? "TOCÁ UNA UBICACIÓN LIBRE BAJO LA VALLA" : pilgrimEquipmentTab
                    ? "TODO EL EQUIPAMIENTO ESTÁ INSTALADO" : "ATENDIDOS " + towerDefenseMode.ServedFans + "/" + towerDefenseMode.TotalFanCount;
            int hintSize = towerDefenseSmallLabel.fontSize;
            towerDefenseSmallLabel.fontSize = FitUpgradeStatusFontSize(towerDefenseSmallLabel, hint, W - 162f);
            DrawTowerDefenseLabel(new Rect(42, top + 21, W - 150, 18), hint, towerDefenseSmallLabel);
            towerDefenseSmallLabel.fontSize = hintSize;
            DrawStandardButton(PilgrimTabBounds(0), PilgrimTabWorkers, "TRABAJADORES");
            DrawStandardButton(PilgrimTabBounds(1), PilgrimTabEquipment, "EQUIPAMIENTO");
            string pageLabel = pilgrimEquipmentTab
                ? (pilgrimPage == PilgrimPageCount - 1 ? "‹  INICIO" : "MÁS  ›")
                : "ESTACIONES  ›";
            DrawStandardButton(PilgrimNextBounds(), pilgrimEquipmentTab ? PilgrimNextPage : PilgrimTabEquipment, pageLabel);
            bool active = TowerDefensePlacementInputAllowed;
            for (int i = 0; i < PilgrimCardsPerPage; i++)
            {
                int item = pilgrimPage * PilgrimCardsPerPage + i;
                if (item >= PilgrimItemCount) break;
                Rect card = PilgrimCardBounds(i);
                if (!pilgrimEquipmentTab)
                {
                    if (item == 0)
                    {
                        int speedCost = towerDefenseMode.SpeedUpgradeCost;
                        DrawPilgrimSpeedCard(card, speedCost, towerDefenseMode.SpeedLevel, active && speedCost > 0 && speedCost <= towerDefenseMode.Coins);
                        continue;
                    }
                    StreetWorkerRole role = PilgrimWorkerCardRoles[item - 1];
                    int cost = towerDefenseMode.HireCost(role);
                    DrawPilgrimWorkerCard(card, PilgrimSelectFirst + i, role, cost,
                        towerDefenseMode.WorkersCount(role), active && (pilgrimSelection == item || cost > 0 && cost <= towerDefenseMode.Coins),
                        pilgrimSelection == item);
                }
                else
                {
                    DrawPilgrimEquipmentCard(card, item);
                }
            }
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
                : towerDefenseMode.PreviewWorkerPlacement(PilgrimWorkerCardRoles[pilgrimSelection - 1], pilgrimPlacementPoint);
            Rect preview = PilgrimWorldToLogical(result.Bounds);
            Color color = result.IsValid && CanSelectPilgrimItem(pilgrimSelection) ? new Color(.12f, .9f, .30f, .28f) : new Color(.95f, .12f, .12f, .28f);
            FillRect(preview, color); color.a = 1;
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

        private void DrawPilgrimEquipmentCard(Rect bounds, int item)
        {
            DrawPilgrimCardBackground(bounds, false, false);
            DrawPilgrimStationIcon(new Rect(bounds.x + 7, bounds.y + 7, 44, 54), item);
            Rect content = new Rect(bounds.x + 55, bounds.y + 6, bounds.width - 61, bounds.height - 12);
            DrawPilgrimCardLabel(new Rect(content.x, content.y, content.width, 28), PilgrimEquipmentNames[item],
                upgradeTitle, new Color(.98f, .99f, 1f), 10);
            DrawPilgrimCardLabel(new Rect(content.x, content.y + 31, content.width, 24), "INSTALADO",
                upgradePercent, new Color(.45f, 1f, .72f), 16);
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

        private Sprite PilgrimEquipmentSprite(int item)
        {
            if (!pilgrimEquipmentSpritesLoaded)
            {
                for (int i = 0; i < PilgrimEquipmentPrefabs.Length; i++)
                {
                    GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + PilgrimEquipmentPrefabs[i]);
                    SpriteRenderer renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
                    pilgrimEquipmentSprites[i] = renderer != null ? renderer.sprite : null;
                }
                pilgrimEquipmentSpritesLoaded = true;
            }
            return item >= 0 && item < pilgrimEquipmentSprites.Length ? pilgrimEquipmentSprites[item] : null;
        }

        private void DrawPilgrimStationIcon(Rect bounds, int item)
        {
            Sprite sprite = PilgrimEquipmentSprite(item);
            if (sprite == null || sprite.texture == null) return;
            try
            {
                Rect source = sprite.textureRect;
                Vector2 size = source.size;
                float scale = Mathf.Min(bounds.width / Mathf.Max(1f, size.x), bounds.height / Mathf.Max(1f, size.y));
                Rect destination = new Rect(bounds.center - size * (scale * .5f), size * scale);
                Rect uv = new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
                    source.width / sprite.texture.width, source.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(destination, sprite.texture, uv, true);
            }
            catch (UnityException)
            {
                // If an atlas has a non-readable texture rect, the explicit station name still
                // identifies the item and placement uses the same serialized prefab.
            }
        }
    }
}
