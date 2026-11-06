# Balance actual de niveles

**Corte:** 2026-11-03 · **Checkout:** `/Users/celestino/HayChoriYPaty` · **Unity:** 6000.6.3f1.

Esta es la referencia operativa única de cuotas, tiempos, caja, plantel inicial y precios de contratación. La tabla efectiva contiene trece posiciones visibles: doce niveles de clubes y Huracán como bonus en la sexta. Camioneros es la posición 11 (ID interno estable 9), cuya única modalidad jugable es Tower Defense. Argentinos Juniors queda al final en posición 13 (ID12).

**Prioridad/fuentes:** `Assets/Scenes/Main.unity` serializa los parámetros globales, 13 catálogos de producto y 13 duraciones. Los balances detallados (`StreetBalance.levelBalances`, 13 filas incluyendo el balance vacío de Huracán) y sus defaults están definidos en `Assets/Scripts/Core/StreetSimulation.cs`; `levelBalances` no está serializado en Main, por lo que en ese caso rigen los defaults de código. El catálogo de Los Redondos también está serializado en Main. Los viejos `levelGoals` agregados son solo compatibilidad: no mandan sobre las cuotas por producto. Orden e IDs estables están en `StreetSimulation.CampaignOrder` y `LevelNames`. Huracán toma su duración y catálogo especial de `StreetBonusTrackState` y los defaults de simulación. Camioneros se juega exclusivamente con el runtime/recurso `Assets/Resources/TowerDefense/Camioneros-PlazaDeMayo.asset`, pero usa el ID 9 existente y la economía, tickets, guardado, estadísticas, liquidación y progresión de campaña. Argentinos Juniors (ID12) copia el balance operativo de Los Redondos (ID10).

> **Límite de verificación:** el preview de Main confirmó orden, metas, tiempo, caja, planteles, multiplicadores, 11 filas de catálogo serializadas y el ScriptableObject de Tower Defense. Tras corregir en fuente el cálculo del primer tier de Parrillero, Unity comenzó una solicitud de recompilación pero el puente MCP se desconectó. No se obtuvo ningún resultado de aserciones post-fix: el primer job EditMode falló en inicialización con `completed=0`; el segundo job no devolvió respuesta porque la sesión Unity MCP se desconectó mientras esperaba su resultado. Por eso los precios corregidos de Parrillero de esta tabla están derivados de la fuente actual y su fórmula, no se presentan como comprobados en un assembly recompilado. Véase [informe](reports/INFORME_CORRECCION_NIVELES.md).

## Tabla completa de campaña

Los IDs de producto son estables: **0** Chori, **1** Paty, **2** Bondiola, **3** Vacío, **4** Coca, **5** Fernet, **6** cerveza. “Catálogo” respeta el orden de servicio/presentación; “metas” usa ID y cantidad. El objetivo total es informativo: la victoria tradicional exige cumplir cada meta habilitada.

