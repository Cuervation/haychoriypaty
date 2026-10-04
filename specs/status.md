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


## Longer startup and Jugar/Salir menu — implemented, checks deferred (2026-10-04)

Source now holds the fully visible cover 4s, reveals the logo, holds it fully visible 4s, then retains both behind original glossy blue JUGAR/SALIR buttons (Luckiest Guy with white/dark-outline text; Apache 2.0 license bundled). Fades are extra: buttons become available at 8.63s unscaled. Jugar starts the saved selected round directly; Salir calls player quit / Editor Play stop. Hidden game controls are not rendered/dispatched while startup/menu owns input; no simulation/economy/progression/product changes. Existing startup regression source adapted to persistent menu, not executed.

**Validation:** Tests, Play-mode/visual/device verification and APK generation explicitly deferred by user; previous 0.2.4 test/build records are historical, not proof of this change. No second editor, package install, phone control, version bump or build. Source diff reviewed; normal asset refresh imported the font with includeFontData enabled and generated new metas; Editor finished reload, remains on stopped Main. Console MCP requests returned session-not-ready/ping timeouts, so clean compilation is not confirmed; no gameplay validation claimed.

**Next:** When user requests combined validation, check the≥4s+≥4s timing, button typography/layout/safe-area, both platform input paths, direct Jugar and native Salir; build/install only if separately requested.


## No Floresta price popup — implemented, untested (2026-10-04)

Removed the full first-level price card/summary from StreetView.ReadyPanel, including replay/Ready paths; non-modal blue Jugar and unlocked-level navigation remain. Hidden product hit targets are also gated to editable-price levels. Simulation already enforces chori $5 (including old saves), so economy/progress and later levels remain unchanged. Startup menu changes from the preceding request are preserved.

**Validation:** Source inspection only. Explicitly no tests, Play-mode/device checks, APK, version/settings/scene/meta changes or new Unity session; runtime/import/compilation for this edit not verified. Test files were not changed in this follow-up.


## Floresta $25 speed / $200 helper and replay reset — implemented, untested (2026-10-04)

Changed StreetBalance defaults and Main serialized values to $25 per speed upgrade and $200 per extra parrillero; +10% base increments and fixed/no-escalation first-level costs remain. StreetSimulation constructs and starts every first-level turn with one worker and SpeedLevel0 (displayed x1.00), clearing old staff and resetting worker IDs; this also covers wrapper reload/retry and return-to-Floresta start. Coins, unlocks, fixed $5 chori and later-level staff/speed rules remain unchanged. Earlier cover/menu/no-price-popup edits preserved.

**Validation:** Source inspection only; user previously explicitly deferred all tests/APK and that instruction is retained. No Play, tests, APK, device/Editor mutation, package install, save-schema/version change or compilation/import verification. Scene diff is limited to the two balance fields; existing metas preserved. Old hard-coded cost/staff-preservation assertions in StreetSimulationTests/StreetPointerTests must be updated for the new contract before combined testing; no test-file edits in this turn.


## FIFO lanes and rear-only replacement — implemented, untested (2026-10-04)

StreetSimulation now releases departing queue slots and advances existing customers in the same column (all levels), retaining ID/order/patience and moving continuously. New arrivals are tail-only; leaving people remain counted until off-screen. Advancing state keeps visible order bubbles and uses existing walking frames; StreetView depth now follows actual positions. Workers reserve/deliver only to settled front customers; stale/moving/departed assignments are canceled without sale/reward or stuck reservations. Earlier first-level costs/reset, no price popup and startup menu preserved.

**Validation:** Source review only. No tests, Play/visual/device checks, APK, compile/import confirmation, package/asset/scene/meta changes, commit or push. Standing no-tests/no-APK instruction retained. Existing tests that assume direct free-slot reuse/rear-row serving need adaptation before later combined checks.


## Open street backdrop / counter-only stand — implemented, game review deferred (2026-10-04)

New original versioned940×1673opaque `street-background-open-street-v4.png` removes the upper stadium/sky, roof/awning, branding header, chalkboards and tall supports. Mural ends at the top frame; broad open crowd street fills the space behind the retained counter. Existing worker sidewalk, condiment bench and lower HUD band retain their bands. StreetView loads v4 and removes both upper product-sign icons; queue/counter/worker/station/input anchors and prior gameplay/menu/reset changes preserved. Old v3/metas retained; new meta has fresh GUIDs and inherited import settings/full bounds. Exact prompt/method/SHA retained in GenerationPrompts.md.

