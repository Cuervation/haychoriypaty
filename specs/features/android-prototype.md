# Entrega Android — 2026-10-24: Vacío 2×2, cortes enteros y stock ilimitado 0.2.66/code68

APK construida desde el commit runtime b1c1426 (solo Android versionada a0.2.66/code68). Incluye cuatro Vacíos anchos en la mitad derecha de la parrilla Premium, seis Bondiolas enteras a la izquierda y sus cocciones; Coca/cerveza inagotables aunque Mercado no tenga unidades; e íconos completos y sin recorte para panes de Mercado. Unity6000.6.3f1, copia aislada, Main-only Development IL2CPP/ARM64: Succeeded, 511.91s, 0errores/1warning.

APK /private/tmp/HayChoriYPaty-0.2.66-code68-vacio-four.apk: 384.458.497bytes, SHA256 9ff3f511f948717b51983334ac51a0085bb3e58b6f8aee6d9323ac636afa81c9. Paquete com.haychoriypaty.game, versión0.2.66/code68, min26/target36, solo arm64-v8a. aapt, ZIP, zipalign, firma APKv2 y parser directo de Main/level0 (StreetGame, StreetView y StreetKitchenRenderer activos y válidos) PASS. Android Debug signer existente; no es firma de Play Store. Firebase Analytics desactivado y Google Analytics Ad ID desactivado. Recibo/logs en /private/tmp/hcyp-android-delivery-0.2.66-code68/.

Validación Unity focalizada: WholeBondiolaArtTests36/36; StreetKitchenProductionTests11/11; StreetSceneBuildGuardTests3/3; StreetArtTests.Market*21/21; GrillMeatVisualBoundsStayInsideTheGrillForEveryLevel1/1; TableAndGrillSlotsDoNotMixTheTwoMeatFamilies1/1; PlayMode WholePremiumCutsFillBothHalvesAndUseRealCookingStates1/1 (Game View1080×1920) y ActiveGrillsStayVisuallyFullWithoutCreatingFoodAndRealCookOwnsItsSlot1/1. El fixture de stock actualizado ActiveCookSandwichesNeverReserveOrConsumeMarketIngredients1/1.

Límite separado: StreetMarketIntegrationTests.ActualHandoffsAndAdaptivePurchasesFundAllElevenFirstUnlockTransitions sigue FAIL al perderse la ronda de nivel6 (esperaba Won, obtuvo Lost); sus aserciones de contabilidad de ingredientes sí llegan a pasar antes de esa aserción. Se conserva como problema conocido de balance; no se alteraron tiempos, metas, stock de Mercado ni economía. La búsqueda amplia inicial Market también había incluido la expectativa vieja de consumo de sandwich/bebidas; se alinearon los tests y el contrato al comportamiento autorizado, sin cambiar runtime para forzar una victoria de campaña.

Source commits b1c1426 and a1486d5 pushed to origin/codex/street-automation; APK binary remains local and is not a Git blob. Instalación y QA físico: NOT_RUN. No se afirma validación completa de campaña, Firebase remoto ni compatibilidad física.

---

# Entrega Android — 2026-10-23: insignias 0.2.65/code67

Incluye21insignias de ventas500/2500/5000 para los7productos, con contadores y flags obtenidos persistidos en el snapshotFirebase porUID. Fuente197c3a3,67archivos runtime/escena comparados sin diferencias; trabajo local ajeno excluido/preservado. Se reutiliza evidencia sin cambios:32/32EditMode+1/1PlayMode y captura real1080×1920 con datosQA, ya revisados independientemente. Firebase remoto real: **NOT_RUN**, no confundir transporte snapshot probado localmente con roundtrip autenticado real.

Unity6000.6.3f1, Main-only Development IL2CPP/ARM64, copia aislada con ruta constante para preservar bindings. **Build Succeeded,447,19s,0errores/1warning**. APK `/private/tmp/HayChoriYPaty-0.2.65-code67-insignias.apk`, **354.357.269bytes**, SHA-256 `98ca84f89eeb6e12635c6ad034234e907309642f7b3f9d1172c232e9b11170b0`. Paquete `com.haychoriypaty.game`, min26/target36. ZIP,zipalign,firmaAPKv2 AndroidDebug existente,metadatos,Analytics/ad-IDoff y parser directo de Main **PASS** (StreetGame/View/KitchenRenderer activos y resueltos). Revisión independiente PASS. No es firma releasePlayStore y **no se instaló**; la0.2.64/code66 instalada se conserva. APK/logs fuera de Git, recibos en `/private/tmp/hcyp-sales-badges-0.2.65-code67/`.

---

# Entrega Android — 2026-10-23: hub 0.2.64/code66

Portada automática con barra compacta, hub de partido, Insignias legibles y nivel6 de260s. Fuente runtime/escena del commit7d44f62(65archivos comparados sin diferencias), sin incluir trabajo ajeno de gameplay. Unity6000.6.3f1, copia aislada, Main-only Development IL2CPP/ARM64. **Build Succeeded,257,59s,0errores/1warning**. Pruebas enfocadas finales **20/20EditMode +10/10PlayMode** y capturas reales1080×1920 revisadas independientemente. La corrida amplia previa no está verde:173/184, con una expectativa de import corregida y10fallas ajenas de rutas/plantilla/balance; no se alteró el balance para hacerlas pasar.

APK `/private/tmp/HayChoriYPaty-0.2.64-code66-hub.apk`, **370.871.429bytes**, SHA-256 `0e5b6ad72fa135e6f06d0e74e43c9f6f71c0027d0a9ad27d471596fe7a591fe3`; package `com.haychoriypaty.game`, min26/target36. ZIP,zipalign,firmaAPKv2(debug signer existente),flags Analytics/ad-ID off y parser directo de bindings de Main: **PASS** (StreetGame/StreetView/renderer activos y resueltos). Instalación y GoogleAuth online: **NOT_RUN**. No contiene una clasificación global real. Evidencia en `/private/tmp/hcyp-hub-0.2.64/final/`; ver [home-hub.md](home-hub.md). APK fuera de Git. El usuario autorizó explícitamente el remoto de origin; no se desactivaron controles de seguridad.

---

# Current Android delivery — 2026-10-23 (0.2.62/code64, Chulengo + handoff updates)

Generated from an isolated copy with Unity 6000.6.3f1; Main-only Development IL2CPP/ARM64. `BuildPipeline` **Succeeded in 89.67 s; 0 errors / 6 warnings**. The final build includes the current Chulengo parchment extension that covers the legacy green CTA edge. Package `com.haychoriypaty.game`, version **0.2.62/code64**, min API26/target36, ARM64.

APK `/private/tmp/HayChoriYPaty-0.2.62-code64.apk`: **345,505,605 bytes (329.50 MiB)**, SHA-256 `74b9700a5693cf5e228e534adbdf5443842b614eb45f402b1a3b0493bc9271e8`. `aapt` metadata, ZIP integrity, `zipalign -c -v 4`, APK Signature Scheme v2, Firebase Analytics deactivation, and Google Analytics ad-ID disabled were verified. Signer: Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused validation before packaging: **40 EditMode + 4 PlayMode** cases for Chulengo, handoff/Mozo and sandwich-table compaction, plus **12/12 Chulengo EditMode** regressions in the isolated copy. The final parchment-extent adjustment was compiled in the APK build; those tests were not rerun for this packaging pass. No physical-device install/QA was done. Chulengo resemblance ≥95% remains **uncertified**; no complete screenshot comparison was claimed. Development/debug-signed, not Play Store release-signed. The APK remains in `/private/tmp` and is not committed to Git. Build log/report: `/private/tmp/hcyp-apk-release-0.2.62-code64-20261023/`.

---

# Current Android delivery — 2026-10-23 (0.2.61/code63, Chulengo club-colored pennants)

Generated in a copy-on-write isolated project with Unity 6000.6.3f1; build scene `Assets/Scenes/Main.unity` only, Development IL2CPP/ARM64. `BuildPipeline` result **Succeeded, 0 errors / 1 warning, 448.59 s** (Unity Diagnostics Data recommends debug symbols). The APK includes the current Chulengo flag recoloring and popup/layout edits plus the current gameplay checkout; local Editor run-in-background/time-scale and Firebase Analytics settings, generated test scenes, screenshots/recovery data, and balance-audit helpers were omitted from the staging copy. Android metadata: package `com.haychoriypaty.game`, **0.2.61/code63**, min API26/target36, ARM64.

APK `/private/tmp/HayChoriYPaty-0.2.61-code63.apk`: **345,506,161 bytes (329.50 MiB)**, SHA-256 `31e2f830b814dfd7399d8ba821bd4e3be81035a17632b311af9abbc859b938ec`. `aapt` metadata, ZIP integrity, `zipalign -c -v 4`, APK v2 signature, and Firebase Analytics-deactivated / Google Analytics ad-ID-disabled manifest flags passed. Signer: existing Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused Chulengo EditMode checks in the isolated copy: **12/12 passed** after correcting the test's reflection unboxing from boxed `Color` to `Color32`. No full Game View/reference-image similarity review or physical-device QA was run; the requested 95% visual match is **not certified**. Development/debug-signed (not Play Store release-signed), not installed. Build log and test results: `/private/tmp/hcyp-apk-release-0.2.61-code63-20261023/`.

---

# Current Android delivery — 2026-10-22 (0.2.60/code62, Chulengo + Racing $200 Fernetero + table-slot fixes)

Built from an isolated copy with Unity 6000.6.3f1; Main-only Development IL2CPP/ARM64. The live Unity Editor was left untouched. Build **succeeded in 601.17 s; 0 errors / 2 warnings**. Racing Fernetero's first hire is $200; later hires retain the $500/$1,000/$1,800/$2,800 progression. This build includes the revised Chulengo layout, exact customer-column final handoffs, and active-sandwich table-cell release/compaction/reflow.

