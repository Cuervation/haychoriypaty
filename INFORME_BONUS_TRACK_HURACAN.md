# Informe — Bonus Track Huracán / Parque Patricios

> **HISTÓRICO — NO ES LA FUENTE VIGENTE.** Informe de implementación del 2026-11-01; conserva el esquema SaveData v5 y el estado de pruebas de aquella fecha. El código actual usa SaveData v6 y el contrato vigente actualiza el orden/slots. Ver [contrato Huracán](specs/features/huracan-bonus-track.md).


**Fecha:** 2026-11-01
**Checkout:** `/Users/celestino/HayChoriYPaty`
**Estado honesto:** funcionalidad integrada en fuente y compilada estáticamente; **no aceptada como terminada**. Faltan ejecución de tests, balance por simulación y revisión/capturas reales de Unity Game View.

## 1. Alcance y archivos

### Nuevos para Huracán

- `Assets/Scripts/Core/StreetBonusTrack.cs` (+ `.meta`): cartas, progresión temporal, estado de partida, scoring y perfil persistente.
- `Assets/Scripts/UI/StreetView.BonusTrack.cs` (+ `.meta`): HUD, intro, selección/Super Bonus, Aguante y panel de resultados.
- `Assets/Art/Street/Resources/street-mural-huracan-master-v1.png` (+ `.meta`): mural nuevo.
- `Assets/Tests/EditMode/StreetBonusTrackTests.cs` (+ `.meta`): cobertura focalizada, con reflexión para respetar el límite entre assemblies.
- `specs/features/huracan-bonus-track.md`: contrato y estado de aceptación.

### Modificados en el alcance

- `Assets/Scripts/Core/StreetSimulation.cs`: ID/campaña, configuración, multiplicadores, elección temporal, trabajadores gratuitos, handoffs y fin de ronda.
- `Assets/Scripts/Core/StreetGame.cs`: guardado v5, migración, desbloqueos, resultados, tickets, récord/reintento/abandono.
- `Assets/Scripts/Core/StreetPlayerStatistics.cs`: arrays de 12 niveles y campos del resultado.
- `Assets/Scripts/Cloud/PlayerLocalRepository.cs`, `firebase/firestore.rules`: v5/campos Bonus y límites de levelId.
- `Assets/Scripts/UI/ClubVisualTheme.cs`, `StreetView.cs`, `StreetView.LevelSelectorPresentation.cs`: mural, paleta, tarjeta de campaña y entrada a la UI especial.
- `specs/README.md`, `specs/current-state.md`, `specs/status.md`: índice y estado/documentación.

El checkout ya contenía cambios locales no relacionados (incluidas escenas, recursos, pruebas, settings y auditorías). Se preservaron; no se limpió ni sincronizó la carpeta de contexto `Documents/ChatGPT/Hay Chori y Paty`.

## 2. Mural y dimensiones

La adaptación toma como referencia principal “EN EL BARRIO / MANDO YO”: globo bordó a la izquierda, texto bordó grande, pared de ladrillo blanco con desgaste y líneas de pintura. No se pegaron las fotografías. El archivo es PNG RGB **1898×829 px**, igual que `street-mural-ferro-master-v1.png`; se reutilizó su configuración de textura/meta, con GUID propio. La inspección local del asset confirma texto legible y composición completa.

Esto verifica el recurso aislado, **no su integración visual en la escena**. El ajuste de crop, encuadre, continuidad de calle y visibilidad de objetos sigue pendiente de Game View.

## 3. Orden y migración

La interfaz presenta Huracán como **nivel/casillero 6** entre Independiente y Racing, pero el ID de persistencia es **11**. Mantener ID11 evita reinterpretar IDs históricos 5–10 como otros clubes. `CampaignOrder` separa orden visible de identidad estable. Ganar ID4 habilita Huracán; alcanzar al menos una estrella la primera vez habilita Racing; se permite repetir Huracán.

Save v4 migra a v5 sin aumentar el frontier numérico (que sigue acotado 0–10): progreso antiguo `unlockedLevel >= 5` conserva el acceso previo a Racing y habilita Huracán. Nuevos perfiles usan flags independientes. Arrays de nivel se extienden de 11 a 12, copiando los valores existentes en sus mismos índices. La ruta local cloud admite v1–v5 y las reglas Firestore contemplan resultado del ID11; no hubo prueba de Firebase real.

## 4. Arquitectura y configuración

