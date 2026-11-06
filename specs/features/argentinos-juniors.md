# Argentinos Juniors / La Paternal

## Objetivo

Agregar Argentinos Juniors como último nivel normal de campaña, con las fotos aportadas por el usuario como referencia para el mural y el vestuario de hinchas, conservando la presentación, el loop y los sprites 2D del juego. Las camisetas deben representar los cinco estilos enviados: roja con dos líneas blancas paralelas, roja jaspeada, blanca con línea roja, roja con banda diagonal blanca y marfil con hombros/mangas rojos. Cada hincha conserva el mismo uniforme en sus poses normales y de trifulca; no añadir sponsors ni letras. El escudo del club se usa solo desde la imagen oficial aportada por el usuario, sin crear una versión independiente.

## Contrato de campaña y balance

- ID estable nuevo: **12**. Se agrega al final después de Los Redondos (ID10); ninguna posición/ID existente se renumera. Huracán (ID11) conserva su bonus entre Independiente y Racing. Orden visible: `0,1,2,3,4,11,5,6,7,8,9,10,12`.
- El selector existente lo lista como **Argentinos Juniors / La Paternal**, última tarjeta y inicialmente bloqueada.
- Su balance tradicional copia exactamente Los Redondos (ID10), el nivel regular anterior. Las dos filas vigentes están en [la tabla maestra](../../docs/BALANCE_ACTUAL_NIVELES.md); no duplicar aquí cifras de metas/caja/tiers. No copiar la modalidad Tower Defense de Camioneros ni el Bonus Track de Huracán.
- Ganar ID10 habilita ID12. El frontier histórico `unlockedLevel` sigue acotado a 0–10; ID12 usa su propio flag persistido. Ganar ID12 no crea un ID nuevo.
- Migración SaveData v5→v6 solo concede el flag si hay evidencia de una victoria ID10 en estadísticas o un match terminal pendiente; `unlockedLevel == 10` por sí solo solo acredita acceso a Los Redondos. Preservar prefijos existentes de estadísticas y watermarks de Mercado.
- Partidos/estadísticas de ID12 se guardan en su slot independiente. Firestore local permite resultado `level == 12` y rechaza `level > 12`; `unlockedLevel` remoto sigue limitado a10.
- Mural y hinchas tienen recursos dedicados; las camisetas de Huracán/Camioneros/otros clubes no deben reutilizarse como sprites de Argentinos. Los sprites de hinchas integran los cinco patrones de camiseta aportados, sin sponsors ni letras agregadas. No crear un escudo independiente.
- El escudo oficial aportado por el usuario se asigna a Argentinos ID12. Huracán ID11 también tiene su propio escudo oficial aportado por el usuario; cada recurso se enruta por ID y no se intercambian.

## Aceptación

| Criterio | Evidencia | Estado |
|---|---|---|
| Orden de campaña, nombre/ID y siguiente nivel ID10→ID12; ID12 terminal | EditMode `StreetBonusTrackTests` y `StreetTicketsTests` | PASS — job `9f35be2dd95e4a2ba473fb19a6699a2e` |
| Configuración ID12 coincide campo por campo con ID10, incluidas cuotas, duración, caja y plantel de inicio | EditMode `StreetSimulationTests` | PASS — job `9f35be2dd95e4a2ba473fb19a6699a2e` |
| Migración v5 preserva el frontier y solo desbloquea ID12 ante evidencia de victoria ID10 | EditMode `StreetBonusTrackTests` | PASS — job `9f35be2dd95e4a2ba473fb19a6699a2e` |
| Guardado de stats y watermarks de Mercado no colisiona con ID11; local repo acepta v6 | EditMode de estadísticas, Mercado, tickets y snapshots | PASS — job `9f35be2dd95e4a2ba473fb19a6699a2e` |
| Tema, mural y nuevos atlases v2 se cargan desde recursos propios; selector muestra la última tarjeta | EditMode de arte, tema, atlas y selector | BLOCKED — Unity importó las dos texturas, pero el bridge se desconectó antes de ejecutar el job EditMode |
| Los cinco patrones de camiseta aportados aparecen integrados en los hinchas y cada par de poses mantiene uniforme coherente entre normal y trifulca | Inspección visual directa de ambos atlases v2 | PASS — asset review; no equivale a captura Game View |
| El escudo oficial suministrado aparece en Argentinos sin asignarse a Huracán, y se recorta su fondo exterior blanco preservando detalles blancos internos | EditMode `StreetArtTests` (manifiesto, carga del recurso y cutout) | NOT_RUN — pendiente de reintentar Unity tras desconexión del bridge |
| Firestore local acepta ID12 y rechaza ID13 | suite local de reglas/emulador | NOT_RUN |
| Vista Unity del nivel y captura Game View actual | Unity Editor conectado; captura inspeccionada | BLOCKED — MCP perdió la sesión del Editor antes de obtener una captura |

**Validación Unity 2026-11-06 (integración original):** la compilación de scripts terminó sin errores (un warning UAC0005 no relacionado). El job EditMode `9f35be2dd95e4a2ba473fb19a6699a2e` terminó con **33/33 PASS**, 0 fallas y 0 omitidas. La compilación/test inicial había fallado antes de iniciar pruebas por una llave omitida en el test de manifiesto; la llave se corrigió antes de este job exitoso. La suite local Firestore y una captura Game View no se ejecutaron.

**Actualización de camisetas 2026-11-06:** se generaron atlases versionados v2 con cinco diseños basados en las fotos del usuario y asignación idéntica por identidad en poses de caminata/celebración y trifulca; ambos PNG miden 1536×1024, conservan canal alpha y Unity registró su importación en `Logs/Editor.log`. El primer `refresh_unity` agotó 60 s esperando readiness; un refresh posterior recuperó el Editor, pero `run_tests` terminó con `Unity plugin session ... disconnected while awaiting command_result`. Por eso compile/test de esta actualización y captura Game View siguen **BLOCKED/NOT_RUN**, no aprobados.

La edición de `firebase/firestore.rules` no publica reglas en el proyecto remoto. No se declara aceptación Android ni APK hasta que se solicite y se ejecute por separado.
