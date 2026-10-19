# Modular kitchen — requested 2026-10-07, implemented and validated

> Desde Racing (nivel6), [active-cooks-mozos](active-cooks-mozos.md) reemplaza únicamente la producción/traslado de sándwiches y el layout inferior. El flujo automático y la geometría aquí documentados continúan en niveles1–5.

> Ingredient-backed campaign stock is governed by [Mercado](market-fridge.md): capacities/default mise-en-place below never grant extra ingredients. Racing+ has no automatic initial/background sandwich production; use the active-cook contract. Later dated presentation corrections supersede earlier dimensions/placement notes only where specified.

## Scope / visual authority

The user-supplied [layout reference](../../Assets/Art/Reference/modular-kitchen-layout-reference-20261007.jpg) is composition/style guidance only, never gameplay background. Requested: two separate empty grills with independently placed Chori/Paty vs Bondiola/Vacío; individual Raw/Cooked/Passed meat visuals (Cooking remains an internal timer phase), compact thick cylindrical Bondiola; independent bread sandwiches; three equivalent-size tables for normal/premium/Fernet; separate blue plastic beer/Coca barrels and containers/ice; Fernet served in an 800 cc disposable plastic cup with ice and no straw, with 1 L Branca and large Coca bottles displayed on the left side of its tabletop and prepared Fernet cups on the right. Bottom grills side by side, active preparation stations reflowed in catalog order Normal table→Premium table→Fernet table→Coca barrel→Beer barrel, clear circulation before counter; preserve UI, club scenery/fans/riots and catalog progression.

## Verified starting baseline / resolved prerequisites

Active checkout `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`, clean starting commit `ff65e2a`; Unity6000.6.3f1 MCP connected and Main hierarchy inspected. Main is IMGUI StreetGame/StreetView; no current station/food SpriteRenderer prefab instances. Assets/Prefabs contains only .gitkeep; furniture art includes baked food. Legacy GameController/PrototypeView are disabled, not concurrent gameplay.

1. **Real cooking/stock semantics are absent.** StreetSimulation.cs:689–715 uses pickupSeconds/WorkRate then carries a reserved customer-order unit. Lines:821–837 reserve demand, not physical food; handoff:872–896 consumes a unit of demand and earns revenue. There are no individual food entities, raw/cooked/burned states, table inventory/capacity or refill policy. Legacy GameController.cs:156–211 cooks whole Chori/Coca orders and awards whole-order income; enabling it would violate the single-simulation and existing handoff contract. Do not equate order Reserved with table stock, animate invented cooking clocks, or borrow legacy8-second/capacity values without approval.
2. **Requested one-row geometry conflicts with current fixed portrait sizes.** World canvas width540; each grill282×94, table196×98. Two grills plus minimum6 spacing require570. Three equal-size tables alone require588, before two barrels/gaps. Therefore exact left-to-right rows cannot fit while retaining current visual dimensions and no overlap. Requires approval for a separate responsive scene viewport/uniform world scale (HUD unchanged), or a portrait wrap/layout exception. No camera/orientation/scale changes made yet.

## Approved design (user: “Avanza”, 2026-10-07)

Both prerequisites above are explicitly authorized. Extend **only StreetSimulation**, not the disabled legacy motor. Kitchen geometry uses a uniformly scaled760-wide authored composition inside the existing540-wide canvas; HUD, camera orientation, worker proportions, catalog and club scenery remain unchanged. This supersedes the former fixed furniture visual-size rule for this feature.

### Real item lifecycle / defaults

`StreetKitchenProduction` is owned/stepped by the existing simulation. Stable food IDs move Grill→Table→Carried→consumed only at validated physical handoff. Order reservations are still demand, not stock. Meat states are Raw→Cooking→Cooked; cooked grill meat transfers to its specialty table when a slot is free, otherwise burns after20s, then is discarded after1s. Burned units cannot be served. Prepared table sandwiches never burn. Raw items cannot be taken. All production follows actual availability and the hired responsible specialty; levels1–5 automatic meat preparation uses CookWorkRate (+5% per speed upgrade), while beverages retain WorkRate (+10%). Existing pickup/handoff delays, economics and progression remain intact.

