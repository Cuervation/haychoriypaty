# Project status

## Level selector and victory-popup route — source update (2026-10-05)

Presentation **JUGAR** opens a five-location selector. All Boys/Floresta and Nueva Chicago cards use their own existing mural and shield; locked later cards show the grill/product progression with restrained club-color accents. A card is enabled only up to persisted `UnlockedLevel`; Floresta starts directly, later levels retain their existing price/setup screen. The won-level popup now contains only **SALIR** after sales/final balance/remaining-time summary, and this action resets to Ready then returns to the selector. Main presentation **SALIR** remains app exit. Added pointer/progression and card-art layout regression source but did not execute it, preserving the user's standing no-test instruction. No Unity/Game-view, APK or device work was done.

## Top HUD extended to physical screen edges — source update (2026-10-05)

The amber top bar now draws its outer frame edge-to-edge, including its decorative fill behind the reserved camera/notch channel. Expanded the sales capsule/text slot; HUD labels use a larger preferred font and fit to both width and height, with compact paired Level-2 counters identified by their icons. No change to HUD height, safe-area interaction geometry, game rules or timer. Updated the focused test source and HUD specifications. No tests, Unity/Game view, APK or phone update per standing no-test instruction.

## Android APK 0.2.11/code13 generated (2026-10-05)

