# Continuidad — Hay Chori y Paty

Checkpoint: 2026-10-05 — Nivel 1 120s y Nivel 2 180s. Nivel 2: servicio del parrillero debajo del mostrador e indumentaria ajustada por pose; suite completa Unity aprobada (116 EditMode + 21 PlayMode). APK 0.2.9/code11 ARM64 generada, verificada e instalada por USB en el Motorola Edge 60 Fusion; PlayerPrefs idénticas antes/después. No se abrió la app ni se hizo revisión visual nativa. Detalle y hash en `specs/status.md` y `specs/features/android-prototype.md`.

## Dónde continuar

- **Proyecto Unity activo: `/Users/celestino/HayChoriYPaty`.** La carpeta `/Users/celestino/Documents/ChatGPT/Hay Chori y Paty` es el contexto inicial del chat, no el checkout activo; no implementar cambios sobre esa copia antigua.
- Unity **6000.6.3f1**, rama `codex/street-automation`, remoto `https://github.com/Cuervation/haychoriypaty.git`.
- Leer `AGENTS.md`, `specs/README.md` y solamente la especificación del próximo pedido. Las reglas vigentes están en `specs/features/street-automation.md`; arte en `specs/visual-reference.md`; Android en `specs/features/android-prototype.md`; evidencias/pendientes en `specs/status.md`. Este archivo es un resumen, no una especificación alternativa.
- Reutilizar skills/agentes existentes, sin instalaciones ni refactors ajenos. Preservar `.meta`, siete productos y estética cartoon 2D vertical argentina. No lanzar un segundo Unity ni borrar progreso del celular.

## Último estado implementado

- Floresta/All Boys: límite120segundos, victoria inmediata al vender1000choripanes (derrota al vencer tiempo si falta la meta), chori fijo $5 sin popup de precio (ni resumen de solo lectura); Ready deja Jugar azul y navegación de niveles, pedidos de 1–4; salida desplaza a los que esperan detrás y el nuevo se incorpora al final; velocidad +10 puntos porcentuales de la base por $25, ayudante $200.
- Cada partida nueva o repetida del primer nivel vuelve a un parrillero y velocidad ×1.00 (cero mejoras), también al cargar progreso previo. Se conservan monedas, precios/desbloqueos; los niveles posteriores mantienen sus reglas de persistencia. Implementación nueva sin probar. Antes de la validación conjunta, adaptar las regresiones antiguas que todavía esperan costos $5/$25 y conservar equipo/velocidad en Floresta.
- Parrillero original de 16 poses, gordo/sin remera/delantal blanco sucio y cara cartoon elegida; parrilla grande argentina, mural cartoon y 15 prendas sobre los hinchas existentes del primer nivel.
- Portada/logo:≥4s de portada completamente visible, aparición del logo/destello,≥4s de logo completamente visible; botones JUGAR/SALIR azules con Luckiest Guy blanca/contorno oscuro desde 8.63s no escalados. Menú persistente: Jugar inicia la partida directamente, Salir cierra player/detiene Play en editor. No inicia ni consume turno mientras se espera. Implementación nueva **sin probar**; regresión existente adaptada pero no ejecutada.

## Evidencia verificada (no confundir con pendientes)

- Última validación **anterior al nuevo menú**: 11/11 tests de arte EditMode y 12/12 StreetPointer PlayMode en editor normal. Hubo dos intentos batch fallidos registrados, no contados como aprobados.
- Capturas reales 1080×1920 de portada/logo/Ready e indumentaria revisadas; progreso de editor restaurado exactamente y Play detenido en Main limpio al cerrar el desarrollo.
- Build MCP `build-1970fbd5d5`: 364.73 s, 0 errores / 3 warnings (símbolos de diagnóstico y splash de Unity). ARM64 IL2CPP, API26 mínimo/API36 objetivo, firma V2 local/development; no firma de Play Store.
- APK **0.2.4 / código6**, paquete `com.haychoriypaty.game`, 106121029 bytes. SHA256 `50e7c191f7818bd6dae8c44e20c4007723d48057500a30c45266fb4508ed7d75`.
- Instalada en Motorola Edge60Fusion USB `ZY22MBNWRB`: `adb install -r` Success, versión verificada, GameActivity cold launch Statusok/769ms y proceso vivo. Sin uninstall/data clear. Otra app tomó el primer plano antes de captura: **no se verificó la nueva apariencia/intro/tacto/rendimiento en el celular**.

## Artefactos conservados fuera de Git

