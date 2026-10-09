# Feature: Street automation and progression

## Authority and scope

This newly requested milestone supersedes the delivery boundary of the [historical Floresta prototype](floresta-prototype.md), not its validation record. [Product](../product.md) retains the exact seven-product catalog and outdoor standing-only setting. [Visual reference](../visual-reference.md) distinguishes observed video evidence from prompt requirements. No copied Food Fever assets, new services/packages, manual movement or complex cooking controls.

Playable Floresta first; reuse the same small scene/simulation for Nueva Chicago, Liniers - Velez Sarsfield, Ferro Carril Oeste and Independiente de Avellaneda. Defaults below are implementation balance choices, not values inferred from unseen video action; all other gameplay balance values remain inspector-editable; the $5 sale price is fixed.

### Official definitive level order — 2026-10-07

User-authoritative ordering. Never reorder, omit a reserved slot or shift a club into another club's index:

| Level | Internal index | Official club / location |
|---|---|---|
| 1 | 0 | All Boys / Floresta |
| 2 | 1 | Nueva Chicago / Mataderos |
| 3 | 2 | Vélez Sarsfield / Liniers |
| 4 | 3 | Ferro / Caballito |
| 5 | 4 | Independiente / Avellaneda |
| 6 | 5 | Racing Club / Avellaneda |
| 7 | 6 | San Lorenzo / Boedo |
| 8 | 7 | River Plate / Núñez |
| 9 | 8 | Boca Juniors / La Boca |
| 10 | 9 | Sindicato de Camioneros / Plaza de Mayo |
| 11 | 10 | Los Redondos / Tandil |

This order governs LevelNames, the selector, internal indices, progression/unlocks, visible names, backgrounds, configuration, asset mappings, navigation and tests. In particular, River owns index7; Boca/Camioneros/Los Redondos own8/9/10. Earlier provisional prepared-art mappings that skipped River are obsolete. The user's subsequent activation request below now supplies the missing configuration; the original order-only instruction did not authorize invented defaults.

### Current per-product level balance — 2026-10-08

This table supersedes the historical aggregate-goal and shared-economy sections below. Product IDs remain Chori0, Paty1, Bondiola2, Vacío3, Coca4, Fernet5, Beer6. Every listed quota is an independently mandatory handoff count; summed totals are informational only and never determine victory.

| Level | Chori (0) | Paty (1) | Coca (4) | Beer (6) | Bondiola (2) | Vacío (3) | Fernet (5) | Seconds |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 All Boys | 200 | — | — | — | — | — | — | 120 |
| 2 Nueva Chicago | 200 | — | 200 | — | — | — | — | 180 |
| 3 Vélez | 180 | 120 | 150 | — | — | — | — | 180 |
| 4 Ferro | 185 | 125 | 155 | 75 | 60 | — | — | 210 |
| 5 Independiente | 190 | 130 | 160 | 80 | 70 | 65 | 65 | 240 |
| 6 Racing | 210 | 145 | 175 | 90 | 80 | 75 | 75 | 255 |
| 7 San Lorenzo | 230 | 165 | 195 | 105 | 100 | 85 | 80 | 270 |
| 8 River | 250 | 180 | 210 | 115 | 110 | 95 | 90 | 295* |
| 9 Boca | 275 | 195 | 225 | 125 | 120 | 110 | 100 | 300 |
| 10 Camioneros | 310 | 220 | 255 | 145 | 135 | 125 | 110 | 300 |
| 11 Los Redondos | 345 | 245 | 285 | 165 | 150 | 140 | 120 | 300 |

*River's deadline is 10 seconds longer than the originally requested 285 seconds. Real runs missed only the last unit in one representative seed at 285 seconds; 295 seconds yielded 3/3 wins with the optimized purchase strategy.

Official unit-sale price by ID is fixed:

| ID | Product | Price |
|---:|---|---:|
| 0 | Chori | $5 |
| 1 | Paty | $5 |
| 2 | Bondiola | $10 |
| 3 | Vacío | $12 |
| 4 | Coca | $5 |
| 5 | Fernet | $12 |
| 6 | Beer | $7 |

A real completed handoff alone increments that product's delivered count and awards its exact price. Price has no effect on order distribution or arrival rate.

| Level | First paid Parrillero | Cocacolero | Premium | Fernetero | Speed-cost multiplier | Opening coins |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 15 | — | — | — | 1.00 | 0 |
| 2 | 20 | 20 | — | — | 1.00 | 0 |
| 3 | 25 | 25 | — | — | 1.15 | 0 |
| 4 | 30 | 30 | 35 | — | 1.30 | 0 |
| 5 | 35 | 35 | 45 | 40 | 1.45 | 0 |
| 6 | 40 | 40 | 50 | 45 | 1.60 | 0 |
| 7 | 45 | 45 | 55 | 50 | 1.75 | 0 |
| 8 | 50 | 50 | 65 | 55 | 1.90 | 0 |
| 9 | 55 | 55 | 70 | 65 | 2.05 | 50 |
| 10 | 60 | 60 | 80 | 70 | 2.20 | 150 |
| 11 | 70 | 70 | 90 | 80 | 2.35 | 200 |

The first Parrillero is free (max five total); each other role has a max of four. Each role's independent successive paid-hire multipliers are ×1, ×2.5, ×5 and ×9, rounded up to a $5 multiple. Speed remains +10% per upgrade to ×1.90; base costs are $5/$10/$15/$20/$30/$45/$65/$90/$125, multiplied by the level factor above and rounded up to $5. Attempt opening coins are a disclosed balance adjustment: levels 1–8 begin at $0, levels 9–11 at the table values. They are not earned revenue, never carry over and reset on a fresh attempt.

Quota-aware demand retains random 1–4 quantities and mixed tickets (up to five unlocked products), weights products by outstanding goal deficit/time and excludes products whose required role is not staffed. Completed quotas stay visible and may still sell for real revenue. Role responsibilities, stations, routes, stock and handoff accounting remain unchanged. Victory requires every unlocked product quota.

- [x] The code centralizes all 11 per-ID quota, hire, speed-multiplier and opening-balance rows; the real simulation identified the Level-8 deadline and Level-9–11 opening-balance adjustments recorded above.
- [ ] Focused Unity EditMode checks verify every table row, attempt-entry path, quota predicate and real handoff accounting after the adjustment.
- [ ] Representative real-simulation strategies A–D complete all levels; results and any limitations are recorded in [status](../status.md).
- [ ] Review quota HUD/results and portrait layout in the active Unity Game view.

### Historical aggregate goal/demand activation — superseded 2026-10-08

The values and acceptance below preserve the 2026-10-07 configuration history only. Use the current per-product table above for active goals, timers, economy and acceptance.

User authorizes integrating all remaining clubs using level5 as the baseline. Keep every new duration at300seconds and all seven existing products in order `0,1,2,3,4,6,5`. Increase goal/demand10% per successive level; whole-unit goals round normally. Preserve level1–5 values, $5 sales, shared hire/upgrade costs, specialty/FIFO rules and fresh-attempt baseline.

| Level | Club | Goal | Seconds | Demand multiplier |
|---|---|---:|---:|---:|
| 6 | Racing | 121 | 300 | 1.65 |
| 7 | San Lorenzo | 133 | 300 | 1.815 |
| 8 | River | 146 | 300 | 1.9965 |
| 9 | Boca | 161 | 300 | 2.19615 |
| 10 | Camioneros | 177 | 300 | 2.415765 |
| 11 | Los Redondos | 195 | 300 | 2.6573415 |

Use the existing two-column mural-card selector in pages of6, native pointer targets restricted to the visible page, and the established standard buttons for previous/next/back. All Ready/win/loss navigation returns to the same selector; old save unlocks remain intact. Last level must win without accessing level12. Reuse prepared club murals/fans, cropped as scenery above the shared counter rather than replacing worker geometry. Camioneros reuses Ferro outfits and matched Cocacolero art applies to all specialist levels.

- [x] Eleven catalog/config/scene entries import and focused progression/save/legacy-fallback checks pass.
- [x] Both selector pages and locked/unlocked touch navigation work; all six actual-goal rounds finish with earned upgrades before300seconds. Final level wins without a next entry.
- [x] All six newly playable clubs reviewed at1080×1920 in Unity with correct wall/fans/pennants/stations; final console0errors/0warnings.33/33 focused EditMode,5/5 PlayMode and6/6 final selector/theme rechecks pass. Native Android validation not performed.

## Rules and state contract

### Mandatory club trifulca / level Definition of Done — 2026-10-07

Every registered/future level is incomplete without its own scenery and one complete fan definition containing Front, Walking and Riot resources, variant count and explicit atlas/frame layouts. Normal, walking, raised-stick and down-swing frames must depict the same character/outfit selected deterministically by customerId. Each variant requires two distinct angry wooden-stick poses, not cheering/fists substituted for a missing frame. Ferro artwork is explicitly shared with Camioneros as requested; no official level may depend on the generic riot fallback.

Use ClubVisualTheme's fan definition for regular crowd, anger crossfade and riot frames instead of growing level-index if/else lists. Retain generic sprites only as emergency fallback. Structural tests traverse all registered levels and fail on missing resources, incomplete layouts, wrong variant capacity or generic-only RiotFans. New levels require Front/Walking/Riot art, integration and visual loss-state review before completion.

Preserve the real queued customers/positions/IDs, gradual anger, unscaled pose alternation, destruction/impact/debris/shake and GAME OVER/VOLVER. No economy, difficulty, products, workers, time or progression changes.

- [ ] All11levels have complete non-generic two-pose riot definitions and identity-consistent assets.
- [ ] Structural tests enforce this Definition of Done for existing and future entries.
- [ ] All11loss sequences visually reviewed; both poses, real queue, unscaled animation and VOLVER validated.

### River visual identity — initial art checkpoint, level8 / index7 (2026-10-07)

Reinterpret the user's two specific River murals as an inked cartoon wall: A preserves the Libertadores trophy / historical-figure collage; B preserves the dense supporters / red-white flag composition. Keep the All Boys master counter, public/player floors and existing queue/worker/station geometry. Reuse paired6×4 complete-body atlases for exactly12 researched outfit variants, preserving their garments across cheer, directional walk and riot poses. Use real Tienda River product photographs and record per-variant SKU links in `Assets/Art/Street/RiverVisualReferences.md`; no photo textures or floating clothing overlays. River's theme is red/white, clear-day neutral lighting. The initial art task reserved index7 without activating gameplay and used an editor-only nonserialized preview. The subsequent activation section above supersedes that restriction; rendering now reads the actual simulation level.

- [x] Two source murals reinterpreted;12 retail-derived complete-body outfits with matching walk/riot variants prepared.
- [x] Unity imports and focused palette/atlas/index checks pass; live catalog remains unchanged.
- [x] River-only in-editor visual preview reviewed after final alpha cleanup: normal orders,12 front outfits, left/right walk and riot poses. River's full-width wall keeps portrait aspect ratios; counter/actor geometry and garment continuity preserved. This is not playable level8 certification.

### Master scene perspective and fresh-attempt baseline — 2026-10-07

This request supersedes the historical per-backdrop counter heights, Chicago-only handoff/approach height and within-level purchased-team persistence at attempt entry. All Boys is the spatial root; Chicago validates the same template with specialist drinks. `StreetSceneLayout` owns the root 540×960 canvas / 940×1673 art geometry, counter top/front, customer clipping/offset and common player-side service line. `StreetWorkstationLayout` retains reference station sizes and obstacle-safe pickup routes. Background art must fit this geometry, never determine it. Recompose later club walls as wide mural scenery above the queue and reuse the root counter/public-ground/player-floor composition; preserve specific murals and clothing. Scenery-only neutral/warm/cool atmosphere must not tint characters, orders or controls. Do not add playable clubs, change camera, prices, products, goals, durations or demand.

Every construction/load, level selection, next-level entry, StartRound and retry begins with exactly one Parrillero, no Premium/Cocacolero/Fernetero, SpeedLevel 0 and ×1.00. Hires/upgrades remain effective within a playing attempt; unlocks and fixed prices remain saved. Legacy purchase fields stay schema-compatible but must not restore purchases into a fresh attempt.

