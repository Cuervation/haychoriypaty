using System.Globalization;
using HayChoriYPaty.Cloud;
using UnityEngine;

namespace HayChoriYPaty
{
    // Perfil del hincha (2026-11-26, a pedido; referencia: la tarjeta de jugador y la ficha de perfil de un juego móvil,
    // llevadas a la estética del juego). En el barrio, una tarjeta arriba con el medallón, el apodo y los Puntos de Liga;
    // al tocarla se abre la ficha: apodo editable (el mismo del Ranking), estadísticas, el mail de la cuenta de Google y
    // CERRAR SESIÓN con confirmación. No cambia datos ni reglas: el apodo se guarda con el mismo camino que el Ranking.
    public sealed partial class StreetView
    {
        private const int HubProfileAction = 240, ProfileBackAction = 241, ProfileEditAction = 242, ProfileSaveAction = 243,
            ProfileCancelEditAction = 244, ProfileSignOutAction = 245, ProfileConfirmSignOutAction = 246,
            ProfileCancelSignOutAction = 247, ProfileSignInAction = 248;
        private static readonly Color ProfileRed = new Color(.86f, .20f, .14f), ProfileGreen = new Color(.36f, .74f, .20f);
        private bool profileActive, profileEditing, profileSaving, profileConfirmSignOut;
        private string profileNicknameInput = "", profileNicknameError;
        private TouchScreenKeyboard profileKeyboard;
        private GUIStyle profileFieldStyle;

        // Editor captures without a Firebase session read these instead (never set in the game, where the session always exists).
        private string profilePreviewNickname, profilePreviewEmail;
        private bool profilePreviewSignedIn;

        private static FirebasePlayerSession ProfileSession => FirebasePlayerSession.Instance;
        private string ProfileNickname => ProfileSession != null ? ProfileSession.LeaderboardNickname : profilePreviewNickname;
        private bool ProfileSignedIn => ProfileSession != null ? ProfileSession.ProfileSignedIn : profilePreviewSignedIn;
        private string ProfileEmail => ProfileSession != null ? ProfileSession.AccountEmail : profilePreviewEmail;
        private static string ProfileNumber(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));

