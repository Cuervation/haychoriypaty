## 2026-10-20 — Barra y expiración por paciencia eliminadas

Se quitó el indicador verde del render y toda la cuenta regresiva individual en `StreetSimulation`; los hinchas ya no abandonan la cola por esperar y conservan pedidos/reservas hasta una salida normal. Se eliminaron los parámetros y datos serializados de paciencia de `StreetBalance`/Main; se preservó `CustomersLost` como estadística legacy que devuelve cero. Unity MCP respondió tras telemetry ping + refresh de scripts; compilación final limpia (consola: 0 errores/advertencias). 6/6 pruebas EditMode focalizadas pasaron (`507b4867f73b48d7a450818d86702f57`), cubriendo espera larga, reservas, ocupación FIFO y especialidades. No se capturó Game View en esa validación. Luego se publicó el commit `4e8af36` y se generó APK 0.2.53/code55 desde ese commit: 355.234.955 bytes, SHA-256 `9c4ff8d133a2f48fe05eab73c2424d8926236f035bb1988ab022d645d7fd1f65`; metadata, ZIP, alineación y firma v2 verificadas. No se instaló en dispositivo. Detalles en [android-prototype](features/android-prototype.md).

## 2026-10-20 — Anillo de progreso sobre la cabeza del Parrillero

El centro del anillo ahora se calcula desde la altura visible del sprite y su escala vertical, dejando 4 puntos entre la cabeza y el borde exterior; tamaño, progreso y apilado quedan igual. Unity 6000.6.3f1: 3/3 EditMode (escalas verticales .8/1/1.25) y 1/1 PlayMode con captura Game View real a 1080×1920. Sin cambio a movimiento, cocción ni inventario.

## 2026-10-19 — Parrilleros +6% de velocidad por mejora

CookWorkRate ahora escala como `1 + SpeedLevel × 0.06`: ×1.00 inicial, ×1.06/×1.12 en las dos primeras compras y ×1.54 al máximo nueve. Normal y Premium comparten la tasa en movimiento y cocción; demás trabajadores conservan +10% por compra. Precios, límite y mejoras restantes sin cambios. Unity 6000.6.3f1: compilation completed, console reported 0 errors/warnings, and 4/4 focused EditMode tests passed (rate milestones/roles, automatic and active cooking, opposing-worker movement). Reconnected MCP by selecting the active Unity instance and refreshing scripts. No broad suite or APK.

## 2026-10-19 — Alternancia fija de Parrilleros

Racing+: el Parrillero sigue un turno global Chori→Paty y el Premium Bondiola→Vacío. La receta se asigna una sola vez por turno, independiente de metas, pedidos, stock o cantidad de trabajadores. Si no se puede reservar producción real (incluida meta completa), termina esa misma receta como ciclo visual; no consume stock ni crea unidades/ventas/metas y continúa la alternancia en el límite siguiente. Niveles1–5 siguen automáticos.

**Validación real Unity 6000.6.3f1:** 12/12 EditMode focalizados; 2/2 PlayMode de captura: 1–4 cocineros real/transporte, más ciclos visuales sin stock de productos0–3 y cuatro cocineros simultáneos. Game View 1080×1920 inspeccionado; arte ambiental de parrilla sigue lleno, mientras el producto de cada turno se verificó en estado de simulación (sin agregar etiquetas ni sprites). Evidencia: `Logs/Acceptance/FixedCookAlternation-20261019/`. MCP se recuperó con telemetry ping tras un `ping not answered`; compilación limpia, consola 0 errores; escena temporal, partida y PlayerPrefs persistente intactos.

**Balance observado, no modificado:** el audit de campaña desde save nuevo ganó1–5 y perdió nivel6 en las variantes de policy factor1.2,1.5 y2.0. Al mejor caso de esas tres le faltaron 89 Choris,24 Patys,11 Bondiolas,8 Vacíos,35 Coca,3 Fernets y16 cervezas dentro de255s. Esto documenta un riesgo para Racing bajo la política automática auditada, no una prueba de imposibilidad universal; niveles7–11 no se recorrieron en una campaña fresca. No se cambiaron tiempos, metas, economía ni precios.

Jobs: EditMode `ab57e76949a447efae965beaa44ab5df` 12/12; PlayMode `67abf30458f84ba68de4a3c035dd540d` 1/1 y `b5e9cec8d59f4978863bf5ce6f60b135` 1/1. El fixture amplio previo a dos casos agregados fue60/62: solo repitió los dos fallos previamente conocidos de vuelta al punto seguro (Parrillero nivel0 y Mozo nivel5); no se reejecutó completo. Sin APK, Git commit/push ni instalación.

## 2026-10-19 — Fernet-table bottle sizing

Reduced the display-only Coca-Cola bottles on the Fernet table by a further 5% (`.76` → `.722`) and Fernet bottles by 7% (`1.00` → `.93`). Their visible bases stay anchored to the same tabletop planes; bottle count, spacing logic, gameplay stock, and other Coca renders are unchanged. Focused EditMode/PlayMode renderer checks now assert the exact sizes.

Follow-up: reduced only the Fernet tabletop display bottles by another 10% multiplicatively (`.93` → `.837`); Coca display bottles remain `.722`. Layout, tabletop contact, bottle count and gameplay stock are unchanged. Updated the focused EditMode/PlayMode exact-size assertions. Unity MCP reconnected via telemetry ping and script refresh, but the focused test runner timed out before starting; no visual capture was taken.

## 2026-10-19 — APK 0.2.49/code51

Development IL2CPP/ARM64 APK built from pushed commit `2c17fb9` with Unity 6000.6.3f1 in an isolated copy. Build succeeded (0 errors/1 non-blocking clang warning); package/version/ABI, ZIP integrity, alignment and v2 signature verified. APK: `Builds/Android/HayChoriYPaty-0.2.49.apk`, 346,733,372 bytes, SHA-256 `9bc5f44d0933edc268e17c8bf6f1cb62a6a49a19222ae9cceb126b6acf6e46ea`. Debug-signed, not installed; no new gameplay tests or full Game View QA during packaging.

## 2026-10-19 — Parrillero visual contact shared across render paths

The source image looked unchanged because the fitted-grill render offset was applied only by the native worker projection; the IMGUI fallback still rendered at the safe logical post. `StreetWorkerProjection.PresentationPosition` now owns the render-only 30%-depth contact anchor, used by both paths. Simulation posts, obstacle clearance, routes, cooking, and slot alignment remain unchanged.

**Validación:** Unity 6000.6.3f1: 4/4 EditMode cases (Racing/River geometry and native/fallback shared anchor) plus 1/1 focused PlayMode case passed. Actual native kitchen-camera captures were inspected at 1080×1920 and 1080×2400, including four cooks; this is not a full Game View screenshot. No APK, Git commit, or push.

## 2026-10-18 — APK 0.2.48/code50 y push
Build Android Development Main-only IL2CPP/ARM64 desde copia aislada de Unity 6000.6.3f1: 446.11 s, 0 errores/1 warning. APK 348,924,731 bytes; SHA-256 e06e52d1fb3b433d33ad08834633bd5b02ef82e7e955de8c2f4fff91b3000e29. aapt, ZIP, zipalign y firma APK v2 verificados. Se incluye la restauración de rutas directas de trabajadores y cambios actuales de cocina, con ETC2 Android-only; debug-signed, no instalada. Unity MCP no respondió; el Editor activo del checkout quedó intacto. Los tests focalizados previos dieron 10/10 y no se repitieron en la entrega. Sin Game View ni QA en teléfono.

## 2026-10-18 — Restaurado movimiento directo de trabajadores

Comparación puntual de `349a7b1`→`4b9df91`: se retiró de `MoveWorker` la anticipación de colisiones, radio 48, derecho de paso por ID, desvíos laterales y reducción temporal al 35%. El avance vuelve a ser directo por waypoint y conserva `WorkRateFor`. Se mantienen rutas alrededor de muebles fijos (`FindShortestSafeRoute`), anclas/esperas distintas, selección de handoff y rotación/cola de Parrilleros; no se restauraron commits ni archivos completos.

**Validación:** Unity 6000.6.3f1 compiló en copia aislada; 10/10 pruebas focalizadas pasaron (cruces opuestos y con worker idle, tres Mozos, velocidades por rol, transporte/deposito, handoffs mixtos y ruta con obstáculo fijo). Fixture completo StreetActiveCooksTests: 52/54; fallan las dos variantes ya fallidas de retorno al punto seguro (Parrillero nivel 0, Mozo nivel 5). Unity MCP `127.0.0.1:8080` no respondía; puerto sin listener y log/lock del Editor estaban fechados 16/10; se dejó el Editor del usuario intacto. Sin captura Game View, APK ni Git.

## 2026-10-18 — APK 0.2.47/code49 y reducción de tamaño

141 PNGs de `Resources` ahora usan ETC2_RGBA8 en Android; 87 antes estaban fijadas a RGBA32 y 54 usaban ajustes automáticos/sin override. Solo cambia la importación Android, no el arte fuente, otras plataformas ni gameplay. Unity 6000.6.3f1 batch Main-only Development IL2CPP/ARM64 compiló en 38.84 s, 0 errores/1 warning. APK final **347,748,382 bytes**, 74,522,681 bytes (17.65%) menor que0.2.46; se realineó y re-firmó con el certificado debug habitual. `aapt`, integridad ZIP, `zipalign` y `apksigner` pasaron. Unity MCP estaba caído; el batch aislado funcionó tras habilitar IPC local. Sin tests de gameplay, Game View ni instalación en el teléfono. Ver [android-prototype.md](features/android-prototype.md).

## 2026-10-18 — APK 0.2.46/code48 y validación focalizada

En una copia aislada de Unity 6000.6.3f1 se compilaron los tests del proyecto y pasaron **17/17 EditMode**: dos filas de sándwiches, puntos de espera/acceso distintos, rotación de cuatro parrilleros por parrilla y entrega mixta de los siete productos. El caso mixto destapó un handoff de Coca-Cola bloqueado por personal estacionado; el selector ahora elige el punto libre más seguro. La grilla de espera usa seis columnas y filas escalonadas para no repetir anchors cercanos.

La APK Main-only Development IL2CPP/ARM64 se generó correctamente (405,47 s, 0 errores, 1 warning clang) y se verificaron paquete/versión/API/ABI, integridad ZIP y firma v2. Metadatos, tamaño y hash en [android-prototype.md](features/android-prototype.md). Unity MCP siguió intermitente; no se ejecutaron PlayMode, captura Game View ni prueba en dispositivo. APK generada localmente, no instalada.

## 2026-10-18 — Circulación y separación local de trabajadores

Se corrigieron causas de amontonamiento en simulación: anclas repetidas de espera, depósito, recogida y handoff; `MoveWorker` ahora anticipa vecinos cercanos y se desvía lateralmente con rumbo estable, sin mover trabajadores parados ni crear búsquedas de ruta por frame. Los cocineros distribuyen depósitos por cuatro puntos en dos filas; sus cuatro puestos y la espera FIFO se conservan. Se agregaron cuatro regresiones focalizadas (cinco casos) en `StreetActiveCooksTests`.

`git diff --check` pasó. El refresh inicial de Unity MCP devolvió `tests_running`; un reintento luego informó conexión recuperada/listo, pero el ping de estado/consola no respondió y la solicitud de tests se desconectó antes de informar ID o resultado. No se confirman compilación, tests ni capturas. La aceptación visual de cruces reales, muchos Mozos en una mesa, ambos equipos de cinco cocineros, bebidas y vista Racing/San Lorenzo queda pendiente. Sin APK ni cambios a GitHub.

## 2026-10-18 — Cuatro puestos de Parrillero y cola rotativa

La lógica local asigna como máximo cuatro puestos por parrilla, reserva por rol Normal/Premium y rotación FIFO al dejar/recuperar un producto real; los demás usan una zona de espera con bob leve y sin cocinar ni reservar fuera de puesto. Se retiró la reducción de escala por cantidad. Se agregaron regresiones para equipos1/2/3/4/5/6/8, exclusividad, escala, rutas y promoción del quinto. `git diff --check` pasó. Unity 6000.6.3f1 compiló los assemblies runtime y EditMode (Tundra build success); el test job del assembly EditMode se desconectó antes de reportar resultado y `TestResults.xml` no se actualizó, por lo que no se declara ningún test aprobado. Capturas solicitadas de cinco Parrilleros normales y cinco Premium pendientes. No APK, GitHub ni commit/push.

## 2026-10-18 — Producción proporcional y velocidad diferenciada

Racing+ ignora pedidos y stock preparado para priorizar; Parrilleros comparten un weighted virtual-finish por metas, omiten cuotas ya completas y siguen con la receta viable cuando faltan ingredientes o espacio. Los contadores se reinician por intento. Velocidad por mejora: Parrillero/Premium +5% (×1.45 a nueve); Coca/Fernet/Mozo +10% (×1.90). Niveles1–5 conservan producción automática; se separan sus tasas de cocción y bebidas sin tocar capacidades ni economía. Unity6000.6.3f1: `StreetActiveCooksTests` 28/28 EditMode pasó, incluyendo ritmo100:50,80:40 y7:3, concurrencia global, bloqueo/reanudación por capacidad e ingredientes, cuota completa, cero pedidos, stock preparado, bebida independiente y modo automático previo a Racing. Sin APK, cambio de UI, Git commit/push ni ejecución PlayMode.

## 2026-10-18 — Corrección integral de brazos de parrilleros

Causa confirmada en el video/código: el cuerpo original ya dibujaba brazos y el overlay los duplicaba, desplazándose además hacia el slot; el corte de torso podía dejar fragmentos de pinzas. Se reemplazó durante la cocción el personaje dividido por una pose transparente completa, anclada a los pies y sin offset al alimento. Normal/Premium comparten las cuatro fases del atlas con variante de paleta; los estados de salida recuperan el render normal. Los cocineros se dirigen al borde accesible del slot real y se prefiere una separación de 84 puntos; la cola bloqueada por obstáculos se trasladó al área despejada frente a las parrillas. No cambian metas, recetas, tiempos de producción, inventario ni economía.

Unity 6000.6.3f1 en copia aislada: alcance/espaciado 1/1 PlayMode, poses 1–4 cocineros/cuatro fases/dos alturas 1/1 PlayMode, reserva/fallback 1/1 EditMode y marcha/transporte real 1/1 PlayMode. StreetActiveCooksTests quedó 51/53 (fallaron dos retornos a punto seguro: Parrillero nivel 0 y Mozo nivel 5); el fixture completo StreetWorkerProjectionTests quedó 3/5 (reacomodo inmediato de equipos recién contratados y expectativa de ocultamiento idle). Capturas nativas de cámara (no Game View completa) muestran cocineros Normal/Premium en 1080×1920 y 1080×2400. Unity MCP no respondió en `127.0.0.1:8080`; el editor activo no se reinició. No se ejecutó suite global, APK, commit ni push.

## 2026-10-18 — Auditoría y tabla central de escudos

El pie de nivel y el encabezado del Mercado ahora consultan una tabla de recursos única alineada con los 11 niveles. All Boys, Nueva Chicago y Vélez conservan sus imágenes originales; no se agregaron escudos para Ferro, Independiente, Racing, San Lorenzo, River, Boca, el Sindicato de Camioneros ni Los Redondos: las fuentes revisadas no acreditan el permiso comercial requerido (River además exige autorización previa; Independiente somete el arte a aprobación). Los ocho slots quedan `null`, sin inventar marcas. Fuentes y condiciones: [auditoría visual](visual-reference.md). Se agregó una regresión focalizada para tabla/recursos; Unity MCP no respondió, por lo que aún no se ejecutan importación, compilación, pruebas ni captura visual.

## 2026-10-18 — APK 0.2.45/code47

Unity 6000.6.3f1 Main-only Development IL2CPP/ARM64 build-4383fb3aa1 succeeded in 383.53 s (0 errors, 1 warning). Se verificó paquete, versión, API, ABI, integridad ZIP y firma debug. Regresiones focalizadas: globos/footer EditMode 9/9; acceso Google PlayMode 2/2. APK generada, no instalada. Los escudos oficiales nuevos siguen pendientes por permisos de reutilización no verificados. Ver [android-prototype.md](features/android-prototype.md).

## 2026-10-18 — Zócalo inferior de madera

El fondo crema desde el límite inferior del piso hasta el borde de pantalla ahora reutiliza un recorte ilustrado del recurso de madera del puesto, con tintado marrón cálido y una línea superior oscura/acento. El recorte se adapta por cobertura para no deformar la veta; los banderines tradicionales se renderizan encima con fondo transparente. Unity 6000.6.3f1: compilación sin errores y 4/4 pruebas EditMode focalizadas pasaron (transparencia y alturas compacta/normal/alta). No se capturó Game View porque iniciar Main podía acceder al guardado real; no se generó APK ni se hizo commit/push.

## 2026-10-18 — Globos solo en las dos filas delanteras

`StreetView` dibuja globos únicamente en las filas visuales 0–1 usando el orden por posición ya existente. Los personajes y sus barras de paciencia siguen dibujándose en todas las filas; no se modifica la cola ni el estado de pedidos. La regresión EditMode focalizada pasó 5/5. La clase amplia (175 casos) tuvo 6 fallos en expectativas de banderines/footer, ajenos a este cambio. No se generó captura de Racing para evitar iniciar o alterar la partida persistida. Sin APK, commit ni push.

## 2026-10-18 — Legibilidad visual de Parrilleros

Parrilleros normales y Premium quedan en una fila frente a la parrilla, más cerca del borde; al cocinar/esperar miran al frente con un bob leve. La proyección escala equipos densos, asigna sorting de fila estable, no oculta workers solo por solapar sus rectángulos y separa visualmente los idle que comparten mesa. Se baja el ancla del producto transportado a la mano y se contienen los slots de chori/paty/bondiola/vacío en el tablero. Unity 6000.6.3f1 compiló sin errores; 11 EditMode + 14 PlayMode focalizados pasaron. No quedó una captura visual actual del gameplay, por lo que la revisión manual en pantalla sigue abierta. Sin cambios de economía, tiempos o mecánicas.

## 2026-10-17 — Botón Google blanco y entrada al juego

El acceso Google ahora reutiliza las proporciones/cápsula de JUGAR con cara blanca, borde gris sutil, G multicolor y letras negras en la fuente del menú. Al resolver inicio/perfil/conflictos, abre el selector de canchas como hacía JUGAR; no inicia un partido ni saltea la selección/mercado. Se añadieron dos regresiones PlayMode para la navegación inmediata y la espera de la presentación. Unity MCP informó sesión no lista (ping sin respuesta): compilación, pruebas y revisión visual quedan pendientes; no se generó APK ni se instaló.

## 2026-10-17 — APK0.2.44/code46 con botón “Jugar con Google”

- Unity6000.6.3f1 Main-only Development IL2CPP/ARM64: **Succeeded288.21s,0 errores/2 warnings** (símbolos de diagnóstico y aviso genérico de postprocesamiento/cambios de código). APK420,716,301bytes; SHA-256 `dcadb2f7171665c62ec521f6cbf0a543573c62bcdf6bf1495d132a90e49244dc`; firma debug, paquete, code46, ARM64, recursos de login y librerías Firebase comprobados. `StreetMarketViewTests`9/9 PlayMode. Instalada en Motorola Edge 60 Fusion mediante adb install -r, conservando los datos; el juego no se abrió. Login real sigue pendiente. Detalle: [android-prototype.md](features/android-prototype.md).

## 2026-10-16 — Backend Firebase configurado, validación del cliente pendiente

