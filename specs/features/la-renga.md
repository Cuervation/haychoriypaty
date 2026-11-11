# La Renga — Autódromo Bs. As. (ID13)

**Contrato nuevo — 2026-11-10. Estado: implementación incorporada; aceptación Unity pendiente por bloqueo MCP.** Este delta se aplica únicamente a La Renga y a la ampliación aditiva de campaña/persistencia. Reutiliza [Tower Defense](tower-defense.md), [Mercado](market-fridge.md), [Firebase](firebase-player.md) y el límite de [siete productos](../product.md); no reemplaza sus reglas compartidas ni sus pendientes históricos.

> Delta posterior del mismo día: [Peregrinación ID14](peregrinacion-lujan.md) se inserta entre13 y4; campaña vigente15 y sucesión13→14→4. El resto de este registro conserva el alcance original de la incorporación de La Renga. No cambia su balance ni sus assets.

## Identidad y campaña

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
