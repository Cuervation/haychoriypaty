# Product specification

**Hay Chori y Paty** is a portrait, 2D cartoon time-management/arcade game for Android. The player manages a street-food stand outside Argentine football grounds; football results do not affect play. The first location is Floresta, outside All Boys' stadium. Customers stand at the counter or leave; there is no restaurant, indoor stand, or seating.

## Definitive product catalog

Exactly seven products, progressively unlocked:

1. Chorizo sandwich (always on bread).
2. Hamburger-patty sandwich (always on bread).
3. Bondiola sandwich (always on bread).
4. Beef-flank sandwich (always on bread).
5. Coca-Cola original 600 ml bottle (replaces the earlier cup presentation in the same catalog slot).
6. Fernet with Coca in a large cup.
7. Beer in a can.

Do not add products or variants without authorization. The condiment table is not a product: customers who bought food automatically may add lettuce, eggplant, tomato, chimichurri, salsa criolla, and onion there; the player never chooses condiments manually.

## Current delivery boundary

The user-authorized street automation milestone replaces the earlier two-product prototype scope: moving automatic workers, mass quantity orders, pre-round prices, delivery-earned coins, immediate hires/speed upgrades, and progressively unlocked locations. Floresta comes first, followed by Nueva Chicago, Argentinos Juniors, Vélez and Ferro; use only the seven products above. Keep catalog IDs stable so existing saved per-product prices remain aligned; Nueva Chicago intentionally offers chori plus the bottle Coca in slot 5, skipping Paty without adding a product. Exact level defaults and acceptance belong in [features/street-automation.md](features/street-automation.md). The prior Floresta prototype remains a historical validation record, not the current feature contract.
