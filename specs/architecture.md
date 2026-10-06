# Technical architecture

## Current runtime contract

See [features/street-automation.md](features/street-automation.md) for behavior and acceptance. These are intended boundaries; validation/status records determine what has actually been imported and run.

- `StreetGame` (MonoBehaviour): lifecycle, scene inspector `StreetBalance`, management commands and dedicated versioned PlayerPrefs JSON persistence for progression/purchases and legacy-compatible fixed-price fields. Coins are attempt-local; discard saved legacy balances on load and start new/retried levels at zero.
- `StreetSimulation` (plain C#): deterministic customer/worker states and movement, station/pickup/handoff progression, reservations, units, income, demand and outcomes. Bounded substeps/capped frame catch-up prevent unbounded loops. Limits default to 21 customers/five workers; speed/hiring use explicit capped price tables. Customer slots are seven FIFO columns: departure compacts existing people, admission appends at a column tail, and worker service requires a settled front-row customer.
- `StreetView`: renders actual positions/state and original replaceable art, including coin/delivery feedback. Art and animation never mint income or independently complete an order. Use one aspect-responsive540-wide portrait safe-area transform for actionable drawing/hit-testing; full-physical-screen scenery and read-only edge HUD use a separate decorative transform so safe-area insets never expose camera clear color. Center-cutout status text avoids the camera without interrupting the full-width decorative HUD or moving hit targets.
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

## Independent upgrade commands
StreetSimulation exposes role counts, role-local price/cap/eligibility and TryHire(role). Legacy parameterless TryHire explicitly buys Parrillero. StreetGame persists optional role counts with their level alongside the v2 legacy total, restoring only matching-level composition; existing v1/v2 saves remain supported. StreetView draws/hit-tests one shared configured two/three-card layout, never changes role label based on NextHireRole. StartRound retains purchased roles.
