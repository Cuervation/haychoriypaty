# Feature: Portada y hub de día de partido

## Alcance vigente — 2026-10-23

La portada reproduce la ilustración proporcionada por el usuario, a pantalla completa y sin botones. Tras autenticar/cargar el perfil obligatorio y mostrarla durante cuatro segundos, el juego avanza automáticamente al hub; la barra de carga es compacta (118×6 unidades lógicas), dibujada por el juego y no forma parte del arte fuente. El cronómetro no consume tiempo mientras la sesión bloquea el juego.

El hub es una escena ilustrada vertical, vista elevada de una calle de partido: se ve parte del estadio, viviendas/vereda, hinchas y un control policial donde se revisan DNI. Los puestos ilustrados funcionan como hotspots, dentro de los límites de su arte y respetando el área segura del canvas:

| Elemento del escenario | Acción | Destino / límite |
|---|---|---|
| Puesto de choripán | JUGAR | Selector de clubes existente; el jugador sigue el flujo de nivel y Mercado actual. No salta la selección ni inicia una partida sin la preparación vigente. |
| Almacén/supermercado | MERCADO | Pantalla Mercado existente; comprar sigue sujeto a saldo/stock/gates actuales. |
| Vendedor ambulante de camisetas | INSIGNIAS | Colección de21insignias por ventas acumuladas:500/2500/5000 para cada uno de los7productos, derivadas de estadísticas del perfil Firebase (ver delta vigente abajo). Sin premios nuevos. |
| Tablero del estadio | RANKING | Muestra canchas desbloqueadas y canchas ya ganadas en el perfil propio, y aclara que el ranking global está pendiente. No consulta ni expone una lista pública de otros jugadores: el contrato Firebase actual no autoriza ese acceso. |
| Avatar/engranaje | CUENTA | Reutiliza el estado/modal Firebase/Google existente para iniciar sesión o cerrar sesión; no modifica la obligación de autenticación. |

Las páginas nuevas vuelven al hub; se reutiliza IMGUI y las rutas/mercado existentes, no se agrega un segundo framework de UI ni servicios. El arte del hub es 941×1672 y sus hitboxes se mapean de la composición full-bleed a la safe area con coordenadas GUI top-origin.

## Aceptación histórica del hub (0.2.64)

- [x] Portada sin botones superpuestos y barra de progreso pequeña; pasa automáticamente al hub.
- [x] Hub con hotspots para jugar, Mercado, Insignias, Ranking y Cuenta; entrada/salida preserva la autenticación y flujos actuales.
- [x] Insignias muestran colección temporal derivada de desbloqueos/victorias ya guardados, sin reclamar persistencia o premios nuevos; la última también puede obtenerse al ganar Los Redondos. Ranking no presenta datos globales ficticios.
- [x] Mapeo de hotspots cubierto por prueba geométrica con insets superiores/inferiores asimétricos.
- [x] Compilación Unity y pruebas focalizadas finales: **20/20 EditMode + 10/10 PlayMode**. Capturas reales de UI PlayMode a1080×1920 revisadas independientemente: portada, hub, insignias y ranking. Corregidos recorte de nombres y contraste en Insignias.
- [x] APK Android **0.2.64/code66**, Main-only Development IL2CPP/ARM64: build Succeeded,0errores/1warning; metadatos, ZIP, alineación, firma APKv2 y bindings de escena verificados.
- [ ] Validación física en Android y login Google online: **NOT_RUN** en esta entrega. Las pruebas usan el gate local aislado, no una sesión de usuario real.

## Evidencia de entrega — 2026-10-23

Unity6000.6.3f1 se ejecutó en copia aislada, después de confirmar que el usuario había cerrado el Editor. Sin Unity MCP disponible en este chat; sin modificar trust/servicios/seguridad global. Las65fuentes de runtime/escena coinciden con commit7d44f62; los cambios locales ajenos de barriles ilimitados se preservaron en el checkout y se excluyeron del snapshot final.