| Pos. | ID | Club / ubicación | Modo | Tiempo | Catálogo IDs | Metas por producto (ID=cantidad; total) | Caja inicial | Plantel gratis | Primer precio pagado P / C / Premium / Fern / Mozo ($) | Speed factor | Objetivo/min | Lectura de presión y cuello observable |
|---:|---:|---|---|---:|---|---|---:|---|---|---:|---:|---|
| 1 | 0 | All Boys / Floresta | Campaña | 120 s | 0 | 0=200 (200) | 0 | 1 P | 15 / — / — / — / — | ×1,00 | 100,00 | Referencia inicial, un producto y un Parrillero; es baseline, no certificación de victoria nueva. |
| 2 | 1 | Nueva Chicago / Mataderos | Campaña | 150 s | 0,4 | 0=200; 4=200 (400) | 10 | 1 P | 20 / 20 / — / — / — | ×1,50 | 160,00 | +60% de unidades/min frente a All Boys y primera especialidad Coca; hay que financiar su contratación/stock. |
| 3 | 2 | Vélez / Liniers | Campaña | 180 s | 0,1,4 | 0=180; 1=120; 4=150 (450) | 0 | 1 P | 25 / 25 / — / — / — | ×2,25 | 150,00 | Añade Paty; Chori/Paty son del Parrillero y Coca requiere Cocacolero. |
| 4 | 3 | Ferro / Caballito | Campaña | 210 s | 0,1,2,4,6 | 0=185; 1=125; 2=60; 4=155; 6=75 (600) | 0 | 1 P | 30 / 30 / 35 / — / — | ×3,25 | 171,43 | Bondiola exige Premium y Coca/cerveza Cocacolero; cinco metas y sin caja de apertura. |
| 5 | 4 | Independiente / Avellaneda | Campaña | 240 s | 0,1,2,3,4,6,5 | 0=190; 1=130; 2=70; 3=65; 4=160; 5=65; 6=80 (760) | 20 | 1 P | 35 / 35 / 45 / 40 / — | ×4,50 | 190,00 | Catálogo completo y siete metas; suma Vacío y Fernet, que requieren Premium y Fernetero. |
| 6 | 11 | Huracán / Parque Patricios | Bonus Track | 120 s | 0,1,4 | Metas de producto: no aplica; puntaje especial | 0 | 2 P + 1 Coca + 1 Mozo | No aplica: altas por cartas de bonus | No aplica | No aplica | Cinco elecciones temporizadas, entregas/pedidos suman puntos; umbrales de estrellas 3000/6500/10000 son provisionales. |
| 7 | 5 | Racing / Avellaneda | Campaña (cocina activa) | 260 s | 0,1,2,3,4,6,5 | 0=210; 1=145; 2=80; 3=75; 4=175; 5=75; 6=90 (850) | 250 | 1 P + 1 Premium + 1 Mozo | 40 / 40 / 50 / **200** / 50 | ×6,00 | 196,15 | Cocineros activos producen por unidad; Mozo entrega sándwiches. Fernetero $200 es deliberado y queda inmediatamente pagable: restan $50. |
| 8 | 6 | San Lorenzo / Boedo | Campaña (cocina activa) | 300 s | 0,1,2,3,4,6,5 | 0=230; 1=175; 2=110; 3=95; 4=205; 5=90; 6=115 (1020) | 250 | 2 P + 1 Mozo | 45 / 45 / 55 / 50 / 55 | ×8,00 | 204,00 | Dos Parrilleros y un Mozo gratuitos; quedan por contratar las especialidades faltantes. |
| 9 | 7 | River / Núñez | Campaña (cocina activa) | 340 s | 0,1,2,3,4,6,5 | 0=250; 1=180; 2=110; 3=95; 4=210; 5=90; 6=115 (1050) | 800 | 1 P | 50 / 50 / 65 / 55 / 60 | ×10,50 | 185,29 | Apertura U adoptada: un solo Parrillero y sin Mozo gratis; sándwiches en mesa esperan contratación, aunque hay dinero inicial. |
| 10 | 8 | Boca / La Boca | Campaña (cocina activa) | 340 s | 0,1,2,3,4,6,5 | 0=275; 1=195; 2=120; 3=110; 4=225; 5=100; 6=125 (1150) | 800 | 1 P + 1 Mozo | 55 / 55 / 70 / 65 / 65 | ×13,50 | 202,94 | Conserva la apertura $800 de U y agrega Mozo gratis solo para Boca; un único cocinero inicial. |
| 11 | 9 | Camioneros / Plaza de Mayo | **Tower Defense (única modalidad)** | 380 s | 0,1,2,3,4,6,5 | 0=310; 1=220; 2=135; 3=125; 4=255; 5=110; 6=145 (1300) | 500 | 1 P + 1 Coca + 1 Mozo | 60 / 60 / 80 / 70 / 70 | ×17,00 | 205,26 | Posición visible 11 con ID estable 9. Runtime de ruta/trabajadores propio; inicio, caja, staff/tickets, ventas, resultado y progreso son de campaña. |
| 12 | 10 | Los Redondos / Tandil | Campaña (cocina activa) | 315 s | 0,1,2,3,4,6,5 | 0=345; 1=245; 2=150; 3=140; 4=285; 5=120; 6=165 (1450) | 500 | 1 P + 1 Coca + 1 Mozo | 70 / 70 / 90 / 80 / 80 | ×21,00 | 276,19 | Sube 34,5% unidades/min sobre Camioneros mientras recorta 65 s; presión de diseño a revisar, no error demostrado. |
| 13 | 12 | Argentinos Juniors / La Paternal | Campaña (cocina activa) | 315 s | 0,1,2,3,4,6,5 | 0=345; 1=245; 2=150; 3=140; 4=285; 5=120; 6=165 (1450) | 500 | 1 P + 1 Coca + 1 Mozo | 70 / 70 / 90 / 80 / 80 | ×21,00 | 276,19 | Último nivel; copia las reglas y el balance de Los Redondos sin cambiar el ID10 ni el bonus Huracán. |

