# SDD — índice de lectura progresiva

`AGENTS.md` contiene reglas permanentes. Para retomar, leer [current-state.md](current-state.md); para una tarea concreta, abrir **solo su contrato**. Consultar historiales únicamente por fecha/tema y necesidad concreta.

| Necesidad | Documento vigente |
|---|---|
| Estado confirmado, versión, pendientes y entorno | [current-state.md](current-state.md) |
| Límite del producto y siete IDs | [product.md](product.md) |
| Resumen del loop actual | [gameplay.md](gameplay.md) |
| Campaña actual: 15 posiciones visibles (Huracán es nivel normal desde 2026-11-27); no confundir posición e ID | [current-state.md](current-state.md) y [street-automation.md](features/street-automation.md) |
| Única tabla vigente de balance: 10 modos tradicionales y excepciones especiales | [BALANCE_ACTUAL_NIVELES.md](../docs/BALANCE_ACTUAL_NIVELES.md); mecánicas y economía compartida en [street-automation.md](features/street-automation.md) |
| Cocineros activos 2 s / CookWorkRate, Mozo, seis cards y ampliación desde Racing | [active-cooks-mozos.md](features/active-cooks-mozos.md) (incluye restauración posterior del escenario) |
| Cocina modular, stock físico, geometría y proyección | [modular-kitchen.md](features/modular-kitchen.md) (modo base niveles 1–5; Racing+ aplica el delta anterior) |
| Mercado/Heladera, settlement de tickets y gates de stock por modo | [market-fridge.md](features/market-fridge.md) |
| Huracán ID11 / Parque Patricios (posición visible 8; nivel normal desde 2026-11-27, antes Bonus Track) | [huracan-bonus-track.md](features/huracan-bonus-track.md) |
| Argentinos Juniors ID12 / La Paternal (posición visible 5) | [argentinos-juniors.md](features/argentinos-juniors.md) |
| Tower Defense — Camioneros ID9 (posición 3) y Los Redondos ID10 (posición 11; sin cuotas ni deadline) | [tower-defense.md](features/tower-defense.md) |
| La Renga ID13 / Autódromo Bs. As. (posición 7; tercer Tower Defense) | [la-renga.md](features/la-renga.md) |
| Peregrinación a Luján ID14 / Moreno (posición 15; TD horizontal, única valla y construcción libre) | [peregrinacion-lujan.md](features/peregrinacion-lujan.md) |
| Firebase/Google obligatorio, perfiles UID, outbox y estado de integración | [firebase-player.md](features/firebase-player.md) (implementación local, aceptación real pendiente) |
| Workers, gait y agarre físico | [worker-animation.md](features/worker-animation.md) |
| Arte/UI y composición | [visual-reference.md](visual-reference.md) (solo si se modifica presentación) |
| Portada y hub de día de partido | [home-hub.md](features/home-hub.md) |
| Límites de código/render/persistencia | [architecture.md](architecture.md) |
| APK/instalaciones y comprobaciones de entrega | [android-prototype.md](features/android-prototype.md) (consultar encabezado reciente) |
| Agentes/skills compartidos Codex + Claude, trabajo en paralelo y worktrees futuros | [.agents/README.md](../.agents/README.md) (reglas en `AGENTS.md`) |
| Flujo Git/GitHub, commits, validación rápida/Unity/versión y recuperación | [CONTRIBUTING.md](../CONTRIBUTING.md); estado de ramas y migración a `main` (2026-11-17): [GIT_AUDIT_2026-11-17](../docs/reports/GIT_AUDIT_2026-11-17.md) |
| Evidencia histórica de avances y pruebas | [status.md](status.md) (buscar fecha/sistema) |
| Decisiones históricas y reemplazos | [decisions.md](decisions.md) (buscar tema/fecha) |
| Continuidad histórica completa hasta 2026-10-25 (no vigente) | [history/continuity-log-through-20261025.md](history/continuity-log-through-20261025.md) |
| Snapshots anteriores de `current-state.md` (históricos; no usar para reglas) | [hasta 2026-11-13](history/current-state-snapshot-through-20261113.md), [hasta 2026-11-06](history/current-state-snapshot-through-20261106.md) |
| Checkpoint anterior completo, solo antecedentes | [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md) |
| Auditoría documental y contradicciones resueltas (2026-11-06) | [AUDITORIA_DOCUMENTACION_VIGENTE_2026-11-06.md](../docs/reports/AUDITORIA_DOCUMENTACION_VIGENTE_2026-11-06.md) |

Los contratos recientes de cocineros/Mercado/modular reemplazan sus reglas antiguas, no toda la funcionalidad de otros documentos. Frente a una discrepancia, contrastar código y serialización actuales antes de editar documentación; no modificar el juego para ajustarlo a un registro histórico. Pendientes no son órdenes de implementación.

Auditoría de recursos y limpieza conservadora (2026-11-06): [informe](../docs/reports/AUDITORIA_ASSETS_Y_LIMPIEZA.md), [inventario A/B/C/D](../docs/reports/asset-audit-inventory-20261106.csv).
