# Current state — Hay Chori y Paty

**Corte:** 2026-11-27, rama `docs/post-cleanup-state` (subida a `origin`). Resumen corto para retomar; las reglas están en `AGENTS.md`, el detalle en cada contrato y la [tabla de balance](../docs/BALANCE_ACTUAL_NIVELES.md). Índice: [README](README.md). Snapshot anterior: [history/current-state-snapshot-through-20261113.md](history/current-state-snapshot-through-20261113.md).

## Para retomar (Codex u otro agente)

- Unity `6000.6.3f1` (ver `ProjectSettings/ProjectVersion.txt`), proyecto Unity en la raíz del repo, escena `Assets/Scenes/Main.unity`, UI IMGUI (`StreetView` en partials). Comprobación rápida: `bash tools/git/check-repo.sh --staged`; pruebas Unity proporcionales (skill `targeted-testing`).
- Leer solo el contrato de la tarea (tabla del README). `status.md`/`decisions.md`/`history/` son historial.
- APK: `manage_build` Android no-Development IL2CPP/ARM64 → `Builds/Android/Archive/` (ignorado por Git); registro y verificación en [android-prototype.md](features/android-prototype.md) (encabezado = última).
- Pruebas que ya fallaban y no se tocaron: EditMode `RacingBalanceAuditGameplayTests.TestedThirteenWorkerCapacityTeamWinsOnlyInExplicitTheoreticalFixture`; PlayMode `StreetPointerTests.RiotReturnOpensLevelSelectorWithoutStartingAutomaticallyAndDiscardsCoins`. Las capturas manuales del Cocacolero agachándose (`*CocacoleroCrouchFrames`) fallan también. Las pruebas `[Explicit]` son capturas/diagnósticos, no regresiones.

## Campaña (15 posiciones; posición ≠ ID)

`CampaignOrder` = `{0,1,9,2,12,3,13,11,4,5,10,6,7,8,14}`: All Boys, Chicago, **Camioneros (TD)**, Vélez, Argentinos, Ferro, **La Renga (TD)**, **Huracán**, Independiente, Racing, **Los Redondos (TD)**, San Lorenzo, River, Boca, **Peregrinación (TD)**.

- **Huracán ID11 es un nivel normal desde el 2026-11-27** (antes Bonus Track): dificultad entre Ferro e Independiente, menú de Ferro + vacío, cocina pasiva; el código del bonus quedó inactivo (`IsBonusTrack == false`). No suma Puntos de Liga (tope de 14 canchas de las reglas de Firestore). [Contrato](features/huracan-bonus-track.md).
- Reglas "desde Racing" (cocina activa, Mozos, trabajadores por tickets) en `StreetSimulation.IsRacingOrLaterLevel` (ID ≥ 5 salvo Huracán).
- Tower Defense ([contrato](features/tower-defense.md)): Camioneros con rampa ×1,20 + 0,10 por tramo y pedidos de hasta 5 unidades (el equipo máximo no gana; decisión del usuario dejarlo así); Los Redondos con rampa ×1,10 + 0,10 cada 10 hinchas, pedidos de hasta 5 unidades y 4 productos, Fernetero a 120; La Renga y Peregrinación sin cambios recientes. Ningún TD está equilibrado/medido.

## Funciones recientes (2026-11-25 a 27)

- Ranking de la hinchada con podio y precarga al arrancar; 20 filas de relleno en Firestore (borrarlas con `python3 -I tools/firebase/leaderboard_filler.py delete` cuando haya jugadores reales). [firebase-player](features/firebase-player.md).
- Perfil del hincha en el barrio (apodo editable, estadísticas, mail, cerrar sesión) y vitrina de insignias con trofeos. [home-hub](features/home-hub.md), [visual-reference](visual-reference.md).
- La partida en curso recupera el tiempo pasado en segundo plano (hasta 1 h). [product](product.md).
- Revisión visual de todas las pantallas en 10 proporciones (16:9 a plegables) con arreglos; sin probar: recortes de cámara y barra de 3 botones. [visual-reference](visual-reference.md).
- Limpieza de assets y código sin uso (2026-11-27). [informe](../docs/reports/AUDITORIA_ASSETS_Y_LIMPIEZA.md).

## Pendiente / no verificado

- **Huracán como nivel normal (2026-11-27): compila (Editor.log), pero sus pruebas y una partida NO se corrieron** (Unity quedó desconectado con la pantalla bloqueada). Primer paso al retomar: compilar, correr EditMode completo y PlayMode `StreetPointerTests`/`StreetHubViewTests`, y jugar Huracán desde el selector (Mercado → partida → Chulengo/disturbio).
- Balance de Camioneros, Los Redondos y Huracán sin medir; nada se jugó entero en el celular.
- Voces del puestero: borradores con voces de macOS fuera del proyecto (`Logs/Acceptance/PuesteroVoces-20261126/`), no aptas para publicar.
- Grant de QA de +30 tickets por inicio fuera del Editor: retirar antes de una release.
- Disco local: ~29 GB de APKs viejas y capturas en `Builds/` y `Logs/` (no versionados).
