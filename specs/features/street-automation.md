# Feature: Street automation and progression

## Authority and scope

This newly requested milestone supersedes the delivery boundary of the [historical Floresta prototype](floresta-prototype.md), not its validation record. [Product](../product.md) retains the exact seven-product catalog and outdoor standing-only setting. [Visual reference](../visual-reference.md) distinguishes observed video evidence from prompt requirements. No copied Food Fever assets, new services/packages, manual movement or complex cooking controls.

Playable Floresta first; reuse the same small scene/simulation for Nueva Chicago, Argentinos Juniors, Vélez and Ferro. Defaults below are implementation balance choices, not values inferred from unseen video action; all gameplay values remain inspector-editable.

## Rules and state contract

- Floresta starts with chori fixed at $5: never show a price popup, read-only price summary, slider/drag target or price-selection replay text. Startup Jugar enters gameplay directly; if Ready is reached after replay, retain only the non-modal blue Jugar button and unlocked-level navigation, with no price panel. Normalize older saved chori prices to $5 without changing other product prices; every fresh attempt resets coins as specified below; opening staff/speed follow the first-level round reset below. Later levels show the selected product sprite, price slider and Start button. Their per-product price is editable from $0–$60 by default, initially $5 (configurable; the observed $52 does not prove the video maximum); a lower price increases configurable arrival demand, a higher price reduces it. Demand uses the mean appetite of unlocked product prices; below 0.8 appetite it also reduces automatic crowd capacity and bulk quantity, while weighted product selection favors affordable products. In later levels, the default $5 retains 21 clients/999-unit bulk orders; expensive prices trade revenue per unit for slower, smaller demand. These curves are balance choices, not inferred video rules. Charge that product's actual chosen price only after a successful handoff.
- Every level attempt begins with zero coins: clear the previous balance on load, level selection, next level, retry and Start. Coins earned during an attempt remain spendable only in that attempt; no carryover or refund. Keep unlocks, product prices and the existing staff/speed rules. Customers automatically enter, move to reserved standing positions, wait, receive products and exit. Support up to 21 simultaneous customers in seven columns/three rows; arrival rate and capacity are configurable per level.
- Each Floresta customer requests 1–4 choris (uniform random inclusive, including the first arrival). Complete the requested units and animate receipt/departure; then the existing people behind advance in the same column and only its tail may receive a newcomer. Across all levels, preserve each column as FIFO: workers serve only its settled front customer, never a waiting person behind or a newcomer placed ahead. No forced999 exception in this level. At Nueva Chicago, orders are randomly chori-only, bottled-Coca-only, or both; each product quantity is independently 1–4, and combined requests complete all choris before Coca. Other later customers request one unlocked product and an integer quantity from 1 through999; simultaneous customers may request different products. Display all requested product sprites, remaining quantities and individual patience. A handoff decreases the corresponding quantity by exactly one, increments delivered-unit total by one and earns exactly that product's unit price. There is no counter jump, reward at pickup, or fake timer-only delivery.
- Workers automatically reserve an outstanding unit, move to its station, pick up, carry to the customer's handoff position, deliver, then repeat. A configurable short station/condiment delay can remain automatic, but must not interrupt the circuit with manual cooking. Food always includes bread; condiments remain automatic and are never extra products.
- On Nueva Chicago, keep idle workers and every handoff at logical y=485 on the player-side floor, below the counter front (bottom y=384). Their station route may approach the grill/table/barrel, but must never send the parrillero across or visually through the counter. Other levels retain their existing handoff positions.
- Hire up to five independently moving workers by default. Reservations prevent duplicate delivery to the same final unit; clients that leave release reservations, and in-flight workers cancel safely without earning coins or decrementing another order. Delivery refreshes customer patience. Waiting expiry makes the customer leave and compacts that same FIFO column, without changing the identities/orders/patience of those advancing.
- Simulation position/state is authoritative: revenue and quantity changes are gated by arrival and handoff, not by an unrelated view timer. View animates entering/waiting/receiving/exiting customers and directional worker walking/pickup/carry/handoff; coin amount/effect, upgrade and hire feedback reflect actual events.
- Speed/hire purchases use the explicit progressive tables below in every level, with a five-worker cap and speed x1.9 maximum. Purchases update the next cost and affect movement/productivity immediately. Reject insufficient-funds or capped purchases without side effects, and dispatch each tap exactly once.
- Floresta (level1) wins immediately when 200 actual choripán handoffs have been sold; retain the user-confirmed 120-second limit and lose at its deadline if below the goal. Default `florestaFinishAtDeadline` is false (the old deadline-only experiment remains an optional inspector toggle, not the active Main rule). Stop further worker handoffs at the winning unit so the first-level counter freezes at200/200. Nueva Chicago uses its separate two-product goal below; the remaining later levels keep their existing goals/early victory/time limits. Results show outcome and delivered units; successful completion unlocks the next location. Do not require clearing every customer to win; preserve unlocks but reset coins on replay; every new Floresta turn starts with one parrillero and speed x1.00, while later-level staff/speed persistence remains unchanged.