**Validation:** Built-in edited image statically reviewed and source diff inspected only. Explicitly no tests, Play, APK, device work or Unity import/compile confirmation. Runtime crowd/overlay/counter alignment with new backdrop is still unverified. No package/settings/scene changes in this turn; no commit/push.


## Waiting supporter animations — implemented, untested (2026-10-04)

StreetView now adds per-customer staggered breathing, foot-pivot leaning/weight shifts and occasional two-bounce cheering while stationary. Existing supporter identity and Floresta clothing share the same body transform; GUI matrix is restored before stable order/patience indicators. Walking/Advancing and receipt bob remain; existing AnimationTime drives all idle movement, with no changes to simulation, FIFO, economy, input, sprites/metas or prior work.

**Validation:** Source/diff review only; no tests, Play, APK, device work or Unity compilation/import confirmation. Visual motion, garment attachment and dense-row readability await the later combined check requested by the user. No test files changed for this request.


## Presentation-logo app icon — implemented, unbuilt (2026-10-04)

PlayerSettings assigns the existing street-logo texture as default and all12Android single-layer icons. New Editor-only StreetAppIcon Gradle callback copies that same PNG byte-for-byte and composes a cream adaptive icon with percentage padding/aspect preservation, avoiding cropped lettering or generated/redrawn art. No source logo/meta, intro/gameplay, package/version/signing/input or earlier changes modified. New script meta has a fresh GUID.

**Validation:** Source/diff review only; Unity MCP readiness read failed ping. No tests, Play, APK/export, device change or verified Unity compilation/import/callback run. Phone still has prior installed0.2.4/icon; verify branding with the later combined build/install.


## First-level1000goal / gold coin / corner HUD — implemented, untested (2026-10-04)

Floresta default/fallback/Main goal changed24→1000, deadline-only flagtrue→false, user-confirmed180seconds unchanged. Early first-level threshold handoff stops remaining workers in that slice, preserving actual unit/price accounting and freezing winning counter at1000/1000. Other goals/rules unaffected.

Built-in ImageGen produced a new original transparent1254×1254coin; after user rejected colored accents, a scoped gold-only edit preserved choripán/paty relief. Final `street-coin-gold-v2.png` copied intact with fresh meta/full bounds, NPOT/mips off and AndroidRGBA32; draft/output retained, exact prompts/hash in GenerationPrompts.md. StreetView loads it for HUD/income effects; moves coin balance top-left and sold/goal top-right in slim panels above rear orders, leaving timer lower and all interaction/queue anchors unchanged. Current gameplay summary updated to authoritative costs/FIFO/goal without changing those earlier systems.

**Validation:** Static image/source/diff review only. No tests, Play, APK/export/device work or verified Unity import/compilation; new goal/balance,1000-unit win/deadline loss, corner readability and gold icon appearance remain pending combined checks. Old24-unit/deadline-only fixtures need adaptation; no test files changed. Earlier app-icon/menu/queue/cost/reset/art/idle work preserved; no package/version/signing change, commit or push.


## Combined validation checkpoint — 2026-10-04
Latest user now authorizes combined tests/APK/phone update. Active checkout remains`/Users/celestino/HayChoriYPaty`, Unity6000.6.3f1/Android/Main. Updated Street fixtures and targeted EditMode52succeeded/no failures + PlayMode12/12passed; relevant evidence in`features/street-automation.md`. Reviewed actual menu/crowd/HUD and paired idle animation captures in ignored`Logs/Acceptance/Combined`; exact editor save restored, Play stopped. Android0.2.5/code7 development Main-only build`build-47e1c59fff` succeeded277.796s/0errors/3warnings. APK114272726bytes/SHA`99d78efd142a624e612259737664ab013a0667e41d8319a62e6733b3d296403c`, V2same debug certificate, min26/target36/ARM64; original adaptive logo bytes verified. RollingAPK plus0.2.5-code7 archive preserved, older0.2.4 archive untouched. Seven-product Ready/Playing layout also reviewed and exact editor save restored. Motorola serial`ZY22MBNWRB` was connected with0.2.4/code6 but then disconnected; finaladb inventory empty. NewAPK is **not yet installed/launched**, nativeSalir/launcher/device behavior still pending. User asked to reconnect/unlock; next action`adb install -r` without data wipe, then native smoke check. Evidence`Logs/Acceptance/Combined/summary.json`, full Android delivery in`features/android-prototype.md`.
