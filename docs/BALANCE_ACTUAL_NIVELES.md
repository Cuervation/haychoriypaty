# Balance vigente de campaña — fuente maestra

**Corte técnico:** 2026-11-10 · **Checkout:** `/Users/celestino/HayChoriYPaty` · **Unity:** `6000.6.3f1`.

Esta es la **única tabla actual de niveles**. El selector contiene **15 posiciones visibles: 14 niveles de clubes/recitales/peregrinación y Huracán como Bonus Track**. No confundir posición visible con ID estable. Hay 10 modos tradicionales; Camioneros ID9, Los Redondos ID10, La Renga ID13 y Peregrinación ID14 son Tower Defense y Huracán ID11 es el bonus. Los contratos de [Street automation](../specs/features/street-automation.md), [Tower Defense](../specs/features/tower-defense.md), [Huracán](../specs/features/huracan-bonus-track.md), [Argentinos Juniors](../specs/features/argentinos-juniors.md) y [Mercado](../specs/features/market-fridge.md) describen mecánicas y evidencia; no mantienen copias alternativas de esta tabla.

## Fuentes y prioridad

1. Runtime C# efectivo: `StreetSimulation.CampaignOrder`, `LevelNames`/`LevelDisplayNames`, `StreetBalance.CreateOfficialLevelBalances()`, `StreetBonusTrackState`, `TowerDefenseLevelConfig`/`TowerDefenseGameMode` y progresión de `StreetGame`.
2. `Assets/Scenes/Main.unity` serializa 15 catálogos, 15 duraciones y campos globales; **no serializa `levelBalances`**. Las metas, caja, precios de contratación y multiplicadores efectivos para niveles tradicionales provienen de los defaults de código.
3. `Main` conserva `levelGoals` agregados legacy y un catálogo/duración heredados de Camioneros ID9. En campaña ID9/ID10/ID13/ID14 esas columnas no controlan los pedidos ni victoria: el runtime TD solo permite Chori/Coca y no usa cuotas ni deadline. No borrar ni “corregir” esos campos de compatibilidad basándose en esta tabla.
4. Los siete precios oficiales de venta se seleccionan por ID de producto y no por el precio guardado del jugador. El producto Fernet conserva una discrepancia de etiqueta/volumen entre runtime y referencia visual; consultar [product.md](../specs/product.md), no deducir ni cambiar receta/arte desde esta tabla.

## Tabla maestra

Los IDs de producto son estables: **0** Chori, **1** Paty, **2** Bondiola, **3** Vacío, **4** Coca, **5** Fernet, **6** cerveza. Metas en formato `ID=cantidad`; el total es informativo y la victoria tradicional exige cada meta habilitada.

