# Tower Defense — Camioneros / Plaza de Mayo

**Estado:** modo integrado y ejecutable en Unity; aceptación pendiente. Sigue en implementación y no se declara completo ni aceptado. Este contrato cubre solamente el primer nivel de la modalidad nueva. La campaña tradicional y sus datos siguen siendo independientes.

## Alcance y límites

- Primer nivel Tower Defense: Camioneros / Plaza de Mayo, tres tramos horizontales y dos zonas intermedias de trabajo.
- La ruta tiene entrada arriba a la izquierda, alterna izquierda→derecha / derecha→izquierda y acaba abajo a la derecha. Los giros conectan cada tramo sin teletransportes.
- El modo mantiene su propio identificador, configuración, simulación y progresión. No insertar este nivel en la campaña, `StreetSimulation.CampaignOrder`, ni en el guardado/selección de campaña de `StreetGame`.
- Reutilizar productos, parrillas, mesas, sprites y animaciones existentes. No duplicar ni cambiar sistemas tradicionales de producción, pedidos, mercado, tickets o balance.
- Derivar valores compatibles de la configuración vigente de Camioneros cuando hagan falta; no inventar economía, metas ni premios.
- Sin temporizadores individuales de paciencia ni abandono por impaciencia. Al salir, registrar si el pedido está completo; la consecuencia de un pedido incompleto queda configurable y sin penalización inventada.
- Implementar primero el nivel de tres tramos. Cinco/siete tramos son validaciones del generador, no niveles desbloqueados.
- No añadir policías, oleadas, compra de torres ni otras reglas que no están en la solicitud.

## Fuentes visuales y activos

- Conservar el lenguaje de dibujo 2D, perspectiva frontal levemente elevada, escala, capas y sprites actuales; las vallas son el único arte nuevo necesario para la geometría.
- El usuario aprobó explícitamente usar como base visual el fondo activo de Camioneros `Assets/Art/Street/Resources/street-background-camioneros-v1.png` (panorámica ilustrada de Plaza de Mayo) y la ruta S de tres tramos que describe la solicitud. El checkout no contiene un mural de pared separado de Camioneros; llamar al recurso por su naturaleza real y no sustituirlo por el mural de otro club. La solicitud adjunta en esta sesión fue solo texto; no incluyó sus dos imágenes de referencia.
- La configuración activa de Camioneros usa actualmente atlas de hinchas de Ferro; no llamarlos hinchas ilustrados específicamente para Camioneros ni redibujarlos silenciosamente.
- Nuevas piezas de valla deben ser sprites separados, reutilizables, con escala/pivotes/conectores consistentes. El generador debe crear piezas GameObject individuales y collider/límites coherentes; no hornear la ruta en una imagen ni en una valla gigante.

## Arquitectura vigente para el modo nuevo

Mantener un conjunto desacoplado de config de nivel, geometría de ruta, ensamblado modular de vallas, seguidores de ruta, estaciones/alcances y coordinador del modo. Los nombres pueden seguir las convenciones existentes. La campaña no debe convertirse en autoridad del Tower Defense. `StreetKitchenProduction` solo se reutiliza si su API transaccional resulta compatible; `StreetKitchenLayout` y `StreetKitchenRenderer` están ligados a la simulación/escena de campaña y no son una base de navegación Tower Defense.

La configuración debe permitir cantidad y longitudes de tramos, ancho de corredor, separación/altura, límites del área jugable, entrada/salida, espacios de estación, prefabs/sprites de valla y puntos de servicio. Waypoints y alcances deben ser inspeccionables. Las configuraciones fuera de pantalla deben advertir/fallar validación: nunca encoger personajes o deformar arte automáticamente.

## Criterios obligatorios de aceptación

Estados permitidos para evidencia: `PASS`, `FAIL`, `BLOCKED`, `NOT_RUN`, `NOT_APPLICABLE` (con motivo). Al iniciar el trabajo, todo criterio de runtime/Unity está `NOT_RUN`.

### A. Dominio y campaña

- `PASS` — Tower Defense tiene ID/config/estado propios y Camioneros es su nivel 1. `TowerDefenseGameMode` se inició en Play con `tower-defense.camioneros.plaza-de-mayo.level-1`; fixture de dominio invocado manualmente en Editor.
- `NOT_RUN` — El modo tradicional permanece intacto, mantiene su ID/config/guardado y se puede iniciar después de salir de Tower Defense.
- `PASS` — No se agregan metas, tiempos, precios, vidas ni penalizaciones nuevos sin evidencia/configuración previa. Duración, metas y saldo inicial se leen de Camioneros; monedas llegan de las ventas al precio unitario oficial; no existe premio nuevo por completar.