Configurable defaults: cook4s; burn20s; discard1s; drinks preparation0.28s; normal table48total, Premium36total, Fernet45total, each barrel12. Fresh attempts begin with real prepared mise-en-place at these capacities, divided between currently available products; this preserves delivery throughput without inventing revenue. No prior storage capacity existed to override. Normal grill18slots (12Chori/6Paty), Premium7slots (4Bondiola/3Vacío); single-product catalogs use available meat only. Empty pickup waits; full table holds cooked grill items until room/burning. Cancellation returns an edible carried unit if room, otherwise discards without income. Reset/retry/select rebuild IDs/stock; no in-flight save added.

### Independent native visual projection

Actual reusable SpriteRenderer prefabs and live per-ID GameObjects project this domain. Transparent offscreen kitchen/carry cameras provide render textures composed with the retained IMGUI scenery/HUD; these are live renders of separate objects, never reference/background images. Food state sprites, sandwiches, drinks, ice, smoke/heat and empty furniture remain separately reusable. Shared furniture texture is permitted; meats/products must never be baked into it. Render-only components cannot produce stock or money.

Authored prep row Normal→Premium→Fernet→Coca→Beer; tables remain equal196×98-native dimensions at logical y≈503.03, and equal91.08×117.6-native barrels (10% wider than82.8-wide; 26.5% over the original72-wide; original height retained) align their lower bounds with the tables. The prep block derives its vertical position from the tiled-field lower frame (reference y=665), leaving4 logical points above the grills and placing every active grill5 logical points above that frame (grill bounds end at y=660). The same vertical-responsive transform is applied to both the kitchen and frame, preserving this margin across viewport heights. Table/barrel dimensions and grill aspect ratio remain intact; station pickup anchors follow their layout. Active stations reflow across available width from the level catalog (never from momentary stock). In the full five-station row, equal authored gutters become approximately−1.69 units, a bounded overlap of station bounds; the runtime Game View has not yet been recaptured for this follow-up. A single grill remains centered; paired grills remain equally sized and aligned. Catalog layout also owns the shortest-safe path graph for worker travel: direct/diagonal segments when clear and exposed obstacle corners around actual station bases expanded by the36×12 feet collider. Each outbound, return and next-unit path is recalculated from the worker's real position; pickup and handoff semantics remain unchanged. Tall safe-area adaptation retains sprite aspect, worker proportions and HUD controls.

## Implementation sequence

1. Define item lifecycle/stock within existing domain, keeping one simulation and specialty authority; do not create parallel legacy cooking.
2. Generate original clean independent transparent resources (empty props, four meat sources projected as three visual states: raw and cooking-progress share Raw art, Cooked uses Cooked art, and Passed reuses warm Cooked art with a subtle tint; the obsolete black-char frame is never displayed, four sandwiches, separate drinks/ice/heat/smoke); never cut contaminated composites. Keep new resources separate from existing art/metas.
3. Add reusable visual prefabs/configuration and placement slots, integrating live scene instances with the existing HUD/scenery and catalog authority. Inventory objects project actual item IDs/states, not cosmetic counts.
4. Apply responsive kitchen geometry and paired safe worker routes; preserve counter, UI and club-specific content.
5. Focused domain/item, specialty, inventory/pickup/cancel, prefab/resource/route and mouse/touch regression checks; real portrait/tall Game View comparison before sign-off. Do not repeat full suites without a relevant milestone reason.
6. Update acceptance/results, review scoped diff, commit/push only the completed implementation.

## Acceptance status

