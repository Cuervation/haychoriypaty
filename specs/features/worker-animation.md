# Worker animation — 2026-10-08

## Approved scope
Improve all four existing roles across catalog levels and arbitrary hired workers. Keep 90×98 worker frame, cartoon identity, route/timing/economy and physical-item lifecycle unchanged. Existing 16 Parrillero poses +8 diagonal poses and16 poses in each Coca atlas are retained. Palette variants remain cached Premium blue/Fernet black.

## Implementation contract
Eight canvas-space directions; four distinct gait samples per direction, reusing contact poses plus articulated passing legs. A four-frame stride spans80 logical units (40% lower cadence than the previous48-unit cycle). Gait advances only with actual traveled distance, not wall time; idle/pickup/handoff do not walk. Persist facing through stops and share presentation between worker and physical CarriedItemId. Reuse empty-handed Parrillero carry/reach poses; add only missing beverage diagonal poses and empty-hand carry quarters where baked bottle art cannot represent other products. Each live worker uses reusable cutout SpriteRenderers in the existing kitchen projection; carried unit, torso and finger layers share depth, foot and grip anchors. No extra camera, Animator controller, package or per-frame texture generation.

## Acceptance
- [x] Four visual gait samples ×eight directions for both families; all four roles use the same controller.
- [x] Station pickup, carrying, handoff and empty return track real states/physical IDs; no duplicate baked product.
- [x] Product contacts its hand and is correctly occluded by fingers/torso, including tall portrait.
- [x] Focused movement/appearance/native projection tests and real Main gameplay captures reviewed.
- [x] Original artwork/metas, player save, and mechanics preserved.
- [x] Beverage pickup reaches a safe barrel anchor, animates empty-handed during the existing dwell, and only carries the real inventory unit after withdrawal; Coca/beer workers operate concurrently.
- [x] Worker gait cadence is40% slower, remains distance-proportional for all four roles (including the fallback renderer), and halts in idle/pickup/handoff without changing fan animation.

## Validation
- Original PNGs and their `.meta` GUIDs are unchanged. New transparent `street-worker-direction-extension-v1.png` adds only 20 missing poses (1086×1448): food quarter-carry, beverage quarter-walk and empty-handed carry views. Existing contact art plus articulated passing legs provide four **runtime-composed gait samples**, not 32 newly authored bitmap frames. Supplement imports at original dimensions, Android RGBA32, no obsolete iPhone PVRTC override.
- 19 distinct EditMode cases passed: 18 animation/movement cases (17 first run +1 corrected fixture) and 1 palette regression, including warm off-white Premium apron and unchanged skin/face/alpha.
- Native PlayMode acceptance passed:7 real products ×8 directions ×4 gait phases ×2 portrait scales = 448 samples. Checks hand contact, uniform body fit, four distinct gait samples, preserved physical object identity, front/back/finger depth, consume cleanup and no duplicate carried overlay.
- Real Main gameplay reviewed with 5 and 17 hired workers covering all four roles, at 1080×1920 and 1220×2712. Actual deliveries/revenue progressed through unchanged simulation; visual gait captures and hand-held products reviewed. Live StreetView disable/re-enable recreated palette/sprite resources safely; final runtime console had 0 errors/warnings. Exact original save, GameView, timeScale/background settings restored; Play stopped, scene clean.
- Evidence (ignored): `Logs/Acceptance/WorkerAnimation-20261008/main-five-staff-portrait.png`, `main-five-staff-tall.png`, `main-full-staff-final.png`, `main-full-staff-tall-final.png`, `gait-contact-sheet.png`, `workers-live-gait.gif`.
- Workers pool four SpriteRenderers each; cached sprite slices and palette textures, reused carried-ID set, no per-frame pixel/key generation. Native workers reuse the props camera and disable the old carried-only pass. No full-suite run, new APK, phone validation or Android FPS certification.

### 2026-10-09 — Barrel pickup reach and leg cadence

Coca and beer now use explicit accessible points at their respective barrel fronts. The worker pauses for the existing pickup duration; an empty-handed reach pose follows pickup progress and the carried drink is projected only after a real TryTake. Multiple Cocacoleros have no shared pickup lock and can serve separate Coca/beer customers in parallel. Gait uses80 logical units per four phases (versus48 before), driven by measured travel rather than time; movement speed, service duration, item lifecycle, fan animation, economy and objectives are unchanged.

Focused Unity checks passed: four EditMode worker/pickup cases, five route-matrix cases across levels0–4, and one native PlayMode projection case covering Coca/beer empty-hand reach, no pre-pickup carried object, post-pickup real unit ID, all seven products/eight directions/four gait phases/two portrait scales. An in-editor concurrent Coca+beer delivery also passed and earned only the existing $5+$7. The initial route test exposed a boxed-float cast in its expected-price assertion; that test-only cast was fixed and all five route cases passed on rerun. No full suite, APK or device build was run.