### B. Ruta y vallas

- `PASS` — Tres tramos jugables conectados: entrada izquierda del tramo superior; derecha; giro hacia abajo; izquierda; giro hacia abajo; derecha; salida abajo a la derecha. La ruta S se capturó en Game View con hinchas en movimiento.
- `PASS` — Doble valla construida a partir de segmentos SpriteRenderer independientes, con rectas, curvas/codos, terminales y señal direccional modular. Verificación directa en Editor de `GeneratesIndependentFenceSpritesAndTwoContinuousColliderRails` (no vía Test Runner).
- `NOT_RUN` — Vallas y límites son coherentes con la ruta; no hay huecos en giros y el seguidor no puede abandonar la polilínea configurada.
- `PASS` — Longitudes configurables y generador reutilizable para 3, 5 y 7 tramos; alturas/separaciones/salida validadas contra área visible, sin escalado arbitrario. Geometría 3/5/7 pasó invocación manual y la comprobación de layout pasó para 720×1280, 1080×1920 y 720×1600; el layout usó `Camera.pixelWidth/Height`.

### C. Hinchas, trabajo y pedidos

- `PASS` — Hinchas conservan ID, pedido/productos pendientes, distancia/posición de recorrido y estado; siguen waypoints continuamente, orientan sprites y terminan en salida sin atascarse. Captura real muestra varios seguidores sobre la ruta; verificaciones manuales de giros y avance multi-waypoint pasaron.
- `PASS` — Existen dos zonas de trabajo entre los tramos 1–2 y 2–3, sin invadir corredor ni ocultar interfaz. Se ven ambas zonas de estaciones en captura Game View 1080×1920.
- `NOT_RUN` — Estaciones usan equipo/sprites originales; trabajadores sólo trabajan productos para los que están habilitados y se mueven por puntos transitables sin atravesar vallas.
- `NOT_RUN` — Producción continua, stock físico, especialidades, preparación/recolección/entrega actualizan una única vez el pedido correcto y el indicador de objetivo.
- `PASS` — Alcances de servicio configurables y verificables; salida detecta pedido completo/incompleto sin introducir paciencia o penalización no especificada. Pruebas de dominio invocadas manualmente cubren la entrega física de un sándwich y salida incompleta sin timeout/cargo.

### D. Presentación, accesibilidad y validación

- `PASS` — Nivel vertical con HUD superior, fondo de Camioneros/Plaza de Mayo, ruta/estaciones y panel inferior funcional; se mantiene el estilo/sorting y la campaña conserva su HUD por el render condicional del overlay TD. Captura real Game View 1080×1920 en `Assets/Screenshots/tower-defense-game-view-2026-11-03-final.png`.
- `NOT_RUN` — La vista vertical responsive conserva proporciones y muestra HUD, salida, vallas, estaciones y controles sin recorte a distintas relaciones de aspecto.
- `NOT_RUN` — Hay verificación real en Unity de rutas de 3/5/7, varios hinchas, producción/entrega, encaje de vallas y regresión de la campaña tradicional.
- `PASS` — Hay capturas reales de Game View de esta implementación; una composición estática o previsualización no cuenta como captura Unity.

## Configuración vigente y pendientes

- Camioneros de campaña es `LevelIndex 9`; su duración vigente es 380 s y su balance/catálogo están en `StreetBalance`. No usar ese índice como ID de misión Tower Defense ni modificar esos valores.
- La configuración inicial Tower Defense debe limitarse al recorrido de tres tramos, doble valla y dos zonas de la solicitud.
- Valores finales de pedidos incompletos, objetivos de misión/resultado y persistencia específica siguen sin definición; mantenerlos explícitos/configurables en vez de inventarlos.
- Las imágenes Image 1/Image 2 mencionadas en el texto pegado no llegaron en esta sesión. El usuario autorizó usar el fondo panorámico existente de Plaza de Mayo, de modo que la ausencia de un mural de pared separado no bloquea el primer nivel. No sustituir por Ferro/All Boys.

## Validación