Implemented selected upgrade-card option1: current speed multiplier plus next `+10%` in the Velocidad status row, live `EQUIPO N` in the Parrillero status row, and removed the duplicated footer. Main-only Android IL2CPP/ARM64 build `build-2dccbdc803` succeeded in19.38s (0errors/1warning). Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.11-code13-upgrade-option1.apk`,181,258,740bytes; SHA-256`7ff670d8c5999a4c980fd263433c57289a6468c09aeea1928da9aec5ffe4fbab`; package/version/ABI and V2 signature verified. No tests or phone install per the user's standing instruction/request scope.

## Android APK 0.2.10/code12 built and installed (2026-10-05)

Built current Main source with Unity6000.6.3f1 as Development IL2CPP/ARM64. Build succeeded in 246.68s. The log contains a non-blocking Licensing access-token update diagnostic and one Diagnostics Data debug-symbol recommendation; no gameplay tests were run. APK `Builds/Android/HayChoriYPaty-street.apk` and archive `Builds/Android/Archive/HayChoriYPaty-0.2.10-code12-mural-wardrobe.apk`, 212,807,089 bytes, SHA-256 `639e11908265ff2b228afc2a40621f69a6b2d000f5dd1993f4aa5bb8244eb8a4`. V2 signature/package/version verified. Installed successfully with `adb install -r` on the authorized Motorola Edge 60 Fusion (serial `ZY22MBNWRB`); PackageManager confirms versionCode12/versionName0.2.10. No uninstall, app-data clear, or launch; no tests were run. Evidence is in ignored `Logs/Acceptance/Level1MuralWardrobe-20261005/`.

## Nueva Chicago supporters — eight integrated outfits (2026-10-05)

Replaced Level 2's separate garment overlays with eight complete green/black/off-white fan designs in matched front and profile-walking 3×3 atlases. A separate 4×2 Chicago-colored atlas keeps its full-body outfits during the angry stick-raising/swinging timeout animation. The old garment sheet remains in the project as historical art but is not loaded by the renderer. No gameplay/simulation behavior changed.

- [x] Added transparent RGBA front, walking and timeout atlases with Android RGBA32 import metadata.
- [x] Level 2 crowd art selects outfit deterministically by customer ID; facing direction, walking, anger and swinging remain pose-driven.
- [x] Unity 6000.6.3f1 imported the assets and compiled the changed scripts with zero console errors; focused EditMode atlas/import/runtime-selection test passed **1/1**.
- [x] Live 1080×1920 Level-2 Game-view screenshot reviewed: `Logs/Acceptance/ChicagoOutfits-20261005/chicago-eight-outfits-playing.png`. Eight integrated green/black outfit looks appear in the crowd, including a walking pose, with no separate garment layer.
- [x] Unity Play stopped and the prior `StreetGame.v1` PlayerPrefs progress/economy value restored exactly after capture.
- [x] No APK/device installation was requested; none generated.

## All Boys supporters — nine integrated outfits (2026-10-05)

The prior four black/white-uniform-only poses failed the user's visual expectation that the curated All Boys apparel should appear distinctly in the crowd. Added two matched transparent 3×3 atlases: `street-allboys-fans-front-v1.png` and `street-allboys-fans-walk-v1.png`. Each cell is a complete fan body in one of nine white/black/gray/cream looks; runtime cycles by customer ID for both cheering and walking. Level 1 never overlays garment art; Nueva Chicago was still using a separate wardrobe renderer at this checkpoint and was refreshed separately below.

- [x] Unity 6000.6.3f1 complete test assemblies passed: EditMode **116/116** and PlayMode **21/21**, zero failures/skips. EditMode includes the 9-outfit atlas/crop/pose regression.
- [x] Live 1080×1920 Level-1 Game-view screenshot inspected and saved at `Logs/Acceptance/AllBoysOutfits-20261005/allboys-nine-outfits-playing-screen.png`; nine looks rotate deterministically, are part of full-body sprites, and have no garment overlay.
- [x] Editor Play stopped and the exact original `StreetGame.v1` PlayerPrefs JSON (240 coins, level unlock, price/staff/speed) restored after the capture. No APK was generated or installed; native-device appearance not verified.

## All Boys four-uniform pose correction — historical/superseded (2026-10-05)

At this earlier source revision, the nine separate garment overlays had been removed and replaced with four complete front and four matching walking poses from street-characters. This stopped the superimposed-shirt defect, but did not visibly use the varied curated outfit styles. The later nine-outfit full-body atlases above supersede this clothing selection; the no-overlay rule and unchanged Nueva Chicago apparel remain current.

**Validation:** Unity6000.6.3f1 StreetArtTests **44/44 EditMode** and StreetPointerTests **17/17 PlayMode**, 0 failures/skips. Opened Main in Play Mode, started Level 1, and saved/visually reviewed the 1080×1920 screenshot Logs/Acceptance/AllBoysKit-20261005/allboys-level1-playing.png. Editor PlayerPrefs were restored exactly after capture. No full-suite run, APK build, or phone install; installed 0.2.10/code12 predates this source-only correction. Evidence logs are under the ignored Logs/Acceptance/AllBoysKit-20261005/.

## Floresta mural continuity — source update (2026-10-05)

The earlier crop ended at source y=234, before the mural wall/coping image finished. `StreetView` now draws one continuous mural strip from source `(0,0,940,280)` into logical `(0,0,540,118)`, covering the underlying v4 wall so its mural cannot reappear as a second band. The street, counter, foreground and gameplay anchors below remain unchanged. Updated the focused asset/layout regression source.

**Validation:** Source and diff review only. No tests, Unity/Game-view review, APK build or device install, following the standing instruction. Visual confirmation of the seam is still pending.

## Floresta All Boys apparel overlay selection — superseded (2026-10-05)

This intermediate source revision temporarily selected nine standalone garment sprites for Level 1. The user rejected that approach because the garments read as overlays rather than naturally worn All Boys clothing. The current implementation uses the complete uniformed fan poses; see the correction above.

**Historical validation:** No tests or Unity/Game-view review were run on that intermediate revision. It was not built or installed on device.

## Nueva Chicago fix — Android APK 0.2.9/code11 built and installed (2026-10-05)

Packaged the current project in Unity6000.6.3f1 as a Main-only Android Development IL2CPP/ARM64 APK. Build succeeded in **240.64s**, 0 errors / 1 Diagnostics Data debug-symbol recommendation. APK: `Builds/Android/HayChoriYPaty-street.apk` and versioned archive `Builds/Android/Archive/HayChoriYPaty-0.2.9-code11-level2.apk`; **171,936,606 bytes**, SHA-256 `be171c5d141d7de2fd4af2dab0bd8654d67b5c629c14ae3e299d525a9bdf08fb`, V2 signature verified. Package `com.haychoriypaty.game`, min API26 / target36, ARM64.

Installed to the connected Motorola Edge 60 Fusion with `adb install -r`; versionCode11/versionName0.2.9 confirmed. PlayerPrefs matched byte-for-byte before/after and original firstInstallTime remained intact. No uninstall, data clear, or app launch. The full 116 EditMode + 21 PlayMode suite had passed on this exact code before the version-only build. No native gameplay/visual review was performed. Details/evidence: [Android delivery](features/android-prototype.md) and ignored `Logs/Acceptance/Level2Delivery-20261005/`.

## Nueva Chicago delivery clearance and wardrobe fit — source update (2026-10-05)

Moved Chicago's idle/handoff point to the player-side floor (logical y=485, below counter front y=384) and kept all worker station/handoff travel outside the counter. Fitted Chicago wardrobe overlays independently for frontal standing and side-walking poses. Added regression checks for both-product handoff clearance and pose-specific outfit bounds.

**Validation:** Full Unity6000.6.3f1 batch suites passed: EditMode **116/116** and PlayMode **21/21**, 0 failures/skips. Automated route/layout checks are not a substitute for native Game-view/phone visual review; the later APK/install above contains this change but no gameplay screenshot was taken.

## Full Unity suite and Android APK — 0.2.8/code10 (2026-10-05)

All Editor tests passed on Unity6000.6.3f1: **115 EditMode + 21 PlayMode**, 0 failed/skipped. The first PlayMode run had 3 failures caused by missing Game-view focus in an older pointer fixture; the fixture now preserves/restores the same temporary Input System focus settings as the newer suite, and all 21 passed on rerun. Android Development IL2CPP/ARM64 build succeeded with 0 errors and 1 Diagnostics Data debug-symbol recommendation. APK: `Builds/Android/HayChoriYPaty-street.apk` (archive: `Builds/Android/Archive/HayChoriYPaty-0.2.8-code10-fullsuite.apk`), 168,244,348 bytes; SHA-256 `6600a5dc54d5c9f917ce2d33d8f2ee999f613ba87cebcd8ef02f44ea8371b240`; V2 signature verified. No phone install or native gameplay validation was requested/performed. Evidence is ignored in `Logs/Acceptance/FullSuite-20261005/`.

## Nueva Chicago duration set to three minutes — source update (2026-10-05)

User requested a 180-second Level 2 limit. Updated the inspector/default/fallback durations and Main serialization; Floresta remains 120 seconds, while Levels 3–5 remain 240/270/300 seconds. Added a regression-source assertion for Chicago's default time and updated the progression table. No tests, Unity run, APK or device install per standing instruction.

## Floresta duration reduced to two minutes — source update (2026-10-05)

User requested a 120-second Level 1 limit. Updated StreetBalance inspector/default/fallback values and Main serialization; goal remains 200 actual choripán handoffs, win-at-goal/deadline-loss behavior and all later-level durations are unchanged. Regression source now expects 120 seconds and includes the 2:00 HUD formatting case. The previous deterministic balance run completed in 98.9 seconds, which is 21.1 seconds under the new limit by comparison; it was not rerun after this edit. No tests, Unity run, APK or device install per standing instruction.

**Current source milestone:** Level 2 / Nueva Chicago has its club mural/shield, bottle/barrel, chori-only/Coca-only/combined orders, eight original green/black/off-white fan outfits integrated into full-body front/walk/timeout sprites, and a two-part win goal of 200 choris **and** 200 Coca bottles. Its upper sales capsule tracks the products separately. Level 1 rotates nine neutral All Boys-inspired looks integrated into complete front/walking crowd sprites; no clothing layer is overlaid in either level. Current source correction and validation are recorded above. The latest installed phone APK is **0.2.10/code12**; it predates both integrated apparel sprite refreshes. Earlier entries below are historical checkpoints, not current source facts. See [Android delivery](features/android-prototype.md).

## Timeout trifulca becomes a live animation — source update (2026-10-05)

Replaced the single static crowd composite as the active loss presentation. `StreetView` now animates the actual frozen waiting queue through fear/anger into staggered raised-stick/swing poses from a new 4×2 fan atlas. A people-free damaged-stall background fades in during the attack, while fan frames, screen rumble and wood debris continue on unscaled time. Added fresh metas for both generated assets and updated the focused test source and authoritative visual/gameplay specs.

**Validation:** Static source/art/spec review and diff whitespace check only. No tests or Unity/Game-view run, APK or phone changes, per the user's standing instruction. Visual pose alignment and full sequence timing remain pending.

## Floresta mural seam correction — initial source update (2026-10-05)

The first correction extended the new mural overlay through logical y=106 while retaining the v4 street/counter/foreground. The user later reported that the wall still appeared cut/restarted; the follow-up below extends the source crop through the full lower wall edge/coping and slightly increases the continuous overlay band.

## Nueva Chicago 200 + 200 objective — source update (2026-10-05)

Chori and bottled Coca-Cola progress now has independent per-round counters; Level 2 wins only after at least 200 actual handoffs of each (400 minimum combined). Timeout/loss and HUD/result progress use the same per-product target. Other levels retain their existing goal logic. Both products start at a $5 sale price; Level-2 price controls remain available.

**Validation:** Added focused simulation regression source but did not run tests or Unity; no APK/device install.

## Nueva Chicago supporter wardrobe — historical overlay implementation (2026-10-04)

Level-2 fans now layer one of eight generic logo-free jersey, T-shirt, hoodie/sweatshirt, track-jacket and windbreaker designs over their existing animated bodies. The green/black/white palette and garment categories follow the current [Tienda Oficial de Nueva Chicago](https://tiendanuevachicago.com.ar/); store photography, exact crests, and manufacturer marks are not reused. Floresta's separate All Boys rotation is curated to nine monochrome/neutral choices (see correction below).

**Validation:** Focused regression source was added but not run, per the user's standing instruction. Unity import/render review, test execution and APK/device install are not claimed.

## All Boys supporter clothing correction — source update (2026-10-05)

At this checkpoint, Floresta's active fanwear rotated only nine white, black, gray and cream options; Chicago's separate green/black/white wardrobe was unchanged then. Both levels' current complete integrated sprite implementations are recorded in the later updates above.

**Validation:** Updated the focused regression source and visual/feature specs; did not run tests or Unity/Game view, per the standing user instruction. No APK/device install.

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

- The phone remains on the earlier0.2.7/code9 build with data preserved; the current Level-2 feature is not installed. Controlled native intro/Salir/launcher-mask/touch accounting, Level-2 visual review and FPS/thermal validation remain pending.
- Argentinos Juniors, Vélez and Ferro still share existing gameplay art; location-specific murals and supporter kits remain pending.
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

Added a first-level-only renderer overlay which selects among the 15 candidate logo-free `FanWardrobe` textures by customer ID; the active list has since been curated to nine monochrome/neutral choices. Original fan sprites and later levels stay unchanged. Added focused EditMode coverage for resource loading, alpha/import settings, Android RGBA32 and deterministic rotation. Unity Editor import/test/Game-view verification of the current curated list remains pending.

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
Latest user now authorizes combined tests/APK/phone update. Active checkout remains`/Users/celestino/HayChoriYPaty`, Unity6000.6.3f1/Android/Main. Updated Street fixtures and targeted EditMode52succeeded/no failures + PlayMode12/12passed; relevant evidence in`features/street-automation.md`. Reviewed actual menu/crowd/HUD and paired idle animation captures in ignored`Logs/Acceptance/Combined`; exact editor save restored, Play stopped. Android0.2.5/code7 development Main-only build`build-47e1c59fff` succeeded277.796s/0errors/3warnings. APK114272726bytes/SHA`99d78efd142a624e612259737664ab013a0667e41d8319a62e6733b3d296403c`, V2same debug certificate, min26/target36/ARM64; original adaptive logo bytes verified. RollingAPK plus0.2.5-code7 archive preserved, older0.2.4 archive untouched. Seven-product Ready/Playing layout also reviewed and exact editor save restored. Motorola serial`ZY22MBNWRB` reconnected under user request; **0.2.5/code7 install-r Success**, PackageManager verified, exact dedicated playerprefs XML unchanged and first-install timestamp retained. Own GameActivity cold launchStatusok/777ms/PID15213 confirmed. USB disconnected again before runtime-log follow-up; that waiting command cancelled. NativeSalir/launcher/touch/performance remain unverified, not installation blockers. No data wipe or rebuild. Evidence`Logs/Acceptance/Combined/summary.json`, full Android delivery in`features/android-prototype.md`.


## Full-screen Parrillero presentation — 2026-10-04
User's0.2.5phone screenshot exposed intro letterboxing and mismatched cover face. StreetView now renders versioned no-hero backdrop across physical screen with proportional crop; exact existing gameplay/hire Parrillero portrait is drawn as foreground, original logo and blue menu stay on unchanged safe-area/input canvas. Black/shade/flash layers also cover full screen;4+4second timing/gameplay unchanged. Asset`street-cover-no-hero-v2.png` +freshmeta/provenance retained, old cover/portrait/logo/metas untouched. FocusedStreetArt17/17 andStreetPointer13/13passed; actual1080×1920and1200×2670menus reviewed in ignored`Logs/Acceptance/IntroFullScreen`. Editor progress and view selection restored, temporary tall QA preset removed, Main stopped. User chose **project only**, explicitly no new APK/install; installed0.2.5 stays old. Source/docs/art changes remain local/uncommitted; GitHub's1b2b99b does not yet include this refinement.


## Round-local coin balance — source update (2026-10-04)
All five levels now discard coins on Start/selection/next level/retry/load. Current-round delivery income/spending, unlocked locations, prices and prior staff/speed rules are preserved. Saved legacy balances are ignored without a save-version change. Focused simulation/lifecycle fixtures updated; static diff check passed. UnityMCP readiness ping failed, so compilation and tests are **unverified**, not passed. No APK/device change or commit/push. See the scoped acceptance in features/street-automation.md.


## Goal200 and progressive purchase tables — source update (2026-10-04)
Floresta goal is200actualchoripanes/180seconds, with $5 price and zero coins per attempt retained. All locations share the specified nine +0.1 speed tiers (ending x1.9) and four hires (ending five total staff); next costs advance after purchase, exhausted cards show disabled MAX, and legacy out-of-range purchases clamp to the caps. Runtime/default/fallback/Main settings agree in a read-only static parity check; git diff check is clean. Updated focused simulation/pointer fixtures are not executed: UnityMCP readiness fails, so compilation/import/tests/Game-view/device behavior remain unverified. No APK/install/editor restart/commit/push. Exact prices and acceptance are in features/street-automation.md.


## Unified top coin/sales/time HUD — implemented, engine validation pending (2026-10-04)

StreetView replaces old blue/cream corner panels with one original procedural amber/brown glossy bar: gold-only coin left, clock/countdown center, actual Delivered/Goal green capsule right. Bundled Luckiest Guy white outlined numbers auto-fit long values. All artwork and text remain logicaly2–36, above rear bubblesy38; lower Tiempo label removed, simulation rules/anchors and upgrade buttons untouched. Texture created once per enable/released on disable. Focused art fixtures cover HUD separation/bounds, six countdown cases and texture dimensions/disposal. Static source/geometry checks passed; Unity6000.6.3f1 MCP state ping unanswered, so compilation/tests/Game-view/device unverified. No APK/build/install/git.


## Recognizable real Floresta mural / All Boys corner — integrated, engine review pending (2026-10-04)

Original built-in ImageGen edits use new user photos to depict real mural composition (black bands/windows/player rows/team/buildings/justice portraits/FLORESTA POR JUSTICIA), plus corner AB blue oval, shield/monograms and sneaker memorial. Final940×1673RGB plate retained as Resources/street-mural-real-v5.png with fresh inherited meta. Both full-plate outputs moved counter/panel about80px, so only source wall(0,0,940,234) is rendered into logical(0,0,540,90) above retained v4 background; all lower scenery pixels/anchors and unified HUD/upgrades/economy untouched. Existing art/metas retained. PNG/hash/metas/resource/crop bounds/static diff checks pass; 2 focused asset/layer fixture cases added, not executed. Unity6000.6.3f1 MCP transport unavailable, so import/compilation/Game-view/device pending. No APK/git.


## Floresta location font + All Boys shield — integrated, Unity pending (2026-10-04)

StreetView replaces Ready/Playing location labels with centered uppercase Luckiest Guy/white-dark-outline text; untouched club-hosted320×320RGBA crest at left only for level0. Later names auto-fit without wrong club branding. Playing footery886–924, Ready884–910 clears team stats and ready level buttonsy912; speed/hire/HUD/mural/gameplay unchanged. Fresh inherited meta, source/hash/provenance retained. Five focused cases added (asset dimensions/import,3layout cases,crest/font load), not executed. Static file/meta/layout/resource/diff checks pass; Unity6000.6.3f1 MCP transport unavailable, so import/compilation/tests/Game-view/bunting/readability unverified. No APK/git.


## Current combined validation and phone delivery — 2026-10-04 (0.2.6/code8)

Latest user authorized tests, APK and phone installation. Installed Unity6000.6.3f1 was closed (no running editor/MCP); used the exact installed editor in sequential batch runs, without new packages or a second editor. Current Street **82/82 EditMode + 15/15 PlayMode passed**,0failed/0skipped. Scope includes200-handoff victory/180s deadline, FIFO/reservations, round-local coins and first-level reset, exact progressive prices/caps/MAX, mouse/touch dispatch/start/replay/load, intro timing/asset/viewport guards, current art/HUD/mural/footer imports and layout. Not an unrelated legacy full-suite run or mobile performance certification.

Initial batch pointer runs failed due to unfocused simulated input; swapping transient InputSettings also destroyed the previous instance. Final fixture only saves/restores backgroundBehavior and editorInputBehaviorInPlayMode enum values on the existing settings (IgnoreFocus/AllDeviceInputAlwaysGoesToGameView during tests). No runtime input code or persistent input settings changed. Failed attempts remain in ignored evidence; final15/15 is the successful rerun.

Main-only development Android IL2CPP/ARM64 build **Succeeded340.915261s,0errors/1warning** (Diagnostics Data debug-symbol recommendation). APK **0.2.6/code8**,150547329bytes,min26/target36,packagecom.haychoriypaty.game; V2signature/same debug certificate and original packaged adaptive-logo PNG bytes verified. SHA256`e2cc6576dfbe07e126c8c4f8493ae79640e33b878fd0c3cbd4cef2a825bbe6d5`. Archive`Builds/Android/Archive/HayChoriYPaty-0.2.6-code8.apk`; rolling`Builds/Android/HayChoriYPaty-street.apk` updated after verification; previous archives preserved. Temporary editor build helper/meta removed after saving ignored evidence.

Authorized Motorola Edge60Fusion (API36/1220×2712) replacement install returned **Success**; PackageManager confirms0.2.6/code8. Dedicated playerprefs XML byte-identical before/after install, firstInstallTime preserved; no uninstall/data clear. Own GameActivity cold launch **Statusok/733ms**. Reviewed two native gameplay captures during user play: unified gold-coin/m:ss/sales HUD goal200, recognizable mural/corner behind crowd, pan francés and no floating grill badge, current Parrillero and comic Floresta caption/original crest. The captures show different worker poses and a subsequent fresh first-level round withteam1/×1.00 and initial25/200costs; these are observations, not controlled native tap/reset/accounting tests. User was actively playing/retrying, so no forced restart/quit for additional review. Native gameplay letterboxing remains intentional; full-screen change applies to presentation only.

Own-PID log snapshot contains no matchedFATAL EXCEPTION/NullReferenceException/MissingReferenceException/IndexOutOfRangeException/AndroidJavaException; this is bounded evidence, not an all-session zero-error guarantee. Native intro timing/Salir/launcher masks, device FPS/thermal and broader cutouts remain unverified. Unselected upgrade-card/speed-icon proposals remain unintegrated (existing shoe/card still visible). Evidence stays ignored in`Logs/Acceptance/Current/`: final editmode.xml/playmode.xml, build.json/log, signature/badging, install/package/launch, preferences snapshots and phone-runtime.txt. Both phone-menu.png and phone-result.png actually contain gameplay, not menu/result acceptance. No Git commit/push in this turn.


## Diagonal Parrillero gait — 2026-10-04

Added a separate generated RGBA 4×2 walk atlas for front-left/front-right and rear-left/rear-right diagonal travel, with two alternating steps for each direction. StreetView chooses its pair from the actual worker movement vector at the existing8fps cadence; original poses, scale/foot anchor and pickup/handoff frames remain untouched. Source artwork provenance/hash and exact prompt are in `Assets/Art/Street/ParrilleroGenerationPrompts.md`; old atlas/GUID preserved.

- [x] Unity import/native source size/alpha/platform format and selector pair/phase regressions passed.
- [ ] Actual Game-view motion/foot anchoring reviewed.
- [ ] New APK installed; current installed0.2.6/code8 omits this change.

**Validation:** Unity6000.6.3f1 `StreetArtTests` **35/35 passed**,0failed/0skipped, including new diagonal source texture and selector tests (`Logs/Acceptance/DiagonalWalk/editmode.xml`); no compile/import errors in the batch log. No PlayMode/device screenshot or APK build/install was performed for this source refinement.


## Parrilla Criolla upgrade buttons — source-only update (2026-10-04)

The user selected the wood-and-riveted-iron upgrade card option and requested forward arrows instead of the Speed shoe, with room for four-digit prices. Added reusable transparent card art and amber forward-chevron art with fresh `.meta` files; StreetView uses the existing Parrillero portrait, gold coin, live title/price/+10% and existing purchase controls. Cards are 234×120 and keep independent touch rectangles; no cost/economy edits. Asset provenance and acceptance were recorded in `GenerationPrompts.md`, `visual-reference.md`, and `features/street-automation.md`.

**Validation:** User then explicitly said not to test until asked. A Unity EditMode command had already been launched and aborted during test-assembly compilation with CS1061 (`GUIStyle.Dispose` unavailable in this editor); no test case executed. Removed that unsupported call, but per user instruction did not rerun. Unity import/layout, pointer, Game-view/device, and price-fit assertions therefore remain unverified. No APK/build/install, Git commit or push.


## Used trestle choripán table — source-only update (2026-10-04)

Generated and added `Resources/street-serving-table-v1.png` (1774×887 transparent cartoon asset) and a fresh no-mipmap/alpha/Android-RGBA32 Unity meta. `StreetView` places the loaded pan-francés choripán table in the near-left foreground beside the grill; product0 station coordinate now sends the Parrillero to the right edge of the table, and overlapping product-name text is suppressed. Later product stations, grill product art, costs, and delivery accounting remain unchanged. Prompt, references, and SHA are in `Assets/Art/Street/GenerationPrompts.md`.

**Validation:** User's instruction not to test remains in force. No Unity import/compiler/test/Game view run, no APK/build/phone install. The sprite was inspected directly; in-game scale/overlap and pickup animation are not visually verified.


## Floresta upgrade-cost playability — calibrated in Unity simulation (2026-10-04)

The old level-one curve yielded 66 sales without upgrades and 82 sales with affordable speed purchases; no parrillero hire became affordable by the deadline. Added a separate Floresta-only curve: speed costs $5, $10, $15, $20, $30, $45, $65, $90, $125 and hires $15, $30, $60, $100. Later locations keep their established tables; +10% per speed tier and x1.9 cap are unchanged. A deterministic real-handoff simulation using only earned revenue purchased all upgrades and won 200/200 in 98.9/180 seconds ($390 left).

Validation: Unity6000.6.3f1 `StreetSimulationTests` **50/50 passed** and focused `UpgradeCardsUpdateNextCostsAndStopAtMaximum` PlayMode test **1/1 passed**. The purchase-driven deterministic simulation won200/200 in98.9s with$390 remaining. No Android build, APK, or install was requested or performed.


## Full-body cover Parrillero — source update (2026-10-04)

Generated Assets/Art/Street/Resources/street-cover-parrillero-full-v1.png (1024×1536 RGBA) from the established portrait and gameplay sprite-sheet references. New art shows hair-to-shoes full body; sampled face/arm base medians match (RGB 246/150/83 vs 246/150/84). Unity draws it in a left-side portrait slot and shifts the same title badge right to keep the figure visible beside, rather than behind, the logo; Jugar/Salir hit areas and timing stay unchanged. Original game/hire portrait and gameplay sprite sheet are preserved. Fresh .meta GUID with no mipmaps/Android RGBA32; source hash and prompt are in Assets/Art/Street/ParrilleroGenerationPrompts.md.

**Validation:** Generated PNG was visually inspected; source dimensions and alpha confirmed, and git diff --check passed. No Unity import/Game-view/test/APK was run for this image change, so in-game composition remains unverified.


## Android delivery — 2026-10-04 (0.2.7/code9)

Built current Main-only source with Unity6000.6.3f1 Development Android IL2CPP/ARM64. Build succeeded in257.10s with0errors/1 warning (`clang++: '-x c++' after last input file has no effect`). APK `Builds/Android/Archive/HayChoriYPaty-0.2.7-code9.apk` (136,481,224bytes), package `com.haychoriypaty.game`, min26/target36, version0.2.7/code9; V2 signature uses same local Android Debug certificate as0.2.6. SHA-256 `92691a99f7a0c02eaed6930eb541aacf9f1e6ea25d9e6e2dfdfa3c120388313a`. Rolling `Builds/Android/HayChoriYPaty-street.apk` matches.

Installed to authorized Motorola Edge60 Fusion (`ZY22MBNWRB`) with `adb install -r`: Success; package query reports0.2.7/code9; PlayerPrefs XML is byte-identical before/after (no uninstall/data clear). Launched via Android package launcher; process remained present. No test suite or gameplay/visual/device acceptance test was run in this delivery; build success is not gameplay validation. No commit or push.


## Edge-to-edge Floresta / clear table route — source update (2026-10-04)

Implemented full physical-screen scenery beneath safe-area gameplay (fixes exposed gray top inset), physical-y0 coin/time/sales HUD with alternate camera-clear layout, and retained original safe control/hit transforms. First-level table nowleft(16,540,196,98), grillright(242,550,282,94), pickup(205,585) via clear(205,510) on both outgoing/return travel. Customer rows324/268/212 expose more wall and put front supporters closer to counter. Later multi-product station geometry, existing art/metas and actual economy/target/duration remain unchanged. Added/updated deferred geometry fixtures.

No tests or APK/install by standing instruction. Source diff/static review passed; Unity6000.6.3f1 batch import/compilation succeeded (exit0, no compiler errors). No PlayMode/Game-view/native visual review was performed. Native framing, camera readability, table pickup/gait and changed-route playability remain unverified. Installed0.2.7 does not contain this follow-up.


## Selected grill-scene presentation — source update (2026-10-04)

User-selected full-bleed grill illustration edited with built-in image_gen using the game Parrillero portrait/atlas as facial references. Natural face-neck transition and warm skin continuity visually inspected; opaque1024×1536RGB asset `street-cover-grill-parrillero-v3.png` integrated with fresh meta. StreetView retains physical-screen proportional crop, removes the obsolete separate cover cutout overlay, and recenters unchanged logo/flash. Menu actions/4+4 timing/gameplay/economy untouched; all earlier art/metas preserved. Deferred startup fixture source updated, not run.

No tests, Play/Game-view/device review, APK/install or Git push. Native phone appearance remains unverified; installed0.2.7 does not contain this change.

Unity6000.6.3f1 batch asset import/compilation completed successfully (exit0, no compiler errors), log `Logs/Acceptance/SelectedGrillCover20261004/compile.log`. This is not test execution or visual/device acceptance. `git diff --check` passed.


## Cover face/body rendering correction — source update (2026-10-04)

User pointed out v3's face style visibly differed from its body. Preserved v3 and authored v4 with a painterly target-style face/head/neck retaining game identity; integrated via the single fullscreen cover resource. Art visually inspected only; no tests or APK. Device visual and user's acceptance pending.
Unity6000.6.3f1 batch import/source compilation succeeded for v4 (exit0, no compiler errors), log `Logs/Acceptance/SelectedGrillCoverFaceMatch20261004/compile.log`. `git diff --check` passed. Tests, Play/Game-view/native device review and APK were not run/built.


## Active-game timeout riot — source update (2026-10-04)

The earlier riot vignette existed only in legacy `PrototypeView`, so it was never shown by Main/`StreetView`. Added versioned `street-riot-defeat-v1` full-screen scene with unmistakably angry shouting supporters, raised sticks and visibly shattered serving counter/tipped condiment stand. On actual `RoundPhase.Lost` below the goal, `StreetView` suppresses the calm playfield and displays this riot scene, physical HUD, rage headline, final sales/goal and replay. Background rumbles and nine wood debris pieces move using `Time.unscaledTime`, independently of frozen simulation. Victory and gameplay simulation/balance unchanged.

Unity6000.6.3f1 focused PlayMode test `StreetPointerTests.ReplayReturnsToReadyWithoutStartingAutomaticallyAndDiscardsCoins`: **1/1 passed**,0failed/0skipped. It reaches a short real deadline loss, verifies active riot texture/state plus moving debris, then retries to Ready with round-local coins reset. Final filtered batch compile/test succeeded. First run caught CS0103 stale `won` references in the now win-only result method; after fixing, headless test showed Lost-state initialization was incorrectly coupled to OnGUI, now tracked in `LateUpdate`. `git diff --check` on changed paths is clean. No full-suite run, actual Game-view/device review, APK/install, version bump or push.


## User-selected startup cover — source update (2026-10-05)

Copied the user's 941×1672 cover image byte-for-byte to `street-cover-user-v5.png` (SHA-256 `e7bc3d59b722432c53da3eb63a6bfd3e18dd5ebd22ab607af097d4b52b791462`), created fresh Unity texture metadata, and switched `StreetView`'s intro resource to it. The original transparent logo, 4+4-second sequence, menu buttons, and gameplay artwork/logic remain unchanged.

Unity import/Game-view/device review and tests were not run; no APK was built or installed.


## Floresta supporter clothing alignment correction — 2026-10-05

Historical checkpoint: nine standalone shirt images were once stretched over the old base bodies and looked superimposed. That overlay was disabled, then replaced by the complete nine-outfit front/walking atlases described in the current entry above.

Unity batch import/compile completed and the focused EditMode case passed 1/1. Game-view/device visual review remains pending; no APK/device update.

## All Boys riot apparel consistency — source update (2026-10-05)

Added a dedicated transparent nine-outfit timeout atlas with paired raised-stick/swing poses. The riot renderer chooses outfits with the same customer-ID mapping as the ordinary Floresta crowd, so a supporter keeps the same integrated black/white/neutral clothing when the timeout sequence turns into a trifulca. The Nueva Chicago-specific riot atlas and fallback behavior remain unchanged.

**Validation:** Static source/asset metadata review only. No tests, Unity/Game view, APK, or device operations were run, per the user's standing instruction.

## Nueva Chicago riot wardrobe consistency — source update (2026-10-05)

Replaced Level 2's four-look riot sprites at runtime with a new versioned 4×4 transparent atlas: all eight complete Chicago supporter outfits now have raised-stick and swing poses. The same customer-ID variant drives normal and riot rendering, matching the All Boys rule. Recorded a general project requirement that every future team's waiting-crowd and trifulca sprites preserve the same team-specific outfit. The prior Chicago atlas is preserved.

**Validation:** Static source/asset metadata review only. No tests or Unity/Game-view/APK/device operations, per the user's standing no-test instruction.


## Cartoon impact smoke on the trifulca transition — source update (2026-10-05)

Added a large transparent comic dust-cloud sprite that quickly expands over the frozen waiting queue at the instant timeout switches the active scene into the live riot, pulses briefly, and fades as the broken-stall background and furious team-outfitted fans come into view. This is visual-only, driven by unscaled time, and leaves the HUD/results drawn above it.

**Validation:** Static code/spec/import-metadata review only. No tests, Unity/Game-view/device review, APK, or installation, per the user's standing instruction.


## Trifulca smoke APK installed — 2026-10-05

Built Main-only Development Android IL2CPP/ARM64 APK 0.2.12/code14 with the newly added timeout smoke transition. Unity build `build-059ad48e84` succeeded in 273.46s (0 errors, 2 warnings). V2 signature verified; archive SHA-256 `524b63abbfb4ea413ab597282b798f743221329c2f2667c224884662de5da926`. The archive and rolling APK match.

Installed on Motorola Edge 60 Fusion `ZY22MBNWRB` using `adb install -r`; PackageManager confirms 0.2.12/code14. No uninstall/data clear, test suite, app launch or gameplay visual review.
