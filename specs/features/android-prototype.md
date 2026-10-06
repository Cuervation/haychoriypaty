# Current Android delivery — 2026-10-05 (0.2.12/code14, trifulca smoke)

Unity6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-059ad48e84`: **Succeeded in 273.46s**, 0 errors / 2 warnings. The user requested the APK and phone installation; no tests or in-game gameplay review were requested or run.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.12-code14-trifulca-smoke.apk`: **186,968,662 bytes**, package `com.haychoriypaty.game`, version0.2.12/code14, min API26 / target36, `arm64-v8a`. SHA-256 `524b63abbfb4ea413ab597282b798f743221329c2f2667c224884662de5da926`. APK v2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. The rolling APK `Builds/Android/HayChoriYPaty-street.apk` matches byte-for-byte.

**Installed:** `adb install -r` onto the connected Motorola Edge 60 Fusion (`ZY22MBNWRB`) returned successfully. PackageManager reports versionCode14/versionName0.2.12. Replacement install only; no uninstall or data clear. App was not launched, and no visual/device gameplay check was performed.

---

# Current Android delivery — 2026-10-05 (0.2.11/code13, upgrade status ribbon)

Unity6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-2dccbdc803`: **Succeeded in 19.38s**, 0 errors / 1 warning. Tests were not run per the user's standing instruction.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.11-code13-upgrade-option1.apk`: **181,258,740 bytes**, package `com.haychoriypaty.game`, version0.2.11/code13, min API26 / target36, `arm64-v8a`. SHA-256 `7ff670d8c5999a4c980fd263433c57289a6468c09aeea1928da9aec5ffe4fbab`. APK V2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. No phone installation was requested or performed; the previous rolling APK remains unchanged.

## Previous Android delivery — 2026-10-05 (0.2.10/code12, Floresta mural and apparel)

Current Unity source built as a Main-only Development Android IL2CPP/ARM64 APK with Unity6000.6.3f1. Build report: **Succeeded in 246.68s**; the build log also contains a non-blocking Licensing access-token update diagnostic and the Diagnostics Data debug-symbol recommendation. The requested game tests were not run.

APK `Builds/Android/HayChoriYPaty-street.apk`; versioned archive `Builds/Android/Archive/HayChoriYPaty-0.2.10-code12-mural-wardrobe.apk`; **212,807,089 bytes**, package `com.haychoriypaty.game`, version0.2.10/code12, min API26 / target36, `arm64-v8a`. SHA-256 `639e11908265ff2b228afc2a40621f69a6b2d000f5dd1993f4aa5bb8244eb8a4`. APK V2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. `aapt` confirms package/version metadata; archive matches rolling APK byte-for-byte.

**Installed:** after the user reconnected the Motorola Edge 60 Fusion (serial `ZY22MBNWRB`), `adb install -r` returned **Success**. PackageManager confirms versionCode12/versionName0.2.10 on the phone. No tests were run; the app was not launched and data was not cleared or uninstalled.

## Previous Android delivery — 2026-10-05 (0.2.9/code11, Level 2 fix)

The complete Unity Editor test suite for the shipped source passed on Unity6000.6.3f1 immediately before this version-only build: **116/116 EditMode + 21/21 PlayMode**, 0 failures/skips. Development Android IL2CPP/ARM64 build **Succeeded in 240.64s**, 0 errors / 1 warning (Diagnostics Data recommends debug symbols for detailed crash stack traces). Main scene only; min API26 / target36; `arm64-v8a`.