- [x] Architecture/domain/art/prefab baseline inspected; MCP Main hierarchy confirmed.
- [x] Original supplied reference retained as reference-only asset.
- [x] Production/inventory/burning and responsive-size decisions explicitly approved.
- [x] Independent resources/prefabs and real item-state integration implemented.
- [x] Relevant tests and actual Game Views validated.
- [x] Scoped initial implementation reviewed and delivered to Git (see the dated refinement notes below).
- [x] Catalog-driven visibility/reflow, three visual states, and ambient grill effects validated against the active Unity editor and the level1/3/4/5 Game Views.
- [x] Fernet tabletop stock display: the live prefab has its SpriteRenderer on the positioned station root, so the old placement double-counted the station anchor and pushed bottles offscreen. The renderer now measures sprite corners in station-local space and loads the existing 2.5 L Coca display art. Focused non-origin/scaled EditMode regression passed; live Level 6 Game View at 608×1080 confirms Coca/Branca stock on the tabletop with prepared cups to the right (`Logs/Acceptance/FernetTabletop-20261013/level6-after-display-fix.png`). Coca display scale is 5% below the former target (`.76` vs `.8`); bottles sit slightly into the tabletop plane with a subtle base contact shadow. The tabletop stock is now four Coca and four Branca bottles arranged in two compact, staggered rows; focused placement/scale/shadow/row regression passed 1/1, and Level 6 Game View confirms contact placement and spacing (`Logs/Acceptance/FernetTabletop-20261013/level6-after-two-stock-rows.png`, 2160×3840 capture).
- [x] Footer pennants adapt to consume the unused footer band without moving controls. Focused footer tests passed (2026-10-12).
- [x] Premium Bondiola/Vacío table/carried servings render 10% larger and barrel Coca/beer items 8% larger; focused size/projection checks passed. Two staggered low-alpha smoke puffs per active grill rise/fade on a 3.125 s cycle; EditMode profile tests and a live Level 6 portrait Game View capture at 540×1200 confirm the visible subtle effect (capture: `Logs/Acceptance/VisualSmoke-20261013/level6-gameview-540x1200.png`). Device-specific visual review remains untested.

The original modular-kitchen implementation and its historical Unity validation are documented below. The catalog-driven correction is a separate delta; the prior screenshots do not validate its dynamic station rows, new barrel scale, three-stage projection, or ambient effects.

## Generated resource set / prompt record

Built-in `image_gen` only (not a paid external API/CLI). Original PNGs saved under `Assets/Resources/ModularKitchen/Art/`; source reference guides style, never used as a rendered backdrop. Each prompt requested a clean isolated detailed2D cartoon object, bold dark contours, elevated frontal perspective, genuine alpha, no scenery/labels/ground or unrelated objects. Food/furniture must never be precomposited.

| PNG | Subject-specific prompt constraints |
|---|---|
| empty-grill | Empty black iron charcoal grill,3:1 whole-object proportions, red coals below grate, short legs/side handles; no meat or smoke. |
| empty-table | Empty wood/iron preparation table2:1, empty tabletop/lower shelf; no food, cups or vegetables. |
| blue-barrel | Open cobalt-blue plastic cylinder1:1.9, molded rings, empty opening; no keg/tap/branding/drinks/ice. |
| meat-chori | Four equal horizontal frame cells: shallow horizontal curved elongated sausage (final variant uses simplified cartoon shading) raw pink→part brown→grill-marked cooked redbrown→black burned; same outline/angle, transparent margin in each cell. |
| meat-paty | Four equal cells: relatively flat circular medallion raw pink→partial browning→brown grill marks→black burned; no bread. |
| meat-bondiola | Four equal cells: thick high compact approximately cylindrical irregular pork neck roast, not steak/shredded meat; raw marbling→partial browning→golden grill marks→black char. |
| meat-vacio | Four equal cells: long flat, asymmetric flank cut with one fuller end gradually tapering toward a narrower tip; not patty/roast; raw red/fat→partial browning→grill marks→black char. |
| sandwich-chori | One choripán, curved grilled chorizo visibly inside elongated golden bread, chimichurri; no plate/table. |
| sandwich-paty | One rounded bread burger, visible circular flat beef patty, thin tomato/lettuce; no sausage/pork. |
| sandwich-bondiola | Bread roll with thick compact roast-pork slices, juicy marbled interior; no shredded pulled pork or beef patty. |
| sandwich-vacio | Bread roll with thin long chargrilled flank-steak strips, clearly different from pork/sausage/burger. |
| coca | One small600ml PET dark-cola bottle, red cap/wrapper, recognizable white cola-brand mark; no barrel/ice. |
| beer | One yellow/blue330ml closed aluminum can, silver pull-tab top, no text/tap/barrel. |
| fernet | One large1L transparent cylindrical dark fernet-cola glass with many chunky visible ice cubes; absolutely no straw/stirrer/garnish. |
| ice | One shallow elliptical paleblue translucent chunky-ice cluster, no container/drinks. |
| smoke | One soft warmgray vertical airy smoke/steam curl, partial alpha, no food/flame. |
| heat | Three thin translucent amber/orange gently waving heat-shimmer lines, no flames/background. |

