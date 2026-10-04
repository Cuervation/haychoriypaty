# Project status

**Current milestone:** Street automation requested from the video, active on Main in Unity6000.6.3f1. Editor gameplay and original art verified; Android0.2.0 APK compiled/installed and native emulator round verified. [Authoritative feature](features/street-automation.md). Earlier two-product Android0.1.0 evidence remains in [Android history](features/android-prototype.md), not acceptance of this new APK.

## Implemented and executed

- Main owns `StreetGame`/`StreetView`; legacy `GameController`/`PrototypeView` disabled there, source/tests preserved. Main/SampleScene GUIDs and build entries preserved; no new package/service.
- Original sprite assets: Floresta/All Boys cartoon background,16 character poses, seven products plus street grill/cooler/condiments and management UI. No copied video assets or geometric stand-in actors. Active art bounds/layout visually checked in portrait.
- Bounded deterministic simulation:21 clients in7×3 rows, orders1–999, independent reservations and physical station/pickup/carry/handoff gating; per-product prices/demand, up to8 workers, affordable escalating speed/hire, patience/exit, win/lose/replay and five progression levels.
- Save v1 restores prices/coins/staff/speed/unlocked level only, not in-flight rounds. Corrupt/incompatible saves reset safely; original user prefs preserved during isolated checks.
- Platform-specific input: editor Input System bridge; native IMGUI only in Android. Single shared safe-area geometry with stale editor-safeArea fallback.

## Validation — 2026-10-03

- Full reference video decoded: **1798/1798 encoded frames**,60fps,29.9667s (roughly1800). Inspected whole time range and transition samples; image analysis found22 stable decrements999→977. No hire/speed tap or completed client occurs in this clip. Video mechanics vs requested extensions are separated in [visual reference](visual-reference.md); raw/extracted evidence stays private in ignored `Logs/ReferenceAnalysis/`.
- Street EditMode **16/16 passed**, job `41993b7f6e044a3c91f61f0b1ec9bafd`: handoff, prices/demand, cancellation, caps, independent RNG, reservations, throughput, costs and outcomes.
- Street PlayMode **9/9 passed**, final job `39937369fdf3482eaa79024ae973f1c5`: queued mouse/touch Start, slider endpoints, unique purchases, replay, independent product-price selection/no HUD overlap and viewport edge cases. These are editor tests, not physical-device certification; no unrelated full-suite run.
- Actual Main one-worker play finished **Won24 units/$120 earned in63.65 simulated seconds**; reload restored coins/unlock. Second run restored120 and won24 at240coins. This is deliberately slower than video counter cadence because user-required real roundtrips gate each unit.
- Isolated five-level runs with8 workers reached goals24/40/65/85/110 and unlocked product counts1/3/5/6/7. Actual wrapper load/save restored123coins/3staff/2speed/level4/seven distinct prices; corrupt JSON and version99 defaulted safely.
- Pure simulation profile21clients/8workers:1000steps (16.67sim seconds) cost1.403ms total in editor;49deliveries vs6 with1worker. **Not GPU/mobile FPS**. All Assets paths have metas; duplicate GUID scan empty.
- Reviewed real Game-view `Logs/Acceptance/street-layout-final.png`: upper crowd/menu, central street workers, bottom stations and large Speed/Cook cards. Captures/logs ignored in Git. Editor compilation completed before focused tests.

## Android0.2.0 actual run

Final build `build-9d977619d5`:0errors/52warnings, signature/package/ABI verified. Native Start, price60→5,21clients/999orders, exactly one hire15 and speed5; victory24/$100/team2/rate1.25 confirms24×5−20. Native next unlockedChicago/3products/goal40; zero endpoint, separatePaty11/Chori5 and restart restored100coins/team2/rate1.25/unlocked1. Product selectors moved below price-panel title after real screenshot exposed HUD overlap;9/9 regression passed, final APK rebuilt/installed; native product selection and visible/unobstructed HUD confirmed. Read [Android delivery](features/android-prototype.md) for artifacts, warnings and scoped limits.

## Remaining

- Physical-phone installation/validation pending: phone currently absent from adb; new0.2.0 emulator install does not update the old phone APK.
- Five gameplay locations share the first Floresta artwork; distinct Chicago/Argentinos/Vélez/Ferro scenery and supporter kits still pending.
- Audio, final animation/art polish, device FPS/thermal profiling, broader cutouts and pause/resume stress, balancing and release signing/Play Store publication remain unverified or out of scope.