APK `/private/tmp/HayChoriYPaty-0.2.60-code62.apk`: **345,499,705 bytes (329.49 MiB)**, package `com.haychoriypaty.game`, version **0.2.60/code62**, min API26/target36, ARM64. SHA-256 `5f866d282f501d3a4cb14a1dbfd3e9a3a9e351145d7556a12abc765d7ce756fb`. `aapt` package/version metadata, ZIP integrity, `zipalign -c -v 4`, APK Signature Scheme v2 verification, and the manifest's Firebase Analytics-off / Google Analytics ad-ID-off metadata all passed; signer is the established Android Debug certificate `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused Unity 6000.6.3f1 validation in the isolated copy: **35/35 EditMode + 19/19 PlayMode**. Tests cover table compaction/claims/return, customer-column handoffs and the affected Chulengo, Racing-cost and renderer behavior. No full suite, live Game View/reference similarity score, or physical-device QA was performed; the requested ≥90% Chulengo visual match is still unverified. Development/debug-signed, not Play Store release-signed; not installed. The APK remains a local `/private/tmp` artifact and is not a Git blob. Build log: `/private/tmp/hcyp-apk-release-0.2.60-code62-20261022/build.log`.

---

# Previous Android delivery — 2026-10-21 (0.2.59/code61, Chulengo + Racing Fernetero)

Includes the current Chulengo victory-reward/multiplier UI and Racing Fernetero starting price of $300. Built through the already-open Unity 6000.6.3f1 Editor, Main-only Development IL2CPP/ARM64; the live Editor was left open. Build **succeeded in 408.29 s; 0 errors / 2 warnings** (Diagnostics Data recommends debug symbols; Unity also reported uncompiled post-processing/import changes). Firebase analytics collection is explicitly deactivated in the APK manifest and Google Analytics ad-ID collection is disabled. The local Editor runInBackground/time-scale values were restored afterward.

APK `/private/tmp/HayChoriYPaty-0.2.59-code61.apk`: **344,988,019 bytes (329.01 MiB)**, package `com.haychoriypaty.game`, version **0.2.59/code61**, min API26/target36, ARM64. SHA-256 `391a607817f7a97fa9c20bb6b2bd211c43ee576dbb0b5128787513bd57a83755`. `aapt` metadata, ZIP integrity, `zipalign -c -v 4`, and APK Signature Scheme v2 verification passed; signer is the established Android Debug certificate `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused EditMode validation passed **8/8**: seven Chulengo regressions and Racing Fernetero starting-price/curve. No PlayMode/Game View or physical-device QA was run; APK was not installed. The APK remains a local `/private/tmp` artifact and was not committed.

---

# Previous Android delivery — 2026-10-21 (0.2.58/code60, Chulengo + Racing Premium)

Includes the new post-victory Chulengo claim/multiplier and one free Premium Parrillero on each fresh Racing attempt. Built with Unity 6000.6.3f1 in a warm isolated copy: baseline source commit `70ce8b0` plus the current runtime overlay (`StreetGame.cs`, `StreetMarket.cs`, `StreetSimulation.cs`, `StreetView.cs`, and new `StreetChulengo.cs` + meta), with Android version/name set to0.2.58/code60. The local Firebase Analytics cookie/setting, Editor runInBackground/time-scale values, temporary scenes, screenshots, recovery data, audit tooling and Editor-only tests were excluded. The source overlay is now tracked on `codex/street-automation`; the APK itself remains a local ignored artifact, not a Git blob.

Unity Main-only Development IL2CPP/ARM64 build: **Succeeded in 524.18s; 0 errors / 1 BuildReport warning**. APK **344,979,035 bytes (328.99 MiB)**, package `com.haychoriypaty.game`, version **0.2.58/code60**, min API26/target36, ARM64. SHA-256 `e695784e59eb3de0945616e93249fa79e866efd3fbeacb51dc827a5aebeeb9e9`. `aapt`, ZIP integrity, `zipalign -c -v 4`, and APK Signature Scheme v2 verification passed; signer is the established Android Debug certificate `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

The verified artifact is `/private/tmp/HayChoriYPaty-0.2.58-code60.apk`; full build log: `/private/tmp/hcyp-release-0.2.57-code59-20261020/build-0.2.58.log`. Focused pre-package validation: Chulengo **37/37** unique checks and Racing starter **2/2**. The broader `StreetActiveCooksTests` run had **78/82 passed, 4 failed** in other existing assertions; that suite is not claimed green. Unity compilation/build succeeded. Motorola Edge 60 Fusion install via `adb install -r` returned **Success**; PackageManager verified code60/min26/target36. App was not launched after installation; no device gameplay test. Development/debug-signed, not Play Store release-signed.

---

# Current Android delivery — 2026-10-20 (0.2.57/code59, Mozo LIFO + lossless legacy-art exclusion)

This APK includes the Mozo pickup rule: for active-cook levels, a newly assigned Mozo claims the most recently deposited available sandwich of the requested product. Existing claims remain stable en route; legacy levels and beverages keep FIFO. Source commit: `70ce8b0` (delivery documentation follows in a docs-only commit).

Unity 6000.6.3f1 isolated Development IL2CPP/ARM64 build: **Succeeded in 85.22 s; 0 errors / 1 BuildReport warning**. APK **344,965,727 bytes (328.98 MiB)**, package `com.haychoriypaty.game`, version **0.2.57/code59**, min API26/target36, ARM64. SHA-256 `c406c8bce490fc34430970782e854f3766e25063d08153a7a9c110c7742eadf6`. `aapt` metadata, `unzip -t`, `zipalign -c -v 4`, and APK Signature Scheme v2 verification passed; signer is the established Android Debug certificate `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.57-code59-mozo-lifo-lossless.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.57.apk` and `Builds/Android/HayChoriYPatY-street.apk` are byte-identical. Compared with 0.2.55/code57, the APK is **10,271,000 bytes / 2.89% smaller**. Four unused archived legacy-texture entries are absent; the other **140 mapped Texture2D payloads are byte-identical** and all archived source/meta hashes are unchanged.

The focused Mozo LIFO EditMode job passed **14/14** before packaging (`0272fc59af224c089f88aa616041f48d`); it was not rerun in this packaging turn. The Android build itself compiled successfully. No PlayMode/Game View run, phone installation, or on-device test was performed. The Unity MCP project-info endpoint identified the correct 6000.6.3f1 project, but its editor-state query timed out; the build therefore used a warm, isolated snapshot via Unity CLI without restarting or modifying the live Editor.

Build log `Logs/Acceptance/AndroidBuild-20261020/build-0.2.57.log`; source/verification summary `source-0.2.57.json`. Firebase Analytics, runInBackground, TimeManager preferences, temporary scenes, and audit tools were excluded from the build/commit. Development/debug-signed, not Play Store release-signed.

---

# Previous Android delivery — 2026-10-20 (0.2.56/code58, lossless legacy-art exclusion)

> Esta versión previa no incluía el retiro LIFO del Mozo; el cambio se incorpora en la APK 0.2.57 de arriba.

**Lossless-only policy:** archive four superseded table PNG/meta sources under `Assets/Art/Editor/LegacyKitchen`, preserving original bytes/GUIDs. Keep the prototype resources and all live texture import/compression/mip settings unchanged. Remove only two unused StreetView loads/fields and the uncalled old table draw method; preserve the editor table-builder baseline via its updated archive path. No gameplay, dependency or Firebase changes.

Unity6000.6.3f1 isolated Development IL2CPP/ARM64 build: **Succeeded, 40.48s, 0 errors / 1 BuildReport warning** in the final verification. APK **344,964,983 bytes (328.98 MiB)**, versus355,236,727: **10,271,744 bytes / 2.89% smaller**. Package `com.haychoriypaty.game`, min26/target36,0.2.56/code58. SHA-256 `d7fb4859a44cb7175513e566d5e3fa6d933ebf7622968033ea7347fe3c20a81d`. Metadata,ZIP integrity,alignment and Debug signature verified. Archive `Builds/Android/Archive/HayChoriYPatY-0.2.56-code58-lossless-cleanup.apk`; aliases `HayChoriYPatY-0.2.56.apk` and `HayChoriYPatY-street.apk` (at that delivery).

**Actual artifact equality:** all140retained mapped Texture2D objects are byte-identical to0.2.55; four archived entries absent. All182other PNGs/metas unchanged. Native renderer table/Fernet region1080px wide,rows0–1199 has zero changed color bytes; full frames differ because grill smoke is animated. This is renderer/projection evidence, not complete-match or device performance certification.

Focused Editor6/6 and native renderer PlayMode1/1 passed; separate staging-cleanup matrix/no-op tests recorded in `Audits/LosslessApk-20261020-0256/INFORME.md`. Independent static review checked archive integrity, prefab dependencies and removal-only runtime changes.

The first build incorrectly retained orphan Gradle asset files; the second excluded them but retained10.4MB ZIP holes. Neither was released. Added Editor-only `AndroidLegacyArtStagingCleanup` prebuild hook: delete only known archived-GUID copies in generated Gradle staging and reset only incremental APK packaging caches/outputs when needed. Final warm-cache regression seeded old copies in the isolated staging, then verified hook execution and actual APK reduction. Native caches/source files untouched.

Build log `Logs/Acceptance/AndroidBuild-20261020/build-0.2.56.log`; exact overlay/source and asset hashes in `source-0.2.56.json` beside it. Local Firebase Analytics, runInBackground and TimeManager preferences excluded from build and preserved in the live checkout. No push/install. No phone cold-start/FPS benchmark; do not claim identical wall-time or a globally certified performance pass. The earlier150–200MBtarget required format/resolution tradeoffs and is not claimed with pixel-identical restrictions.
---

