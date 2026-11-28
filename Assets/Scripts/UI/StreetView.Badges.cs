using System.Globalization;
using UnityEngine;

namespace HayChoriYPaty
{
    // Insignias de la parrilla como vitrina de trofeos (2026-11-26, a pedido; referencia: estantes de trofeos de un juego móvil).
    // Un estante por producto con sus tres insignias (vender 500, 2.500 y 5.000): estatuillas de bronce, plata y oro sobre
    // pedestales con placa; las que faltan se ven como siluetas pálidas con su progreso. La lista se desplaza con el dedo.
    // Mismas insignias, umbrales y datos que antes (StreetSalesBadges + ventas acumuladas); arte en StreetView.BadgesArt.cs.
    public sealed partial class StreetView
    {
        private const float BadgeCaseHeight = 236f, BadgeCaseGap = 12f;
        private static readonly string[] BadgeProductNames = { "CHORI", "PATY", "BONDIOLA", "VACÍO", "COCA", "FERNET", "CERVEZA" };
        private static readonly string[] BadgeProductPlurals = { "CHORIS", "PATYS", "BONDIOLAS", "VACÍOS", "COCAS", "FERNETS", "CERVEZAS" };
        private float badgesScroll;
        private bool badgesPointerHeld, badgesDragging, badgesTouchHeld, badgesTouchDragging;
        private float badgesPointerDownY, badgesPointerDownScroll, badgesTouchDownY, badgesTouchDownScroll;
        private int badgesIgnoreNativeTapUntilFrame = -1;

        private static Rect BadgesPanelBounds(float height) => new Rect(12, 16, W - 24, height - 16 - 92);
        private static Rect BadgesViewportBounds(float height)
        {
            Rect panel = BadgesPanelBounds(height);
            float top = panel.y + 178f;
            return new Rect(panel.x + 10f, top, panel.width - 20f, Mathf.Max(80f, panel.yMax - 30f - top));
        }
        // Case bounds in scroll-content coordinates (0 = top of the viewport, before scrolling).
        private static Rect BadgeCaseBounds(int product, float viewportWidth) =>
            new Rect(0f, product * (BadgeCaseHeight + BadgeCaseGap), viewportWidth - 10f, BadgeCaseHeight);
        private static float BadgesContentHeight => StreetSalesBadges.ProductCount * (BadgeCaseHeight + BadgeCaseGap) - BadgeCaseGap;
        private static float BadgesMaxScroll(float height) => Mathf.Max(0f, BadgesContentHeight - BadgesViewportBounds(height).height);

        private static string BadgeNumber(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));

        private void OpenBadges()
        {
            hubActive = false; badgesActive = true; badgesScroll = 0f;
            badgesPointerHeld = badgesDragging = badgesTouchHeld = badgesTouchDragging = false;
        }

