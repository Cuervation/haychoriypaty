using System;
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
        private const int TowerDefenseHireLastAction = TowerDefenseHireFirstAction + 4;
        private static readonly StreetWorkerRole[] TowerDefenseHireRoles =
        {
            StreetWorkerRole.Parrillero, StreetWorkerRole.Cocacolero, StreetWorkerRole.ParrilleroPremium,
            StreetWorkerRole.Fernetero, StreetWorkerRole.Mozo
        };
        private static readonly string[] TowerDefenseHireLabels = { "PARRILLA", "COCA", "PREMIUM", "FERNET", "MOZO" };

        private bool towerDefenseActive;
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
                if (index >= 0 && index < TowerDefenseHireRoles.Length)
                {
                    if (!game.TryHireTowerDefense(towerDefenseMode, TowerDefenseHireRoles[index]))
                        towerDefenseError = "No se pudo guardar la contratación.";
                }
            }
            return true;
        }

        private int HitTowerDefenseAction(Vector2 point)
        {
            point.y *= layoutVerticalScale;
            float height = logicalCanvasHeight;
            if (towerDefenseMode == null) return 0;
            if (TowerDefensePauseBounds(height).Contains(point)) return TowerDefensePauseAction;
            if (TowerDefenseExitBounds(height).Contains(point)) return TowerDefenseExitAction;
            if (towerDefenseMode.Phase != TowerDefenseRunPhase.Playing) return 0;
            if (TowerDefenseSpeedBounds(height).Contains(point) && towerDefenseMode.SpeedUpgradeCost > 0 &&
                towerDefenseMode.Coins >= towerDefenseMode.SpeedUpgradeCost) return TowerDefenseSpeedAction;
            Rect[] roleBounds = TowerDefenseHireBounds(height);
            for (int i = 0; i < roleBounds.Length; i++)
                if (roleBounds[i].Contains(point) && towerDefenseMode.IsRoleAvailable(TowerDefenseHireRoles[i]) &&
                    towerDefenseMode.HireCost(TowerDefenseHireRoles[i]) > 0 &&
                    towerDefenseMode.Coins >= towerDefenseMode.HireCost(TowerDefenseHireRoles[i]))
                    return TowerDefenseHireFirstAction + i;
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
                    towerDefenseError = "No se pudo reservar el intento de campaña y sus trabajadores de tickets.";
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
                    GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), towerDefensePresentation.OverlayTexture,
                        ScaleMode.StretchToFill, true);
                GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y), Quaternion.identity,
                    new Vector3(viewport.width / W, viewport.width / W, 1f));
                GUI.color = Color.white;
                EnsureTowerDefenseStyles();
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

            int[] delivered = towerDefenseMode.DeliveredByProduct;
            int[] goals = towerDefenseMode.GoalByProduct;
            Rect speed = TowerDefenseSpeedBounds(height);
            Rect pause = TowerDefensePauseBounds(height);
            Rect exit = TowerDefenseExitBounds(height);
            bool active = towerDefenseMode.Phase == TowerDefenseRunPhase.Playing;
            bool canUpgrade = active && towerDefenseMode.SpeedUpgradeCost > 0 && towerDefenseMode.Coins >= towerDefenseMode.SpeedUpgradeCost;
            DrawStandardButton(speed, TowerDefenseSpeedAction,
                towerDefenseMode.SpeedUpgradeCost > 0 ? "VELOCIDAD $" + towerDefenseMode.SpeedUpgradeCost : "VELOCIDAD MÁX.", canUpgrade);
            DrawStandardButton(pause, TowerDefensePauseAction,
                towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "CONTINUAR" : "PAUSAR", active || towerDefenseMode.Phase == TowerDefenseRunPhase.Paused);
            DrawStandardButton(exit, TowerDefenseExitAction, "VOLVER A NIVELES");

            Rect[] hireBounds = TowerDefenseHireBounds(height);
            for (int i = 0; i < TowerDefenseHireRoles.Length; i++)
            {
                StreetWorkerRole role = TowerDefenseHireRoles[i];
                bool available = active && towerDefenseMode.IsRoleAvailable(role);
                int cost = towerDefenseMode.HireCost(role);
                string caption = TowerDefenseHireLabels[i] + (cost > 0 ? " $" + cost : " —");
                DrawStandardButton(hireBounds[i], TowerDefenseHireFirstAction + i, caption,
                    available && cost > 0 && towerDefenseMode.Coins >= cost);
                DrawTowerDefenseLabel(new Rect(hireBounds[i].x, hireBounds[i].y + hireBounds[i].height, hireBounds[i].width, 18f),
                    "EQUIPO " + towerDefenseMode.WorkersCount(role), towerDefenseSmallLabel);
            }

            if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused || towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
            {
                DrawTowerDefensePanel(new Rect(65f, height * .43f, W - 130f, 112f), new Color(.015f, .04f, .08f, .92f));
                string status = towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "PARTIDA EN PAUSA"
                    : towerDefenseMode.HasReachedAllGoals ? "¡OBJETIVOS COMPLETADOS!" : "FIN DE LA PARTIDA";
                DrawTowerDefenseLabel(new Rect(78f, height * .45f, W - 156f, 42f), status, towerDefenseTitle);
                if (towerDefenseMode.Phase == TowerDefenseRunPhase.Finished)
                    DrawTowerDefenseLabel(new Rect(78f, height * .50f, W - 156f, 40f),
                        "Completos: " + towerDefenseMode.CompletedOrdersAtExit + " · Incompletos: " + towerDefenseMode.IncompleteOrdersAtExit,
                        towerDefenseSmallLabel);
            }
            if (!string.IsNullOrEmpty(towerDefenseError))
                DrawTowerDefenseLabel(new Rect(24f, height - 189f, W - 48f, 18f), towerDefenseError, towerDefenseSmallLabel);
        }

        private void DrawTowerDefenseCampaignHud()
        {
            Rect cutout = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            EnsureHudBarTexture(cutout);
            Rect bar = new Rect(0f, 0f, W, HudBarHeightFor(cutout));
            Texture2D texture = cutout.width > 0f ? cutoutHudBarTexture : hudBarTexture;
            if (texture != null) GUI.DrawTexture(LayoutRect(bar), texture, ScaleMode.StretchToFill, true);

            DrawHudIcon(HudCoinIconBounds(cutout), 10);
            DrawHudNumber(HudStatusTextBounds(0, cutout), towerDefenseMode.Coins.ToString(), HudStatusValueFontSize);
            DrawHudNumber(HudStatusTextBounds(1, cutout), FormatTowerDefenseTime(towerDefenseMode.TimeRemaining), HudClockFontSize);

            int[] products = towerDefenseRuntimeConfig != null ? towerDefenseRuntimeConfig.AvailableProducts : Array.Empty<int>();
            int[] delivered = towerDefenseMode.DeliveredByProduct;
            int[] goals = towerDefenseMode.GoalByProduct;
            hudQuotaProductCount = Mathf.Min(products.Length, hudQuotaProductIds.Length);
            for (int i = 0; i < hudQuotaProductCount; i++) hudQuotaProductIds[i] = products[i];
            int sharedFontSize = HudQuotaCounterFontSize;
            for (int slot = 0; slot < hudQuotaProductCount; slot++)
            {
                int product = hudQuotaProductIds[slot];
                if (product < 0 || product >= delivered.Length) continue;
                Rect card = HudQuotaCellBounds(slot, hudQuotaProductIds, hudQuotaProductCount, cutout);
                if (card.width <= 0f || card.height <= 0f) continue;
                bool complete = delivered[product] >= goals[product];
                Color previous = GUI.color;
                GUI.color = HudQuotaCardColor(complete);
                if (hudQuotaCardTexture != null) GUI.DrawTexture(LayoutRect(card), hudQuotaCardTexture, ScaleMode.StretchToFill, true);
                else FillRect(LayoutRect(card), HudQuotaCardColor(complete));
                if (hudQuotaCardOutlineTexture != null)
                {
                    GUI.color = new Color(.45f, .88f, 1f, .65f);
                    GUI.DrawTexture(LayoutRect(card), hudQuotaCardOutlineTexture, ScaleMode.StretchToFill, true);
                }
                GUI.color = previous;
                float iconSize = Mathf.Min(HudQuotaIconMaxSize, card.height - HudQuotaCardInset * 2f);
                Rect icon = new Rect(card.x + HudQuotaCardInset, card.center.y - iconSize * .5f, iconSize, iconSize);
                DrawHudIcon(icon, product);
                Rect counter = HudQuotaCounterBounds(card, icon);
                DrawHudNumber(counter, delivered[product] + "/" + goals[product], sharedFontSize, HudQuotaCounterTextAlignment);
            }
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
            Rect mural = new Rect(0f, 0f, W, TowerDefenseResponsiveLayout.RouteTopReserve - 24f);
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

        private static Rect[] TowerDefensePrimaryActionBounds(float height)
        {
            const float margin = 12f, gap = 6f;
            float width = (W - margin * 2f - gap * 2f) / 3f;
            float y = height - 171f;
            return new[] {
                new Rect(margin, y, width, 50f),
                new Rect(margin + width + gap, y, width, 50f),
                new Rect(margin + (width + gap) * 2f, y, width, 50f)
            };
        }

        private static Rect TowerDefenseSpeedBounds(float height) => TowerDefensePrimaryActionBounds(height)[0];
        private static Rect TowerDefensePauseBounds(float height) => TowerDefensePrimaryActionBounds(height)[1];
        private static Rect TowerDefenseExitBounds(float height) => TowerDefensePrimaryActionBounds(height)[2];

        private static Rect[] TowerDefenseHireBounds(float height)
        {
            const float gap = 4f, margin = 8f;
            float width = (W - margin * 2f - gap * 4f) / 5f;
            var bounds = new Rect[5];
            for (int i = 0; i < bounds.Length; i++) bounds[i] = new Rect(margin + i * (width + gap), height - 111f, width, 50f);
            return bounds;
        }

        private static string FormatTowerDefenseTime(float seconds)
        {
            int value = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (value / 60).ToString("00") + ":" + (value % 60).ToString("00");
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
