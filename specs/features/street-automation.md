# Feature: Street automation and progression

## Authority and scope

This newly requested milestone supersedes the delivery boundary of the [historical Floresta prototype](floresta-prototype.md), not its validation record. [Product](../product.md) retains the exact seven-product catalog and outdoor standing-only setting. [Visual reference](../visual-reference.md) distinguishes observed video evidence from prompt requirements. No copied Food Fever assets, new services/packages, manual movement or complex cooking controls.

Playable Floresta first; reuse the same small scene/simulation for Nueva Chicago, Argentinos Juniors, Vélez and Ferro. Defaults below are implementation balance choices, not values inferred from unseen video action; all gameplay values remain inspector-editable.

## Rules and state contract

- Before start, show the selected product sprite, price slider and Start button. Per-product price is editable from $0–$60 by default, initially $5 (configurable; the observed $52 does not prove the video maximum); a lower price increases configurable arrival demand, a higher price reduces it. Demand uses the mean appetite of unlocked product prices; below 0.8 appetite it also reduces automatic crowd capacity and bulk quantity, while weighted product selection favors affordable products. The default $5 retains 21 clients/999-unit bulk orders; expensive prices trade revenue per unit for slower, smaller demand. These curves are balance choices, not inferred video rules. Charge that product's actual chosen price only after a successful handoff.
- Begin with zero coins and one worker. Customers automatically enter, move to reserved standing positions, wait, receive products and exit. Support up to 21 simultaneous customers in seven columns/three rows; arrival rate and capacity are configurable per level.
- Each customer requests one unlocked product and an integer quantity from 1 through 999; simultaneous customers may request different products. Display product sprite, remaining quantity and individual patience. A handoff decreases remaining quantity by exactly one, increments delivered-unit total by one and earns exactly one unit price. There is no counter jump, reward at pickup, or fake timer-only delivery.
- Workers automatically reserve an outstanding unit, move to its station, pick up, carry to the customer's handoff position, deliver, then repeat. A configurable short station/condiment delay can remain automatic, but must not interrupt the circuit with manual cooking. Food always includes bread; condiments remain automatic and are never extra products.
- Hire up to eight independently moving workers by default. Reservations prevent duplicate delivery to the same final unit; clients that leave release reservations, and in-flight workers cancel safely without earning coins or decrementing another order. Delivery refreshes customer patience. Waiting expiry makes the customer leave.
- Simulation position/state is authoritative: revenue and quantity changes are gated by arrival and handoff, not by an unrelated view timer. View animates entering/waiting/receiving/exiting customers and directional worker walking/pickup/carry/handoff; coin amount/effect, upgrade and hire feedback reflect actual events.
- Speed initially costs $5; hire initially costs $15. Subsequent costs escalate through inspector balance; purchases update availability/prices and affect movement/productivity immediately. Reject insufficient-funds purchases without side effects, and dispatch each tap exactly once.
- Win when the configurable delivered-unit goal is reached, even if a 999-unit order remains unfinished. Lose when the level clock expires first. Results show outcome and units delivered; successful completion unlocks the next location. Do not require all orders to reach zero to win.

## Progression defaults

Products unlock in catalog order: chori, paty, bondiola, vacío, Coca cup, Fernet/Coca large cup, beer can. Every unlocked product has its own original sprite, editable price and station.

| Location | Unlocked product count | Unit goal | Round seconds |
|---|---:|---:|---:|
| Floresta / All Boys | 1 | 24 | 180 |
| Nueva Chicago | 3 | 40 | 210 |
| Argentinos Juniors | 5 | 65 | 240 |
| Vélez | 6 | 85 | 270 |
| Ferro | 7 | 110 | 300 |

Demand, duration, goal, limits, speed, station delays, patience and upgrade costs remain configurable. Persist unlocked level, prices, purchased upgrades/workers and coins in a versioned JSON value under a dedicated PlayerPrefs key; reject/reset malformed or incompatible data safely. This is progression/economy persistence, not a claim of full in-flight round resume. Define reset/replay handling explicitly in code and test it so restored purchases are not charged again.

## Minimal integration

`StreetGame` owns lifecycle, inspector `StreetBalance`, management commands and persistence; `StreetSimulation` is pure deterministic runtime data/logic with bounded substeps and capped frame catch-up; `StreetView` renders actual state with original replaceable sprite assets. Main uses the new pair, disabling legacy `GameController`/`PrototypeView` there while retaining their files/tests. Keep one safe-area transform for drawing and pointer hit-testing (540×960 logical portrait canvas); use Input System bridge on editor/desktop and native GameActivity IMGUI events on Android, never both dispatching the same action.

No new framework, service, generalized content pipeline or package is necessary. Fixed customer/worker caps and bounded catch-up make load controllable; actual device profiling remains required before any performance claim.

## Acceptance — evidence scoped below

- [x] Main opens/runs without errors and only the intended new runtime/view owns gameplay/input.
- [x] Original imported art matches reference functional composition; no placeholder geometric actors, copied assets, 3D/isometric/cenital view or seated diners. All seven product sprites/stations are connected to their unlocked levels.
- [x] Price panel changes actual demand and revenue; start works with editor mouse and Android touch, with aligned safe-area hit bounds.
- [x] Dozens of customers enter and group in rows; each shows correct product, remaining quantity and patience with actual enter/wait/receive/exit states.
- [x] A configured 999-unit order decreases by exactly one on each real handoff; cancellations/departures cannot duplicate decrements or earnings.
- [x] One worker visibly performs station→pickup→carry→handoff repeatedly; hired workers coordinate reservations and increase measured throughput.
- [x] Coin effects/amounts occur only for real deliveries. $5 speed and $15 hire initial costs, escalation, affordability and immediate response work; one tap causes one charge.
- [x] Win/lose/results/replay work end-to-end; five increasing levels unlock exactly the catalog counts above, never an eighth product.
- [x] Versioned save/load restores supported progression/economy state and handles corrupt data; no claim of full-round resume without a dedicated test.
- [x] Directional walk/carry/pickup/handoff, customer states and management feedback are actual animations, not static claims.
- [x] Targeted simulation/input tests, Unity Main playthrough, portrait comparisons and Android player verification recorded (16EditMode/9PlayMode; actual native24-unit round).
- [ ] Physical device/GPU profiling before mobile-performance certification; pure21client/8worker simulation measured, not mobile FPS.

## Validation record

Editor:16/16 Street EditMode,9/9 Street PlayMode, Main actual24-unit victory and persistent reload; original atlas/import/layout reviewed. Isolated five-level wins and real wrapper corrupt/versioned save checks passed. See [status](../status.md) for exact jobs and limitations. Native Android slider0/60,Start,unique hire/speed,24-unit victory,next unlock and restart/save verified; final rebuilt APK installed; native product selection and unobstructed HUD confirmed. Physical performance remains pending; checked simulation/progression criteria do not imply five distinct club backgrounds, audio or physical-device certification.
