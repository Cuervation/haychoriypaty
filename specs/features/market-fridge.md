# Mercado y stock persistente — authorized 2026-10-13

## Contract

> Current cash-only contract includes the 2026-10-15 per-attempt victory payout correction below; it supersedes old per-level victory suppression. The separate first-unlock solvency/legacy updates remain scoped compatibility history. Historical release tests and the all-level settlement campaign predate active cooks; they do not certify Racing+ gameplay balance after its layout change. See [active-cooks-mozos.md](active-cooks-mozos.md) for open balance9–11 acceptance.

Keep inventory persistent in StreetGame, but present all stock and purchasing through one Mercado IMGUI screen; there is no separate Heladera/Stock view or tab. Reuse StreetView's existing visual style and controls. Preserve all eleven actual catalogs/quotas, seven final products, kitchen lifecycle, sale prices, routes, hiring/speed costs, existing cover and gameplay HUD. Level 1 enters directly; levels 2–11 enter Mercado first. Mercado access from the selector must not start a match. Purchasing unlocks only after winning All Boys. No retry action: loss returns to levels. Last-level victory never selects level 12.

## Domain and persistence

Add a central ten-ingredient catalog and configurable pack prices in integer cents (150,100,150,100,400,500,250,350,1600,800). IDs: Chorizo0, Pan común1, Paty2, Pan Paty3, Bondiola4, Vacío5, Coca6, Cerveza7, Fernet8, Coca grande9. Last two purchase packs provide four integer portions; all other packs provide one unit. Stable final product recipes: 0→0+1, 1→2+3, 2→4+1, 3→5+1, 4→6, 5→8+9 (one portion each), 6→7. Coca grande uses the requested 2,25 L inventory label; existing visual art is not modified.

StreetMarketData is embedded in the existing versioned StreetGame save (v4). A genuinely fresh save receives its actual level 1 recipe requirement as a one-time free opening kit (currently 200 Chorizo and 200 Pan común); no optional ingredients or money are added. Existing saves with persisted market data retain their exact stock when loaded. Each actual level 1 start fills only any deficit in Chorizo and Pan común up to 200 units apiece; stock above 200 is retained, and unrelated ingredients and later levels are untouched. The grant is included in the saved pre-attempt snapshot, so loss restores the topped-up starting stock and replay does not stack another 200. Pre-Mercado legacy saves keep unlocks and receive one controlled stock migration for their highest unlocked level, not recurring gifts. Lower already-completed levels receive reward recognition watermarks during migration. If that watermark suppressed a legacy completion payout while Mercado cash remained zero and no market purchases were recorded, a one-time transition repair credits exactly the missing-stock purchase cost for the next unlocked level; a persisted marker prevents repetition. A narrow follow-up also recovers v4 saves whose earlier repair marker was already consumed but whose persisted last result still proves a successful zero-payout transition; it is guarded by its own marker, requires zero balance, and preserves the actual stock and purchase history. This is migration recovery, not credit/debt, and does not modify stock, prices, objectives, or normal rewards. Serialize the available balance and exact stock in the same PlayerPrefs JSON operation; ignore the legacy `debtCents` field when loading v4 saves, and retain snapshot/active attempt recovery. No external storage/service.

Purchases validate before changing money or stock. Persist a monotonic purchase operation ID and cumulative purchases; same/stale ID rejects. UI confirm controls also debounce accidental double taps. Every purchase, including minimum missing packs, requires sufficient available balance and is rejected atomically otherwise. There is no supplier credit, debt, loan, or other financing.

On **every real victory**, including a replay of an unlocked level, add the full 50% gross sales + 2×attempt cash remaining to the available Mercado balance (user-confirmed 2026-10-15). Deduplicate by persisted attempt ID, not by level. Defeats retain the existing incremental recognized-sales policy and exact pre-attempt stock restoration, with no leftover bonus. On a first-time victory that unlocks the next level, award only the cash shortfall between current balance and that next level’s exact minimum missing-stock purchase cost; this is a one-time earned progression/restocking reward, not financing. Replays, losses and the final level receive no such reward. No debt deduction occurs. Guard settlement with the persisted attempt ID. The former per-level victory watermark/once-only bonus restriction is superseded: `highestRecognizedSalesByLevel` and `bonusPaidByLevel` remain compatibility/accounting fields, not gates on a new victory. Only the existing defeat sales-increment guard continues to use its watermark. Include the progression award separately in the saved result/receipt and its earned/total amounts.