APK **0.2.9/code11**, **171,936,606 bytes (~164 MiB)**, package `com.haychoriypaty.game`. V2 signature verified with the existing local Android Debug certificate (SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`). APK SHA-256 `be171c5d141d7de2fd4af2dab0bd8654d67b5c629c14ae3e299d525a9bdf08fb`.

Versioned archive: `Builds/Android/Archive/HayChoriYPaty-0.2.9-code11-level2.apk`; rolling APK: `Builds/Android/HayChoriYPaty-street.apk`. Installed on the connected Motorola Edge 60 Fusion using `adb install -r`; PackageManager confirms version 0.2.9/code11. PlayerPrefs matched byte-for-byte before/after and firstInstallTime was preserved; no uninstall or data clear. The app was not launched for native gameplay/visual review. Build log, signature/package verification, and preference comparison are in ignored `Logs/Acceptance/Level2Delivery-20261005/`.

## Previous Android delivery — 2026-10-05 (0.2.8/code10, full suite)

The complete Unity Editor test suite passed on Unity6000.6.3f1: **115/115 EditMode + 21/21 PlayMode**, 0 failures/skips. Development Android IL2CPP/ARM64 build **Succeeded**, 0 errors / 1 warning (Diagnostics Data recommends debug symbols for detailed crash stack traces). APK **0.2.8/code10**, **168,244,348 bytes**, package `com.haychoriypaty.game`, min API26 / target36, `arm64-v8a`. V2 signature verified with the existing local Android Debug certificate (SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`). APK SHA-256 `6600a5dc54d5c9f917ce2d33d8f2ee999f613ba87cebcd8ef02f44ea8371b240`.

Versioned archive: `Builds/Android/Archive/HayChoriYPaty-0.2.8-code10-fullsuite.apk`; APK was not installed in that delivery. Evidence is in ignored `Logs/Acceptance/FullSuite-20261005/`.

---

## Previous Android delivery — 2026-10-04 (0.2.7/code9, timeout riot)

Current Main source (including the timeout-riot presentation) built with Unity6000.6.3f1 as Development Android IL2CPP ARM64: **Succeeded**,255.41s,0errors/1 Clang warning (`'-x c++' after last input file has no effect`). APK archive `Builds/Android/Archive/HayChoriYPaty-0.2.7-code9-deadline-riot.apk`,157,471,790bytes; rolling artifact `Builds/Android/HayChoriYPaty-street.apk`; package `com.haychoriypaty.game`, version0.2.7/code9, minAPI26/target36, `arm64-v8a`. V2 signature verified; SHA-256 `0c9708a210939c256414ea3c059e64659f73de767482dcfbbe2101c2ffb25ded`; local Android Debug certificate unchanged (`d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`).

Installed on the authorized Motorola Edge60 Fusion (`ZY22MBNWRB`) using `adb install -r`: **Success**, PackageManager reports0.2.7/code9. PlayerPrefs XML matched byte-for-byte before/after; no uninstall or data clear. GameActivity launch succeeded and the app process remained live; at final inspection Android showed Rappi resumed over the game, so it was not forced back to the foreground. The focused deadline→riot→replay PlayMode test had passed1/1 before this delivery; no tests were rerun during the build/install, and no native gameplay/visual acceptance review was performed. Evidence (build log, APK metadata/signature and preference snapshots) is in ignored `Logs/Acceptance/DeadlineRiot20261004/`.

---

# Android delivery — current Street and historical prototype


## Current combined validation and phone delivery — 2026-10-04 (0.2.6/code8)

Latest user authorized tests, APK and phone installation. Installed Unity6000.6.3f1 was closed (no running editor/MCP); used the exact installed editor in sequential batch runs, without new packages or a second editor. Current Street **82/82 EditMode + 15/15 PlayMode passed**,0failed/0skipped. Scope includes200-handoff victory/180s deadline, FIFO/reservations, round-local coins and first-level reset, exact progressive prices/caps/MAX, mouse/touch dispatch/start/replay/load, intro timing/asset/viewport guards, current art/HUD/mural/footer imports and layout. Not an unrelated legacy full-suite run or mobile performance certification.

Initial batch pointer runs failed due to unfocused simulated input; swapping transient InputSettings also destroyed the previous instance. Final fixture only saves/restores backgroundBehavior and editorInputBehaviorInPlayMode enum values on the existing settings (IgnoreFocus/AllDeviceInputAlwaysGoesToGameView during tests). No runtime input code or persistent input settings changed. Failed attempts remain in ignored evidence; final15/15 is the successful rerun.

Main-only development Android IL2CPP/ARM64 build **Succeeded340.915261s,0errors/1warning** (Diagnostics Data debug-symbol recommendation). APK **0.2.6/code8**,150547329bytes,min26/target36,packagecom.haychoriypaty.game; V2signature/same debug certificate and original packaged adaptive-logo PNG bytes verified. SHA256`e2cc6576dfbe07e126c8c4f8493ae79640e33b878fd0c3cbd4cef2a825bbe6d5`. Archive`Builds/Android/Archive/HayChoriYPaty-0.2.6-code8.apk`; rolling`Builds/Android/HayChoriYPaty-street.apk` updated after verification; previous archives preserved. Temporary editor build helper/meta removed after saving ignored evidence.

