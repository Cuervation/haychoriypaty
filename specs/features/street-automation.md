# Feature: Street automation and progression

## Authority and scope

This newly requested milestone supersedes the delivery boundary of the [historical Floresta prototype](floresta-prototype.md), not its validation record. [Product](../product.md) retains the exact seven-product catalog and outdoor standing-only setting. [Visual reference](../visual-reference.md) distinguishes observed video evidence from prompt requirements. No copied Food Fever assets, new services/packages, manual movement or complex cooking controls.

Playable Floresta first; reuse the same small scene/simulation for Nueva Chicago, Argentinos Juniors, Vélez and Ferro. Defaults below are implementation balance choices, not values inferred from unseen video action; all gameplay values remain inspector-editable.

## Rules and state contract

- Floresta starts with chori fixed at $5: never show a price popup, read-only price summary, slider/drag target or price-selection replay text. Startup Jugar enters gameplay directly; if Ready is reached after replay, retain only the non-modal blue Jugar button and unlocked-level navigation, with no price panel. Normalize older saved chori prices to $5 without changing coins or other product prices; opening staff/speed follow the first-level round reset below. Later levels show the selected product sprite, price slider and Start button. Their per-product price is editable from $0–$60 by default, initially $5 (configurable; the observed $52 does not prove the video maximum); a lower price increases configurable arrival demand, a higher price reduces it. Demand uses the mean appetite of unlocked product prices; below 0.8 appetite it also reduces automatic crowd capacity and bulk quantity, while weighted product selection favors affordable products. In later levels, the default $5 retains 21 clients/999-unit bulk orders; expensive prices trade revenue per unit for slower, smaller demand. These curves are balance choices, not inferred video rules. Charge that product's actual chosen price only after a successful handoff.
- Begin with zero coins and one worker. Customers automatically enter, move to reserved standing positions, wait, receive products and exit. Support up to 21 simultaneous customers in seven columns/three rows; arrival rate and capacity are configurable per level.
- Each Floresta customer requests 1–4 choris (uniform random inclusive, including the first arrival). Complete the requested units and animate receipt/departure; then the existing people behind advance in the same column and only its tail may receive a newcomer. Across all levels, preserve each column as FIFO: workers serve only its settled front customer, never a waiting person behind or a newcomer placed ahead. No forced999 exception in this level. Later customers request one unlocked product and an integer quantity from 1 through999; simultaneous customers may request different products. Display product sprite, remaining quantity and individual patience. A handoff decreases remaining quantity by exactly one, increments delivered-unit total by one and earns exactly one unit price. There is no counter jump, reward at pickup, or fake timer-only delivery.
- Workers automatically reserve an outstanding unit, move to its station, pick up, carry to the customer's handoff position, deliver, then repeat. A configurable short station/condiment delay can remain automatic, but must not interrupt the circuit with manual cooking. Food always includes bread; condiments remain automatic and are never extra products.
- Hire up to eight independently moving workers by default. Reservations prevent duplicate delivery to the same final unit; clients that leave release reservations, and in-flight workers cancel safely without earning coins or decrementing another order. Delivery refreshes customer patience. Waiting expiry makes the customer leave and compacts that same FIFO column, without changing the identities/orders/patience of those advancing.
- Simulation position/state is authoritative: revenue and quantity changes are gated by arrival and handoff, not by an unrelated view timer. View animates entering/waiting/receiving/exiting customers and directional worker walking/pickup/carry/handoff; coin amount/effect, upgrade and hire feedback reflect actual events.
- Floresta speed costs $25 on every purchase and adds10percentage points of base movement/productivity per upgrade (x1.00→1.10→1.20). Each extra parrillero/ayudante costs $200 without escalation; retain the eight-worker cap. Later-level speed starts$5/+25percentage points and hire starts$15, retaining their existing escalating cost rules; purchases update availability/prices and affect movement/productivity immediately. Reject insufficient-funds purchases without side effects, and dispatch each tap exactly once.
- Floresta (level1) wins immediately when 1000 actual choripán handoffs have been sold; retain the user-confirmed 180-second limit and lose at its deadline if below the goal. Default `florestaFinishAtDeadline` is false (the old deadline-only experiment remains an optional inspector toggle, not the active Main rule). Stop further worker handoffs at the winning unit so the first-level counter freezes at1000/1000. Other levels keep their existing goals/early victory/time limits. Results show outcome and delivered units; successful completion unlocks the next location. Do not require clearing every customer to win; preserve coins/unlocks on replay; every new Floresta turn starts with one parrillero and speed x1.00, while later-level staff/speed persistence remains unchanged.

