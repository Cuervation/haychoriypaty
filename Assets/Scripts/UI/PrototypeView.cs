using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    /// <summary>Temporary one-scene IMGUI view. Replace this class/art without changing the simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeView : MonoBehaviour
    {
        private const float CanvasWidth = 540f;
        private const float CanvasHeight = 960f;
        private static readonly Rect StartButton = new Rect(91, 567, 358, 61);
        private static readonly Rect ReplayButton = new Rect(97, 557, 346, 57);
        private static readonly Rect HireButton = new Rect(22, 847, 241, 84);
        private static readonly Rect UpgradeButton = new Rect(277, 847, 241, 84);
        private int pressedAction;
        private GameController game;
        private Texture2D background;
        private Texture2D atlas;
        private readonly Vector2[] sellerPositions = { new Vector2(170, 450), new Vector2(300, 450) };
        private int lastServed;
        private float serveUntil;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle buttonSmallStyle;
        private readonly Dictionary<string, GUIStyle> styleCache = new Dictionary<string, GUIStyle>();

        private void Awake()
        {
            game = GetComponent<GameController>();
            if (game == null) game = gameObject.AddComponent<GameController>();
        }

        private void OnEnable()
        {
            // Enter Play Mode without domain reload must not reuse invalid native GUIStyle handles.
            background = Resources.Load<Texture2D>("floresta-background-v2");
            atlas = Resources.Load<Texture2D>("floresta-atlas-v2");
            game = GetComponent<GameController>();
            titleStyle = null;
            sectionStyle = null;
            bodyStyle = null;
            smallStyle = null;
            buttonStyle = null;
            buttonSmallStyle = null;
            styleCache.Clear();
            sellerPositions[0] = new Vector2(170, 450);
            sellerPositions[1] = new Vector2(300, 450);
            lastServed = 0;
            serveUntil = 0;
            pressedAction = 0;
        }

        private void Update()
        {
            if (game == null) return;
            PollPointer();
            if (game.Served > lastServed) serveUntil = Time.time + 0.85f;
            lastServed = game.Served;
            for (int i = 0; i < game.StaffCount; i++)
                sellerPositions[i] = Vector2.MoveTowards(sellerPositions[i], SellerTarget(i),
                    Time.deltaTime * 115f * game.WorkRate);
        }

        private void PollPointer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // GameActivity supplies native IMGUI touch events; it owns Android actions.
            return;
#elif ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Input-System-only projects do not deliver input to runtime OnGUI.
            // IMGUI remains the replaceable renderer; process the same button bounds here.
            var touch = Touchscreen.current;
            var mouse = Mouse.current;
            bool touchDown = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
            bool touchUp = touch != null && touch.primaryTouch.press.wasReleasedThisFrame;
            if (touchDown || touchUp)
            {
                HandlePointer(touch.primaryTouch.position.ReadValue(), touchDown, touchUp);
            }
            else if (mouse != null)
            {
                HandlePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame,
                    mouse.leftButton.wasReleasedThisFrame);
            }
#endif
        }

        private void HandlePointer(Vector2 pixels, bool down, bool up)
        {
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float scale = viewport.width / CanvasWidth;
            if (scale <= 0) return;
            Vector2 offset = viewport.position;
            Vector2 point = (new Vector2(pixels.x, Screen.height - pixels.y) - offset) / scale;
            int action = HitAction(point);
            if (down) pressedAction = action;
            if (!up) return;
            if (pressedAction != 0 && pressedAction == action)
            {
                DispatchAction(action);
            }
            pressedAction = 0;
        }

        private void DispatchAction(int action)
        {
            switch (action)
            {
                case 1: game.StartRound(); break;
                case 2: game.TryHire(); break;
                case 3: game.TryUpgradeSpeed(); break;
            }
        }

        private void HandleGuiAction(int action)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Verified native Android IMGUI path; never also poll its touch device.
            DispatchAction(action);
#elif ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // The desktop/editor bridge owns actions when legacy IMGUI input is absent.
            return;
#else
            DispatchAction(action);
