# La Renga ID13 — implementación y límites de entrega

**Fecha:** 2026-11-10. Checkout `/Users/celestino/HayChoriYPaty`, Unity 6000.6.3f1. **Estado: implementación incorporada, aceptación completa pendiente.** No commit/push, APK, deploy Firebase ni escritura en perfil real. Se preservó trabajo local previo, incluido Camioneros 120 / 0.20475 y su ajuste de globo.

## Resultado incorporado

- ID estable **13**, posición **9**, `La Renga – Autódromo Bs. As.`. Orden `0,1,9,2,12,3,11,10,13,4,5,6,7,8`; cierre común 10→13→4. Sin reasignar IDs previos.
- `LaRenga-AutodromoBuenosAires.asset`: 60 hinchas, velocidad .1575, separación 96 px, $500, cero trabajadores, Chori/Coca, P/C desde $60, tres tramos y dos áreas. Sin deadline. Camioneros conserva120/.20475 y Redondos60/.1575.
- Reutiliza `TowerDefenseGameMode`, ruta/follower, doble valla modular, estaciones/producción/recogida/lanzamiento/recepción, economía y liquidación del intento. No escena/motor/cocina paralelos ni roles nuevos. Tutorial sigue exclusivo de ID9.
- Mural panorámico e insignia originales y doce identidades propias en atlas lateral/reversa por flipX, descendente y recepción. Dos fases por identidad; salida por desaparición compartida. La metadata direccional es opcional y conserva el gait anterior de ID9/10. Globo ID13 anclado al límite superior real del sprite, con 6 px lógicos libres incluyendo bob.
- SaveData **8**, envelope local/remoto aún **1**. Copia bits0–12 y amplía a14. Abre13 por accesos anteriores4–8 o victoria no-legacy TD10, no por frontera legacy10. Estadísticas14; snapshots v7=13 y v8=14 aceptados según versión. No se modifican bytes/hash/UUID/revisión de outbox antiguo. Firestore admite match.level13 en reglas locales, **sin desplegar**.

## Archivos relevantes

| Área | Archivos creados o modificados por esta integración |
|---|---|
| Contrato/estado | `specs/features/la-renga.md`, `specs/README.md`, `current-state.md`, `architecture.md`, `status.md`, `features/tower-defense.md`; `docs/BALANCE_ACTUAL_NIVELES.md`, conteo en `AGENTS.md` |
| Core | `Assets/Scripts/Core/StreetSimulation.cs`, `StreetGame.cs`, `StreetPlayerStatistics.cs`, `StreetMarket.cs` |
| Persistencia | `Assets/Scripts/Cloud/PlayerLocalRepository.cs`, `firebase/firestore.rules` |
| Config | `Assets/Scripts/TowerDefense/TowerDefenseLevelConfig.cs`, `TowerDefenseEconomyProfile.cs`, `Assets/Editor/TowerDefenseAssetBuilder.cs`, nuevo asset y meta `Assets/Resources/TowerDefense/LaRenga-AutodromoBuenosAires.asset`, extensión aditiva de arrays `Assets/Scenes/Main.unity` |
| Presentación | `Assets/Scripts/UI/ClubVisualTheme.cs`, `StreetView.cs`, `StreetView.TowerDefense.cs`, `Assets/Scripts/TowerDefense/TowerDefenseWorldPresentation.cs` |
| Arte nuevo | Cinco PNG `*larenga*` y metas en `Assets/Art/Street/Resources/`; [procedencia](../design/la-renga/ART_PROVENANCE.md) y manifest SHA-256 |
| Pruebas | Nuevos `Assets/Tests/EditMode/LaRengaCampaignTests.cs`, `LaRengaVisualTests.cs` y metas; regresiones adaptadas en tests de campaña/estadísticas/repositorio/arte y nuevos casos LaRenga en `Assets/Tests/PlayMode/TowerDefensePlacementViewTests.cs` |

Los archivos compartidos ya tenían cambios locales: esta tabla no afirma propiedad de todos sus diffs. No se hizo staging ni limpieza global.

## Comprobaciones y resultado