Four-cell source strips are texture-sharing only: each imported cell has its own reusable Sprite resource. The fourth black-char cell remains a source-art asset but is no longer selected by the renderer; passed food uses the warm cooked frame with a mild tint. PNG pixels remain unchanged; the import/build step trims only alpha bounding rectangles. Existing art/metas remain intact.

Texture sources use NPOTScale.None to preserve original dimensions, mipmaps and Trilinear minification to prevent sparkle in live mobile-sized views. Sprite.Create assets share the unchanged original texture; the final selected Chori variant corrects the initial diagonal pose while retaining the new resource GUID.

## Verified acceptance —2026-10-07

- Native import:17PNG sources,29persistent Sprite resources,21prefabs; GUID-stable regeneration verified in ignored `Logs/Acceptance/ModularKitchen-20261007/rebuild-verification.json`.
- 2026-10-08 visual refinement: the user-supplied grilled-meat photo is shape-only guidance for Vacío. Its independent four-state source now has a slightly broader left end tapering to a narrower right tip; state order, transparent four-cell layout and 2172×724 texture remain unchanged. Existing `.meta`/texture GUID is preserved; Unity reimport confirmed all Raw/Cooking/Cooked/Burned Sprite assets still load from the same-size texture with NPOTScale.None, mipmaps, Trilinear and alpha. `StreetKitchenPresentationTests` passed5/5 after import. Sandwich, cooking logic, positions and inventory were not changed. Existing APK0.2.17/code19 predates this PNG edit; no APK was rebuilt.
- 2026-10-08 layout-only correction (superseded by the catalog-driven refinement below): top furniture order is Normal, Premium, Fernet, Coca, Beer; equal-size tables and barrels plus two aligned grills were validated in Portrait Main Game View. The former choice to show every locked station shell was later corrected because the Velez gameplay screenshot still showed empty, unavailable stations.
- 2026-10-08 catalog-driven correction: visibility now derives solely from the simulation's level product catalog, never temporary stock. The same immutable `StreetKitchenLayout` instance supplies visible station bounds, food slots, pickup positions, and worker approach routes. Active upper-row stations are packed evenly with no reserved holes; all tables are196×98 native units; all barrels are60×117.6 (20% larger than the prior50×98). A lone active grill is centered and enlarged only within the reserved UI band; paired grills remain equal-sized, aligned and aspect-preserving. Level profiles remain untouched.
- Cooking keeps its existing internal Raw→Cooking→Cooked→Burned timer/discard behavior and balance, but renderer projects only three visual states: Raw and Cooking map to Raw art, Cooked maps to Cooked art, and Passed reuses warm Cooked art with a light warm tint instead of showing the black-char frame. Ambient embers/shimmer plus two slow fading smoke curls are bounded to at most3 pooled sprite objects per active grill and are disabled for inactive grills.
- Focused EditMode job `b0ea015d1bcf4a5398284bc7fe2a835e` passed6/6: level-select/next-level layout refresh, all11 catalog stock and layout profiles, worker route clearance, separated product slots, and three-state projection. Unity6000.6.3f1 compiled the changed scripts without remaining errors/warnings. Portrait GameViews (619×1100) were captured and visually checked for levels1,3,4,5 under ignored `Logs/Acceptance/StationRefinement/`; the Level3 view confirms its active Coca barrel appears after level switching, Level1 hides all locked stations, Level4 reflows Premium/Beer, and Level5 shows all catalog stations. During live Level3 inspection the active grill owned one reused heat effect and two fading smoke sprites; inactive grill effects were disabled. No worker, catalog, economy, save, or timing rules were changed.
- Affected EditMode149/149 (`c40409e402974e1a9038809056c6916e`), final kitchen-only12/12 (`94a96f1af330480290b425aad7b774ee`). Full project suites not repeated; historical art/geometry tests are compatibility evidence, not new live geometry proof.
- Mouse/touch/start/specialty purchase PlayMode4/4 (`f0e7761596594ca8bf942a8df691fb21`); native tall-anchor/individual carried-object removal regression1/1 (`c3dbf7bcbb8046f9a8c2437eb8e7fa91`). Zero current Unity console errors/warnings.
- Actual Main GameViews inspected: `independiente-final.png`1080×1920, corrected `independiente-tall-corrected.png`1220×2712, `ferro-final.png` and `floresta-final.png`, under ignored `Logs/Acceptance/ModularKitchen-20261007`. Paired grills, empty independent furniture, product-separated slots,4compact cylindrical Bondiolas/3Vacíos in full catalog, equivalent-size tables, blue plastic barrels with separate containers/ice and large straw-free Fernet glasses are native independent objects. Counter/crowd/club art and existing controls stay intact. Tall projection keeps internal food offsets anchored to the correct grill/table without stretching sprites or floating meats.
-180native food objects matched180real IDs with0state mismatches and2carried objects.2physical handoffs consumed those2IDs/removed2objects; delivery4→6 and earnedcoins20→30. Stock projections were48normal/36premium/45Fernet, with current available products only. Floresta verified no locked Coca/Premium/Fernet stock; catalogs checked across all11levels.
-2000-step178-unit isolated editor probe587→117ms after full-table fast path; not Android/device/FPS evidence.
- Initial MCP jobs that timed out before execution are excluded. New `.cs` must be imported with all/assets refresh; test discovery was explicitly confirmed before the final new-group run.
- Exact prior PlayerPrefs, GameView19, timeScale1 and background=false restored; Play stopped, Main clean. No package/service installed, no gameplay prices/goals/speed tables/save policy changed. No new APK built/device tested; prior0.2.16 APK does not contain this feature.


