# Active cooks and Mozos — requested 2026-10-15

## Contract

LevelIndex >= 5 (Racing through Los Redondos) enables active sandwich production and Mozo delivery, centrally gated. Levels 1–5 retain their existing automatic production, delivery, layout and costs. Stable role IDs append Mozo=4. Existing seven products, goals, duration, demand, speed limits and unlimited hiring remain unchanged. Market stock reservations/debits remain for legacy automatic production and beverages; active-cook sandwiches 0–3 are not backed by Market/Fridge ingredients.

Normal/Premium cooks independently travel to their respective grill, cook one identified unit for 2 / CookWorkRate seconds, carry that same unit along a safe route and deposit it in a reserved available specialty table slot. No automatic sandwich transfer in active mode. Each cooking worker exposes real progress. Mozo claims a cooked sandwich and routes to that reserved unit's actual product/slot on its table (Chori and Bondiola use their left product zones; Paty and Vacío use their right zones), then serves the actual pending customer; only handoff consumes reserved ingredients and records revenue. Beverage roles retain existing behavior. Cancellations preserve edible physical stock; no duplicate IDs, claims, ingredients or sales. Tables bound both prepared and in-flight production.

Active cooks produce continuously without checking customers, order lines or prepared-stock counts. Normal cooks use a fixed shared global turn sequence Chori (0) → Paty (1); Premium cooks independently use Bondiola (2) → Vacío (3). Each cursor is deterministic in worker iteration order and resets on a fresh attempt. If only one recipe in a specialty is enabled, it repeats alone. For active sandwiches 0–3, goals and ingredient stock never gate real cooking; they do not choose or reorder turns. A turn becomes visual-only only when the physical grill/table slots cannot be reserved or its route fails; no alternate recipe is substituted during that turn. Capacity returning resumes real production on the next eligible alternating turn. Active sandwiches create one real transportable unit without Market reservation or debit; sales consume that unique unit exactly once and still record normal revenue/goals. Visual-only cycles never reserve capacity, create food IDs, alter stock/sales/goals/currency, or start delivery. Drinks and levels 1–5 keep their existing ingredient transactions and automatic grill pipeline; beverage preparation and non-cook roles retain the original speed rate.

Mozo is hired only during play, not automatically; starts at zero. Per-level configurable base costs follow existing successive-hire curve, with River reference $60. Every fresh attempt still starts with one Parrillero and speed x1.00. **Racing only (level index 5) also starts each fresh attempt/retry with one free Premium Parrillero; this starter does not consume the first paid Premium hire tier. Levels 7–11 and levels 1–5 keep their existing starting roster.** Save compatibility retained; no persistent earnings/unlocks or new pre-match roster screen.

## Presentation

Reuse existing furniture, counter, mural, fans, HUD, art, fonts, wood/iron cards and directional worker infrastructure. From Racing remove lower pennants, lower cream footer and both grills, extend existing tiled floor and approximately double actual grill-to-table circulation without resizing/distorting furniture. Geometry, projection, pickup/work anchors and obstacles share the same layout. Six cards: Speed/Normal/Premium then Coca/Fernet/Mozo. Per-worker circular progress only while cooking. Mozo uses an adapted existing worker outfit and real carried unit. Earlier levels remain unchanged.

### Direct employee movement — 2026-10-18

The 2026-10-17 direct movement behavior is restored in `MoveWorker`: each employee moves straight toward the current waypoint at `balance.workerSpeed * WorkRateFor(role)`. There is no look-ahead against other workers, separation radius, worker-ID right-of-way, lateral detour, or yield slowdown; crossing characters may overlap briefly. Fixed furniture avoidance remains in `StreetKitchenLayout.FindShortestSafeRoute` and each movement segment still follows that precomputed safe route. Distinct station/work/wait points and traffic-aware selection of an available counter handoff remain intact; neither changes a worker's mid-route movement.

Focused Unity EditMode validation in an isolated copy (6000.6.3f1): 10/10 passed, covering idle-worker crossing, opposing crossings at the Parrillero +5% / Cocacolero +10% role speeds, three Mozos crossing, Parrillero carry/deposit, product-specific Mozo pickup, mixed deliveries, and routes around fixed worker-foot-expanded furniture. The full `StreetActiveCooksTests` fixture passed 52/54; only the pre-existing `UnassignedWorkerReturnsToItsOwnSafeWaitingPoint` cases (Parrillero level 0 and Mozo level 5) failed as in the prior run. Unity MCP at `127.0.0.1:8080` was unavailable; no Game View test was run.

## Validation / acceptance