| Comprobación | Estado | Evidencia |
|---|---|---|
| Compilación estática runtime | PASS | Roslyn instalado de Unity, respuesta Bee con salida/refout temporales; exit0, `Logs/Acceptance/LaRenga-20261110/Assembly-CSharp.log` |
| Compilación estática Editor | PASS | `Assembly-CSharp-Editor.log`, exit0 |
| Compilación estática EditMode | PASS | `HayChoriYPaty.EditModeTests.log`, exit0; no ejecución de assertions |
| Compilación estática PlayMode | PASS | `HayChoriYPaty.PlayModeTests.log`, exit0; no ejecución de assertions |
| Configs/orden/identidad por inspección | PASS | Valores YAML/código, manifest PNG; revisión independiente. No demuestra gameplay |
| Importación/compilación del Editor | BLOCKED | MCP sin respuesta; éxito Roslyn no la sustituye |
| EditMode LaRenga | BLOCKED | `run_tests` agotó timeout de120s sin job ID; **cero tests confirmados ejecutados** |
| PlayMode LaRenga / regresiones | NOT_RUN | No lanzados después del bloqueo |
| Game View 540×960 /1080×1920 /720×1280 | BLOCKED | No existen capturas reales nuevas; no se presentan renders generados como Game View |
| Firebase real / APK / dispositivo | NOT_RUN | Sin autorización de publicación/build; no se tocó perfil real |

La evidencia de cada criterio obligatorio LR1–LR10 está en el [contrato](../../specs/features/la-renga.md) y la [revisión independiente](LA_RENGA_REVISION_INDEPENDIENTE_2026-11-10.md). Ninguno de los criterios dinámicos se declara aprobado por compilación estática.

### Cobertura añadida, todavía no ejecutada

EditMode: orden y catálogos; migraciones v7/v8, legacy ID10 y conservación de datos; cierre real10→13→4 y retries sin duplicar premios/tickets; formas de snapshot y estabilidad byte/hash/UUID/revisión outbox tras reinicio; configs; texturas/importer/identidades/source rects y selección de poses sin alterar9/10.

PlayMode: ola natural de60 con hires legales y victoria tras salidas; derrota sin trabajadores; pedido mixto con entrega física y proyección común; captura de ambas áreas con productos en vuelo a tres resoluciones. El caso visual retiene nueve objetivos para composición aislada: no demuestra una victoria natural; ésta tiene prueba separada.

## Problemas encontrados y correcciones

- Clave visual con barra no coincidía con nombre en dash: corregida coincidencia exacta y agregada aserción; footer acepta slash/dash.
- Metas clonados conservaban subassets ajenos: reemplazados por importers limpios preservando GUIDs nuevos; importación Unity pendiente.
- Test assembly sin referencia directa a runtime: acceso por reflexión y casts corregidos; las cuatro assemblies compilan.
- Pivote cercano al pie invalidaba estimación de cabeza: ID13 proyecta `SpriteRenderer.bounds.max.y`; no altera anclas previas de ID9/10.
- Duración nueva ID13 en Main normalizada a0 consistente con default sin deadline, sin cambiar filas0–12.

## Bloqueo real y próximo paso

Se confirmó el checkout en una consulta inicial a Unity. Después el Editor existente dejó de responder. Se hicieron **tres** reconexiones/refresh acotados: refresh con compilación, selección de instancia y consulta con timeout, y refresh que declaró recuperación pero fue seguido de ping/estado/execute_code fallidos. El último run_tests terminó sin job. Log del Editor observado: `/private/tmp/hcyp-unity-mcp-restart-20261110-3.log`. No se cerró/reinició Unity, no se abrió otro Editor ni se alteró trust/config global.

Recuperar el Editor/MCP (reiniciar sólo con autorización específica), importar los metas finales, ejecutar suites afectadas y revisar las tres Game Views. Comprobar migración/reapertura y recorrido completo, producción, saldo, victoria/derrota y tutorial preservado. Si hay fallos, corregir sólo el criterio afectado y repetir su comprobación. **No entregar como nivel plenamente validado hasta completar esa evidencia.**
