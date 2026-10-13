# Mercado y Heladera — authorized 2026-10-13

## Contract

Implement the supplied Mercado/Heladera request as one new IMGUI screen with two tabs, reusing StreetView's standard cover buttons. Preserve all eleven actual catalogs/quotas, seven final products, kitchen lifecycle, sale prices, routes, hiring/speed costs, existing cover and gameplay HUD. Level 1 enters directly; levels 2–11 enter Mercado first. Stock review from the selector must not start a match. Purchasing unlocks only after winning All Boys; its fridge can be consulted before that. No retry action: loss returns to levels. Last-level victory never selects level 12.

## Domain and persistence

Add a central ten-ingredient catalog and configurable pack prices in integer cents (150,100,150,100,400,500,250,350,1600,800). IDs: Chorizo0, Pan común1, Paty2, Pan Paty3, Bondiola4, Vacío5, Coca6, Cerveza7, Fernet8, Coca grande9. Last two purchase packs provide four integer portions; all other packs provide one unit. Stable final product recipes: 0→0+1, 1→2+3, 2→4+1, 3→5+1, 4→6, 5→8+9 (one portion each), 6→7. Coca grande uses the requested 2,25 L inventory label; existing visual art is not modified.

StreetMarketData is embedded in the existing versioned StreetGame save (v4). Initialize once from the actual level-1 requirements; legacy saves keep unlocks and receive one controlled stock migration for their highest unlocked level, not recurring gifts. Lower already-completed levels receive reward recognition watermarks during migration. Serialize balances/debt and exact stock in the same PlayerPrefs JSON operation; retain snapshot/active attempt and restore on reload/abandonment without credit. No external storage/service.

Purchases validate before changing money or stock. Persist a monotonic purchase operation ID and cumulative purchases; same/stale ID rejects. UI confirm controls also debounce accidental double taps. Finance only the minimum missing purchase packs (ceil portions/4 for bottles); cash alone buys extras. Fiado never grants free cash and has no interest. Credits pay debt before increasing spendable balance.

At victory: credit 50% gross sales + 2×attempt cash remaining. At loss: only 50% gross sales and restore the exact pre-attempt stock. Guard settlement with the persisted attempt ID. To prevent replay farming, only recognize the increase over each level's highest previously recognized 50%-sales credit; pay the remaining-cash bonus once on the first victory of that level. Display actual formula and newly recognized credit separately when a replay has already recognized sales.

## Production and handoff

Do not create a second simulation. StreetSimulation optionally binds an IStreetKitchenStock provider (standalone simulation remains unlimited for compatibility). StreetKitchenProduction reserves ingredients temporarily per physical food ID when creating units; failed preparation/burn/discard/cancel releases reservations without consuming permanent stock. Validate physical carried food before committing a recipe once in Kitchen.Consume at successful handoff. Quotas and revenue mutate only after that same valid consume. Reservations protect outstanding minimum goals of other products that share ingredients, avoiding extra Chori tying up bread required for Bondiola/Vacío. Remaining stock survives victory; reservations do not. Loss restores snapshot including bottle portions.

## Shared APIs / file ownership for implementation

- StreetMarket.cs: StreetIngredientCatalog (Names, PackUnits, recipe authority); StreetMarketBalance; StreetMarketData; StreetMarketResult; IStreetKitchenStock; StreetMarket.
- StreetMarket ctor(StreetMarketData, StreetBalance), Initialize(int unlockedLevel), RecoverAbandonedAttempt(), Units(int), PackPriceCents(int), RequiredUnits(StreetSimulation), MissingUnits(sim,id), MissingPacks(sim,id), IsIngredientUnlocked(id,unlocked), HasRequiredStock(sim), MinimumPurchaseCostCents(sim), FinancingNeededCents(sim), TryBuy(sim,id,packs,financeMinimum,unlocked,operationId), BeginAttempt(sim), SettleAttempt(sim), AbortAttempt(). Properties Data, BalanceCents, DebtCents, NextPurchaseId, LastResult.
- IStreetKitchenStock: bool CanPrepareFood(int product), bool TryReserveFood(int foodId,int product), bool ConsumeReservedFood(int foodId,int product), void ReleaseFood(int foodId).
- StreetMarketResult fields: won, level, grossSalesCents, leftoverCents, halfSalesCents, recognizedSalesCents, bonusCents, newCreditCents, debtRepaidCents, totalBalanceCents, consumedUnits[], remainingUnits[].
- StreetGame exposes Market, CanStartRound, TryStartRound(), TryBuyIngredient(id,packs,financeMinimum=false,operationId=0), BuyMissingIngredient(id), ReturnToLevels(), ProcessRoundResult(). Keep StartRound wrapper; Retry rejects. SelectLevel allows Ready/result selection but not live match; NextLevel only selects a valid next Ready level. Parent owns StreetGame, StreetSimulation, StreetKitchenProduction and integration/specs.
- UI worker owns only StreetView.cs and new market UI tests; core domain worker owns only new StreetMarket.cs and new domain tests. Preserve prior uncommitted HUD edits.

## Acceptance / focused validation

- [x] Exact individual purchases, no negative stock/cash or duplicate operations; persistence/migration and one-time kit.
- [x] Actual requirements and shared bread; quarter-bottle remainder and independent shortages.
- [x] Minimum-only fiado/debt repayments and all-eleven-level economic solvency.
- [x] Successful physical handoff consumes once; unavailable ingredients cannot produce/deliver; production and movement preserved.
- [x] Victory stock/credit/progression, defeat snapshot/credit, abandonment/reload restoration and settlement/replay deduplication.
- [x] Mercado/Heladera navigation, disabled play until stocked, no retry, no level 12.
- [x] Unity focused EditMode/PlayMode checks and real Mercado/Heladera captures at portrait 16:9, 19.5:9 and 20:9; report unavailable/device validation truthfully.


## Verified 2026-10-13

Unity 6000.6.3f1 / MCP, active Main scene: 29/29 focused EditMode (Market, integration, physical kitchen, specialties), followed by 14/14 final Market/integration checks including strengthened shared-bread and independent bottle-remainder cases. Real default-seed1337 adaptive hiring campaign completed11/11 levels with minimum stock, actual physical deliveries and no economic balance changes. Focused PlayMode passed37/37 existing pointer/navigation + Market checks, followed by7/7 final Market checks (purchase unlock, debounce, stock gate, tabs, hidden callbacks, no level12, drag without purchase, capture).

Nine actual Game View images of representative level5 / test inventory at540×960,540×1170 and540×1200: `Logs/Acceptance/MarketFridge-20261013/` (both tabs and bottom of fridge with partial bottles). Tab hit targets verified at all three ratios. Player save is restored after fixture destruction. `git diff --check` passed at initial implementation. Final release follow-up: EditMode Market/kitchen/specialty 30/30, HUD texture/popup geometry 4/4, and PlayMode Market view/pointer/navigation 39/39 passed. Android 0.2.30/code32 built and package/archive/v2-signature verified; see `android-prototype.md`. No device install or phone-side gameplay test was performed.

Finite-stock integration: if every residual line is an already-completed quota with no physical or preparable supply, use normal departure/cancellation without marking the order complete or granting sales. This prevents sold-out extra tickets blocking FIFO columns; unmet goals/deliverable tickets and unlimited standalone simulations retain their behavior. Rejected settlement/save prevents navigation/progression.
