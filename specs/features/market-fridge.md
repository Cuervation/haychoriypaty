# Mercado y Heladera — authorized 2026-10-13

## Contract

Implement the supplied Mercado/Heladera request as one new IMGUI screen with two tabs, reusing StreetView's standard cover buttons. Preserve all eleven actual catalogs/quotas, seven final products, kitchen lifecycle, sale prices, routes, hiring/speed costs, existing cover and gameplay HUD. Level 1 enters directly; levels 2–11 enter Mercado first. Stock review from the selector must not start a match. Purchasing unlocks only after winning All Boys; its fridge can be consulted before that. No retry action: loss returns to levels. Last-level victory never selects level 12.

## Domain and persistence

Add a central ten-ingredient catalog and configurable pack prices in integer cents (150,100,150,100,400,500,250,350,1600,800). IDs: Chorizo0, Pan común1, Paty2, Pan Paty3, Bondiola4, Vacío5, Coca6, Cerveza7, Fernet8, Coca grande9. Last two purchase packs provide four integer portions; all other packs provide one unit. Stable final product recipes: 0→0+1, 1→2+3, 2→4+1, 3→5+1, 4→6, 5→8+9 (one portion each), 6→7. Coca grande uses the requested 2,25 L inventory label; existing visual art is not modified.

StreetMarketData is embedded in the existing versioned StreetGame save (v4). Initialize once from the actual level-1 requirements; legacy saves keep unlocks and receive one controlled stock migration for their highest unlocked level, not recurring gifts. Lower already-completed levels receive reward recognition watermarks during migration. Serialize the available balance and exact stock in the same PlayerPrefs JSON operation; ignore the legacy `debtCents` field when loading v4 saves, and retain snapshot/active attempt recovery. No external storage/service.

Purchases validate before changing money or stock. Persist a monotonic purchase operation ID and cumulative purchases; same/stale ID rejects. UI confirm controls also debounce accidental double taps. Every purchase, including minimum missing packs, requires sufficient available balance and is rejected atomically otherwise. There is no supplier credit, debt, loan, or other financing.

At victory: add 50% gross sales + 2×attempt cash remaining to the available Mercado balance; at loss add only 50% gross sales and restore the exact pre-attempt stock. No debt deduction occurs. Guard settlement with the persisted attempt ID. To prevent replay farming, only recognize the increase over each level's highest previously recognized 50%-sales amount; pay the remaining-cash bonus once on the first victory of that level. Display the actual formula and newly earned amount separately when a replay has already recognized sales.

## Production and handoff

Do not create a second simulation. StreetSimulation optionally binds an IStreetKitchenStock provider (standalone simulation remains unlimited for compatibility). StreetKitchenProduction reserves ingredients temporarily per physical food ID when creating units; failed preparation/burn/discard/cancel releases reservations without consuming permanent stock. Validate physical carried food before committing a recipe once in Kitchen.Consume at successful handoff. Quotas and revenue mutate only after that same valid consume. Reservations protect outstanding minimum goals of other products that share ingredients, avoiding extra Chori tying up bread required for Bondiola/Vacío. Remaining stock survives victory; reservations do not. Loss restores snapshot including bottle portions.

## Shared APIs / file ownership for implementation

- StreetMarket.cs: StreetIngredientCatalog (Names, PackUnits, recipe authority); StreetMarketBalance; StreetMarketData; StreetMarketResult; IStreetKitchenStock; StreetMarket.
- StreetMarket ctor(StreetMarketData, StreetBalance), Initialize(int unlockedLevel), RecoverAbandonedAttempt(), Units(int), PackPriceCents(int), RequiredUnits(StreetSimulation), MissingUnits(sim,id), MissingPacks(sim,id), IsIngredientUnlocked(id,unlocked), HasRequiredStock(sim), MinimumPurchaseCostCents(sim), TryBuy(sim,id,packs,unlocked,operationId), BeginAttempt(sim), SettleAttempt(sim), AbortAttempt(). Properties Data, BalanceCents, NextPurchaseId, LastResult.
- IStreetKitchenStock: bool CanPrepareFood(int product), bool TryReserveFood(int foodId,int product), bool ConsumeReservedFood(int foodId,int product), void ReleaseFood(int foodId).
- StreetMarketResult fields: won, level, grossSalesCents, leftoverCents, halfSalesCents, recognizedSalesCents, bonusCents, earnedCents, totalBalanceCents, consumedUnits[], remainingUnits[].
- StreetGame exposes Market, CanStartRound, TryStartRound(), TryBuyIngredient(id,packs,operationId=0), BuyMissingIngredient(id), ReturnToLevels(), ProcessRoundResult(). Keep StartRound wrapper; Retry rejects. SelectLevel allows Ready/result selection but not live match; NextLevel only selects a valid next Ready level. Parent owns StreetGame, StreetSimulation, StreetKitchenProduction and integration/specs.
- UI worker owns only StreetView.cs and new market UI tests; core domain worker owns only new StreetMarket.cs and new domain tests. Preserve prior uncommitted HUD edits.

