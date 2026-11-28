# La Renga — Autódromo Bs. As. (ID13)

**Contrato nuevo — 2026-11-10. Estado: implementación incorporada; aceptación Unity pendiente por bloqueo MCP.** Este delta se aplica únicamente a La Renga y a la ampliación aditiva de campaña/persistencia. Reutiliza [Tower Defense](tower-defense.md), [Mercado](market-fridge.md), [Firebase](firebase-player.md) y el límite de [siete productos](../product.md); no reemplaza sus reglas compartidas ni sus pendientes históricos.

> **Delta 2026-11-18 (vigente para ruta, cocina, footer y equipo inicial):** La Renga es un nivel de **valla en C** con la cocina de Huracán y el footer de Camioneros, ver [tower-defense.md](tower-defense.md#la-renga-id13-valla-en-c-con-la-cocina-de-huracán-y-el-footer-de-camioneros--2026-11-18-vigente). Lo que dice este registro sobre "tres tramos S", "dos áreas" y "trabajadores iniciales 0" quedó superado.

> Delta posterior del mismo día: [Peregrinación ID14](peregrinacion-lujan.md) se inserta entre13 y4; campaña vigente15 y sucesión13→14→4. El resto de este registro conserva el alcance original de la incorporación de La Renga. No cambia su balance ni sus assets.

## Rampa de hinchas vigente — 2026-11-28

**La Renga (ID13) y Los Redondos (ID10):** 440 hinchas en 22 tramos de 20. La velocidad se fija al entrar, de ×1,00 a ×2,68, con incrementos de 0,08. Cada nivel conserva su velocidad base (`fanSpeed` 0,1875 / 0,1535625, respectivamente), el mínimo de 70 px lógicos entre entradas, su recorrido, cocina, menú, plantel y precios. No cambia la regla terminal: sin plazo, hay que servir a todos y se pierde al primer hincha que llega a la salida con pedido pendiente.

Las dos últimas columnas son porcentajes sobre **todos** los pedidos: «al menos 2» incluye a los que piden 3. Por ejemplo, en 201–220 salen 25% de un producto, 70% de exactamente dos y 5% de tres. Una misma tirada decide el tamaño del pedido; los productos son distintos. Las unidades se sortean por separado para cada producto (el resto pide una); no salen 4 ni 5 unidades de un producto ni pedidos de cuatro productos. El tramo escrito como «240–261» se normaliza a 241–260, sin pisar los tramos vecinos.

| Hinchas | Velocidad | 2 unidades por producto | 3 unidades por producto | Al menos 2 productos | 3 productos |
|---|---:|---:|---:|---:|---:|
| 1–20 | ×1,00 | 0% | 0% | 20% | 0% |
| 21–40 | ×1,08 | 30% | 0% | 25% | 0% |
| 41–60 | ×1,16 | 40% | 20% | 30% | 0% |
| 61–80 | ×1,24 | 30% | 40% | 35% | 0% |
| 81–100 | ×1,32 | 20% | 60% | 40% | 0% |
| 101–120 | ×1,40 | 10% | 80% | 45% | 0% |
| 121–140 | ×1,48 | 10% | 90% | 55% | 0% |
| 141–160 | ×1,56 | 0% | 100% | 60% | 0% |
| 161–180 | ×1,64 | 0% | 100% | 65% | 0% |
| 181–200 | ×1,72 | 0% | 100% | 70% | 0% |
| 201–220 | ×1,80 | 0% | 100% | 75% | 5% |
| 221–240 | ×1,88 | 0% | 100% | 80% | 5% |
| 241–260 | ×1,96 | 0% | 100% | 85% | 10% |
| 261–280 | ×2,04 | 0% | 100% | 90% | 20% |
| 281–300 | ×2,12 | 0% | 100% | 95% | 30% |
| 301–320 | ×2,20 | 0% | 100% | 100% | 40% |
| 321–340 | ×2,28 | 0% | 100% | 100% | 50% |
| 341–360 | ×2,36 | 0% | 100% | 100% | 60% |
| 361–380 | ×2,44 | 0% | 100% | 100% | 70% |
| 381–400 | ×2,52 | 0% | 100% | 100% | 80% |
| 401–420 | ×2,60 | 0% | 100% | 100% | 90% |
| 421–440 | ×2,68 | 0% | 100% | 100% | 100% |

Los valores se serializan en los dos assets de nivel. Este delta reemplaza las rampas anteriores de 240 hinchas de La Renga y Los Redondos; Camioneros y Peregrinación no cambian. No certifica balance ni una estrategia ganadora.

**Validación:** EditMode **PASS 18/18** (tabla de ambos niveles, umbrales reales de pedido y tope de 440, regresión de Camioneros). PlayMode **PASS 3/3** entre la primera ejecución y la repetición de la auditoría de La Renga con su resultado actualizado: apertura de Los Redondos, derrota de La Renga y partida automática de La Renga. Esa estrategia perdió con 223/440 atendidos en 540×960 y 220/440 en 610×1356. Partida completa de Los Redondos y APK **NOT_RUN**.

## Identidad y campaña (registro original)

- ID estable **13**, posición visible **9**, nombre `La Renga – Autódromo Bs. As.`. Ambientación: recital multitudinario en el Autódromo Oscar y Juan Gálvez, inspirado en el 17/11/2007, sin atribuir precisión documental al arte.
- Orden exacto de catorce posiciones: `0,1,9,2,12,3,11,10,13,4,5,6,7,8`. Todos los IDs anteriores mantienen su significado. Ganar Los Redondos ID10 desbloquea ID13; ganar ID13 desbloquea Independiente ID4 mediante el cierre común idempotente.
- Selector, navegación, estadísticas, preparación del Mercado, reserva/liquidación de tickets y recuperación usan el ID estable; no deducir permisos por prefijos de posición ni por ID máximo.
- No modificar balance/jugabilidad de los trece niveles anteriores: Camioneros conserva **120 / 0.20475**, Los Redondos **60 / 0.1575**, tradicionales y Huracán sin cambios.

## Configuración y juego compartido

| Campo | La Renga |
|---|---|
| Motor | `TowerDefenseGameMode`, sin motor ni escena paralelos |
| Config Resources | `TowerDefense/LaRenga-AutodromoBuenosAires` |
| Fondo Resources | `street-background-larenga-autodromo-v1` |
| Hinchas / factor velocidad / separación entrada | **60 / 0.1575 / 96 px lógicos** |
| Caja / trabajadores iniciales | **500 / 0** |
| Primer Parrillero / Cocacolero | **60 / 60**, perfil económico TD compartido |
| Catálogo / roles permitidos | Chori ID0 y Coca ID4; Parrillero y Cocacolero |
| Ruta | Tres tramos S: izquierda→derecha, descenso, derecha→izquierda, descenso, izquierda→derecha |
| Servicio | Dos áreas; máximo tres trabajadores de cada rol por área |
| Final | Sin deadline; victoria tras atender 60 y completar sus salidas; derrota al alcanzar salida con pedidos pendientes |

Reutilizar ruta/follower, generador de doble valla modular, flechas, colisiones, mesa de choris, barril, preparación/recogida/lanzamiento/recepción, descuento de unidades, cobro y estadísticas. Trabajadores permanecen en su área. No añadir Mozo, Premium, Fernetero, paciencia ni parrillas tradicionales. No ampliar a cuarta fila/tercera área: una futura variante exigiría otra especificación/validación, no es parte de este pedido. No repetir el tutorial de Camioneros; su activación exclusiva ID9 permanece intacta.

## Arte y presentación independientes

- Referencias suministradas e identificadas: `codex-clipboard-1b4b178b-7a74-45c1-8f5c-2e2995a876e3.png` (collage de murales La Renga) y `codex-clipboard-3ef40b13-e659-4a58-a97f-bdb02a9ecb50.png` (mural león/guitarrista). No pegar fotografías completas ni recortes fotográficos de personas. Transformar los motivos en arte 2D original del juego; contexto coherente de autódromo/recital, sin mezclar otra locación.
- Mural/fondo e insignia propios registrados en `ClubVisualTheme`; proporción, recorte y ancla de los murales actuales. Conservar asfalto, vereda, mobiliario operativo, UI y perspectiva portrait. No crear mesas para sentarse.
- Familia exclusiva de **al menos 12 identidades**: ropa rock negra, jean/bermudas, variedad de pelo, género, edades y accesorios. Nunca fallback silencioso a hinchas Ferro/Camioneros/Los Redondos en ID13.
- Atlas transparentes compatibles con grilla/tamaño/pivote y convenciones existentes: caminata lateral ambas direcciones, dos poses reales de caminata frontal descendente en atlas pareado, recepción y salida. Mantener coherencia de las doce identidades entre poses.
- Globos unos píxeles sobre la cabeza sin cubrirla; separación de entrada impide superposición de hinchas. Mural legible y HUD/controles completos, sin vallas/estaciones/trabajadores invadiendo pasillos ni controles.

## Área de juego con poco alto — 2026-11-20
El arranque compartido Defense prueba primero el área segura habitual. Si falla la ruta o la cocina en un formato más bajo que los tamaños de teléfono aprobados, se valida de nuevo con una cancha retrato 540×960 centrada horizontalmente dentro del área útil. HUD, mundo y toque coinciden con el mismo recorte. No se modifica el recorrido, el balance ni el diseño que ya encaja. Si tampoco encaja el formato alternativo, no se abre un intento y el selector sigue operativo.

**QA #2 (PASS):** Unity PlayMode 4/4, 16 fallos de arranque recuperados con contrataciones y entregas reales; cuatro combinaciones cortas ya válidas sin encuadre alternativo; 36 combinaciones Defense en los nueve tamaños protegidos con las mismas entregas. Reflow en una partida y rechazo sin intento también comprobados. Evidencia de Game View en `Logs/Acceptance/QA-Responsive-2/`; teléfono físico y APK no ejecutados.

## Persistencia: migración mínima compatible

- Elevar **SaveData 7→8**, no el envelope local/remoto `schemaVersion=1`. Conservar lectura v1–v7 y sus reparaciones/archivado legacy previos; luego aplicar migración v7→v8 una sola vez.
- Ampliar unlocks de 13 a 14 copiando exactamente bits IDs0–12; no reordenar arrays por posición visible. Nuevos perfiles tienen ID0 abierto e ID13 cerrado.
- En perfil anterior, abrir ID13 si ya estaba abierto cualquiera de los niveles posteriores `4,5,6,7,8`, o existe victoria TD no-legacy de ID10. Conservar abiertos todos los niveles anteriores aunque el nuevo predecesor no esté ganado. No inferir victoria TD desde `unlockedLevel==10` ni desde récords tradicionales ID10 archivados. Una victoria pendiente no-legacy se conserva y su cierre común habilita el sucesor sin duplicar premios.
- Extender arrays de récords/victorias/derrotas a 14 conservando índices 0–12 y cero en13; retirar el clamp de estadísticas a12. No resetear monedas, inventario, tickets, mejoras, marcas de deduplicación, preparaciones ni intentos/recibos pendientes.
- `unlockedLevel` sigue siendo frontera **legacy 0–10**. No convertirlo en índice máximo13; permisos actuales vienen de `unlockedLevelIds`.
- `PlayerLocalRepository.ValidateSnapshot` acepta versiones antiguas por su propia forma (v7=13 bits, v8=14), no por el tamaño de la campaña actual. Nunca migrar/re-hashear operaciones ya encoladas: sus snapshots, UUID, payloadHash y revisiones son inmutables. La migración del estado vivo se persiste por la unidad duradera/outbox existente.
- Firebase conserva rutas, CAS, recibos y UID; revisar que ID13 sea válido en matches. No desplegar reglas ni escribir en un perfil real para validar sin autorización. Mock/local no demuestra Firebase real.
- Recuperación de interrupción conserva el mecanismo actual de abandono técnico/restauración de stock; no inventar reanudación física o victoria.

## Superficies afectadas / responsabilidad

- **Core/config**: `Assets/Scripts/Core/StreetSimulation.cs` (IDs/orden/nombres/modalidad/defaults), `StreetGame.cs` (v8/migración/load), `StreetPlayerStatistics.cs` (14 buckets/índice), `Assets/Scripts/Cloud/PlayerLocalRepository.cs` (compatibilidad de snapshots), `Assets/Scripts/TowerDefense/TowerDefenseLevelConfig.cs` y `Assets/Resources/TowerDefense/LaRenga-AutodromoBuenosAires.asset` (config). `Assets/Scenes/Main.unity`: extender catálogos/duraciones serializados sin cambiar entradas anteriores. Auditar reglas `firebase/firestore.rules`, sin cambiar esquema por conveniencia.
- **Presentación/arte**: `Assets/Scripts/UI/ClubVisualTheme.cs`, `StreetView.cs`, `StreetView.TowerDefense.cs`, `StreetView.LevelSelectorPresentation.cs` según necesidad; `Assets/Scripts/TowerDefense/TowerDefenseWorldPresentation.cs` selecciona los atlas/poses reales. Recursos PNG y `.meta` propios dentro de la estructura Resources existente; preservar GUIDs anteriores.
- **Pruebas focalizadas**: campaña/settlement/migraciones, `PlayerLocalRepositoryTests`, `StreetPlayerStatisticsTests`, `TowerDefenseGameModeTests`, `ClubVisualThemeTests`, `TowerDefensePlacementViewTests` y PlayMode de ID13. No ejecutar suites globales por defecto.

## Aceptación obligatoria

Todas las filas son obligatorias. `NOT_RUN` es ausencia de evidencia, no aprobación. Actualizar con resultados directos, nombres/jobs y rutas de capturas; validación independiente antes de declarar completo.

| ID | Criterio verificable | Estado / evidencia |
|---|---|---|
| LR1 | Compila Unity sin errores; 14 IDs/posiciones/nombres/catálogos y selector en orden exacto; sin índices/NullReference | `BLOCKED` — Editor MCP sin respuesta; sin evidencia Unity |
| LR2 | EditMode: ganar10→13 y ganar13→4, replay/settlement sin premios/tickets duplicados | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR3 | EditMode: migrar v7 de 13 bits y legado anterior; preservar cada unlock/economía/inventario/tickets/records; repetir migración no cambia nada; no confundir ID10 tradicional | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR4 | Validación local/cloud acepta snapshots v7 y v8; outbox antiguo conserva bytes/hash/UUID/revisión; guardar/reabrir y recuperación de interrupción preservan estado | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR5 | Configs reales: ID9=120/0.20475, ID10=60/0.1575, ID13=60/0.1575/96; mismo motor/perfil; tradicionales/Huracán sin regresión | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR6 | EditMode+PlayMode: 60 recorren tres tramos y conectores con doble valla; victoria tras última salida; derrota con pedido pendiente; sin paciencia/deadline | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR7 | PlayMode: contratar/colocar ambos roles en ambas áreas; preparar/recoger/lanzar/recibir; unidades/precios/caja/estadísticas correctos; sólo Chori/Coca | `NOT_RUN` — prueba escrita, no ejecutada en Unity |
| LR8 | Arte real: mural/insignia propios, ≥12 identidades coherentes en lateral/reversa/frontal/recepción/salida; sin assets de hinchas ajenos; transparencia/pivotes/importación correctos | `BLOCKED` — Editor MCP sin respuesta; sin evidencia Unity |
| LR9 | Game View real **540×960**, **1080×1920**, **720×1280**: recorrido completo/vallas unidas/mural legible/hinchas/ambas áreas/lanzamientos/HUD; sin superposiciones/globos sobre cabeza/controles cubiertos | `BLOCKED` — Editor MCP sin respuesta; sin evidencia Unity |
| LR10 | Tutorial ID9 preservado; ID13 no repite introducción; selector→Mercado/intento→resultado→salida/reentrada integrado | `NOT_RUN` — prueba escrita, no ejecutada en Unity |

**Evidencia 2026-11-10:** cuatro assemblies compiladas mediante Roslyn instalado (`PASS` estático), no importación/compilación del Editor. Unity MCP no respondió tras tres intentos acotados; run_tests agotó timeout sin job ID. LR1/LR8/LR9 `BLOCKED`; demás pruebas dinámicas `NOT_RUN`. Ver [informe](../../docs/reports/LA_RENGA_IMPLEMENTACION_2026-11-10.md) y [revisión independiente](../../docs/reports/LA_RENGA_REVISION_INDEPENDIENTE_2026-11-10.md).

**Entrega:** actualizar índice/tabla maestra y estado con evidencia real. No commit, push, APK ni publicación Firebase sin autorización. Si queda alguna fila `FAIL`, `BLOCKED` o `NOT_RUN`, informar pendiente y no declarar aceptación completa.

**Carne en las parrillas angostas (2026-11-25, a pedido: "hay 2 patys, tiene que haber 4"; luego "una fila más de patys y de bondiolas"; luego "simétricos, nada afuera y el mínimo de espacio sin carne"):** en La Renga y Los Redondos la parrilla mide el 73 % de la ancha, pero la carne conservaba su tamaño y las filas se apilaban en una sola línea (se veían 2 patys). Ahora, con Chori+Paty en la misma parrilla y con Bondiola sola, los cortes forman **una grilla llena y simétrica** (`StreetKitchenLayout.NarrowGrid*`): mitad izquierda **Chori 2 columnas × 4 filas = 8**, mitad derecha **Paty 3 columnas × 2 filas = 6**, la parrilla premium con solo Bondiola **4 columnas × 2 filas = 8** y, cuando lleva Bondiola y Vacío (Los Redondos), **mitad y mitad: Bondiola 2×2 = 4 y Vacío 2×2 = 4** (el Vacío usa la proporción real de su arte, 2001×786). Columnas del mismo ancho, filas repartidas parejo sobre toda la parrilla, tamaño del corte lo mayor que permite su columna (las filas se solapan por la perspectiva) y nada pasa del borde **trasero** de la rejilla (el más angosto), de modo que ningún corte cuelga de un costado a ninguna profundidad. `StreetKitchenProduction`: parrilla normal con Chori+Paty **14 (8+6)** (antes 12), Bondiola sola **8** (antes 4). En La Renga (cocina pasiva) la carne de la parrilla es solo visual; **en Los Redondos (cocina activa) sube la capacidad de cocción y cambia el balance, que no se midió** y ya estaba sin equilibrar. Verificado en captura PlayMode de La Renga (610×1356), 117/126 EditMode (9 ignoradas de antes) y 2 PlayMode de Redondos; no se vio en el celu.