- [x] Common geometry replaces background-dependent counter/clipping and Chicago-only worker service heights.
- [x] Later club art respects the All Boys template while retaining its specific mural identity and distinct readable atmosphere.
- [x] All five current levels verified visually with the same crowd/counter/workstation depth and scale.
- [x] Focused checks cover fresh load/start, level selection/advance and retry baseline on all current levels.

- Product IDs have the fixed official sale prices in the current per-product balance table; never show price-edit controls or allow legacy saved values to override them. Selecting any unlocked level starts it directly. Preserve catalog IDs, sales accounting and the save schema. Only a successful real handoff increments the corresponding delivered-product count and awards that product's official price. Price must not affect demand distribution or arrival rate.
- A new attempt starts with the configured opening coins in the current balance table; discard any previous attempt balance and saved coins on level selection, next level, retry and Start. Opening coins are not earned revenue, and coins earned during the attempt remain spendable only in that attempt; no carryover or refund. Successful advancement resets the next level to one Parrillero and speed ×1.00; retry restores that same team/speed baseline and that level's configured opening coins. Customers automatically enter, move to reserved standing positions, wait, receive products and exit. Support up to 21 simultaneous customers in seven columns/three rows; arrival rate and capacity are configurable per level.
- Each Floresta customer requests 1–4 choris (uniform random inclusive, including the first arrival). Complete the requested units and animate receipt/departure; then the existing people behind advance in the same column and only its tail may receive a newcomer. Across all levels, preserve each column as FIFO: workers serve only its settled front customer, never a waiting person behind or a newcomer placed ahead. No forced999 exception in this level. At Nueva Chicago, orders are randomly chori-only, bottled-Coca-only, or both; each quantity is independently 1–4. For mixed requests, the Parrillero completes Chori and the Cocacolero completes Coca in parallel. At Level 3, Liniers - Velez Sarsfield, customers may request any non-empty subset of Chori, Paty and bottled Coca, independently 1–4 each. Their lines stay in catalog order; one normal Parrillero owns Chori then Paty, while a Cocacolero owns Coca and may work in parallel. Ferro and Independiente customers may request any non-empty subset of that location's unlocked catalog, preserving catalog order, up to five distinct product types; each automatic quantity is independently 1–4. Across all levels the complete model rejects tickets with more than five distinct products, and automatic generation never repeats a product type. Across all levels, the order bubble shows the first two pending product types and quantities, skips every zero-quantity type, and adds a small “+N más” footer counting hidden pending types (not units). Each completed line immediately leaves the display window so the next original-order line appears; a lone line is centered, and the content-sized bubble stays bounded. The bubble is a presentation window only: it never changes real quantities, reservations, FIFO, or worker delivery. Individual patience remains visible. A handoff decreases the corresponding quantity by exactly one, increments delivered-unit total by one and earns exactly $5. There is no counter jump, reward at pickup, or fake timer-only delivery.
- Workers automatically reserve an outstanding unit, move to its station, pick up, carry to the customer's handoff position, deliver, then repeat. A configurable short station/condiment delay can remain automatic, but must not interrupt the circuit with manual cooking. Food always includes bread; condiments remain automatic and are never extra products.
- For each outbound, return, and successive-unit leg, compute the shortest traversable route from the worker's actual current feet position to the exact pickup/handoff. Use a direct diagonal when its full segment is clear; otherwise route around active solid station bases using the 36×12 feet geometry. A table/Fernet solid is only its front base band; a barrel uses its full footprint; an active grill uses its physical bounds. Compute the return independently; never force the outbound waypoints in reverse or send an employee to its home point between assigned units. Keep reservations, specialty ownership, physical item IDs, pickup/handoff delays, speed and delivery/income rules unchanged. Do not introduce worker-vs-worker collisions, NavMesh or packages. Apply identically to all four worker roles and every catalog/future level.
- On Nueva Chicago, keep idle workers and every handoff at logical y=485 on the player-side floor, below the counter front (bottom y=384). Their station route may approach the grill/table/barrel, but must never send the parrillero across or visually through the counter. Vélez uses one wide two-zone Chori/Paty grill plus the separate Coca barrel; food approaches use the clear side lanes around the grill, and the Cocacolero uses the barrel approach. Do not walk a worker through the hot cooking surface. Other levels retain their existing handoff positions.
- Hire up to five independently moving workers by default. Reservations prevent duplicate delivery to the same final unit; clients that leave release reservations, and in-flight workers cancel safely without earning coins or decrementing another order. Delivery refreshes customer patience. Waiting expiry makes the customer leave and compacts that same FIFO column, without changing the identities/orders/patience of those advancing.
- Simulation position/state is authoritative: revenue and quantity changes are gated by arrival and handoff, not by an unrelated view timer. View animates entering/waiting/receiving/exiting customers and directional worker walking/pickup/carry/handoff; coin amount/effect, upgrade and hire feedback reflect actual events.
- Speed/hire purchases use the per-level tables above, role-independent hire tiers, five Parrilleros/four of each specialist caps, and a ×1.90 speed maximum. Purchases update the next cost and affect productivity immediately. Reject insufficient-funds or capped purchases without side effects, and dispatch each tap exactly once.
- Every level wins immediately only when all its per-product quotas are met by real handoffs; a total-unit sum is informational and cannot substitute. At the deadline, any outstanding product quota causes loss and the level's normal trifulca. Freeze remaining handoffs on victory. Results show completed/pending products, delivered units and time remaining; successful completion unlocks the next location. Do not require clearing every customer to win.

## Historical shared upgrade economy — superseded 2026-10-08

This is a temporary test configuration, not a permanent balance decision. All five levels in the current runtime catalog use one TEST_ECONOMY_PROFILE; StreetBalance.levelUpgradeCostProfileIds maps each level to a profile, so future club-specific curves require data configuration rather than level-specific code. Main serializes the same profile and mapping as the code defaults.

The first starting Parrillero remains free. Subsequent Parrillero hires use the shared four-row curve. Cocacolero uses that exact same hire-cost array, indexed from zero Cocacoleros: its first independently hired role costs $15 when that role is available. Existing role behavior, products served, speed, route, caps, saves and progression are unchanged. Exhausted purchase curves display MAX and reject purchases without charge.

| Upgrade | Starting value | Successive cost by purchase |
|---|---:|---|
| Parrillero hire | One starting worker is free | $15 → $30 → $60 → $100 → MAX |
| Cocacolero hire | No new starting worker | $15 → $30 → $60 → $100 → MAX |
| Speed | ×1.00, free | ×1.10 $5 → ×1.20 $10 → ×1.30 $15 → ×1.40 $20 → ×1.50 $30 → ×1.60 $45 → ×1.70 $65 → ×1.80 $90 → ×1.90 $125 → MAX |

Speed keeps the current +10% per purchase and x1.90 cap. The former later-level hire curve ($200/$500/$1200/$2800) and speed curve ($25/$40/$65/$100/$160/$250/$400/$640/$1000), plus the prior Floresta-specific split, are superseded and must not affect current gameplay. Fixed $5 product prices, coins, timers, goals, role caps, per-level staff/speed resets, purchases within the active attempt and legacy-save clamping remain unchanged.

- [x] One data-driven profile supplies both hire-role curves and the speed curve; all five current runtime levels map to profile 0 in Assets/Scenes/Main.unity.
- [x] Legacy serialized cost fields remain hidden for scene/save compatibility but are not read by runtime pricing.
- [x] Focused EditMode tests cover exact hire/speed values, affordability, deductions, +10%, x1.90/MAX on all five runtime levels, Cocacolero's 15/30/60/100/MAX sequence where available, and per-level profile overrides.
- [x] Focused PlayMode test covers upgrade-card purchase/debit/next-price/MAX behavior through the live view.

**Validation:** Unity 6000.6.3f1 EditMode economy tests passed 12/12; PlayMode UpgradeCardsUpdateNextCostsAndStopAtMaximum passed 1/1. The former full StreetSimulation fixture contains unrelated existing failures and was not used as the result for this focused change. Current active clubs are All Boys, Nueva Chicago, Liniers - Velez Sarsfield, Ferro Carril Oeste and Independiente de Avellaneda; this list is read from runtime configuration, not the stale pasted Argentinos/Vélez ordering. No APK, device install, commit or push was performed.

## Historical progression defaults — superseded 2026-10-08

Keep product IDs/saved price slots stable: chori (0), paty (1), bondiola sandwich (2), vacío sandwich (3), Coca-Cola 600 ml bottle (4), Fernet con Coca + ice in a 1-liter cup (5), beer can (6). Nueva Chicago offers IDs 0 and 4; Vélez adds Paty; Ferro offers IDs 0,1,2,4,6; Independiente offers all seven in order 0,1,2,3,4,6,5. Each ticket still caps at five distinct unlocked types. Every unlocked product has its own original sprite and station; every catalog product is sold for the same fixed $5 in every level.

| Location | Unlocked product count | Unit goal | Round seconds |
|---|---:|---:|---:|
| Floresta / All Boys | 1 | 200 | 120 |
| Nueva Chicago | 2 | 200 choris + 200 Coca bottles | 180 |
| Liniers - Velez Sarsfield | 3 | 65 | 240 |
| Ferro Carril Oeste | 5 | 85 | 270 |
| Independiente de Avellaneda | 7 | 110 | 300 |

Demand, duration, goal, limits, speed, station delays, patience and upgrade costs remain configurable. Persist unlocked level, prices and purchased upgrades/workers in a versioned JSON value under a dedicated PlayerPrefs key; reject/reset malformed or incompatible data safely. The legacy coins field remains JSON-compatible but its stored balance is discarded on load and never transferred to a new attempt. On the one-time v1→v2 migration, reset inflated development workers/speed to one worker and base speed while preserving unlocked level and prices; v2 saves retain purchase fields for schema compatibility but new loads never restore those purchases; successful advancement resets team/speed for the next level while retaining unlocked progress and product prices. Constructing or starting any fresh round discards previous staff/speed upgrades (one parrillero, SpeedLevel0 = x1.00), preserving unlocks and other product prices. This is progression persistence, not full in-flight round resume. Define reset/replay handling explicitly in code and test it so restored purchases are not charged again.

## Minimal integration

`StreetGame` owns lifecycle, inspector `StreetBalance`, management commands and persistence; `StreetSimulation` is pure deterministic runtime data/logic with bounded substeps and capped frame catch-up; `StreetView` renders actual state with original replaceable sprite assets. Main uses the new pair, disabling legacy `GameController`/`PrototypeView` there while retaining their files/tests. Use one safe-area transform for drawing/hit-testing. Preserve a uniform sprite scale but expand the portrait logical height with available display aspect instead of fitting a fixed 540×960 canvas and leaving gameplay letterbox bands. Use Input System bridge on editor/desktop and native GameActivity IMGUI events on Android, never both dispatching the same action.

No new framework, service, generalized content pipeline or package is necessary. Fixed customer/worker caps and bounded catch-up make load controllable; actual device profiling remains required before any performance claim.

## Historical integration acceptance record — prior milestones

The active balance acceptance is the checklist at the top of this file. The evidence below records earlier integration milestones, not approval of the new quota balance.