**Abreviaturas de plantel y contratación:** P = Parrillero; C = Cocacolero; Premium = Parrillero Premium; Fern = Fernetero; M = Mozo. Los IDs de producto son 0 Chori, 1 Paty, 2 Bondiola, 3 Vacío, 4 Coca, 5 Fernet y 6 cerveza. `—` significa rol no habilitado, no contratación de costo cero. La tabla da el **primer precio pagado** por especialidad. Las contrataciones siguientes son locales al rol y usan factores `1×, 2,5×, 5×, 9×, 14×, 20×, 27×, 35×, 44×...` sobre esa base, redondeados hacia arriba al siguiente múltiplo de $5. Parrillero gratuito, otros starters y trabajadores de tickets no consumen tiers pagados.

## Tower Defense: configuración de ruta integrada al nivel ID 9

| Recurso/runtime | Balance de campaña | Tiempo / caja efectiva | Productos y metas | Plantel efectivo | Primer precio pagado ($) | Integración |
|---|---|---|---|---|---|---|
| `tower-defense` / `tower-defense.camioneros.plaza-de-mayo.level-1` | Camioneros campaña ID 9 | 380 s / $500 iniciales | IDs 0,1,2,3,4,6,5; cuotas ID 9 (1300 unidades) | Plantel campaña + trabajadores reservados por tickets | 60 / 60 / 80 / 70 / 70 (tiers calculados con roster/tickets) | Configuración de recorrido/estaciones y runtime de hinchas propios; se suspende el step tradicional del ID 9. El mismo intento liquida Mercado, tickets, estadísticas, guardado y unlock del ID 10. No es una fila ni selector aparte. |

## Mejoras, producción y capacidad

- Todas las partidas normales comienzan a **×1,00** y ofrecen nueve mejoras. La base común de costos es `$5/$10/$15/$20/$30/$45/$65/$90/$125`; se multiplica por el factor de cada nivel y se redondea hacia arriba al siguiente múltiplo de $5. Los factores son los de la tabla anterior. Para no dejar los precios derivados implícitos, las secuencias efectivas por nivel son:

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
| ×17,00 | 85 / 170 / 255 / 340 / 510 / 765 / 1105 / 1530 / 2125 |
| ×21,00 | 105 / 210 / 315 / 420 / 630 / 945 / 1365 / 1890 / 2625 |

- Cada mejora común da +10% a movimiento/trabajo (hasta ×1,90). La cocina Parrillero/Premium usa +6% por mejora (×1,54 tras nueve). Desde Racing la cocina activa es de 2 s por unidad, nominalmente 30 unidades/min por cocinero a ×1 (46,2 a ×1,54); bebidas usan 0,28 s de preparación (214,3/min teóricos por estación). No son tasas de entrega: desplazamiento, asignación/colas, pedidos, acceso a estaciones, mesa llena y handoff reducen el throughput real.
- Capacidades físicas base de cocina: mesa normal 48, Premium 36, Fernet 45 y barril Coca/cerveza 12. En Racing y posteriores IDs 0–3 salen de la mesa solo mediante Mozo. River carece de Mozo inicial; los sándwiches preparados no se entregan hasta contratar uno. La capacidad numérica por minuto de handoff real no se deduce de estas cotas estáticas y no está certificada para una campaña fresca completa.
- Los objetivos/minutos de la tabla son `sum(cuotas)/duración × 60`, no tasas observadas de ventas. Los valores no prueban factibilidad de ganar; no se corrió una simulación de campaña nueva completa en este trabajo.

## Hallazgos de balance conservados

- **Racing Fernetero $200 — verificado sin cambio.** El balance oficial, la especificación de economía, el historial y `RacingFerneteroStartsAtTwoHundredAndRetainsTheRoleLocalHireCurve` coinciden: Racing abre con $250, el primer Fernetero cuesta $200 y deja $50; siguientes valores son $500/$1000/$1800/$2800. No se rebajó a $45.
- **River — configuración U intencional.** River mantiene $800, un Parrillero y sin Mozo; solo Boca recibe Mozo gratis. Se conserva el tradeoff y se documenta la barrera de entrega. El registro existente de River fue 2/10, no evidencia de aceptación robusta; no hay prueba separada de Boca que certifique el balance.
- **Chicago y Los Redondos — revisión de diseño, no corrección automática.** Chicago sube de 100 a 160 unidades requeridas/min y añade Coca; Redondos supera a Camioneros en 34,5% y tiene menos tiempo. Se mantienen metas/tiempos/caja porque no son errores de configuración demostrados.
- Las auditorías de octubre contienen cifras de otras versiones. Úselas solo como historial; la referencia de configuración vigente es este documento. No se afirma que la configuración actual sea humanamente ganable en los 12 niveles.
