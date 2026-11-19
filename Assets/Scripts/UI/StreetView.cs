using System.Collections;
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
        private const string HuracanCrestResource = "street-huracan-crest-v1";
        private const string ArgentinosCrestResource = "street-argentinos-crest-v1";
        private const string LaRengaCrestResource = "street-larenga-crest-v1";
        // Ordered by StreetSimulation.LevelDisplayNames; user-supplied crests are scoped to their levels.
        private static readonly string[] LevelCrestResourceNames = {
            AllBoysCrestResource, ChicagoCrestResource, VelezCrestResource,
            FerroCrestResource, IndependienteCrestResource, RacingCrestResource,
            SanLorenzoCrestResource, RiverCrestResource, BocaCrestResource,
            CamionerosCrestResource, LosRedondosCrestResource, HuracanCrestResource, ArgentinosCrestResource, LaRengaCrestResource, "street-peregrinacion-crest-v1"
        };
        private const string VelezGrillResource = "street-parrilla-velez-v1";
        private const string FerroBackgroundResource = "street-background-ferro-v2";
        private const string IndependienteBackgroundResource = "street-background-independiente-v2";
        private const string RacingBackgroundResource = "street-background-racing-v1";
        private const string SanLorenzoBackgroundResource = "street-background-sanlorenzo-v1";
        private const string BocaBackgroundResource = "street-background-boca-v1";
        private const int RiverVisualLevelIndex = 7;
        private const string RiverMuralResource = "street-mural-river-master-v1";
        private const string RiverHeaderExtensionResource = "street-river-header-extension-v1";
        private const string CamionerosBackgroundResource = "street-background-camioneros-v1";
        private const string LosRedondosBackgroundResource = "street-background-losredondos-v1";
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
        private const int AllBoysFanVariantCount = 9;
        private const int AllBoysFanAtlasColumns = 3;
        private const int AllBoysLegacyFrontFanFirstFrame = 8;
        private const int AllBoysLegacyWalkingFanFirstFrame = 12;
        private const int AllBoysLegacyFanVariantCount = 4;
        // Eight original Nueva Chicago outfits are integrated into complete character poses.
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
        private const TextAnchor HudQuotaCounterTextAlignment = TextAnchor.MiddleCenter;
        private const float HudChipTextHeightInset = 3f * HudBandScale;
        private const float HudQuotaBaseCardHeight = (HudQuotaBaseGridHeight - HudObjectiveGap - HudChipInset) * .5f;
        private const float HudQuotaCardHeight = HudQuotaBaseCardHeight * HudQuotaCardScale;
        private const float HudQuotaCardBaseWidth = 84f;
        private const float HudQuotaCardWidth = HudQuotaCardBaseWidth * HudQuotaCardEnlargement;
        private const int HudQuotaColumnsPerGroup = 2;
        private const float HudQuotaColumnSpacing = 12f;
        private const int HudQuotaSolidProductCount = 4;
        private const float HudQuotaGridHeight = HudQuotaCardHeight * 2f + HudObjectiveGap;
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
        private static readonly Rect ChicagoBarrelRect = StreetWorkstationLayout.CocaBounds;
        private static readonly Rect VelezBarrelRect = StreetWorkstationLayout.CocaBounds;
        private static Rect ServingTableBoundsForLevel(int productCount, int levelIndex) => StreetWorkstationLayout.FoodTableBounds;
        private static readonly Rect Start = new Rect(161, 555, 218, 61);
        private static readonly Rect Speed = new Rect(28, 710, 234, 120);
        private static readonly Rect HireParrillero = new Rect(278, 710, 234, 120);
        private static readonly Rect HireCocacolero = new Rect(360, 710, 156, 120);
        private const int HireCocacoleroAction = 16;
        private const int HirePremiumAction = 20, HireFerneteroAction = 21, HireMozoAction = 22;
        // Called several times per frame; the result only depends on these flags, so share read-only arrays.
        private static readonly System.Collections.Generic.Dictionary<int, int[]> catalogUpgradeActionsCache =
            new System.Collections.Generic.Dictionary<int, int[]>();
        private static int[] CatalogUpgradeActions(StreetSimulation sim)
        {
            bool active = sim.UsesActiveCooks;
            int key = active ? 1 : 0;
            if (!active)
            {
                if (sim.HasRole(StreetWorkerRole.Parrillero)) key |= 2;
                if (sim.HasRole(StreetWorkerRole.ParrilleroPremium)) key |= 4;
                if (sim.HasRole(StreetWorkerRole.Cocacolero)) key |= 8;
                if (sim.HasRole(StreetWorkerRole.Fernetero)) key |= 16;
            }
            int[] cached;
            if (catalogUpgradeActionsCache.TryGetValue(key, out cached)) return cached;
            var actions = new System.Collections.Generic.List<int> { 3 };
            if (active)
                actions.AddRange(new[] { 2, HirePremiumAction, HireCocacoleroAction, HireFerneteroAction, HireMozoAction });
            else
                foreach (var role in new[] { StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Cocacolero, StreetWorkerRole.Fernetero })
                    if (sim.HasRole(role)) actions.Add(ActionForRole(role));
            cached = actions.ToArray();
            catalogUpgradeActionsCache[key] = cached;
            return cached;
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
            public Rect[] BonusChoiceBounds;
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


        private static ResponsiveFooterLayout BuildResponsiveFooterLayoutCore(
            StreetSimulation sim, float canvasHeight, bool showUpgrades, bool showReadySelector, float decorationBottom)
        {
            float verticalScale = Mathf.Max(.01f, canvasHeight / H);
            float compactScale = Mathf.Min(1f, verticalScale);
            int[] actions = showUpgrades ? CatalogUpgradeActions(sim) : new int[0];
            bool showBonusChoices = sim.IsBonusTrack && sim.BonusTrack != null && sim.BonusTrack.SelectionPending;
            int upgradeCount = actions.Length;
            int bonusChoiceCount = !showBonusChoices ? 0 : sim.BonusTrack.EmergencyRoleSelectionPending
                ? sim.BonusTrack.EmergencyRoles.Count : sim.BonusTrack.CurrentCards.Count;
            int count = showBonusChoices ? bonusChoiceCount : upgradeCount;
            int rows = count == 0 ? 0 : count <= 3 ? 1 : 2;
            float cardHeight = showBonusChoices ? 120f * compactScale
                : (count >= 5 ? 100f : count == 4 ? 104f : 120f) * compactScale;
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
            // Levels 1–5 keep their pennants. Huracán is the explicit bonus-track exception;
            // regular Racing and later campaign levels keep the match scenery unobstructed.
            float pennantHeight = sim.LevelIndex < 5 || sim.IsBonusTrack
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

            var cards = new Rect[upgradeCount];
            for (int i = 0; i < upgradeCount; i++)
            {
                Rect authored = CatalogUpgradeCardBounds(sim, actions[i]);
                int row = upgradeCount == 4 ? i / 2 : upgradeCount == 6 && sim.UsesActiveCooks ? i / 3 : upgradeCount == 5 && i >= 3 ? 1 : 0;
                cards[i] = new Rect(authored.x, (cardTop + row * (cardHeight + rowGap)) / verticalScale,
                    authored.width, authored.height);
            }

            var bonusChoices = new Rect[showBonusChoices ? bonusChoiceCount : 0];
            if (showBonusChoices)
            {
                // Bonus cards and Refuerzo role choices are the hit targets themselves. Reuse
                // the normal one-row upgrade-card footprint and reserve it before the pennants.
                float baseChoiceCardGap = bonusChoiceCount <= 2 ? 12f : 9f;
                float choiceCardGap = baseChoiceCardGap * compactScale;
                float baseChoiceCardWidth = bonusChoiceCount <= 2
                    ? (W - 88f - baseChoiceCardGap * Mathf.Max(0, bonusChoiceCount - 1)) / Mathf.Max(1, bonusChoiceCount)
                    : 156f;
                float choiceCardWidth = baseChoiceCardWidth * compactScale;
                float cardStart = (W - (bonusChoiceCount * choiceCardWidth +
                    Mathf.Max(0, bonusChoiceCount - 1) * choiceCardGap)) * .5f;
                for (int i = 0; i < bonusChoices.Length; i++)
                    bonusChoices[i] = new Rect(cardStart + i * (choiceCardWidth + choiceCardGap),
                        cardTop, choiceCardWidth, cardHeight);
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
                UpgradeBounds = cards, BonusChoiceBounds = bonusChoices, LevelBadge = badge,
                ReadySelector = selector, Pennants = pennants
            };
        }

        private ResponsiveFooterLayout CurrentResponsiveFooter(StreetSimulation sim, float canvasHeight, float bottomBleed = 0f)
        {
            bool ready = !levelSelectActive && sim.Phase == RoundPhase.Ready;
            bool play = VictoryTransitionActive || (!levelSelectActive && sim.Phase == RoundPhase.Playing);
            // Bonus Track uses its lower footer for pending, directly tappable choice cards;
            // regular catalog upgrades remain exclusive to the campaign levels.
            bool showCards = !sim.IsBonusTrack && (play || (ready && (CatalogUpgradeActions(sim).Length <= 3 ||
                levelFourSpecialistsTutorialActive || levelFiveSpecialistsTutorialActive || levelSixRacingTutorialActive)));
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
        private const int LevelSelectorBackAction = 8, VictoryExitAction = 9, RiotReturnAction = 15, LevelSelectFirstAction = 30;
        private const int HubPlayAction = 200, HubMarketAction = 201, HubBadgesAction = 202, HubRankingAction = 203, HubAccountAction = 204;
        private const int BadgeBackAction = 205, RankingBackAction = 206;
        private const int HubSettingsAction = 207, HubSettingsMusicAction = 208, HubSettingsBackAction = 209;
        private const int HubTopMusicAction = 210;
        private const string HubMusicEnabledPlayerPrefKey = "HayChoriYPaty.Hub.MusicEnabled";
        private static readonly Rect HubSettingsArtworkSourceBounds = new Rect(1f, 121f, 40f, 40f);
        private static readonly Rect HubMusicArtworkSourceBounds = new Rect(714f, 91f, 40f, 40f);
        private static readonly Rect HubSettingsPanelBounds = new Rect(48, 300, 444, 360);
        private static readonly Rect HubSettingsMusicButtonBounds = new Rect(76, 410, 388, 96);
        private static readonly Rect HubSettingsBackButtonBounds = new Rect(126, 520, 288, 96);
        private const float HubSettingsButtonWidth = 48f, HubSettingsButtonHeight = 48f;
        private const float HubTopMusicButtonSize = 44f, HubTopControlsGap = 8f;
        // Shared with FirebasePlayerSession so CUENTA and the music control stay aligned.
        private const float HubAccountButtonRightInset = 126f, HubAccountButtonWidth = 110f;
        private const float HubAccountButtonTop = 16f, HubAccountButtonHeight = 32f;
        private enum HubControlIconKind { Settings, Music, Muted }
        private static readonly GUIContent HubSettingsButtonContent = new GUIContent("", "Abrir configuración");
        private static readonly GUIContent HubMusicOnButtonContent = new GUIContent("", "Silenciar música");
        private static readonly GUIContent HubMusicOffButtonContent = new GUIContent("", "Activar música");
        private static readonly int[] HubArtworkActions =
            { HubRankingAction, HubBadgesAction, HubMarketAction, HubPlayAction };
        private static readonly Vector2[] HubSpeakerPolygon =
        {
            new Vector2(10f, 24f), new Vector2(21f, 24f),
            new Vector2(37f, 12f), new Vector2(37f, 52f),
            new Vector2(21f, 40f), new Vector2(10f, 40f)
        };
        private const string LevelOneTutorialCompletedKey = "HayChoriYPaty.LevelOneTutorial.Completed.v2";
        private const string LevelTwoCocacoleroTutorialCompletedKey = "HayChoriYPaty.LevelTwoCocacoleroTutorial.Completed.v1";
        private const string LevelThreePatyTutorialCompletedKey = "HayChoriYPaty.LevelThreePatyTutorial.Completed.v1";
        private const string LevelFourSpecialistsTutorialCompletedKey = "HayChoriYPaty.LevelFourSpecialistsTutorial.Completed.v1";
        private const string LevelFiveSpecialistsTutorialCompletedKey = "HayChoriYPaty.LevelFiveSpecialistsTutorial.Completed.v1";
        private const string LevelSixRacingTutorialCompletedKey = "HayChoriYPaty.LevelSixRacingTutorial.Completed.v1";
        // Editor-only QA override. Player builds must retain the once-only tutorial behavior.
#if UNITY_EDITOR
        private const bool RepeatLevelOneTutorialForTesting = true;
#else
        private const bool RepeatLevelOneTutorialForTesting = false;
#endif
        private const int LevelOneTutorialPrimaryAction = 300, LevelOneTutorialSkipAction = 301;
        private const float LevelOneTutorialBubbleWidth = 320f;
        private const float LevelOneTutorialBubbleHeight = 140f;
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
        private static readonly string[] LevelTwoCocacoleroTutorialHeadlines = {
            "En Nueva Chicago,\nla Coca tiene su meta.",
            "Para entregar Coca,\ncontratá al Cocacolero."
        };
        private static readonly string[] LevelTwoCocacoleroTutorialExplanations = {
            "Completá 200 entregas\ndurante el partido.",
            "El costo aparece en esta tarjeta.\nContratalo al empezar."
        };
        private const string LevelThreePatyTutorialHeadline = "En Vélez también\nvendés Patys.";
        private const string LevelThreePatyTutorialExplanation = "Meta: 120 entregas.\nCada Paty suma $5.";
        private static readonly string[] LevelFourSpecialistsTutorialHeadlines = {
            "La Bondiola la entrega\nel Parrillero Premium.",
            "La cerveza la entrega\nel Cocacolero."
        };
        private static readonly string[] LevelFourSpecialistsTutorialExplanations = {
            "Meta: 60 Bondiolas.\nContratalo en esta tarjeta.",
            "Meta: 75 cervezas.\nContratalo en esta tarjeta."
        };
        private static readonly string[] LevelFiveSpecialistsTutorialHeadlines = {
            "El Vacío lo entrega\nel Parrillero Premium.",
            "El Fernet lo entrega\nel Fernetero."
        };
        private static readonly string[] LevelFiveSpecialistsTutorialExplanations = {
            "Meta: 65 Vacíos.\nContratalo en esta tarjeta.",
            "Meta: 65 Fernets.\nContratalo en esta tarjeta."
        };
        private static readonly string[] LevelSixRacingTutorialHeadlines = {
            "En Racing, la cocina\npasa a ser activa.",
            "El Mozo recoge y entrega\ncada sándwich."
        };
        private static readonly string[] LevelSixRacingTutorialExplanations = {
            "Normal y Premium cocinan\nun sándwich y lo dejan en mesa.",
            "Enojo: su sándwich no está\nlisto. Al retirarlo, se calma."
        };
        private const float LevelSelectorCardHeight = 184f, LevelSelectorCardGap = 12f;
        private static readonly Rect LevelSelectorViewport = new Rect(14f, 154f, W - 28f, 706f);
        private float levelSelectScroll, levelSelectPointerDownY, levelSelectPointerDownScroll;
        private bool levelSelectPointerHeld, levelSelectDragging;
        private float levelSelectTouchDownY, levelSelectTouchDownScroll;
        private bool levelSelectTouchHeld, levelSelectTouchDragging;
        private int levelSelectIgnoreNativeTapUntilFrame = -1;
        private const int ReadyLevelSelectorAction = 19;
        private const int MarketBackAction = 70, MarketOtherAction = 190;
        private const int MarketIngredientsTabAction = 191, MarketExtrasTabAction = 192, MarketPermitsTabAction = 193, MarketTicketRedeemAction = 220;
        private const float MarketTicketCardHeight = 108f, MarketTicketCardGap = 8f, MarketExtrasNoticeHeight = 38f;
        private const int MarketMinusAction = 80, MarketPlusAction = 100, MarketPlusTenAction = 120;
        private const int MarketBuyAction = 140, MarketMissingAction = 150, MarketPlayAction = 160;
        private const int MarketRowMissingAction = 170, MarketExpandAction = 180;
        private const int MarketMunicipalPermitAction = 230, MarketPoliceContributionAction = 231;
        private const float MarketPermitCardHeight = 112f, MarketPermitCardGap = 8f;
        private const int ResultLevelsAction = 163;
        private const int ChulengoMultiplyAction = 164, ChulengoCollectAction = 165;
        private static readonly Rect ReadyLevelSelector = new Rect(161, 905, 218, 44);
        private bool marketActive, marketFromSelector;
        private int marketTab;
        private bool marketOtherExpanded;
        private int marketExpandedIngredient = -1;
        private GUIStyle marketLabelStyle;
        private readonly Sprite[] marketIngredientSprites = new Sprite[10];
        private readonly Texture2D[] marketFreshMeatTextures = new Texture2D[10];
        private Texture2D marketFernetBottle, marketCocaLarge, marketBreadCommon, marketBreadBurger;
        private Texture2D municipalPermitIcon, policeContributionIcon;
        private readonly int[] marketPendingPacks = new int[10];
        private float marketScroll, marketPointerDownY, marketPointerDownScroll, marketLastPurchaseTime = -10f, marketLastPermitPurchaseTime = -10f;
        private bool marketDragging, marketPointerHeld;
        private float marketTouchDownY, marketTouchDownScroll;
        private bool marketTouchHeld, marketTouchDragging;
        private int marketIgnoreNativeTapUntilFrame = -1;
        private string marketMessage = "";
        private static readonly Rect LevelSelectTitle = new Rect(54, 88, 432, 58);
        private static readonly Rect LevelSelectBack = new Rect(139, 870, 262, 66);
        private const string VictoryPopupResource = "street-victory-popup-wood-v1";
        private const string ChulengoBowlResource = "street-chulengo-bowl-v1";
        private static readonly Vector2 VictoryPopupArtSize = new Vector2(1122, 1402);
        private const string ChulengoVictoryTitleFirstLine = "NIVEL";
        private const string ChulengoVictoryTitleSecondLine = "COMPLETADO";
        private const string ChulengoTicketCaption = "+1 TICKET";
        private static readonly Color ChulengoTitleOutline = new Color(.015f, .055f, .19f, .99f);
        private static readonly Rect ChulengoTitleFirstLineSlot = new Rect(.235f, .125f, .53f, .085f);
        private static readonly Rect ChulengoTitleSecondLineSlot = new Rect(.115f, .216f, .77f, .089f);
        private static readonly Rect ChulengoPrizeLabelSlot = new Rect(.505f, .405f, .30f, .047f);
        private static readonly Rect ChulengoPrizeAmountSlot = new Rect(.505f, .455f, .30f, .088f);
        private static readonly Rect ChulengoTicketCardSlot = new Rect(.83f, .387f, .16f, .08f);
        private static readonly Rect ChulengoTicketCaptionSlot = new Rect(.815f, .467f, .18f, .038f);
        // Normalized slots in the transparent artwork; text and pointer bounds use the same frame.
        private static readonly Rect VictoryExit = new Rect(.195f, .839f, .62f, .151f);
        private const string IntroBackdropResource = "street-cover-user-v5";
        private const string HubBackdropResource = "street-hub-stadium-v1";
        private const string HubMusicResourcePath = "Audio/Muchachos_16bit_20pct_mas_lento_LOOP";
        private const float HubMusicVolume = 0.35f;
        private const string HubMusicObjectName = "Hub Background Music";
        private static readonly Vector2 HubArtworkSize = new Vector2(841, 1870);
        private const string RiotBackdropResource = "street-riot-environment-v1";
        private const string RiotFanAtlasResource = "street-riot-fans-v1";
        private const string RiotImpactCloudResource = "street-riot-fight-cloud-v1";
        private const float RiotImpactCloudDuration = 1.55f;
        private const float RiotAngerRampSeconds = .55f, RiotBreakStartSeconds = .55f, RiotBreakTransitionSeconds = .9f, RiotFanFramesPerSecond = 6f;
        private const float RiotFanAtlasRowSplitY = 493f;
        private static readonly Rect RiotReturnButton = new Rect(88, 858, 364, 66);
        private StreetGame game;
        private AudioSource hubMusicAudioSource;
        private AudioClip hubMusicClip;
        private bool hubMusicLoadWarningLogged;
        private Texture2D hubSettingsGearIcon, hubMusicOnIcon, hubMusicOffIcon;
        private int VisualLevelIndex => game != null && game.Sim != null ? game.Sim.LevelIndex : 0;
        private Texture2D backdrop, chicagoBackground, velezBackground, velezGrill, people, items, parrillero, parrilleroDiagonal, cocacolero, laterCocacolero, parrilleroIcon, upgradeWood, speedArrows, largeGrill, gameCoin, muralArt, allBoysCrest, chicagoCrest, cocaBottle, beverageBarrel, chicagoGrill;
        private Texture2D[] levelCrests;
        private readonly System.Collections.Generic.Dictionary<Texture2D, Rect> crestVisibleUvs =
            new System.Collections.Generic.Dictionary<Texture2D, Rect>();
        private readonly System.Collections.Generic.Dictionary<Texture2D, Rect> crestBodyUvs =
            new System.Collections.Generic.Dictionary<Texture2D, Rect>();
        private static Texture2D racingCrestCutout, camionerosCrestCutout, losRedondosCrestCutout,
            huracanCrestCutout, argentinosCrestCutout;
        private Texture2D clubPennantTexture;
        private string clubPennantThemeName;
        private float clubPennantLogicalHeight = -1f;
        private Texture2D clubMuralTexture;
        private string clubMuralResource;
        private Texture2D allBoysFrontFans, allBoysWalkingFans;
        private Texture2D chicagoFrontFans, chicagoWalkingFans;
        private Texture2D velezFrontFans, velezWalkingFans;
        private Texture2D ferroBackground, ferroFrontFans, ferroWalkingFans;
        private Texture2D independienteBackground, independienteFrontFans, independienteWalkingFans;
        private Texture2D racingBackground, racingFans;
        private Texture2D sanLorenzoBackground, sanLorenzoFans;
        private Texture2D bocaBackground, bocaFans;
        private Texture2D riverMural, riverHeaderExtension, riverFans;
        private Texture2D camionerosBackground;
        private Texture2D losRedondosBackground, losRedondosFrontFans, losRedondosWalkingFans;
        private Texture2D fourZoneGrill, premiumGrill, beerBarrel, fernetTable;
        private Texture2D premiumWorker, premiumDiagonal, fernetWorker, fernetWorkerLater;
        private Texture2D mozoWorker;
        private Texture2D victoryPopup, clubColoredVictoryPopup;
        private int clubColoredVictoryPopupLevel = int.MinValue;
        private Texture2D chulengoMeter, chulengoNeedle, chulengoHub, chulengoParchment, chulengoBowl, chulengoTicketTexture;
        private Texture2D coverArt, hubBackdrop, titleLogo, menuButton, chulengoCollectButton, chulengoMultiplyButton, googleButtonSurface, hudBarTexture, cutoutHudBarTexture, hudQuotaCardTexture, hudQuotaCardOutlineTexture, gameplayFooterTexture, riotBackdrop, riotFanAtlas, riotImpactCloud;
        private readonly System.Collections.Generic.Dictionary<(int, int), Texture2D> glossyUpgradeCardTextures =
            new System.Collections.Generic.Dictionary<(int, int), Texture2D>();
        private readonly System.Collections.Generic.Dictionary<(int, int), Texture2D> affordableUpgradeBorderTextures =
            new System.Collections.Generic.Dictionary<(int, int), Texture2D>();
        private Font menuFont, googleSansFont;
        private GUIStyle menuTitle, levelTitle;
        private GUIStyle introStatusStyle;
        private Texture2D introPreviewBar;
        private Texture2D[] introLogoShines;
        private int introPreviewBarKey = -1;
        private bool introActive;
        private bool levelSelectActive;
        private bool hubActive, badgesActive, rankingActive, settingsActive;
        private bool levelOneTutorialActive;
        private int levelOneTutorialStep;
        private bool levelTwoCocacoleroTutorialActive;
        private int levelTwoCocacoleroTutorialStep;
        private bool levelThreePatyTutorialActive;
        private bool levelFourSpecialistsTutorialActive;
        private int levelFourSpecialistsTutorialStep;
        private bool levelFiveSpecialistsTutorialActive;
        private int levelFiveSpecialistsTutorialStep;
        private bool levelSixRacingTutorialActive;
        private int levelSixRacingTutorialStep;
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
        private IEnumerator LoadLevelCrestsAsync()
        {
            levelCrests = new Texture2D[LevelCrestResourceNames.Length];
            for (int i = 0; i < LevelCrestResourceNames.Length; i++)
            {
                string resource = LevelCrestResourceNames[i];
                int crestIndex = i;
                if (!string.IsNullOrEmpty(resource))
                    yield return LoadStartupResourceAsync<Texture2D>(resource, loaded => levelCrests[crestIndex] = loaded);
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
            if (levelCrests[11] != null)
            {
                if (huracanCrestCutout == null) huracanCrestCutout = CreateLevelCrestCutout(levelCrests[11]);
                if (huracanCrestCutout != null) levelCrests[11] = huracanCrestCutout;
            }
            if (levelCrests[12] != null)
            {
                if (argentinosCrestCutout == null) argentinosCrestCutout = CreateLevelCrestCutout(levelCrests[12]);
                if (argentinosCrestCutout != null) levelCrests[12] = argentinosCrestCutout;
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

        private const int TotalStartupResourceRequests = 68;
        private Coroutine startupResourceRoutine;
        private int startupResourcesCompleted;
        private float startupLoadProgress, startupLoadStartedAt;
        private bool startupArtReady, viewResourcesReady;

        private IEnumerator LoadStartupResourceAsync<T>(string resourceName, System.Action<T> assign)
            where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(resourceName);
            float requestStartedAt = Time.realtimeSinceStartup;
            while (!request.isDone)
            {
                startupLoadProgress = (startupResourcesCompleted + Mathf.Clamp01(request.progress)) /
                    TotalStartupResourceRequests;
                yield return null;
            }

            T loaded = request.asset as T;
            assign(loaded);
            startupResourcesCompleted++;
            startupLoadProgress = Mathf.Clamp01((float)startupResourcesCompleted / TotalStartupResourceRequests);
#if DEVELOPMENT_BUILD
            float loadSeconds = Time.realtimeSinceStartup - requestStartedAt;
            if (loaded == null)
                Debug.LogWarning("StreetView startup resource was not found: " + resourceName);
            else if (loadSeconds >= .5f)
                Debug.Log("StreetView startup resource loaded in " + loadSeconds.ToString("F2") + "s: " + resourceName);
#endif
            // Keep even cache hits from collapsing into one long OnEnable frame.
            yield return null;
        }

        private IEnumerator LoadViewResourcesAsync()
        {
            // Let Unity render its first frame and the Firebase sign-in/status UI before loading game art.
            yield return null;
            startupLoadStartedAt = Time.realtimeSinceStartup;
#if DEVELOPMENT_BUILD
            Debug.Log("StreetView startup resource loading started.");
#endif

            // Minimum set for the branded startup and account button; this becomes visible before
            // the rest of the large club/gameplay art has finished loading.
            yield return LoadStartupResourceAsync<Texture2D>(IntroBackdropResource, loaded => coverArt = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-logo", loaded => titleLogo = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(HubBackdropResource, loaded => hubBackdrop = loaded);
            yield return LoadStartupResourceAsync<Font>("Menu/LuckiestGuy-Regular", loaded => menuFont = loaded);
            yield return LoadStartupResourceAsync<Font>("Google/GoogleSans-Medium", loaded => googleSansFont = loaded);
            menuButton = CreateMenuButton();
            chulengoCollectButton = CreateMenuButton(new Color(.73f, .92f, .32f), new Color(.07f, .28f, .02f),
                new Color(.69f, .90f, .10f), new Color(.45f, .70f, .035f), "Glossy lime-green Chulengo collect button");
            chulengoMultiplyButton = CreateMenuButton(new Color(1f, 1f, .77f), new Color(.42f, .24f, .03f),
                new Color(1f, .90f, .23f), new Color(.93f, .53f, .01f), "Glossy yellow Chulengo multiply button");
            googleButtonSurface = CreateGoogleButtonSurface();
            startupArtReady = true;
#if DEVELOPMENT_BUILD
            Debug.Log("StreetView startup cover assets ready after " +
                (Time.realtimeSinceStartup - startupLoadStartedAt).ToString("F2") + "s.");
#endif

            yield return LoadStartupResourceAsync<Texture2D>("street-background-open-street-v4", loaded => backdrop = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(MuralResource, loaded => muralArt = loaded);
            yield return LoadLevelCrestsAsync();
            yield return LoadStartupResourceAsync<Texture2D>(ChicagoBackgroundResource, loaded => chicagoBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(VelezBackgroundResource, loaded => velezBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(VelezGrillResource, loaded => velezGrill = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(FerroBackgroundResource, loaded => ferroBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(IndependienteBackgroundResource, loaded => independienteBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RacingBackgroundResource, loaded => racingBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(SanLorenzoBackgroundResource, loaded => sanLorenzoBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(BocaBackgroundResource, loaded => bocaBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RiverMuralResource, loaded => riverMural = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RiverHeaderExtensionResource, loaded => riverHeaderExtension = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(CamionerosBackgroundResource, loaded => camionerosBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(LosRedondosBackgroundResource, loaded => losRedondosBackground = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(FourZoneGrillResource, loaded => fourZoneGrill = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(BeerBarrelResource, loaded => beerBarrel = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(FernetTableResource, loaded => fernetTable = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(CocaBottleResource, loaded => cocaBottle = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(BeverageBarrelResource, loaded => beverageBarrel = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-characters", loaded => people = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-items", loaded => items = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-parrillero", loaded => parrillero = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-parrillero-diagonal-v1", loaded => parrilleroDiagonal = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-cocacolero-v1", loaded => cocacolero = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(LaterCocacoleroResource, loaded => laterCocacolero = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-parrillero-icon", loaded => parrilleroIcon = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-upgrade-wood-v1", loaded => upgradeWood = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-speed-arrows-v1", loaded => speedArrows = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-parrilla-large-v2", loaded => largeGrill = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(ChicagoGrillResource, loaded => chicagoGrill = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-coin-gold-v2", loaded => gameCoin = loaded);

            string[] marketArt = { "meat-chori/meat-chori_Raw", "sandwich-chori/sandwich-chori",
                "meat-paty/meat-paty_Raw", "sandwich-paty/sandwich-paty", "meat-bondiola/meat-bondiola_Raw",
                "meat-vacio/meat-vacio_Raw", "coca/coca", "beer/beer" };
            for (int i = 0; i < marketArt.Length; i++)
            {
                int spriteIndex = i;
                yield return LoadStartupResourceAsync<Sprite>("ModularKitchen/Sprites/" + marketArt[i],
                    loaded => marketIngredientSprites[spriteIndex] = loaded);
            }
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/fernet-bottle-1l-v1", loaded => marketFernetBottle = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/coca-2500ml-display-v1", loaded => marketCocaLarge = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/market-bread-common-v1", loaded => marketBreadCommon = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/market-bread-burger-v1", loaded => marketBreadBurger = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/market-municipal-permit-v1", loaded => municipalPermitIcon = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("ModularKitchen/Art/market-police-contribution-v1", loaded => policeContributionIcon = loaded);
            foreach (int id in new[] { 4, 5 })
            {
                int ingredientId = id;
                yield return LoadStartupResourceAsync<Texture2D>(MarketFreshMeatResource(ingredientId),
                    loaded => marketFreshMeatTextures[ingredientId] = loaded);
            }
            yield return LoadStartupResourceAsync<Texture2D>(VictoryPopupResource, loaded => victoryPopup = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RiotBackdropResource, loaded => riotBackdrop = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RiotFanAtlasResource, loaded => riotFanAtlas = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(RiotImpactCloudResource, loaded => riotImpactCloud = loaded);
            yield return LoadStartupResourceAsync<Texture2D>(ChulengoBowlResource, loaded => chulengoBowl = loaded);
            yield return LoadStartupResourceAsync<Texture2D>("street-grill-premium-v1", loaded => premiumGrill = loaded);

            chulengoMeter = CreateChulengoMeterTexture();
            yield return null;
            chulengoNeedle = CreateChulengoNeedleTexture();
            yield return null;
            chulengoHub = CreateChulengoHubTexture();
            yield return null;
            chulengoParchment = CreateChulengoParchmentTexture();
            yield return null;
            chulengoTicketTexture = CreateChulengoTicketTexture();
            yield return null;
            hudBarTexture = CreateHudBar();
            yield return null;
            cutoutHudBarTexture = CreateHudBarTexture(true);
            cachedHudCutoutGap = CutoutHudGap;
            yield return null;
            hudQuotaCardTexture = CreateHudQuotaCardTexture(false);
            yield return null;
            hudQuotaCardOutlineTexture = CreateHudQuotaCardTexture(true);
            yield return null;
            gameplayFooterTexture = CreateGameplayFooterTexture();
            yield return null;

            premiumWorker = StreetWorkerAppearance.CreateVariant(parrillero, StreetWorkerRole.ParrilleroPremium, ParrilleroPoses, new Vector2(1315,1197));
            yield return null;
            premiumDiagonal = StreetWorkerAppearance.CreateVariant(parrilleroDiagonal, StreetWorkerRole.ParrilleroPremium, ParrilleroDiagonalPoses, new Vector2(1536,1024));
            yield return null;
            mozoWorker = StreetWorkerAppearance.CreateVariant(parrillero, StreetWorkerRole.Mozo, ParrilleroPoses, new Vector2(1315,1197));
            yield return null;
            fernetWorker = StreetWorkerAppearance.CreateVariant(cocacolero, StreetWorkerRole.Fernetero, ChicagoCocacoleroPoses, new Vector2(1247,1261));
            yield return null;
            fernetWorkerLater = StreetWorkerAppearance.CreateVariant(laterCocacolero, StreetWorkerRole.Fernetero, MatchedCocacoleroPoses, new Vector2(1247,1261));
            startupResourcesCompleted = TotalStartupResourceRequests;
            startupLoadProgress = 1f;
            startupResourceRoutine = null;
            viewResourcesReady = true;
#if DEVELOPMENT_BUILD
            Debug.Log("StreetView startup assets fully ready after " +
                (Time.realtimeSinceStartup - startupLoadStartedAt).ToString("F2") + "s.");
#endif
        }

        private void OnEnable()
        {
            ResetTowerDefenseUi();
            game = GetComponent<StreetGame>();
            startupArtReady = false;
            viewResourcesReady = false;
            startupResourcesCompleted = 0;
            startupLoadProgress = 0f;
            introActive = true;
            levelSelectActive = false;
            levelOneTutorialActive = false;
            levelOneTutorialStep = 0;
            levelTwoCocacoleroTutorialActive = false;
            levelTwoCocacoleroTutorialStep = 0;
            levelThreePatyTutorialActive = false;
            levelFourSpecialistsTutorialActive = false;
            levelFourSpecialistsTutorialStep = 0;
            levelFiveSpecialistsTutorialActive = false;
            levelFiveSpecialistsTutorialStep = 0;
            levelSixRacingTutorialActive = false;
            levelSixRacingTutorialStep = 0;
            introStarted = Time.unscaledTime;
            splashClockStarted = false;
            tiny = small = text = title = header = amount = invisible = menuTitle = levelTitle = marketLabelStyle = null;
            upgradeTitle = upgradePercent = upgradePrice = riotCue = null;
            levelOneTutorialTitleStyle = levelOneTutorialBodyStyle = levelOneTutorialSkipStyle = null;
            levelOneTutorialBubbleTexture = null;
            levelOneTutorialBubbleTextureStep = -1;
            pressedAction = lastAction = 0;
            startupResourceRoutine = StartCoroutine(LoadViewResourcesAsync());
        }
        private void OnDisable()
        {
            rewardShutterStarted = -1f; // Uncommitted choices remain pending in the existing save.
            rewardShutterWhenCovered = null;
            DisposeTowerDefenseUi();
            if (hubMusicAudioSource != null) hubMusicAudioSource.Stop();
            ReleaseHubSignageResources();
            ReleaseHubAmbientEffectsResources();
            ReleaseHubLayeredEnvironmentResources();
            ReleaseHubCharacterAnimationsResources();
            ReleaseHubControlIcons();
            if (startupResourceRoutine != null) StopCoroutine(startupResourceRoutine);
            startupResourceRoutine = null;
            startupArtReady = false;
            viewResourcesReady = false;
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
            if (chulengoTicketTexture != null) Destroy(chulengoTicketTexture);
            chulengoTicketTexture = null;
            if (menuButton != null) Destroy(menuButton);
            menuButton = null;
            if (chulengoCollectButton != null) Destroy(chulengoCollectButton);
            chulengoCollectButton = null;
            if (chulengoMultiplyButton != null) Destroy(chulengoMultiplyButton);
            chulengoMultiplyButton = null;
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
        private static bool HubMusicEnabled() =>
            PlayerPrefs.GetInt(HubMusicEnabledPlayerPrefKey, 1) != 0;

        private static void SetHubMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(HubMusicEnabledPlayerPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ToggleHubMusic()
        {
            SetHubMusicEnabled(!HubMusicEnabled());
            UpdateHubMusic();
        }

        private static bool ShouldPlayHubMusic(bool hub, bool intro, bool market, bool levelSelect,
            bool badges, bool ranking, bool result)
        {
            return hub && !intro && !market && !levelSelect && !badges && !ranking && !result;
        }

        private void UpdateHubMusic()
        {
            bool shouldPlay = HubMusicEnabled() && ShouldPlayHubMusic(hubActive, introActive, marketActive, levelSelectActive,
                badgesActive, rankingActive, game != null && game.ShowChulengoResult);
            if (!shouldPlay)
            {
                if (hubMusicAudioSource != null) hubMusicAudioSource.Stop();
                return;
            }

            if (hubMusicClip == null)
            {
                hubMusicClip = Resources.Load<AudioClip>(HubMusicResourcePath);
                if (hubMusicClip == null)
                {
                    if (!hubMusicLoadWarningLogged)
                    {
                        Debug.LogWarning("Could not load the barrio hub music at Resources/" + HubMusicResourcePath);
                        hubMusicLoadWarningLogged = true;
                    }
                    return;
                }
            }

            if (hubMusicAudioSource == null)
            {
                Transform child = transform.Find(HubMusicObjectName);
                GameObject audioObject = child != null ? child.gameObject : new GameObject(HubMusicObjectName);
                if (child == null) audioObject.transform.SetParent(transform, false);
                hubMusicAudioSource = audioObject.GetComponent<AudioSource>();
                if (hubMusicAudioSource == null) hubMusicAudioSource = audioObject.AddComponent<AudioSource>();
            }

            ConfigureHubMusicSource(hubMusicAudioSource, hubMusicClip);
            if (!hubMusicAudioSource.isPlaying) hubMusicAudioSource.Play();
        }

        private static void ConfigureHubMusicSource(AudioSource source, AudioClip clip)
        {
            if (source.clip != clip) source.clip = clip;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = HubMusicVolume;
        }

        private void Update()
        {
            UpdatePostProcessingPolicy();
            UpdateRewardShutter();
            UpdateHubMusic();
            // The branded presentation belongs to startup, not to the authenticated game session.
            // Let it complete while Firebase resolves; game actions remain gated separately.
            if (introActive && startupArtReady)
            {
                if (!splashClockStarted) { introStarted = Time.unscaledTime; splashClockStarted = true; }
                if (viewResourcesReady && SplashComplete) OpenHubFromIntro();
            }
            if (IsTowerDefenseNavigationActive)
            {
                UpdateTowerDefenseFrame();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_ANDROID && !UNITY_EDITOR
                Touchscreen towerDefenseTouch = Touchscreen.current;
                if (towerDefenseTouch != null)
                {
                    var press = towerDefenseTouch.primaryTouch.press;
                    if (press.isPressed || press.wasReleasedThisFrame)
                        HandlePointer(towerDefenseTouch.primaryTouch.position.ReadValue(),
                            press.wasPressedThisFrame, press.wasReleasedThisFrame);
                }
#else
                var towerDefenseMouse = Mouse.current;
                if (towerDefenseMouse != null)
                    HandlePointer(towerDefenseMouse.position.ReadValue(), towerDefenseMouse.leftButton.wasPressedThisFrame,
                        towerDefenseMouse.leftButton.wasReleasedThisFrame);
#endif
#endif
                return;
            }
            if (!viewResourcesReady || game == null || !game.IsPlayerActive || game.Sim == null) return;
            ObserveVictoryTransition();
            if (introActive && !game.ShowChulengoResult) return;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android taps remain native IMGUI, but native buttons do not provide a scroll container.
            // Track touch drags separately; taps still dispatch through the native GUI controls below.
            if (levelSelectActive && !game.ShowChulengoResult)
            {
                Touchscreen touch = Touchscreen.current;
                if (touch != null)
                {
                    var press = touch.primaryTouch.press;
                    if (press.isPressed || press.wasReleasedThisFrame)
                        HandleLevelSelectorTouchScroll(touch.primaryTouch.position.ReadValue(),
                            press.wasPressedThisFrame, press.isPressed, press.wasReleasedThisFrame);
                }
            }
            if (marketActive && !game.ShowChulengoResult)
            {
                Touchscreen marketTouch = Touchscreen.current;
                if (marketTouch != null)
                {
                    var press = marketTouch.primaryTouch.press;
                    if (press.isPressed || press.wasReleasedThisFrame)
                        HandleMarketTouchScroll(marketTouch.primaryTouch.position.ReadValue(),
                            press.wasPressedThisFrame, press.isPressed, press.wasReleasedThisFrame);
                }
            }
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

        public bool AccountMenuVisible => !introActive && (hubActive || badgesActive || rankingActive || marketActive);
        public bool HubAccountActionRendered => false;

        private void OnGUI()
        {
            bool enabled = GUI.enabled;
            if (RewardShutterActive) GUI.enabled = false;
            DrawGameGui();
            GUI.enabled = enabled;
            DrawRewardShutter();
        }

        private void DrawGameGui()
        {
            if (game == null) return;
            if (!startupArtReady)
            {
                // Show the real cover as soon as it is available; do not interpose a generic loading screen.
                DrawIntroScreen(coverArt, Color.white, ScaleMode.ScaleAndCrop);
                return;
            }
            Styles();
            Vector2 viewSize = IsTowerDefenseNavigationActive ? TowerDefenseRenderSize(Camera.main) : new Vector2(Screen.width, Screen.height);
            Rect v=CanvasViewport(viewSize,Screen.safeArea); if(v.width<=0)return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            projectedCounterTopY = StreetSceneLayout.CounterTopInViewport(Screen.width, Screen.height, v);
            Matrix4x4 m=GUI.matrix; Color old=GUI.color;
            if (!viewResourcesReady || introActive)
            {
                // Intro foreground uses logical-canvas coordinates just like the hub/game UI.
                // DrawIntroScreen temporarily switches to identity for full-bleed background art.
                GUI.matrix = Matrix4x4.TRS(new Vector3(v.x, v.y), Quaternion.identity,
                    new Vector3(v.width / W, v.width / W, 1f));
                GUI.color = Color.white;
                float loadingElapsed = splashClockStarted ? Time.unscaledTime - introStarted : 0f;
                DrawIntro(loadingElapsed);
                GUI.color = old; GUI.matrix = m;
                return;
            }
            if (IsTowerDefenseNavigationActive)
            {
                DrawTowerDefenseUi(v);
                return;
            }
            bool chulengoResult = game.ShowChulengoResult;
            var sim = game.IsPlayerActive ? game.Sim : null;
            if (sim == null)
            {
                // Firebase may not have an owner yet; the account hub is still a real screen.
                // Gameplay/navigation destinations are gated in DispatchAction until auth resolves.
                if (hubActive && !chulengoResult)
                {
                    GUI.matrix=Matrix4x4.TRS(new Vector3(v.x,v.y),Quaternion.identity,new Vector3(v.width/W,v.width/W,1));
                    GUI.color=Color.white;
                    DrawHubScreen();
                }
                GUI.color = old; GUI.matrix = m;
                return;
            }
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
                if (sim.IsBonusTrack) { DrawBonusTrackHud(sim); DrawBonusTrackResultPanel(); }
                else { DrawCounters(sim); DrawRiotResult(); }
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
            if (sim.IsBonusTrack) DrawBonusTrackHud(sim);
            else DrawCounters(sim);
            // The same footer geometry drives rendering, touch targets and the decorative pennant gap.
            int[] catalogActions = CatalogUpgradeActions(sim);
            if (!sim.IsBonusTrack && (VictoryTransitionActive || levelFourSpecialistsTutorialActive || levelFiveSpecialistsTutorialActive ||
                levelSixRacingTutorialActive ||
                ShouldDrawUpgradeCards(sim.Phase, catalogActions.Length)))
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
                if (levelOneTutorialActive || levelTwoCocacoleroTutorialActive || levelThreePatyTutorialActive ||
                    levelFourSpecialistsTutorialActive || levelFiveSpecialistsTutorialActive || levelSixRacingTutorialActive) DrawLevelBadge(game.SelectedLevel, footer.LevelBadge);
                else ReadyPanel(footer);
            }
            else if (VictoryTransitionActive) DrawVictoryTransition();
            else if(sim.Phase==RoundPhase.Won||sim.Phase==RoundPhase.Lost) ResultPanel();
            else DrawLevelBadge(sim.LevelIndex, footer.LevelBadge);
            if (levelOneTutorialActive) DrawLevelOneTutorial();
            else if (levelTwoCocacoleroTutorialActive) DrawLevelTwoCocacoleroTutorial();
            else if (levelThreePatyTutorialActive) DrawLevelThreePatyTutorial();
            else if (levelFourSpecialistsTutorialActive) DrawLevelFourSpecialistsTutorial();
            else if (levelFiveSpecialistsTutorialActive) DrawLevelFiveSpecialistsTutorial();
            else if (levelSixRacingTutorialActive) DrawLevelSixRacingTutorial();
            GUI.color=old;GUI.matrix=m;
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
            if (!startupArtReady)
            {
                if (GUI.Button(bounds, caption)) activated?.Invoke();
                return;
            }
            Styles(); float previousScale = layoutVerticalScale; layoutVerticalScale = 1f;
            try { DrawStandardButton(bounds, int.MinValue, caption, true, activated); }
            finally { layoutVerticalScale = previousScale; }
        }

        public void DrawGoogleAccountButton(Rect bounds, string caption, Texture2D googleMark, System.Action activated)
        {
            if (!startupArtReady)
            {
                if (GUI.Button(bounds, caption)) activated?.Invoke();
                return;
            }
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
                var label = CachedStandardButtonLabelStyle(menuTitle, fontSize,
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
            // Sign-in resolves the player profile but must not replay the presentation or
            // bypass the hub's explicit Jugar -> selector -> Mercado flow.
            if (game == null || !game.IsPlayerActive) return;
            if (introActive && viewResourcesReady && SplashComplete) OpenHubFromIntro();
        }

        public void ReturnToHubAfterAuthenticationRequired()
        {
            if (!viewResourcesReady || introActive) return;
            levelSelectActive = false; levelOneTutorialActive = false; levelOneTutorialStep = 0;
            levelTwoCocacoleroTutorialActive = false; levelTwoCocacoleroTutorialStep = 0;
            levelThreePatyTutorialActive = false;
            levelFourSpecialistsTutorialActive = false; levelFourSpecialistsTutorialStep = 0;
            levelFiveSpecialistsTutorialActive = false; levelFiveSpecialistsTutorialStep = 0;
            marketActive = false; badgesActive = false; rankingActive = false; hubActive = true;
            marketFromSelector = false; pressedAction = 0;
        }

#if UNITY_EDITOR
        private static void ClearLevelOneTutorialMarkerForTesting()
        {
            if (!RepeatLevelOneTutorialForTesting || !PlayerPrefs.HasKey(LevelOneTutorialCompletedKey)) return;
            PlayerPrefs.DeleteKey(LevelOneTutorialCompletedKey);
            PlayerPrefs.Save();
        }
#endif

        private void OpenHubFromIntro()
        {
            if (!introActive) return;
#if UNITY_EDITOR
            ClearLevelOneTutorialMarkerForTesting();
#endif
            introActive = false; levelSelectActive = false; marketActive = false;
            badgesActive = false; rankingActive = false; settingsActive = false;
            hubActive = true; marketFromSelector = false;
            pressedAction = 0;
        }

        private static float IntroEase(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static Rect GrillRect(int productCount) => StreetWorkstationLayout.GrillBounds;
        private static Rect GrillRectForLevel(int productCount, int levelIndex) => StreetWorkstationLayout.GrillBounds;
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
        private static bool ShouldDrawStationProductBadge(int productIndex)
        {
            // Product 0 is represented by the loaded choripán table;
            // a second floating badge above the grill is visually redundant.
            return productIndex != 0;
        }
        private void ReadyPanel(ResponsiveFooterLayout footer)
        {
            // Prices are fixed in every location; Ready has no price-selection panel or hidden controls.
            DrawStandardButton(Start, 1, "JUGAR");
            DrawLevelBadge(game.SelectedLevel, footer.LevelBadge);
            DrawStandardButton(footer.ReadySelector, ReadyLevelSelectorAction, "CANCHAS");
        }
        private static Rect LevelButton(int i){return new Rect(108+i*67,912,57,35);}
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
            Font tutorialFont = googleSansFont;
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
