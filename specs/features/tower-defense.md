# Tower Defense — Camioneros / Plaza de Mayo

**Estado:** integrado en el código fuente como la única modalidad jugable de Camioneros (posición visible 11, ID de campaña estable 9); aceptación end-to-end pendiente. Sigue en implementación y no se declara completo ni aceptado. El runtime de ruta/hinchas es específico de Tower Defense, pero inicio, economía de campaña, mercado, tickets, guardado, estadísticas, resultados y progresión pertenecen al mismo intento del nivel ID 9.

## Alcance y límites

- Camioneros / Plaza de Mayo es la posición visible 11 de la campaña, con ID estable 9, después de Boca (ID 8) y antes de Los Redondos (ID 10); sus tres tramos y dos zonas intermedias de trabajo se juegan exclusivamente como Tower Defense.
- La ruta tiene entrada arriba a la izquierda, alterna izquierda→derecha / derecha→izquierda y acaba abajo a la derecha. Los giros conectan cada tramo sin teletransportes.
- No crear un segundo slot, selector de modalidad ni ruta jugable de Camioneros tradicional; la entrada de campaña del ID 9 inicia Tower Defense directamente.
- Reutilizar productos, workers/especialidades, producción, precios oficiales, mercado, tickets, guardado, estadísticas y settlement existentes. La simulación tradicional del ID 9 no avanza durante el runtime TD y no puede iniciar una partida convencional.
- Tomar del balance de campaña ID 9 duración, metas por producto, moneda inicial, roster, precios oficiales, tiers de contratación y demanda; importar ventas y hires a ese mismo intento. No inventar economía, metas ni premios.
- Sin temporizadores individuales de paciencia ni abandono por impaciencia. Salir registra el intento como abandonado por el flujo común, sin premio/desbloqueo; el mercado restaura el snapshot conforme a su política existente. Cerrar/reiniciar la app no reanuda el runtime volante: el intento activo usa la recuperación/checkpoint de mercado existente y no duplica resultados.
- Implementar primero el nivel de tres tramos. Cinco/siete tramos son validaciones del generador, no niveles desbloqueados.
- No añadir policías, oleadas, compra de torres ni otras reglas que no están en la solicitud.

## Fuentes visuales y activos

- Conservar el lenguaje de dibujo 2D, perspectiva frontal levemente elevada, escala, capas y sprites actuales; las vallas son el único arte nuevo necesario para la geometría.
- El usuario aprobó explícitamente usar como base visual el fondo activo de Camioneros `Assets/Art/Street/Resources/street-background-camioneros-v1.png` (panorámica ilustrada de Plaza de Mayo) y la ruta S de tres tramos que describe la solicitud. El checkout no contiene un mural de pared separado de Camioneros; llamar al recurso por su naturaleza real y no sustituirlo por el mural de otro club. La solicitud adjunta en esta sesión fue solo texto; no incluyó sus dos imágenes de referencia.
- La configuración activa de Camioneros usa actualmente atlas de hinchas de Ferro; no llamarlos hinchas ilustrados específicamente para Camioneros ni redibujarlos silenciosamente.
- Nuevas piezas de valla deben ser sprites separados, reutilizables, con escala/pivotes/conectores consistentes. El generador debe crear piezas GameObject individuales y collider/límites coherentes; no hornear la ruta en una imagen ni en una valla gigante.

## Arquitectura vigente e integración de campaña

Mantener desacoplados config, geometría, vallas, seguidores, estaciones/alcances y coordinador. El objeto de `StreetSimulation` ID 9 sigue siendo la autoridad transaccional de campaña y el mismo objeto enlazado por `StreetMarket.BeginAttempt`; `StreetGame.TryStartTowerDefenseRound` inicia el intento común y marca la simulación como runtime externo. `StreetSimulation.Step` no avanza mientras lo posee TD. Al guardar/checkpoint o terminar, TD proyecta tiempo, ventas, ingresos, hires, speed y trabajadores a esa simulación y `StreetGame.ProcessRoundResult` liquida el intento común; salida manual registra abandono por `ReturnToLevels`. No añadir otro esquema de guardado, ledger de premios ni camino de tickets. `StreetKitchenProduction` conserva la configuración de producción existente del modo, sin cambiar las restricciones transaccionales históricas de TD.