| Pos. | ID | Club / ubicación | Modalidad | Tiempo de nivel | Catálogo efectivo | Metas / condición terminal | Caja | Plantel inicial gratis | Primer hire pagado P/C/Pr/F/M ($) | Factor speed* | Meta/min** |
|---:|---:|---|---|---:|---|---|---:|---|---|---:|---:|
| 1 | 0 | All Boys / Floresta | Tradicional | 120 s | 0 | 0=200 (200) | 0 | 1 P | 15/—/—/—/— | ×1,00 | 100,00 |
| 2 | 1 | Nueva Chicago / Mataderos | Tradicional | 150 s | 0,4 | 0=200; 4=200 (400) | 10 | 1 P | 20/20/—/—/— | ×1,50 | 160,00 |
| 3 | 9 | Camioneros / Plaza de Mayo | TD en valla en C como La Renga (2026-11-23, a pedido; antes valla horizontal), cocina y footer de Nueva Chicago a su tamaño exacto (escala 1), estaciones preinstaladas | Sin deadline | 0,4 | 120 hinchas finitos, una unidad por producto; completar y retirar todos para ganar; pierde si uno con pedido pendiente llega a salida | 20 (2026-11-24, a pedido; antes 50 y, antes de eso, 500) | Ninguno | 20/20/—/—/— como Nueva Chicago (nivel normal anterior) y con su mismo crecimiento por compra (20, 50, 100; 2026-11-23, a pedido; antes 60/60/—/—/— fijos) | fanSpeed 0.096875 (2026-11-26, a pedido: +25 % a la velocidad inicial y, con ella, a todos los tramos; antes 0.0775 y, antes del 2026-11-18, 0.20475); rampa de dificultad creciente (2026-11-24, a pedido: la tabla "E" de La Renga sobre los 120 hinchas; antes ×1,00 a ×1,66): doce tramos de 10 hinchas con velocidad ×1,16 a ×2,04 (+0,08 por tramo; antes ×1,00 a ×1,88, cambio del 2026-11-24 a pedido), cantidad 2/3 unidades por producto 0/0, 30/0, 40/20, 30/40, 20/60, 10/80, 10/90 y 0/100 desde el hincha 70, y 2º producto 20→100 % (tabla en el contrato); **no se probó con esta rampa** (la anterior ganaba 120/120 con 331 entregas con 6 contrataciones y mejoras de velocidad); tarjeta Velocidad del footer de Chicago; sin los demás cambios de La Renga del 2026-11-23 (200 hinchas, 0 monedas, precios de Ferro, guardias); en la C con 6 contrataciones una partida automática gana 120/120 con 140 entregas (una sola partida, sin barrido); balance NO certificado | N/A |
| 4 | 2 | Vélez / Liniers | Tradicional | 180 s | 0,1,4 | 0=180; 1=120; 4=150 (450) | 0 | 1 P | 25/25/—/—/— | ×2,25 | 150,00 |
| 5 | 12 | Argentinos Juniors / La Paternal | Tradicional, cocina activa | 315 s | 0,1,2,3,4,6,5 | 0=118; 1=105; 2=85; 3=80; 4=115; 5=76; 6=90 (669) | 500 | 1 P + 1 C + 1 M | 70/70/90/80/80 | ×21,00 | 127,43 |
| 6 | 3 | Ferro / Caballito | Tradicional | 210 s | 0,1,2,4,6 | 0=185; 1=125; 2=60; 4=155; 6=75 (600) | 0 | 1 P | 30/30/35/—/— | ×3,25 | 171,43 |
| 7 | 13 | La Renga / Autódromo Bs. As. | Tower Defense (valla en C, cocina pasiva y footer de Ferro, 2026-11-19) | Sin deadline | 0,1,2,4,6 | 240 hinchas finitos (2026-11-24, a pedido; el 2026-11-23 fueron 200 y antes 120); completar y retirar todos; pierde si uno con pedido pendiente llega a salida | 40 (2026-11-24, a pedido; antes 0, el 2026-11-23 fue 110 y antes 500) | Ninguno | 30/30/35/—/— como Ferro y con su mismo crecimiento por compra (30, 75, 150, 270, 420, 600…; el Parrillero inicial es gratis y el Cocacolero ya no arranca (2026-11-24, a pedido); antes 60/60/—/—/— fijos); mejora de velocidad también como Ferro (20, 35, 50, 65…; antes 85) | fanSpeed 0.1875 (2026-11-21, a pedido: mitad de 0.375); rampa de dificultad por hincha nacido (2026-11-24, a pedido; `difficultyTiers` del config): doce tramos de 20 hinchas con velocidad ×1,00/1,08/1,16/1,24/1,32/1,40/1,48/1,56/1,64/1,72/1,80/1,88, cantidad 2 y 3 unidades por producto (0+0/30+0/40+20/30+40/20+60/10+80/10+90/0+100/0+100/0+100/0+100/0+100 %) y 2º producto (20/45/55/60/65/70/75/80/85/90/95/100 %); sin tope de trabajadores por rol (`maximumWorkersPerRole: 0`; solo limitan las monedas); inicio con un Parrillero y 40 monedas, sin Cocacolero (2026-11-24, a pedido; antes un Parrillero, un Cocacolero y 110 monedas); la ola automática de prueba (una sola estrategia, hasta 24 contrataciones y mejoras de velocidad) **pierde** con esta rampa: 211/240 con 897 entregas en 540×960 y 120/240 con 366 en 610×1356 (NO certificado: no hay estrategia ganadora medida) | N/A |
| 8 | 11 | Huracán / Parque Patricios | Bonus Track | 120 s | 0,1,4 | Sin cuotas; puntuación especial | 0 | 2 P + 1 C + 1 M | Por cartas de bonus; no usa la curva tradicional | N/A | N/A |
| 9 | 4 | Independiente / Avellaneda | Tradicional | 240 s | 0,1,2,3,4,6,5 | 0=190; 1=130; 2=70; 3=65; 4=160; 5=65; 6=80 (760) | 20 | 1 P | 35/35/45/40/— | ×4,50 | 190,00 |
| 10 | 5 | Racing Club / Avellaneda | Tradicional, cocina activa | 260 s | 0,1,2,3,4,6,5 | 0=210; 1=145; 2=80; 3=75; 4=175; 5=75; 6=90 (850) | 250 | 1 P + 1 Pr + 1 M | 40/40/50/200/50 | ×6,00 | 196,15 |
| 11 | 10 | Los Redondos / Tandil | TD en la C invertida, con la cocina activa de Racing (7 estaciones, Parrilleros cocinan y Mozos entregan; 2026-11-25, a pedido), estaciones preinstaladas | Sin deadline | 0,1,2,3,4,5,6 (los 7 de Racing; antes 0,1,2,4,6) | 240 hinchas finitos (2026-11-24, a pedido; antes 60), con pedidos de 1 a 3 unidades y segundo producto según la rampa; completar y retirar todos para ganar; pierde si uno con pedido pendiente llega a salida | 40 (2026-11-24); equipo inicial 1 P + 1 Pr + 1 M (como Racing) | Ninguno (sin tope de trabajadores por rol) | como Racing (nivel 5): P 40, C 40, Pr 50, F 200, M 50 y su crecimiento por compra; mejora de velocidad como Racing | 240 hinchas, rampa "E" de 12 tramos, **fanSpeed 0.1535625** (2026-11-25, a pedido, +50 % sobre 0.102375, que era la de Luján; antes 0.1875): 46,1 px/s lógicos al inicio y 86,6 px/s en el último tramo (×1,88), recorrido de 1588 px (34 s → 18 s), C invertida y guardias; **NO equilibrado**: la partida automática (24 trabajadores) pierde con 41/240 servidos; balance NO medido | N/A |
| 12 | 6 | San Lorenzo / Boedo | Tradicional, cocina activa | 300 s | 0,1,2,3,4,6,5 | 0=230; 1=175; 2=110; 3=95; 4=205; 5=90; 6=115 (1020) | 250 | 2 P + 1 M | 45/45/55/50/55 | ×8,00 | 204,00 |
| 13 | 7 | River Plate / Núñez | Tradicional, cocina activa | 340 s | 0,1,2,3,4,6,5 | 0=250; 1=180; 2=110; 3=95; 4=210; 5=90; 6=115 (1050) | 800 | 1 P | 50/50/65/55/60 | ×10,50 | 185,29 |
| 14 | 8 | Boca Juniors / La Boca | Tradicional, cocina activa | 315 s (2026-11-26, a pedido: +5 s; antes 310 s, y 340 s hasta el 2026-11-23) | 0,1,2,3,4,6,5 | 0=275; 1=195; 2=120; 3=110; 4=225; 5=100; 6=125 (1150) | 800 | 1 P + 1 M | 55/55/70/65/65 | ×13,50 | 219,05 |
| 15 | 14 | Peregrinación a Luján / Moreno | TD horizontal, estaciones preinstaladas (no comprables) | Sin deadline | 0,1,2,3,4,6,5 | 60 peregrinos; atender y retirar todos; fuga incompleta produce derrota | 1200 | 1 P + 1 M; una estación física de cada uno de los siete tipos | 55/55/70/65/65 (base ID8 Boca) | fanSpeed 0.102375 (0.0525 × 1,3 × 1,5, a pedido; ~30,7 px lógicos/s con base300); entrada continua: separación 58 px, intervalo 0,1 s; speed UI +10%, primer costo70 | N/A |

