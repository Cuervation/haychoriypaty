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
        private static readonly StreetWorkerRole[] TowerDefenseHireRoles =
        {
            StreetWorkerRole.Parrillero, StreetWorkerRole.Cocacolero
        };

        private bool towerDefenseActive;
        private int towerDefenseSelectedRole = -1;
        private string towerDefenseError;
        private TowerDefenseLevelConfig towerDefenseSourceConfig;
        private TowerDefenseLevelConfig towerDefenseRuntimeConfig;
        private TowerDefenseGameMode towerDefenseMode;
        private TowerDefenseWorldPresentation towerDefensePresentation;
        private Texture2D towerDefenseBackdrop;
        private Texture2D towerDefenseCrest;
        private Texture2D towerDefenseCoinHud;
        private Rect towerDefenseCoinHudGap;
        private GUIStyle towerDefenseLabel, towerDefenseSmallLabel, towerDefenseTitle;
        private Vector2 towerDefenseScreenSize;
        private Rect towerDefenseSafeArea;

        private bool IsTowerDefenseNavigationActive => towerDefenseActive;

        private static Vector2 TowerDefenseRenderSize(Camera camera)
        {
            if (camera != null && camera.pixelWidth > 0 && camera.pixelHeight > 0)
                return new Vector2(camera.pixelWidth, camera.pixelHeight);
            return new Vector2(Screen.width, Screen.height);
        }

        private void ResetTowerDefenseUi()
        {
            if (towerDefenseCoinHud != null) Destroy(towerDefenseCoinHud);
            towerDefenseCoinHud = null;
            towerDefenseActive = false;
            towerDefenseSelectedRole = -1;
            towerDefenseError = null;
            towerDefenseBackdrop = null;
            towerDefenseCrest = null;
            towerDefenseLabel = towerDefenseSmallLabel = towerDefenseTitle = null;
            towerDefenseScreenSize = Vector2.zero;
            towerDefenseSafeArea = Rect.zero;
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
            if (game != null && game.Sim != null && game.Sim.IsExternalCampaignRuntime && !game.IsPlayerActive) return true;
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
                else towerDefenseError = "No se pudo guardar la salida de Camioneros.";
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

        private bool TowerDefensePlacementInputAllowed => towerDefenseMode != null &&
            towerDefenseMode.Phase == TowerDefenseRunPhase.Playing &&
            (game == null || game.Sim == null || !game.Sim.IsExternalCampaignRuntime || game.IsPlayerActive);

        private bool CanSelectTowerDefenseRole(int index) => TowerDefensePlacementInputAllowed &&
            index >= 0 && index < TowerDefenseHireRoles.Length &&
            (towerDefenseMode.CanPlaceWorker(TowerDefenseHireRoles[index], 0) ||
             towerDefenseMode.CanPlaceWorker(TowerDefenseHireRoles[index], 1));

        private int HitTowerDefenseAction(Vector2 point)
        {
            point.y *= layoutVerticalScale;
            if (!TowerDefensePlacementInputAllowed) return 0;
            Rect[] roleBounds = TowerDefenseHireBounds(logicalCanvasHeight);
            for (int i = 0; i < roleBounds.Length; i++)
                if (roleBounds[i].Contains(point) && (towerDefenseSelectedRole == i || CanSelectTowerDefenseRole(i)))
                    return TowerDefenseHireFirstAction + i;
            if (towerDefenseSelectedRole >= 0)
            {
                Camera camera = Camera.main;
                Rect viewport = CanvasViewport(TowerDefenseRenderSize(camera), Screen.safeArea);
                Rect[] areas = TowerDefensePlacementAreaBounds(viewport);
                for (int i = 0; i < areas.Length; i++)
                    if (areas[i].Contains(point)) return TowerDefensePlaceFirstAction + i;
            }
            return 0;
        }

        private bool StartTowerDefenseRun()
        {
            towerDefenseError = null;
            if (game == null || game.Sim == null || game.SelectedLevel != StreetSimulation.TowerDefenseCampaignLevelId ||
                game.Sim.LevelIndex != StreetSimulation.TowerDefenseCampaignLevelId ||
                !game.IsLevelUnlocked(StreetSimulation.TowerDefenseCampaignLevelId))
            {
                towerDefenseError = "Camioneros Tower Defense requiere seleccionar el nivel 11 desbloqueado.";
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
            towerDefenseSourceConfig = Resources.Load<TowerDefenseLevelConfig>("TowerDefense/Camioneros-PlazaDeMayo");
            if (towerDefenseSourceConfig == null)
            {
                towerDefenseError = "Falta la configuración TowerDefense/Camioneros-PlazaDeMayo.";
                return false;
            }
            towerDefenseBackdrop = Resources.Load<Texture2D>(towerDefenseSourceConfig.BackgroundResource);
            if (towerDefenseBackdrop == null) towerDefenseBackdrop = camionerosBackground;
            towerDefenseCrest = Resources.Load<Texture2D>("street-camioneros-crest-v1");
            if (towerDefenseBackdrop == null)
            {
                towerDefenseError = "No se pudo cargar el fondo existente de Camioneros / Plaza de Mayo.";
                return false;
            }

            Vector2 screenSize = TowerDefenseRenderSize(camera);
            Rect viewport = CanvasViewport(screenSize, Screen.safeArea);
            float logicalHeight = CanvasLogicalHeight(viewport);
            TowerDefenseRouteGeometryConfig route;
            float worldPerLogicalPixel;
            Vector2 firstZone, secondZone;
            string error = null;
            if (!TowerDefenseResponsiveLayout.TryCreate(towerDefenseSourceConfig, camera, viewport, W, logicalHeight,
                    out towerDefenseRuntimeConfig, out route, out worldPerLogicalPixel, out firstZone, out secondZone, out error))
            {
                towerDefenseError = error;
                return false;
            }

            try
            {
                if (!game.TryStartTowerDefenseRound())
                {
                    towerDefenseError = "No se pudo iniciar el intento de campaña.";
                    if (towerDefenseRuntimeConfig != null) Destroy(towerDefenseRuntimeConfig);
                    towerDefenseRuntimeConfig = null;
                    return false;
                }
                StreetSimulation campaign = game.Sim;
                towerDefenseMode = new TowerDefenseGameMode(towerDefenseRuntimeConfig, game.Balance, route,
                    worldPerLogicalPixel, firstZone, secondZone, campaign.WorkerCountsByRole,
                    campaign.TicketWorkerCountsByRole, campaign.Coins);
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
                logicalCanvasHeight = logicalHeight;
                layoutVerticalScale = logicalHeight / H;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
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
            if (towerDefenseCoinHud != null) Destroy(towerDefenseCoinHud);
            towerDefenseCoinHud = null;
            towerDefenseSelectedRole = -1;
            if (towerDefensePresentation != null) towerDefensePresentation.DisposePresentation();
            if (towerDefenseMode != null)
            {
                game?.DetachTowerDefenseRuntime(towerDefenseMode);
                towerDefenseMode.Stop();
            }
            towerDefenseMode = null;
            towerDefenseActive = false;
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
            Rect activeViewport = CanvasViewport(screenSize, Screen.safeArea);
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
            if (towerDefensePresentation != null) towerDefensePresentation.Sync(towerDefenseMode);
            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
            {
                if (game.CompleteTowerDefenseRound(towerDefenseMode))
                {
                    EndTowerDefenseRun();
                    hubActive = false; marketActive = false; marketFromSelector = false; levelSelectActive = false;
                }
                else towerDefenseError = "No se pudo guardar el resultado de Camioneros.";
            }
        }

        private void ReflowTowerDefense(Vector2 screenSize)
        {
            Camera camera = Camera.main;
            Rect viewport = CanvasViewport(screenSize, Screen.safeArea);
            string error = null;
            TowerDefenseLevelConfig nextConfig;
            TowerDefenseRouteGeometryConfig route;
            float worldPerLogicalPixel;
            Vector2 firstZone, secondZone;
            if (camera == null || !TowerDefenseResponsiveLayout.TryCreate(towerDefenseSourceConfig, camera, viewport,
                    W, CanvasLogicalHeight(viewport), out nextConfig, out route, out worldPerLogicalPixel,
                    out firstZone, out secondZone, out error))
            {
                towerDefenseError = camera == null ? "No se pudo recalcular la cámara." : error;
                towerDefenseMode.Pause();
                return;
            }

            TowerDefenseLevelConfig previous = towerDefenseRuntimeConfig;
            towerDefenseRuntimeConfig = nextConfig;
            towerDefenseMode.Reflow(route, worldPerLogicalPixel, firstZone, secondZone);
            if (towerDefensePresentation != null)
            {
                towerDefensePresentation.ResizeTarget(screenSize);
                towerDefensePresentation.RebuildRoute(nextConfig, screenSize);
            }
            if (previous != null) Destroy(previous);
            towerDefenseScreenSize = screenSize;
            towerDefenseSafeArea = Screen.safeArea;
            logicalCanvasHeight = CanvasLogicalHeight(viewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            towerDefenseError = null;
        }

        private void DrawTowerDefenseUi(Rect viewport)
        {
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
                    GUI.DrawTexture(new Rect(0f, -TowerDefenseResponsiveLayout.VisualStageLift * viewport.width / W,
                        screenSize.x, screenSize.y), towerDefensePresentation.OverlayTexture,
                        ScaleMode.StretchToFill, true);
                GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y), Quaternion.identity,
                    new Vector3(viewport.width / W, viewport.width / W, 1f));
                GUI.color = Color.white;
                EnsureTowerDefenseStyles();
                DrawTowerDefensePlacementOverlay(viewport);
                if (towerDefenseActive && towerDefenseMode != null)
                    DrawTowerDefenseOrderBubbles(viewport);
                DrawTowerDefenseHud(logicalHeight);
            }
            finally
            {
                layoutVerticalScale = hitTestVerticalScale;
                GUI.matrix = previous;
                GUI.color = oldColor;
            }
        }

        private void DrawTowerDefenseHud(float height)
        {
            if (towerDefenseMode == null) return;
            DrawTowerDefenseCampaignHud();

            float footerTop = height - TowerDefenseResponsiveLayout.FooterReserve;
            DrawFooterBluePanel(LayoutRect(new Rect(0f, footerTop, W, height - footerTop)), layoutVerticalScale);
            if (towerDefenseCrest != null)
                DrawLevelCrest(LayoutRect(new Rect(18f, footerTop + 9f, 34f, 42f)), towerDefenseCrest);
            DrawTowerDefenseLabel(new Rect(58f, footerTop + 2f, W - 76f, 29f), "CAMIONEROS", towerDefenseTitle);
            DrawTowerDefenseLabel(new Rect(58f, footerTop + 29f, W - 76f, 22f), "PLAZA DE MAYO", towerDefenseSmallLabel);

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

        private void DrawTowerDefenseCampaignHud() => DrawTowerDefenseCoinsHud(towerDefenseMode.Coins);

        private void DrawTowerDefenseCoinsHud(int coins)
        {
            Rect cutout = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            if (towerDefenseCoinHud == null || towerDefenseCoinHudGap != cutout)
            {
                if (towerDefenseCoinHud != null) Destroy(towerDefenseCoinHud);
                towerDefenseCoinHud = CreateTowerDefenseCoinHud(cutout);
                towerDefenseCoinHudGap = cutout;
            }
            Rect bar = new Rect(0f, 0f, W, HudBarHeightFor(cutout));
            GUI.DrawTexture(LayoutRect(bar), towerDefenseCoinHud, ScaleMode.StretchToFill, true);
            DrawHudIcon(HudCoinIconBounds(cutout), 10);
            Rect amount = cutout.width > 0f ? HudStatusTextBounds(0, cutout)
                : new Rect(42f, 0f, W - 54f, bar.height);
            DrawHudNumber(amount, coins.ToString(), HudStatusValueFontSize);
            hudQuotaProductCount = 0; // Defense has no traditional product quota HUD.
        }

        // Reuse the campaign's original glossy vector palette, but no clock motif
        // or second status cell is baked into this Defense-only texture.
        private static Texture2D CreateTowerDefenseCoinHud(Rect gap)
        {
            const int width = 1080;
            int height = Mathf.CeilToInt(HudBarHeightFor(gap) * 2f);
            float logicalHeight = height * .5f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Defense coins only HUD", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var shadow = new Rect(0, 5, W, logicalHeight - 5f);
            var rim = new Rect(0, 0, W, logicalHeight);
            var face = new Rect(2, 2, W - 4f, logicalHeight - 4f);
            Rect cell = gap.width > 0f ? HudStatusCellBounds(0, gap)
                : new Rect(3f, 3f, W - 6f, logicalHeight - 6f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * .5f, py = (height - y - .5f) * .5f;
                Color color = new Color(.008f, .025f, .12f, .58f * RoundedCoverage(px, py, shadow, 22));
                color = Over(color, new Color(.012f, .045f, .17f), RoundedCoverage(px, py, rim, 22));
                color = Over(color, Color.Lerp(new Color(.10f, .47f, .90f), new Color(.025f, .14f, .48f),
                    Mathf.Clamp01(py / Mathf.Max(1f, logicalHeight))), RoundedCoverage(px, py, face, 20));
                pixels[y * width + x] = HudCapsule(color, px, py, cell,
                    new Color(.30f, .86f, 1f), new Color(.02f, .22f, .72f));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return texture;
        }

        private void DrawTowerDefenseOrderBubbles(Rect viewport)
        {
            if (towerDefenseMode == null || items == null || viewport.width <= 0f) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            float canvasScale = viewport.width / W;
            float feetOffset = towerDefenseMode.WorldUnitsPerLogicalPixel * 38f;
            IReadOnlyList<TowerDefenseFanState> fans = towerDefenseMode.Fans;
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
                // TD lanes have far more vertical separation than the campaign queue;
                // keep the existing full-size product icons instead of shrinking each
                // two-item order bubble to fit a regular queue row.
                Rect bubble = OrderBubbleBounds(fanFeet, visibleLines, 20f);
                bubble.x = Mathf.Clamp(bubble.x, 6f, W - bubble.width - 6f);
                if (bubble.yMax <= 0f || bubble.yMin >= logicalCanvasHeight) continue;

                DrawOrderBubble(bubble, 1f);
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
            if (backdrop == null || towerDefenseBackdrop == null || canvasHeight <= 0f) return;
            // Draw within the active safe-area logical canvas, like the route and footer.
            // Use existing Camioneros mural art all the way to the route so no unrelated
            // campaign mural can show through behind the HUD.
            Rect themeSource = ClubVisualTheme.ForLevel(StreetSimulation.TowerDefenseCampaignLevelId).MuralSource;
            Rect muralSource = new Rect(themeSource.x * towerDefenseBackdrop.width,
                themeSource.y * towerDefenseBackdrop.height, themeSource.width * towerDefenseBackdrop.width,
                themeSource.height * towerDefenseBackdrop.height);
            Rect mural = new Rect(0f, 0f, W, TowerDefenseResponsiveLayout.RouteTopReserve - 24f
                - TowerDefenseResponsiveLayout.VisualStageLift);
            DrawRawCover(mural, towerDefenseBackdrop, muralSource);

            float sx = backdrop.width / StreetSceneLayout.ReferenceBackdropSize.x;
            float sy = backdrop.height / StreetSceneLayout.ReferenceBackdropSize.y;
            Rect sourceFarSidewalk = new Rect(0f, 150f * sy, backdrop.width, 30f * sy);
            Rect sourceRoad = new Rect(100f * sx, 200f * sy, 740f * sx, 280f * sy);
            Rect sourceNearSidewalk = new Rect(100f * sx, 495f * sy, 740f * sx, 50f * sy);
            float sidewalkTop = mural.yMax;
            const float farSidewalkHeight = 24f, nearSidewalkHeight = 30f, stallSpaceHeight = 90f;
            float footerTop = canvasHeight - TowerDefenseResponsiveLayout.FooterReserve;
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
                string status = selected ? "ELEGÍ ÁREA" : "EQUIPO " + count;
                upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, status, UpgradeStatusBounds(card).width);
                UpgradeOutline(UpgradeStatusBounds(card), status, upgradePercent, new Color(.55f, .90f, 1f));
                upgradePercent.fontSize = statusSize;
                if (role == StreetWorkerRole.Parrillero && parrilleroIcon != null)
                    DrawUpgradeOvalContent(card, parrilleroIcon, new Rect(22, 6, 1269, 1188));
                else if (role == StreetWorkerRole.Cocacolero && CocacoleroArtForLevel(9) != null)
                    DrawUpgradeOvalContent(card, CocacoleroArtForLevel(9), CocacoleroPoseForLevel(9, 0));
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
            Texture2D texture;
            Rect[] poses;
            if (role == StreetWorkerRole.Parrillero)
            {
                texture = parrillero;
                poses = ParrilleroPoses;
            }
            else if (role == StreetWorkerRole.ParrilleroPremium)
            {
                texture = premiumWorker;
                poses = ParrilleroPoses;
            }
            else if (role == StreetWorkerRole.Mozo)
            {
                texture = mozoWorker;
                poses = ParrilleroPoses;
            }
            else if (role == StreetWorkerRole.Cocacolero)
            {
                texture = CocacoleroArtForLevel(9);
                poses = null;
            }
            else
            {
                texture = UsesMatchedCocacoleroArt(9) && fernetWorkerLater != null ? fernetWorkerLater : fernetWorker;
                poses = null;
            }

            if (texture == null) return Array.Empty<Sprite>();
            const int frameCount = 4;
            var frames = new Sprite[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                Rect topOrigin;
                if (role == StreetWorkerRole.Cocacolero) topOrigin = CocacoleroPoseForLevel(9, frame);
                else if (role == StreetWorkerRole.Fernetero) topOrigin = MatchedCocacoleroPose(frame, new Vector2(texture.width, texture.height));
                else topOrigin = poses[frame];
                Rect spriteRect = new Rect(topOrigin.x, texture.height - topOrigin.yMax, topOrigin.width, topOrigin.height);
                frames[frame] = Sprite.Create(texture, spriteRect, new Vector2(.5f, .02f), 100f, 0, SpriteMeshType.Tight);
                frames[frame].name = "TD " + role + " campaign pose " + frame;
                frames[frame].hideFlags = HideFlags.DontSave;
            }
            return frames;
        }
    }
}
