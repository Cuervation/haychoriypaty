# Informe — corrección de inconsistencias de niveles

**Fecha:** 2026-11-03 · **Checkout activo:** `/Users/celestino/HayChoriYPaty` · **Sin commit/push.** Se preservó el trabajo local ajeno.

> **Actualización posterior — 2026-11-03:** las frases más abajo que describen Tower Defense como modo separado corresponden al corte de esta corrección de balance y quedaron supersedidas por [la integración de Camioneros en la campaña](INTEGRACION_CAMIONEROS_TOWER_DEFENSE.md). Camioneros es ahora la posición visible 11 (ID 9) y su única modalidad jugable es TD; el recurso/configuración de ruta sigue separado, pero el intento usa progresión, Mercado, tickets, guardado y resultado de campaña. Este cambio no altera los valores de balance referidos en este informe.

## A. Inconsistencias encontradas

1. `StreetSimulation.CalculateHireCost` restaba de la cuenta de contrataciones pagadas los Parrilleros gratuitos de San Lorenzo y Huracán, pero no el starter gratuito normal de los otros niveles. Así el primer Parrillero pagado de cada nivel saltaba al tier ×2,5: ejemplos observados en el assembly que tenía Unity cargado antes de la recompilación: All Boys $40 en vez de $15; Chicago $50 en vez de $20; Racing $100 en vez de $40; River $125 en vez de $50. El balance oficial, reglas de workers y pruebas ya existentes definen el primero como gratuito y el primer contratado como tier 1.
2. `Main.unity` serializaba 10 cadenas en `levelProductIds` para 11 niveles estándar. Los Redondos funcionaba por fallback al catálogo default, pero escena y default no coincidían y el último valor no estaba explícito.
3. Algunos datos anteriores (River/Boca $250, Camioneros 360 s) pertenecen a balances anteriores. Se conservan, pero pueden confundirse con los vigentes.
4. Se comprobó el posible salto de Fernetero en Racing: **$200 es deliberado**, no un error. Código, valor oficial, test específico, historial y documentación coinciden; con $250 inicial quedan $50. No se cambió a $45.

## B. Correcciones realizadas

| Archivo | Antes | Después | Estado/justificación |
|---|---|---|---|
| `Assets/Scripts/Core/StreetSimulation.cs` | Parrillero gratuito base 0 salvo San Lorenzo/Huracán (base 2) | Base gratis 1 en niveles normales y 2 en San Lorenzo/Huracán | CORREGIDO en fuente. El primer hire pagado vuelve al costo por-level definido; no cambia curva/tickets/plantel. Aplica al helper compartido de Tower Defense. |
| `Assets/Tests/EditMode/StreetSpecialtyDomainTests.cs` | Sin regresión de primer tier de Parrillero en todos los niveles | Nuevo test comprueba starter y tiers 1/2 en 11 niveles | AGREGADO; no ejecutado por el runner actual. |
| `Assets/Scenes/Main.unity` | 10 IDs de catálogo serializados | 11 IDs, incluyendo Redondos `0,1,2,3,4,6,5` | CORREGIDO sin alterar el gameplay efectivo previo (antes operaba por fallback). |
| `docs/BALANCE_ACTUAL_NIVELES.md`, contrato y status | Valores actuales/históricos mezclados | Referencia central, límites de evidencia y etiquetas históricas | ACTUALIZADO; sin modificar metas, monedas, tiempos ni staff de diseño. |

No se modificó la economía de productos, orden de campaña, mecánicas de Huracán, reglas de tickets ni configuración de Tower Defense. La migración de saves y su esquema no cambiaron.

## C. Valores actuales

Tabla resumida de las doce posiciones (incluido el bonus) y el nivel TD. Para secuencias exactas de los nueve precios de mejora, throughput teórico y jerarquía de fuentes, ver [Balance actual de niveles](../BALANCE_ACTUAL_NIVELES.md), referencia central vigente.

