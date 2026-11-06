# Huracán / Parque Patricios — Bonus Track

**Estado:** lógica y presentación fuente integradas; aceptación total pendiente. Este contrato describe lo que hoy hace la fuente, no certifica ejecución de escena, balance final ni Game View.

## Identidad y progresión

- ID persistente nuevo: `11`. La campaña visible se resuelve aparte con `CampaignOrder = [0,1,2,3,4,11,5,6,7,8,9,10]`; Huracán ocupa el sexto lugar sin cambiar IDs, precios ni tickets de los clubes preexistentes.
- Tras ganar Independiente (ID4) se habilita Huracán. Una estrella o más en Huracán habilita Racing (ID5); Huracán se puede repetir. Los perfiles v4 conservan el límite estable `unlockedLevel` (0–10) y migran a v5 con flags para conservar accesos previos a Racing y habilitar Huracán cuando el viejo progreso ya había llegado a Racing.
- `winsByLevel`, `lossesByLevel` y métricas de récord se amplían de 11 a 12 slots copiando el prefijo; los IDs anteriores no se desplazan.
- Huracán usa `street-mural-huracan-master-v1` y paleta bordó/blanco. El asset es 1898×829, igual al mural maestro de Ferro, e importa con el preset de textura existente. El escudo oficial usa la imagen aportada por el usuario (`street-huracan-crest-v1`) y se muestra solo en las superficies comunes asociadas al ID11; su fondo claro exterior se elimina al cargarlo, preservando el arte interior. No se usan las fotos de referencia directamente ni se reemplaza arte de otros clubes.

## Reglas implementadas

- Ronda de 120 s efectivos; cinco selecciones detenidas en 20/40/60/80/100 s. La tercera se presenta como Super Bonus. Mientras hay una carta (o la elección de rol de Refuerzo), `StreetSimulation.Step` no avanza.
- Equipo inicial de fuente: 2 Parrilleros, 1 Cocacolero y 1 Mozo; sin Premium/Fernetero. Productos `{0,1,4}` (Chori, Paty y Coca), sin objetivos ni monedas. El modo no enlaza stock de Mercado, por lo que cocina usa su comportamiento ilimitado; los demás niveles conservan su stock normal.
- Cartas normales: Parrilla a Fondo (+30% producción alimento, hasta 3 stacks); Otro Parrillero (límite de la geometría de cook stations); Coca Express (+35%, hasta 3); Otro Cocacolero (hasta 3 del rol); Mozo Relámpago (+40% movimiento, hasta 3); Otro Mozo (hasta 3). El Super Bonus filtra entre Todos a Fondo (+25% global/15 s), Entrega Dorada (10 unidades entregadas a 200 puntos), Manos Rápidas (−30% recogida por stack, hasta 2) y Refuerzo de Emergencia (elige un rol válido). Se excluyen cartas sin efecto aplicable.
- ¡Aguante! carga con diez entregas válidas; activación manual, +25% temporal a producción/movimiento durante 8 s y una carga como máximo.
- Multiplicadores se aplican en cada ciclo/ruta/recogida; no mutan velocidades persistentes. Límites: tres stacks de velocidades individuales, dos stacks de Manos Rápidas, máximo de cook stations para Parrilleros y máximo de tres Cocacoleros/Mozos.
- Una entrega física válida suma 100 puntos (200 con Entrega Dorada); cerrar un pedido suma 150 una sola vez. Sin ingresos ni monedas persistentes. A 120 s el modo termina; las estrellas se determinan por puntos. Los valores 3000/6500/10000 son **provisionales y no están aprobados**.
- Con ≥1 estrella: un ticket normal por intento; el primer 3★ añade un ticket una sola vez por perfil. Deduplicación por `attemptId`; abandono no acredita premios. Récord/perfil y ficha de intento se serializan junto con estadísticas.

## Integración técnica

- Dominio en `StreetBonusTrackState`; ciclo, trabajadores, rutas y handoffs siguen en `StreetSimulation`; lifecycle/progresión/persistencia en `StreetGame`; controles/HUD en `StreetView` y `StreetView.BonusTrack`; tema/mural en `ClubVisualTheme`.
- `StreetPlayerStatistics` mantiene arrays de niveles en longitud 12. `PlayerLocalRepository` acepta snapshots hasta v5 y Firestore admite nivel 11 y campos del resultado Bonus; `unlockedLevel` normal sigue limitado a 10.
- Pruebas EditMode añadidas usan reflexión desde el assembly de tests para preservar el límite actual entre assemblies.

## Evidencia y aceptación

| Criterio | Estado | Evidencia / límite |
|---|---|---|
| Escudo oficial suministrado aparece para Huracán ID11, con fondo exterior claro recortado | NOT_RUN | Recurso `street-huracan-crest-v1` añadido al manifiesto; `StreetArtTests` cubre carga, recorte y ajuste. Falta reimportar y ejecutar EditMode/Game View en Unity. |
| Orden estable y migración v4 | PASS estático | Fuente `StreetSimulation`/`StreetGame`; cobertura nueva `CampaignOrderInsertsHuracanWithoutShiftingStableLevelIds` y `LegacyV4RacingProgressAlsoUnlocksHuracanWithoutMovingTheFrontier`. Falta ejecutar Unity. |
| Temporización, pausa y roster | PASS estático | Integración en fuente y test `BonusStartsWithFreeTeamAndPausesAtFiveExactChoiceDeadlines`. Falta ejecución. |
| Puntuación por handoff, Aguante, tickets/idempotencia | PASS estático | Guards/efectos en fuente y casos EditMode relevantes. Falta ejecución. |
| Compilación de runtime/tests | PASS compilación estática | Unity Roslyn 6000.6.3f1 compiló assemblies en `/private/tmp`; no es import/compile del Editor ni ejecución de pruebas. Advertencias: rama inalcanzable preexistente, `DEVELOPMENT_BUILD` obsoleto y `AppDomain.GetAssemblies` en test existente. |
| Umbrales y 20 corridas por estrategia | NOT_RUN | No se ejecutó simulación real; estrellas siguen provisionales. No hay cifras observadas de entregas ni estrategias. |
| Escena, resoluciones y Game View | BLOCKED | Tras tres intentos acotados Unity MCP dejó de responder (`ping not answered`). No se reinició/duplicó el editor; no hay captura de juego ni aceptación visual. |
| APK Android Development — Main-only, IL2CPP/ARM64 | PASS (generación/paquete) | Job build-e2f77bf4d8 Succeeded; artefacto 0.2.81/code83 verificado en android-prototype.md. Esto no valida ejecución de la ronda. |
| Android gameplay / visual en dispositivo / Firebase real | NOT_RUN | No se instaló ni lanzó el APK; no hay prueba Firebase real ni capturas de gameplay del bonus. |

No marcar esta feature como aceptación completa mientras queden criterios obligatorios pendientes en `INFORME_BONUS_TRACK_HURACAN.md`.
