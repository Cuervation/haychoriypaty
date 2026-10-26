# Current state — Hay Chori y Paty

**Checkpoint date:** 2026-10-26. This is a compact snapshot, not a timeline or replacement for the contracts. Historical continuity entries are preserved verbatim in [continuity-log-through-20261025.md](history/continuity-log-through-20261025.md); older checkpoint: [continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md).

## Project and environment

- Active checkout: `/Users/celestino/HayChoriYPaty`, branch `codex/street-automation`; preserve all pre-existing local changes. The Documents/ChatGPT folder is a context copy, not the active checkout.
- Unity `6000.6.3f1`; Android-first 2D portrait. `ProjectSettings.asset` reports `0.2.71` / code `73`; Development IL2CPP/ARM64 APK was generated and validated at `/private/tmp/hcyp-release-0.2.71-code73/HayChoriYPaty-0.2.71-code73.apk` (SHA-256 `b751dabc3ec5f9b0e6abbc4a58bcb85bb59f73a6b7a36fc9bab3cd882d4f20ad`). It is debug-signed and has not been installed. Delivery evidence is in [Android prototype](features/android-prototype.md).
- The user confirmed selection/trust of the active checkout on 2026-10-26. No global settings were changed; existing skills/roles and configured model/effort values remain untouched.

## Implemented scope (high-level)

- Eleven clubs, progressive seven-product catalog, per-product quotas, fixed product-ID prices, physical handoffs, FIFO queues, and no customer patience timeout.
- Persistent cash/stock Mercado and Heladera; no credit; victory settlement and attempt-local hires/speed.
- From Racing (level 6), active cooks, physical modular kitchen and Mozo-only sandwich handoff; beverage specialties remain.
- Main presentation includes level selection, hub, responsive UI, tutorial and Chulengo. Implementation does not imply universal visual/device acceptance; use each contract's acceptance.

## Open risks / pending

- No current evidence here certifies a fresh-save, human-playable campaign across all 11 levels after the latest balance. Historical campaign results are scoped in [status](status.md); do not adjust goals, prices, or times without authorization.
- Physical-device QA (safe area/cutout, touch, performance) and open visual acceptance remain contract-specific; see [worker animation](features/worker-animation.md), [street automation](features/street-automation.md), and [Android delivery](features/android-prototype.md).

## Contract map

Start at [SDD index](README.md). Product boundary: [product](product.md); loop: [gameplay](gameplay.md); runtime boundaries: [architecture](architecture.md); balance, sales and queue: [street automation](features/street-automation.md#current-per-product-level-balance--2026-10-24); active cooking/Mozos: [active cooks](features/active-cooks-mozos.md); persistence: [Firebase player](features/firebase-player.md) and [market/fridge](features/market-fridge.md). Other feature contracts are listed in the index.

## Evidence and history

Latest pickup presentation (2026-10-26) restores upright frontal Coca pickup and final approach, removes the lateral shift and locates the visual feet 24 logical units behind the barrel base. The barrel occludes the lower body; simulation routes/anchors, stock and timing are unchanged. Two focused native PlayMode tests and one interactive full Game View capture passed (1080×1920 with tiled floor). User visual acceptance and complete live-transition video remain open. Evidence: Logs/Acceptance/CocacoleroFront-20261026/. Earlier unrelated refinements retain their own validation statuses in [status](status.md). Historical decisions remain in [decisions](decisions.md).