| Pos. | ID | Nivel / ubicación | Modo | s | Productos ID | Metas por ID (total) | $ | Gratis | Primer hire pagado P/C/Prem/F/M ($) | Speed × | unidades/min | Observación |
|---:|---:|---|---|---:|---|---|---:|---|---|---:|---:|---|
| 1 | 0 | All Boys / Floresta | Campaña | 120 | 0 | 0=200 (200) | 0 | 1 P | 15/—/—/—/— | 1,00 | 100,00 | Un producto, baseline. |
| 2 | 1 | Nueva Chicago / Mataderos | Campaña | 150 | 0,4 | 0=200;4=200 (400) | 10 | 1 P | 20/20/—/—/— | 1,50 | 160,00 | +60% objetivo/min y añade Coca. |
| 3 | 2 | Vélez / Liniers | Campaña | 180 | 0,1,4 | 0=180;1=120;4=150 (450) | 0 | 1 P | 25/25/—/—/— | 2,25 | 150,00 | Paty y Coca con roles propios. |
| 4 | 3 | Ferro / Caballito | Campaña | 210 | 0,1,2,4,6 | 0=185;1=125;2=60;4=155;6=75 (600) | 0 | 1 P | 30/30/35/—/— | 3,25 | 171,43 | Premium + Coca/cerveza. |
| 5 | 4 | Independiente / Avellaneda | Campaña | 240 | 0,1,2,3,4,6,5 | 0=190;1=130;2=70;3=65;4=160;5=65;6=80 (760) | 20 | 1 P | 35/35/45/40/— | 4,50 | 190,00 | Siete metas, incluye Fernet. |
| 6 | 11 | Huracán / Parque Patricios | Bonus | 120 | 0,1,4 | No aplica; puntaje | 0 | 2 P + 1 C + 1 M | No aplica (cartas) | N/A | N/A | Cinco elecciones; 3000/6500/10000 puntos provisionales. |
| 7 | 5 | Racing / Avellaneda | Campaña activa | 260 | 0,1,2,3,4,6,5 | 0=210;1=145;2=80;3=75;4=175;5=75;6=90 (850) | 250 | 1 P + 1 Prem + 1 M | 40/40/50/**200**/50 | 6,00 | 196,15 | Fernetero $200 deliberado. |
| 8 | 6 | San Lorenzo / Boedo | Campaña activa | 300 | 0,1,2,3,4,6,5 | 0=230;1=175;2=110;3=95;4=205;5=90;6=115 (1020) | 250 | 2 P + 1 M | 45/45/55/50/55 | 8,00 | 204,00 | Dos cocineros normales, un Mozo. |
| 9 | 7 | River / Núñez | Campaña activa | 340 | 0,1,2,3,4,6,5 | 0=250;1=180;2=110;3=95;4=210;5=90;6=115 (1050) | 800 | 1 P | 50/50/65/55/60 | 10,50 | 185,29 | Sin Mozo inicial; unidades esperan hire/entrega. |
| 10 | 8 | Boca / La Boca | Campaña activa | 340 | 0,1,2,3,4,6,5 | 0=275;1=195;2=120;3=110;4=225;5=100;6=125 (1150) | 800 | 1 P + 1 M | 55/55/70/65/65 | 13,50 | 202,94 | Mozo gratuito solo en Boca. |
| 11 | 9 | Camioneros / Plaza de Mayo | Tower Defense (única modalidad) | 380 | 0,1,2,3,4,6,5 | 0=310;1=220;2=135;3=125;4=255;5=110;6=145 (1300) | 500 | 1 P + 1 C + 1 M | 60/60/80/70/70 | 17,00 | Slot 11/ID9; runtime TD, campaña común. |
| 12 | 10 | Los Redondos / Tandil | Campaña activa | 315 | 0,1,2,3,4,6,5 | 0=345;1=245;2=150;3=140;4=285;5=120;6=165 (1450) | 500 | 1 P + 1 C + 1 M | 70/70/90/80/80 | 21,00 | 276,19 | +34,5%/min vs Camioneros, 65 s menos. |
| TD | runtime propio | Camioneros / Plaza de Mayo | `tower-defense` integrado a ID 9 | 380 | 0,1,2,3,4,6,5 | Igual a ID 9 (1300) | 500 | Roster de campaña + tickets reservados | 60/60/80/70/70 | 17,00 | Ruta/config propios; liquidación y progreso comunes. |

## D. Problemas de balance no modificados

- **Chicago:** presión requerida pasa 100→160 unidades/min (+60%) y agrega Coca. Es una observación aritmética, no demuestra error ni imposibilidad.
- **River:** cae de dos Parrilleros + Mozo de San Lorenzo a uno solo, sin Mozo. Es la configuración U adoptada por el usuario; el costo es un cuello real de entrega hasta contratarlo. Se conserva.
- **Los Redondos:** 276,19 unidades/min frente a 205,26 de Camioneros, 34,5% más con 65 segundos menos. Propuesta de revisión de diseño; no se tocan cuotas/tiempo/dinero.
- **Racing:** no hay corrección de Fernetero; $200 es valor deliberado y permite contratarlo desde los $250 iniciales.
- **Factibilidad global:** no se hizo partida completa fresca ni campaña humana. Pruebas históricas son de balances anteriores; no se infiere que todos los niveles sean ganables ni que sean imposibles.

## E. Pruebas y validación

