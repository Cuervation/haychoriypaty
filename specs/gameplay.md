# Gameplay rules

## Current loop

All 11 levels use the per-product quotas and time limits in [street automation](features/street-automation.md#current-per-product-level-balance); victory requires every unlocked quota, not a total. Fixed ID-based prices are Chori0 $5, Paty1 $5, Bondiola2 $10, Vacío3 $12, Coca4 $5, Fernet5 $12, Beer6 $7. A successful real handoff increments only that product and awards its exact price; saved legacy prices and sale prices cannot alter demand cadence or selection. No price controls are exposed.

Each customer has independent patience, refreshed by deliveries; expiry triggers departure and reservation cancellation. Orders retain 1–4 units per product and mix up to five unlocked types; quota deficit/time weights order selection while preserving randomness and variety. Every product enabled by the level catalog may be requested regardless of whether its specialist is hired. Such requests remain pending while the customer waits and can be served after the responsible employee is hired; without that worker, no delivery or revenue is counted, and normal patience/abandonment still applies. A completed quota remains visible and may generate genuine extra sales. Existing FIFO queues, product stations, physical stock/cooking and specialty assignment remain intact; a player need not clear every customer order. The upper HUD shows all active product progress with real icons, delivered/goal and completion state; results identify completed or pending quotas. Attempt opening coins and level-specific hiring/speed prices follow the balance table, with no carryover from saves or prior attempts. There is no seating or football-result simulation.

Defaults, exact state contracts, persistence and acceptance are authoritative in [features/street-automation.md](features/street-automation.md). The former chori/Coca, shared crowd-patience prototype rules and test record remain historical in [features/floresta-prototype.md](features/floresta-prototype.md), not requirements for the new runtime.


## Exclusive worker specialties (2026-10-07)

Product catalog determines specialties/stations, not level number: Normal Chori/Paty; Premium Bondiola/Vacío on a separate grill; Cocacolero Coca/Cerveza; Fernetero exclusively Fernet. Mixed lines run in parallel with specialty-local ownership/FIFO; customers leave only after all lines complete. The first normal Parrillero is free; each role then has an independent per-level base cost and paid-tier curve. Unlimited hiring for catalog-enabled roles, the speed table and attempt opening balances are defined in the authoritative street-automation contract.
