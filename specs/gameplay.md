# Gameplay rules

## Current loop

All 11 levels use the per-product quotas and time limits in [street automation](features/street-automation.md#current-per-product-level-balance--2026-10-24); victory requires every unlocked quota, not a total. Fixed ID-based prices are Chori0 $5, Paty1 $5, Bondiola2 $10, Vacío3 $12, Coca4 $5, Fernet5 $12, Beer6 $7. A successful real handoff increments only that product and awards its exact price; saved legacy prices and sale prices cannot alter demand cadence or selection. No price controls are exposed.

Customers have no patience timer/bar or timeout departure; they remain queued until their order completes or the existing sold-out-extras rule releases the lane. Orders retain 1–4 units per product and mix up to five unlocked types; quota deficit/time weights order selection while preserving randomness and variety. Every product enabled by the level catalog may be requested regardless of whether its specialist is hired. Such requests remain pending while the customer waits and can be served after the responsible employee is hired; without that worker, no delivery or revenue is counted. A completed quota remains visible and may generate genuine extra sales. Existing FIFO queues, product stations, physical stock/cooking and specialty assignment remain intact; a player need not clear every customer order. The upper HUD shows all active product progress with real icons, delivered/goal and completion state; results identify completed or pending quotas. Attempt opening coins and level-specific hiring/speed prices follow the balance table, with no carryover from saves or prior attempts. There is no seating or football-result simulation.

Defaults, exact state contracts, persistence and acceptance are authoritative in [features/street-automation.md](features/street-automation.md). The former chori/Coca, shared crowd-patience prototype rules and test record remain historical in [features/floresta-prototype.md](features/floresta-prototype.md), not requirements for the new runtime.


## Five worker roles (current; Racing+ delta 2026-10-15)

Four production specialties/stations derive from the product catalog; the fifth role Mozo additionally requires Racing+ (`LevelIndex >= 5`): Normal Chori/Paty; Premium Bondiola/Vacío on a separate grill; Cocacolero Coca/Cerveza; Fernetero exclusively Fernet. Mixed lines run in parallel with specialty-local ownership/FIFO; customers leave only after all lines complete. The first normal Parrillero is free; each role then has an independent per-level base cost and paid-tier curve. Unlimited hiring for catalog-enabled roles, the speed table and attempt opening balances are defined in the authoritative street-automation contract.


## Racing+ active kitchen (2026-10-15)

[Active cooks and Mozos](features/active-cooks-mozos.md) overrides automatic sandwich production/direct cook delivery only from level 6 onward. Levels 1–5 and beverage delivery retain their existing rules. Physical per-worker cooking takes configured activeCookSeconds (2 s at ×1, scaled by CookWorkRate); production and table placement never earn revenue. Sandwiches follow grill → table → Mozo → customer; Cocacolero/Fernetero still deliver their drinks. All enabled roles have unlimited affordable hiring, but speed has nine +10-point upgrades to ×1.90. Every fresh attempt resets hires/speed; Mercado stock/balance and level progression persist.

ARMÁ TU PLANTEL, permanent worker/upgrade unlocks and persistent 5% earnings are not implemented contracts. Do not add them without a new authorized task; they are distinct from existing level progression and Mercado settlement.