- [x] Main opens/runs without errors and only the intended new runtime/view owns gameplay/input.
- [x] Historical imported art matched the then-current reference composition before the open-street change below; no placeholder geometric actors, copied assets, 3D/isometric/cenital view or seated diners. All seven product sprites/stations are connected to their unlocked levels.
- [x] Fixed $5 prices cannot be changed; level selection starts directly and no former price-popup location has an active hit target. Start works with editor mouse and Android touch, with aligned safe-area hit bounds.
- [x] Dozens of customers enter and group in rows; each shows correct product, remaining quantity and patience with actual enter/wait/receive/exit states.
- [x] Floresta supporters rotate across nine complete black/white/gray/cream All Boys-inspired outfits, integrated directly into matching full-body front, side-walking, and raised-stick/swing timeout sprites. The same customer ID selects the same outfit in all contexts; no shirt overlay; blue/pink/goalkeeper palettes remain excluded.
- [x] Nueva Chicago supporters rotate across eight complete green/black/off-white outfits in integrated front, side-walking, and paired angry timeout sprites. The same customer ID selects the same outfit across regular and riot contexts; no garment overlays.
- [x] Focused EditMode regression verifies both 3×3 atlases, their alpha/import settings and crop bounds, all nine identities in both poses, deterministic wrap, and absence of a Level-1 clothing overlay.
- [x] Captured and visually reviewed the corrected Level-1 crowd in Unity Game view.
- [x] A configured 999-unit order decreases by exactly one on each real handoff; cancellations/departures cannot duplicate decrements or earnings.
- [x] One worker visibly performs station→pickup→carry→handoff repeatedly; hired workers coordinate reservations and increase measured throughput.
- [x] Coin effects/amounts occur only for real deliveries. Historical/later-level $5 speed and $15 hire initial costs, escalation, affordability and immediate response verified; Floresta now uses the separate rules below.
- [x] Win/lose/results/replay work end-to-end; five increasing levels unlock exactly the catalog counts above, never an eighth product.
- [x] Versioned save/load restores supported progression/economy state and handles corrupt data; no claim of full-round resume without a dedicated test.
- [x] Historical v1→v2 migration resets development starting staff/speed to 1 worker and ×1.00 while preserving raw save fields; current loading discards coins under the round-local rule below.
- [x] Directional walk/carry/pickup/handoff, customer states and management feedback are actual animations, not static claims.
**All Boys integrated-outfit validation (2026-10-05):** Updated after the user reported that the earlier four uniform-only looks did not show the collected clothing. Unity6000.6.3f1 complete EditMode assembly passed **116/116** and PlayMode passed **21/21**, zero failures/skips. This includes nine-outfit front/walking atlas import, deterministic selection, crop bounds and no-overlay checks. Game View at 1080×1920 was visually inspected and saved to `Logs/Acceptance/AllBoysOutfits-20261005/allboys-nine-outfits-playing-screen.png`. Play Mode was stopped and the exact prior editor PlayerPrefs JSON restored. No APK/device change was requested. Earlier four-pose results are historical.
- [x] Targeted simulation/input tests, Unity Main playthrough, portrait comparisons and Android player verification recorded (16EditMode/9PlayMode; actual native24-unit round).
- [ ] Physical device/GPU profiling before mobile-performance certification; pure21client/8worker simulation measured, not mobile FPS.

## Validation record

Editor:16/16 Street EditMode,9/9 Street PlayMode, Main actual24-unit victory and persistent reload; original atlas/import/layout reviewed. Isolated five-level wins and real wrapper corrupt/versioned save checks passed. See [status](../status.md) for exact jobs and limitations. Native Android slider0/60,Start,unique hire/speed,24-unit victory,next unlock and restart/save verified; final rebuilt APK installed; native product selection and unobstructed HUD confirmed. Physical performance remains pending; checked simulation/progression criteria do not imply five distinct club backgrounds, audio or physical-device certification.

## Historical three-minute Floresta deadline-only trial

User approved this duration experiment. Focused simulation acceptance passed22/22 (job `e1d05e79e164439f94d13b0e4c107315`): level1 staysPlaying beyond24 and continues earnings until180s; deadline evaluatesWon/Lost and freezesclock; otherlevels still early-win. Existing progression/economy save is unchanged; Android0.2.1 built/installed and native60units with172s left verified; full native deadline check pending. No new waves/products/art/UI changes.

## Parrillero art/name requirement — 2026-10-03
- Staff are visibly named Parrillero. Use new original16-pose heavy shirtless/dirty-white-apron character and matching hire portrait; keep fans, gameplay, prices and progress unchanged.
- [x] New RGBA textures/16 named sprites import without resizing or mipmaps; bounds/alpha reviewed, bottom-center pivots verified.
- [x] Runtime worker/hire visuals and Parrillero label reviewed in real editor Game view;3/3EditMode and1/1PlayMode passed. Native visual/performance review remains pending.

## Diagonal Parrillero gait — 2026-10-04

Keep the user's selected Parrillero identity and original16 cardinal/action poses. Add an original transparent eight-frame diagonal cycle to cover front-left/front-right and back-left/back-right movement; each direction has opposite leg-stride phases. Select the matching diagonal pair from the worker's actual target vector, alternating at8fps. Preserve existing cardinal, pickup and handoff poses, food rendering, worker scale/ground anchor, simulation timing and delivery behavior. Near-cardinal movement keeps the existing cardinal pair to avoid direction flicker.

- [x] Source contains eight reviewed bounds and selects/alternates both phases in all four diagonal directions while retaining cardinal selection.
- [ ] Unity visual review confirms natural leg separation, stable foot anchor and no art seams during station/customer travel.

**Validation:** Unity6000.6.3f1 `StreetArtTests` passed **35/35**, including new atlas-alpha/native-size/subasset checks and all four diagonal directions with alternating8fps gait phases; import/compiler errors none. Game-view animation review, Android build and device update were not part of this source refinement.

## Floresta small orders and fixed economy — 2026-10-04
- [x] Historical fixed-$5/no-slider, drag rejection, replay/start and saved-price normalization checks passed before the popup removal below.
- [x] Historical +10%/$5 speed and $25 helper costs were verified before the cost/reset change below; these results do not validate the current $25/$200 rules.
- [x] Automatic1–4 orders, exact per-unit income, completed-customer exit and replacement verified.
- Historical slice preserved180seconds/goal24 and original art/progress; current Main uses the200-unit early-win rule. User explicitly requested noAPK for this change; only focused editor checks.

Current editor verification:32/32simulation+9/9art jobc900430c9df54e1ab041b0a2475b6e54 and11/11pointer job81416a6df3c746eb8fa26d209fbcf885,0failed/skipped. Ready/Playing Game-view confirms no price slider,$5/$25/+10%,1–4 orders and large parrilla. First-level180s deadline regressions passed. The original noAPK instruction applied to that source-change turn; subsequent large-parrilla request explicitly authorizes APK build, not phone install.

## Startup cover/title/menu — 2026-10-04

Keep the transparent logo/menu unchanged. The latest full-screen Parrillero refinement below replaces the cover composition only. Using unscaled time, hold the fully visible cover for at least 4 seconds, reveal the logo with the existing brief warm flash, and hold the fully visible logo for at least 4 more seconds before showing **Jugar** and **Salir**. Keep cover/logo/menu visible until an explicit choice, rather than automatically fading into Ready. Buttons use original glossy cyan→blue rounded geometry and white, dark-outlined Luckiest Guy lettering inspired by the supplied blue NEXT reference; do not reuse its pixels. Ship the Apache 2.0 font license.

Jugar opens the level selector after the existing 4+4-second presentation; it does not start a round. Only levels at or below persisted `UnlockedLevel` are active. Selecting any unlocked level starts its fixed-price round directly; there is no price/setup screen. The victory result has one action, **Salir**, which resets the finished attempt and returns to the level selector (the next location is unlocked only after a win). Presentation-menu Salir still calls `Application.Quit` in the player (stops Play mode in the Editor). Block hidden gameplay/selector actions during intro and locked-card actions at all times. Keep the shared safe-area transform and existing native Android/desktop input ownership; no extra scene/package, unlock reset or new products; the round-local coin reset and first-level cost/staff/speed reset rules below apply.

- [x] Cover is fully visible for ≥4s before logo begins; logo is fully visible for ≥4s before buttons appear, including at timeScale 0.
- [x] Persistent menu shows readable blue Jugar/Salir; no hidden level/price/upgrade/start input leaks through.
- [ ] Mouse/native Android touch: Jugar opens the level selector; any unlocked tile starts that level directly; only the preceding victory unlocks the next tile; victory-popup Salir returns to selector; presentation Salir exits the player.

**Implementation/validation:** Source updated (0.28s cover fade + 4s cover hold + 0.35s logo reveal + 4s logo hold = 8.63s before buttons). Existing startup regression source adapted, **not run**. User explicitly deferred tests, Play/device review and APK generation until later combined validation. Prior 0.2.4 evidence below does not validate this changed flow.

### Historical startup 0.2.4 validation — before the menu change

Use the selected user-supplied cover and separate transparent logo unchanged. The 4.1-second unscaled intro shows the cover first, reveals the logo with a single short warm flash, then fades to the existing Ready screen without starting a round. Gate both native Android IMGUI and desktop bridge actions during the intro; no extra scene, package, video playback or gameplay changes.

- [x] Both resources import at source dimensions with Android RGBA32/no mipmaps; 11/11 focused art tests passed.
- [x] Intro blocks actions, ends with timeScale 0, stays Ready and permits Start afterward; 12/12 editor pointer tests passed (job adfc5a956d4f48548c73f34aa2caec0d).
- [x] Actual 1080×1920 Game-view cover/logo-flash/Ready captures reviewed in ignored Logs/Acceptance/Intro/.
- [x] Android0.2.4/code6 APK built; V2 signature/package/ARM64/min26/target36 verified. User subsequently requested phone installation: 0.2.4/code6 install-r Success and cold launch Statusok verified; new native appearance/touch remain unchecked (see Android delivery).


## Floresta without price popup — source update (2026-10-04)

Ready never draws the price panel/background, product/amount/demand summary or hidden product hit targets; only non-modal Jugar and level navigation remain. The all-level fixed-price contract below replaces historical later-level price selection.

- [x] First entry/replay/return to Floresta has no price popup and still charges $5 per delivered chori; the later all-level fixed-price contract is recorded below.

**Validation:** Implementation only; user explicitly requests no tests, Play/device testing or APK. Historical passing records do not verify this new UI change.


## Historical Floresta fixed upgrade costs and fresh-round baseline — source update (2026-10-04)

This checkpoint used fixed $25 speed/$200 helper costs; these prices are superseded by the progressive tables above. Every newly constructed or started first-level round has one parrillero and x1.00 speed (zero purchased speed upgrades), including replay, saved first-level starts and returning to level 1 before Start. Remove old workers/reservations before starting; reset coins for every fresh attempt (no refund), but preserve prices, unlocked levels and later-level purchases.

- [x] Historical first-level UI/purchases used fixed $25 speed/$200 helper costs; the new progressive-table criteria above supersede these prices.
- [x] Fresh first-level launch/replay/start returns to one parrillero and x1.00, preserving unlocks and later-level staff/speed behavior while clearing coins.

**Validation:** Source-only changes. Existing hard-coded old-cost/staff-preservation test assertions need adapting before later combined checks. User's no-tests/no-APK instruction remains in force; no Play/device/build or compilation verification this turn.


## FIFO customer columns — source update (2026-10-04)

The existing seven columns/three rows are FIFO lanes in every level. Once a served or expired customer starts leaving, release its queue slot; the older people immediately behind advance continuously into the earlier positions of that column. Preserve customer ID, product, remaining quantity and accumulated patience; keep bubbles visible while advancing. New arrivals append after the last queued person in an available lane, never into a gap ahead of that lane's existing people. Empty lanes may accept their first customer; initial admission retains the original front-row-first layout. Leaving people still count toward the bounded population until their exit walk finishes.

Only settled front-row Waiting customers receive worker reservations; Receiving front-row customers may finish existing reserved handoffs. No service to rear rows or moving customers. Cancel invalid/moving/departed reservations without decrement or income; advancing itself never delivers an order. Prices, recipes, upgrades, first-level reset and progression stay unchanged.

- [x] Served front departure advances the same-column second/third customer; newcomer joins only the back, with no teleport or order reset.
- [x] Timeout/multiple departures, partial/empty lanes and bounded capacity retain FIFO/unique slots and no skipping service.
- [x] Concurrent workers and advancing customers cannot duplicate a unit/reward or leave stuck reservations; existing orders and patience move with their owners.

**Validation:** Source-only implementation; no tests, Play, visual/device review, APK or compile/import confirmation by the user's standing instruction. Historical free-slot/rear-row service checks do not validate this new policy; update affected fixtures before combined validation.


## Unobstructed crowd street — source update (2026-10-04)

Use the versioned v4 backdrop with upper framing ending at the mural, no upper stadium, stall roof/branding/header/boards or tall supports, and broaden the empty customer street behind the existing low counter. Remove old sign-bound product overlays; customer order/patience bubbles stay. Keep FIFO feet anchors170/240/310, counter/worker/station and safe-area/HUD layout unchanged; no gameplay or product changes.

- [x] Versioned original backdrop generated and statically reviewed; previous art/metas preserved.
- [x] Actual Game/device view confirms clear crowd/orders and correct counter/UI alignment with the new background.

