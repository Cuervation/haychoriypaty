using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Medallas, ranking y selector de niveles. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private static Rect BadgeBackBounds(float height) => new Rect(110, height - 80, 320, 60);
        private static Rect RankingBackBounds(float height) => new Rect(110, height - 80, 320, 60);
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
    }
}
