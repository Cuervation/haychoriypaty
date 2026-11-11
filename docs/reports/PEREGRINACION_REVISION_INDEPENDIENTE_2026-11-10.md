# Peregrinación — revisión independiente (2026-11-10)

**Estado: implementación INCOMPLETA; revisión estática confirma una limitación funcional, además de aceptación Unity pendiente.** El revisor de arquitectura redactó el contrato y este informe, pero no implementó runtime, persistencia, UI, arte ni pruebas. Principal e implementador corrigieron los defectos informados. Sin llamadas Unity MCP por este revisor ni reinicios/cierres de Editor.

Contrato: [Peregrinación a Luján — Moreno](../../specs/features/peregrinacion-lujan.md). Alcance: lectura focalizada de campaña, guardado/migración, configuración real, geometría/valla, construcción/BFS, cocina/servicio/reflow, presentación/UI y pruebas nuevas. No se atribuye todo el diff local a esta feature: existía trabajo previo ajeno.

## Hallazgos y correcciones verificadas por lectura

- Campaña15: ID14/posición10 y orden `0,1,9,2,12,3,11,10,13,14,4,5,6,7,8`; sucesores13→14→4. SaveData9 migra v8 copiando14bits y agrega14 por acceso posterior4–8 o victoria/pending13. No infiere victoria desde frontera legacy0–10. Snapshot validator acepta v7/8/9 con13/14/15bits; reglas locales admiten match14. Límites antiguos validator8/rules13 detectados durante edición y corregidos. Sin validación Firebase real ni despliegue.
- Ruta horizontal: dos extremos con avance constanteX, entrada/salida fuera de ambos bordes para aparecer/desaparecer progresivamente; responsive evita división por `RowCount-1`. FenceGenerator opt-in dibuja/collider sólo frontera inferior. Serpenteantes conservan rama anterior.
- Siete tipos de muebles seleccionables y cinco roles. Preview/commit usan el mismo snap; AABB respeta pivote/cuerpo del rol; BFS valida extremos exactos, conexiones locales y barrido del cuerpo, no el antiguo nodo global más cercano. Nueva colocación recalcula accesos de muebles existentes. Compras inválidas no cobran.
- Una Kitchen activa compartida. IDs5Fernet/6cerveza intactos. Mozo ahora reserva mesa antes de `TryTake`; faltaba esa reserva y la revisión detectó que impedía retirar sandwiches. Cooks alternan0/1 y2/3; la corrección reemplaza depósito inmediato por traslado con unidad `Carried` hasta mesa, y retorna a parrilla antes de cocinar.
- Servicio usa fases pickup/carry/servicio; consumo/venta tras llegar, una unidad física por recibo. La revisión de segundo ciclo detectó que reflow preservaba fase pero borraba rutas, dejando etapas1/2 inmóviles. Se verificó corrección: trabajadores inmóviles procesan cualquier fase activa, pickup comprueba ubicación, y entrega comprueba altura/alcance y reconstruye trayecto cuando corresponde. No se certifica ejecución por esta lectura.
- Reflow preserva offsets relativos a zona/escala y foodIDs; rechaza/pausa si muebles/cuerpos no caben, con `LastReflowSucceeded` atendido en UI. Evita el reflow anterior que comprimía centros conservando tamaños y generaba solapes. Su comportamiento requiere prueba de resize durante transporte real.
- Food renderer ahora distribuye unidades según capacidad/filas/columnas y separa productos compañeros; eliminado el patrón anterior de cuatro posiciones repetidas. Unidades llevadas siguen al worker; recepción y lanzamiento reutilizan vía común.
- Arte fuente propio inspeccionado: mural2D de avenida/puestos/postes, atlas6×4 con12identidades peregrinas y2poses diferentes por par. Diagnóstico PNG fuente1536×1024: walk50.67%alpha0, front51.26%alpha0; no demuestra importación/pivotes en Unity. Principal corrigió luego mural a titular `PEREGRINACIÓN LUJÁN` y perspectiva/caminantes/flecha derecha según usuario; el revisor no lo considera Game View ni aceptación Unity.

## Matriz obligatoria final