### Mercado e ingredientes

El intento usa el stock preflight, snapshot, recibo/settlement y guardado de `StreetMarket`. Sin embargo, la política existente de cocina activa para este catálogo permite sándwiches de ID 0–3 sin reserva de ingredientes; Coca/cerveza y Fernet también siguen las reglas existentes de productos de bebida. Por eso TD no reporta un débito de unidades de ingredientes por entrega bajo las reglas actuales. No inventar un consumo nuevo al integrar la campaña ni describir el gate/snapshot de Mercado como consumo; si se cambia esa política en el futuro, requiere una decisión de balance y pruebas propias.

La configuración debe permitir cantidad y longitudes de tramos, ancho de corredor, separación/altura, límites del área jugable, entrada/salida, espacios de estación, prefabs/sprites de valla y puntos de servicio. Waypoints y alcances deben ser inspeccionables. Las configuraciones fuera de pantalla deben advertir/fallar validación: nunca encoger personajes o deformar arte automáticamente.

## Criterios obligatorios de aceptación

Estados permitidos para evidencia: `PASS`, `FAIL`, `BLOCKED`, `NOT_RUN`, `NOT_APPLICABLE` (con motivo). La evidencia nueva de esta integración se distingue de los checkpoints standalone anteriores; una prueba/captura histórica no valida por sí sola el acceso desde el selector ni el settlement de campaña.

### A. Dominio y campaña

- `PASS` (inspección estática) — `CampaignOrder` mantiene ID 9 en posición 11 e ID 10 en posición 12; la selección de ID 9 llama `StartTowerDefenseRun`, el inicio tradicional lo rechaza y la UI no ofrece un modo Camioneros alternativo. No se ejecutó una interacción humana en esta revisión.
- `PASS` (inspección estática) — `TryStartTowerDefenseRound` reserva y persiste el intento/tickets con la misma instancia de simulación que `StreetMarket.BeginAttempt`; la proyección terminal invoca `ProcessRoundResult` común. La regresión de progresión fue añadida pero su Test Runner no se ejecutó.
- `PASS` (inspección estática) — El runtime toma metas, duración y economía fuente de ID 9; usa roster/tickets de campaña y precios oficiales, y proyecta métricas al match común sin premio adicional.
- `PASS` (inspección estática) — `ExitTowerDefenseRound` reenvía al cierre normal una partida que ya está en fase `Finished` con goals/timeout terminal, evitando que una salida en el mismo frame la convierta en abandono. La regresión correspondiente está añadida y no se ejecutó.
- `NOT_RUN` — Boca→Camioneros, ganar y desbloquear Los Redondos, repetir ID 9 y regresar a niveles tradicionales en Unity; las regresiones fueron agregadas pero no ejecutadas.
- `NOT_RUN` — Compatibilidad runtime con guardados anteriores y recuperación tras reiniciar la aplicación; no se cambió el schema/IDs y el código usa la recuperación existente, pero no se probó con save real.

### B. Ruta y vallas

- `NOT_RUN` — Ejecutar de nuevo en el run de campaña el recorrido de tres tramos, las vallas/codos, el área visible y los followers. Hay evidencia standalone previa de geometría/layout, pero no equivale a una comprobación desde la campaña en la revisión actual.

### C. Hinchas, trabajo y pedidos

- `NOT_RUN` — Hinchas recorren la ruta dentro del run de ID 9 y entregan tras completar pedidos; esa interacción actual no se reejercitó en Unity.
- `NOT_RUN` — Trabajadores de cada especialidad producen, recogen y atienden dentro del alcance sin atravesar vallas; entregar y completar los siete productos, incluido Mozo/sándwiches y bebidas, sigue sin validar en Unity integrada.
- `NOT_RUN` — Política de mercado/stock del runtime TD y acumulación de ventas/coins están proyectadas a campaña; una cobertura integrada de cada producto y de toda la transacción aún no corrió.

