# SDD — índice de lectura progresiva

`AGENTS.md` contiene reglas permanentes. Para retomar, leer [continuity.md](continuity.md); para una tarea concreta, abrir **solo su contrato**. No cargar automáticamente los historiales completos.

| Necesidad | Documento vigente |
|---|---|
| Estado confirmado, versión, pendientes y entorno | [continuity.md](continuity.md) |
| Límite del producto y siete IDs | [product.md](product.md) |
| Resumen del loop actual | [gameplay.md](gameplay.md) |
| Catálogos, precios, metas/tiempos y contratación/velocidad | [street-automation.md](features/street-automation.md#current-per-product-level-balance--2026-10-08) (tablas vigentes; resto contiene evolución histórica) |
| Cocineros activos 2 / WorkRate s, Mozo, seis cards y ampliación desde Racing | [active-cooks-mozos.md](features/active-cooks-mozos.md) (incluye restauración posterior del escenario) |
| Cocina modular, stock físico, geometría y proyección | [modular-kitchen.md](features/modular-kitchen.md) (modo base niveles 1–5; Racing+ aplica el delta anterior) |
| Mercado/Heladera persistentes y compras sin crédito | [market-fridge.md](features/market-fridge.md) |
| Firebase/Google obligatorio, perfiles UID, outbox y estado de integración | [firebase-player.md](features/firebase-player.md) (implementación local, aceptación real pendiente) |
| Workers, gait y agarre físico | [worker-animation.md](features/worker-animation.md) |
| Arte/UI y composición | [visual-reference.md](visual-reference.md) (solo si se modifica presentación) |
| Límites de código/render/persistencia | [architecture.md](architecture.md) |
| APK/instalaciones y comprobaciones de entrega | [android-prototype.md](features/android-prototype.md) (consultar encabezado reciente) |
| Evidencia histórica de avances y pruebas | [status.md](status.md) (buscar fecha/sistema) |
| Decisiones históricas y reemplazos | [decisions.md](decisions.md) (buscar tema/fecha) |
| Primer prototipo, no runtime actual | [floresta-prototype.md](features/floresta-prototype.md) |
| Checkpoint anterior completo, solo antecedentes | [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md) |

Los contratos recientes de cocineros/Mercado/modular reemplazan sus reglas antiguas, no toda la funcionalidad de otros documentos. Frente a una discrepancia, contrastar código y serialización actuales antes de editar documentación; no modificar el juego para ajustarlo a un registro histórico. Pendientes no son órdenes de implementación.
