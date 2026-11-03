using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const int TowerDefenseCampaignAction = 7801;
        private const int TowerDefenseStartAction = 7802;
        private const int TowerDefenseSelectorHubAction = 7803;
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
        private static readonly string[] TowerDefenseProductLabels = { "CHO", "PAT", "BON", "VAC", "COC", "FER", "CER" };

        private bool towerDefenseSelectorActive;
        private bool towerDefenseActive;
        private string towerDefenseError;
        private TowerDefenseLevelConfig towerDefenseSourceConfig;
        private TowerDefenseLevelConfig towerDefenseRuntimeConfig;
        private TowerDefenseGameMode towerDefenseMode;
        private TowerDefenseWorldPresentation towerDefensePresentation;
        private Texture2D towerDefenseBackdrop;
        private GUIStyle towerDefenseLabel, towerDefenseSmallLabel, towerDefenseTitle;
        private Vector2 towerDefenseScreenSize;
        private Rect towerDefenseSafeArea;

        private bool IsTowerDefenseNavigationActive => towerDefenseSelectorActive || towerDefenseActive;

        private static Vector2 TowerDefenseRenderSize(Camera camera)
        {
            if (camera != null && camera.pixelWidth > 0 && camera.pixelHeight > 0)
                return new Vector2(camera.pixelWidth, camera.pixelHeight);
            return new Vector2(Screen.width, Screen.height);
        }

        private void ResetTowerDefenseUi()
        {
            towerDefenseSelectorActive = false;
            towerDefenseActive = false;
            towerDefenseError = null;
            towerDefenseBackdrop = null;
            towerDefenseLabel = towerDefenseSmallLabel = towerDefenseTitle = null;
            towerDefenseScreenSize = Vector2.zero;
            towerDefenseSafeArea = Rect.zero;
            TowerDefenseGameMode.SetCampaignStepSuspended(false);
        }

        private void DisposeTowerDefenseUi()
        {
            EndTowerDefenseRun(false);
            towerDefenseSelectorActive = false;
            TowerDefenseGameMode.SetCampaignStepSuspended(false);
        }

        private bool HandleTowerDefenseAction(int action)
        {
            if (!towerDefenseSelectorActive && !towerDefenseActive)
            {
                if (action != HubPlayAction || !hubActive || marketActive) return false;
                hubActive = false;
                levelSelectActive = false;
                towerDefenseSelectorActive = true;
                towerDefenseError = null;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
                return true;
            }

            if (towerDefenseSelectorActive)
            {
                if (action == TowerDefenseCampaignAction)
                {
                    towerDefenseSelectorActive = false;
                    levelSelectActive = true;
                    levelSelectScroll = 0f;
                    hubActive = false;
                    TowerDefenseGameMode.SetCampaignStepSuspended(false);
                }
                else if (action == TowerDefenseStartAction)
                {
                    StartTowerDefenseRun();
                }
                else if (action == TowerDefenseSelectorHubAction)
                {
                    towerDefenseSelectorActive = false;
                    hubActive = true;
                    TowerDefenseGameMode.SetCampaignStepSuspended(false);
                }
                return true;
            }

            if (towerDefenseMode == null) return true;
            if (action == TowerDefensePauseAction)
            {
                if (towerDefenseMode.Phase == TowerDefenseRunPhase.Paused) towerDefenseMode.Resume();
                else towerDefenseMode.Pause();
            }
            else if (action == TowerDefenseExitAction)
            {
                EndTowerDefenseRun(true);
            }
            else if (action == TowerDefenseSpeedAction)
            {
                towerDefenseMode.TryUpgradeSpeed();
            }
            else if (action >= TowerDefenseHireFirstAction && action <= TowerDefenseHireLastAction)
            {
                int index = action - TowerDefenseHireFirstAction;
                if (index >= 0 && index < TowerDefenseHireRoles.Length)
                    towerDefenseMode.TryHire(TowerDefenseHireRoles[index]);
            }
            return true;
        }

        private int HitTowerDefenseAction(Vector2 point)
        {
            point.y *= layoutVerticalScale;
            float height = logicalCanvasHeight;
            if (towerDefenseSelectorActive)
            {
                if (TowerDefenseCampaignBounds(height).Contains(point)) return TowerDefenseCampaignAction;
                if (TowerDefenseStartBounds(height).Contains(point)) return TowerDefenseStartAction;
                if (TowerDefenseSelectorHubBounds(height).Contains(point)) return TowerDefenseSelectorHubAction;
                return 0;
            }
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

        private void StartTowerDefenseRun()
        {
            towerDefenseError = null;
            Camera camera = Camera.main;
            if (camera == null)
            {
                towerDefenseError = "No hay una cámara activa para proyectar el recorrido.";
                return;
            }
            towerDefenseSourceConfig = Resources.Load<TowerDefenseLevelConfig>("TowerDefense/Camioneros-PlazaDeMayo");
            if (towerDefenseSourceConfig == null)
            {
                towerDefenseError = "Falta la configuración TowerDefense/Camioneros-PlazaDeMayo.";
                return;
            }
            towerDefenseBackdrop = Resources.Load<Texture2D>(towerDefenseSourceConfig.BackgroundResource);
            if (towerDefenseBackdrop == null) towerDefenseBackdrop = camionerosBackground;
            if (towerDefenseBackdrop == null)
            {
                towerDefenseError = "No se pudo cargar el fondo existente de Camioneros / Plaza de Mayo.";
                return;
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
                return;
            }

            try
            {
                towerDefenseMode = new TowerDefenseGameMode(towerDefenseRuntimeConfig, game.Balance, route,
                    worldPerLogicalPixel, firstZone, secondZone);
                towerDefenseMode.Begin();
                if (towerDefensePresentation == null)
                    towerDefensePresentation = GetComponent<TowerDefenseWorldPresentation>() ?? gameObject.AddComponent<TowerDefenseWorldPresentation>();
                if (!towerDefensePresentation.Configure(this, towerDefenseRuntimeConfig, towerDefenseMode, camera,
                        screenSize))
                    throw new InvalidOperationException("No se pudieron ensamblar las vallas y estaciones del nivel.");
                towerDefenseActive = true;
                towerDefenseSelectorActive = false;
                hubActive = false;
                levelSelectActive = false;
                towerDefenseScreenSize = screenSize;
                towerDefenseSafeArea = Screen.safeArea;
                logicalCanvasHeight = logicalHeight;
                layoutVerticalScale = logicalHeight / H;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
            }
            catch (Exception exception)
            {
                Debug.LogError("Tower Defense could not start: " + exception.Message);
                towerDefenseError = "No se pudo iniciar Tower Defense: " + exception.Message;
                EndTowerDefenseRun(false);
                towerDefenseSelectorActive = true;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
            }
        }

        private void EndTowerDefenseRun(bool returnToSelector)
        {
            if (towerDefensePresentation != null) towerDefensePresentation.DisposePresentation();
            if (towerDefenseMode != null) towerDefenseMode.Stop();
            towerDefenseMode = null;
            towerDefenseActive = false;
            if (towerDefenseRuntimeConfig != null)
            {
                Destroy(towerDefenseRuntimeConfig);
                towerDefenseRuntimeConfig = null;
            }
            if (returnToSelector)
            {
                towerDefenseSelectorActive = true;
                hubActive = false;
                levelSelectActive = false;
                TowerDefenseGameMode.SetCampaignStepSuspended(true);
            }
            else if (!towerDefenseSelectorActive)
            {
                TowerDefenseGameMode.SetCampaignStepSuspended(false);
            }
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
            if (background != null)
                GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), background, ScaleMode.ScaleAndCrop, true);
            else
                GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), Texture2D.blackTexture, ScaleMode.StretchToFill, true);
            if (towerDefenseActive && towerDefensePresentation != null && towerDefensePresentation.OverlayTexture != null)
                GUI.DrawTexture(new Rect(0f, 0f, screenSize.x, screenSize.y), towerDefensePresentation.OverlayTexture,
                    ScaleMode.StretchToFill, true);

            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y), Quaternion.identity,
                new Vector3(viewport.width / W, viewport.width / W, 1f));
            GUI.color = Color.white;
            EnsureTowerDefenseStyles();
            if (towerDefenseActive) DrawTowerDefenseHud(logicalHeight);
            else DrawTowerDefenseModeSelector(logicalHeight);
            GUI.matrix = previous;
            GUI.color = oldColor;
        }

        private void DrawTowerDefenseModeSelector(float height)
        {
            DrawTowerDefensePanel(new Rect(30f, height * .21f, W - 60f, height * .51f), new Color(.018f, .07f, .13f, .84f));
            DrawTowerDefenseLabel(new Rect(48f, height * .24f, W - 96f, 52f), "ELEGÍ TU MODALIDAD", towerDefenseTitle);
            DrawTowerDefenseLabel(new Rect(56f, height * .32f, W - 112f, 70f), "Tu progreso de campaña y Tower Defense se mantienen por separado.", towerDefenseLabel);
            DrawStandardButton(TowerDefenseCampaignBounds(height), TowerDefenseCampaignAction, "CAMPAÑA TRADICIONAL");
            DrawStandardButton(TowerDefenseStartBounds(height), TowerDefenseStartAction, "TOWER DEFENSE · CAMIONEROS");
            DrawStandardButton(TowerDefenseSelectorHubBounds(height), TowerDefenseSelectorHubAction, "VOLVER AL BARRIO");
            if (!string.IsNullOrEmpty(towerDefenseError))
                DrawTowerDefenseLabel(new Rect(38f, height * .76f, W - 76f, 76f), towerDefenseError, towerDefenseSmallLabel);
        }

        private void DrawTowerDefenseHud(float height)
        {
            if (towerDefenseMode == null) return;
            DrawTowerDefensePanel(new Rect(0f, 0f, W, 70f), new Color(.015f, .045f, .085f, .90f));
            DrawTowerDefenseLabel(new Rect(14f, 4f, W - 28f, 27f), "CAMIONEROS · PLAZA DE MAYO", towerDefenseSmallLabel);
            DrawTowerDefenseLabel(new Rect(14f, 38f, 165f, 26f), "MONEDAS  $" + towerDefenseMode.Coins, towerDefenseSmallLabel);
            DrawTowerDefenseLabel(new Rect(188f, 38f, 130f, 26f), "TIEMPO  " + FormatTowerDefenseTime(towerDefenseMode.TimeRemaining), towerDefenseSmallLabel);
            DrawTowerDefenseLabel(new Rect(340f, 38f, 185f, 26f), "HINCHAS  " + towerDefenseMode.ActiveFanCount + "/" + towerDefenseMode.MaximumFans, towerDefenseSmallLabel);

            DrawTowerDefensePanel(new Rect(0f, 70f, W, 74f), new Color(.03f, .09f, .14f, .78f));
            int[] delivered = towerDefenseMode.DeliveredByProduct;
            int[] goals = towerDefenseMode.GoalByProduct;
            float cellWidth = W / 7f;
            for (int i = 0; i < 7; i++)
            {
                float x = i * cellWidth;
                DrawTowerDefenseLabel(new Rect(x, 79f, cellWidth, 22f), TowerDefenseProductLabels[i], towerDefenseSmallLabel);
                DrawTowerDefenseLabel(new Rect(x, 103f, cellWidth, 29f), delivered[i] + "/" + goals[i], towerDefenseSmallLabel);
            }

            float controlsY = height - 155f;
            DrawTowerDefensePanel(new Rect(0f, controlsY - 8f, W, height - controlsY + 8f), new Color(.012f, .048f, .085f, .92f));
            Rect speed = TowerDefenseSpeedBounds(height);
            Rect pause = TowerDefensePauseBounds(height);
            Rect exit = TowerDefenseExitBounds(height);
            bool active = towerDefenseMode.Phase == TowerDefenseRunPhase.Playing;
            bool canUpgrade = active && towerDefenseMode.SpeedUpgradeCost > 0 && towerDefenseMode.Coins >= towerDefenseMode.SpeedUpgradeCost;
            DrawStandardButton(speed, TowerDefenseSpeedAction,
                towerDefenseMode.SpeedUpgradeCost > 0 ? "VELOCIDAD $" + towerDefenseMode.SpeedUpgradeCost : "VELOCIDAD MÁX.", canUpgrade);
            DrawStandardButton(pause, TowerDefensePauseAction,
                towerDefenseMode.Phase == TowerDefenseRunPhase.Paused ? "CONTINUAR" : "PAUSAR", active || towerDefenseMode.Phase == TowerDefenseRunPhase.Paused);
            DrawStandardButton(exit, TowerDefenseExitAction, "SALIR");

            Rect[] hireBounds = TowerDefenseHireBounds(height);
            for (int i = 0; i < TowerDefenseHireRoles.Length; i++)
            {
                StreetWorkerRole role = TowerDefenseHireRoles[i];
                bool available = active && towerDefenseMode.IsRoleAvailable(role);
                int cost = towerDefenseMode.HireCost(role);
                string caption = TowerDefenseHireLabels[i] + (cost > 0 ? " $" + cost : " —");
                DrawStandardButton(hireBounds[i], TowerDefenseHireFirstAction + i, caption,
                    available && cost > 0 && towerDefenseMode.Coins >= cost);
                DrawTowerDefenseLabel(new Rect(hireBounds[i].x, hireBounds[i].y + 40f, hireBounds[i].width, 19f),
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
                DrawTowerDefenseLabel(new Rect(24f, height - 185f, W - 48f, 30f), towerDefenseError, towerDefenseSmallLabel);
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

        private static Rect TowerDefenseCampaignBounds(float height) => new Rect(48f, height * .42f, W - 96f, 78f);
        private static Rect TowerDefenseStartBounds(float height) => new Rect(48f, height * .55f, W - 96f, 78f);
        private static Rect TowerDefenseSelectorHubBounds(float height) => new Rect(90f, height * .68f, W - 180f, 68f);
        private static Rect TowerDefenseSpeedBounds(float height) => new Rect(12f, height - 150f, 200f, 52f);
        private static Rect TowerDefensePauseBounds(float height) => new Rect(220f, height - 150f, 150f, 52f);
        private static Rect TowerDefenseExitBounds(float height) => new Rect(378f, height - 150f, 150f, 52f);

        private static Rect[] TowerDefenseHireBounds(float height)
        {
            const float gap = 4f, margin = 8f;
            float width = (W - margin * 2f - gap * 4f) / 5f;
            var bounds = new Rect[5];
            for (int i = 0; i < bounds.Length; i++) bounds[i] = new Rect(margin + i * (width + gap), height - 86f, width, 60f);
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