# Previous Android delivery — 2026-10-20 (0.2.55/code57, supported stacks and reachable pickups)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built in an isolated snapshot of base `251d236` plus the three current runtime corrections (StreetKitchenLayout, StreetKitchenRenderer, StreetWorkerProjection) and version fields: **Succeeded in 778.39 s; 0 errors / 1 BuildReport warning**. Build log also contains the temporary helper's obsolete API notice and clang unused-argument notices. Local preferences, Firebase Analytics settings, TimeManager, temporary scenes, audit tooling and editor tests were excluded. No push or installation performed.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.55-code57-stack-pickup-fix.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.55.apk` and `Builds/Android/HayChoriYPatY-street.apk`: **355,236,727 bytes (338.78 MiB)**; SHA-256 `f3c0b11bb20dae11ca7ed7ea684e4a0fcf8f81556954ce0a740991ebbb6a8a0f`. Package `com.haychoriypaty.game`, **0.2.55/code57**, min API26/target36, ARM64. `aapt`, `unzip -t`, `zipalign -c 4`, and `apksigner verify --print-certs` passed. Development/debug-signed with unchanged certificate; not Play Store release signing. No physical-device test claimed.

Source overlay hashes and exclusions: `Logs/Acceptance/AndroidBuild-20261020/source-0.2.55.json`; full log `build-0.2.55.log` alongside it. BuildReport total output bytes includes symbols/other build artifacts; the APK size above is measured directly.

Focused validation: final EditMode **7/7 passed** (job `a31295694f4d4039876b74007ba81f40`), native renderer capture **1/1 passed**. Final PlayMode **2/2 passed** (job `98c27dbd63854ef7994653dfa37c9665`): carried-food depth regression and full real255s Racing match. Sales continued after150s; matchLost, not a victory/balance certification. Captures and limitations are recorded in the focused correction entry in `modular-kitchen.md`.

---

# Previous Android delivery — 2026-10-20 (0.2.54/code56, capped sandwich piles)

Source commits `e13f1d0` (feature) and `b5485bb` (Android version) were pushed to `origin/codex/street-automation`. Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built in an isolated snapshot of `b5485bb`: **Succeeded in 473.83 s; 0 errors / 1 warning**. Only the ignored `Library` cache was reused; the live Editor/scene, local Firebase Analytics, `runInBackground`, time-scale, temporary scenes, screenshots and recovery files were excluded.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.54-code56-sandwich-piles.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.54.apk` and `Builds/Android/HayChoriYPatY-street.apk`: **355,235,135 bytes**, SHA-256 `fd87ffa72e0038f7a0d56894c2efd9387d9eebbfa3591b8623bb5ff3cb8e04cb` (all hard-linked, byte-identical). Package `com.haychoriypaty.game`, version **0.2.54/code56**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c -v 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused validation for this release passed **7/7 EditMode + 1/1 PlayMode** (per-product 48-unit reservations/stop-resume, supported/randomized pile layout and renderer capture). Unity MCP initially failed to initialize a combined test job; refresh/retry succeeded and the focused runs passed. Development/debug-signed, not Play Store release-signed. No phone install or on-device gameplay QA. Build log: `/private/tmp/hcyp-release-0.2.54-code56-20261020/build.log`.

---

# Previous Android delivery — 2026-10-20 (0.2.53/code55, patience removal + folding-table presentation)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built from pushed source commit `4e8af36`: **Succeeded in 284.34 s; 0 errors / 1 warning** (temporary build helper uses the obsolete `PlayerSettings.SetScriptingBackend(BuildTargetGroup, ...)` overload). Built in an isolated project snapshot via Unity batch mode; only the ignored `Library` cache was reused, and the live Editor/scene were not touched. Local Firebase Analytics, `runInBackground`, time-scale, temporary scenes, screenshots and recovery files were excluded.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.53-code55-customer-waiting-and-folding-tables.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.53.apk` and `Builds/Android/HayChoriYPatY-street.apk`: **355,234,955 bytes**, SHA-256 `9c4ff8d133a2f48fe05eab73c2424d8926236f035bb1988ab022d645d7fd1f65` (all hard-linked, byte-identical). Package `com.haychoriypaty.game`, version **0.2.53/code55**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c -v 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused implementation validation passed before packaging: patience/queue EditMode **6/6**; table presentation EditMode **8/8**, PlayMode **12/12**. These were not rerun during packaging. Development/debug-signed, not Play Store release-signed. No device install or on-device gameplay QA. Build log: `Logs/Acceptance/AndroidBuild-20261020/build-0.2.53.log`.

---

# Previous Android delivery — 2026-10-20 (0.2.52/code54, continuous active cooking + kitchen visuals)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built from clean isolated commit `b431f90`: **Succeeded in 415.01 s; 0 errors / 1 warning** (clang++ `-x c++` after the last input file has no effect). Used the installed Unity editor in batch mode; the live checkout/editor was not opened or modified. The user-authorized commit includes continuous active cooking for sandwiches 0–3 independent of quotas/Market ingredients, the caballete food-table presentation, and cooking-ring placement/color changes. Local Firebase Analytics, `runInBackground`, time-scale, temporary scenes, screenshots and recovery files were excluded from the build/commit.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.52-code54-active-cooks-tables-ring.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.52.apk` and `Builds/Android/HayChoriYPatY-street.apk`: **351,354,696 bytes**, SHA-256 `6927007f11678787d695981e76e66b23ad5144d839749d4ef8073a847b2f241e` (all hard-linked, byte-identical). Package `com.haychoriypaty.game`, version **0.2.52/code54**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Focused gameplay/presentation validation from the implementation run passed **5/5 EditMode + 1/1 Racing strategy**; not rerun during packaging. Development/debug-signed, not Play Store release-signed. No device installation or gameplay-on-phone QA was requested or performed. Build log: `Logs/Acceptance/AndroidBuild-20261020/build-0.2.52.log`.

---

# Previous Android delivery — 2026-10-20 (0.2.51/code53, active cooks, tables and cooking ring)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from an isolated snapshot of the then-staged `codex/street-automation` source: **Succeeded in 531.43 s; 0 errors / 4 warnings**. The sandboxed attempt could not open Unity Package Manager's local IPC socket; retrying outside the restricted socket sandbox succeeded without touching the active editor or scene. This predecessor artifact is superseded by 0.2.52/code54, which was built from pushed commit `b431f90`.

Archive `Builds/Android/Archive/HayChoriYPatY-0.2.51-code53-active-cooks-tables-ring.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPatY-0.2.51.apk` and `Builds/Android/HayChoriYPatY-street.apk`: **345,921,182 bytes**, SHA-256 `03c716afc5b4db4eb427f31f923b1537d95bb0f4ea6f3e2cab260cf8efd57183` (hard-linked, byte-identical). Package `com.haychoriypaty.game`, version **0.2.51/code53**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Development/debug-signed, not Play Store release-signed. The ring placement had passed 3/3 focused EditMode and 1/1 PlayMode capture tests before that packaging run; no tests were rerun for packaging. No device installation or gameplay-on-phone QA.

---

# Previous Android delivery — 2026-10-19 (0.2.50/code52, kitchen visuals and worker movement)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from pushed commit `df00c91`: **Succeeded; 0 errors / 1 warning** (Unity Diagnostics Data recommends enabling SymbolTable/Full debug symbols). Built through Unity CLI in the isolated cached project because MCP editor-state ping did not answer; the live project was not restarted or modified. Local Firebase Analytics cookie/setting, `runInBackground`, `TimeManager` scale, temporary scenes, screenshots and recovery files were excluded from Git and the clean build.

Archive `Builds/Android/Archive/HayChoriYPaty-0.2.50-code52-kitchen-visuals.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPaty-0.2.50.apk` and `Builds/Android/HayChoriYPaty-street.apk`: **347,626,599 bytes**, SHA-256 `a93608239dd515cd107f52246c8fdfb0660831f7c3c55bdcf19e0a42e55d3f11` (all copies byte-identical). Package `com.haychoriypaty.game`, version **0.2.50/code52**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Development/debug-signed, not Play Store release-signed. Focused visual/layout EditMode 4/4 and PlayMode 11/11 had passed before packaging; no tests were rerun during the build. No device install or post-build gameplay QA. Build log: `Logs/Acceptance/AndroidBuild-20261019/build-0.2.50.log`.

---

# Current Android delivery — 2026-10-19 (0.2.49/code51, grill contact and presentation)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from an isolated copy of commit `2c17fb9`: **Succeeded; 0 errors / 1 warning** (clang `-x c++` after the last input file has no effect). The live checkout/editor was not used for batch mode. The APK contains the pushed source at `codex/street-automation`; local Firebase Analytics, run-in-background and time-scale preferences were excluded by building the clean committed tree.

Archive `Builds/Android/Archive/HayChoriYPaty-0.2.49-code51-grill-contact.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPaty-0.2.49.apk` and `Builds/Android/HayChoriYPaty-street.apk`: **346,733,372 bytes**, SHA-256 `9bc5f44d0933edc268e17c8bf6f1cb62a6a49a19222ae9cceb126b6acf6e46ea`. Package `com.haychoriypaty.game`, version **0.2.49/code51**, min API26/target36, `arm64-v8a`. `aapt` metadata, `unzip -t`, `zipalign -c 4`, and APK Signature Scheme v2 verification passed; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

The APK is Development/debug-signed, not Play Store release-signed. No Unity tests were rerun during packaging; prior focused validations are recorded below. No full Game View review, phone install or on-device QA was performed. Build log: `Logs/Acceptance/AndroidBuild-20261019/build.log`.

---

# Previous Android delivery — 2026-10-18 (0.2.48/code50, direct employee movement)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from an isolated project copy: **Succeeded in 446.11 s; 0 errors / 1 warning** (clang++ unused command-line argument). Unity MCP at 127.0.0.1:8080/mcp was unavailable; inspection found the live Editor on the correct checkout holding its lock, so it was not restarted or used for batch mode. The build ran in an isolated copy. It includes the current worker route/spacing changes and Android-only ETC2 overrides; the user's local Analytics preference was excluded from the build.