## Progressive speed and hiring prices — current contract (2026-10-04)

Use these exact tables for later locations; Floresta has a separate affordable first-level curve below. Initial speed x1.0 and the first worker are free. Each speed purchase adds0.1of base speed; each hire adds one worker. The next price changes immediately after a successful purchase. Stop at speed x1.9 / five workers: exhausted cards display MAX and reject purchases without debiting coins. Do not extrapolate prices past the listed rows. Preserve zero coins per attempt and first-level team/speed reset; later-level purchases still persist, with older out-of-range counts clamped to the new caps.

| Speed tier | Work rate | Cost |
|---|---:|---:|
| Initial | 1.0 | Free |
| 2 | 1.1 | $25 |
| 3 | 1.2 | $40 |
| 4 | 1.3 | $65 |
| 5 | 1.4 | $100 |
| 6 | 1.5 | $160 |
| 7 | 1.6 | $250 |
| 8 | 1.7 | $400 |
| 9 | 1.8 | $640 |
| 10 | 1.9 | $1000 |

| Total workers after hire | Cost |
|---|---:|
| 1 (initial) | Free |
| 2 | $200 |
| 3 | $500 |
| 4 | $1200 |
| 5 | $2800 |

### Floresta first-level affordability curve — playability adjustment (2026-10-04)

Only the first level uses these lower costs; subsequent locations retain the tables above. Speed remains +0.1 per tier, starting x1.0 free and capped at x1.9; the initial parrillero is free and the team still caps at five. The arrays are independently inspector-configurable.

| Speed tier | Work rate | Cost |
|---|---:|---:|
| Initial | 1.0 | Free |
| 2 | 1.1 | $5 |
| 3 | 1.2 | $10 |
| 4 | 1.3 | $15 |
| 5 | 1.4 | $20 |
| 6 | 1.5 | $30 |
| 7 | 1.6 | $45 |
| 8 | 1.7 | $65 |
| 9 | 1.8 | $90 |
| 10 | 1.9 | $125 |

| Total workers after hire | Cost |
|---|---:|
| 1 (initial) | Free |
| 2 | $15 |
| 3 | $30 |
| 4 | $60 |
| 5 | $100 |

The deterministic first-level purchase playthrough spent only earned $5-per-choripán income, bought all four helpers and nine speed tiers, and won at 200/200 in 98.9s with $390 remaining. That measured run was under the former 180s limit; 98.9s is 21.1s below the new 120s limit, but the playthrough was not rerun after the timer change. The old curve lost at 82/200 even while buying affordable upgrades. No bonus coins or starting team were injected in the successful run.

- [x] Source uses explicit serialized/default/fallback price tables, updates next-tier costs, clamps old purchases and prevents purchases past the last row.
- [x] Source/Main first-level goal is200; keep120seconds, $5 per choripán, exact early win and Goal-based HUD.
- [x] Deterministic purchase-driven first-level simulation reaches 200/200 before the deadline using only earned sales income.
- [x] Focused Unity regressions cover the win path, displayed tier costs and the upgrade-card purchase input.

**Validation:** Unity6000.6.3f1 batch: `StreetSimulationTests` **50/50 passed**; focused `UpgradeCardsUpdateNextCostsAndStopAtMaximum` PlayMode test **1/1 passed**. The simulation won at200/200 in98.9/180seconds, reaching five workers/x1.9 and ending with$390. `git diff --check` passed. This was a deterministic Unity simulation and focused editor tests, not a manual phone playthrough. No Android build/APK or phone installation was requested or performed.