- [x] Active per-worker cooking, one real carried/deposited unit and speed scaling.
- [x] Continuous active cooking with fixed shared global 0→1 and 2→3 alternation; completed goals and ingredient stock never block real sandwiches. Physical slot capacity alone can defer a real unit; blocked turns preserve the recipe and resume automatically without fallback.
- [x] Each speed upgrade gives Parrilleros +6% and other roles +10%; nine upgrades cap at ×1.54/×1.90. Cooking, movement and worker operations use the role-specific rate; legacy automatic levels retain their flow and beverage timing.
- [x] Parrilleros del mismo rol usan puestos de parrilla separados al cocinar en paralelo; EditMode cubre separación y rutas seguras. Revisión visual de este ajuste sigue pendiente.
- [x] Los handoffs seleccionan un punto libre del mostrador que evita trabajadores estacionados o en ruta; la espera usa una grilla de seis columnas con filas escalonadas. Las regresiones focalizadas de dos filas, cuatro puestos/cola, acceso del Mozo y entrega mixta pasaron 17/17 EditMode el 18/10/2026.
- [x] Revisión 18/10: anchors de cocción, separación, claridad idle/cocción, agarre y slots de cuatro sándwiches cubiertos por 11 EditMode + 14 PlayMode focalizados. La nueva regla de cuatro puestos/cola rotativa abajo reemplaza la fila escalada para equipos densos.
- [x] Capturas actuales de Game View a 1080×1920: Normal/Premium, tres y cuatro cocineros en cocción, dos fases de brazos y transición de unidad real a transporte; contacto de pinzas y parrilla revisado visualmente el 18/10/2026.
- [x] Exclusive Mozo sandwich handoff and unchanged beverage/mixed-order flow.
- [x] Mozo routes to the actual reserved table slot for Chori, Paty, Bondiola, and Vacío (focused EditMode suite passed 19/19 on2026-10-16; live visual route review remains pending).
- [x] Concurrent claims, finite physical capacity and cancellation; active sandwiches bypass ingredient reservations/debits, while legacy products and beverages conserve stock transactionally.
- [x] Role hiring/costs/save compatibility; earlier-level regressions.
- [x] Unified expanded geometry, original assets, six responsive cards and circular progress.
- [x] Unity compilation and focused tests.
- [x] Actual Unity captures: Level 8, partial cooking, transport, deposit, Mozo pickup/handoff; compact/tall/safe-area layouts.
- [x] Quantified throughput/balance assessment without changing goals/duration/economics.

Task is not complete until runtime validation and real visual evidence are recorded.

- [x] Parrilleros normales/Premium siguen en un ciclo visual sin unidad real si no se puede reservar producción; regresiones focalizadas cubren metas completas, mesas/ingredientes bloqueados, inventario inalterado y reanudación automática.
- [x] Captura de Game View con cocción simulada sin ingredientes, turnos visuales de los cuatro productos y cuatro cocineros simultáneos; progreso/brazos revisados visualmente (2026-10-19).

## Alternancia fija de cocción — 2026-10-19 (regla histórica supersedida el 2026-10-20)

Reemplazó el virtual-finish ponderado por metas: dos cursores por intento, uno normal (0↔1) y otro Premium (2↔3), compartidos por todos los trabajadores de la especialidad. La regla de stock/cuota de esta versión histórica fue sustituida por la producción continua descrita abajo; los niveles1–5 permanecen en su pipeline automático.

**Validación Unity 6000.6.3f1:** 12/12 EditMode focalizados pasaron: secuencias con 1–5 workers (incluida cola), alta de trabajador durante la ronda, uso de receta configurada en niveles6–11, niveles1–5 sin modo activo, único producto habilitado, cuota terminada, mesa/stock sin capacidad, visual→real y ausencia de efectos en inventario/metas/ventas. PlayMode 2/2: captura existente de uno a cuatro cocineros en cocción/transporte y captura nueva sin stock que comprobó/capturó los cuatro turnos visuales y cuatro Parrilleros simultáneos; no cambió el guardado del jugador ni se guardó escena. Capturas reales a1080×1920 en `Logs/Acceptance/FixedCookAlternation-20261019/`. Las parrillas conservan su arte ambiental lleno y no identifican visualmente la receta individual; la asignación exacta se verificó con estado de simulación/test, no con una etiqueta añadida.

**Compatibilidad de campaña:** la auditoría secuencial de save nuevo ganó niveles1–5, pero perdió Racing (nivel6) con las tres variantes existentes de política de contratación (factores1.2/1.5/2.0), dentro de255s. En el mejor de esos intentos, ventas/productos fueron Chori121/210, Paty121/145, Bondiola69/80, Vacío67/75, Coca140/175, Fernet72/75 y cerveza74/90; por tanto, esta regla muestra un riesgo de ganabilidad real bajo esa política. No demuestra imposibilidad con toda estrategia o equipo y no prueba niveles7–11 en una campaña fresca. No se modificaron balance, objetivos, precios, stock, tiempos ni economía; se debe conservar este límite explícito hasta una prueba de estrategia adicional/autorización de balance.

El factor de abajo es la valoración de speed-up usada por la política adaptativa, **no** un multiplicador de velocidad del juego. Productos y faltantes están en IDs0–6 (Chori, Paty, Bondiola, Vacío, Coca, Fernet, cerveza):

| Valoración de la política | Resultado Racing | Vendidos | Faltantes al vencer |
|---|---|---|---|
| 1.2 | Derrota, 255 s | 121, 121, 69, 67, 140, 72, 74 | 89, 24, 11, 8, 35, 3, 16 |
| 1.5 | Derrota, 255 s | 112, 115, 69, 57, 136, 72, 73 | 98, 30, 11, 18, 39, 3, 17 |
| 2.0 | Derrota, 255 s | 118, 120, 67, 61, 138, 68, 73 | 92, 25, 13, 14, 37, 7, 17 |