APK archive Builds/Android/Archive/HayChoriYPaty-0.2.48-code50-direct-worker-movement.apk; versioned and rolling aliases Builds/Android/HayChoriYPaty-0.2.48.apk and Builds/Android/HayChoriYPaty-street.apk: **348,924,731 bytes**, SHA-256 e06e52d1fb3b433d33ad08834633bd5b02ef82e7e955de8c2f4fff91b3000e29. Package com.haychoriypaty.game, version0.2.48/code50, min API26/target36, arm64-v8a. aapt, unzip -t, zipalign -c -v 4, and apksigner v2 verification passed; signer is the established Android Debug certificate d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e.

Focused direct-movement tests passed 10/10 in the preceding implementation turn; the full StreetActiveCooksTests fixture was 52/54 with the same two safe-wait-point cases already failing before this change. Tests were not rerun for packaging. Development/debug-signed, not Play Store release-signed. No Game View/PlayMode/device visual review or phone installation was performed.

---

# Previous Android delivery — 2026-10-18 (0.2.47/code49, ETC2 texture compression)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build in an isolated copy: **Succeeded in 37.41 s; 0 errors / 1 warning**. The Unity MCP endpoint at `127.0.0.1:8080` was unavailable and the recorded Editor PID was not running; batch mode recovered the build without restarting an Editor or touching the phone. The automatic-compression experiment saved only 1.8%; the final importers use Android-only `ETC2_RGBA8` on all 141 PNGs in `Resources` (87 previously pinned to RGBA32 plus 54 automatic/unset). Source art, other platforms, and gameplay are unchanged. BuildReport Texture2D payload fell from 809.7 to 688.1 MiB.

The Unity-generated APK had a 41.8 MB gap before its ZIP central directory. Rewriting it through `zipalign` and signing it with the established debug key removed the gap and retained a valid aligned APK. Archive `Builds/Android/Archive/HayChoriYPaty-0.2.47-code49-texture-optimized.apk`; versioned `Builds/Android/HayChoriYPaty-0.2.47.apk` and rolling alias `Builds/Android/HayChoriYPaty-street.apk`: **347,748,382 bytes**, SHA-256 `7e63f27a388b01747c27af89264722c812a653612d9a2288d190f6bc0d563303`. This is **74,522,681 bytes (17.65%) smaller** than 0.2.46 (422,271,063 bytes). Package `com.haychoriypaty.game`, version0.2.47/code49, min API26/target36, `arm64-v8a`. `aapt` verified package/version, ZIP integrity passed, `zipalign -c -v 4` passed, and `apksigner` verified APK Signature Scheme v2 with Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Development/debug-signed, not Play Store release-signed. No gameplay tests, Game View screenshot, or phone install/visual review was performed.

---

# Previous Android delivery — 2026-10-18 (0.2.46/code48, two-row tables and worker fixes)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from an isolated checkout after the Unity MCP editor ping stopped responding: **Succeeded in 405.47s; 0 errors / 1 warning** (clang `-Wunused-command-line-argument`). Focused EditMode regressions: **17/17** across two-row sandwich layout, worker access/wait spacing, four-post rotation and mixed-product delivery. No PlayMode, Game View or device test was run.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.46-code48-current-updates.apk`; versioned and rolling aliases `Builds/Android/HayChoriYPaty-0.2.46.apk` and `Builds/Android/HayChoriYPaty-street.apk`: **422,271,063 bytes**, SHA-256 `f8acf6162a017f863e886d8cfd7300d6f7a49cca823a7f928ad4e282ea0e5e16` (all copies byte-identical). Package `com.haychoriypaty.game`, version0.2.46/code48, min API26/target36, `arm64-v8a`. `aapt` metadata, ZIP integrity and APK Signature Scheme v2 verified; Android Debug signer SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Development/debug-signed, not Play Store release-signed. Generated locally; not installed on a phone.

---

# Previous Android delivery — 2026-10-18 (0.2.45/code47, current changes)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build through the existing editor (build-4383fb3aa1): **Succeeded in 383.53s; 0 errors / 1 warning**. Focused current regressions: EditMode **9/9** (two-row order bubbles and responsive footer) and PlayMode **2/2** (Google sign-in navigation). Package com.haychoriypaty.game, min API26/target36, arm64-v8a. APK archive integrity passed; APK Signature Scheme v2 verified with the established Android Debug certificate SHA-256 d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e.

Archive APK Builds/Android/Archive/HayChoriYPaty-0.2.45-code47-current-updates.apk; versioned and rolling aliases Builds/Android/HayChoriYPaty-0.2.45.apk and Builds/Android/HayChoriYPaty-street.apk: **487,958,557 bytes**, SHA-256 19c6ea2fc929c1b4724fe920d2304d7be8d259a1bf5148744001b62e7298e003 (aliases hard-linked to the archive). Development/debug-signed, not Play Store release-signed. Not installed/launched; no device login test. New official club crests beyond the existing levels 1–3 were **not** added because game-use rights remain unverified.

---

# Previous Android delivery — 2026-10-17 (0.2.44/code46, Google sign-in button)

Unity6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build through the existing editor: **Succeeded in288.21s;0 errors/2 warnings**. Warnings: Diagnostics Data recommends SymbolTable/Full crash symbols; generic “uncompiled code changes” post-processing/import notice. APK IL2CPP metadata includes `JUGAR CON GOOGLE` and `google-g`; Firebase App/Auth/Firestore native libraries are present. `StreetMarketViewTests` PlayMode passed **9/9**; no device Google-login validation.

APK `Builds/Android/HayChoriYPaty-0.2.44-Firebase.apk`: **420,716,301 bytes**, SHA-256 `dcadb2f7171665c62ec521f6cbf0a543573c62bcdf6bf1495d132a90e49244dc`. Package `com.haychoriypaty.game`, version0.2.44/code46, min API26/target36, `arm64-v8a`. ZIP integrity and APK Signature Scheme v2 verified with established Android Debug SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Development/debug signed, not Play Store release signed. Installed on Motorola Edge 60 Fusion (ZY22MBNWRB) via adb install -r; PackageManager confirms versionName 0.2.44/code46. Existing app data was preserved; the game was not launched and Google sign-in was not tested on-device.

---

# Previous Android delivery — 2026-10-16 (0.2.42/code44, 2 s active cooking)

Unity6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build from an isolated copy of the active checkout: **Succeeded, 0 errors/1 warning**. The Unity MCP endpoint was unavailable, so the installed Editor's CLI built the isolated clone. The sole warning is the existing obsolete `PlayerSettings.SetScriptingBackend(BuildTargetGroup, ...)` call in `Assets/Editor/AutomationAndroidBuild.cs`; gameplay scripts compile. PlayerSettings advanced from0.2.41/code43 to0.2.42/code44. Includes the active Parrillero/Premium cooking duration of **2 / WorkRate seconds**; focused `StreetActiveCooksTests` passed **19/19 EditMode** on this source.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.42-code44-active-cook-2s.apk`; versioned copy `Builds/Android/HayChoriYPaty-0.2.42.apk`; rolling copy `Builds/Android/HayChoriYPaty-street.apk`. Each is **410,348,871 bytes**, SHA-256 `66bd2dc4a13bb571a8bab86f25c3794d93545bb7f7f5c1f703ffb1e58a1eb3bf`. `aapt` confirmed package `com.haychoriypaty.game`, version0.2.42/code44, min API26/target36 and `arm64-v8a`; `unzip -t` passed; bundled OpenJDK `apksigner` verified APK Signature Scheme v2 with Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. All three APK copies are byte-identical.

**Installation follow-up — 2026-10-16:** On Motorola Edge60Fusion (`ZY22MBNWRB`), `adb install -r` succeeded; PackageManager confirms versionCode44/versionName0.2.42. This updated the existing app without uninstalling or clearing data. The game was not launched, so there is no post-install gameplay/visual QA.

Development/debug-signed, not Play Store release-signed. PlayMode and live Unity Game View/visual review were not performed.

---

# Previous Android delivery — 2026-10-16 (0.2.40/code42, Mercado every-victory payout)

Unity6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build through the restored existing Unity/MCP editor: **Succeeded in284.55s; 0 errors/1 warning**. Build job `build-38c7ba9d9d`. Package `com.haychoriypaty.game`, version0.2.40/code42, min API26/target36, `arm64-v8a`.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.40-code42-every-victory-market.apk`; versioned copy `Builds/Android/HayChoriYPaty-0.2.40.apk`; rolling copy `Builds/Android/HayChoriYPaty-street.apk`. Each is **477,523,029 bytes**, SHA-256 `18f6366ba50e33cbbcc2d02f85e48e27bc3bbd18a80f39b31e7dbb9996e8a7ff`; the three copies compare byte-for-byte. `aapt` confirmed package/version/API and `arm64-v8a`; `unzip -t` found no archive errors; Unity-bundled OpenJDK `apksigner` verified APK Signature Scheme v2 with the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Includes full payout on every real victory, per-attempt deduplication, and exact one-time recovery of a valid legacy victory receipt. Targeted EditMode regressions passed **19/19 market +5/5 lifecycle +8/8 payout**; two Level11 audits using the phone's already-unlocked progress also passed separately, but fresh-campaign balance10 remains unresolved. APK is Development/debug-signed, not Play Store release signed. **Generated only; not installed or launched.** The phone save was not written/cleared; no post-build device QA.

---

# Latest Android delivery — 2026-10-15 (0.2.39/code41, restored street + worker layout)

**Installation follow-up:** user subsequently requested the phone update. Motorola Edge60Fusion (`ZY22MBNWRB`), `adb install -r`: Success; PackageManager confirmed versionName0.2.39/versionCode41. No uninstall/data clear, launch or post-install gameplay/visual/performance check. The packaging paragraph below records the earlier build-only state.

Pushed source commit `f077324` (`codex/street-automation`). Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 Android build (`build-2a50e40720`): **Succeeded in 516.95s; 0 errors / 2 warnings** (Unity build result reported warning count only). Focused EditMode job for `StreetActiveCooksTests` and `StreetSpecialtyPresentationTests` failed to initialize within timeout before any tests ran; no new test pass is claimed. `git diff --cached --check` passed before source commit.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.39-code41-street-restored-workers.apk` (also `Builds/Android/HayChoriYPaty-0.2.39.apk` and rolling `Builds/Android/HayChoriYPaty-street.apk`): 455MB; SHA-256 `e9cf00f55f77fb37645ba22109e7243bbd5fda78160acc0573124344143cd770`. `aapt` confirms package `com.haychoriypaty.game`, version0.2.39/code41, min API26/target36; APK contains `arm64-v8a`. `unzip -t` found no archive errors and `apksigner` verified APK Signature Scheme v2, Android Debug signer SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. All three copies are byte-identical. Development/debug-signed, not Play Store release signing. APK was not installed on a device.