- APK estable: `Builds/Android/Archive/HayChoriYPaty-0.2.4-code6.apk` (mismo hash). La ruta `Builds/Android/HayChoriYPaty-street.apk` es rolling y puede cambiar en futuras compilaciones.
- Capturas y resultados de desarrollo: `Logs/Acceptance/Intro/` y los registros referidos en `specs/status.md`. Build/Logs/Library no se incluyen en commits.
- Fuentes/sprites/metas y specs sí se guardan en Git. Este checkpoint es local; no implica por sí mismo un push remoto.

## Unity MCP / Android para retomar

- CoplayDev Unity MCP 10.0.0 existente, endpoint loopback `http://127.0.0.1:8080/mcp`; estuvo verificado tras reiniciar Unity autorizado. Comprobar estado actual con consultas de solo lectura, no asumir que sigue conectado ni duplicar servidor/config.
- Si el editor se bloquea por Reload/modal, resolver antes de operaciones; no abrir otra instancia ni terminar procesos sin autorización específica.
- SDK/ADB ya instalado: `/Applications/Unity/Hub/Editor/6000.6.3f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`; verificar dispositivo antes de usar el serial y preferir install-r para preservar datos.
- Memoria persistente del proyecto: `haychoriypaty`. Recuperar contexto y usar este checkpoint; no depender de archivos temporales `/private/tmp`.

## Pendientes / siguiente pedido

No ejecutar tests, entrar a Play para probar ni generar APK en este pedido: el usuario quiere probar todo junto después. Retomar solamente lo que pida; verificar presentación y menú antes de afirmar que funcionan en editor/celular. Siguen pendientes pruebas físicas visuales/táctiles/FPS/térmicas, ambientaciones propias de los otros clubes, audio/pulido/balanceo y firma/publicación release. No afirmar que todo el juego está terminado.


## Último cambio de fila (2026-10-04, sin probar)

Los siete carriles conservan FIFO en todos los niveles: detrás→delante dentro de la misma columna, nuevo al final; solo se atiende al cliente ya asentado adelante. Salidas por pedido o paciencia cancelan reservas y compactan; movimiento mantiene pedido/identidad/paciencia, estado Advancing con walking/burbujas existentes. No tests/Play/APK hasta el pedido de validación conjunta; adaptar las regresiones que todavía suponen servicio en filas traseras o reemplazo directo del hueco.


## Último cambio visual (2026-10-04, juego sin probar)

StreetView usa `Resources/street-background-open-street-v4.png` (940×1673): mural al borde superior, sin estadio/cielo de arriba, sin techo/letrero/carteles/soportes del puesto; calle ancha para la gente detrás del mismo mostrador. Íconos sueltos de los dos carteles superiores eliminados; burbujas de pedidos se conservan. Fondo v3/GUID original retenidos. Imagen revisada sola, no en Game-view; no tests/Play/APK/import/compilación verificadas. Anclas del juego intactas. Prompt de ImageGen integrado y hash guardados en GenerationPrompts.md.


## Últimas animaciones de espera (2026-10-04, sin probar)

StreetView agrega respiración, balanceo/apoyo del cuerpo y pequeños saltos dobles de aliento, desfasados por hincha. Reutiliza cada personaje/ropa y AnimationTime existente; restaura GUI.matrix antes de pedido/cantidad/paciencia. No cambia posición lógica, fila FIFO, entregas ni monedas; conserva walking/Advancing y feedback de recepción. Fuentes revisadas solamente; no tests/Play/APK/compilación/importación ni revisión visual en juego. Verificar junto con los cambios anteriores cuando el usuario lo pida.


## Último cambio de ícono (2026-10-04, sin nueva APK)

Logo de la presentación `Resources/street-logo.png` asignado como ícono por defecto y en12slots Android legacy/round. `Assets/Scripts/Editor/StreetAppIcon.cs` prepara el adaptativo al generar el próximo Gradle: copia el PNG original y usa XML con margen20% horizontal/20.92% vertical sobre crema, sin redibujar logo ni tocar presentación. No tests/Play/APK/celular; compilación/import/callback no verificados (MCP sin respuesta de ping). El ícono instalado no cambia hasta la futura build/instalación autorizada.


## Última meta/HUD/moneda (2026-10-04, sin probar)