## Production and handoff

Do not create a second simulation. StreetSimulation optionally binds an IStreetKitchenStock provider (standalone simulation remains unlimited for compatibility). StreetKitchenProduction reserves ingredients temporarily per physical food ID when creating units; failed preparation/burn/discard/cancel releases reservations without consuming permanent stock. Validate physical carried food before committing a recipe once in Kitchen.Consume at successful handoff. Quotas and revenue mutate only after that same valid consume. Reservations protect outstanding minimum goals of other products that share ingredients, avoiding extra Chori tying up bread required for Bondiola/Vacío. Remaining stock survives victory; reservations do not. Loss restores snapshot including bottle portions.

## API reference / historical implementation allocation

- StreetMarket.cs: StreetIngredientCatalog (Names, PackUnits, recipe authority); StreetMarketBalance; StreetMarketData; StreetMarketResult; IStreetKitchenStock; StreetMarket.
- StreetMarket ctor(StreetMarketData, StreetBalance), Initialize(int unlockedLevel), RecoverAbandonedAttempt(), Units(int), PackPriceCents(int), RequiredUnits(StreetSimulation), MissingUnits(sim,id), MissingPacks(sim,id), IsIngredientUnlocked(id,unlocked), HasRequiredStock(sim), MinimumPurchaseCostCents(sim), TryBuy(sim,id,packs,unlocked,operationId), BeginAttempt(sim), SettleAttempt(sim), AbortAttempt(). Properties Data, BalanceCents, NextPurchaseId, LastResult.
- IStreetKitchenStock: bool CanPrepareFood(int product), bool TryReserveFood(int foodId,int product), bool ConsumeReservedFood(int foodId,int product), void ReleaseFood(int foodId).
- StreetMarketResult fields: won, level, grossSalesCents, leftoverCents, halfSalesCents, recognizedSalesCents, bonusCents, earnedCents, totalBalanceCents, consumedUnits[], remainingUnits[].
- StreetGame exposes Market, CanStartRound, TryStartRound(), TryBuyIngredient(id,packs,operationId=0), BuyMissingIngredient(id), ReturnToLevels(), ProcessRoundResult(). Keep StartRound wrapper; Retry rejects. SelectLevel allows Ready/result selection but not live match; NextLevel only selects a valid next Ready level. Parent owns StreetGame, StreetSimulation, StreetKitchenProduction and integration/specs.
- Historical task allocation was UI→StreetView/tests and domain→StreetMarket/tests. It is not an ongoing ownership lock or instruction to spawn agents; assign fresh exclusive file ownership only if a future task benefits from delegation.

## Acceptance / focused validation

- [x] Exact individual purchases, no negative stock/cash or duplicate operations; persistence/migration and starter stock behavior.
- [x] Fresh-save kit equals level 1 recipe requirements; existing inventory is unchanged on load and Level 1 starts top up only shortages to 200 Chorizo/Pan (focused EditMode tests passed).
- [x] Legacy paid flags do not suppress a current victory; its full sales/leftover reward precedes the bounded first-unlock shortfall grant. Older zero-payout transition repairs remain idempotent compatibility paths. Latest valid settled receipts are reconciled once by `victoryPayoutVersion` (eight focused payout cases passed).
- [x] Actual requirements and shared bread; quarter-bottle remainder and independent shortages.
- [x] Cash-only purchases reject unaffordable transactions atomically; debt is not serialized or deducted from earnings; existing v4 saves ignore legacy `debtCents`.
- [x] All Boys Level 1 hides the optional ingredients section and reserves no layout/hit-test area for it; later levels retain the expandable group (focused PlayMode regression passed 1/1, 2026-10-15).
- [x] Successful physical handoff consumes once; unavailable ingredients cannot produce/deliver; production and movement preserved.
- [x] Victory stock/earned balance/progression, defeat snapshot/earnings, abandonment/reload restoration and settlement/replay deduplication. The pre-active-cook cash-only campaign verified stock affordability, wins and replay-award deduplication across eleven levels. This settlement evidence is not final acceptance of current active-kitchen balance9–11.
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

