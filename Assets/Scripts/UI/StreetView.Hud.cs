using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // HUD de partida: contadores, cuotas y texturas de barra. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private static Rect TopHudCutoutBounds(Vector2 screenSize, Rect[] cutouts)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || cutouts == null || cutouts.Length == 0) return Rect.zero;
            float scale = screenSize.x / W;
            float xMin = W, xMax = 0f;
            bool found = false;
            for (int i = 0; i < cutouts.Length; i++)
            {
                Rect cutout = cutouts[i];
                if (cutout.width <= 0f || cutout.height <= 0f || cutout.yMax < screenSize.y - 1f) continue;
                float top = screenSize.y - cutout.yMax;
                if (top > CutoutHudGap.height * scale) continue;
                xMin = Mathf.Min(xMin, cutout.xMin / scale - 8f);
                xMax = Mathf.Max(xMax, cutout.xMax / scale + 8f);
                found = true;
            }
            if (!found) return Rect.zero;
            xMin = Mathf.Clamp(xMin, 0f, W);
            xMax = Mathf.Clamp(xMax, 0f, W);
            return xMax > xMin ? new Rect(xMin, 0f, xMax - xMin, CutoutHudGap.height) : Rect.zero;
        }

        private void EnsureHudBarTexture(Rect gap)
        {
            if (gap.width <= 0f || gap.height <= 0f)
            {
                if (hudBarTexture != null) return;
                hudBarTexture = CreateHudBarTexture(Rect.zero, false);
                return;
            }
            if (cachedHudCutoutGap == gap && cutoutHudBarTexture != null) return;
            if (cutoutHudBarTexture != null) Destroy(cutoutHudBarTexture);
            cutoutHudBarTexture = CreateHudBarTexture(gap, false);
            cachedHudCutoutGap = gap;
        }

        private void DrawCounters(StreetSimulation sim)
        {
            // Read-only status is pinned to physical y=0; only interactive controls use Screen.safeArea.
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float previousVertical = layoutVerticalScale;
            try
            {
                layoutVerticalScale = 1f;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                Rect cutoutGap = TopHudCutoutBounds(screenSize, Screen.cutouts);
                if (StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex))
                {
                    GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.width / W, 1f));
                    GUI.color = Color.white;
                    DrawTowerDefenseStatusHud(sim.Coins, sim.Elapsed);
                    return;
                }
                EnsureHudBarTexture(cutoutGap);
                hudQuotaProductCount = Mathf.Min(sim.ProductCount, hudQuotaProductIds.Length);
                for (int slot = 0; slot < hudQuotaProductCount; slot++)
                    hudQuotaProductIds[slot] = sim.GetAvailableProduct(slot);
                GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.width / W, 1f));
                GUI.color = Color.white;
                Rect compactStatus = Rect.zero;
                Rect compactQuotaBand = (sim.Phase == RoundPhase.Playing || VictoryTransitionActive)
                    ? CompactHudQuotaBand(cutoutGap, hudQuotaProductIds, hudQuotaProductCount, out compactStatus) : Rect.zero;
                if (compactStatus.width > 0f) DrawCompactHudStatus(sim, compactStatus);
                else
                {
                    Texture2D bar = cutoutGap.width > 0f ? cutoutHudBarTexture : hudBarTexture;
                    Rect barBounds = new Rect(0f, 0f, W, HudBarHeightFor(cutoutGap));
                    if (bar != null) GUI.DrawTexture(barBounds, bar, ScaleMode.StretchToFill, true);
                    Rect coinCell = HudStatusCellBounds(0, cutoutGap);
                    float statusScale = HudStatusScale(cutoutGap);
                    float coinIcon = Mathf.Min(HudStatusIconSize * statusScale, coinCell.height - HudChipInset * 2f);
                    float coinInset = HudStatusCardPadding * statusScale;
                    DrawHudIcon(new Rect(coinCell.x + coinInset, coinCell.center.y - coinIcon * .5f, coinIcon, coinIcon), 10);
                    DrawHudNumber(HudStatusTextBounds(0, cutoutGap), sim.Coins.ToString(),
                        Mathf.RoundToInt(HudStatusValueFontSize * statusScale));
                    DrawHudNumber(HudStatusTextBounds(1, cutoutGap), FormatRemainingTime(sim.TimeRemaining),
                        Mathf.RoundToInt(HudClockFontSize * statusScale));
                }
                int quotaFontSize = HudQuotaCounterFontSizeFor(sim, cutoutGap, compactQuotaBand);
                for (int slot = 0; slot < hudQuotaProductCount; slot++)
                    DrawQuotaChip(sim, slot, HudQuotaDisplayBounds(slot, hudQuotaProductIds, hudQuotaProductCount, cutoutGap, compactQuotaBand), quotaFontSize);
            }
            finally { GUI.matrix = previous; GUI.color = color; layoutVerticalScale = previousVertical; }
        }

        private static Rect HudStatusCellBounds(int slot, Rect cutoutGap)
        {
            if (slot < 0 || slot > 1) return Rect.zero;
            float width = (W - HudStatusCardPadding * 2f - HudStatusCardSpacing) * .5f;
            float x = slot == 0 ? HudStatusCardPadding : W - HudStatusCardPadding - width;
            float y = HudTopPadding;
            if (cutoutGap.width > 0f)
            {
                float leftWidth = cutoutGap.xMin - HudStatusCardPadding - HudCutoutClearance;
                float rightWidth = W - HudStatusCardPadding - cutoutGap.xMax - HudCutoutClearance;
                float safeWidth = Mathf.Min(width, Mathf.Min(leftWidth, rightWidth));
                // Small centered camera channels shrink both cards equally. For an unusually
                // large/off-center island, preserve readable cards by placing the row below it.
                if (safeWidth >= width * .55f)
                    width = Mathf.Max(0f, safeWidth);
                else
                    y = cutoutGap.yMax + HudCutoutClearance;
                x = slot == 0 ? HudStatusCardPadding : W - HudStatusCardPadding - width;
            }
            return new Rect(x, y, width, HudStateRowHeight);
        }

        private static float HudStatusScale(Rect cutoutGap)
        {
            float defaultWidth = HudStatusCellBounds(0, Rect.zero).width;
            if (defaultWidth <= 0f) return 1f;
            return Mathf.Clamp01(HudStatusCellBounds(0, cutoutGap).width / defaultWidth);
        }

        private static Rect HudCoinIconBounds(Rect cutoutGap)
        {
            Rect cell = HudStatusCellBounds(0, cutoutGap);
            float scale = HudStatusScale(cutoutGap);
            float size = Mathf.Min(HudStatusIconSize * scale, cell.height - HudChipInset * 2f);
            float inset = HudStatusCardPadding * scale;
            return new Rect(cell.x + inset, cell.center.y - size * .5f, size, size);
        }

        private static Rect HudStatusTextBounds(int slot, Rect cutoutGap)
        {
            if (slot < 0 || slot > 1) return Rect.zero;
            Rect cell = HudStatusCellBounds(slot, cutoutGap);
            float scale = HudStatusScale(cutoutGap);
            float iconSize = Mathf.Min(HudStatusIconSize * scale, cell.height - HudChipInset * 2f);
            float inset = HudStatusCardPadding * scale;
            float contentX = cell.x + inset + iconSize + HudStatusTextGap * scale;
            float contentRight = cell.xMax - inset;
            return new Rect(contentX, cell.y, Mathf.Max(0f, contentRight - contentX), cell.height);
        }

        private static readonly int[] DefaultHudProductIds = { 0, 1, 2, 3, 4, 6, 5 };
        private const float HudQuotaColumnInset = 8f;

        private static int HudQuotaColumnForProduct(int productId) =>
            productId >= 0 && productId < HudQuotaSolidProductCount ? 0 : 1;

        private static int HudQuotaCountInColumn(int[] productIds, int productCount, int column)
        {
            int count = 0;
            for (int i = 0; i < productCount; i++)
                if (HudQuotaColumnForProduct(productIds[i]) == column) count++;
            return count;
        }

        private static int HudQuotaRowsInColumn(int[] productIds, int productCount, int column)
        {
            int count = HudQuotaCountInColumn(productIds, productCount, column);
            return count <= 0 ? 0 : Mathf.CeilToInt(count / (float)HudQuotaColumnsPerGroup);
        }

        private static Rect HudQuotaGridBounds(Rect cutoutGap, int[] productIds, int productCount)
        {
            int solidRows = HudQuotaRowsInColumn(productIds, productCount, 0);
            int liquidRows = HudQuotaRowsInColumn(productIds, productCount, 1);
            int rows = Mathf.Max(solidRows, liquidRows);
            float height = rows <= 0 ? 0f : rows * HudQuotaCardHeight + (rows - 1) * HudObjectiveGap;
            return new Rect(HudQuotaColumnInset, HudBarHeightFor(cutoutGap) + HudObjectiveGap,
                W - HudQuotaColumnInset * 2f, height);
        }

        private static Rect HudQuotaGridBounds(Rect cutoutGap) =>
            HudQuotaGridBounds(cutoutGap, DefaultHudProductIds, DefaultHudProductIds.Length);

        private static Rect HudQuotaGridBounds(Rect cutoutGap, int productCount) =>
            HudQuotaGridBounds(cutoutGap, DefaultHudProductIds, Mathf.Clamp(productCount, 0, DefaultHudProductIds.Length));

        private static float HudBarHeightFor(Rect cutoutGap)
        {
            float statusBottom = Mathf.Max(HudStatusCellBounds(0, cutoutGap).yMax,
                HudStatusCellBounds(1, cutoutGap).yMax);
            return statusBottom + HudBottomPadding;
        }

        private static Rect HudQuotaCellBounds(int slot, int[] productIds, int productCount, Rect cutoutGap)
        {
            if (productIds == null || slot < 0 || slot >= productCount || slot >= productIds.Length) return Rect.zero;
            int productId = productIds[slot];
            int column = HudQuotaColumnForProduct(productId);
            int groupIndex = 0;
            for (int i = 0; i < slot; i++)
                if (HudQuotaColumnForProduct(productIds[i]) == column) groupIndex++;
            float availableWidth = W - HudQuotaColumnInset * 2f;
            float width = Mathf.Min(HudQuotaCardWidth, (availableWidth - HudQuotaColumnSpacing) / HudQuotaColumnsPerGroup);
            float groupWidth = width * HudQuotaColumnsPerGroup + HudQuotaColumnSpacing;
            float groupX = column == 0 ? HudQuotaColumnInset : W - HudQuotaColumnInset - groupWidth;
            float x = groupX + groupIndex % HudQuotaColumnsPerGroup * (width + HudQuotaColumnSpacing);
            int row = groupIndex / HudQuotaColumnsPerGroup;
            float y = HudBarHeightFor(cutoutGap) + HudObjectiveGap + row * (HudQuotaCardHeight + HudObjectiveGap);
            return new Rect(x, y, width, HudQuotaCardHeight);
        }

        private static Rect HudQuotaCellBounds(int slot, Rect cutoutGap) =>
            HudQuotaCellBounds(slot, DefaultHudProductIds, DefaultHudProductIds.Length, cutoutGap);

        private static Rect HudQuotaCellBounds(int slot) => HudQuotaCellBounds(slot, Rect.zero);

        private static Rect HudQuotaCellBounds(int slot, int productCount, Rect cutoutGap) =>
            HudQuotaCellBounds(slot, DefaultHudProductIds, Mathf.Clamp(productCount, 0, DefaultHudProductIds.Length), cutoutGap);

        private static Rect CompactHudQuotaBand(Rect cutoutGap, int[] products, int count)
            => CompactHudQuotaBand(cutoutGap, products, count, out _);

        private static Rect CompactHudQuotaBand(Rect cutoutGap, int[] products, int count, out Rect status)
        {
            status = Rect.zero;
            Vector2 size = new Vector2(Screen.width, Screen.height);
            Rect safe = CanvasViewport(size, Screen.safeArea);
            float height = CanvasLogicalHeight(safe);
            if (count <= 0 || size.x <= 0f || height >= 958f) return Rect.zero;
            float safeScale = safe.width / W, screenScale = size.x / W;
            float queueScale = height / H;
            float counter = StreetSceneLayout.CounterTopInViewport(size.x, size.y, safe);
            // DrawWaitingCrowd rounds to the nearest visual row. Reserve the tallest
            // bubble even at the rear edge of the second row while a customer advances.
            float clientY = StreetSimulation.FrontQueueY - 1.5f * StreetSimulation.QueueRowSpacing
                + StreetSceneLayout.CustomerOffset(counter, queueScale);
            Rect backBubble = OrderBubbleLayoutBounds(OrderBubbleBounds(new Vector2(0f, clientY),
                StreetCustomer.MaxVisibleOrderProducts + 1), queueScale);
            float firstBubbleTop = (safe.y + backBubble.yMin * safeScale) / screenScale;
            const float clearance = 2f, bandHeight = 28f;
            if (HudQuotaGridBounds(cutoutGap, products, count).yMax + clearance <= firstBubbleTop) return Rect.zero;
            float top = HudBarHeightFor(cutoutGap) + HudObjectiveGap;
            // Prefer the existing HUD edge. If even one row would enter the queue, use
            // the counter face immediately below the crowd clip, not the customer lane.
            if (top + bandHeight + clearance > firstBubbleTop)
                top = (safe.y + counter * safeScale) / screenScale + clearance;
            Rect band = new Rect(safe.xMin / screenScale + HudQuotaColumnInset, top,
                safe.width / screenScale - HudQuotaColumnInset * 2f, bandHeight);
            // In the square/very short case the old full-width status bar also covered
            // rear orders. Keep both readings at the left of the same counter strip.
            if (firstBubbleTop < HudBarHeightFor(cutoutGap))
            {
                status = new Rect(band.x, band.y, Mathf.Min(100f, band.width * .21f), band.height);
                band.xMin = status.xMax + 4f;
            }
            return band;
        }

        private static Rect CompactHudCoinSlot(Rect status) => new Rect(status.x + 3f, status.center.y - 6f, 12f, 12f);

        private Rect HudDisplayedCoinBounds(Rect cutoutGap)
        {
            if (game.Sim.Phase != RoundPhase.Playing) return HudCoinIconBounds(cutoutGap);
            CompactHudQuotaBand(cutoutGap, hudQuotaProductIds, hudQuotaProductCount, out Rect status);
            return status.width > 0f ? CompactHudCoinSlot(status) : HudCoinIconBounds(cutoutGap);
        }

        private void DrawCompactHudStatus(StreetSimulation sim, Rect status)
        {
            FillRect(status, HudQuotaCardColor(false));
            DrawHudIcon(CompactHudCoinSlot(status), 10);
            float half = status.width * .5f;
            DrawHudNumber(new Rect(status.x + 16f, status.y, half - 16f, status.height), sim.Coins.ToString(),
                14, TextAnchor.MiddleCenter, 8);
            FillRect(new Rect(status.x + half, status.y + 4f, 1f, status.height - 8f), new Color(.45f, .88f, 1f, .65f));
            DrawHudNumber(new Rect(status.x + half + 1f, status.y, half - 1f, status.height),
                FormatRemainingTime(sim.TimeRemaining), 14, TextAnchor.MiddleCenter, 8);
        }

        private Rect HudQuotaDisplayBounds(int slot, int[] products, int count, Rect cutoutGap) =>
            HudQuotaDisplayBounds(slot, products, count, cutoutGap,
                game.Sim.Phase == RoundPhase.Playing ? CompactHudQuotaBand(cutoutGap, products, count) : Rect.zero);

        private static Rect HudQuotaDisplayBounds(int slot, int[] products, int count, Rect cutoutGap, Rect compactBand)
        {
            if (compactBand.width <= 0f) return HudQuotaCellBounds(slot, products, count, cutoutGap);
            if (slot < 0 || slot >= count) return Rect.zero;
            const float gap = 4f;
            float width = Mathf.Min(HudQuotaCardWidth, (compactBand.width - gap * (count - 1)) / count);
            float left = compactBand.center.x - (width * count + gap * (count - 1)) * .5f;
            return new Rect(left + slot * (width + gap), compactBand.y, width, compactBand.height);
        }

        private static Color HudQuotaCardColor(bool complete) =>
            complete ? new Color(.025f, .34f, .40f, .65f) : new Color(.025f, .09f, .24f, .65f);

        private int HudQuotaCounterFontSizeFor(StreetSimulation sim, Rect cutoutGap, Rect compactQuotaBand)
        {
            int sharedFontSize = HudQuotaCounterFontSize;
            for (int slot = 0; slot < hudQuotaProductCount; slot++)
            {
                Rect card = HudQuotaDisplayBounds(slot, hudQuotaProductIds, hudQuotaProductCount, cutoutGap, compactQuotaBand);
                if (card.width <= 0f || card.height <= 0f) continue;
                int product = sim.GetAvailableProduct(slot);
                float iconSize = QuotaIconSize(card);
                Rect icon = new Rect(card.x + HudQuotaCardInset, card.center.y - iconSize * .5f, iconSize, iconSize);
                Rect counter = HudQuotaCounterBounds(card, icon);
                string value = sim.GetProductDelivered(product) + "/" + sim.GetProductGoal(product);
                sharedFontSize = Mathf.Min(sharedFontSize,
                    HudNumberFitFontSizeWithMinimum(hudNumber, counter, value, HudQuotaCounterFontSize,
                        card.width < 72f ? 8 : 12));
            }
            return sharedFontSize;
        }

        private void DrawQuotaChip(StreetSimulation sim, int slot, Rect bounds, int sharedFontSize)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            int product = sim.GetAvailableProduct(slot);
            int goal = sim.GetProductGoal(product);
            int delivered = sim.GetProductDelivered(product);
            bool complete = sim.IsProductGoalComplete(product);
            Color previous = GUI.color;
            if (hudQuotaCardTexture != null)
            {
                GUI.color = HudQuotaCardColor(complete);
                GUI.DrawTexture(bounds, hudQuotaCardTexture, ScaleMode.StretchToFill, true);
                if (hudQuotaCardOutlineTexture != null)
                {
                    GUI.color = new Color(.45f, .88f, 1f, .65f);
                    GUI.DrawTexture(bounds, hudQuotaCardOutlineTexture, ScaleMode.StretchToFill, true);
                }
            }
            else FillRect(bounds, HudQuotaCardColor(complete));
            GUI.color = previous;
            float iconSize = QuotaIconSize(bounds);
            Rect icon = new Rect(bounds.x + HudQuotaCardInset, bounds.center.y - iconSize * .5f, iconSize, iconSize);
            DrawHudIcon(icon, product);
            Rect counter = HudQuotaCounterBounds(bounds, icon);
            DrawHudNumber(counter, delivered + "/" + goal, sharedFontSize, HudQuotaCounterTextAlignment,
                bounds.width < 72f ? 8 : 12);
        }
        private static float QuotaIconSize(Rect card)
        {
            float height = Mathf.Min(HudQuotaIconMaxSize, card.height - HudQuotaCardInset * 2f);
            return card.width < 72f ? Mathf.Min(height, card.width * .25f) : height;
        }
        private static Rect HudQuotaCounterBounds(Rect card, Rect icon)
        {
            float x = icon.xMax + HudQuotaCardInset;
            float verticalInset = HudChipTextHeightInset * HudQuotaCardEnlargement * .5f;
            return new Rect(x, card.y + verticalInset,
                Mathf.Max(0f, card.xMax - HudQuotaCounterRightInset - x),
                Mathf.Max(0f, card.height - verticalInset * 2f));
        }
        private void DrawHudIcon(Rect slot, int itemId)
        {
            Texture2D texture = itemId == 10 && gameCoin != null ? gameCoin
                : itemId == 4 && cocaBottle != null ? cocaBottle : items;
            if (texture == null) return;
            Rect source = texture == items ? Items[itemId] : new Rect(0, 0, texture.width, texture.height);
            Draw(HudIconBounds(slot, source.size), texture, source, true, false);
        }
        private static Rect HudIconBounds(Rect slot, Vector2 sourceSize)
        {
            float scale = Mathf.Min(slot.width / sourceSize.x, slot.height / sourceSize.y);
            Vector2 size = sourceSize * scale;
            return new Rect(slot.center - size * .5f, size);
        }
        private static string FormatElapsedTime(float elapsed)
        {
            int seconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
        private static string FormatRemainingTime(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
        private static int HudNumberFitFontSize(GUIStyle style, Rect bounds, string value, int preferredFontSize) =>
            HudNumberFitFontSizeWithMinimum(style, bounds, value, preferredFontSize, 12);

        private static int HudNumberFitFontSizeWithMinimum(GUIStyle style, Rect bounds, string value, int preferredFontSize, int minimum)
        {
            int originalSize = style.fontSize;
            try
            {
                int baseFontSize = Mathf.Max(minimum, preferredFontSize);
                style.fontSize = baseFontSize;
                Vector2 needed = CachedCalcSize(style, value);
                float fitWidth = (bounds.width - 4f) / Mathf.Max(1f, needed.x);
                float fitHeight = (bounds.height - 2f) / Mathf.Max(1f, needed.y);
                float fitScale = Mathf.Min(1f, fitWidth, fitHeight);
                return fitScale < 1f
                    ? Mathf.Max(minimum, Mathf.FloorToInt(baseFontSize * fitScale))
                    : baseFontSize;
            }
            finally { style.fontSize = originalSize; }
        }

        private void DrawHudNumber(Rect bounds, string value, int preferredFontSize = 40,
            TextAnchor alignment = TextAnchor.MiddleCenter, int minimum = 12)
        {
            int originalSize = hudNumber.fontSize;
            TextAnchor originalAlignment = hudNumber.alignment;
            Color originalTextColor = hudNumber.normal.textColor;
            hudNumber.alignment = alignment;
            hudNumber.fontSize = HudNumberFitFontSizeWithMinimum(hudNumber, bounds, value, preferredFontSize, minimum);
            hudNumber.normal.textColor = new Color(.005f, .025f, .11f);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    Label(new Rect(bounds.x + x, bounds.y + y, bounds.width, bounds.height), value, hudNumber);
            hudNumber.normal.textColor = Color.white;
            Label(bounds, value, hudNumber);
            hudNumber.fontSize = originalSize;
            hudNumber.alignment = originalAlignment;
            hudNumber.normal.textColor = originalTextColor;
        }
        private static Texture2D CreateHudBar() => CreateHudBarTexture(false);
        private static Texture2D CreateHudBarTexture(bool cutout)
            => CreateHudBarTexture(cutout ? CutoutHudGap : Rect.zero, false);

        private static Texture2D CreateHudBarTexture(Rect reservedHudGap, bool keepReadable)
        {
            // Original code-authored artwork: no reference pixels or baked-in live numbers.
            const int width = 1080;
            int height = Mathf.CeilToInt(HudBarHeightFor(reservedHudGap) * 2f);
            float logicalHeight = height * .5f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado blue gameplay HUD",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var shadow = new Rect(0, 5, W, logicalHeight - 5f);
            var rim = new Rect(0, 0, W, logicalHeight);
            var face = new Rect(2, 2, W - 4f, logicalHeight - 4f);
            Rect[] stateCells = {
                HudStatusCellBounds(0, reservedHudGap),
                HudStatusCellBounds(1, reservedHudGap)
            };
            Color[] capsuleTops = { new Color(.30f, .86f, 1f), new Color(.30f, .86f, 1f) };
            Color[] capsuleBottoms = { new Color(.02f, .22f, .72f), new Color(.015f, .20f, .66f) };
            Rect clockCell = stateCells[1];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) / 2f, py = (height - y - .5f) / 2f;
                Color color = new Color(.008f, .025f, .12f, .58f * RoundedCoverage(px, py, shadow, 22));
                color = Over(color, new Color(.012f, .045f, .17f), RoundedCoverage(px, py, rim, 22));
                color = Over(color, Color.Lerp(new Color(.10f, .47f, .90f), new Color(.025f, .14f, .48f),
                    Mathf.Clamp01(py / Mathf.Max(1f, logicalHeight))), RoundedCoverage(px, py, face, 20));
                for (int i = 0; i < stateCells.Length; i++)
                    color = HudCapsule(color, px, py, stateCells[i], capsuleTops[i], capsuleBottoms[i]);
                // Reuse the original vector clock motif in the centered time capsule.
                float clockScale = Mathf.Min(1f, clockCell.height / 28f, HudStatusScale(reservedHudGap));
                float dx = (px - (clockCell.x + 17f)) / clockScale;
                float dy = (py - clockCell.center.y) / clockScale;
                float radius = new Vector2(dx, dy).magnitude;
                color = Over(color, new Color(.01f, .045f, .17f), Mathf.Clamp01(10.5f - radius));
                color = Over(color, new Color(.42f, .86f, 1f), Mathf.Clamp01(9.3f - radius));
                color = Over(color, new Color(.92f, .99f, 1f), Mathf.Clamp01(7.8f - radius));
                bool minuteHand = Mathf.Abs(dx) < .8f && dy >= -5.5f && dy <= .8f;
                bool hourHand = dx >= -.5f && dx <= 4.8f && Mathf.Abs(dy - dx * .5f) < .9f;
                if (minuteHand || hourHand) color = new Color(.015f, .055f, .18f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private static Texture2D CreateHudQuotaCardTexture(bool outline)
        {
            const int width = 192, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = outline ? "Translucent objective card rim" : "Translucent objective card face",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            Rect outer = new Rect(1f, 1f, width - 2f, height - 2f);
            Rect inner = new Rect(3f, 3f, width - 6f, height - 6f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float outerCoverage = RoundedCoverage(px, py, outer, 12f);
                float innerCoverage = RoundedCoverage(px, py, inner, 9f);
                float coverage = outline ? Mathf.Max(0f, outerCoverage - innerCoverage) : outerCoverage;
                // Coverage supplies only the rounded silhouette; GUI.color applies the requested 65% opacity.
                pixels[y * width + x] = new Color(1f, 1f, 1f, coverage);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateGameplayFooterTexture()
        {
            const int width = 256, height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado blue gameplay footer",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float vertical = (height - y - .5f) / height;
                float horizontal = Mathf.Sin(x / (float)(width - 1) * Mathf.PI * 2f) * .012f;
                Color color = Color.Lerp(new Color(.012f, .075f, .23f), new Color(.025f, .29f, .62f), vertical);
                color.r = Mathf.Clamp01(color.r + horizontal);
                float upperGloss = .22f * (1f - IntroEase((vertical - .72f) / .25f));
                color = Color.Lerp(color, new Color(.36f, .85f, 1f), upperGloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Color HudCapsule(Color below, float x, float y, Rect bounds, Color top, Color bottom)
        {
            float radius = Mathf.Min(15f, bounds.height * .5f);
            Color color = Over(below, new Color(.008f, .04f, .16f), RoundedCoverage(x, y, bounds, radius));
            var inside = new Rect(bounds.x + 1.5f, bounds.y + 1.5f, bounds.width - 3, bounds.height - 3);
            float coverage = RoundedCoverage(x, y, inside, Mathf.Max(1f, radius - 1.5f));
            float t = Mathf.Clamp01((y - inside.y) / inside.height);
            color = Over(color, Color.Lerp(top, bottom, t), coverage);
            return Over(color, Color.white, coverage * .28f * (1f - IntroEase(t / .5f)));
        }
    }
}
