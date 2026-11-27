using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Entrada de puntero, scroll táctil y despacho de acciones. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void HandleLevelSelectorTouchScroll(Vector2 pixel, bool down, bool held, bool up)
        {
            if (!levelSelectActive || game == null) return;
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            if (viewport.width <= 0f) return;
            logicalCanvasHeight = CanvasLogicalHeight(viewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 point = ScreenToLayoutPoint(pixel, new Vector2(Screen.width, Screen.height), viewport);
            point.y *= layoutVerticalScale;

            if (down)
            {
                levelSelectTouchHeld = LevelSelectorViewportBounds(logicalCanvasHeight).Contains(point);
                levelSelectTouchDownY = point.y;
                levelSelectTouchDownScroll = levelSelectScroll;
                levelSelectTouchDragging = false;
                levelSelectIgnoreNativeTapUntilFrame = -1;
            }
            if (held && levelSelectTouchHeld)
            {
                float delta = levelSelectTouchDownY - point.y;
                if (Mathf.Abs(delta) > 8f) levelSelectTouchDragging = true;
                if (levelSelectTouchDragging)
                    levelSelectScroll = Mathf.Clamp(levelSelectTouchDownScroll + delta, 0f,
                        LevelSelectorMaxScroll(logicalCanvasHeight));
            }
            if (up)
            {
                if (levelSelectTouchDragging)
                    levelSelectIgnoreNativeTapUntilFrame = Time.frameCount + 1;
                levelSelectTouchHeld = false;
                levelSelectTouchDragging = false;
            }
        }
        private bool SuppressLevelSelectorNativeTap => levelSelectActive &&
            (levelSelectTouchDragging || Time.frameCount <= levelSelectIgnoreNativeTapUntilFrame);
        private void HandleMarketTouchScroll(Vector2 pixel, bool down, bool held, bool up)
        {
            if (!marketActive || game == null || game.Sim == null || game.ShowChulengoResult) return;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Rect viewport = CanvasViewport(screenSize, Screen.safeArea);
            if (viewport.width <= 0f) return;
            logicalCanvasHeight = CanvasLogicalHeight(viewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 point = ScreenToLayoutPoint(pixel, screenSize, viewport);
            point.y *= layoutVerticalScale;
            UpdateMarketTouchScroll(point, down, held, up);
        }
        private void UpdateMarketTouchScroll(Vector2 point, bool down, bool held, bool up)
        {
            if (!marketActive || game == null || game.Sim == null || game.ShowChulengoResult) return;
            Rect body = MarketBodyBounds();
            if (down)
            {
                marketTouchHeld = body.Contains(point);
                marketTouchDownY = point.y;
                marketTouchDownScroll = marketScroll;
                marketTouchDragging = false;
                marketIgnoreNativeTapUntilFrame = -1;
            }
            if (held && marketTouchHeld)
            {
                float delta = marketTouchDownY - point.y;
                if (Mathf.Abs(delta) > 8f) marketTouchDragging = true;
                if (marketTouchDragging)
                    marketScroll = Mathf.Clamp(marketTouchDownScroll + delta, 0f, MarketMaxScroll());
            }
            if (up)
            {
                if (marketTouchDragging) marketIgnoreNativeTapUntilFrame = Time.frameCount + 1;
                marketTouchHeld = false;
                marketTouchDragging = false;
            }
        }
        private bool SuppressMarketNativeTap => marketActive &&
            (marketTouchDragging || Time.frameCount <= marketIgnoreNativeTapUntilFrame);
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
        private Rect ResponsiveUpgradeCardBounds(StreetSimulation sim, int action)
        {
            ResponsiveFooterLayout footer = CurrentResponsiveFooter(sim, logicalCanvasHeight);
            int index = System.Array.IndexOf(CatalogUpgradeActions(sim), action);
            if (index < 0 || footer.UpgradeBounds == null || index >= footer.UpgradeBounds.Length) return Rect.zero;
            return ScaleCardBounds(footer.UpgradeBounds[index], layoutVerticalScale);
        }
        private void HandlePointer(Vector2 pixel, bool down, bool up)
        {
            if (RewardShutterActive) return;
            if (game == null || (!IsTowerDefenseNavigationActive && (!game.IsPlayerActive || game.Sim == null)) ||
                (Cloud.FirebasePlayerSession.Instance != null && Cloud.FirebasePlayerSession.Instance.BlocksGameInput)) return;
            if (ConsumeVictoryTransitionPointer(down, up)) return;
            Vector2 pointerScreenSize = IsTowerDefenseNavigationActive ? TowerDefenseRenderSize(Camera.main) : new Vector2(Screen.width, Screen.height);
            Rect v = CanvasViewport(pointerScreenSize,Screen.safeArea);
            if (v.width <= 0) return;
            logicalCanvasHeight = CanvasLogicalHeight(v);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 p = ScreenToLayoutPoint(pixel, pointerScreenSize, v);
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
            if (badgesActive && !game.ShowChulengoResult)
            {
                p.y *= layoutVerticalScale;
                HandleBadgesPointer(p, down, up);
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
        // Returning from the bonus keeps Huracán selected in Ready, but the visible selector/hub owns input then.
        private bool BonusTrackInputActive => game != null && game.IsBonusTrack && game.Sim.Phase != RoundPhase.Ready;

        private int HitAction(Vector2 p)
        {
            if (VictoryTransitionActive || victorySkipHeld) return 0;
            if (IsTowerDefenseNavigationActive) return HitTowerDefenseAction(p);
            // Huracán has its own result panel and actions. A previous club's Chulengo receipt
            // may still be present in Mercado.LastResult; it must never capture Bonus Track taps.
            if (BonusTrackInputActive) return HitBonusTrackAction(p);
            if (game.ShowChulengoResult)
            {
                Vector2 resultPoint = new Vector2(p.x, p.y * layoutVerticalScale);
                if (game.HasPendingChulengo && !game.ChulengoChoiceLocked)
                    return ChulengoMultiplyBounds().Contains(resultPoint) ? ChulengoMultiplyAction
                        : ChulengoCollectBounds().Contains(resultPoint) ? ChulengoCollectAction : 0;
                return ChulengoCollectBounds().Contains(resultPoint) ? ChulengoCollectAction : 0;
            }
            if (marketActive) { p.y *= layoutVerticalScale; return HitMarketAction(p); }
            if (introActive) return 0;
            if (levelOneTutorialActive)
            {
                if (LevelOneTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelOneTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (levelTwoCocacoleroTutorialActive)
            {
                if (LevelTwoCocacoleroTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelTwoCocacoleroTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (levelThreePatyTutorialActive)
            {
                if (LevelThreePatyTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelThreePatyTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (levelFourSpecialistsTutorialActive)
            {
                if (LevelFourSpecialistsTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelFourSpecialistsTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (levelFiveSpecialistsTutorialActive)
            {
                if (LevelFiveSpecialistsTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelFiveSpecialistsTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (levelSixRacingTutorialActive)
            {
                if (LevelSixRacingTutorialSkipBounds().Contains(p)) return LevelOneTutorialSkipAction;
                return LevelSixRacingTutorialPrimaryBounds().Contains(p) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (profileActive) { p.y *= layoutVerticalScale; return HitProfileAction(p); }
            if (badgesActive) { p.y *= layoutVerticalScale; return BadgeBackBounds(logicalCanvasHeight).Contains(p) ? BadgeBackAction : 0; }
            if (rankingActive) { p.y *= layoutVerticalScale; return HitRankingAction(p); }
            if (hubActive && !marketActive)
            {
                if (settingsActive)
                {
                    Vector2 canvas = new Vector2(p.x, p.y * layoutVerticalScale);
                    if (HubSettingsMusicButtonCanvasBounds.Contains(canvas)) return HubSettingsMusicAction;
                    if (HubSettingsBackButtonCanvasBounds.Contains(canvas)) return HubSettingsBackAction;
                    return 0;
                }
                if (HubSettingsButtonLayoutBounds(new Vector2(Screen.width, Screen.height), Screen.safeArea).Contains(p))
                    return HubSettingsAction;
                if (HubProfileCardBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale))) return HubProfileAction;
                if (HubTopMusicButtonLayoutBounds(new Vector2(Screen.width, Screen.height), Screen.safeArea).Contains(p))
                    return HubTopMusicAction;
                foreach (int action in HubArtworkActions)
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
                ? (game.CanAdvanceAfterResult && VictoryExitBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? VictoryExitAction
                    : ResultLevelsBounds().Contains(new Vector2(p.x, p.y * layoutVerticalScale)) ? ResultLevelsAction : 0)
                : 0;
        }
        // What tapping an unlocked level does: select it and go to its Market, tutorial or round (runs once the shutter has closed).
        private void EnterSelectedLevel(int a, int level)
        {
            if (game == null) return;
            levelSelectorStartError = null;
            bool selectionSucceeded = game.SelectLevel(level);
            if (selectionSucceeded)
            {
                levelSelectActive = false;
                if (StreetSimulation.IsTowerDefenseLevel(level))
                {
                    marketFromSelector = true;
                    if (game.CanStartRound)
                    {
                        if (StartTowerDefenseRun()) marketFromSelector = false;
                        else
                        {
                            levelSelectorStartError = string.IsNullOrEmpty(towerDefenseError)
                                ? "No se pudo iniciar el partido." : towerDefenseError;
                            levelSelectActive = true;
                        }
                    }
                    else
                    {
                        marketActive = true; marketTab = 0; marketMessage = "Prepará Mercado para iniciar Tower Defense.";
                        marketScroll = 0; marketOtherExpanded = false; marketExpandedIngredient = -1;
                    }
                }
                else if (level == 0)
                {
                    if (game.CanStartRound)
                    {
                        marketMessage = "";
                        if (RepeatLevelOneTutorialForTesting ||
                            PlayerPrefs.GetInt(LevelOneTutorialCompletedKey, 0) == 0)
                        {
                            levelOneTutorialActive = true;
                            levelOneTutorialStep = 0;
                        }
                        else game.TryStartRound();
                    }
                    else { marketMessage="Falta stock para iniciar. Revisá Mercado."; levelSelectActive=true; }
                }
                else { marketFromSelector = true; marketActive = true; marketTab = 0; marketMessage = ""; marketScroll = 0; marketOtherExpanded = false; marketExpandedIngredient = -1; }
            }
            lastAction = a; feedbackUntil = Time.unscaledTime + 0.22f; feedbackScale = selectionSucceeded ? 1.04f : 0.97f;
        }

        // The Market's JUGAR PARTIDO: Tower Defense run, a level's first-time tutorial or the round itself.
        private void HandleMarketPlay()
        {
            if (game?.Sim == null) return;
            if (StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex))
            {
                if (StartTowerDefenseRun())
                {
                    marketActive = false; marketFromSelector = false; hubActive = false; levelSelectActive = false;
                }
                else marketMessage = string.IsNullOrEmpty(towerDefenseError) ? "Falta stock para iniciar." : towerDefenseError;
                return;
            }
            if (game.CanStartRound && game.Sim.LevelIndex == 2 &&
                PlayerPrefs.GetInt(LevelThreePatyTutorialCompletedKey, 0) == 0)
            {
                marketMessage = "";
                marketActive = false;
                hubActive = false;
                levelSelectActive = false;
                levelThreePatyTutorialActive = true;
                return;
            }
            if (game.CanStartRound && game.Sim.LevelIndex == 3 &&
                PlayerPrefs.GetInt(LevelFourSpecialistsTutorialCompletedKey, 0) == 0)
            {
                marketMessage = "";
                marketActive = false;
                hubActive = false;
                levelSelectActive = false;
                levelFourSpecialistsTutorialActive = true;
                levelFourSpecialistsTutorialStep = 0;
                return;
            }
            if (game.CanStartRound && game.Sim.LevelIndex == 4 &&
                PlayerPrefs.GetInt(LevelFiveSpecialistsTutorialCompletedKey, 0) == 0)
            {
                marketMessage = "";
                marketActive = false;
                hubActive = false;
                levelSelectActive = false;
                levelFiveSpecialistsTutorialActive = true;
                levelFiveSpecialistsTutorialStep = 0;
                return;
            }
            if (game.CanStartRound && game.Sim.LevelIndex == 5 &&
                PlayerPrefs.GetInt(LevelSixRacingTutorialCompletedKey, 0) == 0)
            {
                marketMessage = "";
                marketActive = false;
                hubActive = false;
                levelSelectActive = false;
                levelSixRacingTutorialActive = true;
                levelSixRacingTutorialStep = 0;
                return;
            }
            if (game.CanStartRound && game.Sim.LevelIndex == 1 &&
                PlayerPrefs.GetInt(LevelTwoCocacoleroTutorialCompletedKey, 0) == 0)
            {
                marketMessage = "";
                marketActive = false;
                hubActive = false;
                levelSelectActive = false;
                levelTwoCocacoleroTutorialActive = true;
                levelTwoCocacoleroTutorialStep = 0;
                return;
            }
            if (game.TryStartRoundWithSaveFeedback(out bool initialSaveRejected))
                marketMessage = "";
            else if (initialSaveRejected)
                marketMessage = "No se pudo guardar el inicio. Volvé a intentar.";
            else if (!game.Sim.IsBonusTrack && game.Market != null &&
                !game.Market.CanMeetRequiredStockWithLevelOneFloor(game.Sim))
                marketMessage = "Falta stock para iniciar";
            else
                marketMessage = "No se pudo iniciar el partido.";
            if (game.Sim.Phase == RoundPhase.Playing) { marketActive = false; hubActive = false; levelSelectActive = false; }
            return;
        }

        private void DispatchAction(int a)
        {
            if (RewardShutterActive || VictoryTransitionActive || victorySkipHeld || game == null) return;
            var firebaseSession = Cloud.FirebasePlayerSession.Instance;
            bool isHubSettingsAction = hubActive &&
                (a == HubSettingsAction || a == HubSettingsMusicAction || a == HubSettingsBackAction || a == HubTopMusicAction);
            if (firebaseSession != null && firebaseSession.BlocksGameInput && !isHubSettingsAction)
            {
                if (firebaseSession.AccountMenuOpen) return;
                // Game destinations still require Firebase; local hub controls remain accessible
                // without an account and do not alter the authentication gate.
                if (hubActive) firebaseSession.OpenAccountMenu();
                return;
            }
            if (HandleTowerDefenseAction(a)) return;
            // Route by the currently rendered mode before inspecting any stale normal-level receipt.
            if (BonusTrackInputActive)
            {
                bool bonusActionSucceeded = false;
                if (a >= BonusCardFirstAction && a < BonusCardFirstAction + 3) game.ChooseBonusCard(a - BonusCardFirstAction);
                else if (a >= BonusWorkerFirstAction && a < BonusWorkerFirstAction + 3)
                {
                    System.Collections.Generic.IReadOnlyList<StreetWorkerRole> roles = game.Sim.BonusTrack?.EmergencyRoles;
                    int index = a - BonusWorkerFirstAction;
                    if (roles != null && index < roles.Count) game.ChooseBonusEmergencyRole(roles[index]);
                }
                else if (a == BonusAguanteAction) game.ActivateBonusAguante();
                else if (a == ResultLevelsAction && (game.Sim.Phase == RoundPhase.Won || game.Sim.Phase == RoundPhase.Lost))
                {
                    bonusActionSucceeded = game.ReturnToLevels();
                    if (bonusActionSucceeded) { marketActive = false; levelSelectActive = true; levelSelectScroll = 0f; }
                }
                else if (a == VictoryExitAction && game.Sim.Phase == RoundPhase.Won && game.CanAdvanceAfterResult)
                {
                    bonusActionSucceeded = game.NextLevel();
                    if (bonusActionSucceeded && game.IsBonusTrack) bonusActionSucceeded = game.TryStartRound();
                    else if (bonusActionSucceeded) { marketActive = true; marketFromSelector = true; marketOtherExpanded = false; marketMessage = ""; }
                }
                if (a == ResultLevelsAction || a == VictoryExitAction)
                { lastAction = a; feedbackUntil = Time.unscaledTime + .22f; feedbackScale = bonusActionSucceeded ? 1.04f : .97f; }
                return;
            }
            if (game.ShowChulengoResult)
            {
                if (a == ChulengoCollectAction)
                {
                    StartRewardShutter();
                    return;
                }
                bool selected = a == ChulengoMultiplyAction && game.HasPendingChulengo && !game.ChulengoChoiceLocked &&
                    game.TrySelectVictoryRewardMultiplierAtPosition(StreetChulengo.PositionAtTime(Time.unscaledTime));
                if (a == ChulengoMultiplyAction || a == ChulengoCollectAction)
                {
                    lastAction = a;
                    feedbackUntil = Time.unscaledTime + .22f;
                    feedbackScale = selected || a == ChulengoCollectAction ? 1.04f : .97f;
                }
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
                        if (!RepeatLevelOneTutorialForTesting)
                        {
                            PlayerPrefs.SetInt(LevelOneTutorialCompletedKey, 1);
                            PlayerPrefs.Save();
                        }
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelOneTutorialStep < 3)
                    levelOneTutorialStep++;
                return;
            }
            if (levelTwoCocacoleroTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction ||
                    (a == LevelOneTutorialPrimaryAction && levelTwoCocacoleroTutorialStep == 1))
                {
                    if (game.TryStartRound())
                    {
                        levelTwoCocacoleroTutorialActive = false;
                        levelTwoCocacoleroTutorialStep = 0;
                        PlayerPrefs.SetInt(LevelTwoCocacoleroTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                        marketActive = false;
                        hubActive = false;
                        levelSelectActive = false;
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelTwoCocacoleroTutorialStep == 0)
                    levelTwoCocacoleroTutorialStep++;
                return;
            }
            if (levelThreePatyTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction || a == LevelOneTutorialPrimaryAction)
                {
                    if (game.TryStartRound())
                    {
                        levelThreePatyTutorialActive = false;
                        PlayerPrefs.SetInt(LevelThreePatyTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                        marketActive = false;
                        hubActive = false;
                        levelSelectActive = false;
                    }
                }
                return;
            }
            if (levelFourSpecialistsTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction ||
                    (a == LevelOneTutorialPrimaryAction && levelFourSpecialistsTutorialStep == 1))
                {
                    if (game.TryStartRound())
                    {
                        levelFourSpecialistsTutorialActive = false;
                        levelFourSpecialistsTutorialStep = 0;
                        PlayerPrefs.SetInt(LevelFourSpecialistsTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                        marketActive = false;
                        hubActive = false;
                        levelSelectActive = false;
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelFourSpecialistsTutorialStep == 0)
                    levelFourSpecialistsTutorialStep++;
                return;
            }
            if (levelFiveSpecialistsTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction ||
                    (a == LevelOneTutorialPrimaryAction && levelFiveSpecialistsTutorialStep == 1))
                {
                    if (game.TryStartRound())
                    {
                        levelFiveSpecialistsTutorialActive = false;
                        levelFiveSpecialistsTutorialStep = 0;
                        PlayerPrefs.SetInt(LevelFiveSpecialistsTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                        marketActive = false;
                        hubActive = false;
                        levelSelectActive = false;
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelFiveSpecialistsTutorialStep == 0)
                    levelFiveSpecialistsTutorialStep++;
                return;
            }
            if (levelSixRacingTutorialActive)
            {
                if (a == LevelOneTutorialSkipAction ||
                    (a == LevelOneTutorialPrimaryAction && levelSixRacingTutorialStep == 1))
                {
                    if (game.TryStartRound())
                    {
                        levelSixRacingTutorialActive = false;
                        levelSixRacingTutorialStep = 0;
                        PlayerPrefs.SetInt(LevelSixRacingTutorialCompletedKey, 1);
                        PlayerPrefs.Save();
                        marketActive = false;
                        hubActive = false;
                        levelSelectActive = false;
                    }
                }
                else if (a == LevelOneTutorialPrimaryAction && levelSixRacingTutorialStep == 0)
                    levelSixRacingTutorialStep++;
                return;
            }
            if (hubActive && !marketActive)
            {
                if (a == HubSettingsAction)
                    settingsActive = true;
                else if (a == HubTopMusicAction && !settingsActive)
                    ToggleHubMusic();
                else if (settingsActive)
                {
                    if (a == HubSettingsMusicAction)
                        ToggleHubMusic();
                    else if (a == HubSettingsBackAction)
                        settingsActive = false;
                }
                else if (a == HubPlayAction)
                {
                    hubActive = false; marketActive = false; levelSelectActive = true;
                    marketFromSelector = false;
                    levelSelectScroll = 0f;
                }
                else if (a == HubMarketAction)
                {
                    marketActive = true; marketFromSelector = false; marketTab = 0; marketMessage = "";
                    marketScroll = 0; marketOtherExpanded = false; marketExpandedIngredient = -1;
                }
                else if (a == HubBadgesAction) OpenBadges();
                else if (a == HubProfileAction) OpenProfile();
                else if (a == HubRankingAction) OpenRanking();
                else if (a == HubAccountAction) Cloud.FirebasePlayerSession.Instance?.OpenAccountMenu();
                return;
            }
            if (profileActive)
            {
                DispatchProfileAction(a);
                return;
            }
            if (badgesActive)
            {
                if (a == BadgeBackAction) { badgesActive = false; hubActive = true; }
                return;
            }
            if (rankingActive)
            {
                DispatchRankingAction(a);
                return;
            }
            if (levelSelectActive)
            {
                if (a == LevelSelectorBackAction)
                {
                    levelSelectorStartError = null;
                    levelSelectActive = false;
                    hubActive = true;
                    marketFromSelector = false;
                    return;
                }
                if (a >= LevelSelectFirstAction && a < LevelSelectFirstAction + StreetSimulation.LevelNames.Length)
                {
                    int campaignPosition = a - LevelSelectFirstAction;
                    int level = StreetSimulation.CampaignLevelAt(campaignPosition);
                    if (game.IsLevelUnlocked(level))
                    {
                        // The shutter closes over the selector, the level is entered behind it and the shutter opens on the result.
                        int tappedAction = a;
                        StartRewardShutter(() => EnterSelectedLevel(tappedAction, level));
                        return;
                    }
                    lastAction = a; feedbackUntil = Time.unscaledTime + 0.22f; feedbackScale = 0.97f;
                    return;
                }
                return;
            }
            if (marketActive)
            {
                if (a == MarketBackAction) { marketActive=false; marketMessage=game.CanStartRound?"Stock suficiente para jugar.":"Falta stock para iniciar. Revisá Mercado."; if(marketFromSelector)levelSelectActive=true; return; }
                if (a == MarketIngredientsTabAction || a == MarketExtrasTabAction || a == MarketPermitsTabAction)
                {
                    int nextTab = a == MarketIngredientsTabAction ? 0 : a == MarketExtrasTabAction ? 1 : 2;
                    if (marketTab != nextTab) { marketTab = nextTab; marketScroll = 0f; marketMessage = ""; }
                    return;
                }
                if (a == MarketMunicipalPermitAction || a == MarketPoliceContributionAction)
                {
                    if (Time.unscaledTime - marketLastPermitPurchaseTime < .25f) return;
                    marketLastPermitPurchaseTime = Time.unscaledTime;
                    int item = a == MarketMunicipalPermitAction ? (int)StreetMarketPermitItem.MunicipalLicense : (int)StreetMarketPermitItem.PoliceContribution;
                    bool success = game.TryBuyPreparationPermit(item);
                    marketMessage = success ? StreetMarketPreparationCatalog.Name(item) + " adquirida para esta preparación."
                        : game.IsPreparationPermitPurchased(item) ? "Este permiso ya está comprado."
                        : game.Market.BalanceCents < StreetMarketPreparationCatalog.ItemPriceCents ? MarketInsufficientFundsMessage(StreetMarketPreparationCatalog.ItemPriceCents)
                        : "No se pudo guardar la compra; el saldo no cambió.";
                    return;
                }
                if (a >= MarketTicketRedeemAction && a < MarketTicketRedeemAction + 5)
                {
                    StreetWorkerRole role = (StreetWorkerRole)(a - MarketTicketRedeemAction);
                    bool success = game.TryRedeemTicketWorker(role, game.TicketRedemptionOperationId(role));
                    marketMessage = success ? "¡CANJE REALIZADO! Tu trabajador está reservado para el próximo partido."
                        : game.TicketBalance < 3 ? "Te faltan " + System.Math.Max(0L, 3L - game.TicketBalance) + " tickets para canjear."
                        : !StreetSimulation.IsRacingOrLaterLevel(game.Sim.LevelIndex) || !game.TicketExtrasUnlocked ? "Desbloqueá Racing y elegí una cancha compatible."
                        : "No se pudo guardar el canje; el saldo no cambió.";
                    return;
                }
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
                    if(!MarketHasMissing()){marketMessage="No hay faltantes pendientes.";return;}
                    long cost=game.Market.MinimumPurchaseCostCents(game.Sim);
                    if(cost>game.Market.BalanceCents){marketMessage=MarketInsufficientFundsMessage(cost);return;}
                    marketMessage=game.TryBuyMissingIngredients() ? "Compra realizada. Stock completo."
                        : "No se completó la compra. Tu saldo y stock no cambiaron.";
                    return;
                }
                if (a == MarketPlayAction)
                {
                    // Going from the Market to play closes the shutter too, unless there is nothing to start (the message shows at once).
                    if (game.CanStartRound) StartRewardShutter(HandleMarketPlay); else HandleMarketPlay();
                    return;
                }
            }
            bool ok = true;
            if (a == ReadyLevelSelectorAction && game.Sim.Phase == RoundPhase.Ready)
            {
                levelSelectActive = true;
                levelSelectScroll = 0f;
                return;
            }
            if (a == 1)
            {
                if (game.Sim.LevelIndex == 2 && game.CanStartRound &&
                    PlayerPrefs.GetInt(LevelThreePatyTutorialCompletedKey, 0) == 0)
                {
                    levelThreePatyTutorialActive = true;
                    marketActive = false;
                    hubActive = false;
                    levelSelectActive = false;
                }
                else if (game.Sim.LevelIndex == 3 && game.CanStartRound &&
                    PlayerPrefs.GetInt(LevelFourSpecialistsTutorialCompletedKey, 0) == 0)
                {
                    levelFourSpecialistsTutorialActive = true;
                    levelFourSpecialistsTutorialStep = 0;
                    marketActive = false;
                    hubActive = false;
                    levelSelectActive = false;
                }
                else if (game.Sim.LevelIndex == 4 && game.CanStartRound &&
                    PlayerPrefs.GetInt(LevelFiveSpecialistsTutorialCompletedKey, 0) == 0)
                {
                    levelFiveSpecialistsTutorialActive = true;
                    levelFiveSpecialistsTutorialStep = 0;
                    marketActive = false;
                    hubActive = false;
                    levelSelectActive = false;
                }
                else if (game.Sim.LevelIndex == 5 && game.CanStartRound &&
                    PlayerPrefs.GetInt(LevelSixRacingTutorialCompletedKey, 0) == 0)
                {
                    levelSixRacingTutorialActive = true;
                    levelSixRacingTutorialStep = 0;
                    marketActive = false;
                    hubActive = false;
                    levelSelectActive = false;
                }
                else if (game.Sim.LevelIndex == 1 && game.CanStartRound &&
                    PlayerPrefs.GetInt(LevelTwoCocacoleroTutorialCompletedKey, 0) == 0)
                {
                    levelTwoCocacoleroTutorialActive = true;
                    levelTwoCocacoleroTutorialStep = 0;
                    marketActive = false;
                    hubActive = false;
                    levelSelectActive = false;
                }
                else game.TryStartRound();
            }
            else if(a==2) ok=game.TryHireParrillero(); else if(a==3) ok=game.TryUpgradeSpeed(); else if (a==HireCocacoleroAction || a==HirePremiumAction || a==HireFerneteroAction) ok=game.TryHire(RoleForAction(a)); else if (a==HireMozoAction) ok=game.TryHireMozo();
            else if (a == RiotReturnAction && game.Sim.Phase == RoundPhase.Lost)
            {
                ok = game.ReturnToLevels();
                if (ok) { levelSelectActive = true; levelSelectScroll = 0f; }
            }
            else if (a == VictoryExitAction && game.Sim.Phase == RoundPhase.Won)
            {
                if (game.CanAdvanceAfterResult)
                {
                    ok=game.NextLevel();
                    if(ok && game.IsBonusTrack) ok = game.TryStartRound();
                    else if(ok){marketActive=true;marketFromSelector=true;marketOtherExpanded=false;marketMessage="";}
                }
                else ok=false;
            }
            else if (a == ResultLevelsAction && (game.Sim.Phase == RoundPhase.Won || game.Sim.Phase == RoundPhase.Lost))
            {
                ok=game.ReturnToLevels(); if(ok){marketActive=false;levelSelectActive=true;levelSelectScroll=0f;}
            }
            lastAction=a; feedbackUntil=Time.unscaledTime+0.22f; feedbackScale=ok?1.04f:0.97f;
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
    }
}
