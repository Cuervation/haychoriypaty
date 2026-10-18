# Active cooks and Mozos — requested 2026-10-15

## Contract

LevelIndex >= 5 (Racing through Los Redondos) enables active sandwich production and Mozo delivery, centrally gated. Levels 1–5 retain their existing production, delivery, layout and costs. Stable role IDs append Mozo=4. Existing seven products, goals, duration, demand, stock reservations, speed limits and unlimited hiring remain unchanged.

Normal/Premium cooks independently travel to their respective grill, cook one identified unit for 2 / WorkRate seconds, carry that same unit along a safe route and deposit it in a reserved available specialty table slot. No automatic sandwich transfer in active mode. Each cooking worker exposes real progress. Mozo claims a cooked sandwich and routes to that reserved unit's actual product/slot on its table (Chori and Bondiola use their left product zones; Paty and Vacío use their right zones), then serves the actual pending customer; only handoff consumes reserved ingredients and records revenue. Beverage roles retain existing behavior. Cancellations preserve edible physical stock; no duplicate IDs, claims, ingredients or sales. Tables bound both prepared and in-flight production. Pending products take precedence, with fair stock replenishment.

Mozo is hired only during play, not automatically; starts at zero. Per-level configurable base costs follow existing successive-hire curve, with River reference $60. Every fresh attempt still starts with one Parrillero and speed x1.00. Save compatibility retained; no persistent earnings/unlocks or new pre-match roster screen.

## Presentation

Reuse existing furniture, counter, mural, fans, HUD, art, fonts, wood/iron cards and directional worker infrastructure. From Racing remove lower pennants, lower cream footer and both grills, extend existing tiled floor and approximately double actual grill-to-table circulation without resizing/distorting furniture. Geometry, projection, pickup/work anchors and obstacles share the same layout. Six cards: Speed/Normal/Premium then Coca/Fernet/Mozo. Per-worker circular progress only while cooking. Mozo uses an adapted existing worker outfit and real carried unit. Earlier levels remain unchanged.

## Validation / acceptance

- [x] Active per-worker cooking, one real carried/deposited unit and speed scaling.
- [x] Parrilleros del mismo rol usan puestos de parrilla separados al cocinar en paralelo; EditMode cubre separación y rutas seguras. Revisión visual de este ajuste sigue pendiente.
- [x] Revisión 18/10: anchors 1–9 comparten una sola fila horizontal; separación, claridad idle/cocción, agarre y slots de cuatro sándwiches cubiertos por 11 EditMode + 14 PlayMode focalizados.
- [ ] Captura actual del juego para cierre manual de la apariencia final; Unity MCP no expuso captura visual en esta sesión.
- [x] Exclusive Mozo sandwich handoff and unchanged beverage/mixed-order flow.
- [x] Mozo routes to the actual reserved table slot for Chori, Paty, Bondiola, and Vacío (focused EditMode suite passed 19/19 on2026-10-16; live visual route review remains pending).
- [x] Concurrent claims, finite stock/capacity, cancellation and ingredient conservation.
- [x] Role hiring/costs/save compatibility; earlier-level regressions.
- [x] Unified expanded geometry, original assets, six responsive cards and circular progress.
- [x] Unity compilation and focused tests.
- [x] Actual Unity captures: Level 8, partial cooking, transport, deposit, Mozo pickup/handoff; compact/tall/safe-area layouts.
- [x] Quantified throughput/balance assessment without changing goals/duration/economics.

Task is not complete until runtime validation and real visual evidence are recorded.

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