### 2026-10-08 floor-safe station correction

The live Vélez Game View exposed that grill artwork extended into the pale lower UI field even though the previous geometry test only checked a looser upgrade-band bound. Raise the complete preparation row to logical y=466, derive both grill rows from the active row's lower bound plus 10 units, and cap grill bounds at y=660 (with the observed field transition near y=665). Keep standard grill dimensions, table/barrel dimensions, catalog, worker roles, economy and cooking unchanged; move table/barrel pickup targets upward by the same 54 units so routes still approach their stations. Focused EditMode layout/route checks passed3/3 and cover all11 catalog profiles; actual portrait Game Views for Vélez(Level3) and Independiente(Level5) were visually checked. Both grills now remain fully on the tiled playfield above the pale lower field, and the paired grills share one baseline.

### 2026-10-08 — Shortest safe employee trips

Replaced fixed center-lane/forward-reverse waypoints with the shortest path around active station solids using the full36×12 worker-feet sweep. Clear direct diagonals need no detour; blocked routes use exposed obstacle corners. Each return and later assigned unit is recalculated from the actual current position, with pickup/handoff, reservations, stock, economy and employee roles unchanged. Focused Unity EditMode coverage includes all840 outbound/return journeys across11 catalog profiles and seven handoff columns; after the final kitchen reflow, distance reduction averaged33.7% per journey (maximum82.5%, aggregate35.1%) against the previous route. Exact Main Game View movement was reviewed at levels1 and11; saved captures are in `Logs/Acceptance/ShortestRoutes-20261008/`.

### 2026-10-08 — Four-point table/grill spacing

Reduced the common vertical clearance from10 to4 logical units, measured from the bottom of the complete active prep row to the top of the grills. Table/barrel dimensions and lower-bound alignment remain unchanged. Three focused Unity EditMode checks passed after the change, covering the exact requested clearance, all11 catalog layouts, safe grill bounds and catalog-route journeys.

### 2026-10-08 — Two-point lower grill margin (superseded)

The previous iteration used a2-logical-unit margin below the grills, with the4-unit upper clearance. It moved the prep/grill block down40 logical units to y=506. This was superseded by the responsive five-point lower-frame requirement below.

### 2026-10-08 — Five-point responsive lower grill margin

Set the grill bottom exactly5 logical points above the tiled-field lower frame while keeping the upper prep-row clearance at4 points. Derive TableY from that frame, the five-point lower margin, standard grill height, table height, and upper gap (reference TableY≈503.03, grill bottom y=660). The existing responsive vertical transform applies to both layout and frame. Station dimensions/aspect and pickup tracking remain unchanged. Focused Unity EditMode checks passed17/17, covering all11 catalog profiles,840 journeys, level refresh and seven route cases.

### 2026-10-08 — Wider barrels and visible tossed stock