Usuario confirmó1000choripanes para pasar el primer nivel y mantener180segundos. Defaults/fallback/Main usan1000y flagdeadline-onlyfalse; el handoff1000gana y corta otros trabajadores de ese subpaso. Nueva moneda `Resources/street-coin-gold-v2.png`,1254×1254RGBA,100%dorada con chori/paty en relieve (sin colores/bandera); meta/prompt/hash propios, draft no integrado. HUD fino arriba: saldo con moneda izquierda, ventas/meta derecha (100/1000), reloj abajo; efectos de venta usan misma moneda. Fila, precios/costos/reset/presentación/ícono/animaciones/laterlevels conservados. No tests/Play/APK/compilación/import ni Game-view/celular verificados. Adaptar regresiones de24/deadline-only antes de validación conjunta cuando el usuario lo pida.


## Combined validation checkpoint — 2026-10-04
Latest user now authorizes combined tests/APK/phone update. Active checkout remains`/Users/celestino/HayChoriYPaty`, Unity6000.6.3f1/Android/Main. Updated Street fixtures and targeted EditMode52succeeded/no failures + PlayMode12/12passed; relevant evidence in`features/street-automation.md`. Reviewed actual menu/crowd/HUD and paired idle animation captures in ignored`Logs/Acceptance/Combined`; exact editor save restored, Play stopped. Android0.2.5/code7 development Main-only build`build-47e1c59fff` succeeded277.796s/0errors/3warnings. APK114272726bytes/SHA`99d78efd142a624e612259737664ab013a0667e41d8319a62e6733b3d296403c`, V2same debug certificate, min26/target36/ARM64; original adaptive logo bytes verified. RollingAPK plus0.2.5-code7 archive preserved, older0.2.4 archive untouched. Seven-product Ready/Playing layout also reviewed and exact editor save restored. Motorola serial`ZY22MBNWRB` reconnected under user request; **0.2.5/code7 install-r Success**, PackageManager verified, exact dedicated playerprefs XML unchanged and first-install timestamp retained. Own GameActivity cold launchStatusok/777ms/PID15213 confirmed. USB disconnected again before runtime-log follow-up; that waiting command cancelled. NativeSalir/launcher/touch/performance remain unverified, not installation blockers. No data wipe or rebuild. Evidence`Logs/Acceptance/Combined/summary.json`, full Android delivery in`features/android-prototype.md`.


## Full-screen Parrillero presentation — 2026-10-04
User's0.2.5phone screenshot exposed intro letterboxing and mismatched cover face. StreetView now renders versioned no-hero backdrop across physical screen with proportional crop; exact existing gameplay/hire Parrillero portrait is drawn as foreground, original logo and blue menu stay on unchanged safe-area/input canvas. Black/shade/flash layers also cover full screen;4+4second timing/gameplay unchanged. Asset`street-cover-no-hero-v2.png` +freshmeta/provenance retained, old cover/portrait/logo/metas untouched. FocusedStreetArt17/17 andStreetPointer13/13passed; actual1080×1920and1200×2670menus reviewed in ignored`Logs/Acceptance/IntroFullScreen`. Editor progress and view selection restored, temporary tall QA preset removed, Main stopped. User chose **project only**, explicitly no new APK/install; installed0.2.5 stays old. Source/docs/art changes remain local/uncommitted; GitHub's1b2b99b does not yet include this refinement.


## Latest combined delivery — 2026-10-04, 0.2.6/code8
Authoritative checkout `/Users/celestino/HayChoriYPaty`. 82currentStreetEditMode+15PlayModepassed after fixture-only batch focus isolation (save/restore enums, never replaceInputSettings). Main-only APK0errors/1symbolwarning, signature/logo/package/ARM64 verified, archive0.2.6-code8 preserved. Authorized Motorola install-rSuccess/versionverified, prefs byte-identical, coldlaunchok; native gameplay HUD/mural/Frenchbread/crest reviewed while user plays. No forced interruption, no controlled nativeSalir/intro/launcher/performance certification. Evidence ignoredLogs/Acceptance/Current; phone-menu/result filenames are gameplay captures. Unselected button/icon proposals unchanged. Updated source/spec/assets remain local; no commit/push this turn. Editor batch processes exited, no new packages/helpers left.


## Gameplay fills portrait screen — source complete, native delivery pending
New current request removes the gameplay letterbox too. In authoritative repo, StreetView's CanvasViewport fills safe bounds, CanvasLogicalHeight adapts portrait y anchors/input inverse transform, and background ScaleAndCrop fills the extended scene while interaction sprites remain uniform. Focused StreetArt33/33 EditMode+StreetPointer17/17 PlayMode passed on Unity6000.6.3f1; final screenshots are not yet native because Android build/update not done. Current physical phone is on0.2.6/code8 (does not contain this refinement); installing an update interrupts any unsaved in-flight round, so check app/user state before delivery. No screenshot visual claims for the new layout.
