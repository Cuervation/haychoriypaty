# Current state — Hay Chori y Paty

**Checkpoint date:** 2026-10-27. This is a compact snapshot, not a timeline or replacement for the contracts. Historical continuity entries are preserved verbatim in [continuity-log-through-20261025.md](history/continuity-log-through-20261025.md); older checkpoint: [continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md).

## Project and environment

- Active checkout: `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`; preserve all pre-existing local changes. The Documents/ChatGPT folder is a context copy, not the active checkout.
- Unity `6000.6.3f1`; Android-first 2D portrait. Player Settings release fields: `0.2.77` / code `79`. Latest Development IL2CPP/ARM64 APK: `Builds/Android/HayChoriYPaty-0.2.77-Firebase.apk` (387,369,019 bytes; SHA-256 `5a27b7225b3a185bd5ddcdbc8dedc42a330e76577f326b2af19a71c05efa1321`), debug-signed; BuildPipeline succeeded (0 errors/5 warnings), ZIP, package/version/ABI, `zipalign` and APK Signature Scheme v2 verified. Generated from clean commit `18d97b6` in an isolated copy; local `runInBackground: 1` preference was not included. APK is ignored by Git and was not installed or tested on a device. No authenticated OAuth/game session has been confirmed. Details: [Android prototype](features/android-prototype.md).
- The user confirmed selection/trust of the active checkout on 2026-10-26. No global settings were changed; existing skills/roles and configured model/effort values remain untouched.

## Implemented scope (high-level)

- Eleven clubs, progressive seven-product catalog, per-product quotas, fixed product-ID prices, physical handoffs, FIFO queues, and no customer patience timeout.
- Persistent cash/stock Mercado and Heladera; no credit; victory settlement and attempt-local hires/speed.
- From Racing (level 6), active cooks, physical modular kitchen and Mozo-only sandwich handoff; beverage specialties remain.
- Main presentation includes level selection, hub, responsive UI, tutorial and Chulengo. Implementation does not imply universal visual/device acceptance; use each contract's acceptance.

## Open risks / pending

- No current evidence here certifies a fresh-save, human-playable campaign across all 11 levels after the latest balance. Historical campaign results are scoped in [status](status.md); do not adjust goals, prices, or times without authorization.
- Authenticated gameplay on a real player account remains NOT_RUN. The AVD confirms logged-out behavior (hub stays visible; JUGAR/avatar opens CUENTA), while focused PlayMode tests with the isolated simulated Ready session confirm selector and round-start flows. AVD cold startup is functionally visible but slow: the full 66-resource load took 99.03 s; performance optimization and real OAuth remain open. No current physical-device QA was performed in this task; see [Android delivery](features/android-prototype.md).
- Racing retains its isolated free-Mozo trial (LevelIndex 5; 1/5 wins in five authentic ×1 PlayMode attempts). San Lorenzo has a separate provisional trial: quotas `[230,175,110,95,205,90,115]` and one free Mozo. Effective Main-scene goals, Market requirements and worker/reset/cost behavior passed 14/14 focused EditMode checks, but its five real ×1 PlayMode matches are BLOCKED / NOT_RUN: no authentic Racing-win checkpoint unlocking LevelIndex 6 was produced. See section 11 of the balance audit; no San Lorenzo outcome or bottleneck is claimed.

- River y Boca adoptan provisionalmente la apertura U por pedido del usuario: $800, 340 s, un Parrillero normal inicial, sin otros starters y ×1.00; se conservan sus cuotas/costos propios. River U obtuvo 2/10 victorias sin racha de tres; Boca no tiene ensayo separado ni aceptación humana. Ver [street automation](features/street-automation.md).

## Contract map

Start at [SDD index](README.md). Product boundary: [product](product.md); loop: [gameplay](gameplay.md); runtime boundaries: [architecture](architecture.md); balance, sales and queue: [street automation](features/street-automation.md#current-per-product-level-balance--2026-10-24); active cooking/Mozos: [active cooks](features/active-cooks-mozos.md); persistence: [Firebase player](features/firebase-player.md) and [market/fridge](features/market-fridge.md). Other feature contracts are listed in the index.

## Evidence and history

Latest Coca presentation keeps the upright frontal approach, with its baseline offset now interpolated linearly only across the final/first 48 route units; feet remain 24 logical units behind the barrel base at pickup. The barrel occludes the lower body; routes/anchors, stock, timing and physical IDs are unchanged. EditMode blend regression passed 1/1. PlayMode integration is blocked by Unity URP Null Device SIGSEGV in headless mode; the updated live-transition capture and user visual acceptance remain open. The preceding full Game View capture at 1080×1920 is preserved in `Logs/Acceptance/CocacoleroFront-20261026/` but does not validate the new linear timing. Latest APK and complete delivery limits are in [Android prototype](features/android-prototype.md) and [status](status.md). Historical decisions remain in [decisions](decisions.md).