**Abreviaturas:** P = Parrillero; C = Cocacolero; Pr = Parrillero Premium; F = Fernetero; M = Mozo; `—` = rol no disponible en esa modalidad/catálogo, no un hire gratuito. En Tower Defense serpenteante (Los Redondos, La Renga) P/C se contratan al elegir área, con máximo de tres de cada tipo en cada una de las dos áreas compartidas; en Camioneros y Peregrinación (valla horizontal) la contratación es de un toque en el puesto del rol, con máximo de tres por rol. Huracán obtiene empleados por sus cartas.

- **Orden persistido distinto de la presentación:** `CampaignOrder = [0,1,9,2,12,3,13,11,4,5,10,6,7,8,14]`. Ferro habilita La Renga, que habilita Huracán; una o más estrellas allí habilitan Independiente; Racing habilita Los Redondos; Boca habilita Peregrinación (último). SaveData v9 conserva desbloqueos explícitos por ID y migra los accesos anteriores; `unlockedLevel` queda como dato legacy.
- **Efectos especiales:** Huracán dura 120 s efectivos, pausa en cinco decisiones en 20/40/60/80/100 s (la tercera es Super Bonus), puntúa entregas/cierres y termina por reloj; los umbrales 3000/6500/10000 son provisionales. Tower Defense no tiene reloj/cuotas/tickets de contratación; IDs9/10/13 comienzan sin trabajadores y conservan sus reglas de producto/ola, mientras ID14 empieza con1 P +1 M, usa los siete productos y atiende60 peregrinos. ID9 atiende120 hinchas; ID10/13 atienden60 al desaparecer cada hincha. Ver contrato de cada modo para derrota, tickets y evidencia.
- **ID9/ID10 legacy:** `StreetBalance` y Main conservan datos tradicionales de compatibilidad por ID; para ID9 figuran380 s y factor ×17; para ID10 los valores tradicionales históricos incluyen315 s y factor ×21. Ninguno de esos objetivos/reloj/productos/starter define el runtime TD. Los tres TD serpenteantes abren con$500 y contratan P/C desde$60. No exponen control de mejora de velocidad. ID14 conserva su velocidad de ruta `.035`, pero expone +10% speed con precios del source balance Boca ID8: factor13.50, primer costo70; la progresión común de hires/speed afecta precios de compra, no la ola. La compra/settlement y persistencia continúan usando el intento de campaña común.