Some progressed saves had Level 1's sales watermark and one-time bonus marked paid by migration although no Mercado balance was ever credited. When that same suppressed payout is detected after a victory, repair the transition before showing Mercado; already-unlocked migrated saves are also checked on load. If the balance is zero, no Mercado purchases were recorded, and the prior-level watermark/result proves the suppressed payout, credit exactly the live minimum missing-stock cost for the next unlocked level. A persisted marker prevents repeat grants. This is migration recovery, not a loan or recurring stipend; it does not change inventory, recipes, objectives, prices, or saves with cash/purchase history. The migrated Nivel 2 example costs $587.50 for its actual missing Chorizo/Coca stock. Focused Unity EditMode validation for this follow-up passed `StreetMarketIntegrationTests` 8/8 and `StreetMarketTests` 15/15, including the same-session victory transition, persistence/idempotency, exact cash-only purchases and the stock gate. The current fix is packaged as Android 0.2.37/code39; artifact checks are recorded in `android-prototype.md`. Full suite, Game View and device validation were not rerun.

## Level 1 stock floor — 2026-10-14

Every Level 1 attempt starts with at least 200 Chorizo and 200 Pan común. Existing surplus is used as-is; partial shortages are filled only to the floor. The start button remains enabled when this bounded top-up is sufficient for the level, and the save records the final stock in the pre-attempt snapshot. No other inventory, balance, level, recipe, objective, or economy value is changed. Unity 6000.6.3f1 batch validation passed: 19/19 focused EditMode Market tests and 1/1 focused PlayMode stock-gate regression. Main-only Development IL2CPP/ARM64 APK 0.2.34/code36 built successfully and its package metadata, ABI, archive integrity and v2 debug signature verified; no device installation or gameplay check was performed.


## Cash-only first-unlock solvency and zero-payout recovery — 2026-10-15

The deterministic real-simulation campaign showed that ordinary Level 1 settlement could leave $7.40 while Level 2 required $10.00 of missing stock. Preserve cash-only purchase gates and all sale/pricing rules: when a victory unlocks a level for the first time, award only the current shortfall to that next level's exact missing-stock cost. This award is persisted as `progressionBonusCents`, included once in the result receipt/income, and cannot be claimed on replay, loss, or the last level. Also recover legacy v4 zero-payout wins missed by the already-consumed migration repair marker; require the persisted success/watermark proof and zero wallet, preserve purchases/stock, and persist an idempotency marker.

Focused Unity EditMode jobs passed: `3ce33e9afa7540f39d8121aaa7cde684` (3/3: all-level actual-handoff/economy campaign, legacy zero-payout save with purchase history, replay deduplication) and `0fb698bfb03c4887ae74ab658ee11891` (6/6: settlement, cash-only gate, migration and legacy transition domain checks). The campaign completed levels 1–11 in sequence, buying required stock with cash, settling wins, preserving consumption, and topping up only first-unlock shortages. A redundant combined-filter request failed before tests initialized; the focused reruns passed. No new device or Game View QA was done.


## Every real victory pays — confirmed 2026-10-15

User explicitly selected **Cobrar cada victoria real**: repeated successful attempts earn 50% of their full real sales plus twice their actual unspent attempt cash. The same attempt cannot be settled twice, but another victory is not a duplicate. Purchases remain cash-only; no changes to sale prices, recipes, quotas, deadlines, starting grants, hired-worker/speed resets or defeat rules. First-unlock stock-shortfall rewards remain distinct and cannot repeat on an already-unlocked replay.

Read-only phone evidence (0.2.39/code41): migrated unlockedLevel10, both old repair markers already consumed, Level2 win gross$2000/leftover$1285 but recognized/bonus/earned all0. Correct omitted reward is$1000+$2570=**$3570**. The persistent245Paty are inherited stock, not proof of a new purchase.

Additive v4 `victoryPayoutVersion` guards `RecoverLegacyVictoryPayout`: on load, before earlier compatibility repairs, reconcile only a valid latest settled successful receipt with matching attempt/level and internally consistent money. Credit only full formula minus receipt components actually paid, preserve prior progression awards and all stock/purchase/unlock data, update wallet/receipt/version atomically through StreetGame save. No active/lost/unsettled/corrupt/overflow receipt invents money. Earlier missing-stock recovery markers do not block this exact earned-income correction.

Focused validation and strict fresh all-level campaign results belong in status; do not infer them from older artificially funded strategy tests or the pre-Mozo campaign.
