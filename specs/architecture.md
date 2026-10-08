# Technical architecture

## Current runtime contract

`StreetSceneLayout` now fixes the All Boys spatial root for every club: master background projection, counter top/front, crowd clipping/offset and one player-side service height. `StreetKitchenLayout` supplies the approved uniformly fitted modular kitchen and actual worker routes; `StreetWorkstationLayout` remains historical geometry/API compatibility only. `StreetView` recomposes club-specific murals into the root scenery band; `ClubVisualTheme` stores mural resource/crop and subtle scenery-only atmosphere, not geometry. Later backdrops never set counter heights or camera perspective. Every fresh attempt/load/selection/start/retry begins with one normal Parrillero, no Premium/Cocacolero/Fernetero and SpeedLevel0; saved purchases remain compatibility fields, not restored teams.

See [features/street-automation.md](features/street-automation.md) for behavior and acceptance. These are intended boundaries; validation/status records determine what has actually been imported and run.

- `StreetGame` (MonoBehaviour): lifecycle, scene inspector `StreetBalance`, management commands and dedicated versioned PlayerPrefs JSON persistence for progression/purchases and legacy-compatible fixed-price fields. Coins are attempt-local; discard saved legacy balances on load and start new/retried levels at zero.
- `StreetSimulation` (plain C#): deterministic customer/worker states and movement, station/pickup/handoff progression, reservations, units, income, demand and outcomes. Bounded substeps/capped frame catch-up prevent unbounded loops. Limits default to 21 customers; worker caps and paid tiers are specialty-local (one free normal plus four paid hires, four paid hires per other role); speed/hiring use explicit capped price tables. Customer slots are seven FIFO columns: departure compacts existing people, admission appends at a column tail, and worker service requires a settled front-row customer.
- `StreetView`: renders actual positions/state and original replaceable art, including coin/delivery feedback. Art and animation never mint income or independently complete an order. Use one aspect-responsive540-wide portrait safe-area transform for actionable drawing/hit-testing; full-physical-screen scenery and read-only edge HUD use a separate decorative transform so safe-area insets never expose camera clear color. Center-cutout status text avoids the camera without interrupting the full-width decorative HUD or moving hit targets.
- ClubVisualTheme resolves the current StreetSimulation.LevelNames entry to one club palette; StreetView uses it for the same cached bottom-edge pennant strip on non-Floresta levels. Keep Floresta's existing baked black/white row untouched. Register a palette by the exact runtime club name when adding a level; never duplicate color selection in scenes.
- Main must disable legacy `GameController`/`PrototypeView` when integrating StreetGame/StreetView; retain legacy code, scene/script GUIDs, assets and tests as the historical prototype, not a second concurrent simulation.

Keep this in-process and single-scene. The explicitly requested small local save now has a concrete need; it does not justify services, dependency injection, Addressables or a generalized pipeline. Save progression/purchases and normalized legacy price fields only; full in-flight round resume is not asserted.

## Retained legacy prototype

The existing ClubVisualTheme owns each club's ClubFanDefinition: resource names, expected wardrobe count, front/walk/paired-riot layouts, deterministic customerId selection and optional authored Riot pose body-scale/foot-padding metrics. Active normal, walking and defeat drawing share this definition rather than per-level renderer branches. Metrics compensate transparent atlas margins without measuring the raised stick as body height; existing validated atlases keep their original bounds. Official-level completeness is enforced by ClubFanContractTests; generic Riot remains an emergency asset only.

`GameController`, `CustomerOrder`, `GameBalance` and `PrototypeView` implement the earlier timed two-product round. Its v2 empty backdrop/cropped original atlas and view-only movement remain historical replaceable art. Do not interpret its passed tests or Android build as validation of the new moving-worker simulation.

## Unity layout

- `Assets/Scenes`: main playable scene.
- `Assets/Scripts/Core`: orders, balancing data, simulation.
- `Assets/Scripts/UI`: placeholder view and results.
- `Assets/Scripts/Editor`: scene/setup utilities only.
- `Assets/Art/Reference`: user-provided style/layout reference, not game content.
- `Assets/Art/Placeholder`: original provisional background and seller sprites; replace with final art without changing simulation.
- `Assets/Animations`, `Assets/Audio`, `Assets/Prefabs`: prepared extension points, initially empty.
- `Assets/Tests/EditMode`: focused simulation/lifecycle tests in an Editor-only test assembly; existing legacy tests remain intact.
- `Assets/Tests/PlayMode`: pointer-event integration tests for mouse/touch buttons. Test assemblies locate Assembly-CSharp by reflection so existing scene/script GUIDs stay unchanged.

Use Unity 6 as installed (`6000.6.3f1`). Keep the supplied URP template and package versions; this slice adds no package dependencies. The view uses built-in IMGUI rendering. Input-System-only editor/desktop needs the existing Input System pointer bridge; Android GameActivity supplies native IMGUI touch and must not also poll/dispatch that touch. No package or Input Handling setting change is required. Code remains plain, readable C# with no external dependencies.

## Four exclusive catalog-derived specialties

`StreetSpecialties` is the sole product → worker/station authority. Stable IDs remain unchanged:

| IDs / products | Worker | Physical station |
|---|---|---|
| 0 Chori, 1 Paty | Parrillero | Normal grill and normal finished-food table |
| 2 Bondiola, 3 Vacío | Parrillero Premium | One separate Premium grill |
| 4 Coca, 6 Cerveza | Cocacolero | Existing Coca/beer barrels |
| 5 Fernet con Coca 1 L | Fernetero | Existing Fernet table |

Role availability, hire cards and station visibility derive from actual `levelProductIds`, never the level index. Unknown IDs are rejected by the authority; existing catalog parser rejects invalid IDs. Every product must resolve to exactly one role/station. Each worker retains its customer’s complete specialty-specific share in original request order; other specialties may own other lines of that same ticket concurrently. Same-specialty workers cannot double-own a customer. In-flight unit reservations remain one per worker; only actual handoff decrements quantities/mints coins. Timeout/departure/cancellation releases line ownership and reservations.

Existing enum IDs are stable: Parrillero0, Cocacolero1; append Premium2, Fernetero3. Save v3 extends compatibility counters; v1/v2 migrate without resetting unlocks. No in-flight resume or restore of purchased workers is introduced: every attempt has one free normal worker, no other workers, speed ×1.00, zero coins.

## Independent hiring and shared costs

All four roles use the same `StreetUpgradeCostProfile` selected by `levelUpgradeCostProfileIds`, with current hire curve **$15 → $30 → $60 → $100 → MAX**. Paid tiers are independent; only normal subtracts its free initial worker when calculating the tier. Exhausted arrays/caps return zero/MAX and purchases reject zero-cost hires. Optional `roleHireCostProfileIds` overrides refer to existing profile objects for future differentiation; absent/negative overrides inherit the common level profile. No duplicate per-role price arrays.

`StreetView` uses one catalog action list for drawing/hit testing: speed followed by Normal/Premium/Coca/Fernet when available. Two/three cards keep the established row; four/five cards use two responsive rows with the same wood/iron visual family. Hidden specialties reserve no slot. Large-catalog Ready navigation is shown without overlapping non-interactive upgrade cards.

The normal grill retains its282×94 frame and existing artwork/position; Premium is a separate same-size frame. With Premium present, the normal table moves16px left at unchanged196×98 size; beer moves into the upper-right band and the existing Fernet station moves below the table. The expanded routes are catalog-triggered forward/reverse waypoint paths, sampled against full foot rectangles, not diagonal shortcuts through furniture. Normal/Premium pickup and approach points are distinct. No-Premium catalogs retain existing furniture/routes. New workers reuse exact original atlas geometry and animation poses with cached outfit palettes (blue Premium apron, black Fernetero apron); faces, alpha, pose sizes and normal/Coca sprites remain unchanged. Cached runtime textures are destroyed on view disable.

## Ordinary button source of truth — 2026-10-06

The active Main scene uses StreetView IMGUI, not uGUI Button components or prefabs. `DrawStandardButton(bounds, action, caption, enabled)` consolidates the existing cover renderer and reuses the single `CreateMenuButton()` runtime texture and bundled Luckiest Guy font. Original cover mapping, glossy blue palette, white outlined lettering and pressed `.97`/blue tint remain; disabled buttons use a muted blue palette. Other aspect ratios resize only the straight center through horizontal three-slicing, with subpixel internal-edge overlap to prevent portrait raster gaps. Caption/outline styles reuse the same font with isolated normal/hover/active/focused colors, preventing inherited skin-hover whitening and shared-style leakage. Hit bounds and NativeAction/DispatchAction routes are unchanged. No new assets, package or parallel UI system.

All ordinary runtime buttons use this helper. Explicit exceptions are the user-selected wood/iron upgrade cards (integrated live values/prices) and mural level tiles (content previews). Historical inactive PrototypeView UI is outside this change.

## Shared workstation sizing and circulation — 2026-10-06

StreetWorkstationLayout is the single canvas-space geometry configuration used by StreetView and StreetSimulation. The live Main has no workstation GameObjects, SpriteRenderers, pivots, colliders or prefabs; equivalent visual/occupancy bounds are the draw rectangles, texture source proportions and worker feet. Reference sizes were read directly from the running Floresta view: grill282×94 and table196×98. Size does not depend on level/product/worker count. Fit existing grill art proportionally inside that frame; reuse Floresta's actual196×98 table artwork in every level. No new sprite assets.

Placement is separate from sizing: keep the table left, stagger the grill lower/right and secondary stations in an upper line. Coke and beer leave a full worker-width corridor; Fernet uses an upper-left work point. Normal food is picked up beside the finished-product table; Premium food uses its separate Premium pickup. Existing station/approach APIs forward to the shared config; later-level entry stages reuse the unobstructed upper lane, retaining assignment/reservation/pickup/handoff states. Minimum station spacing and full36×12 foot bounds are explicit occupancy checks. Original no-Premium routes additionally retain strict90×98 body clearance; expanded routes permit projected body/prop overlap at different depths, with real Game View QA required. Future levels automatically inherit furniture dimensions; added secondary layouts must validate bounds, interaction clearance and all seven customer-to-station routes.

## Modular kitchen projection (approved2026-10-07)

See [features/modular-kitchen.md](features/modular-kitchen.md) for the actual stock lifecycle/defaults and layout authority. `StreetSimulation` owns/steps `StreetKitchenProduction`; physical carried IDs gate handoff. `StreetKitchenRenderer` projects independent SpriteRenderer prefab instances into transparent live render textures, composed with retained club scenery/HUD by `StreetView`. Renderers never mint stock/revenue. Editor-only `StreetKitchenPrefabBuilder` creates persistent alpha-trimmed Sprite assets with Sprite.Create (no Sprite Editor package installed), preserving .meta GUIDs on rebuild. Source PNGs remain unchanged.