Artefacto local `/private/tmp/HayChoriYPaty-0.2.64-code66-hub.apk`:370.871.429bytes, SHA-256 `0e5b6ad72fa135e6f06d0e74e43c9f6f71c0027d0a9ad27d471596fe7a591fe3`. Es debug Development, no firmado para PlayStore; no se instaló. Recibos/capturas: `/private/tmp/hcyp-hub-0.2.64/final/`; informe de build: `/private/tmp/hcyp-hub-0.2.64/build-report.txt`. El tamaño del APK se toma del archivo, no del campo agregado de BuildReport.

**Límite de validación:** la corrida exploratoria ampliada sobre el workspace inicial ejecutó184casos:173PASS/11FAIL. Se corrigió la expectativa obsoleta RGBA32 de portada/logo (Android ya usa ETC2), y el recheck final enfocado quedó verde. Las otras10fallas son reflexión ambigua de rutas y expectativas históricas de plantilla/campaña/balance fuera de este pedido; no se cambiaron gameplay/economía para forzar su aprobación. Los reruns no se suman como casos únicos adicionales. La referencia directa de interfaz en una prueba local ajena impedía compilar: se sustituyó por proxy/reflection, conservando sus demás cambios locales; ese trabajo no se incorporó al release.

## Insignias por ventas — delta vigente 2026-10-23

Reemplaza la colección provisional por clubes. Cada producto (Chori, Paty, Bondiola, Vacío, Coca, Fernet, Cerveza; IDs0–6) tiene3insignias acumulativas e inclusivas: **bronce500, plata2500, oro5000**. Total21. Una venta de Chori no cuenta para otro producto. Se muestran total vendido, avance por objetivo, conseguido/en progreso, estado de sincronización y advertencia de historial antiguo parcial cuando corresponde. Se reutilizan iconos, paneles y botón estándar; no se añaden premios ni cambios de gameplay.

Ventas: `SaveData.statistics.deliveredByProduct`, expuesta por `StreetGame.GetCumulativeProductSales` sin arrays mutables. Insignias obtenidas: `statistics.earnedProductSalesBadges[21]`, índice estable `producto*3+tier`, expuestas por `HasProductSalesBadge`. Se otorgan al consolidar entregas/recuperar checkpoint y se conservan una vez obtenidas; Normalize migra los perfiles existentes usando solo ventas registradas. Incluye entregas reales de resultados ganados/perdidos/abandonados confirmados y checkpoints recuperados por el flujo vigente. La UI muestra acumulado consolidado: tras victoria espera la elección de Chulengo; no suma otra vez pendingVictoryMatch ni activeAttemptSummary y multiplicar premio no multiplica ventas. No reconstruye ventas legacy con unlocks/metas/saldo.

Firebase almacena tanto los7totales como los21flags de insignias obtenidas dentro del mismo snapshot v4 de `players/{uid}/state/current`. Son campos aditivos compatibles de estadísticas; no hay otro documento por medalla, cambio de schema/reglas ni llamada de red por venta. Se conserva outbox porUID, deduplicación yCAS/conflicto existentes. Offline conserva progreso local pendiente; reinstalación recupera solo lo confirmado remoto y un crash solo el último checkpoint.

Aceptación de este delta (obligatoria):
- [x] Umbrales499/500,2499/2500,4999/5000, independencia7productos, IDs inválidos/long y migración sin ventas inventadas; flags obtenidos permanentes y recuperación deduplicada.
- [x] Roundtrip local snapshot/outbox/ACK/reapertura conserva totales y21flags explícitos de medallas, con aislamientoUID; reintentos no duplican. No sustituye roundtrip remoto real.
- [x] UI PlayMode real1080×1920:7productos,21medallas, progreso y botón regresar legibles; muestra QA9/21, no progreso real del usuario.
- [x] Unity6000.6.3f1:32/32EditMode +1/1PlayMode finales PASS, sin sumar reruns; geometría filas/footer verificada para900/960/1200unidades lógicas.
- [ ] Firebase remoto real de este delta: no aprobado sin roundtrip remoto autenticado (evidencia local no lo sustituye).

Evidencia local final: `/private/tmp/hcyp-sales-badges/persisted/` (XML, logs y captura de insignias). No hubo APK nueva, instalación, push ni escrituras remotas de prueba; la APK0.2.64 instalada previamente no contiene este delta. Los cambios locales ajenos se preservaron.
