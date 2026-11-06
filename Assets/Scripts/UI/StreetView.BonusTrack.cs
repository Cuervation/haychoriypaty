using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private long bonusIntroAttempt = -1;
        private float bonusIntroUntil;
        private const int BonusCardFirstAction = 5000;
        private const int BonusAguanteAction = 5010;
        private const int BonusWorkerFirstAction = 5020;
        private Rect BonusAguanteBounds => new Rect(W - 166f, 76f, 150f, 42f);

        private int HitBonusTrackAction(Vector2 point)
        {
            if (game == null || game.Sim == null || !game.IsBonusTrack) return 0;
            float y = point.y * layoutVerticalScale;
            StreetBonusTrackState state = game.Sim.BonusTrack;
            if (game.Sim.Phase == RoundPhase.Playing && state != null)
            {
                if (state.SelectionPending)
                {
                    if (state.EmergencyRoleSelectionPending)
                    {
                        Rect[] choices = CurrentResponsiveFooter(game.Sim, logicalCanvasHeight).BonusChoiceBounds;
                        for (int i = 0; i < state.EmergencyRoles.Count && i < choices.Length; i++)
                            if (choices[i].Contains(new Vector2(point.x, y)))
                                return BonusWorkerFirstAction + i;
                    }
                    else
                    {
                        Rect[] buttons = CurrentResponsiveFooter(game.Sim, logicalCanvasHeight).BonusChoiceBounds;
                        for (int i = 0; i < state.CurrentCards.Count && i < buttons.Length; i++)
                            if (buttons[i].Contains(new Vector2(point.x, y))) return BonusCardFirstAction + i;
                    }
                }
                else if (state.AguanteReady && BonusAguanteBounds.Contains(new Vector2(point.x, y))) return BonusAguanteAction;
            }
            else if (game.Sim.Phase == RoundPhase.Won || game.Sim.Phase == RoundPhase.Lost)
            {
                Vector2 resultPoint = new Vector2(point.x, y);
                if (BonusResultLevelsBounds(logicalCanvasHeight).Contains(resultPoint)) return ResultLevelsAction;
                if (game.CanAdvanceAfterResult && BonusResultNextBounds(logicalCanvasHeight).Contains(resultPoint))
                    return VictoryExitAction;
            }
            return 0;
        }

        private void DrawBonusTrackHud(StreetSimulation sim)
        {
            StreetBonusTrackState state = sim != null ? sim.BonusTrack : null;
            if (state == null) return;
            DrawMarketBluePanel(new Rect(0f, 0f, W, 64f), new Color(.015f, .08f, .28f, .97f));
            MarketText(new Rect(12f, 4f, 245f, 28f), "BONUS TRACK · HURACÁN", 20, new Color(1f, .83f, .25f), TextAnchor.MiddleLeft);
            MarketText(new Rect(262f, 3f, 112f, 28f), "PARQUE PATRICIOS", 12, Color.white, TextAnchor.MiddleCenter);
            MarketText(new Rect(380f, 2f, 150f, 31f), FormatRemainingTime(sim.TimeRemaining), 24, Color.white, TextAnchor.MiddleRight);
            MarketText(new Rect(12f, 34f, 170f, 24f), "PUNTOS " + state.Score.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")), 17, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(185f, 34f, 132f, 24f), "★ " + state.Stars + " / 3", 18, new Color(1f, .86f, .31f));
            MarketText(new Rect(317f, 34f, 120f, 24f), "MEJORAS " + state.ChoiceCount + " / 5", 15, Color.white);
            float nextAt = state.ChoiceCount >= StreetBonusTrackState.MaximumChoices
                ? StreetBonusTrackState.DurationSeconds
                : Mathf.Min(StreetBonusTrackState.DurationSeconds, (state.ChoiceCount + 1) * 20f);
            float progress = state.SelectionPending ? 1f : Mathf.Clamp01((sim.Elapsed - state.ChoiceCount * 20f) / Mathf.Max(.01f, nextAt - state.ChoiceCount * 20f));
            Rect bar = new Rect(12f, 62f, 516f, 4f);
            FillRect(bar, new Color(.02f, .025f, .06f, .75f));
            FillRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height), new Color(1f, .74f, .12f));
            DrawMarketBluePanel(new Rect(10f, 73f, 520f, 27f), new Color(.02f, .055f, .16f, .83f));
            string status = state.AguanteActive ? "¡AGUANTE! ACTIVO " + state.AguanteBoostRemaining.ToString("0.0") + " s"
                : state.AguanteReady && state.SelectionPending ? "¡AGUANTE! LISTO · ELEGÍ UNA MEJORA"
                : state.AguanteReady ? "¡AGUANTE! LISTO · TOCÁ PARA ACTIVAR"
                : "¡AGUANTE! " + Mathf.RoundToInt(state.AguanteProgress * 10f) + "/10 ENTREGAS";
            if (state.GoldenDeliveriesRemaining > 0) status += "   ·   ENTREGA DORADA " + state.GoldenDeliveriesRemaining;
            if (state.GlobalBoostRemaining > 0f) status += "   ·   TODOS A FONDO " + state.GlobalBoostRemaining.ToString("0") + " s";
            MarketText(new Rect(17f, 74f, 375f, 25f), status, 13, Color.white, TextAnchor.MiddleLeft);
            if (state.AguanteReady && sim.Phase == RoundPhase.Playing)
                DrawStandardButton(BonusAguanteBounds, BonusAguanteAction, "¡AGUANTE!", !state.SelectionPending);
            else if (state.AguanteActive)
                DrawStandardButton(BonusAguanteBounds, BonusAguanteAction, "ACELERÓN " + state.AguanteBoostRemaining.ToString("0") + " s", false);
            int nextStar = state.Stars >= 3 ? -1 : state.Stars == 0 ? StreetBonusTrackState.BasicStarScore
                : state.Stars == 1 ? StreetBonusTrackState.GoodStarScore : StreetBonusTrackState.ExcellentStarScore;
            string starProgress = nextStar < 0 ? "TRES ESTRELLAS · " + state.Score.ToString("N0") + " PUNTOS"
                : "PRÓXIMA ESTRELLA " + state.Score.ToString("N0") + " / " + nextStar.ToString("N0");
            MarketText(new Rect(14f, 102f, 270f, 15f), starProgress, 10, new Color(.92f, .96f, 1f), TextAnchor.MiddleLeft);
            Rect starBar = new Rect(286f, 108f, 244f, 4f);
            FillRect(starBar, new Color(.02f, .025f, .06f, .75f));
            int lowerStar = state.Stars == 1 ? StreetBonusTrackState.BasicStarScore :
                state.Stars == 2 ? StreetBonusTrackState.GoodStarScore : 0;
            float starFill = nextStar < 0 ? 1f : Mathf.Clamp01((state.Score - lowerStar) / (float)Mathf.Max(1, nextStar - lowerStar));
            FillRect(new Rect(starBar.x, starBar.y, starBar.width * starFill, starBar.height), new Color(1f, .78f, .16f));
            long attempt = game.BonusTrackProgress != null ? game.BonusTrackProgress.attemptsStarted : -1;
            if (attempt != bonusIntroAttempt)
            {
                bonusIntroAttempt = attempt;
                bonusIntroUntil = Time.unscaledTime + 2.4f;
            }
            if (!state.SelectionPending && Time.unscaledTime < bonusIntroUntil)
                DrawBonusTrackIntro(logicalCanvasHeight);
            if (state.SelectionPending) DrawBonusChoiceCards(state);
        }

        private void DrawBonusTrackIntro(float height)
        {
            float alpha = Mathf.Clamp01(Mathf.Min((bonusIntroUntil - Time.unscaledTime) / .25f, 1f));
            Rect panel = new Rect(24f, height * .36f, W - 48f, 136f);
            DrawMarketBluePanel(panel, new Color(.32f, .035f, .075f, .92f * alpha));
            MarketText(new Rect(panel.x + 8f, panel.y + 19f, panel.width - 16f, 50f), "¡SE PUDRIÓ TODO!", 32,
                new Color(1f, .88f, .62f, alpha));
            MarketText(new Rect(panel.x + 8f, panel.y + 76f, panel.width - 16f, 35f), "120 SEGUNDOS PARA HACER HISTORIA", 17,
                new Color(1f, 1f, 1f, alpha));
        }

        private void DrawBonusChoiceCards(StreetBonusTrackState state)
        {
            float previousVerticalScale = layoutVerticalScale;
            layoutVerticalScale = 1f;
            try
            {
                Rect[] choices = CurrentResponsiveFooter(game.Sim, logicalCanvasHeight).BonusChoiceBounds;
                bool choosingRole = state.EmergencyRoleSelectionPending;
                bool super = state.IsSuperBonusPending && !choosingRole;
                int count = choosingRole ? state.EmergencyRoles.Count : state.CurrentCards.Count;
                for (int i = 0; i < count && i < choices.Length; i++)
                {
                    Rect card = choices[i];
                    float compact = Mathf.Clamp01(card.height / 120f);
                    int action = choosingRole ? BonusWorkerFirstAction + i : BonusCardFirstAction + i;
                    Rect face = new Rect(card.x + 2f * compact, card.y + 2f * compact,
                        card.width - 4f * compact, card.height - 6f * compact);
                    Color color = choosingRole ? new Color(.055f, .24f, .47f, .99f)
                        : super ? new Color(.39f, .105f, .14f, .99f) : new Color(.055f, .24f, .47f, .99f);
                    DrawMarketBluePanel(card, color);
                    if (pressedAction == action) DrawMarketBluePanel(face, new Color(.16f, .52f, .78f, .72f));

                    if (choosingRole)
                    {
                        StreetWorkerRole role = state.EmergencyRoles[i];
                        DrawBonusRoleIcon(new Rect(card.center.x - 20f * compact, card.y + 5f * compact,
                            40f * compact, 38f * compact), role);
                        MarketText(new Rect(card.x + 6f * compact, card.y + 44f * compact,
                                card.width - 12f * compact, 34f * compact),
                            StreetBonusTrackState.RoleTitle(role), Mathf.Max(10, Mathf.RoundToInt(18f * compact)), Color.white);
                        MarketText(new Rect(card.x + 6f * compact, card.y + 78f * compact,
                                card.width - 12f * compact, 28f * compact),
                            "TOCÁ PARA SUMAR", Mathf.Max(9, Mathf.RoundToInt(13f * compact)), new Color(.76f, .92f, 1f));
                    }
                    else
                    {
                        StreetBonusCard option = state.CurrentCards[i];
                        float iconTop = card.y + (super ? 14f : 4f) * compact;
                        float iconSize = (super ? 31f : 39f) * compact;
                        if (super)
                            MarketText(new Rect(card.x + 5f * compact, card.y + compact,
                                    card.width - 10f * compact, 15f * compact),
                                "✦ SUPER BONUS ✦", Mathf.Max(9, Mathf.RoundToInt(11f * compact)), new Color(1f, .82f, .25f));
                        DrawBonusCardIcon(new Rect(card.center.x - iconSize * .5f, iconTop,
                            iconSize, iconSize), option);
                        float titleTop = card.y + (super ? 46f : 41f) * compact;
                        MarketText(new Rect(card.x + 7f * compact, titleTop,
                                card.width - 14f * compact, 30f * compact),
                            WrapBonusChoiceText(StreetBonusTrackState.CardTitle(option), 13),
                            Mathf.Max(10, Mathf.RoundToInt(16f * compact)), Color.white);
                        float descriptionTop = card.y + (super ? 78f : 72f) * compact;
                        MarketText(new Rect(card.x + 8f * compact, descriptionTop, card.width - 16f * compact,
                            card.yMax - 6f * compact - descriptionTop),
                            WrapBonusChoiceText(StreetBonusTrackState.CardDescription(option), 17),
                            Mathf.Max(9, Mathf.RoundToInt(12f * compact)),
                            new Color(.94f, .97f, 1f));
                    }

                    // The whole framed card is the action target; there is no secondary ELEGIR button.
                    if (GUI.Button(LayoutRect(card), GUIContent.none, invisible) &&
                        !SuppressLevelSelectorNativeTap && !SuppressMarketNativeTap)
                        NativeAction(action);
                }
            }
            finally { layoutVerticalScale = previousVerticalScale; }
        }

        private void DrawBonusCardIcon(Rect iconBounds, StreetBonusCard card)
        {
            Texture2D texture = null;
            Rect source = Rect.zero;
            if (card == StreetBonusCard.ParrillaAFondo || card == StreetBonusCard.OtroParrillero)
            {
                texture = parrilleroIcon;
                if (texture != null) source = new Rect(0f, 0f, texture.width, texture.height);
            }
            else if (card == StreetBonusCard.CocaExpress || card == StreetBonusCard.OtroCocacolero)
            {
                int level = StreetSimulation.HuracanBonusLevelId;
                texture = CocacoleroArtForLevel(level);
                if (texture != null) source = CocacoleroPoseForLevel(level, 0);
            }
            else if (card == StreetBonusCard.MozoRelampago || card == StreetBonusCard.OtroMozo)
            {
                texture = mozoWorker;
                if (texture != null) source = ParrilleroPoses[0];
            }
            else
            {
                texture = gameCoin;
                if (texture != null) source = new Rect(0f, 0f, texture.width, texture.height);
            }

            if (texture != null) Draw(iconBounds, texture, source, false, false);
            else MarketText(iconBounds, "★", 32, new Color(1f, .86f, .34f));
        }

        private void DrawBonusRoleIcon(Rect iconBounds, StreetWorkerRole role)
        {
            int level = StreetSimulation.HuracanBonusLevelId;
            Texture2D texture = role == StreetWorkerRole.Parrillero ? parrilleroIcon
                : role == StreetWorkerRole.Mozo ? mozoWorker
                : CocacoleroArtForLevel(level);
            Rect source = Rect.zero;
            if (texture != null)
                source = role == StreetWorkerRole.Mozo ? ParrilleroPoses[0]
                    : role == StreetWorkerRole.Cocacolero ? CocacoleroPoseForLevel(level, 0)
                    : new Rect(0f, 0f, texture.width, texture.height);
            if (texture != null) Draw(iconBounds, texture, source, false, false);
            else MarketText(iconBounds, "★", 28, new Color(1f, .86f, .34f));
        }

        private static string WrapBonusChoiceText(string value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value) || maxCharacters < 1) return value ?? string.Empty;
            string[] words = value.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var lines = new System.Collections.Generic.List<string>();
            string line = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (line.Length > 0 && line.Length + 1 + word.Length > maxCharacters)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = line.Length == 0 ? word : line + " " + word;
            }
            if (line.Length > 0) lines.Add(line);
            return string.Join("\n", lines.ToArray());
        }

        private Rect BonusResultPanelBounds(float height)
        {
            float panelHeight = Mathf.Min(590f, height - 100f);
            return new Rect(14f, (height - panelHeight) * .5f, W - 28f, panelHeight);
        }

        private Rect BonusResultLevelsBounds(float height)
        {
            Rect panel = BonusResultPanelBounds(height);
            return new Rect(panel.x + 52f, panel.yMax - 76f, 174f, 60f);
        }

        private Rect BonusResultNextBounds(float height)
        {
            Rect panel = BonusResultPanelBounds(height);
            return new Rect(panel.xMax - 226f, panel.yMax - 76f, 174f, 60f);
        }

        private void DrawBonusTrackResultPanel()
        {
            StreetSimulation sim = game.Sim;
            StreetBonusTrackState state = sim.BonusTrack;
            if (state == null) return;
            FillRect(new Rect(0f, 0f, W, logicalCanvasHeight), new Color(0f, 0f, 0f, .74f));
            Rect panel = BonusResultPanelBounds(logicalCanvasHeight);
            DrawMarketBluePanel(panel, new Color(.015f, .08f, .28f, .99f));
            MarketText(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, 49f), "¡BONUS TRACK TERMINADO!", 29, new Color(1f, .82f, .2f));
            string stars = new string('★', state.Stars) + new string('☆', 3 - state.Stars);
            MarketText(new Rect(panel.x + 16f, panel.y + 64f, panel.width - 32f, 48f), stars, 38, new Color(1f, .84f, .22f));
            MarketText(new Rect(panel.x + 20f, panel.y + 113f, panel.width - 40f, 38f), "PUNTAJE  " + state.Score.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")), 26, Color.white);
            StreetBonusTrackProfile progress = game.BonusTrackProgress;
            bool record = progress != null && progress.bestScore > progress.previousBestScore;
            MarketText(new Rect(panel.x + 22f, panel.y + 151f, panel.width - 44f, 30f),
                "ENTREGAS " + state.Deliveries + "   ·   PEDIDOS COMPLETOS " + state.CompletedOrders, 17, new Color(.82f, .92f, 1f));
            MarketText(new Rect(panel.x + 22f, panel.y + 184f, panel.width - 44f, 28f),
                "RÉCORD " + (progress == null ? 0 : progress.previousBestScore.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR"))) +
                    " → " + (progress == null ? state.Score.ToString("N0") : progress.bestScore.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR"))) +
                    (record ? " · NUEVO" : ""),
                16, record ? new Color(1f, .82f, .2f) : Color.white);
            MarketText(new Rect(panel.x + 22f, panel.y + 211f, panel.width - 44f, 24f),
                "MEJOR: " + (progress == null ? state.Deliveries : progress.bestDeliveries) + " ENTREGAS · " +
                (progress == null ? state.Stars : progress.bestStars) + "★", 14, new Color(.88f, .94f, 1f));
            MarketText(new Rect(panel.x + 18f, panel.y + 237f, panel.width - 36f, 56f),
                "MEJORAS: " + string.Join(" · ", new System.Collections.Generic.List<string>(state.ChoiceHistory).ToArray()), 13, new Color(.82f, .9f, 1f));
            int tickets = progress == null ? state.TicketsAwardedThisRun : progress.lastTickets;
            MarketText(new Rect(panel.x + 18f, panel.y + 294f, panel.width - 36f, 38f),
                tickets > 0 ? "TICKETS GANADOS: " + tickets + "   ·   SALDO " + game.TicketBalance : "SIN ESTRELLAS · SIN TICKETS", 18,
                tickets > 0 ? new Color(1f, .84f, .27f) : Color.white);
            if (progress != null && progress.threeStarTicketAwarded && state.Stars == 3)
                MarketText(new Rect(panel.x + 18f, panel.y + 333f, panel.width - 36f, 22f), "Bono de primera vez por 3 estrellas: cobrado", 12, new Color(.8f, .9f, 1f));

            bool canAdvance = game.CanAdvanceAfterResult;
            DrawStandardButton(BonusResultLevelsBounds(logicalCanvasHeight), ResultLevelsAction, "NIVELES");
            DrawStandardButton(BonusResultNextBounds(logicalCanvasHeight), VictoryExitAction,
                canAdvance ? "SIGUIENTE PARTIDO" : "ÚLTIMO NIVEL", canAdvance);
        }
    }
}