Both blue barrels now use72×117.6 authored bounds: diameter/width is20% wider than the former60 while height is unchanged. The renderer horizontally widens only the barrel artwork to match those bounds. Each existing12-item inventory is projected in two staggered rows across the open top with stable varied rotations; on-barrel drinks use compact8×15 visual bounds so individual cans/bottles read as a loose pile. Carrying resets rotation. No capacity, stock, recipe, worker, route target or service behavior changes. Focused EditMode geometry/scatter checks passed2/2; all-catalog route-clearance passed1/1; renderer PlayMode projection passed1/1. A real1080×1920 Level5 Main Game View confirmed the wider equal barrels and the tossed bottle/can piles; capture: `Logs/Acceptance/StationRefinement/barrel-tossed-fullcatalog-gameview.png` (ignored build/QA output).

### 2026-10-08 — Perspective-correct, densely packed grill meat

The player Game View showed Choris at the rear corners outside the grill's trapezoidal grate even though they were inside the prop's rectangular bounds. Meat positions now use the actual narrowing rear/widening front surface, and visual sizes adapt to the available grill area; a Chori-only grill packs its existing18 real items more tightly. Product catalogs, capacities, cooking, and delivery logic are unchanged. Focused EditMode checks passed2/2, including every meat slot against the grate bounds across all11 levels and family separation. Game Views for all11 levels were captured and reviewed under ignored `Logs/Acceptance/GrillFix-20261008/all-level-*.png`; PlayerPrefs were restored and Play Mode stopped after capture.

### 2026-10-08 — Larger sandwich presentation

The user noted that sandwiches looked disproportionately small. Increase their renderer-only target size by 1.5× on the prep tables and 1.25× while carried; the existing aspect-preserving sizing keeps each source illustration undistorted. Drinks, station bounds, stock, capacity, recipes, routes and economy are unchanged. Focused `StreetKitchenRendererTests` PlayMode class passed 12/12, including all four sandwich families in table/carried states and unchanged drink targets. Main-only Android0.2.20/code22 APK built successfully and package/integrity/signature verified; Motorola installation is pending device reconnection (see `android-prototype.md`).

### 2026-10-09 — Explicit beverage barrel pickup anchors

Coca and beer trips now target an explicit free point on the front edge of their own barrel (center.x, yMin−12) before returning to the customer. Existing obstacle-expanded shortest-path routing remains responsible for avoiding fixed station bases. There is no barrel lock, so Cocacoleros can retrieve Coca and beer simultaneously. Focused EditMode route matrix passed for catalog levels0–4; a concurrent Unity simulation verified both workers reached their respective anchors and completed real deliveries without duplicate revenue.


### 2026-10-10 — Additional 15% barrel diameter

Both blue barrels grow from72 to82.8 authored width (15% over their current diameter); height remains117.6. Catalog packing, sprite fit and route footprints share the expanded width, preserving positive gutters when all five prep stations are active. The twelve stock slots, drink rendering, pickup, production and service behavior are unchanged. Focused EditMode checks pass3/3 (all11 catalog layouts, positive spacing, scattered drink slots, route clearance); PlayMode renderer projection passes1/1 and asserts that both barrel sprites match their widened footprints without changing height. Unity 6000.6.3f1 ran against an isolated copy of the candidate sources because the authoritative editor checkout was already open. No fresh Game View screenshot or device review was performed.

## Barrel beverage display scale — 2026-10-10

Coca bottles and beer cans resting inside their barrels render at 1.35× their previous 8×15 authored fit target. Scaling is uniform and aspect-preserving; carried drinks and inventory/serving behavior retain their existing sizes and logic. The focused full-catalog PlayMode renderer test asserts the same 35% display-scale increase for both barrel types.

## Additional barrel width — 2026-10-12

Increased the existing 82.8-unit barrel width by a further 10%, to91.08 authored units; the original117.6-unit height is unchanged. In the maximum five-station row, the equal authored gutter is−1.69 units, so the station bounds overlap slightly; the focused catalog geometry test caps that overlap at2 units. Stock, pickup behavior, beverage sizes and service logic are unchanged. Unity Game View verification is pending.

## Fernet-table bottle shelf proportions — 2026-10-12

