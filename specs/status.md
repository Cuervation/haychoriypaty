# Project status

**Current milestone:** Street automation active on Main in Unity6000.6.3f1. Current trial0.2.2: new Parrillero sprites/name and Floresta runs the full180seconds; below is the historical0.2.0 acceptance plus the new scoped record. Editor gameplay and original art verified; Android0.2.0 APK compiled/installed and native emulator round verified. [Authoritative feature](features/street-automation.md). Earlier two-product Android0.1.0 evidence remains in [Android history](features/android-prototype.md), not acceptance of this new APK.

## Implemented and executed

- Main owns `StreetGame`/`StreetView`; legacy `GameController`/`PrototypeView` disabled there, source/tests preserved. Main/SampleScene GUIDs and build entries preserved; no new package/service.
- Original sprite assets: Floresta/All Boys cartoon background,16 character poses, seven products plus street grill/cooler/condiments and management UI. No copied video assets or geometric stand-in actors. Active art bounds/layout visually checked in portrait.
- First-level mural update: active StreetView background uses versioned `street-background-mural-v3.png`, with original monochrome supporter art on the far wall across the street; old background retained. Unity import/refresh completed and Play-mode Game View at the start menu visually reviewed (`Logs/Acceptance/Mural/floresta-mural-menu.png`). Console had no compile errors; no APK/device validation requested or run.
- Bounded deterministic simulation:21 clients in7×3 rows, orders1–999, independent reservations and physical station/pickup/carry/handoff gating; per-product prices/demand, up to8 workers, affordable escalating speed/hire, patience/exit, win/lose/replay and five progression levels.
- Save v2 adds a one-time v1 migration resetting inflated starting staff/speed to 1 worker/×1.00, while preserving prices/coins/unlocked level; in-flight rounds are not saved. Focused migration EditMode test passed (job `9a01a404c67042c49eae40f8170a5e36`, 1/1).
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

- Physical phone now updated to0.2.4/code6 via USB install-rSuccess/launchStatusok (see delivery below); physical-screen/touch/FPS validation remains pending.
- Five gameplay locations share the first Floresta artwork; distinct Chicago/Argentinos/Vélez/Ferro scenery and supporter kits still pending.
- Audio, final animation/art polish, device FPS/thermal profiling, broader cutouts and pause/resume stress, balancing and release signing/Play Store publication remain unverified or out of scope.

## Floresta three-minute trial — 2026-10-03

User chose a full180-second first level. `florestaFinishAtDeadline=true` on Main means reaching24 units no longer ends Floresta: continue serving/earning until the clock, then evaluateWon/Lost. Clock clamps to the deadline; other levels still win early. No waves, products, art/input, upgrade/reset or save-schema changes.

- Street simulation **22/22 passed**,0failed/skipped, job `e1d05e79e164439f94d13b0e4c107315`: new continuity/extra earnings with8staff+7speed,Playing at179s,Won exactly180/frozen after end, insufficient-goal deadline loss, and four later-level early wins.
- Main new policy explicitly serialized; editor0 compilation errors after import. Version0.2.1/code3 flushed toPlayerSettings. APK0.2.1/code3 build75.02s/0errors/4warnings,signature/ABI/version verified and installed. Native continued60units/$972 aftergoal24 with172s left (baseline672coins,7staff/rate3.25 preserved); complete deadline check pending. Earlier0.2.0 screenshots/results remain historical.
- MCP recovered using installed10.0.0 cache and existing bridge API, no install/second editor. Chat client cached startup error; real MCP SDK client can query/call tools. Temporary editor bootstrap restored, metas unchanged.

- Physical Motorola updated0.2.1/code3 and launchedStatusok after user chose phone over emulator; USB subsequently disconnected before screen verification. Old emulator endpoint shows958sales/56s remaining, not a verified deadline result. No emulator now running; preserved data with install-r.

## Parrillero character — 2026-10-03

Generated original16-pose transparent atlas and hire portrait following user photo/required fat shirtless character with dirty white apron. Active view switched to new assets and visible Parrillero label; supporters, gameplay/save and existing metas preserved. Final selected face uses wavy dark hair/light stubble, fuller cheeks/jaw and matched skin; previous giant-eyed alternate discarded.16 named Unity sprites/foot pivots verified.3/3 StreetArtTests passed (job962b86955487489d9818270118bbc5c3),1/1 affected hire/speed pointer test passed (job5e6b90e5136b4cf5bac2115a5e3e1aa7),0compilation errors. First pre-import job found0tests and is not counted. Extra carry/celebrate/tired poses delivered, no new simulation actions. APK0.2.2/code4 succeeded210.41s0errors/3warnings and signature/ABI verified; phone install/native visual check pending because USB/adb inventory empty even after user reconnection and oneADB restart.