### D. Presentación, accesibilidad y validación

- `NOT_RUN` — Selector→TD, HUD responsive y resultado completo en Unity tras conectar ID 9; existe una captura real 1080×1920 de la versión standalone previa, pero no representa esta integración de campaña.
- `NOT_RUN` — Validación visual de HUD/controles a distintas relaciones de aspecto, vallas/curvas y acciones con hitboxes reales.
- `NOT_RUN` — Regresión holística en Unity: rutas 3/5/7, varios hinchas, producción/entrega y niveles tradicionales.

## Configuración vigente y pendientes

- Camioneros de campaña es el ID estable `9`, posición visible 11; duración 380 s y balance/catálogo provienen de `StreetBalance`. El identificador interno de recurso TD no reemplaza ni altera el ID de campaña.
- La configuración inicial Tower Defense debe limitarse al recorrido de tres tramos, doble valla y dos zonas de la solicitud.
- No añadir una persistencia paralela ni objetivos/recompensas nuevos. La campaña usa sus metas de producto y su resultado normal; un intento incompleto al salir se registra como abandono según la política común.
- Las imágenes Image 1/Image 2 mencionadas en el texto pegado no llegaron en esta sesión. El usuario autorizó usar el fondo panorámico existente de Plaza de Mayo, de modo que la ausencia de un mural de pared separado no bloquea el primer nivel. No sustituir por Ferro/All Boys.

## Validación

- Auditoría estática de checkout/activos y versión Unity `6000.6.3f1`: realizada antes del runtime; hallazgos en `docs/design/tower-defense/README.md`.
- 2026-11-03: añadidos config independiente (`tower-defense` / `tower-defense.camioneros.plaza-de-mayo.level-1`), geometría, seguidor de hinchas, generador de valla y pruebas EditMode focalizadas; importados el sprite modular base, señal y tres prefabs de piezas. `TowerDefenseAssetBuilder` ejecutado en la instancia Unity 6000.6.3f1; Console confirmó creación de tres prefabs, señal y config. Esto solo valida el pipeline de assets, no el modo jugable ni el ensamblaje de la ruta en una escena.
- `validate_script` de geometría, seguidor, generador, builder y pruebas: sin errores; config también sin errores tras cambiar su inicialización inline. La comprobación de whitespace focalizada no halló espacios finales en los archivos nuevos; el `git diff --check` global sí expuso whitespace preexistente en metas de arte de cocina fuera del alcance, que se preservó.
- Los jobs históricos `5f55dfcadd294e8a9992a5a24e715467`, `63eb53210e4b4952b0af1d1380713dd0` y otros checkpoints inicializaron cero pruebas; esos fallos de arranque no son resultados de assertions. En la revisión de integración de campaña se añadieron pruebas de progresión/settlement y economía, pero el editor MCP no respondió y no se ejecutaron.
- No declarar completo el modo mientras haya un criterio obligatorio en `FAIL`, `BLOCKED` o `NOT_RUN`.

### 2026-11-03 — Integración de Camioneros en la campaña (aceptación pendiente)