La lógica queda en el modo especial de `StreetSimulation`, no en una segunda simulación. Configuración de fuente: 120 s; 2 Parrilleros + 1 Cocacolero + 1 Mozo; productos Chori/Paty/Coca; sin objetivo convencional, Premium, Fernetero, compra, ingreso o moneda persistente. No se enlaza el inventario de Mercado en Huracán. La cola, espera indefinida, rutas, estaciones y handoffs continúan usando los sistemas compartidos.

Elecciones en tiempo efectivo 20/40/60/80/100; en el tercer evento aparece Super Bonus. El stepping se acota al deadline; con la selección abierta no avanzan ciclos ni trabajadores. Selección de cartas es seeded y ponderada por stock de mesa/cola/trabajadores, intenta cubrir producción, distribución y estrategia; descarta opciones sin efecto. `Todos a Fondo` dura 15 s y `¡Aguante!` dura 8 s. La UI usa tiempo no escalado para permitir interacción mientras el estado del juego está detenido.

## 5. Cartas y límites implementados

| Carta | Efecto fuente | Tope/regla |
|---|---|---|
| Parrilla a Fondo | +30% producción de alimento por stack | 3 stacks |
| Otro Parrillero | añade un Parrillero gratis | no supera cook stations permitidas |
| Coca Express | +35% preparación de bebidas por stack | 3 stacks |
| Otro Cocacolero | añade Cocacolero gratis | máximo 3 del rol |
| Mozo Relámpago | +40% movimiento del Mozo por stack | 3 stacks |
| Otro Mozo | añade Mozo gratis | máximo 3 del rol |
| Todos a Fondo | +25% global temporal | 15 s efectivos |
| Entrega Dorada | siguientes 10 entregas válidas valen 200 puntos | no cuenta producción no entregada |
| Manos Rápidas | −30% del tiempo de recogida por stack | máximo 2 stacks / mínimo x0.4 |
| Refuerzo de Emergencia | el jugador elige rol entre los que aún caben | solo roles válidos |

Las mejoras son del intento; multiplicadores se consultan durante ciclo/ruta/recogida y no editan velocidades guardadas. El máximo se verifica al ofrecer y al aplicar una carta. El resultado no conserva trabajadores bonus al volver a la campaña.

## 6. Aguante, score, estrellas y tickets

- Diez entregas físicas válidas cargan ¡Aguante!; se activa manualmente, aplica +25% de producción/movimiento por 8 s y vuelve a cero al expirar.
- Entrega válida: 100 puntos; Entrega Dorada: 200; completar el pedido una sola vez: +150. Cocinar/stockear no puntúa. La moneda normal queda en cero.
- Los umbrales actuales (3000 / 6500 / 10000) son **provisionales de desarrollo**. No se estimaron ni aceptaron como definitivos por falta de datos empíricos.
- Con ≥1★ se acredita un ticket normal en ese intento. El primer 3★ agrega uno solo por perfil; los posteriores siguen dando solo el ticket repetible. Ticket y record se guardan junto con `attemptId`; el resultado repetido no vuelve a acreditar. Abandono no llama al pago de bonus.
- Se registran mejor score/estrellas/entregas/pedidos, último intento y elecciones. Los campos del intento se incluyen en la ficha de partida para persistencia cloud.

## 7. Simulaciones y balance — **NOT_RUN**

No se ejecutaron corridas de referencia, 20 semillas por estrategia ni auditoría de producción real. Por lo tanto **no hay resultados observados** para baseline, prioridad parrilleros, Coca, Mozos, combinación equilibrada, aleatoria, Aguante temprano/estratégico, plantilla máxima o topes. Producción por rol, entregas, pedidos, cola/mesa, tiempos ociosos, puntaje y tickets no tienen una medición empírica en este informe. No se ajustaron valores a partir de resultados ficticios. Se deben recoger al menos 20 corridas reproducibles de cada estrategia y usar esos datos antes de aprobar estrellas/balance.

## 8. Errores y correcciones de implementación

- Durante la compilación, el contador/constante `ChoiceCount` colisionaba; se renombró la constante de límite como `MaximumChoices` y se actualizaron sus consumidores.
- Se agregó la propiedad `BonusTrackProgress` que requería la UI de resultados/perfil.
- La primera compilación del test detectó overloads NUnit con tipos boxed; los asserts se tiparon explícitamente. La compilación final de tests terminó sin error.
- No se detectaron errores C# en la compilación Roslyn focalizada. No se puede descartar error de import/runtime Unity sin Editor funcional.

