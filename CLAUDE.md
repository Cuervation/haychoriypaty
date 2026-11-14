# CLAUDE.md — Hay Chori y Paty

Las reglas operativas completas están en [AGENTS.md](AGENTS.md) y el índice de contratos en [specs/README.md](specs/README.md). Este archivo solo resume lo que Claude necesita al arrancar.

## Proyecto
- Juego Unity 6000.6.3f1 para Android: gestión de puestos callejeros de estadios. Repo `Cuervation/haychoriypaty`.
- Selector de 15 posiciones, orden de campaña `{0,1,9,2,12,3,11,10,13,14,4,5,6,7,8}`; los IDs de Tower Defense son 9, 10, 13 y 14. No confundir posición con ID estable. Guardado v9.
- Balance vigente: [docs/BALANCE_ACTUAL_NIVELES.md](docs/BALANCE_ACTUAL_NIVELES.md).

## Plan y estado
- Plan de optimización y estado por fase: [Docs/plan-optimizacion.md](Docs/plan-optimizacion.md). Leerlo antes de seguir con la limpieza (Fase 3 pendiente).
- Tests PlayMode pendientes de rediseño: [Docs/tests-playmode-pendientes.md](Docs/tests-playmode-pendientes.md).
- Rama de trabajo actual: `chore/tests-fix` (PR #1).

## Reglas del usuario
- Los tests de Unity (EditMode y PlayMode) los corre Claude, siempre; no se le dejan al usuario.
- Claude es autónomo: si se pierde la conexión con la Mac, se reconecta y lo resuelve.
- Responder en español (Argentina).
- No ocultar problemas: un test con `[Ignore]` lleva siempre el motivo y se documenta.

## Trabajo con Unity
- Tras editar scripts: `Assets > Refresh`, esperar la recompilación y recién ahí lanzar tests.
- Si la pantalla del Mac se bloquea, Unity se congela; avisar al usuario.
- Revisar Console/popups antes y después de cada interacción con Unity (ver AGENTS.md).

## Memoria
- Engram (si está instalado): al empezar, buscar el contexto de HayChoriYPaty; al terminar, guardar decisiones y estado.
- Documento del Project en claude.ai: `claude/plan-optimizacion.md`.