| Criterio | Estado | Evidencia/límite |
|---|---|---|
| Orden 12 niveles, IDs y Huracán entre Independiente/Racing | PASS estático + preview | `CampaignOrder`: `0,1,2,3,4,11,5,6,7,8,9,10`; escena preview mostró orden activo. |
| Main catalog/duración por escena | PASS preview | Preview de Unity leyó 11 filas de catálogo tras cambio y duraciones `120,150,180,210,240,260,300,340,340,380,315`. |
| Metas/caja/staff, producto activo y specialty | PASS estático + preview | Preview anterior a recompilar el parche leyó 12 balances, catálogo/meta/roster; cuotas/tiempos/caja concuerdan con fuente. |
| Primer hire de Parrillero en once niveles y tiers de economía | NOT_RUN en Unity | Se agregó `FreeParrilleroDoesNotConsumePaidHireTierAcrossAllCampaignLevels`. Primer job `535103eb89804d4aa72c3bea239fe856`: falló al inicializar (`completed=0`, “tests did not start within timeout”). Segundo job `9ac8aa4eae8148d6b6530c5e92593741`: no devolvió resultado; Unity MCP se desconectó mientras se esperaba `command_result`. Costos corregidos revisados en fuente, no en assembly post-fix. |
| Auditoría estática independiente de tablas/configuración | PASS estático | Revisión de solo lectura confirmó orden, metas, tiempos, caja, roster, costos, multiplicadores, TD y Racing Fernetero $200 contra las fuentes actuales; no sustituye compilación ni tests Unity. |
| Ausencia de contrataciones accidentales a $0 y fórmula de tiers | PASS estático; test Unity NOT_RUN | `CanHireRole` exige `hireCost > 0`; la auditoría comparó costos base y curva role-local con el código. La regresión recién agregada no pudo ejecutarse en esta sesión. |
| Racing Fernetero $200 y curva | PASS consistencia estática; rerun NOT_RUN | `RacingFerneteroStartsAtTwoHundredAndRetainsTheRoleLocalHireCurve` documenta aserciones 200/500/1000/1800/2800 y saldo remanente $50. Coincide con balance/fuente y auditoría estática independiente; no se ejecutó de nuevo en esta sesión. |
| River/Boca, Camioneros/Redondos, desbloqueos y funcionamiento de Huracán | NOT_RUN en esta sesión | Se seleccionaron casos de regresión relacionados, pero no se obtuvo resultado de Unity. No se alteró la progresión ni el código del Bonus Track. |
| Compatibilidad de partidas guardadas anteriores | NOT_RUN | No se modificaron serialización, versión de save ni migraciones; los tests existentes de compatibilidad no se ejecutaron, por lo que no se certifica runtime. |
| Factibilidad de niveles completos y throughput real de entrega | NOT_RUN | No se hizo una partida/simulación fresca completa. Los valores objetivo/min, tiempos de cocina y capacidades son límites/cálculos estáticos, no throughput de handoff observado ni prueba de victoria. |
| Compilación y Console post-fix | BLOCKED | Se solicitó recompilación con Unity MCP y se recibió estado `compiling`; después el puente se desconectó, sin poder comprobar `isCompiling`, Console ni estado del assembly actualizado. |
| Recurso Tower Defense vinculado a Camioneros ID 9 | PASS estático + preview histórico | El asset de ruta mantiene mode/level ID internos, `sourceCampaignLevelId: 9` y siete productos. La integración de campaña posterior se detalla en el informe enlazado arriba; las nuevas pruebas de settlement/economía no se ejecutaron. |
| Game View / dispositivo | NOT_APPLICABLE a estos cambios de economía/config; NOT_RUN | No hubo retoque visual ni build. La captura no valida gameplay/factibilidad. |
| `git diff --check` + espacios al final en documentos nuevos | PASS | `git diff --check` sin errores en las rutas trackeadas afectadas; escaneo de los dos documentos nuevos sin espacios en blanco al final de línea. |

**Abreviaturas de tabla:** P = Parrillero; C = Cocacolero; Prem = Parrillero Premium; F = Fernetero; M = Mozo. IDs de producto: 0 Chori, 1 Paty, 2 Bondiola, 3 Vacío, 4 Coca, 5 Fernet, 6 cerveza.

## F. Estado final

- **Corregido en fuente:** cálculo de primer tier del Parrillero y catálogo Main de Redondos.
- **Verificado sin cambios en esta corrección de balance:** Fernetero Racing $200; River/Boca; balances de metas/tiempos/caja; Huracán sigue especial y entre Independiente/Racing. La configuración/recurso de ruta TD sigue identificado aparte, pero su entrada jugable y progresión usan ahora el nivel de campaña Camioneros ID 9.
- **Pendiente de decisión de diseño:** presión de Chicago; tradeoff de plantel/entrega River; presión de Los Redondos.
- **No validado:** compilación post-fix, ejecución EditMode y verificación Unity post-recompilación, saves por test, playthrough fresco de 12 niveles, entrega/throughput medido y aceptación humana. El primer job reportó `completed=0`; el segundo no devolvió resultado por desconexión de Unity MCP. No se presentan como aprobados.
