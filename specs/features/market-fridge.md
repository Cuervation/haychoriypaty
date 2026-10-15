# Mercado y stock persistente — authorized 2026-10-13

## Contract

Keep inventory persistent in StreetGame, but present all stock and purchasing through one Mercado IMGUI screen; there is no separate Heladera/Stock view or tab. Reuse StreetView's existing visual style and controls. Preserve all eleven actual catalogs/quotas, seven final products, kitchen lifecycle, sale prices, routes, hiring/speed costs, existing cover and gameplay HUD. Level 1 enters directly; levels 2–11 enter Mercado first. Mercado access from the selector must not start a match. Purchasing unlocks only after winning All Boys. No retry action: loss returns to levels. Last-level victory never selects level 12.

## Domain and persistence

Add a central ten-ingredient catalog and configurable pack prices in integer cents (150,100,150,100,400,500,250,350,1600,800). IDs: Chorizo0, Pan común1, Paty2, Pan Paty3, Bondiola4, Vacío5, Coca6, Cerveza7, Fernet8, Coca grande9. Last two purchase packs provide four integer portions; all other packs provide one unit. Stable final product recipes: 0→0+1, 1→2+3, 2→4+1, 3→5+1, 4→6, 5→8+9 (one portion each), 6→7. Coca grande uses the requested 2,25 L inventory label; existing visual art is not modified.

StreetMarketData is embedded in the existing versioned StreetGame save (v4). A genuinely fresh save receives its actual level 1 recipe requirement as a one-time free opening kit (currently 200 Chorizo and 200 Pan común); no optional ingredients or money are added. Existing saves with persisted market data retain their exact stock when loaded. Each actual level 1 start fills only any deficit in Chorizo and Pan común up to 200 units apiece; stock above 200 is retained, and unrelated ingredients and later levels are untouched. The grant is included in the saved pre-attempt snapshot, so loss restores the topped-up starting stock and replay does not stack another 200. Pre-Mercado legacy saves keep unlocks and receive one controlled stock migration for their highest unlocked level, not recurring gifts. Lower already-completed levels receive reward recognition watermarks during migration. If that watermark suppressed a legacy completion payout while Mercado cash remained zero and no market purchases were recorded, a one-time transition repair credits exactly the missing-stock purchase cost for the next unlocked level; a persisted marker prevents repetition. This is migration recovery, not credit/debt, and does not modify stock, prices, objectives, or normal rewards. Serialize the available balance and exact stock in the same PlayerPrefs JSON operation; ignore the legacy `debtCents` field when loading v4 saves, and retain snapshot/active attempt recovery. No external storage/service.

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

- [x] Exact individual purchases, no negative stock/cash or duplicate operations; persistence/migration and starter stock behavior.
- [x] Fresh-save kit equals level 1 recipe requirements; existing inventory is unchanged on load and Level 1 starts top up only shortages to 200 Chorizo/Pan (focused EditMode tests passed).
- [x] A legacy Level 2 save with watermarked Level 1 earnings but no Mercado payout gets only its exact current minimum-stock transition cost once; existing cash/purchase history excludes repair.
- [x] Actual requirements and shared bread; quarter-bottle remainder and independent shortages.
- [x] Cash-only purchases reject unaffordable transactions atomically; debt is not serialized or deducted from earnings; existing v4 saves ignore legacy `debtCents`.
- [x] All Boys Level 1 hides the optional ingredients section and reserves no layout/hit-test area for it; later levels retain the expandable group (focused PlayMode regression passed 1/1, 2026-10-15).
- [x] Successful physical handoff consumes once; unavailable ingredients cannot produce/deliver; production and movement preserved.
- [x] Victory stock/earned balance/progression, defeat snapshot/earnings, abandonment/reload restoration and settlement/replay deduplication.
- [x] Single Mercado navigation, disabled play until stocked, no retry, no level 12.
- [x] Unity focused EditMode/PlayMode checks and real portrait captures; report unavailable/device validation truthfully.
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

## Visual Mercado redesign — approved reference (2026-10-13)

Keep IMGUI, current recipes/prices/persistence/stock gates and cash-only purchasing unchanged. Reuse original club scenery, wood frame, menu buttons, font and kitchen ingredient sprites. Header: one NIVELES action, title, club identity/locality, dynamic objective cards, and prominent available cash. Objectives retain the accepted 1–7 distributions, now with larger individual icons, names and amounts.

