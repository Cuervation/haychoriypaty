# Plan de optimización y limpieza — HayChoriYPaty

Contexto para cualquier chat o agente nuevo. Proyecto Unity 6000.6.3f1 (Android), repo `Cuervation/haychoriypaty`, rama de trabajo `chore/tests-fix` (PR #1). Reglas operativas en `AGENTS.md`.

Regla del usuario: los tests de Unity los corre Claude (EditMode y PlayMode, vía Test Runner en la Mac); no se le dejan al usuario.

## Estado por fase
| Fase | Qué | Estado |
|---|---|---|
| 1 | Quitar plantilla sin uso y capturas sueltas (commit 7c56a7d) | Hecha |
| 2 | Quitar 14 texturas de Resources superseded, ~36 MB (1759a08) | Hecha |
| 2b | Quitar FanWardrobe, ChicagoWardrobe y 5 texturas, ~32 MB (6e383f8) | Hecha |
| Tests | EditMode en verde; PlayMode: 137 OK, 72 con `[Ignore]` y motivo, 0 fallos | Hecha (pendientes en `Docs/tests-playmode-pendientes.md`) |
| 3 | Quitar código de prototipo sin uso | Pendiente |
| 5, 6 | A definir con el usuario | Pendiente |
| README | Actualizar 13 → 15 posiciones de selector | Pendiente |

## Hechos útiles
- Selector: 15 posiciones, orden de campaña `{0,1,9,2,12,3,11,10,13,14,4,5,6,7,8}`; los IDs de Tower Defense son 9, 10, 13 y 14. No confundir posición con ID estable.
- Guardado versión 9; `TryLoad` exige `unlockedLevelIds` de largo 15 con `[0]` true.
- `StreetView.cs` no fue tocado por la limpieza.

## Cómo se trabaja
- Unity se maneja por computer-use en el Test Runner; la pantalla bloqueada del Mac congela Unity (no repinta el Test Runner). Conviene desactivar el bloqueo durante corridas largas.
- Los archivos se pasan al Mac con `device_commit_files`; verificar con md5 que llegaron (el primer envío tras editar a veces llega con retraso).
- Tras editar: `Assets > Refresh`, esperar la recompilación y recién ahí lanzar tests.

## Fase 3 — criterio propuesto
1. Listar clases/archivos de prototipo candidatos y verificar referencias (código, escenas, Resources, tests).
2. Borrar solo lo sin referencias; compilar; EditMode completo.
3. Un commit por bloque, en rama aparte, con PR.