        // ---- Hub card ----
        // Canvas-logical bounds (y in logicalCanvasHeight units). Beside the settings gear when it falls on the card's row (16:9).
        private Rect HubProfileCardBounds()
        {
            Rect card = new Rect(10f, 8f, 300f, 54f);
            Rect gear = HubSettingsButtonLayoutBounds(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            gear.y *= layoutVerticalScale; gear.height *= layoutVerticalScale;
            if (gear.width > 0f && gear.yMin < card.yMax + 2f && gear.yMax > card.yMin - 2f) card.x = gear.xMax + 8f;
            // Never cover the music button (on 16:9 it falls back to the top row).
            Rect music = HubTopMusicButtonLayoutBounds(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            music.y *= layoutVerticalScale; music.height *= layoutVerticalScale;
            if (music.width > 0f && music.yMin < card.yMax + 2f && music.yMax > card.yMin - 2f && music.xMin < card.xMax + 8f && music.xMax > card.x)
                card.width = Mathf.Max(210f, music.xMin - 8f - card.x);
            return card;
        }

        private LeaderboardEntryView ProfileEntry() => new LeaderboardEntryView {
            uid = ProfileSession?.AccountUid ?? "local", nickname = ProfileNickname ?? "?" };

        private void DrawHubProfileCard()
        {
            float previousScale = layoutVerticalScale;
            Rect card = HubProfileCardBounds();
            layoutVerticalScale = 1f;
            try
            {
                DrawRankingRowBase(card, RankingRowKind.Normal, 0f);
                DrawRankingMedallion(new Rect(card.x + 7, card.y + 5, 42, 42), RankingGold, ProfileEntry(), false);
                string name = ProfileNickname ?? (ProfileSignedIn ? "ELEGÍ TU APODO" : "MI PERFIL");
                RankingOutlined(new Rect(card.x + 58, card.y + 3, card.width - 68, 28), name, 22, Color.white, MarketInk, TextAnchor.MiddleLeft);
                StreetPlayerStatistics stats = game?.PlayerStatistics;
                DrawRankingStar(new Rect(card.x + 58, card.y + 30, 16, 16));
                MarketText(new Rect(card.x + 78, card.y + 29, card.width - 90, 18),
                    ProfileNumber(StreetLeague.Points(stats)) + " PUNTOS · " + StreetLeague.WonLevels(stats) + "/" + StreetLeague.MaxWonLevels + " CANCHAS",
                    13, MarketInk, TextAnchor.MiddleLeft);
                if (GUI.Button(card, "", invisible)) NativeAction(HubProfileAction);
            }
            finally { layoutVerticalScale = previousScale; }
        }

        // ---- Profile screen layout (canvas-logical, drawn with layoutVerticalScale = 1) ----
        private static Rect ProfilePanelBounds(float height) => new Rect(12, 16, W - 24, height - 16 - 92);
        private static Rect ProfileCardBounds(float height) { Rect p = ProfilePanelBounds(height); return new Rect(p.x + 14, p.y + 70, p.width - 28, 170); }
        private static Rect ProfileNameBounds(float height) { Rect c = ProfileCardBounds(height); return new Rect(c.x + 150, c.y + 30, c.width - 166, 54); }
        private static Rect ProfileEditBounds(float height) { Rect c = ProfileCardBounds(height); return new Rect(c.xMax - 138, c.y + 100, 120, 40); }
        private static Rect ProfileSaveBounds(float height) { Rect c = ProfileCardBounds(height); return new Rect(c.x + 150, c.y + 100, 150, 42); }
        private static Rect ProfileCancelEditBounds(float height) { Rect c = ProfileCardBounds(height); return new Rect(c.x + 310, c.y + 100, 150, 42); }
        private static Rect ProfileTileBounds(float height, int index)
        {
            Rect c = ProfileCardBounds(height);
            float width = (c.width - 20f) / 3f;
            return new Rect(c.x + (index % 3) * (width + 10f), c.yMax + 34f + (index / 3) * 112f, width, 100f);
        }
        // The Google account sits at the foot of the panel; the next objective fills the space between it and the stats.
        private static Rect ProfileSignOutBounds(float height) { Rect p = ProfilePanelBounds(height); return new Rect(W * .5f - 150f, p.yMax - 24f - 56f, 300f, 56f); }
        private static Rect ProfileEmailBounds(float height) { Rect s = ProfileSignOutBounds(height); Rect c = ProfileCardBounds(height); return new Rect(c.x, s.y - 16f - 50f, c.width, 50f); }
        private static Rect ProfileObjectiveBounds(float height)
        {
            Rect tiles = ProfileTileBounds(height, 5), email = ProfileEmailBounds(height), c = ProfileCardBounds(height);
            float top = tiles.yMax + 12f, bottom = email.y - 34f;
            return bottom - top < 64f ? Rect.zero : new Rect(c.x, (top + bottom) * .5f - 32f, c.width, 64f);
        }
        private static Rect ProfileConfirmPanelBounds(float height) => new Rect(36, height * .5f - 130f, W - 72, 250);
        private static Rect ProfileConfirmYesBounds(float height) { Rect p = ProfileConfirmPanelBounds(height); return new Rect(p.x + 22, p.yMax - 80, (p.width - 56) * .5f, 56); }
        private static Rect ProfileConfirmNoBounds(float height) { Rect y = ProfileConfirmYesBounds(height); return new Rect(y.xMax + 12, y.y, y.width, y.height); }

        private void OpenProfile()
        {
            hubActive = false; profileActive = true;
            profileEditing = profileSaving = profileConfirmSignOut = false;
            profileNicknameError = null; profileKeyboard = null;
        }

        private void CloseProfile()
        {
            profileActive = false; profileEditing = false; profileConfirmSignOut = false;
            if (profileKeyboard != null) { profileKeyboard.active = false; profileKeyboard = null; }
        }

        private void DrawProfileScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .72f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            Rect panel = ProfilePanelBounds(height);
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .97f));
            float star = 30f;
            DrawRankingStar(new Rect(panel.x + 22, panel.y + 22, star, star));
            DrawRankingStar(new Rect(panel.xMax - 22 - star, panel.y + 22, star, star));
            RankingOutlined(new Rect(panel.x + 58, panel.y + 14, panel.width - 116, 46), "MI PERFIL DE HINCHA", 32, RankingGold, new Color(.16f, .06f, .01f), TextAnchor.MiddleCenter, 1.3f);