#endif
        }

        private int HitAction(Vector2 point)
        {
            if (game.Phase == RoundPhase.Ready) return StartButton.Contains(point) ? 1 : 0;
            if (game.Phase == RoundPhase.Won || game.Phase == RoundPhase.Lost)
                return ReplayButton.Contains(point) ? 1 : 0;
            if (HireButton.Contains(point)) return 2;
            return UpgradeButton.Contains(point) ? 3 : 0;
        }

        private Vector2 SellerTarget(int index)
        {
            if (game.Phase != RoundPhase.Playing) return new Vector2(170 + index * 125, 450);
            if (Time.time < serveUntil) return new Vector2(210 + index * 95, 330);
            if (index == 1) return new Vector2(330, 415);
            if (game.CookingCount == 0) return new Vector2(205, 450);
            return new Vector2(135, 475);
        }

        // Pixel bounds were reviewed against the original 1402x1122 generated atlas.
        // It is NOT an exact grid: explicit rects avoid sampling neighboring drawings.
        private static readonly Rect[] SpriteBounds =
        {
            new Rect(54, 42, 197, 283), new Rect(327, 42, 197, 283),
            new Rect(588, 42, 216, 283), new Rect(858, 42, 256, 283), new Rect(1177, 42, 223, 283),
            new Rect(38, 340, 219, 277), new Rect(302, 340, 278, 277),
            new Rect(588, 347, 229, 272), new Rect(869, 347, 236, 272), new Rect(1141, 347, 250, 272),
            new Rect(31, 629, 222, 250), new Rect(270, 630, 287, 226),
            new Rect(576, 630, 254, 231), new Rect(852, 644, 289, 213), new Rect(1155, 663, 239, 186),
            new Rect(60, 897, 159, 195), new Rect(330, 900, 180, 181), new Rect(578, 912, 268, 167),
            new Rect(889, 865, 194, 235), new Rect(1147, 871, 250, 229)
        };

        private void DrawSprite(Rect rect, int cell)
        {
            if (atlas == null || cell < 0 || cell >= SpriteBounds.Length) return;
            Rect source = SpriteBounds[cell];
            float scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
            Rect fitted = new Rect(rect.center.x - source.width * scale * 0.5f,
                rect.yMax - source.height * scale, source.width * scale, source.height * scale);
            GUI.DrawTextureWithTexCoords(fitted, atlas, new Rect(source.x / 1402f,
                1f - source.yMax / 1122f, source.width / 1402f, source.height / 1122f), true);
        }

        // Screen.safeArea uses bottom-left pixels; GUI uses top-left pixels.
        // One transform keeps Android drawing and touch hit testing aligned.
        private static Rect CanvasViewport(Vector2 screenSize, Rect safeArea)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0) return new Rect();
            float left = Mathf.Clamp(safeArea.xMin, 0, screenSize.x);
            float right = Mathf.Clamp(safeArea.xMax, 0, screenSize.x);
            float bottom = Mathf.Clamp(safeArea.yMin, 0, screenSize.y);
            float top = Mathf.Clamp(safeArea.yMax, 0, screenSize.y);
            if (right <= left || top <= bottom)
            {
                left = bottom = 0; right = screenSize.x; top = screenSize.y;
            }
            float scale = Mathf.Min((right - left) / CanvasWidth, (top - bottom) / CanvasHeight);
            return new Rect(left + (right - left - CanvasWidth * scale) * 0.5f,
                screenSize.y - top + (top - bottom - CanvasHeight * scale) * 0.5f,
                CanvasWidth * scale, CanvasHeight * scale);
        }

        private void OnGUI()
        {
            if (game == null) return;

            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float scale = viewport.width / CanvasWidth;
            if (scale <= 0) return;
            float offsetX = viewport.x;
            float offsetY = viewport.y;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            EnsureStyles();
            GUI.color = Color.white;

            DrawBackdrop();
            DrawHeader();
            DrawScoreStrip();
            DrawCrowdMood();
            DrawQueue();
            DrawAnimatedSeller();
            DrawGrill();
            DrawCondiments();
            if (game.Phase == RoundPhase.Playing) DrawActions();
            DrawFooter();

            if (game.Phase == RoundPhase.Ready) DrawStartCard();
            else if (game.Phase == RoundPhase.Won || game.Phase == RoundPhase.Lost) DrawResults();

            GUI.color = Color.white;
            GUI.matrix = oldMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = MakeStyle(28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            sectionStyle = MakeStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.25f, 0.12f, 0.08f));
            bodyStyle = MakeStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.18f, 0.12f, 0.09f));
            smallStyle = MakeStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.25f, 0.18f, 0.14f));
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            buttonSmallStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
            buttonSmallStyle.normal.textColor = Color.white;
            buttonSmallStyle.hover.textColor = Color.white;
            buttonSmallStyle.active.textColor = Color.white;
        }

        private GUIStyle MakeStyle(int size, FontStyle weight, TextAnchor align, Color color)
        {
            string key = size + "|" + weight + "|" + align + "|" + ColorUtility.ToHtmlStringRGBA(color);
            GUIStyle cached;
            if (styleCache.TryGetValue(key, out cached)) return cached;
            cached = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                alignment = align,
                wordWrap = true,
                normal = { textColor = color }
            };
            styleCache.Add(key, cached);
            return cached;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawCard(Rect rect, Color color)
        {
            DrawRect(rect, new Color(0.12f, 0.07f, 0.05f, 0.15f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height - 3f), color);
        }

        private void Label(Rect rect, string text, GUIStyle style)
        {
            GUI.Label(rect, text, style);
        }

        private void DrawBackdrop()
        {
            if (background != null)
                GUI.DrawTexture(new Rect(0, 0, CanvasWidth, 846), background, ScaleMode.StretchToFill, false);
            else
            {
                DrawRect(new Rect(0, 0, CanvasWidth, 846), new Color(0.40f, 0.70f, 0.90f));
                DrawRect(new Rect(0, 260, CanvasWidth, 586), new Color(0.82f, 0.68f, 0.47f));
            }
            DrawRect(new Rect(0, 838, CanvasWidth, 122), new Color(0.94f, 0.91f, 0.98f));
        }

        private void DrawHeader()
        {
            DrawCard(new Rect(12, 9, 516, 44), new Color(0.42f, 0.11f, 0.09f, 0.94f));
            Label(new Rect(25, 12, 330, 26), "HAY CHORI Y PATY", titleStyle);
            Label(new Rect(26, 34, 355, 15), "FLORESTA · FRENTE A ALL BOYS", MakeStyle(10, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.63f)));
            Label(new Rect(416, 15, 96, 28), "TURNO 1", MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleRight, Color.white));
        }

        private void DrawScoreStrip()
        {
            DrawCard(new Rect(12, 58, 516, 39), new Color(1f, 0.96f, 0.85f, 0.96f));
            Label(new Rect(22, 63, 125, 22), "$ " + game.Coins + " MON.", MakeStyle(14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.48f, 0.24f, 0.08f)));
            Label(new Rect(178, 63, 116, 22), "TIEMPO " + FormatTime(game.TimeRemaining), MakeStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.25f, 0.19f, 0.16f)));
            Label(new Rect(330, 63, 185, 22), "VENTAS  " + game.Served + "/" + game.Balance.customersToWin, MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.25f, 0.19f, 0.16f)));
        }

        private void DrawCrowdMood()
        {
            DrawCard(new Rect(15, 101, 510, 25), new Color(1f, 0.94f, 0.79f, 0.94f));
            Label(new Rect(23, 103, 102, 19), "HINCHADA", MakeStyle(11, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.38f, 0.17f, 0.11f)));
            float fraction = Mathf.Clamp01(game.CrowdPatience / 100f);
            DrawRect(new Rect(127, 109, 360, 9), new Color(0.34f, 0.24f, 0.20f));
            DrawRect(new Rect(127, 109, 360 * fraction, 9), Color.Lerp(new Color(0.83f, 0.20f, 0.14f), new Color(0.20f, 0.62f, 0.30f), fraction));
            Label(new Rect(488, 103, 30, 18), Mathf.CeilToInt(game.CrowdPatience) + "%", MakeStyle(10, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.38f, 0.17f, 0.11f)));
        }

        private void DrawQueue()
        {
            int shown = Mathf.Min(game.Guests.Count, 12);
            if (shown == 0)
            {
                DrawCard(new Rect(170, 140, 200, 24), new Color(1f, 0.95f, 0.83f, 0.93f));
                Label(new Rect(173, 140, 194, 22), "¡SE ACERCA LA HINCHADA!",
                    MakeStyle(11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.36f, 0.18f, 0.11f)));
                return;
            }
            for (int i = shown - 1; i >= 0; i--)
            {
                CustomerOrder guest = game.Guests[i];
                float x = 5 + (i % 6) * 89;
                float y = 137 + (i / 6) * 143;
                float patience = game.PatienceFraction(guest);
                int fan = 7 + (guest.Id % 4);
                if (patience < 0.28f) fan = 18;
                bool seasoning = guest.HasFood && guest.State == GuestState.Seasoning;
                GUI.color = seasoning ? new Color(1, 1, 1, 0.45f) : Color.white;
                DrawSprite(new Rect(x + 5, y + 43, 77, 96), fan);
                GUI.color = Color.white;
                DrawCard(new Rect(x, y, 86, 32), new Color(1f, 0.98f, 0.91f, 0.98f));
                if (guest.HasFood) DrawSprite(new Rect(x + 4, y + 1, 31, 27), 14);
                if (guest.Kind != OrderKind.Chori) DrawSprite(new Rect(x + 31, y + 1, 26, 28), 15);
                string label = guest.Kind == OrderKind.CocaOnly ? "COCA" : "CHORI";
                Label(new Rect(x + 56, y + 1, 29, 27), label,
                    MakeStyle(8, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.20f, 0.16f, 0.13f)));
                DrawRect(new Rect(x + 4, y + 35, 78, 7), new Color(0.28f, 0.20f, 0.17f));
                DrawRect(new Rect(x + 5, y + 36, 76 * patience, 5), Color.Lerp(
                    new Color(0.83f, 0.18f, 0.12f), new Color(0.21f, 0.62f, 0.28f), patience));
                if (seasoning) DrawSprite(new Rect(439, 469, 62, 80), fan);
            }
        }

        private void DrawAnimatedSeller()
        {
            for (int i = 0; i < game.StaffCount; i++)
            {
                Vector2 target = SellerTarget(i);
                bool moving = Vector2.Distance(sellerPositions[i], target) > 3f;
                int frame = (int)(Time.time * 7f) % 2;
                int cell = 0;
                string action = "LISTO";
                if (game.Phase == RoundPhase.Playing)
                {
                    if (Time.time < serveUntil) { cell = 5 + frame; action = "ENTREGANDO"; }
                    else if (moving) { cell = 1 + frame; action = "EN CAMINO"; }
                    else if (game.CookingCount > 0 && i == 0) { cell = 3 + frame; action = "COCINANDO"; }
                    else { cell = 3 + frame; action = "PREPARANDO"; }
                }
                Vector2 position = sellerPositions[i];
                GUI.color = i == 1 ? new Color(0.87f, 0.94f, 1f) : Color.white;
                DrawSprite(new Rect(position.x - 15, position.y, 116, 108), cell);
                GUI.color = Color.white;
                DrawCard(new Rect(position.x - 2, position.y + 106, 92, 18), new Color(1, 0.96f, 0.84f, 0.93f));
                Label(new Rect(position.x, position.y + 106, 88, 18), action,
                    MakeStyle(9, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.35f, 0.18f, 0.12f)));
            }
        }

        private void DrawGrill()
        {
            DrawSprite(new Rect(104, 655, 252, 172), 11);
            DrawSprite(new Rect(6, 679, 120, 136), 12);
            DrawSprite(new Rect(312, 699, 87, 69), 14);
            DrawSprite(new Rect(20, 655, 35, 41), 15);
            DrawCard(new Rect(17, 600, 292, 55), new Color(0.23f, 0.15f, 0.12f, 0.91f));
            Label(new Rect(27, 604, 270, 18), "PARRILLA CALLEJERA", MakeStyle(12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.90f, 0.67f)));
            if (game.CookingOrder == null)
            {
                Label(new Rect(28, 622, 270, 22), "Brasas listas · esperando chori", MakeStyle(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
                DrawRect(new Rect(29, 645, 220, 6), new Color(0.91f, 0.42f, 0.12f));
            }
            else
            {
                float progress = 1f - Mathf.Clamp01(game.CookingOrder.WorkRemaining / Mathf.Max(0.5f, game.Balance.choriCookSeconds));
                Label(new Rect(28, 622, 270, 20), "Chori al fuego · " + game.CookingCount + " en cocción", MakeStyle(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
                DrawRect(new Rect(29, 645, 220, 7), new Color(0.37f, 0.27f, 0.22f));
                DrawRect(new Rect(29, 645, 220 * progress, 7), new Color(1f, 0.49f, 0.12f));
            }
        }

        private void DrawCondiments()
        {
            DrawSprite(new Rect(363, 552, 169, 129), 13);
            DrawCard(new Rect(347, 681, 179, 52), new Color(0.94f, 0.93f, 0.71f, 0.91f));
            Label(new Rect(354, 685, 165, 20), "MESA DE CONDIMENTOS", MakeStyle(10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.27f, 0.35f, 0.14f)));
            Label(new Rect(354, 705, 165, 23), "Auto · chimi · criolla · cebolla", MakeStyle(8, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.30f, 0.25f, 0.16f)));
        }

        private void DrawActions()
        {
            bool canHire = game.StaffCount < 2 && game.Coins >= game.Balance.hireCost;
            bool canUpgrade = game.SpeedUpgrades < game.Balance.maxSpeedUpgrades && game.Coins >= game.Balance.speedUpgradeCost;
            DrawCard(new Rect(12, 839, 516, 108), new Color(0.98f, 0.95f, 1f, 0.99f));

            Rect hire = HireButton;
            DrawRect(new Rect(hire.x, hire.y + 5, hire.width, hire.height), new Color(0.03f, 0.28f, 0.64f));
            DrawRect(hire, canHire ? new Color(0.10f, 0.61f, 0.91f) : new Color(0.48f, 0.53f, 0.57f));
            DrawRect(new Rect(hire.x + 8, hire.y + 7, hire.width - 16, 43), new Color(1f, 1f, 1f));
            DrawSprite(new Rect(hire.x + 10, hire.y + 8, 34, 41), 0);
            Label(new Rect(hire.x + 47, hire.y + 10, hire.width - 58, 35), "CONTRATAR VENDEDOR", MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.48f, 0.78f)));
            Label(new Rect(hire.x + 8, hire.y + 51, hire.width - 16, 27),
                game.StaffCount < 2 ? "$ " + game.Balance.hireCost + " MONEDAS" : "EQUIPO COMPLETO",
                MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.enabled = canHire;
            if (GUI.Button(hire, GUIContent.none, GUIStyle.none)) HandleGuiAction(2);
            GUI.enabled = true;

            Rect upgrade = UpgradeButton;
            DrawRect(new Rect(upgrade.x, upgrade.y + 5, upgrade.width, upgrade.height), new Color(0.03f, 0.28f, 0.64f));
            DrawRect(upgrade, canUpgrade ? new Color(0.10f, 0.61f, 0.91f) : new Color(0.48f, 0.53f, 0.57f));
            DrawRect(new Rect(upgrade.x + 8, upgrade.y + 7, upgrade.width - 16, 43), new Color(1f, 1f, 1f));
            DrawSprite(new Rect(upgrade.x + 10, upgrade.y + 8, 37, 41), 17);
            Label(new Rect(upgrade.x + 50, upgrade.y + 10, upgrade.width - 61, 35), "MEJORAR VELOCIDAD", MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.48f, 0.78f)));
            Label(new Rect(upgrade.x + 8, upgrade.y + 51, upgrade.width - 16, 27),
                game.SpeedUpgrades < game.Balance.maxSpeedUpgrades ? "$ " + game.Balance.speedUpgradeCost + " MONEDAS" : "VELOCIDAD MÁXIMA",
                MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.enabled = canUpgrade;
            if (GUI.Button(upgrade, GUIContent.none, GUIStyle.none)) HandleGuiAction(3);
            GUI.enabled = true;
        }

        private void DrawFooter()
        {
            Label(new Rect(17, 934, 507, 17), "FLORESTA · ILUSTRACIÓN ORIGINAL PROVISIONAL", MakeStyle(9, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.38f, 0.31f, 0.40f)));
        }

        private void DrawStartCard()
        {
            DrawRect(new Rect(0, 0, CanvasWidth, CanvasHeight), new Color(0.08f, 0.07f, 0.06f, 0.78f));
            DrawCard(new Rect(35, 301, 470, 353), new Color(0.97f, 0.88f, 0.72f));
            Label(new Rect(63, 326, 414, 85), "¡HAY CHORI\nY PATY!", MakeStyle(35, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.47f, 0.12f, 0.09f)));
            Label(new Rect(68, 418, 404, 74), "La previa ya empezó.\nAtendé la parrilla y cuidá la paciencia.", MakeStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.26f, 0.20f, 0.15f)));
            Label(new Rect(76, 496, 388, 57), "El primer vendedor cocina y entrega automáticamente.\nContratá ayuda con las monedas de cada venta.", smallStyle);
            GUI.color = new Color(0.73f, 0.20f, 0.11f);
            if (GUI.Button(StartButton, "EMPEZAR TURNO EN FLORESTA", buttonStyle)) HandleGuiAction(1);
            GUI.color = Color.white;
        }

        private void DrawResults()
        {
            DrawRect(new Rect(0, 0, CanvasWidth, CanvasHeight), new Color(0.08f, 0.07f, 0.06f, 0.78f));
            DrawCard(new Rect(42, 331, 456, 300), new Color(0.97f, 0.88f, 0.72f));
            bool won = game.Phase == RoundPhase.Won;
            if (won)
            {
                Label(new Rect(63, 357, 415, 70), "¡TURNO CUMPLIDO!", MakeStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.25f, 0.48f, 0.24f)));
                Label(new Rect(70, 441, 400, 62), "Pedidos entregados: " + game.Served + "\nHinchas que se fueron: " + game.Departed, MakeStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.24f, 0.18f, 0.13f)));
                Label(new Rect(70, 510, 400, 37), "La parrilla de Floresta fue un éxito.", MakeStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.34f, 0.26f, 0.20f)));
            }
            else
            {
                Label(new Rect(63, 357, 415, 58), "¡LA HINCHADA VOLTEÓ EL PUESTO!", MakeStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.62f, 0.14f, 0.11f)));
                DrawCartoonRiot();
                Label(new Rect(70, 486, 400, 59), "Pedidos entregados: " + game.Served + "\nHinchas que se fueron: " + game.Departed, MakeStyle(17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.24f, 0.18f, 0.13f)));
            }
            GUI.color = new Color(0.73f, 0.20f, 0.11f);
            if (GUI.Button(ReplayButton, "VOLVER A JUGAR", buttonStyle)) HandleGuiAction(1);
            GUI.color = Color.white;
        }

        private void DrawCartoonRiot()
        {
            // Minimal provisional caricature: bouncing supporters tumble the counter on defeat.
            float beat = Time.unscaledTime * 11f;
            DrawRect(new Rect(103, 458, 332, 7), new Color(0.38f, 0.18f, 0.11f));
            for (int i = 0; i < 3; i++)
            {
                float bob = Mathf.Sin(beat + i * 1.8f) * 5f;
                float x = 137f + i * 106f;
                DrawSprite(new Rect(x - 11, 403 + bob, 56, 62), 18);
            }
            Label(new Rect(88, 454, 364, 23), "¡PUM!  ¡CRASH!  —  caricatura provisoria", MakeStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.74f, 0.25f, 0.09f)));
        }

        private static string GuestStateName(CustomerOrder guest)
        {
            if (!guest.HasFood && guest.State == GuestState.Seasoning) return "ARMANDO LA COCA";
            switch (guest.State)
            {
                case GuestState.Cooking: return "A LA PARRILLA";
                case GuestState.Seasoning: return "EN CONDIMENTOS";
                case GuestState.Ready: return "LISTO PARA ENTREGAR";
                default: return "ESPERANDO";
            }
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.CeilToInt(seconds);
            return (value / 60).ToString("00") + ":" + (value % 60).ToString("00");
        }
    }
}
