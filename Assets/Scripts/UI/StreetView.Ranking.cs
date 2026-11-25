using System.Globalization;
using HayChoriYPaty.Cloud;
using UnityEngine;

namespace HayChoriYPaty
{
    // Ranking de la hinchada (2026-11-25, a pedido): podio con los tres primeros, el resto del Top 10 y la fila propia fija abajo.
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
        private static readonly Color RankingMine = new Color(.62f, .86f, .36f), RankingRowFace = new Color(.93f, .86f, .68f), RankingRowEdge = new Color(.24f, .105f, .025f);

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
            Rect panel = new Rect(16, 20, W - 32, height - 20 - 96 - (mainButton ? 64f : 0f));
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .97f));
            MarketText(new Rect(panel.x + 18, panel.y + 16, panel.width - 36, 40), "RANKING DE LA HINCHADA", 30, RankingGold);
            MarketText(new Rect(panel.x + 18, panel.y + 56, panel.width - 36, 22), "PUNTOS DE LIGA · TODOS LOS TIEMPOS", 14, new Color(.95f, .87f, .72f));

            switch (rankingState)
            {
                case RankingState.Ready: DrawRankingBoard(panel); break;
                case RankingState.NeedNickname: DrawRankingNicknameForm(panel, height); break;
                case RankingState.Loading: case RankingState.Idle:
                    MarketText(new Rect(panel.x + 20, panel.y + 190, panel.width - 40, 40), "CARGANDO POSICIONES…", 24, Color.white); break;
                default: DrawRankingUnavailable(panel, height); break;
            }
            DrawStandardButton(RankingBackBounds(height), RankingBackAction, "VOLVER AL BARRIO");
        }

        private void DrawRankingBoard(Rect panel)
        {
            LeaderboardSnapshot snapshot = rankingSnapshot;
            float height = logicalCanvasHeight;
            float ownRowTop = panel.yMax - 12 - 8 - 56;
            float podiumTop = panel.y + 88, podiumHeight = 200;
            DrawRankingPodium(panel, podiumTop, podiumHeight, snapshot);

            float rowsTop = podiumTop + podiumHeight + 8, pitch = 50f;
            int fit = Mathf.Clamp(Mathf.FloorToInt((ownRowTop - 30f - rowsTop) / pitch), 0, RankingListRows);
            for (int i = 0; i < fit; i++)
            {
                int index = 3 + i;
                if (index >= snapshot.top.Count) break;
                DrawRankingRow(new Rect(panel.x + 18, rowsTop + i * pitch, panel.width - 36, 44), index + 1, snapshot.top[index], snapshot.top[index].uid == snapshot.me?.uid);
            }
            if (snapshot.me != null)
            {
                MarketText(new Rect(panel.x + 18, ownRowTop - 26, panel.width - 36, 22), RankingGapText(snapshot), 15, new Color(.52f, 1f, .47f));
                DrawRankingRow(new Rect(panel.x + 18, ownRowTop, panel.width - 36, 56), snapshot.myRank, snapshot.me, true);
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

        // 2º a la izquierda, 1º al centro (más alto) y 3º a la derecha, cada uno sobre su pedestal.
        private void DrawRankingPodium(Rect panel, float top, float zoneHeight, LeaderboardSnapshot snapshot)
        {
            int[] place = { 1, 0, 2 };
            float[] pedestal = { 78f, 108f, 62f };
            Color[] medal = { RankingSilver, RankingGold, RankingBronze };
            float bottom = top + zoneHeight, step = (panel.width - 36f) / 3f;
            for (int column = 0; column < 3; column++)
            {
                float cx = panel.x + 18f + step * (column + .5f);
                float baseTop = bottom - pedestal[column];
                FillRect(new Rect(cx - step * .5f + 4, baseTop, step - 8, pedestal[column]), new Color(medal[column].r, medal[column].g, medal[column].b, .30f));
                FillRect(new Rect(cx - step * .5f + 4, baseTop, step - 8, 4), medal[column]);
                int rank = place[column] + 1;
                LeaderboardEntry entry = place[column] < snapshot.top.Count ? snapshot.top[place[column]] : null;
                Rect badge = new Rect(cx - 50, baseTop - 62, 56, 56);
                FillRect(new Rect(badge.x - 3, badge.y - 3, badge.width + 6, badge.height + 6), medal[column]);
                FillRect(badge, new Color(.20f, .10f, .05f));
                MarketText(badge, entry == null ? "?" : entry.nickname.Substring(0, 1).ToUpperInvariant(), 34, medal[column]);
                MarketText(new Rect(cx + 10, baseTop - 66, 48, 56), rank.ToString(), 46, medal[column], TextAnchor.MiddleLeft);
                if (entry == null) { MarketText(new Rect(cx - step * .5f + 6, baseTop + 10, step - 12, 26), "—", 18, Color.white); continue; }
                bool mine = entry.uid == snapshot.me?.uid;
                MarketText(new Rect(cx - step * .5f + 6, baseTop + 10, step - 12, 26), entry.nickname, 20, mine ? RankingMine : Color.white);
                MarketText(new Rect(cx - step * .5f + 6, baseTop + 36, step - 12, 24), "★ " + RankingNumber(entry.points), 18, RankingGold);
            }
        }

        private void DrawRankingRow(Rect row, long rank, LeaderboardEntry entry, bool mine)
        {
            FillRect(row, mine ? new Color(.20f, .42f, .12f) : RankingRowEdge);
            FillRect(new Rect(row.x + 3, row.y + 3, row.width - 6, row.height - 6), mine ? RankingMine : RankingRowFace);
            string place = rank > 0 ? rank.ToString() : "—";
            // Positions into the thousands (a long way down the table) get a wider column so they never touch the apodo.
            float placeWidth = place.Length > 3 ? 92f : 60f, nameX = row.x + 14 + placeWidth;
            MarketText(new Rect(row.x + 8, row.y, placeWidth, row.height), place, row.height > 50 ? 28 : 24, MarketInk);
            MarketText(new Rect(nameX, row.y + 3, row.xMax - nameX - 150, row.height * .58f), entry.nickname, 21, MarketInk, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX, row.y + row.height * .56f, row.xMax - nameX - 150, row.height * .38f),
                entry.wonLevels + " / " + StreetLeague.MaxWonLevels + " CANCHAS", 13, new Color(.36f, .20f, .08f), TextAnchor.MiddleLeft);
            MarketText(new Rect(row.xMax - 146, row.y, 138, row.height), "★ " + RankingNumber(entry.points), 22, MarketInk, TextAnchor.MiddleRight);
        }

        private void DrawRankingNicknameForm(Rect panel, float height)
        {
            MarketText(new Rect(panel.x + 24, panel.y + 120, panel.width - 48, 36), "ELEGÍ TU APODO", 28, Color.white);
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
            MarketText(new Rect(panel.x + 22, panel.y + 120, panel.width - 44, 40), failed ? "NO PUDIMOS CARGAR EL RANKING" : "RANKING SIN CONEXIÓN", 24, Color.white);
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