## Ajuste vigente de cocción activa — 2026-10-16

A pedido del usuario, desde Racing+ Parrillero y Premium cocinan cada unidad en **2 / WorkRate segundos** (2 s a velocidad de trabajo ×1). El balance predeterminado y `Main.unity` guardan explícitamente 2 s. Niveles 1–5 conservan su `cookSeconds=4`; el ajuste previo de 1,3 s queda supersedido. La regresión de duración prueba el umbral de 2 s y el escalado por WorkRate; `StreetActiveCooksTests` pasó **19/19 EditMode** el2026-10-16. La APK0.2.42/code44 ya está instalada en Motorola Edge60Fusion; no se ejecutaron PlayMode ni revisión visual en vivo y el juego no se abrió. Detalle de build/firma en [android-prototype.md](android-prototype.md).

### Ajuste previo supersedido — 2026-10-16

El pedido anterior fijó temporalmente la cocción activa en 1,3 / WorkRate segundos. Fue reemplazado por el ajuste vigente de 2 / WorkRate segundos; no cambian los 4 s base de niveles 1–5.

## Ajuste de proximidad de trabajadores — 2026-10-16

A pedido del usuario, el Parrillero se ubica 8 puntos más cerca de la parrilla: `GrillWorkClearance` pasó de 14 a 6 puntos. En la revisión del 18/10/2026 se reduce a 2 puntos. La posición se interpola horizontalmente para cualquier tamaño de equipo en una única fila, sin filas interiores; la escala visual baja solo en equipos densos. El Cocacolero conserva el acercamiento de 2 puntos al frente de los barriles: `BarrelPickupClearance` pasa de 2 a 0; Coca y Cerveza comparten esta geometría y la prueba confirma tangencia sin superposición, además de rutas seguras.


### Puestos de Parrilleros separados — 2026-10-16

Cada Parrillero/Premium recibe un puesto estable, calculado por su orden dentro de su equipo y no por el slot del alimento. La implementación inicial colocaba los dos primeros en extremos, el tercero al centro y escalonaba equipos mayores en filas adicionales. Ese patrón quedó reemplazado el 18/10/2026 por anchors interpolados en una sola fila horizontal; véase la revisión visual vigente abajo. La regresión histórica del flujo simultáneo verificó más de 120 puntos entre los dos primeros. `StreetActiveCooksTests`, `StreetKitchenPresentationTests` y `StreetSpecialtyPresentationTests` pasaron **55/55 EditMode** en copia aislada con Unity6000.6.3f1.

## Legibilidad visual de cocineros — 2026-10-18

La revisión visual mantiene los anchors de parrilla en una sola fila para equipos 1–9, a 2 puntos del borde de parrilla; el proyector adapta tamaño por densidad y agrega orden estable para el empate de profundidad. Normal y Premium idle en el grill se ven al frente con respiración leve; si un equipo está idle en la mesa de depósito, se separa visualmente allí sin alterar posición ni rutas de simulación. Los tamaños de slot completos quedan dentro de la banda superior de madera para Chori, Paty, Bondiola y Vacío, tanto con producto compañero como sin él. Validación focalizada: 11 EditMode + 14 PlayMode pasaron con Unity6000.6.3f1; la captura/revisión manual de gameplay no se pudo hacer en esta sesión.

### Diagnóstico de circulación y contacto visual — 2026-10-18

La inspección focalizada encontró movimiento directo sin evasión dinámica y rutas seguras de muebles intactas. En escenarios instrumentados, los trabajadores con ticket avanzaron, completaron la entrega, liberaron cliente/reserva y volvieron a su ancla; dos especialistas del mismo rol toman tickets elegibles en paralelo. La inmovilidad observada en personal sin ticket es espera legítima. No se reprodujo un bloqueo de producción/asignación, por lo que no se modificaron estados, reservas ni mecánicas de simulación. Regresión EditMode cubre Cocacolero, Fernetero y Mozo con dos empleados y tres pedidos simultáneos por especialidad.

El grill `empty-grill.png` tiene 62 px transparentes arriba de su borde visible dentro de 724 px. `StandardGrillHeight` incluye la escala adaptable del layout (83,37 puntos a 540 de ancho), así que la franja aparente equivale a unos 7,14 puntos, no 10,05. `StreetWorkerProjection` compensa esa franja solo en la pose `WorkingAtGrill`, hacia abajo en coordenadas de pantalla: pies visuales quedan junto al borde sin mover la posición lógica, `GrillWorkClearance=2f`, la cola (~55 puntos), rutas ni pinzas/arte. PlayMode verifica el anclaje en escalas verticales 960 y 1200; capturas Unity 1080×1920 antes/después disponibles en `/private/tmp/hcyp-parrillero-before.png` y `/private/tmp/hcyp-parrillero-after.png`. Son capturas reales del proyector aislado, no de una partida completa.

### Verificación de distancia a parrillas — 2026-10-18

