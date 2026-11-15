using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Riot / resultado de partida. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
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
            return game != null && game.HasPendingChulengo && !game.ChulengoChoiceLocked
                ? ChulengoPendingCollectBoundsForFrame(frame, logicalCanvasHeight)
                : ChulengoFinalCollectBoundsForFrame(frame);
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
            string message = game != null && game.Sim != null && StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex)
                ? "Un hincha llegó a la salida sin su pedido." : "Los hinchas se cansaron de esperar......";
            int fontSize = FitStandardButtonFontSize(menuTitle, message, bounds);
            GUIStyle caption = CachedStandardButtonLabelStyle(menuTitle, fontSize, Color.white);
            GUIStyle outline = CachedStandardButtonLabelStyle(menuTitle, fontSize, new Color(.035f, .075f, .16f));
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
    }
}
