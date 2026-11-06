# Current state — Hay Chori y Paty

**Corte:** 2026-11-06. Resumen vigente compacto; las reglas operativas están en los contratos y la tabla maestra. [Índice SDD](README.md). Se archivó el snapshot extenso anterior en [history/current-state-snapshot-through-20261106.md](history/current-state-snapshot-through-20261106.md).

## Checkout y evidencia de entrega

- Checkout activo `/Users/celestino/HayChoriYPaty`, rama `codex/street-automation`; preservar cambios locales preexistentes. Unity `6000.6.3f1`; Player Settings `0.2.83/code85`.
- Último Development APK documentado (2026-11-06): Main-only, IL2CPP/ARM64, build e instalación física actualizados correctamente en Motorola Edge 60 Fusion sin desinstalar ni borrar datos. La app no se abrió; gameplay smoke, EditMode/PlayMode de ese build: `NOT_RUN`. No confundir APK instalada con aceptación del juego. Detalle/hash en [Android delivery](features/android-prototype.md).
- Desarrollo conserva un grant de QA de `+30` tickets por inicio de proceso fuera del Editor; retirar antes de cualquier distribución release. No se limpiaron datos/saves para esta auditoría.

## Campaña efectiva

- Hay **13 posiciones visibles**: 12 clubes y el bonus Huracán. Orden ID: `0,1,2,3,4,11,5,6,7,8,9,10,12`. Son 11 modos tradicionales; Camioneros ID9 es Tower Defense; Huracán ID11 es Bonus Track; Argentinos Juniors ID12 cierra la campaña regular. No confundir ID persistente con posición.
- La fuente única de metas, tiempos, catálogo, caja, planteles y precios por ID es [BALANCE_ACTUAL_NIVELES.md](../docs/BALANCE_ACTUAL_NIVELES.md). Mecánicas compartidas: [street-automation](features/street-automation.md), Mercado/persistencia: [market-fridge](features/market-fridge.md).
- Main serializa 13 catálogos y 13 duraciones; no serializa `levelBalances`. Los defaults de `StreetBalance` aportan los balances tradicionales. Camioneros TD sustituye operativamente los campos legacy ID9 y no los usa para objetivos/fin de partida. Ver [Tower Defense](features/tower-defense.md).

## Especiales y validación pendiente

- **Camioneros TD:** 10 hinchas finitos, Chori/Coca, sin cuotas/deadline, inicio sin trabajadores, dos áreas compartidas y máximo 3 de cada rol/área. Ahora muestra mesas de choris y barriles cargados, no parrillas, con feedback de entrega física y prioridad de stock compartido corregida. Revisión actual Unity: **36 EditMode únicos PASS; 9 PlayMode únicos PASS y 1 FAIL**. A/B/C y Game View 540×960 pasan; la política probada en ola natural dejó 1 de 36 unidades sin entregar, 9/10 atendidos y un escape. No declarar victoria natural ni aceptación global aprobadas. Contrato/evidencia y límites: [tower-defense](features/tower-defense.md). Suite global/APK/dispositivo `NOT_RUN`.
- **Huracán:** ID11, posición 6, Bonus Track de 120 s con cinco elecciones; umbrales de estrellas aún provisionales. Tests/dispositivo/Game View del nivel completo no equivalen a aceptación total; ver estados criterio por criterio en [contrato](features/huracan-bonus-track.md).
- **Argentinos Juniors:** ID12, posición 13, replica balance de Los Redondos ID10; SaveData v6 y flag propio. Integración inicial: 33/33 EditMode; atlases v2 revisados como assets, pero tests nuevos de importación/escudos, Firestore, Game View y Android `NOT_RUN` o `BLOCKED`, no aceptados como campaña end-to-end. Ver [contrato](features/argentinos-juniors.md).
- No hay evidencia actual que certifique una campaña humana fresca ganada de principio a fin. Los resultados de balance y APKs anteriores son evidencia fechada y acotada, no aceptación vigente.

## Reglas y fuentes

- Runtime C# efectivo → configuración serializada/ScriptableObjects usados → escena Main → pruebas realmente ejecutadas → contratos SDD → informes fechados. Si fuente y serialización discrepan, documentar qué se instancia/consume; no ajustar el juego a un documento antiguo.
- El historial cronológico queda en [status](status.md), [decisions](decisions.md) y [continuidad previa](history/continuity-log-through-20261025.md); no prevalecen sobre reglas actuales.

## Auditoría de assets — 2026-11-06

Limpieza conservadora: retirados `street-cover-loading-v1.png` y su meta (2.493.520 bytes). 472→471 assets; resto preservado. StreetArtTests repite 296 PASS/14 FAIL previos, sin regresiones nuevas; 17 vistas antes/después y startup PlayMode PASS. Partidas completas/APK NOT_RUN: no validación integral. [Informe y clasificación](../docs/reports/AUDITORIA_ASSETS_Y_LIMPIEZA.md). Sin commit/push.