---

# Latest Android delivery — 2026-10-15 (0.2.38/code40, Level 1 Mercado cleanup)

Unity 6000.6.3f1 batch build from the scoped Git commit, Main-only Development IL2CPP/ARM64: **Succeeded in 445.27s; 0 errors / 1 warning** (clang unused-argument warning). The focused PlayMode Mercado view regression passed **1/1**; it verifies the Level 1 optional group has no content height or pointer hit target, while later levels retain the expandable products. `git diff --check` passed. Unity MCP's editor ping did not answer, so no Game View screenshot or device visual review was available.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.38-code40-level1-market-cleanup.apk`; aliases `Builds/Android/HayChoriYPaty-0.2.38.apk` and `Builds/Android/HayChoriYPaty-street.apk`: **410,269,173 bytes**; SHA-256 `5d98c61ea3374f5135965ddeb53279d975a2e3ddd1c7ec14eaa377a2aa613741`. Package `com.haychoriypaty.game`, version0.2.38/code40, min API26/target36, `arm64-v8a`. `aapt` metadata, ZIP integrity and APK Signature Scheme v2 verified; signer is the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. All three paths are byte-identical. Development/debug-signed; not Play Store release signing. APK was not installed on a phone.

---

# Latest Android delivery — 2026-10-14 (0.2.35/code37, legacy Mercado reward repair)

Unity 6000.6.3f1 batch build from `Assets/Scenes/Main.unity`: **Succeeded in 275.3s; 0 errors / 1 warning** (Main-only Development IL2CPP/ARM64). Focused Unity validation: Market EditMode **22/22** and cash-only PlayMode stock-gate regression **1/1** passed before packaging.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.35-code37-market-migration.apk`; rolling copy `Builds/Android/HayChoriYPaty-street.apk`: **410,265,717 bytes**; SHA-256 `4d407239dede83d421a75cfc0f2e90586bb98bc4877685f42378d690bfa95b21`. Package `com.haychoriypaty.game`, version0.2.35/code37, min API26/target36, `arm64-v8a`. `aapt` metadata, ZIP integrity, and APK Signature Scheme v2 verified; Android Debug signer SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. The two APK paths are byte-identical. Development/debug-signed; not Play Store release signing. Phone install was not performed because ADB returned no connected device.

---

# Latest Android delivery — 2026-10-14 (0.2.34/code36, Level 1 stock floor)

Unity 6000.6.3f1 batch build from `Assets/Scenes/Main.unity`: **Succeeded in 288.55s** (Main-only Development IL2CPP/ARM64). Focused stock validation: EditMode **19/19** and PlayMode **1/1** passed before packaging.

Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.34-code36-level1-stock.apk`; rolling copy `Builds/Android/HayChoriYPaty-street.apk`: **410,263,293 bytes**; SHA-256 `bcd7da753a551fdab9b2220faf01e92b6fc17197f1bae52d88fe25315e814cc2`. Package `com.haychoriypaty.game`, version0.2.34/code36, min API26/target36, `arm64-v8a`. `aapt` metadata, APK archive integrity and APK Signature Scheme v2 verified; Android Debug signer SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Archive and rolling artifact are byte-identical. Development/debug-signed, not Play Store release signing. No phone install or device gameplay validation was performed.

---

# Latest Android delivery — 2026-10-14 (0.2.33/code35, unified Mercado + Level 1 stock kit)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built through Unity MCP job `build-f915a3143b`: **Succeeded in 27.68s; 0 errors / 3 warnings** (incremental build). The build included the unified Mercado UI, bread icon crop, and fresh-save Level 1 kit calculation. Unity test suite was not verified in this delivery: MCP returned zero tests for filtered calls and timed out on the assembly run.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.33-code35-level1-kit.apk`: **477,637,589 bytes**; SHA-256 `333f891ba1b18da0d13a5f383fb6b5e3bcd3a2448ef3985f8f6f63a67e2bcef9`. Package `com.haychoriypaty.game`, version0.2.33/code35, min API26/target36, `arm64-v8a`. `aapt` confirmed package/version, `unzip -t` passed, APK Signature Scheme v2 verified with established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned and rolling aliases: `Builds/Android/HayChoriYPaty-0.2.33.apk`, `Builds/Android/HayChoriYPaty-street.apk`.

Development/debug-signed; not Play Store release signing. No phone installation or gameplay validation performed.

---

# Previous Android delivery — 2026-10-13 (0.2.32/code34, Mercado visual redesign)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built through Unity MCP job `build-5b5b4bbe2f`: **Succeeded in 447.26s; 0 errors / 4 warnings**. Warnings: Diagnostics Data recommends Full debug symbols; generic uncompiled-code notice; obsolete PVRTC compression of Unity splash logo and uncompressed fallback. The updated Mercado source was compiled and tested before packaging; APK IL2CPP metadata contains the new card/wood-panel/others renderers. BuildReport output size 2,078.84 MB is not the APK download size.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.32-code34-market-visual-redesign.apk`: **477,642,081 bytes**; SHA-256 `2b862092559416463af13940775e014aba2ff1f080be8c547067f17ea6e831b2`. Package `com.haychoriypaty.game`, version0.2.32/code34, min API26/target36, `arm64-v8a`. `aapt` metadata, ZIP integrity, source/copy hashes and APK Signature Scheme v2 verified with established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned and rolling aliases: `Builds/Android/HayChoriYPaty-0.2.32.apk`, `Builds/Android/HayChoriYPaty-street.apk`.

**Validation:** Identical Mercado source passed focused PlayMode checks 9/9 before packaging; real Game View captures reviewed at 540×960, 540×1170 and 540×1200, including levels2/4/10, purchase/unused states and unchanged Heladera. Evidence: `Logs/Acceptance/MarketVisualRedesign-20261013/`, test job `3bd96811f4fe44cba75d56ef01f63585`. `git diff --check` passed. No full-suite rerun, Git publication, phone installation or physical-device review in this packaging task. Development/debug-signed; not Play Store release signing. Official crest resource coverage remains levels1–3 only; later Mercado clubs use palette/initials fallback tiles, not fabricated crests.

---

# Previous Android delivery — 2026-10-13 (0.2.31/code33, Mercado cash-only and objective cards)

Unity 6000.6.3f1 Development IL2CPP/ARM64 APK built from `Assets/Scenes/Main.unity`: **Succeeded; 0 errors / 2 warnings**. Unity MCP was unavailable, so the build used a temporary batch-mode editor entry point (removed after build).

APK `Builds/Android/Archive/HayChoriYPaty-0.2.31-code33-market-cashonly-objective-cards.apk`: **410,257,353 bytes**; SHA-256 `f2c443c23878c515d063be7d5235a7022c49ea2c2cb8249bfab3c1b878b8a165`. Package `com.haychoriypaty.game`, version0.2.31/code33, min API26/target36, `arm64-v8a`. `aapt` metadata and ZIP integrity passed; APK Signature Scheme v2 verified with the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned and rolling aliases: `Builds/Android/HayChoriYPaty-0.2.31.apk`, `Builds/Android/HayChoriYPaty-street.apk`.

**Validation:** Focused Mercado PlayMode checks passed 3/3 (objective card layout, explicit cash-only purchase, and stock gate) across logical frames 540×960, 540×1170 and 540×1200; `git diff --check` passed. Unity MCP was unavailable, so no Game View screenshot or manual visual review was captured. Development/debug-signed; not Play Store release signing.

---

# Previous Android delivery — 2026-10-13 (0.2.30/code32, Mercado and Heladera)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built through Unity MCP: **Succeeded in 449.24s; 0 errors / 2 warnings**. Warnings were the generic uncompiled-code notice and Diagnostics Data's recommendation to use Full debug symbols. Relevant C# assemblies had been compiled after their source edits, and the APK IL2CPP metadata contains the Market/Heladera identifiers. BuildReport output size was 2,068.77 MB (not APK download size).

APK `Builds/Android/Archive/HayChoriYPaty-0.2.30-code32-market-fridge.apk`: **477,497,857 bytes**; SHA-256 `b46a6b851c3d615465304a13553ba9332cf53ac60dbc1bf697d39cb97d8a370d`. Package `com.haychoriypaty.game`, version0.2.30/code32, min API26/target36, `arm64-v8a`. `aapt dump badging` confirms package/version/SDK; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies APK Signature Scheme v2 with the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned alias: `Builds/Android/HayChoriYPaty-0.2.30.apk`; rolling alias: `Builds/Android/HayChoriYPaty-street.apk`.

**Validation:** Focused EditMode Market/kitchen/specialty tests passed 30/30; HUD texture/popup geometry passed 4/4 (including 700, 960 and 1200 logical-pixel frames); PlayMode Market view/pointer/navigation passed 39/39; `git diff --check` passed. No phone install or device gameplay review was performed. Development/debug-signed; not Play Store release signing.