The scroll-only Mercado list shows current-goal ingredients first. One collapsed OTROS INGREDIENTES row lists optional products available through unlocked progression; opening it allows stock consultation and explicit purchases, but locked products are never shown. All Boys (Level 1) omits this row entirely because the opening market contains only Chorizo and Pan común; later levels retain the optional group. Each ingredient card summarizes pack price, NECESITÁS/TENÉS/FALTAN and coverage. Only one row's quantity/purchase controls may expand at once; no automatic purchase. The fixed footer keeps minimum-missing cost and COMPRAR FALTANTES beside the primary JUGAR PARTIDO action; playing remains stock-gated. No separate Heladera view, screen-switch state, FIADO or financing.

- [x] Focused pointer/expansion/stock gate and layout checks at 16:9, 19.5:9, 20:9.
- [x] Real visual review for Nueva Chicago (2), Ferro (4), Camioneros (10).

Existing dedicated crest assets cover levels 1–3 only; other levels use a clearly non-official palette/initials identity tile rather than inventing a club crest. No source art is modified. Bread icons reuse the bun-only crop from original sandwich sprites, because separate bread assets do not exist.

Validation before the unified-screen update: Unity6000.6.3f1 via MCP, **9/9 focused PlayMode** passed (job `3bd96811f4fe44cba75d56ef01f63585`), including purchase/stock gates, navigation and real Mercado/Heladera captures. Those historical Heladera captures do not represent the current UI. Capture fixtures use actual goals/prices and controlled stock/balance, restoring the prior test-project save. `git diff --check` passed.

**Unified Mercado/stock UI update (2026-10-14):** The standalone Heladera screen, tab, button and view state were removed while StreetGame's inventory domain/persistence is untouched. A single expandable OTROS INGREDIENTES section lists only unlocked, non-required stock; All Boys still initially shows Chorizo and Pan común. Fixed Mercado footer now includes the missing-stock bulk purchase action and retains stock-gated play. Unity's focused test job failed before starting any tests and the editor MCP stopped responding; no updated visual captures are available. See `specs/status.md` for the validation limitation.

## Level 1 opening kit correction (2026-10-14)

Fresh saves receive the exact level 1 recipe requirements: currently 200 Chorizo and 200 Pan común. Persisted market inventories remain unchanged when loaded; the actual Level 1 start raises each of those two ingredients to at least 200, granting only the shortage and retaining any surplus. The grant is saved before the attempt snapshot, so loss restores the topped-up stock, and replay never stacks a second kit on top of 200. Later levels and optional ingredients are unaffected.


## Legacy transition balance repair — 2026-10-14

Some progressed saves had Level 1's sales watermark and one-time bonus marked paid by migration although no Mercado balance was ever credited. On load, if the next level is already unlocked, the balance is zero, no Mercado purchases were recorded, and the prior-level watermark/bonus proves the suppressed migration state, credit a single amount equal to the live minimum missing-stock cost of that next level. The repair is persisted and is not a loan or recurring stipend; it never changes inventory, recipes, objectives, prices, or a save with existing cash or purchase history. The migrated Nivel 2 example costs $587.50 for its actual missing Chorizo/Coca stock. Focused Unity validation passed 22/22 EditMode Market tests (including save reload/idempotency and exact cash-only purchases) and 1/1 PlayMode stock-gate test. Android 0.2.35/code37 build and artifact verification recorded in `android-prototype.md`.

## Level 1 stock floor — 2026-10-14

Every Level 1 attempt starts with at least 200 Chorizo and 200 Pan común. Existing surplus is used as-is; partial shortages are filled only to the floor. The start button remains enabled when this bounded top-up is sufficient for the level, and the save records the final stock in the pre-attempt snapshot. No other inventory, balance, level, recipe, objective, or economy value is changed. Unity 6000.6.3f1 batch validation passed: 19/19 focused EditMode Market tests and 1/1 focused PlayMode stock-gate regression. Main-only Development IL2CPP/ARM64 APK 0.2.34/code36 built successfully and its package metadata, ABI, archive integrity and v2 debug signature verified; no device installation or gameplay check was performed.
