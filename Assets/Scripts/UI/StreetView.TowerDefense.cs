using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int TowerDefenseSpeedAction = 7810;
        private const int TowerDefensePauseAction = 7811;
        private const int TowerDefenseExitAction = 7812;
        private const int TowerDefenseHireFirstAction = 7820;
        private const int TowerDefenseHireLastAction = TowerDefenseHireFirstAction + 1;
        private const int TowerDefensePlaceFirstAction = 7830;
        private const float TowerDefenseMinimumFanRevealBeforeOrderBubbleLogical =
            TowerDefenseResponsiveLayout.FanFrameWidthLogical * .5f;
        private const float TowerDefenseOrderBubbleHeadClearanceLogical = 6f;
        private readonly List<Rect> laRengaOrderBubbles = new List<Rect>();
        private const string CamionerosTutorialCompletedKey = "HayChoriYPaty.TowerDefense.Camioneros.Tutorial.Completed.v2";
        private static readonly string[] CamionerosTutorialHeadlines =
        {
            "Para el chori,\ncontratá al Parrillero.",
            "Para la Coca,\ncontratá al Cocacolero.",
            "Deportivos:\nrápidos, pedidos chicos.",
            "Casuales:\nritmo intermedio.",
            "Abrigados:\ntranquilos, piden más."
        };
        private static readonly string[] CamionerosTutorialExplanations =
        {
            "Tocá Parrillero y elegí\ncualquiera de las dos áreas.",
            "Tocá Cocacolero y elegí\ncualquiera de las dos áreas.",
            "Van más juntos (10 px).\nPiden 1 por producto.",
            "Separación de 20 px.\nPiden de 1 a 3 por producto.",
            "Separación de 30 px.\nPiden de 2 a 4 por producto."
        };
        private static readonly StreetWorkerRole[] TowerDefenseHireRoles =
        {
            StreetWorkerRole.Parrillero, StreetWorkerRole.Cocacolero
        };

        private bool towerDefenseActive;
        private bool towerDefenseTutorialActive;
        private int towerDefenseTutorialStep;
        private int towerDefenseSelectedRole = -1;
        private string towerDefenseError;
        private TowerDefenseLevelConfig towerDefenseSourceConfig;
        private TowerDefenseLevelConfig towerDefenseRuntimeConfig;
        private TowerDefenseGameMode towerDefenseMode;
        private TowerDefenseWorldPresentation towerDefensePresentation;
        private Texture2D towerDefenseBackdrop;
        private Texture2D towerDefenseCrest;
        private GUIStyle towerDefenseLabel, towerDefenseSmallLabel, towerDefenseTitle;
        private Vector2 towerDefenseScreenSize;
        private Rect towerDefenseSafeArea;
        private Rect? towerDefenseCompactViewport;

        private bool IsTowerDefenseNavigationActive => towerDefenseActive;

        private static Vector2 TowerDefenseRenderSize(Camera camera)
        {
            if (camera != null && camera.pixelWidth > 0 && camera.pixelHeight > 0)
                return new Vector2(camera.pixelWidth, camera.pixelHeight);
            return new Vector2(Screen.width, Screen.height);
        }

        private void ResetTowerDefenseUi()
        {
            towerDefenseActive = false;
            towerDefenseTutorialActive = false;
            towerDefenseTutorialStep = 0;
            towerDefenseSelectedRole = -1;
            towerDefenseError = null;
            towerDefenseBackdrop = null;
            towerDefenseCrest = null;
            towerDefenseLabel = towerDefenseSmallLabel = towerDefenseTitle = null;
            towerDefenseScreenSize = Vector2.zero;
            towerDefenseSafeArea = Rect.zero;
            towerDefenseCompactViewport = null;
            TowerDefenseGameMode.SetCampaignStepSuspended(false);
        }

        private void DisposeTowerDefenseUi()
        {
            if (towerDefenseMode != null && game != null && game.Sim != null && game.Sim.IsExternalCampaignRuntime)
                game.PersistTowerDefenseProgress(towerDefenseMode);
            EndTowerDefenseRun();
            TowerDefenseGameMode.SetCampaignStepSuspended(false);
        }

        private bool HandleTowerDefenseAction(int action)
        {
            if (!towerDefenseActive) return false;

            if (towerDefenseMode == null) return true;
            if (IsPilgrimConstruction && HandlePilgrimAction(action)) return true;
            if (game != null && game.Sim != null && game.Sim.IsExternalCampaignRuntime && !game.IsPlayerActive) return true;
            if (towerDefenseTutorialActive)
            {
                if (action == LevelOneTutorialSkipAction ||
                    (action == LevelOneTutorialPrimaryAction && towerDefenseTutorialStep == CamionerosTutorialHeadlines.Length - 1))
                    CompleteCamionerosTutorial();
                else if (action == LevelOneTutorialPrimaryAction && towerDefenseTutorialStep < CamionerosTutorialHeadlines.Length - 1)
                    towerDefenseTutorialStep++;
                return true;
            }
            if (action == TowerDefensePauseAction)
            {
                if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused) towerDefenseMode.Resume();
                else towerDefenseMode.Pause();
                if (!game.PersistTowerDefenseProgress(towerDefenseMode))
                    towerDefenseError = "No se pudo guardar el estado de la partida.";
            }
            else if (action == TowerDefenseExitAction)
            {
                if (game.ExitTowerDefenseRound(towerDefenseMode))
                {
                    EndTowerDefenseRun();
                    marketActive = false; marketFromSelector = false; hubActive = false;
                    levelSelectActive = true; levelSelectScroll = 0f;
                }
                else towerDefenseError = "No se pudo guardar la salida de Tower Defense.";
            }
            else if (action == TowerDefenseSpeedAction)
            {
                if (!game.TryUpgradeTowerDefenseSpeed(towerDefenseMode))
                    towerDefenseError = "No se pudo guardar la mejora.";
            }
            else if (action >= TowerDefenseHireFirstAction && action <= TowerDefenseHireLastAction)
            {
                int index = action - TowerDefenseHireFirstAction;
                if (TowerDefensePlacementInputAllowed && index >= 0 && index < TowerDefenseHireRoles.Length)
                {
                    if (towerDefenseSelectedRole == index) towerDefenseSelectedRole = -1;
                    else if (CanSelectTowerDefenseRole(index)) towerDefenseSelectedRole = index;
                    towerDefenseError = null;
                }
            }
            else if (action >= TowerDefensePlaceFirstAction && action < TowerDefensePlaceFirstAction + 2)
            {
                if (!TowerDefensePlacementInputAllowed || towerDefenseSelectedRole < 0 ||
                    towerDefenseSelectedRole >= TowerDefenseHireRoles.Length) return true;
                int zone = action - TowerDefensePlaceFirstAction;
                StreetWorkerRole role = TowerDefenseHireRoles[towerDefenseSelectedRole];
                if (!towerDefenseMode.CanPlaceWorker(role, zone))
                {
                    towerDefenseError = towerDefenseMode.WorkersInZone(role, zone) >= TowerDefenseGameMode.MaxWorkersPerRolePerZone
                        ? "Esta área ya tiene 3 trabajadores de ese tipo." : "No alcanza el saldo para colocarlo.";
                    return true;
                }
                // Clear before the transaction, including a persistence failure: a
                // repeated pointer release must never repeat the purchase.
                towerDefenseSelectedRole = -1;
                if (game == null || !game.TryHireTowerDefense(towerDefenseMode, role, zone))
                    towerDefenseError = "No se pudo guardar la contratación.";
                else towerDefenseError = null;
            }
            return true;
        }

        private void CompleteCamionerosTutorial()
        {
            if (!towerDefenseTutorialActive || towerDefenseMode == null) return;

            towerDefenseMode.Resume();
            if (game == null || !game.PersistTowerDefenseProgress(towerDefenseMode))
            {
                towerDefenseMode.Pause();
                towerDefenseError = "No se pudo guardar el comienzo de la partida.";
                return;
            }

            towerDefenseTutorialActive = false;
            towerDefenseTutorialStep = 0;
            towerDefenseError = null;
            PlayerPrefs.SetInt(CamionerosTutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        private bool TowerDefensePlacementInputAllowed => towerDefenseMode != null &&
            towerDefenseMode.Phase == TowerDefenseRunPhase.Playing &&
            (game == null || game.Sim == null || !game.Sim.IsExternalCampaignRuntime || game.IsPlayerActive);

        private bool CanSelectTowerDefenseRole(int index) => TowerDefensePlacementInputAllowed &&
            index >= 0 && index < TowerDefenseHireRoles.Length &&
            (towerDefenseMode.CanPlaceWorker(TowerDefenseHireRoles[index], 0) ||
             towerDefenseMode.CanPlaceWorker(TowerDefenseHireRoles[index], 1));

        private int HitTowerDefenseAction(Vector2 point)
        {
            // The common pointer path supplies the unmodified safe-canvas point.
            // Map it into the same compact rectangle used by the Defense GUI and camera projection.
            if (towerDefenseCompactViewport.HasValue)
            {
                Rect safe = CanvasViewport(TowerDefenseRenderSize(Camera.main), Screen.safeArea);
                Rect compact = towerDefenseCompactViewport.Value;
                point.x = (safe.x + point.x * safe.width / W - compact.x) * W / compact.width;
                logicalCanvasHeight = CanvasLogicalHeight(compact);
                layoutVerticalScale = logicalCanvasHeight / H;
                if (point.x < 0f || point.x >= W || point.y < 0f || point.y >= H) return 0;
            }
            point.y *= layoutVerticalScale;
            if (IsPilgrimConstruction) return HitPilgrimAction(point);
            if (towerDefenseTutorialActive)
            {
                if (CamionerosTutorialSkipBounds().Contains(point)) return LevelOneTutorialSkipAction;
                return CamionerosTutorialPrimaryBounds().Contains(point) ? LevelOneTutorialPrimaryAction : 0;
            }
            if (!TowerDefensePlacementInputAllowed) return 0;
            Rect[] roleBounds = TowerDefenseHireBounds(logicalCanvasHeight);
            for (int i = 0; i < roleBounds.Length; i++)
                if (roleBounds[i].Contains(point) && (towerDefenseSelectedRole == i || CanSelectTowerDefenseRole(i)))
                    return TowerDefenseHireFirstAction + i;
            if (towerDefenseSelectedRole >= 0)
            {
                Camera camera = Camera.main;
                Rect viewport = towerDefenseCompactViewport ?? CanvasViewport(TowerDefenseRenderSize(camera), Screen.safeArea);
                Rect[] areas = TowerDefensePlacementAreaBounds(viewport);
                for (int i = 0; i < areas.Length; i++)
                    if (areas[i].Contains(point)) return TowerDefensePlaceFirstAction + i;
            }
            return 0;
        }

        private struct TowerDefenseLayoutDraft
        {
            public Rect Viewport;
            public bool Compact;
            public TowerDefenseLevelConfig Config;
            public TowerDefenseRouteGeometryConfig Route;
            public float WorldPixel;
            public Vector2 FirstZone, SecondZone;
            public TowerDefenseGameMode Mode;
        }

        private bool TryDraftTowerDefenseLayout(Camera camera, Rect viewport, bool validateStations,
            out TowerDefenseLayoutDraft draft, out string error)
        {
            draft = new TowerDefenseLayoutDraft { Viewport = viewport };
            if (!TowerDefenseResponsiveLayout.TryCreate(towerDefenseSourceConfig, camera, viewport, W,
                    CanvasLogicalHeight(viewport), out draft.Config, out draft.Route, out draft.WorldPixel,
                    out draft.FirstZone, out draft.SecondZone, out error))
                return false;
            if (!validateStations) return true;
            try
            {
                int startingCoins = StreetSimulation.GetStartingCoinsForLevel(game.Balance, towerDefenseSourceConfig.SourceCampaignLevelId);
                draft.Mode = new TowerDefenseGameMode(draft.Config, game.Balance, draft.Route, draft.WorldPixel,
                    draft.FirstZone, draft.SecondZone, null, null, startingCoins);
                return true;
            }
            catch (ArgumentException exception) when (exception.ParamName == "config" || exception.ParamName == "runtimeRoute")
            {
                Destroy(draft.Config);
                draft.Config = null;
                error = exception.Message;
                return false;
            }
        }

        private bool TryFitTowerDefenseLayout(Camera camera, Rect safe, bool starting,
            out TowerDefenseLayoutDraft draft, out string error)
        {
            bool canCompact = TowerDefenseResponsiveLayout.TryCompactViewport(safe, out Rect compact);
            if (TryDraftTowerDefenseLayout(camera, safe, starting || canCompact, out draft, out error)) return true;
            if (!canCompact || !TryDraftTowerDefenseLayout(camera, compact, true, out draft, out error)) return false;
            draft.Compact = true;
            return true;
        }

        private bool StartTowerDefenseRun()
        {
            towerDefenseError = null;
            int campaignLevelId = game != null && game.Sim != null ? game.Sim.LevelIndex : -1;
            string configResource = TowerDefenseLevelConfig.ResourcesPathForCampaignLevel(campaignLevelId);
            if (game == null || game.Sim == null || string.IsNullOrEmpty(configResource) ||
                game.SelectedLevel != campaignLevelId || !game.IsLevelUnlocked(campaignLevelId))
            {
                towerDefenseError = "Tower Defense requiere seleccionar un nivel Defense desbloqueado.";
                return false;
            }
            if (!game.CanStartRound)
            {
                towerDefenseError = "Falta stock para iniciar. Revisá Mercado.";
                return false;
            }
            Camera camera = Camera.main;
            if (camera == null)
            {
                towerDefenseError = "No hay una cámara activa para proyectar el recorrido.";
                return false;
            }
            towerDefenseSourceConfig = Resources.Load<TowerDefenseLevelConfig>(configResource);
            if (towerDefenseSourceConfig == null)
            {
                towerDefenseError = "Falta la configuración " + configResource + ".";
                return false;
            }
            towerDefenseBackdrop = Resources.Load<Texture2D>(towerDefenseSourceConfig.BackgroundResource);
            if (towerDefenseBackdrop == null) towerDefenseBackdrop = campaignLevelId == 9 ? camionerosBackground : losRedondosBackground;
            towerDefenseCrest = LevelCrestForLevel(campaignLevelId);
            if (towerDefenseBackdrop == null)
            {
                towerDefenseError = "No se pudo cargar el fondo de " + towerDefenseSourceConfig.DisplayName + ".";
                return false;
            }

            Vector2 screenSize = TowerDefenseRenderSize(camera);
            try
            {
                // Validate the normal layout, and its compact fallback if needed, before committing an attempt.
                if (!TryFitTowerDefenseLayout(camera, CanvasViewport(screenSize, Screen.safeArea), true,
                        out TowerDefenseLayoutDraft draft, out string error))
                {
                    towerDefenseError = error;
                    return false;
                }
                towerDefenseRuntimeConfig = draft.Config;
                if (!game.TryStartTowerDefenseRoundWithSaveFeedback(out bool initialSaveRejected))
                {
                    towerDefenseError = initialSaveRejected ? "No se pudo guardar el inicio. Volvé a intentar."
                        : "No se pudo iniciar el intento de campaña.";
                    if (towerDefenseRuntimeConfig != null) Destroy(towerDefenseRuntimeConfig);
                    towerDefenseRuntimeConfig = null;
                    return false;
                }
                towerDefenseMode = draft.Mode;
                towerDefenseMode.Begin();
                if (towerDefensePresentation == null)
                    towerDefensePresentation = GetComponent<TowerDefenseWorldPresentation>() ?? gameObject.AddComponent<TowerDefenseWorldPresentation>();
                if (!towerDefensePresentation.Configure(this, towerDefenseRuntimeConfig, towerDefenseMode, camera,
                        screenSize))
                    throw new InvalidOperationException("No se pudieron ensamblar las vallas y estaciones del nivel.");
                if (!game.AttachTowerDefenseRuntime(towerDefenseMode))
                    throw new InvalidOperationException("No se pudo guardar el comienzo de la partida de campaña.");
                towerDefenseSelectedRole = -1;
                    towerDefenseActive = true;
                hubActive = false;
                levelSelectActive = false;
                towerDefenseScreenSize = screenSize;
                towerDefenseSafeArea = Screen.safeArea;
                towerDefenseCompactViewport = draft.Compact ? draft.Viewport : (Rect?)null;
                logicalCanvasHeight = CanvasLogicalHeight(draft.Viewport);
                layoutVerticalScale = logicalCanvasHeight / H;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
                // The old tutorial explains choosing one of two shared areas; the horizontal kitchen version hires with one tap.
                if (campaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId && !towerDefenseMode.FreeConstruction &&
                    PlayerPrefs.GetInt(CamionerosTutorialCompletedKey, 0) == 0)
                {
                    towerDefenseTutorialActive = true;
                    towerDefenseTutorialStep = 0;
                    towerDefenseMode.Pause();
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Tower Defense could not start: " + exception.Message);
                towerDefenseError = "No se pudo iniciar Tower Defense: " + exception.Message;
                if (towerDefenseMode != null && game.IsTowerDefenseCampaignRun)
                    game.ExitTowerDefenseRound(towerDefenseMode);
                EndTowerDefenseRun();
                return false;
            }
        }

        private void EndTowerDefenseRun()
        {
            towerDefenseTutorialActive = false;
            towerDefenseTutorialStep = 0;
            towerDefenseSelectedRole = -1;
            if (towerDefensePresentation != null) towerDefensePresentation.DisposePresentation();
            if (towerDefenseMode != null)
            {
                game?.DetachTowerDefenseRuntime(towerDefenseMode);
                towerDefenseMode.Stop();
            }
            towerDefenseMode = null;
            towerDefenseActive = false;
            towerDefenseCompactViewport = null;
            if (towerDefenseRuntimeConfig != null)
            {
                Destroy(towerDefenseRuntimeConfig);
                towerDefenseRuntimeConfig = null;
            }
            TowerDefenseGameMode.SetCampaignStepSuspended(false);
        }

        private void UpdateTowerDefenseFrame()
        {
            Camera camera = Camera.main;
            Vector2 screenSize = TowerDefenseRenderSize(camera);
            Rect activeViewport = towerDefenseCompactViewport ?? CanvasViewport(screenSize, Screen.safeArea);
            logicalCanvasHeight = CanvasLogicalHeight(activeViewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            if (!viewResourcesReady || game == null) return;
            if (!towerDefenseActive || towerDefenseMode == null) return;
            if (game.Sim != null && game.Sim.IsExternalCampaignRuntime && !game.IsPlayerActive)
            {
                towerDefenseSelectedRole = -1;
                towerDefenseMode.Pause();
                return;
            }
            if (!TowerDefensePlacementInputAllowed) towerDefenseSelectedRole = -1;
            if (screenSize != towerDefenseScreenSize || Screen.safeArea != towerDefenseSafeArea)
                ReflowTowerDefense(screenSize);
            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Playing)
                towerDefenseMode.Step(Time.deltaTime);
            // Back from the background (2026-11-26): up to 10 s of the time away per frame, in the mode's own 1 s calls.
            for (int i = 0; i < 10 && game.PendingBackgroundCatchUpSeconds > 0f && towerDefenseMode.Phase == TowerDefenseRunPhase.Playing; i++)
                towerDefenseMode.Step(game.TakeBackgroundCatchUp(1f));
            if (towerDefensePresentation != null) towerDefensePresentation.Sync(towerDefenseMode);
            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
            {
                if (game.CompleteTowerDefenseRound(towerDefenseMode))
                {
                    EndTowerDefenseRun();
                    hubActive = false; marketActive = false; marketFromSelector = false; levelSelectActive = false;
                }
                else towerDefenseError = "No se pudo guardar el resultado de Tower Defense.";
            }
        }

        private void ReflowTowerDefense(Vector2 screenSize)
        {
            Camera camera = Camera.main;
            string error = null;
            if (camera == null || !TryFitTowerDefenseLayout(camera, CanvasViewport(screenSize, Screen.safeArea), false,
                    out TowerDefenseLayoutDraft draft, out error))
            {
                towerDefenseError = camera == null ? "No se pudo recalcular la cámara." : error;
                towerDefenseMode.Pause();
                return;
            }

            towerDefenseMode.Reflow(draft.Route, draft.WorldPixel, draft.FirstZone, draft.SecondZone);
            if (!towerDefenseMode.LastReflowSucceeded)
            {
                Destroy(draft.Config);
                towerDefenseError = "El área quedó demasiado pequeña para esta distribución. Restaurá el formato anterior.";
                return;
            }
            TowerDefenseLevelConfig previous = towerDefenseRuntimeConfig;
            towerDefenseRuntimeConfig = draft.Config;
            if (towerDefensePresentation != null)
            {
                towerDefensePresentation.ResizeTarget(screenSize);
                towerDefensePresentation.RebuildRoute(draft.Config, screenSize);
            }
            if (previous != null) Destroy(previous);
            towerDefenseScreenSize = screenSize;
            towerDefenseSafeArea = Screen.safeArea;
            towerDefenseCompactViewport = draft.Compact ? draft.Viewport : (Rect?)null;
            logicalCanvasHeight = CanvasLogicalHeight(draft.Viewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            towerDefenseError = null;
        }

        private void DrawTowerDefenseUi(Rect viewport)
        {
            viewport = towerDefenseCompactViewport ?? viewport;
            float logicalHeight = CanvasLogicalHeight(viewport);
            logicalCanvasHeight = logicalHeight;
            layoutVerticalScale = logicalHeight / H;
            Matrix4x4 previous = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            Texture2D background = towerDefenseActive ? towerDefenseBackdrop : (hubBackdrop != null ? hubBackdrop : backdrop);
            Vector2 screenSize = TowerDefenseRenderSize(Camera.main);
            if (!towerDefenseActive && background != null)
                GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), background, ScaleMode.ScaleAndCrop, true);
            else if (!towerDefenseActive)
                GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), Texture2D.blackTexture, ScaleMode.StretchToFill, true);
            float hitTestVerticalScale = layoutVerticalScale;
            try
            {
                // TD bounds are authored against logicalCanvasHeight; drawing must not apply
                // the legacy 960-high canvas multiplier a second time.
                layoutVerticalScale = 1f;
                GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y), Quaternion.identity,
                    new Vector3(viewport.width / W, viewport.width / W, 1f));
                GUI.color = Color.white;
                DrawTowerDefenseSceneLayers(logicalHeight);
                GUI.matrix = previous;
                if (towerDefenseActive && towerDefensePresentation != null && towerDefensePresentation.OverlayTexture != null)
                {
                    Rect overlay = new Rect(0f, -TowerDefenseResponsiveLayout.VisualStageLift * viewport.width / W, screenSize.x, screenSize.y);
                    if (TowerDefenseMirrored)
                    {
                        // Mirrored about the playable viewport's centre, the same axis the bubbles and dialogues use.
                        overlay.x = 2f * (viewport.x + viewport.width * .5f) - screenSize.x;
                        GUI.DrawTextureWithTexCoords(overlay, towerDefensePresentation.OverlayTexture, new Rect(1f, 0f, -1f, 1f), true);
                    }
                    else GUI.DrawTexture(overlay, towerDefensePresentation.OverlayTexture, ScaleMode.StretchToFill, true);
                }
                GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y), Quaternion.identity,
                    new Vector3(viewport.width / W, viewport.width / W, 1f));
                GUI.color = Color.white;
                EnsureTowerDefenseStyles();
                DrawTowerDefensePlacementOverlay(viewport);
                if (towerDefenseActive && towerDefenseMode != null)
                {
                    DrawTowerDefenseOrderBubbles(viewport);
                    DrawTowerDefenseCrewChants(viewport);
                }
                DrawTowerDefenseHud(logicalHeight);
                if (towerDefenseTutorialActive) DrawCamionerosTutorial();
                if (towerDefenseCompactViewport.HasValue) MaskTowerDefenseViewport(viewport, screenSize);
            }
            finally
            {
                layoutVerticalScale = hitTestVerticalScale;
                GUI.matrix = previous;
                GUI.color = oldColor;
            }
        }

        private static void MaskTowerDefenseViewport(Rect viewport, Vector2 size)
        {
            // The source and overlay cameras still project to the full display. Mask the areas
            // outside the playable safe rectangle, including fans waiting off the route edge.
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(.024f, .034f, .05f, 1f);
            Texture2D fill = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(0f, 0f, viewport.xMin, size.y), fill);
            GUI.DrawTexture(new Rect(viewport.xMax, 0f, size.x - viewport.xMax, size.y), fill);
            GUI.DrawTexture(new Rect(viewport.xMin, 0f, viewport.width, viewport.yMin), fill);
            GUI.DrawTexture(new Rect(viewport.xMin, viewport.yMax, viewport.width, size.y - viewport.yMax), fill);
        }

        private void DrawTowerDefenseHud(float height)
        {
            if (towerDefenseMode == null) return;
            if (IsPilgrimConstruction) { DrawPilgrimHud(height); return; }
            DrawTowerDefenseCampaignHud();

            float footerTop = height - TowerDefenseResponsiveLayout.FooterReserve;
            DrawFooterBluePanel(LayoutRect(new Rect(0f, footerTop, W, height - footerTop)), layoutVerticalScale);
            if (towerDefenseCrest != null)
                DrawLevelCrest(LayoutRect(new Rect(18f, footerTop + 9f, 34f, 42f)), towerDefenseCrest);
            string[] identity = (towerDefenseSourceConfig != null ? towerDefenseSourceConfig.DisplayName : "Tower Defense")
                .Split(new[] {'/', '–', '—'});
            string title = identity.Length > 0 ? identity[0].Trim().ToUpperInvariant() : "TOWER DEFENSE";
            string location = identity.Length > 1 ? identity[1].Trim().ToUpperInvariant() : string.Empty;
            DrawTowerDefenseLabel(new Rect(58f, footerTop + 2f, W - 76f, 29f), title, towerDefenseTitle);
            DrawTowerDefenseLabel(new Rect(58f, footerTop + 29f, W - 76f, 22f), location, towerDefenseSmallLabel);

            bool active = towerDefenseMode.Phase == TowerDefenseRunPhase.Playing;
            Rect[] hireBounds = TowerDefenseHireBounds(height);
            for (int i = 0; i < TowerDefenseHireRoles.Length; i++)
            {
                StreetWorkerRole role = TowerDefenseHireRoles[i];
                bool available = active && (towerDefenseSelectedRole == i || CanSelectTowerDefenseRole(i));
                int cost = towerDefenseMode.HireCost(role);
                DrawTowerDefenseHireCard(hireBounds[i], TowerDefenseHireFirstAction + i, role,
                    cost, towerDefenseMode.WorkersCount(role), available, towerDefenseSelectedRole == i);
            }

            if (towerDefenseSelectedRole >= 0)
                DrawTowerDefenseLabel(new Rect(12f, footerTop + 51f, W - 24f, 18f),
                    string.IsNullOrEmpty(towerDefenseError) ? "ELEGÍ UN ÁREA PARA COLOCARLO" : towerDefenseError,
                    towerDefenseSmallLabel);

            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused || towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
            {
                DrawTowerDefensePanel(new Rect(65f, height * .43f, W - 130f, 112f), new Color(.015f, .04f, .08f, .92f));
                string status = towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "PARTIDA EN PAUSA"
                    : towerDefenseMode.HasWon ? "¡TODOS ATENDIDOS!" : "UN HINCHA LLEGÓ A LA SALIDA";
                DrawTowerDefenseLabel(new Rect(78f, height * .45f, W - 156f, 42f), status, towerDefenseTitle);
                if (towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
                    DrawTowerDefenseLabel(new Rect(78f, height * .50f, W - 156f, 40f),
                        "Hinchas atendidos: " + towerDefenseMode.ServedFans + "/" + towerDefenseMode.TotalFanCount,
                        towerDefenseSmallLabel);
            }
            if (!string.IsNullOrEmpty(towerDefenseError) && towerDefenseSelectedRole < 0)
                DrawTowerDefenseLabel(new Rect(24f, height - 189f, W - 48f, 18f), towerDefenseError, towerDefenseSmallLabel);
        }

        private Rect[] TowerDefensePlacementAreaBounds(Rect viewport)
        {
            Camera camera = Camera.main;
            if (camera == null || towerDefenseMode == null || towerDefenseRuntimeConfig == null || viewport.width <= 0f)
                return Array.Empty<Rect>();
            if (towerDefenseRuntimeConfig.SourceCampaignLevelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId && !towerDefenseMode.FreeConstruction)
            {
                StreetWorkerRole selectedRole = towerDefenseSelectedRole >= 0 && towerDefenseSelectedRole < TowerDefenseHireRoles.Length
                    ? TowerDefenseHireRoles[towerDefenseSelectedRole] : StreetWorkerRole.Parrillero;
                float scale = viewport.width / W;
                float redondosScreenHeight = TowerDefenseRenderSize(camera).y;
                var bays = new Rect[2];
                for (int zone = 0; zone < bays.Length; zone++)
                {
                    Rect world = towerDefenseMode.RightBayBounds(selectedRole, zone);
                    Vector3 upperLeft = camera.WorldToScreenPoint(new Vector3(world.xMin, world.yMax, 0f));
                    Vector3 lowerRight = camera.WorldToScreenPoint(new Vector3(world.xMax, world.yMin, 0f));
                    float left = (upperLeft.x - viewport.x) / scale;
                    float right = (lowerRight.x - viewport.x) / scale;
                    float top = (redondosScreenHeight - upperLeft.y - viewport.y) / scale - TowerDefenseResponsiveLayout.VisualStageLift;
                    float bottom = (redondosScreenHeight - lowerRight.y - viewport.y) / scale - TowerDefenseResponsiveLayout.VisualStageLift;
                    bays[zone] = Rect.MinMaxRect(left, top, right, bottom);
                }
                return bays;
            }
            TowerDefenseRouteGeometryConfig route = towerDefenseRuntimeConfig.Route;
            Vector2[] centerline = towerDefenseMode.RouteGeometry.Centerline;
            int stride = route.TurnSegments + 1;
            if (centerline.Length <= stride * 2) return Array.Empty<Rect>();
            float halfHorizontal = (route.HorizontalCorridorWidth > 0f
                ? route.HorizontalCorridorWidth : route.CorridorWidth) * .5f;
            float halfVertical = route.CorridorWidth * .5f;
            float canvasScale = viewport.width / W;
            float screenHeight = TowerDefenseRenderSize(camera).y;
            var areas = new Rect[2];
            for (int zone = 0; zone < areas.Length; zone++)
            {
                int row = zone * stride;
                Vector2 turn = centerline[row + 1];
                Vector3 upper = camera.WorldToScreenPoint(new Vector3(turn.x,
                    centerline[row].y - halfHorizontal, 0f));
                Vector3 lower = camera.WorldToScreenPoint(new Vector3(turn.x,
                    centerline[(zone + 1) * stride].y + halfHorizontal, 0f));
                Vector3 innerWall = camera.WorldToScreenPoint(new Vector3(
                    turn.x + (zone % 2 == 0 ? -halfVertical : halfVertical), turn.y, 0f));
                float wallX = (innerWall.x - viewport.x) / canvasScale;
                float left = zone % 2 == 0 ? 8f : wallX + 10f;
                float right = zone % 2 == 0 ? wallX - 10f : W - 8f;
                float top = (screenHeight - upper.y - viewport.y) / canvasScale - TowerDefenseResponsiveLayout.VisualStageLift + 10f;
                float bottom = (screenHeight - lower.y - viewport.y) / canvasScale - TowerDefenseResponsiveLayout.VisualStageLift - 10f;
                top = Mathf.Max(0f, top);
                bottom = Mathf.Min(logicalCanvasHeight - TowerDefenseResponsiveLayout.FooterReserve - 4f, bottom);
                areas[zone] = right > left && bottom > top ? Rect.MinMaxRect(left, top, right, bottom) : Rect.zero;
            }
            return areas;
        }

        private void DrawTowerDefensePlacementOverlay(Rect viewport)
        {
            if (IsPilgrimConstruction) return;
            if (towerDefenseSelectedRole < 0 || !TowerDefensePlacementInputAllowed) return;
            StreetWorkerRole role = TowerDefenseHireRoles[towerDefenseSelectedRole];
            Rect[] areas = TowerDefensePlacementAreaBounds(viewport);
            for (int zone = 0; zone < areas.Length; zone++)
            {
                Rect area = areas[zone];
                bool available = towerDefenseMode.CanPlaceWorker(role, zone);
                // Shared area hint only: no frame, role label or capacity counters.
                FillRect(area, available ? new Color(.015f, .10f, .18f, .20f)
                    : new Color(.20f, .025f, .025f, .22f));
                DrawTowerDefenseClickTarget(area, TowerDefensePlaceFirstAction + zone, true);
            }
        }

        private void DrawTowerDefensePlacementBorder(Rect bounds, Color color)
        {
            const float thickness = 2f;
            FillRect(new Rect(bounds.x, bounds.y, bounds.width, thickness), color);
            FillRect(new Rect(bounds.x, bounds.yMax - thickness, bounds.width, thickness), color);
            FillRect(new Rect(bounds.x, bounds.y, thickness, bounds.height), color);
            FillRect(new Rect(bounds.xMax - thickness, bounds.y, thickness, bounds.height), color);
        }

        private void DrawTowerDefenseCampaignHud() =>
            DrawTowerDefenseStatusHud(towerDefenseMode.Coins, towerDefenseMode.Elapsed,
                TowerDefenseWaveCaption(towerDefenseMode));

        private static string TowerDefenseWaveCaption(TowerDefenseGameMode mode) =>
            "PEDIDOS " + mode.OrdersCompleted + "/" + mode.TotalFanCount;

        private void DrawTowerDefenseStatusHud(int coins, float elapsed, string waveCaption = null)
        {
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            float previousVertical = layoutVerticalScale;
            try
            {
                // Read-only HUD matches campaign: physical top, independent of the safe-area viewport.
                layoutVerticalScale = 1f;
                bool compact = !string.IsNullOrEmpty(waveCaption) && towerDefenseCompactViewport.HasValue;
                Rect view = towerDefenseCompactViewport.GetValueOrDefault();
                GUI.matrix = compact
                    ? Matrix4x4.TRS(view.position, Quaternion.identity, new Vector3(view.width / W, view.width / W, 1f))
                    : Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.width / W, 1f));
                GUI.color = Color.white;
                Rect cutout = compact ? Rect.zero : TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
                EnsureHudBarTexture(cutout);
                Texture2D bar = cutout.width > 0f ? cutoutHudBarTexture : hudBarTexture;
                GUI.DrawTexture(new Rect(0f, 0f, W, HudBarHeightFor(cutout)), bar, ScaleMode.StretchToFill, true);
                DrawHudIcon(HudCoinIconBounds(cutout), 10);
                float statusScale = HudStatusScale(cutout);
                DrawHudNumber(HudStatusTextBounds(0, cutout), coins.ToString(),
                    Mathf.RoundToInt(HudStatusValueFontSize * statusScale));
                DrawHudNumber(HudStatusTextBounds(1, cutout), FormatElapsedTime(elapsed),
                    Mathf.RoundToInt(HudClockFontSize * statusScale));
                // 2026-11-24 (a pedido): no "PEDIDOS n/total" panel under the top bar in any Defense level, so the player does not know how many
                // orders are left (La Renga lost it on 2026-11-23). The caption still selects the compact HUD above.
                hudQuotaProductCount = 0; // Defense has no traditional product quotas or deadline.
            }
            finally
            {
                GUI.matrix = previous;
                GUI.color = previousColor;
                layoutVerticalScale = previousVertical;
            }
        }

        private void DrawTowerDefenseOrderBubbles(Rect viewport)
        {
            if (towerDefenseMode == null || items == null || viewport.width <= 0f) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            float canvasScale = viewport.width / W;
            float feetOffset = towerDefenseMode.WorldUnitsPerLogicalPixel * 38f;
            IReadOnlyList<TowerDefenseFanState> fans = towerDefenseMode.Fans;
            int levelId = towerDefenseSourceConfig != null
                ? towerDefenseSourceConfig.SourceCampaignLevelId : -1;
            bool avoidOverlap = towerDefenseSourceConfig != null && towerDefenseSourceConfig.UsesLaRengaDynamics;
            bool mirrored = TowerDefenseMirrored;
            laRengaOrderBubbles.Clear();
            // Camioneros and Peregrinación bubbles share the viewport edge with the
            // world-space fan during progressive entry. Clamping either bubble into view
            // makes it pop at the margin before the supporter has walked onto the screen.
            GUI.BeginGroup(new Rect(0f, 0f, W, logicalCanvasHeight));
            try
            {
                for (int fanIndex = 0; fanIndex < fans.Count; fanIndex++)
                {
                    TowerDefenseFanState fan = fans[fanIndex];
                    int visibleLines = TowerDefenseVisiblePendingLineCount(fan);
                    if (visibleLines <= 0) continue;

                    Vector2 routePosition = fan.RouteFollower.Position;
                    Vector3 projectedFeet = camera.WorldToScreenPoint(new Vector3(
                        routePosition.x, routePosition.y - feetOffset, 0f));
                    if (projectedFeet.z <= 0f) continue;
                    Vector2 fanFeet = new Vector2(
                        (projectedFeet.x - viewport.x) / canvasScale,
                        (Screen.height - projectedFeet.y - viewport.y) / canvasScale
                            - TowerDefenseResponsiveLayout.VisualStageLift);
                    float visibleFanFrameLogical = Mathf.Clamp(fanFeet.x +
                        TowerDefenseResponsiveLayout.FanFrameWidthLogical * .5f, 0f,
                        TowerDefenseResponsiveLayout.FanFrameWidthLogical);
                    if (!ShouldDrawTowerDefenseBubbleForFanEntry(levelId, visibleFanFrameLogical)) continue;
                    if (avoidOverlap && (fanFeet.x < 0f || fanFeet.x > W)) continue;
                    // TD lanes have far more vertical separation than the campaign queue;
                    // keep the existing full-size product icons instead of shrinking each
                    // two-item order bubble to fit a regular queue row.
                    Rect bubble = TowerDefenseOrderBubbleBounds(fanFeet, visibleLines, levelId);
                    // La Renga: a fan still entering would have its bubble squeezed against the left margin by the clamp below,
                    // sliding it onto the previous fan's bubble and lifting it. Wait until the bubble fits beside its own fan.
                    if (avoidOverlap && bubble.x < 6f) continue;
                    // The new directional atlas uses a foot pivot, not a centered frame.
                    // Anchor ID13 to the actual rendered head, including its gait bob.
                    Bounds visualBounds;
                    if ((levelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId ||
                        levelId == TowerDefenseLevelConfig.LosRedondosCampaignLevelId || IsPilgrimConstruction) && towerDefensePresentation != null &&
                        towerDefensePresentation.TryGetFanVisualBounds(fan.Id, out visualBounds))
                    {
                        Vector3 head = camera.WorldToScreenPoint(new Vector3(visualBounds.center.x, visualBounds.max.y, 0f));
                        float headY = (Screen.height - head.y - viewport.y) / canvasScale
                            - TowerDefenseResponsiveLayout.VisualStageLift;
                        bubble.y += headY - TowerDefenseOrderBubbleHeadClearanceLogical - bubble.yMax;
                    }
                    if (!UsesProgressiveTowerDefenseFanEntry(levelId))
                        bubble.x = Mathf.Clamp(bubble.x, 6f, W - bubble.width - 6f);
                    if (avoidOverlap) bubble = PlaceLaRengaOrderBubble(bubble, laRengaOrderBubbles);
                    if (bubble.yMax <= 0f || bubble.yMin >= logicalCanvasHeight) continue;
                    if (avoidOverlap) laRengaOrderBubbles.Add(bubble);

                    // Mirrored world (2026-11-24): the bubbles were laid out as if the fans entered from the left; reflect the final rectangle and its art.
                    if (mirrored) bubble.x = W - bubble.xMax;
                    DrawTowerDefenseOrderBubbleArt(bubble, mirrored);
                    int shown = 0;
                    for (int lineIndex = 0; lineIndex < fan.Order.Count; lineIndex++)
                    {
                        TowerDefenseOrderLine line = (TowerDefenseOrderLine)fan.Order[lineIndex];
                        if (line.Remaining <= 0) continue;
                        Rect row = OrderLineBounds(bubble, shown, visibleLines, 1f);
                        DrawProductIcon(OrderIconBounds(row, 1f), line.Product, false);
                        int originalFontSize = text.fontSize;
                        Rect quantity = OrderQuantityBounds(row, 1f);
                        text.fontSize = FitOrderQuantityFontSize(text, line.Remaining.ToString(), quantity.width);
                        GUI.Label(quantity, line.Remaining.ToString(), text);
                        text.fontSize = originalFontSize;
                        shown++;
                    }
                }
            }
            finally { GUI.EndGroup(); }
        }

        // A worker calls out once after coming back to its own waiting spot or work post, never just for being hired.
        private readonly Dictionary<int, bool> crewWasSettled = new Dictionary<int, bool>();
        private readonly Dictionary<int, float> crewChantStart = new Dictionary<int, float>();
        private TowerDefenseGameMode crewChantMode;

        private static string CrewChantTextFor(StreetWorkerRole role, bool activeCooks, bool sellsVacio)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero when !activeCooks:
                case StreetWorkerRole.Mozo when activeCooks:
                    return "¡Hay\nchori y Paty!";
                case StreetWorkerRole.ParrilleroPremium: return sellsVacio ? "¡Hay Bondio\ny Vacío!" : "¡Hay Bondio!";
                case StreetWorkerRole.Cocacolero: return "¡Hay coca\ny birra!";
                case StreetWorkerRole.Fernetero: return "¡Hay Fernet!";
                default: return null;
            }
        }

        private static bool CrewIsSettled(TowerDefenseGameMode mode, TowerDefenseWorkerState worker) =>
            worker.IsIdle && Vector2.Distance(worker.Position, mode.WaitSpot(worker)) <= 2f * mode.WorldUnitsPerLogicalPixel;

        private void DrawTowerDefenseCrewChants(Rect viewport)
        {
            if (towerDefenseMode == null || !towerDefenseMode.UsesCampaignIdleCrew || items == null || viewport.width <= 0f || text == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            if (!ReferenceEquals(crewChantMode, towerDefenseMode)) { crewChantMode = towerDefenseMode; crewWasSettled.Clear(); crewChantStart.Clear(); }
            float now = towerDefenseMode.Elapsed, canvasScale = viewport.width / W;
            float pixel = towerDefenseMode.WorldUnitsPerLogicalPixel;
            float headWorld = pixel * TowerDefenseGameMode.WorkerVisualHeightLogical;
            Color previousColor = GUI.color;
            GUI.BeginGroup(new Rect(0f, 0f, W, logicalCanvasHeight));
            try
            {
                IReadOnlyList<TowerDefenseWorkerState> crew = towerDefenseMode.Workers;
                bool activeCooks = towerDefenseMode.Kitchen.ActiveCooks;
                bool sellsVacio = towerDefenseMode.ContainsProduct(3);
                for (int i = 0; i < crew.Count; i++)
                {
                    TowerDefenseWorkerState worker = crew[i];
                    string chant = CrewChantTextFor(worker.Role, activeCooks, sellsVacio);
                    if (chant == null) continue;
                    bool settled = CrewIsSettled(towerDefenseMode, worker), wasSettled;
                    if (!crewWasSettled.TryGetValue(worker.Id, out wasSettled)) wasSettled = true;
                    if (settled && !wasSettled) crewChantStart[worker.Id] = now;
                    crewWasSettled[worker.Id] = settled;
                    float started;
                    if (!crewChantStart.TryGetValue(worker.Id, out started)) continue;
                    float age = now - started;
                    if (age < 0f || age >= CrewChantSeconds) continue;
                    Vector3 head = camera.WorldToScreenPoint(new Vector3(worker.Position.x, worker.Position.y + headWorld, 0f));
                    if (head.z <= 0f) continue;
                    if (TowerDefenseMirrored) head.x = 2f * (viewport.x + viewport.width * .5f) - head.x;
                    float headX = (head.x - viewport.x) / canvasScale;
                    float headY = (Screen.height - head.y - viewport.y) / canvasScale - TowerDefenseResponsiveLayout.VisualStageLift;
                    DrawCrewChantBubble(chant, headX, headY, age);
                }
            }
            finally { GUI.EndGroup(); GUI.color = previousColor; }
        }

        private static Rect PlaceLaRengaOrderBubble(Rect bubble, IReadOnlyList<Rect> alreadyPlaced)
        {
            const float gap = 3f;
            // Preserve horizontal alignment to the fan. Only lift when another full-size order is in the way.
            for (int i = 0; i < alreadyPlaced.Count;)
            {
                Rect other = alreadyPlaced[i++];
                if (bubble.xMax + gap <= other.xMin || other.xMax + gap <= bubble.xMin ||
                    bubble.yMax + gap <= other.yMin || other.yMax + gap <= bubble.yMin) continue;
                bubble.y = other.yMin - gap - .05f - bubble.height;
                i = 0; // A newly raised rectangle may now intersect a bubble checked before this one.
            }
            return bubble;
        }

        private const int TowerDefenseMaxOrderRows = 4;
        private static Rect TowerDefenseOrderBubbleBounds(Vector2 fanFeet, int visibleLines, int campaignLevelId)
        {
            Rect bubble = OrderBubbleBounds(fanFeet, visibleLines, 20f);
            // Defense orders can list up to four products (2026-11-26): the bubble grows a row per product instead of the
            // campaign's two rows and "more" footer, keeping its bottom (tail) where it was.
            int rows = Mathf.Clamp(visibleLines, 1, TowerDefenseMaxOrderRows);
            if (visibleLines > StreetCustomer.MaxVisibleOrderProducts)
            {
                float bottom = bubble.yMax;
                bubble.height = OrderPaddingY * 2f + rows * OrderRowHeight + OrderTailHeight;
                bubble.y = bottom - bubble.height;
            }
            if (campaignLevelId != TowerDefenseLevelConfig.CamionerosCampaignLevelId) return bubble;

            // The shared campaign anchor lets the speech tail overlap the top of the
            // 76px fan frame. Keep the full TD bubble, including its tail, clear of the head.
            float desiredBottom = fanFeet.y - StreetSceneLayout.CustomerHeight
                - TowerDefenseOrderBubbleHeadClearanceLogical;
            bubble.y += desiredBottom - bubble.yMax;
            return bubble;
        }

        /// <summary>Los Redondos plays La Renga's C inverted: the whole Defense world is drawn mirrored left-right (2026-11-24, a pedido).</summary>
        private bool TowerDefenseMirrored => towerDefenseSourceConfig != null && towerDefenseSourceConfig.MirrorHorizontally;

        // The order bubble's tail sits at the bottom right; mirrored it sits at the bottom left. Only the art flips, never the icons or numbers.
        private void DrawTowerDefenseOrderBubbleArt(Rect bubble, bool mirrored)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            if (mirrored)
                GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(bubble.center.x, 0f, 0f), Quaternion.identity, new Vector3(-1f, 1f, 1f))
                    * Matrix4x4.TRS(new Vector3(-bubble.center.x, 0f, 0f), Quaternion.identity, Vector3.one);
            DrawOrderBubble(bubble, 1f);
            GUI.matrix = previousMatrix;
        }

        private static bool ShouldDrawTowerDefenseBubbleForFanEntry(int campaignLevelId, float visibleFrameWidthLogical) =>
            !UsesProgressiveTowerDefenseFanEntry(campaignLevelId) ||
            visibleFrameWidthLogical >= TowerDefenseMinimumFanRevealBeforeOrderBubbleLogical;

        private static bool UsesProgressiveTowerDefenseFanEntry(int campaignLevelId) =>
            campaignLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId ||
            campaignLevelId == TowerDefenseLevelConfig.PeregrinacionLujanCampaignLevelId;

        private static int TowerDefenseVisiblePendingLineCount(TowerDefenseFanState fan)
        {
            if (fan == null) return 0;
            int count = 0;
            for (int i = 0; i < fan.Order.Count; i++)
                if (fan.Order[i].Remaining > 0) count++;
            return count;
        }

        private void DrawTowerDefenseSceneLayers(float canvasHeight)
        {
            float footerTop = IsPilgrimConstruction
                ? TowerDefenseResponsiveLayout.PilgrimFooterTop(canvasHeight, PilgrimFooterSourceLevelId)
                : canvasHeight - TowerDefenseResponsiveLayout.FooterReserve;
            DrawTowerDefenseSceneLayers(canvasHeight, towerDefenseBackdrop,
                towerDefenseSourceConfig != null ? towerDefenseSourceConfig.SourceCampaignLevelId : 9, footerTop);
        }

        private void DrawTowerDefenseSceneLayers(float canvasHeight, Texture2D levelBackdrop, int level, float footerTop)
        {
            if (backdrop == null || levelBackdrop == null || canvasHeight <= 0f) return;
            // The same level mural and street are also used before the run, without instantiating it.
            Rect themeSource = ClubVisualTheme.ForLevel(level).MuralSource;
            Rect muralSource = new Rect(themeSource.x * levelBackdrop.width, themeSource.y * levelBackdrop.height,
                themeSource.width * levelBackdrop.width, themeSource.height * levelBackdrop.height);
            Rect mural = new Rect(0f, 0f, W, TowerDefenseResponsiveLayout.RouteTopReserve - 24f
                - TowerDefenseResponsiveLayout.VisualStageLift);
            DrawRawCover(mural, levelBackdrop, muralSource);

            float sx = backdrop.width / StreetSceneLayout.ReferenceBackdropSize.x;
            float sy = backdrop.height / StreetSceneLayout.ReferenceBackdropSize.y;
            Rect sourceFarSidewalk = new Rect(0f, 150f * sy, backdrop.width, 30f * sy);
            Rect sourceRoad = new Rect(100f * sx, 200f * sy, 740f * sx, 280f * sy);
            Rect sourceNearSidewalk = new Rect(100f * sx, 495f * sy, 740f * sx, 50f * sy);
            float sidewalkTop = mural.yMax;
            const float farSidewalkHeight = 24f, nearSidewalkHeight = 30f, stallSpaceHeight = 90f;
            float roadBottom = footerTop - nearSidewalkHeight - stallSpaceHeight;
            Rect cleanRoad = new Rect(0f, sidewalkTop + farSidewalkHeight, W,
                Mathf.Max(1f, roadBottom - sidewalkTop - farSidewalkHeight));
            DrawRawCover(new Rect(0f, sidewalkTop, W, farSidewalkHeight), backdrop, sourceFarSidewalk);
            DrawRawCover(cleanRoad, backdrop, sourceRoad);
            DrawRawCover(new Rect(0f, roadBottom, W, nearSidewalkHeight), backdrop, sourceNearSidewalk);
            Rect sourcePavement = StreetSceneLayout.PavementContinuationSource(
                new Vector2(backdrop.width, backdrop.height));
            DrawRawCover(new Rect(0f, roadBottom + nearSidewalkHeight, W, stallSpaceHeight), backdrop, sourcePavement);
        }

        private void EnsureTowerDefenseStyles()
        {
            if (towerDefenseLabel == null)
            {
                towerDefenseLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 21 };
                towerDefenseLabel.normal.textColor = Color.white;
                towerDefenseLabel.wordWrap = true;
            }
            if (towerDefenseSmallLabel == null)
            {
                towerDefenseSmallLabel = new GUIStyle(towerDefenseLabel) { fontSize = 15 };
            }
            if (towerDefenseTitle == null)
            {
                towerDefenseTitle = new GUIStyle(towerDefenseLabel) { fontSize = 26 };
            }
            // Keep the Luján identity caption on the same Luckiest Guy face used by
            // the regular level badge instead of Unity's default IMGUI label font.
            if (IsPilgrimConstruction && menuFont != null)
            {
                towerDefenseTitle.font = menuFont;
                towerDefenseTitle.fontStyle = FontStyle.Normal;
                towerDefenseTitle.wordWrap = false;
            }
        }

        private void DrawTowerDefensePanel(Rect bounds, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            bounds.y *= layoutVerticalScale;
            GUI.DrawTexture(bounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void DrawTowerDefenseLabel(Rect bounds, string value, GUIStyle style)
        {
            bounds.y *= layoutVerticalScale;
            GUI.Label(bounds, value, style);
        }

        // No speed/pause/back button row in this mode; only the two hire cards.
        private static Rect[] TowerDefensePrimaryActionBounds(float height) => Array.Empty<Rect>();

        private static Rect[] TowerDefenseHireBounds(float height)
        {
            const float width = 234f, cardHeight = 120f, gap = 16f;
            float left = (W - (width * 2f + gap)) * .5f;
            return new[] { new Rect(left, height - 170f, width, cardHeight),
                new Rect(left + width + gap, height - 170f, width, cardHeight) };
        }

        private Rect CamionerosTutorialTargetBounds()
        {
            if (towerDefenseTutorialStep >= 2)
                return new Rect((W - 78f) * .5f, logicalCanvasHeight - 360f, 78f, 76f);
            Rect[] cards = TowerDefenseHireBounds(logicalCanvasHeight);
            return cards.Length > towerDefenseTutorialStep
                ? cards[towerDefenseTutorialStep]
                : Rect.zero;
        }

        private Rect CamionerosTutorialPanelBounds()
        {
            Rect target = CamionerosTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            return new Rect(x, target.yMin - 260f, width, LevelOneTutorialBubbleHeight);
        }

        private Rect CamionerosTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(CamionerosTutorialPanelBounds(), 1f);

        private Rect CamionerosTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(CamionerosTutorialPanelBounds(), 1f);

        private void DrawCamionerosTutorial()
        {
            if (towerDefenseMode == null || towerDefenseTutorialStep < 0 ||
                towerDefenseTutorialStep >= CamionerosTutorialHeadlines.Length) return;

            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = CamionerosTutorialPanelBounds();
            Rect target = CamionerosTutorialTargetBounds();
            if (towerDefenseTutorialStep >= 2)
            {
                int appearance = towerDefenseTutorialStep == 2 ? 0 : towerDefenseTutorialStep == 3 ? 3 : 7;
                DrawFan(target, CamionerosFanProfile.AtlasIdForAppearance(appearance), false, false,
                    ClubVisualTheme.ForLevel(TowerDefenseLevelConfig.CamionerosCampaignLevelId).Fans);
            }
            const bool targetAbove = false;
            const float tailWidth = 42f;
            float baseX = Mathf.Clamp(target.center.x, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, target.yMin);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(panel, tip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != towerDefenseTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(panel) || !levelOneTutorialBubbleTextureTip.Equals(tip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(panel, tip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = towerDefenseTutorialStep;
                levelOneTutorialBubbleTexturePanel = panel;
                levelOneTutorialBubbleTextureTip = tip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }

            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headline = new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 58f);
            DrawTutorialText(headline, CamionerosTutorialHeadlines[towerDefenseTutorialStep],
                levelOneTutorialTitleStyle, 25, 20);
            Rect explanation = new Rect(panel.x + 18f, panel.y + 72f, panel.width - 36f, 40f);
            DrawTutorialText(explanation, CamionerosTutorialExplanations[towerDefenseTutorialStep],
                levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(CamionerosTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = CamionerosTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            GUI.Label(skipBounds, "SALTAR", levelOneTutorialSkipStyle);
#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            if (GUI.Button(skipBounds, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
#endif
        }

        private void DrawTowerDefenseHireCard(Rect bounds, int action, StreetWorkerRole role,
            int cost, int count, bool available, bool selected)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            Rect drawn = selected ? StandardButtonVisualBounds(bounds, true) : bounds;
            Rect card = new Rect(0f, 0f, drawn.width, drawn.height);
            try
            {
                GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(drawn.x, drawn.y, 0f),
                    Quaternion.identity, Vector3.one);
                GUI.color = selected ? new Color(.60f, .79f, 1f, 1f)
                    : available ? Color.white : new Color(.57f, .57f, .57f, 1f);
                Texture2D texture = GetGlossyUpgradeCardTexture(card);
                if (texture != null) GUI.DrawTexture(card, texture, ScaleMode.StretchToFill, true);
                else FillRect(card, new Color(.025f, .14f, .40f, 1f));
                GUI.color = previousColor;
                if (selected) DrawTowerDefensePlacementBorder(card, new Color(1f, .86f, .18f, 1f));
                else if (available) DrawAffordableUpgradeBorder(card);
                int titleSize = upgradeTitle.fontSize;
                upgradeTitle.fontSize = FitUpgradeStatusFontSize(upgradeTitle, RoleCaption(role), UpgradeTitleBounds(card).width);
                UpgradeOutline(UpgradeTitleBounds(card), RoleCaption(role), upgradeTitle, new Color(.98f, .99f, 1f));
                upgradeTitle.fontSize = titleSize;
                int statusSize = upgradePercent.fontSize;
                string status = selected ? (IsPilgrimConstruction ? "COLOCAR" : "ELEGÍ ÁREA") : "EQUIPO " + count;
                upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, status, UpgradeStatusBounds(card).width);
                UpgradeOutline(UpgradeStatusBounds(card), status, upgradePercent, new Color(.55f, .90f, 1f));
                upgradePercent.fontSize = statusSize;
                if (role == StreetWorkerRole.Parrillero && parrilleroIcon != null)
                    DrawUpgradeOvalContent(card, parrilleroIcon, new Rect(22, 6, 1269, 1188));
                else if (role == StreetWorkerRole.Cocacolero && CocacoleroArtForLevel(TowerDefenseLevelConfig.CommonEconomicBalanceLevelId) != null)
                    DrawUpgradeOvalContent(card, CocacoleroArtForLevel(TowerDefenseLevelConfig.CommonEconomicBalanceLevelId), CocacoleroPoseForLevel(TowerDefenseLevelConfig.CommonEconomicBalanceLevelId, 0));
                if (role == StreetWorkerRole.ParrilleroPremium && premiumWorker != null)
                    DrawUpgradeOvalContent(card, premiumWorker, ParrilleroPoses[0]);
                else if (role == StreetWorkerRole.Mozo && mozoWorker != null)
                    DrawUpgradeOvalContent(card, mozoWorker, ParrilleroPoses[0]);
                else if (role == StreetWorkerRole.Fernetero && fernetWorker != null)
                    DrawUpgradeOvalContent(card, fernetWorker, MatchedCocacoleroPose(0, new Vector2(fernetWorker.width, fernetWorker.height)));
                string price = cost > 0 ? cost.ToString() : "MAX";
                if (cost > 0) Item(UpgradeCoinBounds(card), 10);
                int priceSize = upgradePrice.fontSize;
                upgradePrice.fontSize = FitUpgradePriceFontSize(upgradePrice, price, UpgradePriceTextBounds(card).width);
                UpgradeOutline(UpgradePriceTextBounds(card), price, upgradePrice, new Color(.98f, .99f, 1f));
                upgradePrice.fontSize = priceSize;
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
            DrawTowerDefenseClickTarget(bounds, action, available);
        }

        // Input System sends both role and area taps through HandlePointer. Do not
        // additionally dispatch IMGUI taps on Android, which would toggle twice.
        private void DrawTowerDefenseClickTarget(Rect bounds, int action, bool available)
        {
#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            bool previousEnabled = GUI.enabled;
            GUI.enabled = available;
            if (GUI.Button(LayoutRect(bounds), "", invisible)) NativeAction(action);
            GUI.enabled = previousEnabled;
#endif
        }

        /// <summary>Creates animation sprites from the same cropped role art used by the campaign.</summary>
        internal Sprite[] CreateTowerDefenseWorkerFrames(StreetWorkerRole role)
        {
            Texture2D texture = TowerDefenseWorkerTexture(role);
            if (texture == null) return Array.Empty<Sprite>();
            var frames = new Sprite[4];
            for (int frame = 0; frame < frames.Length; frame++)
                frames[frame] = CreateTowerDefenseWorkerFrame(role, texture, frame);
            return frames;
        }

        internal Sprite CreateTowerDefenseWorkerRearIdleFrame(StreetWorkerRole role)
        {
            Texture2D texture = TowerDefenseWorkerTexture(role);
            // Same standing rear pose (index 4) as StreetWorkerProjection.
            return texture != null ? CreateTowerDefenseWorkerFrame(role, texture, 4) : null;
        }

        private Texture2D TowerDefenseWorkerTexture(StreetWorkerRole role)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero: return parrillero;
                case StreetWorkerRole.ParrilleroPremium: return premiumWorker;
                case StreetWorkerRole.Mozo: return mozoWorker;
                case StreetWorkerRole.Cocacolero: return CocacoleroArtForLevel(TowerDefenseLevelConfig.CommonEconomicBalanceLevelId);
                default: return UsesMatchedCocacoleroArt(9) && fernetWorkerLater != null ? fernetWorkerLater : fernetWorker;
            }
        }

        private Sprite CreateTowerDefenseWorkerFrame(StreetWorkerRole role, Texture2D texture, int frame)
        {
            Rect topOrigin;
            if (role == StreetWorkerRole.Cocacolero) topOrigin = CocacoleroPoseForLevel(TowerDefenseLevelConfig.CommonEconomicBalanceLevelId, frame);
            else if (role == StreetWorkerRole.Fernetero) topOrigin = MatchedCocacoleroPose(frame, new Vector2(texture.width, texture.height));
            else topOrigin = ParrilleroPoses[frame];
            Rect spriteRect = new Rect(topOrigin.x, texture.height - topOrigin.yMax, topOrigin.width, topOrigin.height);
            Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(.5f, TowerDefenseGameMode.WorkerVisualPivotY), 100f, 0, SpriteMeshType.Tight);
            sprite.name = "TD " + role + " campaign pose " + frame;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Campaign kitchen crew bodies (eight directions, carry, pickup) for a Tower Defense presentation.</summary>
        internal StreetWorkerProjection CreateTowerDefenseCrewProjection(int levelId)
        {
            var projection = new StreetWorkerProjection(null, null);
            bool matched = UsesMatchedCocacoleroArt(levelId);
            projection.SetArt(parrillero, parrilleroDiagonal, premiumWorker, premiumDiagonal,
                CocacoleroArtForLevel(levelId), matched ? fernetWorkerLater : fernetWorker, matched);
            if (projection.Ready) return projection;
            projection.Dispose();
            return null;
        }

        /// <summary>
        /// Creates short delivery gestures from already-authored campaign poses. Coca's reach
        /// pose contains a bottle, so the delivery cue must stay hidden until the release.
        /// </summary>
        private Texture2D towerDefensePremiumCookingPoses;

        internal Sprite[] CreateTowerDefenseHandoffFrames(StreetWorkerRole role)
        {
            Texture2D texture;
            Rect[] poses;
            if (role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium)
            {
                texture = Resources.Load<Texture2D>("street-parrillero-cooking-arms-v1");
                poses = StreetWorkerProjection.CookingPoseBounds;
                // The Premium cooks in its own outfit, not the Parrillero's white apron (2026-11-26, a pedido): the same recolor
                // the campaign kitchen uses for its cooking poses, made once per view.
                if (role == StreetWorkerRole.ParrilleroPremium && texture != null)
                {
                    if (towerDefensePremiumCookingPoses == null)
                        towerDefensePremiumCookingPoses = StreetWorkerAppearance.CreateVariant(texture, role, poses, StreetWorkerProjection.CookingPoseAtlasSize);
                    if (towerDefensePremiumCookingPoses != null) texture = towerDefensePremiumCookingPoses;
                }
            }
            else if (role == StreetWorkerRole.Cocacolero)
            {
                texture = Resources.Load<Texture2D>("street-cocacolero-barrel-reach-v1");
                poses = texture != null ? new[] { new Rect(0f, 0f, texture.width, texture.height) } : Array.Empty<Rect>();
            }
            else
            {
                return Array.Empty<Sprite>();
            }

            if (texture == null || poses == null || poses.Length == 0) return Array.Empty<Sprite>();
            var frames = new Sprite[poses.Length];
            for (int frame = 0; frame < poses.Length; frame++)
            {
                Rect pose = poses[frame];
                Rect spriteRect = new Rect(pose.x, texture.height - pose.yMax, pose.width, pose.height);
                frames[frame] = Sprite.Create(texture, spriteRect, new Vector2(.5f, TowerDefenseGameMode.WorkerVisualPivotY), 100f, 0, SpriteMeshType.Tight);
                frames[frame].name = "TD " + role + " handoff pose " + frame;
                frames[frame].hideFlags = HideFlags.DontSave;
            }
            return frames;
        }
    }
}
