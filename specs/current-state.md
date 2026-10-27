# Current state — Hay Chori y Paty

**Checkpoint date:** 2026-10-26. This is a compact snapshot, not a timeline or replacement for the contracts. Historical continuity entries are preserved verbatim in [continuity-log-through-20261025.md](history/continuity-log-through-20261025.md); older checkpoint: [continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md).

## Project and environment

- Active checkout: `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`; preserve all pre-existing local changes. The Documents/ChatGPT folder is a context copy, not the active checkout.
- Unity `6000.6.3f1`; Android-first 2D portrait. `ProjectSettings.asset` reports `0.2.73` / code `75`. Latest Development IL2CPP/ARM64 APK: `/private/tmp/hcyp-release-0.2.73-code75/HayChoriYPaty-0.2.73-code75.apk` (SHA-256 `233361b184ef1b8b2ad5eebc6029b3188664be8a0ef553c0f87ff6ec42c7b28b2`), debug-signed. Package/ZIP/alignment/signature checks pass; the AVD launched the Unity activity but remained visually blank, so startup is not accepted. No physical device was attached. Details: [Android prototype](features/android-prototype.md).
- The user confirmed selection/trust of the active checkout on 2026-10-26. No global settings were changed; existing skills/roles and configured model/effort values remain untouched.

## Implemented scope (high-level)

- Eleven clubs, progressive seven-product catalog, per-product quotas, fixed product-ID prices, physical handoffs, FIFO queues, and no customer patience timeout.
- Persistent cash/stock Mercado and Heladera; no credit; victory settlement and attempt-local hires/speed.
- From Racing (level 6), active cooks, physical modular kitchen and Mozo-only sandwich handoff; beverage specialties remain.
- Main presentation includes level selection, hub, responsive UI, tutorial and Chulengo. Implementation does not imply universal visual/device acceptance; use each contract's acceptance.

## Open risks / pending

- No current evidence here certifies a fresh-save, human-playable campaign across all 11 levels after the latest balance. Historical campaign results are scoped in [status](status.md); do not adjust goals, prices, or times without authorization.
- Physical-device QA (safe area/cutout, touch, performance) remains NOT_RUN. The 0.2.73/code75 AVD startup smoke showed a live foreground Unity activity but a blank dark frame after cold and warm launches; do not claim visible startup acceptance until verified/fixed on a suitable device. See [Android delivery](features/android-prototype.md).
- Racing-only free-Mozo experiment (LevelIndex 5): five authentic ×1 PlayMode attempts yielded 1/5 wins (256.06 s, 3.94 s left); target ≥3/5 and preferred ≥15 s was not met. Keep only as a provisional local experiment; do not generalize to other levels. Details and per-product results: [active cooks/Mozos](features/active-cooks-mozos.md) and [balance audit addendum](../docs/audits/2026-10-21-live-balance/INFORME_COMPLETO_TODOS_LOS_NIVELES.md).

## Contract map

Start at [SDD index](README.md). Product boundary: [product](product.md); loop: [gameplay](gameplay.md); runtime boundaries: [architecture](architecture.md); balance, sales and queue: [street automation](features/street-automation.md#current-per-product-level-balance--2026-10-24); active cooking/Mozos: [active cooks](features/active-cooks-mozos.md); persistence: [Firebase player](features/firebase-player.md) and [market/fridge](features/market-fridge.md). Other feature contracts are listed in the index.

## Evidence and history

Latest Coca presentation keeps the upright frontal approach, with its baseline offset now interpolated linearly only across the final/first 48 route units; feet remain 24 logical units behind the barrel base at pickup. The barrel occludes the lower body; routes/anchors, stock, timing and physical IDs are unchanged. EditMode blend regression passed 1/1. PlayMode integration is blocked by Unity URP Null Device SIGSEGV in headless mode; the updated live-transition capture and user visual acceptance remain open. The preceding full Game View capture at 1080×1920 is preserved in `Logs/Acceptance/CocacoleroFront-20261026/` but does not validate the new linear timing. Latest APK and complete delivery limits are in [Android prototype](features/android-prototype.md) and [status](status.md). Historical decisions remain in [decisions](decisions.md).
