using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Medallas, ranking y selector de niveles. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private static Rect BadgeBackBounds(float height) => new Rect(110, height - 80, 320, 60);
        private static Rect RankingBackBounds(float height) => new Rect(110, height - 80, 320, 60);
        private static Rect SalesBadgeRowBounds(int product, float height)
        {
            float rowHeight = Mathf.Min(108f, (height - 280f) / StreetSalesBadges.ProductCount - 6f);
            return new Rect(20, 169f + product * (rowHeight + 6f), W - 40, rowHeight);
        }
        private void DrawSalesMedal(Rect bounds, long sales, int tier, bool earned)
        {
            long target = StreetSalesBadges.Threshold(tier);
            Color metal = tier == 0 ? new Color(.84f, .49f, .24f) : tier == 1
                ? new Color(.75f, .82f, .88f) : new Color(1f, .77f, .18f);
            FillRect(bounds, earned ? metal : new Color(.66f, .64f, .59f));
            Rect face = new Rect(bounds.x + 2, bounds.y + 2, bounds.width - 4, bounds.height - 4);
            FillRect(face, earned ? new Color(.98f, .92f, .72f) : new Color(.85f, .83f, .77f));
            string name = tier == 0 ? "BRONCE" : tier == 1 ? "PLATA" : "ORO";
            MarketText(new Rect(face.x, face.y + 1, face.width, 18), name, 13, MarketInk);
            MarketText(new Rect(face.x, face.y + 20, face.width, 20), target.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")), 18, MarketInk);
            float trackY = face.yMax - 25;
            Rect track = new Rect(face.x + 6, trackY, face.width - 12, 4);
            FillRect(track, new Color(.36f, .31f, .24f));
            float progress = earned ? 1f : (float)System.Math.Min(1d, System.Math.Max(0d, (double)sales / target));
            FillRect(new Rect(track.x, track.y, track.width * progress, track.height), earned ? new Color(.07f, .48f, .16f) : metal);
            MarketText(new Rect(face.x, face.yMax - 20, face.width, 18), earned ? "CONSEGUIDA" : "EN PROGRESO", 12,
                earned ? new Color(.03f, .29f, .08f) : MarketInk);
        }
        private void DrawBadgesScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .68f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            DrawMarketWoodPanel(new Rect(14, 14, W - 28, height - 28), new Color(.11f, .065f, .036f, .96f));
            MarketText(new Rect(28, 28, W - 56, 40), "INSIGNIAS DE LA PARRILLA", 29, new Color(1f, .84f, .38f));
            MarketText(new Rect(28, 69, W - 56, 24), "Vendé 500, 2.500 y 5.000 de cada producto", 17, Color.white);
            int earned = 0;
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    if (game.HasProductSalesBadge(product, tier)) earned++;
            MarketText(new Rect(28, 97, W - 56, 24), earned + " / " + StreetSalesBadges.BadgeCount + " INSIGNIAS CONSEGUIDAS", 17, new Color(.52f, 1f, .47f));
            string sync = Cloud.FirebasePlayerSession.Instance != null
                ? Cloud.FirebasePlayerSession.Instance.ProgressSyncStatus : "Progreso local de prueba";
            MarketText(new Rect(28, 126, W - 56, 22), sync, 14, Color.white);
            string[] names = { "CHORI", "PATY", "BONDIOLA", "VACÍO", "COCA", "FERNET", "CERVEZA" };
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
            {
                long sales = game.GetCumulativeProductSales(product);
                Rect row = SalesBadgeRowBounds(product, height);
                DrawMarketWoodPanel(row, new Color(.93f, .86f, .68f));
                DrawProductIcon(new Rect(row.x + 10, row.y + 8, 46, row.height - 16), product);
                MarketText(new Rect(row.x + 58, row.y + 12, 120, 22), names[product], 17, MarketInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(row.x + 58, row.y + 37, 120, 20), sales.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-AR")) + " vendidas", 13, MarketInk, TextAnchor.MiddleLeft);
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    DrawSalesMedal(new Rect(row.x + 185 + tier * 100, row.y + 7, 94, row.height - 14), sales, tier, game.HasProductSalesBadge(product, tier));
            }
            string coverage = game.HasIncompleteSalesHistory ? "Historial antiguo parcial: contamos las ventas registradas."
                : "Ventas acumuladas al cerrar cada resultado.";
            MarketText(new Rect(28, height - 109, W - 56, 22), coverage, 13, Color.white);
            DrawStandardButton(BadgeBackBounds(height), BadgeBackAction, "VOLVER AL BARRIO");
        }
        private string RankingObjectiveText(out int unlockedCount)
        {
            unlockedCount = 0;
            int firstLocked = -1, firstWithoutWin = -1;
            for (int i = 0; i < StreetSimulation.CampaignOrder.Length; i++)
            {
                int levelId = StreetSimulation.CampaignLevelAt(i);
                if (game.IsLevelUnlocked(levelId)) unlockedCount++;
                else if (firstLocked < 0) firstLocked = levelId;
                if (firstWithoutWin < 0 && !game.HasWonLevel(levelId)) firstWithoutWin = levelId;
            }
            int nextLevel = firstLocked >= 0 ? firstLocked : firstWithoutWin;
            return nextLevel < 0 ? "TODAS LAS CANCHAS SUPERADAS"
                : "PRÓXIMO OBJETIVO: " + StreetSimulation.LevelDisplayNames[nextLevel].Split('/')[0].Trim().ToUpperInvariant();
        }
        private string levelSelectorStartError;

        private void DrawLevelSelector()
        {
            Rect viewport = LevelSelectorViewportBounds(logicalCanvasHeight);
            DrawLevelSelectorBackdrop();
            DrawLevelSelectorHeading();
            if (!string.IsNullOrEmpty(levelSelectorStartError))
            {
                // The mural strip above the title is outside the scroll area and all tap targets.
                Rect notice = new Rect(22f, 23f, W - 44f, 46f);
                DrawMarketWoodPanel(notice, new Color(.25f, .09f, .045f, .97f));
                MarketText(new Rect(notice.x + 12f, notice.y, notice.width - 24f, notice.height),
                    levelSelectorStartError, 19, Color.white);
            }
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                Vector2 point = ScreenToLayoutPoint(evt.mousePosition, new Vector2(Screen.width, Screen.height),
                    CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea));
                point.y *= logicalCanvasHeight / H;
                if (viewport.Contains(point))
                {
                    levelSelectScroll = Mathf.Clamp(levelSelectScroll + evt.delta.y * 28f, 0f,
                        LevelSelectorMaxScroll(logicalCanvasHeight));
                    evt.Use();
                }
            }
            levelSelectScroll = Mathf.Clamp(levelSelectScroll, 0f, LevelSelectorMaxScroll(logicalCanvasHeight));
            GUI.BeginGroup(LayoutRect(viewport));
            try
            {
                for (int i = 0; i < StreetSimulation.LevelNames.Length; i++)
                {
                    Rect card = LevelSelectCardBounds(i);
                    if (card.yMax <= viewport.y + levelSelectScroll ||
                        card.y >= viewport.yMax + levelSelectScroll) continue;
                    card.x -= viewport.x;
                    card.y -= viewport.y + levelSelectScroll;
                    DrawLevelSelectCard(i, card);
                }
            }
            finally { GUI.EndGroup(); }
            DrawLevelSelectorScrollBar();
            DrawStandardButton(LevelSelectorBackBounds(logicalCanvasHeight), LevelSelectorBackAction, "VOLVER");
        }
        private static float LevelSelectorContentBottom(int levelCount)
        {
            int rows = Mathf.CeilToInt(Mathf.Max(0, levelCount) / 2f);
            return rows <= 0 ? 0f : 160f + rows * LevelSelectorCardHeight + (rows - 1) * LevelSelectorCardGap;
        }
        private static float LevelSelectorMaxScroll(float canvasHeight) =>
            Mathf.Max(0f, LevelSelectorContentBottom(StreetSimulation.LevelNames.Length) -
                LevelSelectorViewportBounds(canvasHeight).yMax);

        private void DrawLevelSelectorScrollBar()
        {
            float max = LevelSelectorMaxScroll(logicalCanvasHeight);
            if (max <= 0f) return;
            Rect viewport = LevelSelectorViewportBounds(logicalCanvasHeight);
            Rect track = new Rect(W - 13f, viewport.y, 5f, viewport.height);
            FillRect(track, new Color(.12f, .10f, .075f, .52f));
            float thumbHeight = Mathf.Max(30f, track.height * track.height / (track.height + max));
            FillRect(new Rect(track.x, track.y + (track.height - thumbHeight) * levelSelectScroll / max,
                track.width, thumbHeight), new Color(1f, .95f, .79f, .97f));
        }

        private static Rect LevelSelectCardBounds(int index)
        {
            const float width = 244f;
            int row = index / 2, column = index % 2;
            float x = index == StreetSimulation.LevelNames.Length - 1 && column == 0
                ? (W - width) * .5f : 20f + column * 256f;
            return new Rect(x, 160f + row * (LevelSelectorCardHeight + LevelSelectorCardGap),
                width, LevelSelectorCardHeight);
        }
        private void DrawLevelSelectCard(int index, Rect baseBounds)
        {
            DrawLevelSelectorCardPresentation(index, baseBounds);
        }
        private static Color LevelSelectAccent(int levelIndex)
        {
            switch (levelIndex)
            {
                case 2: return new Color(.08f, .27f, .59f); // Vélez: blue/white
                case 3: return new Color(.04f, .38f, .19f); // Ferro: green/white
                default: return new Color(.79f, .035f, .06f); // Independiente: red/white
            }
        }
    }
}