Enlarged only the decorative bottles under the Fernet table: the 1 L Fernet bottle now occupies 25% of table height and the 2.5 L Coca bottle 33.3%, with uniform sprite scaling and redistributed centers to keep all six separated. The 600 ml barrel stock, serving visuals, gameplay and furniture bounds are unchanged. The focused PlayMode renderer test checks these prior target heights, aspect ratios and gaps; after restoring this sizing, it passed 1/1 in an isolated Unity 6000.6.3f1 copy. A live Game View screenshot was not captured.

## 2026-10-12 — Restore the prior presentation sizes

The shared proportional-scale correction from commit `a7937b2` was reverted after it made food and drink sprites too small. Restore the renderer's prior serving/grill dimensions and the Fernet-table bottle targets (Fernet 25%, Coca 33.3% of table height). The Fernet remains an 800 cc disposable plastic cup; this rollback changes only visual size presentation, not products or gameplay.


## Fernet table shelf bottles tripled — 2026-10-12

At the user's request, the six decorative Fernet/Coca bottles below the Fernet table are exactly 3× their former proportional target heights: Fernet 75% and Coca 99.9% of table height. Sprites retain aspect ratio; positions are recomputed from rendered widths with even gaps to keep the row centered and prevent overlap. This is presentation-only; bottle art, table, cup, inventory, delivery and worker behavior do not change. Focused renderer PlayMode validation passed 1/1 in isolated Unity 6000.6.3f1.


## Two-row sandwich presentation — 2026-10-18

All four sandwich products render in two stable table-depth rows. Shared tables retain distinct left/right product zones; each product zone uses two compact pile anchors per row, with real units fanning by at most 6 logical units as stock accumulates. Table dimensions, inventory identities, and configured storage capacities (24 normal / 18 premium per product when both companions are available; full table capacity when alone) remain unchanged. On-table sandwich art is 80% of its former display target (still 1.2× the normal base size and 1.32× for premium) to reveal a rear row within the existing tabletop depth. `TableSlotPosition` drives both item rendering and `TableSlotPickupPosition`; front-row sorting is above the rear row, so reserved Mozo pickups remain aligned with the exact real slot.

- [x] Two row anchors and the corresponding back-to-front sorting are covered for Chori, Paty, Bondiola, and Vacío across shared and single-product table layouts.
- [ ] Full real-Unity Game View screenshots for low/high/max stock and front/rear Mozo pickups, at compact and tall portrait resolutions, remain required for visual sign-off.

## 2026-10-19 — Parrillero visual contact with the grill

The follow-up Racing screenshot still made the cooks look distant. The earlier72% anchor pushed the cooks deep into the fitted grill and hid too much of each body. The visible contact point is the rear/cook-side rim, not the forward edge: the first15% render-only inset still read as a gap in the full Racing screenshot, so the contact anchor is now30% into the fitted grill depth. This moves only the visual pose far enough to overlap the cook-side rim clearly; hands remain on the grate and the upper body stays readable. This scales with responsive grill geometry; `GrillWorkPosition`, `GrillSlotWorkPosition`, obstacle clearance, routes, food slots, and cooking behavior stay unchanged. Projection tests measure the visible foot position rather than the sprite pivot. A full Racing Game View capture remains pending; native Unity kitchen-camera captures have confirmed the projection.

### 2026-10-19 — Shared cook contact position

The fitted-grill render offset is now centralized in `StreetWorkerProjection.PresentationPosition` and therefore applies to both native SpriteRenderer workers and `StreetView`'s IMGUI fallback. The fallback previously bypassed the native-only adjustment and stayed at the route-safe logical post, reproducing the apparent gap. No simulation, route, food-slot, or production coordinates are changed. Focused Unity validation passed 4/4 EditMode cases and 1/1 PlayMode case; native kitchen-camera captures were inspected for one-to-four cooks at 1080×1920 and 1080×2400. Full Game View Racing review remains open. Follow-up after the user reported the 15% change still looked unchanged: the 30% anchor passed `CookVisualInsetUsesFittedGrillEdgeWithoutMovingSafeLogicalPosts` (EditMode 2/2) and `CookingPosesReplaceOriginalBodyAndStayAnchoredForOneToFourCooksAtTwoPortraitScales` (PlayMode 1/1); actual Unity camera captures at 1080×1920 and 1080×2400 show a visibly closer contact point. These are isolated kitchen-camera captures; a full Main/Racing Game View capture remains unverified.
