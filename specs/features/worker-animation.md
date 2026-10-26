# Worker animation — 2026-10-08

> Original four-role gait contract below; [Racing+ Mozo](#racing-mozo--2026-10-15) extends it to the fifth role. Keep open visual acceptance items open until actual evidence closes them; no new worker/animation QA in the documentation audit.

## Approved scope
Improve all four existing roles across catalog levels and arbitrary hired workers. Keep 90×98 worker frame, cartoon identity, route/timing/economy and physical-item lifecycle unchanged. Existing 16 Parrillero poses +8 diagonal poses and16 poses in each Coca atlas are retained. Palette variants remain cached Premium blue/Fernet black.

## Implementation contract
Eight canvas-space directions; four distinct gait samples per direction, reusing contact poses plus articulated passing legs. A four-frame stride spans80 logical units (40% lower cadence than the previous48-unit cycle). Gait advances only with actual traveled distance, not wall time; idle/pickup/handoff do not walk. Persist facing through stops and share presentation between worker and physical CarriedItemId. Reuse empty-handed Parrillero carry/reach poses; add only missing beverage diagonal poses and empty-hand carry quarters where baked bottle art cannot represent other products. Each live worker uses reusable cutout SpriteRenderers in the existing kitchen projection; carried unit, torso and finger layers share depth, foot and grip anchors. No extra camera, Animator controller, package or per-frame texture generation.

## Acceptance
- [x] Four visual gait samples ×eight directions for both families; all four roles use the same controller.
- [x] Station pickup, carrying, handoff and empty return track real states/physical IDs; no duplicate baked product.
- [x] Product contacts its hand and is correctly occluded by fingers/torso, including tall portrait.
- [x] Focused movement/appearance/native projection tests and real Main gameplay captures reviewed.
- [x] Original artwork/metas, player save, and mechanics preserved.
- [x] Beverage pickup reaches a safe barrel anchor, animates empty-handed during the existing dwell, and only carries the real inventory unit after withdrawal; Coca/beer workers operate concurrently.
- [x] Worker gait cadence is40% slower, remains distance-proportional for all four roles (including the fallback renderer), and halts in idle/pickup/handoff without changing fan animation.
- [x] Coca pickup restores the existing upright, front-facing empty-handed pose; its final approach also faces forward, with no horizontal presentation shift.
- [x] The visual foot baseline reaches barrel.yMax minus 24 logical units at pickup, and the barrel occludes the lower body rather than drawing the torso or feet through its front; the final frontal pose was reviewed in a full Game View capture.
- [ ] During Coca approach/departure, the baseline correction advances linearly only across the final/first 48 route units to avoid a nonlinear apparent-speed spike while preserving the same route, speed, timing, inventory and physical IDs. The EditMode math regression passed; PlayMode renderer validation and live-transition capture remain pending.
- [ ] User acceptance of the revised frontal perspective and live transition; the historic 2-unit rim clearance is not a ground-plane measurement and is superseded by the base-relative presentation above.

## Validation
- Original PNGs and their `.meta` GUIDs are unchanged. New transparent `street-worker-direction-extension-v1.png` adds only 20 missing poses (1086×1448): food quarter-carry, beverage quarter-walk and empty-handed carry views. Existing contact art plus articulated passing legs provide four **runtime-composed gait samples**, not 32 newly authored bitmap frames. Supplement imports at original dimensions, Android RGBA32, no obsolete iPhone PVRTC override.
- 19 distinct EditMode cases passed: 18 animation/movement cases (17 first run +1 corrected fixture) and 1 palette regression, including warm off-white Premium apron and unchanged skin/face/alpha.
- Native PlayMode acceptance passed:7 real products ×8 directions ×4 gait phases ×2 portrait scales = 448 samples. Checks hand contact, uniform body fit, four distinct gait samples, preserved physical object identity, front/back/finger depth, consume cleanup and no duplicate carried overlay.
- Real Main gameplay reviewed with 5 and 17 hired workers covering all four roles, at 1080×1920 and 1220×2712. Actual deliveries/revenue progressed through unchanged simulation; visual gait captures and hand-held products reviewed. Live StreetView disable/re-enable recreated palette/sprite resources safely; final runtime console had 0 errors/warnings. Exact original save, GameView, timeScale/background settings restored; Play stopped, scene clean.
- Evidence (ignored): `Logs/Acceptance/WorkerAnimation-20261008/main-five-staff-portrait.png`, `main-five-staff-tall.png`, `main-full-staff-final.png`, `main-full-staff-tall-final.png`, `gait-contact-sheet.png`, `workers-live-gait.gif`.
- Workers pool four SpriteRenderers each; cached sprite slices and palette textures, reused carried-ID set, no per-frame pixel/key generation. Native workers reuse the props camera and disable the old carried-only pass. No full-suite run, new APK, phone validation or Android FPS certification.

### 2026-10-09 — Barrel pickup reach and leg cadence

Coca and beer now use explicit accessible points at their respective barrel fronts. The worker pauses for the existing pickup duration; an empty-handed reach pose follows pickup progress and the carried drink is projected only after a real TryTake. Multiple Cocacoleros have no shared pickup lock and can serve separate Coca/beer customers in parallel. Gait uses80 logical units per four phases (versus48 before), driven by measured travel rather than time; movement speed, service duration, item lifecycle, fan animation, economy and objectives are unchanged.

Focused Unity checks passed: four EditMode worker/pickup cases, five route-matrix cases across levels0–4, and one native PlayMode projection case covering Coca/beer empty-hand reach, no pre-pickup carried object, post-pickup real unit ID, all seven products/eight directions/four gait phases/two portrait scales. An in-editor concurrent Coca+beer delivery also passed and earned only the existing $5+$7. The initial route test exposed a boxed-float cast in its expected-price assertion; that test-only cast was fixed and all five route cases passed on rerun. No full suite, APK or device build was run.

### 2026-10-11 — Stronger visible barrel reach

Visual code review found the native Cocacolero borrowed an empty-handed carry pose and moved its torso only 5.5 logical units at peak dwell (about 5.6% of the 98-unit worker frame); its grip/palm anchor did not follow that movement. The larger barrel drink scale is render-only and does not move the pickup anchor. Peak reach now lowers the torso and palm anchor together by 20% of worker height (19.6 logical units); the pickup route, dwell, inventory and `TryTake` timing remain unchanged. The focused PlayMode regression now checks the displacement and continued absence of a carried object before withdrawal. `git diff --check` passed. The updated PlayMode test and live visual result have not been rerun/captured in this change because the Unity editor state could not be confirmed (a Unity lock file is present) and no current device capture was available.


### 2026-10-12 — Front-facing, closer Cocacolero pickup

During an empty-handed Cocacolero barrel-pickup dwell, presentation now forces the existing front-facing reach pose, independent of the worker's last route direction; normal route-facing resumes outside the pickup state. Coca/beer pickup anchors moved from 12 to 2 logical units in front of each barrel (10 units closer), retaining 2 units of worker-foot clearance. Routes still use the shared catalog layout; pickup duration, inventory withdrawal, carried-item timing, movement, and delivery logic are unchanged. Focused EditMode regressions cover front-facing state and all 11 catalog layouts; Unity execution/visual capture status is pending.

### 2026-10-25 — Dedicated Coca barrel reach pose

Added the transparent `street-cocacolero-barrel-reach-v1` full-body pose for Coca pickup. Native projection slices the empty-handed torso/reaching arm farther down, moves the hand anchor with a 24%-height peak reach, and sorts that upper cutout over the barrel while keeping the existing legs behind it. The change is presentation-only: worker route/anchor, dwell, `TryTake`, barrel stock, and carried-item identity are unchanged. Focused Unity 6000.6.3f1 validation passed: asset-import EditMode 1/1 and native worker-projection PlayMode 1/1. Reviewed actual kitchen-camera projection captures at 1080×1920 and 1080×2400; this is projection-only evidence, not a full Game View capture.

### 2026-10-25 — Visible feet beside Coca barrel (historical; superseded below)

The Cocacolero previously stood directly behind the Coca barrel during pickup, hiding its lower body. Over the final 48 logical units of approach, during Coca pickup, and over the first 48 units of departure, the presentation eases the worker toward the barrel's screen-center side by `min(0.65 × barrel width, 54)` logical units; the reach pose mirrors inward so the hand stays over stocked drinks. The route/simulation anchor, pickup dwell, stock withdrawal, carried item identity, and barrel depth remain unchanged. The legacy fallback uses the same side/arm choice. Unity 6000.6.3f1 PlayMode passed the Chicago approach/pickup/departure projection test 1/1 and native worker projection regression 1/1. The regenerated native kitchen-camera capture is station/worker-only, not a full Game View or device QA.


### 2026-10-26 — Grounded frontal Coca pickup

The user rejected the remaining distance apparent against the tiles and requested frontal arrival. The lateral shift and dedicated leaning pose are replaced by the existing upright frontal empty-hand pose. The presentation stays on the route's X axis and eases the foot baseline to 24 logical units behind the barrel base, not beyond its foreground edge. Torso/legs sort behind the barrel; the small upward pickup motion no longer lowers the whole torso toward the floor. The dedicated PNG remains preserved but inactive. Native and fallback share the base-relative position/front approach. No station dimensions, route anchors, stock, timers or carried IDs change.

Unity 6000.6.3f1: two focused native PlayMode tests PASS (Chicago transition/grounding and seven-product/two-scale projection), plus interactive full Game View capture PASS at 1080×1920. The capture fixture first encountered an ambiguous TryHire overload, then the documented batch ScreenCapture restriction; explicit overload selection and interactive Editor capture resolved these test-only failures. Reviewed the full gameplay view with the tiled floor and foreground barrel; this is controlled pickup-state QA, not a physical-device or entire live-route video test. User visual acceptance remains open. Evidence: Logs/Acceptance/CocacoleroFront-20261026/.

## Racing+ Mozo — 2026-10-15

[Active cooks and Mozos](active-cooks-mozos.md) agrega Mozo4 con variante de delantal verde del atlas original y mantiene piel/cara/alpha/gait de80unidades. ToGrill/ToTable usan locomoción existente; Deposit reutiliza reach; cualquier transporte requiere CarriedItemId real. El agarre trasero de los nuevos viajes deja la unidad visible junto al antebrazo. Cocción muestra progreso real independiente y anillos separados. Render activo aplica profundidad coherente con muebles/pies; niveles1–5 conservan su proyección previa. Validación:71EditMode+7PlayMode y rechecks focalizados; capturas nativas en1080×1920/1440×2960/320×568.

## Revisión visual de Parrilleros — 2026-10-18

En Parrilleros/Premium, `Idle` y `Cooking` orientan al frente y usan un bob sutil de hasta 1,2 puntos; caminar conserva la orientación direccional. Los ítems transportados siguen siendo unidades reales; se baja el punto de agarre frontal hacia la cintura. Las pruebas focalizadas de orientación/bob, agarre y proyección pasaron; la aceptación visual en vivo quedó abierta hasta revisar una captura actual. El ajuste vigente de cuatro puestos y cola de espera abajo reemplaza el escalado en equipos densos.

## Anclas laterales de Coca — 2026-10-25

En los niveles 2 (Nueva Chicago) y 3 (Liniers/Vélez), todos los Cocacoleros apuntan al centro del frente del barril: al estar aislado, no se distribuyen lateralmente. Desde el nivel 4 se conservan las anclas separadas existentes para concurrencia; la distancia frontal/vertical y la lógica de stock no cambian. Prueba EditMode focalizada `CocaPickupLaneIsCenteredOnlyInLevelsTwoAndThree` aprobada 3/3 en una copia aislada con Unity 6000.6.3f1 (niveles 2/3 centrados y nivel 4 conserva dos accesos laterales).

## Contacto y animación real de cocción — 2026-10-18

El video mostró brazos duplicados/separados: `Original(w, 0, false)` ya contenía los brazos, mientras que el overlay añadía un segundo par desplazado por `cookingTarget - displayPosition`; una división en la cintura también podía cortar mano/pinzas. Parrillero y Premium ahora usan cuatro recortes transparentes de cuerpo completo (torso, hombros, brazos, manos, pinzas y pies) del atlas de cocción. El sprite único nace del pivote de pies del trabajador, usa la misma escala/profundidad y sustituye el torso y piernas originales solo durante la cocción; fuera de ese estado vuelve al render normal. No hay offset por slot ni duplicación de brazos. El frame sigue `CookingCycleProgress` real con fase individual por worker. La reserva se sigue asociando a comida real; la ruta ahora se dirige al borde accesible del puesto reservado y prefiere una separación visual de 84 puntos entre cocineros simultáneos (usa el mejor puesto restante si no es posible). La cola previa, inaccesible por obstáculos, se movió al área despejada frente a las parrillas. No se modifican metas, recetas, cantidades, tiempos de producción, inventario ni economía.

Validación aislada con Unity 6000.6.3f1: pasaron las pruebas focalizadas de alcance/espaciado real (1/1 PlayMode), cuerpo de cocción en 1–4 trabajadores/cuatro fases/dos escalas (1/1 PlayMode) y reserva preferida con fallback (1/1 EditMode). También pasó el test nativo de cuatro pasos de marcha y transporte real (1/1). StreetActiveCooksTests: 51/53; fallaron dos variantes preexistentes de retorno del trabajador a su punto seguro (Parrillero nivel 0 y Mozo nivel 5). El fixture amplio de StreetWorkerProjectionTests quedó en 3/5: fallaron la comprobación inmediata de reacomodo de equipos recién contratados (se evalúa antes de que se desplacen) y una expectativa de ocultamiento de trabajadores idle. Capturas de cámara nativa de Unity en 1080×1920 y 1080×2400; no son capturas completas de Game View. Unity MCP no respondió en `127.0.0.1:8080`; se preservó el editor activo y se validó en copia aislada. Sin suite global, APK ni Git push/commit.

## Circulación local y esperas diferenciadas — 2026-10-18

El amontonamiento real venía de compartir un único punto de depósito por producto, anclas comunes de recogida/barrel y el mismo `WorkerHomePosition`; además `MoveWorker` seguía cada waypoint sin observar a otros workers. La proyección ya dibuja todos los workers desde su posición real y los ordena por pies con desempate estable, por lo que no se alteró escala, ocultamiento ni renderer.

Se reparten las recogidas concurrentes por la cara frontal de cada estación, los cuatro depósitos de Parrilleros por dos filas accesibles en el borde de mesa y hasta cinco handoffs por un pequeño abanico frente al cliente. Los trabajadores sin tarea regresan por la ruta segura a un punto propio en un banco compacto situado fuera de las huellas de barriles y mesas. Durante el movimiento se usa una previsión local corta con preferencia determinista por ID y leve desvío lateral; los anchors estacionarios nunca se mueven, los desvíos se validan contra las huellas existentes y no se busca una ruta nueva por frame. La orientación a producto, reservas, IDs físicos, tempos, economía y los cuatro puestos por parrilla no cambian.

- [x] Implementación y cuatro regresiones EditMode añadidas (cinco casos, incluyendo retorno en All Boys y Racing) para accesos distintos, rodeo y puntos de espera.
- [ ] Ejecutar esas regresiones en Unity y revisar visualmente los cruces y los casos de alta concurrencia; no se cuenta como validado aún.

`git diff --check` pasó. El primer refresh de Unity MCP respondió `tests_running`; un reintento luego informó que recuperó la conexión y que el editor estaba listo, pero consola/estado siguieron sin responder al ping. El pedido de las pruebas focalizadas se desconectó mientras esperaba el resultado, sin ID ni reporte; por eso no se confirma compilación/tests ni se generaron capturas.


### 2026-10-26 — Avoid apparent acceleration on Coca barrel approach

The user reported that the Cocacolero seemed to accelerate as it reached the barrel. `MoveWorker` uses constant `MoveTowards` speed; the apparent spike came from an ease-in/ease-out ground-plane offset that added up to ~94 logical units over only the final 48 units. Each assigned route records its total length as presentation-only telemetry. The ground-plane correction is now blended linearly over only the final/first 48 traveled route units on approach/departure, while pickup keeps the full offset. This removes the nonlinear late-route acceleration without changing the transition span, logical route, `WorkRate`, movement speed, pickup dwell, inventory, or items. The EditMode regression `CocaBarrelBaselineOffsetIsLinearAcrossTheFinalAndFirstFortyEightUnits` passed 1/1 in an isolated copy. `ChicagoCocacoleroPickupIsFrontalAndGroundedAtBarrel` was attempted headless: a stale fixture X expectation was corrected from ±16 to ±12; the rerun then crashed in Unity URP/Null Device (exit 139) during camera rendering, so the PlayMode integration result is BLOCKED, not PASS. Live-transition capture and user acceptance remain pending because no interactive editor control is available; the active Unity lock was preserved.