---

# Previous Android delivery — 2026-10-13 (0.2.29/code31, Fernet tabletop stock)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK built through Unity MCP: **Succeeded in 28.10s; 0 errors / 1 warning**. BuildReport total output size was 2,068.29 MB (not APK download size).

APK `Builds/Android/Archive/HayChoriYPaty-0.2.29-code31-fernet-tabletop.apk`: **477,470,333 bytes**; SHA-256 `d88b4914ec77a73b2a612d6a75bac0107c555a9f75f31ad7ad69eb60b20c3ea9`. Package `com.haychoriypaty.game`, version0.2.29/code31, min API26/target36, `arm64-v8a`. Unity `aapt dump badging` confirms package/version/SDK/ABI; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies APK Signature Scheme v2 with the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned alias: `Builds/Android/HayChoriYPaty-0.2.29.apk`; rolling alias: `Builds/Android/HayChoriYPaty-street.apk`.

**Validation:** Focused `FernetPresentationUses800CcCupAndBottlesBesideFinishedDrinks` EditMode regression passed 1/1 before packaging; Unity MCP Game View capture reviewed at 2160×3840 (`Logs/Acceptance/FernetTabletop-20261013/level6-after-two-stock-rows-2.png`). No device install or phone-side gameplay review was performed. Development/debug-signed; not Play Store release signing.

---

# Previous Android delivery — 2026-10-12 (0.2.27/code29, installed for device review)

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 APK build from an isolated copy: **Succeeded, 0 build errors / 1 warning** (Diagnostics Data recommends Full debug symbols). APK `Builds/Android/Archive/HayChoriYPaty-0.2.27-code29-responsive-ui.apk`: **410,226,097 bytes**; SHA-256 `707c66a4db6b7cb7ed96828c07ea0922f5435b7f84354087a5ca6ac25200b9f6`. Package `com.haychoriypaty.game`, version0.2.27/code29, min API26/target36, `arm64-v8a`. `aapt dump badging`, ZIP integrity and APK Signature Scheme v2 verified; Android Debug certificate SHA-256 remains `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Installed with `adb install -r` on Motorola Edge 60 Fusion (`ZY22MBNWRB`): **Success**; PackageManager confirms versionName0.2.27/versionCode29 and original firstInstallTime remains 2026-10-03 18:49:24 (no uninstall/data clear). APK was installed at the user's request despite broad test failures; no game launch or native visual review followed. Full EditMode:391/403 pass,12 fail; full PlayMode:44/50 pass,6 fail. Focused current HUD/footer tests (7 across five resolutions and eleven levels) and Fernet bottle shelf test passed. Unity Game View review remains unavailable. This is Development/debug-signed, not Play Store release signing. No commit or push was made.

Versioned APK: `Builds/Android/HayChoriYPaty-0.2.27.apk`; rolling alias: `Builds/Android/HayChoriYPaty-street.apk`.

---

# Previous Android delivery — 2026-10-12 (0.2.26/code28, restored product sizes)

Unity 6000.6.3f1 Main-only Android Development IL2CPP/ARM64 build in an isolated project copy: **Succeeded, 0 errors / 1 clang warning**.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.26-code28-product-size-rollback.apk`: **410,224,713 bytes**. SHA-256 `97093346de768d4dd3a2d5c33190194eb90e36f8d17c941369f7aa69e2ae197a`. Package `com.haychoriypaty.game`, version0.2.26/code28, min API26/target36, `arm64-v8a`. ZIP alignment and APK Signature Scheme v2 verified with the established Android Debug certificate.

Focused Unity tests passed 13/13 across EditMode and PlayMode: 800 cc Fernet cup/shelf content, grill meat bounds across all eleven levels, all ten restored service-size cases, and shelf size/aspect/separation. `adb install -r` returned **Success** on Motorola Edge 60 Fusion (`ZY22MBNWRB`); PackageManager confirms versionName0.2.26/versionCode28. No live Unity Game View screenshot or device visual review was captured. This is a Development/debug-signed APK, not a Play Store release.

---

# Previous Android delivery — 2026-10-12 (0.2.24/code26, responsive gameplay HUD)

Unity 6000.6.3f1 Main-only Android Development IL2CPP/ARM64 build `build-85da0aba77`: **Succeeded in 37.46s; 0 errors, 1 warning**. BuildReport total output size was 2,059.62 MB (not APK file size).

APK `Builds/Android/Archive/HayChoriYPaty-0.2.24-code26-responsive-header.apk`: **474,896,975 bytes**. SHA-256 `2502f389d90766c5755ed281804093e83caef806f3c549b332de113566ceec64`. Package `com.haychoriypaty.game`, version0.2.24/code26, min API26/target36, `arm64-v8a`. `aapt dump badging` confirms metadata; `unzip -t` reports no archive errors; Unity-bundled JDK + `apksigner` verifies APK Signature Scheme v2 with the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. The focused Unity EditMode HUD/layout checks passed 9/9. This is a Development/debug-signed APK, not a Play Store release. No phone install or Unity Game View/device visual review was performed.

---

## Previous Android delivery — 2026-10-12 (0.2.23/code25, order-delivery soft pop)

Unity 6000.6.3f1 Main-only Android Development IL2CPP/ARM64 build `build-814afe9a12`, run from the active Editor through Unity MCP: **Succeeded in 332.55s; BuildReport reported 0 errors and 1 warning**. BuildReport total output size was 2,059.7 MB and is not the APK file size. Build scene override was `Assets/Scenes/Main.unity`; PlayerSettings remained IL2CPP/ARM64.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.23-code25-order-pop.apk`: **474,897,283 bytes**. SHA-256 `6344d4277aa13f0f3f4529583f4838517580bed0b6ba8e548f320f6ba7a79651`. Package `com.haychoriypaty.game`, version0.2.23/code25, min API26/target36, `arm64-v8a`. `aapt dump badging` confirms metadata; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies APK Signature Scheme v2 using the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Root aliases `Builds/Android/HayChoriYPaty-0.2.23.apk` and `Builds/Android/HayChoriYPaty-street.apk` are hardlinks to the same verified artifact.

**Installed:** `adb install -r` returned **Success** on Motorola Edge 60 Fusion (`ZY22MBNWRB`); PackageManager confirms versionCode25/versionName0.2.23. Replacement install only: no uninstall or data-clear command was used. The app was not launched and no native visual/playback review was performed; the whole-order soft-pop runtime behavior remains unverified, and tests were not rerun for this packaging request. This is a local Development/debug-signed APK, not a Play Store release. No source code was committed or pushed.


---

## Previous Android delivery — 2026-10-10 (0.2.22/code24, Coca/beer barrel scale)

This historical 0.2.22 artifact predates source version 0.2.23/code25 and does not include subsequent changes. The latest verified APK is documented above.

Unity 6000.6.3f1 Main-only Android Development IL2CPP/ARM64 build from the current local `codex/street-automation` tree, including the +35% on-barrel Coca/beer sprite display size and the wider barrels: **Succeeded**, 0 build errors / 1 warning. Built in an isolated project copy so the user's open Unity editor was not disturbed. BuildReport total size is 1,698,854,456 bytes and is not the APK file size.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.22-code24-barrel-drinks.apk`: **506,529,693 bytes**. SHA-256 `ac7b078c4f5fd74511ac00d318812bc29c678986ae70f102c78962337256cd81`. Package `com.haychoriypaty.game`, version0.2.22/code24, min API26/target36, `arm64-v8a`. `aapt dump badging` confirms package/version/SDK metadata; `unzip -t` reports no archive errors; Unity-bundled `apksigner` verifies APK Signature Scheme v2 using the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned aliases `Builds/Android/HayChoriYPaty-0.2.22.apk` and `Builds/Android/HayChoriYPaty-street.apk` point to the same verified artifact.

The focused `FullCatalogBarrelDrinksRenderAsTossedIndividualObjects` PlayMode test passed 1/1 in an isolated Unity project copy. No phone installation, current Game View capture, or on-device visual review was performed. This is a local Development/debug-signed APK, not a Play Store release. Source/version changes remain local and were not committed or pushed.

---

## Previous Android delivery — 2026-10-10 (0.2.21/code23, victory popup fix)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 APK build from responsive commit 18e697a plus the victory-popup source fix: Succeeded; 0 build errors. The build ran in an isolated project copy because the authoritative checkout is already open in Unity. Unity emitted non-blocking diagnostics from the temporary build helper (SetScriptingBackend deprecation), clang++ and licensing-token refresh; none prevented the successful build.

APK Builds/Android/Archive/HayChoriYPaty-0.2.21-code23-victory-popup.apk: 402,286,236 bytes. SHA-256 b3ced92c175ff47ff23b3767e37d8a4cfac3886c06232ed9a6bc7623be128ace. Package com.haychoriypaty.game, version0.2.21/code23, min API26/target36, arm64-v8a. aapt dump badging confirms package/version/SDK metadata, unzip -t reports no archive errors, and Unity-bundled OpenJDK + apksigner verifies APK Signature Scheme v2 with the established Android Debug certificate SHA-256 d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e. Versioned and rolling aliases at Builds/Android/HayChoriYPaty-0.2.21.apk and Builds/Android/HayChoriYPaty-street.apk match byte-for-byte.

The source prevents upgrade-card drawing in terminal phases so the Won result popup can render. Focused Unity 6000.6.3f1 EditMode validation in an isolated copy of commit cec7d36 passed 21/21 cases: 6 upgrade-draw guard cases, 5 responsive footer resolutions covering all 11 level catalogs, and 10 victory popup frame/text/time cases. Full EditMode/PlayMode suites and visual/Game View/device verification remain unrun; no phone install or native visual review was performed. This is a local Development/debug-signed APK, not a Play Store release.

---