- Auditoría estática de checkout/activos y versión Unity `6000.6.3f1`: realizada antes del runtime; hallazgos en `docs/design/tower-defense/README.md`.
- 2026-11-03: añadidos config independiente (`tower-defense` / `tower-defense.camioneros.plaza-de-mayo.level-1`), geometría, seguidor de hinchas, generador de valla y pruebas EditMode focalizadas; importados el sprite modular base, señal y tres prefabs de piezas. `TowerDefenseAssetBuilder` ejecutado en la instancia Unity 6000.6.3f1; Console confirmó creación de tres prefabs, señal y config. Esto solo valida el pipeline de assets, no el modo jugable ni el ensamblaje de la ruta en una escena.
- `validate_script` de geometría, seguidor, generador, builder y pruebas: sin errores; config también sin errores tras cambiar su inicialización inline. La comprobación de whitespace focalizada no halló espacios finales en los archivos nuevos; el `git diff --check` global sí expuso whitespace preexistente en metas de arte de cocina fuera del alcance, que se preservó.
- En el primer checkpoint, el job EditMode `5f55dfcadd294e8a9992a5a24e715467` no inició pruebas (`0` ejecutadas) y expiró a los 120 s con `Test job failed to initialize`; no fue un resultado de aprobación/fallo de assertions. En aquel checkpoint aún faltaban coordinador, fans animados, producción/entrega integrada, selector y HUD. La implementación/integración progresó el 2026-11-03; su evidencia actual está abajo.
- No declarar completo el modo mientras haya un criterio obligatorio en `FAIL`, `BLOCKED` o `NOT_RUN`.

### 2026-11-03 — Integración en Main y comprobaciones parciales (no aceptación final)

- Se conectaron selector de modo desde el hub, modo de campaña tradicional, inicio/salida TD, presentación de vallas/estaciones/hinchas/trabajadores, HUD/controles y pausa de la simulación de campaña. La secuencia de acción del hub/selector y `StartTowerDefenseRun` se ejercitaron en Play vía invocación directa del handler; no equivale a verificar hitboxes con entrada humana.
- Captura real Unity Game View, 1080×1920: `Assets/Screenshots/tower-defense-game-view-2026-11-03-final.png`. Durante el run un muestreo registró 27 hinchas activos y 27 entregas; la captura posterior muestra contadores/saldo avanzados por ventas oficiales. Se usó el fondo panorámico de Plaza de Mayo autorizado. La captura deja ver una escena densa y los módulos de valla se perciben separados en algunas curvas; la continuidad visual requiere revisión, aunque el test estructural confirma dos `EdgeCollider2D` continuos.
- Muestreo de pedidos corregido para replicar el algoritmo oficial de demanda por metas/tiempo/pedidos pendientes; cada cantidad sigue asociada a su producto. `validate_script` del coordinador y fixture EditMode: 0 errores/advertencias. La última lectura de Console, tras reconectar Unity MCP, devolvió 0 errores; sí mostró advertencia de compresión PVRTC obsoleta para el PNG de captura y logs de desconexión/reconexión WebSocket. En una lectura anterior también aparecieron warnings en `StreetView.cs`/`StreetSceneBuildGuardTests.cs` y limpieza de job obsoleto, sin causalidad determinada.
- **Invocación manual de cuerpos de pruebas en Editor (no NUnit Test Runner):** 5/5 `TowerDefenseGameModeTests` (incluida nueva regresión de demanda), 3/3 geometrías de ruta para filas 3/5/7, prueba de generador modular 1/1, y dos verificaciones de seguidor 2/2. Es evidencia acotada; no reemplaza al Test Runner.
- **Test Runner EditMode:** job `63eb53210e4b4952b0af1d1380713dd0` falló al inicializar con `completed=0` (se configuró un timeout de inicio de 30 s); ningún test comenzó. El job previo `0c1fafe76f4644d3ab02f92dbe2607ec` pasó 8/8 antes de las modificaciones recientes y no valida esta revisión.
- **Layout responsive:** 9/9 comprobaciones de geometría/render target a 720×1280, 1080×1920 y 720×1600 para rutas de 3/5/7 filas. Solo una Game View de la implementación se capturó en 1080×1920; HUD completo a otras relaciones sigue `NOT_RUN`.
- Permanecen obligatoriamente `NOT_RUN`: reentrada y preservación de guardado de campaña tradicional (A2), paso no cruzado de trabajadores (C3), cobertura integrada de producción/stock y los siete productos/bebidas (C4), continuidad visual de vallas en curvas (B3), responsive de UI Game View a otras relaciones (D2) y validación holística/campaña (D3). Por ello el modo sigue **en implementación, no aceptado**.