Authorized Motorola Edge60Fusion (API36/1220×2712) replacement install returned **Success**; PackageManager confirms0.2.6/code8. Dedicated playerprefs XML byte-identical before/after install, firstInstallTime preserved; no uninstall/data clear. Own GameActivity cold launch **Statusok/733ms**. Reviewed two native gameplay captures during user play: unified gold-coin/m:ss/sales HUD goal200, recognizable mural/corner behind crowd, pan francés and no floating grill badge, current Parrillero and comic Floresta caption/original crest. The captures show different worker poses and a subsequent fresh first-level round withteam1/×1.00 and initial25/200costs; these are observations, not controlled native tap/reset/accounting tests. User was actively playing/retrying, so no forced restart/quit for additional review. Native gameplay letterboxing remains intentional; full-screen change applies to presentation only.

Own-PID log snapshot contains no matchedFATAL EXCEPTION/NullReferenceException/MissingReferenceException/IndexOutOfRangeException/AndroidJavaException; this is bounded evidence, not an all-session zero-error guarantee. Native intro timing/Salir/launcher masks, device FPS/thermal and broader cutouts remain unverified. Unselected upgrade-card/speed-icon proposals remain unintegrated (existing shoe/card still visible). Evidence stays ignored in`Logs/Acceptance/Current/`: final editmode.xml/playmode.xml, build.json/log, signature/badging, install/package/launch, preferences snapshots and phone-runtime.txt. Both phone-menu.png and phone-result.png actually contain gameplay, not menu/result acceptance. No Git commit/push in this turn.


## Presentation-logo app icon — source update (2026-10-04)

Use the same selected `Assets/Art/Street/Resources/street-logo.png` as the application icon, without redrawing its text/art or modifying the presentation asset/meta. Assign its texture to the Unity default icon and all Android single-layer legacy/round slots. On future Android generation, the Editor-only StreetAppIcon callback copies the exact source PNG to launcher drawable-nodpi and supplies a cream background plus an XML-inset adaptive foreground (20% horizontal/20.92% vertical), preserving the full badge/aspect for varied launcher masks. No new package or raster derivative; source PNG stays authoritative.

- [x] Default/Android single-layer icon references and adaptive build-source composition implemented.
- [ ] Next authorized build/install confirms the logo replaces the Unity icon, stays readable/unclipped under launcher masks and keeps presentation unchanged.

**Validation:** Source review only. No tests, Play, APK/export generation or device changes by standing user instruction. Unity MCP state ping did not respond; import/compilation and callback execution remain unverified. Installed0.2.4 retains its previous icon until a future authorized update; historical build results below do not validate this source change.


## Historical Street0.2.5 — combined tested APK (2026-10-04)