        private void DrawBadgesScreen()
        {
            DrawIntroScreen(hubBackdrop, new Color(.42f, .42f, .42f, 1f), ScaleMode.ScaleAndCrop);
            DrawIntroScreen(Texture2D.whiteTexture, new Color(.025f, .018f, .012f, .72f), ScaleMode.StretchToFill);
            float height = logicalCanvasHeight;
            Rect panel = BadgesPanelBounds(height);
            DrawMarketWoodPanel(panel, new Color(.11f, .065f, .036f, .97f));

            // Header: outlined gold title between stars and the rule chip, as in the ranking.
            float star = 30f;
            DrawRankingStar(new Rect(panel.x + 22, panel.y + 22, star, star));
            DrawRankingStar(new Rect(panel.xMax - 22 - star, panel.y + 22, star, star));
            RankingOutlined(new Rect(panel.x + 58, panel.y + 14, panel.width - 116, 46), "INSIGNIAS DE LA PARRILLA", 32, RankingGold, new Color(.16f, .06f, .01f), TextAnchor.MiddleCenter, 1.3f);
            Rect chip = new Rect(panel.center.x - 175, panel.y + 62, 350, 24);
            RankingPill(chip, new Color(0f, 0f, 0f, .42f));
            MarketText(new Rect(chip.x + 10, chip.y, chip.width - 20, chip.height), "VENDÉ 500 · 2.500 · 5.000 DE CADA PRODUCTO", 14, new Color(.98f, .90f, .74f));

            int earned = 0;
            for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
                for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
                    if (game.HasProductSalesBadge(product, tier)) earned++;
            DrawBadgesSummary(new Rect(panel.x + 14, panel.y + 96, panel.width - 28, 58), earned);
            string sync = Cloud.FirebasePlayerSession.Instance != null
                ? Cloud.FirebasePlayerSession.Instance.ProgressSyncStatus : "Progreso local de prueba";
            MarketText(new Rect(panel.x + 14, panel.y + 155, panel.width - 28, 20), sync, 12, new Color(.98f, .90f, .74f));

            Rect viewport = BadgesViewportBounds(height);
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                Vector2 point = ScreenToLayoutPoint(evt.mousePosition, new Vector2(Screen.width, Screen.height),
                    CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea));
                point.y *= height / H;
                if (viewport.Contains(point)) { badgesScroll += evt.delta.y * 28f; evt.Use(); }
            }
            badgesScroll = Mathf.Clamp(badgesScroll, 0f, BadgesMaxScroll(height));
            GUI.BeginGroup(LayoutRect(viewport));
            try
            {
                for (int product = 0; product < StreetSalesBadges.ProductCount; product++)
                {
                    Rect bounds = BadgeCaseBounds(product, viewport.width);
                    bounds.y -= badgesScroll;
                    if (bounds.yMax <= 0f || bounds.y >= viewport.height) continue;
                    DrawBadgeProductCase(bounds, product);
                }
            }
            finally { GUI.EndGroup(); }
            DrawBadgesScrollBar(viewport, height);

            string coverage = game.HasIncompleteSalesHistory ? "Historial antiguo parcial: contamos las ventas registradas."
                : "Ventas acumuladas al cerrar cada resultado.";
            MarketText(new Rect(panel.x + 14, panel.yMax - 28, panel.width - 28, 22), coverage, 12, new Color(.98f, .90f, .74f));
            DrawStandardButton(BadgeBackBounds(height), BadgeBackAction, "VOLVER AL BARRIO");
        }

        // Count of badges won: gold block with a star, the big count and a progress bar.
        private void DrawBadgesSummary(Rect bounds, int earned)
        {
            DrawRankingRowBase(bounds, RankingRowKind.Gold, 70f);
            DrawRankingStar(new Rect(bounds.x + 15, bounds.y + 11, 40, 40));
            RankingOutlined(new Rect(bounds.x + 82, bounds.y + 4, 150, 34), earned + " / " + StreetSalesBadges.BadgeCount, 28,
                new Color(1f, .97f, .88f), new Color(.24f, .10f, .02f), TextAnchor.MiddleLeft);
            MarketText(new Rect(bounds.x + 82, bounds.y + 34, 170, 16), "INSIGNIAS CONSEGUIDAS", 12, MarketInk, TextAnchor.MiddleLeft);
            Rect track = new Rect(bounds.x + 258, bounds.y + 21, bounds.width - 274, 16);
            RankingPill(track, new Color(.28f, .17f, .08f, .85f));
            float progress = Mathf.Clamp01(earned / (float)StreetSalesBadges.BadgeCount);
            if (progress > 0f)
                RankingPill(new Rect(track.x + 2, track.y + 2, Mathf.Max(12f, (track.width - 4) * progress), track.height - 4), new Color(.36f, .78f, .20f));
        }

        private void DrawBadgeProductCase(Rect bounds, int product)
        {
            long sales = game.GetCumulativeProductSales(product);
            DrawBadgeCase(bounds);
            // Product name plate with its icon, and the running total on the right.
            Rect plate = new Rect(bounds.x + 12, bounds.y + 10, 178, 30);
            RankingPill(new Rect(plate.x, plate.y + 2, plate.width, plate.height), new Color(.10f, .04f, .0f, .35f));
            RankingPill(plate, MarketNavy);
            RankingPill(new Rect(plate.x + 2, plate.y + 2, plate.width - 4, plate.height - 4), new Color(.10f, .30f, .62f));
            DrawProductIcon(new Rect(plate.x + 8, plate.y + 1, 34, 28), product);
            RankingOutlined(new Rect(plate.x + 46, plate.y + 1, plate.width - 54, plate.height - 2), BadgeProductNames[product], 19, Color.white, MarketNavy, TextAnchor.MiddleLeft);
            MarketText(new Rect(bounds.xMax - 182, bounds.y + 10, 168, 30), BadgeNumber(sales) + " VENDIDAS", 15, MarketInk, TextAnchor.MiddleRight);

            float slotWidth = (bounds.width - 24f) / StreetSalesBadges.TierCount;
            float shelfTop = bounds.y + 186f;
            for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
            {
                Rect slot = new Rect(bounds.x + 12f + tier * slotWidth, bounds.y + 44f, slotWidth, shelfTop - bounds.y - 44f);
                DrawBadgeTrophy(slot, product, tier, sales, game.HasProductSalesBadge(product, tier), shelfTop);
            }
            DrawBadgeShelf(new Rect(bounds.x + 8, shelfTop, bounds.width - 16, 20));
            for (int tier = 0; tier < StreetSalesBadges.TierCount; tier++)
            {
                Rect slot = new Rect(bounds.x + 12f + tier * slotWidth, shelfTop + 18f, slotWidth, 22f);
                DrawBadgeStatus(new Rect(slot.x + 10, slot.y, slot.width - 20, slot.height), sales, tier, game.HasProductSalesBadge(product, tier));
            }
        }

        private void DrawBadgeTrophy(Rect slot, int product, int tier, long sales, bool earned, float shelfTop)
        {
            long target = StreetSalesBadges.Threshold(tier);
            Color caption = earned ? MarketInk : new Color(.45f, .36f, .27f);
            MarketText(new Rect(slot.x, slot.y, slot.width, 16), "VENDÉ " + BadgeNumber(target), 14, caption);
            MarketText(new Rect(slot.x, slot.y + 15, slot.width, 14), BadgeProductPlurals[product], 11, caption);

            float pedestalWidth = 82f + tier * 10f, pedestalHeight = 38f + tier * 6f;
            Rect pedestal = new Rect(slot.center.x - pedestalWidth * .5f, shelfTop + 3f - pedestalHeight, pedestalWidth, pedestalHeight);
            float top = slot.y + 30f, box = Mathf.Min(86f + tier * 10f, slot.width - 30f);
            Texture2D statuette = BadgeStatuette(product, tier, earned);
            Rect figure = new Rect(slot.center.x - box * .5f, pedestal.y + 4f - box, box, box);
            if (statuette != null)
            {
                float aspect = statuette.width / (float)statuette.height;
                float fw = aspect >= 1f ? box : box * aspect, fh = aspect >= 1f ? box / aspect : box;
                float room = pedestal.y + 4f - top;
                if (fh > room) { fw *= room / fh; fh = room; }
                figure = new Rect(slot.center.x - fw * .5f, pedestal.y + 4f - fh, fw, fh);
            }
            if (earned && tier == 2)
            {
                float glow = Mathf.Max(figure.width, figure.height) * 1.9f;
                RankingDraw(new Rect(figure.center.x - glow * .5f, figure.center.y - glow * .5f, glow, glow), BadgeGlow(), Color.white);
            }
            // Soft contact shadow of the pedestal on the shelf.
            RankingPill(new Rect(pedestal.x - 4f, shelfTop - 1f, pedestal.width + 8f, 7f), new Color(.10f, .04f, .0f, earned ? .30f : .14f));
            DrawBadgePedestal(pedestal, tier, earned);
            if (statuette != null) GUI.DrawTexture(LayoutRect(figure), statuette, ScaleMode.StretchToFill, true);
            Rect plaque = new Rect(pedestal.center.x - pedestal.width * .32f, pedestal.y + 12f, pedestal.width * .64f, 15f);
            RankingPill(plaque, earned ? Color.Lerp(BadgeMetal(tier), Color.white, .25f) : new Color(1f, .98f, .93f, .80f), 4f);
            MarketText(plaque, BadgeNumber(target), 11, earned ? MarketInk : new Color(.70f, .62f, .52f));
            if (earned)
            {
                // Sparkles: one for bronze, two for silver, three for gold.
                Vector2[] spots = { new Vector2(.92f, .10f), new Vector2(.06f, .28f), new Vector2(.84f, .62f) };
                for (int i = 0; i <= tier; i++)
                {
                    float size = i == 0 ? 16f : 12f;
                    DrawRankingStar(new Rect(figure.x + figure.width * spots[i].x - size * .5f, figure.y + figure.height * spots[i].y - size * .5f, size, size));
                }
            }
        }

        // Under the shelf: "¡CONSEGUIDA!" or the progress towards that badge.
        private void DrawBadgeStatus(Rect bounds, long sales, int tier, bool earned)
        {
            long target = StreetSalesBadges.Threshold(tier);
            if (earned)
            {
                RankingPill(bounds, new Color(.13f, .36f, .06f));
                RankingPill(new Rect(bounds.x + 2, bounds.y + 2, bounds.width - 4, bounds.height - 4), new Color(.36f, .74f, .20f));
                RankingOutlined(bounds, "¡CONSEGUIDA!", 13, Color.white, new Color(.08f, .25f, .03f));
                return;
            }
            RankingPill(bounds, new Color(.33f, .24f, .15f, .90f));
            float progress = Mathf.Clamp01((float)((double)System.Math.Max(0L, sales) / target));
            if (progress > 0f)
                RankingPill(new Rect(bounds.x + 2, bounds.y + 2, Mathf.Max(bounds.height - 4f, (bounds.width - 4f) * progress), bounds.height - 4),
                    Color.Lerp(BadgeMetal(tier), new Color(.55f, .40f, .25f), .25f));
            RankingOutlined(bounds, BadgeNumber(System.Math.Min(sales, target)) + " / " + BadgeNumber(target), 12, Color.white, new Color(.20f, .09f, .02f));
        }

        private void DrawBadgesScrollBar(Rect viewport, float height)
        {
            float max = BadgesMaxScroll(height);
            if (max <= 0f) return;
            Rect track = new Rect(viewport.xMax - 5f, viewport.y + 4f, 5f, viewport.height - 8f);
            RankingPill(track, new Color(.12f, .10f, .075f, .55f), 2.5f);
            float thumb = Mathf.Max(30f, track.height * track.height / (track.height + max));
            RankingPill(new Rect(track.x, track.y + (track.height - thumb) * badgesScroll / max, track.width, thumb), new Color(1f, .95f, .79f, .97f), 2.5f);
        }

        // Android: native IMGUI taps keep working; a drag inside the vitrina scrolls it.
        private void HandleBadgesTouchScroll(Vector2 pixel, bool down, bool held, bool up)
        {
            if (!badgesActive || game == null) return;
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            if (viewport.width <= 0f) return;
            logicalCanvasHeight = CanvasLogicalHeight(viewport);
            layoutVerticalScale = logicalCanvasHeight / H;
            Vector2 point = ScreenToLayoutPoint(pixel, new Vector2(Screen.width, Screen.height), viewport);
            point.y *= layoutVerticalScale;
            if (down)
            {
                badgesTouchHeld = BadgesViewportBounds(logicalCanvasHeight).Contains(point);
                badgesTouchDownY = point.y; badgesTouchDownScroll = badgesScroll;
                badgesTouchDragging = false; badgesIgnoreNativeTapUntilFrame = -1;
            }
            if (held && badgesTouchHeld)
            {
                float delta = badgesTouchDownY - point.y;
                if (Mathf.Abs(delta) > 8f) badgesTouchDragging = true;
                if (badgesTouchDragging) badgesScroll = Mathf.Clamp(badgesTouchDownScroll + delta, 0f, BadgesMaxScroll(logicalCanvasHeight));
            }
            if (up)
            {
                if (badgesTouchDragging) badgesIgnoreNativeTapUntilFrame = Time.frameCount + 1;
                badgesTouchHeld = false; badgesTouchDragging = false;
            }
        }
        private bool SuppressBadgesNativeTap => badgesActive &&
            (badgesTouchDragging || Time.frameCount <= badgesIgnoreNativeTapUntilFrame);

        // Mouse/editor pointer: drag to scroll, tap the back button.
        private void HandleBadgesPointer(Vector2 p, bool down, bool up)
        {
            float height = logicalCanvasHeight;
            int hit = BadgeBackBounds(height).Contains(p) ? BadgeBackAction : 0;
            if (down)
            {
                badgesPointerHeld = BadgesViewportBounds(height).Contains(p);
                badgesPointerDownY = p.y; badgesPointerDownScroll = badgesScroll; badgesDragging = false;
                pressedAction = hit;
            }
            if (!down && !up && badgesPointerHeld)
            {
                float delta = badgesPointerDownY - p.y;
                if (Mathf.Abs(delta) > 8f) { badgesDragging = true; pressedAction = 0; }
                if (badgesDragging) badgesScroll = Mathf.Clamp(badgesPointerDownScroll + delta, 0f, BadgesMaxScroll(height));
            }
            if (up)
            {
                if (!badgesDragging && pressedAction != 0 && pressedAction == hit) DispatchAction(pressedAction);
                pressedAction = 0; badgesPointerHeld = false; badgesDragging = false;
            }
        }
    }
}