Se confirmó que el margen visual también incluye 24 px transparentes bajo los pies en dos cuadros del atlas de cocción y el ajuste de aspecto del sprite de parrilla. La proyección compensa ambos solo al dibujar a quienes están en `WorkingAtGrill`; conserva posiciones lógicas, colisiones y rutas. Una captura real de Game View a 1080×1920 con simulación temporal mostró al Parrillero y Premium junto a sus respectivas parrillas, con la comida visible en el punto de cocción. La escena temporal no usó ni escribió el guardado del jugador.

La línea horizontal restante se rastreó a la junta de pastina dibujada en el fondo compartido (`street-background-open-street-v4.png`, alrededor de la fila fuente 1074), no a filtering ni a una costura entre paneles. La textura es 940×1673, Bilinear/Clamp, sin mipmaps y sin compresión. Un parche local de pocos píxeles produjo una franja clara visible en Game View; se revirtió para no empeorar el arte. Por lo tanto, la distancia de cocineros queda corregida, pero la eliminación de esa junta queda pendiente de una edición/copia de arte que no afecte la textura compartida de los demás niveles. La prueba focalizada de EditMode compiló, pero Unity MCP se desconectó/inicializó mal al ejecutarla; no se declara pasada en esta revisión.

## Resultado y validación — 2026-10-15

**Mecánica y presentación implementadas; no se declara cerrado el balance de niveles 9–11.**

- Unity 6000.6.3f1: compilación sin errores; 71/71 EditMode, 7/7 PlayMode. Tras las correcciones visuales se repitieron solamente 35/35 checks afectados y 20/20 de animación. No sumar estas repeticiones como pruebas únicas. Los fallos de iteración anteriores fueron corregidos (ruta bloqueada, reflexión del fixture y expectativas heredadas de stock/layout/coords); no se cuentan como aprobados.
- Cocción: 0.5s a WorkRate 1, avance real independiente, producto identificado único; unidad 70 observada Grill→Carried→Table0→Mozo2→consumida. Cocinar/depositar dejó ventas 0 / $0; entregar produjo exactamente 1 venta / $5. Dos cocineros simultáneos con IDs 70/72 y progresos independientes, mientras Premium transportaba 71.
- Stock/ingredientes/cancelación, cuatro sándwiches, bebidas/pedidos mixtos, compatibilidad v4, costos, compras mouse/touch y reinicio de todas las composiciones cubiertos por los fixtures focalizados.
- Layout: separación física de 70, parrillas bajadas 66, piso hasta 735, misma escala/perspectiva de muebles. Normal/Premium y barriles quedaron en sitio; en esa iteración Fernet subió85 para abrir paso (**revertido por la restauración visual documentada abajo**). Profundidades activas ordenadas por pies, no por un orden fijo delante de trabajadores; anteriores conservan su renderizado. Mozo adapta el delantal del atlas original, sin redibujar piel/cara ni duplicar comida.
- Capturas reales: 1080×1920 (circuito completo),1440×2960 y320×568; seis botones completos, sin banderines. Geometría responsive y safe-inset probada; notch/táctil/rendimiento de dispositivo Android físico **no verificados**.
- Play detenido, Main limpio, JSON original de progreso restaurado exactamente, timeScale1/runInBackgroundfalse y selección GameView19 restablecidos; preset QA temporal eliminado. No APK/commit/push. Ningún PNG original, prefab o `.meta` existente fue reemplazado.

**Evidencia:** `Logs/Acceptance/ActiveCooks-20261015/Final/README.md` (galería), `validation.json` (jobs/resultados/balance/SHA256).

## Balance medido (no retocado)

Seed 1337, recetas finitas exactamente para las metas, grant inicial oficial y contrataciones/mejoras pagadas con ventas reales. Estrategia automática no óptima: decide cada 0.5 s, cubre roles faltantes, ahorra para la compra de mayor reducción marginal de carga por costo; prioriza Mozo y evita comprar cocineros cuando hay stock. No altera metas, precios, stock gratuito, velocidad máxima ni demanda.

| Nivel | Resultado / tiempo oficial | Productos vendidos0,1,2,3,4,5,6 |
|---|---|---|
| Racing 6 | Victoria 208.61/255s | 210,145,80,75,175,75,90 |
| San Lorenzo 7 | Victoria 252.06/270s | 230,165,100,85,195,80,105 |
| River 8 | Victoria 280.75/295s | 250,180,110,95,210,90,115 |
| Boca 9 | Derrota 300s | 260,194,120,110,217,100,125 |
| Camioneros 10 | Derrota 300s | 241,208,135,125,215,110,140 |
| Redondos 11 | Derrota 315s | 311,240,150,140,268,120,165 |

River completa 635 sándwiches y 415 bebidas con 4 Normal / 3 Premium / 6 Mozos / 5 Coca / 2 Fernet, velocidad ×1.6; ventas $7325 y saldo $415. La política fallida sin ingredientes finitos sobrevendía otros productos: no es una medición válida del flujo real de Mercado.

