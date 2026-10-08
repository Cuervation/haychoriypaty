# Modular kitchen — requested 2026-10-07, implemented and validated

## Scope / visual authority

The user-supplied [layout reference](../../Assets/Art/Reference/modular-kitchen-layout-reference-20261007.jpg) is composition/style guidance only, never gameplay background. Requested: two separate empty grills with independently placed Chori/Paty vs Bondiola/Vacío; individual raw/cooking/cooked/burned meat visuals, compact thick cylindrical Bondiola; independent bread sandwiches; three equivalent-size tables for normal/premium/Fernet; separate blue plastic beer/Coca barrels and containers/ice; Fernet1L with ice and no straw. Bottom grills side by side, preparation row Beer→Coca→Normal table→Premium table→Fernet table, clear circulation before counter; preserve UI, club scenery/fans/riots and catalog progression.

## Verified starting baseline / resolved prerequisites

Active checkout `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`, clean starting commit `ff65e2a`; Unity6000.6.3f1 MCP connected and Main hierarchy inspected. Main is IMGUI StreetGame/StreetView; no current station/food SpriteRenderer prefab instances. Assets/Prefabs contains only .gitkeep; furniture art includes baked food. Legacy GameController/PrototypeView are disabled, not concurrent gameplay.

1. **Real cooking/stock semantics are absent.** StreetSimulation.cs:689–715 uses pickupSeconds/WorkRate then carries a reserved customer-order unit. Lines:821–837 reserve demand, not physical food; handoff:872–896 consumes a unit of demand and earns revenue. There are no individual food entities, raw/cooked/burned states, table inventory/capacity or refill policy. Legacy GameController.cs:156–211 cooks whole Chori/Coca orders and awards whole-order income; enabling it would violate the single-simulation and existing handoff contract. Do not equate order Reserved with table stock, animate invented cooking clocks, or borrow legacy8-second/capacity values without approval.
2. **Requested one-row geometry conflicts with current fixed portrait sizes.** World canvas width540; each grill282×94, table196×98. Two grills plus minimum6 spacing require570. Three equal-size tables alone require588, before two barrels/gaps. Therefore exact left-to-right rows cannot fit while retaining current visual dimensions and no overlap. Requires approval for a separate responsive scene viewport/uniform world scale (HUD unchanged), or a portrait wrap/layout exception. No camera/orientation/scale changes made yet.

## Approved design (user: “Avanza”, 2026-10-07)

Both prerequisites above are explicitly authorized. Extend **only StreetSimulation**, not the disabled legacy motor. Kitchen geometry uses a uniformly scaled760-wide authored composition inside the existing540-wide canvas; HUD, camera orientation, worker proportions, catalog and club scenery remain unchanged. This supersedes the former fixed furniture visual-size rule for this feature.

### Real item lifecycle / defaults

`StreetKitchenProduction` is owned/stepped by the existing simulation. Stable food IDs move Grill→Table→Carried→consumed only at validated physical handoff. Order reservations are still demand, not stock. Meat states are Raw→Cooking→Cooked; cooked grill meat transfers to its specialty table when a slot is free, otherwise burns after20s, then is discarded after1s. Burned units cannot be served. Prepared table sandwiches never burn. Raw items cannot be taken. All production follows actual availability and the hired responsible specialty; WorkRate scales preparation. Existing pickup/handoff delays, economics and progression remain intact.

Configurable defaults: cook4s; burn20s; discard1s; drinks preparation0.28s; normal table48total, Premium36total, Fernet45total, each barrel12. Fresh attempts begin with real prepared mise-en-place at these capacities, divided between currently available products; this preserves delivery throughput without inventing revenue. No prior storage capacity existed to override. Normal grill18slots (12Chori/6Paty), Premium7slots (4Bondiola/3Vacío); single-product catalogs use available meat only. Empty pickup waits; full table holds cooked grill items until room/burning. Cancellation returns an edible carried unit if room, otherwise discards without income. Reset/retry/select rebuild IDs/stock; no in-flight save added.

### Independent native visual projection

