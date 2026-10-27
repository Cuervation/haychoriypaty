# Current state — Hay Chori y Paty

**Checkpoint date:** 2026-10-27. This is a compact snapshot, not a timeline or replacement for the contracts. Historical continuity entries are preserved verbatim in [continuity-log-through-20261025.md](history/continuity-log-through-20261025.md); older checkpoint: [continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md).

## Project and environment

- Active checkout: `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`; preserve all pre-existing local changes. The Documents/ChatGPT folder is a context copy, not the active checkout.
- Unity `6000.6.3f1`; Android-first 2D portrait. Player Settings release fields: `0.2.76` / code `78`. Latest Development IL2CPP/ARM64 APK: `/private/tmp/hcyp-splash-fix-0.2.76-code78/HayChoriYPaty-0.2.76-code78.apk` (387,369,899 bytes; SHA-256 `1802d95ae1a792b7aa2f60d22ca092d9d4090832e27bc892c247eb339b819953`), debug-signed; BuildPipeline succeeded (0 errors/5 warnings), ZIP, package/version/ABI, `zipalign` and APK Signature Scheme v2 verified. It was generated with the existing local `runInBackground: 1` preference, which remains unstaged/unpublished. This 0.2.76 APK was not installed; the previous AVD Pixel 7a/API36 smoke evidence belongs to 0.2.75/code77 only. No physical phone was touched. No authenticated OAuth/game session has been confirmed. Details: [Android prototype](features/android-prototype.md).
- The user confirmed selection/trust of the active checkout on 2026-10-26. No global settings were changed; existing skills/roles and configured model/effort values remain untouched.

## Implemented scope (high-level)

- Eleven clubs, progressive seven-product catalog, per-product quotas, fixed product-ID prices, physical handoffs, FIFO queues, and no customer patience timeout.
- Persistent cash/stock Mercado and Heladera; no credit; victory settlement and attempt-local hires/speed.
- From Racing (level 6), active cooks, physical modular kitchen and Mozo-only sandwich handoff; beverage specialties remain.
- Main presentation includes level selection, hub, responsive UI, tutorial and Chulengo. Implementation does not imply universal visual/device acceptance; use each contract's acceptance.

## Open risks / pending

- No current evidence here certifies a fresh-save, human-playable campaign across all 11 levels after the latest balance. Historical campaign results are scoped in [status](status.md); do not adjust goals, prices, or times without authorization.
- Authenticated gameplay on a real player account remains NOT_RUN. The AVD confirms logged-out behavior (hub stays visible; JUGAR/avatar opens CUENTA), while focused PlayMode tests with the isolated simulated Ready session confirm selector and round-start flows. AVD cold startup is functionally visible but slow: the full 66-resource load took 99.03 s; performance optimization and real OAuth remain open. No current physical-device QA was performed in this task; see [Android delivery](features/android-prototype.md).
- Racing-only free-Mozo experiment (LevelIndex 5): five authentic ×1 PlayMode attempts yielded 1/5 wins (256.06 s, 3.94 s left); target ≥3/5 and preferred ≥15 s was not met. Keep only as a provisional local experiment; do not generalize to other levels. Details and per-product results: [active cooks/Mozos](features/active-cooks-mozos.md) and [balance audit addendum](../docs/audits/2026-10-21-live-balance/INFORME_COMPLETO_TODOS_LOS_NIVELES.md).

## Contract map

Start at [SDD index](README.md). Product boundary: [product](product.md); loop: [gameplay](gameplay.md); runtime boundaries: [architecture](architecture.md); balance, sales and queue: [street automation](features/street-automation.md#current-per-product-level-balance--2026-10-24); active cooking/Mozos: [active cooks](features/active-cooks-mozos.md); persistence: [Firebase player](features/firebase-player.md) and [market/fridge](features/market-fridge.md). Other feature contracts are listed in the index.

## Evidence and history

Latest Coca presentation keeps the upright frontal approach, with its baseline offset now interpolated linearly only across the final/first 48 route units; feet remain 24 logical units behind the barrel base at pickup. The barrel occludes the lower body; routes/anchors, stock, timing and physical IDs are unchanged. EditMode blend regression passed 1/1. PlayMode integration is blocked by Unity URP Null Device SIGSEGV in headless mode; the updated live-transition capture and user visual acceptance remain open. The preceding full Game View capture at 1080×1920 is preserved in `Logs/Acceptance/CocacoleroFront-20261026/` but does not validate the new linear timing. Latest APK and complete delivery limits are in [Android prototype](features/android-prototype.md) and [status](status.md). Historical decisions remain in [decisions](decisions.md).