**Riesgo pendiente:** los fracasos9–11 no demuestran imposibilidad matemática ni certifican balance humano. Propuesta acotada, **no aplicada**: probar solo duración 320/335/335 s (+20/+35/+20). En clones de configuración esa hipótesis ganó en 313.50/328.84/330.19 s con las mismas metas y economía. Es una primera alternativa pequeña comprobada para esta estrategia, no el mínimo global; requiere autorización antes de tocar configuración oficial. La duración influye también en la ponderación temporal de demanda existente.

- [ ] Aceptación final de balance9–11 (o autorización de ajuste).
- [ ] Validación Android física/cutout/performance; fuera de esta validación de editor, sin nueva APK.

## Archivos de esta implementación

Runtime: `Assets/Scripts/Core/StreetSimulation.cs`, `StreetKitchenProduction.cs`, `StreetSpecialties.cs`, `StreetKitchenLayout.cs`, `StreetGame.cs`; `Assets/Scripts/UI/StreetView.cs`, `StreetKitchenRenderer.cs`, `StreetWorkerProjection.cs`, `StreetWorkerAnimation.cs`, `StreetWorkerAppearance.cs`.

Tests nuevos: `Assets/Tests/EditMode/StreetActiveCooksTests.cs`, `Assets/Tests/PlayMode/StreetActiveCooksPointerTests.cs` (con `.meta` nuevos). Regresiones adaptadas: `StreetKitchenPresentationTests.cs`, `StreetSpecialtyDomainTests.cs`, `StreetSpecialtyPresentationTests.cs`, `StreetPointerTests.cs`. Specs: índice, gameplay, arquitectura, decisiones, estado, modular-kitchen, worker-animation y esta especificación. Cambios previos de Mercado/StreetArt/otros fixtures se conservaron y no se atribuyen a esta tarea.

## Restauración visual solicitada — 2026-10-15

El usuario confirmó: **solo el escenario; mantener cocineros y Mozos**. Se conserva la ampliación inferior, pero Fernet vuelve exactamente a la fila original, con los mismos bounds que en modo anterior. Los cocineros aparecen del lado de las parrillas y depositan desde abajo; los Mozos retiran desde el lado del mostrador. Así no se desplaza un mueble ni se atraviesa la pared de bases de la fila completa.

El mural activo queda limitado al plano lejano, dejando al menos90 puntos escalados de calle antes del mostrador. La mesa lateral de chimi estaba horneada en el fondo ancho; la composición usa la mitad limpia original del mostrador/piso reflejada a la derecha, sin modificar/generar PNG, muebles o prefabs. Niveles1–5 conservan su composición; no se cambió economía, metas, tiempos de cocción, stock o responsabilidades.

**Validación final:** Unity6000.6.3f1,35/35 EditMode (cocineros/stock/roles, fila original en6–11, rutas de cada slot, calle y proyección responsive), job`38401aada89e48679e46e9bb5978404a`. El reintento PlayMode de3 pruebas de puntero **no ejecutó casos**: el Test Framework lanzó NullReferenceException en PlayModeRunTask.cs:52; no se declara aprobado. Main se restauró y se eliminó únicamente su escena temporal generada.

Capturas nativas revisadas de River1080×1920 y Racing1440×2960, en `Logs/Acceptance/SceneRestoration-20261015/README.md`. En River se observaron Normal cocinando, Premium transportando, Mozo llevando unidad74 y2 ventas reales después de6s. Consola de la escena sin errores/warnings. Play detenido/Main limpio, JSON de progreso restaurado exactamente, GameView19/timeScale1/runInBackgroundfalse. Sin APK/instalación/commit/push; Android físico no verificado. Las cifras de balance anteriores son históricas, previas a esta restauración de geometría; no se repitió una auditoría de campaña.


### Auditoría posterior a la restauración — 2026-10-16

Nueva campaña real cash-only/lifecycle ganó1–9 sin alterar configuración; varias políticas perdieron10, mejor final13Chori pendientes a300s con saldo suficiente (no un bloqueo de Mercado).11ganó302,9/302,7s en dos pruebas separadas desde los desbloqueos existentes del JSON auténtico del teléfono; no acredita campaña fresca ganadora1–11ni QA humano. La aceptación final de balance sigue abierta, ahora concentrada en10y recorrido completo. Ver [status.md](../status.md) y `Logs/Acceptance/EconomyAudit-20261015/economy-audit.md`; no aplicar las duraciones hipotéticas históricas sin autorización.


## Cocción visual permanente — 2026-10-18

En Racing+ cuando no se puede reservar una receta por meta completa, falta de ingredientes o capacidad, cada Parrillero permanece junto a su parrilla en modo visual: los brazos reutilizan el mismo atlas, el anillo completa ciclos repetidos y la receta cosmética sigue los objetivos proporcionales mediante contadores separados. El modo no reserva ni crea StreetFoodUnit, no toca ingredientes/stock/ventas/metas, ni inicia viajes falsos. Al liberarse recursos vuelve a reservar producción real automáticamente. La fase de brazos se conserva al alternar modos. El humo y calor/brasas ambientales existentes ya se reciclan por parrilla y continúan activos; no se añaden efectos ni objetos por ciclo. Niveles1–5 conservan su pipeline automático y no entran a este modo activo.

