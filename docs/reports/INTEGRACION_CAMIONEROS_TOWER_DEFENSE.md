# Informe — Camioneros Tower Defense integrado a la campaña

**Fecha:** 2026-11-03 · **Checkout:** `/Users/celestino/HayChoriYPaty` · **Unity del proyecto:** `6000.6.3f1`. Se preservaron los demás cambios locales.

## Resultado de implementación

Camioneros ocupa la **posición visible 11**, con su **ID de campaña estable 9**, entre Boca (ID 8) y Los Redondos (ID 10). No se alteró `CampaignOrder` ni se añadió una fila/ID; su única entrada jugable es Tower Defense. Desde el selector, si el preflight de stock del nivel está listo, seleccionar Camioneros inicia el runtime TD; si falta stock, se muestra Mercado y su acción de jugar vuelve a iniciar directamente TD. No se agregó un selector de modalidad ni se conserva una ruta tradicional de Camioneros: el inicio convencional `TryStartRound` rechaza ID 9.

El runtime de ruta, hinchas, órdenes y estaciones sigue usando los componentes Tower Defense existentes. El dueño del intento vuelve a ser campaña: `TryStartTowerDefenseRound` recorre el mismo inicio que las partidas normales (reserva de trabajadores de tickets, GUID de intento, `StreetMarket.BeginAttempt` sobre la instancia exacta de simulación, snapshot/guardado y binding de inventario). La simulación tradicional ID 9 no avanza mientras TD la posee. Las métricas del modo se importan a esa simulación; los terminales pasan por `ProcessRoundResult` → `StreetMarket.SettleAttempt` → recompensa/estadísticas/guardado/outbox existentes. Una victoria conserva la idempotencia de tickets por intento y el unlock de ID 10; el reintento de Camioneros sigue accesible desde el selector. Si la acción Salir coincide en el mismo frame con la finalización, el entrypoint reevalúa el modo ya `Finished` y evita convertir ese resultado en abandono.

El modo toma duración, metas por producto y demanda del balance actual de Camioneros; empieza con su caja/plantel y suma los workers reservados por tickets. Contratos y mejoras usan la fórmula de coste del nivel 9, considerando el staff de tickets. Las entregas contabilizan cantidades, clientes atendidos, órdenes completas e ingreso a los precios oficiales de producto; esos datos, hires, gastos y velocidad se proyectan al resultado/estadísticas de campaña. No se alteraron cuotas, duración, precios, starters, curva de hires, esquema de guardado ni premios.

**Alcance de inventario:** el intento sí usa el snapshot, el gate de stock del Mercado y su settlement comunes. Se conserva la regla ya existente de `StreetKitchenProduction` para la cocina activa TD/campaña (sándwiches activos y bebidas configuradas no consumen ingredientes vía reservas del mercado); no se añadió un coste/penalización TD nuevo. Abandonar el run usa `ReturnToLevels`, registra las métricas como abandono sin premio y restaura el snapshot según la política común. Pausa/suspensión/cierre checkpointan los datos disponibles; el runtime TD no se reanuda como estado serializado al volver a abrir la app, sino que la recuperación de intento interrumpido ya existente descarta el runtime y aplica su checkpoint una sola vez.

## Archivos de runtime y regresiones añadidas

- `Assets/Scripts/Core/StreetSimulation.cs`: marca protegida de runtime externo ID 9, proyección de métricas y bloqueo de step tradicional durante TD.
- `Assets/Scripts/Core/StreetMarket.cs`: acceso de solo lectura a consumos reales del intento para los registros de abandono.
- `Assets/Scripts/Core/StreetGame.cs`: inicio dedicado integrado, checkpoints, importación, hires/mejoras, cierre, settlement común y guard para el inicio tradicional ID 9.
- `Assets/Scripts/TowerDefense/TowerDefenseGameMode.cs`: recibe caja/roster/tickets de campaña, usa costes comunes y expone métricas de entrega/economía.
- `Assets/Scripts/UI/StreetView.cs` y `StreetView.TowerDefense.cs`: navegación directa desde selector/Mercado, controles de TD, salida/resultado de campaña; se quitó la ruta visible de selector de modo.
- `Assets/Tests/EditMode/StreetTicketsTests.cs`: `CamionerosTowerDefenseUsesCampaignId9SettlementAndUnlocksLosRedondosExactlyOnce` y `ExitAtTowerDefenseCompletionSettlesVictoryInsteadOfAbandoning`.
- `Assets/Tests/EditMode/TowerDefenseGameModeTests.cs`: `CampaignRosterTicketWorkersAndTDSpendingStayInTheCampaignEconomy` y contadores de ingresos/entregas.
- Contrato e índice SDD actualizados en `specs/features/tower-defense.md` y `specs/README.md`; el detalle operativo y la matriz de evidencia están en este informe. Las auditorías históricas no se reescribieron.

