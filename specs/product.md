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

The selector has 15 visible positions: 11 traditional quota/deadline levels (Huracán ID11 became one on 2026-11-27; it was a timed score Bonus Track) and 4 Tower Defense levels (Camioneros ID9, Los Redondos ID10, La Renga ID13, Peregrinación ID14). Stable IDs, campaign order and effective per-level values are centralized in [the balance table](../docs/BALANCE_ACTUAL_NIVELES.md); mode rules belong to [Tower Defense](features/tower-defense.md) and [Huracán](features/huracan-bonus-track.md). Across applicable modes there are catalog-derived fixed-price products, physical stock/cooking and attempt-local hiring/speed; there are no editable pre-round sale prices. Traditional IDs5–8, 10 and 12 use the active-cook/Mozo loop; Huracán's runtime also uses active cooking but its roster and modifiers are bonus-specific. Mercado/Heladera stock and available cash persist; no supplier credit. Hires/speed reset each attempt. Argentinos progression/art is in [Argentinos Juniors](features/argentinos-juniors.md); the standard active-cook flow/layout is in [active-cooks-mozos.md](features/active-cooks-mozos.md); settlement in [market-fridge.md](features/market-fridge.md). The earlier prototype is historical evidence, not current scope. Permanent roster/upgrade unlocks and a 5% earnings mechanic remain pending, not authorized work.

## Tiempo en segundo plano (2026-11-26, a pedido)

Pedido del usuario: "que cuando la aplicación esté en segundo plano continúe andando, avancen los hinchas, el tiempo, todo". Android congela el juego mientras la app no está al frente, así que nada corre ahí; en cambio, **al volver, la partida que se estaba jugando recupera el tiempo que estuvo afuera**. `StreetGame.OnApplicationPause` anota la hora (`DateTime.UtcNow`) al irse y, al volver, si había una ronda en juego (nivel normal en `Playing` o Tower Defense en `Playing`), le debe ese tiempo: hasta **1 hora**; un reloj atrasado no da nada. La deuda se juega en el mismo motor de siempre, unos segundos por cuadro para no congelar la pantalla: niveles normales 30 s por cuadro (`StreetSimulation.Step` en llamadas de 10 s), Tower Defense 10 s por cuadro (`TowerDefenseGameMode.Step` en llamadas de 1 s). Avanzan el reloj, los hinchas, los trabajadores, las entregas y las monedas; si en ese tiempo la ronda se gana o se pierde, se resuelve igual que jugando. Una ronda pausada por el jugador, terminada o abandonada (barrio, selector) no recibe nada. Si Android cierra la app mientras está afuera, ese tiempo se pierde (se retoma desde lo guardado al irse). Evidencia: PlayMode `TimeAwayInTheBackgroundIsPlayedWhenTheAppReturns` (Camioneros: 25 s afuera se juegan en pocos cuadros; una ronda pausada no recibe nada). En el celular: NOT_RUN.