## Economía compartida (modos tradicionales salvo excepción indicada)

### Venta, contratación y speed

| ID | Producto | Precio oficial de venta |
|---:|---|---:|
| 0 | Chori | $5 |
| 1 | Paty | $5 |
| 2 | Bondiola | $10 |
| 3 | Vacío | $12 |
| 4 | Coca | $5 |
| 5 | Fernet | $12 |
| 6 | Cerveza en lata | $7 |

Cada rol habilitado tiene su propia curva de contratación. El primer trabajador gratuito no consume tier pagado; las contrataciones por tickets, donde estén habilitadas, tampoco avanzan esa curva. Pagos posteriores multiplican la base de la fila: tiers 1, 2 y 3 usan ×1, ×2,5 y ×5; desde el cuarto hire el factor es `n(n+1)/2 − 1`, redondeado hacia arriba al múltiplo de $5. Contratar es ilimitado para los modos tradicionales mientras se pueda pagar. `maxStaff`/caps serializados son legacy; Tower Defense tiene su propio máximo por tipo y área.

Los modos tradicionales empiezan en speed ×1,00 y tienen nueve mejoras de +10 puntos porcentuales hasta ×1,90. Costos base `$5/$10/$15/$20/$30/$45/$65/$90/$125`, multiplicados por el factor de la fila y redondeados hacia arriba a $5:

| Factor | Costos de las 9 mejoras ($) |
|---:|---|
| ×1,00 | 5 / 10 / 15 / 20 / 30 / 45 / 65 / 90 / 125 |
| ×1,50 | 10 / 15 / 25 / 30 / 45 / 70 / 100 / 135 / 190 |
| ×2,25 | 15 / 25 / 35 / 45 / 70 / 105 / 150 / 205 / 285 |
| ×3,25 | 20 / 35 / 50 / 65 / 100 / 150 / 215 / 295 / 410 |
| ×4,50 | 25 / 45 / 70 / 90 / 135 / 205 / 295 / 405 / 565 |
| ×6,00 | 30 / 60 / 90 / 120 / 180 / 270 / 390 / 540 / 750 |
| ×8,00 | 40 / 80 / 120 / 160 / 240 / 360 / 520 / 720 / 1000 |
| ×10,50 | 55 / 105 / 160 / 210 / 315 / 475 / 685 / 945 / 1315 |
| ×13,50 | 70 / 135 / 205 / 270 / 405 / 610 / 880 / 1215 / 1690 |
| ×21,00 | 105 / 210 / 315 / 420 / 630 / 945 / 1365 / 1890 / 2625 |