**Validation:** Source/static-asset review only. No tests, Play, APK or import/compilation confirmation; earlier art/Game-view records do not validate v4.


## Animated waiting supporters — source update (2026-10-04)

Waiting supporters visibly breathe, shift their weight/lean and occasionally bounce twice in a cheering gesture. Vary phase/rhythm per customer so the crowd is not synchronized. Reuse each original front-facing fan and its Floresta clothing, transforming both together around the feet; preserve existing walking/Advancing and receiving feedback. Order/quantity/patience indicators remain anchored to the real customer position and readable. Use the existing simulation-driven AnimationTime only: no new timer, logical queue movement, order decrement, sale or reward comes from idle motion.

- [x] View-only staggered idle transforms implemented; original identities, wardrobe and shared safe-area matrix retained.
- [ ] Game/device view confirms visible varied motion in all three rows, attached clothing and stable readable bubbles, including transitions to advance/receive/leave.

**Validation:** Source review only. No tests, Play, APK or Unity compilation/import confirmation by the user's standing instruction; animation appearance/performance remain unverified until combined checks.


## Historical thousand-choripán goal and corner counters (superseded by current200goal) — source update (2026-10-04)

User confirmed180seconds and victory immediately at1000actual choripán sales. Defaults, fallback and Main serialized goal use1000; deadline-only flag defaults false. The existing real unit handoff is still the only source of Delivered/coins; stop worker iteration at the winning first-level handoff, not at pickup or on an unrelated UI clock. Later goals/durations and all fixed-price/cost/reset/FIFO/progression contracts remain.

Draw the generated100%gold Chori y Paty relief coin with current coin balance in the upper-left safe-area corner; show actual Delivered/Goal without spaces (e.g.100/1000) with choripán icon in the upper-right. Keep these32px-high panels above the back-row bubbles; use generic progress icon later, retain countdown in lower band. No duplicate bottom balance/goal. The new coin also replaces the generic coin in actual +income feedback; no colored food/flag accents, no currency-rule change.

- [x] Source goal/defaults/Main early-win policy and both HUD corners implemented; final transparent gold-only coin generated, statically reviewed and retained with new meta/provenance.
- [x] Exactly1000first-level successful handoffs win immediately without same-slice overshoot;180seconds below1000lose and replay/later goals remain correct.
- [x] Game/device review confirms new coin, readable corner numbers and no overlap with back-row bubbles/cutouts or later-level controls.

**Validation:** Source/static-art review only. No tests, Play, APK/device changes or verified Unity compilation/import by user instruction. Historical24-unit/deadline-only/HUD tests and captures do not validate this update; adapt relevant fixtures before the later combined checks.


## Combined validation — 2026-10-04 (current accumulated implementation)

Latest user explicitly authorized tests and APK, superseding earlier deferral. Existing Unity6000.6.3f1 on Android imported/compiled the accumulated changes with0console error entries. Relevant fixtures were updated, not production balance weakened.

- StreetSimulationTests + StreetArtTests EditMode: **52 completed, succeeded, no reported failures**, job`be5287b37f464f0e9e105241e28ac196`. New regressions exercise exactly1000 real handoffs/early freeze, eight concurrent handoffs at999, served/expired front-lane advancement and tail-only admission, front-only reservations,90seconds concurrent slot/reservation/exact-income invariants, and restart reset without refunds. Native-resolution new coin/background and startup font also pass. The1000-sale test accelerates travel solely as a test fixture; it is not a claim that ordinary new-save play is balanced to1000within180s.
- StreetPointerTests PlayMode: **12/12 passed,0failed/0skipped**, job`bb035620ab614035a04366439ee8e511`; actual mouse/touch menu/start/drag/purchases/replay/load, safe-area geometry and unscaled4+4seconds guards. Exact previous PlayerPrefs restored by fixtures. Salir was deliberately not invoked inside the editor test runner because it stops Play.
- Actual1080×1920 Main Game-view menu and dense21-fan gameplay reviewed: original logo, blue outlined Jugar/Salir, mural/open crowd street/counter-only stall, first-level gold coin275 and7/1000 HUD, $25/$200 buttons, team1/×1.00. Two animation-time samples with identical positions/orders/coins/timer show staggered body+garment movement while bubbles remain anchored. Captures in ignored`Logs/Acceptance/Combined/editor-*.png`. Main Play stopped and exact editor progress restored.
- Later seven-product Ready/Playing Game-view captures also reviewed: independent price panel/tabs, all seven stations, generic right progress icon and corner counters remain unobstructed. Editor-only seeded unlock was restored to exact original saved JSON afterward.
- Android0.2.5/code7 build, packaged adaptive logo and later Motorola install/launch verified with unchanged saved XML; native touch/Salir and launcher-mask/device review remain pending (USB disconnected again after launch). No physical FPS/thermal or extended device stress certification implied. See`android-prototype.md` current delivery evidence.


## Edge-to-edge presentation with exact game Parrillero — 2026-10-04

User's physical0.2.5 screenshot shows letterbox bands and a cover character unlike the gameplay Parrillero. The entire intro/menu backdrop must fill the actual screen (including beyond safe-area portrait-canvas bands), with proportional center-crop rather than stretching. Keep gameplay's540×960 safe-area layout/input transform unchanged. Keep logo, unscaled4+4second holds, flash, Jugar/Salir positions/behavior and saved progression unchanged.

Use the exact existing`street-parrillero-icon.png` as the separately rendered hero: same face/hair/smile/body/apron/tattoo as the game, not a newly approximated portrait. Replace only the old cover background with a versioned original scene lacking the old hero, retaining street-stall/night-football/foreground-food ambience; no baked logo or buttons. Preserve previous cover, portrait, logo and every meta.

- [x] Intro/menu background covers full screen at9:16 and tall phone ratios without bars; foreground remains correctly scaled and menu targets remain safe/aligned.
- [x] Exact game Parrillero portrait is visible on cover before the unchanged logo reveal, with recognizable face/apron and no old cover hero.

**Validation:** Implemented/imported in existingUnity6000.6.3f1. StreetArtTests **17/17passed**,0failed/0skipped, job`ea33e273724845f8b843ca57def615bd` (four full-screen size cases, active backdrop/font/logo/native-resolution imports). StreetPointerTests **13/13passed**,0failed/0skipped, job`328786258cf54e44aa3f8d6be820c1f3` (exact asset identity, unchanged4+4second guards, actual mouse/touch/start/purchases/replay/load/safe-area paths). Actual Main Game-view menus **1080×1920** and **1200×2670** reviewed in ignored`Logs/Acceptance/IntroFullScreen/menu-*.png`: no external bars, same chosen Parrillero, proportional art, original logo and safe controls. Exact editor progress restored; temporary tall-resolution option removed, previous Game-view selection restored, Main stopped. User explicitly chose **only leave change in project**, so no new APK/build/version/phone update or Git commit/push. Installed0.2.5 and its historical evidence do not contain this fix; native validation awaits a later authorized build.


## Round-local coins — source update (2026-10-04)

Every attempt in all five levels starts with0coins, including loading saved progress, choosing a level, advancing after victory, retrying and StartRound. Discard remaining coins rather than carrying them forward or refunding purchases. Income and spending within the active attempt remain unchanged. Keep unlocked locations/prices and the existing first-level reset versus later-level staff/speed persistence rules. Retain save version2 and its legacy coin field for JSON compatibility, but ignore its stored balance on load; this supersedes historical coin-preservation requirements, not their old validation records.

- [x] Source resets the balance at each entry path and prevents retries/next-level construction from transferring coins.
- [x] Five-level start/restart, real-income, next-level, retry, reload and purchase Unity regressions passed (latest combined record below).

**Validation:** Source/diff review only. Added/updated focused EditMode and PlayMode fixtures, including duplicate Start protection and preserved later-level progression. Existing Unity6000.6.3f1 MCP instance repeatedly failed readiness ping, so compilation/tests/Game-view/device checks were not run. No APK, editor restart, commit or push.


## Unified coin/sales/time HUD — source update (2026-10-04)