## Previous Android delivery — 2026-10-10 (0.2.20/code22, responsive gameplay regions)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build: **Succeeded in 524.75s; BuildReport: 0 errors, 2 warnings**. Package `com.haychoriypaty.game`, version0.2.20/code22, min API26/target36, `arm64-v8a`.

APK `/tmp/HayChoriYPaty-0.2.20-code22-responsive-regions.apk`: **402,287,116 bytes**. SHA-256 `b810f2f0aceedcabc89bda92d5c8d2c19497841c118f677423a62583887feee4`. `aapt dump badging` confirms package/version/min/target; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies APK Signature Scheme v2 and the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. This is Development/debug signed, not a Play Store release. No phone installation or physical-device review was performed. Unity build and tests ran in an isolated copy because another Unity instance already had the primary checkout open.

Focused `StreetArtTests` executed 145 cases: **144 passed, 1 unrelated existing quota-label assertion failed** (`VACÍO` expected, current abbreviated product label is `VAC`). All **11 new responsive geometry cases** passed across the five requested resolutions, including the eleven level catalogs, single-counter projection, and safe top-cutout.

---

## Previous Android delivery — 2026-10-09 (0.2.20/code22, responsive corrections)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-8d0c922186`: **Succeeded in 460.65s; BuildReport: 0 errors, 1 warning**. Package `com.haychoriypaty.game`, version0.2.20/code22, min API26/target36, `arm64-v8a`.

APK `/tmp/HayChoriYPaty-0.2.20-code22-responsive.apk`: **618,752,341 bytes**. SHA-256 `6609a746936d7795e25185aaa8d617acc3a1bcbbdfc96dc29bf88c1201f5b069`. `aapt dump badging` confirms package/version/min/target; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies Signature Scheme v2 and the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. No phone installation or physical-device review was requested/performed. This is Development/debug signed, not a Play Store release.

Focused Unity EditMode `StreetArtTests` report at `TestResults.xml` records 8/8 passing: safe-area HUD, tablet selector/upgrade bounds, seven NPOT mural imports, pennant masking, and queue bubble geometry at v=.75/1/1.25/1.42. The MCP job tracker timed out after Unity had written the successful 8/8 result; report XML is the validation source. Archive size is file size; BuildReport total-size field includes other outputs.

---

## Previous Android delivery — 2026-10-08 (0.2.20/code22, larger sandwiches)

Unity 6000.6.3f1 Main-only Android Development IL2CPP/ARM64 build `build-f0bd2b66cd`: **Succeeded in73s; BuildReport: 0 errors, 1 warning**. Package `com.haychoriypaty.game`, version0.2.20/code22, min API26/target36, `arm64-v8a`.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.20-code22-sandwich-scale.apk`: **618,489,985 bytes**. SHA-256 `3f948c1f1235023b3c4d9a2264fe23d63680f4a879b6eb5fe87d518bde956024`. `aapt dump badging` confirms package/version/API/architecture; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies Signature Scheme v2 and established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned alias and rolling APK match the archive byte-for-byte.

Includes the renderer-only increase of four sandwich families to1.5× on tables /1.25× carried, with artwork aspect preserved and drinks unchanged. Focused `StreetKitchenRendererTests` PlayMode class passed12/12 before build. **Not installed:** Motorola Edge60 Fusion `ZY22MBNWRB` was not present in `adb devices`; replacement installation remains pending reconnection. No uninstall/data clear/launch occurred. This is Development/debug signing, not a Play Store production release.

---

## Previous Android delivery — 2026-10-08 (0.2.19/code21, perspective grill fit)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-4524fd8d93`: **Succeeded in409.62s; BuildReport: 0 errors, 1 warning**. Package `com.haychoriypaty.game`, version0.2.19/code21, min API26/target36, `arm64-v8a`.

APK `Builds/Android/Archive/HayChoriYPaty-0.2.19-code21-grill-fit.apk`: **492,872,066 bytes**. SHA-256 `078b09aa340f8ca7bcd36f4c21c185a2bdcdacd6f095d58b265d346b2b39e9fe`. `aapt dump badging` confirms package/version/API/architecture; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` verifies Signature Scheme v2 and established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Versioned alias and rolling APK match the archive; previous archives preserved. BuildReport's total-size figure includes other build outputs and is not the APK file size.

Includes pushed perspective-grill correction commit `9648dfbafba65ee536f5b105cca2fe3e442f291e`; PlayerSettings records0.2.19/code21. Prior focused tests/Game Views for these corrections are documented in `modular-kitchen.md`; no tests were rerun for this packaging-only request. No device installation, launch or on-device validation was requested or performed. Main was clean and Play stopped before building; post-build editor-state read timed out, so no later editor-state confirmation is claimed. This is Development/debug signing, not a Play Store production release.

---

# Current Android delivery — 2026-10-08 (0.2.18/code20, station layout)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-ff697f0c18`: **Succeeded in28.71s; BuildReport: 0 errors, 1 warning** (`1 URP assets included in build`). Package `com.haychoriypaty.game`, version0.2.18/code20, min API26/target36, `arm64-v8a`.

APK `/tmp/HayChoriYPaty-0.2.18-code20.apk`: **492,866,782 bytes**. SHA-256 `00835bd2f3c050cc3b168650a6d9b5c6440bb30d497fa210c7f1d9b2682f5ed4`. `aapt dump badging` confirms package/version/min/target; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` confirms APK Signature Scheme v2 and established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`.

Built from station-layout commit `1718aedb619b95bb1151feb4cb880aed6c170b95`; `ProjectSettings/ProjectSettings.asset` records 0.2.18/code20. Previously documented Unity visual and EditMode validations remain the source validation; no tests were rerun for this packaging-only request. Installed on Motorola Edge 60 Fusion `ZY22MBNWRB` using `adb install -r` (**Success**); PackageManager verified versionCode20/versionName0.2.18. No uninstall or data clear occurred. The app was not launched and no on-device visual check was performed. This is a local Development/debug-signed APK, not a Play Store production-signed release.

---

## Previous Android delivery — 2026-10-07 (0.2.16/code18, four specialties)

Unity 6000.6.3f1 Main-only Development Android IL2CPP ARM64 build `build-c291df4c16`: **Succeeded in424.81s; BuildReport: 0 errors, 1 warning**. The sole build warning is URP reporting1 asset included. Console additionally logs a Diagnostics Data debug-symbol configuration advisory at Error log level; it did not produce a build error. Package `com.haychoriypaty.game`, version0.2.16/code18, min API26/target36, `arm64-v8a`.

Fresh archive `Builds/Android/Archive/HayChoriYPaty-0.2.16-code18-four-specialties.apk`: **439,337,048 bytes**. SHA-256 `df2944e2975c5c96e03e0c133df78550028f686991c675910aac0c22fe46ed1c`. `aapt dump badging` verifies package/version/min/target; `unzip -t` reports no archive errors; Unity-bundled OpenJDK + `apksigner` confirms APK Signature Scheme v2 and the established Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Root alias `Builds/Android/HayChoriYPaty-0.2.16.apk` and rolling `Builds/Android/HayChoriYPaty-street.apk` match the archive byte-for-byte. Earlier archive deliveries were preserved.

Built from the already-pushed four-specialty commit `cc456c87c04e5dc29749bf2537dcbf5f71767f11`; final full EditMode310/310 and PlayMode35/35 and Game View QA were run immediately before packaging, no project code changed during this build. The project version remains0.2.16/code18; no installer/device operation was requested or performed. This is local Development/debug signing, not a Play Store production-signed release.

---

## Previous Android delivery — 2026-10-06 (0.2.15/code17, Chicago grill)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-b25ab47045`: **Succeeded in 264.65s**, 0 errors / 1 warning. APK package `com.haychoriypaty.game`, version0.2.15/code17, min API26 / target36, `arm64-v8a`.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.15-code17-chicago-grill.apk`: **243,108,702 bytes**. SHA-256 `8ff5f1d1aeabab3a222e4e7d078ce94eb93f10121b1604d06f6a793a2c31d3c3`. `aapt` confirms package/version; `apksigner` verifies APK Signature Scheme v2. Rolling APK `Builds/Android/HayChoriYPaty-street.apk` matches byte-for-byte.

**Installed:** `adb install -r` returned **Success** on Motorola Edge 60 Fusion (`ZY22MBNWRB`); PackageManager confirms versionCode17/versionName0.2.15 and firstInstallTime 2026-10-03. No uninstall or data clear. The targeted Chicago grill EditMode checks had already passed **2/2** with a reviewed Game View capture before this packaging request; tests were not rerun for the build. No native on-device visual review was done.

---

## Previous Android delivery — 2026-10-06 (0.2.14/code16, Levels 4–5)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-714fa2d0bb`: **Succeeded in 284.30s**, 0 errors / 1 warning. The APK contains `com.haychoriypaty.game`, version0.2.14/code16, min API26 / target36, `arm64-v8a`.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.14-code16-levels4-5.apk`: **297,881,149 bytes**. SHA-256 `de81a45d374f8db6545bca6e52ddc9f4fa1947615288e4e1777d0a578eaace1d`. `aapt` confirms package/version; `apksigner` verifies APK Signature Scheme v2 with the existing Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Rolling APK `Builds/Android/HayChoriYPaty-street.apk` matches byte-for-byte. The focused Levels 4–5 EditMode suite previously passed **8/8**, zero failures/skips; it was not rerun during this APK-only request. No phone installation was requested or performed.

---

# Current Android delivery — 2026-10-06 (0.2.13/code15, victory popup)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-f93d61fec0`: **Succeeded in 263.61s**, 0 errors / 1 warning (Diagnostics Data recommends enabling debug symbols for detailed crash reports). The complete Street test assemblies passed: **139/139 EditMode + 23/23 PlayMode**, 0 failures/skips. Two test/source compilation issues found in the first validation attempt were fixed before these passing runs.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.13-code15-victory-popup.apk`: **190,308,172 bytes**, package `com.haychoriypaty.game`, version0.2.13/code15, min API26 / target36, `arm64-v8a`. SHA-256 `f0b963338611ad7cd12fadc1a282dee81bac59294ed2d70520541893e4aa9680`. APK v2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. Rolling APK `Builds/Android/HayChoriYPaty-street.apk` matches byte-for-byte.