## Progression defaults

Keep product IDs/saved price slots stable: chori (0), paty (1), bondiola (2), vacío (3), Coca-Cola 600 ml bottle (4, replacing the old cup art), Fernet/Coca large cup (5), beer can (6). Nueva Chicago offers only IDs 0 and 4, deliberately skipping Paty without remapping catalog IDs. Subsequent levels retain their existing progression. Every unlocked product has its own original sprite and station; only Floresta chori has a fixed price, later-level prices remain editable.

| Location | Unlocked product count | Unit goal | Round seconds |
|---|---:|---:|---:|
| Floresta / All Boys | 1 | 200 | 120 |
| Nueva Chicago | 2 | 200 choris + 200 Coca bottles | 180 |
| Argentinos Juniors | 5 | 65 | 240 |
| Vélez | 6 | 85 | 270 |
| Ferro | 7 | 110 | 300 |

Demand, duration, goal, limits, speed, station delays, patience and upgrade costs remain configurable. Persist unlocked level, prices and purchased upgrades/workers in a versioned JSON value under a dedicated PlayerPrefs key; reject/reset malformed or incompatible data safely. The legacy coins field remains JSON-compatible but its stored balance is discarded on load and never transferred to a new attempt. On the one-time v1→v2 migration, reset inflated development workers/speed to one worker and base speed while preserving unlocked level and prices; v2 saves persist purchases normally for later levels. Constructing or starting a fresh Floresta round also discards previous staff/speed upgrades (one parrillero, SpeedLevel0 = x1.00), preserving unlocks and other product prices. This is progression persistence, not full in-flight round resume. Define reset/replay handling explicitly in code and test it so restored purchases are not charged again.

## Minimal integration

`StreetGame` owns lifecycle, inspector `StreetBalance`, management commands and persistence; `StreetSimulation` is pure deterministic runtime data/logic with bounded substeps and capped frame catch-up; `StreetView` renders actual state with original replaceable sprite assets. Main uses the new pair, disabling legacy `GameController`/`PrototypeView` there while retaining their files/tests. Use one safe-area transform for drawing/hit-testing. Preserve a uniform sprite scale but expand the portrait logical height with available display aspect instead of fitting a fixed 540×960 canvas and leaving gameplay letterbox bands. Use Input System bridge on editor/desktop and native GameActivity IMGUI events on Android, never both dispatching the same action.

No new framework, service, generalized content pipeline or package is necessary. Fixed customer/worker caps and bounded catch-up make load controllable; actual device profiling remains required before any performance claim.

## Acceptance — evidence scoped below

- [x] Main opens/runs without errors and only the intended new runtime/view owns gameplay/input.
- [x] Historical imported art matched the then-current reference composition before the open-street change below; no placeholder geometric actors, copied assets, 3D/isometric/cenital view or seated diners. All seven product sprites/stations are connected to their unlocked levels.
- [x] Historical/later-level price panel changes actual demand and revenue; start works with editor mouse and Android touch, with aligned safe-area hit bounds.
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

Jugar opens the level selector after the existing 4+4-second presentation; it does not start a round. Only levels at or below persisted `UnlockedLevel` are active. Selecting Floresta starts its fixed-price round directly; selecting later levels opens their existing price/setup screen before Start. The victory result has one action, **Salir**, which resets the finished attempt and returns to the level selector (the next location is unlocked only after a win). Presentation-menu Salir still calls `Application.Quit` in the player (stops Play mode in the Editor). Block hidden gameplay/selector actions during intro and locked-card actions at all times. Keep the shared safe-area transform and existing native Android/desktop input ownership; no extra scene/package, unlock reset or new products; the round-local coin reset and first-level cost/staff/speed reset rules below apply.

- [x] Cover is fully visible for ≥4s before logo begins; logo is fully visible for ≥4s before buttons appear, including at timeScale 0.
- [x] Persistent menu shows readable blue Jugar/Salir; no hidden level/price/upgrade/start input leaks through.
- [ ] Mouse/native Android touch: Jugar opens the level selector; an unlocked tile enters its level path; only the preceding victory unlocks the next tile; victory-popup Salir returns to selector; presentation Salir exits the player.