La cocina activa de Parrillero/Premium aplica CookWorkRate +6% por nivel de speed; no equivale a que todas las especialidades tengan la misma tasa de entrega. Detalles de producción física y excepciones de stock están en [active-cooks-mozos](../specs/features/active-cooks-mozos.md) y [market-fridge](../specs/features/market-fridge.md).

### Mercado, liquidación y alcance de la evidencia

El saldo/stock de Mercado persiste; compras requieren efectivo, sin crédito. Inicio de partida, balance de apertura, hires/speed y progreso de partida son por intento; resultados y liquidación se enlazan al mismo intento persistente. La política exacta de stock, reservas, pagos de victoria, tickets e idempotencia pertenece a [market-fridge.md](../specs/features/market-fridge.md), no duplicada aquí. Huracán usa scoring/tickets propios; TD comparte el settlement de campaña pero su cocina de Chori/Coca no activa el gate de cuotas/ingredientes tradicional.

**La tabla describe configuración efectiva, no demuestra ganabilidad.** Objetivo/min = suma de cuotas tradicionales ÷ duración ×60; no es throughput observado ni garantía de victoria. River/Boca usan apertura U adoptada sin prueba humana actualizada; Chicago/Redondos mantienen metas altas no certificadas como imposibles o ganables.

## Estado de validación por modo (al corte)

- **Peregrinación ID14:** fila de balance **provisional, NO certificada ganable**; abre con1200 monedas,1 P +1 M y una estación física de cada uno de los siete tipos, sin compras/placement de equipamiento durante la partida. Curva de precios Boca ID8 (hire base55/55/70/65/65 en orden P/C/Pr/F/M; speed first70), `fanSpeed .0525` (50% sobre .035); máximo3 porrol. Footer Boca-like 3×2 ya tuvo captura; las capturas anteriores de cards de equipo comprables son obsoletas para el nuevo panel `INSTALADO`. Ganabilidad con la nueva velocidad requiere validación. [Contrato](../specs/features/peregrinacion-lujan.md), [informe](reports/PEREGRINACION_IMPLEMENTACION_2026-11-10.md).

- **La Renga ID13:** implementación y compilación estática de cuatro assemblies; pruebas Unity y capturas reales pendientes por MCP bloqueado. No aceptación completa. [Contrato](../specs/features/la-renga.md) e [informe](reports/LA_RENGA_IMPLEMENTACION_2026-11-10.md).

- **Tower Defense:** la revisión anterior de diez hinchas registra 67 EditMode y 4 PlayMode aprobados. La revisión posterior de mesas/servicio registra 36 EditMode únicos `PASS`, 9 PlayMode únicos `PASS` y 1 `FAIL`: entrega A/B/C, recursos y Game View 540×960 aprobados, pero la política probada en ola natural terminó 35/36 unidades y un escape. No declarar aceptación/victoria natural completa. Detalle y evidencia en el contrato TD. Suite global, APK y dispositivo: `NOT_RUN`.
- **Argentinos Juniors:** balance y progresión ID12 cubiertos en integración inicial 33/33 EditMode; pruebas de camisetas v2/escudo, Firestore, captura Game View del nivel y Android aún `NOT_RUN`/`BLOCKED` según criterio. No equivale a aceptación visual o end-to-end.
- **Huracán:** modo Bonus Track implementado; la tabla conserva sus reglas de fuente, pero aceptación completa de partida/umbrales visuales/dispositivo continúa pendiente. Ver matriz del contrato.
- No hay evidencia de playthrough humano de una campaña fresca completa con la configuración actual.
