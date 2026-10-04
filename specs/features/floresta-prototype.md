# Feature: Floresta playable prototype

## Scope

One vertical (1080×1920 reference) 2D cartoon scene on the Floresta public sidewalk outside All Boys. Consult [visual-reference.md](../visual-reference.md) before any visual design. Preserve its top-supporters/orders, central-vendors, lower-parrilla/equipment and bottom-upgrades hierarchy without copying its artwork. Show iron street grill/brasas, chorizo/bread, drinks cooler, condiment table, and standing supporters. The active empty background and individually cropped sprite atlas are original, provisional and swappable; live supporters replace the old static plate crowd. No indoor restaurant, seating, or football match simulation.

Playable systems: one starting vendor; chori sandwich and Coca cup orders; arrivals; per-customer patience; automatic timed cooking (no flipping), automatic condiment stop, delivery and coin reward; purchase an additional automatic worker and a basic speed improvement; shared crowd patience; win/lose result UI. Tunables live on the scene controller.

## Acceptance criteria

- [x] The prototype scene opens and enters play without adding runtime packages.
- [x] Portrait presentation is configured at 1080×1920 reference resolution.
- [x] Screen composition follows visual-reference.md: supporters/orders/patience at top, vendors centrally, street parrilla and side condiments below, upgrade controls at bottom.
- [x] Original 2D cartoon style stays consistent (rounded expressive characters, vivid palette, defined outlines); no photorealism, 3D, generic restaurant, or copied reference assets.
- [x] Scene reads as an outdoor Floresta street stand with grill/counter/cooler/condiments and standing customers; generated plate is identified as provisional.
- [x] Distinct supporter/equipment sprites and frame-based walk/cook/serve animation states replace the remaining static art before claiming the visual milestone complete.
- [x] New customers arrive, show an order and individual patience; patience expires if neglected.
- [x] Chori cooks over time automatically; ready food passes the condiment stop automatically; no manual flipping/condiment selection is required.
- [x] Orders are automatically handed off by staff, with configurable coins added to the balance.
- [x] Start with one seller; hiring a second seller is possible with earned coins and increases automatic throughput.
- [x] A speed upgrade is purchasable, affordable/costed from the balance, and changes throughput.
- [x] Shared crowd patience changes under pressure and reaching zero ends in defeat; reaching target wins; results are visible.
- [x] Values for arrival, cooking, condiment, patience, time, service target, starting funds, prices/reward and speed are inspector-configurable.
- [x] Only chori and Coca are active; seven-product catalog remains authoritative in `product.md`.

## Validation record

See [`../status.md`](../status.md) for the 13 EditMode + 3 PlayMode cases, actual Main run and portrait screenshots. These checked criteria describe the editor prototype, not Android/device/final-art certification. Physical-device touch, safe area, performance and final polish remain pending.