**Implementation/validation:** Source updated (0.28s cover fade + 4s cover hold + 0.35s logo reveal + 4s logo hold = 8.63s before buttons). Existing startup regression source adapted, **not run**. User explicitly deferred tests, Play/device review and APK generation until later combined validation. Prior 0.2.4 evidence below does not validate this changed flow.

### Historical startup 0.2.4 validation — before the menu change

Use the selected user-supplied cover and separate transparent logo unchanged. The 4.1-second unscaled intro shows the cover first, reveals the logo with a single short warm flash, then fades to the existing Ready screen without starting a round. Gate both native Android IMGUI and desktop bridge actions during the intro; no extra scene, package, video playback or gameplay changes.

- [x] Both resources import at source dimensions with Android RGBA32/no mipmaps; 11/11 focused art tests passed.
- [x] Intro blocks actions, ends with timeScale 0, stays Ready and permits Start afterward; 12/12 editor pointer tests passed (job adfc5a956d4f48548c73f34aa2caec0d).
- [x] Actual 1080×1920 Game-view cover/logo-flash/Ready captures reviewed in ignored Logs/Acceptance/Intro/.
- [x] Android0.2.4/code6 APK built; V2 signature/package/ARM64/min26/target36 verified. User subsequently requested phone installation: 0.2.4/code6 install-r Success and cold launch Statusok verified; new native appearance/touch remain unchecked (see Android delivery).


## Floresta without price popup — source update (2026-10-04)

Ready never draws the price panel/background, product/amount/demand summary or hidden product hit targets in level 1; only non-modal Jugar and level navigation remain. Existing simulation already forces chori to $5 on construction, saved-price restoration and level selection; no economy/persistence changes are needed. Later-level price selection stays unchanged.

- [x] First entry/replay/return to Floresta has no price popup and still charges $5 per delivered chori.

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

The Main scene uses `StreetView`, not the retired `PrototypeView`. On the existing simulation `Lost` phase (timer expires below goal), render the full-bleed versioned angry-crowd/stall-destroyed scene instead of the ordinary playfield. Top counters still show the frozen outcome and zero time; add a short clear riot headline, sales/goal and replay control. Animate background rumble and flying debris with unscaled time so the mayhem continues after simulation stops. Do not change timeout rules, customer behavior during play, balance, sales or saved progression; victory presentation remains unchanged.

- [x] Dedicated angry/sticks/broken-stall art resource wired only into active `StreetView` loss result.
- [x] Riot motion continues on unscaled time; result state exposes a visible replay target.
- [x] Focused PlayMode test passes for real timeout, loaded riot resource, moving debris and replay return (1/1).

**Validation:** Source/art integration is new; earlier `PrototypeView` riot code is only legacy evidence, not Main verification. User authorized one relevant test after completion. No APK/device install requested.

**Validation (2026-10-04):** Unity6000.6.3f1 filtered PlayMode test `HayChoriYPaty.Tests.StreetPointerTests.ReplayReturnsToReadyWithoutStartingAutomaticallyAndDiscardsCoins` passed1/1,0failed/0skipped. It triggers a 0.05-second level deadline, confirms `Lost`, active Main riot texture/state and changed debris trajectory, then taps replay and confirms Ready with coins reset. Batch compilation/import succeeded in this final run. Actual Game-view/device appearance was not checked; no APK. First attempts surfaced stale win-panel compiler references and OnGUI-only state initialization under `-nographics`; both were corrected before the passing run.


## Presentation-to-level selector and victory return — 2026-10-05

The presentation's **JUGAR** now opens a dedicated level selector rather than starting the saved location automatically. Keep the current five canonical locations; only indices `0..UnlockedLevel` are tappable, and saved progression unlocks the next index after the preceding level is won. Floresta starts directly from its unlocked card (fixed $5, no price panel); later unlocked locations continue to their existing product-price setup before starting. Existing presentation **SALIR** still exits the app. The level-1 card uses the original All Boys mural and shield; level 2 uses its Nueva Chicago mural and shield; locked later cards preview the game grill/product set with club-color accents instead of invented club crests. The win result removes price replay/next-cancha actions, summarizes sales, round coins earned and remaining time, and has a single **SALIR** that returns to the selector without quitting or starting another round. Loss/riot retry behavior remains unchanged.