## 9. Pruebas automáticas y estáticas

### Añadidas (fuente; **runtime NOT_RUN**)

1. Orden y continuidad de IDs/campaña.
2. Migración v4 con acceso previo a Racing.
3. Plantilla inicial y cinco deadlines exactos con pausa.
4. Carga/expiración de Aguante.
5. Puntos solo por handoffs reales, sin dinero.
6. Recompensa repetible, extra primer 3★ e idempotencia de resultado.

### Ejecutadas

- **PASS — compilación estática del runtime** con Roslyn incluido en Unity 6000.6.3f1, response files actuales; salida en `/private/tmp`. 0 errores; avisos de código no alcanzable / `DEVELOPMENT_BUILD` obsoleto en StreetView.
- **PASS — compilación estática del assembly EditMode**, incluyendo el test nuevo; 0 errores. Un warning de `AppDomain.GetAssemblies()` en `StreetSceneBuildGuardTests` preexistente.
- **PASS — `git diff --check` focalizado** en fuentes/reglas de Huracán y whitespace de nuevos fuentes/tests.
- **NOT_RUN — Unity EditMode/PlayMode**: la compilación de tests no los ejecuta.
- **NOT_RUN — reglas con emulador Firebase, APK o dispositivo**.

## 10. Unity Game View, resoluciones, rendimiento

Game View y todas las capturas solicitadas (selector, inicio, 20 s, Super Bonus, Aguante activo, fin y resultados) **BLOCKED / no generadas**. No hay imágenes dentro del proyecto que se presenten como capturas de juego. Unity MCP primero dejó de responder durante la recuperación solicitada; tras tres intentos acotados, se evitó abrir un segundo Editor, reiniciar Unity o tocar su configuración. El material disponible solo permite compilación estática; no es sustituto de la escena abierta.

No se midió FPS, GC, memoria ni perfiles de dispositivo. La revisión de performance queda NOT_RUN; no se atribuye mejora de rendimiento a la compilación.

## Artefacto Android generado — 2026-11-01 (sin aceptación de runtime)

A pedido del usuario se generó una APK Development Android IL2CPP/ARM64, Main-only, desde el Editor Unity 6000.6.3f1 activo. Job build-e2f77bf4d8: Succeeded, 357.18 s; 0 errores/5 advertencias. Artefacto ignorado por Git Builds/Android/Archive/HayChoriYPaty-0.2.81-code83-huracan-20261101.apk, 411,941,423 bytes, SHA-256 f3b4b1d2b3f46834b9c6423beb6cbe978c63db4e843780ec5eeb06695128899b.

PASS: integridad ZIP, alineación zipalign -c 4, firma Android Debug APK v2, paquete com.haychoriypaty.game, versionName 0.2.81/code83, min API26/target36, ABI arm64-v8a, debuggable=true y metadata IL2CPP con StreetBonusTrackState, StreetBonusTrackProfile y HuracanBonusLevelId. Firebase Analytics y recolección Ad-ID figuran desactivadas en el manifest; permisos AD-ID de dependencias permanecen. No está firmado para Play Store.

No equivale a aceptación: no se instaló, abrió, ni probaron ronda, UI Game View, Firebase real o balance en dispositivo. Sigue vigente que los tests nuevos compilaron pero no se ejecutaron y que 3000/6500/10000 son umbrales provisionales. La Development APK incluye el bono QA de +30 tickets; quitarlo antes de release. El APK no se versiona.

## 11. Pendiente obligatorio para aceptar

1. Volver a una sesión del Editor ya conectado y comprobar Main/selector y partida Huracán a 540×960, 540×1170/1200, 1080×1920 y otras proporciones relevantes.
2. Ejecutar los seis tests nuevos y tests focales de navegación/campaña/migración; reparar cualquier fallo observado.
3. Instrumentar y ejecutar las diez estrategias descritas en la solicitud, al menos 20 corridas seeded por estrategia; medir cuellos de botella y recalibrar las estrellas según entregas alcanzables.
4. Guardar y revisar capturas reales de todos los estados requeridos y corroborar recorrido, worker spacing, mural/crop y pause/resume.
5. Verificar Firestore y persistencia cloud cuando el servicio/emulador esté disponible; medir rendimiento si se habilita.

**Conclusión:** la estructura principal y la lógica de juego están en fuente y compilan estáticamente. La entrega no satisface todavía el criterio de aceptación de “completamente integrada, probada y validada visualmente”; no usar el umbral de estrellas actual como balance final.