## Progression defaults

Products unlock in catalog order: chori, paty, bondiola, vacío, Coca cup, Fernet/Coca large cup, beer can. Every unlocked product has its own original sprite and station; only Floresta chori has a fixed price, later-level prices remain editable.

| Location | Unlocked product count | Unit goal | Round seconds |
|---|---:|---:|---:|
| Floresta / All Boys | 1 | 1000 | 180 |
| Nueva Chicago | 3 | 40 | 210 |
| Argentinos Juniors | 5 | 65 | 240 |
| Vélez | 6 | 85 | 270 |
| Ferro | 7 | 110 | 300 |

Demand, duration, goal, limits, speed, station delays, patience and upgrade costs remain configurable. Persist unlocked level, prices, purchased upgrades/workers and coins in a versioned JSON value under a dedicated PlayerPrefs key; reject/reset malformed or incompatible data safely. On the one-time v1→v2 migration, reset inflated development workers/speed to one worker and base speed while preserving unlocked level, coins and prices; v2 saves persist purchases normally for later levels; constructing or starting a fresh Floresta round discards previous staff/speed upgrades (one parrillero, SpeedLevel0 = x1.00) without resetting coins, unlocks or other product prices. This is progression/economy persistence, not a claim of full in-flight round resume. Define reset/replay handling explicitly in code and test it so restored purchases are not charged again.

## Minimal integration

`StreetGame` owns lifecycle, inspector `StreetBalance`, management commands and persistence; `StreetSimulation` is pure deterministic runtime data/logic with bounded substeps and capped frame catch-up; `StreetView` renders actual state with original replaceable sprite assets. Main uses the new pair, disabling legacy `GameController`/`PrototypeView` there while retaining their files/tests. Keep one safe-area transform for drawing and pointer hit-testing (540×960 logical portrait canvas); use Input System bridge on editor/desktop and native GameActivity IMGUI events on Android, never both dispatching the same action.

No new framework, service, generalized content pipeline or package is necessary. Fixed customer/worker caps and bounded catch-up make load controllable; actual device profiling remains required before any performance claim.

## Acceptance — evidence scoped below

- [x] Main opens/runs without errors and only the intended new runtime/view owns gameplay/input.
- [x] Historical imported art matched the then-current reference composition before the open-street change below; no placeholder geometric actors, copied assets, 3D/isometric/cenital view or seated diners. All seven product sprites/stations are connected to their unlocked levels.
- [x] Historical/later-level price panel changes actual demand and revenue; start works with editor mouse and Android touch, with aligned safe-area hit bounds.
- [x] Dozens of customers enter and group in rows; each shows correct product, remaining quantity and patience with actual enter/wait/receive/exit states.
- [x] Floresta rotates all 15 logo-free wardrobe overlays over existing fan poses; focused import/rotation checks and representative portrait Game-view placement review passed.
- [x] A configured 999-unit order decreases by exactly one on each real handoff; cancellations/departures cannot duplicate decrements or earnings.
- [x] One worker visibly performs station→pickup→carry→handoff repeatedly; hired workers coordinate reservations and increase measured throughput.
- [x] Coin effects/amounts occur only for real deliveries. Historical/later-level $5 speed and $15 hire initial costs, escalation, affordability and immediate response verified; Floresta now uses the separate rules below.
- [x] Win/lose/results/replay work end-to-end; five increasing levels unlock exactly the catalog counts above, never an eighth product.
- [x] Versioned save/load restores supported progression/economy state and handles corrupt data; no claim of full-round resume without a dedicated test.
- [x] One-time v1→v2 save migration resets development starting staff/speed to 1 worker and ×1.00 while preserving unlocks, coins and prices.
- [x] Directional walk/carry/pickup/handoff, customer states and management feedback are actual animations, not static claims.
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