- ID 9 queda en posición visible 11 (`CampaignOrder` termina `...,8,9,10`); el selector abre TD directamente y `TryStartRound` bloquea la ruta tradicional de ID 9. `TryStartTowerDefenseRound` usa el intento común de campaña/mercado/reserva de tickets/guardado; los resultados pasan por `ProcessRoundResult`, y salir durante una partida no terminal llama al flujo común de abandono. Una partida que ya terminó se liquida como resultado común.
- El runtime recibe los trabajadores y tickets reservados de la campaña, caja inicial del ID 9 y precios oficiales; registra ingresos, ventas por producto, clientes atendidos, órdenes completadas, upgrades y hires en el objeto de simulación de campaña. No se cambió el schema ni la asignación de IDs.
- Añadidas `CamionerosTowerDefenseUsesCampaignId9SettlementAndUnlocksLosRedondosExactlyOnce`, `ExitAtTowerDefenseCompletionSettlesVictoryInsteadOfAbandoning` y `CampaignRosterTicketWorkersAndTDSpendingStayInTheCampaignEconomy`. Son fuentes de regresión; **no se ejecutaron**. La auditoría independiente encontró los enlaces de código y no encontró un defecto concreto, pero no constituye prueba runtime.
- **Validación actual:** el recurso `mcpforunity://instances` devolvió una instancia `HayChoriYPaty`/Unity `6000.6.3f1`, pero `project_info` y `editor_state` fallaron por sesión de plugin desconectada / `ping not answered`. No se lanzó ni reinició Editor. Test Runner, compile/Console posterior, juego integrado, saves y captura Game View de esta versión son `BLOCKED` o `NOT_RUN`, según el criterio abajo.
- **Criterios aún `NOT_RUN`:** Boca→Camioneros en ejecución; recorrido/fences con hinchas en ruta desde selector; alcance y producción real de especialidades; cobertura entregable de siete productos; economía integrada ejecutada; ganar/desbloquear ID 10 y replay; recuperación tras reinicio; niveles tradicionales; y visuales Game View responsive. No declarar aceptado.

### Checkpoint histórico — Tower Defense standalone (2026-11-03; no valida la integración de campaña)

- En ese checkpoint existía un selector de modo separado y la lógica de campaña tradicional de Camioneros se mantenía aislada; este acceso fue reemplazado por selección directa del nivel ID 9.
- Captura real Unity Game View, 1080×1920: `Assets/Screenshots/tower-defense-game-view-2026-11-03-final.png`. Durante el run un muestreo registró 27 hinchas activos y 27 entregas; la captura posterior muestra contadores/saldo avanzados por ventas oficiales. Se usó el fondo panorámico de Plaza de Mayo autorizado. La captura deja ver una escena densa y los módulos de valla se perciben separados en algunas curvas; la continuidad visual requiere revisión, aunque el test estructural confirma dos `EdgeCollider2D` continuos.
- Muestreo de pedidos corregido para replicar el algoritmo oficial de demanda por metas/tiempo/pedidos pendientes; cada cantidad sigue asociada a su producto. `validate_script` del coordinador y fixture EditMode: 0 errores/advertencias. La última lectura de Console, tras reconectar Unity MCP, devolvió 0 errores; sí mostró advertencia de compresión PVRTC obsoleta para el PNG de captura y logs de desconexión/reconexión WebSocket. En una lectura anterior también aparecieron warnings en `StreetView.cs`/`StreetSceneBuildGuardTests.cs` y limpieza de job obsoleto, sin causalidad determinada.
- **Invocación manual de cuerpos de pruebas en Editor (no NUnit Test Runner):** 5/5 `TowerDefenseGameModeTests` (incluida nueva regresión de demanda), 3/3 geometrías de ruta para filas 3/5/7, prueba de generador modular 1/1, y dos verificaciones de seguidor 2/2. Es evidencia acotada; no reemplaza al Test Runner.
- **Test Runner EditMode:** job `63eb53210e4b4952b0af1d1380713dd0` falló al inicializar con `completed=0` (se configuró un timeout de inicio de 30 s); ningún test comenzó. El job previo `0c1fafe76f4644d3ab02f92dbe2607ec` pasó 8/8 antes de las modificaciones recientes y no valida esta revisión.
- **Layout responsive:** 9/9 comprobaciones de geometría/render target a 720×1280, 1080×1920 y 720×1600 para rutas de 3/5/7 filas. Solo una Game View de la implementación se capturó en 1080×1920; HUD completo a otras relaciones sigue `NOT_RUN`.
- Permanecen obligatoriamente `NOT_RUN`: reentrada y preservación de guardado de campaña tradicional (A2), paso no cruzado de trabajadores (C3), cobertura integrada de producción/stock y los siete productos/bebidas (C4), continuidad visual de vallas en curvas (B3), responsive de UI Game View a otras relaciones (D2) y validación holística/campaña (D3). Por ello el modo sigue **en implementación, no aceptado**.
