# Current state — Hay Chori y Paty

**Corte:** 2026-11-10. Resumen vigente compacto; las reglas operativas están en los contratos y la tabla maestra. [Índice SDD](README.md). Se archivó el snapshot extenso anterior en [history/current-state-snapshot-through-20261106.md](history/current-state-snapshot-through-20261106.md).

## Checkout y evidencia de entrega

- Checkout activo `/Users/celestino/HayChoriYPaty`, rama `codex/street-automation`; preservar cambios locales preexistentes. Unity `6000.6.3f1`; Player Settings `0.2.83/code85`.
- APK local más reciente (2026-11-10): `Builds/Android/Archive/HayChoriYPaty-0.2.83-code85-camioneros-tutorial-20261110.apk`, Development IL2CPP/ARM64, solo `Main.unity`, 422,576,832 bytes, SHA-256 `d6829dbb9452ffb0e3241a1303fe20799ba0cda70336ec03bb77a57ff674e2ee`. Unity 6000.6.3f1 BuildPipeline mediante MCP succeeded en 341.88 s (0 errores/7 advertencias); ZIP, `zipalign`, firma v2 y metadata paquete/versión/API/ABI **PASS**. Contiene el tutorial Camioneros; sus dos pruebas PlayMode pasaron 2/2 y se revisaron capturas reales de ambos pasos. No instalada ni lanzada; no se hizo smoke físico. El build/tutoría focalizada no implican aceptación completa de Tower Defense. Detalle en [Android delivery](features/android-prototype.md).
- Desarrollo conserva un grant de QA de `+30` tickets por inicio de proceso fuera del Editor; retirar antes de cualquier distribución release. No se limpiaron datos/saves para esta auditoría.

## Campaña efectiva

- Hay **13 posiciones visibles**: 12 clubes y el bonus Huracán. Orden ID: `0,1,9,2,12,3,11,10,4,5,6,7,8`. Son 10 modos tradicionales; Camioneros ID9 y Los Redondos ID10 son Tower Defense; Huracán ID11 es Bonus Track; Argentinos Juniors ID12 ocupa posición 5 y Boca cierra la campaña. No confundir ID persistente con posición.
- La fuente única de metas, tiempos, catálogo, caja, planteles y precios por ID es [BALANCE_ACTUAL_NIVELES.md](../docs/BALANCE_ACTUAL_NIVELES.md). Mecánicas compartidas: [street-automation](features/street-automation.md), Mercado/persistencia: [market-fridge](features/market-fridge.md).
- Main serializa 13 catálogos y 13 duraciones; no serializa `levelBalances`. Los defaults de `StreetBalance` aportan los balances tradicionales. Los dos Tower Defense sustituyen operativamente los campos legacy ID9/ID10 y no los usan para objetivos/fin de partida. Ver [Tower Defense](features/tower-defense.md).

## Especiales y validación pendiente

- **Mercado — permisos/contribuciones (2026-11-06):** la pestaña y compra de Habilitación Municipal / Contribución Policial están implementadas estáticamente a $500 cada una, con flags ligados a la preparación y sin efecto jugable. El build Android de la copia aislada del working tree actual pasó; EditMode/PlayMode y las cinco capturas a 540×960 / 1080×1920 siguen pendientes. Ver [informe](../docs/reports/MERCADO_PERMISOS_Y_CONTRIBUCIONES.md).
- **Tower Defense (Camioneros ID9 y Los Redondos ID10):** ambos usan 60 hinchas por partida y `fanSpeed=0.1575`. La APK local más reciente contiene la entrada progresiva desde el borde izquierdo de Camioneros ID9, junto a los atlas, formación de trabajadores y espaciamiento vigentes; Los Redondos mantiene su entrada anterior. En Unity, el 2026-11-09 pasaron la regresión EditMode de las poses (1/1), la ola integrada PlayMode de 60 (1/1: 60 atendidos, cero escapes y victoria) y el ciclo visual PlayMode (1/1: texturas 0→1→2→0 al avanzar 18 px lógicos). Se capturaron Game Views reales a 540×960, 720×1280 y 1080×1920; Main quedó activa y el Editor fuera de Play Mode. Esas pruebas y capturas preceden a los cambios más recientes. **El tutorial de dos pasos para Camioneros ID9 indica explícitamente elegir cualquiera de las dos áreas para Parrillero y Cocacolero, conserva tarjetas/controles existentes y mantiene la partida pausada mientras se lee. En Unity 6000.6.3f1 pasó PlayMode 2/2 (job `d0d3fe53eec84a80af806f5dd0177601`) y se revisaron capturas Game View 540×960 de ambos pasos; Main quedó activa, fuera de Play Mode.** La regresión de entrada progresiva actualizada antes no ejecutó casos en su intento de Test Runner; el build posterior pasó sin ejecutar esa prueba. La APK nueva no se instaló ni se hizo smoke físico. Ver [contrato Tower Defense](features/tower-defense.md) y [Android delivery](features/android-prototype.md).
- **Huracán:** ID11, posición 7, Bonus Track de 120 s con cinco elecciones; umbrales de estrellas aún provisionales. Tests/dispositivo/Game View del nivel completo no equivalen a aceptación total; ver estados criterio por criterio en [contrato](features/huracan-bonus-track.md).
- **Argentinos Juniors:** ID12, posición 5, conserva el balance tradicional anterior de Los Redondos; SaveData v7 conserva el acceso mediante desbloqueo por ID. Integración inicial: 33/33 EditMode; atlases v2 revisados como assets, pero tests nuevos de importación/escudos, Firestore, Game View y Android `NOT_RUN` o `BLOCKED`, no aceptados como campaña end-to-end. Ver [contrato](features/argentinos-juniors.md).
- No hay evidencia actual que certifique una campaña humana fresca ganada de principio a fin. Los resultados de balance y APKs anteriores son evidencia fechada y acotada, no aceptación vigente.

## Reglas y fuentes

- Runtime C# efectivo → configuración serializada/ScriptableObjects usados → escena Main → pruebas realmente ejecutadas → contratos SDD → informes fechados. Si fuente y serialización discrepan, documentar qué se instancia/consume; no ajustar el juego a un documento antiguo.
- El historial cronológico queda en [status](status.md), [decisions](decisions.md) y [continuidad previa](history/continuity-log-through-20261025.md); no prevalecen sobre reglas actuales.

## Auditoría de assets — 2026-11-06

Limpieza conservadora: retirados `street-cover-loading-v1.png` y su meta (2.493.520 bytes). 472→471 assets; resto preservado. StreetArtTests repite 296 PASS/14 FAIL previos, sin regresiones nuevas; 17 vistas antes/después y startup PlayMode PASS. Partidas completas/APK NOT_RUN: no validación integral. [Informe y clasificación](../docs/reports/AUDITORIA_ASSETS_Y_LIMPIEZA.md). Sin commit/push.