**Validación:** Unity6000.6.3f1, StreetActiveCooksTests focalizado **31/31 EditMode**, incluyendo stock en cero, objetivos completos, mesas de las cuatro recetas llenas, inventario/unidades sin cambios durante ciclos visuales y recuperación de stock/capacidad; git diff --check pasó. Intenté captura nativa Game View en una sesión temporal sin escritura de guardado, pero el editor mantuvo la portada «Jugar con Google» aun cuando la simulación estaba inyectada y introActive=false; no se obtuvo evidencia visual del gameplay y no se marca aceptada la captura.

## Cuatro puestos y rotación de Parrilleros — 2026-10-18

Cada parrilla normal/Premium administra cuatro puestos físicos independientes. Los primeros cuatro cocineros ocupan la fila a escala visual normal; los demás quedan en una zona de espera visible con idle leve y sin progreso/producto real. Al salir hacia su mesa un cocinero libera inmediatamente su puesto; el trabajador con el ticket de espera más antiguo camina hasta él. Al depositar, el primero vuelve al final de la cola. Las colas y los identificadores de puesto son independientes por tipo de parrilla; el quinto y siguientes nunca cocinan ni reservan unidades sin un puesto. `GrillWorkPosition` mantiene los cuatro anchors dentro de bounds reales, reservando el ancho visible del personaje para que Premium no cruce al grill normal. Parrillero/Premium ya no reducen escala cuando se contratan más; Mozos y roles de bebidas no cambian.

**Validación de implementación:** se añadieron regresiones de 1/2/3/4/5/6/8 workers por cada tipo, límites y exclusividad de puestos, escala constante, rutas de cola→parrilla/mesa→cola y promoción de un quinto trabajador mientras se transporta una unidad real. Unity 6000.6.3f1 compiló runtime y EditMode (Tundra build success), pero el job EditMode se desconectó antes de reportar sus resultados y `TestResults.xml` conservó la fecha anterior; no se marca ningún test aprobado. Las capturas requeridas de cinco Parrilleros normales/Premium siguen pendientes por la desconexión del Editor.

## Carne ambiental en parrillas activas — 2026-10-18

En los niveles 6–11, StreetKitchenRenderer mantiene una pieza ambiental reutilizable en cada slot de parrilla que no esté ocupado por una unidad real. El pool usa GrillCapacity, GrillSlotPosition y GrillMeatSize; por eso conserva 18 posiciones normales / 7 Premium y los repartos 12+6 y 4+3 según el catálogo. Cada pieza alterna de manera desfasada entre los sprites Raw, Cooking y Cooked existentes; no se usa el estado Burned. Al reservar una unidad real, solo se oculta su par (producto, slot); al liberarse, vuelve a mostrarse. Las piezas ambientales no son StreetFoodUnit, ni se incluyen en inventarios, reservas, mesas, ventas, tiempos o economía.

- [x] Presentación separada del gameplay, pool por slot y estados reutilizados sin instanciación por ciclo.
- [x] Unity 6000.6.3f1: verificación runtime aislada confirmó slots 18+7, niveles clásicos sin cambios, parrillas llenas sin stock/mesa disponible, enmascaramiento de slot real, progreso de cocción conservado y retorno ambiental al transportar; sin alterar unidades ni monedas.
- [x] Capturas reales del proyector aislado antes/después en `Assets/Screenshots/grill-ambient-before-qa.png` y `Assets/Screenshots/grill-ambient-after-qa.png`; no representan una partida completa.
- [ ] El PlayMode focalizado no llegó a inicializar casos: Unity Test Runner expiró a 120 s (`TestJobManager failed to initialize`). La prueba permanece pendiente y no se marca aprobada.

## Cierre visual parrillero–parrilla — 2026-10-19

Para corregir el espacio visible que persistía después de compensar el borde aspect-fit, la proyección de un Parrillero estacionado agrega `GrillVisualContactOffset = 10` unidades hacia la parrilla, además del ajuste del sprite y del margen transparente por frame. Es exclusivamente de render: `GrillWorkPosition`, puestos, obstáculos y rutas conservan el clearance lógico de 2. La regresión Unity PlayMode pasó 1/1: proyectó de 1 a 4 cocineros a 1080×1920 y 1080×2400, y produjo capturas reales de la cámara nativa de cocina (`/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-parrillero-cooks-3-960.png`). La captura confirma el contacto visual para Normal y Premium, aunque no es una partida/Game View completa. El EditMode focalizado no llegó a iniciar casos y expiró al inicializar; no se marca aprobado.

### Reajuste tras captura real de Racing — 2026-10-19