Actual editor Main capture reviewed (2026-10-04): new profile walk, matching hire portrait and unclipped Parrillero label in original Floresta layout. Ignored Logs/Acceptance/Parrillero/parrillero-final.png; stopped Play and restored exact prior editor progress. USB/ioreg and adb show no physical phone, so APK0.2.2 not yet installed/launched there. Three build warnings: diagnostic symbols and Unity-splashPVRTC/fallback; no postprocessing warning in this build.

## Floresta fixed economy and small orders — 2026-10-04

Implemented in source: fixed$5 chori and read-only start/replay UI (no slider or hidden drag action), fixed$5 speed purchases/+10percentage points of base rate, fixed$25 extra parrillero, automatic1–4 orders including first arrival. Completed orders retain existing receipt→exit→replacement flow, with per-unit$5 earnings. Old stored price normalizes without resetting coins/staff/speed/other prices; later levels preserve existing rules. Full180seconds/goal24 and art unchanged. Added10EditMode/2PlayMode cases and adjusted affected previous fixtures.

Validation pending: git diff check passed, but MCP refresh timed out60seconds and test invocation disconnected. Read-only Unity process sample confirms Main scene-change/reload modal blocks editor command processing; user asked to accept Reload. No test success claimed before rerun; no unrelated suite or second editor. User explicitly requested noAPK: existing0.2.2/code4 artifact does not contain this change and was not rebuilt/installed.

## Large parrilla and validated Floresta rules — 2026-10-04

Original long iron-grill sprite with dense rows of chorizos, bread and embers replaces miniature BBQ presentation.494x108logicalpx for1–4 unlocked hot foods;280x90when beverages need right-side space. Seven-product catalog and all simulation pickup anchors remain unchanged. Only unlocked markers drawn; original artwork remains fallback. Previous first-level economy/order change now verified (earlier blocked record historical):41/41EditMode (32simulation+9art),11/11PlayMode; jobs c900430c9df54e1ab041b0a2475b6e54 and81416a6df3c746eb8fa26d209fbcf885,0failed/skipped.

Real1080x1920Game-view Ready/FlorestaPlaying/seven-product captures reviewed; grill scale, no price slider,$5/$25/+10%,1–4orders and unobstructed HUD/buttons/drinks confirmed. Exact prior editor progress restored(true),Play stopped. Unity0compilation errors before tests. User now explicitly requests APK, but no phone connection/install/launch. Version0.2.3/code5 build-4df2bd9cd6 succeeded363.11s,0errors/3warnings. APK57,681,379bytes, V2signature/package/ARM64 IL2CPP/min26/target36 verified; SHA256af50ac8961483c875191fa5ad4540484454ddf8037bb3e1e5e3ad2fcd471db90. Warnings: diagnostics require symbols, obsolete Unity-splashPVRTC and uncompressed splash fallback. No new packages, second editor, emulators or adb actions.

## Floresta fan wardrobe overlay — 2026-10-04

Added a first-level-only renderer overlay which selects among the 15 logo-free `FanWardrobe` textures by customer ID; original fan sprites and later levels stay unchanged. Added focused EditMode coverage for resource loading, alpha/import settings, Android RGBA32 and deterministic rotation. Unity Editor import/test/Game-view verification was not run in this turn; visual placement acceptance remains pending.

## Startup cover/logo intro — editor and APK verified (2026-10-04)

Supplied cover/logo textures and 4.1-second unscaled reveal/flash now implemented; central dispatch gates native Android and desktop actions, with a black backing during initial fade. 11/11 focused art EditMode checks passed. Two batch pointer attempts failed to process queued inputs (first also had a stale v1 preservation fixture); those failures are not hidden passes. The corrected v2 fixture and full affected pointer suite passed 12/12 in real editor (job adfc5a956d4f48548c73f34aa2caec0d), including intro input/timeScale/Ready/Start regression.

Actual 1080×1920 cover, logo-flash, Ready and representative Floresta wardrobe captures reviewed in ignored Logs/Acceptance/Intro/. Prior editor progress restored exactly (true); Play stopped, Main is clean. Authorized stuck-editor shutdown/restart restored MCP; project, state, active scene, hierarchy and console compilation-error query succeeded. No new packages or simultaneous editors. Android0.2.4/code6 ARM64/IL2CPP Main-only build1970fbd5d5 succeeded364.73s,0errors/3warnings. APK106,121,029bytes (101.20MiB); V2signature/package/min26/target36 verified. Exact SHA and warning details are in [Android delivery](features/android-prototype.md). Unity remains open on clean Main, Play stopped; no phone/emulator installation or native verification.

## Startup APK — physical phone delivery (2026-10-04)

User requested installation on the connected Motorola Edge60Fusion. Serial-targeted replacement install returned Success; PackageManager confirms0.2.4/code6. GameActivity cold launch Statusok/769ms and app process alive. No uninstall/data clear, rebuild, new packages or emulator. Foreground changed to another app before screenshot, so no physical-screen/intro/touch/performance verification claimed. Existing editor stays open, Main clean/Play stopped.
