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

The selector has 13 visible positions: 12 clubs plus Huracán as a bonus slot. Eleven traditional club modes use quota/deadline rules; Camioneros ID9 uses Tower Defense (10 finite fans, Chori/Coca only, no quota or level deadline), while Huracán ID11 is a timed score Bonus Track. Argentinos Juniors ID12 is the last traditional club level. Stable IDs, campaign order and effective per-level values are centralized in [the balance table](../docs/BALANCE_ACTUAL_NIVELES.md); mode rules belong to [Tower Defense](features/tower-defense.md) and [Huracán](features/huracan-bonus-track.md). Across applicable modes there are catalog-derived fixed-price products, physical stock/cooking and attempt-local hiring/speed; there are no editable pre-round sale prices. Traditional IDs5–8, 10 and 12 use the active-cook/Mozo loop; Huracán's runtime also uses active cooking but its roster and modifiers are bonus-specific. Mercado/Heladera stock and available cash persist; no supplier credit. Hires/speed reset each attempt. Argentinos progression/art is in [Argentinos Juniors](features/argentinos-juniors.md); the standard active-cook flow/layout is in [active-cooks-mozos.md](features/active-cooks-mozos.md); settlement in [market-fridge.md](features/market-fridge.md). The earlier prototype is historical evidence, not current scope. Permanent roster/upgrade unlocks and a 5% earnings mechanic remain pending, not authorized work.