- Latest user authorized tests/build/phone update. Existing Unity6000.6.3f1/Android/Main: relevant EditMode52completed/succeeded/no reported failures; PlayMode12/12passed,0failed/0skipped. Regression scope and jobs are in[street automation](street-automation.md#combined-validation--2026-10-04-current-accumulated-implementation). Actual menu,21-fan crowd, paired idle animation frames and seven-product Ready/Playing captures reviewed in ignored`Logs/Acceptance/Combined`; exact editor progress restored and Play stopped.
- Build`build-47e1c59fff`: **Succeeded**,277.796s,**0errors/3warnings**. Main-only Android development/IL2CPP ARM64. Warnings: Diagnostics Data recommends fuller debug symbols; obsolete Unity splash PVRTC plus uncompressed-logo fallback. These are recorded, not hidden errors or zero-warning claims. No package/toolchain install or second editor.
- APK`Builds/Android/HayChoriYPaty-street.apk`, archive`Builds/Android/Archive/HayChoriYPaty-0.2.5-code7.apk`: **114,272,726bytes (108.98MiB)**, package`com.haychoriypaty.game`,0.2.5/code7,minAPI26/targetAPI36,`arm64-v8a`,GameActivity. BuildReport998.76MiB includes separate debug outputs, not download size.
- SHA256:`99d78efd142a624e612259737664ab013a0667e41d8319a62e6733b3d296403c`. V2signature verified; same local Android Debug certificate as0.2.4 (SHA256`d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`), suitable for non-destructive`install -r`; not Play Store release signing.
- StreetAppIcon callback executed: generated adaptive icon/foreground/cream background XML are present, manifest application icon references`@mipmap/app_icon`, packaged`res/drawable-nodpi-v4/hay_chori_paty_logo.png` exactly matches original presentation PNG bytes/SHA`bdce6ed77abbcfca57bcb9676e887b70044da4c7d38626d0d726a586733fd247`. Actual phone-launcher mask rendering still pending.
- User reconnected Motorola Edge60Fusion`ZY22MBNWRB` and requested installation. Serial-targeted`adb install -r` returned **Success**; PackageManager confirms **0.2.5/code7**. Dedicated Unity playerprefs XML byte-identical before/after update, firstInstallTime2026-10-03 18:49:24 retained: no wipe/uninstall. GameActivity cold launch **Statusok**,777ms,PID15213 alive immediately after launch. Evidence in ignored`Logs/Acceptance/Combined/{summary.json,phone-prefs-before-install.xml,phone-prefs-after-install.xml}`. USB disconnected again before post-launch runtime log query; waiting query cancelled, no healthy-log/screenshot/native-touch/Salir/launcher-mask/performance claim. App installation and launch are complete; remaining functional/device review is separate.

## Historical Street0.2.4 — selected cover/logo intro (2026-10-04)

- Supplied cover/logo assets, 4.1-second unscaled reveal with one warm flash, native/desktop action gating until Ready; includes the earlier mural, 15 fan-clothing overlays and v1→v2 opening staff/speed migration. No new game mechanics, packages or scene changes.
- Focused art EditMode 11/11 passed; affected real-editor pointer PlayMode 12/12 passed (job `adfc5a956d4f48548c73f34aa2caec0d`). Earlier batch pointer failures are recorded in [status](../status.md), not counted as passing checks. Actual 1080×1920 Game-view intro and representative wardrobe captures reviewed; editor progress restored exactly and Main remains clean/stopped.
- Existing-editor MCP build `build-1970fbd5d5` succeeded 364.73s (actual BuildReport 364.70s), 0 errors / 3 warnings: diagnostic-symbol recommendation and two Unity-splash PVRTC/fallback notices. Main only; preserved Main/SampleScene Build Settings entries. No simultaneous editor.
- Rolling local artifact `Builds/Android/HayChoriYPaty-street.apk`: **106,121,029 bytes (101.20 MiB)**, package `com.haychoriypaty.game`, version **0.2.4/code6**, minimum API26 / target36, ARM64 IL2CPP, V2 signature verified. Debug/local build, not Play Store release signing. Historical hashes below do not describe this replaced file.
- SHA256: `50e7c191f7818bd6dae8c44e20c4007723d48057500a30c45266fb4508ed7d75`.
- Unity was safely restarted under user authorization; live MCP state/project/scene/hierarchy/console queries now work. Editor left open on clean Main, Play stopped.
- User-requested physical Motorola Edge60Fusion delivery: serial-targeted `adb install -r` returned **Success**; installed **0.2.4/code6** verified by PackageManager. GameActivity cold launch **Status ok**, 769ms; app process remained alive. No uninstall/data clear, rebuild or emulator. Foreground changed to another app before screen capture, so new native appearance/touch/performance remain unverified.

## Historical Street0.2.2 — Parrillero poses (2026-10-04)

- Original16-pose atlas and selected fuller-face/skin-matched portrait, visible Parrillero name.3/3focused art EditMode checks and1/1hire/speed PlayMode regression passed. No save/gameplay schema change.
- Real existing-editor MCP build build-69c0e0ea94 succeeded210.41s,0errors/3warnings (diagnostic-symbol recommendation and two Unity-splashPVRTC/fallback notices). APK67,471,601bytes (64.35MiB), package com.haychoriypaty.game0.2.2/code4,min26/target36/ARM64,V2signature verified. SHA256121d1b815a5dbc6976eeeb9256ce9c37f6167a1b2a5f75c766946bb7903fca0b.
- Rolling artifact Builds/Android/HayChoriYPaty-street.apk now contains this build; historical hashes below describe earlier files. Phone install/visual verification pending: authorized Motorola briefly reappeared then USB/adb absent; one existingADB restart did not restore it. No new components/data wipe.

## Historical Street0.2.1 — three-minute trial

`Builds/Android/HayChoriYPaty-street.apk` is a rolling local artifact: historical0.2.1/code3 built/installed; native past-goal continuation verified, complete deadline check pending. Historical hashes/results below describe0.2.0, not the current file after replacement. No package/toolchain/input change; install-r preserves the versioned save.

- Build `build-a71ad1b4b7`:Succeeded75.02s,0errors/4warnings (diagnostic symbols, uncompiled postprocessing notice and two splashPVRTC/fallback notices). APK70,044,678bytes (66.80MiB),min26/target36/ARM64,V2 signature verified. SHA256 `2ac35039d858cd4b0aa4b454eaa2737789e2c10840141f54e22c50e6056df477`.
- Real native install-rSuccess/launchStatusok; Ready preserved672coins/7staff/rate3.25. One Start tap, no purchases:60units/$972 (=672+60×5) with172s remaining and no result panel, proving goal24 no longer finishes Floresta early. Evidence ignored `Logs/Acceptance/ThreeMinuteTrial/`. Full deadline/result/save check pending; emulator is functional QA, not phone performance. Endpoint probe stillPlaying958sales/56s left: slow emulator wall time does not equal simulated time; no native deadline victory claimed.
- User then chose the connected physical Motorola Edge60Fusion. USB-target install-rSuccess and GameActivity launchStatusok/cold819ms; installed0.2.1/code3 verified. USB disconnected before foreground/screenshot query, so physical visual/touch/performance checks remain unverified. No data wipe; emulator no longer running.

## Historical Street0.2.0 — verified APK/emulator

Current gameplay authority: [street automation](street-automation.md). Same existing toolchain/AVD/input ownership as below; no dependencies or second Unity installed/launched.

- Build MCP final `build-9d977619d5` succeeded105.11s (first build `build-f16e1134d4`433.10s), **0 errors/52 warnings**. Forty-eight repeated SDK remote manifest/source-list connection warnings plus four diagnostic/postprocessing/splashPVRTC notices. Existing localSDK was sufficient; no downloads/install workaround. Actual reflected BuildReport and Unity LogEntries counts agree0errors/52warnings; MCPv10 read_console mislabels warnings as errors with this editor, so use actual counts/report before diagnosis.
- APK `Builds/Android/HayChoriYPaty-street.apk`: **84,269,076 bytes (80.37MiB)**, version0.2.0/code2, `com.haychoriypaty.game`, min26/target36, ARM64, V2 signature verified; debug/local build, not release signing. SHA256 `c1254fecc2ae01d1d32fbf03811c740547395fab2c52a25f4fc3fc6db78b4725`.
- Existing emulator-5554 install-r returned Success and GameActivity launch Statusok. Native Android drag reached0 and60, tap restored5, Start initiated the actual new game. Live crowd21 has999-unit orders and shows one-unit decrements; workers carry original chori sprite.
- Exactly one Speed5 tap and one Cook15 tap gave17sales/$65/team2/rate1.25/nextcosts10 and30:17×5−20=65. Actual round then won **24 units/$100**,24×5−20=100. No duplicate charges.
- Street editor validation16EditMode/9PlayMode is recorded in [status](../status.md); it is distinct from these real native Android interactions. Evidence stays ignored `Logs/Acceptance/StreetAndroid/`.
- Native Next unlockedNuevaChicago/3products/goal40. SeparatePaty11/Chori5 saved on pause; forced own-app restart restored100coins/2staff/speed1/unlocked1. Price tabs initially coveredHUD; moved to panel under title and9/9 regression passed; final APK rebuilt/installed; native product selection and visible/unobstructed HUD confirmed.
- Process log0 FATAL EXCEPTION/NullReference/MissingReference/IndexOutOfRange; optionalAssetPackManager/EGL/URP warnings exist, not a zero-error log.
- Physical phone absent in current adb inventory; earlier installation below was the old0.1.0 APK, not this update. Physical touch/FPS/thermal/other cutouts still unverified. Later-club scenery, audio and final polish pending.

## Historical0.1.0 scope
Deliver the existing two-product portrait prototype as a locally installable Android APK and run it in the existing visible emulator. Preserve gameplay, 2D cartoon reference, package dependencies, scene/meta identities and seven-product boundary. No Play Store publication or release signing.

## Implementation
- Unity 6000.6.3f1, bundled SDK/JDK/NDK, IL2CPP ARM64, minimum API26, application `com.haychoriypaty.game`.
- Build Main only via existing Unity MCP editor; do not launch a second editor or overwrite the preserved SampleScene build entry.
- Exactly one action owner per platform: native GameActivity IMGUI on Android player (no PollPointer), Input System bridge in Input-System-only desktop/editor (GUI render-only).
- Fit the 540×960 view within Screen.safeArea; share that transform with primary-touch hit testing, preserve portrait letterboxing.
- Reuse Asadito_Pixel_7a_API_36 (Google APIs x86_64 + libndk ARM64 translation), visible SwiftShader. No duplicated AVD, data wipe or install of new SDK components.

## Acceptance
- [x] Android build succeeds; APK signature, package, ABI and minimum SDK verified.
- [x] Safe-area geometry tests and affected pointer tests pass.
- [x] Existing emulator boots visibly; package installs and launches without fatal Unity/runtime errors.
- [x] Real emulator taps start a turn, buy staff/speed with earned coins, complete a turn and replay.
- [x] Reviewed emulator screenshots show reference-consistent portrait UI; buttons remain unobstructed.
- [x] Emulator remains running with game visible; report evidence and limits.

## Limits
Emulator ARM translation/software rendering is functional QA, not physical-phone touch, thermal/FPS certification or Unity-supported-device certification. Final art/audio/balancing and Play Store delivery remain separate.

## Evidence — 2026-10-03
- AndroidViewport EditMode **4/4 passed**, job `059678e9abc54279826b75b0d50813c8`; reference portrait, asymmetric cutout/navigation, side insets, invalid/zero dimensions.
- Affected FlorestaPointer PlayMode **3/3 passed**, job `04791fa77ba84246ba490660a24d7414`; actual queued mouse/touch start/hire/upgrade/replay and outside/drag rejection. These editor checks are not emulator/device inputs.

- First emulator run exposed duplicate speed purchase: served9/coins100 reflected two $24 charges after one upgrade tap plus hire. A bridge-only Android attempt did not start with the same adb tap. Use the already verified native GameActivity IMGUI path as sole Android owner; desktop/editor retains the bridge. Regression and final rebuilt APK validation completed below.

### Final delivery
- Final build `build-e95274f9a4`: **Succeeded**, 41.97 seconds, 0 errors / 4 warnings. APK **69,595,590 bytes (66.37 MiB)**; BuildReport769.82MiB includes separate debug outputs, not download size.
- APK V2 signature verified; package `com.haychoriypaty.game`, version0.1.0/code1, minAPI26/targetAPI36, `arm64-v8a`, GameActivity. Debug/local prototype, not Play Store release signing.
- SHA256: `94ae293c617e4dbcbbd3afef81b05383f3d99e6c111803e1f3cd5be567028fbc`.
- Final affected pointer suite **4/4 passed**, job `f82f79af68344b51b834d784a8d75c21`, including duplicate-GUI notification regression in editor; earlier geometry **4/4 passed**. Eight distinct targeted editor cases, not Android player tests.
- Reused visible AVD `Asadito_Pixel_7a_API_36`, adb `emulator-5554`, 1080×2400, API36, `libndk_translation.so`, SwiftShader. Real install-r/am-start succeeded. No new SDK packages/AVD or user-data wipe.
- Native adb taps started/replayed rounds, hired one seller and bought exactly one speed level. Final purchase screenshot: served4 / coins34 =16+4×18−30−24, two sellers. Final result: **12 delivered,0 departed,178 coins** =16+12×18−30−24. This confirms no duplicate charge.
- Inspected portrait UI/letterboxing on native1080×2400: top/cutout and bottom controls remain unobstructed. Screenshots/log evidence retained only in ignored `Logs/Acceptance/Android/{ready,purchases,victory}.png` and `runtime-logcat.txt`.
- Unity remains stopped on clean Main, Android active; preserved Main/SampleScene entries. Emulator left running with the game in the foreground on its Ready screen, without a timer running until the user starts.

### Observed limitations, not hidden passes
- Final build warnings: diagnostic symbol recommendation, editor uncompiled-postprocessing notice, obsolete PVRTC Unity splash logo and uncompressed splash fallback. The generated player IL2CPP code contains the current native-only Android input path; actual APK interactions verified it. No postprocessor code changed in this task.
- Final gameplay process log:0 `FATAL EXCEPTION` and0 observed NullReference/MissingReference/IndexOutOfRange exceptions. **Not a zero-error log**: one engine `AssetPackManager` ClassNotFound diagnostic (standalone APK uses bundled resources),4 emulator EGL capability/fallback errors, URP unsupported-cookie/postprocess notices. These did not prevent the recorded rounds. No Play Asset Delivery dependency added merely to suppress an optional-engine diagnostic.
- Cold boot initially showed a System UI ANR; Wait let startup finish. SwiftShader/ARM translation is slow and simulation can lag wall time; do not infer phone FPS from this AVD. Physical phone touch, other cutouts, pause/resume stress and thermal/FPS remain unverified.
- Installed Unity reflection marks X86_64 no longer supported; use ARM64 translation, not a forced obsolete ABI ([Unity removal notice](https://discussions.unity.com/t/platform-support-update-upcoming-magic-leap-x86-64-build-target-removal-in-unity-6-5/1706286)). Unity does not certify Android emulators as supported player platforms ([system requirements](https://docs.unity.com/en-us/engine/6000.5/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements)).

### Reproduce historical0.1.0 narrowly
Use the existing connected MCP editor: `manage_build` build/android, scenes `["Assets/Scenes/Main.unity"]`, development true, output `Builds/Android/HayChoriYPaty-prototype.apk`; poll its job. Do not start another Unity. For the already-installed bundled SDK, run the existing AVD visibly with `-gpu swiftshader_indirect -no-snapshot-load`; prefer a persistent exec/PTY session (a detached nohup launch did not survive this tool session). Then adb install-r and start `com.haychoriypaty.game/com.unity3d.player.UnityPlayerGameActivity`. Reuse an already-online emulator; never wipe it. Full suite is unnecessary for view/config-only changes.

### Physical phone installation — 2026-10-03
- User requested installation on the connected Motorola Edge60Fusion: adb reported AndroidAPI36 / arm64-v8a; explicit USB-target install-r returned Success and GameActivity launch Statusok (cold647ms). Existing data preserved; no uninstall or device settings changed.
- Phone disconnected before the subsequent process/foreground/screenshot query, so no physical-screen review, touch round or FPS validation claimed. Emulator evidence above remains distinct.

## Large parrilla and first-level economy — 2026-10-04 (build only)

User explicitly requests APK but no phone connection/install/launch. Main first level now fixed$5/no price slider, speed+10base percentage points for fixed$5, helperfixed$25 and1–4orders with completion/exit/replacement;180s preserved. Original broad shared iron-grill sprite with many chorizos/embers/bread; earlier native screenshots/results remain historical, not validation of this version.

- Real MCP build`build-4df2bd9cd6`:Succeeded363.106656s,0errors/3warnings; actualBuildReport agrees. Warnings only: diagnostics need symbols, obsolete Unity-splashPVRTC and uncompressed splash fallback.
- Ignored APK`Builds/Android/HayChoriYPaty-street.apk`:0.2.3/code5,57,681,379bytes; signatureV2 verified, packagecom.haychoriypaty.game,ARM64IL2CPP,min26,target36; SHA256`af50ac8961483c875191fa5ad4540484454ddf8037bb3e1e5e3ad2fcd471db90`.
- Targeted41/41EditMode (32simulation+9art) and11/11PlayMode passed; real1080x1920editor Ready/Playing/seven-product screenshots reviewed. Exact editor progress restored,Play stopped.
- No adb calls/emulator/phone install/launch performed. New native visual/performance certification not claimed. Existing generated performance-test outputs/folder meta ignored, all authored art/metas retained.


## Gameplay viewport follow-up — not yet built/installed (2026-10-04)

Latest source removes solid gameplay fit-letterboxes using a tall aspect-responsive logical canvas; targeted StreetArt33/33 and StreetPointer17/17 passed. Existing Motorola0.2.6/code8 predates this change. No Android export or installation performed for this follow-up; do not equate earlier gameplay screenshot with full-screen acceptance. Native layout/touch review remains pending; an install will interrupt any unsaved active round.
