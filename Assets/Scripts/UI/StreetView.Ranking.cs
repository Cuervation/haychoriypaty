using System.Globalization;
using HayChoriYPaty.Cloud;
using UnityEngine;

namespace HayChoriYPaty
{
    // Ranking de la hinchada (2026-11-25, a pedido): podio con los tres primeros, el resto del Top 10 y la fila propia fija abajo.
    // Rediseño visual del mismo día: tarjeta de podio, medallones y filas redondeadas (arte en StreetView.RankingArt.cs).
    // Los datos vienen del servidor (colección leaderboard); sin sesión o sin conexión se muestra el progreso propio.
    public sealed partial class StreetView
    {
        private enum RankingState { Idle, Loading, Ready, NeedNickname, Unavailable, Error }
        private const int RankingRetryAction = 211, RankingNicknameSaveAction = 212;
        private const int RankingListRows = 7;
        private RankingState rankingState;
        private LeaderboardSnapshot rankingSnapshot;
        private string rankingNicknameInput = "", rankingNicknameError;
        private int rankingLoadToken;
        private GUIStyle rankingFieldStyle;

        private static readonly Color RankingGold = new Color(1f, .84f, .38f), RankingSilver = new Color(.80f, .84f, .90f), RankingBronze = new Color(.84f, .54f, .28f);
        private static readonly Color RankingMine = new Color(.62f, .86f, .36f), RankingRowFace = new Color(.93f, .86f, .68f);

        private static Rect RankingPrimaryBounds(float height) => new Rect(110, height - 148, 320, 56);

        private void OpenRanking()
        {
            hubActive = false; rankingActive = true;
            rankingNicknameError = null;
            LoadRanking();
        }

        private async void LoadRanking()
        {
            int token = ++rankingLoadToken;
            FirebasePlayerSession session = FirebasePlayerSession.Instance;
            if (session == null || !session.LeaderboardReady) { rankingState = RankingState.Unavailable; return; }
            rankingState = RankingState.Loading;
            try
            {
                LeaderboardSnapshot snapshot = await session.LoadLeaderboardAsync();
                if (this == null || token != rankingLoadToken) return;
                rankingSnapshot = snapshot;
                rankingState = snapshot.me == null ? RankingState.NeedNickname : RankingState.Ready;
            }
            catch (System.Exception)
            {
                // The session already logs provider failures without account data; the screen only offers a retry.
                if (this != null && token == rankingLoadToken) rankingState = RankingState.Error;
            }
        }

        private async void SubmitRankingNickname()
        {
            if (!StreetLeague.TryNormalizeNickname(rankingNicknameInput, out string nickname))
            {
                rankingNicknameError = "3 a 16 letras, números, espacio, punto o guion.";
                return;
            }
            FirebasePlayerSession session = FirebasePlayerSession.Instance;
            if (session == null || !session.LeaderboardReady) { rankingState = RankingState.Unavailable; return; }
            rankingNicknameError = null; rankingState = RankingState.Loading;
            bool saved = await session.SaveLeaderboardNicknameAsync(nickname);
            if (this == null) return;
            if (saved) LoadRanking();
            else { rankingNicknameError = "No pudimos guardar el apodo. Probá de nuevo."; rankingState = RankingState.NeedNickname; }
        }

        private int HitRankingAction(Vector2 p)
        {
            float height = logicalCanvasHeight;
            if (RankingBackBounds(height).Contains(p)) return RankingBackAction;
            Rect primary = RankingPrimaryBounds(height);
            if (rankingState == RankingState.NeedNickname && primary.Contains(p)) return RankingNicknameSaveAction;
            if ((rankingState == RankingState.Error || rankingState == RankingState.Unavailable) && primary.Contains(p)) return RankingRetryAction;
            return 0;
        }

        private void DispatchRankingAction(int action)
        {
            if (action == RankingBackAction) { rankingActive = false; hubActive = true; rankingLoadToken++; }
            else if (action == RankingRetryAction) LoadRanking();
            else if (action == RankingNicknameSaveAction) SubmitRankingNickname();
        }