Actual reusable SpriteRenderer prefabs and live per-ID GameObjects project this domain. Transparent offscreen kitchen/carry cameras provide render textures composed with the retained IMGUI scenery/HUD; these are live renders of separate objects, never reference/background images. Food state sprites, sandwiches, drinks, ice, smoke/heat and empty furniture remain separately reusable. Shared furniture texture is permitted; meats/products must never be baked into it. Render-only components cannot produce stock or money.

Authored prep row Beer→Coca→Normal→Premium→Fernet at logicaly520, table196×98scaleduniformly; two352×117.33grills at y620. Locked stations are hidden without unlocking products. Counter-to-prep circulation136logicalpx, grill gap~30px. Routes approach from the open upper lane, respecting physical base/foot footprints (distinct from projected tabletop silhouette). Same layout across all11 catalogs; tall safe-area adaptation retains sprite aspect, worker proportions and HUD controls.

## Implementation sequence

1. Define item lifecycle/stock within existing domain, keeping one simulation and specialty authority; do not create parallel legacy cooking.
2. Generate original clean independent transparent resources (empty props, four meats×four states, four sandwiches, separate drinks/ice/heat/smoke); never cut contaminated composites. Keep new resources separate from existing art/metas.
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
- [x] Scoped implementation reviewed and ready for Git delivery; commit/remote receipt is verified and reported at delivery.

Implementation, import and live Unity validation are complete; actual evidence is recorded below. The starting audit is retained as provenance, not substituted for visual QA.

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
| meat-vacio | Four equal cells: long flat irregular beef flank, not patty/roast; raw red/fat→partial browning→grill marks→black char. |
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

Four-state strips are texture-sharing only: each state has its own reusable Sprite resource. PNG pixels remain unchanged; the import/build step trims only alpha bounding rectangles. Existing art/metas remain intact.

Texture sources use NPOTScale.None to preserve original dimensions, mipmaps and Trilinear minification to prevent sparkle in live mobile-sized views. Sprite.Create assets share the unchanged original texture; the final selected Chori variant corrects the initial diagonal pose while retaining the new resource GUID.

## Verified acceptance —2026-10-07

- Native import:17PNG sources,29persistent Sprite resources,21prefabs; GUID-stable regeneration verified in ignored `Logs/Acceptance/ModularKitchen-20261007/rebuild-verification.json`.
- Affected EditMode149/149 (`c40409e402974e1a9038809056c6916e`), final kitchen-only12/12 (`94a96f1af330480290b425aad7b774ee`). Full project suites not repeated; historical art/geometry tests are compatibility evidence, not new live geometry proof.
- Mouse/touch/start/specialty purchase PlayMode4/4 (`f0e7761596594ca8bf942a8df691fb21`); native tall-anchor/individual carried-object removal regression1/1 (`c3dbf7bcbb8046f9a8c2437eb8e7fa91`). Zero current Unity console errors/warnings.
- Actual Main GameViews inspected: `independiente-final.png`1080×1920, corrected `independiente-tall-corrected.png`1220×2712, `ferro-final.png` and `floresta-final.png`, under ignored `Logs/Acceptance/ModularKitchen-20261007`. Paired grills, empty independent furniture, product-separated slots,4compact cylindrical Bondiolas/3Vacíos in full catalog, equivalent-size tables, blue plastic barrels with separate containers/ice and large straw-free Fernet glasses are native independent objects. Counter/crowd/club art and existing controls stay intact. Tall projection keeps internal food offsets anchored to the correct grill/table without stretching sprites or floating meats.
-180native food objects matched180real IDs with0state mismatches and2carried objects.2physical handoffs consumed those2IDs/removed2objects; delivery4→6 and earnedcoins20→30. Stock projections were48normal/36premium/45Fernet, with current available products only. Floresta verified no locked Coca/Premium/Fernet stock; catalogs checked across all11levels.
-2000-step178-unit isolated editor probe587→117ms after full-table fast path; not Android/device/FPS evidence.
- Initial MCP jobs that timed out before execution are excluded. New `.cs` must be imported with all/assets refresh; test discovery was explicitly confirmed before the final new-group run.
- Exact prior PlayerPrefs, GameView19, timeScale1 and background=false restored; Play stopped, Main clean. No package/service installed, no gameplay prices/goals/speed tables/save policy changed. No new APK built/device tested; prior0.2.16 APK does not contain this feature.
