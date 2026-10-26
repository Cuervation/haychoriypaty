# SDD — índice de lectura progresiva

`AGENTS.md` contiene reglas permanentes. Para retomar, leer [current-state.md](current-state.md); para una tarea concreta, abrir **solo su contrato**. Consultar historiales únicamente por fecha/tema y necesidad concreta.

| Necesidad | Documento vigente |
|---|---|
| Estado confirmado, versión, pendientes y entorno | [current-state.md](current-state.md) |
| Límite del producto y siete IDs | [product.md](product.md) |
| Resumen del loop actual | [gameplay.md](gameplay.md) |
| Catálogos, precios, metas/tiempos y contratación/velocidad | [street-automation.md](features/street-automation.md#current-per-product-level-balance--2026-10-24) (tablas vigentes; resto contiene evolución histórica) |
| Cocineros activos 2 s / CookWorkRate, Mozo, seis cards y ampliación desde Racing | [active-cooks-mozos.md](features/active-cooks-mozos.md) (incluye restauración posterior del escenario) |
| Cocina modular, stock físico, geometría y proyección | [modular-kitchen.md](features/modular-kitchen.md) (modo base niveles 1–5; Racing+ aplica el delta anterior) |
| Mercado/Heladera persistentes y compras sin crédito | [market-fridge.md](features/market-fridge.md) |
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