        private static string RankingNumber(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));

        private void DrawRankingScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .72f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            // Screens with a main button keep room for it above "VOLVER AL BARRIO" instead of overlapping the frame.
            bool mainButton = rankingState == RankingState.NeedNickname || rankingState == RankingState.Unavailable || rankingState == RankingState.Error;
            Rect panel = new Rect(12, 16, W - 24, height - 16 - 92 - (mainButton ? 64f : 0f));
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .97f));
            DrawRankingHeader(panel);

            switch (rankingState)
            {
                case RankingState.Ready: DrawRankingBoard(panel); break;
                case RankingState.NeedNickname: DrawRankingNicknameForm(panel, height); break;
                case RankingState.Loading: case RankingState.Idle:
                    RankingOutlined(new Rect(panel.x + 20, panel.y + 190, panel.width - 40, 40), "CARGANDO POSICIONES…", 24, Color.white, MarketInk); break;
                default: DrawRankingUnavailable(panel, height); break;
            }
            DrawStandardButton(RankingBackBounds(height), RankingBackAction, "VOLVER AL BARRIO");
        }

        // Title in outlined gold between two stars, and the "Puntos de Liga" chip underneath.
        private void DrawRankingHeader(Rect panel)
        {
            float star = 30f;
            DrawRankingStar(new Rect(panel.x + 22, panel.y + 22, star, star));
            DrawRankingStar(new Rect(panel.xMax - 22 - star, panel.y + 22, star, star));
            RankingOutlined(new Rect(panel.x + 58, panel.y + 14, panel.width - 116, 46), "RANKING DE LA HINCHADA", 32, RankingGold, new Color(.16f, .06f, .01f), TextAnchor.MiddleCenter, 1.3f);
            Rect chip = new Rect(panel.center.x - 150, panel.y + 62, 300, 24);
            RankingPill(chip, new Color(0f, 0f, 0f, .42f));
            MarketText(new Rect(chip.x + 10, chip.y, chip.width - 20, chip.height), "PUNTOS DE LIGA · TODOS LOS TIEMPOS", 14, new Color(.98f, .90f, .74f));
        }

        private void DrawRankingBoard(Rect panel)
        {
            LeaderboardSnapshot snapshot = rankingSnapshot;
            const float ownRowHeight = 66f;
            float ownRowTop = panel.yMax - 14 - ownRowHeight;
            // Tall phones share the spare height between a taller podium and roomier rows instead of leaving a gap.
            const float baseCard = 246f, basePitch = 62f;
            float spare = Mathf.Max(0f, ownRowTop - 36f - (panel.y + 96 + baseCard + 10) - RankingListRows * basePitch + 6f);
            float cardGrow = Mathf.Min(48f, spare * .3f);
            float pitch = Mathf.Min(76f, basePitch + (spare - cardGrow) / RankingListRows), rowHeight = pitch - 6f;
            Rect card = new Rect(panel.x + 14, panel.y + 96, panel.width - 28, baseCard + cardGrow);
            DrawRankingPodium(card, snapshot, cardGrow);

            float rowsTop = card.yMax + 10;
            int fit = Mathf.Clamp(Mathf.FloorToInt((ownRowTop - 36f - rowsTop + (pitch - rowHeight)) / pitch), 0, RankingListRows);
            for (int i = 0; i < fit; i++)
            {
                int index = 3 + i;
                Rect row = new Rect(panel.x + 14, rowsTop + i * pitch, panel.width - 28, rowHeight);
                LeaderboardEntry entry = index < snapshot.top.Count ? snapshot.top[index] : null;
                // Unused places stay visible as "puesto libre" so the table reads as a table, never as made-up players.
                DrawRankingRow(row, index + 1, LeaderboardEntryView.From(entry), entry != null && entry.uid == snapshot.me?.uid);
            }
            if (snapshot.me != null)
            {
                string gap = RankingGapText(snapshot);
                float gapWidth = Mathf.Min(panel.width - 40, 22f + gap.Length * 9.2f);
                Rect chip = new Rect(panel.center.x - gapWidth * .5f, ownRowTop - 32, gapWidth, 26);
                RankingPill(chip, new Color(0f, 0f, 0f, .55f));
                MarketText(new Rect(chip.x + 10, chip.y, chip.width - 20, chip.height), gap, 15, new Color(.52f, 1f, .47f));
                DrawRankingRow(new Rect(panel.x + 8, ownRowTop, panel.width - 16, ownRowHeight), snapshot.myRank, LeaderboardEntryView.From(snapshot.me), true);
            }
        }

        private static string RankingGapText(LeaderboardSnapshot snapshot)
        {
            if (snapshot.me == null) return "";
            if (snapshot.myRank == 1) return "SOS EL Nº 1 DE LA HINCHADA";
            if (snapshot.myRank > 1 && snapshot.myRank <= snapshot.top.Count)
            {
                long missing = snapshot.top[(int)snapshot.myRank - 2].points - snapshot.me.points + 1;
                return "TE FALTAN " + RankingNumber(System.Math.Max(1, missing)) + " PUNTOS PARA EL PUESTO " + (snapshot.myRank - 1);
            }
            if (snapshot.top.Count >= LeaderboardSnapshot.TopSize)
            {
                long missing = snapshot.top[snapshot.top.Count - 1].points - snapshot.me.points + 1;
                return "TE FALTAN " + RankingNumber(System.Math.Max(1, missing)) + " PUNTOS PARA ENTRAR AL TOP " + LeaderboardSnapshot.TopSize;
            }
            return "GANÁ CANCHAS PARA SUBIR EN LA TABLA";
        }

        // 2º a la izquierda, 1º al centro (más alto) y 3º a la derecha: medallón con laureles, número grande y pedestal metálico.
        private void DrawRankingPodium(Rect card, LeaderboardSnapshot snapshot, float grow)
        {
            DrawRankingPodiumCard(card);
            int[] place = { 1, 0, 2 };
            float lift = Mathf.Round(grow * .6f);
            float[] pedestal = { 92f + lift, 118f + lift, 80f + lift }, medal = { 64f, 78f, 64f };
            float bottom = card.yMax - 14f, step = (card.width - 20f) / 3f;
            for (int column = 0; column < 3; column++)
            {
                int index = place[column];
                Color metal = RankingMetal(index);
                float cx = card.x + 10f + step * (column + .5f);
                float baseTop = bottom - pedestal[column];
                DrawRankingPedestal(new Rect(cx - step * .5f + 5, baseTop, step - 10, pedestal[column]), index);
                LeaderboardEntryView entry = LeaderboardEntryView.From(index < snapshot.top.Count ? snapshot.top[index] : null);
                float s = medal[column];
                Rect medallion = new Rect(cx - s * .5f, baseTop - s - 16f, s, s);
                DrawRankingMedallion(medallion, metal, entry, true);
                if (!entry.empty)
                {
                    // Canchas ganadas as a small badge on the medallion, like a level tag.
                    Rect badge = new Rect(cx - 25, medallion.yMax - 11, 50, 20);
                    RankingPill(new Rect(badge.x - 2, badge.y - 2, badge.width + 4, badge.height + 4), MarketNavy);
                    RankingPill(badge, new Color(.12f, .44f, .86f));
                    MarketText(badge, entry.wonLevels + "/" + StreetLeague.MaxWonLevels, 13, Color.white);
                }
                RankingOutlined(new Rect(medallion.xMax - s * .02f, medallion.y + s * .30f, 44, s * .78f), (index + 1).ToString(),
                    index == 0 ? 46 : 40, metal, new Color(.14f, .06f, .01f), TextAnchor.MiddleLeft, 1.4f);

                Rect nameRect = new Rect(cx - step * .5f + 10, baseTop + 16, step - 20, 26);
                if (entry.empty)
                {
                    RankingOutlined(nameRect, "LIBRE", 18, new Color(1f, 1f, 1f, .85f), new Color(.14f, .06f, .01f, .8f));
                    continue;
                }
                bool mine = entry.uid == snapshot.me?.uid;
                RankingOutlined(nameRect, entry.nickname, 20, mine ? RankingMine : Color.white, new Color(.14f, .06f, .01f));
                string points = RankingNumber(entry.points);
                float pillWidth = Mathf.Min(step - 18, 40f + points.Length * 11f);
                Rect pill = new Rect(cx - pillWidth * .5f, nameRect.yMax + 4, pillWidth, 26);
                RankingPill(pill, new Color(.10f, .04f, .01f, .55f));
                DrawRankingStar(new Rect(pill.x + 4, pill.y + 2, 22, 22));
                MarketText(new Rect(pill.x + 26, pill.y, pill.width - 30, pill.height), points, 18, Color.white);
            }
        }

        private void DrawRankingRow(Rect row, long rank, LeaderboardEntryView entry, bool mine)
        {
            RankingRowKind kind = entry.empty ? RankingRowKind.Free : mine ? RankingRowKind.Mine
                : rank == 1 ? RankingRowKind.Gold : rank == 2 ? RankingRowKind.Silver : rank == 3 ? RankingRowKind.Bronze : RankingRowKind.Normal;
            string place = rank > 0 ? rank.ToString() : "—";
            // Positions into the thousands (a long way down the table) get a wider block so they never touch the avatar.
            float block = place.Length > 3 ? 96f : place.Length > 2 ? 80f : 66f;
            DrawRankingRowBase(row, kind, block);
            Color ink = new Color(.14f, .06f, .01f);
            float faceHeight = row.height - 6f;
            RankingOutlined(new Rect(row.x + 4, row.y, block - 8, faceHeight), place, row.height > 60 ? 34 : 30, entry.empty ? new Color(.85f, .76f, .62f) : Color.white, ink, TextAnchor.MiddleCenter, 1.3f);

            float avatar = Mathf.Min(46f, faceHeight - 8f);
            Rect medallion = new Rect(row.x + block + 10, row.y + (faceHeight - avatar) * .5f + 1, avatar, avatar);
            Color ring = mine ? new Color(.30f, .78f, .22f) : kind == RankingRowKind.Normal ? new Color(.94f, .88f, .74f) : kind == RankingRowKind.Free ? new Color(.52f, .42f, .30f) : RankingMetal((int)rank - 1);
            DrawRankingMedallion(medallion, ring, entry, false);

            float nameX = medallion.xMax + 10, nameWidth = row.xMax - nameX - 118;
            if (entry.empty)
            {
                MarketText(new Rect(nameX, row.y, nameWidth, faceHeight), "PUESTO LIBRE", 18, new Color(.82f, .72f, .58f), TextAnchor.MiddleLeft);
                return;
            }
            RankingOutlined(new Rect(nameX, row.y + 3, nameWidth, faceHeight * .58f), entry.nickname, row.height > 60 ? 24 : 21,
                mine ? Color.white : RankingGold, ink, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX, row.y + faceHeight * .58f, nameWidth, faceHeight * .36f),
                entry.wonLevels + " / " + StreetLeague.MaxWonLevels + " CANCHAS", 13, mine ? new Color(.10f, .30f, .06f) : new Color(.42f, .24f, .10f), TextAnchor.MiddleLeft);
            float right = row.xMax - 14;
            DrawRankingStar(new Rect(right - 26, row.y + 4, 24, 24));
            MarketText(new Rect(right - 112, row.y + faceHeight * .48f, 112, faceHeight * .48f), RankingNumber(entry.points), 22, MarketInk, TextAnchor.MiddleRight);
        }

        private void DrawRankingNicknameForm(Rect panel, float height)
        {
            RankingOutlined(new Rect(panel.x + 24, panel.y + 120, panel.width - 48, 40), "ELEGÍ TU APODO", 30, Color.white, MarketInk);
            MarketText(new Rect(panel.x + 28, panel.y + 160, panel.width - 56, 48),
                "Es lo único que ven los otros hinchas. Nunca publicamos tu nombre ni tu mail de Google.", 15, new Color(.95f, .87f, .72f));
            Rect field = new Rect(panel.x + 56, panel.y + 232, panel.width - 112, 56);
            FillRect(new Rect(field.x - 3, field.y - 3, field.width + 6, field.height + 6), RankingGold);
            FillRect(field, RankingRowFace);
            if (rankingFieldStyle == null)
            {
                // A bare style: the skin's text field paints its own grey box over the wood-and-cream field drawn above.
                rankingFieldStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 26, clipping = TextClipping.Clip };
                rankingFieldStyle.normal.textColor = rankingFieldStyle.focused.textColor = rankingFieldStyle.hover.textColor = rankingFieldStyle.active.textColor = MarketInk;
                if (menuTitle != null) rankingFieldStyle.font = menuTitle.font;
            }
            rankingNicknameInput = GUI.TextField(field, rankingNicknameInput ?? "", StreetLeague.NicknameMaxLength, rankingFieldStyle);
            MarketText(new Rect(panel.x + 28, field.yMax + 14, panel.width - 56, 26),
                string.IsNullOrEmpty(rankingNicknameError) ? "3 a 16 letras, números, espacio, punto o guion." : rankingNicknameError, 15,
                string.IsNullOrEmpty(rankingNicknameError) ? new Color(.95f, .87f, .72f) : new Color(1f, .55f, .45f));
            StreetPlayerStatistics stats = game?.PlayerStatistics;
            MarketText(new Rect(panel.x + 28, field.yMax + 60, panel.width - 56, 28),
                "TUS PUNTOS: ★ " + RankingNumber(StreetLeague.Points(stats)) + " · " + StreetLeague.WonLevels(stats) + " / " + StreetLeague.MaxWonLevels + " CANCHAS", 17, RankingGold);
            DrawStandardButton(RankingPrimaryBounds(height), RankingNicknameSaveAction, "ENTRAR AL RANKING");
        }

        private void DrawRankingUnavailable(Rect panel, float height)
        {
            bool failed = rankingState == RankingState.Error;
            RankingOutlined(new Rect(panel.x + 22, panel.y + 120, panel.width - 44, 40), failed ? "NO PUDIMOS CARGAR EL RANKING" : "RANKING SIN CONEXIÓN", 26, Color.white, MarketInk);
            MarketText(new Rect(panel.x + 28, panel.y + 168, panel.width - 56, 48),
                failed ? "Revisá tu conexión y probá de nuevo." : "Necesitás conexión y tu sesión de Google para ver las posiciones.", 16, new Color(.95f, .87f, .72f));
            StreetPlayerStatistics stats = game?.PlayerStatistics;
            MarketText(new Rect(panel.x + 26, panel.y + 250, panel.width - 52, 30), "TU PROGRESO", 19, RankingGold);
            MarketText(new Rect(panel.x + 26, panel.y + 286, panel.width - 52, 36), "★ " + RankingNumber(StreetLeague.Points(stats)) + " PUNTOS DE LIGA", 24, Color.white);
            MarketText(new Rect(panel.x + 26, panel.y + 326, panel.width - 52, 32), "CANCHAS GANADAS: " + StreetLeague.WonLevels(stats) + " / " + StreetLeague.MaxWonLevels, 19, Color.white);
            MarketText(new Rect(panel.x + 26, panel.y + 366, panel.width - 52, 44), RankingObjectiveText(out int _), 16, new Color(.52f, 1f, .47f));
            DrawStandardButton(RankingPrimaryBounds(height), RankingRetryAction, "REINTENTAR");
        }
    }
}
