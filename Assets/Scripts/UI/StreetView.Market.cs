using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Mercado: layout, hit-testing y dibujo. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private static Rect MarketBackBounds() => new Rect(12, 10, 132, 50);
        private const float MarketObjectiveTop = 202f;
        private const float MarketCompactRowHeight = 104f, MarketPurchaseHeight = 64f, MarketOthersHeight = 48f;
        private static readonly Color MarketInk = new Color(.20f, .08f, .025f);
        private static readonly Color MarketBlueInk = new Color(.025f, .075f, .20f);
        private static readonly Color MarketIce = new Color(.91f, .97f, 1f);
        private static readonly Color MarketNavy = new Color(.008f, .035f, .14f);
        private static readonly Color MarketRoyalBlue = new Color(.025f, .24f, .70f);
        private static readonly Color MarketCyan = new Color(.22f, .82f, 1f);
        private static readonly Color MarketGold = new Color(1f, .80f, .24f);
        private static readonly Color MarketGreen = new Color(.04f, .40f, .19f);
        private static readonly Color MarketRed = new Color(.73f, .12f, .10f);
        private Rect MarketBodyBounds() => MarketBodyBounds(game.Sim.ProductCount, logicalCanvasHeight);
        private static Rect MarketBodyBounds(int productCount, float canvasHeight) =>
            new Rect(12, MarketBodyTop(productCount), W - 24, Mathf.Max(140f, canvasHeight - 184f - MarketBodyTop(productCount)));
        private static Rect MarketEconomyFooterBounds(float canvasHeight) => new Rect(12, canvasHeight - 154f, W - 24, 66f);
        private static Rect MarketBuyMissingBounds(float canvasHeight)
        {
            Rect footer = MarketEconomyFooterBounds(canvasHeight);
            return new Rect(footer.x + 270f, footer.y + 8f, footer.width - 282f, footer.height - 16f);
        }
        private static int MarketObjectiveRowCount(int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            return count == 0 ? 0 : count <= 3 ? 1 : 2;
        }
        private static int MarketObjectiveFirstRowCount(int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            return count <= 3 ? count : count == 4 ? 2 : count == 7 ? 4 : 3;
        }
        private static int MarketObjectiveItemsInRow(int productCount, int row)
        {
            int count = Mathf.Clamp(productCount, 0, 7), rows = MarketObjectiveRowCount(count);
            if (row < 0 || row >= rows) return 0;
            return rows == 1 ? count : row == 0 ? MarketObjectiveFirstRowCount(count) : count - MarketObjectiveFirstRowCount(count);
        }
        private static float MarketObjectiveCardHeight(int productCount) => productCount <= 3 ? 72f : 64f;
        private static float MarketObjectiveBandHeight(int productCount)
        {
            int rows = MarketObjectiveRowCount(productCount);
            return rows == 0 ? 0f : rows * MarketObjectiveCardHeight(productCount) + (rows - 1) * 4f;
        }
        private static Rect MarketObjectiveCardBounds(int slot, int productCount)
        {
            int count = Mathf.Clamp(productCount, 0, 7);
            if (slot < 0 || slot >= count) return Rect.zero;
            int first = MarketObjectiveFirstRowCount(count), row = count > 3 && slot >= first ? 1 : 0;
            int column = row == 0 ? slot : slot - first, itemsInRow = MarketObjectiveItemsInRow(count, row);
            const float gap = 6f;
            float width = itemsInRow == 1 ? 240f : (W - 52f - gap * (itemsInRow - 1)) / itemsInRow;
            float rowWidth = width * itemsInRow + gap * (itemsInRow - 1);
            return new Rect((W - rowWidth) * .5f + column * (width + gap),
                MarketObjectiveTop + row * (MarketObjectiveCardHeight(count) + 4f), width, MarketObjectiveCardHeight(count));
        }
        private static Rect MarketEconomyBounds(int productCount) =>
            new Rect(46, MarketObjectiveTop + MarketObjectiveBandHeight(productCount) + 8f, W - 92, 58f);
        private static Rect MarketTabsBounds(int productCount) =>
            new Rect(14f, MarketEconomyBounds(productCount).yMax + 7f, W - 28f, 42f);
        private static Rect MarketTabBounds(int tab, int productCount)
        {
            Rect strip = MarketTabsBounds(productCount);
            const int tabCount = 3;
            float gap = 6f, width = (strip.width - gap * (tabCount - 1)) / tabCount;
            return new Rect(strip.x + Mathf.Clamp(tab, 0, tabCount - 1) * (width + gap), strip.y, width, strip.height);
        }
        private Rect MarketTabsBounds() => MarketTabsBounds(game.Sim.ProductCount);
        private Rect MarketTabBounds(int tab) => MarketTabBounds(tab, game.Sim.ProductCount);
        private static float MarketBodyTop(int productCount) => MarketTabsBounds(productCount).yMax + 8f;
        private int[] MarketIngredientIds(bool required)
        {
            // All Boys' opening market is intentionally limited to its two required products.
            if (!required && game.Sim.LevelIndex == 0) return new int[0];
            int[] need = game.Market.RequiredUnits(game.Sim);
            var ids = new System.Collections.Generic.List<int>();
            for (int i = 0; i < need.Length; i++)
                if (required ? need[i] > 0 : need[i] == 0 && game.IsIngredientUnlocked(i)) ids.Add(i);
            return ids.ToArray();
        }
        private int[] MarketVisibleIngredients()
        {
            var ids = new System.Collections.Generic.List<int>(MarketIngredientIds(true));
            if (marketOtherExpanded) ids.AddRange(MarketIngredientIds(false));
            return ids.ToArray();
        }
        private int MarketExpandedId()
        {
            if (marketExpandedIngredient == -2) return -1;
            foreach (int id in MarketVisibleIngredients()) if (id == marketExpandedIngredient && game.IsIngredientUnlocked(id)) return id;
            foreach (int id in MarketIngredientIds(true)) if (game.Market.MissingUnits(game.Sim, id) > 0) return id;
            return -1;
        }
        private float MarketRowHeight(int id) =>
            (game.Market.RequiredUnits(game.Sim)[id] > 0 ? MarketCompactRowHeight : 96f) + (id == MarketExpandedId() ? MarketPurchaseHeight : 0f);
        private float MarketRequiredContentHeight()
        {
            float height = 0f;
            foreach (int id in MarketIngredientIds(true)) height += MarketRowHeight(id);
            return height;
        }
        private float MarketContentHeight()
        {
            if (marketTab == 2) return 40f + 2f * MarketPermitCardHeight + MarketPermitCardGap;
            if (marketTab == 1)
                return game.TicketExtrasUnlocked ? MarketExtrasNoticeHeight + 8f +
                    5f * MarketTicketCardHeight + 4f * MarketTicketCardGap : MarketBodyBounds().height;
            float height = 0f;
            foreach (int id in MarketVisibleIngredients()) height += MarketRowHeight(id);
            if (MarketIngredientIds(false).Length > 0) height += MarketOthersHeight;
            return height;
        }
        private Rect MarketExtrasNoticeBounds() =>
            new Rect(16f, MarketBodyBounds().y - marketScroll, W - 32f, MarketExtrasNoticeHeight);
        private Rect MarketTicketCardBounds(int slot)
        {
            Rect body = MarketBodyBounds();
            if (slot < 0 || slot >= 5) return Rect.zero;
            return new Rect(body.x, body.y + MarketExtrasNoticeHeight + 8f + slot * (MarketTicketCardHeight + MarketTicketCardGap) - marketScroll,
                body.width, MarketTicketCardHeight);
        }
        private static Rect MarketTicketRedeemButtonBounds(Rect card) =>
            new Rect(card.xMax - 151f, card.y + 25f, 140f, 58f);
        private Rect MarketTicketRedeemButtonBounds(int slot) => MarketTicketRedeemButtonBounds(MarketTicketCardBounds(slot));
        private Rect MarketPermitCardBounds(int slot)
        {
            Rect body = MarketBodyBounds();
            if (slot < 0 || slot >= StreetMarketPreparationCatalog.ItemCount) return Rect.zero;
            return new Rect(body.x, body.y + 40f + slot * (MarketPermitCardHeight + MarketPermitCardGap) - marketScroll,
                body.width, MarketPermitCardHeight);
        }
        private static Rect MarketPermitBuyButtonBounds(Rect card) =>
            new Rect(card.xMax - 154f, card.y + 34f, 142f, 48f);
        private float MarketMaxScroll() => Mathf.Max(0, MarketContentHeight() - MarketBodyBounds().height);
        private Rect MarketRowBounds(int slot)
        {
            int[] visible = MarketVisibleIngredients();
            if (slot < 0 || slot >= visible.Length) return Rect.zero;
            Rect body = MarketBodyBounds();
            float y = body.y - marketScroll;
            for (int i = 0; i < slot; i++) y += MarketRowHeight(visible[i]);
            if (slot >= MarketIngredientIds(true).Length) y += MarketOthersHeight;
            return new Rect(body.x, y, body.width, MarketRowHeight(visible[slot]));
        }
        private Rect MarketOthersBounds() => new Rect(12, MarketBodyBounds().y + MarketRequiredContentHeight() - marketScroll, W - 24, MarketOthersHeight);
        // These same rectangles drive native pointer input and IMGUI rendering.
        private static Rect MarketPurchaseControlBounds(Rect row, int action)
        {
            float y = row.yMax - MarketPurchaseHeight + 10f;
            switch (action)
            {
                case MarketMinusAction: return new Rect(row.x + 10, y, 40, 46);
                case MarketPlusAction: return new Rect(row.x + 98, y, 40, 46);
                case MarketPlusTenAction: return new Rect(row.x + 142, y, 56, 46);
                case MarketBuyAction: return new Rect(row.x + 202, y, 142, 46);
                default: return new Rect(row.x + 348, y, row.width - 358, 46);
            }
        }
        private static bool MarketPurchaseControlsVisible(Rect row, Rect viewport) =>
            row.yMax <= viewport.yMax && row.yMax - MarketPurchaseHeight >= viewport.y;
        private int HitMarketAction(Vector2 p)
        {
            if (MarketBackBounds().Contains(p)) return MarketBackAction;
            if (MarketTabBounds(0).Contains(p)) return MarketIngredientsTabAction;
            if (MarketTabBounds(1).Contains(p)) return MarketExtrasTabAction;
            if (MarketTabBounds(2).Contains(p)) return MarketPermitsTabAction;
            if (MarketPlayBounds().Contains(p)) return MarketPlayAction;
            if (marketTab == 0 && MarketBuyMissingBounds(logicalCanvasHeight).Contains(p)) return MarketCanBuyMissing() ? MarketMissingAction : 0;
            Rect body = MarketBodyBounds();
            if (!body.Contains(p)) return 0;
            if (marketTab == 2)
            {
                for (int item = 0; item < StreetMarketPreparationCatalog.ItemCount; item++)
                {
                    Rect card = MarketPermitCardBounds(item);
                    if (MarketFullyVisibleInViewport(card, body) && MarketPermitBuyButtonBounds(card).Contains(p) &&
                        !game.IsPreparationPermitPurchased(item) && game.Market.BalanceCents >= StreetMarketPreparationCatalog.ItemPriceCents)
                        return item == (int)StreetMarketPermitItem.MunicipalLicense ? MarketMunicipalPermitAction : MarketPoliceContributionAction;
                }
                return 0;
            }
            if (marketTab == 1)
            {
                if (!game.TicketExtrasUnlocked) return 0;
                for (int role = 0; role < 5; role++)
                {
                    Rect card = MarketTicketCardBounds(role), button = MarketTicketRedeemButtonBounds(card);
                    if (MarketFullyVisibleInViewport(card, body) && button.Contains(p) && game.CanRedeemTicketWorker((StreetWorkerRole)role))
                        return MarketTicketRedeemAction + role;
                }
                return 0;
            }
            Rect othersBounds = MarketOthersBounds();
            if (MarketIngredientIds(false).Length > 0 && MarketFullyVisibleInViewport(othersBounds, body) && othersBounds.Contains(p)) return MarketOtherAction;
            int[] visible = MarketVisibleIngredients();
            for (int slot = 0; slot < visible.Length; slot++)
            {
                int id = visible[slot]; Rect row = MarketRowBounds(slot);
                if (!MarketFullyVisibleInViewport(row, body) || !row.Contains(p) || !game.IsIngredientUnlocked(id)) continue;
                if (id == MarketExpandedId() && MarketPurchaseControlsVisible(row, body))
                    foreach (int action in new[] { MarketMinusAction, MarketPlusAction, MarketPlusTenAction, MarketBuyAction, MarketRowMissingAction })
                        if (MarketPurchaseControlBounds(row, action).Contains(p)) return action + id;
                return MarketExpandAction + id;
            }
            return 0;
        }
        private static bool MarketFullyVisibleInViewport(Rect content, Rect viewport) =>
            content.yMin >= viewport.yMin && content.yMax <= viewport.yMax;
        private Rect MarketPlayBounds() => new Rect(22, logicalCanvasHeight - 80, W - 44, 66);
        private void MarketRevealIngredient(int id)
        {
            int[] ids = MarketVisibleIngredients();
            int slot = System.Array.IndexOf(ids, id);
            if (slot < 0) { marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll()); return; }
            Rect row = MarketRowBounds(slot), body = MarketBodyBounds();
            if (row.y < body.y) marketScroll -= body.y - row.y;
            else if (row.yMax > body.yMax) marketScroll += row.yMax - body.yMax;
            marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll());
        }
        private void DrawMarketScreen()
        {
            Texture2D scene = MarketSceneArt();
            if (scene != null) DrawRawCover(new Rect(0, 0, W, logicalCanvasHeight), scene, new Rect(0, 0, scene.width, scene.height * .58f));
            FillRect(new Rect(0, 0, W, logicalCanvasHeight), new Color(.015f, .045f, .14f, .38f));
            DrawStandardButton(MarketBackBounds(), MarketBackAction, marketFromSelector ? "NIVELES" : "BARRIO");
            DrawMarketBluePanel(new Rect(150, 8, 240, 56), MarketRoyalBlue);
            MarketText(new Rect(164, 10, 212, 50), "MERCADO", 34, Color.white);
            DrawMarketClubHeader();
            MarketText(new Rect(26, 176, 488, 22), StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex) ? "MODO DEFENSE" : "OBJETIVOS DEL PARTIDO", 18, new Color(.84f, .96f, 1f));
            DrawMarketObjectiveCards(game.Sim);
            Rect economy = MarketEconomyBounds(game.Sim.ProductCount);
            DrawMarketBluePanel(economy, new Color(.012f, .075f, .25f, .98f));
            if (marketTab != 1)
            {
                if (gameCoin != null) GUI.DrawTexture(new Rect(economy.x + 12, economy.y + 11, 36, 36), gameCoin, ScaleMode.ScaleToFit, true);
                MarketText(new Rect(economy.x + 54, economy.y + 12, 210, 32), "SALDO DISPONIBLE", 18, Color.white, TextAnchor.MiddleLeft);
                MarketText(new Rect(economy.x + 267, economy.y + 7, economy.width - 278, 44), FormatMarketMoney(game.Market.BalanceCents), 38, MarketGold, TextAnchor.MiddleRight);
            }
            else
            {
                MarketText(new Rect(economy.x + 14, economy.y + 12, 230, 32), "MIS TICKETS", 21, Color.white, TextAnchor.MiddleLeft);
                MarketText(new Rect(economy.x + 250, economy.y + 7, economy.width - 264, 44), game.TicketBalance.ToString(), 40, MarketGold, TextAnchor.MiddleRight);
            }
            DrawMarketTabs();
            Rect body = MarketBodyBounds();
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel)
            {
                Vector2 point = ScreenToLayoutPoint(evt.mousePosition, new Vector2(Screen.width, Screen.height), CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea));
                point.y *= logicalCanvasHeight / H;
                if (body.Contains(point)) { marketScroll = Mathf.Clamp(marketScroll + evt.delta.y * 24f, 0, MarketMaxScroll()); evt.Use(); }
            }
            marketScroll = Mathf.Clamp(marketScroll, 0, MarketMaxScroll());
            GUI.BeginGroup(body);
            try
            {
                if (marketTab == 0)
                {
                    int[] visible = MarketVisibleIngredients();
                    for (int slot = 0; slot < visible.Length; slot++)
                    {
                        Rect row = MarketRowBounds(slot);
                        // Never render a clipped card: ingredient names, figures, and controls stay intact.
                        if (!MarketFullyVisibleInViewport(row, body)) continue;
                        bool showControls = MarketPurchaseControlsVisible(row, body);
                        row.x -= body.x; row.y -= body.y;
                        DrawMarketIngredientRow(row, visible[slot], showControls);
                    }
                    if (MarketIngredientIds(false).Length > 0)
                    {
                        Rect others = MarketOthersBounds();
                        if (MarketFullyVisibleInViewport(others, body))
                        {
                            others.x -= body.x; others.y -= body.y;
                            DrawMarketOthers(others);
                        }
                    }
                }
                else if (marketTab == 1) DrawMarketExtrasBody(body);
                else DrawMarketPermitsBody(body);
            }
            finally { GUI.EndGroup(); }
            DrawMarketScrollBar(body);
            if (!string.IsNullOrEmpty(marketMessage))
            {
                Rect messageBounds = new Rect(20, logicalCanvasHeight - 180, W - 40, 22);
                DrawMarketBluePanel(messageBounds, new Color(.015f, .12f, .38f, .96f));
                MarketText(new Rect(messageBounds.x + 6, messageBounds.y, messageBounds.width - 12, messageBounds.height), marketMessage, 14, Color.white);
            }
            if (marketTab == 0) DrawMarketEconomyFooter();
            else if (marketTab == 1) DrawMarketExtrasFooter();
            else DrawMarketPermitsFooter();
            DrawMarketPlayButton(MarketPlayBounds(), "JUGAR PARTIDO", game.CanStartRound);
        }
        private void DrawMarketTabs()
        {
            for (int tab = 0; tab < 3; tab++)
                DrawStandardButton(MarketTabBounds(tab), tab == 0 ? MarketIngredientsTabAction : tab == 1 ? MarketExtrasTabAction : MarketPermitsTabAction,
                    tab == 0 ? "INGREDIENTES" : tab == 1 ? "EXTRAS" : "PERMISOS");
            Rect selected = MarketTabBounds(marketTab);
            FillRect(new Rect(selected.x + 10f, selected.yMax - 4f, selected.width - 20f, 4f), MarketGold);
        }
        private void DrawMarketPermitsBody(Rect body)
        {
            MarketText(new Rect(8f, 2f, body.width - 16f, 34f), "PERMISOS Y CONTRIBUCIONES", 20, MarketGold, TextAnchor.MiddleLeft);
            for (int item = 0; item < StreetMarketPreparationCatalog.ItemCount; item++)
            {
                Rect absolute = MarketPermitCardBounds(item);
                if (!MarketFullyVisibleInViewport(absolute, body)) continue;
                Rect card = new Rect(absolute.x - body.x, absolute.y - body.y, absolute.width, absolute.height);
                DrawMarketBluePanel(card, MarketIce);
                Texture2D icon = item == (int)StreetMarketPermitItem.MunicipalLicense ? municipalPermitIcon : policeContributionIcon;
                if (icon != null) GUI.DrawTexture(new Rect(card.x + 10f, card.y + 19f, 72f, 72f), icon, ScaleMode.ScaleToFit, true);
                MarketText(new Rect(card.x + 88f, card.y + 24f, card.width - 260f, 30f),
                    StreetMarketPreparationCatalog.Name(item).ToUpperInvariant(), 19, MarketBlueInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(card.x + 88f, card.y + 57f, card.width - 260f, 28f),
                    "$" + (StreetMarketPreparationCatalog.ItemPriceCents / 100L).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    23, MarketRoyalBlue, TextAnchor.MiddleLeft);
                bool purchased = game.IsPreparationPermitPurchased(item);
                bool enough = game.Market.BalanceCents >= StreetMarketPreparationCatalog.ItemPriceCents;
                string caption = purchased ? "COMPRADO" : enough ? "COMPRAR" : "SALDO INSUFICIENTE";
                int action = item == (int)StreetMarketPermitItem.MunicipalLicense ? MarketMunicipalPermitAction : MarketPoliceContributionAction;
                DrawStandardButton(MarketPermitBuyButtonBounds(card), action, caption, !purchased && enough);
            }
        }
        private void DrawMarketPermitsFooter()
        {
            Rect footer = MarketEconomyFooterBounds(logicalCanvasHeight);
            DrawMarketBluePanel(footer, new Color(.012f, .075f, .25f));
            MarketText(new Rect(footer.x + 12f, footer.y + 7f, footer.width - 24f, 23f), "HABILITACIONES DE PREPARACIÓN", 14, Color.white);
            MarketText(new Rect(footer.x + 12f, footer.y + 31f, footer.width - 24f, 24f), "No modifican la jugabilidad por ahora.", 17, MarketGold);
        }
        private void DrawMarketExtrasBody(Rect body)
        {
            if (!game.TicketExtrasUnlocked)
            {
                Rect panel = new Rect(14f, body.y + Mathf.Max(12f, (body.height - 116f) * .5f), W - 28f, 116f);
                DrawMarketBluePanel(panel, new Color(.012f, .075f, .25f, .98f));
                MarketText(new Rect(panel.x + 10f, panel.y + 12f, panel.width - 20f, 38f), "EXTRAS SE DESBLOQUEA EN RACING", 21, MarketGold);
                MarketText(new Rect(panel.x + 16f, panel.y + 54f, panel.width - 32f, 46f), "Juntá tickets con cada victoria.\nCanjeá desde Racing (nivel 10).", 17, Color.white);
                return;
            }
            Rect notice = MarketExtrasNoticeBounds();
            if (MarketFullyVisibleInViewport(notice, body))
            {
                Rect localNotice = new Rect(notice.x - body.x, notice.y - body.y, notice.width, notice.height);
                DrawMarketBluePanel(localNotice, new Color(.012f, .075f, .25f, .96f));
                string message = StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex) ? "TOWER DEFENSE SE JUEGA SIN TRABAJADORES DE TICKETS"
                    : game.Sim.LevelIndex >= 5 ? "CADA TRABAJADOR CUESTA 3 TICKETS" : "SE CANJEA AL PREPARAR RACING";
                MarketText(new Rect(localNotice.x + 8f, localNotice.y + 2f, localNotice.width - 16f, localNotice.height - 4f), message, 17, Color.white);
            }
            for (int index = 0; index < 5; index++)
            {
                Rect card = MarketTicketCardBounds(index);
                if (!MarketFullyVisibleInViewport(card, body)) continue;
                Rect local = new Rect(card.x - body.x, card.y - body.y, card.width, card.height);
                DrawMarketBluePanel(local, MarketIce);
                StreetWorkerRole role = (StreetWorkerRole)index;
                DrawMarketTicketWorker(local, role);
                float textX = local.x + 82f;
                MarketText(new Rect(textX, local.y + 10f, 232f, 26f), TicketRoleName(role), 21, MarketBlueInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(textX, local.y + 38f, 232f, 20f), "COSTO: 3 TICKETS", 15, MarketRoyalBlue, TextAnchor.MiddleLeft);
                MarketText(new Rect(textX, local.y + 65f, 232f, 24f), "RESERVADOS: " + game.PendingTicketWorkers(role), 15, MarketBlueInk, TextAnchor.MiddleLeft);
                bool canRedeem = game.CanRedeemTicketWorker(role);
                string caption = StreetSimulation.IsTowerDefenseLevel(game.Sim.LevelIndex) ? "NO DISPONIBLE"
                    : game.Sim.LevelIndex < 5 ? "DISPONIBLE EN RACING" : game.TicketBalance < 3
                    ? "FALTAN " + System.Math.Max(0L, 3L - game.TicketBalance) + " TICKETS" : "CANJEAR";
                DrawStandardButton(MarketTicketRedeemButtonBounds(local), MarketTicketRedeemAction + index, caption, canRedeem);
            }
        }
        private void DrawMarketTicketWorker(Rect card, StreetWorkerRole role)
        {
            Texture2D texture = null;
            Rect source = Rect.zero;
            int level = game.Sim.LevelIndex;
            switch (role)
            {
                case StreetWorkerRole.Parrillero: texture = parrillero; source = ParrilleroPoses[0]; break;
                case StreetWorkerRole.Cocacolero: texture = CocacoleroArtForLevel(level); source = CocacoleroPoseForLevel(level, 0); break;
                case StreetWorkerRole.ParrilleroPremium: texture = premiumWorker; source = ParrilleroPoses[0]; break;
                case StreetWorkerRole.Fernetero:
                    texture = UsesMatchedCocacoleroArt(level) ? fernetWorkerLater : fernetWorker;
                    source = UsesMatchedCocacoleroArt(level) ? MatchedCocacoleroPoses[0] : ChicagoCocacoleroPoses[0];
                    break;
                case StreetWorkerRole.Mozo: texture = mozoWorker; source = ParrilleroPoses[0]; break;
            }
            if (texture == null || source.width <= 0f || source.height <= 0f) return;
            Rect slot = new Rect(card.x + 10f, card.y + 7f, 62f, card.height - 14f);
            float scale = Mathf.Min(slot.width / source.width, slot.height / source.height);
            Vector2 size = new Vector2(source.width * scale, source.height * scale);
            Rect fitted = new Rect(slot.center - size * .5f, size);
            DrawRaw(fitted, texture, source, true, false);
        }
        private static string TicketRoleName(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero ? "PARRILLERO"
            : role == StreetWorkerRole.Cocacolero ? "COCACOLERO" : role == StreetWorkerRole.ParrilleroPremium ? "PARRILLERO PREMIUM"
            : role == StreetWorkerRole.Fernetero ? "FERNETERO" : "MOZO";
        private void DrawMarketExtrasFooter()
        {
            Rect footer = MarketEconomyFooterBounds(logicalCanvasHeight);
            DrawMarketBluePanel(footer, new Color(.012f, .075f, .25f));
            long reserved = 0;
            for (int role = 0; role < 5; role++) reserved += game.PendingTicketWorkers((StreetWorkerRole)role);
            MarketText(new Rect(footer.x + 12f, footer.y + 9f, footer.width - 24f, 23f), "TRABAJADORES RESERVADOS", 15, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(footer.x + 12f, footer.y + 31f, footer.width - 24f, 25f), reserved.ToString(), 25, MarketGold, TextAnchor.MiddleLeft);
        }
        private Texture2D MarketSceneArt()
        {
            switch (VisualLevelIndex)
            {
                case 0: return muralArt; case 1: return chicagoBackground; case 2: return velezBackground;
                case 3: return ferroBackground; case 4: return independienteBackground; case 5: return racingBackground;
                case 6: return sanLorenzoBackground; case 7: return riverMural; case 8: return bocaBackground;
                case 9: return camionerosBackground;
                case StreetSimulation.HuracanBonusLevelId: return Resources.Load<Texture2D>("street-mural-huracan-master-v1");
                case StreetSimulation.ArgentinosJuniorsLevelId: return Resources.Load<Texture2D>("street-mural-argentinos-master-v1");
                default: return losRedondosBackground;
            }
        }
        private void DrawMarketClubHeader()
        {
            DrawMarketBluePanel(new Rect(12, 70, W - 24, 100), new Color(.012f, .065f, .22f, .97f));
            int level = game.Sim.LevelIndex;
            Texture2D crest = LevelCrestForLevel(level);
            Rect badge = LevelCrestSizedSlot(new Rect(28, 82, 76, 76), crest);
            if (crest != null) DrawLevelCrest(badge, crest);
            else
            {
                // No invented official crest: existing club palette/initials identify levels without a crest asset.
                DrawMarketBluePanel(badge, ClubVisualTheme.ForLevel(level).PrimaryColor);
                MarketText(new Rect(badge.x + 6, badge.y + 20, badge.width - 12, 36), MarketClubInitials(level), 22, Color.white);
            }
            string[] club = StreetSimulation.LevelDisplayNames[level].Split(new[] {'/', '–', '—'});
            if (level == StreetSimulation.PeregrinacionTowerDefenseCampaignLevelId) club = new[] {"Peregrinación", "a Luján · Moreno"};
            float captionX = Mathf.Max(120f, badge.xMax + 16f);
            float captionWidth = 506f - captionX;
            MarketText(new Rect(captionX, 77, captionWidth, 24), "NIVEL " + (StreetSimulation.CampaignPositionOf(level) + 1), 19, MarketCyan, TextAnchor.MiddleLeft);
            MarketText(new Rect(captionX, 100, captionWidth, 35), club[0].Trim().ToUpperInvariant(), 29, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(captionX, 136, captionWidth, 23), club.Length > 1 ? club[1].Trim().ToUpperInvariant() : "", 19, new Color(.82f, .94f, 1f), TextAnchor.MiddleLeft);
        }
        private static string MarketClubInitials(int level)
        {
            if (level == StreetSimulation.ArgentinosJuniorsLevelId) return "AJ";
            string name = StreetSimulation.LevelNames[level];
            string[] words = name.Split(' '); string value = "";
            foreach (string word in words) if (word.Length > 0 && value.Length < 3) value += char.ToUpperInvariant(word[0]);
            return value;
        }
        private static string MarketObjectiveCaption(StreetSimulation sim, int slot, bool compact)
        {
            if (sim == null || slot < 0 || slot >= sim.ProductCount) return string.Empty;
            int product = sim.GetAvailableProduct(slot);
            string name = StreetSimulation.ProductNames[product]; int separator = name.IndexOf(' ');
            return (separator > 0 ? name.Substring(0, separator) : name) +
                (StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex) ? string.Empty : " ×" + sim.GetProductGoal(product));
        }
        private void DrawMarketObjectiveCards(StreetSimulation sim)
        {
            if (StreetSimulation.IsTowerDefenseLevel(sim.LevelIndex))
            {
                Rect card = new Rect(26f, 207f, 488f, 76f);
                DrawMarketBluePanel(card, MarketIce);
                MarketText(new Rect(36f, 211f, 468f, 30f), "ATENDÉ A LOS " + TowerDefenseLevelConfig.TotalFanCountForCampaignLevel(sim.LevelIndex) + (sim.LevelIndex == StreetSimulation.PeregrinacionTowerDefenseCampaignLevelId ? " PEREGRINOS" : " HINCHAS"), 24, MarketBlueInk);
                MarketText(new Rect(36f, 244f, 468f, 32f), sim.LevelIndex == StreetSimulation.PeregrinacionTowerDefenseCampaignLevelId ? "7 productos · Construí y atendé antes de la salida" : "Chori y Coca · Que ninguno llegue a la salida", 16, MarketBlueInk);
                return;
            }
            for (int slot = 0; slot < sim.ProductCount; slot++)
            {
                int product = sim.GetAvailableProduct(slot); Rect card = MarketObjectiveCardBounds(slot, sim.ProductCount);
                DrawMarketBluePanel(card, MarketIce);
                float iconSize = sim.ProductCount > 3 ? 32f : 48f;
                Rect icon = new Rect(card.x + 10, card.center.y - iconSize * .5f, iconSize, iconSize);
                DrawProductIcon(icon, product);
                float textX = icon.xMax + 4f, textWidth = card.xMax - textX - 10f;
                string name = MarketObjectiveCaption(sim, slot, false).Split('×')[0].Trim();
                MarketText(new Rect(textX, card.y + 12, textWidth, 20), name, sim.ProductCount > 3 ? 14 : 20, MarketBlueInk, TextAnchor.MiddleLeft);
                MarketText(new Rect(textX, card.y + 31, textWidth, 27), "×" + sim.GetProductGoal(product), sim.ProductCount > 3 ? 24 : 28, MarketBlueInk, TextAnchor.MiddleLeft);
            }
        }
        private static string MarketIngredientName(int id) => id == 9 ? "Coca 2,25 L" : StreetIngredientCatalog.Names[id];
        private void DrawMarketIngredientRow(Rect row, int id, bool showControls)
        {
            bool expanded = id == MarketExpandedId();
            float faceHeight = row.height - (expanded ? MarketPurchaseHeight : 0f);
            Rect card = new Rect(row.x, row.y + 2, row.width, row.height - 5);
            DrawMarketBluePanel(card, MarketIce);
            DrawMarketIngredientIcon(MarketIngredientIconBounds(row, faceHeight), id);
            float nameX = row.x + 100;
            MarketText(new Rect(nameX, row.y + 17, 144, 27), MarketIngredientName(id), 28, MarketBlueInk, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX, row.y + 47, 94, 26), FormatMarketMoney(game.Market.PackPriceCents(id)), 25, MarketRoyalBlue, TextAnchor.MiddleLeft);
            MarketText(new Rect(nameX + 96, row.y + 47, 50, 27), StreetIngredientCatalog.PackUnits[id] == 1 ? "1 u." : "4 porc.", 12, MarketBlueInk, TextAnchor.MiddleLeft);
            int needed = game.Market.RequiredUnits(game.Sim)[id], stock = game.Market.Units(id), missing = game.Market.MissingUnits(game.Sim, id);
            float statsX = row.x + 252, statsWidth = row.width - 264, column = statsWidth / 3f;
            string[] labels = { "NECESITÁS", "TENÉS", "FALTAN" };
            int[] values = { needed, stock, missing };
            for (int i = 0; i < 3; i++)
            {
                MarketText(new Rect(statsX + i * column, row.y + 12, column, 17), labels[i], 13, MarketBlueInk);
                MarketText(new Rect(statsX + i * column, row.y + 28, column, 30), values[i].ToString(), 28,
                    i == 1 ? MarketGreen : i == 2 && missing > 0 ? MarketRed : MarketBlueInk);
            }
            Rect track = new Rect(statsX, row.y + 66, statsWidth - 84, 16);
            FillRect(track, MarketNavy);
            bool unlocked = game.IsIngredientUnlocked(id);
            float coverage = needed > 0 ? Mathf.Clamp01((float)stock / needed) : 0f;
            FillRect(new Rect(track.x + 2, track.y + 2, (track.width - 4) * coverage, track.height - 4), missing > 0 ? MarketCyan : new Color(.20f, .78f, .32f));
            MarketText(new Rect(track.xMax + 4, row.y + 62, 80, 24), !unlocked ? "BLOQUEADO" : needed == 0 ? "RESERVA" : missing == 0 ? "COMPLETO" : "FALTANTE", 12,
                !unlocked || needed == 0 ? MarketBlueInk : missing > 0 ? MarketRed : MarketGreen);
            if (unlocked)
            {
                if (expanded && showControls)
                {
                    int packs = marketPendingPacks[id];
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketMinusAction), MarketMinusAction + id, "−", packs > 0);
                    Rect minus = MarketPurchaseControlBounds(row, MarketMinusAction);
                    DrawMarketBluePanel(new Rect(row.x + 54, minus.y + 3, 40, 40), MarketNavy);
                    MarketText(new Rect(row.x + 54, minus.y, 40, 46), packs.ToString(),
                        Mathf.Min(24, 60 / packs.ToString().Length), Color.white);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketPlusAction), MarketPlusAction + id, "+");
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketPlusTenAction), MarketPlusTenAction + id, "+10");
                    long cost = (long)packs * game.Market.PackPriceCents(id);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketBuyAction), MarketBuyAction + id,
                        (missing == 0 && needed > 0 ? "COMPRAR MÁS\n" : "COMPRAR\n") + FormatMarketMoney(cost), packs > 0 && cost <= game.Market.BalanceCents);
                    DrawStandardButton(MarketPurchaseControlBounds(row, MarketRowMissingAction), MarketRowMissingAction + id, "COMPRAR\nFALTANTES", missing > 0 && (long)game.Market.MissingPacks(game.Sim, id) * game.Market.PackPriceCents(id) <= game.Market.BalanceCents);
                }
                // Row expansion never buys anything. On Android the hit surface is limited to its compact face.
                bool prior = GUI.enabled;
                if (GUI.Button(new Rect(row.x, row.y, row.width, faceHeight), "", invisible) && !SuppressMarketNativeTap)
                NativeAction(MarketExpandAction + id);
                GUI.enabled = prior;
            }
        }
        private static string MarketFreshMeatResource(int id) => id == 4
            ? "ModularKitchen/Art/meat-bondiola-whole-v1_Raw"
            : id == 5 ? "ModularKitchen/Art/meat-vacio-whole-v1_Raw" : null;

        private static Rect MarketIngredientIconBounds(Rect row, float faceHeight)
        {
            float size = Mathf.Min(faceHeight < 100f ? 62f : 78f, Mathf.Max(0f, faceHeight - 24f));
            return new Rect(row.x + 14f, row.y + (faceHeight - size) * .5f, size, size);
        }
        private static Rect FitMarketIngredientIcon(Rect bounds, Vector2 sourceSize)
        {
            if (bounds.width <= 0f || bounds.height <= 0f || sourceSize.x <= 0f || sourceSize.y <= 0f) return Rect.zero;
            float scale = Mathf.Min(bounds.width / sourceSize.x, bounds.height / sourceSize.y);
            Vector2 size = sourceSize * scale;
            return new Rect(bounds.center - size * .5f, size);
        }
        private static Rect MarketIngredientSpriteSource(Sprite sprite)
        {
            Rect source = sprite.rect;
            // External meat sprites keep authored pixel bounds even when Android downsizes the atlas.
            // Normalize the entire source, not just its origin, so adjacent cooking states cannot bleed in.
            if (sprite.name.StartsWith("meat-", System.StringComparison.Ordinal))
            {
                Vector2 scale = new Vector2(sprite.texture.width / 2172f, sprite.texture.height / 724f);
                source = new Rect(Vector2.Scale(source.position, scale), Vector2.Scale(source.size, scale));
            }
            source.y = sprite.texture.height - source.yMax;
            return source; // Full sprite, never slice a sandwich to fake a bread icon.
        }
        private void DrawMarketIngredientIcon(Rect bounds, int id)
        {
            Texture2D updatedMeat = id >= 0 && id < marketFreshMeatTextures.Length ? marketFreshMeatTextures[id] : null;
            if (updatedMeat != null)
            {
                GUI.DrawTexture(FitMarketIngredientIcon(bounds, new Vector2(updatedMeat.width, updatedMeat.height)),
                    updatedMeat, ScaleMode.StretchToFill, true);
                return;
            }
            Texture2D standalone = id == 1 ? marketBreadCommon : id == 3 ? marketBreadBurger
                : id == 8 ? marketFernetBottle : id == 9 ? marketCocaLarge : null;
            if (standalone != null)
            {
                GUI.DrawTexture(FitMarketIngredientIcon(bounds, new Vector2(standalone.width, standalone.height)), standalone, ScaleMode.StretchToFill, true);
                return;
            }
            Sprite sprite = id >= 0 && id < marketIngredientSprites.Length ? marketIngredientSprites[id] : null;
            if (sprite == null) { DrawProductIcon(bounds, id == 6 ? 4 : id == 7 ? 6 : id == 8 ? 5 : 0); return; }
            Rect source = MarketIngredientSpriteSource(sprite);
            Rect fit = FitMarketIngredientIcon(bounds, source.size);
            DrawRaw(fit, sprite.texture, source, true, false);
        }
        private void DrawMarketScrollBar(Rect body)
        {
            float max = MarketMaxScroll(); if (max <= 0) return;
            Rect track = new Rect(W - 9, body.y, 4, body.height);
            FillRect(track, new Color(.008f, .035f, .14f, .88f));
            float height = Mathf.Max(28, body.height * body.height / (body.height + max));
            FillRect(new Rect(track.x, track.y + (track.height - height) * marketScroll / max, 4, height), MarketCyan);
        }
        private void DrawMarketOthers(Rect bounds)
        {
            DrawMarketBluePanel(new Rect(bounds.x, bounds.y + 2, bounds.width, bounds.height - 4), new Color(.025f, .18f, .48f));
            int[] others = MarketIngredientIds(false);
            for (int i = 0; i < Mathf.Min(3, others.Length); i++) DrawMarketIngredientIcon(new Rect(bounds.x + 10 + i * 34, bounds.y + 8, 30, 30), others[i]);
            MarketText(new Rect(bounds.x + 118, bounds.y + 7, bounds.width - 156, 32), "OTROS INGREDIENTES", 18, Color.white, TextAnchor.MiddleLeft);
            MarketText(new Rect(bounds.xMax - 38, bounds.y + 6, 28, 32), marketOtherExpanded ? "−" : "+", 28, Color.white);
            if (GUI.Button(bounds, "", invisible) && !SuppressMarketNativeTap) NativeAction(MarketOtherAction);
        }
        private void DrawMarketEconomyFooter()
        {
            Rect footer = MarketEconomyFooterBounds(logicalCanvasHeight);
            DrawMarketBluePanel(footer, new Color(.012f, .075f, .25f));
            long cost = game.Market.MinimumPurchaseCostCents(game.Sim);
            MarketText(new Rect(footer.x + 12, footer.y + 7, 230, 23), "COSTO PARA COMPLETAR", 14, Color.white);
            MarketText(new Rect(footer.x + 12, footer.y + 30, 230, 30), FormatMarketMoney(cost), 36, MarketGold);
            bool canBuyMissing = MarketCanBuyMissing();
            string caption = !MarketHasMissing() ? "STOCK COMPLETO" : canBuyMissing ? "COMPRAR FALTANTES" : "SALDO INSUFICIENTE";
            DrawStandardButton(MarketBuyMissingBounds(logicalCanvasHeight), MarketMissingAction, caption, canBuyMissing);
        }
        private void DrawMarketPlayButton(Rect bounds, string caption, bool enabled)
        {
            DrawStandardButton(bounds, MarketPlayAction, caption, enabled);
        }

        private readonly System.Collections.Generic.Dictionary<Color,
            System.Collections.Generic.Dictionary<Vector2Int, Texture2D>> marketPanelTextures =
            new System.Collections.Generic.Dictionary<Color, System.Collections.Generic.Dictionary<Vector2Int, Texture2D>>();

        private void DrawMarketBluePanel(Rect bounds, Color face)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            System.Collections.Generic.Dictionary<Vector2Int, Texture2D> sizes;
            if (!marketPanelTextures.TryGetValue(face, out sizes))
            {
                sizes = new System.Collections.Generic.Dictionary<Vector2Int, Texture2D>();
                marketPanelTextures.Add(face, sizes);
            }
            var size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(bounds.width * 2f)),
                Mathf.Max(1, Mathf.RoundToInt(bounds.height * 2f)));
            Texture2D texture;
            if (!sizes.TryGetValue(size, out texture))
            {
                texture = CreateRoundedMarketPanel(size.x, size.y, face);
                sizes.Add(size, texture);
            }
            GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill, true);
        }

        private static Texture2D CreateRoundedMarketPanel(int width, int height, Color faceColor)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = "Rounded navy cyan market panel", hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float w = width * .5f, h = height * .5f;
            float radius = Mathf.Min(16f, h * .32f);
            Rect shadow = new Rect(1, 3, w - 1, h - 3);
            Rect rim = new Rect(0, 0, w - 1, h - 3);
            Rect cyan = new Rect(2, 2, w - 5, h - 7);
            Rect face = new Rect(4, 4, w - 9, h - 11);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * .5f, py = (height - y - .5f) * .5f;
                Color color = new Color(.003f, .018f, .075f,
                    .64f * RoundedCoverage(px, py, shadow, radius));
                color = Over(color, MarketNavy, RoundedCoverage(px, py, rim, radius));
                color = Over(color, MarketCyan, RoundedCoverage(px, py, cyan, Mathf.Max(1, radius - 2)));
                float coverage = RoundedCoverage(px, py, face, Mathf.Max(1, radius - 4));
                color = Over(color, faceColor, coverage);
                float gloss = .22f * Mathf.Clamp01(1f - (py - face.y) / Mathf.Max(1, face.height * .38f));
                color = Over(color, Color.white, coverage * gloss);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply();
            return texture;
        }
        private void DrawMarketWoodPanel(Rect bounds, Color face)
        {
            FillRect(bounds, new Color(.24f, .105f, .025f));
            if (upgradeWood != null)
            {
                const float border = 12f, sourceBorder = 180f;
                Rect src = UpgradeWoodBounds;
                float[] sx = { src.x, src.x + sourceBorder, src.xMax - sourceBorder, src.xMax };
                float[] sy = { src.y, src.y + sourceBorder, src.yMax - sourceBorder, src.yMax };
                float[] dx = { bounds.x, bounds.x + border, bounds.xMax - border, bounds.xMax };
                float[] dy = { bounds.y, bounds.y + border, bounds.yMax - border, bounds.yMax };
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                    DrawRaw(Rect.MinMaxRect(dx[x], dy[y], dx[x + 1], dy[y + 1]), upgradeWood,
                        Rect.MinMaxRect(sx[x], sy[y], sx[x + 1], sy[y + 1]), true, false);
            }
            FillRect(new Rect(bounds.x + 12, bounds.y + 12, bounds.width - 24, bounds.height - 24), face);
        }
        private static int FitMarketTextFontSize(GUIStyle style, string value, int preferredSize, Vector2 bounds)
        {
            if (style == null || preferredSize <= 0) return Mathf.Max(1, preferredSize);
            style.wordWrap = false;
            style.padding = new RectOffset();
            style.margin = new RectOffset();
            string content = value ?? string.Empty;
            for (int candidate = preferredSize; candidate > 1; candidate--)
            {
                style.fontSize = candidate;
                Vector2 measured = style.CalcSize(new GUIContent(content));
                if (measured.x <= Mathf.Max(1f, bounds.x - 2f) && measured.y <= Mathf.Max(1f, bounds.y - 2f))
                    return candidate;
            }
            return 1;
        }
        private void MarketText(Rect bounds, string value, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            if (marketLabelStyle == null) marketLabelStyle = new GUIStyle(menuTitle) { padding = new RectOffset(), margin = new RectOffset(), wordWrap = false };
            marketLabelStyle.alignment = alignment;
            marketLabelStyle.fontSize = FitMarketTextFontSize(marketLabelStyle, value, size, bounds.size);
            marketLabelStyle.normal.textColor = color;
            GUI.Label(bounds, value, marketLabelStyle);
        }
        private string MarketEconomyFooterSummary()
        {
            long balance = game.Market.BalanceCents, cost = game.Market.MinimumPurchaseCostCents(game.Sim);
            return "FALTANTES " + FormatMarketMoney(cost) + " · SALDO DESPUÉS " + (cost <= balance ? FormatMarketMoney(balance - cost) : "INSUFICIENTE");
        }
        private static string FormatBottleStock(int units) => (units/4)+" botella(s) + "+(units%4)+"/4 porción";
        private bool MarketHasMissing()
        {
            for (int i = 0; i < StreetIngredientCatalog.Names.Length; i++) if (game.Market.MissingPacks(game.Sim, i) > 0) return true;
            return false;
        }
        private bool MarketCanBuyMissing() => MarketHasMissing() && game.Market.MinimumPurchaseCostCents(game.Sim) <= game.Market.BalanceCents;
        private string MarketInsufficientFundsMessage(long totalCost) => "Saldo insuficiente: faltan " + FormatMarketMoney(totalCost-game.Market.BalanceCents);
        private static string FormatMarketMoney(long cents) => "$"+(cents/100L)+","+(cents%100L).ToString("00");
    }
}
