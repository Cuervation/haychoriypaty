# Arquitectura técnica vigente

Consultar [README.md](README.md) para el contrato de cada tarea y [continuity.md](continuity.md) para la referencia de versión. Este archivo describe el runtime actual; decisiones anteriores permanecen en [decisions.md](decisions.md), no constituyen una arquitectura alternativa.

## Runtime y responsabilidades

- `StreetGame` (MonoBehaviour): lifecycle, configuración `StreetBalance`, comandos, progresión y payload JSON v4 con Mercado, activado por repositorio de cuenta; PlayerPrefs original queda como legado protegido. Normaliza campos legacy; no restaura equipos comprados ni partidas en vuelo.
- `StreetSimulation` (C#): clientes/colas FIFO, paciencia, workers, rutas, reservas/ownership, unidades, handoff, ingresos, cuotas y resultados. Siete columnas y hasta21clientes. Subpasos limitados/catch-up acotado; contratación sin tope de gameplay, velocidad sí limitada.
- `StreetKitchenProduction`: IDs físicos y estados/reservas/capacidades. Lo avanza la simulación; producción/render nunca acreditan ventas por sí solos. `StreetMarket` enlaza recetas/ingredientes mediante `IStreetKitchenStock`.
- `StreetView`: dibujo IMGUI y un catálogo compartido de acciones/hit-test. Canvas seguro de ancho540 y altura adaptable para interacción; escena/HUD decorativos pueden llenar pantalla física con su transformación separada. El puente Input System es para editor/desktop; Android GameActivity no debe duplicar el despacho nativo de touch IMGUI.
- `StreetSceneLayout`: proyección de fondo raíz, mostrador, crowd y servicio compartidos. `ClubVisualTheme` define mural/ambiente e identidad de fans, no cambia perspectiva/alturas de gameplay.
- `StreetKitchenLayout`: única geometría activa de muebles/anchors/obstáculos/rutas. Ajuste modular uniforme760→540; pies36×12 y proporciones de worker90×98. Grafo de visibilidad sobre bases sólidas expandidas: ruta directa/diagonal si es segura, cada tramo desde la posición real. No NavMesh ni colisión worker-vs-worker.
- `StreetKitchenRenderer` y proyección/animación de workers: prefabs/objetos SpriteRenderer independientes renderizados en capas transparentes en vivo, compuestas por IMGUI. Carga/palma/profundidad siguen el ID real; nada de producto duplicado horneado. Texturas/sprites derivados cacheados y liberados al desactivar. `StreetKitchenPrefabBuilder` editor conserva GUIDs al regenerar sprites alpha-trimmed; no requiere Sprite Editor nuevo.

Main usa una simulación, una escena y los paquetes URP/Input System existentes. Mantener desactivado `GameController`/`PrototypeView`, que son el prototipo histórico con tests/escena/GUIDs conservados. Firebase/Auth Google es la extensión autorizada de persistencia; ver [contrato y estado](features/firebase-player.md). Sin framework de DI, uGUI paralelo ni dependencias nuevas por conveniencia.

## Cinco roles y responsabilidad de productos

`StreetSpecialties` es la autoridad de producto→producción/entrega; IDs estables:

| Productos | Producción / estación | Entrega niveles1–5 | Entrega Racing+ |
|---|---|---|---|
| Chori0, Paty1 | Parrillero0 / parrilla y mesa normal | Parrillero | Mozo4 |
| Bondiola2, Vacío3 | Premium2 / parrilla y mesa Premium | Premium | Mozo4 |
| Coca4, Cerveza6 | Cocacolero1 / barriles existentes | Cocacolero | Cocacolero |
| Fernet5 | Fernetero3 / mesa existente | Fernetero | Fernetero |

El catálogo habilita los cuatro productores; Mozo además requiere `UsesActiveCooks` (LevelIndex>=5). `GetProductionRole` no cambia IDs; `GetDeliveryRole` aplica el gate de entrega. Líneas de un ticket pueden correr en paralelo entre especialidades; ownership/reservas impiden doble entrega. Solo consume/cobra el handoff físico válido; abandono/cancelación libera reclamos.

Desde Racing cada cocinero reserva grill, ID de producto y lugar de mesa; viaja, cocina0,5/WorkRate, transporta y deposita la misma unidad. Mozo reserva stock real antes de ir a buscarlo y servir. No hay timer autónomo de sándwiches en este modo. Niveles1–5 conservan el ciclo base. Detalle y límites de stock: [active-cooks-mozos.md](features/active-cooks-mozos.md), [modular-kitchen.md](features/modular-kitchen.md).

## Contratación, velocidad y reinicio

`StreetBalance.levelBalances`/sus defaults configuran cuotas, bases por rol, multiplicador de velocidad y grant de monedas por intento; `levelProductIds` define disponibilidad. `HireCostForRole` es la autoridad de costo actual: ×1/×2,5/×5, luego n(n+1)/2−1, redondeado arriba a $5. No hay MAX de empleados por gameplay; `maxStaff`/caps y arrays antiguos son compatibilidad, no deben reactivarse. La saturación numérica no es un cap de diseño.

Velocidad: nueve costos base5/10/15/20/30/45/65/90/125, multiplicador del nivel y redondeo a $5; +0,10 hasta×1,90. Toda nueva construcción/carga/selección/inicio/reintento reinicia a un Parrillero normal, cero otros roles/Mozo y SpeedLevel0; StartRound fija las monedas iniciales del nivel. No restaurar compras de plantilla/velocidad de saves antiguos. Tablas vigentes: [street-automation.md](features/street-automation.md#current-per-product-level-balance--2026-10-08).

## UI, geometría y arte

- Botones comunes: `StreetView.DrawStandardButton`, textura azul compartida, Luckiest Guy y input existente. Cards madera/hierro y selector mural son excepciones documentadas; no crear estilos/paralelos sin pedido.
- Acciones derivadas de catálogo; desde Racing seis cards: Velocidad/Normal/Premium y Coca/Fernet/Mozo. Retratos/flechas centrados proporcionalmente en óvalos; estado de velocidad elevado respecto al precio.
- Geometría base: cocina modular, no tamaños fijos pre-modulares. Racing+ amplía piso/circulación, baja parrillas y quita banderines inferiores; conserva muebles originales y fila de Fernet. Composición de calle/mural y eliminación de mesa lateral no aprobada según restauración en [active-cooks-mozos.md](features/active-cooks-mozos.md#restauración-visual-solicitada--2026-10-15).
- `StreetWorkstationLayout` conserva APIs/geometría histórica para compatibilidad/tests, no dirige rutas activas. No usar sus282×94/196×98 como regla universal contra el layout modular.
- `ClubFanDefinition` mantiene variantes Front/Walking/Riot por customerId y dos poses reales de palo; compensa alpha/foot padding sin deformar cuerpos. `ClubFanContractTests` cubre estructura; trifulca genérica es fallback, no arte final de un club publicado. Ver [visual-reference.md](visual-reference.md).

## Persistencia y Mercado

Save v4 conserva desbloqueo de niveles, saldo/stock de Mercado, IDs de compra/intento, watermarks/resultados y campos legacy compatibles. No hay resume de ronda en vuelo: el snapshot recupera abandono/derrota. No confundir saldo persistente de Mercado con monedas y mejoras locales del intento.

Compras atómicas con saldo suficiente, sin deuda/crédito. Ingredientes se reservan por ID físico y se consumen una vez en entrega. Victoria conserva sobrantes; derrota restaura snapshot; cada victoria real cobra una vez por intento (no por nivel); liquidación/recompensas/reparaciones son idempotentes. La regla50%/bonus/reposición exacta está exclusivamente en [market-fridge.md](features/market-fridge.md); no es5% ni plantilla permanente. Una simulación sin proveedor de stock conserva compatibilidad ilimitada de fixtures, no describe inventario de campaña.

## Carpetas y validación

`Assets/Scenes` contiene Main; `Scripts/Core` dominio; `Scripts/UI` presentación; `Scripts/Editor` herramientas; `Resources`/arte/prefabs son recursos originales. EditMode valida dominio/geometría; PlayMode interacción/puente de entrada, usando assemblies de tests que acceden por reflexión sin cambiar GUIDs. Versiones en `ProjectSettings/ProjectVersion.txt`; preservar `.meta`, escenas y paquetes.

Usar el editor ya abierto y las pruebas afectadas según `targeted-testing`; los registros de builds/tests previos no prueban cambios posteriores ni QA físico. Estado de pendientes y evidencia: [continuity.md](continuity.md), [status.md](status.md).