            DrawProfileCard(height);
            DrawProfileStats(height);
            DrawProfileAccount(height);
            DrawStandardButton(BadgeBackBounds(height), ProfileBackAction, "VOLVER AL BARRIO", !profileConfirmSignOut);
            if (profileConfirmSignOut) DrawProfileSignOutConfirm(height);
        }

        // Sky card like the ranking podium: medallion with laurels, the apodo (tap to edit) and the Liga points.
        private void DrawProfileCard(float height)
        {
            Rect card = ProfileCardBounds(height);
            DrawRankingPodiumCard(card);
            DrawRankingMedallion(new Rect(card.x + 28, card.y + 34, 104, 104), RankingGold, ProfileEntry(), true);
            Rect name = ProfileNameBounds(height);
            StreetPlayerStatistics stats = game?.PlayerStatistics;
            if (profileEditing)
            {
                RankingPill(new Rect(name.x - 3, name.y - 3, name.width + 6, name.height + 6), RankingGold, 12f);
                RankingPill(name, RankingRowFace, 10f);
                bool nativeKeyboard = profileKeyboard != null;
                if (nativeKeyboard || profileSaving)
                {
                    MarketText(name, string.IsNullOrEmpty(profileNicknameInput) ? "TU APODO" : profileNicknameInput, 26,
                        string.IsNullOrEmpty(profileNicknameInput) ? new Color(.55f, .45f, .35f) : MarketInk);
                }
                else if (TouchScreenKeyboard.isSupported && !Application.isEditor)
                {
                    MarketText(name, string.IsNullOrEmpty(profileNicknameInput) ? "TOCÁ PARA ESCRIBIR" : profileNicknameInput, 24, MarketInk);
                    if (GUI.Button(name, "", invisible)) OpenProfileKeyboard();
                }
                else
                {
                    if (profileFieldStyle == null)
                    {
                        profileFieldStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 26, clipping = TextClipping.Clip };
                        profileFieldStyle.normal.textColor = profileFieldStyle.focused.textColor = profileFieldStyle.hover.textColor = profileFieldStyle.active.textColor = MarketInk;
                        if (menuTitle != null) profileFieldStyle.font = menuTitle.font;
                    }
                    GUI.SetNextControlName("ProfileNickname");
                    profileNicknameInput = GUI.TextField(name, profileNicknameInput ?? "", StreetLeague.NicknameMaxLength, profileFieldStyle);
                    if (GUI.GetNameOfFocusedControl() != "ProfileNickname") GUI.FocusControl("ProfileNickname");
                }
                DrawProfilePillButton(ProfileSaveBounds(height), ProfileSaveAction, profileSaving ? "GUARDANDO…" : "GUARDAR", ProfileGreen, !profileSaving);
                DrawProfilePillButton(ProfileCancelEditBounds(height), ProfileCancelEditAction, "CANCELAR", new Color(.30f, .22f, .15f), !profileSaving);
            }
            else
            {
                string nickname = ProfileNickname;
                RankingOutlined(name, nickname ?? (ProfileSignedIn ? "SIN APODO" : "HINCHA"), 38, Color.white, MarketNavy, TextAnchor.MiddleLeft, 1.2f);
                Rect chip = new Rect(name.x, card.y + 104, 196, 32);
                RankingPill(chip, new Color(0f, .03f, .12f, .55f));
                DrawRankingStar(new Rect(chip.x + 8, chip.y + 6, 20, 20));
                MarketText(new Rect(chip.x + 32, chip.y, chip.width - 40, chip.height), ProfileNumber(StreetLeague.Points(stats)) + " PUNTOS DE LIGA", 14, Color.white, TextAnchor.MiddleLeft);
                if (ProfileSignedIn)
                {
                    DrawProfilePillButton(ProfileEditBounds(height), ProfileEditAction, nickname == null ? "ELEGIR" : "EDITAR", ProfileGreen, true);
                    if (GUI.Button(name, "", invisible)) NativeAction(ProfileEditAction);
                }
            }
            string note = profileNicknameError ?? (profileEditing ? "3 a 16 letras, números, espacio, punto o guion. Es el que ven en el Ranking." : null);
            if (note != null)
                MarketText(new Rect(card.x, card.yMax + 4, card.width, 22), note, 13, profileNicknameError != null ? new Color(1f, .58f, .48f) : new Color(.98f, .90f, .74f));
        }

        // Six cream tiles: matches, wins, courts, badges, products sold, customers served.
        private void DrawProfileStats(float height)
        {
            StreetPlayerStatistics stats = game?.PlayerStatistics;
            long sold = 0;
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++) sold += game.GetCumulativeProductSales(product);
            int badges = 0;
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    if (game.HasProductSalesBadge(product, tier)) badges++;
            string[] values = {
                ProfileNumber(stats != null ? stats.attemptsFinished : 0), ProfileNumber(stats != null ? stats.wins : 0),
                StreetLeague.WonLevels(stats) + "/" + StreetLeague.MaxWonLevels, badges + "/" + StreetSalesBadges.BadgeCount,
                ProfileNumber(sold), ProfileNumber(stats != null ? stats.customersAttended : 0) };
            string[] labels = { "PARTIDAS JUGADAS", "PARTIDAS GANADAS", "CANCHAS GANADAS", "INSIGNIAS", "PRODUCTOS VENDIDOS", "CLIENTES ATENDIDOS" };
            for (int i = 0; i < values.Length; i++)
            {
                Rect tile = ProfileTileBounds(height, i);
                DrawRankingRowBase(tile, RankingRowKind.Normal, 0f);
                RankingOutlined(new Rect(tile.x + 6, tile.y + 10, tile.width - 12, 46), values[i], 34, RankingGold, new Color(.24f, .10f, .02f));
                MarketText(new Rect(tile.x + 6, tile.y + 60, tile.width - 12, 24), labels[i], 12, MarketInk);
            }
        }

        // The Google account: its mail (only shown to its owner) and CERRAR SESIÓN, or a way to sign in.
        private void DrawProfileAccount(float height)
        {
            Rect objective = ProfileObjectiveBounds(height);
            if (objective.width > 0f)
            {
                DrawRankingRowBase(objective, RankingRowKind.Mine, 64f);
                DrawRankingStar(new Rect(objective.x + 13, objective.y + 12, 38, 38));
                MarketText(new Rect(objective.x + 76, objective.y + 6, objective.width - 90, 22), "TU PRÓXIMO OBJETIVO", 13, new Color(.10f, .30f, .06f), TextAnchor.MiddleLeft);
                RankingOutlined(new Rect(objective.x + 76, objective.y + 26, objective.width - 90, 28), RankingObjectiveText(out int _).Replace("PRÓXIMO OBJETIVO: ", ""), 20,
                    Color.white, new Color(.08f, .25f, .03f), TextAnchor.MiddleLeft);
            }
            Rect email = ProfileEmailBounds(height);
            MarketText(new Rect(email.x, email.y - 28, email.width, 24), "TU CUENTA DE GOOGLE", 16, RankingGold);
            DrawRankingRowBase(email, RankingRowKind.Normal, 0f);
            string mail = ProfileEmail;
            MarketText(new Rect(email.x + 14, email.y + 4, email.width - 28, email.height - 12),
                !string.IsNullOrEmpty(mail) ? mail : ProfileSignedIn ? "Cuenta de Google sin mail visible" : "No iniciaste sesión: tu progreso se guarda solo en este teléfono.",
                !string.IsNullOrEmpty(mail) ? 18 : 13, MarketInk);
            if (ProfileSignedIn) DrawProfilePillButton(ProfileSignOutBounds(height), ProfileSignOutAction, "CERRAR SESIÓN", ProfileRed, !profileConfirmSignOut && !profileSaving);
            else DrawProfilePillButton(ProfileSignOutBounds(height), ProfileSignInAction, "INICIAR SESIÓN", ProfileGreen, !profileConfirmSignOut);
        }

        private void DrawProfileSignOutConfirm(float height)
        {
            DrawIntroScreen(Texture2D.whiteTexture, new Color(0f, 0f, 0f, .55f), ScaleMode.StretchToFill);
            Rect panel = ProfileConfirmPanelBounds(height);
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .98f));
            RankingOutlined(new Rect(panel.x + 20, panel.y + 22, panel.width - 40, 44), "¿CERRAR SESIÓN?", 30, Color.white, MarketInk);
            MarketText(new Rect(panel.x + 24, panel.y + 76, panel.width - 48, 26), "Tu progreso queda guardado en tu cuenta.", 17, new Color(.98f, .90f, .74f));
            MarketText(new Rect(panel.x + 24, panel.y + 104, panel.width - 48, 26), "Para volver a jugar, iniciá sesión de nuevo.", 17, new Color(.98f, .90f, .74f));
            DrawProfilePillButton(ProfileConfirmYesBounds(height), ProfileConfirmSignOutAction, "SÍ, CERRAR", ProfileRed, true);
            DrawProfilePillButton(ProfileConfirmNoBounds(height), ProfileCancelSignOutAction, "CANCELAR", new Color(.15f, .45f, .86f), true);
        }

        // Rounded game-style button (dark rim, coloured face, top gloss, outlined caption) for colours the standard button lacks.
        private void DrawProfilePillButton(Rect bounds, int action, string caption, Color face, bool enabled)
        {
            bool pressed = enabled && pressedAction == action;
            Rect drawn = pressed ? new Rect(bounds.x, bounds.y + 2, bounds.width, bounds.height) : bounds;
            Color tint = enabled ? Color.white : new Color(.6f, .6f, .6f, .85f);
            Color dark = new Color(face.r * .38f, face.g * .38f, face.b * .38f, tint.a);
            RankingPill(new Rect(drawn.x, drawn.y + 3, drawn.width, drawn.height), new Color(0f, 0f, 0f, .35f * tint.a));
            RankingPill(drawn, dark);
            RankingPill(new Rect(drawn.x + 3, drawn.y + 3, drawn.width - 6, drawn.height - 6), face * tint);
            RankingPill(new Rect(drawn.x + 7, drawn.y + 5, drawn.width - 14, (drawn.height - 10) * .45f), new Color(1f, 1f, 1f, .22f * tint.a));
            RankingOutlined(drawn, caption, Mathf.RoundToInt(drawn.height * .42f), Color.white, dark);
            if (enabled && GUI.Button(bounds, "", invisible)) NativeAction(action);
        }

        // ---- Input ----
        private int HitProfileAction(Vector2 p)
        {
            float height = logicalCanvasHeight;
            if (profileConfirmSignOut)
            {
                if (ProfileConfirmYesBounds(height).Contains(p)) return ProfileConfirmSignOutAction;
                return ProfileConfirmNoBounds(height).Contains(p) ? ProfileCancelSignOutAction : 0;
            }
            if (profileEditing)
            {
                if (profileSaving) return 0;
                if (ProfileSaveBounds(height).Contains(p)) return ProfileSaveAction;
                if (ProfileCancelEditBounds(height).Contains(p)) return ProfileCancelEditAction;
            }
            else if (ProfileSignedIn && (ProfileEditBounds(height).Contains(p) || ProfileNameBounds(height).Contains(p))) return ProfileEditAction;
            if (ProfileSignOutBounds(height).Contains(p)) return ProfileSignedIn ? ProfileSignOutAction : ProfileSignInAction;
            return BadgeBackBounds(height).Contains(p) ? ProfileBackAction : 0;
        }

        private void DispatchProfileAction(int action)
        {
            switch (action)
            {
                case ProfileBackAction: if (!profileConfirmSignOut) { CloseProfile(); hubActive = true; } break;
                case ProfileEditAction:
                    if (!ProfileSignedIn) break;
                    profileEditing = true; profileNicknameInput = ProfileNickname ?? ""; profileNicknameError = null;
                    if (TouchScreenKeyboard.isSupported && !Application.isEditor) OpenProfileKeyboard();
                    break;
                case ProfileSaveAction: SubmitProfileNickname(); break;
                case ProfileCancelEditAction:
                    if (profileSaving) break;
                    profileEditing = false; profileNicknameError = null;
                    if (profileKeyboard != null) { profileKeyboard.active = false; profileKeyboard = null; }
                    break;
                case ProfileSignOutAction: if (ProfileSignedIn) profileConfirmSignOut = true; break;
                case ProfileCancelSignOutAction: profileConfirmSignOut = false; break;
                case ProfileConfirmSignOutAction:
                    profileConfirmSignOut = false;
                    CloseProfile(); hubActive = true;
                    ProfileSession?.SignOut();
                    break;
                case ProfileSignInAction: CloseProfile(); hubActive = true; ProfileSession?.OpenAccountMenu(); break;
            }
        }

        // Android: the native keyboard with its own input line, like the reference; "Aceptar" saves.
        private void OpenProfileKeyboard()
        {
            profileKeyboard = TouchScreenKeyboard.Open(profileNicknameInput ?? "", TouchScreenKeyboardType.Default, false, false, false, false,
                "Tu apodo", StreetLeague.NicknameMaxLength);
        }

        private void UpdateProfileKeyboard()
        {
            if (profileKeyboard == null) return;
            if (!profileActive || !profileEditing) { profileKeyboard.active = false; profileKeyboard = null; return; }
            if (profileKeyboard.text != null) profileNicknameInput = profileKeyboard.text;
            switch (profileKeyboard.status)
            {
                case TouchScreenKeyboard.Status.Done: profileKeyboard = null; SubmitProfileNickname(); break;
                case TouchScreenKeyboard.Status.Canceled: profileKeyboard = null; break;
                case TouchScreenKeyboard.Status.LostFocus: profileKeyboard = null; break;
            }
        }

        private async void SubmitProfileNickname()
        {
            if (profileSaving) return;
            if (!StreetLeague.TryNormalizeNickname(profileNicknameInput, out string nickname))
            {
                profileNicknameError = "3 a 16 letras, números, espacio, punto o guion.";
                return;
            }
            FirebasePlayerSession session = ProfileSession;
            if (session == null || !session.LeaderboardReady) { profileNicknameError = "Necesitás conexión y tu sesión de Google para cambiar el apodo."; return; }
            if (nickname == session.LeaderboardNickname) { profileEditing = false; profileNicknameError = null; return; }
            profileSaving = true; profileNicknameError = null;
            bool saved = await session.SaveLeaderboardNicknameAsync(nickname);
            if (this == null) return;
            profileSaving = false;
            if (saved) { profileEditing = false; profileNicknameError = null; }
            else profileNicknameError = "No pudimos guardar el apodo. Probá de nuevo.";
        }
    }
}
