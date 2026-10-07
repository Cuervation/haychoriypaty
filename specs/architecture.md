# Technical architecture

## Current runtime contract

See [features/street-automation.md](features/street-automation.md) for behavior and acceptance. These are intended boundaries; validation/status records determine what has actually been imported and run.

- `StreetGame` (MonoBehaviour): lifecycle, scene inspector `StreetBalance`, management commands and dedicated versioned PlayerPrefs JSON persistence for progression/purchases and legacy-compatible fixed-price fields. Coins are attempt-local; discard saved legacy balances on load and start new/retried levels at zero.
- `StreetSimulation` (plain C#): deterministic customer/worker states and movement, station/pickup/handoff progression, reservations, units, income, demand and outcomes. Bounded substeps/capped frame catch-up prevent unbounded loops. Limits default to 21 customers/five workers; speed/hiring use explicit capped price tables. Customer slots are seven FIFO columns: departure compacts existing people, admission appends at a column tail, and worker service requires a settled front-row customer.
- `StreetView`: renders actual positions/state and original replaceable art, including coin/delivery feedback. Art and animation never mint income or independently complete an order. Use one aspect-responsive540-wide portrait safe-area transform for actionable drawing/hit-testing; full-physical-screen scenery and read-only edge HUD use a separate decorative transform so safe-area insets never expose camera clear color. Center-cutout status text avoids the camera without interrupting the full-width decorative HUD or moving hit targets.
- ClubVisualTheme resolves the current StreetSimulation.LevelNames entry to one club palette; StreetView uses it for the same cached bottom-edge pennant strip on non-Floresta levels. Keep Floresta's existing baked black/white row untouched. Register a palette by the exact runtime club name when adding a level; never duplicate color selection in scenes.
- Main must disable legacy `GameController`/`PrototypeView` when integrating StreetGame/StreetView; retain legacy code, scene/script GUIDs, assets and tests as the historical prototype, not a second concurrent simulation.

Keep this in-process and single-scene. The explicitly requested small local save now has a concrete need; it does not justify services, dependency injection, Addressables or a generalized pipeline. Save progression/purchases and normalized legacy price fields only; full in-flight round resume is not asserted.

## Retained legacy prototype

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

## Specialist food and beverage workers across later levels

A `StreetWorker` has one role, and each `StreetOrderLine` has at most one owning worker ID plus its current in-flight unit reservation. From Nueva Chicago onward, the Parrillero owns only available food lines (IDs 0–3), while the red-apron Cocacolero owns only available drinks (IDs 4–6). The level catalog is data-driven with stable product IDs: Chicago 0/4; Vélez 0/1/4; Ferro 0/1/2/4/6; Independiente 0/1/2/3/4/6/5. One worker retains one customer until its entire role-specific share is complete in request order; the other role may serve the same mixed ticket concurrently, while same-role workers cannot share a customer. New assignment requires an oldest eligible, settled FIFO front-row `Waiting` customer. Timeout, departure and cancellation release each line owner and active unit reservation. `StreetView` maps each customer ID to consistent complete front/walk/riot team apparel; the same red-apron Cocacolero animation and drink-station routing serve both new clubs.

## Independent upgrade commands and per-level pricing
StreetSimulation exposes role counts, role-local price/cap/eligibility and TryHire(role). Legacy parameterless TryHire explicitly buys Parrillero. StreetGame persists optional role counts with their level alongside the v2 legacy total, restoring only matching-level composition; existing v1/v2 saves remain supported. StreetView draws/hit-tests one shared configured two/three-card layout, never changes role label based on NextHireRole. StartRound retains purchased roles.

Upgrade prices resolve through StreetBalance.upgradeCostProfiles and levelUpgradeCostProfileIds; a missing/invalid map falls back to profile 0/default. Main currently maps each of the five runtime level indices to shared TEST_ECONOMY_PROFILE data: hire costs [15,30,60,100] and speed costs [5,10,15,20,30,45,65,90,125]. Both roles read the common hire array, with the existing free starting Parrillero and Cocacolero role-local index preserved. Legacy hireCosts, speedUpgradeCosts, florestaHireCosts and florestaSpeedUpgradeCosts remain hidden serialized compatibility fields and are not consulted by runtime pricing. Add future club-specific curves by adding a profile and changing only the level-to-profile data map; do not add per-level price branches. Exhausted costs are zero/MAX, and eligibility rejects zero-cost purchases so they cannot become free upgrades.

## Ordinary button source of truth — 2026-10-06

The active Main scene uses StreetView IMGUI, not uGUI Button components or prefabs. `DrawStandardButton(bounds, action, caption, enabled)` consolidates the existing cover renderer and reuses the single `CreateMenuButton()` runtime texture and bundled Luckiest Guy font. Original cover mapping, glossy blue palette, white outlined lettering and pressed `.97`/blue tint remain; disabled buttons use a muted blue palette. Other aspect ratios resize only the straight center through horizontal three-slicing, with subpixel internal-edge overlap to prevent portrait raster gaps. Caption/outline styles reuse the same font with isolated normal/hover/active/focused colors, preventing inherited skin-hover whitening and shared-style leakage. Hit bounds and NativeAction/DispatchAction routes are unchanged. No new assets, package or parallel UI system.

All ordinary runtime buttons use this helper. Explicit exceptions are the user-selected wood/iron upgrade cards (integrated live values/prices) and mural level tiles (content previews). Historical inactive PrototypeView UI is outside this change.

## Shared workstation sizing and circulation — 2026-10-06

StreetWorkstationLayout is the single canvas-space geometry configuration used by StreetView and StreetSimulation. The live Main has no workstation GameObjects, SpriteRenderers, pivots, colliders or prefabs; equivalent visual/occupancy bounds are the draw rectangles, texture source proportions and worker feet. Reference sizes were read directly from the running Floresta view: grill282×94 and table196×98. Size does not depend on level/product/worker count. Fit existing grill art proportionally inside that frame; reuse Floresta's actual196×98 table artwork in every level. No new sprite assets.

Placement is separate from sizing: keep the table left, stagger the grill lower/right and secondary stations in an upper line. Coke and beer leave a full worker-width corridor; Fernet uses an upper-left work point. All food products are picked up beside the finished-product table, not the hot grate. Existing station/approach APIs forward to the shared config; later-level entry stages reuse the unobstructed upper lane, retaining assignment/reservation/pickup/handoff states. Minimum station spacing and full90×98 worker/36×12 foot bounds are explicit occupancy checks. Future levels automatically inherit furniture dimensions; added secondary layouts must validate bounds, interaction clearance and all seven customer-to-station routes.
