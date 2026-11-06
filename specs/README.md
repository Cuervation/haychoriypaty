# SDD — índice de lectura progresiva

`AGENTS.md` contiene reglas permanentes. Para retomar, leer [current-state.md](current-state.md); para una tarea concreta, abrir **solo su contrato**. Consultar historiales únicamente por fecha/tema y necesidad concreta.

| Necesidad | Documento vigente |
|---|---|
| Estado confirmado, versión, pendientes y entorno | [current-state.md](current-state.md) |
| Límite del producto y siete IDs | [product.md](product.md) |
| Resumen del loop actual | [gameplay.md](gameplay.md) |
| Balance operativo actual de niveles: catálogos, precios, metas/tiempos, caja y plantel | [BALANCE_ACTUAL_NIVELES.md](../docs/BALANCE_ACTUAL_NIVELES.md) (fuente consolidada) + [street-automation.md](features/street-automation.md) (contrato de mecánicas y economía; tablas fechadas antiguas son históricas) |
| Cocineros activos 2 s / CookWorkRate, Mozo, seis cards y ampliación desde Racing | [active-cooks-mozos.md](features/active-cooks-mozos.md) (incluye restauración posterior del escenario) |
| Cocina modular, stock físico, geometría y proyección | [modular-kitchen.md](features/modular-kitchen.md) (modo base niveles 1–5; Racing+ aplica el delta anterior) |
| Mercado/Heladera persistentes y compras sin crédito | [market-fridge.md](features/market-fridge.md) |
| Huracán / Parque Patricios Bonus Track (campaña, cartas y persistencia) | [huracan-bonus-track.md](features/huracan-bonus-track.md) |
| Argentinos Juniors / La Paternal (último nivel de campaña, murales y progresión) | [argentinos-juniors.md](features/argentinos-juniors.md) |
| Tower Defense — Camioneros / Plaza de Mayo (posición 11 de campaña, ID estable 9; aceptación pendiente) | [tower-defense.md](features/tower-defense.md) |
| Firebase/Google obligatorio, perfiles UID, outbox y estado de integración | [firebase-player.md](features/firebase-player.md) (implementación local, aceptación real pendiente) |
| Workers, gait y agarre físico | [worker-animation.md](features/worker-animation.md) |
| Arte/UI y composición | [visual-reference.md](visual-reference.md) (solo si se modifica presentación) |
| Portada y hub de día de partido | [home-hub.md](features/home-hub.md) |
| Límites de código/render/persistencia | [architecture.md](architecture.md) |
| APK/instalaciones y comprobaciones de entrega | [android-prototype.md](features/android-prototype.md) (consultar encabezado reciente) |
| Evidencia histórica de avances y pruebas | [status.md](status.md) (buscar fecha/sistema) |
| Decisiones históricas y reemplazos | [decisions.md](decisions.md) (buscar tema/fecha) |
| Primer prototipo, no runtime actual | [floresta-prototype.md](features/floresta-prototype.md) |
| Continuidad histórica completa hasta 2026-10-25 (no vigente) | [history/continuity-log-through-20261025.md](history/continuity-log-through-20261025.md) |
| Checkpoint anterior completo, solo antecedentes | [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md) |

Los contratos recientes de cocineros/Mercado/modular reemplazan sus reglas antiguas, no toda la funcionalidad de otros documentos. Frente a una discrepancia, contrastar código y serialización actuales antes de editar documentación; no modificar el juego para ajustarlo a un registro histórico. Pendientes no son órdenes de implementación.