## Validación criterio por criterio

`PASS` significa evidencia directa para ese criterio (aquí puede ser inspección estática, indicada expresamente); no sustituye ejecución Unity. `BLOCKED` describe un chequeo que el entorno impidió iniciar. No se marca una prueba no ejecutada como aprobada.

| Criterio solicitado | Estado | Evidencia/límite |
|---|---|---|
| 1. Completar Boca y desbloquear Camioneros | `PASS` (estático) | Orden mantiene Boca ID 8 → Camioneros ID 9; `NextCampaignLevelId` y el unlock existente usan el ID estable. No se jugó una campaña nueva en Unity. |
| 2. Entrar a Camioneros e iniciar Tower Defense | `PASS` (estático) | Selector ID 9 llama la ruta TD; el inicio convencional rechaza ID 9. Flujo de toque/hitbox real en Unity: `NOT_RUN`. |
| 3. Vallas y recorrido | `NOT_RUN` | Hay evidencia histórica de geometría/vallas del modo anterior, pero no se revalidó un run que entra desde la campaña. |
| 4. Hinchas avanzan por el recorrido | `NOT_RUN` | Sin run integrado/captura actual de Unity. |
| 5. Atención por trabajadores dentro del alcance | `NOT_RUN` | No se ejecutó una partida ni prueba integrada de estaciones/handoffs. |
| 6. Producción y entrega de los siete productos | `NOT_RUN` | El código conserva catálogo/especialidades y proyecta los siete contadores; no se completó cobertura runtime de cada producto/bebida. |
| 7. Economía de campaña funciona | `NOT_RUN` | Se revisó el cableado de caja, roster/tickets, precios, hires, upgrades, ventas y settlement; la regresión de economía agregada no se ejecutó. |
| 8. Ganar Camioneros desbloquea Los Redondos | `NOT_RUN` | Flujo de ID 9→10 estático y test de regresión añadido; no se ejecutó Test Runner ni partida. |
| 9. Repetir Camioneros luego de ganarlo | `PASS` (estático) | El selector mapea de nuevo la posición 11 a ID 9 y el unlock superior no lo bloquea; replay real: `NOT_RUN`. |
| 10. Reiniciar app y comprobar persistencia | `NOT_RUN` | No se reinició app ni se probó un save real. No se añadió schema; el código usa recuperación/checkpoint común, sin reanudación de runtime TD. |
| 11. Niveles tradicionales continúan funcionando | `NOT_RUN` | El guard afecta solo ID 9; no se corrió regresión de niveles tradicionales después del cambio. |
| 12. UI responsive en varias resoluciones | `NOT_RUN` | La geometría responsive previa no acredita Game View de la campaña integrada; sin captura actual de varias relaciones. |
| Compilación C# posterior a los cambios | `BLOCKED` | Unity MCP reportó una instancia en `mcpforunity://instances`, pero `project_info` falló con “plugin session … disconnected while awaiting command_result” y `editor_state` con “ping not answered”. No se abrió ni reinició Editor. |
| Test Runner de las regresiones nuevas | `BLOCKED` | No se pudo iniciar con Unity MCP desconectado. Los dos tests listados arriba son código fuente agregado, no resultados. Jobs anteriores de otro checkpoint que iniciaron 0 pruebas no validan estos cambios. |
| Auditoría independiente del cableado | `PASS` (estático, no runtime) | Revisor independiente recorrió scripts/tests y confirmó criterios de orden, dispatch, intento/mercado, settlement/idempotencia y economía; en una segunda revisión verificó que la salida tras completar Tower Defense conserve la victoria y no registre abandono. No halló defecto concreto ni afirmó ejecución de tests. |

## Pendientes para aceptación

Conectar al Editor activo de este checkout sin abrir otro proceso, compilar y revisar Console; correr las dos regresiones nuevas y pruebas focalizadas de ruta, follower, vallas, cocina y settlement; ejecutar el flujo selector→TD con entrada real; validar producción/entrega de los siete productos, ganar/desbloquear/repetir, salida/reinicio/guardado y regresión de niveles normales; obtener capturas **Game View completas** a varias relaciones de aspecto. Hasta entonces el estado es **integrado en fuente, no aceptado end-to-end**. No se hizo APK.
