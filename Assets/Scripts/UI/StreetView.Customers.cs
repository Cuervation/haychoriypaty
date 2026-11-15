using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Clientes, burbujas de pedido y hinchas por club. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void DrawWaitingCrowd(StreetSimulation sim)
        {
            // Clip the crowd at the shared counter edge; custom later-level backdrops draw the counter after it.
            // Workers, products and sale effects stay on the player side and outside this group.
            GUI.BeginGroup(new Rect(0, 0, W, CustomerCounterTopY()));
            try
            {
                for (int row = 4; row >= 0; row--)
                for (int i = 0; i < sim.Customers.Count; i++)
                {
                    StreetCustomer customer = sim.Customers[i];
                    if (Mathf.Clamp(Mathf.RoundToInt((StreetSimulation.FrontQueueY - customer.Position.y)
                        / StreetSimulation.QueueRowSpacing), 0, 4) != row) continue;
                    DrawCustomer(customer, ShouldDrawOrderBubble(row));
                }
            }
            finally { GUI.EndGroup(); }
        }
        private float CustomerCounterTopY()
        {
            return projectedCounterTopY;
        }
        private static float CounterSurfaceY(float canvasHeight, Vector2 backdropSize, int levelIndex)
        {
            // Legacy private test wrapper; neither club nor replacement texture dimensions control perspective.
            return StreetSceneLayout.CounterTop(canvasHeight);
        }
        private static float CustomerViewOffsetY(float counterTop, float verticalScale)
        {
            return StreetSceneLayout.CustomerOffset(counterTop, verticalScale);
        }
        private Vector2 CustomerViewPosition(Vector2 position)
        {
            position.y += CustomerViewOffsetY(CustomerCounterTopY(), layoutVerticalScale);
            return position;
        }
        private static bool ShouldDrawOrderBubble(int visualRow) => visualRow < 2;

        private void DrawCustomer(StreetCustomer c, bool showOrderBubble)
        {
            Vector2 position = CustomerViewPosition(c.Position);
            bool walking=c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving||c.State==StreetCustomerState.Advancing;
            DrawCustomerBody(c, walking);
            if(c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving)return;
            int pendingLines = c.PendingOrderLineCount;
            if (showOrderBubble && pendingLines > 0)
            {
                int visibleLines = Mathf.Min(StreetCustomer.MaxVisibleOrderProducts, pendingLines);
                float quantityWidth = 20f;
                for (int i = 0; i < visibleLines; i++)
                    quantityWidth = Mathf.Max(quantityWidth, text.CalcSize(new GUIContent(c.GetVisibleOrderLine(i).Remaining.ToString())).x);
                // Transform the anchor once; local content offsets must not grow on tall screens.
                Rect logicalBubble = OrderBubbleBounds(position, pendingLines, quantityWidth);
                float contentScale = OrderBubbleContentScale(logicalBubble.height, layoutVerticalScale);
                Rect bubble = OrderBubbleLayoutBounds(logicalBubble, layoutVerticalScale);
                DrawOrderBubble(bubble, contentScale);
                for (int i = 0; i < visibleLines; i++)
                    DrawOrderLine(OrderLineBounds(bubble, i, visibleLines, contentScale), c.GetVisibleOrderLine(i), contentScale);
                string hiddenOrderIndicator = HiddenOrderIndicatorText(c.HiddenOrderLineCount);
                if (!string.IsNullOrEmpty(hiddenOrderIndicator))
                    GUI.Label(OrderMoreBounds(bubble, contentScale), hiddenOrderIndicator, tiny);
            }
        }
        private const float OrderRowHeight = 25f;
        private const float OrderPaddingY = 8f;
        private const float OrderTailHeight = 8f;
        private const float OrderFooterHeight = 14f;

        private static string HiddenOrderIndicatorText(int hiddenOrderLineCount) =>
            hiddenOrderLineCount > 0 ? "•••" : string.Empty;

        private static float OrderBubbleContentScale(float bubbleHeight, float verticalScale)
        {
            float rowPitch = StreetSimulation.QueueRowSpacing * Mathf.Max(.01f, verticalScale);
            return Mathf.Min(1f, rowPitch / Mathf.Max(1f, bubbleHeight));
        }

        private static Rect OrderBubbleBounds(Vector2 customerPosition, int pendingLines, float quantityWidth = 28f)
        {
            int rows = Mathf.Clamp(pendingLines, 1, StreetCustomer.MaxVisibleOrderProducts);
            float width = Mathf.Clamp(42f + quantityWidth, 62f, 70f);
            float height = OrderPaddingY * 2f + rows * OrderRowHeight + OrderTailHeight
                + (pendingLines > rows ? OrderFooterHeight : 0f);
            // Keep the same customer-relative tail baseline and horizontal center as before.
            return new Rect(customerPosition.x - width * .5f, customerPosition.y - 68f - height, width, height);
        }

        private static Rect OrderBubbleLayoutBounds(Rect bubble, float verticalScale)
        {
            float anchoredBottom = bubble.yMax * verticalScale;
            float scale = OrderBubbleContentScale(bubble.height, verticalScale);
            bubble.height *= scale;
            bubble.y = anchoredBottom - bubble.height;
            return bubble;
        }

        private static Rect OrderLineBounds(Rect bubble, int row, int visibleRows, float contentScale) =>
            new Rect(bubble.x + 5f, bubble.y + OrderPaddingY * contentScale + row * OrderRowHeight * contentScale,
                bubble.width - 10f, OrderRowHeight * contentScale);

        private static Rect OrderMoreBounds(Rect bubble, float contentScale) =>
            new Rect(bubble.x + 4f, bubble.y + (OrderPaddingY + 2f * OrderRowHeight) * contentScale,
                bubble.width - 8f, OrderFooterHeight * contentScale);

        private static Rect OrderBubbleSlice(Rect bounds, int column, int row, float borderX, float borderY)
        {
            float x = column == 0 ? bounds.x : column == 1 ? bounds.x + borderX : bounds.xMax - borderX;
            float y = row == 0 ? bounds.y : row == 1 ? bounds.y + borderY : bounds.yMax - borderY;
            return new Rect(x, y, column == 1 ? bounds.width - borderX * 2f : borderX,
                row == 1 ? bounds.height - borderY * 2f : borderY);
        }

        private void DrawOrderBubble(Rect bounds, float contentScale)
        {
            if (items == null) return;
            // Reuse the authored speech shape; shorten vertical corners together with compact rows.
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                DrawRaw(OrderBubbleSlice(bounds, column, row, 20f, 15f * contentScale), items,
                    OrderBubbleSlice(Items[11], column, row, 84f, 65f), true, false);
        }

        private static Rect OrderIconBounds(Rect row, float contentScale) =>
            new Rect(row.x + 2f, row.y + contentScale, 26f * contentScale, Mathf.Max(1f, row.height - 2f * contentScale));
        private static Rect OrderQuantityBounds(Rect row, float contentScale) =>
            new Rect(row.x + 30f * contentScale, row.y, row.width - 32f * contentScale, row.height);

        private static int FitOrderQuantityFontSize(GUIStyle style, string value, float maxWidth)
        {
            int original = style.fontSize;
            float measured = style.CalcSize(new GUIContent(value)).x;
            return measured > maxWidth ? Mathf.Max(11, Mathf.FloorToInt(original * maxWidth / measured)) : original;
        }

        private void DrawOrderLine(Rect row, StreetOrderLine line, float contentScale)
        {
            if (line == null || line.Remaining <= 0) return;
            DrawProductIcon(OrderIconBounds(row, contentScale), line.Product, false);
            int originalFontSize = text.fontSize;
            text.fontSize = Mathf.Max(10, Mathf.RoundToInt(originalFontSize * contentScale));
            Rect quantity = OrderQuantityBounds(row, contentScale);
            text.fontSize = FitOrderQuantityFontSize(text, line.Remaining.ToString(), quantity.width);
            GUI.Label(quantity, line.Remaining.ToString(), text);
            text.fontSize = originalFontSize;
        }
        private void DrawCustomerBody(StreetCustomer c, bool walking)
        {
            Vector2 position = CustomerViewPosition(c.Position);
            float bob = walking ? Mathf.Sin(c.AnimationTime * 14) * 2 : 0f;
            if (c.State == StreetCustomerState.Receiving) bob -= 3;
            Rect person = new Rect(position.x - 39, position.y - 76 + bob, 78, 76);
            Matrix4x4 viewMatrix = GUI.matrix;
            if (!walking)
            {
                // The atlas front poses are different people, not interchangeable idle frames.
                // Animate their existing silhouettes from simulation time, never OnGUI events.
                float phase = c.Id * 2.399963f;
                float sway = Mathf.Sin(c.AnimationTime * (2.2f + (c.Id % 3) * .18f) + phase);
                float breath = Mathf.Sin(c.AnimationTime * 3.2f + phase);
                float cheerTime = Mathf.Repeat(c.AnimationTime + c.Id * .83f, 4.8f + (c.Id % 4) * .55f);
                float hop = 0f;
                if (cheerTime < 1.2f)
                {
                    float pulse = Mathf.Sin(cheerTime * Mathf.PI * 2f / 1.2f);
                    hop = pulse * pulse; // Two soft hops, with zero velocity at either end.
                }

                Rect responsivePerson = LayoutRect(person);
                Vector3 feet = new Vector3(responsivePerson.center.x, responsivePerson.yMax, 0f);
                Vector3 offset = new Vector3(sway * 1.2f, -hop * 4f, 0f);
                Vector3 scale = new Vector3(1f + breath * .012f - hop * .018f,
                    1f - breath * .016f + hop * .025f, 1f);
                GUI.matrix = viewMatrix * Matrix4x4.TRS(feet + offset,
                    Quaternion.Euler(0f, 0f, sway * 2.8f), scale) * Matrix4x4.Translate(-feet);
            }

            try
            {
                bool flip = walking && c.Target.x < c.Position.x;
                DrawFan(person, c.Id, walking, flip, ClubVisualTheme.ForLevel(VisualLevelIndex).Fans);
            }
            finally
            {
                // Only the body/garment move: badges and subsequent UI keep the safe-area matrix.
                GUI.matrix = viewMatrix;
            }
        }
        private void DrawFan(Rect person, int customerId, bool walking, bool flip, ClubFanDefinition fans)
        {
            if (fans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Texture2D atlas = walking ? fans.LoadWalkingFans() : fans.LoadFrontFans();
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Rect source = walking
                ? fans.WalkingSourceRect(atlas.width, atlas.height, customerId)
                : fans.FrontSourceRect(atlas.width, atlas.height, customerId);
            Draw(person, atlas, source, false, flip);
        }
        private static int GetAllBoysFanVariant(int customerId)
        {
            int variant = customerId % AllBoysFanVariantCount;
            return variant < 0 ? variant + AllBoysFanVariantCount : variant;
        }
        private static int GetAllBoysFanFrame(int customerId, bool walking) =>
            (walking ? AllBoysFanVariantCount : 0) + GetAllBoysFanVariant(customerId);
        private static int GetLegacyAllBoysFanFrame(int customerId, bool walking)
        {
            int variant = customerId % AllBoysLegacyFanVariantCount;
            if (variant < 0) variant += AllBoysLegacyFanVariantCount;
            return (walking ? AllBoysLegacyWalkingFanFirstFrame : AllBoysLegacyFrontFanFirstFrame) + variant;
        }
        private void DrawAllBoysFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? allBoysWalkingFans : allBoysFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, GetAllBoysFanSourceRect(atlas, GetAllBoysFanVariant(customerId)), false, flip);
        }
        private static Rect GetAllBoysFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % AllBoysFanVariantCount;
            float cellWidth = atlas.width / (float)AllBoysFanAtlasColumns;
            float cellHeight = atlas.height / (float)AllBoysFanAtlasColumns;
            int column = index % AllBoysFanAtlasColumns;
            int rowFromTop = index / AllBoysFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight, cellWidth, cellHeight);
        }
        private static int GetChicagoFanVariant(int customerId)
        {
            int variant = customerId % ChicagoFanVariantCount;
            return variant < 0 ? variant + ChicagoFanVariantCount : variant;
        }
        private void DrawChicagoFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? chicagoWalkingFans : chicagoFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, GetChicagoFanSourceRect(atlas, GetChicagoFanVariant(customerId)), false, flip);
        }
        private static Rect GetChicagoFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % ChicagoFanVariantCount;
            float cellWidth = atlas.width / (float)ChicagoFanAtlasColumns;
            float cellHeight = atlas.height / (float)ChicagoFanAtlasColumns;
            int column = index % ChicagoFanAtlasColumns;
            int rowFromTop = index / ChicagoFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawVelezFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? velezWalkingFans : velezFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = GetAllBoysFanVariant(customerId);
            Draw(person, atlas, VelezFanSourceRect(atlas, variant), false, flip);
        }
        private static Rect VelezFanSourceRect(Texture2D atlas, int variant)
        {
            int index = variant % VelezFanVariantCount;
            float cellWidth = atlas.width / (float)VelezFanAtlasColumns;
            float cellHeight = atlas.height / (float)VelezFanAtlasColumns;
            int column = index % VelezFanAtlasColumns;
            int rowFromTop = index / VelezFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawFerroFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? ferroWalkingFans : ferroFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = customerId % FerroFanVariantCount;
            if (variant < 0) variant += FerroFanVariantCount;
            Draw(person, atlas, FerroFanSourceRect(atlas, variant), false, flip);
        }
        private static Rect FerroFanSourceRect(Texture2D atlas, int variant)
        {
            if (atlas == null) return Rect.zero;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawLosRedondosFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? losRedondosWalkingFans : losRedondosFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, LosRedondosFanSourceRect(atlas, customerId), false, flip);
        }
        private static Rect LosRedondosFanSourceRect(Texture2D atlas, int customerId)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % LosRedondosFanVariantCount;
            if (variant < 0) variant += LosRedondosFanVariantCount;
            float cellWidth = atlas.width / (float)LosRedondosFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % LosRedondosFanAtlasColumns;
            int rowFromTop = variant / LosRedondosFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawIndependienteFan(Rect person, int customerId, bool walking, bool flip)
        {
            Texture2D atlas = walking ? independienteWalkingFans : independienteFrontFans;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            int variant = customerId % IndependienteFanVariantCount;
            if (variant < 0) variant += IndependienteFanVariantCount;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / 4f;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            Rect source = new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
            Draw(person, atlas, source, false, flip);
        }
        private void DrawTeamFan(Rect person, int customerId, bool walking, bool flip, Texture2D front, Texture2D walk)
        {
            Texture2D atlas = walking ? walk : front;
            if (atlas == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, atlas, TeamFanSourceRect(atlas, customerId), false, flip);
        }
        private static Rect TeamFanSourceRect(Texture2D atlas, int customerId)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % TeamFanVariantCount;
            if (variant < 0) variant += TeamFanVariantCount;
            float cellWidth = atlas.width / (float)TeamFanAtlasColumns;
            float cellHeight = atlas.height / (float)TeamFanAtlasColumns;
            int column = variant % TeamFanAtlasColumns;
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect FerroRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % FerroFanVariantCount;
            if (variant < 0) variant += FerroFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int outfitColumn = (variant % TeamFanAtlasColumns) * 2;
            int pairedPoseColumn = outfitColumn + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect TeamRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % TeamFanVariantCount;
            if (variant < 0) variant += TeamFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 3f;
            int pairedPoseColumn = (variant % TeamFanAtlasColumns) * 2 + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect IndependienteRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % IndependienteFanVariantCount;
            if (variant < 0) variant += IndependienteFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % TeamFanAtlasColumns) * 2 + (pose & 1);
            int rowFromTop = variant / TeamFanAtlasColumns;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private void DrawRacingFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (racingFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            Draw(person, racingFans, RacingFanSourceRect(racingFans, customerId, walking), false, flip);
        }
        private void DrawSanLorenzoFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (sanLorenzoFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            // This paired 6x4 atlas shares the established twelve-variant outfit/pose grid.
            Draw(person, sanLorenzoFans, RacingFanSourceRect(sanLorenzoFans, customerId, walking), false, flip);
        }
        private void DrawBocaFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (bocaFans == null)
            {
                Person(person, GetLegacyAllBoysFanFrame(customerId, walking), walking && !flip);
                return;
            }
            // The Boca atlas shares the established paired 6x4 grid with twelve outfit variants.
            Draw(person, bocaFans, RacingFanSourceRect(bocaFans, customerId, walking), false, flip);
        }
        private void DrawRiverFan(Rect person, int customerId, bool walking, bool flip)
        {
            if (riverFans == null) return;
            Draw(person, riverFans, RacingFanSourceRect(riverFans, RiverAtlasVariant(customerId), walking), false, flip);
        }
        private static int RiverAtlasVariant(int customerId)
        {
            int variant = ((customerId % RacingFanVariantCount) + RacingFanVariantCount) % RacingFanVariantCount;
            // River provenance uses top-to-bottom rows; reuse the existing bottom-origin atlas helper.
            return (3 - variant / 3) * 3 + variant % 3;
        }
        private static Rect RacingFanSourceRect(Texture2D atlas, int customerId, bool walking)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % RacingFanVariantCount;
            if (variant < 0) variant += RacingFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % 3) * 2 + (walking ? 1 : 0);
            int rowFromTop = variant / 3;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
        private static Rect RacingRiotFanFrameSource(Texture2D atlas, int customerId, int pose)
        {
            if (atlas == null) return Rect.zero;
            int variant = customerId % RacingFanVariantCount;
            if (variant < 0) variant += RacingFanVariantCount;
            float cellWidth = atlas.width / 6f;
            float cellHeight = atlas.height / 4f;
            int pairedPoseColumn = (variant % 3) * 2 + (pose & 1);
            int rowFromTop = variant / 3;
            return new Rect(pairedPoseColumn * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight,
                cellWidth, cellHeight);
        }
    }
}
