using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int PilgrimSelectFirst = 7910;
        private const int PilgrimCardsPerPage = 6;
        // The hire cards are the ones of the copied campaign footer: its catalog actions after "Velocidad" (3). Boca: five
        // roles; Nueva Chicago: Parrillero and Cocacolero; Ferro: Parrillero, Parrillero Premium and Cocacolero.
        private StreetWorkerRole[] PilgrimWorkerCardRoles
        {
            get
            {
                int level = PilgrimFooterReferenceLevel(PilgrimSourceLevelId);
                if (pilgrimCardRolesLevel != level || pilgrimCardRoles == null)
                {
                    var sim = new StreetSimulation(new StreetBalance(), level, 5f, 0, 1, 0);
                    var roles = new System.Collections.Generic.List<StreetWorkerRole>();
                    foreach (int action in CatalogUpgradeActions(sim)) if (action != 3) roles.Add(RoleForAction(action));
                    pilgrimCardRoles = roles.ToArray();
                    pilgrimCardRolesLevel = level;
                }
                return pilgrimCardRoles;
            }
        }
        private static StreetWorkerRole[] pilgrimCardRoles;
        private static int pilgrimCardRolesLevel = -1;
        // The campaign level whose kitchen, footer and caption this level copies (Boca for Luján, Nueva Chicago for Camioneros).
        private int PilgrimSourceLevelId => towerDefenseSourceConfig != null
            ? towerDefenseSourceConfig.SourceCampaignLevelId : TowerDefenseLevelConfig.PeregrinacionLujanCampaignLevelId;
        // Index zero is the immediate speed upgrade; the remaining cards hire real staff.
        // Like Boca's footer, Luján has no category switcher or equipment catalogue.
        private bool IsPilgrimConstruction => towerDefenseMode != null && towerDefenseMode.FreeConstruction;

        private int PilgrimItemCount => PilgrimWorkerCardRoles.Length + 1;
        // Luján's footer is Boca's: the same height, caption and 3x2 card block, laid out by Boca's own footer code
        // for a reference Boca simulation (six active-cook cards, no pennants, no ready selector).
        private static StreetSimulation pilgrimFooterReference;
        private static int pilgrimFooterReferenceLevel = -1;
        private static ResponsiveFooterLayout pilgrimFooterCache;
        private static float pilgrimFooterCacheHeight = -1f;
        private static int pilgrimFooterCacheLevel = -1;
        // Campaign level index whose own footer the level copies: Nueva Chicago (1) for Camioneros and La Renga, Ferro (3) for Los Redondos, Boca (8) for Luján.
        private static int PilgrimFooterReferenceLevel(int sourceLevelId) =>
            sourceLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId || sourceLevelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId ? 1
            : sourceLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId ? 3 : 8;
        private ResponsiveFooterLayout PilgrimFooter(float canvasHeight)
        {
            int level = PilgrimFooterReferenceLevel(PilgrimSourceLevelId);
            if (Mathf.Abs(canvasHeight - pilgrimFooterCacheHeight) > .001f || pilgrimFooterCache.UpgradeBounds == null ||
                pilgrimFooterCacheLevel != level)
            {
                if (pilgrimFooterReference == null || pilgrimFooterReferenceLevel != level)
                {
                    pilgrimFooterReference = new StreetSimulation(new StreetBalance(), level, 5f, 0, 1, 0);
                    pilgrimFooterReferenceLevel = level;
                }
                pilgrimFooterCache = BuildResponsiveFooterLayout(pilgrimFooterReference, canvasHeight, true, false);
                pilgrimFooterCacheHeight = canvasHeight;
                pilgrimFooterCacheLevel = level;
            }
            return pilgrimFooterCache;
        }

        // Boca's footer rects are authored in 960-high layout units: y moves with the screen height, sizes do not.
        private Rect PilgrimCardBounds(int visibleIndex)
        {
            Rect[] cards = PilgrimFooter(logicalCanvasHeight).UpgradeBounds;
            if (visibleIndex < 0 || visibleIndex >= cards.Length) return Rect.zero;
            Rect card = cards[visibleIndex];
            return new Rect(card.x, card.y * (logicalCanvasHeight / H), card.width, card.height);
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
            for (int cardIndex = 0; cardIndex < PilgrimItemCount; cardIndex++)
            {
                if (!PilgrimCardBounds(cardIndex).Contains(logical)) continue;
                if (cardIndex == 0) return CanUpgradePilgrimSpeed ? TowerDefenseSpeedAction : 0;
                return CanSelectPilgrimWorker(cardIndex) ? PilgrimSelectFirst + cardIndex : 0;
            }
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
            if (action < PilgrimSelectFirst || action >= PilgrimSelectFirst + PilgrimItemCount) return false;
            if (!TowerDefensePlacementInputAllowed) return true;
            int selectedCard = action - PilgrimSelectFirst;
            if (selectedCard <= 0 || !CanSelectPilgrimWorker(selectedCard)) return true;

            // One tap hires: the worker appears at its role's post and starts working, as in Boca.
            StreetWorkerRole role = PilgrimWorkerCardRoles[selectedCard - 1];
            if (!towerDefenseMode.CanHireAtPost(role))
            {
                towerDefenseError = "Falta una estación o el cupo de este rol está completo.";
                return true;
            }
            bool saved = game.TryHireTowerDefenseAtPost(towerDefenseMode, role);
            towerDefenseError = saved ? null : "No se pudo guardar. La partida quedó en pausa.";
            if (!saved) towerDefenseMode.Pause();
            return true;
        }

        private void DrawPilgrimHud(float height)
        {
            DrawTowerDefenseCampaignHud();
            float top = TowerDefenseResponsiveLayout.PilgrimFooterTop(height, PilgrimSourceLevelId);
            DrawFooterBluePanel(LayoutRect(new Rect(0, top, W, height - top)), layoutVerticalScale);
            // Same caption as the source level's footer: club first, neighborhood second, with the level's crest.
            ResponsiveFooterLayout pilgrimFooterLayout = PilgrimFooter(height);
            DrawPilgrimFooterPennants(pilgrimFooterLayout.Pennants);
            Rect badge = pilgrimFooterLayout.LevelBadge;
            DrawLevelBadge(PilgrimSourceLevelId,
                new Rect(badge.x, badge.y * (height / H), badge.width, badge.height));

            string hint = !string.IsNullOrEmpty(towerDefenseError) ? towerDefenseError : string.Empty;
            if (!string.IsNullOrEmpty(hint))
            {
                int hintSize = towerDefenseSmallLabel.fontSize;
                towerDefenseSmallLabel.fontSize = FitUpgradeStatusFontSize(towerDefenseSmallLabel, hint, W - 162f);
                DrawTowerDefenseLabel(new Rect(42, top - 18, W - 150, 18), hint, towerDefenseSmallLabel);
                towerDefenseSmallLabel.fontSize = hintSize;
            }

            bool active = TowerDefensePlacementInputAllowed;
            for (int cardIndex = 0; cardIndex < PilgrimItemCount; cardIndex++)
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
                    towerDefenseMode.WorkersCount(role), active && cost > 0 && cost <= towerDefenseMode.Coins);
            }

            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused)
            {
                DrawTowerDefensePanel(new Rect(75, height * .43f, W - 150, 80), new Color(.015f, .04f, .08f, .92f));
                DrawTowerDefenseLabel(new Rect(85, height * .44f, W - 170, 55), "PARTIDA EN PAUSA", towerDefenseTitle);
            }
        }

        // Nueva Chicago's footer keeps its club pennants in the band under the cards (Boca's has none).
        private void DrawPilgrimFooterPennants(Rect pennants)
        {
            if (pennants.height <= 1f) return;
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(PilgrimSourceLevelId);
            if (clubPennantTexture == null || clubPennantThemeName != theme.ClubName ||
                Mathf.Abs(clubPennantLogicalHeight - pennants.height) > .25f)
            {
                if (clubPennantTexture != null) Destroy(clubPennantTexture);
                clubPennantTexture = CreateFooterPennants(theme, pennants.height);
                clubPennantThemeName = theme.ClubName;
                clubPennantLogicalHeight = pennants.height;
            }
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, pennants.y, W, pennants.height), clubPennantTexture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private void DrawPilgrimWorkerCard(Rect bounds, int action, StreetWorkerRole role, int cost,
            int count, bool available)
        {
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
            DrawPilgrimUpgradeCard(bounds, action, false, role == StreetWorkerRole.ParrilleroPremium, RoleCaption(role),
                "EQUIPO " + count, cost, available, art, source);
        }

        private void DrawPilgrimSpeedCard(Rect bounds, int cost, int speedLevel, bool available)
        {
            string multiplier = (1f + speedLevel * .1f).ToString("0.00").Replace('.', ',');
            // Same text as Boca's Upgrade(): one line on the wide cards, two on the compact ones.
            string status = bounds.width < 200f ? "×" + multiplier + "\n+10%" : "×" + multiplier + "  ·  +10%";
            DrawPilgrimUpgradeCard(bounds, TowerDefenseSpeedAction, true, false, "VELOCIDAD", status,
                cost, available, speedArrows, SpeedArrowBounds);
        }

        // Same card as Boca's Upgrade(): the compact 156x100 card, with its own title/status/coin/price placement.
        private void DrawPilgrimUpgradeCard(Rect r, int action, bool speed, bool premium, string name, string status,
            int cost, bool available, Texture2D portrait, Rect portraitSource)
        {
            DrawPilgrimCardBackground(r, available, false);
            bool compact = r.width < 200;
            int titleSize = upgradeTitle.fontSize;
            upgradeTitle.fontSize = r.height < 120 ? (premium ? 13 : compact ? 15 : 17) : compact ? 16 : 18;
            Rect titleBounds = UpgradeTitleBounds(r);
            upgradeTitle.fontSize = FitUpgradeStatusFontSize(upgradeTitle, name, titleBounds.width);
            UpgradeOutline(titleBounds, name, upgradeTitle, new Color(.98f, .99f, 1f));
            upgradeTitle.fontSize = titleSize;
            Rect statusBounds = speed ? UpgradeSpeedStatusBounds(r) : UpgradeStatusBounds(r);
            int statusSize = upgradePercent.fontSize;
            if (compact) upgradePercent.fontSize = speed ? 12 : 13;
            upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, status, statusBounds.width);
            UpgradeOutline(statusBounds, status, upgradePercent, new Color(.55f, .90f, 1f));
            upgradePercent.fontSize = statusSize;
            if (portrait != null) DrawUpgradeOvalContent(r, portrait, portraitSource);
            string priceText = cost > 0 ? cost.ToString() : "MAX";
            if (cost > 0) Item(UpgradeCoinBounds(r), 10);
            int priceSize = upgradePrice.fontSize;
            if (compact || r.height < 120) upgradePrice.fontSize = 24;
            upgradePrice.fontSize = FitUpgradePriceFontSize(upgradePrice, priceText, UpgradePriceTextBounds(r).width);
            UpgradeOutline(UpgradePriceTextBounds(r), priceText, upgradePrice, new Color(.98f, .99f, 1f));
            upgradePrice.fontSize = priceSize;
            DrawTowerDefenseClickTarget(r, action, available);
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