| Criterio | Estado | Evidencia / límite |
|---|---|---|
| PL1 | `BLOCKED` | Orden/nombres/config15 inspeccionados y compilación Roslyn informada por principal; sin importación/compilación Editor ni selector/settlement Unity ejecutados |
| PL2 | `NOT_RUN` | Migración/snapshots/core inspeccionados; casos escritos, sin save/reopen/outbox ejecutados en Unity |
| PL3 | `NOT_RUN` | Rama horizontal y única frontera inferior verificadas en código; sin geometría/collider/follower ejecutados |
| PL4 | `FAIL` | Colocación múltiple cobra y dibuja equipos adicionales, pero sólo el primer equipo de cada tipo se utiliza; construcción operativa múltiple requerida no está completa. Resto de interacción/congestión `NOT_RUN` |
| PL5 | `FAIL` | Asignación de empleados/cocina/pickup siempre al primer equipo del tipo; las instalaciones adicionales no reciben asignación operativa. Movimiento multiworker/colas en Unity `NOT_RUN` |
| PL6 | `NOT_RUN` | Reservas Mozo y consumo/handoff unitario corregidos; sin trazabilidad de sieteproductos/órdenes mixtas ejecutada |
| PL7 | `NOT_RUN` | Prueba PlayMode de ola60 escrita; ninguna partida legal completa ejecutada. Perfil1200/.035/96/9s NO certificado ganable |
| PL8 | `NOT_RUN` | Core/stock/settlement/reflow inspeccionados; recuperación de etapas corregida en segundo ciclo, no prueba dinámica de pausa/resize/abandono/reingreso |
| PL9 | `BLOCKED` | Cero Game Views reales nuevas540×960,1080×1920,720×1280; arte fuente no las reemplaza |
| PL10 | `NOT_RUN` | Ramas/config previas preservadas por lectura; ninguna regresión Unity ejecutada |

## Defecto funcional confirmado pendiente: instalaciones adicionales sin asignación

`TowerDefenseGameMode.Peregrinacion.cs:43–48` calcula80monedas para cada equipo adicional y `TryPlaceStation` cobra/crea su ID. Sin embargo, `FindPlacedStation` (línea204), `RoleStation` (441–457) y búsqueda de pickup (250) retornan siempre el primer objeto del tipo. Cocina (167) y retiros (265) usan esa selección; `TowerDefenseWorldPresentation.Peregrinacion.cs:82` también proyecta las unidades únicamente sobre el primer mueble del tipo.

**Consecuencia demostrada por fuente:** una segunda parrilla/mesa/barril es comprable y ocupa espacio, pero no recibe asignación propia de producción/servicio/unidades. No cumple el pedido de múltiples instalaciones operativas, aunque compilen todos los assemblies. PL4/PL5 son `FAIL` por esta contradicción funcional; no es una prueba Unity fallida ni debe disfrazarse como simple falta de evidencia. Resolver requiere selección/asignación por ID de estación y capacidad/anchors disponibles, manteniendo una sola cocina y unidades únicas. Tras dos ciclos se detienen cambios runtime; se entrega explícitamente incompleto. Congestión en acceso compartido cook/Mozo sigue sin resolver/probar.

## Riesgos que deben comprobarse, no defectos ya demostrados

1. Varios trabajadores usan primer acceso de estación y cuerpos como obstáculos dinámicos. Esperan si el siguiente tramo se bloquea; no se observó un algoritmo general de replanificación/turnos que garantice ausencia de deadlock. Ejecutar mezcla completa con múltiples empleados y mobiliario denso.
2. Ganabilidad y dificultad siguen sin evidencia: ruta corta, apertura de construcción manual, tiempos de pickup/carry, velocidad y evolución de caja requieren partidas naturales legales. No sustituirlas por targets retenidos, monedas añadidas o reloj modificado.
3. Reflow durante cook/carry/servicio, salida del target durante transporte, liberación de reservas y acciones repetidas deben ejercitarse con tests; la lectura no demuestra que todas las intercalaciones preserven unidades/estadísticas.
4. Importadores/atlas/capas/proporciones/tamaño táctil y leggibilidad de pedidos largos requieren Editor y capturas reales de las tres resoluciones. Sin esa evidencia no afirmar cero solapes.

## Ejecución y siguiente paso

Pruebas escritas: `PeregrinacionCampaignTests` (orden/migración/shapes/identidad); PlayMode en `TowerDefensePlacementViewTests`: `PeregrinacionNaturalSixtyWithSevenProductsUsesLegalConstructionAndCanWin`, `PeregrinacionGameViewSingleFenceFreeConstructionAtThreeResolutions`, `PeregrinacionPointerPreviewPlacesFurnitureAndRejectsOverlapWithoutCharge`, `PeregrinacionWithoutWorkersLosesAndPauseStopsSimulation`. **Nombres y código no significan tests aprobados.** Compilación estática y logs son evidencia separada, documentada por principal en [informe de implementación](PEREGRINACION_IMPLEMENTACION_2026-11-10.md).

Principal informa Unity/MCP desconectado y luego transporte `127.0.0.1:8080` inaccesible. Este revisor no repitió reconexiones ni abrió otro Editor. Recuperar entorno sólo con autorización necesaria; ejecutar checks focalizados, partidas legales, inspeccionar Game Views y actualizar cada fila con resultados directos. Se agotaron dos ciclos de corrección estática; no se mantuvo un bucle indefinido de cambios sin evidencia.

Sin aceptación completa; no recomendar publicar. No commit, push, APK o despliegue Firebase como parte de esta revisión.