## Floresta small orders and fixed economy — 2026-10-04
- [x] Historical fixed-$5/no-slider, drag rejection, replay/start and saved-price normalization checks passed before the popup removal below.
- [x] Historical +10%/$5 speed and $25 helper costs were verified before the cost/reset change below; these results do not validate the current $25/$200 rules.
- [x] Automatic1–4 orders, exact per-unit income, completed-customer exit and replacement verified.
- Historical slice preserved180seconds/goal24 and original art/progress; current Main uses the1000-unit early-win rule below. User explicitly requested noAPK for this change; only focused editor checks.

Current editor verification:32/32simulation+9/9art jobc900430c9df54e1ab041b0a2475b6e54 and11/11pointer job81416a6df3c746eb8fa26d209fbcf885,0failed/skipped. Ready/Playing Game-view confirms no price slider,$5/$25/+10%,1–4 orders and large parrilla. First-level180s deadline regressions passed. The original noAPK instruction applied to that source-change turn; subsequent large-parrilla request explicitly authorizes APK build, not phone install.

## Startup cover/title/menu — 2026-10-04

Keep the selected cover and transparent logo unchanged. Using unscaled time, hold the fully visible cover for at least 4 seconds, reveal the logo with the existing brief warm flash, and hold the fully visible logo for at least 4 more seconds before showing **Jugar** and **Salir**. Keep cover/logo/menu visible until an explicit choice, rather than automatically fading into Ready. Buttons use original glossy cyan→blue rounded geometry and white, dark-outlined Luckiest Guy lettering inspired by the supplied blue NEXT reference; do not reuse its pixels. Ship the Apache 2.0 font license.

Jugar starts the selected saved level directly through StreetGame.StartRound, without another Empezar step. Salir calls Application.Quit in the player (stops Play mode in the Editor). Block hidden gameplay controls throughout the presentation/menu and all menu actions before the minimum durations. Keep the shared safe-area transform and existing native Android/desktop input ownership; no extra scene/package, coin/unlock reset or new products; the newer first-level cost/staff/speed reset rules below apply.

- [x] Cover is fully visible for ≥4s before logo begins; logo is fully visible for ≥4s before buttons appear, including at timeScale 0.
- [x] Persistent menu shows readable blue Jugar/Salir; no hidden level/price/upgrade/start input leaks through.
- [ ] Mouse/native Android touch: Jugar enters Playing directly; Salir exits the player.

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


## Floresta upgrade costs and fresh-round baseline — source update (2026-10-04)

Speed costs $25 per purchase (+10 percentage points of base speed unchanged); each helper costs $200, with no cost escalation in level 1. Both code defaults and Main serialized balance use these values. Every newly constructed or started first-level round has one parrillero and x1.00 speed (zero purchased speed upgrades), including replay, saved first-level starts and returning to level 1 before Start. Remove old workers/reservations before starting; do not refund/reset coins, prices, unlocked levels or later-level purchases.

- [x] First-level UI/purchases use $25 speed and $200 helper, including repeated purchases and insufficient funds.
- [x] Fresh first-level launch/replay/start returns to one parrillero and x1.00, preserving coins/unlocks and later-level behavior.

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


## Thousand-choripán goal and corner counters — source update (2026-10-04)

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
- Android0.2.5/code7 build and packaged adaptive logo verified; native touch/Salir and launcher-mask/device review remain pending because USB Motorola disconnected. No physical FPS/thermal or extended device stress certification implied. See`android-prototype.md` current delivery evidence.