La captura de Racing compartida por el usuario precede el ajuste final a 20 puntos y no sirve como resultado posterior; por eso parecía igual. La corrección sigue siendo solo de render: las posiciones lógicas, las rutas seguras y el clearance de 2 puntos no cambian. Se validó el código actual en una copia aislada con Unity 6000.6.3f1: `HayChoriYPaty.Tests.StreetWorkerProjectionTests.CookingPosesReplaceOriginalBodyAndStayAnchoredForOneToFourCooksAtTwoPortraitScales` pasó 1/1 y generó capturas de cámara reales a 1080×1920 y 1080×2400, para uno y cuatro cocineros, guardadas en `Logs/Acceptance/GrillContact-20261019/`. La revisión muestra manos/utensilio junto a la superficie de cocción y el cuerpo solapado naturalmente con el borde posterior; es una cámara de cocina aislada, no una partida completa. Unity MCP sigue sin responder al ping; la Game View completa queda pendiente.


## Recorrido directo y contacto progresivo — 2026-10-19

Al asignarse una cocción real, Parrillero/Premium reserva primero la unidad y calcula desde su posición actual una ruta segura directamente al anclaje alineado con ese slot. La selección del slot conserva como referencia el puesto asignado, evitando cambiar prioridades al quitar el waypoint general. Si no hay unidad real disponible, se conserva la espera/cocción visual y la promoción de cola. Reservas, cuatro puestos/cola, obstáculos, velocidad, producción y entrega no cambian.

El anclaje visual del borde de cocción ahora interpola por distancia recorrida de ruta (SmoothStep durante los últimos/primeros 60 puntos lógicos en la ida y salida). Esto evita aplicar de golpe el offset completo al cambiar de estado y respeta los desvíos por muebles; no cambia el clearance ni las posiciones lógicas. El ajuste de pies transparentes del atlas de cocción queda separado.

**Validación Unity 6000.6.3f1:** EditMode focalizado **23/23** (incluye destino directo para Normal/Premium en Racing y nivel posterior, entrega real, concurrencia, 4/5 puestos/cola y retorno desde cocción visual); PlayMode **3/3** (continuidad aproximación→cocción→mesa, test previo de reserva/slot y poses con 1–4 cocineros en dos escalas); una regresión adicional de entregas mixtas/retorno pasó **1/1** tras reintentar una inicialización de Test Runner sin casos. Capturas Unity reales: cámara aislada de cocina en `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-cook-approach-after.png`, `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-cook-animation-after.png` y `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-cook-table-departure-after.png`; Game View completo de `StreetView` + simulación Racing temporal en `/private/tmp/hay-chori-gameview-racing-after-clean.png` (1080×1920, cuatro Parrilleros y un Premium; sin errores nuevos en consola). El objeto/simulación fueron temporales, el `StreetGame` persistente estuvo desactivado y no se escribió el guardado; la escena abierta siguió limpia. El video de referencia se revisó como miniatura de Quick Look, no como muestreo de los intervalos 6–12/18–24/28–36s. No hay una captura Game View equivalente anterior al cambio; la miniatura del video es solo referencia, por lo que no se afirma una comparación antes/después alineada.

## Anillos de cocción anclados al pelo — 2026-10-20

La posición ya no presupone `StreetWorkerProjection.Height * verticalScale`. Para los cuatro frames reales de cocción usa el alto alpha-visible del recorte y su escala de ajuste del proyector; los insets superiores medidos en `street-parrillero-cooking-arms-v1.png` son 0, 0, 6 y 5 px. Suma además el desplazamiento de raíz que el proyector aplica a los pies en los dos frames inferiores. Con ello el borde exterior inferior del anillo queda a 4 unidades lógicas del pelo en cada fase, también con las escalas verticales probadas; el indicador sigue el cambio de pose en vez de usar una altura corporal teórica. No cambia tamaño, tiempos ni movimiento. Los puntos activos son blanco puro `#FFFFFF` para Parrillero y azul `#168BFF` para Premium; el arco inactivo conserva el fondo oscuro discreto.

**Validación Unity 6000.6.3f1:** compilación del runtime completada (Tundra build success). EditMode focalizado **4/4**: separación exacta para cada pose a escalas `.8/1/1.25` y colores RGB. PlayMode de Game View **1/1**: ocho capturas reales `StreetView`, una Normal, una Premium, cuatro Normal, cuatro Premium y mezcla de ambos equipos, a 540×960 y 540×1200; las capturas muestran los anillos separados de cabezas/caras y sin superposición entre círculos. El fixture emitió la advertencia habitual de falta de `AudioListener`, sin errores de compilación/código. Evidencia en `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-sandwich-cooking-ring-*.png`. Unity MCP tuvo intermitencias de ping, pero se seleccionó la instancia correcta, se refrescaron/compilaron scripts y se completaron estas pruebas/capturas; un reintento posterior se desconectó antes de devolver resultado, sin afectar el primer pase ni las capturas. No se reinició el Editor.


## Producción real continua de sándwiches — 2026-10-20

Desde Racing (`LevelIndex >= 5`), los cuatro productos de parrilla Chori/Paty/Bondiola/Vacío continúan produciéndose como unidades reales incluso después de completar cada cuota y con cero ingredientes en Mercado/Heladera. La alternancia global por especialidad se conserva; producción solo espera si no hay slots físicos simultáneamente libres en mesa/parrilla, y reanuda automáticamente sin perder el producto del turno. No se crean unidades ficticias durante los ciclos visuales.

