# Continuidad — Hay Chori y Paty

Checkpoint: 2026-10-04, después de instalar la APK 0.2.4 en el celular.

## Dónde continuar

- **Proyecto Unity activo: `/Users/celestino/HayChoriYPaty`.** La carpeta `/Users/celestino/Documents/ChatGPT/Hay Chori y Paty` es el contexto inicial del chat, no el checkout activo; no implementar cambios sobre esa copia antigua.
- Unity **6000.6.3f1**, rama `codex/street-automation`, remoto `https://github.com/Cuervation/haychoriypaty.git`.
- Leer `AGENTS.md`, `specs/README.md` y solamente la especificación del próximo pedido. Las reglas vigentes están en `specs/features/street-automation.md`; arte en `specs/visual-reference.md`; Android en `specs/features/android-prototype.md`; evidencias/pendientes en `specs/status.md`. Este archivo es un resumen, no una especificación alternativa.
- Reutilizar skills/agentes existentes, sin instalaciones ni refactors ajenos. Preservar `.meta`, siete productos y estética cartoon 2D vertical argentina. No lanzar un segundo Unity ni borrar progreso del celular.

## Último estado implementado

- Floresta/All Boys: 180 segundos, chori fijo $5 sin selector, pedidos de 1–4 con salida/reemplazo; velocidad +10 puntos porcentuales de la base por $5, ayudante $25.
- Partida nueva: un parrillero y velocidad ×1.00. Migración única v1→v2 corrige valores de desarrollo; las compras normales v2 se conservan.
- Parrillero original de 16 poses, gordo/sin remera/delantal blanco sucio y cara cartoon elegida; parrilla grande argentina, mural cartoon y 15 prendas sobre los hinchas existentes del primer nivel.
- Portada y logo elegidos: intro de 4.1 segundos en tiempo no escalado, aparición del logo/destello y vuelta a Ready. Bloquea entradas durante la intro, no inicia ni consume el turno.

## Evidencia verificada (no confundir con pendientes)

- Última validación: 11/11 tests de arte EditMode y 12/12 StreetPointer PlayMode en editor normal. Hubo dos intentos batch fallidos registrados, no contados como aprobados.
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

No hay desarrollo adicional autorizado en este cierre. Retomar solamente lo que pida el usuario. Siguen pendientes pruebas físicas visuales/táctiles/FPS/térmicas, ambientaciones propias de los otros clubes, audio/pulido/balanceo y firma/publicación release. No afirmar que todo el juego está terminado.
