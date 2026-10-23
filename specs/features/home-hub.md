# Feature: Portada y hub de día de partido

## Alcance vigente — 2026-10-23

La portada reproduce la ilustración proporcionada por el usuario, a pantalla completa y sin botones. Tras autenticar/cargar el perfil obligatorio y mostrarla durante cuatro segundos, el juego avanza automáticamente al hub; la barra de carga es compacta (118×6 unidades lógicas), dibujada por el juego y no forma parte del arte fuente. El cronómetro no consume tiempo mientras la sesión bloquea el juego.

El hub es una escena ilustrada vertical, vista elevada de una calle de partido: se ve parte del estadio, viviendas/vereda, hinchas y un control policial donde se revisan DNI. Los puestos ilustrados funcionan como hotspots, dentro de los límites de su arte y respetando el área segura del canvas:

| Elemento del escenario | Acción | Destino / límite |
|---|---|---|
| Puesto de choripán | JUGAR | Selector de clubes existente; el jugador sigue el flujo de nivel y Mercado actual. No salta la selección ni inicia una partida sin la preparación vigente. |
| Almacén/supermercado | MERCADO | Pantalla Mercado existente; comprar sigue sujeto a saldo/stock/gates actuales. |
| Vendedor ambulante de camisetas | INSIGNIAS | Página de colección provisional, tematizada como el puesto de cancha con camisetas colgadas y en el suelo. La colección/estado se deriva de unlocks y victorias por club ya guardadas (`winsByLevel`); este hub no inventa persistencia de insignias ni premios. |
| Tablero del estadio | RANKING | Muestra canchas desbloqueadas y canchas ya ganadas en el perfil propio, y aclara que el ranking global está pendiente. No consulta ni expone una lista pública de otros jugadores: el contrato Firebase actual no autoriza ese acceso. |
| Avatar/engranaje | CUENTA | Reutiliza el estado/modal Firebase/Google existente para iniciar sesión o cerrar sesión; no modifica la obligación de autenticación. |

Las páginas nuevas vuelven al hub; se reutiliza IMGUI y las rutas/mercado existentes, no se agrega un segundo framework de UI ni servicios. El arte del hub es 941×1672 y sus hitboxes se mapean de la composición full-bleed a la safe area con coordenadas GUI top-origin.

## Aceptación

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