Keep all live counters in the original warm glossy top bar described in [visual reference](../visual-reference.md#unified-upper-status-bar--source-update-2026-10-04): gold coin/balance left, remaining m:ss center, actual sales/goal in green right. The decorative frame fills the physical screen width; when a cutout exists, keep only counter content clear of the camera. Fit live fonts to both slot width and height. Remove lower countdown. Read existing simulation values only; do not change timing, handoffs, goals, input or upgrades.

- [x] Source implements compact top HUD and removes lower timer.
- [x] Focused HUD Unity fixtures passed; native safe-area gameplay HUD readability reviewed (latest combined record below).

**Validation:** Static review only; current editor state ping unanswered. No Unity compilation/Play/device verification or APK.

### Edge-to-edge HUD width and adaptive labels — source update (2026-10-05)

Extend the amber HUD artwork flush to both physical screen edges, including behind the reserved notch channel. Widen the sales capsule/text slot and let live numbers shrink to the available width and height. Start from a slightly larger font while retaining readable fit for long coin totals and the two Level-2 progress values. Do not change the HUD height, safe-area controls, timer, goals, or gameplay layout.

- [x] Source artwork fills the logical edge-to-edge bar and continues under the camera-safe gap.
- [x] HUD number font is fitted to its available slot; Level-2 paired icons identify compact counters.
- [ ] Unity/Game-view/device visual validation not run per standing user instruction.

**Validation:** Source/spec/fixture review only. No tests, APK build, or phone update.


## Real mural likeness — art/source update (2026-10-04)

Use the latest supplied real mural/corner photos for recognizable cartoon motifs as defined in [visual reference](../visual-reference.md#recognizable-real-mural-and-stadium-corner-layer--2026-10-04). Composite only the new wall crop above logicaly90 over retained v4; preserve exact wide customer street, counter and lower bands, simulation and input.

- [x] Source selects original v5 wall layer; native file/fresh meta and static composition/source bounds reviewed.
- [x] Focused Unity asset/crop tests passed; native crowd/mural occlusion reviewed (latest combined record below).

**Validation:** Static only. UnityMCP transport unavailable; compilation/import/tests/Play unverified. No APK/git/device action.


## Location footer branding — source update (2026-10-04)

Use bundled comic typography and existing All Boys shield for Floresta footer (Ready/Playing) per visual spec. Long later names fit on one line and never show All Boys crest; footer is decorative, not a new action. Existing input/gameplay unchanged.

- [x] Source uses centered comic caption with level0-only shield and preserves button bounds.
- [x] Focused Unity asset/font/layout cases passed; native small-size footer/crest reviewed (latest combined record below).

**Validation:** Static only, Unity transport unavailable; no engine tests/import/Play/device/APK.


## Current combined validation and phone delivery — 2026-10-04 (0.2.6/code8)

Latest user authorized tests, APK and phone installation. Installed Unity6000.6.3f1 was closed (no running editor/MCP); used the exact installed editor in sequential batch runs, without new packages or a second editor. Current Street **82/82 EditMode + 15/15 PlayMode passed**,0failed/0skipped. Scope includes200-handoff victory/180s deadline, FIFO/reservations, round-local coins and first-level reset, exact progressive prices/caps/MAX, mouse/touch dispatch/start/replay/load, intro timing/asset/viewport guards, current art/HUD/mural/footer imports and layout. Not an unrelated legacy full-suite run or mobile performance certification.

Initial batch pointer runs failed due to unfocused simulated input; swapping transient InputSettings also destroyed the previous instance. Final fixture only saves/restores backgroundBehavior and editorInputBehaviorInPlayMode enum values on the existing settings (IgnoreFocus/AllDeviceInputAlwaysGoesToGameView during tests). No runtime input code or persistent input settings changed. Failed attempts remain in ignored evidence; final15/15 is the successful rerun.

Main-only development Android IL2CPP/ARM64 build **Succeeded340.915261s,0errors/1warning** (Diagnostics Data debug-symbol recommendation). APK **0.2.6/code8**,150547329bytes,min26/target36,packagecom.haychoriypaty.game; V2signature/same debug certificate and original packaged adaptive-logo PNG bytes verified. SHA256`e2cc6576dfbe07e126c8c4f8493ae79640e33b878fd0c3cbd4cef2a825bbe6d5`. Archive`Builds/Android/Archive/HayChoriYPaty-0.2.6-code8.apk`; rolling`Builds/Android/HayChoriYPaty-street.apk` updated after verification; previous archives preserved. Temporary editor build helper/meta removed after saving ignored evidence.

Authorized Motorola Edge60Fusion (API36/1220×2712) replacement install returned **Success**; PackageManager confirms0.2.6/code8. Dedicated playerprefs XML byte-identical before/after install, firstInstallTime preserved; no uninstall/data clear. Own GameActivity cold launch **Statusok/733ms**. Reviewed two native gameplay captures during user play: unified gold-coin/m:ss/sales HUD goal200, recognizable mural/corner behind crowd, pan francés and no floating grill badge, current Parrillero and comic Floresta caption/original crest. The captures show different worker poses and a subsequent fresh first-level round withteam1/×1.00 and initial25/200costs; these are observations, not controlled native tap/reset/accounting tests. User was actively playing/retrying, so no forced restart/quit for additional review. Native gameplay letterboxing remains intentional; full-screen change applies to presentation only.

Own-PID log snapshot contains no matchedFATAL EXCEPTION/NullReferenceException/MissingReferenceException/IndexOutOfRangeException/AndroidJavaException; this is bounded evidence, not an all-session zero-error guarantee. Native intro timing/Salir/launcher masks, device FPS/thermal and broader cutouts remain unverified. Unselected upgrade-card/speed-icon proposals remain unintegrated (existing shoe/card still visible). Evidence stays ignored in`Logs/Acceptance/Current/`: final editmode.xml/playmode.xml, build.json/log, signature/badging, install/package/launch, preferences snapshots and phone-runtime.txt. Both phone-menu.png and phone-result.png actually contain gameplay, not menu/result acceptance. No Git commit/push in this turn.


## Full-screen gameplay viewport — 2026-10-04

The user now asks for the entire gameplay screen to fill the phone, not only the earlier intro/menu backdrop. StreetView uses the full available safe-area rectangle and calculates a taller logical portrait height from screen aspect. Existing 540×960 layout Y anchors spread over that height; drawable bounds and touch points share the same vertical transform, while one uniform screen scale keeps character/UI proportions and hit-target dimensions unchanged. Background uses ScaleAndCrop only to cover the enlarged scene, cropping background edges rather than gameplay controls. HUD remains top-anchored; upgrade/footer and level buttons remain bottom-anchored. Portrait is the supported device orientation. Prior Game-view/native screenshot was fixed-ratio and does not validate the new native composition.

- [x] Safe viewport fills every requested display aspect instead of center-fit letterboxing.
- [x] Tall1220×2712 logical height expands to1200.39 from960 without a non-uniform sprite transform.
- [x] Added pointer round-trip regression for the speed card in the full tall-screen transform.
- [ ] Build/install updated APK and review native gameplay composition/touch on phone (check before updating because replacing the currently open app interrupts its unsaved round).

**Validation:** Focused StreetArtTests33/33 and StreetPointerTests17/17 passed in Unity6000.6.3f1 batch;0failed/0skipped. These are editor/layout/interaction tests, not native screenshot acceptance. Current installed0.2.6/code8 does **not** include this change; no APK built or installed in this refinement. Broad other artwork/simulation behavior was not retested.


## Parrilla Criolla upgrade cards — visual-only source update (2026-10-04)

Use the user's selected wood-and-riveted-iron card style for the two playing upgrades. Replace the Speed sneaker icon with three forward chevrons; reuse the existing Parrillero portrait for hiring. Keep the +10% indicator, gold coin, current dynamic cost/MAX, existing cost tables and purchase/input behavior unchanged. Cards must allow the largest four-digit current hire price ($2800) to fit. Source image prompts/hashes are tracked in `Assets/Art/Street/GenerationPrompts.md` and the design acceptance notes in `visual-reference.md`.

- [x] Reusable wood/iron card art, arrow icon, live labels/coin/price and independent touch bounds are implemented.
- [ ] Focused Unity import/layout and upgrade pointer checks pass; runtime small-screen visual review remains separate.

**Validation:** Pending focused Unity tests; no APK build or install requested/performed.

### Dynamic status ribbon — selected option 1 (2026-10-05)

The user selected a compact status ribbon directly below each card title. Speed displays its current multiplier alongside the existing `+10%` next-upgrade indicator; the helper card displays the current team size. Remove the redundant standalone team/speed footer. Keep the gold price plate, dynamic prices (including four digits), purchase logic, and touch target unchanged.

- [x] Runtime values render inside the corresponding card status row.
- [x] Existing cost plate and card tap bounds remain unchanged.
- [ ] Focused visual/runtime review not run per the user's standing no-tests instruction.


## Parrillero serving table — visual pickup source update (2026-10-04)

Use an original, used/scuffed wooden sawhorse table loaded with finished choripanes in pan francés beside the parrilla. Product-zero pickups must route workers to the table's side, not the grate. Draw it in the foreground near the grill while preserving the shared grill and later drink stations. Workers continue through the existing automatic pickup and handoff states; this changes the visible pickup location only, not order quantities, recipes, income or queue rules.

- [x] Generated transparent cartoon table sprite is integrated in the street view; product-zero station target moves to its right edge.
- [x] Existing art fixture expectation updated for the new pickup location and unchanged next station.
- [ ] Unity import, pickup-path/Game-view composition and non-overlap review; no test run until the user asks.

**Validation:** Not run by explicit user instruction. No APK build or phone install.


## Full-body Parrillero on startup cover — 2026-10-04 refinement

The user's latest request replaces the previously reused waist-up portrait on the cover with an original full-body cutout of the same Parrillero and an even face/body skin tone. Use the new street-cover-parrillero-full-v1 cover-only resource, guided by the existing face portrait and actual gameplay sprite sheet. Keep the original portrait, sprite atlases, no-hero backdrop, logo art, Jugar/Salir labels and bounds, and 4+4-second holds unchanged. To keep the figure head-to-shoes unobstructed instead of hidden behind the central badge, balance it beside the title logo: hero at logical x=8/w=168/h=252, logo base x=184/w=352/h=345; the combined composition stays centered.

- [x] New transparent 1024×1536 head-to-toe hero preserves the established face identity, full outfit/tattoo/shorts/shoes, and matches exposed face/arm base skin colors.
- [x] Startup renderer uses the versioned hero beside the title and leaves the original gameplay/hire portrait untouched.
- [ ] Unity import and actual 9:16/tall-screen cover composition visually reviewed.

**Validation:** Generated source dimensions/alpha and appearance reviewed; face/arm sampled median colors match (RGB 246/150/83 vs 246/150/84). Resource meta uses no mipmaps and Android RGBA32. Source-level git diff --check passes. Unity import/Game-view/tests/APK were not run for this art change; in-game appearance remains unverified.


## Edge-to-edge Floresta scene and obstruction-free pickup — 2026-10-04

User follow-up requires full physical-screen scenery (no upper gray safe-area strip), a physical-edge y0 info bar with center-camera clearance, lower/closer supporter rows, and a service table beside—not below—the smaller grill. Safe-area input/control geometry, active balance tables,200sales/180s/$5 and all art/metas remain unchanged. View paints scenery/full-edge status independently from actionable controls.

Single-product pickup is(205,585) on the left table; both ToStation and ToCounter traverse(205,510) outside the right grill before the actual station/handoff delay. Reservation cancellation clears intermediate-route state. Table/grill bounds are disjoint; only real station arrival transitions to Pickup, and only final counter arrival transitions to Handoff. Later multi-product table/station layouts remain as before. Queue feet use324/268/212 while retaining seven columns/three FIFO rows and live owner state.

- [x] Source exposes no unpainted top inset and anchors read-only HUD to the physical edge with centered-camera gap.
- [x] Source uses disjoint Floresta table/grill, clear outgoing/return lane and actual final-arrival pickup/handoff gating.
- [x] Crowd source is lowered/compacted to reveal mural and approach counter.
- [ ] Updated targeted geometry/route/input fixtures executed (deferred: user says no tests until requested).
- [ ] Native full-screen/cutout/placement/movement checked.

No balance proposal selected/applied; route-distance change requires future playability confirmation. No APK or installation requested.


## User-selected startup cover — 2026-10-05

The user supplied a new portrait illustration and selected it as the game's cover. Use the exact image unchanged as one full-bleed opaque scene; do not add a second Parrillero cutout. Preserve the transparent title logo, 4+4-second holds, brief reveal flash, persistent Jugar/Salir controls, safe-area placement and input. Gameplay art and behavior stay untouched.

- [x] Added the supplied 941×1672 PNG as versioned `street-cover-user-v5` with fresh Unity metadata and switched `StreetView` to load it.
- [ ] Unity import and presentation composition on standard/tall phone ratios reviewed.

**Validation:** Source image dimensions/hash and resource-reference change checked. No Unity import, tests, Game-view/device review, APK or installation was run for this art-only request.


## Selected integrated startup cover — 2026-10-04 latest refinement

Latest user-selected night-grill illustration replaces the separate no-hero background/full-body cutout composition. Use the edited versioned opaque scene `street-cover-grill-parrillero-v3` with recognizable game Parrillero face, natural neck connection and matched skin family. Render once across physical-screen pixels using proportional crop, without a duplicate foreground cook. Preserve original gameplay assets/logo, 4+4 holds, light flash, safe menu buttons and actions.

- [x] Cover asset/provenance/new meta and single-scene rendering integrated; title/flash recentered.
- [ ] Native full-screen presentation/menu visual acceptance reviewed.

Existing startup fixture source adapted, not executed. No test, Play/device review, APK/install or balance changes in this request. Historical cover test evidence above does not validate this new art/composition.


## Deadline riot in the active game — 2026-10-04

The Main scene uses `StreetView`, not the retired `PrototypeView`. On the existing simulation `Lost` phase (timer expires below goal), render the full-bleed versioned angry-crowd/stall-destroyed scene instead of the ordinary playfield. Top counters still show the frozen outcome and zero time; add direct failure copy, animated GAME OVER, sales/goal and one Volver control back to the unlocked level selector. Animate background rumble and flying debris with unscaled time so the mayhem continues after simulation stops. Do not change timeout rules, customer behavior during play, balance, sales or saved progression; victory presentation remains unchanged.

- [x] Dedicated angry/sticks/broken-stall art resource wired only into active `StreetView` loss result.
- [x] Riot motion continues on unscaled time; result state exposes a visible replay target.
- [x] Focused PlayMode test passes for real timeout, loaded riot resource, moving debris and replay return (1/1).

**Validation:** Source/art integration is new; earlier `PrototypeView` riot code is only legacy evidence, not Main verification. User authorized one relevant test after completion. No APK/device install requested.

**Validation (2026-10-04):** Unity6000.6.3f1 filtered PlayMode test `HayChoriYPaty.Tests.StreetPointerTests.ReplayReturnsToReadyWithoutStartingAutomaticallyAndDiscardsCoins` passed1/1,0failed/0skipped. It triggers a 0.05-second level deadline, confirms `Lost`, active Main riot texture/state and changed debris trajectory, then taps replay and confirms Ready with coins reset. Batch compilation/import succeeded in this final run. Actual Game-view/device appearance was not checked; no APK. First attempts surfaced stale win-panel compiler references and OnGUI-only state initialization under `-nographics`; both were corrected before the passing run.


## Presentation-to-level selector and victory return — 2026-10-05

The presentation's **JUGAR** now opens a dedicated level selector rather than starting the saved location automatically. Keep the current five canonical locations; only indices `0..UnlockedLevel` are tappable, and saved progression unlocks the next index after the preceding level is won. Every unlocked card starts its level directly at fixed $5, with no price setup. Existing presentation **SALIR** still exits the app. The level-1 card uses the original All Boys mural and shield; level 2 uses its Nueva Chicago mural and shield; locked later cards preview the game grill/product set with club-color accents instead of invented club crests. The win result removes price replay/next-cancha actions, summarizes sales, round coins earned and remaining time, and has a single **SALIR** that returns to the selector without quitting or starting another round. Loss/riot now explains the missed goal and offers one Volver action back to the unlocked level selector.

- [x] Source opens selector from presentation Jugar and gates card selection by persisted unlock level.
- [x] Source uses the existing All Boys/Chicago mural resources for the first two card illustrations; later locked cards use original game product art and accent colors.
- [x] Win-result source has relevant round summary, only Salir, and returns to selector after recording the unlock.
- [ ] Focused pointer/progression tests, Unity import and actual portrait Game-view review remain pending; user asked not to run tests until requested.

**Validation:** Source/spec review only. No Unity editor/player, tests, APK or device actions were run.


## Live timeout rage and stall-break sequence — 2026-10-05

Refine the earlier Lost-state vignette: a single angry-crowd composite is not the event. When the deadline expires below goal, render the actual frozen `Waiting`/`Receiving`/`Advancing` queue in its existing slots. Their existing looks first jitter and raise anger cues, then fade into angry supporter poses in the same team-specific outfits they wore while waiting, with raised-stick and swing-down non-contact frames. Alternate two frames per person at 6 fps with staggered phases on unscaled time so the sequence continues while the simulation remains frozen. Start the broken-environment crossfade after the anger reaction, then keep the live fan sprites in front of the shattered stall with moving wood debris and rumble. Preserve result counters, economy and progression; show the direct Spanish failure copy and animated GAME OVER, with Volver returning to the unlocked level selector without auto-starting. `street-riot-defeat-v1` remains archived; Floresta uses the nine-outfit `street-allboys-riot-fans-v1` atlas, Level 2 uses the 4×4 `street-chicago-riot-fans-v2` atlas with the same eight integrated outfits and customer-ID mapping as its regular crowd; `street-riot-fans-v1` remains the generic fallback. Future teams follow the same team-specific, full-body wardrobe mapping across normal and riot states.

- [x] Timeout animation uses actual waiting queue positions and staggered angry/stick-swing frames, not a static crowd plate.
- [x] Broken counter/environment fades in after the anger beat; supporters and debris continue moving on unscaled time.
- [x] A large comic smoke-and-impact cloud rapidly forms over the queue at the gameplay-to-riot cut, then dissipates to reveal the angry crowd and destroyed stand; it is presentation-only and uses unscaled time.
- [ ] Focused timeout/replay test source now checks the new resources and pose alternation; execution and Game-view/device review are pending.

**Validation:** Added the versioned transparent smoke asset/import metadata and wired it into the active `StreetView` loss transition. No tests, Unity/Game view, APK or device operations were run per the user's standing instruction.

## Nueva Chicago bottle service and order variants — 2026-10-04

Level 2 now uses the new full-street Nueva Chicago mural plate and C.A.N.CH. footer shield. Its two sellable items are product IDs 0 (chori) and 4 (Coca-Cola 600 ml bottle), with the older cup art replaced at the same stable price-array slot. The definitive Chicago arrangement is left-to-right **Coca barrel → centered parrilla → finished-choripán table**. All three share the y=686 ground line and equal 56-pixel visible gaps, with uniformly reduced original artwork proportions. Coca pickup is at (155,648), reached via (155,510); finished chori pickup is at (395,665), reached via (395,510). Both are in the open gaps beside the objects, not inside the grill or table. Return trips reverse the same side approach before going to the unchanged customer handoff at y=485. Reuse the existing mirrored side-reach frame at Pickup, direction-correct cardinal walks and existing diagonal walk pairs. No role/order/FIFO/HUD/economy changes; other levels retain their station layouts.

Each arriving customer chooses one of three order variants: chori-only, Coca-only, or both. Each requested quantity is independently uniform 1–4. In a combined order the Parrillero owns and completes all requested choris, and the Cocacolero owns and completes all Coca; their separate parts may be handed off in parallel. Each actual handoff counts toward the corresponding per-product objective and earns the selected price for that specific product. Orders show both remaining product quantities in two rows.

- [x] Level 2 exposes only catalog IDs 0 and 4 without shifting saved product prices or adding an eighth item.
- [x] Added bottle/barrel props, Chicago background/shield, side-by-side station bounds, bottle product tabs/icons, dual-product order display, and the earlier sequential combined-order fulfillment. **Superseded 2026-10-06:** specialist ownership allows the Parrillero and Cocacolero to serve their respective parts of a mixed ticket concurrently.
- [x] Focused Unity6000.6.3f1 checks passed: StreetSimulation EditMode53/53, StreetArt EditMode42/42, and StreetPointer PlayMode17/17.
- [ ] Game-view/phone composition review and native-device behavior remain pending.

No APK was built or installed. These tests cover simulation variants/quantities/revenue/priority, imported resources/geometry, and price-tab mapping; they do not verify device visual composition.

### Definitive Chicago layout validation — 2026-10-06

- [x] Permanent barrel-left / grill-center / ready-chori-table-right layout, uniform 56px visible spacing and y686 ground line; all three preserve their original visual proportions at 72% scale.
- [x] Food Pickup at (395,665) via (395,510), Coke Pickup at (155,648) via (155,510); reverse the approach on return, retain handoff at y485. Feet and rendered worker widths stay clear of the grill.
- [x] Existing Parrillero side-reach/cardinal flips and diagonal stride pairs are reused. Chicago Coca uses corrected, connected-component-tight source rows from the same authored atlas so walking really cycles walking poses rather than idle, without stray neighboring hair pixels. Its specialist behavior is unchanged; other levels keep their visual mappings.
- [x] Unity 6000.6.3f1 final focused EditMode run: **16/16 passed**, zero failures/skips, job `240c347e2ffd4dddb4b300259ce85fa6`. All 2,828 route samples from seven customer columns across both products were clear. An isolated 4-chori + 4-Coca ticket produced 8 real sales/$40 and returned both workers to Idle.
- [x] Actual 1080×1920 Game View snapshots reviewed: `Logs/Acceptance/ChicagoLayout-20261006/chicago-playing-final.png`, `chicago-coca-pickup-final.png`, `chicago-walking-right-final-1.png`, `chicago-handoff-final.png`. This includes a full normal 21-customer queue. Simulation was stepped/frozen for snapshots, and the original editor save was restored.
- [ ] Native phone review is not part of this checkpoint; no APK/build/install was requested.

## Nueva Chicago sales objective — 2026-10-05

The Level-2 objective is **200 successfully handed-off choris and 200 successfully handed-off Coca bottles**, not 200 combined. Both products sell for a fixed $5 per unit; there are no later-level price controls. Track product IDs 0 and 4 independently while retaining `Delivered` as the aggregate in-round count. Use `levelGoals[1]` as the per-product target (200 by default); win only once both counters reach it, and at the deadline lose unless both do. The upper sales capsule shows each product's separate `current/target` progress. Coin rewards still occur only at handoff.

- [x] Added separate per-round chori/Coca counters and both-target win condition.
- [x] Added Level-2 HUD and result progress for each product; Floresta and other locations keep existing aggregate goals.
- [x] Regression source covers default 200-per-product target, $5 default unit prices, requiring both targets, and clearing both counters on round restart.
- [ ] Focused simulation and visual checks remain pending.

**Validation:** Regression source updated but not run, per the user's standing instruction. No Unity run or APK/device install.

## Nueva Chicago supporter apparel — 2026-10-04 (historical overlay implementation)

This entry records the first overlay-based implementation, superseded by the integrated outfit sprite atlases below. The eight green/black/white clothing designs remain generic and logo-free; no store photos or exact kit marks are copied. Apparel remains appearance-only and does not alter crowd identities, order, position, or simulation.

- [x] Added an eight-look transparent clothing atlas and level-2-only deterministic rotation (historical input art; no longer drawn).
- [x] Fitted clothing bounds separately to front-facing standing poses and narrower side-walking poses (historical overlay implementation).

**Historical validation (2026-10-05):** Full Unity6000.6.3f1 batch suites passed: EditMode116/116 and PlayMode21/21. These checks covered the previous overlay fit and Chicago counter clearance.


**All Boys wardrobe refresh (2026-10-05):** Added separate matching 3×3 full-body atlases for nine neutral monochrome outfits in front and walking poses. No clothing overlay is used; old sprites are fallback only. Chicago's separate clothing implementation was unchanged at that checkpoint and was refreshed below. See `specs/status.md` for Unity test results and the visually reviewed Game-view screenshot. No APK/device changes were part of the request.

**Nueva Chicago outfit refresh (2026-10-05):** Replaced level-2 garment overlays with eight complete front and profile-walking sprite variants, plus matching fully integrated green/black timeout-riot poses. Selection remains deterministic by customer ID and apparel is appearance-only. Focused atlas/import/selection EditMode test passed 1/1. Reviewed the 1080×1920 Game-view capture at `Logs/Acceptance/ChicagoOutfits-20261005/chicago-eight-outfits-playing.png`; the clothing reads as part of each full character, including the walking pose. No APK/device changes were part of the request.

## Crowd behind the counter and double-height HUD — source update (2026-10-05)

Across all five locations, shift the rendered queue toward the actual counter edge and clip waiting/advancing supporters behind its top surface so the front row's legs cannot appear on or in front of the stand. Derive the edge from the active backdrop's ScaleAndCrop geometry (open-street y548, Chicago y642); keep simulation queue anchors, FIFO, worker paths, sales, costs and balance unchanged. Bodies and order/patience indicators share the same view-only offset. The initial timeout reaction keeps these positions/occlusion; full riot poses can emerge once the stand has broken, with the impact cloud following the queue.

The top amber HUD remains full-screen-width and physical-top anchored, but grows from 34 to 68 logical pixels high. Increase coin/product/clock artwork and preferred number lettering, then fit each live value to its own slot. On camera-cutout phones, put the countdown below the upper camera channel instead of squeezing it into the former narrow slot. Nueva Chicago's chori/Coca counters use two separate, icon-labelled rows inside the green capsule.

- [x] Source shares counter-aware crowd position/clipping across all levels and the initial timeout transition without changing simulation geometry.
- [x] Source doubles HUD height and updates cached texture, live text/icon slots, cutout countdown and paired Chicago counters.
- [ ] Focused Unity art/layout checks and actual portrait Game-view/device review remain pending; user has not authorized tests for this change.

**Validation:** Static source/spec review and git diff --check only. No Unity/editor/player tests, APK or device actions.

## Selected victory popup — source update (2026-10-05)

Use the user's selected football/wood/forged-iron/parchment composition: gold outlined **¡TURNO COMPLETADO!**, decorative football/laurels/black-white flags, three rows for **VENTAS**, **MONEDAS GANADAS**, **TIEMPO SOBRANTE**, and one large green **SALIR**. Use the versioned transparent blank panel artwork; no title/label/live number is baked in. Dim the whole physical scene behind it, including the top bar. Fit the popup uniformly into the safe portrait canvas; the same normalized frame/exit slot drives draw and mouse/native touch geometry. Salir returns to the level selector, never grants another reward or starts a round; loss/riot UI is unchanged. Chicago's sales row shows separate chori/Coca goals.

MONEDAS GANADAS is gross actual handoff income for this attempt, not the spendable balance after purchases. The round-local CoinsEarned tally is incremented only where a handoff already credits Coins and is not reduced by upgrade/hiring costs. Reset it on new attempt/level and do not persist or carry it; no economy changes or duplicate reward. Remaining time uses mm:ss with two-digit minutes; values/font sizes fit variable counts.

- [x] Source loads the original blank RGBA artwork and overlays the requested heading, live summary and green-button caption.
- [x] Source retains selector navigation and uses shared aspect-correct draw/touch bounds.
- [x] Gross earnings source records exact product income independently of spending/reset rules; focused regression source added.
- [x] Unity6000.6.3f1 import/compilation and full Street test assemblies passed: **139/139 EditMode + 23/23 PlayMode**, 0 failures/skips. This includes popup art/layout/live values/earned-income and pointer-to-selector regressions.
- [ ] Actual portrait/Game-view/native popup appearance was not manually reviewed.

**Validation and delivery:** Main-only Android Development IL2CPP/ARM64 `build-f93d61fec0` succeeded in 263.61s (0 errors, 1 Diagnostics Data debug-symbol warning). APK 0.2.13/code15 v2 signature/package/ABI verified and installed on Motorola Edge 60 Fusion `ZY22MBNWRB`; see [Android delivery](android-prototype.md). No manual native gameplay/popup screenshot or physical performance certification.


## Global customer order bubble window — source update (2026-10-06)

Use one reusable order model and display layout in every current/future level. An order retains up to five distinct unlocked product lines in request order. The light, dark-edged speech-card shows only the first two lines with product icon at left and a fitted readable quantity at right; completed lines are skipped immediately, and “+N más” counts pending hidden types only. A lone visible line is centered inside a compact content-sized background. Use 49/74/88-unit heights for one row/two rows/two rows plus hidden-type footer, with measured width capped at the existing seven-column envelope. Reuse the speech atlas with fixed-corner nine-slice drawing; keep icon/font sizes and the customer-relative tail anchor unchanged. The window does not mutate the order or alter level-specific spawn, work, reservations, FIFO, economy, or victory rules. Existing automatic demand remains one item in Floresta, the established chori/Coca variants in Chicago, and one unlocked item on other current levels.

- [x] Core stores and serves up to five ordered product/quantity/reservation lines; legacy one-/two-product entry points remain compatible.
- [x] StreetView draws one shared maximum-two-line order window and hides completed/zero lines.
- [x] Focused EditMode coverage checks one through five lines, hidden counts/reveal/order, multiple levels, one-row centering, old one-/two-product delivery and layout containment.
- [x] Unity6000.6.3f1 script compilation and complete EditMode suite passed: **154/154**, including order quantities/counts/reveal/order, single-line geometry, FIFO, reservations, worker routing, deliveries, handoff coins and old combined orders.
- [x] Complete PlayMode suite passed: **23/23**, including input, pointer, timeout and round lifecycle regressions.
- [x] Adaptive-bubble follow-up (2026-10-06): 14/14 focused EditMode cases passed, including one through five pending types, local content containment, fixed corner geometry and stable anchors at tall-screen scales. Game-view captures reviewed at 1080×1920 and 1220×2712 with real model tickets containing one through four (plus five) product types; normal auto-generated All Boys/Ferro crowds also reviewed. No native-device check. No project warnings/errors during the final runtime console window; exact editor PlayerPrefs restored after review.

## Chicago specialist workers and sticky customer ownership — 2026-10-06

Nueva Chicago maps the existing Parrillero role exclusively to choripán (product 0) and adds the red-apron Cocacolero exclusively to bottled Coca (product 4). Keep the existing one-worker starting baseline, five-worker limit, upgrade costs, speed, timers, goals, recipes and economy unchanged. Parrillero and Cocacolero use the same shared hire-cost table and speed multiplier: there is no specialty surcharge or separate price progression by role. The next hire price is indexed by the total team size, not the number of workers of that specialty. In Chicago assign each new hire to the currently less-represented role (ties go to Parrillero); therefore the first hire after the initial Parrillero is a Cocacolero. Outside Chicago, all workers remain Parrilleros and retain the current level-specific service products.

Replace round-robin-by-unit dispatch with a FIFO scan of customers in append/arrival order. A new assignment requires front-row `IsAtCounter`, settled `Waiting`, pending product for the worker's Chicago specialty (or the level's ordered line), and a free line owner. Store the worker ID on that order line. A worker carries the same customer/product part through repeated station→pickup→handoff trips, holding one unit reservation per trip; release ownership only when that product part is complete. Chicago's two different lines can be owned and serviced in parallel, while same-role workers skip an already-owned line and claim the next eligible oldest customer. Other locations keep their ordered product lines on one general Parrillero assignment.

If a customer expires, leaves, or becomes ineligible while the worker is travelling, cancel the trip, release its one reservation and line owner, and do not deliver or pay. A handoff commits only for that line's owning worker. Keep the existing seven-column FIFO, queue compaction/tail admission, front-row positions, station geometry and entry/exit transitions.

The Cocacolero is an original 4×4 transparent pose atlas with a red bib apron, matching the existing worker atlas framing/indexes. Front/back/side idle, alternating walks, low pickup and bottle handoff are integrated in StreetView; Chicago Coca continues to use the current barrel station.

- [x] Chicago role filters prohibit Parrillero→Coca and Cocacolero→chori; other levels remain generic Parrillero service.
- [x] Each same-role customer portion has at most one persistent worker owner; mixed chori+Coca can have two distinct owners concurrently.
- [x] Only the oldest settled eligible first-row Waiting customer is newly claimed; a claimed worker completes the specialty portion before seeking the next client.
- [x] Cancellation clears only the worker's active reservation/owner; invalid/double handoffs cannot pay.
- [x] Red-apron Cocacolero atlas and original pickup/handoff/walk poses are loaded for its role; Parrillero art and Coca barrel route stay intact.
- [x] Focused Unity6000.6.3f1 EditMode logic/import run: 13/13 passed, 0 failed/skipped; covers both worker orders, separate same-role ownership, mixed parallel fulfillment, front-row/FIFO eligibility, idle roles, expiration/reward cancellation, unchanged hire price/cap and all 16 Cocacolero atlas bounds/import settings.
- [ ] Integrated portrait Game-view appearance was not captured; only the original Cocacolero atlas itself was visually inspected.

## Clear stalls and specialist-art consistency — 2026-10-06

Levels 3–5 retain their club-specific street/mural plates. Because those background plates omit the serving counter, `StreetView` overlays the same wood-top/slatted-front counter crop used by the original street plate at the shared crowd-clipping edge; waiting supporters remain visually behind it, while workers and stations remain on the player side. This is presentation-only and does not move simulation queue or worker anchors.

Levels 2–5 (runtime indexes 1–4) use the dedicated transparent 4×4 Cocacolero atlas with the red apron retained and the stocky outlined cartoon proportions/shading matched to the Parrillero. Level 1 keeps its legacy Coca worker because it has no specialist hire. All levels retain the same worker frame, role behavior and Coca route.

- [x] Levels 3–5 show a continuous counter in front of the waiting crowd while preserving each club background.
- [x] Matching-style Cocacolero art is used on Chicago and levels 3–5; the original legacy atlas remains the fallback if the shared art is unavailable.
- [x] Focused Game-view checks covered levels 3, 4 and 5; no gameplay or economy rules changed.

## Level 3 — Liniers / Vélez Sarsfield

Level-selector slot 3 displays exactly `Liniers - Velez Sarsfield`. It uses team-specific street/mural art based on the user's three Vélez/Liniers corner-wall references, not an in-stadium setting or a recycled Floresta/Chicago background. Available catalog IDs remain stable: Chori 0, Paty 1 and Coca 4; Level 3 has three products. The food art is one large grill with visibly distinct Chori and Paty halves; Coca remains at the existing barrel station. Use the current Paty sandwich icon and Coca bottle/Cocacolero instead of adding catalog products.

Automatic requests cover all seven non-empty Chori/Paty/Coca combinations (one-, two- and three-product tickets); each line is independently 1–4. The global two-visible-line order-bubble window and `+N más` footer remain unchanged. Workers retain their customer per role: a Parrillero owns both pending food lines and delivers Chori before Paty; a Cocacolero owns Coca. One of each may serve the same ticket concurrently. Same-role assignments cannot share the same customer; FIFO front-row eligibility, queue compaction/tail admission, timeout cancellation and handoff-gated income remain common.

Preserve the existing Level 3 slot's goal (65 total delivered units), 240-second timer, demand multiplier, prices, upgrade tables and economy. Level 4 is Ferro Carril Oeste and level 5 is Independiente de Avellaneda; the two new identity/asset sets use their own maps and do not shift stable catalog price IDs. Existing Level 3 goal/time and all five balance slots remain unchanged.

**Implementation/validation checkpoint (2026-10-06):** Focused regression sources now cover Level 3 product availability and order subsets, role-only ownership, parallel fulfillment, same-role exclusivity/FIFO order, rear-row ineligibility, timeout cleanup, final departure and three-product bubble reveal, plus atlas/resource imports. Execution remains pending: Unity's editor-state/console ping did not respond and the filtered EditMode request timed out before a test job was returned. No compilation, Unity import, Game-view screenshot or passing test is claimed. See [current project status](../status.md).

## Level-transition upgrade reset — source update (2026-10-06)

After a valid win advances to the next location, start it with exactly one Parrillero (no Cocacolero) and base speed x1.00. Because the displayed next prices derive from the active level and worker/speed tier, they return to that new level's first hire/speed costs. Coins still reset as before; unlocked progress and all product prices are retained. Retry behavior now follows the master fresh-attempt baseline specified above.

- [x] `NextLevel()` resets staff and speed before the ready simulation is rebuilt and saved.
- [x] Focused EditMode/PlayMode regression source checks baseline team, speed and first prices after advancement.

**Validation:** Static source/spec review only. No Unity tests/editor run, APK or device action was performed; tests remain pending until requested.


## Official selling prices by stable product ID — current contract (2026-10-08)

All locations use the immutable catalog sale-price table: ID 0 Chori $5; ID 1 Paty $5; ID 2 Bondiola $10; ID 3 Vacío $12; ID 4 Coca $5; ID 5 Fernet $12; ID 6 Cerveza $7. Keep IDs and legacy `price`/`prices` fields/setter wrappers for compatibility, but constructors, level changes, and loads must always expose the official values; old saved prices must never override them, and subsequent saves must write the official table. Revenue is credited only on each completed real handoff and equals that product ID’s price; delivered-unit counters and every victory goal remain quantity-based. Keep all price-selection popups, tabs, displays, sliders, and hidden price hit targets absent. Do not let selling prices influence customer demand, order frequency, product probabilities or quantities; do not change goals, timers, hiring/speed economics, catalog, order generation, or any other balance.

- [x] Simulation reports the exact seven-ID table in all 11 levels and rejects legacy price edits.
- [x] Old saved values cannot override the table; saving serializes official values.
- [x] Completed handoffs credit exact ID-based revenue while unit delivery counters remain unchanged.
- [x] Every unlocked selector/Ready level action starts gameplay directly; no price hit target exists.
- [x] Unity 6000.6.3f1 focused regression passed: 5/5 EditMode and 2/2 PlayMode, covering all 11 levels, exact handoff revenue for IDs 0–6, unit counters, legacy-save normalization/reserialization, disabled editing, and unchanged demand/orders.


## Defeat-result copy and return action — 2026-10-06

Keep the animated full-screen trifulca, but replace the celebratory riot slogans with the direct explanation **“No llegaste a entregar todos los pedidos.”** Show a pulsing, outlined **GAME OVER** animation immediately below it. Keep the gameplay sales counters in the upper HUD. The lower result area contains only **VOLVER**: do not draw a repeated objective summary or its translucent backing panel. The only defeat action is **VOLVER**; it resets the failed attempt to Ready and opens the unlocked level selector without auto-starting a round. It does not alter unlocked progression or gameplay rules.

- [x] Loss copy, unscaled animated Game Over and Volver action are wired in active StreetView.
- [x] Lower result summary and its backing panel are removed entirely; no layout container reserves space (result is IMGUI).
- [x] Focused PlayMode timeout/return test passes; live portrait Game View confirms a clean lower area and working button.

**Validation:** `RiotReturnOpensLevelSelectorWithoutStartingAutomaticallyAndDiscardsCoins` passed 1/1. Inspected actual Game View screenshot `Logs/Acceptance/GameOverBottom-20261006/gameover-clean-bottom-1.png`; no summary/panel appears. Unity console: 0 errors/warnings. PlayerPrefs, time scale, Game View and clean Main scene restored. No APK/device install requested.

## Levels 4 and 5 — Ferro Carril Oeste and Independiente de Avellaneda

Level 4, Ferro Carril Oeste, reuses stable catalog IDs in cumulative order `0,1,2,4,6`: choripán, paty, bondiola sandwich, Coca 600 ml bottle and beer can. Level 5, Independiente de Avellaneda, uses `0,1,2,3,4,6,5`, adding vacío sandwich and a one-liter Fernet con Coca/ice cup. Orders remain one to five distinct unlocked types, with automatic line quantities 1–4. The Parrillero completes only pending food lines (ID 0–3), and the red-apron Cocacolero only available drink lines (ID 4–6); they may own different parts of one ticket concurrently, but same-role workers cannot share a ticket. FIFO eligibility, two visible bubble rows +N, hidden-completed-line handling, deadline/cancel behavior, $5 handoff income, queue and original L4/L5 goals/timers are unchanged.

The game view uses original, replaceable cartoon plates rather than traced photos: Ferro’s Caballito/Etcheverri street wall now reinterprets one two-angle green-and-white historical mural plus the separate historic-player shutter mural, with twelve complete green/white supporter variants; Independiente’s original red Avellaneda wall reinterprets both supplied mural compositions: the two-player red tribute and the broad historic mural with the outlined 10, cup and two legends; twelve complete supporter outfits stay consistent across front, walk and riot atlases. Each club has distinct front, walk, and two-pose riot atlases keyed by customer ID; Ferro’s front/walk/riot atlases preserve the twelve-outfit roster across contexts. Both levels render an aligned shared four-bay grill (three visible bays at Ferro; all four at Independiente), a separate finished-sandwich trestle, Coca barrel and beer-can barrel; Independiente additionally displays the 1-liter Fernet preparation trestle. Four food pickup anchors follow the same evenly spaced x positions; no locked cooking bay or product is drawn. The four-zone texture is one aligned segmented grill sprite, not four separate overlapping images.

Acceptance coverage added for cumulative product IDs and locked products, the five-type cap, 1–4 automatic quantities, complete mixed food/drink fulfillment in both levels, one owner per specialty/customer, FIFO priority, visible station anchors and all team/worker art resource imports. Static asset/source validation passed; Unity import, compilation, tests and integrated Game-view validation remain blocked by the nonresponsive editor bridge. See [current project status](../status.md).

Independiente Store product categories informed the fanwear set: [PUMA range](https://www.independientestore.com.ar/puma/) (home/away jerseys, red training top, padded jacket, quarter-zip, and black culture tee) and [upperwear collection](https://www.independientestore.com.ar/coleccion/partes-de-arriba/) (woven jersey, windbreaker, away shirt, black sweatshirt, thermal shirt and softshell). These are original outfit interpretations, not copied product images.

Official visual research: [Tienda Verdolaga](https://www.tiendaverdolaga.com.ar/productos/) categories and product listings informed Ferro fanwear variety: current green/white jerseys, green polo, green/black hoodies, training sweatshirts, mid-zip tops, windbreaker and outing jacket; the sprite designs are original interpretations, not exact product replicas. [Ferro Carril Oeste 2025 kits](https://www.ferrocarriloeste.org.ar/marketing/camisetas-2025-la-historia-es-nuestra/) documents green home, white/gothic-F away and British-rooted third kit; [Ferro stadium](https://www.ferrocarriloeste.org.ar/estadio/) identifies the Ricardo Etcheverri in Caballito. [Independiente 2026/27 PUMA kit](https://www.clubaindependiente.com.ar/marketing/noticias/1787923475_nueva-camiseta-alternativa-pumaxcai-2026-27) and [the club stadium page](https://clubaindependiente.com.ar/institucion/estadio) ground the identity and Avellaneda setting. Contemporary mural reporting ([Independiente history mural at the Libertadores access](https://www.tycsports.com/futbol/independiente-inauguro-su-mural-para-las-glorias.html), [Ferro/Caballito wall context](https://caballitourbano.com.ar/las-huellas-de-un-barrio/)) is contextual inspiration only. The game backgrounds, fanwear and objects are original illustrations, not copied photos, exact licensed uniforms, or literal mural replicas.

## Independent specialty hiring HUD — 2026-10-06 (supersedes shared total-team hiring tiers/cap)
Floresta keeps two cards. Any specialist level with drinks uses one row ordered Speed, Parrillero, Cocacolero, with exact separate draw/touch rectangles. Each hire buys only the selected role; team count, next cost and MAX are role-local. Existing configured price arrays are shared values, not shared progression: index max(0, roleCount-1). The initial Coca hire from zero and next hire at one both use the first configured row; no price extrapolation. Role caps independently inherit maxStaff (5) unless overridden; food MAX never blocks drink hiring. Speed, stations, service and orders remain unchanged. Retry/load preserve explicit composition rather than re-alternating roles; old saves without composition retain their legacy alternating constructor mapping. Advancement retains one Parrillero/zero Coca/base speed.
- [x] Role-only hiring/counts, independent price progression/caps and composition restoration verified.
- [x] Three-card draw/touch layout, four-digit prices and MAX verified; Floresta retains two cards.
- [x] Mouse/touch exact dispatch and adjacent gaps/edges checked, plus retry/reload and advancement reset.
Validation: 25 distinct focused EditMode cases and 8 distinct PlayMode cases passed in Unity6000.6.3f1. A legacy advancement fixture depended on automatic Coca hiring; it was changed to explicit Coca and rerun successfully. Live Game View captures reviewed at1080×1920 and1220×2712, including both EQUIPO2/500 cards, food2800 with CocaMAX, and Floresta two-card compatibility. Tall-card internal offsets and pointer heights now share an anchor-relative scale. Original editor PlayerPrefs/timeScale restored; Play stopped. No APK/device run.

## Chicago parrilla capacity refresh — 2026-10-06

The established station order stays Coca barrel left → centered parrilla → finished-chori table right. Chicago now uses `street-parrilla-large-v4`, with four clear chorizo rows (one more than the former three-row art), small bread rolls matching chorizo scale, and a modest 10% increase in displayed width. The grill stays centered on the shared y=686 baseline with 56.08px visible side gaps; barrel/table offsets and their side approach/pickup points remain paired so each worker can reach its own station without crossing the grill. No recipes, quantities, prices, goals, timers, hiring, FIFO, order handling or HUD behavior changed.

- [x] Four-row transparent artwork imported at 2170×725, no baked checkerboard, Android RGBA32.
- [x] Focused Game-view and pickup-route checks pass; visual screenshot: `Logs/Acceptance/ChicagoGrill-20261006/chicago-four-row-grill-review.png`.

## Shared cover-standard navigation buttons — 2026-10-06

Consolidate the existing IMGUI cover button renderer rather than adding a prefab/uGUI system. Ordinary cover/menu/selector/Ready/result controls use the same generated blue capsule and bundled outlined white Luckiest Guy caption. GAME OVER VOLVER retains its exact return action/position; victory SALIR retains its normalized exit slot/navigation and replaces the earlier green face with the shared style. Ready 1–5 controls retain unlock and input rules. Gameplay, persistence/progression, economics and special upgrade/mural cards are unchanged.

- [x] One shared texture/font/renderer; no duplicate assets or fallback fonts.
- [x] Normal/pressed/disabled states and unchanged locked navigation validated.
- [x] Cover reference preserved; focused mouse/touch routing and result exit tests passed.
- [x] Ordinary controls audited/migrated; special card exceptions documented.
- [x] Future-button rule stored in AGENTS.md.

## Definitive fixed furniture sizing and accessible work block — 2026-10-06

Latest user request supersedes earlier per-level scaling/positions, including Chicago's reduced single-line arrangement and the larger Ferro/Independiente table. Main furniture sizes are measured directly from live Floresta IMGUI: grill282×94 and serving table196×98. StreetWorkstationLayout owns those fixed frames for all five current levels and future levels, independent of product/employee/upgrade counts. Existing grill art fits proportionally; the same Floresta table sprite is reused in every club. Food glyphs identify other ready products. Ferro retains the complete grill width, reusing Chori artwork in the otherwise unavailable fourth food bay rather than stretching a three-bay crop; no unavailable recipe/order is enabled.

Size and position are separate. Final standard table is (16,540,196,98); grill is (242,577,282,94). Drink props sit in the upper work line: Coca ends at x524, beer ends at x364, both y488 and80high with their own source aspect ratios. Fernet uses its proportional94×66 prep table at(16,434). Minimum station gap6; the clear drink corridor is wider than a90pixel worker. No fixed-size table/grill is reduced to fit another prop.

Every food pickup is beside the finished table at(235,575) through(235,480), not the cooking grate. Coca(435,565) approaches via(435,450); beer(395,565) via(395,450); Fernet(155,500) via(155,415). Later levels reuse(433,485) as their upper entry/exit stage. Final-arrival gates alone trigger Pickup/Handoff. Correct existing side-reach/flip/crop and reuse walks; no new animation assets. A faded near-station presentation correction keeps the same hand depth relative to fixed-height props on taller portraits; it does not change simulation, pickup delays or counter handoffs.

Validate all available product paths from all seven counter columns using full90×98 worker+bob and36×12foot bounds. Arms may overlap only the station being reached; feet never enter any station. Pairwise station bounds, screen limits, HUD separation and tall pickup geometry are checked. The live IMGUI scene has no station colliders or sorting layers to migrate. Keep economics, speed/counts, requests/FIFO, goals/durations, unlocks and backgrounds unchanged. See status for executed validation.

- [x] Live Floresta geometry inspected before edits; fixed furniture dimensions shared across all five levels.
- [x] Proportional secondary props relocated without shrinking main furniture; full-body/foot station clearance checked.
- [x] All available products reach Pickup/carry/Handoff from all seven columns; timeout/reservation regressions pass.
- [x] Actual Game View reviewed for all five levels, plus tall portrait drink/table reach; final console clean.
- [x] Permanent workstation rule recorded in AGENTS.md; original editor save/state restored.


## Four specialties and two grills — authoritative update (2026-10-07)

This section supersedes earlier two-role/all-food/all-drinks rules and four-zone shared-grill descriptions. Historical validation reports are retained for traceability, not the current specialty contract.

- **Parrillero → Chori0 + Paty1 only.** Normal grill/table remain exclusive to these IDs.
- **Parrillero Premium → Bondiola2 + Vacío3 only.** They never use the Chori/Paty grill; Chori/Paty never use Premium.
- **Cocacolero → Coca4 + Cerveza6 only.** It cannot serve Fernet, even when idle.
- **Fernetero → Fernet5 only.** It cannot serve Coca/Cerveza, even when idle. Retain the existing1L iced Fernet representation.
- **Todo nivel cuyo catálogo incluya Bondiola y/o Vacío debe crear/habilitar automáticamente una Parrilla Premium y habilitar la contratación del Parrillero Premium.** Both products share one Premium grill, not two grills.
- **Todo nivel cuyo catálogo incluya Fernet debe habilitar automáticamente el Fernetero.** Its existing station and dedicated hire card must be available.
- **Las especialidades y estaciones se derivan del catálogo del nivel y no de su índice.** Synthetic Chori+Vacío+Fernet catalogs at index0 and10 derive Normal+Premium+Fernet, without Coca.
- Level1 has Normal;2–3 Normal+Coca;4 Normal+Premium+Coca (no Fernet);5–11 all four with the current unchanged catalogs. Product IDs, sale prices/goals/timers/demand/club themes are not changed.
- Mixed orders may have up to five types as before. Different specialties process lines concurrently; same-role ownership, FIFO, reservation cleanup and handoff-gated income remain required. Customers cannot finish until all lines are delivered.
- All four hiring profiles currently share **$15/$30/$60/$100/MAX**; each owns its paid tier/cap. The free initial normal worker is excluded from paid-tier calculation. Fresh attempt/retry/next/load has one free Normal, zero other roles, speed×1.00 and zero attempt coins.
- Reuse wood/iron cards and original atlas pose geometry. Premium’s blue outfit and Fernetero’s black apron distinguish roles without rescaling characters. Normal/Premium grill282×94 and normal table196×98 remain constant.

### Definition of Done for every new level

Analyze its Product IDs automatically through StreetSpecialties. Each available product must have its mapped specialty, station, clean route and corresponding hire card. No index gates or unowned product are allowed. Test catalog-derived visibility, station/pickup separation, independent hiring, combined-line ownership/delivery and cancellation. Review real Game View (including5-card HUD when applicable), not EditMode alone.

### Validation for this change

Implementation and automatic/visual validation results are recorded in the current status entry. Do not interpret earlier two-role test reports as validation of this contract.

- [x] Central product/role/station mapping; synthetic index0/10 catalogs and all11 unchanged live catalogs.
- [x] Separate Premium grill and exclusive four-role ownership, mixed handoffs, FIFO/cancel/expiry cleanup.
- [x] One shared cost profile, independent $15/$30/$60/$100/MAX tiers and free-normal baseline; v1/v2→v3 saves.
- [x] Blue Premium / black Fernetero, original world-worker geometry/poses; dynamic2/3/4/5-card HUD and mouse/touch hires.
- [x] Unity automatic suites and real Game Views; detailed evidence and limitations in status.