Firebase real configurado: Google habilitado, Android com.haychoriypaty.game con SHA debug, Firestore Standard en São Paulo y reglas seguras desplegadas; Billing=false. Roundtrip administrativo IAM real aprobado, no progreso de jugadores. SDK importado y compila; 46/47 EditMode (falla campaña nivel6) y4/4 PlayMode, gate manual sin sesión verificado. APK0.2.44/code46 IL2CPP/ARM64 generada y verificada; ver encabezado actual de [android-prototype.md](features/android-prototype.md). Sin instalación. Google real del cliente y reapertura/dispositivo pendientes. Detalle: [firebase-player.md](features/firebase-player.md#informe-verificable-de-entrega--2026-10-16).

## 2026-10-16 — Auditoría visual de dibujos del Mercado

- Se corrigió Pan común: el ícono ahora usa el pan largo del sándwich de chori; Pan Paty conserva el pan del sándwich de paty. El cambio es de selección/crop de sprites existentes, sin modificar arte fuente ni catálogo.
- Revisión manual del Mercado en niveles 2, 4 y 10 a 540×960, 540×1170 y 540×1200, incluyendo OTROS INGREDIENTES expandida y desplazada hasta el final: los dibujos de chorizo, ambos panes, paty, bondiola, vacío, Coca, cerveza, Fernet y Coca 2,25 L entran completos en sus tarjetas. `CaptureUnifiedMarketLevelsAtPortraitGameViewSizes`: **1/1 PlayMode passed**.
- Unity MCP se reconectó al Editor del proyecto para esta captura. Esto no valida visualmente la separación/posición de Parrilleros; esa revisión sigue pendiente. No se generó APK ni se instaló en el teléfono.

## 2026-10-16 — Parrilleros ocupan puestos separados al cocinar

- Las posiciones de trabajo ya no derivan del slot de comida. Cada Parrillero de su equipo tiene un ancla estable propia: dos trabajadores quedan en extremos opuestos y el tercero en el centro; filas adicionales conservan la ruta y la separación de la base de la mesa.
- `StreetActiveCooksTests`, `StreetKitchenPresentationTests` y `StreetSpecialtyPresentationTests`: **55/55 EditMode passed** en copia aislada con Unity6000.6.3f1, incluidas rutas para equipos de hasta9 y verificación de dos Parrilleros simultáneos separados por más de120 puntos. Unity MCP no conectó, por eso QA visual no se ejecutó. Sin APK/build/install; la0.2.42 mantiene la posición anterior.

## 2026-10-16 — Parrillero y Cocacolero más cerca de sus estaciones

- Parrillero: clearance de trabajo junto a la parrilla de14a6 puntos (8 más cerca); Cocacolero: clearance al frente del barril de2a0 puntos (2 más cerca). Se mantiene la separación entre filas y la tangencia del área de pies, sin solapar el barril.
- Validación focalizada en copia aislada con Unity6000.6.3f1: **42/42 EditMode passed** (`StreetActiveCooksTests`, `StreetKitchenPresentationTests` y ruta de bebidas). Unity MCP no conectó, por lo que no hubo revisión visual en vivo. No se generó ni instaló APK; la0.2.42 conserva las posiciones anteriores.

## 2026-10-16 — Parrilleros cocinan en 2 segundos

- Por pedido del usuario, Racing+ cambia `activeCookSeconds` de1.3a2.0: a WorkRate×1 cada cocción tarda2s; con mejoras sigue aplicándose el multiplicador existente. Los niveles1–5 conservan `cookSeconds=4`. Actualizados balance por defecto, `Main.unity` y regresiones de duración/progreso/escalado.
- Validación focalizada `StreetActiveCooksTests`: **19/19 EditMode passed**, incluida la regresión del umbral de2s. Se generó y verificó APK0.2.42/code44 (Main, Development IL2CPP/ARM64;0 errores/1 warning existente) y `adb install -r` la instaló en Motorola Edge60Fusion; PackageManager confirma versión44. No se borraron datos ni se abrió el juego; PlayMode y revisión visual siguen sin ejecutar. Hash y detalles en [android-prototype.md](features/android-prototype.md).

## 2026-10-16 — APK0.2.41/code43 con pickup por slot de Mozo

- Se generó la APK Development IL2CPP/ARM64 de Main con Unity6000.6.3f1 desde una copia aislada, porque la instancia MCP detectada no respondió al ping. Build incremental: **0 errors/1 warning**; `aapt`, ZIP y firma v2 verificados. PlayerSettings pasa a0.2.41/code43. `adb install -r` actualizó el Motorola Edge60Fusion; PackageManager confirma code43. No se desinstaló/borró datos ni se abrió el juego; no hubo PlayMode ni revisión visual en vivo.
- `StreetActiveCooksTests` EditMode: **19/19 passed**, incluidos los cuatro destinos de pickup y geometría. APK y hash en [android-prototype.md](features/android-prototype.md). La revisión visual específica todavía queda pendiente.

## 2026-10-16 — Mozo busca el slot real de cada sándwich

- El Mozo reserva una unidad cocida identificada y ahora calcula el destino desde su producto y `Slot` real usando la misma geometría `TableSlotPosition` del renderer. Chori/Bondiola y Paty/Vacío conservan sus zonas separadas; se mantiene el lado accesible, el límite visual del personaje y las rutas seguras. Bebidas, niveles 1–5, stock, ventas y economía no cambian.
- Se agregaron regresiones geométricas por producto/profundidad y una prueba de flujo real para cada uno de los cuatro sándwiches. El reintento en copia aislada pasó **19/19 EditMode**; Unity MCP sigue sin responder al ping, así que la revisión visual en Editor queda **sin ejecutar**. `git diff --check` pasó. APK0.2.41/code43 generada aparte; ver encabezado actual.

## 2026-10-16 — cocción activa ajustada a 1,3 s (supersedido)

- Racing+ usa ahora `activeCookSeconds=1.3`, dividido por `WorkRate`; Niveles1–5 mantienen su cocción base de4 s. Se fijó el valor también en `Main.unity` y se añadió una regresión focalizada de duración, ajustando expectativas antiguas que dependían de0,5 s.
- `git diff --check` pasó. El refresco/compilación de Unity MCP agotó60 s y la instancia dejó de responder; la suite EditMode y la comprobación de import quedan **sin ejecutar**. No se generó otra APK ni se probó gameplay en el dispositivo.

## 2026-10-16 — Android0.2.40 build and source push requested

- Unity/MCP build `build-38c7ba9d9d`: Main-only Development IL2CPP/ARM64, **success 284.55s, 0 errors/1 warning**. PlayerSettings advanced from0.2.39/code41 to0.2.40/code42.
- Archive APK `Builds/Android/Archive/HayChoriYPaty-0.2.40-code42-every-victory-market.apk`, versioned `Builds/Android/HayChoriYPaty-0.2.40.apk` and rolling `Builds/Android/HayChoriYPaty-street.apk`:477523029bytes each, SHA-256 `18f6366ba50e33cbbcc2d02f85e48e27bc3bbd18a80f39b31e7dbb9996e8a7ff`. `aapt`, `unzip -t`, `apksigner` v2 verified; Development/debug signature. Not installed/launched.
- Fuente, pruebas y documentación solicitadas quedaron en `1f83e6c` (`Fix Mercado victory payout and ship Android 0.2.40`), rama `codex/street-automation`, publicada en `origin` el2026-10-16. APK instalada como actualización en Motorola Edge 60 Fusion; PackageManager confirmó0.2.40/code42, sin abrir el juego.

# Historial de estado y validaciones

## 2026-10-16 — Economía, cada victoria y conexión Unity recuperada

- Causa confirmada en copia de solo lectura del Motorola0.2.39: victoria nivel2 con$2000ventas/$1285sobrante, pero crédito0 por flags/watermarks migrados. Usuario confirmó50%+2×sobrante y **cada victoria real**. Fuente corregida por intento, con guard de simulación y recuperación idempotente del último recibo válido:357000centavos; test del JSON exacto compra82500faltantes de3y deja274500sin duplicar al recargar. Contrato: [market-fridge.md](features/market-fridge.md#every-real-victory-pays--confirmed-2026-10-15).
- **32/32regresiones únicas aprobadas**: job`e7a373a4cf174cb4b1998837a0744685`19Mercado, `1d0731211b184cfda4e1eef91b480fdb`5lifecycle, ocho payout del XML`policy-sweep-and-exact-phone.xml` (su job`f41a97a7fb004f35a568c82a8cdf3f5b` también contiene3benchmarks fallidos; no declararlo11/11). XML efectivo usado cuando MCP perdió callback.
- Campañas frescas reales compran solo faltantes con dinero ganado y reglas existentes, pagan equipo/velocidad y recargan cada nivel. Varias políticas ganaron1–9; final job`4fc48e4d964b48068bd97e11024df809`3benchmarks perdieron10. Mejor variante:1289entregas/300s,13Chori pendientes, Mercado$5784−$4567=$1217antes de resultado, $5751,50después de derrota; no softlock de efectivo. Benchmark marcado explícito/manual, sin borrar fallos ni alterar dificultad. **No se logró campaña fresca ganadora1–11.**
- Nivel11 se probó por separado con el desbloqueo ya existente de la copia del teléfono: recuperación$3570, reposición$1865, saldo$1705, sin desbloqueo/dinero/stock artificial. Job`d3bf9d3afed34aacb57b5067d19eac5c`:2/2victorias a302,9/302,7s y1450entregas dentro de315s. No equivale a pasar el cuello de botella10ni QA humano.
- Informe/ledgers/XML ignorados: `Logs/Acceptance/EconomyAudit-20261015/economy-audit.md`. Main/default difieren solo en dos arrays legacy con fallback equivalente; metas/costos/tiempos restantes coinciden. Editor955caracteres restaurados exactamente. Unity6000.6.3f1/MCP10.0 reconectados con herramientas locales existentes, sin installs/global config. No teléfono escrito, APK, instalación, cambios de balance/escena/UI, PlayMode, commit/push. ADBsin dispositivo al cierre; arreglo aún no está en APK instalada.


> Para el estado vigente leer [continuity.md](continuity.md), no este archivo completo. Entradas conservadas por tarea/fecha, no siempre ordenadas cronológicamente. Un resultado vale para su código/alcance: los totales no se acumulan y las instrucciones temporales de una tarea vieja no autorizan acciones futuras. Racing+ reemplaza la franja de banderines y el flujo anteriores según [active-cooks-mozos.md](features/active-cooks-mozos.md).

## Auditoría de agentes, skills y contexto SDD — 2026-10-15

**Alcance documental/configuración:** revisados AGENTS, `.codex/config.toml`, tres TOML de roles, cuatro SKILL.md, índice/continuidad/arquitectura/estado/decisiones y contratos actuales. Se contrastaron solo símbolos/valores relevantes de StreetGame/Simulation/Specialties/Kitchen/Market/View y Main, sin modificarlos. Actualizados los entrypoints y contratos contradichos, no gameplay, assets, metas, tiempos, precios o inventario. Checkpoint anterior completo conservado en [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md); AGENTS de la copia inicial ahora solo redirige al checkout real.

**Entorno:** rama `codex/street-automation`, HEAD base `6fcbf39`, Unity6000.6.3f1; app26.1002.52244/build13536 y CLI0.162.0-alpha.2. Check de versión del app confirmó esos valores; updater devolvió busy, sin conclusión de actualización ni instalación. MCP solo lectura confirmó raíz real, Main detenido/idle, sin tests/compilación; la otra instancia temporal no se confundió con este proyecto.

**Contradicciones resueltas:** tamaños fijos pre-modulares vs StreetKitchenLayout; caps/MAX de personal vs contratación ilimitada; cuatro roles de producción vs quinto Mozo con gate Racing; timers/entrega base vs cocción0,5s→mesa→Mozo; banderines/layout viejo vs ampliación desde6 y restauración de Fernet/calle; cinco niveles/precios editables vs once/fijos por ID; monedas/equipo locales vs Mercado persistente sin crédito; tabla Redondos300s vs fuente y Main315s. ARMÁ TU PLANTEL/5%/plantilla permanente siguen pendientes. Pruebas antiguas de campaña no certifican el nuevo balance9–11.

**Configuración conservada:** architecture=`gpt-6.1-sol`/medium, implementation=`gpt-6-luna`/medium, validation=`gpt-6-luna`/low. Dos subagentes adicionales al principal; sin roles/skills nuevos ni pipeline obligatorio. Config global leída sigue Sol/xhigh, sin cambio ni afirmación sobre overrides del chat. Categorías SIMPLE/MEDIA/COMPLEJA, ownership exclusivo, lectura progresiva y pruebas proporcionales ahora explícitas. Modelo/esfuerzo de cada rol y valores efectivos del config de proyecto son idénticos a HEAD; solo prompts/comentario cambiaron.

**Validación estática ejecutada:** cuatro TOML parseados con tomllib; schema mínimo de roles y esfuerzos contrastados con catálogo `codex debug models --bundled`; cuatro skills pasaron `quick_validate.py` con PyYAML ya disponible (sin instalación); Markdown/rutas/anchors resueltos y dos anchors previos corregidos; once filas de metas, duración, costos por rol, multiplicadores/grants comparadas con fuente y duraciones serializadas de Main. `git diff --check` pasó; sin diferencias en Assets/Packages/ProjectSettings ni archivos versionados eliminados. `config/read` del app-server en modo estricto y `skills/list` ejecutados sin inferencia: cuatro skills habilitados/sin errores. Capa global idéntica antes/después. Esquema/discovery también contrastados con la documentación oficial enlazada en [decisions.md](decisions.md).

**Limitación sin resolver por restricción del pedido:** el checkout real no tiene trust en la configuración global; config/read marca su capa `.codex` inactiva. Los roles existen/válidos y están expuestos en este chat ya abierto, pero no se certifica su carga en una sesión nueva del checkout real ni que su límite local sea efectivo allí. Requiere seleccionar el proyecto correcto y autorizar trust; no se modificó globalmente ni se forzó un override de seguridad para el diagnóstico. Otra discrepancia explícita, sin resolver por invención: nombre runtime Fernet1L frente al vaso visual800cc; queda registrada en product/continuity sin alterar rótulo, receta, arte o volumen.

No se lanzaron subagentes, Unity tests, PlayMode, APK o instalación en esta auditoría. No commit/push; todo queda local. Pendientes de juego/dispositivo se conservan en sus contratos, no se convierten en trabajo autorizado.

---

## Altura uniforme de banderines — 2026-10-14

El alto libre se calculaba después de cada panel de mejoras: en Playing, niveles 1–3 tenían 131.5/160.6/187.1/196.0/98.6 unidades lógicas para 16:9/18:9/19.5:9/20:9/tablet 4:3; Ferro (nivel 4), 43.2/68.6/95.1/104.0/32.4; niveles 5–11, 52.8/78.6/105.1/114.0/39.6. La diferencia provenía de las filas: 2–3 acciones en niveles 1–3, 4 en Ferro y 5 en los niveles restantes.

Ahora se reserva el mismo alto para los 11 catálogos y fases por viewport, calculado según el mayor panel de pie; paneles menores se alinean sobre esa franja en vez de dejar hueco. La altura continúa adaptándose a la Safe Area/aspect ratio. Se agregaron aserciones geométricas para Playing, Ready y sin controles en las cinco relaciones anteriores. Unity/MCP apunta a una copia temporal distinta y no se ejecutaron esas pruebas ni se capturó Game View en esta tarea; `git diff --check` pasó.

## Suite PlayMode y geometría de resultados — 2026-10-14

Se resolvieron las fallas de la corrida completa, paso a paso:

- Los 12 fallos de `StreetPointerTests` nacían de un fixture heredado sin saldo de Mercado; los tests intentaban compras cash-only antes de ejercitar punteros/partida. El fixture ahora carga fondos solo para pruebas y prepara stock; la economía del juego no cambió.
- Un fallo sí era visual/funcional: en alturas cortas el botón de siguiente partido del popup invadía el botón NIVELES. `VictoryPopupBounds` ahora reserva el zócalo; se agregaron comprobaciones geométricas entre 600 y 1200 unidades lógicas.
- La interacción de Mercado fallaba por asumir que el host batch apaisado de 640×480 equivalía al layout vertical; el test ahora valida hit-actions/debounce con altura lógica 960.
- El test del display de botellas estaba desactualizado: esperaba cuatro unidades y multiplicador Coca 0,8; el arte actual implementa ocho (cuatro por fila) y 0,76 tras la reducción adicional de 5%. La aserción ahora comprueba composición, proporción, apoyo y separación.

**Validación Unity 6000.6.3f1:** EditMode **434/434 pass**; PlayMode **59 pass, 0 fail, 1 skip (60 total)**. El único skip es la captura responsive de los 11 niveles, que requiere Editor interactivo. Unity MCP solo expuso `hcyp-visual-test` y no respondió al ping, por lo que no se obtuvieron capturas visuales. `git diff --check` pasó.

## Reparación de recompensa migrada en Mercado — 2026-10-14

Los guardados heredados podían conservar Nivel 2 desbloqueado y watermark de recompensa del Nivel 1, pero saldo Mercado cero. Al reabrir la partida, si no hubo saldo disponible ni compras Mercado registradas, se entrega una única reparación por el coste exacto del stock faltante del siguiente nivel desbloqueado (en la partida de referencia, $587,50). Queda persistida; no cambia inventario, precios, objetivos ni FIADO. Las pruebas focalizadas pasaron: EditMode Mercado **22/22**, PlayMode stock-gate **1/1**, incluyendo recarga/idempotencia y exclusión de partidas con saldo o compras previas.

APK Android 0.2.35/code37 Main-only Development IL2CPP/ARM64 generada en 275.3 s, 0 errores/1 warning. Package/version, ZIP e firma v2 verificados; SHA-256 `4d407239dede83d421a75cfc0f2e90586bb98bc4877685f42378d690bfa95b21`. No se instaló en el teléfono: ADB no detectó el dispositivo al intentar verificar conexión. Recibo completo en `features/android-prototype.md`.

## Saldo cero al abrir Mercado tras ganar nivel 1 — 2026-10-14

La reparación de watermark heredado corría solo al habilitar `StreetGame`; si una partida migrada ganaba Nivel 1 y el watermark suprimía su pago, la transición inmediata al Mercado de Nivel 2 mostraba saldo $0. `ProcessRoundResult` ahora aplica la misma reparación acotada antes de desbloquear/guardar el siguiente nivel. El crédito es solamente el costo exacto del stock faltante, condicionado al resultado/watermark, saldo cero y cero compras, con marcador persistente de una sola vez. La regresión reprodujo saldo cero antes del cambio; después verificó persistencia, idempotencia, compra cash-only de faltantes y habilitación de JUGAR. Unity 6000.6.3f1 focalizado: Integration **8/8** y Market domain **15/15**; `git diff --check` pasó. APK Android 0.2.37/code39 generada con Unity 6000.6.3f1 (Main-only, Development IL2CPP/ARM64; 0 errores/1 warning). Tamaño 410,268,801 bytes; SHA-256 `a5a7926f889448f858830ba18d25832488b86571db9c419f95a5307280306fc6`. Metadatos de paquete, ZIP y firma v2 verificados. Suite completa, Game View y dispositivo no se validaron en esta tarea de packaging.

## Level 1 minimum opening stock — 2026-10-14

Every actual Level 1 attempt now starts with at least the live recipe requirement (200 Chorizo + 200 Pan común): persisted surplus is consumed as-is and only partial deficits are granted. Fresh saves seed exactly the live goal rather than a fixed 220; the top-up is persisted in the pre-attempt snapshot so defeat restores it and replay does not stack a kit. Other ingredients, later levels, prices and economy are unchanged.

**Validation/delivery:** Unity 6000.6.3f1 batch run passed 19/19 focused EditMode Market tests and 1/1 focused PlayMode stock-gate regression. Android Main-only Development IL2CPP/ARM64 APK 0.2.34/code36 built successfully in 288.55s; package/version/min API/ABI, ZIP integrity and v2 debug signature verified. Archive and rolling artifact details are in `features/android-prototype.md`. No physical-device install or gameplay check was performed.

## Mercado/Heladera final validation and Android delivery — 2026-10-13

The persistent Market/Heladera implementation is complete and packaged as Android 0.2.30/code32. Focused EditMode Market/kitchen/specialty tests passed 30/30; HUD texture and victory-popup bounds passed 4/4; PlayMode Market view/pointer/navigation passed 39/39. The wider targeted EditMode pass initially exposed a stale 193px HUD texture expectation and the victory popup touching the top edge at a 700px logical height; the expectation now matches the 211px texture and popup scaling reserves the top HUD plus 16px screen insets. The focused reruns passed.

APK build succeeded (Main-only, Development IL2CPP/ARM64, 0 errors / 2 warnings); package/version, ZIP integrity, and v2 Android Debug signature verified. No physical-device install or gameplay validation was performed. Full artifact/hash details are in `features/android-prototype.md`.

## Upper HUD resource cards — 2026-10-13

Removed the level badge from the top row and split the status band into equal coin-left/time-right capsules, enlarged 20%. Re-skinned objective counters as uniform compact translucent cards sized for five columns; 1–5 goals now occupy one row and 6–7 use a second centered row. The single-row bar contracts to remove the unused row. All eleven gameplay and selector captions now show the official team/club followed by its neighborhood, while internal theme lookup names remain stable. The notch geometry still prevents obstruction.

**Validation:** Unity 6000.6.3f1 focused EditMode HUD/caption checks passed 12/12; interactive PlayMode screenshot test passed 1/1 and refreshed eleven 1080×1920 captures in `Logs/Acceptance/HudResourceCards-20261013/`. The updated 11-level contact sheet was reviewed. Physical-device/notch testing was not performed.

**Card-size follow-up:** Quota cards now retain five-column width while growing 20% taller; preferred icon/counter sizes scale ~20% and inner padding is halved. Unity EditMode geometry, five-column, one/two-row and cutout/safe-area checks passed (12 total across two focused runs); the all-level interactive PlayMode capture passed 1/1 and refreshed all eleven 1080×1920 images. The pre-alignment montage was `/private/tmp/hcyp-hud-all-levels-20pct-20261013.png`; the current right-aligned version is `/private/tmp/hcyp-hud-all-levels-rightaligned-20261013.png`. No device test was run.

**Counter alignment follow-up:** Every objective-card counter is right-aligned in its text area, with its product icon anchored at the left inset. Unity 6000.6.3f1 EditMode layout/alignment checks passed 10/10, including 1–7 goal wrapping and notch clearance; PlayMode capture passed 1/1 and refreshed all eleven 1080×1920 level screenshots. No device test was run.

**Counter right-padding follow-up:** Added a 4.8-logical-pixel minimum gap from each right-aligned value to the card edge. Focused Unity EditMode alignment, width, and row-layout checks passed 9/9; the all-11-level 1080×1920 PlayMode capture passed 1/1. Visually reviewed Level 6 at `/private/tmp/hcyp-hud-right-padding-20261013.png`. No device test was run.

## Fernet tabletop extra row spacing — 2026-10-13

The stock display contains four Coca and four Branca bottles in two close rows. The back/front tabletop positions were separated further to make the second row easier to read while keeping every visible bottle base seated on the plank; the bottles remain grouped to the left of finished Fernet cups. Level 6 Game View was re-captured after the adjustment at 2160×3840: `Logs/Acceptance/FernetTabletop-20261013/level6-after-two-stock-rows-2.png`. Focused EditMode presentation test passed 1/1.

## Fernet tabletop bottle size and contact cue — 2026-10-13

The tabletop Coca render target was reduced 5% relative to its previous setting (`.8` → `.76`). Coca and Branca bases are placed slightly inside the visible top plank, each with a low-alpha contact shadow underneath so the bottles read as supported rather than floating; art assets and gameplay are unchanged. Focused Unity EditMode regression `FernetPresentationUses800CcCupAndBottlesBesideFinishedDrinks` passed 1/1. Live Level 6 Game View was reviewed at 608×1080; the saved supersampled capture is 2160×3840: `Logs/Acceptance/FernetTabletop-20261013/level6-after-contact-fix-verified.png`. Unity Editor returned to stopped mode; the error/warning console was empty.

## Fernet tabletop bottles missing in the live view — 2026-10-13

Root cause: `Station_FernetTable` has its `SpriteRenderer` on the positioned station root; `EnsureFernetTableDisplay` treated the root's `localPosition` as table-local and applied the anchor twice, putting bottles outside the render target. The renderer now converts sprite corners into station-local space and uses the existing 2.5 L Coca display sprite. Unity MCP is connected to `/Users/celestino/HayChoriYPaty` in Unity 6000.6.3f1. The focused `FernetPresentationUses800CcCupAndBottlesBesideFinishedDrinks` EditMode test passed (1/1), Console had no project errors, and a live Level 6 portrait Game View capture at 608×1080 visually confirms the Coca/Branca stock bottles on the tabletop and prepared cups to their right: `Logs/Acceptance/FernetTabletop-20261013/level6-after-display-fix.png`. The visual-only level selection bypassed `StreetGame` save methods; Play mode was stopped after capture. No APK was rebuilt. Do not present the old capture as proof of the fix.

## Unity Game View QA and Android 0.2.28 — 2026-10-13

Unity MCP's previous Editor session was disconnected. Opened Unity 6000.6.3f1 on an isolated copy to recover a live Game View, set portrait 540×1200 and started Level 6 (Racing). The capture `Logs/Acceptance/VisualSmoke-20261013/level6-gameview-540x1200.png` shows the full gameplay composition. Both grill effect sets are present; all four smoke sprites stay active, rise/fade on staggered phases, and continue updating while gameplay time is frozen. Visual QA is confirmed for this one Editor aspect ratio; no phone screenshot/device install was performed.

Built `Builds/Android/Archive/HayChoriYPaty-0.2.28-code30-responsive-smoke.apk` (475,893,549 bytes; SHA-256 `5e8683774d4e22df68d386a71b05ce7947b3e7022f6f6b358c57aa5d1b8bf327`). Unity Android Main-only Development IL2CPP/ARM64 build succeeded in512.21s with0 errors and547 warnings; package metadata, ZIP integrity and APK v2 debug signature verified. Complete Unity suites previously passed403/403 EditMode and50/50 PlayMode; focused current checks passed10/10 EditMode and11/11 PlayMode.

## Full suite and late-level gameplay balance — 2026-10-12

The first full EditMode sweep exposed real late-level funding failures. Kept product quotas/prices and hire/speed-cost curves intact; set attempt-only starting funds to $250 for Levels 6–9, $500 for Level 10 and $2,000 for Level 11, and extended only Level 11 from 5:00 to 5:15. The final default adaptive strategy (D) now wins each of the 11 levels in all three deterministic seeds (1337/407/2026); Levels 6–11 average 188.4s, 206.2s, 230.6s, 253.8s, 282.5s and 277.5s respectively.

**Validation:** Unity 6000.6.3f1 batchmode on an isolated project copy passed **403/403 EditMode** and **50/50 PlayMode** tests. The full suite also covered all level table entries, real product handoffs, worker/purchase interactions and responsive pointer dispatch. Unity MCP's active Editor did not answer its state ping; no live Game View screenshot, manual visual review or device gameplay check was possible, so those remain unverified.

## Previous Android install — 0.2.27/code29 (2026-10-12)

Installed Development ARM64 APK on Motorola Edge 60 Fusion after the user requested installation. Build succeeded with 0 errors/1 warning; APK verification passed. Broad tests remain red (EditMode 391/403; PlayMode 44/50), so this is for user device review, not a claim that all gameplay is certified. No Game View/native launch review or Git push was performed. Receipt: `features/android-prototype.md`.

## Whole-order delivery sound — source implemented, runtime verification pending (2026-10-11)

Added a single order-completion event at the final valid real handoff, guarded once per customer ticket; mixed multi-line and multi-quantity orders do not cue on partial delivery. `OrderDeliverySound` attaches as a reusable 2D AudioSource adapter to `StreetGame`, rebinds on retry/next level, and loads a locally synthesized 0.19s soft-pop from `Assets/Resources/Audio/order-delivery-soft-pop.wav` at 20% volume with ±3% pitch and 150ms spacing. This is a fallback equivalent, not Pixabay's Happy Pop 2 file. A focused mixed-order regression assertion was added but Unity execution and audible playback remain unverified. The Android 0.2.23/code25 APK now contains this source, but packaging and installation do not verify audible playback; no gameplay/audio test was run during delivery. See the current Android receipt in `features/android-prototype.md`.

## Two-row gameplay header — source and focused validation (2026-10-12)

Reorganized the shared in-game HUD: coins left, remaining time centered, dynamic `NIVEL N` right, then compact per-product actual/goal cards. Existing catalogs of 1/2/3 products use one centered objective row; 5/7 use two balanced rows. The notch gap remains clear and expands the header vertically only when needed. Game state, goals and gameplay are unchanged. Focused Unity 6000.6.3f1 EditMode checks passed 9/9 across five HUD/cutout/layout tests; Game-view/device review remains pending. Android source version prepared as 0.2.24/code26 for requested packaging.

# Project status

## Club pennant color correction — source change (2026-10-12)

Corrected the live ClubVisualTheme palettes to match the requested flag colors: Ferro starts green then white, Vélez blue/white, All Boys white/black, Boca blue/yellow, River white/red, Racing celeste/white, and San Lorenzo blue/red. Removed stray third accent pennants from Boca, Racing and San Lorenzo; other club palettes and all pennant geometry/layout remain unchanged. Added a focused regression that checks all 20 flags in left-to-right alternating order for these seven clubs.

**Validation:** `git diff --check` passed. Unity MCP at `127.0.0.1:8080` was unavailable, so the EditMode test and in-game visual review have not been run.


## Unlimited workers and speed-price scale — 2026-10-09

Removed employee-count caps for all catalog-enabled roles while preserving the free opening Parrillero, per-level/role base costs, and independent paid-hire curves (x1/x2.5/x5, then n(n+1)/2−1 with safe $5 rounding). Updated all eleven speed multipliers; the existing nine +10% speed purchases still stop at ×1.90. Idle overlapping worker visuals are suppressed without moving simulation workers or changing routes, and legacy cap/save fields remain compatible but no longer constrain hiring. Focused EditMode/PlayMode regression tests were updated/added for 15 hires per role, legacy caps, 15 workers servicing real handoffs, price overflow, exact speed pricing, attempt reset, and idle sprite visibility. `git diff --check` and static source/table arithmetic checks passed. Unity MCP returned no active editor instance; the Unity Editor could not compile or run the focused tests, so runtime validation remains pending. No APK built.

## Orders independent of hired specialists — 2026-10-09

Removed employee-count filtering from quota-balanced order generation; demand now draws only from the active level catalog and existing quota/time weights. Coca and other specialist products may be requested before hiring; lines wait normally and require the correct worker plus a real handoff for delivery and revenue. Unity 6000.6.3f1 focused EditMode validation passed 4/4 cases: first-customer Coca in Chicago/Vélez, all enabled catalogs across 11 levels with zero specialists hired, no locked products, mixed 1–4 quantities, and a pre-existing Coca order remaining pending without fake delivery then served after Cocacolero hire. No APK or broader suite run.

## Per-product balance for all 11 levels — 2026-10-09

Implemented the per-ID quota/hire/speed table, individual victory predicate, quota-aware demand, compact seven-product HUD and product-aware result summary. Fixed official sale prices and hiring/speed costs remain unchanged. Real `StreetSimulation` runs exposed an economy/deadline conflict: at the requested zero-start-money baseline, the optimized employee-first strategy won only 2/3 River and Boca seeds and 0/3 Camioneros/Los Redondos. An explicit minimal balance adjustment is now in source/spec: River gets 10 additional seconds (4:55); Boca, Camioneros and Los Redondos start with $50/$150/$200, respectively; opening funds are not earned income and never carry over.

Measured real-simulation outcomes (three seeds: 1337, 407, 2026, full route/production/order/purchase logic) with those adjustments: River 3/3, average win 285.0s; Boca 3/3, 292.0s; Camioneros 3/3, 252.4s; Los Redondos 3/3, 278.0s. At zero opening funds, L6 and L7 were 3/3 (251s / 260s averages); L8–L11 respectively were 2/3, 2/3, 0/3, 0/3. For L10, $100 opening funds yielded 2/3 and $150 3/3; for L11, $100 yielded 0/3, $150 2/3 and $200 3/3.

Unity 6000.6.3f1 compiled the edited scripts; the console query returned 0 errors/warnings. The focused NUnit job started but Unity MCP Test Runner disconnected/timed out before publishing results; `TestResults.xml` remained stale, so no new NUnit pass is claimed. The all-strategy A–D regression and portrait Game View review remain pending due that editor/MCP outage. See [the authoritative balance table](features/street-automation.md#current-per-product-level-balance--2026-10-08).

## Official product sale prices — 2026-10-08

The simulation uses one stable ID-based price table (Chori $5, Paty $5, Bondiola $10, Vacío $12, Coca $5, Fernet $12, Cerveza $7). Only completed real handoffs award the corresponding amount; victory remains based on units delivered. Legacy saved price values cannot override the table, and demand generation retains its previous uniform-$5 reference. Unity 6000.6.3f1 focused validation passed: 5/5 EditMode and 2/2 PlayMode. Checked all 11 levels, real handoff revenue for all seven IDs across two mixed-ticket tests, unchanged delivered counts/demand/orders, and legacy saves reserializing to official prices.

## Shortest safe employee routes — 2026-10-08

All worker trips now use a shortest-path visibility graph around active station solids expanded by the 36×12 feet collider; direct diagonal segments are retained when clear. Outbound, return, and next-unit legs start from actual worker positions; no forced home return, worker collision system, or NavMesh. Focused Unity EditMode checks passed 10/10 after the final kitchen reflow, covering 840 product/column journeys across all 11 catalog profiles: average per-trip distance reduction 33.7%, maximum 82.5%, aggregate 35.1%. Main Game View inspected during real movement at levels 1 and 11; screenshots: `Logs/Acceptance/ShortestRoutes-20261008/`.

## Parrilla/mesa spacing — 2026-10-08

The prep row is positioned at logical y≈503.03 from the tiled-field lower frame; grills end at y=660, exactly5 points above that frame, while preserving the4-point upper clearance. Both frame and kitchen use the same responsive vertical transform; table/barrel sizes, grill aspect ratio and pickup anchors remain consistent. Focused Unity EditMode checks passed17/17: all11 catalog layouts,840 journey cases, level refresh, and seven standard/specialist route cases. Details: `features/modular-kitchen.md`.

## Employee animation — 2026-10-08

All four roles use eight-direction, distance-driven four-sample native gait and shared hand grips for real physical product IDs. Existing artwork/proportions/routes/economy are preserved; 20 supplemental poses fill missing directions/empty-handed carries. 19 distinct focused EditMode cases and one 448-sample PlayMode acceptance passed. Actual Main gameplay with 5/17 workers at 1080×1920 and 1220×2712 inspected, including lifecycle re-enable and unchanged delivery/revenue; 0 final runtime errors/warnings. Original save/editor state restored. No new APK or on-device performance claim. Contract and evidence: `features/worker-animation.md`.

## Sandwich presentation scale — 2026-10-08

Renderer-only display targets now scale all four sandwiches 1.5× on their tables and 1.25× when carried, preserving art aspect ratio; drinks and gameplay dimensions/economy are unchanged. Focused `StreetKitchenRendererTests` passed 12/12. Main-only Android 0.2.20/code22 IL2CPP/ARM64 build `build-f0bd2b66cd` succeeded in73s,0errors/1warning; APK618,489,985bytes, SHA-256 `3f948c1f1235023b3c4d9a2264fe23d63680f4a879b6eb5fe87d518bde956024`, integrity/package/v2 signature verified and aliases match. Motorola `ZY22MBNWRB` was disconnected, so installation is pending. Full receipt: `features/android-prototype.md`; implementation: `features/modular-kitchen.md`.

## Modular kitchen — implementation2026-10-07

User approved real production/stock and responsive kitchen fitting. Main now contains StreetKitchenRenderer; the single StreetSimulation owns physical item IDs, cooking/burning, real stock, individual take/carry/consume/cancel and role-gated refill.17original transparent PNGs produce29Sprite assets and21reusable prefabs (empty furniture, separate food/drinks/ice/effects). Paired grills and Beer→Coca→Normal→Premium→Fernet preparation row reuse one760→540layout across actual catalogs, with clear worker foot routes, unchanged HUD/worker proportions and club scenery. Original sourcePNG dimensions are preserved with NPOTScale.None; mipmaps/Trilinear avoid minification sparkle. Native Sprite.Create assets require no extra package and preserve GUIDs on rebuild.

Validated149affected EditMode checks (`c40409e402974e1a9038809056c6916e`); final kitchen production/presentation12/12 (`94a96f1af330480290b425aad7b774ee`); four mouse/touch/start/specialty PlayMode cases4/4 (`f0e7761596594ca8bf942a8df691fb21`). Actual Main GameViews checked native composition, all-food profile, barrels/glasses, stock48/36/45,4cylindricalBondiolas/3Vacíos and distinctnormal foods. Native180objects matched180real IDs with0state mismatches;2consumedcarriedIDs removed2objects and handoffs4→6earned20→30. Editor-only2000step/178unit probe587→117ms after full-table early exit; not a phone benchmark. Tall QA exposed a meat-anchor bug (using table instead of grill) now fixed, with native regression1/1 (`c3dbf7bcbb8046f9a8c2437eb8e7fa91`) and corrected1220×2712capture. Initial unexecuted test-job timeouts are not counted; explicit all/assets refresh is required before compiling a newly created test file.

Final corrected tall and Floresta/Ferro/all-food Main GameViews inspected;0console errors/warnings. Exact prior save/GameView19/timeScale1/background=false restored, Play stopped and Main clean. Scoped implementation commit `d2741dbedf35161a0498eca7c09110dbd372269c` was pushed on `codex/street-automation`.

## Vacío sprite refinement — 2026-10-08

The isolated four-state Vacío cut is now asymmetrical: a slightly broader end gradually tapers to a narrower tip, matching the user photo as shape reference only. Replaced `Assets/Resources/ModularKitchen/Art/meat-vacio.png` at its original 2172×724 size and retained its `.meta` GUID; Unity confirmed all four existing Sprite states load unchanged and the focused `StreetKitchenPresentationTests` passed5/5. No gameplay, sandwich, inventory, layout or prefab changes. Current Android APK0.2.18/code20 includes this art refinement. Full detail: `features/modular-kitchen.md`.

## Station layout correction — 2026-10-08

Reordered the upper row to Normal table, Premium table, Fernet table, Coca barrel, Beer barrel. Three tables remain equal at139.3×69.6 logical units; both barrels remain equal at35.5×69.6. Their bounds run across x8.5–531.6 at y520. Both grills remain equal at250.1×83.4, aligned at y620, from x9.9 to530.1 overall (96.3% of the540-unit canvas width). All seven empty furniture shells remain visible at every level; no locked products, stock, or worker roles were unlocked. Table/barrel pickup centers and footprint bounds follow the new station rectangles, with existing route lane/y anchors unchanged. Actual portrait Game View inspected at `Logs/Acceptance/StationLayout-20261008/station-layout-final-settled.png` (788×1400): all five upper stations and both lower grills are on-screen and separated. Focused `StreetKitchenPresentationTests` passed5/5, including all11-level route-clearance and unchanged-catalog checks. No gameplay, progression, HUD, prices or timer code changed. For the required Game View check, the normal level-0 StartRound path was invoked and performed its existing PlayerPrefs save; Unity is left paused because the pre-preview PlayerPrefs value was not snapshotted, so stopping Play may save again. No hires, sales, or progression were performed.

## Latest Android APK — perspective grill fit 0.2.19/code21 (2026-10-08)

Pushed grill geometry/renderer corrections `9648dfb` and packaged Main-only Android Development IL2CPP/ARM64 using Unity6000.6.3f1. Build `build-4524fd8d93` succeeded in409.62s,0errors/1warning. APK `Builds/Android/Archive/HayChoriYPaty-0.2.19-code21-grill-fit.apk`,492,872,066bytes, package `com.haychoriypaty.game`, min26/target36; SHA-256 `078b09aa340f8ca7bcd36f4c21c185a2bdcdacd6f095d58b265d346b2b39e9fe`. Archive integrity, package metadata and v2 debug signature verified; aliases match. No new tests, phone installation or native visual validation. Full receipt and post-build editor-state limitation: `features/android-prototype.md`.

## Previous Android APK — station layout 0.2.18/code20 (2026-10-08)

Unity6000.6.3f1 Main-only Android Development/IL2CPP/ARM64 build `build-ff697f0c18` succeeded in28.71s,0errors/1URP warning. APK `/tmp/HayChoriYPaty-0.2.18-code20.apk`,492,866,782bytes, package `com.haychoriypaty.game`, min26/target36, SHA-256 `00835bd2f3c050cc3b168650a6d9b5c6440bb30d497fa210c7f1d9b2682f5ed4`; v2 debug signature/package metadata and archive integrity verified. Build receipts and limitations: `features/android-prototype.md`. Installed via `adb install -r` on Motorola Edge60 Fusion `ZY22MBNWRB` (Success); PackageManager confirms code20/version0.2.18. No uninstall/data clear. App not launched; no on-device visual check.

## Previous Android APK — four-specialty 0.2.16/code18 (2026-10-07)

Packaged the pushed `cc456c8` source with Unity6000.6.3f1 for Main-only Android Development/IL2CPP/ARM64. APK version0.2.16/code18 built successfully (BuildReport0 errors/1 warning), integrity/package metadata and v2 debug signature verified. Archive, rolling and versioned aliases match, size439,337,048 bytes; SHA-256 `df2944e2975c5c96e03e0c133df78550028f686991c675910aac0c22fe46ed1c`. Console notes one URP asset-inclusion warning and a non-blocking Diagnostics Data symbols advisory. No device install/launch. Detailed Android receipt in `features/android-prototype.md`.


## Four exclusive worker specialties — 2026-10-07 (implemented and validated)

StreetSpecialties is the product→role/station authority: normal Chori/Paty; Premium Bondiola/Vacío on a separate equal-size grill; Cocacolero Coca/Cerveza; Fernetero Fernet only. Role/cards/station availability follows actual catalogs, including synthetic index0/10 tests. All11 live catalogs, IDs, prices, goals, durations, demand, club themes and original Main serialization are unchanged. One TEST_ECONOMY_PROFILE supplies $15/$30/$60/$100/MAX to independent paid tiers; initial normal is free and first paid normal hire is $15. Optional role-profile overrides inherit the existing level profile by default. Save v3 migrates v1/v2 safely, preserves unlocks, and never restores hires/speed/coins into a fresh attempt.

- [x] Separate282×94 Premium and normal grills; normal196×98 finished-food table unchanged in size. Premium kitchen repositions beer/Fernet/table without shrinking, validates full36×12 foot clearance across all7 products/all7 counter columns, and uses reverse return waypoints. Projected90×98 body overlap at different depths is legitimate; old no-Premium strict body-clearance checks remain.
- [x] Four specialty-local FIFO/ownership flows run concurrently; mixed five-type orders complete only after actual unit handoffs. Expiry/cancel clears reservations, income stays handoff-gated. Fresh load/start/retry/next retains one normal, zero other roles and speed×1.00.
- [x] Dynamic2/3/4/5-card wood/iron HUD shares draw/hit targets. Mouse Premium and touch Fernet hires checked. Fixed short-card cost clipping, skin recolor threshold and Ferro's wide Premium title overflow discovered in live QA.
- [x] Premium BLUE apron, Fernetero BLACK apron. Cached palette variants preserve original source geometry, world worker90×98 size/proportions and poses; normal Parrillero/Cocacolero original assets unchanged. Premium-only transparent grill asset added with new meta and generation provenance. Preserved/integrated initial uncommitted drink-label removal, the only pre-existing dirty change.
- [x] Unity6000.6.3f1 FINAL full EditMode **310/310**,0failed/skipped (`cb87605328ec4170831eb4f152fcbe75`,26.85s); FINAL full PlayMode **35/35**,0failed/skipped (`fc1f0e575fbb4633b55a07bb66595609`,53.82s). Post-typography focused presentation run8/8 also passed (`83be9571fdee455294024098f99a0bcf`). These include all existing legacy, catalog, visual-contract, pointer, lifecycle and new specialty checks. Earlier interrupted jobs were not counted as passes.
- [x] Actual Game Views inspected: levels1/3/4/5/6 at1080×1920 and level5 at1220×2712, simultaneous four-role movement/carry, real handoffs (delivered2→10 under manual Step with unchanged balance). Evidence in ignored `Logs/Acceptance/Specialties/level1-final.png`, `level3-final.png`, `level4-final-1.png`, `level5-final.png`, `level6-final.png`, `level5-tall-final.png`. Ferro title was recaptured after correction; others unaffected by that wide-title-only correction.
- [x] Play stopped; Main clean; exact original PlayerPrefs restored and compared; TimeScale1/GameView19/background=false restored. Transient orphan test scenes removed, no existing metas removed. ProjectSettings and Main have no diff. `git diff --check` clean.

**Diagnostics/debt:** No new game compile/runtime errors or failed final tests. A retry was needed after Unity Test Framework `PlayModeRunTask.cs:52` lost its runner at30/35; preserved evidence in Logs/Editor.log and marked orphan job failed rather than claiming success. Final runner messages: results-file notification (bridge classifies it Exception) and PerformanceTesting IPostBuildCleanup warning; neither is a game error. Existing Vélez/Ferro/Camioneros crowd-atlas fragments/clipped cells remain the previously documented visual debt, not altered here. Runtime palette mobile-memory/startup profiling, APK and native-device validation were not run. No packages/services installed. Git delivery identity/result is in the final task report.


## Club Riot completion / scale review — 2026-10-07 (visual gate still open)

Centralized Front/Walking/Riot resources and deterministic customerId frame rules in ClubVisualTheme; preserved live queue, unscaled-time defeat effects and VOLVER. Six new Riot files: Independiente v3, Racing/San Lorenzo/River/Boca v2, Los Redondos v1. Existing All Boys/Chicago/Vélez/Ferro action art retained; Camioneros shares Ferro. New atlases have clean6×4cell gutters and per-pose body-scale/shoe-baseline calibration against their actual normal variant, rather than sizing from stick height or transparent cell area.

Focused checks:19/19 EditMode theme/contract/alpha/body-scale cases passed; real timeout/queue-freeze/TimeScale0/pose alternation/VOLVER test passed across all11clubs. Evidence: ignored Logs/Acceptance/RiotDoD-20261007/level01–11 normal/pose-a/pose-b screenshots (1080×2160). Initial test timing issue corrected to wait for LateUpdate, not a runtime bug. Ferro importer was stretching1536×1024 to2048×2048; fixed its existing meta only and repeated all11loss checks successfully.

**Not yet a final visual sign-off:** review exposed legacy source-cell fragments/clipped edges in Ferro/Camioneros (and minor Vélez fragments), despite valid stick poses. Resolve authored frame separation without altering wardrobes before closing the all11visual acceptance. Some new garment cuts/graphics also need final fidelity review; structural mapping is not proof of exact pixel-level clothing identity. No gameplay/balance/progression changes, APK, commit or push in this task.

## Eleven-level integration — 2026-10-07 (validated in Unity)

User now authorizes activation of all remaining clubs in official order. Runtime/catalog/serialized Main balance extended to11entries: Racing, San Lorenzo, River, Boca, Camioneros, Los Redondos at indices5–10. All six use300seconds and the existing seven products; goals121/133/146/161/177/195 and demand1.65/1.815/1.9965/2.19615/2.415765/2.6573415 increase10% sequentially from level5, with whole-unit rounding. Existing1–5 values, sale/hire/upgrade prices and save schema preserved.

- The mural-card selector now has two6-card pages with standard buttons and visible-page-only pointer targets. Ready/win/loss return to the same selector; saved unlocks cap at the actual final index10.
- Prepared club art is connected to live themes and cropped into the common root scene. Camioneros retains its Plaza de Mayo scenery and Ferro wardrobe; matched Cocacolero art is used for every specialist level. No assets regenerated, worker geometry/routes altered, or products added.
- Unity6000.6.3f1 focused validation passed:33/33 EditMode (`e574bc87df2142c58b438f4fc46a83f1`) including all six actual-goal rounds completed through earned-income upgrades, legacy five-entry fallback and final-level termination;5/5 PlayMode (`c3c6a0f2591142009c113f192f1766b7`) covering both-page pointer selection, locks, persisted unlocks/load/retry/next and return navigation. Final thumbnail correction rechecked6/6 selector/theme cases (`ea76c18647734d7a8b9c3dc35c52dea0`).
- Reviewed actual-level Game Views6–11 at1080×1920 and both final selector pages. Correct club walls, crowd wardrobe, palettes, HUD goals, shared furniture/worker scale; Coca hired from earned income for operator-route presentation. Found and fixed old full-background selector crops that showed asphalt instead of club murals. Evidence: ignored `Logs/Acceptance/Levels6-11-20261007/level6.png` through `level11.png`, `selector-page1.png`, `selector-page2.png`.
- Final console0errors/0warnings; Main scene clean, Play stopped, original PlayerPrefs JSON and editor Time/GameView/background settings restored and verified. Main serialized goals/durations both11entries. No native Android check, APK, device action, commit or push. Historical earlier five-level/art-provenance checkpoints below are retained, not claims that the catalog is still five entries.

## River visual identity — prepared level8, not activated (2026-10-07)

- Added the two-reference River mural and two transparent6×4 full-body atlases:12 Tienda River garment variants,24 front/walk cells and24 matching riot cells. Exact garment/SKU-to-variant links are recorded in `Assets/Art/Street/RiverVisualReferences.md`; sponsor microdetails are simplified and pants/shoes are style choices, not claimed retail replicas.
- `ClubVisualTheme` reserves River index7; later prepared art follows official Boca8/Camioneros9/Redondos10 slots. `StreetView` reuses existing pose/flip/customer rendering and adds only a nonserialized editor visual override. Root counter/actor/station geometry, all five playable entries, balance and progression remain unchanged.
- Reviewed River-only1080×1920 Game View: full-width aspect-preserving wall, normal orders,12 front garments, both walk directions and matched riot outfits. Transparent-background haze found during review was removed using imagegen, with a pixel-level alpha regression. Evidence: ignored `Logs/Acceptance/RiverIdentity-20261007/`.
- Focused Unity6000.6.3f1 EditMode validation:8/8 River/theme checks passed, then2/2 atlas cases revalidated after alpha cleanup. **Visual preview only**, using existing level5 simulation; River gameplay/configuration is still not enabled. No native-device validation, APK, commit or push.

## Master perspective and fresh attempts — 2026-10-07

All five live levels now use the All Boys root ground, counter and operator floor. StreetSceneLayout centralizes the source-art transform, counter top/front, customer clipping and player-side service line; existing StreetWorkstationLayout remains the furniture/pickup authority. Removed backdrop-size/club-dependent counter heights, Chicago-only service/approach heights and the counter's duplicate vertical scaling. No camera, catalog, balance or apparel changes.

- Reframed Chicago's existing Mataderos mural; added frontal panoramic Vélez/Ferro/Independiente mural resources preserving their specific references. Ferro B/C remain one mural. Panels uniformly fit below the unchanged HUD, retaining portrait proportions. Original backgrounds and their metas remain intact.
- Club scenery alone uses neutral All Boys, warm Chicago, clear/warm Vélez, slightly cool Ferro and warm/red Independiente light; actors, orders and controls remain untinted.
- Construction/load/start/select/next/retry use exactly one Parrillero, zero Cocacoleros, SpeedLevel0 / ×1.00. Current-attempt purchases remain effective; legacy save fields stay compatible but are not restored into a fresh attempt. Constructor-injected coins retain their compatibility API; actual attempt-entry coin clearing is unchanged.
- Fixed an in-review Cocacolero rendering issue: its matched atlas has uneven authored rows. Sixteen measured crops now exclude neighboring feet and scale to imported texture dimensions; no new poses or sprites.
- Focused Unity 6000.6.3f1 validation: **128/128 EditMode** simulation/master-layout cases (`cc966d0bfc5f4dd9a68b03ad2ddaa18d`), **26/26 PlayMode** wrapper/pointer cases (`daafeea392b54417801a9ad24be4d86b`), plus **1/1** final Cocacolero row/UV regression (`43cf4bab06a94ed1b728b6cbf4d4ea05`). Earlier obsolete fixture assertions were corrected and revalidated; disconnected MCP responses were not assumed to mean a test pass.
- Reviewed live Game-view captures individually: All Boys, Chicago, Vélez, Ferro, Independiente at1080×1920; also Independiente at1220×2712 and with a hired specialist reaching its drink station. Evidence: ignored `Logs/Acceptance/MasterScene-20261007/level1-first.png`, `level2-final.png` through `level5-final.png`, `level5-tall.png`, `level5-specialist-final.png`. Queue behind the counter; workers and stations on the operator side.
- Play stopped; original PlayerPrefs JSON, time scale, run-in-background flag and Game-view selection/maximization restored. Main scene remains clean. No APK, native-device validation, commit or push performed. The earlier apparel-provenance release gate remains outside this task.

## Final audit checkpoint — 2026-10-07 (release blocked)

Current live catalog: All Boys, Nueva Chicago, Vélez Sarsfield, Ferro Carril Oeste, and Independiente; runtime indices are 0–4. Other prepared club visuals are not playable catalog entries.

- Fixed Chicago level-2 worker approach to stop at/above its counter and selected the shared matched Cocacolero atlas for Chicago (levels 2–5).
- Reworked Ferro and Independiente wall art from the supplied references while preserving canvas dimensions and Unity `.meta` files; Ferro B/C are two angles of one mural.
- Reviewed 9:16 Unity Game-view captures for all five live levels in `Logs/Acceptance/FinalAudit-20261007/`. Revised Ferro and Independiente murals were reloaded and checked in-game; the queue reads behind the counter and stations/workers remain on the player side.
- Unity 6000.6.3f1 suites passed **245/245 EditMode** and **29/29 PlayMode**. Final post-review console query returned 0 errors and 0 warnings. These are editor tests, not a native Android round/device certification.
- **Open release gate — apparel provenance:** runtime atlases are complete-body sprites with no clothing overlays, but there is no reliable per-cell mapping to an exact real retail garment across the five live levels. All Boys has 9 variants; only the white shirt with a broad black band is tied to its documented 2025 shirt reference, while the other designs are category/palette interpretations. Nueva Chicago has 8 variants supported only by shop/category references, with no exact product-to-cell links. Vélez has 9; its blue-V shirt relates to the documented home kit, but no complete product map exists. Ferro and Independiente each have 12; official catalogs/categories informed their designs, but the atlases are explicitly original interpretations without item/SKU mapping. In particular, the current Ferro suplente 2026 is white with broad mint horizontal stripes, and no confidently corresponding atlas cell exists. Sources include [AlboShop / All Boys shirt references](https://tiendaalboshop.com.ar/), [Tienda Nueva Chicago](https://tiendanuevachicago.com.ar/), [Tienda Vélez](https://tiendavelez.com.ar/indumentaria-velez/), [Tienda Verdolaga](https://www.tiendaverdolaga.com.ar/productos/), and [Independiente Store PUMA](https://www.independientestore.com.ar/puma/). No fictitious product mappings were added; resolving this gate requires source-faithful outfit art and per-variant validation.
- At the user's later explicit request, a fresh Main-only Android Development APK was built and installed on the authorized Motorola Edge 60 Fusion: `Builds/Android/Archive/HayChoriYPaty-0.2.15-code17-final-audit.apk` (406,258,507 bytes; SHA-256 `e0b0d607f5f5c3ccda1f4c01991736110973c7c7284fdeabc0b99ffdb809e8f0`). Unity build completed with 0 errors / 1 warning; package manager confirms versionCode17/versionName0.2.15. This does not close the apparel-provenance release gate above; no native gameplay visual review, commit, or push was performed.

## Adaptive customer-order bubbles — validated source update (2026-10-06)

StreetView now sizes the existing atlas speech background to its visible rows, measured quantity width and optional +N footer. Single-product bubbles shrink from 70×86 to typically 62×49 without scaling the 26×23 icon or 18-point quantity text. Two rows use 74 units of height; two rows plus the existing hidden-type footer use 88. Fixed-corner nine-slice drawing keeps the authored silhouette natural, and one tail-baseline transform preserves the anchor/local padding on tall screens. Three/four/five-product tickets retain the existing two-visible-product summary window. No simulation, generation, economy, client or timing code changed.

**Validation:** 14/14 focused EditMode tests passed via Unity MCP on 6000.6.3f1. Actual Game-view captures of one through four (plus five) product types coexist at 1080×1920 and 1220×2712; auto-generated All Boys and Ferro queues also reviewed. Final runtime console window: 0 errors/warnings (an earlier MCP WebSocket warning was tool-only). Capture evidence in ignored `Logs/Acceptance/AdaptiveOrders-20261006/final-*.png`. Editor Play stopped, original Game-view selection/time scale and exact PlayerPrefs restored. No APK/device build or native-device test requested/performed.

## Club-colored pennants — source update (2026-10-06)

Added a reusable ClubVisualTheme lookup keyed by the live StreetSimulation.LevelNames values. Floresta keeps its original baked monochrome row; the other levels draw the same cached footer pennant geometry using All Boys black/white, Nueva Chicago green/black, Vélez blue/white, Ferro green/white and Independiente red/white. No level-specific scene edits or gameplay changes.

**Validation:** `ClubVisualThemeTests` passed 5/5 in Unity 6000.6.3f1 EditMode. Game-view captures were reviewed for all five live levels, including a sequential level switch; the cached pennant palette updated per club and Floresta kept its baked row. Compilation completed without project errors; the console also showed one MCP-for-Unity WebSocket-not-initialized warning (tool bridge), not a game-code warning. Captures are in ignored `Logs/Acceptance/ClubPennants/final-*.png`.

## Chicago grill APK 0.2.15 installed (2026-10-06)

Main-only Android Development IL2CPP/ARM64 build `build-b25ab47045` succeeded in 264.65s with 0 errors / 1 warning. Generated `Builds/Android/Archive/HayChoriYPaty-0.2.15-code17-chicago-grill.apk` (243,108,702 bytes; SHA-256 `8ff5f1d1aeabab3a222e4e7d078ce94eb93f10121b1604d06f6a793a2c31d3c3`); `aapt` metadata and v2 signature verified, rolling APK matches. Installed via `adb install -r` on Motorola Edge 60 Fusion `ZY22MBNWRB`; PackageManager confirms versionCode17/versionName0.2.15. No uninstall/data clear; firstInstallTime remains 2026-10-03. The focused grill EditMode checks had passed 2/2 with a reviewed Game View screenshot in the immediately preceding grill task; tests were not repeated for this build. Native-device visual review was not done.


## Selected victory popup tested and Android 0.2.13 installed (2026-10-06)

The wood/iron/parchment result screen, gross CoinsEarned summary, responsive Salir-to-selector navigation, and existing Chicago dual-goal presentation are integrated. Unity 6000.6.3f1 full Street suites passed **139/139 EditMode + 23/23 PlayMode**, 0 failures/skips. Validation found and fixed a selector local-name compile conflict and an iterator/reflection issue in its regression fixture. Main-only Android Development IL2CPP/ARM64 build `build-f93d61fec0` succeeded in 263.61s (0 errors, 1 Diagnostics Data warning). APK 0.2.13/code15 (190,308,172 bytes; SHA-256 `f0b963338611ad7cd12fadc1a282dee81bac59294ed2d70520541893e4aa9680`) was v2-signature/package verified and installed by `adb install -r` on Motorola Edge 60 Fusion `ZY22MBNWRB`; PackageManager confirms versionCode15/versionName0.2.13. No uninstall/data clear. Actual popup visual review in Game view/on device was not done.

## Crowd counter occlusion and double-height HUD — source update (2026-10-05)

All locations use a view-only queue offset matched to their active background counter edge, with crowd clipping that hides front-row legs behind the stand. Order/patience badges move with the bodies; simulation/FIFO/worker routes and economy are unchanged. The initial anger transition preserves queue placement, then exposes riot poses after counter destruction. Top HUD is now 68 logical pixels high (was 34), still full physical width, with larger fitted comic text/icons, countdown below the top camera channel and two separate Chicago goal rows. Updated focused art/layout regression source and visual/feature acceptance. Only static review and git diff --check; no Unity tests, Game view, APK or device action per standing instruction.

## Level selector and victory-popup route — source update (2026-10-05)

Presentation **JUGAR** opens a five-location selector. All Boys/Floresta and Nueva Chicago cards use their own existing mural and shield; locked later cards show the grill/product progression with restrained club-color accents. A card is enabled only up to persisted `UnlockedLevel`; Floresta starts directly, later levels retain their existing price/setup screen. The won-level popup now contains only **SALIR** after sales/final balance/remaining-time summary, and this action resets to Ready then returns to the selector. Main presentation **SALIR** remains app exit. Added pointer/progression and card-art layout regression source but did not execute it, preserving the user's standing no-test instruction. No Unity/Game-view, APK or device work was done.

## Top HUD extended to physical screen edges — source update (2026-10-05)

The amber top bar now draws its outer frame edge-to-edge, including its decorative fill behind the reserved camera/notch channel. Expanded the sales capsule/text slot; HUD labels use a larger preferred font and fit to both width and height, with compact paired Level-2 counters identified by their icons. No change to HUD height, safe-area interaction geometry, game rules or timer. Updated the focused test source and HUD specifications. No tests, Unity/Game view, APK or phone update per standing no-test instruction.

## Android APK 0.2.11/code13 generated (2026-10-05)

Implemented selected upgrade-card option1: current speed multiplier plus next `+10%` in the Velocidad status row, live `EQUIPO N` in the Parrillero status row, and removed the duplicated footer. Main-only Android IL2CPP/ARM64 build `build-2dccbdc803` succeeded in19.38s (0errors/1warning). Versioned APK `Builds/Android/Archive/HayChoriYPaty-0.2.11-code13-upgrade-option1.apk`,181,258,740bytes; SHA-256`7ff670d8c5999a4c980fd263433c57289a6468c09aeea1928da9aec5ffe4fbab`; package/version/ABI and V2 signature verified. No tests or phone install per the user's standing instruction/request scope.

## Android APK 0.2.10/code12 built and installed (2026-10-05)

Built current Main source with Unity6000.6.3f1 as Development IL2CPP/ARM64. Build succeeded in 246.68s. The log contains a non-blocking Licensing access-token update diagnostic and one Diagnostics Data debug-symbol recommendation; no gameplay tests were run. APK `Builds/Android/HayChoriYPaty-street.apk` and archive `Builds/Android/Archive/HayChoriYPaty-0.2.10-code12-mural-wardrobe.apk`, 212,807,089 bytes, SHA-256 `639e11908265ff2b228afc2a40621f69a6b2d000f5dd1993f4aa5bb8244eb8a4`. V2 signature/package/version verified. Installed successfully with `adb install -r` on the authorized Motorola Edge 60 Fusion (serial `ZY22MBNWRB`); PackageManager confirms versionCode12/versionName0.2.10. No uninstall, app-data clear, or launch; no tests were run. Evidence is in ignored `Logs/Acceptance/Level1MuralWardrobe-20261005/`.

## Nueva Chicago supporters — eight integrated outfits (2026-10-05)

Replaced Level 2's separate garment overlays with eight complete green/black/off-white fan designs in matched front and profile-walking 3×3 atlases. A separate 4×2 Chicago-colored atlas keeps its full-body outfits during the angry stick-raising/swinging timeout animation. The old garment sheet remains in the project as historical art but is not loaded by the renderer. No gameplay/simulation behavior changed.

- [x] Added transparent RGBA front, walking and timeout atlases with Android RGBA32 import metadata.
- [x] Level 2 crowd art selects outfit deterministically by customer ID; facing direction, walking, anger and swinging remain pose-driven.
- [x] Unity 6000.6.3f1 imported the assets and compiled the changed scripts with zero console errors; focused EditMode atlas/import/runtime-selection test passed **1/1**.
- [x] Live 1080×1920 Level-2 Game-view screenshot reviewed: `Logs/Acceptance/ChicagoOutfits-20261005/chicago-eight-outfits-playing.png`. Eight integrated green/black outfit looks appear in the crowd, including a walking pose, with no separate garment layer.
- [x] Unity Play stopped and the prior `StreetGame.v1` PlayerPrefs progress/economy value restored exactly after capture.
- [x] No APK/device installation was requested; none generated.

## All Boys supporters — nine integrated outfits (2026-10-05)

The prior four black/white-uniform-only poses failed the user's visual expectation that the curated All Boys apparel should appear distinctly in the crowd. Added two matched transparent 3×3 atlases: `street-allboys-fans-front-v1.png` and `street-allboys-fans-walk-v1.png`. Each cell is a complete fan body in one of nine white/black/gray/cream looks; runtime cycles by customer ID for both cheering and walking. Level 1 never overlays garment art; Nueva Chicago was still using a separate wardrobe renderer at this checkpoint and was refreshed separately below.

- [x] Unity 6000.6.3f1 complete test assemblies passed: EditMode **116/116** and PlayMode **21/21**, zero failures/skips. EditMode includes the 9-outfit atlas/crop/pose regression.
- [x] Live 1080×1920 Level-1 Game-view screenshot inspected and saved at `Logs/Acceptance/AllBoysOutfits-20261005/allboys-nine-outfits-playing-screen.png`; nine looks rotate deterministically, are part of full-body sprites, and have no garment overlay.
- [x] Editor Play stopped and the exact original `StreetGame.v1` PlayerPrefs JSON (240 coins, level unlock, price/staff/speed) restored after the capture. No APK was generated or installed; native-device appearance not verified.

## All Boys four-uniform pose correction — historical/superseded (2026-10-05)

At this earlier source revision, the nine separate garment overlays had been removed and replaced with four complete front and four matching walking poses from street-characters. This stopped the superimposed-shirt defect, but did not visibly use the varied curated outfit styles. The later nine-outfit full-body atlases above supersede this clothing selection; the no-overlay rule and unchanged Nueva Chicago apparel remain current.

**Validation:** Unity6000.6.3f1 StreetArtTests **44/44 EditMode** and StreetPointerTests **17/17 PlayMode**, 0 failures/skips. Opened Main in Play Mode, started Level 1, and saved/visually reviewed the 1080×1920 screenshot Logs/Acceptance/AllBoysKit-20261005/allboys-level1-playing.png. Editor PlayerPrefs were restored exactly after capture. No full-suite run, APK build, or phone install; installed 0.2.10/code12 predates this source-only correction. Evidence logs are under the ignored Logs/Acceptance/AllBoysKit-20261005/.

## Floresta mural continuity — source update (2026-10-05)

The earlier crop ended at source y=234, before the mural wall/coping image finished. `StreetView` now draws one continuous mural strip from source `(0,0,940,280)` into logical `(0,0,540,118)`, covering the underlying v4 wall so its mural cannot reappear as a second band. The street, counter, foreground and gameplay anchors below remain unchanged. Updated the focused asset/layout regression source.

**Validation:** Source and diff review only. No tests, Unity/Game-view review, APK build or device install, following the standing instruction. Visual confirmation of the seam is still pending.

## Floresta All Boys apparel overlay selection — superseded (2026-10-05)

This intermediate source revision temporarily selected nine standalone garment sprites for Level 1. The user rejected that approach because the garments read as overlays rather than naturally worn All Boys clothing. The current implementation uses the complete uniformed fan poses; see the correction above.

**Historical validation:** No tests or Unity/Game-view review were run on that intermediate revision. It was not built or installed on device.

## Nueva Chicago fix — Android APK 0.2.9/code11 built and installed (2026-10-05)

Packaged the current project in Unity6000.6.3f1 as a Main-only Android Development IL2CPP/ARM64 APK. Build succeeded in **240.64s**, 0 errors / 1 Diagnostics Data debug-symbol recommendation. APK: `Builds/Android/HayChoriYPaty-street.apk` and versioned archive `Builds/Android/Archive/HayChoriYPaty-0.2.9-code11-level2.apk`; **171,936,606 bytes**, SHA-256 `be171c5d141d7de2fd4af2dab0bd8654d67b5c629c14ae3e299d525a9bdf08fb`, V2 signature verified. Package `com.haychoriypaty.game`, min API26 / target36, ARM64.

Installed to the connected Motorola Edge 60 Fusion with `adb install -r`; versionCode11/versionName0.2.9 confirmed. PlayerPrefs matched byte-for-byte before/after and original firstInstallTime remained intact. No uninstall, data clear, or app launch. The full 116 EditMode + 21 PlayMode suite had passed on this exact code before the version-only build. No native gameplay/visual review was performed. Details/evidence: [Android delivery](features/android-prototype.md) and ignored `Logs/Acceptance/Level2Delivery-20261005/`.

## Nueva Chicago delivery clearance and wardrobe fit — source update (2026-10-05)

Moved Chicago's idle/handoff point to the player-side floor (logical y=485, below counter front y=384) and kept all worker station/handoff travel outside the counter. Fitted Chicago wardrobe overlays independently for frontal standing and side-walking poses. Added regression checks for both-product handoff clearance and pose-specific outfit bounds.

**Validation:** Full Unity6000.6.3f1 batch suites passed: EditMode **116/116** and PlayMode **21/21**, 0 failures/skips. Automated route/layout checks are not a substitute for native Game-view/phone visual review; the later APK/install above contains this change but no gameplay screenshot was taken.

## Full Unity suite and Android APK — 0.2.8/code10 (2026-10-05)

All Editor tests passed on Unity6000.6.3f1: **115 EditMode + 21 PlayMode**, 0 failed/skipped. The first PlayMode run had 3 failures caused by missing Game-view focus in an older pointer fixture; the fixture now preserves/restores the same temporary Input System focus settings as the newer suite, and all 21 passed on rerun. Android Development IL2CPP/ARM64 build succeeded with 0 errors and 1 Diagnostics Data debug-symbol recommendation. APK: `Builds/Android/HayChoriYPaty-street.apk` (archive: `Builds/Android/Archive/HayChoriYPaty-0.2.8-code10-fullsuite.apk`), 168,244,348 bytes; SHA-256 `6600a5dc54d5c9f917ce2d33d8f2ee999f613ba87cebcd8ef02f44ea8371b240`; V2 signature verified. No phone install or native gameplay validation was requested/performed. Evidence is ignored in `Logs/Acceptance/FullSuite-20261005/`.

## Nueva Chicago duration set to three minutes — source update (2026-10-05)

User requested a 180-second Level 2 limit. Updated the inspector/default/fallback durations and Main serialization; Floresta remains 120 seconds, while Levels 3–5 remain 240/270/300 seconds. Added a regression-source assertion for Chicago's default time and updated the progression table. No tests, Unity run, APK or device install per standing instruction.

## Floresta duration reduced to two minutes — source update (2026-10-05)

User requested a 120-second Level 1 limit. Updated StreetBalance inspector/default/fallback values and Main serialization; goal remains 200 actual choripán handoffs, win-at-goal/deadline-loss behavior and all later-level durations are unchanged. Regression source now expects 120 seconds and includes the 2:00 HUD formatting case. The previous deterministic balance run completed in 98.9 seconds, which is 21.1 seconds under the new limit by comparison; it was not rerun after this edit. No tests, Unity run, APK or device install per standing instruction.

**Current source milestone:** Level 2 / Nueva Chicago has its club mural/shield, bottle/barrel, chori-only/Coca-only/combined orders, eight original green/black/off-white fan outfits integrated into full-body front/walk/timeout sprites, and a two-part win goal of 200 choris **and** 200 Coca bottles. Its upper sales capsule tracks the products separately. Level 1 rotates nine neutral All Boys-inspired looks integrated into complete front/walking crowd sprites; no clothing layer is overlaid in either level. Current source correction and validation are recorded above. The latest installed phone APK is **0.2.10/code12**; it predates both integrated apparel sprite refreshes. Earlier entries below are historical checkpoints, not current source facts. See [Android delivery](features/android-prototype.md).

## Timeout trifulca becomes a live animation — source update (2026-10-05)

Replaced the single static crowd composite as the active loss presentation. `StreetView` now animates the actual frozen waiting queue through fear/anger into staggered raised-stick/swing poses from a new 4×2 fan atlas. A people-free damaged-stall background fades in during the attack, while fan frames, screen rumble and wood debris continue on unscaled time. Added fresh metas for both generated assets and updated the focused test source and authoritative visual/gameplay specs.

**Validation:** Static source/art/spec review and diff whitespace check only. No tests or Unity/Game-view run, APK or phone changes, per the user's standing instruction. Visual pose alignment and full sequence timing remain pending.

## Floresta mural seam correction — initial source update (2026-10-05)

The first correction extended the new mural overlay through logical y=106 while retaining the v4 street/counter/foreground. The user later reported that the wall still appeared cut/restarted; the follow-up below extends the source crop through the full lower wall edge/coping and slightly increases the continuous overlay band.

## Nueva Chicago 200 + 200 objective — source update (2026-10-05)

Chori and bottled Coca-Cola progress now has independent per-round counters; Level 2 wins only after at least 200 actual handoffs of each (400 minimum combined). Timeout/loss and HUD/result progress use the same per-product target. Other levels retain their existing goal logic. Both products start at a $5 sale price; Level-2 price controls remain available.

**Validation:** Added focused simulation regression source but did not run tests or Unity; no APK/device install.

## Nueva Chicago supporter wardrobe — historical overlay implementation (2026-10-04)

Level-2 fans now layer one of eight generic logo-free jersey, T-shirt, hoodie/sweatshirt, track-jacket and windbreaker designs over their existing animated bodies. The green/black/white palette and garment categories follow the current [Tienda Oficial de Nueva Chicago](https://tiendanuevachicago.com.ar/); store photography, exact crests, and manufacturer marks are not reused. Floresta's separate All Boys rotation is curated to nine monochrome/neutral choices (see correction below).

**Validation:** Focused regression source was added but not run, per the user's standing instruction. Unity import/render review, test execution and APK/device install are not claimed.

## All Boys supporter clothing correction — source update (2026-10-05)

At this checkpoint, Floresta's active fanwear rotated only nine white, black, gray and cream options; Chicago's separate green/black/white wardrobe was unchanged then. Both levels' current complete integrated sprite implementations are recorded in the later updates above.

**Validation:** Updated the focused regression source and visual/feature specs; did not run tests or Unity/Game view, per the standing user instruction. No APK/device install.

## Implemented and executed

- Main owns `StreetGame`/`StreetView`; legacy `GameController`/`PrototypeView` disabled there, source/tests preserved. Main/SampleScene GUIDs and build entries preserved; no new package/service.
- Original sprite assets: Floresta/All Boys cartoon background,16 character poses, seven products plus street grill/cooler/condiments and management UI. No copied video assets or geometric stand-in actors. Active art bounds/layout visually checked in portrait.
- First-level mural update: active StreetView background uses versioned `street-background-mural-v3.png`, with original monochrome supporter art on the far wall across the street; old background retained. Unity import/refresh completed and Play-mode Game View at the start menu visually reviewed (`Logs/Acceptance/Mural/floresta-mural-menu.png`). Console had no compile errors; no APK/device validation requested or run.
- Bounded deterministic simulation:21 clients in7×3 rows, orders1–999, independent reservations and physical station/pickup/carry/handoff gating; per-product prices/demand, up to8 workers, affordable escalating speed/hire, patience/exit, win/lose/replay and five progression levels.
- Save v2 adds a one-time v1 migration resetting inflated starting staff/speed to 1 worker/×1.00, while preserving prices/coins/unlocked level; in-flight rounds are not saved. Focused migration EditMode test passed (job `9a01a404c67042c49eae40f8170a5e36`, 1/1).
- Platform-specific input: editor Input System bridge; native IMGUI only in Android. Single shared safe-area geometry with stale editor-safeArea fallback.

## Validation — 2026-10-03

- Full reference video decoded: **1798/1798 encoded frames**,60fps,29.9667s (roughly1800). Inspected whole time range and transition samples; image analysis found22 stable decrements999→977. No hire/speed tap or completed client occurs in this clip. Video mechanics vs requested extensions are separated in [visual reference](visual-reference.md); raw/extracted evidence stays private in ignored `Logs/ReferenceAnalysis/`.
- Street EditMode **16/16 passed**, job `41993b7f6e044a3c91f61f0b1ec9bafd`: handoff, prices/demand, cancellation, caps, independent RNG, reservations, throughput, costs and outcomes.
- Street PlayMode **9/9 passed**, final job `39937369fdf3482eaa79024ae973f1c5`: queued mouse/touch Start, slider endpoints, unique purchases, replay, independent product-price selection/no HUD overlap and viewport edge cases. These are editor tests, not physical-device certification; no unrelated full-suite run.
- Actual Main one-worker play finished **Won24 units/$120 earned in63.65 simulated seconds**; reload restored coins/unlock. Second run restored120 and won24 at240coins. This is deliberately slower than video counter cadence because user-required real roundtrips gate each unit.
- Isolated five-level runs with8 workers reached goals24/40/65/85/110 and unlocked product counts1/3/5/6/7. Actual wrapper load/save restored123coins/3staff/2speed/level4/seven distinct prices; corrupt JSON and version99 defaulted safely.
- Pure simulation profile21clients/8workers:1000steps (16.67sim seconds) cost1.403ms total in editor;49deliveries vs6 with1worker. **Not GPU/mobile FPS**. All Assets paths have metas; duplicate GUID scan empty.
- Reviewed real Game-view `Logs/Acceptance/street-layout-final.png`: upper crowd/menu, central street workers, bottom stations and large Speed/Cook cards. Captures/logs ignored in Git. Editor compilation completed before focused tests.

## Android0.2.0 actual run

Final build `build-9d977619d5`:0errors/52warnings, signature/package/ABI verified. Native Start, price60→5,21clients/999orders, exactly one hire15 and speed5; victory24/$100/team2/rate1.25 confirms24×5−20. Native next unlockedChicago/3products/goal40; zero endpoint, separatePaty11/Chori5 and restart restored100coins/team2/rate1.25/unlocked1. Product selectors moved below price-panel title after real screenshot exposed HUD overlap;9/9 regression passed, final APK rebuilt/installed; native product selection and visible/unobstructed HUD confirmed. Read [Android delivery](features/android-prototype.md) for artifacts, warnings and scoped limits.

## Remaining

- The phone remains on the earlier0.2.7/code9 build with data preserved; the current Level-2 feature is not installed. Controlled native intro/Salir/launcher-mask/touch accounting, Level-2 visual review and FPS/thermal validation remain pending.
- Argentinos Juniors, Vélez and Ferro still share existing gameplay art; location-specific murals and supporter kits remain pending.
- Audio, final animation/art polish, device FPS/thermal profiling, broader cutouts and pause/resume stress, balancing and release signing/Play Store publication remain unverified or out of scope.

## Floresta three-minute trial — 2026-10-03

User chose a full180-second first level. `florestaFinishAtDeadline=true` on Main means reaching24 units no longer ends Floresta: continue serving/earning until the clock, then evaluateWon/Lost. Clock clamps to the deadline; other levels still win early. No waves, products, art/input, upgrade/reset or save-schema changes.

- Street simulation **22/22 passed**,0failed/skipped, job `e1d05e79e164439f94d13b0e4c107315`: new continuity/extra earnings with8staff+7speed,Playing at179s,Won exactly180/frozen after end, insufficient-goal deadline loss, and four later-level early wins.
- Main new policy explicitly serialized; editor0 compilation errors after import. Version0.2.1/code3 flushed toPlayerSettings. APK0.2.1/code3 build75.02s/0errors/4warnings,signature/ABI/version verified and installed. Native continued60units/$972 aftergoal24 with172s left (baseline672coins,7staff/rate3.25 preserved); complete deadline check pending. Earlier0.2.0 screenshots/results remain historical.
- MCP recovered using installed10.0.0 cache and existing bridge API, no install/second editor. Chat client cached startup error; real MCP SDK client can query/call tools. Temporary editor bootstrap restored, metas unchanged.

- Physical Motorola updated0.2.1/code3 and launchedStatusok after user chose phone over emulator; USB subsequently disconnected before screen verification. Old emulator endpoint shows958sales/56s remaining, not a verified deadline result. No emulator now running; preserved data with install-r.

## Parrillero character — 2026-10-03

Generated original16-pose transparent atlas and hire portrait following user photo/required fat shirtless character with dirty white apron. Active view switched to new assets and visible Parrillero label; supporters, gameplay/save and existing metas preserved. Final selected face uses wavy dark hair/light stubble, fuller cheeks/jaw and matched skin; previous giant-eyed alternate discarded.16 named Unity sprites/foot pivots verified.3/3 StreetArtTests passed (job962b86955487489d9818270118bbc5c3),1/1 affected hire/speed pointer test passed (job5e6b90e5136b4cf5bac2115a5e3e1aa7),0compilation errors. First pre-import job found0tests and is not counted. Extra carry/celebrate/tired poses delivered, no new simulation actions. APK0.2.2/code4 succeeded210.41s0errors/3warnings and signature/ABI verified; phone install/native visual check pending because USB/adb inventory empty even after user reconnection and oneADB restart.

Actual editor Main capture reviewed (2026-10-04): new profile walk, matching hire portrait and unclipped Parrillero label in original Floresta layout. Ignored Logs/Acceptance/Parrillero/parrillero-final.png; stopped Play and restored exact prior editor progress. USB/ioreg and adb show no physical phone, so APK0.2.2 not yet installed/launched there. Three build warnings: diagnostic symbols and Unity-splashPVRTC/fallback; no postprocessing warning in this build.

## Floresta fixed economy and small orders — 2026-10-04

Implemented in source: fixed$5 chori and read-only start/replay UI (no slider or hidden drag action), fixed$5 speed purchases/+10percentage points of base rate, fixed$25 extra parrillero, automatic1–4 orders including first arrival. Completed orders retain existing receipt→exit→replacement flow, with per-unit$5 earnings. Old stored price normalizes without resetting coins/staff/speed/other prices; later levels preserve existing rules. Full180seconds/goal24 and art unchanged. Added10EditMode/2PlayMode cases and adjusted affected previous fixtures.

Validation pending: git diff check passed, but MCP refresh timed out60seconds and test invocation disconnected. Read-only Unity process sample confirms Main scene-change/reload modal blocks editor command processing; user asked to accept Reload. No test success claimed before rerun; no unrelated suite or second editor. User explicitly requested noAPK: existing0.2.2/code4 artifact does not contain this change and was not rebuilt/installed.

## Large parrilla and validated Floresta rules — 2026-10-04

Original long iron-grill sprite with dense rows of chorizos, bread and embers replaces miniature BBQ presentation.494x108logicalpx for1–4 unlocked hot foods;280x90when beverages need right-side space. Seven-product catalog and all simulation pickup anchors remain unchanged. Only unlocked markers drawn; original artwork remains fallback. Previous first-level economy/order change now verified (earlier blocked record historical):41/41EditMode (32simulation+9art),11/11PlayMode; jobs c900430c9df54e1ab041b0a2475b6e54 and81416a6df3c746eb8fa26d209fbcf885,0failed/skipped.

Real1080x1920Game-view Ready/FlorestaPlaying/seven-product captures reviewed; grill scale, no price slider,$5/$25/+10%,1–4orders and unobstructed HUD/buttons/drinks confirmed. Exact prior editor progress restored(true),Play stopped. Unity0compilation errors before tests. User now explicitly requests APK, but no phone connection/install/launch. Version0.2.3/code5 build-4df2bd9cd6 succeeded363.11s,0errors/3warnings. APK57,681,379bytes, V2signature/package/ARM64 IL2CPP/min26/target36 verified; SHA256af50ac8961483c875191fa5ad4540484454ddf8037bb3e1e5e3ad2fcd471db90. Warnings: diagnostics require symbols, obsolete Unity-splashPVRTC and uncompressed splash fallback. No new packages, second editor, emulators or adb actions.

## Floresta fan wardrobe overlay — 2026-10-04

Added a first-level-only renderer overlay which selects among the 15 candidate logo-free `FanWardrobe` textures by customer ID; the active list has since been curated to nine monochrome/neutral choices. Original fan sprites and later levels stay unchanged. Added focused EditMode coverage for resource loading, alpha/import settings, Android RGBA32 and deterministic rotation. Unity Editor import/test/Game-view verification of the current curated list remains pending.

## Startup cover/logo intro — editor and APK verified (2026-10-04)

Supplied cover/logo textures and 4.1-second unscaled reveal/flash now implemented; central dispatch gates native Android and desktop actions, with a black backing during initial fade. 11/11 focused art EditMode checks passed. Two batch pointer attempts failed to process queued inputs (first also had a stale v1 preservation fixture); those failures are not hidden passes. The corrected v2 fixture and full affected pointer suite passed 12/12 in real editor (job adfc5a956d4f48548c73f34aa2caec0d), including intro input/timeScale/Ready/Start regression.

Actual 1080×1920 cover, logo-flash, Ready and representative Floresta wardrobe captures reviewed in ignored Logs/Acceptance/Intro/. Prior editor progress restored exactly (true); Play stopped, Main is clean. Authorized stuck-editor shutdown/restart restored MCP; project, state, active scene, hierarchy and console compilation-error query succeeded. No new packages or simultaneous editors. Android0.2.4/code6 ARM64/IL2CPP Main-only build1970fbd5d5 succeeded364.73s,0errors/3warnings. APK106,121,029bytes (101.20MiB); V2signature/package/min26/target36 verified. Exact SHA and warning details are in [Android delivery](features/android-prototype.md). Unity remains open on clean Main, Play stopped; no phone/emulator installation or native verification.

## Startup APK — physical phone delivery (2026-10-04)

User requested installation on the connected Motorola Edge60Fusion. Serial-targeted replacement install returned Success; PackageManager confirms0.2.4/code6. GameActivity cold launch Statusok/769ms and app process alive. No uninstall/data clear, rebuild, new packages or emulator. Foreground changed to another app before screenshot, so no physical-screen/intro/touch/performance verification claimed. Existing editor stays open, Main clean/Play stopped.


## Longer startup and Jugar/Salir menu — implemented, checks deferred (2026-10-04)

Source now holds the fully visible cover 4s, reveals the logo, holds it fully visible 4s, then retains both behind original glossy blue JUGAR/SALIR buttons (Luckiest Guy with white/dark-outline text; Apache 2.0 license bundled). Fades are extra: buttons become available at 8.63s unscaled. Jugar starts the saved selected round directly; Salir calls player quit / Editor Play stop. Hidden game controls are not rendered/dispatched while startup/menu owns input; no simulation/economy/progression/product changes. Existing startup regression source adapted to persistent menu, not executed.

**Validation:** Tests, Play-mode/visual/device verification and APK generation explicitly deferred by user; previous 0.2.4 test/build records are historical, not proof of this change. No second editor, package install, phone control, version bump or build. Source diff reviewed; normal asset refresh imported the font with includeFontData enabled and generated new metas; Editor finished reload, remains on stopped Main. Console MCP requests returned session-not-ready/ping timeouts, so clean compilation is not confirmed; no gameplay validation claimed.

**Next:** When user requests combined validation, check the≥4s+≥4s timing, button typography/layout/safe-area, both platform input paths, direct Jugar and native Salir; build/install only if separately requested.


## No Floresta price popup — implemented, untested (2026-10-04)

Removed the full first-level price card/summary from StreetView.ReadyPanel, including replay/Ready paths; non-modal blue Jugar and unlocked-level navigation remain. Hidden product hit targets are also gated to editable-price levels. Simulation already enforces chori $5 (including old saves), so economy/progress and later levels remain unchanged. Startup menu changes from the preceding request are preserved.

**Validation:** Source inspection only. Explicitly no tests, Play-mode/device checks, APK, version/settings/scene/meta changes or new Unity session; runtime/import/compilation for this edit not verified. Test files were not changed in this follow-up.


## Floresta $25 speed / $200 helper and replay reset — implemented, untested (2026-10-04)

Changed StreetBalance defaults and Main serialized values to $25 per speed upgrade and $200 per extra parrillero; +10% base increments and fixed/no-escalation first-level costs remain. StreetSimulation constructs and starts every first-level turn with one worker and SpeedLevel0 (displayed x1.00), clearing old staff and resetting worker IDs; this also covers wrapper reload/retry and return-to-Floresta start. Coins, unlocks, fixed $5 chori and later-level staff/speed rules remain unchanged. Earlier cover/menu/no-price-popup edits preserved.

**Validation:** Source inspection only; user previously explicitly deferred all tests/APK and that instruction is retained. No Play, tests, APK, device/Editor mutation, package install, save-schema/version change or compilation/import verification. Scene diff is limited to the two balance fields; existing metas preserved. Old hard-coded cost/staff-preservation assertions in StreetSimulationTests/StreetPointerTests must be updated for the new contract before combined testing; no test-file edits in this turn.


## FIFO lanes and rear-only replacement — implemented, untested (2026-10-04)

StreetSimulation now releases departing queue slots and advances existing customers in the same column (all levels), retaining ID/order/patience and moving continuously. New arrivals are tail-only; leaving people remain counted until off-screen. Advancing state keeps visible order bubbles and uses existing walking frames; StreetView depth now follows actual positions. Workers reserve/deliver only to settled front customers; stale/moving/departed assignments are canceled without sale/reward or stuck reservations. Earlier first-level costs/reset, no price popup and startup menu preserved.

**Validation:** Source review only. No tests, Play/visual/device checks, APK, compile/import confirmation, package/asset/scene/meta changes, commit or push. Standing no-tests/no-APK instruction retained. Existing tests that assume direct free-slot reuse/rear-row serving need adaptation before later combined checks.


## Open street backdrop / counter-only stand — implemented, game review deferred (2026-10-04)

New original versioned940×1673opaque `street-background-open-street-v4.png` removes the upper stadium/sky, roof/awning, branding header, chalkboards and tall supports. Mural ends at the top frame; broad open crowd street fills the space behind the retained counter. Existing worker sidewalk, condiment bench and lower HUD band retain their bands. StreetView loads v4 and removes both upper product-sign icons; queue/counter/worker/station/input anchors and prior gameplay/menu/reset changes preserved. Old v3/metas retained; new meta has fresh GUIDs and inherited import settings/full bounds. Exact prompt/method/SHA retained in GenerationPrompts.md.

**Validation:** Built-in edited image statically reviewed and source diff inspected only. Explicitly no tests, Play, APK, device work or Unity import/compile confirmation. Runtime crowd/overlay/counter alignment with new backdrop is still unverified. No package/settings/scene changes in this turn; no commit/push.


## Waiting supporter animations — implemented, untested (2026-10-04)

StreetView now adds per-customer staggered breathing, foot-pivot leaning/weight shifts and occasional two-bounce cheering while stationary. Existing supporter identity and Floresta clothing share the same body transform; GUI matrix is restored before stable order/patience indicators. Walking/Advancing and receipt bob remain; existing AnimationTime drives all idle movement, with no changes to simulation, FIFO, economy, input, sprites/metas or prior work.

**Validation:** Source/diff review only; no tests, Play, APK, device work or Unity compilation/import confirmation. Visual motion, garment attachment and dense-row readability await the later combined check requested by the user. No test files changed for this request.


## Presentation-logo app icon — implemented, unbuilt (2026-10-04)

PlayerSettings assigns the existing street-logo texture as default and all12Android single-layer icons. New Editor-only StreetAppIcon Gradle callback copies that same PNG byte-for-byte and composes a cream adaptive icon with percentage padding/aspect preservation, avoiding cropped lettering or generated/redrawn art. No source logo/meta, intro/gameplay, package/version/signing/input or earlier changes modified. New script meta has a fresh GUID.

**Validation:** Source/diff review only; Unity MCP readiness read failed ping. No tests, Play, APK/export, device change or verified Unity compilation/import/callback run. Phone still has prior installed0.2.4/icon; verify branding with the later combined build/install.


## First-level1000goal / gold coin / corner HUD — implemented, untested (2026-10-04)

Floresta default/fallback/Main goal changed24→1000, deadline-only flagtrue→false, user-confirmed180seconds unchanged. Early first-level threshold handoff stops remaining workers in that slice, preserving actual unit/price accounting and freezing winning counter at1000/1000. Other goals/rules unaffected.

Built-in ImageGen produced a new original transparent1254×1254coin; after user rejected colored accents, a scoped gold-only edit preserved choripán/paty relief. Final `street-coin-gold-v2.png` copied intact with fresh meta/full bounds, NPOT/mips off and AndroidRGBA32; draft/output retained, exact prompts/hash in GenerationPrompts.md. StreetView loads it for HUD/income effects; moves coin balance top-left and sold/goal top-right in slim panels above rear orders, leaving timer lower and all interaction/queue anchors unchanged. Current gameplay summary updated to authoritative costs/FIFO/goal without changing those earlier systems.

**Validation:** Static image/source/diff review only. No tests, Play, APK/export/device work or verified Unity import/compilation; new goal/balance,1000-unit win/deadline loss, corner readability and gold icon appearance remain pending combined checks. Old24-unit/deadline-only fixtures need adaptation; no test files changed. Earlier app-icon/menu/queue/cost/reset/art/idle work preserved; no package/version/signing change, commit or push.


## Combined validation checkpoint — 2026-10-04
Latest user now authorizes combined tests/APK/phone update. Active checkout remains`/Users/celestino/HayChoriYPaty`, Unity6000.6.3f1/Android/Main. Updated Street fixtures and targeted EditMode52succeeded/no failures + PlayMode12/12passed; relevant evidence in`features/street-automation.md`. Reviewed actual menu/crowd/HUD and paired idle animation captures in ignored`Logs/Acceptance/Combined`; exact editor save restored, Play stopped. Android0.2.5/code7 development Main-only build`build-47e1c59fff` succeeded277.796s/0errors/3warnings. APK114272726bytes/SHA`99d78efd142a624e612259737664ab013a0667e41d8319a62e6733b3d296403c`, V2same debug certificate, min26/target36/ARM64; original adaptive logo bytes verified. RollingAPK plus0.2.5-code7 archive preserved, older0.2.4 archive untouched. Seven-product Ready/Playing layout also reviewed and exact editor save restored. Motorola serial`ZY22MBNWRB` reconnected under user request; **0.2.5/code7 install-r Success**, PackageManager verified, exact dedicated playerprefs XML unchanged and first-install timestamp retained. Own GameActivity cold launchStatusok/777ms/PID15213 confirmed. USB disconnected again before runtime-log follow-up; that waiting command cancelled. NativeSalir/launcher/touch/performance remain unverified, not installation blockers. No data wipe or rebuild. Evidence`Logs/Acceptance/Combined/summary.json`, full Android delivery in`features/android-prototype.md`.


## Full-screen Parrillero presentation — 2026-10-04
User's0.2.5phone screenshot exposed intro letterboxing and mismatched cover face. StreetView now renders versioned no-hero backdrop across physical screen with proportional crop; exact existing gameplay/hire Parrillero portrait is drawn as foreground, original logo and blue menu stay on unchanged safe-area/input canvas. Black/shade/flash layers also cover full screen;4+4second timing/gameplay unchanged. Asset`street-cover-no-hero-v2.png` +freshmeta/provenance retained, old cover/portrait/logo/metas untouched. FocusedStreetArt17/17 andStreetPointer13/13passed; actual1080×1920and1200×2670menus reviewed in ignored`Logs/Acceptance/IntroFullScreen`. Editor progress and view selection restored, temporary tall QA preset removed, Main stopped. User chose **project only**, explicitly no new APK/install; installed0.2.5 stays old. Source/docs/art changes remain local/uncommitted; GitHub's1b2b99b does not yet include this refinement.


## Round-local coin balance — source update (2026-10-04)
All five levels now discard coins on Start/selection/next level/retry/load. Current-round delivery income/spending, unlocked locations, prices and prior staff/speed rules are preserved. Saved legacy balances are ignored without a save-version change. Focused simulation/lifecycle fixtures updated; static diff check passed. UnityMCP readiness ping failed, so compilation and tests are **unverified**, not passed. No APK/device change or commit/push. See the scoped acceptance in features/street-automation.md.


## Goal200 and progressive purchase tables — source update (2026-10-04)
Floresta goal is200actualchoripanes/180seconds, with $5 price and zero coins per attempt retained. All locations share the specified nine +0.1 speed tiers (ending x1.9) and four hires (ending five total staff); next costs advance after purchase, exhausted cards show disabled MAX, and legacy out-of-range purchases clamp to the caps. Runtime/default/fallback/Main settings agree in a read-only static parity check; git diff check is clean. Updated focused simulation/pointer fixtures are not executed: UnityMCP readiness fails, so compilation/import/tests/Game-view/device behavior remain unverified. No APK/install/editor restart/commit/push. Exact prices and acceptance are in features/street-automation.md.


## Unified top coin/sales/time HUD — implemented, engine validation pending (2026-10-04)

StreetView replaces old blue/cream corner panels with one original procedural amber/brown glossy bar: gold-only coin left, clock/countdown center, actual Delivered/Goal green capsule right. Bundled Luckiest Guy white outlined numbers auto-fit long values. All artwork and text remain logicaly2–36, above rear bubblesy38; lower Tiempo label removed, simulation rules/anchors and upgrade buttons untouched. Texture created once per enable/released on disable. Focused art fixtures cover HUD separation/bounds, six countdown cases and texture dimensions/disposal. Static source/geometry checks passed; Unity6000.6.3f1 MCP state ping unanswered, so compilation/tests/Game-view/device unverified. No APK/build/install/git.


## Recognizable real Floresta mural / All Boys corner — integrated, engine review pending (2026-10-04)

Original built-in ImageGen edits use new user photos to depict real mural composition (black bands/windows/player rows/team/buildings/justice portraits/FLORESTA POR JUSTICIA), plus corner AB blue oval, shield/monograms and sneaker memorial. Final940×1673RGB plate retained as Resources/street-mural-real-v5.png with fresh inherited meta. Both full-plate outputs moved counter/panel about80px, so only source wall(0,0,940,234) is rendered into logical(0,0,540,90) above retained v4 background; all lower scenery pixels/anchors and unified HUD/upgrades/economy untouched. Existing art/metas retained. PNG/hash/metas/resource/crop bounds/static diff checks pass; 2 focused asset/layer fixture cases added, not executed. Unity6000.6.3f1 MCP transport unavailable, so import/compilation/Game-view/device pending. No APK/git.


## Floresta location font + All Boys shield — integrated, Unity pending (2026-10-04)

StreetView replaces Ready/Playing location labels with centered uppercase Luckiest Guy/white-dark-outline text; untouched club-hosted320×320RGBA crest at left only for level0. Later names auto-fit without wrong club branding. Playing footery886–924, Ready884–910 clears team stats and ready level buttonsy912; speed/hire/HUD/mural/gameplay unchanged. Fresh inherited meta, source/hash/provenance retained. Five focused cases added (asset dimensions/import,3layout cases,crest/font load), not executed. Static file/meta/layout/resource/diff checks pass; Unity6000.6.3f1 MCP transport unavailable, so import/compilation/tests/Game-view/bunting/readability unverified. No APK/git.


## Current combined validation and phone delivery — 2026-10-04 (0.2.6/code8)

Latest user authorized tests, APK and phone installation. Installed Unity6000.6.3f1 was closed (no running editor/MCP); used the exact installed editor in sequential batch runs, without new packages or a second editor. Current Street **82/82 EditMode + 15/15 PlayMode passed**,0failed/0skipped. Scope includes200-handoff victory/180s deadline, FIFO/reservations, round-local coins and first-level reset, exact progressive prices/caps/MAX, mouse/touch dispatch/start/replay/load, intro timing/asset/viewport guards, current art/HUD/mural/footer imports and layout. Not an unrelated legacy full-suite run or mobile performance certification.

Initial batch pointer runs failed due to unfocused simulated input; swapping transient InputSettings also destroyed the previous instance. Final fixture only saves/restores backgroundBehavior and editorInputBehaviorInPlayMode enum values on the existing settings (IgnoreFocus/AllDeviceInputAlwaysGoesToGameView during tests). No runtime input code or persistent input settings changed. Failed attempts remain in ignored evidence; final15/15 is the successful rerun.

Main-only development Android IL2CPP/ARM64 build **Succeeded340.915261s,0errors/1warning** (Diagnostics Data debug-symbol recommendation). APK **0.2.6/code8**,150547329bytes,min26/target36,packagecom.haychoriypaty.game; V2signature/same debug certificate and original packaged adaptive-logo PNG bytes verified. SHA256`e2cc6576dfbe07e126c8c4f8493ae79640e33b878fd0c3cbd4cef2a825bbe6d5`. Archive`Builds/Android/Archive/HayChoriYPaty-0.2.6-code8.apk`; rolling`Builds/Android/HayChoriYPaty-street.apk` updated after verification; previous archives preserved. Temporary editor build helper/meta removed after saving ignored evidence.

Authorized Motorola Edge60Fusion (API36/1220×2712) replacement install returned **Success**; PackageManager confirms0.2.6/code8. Dedicated playerprefs XML byte-identical before/after install, firstInstallTime preserved; no uninstall/data clear. Own GameActivity cold launch **Statusok/733ms**. Reviewed two native gameplay captures during user play: unified gold-coin/m:ss/sales HUD goal200, recognizable mural/corner behind crowd, pan francés and no floating grill badge, current Parrillero and comic Floresta caption/original crest. The captures show different worker poses and a subsequent fresh first-level round withteam1/×1.00 and initial25/200costs; these are observations, not controlled native tap/reset/accounting tests. User was actively playing/retrying, so no forced restart/quit for additional review. Native gameplay letterboxing remains intentional; full-screen change applies to presentation only.

Own-PID log snapshot contains no matchedFATAL EXCEPTION/NullReferenceException/MissingReferenceException/IndexOutOfRangeException/AndroidJavaException; this is bounded evidence, not an all-session zero-error guarantee. Native intro timing/Salir/launcher masks, device FPS/thermal and broader cutouts remain unverified. Unselected upgrade-card/speed-icon proposals remain unintegrated (existing shoe/card still visible). Evidence stays ignored in`Logs/Acceptance/Current/`: final editmode.xml/playmode.xml, build.json/log, signature/badging, install/package/launch, preferences snapshots and phone-runtime.txt. Both phone-menu.png and phone-result.png actually contain gameplay, not menu/result acceptance. No Git commit/push in this turn.


## Diagonal Parrillero gait — 2026-10-04

Added a separate generated RGBA 4×2 walk atlas for front-left/front-right and rear-left/rear-right diagonal travel, with two alternating steps for each direction. StreetView chooses its pair from the actual worker movement vector at the existing8fps cadence; original poses, scale/foot anchor and pickup/handoff frames remain untouched. Source artwork provenance/hash and exact prompt are in `Assets/Art/Street/ParrilleroGenerationPrompts.md`; old atlas/GUID preserved.

- [x] Unity import/native source size/alpha/platform format and selector pair/phase regressions passed.
- [ ] Actual Game-view motion/foot anchoring reviewed.
- [ ] New APK installed; current installed0.2.6/code8 omits this change.

**Validation:** Unity6000.6.3f1 `StreetArtTests` **35/35 passed**,0failed/0skipped, including new diagonal source texture and selector tests (`Logs/Acceptance/DiagonalWalk/editmode.xml`); no compile/import errors in the batch log. No PlayMode/device screenshot or APK build/install was performed for this source refinement.


## Parrilla Criolla upgrade buttons — source-only update (2026-10-04)

The user selected the wood-and-riveted-iron upgrade card option and requested forward arrows instead of the Speed shoe, with room for four-digit prices. Added reusable transparent card art and amber forward-chevron art with fresh `.meta` files; StreetView uses the existing Parrillero portrait, gold coin, live title/price/+10% and existing purchase controls. Cards are 234×120 and keep independent touch rectangles; no cost/economy edits. Asset provenance and acceptance were recorded in `GenerationPrompts.md`, `visual-reference.md`, and `features/street-automation.md`.

**Validation:** User then explicitly said not to test until asked. A Unity EditMode command had already been launched and aborted during test-assembly compilation with CS1061 (`GUIStyle.Dispose` unavailable in this editor); no test case executed. Removed that unsupported call, but per user instruction did not rerun. Unity import/layout, pointer, Game-view/device, and price-fit assertions therefore remain unverified. No APK/build/install, Git commit or push.


## Used trestle choripán table — source-only update (2026-10-04)

Generated and added `Resources/street-serving-table-v1.png` (1774×887 transparent cartoon asset) and a fresh no-mipmap/alpha/Android-RGBA32 Unity meta. `StreetView` places the loaded pan-francés choripán table in the near-left foreground beside the grill; product0 station coordinate now sends the Parrillero to the right edge of the table, and overlapping product-name text is suppressed. Later product stations, grill product art, costs, and delivery accounting remain unchanged. Prompt, references, and SHA are in `Assets/Art/Street/GenerationPrompts.md`.

**Validation:** User's instruction not to test remains in force. No Unity import/compiler/test/Game view run, no APK/build/phone install. The sprite was inspected directly; in-game scale/overlap and pickup animation are not visually verified.


## Floresta upgrade-cost playability — calibrated in Unity simulation (2026-10-04)

The old level-one curve yielded 66 sales without upgrades and 82 sales with affordable speed purchases; no parrillero hire became affordable by the deadline. Added a separate Floresta-only curve: speed costs $5, $10, $15, $20, $30, $45, $65, $90, $125 and hires $15, $30, $60, $100. Later locations keep their established tables; +10% per speed tier and x1.9 cap are unchanged. A deterministic real-handoff simulation using only earned revenue purchased all upgrades and won 200/200 in 98.9/180 seconds ($390 left).

Validation: Unity6000.6.3f1 `StreetSimulationTests` **50/50 passed** and focused `UpgradeCardsUpdateNextCostsAndStopAtMaximum` PlayMode test **1/1 passed**. The purchase-driven deterministic simulation won200/200 in98.9s with$390 remaining. No Android build, APK, or install was requested or performed.


## Full-body cover Parrillero — source update (2026-10-04)

Generated Assets/Art/Street/Resources/street-cover-parrillero-full-v1.png (1024×1536 RGBA) from the established portrait and gameplay sprite-sheet references. New art shows hair-to-shoes full body; sampled face/arm base medians match (RGB 246/150/83 vs 246/150/84). Unity draws it in a left-side portrait slot and shifts the same title badge right to keep the figure visible beside, rather than behind, the logo; Jugar/Salir hit areas and timing stay unchanged. Original game/hire portrait and gameplay sprite sheet are preserved. Fresh .meta GUID with no mipmaps/Android RGBA32; source hash and prompt are in Assets/Art/Street/ParrilleroGenerationPrompts.md.

**Validation:** Generated PNG was visually inspected; source dimensions and alpha confirmed, and git diff --check passed. No Unity import/Game-view/test/APK was run for this image change, so in-game composition remains unverified.


## Android delivery — 2026-10-04 (0.2.7/code9)

Built current Main-only source with Unity6000.6.3f1 Development Android IL2CPP/ARM64. Build succeeded in257.10s with0errors/1 warning (`clang++: '-x c++' after last input file has no effect`). APK `Builds/Android/Archive/HayChoriYPaty-0.2.7-code9.apk` (136,481,224bytes), package `com.haychoriypaty.game`, min26/target36, version0.2.7/code9; V2 signature uses same local Android Debug certificate as0.2.6. SHA-256 `92691a99f7a0c02eaed6930eb541aacf9f1e6ea25d9e6e2dfdfa3c120388313a`. Rolling `Builds/Android/HayChoriYPaty-street.apk` matches.

Installed to authorized Motorola Edge60 Fusion (`ZY22MBNWRB`) with `adb install -r`: Success; package query reports0.2.7/code9; PlayerPrefs XML is byte-identical before/after (no uninstall/data clear). Launched via Android package launcher; process remained present. No test suite or gameplay/visual/device acceptance test was run in this delivery; build success is not gameplay validation. No commit or push.


## Edge-to-edge Floresta / clear table route — source update (2026-10-04)

Implemented full physical-screen scenery beneath safe-area gameplay (fixes exposed gray top inset), physical-y0 coin/time/sales HUD with alternate camera-clear layout, and retained original safe control/hit transforms. First-level table nowleft(16,540,196,98), grillright(242,550,282,94), pickup(205,585) via clear(205,510) on both outgoing/return travel. Customer rows324/268/212 expose more wall and put front supporters closer to counter. Later multi-product station geometry, existing art/metas and actual economy/target/duration remain unchanged. Added/updated deferred geometry fixtures.

No tests or APK/install by standing instruction. Source diff/static review passed; Unity6000.6.3f1 batch import/compilation succeeded (exit0, no compiler errors). No PlayMode/Game-view/native visual review was performed. Native framing, camera readability, table pickup/gait and changed-route playability remain unverified. Installed0.2.7 does not contain this follow-up.


## Selected grill-scene presentation — source update (2026-10-04)

User-selected full-bleed grill illustration edited with built-in image_gen using the game Parrillero portrait/atlas as facial references. Natural face-neck transition and warm skin continuity visually inspected; opaque1024×1536RGB asset `street-cover-grill-parrillero-v3.png` integrated with fresh meta. StreetView retains physical-screen proportional crop, removes the obsolete separate cover cutout overlay, and recenters unchanged logo/flash. Menu actions/4+4 timing/gameplay/economy untouched; all earlier art/metas preserved. Deferred startup fixture source updated, not run.

No tests, Play/Game-view/device review, APK/install or Git push. Native phone appearance remains unverified; installed0.2.7 does not contain this change.

Unity6000.6.3f1 batch asset import/compilation completed successfully (exit0, no compiler errors), log `Logs/Acceptance/SelectedGrillCover20261004/compile.log`. This is not test execution or visual/device acceptance. `git diff --check` passed.


## Cover face/body rendering correction — source update (2026-10-04)

User pointed out v3's face style visibly differed from its body. Preserved v3 and authored v4 with a painterly target-style face/head/neck retaining game identity; integrated via the single fullscreen cover resource. Art visually inspected only; no tests or APK. Device visual and user's acceptance pending.
Unity6000.6.3f1 batch import/source compilation succeeded for v4 (exit0, no compiler errors), log `Logs/Acceptance/SelectedGrillCoverFaceMatch20261004/compile.log`. `git diff --check` passed. Tests, Play/Game-view/native device review and APK were not run/built.


## Active-game timeout riot — source update (2026-10-04)

The earlier riot vignette existed only in legacy `PrototypeView`, so it was never shown by Main/`StreetView`. Added versioned `street-riot-defeat-v1` full-screen scene with unmistakably angry shouting supporters, raised sticks and visibly shattered serving counter/tipped condiment stand. On actual `RoundPhase.Lost` below the goal, `StreetView` suppresses the calm playfield and displays this riot scene, physical HUD, rage headline, final sales/goal and replay. Background rumbles and nine wood debris pieces move using `Time.unscaledTime`, independently of frozen simulation. Victory and gameplay simulation/balance unchanged.

Unity6000.6.3f1 focused PlayMode test `StreetPointerTests.ReplayReturnsToReadyWithoutStartingAutomaticallyAndDiscardsCoins`: **1/1 passed**,0failed/0skipped. It reaches a short real deadline loss, verifies active riot texture/state plus moving debris, then retries to Ready with round-local coins reset. Final filtered batch compile/test succeeded. First run caught CS0103 stale `won` references in the now win-only result method; after fixing, headless test showed Lost-state initialization was incorrectly coupled to OnGUI, now tracked in `LateUpdate`. `git diff --check` on changed paths is clean. No full-suite run, actual Game-view/device review, APK/install, version bump or push.


## User-selected startup cover — source update (2026-10-05)

Copied the user's 941×1672 cover image byte-for-byte to `street-cover-user-v5.png` (SHA-256 `e7bc3d59b722432c53da3eb63a6bfd3e18dd5ebd22ab607af097d4b52b791462`), created fresh Unity texture metadata, and switched `StreetView`'s intro resource to it. The original transparent logo, 4+4-second sequence, menu buttons, and gameplay artwork/logic remain unchanged.

Unity import/Game-view/device review and tests were not run; no APK was built or installed.


## Floresta supporter clothing alignment correction — 2026-10-05

Historical checkpoint: nine standalone shirt images were once stretched over the old base bodies and looked superimposed. That overlay was disabled, then replaced by the complete nine-outfit front/walking atlases described in the current entry above.

Unity batch import/compile completed and the focused EditMode case passed 1/1. Game-view/device visual review remains pending; no APK/device update.

## All Boys riot apparel consistency — source update (2026-10-05)

Added a dedicated transparent nine-outfit timeout atlas with paired raised-stick/swing poses. The riot renderer chooses outfits with the same customer-ID mapping as the ordinary Floresta crowd, so a supporter keeps the same integrated black/white/neutral clothing when the timeout sequence turns into a trifulca. The Nueva Chicago-specific riot atlas and fallback behavior remain unchanged.

**Validation:** Static source/asset metadata review only. No tests, Unity/Game view, APK, or device operations were run, per the user's standing instruction.

## Nueva Chicago riot wardrobe consistency — source update (2026-10-05)

Replaced Level 2's four-look riot sprites at runtime with a new versioned 4×4 transparent atlas: all eight complete Chicago supporter outfits now have raised-stick and swing poses. The same customer-ID variant drives normal and riot rendering, matching the All Boys rule. Recorded a general project requirement that every future team's waiting-crowd and trifulca sprites preserve the same team-specific outfit. The prior Chicago atlas is preserved.

**Validation:** Static source/asset metadata review only. No tests or Unity/Game-view/APK/device operations, per the user's standing no-test instruction.


## Cartoon impact smoke on the trifulca transition — source update (2026-10-05)

Added a large transparent comic dust-cloud sprite that quickly expands over the frozen waiting queue at the instant timeout switches the active scene into the live riot, pulses briefly, and fades as the broken-stall background and furious team-outfitted fans come into view. This is visual-only, driven by unscaled time, and leaves the HUD/results drawn above it.

**Validation:** Static code/spec/import-metadata review only. No tests, Unity/Game-view/device review, APK, or installation, per the user's standing instruction.


## Trifulca smoke APK installed — 2026-10-05

Built Main-only Development Android IL2CPP/ARM64 APK 0.2.12/code14 with the newly added timeout smoke transition. Unity build `build-059ad48e84` succeeded in 273.46s (0 errors, 2 warnings). V2 signature verified; archive SHA-256 `524b63abbfb4ea413ab597282b798f743221329c2f2667c224884662de5da926`. The archive and rolling APK match.

Installed on Motorola Edge 60 Fusion `ZY22MBNWRB` using `adb install -r`; PackageManager confirms 0.2.12/code14. No uninstall/data clear, test suite, app launch or gameplay visual review.


## Global multi-product order window — 2026-10-06

The current live order model previously stored only `Product/Remaining` and one `SecondaryProduct/SecondaryRemaining`; the view switched between one and two independent bubble layouts. It now retains up to five ordered, distinct product lines with a reservation count per line while preserving the legacy one-/two-product API. Workers still reserve and finish every unit of an earlier line before starting the next. StreetView uses one shared, fixed-size two-line window: zero-quantity lines disappear immediately, following lines reveal in original order, and centered “•••” appears only while pending product types remain hidden (without displaying their count). Existing automatic generation and level balancing are unchanged. Focused model/layout and legacy-delivery tests were added. Unity6000.6.3f1 compilation and the complete EditMode suite passed 154/154; the complete PlayMode suite passed 23/23. These cover 1–5-line windows/reveals/count/order, bubble bounds, FIFO rows, reservations, worker delivery, coins and legacy Chicago combined orders. No actual Game-view or device screenshot was captured, and no APK was requested.

## Chicago role-specific customer ownership — implementation checkpoint (2026-10-06)

Removed the rotating global assignment cursor. New assignments now scan customer IDs oldest-first and require a settled front-row Waiting customer. Chicago workers have explicit Parrillero/Cocacolero roles, chosen to balance the current team without changing initial staffing, the five-worker cap, existing hire prices, speed, timer, goals or revenue. The first purchase after the initial Parrillero is a Cocacolero. Each `StreetOrderLine` has one `OwnerWorkerId`; a worker keeps that customer and product line through every reserved station/pickup/handoff trip, releasing the owner only when its complete specialty part is delivered. Chori and Coca lines on the same mixed Chicago order are independently owned and may progress concurrently. Other levels retain Parrillero handling and keep ordered multi-line tickets with their assigned worker.

The same `StreetView` worker flow loads original 4×4 Cocacolero art: red-apron idle, alternating walking, low bottle pickup and handoff poses. Nueva Chicago's existing blue barrel/product route is reused. Owner checks gate the paid handoff; timeout/departure cancels the reservation and owner before any income. Queue layout/compaction/tail admission/settling, station positions and entry/exit are unchanged.

**Focused validation:** Unity6000.6.3f1 EditMode `StreetSimulationTests` + one `StreetArtTests` case: **13/13 passed, zero failures or skips** (job `76d6ce108ef349fe90da4683f8ffcefb`). Covered Parrillero 4+2+1 order priority, Cocacolero 4+2 order priority, same-role non-sharing, mixed same-customer parallel assignment and final departure, unchanged hire table/cap, rear-row/role-ineligible idling, timeout reservation/owner release with no delivery or coins, general ordered-ticket ownership, existing Chicago counter boundary, overserve protection and the 16-frame atlas/import configuration. No full test suite, PlayMode tests, APK/build, phone/device operation, or in-game portrait screenshot was run. The generated atlas itself was inspected; integrated Game-view appearance remains unverified.

## Vélez / Liniers Level 3 — implementation checkpoint (2026-10-06)

Level 3 is configured as **Liniers - Velez Sarsfield** with stable catalog IDs Chori 0, Paty 1 and Coca 4. Its custom public-street background combines the three user-supplied mural cues; nine original full-body fan looks include prominent blue V/chevron shirts and matched walking/timeout outfits. One transparent two-zone grill shows chorizos and patties; the separate Coca barrel and existing red-apron Cocacolero are reused. The Paty sandwich icon is reused rather than adding a catalog item.

The Parrillero owns one customer's complete Chori+Paty portion (Chori before Paty); a Cocacolero independently owns Coca. Orders are generated from all seven non-empty product subsets, with independent quantities 1–4. FIFO/front-row assignment, parallel distinct roles, delayed final departure, and timeout reservation cleanup have focused regression source.

**Validation checkpoint:** git diff --check passes; a static check confirmed all seven referenced resource files/metas exist, Android RGBA32 is configured, and the new resource GUIDs are unique. Unity 6000.6.3f1 is registered, but the editor does not answer its state/console ping; the filtered EditMode request timed out before returning a test job. Therefore **no Level 3/Chicago/Floresta tests, script compilation, Unity asset import, or Game-view screenshot are claimed as executed**. Visual source assets were inspected directly, not in-game. No APK was built and no device was touched. Rerun focused Unity tests and Game-view review once the editor responds.

## Successful level transition resets upgrades — source update (2026-10-06)

Advancing after a win now resets the next level's team to one Parrillero and speed to x1.00 before `StreetGame` rebuilds/saves the ready simulation. The first hire/speed prices therefore come from the newly selected level's initial price rows. Coins continue to reset; unlocked progress and product prices remain. No other retry, economy or level behavior changed.

**Validation:** Static source/spec inspection only. No tests or Unity editor run, APK, or device operation, consistent with the user's instruction not to test until asked.


## Fixed $5 prices across all levels — source update (2026-10-06)

Removed the per-level product-price setup UI and all product tabs/slider/drag targets. Unlocked selector choices and Ready-state level navigation now start gameplay immediately. The simulation normalizes every catalog price to $5, ignores edits through legacy setter/save fields, and retains those serialized fields for compatibility; Floresta-only hire/speed discounts and its reset behavior remain keyed to level 0 rather than price editability. Updated focused regression sources and current gameplay/architecture/automation specs.

**Validation:** Static source/spec review and `git diff --check` only. No Unity tests, editor import, Game-view, APK or device validation was run, respecting the user's standing instruction not to test until asked.


## Enlarged digital clock in top HUD — source update (2026-10-06)

Expanded the timer label to use more of the center capsule, increased its timer-specific preferred font size, and shifted/shrank the analog face slightly left. The camera-safe field gains height while remaining below the centered cutout and within the bar. Time format, HUD artwork/height, coins, sales and game timing are unchanged. Added focused source assertions for timer bounds and camera clearance.

**Validation:** Static source review and `git diff --check` only. No tests, Unity import/Game-view, APK or device validation, per the standing no-test instruction.

## Cutout-phone countdown readability — source correction (2026-10-06)

The countdown field on tall cutout layouts now uses the full lower blue capsule (124×32 logical pixels at x202/y35), rather than the previous narrow 94×29 slot. It remains completely below the centered camera-clearance channel and inside the existing 68-pixel HUD; the non-cutout layout, clock face, time format and gameplay timing are unchanged. Added a focused geometry assertion and clarified the HUD spec.

**Validation:** `git diff --check` only. Tests, Unity import/compilation, Game-view/device review, APK build and phone install were not run, following the standing no-test preference.


## Defeat result copy and return navigation — 2026-10-06

The active riot result now states that the player did not complete the orders, animates a pulsing GAME OVER label underneath, and replaces Jugar de nuevo with Volver to the unlocked level selector. The focused PlayMode regression source was updated; tests and visual Unity/device review were not run.

## Levels 4–5 implementation checkpoint — 2026-10-06

Added cumulative Ferro and Independiente product maps, original club backgrounds and team outfit atlases, four-zone grill art, finished-sandwich table, beer barrel, Fernet preparation table, and specialist station/delivery routing. New EditMode regression sources cover product unlock/order, locked items, five-type tickets, automatic 1–4 quantities, FIFO/specialty exclusivity, mixed food/drink completion and station anchors; an art-resource test covers both backgrounds, matching fan poses and station assets. Level goals/timers and $5 economy are unchanged.

**Checks run:** `git diff --check` passed. A standard-library PNG/GUID/importer/source-map audit passed for 12 level art resource pairs; the new backgrounds, fan atlases, grill and drink stations were visually inspected as source images. After the user reloaded `Main.unity`, Unity 6000.6.3f1 refreshed/recompiled successfully; the console returned no errors or warnings. Focused EditMode coverage for Levels 4–5 passed **8/8, 0 failures/skips** (job `2b8c8abc78de4b8b988f4089ef3c1698`): product unlocks/order, locked items, five-type mixed tickets, FIFO/specialty ownership, random quantities/catalog, kitchen anchors, and art-resource integration. The Unity GameView was OS-focused before capturing and reviewing both live Play-mode screenshots: `Logs/Acceptance/Levels4-5-20261006/ferro-screen-capture.png` and `Logs/Acceptance/Levels4-5-20261006/independiente-screen-capture.png` (1080×1920 each). The blank camera-only captures were discarded; the reviewed ScreenCapture files show the integrated levels. Mobile visual validation remains unverified. Android APK 0.2.14/code16 was built successfully as `Builds/Android/Archive/HayChoriYPaty-0.2.14-code16-levels4-5.apk` (297,881,149 bytes; SHA-256 `de81a45d374f8db6545bca6e52ddc9f4fa1947615288e4e1777d0a578eaace1d`), signature verified, and copied to the rolling APK path; no phone install was requested. No commit or push was made. The diff also contains accumulated edits from earlier requested features, so it has not been staged or pushed under the task's clean/exclusive-diff condition.


## HUD countdown validation — 2026-10-06

The user explicitly requested tests and a screenshot. Unity 6000.6.3f1 refreshed/recompiled successfully. Focused EditMode jobs `cde423f6903b486c9dd075fedb37ccd0` (5 cases) and `50ae63c849f7408786dcd8b2e4fb7901` (8 cases) passed **13/13**, zero failures/skips: bar/camera-gap geometry, icon fit, time rounding/boundaries, and cached HUD texture creation/disposal. Ten runtime font-metric checks fit five timer strings in both layouts (normal fitted font 39px, cutout 24px).

Reviewed real Level 1 Game View capture `Logs/Acceptance/Clock-20261006/clock-level1-playing-1080x1920.png` at 1080×1920 and 0:40: enlarged digits stay inside the blue capsule, clear of the clock icon and other counters. This is the normal layout; no cutout-phone screenshot is claimed. QA stepped the live simulation to 80 seconds, froze it for capture and restored the original editor save and time scale afterward. No production code or balance changes were necessary after validation. `git diff --check` passed. No APK/device operation or full-suite testing was requested or run.


## Definitive Chicago layout and pickup routes — 2026-10-06

Implemented Coca barrel left, grill centered, finished-chori table right, with a common y686 ground line, 56px visible gaps and uniformly proportional 72% art scaling. Food Pickup/approach moves to (395,665)/(395,510); Coca to (155,648)/(155,510). Return trips use the same safe approaches. Reuse the Parrillero's side reach, fix Chicago cardinal flip at short distances and preserve its diagonal strides. Actual Game View revealed an existing Coca atlas row mismatch (walking selected idle poses); added a Chicago-only source crop table from the existing PNG's alpha components, excluding neighboring hair fragments. The original mappings and old right barrel layout remain unchanged outside Chicago. No new art assets or meta changes.

**Validation:** Unity 6000.6.3f1 refreshed/recompiled; final focused EditMode job `240c347e2ffd4dddb4b300259ce85fa6` passed **16/16**, zero failures/skips. Initial visually detected shoe/grill overlap was corrected and the clearance regression strengthened to account for the rendered workers, then tests/review repeated. 2,828 sampled routes across all seven columns and both products had zero crossings. An isolated 4+4 ticket produced 8 deliveries/$40, both workers Idle with assignments released. Reviewed four actual 1080×1920 live Game View snapshots with the ordinary 21-customer queue in `Logs/Acceptance/ChicagoLayout-20261006/`: `chicago-playing-final.png`, `chicago-coca-pickup-final.png`, `chicago-walking-right-final-1.png`, `chicago-handoff-final.png`. QA stepped/froze the live simulation and restored the original editor save/time scale; Play Mode is stopped. Gameplay outside station targets/routes is unchanged. `git diff --check` passed. No full suite, APK, device install or native screenshot was requested or run.

**Post-review tool limit:** after Play Mode stopped and the original save was restored (both confirmed), the final console-read ping did not answer on two attempts. Earlier compilation, the final 16/16 test job and all listed Game View captures completed successfully; no claim of an additional post-stop console inspection is made.

## Independent hiring HUD — completed 2026-10-06
- Speed / Parrillero / Cocacolero separate cards on Chicago and later drink-specialist levels; Floresta stays two cards. Each hire explicitly buys its role with independent count, price tier and cap. Configured tables retained; role caps inherit5 unless overridden. Composition saved/retried without role alternation; advancement baseline unchanged.
- Focused Unity validation:25 distinct EditMode and8 distinct PlayMode cases passed, including requested10 checks, touch/mouse edges, prices/caps, save/retry and retained service/routes. Jobs:92dae766f6ec4da7b4d25926fae8c105(17/17),124f0fe4e6314a0c9dd83739702f1f2f(6/6,4 repeated+2 tall checks),92be90b702cd47b1b3f4bb0808f8e90f(4/4),52862071d29f47d28bd052116aff7cae(4/4),00fe1803b95c44b6ab9c6ebeed4e8062(5 passed/1 old auto-hire fixture failed),2ac7bc2ae2654021bc8cc00b6e6230a2(corrected fixture1/1). No unresolved failure.
- Reviewed real Game View screenshots1080×1920 and1220×2712. Final images:Logs/Acceptance/IndependentHUD-20261006/chicago-mobile-playing-final.png, chicago-mobile-four-digits-max-final.png, floresta-mobile-two-cards-final.png. Temporarily isolated Game View from Device Simulator's mismatched screen metrics; editor windows restored. Original progress restored and Play stopped. No new art/metas, no APK/device install, no commit/push this task. Accumulated unrelated changes preserved.


## Nueva Chicago parrilla — four rows and scaled bread — 2026-10-06

Replaced the earlier Chicago-specific grill art with `street-parrilla-large-v4.png`: exactly four visible horizontal chorizo rows, one extra over the prior three-row sprite, and three short rolls scaled to about one chorizo each (the bread is not oversized). The grill keeps its centered, modestly wider footprint (174.24×59 logical px, x=182.88, bottom y=686) and the established station layout. The same lateral +8/−8 side-station and approach offsets keep 56.08px gaps and pickups adjacent to the ready table/barrel; the simulation ordering/rules are unchanged. New art is 2170×725 true-alpha RGBA, importer uncompressed/no mipmaps/no NPOT resize/Android RGBA32; SHA-256 `72b48c30a83665081f1a1cfe6241ba5c6f2e5e7f2b3d806966f84df08cd2b363`. The superseded v3 file and `.meta` remain intact.

**Focused validation:** Unity 6000.6.3f1 imported the v4 sprite (alpha and Android RGBA32 confirmed). `ChicagoLocationLoadsItsMuralCrestBottleAndBlueBarrelWithSidePickupLayout` and `ChicagoBothSpecialistsReachNewSidePickupsAndReturnWithoutCrossingProps` passed **2/2** in EditMode, job `7921da557e0c4075a742b33227e546ef`. Reviewed the actual 1080×1920 Play-mode capture at `Logs/Acceptance/ChicagoGrill-20261006/chicago-four-row-grill-review.png`: four rows and proportional rolls are visible; route regression checks both specialists' pickup/carry/handoff without crossing the expanded grill. Full suite, APK and device installation were not run/requested. Original player-save JSON and `Time.timeScale=1` were restored after capture.

## Cover-standard ordinary buttons — completed 2026-10-06

Inspected the actual Main/StreetView architecture via Unity MCP: no prefab assets, no uGUI Button components or AudioSources. Consolidated the existing cover renderer into DrawStandardButton, reusing its single generated texture and bundled Luckiest Guy. GAME OVER VOLVER, victory SALIR and Ready numbered level buttons were migrated; cover JUGAR/SALIR, selector VOLVER and Ready JUGAR already matched and now call the same helper. Actions/hit bounds/locks unchanged. No new art, metas, scenes or core/gameplay changes. Future-button rule recorded in AGENTS.md.

**Audit exceptions:** Velocidad/Parrillero/Cocacolero wood-and-iron live-stat cards and mural selector tiles retain explicitly selected special designs. Inactive historical PrototypeView controls were not migrated; unrelated prototype scenes stay untouched. No other generic button renderer remains in active StreetView.

**Focused validation:** final EditMode job d48158f420e645369362c5cc95dfedb0 passed **18/18**, final PlayMode job 1dfe70070b964e399a6adfb5ad16c073 passed **7/7**, zero failures/skips. Cover factory/font, end-cap geometry, font fit/style isolation, normal/pressed/disabled/label-hover colors, responsive exit frame, cover/start gating, locked navigation and mouse/touch result routes were checked. Earlier runs passed 16/16 and 7/7; final results supersede those counts. An intermediate misplaced test insertion caused CS0116 and was corrected before these final successful runs.

Reviewed actual Game View screenshots at1080×1920 and1220×2712 in ignored Logs/Acceptance/StandardButtons-20261006: accepted-cover.png, accepted-ready.png (disabled locks), accepted-gameover.png, accepted-victory.png, accepted-victory-pressed.png and accepted-gameover-tall-pressed.png. Original cover normal button regions compare with **0 differing pixels**. Initial fractional slice seams were corrected with subpixel edge overlap; inherited GUI.Label hover colors were isolated so they cannot erase caption outlines or popup summary labels. The original victory summary outline/color/metrics remain, while only its exit face uses blue. Final live console read returned **0 errors/warnings**; transient MCP WebSocket warning during reload is tooling-only. Original player prefs/time scale, GameView selection and clean Main scene restored; Play stopped. git diff --check passed. No full suite, native device run, APK, commit or push requested/run. Earlier pennant/adaptive-order edits preserved.


## Floresta-reference workstation standard — completed 2026-10-06

Inspected the actual running Main/Floresta via Unity MCP (no workstation prefabs, SpriteRenderers or physics colliders: IMGUI draw bounds are authoritative). Added StreetWorkstationLayout, preserving the measured grill282×94/table196×98 frames globally. Existing grill variants fit uniformly without distortion; exact Level1 table art is reused in all clubs. Repositioned proportional80-high Coke/beer barrels and94×66 Fernet prop in an upper work band, with shared station/worker-clearance geometry. All food pickup now uses the ready table. Existing simulation stages route through safe side approaches; correct authored walk/reach crops and near-station portrait presentation preserve reachable hands and free feet. Final visual review moved drink pickups closer to rims without changing delay/speed/rules. No source art, scene or serialized asset changed; only the new source has a generated Unity.meta. Economics, orders/FIFO, goals/durations, employee counts/upgrade tables, backgrounds and previous UI edits remain unchanged. Future levels inherit these fixed furniture frames; extra secondary placements must pass shared clearance validation.

**Validation:** final focused EditMode job `7d892eaa2be94f06b125c86d04f359ab` passed **29/29**, zero failures/skips (16.509s). Includes126 two-unit tickets across18 available product/level pairs and7counter columns, checking full90×98 actor+bob bounds against non-target props,36×12foot bounds against all props, Pickup/carry/Handoff/Idle, reservations/income; four tall hand-depth cases, art/layout tests and timeout/specialty assignment regressions. Earlier29/29 job654a7d3e89aa48f689d748c10afcf5cd preceded the final rim alignment. Intermediate failures were obsolete coordinate fixtures and were corrected before final successful jobs.

Reviewed actual Play/Game View at1080×1920 in every implemented level, with ordinary21-client queues and live deliveries, plus1220×2712 pickup reach for Coca/beer/Fernet and table circulation. Final evidence in ignored `Logs/Acceptance/StandardStations-20261006/`: `level1-final.png`, `level2-accepted.png` through `level5-accepted.png`, `chicago-coca-reach-tall-accepted.png`, `independiente-beer-reach-tall-accepted.png`, `independiente-fernet-reach-tall-accepted.png`, `independiente-coca-handoff-accepted.png`. Files named only `*-pickup-final`/earlier drink `*-final` are intermediate iterations, not final rim-alignment evidence.

Final live and post-stop console reads: **0 errors/warnings**. Exact original PlayerPrefs JSON restored, Time.timeScale1, GameView19, Play stopped and Main scene clean confirmed. `git diff --check` passed. No full suite, formal PlayMode test job, physical device validation, APK, commit or push requested/run. Station tests validate prop circulation, not a new employee-vs-employee traffic/physics system; existing same-station crowd behavior is unchanged.


## Clean lower Game Over result — completed 2026-10-06

Removed the lower objective-summary text and the gray translucent panel that framed it from `StreetView.DrawRiotResult`. The existing VOLVER control stays at its proven position/hit bounds and keeps its selector/reset action. There is no uGUI popup hierarchy, Text/TMP component, background Image, Layout Group or Content Size Fitter: `Main` contains a single StreetView IMGUI host, and the block consisted only of draw calls; deleting them removes the content/panel without hidden reserved layout space. Upper gameplay counters, message, animated GAME OVER, riot art, phase behavior and navigation are unchanged.

**Validation:** focused PlayMode `RiotReturnOpensLevelSelectorWithoutStartingAutomaticallyAndDiscardsCoins` passed1/1 (`717cb1599124450fba48f3ddf6269e91`). Actual Game View in `Logs/Acceptance/GameOverBottom-20261006/gameover-clean-bottom-1.png` shows the crowd/trifulca, unchanged top message/counters, and only the VOLVER button at bottom; lower gray panel/summary are absent. Final Unity console0 errors/warnings. Restored exact prior PlayerPrefs, timeScale1, GameView19; Play stopped and Main scene clean. `git diff --check` passed. No APK/device install requested.


## Temporary shared upgrade economy — active test profile (2026-10-06)

Inspected the live Unity project (/Users/celestino/HayChoriYPaty, Unity 6000.6.3f1) rather than relying on the pasted level list. The current runtime catalog has five entries: Floresta / All Boys, Nueva Chicago, Liniers - Velez Sarsfield, Ferro Carril Oeste and Independiente de Avellaneda; it does not contain Argentinos Juniors. Added shared StreetUpgradeCostProfile data and level-index mappings in StreetBalance. All five Main scene mappings are profile 0; default code and serialized Main values match. Runtime Parrillero and Cocacolero prices use the same 15/30/60/100 table (the starting Parrillero remains free, and the first separately hired Cocacolero begins at $15); speed uses 5/10/15/20/30/45/65/90/125 for nine +10% upgrades through ×1.90. Older cost fields remain hidden only for serialized compatibility and are ignored at runtime. No price persistence/reset rules or unrelated balance/gameplay settings were changed.

A per-level profile override is covered: a level can be assigned an independent data profile without altering pricing code. Missing NUnit test annotations were restored on the all-five-level curve, legacy-save clamp, and next-level reset regressions; the Cocacolero max test also proves a MAX hire cannot be free or debit coins.

**Focused Unity validation:** EditMode EveryLevelUsesExactUpgradeTablesAndStopsAtLastTier (levels 0–4), CocacoleroUsesTheSharedHireCurveFromZeroToMax (levels 1–4), and LevelsSelectUpgradeProfilesWithoutReadingLegacyTables, MissingTablesUseDefaultsAndOlderPurchasesClampToLastTier, and WinUnlocksNextLevelAndCreatesReadyNextRound passed 12/12 in final job d6ff6405775d47d6babb450e50900db0. PlayMode UpgradeCardsUpdateNextCostsAndStopAtMaximum passed 1/1 in job 053f52aa6ace48f08567fd036cea9ffa. The passing Unity test jobs compiled the active scripts. An earlier filtered console read showed no errors/warnings; the final post-test console ping did not respond, so no later console status is claimed. git diff --check passed. The broad StreetSimulation test fixture was not rerun as the focused tests passed; its previously observed unrelated failures were not part of this change. No full suite, APK, phone install, commit or push was requested or performed.

## Catalog-driven kitchen visibility/layout refinement — 2026-10-08

The Velez gameplay screenshot showed locked station shells despite product gating. The refinement derives furniture visibility and all station geometry from the active level catalog, reflows only active stations, preserves route/pickup/slot alignment, and keeps product profiles and cooking/economy timers unchanged. All tables remain equal size; barrels are20% larger; lone grills center/enlarge within the lower UI band, paired grills remain aligned. Renderer shows Raw/Cooked/Passed only, with internal Cooking timing preserved; ambient heat/smoke use at most three reused sprite objects per active grill. After a live Level3 GameView exposed that changing levels left the derived layout cached at construction, `SelectLevel` and `NextLevel` now rebuild that layout. Focused EditMode job `b0ea015d1bcf4a5398284bc7fe2a835e` passed6/6. Portrait GameViews for levels1,3,4,5 were captured under ignored `Logs/Acceptance/StationRefinement/` and visually checked; Level3 now shows the Coca barrel. Unity6000.6.3f1 compilation completed without remaining console errors/warnings; no product catalog, worker role, save, timer, or economy rules were changed.


## 2026-10-08 floor-safe grill placement

Corrected the portrait kitchen composition after the Vélez Game View showed grill sprites crossing onto the pale lower field. Preparation tables/barrels move up54 logical units; paired grills remain standard-size and derive their shared y from the active prep row; a solo grill still scales only within the reserved safe bottom. Grill bounds now end at or before y=660, above the visible field transition; pickup targets shift with the prep row. Product catalogs, worker responsibilities, prices, timers, orders, saves and difficulty are unchanged. Focused EditMode layout/route tests passed3/3, covering all11 catalog profiles and pickup-route clearance. Live 1080×1920 Game Views for Vélez(Level3) and Independiente(Level5) were visually inspected: the single and paired grills end above the pale lower field, with paired grills aligned on one baseline.


## Responsive audit corrections — 2026-10-09

Implemented the 12 Unity audit recommendations without changing level balance or product rules: themed pennant mask, cover-cropped mural bands and original NPOT texture dimensions, tablet card/pagination spacing, compact queue bubbles, unified custom hit bounds, safe-area HUD, single backdrop composition, responsive station/item vertical projection, clearer victory labels, and defeat panel below HUD. Also finished the previously requested worker beverage pickup reach and 40% slower distance-driven gait in this checkout. Focused `StreetArtTests` ran 8/8 (captured in Unity `TestResults.xml`); Unity MCP's job tracker timed out after the successful report was written. APK build `build-8d0c922186` succeeded (0 errors/1 warning), with package and signature/archive verified. APK: `/tmp/HayChoriYPaty-0.2.20-code22-responsive.apk`, SHA-256 `6609a746936d7795e25185aaa8d617acc3a1bcbbdfc96dc29bf88c1201f5b069`. No physical phone validation/install.


## Responsive gameplay regions and single counter — source update (2026-10-10)

Root cause: Main has one counter baked into the shared background. StreetView drew that plate full-bleed, then re-drew its counter crop inside the inset safe canvas; the distinct origins/scales misaligned those two projections into apparent parallel counters. The HUD likewise began at the safe viewport's top rather than physical y=0. Upgrade rows used fixed authored Y values while the pennant strip stayed pinned to the screen bottom, so the 4/5-action cards crossed the flags and could clip on short viewports.

The counter is now rendered only from the backdrop, with crowd occlusion projected into the same safe viewport. The HUD uses a physical top-edge matrix and a transparent/reflowed quota-chip channel when Unity reports a top cutout. A shared footer calculation keeps level name, catalog-derived cards, optional Ready selector and aspect-preserved flags between the playfield bottom and safe viewport edge. No simulation, sales, targets, costs, timing, progression, sprite art or control actions changed.

**Unity verification (2026-10-10):** Unity 6000.6.3f1 compiled and ran the focused `StreetArtTests` class in an isolated copy because the original checkout was already open in another Unity instance. All **11/11 new responsive geometry cases passed**: single-counter projection and all 11 level catalogs at 1080×1920, 1080×2340, 1080×2400, 1440×3200 and 1536×2048, plus the reported top-cutout HUD gap. The full class was **144/145**; its sole unrelated failure is the unchanged `SevenQuotaResultsFitCompactlyAndDefeatListsOnlyPendingProducts` assertion expecting `VACÍO` while the current summary emits the abbreviation `VAC`. No game/sales logic was changed to mask it.

**Android verification:** Main-only Unity 6000.6.3f1 Development IL2CPP/ARM64 build succeeded with 0 errors and 2 warnings; APK archive, package/version/SDK metadata and v2 debug signature verified. Delivery details and SHA-256 are in [android-prototype.md](features/android-prototype.md).

**Still unverified:** No Unity Game-view/device screenshot or physical-device run was available, so the in-game appearance has not been visually confirmed. The attached obelisk screenshot does not match the current v4 runtime background resource.


## Victory popup regression — source fix (2026-10-10)

The responsive footer reserves no upgrade-card bounds during terminal phases, but the OnGUI upgrade loop previously still iterated catalog actions after a win. That indexed the empty footer array and threw before ResultPanel() ran. Upgrade cards now draw only while Playing, or in Ready when the catalog fits the single row; Won skips directly to the result panel and Lost keeps its dedicated riot path. Added a focused EditMode regression matrix for Playing/Ready/Won/Lost and compact/large catalogs.

Validation: git diff --check passed. The Main-only Android build of this source as 0.2.21/code23 succeeded with 0 build errors; the APK was integrity/package/signature-verified. Focused Unity 6000.6.3f1 EditMode checks passed 21/21 on an isolated copy of commit cec7d36: 6 upgrade-draw guard cases, 5 responsive-footer resolutions covering all 11 catalogs, and 10 popup frame/text/time cases. Full EditMode/PlayMode suites and visual/Game View/device verification remain unrun; no device install was done.


## Git and Android delivery — 2026-10-10 (0.2.21/code23)

Committed and pushed the victory-popup regression fix on codex/street-automation. Main-only Unity 6000.6.3f1 Development Android IL2CPP/ARM64 build succeeded (0 errors). APK: Builds/Android/Archive/HayChoriYPaty-0.2.21-code23-victory-popup.apk, 402,286,236 bytes, SHA-256 b3ced92c175ff47ff23b3767e37d8a4cfac3886c06232ed9a6bc7623be128ace. Package/version/min/target: com.haychoriypaty.game, 0.2.21/code23, API26/36. Archive integrity and v2 debug signature verified. No EditMode/PlayMode run, phone installation, or visual review was requested or performed.


## Barrel diameter follow-up — 2026-10-10

Expanded both barrel bounds/art from72×117.6 to82.8×117.6 authored units (+15% from current width, height unchanged). The shared catalog layout automatically reduces its equal upper-row gutters while keeping them positive in the full five-station layout; station and worker-route footprints continue to use the same bounds. No stock, worker, route policy, production or serving logic changed. Focused Unity 6000.6.3f1 tests passed3/3 EditMode (11-level responsive geometry, drink scatter, all-catalog paths) and1/1 PlayMode (Coca/beer sprite bounds match the new width and unchanged height). Tests ran on an isolated candidate copy due the open editor; no fresh visual/device capture.

## Barrel beverage visual scale (2026-10-10)

- [x] Increased the on-barrel Coca/beer display targets uniformly by 35%; carried drinks and gameplay data remain unchanged.
- [x] Focused full-catalog renderer PlayMode test passed 1/1 in an isolated Unity 6000.6.3f1 project copy.
- [ ] Current Unity Game View screenshot/visual review remains unverified; the shared preview is illustrative, not a capture from the modified runtime.
- [x] Development APK generated as 0.2.22/code24 (Main-only, IL2CPP/ARM64); signature, package, SDK metadata and ZIP integrity verified. No device install or runtime screenshot.


## Additional barrel width — source updated (2026-10-12)

Increased the existing 82.8-unit barrel width by another 10% to91.08 authored units; height remains117.6. The maximum five-station row's equal authored gutter becomes−1.69 units (a small bounds overlap), and the focused eleven-level layout regression now caps that at2 units. `git diff --check` passed; Unity EditMode/PlayMode and Game View were not run because the existing editor is open and Unity MCP is unavailable. No gameplay, pickup, stock or beverage-size behavior changed.


## Mural header gap — source fix, visual confirmation pending (2026-10-11)

`StreetView.DrawThemedMural` previously painted a theme-tinted solid rectangle over the scenery above every themed mural, causing a flat empty band behind the top bar. Replaced that fill with an aspect-preserved draw of the matching level backdrop’s upper strip, or directly adjacent mural pixels when that texture is already the full backdrop. River Plate receives an independent skyline strip because its mural begins at the texture’s top edge. The original mural bands remain in place and the HUD still draws above them; no existing mural pixels/files, game rules, UI controls, or animations changed.

`git diff --check` and static source/aspect calculations across all 11 level backdrops plus the River extension, at seven portrait/tablet sizes, completed. The Camioneros before/after preview is composited offline from the supplied screenshot using the same source crop; it is not a Unity capture. Unity MCP/editor visual capture was unavailable, so runtime appearance remains unverified. New files: `street-river-header-extension-v1.png` and its `.meta`; import references are in StreetView.


## Two-row mobile HUD — Android delivery (2026-10-12)

Added a two-row gameplay header: coins left, timer centered, dynamic level badge right, and objective chips using one row for up to four goals or two rows for five/seven goals. The cutout-aware header expands only as needed; all values remain read-only. Focused Unity 6000.6.3f1 EditMode checks passed 9/9; `git diff --check` passed. Main-only Android Development IL2CPP/ARM64 build `build-85da0aba77` succeeded (0 errors, 1 warning). APK `Builds/Android/Archive/HayChoriYPaty-0.2.24-code26-responsive-header.apk`, 474,896,975 bytes, SHA-256 `2502f389d90766c5755ed281804093e83caef806f3c549b332de113566ceec64`, package `com.haychoriypaty.game`, version0.2.24/code26, API26/36; archive and v2 debug signature verified. Unity Game View/device appearance remains unverified; no phone install was performed.


## Physical product proportions — previous build record (rolled back 2026-10-12)

- [x] Centralized render-only size targets for drinks, sandwich servings, and grill cuts; all catalog levels share the same per-product sizes.
- [x] Added a distinct transparent 2.5 L Coca shelf sprite and enlarged/rebalanced both bottle groups under the Fernet table without changing furniture, stock, or gameplay.
- [x] Aligned the product catalog spec with the current 800 cc disposable plastic Fernet cup.
- [x] Imported the pushed source (`a7937b2`) in an isolated temporary checkout with Unity 6000.6.3f1; Main-only Android Development IL2CPP/ARM64 build succeeded with 0 errors / 1 warning. APK: `Builds/Android/Archive/HayChoriYPaty-0.2.25-code27-product-proportions.apk`, 410,225,113 bytes, SHA-256 `08ec5d31581dae8a2ca341338780d6cd6be7b33f4cc44c9b0cc073426b1a1bf6`; package `com.haychoriypaty.game`, API26/36, version0.2.25/code27. ZIP alignment and v2 debug signature verified; rolling APK matches.
- [x] Product-size, serving, shelf overlap/aspect, grill-bound, and all-level layout assertions passed. Broader focused fixtures each also exposed one adjacent position assertion: EditMode `BeveragePickupsStandCloseToBarrelFrontWithSafeFootClearanceAcrossCatalog` (L4 P6 expected 508.846, actual 495); PlayMode `TallProjectionKeepsMeatOnItsGrillAndConsumesTheIndividualCarriedObject` (anchor y expected -1.35877, actual -1.39649). These are not size assertions and their implicated placement code was not changed; they remain pending diagnosis.
- [ ] Live Unity Game View / phone visual comparison remains unverified; this previous APK was superseded by the rollback build below.


## Product-size rollback (2026-10-12)

Restored the visual-size implementation from the parent of `a7937b2`: the prior sandwich/carry and grill-cut dimensions, plus Fernet/Coca shelf bottle heights at 25%/33.3% of table height. The 800 cc disposable plastic cup/product identity is preserved. Unity 6000.6.3f1 isolated-copy validation passed 13/13 focused checks: Fernet 800 cc plastic cup/lower shelf 1/1 EditMode, all-level grill meat bounds 1/1 EditMode, restored serving targets 10/10 PlayMode, and prior shelf proportions/no-overlap 1/1 PlayMode. Main-only Android Development IL2CPP/ARM64 build 0.2.26/code28 succeeded with 0 errors and 1 clang warning. APK: `Builds/Android/Archive/HayChoriYPaty-0.2.26-code28-product-size-rollback.apk`, 410,224,713 bytes, SHA-256 `97093346de768d4dd3a2d5c33190194eb90e36f8d17c941369f7aa69e2ae197a`; package `com.haychoriypaty.game`, API26/36, arm64-v8a. ZIP alignment and v2 debug signature verified. `adb install -r` succeeded on Motorola Edge 60 Fusion (`ZY22MBNWRB`); PackageManager confirms versionName 0.2.26/versionCode 28. No Unity Game View capture was taken.

## Fernet tabletop display and footer pennants (2026-10-12)

The Fernet table now shows two Branca and two Coca stock bottles on its tabletop's left side, with prepared Fernet cups reflowed to the right; Coca's render height is 20% below the previous target. Inventory and worker behavior are unchanged. The empty footer band was caused by a fixed 80-logical-unit pennant height; generated pennants now scale to the remaining band and refresh only when palette/height changes.

**Validation:** Unity 6000.6.3f1 isolated-copy focused EditMode checks passed 3/3 (Fernet presentation, footer inset, adaptive pennant texture); focused PlayMode renderer check passed 1/1. `git diff --check` passed. Unity MCP's live editor ping did not respond, so no Game View or device capture was available; visual runtime QA remains pending.

## Enlarged gameplay HUD and kitchen effects (2026-10-13)

Coins and level now stay at the header's top edge; only the centered clock moves below a detected camera cutout. Status/objective bands are 20% taller, their icons/counters are larger, and objective chips use translucent teal/green. Premium Bondiola/Vacío servings are 10% larger; barrel Coca/beer items are 8% larger. The existing ambient grill puffs now rise on a slow, faint 3.125 s cycle. No gameplay values or state changed.

**Validation:** Unity 6000.6.3f1 isolated-copy focused checks passed 10/10 EditMode (HUD geometry/notch/reflow/colors/texture and smoke profile math) and 11/11 PlayMode (premium serving sizes and barrel projection). `git diff --check` passed. Actual Game View/device appearance remains unverified: Unity MCP's editor-state ping did not answer, and the headless PlayMode smoke-motion attempt failed in URP's offscreen RenderTexture path. Smoke math passed EditMode; visual runtime QA is still pending.
## Fernet stock bottles grouped into two rows — 2026-10-13

The Coca/Branca display now has eight tabletop bottles (four of each) in two compact staggered rows. Tightened the row width and increased the horizontal stagger to make every bottle read separately while preserving visible-base contact on the tabletop; gameplay stock and prepared drinks are unchanged. Level 6 Game View capture reviewed at 2160×3840: `Logs/Acceptance/FernetTabletop-20261013/level6-after-two-stock-rows.png`.

**Validation:** focused `FernetPresentationUses800CcCupAndBottlesBesideFinishedDrinks` EditMode regression passed 1/1. `git diff --check` passed. Unity Console had no project errors or warnings after the run.
## Fernet tabletop extra stock row and visible contact — 2026-10-13

Kept four Coca and four Branca bottles grouped in two close rows, but increased their front/back separation on the tabletop to make the extra row read clearly. Both rows' visible bases remain aligned to the tabletop and use subtle contact shadows. Focused EditMode regression passed 1/1; Level 6 Game View capture reviewed at 2160×3840: `Logs/Acceptance/FernetTabletop-20261013/level6-after-two-stock-rows-2.png`. Android 0.2.29/code31 Main-only Development IL2CPP/ARM64 APK built successfully (0 errors/1 warning), metadata/archive/signature verified; details in `specs/features/android-prototype.md`.


## Mercado / persistent Heladera — 2026-10-13

Implemented the approved [market-fridge contract](features/market-fridge.md): ten ingredients, integer cents/four bottle portions, actual-goal requirements/shared bread, one-time kit/legacy migration, cash-only purchases, snapshot recovery, physical handoff consumption, stock/earned-sales settlement deduplication and one shared Mercado/Heladera screen. All Boys remains direct; shopping unlocks after its victory; levels2–11 enter Mercado, losses return to levels, final victory has no level12. Existing art, official prices, quotas, times, hires, routes and gameplay HUD remain unchanged.

Historical launch validation via live Unity6000.6.3f1 MCP: EditMode29/29 + final14/14; PlayMode37/37 + final7/7. The earlier default-seed campaign used the original minimum-stock financing policy and does not establish cash-only solvency. Nine real portrait Game View captures at540×960,540×1170,540×1200 under `Logs/Acceptance/MarketFridge-20261013/`; checked tabs, contrast, controls and bottle remainders. `git diff --check` passed. Physical Android QA/build/install not run. Current cash-only behavior is specified in the market-fridge feature contract.

## Supplier credit removal — focused validation (2026-10-13)

Purchases now require available Mercado cash; old v4 `debtCents` is ignored and dropped on the next save. Legacy credit/debt UI and settlement deductions were removed. The mercado list starts higher in the space released by the removed debt copy. Existing v4 balance, stock, migration, recipes, prices and rewards remain unchanged.

**Validation:** Unity 6000.6.3f1 focused EditMode: 15/15 passed, including cash-only atomic rejection, handoff/settlement, and v4 save migration. Six focused PlayMode functional cases passed across the filtered run and targeted reruns (including affordability UI, restock, stock-gated match and purchase debounce). GameView capture could not run in `-nographics` (`No graphic device is available to initialize the view`); Unity MCP was disconnected. `git diff --check` passed.

**Economic blocker:** under the default seeded campaign, Level 1 completes with $7.40 and Level 2 minimum stock costs $10.00. With financing removed, the player can spend remaining cash only (up to $2.40 of the shortage); insufficient purchases are rejected and `JUGAR PARTIDO` stays disabled. Prices, objectives and progression were intentionally not changed. No visual capture/device validation was completed.


## Dynamic Mercado objectives — UI layout (2026-10-13)

Replaced the multiline text summary with compact product-icon cards generated from the selected simulation's active products and actual goal quantities. 1–3 objectives fit one row; 4–7 use the requested two-row patterns. Header height grows only for the second row, the ingredient list remains the sole scroll area, and the fixed footer summarizes missing-stock cost/current-or-projected balance above its NIVELES/COMPRAR FALTANTES/JUGAR PARTIDO actions. This preserves the current no-fiado policy.

Validation: Unity 6000.6.3f1 focused PlayMode: 3/3 passed (MarketObjectiveCardsFitOneOrTwoRowsAndFollowLiveLevelData, SelectorTabsAndQuantitySelectionRequireExplicitDebouncedPurchase, PlayIsStockGatedCashOnlyRestockAndRetryIsGone). Layout bounds were checked for all counts 1–7 at 540×960, 540×1170, and 540×1200. git diff --check passed. Unity MCP is unavailable; captures/visual appearance remain unverified.

## Mercado visual redesign — 2026-10-13

Implemented the approved visual-reference layout in StreetView only: original wood/club art, large ingredient icons and summary coverage cards, one expanded purchase row, level-required ingredient list, dynamic1–7 objective cards, prominent cash and fixed green play CTA. At that stage the unused group was hidden; later single-screen integration supersedes that detail. Heladera retained its previous renderer/actions; domain recipes/prices/stock/economy/production remain unchanged.

## Unify Mercado and inventory UI — 2026-10-14

Removed the independent Heladera/Stock screen, switch button/tab and screen-switch state. Kept `StreetMarket` inventory/save/recipes/consumption/purchase logic; Mercado now presents required ingredients and a collapsed OTROS INGREDIENTES group for unlocked optional stock, with explicit purchase controls. The fixed footer combines missing-stock cost and COMPRAR FALTANTES while JUGAR PARTIDO remains stock-gated. All Boys starts with only Chorizo/Pan needed rows; later unlocked optional ingredients can be consulted and purchased. Current Unity focused validation and screenshots pending.

Validation attempt: Unity 6000.6.3f1 accepted a script refresh/compile request, returned to ready state, and the console query showed 0 errors/warnings. The focused PlayMode job then failed before starting any tests (`Test job failed to initialize (tests did not start within timeout)`); the editor MCP stopped answering pings afterward. Thus no PlayMode result or Game View capture is claimed. `git diff --check` passed; rerun the focused tests and visual capture when the editor session is responsive.

**Mercado bread icon correction — 2026-10-14:** The Chori-bread crop cut across its sloped lower crust. Both bread stock icons now use the existing complete upper-bun region from the Paty sandwich sprite (no source art edited). Source crop was visually inspected; Unity Game View retest/capture could not run because the MCP disconnected while starting the focused capture test.

Validation: Unity6000.6.3f1 MCP focused PlayMode **9/9 passed**, job `3bd96811f4fe44cba75d56ef01f63585`; actual level2/4/10 screenshots reviewed at540×960 (16:9),540×1170 (19.5:9),540×1200 (20:9), plus unused-group/expanded-purchase states. Evidence: ignored `Logs/Acceptance/MarketVisualRedesign-20261013/`; `git diff --check` passed. Dedicated crest assets still cover only levels1–3: later clubs show palette/initials identity tiles, not invented official crests. No new APK, push or device validation requested/performed.

## Nivel 1 stock inicial — 2026-10-14

New saves now get a one-time 10% reserve over All Boys' recipe-derived objective: 220 chorizos and 220 panes for the current target of 200. Existing Market inventories are preserved exactly; saves predating Mercado retain their prior compatibility migration. The observed 145/435 values are from persisted `market.stockUnits`, not scene defaults; per-ingredient historical transactions are unavailable to determine how that valid inventory accumulated.

Focused EditMode regression tests were added for formula/dynamic goal, persisted 145/435 preservation, and fresh-save lifecycle/defeat. Unity test execution is pending: filtered MCP runs reported zero tests and the assembly run timed out/disconnected; `git diff --check` passed.


## Cash-only campaign funding and zero-payout recovery — 2026-10-15

Root cause: the prior full economic simulation won Level 1 but ended with $7.40 against Level 2's $10.00 minimum missing-stock cost, so the cash-only gate correctly blocked the match. Separately, the affected phone save stored a successful Level 1 result with $0 recognized/earned/total, a paid watermark, a previously consumed migration-repair marker and $787.50 purchase history; the older repair path did not cover that state.

At a first-time unlock only, the victory transition now adds the exact shortfall to the next level's current minimum missing-stock cost. It is an earned, non-repayable progression reward, recorded separately on the last result; no purchase can exceed available cash, and replays/loss/final-level wins do not get it. Existing zero-payout v4 saves receive one narrowly scoped recovery, preserving inventory and transaction history with a separate idempotency marker. Receipt text includes the distinct restocking award.

**Validation:** Unity 6000.6.3f1 compiled the final code changes and the console returned 0 errors. Focused EditMode jobs passed: `3ce33e9afa7540f39d8121aaa7cde684` (3/3: full 11/11 actual-simulation campaign, replay award deduplication, old zero-payout save recovery and cash-only purchase gate) and `0fb698bfb03c4887ae74ab658ee11891` (6/6: settlement, cash-only rejection, loss/replay accounting, legacy migration/repair). The campaign logged wins in levels 1 through 11. A wider combined-filter attempt timed out before starting; the focused jobs above are the executed evidence. No new device/Game View QA was done. `git diff --check` passed. No APK, push or install was requested or done.


## Cocineros activos + Mozo desde Racing — 2026-10-15

- [x] Implementados circuito físico, cocción individual0.5/WorkRate, reservas/ID/stock, Mozo4 contratado desde nivel6, bebidas/anteriores conservados, seis cards y layout ampliado con assets originales.
- [x] Unity6000.6.3f1:71/71EditMode+7/7PlayMode; después35/35 y20/20 rechecks de las correcciones visuales. Sin errores/warnings finales.
- [x] Capturas reales del ciclo completo y320×568/1080×1920/1440×2960; IDs70/71/72 observados, cobro exclusivamente en handoff. Progreso de editor restaurado exactamente, Play detenido/Main limpio; sin APK ni entrega a teléfono.
- [x] Balance con ingredientes finitos y dinero real: Racing208.61s, SanLorenzo252.06s, River280.75s victorias con valores oficiales.
- [ ] Balance9–11: estrategia probada perdió; no es demostración de imposibilidad. Propuesta no aplicada320/335/335s ganó en clones de configuración; pedir autorización antes de ajustar.
- [ ] Dispositivo físico/notch/FPS sin verificar. No declarar cerrado el balance final del cambio.

Especificación, archivos, detalle de pruebas y números: [active-cooks-mozos](features/active-cooks-mozos.md). Galería/telemetría en`Logs/Acceptance/ActiveCooks-20261015/Final/`. Cambios locales previos preservados; no commit/push.

## Escenario original conservando cocineros/Mozos — 2026-10-15

Fernet otra vez alineado en su posición original, calle visible y sin mesa lateral de chimi. Se mantiene solo el espacio inferior ampliado; cocineros/Mozos y economía intactos.35/35 EditMode; revisión nativa River1080×1920 y Racing1440×2960. Reejecución de puntero PlayMode bloqueada por excepción del Test Framework,0casos: no aprobada. Main/progreso/editor restaurados; sin APK. Detalle y evidencia en [active-cooks-mozos](features/active-cooks-mozos.md#restauración-visual-solicitada--2026-10-15).

## Contenido centrado de botones — 2026-10-15

Personajes y flechas centrados dentro de los óvalos existentes, sin deformación; ×1,00/+10% elevado para no tocar el precio.13/13 EditMode, capturas reales de cards compactas/amplias revisadas. Sin cambios de hit areas, escenario o mecánica; sin APK. Evidencia: `Logs/Acceptance/ButtonOvals-20261015/`.


## Cocción visual permanente — 2026-10-18

Racing+: los Parrilleros/Premium alternan automáticamente entre producción real y una cocción cosmética persistente. Ciclos de progreso/brazos usan el rate +5% y ponderan recetas por metas con contadores aparte; no crean comida ni modifican stock, ingredientes, ventas o cuotas. Si el worker quedó tras depositar, vuelve a su ancla de parrilla; cuando se abre una mesa o vuelve stock, retoma reserva real. Humo y calor/brasas ya existentes permanecen activos por parrilla. Niveles1–5 siguen con su producción automática original.

**Validación:** Unity6000.6.3f1, StreetActiveCooksTests 31/31 EditMode; también se preservan las regresiones existentes de reparto/proporción/velocidad. git diff --check pasó. Captura manual intentada con una sesión de Play sin persistencia, pero el Game View quedó en la intro/login y no permitió revisar los brazos/progreso; validación visual real queda pendiente. Play detenido, Main sigue limpio, no APK, Git push ni dispositivo.

## Contacto visual de Parrilleros con la parrilla — 2026-10-19

La nueva captura siguió mostrando a Parrilleros normales y Premium visualmente detrás de la parrilla. La causa fue medir el contacto al punto medio de toda la ilustración, incluyendo el frente/patas, en vez de al borde donde se cocina. Ahora la proyección ancla los pies visibles al72% de la profundidad del arte ajustado, con el frente de la parrilla solapando las piernas; el ajuste escala al tamaño responsive y no mueve los postes lógicos ni rutas. Unity compiló los assemblies de juego y tests sin errores; las aserciones focalizadas se actualizaron, pero sus ejecuciones y la captura Game View se interrumpieron cuando el plugin volvió a dejar de responder. Sin validación visual final.

## Dos filas de sándwiches en mesas — 2026-10-18

`TableSlotPosition` coloca Chori, Paty, Bondiola y Vacío en dos filas de profundidad con cuatro anclajes compactos por zona; conserva sectores, tamaños de mueble y capacidades. El renderer dibuja la fila delantera sobre la trasera. El Mozo sigue el slot reservado y mantiene la corrección de seguridad del carril en X.

**Validación:** en una copia temporal aislada con Unity 6000.6.3f1, el contrato EditMode focalizado pasó mediante un harness Editor: cuatro productos, mesa compartida/individual, footprint completo dentro de la madera, dos filas, sorting y anclajes Mozo. Para compilar esa copia se excluyó únicamente `StreetActiveCooksTests.cs`, que ya tiene referencias no resueltas a tipos ajenos a esta tarea; se corrigió un conflicto de nombre local detectado en `StreetKitchenLayout.cs`. `git diff --check` pasó. Unity MCP identificó el editor/proyecto correctos, pero persistieron cierres WebSocket y timeout del ping. La captura Game View disponible solo muestra cielo/piso de la escena Main sin gameplay; no es evidencia válida. Capturas de stock bajo/alto/máximo, recogidas en ambas filas y resoluciones compacta/alta quedan pendientes; no declarar aceptación visual. Sin APK ni Git.


## Recorridos directos de cocineros y transición visual — 2026-10-19

Los cocineros con producción real reservan y van en una sola tarea hasta el slot definitivo; el contacto de render aparece/desaparece gradualmente según distancia de ruta. Se preservaron puestos/cola, rutas de muebles, velocidad, poses e inventario. Unity 6000.6.3f1: EditMode 23/23 (job `0bba01a8e59a416297edee3d9a192152`), PlayMode 3/3 (job `221788ca225541d494d6e31590c38262`); entrega mixta/retorno EditMode adicional 1/1 (job `2ac245e3770a4721ab989a30042de06d`; un intento previo falló antes de iniciar casos y se reintentó). Capturas reales revisadas: proyección de cocina antes/durante/después del contacto y Game View Racing 1080×1920 con `StreetView` completo, simulación transitoria de cuatro Parrilleros y un Premium (`/private/tmp/hay-chori-gameview-racing-after-clean.png`). Sin guardar cambios en escena ni alterar datos del jugador. El video se examinó como miniatura; no se generó una captura Game View previa equivalente ni se compararon los intervalos exactos indicados. El editor regresó a EditMode y la escena generada quedó limpia. Sin APK, commit, push o instalación.

## Calidad de sándwiches y separación de filas — 2026-10-19

Actualizados los cuatro PNG transparentes de Chori, Paty, Bondiola y Vacío; se conserva la escala de render. El Paty quedó de una sola carne y con proporción menos alargada que el Chori. En `StreetKitchenLayout.TableSlotPosition`, el área de profundidad para sándwiches pasa de 35% a42% del bounds de la mesa; son solo dos filas y no cambia capacidad, orden, separación por producto ni anclajes del Mozo. Los cuatro Sprite `.asset` se reconstruyeron preservando GUID y prefabs; `.meta` de fuente se mantuvieron.

**Validación:** Unity 6000.6.3f1 real: 4/4 EditMode (transparencia/referencias, geometría de dos filas y footprint) y 11/11 PlayMode (targets de renderer y capturador Game View). Capturas antes/después revisadas en540×960 y540×1200; cubren inventario escaso, las filas en mesa normal y Premium, y un Mozo transportando sándwich real. Solo hay una advertencia de fixture por no tener AudioListener; no afecta esta validación. La prueba anterior `IndependentResourcesKeepCookingSourceFramesAndSeparateStationPrefabs` inicialmente reveló una aserción obsoleta que suponía que toda textura importada conserva dimensiones fuente; se ajustó para respetar el límite `maxTextureSize` validando aspecto. No hubo cambios en escena, PlayerPrefs persistente, APK, Git ni instalación.


## Entrega Android 0.2.50 — 2026-10-19

Commit `df00c91` publicado en `origin/codex/street-automation`. APK Main-only Development IL2CPP/ARM64 generada desde ese commit: versión 0.2.50/code52, 347,626,599 bytes; SHA-256 `a93608239dd515cd107f52246c8fdfb0660831f7c3c55bdcf19e0a42e55d3f11`. ZIP, package/version/API/ABI, alineación y firma v2 verificadas; build Unity 0 errores/1 advertencia de recomendación de símbolos diagnósticos. Sin instalación en dispositivo ni QA de juego posterior a la compilación. Detalle: [android-prototype](features/android-prototype.md).

## Mesas de sándwiches 4×3 — 2026-10-19

Normal/Premium se amplían hacia atrás de196×98 a196×196 unidades nativas, conservando el borde de servicio alineado y la mesa Fernet intacta. Una familia de producto ocupa2×3 anclajes y una familia única4×3; excedentes se apilan sobre anclajes, sin alterar capacidad48/36 ni tamaño/arte del sándwich. Los puntos de recogida siguen siendo alcanzables alrededor de la base, y no se modificaron rutas de depósito ni reglas de producción.

Unity6000.6.3f1 MCP se recuperó tras dos desconexiones WebSocket; compilación limpia, consola final sin errores/advertencias. EditMode: 3/3 layout/sorting/pickup, 4/4 rutas de Mozo por producto, 1/1 producción/depósito y1/1 reserva de slot. PlayMode renderer: 1/1. Capturas reales del renderer Unity guardadas en `Logs/Acceptance/SandwichTables-20261019/`: mesa solo Chori, cuatro productos en ambas mesas, stock apilado y resolución1080×2400. No son Game View completa ni muestran Mozos; esa validación visual combinada queda pendiente. No se guardó la escena ni se tocaron PlayerPrefs. Sin APK ni commit/push.

## Mesas de comida tipo caballete — 2026-10-20

Normal/Premium ahora se componen de dos caballetes A independientes y un tablón superior; altura total 80 unidades nativas. La superficie útil ocupa 67%, el render de sándwiches pasa de `.80` a `.72` para conservar las tres filas, y en mesa de un solo producto los primeros dos ocupan extremos opuestos del tablón; se mantienen 4×3, capacidad 48/36, borde de servicio, rutas/pickups y Fernet separado con su arte original. Unity real: EditMode 4/4 (layout, posiciones, rutas y prefabs) y PlayMode 3/3 (composición, sorting, Fernet y capturas), más los reruns focalizados de slots (2/2), Game View (1/1) y workers (1/1). Capturas de cámara de cocina Unity en `Logs/Acceptance/SandwichTables-20261020/` para solo, paired, full stock y depósito del Parrillero a 918×1600, 1080×1920 y 1080×2400; Game View completa a 540×960 y 540×1200 muestra stock escaso, ambas filas y un Mozo llevando un sánguche. No se probó en dispositivo. MCP se recuperó mediante ping + refresh tras un timeout; no se reinició Unity ni se tocó escena guardada/PlayerPrefs. Sin APK, commit ni push.
## Indicadores de cocción a 4 px del pelo — 2026-10-20

- `StreetView` ahora calcula los anillos desde el límite alpha-visible de los cuatro recortes animados de cocción y aplica los mismos offsets de raíz que `StreetWorkerProjection`; borde inferior a 4 unidades lógicas. Normal `#FFFFFF`; Premium `#168BFF`; dimensiones y mecánicas intactas.
- EditMode enfocado: 4/4 (tres escalas verticales × las cuatro poses, más paleta). PlayMode Game View: 1/1; ocho capturas reales a 540×960/540×1200 para Normal, Premium, cuatro trabajadores por equipo y mezcla. MCP intermitente recuperado sin reiniciar Unity. Ver detalles/evidencia en `features/active-cooks-mozos.md`.


## Producción continua post-meta con stock cero — 2026-10-20

Racing+ ahora cocina Chori/Paty/Bondiola/Vacío como unidades reales incluso con metas individuales completas y Mercado/Heladera sin ingredientes. Se omiten reservas y débitos solo para esos cuatro productos en cocina activa; las bebidas y el pipeline niveles1–5 conservan las transacciones existentes. Capacidad física de parrilla/mesa sigue limitando, preserva el turno global y reanuda al abrirse un slot. Se mantiene la venta/moneda normal por unidad; telemetry de abandono no inventa consumo de stock.

Unity 6000.6.3f1: compilación limpia del runtime y EditMode assemblies. Unity MCP detectó el proyecto correcto pero perdió ping/timeout al inicializar su Test Runner (`completed=0`); seleccioné la instancia, pedí refresh/compilación e inspeccioné el log. Para no tocar el editor/escena activa, ejecuté Unity batchmode en una copia aislada bajo `/private/tmp/hcyp-active-cooks-zero-stock-20261020/`: **5/5 EditMode pasaron**, incluyendo `SalesContinueAfterFourSandwichGoalsAtZeroIngredients`, que completa las metas de los cuatro productos, vacía ingrediente, produce y deposita unidades reales, las entrega con Mozos y verifica ventas/ingreso únicos y partida aún activa. También pasaron alternancia post-cuota, producción de los cuatro productos con stock cero y full-table pause/resume sin perder turno. La estrategia aislada `StreetCompletePlayerStrategyTests.L06_forward_bulk_adaptive_bottleneck_D_checkEvery2Ticks_seed17_carry20pct` pasó 1/1 al probar compras, juego real y cierre de mercado en Racing. No se corrió PlayMode visual/Game View ni dispositivo; ninguna escena o PlayerPrefs del editor principal se guardó/modificó. `git diff --check` pasó. Sin APK, commit ni push.