- [x] Source opens selector from presentation Jugar and gates card selection by persisted unlock level.
- [x] Source uses the existing All Boys/Chicago mural resources for the first two card illustrations; later locked cards use original game product art and accent colors.
- [x] Win-result source has relevant round summary, only Salir, and returns to selector after recording the unlock.
- [ ] Focused pointer/progression tests, Unity import and actual portrait Game-view review remain pending; user asked not to run tests until requested.

**Validation:** Source/spec review only. No Unity editor/player, tests, APK or device actions were run.


## Live timeout rage and stall-break sequence — 2026-10-05

Refine the earlier Lost-state vignette: a single angry-crowd composite is not the event. When the deadline expires below goal, render the actual frozen `Waiting`/`Receiving`/`Advancing` queue in its existing slots. Their existing looks first jitter and raise anger cues, then fade into angry supporter poses in the same team-specific outfits they wore while waiting, with raised-stick and swing-down non-contact frames. Alternate two frames per person at 6 fps with staggered phases on unscaled time so the sequence continues while the simulation remains frozen. Start the broken-environment crossfade after the anger reaction, then keep the live fan sprites in front of the shattered stall with moving wood debris and rumble. Preserve the result counters, replay input, economy and progression. `street-riot-defeat-v1` remains archived; Floresta uses the nine-outfit `street-allboys-riot-fans-v1` atlas, Level 2 uses the 4×4 `street-chicago-riot-fans-v2` atlas with the same eight integrated outfits and customer-ID mapping as its regular crowd; `street-riot-fans-v1` remains the generic fallback. Future teams follow the same team-specific, full-body wardrobe mapping across normal and riot states.

- [x] Timeout animation uses actual waiting queue positions and staggered angry/stick-swing frames, not a static crowd plate.
- [x] Broken counter/environment fades in after the anger beat; supporters and debris continue moving on unscaled time.
- [x] A large comic smoke-and-impact cloud rapidly forms over the queue at the gameplay-to-riot cut, then dissipates to reveal the angry crowd and destroyed stand; it is presentation-only and uses unscaled time.
- [ ] Focused timeout/replay test source now checks the new resources and pose alternation; execution and Game-view/device review are pending.

**Validation:** Added the versioned transparent smoke asset/import metadata and wired it into the active `StreetView` loss transition. No tests, Unity/Game view, APK or device operations were run per the user's standing instruction.

## Nueva Chicago bottle service and order variants — 2026-10-04

Level 2 now uses the new full-street Nueva Chicago mural plate and C.A.N.CH. footer shield. Its two sellable items are product IDs 0 (chori) and 4 (Coca-Cola 600 ml bottle), with the older cup art replaced at the same stable price-array slot. One blue ice barrel stocked with bottles sits to the right of a smaller grill; the used choripán table remains to its left. Worker routes use separate side approaches instead of walking across the parrilla.

Each arriving customer chooses one of three order variants: chori-only, Coca-only, or both. Each requested quantity is independently uniform 1–4. A combined order must finish its choris before assigning Coca deliveries; each actual handoff counts toward the corresponding per-product objective and earns the selected price for that specific product. Orders show both remaining product quantities in two rows.

- [x] Level 2 exposes only catalog IDs 0 and 4 without shifting saved product prices or adding an eighth item.
- [x] Added bottle/barrel props, Chicago background/shield, side-by-side station bounds, bottle product tabs/icons, dual-product order display, and sequential mixed-order fulfillment.
- [x] Focused Unity6000.6.3f1 checks passed: StreetSimulation EditMode53/53, StreetArt EditMode42/42, and StreetPointer PlayMode17/17.
- [ ] Game-view/phone composition review and native-device behavior remain pending.

No APK was built or installed. These tests cover simulation variants/quantities/revenue/priority, imported resources/geometry, and price-tab mapping; they do not verify device visual composition.

## Nueva Chicago sales objective — 2026-10-05

The Level-2 objective is **200 successfully handed-off choris and 200 successfully handed-off Coca bottles**, not 200 combined. Both products start at a $5 unit sale price; later-level price controls remain available. Track product IDs 0 and 4 independently while retaining `Delivered` as the aggregate in-round count. Use `levelGoals[1]` as the per-product target (200 by default); win only once both counters reach it, and at the deadline lose unless both do. The upper sales capsule shows each product's separate `current/target` progress. Coin rewards still occur only at handoff.

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
