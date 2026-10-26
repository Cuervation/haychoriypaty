using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    /// <summary>Sprite-backed portrait renderer of actual worker/order state; never simulates deliveries.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StreetGame))]
    public sealed partial class StreetView : MonoBehaviour
    {
        private const float W = StreetSceneLayout.Width, H = StreetSceneLayout.Height;
        private StreetKitchenRenderer kitchenRenderer;
        private const string AllBoysCrestResource = "street-allboys-crest";
        private const string ChicagoCrestResource = "street-new-chicago-crest-v1";
        private const string ChicagoBackgroundResource = "street-background-chicago-v1";
        private const string VelezBackgroundResource = "street-background-velez-v1";
        private const string VelezCrestResource = "street-velez-crest-v1";
        private const string RacingCrestResource = "street-racing-crest-v1";
        private const string IndependienteCrestResource = "street-independiente-crest-v1";
        private const string FerroCrestResource = "street-ferro-crest-v1";
        private const string SanLorenzoCrestResource = "street-san-lorenzo-crest-v1";
        private const string RiverCrestResource = "street-river-crest-v1";
        private const string BocaCrestResource = "street-boca-crest-v1";
        private const string CamionerosCrestResource = "street-camioneros-crest-v1";
        private const string LosRedondosCrestResource = "street-los-redondos-crest-v1";
        // Ordered by StreetSimulation.LevelDisplayNames; user-supplied crests are scoped to their levels.
        private static readonly string[] LevelCrestResourceNames = {
            AllBoysCrestResource, ChicagoCrestResource, VelezCrestResource,
            FerroCrestResource, IndependienteCrestResource, RacingCrestResource,
            SanLorenzoCrestResource, RiverCrestResource, BocaCrestResource,
            CamionerosCrestResource, LosRedondosCrestResource
        };
        private const string VelezFrontFansResource = "street-velez-fans-front-v1";
        private const string VelezWalkingFansResource = "street-velez-fans-walk-v1";
        private const string VelezRiotFansResource = "street-velez-riot-fans-v1";
        private const string VelezGrillResource = "street-parrilla-velez-v1";
        private const string FerroBackgroundResource = "street-background-ferro-v2";
        private const string FerroFrontFansResource = "street-ferro-fans-front-v2";
        private const string FerroWalkingFansResource = "street-ferro-fans-walk-v2";
        private const string FerroRiotFansResource = "street-ferro-riot-fans-v2";
        private const string IndependienteBackgroundResource = "street-background-independiente-v2";
        private const string IndependienteFrontFansResource = "street-independiente-fans-front-v2";
        private const string IndependienteWalkingFansResource = "street-independiente-fans-walk-v2";
        private const string IndependienteRiotFansResource = "street-independiente-riot-fans-v3";
        private const string RacingBackgroundResource = "street-background-racing-v1";
        private const string RacingFansResource = "street-racing-fans-paired-v1";
        private const string RacingRiotFansResource = "street-racing-riot-fans-v2";
        private const string SanLorenzoBackgroundResource = "street-background-sanlorenzo-v1";
        private const string SanLorenzoFansResource = "street-sanlorenzo-fans-paired-v1";
        private const string SanLorenzoRiotFansResource = "street-sanlorenzo-riot-fans-v2";
        private const string BocaBackgroundResource = "street-background-boca-v1";
        private const int RiverVisualLevelIndex = 7;
        private const string RiverMuralResource = "street-mural-river-master-v1";
        private const string RiverHeaderExtensionResource = "street-river-header-extension-v1";
        private const string RiverFansResource = "street-river-fans-paired-v1";
        private const string RiverRiotFansResource = "street-river-riot-fans-v2";
        private const string BocaFansResource = "street-boca-fans-paired-v1";
        private const string BocaRiotFansResource = "street-boca-riot-fans-v2";
        private const string CamionerosBackgroundResource = "street-background-camioneros-v1";
        private const string LosRedondosBackgroundResource = "street-background-losredondos-v1";
        private const string LosRedondosFrontFansResource = "street-losredondos-fans-front-v1";
        private const string LosRedondosWalkingFansResource = "street-losredondos-fans-walk-v1";
        private const string FourZoneGrillResource = "street-grill-four-zones-v1";
        private const string BeerBarrelResource = "street-beer-barrel-v1";
        private const string FernetTableResource = "street-fernet-table-v1";
        private const int VelezFanVariantCount = 9;
        private const int VelezFanAtlasColumns = 3;
        private const int TeamFanVariantCount = 9;
        private const int IndependienteFanVariantCount = 12;
        private const int FerroFanVariantCount = 12;
        private const int RacingFanVariantCount = 12;
        private const int LosRedondosFanVariantCount = 12;
        private const int LosRedondosFanAtlasColumns = 3;
        private const int TeamFanAtlasColumns = 3;
        private const string CocaBottleResource = "street-coca-bottle-v1";
        private const string BeverageBarrelResource = "street-beverage-barrel-v1";
        private const string LaterCocacoleroResource = "street-cocacolero-levels3-5-v1";
        // Nine monochrome All Boys outfits stay integrated in front, walk, and timeout poses.
        // All three atlases use the same customer-ID variant so apparel persists across contexts.
        // The old four uniformed silhouettes remain a safe fallback if an atlas is missing.
        private const string AllBoysFrontFansResource = "street-allboys-fans-front-v1";
        private const string AllBoysWalkingFansResource = "street-allboys-fans-walk-v1";
        private const string AllBoysRiotFansResource = "street-allboys-riot-fans-v1";
        private const int AllBoysFanVariantCount = 9;
        private const int AllBoysFanAtlasColumns = 3;
        private const int AllBoysLegacyFrontFanFirstFrame = 8;
        private const int AllBoysLegacyWalkingFanFirstFrame = 12;
        private const int AllBoysLegacyFanVariantCount = 4;
        // Eight original Nueva Chicago outfits are integrated into complete character poses.
        private const string ChicagoFrontFansResource = "street-chicago-fans-front-v1";
        private const string ChicagoWalkingFansResource = "street-chicago-fans-walk-v1";
        private const string ChicagoRiotFansResource = "street-chicago-riot-fans-v2";
        private const int ChicagoFanVariantCount = 8;
        private const int ChicagoFanAtlasColumns = 3;
        private static readonly Rect PlayingLevelBadge = new Rect(45, 886, 450, 38);
        private static readonly Rect ReadyLevelBadge = new Rect(43, 884, 454, 26);
        private const string MuralResource = "street-mural-real-v5";
        // Draw one complete mural/wall/coping crop; cover the old v4 mural band so it cannot peek through below.
        private static readonly Rect MuralSource = new Rect(0, 0, 940, 280);
        private static readonly Rect MuralBounds = StreetSceneLayout.MuralBounds;
        private static readonly Rect AllBoysSelectorMuralSource = new Rect(45, 0, 610, 280);
        private static readonly Rect ChicagoSelectorMuralSource = new Rect(0, 0, 610, 280);
        private static readonly int[][] LaterLevelCardProducts = {
            new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { 3, 5, 6 }
        };
        // Status is screen-edge anchored; interactive controls still use the safe-area canvas.
        private const float HudBandScale = 1.2f;
        private const float HudStatusHeightScale = 1.2f;
        private const float HudTopPadding = 1f * HudBandScale;
        private const float HudStateRowHeight = 24f * HudBandScale * HudStatusHeightScale;
        private const float HudObjectiveGap = 2f * HudBandScale;
        private const float HudBottomPadding = 3f * HudBandScale;
        private const float HudQuotaBaseGridHeight = 38f * HudBandScale * HudStatusHeightScale;
        private const float HudStatusIconSize = 20f * HudBandScale * HudStatusHeightScale;
        private const float HudQuotaCardEnlargement = 1.25f;
        private const float HudQuotaCardScale = 1.2f * HudQuotaCardEnlargement;
        private const float HudStatusCardPadding = 8f;
        private const float HudStatusCardSpacing = 6f;
        private const float HudStatusTextGap = 6f * HudBandScale;
        private const float HudCutoutClearance = 2f * HudBandScale;
        private const float HudChipInset = 2f * HudBandScale;
        private const float HudQuotaCardInset = HudChipInset * .5f * HudQuotaCardEnlargement;
        private const float HudQuotaCounterRightInset = 4f * HudBandScale * HudQuotaCardEnlargement;
        private const TextAnchor HudQuotaCounterTextAlignment = TextAnchor.MiddleRight;
        private const float HudChipTextTopInset = 1f * HudBandScale;
        private const float HudChipTextHeightInset = 3f * HudBandScale;
        private const float HudQuotaBaseCardHeight = (HudQuotaBaseGridHeight - HudObjectiveGap - HudChipInset) * .5f;
        private const float HudQuotaCardHeight = HudQuotaBaseCardHeight * HudQuotaCardScale;
        private const float HudQuotaCardBaseWidth = 84f;
        private const float HudQuotaCardWidth = HudQuotaCardBaseWidth * HudQuotaCardEnlargement;
        private const int HudQuotaSolidProductCount = 4;
        private const float HudQuotaGridHeight = HudQuotaCardHeight * HudQuotaSolidProductCount
            + HudObjectiveGap * (HudQuotaSolidProductCount - 1);
        private const float HudQuotaIconMaxSize = (HudQuotaBaseCardHeight - HudChipInset * 2f) * HudQuotaCardScale;
        private static readonly Rect HudBar = new Rect(0, 0, 540,
            HudTopPadding + HudStateRowHeight + HudBottomPadding);
        private static readonly Rect HudCoins = HudStatusTextBounds(0, Rect.zero);
        private static readonly Rect HudTime = HudStatusTextBounds(1, Rect.zero);
        private static readonly Rect HudQuotaGrid = new Rect(8,
            HudBar.height + HudObjectiveGap, 524, HudQuotaGridHeight);
        // Read-only product chips are a separate overlay over the mural, not part of the HUD bar.
        private static readonly Rect HudSales = HudQuotaGrid;
        private static readonly Rect CutoutHudGap = new Rect(232, 0, 70, 34);
        private const int HudCounterFontSize = 35;
        private const int HudStatusValueFontSize = HudCounterFontSize;
        private const int HudClockFontSize = HudCounterFontSize;
        private const int HudQuotaCounterFontSize = HudCounterFontSize;
        private const float CustomerHiddenLegHeight = StreetSceneLayout.CustomerHiddenLegHeight;
        // Standard station dimensions come from live Floresta, not per-level fitting.
        private const string ChicagoGrillResource = "street-parrilla-large-v4";
        private static readonly Rect ServingTableRect = StreetWorkstationLayout.FoodTableBounds;
        private static readonly Rect ChicagoServingTableRect = StreetWorkstationLayout.FoodTableBounds;
        private static readonly Rect ChicagoBarrelRect = StreetWorkstationLayout.CocaBounds;
        private static readonly Rect VelezBarrelRect = StreetWorkstationLayout.CocaBounds;
        private static Rect ServingTableBounds(int productCount) => StreetWorkstationLayout.FoodTableBounds;
        private static Rect ServingTableBoundsForLevel(int productCount, int levelIndex) => StreetWorkstationLayout.FoodTableBounds;
        private static readonly Rect Start = new Rect(161, 555, 218, 61);
        private static readonly Rect Speed = new Rect(28, 710, 234, 120);
        private static readonly Rect HireParrillero = new Rect(278, 710, 234, 120);
        private static readonly Rect HireCocacolero = new Rect(360, 710, 156, 120);
        private const int HireCocacoleroAction = 16;
        private const int HirePremiumAction = 20, HireFerneteroAction = 21, HireMozoAction = 22;
        private static int[] CatalogUpgradeActions(StreetSimulation sim)
        {
            var actions = new System.Collections.Generic.List<int> { 3 };
            if (sim.UsesActiveCooks)
                actions.AddRange(new[] { 2, HirePremiumAction, HireCocacoleroAction, HireFerneteroAction, HireMozoAction });
            else
                foreach (var role in new[] { StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Cocacolero, StreetWorkerRole.Fernetero })
                    if (sim.HasRole(role)) actions.Add(ActionForRole(role));
            return actions.ToArray();
        }
        private static int ActionForRole(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero ? 2
            : role == StreetWorkerRole.ParrilleroPremium ? HirePremiumAction : role == StreetWorkerRole.Cocacolero ? HireCocacoleroAction : role == StreetWorkerRole.Fernetero ? HireFerneteroAction : HireMozoAction;
        private static StreetWorkerRole RoleForAction(int action) => action == 2 ? StreetWorkerRole.Parrillero
            : action == HirePremiumAction ? StreetWorkerRole.ParrilleroPremium : action == HireCocacoleroAction ? StreetWorkerRole.Cocacolero : action == HireFerneteroAction ? StreetWorkerRole.Fernetero : StreetWorkerRole.Mozo;
        private static string RoleCaption(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero ? "PARRILLERO"
            : role == StreetWorkerRole.ParrilleroPremium ? "PARRILLERO\nPREMIUM" : role == StreetWorkerRole.Cocacolero ? "COCACOLERO" : role == StreetWorkerRole.Fernetero ? "FERNETERO" : "MOZO";
        private static Rect CatalogUpgradeCardBounds(StreetSimulation sim, int action)
        {
            int[] actions = CatalogUpgradeActions(sim);
            int index = System.Array.IndexOf(actions, action), count = actions.Length;
            if (index < 0) return Rect.zero;
            if (count == 2) return index == 0 ? Speed : HireParrillero;
            if (count == 3) return new Rect(24 + index * 168, 710, 156, 120);
            if (count == 4) return new Rect(28 + (index % 2) * 250, 710 + (index / 2) * 116, 234, 104);
            if (count == 6 && sim.UsesActiveCooks)
                return new Rect(24 + (index % 3) * 168, 740 + (index / 3) * 108, 156, 100);
            if (count == 5) return index < 3
                ? new Rect(24 + index * 168, 740, 156, 100)
                : new Rect(28 + (index - 3) * 250, 848, 234, 100);
            return new Rect(24 + index * 168, 740, 156, 100);
        }

        private struct ResponsiveFooterLayout
        {
            public Rect[] UpgradeBounds;
            public Rect LevelBadge;
            public Rect ReadySelector;
            // Footer layout coordinates; decorative pennants may extend through the bottom safe inset.
            public Rect Pennants;
        }

        private static ResponsiveFooterLayout BuildResponsiveFooterLayout(
            StreetSimulation sim, float canvasHeight, bool showUpgrades, bool showReadySelector)
        {
            return BuildResponsiveFooterLayoutCore(sim, canvasHeight, showUpgrades, showReadySelector, canvasHeight);
        }

        private static ResponsiveFooterLayout BuildResponsiveFooterLayoutWithBottomBleed(
            StreetSimulation sim, float canvasHeight, bool showUpgrades, bool showReadySelector, float bottomBleed)
        {
            return BuildResponsiveFooterLayoutCore(sim, canvasHeight, showUpgrades, showReadySelector,
                canvasHeight + Mathf.Max(0f, bottomBleed));
        }

        private static float MaximumResponsiveFooterPanelHeight(float compactScale)
        {
            // Reserve against the tallest supported footer (four-action, two-row layout),
            // so decorative flags do not change size with the level's upgrade catalog.
            float fourActionPanel = 22f + 6f + 2f * 104f + 8f;
            float fiveActionPanel = 20f + 6f + 2f * 100f + 8f;
            float readyPanel = 26f + 6f + 120f + 6f + ReadyLevelSelector.height;
            return Mathf.Max(fourActionPanel, Mathf.Max(fiveActionPanel, readyPanel)) * compactScale;
        }

        private static ResponsiveFooterLayout BuildResponsiveFooterLayoutCore(
            StreetSimulation sim, float canvasHeight, bool showUpgrades, bool showReadySelector, float decorationBottom)
        {
            float verticalScale = Mathf.Max(.01f, canvasHeight / H);
            float compactScale = Mathf.Min(1f, verticalScale);
            int[] actions = showUpgrades ? CatalogUpgradeActions(sim) : new int[0];
            int count = actions.Length;
            int rows = count == 0 ? 0 : count <= 3 ? 1 : 2;
            float cardHeight = (count >= 5 ? 100f : count == 4 ? 104f : 120f) * compactScale;
            float rowGap = rows > 1 ? 8f * compactScale : 0f;
            float badgeHeight = (count >= 5 ? 20f : count == 4 ? 22f : 26f) * compactScale;
            float sectionGap = (rows > 0 || showReadySelector) ? 6f * compactScale : 0f;
            float selectorHeight = showReadySelector ? ReadyLevelSelector.height * compactScale : 0f;
            float selectorGap = showReadySelector && rows > 0 ? 6f * compactScale : 0f;
            float topPadding = 4f * compactScale;
            float cardBlockHeight = rows * cardHeight + Mathf.Max(0, rows - 1) * rowGap;
            float panelHeight = badgeHeight + sectionGap + cardBlockHeight + selectorGap + selectorHeight;
            float stageBottom = sim.KitchenLayout.PlayfieldBottom * verticalScale;
            float pennantClearance = 2f * compactScale;
            float desiredPennantHeight = 80f * compactScale;
            float availablePennantHeight = Mathf.Max(0f, decorationBottom - stageBottom - topPadding
                - panelHeight - pennantClearance);
            // The Racing campaign begins at zero-based index 5; omit decorative pennants from there onward.
            float pennantHeight = sim.LevelIndex < 5
                ? Mathf.Min(desiredPennantHeight, availablePennantHeight)
                : 0f;
            float pennantTop = decorationBottom - pennantHeight;
            // Keep every authored button/title bound intact. Reserve the visible pennant band
            // only from space left after the actual footer and playfield; never paint it behind cards.
            float panelBottom = pennantHeight > 1f
                ? pennantTop - pennantClearance
                : decorationBottom - 4f * compactScale;
            // Keep the club/neighborhood caption at the top of the footer, just below the
            // kitchen field. Center the controls in the remaining safe space before pennants.
            // If the footer is tight, sacrifice the spare gap before allowing the block to clip.
            float panelTop = Mathf.Min(stageBottom + topPadding, panelBottom - panelHeight);
            float cardTop = panelTop + badgeHeight + sectionGap;
            float buttonBlockHeight = cardBlockHeight + selectorGap + selectorHeight;
            float spareButtonSpace = Mathf.Max(0f, panelBottom - cardTop - buttonBlockHeight);
            cardTop += spareButtonSpace * .5f;

            var cards = new Rect[count];
            for (int i = 0; i < count; i++)
            {
                Rect authored = CatalogUpgradeCardBounds(sim, actions[i]);
                int row = count == 4 ? i / 2 : count == 6 && sim.UsesActiveCooks ? i / 3 : count == 5 && i >= 3 ? 1 : 0;
                cards[i] = new Rect(authored.x, (cardTop + row * (cardHeight + rowGap)) / verticalScale,
                    authored.width, authored.height);
            }

            Rect selector = Rect.zero;
            if (showReadySelector)
            {
                float selectorY = cardTop + cardBlockHeight + selectorGap;
                float width = ReadyLevelSelector.width * compactScale;
                selector = new Rect(ReadyLevelSelector.center.x - width * .5f, selectorY / verticalScale,
                    width, selectorHeight);
            }

            Rect badge = new Rect(45f, panelTop / verticalScale, 450f, badgeHeight);
            Rect pennants = pennantHeight > 1f
                ? new Rect(0f, pennantTop, W, pennantHeight)
                : new Rect(0f, pennantTop, W, 0f);

            return new ResponsiveFooterLayout {
                UpgradeBounds = cards, LevelBadge = badge, ReadySelector = selector, Pennants = pennants
            };
        }

        private ResponsiveFooterLayout CurrentResponsiveFooter(StreetSimulation sim, float canvasHeight, float bottomBleed = 0f)
        {
            bool ready = !levelSelectActive && sim.Phase == RoundPhase.Ready;
            bool play = VictoryTransitionActive || (!levelSelectActive && sim.Phase == RoundPhase.Playing);
            bool showCards = play || (ready && CatalogUpgradeActions(sim).Length <= 3);
            return bottomBleed > 0f
                ? BuildResponsiveFooterLayoutWithBottomBleed(sim, canvasHeight, showCards, ready, bottomBleed)
                : BuildResponsiveFooterLayout(sim, canvasHeight, showCards, ready);
        }

        private Rect CatalogUpgradeTouchBounds(int action)
        {
            Rect rect = ResponsiveUpgradeCardBounds(game.Sim, action);
            rect.height /= Mathf.Max(.01f, layoutVerticalScale);
            return rect;
        }
        private Rect CatalogLevelBadge() => CatalogUpgradeActions(game.Sim).Length == 5
            ? new Rect(45, 716, 450, 20) : CatalogUpgradeActions(game.Sim).Length == 4 ? new Rect(45, 934, 450, 22) : PlayingLevelBadge;
        private static Rect UpgradeTouchBounds(int action, bool specialists, float verticalScale)
        {
            Rect rect = UpgradeCardBounds(action, specialists);
            rect.height /= Mathf.Max(.01f, verticalScale);
            return rect;
        }
        private static Rect UpgradeCardBounds(int action, bool specialists)
        {
            if (!specialists) return action == 3 ? Speed : HireParrillero;
            return action == 3 ? new Rect(24,710,156,120) : action == 2 ? new Rect(192,710,156,120) : HireCocacolero;
        }
        private static readonly Rect MenuPlay = new Rect(126, 704, 288, 92);
        private static readonly Rect MenuQuit = new Rect(126, 816, 288, 92);
        private const int MenuPlayAction = 6, MenuQuitAction = 7;
        private const int LevelSelectorBackAction = 8, VictoryExitAction = 9, RiotReturnAction = 15, LevelSelectFirstAction = 30;
        private const int HubPlayAction = 200, HubMarketAction = 201, HubBadgesAction = 202, HubRankingAction = 203, HubAccountAction = 204;
        private const int BadgeBackAction = 205, RankingBackAction = 206;
        private const string LevelOneTutorialCompletedKey = "HayChoriYPaty.LevelOneTutorial.Completed.v2";
        private const int LevelOneTutorialPrimaryAction = 300, LevelOneTutorialSkipAction = 301;
        private const float LevelOneTutorialBubbleWidth = 320f;
        private const float LevelOneTutorialBubbleHeight = 140f;
        private const float LevelOneTutorialBubbleTailWidth = 52f;
        private const float LevelOneTutorialBackdropDimAlpha = .50f;
        private static Color LevelOneTutorialBackdropDimColor() =>
            new Color(0f, 0f, 0f, LevelOneTutorialBackdropDimAlpha);
        private static readonly string[] LevelOneTutorialHeadlines = {
            "Tu objetivo: entregar\n200 choris.",
            "Cada chori entregado\nsuma monedas.",
            "Contratá más Parrilleros\npara cocinar más choris.",
            "Mejorá la velocidad\nde trabajo."
        };
        private static readonly string[] LevelOneTutorialExplanations = {
            "Acá ves cuánto llevás.",
            "",
            "El costo aparece en esta tarjeta.",
            "El próximo costo aparece\nen esta tarjeta."
        };
        private const float LevelSelectorCardHeight = 184f, LevelSelectorCardGap = 12f;
        private static readonly Rect LevelSelectorViewport = new Rect(14f, 154f, W - 28f, 706f);
        private float levelSelectScroll, levelSelectPointerDownY, levelSelectPointerDownScroll;
        private bool levelSelectPointerHeld, levelSelectDragging;
        private const int ReadyLevelSelectorAction = 19;
        private const int MarketBackAction = 70, MarketOtherAction = 190;
        private const int MarketMinusAction = 80, MarketPlusAction = 100, MarketPlusTenAction = 120;
        private const int MarketBuyAction = 140, MarketMissingAction = 150, MarketPlayAction = 160;
        private const int MarketRowMissingAction = 170, MarketExpandAction = 180;
        private const int MarketCloseAction = 161, ResultLevelsAction = 163;
        private const int ChulengoMultiplyAction = 164, ChulengoCollectAction = 165;
        private static readonly Rect ReadyLevelSelector = new Rect(161, 905, 218, 44);
        private bool marketActive, marketFromSelector;
        private bool marketOtherExpanded;
        private int marketExpandedIngredient = -1;
        private GUIStyle marketLabelStyle;
        private readonly Sprite[] marketIngredientSprites = new Sprite[10];
        private Texture2D marketFernetBottle, marketCocaLarge, marketBreadCommon, marketBreadBurger;
        private readonly int[] marketPendingPacks = new int[10];
        private float marketScroll, marketPointerDownY, marketPointerDownScroll, marketLastPurchaseTime = -10f;
        private bool marketDragging, marketPointerHeld;
        private string marketMessage = "";
        private static readonly Rect LevelSelectTitle = new Rect(54, 88, 432, 58);
        private static readonly Rect LevelSelectBack = new Rect(139, 870, 262, 66);
        private const string VictoryPopupResource = "street-victory-popup-wood-v1";
        private const string ChulengoBowlResource = "street-chulengo-bowl-v1";
        private static readonly Vector2 VictoryPopupArtSize = new Vector2(1122, 1402);
        // Normalized slots in the transparent artwork; text and pointer bounds use the same frame.
        private static readonly Rect VictoryExit = new Rect(.195f, .839f, .62f, .151f);
        private const string IntroBackdropResource = "street-cover-user-v5";
        private const string HubBackdropResource = "street-hub-stadium-v1";
        private static readonly Vector2 HubArtworkSize = new Vector2(941, 1672);
        private const string RiotBackdropResource = "street-riot-environment-v1";
        private const string RiotFanAtlasResource = "street-riot-fans-v1";
        private const string RiotImpactCloudResource = "street-riot-fight-cloud-v1";
        private const float RiotImpactCloudDuration = 1.55f;
        private const float RiotAngerRampSeconds = .55f, RiotBreakStartSeconds = .55f, RiotBreakTransitionSeconds = .9f, RiotFanFramesPerSecond = 6f;
        private const float RiotFanAtlasRowSplitY = 493f;
        private static readonly Rect RiotReturnButton = new Rect(88, 858, 364, 66);
        private StreetGame game;
        private int VisualLevelIndex => game != null && game.Sim != null ? game.Sim.LevelIndex : 0;
        private Texture2D backdrop, chicagoBackground, velezBackground, velezGrill, people, items, parrillero, parrilleroDiagonal, cocacolero, laterCocacolero, parrilleroIcon, upgradeWood, speedArrows, largeGrill, gameCoin, muralArt, allBoysCrest, chicagoCrest, cocaBottle, beverageBarrel, chicagoGrill;
        private Texture2D[] levelCrests;
        private readonly System.Collections.Generic.Dictionary<Texture2D, Rect> crestVisibleUvs =
            new System.Collections.Generic.Dictionary<Texture2D, Rect>();
        private readonly System.Collections.Generic.Dictionary<Texture2D, Rect> crestBodyUvs =
            new System.Collections.Generic.Dictionary<Texture2D, Rect>();
        private static Texture2D racingCrestCutout, camionerosCrestCutout, losRedondosCrestCutout;
        private Texture2D clubPennantTexture;
        private string clubPennantThemeName;
        private float clubPennantLogicalHeight = -1f;
        private Texture2D clubMuralTexture;
        private string clubMuralResource;
        private Texture2D allBoysFrontFans, allBoysWalkingFans, allBoysRiotFans;
        private Texture2D chicagoFrontFans, chicagoWalkingFans, chicagoRiotFans;
        private Texture2D velezFrontFans, velezWalkingFans, velezRiotFans;
        private Texture2D ferroBackground, ferroFrontFans, ferroWalkingFans, ferroRiotFans;
        private Texture2D independienteBackground, independienteFrontFans, independienteWalkingFans, independienteRiotFans;
        private Texture2D racingBackground, racingFans, racingRiotFans;
        private Texture2D sanLorenzoBackground, sanLorenzoFans, sanLorenzoRiotFans;
        private Texture2D bocaBackground, bocaFans, bocaRiotFans;
        private Texture2D riverMural, riverHeaderExtension, riverFans, riverRiotFans;
        private Texture2D camionerosBackground;
        private Texture2D losRedondosBackground, losRedondosFrontFans, losRedondosWalkingFans;
        private Texture2D fourZoneGrill, premiumGrill, beerBarrel, fernetTable;
        private Texture2D premiumWorker, premiumDiagonal, fernetWorker, fernetWorkerLater;
        private Texture2D mozoWorker;
        private Texture2D victoryPopup, clubColoredVictoryPopup;
        private int clubColoredVictoryPopupLevel = int.MinValue;
        private Texture2D chulengoMeter, chulengoNeedle, chulengoHub, chulengoParchment, chulengoBowl;
        private Texture2D coverArt, hubBackdrop, titleLogo, menuButton, googleButtonSurface, hudBarTexture, cutoutHudBarTexture, hudQuotaCardTexture, hudQuotaCardOutlineTexture, gameplayFooterTexture, riotBackdrop, riotFanAtlas, riotImpactCloud;
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> glossyUpgradeCardTextures =
            new System.Collections.Generic.Dictionary<string, Texture2D>();
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> affordableUpgradeBorderTextures =
            new System.Collections.Generic.Dictionary<string, Texture2D>();
        private Font menuFont;
        private GUIStyle menuTitle, levelTitle;
        private GUIStyle introStatusStyle;
        private Texture2D introPreviewBar;
        private Texture2D[] introLogoShines;
        private int introPreviewBarKey = -1;
        private bool introActive;
        private bool levelSelectActive;
        private bool hubActive, badgesActive, rankingActive;
        private bool levelOneTutorialActive;
        private int levelOneTutorialStep;
        private float introStarted;
        private bool splashClockStarted;
        private bool riotScreenActive;
        private float riotStartedAt;
        private const float IntroDuration = 4f;
        private bool SplashComplete { get { return splashClockStarted && Time.unscaledTime - introStarted >= IntroDuration; } }
        private const string ParrilleroLabel = "Parrillero";
        private GUIStyle tiny, small, text, title, header, amount, invisible, hudNumber, upgradeTitle, upgradePercent, upgradePrice, riotCue;
        private GUIStyle levelOneTutorialTitleStyle, levelOneTutorialBodyStyle, levelOneTutorialSkipStyle;
        private Texture2D levelOneTutorialBubbleTexture;
        private int levelOneTutorialBubbleTextureStep = -1;
        private Rect levelOneTutorialBubbleTexturePanel;
        private Vector2 levelOneTutorialBubbleTextureTip;
        private float levelOneTutorialBubbleTextureTailWidth;
        private bool levelOneTutorialBubbleTextureTailAbove;
        private int pressedAction, lastAction;
        private float feedbackUntil, feedbackScale;
        private float layoutVerticalScale = 1f;
        private float logicalCanvasHeight = H;
        // Customer occlusion follows the baked full-screen plate after projection into the safe canvas.
        private float projectedCounterTopY = StreetSceneLayout.CounterTop(H);
        private Rect cachedHudCutoutGap = CutoutHudGap;
        private readonly int[] hudQuotaProductIds = new int[7];
        private int hudQuotaProductCount;
        // Individually reviewed authored pixel bounds, not a geometric-grid assumption.
        private static readonly Rect[] Items = {
            new Rect(13,58,298,240), new Rect(310,48,253,250), new Rect(566,66,303,232), new Rect(856,63,334,232), new Rect(1190,63,195,236),
            new Rect(8,298,244,309), new Rect(328,321,179,286), new Rect(515,309,326,305), new Rect(842,350,278,265), new Rect(1125,360,275,254),
            new Rect(47,640,216,216), new Rect(280,621,296,225), new Rect(586,625,237,226), new Rect(829,670,338,172), new Rect(1168,621,210,239),
            new Rect(5,870,315,231), new Rect(320,846,265,276), new Rect(640,854,175,246), new Rect(875,855,249,245), new Rect(1155,857,233,261)
        };
        private static readonly Rect[] People = {
            new Rect(82,9,186,298),new Rect(378,7,194,303),new Rect(679,8,185,306),new Rect(988,7,194,307),
            new Rect(96,317,168,302),new Rect(395,315,176,305),new Rect(690,314,173,305),new Rect(958,314,232,308),
            new Rect(49,631,236,306),new Rect(350,627,256,313),new Rect(658,629,252,311),new Rect(955,628,261,308),
            new Rect(79,943,196,304),new Rect(383,942,208,306),new Rect(690,940,200,308),new Rect(982,940,242,308)
        };
        // Reviewed alpha bounds from the original 1315x1197 atlas; existing fans unchanged.
        private static readonly Rect[] ParrilleroPoses = {
            new Rect(30,10,266,303),
            new Rect(357,16,263,309),
            new Rect(690,10,246,316),
            new Rect(1020,10,255,303),
            new Rect(31,331,264,288),
            new Rect(367,330,229,291),
            new Rect(708,330,228,290),
            new Rect(1018,335,265,285),
            new Rect(77,622,185,288),
            new Rect(382,623,233,287),
            new Rect(702,622,221,287),
            new Rect(1045,640,242,268),
            new Rect(45,908,234,278),
            new Rect(374,908,227,276),
            new Rect(687,908,249,276),
            new Rect(1044,909,235,277)
        };
        // Alpha-trimmed bounds from the original transparent 1536x1024 4x2 diagonal-walk sheet.
        private static readonly Rect[] ParrilleroDiagonalPoses = {
            new Rect(8,20,363,485), new Rect(436,21,325,484),
            new Rect(835,25,285,479), new Rect(1221,25,300,475),
            new Rect(16,523,356,475), new Rect(424,523,336,476),
            new Rect(820,523,322,477), new Rect(1181,519,342,481)
        };
        // Alpha-trimmed cells in the original transparent 1247x1261 Cocacolero 4x4 pose atlas.
        private static readonly Rect[] CocacoleroPoses = {
            new Rect(103,946,166,309), new Rect(401,946,150,308), new Rect(703,946,147,308), new Rect(1004,946,155,307),
            new Rect(99,631,170,315), new Rect(397,631,155,315), new Rect(702,631,146,315), new Rect(983,643,217,303),
            new Rect(113,315,121,316), new Rect(385,315,192,316), new Rect(690,315,185,316), new Rect(950,315,248,270),
            new Rect(103,17,156,298), new Rect(395,24,155,291), new Rect(692,21,166,294), new Rect(1012,22,139,293)
        };
        // Chicago-only corrected atlas rows, verified against the authored PNG alpha components.
        // The legacy mapping stays unchanged in other levels; all poses are existing artwork.
        private static readonly Rect[] ChicagoCocacoleroPoses = {
            new Rect(103,6,165,291), new Rect(402,7,149,289), new Rect(704,8,146,291), new Rect(1004,8,155,296),
            new Rect(99,315,170,294), new Rect(397,315,154,292), new Rect(702,315,146,294), new Rect(984,315,215,302),
            new Rect(114,630,120,294), new Rect(385,630,192,291), new Rect(690,630,185,292), new Rect(950,677,248,213),
            new Rect(103,946,155,298), new Rect(396,946,154,290), new Rect(692,946,165,293), new Rect(1012,946,139,293)
        };
        // The matched sheet has uneven row heights, not a uniform 4x4 grid.
        private static readonly Rect[] MatchedCocacoleroPoses = {
            new Rect(45,15,235,328), new Rect(356,19,234,327), new Rect(672,18,229,329), new Rect(994,13,229,334),
            new Rect(52,354,224,308), new Rect(357,360,217,305), new Rect(668,361,212,304), new Rect(964,364,253,299),
            new Rect(77,671,163,288), new Rect(371,674,208,284), new Rect(679,675,201,284), new Rect(943,720,276,243),
            new Rect(60,964,208,281), new Rect(370,965,211,280), new Rect(673,968,220,282), new Rect(1004,963,188,282)
        };
        // Threshold-alpha bounds from the transparent upgrade board and three-arrow icon.
        private static readonly Rect UpgradeWoodBounds = new Rect(13,148,1484,715);
        // Clean board face from the existing upgrade-wood art; avoids its oval, plaque and metal frame.
        private static readonly Rect SpeedArrowBounds = new Rect(114,224,1174,662);
        private void LoadLevelCrests()
        {
            levelCrests = new Texture2D[LevelCrestResourceNames.Length];
            for (int i = 0; i < LevelCrestResourceNames.Length; i++)
            {
                string resource = LevelCrestResourceNames[i];
                if (!string.IsNullOrEmpty(resource)) levelCrests[i] = Resources.Load<Texture2D>(resource);
            }
            allBoysCrest = levelCrests[0];
            chicagoCrest = levelCrests[1];
            if (levelCrests[5] != null)
            {
                if (racingCrestCutout == null) racingCrestCutout = CreateLevelCrestCutout(levelCrests[5]);
                levelCrests[5] = racingCrestCutout;
            }
            if (levelCrests[9] != null)
            {
                if (camionerosCrestCutout == null) camionerosCrestCutout = CreateLevelCrestCutout(levelCrests[9]);
                if (camionerosCrestCutout != null) levelCrests[9] = camionerosCrestCutout;
            }
            if (levelCrests[10] != null)
            {
                if (losRedondosCrestCutout == null) losRedondosCrestCutout = CreateLevelCrestCutout(levelCrests[10]);
                if (losRedondosCrestCutout != null) levelCrests[10] = losRedondosCrestCutout;
            }
            crestVisibleUvs.Clear(); crestBodyUvs.Clear();
            for (int i = 0; i < levelCrests.Length; i++)
            {
                Texture2D crest = levelCrests[i];
                if (crest == null) continue;
                Rect visible = LevelCrestVisibleUv(crest);
                crestVisibleUvs[crest] = visible;
                crestBodyUvs[crest] = LevelCrestBodyUv(i, visible);
            }
        }

        private static Rect LevelCrestVisibleUv(Texture2D source)
        {
            // Measure once on load, including non-readable imported PNGs; never mutate assets.
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D readable = null;
            try
            {
                Texture2D texture = source;
                if (!source.isReadable)
                {
                    temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(source, temporary);
                    RenderTexture.active = temporary;
                    readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                    readable.Apply();
                    texture = readable;
                }
                Color32[] pixels = texture.GetPixels32();
                int left = source.width, right = -1, bottom = source.height, top = -1;
                for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                {
                    if (pixels[y * source.width + x].a < 8) continue;
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
                return right < left ? new Rect(0, 0, 1, 1) :
                    new Rect((float)left / source.width, (float)bottom / source.height,
                        (float)(right - left + 1) / source.width, (float)(top - bottom + 1) / source.height);
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
                if (readable != null)
                {
                    if (Application.isPlaying) Destroy(readable);
                    else DestroyImmediate(readable);
                }
            }
        }

        private static Rect LevelCrestBodyUv(int levelIndex, Rect visibleUv)
        {
            // Authored alpha bounds of the supplied Vélez art: main shield 1076 px high,
            // versus 1208 px including its detached star. Keep the star; only sizing excludes it.
            if (levelIndex == 2) visibleUv.height *= 1076f / 1208f;
            return visibleUv;
        }

        private static Rect FitLevelCrest(Rect slot, Vector2 textureSize, Rect visibleUv, Rect bodyUv)
        {
            float scale = slot.height / (textureSize.y * bodyUv.height);
            float bodyWidth = textureSize.x * bodyUv.width * scale;
            return new Rect(slot.center.x - bodyWidth * .5f + (visibleUv.xMin - bodyUv.xMin) * textureSize.x * scale,
                slot.yMin - (visibleUv.yMax - bodyUv.yMax) * textureSize.y * scale,
                textureSize.x * visibleUv.width * scale, textureSize.y * visibleUv.height * scale);
        }

        private Rect LevelCrestSizedSlot(Rect slot, Texture2D crest)
        {
            if (crest == null) return slot;
            Rect body;
            if (!crestBodyUvs.TryGetValue(crest, out body))
                if (!crestVisibleUvs.TryGetValue(crest, out body)) body = new Rect(0, 0, 1, 1);
            slot.width = slot.height * crest.width * body.width / (crest.height * body.height);
            return slot;
        }

        private void DrawLevelCrest(Rect slot, Texture2D crest)
        {
            if (crest == null) return;
            Rect uv, body;
            if (!crestVisibleUvs.TryGetValue(crest, out uv)) uv = new Rect(0, 0, 1, 1);
            if (!crestBodyUvs.TryGetValue(crest, out body)) body = uv;
            slot = LevelCrestSizedSlot(slot, crest);
            GUI.DrawTextureWithTexCoords(FitLevelCrest(slot, new Vector2(crest.width, crest.height), uv, body), crest, uv, true);
        }

        private static Texture2D CreateLevelCrestCutout(Texture2D source)
        {
            if (source == null || !source.isReadable) return null;
            int width = source.width, height = source.height;
            Color32[] pixels = source.GetPixels32();
            bool[] background = new bool[pixels.Length];
            int[] queue = new int[pixels.Length];
            int tail = 0;
            for (int x = 0; x < width; x++)
            {
                QueueLightCrestBackgroundPixel(x, 0, width, height, pixels, background, queue, ref tail);
                QueueLightCrestBackgroundPixel(x, height - 1, width, height, pixels, background, queue, ref tail);
            }
            for (int y = 1; y < height - 1; y++)
            {
                QueueLightCrestBackgroundPixel(0, y, width, height, pixels, background, queue, ref tail);
                QueueLightCrestBackgroundPixel(width - 1, y, width, height, pixels, background, queue, ref tail);
            }
            int head = 0;
            while (head < tail)
            {
                int index = queue[head++], x = index % width, y = index / width;
                if (x > 0) QueueLightCrestBackgroundPixel(x - 1, y, width, height, pixels, background, queue, ref tail);
                if (x + 1 < width) QueueLightCrestBackgroundPixel(x + 1, y, width, height, pixels, background, queue, ref tail);
                if (y > 0) QueueLightCrestBackgroundPixel(x, y - 1, width, height, pixels, background, queue, ref tail);
                if (y + 1 < height) QueueLightCrestBackgroundPixel(x, y + 1, width, height, pixels, background, queue, ref tail);
            }
            for (int i = 0; i < pixels.Length; i++)
                if (background[i]) pixels[i] = new Color32(0, 0, 0, 0);

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.name = "Level crest (light background cutout)";
            result.filterMode = source.filterMode;
            result.wrapMode = TextureWrapMode.Clamp;
            result.hideFlags = HideFlags.HideAndDontSave;
            result.SetPixels32(pixels);
            result.Apply(false, false);
            return result;
        }

        private static void QueueLightCrestBackgroundPixel(int x, int y, int width, int height, Color32[] pixels,
            bool[] background, int[] queue, ref int tail)
        {
            int index = y * width + x;
            if (background[index]) return;
            Color32 color = pixels[index];
            int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            // User-provided RGB crests have a light solid background. Flood fill only from the
            // texture edge, so enclosed white lettering and shapes remain part of the crest.
            if (min < 165 || max - min > 48) return;
            background[index] = true;
            queue[tail++] = index;
        }

        private Texture2D LevelCrestForLevel(int levelIndex)
        {
            return levelCrests != null && levelIndex >= 0 && levelIndex < levelCrests.Length
                ? levelCrests[levelIndex] : null;
        }

        private void OnEnable()
        {
            game = GetComponent<StreetGame>();
            backdrop = Resources.Load<Texture2D>("street-background-open-street-v4");
            muralArt = Resources.Load<Texture2D>(MuralResource);
            LoadLevelCrests();
            chicagoBackground = Resources.Load<Texture2D>(ChicagoBackgroundResource);
            velezBackground = Resources.Load<Texture2D>(VelezBackgroundResource);
            velezGrill = Resources.Load<Texture2D>(VelezGrillResource);
            ferroBackground = Resources.Load<Texture2D>(FerroBackgroundResource);
            independienteBackground = Resources.Load<Texture2D>(IndependienteBackgroundResource);
            racingBackground = Resources.Load<Texture2D>(RacingBackgroundResource);
            sanLorenzoBackground = Resources.Load<Texture2D>(SanLorenzoBackgroundResource);
            bocaBackground = Resources.Load<Texture2D>(BocaBackgroundResource);
            riverMural = Resources.Load<Texture2D>(RiverMuralResource);
            riverHeaderExtension = Resources.Load<Texture2D>(RiverHeaderExtensionResource);
            camionerosBackground = Resources.Load<Texture2D>(CamionerosBackgroundResource);
            losRedondosBackground = Resources.Load<Texture2D>(LosRedondosBackgroundResource);
            fourZoneGrill = Resources.Load<Texture2D>(FourZoneGrillResource);
            beerBarrel = Resources.Load<Texture2D>(BeerBarrelResource);
            fernetTable = Resources.Load<Texture2D>(FernetTableResource);
            cocaBottle = Resources.Load<Texture2D>(CocaBottleResource);
            beverageBarrel = Resources.Load<Texture2D>(BeverageBarrelResource);
            people = Resources.Load<Texture2D>("street-characters");
            items = Resources.Load<Texture2D>("street-items");
            parrillero = Resources.Load<Texture2D>("street-parrillero");
            parrilleroDiagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
            cocacolero = Resources.Load<Texture2D>("street-cocacolero-v1");
            laterCocacolero = Resources.Load<Texture2D>(LaterCocacoleroResource);
            parrilleroIcon = Resources.Load<Texture2D>("street-parrillero-icon");
            upgradeWood = Resources.Load<Texture2D>("street-upgrade-wood-v1");
            speedArrows = Resources.Load<Texture2D>("street-speed-arrows-v1");
            largeGrill = Resources.Load<Texture2D>("street-parrilla-large-v2");
            chicagoGrill = Resources.Load<Texture2D>(ChicagoGrillResource);
            gameCoin = Resources.Load<Texture2D>("street-coin-gold-v2");
            string[] marketArt = { "meat-chori/meat-chori_Raw", "sandwich-chori/sandwich-chori",
                "meat-paty/meat-paty_Raw", "sandwich-paty/sandwich-paty", "meat-bondiola/meat-bondiola_Raw",
                "meat-vacio/meat-vacio_Raw", "coca/coca", "beer/beer" };
            for (int i = 0; i < marketArt.Length; i++) marketIngredientSprites[i] = Resources.Load<Sprite>("ModularKitchen/Sprites/" + marketArt[i]);
            marketFernetBottle = Resources.Load<Texture2D>("ModularKitchen/Art/fernet-bottle-1l-v1");
            marketCocaLarge = Resources.Load<Texture2D>("ModularKitchen/Art/coca-2500ml-display-v1");
            marketBreadCommon = Resources.Load<Texture2D>("ModularKitchen/Art/market-bread-common-v1");
            marketBreadBurger = Resources.Load<Texture2D>("ModularKitchen/Art/market-bread-burger-v1");
            coverArt = Resources.Load<Texture2D>(IntroBackdropResource);
            hubBackdrop = Resources.Load<Texture2D>(HubBackdropResource);
            victoryPopup = Resources.Load<Texture2D>(VictoryPopupResource);
            riotBackdrop = Resources.Load<Texture2D>(RiotBackdropResource);
            riotFanAtlas = Resources.Load<Texture2D>(RiotFanAtlasResource);
            riotImpactCloud = Resources.Load<Texture2D>(RiotImpactCloudResource);
            titleLogo = Resources.Load<Texture2D>("street-logo");
            chulengoMeter = CreateChulengoMeterTexture();
            chulengoNeedle = CreateChulengoNeedleTexture();
            chulengoHub = CreateChulengoHubTexture();
            chulengoParchment = CreateChulengoParchmentTexture();
            chulengoBowl = Resources.Load<Texture2D>(ChulengoBowlResource);
            menuButton = CreateMenuButton();
            googleButtonSurface = CreateGoogleButtonSurface();
            hudBarTexture = CreateHudBar();
            cutoutHudBarTexture = CreateHudBarTexture(true);
            cachedHudCutoutGap = CutoutHudGap;
            hudQuotaCardTexture = CreateHudQuotaCardTexture(false);
            hudQuotaCardOutlineTexture = CreateHudQuotaCardTexture(true);
            gameplayFooterTexture = CreateGameplayFooterTexture();
            menuFont = Resources.Load<Font>("Menu/LuckiestGuy-Regular");
            premiumGrill = Resources.Load<Texture2D>("street-grill-premium-v1");
            premiumWorker = StreetWorkerAppearance.CreateVariant(parrillero, StreetWorkerRole.ParrilleroPremium, ParrilleroPoses, new Vector2(1315,1197));
            premiumDiagonal = StreetWorkerAppearance.CreateVariant(parrilleroDiagonal, StreetWorkerRole.ParrilleroPremium, ParrilleroDiagonalPoses, new Vector2(1536,1024));
            mozoWorker = StreetWorkerAppearance.CreateVariant(parrillero, StreetWorkerRole.Mozo, ParrilleroPoses, new Vector2(1315,1197));
            fernetWorker = StreetWorkerAppearance.CreateVariant(cocacolero, StreetWorkerRole.Fernetero, ChicagoCocacoleroPoses, new Vector2(1247,1261));
            fernetWorkerLater = StreetWorkerAppearance.CreateVariant(laterCocacolero, StreetWorkerRole.Fernetero, MatchedCocacoleroPoses, new Vector2(1247,1261));
            introActive = true;
            levelSelectActive = false;
            levelOneTutorialActive = false;
            levelOneTutorialStep = 0;
            introStarted = Time.unscaledTime;
            splashClockStarted = false;
            tiny = small = text = title = header = amount = invisible = menuTitle = levelTitle = marketLabelStyle = null;
            upgradeTitle = upgradePercent = upgradePrice = riotCue = null;
            levelOneTutorialTitleStyle = levelOneTutorialBodyStyle = levelOneTutorialSkipStyle = null;
            levelOneTutorialBubbleTexture = null;
            levelOneTutorialBubbleTextureStep = -1;
            pressedAction = lastAction = 0;
        }
        private void OnDisable()
        {
            DisposeLevelSelectorPresentation();
            // Release cached sprites before destroying their palette textures.
            if (kitchenRenderer != null) kitchenRenderer.ReleaseWorkers();
            if (clubColoredVictoryPopup != null) Destroy(clubColoredVictoryPopup);
            clubColoredVictoryPopup = null;
            clubColoredVictoryPopupLevel = int.MinValue;
            if (introPreviewBar != null) Destroy(introPreviewBar);
            introPreviewBar = null;
            if (introLogoShines != null)
            {
                for (int i = 0; i < introLogoShines.Length; i++)
                    if (introLogoShines[i] != null) Destroy(introLogoShines[i]);
            }
            introLogoShines = null;
            introStatusStyle = null;
            introPreviewBarKey = -1;
            if (chulengoMeter != null) Destroy(chulengoMeter);
            chulengoMeter = null;
            if (chulengoNeedle != null) Destroy(chulengoNeedle);
            chulengoNeedle = null;
            if (chulengoHub != null) Destroy(chulengoHub);
            chulengoHub = null;
            if (chulengoParchment != null) Destroy(chulengoParchment);
            chulengoParchment = null;
            if (menuButton != null) Destroy(menuButton);
            menuButton = null;
            if (googleButtonSurface != null) Destroy(googleButtonSurface);
            googleButtonSurface = null;
            if (hudBarTexture != null) Destroy(hudBarTexture);
            hudBarTexture = null;
            if (cutoutHudBarTexture != null) Destroy(cutoutHudBarTexture);
            cutoutHudBarTexture = null;
            if (hudQuotaCardTexture != null) Destroy(hudQuotaCardTexture);
            hudQuotaCardTexture = null;
            if (hudQuotaCardOutlineTexture != null) Destroy(hudQuotaCardOutlineTexture);
            hudQuotaCardOutlineTexture = null;
            if (gameplayFooterTexture != null) Destroy(gameplayFooterTexture);
            gameplayFooterTexture = null;
            if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
            levelOneTutorialBubbleTexture = null;
            levelOneTutorialBubbleTextureStep = -1;
            foreach (Texture2D texture in glossyUpgradeCardTextures.Values)
                if (texture != null) Destroy(texture);
            glossyUpgradeCardTextures.Clear();
            foreach (Texture2D texture in affordableUpgradeBorderTextures.Values)
                if (texture != null) Destroy(texture);
            affordableUpgradeBorderTextures.Clear();
            DisposeVictoryTransitionTextures();
            foreach (var sizes in marketPanelTextures.Values)
                foreach (Texture2D texture in sizes.Values)
                    if (texture != null) Destroy(texture);
            marketPanelTextures.Clear();
            if (clubPennantTexture != null) Destroy(clubPennantTexture);
            clubPennantTexture = null;
            clubPennantThemeName = null;
            foreach (Texture2D variant in new[] { premiumWorker, premiumDiagonal, fernetWorker, fernetWorkerLater, mozoWorker })
                if (variant != null) Destroy(variant);
            premiumWorker = premiumDiagonal = fernetWorker = fernetWorkerLater = mozoWorker = null;
        }
        private void Update()
        {
            ObserveVictoryTransition();
            if (introActive && game != null && game.IsPlayerActive)
            {
                if (!splashClockStarted) { introStarted = Time.unscaledTime; splashClockStarted = true; }
                if (SplashComplete) OpenHubFromIntro();
            }
            if (introActive && (game == null || !game.ShowChulengoResult)) return;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_ANDROID && !UNITY_EDITOR
            // Normal Android buttons remain native IMGUI; only the modal skip uses this pointer path.
            if (VictoryTransitionActive || victorySkipHeld)
#endif
            {
            var touch = Touchscreen.current; var mouse = Mouse.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
                HandlePointer(touch.primaryTouch.position.ReadValue(), touch.primaryTouch.press.wasPressedThisFrame, touch.primaryTouch.press.wasReleasedThisFrame);
            else if (mouse != null) HandlePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame);
            }
#endif
        }
        private static Rect CanvasViewport(Vector2 size, Rect safe)
        {
            if(safe.xMax>size.x+1||safe.yMax>size.y+1) safe=new Rect(0,0,size.x,size.y);
            float l = Mathf.Clamp(safe.xMin,0,size.x), r = Mathf.Clamp(safe.xMax,0,size.x);
            float b = Mathf.Clamp(safe.yMin,0,size.y), t = Mathf.Clamp(safe.yMax,0,size.y);
            if (r <= l || t <= b) { l = b = 0; r = size.x; t = size.y; }
            // Fill the usable portrait screen; the logical height grows on tall phones.
            // Layout sprites retain their proportions and use the same uniform pixel scale.
            return new Rect(l,size.y-t,r-l,t-b);
        }
        private static float CanvasLogicalHeight(Rect viewport) => viewport.width <= 0 ? H : viewport.height * W / viewport.width;
        private static float SafeAreaBottomInsetLogical(Rect viewport, float screenHeight) => viewport.width <= 0
            ? 0f : Mathf.Max(0f, screenHeight - viewport.yMax) * W / viewport.width;
        private static Vector2 ScreenToLayoutPoint(Vector2 pixel, Vector2 screenSize, Rect viewport)
        {
            float scale = viewport.width / W;
            float verticalScale = CanvasLogicalHeight(viewport) / H;
            Vector2 point = (new Vector2(pixel.x, screenSize.y - pixel.y) - viewport.position) / scale;
            point.y /= verticalScale;
            return point;
        }
        private Rect LayoutRect(Rect rect) { rect.y *= layoutVerticalScale; return rect; }
        private Rect PointerHitBounds(Rect bounds)
        {
            bounds.height /= Mathf.Max(.01f, layoutVerticalScale);
            return bounds;
        }
        private static Rect ScaleCardBounds(Rect bounds, float verticalScale)
        {
            float scale = Mathf.Min(1f, Mathf.Max(.01f, verticalScale));
            float width = bounds.width * scale;
            bounds.x += (bounds.width - width) * .5f;
            bounds.width = width;
            bounds.height *= scale;
            return bounds;
        }
        private static Rect ScaleCardChild(Rect child, Rect card, float scale)
        {
            float left = card.x + (card.width - card.width * scale) * .5f;
            return new Rect(left + (child.x - card.x) * scale, card.y + (child.y - card.y) * scale,
                child.width * scale, child.height * scale);
        }
        private Rect ResponsiveUpgradeCardBounds(StreetSimulation sim, int action)
        {
            ResponsiveFooterLayout footer = CurrentResponsiveFooter(sim, logicalCanvasHeight);
            int index = System.Array.IndexOf(CatalogUpgradeActions(sim), action);
            if (index < 0 || footer.UpgradeBounds == null || index >= footer.UpgradeBounds.Length) return Rect.zero;
            return ScaleCardBounds(footer.UpgradeBounds[index], layoutVerticalScale);
        }
        private void HandlePointer(Vector2 pixel, bool down, bool up)
        {
            if (game == null || !game.IsPlayerActive || game.Sim == null ||
                (Cloud.FirebasePlayerSession.Instance != null && Cloud.FirebasePlayerSession.Instance.BlocksGameInput)) return;
            if (ConsumeVictoryTransitionPointer(down, up)) return;
            Rect v = CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea);
            if (v.width <= 0) return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 p = ScreenToLayoutPoint(pixel, new Vector2(Screen.width, Screen.height), v);
            if (levelSelectActive && !game.ShowChulengoResult)
            {
                p.y *= layoutVerticalScale;
                if (down)
                {
                    pressedAction = 0;
                    levelSelectPointerHeld = LevelSelectorViewportBounds(logicalCanvasHeight).Contains(p);
                    levelSelectPointerDownY = p.y;
                    levelSelectPointerDownScroll = levelSelectScroll;
                    levelSelectDragging = false;
                    pressedAction = HitLevelSelectorAction(p);
                }
                if (!down && !up && levelSelectPointerHeld)
                {
                    float delta = levelSelectPointerDownY - p.y;
                    if (Mathf.Abs(delta) > 8f) { levelSelectDragging = true; pressedAction = 0; }
                    if (levelSelectDragging)
                        levelSelectScroll = Mathf.Clamp(levelSelectPointerDownScroll + delta, 0f, LevelSelectorMaxScroll(logicalCanvasHeight));
                }
                if (up)
                {
                    int hit = HitLevelSelectorAction(p);
                    if (!levelSelectDragging && pressedAction != 0 && pressedAction == hit)
                        DispatchAction(pressedAction);
                    pressedAction = 0;
                    levelSelectPointerHeld = false;
                    levelSelectDragging = false;
                }
                return;
            }
            if (marketActive && !game.ShowChulengoResult)
            {
                p.y *= layoutVerticalScale; // Market geometry uses the full logical safe-area height.
                if (down) { marketPointerHeld=true; marketPointerDownY = p.y; marketPointerDownScroll = marketScroll; marketDragging = false; pressedAction = HitMarketAction(p); }
                if (!down && !up && marketPointerHeld)
                {
                    float delta = marketPointerDownY - p.y;
                    if (Mathf.Abs(delta) > 8f) { marketDragging = true; pressedAction = 0; }
                    if (marketDragging) marketScroll = Mathf.Clamp(marketPointerDownScroll + delta, 0, MarketMaxScroll());
                }
                if (up)
                {
                    int hit = HitMarketAction(p);
                    if (!marketDragging && pressedAction != 0 && pressedAction == hit) DispatchAction(pressedAction);
                    pressedAction = 0; marketDragging = false; marketPointerHeld=false;
                }
                return;
            }
            if (down) pressedAction = HitAction(p);
            if (!up) return;
            if (pressedAction != 0 && pressedAction == HitAction(p)) DispatchAction(pressedAction);
            pressedAction = 0;
        }
        private int HitAction(Vector2 p)
        {
            if (VictoryTransitionActive || victorySkipHeld) return 0;
            if (game.ShowChulengoResult)
            {
                Vector2 resultPoint = new Vector2(p.x, p.y * layoutVerticalScale);
                if (game.HasPendingChulengo && game.ChulengoChoiceLocked)
                    return ChulengoMultiplyBounds().Contains(resultPoint) ? ChulengoMultiplyAction : 0;
                if (game.HasPendingChulengo)
                    return ChulengoMultiplyBounds().Contains(resultPoint) ? ChulengoMultiplyAction
                        : ChulengoCollectBounds().Contains(resultPoint) ? ChulengoCollectAction : 0;
                return game.Sim.LevelIndex < StreetSimulation.LevelNames.Length - 1 && VictoryExitBounds().Contains(resultPoint)
                    ? VictoryExitAction : ChulengoLevelsBounds().Contains(resultPoint) ? ResultLevelsAction : 0;
            }
            if (marketActive) { p.y *= layoutVerticalScale; return HitMarketAction(p); }
            if (introActive) return 0;
            if (levelOneTutorialActive)
            {
                if (LevelOneTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelOneTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (badgesActive) { p.y *= layoutVerticalScale; return BadgeBackBounds(logicalCanvasHeight).Contains(p) ? BadgeBackAction : 0; }
            if (rankingActive) { p.y *= layoutVerticalScale; return RankingBackBounds(logicalCanvasHeight).Contains(p) ? RankingBackAction : 0; }
            if (hubActive && !marketActive)
            {
                foreach (int action in new[] { HubAccountAction, HubRankingAction, HubBadgesAction, HubMarketAction, HubPlayAction })
                    if (HubArtworkActionBounds(action).Contains(p)) return action;
                return 0;
            }
            if (levelSelectActive)
            {
                p.y *= layoutVerticalScale;
                return HitLevelSelectorAction(p);
            }
            if (game.Sim.Phase == RoundPhase.Ready)
            {
                if (PointerHitBounds(Start).Contains(p)) return 1;
                Rect selector = CurrentResponsiveFooter(game.Sim, logicalCanvasHeight).ReadySelector;
                if (PointerHitBounds(selector).Contains(p)) return ReadyLevelSelectorAction;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Playing)
            {
                foreach (int action in CatalogUpgradeActions(game.Sim))
                    if (CatalogUpgradeTouchBounds(action).Contains(p)) return action;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Lost)
                return RiotResultButtonBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? RiotReturnAction
                    : ResultLevelsBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? ResultLevelsAction : 0;
            return game.Sim.Phase == RoundPhase.Won
                ? (game.Sim.LevelIndex < StreetSimulation.LevelNames.Length - 1 && VictoryExitBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? VictoryExitAction
                    : ResultLevelsBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? ResultLevelsAction : 0)
                : 0;
        }
        private void DispatchAction(int a)
        {
            if (VictoryTransitionActive || victorySkipHeld) return;
            if (game.ShowChulengoResult)
            {
                bool lockedRetry = game.HasPendingChulengo && game.ChulengoChoiceLocked;
                bool chulengoOk = a == ChulengoMultiplyAction ? (lockedRetry
                        ? game.ChulengoSelectedSector < 0 ? game.TryCollectVictoryReward()
                            : game.TryMultiplyVictoryReward(game.ChulengoSelectedSector)
                        : game.TryMultiplyVictoryRewardAtPosition(StreetChulengo.PositionAtTime(Time.unscaledTime)))
                    : a == ChulengoCollectAction ? game.TryCollectVictoryReward()
                    : a == VictoryExitAction && !game.HasPendingChulengo ? game.NextLevel()
                    : a == ResultLevelsAction && !game.HasPendingChulengo ? game.ReturnToLevels() : false;
                if (chulengoOk && a == VictoryExitAction) { marketActive = true; marketFromSelector = true; marketOtherExpanded = false; marketMessage = ""; }
                if (chulengoOk && a == ResultLevelsAction) { marketActive = false; levelSelectActive = true; levelSelectScroll = 0f; }
                if (a == ChulengoMultiplyAction || a == ChulengoCollectAction || a == VictoryExitAction || a == ResultLevelsAction)
                { lastAction = a; feedbackUntil = Time.unscaledTime + .22f; feedbackScale = chulengoOk ? 1.04f : .97f; }
                return;
            }
            if (introActive) return;
            if (levelOneTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction ||
                    (a == LevelOneTutorialPrimaryAction && levelOneTutorialStep == 3))
                {
                    if (game.TryStartRound())
                    {
                        levelOneTutorialActive = false;
                        levelOneTutorialStep = 0;
                        PlayerPrefs.SetInt(LevelOneTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelOneTutorialStep < 3)
                    levelOneTutorialStep++;
                return;
            }
            if (hubActive && !marketActive)
            {
                if (a == HubPlayAction)
                {
                    hubActive = false; levelSelectActive = true;
                    levelSelectScroll = 0f;
                }
                else if (a == HubMarketAction)
                {
                    marketActive = true; marketFromSelector = false; marketMessage = "";
                    marketScroll = 0; marketOtherExpanded = false; marketExpandedIngredient = -1;
                }
                else if (a == HubBadgesAction) { hubActive = false; badgesActive = true; }
                else if (a == HubRankingAction) { hubActive = false; rankingActive = true; }
                else if (a == HubAccountAction) Cloud.FirebasePlayerSession.Instance?.OpenAccountMenu();
                return;
            }
            if (badgesActive)
            {
                if (a == BadgeBackAction) { badgesActive = false; hubActive = true; }
                return;
            }
            if (rankingActive)
            {
                if (a == RankingBackAction) { rankingActive = false; hubActive = true; }
                return;
            }
            if (levelSelectActive)
            {
                if (a == MarketCloseAction) { levelSelectActive=false; marketActive=true; marketFromSelector=true; marketMessage=""; marketScroll=0; marketOtherExpanded=false; marketExpandedIngredient=-1; return; }
                if (a == LevelSelectorBackAction)
                {
                    levelSelectActive = false;
                    hubActive = true;
                    marketFromSelector = false;
                    return;
                }
                if (a >= LevelSelectFirstAction && a < LevelSelectFirstAction + StreetSimulation.LevelNames.Length)
                {
                    int level = a - LevelSelectFirstAction;
                    bool selectionSucceeded = level <= game.UnlockedLevel && game.SelectLevel(level);
                    if (selectionSucceeded)
                    {
                        levelSelectActive = false;
                        if (level == 0)
                        {
                            if (game.CanStartRound)
                            {
                                marketMessage = "";
                                if (PlayerPrefs.GetInt(LevelOneTutorialCompletedKey, 0) == 0)
                                {
                                    levelOneTutorialActive = true;
                                    levelOneTutorialStep = 0;
                                }
                                else game.TryStartRound();
                            }
                            else { marketMessage="Falta stock para iniciar. Revisá Mercado."; levelSelectActive=true; }
                        }
                        else { marketFromSelector = true; marketActive = true; marketMessage = ""; marketScroll = 0; marketOtherExpanded = false; marketExpandedIngredient = -1; }
                    }
                    lastAction = a; feedbackUntil = Time.unscaledTime + 0.22f; feedbackScale = selectionSucceeded ? 1.04f : 0.97f;
                    return;
                }
                return;
            }
            if (marketActive)
            {
                if (a == MarketBackAction) { marketActive=false; marketMessage=game.CanStartRound?"Stock suficiente para jugar.":"Falta stock para iniciar. Revisá Mercado."; if(marketFromSelector)levelSelectActive=true; return; }
                if (a == MarketOtherAction && MarketIngredientIds(false).Length > 0) { marketOtherExpanded = !marketOtherExpanded; marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll()); return; }
                if (a >= MarketExpandAction && a < MarketExpandAction + 10)
                {
                    int id = a - MarketExpandAction;
                    marketExpandedIngredient = MarketExpandedId() == id ? -2 : id;
                    MarketRevealIngredient(id); return;
                }
                if (a >= MarketMinusAction && a < MarketMinusAction+10) { int id=a-MarketMinusAction; marketPendingPacks[id]=Mathf.Max(0,marketPendingPacks[id]-1); return; }
                if (a >= MarketPlusAction && a < MarketPlusAction+10) { int id=a-MarketPlusAction; marketPendingPacks[id]=(int)System.Math.Min(int.MaxValue,(long)marketPendingPacks[id]+1); return; }
                if (a >= MarketPlusTenAction && a < MarketPlusTenAction+10) { int id=a-MarketPlusTenAction; marketPendingPacks[id]=(int)System.Math.Min(int.MaxValue,(long)marketPendingPacks[id]+10); return; }
                if (a >= MarketBuyAction && a < MarketBuyAction+10)
                {
                    int id=a-MarketBuyAction;
                    if(Time.unscaledTime-marketLastPurchaseTime<.25f)return;
                    marketLastPurchaseTime=Time.unscaledTime;
                    int packs=marketPendingPacks[id];long cost=(long)packs*game.Market.PackPriceCents(id);
                    bool success=packs>0&&cost<=game.Market.BalanceCents&&game.TryBuyIngredient(id,packs,game.Market.NextPurchaseId);
                    marketMessage=success?"Compra realizada. Podés seguir comprando.":cost>game.Market.BalanceCents?MarketInsufficientFundsMessage(cost):"No se pudo realizar la compra";if(success){marketPendingPacks[id]=0;marketExpandedIngredient=id;marketScroll=Mathf.Clamp(marketScroll,0,MarketMaxScroll());}return;
                }
                if(a>=MarketRowMissingAction&&a<MarketRowMissingAction+10)
                {
                    int id=a-MarketRowMissingAction;
                    if(Time.unscaledTime-marketLastPurchaseTime<.25f)return;
                    marketLastPurchaseTime=Time.unscaledTime;
                    long cost=(long)game.Market.MissingPacks(game.Sim,id)*game.Market.PackPriceCents(id);
                    if(cost>game.Market.BalanceCents){marketMessage=MarketInsufficientFundsMessage(cost);return;}
                    bool success=game.BuyMissingIngredient(id);
                    if(success)marketExpandedIngredient=id;
                    marketMessage=success?"Objetivo cubierto. Podés comprar más con + y COMPRAR.":"No hay faltante o no se pudo comprar";return;
                }
                if(a==MarketMissingAction)
                {
                    if(Time.unscaledTime-marketLastPurchaseTime<.25f)return;
                    marketLastPurchaseTime=Time.unscaledTime;
                    long cost=game.Market.MinimumPurchaseCostCents(game.Sim);
                    if(cost>game.Market.BalanceCents){marketMessage=MarketInsufficientFundsMessage(cost);return;}
                    bool bought=false;
                    for(int id=0;id<StreetIngredientCatalog.Names.Length;id++)if(game.Market.MissingPacks(game.Sim,id)>0)bought|=game.BuyMissingIngredient(id);
                    marketMessage=bought?"Compras mínimas realizadas":"No hay faltantes pendientes";return;
                }
                if(a==MarketPlayAction){marketMessage=game.TryStartRound()?"":"Falta stock para iniciar";if(game.Sim.Phase==RoundPhase.Playing){marketActive=false;hubActive=false;levelSelectActive=false;}return;}
            }
            bool ok = true;
            if (a == ReadyLevelSelectorAction && game.Sim.Phase == RoundPhase.Ready)
            {
                levelSelectActive = true;
                levelSelectScroll = 0f;
                return;
            }
            if (a==1) game.TryStartRound(); else if (a==2) ok=game.TryHireParrillero(); else if (a==3) ok=game.TryUpgradeSpeed(); else if (a==HireCocacoleroAction || a==HirePremiumAction || a==HireFerneteroAction) ok=game.TryHire(RoleForAction(a)); else if (a==HireMozoAction) ok=game.TryHireMozo();
            else if (a == RiotReturnAction && game.Sim.Phase == RoundPhase.Lost)
            {
                ok = game.ReturnToLevels();
                if (ok) { levelSelectActive = true; levelSelectScroll = 0f; }
            }
            else if (a == VictoryExitAction && game.Sim.Phase == RoundPhase.Won)
            {
                if (game.Sim.LevelIndex < StreetSimulation.LevelNames.Length - 1)
                {
                    ok=game.NextLevel();
                    if(ok){marketActive=true;marketFromSelector=true;marketOtherExpanded=false;marketMessage="";}
                }
                else ok=false;
            }
            else if (a == ResultLevelsAction && (game.Sim.Phase == RoundPhase.Won || game.Sim.Phase == RoundPhase.Lost))
            {
                ok=game.ReturnToLevels(); if(ok){marketActive=false;levelSelectActive=true;levelSelectScroll=0f;}
            }
            lastAction=a; feedbackUntil=Time.unscaledTime+0.22f; feedbackScale=ok?1.04f:0.97f;
        }
        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void NativeAction(int a)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            DispatchAction(a);
#elif !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            DispatchAction(a);
#endif
        }
        private static bool ShouldDrawUpgradeCards(RoundPhase phase, int catalogActionCount)
        {
            return phase == RoundPhase.Playing ||
                (phase == RoundPhase.Ready && catalogActionCount <= 3);
        }

        public bool AccountMenuVisible => !introActive && (hubActive || levelSelectActive || badgesActive || rankingActive || marketActive);
        public bool HubAccountActionRendered => hubActive && !marketActive && !levelSelectActive && !badgesActive && !rankingActive;

        private void OnGUI()
        {
            if (game==null||!game.IsPlayerActive||game.Sim==null ||
                (Cloud.FirebasePlayerSession.Instance != null && Cloud.FirebasePlayerSession.Instance.BlocksGameInput)) return;
            Styles(); Rect v=CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea); if(v.width<=0)return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            projectedCounterTopY = StreetSceneLayout.CounterTopInViewport(Screen.width, Screen.height, v);
            Matrix4x4 m=GUI.matrix; Color old=GUI.color;
            var sim=game.Sim;
            bool chulengoResult = game.ShowChulengoResult;
            if ((!introActive || chulengoResult) && !levelSelectActive && sim.Phase != RoundPhase.Lost) DrawGameplayScreenFill();
            GUI.matrix=Matrix4x4.TRS(new Vector3(v.x,v.y),Quaternion.identity,new Vector3(v.width/W,v.width/W,1)); GUI.color=Color.white;
            if (introActive && !chulengoResult)
            {
                DrawIntro(Time.unscaledTime - introStarted);
                GUI.color = old; GUI.matrix = m;
                return; // Do not create hidden gameplay GUI controls behind the startup menu.
            }
            if (hubActive && !marketActive && !chulengoResult)
            {
                DrawHubScreen();
                GUI.color=old; GUI.matrix=m;
                return;
            }
            if (badgesActive && !chulengoResult)
            {
                float pageScale = layoutVerticalScale; layoutVerticalScale = 1f;
                DrawBadgesScreen(); layoutVerticalScale = pageScale;
                GUI.color=old; GUI.matrix=m;
                return;
            }
            if (rankingActive && !chulengoResult)
            {
                float pageScale = layoutVerticalScale; layoutVerticalScale = 1f;
                DrawRankingScreen(); layoutVerticalScale = pageScale;
                GUI.color=old; GUI.matrix=m;
                return;
            }
            if (levelSelectActive && !chulengoResult)
            {
                float selectorScale = layoutVerticalScale;
                layoutVerticalScale = 1f;
                DrawLevelSelector();
                layoutVerticalScale = selectorScale;
                GUI.color=old; GUI.matrix=m;
                return; // Keep the locked/unlocked selector as the only interactive layer.
            }
            if (marketActive && !chulengoResult)
            {
                float marketScale = layoutVerticalScale;
                layoutVerticalScale = 1f;
                DrawMarketScreen();
                layoutVerticalScale = marketScale;
                GUI.color=old; GUI.matrix=m;
                return;
            }
            if (sim.Phase == RoundPhase.Lost)
            {
                DrawRiotScene();
                DrawCounters(sim);
                DrawRiotResult();
                GUI.color=old; GUI.matrix=m;
                return;
            }
            riotScreenActive = false;
            if (chulengoResult && !VictoryTransitionActive)
            {
                ResultPanel();
                GUI.color=old;GUI.matrix=m;
                return;
            }
            ResponsiveFooterLayout footer = CurrentResponsiveFooter(sim, logicalCanvasHeight);
            // The baked counter is rendered once by the full-bleed backdrop; queue clipping uses that same projection.
            DrawWaitingCrowd(sim);
            if (kitchenRenderer == null)
                kitchenRenderer = GetComponent<StreetKitchenRenderer>() ?? gameObject.AddComponent<StreetKitchenRenderer>();
            kitchenRenderer.Prepare(sim, layoutVerticalScale, logicalCanvasHeight);
            kitchenRenderer.PrepareWorkers(parrillero, parrilleroDiagonal, premiumWorker, premiumDiagonal,
                CocacoleroArtForLevel(sim.LevelIndex), UsesMatchedCocacoleroArt(sim.LevelIndex) ? fernetWorkerLater : fernetWorker,
                UsesMatchedCocacoleroArt(sim.LevelIndex));
            if (!kitchenRenderer.WorkersReady)
                for(int i=0;i<sim.Workers.Count;i++) DrawWorker(sim.Workers[i]);
            // Native independent furniture/food occlude the lower legs of workers reaching
            // from behind a table; the actual carried item remains above the hand/forearm.
            Rect kitchenCanvas = new Rect(0, 0, W, logicalCanvasHeight);
            if (kitchenRenderer.IsReady && kitchenRenderer.BackgroundTexture != null)
                GUI.DrawTexture(kitchenCanvas, kitchenRenderer.BackgroundTexture, ScaleMode.StretchToFill, true);
            if (kitchenRenderer.ForegroundTexture != null)
                GUI.DrawTexture(kitchenCanvas, kitchenRenderer.ForegroundTexture, ScaleMode.StretchToFill, true);
            if (kitchenRenderer.CarriedTexture != null)
                GUI.DrawTexture(kitchenCanvas, kitchenRenderer.CarriedTexture, ScaleMode.StretchToFill, true);
            if (sim.UsesActiveCooks) DrawCookingProgress(sim);
            for(int i=0;i<sim.Sales.Count;i++)
            {
                var s=sim.Sales[i];float a=Mathf.Clamp01(2.2f-s.Age);GUI.color=new Color(1,1,1,a);
                Item(new Rect(s.Position.x-27,s.Position.y-86-s.Age*33,23,23),10);
                Label(new Rect(s.Position.x-3,s.Position.y-86-s.Age*33,48,25),"+"+s.Amount,text);GUI.color=Color.white;
            }
            DrawCounters(sim);
            // The same footer geometry drives rendering, touch targets and the decorative pennant gap.
            int[] catalogActions = CatalogUpgradeActions(sim);
            if (VictoryTransitionActive || ShouldDrawUpgradeCards(sim.Phase, catalogActions.Length))
                for (int i = 0; i < catalogActions.Length; i++)
                {
                    int action = catalogActions[i];
                    bool speed = action == 3;
                    StreetWorkerRole role = RoleForAction(action);
                    int cost = speed ? sim.SpeedCost : role == StreetWorkerRole.Mozo ? sim.MozoHireCost : sim.HireCostForRole(role);
                    bool canBuy = speed ? sim.CanUpgradeSpeed : role == StreetWorkerRole.Mozo ? sim.CanHireMozo : sim.CanHireRole(role);
                    Upgrade(ScaleCardBounds(footer.UpgradeBounds[i], layoutVerticalScale), action, speed ? 15 : 16,
                        speed ? "VELOCIDAD" : RoleCaption(role), cost, canBuy);
                }
            if (sim.Phase == RoundPhase.Ready)
            {
                if (levelOneTutorialActive) DrawLevelBadge(game.SelectedLevel, footer.LevelBadge);
                else ReadyPanel(footer);
            }
            else if (VictoryTransitionActive) DrawVictoryTransition();
            else if(sim.Phase==RoundPhase.Won||sim.Phase==RoundPhase.Lost) ResultPanel();
            else DrawLevelBadge(sim.LevelIndex, footer.LevelBadge);
            if (levelOneTutorialActive) DrawLevelOneTutorial();
            GUI.color=old;GUI.matrix=m;
        }
        private static Rect ScreenBounds(Vector2 size) => new Rect(0, 0, size.x, size.y);
        private void DrawRiotScene()
        {
            UpdateRiotState();
            if (!riotScreenActive)
            {
                riotScreenActive = true;
                riotStartedAt = Time.unscaledTime;
            }

            float elapsed = Mathf.Max(0f, Time.unscaledTime - riotStartedAt);
            // Keep the same safe-canvas counter/queue alignment during the initial anger reaction.
            DrawGameplayScreenFill();

            float destruction = IntroEase(Mathf.Clamp01((elapsed - RiotBreakStartSeconds) / RiotBreakTransitionSeconds));
            if (riotBackdrop != null && destruction > 0f)
            {
                Matrix4x4 previous = GUI.matrix;
                Color previousColor = GUI.color;
                try
                {
                    GUI.matrix = Matrix4x4.identity;
                    float shake = IntroEase(Mathf.Clamp01((elapsed - .56f) / .28f));
                    float x = RiotShakeX(elapsed) * shake;
                    float y = RiotShakeY(elapsed) * shake;
                    const float inset = 7f;
                    GUI.color = new Color(1f, 1f, 1f, destruction);
                    GUI.DrawTexture(new Rect(-inset + x, -inset + y,
                        Screen.width + inset * 2f, Screen.height + inset * 2f),
                        riotBackdrop, ScaleMode.ScaleAndCrop, true);
                }
                finally { GUI.matrix = previous; GUI.color = previousColor; }

                // The wrecked-stall plate has its own generic wall. Keep the active level's
                // gameplay mural/header in front of it so the location does not change at defeat.
                DrawGameplayMuralInScreenSpace();
            }

            DrawRiotWaitingCrowd(game != null ? game.Sim : null, elapsed);
            DrawRiotImpactCloud(elapsed);
            DrawRiotDebris(elapsed);
        }

        private void DrawRiotImpactCloud(float elapsed)
        {
            if (riotImpactCloud == null || elapsed >= RiotImpactCloudDuration) return;

            // A brief oversized comic dust cloud masks the direct gameplay-to-riot cut.
            // Its unscaled-time expansion/fade remains active while the simulation is frozen.
            float grow = IntroEase(Mathf.Clamp01(elapsed / .33f));
            float fade = 1f - IntroEase(Mathf.Clamp01((elapsed - .72f) / (RiotImpactCloudDuration - .72f)));
            float alpha = Mathf.Clamp01(elapsed / .055f) * fade;
            float pulse = 1f + Mathf.Sin(elapsed * 16f) * .028f;
            float width = Mathf.Lerp(96f, 520f, grow) * pulse;
            float height = width * .5f;
            float x = (W - width) * .5f + Mathf.Sin(elapsed * 9f) * 2.5f;
            Vector2 queueCenter = CustomerViewPosition(new Vector2(W * .5f,
                StreetSimulation.FrontQueueY - StreetSimulation.QueueRowSpacing));
            float y = queueCenter.y - 58f - height / (2f * layoutVerticalScale)
                + Mathf.Sin(elapsed * 7f) * 2.8f;
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(LayoutRect(new Rect(x, y, width, height)), riotImpactCloud, ScaleMode.ScaleToFit, true);
            }
            finally { GUI.color = previous; }
        }

        private void LateUpdate() { UpdateRiotState(); ObserveVictoryTransition(); }
        private void UpdateRiotState()
        {
            bool lost = game != null && game.Sim != null && game.Sim.Phase == RoundPhase.Lost;
            if (lost && !riotScreenActive)
            {
                riotScreenActive = true;
                riotStartedAt = Time.unscaledTime;
            }
            else if (!lost) riotScreenActive = false;
        }

        private static float RiotShakeX(float elapsed) => Mathf.Sin(elapsed * 27f) * 4.2f;
        private static float RiotShakeY(float elapsed) => Mathf.Sin(elapsed * 21f + .8f) * 2.6f;
        private static Vector2 RiotDebrisPosition(float elapsed, int index)
        {
            float age = Mathf.Repeat(elapsed * (.72f + (index % 3) * .13f) + index * .19f, 1f);
            float direction = (index % 2 == 0 ? -1f : 1f);
            float x = 270f + direction * (28f + (index % 4) * 39f) * age
                + Mathf.Sin(elapsed * 5f + index * 1.7f) * 13f;
            float y = 485f - 124f * age + 180f * age * age;
            return new Vector2(x, y);
        }

        private void DrawRiotWaitingCrowd(StreetSimulation sim, float elapsed)
        {
            if (sim == null || sim.Customers == null) return;
            Color previousColor = GUI.color;
            // Legs remain behind the intact stand; reveal the full riot poses only once it breaks.
            float clipHeight = elapsed < RiotBreakStartSeconds + RiotBreakTransitionSeconds
                ? CustomerCounterTopY() : logicalCanvasHeight;
            GUI.BeginGroup(new Rect(0, 0, W, clipHeight));
            try
            {
                // Use the frozen queue itself rather than introducing a new, unrelated crowd.
                for (int row = 4; row >= 0; row--)
                for (int i = 0; i < sim.Customers.Count; i++)
                {
                    StreetCustomer customer = sim.Customers[i];
                    if (customer.State != StreetCustomerState.Waiting &&
                        customer.State != StreetCustomerState.Receiving &&
                        customer.State != StreetCustomerState.Advancing) continue;
                    int customerRow = Mathf.Clamp(Mathf.RoundToInt(
                        (StreetSimulation.FrontQueueY - customer.Position.y) / StreetSimulation.QueueRowSpacing), 0, 4);
                    if (customerRow != row) continue;
                    DrawRiotCustomer(customer, sim.LevelIndex, elapsed);
                }
            }
            finally { GUI.color = previousColor; GUI.EndGroup(); }
        }

        private void DrawRiotCustomer(StreetCustomer customer, int levelIndex, float elapsed)
        {
            levelIndex = VisualLevelIndex;
            Vector2 position = CustomerViewPosition(customer.Position);
            ClubFanDefinition fans = ClubVisualTheme.ForLevel(levelIndex).Fans;
            Texture2D activeRiotFans = fans != null ? fans.LoadRiotFans() : null;
            if (activeRiotFans == null) activeRiotFans = riotFanAtlas; // Emergency fallback only; registered clubs have explicit assets.
            float stagger = (customer.Id % 5) * .055f;
            float fury = IntroEase(Mathf.Clamp01((elapsed - stagger) / RiotAngerRampSeconds));
            float rhythm = elapsed * (10f + (customer.Id % 3) * 1.25f) + customer.Id * 1.71f;
            float attack = IntroEase(Mathf.Clamp01((elapsed - .42f) / .55f));
            float sway = Mathf.Sin(rhythm) * (1.2f + attack * 5.2f);
            float hop = Mathf.Abs(Mathf.Sin(rhythm * 1.16f)) * (1.2f + attack * 7f);
            float surge = attack * 13f;

            Rect calmPose = new Rect(position.x - 39f + sway,
                position.y - 76f - hop + surge, 78f, 76f);
            if (fury < 1f || activeRiotFans == null)
            {
                float calmAlpha = activeRiotFans == null ? 1f : 1f - fury;
                GUI.color = new Color(1f, 1f, 1f, calmAlpha);
                DrawFan(calmPose, customer.Id, false, false, fans);
            }

            Rect angryPose = new Rect(position.x - 44f + sway,
                position.y - 88f - hop + surge, 88f, 88f);
            if (fury > 0f && activeRiotFans != null)
            {
                GUI.color = new Color(1f, 1f, 1f, fury);
                int pose = RiotFanPoseIndex(elapsed, customer.Id);
                if (fans != null && activeRiotFans != riotFanAtlas)
                    angryPose = fans.RiotDrawBounds(angryPose, customer.Id, pose);
                Rect source;
                source = fans != null && activeRiotFans != riotFanAtlas
                    ? fans.RiotSourceRect(activeRiotFans.width, activeRiotFans.height, customer.Id, pose)
                    : RiotFanFrameSource(activeRiotFans, customer.Id, pose);
                // Preserve each atlas cell's authored aspect ratio; Velez's 6x3 source cells are not square.
                Draw(angryPose, activeRiotFans, source, false, false);
            }

            float cueAlpha = Mathf.Clamp01((elapsed - .08f) / .2f) * Mathf.Clamp01((1.1f - elapsed) / .3f);
            if (riotCue != null && cueAlpha > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, cueAlpha);
                Label(new Rect(position.x - 17f + sway, position.y - 112f - hop + surge,
                    34f, 32f), "¡!", riotCue);
            }
            GUI.color = Color.white;
        }

        private static int RiotFanPoseIndex(float elapsed, int customerId)
        {
            int tick = Mathf.FloorToInt((elapsed + (customerId % 5) * .047f) * RiotFanFramesPerSecond);
            return tick & 1; // Raised stick, then forceful non-contact swing; repeats while Lost.
        }

        private static Rect AllBoysRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = GetAllBoysFanVariant(customerId);
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 3f;
            int outfitColumn = variant % 3;
            int pairedPoseColumn = outfitColumn * 2 + (pose & 1);
            int rowFromTop = variant / 3;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }

        private static Rect ChicagoRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = GetChicagoFanVariant(customerId);
            float cellWidth = atlas.width / 4f;
            float cellHeight = atlas.height / 4f;
            int outfitColumn = variant % 2;
            int pairedPoseColumn = outfitColumn * 2 + (pose & 1);
            int rowFromTop = variant / 2;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }

        private static Rect RiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            float cellWidth = atlas.width / 4f;
            float split = Mathf.Clamp(RiotFanAtlasRowSplitY, atlas.height * .5f, atlas.height - 1f);
            return pose == 0
                ? new Rect((customerId % 4) * cellWidth, 0f, cellWidth, split)
                : new Rect((customerId % 4) * cellWidth, split, cellWidth, atlas.height - split);
        }

        private void DrawRiotDebris(float elapsed)
        {
            float impact = IntroEase(Mathf.Clamp01((elapsed - .55f) / .4f));
            if (impact <= 0f) return;
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float scale = Screen.width / W;
            try
            {
                for (int i = 0; i < 9; i++)
                {
                    Vector2 position = RiotDebrisPosition(elapsed, i);
                    float age = Mathf.Repeat(elapsed * (.72f + (i % 3) * .13f) + i * .19f, 1f);
                    float alpha = Mathf.Clamp01(Mathf.Min(age * 5f, (1f - age) * 4f)) * impact;
                    float width = 5f + (i % 3) * 2f;
                    float height = 22f + (i % 4) * 7f;
                    Rect shard = new Rect(position.x - width * .5f, position.y - height * .5f, width, height);
                    GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                    GUIUtility.RotateAroundPivot(Mathf.Sin(elapsed * 15f + i * 1.9f) * 72f,
                        new Vector2(position.x, position.y));
                    GUI.color = new Color(.35f + (i % 2) * .12f, .16f, .055f, alpha);
                    GUI.DrawTexture(shard, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }
            }
            finally { GUI.matrix = previous; GUI.color = color; }
        }

        private Rect RiotResultButtonBounds()
        {
            Rect bounds = RiotReturnButton;
            bounds.y = Mathf.Min(bounds.y, logicalCanvasHeight - bounds.height - 12f);
            return bounds;
        }
        private Rect ResultLevelsBounds() => new Rect(150, logicalCanvasHeight - 72f, 240, 52);
        private Rect ChulengoCollectBounds()
        {
            Rect frame = VictoryPopupBounds(logicalCanvasHeight, true);
            return ChulengoCollectBoundsForFrame(frame, logicalCanvasHeight);
        }
        public static Rect ChulengoCollectBoundsForFrame(Rect frame, float canvasHeight)
        {
            float multiplyBottom = ChulengoMultiplyBoundsForFrame(frame).yMax;
            float responsiveScale = Mathf.Clamp(canvasHeight / 1200f, .65f, 1f);
            float height = 44f * responsiveScale;
            return new Rect(150, Mathf.Max(canvasHeight - 130f * responsiveScale, multiplyBottom + 8f),
                240f * responsiveScale, height);
        }
        private Rect ChulengoLevelsBounds() => ChulengoLevelsBoundsForFrame(
            VictoryPopupBounds(logicalCanvasHeight, true), logicalCanvasHeight);
        public static Rect ChulengoLevelsBoundsForFrame(Rect frame, float canvasHeight)
        {
            Rect collect = ChulengoCollectBoundsForFrame(frame, canvasHeight);
            float responsiveScale = Mathf.Clamp(canvasHeight / 1200f, .65f, 1f);
            float height = 52f * responsiveScale;
            float lastSafeTop = canvasHeight - 4f - height;
            float preferredTop = Mathf.Max(canvasHeight - 72f * responsiveScale, collect.yMax + 14f);
            return new Rect(150, Mathf.Min(lastSafeTop, preferredTop), 240f * responsiveScale, height);
        }

        private static Rect RiotGameOverBounds(float canvasHeight)
        {
            float height = Mathf.Min(380f, Mathf.Max(200f, canvasHeight * .5f));
            return new Rect(18f, (canvasHeight - height) * .5f, W - 36f, height);
        }

        private void DrawRiotResult()
        {
            float previousVertical = layoutVerticalScale;
            layoutVerticalScale = 1f;
            try
            {
                Rect titleBounds = RiotGameOverBounds(logicalCanvasHeight);
                DrawRiotWaitMessage(new Rect(22f, titleBounds.y - 66f, W - 44f, 56f));
                DrawAnimatedGameOver(titleBounds, Mathf.Max(0f, Time.unscaledTime - riotStartedAt));
                DrawStandardButton(RiotResultButtonBounds(), RiotReturnAction, "VOLVER A NIVELES");
            }
            finally { layoutVerticalScale = previousVertical; }
        }

        private void DrawRiotWaitMessage(Rect bounds)
        {
            const string message = "Los hinchas se cansaron de esperar......";
            int fontSize = FitStandardButtonFontSize(menuTitle, message, bounds);
            GUIStyle caption = CreateStandardButtonLabelStyle(menuTitle, fontSize, Color.white);
            GUIStyle outline = CreateStandardButtonLabelStyle(menuTitle, fontSize, new Color(.035f, .075f, .16f));
            float stroke = 2.2f * fontSize / 38f;
            Rect letters = new Rect(bounds.x, bounds.y - 2f * bounds.height / 92f,
                bounds.width, bounds.height - 8f * bounds.height / 92f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(letters.x + x * stroke, letters.y + y * stroke,
                        letters.width, letters.height), message, outline);
            Label(letters, message, caption);
        }

        private void DrawAnimatedGameOver(Rect bounds, float elapsed)
        {
            float pulse = 1f + Mathf.Sin(elapsed * 7f) * .055f;
            float shake = Mathf.Sin(elapsed * 19f) * 1.25f;
            float bob = Mathf.Sin(elapsed * 4.5f) * 1.5f;
            Rect animated = new Rect(bounds.center.x + shake - bounds.width * pulse * .5f,
                bounds.center.y + bob - bounds.height * pulse * .5f,
                bounds.width * pulse, bounds.height * pulse);
            float flash = .5f + .5f * Mathf.Sin(elapsed * 5.5f);
            Color fill = Color.Lerp(new Color(1f, .32f, .07f), new Color(1f, .82f, .16f), flash);
            int previousSize = menuTitle.fontSize;
            TextAnchor previousAlignment = menuTitle.alignment;
            menuTitle.alignment = TextAnchor.MiddleCenter;
            Rect gameLine = new Rect(animated.x, animated.y, animated.width, animated.height * .5f);
            Rect overLine = new Rect(animated.x, animated.y + animated.height * .5f, animated.width, animated.height * .5f);
            menuTitle.fontSize = FitVictoryFont(menuTitle, "GAME", 190, gameLine);
            OutlineLabel(gameLine, "GAME", menuTitle, fill, 2f);
            menuTitle.fontSize = FitVictoryFont(menuTitle, "OVER", 190, overLine);
            OutlineLabel(overLine, "OVER", menuTitle, fill, 2f);
            menuTitle.fontSize = previousSize;
            menuTitle.alignment = previousAlignment;
        }

        private void DrawGameplayScreenFill()
        {
            // SafeArea is for controls, not a clipping boundary for scenery.
            // Paint behind the camera/cutout and navigation insets before the safe canvas.
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float scale = Screen.width / W;
            float fullHeight = Screen.height / scale;
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float canvasHeight = CanvasLogicalHeight(viewport);
            float bottomBleed = SafeAreaBottomInsetLogical(viewport, Screen.height);
            ResponsiveFooterLayout footer = CurrentResponsiveFooter(game.Sim, canvasHeight, bottomBleed);
            Rect pennantArtBounds = FooterPennantRenderBounds(footer.Pennants, canvasHeight, bottomBleed);
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                GUI.color = Color.white;
                DrawBackdropLayers(fullHeight, fullHeight / H, viewport, pennantArtBounds);
            }
            finally { GUI.matrix = previous; GUI.color = color; }
        }
        private static Rect FooterPennantRenderBounds(Rect layoutBounds, float safeCanvasHeight, float bottomBleed)
        {
            // A zero slot means the playfield/footer has no safe pennant band at this aspect.
            // Do not fake one behind the cards: that hides fabric and makes the block spacing lie.
            return layoutBounds.height > 1f ? layoutBounds : Rect.zero;
        }
        private Texture2D CurrentGameplayBackdrop()
        {
            if (game == null || game.Sim == null) return backdrop;
            if (VisualLevelIndex == RiverVisualLevelIndex && riverMural != null) return riverMural;
            if (game.Sim.LevelIndex == 1 && chicagoBackground != null) return chicagoBackground;
            if (game.Sim.LevelIndex == 2 && velezBackground != null) return velezBackground;
            if (game.Sim.LevelIndex == 3 && ferroBackground != null) return ferroBackground;
            if (game.Sim.LevelIndex == 4 && independienteBackground != null) return independienteBackground;
            if (game.Sim.LevelIndex == 5 && racingBackground != null) return racingBackground;
            if (game.Sim.LevelIndex == 6 && sanLorenzoBackground != null) return sanLorenzoBackground;
            if (game.Sim.LevelIndex == 8 && bocaBackground != null) return bocaBackground;
            if (game.Sim.LevelIndex == 9 && camionerosBackground != null) return camionerosBackground;
            if (game.Sim.LevelIndex == 10 && losRedondosBackground != null) return losRedondosBackground;
            return backdrop;
        }
        private void DrawBackdropLayers(float canvasHeight, float verticalScale, Rect safeViewport, Rect safePennants)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            // The root public ground/counter/operator floor is invariant; only scenery is themed.
            Color previous = GUI.color;
            GUI.color = theme.SceneryTint;
            if (backdrop != null)
                GUI.DrawTexture(StreetSceneLayout.BackgroundBounds(canvasHeight), backdrop, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            if (game != null && game.Sim != null && game.Sim.UsesActiveCooks && backdrop != null)
            {
                // The wide root plate contains an obsolete condiment table on its right edge.
                // Reuse its clean opposite counter/floor edge, not a new or repainted asset.
                DrawRaw(StreetSceneLayout.ProjectSourceRect(StreetSceneLayout.BakedSideTableSource, canvasHeight),
                    backdrop, StreetSceneLayout.CleanCounterSideSource, true, true);
            }
            DrawGameplayMural(canvasHeight, verticalScale);
            DrawClubPennants(canvasHeight, verticalScale, safeViewport, safePennants);
        }

        private void DrawGameplayMural(float canvasHeight, float verticalScale)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            if (!string.IsNullOrEmpty(theme.MuralResource))
                DrawThemedMural(theme, canvasHeight, verticalScale);
            else if (muralArt != null)
            {
                Rect band = new Rect(0f, HudBar.yMax, W, MuralBounds.height * verticalScale);
                DrawRawCover(band, muralArt, MuralSource);
            }
        }

        private void DrawGameplayMuralInScreenSpace()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            float scale = Screen.width / W;
            float fullHeight = Screen.height / scale;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                GUI.color = Color.white;
                DrawGameplayMural(fullHeight, fullHeight / H);
            }
            finally { GUI.matrix = previous; GUI.color = previousColor; }
        }

        private void DrawThemedMural(ClubVisualTheme theme, float canvasHeight, float verticalScale)
        {
            if (clubMuralResource != theme.MuralResource)
            {
                clubMuralResource = theme.MuralResource;
                clubMuralTexture = Resources.Load<Texture2D>(clubMuralResource);
            }
            if (clubMuralTexture == null) return;
            // Fit the complete mural below the fixed HUD, never hide portraits behind it.
            Rect band = new Rect(0, HudBar.yMax, W, MuralBounds.height * verticalScale);
            Rect normalized = theme.MuralSource;
            Rect source = new Rect(normalized.x * clubMuralTexture.width, normalized.y * clubMuralTexture.height,
                normalized.width * clubMuralTexture.width, normalized.height * clubMuralTexture.height);
            float naturalHeight = W * source.height / Mathf.Max(1f, source.width);
            float availableHeight = Mathf.Max(0f, StreetSceneLayout.CounterTop(canvasHeight) - band.y);
            // Align the complete, aspect-preserved crop directly below the HUD; expand only to its
            // natural aspect or the available counter band, whichever is smaller.
            band.height = Mathf.Min(Mathf.Max(band.height, naturalHeight), availableHeight);
            if (game != null && game.Sim != null && game.Sim.UsesActiveCooks)
                band = StreetSceneLayout.ActiveMuralBand(canvasHeight, verticalScale, HudBar.yMax);
            // Draw an independent neighborhood continuation behind the HUD. Prefer the full
            // club backdrop when the cropped mural has no suitable matching pixels above it.
            Texture2D continuation = VisualLevelIndex == RiverVisualLevelIndex && riverHeaderExtension != null
                ? riverHeaderExtension : CurrentGameplayBackdrop();
            if (continuation != null && continuation != clubMuralTexture)
                DrawMuralHeaderFromBackdrop(continuation, band);
            else
                DrawMuralContinuation(clubMuralTexture, source, band);
            DrawRawCover(band, clubMuralTexture, source);
        }

        private static void DrawMuralHeaderFromBackdrop(Texture2D texture, Rect muralBand)
        {
            if (texture == null || muralBand.y <= 0f || muralBand.width <= 0f) return;
            float extensionHeight = Mathf.Min(muralBand.y, HudBar.yMax);
            float sourceHeight = texture.width * extensionHeight / muralBand.width;
            if (sourceHeight > texture.height)
            {
                sourceHeight = texture.height;
                extensionHeight = sourceHeight * muralBand.width / texture.width;
            }
            Rect source = new Rect(0f, 0f, texture.width, sourceHeight);
            Rect destination = new Rect(muralBand.x, muralBand.y - extensionHeight,
                muralBand.width, extensionHeight);
            DrawRaw(destination, texture, source, true, false);
        }

        private static void DrawMuralContinuation(Texture2D texture, Rect muralSource, Rect muralBand)
        {
            if (texture == null || muralBand.y <= 0f || muralBand.width <= 0f || muralBand.height <= 0f) return;

            // DrawRawCover crops the source to this aspect ratio. Use that same crop and scale so
            // adjacent authored pixels join the unchanged mural band without distortion.
            Rect visibleSource = CropSourceToAspect(muralSource, muralBand.width / muralBand.height);
            if (visibleSource.y <= 0f || visibleSource.height <= 0f) return;

            float extensionHeight = Mathf.Min(muralBand.y,
                visibleSource.y * muralBand.height / visibleSource.height);
            if (extensionHeight <= 0f) return;

            float sourceHeight = extensionHeight * visibleSource.height / muralBand.height;
            Rect source = new Rect(visibleSource.x, visibleSource.y - sourceHeight,
                visibleSource.width, sourceHeight);
            Rect destination = new Rect(muralBand.x, muralBand.y - extensionHeight,
                muralBand.width, extensionHeight);
            DrawRaw(destination, texture, source, true, false);
        }

        private void DrawClubPennants(float canvasHeight, float verticalScale, Rect safeViewport, Rect safePennants)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            Color previousColor = GUI.color;
            try
            {
                if (game != null && game.Sim != null && game.Sim.UsesActiveCooks)
                {
                    // Continue the tiled pavement over the baked cream footer. The source art
                    // changes from tile to cream below row ~1150, so map a reusable tile band
                    // instead of sampling deeper into the backdrop (which creates a white stripe).
                    if (backdrop != null)
                    {
                        Vector2 textureSize = new Vector2(backdrop.width, backdrop.height);
                        Rect pavement = StreetSceneLayout.PavementContinuationSource(textureSize);
                        float floorTop = StreetKitchenLayout.TiledPlayfieldBottom * verticalScale;
                        float fieldBottom = game.Sim.KitchenLayout.PlayfieldBottom * verticalScale;
                        float overlap = StreetSceneLayout.PavementContinuationOverlap(Screen.width);
                        float tileHeight = StreetSceneLayout.PavementContinuationTileHeight(canvasHeight, textureSize);
                        if (pavement.width > 0f && pavement.height > 0f && tileHeight > 0f)
                        {
                            // Match the backdrop's scenery tint so the continuation does not
                            // introduce a brighter horizontal band at its edge.
                            GUI.color = theme.SceneryTint;
                            for (float tileY = floorTop; tileY < fieldBottom; tileY += tileHeight)
                            {
                                // The clean source span avoids grout joints; overdraw closes raster gaps.
                                float drawTop = Mathf.Max(0f, tileY - overlap);
                                float drawBottom = Mathf.Min(fieldBottom, tileY + tileHeight + overlap);
                                float drawnHeight = Mathf.Max(0f, drawBottom - drawTop);
                                // Keep sampling inside the clean crop even when the screen-space bleed expands the destination.
                                float sourceHeight = pavement.height * Mathf.Min(1f, drawnHeight / tileHeight);
                                Rect uv = new Rect(pavement.x / backdrop.width,
                                    1f - (pavement.y + sourceHeight) / backdrop.height,
                                    pavement.width / backdrop.width, sourceHeight / backdrop.height);
                                GUI.DrawTextureWithTexCoords(new Rect(0f, drawTop, W, drawnHeight), backdrop, uv, true);
                            }
                        }
                    }
                    DrawFooterBluePanel(new Rect(0f, game.Sim.KitchenLayout.PlayfieldBottom * verticalScale,
                        W, Mathf.Max(0f, canvasHeight - game.Sim.KitchenLayout.PlayfieldBottom * verticalScale)), verticalScale);
                }
                else
                {
                    // Replace the complete baked cream footer, including behind the transparent pennants.
                    float footerTop = game.Sim.KitchenLayout.PlayfieldBottom * verticalScale;
                    DrawFooterBluePanel(new Rect(0f, footerTop, W, Mathf.Max(0f, canvasHeight - footerTop)), verticalScale);
                }

                if (safePennants.height <= 1f || safeViewport.width <= 0f) return;
                if (clubPennantTexture == null || clubPennantThemeName != theme.ClubName ||
                    Mathf.Abs(clubPennantLogicalHeight - safePennants.height) > .25f)
                {
                    if (clubPennantTexture != null) Destroy(clubPennantTexture);
                    clubPennantTexture = CreateFooterPennants(theme, safePennants.height);
                    clubPennantThemeName = theme.ClubName;
                    clubPennantLogicalHeight = safePennants.height;
                }

                float fullScale = Screen.width / W;
                float safeScale = safeViewport.width / W;
                Rect destination = new Rect(0f,
                    (safeViewport.y + safePennants.y * safeScale) / fullScale,
                    W, safePennants.height * safeScale / fullScale);
                GUI.color = Color.white;
                // The generated texture matches this full-width destination aspect; keep its pennant geometry undistorted.
                GUI.DrawTexture(destination, clubPennantTexture, ScaleMode.ScaleToFit, true);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private static Texture2D CreateFooterPennants(ClubVisualTheme theme, float logicalHeight, bool keepReadable = false) =>
            ClubPennantRenderer.CreateAdaptiveTexture(theme, logicalHeight, keepReadable, false);

        private void DrawFooterBluePanel(Rect bounds, float verticalScale)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            float scale = Mathf.Max(.01f, verticalScale);
            float darkEdgeHeight = Mathf.Min(2f * scale, bounds.height);
            float warmEdgeHeight = Mathf.Min(scale, Mathf.Max(0f, bounds.height - darkEdgeHeight));
            Rect woodBounds = new Rect(bounds.x, bounds.y + darkEdgeHeight + warmEdgeHeight,
                bounds.width, Mathf.Max(0f, bounds.height - darkEdgeHeight - warmEdgeHeight));
            Color previous = GUI.color;
            try
            {
                if (gameplayFooterTexture != null && woodBounds.height > 0f)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(woodBounds, gameplayFooterTexture, ScaleMode.StretchToFill, true);
                }
                else
                {
                    GUI.color = new Color(.025f, .12f, .34f, 1f);
                    GUI.DrawTexture(woodBounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }

                GUI.color = new Color(.012f, .035f, .13f, 1f);
                GUI.DrawTexture(new Rect(bounds.x, bounds.y, bounds.width, darkEdgeHeight),
                    Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                if (warmEdgeHeight > 0f)
                {
                    GUI.color = new Color(.12f, .72f, 1f, 1f);
                    GUI.DrawTexture(new Rect(bounds.x, bounds.y + darkEdgeHeight, bounds.width, warmEdgeHeight),
                        Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }
            }
            finally { GUI.color = previous; }
        }

        private const float CookingRingRadius = 10f;
        private const float CookingRingDotSize = 3f;
        private const float CookingRingHeadGap = 4f;
        internal static float CookingProgressRingCenterY(float workerFootY, float verticalScale,
            float visibleHairHeight, float spriteRootOffset)
        {
            float scale = Mathf.Max(.01f, verticalScale);
            float ringOuterRadius = CookingRingRadius + CookingRingDotSize * .5f;
            float visibleHairTopY = workerFootY * scale + spriteRootOffset - visibleHairHeight;
            return visibleHairTopY - CookingRingHeadGap - ringOuterRadius;
        }
        internal static Color CookingRingProgressColor(StreetWorkerRole role) =>
            role == StreetWorkerRole.ParrilleroPremium
                ? (Color)new Color32(0x16, 0x8B, 0xFF, 0xFF)
                : Color.white;

        private readonly System.Collections.Generic.List<Vector2> cookingRingCenters = new System.Collections.Generic.List<Vector2>();
        private void DrawCookingProgress(StreetSimulation sim)
        {
            cookingRingCenters.Clear();
            Color old = GUI.color;
            const int segments = 24;
            foreach (StreetWorker worker in sim.Workers)
            {
                if (worker.State != StreetWorkerState.Cooking && !worker.VisualCooking) continue;
                Vector2 visualPosition = StreetWorkerProjection.PresentationPosition(sim, worker);
                float cx = Mathf.Clamp(visualPosition.x, 14f, W - 14f);
                StreetWorkerPose pose = StreetWorkerAnimation.Sample(worker);
                bool nativeCookingPose = kitchenRenderer != null && kitchenRenderer.WorkersReady &&
                    kitchenRenderer.CookingPoseAvailable;
                float hairHeight = nativeCookingPose
                    ? StreetWorkerProjection.CookingPoseVisibleHairHeight(pose.CookingFrame)
                    : StreetWorkerProjection.Height;
                float spriteRootOffset = nativeCookingPose
                    ? StreetWorkerProjection.CookingPoseFootInset(pose.CookingFrame)
                    : 0f;
                float cy = CookingProgressRingCenterY(visualPosition.y, layoutVerticalScale,
                    hairHeight, spriteRootOffset);
                // Several real slots may share the same approach column. Stack only
                // their progress rings, rather than overlapping independent clocks.
                for (int attempt = 0; attempt <= sim.Workers.Count; attempt++)
                {
                    bool overlaps = false;
                    for (int j = 0; j < cookingRingCenters.Count; j++)
                        if ((cookingRingCenters[j] - new Vector2(cx, cy)).sqrMagnitude < 28f * 28f)
                        { overlaps = true; break; }
                    if (!overlaps) break;
                    cy -= 28f;
                }
                cookingRingCenters.Add(new Vector2(cx, cy));
                int lit = Mathf.CeilToInt(Mathf.Clamp01(worker.CookingProgress) * segments);
                for (int i = 0; i < segments; i++)
                {
                    float a = i / (float)segments * Mathf.PI * 2f - Mathf.PI * .5f;
                    GUI.color = i < lit ? CookingRingProgressColor(worker.Role) : new Color(.12f, .07f, .035f, .85f);
                    float radius = CookingRingRadius;
                    GUI.DrawTexture(new Rect(cx + Mathf.Cos(a) * radius - 1.5f,
                        cy + Mathf.Sin(a) * radius - CookingRingDotSize * .5f,
                        CookingRingDotSize, CookingRingDotSize), Texture2D.whiteTexture);
                }
            }
            GUI.color = old;
        }

        private const float LevelBadgeCrestSize = 32f;

        private void DrawLevelBadge(int levelIndex, Rect bounds)
        {
            if (VisualLevelIndex == RiverVisualLevelIndex) levelIndex = RiverVisualLevelIndex;
            string caption = StreetSimulation.LevelDisplayNames[levelIndex].ToUpperInvariant();
            Texture2D crest = LevelCrestForLevel(levelIndex);
            bool showCrest = crest != null;
            int originalSize = levelTitle.fontSize;
            float crestWidth = showCrest
                ? LevelCrestSizedSlot(new Rect(0, 0, LevelBadgeCrestSize, LevelBadgeCrestSize), crest).width : 0f;
            float available = bounds.width - (showCrest ? crestWidth + 8f : 0f);
            float measured = levelTitle.CalcSize(new GUIContent(caption)).x;
            if (measured + 8f > available)
                levelTitle.fontSize = Mathf.Max(12, Mathf.FloorToInt(originalSize * (available - 8f) / measured));
            Rect[] layout = LevelBadgeLayoutForWidth(bounds, levelTitle.CalcSize(new GUIContent(caption)).x + 8f, crestWidth);
            if (showCrest) DrawLevelCrest(LayoutRect(layout[0]), crest);
            levelTitle.normal.textColor = new Color(.005f, .035f, .16f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(layout[1].x + x * 1.2f, layout[1].y + y * 1.2f, layout[1].width, layout[1].height), caption, levelTitle);
            levelTitle.normal.textColor = Color.white;
            Label(layout[1], caption, levelTitle);
            levelTitle.fontSize = originalSize;
        }
        private static Rect[] LevelBadgeLayout(Rect bounds, float preferredTextWidth, bool includeCrest)
        {
            return LevelBadgeLayoutForWidth(bounds, preferredTextWidth, includeCrest ? LevelBadgeCrestSize : 0f);
        }
        private static Rect[] LevelBadgeLayoutForWidth(Rect bounds, float preferredTextWidth, float crestWidth)
        {
            float iconHeight = crestWidth > 0f ? LevelBadgeCrestSize : 0f;
            float gap = crestWidth > 0f ? 8f : 0f;
            float textWidth = Mathf.Clamp(preferredTextWidth, 0f, bounds.width - crestWidth - gap);
            float start = bounds.center.x - (crestWidth + gap + textWidth) * .5f;
            return new[] {
                new Rect(start, bounds.center.y - iconHeight * .5f, crestWidth, iconHeight),
                new Rect(start + crestWidth + gap, bounds.y, textWidth, bounds.height)
            };
        }
        private static Rect TopHudCutoutBounds(Vector2 screenSize, Rect[] cutouts)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || cutouts == null || cutouts.Length == 0) return Rect.zero;
            float scale = screenSize.x / W;
            float xMin = W, xMax = 0f;
            bool found = false;
            for (int i = 0; i < cutouts.Length; i++)
            {
                Rect cutout = cutouts[i];
                if (cutout.width <= 0f || cutout.height <= 0f || cutout.yMax < screenSize.y - 1f) continue;
                float top = screenSize.y - cutout.yMax;
                if (top > CutoutHudGap.height * scale) continue;
                xMin = Mathf.Min(xMin, cutout.xMin / scale - 8f);
                xMax = Mathf.Max(xMax, cutout.xMax / scale + 8f);
                found = true;
            }
            if (!found) return Rect.zero;
            xMin = Mathf.Clamp(xMin, 0f, W);
            xMax = Mathf.Clamp(xMax, 0f, W);
            return xMax > xMin ? new Rect(xMin, 0f, xMax - xMin, CutoutHudGap.height) : Rect.zero;
        }

        private void EnsureHudBarTexture(Rect gap)
        {
            if (gap.width <= 0f || gap.height <= 0f)
            {
                if (hudBarTexture != null) return;
                hudBarTexture = CreateHudBarTexture(Rect.zero, false);
                return;
            }
            if (cachedHudCutoutGap == gap && cutoutHudBarTexture != null) return;
            if (cutoutHudBarTexture != null) Destroy(cutoutHudBarTexture);
            cutoutHudBarTexture = CreateHudBarTexture(gap, false);
            cachedHudCutoutGap = gap;
        }

        private void DrawCounters(StreetSimulation sim)
        {
            // Read-only status is pinned to physical y=0; only interactive controls use Screen.safeArea.
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float previousVertical = layoutVerticalScale;
            try
            {
                layoutVerticalScale = 1f;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                Rect cutoutGap = TopHudCutoutBounds(screenSize, Screen.cutouts);
                EnsureHudBarTexture(cutoutGap);
                hudQuotaProductCount = Mathf.Min(sim.ProductCount, hudQuotaProductIds.Length);
                for (int slot = 0; slot < hudQuotaProductCount; slot++)
                    hudQuotaProductIds[slot] = sim.GetAvailableProduct(slot);
                GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.width / W, 1f));
                GUI.color = Color.white;
                Texture2D bar = cutoutGap.width > 0f ? cutoutHudBarTexture : hudBarTexture;
                Rect barBounds = new Rect(0f, 0f, W, HudBarHeightFor(cutoutGap));
                if (bar != null) GUI.DrawTexture(barBounds, bar, ScaleMode.StretchToFill, true);
                Rect coinCell = HudStatusCellBounds(0, cutoutGap);
                float statusScale = HudStatusScale(cutoutGap);
                float coinIcon = Mathf.Min(HudStatusIconSize * statusScale, coinCell.height - HudChipInset * 2f);
                float coinInset = HudStatusCardPadding * statusScale;
                DrawHudIcon(new Rect(coinCell.x + coinInset, coinCell.center.y - coinIcon * .5f, coinIcon, coinIcon), 10);
                DrawHudNumber(HudStatusTextBounds(0, cutoutGap), sim.Coins.ToString(),
                    Mathf.RoundToInt(HudStatusValueFontSize * statusScale));
                DrawHudNumber(HudStatusTextBounds(1, cutoutGap), FormatRemainingTime(sim.TimeRemaining),
                    Mathf.RoundToInt(HudClockFontSize * statusScale));
                for (int slot = 0; slot < hudQuotaProductCount; slot++)
                    DrawQuotaChip(sim, slot, HudQuotaCellBounds(slot, hudQuotaProductIds, hudQuotaProductCount, cutoutGap));
            }
            finally { GUI.matrix = previous; GUI.color = color; layoutVerticalScale = previousVertical; }
        }

        private static Rect HudStatusCellBounds(int slot, Rect cutoutGap)
        {
            if (slot < 0 || slot > 1) return Rect.zero;
            float width = (W - HudStatusCardPadding * 2f - HudStatusCardSpacing) * .5f;
            float x = slot == 0 ? HudStatusCardPadding : W - HudStatusCardPadding - width;
            float y = HudTopPadding;
            if (cutoutGap.width > 0f)
            {
                float leftWidth = cutoutGap.xMin - HudStatusCardPadding - HudCutoutClearance;
                float rightWidth = W - HudStatusCardPadding - cutoutGap.xMax - HudCutoutClearance;
                float safeWidth = Mathf.Min(width, Mathf.Min(leftWidth, rightWidth));
                // Small centered camera channels shrink both cards equally. For an unusually
                // large/off-center island, preserve readable cards by placing the row below it.
                if (safeWidth >= width * .55f)
                    width = Mathf.Max(0f, safeWidth);
                else
                    y = cutoutGap.yMax + HudCutoutClearance;
                x = slot == 0 ? HudStatusCardPadding : W - HudStatusCardPadding - width;
            }
            return new Rect(x, y, width, HudStateRowHeight);
        }

        private static float HudStatusScale(Rect cutoutGap)
        {
            float defaultWidth = HudStatusCellBounds(0, Rect.zero).width;
            if (defaultWidth <= 0f) return 1f;
            return Mathf.Clamp01(HudStatusCellBounds(0, cutoutGap).width / defaultWidth);
        }

        private static Rect HudCoinIconBounds(Rect cutoutGap)
        {
            Rect cell = HudStatusCellBounds(0, cutoutGap);
            float scale = HudStatusScale(cutoutGap);
            float size = Mathf.Min(HudStatusIconSize * scale, cell.height - HudChipInset * 2f);
            float inset = HudStatusCardPadding * scale;
            return new Rect(cell.x + inset, cell.center.y - size * .5f, size, size);
        }

        private static Rect HudStatusTextBounds(int slot, Rect cutoutGap)
        {
            if (slot < 0 || slot > 1) return Rect.zero;
            Rect cell = HudStatusCellBounds(slot, cutoutGap);
            float scale = HudStatusScale(cutoutGap);
            float iconSize = Mathf.Min(HudStatusIconSize * scale, cell.height - HudChipInset * 2f);
            float inset = HudStatusCardPadding * scale;
            float contentX = cell.x + inset + iconSize + HudStatusTextGap * scale;
            float contentRight = cell.xMax - inset;
            return new Rect(contentX, cell.y, Mathf.Max(0f, contentRight - contentX), cell.height);
        }

        private static readonly int[] DefaultHudProductIds = { 0, 1, 2, 3, 4, 6, 5 };
        private const float HudQuotaColumnInset = 8f;

        private static int HudQuotaColumnForProduct(int productId) =>
            productId >= 0 && productId < HudQuotaSolidProductCount ? 0 : 1;

        private static int HudQuotaCountInColumn(int[] productIds, int productCount, int column)
        {
            int count = 0;
            for (int i = 0; i < productCount; i++)
                if (HudQuotaColumnForProduct(productIds[i]) == column) count++;
            return count;
        }

        private static Rect HudQuotaGridBounds(Rect cutoutGap, int[] productIds, int productCount)
        {
            int solidRows = HudQuotaCountInColumn(productIds, productCount, 0);
            int liquidRows = HudQuotaCountInColumn(productIds, productCount, 1);
            int rows = Mathf.Max(solidRows, liquidRows);
            float height = rows <= 0 ? 0f : rows * HudQuotaCardHeight + (rows - 1) * HudObjectiveGap;
            return new Rect(HudQuotaColumnInset, HudBarHeightFor(cutoutGap) + HudObjectiveGap,
                W - HudQuotaColumnInset * 2f, height);
        }

        private static Rect HudQuotaGridBounds(Rect cutoutGap) =>
            HudQuotaGridBounds(cutoutGap, DefaultHudProductIds, DefaultHudProductIds.Length);

        private static Rect HudQuotaGridBounds(Rect cutoutGap, int productCount) =>
            HudQuotaGridBounds(cutoutGap, DefaultHudProductIds, Mathf.Clamp(productCount, 0, DefaultHudProductIds.Length));

        private static float HudBarHeightFor(Rect cutoutGap)
        {
            float statusBottom = Mathf.Max(HudStatusCellBounds(0, cutoutGap).yMax,
                HudStatusCellBounds(1, cutoutGap).yMax);
            return statusBottom + HudBottomPadding;
        }

        private static Rect HudQuotaCellBounds(int slot, int[] productIds, int productCount, Rect cutoutGap)
        {
            if (productIds == null || slot < 0 || slot >= productCount || slot >= productIds.Length) return Rect.zero;
            int productId = productIds[slot];
            int column = HudQuotaColumnForProduct(productId);
            int row = 0;
            for (int i = 0; i < slot; i++)
                if (HudQuotaColumnForProduct(productIds[i]) == column) row++;
            float availableWidth = W - HudQuotaColumnInset * 2f;
            float width = Mathf.Min(HudQuotaCardWidth, availableWidth * .5f);
            float x = column == 0 ? HudQuotaColumnInset : W - HudQuotaColumnInset - width;
            float y = HudBarHeightFor(cutoutGap) + HudObjectiveGap + row * (HudQuotaCardHeight + HudObjectiveGap);
            return new Rect(x, y, width, HudQuotaCardHeight);
        }

        private static Rect HudQuotaCellBounds(int slot, Rect cutoutGap) =>
            HudQuotaCellBounds(slot, DefaultHudProductIds, DefaultHudProductIds.Length, cutoutGap);

        private static Rect HudQuotaCellBounds(int slot) => HudQuotaCellBounds(slot, Rect.zero);

        private static Rect HudQuotaCellBounds(int slot, int productCount, Rect cutoutGap) =>
            HudQuotaCellBounds(slot, DefaultHudProductIds, Mathf.Clamp(productCount, 0, DefaultHudProductIds.Length), cutoutGap);

        private static Color HudQuotaCardColor(bool complete) =>
            complete ? new Color(.025f, .34f, .40f, .65f) : new Color(.025f, .09f, .24f, .65f);

        private void DrawQuotaChip(StreetSimulation sim, int slot, Rect bounds)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            int product = sim.GetAvailableProduct(slot);
            int goal = sim.GetProductGoal(product);
            int delivered = sim.GetProductDelivered(product);
            bool complete = sim.IsProductGoalComplete(product);
            Color previous = GUI.color;
            if (hudQuotaCardTexture != null)
            {
                GUI.color = HudQuotaCardColor(complete);
                GUI.DrawTexture(bounds, hudQuotaCardTexture, ScaleMode.StretchToFill, true);
                if (hudQuotaCardOutlineTexture != null)
                {
                    GUI.color = new Color(.45f, .88f, 1f, .65f);
                    GUI.DrawTexture(bounds, hudQuotaCardOutlineTexture, ScaleMode.StretchToFill, true);
                }
            }
            else FillRect(bounds, HudQuotaCardColor(complete));
            GUI.color = previous;
            float iconSize = Mathf.Min(HudQuotaIconMaxSize, bounds.height - HudQuotaCardInset * 2f);
            Rect icon = new Rect(bounds.x + HudQuotaCardInset, bounds.center.y - iconSize * .5f, iconSize, iconSize);
            DrawHudIcon(icon, product);
            Rect counter = HudQuotaCounterBounds(bounds, icon);
            int fontSize = HudQuotaCounterFontSize;
            DrawHudNumber(counter, delivered + "/" + goal, fontSize, HudQuotaCounterTextAlignment);
        }
        private static Rect HudQuotaCounterBounds(Rect card, Rect icon)
        {
            float x = icon.xMax + HudQuotaCardInset;
            return new Rect(x, card.y + HudChipTextTopInset * HudQuotaCardEnlargement,
                Mathf.Max(0f, card.xMax - HudQuotaCounterRightInset - x),
                Mathf.Max(0f, card.height - HudChipTextHeightInset * HudQuotaCardEnlargement));
        }
        private void DrawHudIcon(Rect slot, int itemId)
        {
            Texture2D texture = itemId == 10 && gameCoin != null ? gameCoin
                : itemId == 4 && cocaBottle != null ? cocaBottle : items;
            if (texture == null) return;
            Rect source = texture == items ? Items[itemId] : new Rect(0, 0, texture.width, texture.height);
            Draw(HudIconBounds(slot, source.size), texture, source, true, false);
        }
        private static Rect HudIconBounds(Rect slot, Vector2 sourceSize)
        {
            float scale = Mathf.Min(slot.width / sourceSize.x, slot.height / sourceSize.y);
            Vector2 size = sourceSize * scale;
            return new Rect(slot.center - size * .5f, size);
        }
        private static string FormatRemainingTime(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
        private void DrawHudNumber(Rect bounds, string value, int preferredFontSize = 40,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            int originalSize = hudNumber.fontSize;
            TextAnchor originalAlignment = hudNumber.alignment;
            Color originalTextColor = hudNumber.normal.textColor;
            hudNumber.alignment = alignment;
            int baseFontSize = Mathf.Max(12, preferredFontSize);
            hudNumber.fontSize = baseFontSize;
            Vector2 needed = hudNumber.CalcSize(new GUIContent(value));
            float fitWidth = (bounds.width - 4f) / Mathf.Max(1f, needed.x);
            float fitHeight = (bounds.height - 2f) / Mathf.Max(1f, needed.y);
            float fitScale = Mathf.Min(1f, fitWidth, fitHeight);
            if (fitScale < 1f)
                hudNumber.fontSize = Mathf.Max(12, Mathf.FloorToInt(baseFontSize * fitScale));
            hudNumber.normal.textColor = new Color(.005f, .025f, .11f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x, bounds.y + y, bounds.width, bounds.height), value, hudNumber);
            hudNumber.normal.textColor = Color.white;
            Label(bounds, value, hudNumber);
            hudNumber.fontSize = originalSize;
            hudNumber.alignment = originalAlignment;
            hudNumber.normal.textColor = originalTextColor;
        }
        private static Texture2D CreateHudBar() => CreateHudBarTexture(false);
        private static Texture2D CreateHudBarTexture(bool cutout)
            => CreateHudBarTexture(cutout ? CutoutHudGap : Rect.zero, false);

        private static Texture2D CreateHudBarTexture(Rect reservedHudGap, bool keepReadable)
        {
            // Original code-authored artwork: no reference pixels or baked-in live numbers.
            const int width = 1080;
            int height = Mathf.CeilToInt(HudBarHeightFor(reservedHudGap) * 2f);
            float logicalHeight = height * .5f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado blue gameplay HUD",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var shadow = new Rect(0, 5, W, logicalHeight - 5f);
            var rim = new Rect(0, 0, W, logicalHeight);
            var face = new Rect(2, 2, W - 4f, logicalHeight - 4f);
            Rect[] stateCells = {
                HudStatusCellBounds(0, reservedHudGap),
                HudStatusCellBounds(1, reservedHudGap)
            };
            Color[] capsuleTops = { new Color(.30f, .86f, 1f), new Color(.30f, .86f, 1f) };
            Color[] capsuleBottoms = { new Color(.02f, .22f, .72f), new Color(.015f, .20f, .66f) };
            Rect clockCell = stateCells[1];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) / 2f, py = (height - y - .5f) / 2f;
                Color color = new Color(.008f, .025f, .12f, .58f * RoundedCoverage(px, py, shadow, 22));
                color = Over(color, new Color(.012f, .045f, .17f), RoundedCoverage(px, py, rim, 22));
                color = Over(color, Color.Lerp(new Color(.10f, .47f, .90f), new Color(.025f, .14f, .48f),
                    Mathf.Clamp01(py / Mathf.Max(1f, logicalHeight))), RoundedCoverage(px, py, face, 20));
                for (int i = 0; i < stateCells.Length; i++)
                    color = HudCapsule(color, px, py, stateCells[i], capsuleTops[i], capsuleBottoms[i]);
                // Reuse the original vector clock motif in the centered time capsule.
                float clockScale = Mathf.Min(1f, clockCell.height / 28f, HudStatusScale(reservedHudGap));
                float dx = (px - (clockCell.x + 17f)) / clockScale;
                float dy = (py - clockCell.center.y) / clockScale;
                float radius = new Vector2(dx, dy).magnitude;
                color = Over(color, new Color(.01f, .045f, .17f), Mathf.Clamp01(10.5f - radius));
                color = Over(color, new Color(.42f, .86f, 1f), Mathf.Clamp01(9.3f - radius));
                color = Over(color, new Color(.92f, .99f, 1f), Mathf.Clamp01(7.8f - radius));
                bool minuteHand = Mathf.Abs(dx) < .8f && dy >= -5.5f && dy <= .8f;
                bool hourHand = dx >= -.5f && dx <= 4.8f && Mathf.Abs(dy - dx * .5f) < .9f;
                if (minuteHand || hourHand) color = new Color(.015f, .055f, .18f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private static Texture2D CreateHudQuotaCardTexture(bool outline)
        {
            const int width = 192, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = outline ? "Translucent objective card rim" : "Translucent objective card face",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect outer = new Rect(1f, 1f, width - 2f, height - 2f);
            Rect inner = new Rect(3f, 3f, width - 6f, height - 6f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float outerCoverage = RoundedCoverage(px, py, outer, 12f);
                float innerCoverage = RoundedCoverage(px, py, inner, 9f);
                float coverage = outline ? Mathf.Max(0f, outerCoverage - innerCoverage) : outerCoverage;
                // Coverage supplies only the rounded silhouette; GUI.color applies the requested 65% opacity.
                pixels[y * width + x] = new Color(1f, 1f, 1f, coverage);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateGameplayFooterTexture()
        {
            const int width = 256, height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado blue gameplay footer",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float vertical = (height - y - .5f) / height;
                float horizontal = Mathf.Sin(x / (float)(width - 1) * Mathf.PI * 2f) * .012f;
                Color color = Color.Lerp(new Color(.012f, .075f, .23f), new Color(.025f, .29f, .62f), vertical);
                color.r = Mathf.Clamp01(color.r + horizontal);
                float upperGloss = .22f * (1f - IntroEase((vertical - .72f) / .25f));
                color = Color.Lerp(color, new Color(.36f, .85f, 1f), upperGloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Color HudCapsule(Color below, float x, float y, Rect bounds, Color top, Color bottom)
        {
            float radius = Mathf.Min(15f, bounds.height * .5f);
            Color color = Over(below, new Color(.008f, .04f, .16f), RoundedCoverage(x, y, bounds, radius));
            var inside = new Rect(bounds.x + 1.5f, bounds.y + 1.5f, bounds.width - 3, bounds.height - 3);
            float coverage = RoundedCoverage(x, y, inside, Mathf.Max(1f, radius - 1.5f));
            float t = Mathf.Clamp01((y - inside.y) / inside.height);
            color = Over(color, Color.Lerp(top, bottom, t), coverage);
            return Over(color, Color.white, coverage * .28f * (1f - IntroEase(t / .5f)));
        }
        private static Rect IntroScreenRect(Vector2 size)
        {
            return new Rect(0, 0, Mathf.Max(0, size.x), Mathf.Max(0, size.y));
        }
        private static Rect IntroBackdropBounds(Vector2 screenSize, float scale)
        {
            float width = Mathf.Max(0f, screenSize.x * scale);
            float height = Mathf.Max(0f, screenSize.y * scale);
            return new Rect((screenSize.x - width) * .5f, (screenSize.y - height) * .5f, width, height);
        }
        private static void DrawIntroScreen(Texture2D texture, Color tint, ScaleMode scaleMode)
        {
            DrawIntroScreen(texture, tint, scaleMode, IntroScreenRect(new Vector2(Screen.width, Screen.height)));
        }
        private static void DrawIntroScreen(Texture2D texture, Color tint, ScaleMode scaleMode, Rect screenBounds)
        {
            if (texture == null) return;
            // Decorative art fills physical pixels; foreground/input keep the safe-area canvas.
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.color = tint;
                GUI.DrawTexture(screenBounds, texture, scaleMode, true);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static Rect IntroProgressBounds() => new Rect((W - 95f) * .5f, 882f, 95f, 28f);
        private static float IntroProgressValue(float elapsed) => Mathf.Clamp01(elapsed / IntroDuration);
        // GUIStyle.fontSize is an integer: 19 × 0.75 = 14.25, rounded to 14.
        private const int IntroStatusFontSize = 14;
        private GUIStyle IntroStatusStyle()
        {
            if (introStatusStyle != null) return introStatusStyle;
            introStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = IntroStatusFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0)
            };
            Font sans = Resources.Load<Font>("Google/GoogleSans-Medium");
            if (sans != null) introStatusStyle.font = sans;
            introStatusStyle.normal.textColor = Color.white;
            return introStatusStyle;
        }
        private static float IntroLogoPop(float t)
        {
            if (t < .10f) return 0f;
            if (t < .22f) return Mathf.Lerp(.55f, 1.22f, Mathf.SmoothStep(0f, 1f, (t - .10f) / .12f));
            if (t < .40f) return Mathf.Lerp(1.22f, .97f, Mathf.SmoothStep(0f, 1f, (t - .22f) / .18f));
            return Mathf.Lerp(.97f, 1f, Mathf.SmoothStep(0f, 1f, (t - .40f) / .13f));
        }
        private static string IntroStatus(float elapsed) =>
            elapsed < 1.35f ? "PRENDIENDO LAS BRASAS..." :
            elapsed < 2.7f ? "LA HINCHADA ESTÁ LLEGANDO..." : "¡QUE EMPIECE LA FIESTA EN EL BARRIO!";
        private static Rect IntroLogoBounds(float scale)
        {
            float width = 270f * scale, height = 260f * scale;
            return new Rect((W - width) * .5f, 450f - height * .5f, width, height);
        }
        private void PrepareIntroLogoShines()
        {
            if (introLogoShines != null || titleLogo == null) return;
            const int size = 256, frames = 24;
            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Texture2D alphaCopy = null;
            Color32[] source;
            try
            {
                Graphics.Blit(titleLogo, rt);
                RenderTexture.active = rt;
                alphaCopy = new Texture2D(size, size, TextureFormat.RGBA32, false);
                alphaCopy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                alphaCopy.Apply(false, false);
                source = alphaCopy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                if (alphaCopy != null) Destroy(alphaCopy);
            }
            introLogoShines = new Texture2D[frames];
            for (int k = 0; k < frames; k++)
            {
                Color[] pixels = new Color[size * size];
                float center = Mathf.Lerp(-.25f, 1.5f, k / (float)(frames - 1));
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs((x / (float)size + y / (float)size * .25f) - center);
                    float shine = Mathf.Pow(Mathf.Clamp01(1f - d / .13f), 1.4f) * .62f;
                    pixels[y * size + x] = new Color(1f, 1f, .92f, shine * source[y * size + x].a / 255f);
                }
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Startup logo shine " + k,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                introLogoShines[k] = texture;
            }
        }
        private void DrawIntroLogo(float elapsed)
        {
            float scale = IntroLogoPop(elapsed);
            if (scale <= 0f || titleLogo == null) return;
            Rect bounds = IntroLogoBounds(scale);
            GUI.DrawTexture(LayoutRect(bounds), titleLogo, ScaleMode.ScaleToFit, true);
            PrepareIntroLogoShines();
            float sweep = elapsed >= 2.65f ? elapsed - 2.65f : elapsed - 1.05f;
            if (sweep >= 0f && sweep < .65f && introLogoShines != null)
            {
                int frame = Mathf.Clamp(Mathf.FloorToInt(sweep / .65f * introLogoShines.Length), 0, introLogoShines.Length - 1);
                GUI.DrawTexture(LayoutRect(bounds), introLogoShines[frame], ScaleMode.ScaleToFit, true);
            }
        }
        private void DrawIntroProgress(float elapsed)
        {
            const int width = 95, height = 28;
            if (introPreviewBar == null)
                introPreviewBar = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Startup cyan animated capsule",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
            int key = Mathf.FloorToInt(elapsed * 30f);
            if (key != introPreviewBarKey)
            {
                introPreviewBarKey = key;
                Color[] pixels = new Color[width * height];
                float progress = Mathf.Clamp01(elapsed / 3.7f);
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float px = x + .5f, py = y + .5f;
                    Color color = Color.clear;
                    float coverage = RoundedCoverage(px, py, new Rect(0, 2, 95, 26), 13);
                    color = Color.Lerp(color, new Color(.09f, .025f, .15f, .85f), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(2, 0, 91, 25), 12.5f);
                    color = Color.Lerp(color, new Color(.11f, .11f, .27f, 1), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(4, 2, 87, 21), 10.5f);
                    color = Color.Lerp(color, new Color(.94f, .99f, 1f, 1), coverage);
                    coverage = RoundedCoverage(px, py, new Rect(7, 5, 81, 15), 7.5f);
                    color = Color.Lerp(color, new Color(.025f, .33f, .50f, 1), coverage);
                    if (px < 7 + 81 * progress)
                    {
                        float stripe = ((px + py * .65f - key * 1.2f) % 23f + 23f) % 23f;
                        Color fill = Color.Lerp(new Color(.04f, .63f, .89f, 1), new Color(.18f, .89f, 1f, 1), 1f - py / 28f);
                        if (stripe < 7f) fill = Color.Lerp(fill, new Color(.6f, .96f, 1f, 1), .3f);
                        color = Color.Lerp(color, fill, coverage);
                    }
                    pixels[(height - 1 - y) * width + x] = color;
                }
                introPreviewBar.SetPixels(pixels);
                introPreviewBar.Apply(false, false);
            }
            GUI.DrawTexture(LayoutRect(IntroProgressBounds()), introPreviewBar);
        }
        private void DrawIntro(float elapsed)
        {
            DrawIntroScreen(coverArt, Color.white, ScaleMode.ScaleAndCrop);
            DrawIntroLogo(elapsed);
            GUIStyle status = IntroStatusStyle();
            string message = IntroStatus(elapsed);
            Rect letters = new Rect(8f, 851f, W - 16f, 30f);
            status.normal.textColor = Color.black;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(letters.x + x * 1.5f, letters.y + y * 1.5f, letters.width, letters.height), message, status);
            status.normal.textColor = Color.white;
            Label(letters, message, status);
            DrawIntroProgress(elapsed);
            GUI.color = Color.white;
        }
        private void DrawHubScreen()
        {
            DrawIntroScreen(hubBackdrop != null ? hubBackdrop : coverArt, Color.white, ScaleMode.ScaleAndCrop);
            DrawHubHotspot(HubAccountAction);
            DrawHubHotspot(HubRankingAction);
            DrawHubHotspot(HubBadgesAction);
            DrawHubHotspot(HubMarketAction);
            DrawHubHotspot(HubPlayAction);
        }
        private void DrawHubHotspot(int action)
        {
            Rect bounds = HubArtworkActionBounds(action);
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            if (GUI.Button(LayoutRect(bounds), "", invisible)) NativeAction(action);
        }
        private static Rect HubArtActionSourceRect(int action)
        {
            switch (action)
            {
                case HubAccountAction: return new Rect(12, 5, 230, 160);
                case HubRankingAction: return new Rect(523, 150, 322, 160);
                case HubPlayAction: return new Rect(112, 610, 352, 174);
                case HubMarketAction: return new Rect(105, 1034, 355, 183);
                case HubBadgesAction: return new Rect(535, 1032, 385, 188);
                default: return Rect.zero;
            }
        }
        private Rect HubArtworkActionBounds(int action) => HubArtworkRectToLayout(
            HubArtActionSourceRect(action), HubArtworkSize, new Vector2(Screen.width, Screen.height), Screen.safeArea);
        private static Rect HubArtworkRectToLayout(Rect artRect, Vector2 artSize, Vector2 screenSize, Rect safeArea)
        {
            if (artSize.x <= 0f || artSize.y <= 0f || screenSize.x <= 0f || screenSize.y <= 0f) return Rect.zero;
            float artScale = Mathf.Max(screenSize.x / artSize.x, screenSize.y / artSize.y);
            float drawnWidth = artSize.x * artScale, drawnHeight = artSize.y * artScale;
            Rect screenRect = new Rect((screenSize.x - drawnWidth) * .5f + artRect.x * artScale,
                (screenSize.y - drawnHeight) * .5f + artRect.y * artScale,
                artRect.width * artScale, artRect.height * artScale);
            Rect viewport = CanvasViewport(screenSize, safeArea);
            if (viewport.width <= 0f || viewport.height <= 0f) return Rect.zero;
            float scaleX = viewport.width / W, scaleY = viewport.height / H;
            float xMin = Mathf.Clamp((screenRect.xMin - viewport.xMin) / scaleX, 0f, W);
            float xMax = Mathf.Clamp((screenRect.xMax - viewport.xMin) / scaleX, 0f, W);
            float yMin = Mathf.Clamp((screenRect.yMin - viewport.yMin) / scaleY, 0f, H);
            float yMax = Mathf.Clamp((screenRect.yMax - viewport.yMin) / scaleY, 0f, H);
            return xMax <= xMin || yMax <= yMin ? Rect.zero : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        private static Rect BadgeBackBounds(float height) => new Rect(110, height - 80, 320, 60);
        private static Rect RankingBackBounds(float height) => new Rect(110, height - 80, 320, 60);
        private static Rect SalesBadgeRowBounds(int product, float height)
        {
            float rowHeight = Mathf.Min(108f, (height - 280f) / StreetSalesBadges.ProductCount - 6f);
            return new Rect(20, 169f + product * (rowHeight + 6f), W - 40, rowHeight);
        }
        private void DrawSalesMedal(Rect bounds, long sales, int tier, bool earned)
        {
            long target = StreetSalesBadges.Threshold(tier);
            Color metal = tier == 0 ? new Color(.84f, .49f, .24f) : tier == 1
                ? new Color(.75f, .82f, .88f) : new Color(1f, .77f, .18f);
            FillRect(bounds, earned ? metal : new Color(.66f, .64f, .59f));
            Rect face = new Rect(bounds.x + 2, bounds.y + 2, bounds.width - 4, bounds.height - 4);
            FillRect(face, earned ? new Color(.98f, .92f, .72f) : new Color(.85f, .83f, .77f));
            string name = tier == 0 ? "BRONCE" : tier == 1 ? "PLATA" : "ORO";
            MarketText(new Rect(face.x, face.y + 1, face.width, 18), name, 13, MarketInk);
            MarketText(new Rect(face.x, face.y + 20, face.width, 20), target.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")), 18, MarketInk);
            float trackY = face.yMax - 25;
            Rect track = new Rect(face.x + 6, trackY, face.width - 12, 4);
            FillRect(track, new Color(.36f, .31f, .24f));
            float progress = earned ? 1f : (float)System.Math.Min(1d, System.Math.Max(0d, (double)sales / target));
            FillRect(new Rect(track.x, track.y, track.width * progress, track.height), earned ? new Color(.07f, .48f, .16f) : metal);
            MarketText(new Rect(face.x, face.yMax - 20, face.width, 18), earned ? "CONSEGUIDA" : "EN PROGRESO", 12,
                earned ? new Color(.03f, .29f, .08f) : MarketInk);
        }
        private void DrawBadgesScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .68f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            DrawMarketWoodPanel(new Rect(14, 14, W - 28, height - 28), new Color(.11f, .065f, .036f, .96f));
            MarketText(new Rect(28, 28, W - 56, 40), "INSIGNIAS DE LA PARRILLA", 29, new Color(1f, .84f, .38f));
            MarketText(new Rect(28, 69, W - 56, 24), "Vendé 500, 2.500 y 5.000 de cada producto", 17, Color.white);
            int earned = 0;
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    if (game.HasProductSalesBadge(product, tier)) earned++;
            MarketText(new Rect(28, 97, W - 56, 24), earned + " / " + StreetSalesBadges.BadgeCount + " INSIGNIAS CONSEGUIDAS", 17, new Color(.52f, 1f, .47f));
            string sync = Cloud.FirebasePlayerSession.Instance != null
                ? Cloud.FirebasePlayerSession.Instance.ProgressSyncStatus : "Progreso local de prueba";
            MarketText(new Rect(28, 126, W - 56, 22), sync, 14, Color.white);
            string[] names = { "CHORI", "PATY", "BONDIOLA", "VACÍO", "COCA", "FERNET", "CERVEZA" };
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
            {
                long sales = game.GetCumulativeProductSales(product);
                Rect row = SalesBadgeRowBounds(product, height);
                DrawMarketWoodPanel(row, new Color(.93f, .86f, .68f));
                DrawProductIcon(new Rect(row.x + 10, row.y + 8, 46, row.height - 16), product);
                MarketText(new Rect(row.x + 58, row.y + 12, 120, 22), names[product], 17, MarketInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(row.x + 58, row.y + 37, 120, 20), sales.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")) + " vendidas", 13, MarketInk, TextAnchor.MiddleLeft);
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    DrawSalesMedal(new Rect(row.x + 185 + tier * 100, row.y + 7, 94, row.height - 14), sales, tier, game.HasProductSalesBadge(product, tier));
            }
            string coverage = game.HasIncompleteSalesHistory ? "Historial antiguo parcial: contamos las ventas registradas."
                : "Ventas acumuladas al cerrar cada resultado.";
            MarketText(new Rect(28, height - 109, W - 56, 22), coverage, 13, Color.white);
            DrawStandardButton(BadgeBackBounds(height), BadgeBackAction, "VOLVER AL BARRIO");
        }
        private void DrawRankingScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .72f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            float panelHeight = Mathf.Min(520f, height - 150f);
            Rect panel = new Rect(22, (height - panelHeight) * .5f, W - 44, panelHeight);
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .97f));
            MarketText(new Rect(panel.x + 18, panel.y + 20, panel.width - 36, 48), "RANKING DE LA HINCHADA", 30, new Color(1f, .84f, .38f));
            Rect trophy = new Rect(W * .5f - 32, panel.y + 84, 64, 64);
            FillRect(new Rect(trophy.x - 5, trophy.y - 5, trophy.width + 10, trophy.height + 10), new Color(.70f, .42f, .08f));
            FillRect(trophy, new Color(.98f, .76f, .19f));
            MarketText(trophy, "★", 44, new Color(.24f, .11f, .025f));
            MarketText(new Rect(panel.x + 22, panel.y + 164, panel.width - 44, 58), "EL RANKING GLOBAL LLEGA PRONTO", 23, Color.white);
            MarketText(new Rect(panel.x + 28, panel.y + 218, panel.width - 56, 52), "Todavía no publicamos posiciones de otros hinchas.", 16, new Color(.95f, .87f, .72f));
            MarketText(new Rect(panel.x + 26, panel.y + 295, panel.width - 52, 30), "TU PROGRESO", 19, new Color(1f, .84f, .38f));
            MarketText(new Rect(panel.x + 26, panel.y + 331, panel.width - 52, 38), "CANCHAS DESBLOQUEADAS: " + (game.UnlockedLevel + 1) + " / " + StreetSimulation.LevelNames.Length, 19, Color.white);
            string next = game.HasWonLevel(StreetSimulation.LevelNames.Length - 1)
                ? "TODAS LAS CANCHAS SUPERADAS"
                : game.UnlockedLevel < StreetSimulation.LevelNames.Length
                    ? StreetSimulation.LevelDisplayNames[game.UnlockedLevel].Split('/')[0].Trim().ToUpperInvariant()
                    : "TODAS LAS CANCHAS";
            MarketText(new Rect(panel.x + 26, panel.y + 373, panel.width - 52, 48), "PRÓXIMO OBJETIVO: " + next, 16, new Color(.52f, 1f, .47f));
            DrawStandardButton(RankingBackBounds(height), RankingBackAction, "VOLVER AL BARRIO");
        }
        private void DrawLevelSelector()
        {
            Rect viewport = LevelSelectorViewportBounds(logicalCanvasHeight);
            DrawLevelSelectorBackdrop();
            DrawLevelSelectorHeading();
            DrawStandardButton(MarketSelectorBounds(), MarketCloseAction, "MERCADO");
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                Vector2 point = ScreenToLayoutPoint(evt.mousePosition, new Vector2(Screen.width, Screen.height),
                    CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea));
                point.y *= logicalCanvasHeight / H;
                if (viewport.Contains(point))
                {
                    levelSelectScroll = Mathf.Clamp(levelSelectScroll + evt.delta.y * 28f, 0f,
                        LevelSelectorMaxScroll(logicalCanvasHeight));
                    evt.Use();
                }
            }
            levelSelectScroll = Mathf.Clamp(levelSelectScroll, 0f, LevelSelectorMaxScroll(logicalCanvasHeight));
            GUI.BeginGroup(LayoutRect(viewport));
            try
            {
                for (int i = 0; i < StreetSimulation.LevelNames.Length; i++)
                {
                    Rect card = LevelSelectCardBounds(i);
                    if (card.yMax <= viewport.y + levelSelectScroll ||
                        card.y >= viewport.yMax + levelSelectScroll) continue;
                    card.x -= viewport.x;
                    card.y -= viewport.y + levelSelectScroll;
                    DrawLevelSelectCard(i, card);
                }
            }
            finally { GUI.EndGroup(); }
            DrawLevelSelectorScrollBar();
            DrawStandardButton(LevelSelectorBackBounds(logicalCanvasHeight), LevelSelectorBackAction, "VOLVER");
        }
        private static float LevelSelectorContentBottom(int levelCount)
        {
            int rows = Mathf.CeilToInt(Mathf.Max(0, levelCount) / 2f);
            return rows <= 0 ? 0f : 160f + rows * LevelSelectorCardHeight + (rows - 1) * LevelSelectorCardGap;
        }
        private static float LevelSelectorMaxScroll(float canvasHeight) =>
            Mathf.Max(0f, LevelSelectorContentBottom(StreetSimulation.LevelNames.Length) -
                LevelSelectorViewportBounds(canvasHeight).yMax);

        private void DrawLevelSelectorScrollBar()
        {
            float max = LevelSelectorMaxScroll(logicalCanvasHeight);
            if (max <= 0f) return;
            Rect viewport = LevelSelectorViewportBounds(logicalCanvasHeight);
            Rect track = new Rect(W - 13f, viewport.y, 5f, viewport.height);
            FillRect(track, new Color(.12f, .10f, .075f, .52f));
            float thumbHeight = Mathf.Max(30f, track.height * track.height / (track.height + max));
            FillRect(new Rect(track.x, track.y + (track.height - thumbHeight) * levelSelectScroll / max,
                track.width, thumbHeight), new Color(1f, .95f, .79f, .97f));
        }

        private static Rect MarketBackBounds() => new Rect(12, 10, 132, 50);
        private static Rect MarketSelectorBounds() => new Rect(W - 118f, 12f, 106f, 32f);
        private const float MarketObjectiveTop = 202f;
        private const float MarketCompactRowHeight = 104f, MarketPurchaseHeight = 64f, MarketOthersHeight = 48f;
        private static readonly Color MarketInk = new Color(.20f, .08f, .025f);
        private static readonly Color MarketBlueInk = new Color(.025f, .075f, .20f);
        private static readonly Color MarketIce = new Color(.91f, .97f, 1f);
        private static readonly Color MarketNavy = new Color(.008f, .035f, .14f);
        private static readonly Color MarketRoyalBlue = new Color(.025f, .24f, .70f);
        private static readonly Color MarketCyan = new Color(.22f, .82f, 1f);
        private static readonly Color MarketGold = new Color(1f, .80f, .24f);
        private static readonly Color MarketGreen = new Color(.04f, .40f, .19f);
        private static readonly Color MarketRed = new Color(.73f, .12f, .10f);
        private Rect MarketBodyBounds() => MarketBodyBounds(game.Sim.ProductCount, logicalCanvasHeight);
        private static Rect MarketBodyBounds(int productCount, float canvasHeight) =>
            new Rect(12, MarketBodyTop(productCount), W - 24, Mathf.Max(140f, canvasHeight - 184f - MarketBodyTop(productCount)));
        private static Rect MarketEconomyFooterBounds(float canvasHeight) => new Rect(12, canvasHeight - 154f, W - 24, 66f);
        private static Rect MarketBuyMissingBounds(float canvasHeight)
        {
            Rect footer = MarketEconomyFooterBounds(canvasHeight);
            return new Rect(footer.x + 270f, footer.y + 8f, footer.width - 282f, footer.height - 16f);
        }
        private static int MarketObjectiveRowCount(int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            return count == 0 ? 0 : count <= 3 ? 1 : 2;
        }
        private static int MarketObjectiveFirstRowCount(int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            return count <= 3 ? count : count == 4 ? 2 : count == 7 ? 4 : 3;
        }
        private static int MarketObjectiveItemsInRow(int productCount, int row)
        {
            int count = Mathf.Clamp(productCount, 0, 7), rows = MarketObjectiveRowCount(count);
            if (row < 0 || row >= rows) return 0;
            return rows == 1 ? count : row == 0 ? MarketObjectiveFirstRowCount(count) : count - MarketObjectiveFirstRowCount(count);
        }
        private static float MarketObjectiveCardHeight(int productCount) => productCount <= 3 ? 72f : 64f;
        private static float MarketObjectiveBandHeight(int productCount)
        {
            int rows = MarketObjectiveRowCount(productCount);
            return rows == 0 ? 0f : rows * MarketObjectiveCardHeight(productCount) + (rows - 1) * 4f;
        }
        private static Rect MarketObjectiveCardBounds(int slot, int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            if (slot < 0 || slot >= count) return Rect.zero;
            int first = MarketObjectiveFirstRowCount(count), row = count > 3 && slot >= first ? 1 : 0;
            int column = row == 0 ? slot : slot - first, itemsInRow = MarketObjectiveItemsInRow(count, row);
            const float gap = 6f;
            float width = itemsInRow == 1 ? 240f : (W - 52f - gap * (itemsInRow - 1)) / itemsInRow;
            float rowWidth = width * itemsInRow + gap * (itemsInRow - 1);
            return new Rect((W - rowWidth) * .5f + column * (width + gap),
                MarketObjectiveTop + row * (MarketObjectiveCardHeight(count) + 4f), width, MarketObjectiveCardHeight(count));
        }
        private static Rect MarketEconomyBounds(int productCount) =>
            new Rect(46, MarketObjectiveTop + MarketObjectiveBandHeight(productCount) + 8f, W - 92, 58f);
        private static float MarketBodyTop(int productCount) => MarketEconomyBounds(productCount).yMax + 10f;
        private int[] MarketIngredientIds(bool required)
        {
            // All Boys' opening market is intentionally limited to its two required products.
            if (!required && game.Sim.LevelIndex == 0) return new int[0];
            int[] need = game.Market.RequiredUnits(game.Sim);
            var ids = new System.Collections.Generic.List<int>();
            for (int i = 0; i < need.Length; i++)
                if (required ? need[i] > 0 : need[i] == 0 && game.Market.IsIngredientUnlocked(i, game.UnlockedLevel)) ids.Add(i);
            return ids.ToArray();
        }
        private int[] MarketVisibleIngredients()
        {
            var ids = new System.Collections.Generic.List<int>(MarketIngredientIds(true));
            if (marketOtherExpanded) ids.AddRange(MarketIngredientIds(false));
            return ids.ToArray();
        }
        private int MarketExpandedId()
        {
            if (marketExpandedIngredient == -2) return -1;
            foreach (int id in MarketVisibleIngredients()) if (id == marketExpandedIngredient && game.Market.IsIngredientUnlocked(id, game.UnlockedLevel)) return id;
            foreach (int id in MarketIngredientIds(true)) if (game.Market.MissingUnits(game.Sim, id) > 0) return id;
            return -1;
        }
        private float MarketRowHeight(int id) =>
            (game.Market.RequiredUnits(game.Sim)[id] > 0 ? MarketCompactRowHeight : 96f) + (id == MarketExpandedId() ? MarketPurchaseHeight : 0f);
        private float MarketRequiredContentHeight()
        {
            float height = 0f;
            foreach (int id in MarketIngredientIds(true)) height += MarketRowHeight(id);
            return height;
        }
        private float MarketContentHeight()
        {
            float height = 0f;
            foreach (int id in MarketVisibleIngredients()) height += MarketRowHeight(id);
            if (MarketIngredientIds(false).Length > 0) height += MarketOthersHeight;
            return height;
        }
        private float MarketMaxScroll() => Mathf.Max(0, MarketContentHeight() - MarketBodyBounds().height);
        private Rect MarketRowBounds(int slot)
        {
            int[] visible = MarketVisibleIngredients();
            if (slot < 0 || slot >= visible.Length) return Rect.zero;
            Rect body = MarketBodyBounds();
            float y = body.y - marketScroll;
            for (int i = 0; i < slot; i++) y += MarketRowHeight(visible[i]);
            if (slot >= MarketIngredientIds(true).Length) y += MarketOthersHeight;
            return new Rect(body.x, y, body.width, MarketRowHeight(visible[slot]));
        }
        private Rect MarketOthersBounds() => new Rect(12, MarketBodyBounds().y + MarketRequiredContentHeight() - marketScroll, W - 24, MarketOthersHeight);
        // These same rectangles drive native pointer input and IMGUI rendering.
        private static Rect MarketPurchaseControlBounds(Rect row, int action)
        {
            float y = row.yMax - MarketPurchaseHeight + 10f;
            switch (action)
            {
                case MarketMinusAction: return new Rect(row.x + 10, y, 40, 46);
                case MarketPlusAction: return new Rect(row.x + 98, y, 40, 46);
                case MarketPlusTenAction: return new Rect(row.x + 142, y, 56, 46);
                case MarketBuyAction: return new Rect(row.x + 202, y, 142, 46);
                default: return new Rect(row.x + 348, y, row.width - 358, 46);
            }
        }
        private static bool MarketPurchaseControlsVisible(Rect row, Rect viewport) =>
            row.yMax <= viewport.yMax && row.yMax - MarketPurchaseHeight >= viewport.y;
        private int HitMarketAction(Vector2 p)
        {
            if (MarketBackBounds().Contains(p)) return MarketBackAction;
            if (MarketBuyMissingBounds(logicalCanvasHeight).Contains(p)) return MarketCanBuyMissing() ? MarketMissingAction : 0;
            if (MarketPlayBounds().Contains(p)) return MarketPlayAction;
            Rect body = MarketBodyBounds();
            if (!body.Contains(p)) return 0;
            if (MarketIngredientIds(false).Length > 0 && MarketOthersBounds().Contains(p)) return MarketOtherAction;
            int[] visible = MarketVisibleIngredients();
            for (int slot = 0; slot < visible.Length; slot++)
            {
                int id = visible[slot]; Rect row = MarketRowBounds(slot);
                if (!row.Contains(p) || !game.Market.IsIngredientUnlocked(id, game.UnlockedLevel)) continue;
                if (id == MarketExpandedId() && MarketPurchaseControlsVisible(row, body))
                    foreach (int action in new[] { MarketMinusAction, MarketPlusAction, MarketPlusTenAction, MarketBuyAction, MarketRowMissingAction })
                        if (MarketPurchaseControlBounds(row, action).Contains(p)) return action + id;
                return MarketExpandAction + id;
            }
            return 0;
        }
        private Rect MarketPlayBounds() => new Rect(22, logicalCanvasHeight - 80, W - 44, 66);
        private void MarketRevealIngredient(int id)
        {
            int[] ids = MarketVisibleIngredients();
            int slot = System.Array.IndexOf(ids, id);
            if (slot < 0) { marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll()); return; }
            Rect row = MarketRowBounds(slot), body = MarketBodyBounds();
            if (row.y < body.y) marketScroll -= body.y - row.y;
            else if (row.yMax > body.yMax) marketScroll += row.yMax - body.yMax;
            marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll());
        }
        private void DrawMarketScreen()
        {
            Texture2D scene = MarketSceneArt();
            if (scene != null) DrawRawCover(new Rect(0, 0, W, logicalCanvasHeight), scene, new Rect(0, 0, scene.width, scene.height * .58f));
            FillRect(new Rect(0, 0, W, logicalCanvasHeight), new Color(.015f, .045f, .14f, .38f));
            DrawStandardButton(MarketBackBounds(), MarketBackAction, marketFromSelector ? "NIVELES" : "BARRIO");
            DrawMarketBluePanel(new Rect(150, 8, 240, 56), MarketRoyalBlue);
            MarketText(new Rect(164, 10, 212, 50), "MERCADO", 34, Color.white);
            DrawMarketClubHeader();
            MarketText(new Rect(26, 176, 488, 22), "OBJETIVOS DEL PARTIDO", 18, new Color(.84f, .96f, 1f));
            DrawMarketObjectiveCards(game.Sim);
            Rect economy = MarketEconomyBounds(game.Sim.ProductCount);
            DrawMarketBluePanel(economy, new Color(.012f, .075f, .25f, .98f));
            if (gameCoin != null) GUI.DrawTexture(new Rect(economy.x + 12, economy.y + 11, 36, 36), gameCoin, ScaleMode.ScaleToFit, true);
            MarketText(new Rect(economy.x + 54, economy.y + 12, 210, 32), "SALDO DISPONIBLE", 18, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(economy.x + 267, economy.y + 7, economy.width - 278, 44), FormatMarketMoney(game.Market.BalanceCents), 38, MarketGold, TextAnchor.MiddleRight);
            Rect body = MarketBodyBounds();
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                Vector2 point = ScreenToLayoutPoint(evt.mousePosition, new Vector2(Screen.width, Screen.height), CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea));
                point.y *= logicalCanvasHeight / H;
                if (body.Contains(point)) { marketScroll = Mathf.Clamp(marketScroll + evt.delta.y * 24f, 0, MarketMaxScroll()); evt.Use(); }
            }
            marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll());
            int[] visible = MarketVisibleIngredients();
            GUI.BeginGroup(body);
            try
            {
                for (int slot = 0; slot < visible.Length; slot++)
                {
                    Rect row = MarketRowBounds(slot);
                    if (row.yMax <= body.y || row.y >= body.yMax) continue;
                    bool showControls = MarketPurchaseControlsVisible(row, body);
                    row.x -= body.x; row.y -= body.y;
                    DrawMarketIngredientRow(row, visible[slot], showControls);
                }
                if (MarketIngredientIds(false).Length > 0)
                {
                    Rect others = MarketOthersBounds(); others.x -= body.x; others.y -= body.y;
                    DrawMarketOthers(others);
                }
            }
            finally { GUI.EndGroup(); }
            DrawMarketScrollBar(body);
            if (!string.IsNullOrEmpty(marketMessage))
            {
                Rect messageBounds = new Rect(20, logicalCanvasHeight - 180, W - 40, 22);
                DrawMarketBluePanel(messageBounds, new Color(.015f, .12f, .38f, .96f));
                MarketText(new Rect(messageBounds.x + 6, messageBounds.y, messageBounds.width - 12, messageBounds.height), marketMessage, 14, Color.white);
            }
            DrawMarketEconomyFooter();
            DrawMarketPlayButton(MarketPlayBounds(), "JUGAR PARTIDO", game.CanStartRound);
        }
        private Texture2D MarketSceneArt()
        {
            switch (VisualLevelIndex)
            {
                case 0: return muralArt; case 1: return chicagoBackground; case 2: return velezBackground;
                case 3: return ferroBackground; case 4: return independienteBackground; case 5: return racingBackground;
                case 6: return sanLorenzoBackground; case 7: return riverMural; case 8: return bocaBackground;
                case 9: return camionerosBackground; default: return losRedondosBackground;
            }
        }
        private void DrawMarketClubHeader()
        {
            DrawMarketBluePanel(new Rect(12, 70, W - 24, 100), new Color(.012f, .065f, .22f, .97f));
            int level = game.Sim.LevelIndex;
            Texture2D crest = LevelCrestForLevel(level);
            Rect badge = LevelCrestSizedSlot(new Rect(28, 82, 76, 76), crest);
            if (crest != null) DrawLevelCrest(badge, crest);
            else
            {
                // No invented official crest: existing club palette/initials identify levels without a crest asset.
                DrawMarketBluePanel(badge, ClubVisualTheme.ForLevel(level).PrimaryColor);
                MarketText(new Rect(badge.x + 6, badge.y + 20, badge.width - 12, 36), MarketClubInitials(level), 22, Color.white);
            }
            string[] club = StreetSimulation.LevelDisplayNames[level].Split('/');
            float captionX = Mathf.Max(120f, badge.xMax + 16f);
            float captionWidth = 506f - captionX;
            MarketText(new Rect(captionX, 77, captionWidth, 24), "NIVEL " + (level + 1), 19, MarketCyan, TextAnchor.MiddleLeft);
            MarketText(new Rect(captionX, 100, captionWidth, 35), club[0].Trim().ToUpperInvariant(), 29, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(captionX, 136, captionWidth, 23), club.Length > 1 ? club[1].Trim().ToUpperInvariant() : "", 19, new Color(.82f, .94f, 1f), TextAnchor.MiddleLeft);
        }
        private static string MarketClubInitials(int level)
        {
            string name = StreetSimulation.LevelNames[level];
            string[] words = name.Split(' '); string value = "";
            foreach (string word in words) if (word.Length > 0 && value.Length < 3) value += char.ToUpperInvariant(word[0]);
            return value;
        }
        private static string MarketObjectiveCaption(StreetSimulation sim, int slot, bool compact)
        {
            if (sim == null || slot < 0 || slot >= sim.ProductCount) return string.Empty;
            int product = sim.GetAvailableProduct(slot);
            string name = StreetSimulation.ProductNames[product]; int separator = name.IndexOf(' ');
            return (separator > 0 ? name.Substring(0, separator) : name) + " ×" + sim.GetProductGoal(product);
        }
        private void DrawMarketObjectiveCards(StreetSimulation sim)
        {
            for (int slot = 0; slot < sim.ProductCount; slot++)
            {
                int product = sim.GetAvailableProduct(slot); Rect card = MarketObjectiveCardBounds(slot, sim.ProductCount);
                DrawMarketBluePanel(card, MarketIce);
                float iconSize = sim.ProductCount > 3 ? 32f : 48f;
                Rect icon = new Rect(card.x + 10, card.center.y - iconSize * .5f, iconSize, iconSize);
                DrawProductIcon(icon, product);
                float textX = icon.xMax + 4f, textWidth = card.xMax - textX - 10f;
                string name = MarketObjectiveCaption(sim, slot, false).Split('×')[0].Trim();
                MarketText(new Rect(textX, card.y + 12, textWidth, 20), name, sim.ProductCount > 3 ? 14 : 20, MarketBlueInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(textX, card.y + 31, textWidth, 27), "×" + sim.GetProductGoal(product), sim.ProductCount > 3 ? 24 : 28, MarketBlueInk, TextAnchor.MiddleLeft);
            }
        }
        private static string MarketIngredientName(int id) => id == 9 ? "Coca 2,25 L" : StreetIngredientCatalog.Names[id];
        private void DrawMarketIngredientRow(Rect row, int id, bool showControls)
        {
            bool expanded = id == MarketExpandedId();
            float faceHeight = row.height - (expanded ? MarketPurchaseHeight : 0f);
            Rect card = new Rect(row.x, row.y + 2, row.width, row.height - 5);
            DrawMarketBluePanel(card, MarketIce);
            DrawMarketIngredientIcon(MarketIngredientIconBounds(row, faceHeight), id);
            float nameX = row.x + 100;
            MarketText(new Rect(nameX, row.y + 17, 144, 27), MarketIngredientName(id), 28, MarketBlueInk, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX, row.y + 47, 94, 26), FormatMarketMoney(game.Market.PackPriceCents(id)), 25, MarketRoyalBlue, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX + 96, row.y + 47, 50, 27), StreetIngredientCatalog.PackUnits[id] == 1 ? "1 u." : "4 porc.", 12, MarketBlueInk, TextAnchor.MiddleLeft);
            int needed = game.Market.RequiredUnits(game.Sim)[id], stock = game.Market.Units(id), missing = game.Market.MissingUnits(game.Sim, id);
            float statsX = row.x + 252, statsWidth = row.width - 264, column = statsWidth / 3f;
            string[] labels = { "NECESITÁS", "TENÉS", "FALTAN" };
            int[] values = { needed, stock, missing };
            for (int i = 0; i < 3; i++)
            {
                MarketText(new Rect(statsX + i * column, row.y + 12, column, 17), labels[i], 13, MarketBlueInk);
                MarketText(new Rect(statsX + i * column, row.y + 28, column, 30), values[i].ToString(), 28,
                    i == 1 ? MarketGreen : i == 2 && missing > 0 ? MarketRed : MarketBlueInk);
            }
            Rect track = new Rect(statsX, row.y + 66, statsWidth - 84, 16);
            FillRect(track, MarketNavy);
            bool unlocked = game.Market.IsIngredientUnlocked(id, game.UnlockedLevel);
            float coverage = needed > 0 ? Mathf.Clamp01((float)stock / needed) : 0f;
            FillRect(new Rect(track.x + 2, track.y + 2, (track.width - 4) * coverage, track.height - 4), missing > 0 ? MarketCyan : new Color(.20f, .78f, .32f));
            MarketText(new Rect(track.xMax + 4, row.y + 62, 80, 24), !unlocked ? "BLOQUEADO" : needed == 0 ? "RESERVA" : missing == 0 ? "COMPLETO" : "FALTANTE", 12,
                !unlocked || needed == 0 ? MarketBlueInk : missing > 0 ? MarketRed : MarketGreen);
            if (unlocked)
            {
                if (expanded && showControls)
                {
                    int packs = marketPendingPacks[id];
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketMinusAction), MarketMinusAction + id, "−", packs > 0);
                    Rect minus = MarketPurchaseControlBounds(row, MarketMinusAction);
                    DrawMarketBluePanel(new Rect(row.x + 54, minus.y + 3, 40, 40), MarketNavy);
                    MarketText(new Rect(row.x + 54, minus.y, 40, 46), packs.ToString(),
                        Mathf.Min(24, 60 / packs.ToString().Length), Color.white);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketPlusAction), MarketPlusAction + id, "+");
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketPlusTenAction), MarketPlusTenAction + id, "+10");
                    long cost = (long)packs * game.Market.PackPriceCents(id);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketBuyAction), MarketBuyAction + id,
                        (missing == 0 && needed > 0 ? "COMPRAR MÁS\n" : "COMPRAR\n") + FormatMarketMoney(cost), packs > 0 && cost <= game.Market.BalanceCents);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketRowMissingAction), MarketRowMissingAction + id, "COMPRAR\nFALTANTES", missing > 0 && (long)game.Market.MissingPacks(game.Sim, id) * game.Market.PackPriceCents(id) <= game.Market.BalanceCents);
                }
                // Row expansion never buys anything. On Android the hit surface is limited to its compact face.
                bool prior = GUI.enabled;
                if (GUI.Button(new Rect(row.x, row.y, row.width, faceHeight), "", invisible)) NativeAction(MarketExpandAction + id);
                GUI.enabled = prior;
            }
        }
        private static Rect MarketIngredientIconBounds(Rect row, float faceHeight)
        {
            float size = Mathf.Min(faceHeight < 100f ? 62f : 78f, Mathf.Max(0f, faceHeight - 24f));
            return new Rect(row.x + 14f, row.y + (faceHeight - size) * .5f, size, size);
        }
        private static Rect FitMarketIngredientIcon(Rect bounds, Vector2 sourceSize)
        {
            if (bounds.width <= 0f || bounds.height <= 0f || sourceSize.x <= 0f || sourceSize.y <= 0f) return Rect.zero;
            float scale = Mathf.Min(bounds.width / sourceSize.x, bounds.height / sourceSize.y);
            Vector2 size = sourceSize * scale;
            return new Rect(bounds.center - size * .5f, size);
        }
        private static Rect MarketIngredientSpriteSource(Sprite sprite)
        {
            Rect source = sprite.rect;
            // External meat sprites keep authored pixel bounds even when Android downsizes the atlas.
            // Normalize the entire source, not just its origin, so adjacent cooking states cannot bleed in.
            if (sprite.name.StartsWith("meat-", System.StringComparison.Ordinal))
            {
                Vector2 scale = new Vector2(sprite.texture.width / 2172f, sprite.texture.height / 724f);
                source = new Rect(Vector2.Scale(source.position, scale), Vector2.Scale(source.size, scale));
            }
            source.y = sprite.texture.height - source.yMax;
            return source; // Full sprite, never slice a sandwich to fake a bread icon.
        }
        private void DrawMarketIngredientIcon(Rect bounds, int id)
        {
            Texture2D standalone = id == 1 ? marketBreadCommon : id == 3 ? marketBreadBurger
                : id == 8 ? marketFernetBottle : id == 9 ? marketCocaLarge : null;
            if (standalone != null)
            {
                GUI.DrawTexture(FitMarketIngredientIcon(bounds, new Vector2(standalone.width, standalone.height)), standalone, ScaleMode.StretchToFill, true);
                return;
            }
            Sprite sprite = id >= 0 && id < marketIngredientSprites.Length ? marketIngredientSprites[id] : null;
            if (sprite == null) { DrawProductIcon(bounds, id == 6 ? 4 : id == 7 ? 6 : id == 8 ? 5 : 0); return; }
            Rect source = MarketIngredientSpriteSource(sprite);
            Rect fit = FitMarketIngredientIcon(bounds, source.size);
            DrawRaw(fit, sprite.texture, source, true, false);
        }
        private void DrawMarketScrollBar(Rect body)
        {
            float max = MarketMaxScroll(); if (max <= 0) return;
            Rect track = new Rect(W - 9, body.y, 4, body.height);
            FillRect(track, new Color(.008f, .035f, .14f, .88f));
            float height = Mathf.Max(28, body.height * body.height / (body.height + max));
            FillRect(new Rect(track.x, track.y + (track.height - height) * marketScroll / max, 4, height), MarketCyan);
        }
        private void DrawMarketOthers(Rect bounds)
        {
            DrawMarketBluePanel(new Rect(bounds.x, bounds.y + 2, bounds.width, bounds.height - 4), new Color(.025f, .18f, .48f));
            int[] others = MarketIngredientIds(false);
            for (int i = 0; i < Mathf.Min(3, others.Length); i++) DrawMarketIngredientIcon(new Rect(bounds.x + 10 + i * 34, bounds.y + 8, 30, 30), others[i]);
            MarketText(new Rect(bounds.x + 118, bounds.y + 7, bounds.width - 156, 32), "OTROS INGREDIENTES", 18, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(bounds.xMax - 38, bounds.y + 6, 28, 32), marketOtherExpanded ? "−" : "+", 28, Color.white);
            if (GUI.Button(bounds, "", invisible)) NativeAction(MarketOtherAction);
        }
        private void DrawMarketEconomyFooter()
        {
            Rect footer = MarketEconomyFooterBounds(logicalCanvasHeight);
            DrawMarketBluePanel(footer, new Color(.012f, .075f, .25f));
            long cost = game.Market.MinimumPurchaseCostCents(game.Sim);
            MarketText(new Rect(footer.x + 12, footer.y + 7, 230, 23), "COSTO PARA COMPLETAR", 14, Color.white);
            MarketText(new Rect(footer.x + 12, footer.y + 30, 230, 30), FormatMarketMoney(cost), 36, MarketGold);
            bool canBuyMissing = MarketCanBuyMissing();
            string caption = !MarketHasMissing() ? "STOCK COMPLETO" : canBuyMissing ? "COMPRAR FALTANTES" : "SALDO INSUFICIENTE";
            DrawStandardButton(MarketBuyMissingBounds(logicalCanvasHeight), MarketMissingAction, caption, canBuyMissing);
        }
        private void DrawMarketPlayButton(Rect bounds, string caption, bool enabled)
        {
            DrawStandardButton(bounds, MarketPlayAction, caption, enabled);
        }

        private readonly System.Collections.Generic.Dictionary<Color,
            System.Collections.Generic.Dictionary<Vector2Int, Texture2D>> marketPanelTextures =
            new System.Collections.Generic.Dictionary<Color, System.Collections.Generic.Dictionary<Vector2Int, Texture2D>>();

        private void DrawMarketBluePanel(Rect bounds, Color face)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            System.Collections.Generic.Dictionary<Vector2Int, Texture2D> sizes;
            if (!marketPanelTextures.TryGetValue(face, out sizes))
            {
                sizes = new System.Collections.Generic.Dictionary<Vector2Int, Texture2D>();
                marketPanelTextures.Add(face, sizes);
            }
            var size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(bounds.width * 2f)),
                Mathf.Max(1, Mathf.RoundToInt(bounds.height * 2f)));
            Texture2D texture;
            if (!sizes.TryGetValue(size, out texture))
            {
                texture = CreateRoundedMarketPanel(size.x, size.y, face);
                sizes.Add(size, texture);
            }
            GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
        }

        private static Texture2D CreateRoundedMarketPanel(int width, int height, Color faceColor)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = "Rounded navy cyan market panel", hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float w = width * .5f, h = height * .5f;
            float radius = Mathf.Min(16f, h * .32f);
            Rect shadow = new Rect(1, 3, w - 1, h - 3);
            Rect rim = new Rect(0, 0, w - 1, h - 3);
            Rect cyan = new Rect(2, 2, w - 5, h - 7);
            Rect face = new Rect(4, 4, w - 9, h - 11);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * .5f, py = (height - y - .5f) * .5f;
                Color color = new Color(.003f, .018f, .075f,
                    .64f * RoundedCoverage(px, py, shadow, radius));
                color = Over(color, MarketNavy, RoundedCoverage(px, py, rim, radius));
                color = Over(color, MarketCyan, RoundedCoverage(px, py, cyan, Mathf.Max(1, radius - 2)));
                float coverage = RoundedCoverage(px, py, face, Mathf.Max(1, radius - 4));
                color = Over(color, faceColor, coverage);
                float gloss = .22f * Mathf.Clamp01(1f - (py - face.y) / Mathf.Max(1, face.height * .38f));
                color = Over(color, Color.white, coverage * gloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply();
            return texture;
        }
        private void DrawMarketWoodPanel(Rect bounds, Color face)
        {
            FillRect(bounds, new Color(.24f, .105f, .025f));
            if (upgradeWood != null)
            {
                const float border = 12f, sourceBorder = 180f;
                Rect src = UpgradeWoodBounds;
                float[] sx = { src.x, src.x + sourceBorder, src.xMax - sourceBorder, src.xMax };
                float[] sy = { src.y, src.y + sourceBorder, src.yMax - sourceBorder, src.yMax };
                float[] dx = { bounds.x, bounds.x + border, bounds.xMax - border, bounds.xMax };
                float[] dy = { bounds.y, bounds.y + border, bounds.yMax - border, bounds.yMax };
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                    DrawRaw(Rect.MinMaxRect(dx[x], dy[y], dx[x + 1], dy[y + 1]), upgradeWood,
                        Rect.MinMaxRect(sx[x], sy[y], sx[x + 1], sy[y + 1]), true, false);
            }
            FillRect(new Rect(bounds.x + 12, bounds.y + 12, bounds.width - 24, bounds.height - 24), face);
        }
        private void MarketText(Rect bounds, string value, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            if (marketLabelStyle == null) marketLabelStyle = new GUIStyle(menuTitle) { padding = new RectOffset(), wordWrap = false };
            marketLabelStyle.alignment = alignment;
            marketLabelStyle.fontSize = size;
            float measured = marketLabelStyle.CalcSize(new GUIContent(value)).x;
            marketLabelStyle.fontSize = Mathf.Max(12, Mathf.FloorToInt(size * Mathf.Min(1f, Mathf.Max(1f, bounds.width - 2f) / Mathf.Max(1f, measured))));
            marketLabelStyle.normal.textColor = color;
            GUI.Label(bounds, value, marketLabelStyle);
        }
        private string MarketEconomyFooterSummary()
        {
            long balance = game.Market.BalanceCents, cost = game.Market.MinimumPurchaseCostCents(game.Sim);
            return "FALTANTES " + FormatMarketMoney(cost) + " · SALDO DESPUÉS " + (cost <= balance ? FormatMarketMoney(balance - cost) : "INSUFICIENTE");
        }
        private static string FormatBottleStock(int units) => (units/4)+" botella(s) + "+(units%4)+"/4 porción";
        private bool MarketHasMissing()
        {
            for (int i = 0; i < StreetIngredientCatalog.Names.Length; i++) if (game.Market.MissingPacks(game.Sim, i) > 0) return true;
            return false;
        }
        private bool MarketCanBuyMissing() => MarketHasMissing() && game.Market.MinimumPurchaseCostCents(game.Sim) <= game.Market.BalanceCents;
        private string MarketInsufficientFundsMessage(long totalCost) => "Saldo insuficiente: faltan " + FormatMarketMoney(totalCost-game.Market.BalanceCents);
        private static string FormatMarketMoney(long cents) => "$"+(cents/100L)+","+(cents%100L).ToString("00");
        private static Rect LevelSelectCardBounds(int index)
        {
            const float width = 244f;
            int row = index / 2, column = index % 2;
            float x = index == StreetSimulation.LevelNames.Length - 1 && column == 0
                ? (W - width) * .5f : 20f + column * 256f;
            return new Rect(x, 160f + row * (LevelSelectorCardHeight + LevelSelectorCardGap),
                width, LevelSelectorCardHeight);
        }
        private void DrawLevelSelectCard(int index, Rect baseBounds)
        {
            DrawLevelSelectorCardPresentation(index, baseBounds);
        }
        private void DrawLevelSelectArtwork(int index, Rect bounds, float cardScale)
        {
            if (index == 0 && muralArt != null)
            {
                DrawRawCover(LayoutRect(bounds), muralArt, AllBoysSelectorMuralSource);
                if (allBoysCrest != null)
                    DrawLevelCrest(LayoutRect(new Rect(bounds.x + 5 * cardScale, bounds.y + 5 * cardScale,
                        30 * cardScale, 34 * cardScale)), allBoysCrest);
                return;
            }
            if (index == 1 && chicagoBackground != null)
            {
                DrawRawCover(LayoutRect(bounds), chicagoBackground, ChicagoSelectorMuralSource);
                if (chicagoCrest != null)
                    DrawLevelCrest(LayoutRect(new Rect(bounds.x + 5 * cardScale, bounds.y + 5 * cardScale,
                        30 * cardScale, 34 * cardScale)), chicagoCrest);
                return;
            }
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(index);
            if (index >= 2 && !string.IsNullOrEmpty(theme.MuralResource))
            {
                Texture2D art = Resources.Load<Texture2D>(theme.MuralResource);
                if (art != null)
                {
                    Rect source = theme.MuralSource;
                    DrawRawCover(LayoutRect(bounds), art, new Rect(source.x * art.width, source.y * art.height,
                        source.width * art.width, source.height * art.height));
                    FillRect(new Rect(bounds.x, bounds.y, bounds.width, 5 * cardScale), theme.PrimaryColor);
                    Texture2D crest = LevelCrestForLevel(index);
                    if (crest != null)
                        DrawLevelCrest(LayoutRect(new Rect(bounds.x + 5 * cardScale, bounds.y + 5 * cardScale,
                            30 * cardScale, 34 * cardScale)), crest);
                    return;
                }
            }

            // Later locations preview their expanding product lineup on the original game grill art.
            if (largeGrill != null) GUI.DrawTexture(LayoutRect(bounds), largeGrill, ScaleMode.ScaleAndCrop, true);
            Color accent = LevelSelectAccent(index);
            FillRect(new Rect(bounds.x, bounds.y, bounds.width, 6 * cardScale), accent);
            FillRect(new Rect(bounds.x, bounds.y + 6 * cardScale, bounds.width, 2 * cardScale), new Color(1f, 1f, 1f, .8f));
            int[] featured = LaterLevelCardProducts[Mathf.Clamp(index - 2, 0, LaterLevelCardProducts.Length - 1)];
            float iconWidth = 43f, gap = 12f;
            float start = bounds.center.x
                - (iconWidth * featured.Length + gap * (featured.Length - 1)) * cardScale * .5f;
            for (int i = 0; i < featured.Length; i++)
            {
                Rect icon = new Rect(start + i * (iconWidth + gap) * cardScale, bounds.y + 38 * cardScale,
                    iconWidth * cardScale, 43 * cardScale);
                FillRect(icon, new Color(.08f, .045f, .025f, .8f));
                DrawProductIcon(new Rect(icon.x + 3, icon.y + 3, icon.width - 6, icon.height - 6), featured[i]);
            }
        }
        private static Color LevelSelectAccent(int levelIndex)
        {
            switch (levelIndex)
            {
                case 2: return new Color(.08f, .27f, .59f); // Vélez: blue/white
                case 3: return new Color(.04f, .38f, .19f); // Ferro: green/white
                default: return new Color(.79f, .035f, .06f); // Independiente: red/white
            }
        }
        private void FillRect(Rect bounds, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(LayoutRect(bounds), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
        private void OutlineLabel(Rect bounds, string value, GUIStyle style, Color fill, float offset)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = new Color(.08f, .035f, .018f, .98f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x * offset, bounds.y + y * offset, bounds.width, bounds.height), value, style);
            style.normal.textColor = fill;
            Label(bounds, value, style);
            style.normal.textColor = previous;
        }
        // The cover is the sole visual source for ordinary runtime buttons.
        // Special upgrade cards and image-based level tiles retain their explicitly selected designs.
        public void DrawAccountButton(Rect bounds, string caption, System.Action activated)
        {
            Styles(); float previousScale = layoutVerticalScale; layoutVerticalScale = 1f;
            try { DrawStandardButton(bounds, int.MinValue, caption, true, activated); }
            finally { layoutVerticalScale = previousScale; }
        }

        public void DrawGoogleAccountButton(Rect bounds, string caption, Texture2D googleMark, System.Action activated)
        {
            Styles();
            float previousScale = layoutVerticalScale;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            layoutVerticalScale = 1f;
            try
            {
                bool interactive = GUI.enabled;
                bool pressed = interactive && pressedAction == int.MinValue;
                Rect hitBounds = LayoutRect(bounds);
                Rect drawn = StandardButtonVisualBounds(hitBounds, pressed);
                GUI.color = interactive ? Color.white : new Color(.93f, .93f, .93f, 1f);
                if (googleButtonSurface != null)
                    GUI.DrawTexture(drawn, googleButtonSurface, ScaleMode.StretchToFill, true);
                else
                    GUI.DrawTexture(drawn, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);

                GUI.color = Color.white;
                float iconInset = googleMark != null ? drawn.height * .64f : 0f;
                Rect fitBounds = new Rect(0f, 0f, Mathf.Max(1f, bounds.width - iconInset * 2f), bounds.height);
                int fontSize = FitStandardButtonFontSize(menuTitle, caption, fitBounds);
                var label = CreateStandardButtonLabelStyle(menuTitle, fontSize,
                    interactive ? new Color(.06f, .06f, .06f) : new Color(.46f, .46f, .46f));
                float textScale = bounds.height / 92f;
                Rect letters = new Rect(drawn.x + iconInset, drawn.y - 2f * textScale,
                    drawn.width - iconInset * 2f, drawn.height - 8f * textScale);

                if (googleMark != null)
                {
                    float iconSize = Mathf.Min(drawn.height * .54f, drawn.width * .12f);
                    float sideInset = drawn.width * .075f;
                    GUI.DrawTexture(new Rect(drawn.x + sideInset, drawn.center.y - iconSize * .5f,
                        iconSize, iconSize), googleMark, ScaleMode.ScaleToFit, true);
                }
                GUI.Label(letters, caption, label);
                GUI.enabled = interactive;
                if (GUI.Button(hitBounds, "", invisible)) activated?.Invoke();
            }
            finally
            {
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
                layoutVerticalScale = previousScale;
            }
        }

        public void ContinueToGameAfterGoogleSignIn()
        {
            // Authentication only unlocks the existing save; start the cover timer once authorized.
            if (!introActive || game == null || !game.IsPlayerActive || game.Sim == null) return;
            introStarted = Time.unscaledTime;
            splashClockStarted = true;
        }

        private void OpenHubFromIntro()
        {
            if (!introActive || game == null || game.Sim == null) return;
            introActive = false; levelSelectActive = false; marketActive = false;
            badgesActive = false; rankingActive = false; hubActive = true; marketFromSelector = false;
            pressedAction = 0;
        }

        private void DrawStandardButton(Rect bounds, int action, string caption, bool enabled = true,
            System.Action activated = null, Texture2D leadingIcon = null)
        {
            bool interactive = enabled && GUI.enabled;
            bool pressed = interactive && pressedAction == action;
            Rect hitBounds = LayoutRect(bounds);
            Rect drawn = StandardButtonVisualBounds(hitBounds, pressed);
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.color = StandardButtonTint(interactive, pressed);
                DrawStandardButtonBackground(drawn);
                GUI.color = Color.white;
                float iconInset = leadingIcon != null ? drawn.height * .64f : 0f;
                Rect fitBounds = bounds;
                fitBounds.width = Mathf.Max(1f, fitBounds.width - iconInset * 2f);
                int fontSize = FitStandardButtonFontSize(menuTitle, caption, fitBounds);
                var captionStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize,
                    interactive ? Color.white : new Color(.80f, .87f, .94f));
                var borderStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize, new Color(.035f, .075f, .16f));
                float textScale = bounds.height / 92f;
                Rect letters = new Rect(drawn.x + iconInset, drawn.y - 2f * textScale,
                    drawn.width - iconInset * 2f, drawn.height - 8f * textScale);
                if (leadingIcon != null)
                {
                    float iconSize = Mathf.Min(drawn.height * .54f, drawn.width * .12f);
                    float sideInset = drawn.width * .075f;
                    GUI.DrawTexture(new Rect(drawn.x + sideInset, drawn.center.y - iconSize * .5f,
                        iconSize, iconSize), leadingIcon, ScaleMode.ScaleToFit, true);
                }
                float outline = 2.2f * captionStyle.fontSize / 38f;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (x != 0 || y != 0)
                        GUI.Label(new Rect(letters.x + x * outline, letters.y + y * outline,
                            letters.width, letters.height), caption, borderStyle);
                GUI.Label(letters, caption, captionStyle);
                GUI.enabled = interactive;
                if (GUI.Button(hitBounds, "", invisible)) { if (activated != null) activated(); else NativeAction(action); }
            }
            finally
            {
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        private static Rect StandardButtonVisualBounds(Rect bounds, bool pressed)
        {
            float scale = pressed ? .97f : 1f;
            return new Rect(bounds.center.x - bounds.width * scale * .5f,
                bounds.center.y - bounds.height * scale * .5f, bounds.width * scale, bounds.height * scale);
        }

        private static Color StandardButtonTint(bool enabled, bool pressed) =>
            !enabled ? new Color(.50f, .62f, .75f) : pressed ? new Color(.78f, .88f, 1f) : Color.white;

        private static GUIStyle CreateStandardButtonLabelStyle(GUIStyle source, int fontSize, Color color)
        {
            var style = new GUIStyle(source) { fontSize = fontSize };
            // GUI.Label also inherits skin hover colors; they must not erase our custom outline.
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            return style;
        }

        private static int FitStandardButtonFontSize(GUIStyle style, string caption, Rect bounds)
        {
            int original = style.fontSize;
            int preferred = Mathf.Max(11, Mathf.RoundToInt(38f * bounds.height / 92f));
            try
            {
                style.fontSize = preferred;
                float width = Mathf.Max(1f, bounds.width - 24f * bounds.height / 92f);
                float measured = style.CalcSize(new GUIContent(caption)).x;
                return measured > width ? Mathf.Max(11, Mathf.FloorToInt(preferred * width / measured)) : preferred;
            }
            finally { style.fontSize = original; }
        }

        private static Rect StandardButtonSliceBounds(Rect bounds, int slice)
        {
            float cap = Mathf.Min(bounds.height * .5f, bounds.width * .5f);
            return slice == 0 ? new Rect(bounds.x, bounds.y, cap, bounds.height)
                : slice == 1 ? new Rect(bounds.x + cap, bounds.y, bounds.width - cap * 2f, bounds.height)
                : new Rect(bounds.xMax - cap, bounds.y, cap, bounds.height);
        }

        private void DrawStandardButtonBackground(Rect bounds)
        {
            if (menuButton == null) return;
            // The original cover aspect uses its original exact texture mapping.
            if (Mathf.Abs(bounds.width / bounds.height - menuButton.width / (float)menuButton.height) < .03f)
            {
                GUI.DrawTexture(bounds, menuButton, ScaleMode.StretchToFill, true);
                return;
            }
            // Resize only the straight middle; end caps, gloss, rim and shadow retain their proportions.
            for (int slice = 0; slice < 3; slice++)
            {
                Rect dest = StandardButtonSliceBounds(bounds, slice);
                Rect source = StandardButtonSliceBounds(new Rect(0, 0, menuButton.width, menuButton.height), slice);
                if (dest.width <= 0f) continue;
                // Fractional portrait transforms can round adjacent IMGUI rectangles apart.
                // Overlap their internal edges by less than one physical pixel, extending matching UVs.
                float seam = .75f / Mathf.Max(.01f, Mathf.Abs(GUI.matrix.m00));
                float sourceSeam = seam * source.width / dest.width;
                if (slice > 0) { dest.xMin -= seam; source.xMin -= sourceSeam; }
                if (slice < 2) { dest.xMax += seam; source.xMax += sourceSeam; }
                DrawRaw(dest, menuButton, source, true, false);
            }
        }

        private static Texture2D CreateMenuButton()
        {
            const int width = 512, height = 164;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Original glossy blue startup button",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect shadow = new Rect(8, 13, 496, 148), rim = new Rect(6, 3, 500, 149), face = new Rect(11, 7, 490, 137);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / face.height);
                Color color = new Color(.015f, .06f, .18f, .6f * RoundedCoverage(px, py, shadow, 70));
                color = Over(color, Color.Lerp(new Color(.73f, .96f, 1f), new Color(.025f, .12f, .57f), t), RoundedCoverage(px, py, rim, 72));
                float faceAlpha = RoundedCoverage(px, py, face, 67);
                color = Over(color, Color.Lerp(new Color(.2f, .81f, 1f), new Color(.02f, .22f, .98f), t), faceAlpha);
                float gloss = .48f * (1f - IntroEase((t - .02f) / .48f));
                color = Over(color, Color.white, faceAlpha * gloss);
                float dx = (px - 47f) / 13f, dy = (py - 25f) / 5f;
                color = Over(color, Color.white, .78f * faceAlpha * Mathf.Clamp01(1f - dx * dx - dy * dy));
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
        private static Texture2D CreateGoogleButtonSurface()
        {
            const int width = 512, height = 164;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "White Google startup button capsule",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect shadow = new Rect(8, 13, 496, 148), rim = new Rect(6, 3, 500, 149), face = new Rect(11, 7, 490, 137);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / face.height);
                Color color = new Color(.08f, .08f, .09f, .16f * RoundedCoverage(px, py, shadow, 70));
                color = Over(color, new Color(.79f, .80f, .82f), RoundedCoverage(px, py, rim, 72));
                float faceAlpha = RoundedCoverage(px, py, face, 67);
                color = Over(color, Color.Lerp(new Color(1f, 1f, 1f), new Color(.965f, .965f, .965f), t), faceAlpha);
                float gloss = .16f * (1f - IntroEase((t - .02f) / .48f));
                color = Over(color, Color.white, faceAlpha * gloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float RoundedCoverage(float x, float y, Rect r, float radius)
        {
            float dx = Mathf.Abs(x - r.center.x) - (r.width * .5f - radius);
            float dy = Mathf.Abs(y - r.center.y) - (r.height * .5f - radius);
            float distance = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude
                             + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
            return Mathf.Clamp01(.5f - distance);
        }
        private static Color Over(Color below, Color above, float coverage)
        {
            float alpha = above.a * coverage, remaining = below.a * (1f - alpha), total = alpha + remaining;
            if (total <= 0f) return Color.clear;
            return new Color((above.r * alpha + below.r * remaining) / total,
                             (above.g * alpha + below.g * remaining) / total,
                             (above.b * alpha + below.b * remaining) / total, total);
        }
        private static float IntroEase(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private void DrawWaitingCrowd(StreetSimulation sim)
        {
            // Clip the crowd at the shared counter edge; custom later-level backdrops draw the counter after it.
            // Workers, products and sale effects stay on the player side and outside this group.
            GUI.BeginGroup(new Rect(0, 0, W, CustomerCounterTopY()));
            try
            {
                for (int row = 4; row >= 0; row--)
                for (int i = 0; i < sim.Customers.Count; i++)
                {
                    StreetCustomer customer = sim.Customers[i];
                    if (Mathf.Clamp(Mathf.RoundToInt((StreetSimulation.FrontQueueY - customer.Position.y)
                        / StreetSimulation.QueueRowSpacing), 0, 4) != row) continue;
                    DrawCustomer(customer, ShouldDrawOrderBubble(row));
                }
            }
            finally { GUI.EndGroup(); }
        }
        private float CustomerCounterTopY()
        {
            return projectedCounterTopY;
        }
        private static float CounterSurfaceY(float canvasHeight, Vector2 backdropSize, int levelIndex)
        {
            // Legacy private test wrapper; neither club nor replacement texture dimensions control perspective.
            return StreetSceneLayout.CounterTop(canvasHeight);
        }
        private static float CustomerViewOffsetY(float counterTop, float verticalScale)
        {
            return StreetSceneLayout.CustomerOffset(counterTop, verticalScale);
        }
        private Vector2 CustomerViewPosition(Vector2 position)
        {
            position.y += CustomerViewOffsetY(CustomerCounterTopY(), layoutVerticalScale);
            return position;
        }
        private static bool ShouldDrawOrderBubble(int visualRow) => visualRow < 2;

        private void DrawCustomer(StreetCustomer c, bool showOrderBubble)
        {
            Vector2 position = CustomerViewPosition(c.Position);
            bool walking=c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving||c.State==StreetCustomerState.Advancing;
            DrawCustomerBody(c, walking);
            if(c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving)return;
            int pendingLines = c.PendingOrderLineCount;
            if (showOrderBubble && pendingLines > 0)
            {
                int visibleLines = Mathf.Min(StreetCustomer.MaxVisibleOrderProducts, pendingLines);
                float quantityWidth = 20f;
                for (int i = 0; i < visibleLines; i++)
                    quantityWidth = Mathf.Max(quantityWidth, text.CalcSize(new GUIContent(c.GetVisibleOrderLine(i).Remaining.ToString())).x);
                // Transform the anchor once; local content offsets must not grow on tall screens.
                Rect logicalBubble = OrderBubbleBounds(position, pendingLines, quantityWidth);
                float contentScale = OrderBubbleContentScale(logicalBubble.height, layoutVerticalScale);
                Rect bubble = OrderBubbleLayoutBounds(logicalBubble, layoutVerticalScale);
                DrawOrderBubble(bubble, contentScale);
                for (int i = 0; i < visibleLines; i++)
                    DrawOrderLine(OrderLineBounds(bubble, i, visibleLines, contentScale), c.GetVisibleOrderLine(i), contentScale);
                string hiddenOrderIndicator = HiddenOrderIndicatorText(c.HiddenOrderLineCount);
                if (!string.IsNullOrEmpty(hiddenOrderIndicator))
                    GUI.Label(OrderMoreBounds(bubble, contentScale), hiddenOrderIndicator, tiny);
            }
        }
        private const float OrderRowHeight = 25f;
        private const float OrderPaddingY = 8f;
        private const float OrderTailHeight = 8f;
        private const float OrderFooterHeight = 14f;

        private static string HiddenOrderIndicatorText(int hiddenOrderLineCount) =>
            hiddenOrderLineCount > 0 ? "•••" : string.Empty;

        private static float OrderBubbleContentScale(float bubbleHeight, float verticalScale)
        {
            float rowPitch = StreetSimulation.QueueRowSpacing * Mathf.Max(.01f, verticalScale);
            return Mathf.Min(1f, rowPitch / Mathf.Max(1f, bubbleHeight));
        }

        private static Rect OrderBubbleBounds(Vector2 customerPosition, int pendingLines, float quantityWidth = 28f)
        {
            int rows = Mathf.Clamp(pendingLines, 1, StreetCustomer.MaxVisibleOrderProducts);
            float width = Mathf.Clamp(42f + quantityWidth, 62f, 70f);
            float height = OrderPaddingY * 2f + rows * OrderRowHeight + OrderTailHeight
                + (pendingLines > rows ? OrderFooterHeight : 0f);
            // Keep the same customer-relative tail baseline and horizontal center as before.
            return new Rect(customerPosition.x - width * .5f, customerPosition.y - 68f - height, width, height);
        }

        private static Rect OrderBubbleLayoutBounds(Rect bubble, float verticalScale)
        {
            float anchoredBottom = bubble.yMax * verticalScale;
            float scale = OrderBubbleContentScale(bubble.height, verticalScale);
            bubble.height *= scale;
            bubble.y = anchoredBottom - bubble.height;
            return bubble;
        }

        private static Rect OrderLineBounds(Rect bubble, int row, int visibleRows, float contentScale) =>
            new Rect(bubble.x + 5f, bubble.y + OrderPaddingY * contentScale + row * OrderRowHeight * contentScale,
                bubble.width - 10f, OrderRowHeight * contentScale);

        private static Rect OrderMoreBounds(Rect bubble, float contentScale) =>
            new Rect(bubble.x + 4f, bubble.y + (OrderPaddingY + 2f * OrderRowHeight) * contentScale,
                bubble.width - 8f, OrderFooterHeight * contentScale);

        private static Rect OrderBubbleSlice(Rect bounds, int column, int row, float borderX, float borderY)
        {
            float x = column == 0 ? bounds.x : column == 1 ? bounds.x + borderX : bounds.xMax - borderX;
            float y = row == 0 ? bounds.y : row == 1 ? bounds.y + borderY : bounds.yMax - borderY;
            return new Rect(x, y, column == 1 ? bounds.width - borderX * 2f : borderX,
                row == 1 ? bounds.height - borderY * 2f : borderY);
        }

        private void DrawOrderBubble(Rect bounds, float contentScale)
        {
            if (items == null) return;
            // Reuse the authored speech shape; shorten vertical corners together with compact rows.
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                DrawRaw(OrderBubbleSlice(bounds, column, row, 20f, 15f * contentScale), items,
                    OrderBubbleSlice(Items[11], column, row, 84f, 65f), true, false);
        }

        private static Rect OrderIconBounds(Rect row, float contentScale) =>
            new Rect(row.x + 2f, row.y + contentScale, 26f * contentScale, Mathf.Max(1f, row.height - 2f * contentScale));
        private static Rect OrderQuantityBounds(Rect row, float contentScale) =>
            new Rect(row.x + 30f * contentScale, row.y, row.width - 32f * contentScale, row.height);

        private static int FitOrderQuantityFontSize(GUIStyle style, string value, float maxWidth)
        {
            int original = style.fontSize;
            float measured = style.CalcSize(new GUIContent(value)).x;
            return measured > maxWidth ? Mathf.Max(11, Mathf.FloorToInt(original * maxWidth / measured)) : original;
        }

        private void DrawOrderLine(Rect row, StreetOrderLine line, float contentScale)
        {
            if (line == null || line.Remaining <= 0) return;
            DrawProductIcon(OrderIconBounds(row, contentScale), line.Product, false);
            int originalFontSize = text.fontSize;
            text.fontSize = Mathf.Max(10, Mathf.RoundToInt(originalFontSize * contentScale));
            Rect quantity = OrderQuantityBounds(row, contentScale);
            text.fontSize = FitOrderQuantityFontSize(text, line.Remaining.ToString(), quantity.width);
            GUI.Label(quantity, line.Remaining.ToString(), text);
            text.fontSize = originalFontSize;
        }
        private void DrawCustomerBody(StreetCustomer c, bool walking)
        {
            Vector2 position = CustomerViewPosition(c.Position);
            float bob = walking ? Mathf.Sin(c.AnimationTime * 14) * 2 : 0f;
            if (c.State == StreetCustomerState.Receiving) bob -= 3;
            Rect person = new Rect(position.x - 39, position.y - 76 + bob, 78, 76);
            Matrix4x4 viewMatrix = GUI.matrix;
            if (!walking)
            {
                // The atlas front poses are different people, not interchangeable idle frames.
                // Animate their existing silhouettes from simulation time, never OnGUI events.
                float phase = c.Id * 2.399963f;
                float sway = Mathf.Sin(c.AnimationTime * (2.2f + (c.Id % 3) * .18f) + phase);
                float breath = Mathf.Sin(c.AnimationTime * 3.2f + phase);
                float cheerTime = Mathf.Repeat(c.AnimationTime + c.Id * .83f, 4.8f + (c.Id % 4) * .55f);
                float hop = 0f;
                if (cheerTime < 1.2f)
                {
                    float pulse = Mathf.Sin(cheerTime * Mathf.PI * 2f / 1.2f);
                    hop = pulse * pulse; // Two soft hops, with zero velocity at either end.
                }

                Rect responsivePerson = LayoutRect(person);
                Vector3 feet = new Vector3(responsivePerson.center.x, responsivePerson.yMax, 0f);
                Vector3 offset = new Vector3(sway * 1.2f, -hop * 4f, 0f);
                Vector3 scale = new Vector3(1f + breath * .012f - hop * .018f,
                    1f - breath * .016f + hop * .025f, 1f);
                GUI.matrix = viewMatrix * Matrix4x4.TRS(feet + offset,
                    Quaternion.Euler(0f, 0f, sway * 2.8f), scale) * Matrix4x4.Translate(-feet);
            }

            try
            {
                bool flip = walking && c.Target.x < c.Position.x;
                DrawFan(person, c.Id, walking, flip, ClubVisualTheme.ForLevel(VisualLevelIndex).Fans);
            }
            finally
            {
                // Only the body/garment move: badges and subsequent UI keep the safe-area matrix.
                GUI.matrix = viewMatrix;
            }
        }
        private void DrawFan(Rect person, int customerId, bool walking, bool flip, ClubFanDefinition fans)
        {
            if (fans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Texture2D atlas = walking ? fans.LoadWalkingFans() : fans.LoadFrontFans();
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Rect source = walking
                ? fans.WalkingSourceRect(atlas.width, atlas.height, customerId)
                : fans.FrontSourceRect(atlas.width, atlas.height, customerId);
            Draw(person, atlas, source, false, flip);
        }
        private static int GetAllBoysFanVariant(int customerId)
        {
            int variant = customerId % AllBoysFanVariantCount;
            return variant < 0 ? variant + AllBoysFanVariantCount : variant;
        }
        private static int GetAllBoysFanFrame(int customerId, bool walking) =>
            (walking ? AllBoysFanVariantCount : 0) + GetAllBoysFanVariant(customerId);
        private static int GetLegacyAllBoysFanFrame(int customerId, bool walking)
        {
            int variant = customerId % AllBoysLegacyFanVariantCount;
            if (variant < 0) variant += AllBoysLegacyFanVariantCount;
            return (walking ? AllBoysLegacyWalkingFanFirstFrame : AllBoysLegacyFrontFanFirstFrame) + variant;
        }
        private void DrawAllBoysFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? allBoysWalkingFans : allBoysFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, GetAllBoysFanSourceRect(atlas, GetAllBoysFanVariant(customerId)), false, flip);
        }
        private static Rect GetAllBoysFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % AllBoysFanVariantCount;
            float cellWidth = atlas.width / (float)AllBoysFanAtlasColumns;
            float cellHeight = atlas.height / (float)AllBoysFanAtlasColumns;
            int column = index % AllBoysFanAtlasColumns;
            int rowFromTop = index / AllBoysFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight, cellWidth, cellHeight);
        }
        private static int GetChicagoFanVariant(int customerId)
        {
            int variant = customerId % ChicagoFanVariantCount;
            return variant < 0 ? variant + ChicagoFanVariantCount : variant;
        }
        private void DrawChicagoFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? chicagoWalkingFans : chicagoFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, GetChicagoFanSourceRect(atlas, GetChicagoFanVariant(customerId)), false, flip);
        }
        private static Rect GetChicagoFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % ChicagoFanVariantCount;
            float cellWidth = atlas.width / (float)ChicagoFanAtlasColumns;
            float cellHeight = atlas.height / (float)ChicagoFanAtlasColumns;
            int column = index % ChicagoFanAtlasColumns;
            int rowFromTop = index / ChicagoFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawVelezFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? velezWalkingFans : velezFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = GetAllBoysFanVariant(customerId);
            Draw(person, atlas, VelezFanSourceRect(atlas, variant), false, flip);
        }
        private static Rect VelezFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % VelezFanVariantCount;
            float cellWidth = atlas.width / (float)VelezFanAtlasColumns;
            float cellHeight = atlas.height / (float)VelezFanAtlasColumns;
            int column = index % VelezFanAtlasColumns;
            int rowFromTop = index / VelezFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawFerroFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? ferroWalkingFans : ferroFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = customerId % FerroFanVariantCount;
            if (variant < 0) variant += FerroFanVariantCount;
            Draw(person, atlas, FerroFanSourceRect(atlas, variant), false, flip);
        }
        private static Rect FerroFanSourceRect(Texture2D atlas, int variant)
        {
            if (atlas == null) return Rect.zero;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawLosRedondosFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? losRedondosWalkingFans : losRedondosFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, LosRedondosFanSourceRect(atlas, customerId), false, flip);
        }
        private static Rect LosRedondosFanSourceRect(Texture2D atlas, int customerId)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % LosRedondosFanVariantCount;
            if (variant < 0) variant += LosRedondosFanVariantCount;
            float cellWidth = atlas.width / (float)LosRedondosFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % LosRedondosFanAtlasColumns;
            int rowFromTop = variant / LosRedondosFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawIndependienteFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? independienteWalkingFans : independienteFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = customerId % IndependienteFanVariantCount;
            if (variant < 0) variant += IndependienteFanVariantCount;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            Rect source = new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
            Draw(person, atlas, source, false, flip);
        }
        private void DrawTeamFan(Rect person, int customerId, bool walking, bool flip, Texture2D front, Texture2D walk)
        {
            Texture2D atlas = walking ? walk : front;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, TeamFanSourceRect(atlas, customerId), false, flip);
        }
        private static Rect TeamFanSourceRect(Texture2D atlas, int customerId)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % TeamFanVariantCount;
            if (variant < 0) variant += TeamFanVariantCount;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / (float)TeamFanAtlasColumns;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect FerroRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % FerroFanVariantCount;
            if (variant < 0) variant += FerroFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int outfitColumn = (variant % TeamFanAtlasColumns) * 2;
            int pairedPoseColumn = outfitColumn + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect TeamRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % TeamFanVariantCount;
            if (variant < 0) variant += TeamFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 3f;
            int pairedPoseColumn = (variant % TeamFanAtlasColumns) * 2 + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect IndependienteRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % IndependienteFanVariantCount;
            if (variant < 0) variant += IndependienteFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % TeamFanAtlasColumns) * 2 + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawRacingFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (racingFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, racingFans, RacingFanSourceRect(racingFans, customerId, walking), false, flip);
        }
        private void DrawSanLorenzoFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (sanLorenzoFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            // This paired 6x4 atlas shares the established twelve-variant outfit/pose grid.
            Draw(person, sanLorenzoFans, RacingFanSourceRect(sanLorenzoFans, customerId, walking), false, flip);
        }
        private void DrawBocaFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (bocaFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            // The Boca atlas shares the established paired 6x4 grid with twelve outfit variants.
            Draw(person, bocaFans, RacingFanSourceRect(bocaFans, customerId, walking), false, flip);
        }
        private void DrawRiverFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (riverFans == null) return;
            Draw(person, riverFans, RacingFanSourceRect(riverFans, RiverAtlasVariant(customerId), walking), false, flip);
        }
        private static int RiverAtlasVariant(int customerId)
        {
            int variant = ((customerId % RacingFanVariantCount) + RacingFanVariantCount) % RacingFanVariantCount;
            // River provenance uses top-to-bottom rows; reuse the existing bottom-origin atlas helper.
            return (3 - variant / 3) * 3 + variant % 3;
        }
        private static Rect RacingFanSourceRect(Texture2D atlas, int customerId, bool walking)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % RacingFanVariantCount;
            if (variant < 0) variant += RacingFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % 3) * 2 + (walking ? 1 : 0);
            int rowFromTop = variant / 3;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect RacingRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % RacingFanVariantCount;
            if (variant < 0) variant += RacingFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % 3) * 2 + (pose & 1);
            int rowFromTop = variant / 3;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static int SpecialistUpgradePortraitSourceLevel(int levelIndex)
        {
            // Keep Cocacolero/Fernetero upgrade-card portraits visually consistent across levels.
            return levelIndex >= 0 && levelIndex < StreetSimulation.LevelNames.Length ? 1 : 0;
        }
        private static bool UsesMatchedCocacoleroArt(int levelIndex)
        {
            // Chicago and later specialist levels share the same worker style and proportions.
            return levelIndex >= 1 && levelIndex < StreetSimulation.LevelNames.Length;
        }
        private Texture2D CocacoleroArtForLevel(int levelIndex)
        {
            return UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null ? laterCocacolero : cocacolero;
        }
        private Rect CocacoleroPoseForLevel(int levelIndex, int frame)
        {
            if (UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null)
            {
                return MatchedCocacoleroPose(frame, new Vector2(laterCocacolero.width, laterCocacolero.height));
            }
            return ChicagoCocacoleroPoses[Mathf.Clamp(frame, 0, ChicagoCocacoleroPoses.Length - 1)];
        }
        private static Rect MatchedCocacoleroPose(int frame, Vector2 importedSize)
        {
            Rect pose = MatchedCocacoleroPoses[Mathf.Clamp(frame, 0, MatchedCocacoleroPoses.Length - 1)];
            Vector2 scale = new Vector2(importedSize.x / 1247f, importedSize.y / 1261f);
            return new Rect(pose.x * scale.x, pose.y * scale.y, pose.width * scale.x, pose.height * scale.y);
        }
        internal static Rect[] WorkerFoodPoseBounds => ParrilleroPoses;
        internal static Rect[] WorkerDiagonalPoseBounds => ParrilleroDiagonalPoses;
        internal static Rect[] WorkerDrinkPoseBounds(bool matched) => matched ? MatchedCocacoleroPoses : ChicagoCocacoleroPoses;

        private void DrawWorker(StreetWorker w)
        {
            if (StreetWorkerProjection.IsObscuredIdleWorker(game.Sim.Workers, w, layoutVerticalScale, game.Sim.UsesActiveCooks)) return;
            bool moving=w.State==StreetWorkerState.ToStation||w.State==StreetWorkerState.ToCounter||w.State==StreetWorkerState.ToGrill||w.State==StreetWorkerState.ToTable;
            int levelIndex = game.Sim.LevelIndex;
            Texture2D cocacoleroArt = CocacoleroArtForLevel(levelIndex);
            bool beverageRole = w.Role == StreetWorkerRole.Cocacolero || w.Role == StreetWorkerRole.Fernetero;
            bool isCocacolero = beverageRole && cocacoleroArt != null;
            if (w.Role == StreetWorkerRole.Fernetero)
                cocacoleroArt = UsesMatchedCocacoleroArt(levelIndex) && fernetWorkerLater != null ? fernetWorkerLater : fernetWorker;
            Vector2 movement=w.Target-w.Position;
            StreetWorkerPose workerPose = StreetWorkerAnimation.Sample(w);
            if (w.State == StreetWorkerState.ToStation && StreetWorkerProjection.CocaBarrelPresentationOffset(game.Sim, w) > .01f)
                movement = new Vector2(0f, 1f);
            moving = workerPose.Moving;
            int frame=moving?SelectParrilleroWalkFrame(movement,workerPose.Frame):0;
            if(w.State==StreetWorkerState.Pickup||w.State==StreetWorkerState.Deposit)frame=beverageRole?0:11;
            if(w.State==StreetWorkerState.Handoff)frame=beverageRole?0:3;
            float bob=workerPose.Bob*1.875f;
            // Preserve the original90×98worker shape while anchoring its feet to the
            // same vertically adapted logical position as the native kitchen projection.
            float stationOffset = 98f * (layoutVerticalScale - 1f) / Mathf.Max(.01f, layoutVerticalScale);
            bob += stationOffset;
            Vector2 displayPosition = StreetWorkerProjection.PresentationPosition(game.Sim, w);
            float presentationScale = StreetWorkerProjection.PresentationScale(game.Sim, w);
            Rect workerRect=new Rect(displayPosition.x-45*presentationScale,displayPosition.y-98*presentationScale+bob*presentationScale,90*presentationScale,98*presentationScale);
            if(isCocacolero)
            {
                if(frame>=16)
                {
                    int phase=workerPose.Frame&1;
                    float x=Mathf.Abs(movement.x),y=Mathf.Abs(movement.y);
                    frame=x>y?9+phase:(movement.y>0?1+phase:5+phase);
                }
                bool flip=game.Sim.LevelIndex==1
                    ? (w.State == StreetWorkerState.Pickup ? (w.Product == 4
                        ? false
                        : !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium))
                        : ChicagoWorkerFlip(true,frame,movement,false))
                    : w.State == StreetWorkerState.Pickup ? (w.Product == 4
                        ? false
                        : !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium))
                        : moving && Mathf.Abs(movement.x)>Mathf.Abs(movement.y) && movement.x<0;
                bool laterStyle = UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null;
                Rect pose = CocacoleroPoseForLevel(levelIndex, frame);
                Draw(workerRect,cocacoleroArt,pose,laterStyle,flip);
            }
            else if (w.Role == StreetWorkerRole.ParrilleroPremium)
            {
                bool flip = w.State == StreetWorkerState.Pickup ? StreetWorkstationLayout.PickupReachesRight(w.Product, true)
                    : moving && Mathf.Abs(movement.x) > Mathf.Abs(movement.y) && movement.x > 0;
                Texture2D art = frame >= 16 ? premiumDiagonal : premiumWorker;
                Rect pose = frame >= 16 ? ParrilleroDiagonalPoses[frame-16] : ParrilleroPoses[frame];
                if (art != null) Draw(workerRect, art, pose, true, flip);
            }
            else if(frame>=16)ParrilleroDiagonal(workerRect,frame-16);
            else Parrillero(workerRect,frame,w.State == StreetWorkerState.Pickup
                ? StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium)
                : game.Sim.LevelIndex == 1 ? ChicagoWorkerFlip(false,frame,movement,false)
                : moving && Mathf.Abs(movement.x) > Mathf.Abs(movement.y) && movement.x > 0);
            // Carried food is a native independent object keyed by CarriedItemId, never a cosmetic icon.
        }
        private static bool ChicagoWorkerFlip(bool cocacolero, int frame, Vector2 movement, bool pickup)
        {
            // Existing pickup/reach poses are mirrored: food reaches right, Coca reaches left.
            // Cardinal walks retain their facing even during the final few pixels of the trip.
            return pickup || ((frame == 9 || frame == 10) && (cocacolero ? movement.x < 0 : movement.x > 0));
        }
        private static int SelectParrilleroWalkFrame(Vector2 movement,int gaitFrame)
        {
            int phase=gaitFrame&1;
            float x=Mathf.Abs(movement.x),y=Mathf.Abs(movement.y);
            if(x>0.01f&&y>0.01f&&Mathf.Min(x,y)>=Mathf.Max(x,y)*.30f)
            {
                int direction=movement.y>0?(movement.x<0?0:1):(movement.x<0?2:3);
                return 16+direction*2+phase;
            }
            if(x>y)return 9+phase;
            return movement.y<0?5+phase:1+phase;
        }
        private static Rect GrillRect(int productCount) => StreetWorkstationLayout.GrillBounds;
        private static Rect GrillRectForLevel(int productCount, int levelIndex) => StreetWorkstationLayout.GrillBounds;
        private void DrawGrill()
        {
            StreetSimulation sim = game.Sim;
            if (sim.HasRole(StreetWorkerRole.Parrillero))
            {
                // No premium meat artwork can be rendered on this grill.
                Texture2D normal = sim.IsProductAvailable(1) && velezGrill != null ? velezGrill
                    : sim.LevelIndex == 0 ? largeGrill : chicagoGrill;
                DrawGrillArtwork(StreetWorkstationLayout.GrillBounds, normal);
            }
            if (sim.HasParrilleroPremium && premiumGrill != null)
            {
                Rect drawn = DrawGrillArtwork(StreetWorkstationLayout.PremiumGrillBounds, premiumGrill);
                // Keep unavailable meat out of the grate while retaining the complete separate frame.
                if (!sim.IsProductAvailable(3))
                    DrawRaw(new Rect(drawn.x + drawn.width * .5f, drawn.y, drawn.width * .5f, drawn.height),
                        premiumGrill, new Rect(0,0,premiumGrill.width*.5f,premiumGrill.height),true,false);
                else if (!sim.IsProductAvailable(2))
                    DrawRaw(new Rect(drawn.x, drawn.y, drawn.width * .5f, drawn.height),
                        premiumGrill, new Rect(premiumGrill.width*.5f,0,premiumGrill.width*.5f,premiumGrill.height),true,false);
            }
        }
        private Rect DrawGrillArtwork(Rect bounds, Texture2D art)
        {
            if (art == null) { Item(bounds,7); return LayoutRect(bounds); }
            Rect drawn = StreetWorkstationLayout.FitArtwork(LayoutRect(bounds), new Vector2(art.width,art.height));
            GUI.DrawTexture(drawn,art,ScaleMode.StretchToFill,true);
            return drawn;
        }
        private static string StationLabelForProduct(int product)
        {
            return product == 4 ? "COCA 600 ml" : product == 6 ? "CERVEZA" : product == 5 ? "FERNET 1 L" : string.Empty;
        }
        private void DrawStation(int i, bool unlocked)
        {
            if (!unlocked) return;
            Rect station = StreetWorkstationLayout.BoundsForProduct(i,game.Sim.HasParrilleroPremium);
            if (i >= 4)
            {
                Texture2D art = i == 4 ? beverageBarrel : i == 6 ? beerBarrel : fernetTable;
                if (art != null) Draw(station, art, new Rect(0, 0, art.width, art.height), false, false);
            }
            if (i >= 4) return;
            if (game.Sim.LevelIndex == 0) return;
            // Labels/icons identify finished products on the one standard table, never on the cooking grate.
            float x = station.x + 25 + (i % 2) * 46;
            DrawProductIcon(new Rect(x - 17, station.y + 3, 34, 25), i);
        }
        private static bool ShouldDrawStationProductBadge(int productIndex)
        {
            // Product 0 is represented by the loaded choripán table;
            // a second floating badge above the grill is visually redundant.
            return productIndex != 0;
        }
        private void Upgrade(Rect r,int action,int icon,string name,int cost,bool affordable)
        {
            // Anchor the card responsively, but keep its internal typography/icons at one scale.
            float previousVertical = layoutVerticalScale;
            r.y *= previousVertical;
            layoutVerticalScale = 1f;
            bool available=affordable&&game.Sim.Phase==RoundPhase.Playing;
            Color old=GUI.color;GUI.color=available?Color.white:new Color(.57f,.57f,.57f,1);
            Rect drawn=r;if(lastAction==action&&Time.unscaledTime<feedbackUntil){drawn.width*=feedbackScale;drawn.height*=feedbackScale;drawn.center=r.center;}
            Texture2D glossyCard = GetGlossyUpgradeCardTexture(r);
            if (glossyCard != null) GUI.DrawTexture(drawn, glossyCard, ScaleMode.StretchToFill, true);
            else FillRect(drawn, new Color(.025f, .14f, .40f, 1f));
            GUI.color=old;
            if (available) DrawAffordableUpgradeBorder(r);
            bool compact = r.width < 200;
            int titleSize = upgradeTitle.fontSize;
            upgradeTitle.fontSize = r.height < 120 ? (action == HirePremiumAction ? 13 : compact ? 15 : 17)
                : compact ? 16 : 18;
            Rect titleBounds = UpgradeTitleBounds(drawn);
            upgradeTitle.fontSize = FitUpgradeStatusFontSize(upgradeTitle,name,titleBounds.width);
            UpgradeOutline(titleBounds,name,upgradeTitle,new Color(.98f,.99f,1f));
            upgradeTitle.fontSize = titleSize;
            string statusText = action == 3
                ? "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "  ·  +10%"
                : "EQUIPO " + game.Sim.WorkerCount(RoleForAction(action));
            if (compact && action == 3) statusText = "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "\n+10%";
            Rect statusBounds = action == 3 ? UpgradeSpeedStatusBounds(drawn) : UpgradeStatusBounds(drawn);
            int originalStatusSize = upgradePercent.fontSize;
            if (compact) upgradePercent.fontSize = action == 3 ? 12 : 13;
            upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, statusText, statusBounds.width);
            UpgradeOutline(statusBounds,statusText,upgradePercent,new Color(.55f,.90f,1f));
            upgradePercent.fontSize = originalStatusSize;
            if(action==3)
            {
                if(speedArrows!=null) DrawUpgradeOvalContent(drawn,speedArrows,SpeedArrowBounds);
            }
            else if((action==HireCocacoleroAction || action==HireFerneteroAction) && CocacoleroArtForLevel(SpecialistUpgradePortraitSourceLevel(game.Sim.LevelIndex))!=null)
            {
                int artLevel = SpecialistUpgradePortraitSourceLevel(game.Sim.LevelIndex);
                Texture2D art = action == HireFerneteroAction ? (UsesMatchedCocacoleroArt(artLevel) ? fernetWorkerLater : fernetWorker) : CocacoleroArtForLevel(artLevel);
                DrawUpgradeOvalContent(drawn,art,CocacoleroPoseForLevel(artLevel,0));
            }
            else if (action == HirePremiumAction && premiumWorker != null) DrawUpgradeOvalContent(drawn,premiumWorker,ParrilleroPoses[0]);
            else if (action == HireMozoAction && mozoWorker != null) DrawUpgradeOvalContent(drawn,mozoWorker,ParrilleroPoses[0]);
            else if (parrilleroIcon != null) DrawUpgradeOvalContent(drawn,parrilleroIcon,new Rect(22,6,1269,1188));
            else if (items != null) DrawUpgradeOvalContent(drawn,items,Items[icon]);
            string priceText=cost>0?cost.ToString():"MAX";
            if(cost>0)Item(UpgradeCoinBounds(drawn),10);
            int originalPriceSize=upgradePrice.fontSize;
            if (compact || r.height < 120) upgradePrice.fontSize = 24;
            upgradePrice.fontSize=FitUpgradePriceFontSize(upgradePrice,priceText,UpgradePriceTextBounds(drawn).width);
            UpgradeOutline(UpgradePriceTextBounds(drawn),priceText,upgradePrice,new Color(.98f,.99f,1f));
            upgradePrice.fontSize=originalPriceSize;
            bool prev=GUI.enabled;GUI.enabled=available;
            if(GUI.Button(LayoutRect(r),"",invisible))NativeAction(action);GUI.enabled=prev;
            layoutVerticalScale = previousVertical;
        }
        private void DrawAffordableUpgradeBorder(Rect card)
        {
            Texture2D border = GetAffordableUpgradeBorderTexture(card);
            if (border == null) return;
            float inset = AffordableUpgradeBorderPaddingPixels(Mathf.RoundToInt(card.width * 2f)) * .5f;
            Color previous = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(card.x - inset, card.y - inset,
                    card.width + inset * 2f, card.height + inset * 2f), border, ScaleMode.StretchToFill, true);
            }
            finally { GUI.color = previous; }
        }
        private Texture2D GetAffordableUpgradeBorderTexture(Rect card)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(card.width * 2f), 128, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(card.height * 2f), 96, 768);
            string key = width + "x" + height;
            if (affordableUpgradeBorderTextures.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            Texture2D created = CreateAffordableUpgradeBorderTexture(width, height);
            affordableUpgradeBorderTextures[key] = created;
            return created;
        }
        private static int AffordableUpgradeBorderPaddingPixels(int cardWidth) => cardWidth < 400 ? 10 : 14;
        private static Rect InflateUpgradeBorderRect(Rect rect, float amount) =>
            new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        private static float UpgradeBorderRingCoverage(float x, float y, Rect subject, float outerWidth, float innerWidth, float baseRadius)
        {
            Rect outer = InflateUpgradeBorderRect(subject, outerWidth);
            Rect inner = InflateUpgradeBorderRect(subject, innerWidth);
            float outerRadius = Mathf.Max(1f, baseRadius + outerWidth);
            float innerRadius = Mathf.Max(1f, baseRadius + innerWidth);
            return Mathf.Max(0f, RoundedCoverage(x, y, outer, outerRadius) - RoundedCoverage(x, y, inner, innerRadius));
        }
        private static Texture2D CreateAffordableUpgradeBorderTexture(int cardWidth, int cardHeight, bool keepReadable = false)
        {
            int padding = AffordableUpgradeBorderPaddingPixels(cardWidth);
            int width = cardWidth + padding * 2, height = cardHeight + padding * 2;
            bool compact = cardWidth < 400;
            float outerLine = compact ? 8f : 12f;
            float outerGap = compact ? 5f : 8f;
            float innerLine = compact ? 3f : 6f;
            float innerEdge = compact ? 0f : 3f;
            float radius = Mathf.Min(cardWidth, cardHeight) * .12f;
            var subject = new Rect(padding, padding, cardWidth, cardHeight);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Double white border for affordable upgrade cards " + cardWidth + "x" + cardHeight,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                Color color = Color.clear;
                color = Over(color, new Color(.96f, .99f, 1f, .98f),
                    UpgradeBorderRingCoverage(px, py, subject, outerLine, outerGap, radius));
                color = Over(color, Color.white,
                    UpgradeBorderRingCoverage(px, py, subject, innerLine, innerEdge, radius));
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private Texture2D GetGlossyUpgradeCardTexture(Rect card)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(card.width * 2f), 128, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(card.height * 2f), 96, 768);
            string key = width + "x" + height;
            if (glossyUpgradeCardTextures.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            Texture2D created = CreateGlossyUpgradeCardTexture(width, height);
            glossyUpgradeCardTextures[key] = created;
            return created;
        }
        private static Texture2D CreateGlossyUpgradeCardTexture(int width, int height, bool keepReadable = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado cyan-gloss upgrade card with circular medallion " + width + "x" + height,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            float radius = Mathf.Min(width, height) * .12f;
            var shadow = new Rect(2f, 4f, width - 4f, height - 7f);
            var outer = new Rect(1f, 1f, width - 2f, height - 5f);
            var rim = new Rect(3f, 3f, width - 6f, height - 9f);
            var face = new Rect(6f, 6f, width - 12f, height - 15f);
            var priceRim = new Rect(face.x + face.width * .49f, face.y + face.height * .64f,
                face.width * .49f, face.height * .30f);
            var priceFace = new Rect(priceRim.x + 2f, priceRim.y + 2f,
                Mathf.Max(1f, priceRim.width - 4f), Mathf.Max(1f, priceRim.height - 4f));
            float priceRadius = Mathf.Min(priceRim.height * .5f, radius * .52f);
            Vector2 medalCenter = new Vector2(face.x + face.width * .25f, face.center.y);
            float medalOuterRadius = face.height * .35f;
            float medalRimRadius = face.height * .32f;
            float medalInnerRadius = face.height * .285f;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / Mathf.Max(1f, face.height));
                Color color = new Color(.004f, .015f, .075f, .72f * RoundedCoverage(px, py, shadow, radius));
                color = Over(color, new Color(.008f, .035f, .15f), RoundedCoverage(px, py, outer, radius));
                color = Over(color, Color.Lerp(new Color(.48f, .94f, 1f), new Color(.02f, .27f, .82f), 1f - t),
                    RoundedCoverage(px, py, rim, Mathf.Max(2f, radius - 2f)));
                float faceCoverage = RoundedCoverage(px, py, face, Mathf.Max(2f, radius - 5f));
                Color faceColor = Color.Lerp(new Color(.045f, .42f, .92f), new Color(.012f, .16f, .62f), 1f - t);
                color = Over(color, faceColor, faceCoverage);
                float gloss = .20f * (1f - IntroEase((t - .56f) / .36f));
                color = Over(color, new Color(.76f, .96f, 1f), faceCoverage * gloss);

                float priceOuterCoverage = RoundedCoverage(px, py, priceRim, priceRadius);
                float priceInnerCoverage = RoundedCoverage(px, py, priceFace, Mathf.Max(1f, priceRadius - 2f));
                color = Over(color, new Color(.008f, .055f, .24f, .96f), priceOuterCoverage);
                Color priceColor = Color.Lerp(new Color(.06f, .66f, 1f), new Color(.015f, .28f, .80f), 1f - t);
                color = Over(color, priceColor, priceInnerCoverage);
                color = Over(color, new Color(.64f, .95f, 1f), priceInnerCoverage * .20f *
                    (1f - IntroEase((t - .62f) / .30f)));

                float medalDistance = Vector2.Distance(new Vector2(px, py), medalCenter);
                color = Over(color, new Color(.002f, .025f, .12f, .72f), CircleCoverage(px, py, medalCenter, medalOuterRadius + 4f));
                color = Over(color, new Color(.008f, .045f, .20f), CircleCoverage(px, py, medalCenter, medalOuterRadius));
                color = Over(color, new Color(.32f, .90f, 1f), CircleCoverage(px, py, medalCenter, medalRimRadius));
                float innerCoverage = CircleCoverage(px, py, medalCenter, medalInnerRadius);
                float medalT = Mathf.Clamp01((py - (medalCenter.y - medalInnerRadius)) / (2f * medalInnerRadius));
                Color medalFace = Color.Lerp(new Color(.035f, .35f, .82f), new Color(.008f, .105f, .40f), 1f - medalT);
                color = Over(color, medalFace, innerCoverage);
                float innerEdge = Mathf.Max(0f, innerCoverage - CircleCoverage(px, py, medalCenter, medalInnerRadius - 2.5f));
                color = Over(color, new Color(.82f, .98f, 1f), innerEdge * .9f);
                float glint = Mathf.Clamp01(1f - Mathf.Abs(medalDistance - medalOuterRadius * .73f) / 2.5f)
                    * Mathf.Clamp01((py - medalCenter.y) / Mathf.Max(1f, medalOuterRadius));
                color = Over(color, Color.white, glint * .52f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private static Rect UpgradeOvalBounds(Rect card)
        {
            float diameter = Mathf.Min(card.height * .68f, card.width * .43f);
            return new Rect(card.x + 8f, card.y + (card.height - diameter) * .5f, diameter, diameter);
        }
        private static Rect UpgradeOvalContentBounds(Rect card, Vector2 sourceSize)
        {
            Rect medallion = UpgradeOvalBounds(card);
            float inset = medallion.width * .10f;
            Rect inner = new Rect(medallion.x + inset, medallion.y + inset,
                medallion.width - inset * 2f, medallion.height - inset * 2f);
            float scale = Mathf.Min(inner.width / Mathf.Max(1f, sourceSize.x),
                inner.height / Mathf.Max(1f, sourceSize.y));
            Vector2 size = sourceSize * scale;
            return new Rect(medallion.center - size * .5f, size);
        }
        private void DrawUpgradeOvalContent(Rect card, Texture2D texture, Rect source)
        {
            if (texture != null) Draw(UpgradeOvalContentBounds(card,source.size),texture,source,true,false);
        }
        private static float UpgradeContentLeft(Rect card) => UpgradeOvalBounds(card).xMax + 6f;
        private static Rect UpgradeSpeedStatusBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 33f, Mathf.Max(0f, card.xMax - 8f - x), 28f);
        }
        private static Rect UpgradeTitleBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 5f, Mathf.Max(0f, card.xMax - 8f - x), 30f);
        }
        private static Rect UpgradeStatusBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 36f, Mathf.Max(0f, card.xMax - 8f - x), 23f);
        }
        private static Rect UpgradeCoinBounds(Rect card)
        {
            float x = UpgradeContentLeft(card) + 1f;
            return new Rect(x, card.y + card.height * .69f, 20f, 20f);
        }
        private static Rect UpgradePriceTextBounds(Rect card)
        {
            Rect coin = UpgradeCoinBounds(card);
            float x = coin.xMax + 2f;
            return new Rect(x, card.y + card.height * .65f, Mathf.Max(0f, card.xMax - 8f - x), card.height * .30f);
        }
        private static int FitUpgradePriceFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=style.CalcSize(new GUIContent(value)).x;
            return needed>maxWidth-2?Mathf.Max(14,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private static int FitUpgradeStatusFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=style.CalcSize(new GUIContent(value)).x;
            return needed>maxWidth-2?Mathf.Max(10,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private void UpgradeOutline(Rect bounds,string value,GUIStyle style,Color fill)
        {
            Color previous=style.normal.textColor;style.normal.textColor=new Color(.005f,.035f,.16f,1);
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=0||y!=0)
                Label(new Rect(bounds.x+x*1.25f,bounds.y+y*1.25f,bounds.width,bounds.height),value,style);
            style.normal.textColor=fill;Label(bounds,value,style);style.normal.textColor=previous;
        }
        private static float CircleCoverage(float x, float y, Vector2 center, float radius) =>
            Mathf.Clamp01(radius + .5f - Vector2.Distance(new Vector2(x, y), center));
        private void ReadyPanel(ResponsiveFooterLayout footer)
        {
            // Prices are fixed in every location; Ready has no price-selection panel or hidden controls.
            DrawStandardButton(Start, 1, "JUGAR");
            DrawLevelBadge(game.SelectedLevel, footer.LevelBadge);
            DrawStandardButton(footer.ReadySelector, ReadyLevelSelectorAction, "CANCHAS");
        }
        private Rect LevelOneTutorialPanelBounds() =>
            LevelOneTutorialPanelBounds(levelOneTutorialStep, LevelOneTutorialTargetBounds(), layoutVerticalScale);

        private static Rect LevelOneTutorialPanelBounds(int step, Rect target, float verticalScale)
        {
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = step == 0 ? target.center.x + 20f - width * .5f
                : step == 1 ? target.center.x + 30f
                : step == 2 ? target.center.x - width * .5f - 55f
                : target.center.x - width * .5f + 155f;
            x = Mathf.Clamp(x, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, verticalScale);
            float y = step < 2 ? target.yMax + 56f / vertical : target.yMin - 310f / vertical;
            return new Rect(x, y, width, LevelOneTutorialBubbleHeight);
        }

        private Rect LevelOneTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelOneTutorialPanelBounds(), layoutVerticalScale);
        private static Rect LevelOneTutorialPrimaryBounds(Rect panel, float verticalScale) =>
            new Rect(panel.center.x - 100f, panel.y + 126f / Mathf.Max(.01f, verticalScale), 200f, 58f);

        private Rect LevelOneTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelOneTutorialPanelBounds(), layoutVerticalScale);
        private static Rect LevelOneTutorialSkipBounds(Rect panel, float verticalScale) =>
            new Rect(panel.center.x - 42f, panel.y + 188f / Mathf.Max(.01f, verticalScale), 84f, 26f);

        private string LevelOneTutorialPrimaryCaption() => "SIGUIENTE";
        private Rect LevelOneTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            Rect gap = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            if (levelOneTutorialStep == 0)
            {
                int count = Mathf.Min(game.Sim.ProductCount, hudQuotaProductIds.Length);
                for (int slot = 0; slot < count; slot++)
                    hudQuotaProductIds[slot] = game.Sim.GetAvailableProduct(slot);
                hudQuotaProductCount = count;
                return HudScreenRectToSafeCanvas(HudQuotaCellBounds(0, hudQuotaProductIds, count, gap));
            }
            if (levelOneTutorialStep == 1)
                return HudScreenRectToSafeCanvas(HudCoinIconBounds(gap));
            return ResponsiveUpgradeCardBounds(game.Sim, levelOneTutorialStep == 2 ? 2 : 3);
        }
        private Rect HudScreenRectToSafeCanvas(Rect screenLogicalBounds)
        {
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Rect viewport = CanvasViewport(screen, Screen.safeArea);
            if (viewport.width <= 0f || screenLogicalBounds.width <= 0f || screenLogicalBounds.height <= 0f) return Rect.zero;
            float screenScale = Screen.width / W;
            Vector2 topLeft = ScreenToLayoutPoint(
                new Vector2(screenLogicalBounds.x * screenScale, Screen.height - screenLogicalBounds.y * screenScale), screen, viewport);
            Vector2 bottomRight = ScreenToLayoutPoint(
                new Vector2(screenLogicalBounds.xMax * screenScale, Screen.height - screenLogicalBounds.yMax * screenScale), screen, viewport);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }
        private void DrawLevelOneTutorial()
        {
            // Dim the entire physical screen behind the guide (including the HUD and safe-area insets).
            // DrawIntroScreen restores the safe-area matrix; the bubble and controls below stay crisp.
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelOneTutorialPanelBounds();
            Rect target = LevelOneTutorialTargetBounds();
            bool targetAbove = levelOneTutorialStep < 2;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float tailWidth = levelOneTutorialStep == 0 ? 52f : levelOneTutorialStep == 1 ? 42f : 36f;
            float baseX = levelOneTutorialStep == 2 ? panel.xMax - 30f
                : levelOneTutorialStep == 3 ? panel.x + 30f : target.center.x;
            baseX = Mathf.Clamp(baseX, panel.x + tailWidth * .5f,
                panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, targetAbove ? target.yMax : target.yMin);
            // LayoutRect scales anchor positions (not element sizes) vertically. Rasterize the
            // complete silhouette in that same rendered coordinate space so the tail stays
            // attached while its gap remains constant across portrait heights.
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelOneTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) ||
                !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelOneTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);

            string headline = LevelOneTutorialHeadlines[levelOneTutorialStep];
            string explanation = LevelOneTutorialExplanations[levelOneTutorialStep];
            Rect headlineBounds = new Rect(panel.x + 18f,
                panel.y + (levelOneTutorialStep == 1 ? 29f : 12f) / vertical,
                panel.width - 36f, levelOneTutorialStep == 1 ? 70f : 58f);
            DrawTutorialText(headlineBounds, headline, levelOneTutorialTitleStyle,
                levelOneTutorialStep == 2 ? 23 : 25, 20);
            if (!string.IsNullOrEmpty(explanation))
            {
                Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 35f);
                if (levelOneTutorialStep == 3) explanationBounds.height = 40f;
                DrawTutorialText(explanationBounds, explanation, levelOneTutorialBodyStyle, 20, 17);
            }

            DrawStandardButton(LevelOneTutorialPrimaryBounds(panel, layoutVerticalScale), LevelOneTutorialPrimaryAction,
                LevelOneTutorialPrimaryCaption());
            Rect skipBounds = LevelOneTutorialSkipBounds(panel, layoutVerticalScale);
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private void DrawTutorialText(Rect bounds, string value, GUIStyle style, int preferredSize, int minimumSize)
        {
            int previous = style.fontSize;
            try
            {
                style.fontSize = preferredSize;
                while (style.fontSize > minimumSize && style.CalcHeight(new GUIContent(value), bounds.width) > bounds.height)
                    style.fontSize--;
                Rect drawn = LayoutRect(bounds);
                if (style == levelOneTutorialTitleStyle || style == levelOneTutorialBodyStyle)
                {
                    const float weight = .55f;
                    GUI.Label(new Rect(drawn.x - weight, drawn.y, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x + weight, drawn.y, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x, drawn.y - weight, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x, drawn.y + weight, drawn.width, drawn.height), value, style);
                }
                GUI.Label(drawn, value, style);
            }
            finally { style.fontSize = previous; }
        }

        private static Rect LevelOneTutorialBubbleDrawBounds(Rect panel, Vector2 tip, float baseX, float tailWidth, bool tailAbove)
        {
            float baseY = tailAbove ? panel.y : panel.yMax;
            return Rect.MinMaxRect(
                Mathf.Min(panel.x, tip.x, baseX - tailWidth * .5f),
                Mathf.Min(panel.y, tip.y, baseY),
                Mathf.Max(panel.xMax, tip.x, baseX + tailWidth * .5f),
                Mathf.Max(panel.yMax, tip.y, baseY));
        }

        private static Texture2D CreateLevelOneTutorialBubbleTexture(Rect panel, Vector2 tip, float baseX,
            float tailWidth, bool tailAbove)
        {
            const float rasterScale = 3f;
            const float radius = 18f, border = 5f;
            Rect bounds = LevelOneTutorialBubbleDrawBounds(panel, tip, baseX, tailWidth, tailAbove);
            int width = Mathf.Max(1, Mathf.CeilToInt(bounds.width * rasterScale));
            int height = Mathf.Max(1, Mathf.CeilToInt(bounds.height * rasterScale));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "First match unified cream speech bubble and pointer",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Color outline = new Color(.22f, .075f, .025f, 1f);
            Color face = new Color(1f, .94f, .80f, 1f);
            float edgeY = tailAbove ? panel.y : panel.yMax;
            Vector2 baseCenter = new Vector2(baseX, edgeY);
            Vector2 leftBase = new Vector2(baseX - tailWidth * .5f, edgeY);
            Vector2 rightBase = new Vector2(baseX + tailWidth * .5f, edgeY);
            Rect innerPanel = Rect.MinMaxRect(panel.x + border, panel.y + border,
                panel.xMax - border, panel.yMax - border);
            float innerRadius = Mathf.Max(1f, radius - border);
            float innerTailWidth = Mathf.Max(4f, tailWidth - border * 2f);
            float innerBaseY = tailAbove ? panel.y + border : panel.yMax - border;
            Vector2 innerTip = Vector2.MoveTowards(tip, baseCenter, border);
            Vector2 innerLeftBase = new Vector2(baseX - innerTailWidth * .5f, innerBaseY);
            Vector2 innerRightBase = new Vector2(baseX + innerTailWidth * .5f, innerBaseY);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(bounds.x + (x + .5f) / rasterScale,
                    bounds.yMax - (y + .5f) / rasterScale);
                bool insideOuter = TutorialBubbleRoundedRectContains(point, panel, radius) ||
                    TutorialBubbleTriangleContains(point, tip, leftBase, rightBase);
                if (!insideOuter) continue;
                bool insideFace = TutorialBubbleRoundedRectContains(point, innerPanel, innerRadius) ||
                    TutorialBubbleTriangleContains(point, innerTip, innerLeftBase, innerRightBase);
                pixels[y * width + x] = insideFace ? face : outline;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static bool TutorialBubbleRoundedRectContains(Vector2 point, Rect bounds, float radius)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return false;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(bounds.width, bounds.height) * .5f);
            float qx = Mathf.Abs(point.x - bounds.center.x) - (bounds.width * .5f - radius);
            float qy = Mathf.Abs(point.y - bounds.center.y) - (bounds.height * .5f - radius);
            float outsideX = Mathf.Max(0f, qx), outsideY = Mathf.Max(0f, qy);
            float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return distance <= 0f;
        }

        private static bool TutorialBubbleTriangleContains(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float ab = (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
            float bc = (point.x - c.x) * (b.y - c.y) - (b.x - c.x) * (point.y - c.y);
            float ca = (point.x - a.x) * (c.y - a.y) - (c.x - a.x) * (point.y - a.y);
            bool negative = ab < 0f || bc < 0f || ca < 0f;
            bool positive = ab > 0f || bc > 0f || ca > 0f;
            return !(negative && positive);
        }
        private static Rect LevelButton(int i){return new Rect(108+i*67,912,57,35);}
        private static Rect VictoryPopupBounds(float canvasHeight, bool chulengo = false)
        {
            float topHudReserve = chulengo ? 224f : 110f;
            const float screenInset = 16f;
            float resultButtonTopInset = chulengo ? 123f : 72f;
            const float resultButtonGap = 8f;
            // Chulengo reserves space for three lower actions and keeps the status HUD visible above the crest.
            float availableHeight = Mathf.Max(1f, canvasHeight - topHudReserve - screenInset - resultButtonTopInset - resultButtonGap);
            float horizontalInset = chulengo ? 32f : 24f;
            float scale = Mathf.Min((W - horizontalInset) / VictoryPopupArtSize.x, availableHeight / VictoryPopupArtSize.y);
            Vector2 size = VictoryPopupArtSize * scale;
            float y = topHudReserve + screenInset + (availableHeight - size.y) * .5f;
            return new Rect((W - size.x) * .5f, y, size.x, size.y);
        }
        private static Rect VictorySlot(Rect frame, Rect normalized) => new Rect(
            frame.x + normalized.x * frame.width, frame.y + normalized.y * frame.height,
            normalized.width * frame.width, normalized.height * frame.height);

        private Texture2D ClubColoredVictoryPopup(int levelIndex)
        {
            if (victoryPopup == null) return null;
            if (clubColoredVictoryPopupLevel == levelIndex)
                return clubColoredVictoryPopup != null ? clubColoredVictoryPopup : victoryPopup;

            if (clubColoredVictoryPopup != null) Destroy(clubColoredVictoryPopup);
            clubColoredVictoryPopup = CreateClubColoredVictoryPopup(victoryPopup, ClubVisualTheme.ForLevel(levelIndex));
            clubColoredVictoryPopupLevel = levelIndex;
            return clubColoredVictoryPopup != null ? clubColoredVictoryPopup : victoryPopup;
        }

        private static Texture2D CreateClubColoredVictoryPopup(Texture2D source, ClubVisualTheme theme, bool keepReadable = false)
        {
            if (source == null || theme == null || !source.isReadable) return null;

            Color32[] pixels;
            try { pixels = source.GetPixels32(); }
            catch (UnityException) { return null; }

            int width = source.width, height = source.height;
            Color32 primary = theme.PrimaryColor, secondary = theme.SecondaryColor;
            int changed = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!IsChulengoFlagPixel(x, y, width, height)) continue;
                int index = y * width + x;
                Color32 sourcePixel = pixels[index];
                if (sourcePixel.a < 48) continue;

                int min = Mathf.Min(sourcePixel.r, Mathf.Min(sourcePixel.g, sourcePixel.b));
                int max = Mathf.Max(sourcePixel.r, Mathf.Max(sourcePixel.g, sourcePixel.b));
                // The poles and gold finials are warm-colored, while the two cloth flags are grayscale.
                if (max - min > 48) continue;

                float luminance = (sourcePixel.r * .299f + sourcePixel.g * .587f + sourcePixel.b * .114f) / 255f;
                Color32 band = ChulengoFlagBandColor(luminance, primary, secondary);
                float shade = luminance < .5f
                    ? Mathf.Lerp(.72f, .96f, Mathf.Clamp01(luminance * 2f))
                    : Mathf.Lerp(.90f, 1f, Mathf.Clamp01((luminance - .5f) * 2f));
                pixels[index] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.r * shade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.g * shade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(band.b * shade), 0, 255), sourcePixel.a);
                changed++;
            }

            if (changed == 0) return null;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Chulengo victory popup - " + theme.ClubName,
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };
            result.SetPixels32(pixels);
            result.Apply(false, !keepReadable);
            return result;
        }

        private static bool IsChulengoFlagPixel(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0) return false;
            float u = (x + .5f) / width, v = (y + .5f) / height;
            if (v < .87f || v > .992f) return false;
            return u >= .15f && u <= .305f || u >= .695f && u <= .85f;
        }

        private static Color32 ChulengoFlagBandColor(float luminance, Color32 primary, Color32 secondary)
        {
            float primaryLuminance = primary.r * .299f + primary.g * .587f + primary.b * .114f;
            float secondaryLuminance = secondary.r * .299f + secondary.g * .587f + secondary.b * .114f;
            bool primaryIsDarker = primaryLuminance <= secondaryLuminance;
            bool sourceIsLightStripe = luminance >= .5f;
            if (Mathf.Abs(primaryLuminance - secondaryLuminance) < 8f)
                return sourceIsLightStripe ? secondary : primary;
            return sourceIsLightStripe == primaryIsDarker ? secondary : primary;
        }
        private Rect ChulengoMultiplyBounds() => ChulengoMultiplyBoundsForFrame(VictoryPopupBounds(logicalCanvasHeight, true));
        // Public for focused geometry checks; pointer and visible control share this normalized frame.
        public static Rect ChulengoMultiplyBoundsForFrame(Rect frame) => VictorySlot(frame, new Rect(.20f, .935f, .60f, .095f));
        public static int ChulengoSectorAtPosition(float position) => StreetChulengo.SectorAtPosition(position);
        private static readonly string[] ChulengoLabels = { "×0", "×1,5", "×1", "×2", "×1", "×1,5", "×0" };
        private static readonly Color[] ChulengoColors = {
            new Color(.76f, .12f, .055f), new Color(.94f, .78f, .15f), new Color(.12f, .46f, .82f),
            new Color(.96f, .64f, .08f), new Color(.12f, .46f, .82f), new Color(.94f, .78f, .15f),
            new Color(.76f, .12f, .055f)
        };
        private static Texture2D CreateChulengoMeterTexture()
        {
            const int width = 1152, height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo forged ember meter", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color clear = new Color(0, 0, 0, 0);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            const float cx = 576f, cy = 30f, outerX = 554f, outerY = 326f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = x + .5f - cx, dy = y + .5f - cy;
                    if (dy < 0f) continue;
                    float outerRadius = Mathf.Sqrt(dx * dx / (outerX * outerX) + dy * dy / (outerY * outerY));
                    if (outerRadius > 1f) continue;

                    Color pixel;
                    if (outerRadius < .695f)
                    {
                        // The transparent inner bowl reveals the authored coal-and-choripan art beneath the live sectors.
                        pixel = clear;
                    }
                    else if (outerRadius < .735f || outerRadius > .947f)
                    {
                        float shine = .5f + .5f * Mathf.Sin(dy * .047f + dx * .011f);
                        if (outerRadius > .947f)
                        {
                            // A dark forged outer rim reads like the charcoal steel bezel in the reference.
                            float highlight = Mathf.Clamp01(.14f + shine * .32f);
                            pixel = Color.Lerp(new Color(.055f, .052f, .048f), new Color(.50f, .42f, .29f), highlight);
                        }
                        else
                        {
                            float rim = Mathf.InverseLerp(.695f, .735f, outerRadius);
                            pixel = Color.Lerp(new Color(.12f, .09f, .075f), new Color(.83f, .68f, .38f), Mathf.Clamp01(.23f + shine * .62f));
                            if (rim > .55f && (x % 19 == 0 || y % 19 == 0)) pixel = Color.Lerp(pixel, new Color(.98f, .84f, .56f), .34f);
                        }
                    }
                    else
                    {
                        // Equal angular slices keep the visible pointer angle identical to gameplay's sector math.
                        float angle = Mathf.Atan2(dy, dx);
                        float position = Mathf.Clamp01(1f - angle / Mathf.PI);
                        int sector = Mathf.Min(6, Mathf.FloorToInt(position * 7f));
                        float edge = Mathf.Min(position * 7f - sector, sector + 1f - position * 7f);
                        if (edge < .022f) pixel = new Color(.08f, .045f, .025f);
                        else
                        {
                            float radialLight = .72f + .24f * Mathf.Sin((outerRadius - .735f) / .212f * Mathf.PI);
                            float grainNoise = Mathf.PerlinNoise(x * .018f, y * .033f);
                            pixel = ChulengoColors[sector] * Mathf.Clamp(radialLight + (grainNoise - .5f) * .15f, .58f, 1.08f);
                            if (outerRadius > .91f) pixel = Color.Lerp(pixel, new Color(.13f, .07f, .04f), Mathf.InverseLerp(.91f, .947f, outerRadius) * .7f);
                        }
                    }

                    // Six small brass rivets lock the two forged rims together.
                    float angleAtRivet = Mathf.Atan2(dy / outerY, dx / outerX);
                    float rivetPosition = Mathf.Clamp01(1f - angleAtRivet / Mathf.PI);
                    int rivet = Mathf.RoundToInt(rivetPosition * 6f);
                    float rivetAngle = Mathf.PI * (1f - rivet / 6f);
                    float boltX = cx + Mathf.Cos(rivetAngle) * outerX * .972f;
                    float boltY = cy + Mathf.Sin(rivetAngle) * outerY * .972f;
                    float bolt = Mathf.Sqrt((x + .5f - boltX) * (x + .5f - boltX) + (y + .5f - boltY) * (y + .5f - boltY));
                    if (bolt < 13f && outerRadius >= .695f) pixel = bolt < 8.1f ? new Color(.95f, .67f, .19f) : new Color(.10f, .065f, .04f);
                    pixels[y * width + x] = pixel;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private const int ChulengoNeedleThicknessScale = 3;
        private static float ChulengoNeedleHalfWidthAt(float t)
        {
            const float currentBaseHalfWidth = 18f + 1.2f;
            const float currentTipHalfWidth = 1.2f;
            return Mathf.Lerp(currentBaseHalfWidth * ChulengoNeedleThicknessScale, currentTipHalfWidth, Mathf.Clamp01(t));
        }

        private static float ChulengoNeedleDisplayHeight(float meterHeight) =>
            Mathf.Max(10f, meterHeight * .16f) * ChulengoNeedleThicknessScale;

        private static Texture2D CreateChulengoNeedleTexture()
        {
            const int width = 512, height = 64 * ChulengoNeedleThicknessScale, tip = 494;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo red pointer", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float t = Mathf.Clamp01((x - 8f) / (tip - 8f));
                    float half = ChulengoNeedleHalfWidthAt(t);
                    float distance = Mathf.Abs(y + .5f - height * .5f);
                    if (x < 8 || x > tip || distance > half + 2f) pixels[y * width + x] = new Color(0, 0, 0, 0);
                    else if (distance > half) pixels[y * width + x] = new Color(.09f, .035f, .025f, 1f);
                    else if (distance < half * .26f) pixels[y * width + x] = new Color(1f, .55f, .36f, 1f);
                    else pixels[y * width + x] = new Color(.83f, .08f, .045f, 1f);
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static Texture2D CreateChulengoHubTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Chulengo steel pivot", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f - size * .5f) / (size * .5f), dy = (y + .5f - size * .5f) / (size * .5f);
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color pixel = new Color(0, 0, 0, 0);
                    if (radius <= 1f)
                    {
                        if (radius > .84f) pixel = new Color(.10f, .065f, .035f);
                        else if (radius > .68f) pixel = Color.Lerp(new Color(.95f, .71f, .23f), new Color(.44f, .24f, .055f), radius);
                        else
                        {
                            float light = Mathf.Clamp01(.62f + .28f * (-dy) + .16f * Mathf.Sin((dx + dy) * 8f));
                            pixel = Color.Lerp(new Color(.20f, .23f, .25f), new Color(.88f, .91f, .90f), light);
                        }
                    }
                    pixels[y * size + x] = pixel;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static Texture2D CreateChulengoParchmentTexture()
        {
            const int width = 512, height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Chulengo textured parchment panel", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    float cornerX = Mathf.Max(0f, 16f - Mathf.Min(x, width - 1 - x));
                    float cornerY = Mathf.Max(0f, 16f - Mathf.Min(y, height - 1 - y));
                    bool roundedCorner = cornerX * cornerX + cornerY * cornerY > 16f * 16f;
                    if (roundedCorner) { pixels[y * width + x] = new Color(0, 0, 0, 0); continue; }
                    float mottling = Mathf.PerlinNoise(x * .026f, y * .031f);
                    float cloudy = Mathf.PerlinNoise(x * .008f, y * .012f);
                    float fineGrain = Mathf.PerlinNoise(x * .14f, y * .16f);
                    float fibers = Mathf.Sin(x * 1.71f + y * .91f) * Mathf.Sin(x * .17f - y * .27f) * .038f;
                    float shade = Mathf.Clamp01(.56f + (mottling - .5f) * .84f + (cloudy - .5f) * .92f + (fineGrain - .5f) * .22f + fibers);
                    Color paper = Color.Lerp(new Color(.76f, .53f, .28f), new Color(1f, .95f, .78f), shade);
                    if (fineGrain > .78f && mottling < .48f) paper = Color.Lerp(paper, new Color(.64f, .37f, .18f), .17f);
                    if (edge < 3f) paper = new Color(.20f, .075f, .025f);
                    else if (edge < 6f) paper = new Color(.71f, .39f, .12f);
                    else if (edge < 9f) paper = new Color(.99f, .84f, .54f);
                    pixels[y * width + x] = paper;
                }
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
        private static Rect FitChulengoMeter(Rect container)
        {
            const float aspect = 3f;
            float height = Mathf.Min(container.height, container.width / aspect);
            float width = height * aspect;
            return new Rect(container.center.x - width * .5f, container.center.y - height * .5f, width, height);
        }
        private void DrawChulengoMeter(Rect container, bool locked, int selectedSector, int labelFontSize)
        {
            Rect bounds = FitChulengoMeter(container);
            float pivotY = bounds.yMax - bounds.height * (30f / 384f);
            if (chulengoBowl != null)
            {
                float bowlWidth = bounds.width * .69f;
                float bowlHeight = bounds.height * .69f;
                Rect bowl = new Rect(bounds.center.x - bowlWidth * .5f, pivotY - bowlHeight, bowlWidth, bowlHeight);
                GUI.DrawTexture(bowl, chulengoBowl, ScaleMode.StretchToFill, true);
            }
            if (chulengoMeter != null) GUI.DrawTexture(bounds, chulengoMeter, ScaleMode.StretchToFill, true);
            if (!locked || selectedSector >= 0)
            {
                float position = locked ? game.ChulengoSelectedPosition : StreetChulengo.PositionAtTime(Time.unscaledTime);
                Vector2 center = new Vector2(bounds.center.x, bounds.yMax - bounds.height * (30f / 384f));
                float rotation = 180f * position - 180f;
                Color old = GUI.color;
                GUI.color = Color.white;
                Matrix4x4 matrix = GUI.matrix;
                try
                {
                    // Rotate in this local canvas before the outer viewport scale is applied.
                    // The artwork's pointed ray is anchored at the exact saved needle position.
                    GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(center.x, center.y, 0f))
                        * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation))
                        * Matrix4x4.Translate(new Vector3(-center.x, -center.y, 0f));
                    // Follow the elliptical rim so the pointer reaches the same visual distance
                    // at the center and at either end of the seven-sector arc.
                    float angle = Mathf.PI * (1f - position);
                    float outerX = bounds.width * (554f / 1152f);
                    float outerY = bounds.height * (326f / 384f);
                    float length = .96f / Mathf.Sqrt(Mathf.Cos(angle) * Mathf.Cos(angle) / (outerX * outerX)
                        + Mathf.Sin(angle) * Mathf.Sin(angle) / (outerY * outerY));
                    // Triple canvas and display thickness together to widen the base without enlarging the tip.
                    float needleHeight = ChulengoNeedleDisplayHeight(bounds.height);
                    Rect needle = new Rect(center.x, center.y - needleHeight * .5f, length, needleHeight);
                    if (chulengoNeedle != null) GUI.DrawTexture(needle, chulengoNeedle, ScaleMode.StretchToFill, true);
                }
                finally { GUI.matrix = matrix; GUI.color = old; }
            }
            Vector2 pivot = new Vector2(bounds.center.x, pivotY);
            float hubSize = Mathf.Max(28f, bounds.height * .31f);
            if (chulengoHub != null)
                GUI.DrawTexture(new Rect(pivot.x - hubSize * .5f, pivot.y - hubSize * .5f, hubSize, hubSize), chulengoHub, ScaleMode.StretchToFill, true);
            for (int i = 0; i < ChulengoLabels.Length; i++)
            {
                float position = (i + .5f) / ChulengoLabels.Length;
                float angle = Mathf.PI * (1f - position);
                float outerX = bounds.width * (554f / 1152f), outerY = bounds.height * (326f / 384f);
                float rayLength = .84f / Mathf.Sqrt(Mathf.Cos(angle) * Mathf.Cos(angle) / (outerX * outerX)
                    + Mathf.Sin(angle) * Mathf.Sin(angle) / (outerY * outerY));
                Vector2 label = new Vector2(pivot.x + rayLength * Mathf.Cos(angle), pivot.y - rayLength * Mathf.Sin(angle));
                VictoryText(new Rect(label.x - 36f, label.y - 21f, 72f, 42f), ChulengoLabels[i], labelFontSize, new Color(1f, .97f, .86f), TextAnchor.MiddleCenter);
            }
        }
        private Rect VictoryExitBounds() => VictorySlot(VictoryPopupBounds(logicalCanvasHeight, game != null && game.ShowChulengoResult), VictoryExit);
        private static string FormatVictoryTime(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        private static int FitVictoryFont(GUIStyle style, string value, int preferred, Rect bounds)
        {
            int original = style.fontSize;
            style.fontSize = preferred;
            Vector2 needed = style.CalcSize(new GUIContent(value));
            style.fontSize = original;
            float scale = Mathf.Min(1f, Mathf.Max(1f, bounds.width - 8f) / Mathf.Max(1f, needed.x),
                Mathf.Max(1f, bounds.height - 6f) / Mathf.Max(1f, needed.y));
            return Mathf.Max(12, Mathf.FloorToInt(preferred * scale));
        }
        private void VictoryText(Rect bounds, string value, int preferred, Color fill, TextAnchor alignment)
        {
            // Keep popup/cover styles isolated and avoid inherited white hover on static summary labels.
            int fontSize = FitVictoryFont(menuTitle, value, preferred, bounds);
            var captionStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize, fill);
            captionStyle.alignment = alignment;
            var borderStyle = CreateStandardButtonLabelStyle(captionStyle, fontSize, new Color(.08f, .035f, .018f, .98f));
            float offset = preferred >= 32 ? 2f : .65f;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x * offset, bounds.y + y * offset, bounds.width, bounds.height), value, borderStyle);
            Label(bounds, value, captionStyle);
        }

        private static string FormatChulengoMoney(long cents)
        {
            long whole = cents / 100L;
            string grouped = whole.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
            string amount = "$" + grouped;
            long remainder = cents % 100L;
            return remainder == 0L ? amount : amount + "," + remainder.ToString("00");
        }

        private void DrawChulengoPaper(Rect frame)
        {
            // Keep the original bolt plates visible while expanding the authored parchment into the reference's reward-card hierarchy.
            // The legacy popup has a baked green CTA at the bottom; extend the paper behind the replacement blue action to hide its exposed edge.
            Rect paper = VictorySlot(frame, new Rect(.07f, .31f, .86f, .64f));
            if (chulengoParchment != null) GUI.DrawTexture(paper, chulengoParchment, ScaleMode.StretchToFill, true);
            else FillRect(paper, new Color(.98f, .89f, .68f));
        }

        private void DrawChulengoHero(Rect frame, bool pending, bool lockedRetry, int sector)
        {
            Color cream = new Color(1f, .94f, .78f);
            float fontScale = frame.width / 516f;
            VictoryText(VictorySlot(frame, new Rect(.39f, .315f, .54f, .086f)), "CHULENGO",
                Mathf.RoundToInt(82f * fontScale), cream, TextAnchor.MiddleCenter);

            Rect cook = VictorySlot(frame, new Rect(.075f, .345f, .31f, .255f));
            if (parrilleroIcon != null) GUI.DrawTexture(cook, parrilleroIcon, ScaleMode.ScaleToFit, true);

            string label;
            long reward;
            if (pending)
            {
                label = "PREMIO DEL TURNO";
                reward = game.ChulengoBaseRewardCents;
            }
            else if (lockedRetry && sector >= 0)
            {
                label = "RESULTADO FIJO: " + ChulengoLabels[Mathf.Clamp(sector, 0, 6)];
                reward = game.ChulengoFinalRewardCents;
            }
            else if (lockedRetry)
            {
                label = "COBRO PENDIENTE";
                reward = game.ChulengoBaseRewardCents;
            }
            else if (sector >= 0)
            {
                label = "CHULENGO " + ChulengoLabels[Mathf.Clamp(sector, 0, 6)];
                reward = game.ChulengoFinalRewardCents;
            }
            else
            {
                label = "PREMIO COBRADO";
                reward = game.ChulengoFinalRewardCents;
            }

            Color resultColor = sector == 0 || sector == 6 ? new Color(.75f, .075f, .035f) : cream;
            if (pending || lockedRetry && sector < 0) resultColor = cream;
            VictoryText(VictorySlot(frame, new Rect(.535f, .405f, .385f, .047f)), label,
                Mathf.RoundToInt(20f * fontScale), resultColor, TextAnchor.MiddleCenter);
            if (gameCoin != null)
            {
                GUI.DrawTexture(VictorySlot(frame, new Rect(.405f, .435f, .075f, .075f)), gameCoin, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(VictorySlot(frame, new Rect(.435f, .415f, .085f, .088f)), gameCoin, ScaleMode.ScaleToFit, true);
                GUI.DrawTexture(VictorySlot(frame, new Rect(.455f, .445f, .075f, .077f)), gameCoin, ScaleMode.ScaleToFit, true);
            }
            VictoryText(VictorySlot(frame, new Rect(.56f, .455f, .37f, .088f)), FormatChulengoMoney(reward),
                Mathf.RoundToInt(80f * fontScale), sector == 0 || sector == 6 ? new Color(.75f, .075f, .035f) : new Color(1f, .69f, .14f), TextAnchor.MiddleCenter);
        }

        private void DrawChulengoFooter(Rect frame, bool pending, bool lockedRetry, int sector)
        {
            float fontScale = frame.width / 516f;
            Color brown = new Color(.20f, .055f, .02f);
            Color cream = new Color(1f, .94f, .78f);
            if (pending)
            {
                long baseReward = game.ChulengoBaseRewardCents;
                long maximum = baseReward <= long.MaxValue / 2L ? baseReward * 2L : long.MaxValue;
                VictoryText(VictorySlot(frame, new Rect(.12f, .748f, .76f, .039f)),
                    "FRENÁ LA AGUJA EN LA ZONA DORADA", Mathf.RoundToInt(22f * fontScale), cream, TextAnchor.MiddleCenter);
                VictoryText(VictorySlot(frame, new Rect(.14f, .792f, .36f, .064f)),
                    "HASTA ×2:", Mathf.RoundToInt(44f * fontScale), cream, TextAnchor.MiddleRight);
                VictoryText(VictorySlot(frame, new Rect(.535f, .788f, .38f, .072f)),
                    FormatChulengoMoney(maximum), Mathf.RoundToInt(68f * fontScale), new Color(1f, .69f, .14f), TextAnchor.MiddleLeft);
                VictoryText(VictorySlot(frame, new Rect(.10f, .87f, .80f, .037f)),
                    "¡EN ×0 PERDÉS TODO EL PREMIO DEL TURNO!", Mathf.RoundToInt(22f * fontScale),
                    new Color(.78f, .07f, .035f), TextAnchor.MiddleCenter);
            }
            else if (lockedRetry)
            {
                VictoryText(VictorySlot(frame, new Rect(.15f, .75f, .70f, .043f)),
                    sector < 0 ? "EL COBRO SE REINTENTA SIN PERDER EL PREMIO" : "RESULTADO GUARDADO · NO SE VUELVE A MULTIPLICAR",
                    Mathf.RoundToInt(13f * fontScale), new Color(.68f, .10f, .035f), TextAnchor.MiddleCenter);
            }
            else
            {
                VictoryText(VictorySlot(frame, new Rect(.15f, .75f, .70f, .043f)),
                    "PREMIO FINAL: " + FormatMarketMoney(game.ChulengoFinalRewardCents),
                    Mathf.RoundToInt(20f * fontScale), brown, TextAnchor.MiddleCenter);
            }
        }
        private static readonly string[] ProductShortNames = { "CHO", "PAT", "BON", "VAC", "COC", "FER", "CER" };

        private static string FormatQuotaSummary(StreetSimulation sim, bool pendingOnly)
        {
            var entries = new System.Collections.Generic.List<string>(sim.ProductCount);
            for (int slot = 0; slot < sim.ProductCount; slot++)
            {
                int product = sim.GetAvailableProduct(slot);
                int remaining = sim.GetProductRemaining(product);
                if (pendingOnly)
                {
                    if (remaining > 0) entries.Add(ProductShortNames[product] + " " + remaining);
                }
                else entries.Add(ProductShortNames[product] + " " + sim.GetProductDelivered(product) + "/" + sim.GetProductGoal(product));
            }
            if (entries.Count == 0) return pendingOnly ? "Todas las metas completas" : "—";
            var lines = new System.Collections.Generic.List<string>(2);
            for (int i = 0; i < entries.Count; i += 4)
                lines.Add(string.Join("  ·  ", entries.GetRange(i, Mathf.Min(4, entries.Count - i)).ToArray()));
            return string.Join("\n", lines.ToArray());
        }

        private void ResultPanel()
        {
            // Physical-screen dimmer covers the HUD and safe-area insets too.
            DrawIntroScreen(Texture2D.whiteTexture, new Color(0f, 0f, 0f, .63f), ScaleMode.StretchToFill);
            // Keep the status HUD crisp above the dimmed playfield, as in the in-game reference.
            if (game.ShowChulengoResult) DrawCounters(game.Sim);
            Rect frame = VictoryPopupBounds(logicalCanvasHeight, game.ShowChulengoResult);
            float previousVertical = layoutVerticalScale;
            try
            {
                // Uniform popup coordinates: never stretch its artwork, type or hit area vertically.
                layoutVerticalScale = 1f;
                Texture2D popup = game.ShowChulengoResult ? ClubColoredVictoryPopup(game.Sim.LevelIndex) : victoryPopup;
                if (popup != null) GUI.DrawTexture(frame, popup, ScaleMode.StretchToFill, true);
                else
                {
                    FillRect(frame, new Color(.19f, .07f, .025f));
                    FillRect(VictorySlot(frame, new Rect(.07f, .35f, .86f, .47f)), new Color(1f, .91f, .71f));
                    FillRect(VictorySlot(frame, VictoryExit), new Color(.015f, .06f, .18f));
                }
                float fontScale = frame.width / 516f;
                Color gold = new Color(1f, .82f, .22f), brown = new Color(.18f, .055f, .018f);
                VictoryText(VictorySlot(frame, new Rect(.095f, .149f, .81f, .189f)),
                    "¡TURNO\nCOMPLETADO!", Mathf.RoundToInt(50f * fontScale), gold, TextAnchor.MiddleCenter);

                bool victory = game.Sim.Phase == RoundPhase.Won;
                if (game.ShowChulengoResult)
                {
                    bool lockedRetry = game.HasPendingChulengo && game.ChulengoChoiceLocked;
                    bool pending = game.HasPendingChulengo && !lockedRetry;
                    int sector = game.ChulengoSelectedSector;
                    DrawChulengoPaper(frame);
                    DrawChulengoHero(frame, pending, lockedRetry, sector);
                    DrawChulengoMeter(VictorySlot(frame, new Rect(.0625f, .47f, .875f, .275f)),
                        !pending, sector, Mathf.RoundToInt(29f * fontScale));
                    DrawChulengoFooter(frame, pending, lockedRetry, sector);
                    if (pending || lockedRetry)
                    {
                        DrawStandardButton(ChulengoMultiplyBounds(), ChulengoMultiplyAction, lockedRetry
                            ? sector < 0 ? "REINTENTAR COBRO" : "REINTENTAR RESULTADO"
                            : "MULTIPLICAR");
                        if (pending)
                            DrawStandardButton(ChulengoCollectBounds(), ChulengoCollectAction,
                                "COBRAR " + FormatChulengoMoney(game.ChulengoBaseRewardCents));
                        DrawStandardButton(ChulengoLevelsBounds(), ResultLevelsAction, "NIVELES", false);
                    }
                    else
                    {
                        bool canAdvanceChulengo = game.Sim.LevelIndex < StreetSimulation.LevelNames.Length - 1;
                        DrawStandardButton(VictorySlot(frame, VictoryExit), VictoryExitAction,
                            canAdvanceChulengo ? "SIGUIENTE PARTIDO" : "ÚLTIMO NIVEL", canAdvanceChulengo);
                        DrawStandardButton(ChulengoLevelsBounds(), ResultLevelsAction, "NIVELES");
                    }
                    return;
                }
                VictoryText(VictorySlot(frame, new Rect(.10f, .345f, .80f, .055f)),
                    victory ? "VENTAS COMPLETADAS" : "FALTAN", Mathf.RoundToInt(23f * fontScale), brown, TextAnchor.MiddleCenter);
                string sales = FormatQuotaSummary(game.Sim, !victory);
                VictoryText(VictorySlot(frame, new Rect(.09f, .398f, .82f, .15f)), sales,
                    Mathf.RoundToInt(17f * fontScale), gold, TextAnchor.MiddleCenter);
                DrawMarketReceipt(VictorySlot(frame, new Rect(.10f, .55f, .80f, .27f)));
                bool canAdvance=game.Sim.LevelIndex < StreetSimulation.LevelNames.Length - 1;
                DrawStandardButton(VictorySlot(frame, VictoryExit), VictoryExitAction, canAdvance?"SIGUIENTE PARTIDO":"ÚLTIMO NIVEL", canAdvance);
                DrawStandardButton(ResultLevelsBounds(), ResultLevelsAction, "NIVELES");
            }
            finally { layoutVerticalScale = previousVertical; }
        }
        private void DrawMarketReceipt(Rect bounds)
        {
            StreetMarketResult r=game.Market!=null?game.Market.LastResult:null;
            if(r==null)return;
            string line1="VENTAS "+FormatMarketMoney(r.grossSalesCents)+" → 50% "+FormatMarketMoney(r.halfSalesCents);
            string line2=r.won?"BONO SOBRANTE: 2 × "+FormatMarketMoney(r.leftoverCents)+" = "+FormatMarketMoney(r.bonusCents):"DERROTA: sin bono por sobrante";
            string line3="VENTAS RECONOCIDAS: "+FormatMarketMoney(r.recognizedSalesCents);
            string line4="REPOSICIÓN: "+FormatMarketMoney(r.progressionBonusCents);
            string line5="INGRESO NUEVO: "+FormatMarketMoney(r.earnedCents);
            string line6="SALDO ACTUAL: "+FormatMarketMoney(r.totalBalanceCents);
            string[] lines={line1,line2,line4,line3,line5,line6};
            int oldSize=small.fontSize;
            TextAnchor oldAlignment=small.alignment;
            bool oldWordWrap=small.wordWrap;
            float row=bounds.height/lines.Length;
            Color oldColor=small.normal.textColor;small.normal.textColor=new Color(.24f,.10f,.035f);
            small.alignment=TextAnchor.MiddleLeft;
            small.wordWrap=false;
            float textX=bounds.x+bounds.width*.31f;
            float textWidth=bounds.width*.66f;
            for(int i=0;i<lines.Length;i++)
            {
                Rect lineBounds=new Rect(textX,bounds.y+row*i,textWidth,row);
                small.fontSize=13;
                while(small.fontSize>10 && small.CalcSize(new GUIContent(lines[i])).x>lineBounds.width) small.fontSize--;
                Label(lineBounds,lines[i],small);
            }
            small.normal.textColor=oldColor;
            small.alignment=oldAlignment;
            small.wordWrap=oldWordWrap;
            small.fontSize=oldSize;
        }
        private void Styles()
        {
            if(tiny!=null)return;
            tiny=Style(11);small=Style(14);text=Style(18);title=Style(23);header=Style(25);header.normal.textColor=Color.white;amount=Style(31);
            invisible=new GUIStyle();
            upgradeTitle=Style(20);upgradePercent=Style(16);upgradePrice=Style(27);
            foreach(GUIStyle style in new[] { upgradeTitle, upgradePercent, upgradePrice })
            {
                if(menuFont!=null){style.font=menuFont;style.fontStyle=FontStyle.Normal;}
                style.wordWrap=false;style.padding=new RectOffset(0,0,0,0);
            }
            upgradePrice.normal.textColor=Color.white;
            riotCue = Style(28); riotCue.normal.textColor = new Color(.96f, .08f, .04f);
            riotCue.wordWrap = false;
            hudNumber = Style(40);
            if (menuFont != null) { hudNumber.font = menuFont; hudNumber.fontStyle = FontStyle.Normal; }
            hudNumber.padding = new RectOffset(0, 0, 0, 0);
            hudNumber.wordWrap = false;
            levelTitle = Style(22);
            if (menuFont != null) { levelTitle.font = menuFont; levelTitle.fontStyle = FontStyle.Normal; }
            levelTitle.padding = new RectOffset(0, 0, 0, 0);
            levelTitle.wordWrap = false;
            menuTitle = Style(38);
            if (menuFont != null) { menuTitle.font = menuFont; menuTitle.fontStyle = FontStyle.Normal; }
            menuTitle.wordWrap = false;
            Font tutorialFont = Resources.Load<Font>("Google/GoogleSans-Medium");
            levelOneTutorialTitleStyle = new GUIStyle(menuTitle) { fontSize = 25, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            if (tutorialFont != null) { levelOneTutorialTitleStyle.font = tutorialFont; levelOneTutorialTitleStyle.fontStyle = FontStyle.Bold; }
            levelOneTutorialTitleStyle.normal.textColor = new Color(.20f, .045f, .015f);
            levelOneTutorialBodyStyle = new GUIStyle(menuTitle) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            if (tutorialFont != null) { levelOneTutorialBodyStyle.font = tutorialFont; levelOneTutorialBodyStyle.fontStyle = FontStyle.Bold; }
            levelOneTutorialBodyStyle.normal.textColor = new Color(.24f, .085f, .03f);
            levelOneTutorialSkipStyle = new GUIStyle(menuTitle) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            levelOneTutorialSkipStyle.normal.textColor = new Color(.045f, .14f, .29f);
        }
        private static GUIStyle Style(int size){var s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};s.normal.textColor=new Color(.16f,.10f,.08f);return s;}
        private void Label(Rect r,string value,GUIStyle style){GUI.Label(LayoutRect(r),value,style);}
        private void Item(Rect r,int id,bool stretch=false)
        {
            if(id==10 && gameCoin!=null)Draw(r,gameCoin,new Rect(0,0,gameCoin.width,gameCoin.height),false,false);
            else if(id==16 && parrilleroIcon!=null)Draw(r,parrilleroIcon,new Rect(22,6,1269,1188),false,false);
            else if(items!=null&&id>=0&&id<Items.Length)Draw(r,items,Items[id],stretch,false);
        }
        private void DrawProductIcon(Rect r, int product, bool applyLayout = true)
        {
            if (applyLayout) r = LayoutRect(r);
            if (product == 4 && cocaBottle != null)
                DrawRaw(r, cocaBottle, new Rect(0, 0, cocaBottle.width, cocaBottle.height), false, false);
            else if (items != null && product >= 0 && product < Items.Length)
                DrawRaw(r, items, Items[product], false, false);
        }
        private void Parrillero(Rect r,int id,bool flip)
        {
            if(parrillero!=null)Draw(r,parrillero,ParrilleroPoses[id],false,flip);
        }
        private void ParrilleroDiagonal(Rect r,int id)
        {
            if(parrilleroDiagonal!=null&&id>=0&&id<ParrilleroDiagonalPoses.Length)
                Draw(r,parrilleroDiagonal,ParrilleroDiagonalPoses[id],false,false);
        }
        private void Person(Rect r,int id,bool flip){if(people==null)return;Draw(r,people,People[id],false,flip);}
        private void Draw(Rect dest,Texture2D texture,Rect src,bool stretch,bool flip)
        {
            DrawRaw(LayoutRect(dest), texture, src, stretch, flip);
        }
        private static Rect CropSourceToAspect(Rect source, float destinationAspect)
        {
            if (source.width <= 0f || source.height <= 0f || destinationAspect <= 0f) return source;
            float sourceAspect = source.width / source.height;
            if (sourceAspect > destinationAspect)
            {
                float width = source.height * destinationAspect;
                source.x += (source.width - width) * .5f;
                source.width = width;
            }
            else if (sourceAspect < destinationAspect)
            {
                float height = source.width / destinationAspect;
                source.y += (source.height - height) * .5f;
                source.height = height;
            }
            return source;
        }
        private static void DrawRawCover(Rect dest, Texture2D texture, Rect src)
        {
            if (texture == null || dest.width <= 0f || dest.height <= 0f) return;
            DrawRaw(dest, texture, CropSourceToAspect(src, dest.width / dest.height), true, false);
        }
        private static void DrawRaw(Rect dest,Texture2D texture,Rect src,bool stretch,bool flip)
        {
            if(!stretch){float scale=Mathf.Min(dest.width/src.width,dest.height/src.height);dest=new Rect(dest.center.x-src.width*scale*.5f,dest.yMax-src.height*scale,src.width*scale,src.height*scale);}
            Rect uv=new Rect(src.x/texture.width,1-src.yMax/texture.height,src.width/texture.width,src.height/texture.height);
            if(flip){uv.x+=uv.width;uv.width=-uv.width;}GUI.DrawTextureWithTexCoords(dest,texture,uv,true);
        }
    }
}
