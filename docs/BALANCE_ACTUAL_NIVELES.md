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
| 3 | 9 | Camioneros / Plaza de Mayo | Tower Defense | Sin deadline | 0,4 | 120 hinchas finitos; completar y retirar todos para ganar; pierde si uno con pedido pendiente llega a salida | 500 | Ninguno | 60/60/—/—/— | fanSpeed 0.20475; sin control UI | N/A |
| 4 | 2 | Vélez / Liniers | Tradicional | 180 s | 0,1,4 | 0=180; 1=120; 4=150 (450) | 0 | 1 P | 25/25/—/—/— | ×2,25 | 150,00 |
| 5 | 12 | Argentinos Juniors / La Paternal | Tradicional, cocina activa | 315 s | 0,1,2,3,4,6,5 | 0=345; 1=245; 2=150; 3=140; 4=285; 5=120; 6=165 (1450) | 500 | 1 P + 1 C + 1 M | 70/70/90/80/80 | ×21,00 | 276,19 |
| 6 | 3 | Ferro / Caballito | Tradicional | 210 s | 0,1,2,4,6 | 0=185; 1=125; 2=60; 4=155; 6=75 (600) | 0 | 1 P | 30/30/35/—/— | ×3,25 | 171,43 |
| 7 | 11 | Huracán / Parque Patricios | Bonus Track | 120 s | 0,1,4 | Sin cuotas; puntuación especial | 0 | 2 P + 1 C + 1 M | Por cartas de bonus; no usa la curva tradicional | N/A | N/A |
| 8 | 10 | Los Redondos / Tandil | Tower Defense | Sin deadline | 0,4 | 60 hinchas finitos; completar y retirar todos para ganar; pierde si uno con pedido pendiente llega a salida | 500 | Ninguno | 60/60/—/—/— | fanSpeed 0.1575; sin control UI | N/A |
| 9 | 13 | La Renga / Autódromo Bs. As. | Tower Defense | Sin deadline | 0,4 | 60 hinchas finitos; completar y retirar todos; pierde si uno con pedido pendiente llega a salida | 500 | Ninguno | 60/60/—/—/— | fanSpeed 0.1575; separación 96 px | N/A |
| 10 | 14 | Peregrinación a Luján / Moreno | TD horizontal, estaciones preinstaladas (no comprables) | Sin deadline | 0,1,2,3,4,6,5 | 60 peregrinos; atender y retirar todos; fuga incompleta produce derrota | 1200 | 1 P + 1 M; una estación física de cada uno de los siete tipos | 55/55/70/65/65 (base ID8 Boca) | fanSpeed 0.035; separación96px; intervalo9s; speed UI +10%, primer costo70 | N/A |
| 11 | 4 | Independiente / Avellaneda | Tradicional | 240 s | 0,1,2,3,4,6,5 | 0=190; 1=130; 2=70; 3=65; 4=160; 5=65; 6=80 (760) | 20 | 1 P | 35/35/45/40/— | ×4,50 | 190,00 |
| 12 | 5 | Racing Club / Avellaneda | Tradicional, cocina activa | 260 s | 0,1,2,3,4,6,5 | 0=210; 1=145; 2=80; 3=75; 4=175; 5=75; 6=90 (850) | 250 | 1 P + 1 Pr + 1 M | 40/40/50/200/50 | ×6,00 | 196,15 |
| 13 | 6 | San Lorenzo / Boedo | Tradicional, cocina activa | 300 s | 0,1,2,3,4,6,5 | 0=230; 1=175; 2=110; 3=95; 4=205; 5=90; 6=115 (1020) | 250 | 2 P + 1 M | 45/45/55/50/55 | ×8,00 | 204,00 |
| 14 | 7 | River Plate / Núñez | Tradicional, cocina activa | 340 s | 0,1,2,3,4,6,5 | 0=250; 1=180; 2=110; 3=95; 4=210; 5=90; 6=115 (1050) | 800 | 1 P | 50/50/65/55/60 | ×10,50 | 185,29 |
| 15 | 8 | Boca Juniors / La Boca | Tradicional, cocina activa | 340 s | 0,1,2,3,4,6,5 | 0=275; 1=195; 2=120; 3=110; 4=225; 5=100; 6=125 (1150) | 800 | 1 P + 1 M | 55/55/70/65/65 | ×13,50 | 202,94 |

**Abreviaturas:** P = Parrillero; C = Cocacolero; Pr = Parrillero Premium; F = Fernetero; M = Mozo; `—` = rol no disponible en esa modalidad/catálogo, no un hire gratuito. En Tower Defense P/C se contratan al elegir área, con máximo de tres de cada tipo en cada una de las dos áreas compartidas. Huracán obtiene empleados por sus cartas.

- **Orden persistido distinto de la presentación:** `CampaignOrder = [0,1,9,2,12,3,11,10,13,14,4,5,6,7,8]`. Ferro habilita Huracán; una o más estrellas allí habilitan Los Redondos; Los Redondos habilita La Renga, que habilita Peregrinación; ésta habilita Independiente. SaveData v9 conserva desbloqueos explícitos por ID y migra los accesos anteriores; `unlockedLevel` queda como dato legacy.
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

- **Peregrinación ID14:** fila de balance **provisional, NO certificada ganable**; abre con1200 monedas,1 P +1 M y una estación física de cada uno de los siete tipos, sin compras/placement de equipamiento durante la partida. Curva de precios Boca ID8 (hire base55/55/70/65/65 en orden P/C/Pr/F/M; speed first70), sin sustituir `fanSpeed .035`; máximo3 porrol. Footer Boca-like 3×2 ya tuvo captura; las capturas anteriores de cards de equipo comprables son obsoletas para el nuevo panel `INSTALADO`. Tests y captura Unity actualizados, producción física y ganabilidad requieren validación. [Contrato](../specs/features/peregrinacion-lujan.md), [informe](reports/PEREGRINACION_IMPLEMENTACION_2026-11-10.md).

- **La Renga ID13:** implementación y compilación estática de cuatro assemblies; pruebas Unity y capturas reales pendientes por MCP bloqueado. No aceptación completa. [Contrato](../specs/features/la-renga.md) e [informe](reports/LA_RENGA_IMPLEMENTACION_2026-11-10.md).

- **Tower Defense:** la revisión anterior de diez hinchas registra 67 EditMode y 4 PlayMode aprobados. La revisión posterior de mesas/servicio registra 36 EditMode únicos `PASS`, 9 PlayMode únicos `PASS` y 1 `FAIL`: entrega A/B/C, recursos y Game View 540×960 aprobados, pero la política probada en ola natural terminó 35/36 unidades y un escape. No declarar aceptación/victoria natural completa. Detalle y evidencia en el contrato TD. Suite global, APK y dispositivo: `NOT_RUN`.
- **Argentinos Juniors:** balance y progresión ID12 cubiertos en integración inicial 33/33 EditMode; pruebas de camisetas v2/escudo, Firestore, captura Game View del nivel y Android aún `NOT_RUN`/`BLOCKED` según criterio. No equivale a aceptación visual o end-to-end.
- **Huracán:** modo Bonus Track implementado; la tabla conserva sus reglas de fuente, pero aceptación completa de partida/umbrales visuales/dispositivo continúa pendiente. Ver matriz del contrato.
- No hay evidencia de playthrough humano de una campaña fresca completa con la configuración actual.