`StreetKitchenProduction.HasSupply` mantiene elegibles los pedidos activos de sándwich sin consultar el stock; `CanReserveCook` solo comprueba capacidad; alta/deposito y consumo de la unidad omiten reservas/debitos de ingredientes exclusivamente para IDs 0–3 con cocción activa. El producto se entrega y contabiliza una vez mediante su ID físico. El cierre de intento también evita reportar ventas de esos cuatro productos como consumo falso de stock. Bebidas, Mercado/Heladera/UI, niveles1–5, metas, precios, tiempos y victoria conservan su comportamiento. La demanda mantiene productos disponibles tras su meta (`ProductDemandWeight` asigna el peso de extras vigente).

**Validación focalizada 2026-10-20:** compilación Unity Tundra/runtime y ensamblado EditMode completados. El runner del Unity MCP no inicializó casos (`completed=0`) tras el timeout; se recuperó la ruta de validación mediante Unity CLI 6000.6.3f1 en una copia aislada del proyecto. Los cinco EditMode focalizados pasaron 5/5: alternancia después de cuota, producción real de los cuatro sándwiches con stock cero/cuotas cumplidas, transacciones sin ingredientes, ventas post-meta con unidad real en mesa y handoff Mozo una sola vez, y pausa/reanudación al liberar slots. Además, la estrategia real de Racing `L06_forward_bulk_adaptive_bottleneck_D_checkEvery2Ticks_seed17_carry20pct` pasó 1/1 y verificó las ventas/stock asentados. No se ejecutó PlayMode/Game View ni se alteró la escena/guardado del editor principal; véase `specs/status.md`.


## Retiro del último sándwich depositado — 2026-10-20

Desde Racing, al asignar un nuevo retiro al Mozo, se reserva la unidad cocida **más recientemente depositada y disponible del producto solicitado**: Chori, Paty, Bondiola o Vacío. El orden se basa en un contador de depósitos real de la cocina, no en ID/creación/inicio de cocción; dos cocineros pueden llegar en orden inverso. No se cambia la selección de productos del pedido.

Una reserva ya en curso permanece estable mientras el Mozo camina: nuevas unidades no le cambian el destino ni le quitan su unidad a otro empleado. Conserva el slot físico, rutas, producto e ID hasta el handoff; cada unidad se consume/vende una vez. Liberar una reserva sin retirar no altera el orden. Un sándwich devuelto a la mesa vuelve a ser el último depositado, preservando el comportamiento existente de devolución y su slot/capacidad. El contador solo vive en el intento y no cambia guardados.

Bebidas y producción/entregas de niveles1–5 conservan el orden previo. No se modificaron precios, producción, tiempos, arte, slots ni capacidades48por producto.

**PASS:** compilación Unity6000.6.3f1 y **14/14 EditMode focalizados**, job`0272fc59af224c089f88aa616041f48d`: cuatro productos con depósitos fuera del orden de creación; reservas exclusivas e idempotentes ante depósitos nuevos; cancelación/devolución; FIFO legacy/bebidas; y cuatro simulaciones de ruta real hasta el slot de la última unidad, transporte del mismo ID y entrega única al cliente. No PlayMode/GameView ni benchmark físico en esta tarea. Unity MCP tuvo ping/estado intermitente; telemetría y consulta directa confirmaron el proyecto y permitieron compilar/probar sin reiniciar.

Implementación local posterior a la APK0.2.56/code58 de limpieza sin pérdida: **esa APK todavía no incluye este cambio de retiro**. No build, instalación o push solicitados/ejecutados para LIFO.


## Plantel inicial Premium en Racing — 2026-10-21

A pedido del usuario, cada partida nueva o reintento de Racing comienza con un Parrillero normal y un Parrillero Premium gratis, conservando velocidad ×1,00. El Premium inicial no incrementa el tier de precio: el primer Premium contratado durante la ronda cuesta el costo base configurado para ese rol/nivel. La excepción se limita al nivel 6; los demás niveles conservan la plantilla de entrada previa.

**Validación 2026-10-21:** `OnlyRacingStartsWithFreePremiumAndItsNextHireUsesBaseCostAcrossRetries` 1/1 y `RacingWithoutMozoCanCookButCannotSellOrWin` 1/1. Se jugaron cinco rondas completas de Main a ×1 y por acciones legales de UI; 0/5 victorias. El mejor vendió197 Chori (vs191 del mejor anterior), faltaron13 productos en total (vs19); índice comparativo0,957918 vs0,929265. Promedio de faltantes24,4 vs51,04 sobre25 rondas previas. El plantel inicial fue N/Premium=1/1. El plan se adaptó entre intentos, por lo que la mejora no atribuye causalidad al regalo solo ni resuelve la ganabilidad. Suite completa activa:82 ejecutados,78 pasaron,4 fallaron en aserciones de visual-cooking/ingredientes activos y fixtures de retorno; no se declara esa suite aprobada. PerfilQA legado restaurado exactamente; ver informe y ledgers locales en `/Users/celestino/.codex/visualizations/2026/10/21/01a15bdf-e29b-7253-8456-9b392d7045a3/racing-default-premium/INFORME.md`. No APK/dispositivo/Firebase/commit/push.
