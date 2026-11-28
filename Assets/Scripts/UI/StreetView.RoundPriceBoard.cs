using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const float RoundPriceBoardSeconds = 3f;
        private const string PriceBoardResource = "Menu/street-price-board-iron-v1";
        private const float PriceBoardWidth = 476f, PriceBoardHeight = PriceBoardWidth * 1.5f;
        private static readonly string[] PriceBoardProductNames = { "CHORI", "PATY", "BONDIOLA", "VACÍO", "COCA", "FERNET", "CERVEZA" };
        private static readonly Color PriceBoardIronTint = new Color(.52f, .52f, .53f);
        private static readonly Color PriceBoardChalk = new Color(1f, .985f, .945f);
        private Action roundStartAfterPrices;
        private StreetSimulation roundPriceBoardSimulation;
        private int[] roundPriceBoardProducts;
        private float roundPriceBoardRemaining;
        private bool roundPriceBoardPaused;
        private string[] roundPriceBoardPrices;
        private Texture2D roundPriceBoardArt, roundPriceBoardDefenseBackdrop;
        private RoundPriceBoardChalk roundPriceBoardChalk;

        private bool RoundPriceBoardActive => roundStartAfterPrices != null;
        private int RoundPriceBoardNumber => RoundPriceBoardActive ? Mathf.Clamp(Mathf.CeilToInt(roundPriceBoardRemaining), 1, 3) : 0;

        private static TowerDefenseLevelConfig PriceBoardDefenseConfig(int level) =>
            StreetSimulation.IsTowerDefenseLevel(level)
                ? Resources.Load<TowerDefenseLevelConfig>(TowerDefenseLevelConfig.ResourcesPathForCampaignLevel(level))
                : null;

        private static int[] RoundPriceBoardProductIds(StreetSimulation sim)
        {
            // AvailableProducts already returns a copy, so sorting never changes TD's catalog.
            int[] products = PriceBoardDefenseConfig(sim.LevelIndex)?.AvailableProducts;
            if (products == null || products.Length == 0)
            {
                products = new int[sim.ProductCount];
                for (int i = 0; i < products.Length; i++) products[i] = sim.GetAvailableProduct(i);
            }
            Array.Sort(products, (a, b) =>
            {
                int order = sim.GetProductPrice(a).CompareTo(sim.GetProductPrice(b));
                return order != 0 ? order : a.CompareTo(b);
            });
            return products;
        }

        private void StartAfterRoundPriceBoard(Action start)
        {
            if (RoundPriceBoardActive || game?.Sim == null) return;
            if (!game.CanStartRound)
            {
                // Let the original start path report stock/guard errors without a fake countdown.
                start();
                return;
            }
            roundPriceBoardSimulation = game.Sim;
            roundPriceBoardProducts = RoundPriceBoardProductIds(game.Sim);
            roundPriceBoardPrices = new string[roundPriceBoardProducts.Length];
            for (int i = 0; i < roundPriceBoardPrices.Length; i++)
                roundPriceBoardPrices[i] = "$" + Mathf.RoundToInt(game.Sim.GetProductPrice(roundPriceBoardProducts[i]));
            roundPriceBoardArt ??= Resources.Load<Texture2D>(PriceBoardResource);
            var defenseConfig = PriceBoardDefenseConfig(game.Sim.LevelIndex);
            roundPriceBoardDefenseBackdrop = defenseConfig != null
                ? Resources.Load<Texture2D>(defenseConfig.BackgroundResource)
                : null;
            roundPriceBoardChalk ??= new RoundPriceBoardChalk();
            roundPriceBoardRemaining = RoundPriceBoardSeconds;
            roundStartAfterPrices = start;
            pressedAction = 0;
            marketPointerHeld = marketDragging = levelSelectPointerHeld = levelSelectDragging = false;
        }

        private void StartTraditionalWithPriceBoard(Action onStarted = null)
        {
            StartAfterRoundPriceBoard(() =>
            {
                if (game.TryStartRound()) onStarted?.Invoke();
            });
        }

        private void OnApplicationPause(bool paused) => roundPriceBoardPaused = paused;

        private void ClearRoundPriceBoard()
        {
            roundStartAfterPrices = null;
            roundPriceBoardSimulation = null;
            roundPriceBoardProducts = null;
            roundPriceBoardPrices = null;
            roundPriceBoardDefenseBackdrop = null;
            roundPriceBoardRemaining = 0f;
        }

        private void AdvanceRoundPriceBoard(float delta)
        {
            if (!RoundPriceBoardActive) return;
            if (game == null || !game.IsPlayerActive || !ReferenceEquals(game.Sim, roundPriceBoardSimulation) ||
                game.Sim.Phase != RoundPhase.Ready || (Cloud.FirebasePlayerSession.Instance?.BlocksGameInput ?? false))
            {
                ClearRoundPriceBoard();
                return;
            }
            if (RewardShutterActive || roundPriceBoardPaused) return;
            roundPriceBoardRemaining -= Mathf.Max(0f, delta);
            if (roundPriceBoardRemaining > 0f) return;
            Action start = roundStartAfterPrices;
            ClearRoundPriceBoard(); // Revalidate and run the existing start path exactly once.
            start();
        }

        private static Rect RoundPriceBoardBounds(float canvasHeight)
        {
            // A real board has fixed proportions; shorter menus don't change the metalwork.
            float scale = Mathf.Clamp01((canvasHeight - 24f) / PriceBoardHeight);
            float boardHeight = PriceBoardHeight * scale;
            return new Rect((W - PriceBoardWidth * scale) * .5f, (canvasHeight - boardHeight) * .55f,
                PriceBoardWidth * scale, boardHeight);
        }

        private void DrawRoundPriceBoard()
        {
            if (!RoundPriceBoardActive || Event.current.type != EventType.Repaint) return;
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            if (viewport.width <= 0f || Screen.width <= 0) return;
            Rect board = RoundPriceBoardBounds(CanvasLogicalHeight(viewport));
            if (board.width <= 0f) return;
            float canvasScale = viewport.width / W;
            float scale = board.width / PriceBoardWidth * canvasScale;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;

            try
            {
                if (roundPriceBoardDefenseBackdrop != null)
                {
                    // Same TD mural art and street layers as the active round, but no HUD or mode initialization.
                    float screenScale = Screen.width / W;
                    float fullHeight = Screen.height / screenScale;
                    GUI.matrix = Matrix4x4.Scale(new Vector3(screenScale, screenScale, 1));
                    GUI.color = Color.white;
                    DrawTowerDefenseSceneLayers(fullHeight, roundPriceBoardDefenseBackdrop,
                        roundPriceBoardSimulation.LevelIndex, fullHeight);
                }
                else DrawGameplayScreenFill(sceneryOnly: true);

                GUI.matrix = Matrix4x4.identity;
                GUI.color = new Color(.05f, .05f, .06f, .13f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.matrix = Matrix4x4.TRS(viewport.position + board.position * canvasScale, Quaternion.identity,
                    new Vector3(scale, scale, 1f));
                GUI.color = Color.white;
                roundPriceBoardChalk.DrawShadow(new Rect(28f, 660f, 423f, 64f));
                roundPriceBoardChalk.DrawShadow(new Rect(66f, 691f, 49f, 25f));
                roundPriceBoardChalk.DrawShadow(new Rect(381f, 691f, 49f, 25f));
                GUI.color = PriceBoardIronTint;
                GUI.DrawTexture(new Rect(0f, 0f, PriceBoardWidth, PriceBoardHeight),
                    roundPriceBoardArt != null ? roundPriceBoardArt : Texture2D.blackTexture);

                const float left = 111f, right = 379f, middle = (left + right) * .5f;
                GUI.color = PriceBoardChalk;
                roundPriceBoardChalk.Write("PRECIOS", middle, 56f, 56f, TextAnchor.MiddleCenter);
                roundPriceBoardChalk.Rule(new Rect(left + 17f, 119f, right - left - 34f, 8f));

                float rowHeight = Mathf.Min(60f, 298f / Mathf.Max(1, roundPriceBoardProducts.Length));
                float firstY = 150f + (298f - roundPriceBoardProducts.Length * rowHeight) * .4f;
                for (int i = 0; i < roundPriceBoardProducts.Length; i++)
                {
                    float y = firstY + i * rowHeight;
                    roundPriceBoardChalk.Write(PriceBoardProductNames[roundPriceBoardProducts[i]], left, y, 34f, TextAnchor.MiddleLeft);
                    roundPriceBoardChalk.Write(roundPriceBoardPrices[i], right, y, 34f, TextAnchor.MiddleRight);
                }
                roundPriceBoardChalk.Rule(new Rect(left + 11f, 460f, right - left - 22f, 8f));
                roundPriceBoardChalk.Write(RoundPriceBoardNumber.ToString(), middle, 482f, 99f, TextAnchor.MiddleCenter);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.matrix = previousMatrix;
            }
        }
    }
}
