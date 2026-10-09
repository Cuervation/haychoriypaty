using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    /// <summary>Sprite-backed portrait renderer of actual worker/order state; never simulates deliveries.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StreetGame))]
    public sealed class StreetView : MonoBehaviour
    {
        private const float W = StreetSceneLayout.Width, H = StreetSceneLayout.Height;
        private StreetKitchenRenderer kitchenRenderer;
        private const string AllBoysCrestResource = "street-allboys-crest";
        private const string ChicagoCrestResource = "street-new-chicago-crest-v1";
        private const string ChicagoBackgroundResource = "street-background-chicago-v1";
        private const string VelezBackgroundResource = "street-background-velez-v1";
        private const string VelezCrestResource = "street-velez-crest-v1";
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
        private const string ReadySandwichesTableResource = "street-ready-sandwiches-table-v1";
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
        private static readonly Rect HudBar = new Rect(0, 0, 540, 68);
        private static readonly Rect HudCoins = new Rect(70, 10, 122, 45);
        private static readonly Rect HudTime = new Rect(228, 8, 96, 50);
        private static readonly Rect HudSales = new Rect(388, 10, 146, 45);
        private static readonly Rect HudChoriSales = new Rect(376, 6, 158, 26);
        private static readonly Rect HudCocaSales = new Rect(376, 34, 158, 26);
        private static readonly Rect CutoutHudGap = new Rect(232, 0, 70, 34);
        private const int HudClockFontSize = 48;
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
        private const int HirePremiumAction = 20, HireFerneteroAction = 21;
        private static int[] CatalogUpgradeActions(StreetSimulation sim)
        {
            var actions = new System.Collections.Generic.List<int> { 3 };
            foreach (var role in new[] { StreetWorkerRole.Parrillero, StreetWorkerRole.ParrilleroPremium, StreetWorkerRole.Cocacolero, StreetWorkerRole.Fernetero })
                if (sim.HasRole(role)) actions.Add(ActionForRole(role));
            return actions.ToArray();
        }
        private static int ActionForRole(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero ? 2
            : role == StreetWorkerRole.ParrilleroPremium ? HirePremiumAction : role == StreetWorkerRole.Cocacolero ? HireCocacoleroAction : HireFerneteroAction;
        private static StreetWorkerRole RoleForAction(int action) => action == 2 ? StreetWorkerRole.Parrillero
            : action == HirePremiumAction ? StreetWorkerRole.ParrilleroPremium : action == HireCocacoleroAction ? StreetWorkerRole.Cocacolero : StreetWorkerRole.Fernetero;
        private static string RoleCaption(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero ? "PARRILLERO"
            : role == StreetWorkerRole.ParrilleroPremium ? "PARRILLERO\nPREMIUM" : role == StreetWorkerRole.Cocacolero ? "COCACOLERO" : "FERNETERO";
        private static Rect CatalogUpgradeCardBounds(StreetSimulation sim, int action)
        {
            int[] actions = CatalogUpgradeActions(sim);
            int index = System.Array.IndexOf(actions, action), count = actions.Length;
            if (index < 0) return Rect.zero;
            if (count == 2) return index == 0 ? Speed : HireParrillero;
            if (count == 3) return new Rect(24 + index * 168, 710, 156, 120);
            if (count == 4) return new Rect(28 + (index % 2) * 250, 710 + (index / 2) * 116, 234, 104);
            return index < 3 ? new Rect(24 + index * 168, 740, 156, 100) : new Rect(28 + (index - 3) * 250, 848, 234, 100);
        }
        private Rect CatalogUpgradeTouchBounds(int action)
        {
            Rect rect = CatalogUpgradeCardBounds(game.Sim, action);
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
        private const int LevelSelectorPreviousAction = 17, LevelSelectorNextAction = 18, LevelSelectorPageSize = 6;
        private const int ReadyLevelSelectorAction = 19;
        private static readonly Rect ReadyLevelSelector = new Rect(161, 912, 218, 35);
        private static readonly Rect LevelSelectPrevious = new Rect(28, 666, 112, 48);
        private static readonly Rect LevelSelectNext = new Rect(400, 666, 112, 48);
        private int levelSelectPage;
        private static readonly Rect LevelSelectTitle = new Rect(28, 61, 484, 64);
        private static readonly Rect LevelSelectHint = new Rect(48, 718, 444, 28);
        private static readonly Rect LevelSelectBack = new Rect(126, 770, 288, 78);
        private const string VictoryPopupResource = "street-victory-popup-wood-v1";
        private static readonly Vector2 VictoryPopupArtSize = new Vector2(1122, 1402);
        // Normalized slots in the transparent artwork; text and pointer bounds use the same frame.
        private static readonly Rect VictoryExit = new Rect(.195f, .839f, .62f, .151f);
        private const string IntroBackdropResource = "street-cover-user-v5";
        private const string RiotBackdropResource = "street-riot-environment-v1";
        private const string RiotFanAtlasResource = "street-riot-fans-v1";
        private const string RiotImpactCloudResource = "street-riot-fight-cloud-v1";
        private const float RiotImpactCloudDuration = 1.55f;
        private const float RiotAngerRampSeconds = .55f, RiotBreakStartSeconds = .55f, RiotBreakTransitionSeconds = .9f, RiotFanFramesPerSecond = 6f;
        private const float RiotFanAtlasRowSplitY = 493f;
        private static readonly Rect RiotReturnButton = new Rect(88, 858, 364, 66);
        private StreetGame game;
        private int VisualLevelIndex => game != null && game.Sim != null ? game.Sim.LevelIndex : 0;
        private Texture2D backdrop, chicagoBackground, velezBackground, velezCrest, velezGrill, people, items, parrillero, parrilleroDiagonal, cocacolero, laterCocacolero, parrilleroIcon, upgradeWood, speedArrows, largeGrill, servingTable, gameCoin, muralArt, allBoysCrest, chicagoCrest, cocaBottle, beverageBarrel, chicagoGrill;
        private Texture2D clubPennantTexture;
        private string clubPennantThemeName;
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
        private Texture2D riverMural, riverFans, riverRiotFans;
        private Texture2D camionerosBackground;
        private Texture2D losRedondosBackground, losRedondosFrontFans, losRedondosWalkingFans;
        private Texture2D fourZoneGrill, premiumGrill, beerBarrel, fernetTable, readySandwichesTable;
        private Texture2D premiumWorker, premiumDiagonal, fernetWorker, fernetWorkerLater;
        private Texture2D victoryPopup;
        private Texture2D coverArt, titleLogo, introGlow, menuButton, hudBarTexture, cutoutHudBarTexture, riotBackdrop, riotFanAtlas, riotImpactCloud;
        private Font menuFont;
        private GUIStyle menuTitle, levelTitle;
        private bool introActive;
        private bool levelSelectActive;
        private float introStarted;
        private bool riotScreenActive;
        private float riotStartedAt;
        private const float CoverFadeDuration = .28f, CoverHoldDuration = 4f;
        private const float LogoFadeDuration = .35f, LogoHoldDuration = 4f;
        private const float LogoRevealAt = CoverFadeDuration + CoverHoldDuration;
        // Each artwork gets four full seconds after its own fade, not including the fade.
        private const float IntroDuration = LogoRevealAt + LogoFadeDuration + LogoHoldDuration;
        private bool MenuAvailable { get { return introActive && Time.unscaledTime - introStarted >= IntroDuration; } }
        private const string ParrilleroLabel = "Parrillero";
        private GUIStyle tiny, small, text, title, header, amount, invisible, hudNumber, upgradeTitle, upgradePercent, upgradePrice, riotCue;
        private int pressedAction, lastAction;
        private float feedbackUntil, feedbackScale;
        private float layoutVerticalScale = 1f;
        private float logicalCanvasHeight = H;
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
        private static readonly Rect SpeedArrowBounds = new Rect(114,224,1174,662);
        private void OnEnable()
        {
            game = GetComponent<StreetGame>();
            backdrop = Resources.Load<Texture2D>("street-background-open-street-v4");
            muralArt = Resources.Load<Texture2D>(MuralResource);
            allBoysCrest = Resources.Load<Texture2D>(AllBoysCrestResource);
            chicagoCrest = Resources.Load<Texture2D>(ChicagoCrestResource);
            chicagoBackground = Resources.Load<Texture2D>(ChicagoBackgroundResource);
            velezBackground = Resources.Load<Texture2D>(VelezBackgroundResource);
            velezCrest = Resources.Load<Texture2D>(VelezCrestResource);
            velezGrill = Resources.Load<Texture2D>(VelezGrillResource);
            ferroBackground = Resources.Load<Texture2D>(FerroBackgroundResource);
            independienteBackground = Resources.Load<Texture2D>(IndependienteBackgroundResource);
            racingBackground = Resources.Load<Texture2D>(RacingBackgroundResource);
            sanLorenzoBackground = Resources.Load<Texture2D>(SanLorenzoBackgroundResource);
            bocaBackground = Resources.Load<Texture2D>(BocaBackgroundResource);
            riverMural = Resources.Load<Texture2D>(RiverMuralResource);
            camionerosBackground = Resources.Load<Texture2D>(CamionerosBackgroundResource);
            losRedondosBackground = Resources.Load<Texture2D>(LosRedondosBackgroundResource);
            fourZoneGrill = Resources.Load<Texture2D>(FourZoneGrillResource);
            beerBarrel = Resources.Load<Texture2D>(BeerBarrelResource);
            fernetTable = Resources.Load<Texture2D>(FernetTableResource);
            readySandwichesTable = Resources.Load<Texture2D>(ReadySandwichesTableResource);
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
            servingTable = Resources.Load<Texture2D>("street-serving-table-v1");
            gameCoin = Resources.Load<Texture2D>("street-coin-gold-v2");
            coverArt = Resources.Load<Texture2D>(IntroBackdropResource);
            victoryPopup = Resources.Load<Texture2D>(VictoryPopupResource);
            riotBackdrop = Resources.Load<Texture2D>(RiotBackdropResource);
            riotFanAtlas = Resources.Load<Texture2D>(RiotFanAtlasResource);
            riotImpactCloud = Resources.Load<Texture2D>(RiotImpactCloudResource);
            titleLogo = Resources.Load<Texture2D>("street-logo");
            introGlow = CreateIntroGlow();
            menuButton = CreateMenuButton();
            hudBarTexture = CreateHudBar();
            cutoutHudBarTexture = CreateHudBarTexture(true);
            menuFont = Resources.Load<Font>("Menu/LuckiestGuy-Regular");
            premiumGrill = Resources.Load<Texture2D>("street-grill-premium-v1");
            premiumWorker = StreetWorkerAppearance.CreateVariant(parrillero, StreetWorkerRole.ParrilleroPremium, ParrilleroPoses, new Vector2(1315,1197));
            premiumDiagonal = StreetWorkerAppearance.CreateVariant(parrilleroDiagonal, StreetWorkerRole.ParrilleroPremium, ParrilleroDiagonalPoses, new Vector2(1536,1024));
            fernetWorker = StreetWorkerAppearance.CreateVariant(cocacolero, StreetWorkerRole.Fernetero, ChicagoCocacoleroPoses, new Vector2(1247,1261));
            fernetWorkerLater = StreetWorkerAppearance.CreateVariant(laterCocacolero, StreetWorkerRole.Fernetero, MatchedCocacoleroPoses, new Vector2(1247,1261));
            introActive = true;
            levelSelectActive = false;
            introStarted = Time.unscaledTime;
            tiny = small = text = title = header = amount = invisible = menuTitle = levelTitle = null;
            upgradeTitle = upgradePercent = upgradePrice = riotCue = null;
            pressedAction = lastAction = 0;
        }
        private void OnDisable()
        {
            // Release cached sprites before destroying their palette textures.
            if (kitchenRenderer != null) kitchenRenderer.ReleaseWorkers();
            if (introGlow != null) Destroy(introGlow);
            introGlow = null;
            if (menuButton != null) Destroy(menuButton);
            menuButton = null;
            if (hudBarTexture != null) Destroy(hudBarTexture);
            hudBarTexture = null;
            if (cutoutHudBarTexture != null) Destroy(cutoutHudBarTexture);
            cutoutHudBarTexture = null;
            if (clubPennantTexture != null) Destroy(clubPennantTexture);
            clubPennantTexture = null;
            clubPennantThemeName = null;
            foreach (Texture2D variant in new[] { premiumWorker, premiumDiagonal, fernetWorker, fernetWorkerLater })
                if (variant != null) Destroy(variant);
            premiumWorker = premiumDiagonal = fernetWorker = fernetWorkerLater = null;
        }
        private void Update()
        {
            if (introActive && !MenuAvailable) return;
#if !(UNITY_ANDROID && !UNITY_EDITOR) && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var touch = Touchscreen.current; var mouse = Mouse.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
                HandlePointer(touch.primaryTouch.position.ReadValue(), touch.primaryTouch.press.wasPressedThisFrame, touch.primaryTouch.press.wasReleasedThisFrame);
            else if (mouse != null) HandlePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame);
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
        private static Vector2 ScreenToLayoutPoint(Vector2 pixel, Vector2 screenSize, Rect viewport)
        {
            float scale = viewport.width / W;
            float verticalScale = CanvasLogicalHeight(viewport) / H;
            Vector2 point = (new Vector2(pixel.x, screenSize.y - pixel.y) - viewport.position) / scale;
            point.y /= verticalScale;
            return point;
        }
        private Rect LayoutRect(Rect rect) { rect.y *= layoutVerticalScale; return rect; }
        private void HandlePointer(Vector2 pixel, bool down, bool up)
        {
            if (game == null || game.Sim == null) return;
            Rect v = CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea);
            if (v.width <= 0) return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 p = ScreenToLayoutPoint(pixel, new Vector2(Screen.width, Screen.height), v);
            if (down) pressedAction = HitAction(p);
            if (!up) return;
            if (pressedAction != 0 && pressedAction == HitAction(p)) DispatchAction(pressedAction);
            pressedAction = 0;
        }
        private int HitAction(Vector2 p)
        {
            if (introActive)
                return !MenuAvailable ? 0 : MenuPlay.Contains(p) ? MenuPlayAction : MenuQuit.Contains(p) ? MenuQuitAction : 0;
            if (levelSelectActive)
            {
                if (LevelSelectBack.Contains(p)) return LevelSelectorBackAction;
                if (levelSelectPage > 0 && LevelSelectPrevious.Contains(p)) return LevelSelectorPreviousAction;
                if ((levelSelectPage + 1) * LevelSelectorPageSize < StreetSimulation.LevelNames.Length && LevelSelectNext.Contains(p)) return LevelSelectorNextAction;
                for (int i = levelSelectPage * LevelSelectorPageSize; i < Mathf.Min((levelSelectPage + 1) * LevelSelectorPageSize, StreetSimulation.LevelNames.Length); i++)
                    if (i <= game.UnlockedLevel && LevelSelectCardBounds(i).Contains(p)) return LevelSelectFirstAction + i;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Ready)
            {
                if (Start.Contains(p)) return 1;
                if (ReadyLevelSelector.Contains(p)) return ReadyLevelSelectorAction;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Playing)
            {
                foreach (int action in CatalogUpgradeActions(game.Sim))
                    if (CatalogUpgradeTouchBounds(action).Contains(p)) return action;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Lost) return RiotReturnButton.Contains(p) ? RiotReturnAction : 0;
            return game.Sim.Phase == RoundPhase.Won &&
                VictoryExitBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? VictoryExitAction : 0;
        }
        private void DispatchAction(int a)
        {
            if (introActive)
            {
                // Native Android and the desktop bridge share the same startup guard.
                if (!MenuAvailable) return;
                if (a == MenuPlayAction)
                {
                    introActive = false;
                    levelSelectActive = true;
                    levelSelectPage = game.Sim.LevelIndex / LevelSelectorPageSize;
                    pressedAction = 0;
                }
                else if (a == MenuQuitAction) QuitGame();
                return;
            }
            if (levelSelectActive)
            {
                if (a == LevelSelectorPreviousAction || a == LevelSelectorNextAction)
                {
                    levelSelectPage = Mathf.Clamp(levelSelectPage + (a == LevelSelectorNextAction ? 1 : -1),
                        0, (StreetSimulation.LevelNames.Length - 1) / LevelSelectorPageSize);
                    pressedAction = 0;
                    return;
                }
                if (a == LevelSelectorBackAction)
                {
                    levelSelectActive = false;
                    introActive = true;
                    introStarted = Time.unscaledTime - IntroDuration;
                    return;
                }
                if (a >= LevelSelectFirstAction && a < LevelSelectFirstAction + StreetSimulation.LevelNames.Length)
                {
                    int level = a - LevelSelectFirstAction;
                    bool selectionSucceeded = level <= game.UnlockedLevel && game.SelectLevel(level);
                    if (selectionSucceeded)
                    {
                        levelSelectActive = false;
                        game.StartRound();
                    }
                    lastAction = a; feedbackUntil = Time.unscaledTime + 0.22f; feedbackScale = selectionSucceeded ? 1.04f : 0.97f;
                    return;
                }
                return;
            }
            bool ok = true;
            if (a == ReadyLevelSelectorAction && game.Sim.Phase == RoundPhase.Ready)
            {
                levelSelectActive = true;
                levelSelectPage = game.Sim.LevelIndex / LevelSelectorPageSize;
                return;
            }
            if (a==1) game.StartRound(); else if (a==2) ok=game.TryHireParrillero(); else if (a==3) ok=game.TryUpgradeSpeed(); else if (a==HireCocacoleroAction || a==HirePremiumAction || a==HireFerneteroAction) ok=game.TryHire(RoleForAction(a));
            else if (a==4) ok=game.Retry(); else if (a==5) ok=game.NextLevel(); else if (a>=10&&a<15) {ok=game.SelectLevel(a-10);if(ok)game.StartRound();}
            else if (a == RiotReturnAction && game.Sim.Phase == RoundPhase.Lost)
            {
                ok = game.Retry();
                if (ok) { levelSelectActive = true; levelSelectPage = game.Sim.LevelIndex / LevelSelectorPageSize; }
            }
            else if (a == VictoryExitAction && game.Sim.Phase == RoundPhase.Won)
            {
                ok = game.Retry();
                if (ok) { levelSelectActive = true; levelSelectPage = game.Sim.LevelIndex / LevelSelectorPageSize; }
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
        private void OnGUI()
        {
            if (game==null||game.Sim==null) return;
            Styles(); Rect v=CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea); if(v.width<=0)return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            Matrix4x4 m=GUI.matrix; Color old=GUI.color;
            var sim=game.Sim;
            if (!introActive && sim.Phase != RoundPhase.Lost) DrawGameplayScreenFill();
            GUI.matrix=Matrix4x4.TRS(new Vector3(v.x,v.y),Quaternion.identity,new Vector3(v.width/W,v.width/W,1)); GUI.color=Color.white;
            if (introActive)
            {
                DrawIntro(Time.unscaledTime - introStarted);
                GUI.color = old; GUI.matrix = m;
                return; // Do not create hidden gameplay GUI controls behind the startup menu.
            }
            if (levelSelectActive)
            {
                DrawLevelSelector();
                GUI.color=old; GUI.matrix=m;
                return; // Keep the locked/unlocked selector as the only interactive layer.
            }
            if (sim.Phase == RoundPhase.Lost)
            {
                DrawRiotScene();
                DrawCounters(sim);
                DrawRiotResult(sim);
                GUI.color=old; GUI.matrix=m;
                return;
            }
            riotScreenActive = false;
            DrawBackdropLayers(logicalCanvasHeight, layoutVerticalScale);
            // Keep the expanded customer street clear; order icons live on their customers.
            DrawWaitingCrowd(sim);
            DrawClubCounter();
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
            for(int i=0;i<sim.Sales.Count;i++)
            {
                var s=sim.Sales[i];float a=Mathf.Clamp01(2.2f-s.Age);GUI.color=new Color(1,1,1,a);
                Item(new Rect(s.Position.x-27,s.Position.y-86-s.Age*33,23,23),10);
                Label(new Rect(s.Position.x-3,s.Position.y-86-s.Age*33,48,25),"+"+s.Amount,text);GUI.color=Color.white;
            }
            DrawCounters(sim);
            // Large catalogs need two rows; Ready navigation remains an unobstructed layer.
            if (sim.Phase != RoundPhase.Ready || CatalogUpgradeActions(sim).Length <= 3)
                foreach (int action in CatalogUpgradeActions(sim))
                {
                    bool speed = action == 3;
                    StreetWorkerRole role = RoleForAction(action);
                    Upgrade(CatalogUpgradeCardBounds(sim, action), action, speed ? 15 : 16,
                        speed ? "VELOCIDAD" : RoleCaption(role), speed ? sim.SpeedCost : sim.HireCostForRole(role),
                        speed ? sim.CanUpgradeSpeed : sim.CanHireRole(role));
                }
            if(sim.Phase==RoundPhase.Ready) ReadyPanel();
            else if(sim.Phase==RoundPhase.Won||sim.Phase==RoundPhase.Lost) ResultPanel();
            else DrawLevelBadge(sim.LevelIndex, CatalogLevelBadge());
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
            DrawBackdropLayers(logicalCanvasHeight, layoutVerticalScale);

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
            }

            DrawRiotWaitingCrowd(game != null ? game.Sim : null, elapsed);
            DrawClubCounter(1f - destruction);
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

        private void LateUpdate() { UpdateRiotState(); }
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

        private void DrawRiotResult(StreetSimulation sim)
        {
            GUI.color = new Color(.035f, .025f, .02f, .78f);
            GUI.DrawTexture(LayoutRect(new Rect(18, 48, 504, 122)), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            Label(new Rect(27, 53, 486, 54), "No llegaste a entregar\ntodos los pedidos.", header);
            DrawAnimatedGameOver(new Rect(54, 106, 432, 51), Mathf.Max(0f, Time.unscaledTime - riotStartedAt));

            DrawStandardButton(RiotReturnButton, RiotReturnAction, "VOLVER");
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
            menuTitle.fontSize = FitVictoryFont(menuTitle, "GAME OVER", 36, animated);
            menuTitle.alignment = TextAnchor.MiddleCenter;
            OutlineLabel(animated, "GAME OVER", menuTitle, fill, 2f);
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
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                GUI.color = Color.white;
                DrawBackdropLayers(fullHeight, fullHeight / H);
            }
            finally { GUI.matrix = previous; GUI.color = color; }
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
        private void DrawBackdropLayers(float canvasHeight, float verticalScale)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            // The root public ground/counter/operator floor is invariant; only scenery is themed.
            Color previous = GUI.color;
            GUI.color = theme.SceneryTint;
            if (backdrop != null)
                GUI.DrawTexture(StreetSceneLayout.BackgroundBounds(canvasHeight), backdrop, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            if (!string.IsNullOrEmpty(theme.MuralResource))
                DrawThemedMural(theme, verticalScale);
            else if (muralArt != null)
                DrawRaw(new Rect((W - W * verticalScale) * .5f, 0,
                    W * verticalScale, MuralBounds.height * verticalScale), muralArt, MuralSource, true, false);
            DrawClubPennants(canvasHeight, verticalScale);
        }

        private void DrawThemedMural(ClubVisualTheme theme, float verticalScale)
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
            if (VisualLevelIndex >= 5)
                // Prepared club compositions need a taller wall, not squeezed portraits or side blanks.
                // Only scenery expands; the master counter and all actor coordinates stay unchanged.
                band.height = Mathf.Min(W * source.height / source.width,
                    Mathf.Max(0f, StreetSceneLayout.CounterTop(H * verticalScale) - band.y));
            // Frontal wall panels may have different aspect ratios, never stretched portrait figures.
            Color previous = GUI.color;
            GUI.color = VisualLevelIndex == 9 ? new Color(.43f, .73f, .92f)
                : Color.Lerp(theme.PrimaryColor, new Color(.36f, .32f, .27f), .22f);
            GUI.DrawTexture(new Rect(0, 0, W, band.yMax), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawRaw(band, clubMuralTexture, source, false, false);
            GUI.color = previous;
        }

        private void DrawClubPennants(float canvasHeight, float verticalScale)
        {
            int levelIndex = VisualLevelIndex;
            // The original Floresta row is part of its background art; leave it pixel-identical.
            if (levelIndex == 0) return;

            ClubVisualTheme theme = ClubVisualTheme.ForLevel(levelIndex);
            if (clubPennantTexture == null || clubPennantThemeName != theme.ClubName)
            {
                if (clubPennantTexture != null) Destroy(clubPennantTexture);
                clubPennantTexture = ClubPennantRenderer.CreateTexture(theme);
                clubPennantThemeName = theme.ClubName;
            }

            float height = ClubPennantRenderer.LogicalHeight * verticalScale;
            Color previousColor = GUI.color;
            try
            {
                // Keep club colors opaque even when a caller is drawing a faded transition layer.
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, canvasHeight - height, W, height),
                    clubPennantTexture, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private void DrawLevelBadge(int levelIndex, Rect bounds)
        {
            if (VisualLevelIndex == RiverVisualLevelIndex) levelIndex = RiverVisualLevelIndex;
            string caption = (levelIndex == RiverVisualLevelIndex ? "River Plate / Núñez"
                : StreetSimulation.LevelNames[levelIndex]).ToUpperInvariant();
            Texture2D crest = levelIndex == 0 ? allBoysCrest : levelIndex == 1 ? chicagoCrest : levelIndex == 2 ? velezCrest : null;
            bool showCrest = crest != null;
            int originalSize = levelTitle.fontSize;
            float available = bounds.width - (showCrest ? Mathf.Min(32f, bounds.height) + 8f : 0f);
            float measured = levelTitle.CalcSize(new GUIContent(caption)).x;
            if (measured + 8f > available)
                levelTitle.fontSize = Mathf.Max(12, Mathf.FloorToInt(originalSize * (available - 8f) / measured));
            Rect[] layout = LevelBadgeLayout(bounds, levelTitle.CalcSize(new GUIContent(caption)).x + 8f, showCrest);
            if (showCrest) GUI.DrawTexture(LayoutRect(layout[0]), crest, ScaleMode.ScaleToFit, true);
            levelTitle.normal.textColor = new Color(.16f, .10f, .08f);
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
            float iconSize = includeCrest ? Mathf.Min(32f, bounds.height) : 0f;
            float gap = includeCrest ? 8f : 0f;
            float textWidth = Mathf.Clamp(preferredTextWidth, 0f, bounds.width - iconSize - gap);
            float start = bounds.center.x - (iconSize + gap + textWidth) * .5f;
            return new[] {
                new Rect(start, bounds.center.y - iconSize * .5f, iconSize, iconSize),
                new Rect(start + iconSize + gap, bounds.y, textWidth, bounds.height)
            };
        }
        private void DrawCounters(StreetSimulation sim)
        {
            // The status bar always uses the same edge-to-edge layout on every device.
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float previousVertical = layoutVerticalScale;
            Rect screen = ScreenBounds(new Vector2(Screen.width, Screen.height));
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(screen.width / W, screen.width / W, 1));
                layoutVerticalScale = 1f;
                GUI.color = Color.white;
                if (hudBarTexture != null) GUI.DrawTexture(HudBar, hudBarTexture, ScaleMode.StretchToFill, true);
                DrawHudIcon(new Rect(8, 4, 58, 56), 10);
                DrawHudNumber(HudCoins, sim.Coins.ToString());
                DrawHudNumber(HudTime, FormatRemainingTime(sim.TimeRemaining), HudClockFontSize);
                if (sim.LevelIndex == 1)
                {
                    // Two larger, independently fitted rows keep both Chicago goals readable.
                    DrawHudIcon(new Rect(338, 7, 31, 24), 0);
                    DrawHudIcon(new Rect(338, 35, 31, 24), 4);
                    DrawHudNumber(HudChoriSales, Mathf.Min(sim.ChoriDelivered, sim.Goal) + "/" + sim.Goal);
                    DrawHudNumber(HudCocaSales, Mathf.Min(sim.CocaDelivered, sim.Goal) + "/" + sim.Goal);
                }
                else if (sim.LevelIndex == 2)
                {
                    // Keep all three Vélez products recognizable beside the shared delivered-unit goal.
                    DrawHudIcon(new Rect(338, 1, 30, 20), 0);
                    DrawHudIcon(new Rect(338, 23, 30, 20), 1);
                    DrawHudIcon(new Rect(338, 45, 30, 20), 4);
                    DrawHudNumber(HudSales, sim.Delivered + "/" + sim.Goal);
                }
                else if (sim.LevelIndex >= 3)
                {
                    // A compact 3x3 product key fits the expanded Ferro/Rojo catalog without crowding sales.
                    for (int slot = 0; slot < sim.ProductCount; slot++)
                    {
                        Rect icon = new Rect(337 + (slot % 3) * 16, 3 + (slot / 3) * 20, 17, 18);
                        DrawHudIcon(icon, sim.GetAvailableProduct(slot));
                    }
                    DrawHudNumber(HudSales, sim.Delivered + "/" + sim.Goal);
                }
                else
                {
                    DrawHudIcon(new Rect(338, 10, 43, 45), sim.LevelIndex == 0 ? 0 : 18);
                    DrawHudNumber(HudSales, sim.Delivered + "/" + sim.Goal);
                }
            }
            finally { GUI.matrix = previous; GUI.color = color; layoutVerticalScale = previousVertical; }
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
        private void DrawHudNumber(Rect bounds, string value, int preferredFontSize = 40)
        {
            int originalSize = hudNumber.fontSize;
            int baseFontSize = Mathf.Max(12, preferredFontSize);
            hudNumber.fontSize = baseFontSize;
            Vector2 needed = hudNumber.CalcSize(new GUIContent(value));
            float fitWidth = (bounds.width - 4f) / Mathf.Max(1f, needed.x);
            float fitHeight = (bounds.height - 2f) / Mathf.Max(1f, needed.y);
            float fitScale = Mathf.Min(1f, fitWidth, fitHeight);
            if (fitScale < 1f)
                hudNumber.fontSize = Mathf.Max(12, Mathf.FloorToInt(baseFontSize * fitScale));
            hudNumber.normal.textColor = new Color(.22f, .10f, .045f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x, bounds.y + y, bounds.width, bounds.height), value, hudNumber);
            hudNumber.normal.textColor = Color.white;
            Label(bounds, value, hudNumber);
            hudNumber.fontSize = originalSize;
        }
        private static Texture2D CreateHudBar() => CreateHudBarTexture(false);
        private static Texture2D CreateHudBarTexture(bool cutout)
        {
            // Original code-authored artwork: no reference pixels or baked-in live numbers.
            const int width = 1080, height = 136;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Original amber status bar with clock and green sales capsule",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var shadow = new Rect(0, 6, 540, 62);
            var rim = new Rect(0, 0, 540, 62);
            var face = new Rect(2, 2, 536, 56);
            var coins = new Rect(10, 4, 186, 54);
            var time = new Rect(202, 4, 124, 54);
            var sales = new Rect(332, 4, 208, 54);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) / 2f, py = (height - y - .5f) / 2f;
                Color color = new Color(.20f, .08f, .015f, .45f * RoundedCoverage(px, py, shadow, 22));
                color = Over(color, new Color(.29f, .12f, .045f), RoundedCoverage(px, py, rim, 22));
                color = Over(color, Color.Lerp(new Color(1f, .84f, .39f), new Color(.83f, .38f, .08f), py / 60f), RoundedCoverage(px, py, face, 20));
                color = HudCapsule(color, px, py, coins, new Color(.96f, .66f, .20f), new Color(.62f, .27f, .07f));
                color = HudCapsule(color, px, py, time, new Color(.26f, .73f, .79f), new Color(.04f, .35f, .46f));
                color = HudCapsule(color, px, py, sales, new Color(.60f, .86f, .17f), new Color(.20f, .48f, .04f));
                // Original clock; on cutout phones its entire face sits below/beside the camera.
                float clockScale = cutout ? 1f : 1.2f;
                float dx = (px - 214f) / clockScale, dy = (py - (cutout ? 48f : 31f)) / clockScale;
                float radius = new Vector2(dx, dy).magnitude;
                color = Over(color, new Color(.26f, .13f, .04f), Mathf.Clamp01(10.5f - radius));
                color = Over(color, new Color(1f, .89f, .55f), Mathf.Clamp01(9.3f - radius));
                color = Over(color, new Color(1f, .98f, .85f), Mathf.Clamp01(7.8f - radius));
                bool minuteHand = Mathf.Abs(dx) < .8f && dy >= -5.5f && dy <= .8f;
                bool hourHand = dx >= -.5f && dx <= 4.8f && Mathf.Abs(dy - dx * .5f) < .9f;
                if (minuteHand || hourHand) color = new Color(.24f, .12f, .045f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
        private static Color HudCapsule(Color below, float x, float y, Rect bounds, Color top, Color bottom)
        {
            Color color = Over(below, new Color(.35f, .17f, .05f), RoundedCoverage(x, y, bounds, 23));
            var inside = new Rect(bounds.x + 1.5f, bounds.y + 1.5f, bounds.width - 3, bounds.height - 3);
            float coverage = RoundedCoverage(x, y, inside, 21.5f);
            float t = Mathf.Clamp01((y - inside.y) / inside.height);
            color = Over(color, Color.Lerp(top, bottom, t), coverage);
            return Over(color, Color.white, coverage * .28f * (1f - IntroEase(t / .5f)));
        }
        private static Texture2D CreateIntroGlow()
        {
            const int size = 128;
            var glow = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Intro radial light flash",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f;
                float dy = (y + .5f) / size * 2f - 1f;
                float alpha = Mathf.Exp(-(dx * dx + dy * dy) * 7f);
                pixels[y * size + x] = new Color(1f, .91f, .69f, alpha);
            }
            glow.SetPixels(pixels);
            glow.Apply(false, true);
            return glow;
        }
        private static Rect IntroScreenRect(Vector2 size)
        {
            return new Rect(0, 0, Mathf.Max(0, size.x), Mathf.Max(0, size.y));
        }
        private static void DrawIntroScreen(Texture2D texture, Color tint, ScaleMode scaleMode)
        {
            if (texture == null) return;
            // Decorative art fills physical pixels; foreground/input keep the safe-area canvas.
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.color = tint;
            GUI.DrawTexture(IntroScreenRect(new Vector2(Screen.width, Screen.height)), texture, scaleMode, true);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private void DrawIntro(float elapsed)
        {
            float coverFadeIn = IntroEase(elapsed / CoverFadeDuration);
            float logoFadeIn = IntroEase((elapsed - LogoRevealAt) / LogoFadeDuration);
            DrawIntroScreen(Texture2D.whiteTexture, Color.black, ScaleMode.StretchToFill);
            DrawIntroScreen(coverArt, new Color(1f, 1f, 1f, coverFadeIn), ScaleMode.ScaleAndCrop);
            // The selected full-bleed cover already contains the Parrillero; no duplicate cutout.

            float shadeAlpha = .48f * logoFadeIn;
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.035f, .025f, .045f, shadeAlpha), ScaleMode.StretchToFill);

            Rect logoRect = new Rect(94f, 292f, 352f, 345f);
            float logoScale = Mathf.Lerp(.76f, 1f, logoFadeIn);
            logoRect = new Rect(W * .5f - logoRect.width * logoScale * .5f,
                                H * .5f - logoRect.height * logoScale / (2f * layoutVerticalScale),
                                logoRect.width * logoScale, logoRect.height * logoScale);

            float flashTime = elapsed - (LogoRevealAt + LogoFadeDuration + .07f);
            if (flashTime >= 0f && introGlow != null)
            {
                float glowProgress = IntroEase(flashTime / .72f);
                float glowAlpha = .9f * (1f - glowProgress) * logoFadeIn;
                float glowScale = Mathf.Lerp(.7f, 1.9f, glowProgress);
                float glowHeight = 460f * glowScale;
                Rect glowRect = new Rect(W * .5f - 235f * glowScale,
                                         H * .5f - glowHeight / (2f * layoutVerticalScale),
                                         470f * glowScale, glowHeight);
                GUI.color = new Color(1f, .91f, .72f, glowAlpha);
                GUI.DrawTexture(LayoutRect(glowRect), introGlow, ScaleMode.StretchToFill, true);

                float flashAlpha = .32f * (1f - IntroEase(flashTime / .18f)) * logoFadeIn;
                if (flashAlpha > .001f)
                {
                    DrawIntroScreen(Texture2D.whiteTexture, new Color(1f, 1f, .96f, flashAlpha), ScaleMode.StretchToFill);
                }
            }

            GUI.color = new Color(1f, 1f, 1f, logoFadeIn);
            if (titleLogo != null) GUI.DrawTexture(LayoutRect(logoRect), titleLogo, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            if (MenuAvailable)
            {
                DrawStandardButton(MenuPlay, MenuPlayAction, "JUGAR");
                DrawStandardButton(MenuQuit, MenuQuitAction, "SALIR");
            }
        }
        private void DrawLevelSelector()
        {
            FillRect(new Rect(0, 0, W, logicalCanvasHeight), new Color(.035f, .025f, .025f, .58f));
            OutlineLabel(LevelSelectTitle, "ELEGÍ TU CANCHA", menuTitle, new Color(1f, .91f, .72f), 2f);
            for (int i = levelSelectPage * LevelSelectorPageSize; i < Mathf.Min((levelSelectPage + 1) * LevelSelectorPageSize, StreetSimulation.LevelNames.Length); i++) DrawLevelSelectCard(i);
            DrawStandardButton(LevelSelectPrevious, LevelSelectorPreviousAction, "ATRÁS", levelSelectPage > 0);
            DrawStandardButton(LevelSelectNext, LevelSelectorNextAction, "MÁS", (levelSelectPage + 1) * LevelSelectorPageSize < StreetSimulation.LevelNames.Length);
            Label(new Rect(148, 675, 244, 30), "CANCHAS " + (levelSelectPage + 1) + " / " + ((StreetSimulation.LevelNames.Length + LevelSelectorPageSize - 1) / LevelSelectorPageSize), small);
            Label(LevelSelectHint, "Superá cada cancha para desbloquear la siguiente", small);
            DrawStandardButton(LevelSelectBack, LevelSelectorBackAction, "VOLVER");
        }
        private static Rect LevelSelectCardBounds(int index)
        {
            const float width = 236f, height = 150f;
            int slot = index % LevelSelectorPageSize;
            int row = slot / 2, column = slot % 2;
            float x = index == StreetSimulation.LevelNames.Length - 1 && column == 0 ? (W - width) * .5f : 24f + column * 256f;
            return new Rect(x, 154f + row * 174f, width, height);
        }
        private void DrawLevelSelectCard(int index)
        {
            Rect bounds = LevelSelectCardBounds(index);
            bool unlocked = index <= game.UnlockedLevel;
            Color frame = unlocked ? new Color(.94f, .62f, .18f) : new Color(.24f, .20f, .18f);
            FillRect(new Rect(bounds.x - 2, bounds.y - 2, bounds.width + 4, bounds.height + 4), frame);
            FillRect(bounds, new Color(.10f, .07f, .045f));
            Rect artwork = new Rect(bounds.x + 3, bounds.y + 3, bounds.width - 6, 106);
            DrawLevelSelectArtwork(index, artwork);

            Rect caption = new Rect(bounds.x + 3, bounds.y + 109, bounds.width - 6, 38);
            FillRect(caption, new Color(.10f, .055f, .028f, .93f));
            string number = "NIVEL " + (index + 1);
            int oldSize = menuTitle.fontSize;
            menuTitle.fontSize = 19;
            OutlineLabel(new Rect(caption.x + 2, caption.y - 1, caption.width - 4, 20), number, menuTitle,
                unlocked ? Color.white : new Color(.76f, .72f, .67f), 1.3f);
            menuTitle.fontSize = oldSize;

            string name = StreetSimulation.LevelNames[index].ToUpperInvariant();
            int oldLevelSize = levelTitle.fontSize;
            levelTitle.fontSize = 15;
            float measured = levelTitle.CalcSize(new GUIContent(name)).x;
            if (measured > caption.width - 10)
                levelTitle.fontSize = Mathf.Max(10, Mathf.FloorToInt(levelTitle.fontSize * (caption.width - 10) / measured));
            OutlineLabel(new Rect(caption.x + 4, caption.y + 17, caption.width - 8, 20), name, levelTitle,
                unlocked ? new Color(1f, .89f, .62f) : new Color(.72f, .69f, .65f), 1.1f);
            levelTitle.fontSize = oldLevelSize;

            if (!unlocked)
            {
                FillRect(artwork, new Color(.015f, .012f, .012f, .60f));
                Item(new Rect(artwork.center.x - 14, artwork.center.y - 16, 28, 34), 17);
            }
            else
            {
                Rect tag = new Rect(artwork.xMax - 83, artwork.y + 6, 77, 19);
                FillRect(tag, new Color(.20f, .52f, .09f, .94f));
                Label(tag, "DISPONIBLE", tiny);
            }

            if (unlocked && GUI.Button(LayoutRect(bounds), "", invisible))
                NativeAction(LevelSelectFirstAction + index);
        }
        private void DrawLevelSelectArtwork(int index, Rect bounds)
        {
            if (index == 0 && muralArt != null)
            {
                DrawRaw(LayoutRect(bounds), muralArt, AllBoysSelectorMuralSource, true, false);
                if (allBoysCrest != null)
                    GUI.DrawTexture(LayoutRect(new Rect(bounds.x + 5, bounds.y + 5, 28, 32)), allBoysCrest, ScaleMode.ScaleToFit, true);
                return;
            }
            if (index == 1 && chicagoBackground != null)
            {
                DrawRaw(LayoutRect(bounds), chicagoBackground, ChicagoSelectorMuralSource, true, false);
                if (chicagoCrest != null)
                    GUI.DrawTexture(LayoutRect(new Rect(bounds.x + 5, bounds.y + 5, 28, 32)), chicagoCrest, ScaleMode.ScaleToFit, true);
                return;
            }
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(index);
            if (index >= 2 && !string.IsNullOrEmpty(theme.MuralResource))
            {
                Texture2D art = Resources.Load<Texture2D>(theme.MuralResource);
                if (art != null)
                {
                    Rect source = theme.MuralSource;
                    DrawRaw(LayoutRect(bounds), art, new Rect(source.x * art.width, source.y * art.height,
                        source.width * art.width, source.height * art.height), false, false);
                    FillRect(new Rect(bounds.x, bounds.y, bounds.width, 5), theme.PrimaryColor);
                    if (index == 2 && velezCrest != null)
                        GUI.DrawTexture(LayoutRect(new Rect(bounds.x + 5, bounds.y + 5, 30, 34)), velezCrest, ScaleMode.ScaleToFit, true);
                    return;
                }
            }

            // Later locations preview their expanding product lineup on the original game grill art.
            if (largeGrill != null) GUI.DrawTexture(LayoutRect(bounds), largeGrill, ScaleMode.ScaleAndCrop, true);
            Color accent = LevelSelectAccent(index);
            FillRect(new Rect(bounds.x, bounds.y, bounds.width, 6), accent);
            FillRect(new Rect(bounds.x, bounds.y + 6, bounds.width, 2), new Color(1f, 1f, 1f, .8f));
            int[] featured = LaterLevelCardProducts[Mathf.Clamp(index - 2, 0, LaterLevelCardProducts.Length - 1)];
            float iconWidth = 43f, gap = 12f;
            float start = bounds.center.x - (iconWidth * featured.Length + gap * (featured.Length - 1)) * .5f;
            for (int i = 0; i < featured.Length; i++)
            {
                Rect icon = new Rect(start + i * (iconWidth + gap), bounds.y + 38, iconWidth, 43);
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
        private void DrawStandardButton(Rect bounds, int action, string caption, bool enabled = true)
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
                int fontSize = FitStandardButtonFontSize(menuTitle, caption, bounds);
                var captionStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize,
                    interactive ? Color.white : new Color(.80f, .87f, .94f));
                var borderStyle = CreateStandardButtonLabelStyle(menuTitle, fontSize, new Color(.035f, .075f, .16f));
                float textScale = bounds.height / 92f;
                Rect letters = new Rect(drawn.x, drawn.y - 2f * textScale,
                    drawn.width, drawn.height - 8f * textScale);
                float outline = 2.2f * captionStyle.fontSize / 38f;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (x != 0 || y != 0)
                        GUI.Label(new Rect(letters.x + x * outline, letters.y + y * outline,
                            letters.width, letters.height), caption, borderStyle);
                GUI.Label(letters, caption, captionStyle);
                GUI.enabled = interactive;
                if (GUI.Button(hitBounds, "", invisible)) NativeAction(action);
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
                    DrawCustomer(customer);
                }
            }
            finally { GUI.EndGroup(); }
        }
        private void DrawClubCounter(float opacity = 1f)
        {
            if (backdrop == null || opacity <= 0f) return;
            // Already canvas-space geometry: applying LayoutRect again would move the counter on tall screens.
            Rect destination = StreetSceneLayout.CounterBounds(logicalCanvasHeight);
            Color previous = GUI.color;
            try
            {
                Color tint = ClubVisualTheme.ForLevel(game.Sim.LevelIndex).SceneryTint;
                tint.a = Mathf.Clamp01(opacity);
                GUI.color = tint;
                DrawRaw(destination, backdrop, StreetSceneLayout.CounterSource, true, false);
            }
            finally { GUI.color = previous; }
        }
        private float CustomerCounterTopY()
        {
            return StreetSceneLayout.CounterTop(logicalCanvasHeight);
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
        private void DrawCustomer(StreetCustomer c)
        {
            Vector2 position = CustomerViewPosition(c.Position);
            bool walking=c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving||c.State==StreetCustomerState.Advancing;
            DrawCustomerBody(c, walking);
            if(c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving)return;
            int pendingLines = c.PendingOrderLineCount;
            if (pendingLines > 0)
            {
                int visibleLines = Mathf.Min(StreetCustomer.MaxVisibleOrderProducts, pendingLines);
                float quantityWidth = 20f;
                for (int i = 0; i < visibleLines; i++)
                    quantityWidth = Mathf.Max(quantityWidth, text.CalcSize(new GUIContent(c.GetVisibleOrderLine(i).Remaining.ToString())).x);
                // Transform the anchor once; local content offsets must not grow on tall screens.
                Rect bubble = OrderBubbleLayoutBounds(OrderBubbleBounds(position, pendingLines, quantityWidth), layoutVerticalScale);
                DrawOrderBubble(bubble);
                for (int i = 0; i < visibleLines; i++)
                    DrawOrderLine(OrderLineBounds(bubble, i, visibleLines), c.GetVisibleOrderLine(i));
                if (c.HiddenOrderLineCount > 0)
                    GUI.Label(OrderMoreBounds(bubble), "+" + c.HiddenOrderLineCount + " más", tiny);
            }
            // A sprite-backed patience strip; no placeholder shape stands in for game art.
            GUI.color=new Color(.32f,.7f,.32f);Item(new Rect(position.x-22,position.y-64,44*c.PatienceFraction,4),13,true);GUI.color=Color.white;
        }
        private const float OrderRowHeight = 25f;
        private const float OrderPaddingY = 8f;
        private const float OrderTailHeight = 8f;
        private const float OrderFooterHeight = 14f;

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
            bubble.y = bubble.yMax * verticalScale - bubble.height;
            return bubble;
        }

        private static Rect OrderLineBounds(Rect bubble, int row, int visibleRows) =>
            new Rect(bubble.x + 5f, bubble.y + OrderPaddingY + row * OrderRowHeight, bubble.width - 10f, OrderRowHeight);

        private static Rect OrderMoreBounds(Rect bubble) =>
            new Rect(bubble.x + 4f, bubble.yMax - OrderTailHeight - OrderPaddingY - 12f, bubble.width - 8f, 12f);

        private static Rect OrderBubbleSlice(Rect bounds, int column, int row, float borderX, float borderY)
        {
            float x = column == 0 ? bounds.x : column == 1 ? bounds.x + borderX : bounds.xMax - borderX;
            float y = row == 0 ? bounds.y : row == 1 ? bounds.y + borderY : bounds.yMax - borderY;
            return new Rect(x, y, column == 1 ? bounds.width - borderX * 2f : borderX,
                row == 1 ? bounds.height - borderY * 2f : borderY);
        }

        private void DrawOrderBubble(Rect bounds)
        {
            if (items == null) return;
            // Reuse the authored speech shape: only its center/edge runs stretch, not the rounded corners.
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                DrawRaw(OrderBubbleSlice(bounds, column, row, 20f, 15f), items,
                    OrderBubbleSlice(Items[11], column, row, 84f, 65f), true, false);
        }

        private static Rect OrderIconBounds(Rect row) => new Rect(row.x + 2f, row.y + 1f, 26f, row.height - 2f);
        private static Rect OrderQuantityBounds(Rect row) => new Rect(row.x + 30f, row.y, row.width - 32f, row.height);

        private static int FitOrderQuantityFontSize(GUIStyle style, string value, float maxWidth)
        {
            int original = style.fontSize;
            float measured = style.CalcSize(new GUIContent(value)).x;
            return measured > maxWidth ? Mathf.Max(11, Mathf.FloorToInt(original * maxWidth / measured)) : original;
        }

        private void DrawOrderLine(Rect row, StreetOrderLine line)
        {
            if (line == null || line.Remaining <= 0) return;
            DrawProductIcon(OrderIconBounds(row), line.Product, false);
            int originalFontSize = text.fontSize;
            text.fontSize = FitOrderQuantityFontSize(text, line.Remaining.ToString(), OrderQuantityBounds(row).width);
            GUI.Label(OrderQuantityBounds(row), line.Remaining.ToString(), text);
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
            bool moving=w.State==StreetWorkerState.ToStation||w.State==StreetWorkerState.ToCounter;
            int levelIndex = game.Sim.LevelIndex;
            Texture2D cocacoleroArt = CocacoleroArtForLevel(levelIndex);
            bool beverageRole = w.Role == StreetWorkerRole.Cocacolero || w.Role == StreetWorkerRole.Fernetero;
            bool isCocacolero = beverageRole && cocacoleroArt != null;
            if (w.Role == StreetWorkerRole.Fernetero)
                cocacoleroArt = UsesMatchedCocacoleroArt(levelIndex) && fernetWorkerLater != null ? fernetWorkerLater : fernetWorker;
            Vector2 movement=w.Target-w.Position;
            int frame=moving?SelectParrilleroWalkFrame(movement,w.AnimationTime):0;
            if(w.State==StreetWorkerState.Pickup)frame=7;
            if(w.State==StreetWorkerState.Handoff)frame=3;
            float bob=moving?Mathf.Sin(w.AnimationTime*16)*1.5f:0;
            // Preserve the original90×98worker shape while anchoring its feet to the
            // same vertically adapted logical position as the native kitchen projection.
            float stationOffset = 98f * (layoutVerticalScale - 1f) / Mathf.Max(.01f, layoutVerticalScale);
            bob += stationOffset;
            Rect workerRect=new Rect(w.Position.x-45,w.Position.y-98+bob,90,98);
            if(isCocacolero)
            {
                if(frame>=16)
                {
                    int phase=((int)(w.AnimationTime*8))%2;
                    float x=Mathf.Abs(movement.x),y=Mathf.Abs(movement.y);
                    frame=x>y?9+phase:(movement.y>0?1+phase:5+phase);
                }
                bool flip=game.Sim.LevelIndex==1
                    ? (w.State == StreetWorkerState.Pickup ? !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium)
                        : ChicagoWorkerFlip(true,frame,movement,false))
                    : w.State == StreetWorkerState.Pickup ? !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium)
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
        private static int SelectParrilleroWalkFrame(Vector2 movement,float animationTime)
        {
            int phase=((int)(animationTime*8))%2;
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
        private void DrawServingTable()
        {
            // Reuse Floresta's actual table art, so its aspect, silhouette and legs stay identical in every club.
            if (servingTable != null && game.Sim.HasRole(StreetWorkerRole.Parrillero))
                Draw(StreetWorkstationLayout.TableBounds(game.Sim.HasParrilleroPremium), servingTable,
                    new Rect(0, 0, servingTable.width, servingTable.height), false, false);
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
            if(upgradeWood!=null)Draw(drawn,upgradeWood,UpgradeWoodBounds,true,false);else Item(drawn,12,true);
            GUI.color=old;
            bool compact = r.width < 200;
            int titleSize = upgradeTitle.fontSize;
            if (r.height < 120) upgradeTitle.fontSize = action == HirePremiumAction ? 12 : (compact ? 15 : 17);
            else if (compact) upgradeTitle.fontSize = 15;
            UpgradeOutline(UpgradeTitleBounds(drawn),name,upgradeTitle,new Color(1f,.9f,.69f));
            upgradeTitle.fontSize = titleSize;
            string statusText = action == 3
                ? "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "  ·  +10%"
                : "EQUIPO " + game.Sim.WorkerCount(RoleForAction(action));
            if (compact && action == 3) statusText = "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "\n+10%";
            Rect statusBounds = UpgradeStatusBounds(drawn);
            int originalStatusSize = upgradePercent.fontSize;
            if (compact) upgradePercent.fontSize = 13;
            upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, statusText, statusBounds.width);
            UpgradeOutline(statusBounds,statusText,upgradePercent,new Color(1f,.76f,.22f));
            upgradePercent.fontSize = originalStatusSize;
            Rect portrait = compact ? new Rect(drawn.x+9,drawn.y+39,43,43) : new Rect(drawn.x+24,drawn.y+26,68,68);
            if(action==3)
            {
                if(speedArrows!=null)Draw(compact ? new Rect(drawn.x+10,drawn.y+48,48,32) : new Rect(drawn.x+20,drawn.y+36,76,48),speedArrows,SpeedArrowBounds,false,false);
            }
            else if((action==HireCocacoleroAction || action==HireFerneteroAction) && CocacoleroArtForLevel(game.Sim.LevelIndex)!=null)
            {
                int levelIndex = game.Sim.LevelIndex;
                Texture2D art = action == HireFerneteroAction ? (UsesMatchedCocacoleroArt(levelIndex) ? fernetWorkerLater : fernetWorker) : CocacoleroArtForLevel(levelIndex);
                bool laterStyle = UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null;
                Draw(portrait,art,CocacoleroPoseForLevel(levelIndex,0),laterStyle,false);
            }
            else if (action == HirePremiumAction && premiumWorker != null) Draw(portrait,premiumWorker,ParrilleroPoses[0],false,false);
            else Item(portrait,icon);
            string priceText=cost>0?cost.ToString():"MAX";
            if(cost>0)Item(UpgradeCoinBounds(drawn),10);
            int originalPriceSize=upgradePrice.fontSize;
            if (compact || r.height < 120) upgradePrice.fontSize = 24;
            upgradePrice.fontSize=FitUpgradePriceFontSize(upgradePrice,priceText,UpgradePriceTextBounds(drawn).width);
            Label(UpgradePriceTextBounds(drawn),priceText,upgradePrice);
            upgradePrice.fontSize=originalPriceSize;
            bool prev=GUI.enabled;GUI.enabled=available;
            if(GUI.Button(LayoutRect(r),"",invisible))NativeAction(action);GUI.enabled=prev;
            layoutVerticalScale = previousVertical;
        }
        private static Rect UpgradeTitleBounds(Rect card){return card.width < 200 ? new Rect(card.x+6,card.y+6,card.width-12,29) : new Rect(card.x+94,card.y+6,card.width-104,32);}
        private static Rect UpgradeStatusBounds(Rect card){return card.width < 200 ? new Rect(card.x+62,card.y+38,card.width-70,29) : new Rect(card.x+94,card.y+35,card.width-104,22);}
        private static Rect UpgradeCoinBounds(Rect card){return card.width < 200 ? new Rect(card.x+63,card.y+card.height*.64f,18,18) : new Rect(card.x+101,card.y+(card.height < 120 ? 64 : 74),21,21);}
        private static Rect UpgradePriceTextBounds(Rect card){return card.width < 200 ? new Rect(card.x+82,card.y+card.height*.60f,card.width-90,30) : new Rect(card.x+126,card.y+(card.height < 120 ? 60 : 68),card.width-145,30);}
        private static int FitUpgradePriceFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=style.CalcSize(new GUIContent(value)).x;
            return needed>maxWidth-2?Mathf.Max(16,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private static int FitUpgradeStatusFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=style.CalcSize(new GUIContent(value)).x;
            return needed>maxWidth-2?Mathf.Max(11,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private void UpgradeOutline(Rect bounds,string value,GUIStyle style,Color fill)
        {
            Color previous=style.normal.textColor;style.normal.textColor=new Color(.12f,.045f,.018f,1);
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=0||y!=0)
                Label(new Rect(bounds.x+x,bounds.y+y,bounds.width,bounds.height),value,style);
            style.normal.textColor=fill;Label(bounds,value,style);style.normal.textColor=previous;
        }
        private void ReadyPanel()
        {
            // Prices are fixed in every location; Ready has no price-selection panel or hidden controls.
            DrawStandardButton(Start, 1, "JUGAR");
            ReadyLevels();
        }
        private void ReadyLevels()
        {
            DrawLevelBadge(game.SelectedLevel, ReadyLevelBadge);
            DrawStandardButton(ReadyLevelSelector, ReadyLevelSelectorAction, "CANCHAS");
        }
        private static Rect LevelButton(int i){return new Rect(108+i*67,912,57,35);}
        private static Rect VictoryPopupBounds(float canvasHeight)
        {
            float scale = Mathf.Min((W - 24f) / VictoryPopupArtSize.x,
                Mathf.Max(1f, canvasHeight - 32f) / VictoryPopupArtSize.y);
            Vector2 size = VictoryPopupArtSize * scale;
            return new Rect((W - size.x) * .5f, (canvasHeight - size.y) * .5f, size.x, size.y);
        }
        private static Rect VictorySlot(Rect frame, Rect normalized) => new Rect(
            frame.x + normalized.x * frame.width, frame.y + normalized.y * frame.height,
            normalized.width * frame.width, normalized.height * frame.height);
        private Rect VictoryExitBounds() => VictorySlot(VictoryPopupBounds(logicalCanvasHeight), VictoryExit);
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
            return Mathf.Max(10, Mathf.FloorToInt(preferred * scale));
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
        private void ResultPanel()
        {
            // Physical-screen dimmer covers the HUD and safe-area insets too.
            DrawIntroScreen(Texture2D.whiteTexture, new Color(0f, 0f, 0f, .63f), ScaleMode.StretchToFill);
            Rect frame = VictoryPopupBounds(logicalCanvasHeight);
            float previousVertical = layoutVerticalScale;
            try
            {
                // Uniform popup coordinates: never stretch its artwork, type or hit area vertically.
                layoutVerticalScale = 1f;
                if (victoryPopup != null) GUI.DrawTexture(frame, victoryPopup, ScaleMode.StretchToFill, true);
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

                VictoryText(VictorySlot(frame, new Rect(.333f, .390f, .20f, .092f)),
                    "VENTAS", Mathf.RoundToInt(24f * fontScale), brown, TextAnchor.MiddleLeft);
                string sales = game.Sim.LevelIndex == 1
                    ? "CHORI " + Mathf.Min(game.Sim.ChoriDelivered, game.Sim.Goal) + "/" + game.Sim.Goal
                        + "\nCOCA " + Mathf.Min(game.Sim.CocaDelivered, game.Sim.Goal) + "/" + game.Sim.Goal
                    : Mathf.Min(game.Sim.Delivered, game.Sim.Goal) + "/" + game.Sim.Goal;
                VictoryText(VictorySlot(frame, new Rect(.554f, .382f, .371f, .105f)), sales,
                    Mathf.RoundToInt((game.Sim.LevelIndex == 1 ? 27f : 40f) * fontScale), gold, TextAnchor.MiddleCenter);
                VictoryText(VictorySlot(frame, new Rect(.333f, .534f, .245f, .104f)),
                    "MONEDAS\nGANADAS", Mathf.RoundToInt(25f * fontScale), brown, TextAnchor.MiddleLeft);
                VictoryText(VictorySlot(frame, new Rect(.600f, .532f, .325f, .104f)),
                    game.Sim.CoinsEarned.ToString(), Mathf.RoundToInt(42f * fontScale), gold, TextAnchor.MiddleCenter);
                VictoryText(VictorySlot(frame, new Rect(.333f, .686f, .245f, .104f)),
                    "TIEMPO\nSOBRANTE", Mathf.RoundToInt(25f * fontScale), brown, TextAnchor.MiddleLeft);
                VictoryText(VictorySlot(frame, new Rect(.588f, .685f, .337f, .104f)),
                    FormatVictoryTime(game.Sim.TimeRemaining), Mathf.RoundToInt(42f * fontScale), gold, TextAnchor.MiddleCenter);
                DrawStandardButton(VictorySlot(frame, VictoryExit), VictoryExitAction, "SALIR");
            }
            finally { layoutVerticalScale = previousVertical; }
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
            upgradePrice.normal.textColor=new Color(.25f,.09f,.025f);
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
        private static void DrawRaw(Rect dest,Texture2D texture,Rect src,bool stretch,bool flip)
        {
            if(!stretch){float scale=Mathf.Min(dest.width/src.width,dest.height/src.height);dest=new Rect(dest.center.x-src.width*scale*.5f,dest.yMax-src.height*scale,src.width*scale,src.height*scale);}
            Rect uv=new Rect(src.x/texture.width,1-src.yMax/texture.height,src.width/texture.width,src.height/texture.height);
            if(flip){uv.x+=uv.width;uv.width=-uv.width;}GUI.DrawTextureWithTexCoords(dest,texture,uv,true);
        }
    }
}