## Acceptance / focused validation

- [x] Exact individual purchases, no negative stock/cash or duplicate operations; persistence/migration and one-time kit.
- [x] Actual requirements and shared bread; quarter-bottle remainder and independent shortages.
- [x] Cash-only purchases reject unaffordable transactions atomically; debt is not serialized or deducted from earnings; existing v4 saves ignore legacy `debtCents`.
- [x] Successful physical handoff consumes once; unavailable ingredients cannot produce/deliver; production and movement preserved.
- [x] Victory stock/earned balance/progression, defeat snapshot/earnings, abandonment/reload restoration and settlement/replay deduplication.
- [x] Mercado/Heladera navigation, disabled play until stocked, no retry, no level 12.
- [x] Unity focused EditMode/PlayMode checks and real Mercado/Heladera captures at portrait 16:9, 19.5:9 and 20:9; report unavailable/device validation truthfully.
- [x] Mercado objectives render current simulation products and goals in at most two compact rows (1–3: one row; 4: 2+2; 5: 3+2; 6: 3+3; 7: 4+3), while the ingredient list alone scrolls and fixed purchase/play controls remain accessible.


## Cash-only policy update — 2026-10-13

Supplier credit is removed. The Mercado balance is the only purchasing source; insufficient funds reject a purchase without changing stock or balance. `JUGAR PARTIDO` stays stock-gated, and the Mercado reports the exact amount still needed to complete minimum stock. Existing v4 saves remain readable: the removed `debtCents` JSON field is ignored and not written back. Prices, recipes, quotas, starting kit, and earned sales settlement are unchanged.

## Initial release validation (before the cash-only update)

Historical Unity 6000.6.3f1 / MCP validation for the original market-release build: focused EditMode and PlayMode checks, plus the then-current 11/11 campaign. This is pre-cash-only evidence and is not a claim that the new cash-only campaign is economically solvable.

Nine actual Game View images of representative level5 / test inventory at540×960,540×1170 and540×1200: `Logs/Acceptance/MarketFridge-20261013/` (both tabs and bottom of fridge with partial bottles). Tab hit targets verified at all three ratios. Player save is restored after fixture destruction. `git diff --check` passed at initial implementation. Final release follow-up: EditMode Market/kitchen/specialty 30/30, HUD texture/popup geometry 4/4, and PlayMode Market view/pointer/navigation 39/39 passed. Android 0.2.30/code32 built and package/archive/v2-signature verified; see `android-prototype.md`. No device install or phone-side gameplay test was performed.

Finite-stock integration: if every residual line is an already-completed quota with no physical or preparable supply, use normal departure/cancellation without marking the order complete or granting sales. This prevents sold-out extra tickets blocking FIFO columns; unmet goals/deliverable tickets and unlimited standalone simulations retain their behavior. Rejected settlement/save prevents navigation/progression.


## Dynamic Mercado objectives — UI layout update (2026-10-13)

The Mercado header builds its objective cards in current StreetSimulation catalog order from ProductCount, GetAvailableProduct, and GetProductGoal. One objective is centered; two/three share one row; four through seven use the accepted two-row patterns. Cards reuse the existing product atlas and short names. At five or more targets they use compact catalog abbreviations. The objective band grows by one card row only; ingredient rows remain the only scrollable region, and the purchase/restock/play footer stays fixed. The current cash-only rule remains unchanged; no FIADO label or financing was reintroduced.

Validation: focused Unity 6000.6.3f1 PlayMode checks passed 3/3: dynamic counts/live captions/layout bounds at 16:9, 19.5:9 and 20:9; selector purchase interaction after header reflow; cash-only restock/stock gate. Unity MCP was disconnected and headless nographics cannot produce GameView captures, so visual screenshot inspection remains pending.