**Installed:** `adb install -r` returned **Success** on Motorola Edge 60 Fusion (`ZY22MBNWRB`); PackageManager confirms versionCode15/versionName0.2.13. Replacement install only; no uninstall or data clear. The app was not launched, and the popup was not manually reviewed in a native Game-view/device screenshot.

---

# Current Android delivery — 2026-10-05 (0.2.12/code14, trifulca smoke)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-059ad48e84`: **Succeeded in 273.46s**, 0 errors / 2 warnings. The user requested the APK and phone installation; no tests or in-game gameplay review were requested or run.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.12-code14-trifulca-smoke.apk`: **186,968,662 bytes**, package `com.haychoriypaty.game`, version0.2.12/code14, min API26 / target36, `arm64-v8a`. SHA-256 `524b63abbfb4ea413ab597282b798f743221329c2f2667c224884662de5da926`. APK v2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. The rolling APK `Builds/Android/HayChoriYPaty-street.apk` matches byte-for-byte.

**Installed:** `adb install -r` onto the connected Motorola Edge 60 Fusion (`ZY22MBNWRB`) returned successfully. PackageManager reports versionCode14/versionName0.2.12. Replacement install only; no uninstall or data clear. App was not launched, and no visual/device gameplay check was performed.

---

# Current Android delivery — 2026-10-05 (0.2.11/code13, upgrade status ribbon)

Unity 6000.6.3f1 Main-only Development Android IL2CPP/ARM64 build `build-2dccbdc803`: **Succeeded in 19.38s**, 0 errors / 1 warning. Tests were not run per the user's standing instruction.

Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.11-code13-upgrade-option1.apk`: **181,258,740 bytes**, package `com.haychoriypaty.game`, version0.2.11/code13, min API26 / target36, `arm64-v8a`. SHA-256 `7ff670d8c5999a4c980fd263433c57289a6468c09aeea1928da9aec5ffe4fbab`. APK V2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. No phone installation was requested or performed; the previous rolling APK remains unchanged.

## Previous Android delivery — 2026-10-05 (0.2.10/code12, Floresta mural and apparel)

Current Unity source built as a Main-only Development Android IL2CPP/ARM64 APK with Unity 6000.6.3f1. Build report: **Succeeded in 246.68s**; the build log also contains a non-blocking Licensing access-token update diagnostic and the Diagnostics Data debug-symbol recommendation. The requested game tests were not run.

APK `Builds/Android/HayChoriYPaty-street.apk`; versioned archive `Builds/Android/Archive/HayChoriYPaty-0.2.10-code12-mural-wardrobe.apk`; **212,807,089 bytes**, package `com.haychoriypaty.game`, version0.2.10/code12, min API26 / target36, `arm64-v8a`. SHA-256 `639e11908265ff2b228afc2a40621f69a6b2d000f5dd1993f4aa5bb8244eb8a4`. APK V2 signature verified; Android Debug certificate SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`. `aapt` confirms package/version metadata; archive matches rolling APK byte-for-byte.

**Installed:** after the user reconnected the Motorola Edge 60 Fusion (serial `ZY22MBNWRB`), `adb install -r` returned **Success**. PackageManager confirms versionCode12/versionName0.2.10 on the phone. No tests were run; the app was not launched and data was not cleared or uninstalled.

## Previous Android delivery — 2026-10-05 (0.2.9/code11, Level 2 fix)

The complete Unity Editor test suite for the shipped source passed on Unity 6000.6.3f1 immediately before this version-only build: **116/116 EditMode + 21/21 PlayMode**, 0 failures/skips. Development Android IL2CPP/ARM64 build **Succeeded in 240.64s**, 0 errors / 1 warning (Diagnostics Data recommends debug symbols for detailed crash stack traces). Main scene only; min API26 / target36; `arm64-v8a`.

APK **0.2.9/code11**, **171,936,606 bytes (~164 MiB)**, package `com.haychoriypaty.game`. V2 signature verified with the existing local Android Debug certificate (SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`). APK SHA-256 `be171c5d141d7de2fd4af2dab0bd8654d67b5c629c14ae3e299d525a9bdf08fb`.

Versioned archive: `Builds/Android/Archive/HayChoriYPaty-0.2.9-code11-level2.apk`; rolling APK: `Builds/Android/HayChoriYPaty-street.apk`. Installed on the connected Motorola Edge 60 Fusion using `adb install -r`; PackageManager confirms version 0.2.9/code11. PlayerPrefs matched byte-for-byte before/after and firstInstallTime was preserved; no uninstall or data clear. The app was not launched for native gameplay/visual review. Build log, signature/package verification, and preference comparison are in ignored `Logs/Acceptance/Level2Delivery-20261005/`.

## Previous Android delivery — 2026-10-05 (0.2.8/code10, full suite)

The complete Unity Editor test suite passed on Unity 6000.6.3f1: **115/115 EditMode + 21/21 PlayMode**, 0 failures/skips. Development Android IL2CPP/ARM64 build **Succeeded**, 0 errors / 1 warning (Diagnostics Data recommends debug symbols for detailed crash stack traces). APK **0.2.8/code10**, **168,244,348 bytes**, package `com.haychoriypaty.game`, min API26 / target36, `arm64-v8a`. V2 signature verified with the existing local Android Debug certificate (SHA-256 `d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`). APK SHA-256 `6600a5dc54d5c9f917ce2d33d8f2ee999f613ba87cebcd8ef02f44ea8371b240`.

Versioned archive: `Builds/Android/Archive/HayChoriYPaty-0.2.8-code10-fullsuite.apk`; APK was not installed in that delivery. Evidence is in ignored `Logs/Acceptance/FullSuite-20261005/`.

---

## Previous Android delivery — 2026-10-04 (0.2.7/code9, timeout riot)

Current Main source (including the timeout-riot presentation) built with Unity 6000.6.3f1 as Development Android IL2CPP ARM64: **Succeeded**,255.41s,0errors/1 Clang warning (`'-x c++' after last input file has no effect`). APK archive `Builds/Android/Archive/HayChoriYPaty-0.2.7-code9-deadline-riot.apk`,157,471,790bytes; rolling artifact `Builds/Android/HayChoriYPaty-street.apk`; package `com.haychoriypaty.game`, version0.2.7/code9, minAPI26/target36, `arm64-v8a`. V2 signature verified; SHA-256 `0c9708a210939c256414ea3c059e64659f73de767482dcfbbe2101c2ffb25ded`; local Android Debug certificate unchanged (`d2fc25710ab22385b7c6979159c0ef647e91672aa9f6a8672916e52df2e28b5e`).

Installed on the authorized Motorola Edge60 Fusion (`ZY22MBNWRB`) using `adb install -r`: **Success**, PackageManager reports0.2.7/code9. PlayerPrefs XML matched byte-for-byte before/after; no uninstall or data clear. GameActivity launch succeeded and the app process remained live; at final inspection Android showed Rappi resumed over the game, so it was not forced back to the foreground. The focused deadline→riot→replay PlayMode test had passed1/1 before this delivery; no tests were rerun during the build/install, and no native gameplay/visual acceptance review was performed. Evidence (build log, APK metadata/signature and preference snapshots) is in ignored `Logs/Acceptance/DeadlineRiot20261004/`.

---

# Android delivery — current Street and historical prototype


## Current combined validation and phone delivery — 2026-10-04 (0.2.6/code8)

Latest user authorized tests, APK and phone installation. Installed Unity 6000.6.3f1 was closed (no running editor/MCP); used the exact installed editor in sequential batch runs, without new packages or a second editor. Current Street **82/82 EditMode + 15/15 PlayMode passed**,0failed/0skipped. Scope includes200-handoff victory/180s deadline, FIFO/reservations, round-local coins and first-level reset, exact progressive prices/caps/MAX, mouse/touch dispatch/start/replay/load, intro timing/asset/viewport guards, current art/HUD/mural/footer imports and layout. Not an unrelated legacy full-suite run or mobile performance certification.

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

- Latest user authorized tests/build/phone update. Existing Unity 6000.6.3f1/Android/Main: relevant EditMode52completed/succeeded/no reported failures; PlayMode12/12passed,0failed/0skipped. Regression scope and jobs are in[street automation](street-automation.md#combined-validation--2026-10-04-current-accumulated-implementation). Actual menu,21-fan crowd, paired idle animation frames and seven-product Ready/Playing captures reviewed in ignored`Logs/Acceptance/Combined`; exact editor progress restored and Play stopped.
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
## Contadores translúcidos — 2026-10-25 (0.2.67/code69)

APK Android Development, Main-only IL2CPP/ARM64, Unity 6000.6.3f1. BuildPipeline succeeded, 0 errores/1 advertencia; package `com.haychoriypaty.game`, min API26/target36. ZIP íntegro, firma v2 verificada (Android Debug; no es release Play Store). APK: `/private/tmp/HayChoriYPaty-0.2.67-code69-translucent-counters.apk`, 384.540.991 bytes, SHA-256 `7db6d2c5560ce92c42df2d69b9f24f9af8e9fa19a988ebd0b98e760593c9375f`.

La fixture StreetArt terminó 213/230: todas las pruebas del nuevo HUD pasaron (11 niveles de catálogo, 5 formatos portrait y opacidad); las 17 fallas restantes son checks de importación de arte que esperan RGBA32 mientras la plataforma Android usa ETC2_RGBA8. No hubo captura de Game View física ni instalación en teléfono. La APK se compiló desde una copia aislada del working tree vigente, que contiene además otros cambios locales todavía no publicados; el commit acotado de este entrega registra solo los contadores.
