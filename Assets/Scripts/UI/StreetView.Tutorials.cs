using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Tutoriales de nivel. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private Rect LevelOneTutorialPanelBounds() =>
            LevelOneTutorialPanelBounds(levelOneTutorialStep, LevelOneTutorialTargetBounds(), layoutVerticalScale);

        private static Rect LevelOneTutorialPanelBounds(int step, Rect target, float verticalScale)
        {
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = step == 0 ? target.center.x + 20f - width * .5f
                : step == 1 ? target.center.x + 30f
                : step == 2 ? target.center.x - width * .5f - 55f
                : target.center.x - width * .5f + 155f;
            x = Mathf.Clamp(x, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, verticalScale);
            float y = step < 2 ? target.yMax + 56f / vertical : target.yMin - 310f / vertical;
            return new Rect(x, y, width, LevelOneTutorialBubbleHeight);
        }

        private Rect LevelOneTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelOneTutorialPanelBounds(), layoutVerticalScale);
        private static Rect LevelOneTutorialPrimaryBounds(Rect panel, float verticalScale) =>
            new Rect(panel.center.x - 100f, panel.y + 126f / Mathf.Max(.01f, verticalScale), 200f, 58f);

        private Rect LevelOneTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelOneTutorialPanelBounds(), layoutVerticalScale);
        private static Rect LevelOneTutorialSkipBounds(Rect panel, float verticalScale) =>
            new Rect(panel.center.x - 42f, panel.y + 188f / Mathf.Max(.01f, verticalScale), 84f, 26f);

        private Rect LevelTwoCocacoleroTutorialPanelBounds()
        {
            Rect target = LevelTwoCocacoleroTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float y = levelTwoCocacoleroTutorialStep == 0
                ? target.yMax + 56f / vertical
                : target.yMin - 260f / vertical;
            return new Rect(x, y, width, LevelOneTutorialBubbleHeight);
        }
        private Rect LevelTwoCocacoleroTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelTwoCocacoleroTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelTwoCocacoleroTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelTwoCocacoleroTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelTwoCocacoleroTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            if (levelTwoCocacoleroTutorialStep == 1)
                return ResponsiveUpgradeCardBounds(game.Sim, HireCocacoleroAction);
            Rect gap = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            int count = Mathf.Min(game.Sim.ProductCount, hudQuotaProductIds.Length);
            for (int slot = 0; slot < count; slot++)
                hudQuotaProductIds[slot] = game.Sim.GetAvailableProduct(slot);
            hudQuotaProductCount = count;
            for (int slot = 0; slot < count; slot++)
                if (hudQuotaProductIds[slot] == 4)
                    return HudScreenRectToSafeCanvas(HudQuotaCellBounds(slot, hudQuotaProductIds, count, gap));
            return Rect.zero;
        }
        private void DrawLevelTwoCocacoleroTutorial()
        {
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelTwoCocacoleroTutorialPanelBounds();
            Rect target = LevelTwoCocacoleroTutorialTargetBounds();
            bool targetAbove = levelTwoCocacoleroTutorialStep == 0;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            const float tailWidth = 42f;
            float baseX = targetAbove ? target.center.x : panel.xMax - 30f;
            baseX = Mathf.Clamp(baseX, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, targetAbove ? target.yMax : target.yMin);
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelTwoCocacoleroTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) || !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelTwoCocacoleroTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headlineBounds = new Rect(panel.x + 18f, panel.y + 12f / vertical, panel.width - 36f, 58f);
            DrawTutorialText(headlineBounds, LevelTwoCocacoleroTutorialHeadlines[levelTwoCocacoleroTutorialStep],
                levelOneTutorialTitleStyle, 25, 20);
            Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 40f);
            DrawTutorialText(explanationBounds, LevelTwoCocacoleroTutorialExplanations[levelTwoCocacoleroTutorialStep],
                levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(LevelTwoCocacoleroTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = LevelTwoCocacoleroTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private Rect LevelThreePatyTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            Rect gap = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            int count = Mathf.Min(game.Sim.ProductCount, hudQuotaProductIds.Length);
            for (int slot = 0; slot < count; slot++)
                hudQuotaProductIds[slot] = game.Sim.GetAvailableProduct(slot);
            hudQuotaProductCount = count;
            for (int slot = 0; slot < count; slot++)
                if (hudQuotaProductIds[slot] == 1)
                    return HudScreenRectToSafeCanvas(HudQuotaCellBounds(slot, hudQuotaProductIds, count, gap));
            return Rect.zero;
        }
        private Rect LevelThreePatyTutorialPanelBounds()
        {
            Rect target = LevelThreePatyTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            return new Rect(x, target.yMax + 56f / vertical, width, LevelOneTutorialBubbleHeight);
        }
        private Rect LevelThreePatyTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelThreePatyTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelThreePatyTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelThreePatyTutorialPanelBounds(), layoutVerticalScale);
        private void DrawLevelThreePatyTutorial()
        {
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelThreePatyTutorialPanelBounds();
            Rect target = LevelThreePatyTutorialTargetBounds();
            const bool targetAbove = true;
            const float tailWidth = 42f;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float baseX = Mathf.Clamp(target.center.x, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, target.yMax);
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != 0 ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) || !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = 0;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headlineBounds = new Rect(panel.x + 18f, panel.y + 12f / vertical, panel.width - 36f, 58f);
            DrawTutorialText(headlineBounds, LevelThreePatyTutorialHeadline, levelOneTutorialTitleStyle, 25, 20);
            Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 40f);
            DrawTutorialText(explanationBounds, LevelThreePatyTutorialExplanation, levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(LevelThreePatyTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = LevelThreePatyTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private Rect LevelFourSpecialistsTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            int action = levelFourSpecialistsTutorialStep == 0 ? HirePremiumAction : HireCocacoleroAction;
            return ResponsiveUpgradeCardBounds(game.Sim, action);
        }
        private Rect LevelFourSpecialistsTutorialPanelBounds()
        {
            Rect target = LevelFourSpecialistsTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            return new Rect(x, target.yMin - 260f / vertical, width, LevelOneTutorialBubbleHeight);
        }
        private Rect LevelFourSpecialistsTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelFourSpecialistsTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelFourSpecialistsTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelFourSpecialistsTutorialPanelBounds(), layoutVerticalScale);
        private void DrawLevelFourSpecialistsTutorial()
        {
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelFourSpecialistsTutorialPanelBounds();
            Rect target = LevelFourSpecialistsTutorialTargetBounds();
            const bool targetAbove = false;
            const float tailWidth = 42f;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float baseX = Mathf.Clamp(target.center.x, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, target.yMin);
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelFourSpecialistsTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) || !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelFourSpecialistsTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headlineBounds = new Rect(panel.x + 18f, panel.y + 12f / vertical, panel.width - 36f, 58f);
            DrawTutorialText(headlineBounds, LevelFourSpecialistsTutorialHeadlines[levelFourSpecialistsTutorialStep],
                levelOneTutorialTitleStyle, 25, 20);
            Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 40f);
            DrawTutorialText(explanationBounds, LevelFourSpecialistsTutorialExplanations[levelFourSpecialistsTutorialStep],
                levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(LevelFourSpecialistsTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = LevelFourSpecialistsTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private Rect LevelFiveSpecialistsTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            int action = levelFiveSpecialistsTutorialStep == 0 ? HirePremiumAction : HireFerneteroAction;
            return ResponsiveUpgradeCardBounds(game.Sim, action);
        }
        private Rect LevelFiveSpecialistsTutorialPanelBounds()
        {
            Rect target = LevelFiveSpecialistsTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            return new Rect(x, target.yMin - 260f / vertical, width, LevelOneTutorialBubbleHeight);
        }
        private Rect LevelFiveSpecialistsTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelFiveSpecialistsTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelFiveSpecialistsTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelFiveSpecialistsTutorialPanelBounds(), layoutVerticalScale);
        private void DrawLevelFiveSpecialistsTutorial()
        {
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelFiveSpecialistsTutorialPanelBounds();
            Rect target = LevelFiveSpecialistsTutorialTargetBounds();
            const bool targetAbove = false;
            const float tailWidth = 42f;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float baseX = Mathf.Clamp(target.center.x, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, target.yMin);
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelFiveSpecialistsTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) || !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelFiveSpecialistsTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headlineBounds = new Rect(panel.x + 18f, panel.y + 12f / vertical, panel.width - 36f, 58f);
            DrawTutorialText(headlineBounds, LevelFiveSpecialistsTutorialHeadlines[levelFiveSpecialistsTutorialStep],
                levelOneTutorialTitleStyle, 25, 20);
            Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 40f);
            DrawTutorialText(explanationBounds, LevelFiveSpecialistsTutorialExplanations[levelFiveSpecialistsTutorialStep],
                levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(LevelFiveSpecialistsTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = LevelFiveSpecialistsTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private Rect LevelSixRacingTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            int action = levelSixRacingTutorialStep == 0 ? 2 : HireMozoAction;
            return ResponsiveUpgradeCardBounds(game.Sim, action);
        }
        private Rect LevelSixRacingTutorialPanelBounds()
        {
            Rect target = LevelSixRacingTutorialTargetBounds();
            float width = Mathf.Min(LevelOneTutorialBubbleWidth, W - 28f);
            float x = Mathf.Clamp(target.center.x - width * .5f, 14f, W - width - 14f);
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            return new Rect(x, target.yMin - 260f / vertical, width, LevelOneTutorialBubbleHeight);
        }
        private Rect LevelSixRacingTutorialPrimaryBounds() =>
            LevelOneTutorialPrimaryBounds(LevelSixRacingTutorialPanelBounds(), layoutVerticalScale);
        private Rect LevelSixRacingTutorialSkipBounds() =>
            LevelOneTutorialSkipBounds(LevelSixRacingTutorialPanelBounds(), layoutVerticalScale);
        private void DrawLevelSixRacingTutorial()
        {
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelSixRacingTutorialPanelBounds();
            Rect target = LevelSixRacingTutorialTargetBounds();
            const bool targetAbove = false;
            const float tailWidth = 42f;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float baseX = Mathf.Clamp(target.center.x, panel.x + tailWidth * .5f, panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, target.yMin);
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelSixRacingTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) || !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelSixRacingTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);
            Rect headlineBounds = new Rect(panel.x + 18f, panel.y + 12f / vertical, panel.width - 36f, 58f);
            DrawTutorialText(headlineBounds, LevelSixRacingTutorialHeadlines[levelSixRacingTutorialStep],
                levelOneTutorialTitleStyle, 25, 20);
            Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 40f);
            DrawTutorialText(explanationBounds, LevelSixRacingTutorialExplanations[levelSixRacingTutorialStep],
                levelOneTutorialBodyStyle, 19, 16);
            DrawStandardButton(LevelSixRacingTutorialPrimaryBounds(), LevelOneTutorialPrimaryAction, "SIGUIENTE");
            Rect skipBounds = LevelSixRacingTutorialSkipBounds();
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private string LevelOneTutorialPrimaryCaption() => "SIGUIENTE";
        private Rect LevelOneTutorialTargetBounds()
        {
            if (game == null || game.Sim == null) return Rect.zero;
            Rect gap = TopHudCutoutBounds(new Vector2(Screen.width, Screen.height), Screen.cutouts);
            if (levelOneTutorialStep == 0)
            {
                int count = Mathf.Min(game.Sim.ProductCount, hudQuotaProductIds.Length);
                for (int slot = 0; slot < count; slot++)
                    hudQuotaProductIds[slot] = game.Sim.GetAvailableProduct(slot);
                hudQuotaProductCount = count;
                return HudScreenRectToSafeCanvas(HudQuotaCellBounds(0, hudQuotaProductIds, count, gap));
            }
            if (levelOneTutorialStep == 1)
                return HudScreenRectToSafeCanvas(HudCoinIconBounds(gap));
            return ResponsiveUpgradeCardBounds(game.Sim, levelOneTutorialStep == 2 ? 2 : 3);
        }
        private Rect HudScreenRectToSafeCanvas(Rect screenLogicalBounds)
        {
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Rect viewport = CanvasViewport(screen, Screen.safeArea);
            if (viewport.width <= 0f || screenLogicalBounds.width <= 0f || screenLogicalBounds.height <= 0f) return Rect.zero;
            float screenScale = Screen.width / W;
            Vector2 topLeft = ScreenToLayoutPoint(
                new Vector2(screenLogicalBounds.x * screenScale, Screen.height - screenLogicalBounds.y * screenScale), screen, viewport);
            Vector2 bottomRight = ScreenToLayoutPoint(
                new Vector2(screenLogicalBounds.xMax * screenScale, Screen.height - screenLogicalBounds.yMax * screenScale), screen, viewport);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }
        private void DrawLevelOneTutorial()
        {
            // Dim the entire physical screen behind the guide (including the HUD and safe-area insets).
            // DrawIntroScreen restores the safe-area matrix; the bubble and controls below stay crisp.
            DrawIntroScreen(Texture2D.whiteTexture, LevelOneTutorialBackdropDimColor(), ScaleMode.StretchToFill);
            Rect panel = LevelOneTutorialPanelBounds();
            Rect target = LevelOneTutorialTargetBounds();
            bool targetAbove = levelOneTutorialStep < 2;
            float vertical = Mathf.Max(.01f, layoutVerticalScale);
            float tailWidth = levelOneTutorialStep == 0 ? 52f : levelOneTutorialStep == 1 ? 42f : 36f;
            float baseX = levelOneTutorialStep == 2 ? panel.xMax - 30f
                : levelOneTutorialStep == 3 ? panel.x + 30f : target.center.x;
            baseX = Mathf.Clamp(baseX, panel.x + tailWidth * .5f,
                panel.xMax - tailWidth * .5f);
            Vector2 tip = new Vector2(target.center.x, targetAbove ? target.yMax : target.yMin);
            // LayoutRect scales anchor positions (not element sizes) vertically. Rasterize the
            // complete silhouette in that same rendered coordinate space so the tail stays
            // attached while its gap remains constant across portrait heights.
            Rect bubblePanel = new Rect(panel.x, panel.y * vertical, panel.width, panel.height);
            Vector2 bubbleTip = new Vector2(tip.x, tip.y * vertical);
            Rect bubbleBounds = LevelOneTutorialBubbleDrawBounds(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
            if (levelOneTutorialBubbleTexture == null || levelOneTutorialBubbleTextureStep != levelOneTutorialStep ||
                !levelOneTutorialBubbleTexturePanel.Equals(bubblePanel) ||
                !levelOneTutorialBubbleTextureTip.Equals(bubbleTip) ||
                !Mathf.Approximately(levelOneTutorialBubbleTextureTailWidth, tailWidth) ||
                levelOneTutorialBubbleTextureTailAbove != targetAbove)
            {
                if (levelOneTutorialBubbleTexture != null) Destroy(levelOneTutorialBubbleTexture);
                levelOneTutorialBubbleTexture = CreateLevelOneTutorialBubbleTexture(bubblePanel, bubbleTip, baseX, tailWidth, targetAbove);
                levelOneTutorialBubbleTextureStep = levelOneTutorialStep;
                levelOneTutorialBubbleTexturePanel = bubblePanel;
                levelOneTutorialBubbleTextureTip = bubbleTip;
                levelOneTutorialBubbleTextureTailWidth = tailWidth;
                levelOneTutorialBubbleTextureTailAbove = targetAbove;
            }
            GUI.DrawTexture(bubbleBounds, levelOneTutorialBubbleTexture, ScaleMode.StretchToFill, true);

            string headline = LevelOneTutorialHeadlines[levelOneTutorialStep];
            string explanation = LevelOneTutorialExplanations[levelOneTutorialStep];
            Rect headlineBounds = new Rect(panel.x + 18f,
                panel.y + (levelOneTutorialStep == 1 ? 29f : 12f) / vertical,
                panel.width - 36f, levelOneTutorialStep == 1 ? 70f : 58f);
            DrawTutorialText(headlineBounds, headline, levelOneTutorialTitleStyle,
                levelOneTutorialStep == 2 ? 23 : 25, 20);
            if (!string.IsNullOrEmpty(explanation))
            {
                Rect explanationBounds = new Rect(panel.x + 18f, panel.y + 72f / vertical, panel.width - 36f, 35f);
                if (levelOneTutorialStep == 3) explanationBounds.height = 40f;
                DrawTutorialText(explanationBounds, explanation, levelOneTutorialBodyStyle, 20, 17);
            }

            DrawStandardButton(LevelOneTutorialPrimaryBounds(panel, layoutVerticalScale), LevelOneTutorialPrimaryAction,
                LevelOneTutorialPrimaryCaption());
            Rect skipBounds = LevelOneTutorialSkipBounds(panel, layoutVerticalScale);
            FillRect(skipBounds, new Color(1f, .98f, .94f, .94f));
            Rect skip = LayoutRect(skipBounds);
            GUI.Label(skip, "SALTAR", levelOneTutorialSkipStyle);
            if (GUI.Button(skip, "", invisible)) NativeAction(LevelOneTutorialSkipAction);
        }

        private void DrawTutorialText(Rect bounds, string value, GUIStyle style, int preferredSize, int minimumSize)
        {
            int previous = style.fontSize;
            try
            {
                style.fontSize = preferredSize;
                while (style.fontSize > minimumSize && style.CalcHeight(new GUIContent(value), bounds.width) > bounds.height)
                    style.fontSize--;
                Rect drawn = LayoutRect(bounds);
                if (style == levelOneTutorialTitleStyle || style == levelOneTutorialBodyStyle)
                {
                    const float weight = .55f;
                    GUI.Label(new Rect(drawn.x - weight, drawn.y, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x + weight, drawn.y, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x, drawn.y - weight, drawn.width, drawn.height), value, style);
                    GUI.Label(new Rect(drawn.x, drawn.y + weight, drawn.width, drawn.height), value, style);
                }
                GUI.Label(drawn, value, style);
            }
            finally { style.fontSize = previous; }
        }

        private static Rect LevelOneTutorialBubbleDrawBounds(Rect panel, Vector2 tip, float baseX, float tailWidth, bool tailAbove)
        {
            float baseY = tailAbove ? panel.y : panel.yMax;
            return Rect.MinMaxRect(
                Mathf.Min(panel.x, tip.x, baseX - tailWidth * .5f),
                Mathf.Min(panel.y, tip.y, baseY),
                Mathf.Max(panel.xMax, tip.x, baseX + tailWidth * .5f),
                Mathf.Max(panel.yMax, tip.y, baseY));
        }

        private static Texture2D CreateLevelOneTutorialBubbleTexture(Rect panel, Vector2 tip, float baseX,
            float tailWidth, bool tailAbove)
        {
            const float rasterScale = 3f;
            const float radius = 18f, border = 5f;
            Rect bounds = LevelOneTutorialBubbleDrawBounds(panel, tip, baseX, tailWidth, tailAbove);
            int width = Mathf.Max(1, Mathf.CeilToInt(bounds.width * rasterScale));
            int height = Mathf.Max(1, Mathf.CeilToInt(bounds.height * rasterScale));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "First match unified cream speech bubble and pointer",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Color outline = new Color(.22f, .075f, .025f, 1f);
            Color face = new Color(1f, .94f, .80f, 1f);
            float edgeY = tailAbove ? panel.y : panel.yMax;
            Vector2 baseCenter = new Vector2(baseX, edgeY);
            Vector2 leftBase = new Vector2(baseX - tailWidth * .5f, edgeY);
            Vector2 rightBase = new Vector2(baseX + tailWidth * .5f, edgeY);
            Rect innerPanel = Rect.MinMaxRect(panel.x + border, panel.y + border,
                panel.xMax - border, panel.yMax - border);
            float innerRadius = Mathf.Max(1f, radius - border);
            float innerTailWidth = Mathf.Max(4f, tailWidth - border * 2f);
            float innerBaseY = tailAbove ? panel.y + border : panel.yMax - border;
            Vector2 innerTip = Vector2.MoveTowards(tip, baseCenter, border);
            Vector2 innerLeftBase = new Vector2(baseX - innerTailWidth * .5f, innerBaseY);
            Vector2 innerRightBase = new Vector2(baseX + innerTailWidth * .5f, innerBaseY);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(bounds.x + (x + .5f) / rasterScale,
                    bounds.yMax - (y + .5f) / rasterScale);
                bool insideOuter = TutorialBubbleRoundedRectContains(point, panel, radius) ||
                    TutorialBubbleTriangleContains(point, tip, leftBase, rightBase);
                if (!insideOuter) continue;
                bool insideFace = TutorialBubbleRoundedRectContains(point, innerPanel, innerRadius) ||
                    TutorialBubbleTriangleContains(point, innerTip, innerLeftBase, innerRightBase);
                pixels[y * width + x] = insideFace ? face : outline;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static bool TutorialBubbleRoundedRectContains(Vector2 point, Rect bounds, float radius)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return false;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(bounds.width, bounds.height) * .5f);
            float qx = Mathf.Abs(point.x - bounds.center.x) - (bounds.width * .5f - radius);
            float qy = Mathf.Abs(point.y - bounds.center.y) - (bounds.height * .5f - radius);
            float outsideX = Mathf.Max(0f, qx), outsideY = Mathf.Max(0f, qy);
            float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return distance <= 0f;
        }

        private static bool TutorialBubbleTriangleContains(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float ab = (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
            float bc = (point.x - c.x) * (b.y - c.y) - (b.x - c.x) * (point.y - c.y);
            float ca = (point.x - a.x) * (c.y - a.y) - (c.x - a.x) * (point.y - a.y);
            bool negative = ab < 0f || bc < 0f || ca < 0f;
            bool positive = ab > 0f || bc > 0f || ca > 0f;
            return !(negative && positive);
        }
    }
}
