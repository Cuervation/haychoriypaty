# Product specification

**Hay Chori y Paty** is a portrait, 2D cartoon time-management/arcade game for Android. The player manages a street-food stand outside Argentine football grounds; football results do not affect play. The first location is Floresta, outside All Boys' stadium. Customers stand at the counter or leave; there is no restaurant, indoor stand, or seating.

## Definitive product catalog

Exactly seven products, progressively unlocked:

1. Chorizo sandwich (always on bread).
2. Hamburger-patty sandwich (always on bread).
3. Bondiola sandwich (always on bread).
4. Beef-flank sandwich (always on bread).
5. Coca-Cola original 600 ml bottle (replaces the earlier cup presentation in the same catalog slot).
6. Fernet with Coca and ice in an 800 cc disposable plastic cup.
7. Beer in a can.

**Discrepancia conservada para decisión de producto:** el catálogo runtime nombra `Fernet con Coca 1 L`, mientras el contrato visual describe un vaso de800cc. La auditoría no cambia rótulo, volumen, receta ni arte para forzar coincidencia; aclarar esa nomenclatura requiere autorización.

Do not add products or variants without authorization. The original condiment-table proposal (lettuce, eggplant, tomato, chimichurri, salsa criolla and onion) is not an implemented runtime mechanic. The later user-requested restoration removes the unwanted baked side table; do not add a station or automatic condiment flow without approval. It never enlarges the seven-product catalog.

## Current delivery boundary

The current runtime has eleven ordered clubs, catalog-derived fixed-price products, automatic workers, physical stock/cooking, quota-based victory and in-round hiring/speed. There are no editable pre-round sale prices. From Racing, active cooks produce sandwiches and Mozos deliver them; beverages retain their own workers. Mercado/Heladera stock and available cash persist; no supplier credit. Hires/speed reset each attempt. Exact catalogs, price IDs, goals and deadlines belong in [street-automation.md](features/street-automation.md); Racing+ flow/layout in [active-cooks-mozos.md](features/active-cooks-mozos.md); settlement in [market-fridge.md](features/market-fridge.md). The earlier prototype is historical evidence, not current scope. Permanent roster/upgrade unlocks and a 5% earnings mechanic remain pending, not authorized work.
