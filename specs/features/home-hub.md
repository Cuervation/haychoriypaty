# Feature: Portada y hub de día de partido

## Señales táctiles animadas y sin destellos fijos — revisión vigente 2026-11-06

- Sustituye los destellos pintados y los nueve glints superpuestos del delta 2026-11-02. Se limpian las estrellas decorativas alrededor de JUGAR, Mercado e Insignias y las cuñas de atención de Ranking en el mismo master 841×1870, conservando composición, personajes, textos y GUID. El original anterior se conserva solo como evidencia local; la identidad byte-a-byte con la referencia antigua es histórica, ya no requisito activo.
- Los cuatro carteles reales pulsan suavemente a escala 1–1,08, ciclo 2,8 s y fases alternadas, usando tiempo no escalado. Al mantener el toque reducen la escala a 0,95 y navegan al soltar. No agregar botones, estrellas nuevas ni recortes de carteles de una versión vieja; se muestrean las regiones locales del propio master limpio. No hay texturas/materiales nuevos por frame.
- La envolvente máxima visible define un área táctil estable: no cambia con la fase y cubre todo el pulso. Se conserva la proyección ScaleAndCrop, safe area, IDs de acciones y destinos/gate Firebase. No reactivar rigs ni capas históricas que tapen el fondo. Los efectos de hojas/humo/brasa siguen independientes; se retiran los brillos/sweeps antiguos sobre Ranking/Jugar para no competir con el pulso.
| Criterio obligatorio | Estado | Evidencia directa |
|---|---|---|
| Quitar estrellas decorativas pintadas y overlay de glints sin rediseñar el hub | `PASS` | Master limpio 841×1870 con GUID preservado y capturas reales; las estrellas del trofeo/escudos son contenido del dibujo, no destellos decorativos |
| Cuatro carteles pulsan y responden al presionar; funcionan con `Time.timeScale=0` | `PASS` | 4 casos parametrizados `EachActualSignPulsesAndPressesInsideAStableTouchEnvelope`; PlayMode `FourCleanSignsAnimatePausedAndTheirStableTargetsNavigateOnRelease` |
| Hitboxes estables cubren la envolvente y navegan al soltar hacia los mismos destinos | `PASS` | PlayMode prueba down/up de acciones 200/201/202/203 en ambas resoluciones; Mercado conserva su overlay con `hubActive` de fondo |
| Texto legible, sin bordes dobles/costuras evidentes, presentación full-bleed | `PASS` | Revisión visual independiente de fases A/B/presionado en Game View 540×1200 y 1080×1920 |

Unity `6000.6.3f1`: **6 casos EditMode únicos aprobados** (los 4 parámetros se revalidaron tras reforzar el pulso a 1,08) y **1 PlayMode aprobado**. El primer intento PlayMode falló por una expectativa antigua de Mercado y el índice de restauración del tamaño temporal; se corrigió solo el fixture y el rerun final aprobó. `git diff --check` focalizado `PASS`. Pruebas con perfil efímero en memoria, sin PlayerPrefs/cloud, y restauración del tamaño Game View. Evidencia no versionada: `Logs/Acceptance/HubTouchAnimation20261106/` (`EditMode-results.xml`, `EditMode-final-results.xml`, `PlayMode-final-results.xml`, capturas fases A/B/presionado en ambas resoluciones). `hub-touch-animation-unity.gif` combina 24 frames auténticos de Unity muestreados en un ciclo de 2,8 s; no es una animación generada ni prueba de dispositivo. Los tests históricos de `StreetHubViewTests` con PlayerPrefs fueron actualizados pero no ejecutados; el fixture nuevo aislado cubre la interacción afectada. APK/dispositivo: **`NOT_RUN`** (no solicitados).

## Alcance vigente — 2026-10-27

La presentación conserva la portada animada vigente (logo, tres frases y barra de progreso compacta de 118×6 unidades lógicas). Se muestra antes del hub, sin requerir que Firebase haya terminado el acceso. Al completar la duración mínima y la carga de recursos, avanza automáticamente a la pantalla principal. Ya no existe una portada Firebase independiente. Durante la carga inicial solo se muestra el arte de portada cuando ya está disponible; no se intercala la pantalla genérica azul «PREPARANDO EL BARRIO...».

La autenticación Firebase sigue siendo obligatoria para perfiles, progreso y juego. El acceso Google se abre dentro del hub desde el flujo de JUGAR: si se pulsa sin sesión lista, aparece el panel CUENTA en vez de dejar el botón sin respuesta. La ilustración no muestra un botón/avatar independiente de CUENTA. Tras autenticarse y resolver perfil/consentimiento/conflicto, se vuelve al hub; solo entonces JUGAR abre el selector. La selección del primer nivel conserva el tutorial inicial y sus existencias/condiciones; los demás niveles mantienen el paso por Mercado y JUGAR PARTIDO. Ningún botón inicia una ronda sin el gate Firebase y el flujo normal de preparación.

## Música de fondo del hub — delta 2026-10-27

La pista proporcionada Muchachos_16bit_20pct_mas_lento_LOOP.wav acompaña en loop únicamente la pantalla principal del barrio. Se inicia al entrar al hub y se detiene al abrir Mercado, selector, Insignias, Ranking, presentación, partida o resultado; permanece activa si se abre el modal de Cuenta sobre el hub. Es audio 2D con volumen moderado y compresión Vorbis, conservando el estéreo original.

- [x] Reproducción en loop condicionada al estado visible del hub, sin solaparse con otras páginas del juego.
- [x] AudioSource 2D, sin autoplay, y detención también al deshabilitar StreetView.
- [ ] Escucha/revisión de loop en Unity: **NOT_RUN** (el editor/MCP no estuvo disponible en esta sesión).

El hub es una escena ilustrada vertical, vista elevada de una calle de partido: se ve parte del estadio, viviendas/vereda, hinchas y un control policial donde se revisan DNI. Los puestos ilustrados funcionan como hotspots, dentro de los límites de su arte y respetando el área segura del canvas:

| Elemento del escenario | Acción | Destino / límite |
|---|---|---|
| Puesto de choripán | JUGAR | Selector de clubes existente; el jugador sigue el flujo de nivel y Mercado actual. No salta la selección ni inicia una partida sin la preparación vigente. |
| Almacén/supermercado | MERCADO | Pantalla Mercado existente; comprar sigue sujeto a saldo/stock/gates actuales. |
| Vendedor ambulante de camisetas | INSIGNIAS | Colección de21insignias por ventas acumuladas:500/2500/5000 para cada uno de los7productos, derivadas de estadísticas del perfil Firebase (ver delta vigente abajo). Sin premios nuevos. |
| Tablero del estadio | RANKING | Muestra canchas desbloqueadas y canchas ya ganadas en el perfil propio, y aclara que el ranking global está pendiente. No consulta ni expone una lista pública de otros jugadores: el contrato Firebase actual no autoriza ese acceso. |

Las páginas nuevas vuelven al hub; se reutiliza IMGUI y las rutas/mercado existentes, sin agregar otro framework ni servicios. Se mantiene el mismo master ilustrado vertical de 841×1870 a todas las resoluciones, dibujado una sola vez a pantalla completa con `ScaleAndCrop`: conserva proporciones y recorta solo el exceso necesario para llenar cada relación de aspecto. No se pintan márgenes ni se repiten, desaturan o generan variantes del arte. Los cuatro hotspots visibles (JUGAR, MERCADO, INSIGNIAS y RANKING) se remapean al recorte visible y permanecen dentro del área segura. En la franja superior, la ruedita reemplaza el rótulo AJUSTES y abre una capa local disponible también sin sesión Firebase; conserva el control para activar/desactivar la música y volver al barrio. Un botón de altavoz a la izquierda de CUENTA ofrece el mismo cambio de música en un toque, muestra ondas cuando está activa y una marca de silencio al apagarla, y permanece sincronizado con el panel. Ambos controles quedan dentro del área segura y fuera de los hotspots ilustrados. La preferencia es local y no altera progreso, autenticación ni reglas de juego.

## Extensión vertical del hub — 2026-10-27

- [x] Un solo master de 841×1870 reemplaza la imagen anterior dentro del mismo recurso/GUID. Continúa cielo/estadio arriba y calle/hinchas abajo; no hay variantes por resolución.
- [x] El master coincide con la relación 9:20 de 575×1280 y se muestra completo; en otras relaciones se contiene proporcionalmente, sin recorte ni estiramiento, y el sobrante se rellena con azul marino uniforme (sin variantes ni copias recortadas del arte).
- [x] Hotspots de Ranking, Jugar, Mercado e Insignias remapeados al master extendido y siguen la misma transformación de pantalla; no hay un hotspot independiente de Cuenta.
- [x] Geometría EditMode ejecutada en Unity 6000.6.3f1: **8/8 PASS** en 360×800, 575×1280, 1080×1920, 1080×2340, 1080×2400, 1200×2670, 1536×2048 y 2048×2732; también comprueba que los cuatro hotspots quedan dentro de la composición y que no existe un rectángulo de acción de Cuenta.
- [x] Capturas reales de Game View PlayMode revisadas en **575×1280, 1080×1920 y 1536×2048**: **3/3 PASS**. La primera relación muestra la ilustración completa; en teléfono estándar y tablet el mismo arte queda contenido, nítido y sin deformación, y el crop tenue del propio arte rellena los lados. Recibos PNG: `/private/tmp/hcyp-hub-responsive-qa-20261027/captures-{575x1280,1080x1920,1536x2048}/hub-playmode-*.png`.
- [x] Flujo de acceso verificado: **1/1 PASS**; sin sesión, JUGAR abre CUENTA; una vez Ready, JUGAR abre el selector. No se reintroduce un botón separado.
- [ ] Verificación en dispositivos físicos con notch y tablets: **NOT_RUN**; la comprobación de tablet fue Game View 1536×2048, no un dispositivo Android físico.
- Nota de validación de recursos: el test `NewStreetAssetsAndGoldCoinKeepNativeResolution` ejecutó 13 casos (7 PASS / 6 FAIL). Los seis fallos llegan al assert de formato Android: espera `RGBA32`, aunque el importador configurado reporta `ETC2_RGBA8`; ocurre también en otros cinco recursos y no señala dimensiones, carga o relación de aspecto. Se preservó el `.meta` existente y no se alteró esa compresión en este ajuste.

## Imagen a pantalla completa y ajustes — 2026-10-29

- [x] La ruedita reemplaza el rótulo AJUSTES en la parte superior izquierda; abre el panel independiente dentro del área segura, separado de CUENTA y de los cuatro hotspots.
- [x] Un botón superior de altavoz, a la izquierda de CUENTA, alterna la misma preferencia persistente; distingue visualmente música activa y silencio y sigue abriendo el panel para el resto de ajustes.
- [x] El panel funciona antes del login y conserva el hub; la música puede apagarse/encenderse al instante y la elección persiste localmente. JUGAR y su gate Firebase no cambian.
- [x] Se presenta una sola vez el mismo master a pantalla completa con recorte proporcional para cubrir; no hay bandas de color ni repetición del arte.
- [x] Matriz geométrica ampliada a 19 resoluciones portrait con safe-area sintéticas; chequeo estático independiente de cobertura total, hotspots visibles, targets de toque y no solapamiento: **PASS**.
- [x] La geometría de ambos iconos conserva targets de al menos 44 px, evita CUENTA/hotspots y remapea la región táctil con el mismo viewport en las 19 resoluciones: **PASS en evaluación estática de la matriz**.
- [ ] Ejecución de las pruebas Unity y revisión visual/capturas multirresolución: **BLOCKED / NOT_RUN**. El job EditMode devolvió `succeeded` pero `total: 0`; la lectura de recursos del Editor reportó la sesión desconectada. No se declara PASS de Unity ni de UI visual.
- [ ] Dispositivos físicos con notch/tablet: **NOT_RUN** en este cambio.
- Arte no regenerado: el master existente mantiene 841×1870; solo cambia su escalado/mapeo responsivo, sin modificar ni reimportar el recurso.

## Animación de personajes originales — delta 2026-10-29 (fase 1; aceptación pendiente)

Se probó únicamente el vendedor del puesto de camisetas, sin reemplazar el master `street-hub-stadium-v1` ni cambiar navegación, hotspots o sistemas de juego. La fase sigue abierta y no habilita incorporar los siguientes grupos hasta que el vendedor pase la revisión visual.

- [x] Derivadas dos capas auxiliares de 100×100 px (brazo/mano segmentados y parche local de fondo) exclusivamente desde el master; recursos en `Assets/Art/Street/Resources/StreetHubCharacters/`.
- [x] Animación temporal y aleatoria limitada al brazo; el dibujo base no se sobreimprime cuando está en reposo.
- [x] El test `SellerWaveCutoutRendersAndRestoresInTheFullHubGameView` terminó **1/1 PASS** y produjo capturas full Game View a **1080×1920**.
- [x] La revisión de la primera captura en movimiento encontró **FAIL visual**: se duplicaba el brazo al rotarlo en el canvas escalado. Captura: `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hcyp-street-hub-view-tests/hub-seller-wave-up-playmode-1080x1920.png`.
- [x] Se corrigió la composición de rotación para el espacio lógico local y se ajustaron los importadores de las dos texturas (sin mipmaps, sin resampleo NPOT, alpha limpio); **la corrección todavía no tiene una captura Unity posterior**.
- [ ] Aceptación visual actual del vendedor: **BLOCKED** por desconexión de Unity MCP; deben revisarse las poses después de la corrección contra reposo, oclusión y restauración.
- [ ] Capturas/chequeo geométrico en otras resoluciones y safe areas: **NOT_RUN** en esta fase.
- [ ] Android físico/perfil de rendimiento: **NOT_RUN**.
- [ ] Policías, personaje del almacén y chicos: **NOT_IMPLEMENTED**; esperar a que el vendedor pase primero.

SHA-256 del master antes y después del trabajo: `0573cc633b2d7aa2bfdde5433d1c1a01b75665b66310fc6e4924ce9e7305c098` (sin cambios).

## Animación ambiental del barrio — delta 2026-10-29

El hub conserva el master vertical vigente de **841×1870** sin redibujarlo ni modificar su archivo. Los efectos se dibujan encima, usando el mismo mapeo ScaleAndCrop/safe-area del arte y sin cambiar navegación ni hitboxes:

- [x] Tres hojas pequeñas amarillas/verdes caen de forma ocasional en el cielo visible con variación de recorrido, giro y tamaño.
- [x] Humo muy tenue y brasas/chispas mínimas quedan localizados sobre la parrilla; el brillo de la estrella de RANKING y el barrido de JUGAR son breves y tienen temporizadores independientes.
- [x] Los recursos de overlay se crean una sola vez mientras se usa el hub, se reutilizan sin arreglos/lecturas de píxeles por frame y se liberan al deshabilitar StreetView. El reloj de efectos avanza solo mientras el hub se dibuja.
- [x] Las anclas de humo, brasas, Ranking y JUGAR pasan la prueba EditMode focalizada en seis relaciones portrait: 360×800, 575×1280, 1080×1920, 1080×2400, 1200×2670 y 1536×2048; la misma prueba asegura que los cuatro rectángulos de hotspots no cambian.
- [x] Unity 6000.6.3f1: prueba EditMode de anclas **1/1 PASS**; prueba de navegación/hotspots PlayMode **1/1 PASS**; captura full Game View PlayMode con reloj de efectos mayor a 15 s **1/1 PASS**. Las dos capturas revisadas a 1080×1920 muestran el master actual al abrir el hub y después de 18 s de ciclo, sin cortes ni duplicación del arte. PNG: Path.GetTempPath()/hcyp-street-hub-view-tests/ (hub-ambient-initial-playmode-1080x1920 y hub-ambient-cycle-playmode-1080x1920).
- [ ] Banderas del estadio y camisetas del puesto derecho permanecen estáticas: están integradas en el master y no hay máscaras/capas limpias; recortarlas/desplazarlas duplicaría bordes.
- [ ] Perfil de rendimiento en Android físico y revisión en dispositivo: NOT_RUN. La matriz de seis relaciones es de geometría EditMode; la captura visual real fue solo 1080×1920.
- Arte no modificado por este delta (SHA-256 antes/después: 0573cc633b2d7aa2bfdde5433d1c1a01b75665b66310fc6e4924ce9e7305c098). El recurso ya tenía cambios locales respecto de HEAD al iniciar esta tarea; se preservó tal cual.

## Refuerzo moderado de animaciones ambientales — 2026-10-30

Por pedido del usuario se refuerzan los efectos que ya estaban en el hub, manteniendo el master/arte de fondo, anclas, escala/safe-area, navegación y hotspots. Se pasa de 3 a 4 hojas algo mayores, humo máximo de alpha 0,105 a 0,135, brasas de alpha 0,026–0,072 a 0,04–0,09 y destellos/barrido algo más brillantes y frecuentes. Los ciclos siguen siendo breves y escalonados. No se reactivan ni se cambian los movimientos superpuestos de personajes; banderas y camisetas continúan estáticas por falta de máscaras limpias.

- [x] Unity 6000.6.3f1: `AmbientImageAnchorsRemainVisibleAcrossPortraitCropsWithoutChangingHotspots` **1/1 EditMode PASS** (seis tamaños portrait, anclas de todos los efectos, hotspots intactos).
- [x] `AmbientOverlaysRenderInTheFullHubGameView` **1/1 PlayMode PASS**, reloj ambiental >15 s; capturas full Game View inicial/ciclo **1080×1920** revisadas en `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hcyp-street-hub-view-tests/`.
- [x] `NeighborhoodCharactersStayStillDuringAShortHubVisit` **1/1 PlayMode PASS**: sin renderer/rig de personajes durante 6,5 s; capturas inicial/final revisadas en el mismo directorio.
- [ ] Android físico y rendimiento: **NOT_RUN**; captura Unity Editor/Game View no equivale a validación en teléfono.

## Actividad ambiental ×3 — 2026-10-30

Reemplaza el refuerzo moderado anterior a pedido del usuario, que seguía percibiendo el barrio estático en el teléfono. Se conservan el master, la navegación, los hotspots y la pausa de rigs/personajes; no se modifican economía ni partidas.

- Densidad simultánea: **4 → 12 hojas**, **3 → 9 emisiones de humo**, con pools fijos reutilizados.
- Humo máximo **0,135 → 0,405** de alpha; brasas **0,04–0,09 → 0,12–0,27**. Hojas y humo algo mayores, con más recorrido vertical del humo.
- Esperas de reaparición de hojas/humo y ciclos de chispas/Ranking/JUGAR divididos por **3**; mayor brillo de Ranking y barrido JUGAR, con alpha siempre acotado a 1.
- Primera oleada de hojas/humo precalentada para que haya movimiento desde la entrada, sin varios segundos de imagen quieta. Se mantiene el reloj no escalado y exclusivo del hub.

Aceptación focalizada en el Editor Unity 6000.6.3f1 existente:
- **PASS 2/2 EditMode**: multiplicador/densidad/alpha/intervalos y anclas en seis tamaños portrait, con los cuatro hotspots intactos.
- **PASS 1/1 PlayMode**: tras una visita breve hay al menos ocho hojas y seis emisiones activas, reloj >15 s aun con `Time.timeScale = 0`, sin instanciar rigs de personajes; fixture restaura tiempo/guardado/tamaño de Game View.
- **PASS visual acotado**: capturas full Game View **1080×1920** a 3 y 4 s revisadas; las hojas cambian de posición, el fondo conserva su composición y botones/textos siguen legibles. Evidencia: `Path.GetTempPath()/hcyp-street-hub-view-tests/hub-ambient-triple-{3s,4s}-playmode-1080x1920.png`.
- **Estado al cierre de esta entrega (2026-10-30):** en ese momento no había nueva APK ni prueba Android; la versión 0.2.79/code81 instalada no contenía ×3. La compilación posterior 0.2.81/code83 y su estado de instalación se documentan en la sección del refuerzo natural 2026-10-31.

## Ambiente por capas separadas — pedido 2026-10-30 (aceptación previa; ver refuerzo 2026-10-31)

Autorizadas seis familias nuevas: banderas del estadio, camisetas colgadas, bordes de toldos, sombras lentas de nubes sobre la calle, pájaros lejanos y variación cálida suave de luces del almacén. Los personajes/rigs siguen pausados. Conservar master original, cuatro hotspots, CUENTA/ajustes/audio y gameplay; las sustituciones visuales usan fondo limpiado más primer plano transparente en recursos separados, nunca mover un recorte opaco con el objeto original visible detrás. El refuerzo ambiental ×3 anterior se conserva.

Criterios obligatorios de esta entrega:
1. Capas nuevas separadas y clean plate detrás de banderas, camisetas y bordes de toldos; master original/GUID intactos. Inspección visual sin huecos ni duplicaciones.
2. Las seis familias varían con el tiempo no escalado durante una visita breve al hub, con fases independientes, sin reactivar personajes ni animar texto/UI.
3. Usar el mismo mapeo del master sin deformar/duplicar el fondo, conservar hitboxes y composición en 1080×1920 y 1080×2400.
4. Recursos y pools reutilizados, reloj solo en hub, liberación al deshabilitar; sin servicios/dependencias ni asignaciones de arrays/texturas por frame.
5. Pruebas EditMode de recursos/mapeo/temporización y PlayMode con capturas reales; revisión independiente contra los criterios. APK/instalación/rendimiento Android NOT_RUN sin pedido.

Estado actual tras **nueva ronda explícita de corrección solicitada por el usuario: PASS en Unity**, revisión independiente incluida; sin certificar Android. El defecto era pasar rectángulos full-bleed por `LayoutRect` (multiplica y pero no height), que truncaba fondos/primeros planos en pantallas altas. El renderer ahora usa el mismo `HubArtworkScreenBounds` del master en píxeles físicos, escala uniforme y `GUI.matrix` identity local, restaurada al salir. No se modifica el mapeo global de UI/hitboxes. Pájaros con silueta curva y cuerpo cacheada una vez, en vez de trazos rectos; texturas destruidas al liberar.

- **PASS — criterio1:** master SHA256 original preservado; capas/clean plate separados; capturas full Game View sin cabeza duplicada, huecos, fragmentos ni doble borde de toldos.
- **PASS — criterio2:** poses0/2/5 muestran banderas/camisetas/toldos con movimiento y aves reconocibles en vuelo; sombras y brillo cálido permanecen sutiles (no bloqueante). Muestras deterministas de familias y reloj no escalado aprobados; rigs siguen null, no se anima texto/UI.
- **PASS — criterio3:** comparación independiente en1080×1920 y1080×2400, camisetas proporcionadas/vendedor visible/composición conservada; cuatro hotspots mantienen su acción. Dos nuevas regresiones verifican ancho y altura a la misma escala física y continuidad de bordes.
- **PASS — criterio4:** recursos cacheados y reutilizados, sin arrays/texturas por frame; PlayMode confirma reloj detenido fuera del hub y destrucción de patches y silueta de pájaro al liberar.
- **PASS — criterio5:** **7/7 EditMode +1/1 PlayMode únicos** focalizados. JobEditMode `b83da6601bd74c539fd8c28e0de74f97`; PlayMode final `0209ac5a03fa4af4b806beb8c4fdef26` (rerun con aserción adicional de liberación del pájaro; no sumar como otro caso). Consola sin errorCS; revisión independiente de8capturas PASS. Una desconexión MCP intermedia se recuperó con ping en el mismo Editor; no se reinició ni abrió otra instancia.

Evidencia real: `Logs/Acceptance/HubLayers-20261030/final/hub-layered-{master,phase-0,phase-2,phase-5}-playmode-1080x{1920,2400}.png`. La primera ronda anterior se detuvo correctamente tras dos ciclos FAIL; esta nueva autorización y evidencia reemplazan ese estado, no convierten los intentos anteriores en aprobados.

APK/instalación/Git **NOT_RUN**; la APK instalada no incluye estas capas. Procedencia de arte: `docs/art/hub-environment-layers-20261030.md`. No confundir este pedido con las antiguas máscaras de personajes pausadas.

## Refuerzo natural de las seis familias ambientales — 2026-10-31 (solicitado)

Mantener intactos el master ilustrado, sus GUID y los clean plates/capas transparentes existentes; no animar personas, textos, hotspots ni gameplay. El nuevo detalle usa las mismas anclas físicas, tiempo no escalado y recursos reutilizados del hub.

- Banderas y camiseta ondulan con amplitud mayor, frecuencias/fases distintas y segundo armónico suave.
- Quince hojas reutilizables parten de cinco zonas (cielo, copas superiores izquierda/derecha y árboles inferiores de ambos lados), y cruzan hacia el paseo; el primer ciclo empieza ya en movimiento.
- Nueve emisiones de humo usan tres siluetas orgánicas cacheadas, con alturas/anchos, curva, balanceo y destinos variados; seis brasas pulsan y dos chispas siguen sus propios intervalos.
- Toldos usan capas con clean backing; el borde superior y laterales quedan anclados mientras el bajo oscila en segmentos cortos y sin estirar la textura.
- Dos sombras blandas recorren áreas mayores de calle en diagonales lentas; las luces del almacén y su reflejo cálido tienen más extensión/brillo, acotados.
- Cuatro aves tienen pool separado, intervalos de aparición, dirección, altura, arco, tamaño y aleteo variables.

**Criterios de aceptación:** seis familias legibles y con fases distintas; toldos sin huecos/dobles bordes; árboles con hojas en movimiento fuera del cielo; master/UI/hotspots intactos; temporización/pools/recursos sin asignaciones por frame; Game View real en1080×1920 y1080×2400.

**Estado de comprobación final:** **PASS Unity** en el proyecto correcto con Unity 6000.6.3f1. El bridge tardó en inicializarse después de abrir el Editor, pero se auto-conectó (log: `Server ready on http://127.0.0.1:8080`, `Session connected`); no fue necesaria una conexión manual. La compilación de scripts tardó 1,35 s, sin errores C#; Console solo registró la advertencia de deprecación del Editor Intel/macOS.

- **EditMode:** **10/10 PASS**, job `35418785bfd14c2d91b6a5f5d50afffc`.
- **PlayMode composición/capas:** **1/1 PASS**, `LayeredEnvironmentRendersCleanIndependentLayersAcrossPortraitViews`, job `ecbccf7f85514039879b1dbbbec9ccc6`.
- **PlayMode animación ambiental:** **1/1 PASS**, `AmbientOverlaysRenderInTheFullHubGameView`, job `081630d8160644539be6acdb6b016d1d`; verificó capturas del Game View y actividad también con `Time.timeScale = 0`.
- Capturas reales revisadas a **1080×1920** y **1080×2400**, fases0/2/5, además de Game View 1080×1920 a3s/4s y luego de15+s con tiempo de juego pausado. Directorio: `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hcyp-street-hub-view-tests/`; muestras 3s/4s: `hub-ambient-triple-3s-playmode-1080x1920.png` y `hub-ambient-triple-4s-playmode-1080x1920.png`.
- La verificación cubre capas limpias, recursos reutilizados, hojas/humo/chispas/aves activas, rigs/personajes ausentes, reloj no escalado, liberación de recursos y conservación de hitboxes/composición. `git diff --check` PASS.

APK Development IL2CPP/ARM64 **0.2.81/code83 generada e instalada** desde Main-only, con estos cambios. ZIP/zipalign/v2 signature/package/version/ABI verificados; SHA-256 `b1a57cf18a76c0e37511d48ac73b0e6d1371a704ffea962259d9a6f059ff0232`. Se instaló como actualización en el Motorola, sin iniciar el juego; rendimiento/visual Android y Git **NOT_RUN**. Las capturas y jobs anteriores a los cambios finales no se usan como evidencia de la ronda Unity final.

## Aceptación histórica del hub (0.2.64)

- [x] Portada sin botones superpuestos y barra de progreso pequeña; pasa automáticamente al hub.
- [x] Hub con hotspots para jugar, Mercado, Insignias, Ranking y Cuenta; entrada/salida preserva la autenticación y flujos actuales.
- [x] Insignias muestran colección temporal derivada de desbloqueos/victorias ya guardados, sin reclamar persistencia o premios nuevos; la última también puede obtenerse al ganar Los Redondos. Ranking no presenta datos globales ficticios.
- [x] Mapeo de hotspots cubierto por prueba geométrica con insets superiores/inferiores asimétricos.
- [x] Compilación Unity y pruebas focalizadas finales: **20/20 EditMode + 10/10 PlayMode**. Capturas reales de UI PlayMode a1080×1920 revisadas independientemente: portada, hub, insignias y ranking. Corregidos recorte de nombres y contraste en Insignias.
- [x] APK Android **0.2.64/code66**, Main-only Development IL2CPP/ARM64: build Succeeded,0errores/1warning; metadatos, ZIP, alineación, firma APKv2 y bindings de escena verificados.
- [ ] Validación física en Android y login Google online: **NOT_RUN** en esta entrega. Las pruebas usan el gate local aislado, no una sesión de usuario real.

## Evidencia de entrega — 2026-10-23

Unity6000.6.3f1 se ejecutó en copia aislada, después de confirmar que el usuario había cerrado el Editor. Sin Unity MCP disponible en este chat; sin modificar trust/servicios/seguridad global. Las65fuentes de runtime/escena coinciden con commit7d44f62; los cambios locales ajenos de barriles ilimitados se preservaron en el checkout y se excluyeron del snapshot final.

Artefacto local `/private/tmp/HayChoriYPaty-0.2.64-code66-hub.apk`:370.871.429bytes, SHA-256 `0e5b6ad72fa135e6f06d0e74e43c9f6f71c0027d0a9ad27d471596fe7a591fe3`. Es debug Development, no firmado para PlayStore; no se instaló. Recibos/capturas: `/private/tmp/hcyp-hub-0.2.64/final/`; informe de build: `/private/tmp/hcyp-hub-0.2.64/build-report.txt`. El tamaño del APK se toma del archivo, no del campo agregado de BuildReport.

**Límite de validación:** la corrida exploratoria ampliada sobre el workspace inicial ejecutó184casos:173PASS/11FAIL. Se corrigió la expectativa obsoleta RGBA32 de portada/logo (Android ya usa ETC2), y el recheck final enfocado quedó verde. Las otras10fallas son reflexión ambigua de rutas y expectativas históricas de plantilla/campaña/balance fuera de este pedido; no se cambiaron gameplay/economía para forzar su aprobación. Los reruns no se suman como casos únicos adicionales. La referencia directa de interfaz en una prueba local ajena impedía compilar: se sustituyó por proxy/reflection, conservando sus demás cambios locales; ese trabajo no se incorporó al release.

## Insignias por ventas — delta vigente 2026-10-23

Reemplaza la colección provisional por clubes. Cada producto (Chori, Paty, Bondiola, Vacío, Coca, Fernet, Cerveza; IDs0–6) tiene3insignias acumulativas e inclusivas: **bronce500, plata2500, oro5000**. Total21. Una venta de Chori no cuenta para otro producto. Se muestran total vendido, avance por objetivo, conseguido/en progreso, estado de sincronización y advertencia de historial antiguo parcial cuando corresponde. Se reutilizan iconos, paneles y botón estándar; no se añaden premios ni cambios de gameplay.

Ventas: `SaveData.statistics.deliveredByProduct`, expuesta por `StreetGame.GetCumulativeProductSales` sin arrays mutables. Insignias obtenidas: `statistics.earnedProductSalesBadges[21]`, índice estable `producto*3+tier`, expuestas por `HasProductSalesBadge`. Se otorgan al consolidar entregas/recuperar checkpoint y se conservan una vez obtenidas; Normalize migra los perfiles existentes usando solo ventas registradas. Incluye entregas reales de resultados ganados/perdidos/abandonados confirmados y checkpoints recuperados por el flujo vigente. La UI muestra acumulado consolidado: tras victoria espera la elección de Chulengo; no suma otra vez pendingVictoryMatch ni activeAttemptSummary y multiplicar premio no multiplica ventas. No reconstruye ventas legacy con unlocks/metas/saldo.

Firebase almacena tanto los7totales como los21flags de insignias obtenidas dentro del mismo snapshot v4 de `players/{uid}/state/current`. Son campos aditivos compatibles de estadísticas; no hay otro documento por medalla, cambio de schema/reglas ni llamada de red por venta. Se conserva outbox porUID, deduplicación yCAS/conflicto existentes. Offline conserva progreso local pendiente; reinstalación recupera solo lo confirmado remoto y un crash solo el último checkpoint.

Aceptación de este delta (obligatoria):
- [x] Umbrales499/500,2499/2500,4999/5000, independencia7productos, IDs inválidos/long y migración sin ventas inventadas; flags obtenidos permanentes y recuperación deduplicada.
- [x] Roundtrip local snapshot/outbox/ACK/reapertura conserva totales y21flags explícitos de medallas, con aislamientoUID; reintentos no duplican. No sustituye roundtrip remoto real.
- [x] UI PlayMode real1080×1920:7productos,21medallas, progreso y botón regresar legibles; muestra QA9/21, no progreso real del usuario.
- [x] Unity6000.6.3f1:32/32EditMode +1/1PlayMode finales PASS, sin sumar reruns; geometría filas/footer verificada para900/960/1200unidades lógicas.
- [ ] Firebase remoto real de este delta: no aprobado sin roundtrip remoto autenticado (evidencia local no lo sustituye).

Evidencia local final: `/private/tmp/hcyp-sales-badges/persisted/` (XML, logs y captura de insignias). No hubo APK nueva, instalación, push ni escrituras remotas de prueba; la APK0.2.64 instalada previamente no contiene este delta. Los cambios locales ajenos se preservaron.

### Empaquetado del delta — 2026-10-23

APK0.2.65/code67 generada: Main-only Development IL2CPP/ARM64,0errores/1warning,447,19s; ZIP/alineación/firma/bindingsMain verificados y revisión independiente PASS. Fuente197c3a3 publicada,32+1tests reutilizados sin cambios. Artefacto `/private/tmp/HayChoriYPaty-0.2.65-code67-insignias.apk`,354.357.269bytes,SHA256 `98ca84f89eeb6e12635c6ad034234e907309642f7b3f9d1172c232e9b11170b0`. No instalada, sin roundtripFirebase real nuevo. Esto actualiza el estado de empaquetado indicado en la evidencia local anterior.

## Animación del barrio — integración y revisión visual 2026-10-30

Se integraron los cinco grupos de capas/reemplazo previstos: vendedor, tres policías, personaje del almacén y chicos. Se preserva el master de 841×1870 (SHA-256 `0573cc633b2d7aa2bfdde5433d1c1a01b75665b66310fc6e4924ce9e7305c098`); sus zonas limpiadas se superponen con partes articuladas. Los manifiestos usan UV normalizados y el runtime admite que Unity downsamplee la textura (p. ej. atlas fuente 720×720 importado en 512×512).

- [x] Unity 6000.6.3f1: `HubCharacterBehaviorTests` había pasado **7/7 EditMode**; la repetición posterior a este retoque visual no llegó a iniciar en Unity MCP (timeout de inicialización; no es un resultado de pruebas). Captura de poses **1/1 PlayMode PASS** a **1080×1920** y **1/1 PASS** a **1080×2400**.
- [x] En ambas resoluciones, la revisión visual independiente **PASS** para vendedor (master/rest/wave/restauración), integridad de policías y personaje del almacén, unión/manga del brazo de festejo del chico, y mano visible del gesto de la chica. Sin seam ni duplicación evidente. A 1080×1920 el encuadre portrait existente recorta la zona inferior; a 1080×2400 se ve más cuerpo sin deriva geométrica. Capturas en `Path.GetTempPath()/hcyp-street-hub-view-tests/` (`hub-seller-{original-master,patched-rest,wave-up,restored}-playmode-*` y `hub-characters-group-gestures-playmode-*`).
- [ ] Android físico y perfil de rendimiento: **NOT_RUN**; la aceptación visual de capturas no equivale a QA de dispositivo.

## Legibilidad de las animaciones del barrio — ajuste local 2026-10-30

El usuario informó que no veía las animaciones después de instalar la APK. La inspección del provenance confirma que la build instalada sí se compiló desde HEAD `688af0b`, donde ya existían `DrawHubCharacterAnimations` y los recursos `StreetHubCharacters/`; `global-metadata.dat` contiene ambos marcadores. El problema identificado era de legibilidad: el primer gesto podía esperar 3–18 s y varios desplazamientos eran de solo 1–3 px del master.

- [x] Se redujeron las pausas aleatorias para que vendedor, policía, almacenero y chicos comiencen gestos durante una visita breve al hub.
- [x] Se agregó movimiento sutil continuo en reposo y se aumentó la amplitud de los gestos, sin mover el master, hitboxes, navegación ni reglas.
- [x] Se añadió `DefaultGesturesBecomeVisibleDuringAShortHubVisit` como regresión para que todos los grupos tengan actividad en una visita breve.
- [x] `DefaultGesturesBecomeVisibleDuringAShortHubVisit`: **1/1 EditMode PASS**.
- [x] `NeighborhoodCharactersAnimateDuringAShortHubVisit`: **1/1 PlayMode PASS**; comprobó actividad de vendedor, al menos un policía, almacenero y chicos durante 6,5 s; capturas inicial/final **revisadas a 1080×1920**. `SellerWaveCutoutRendersAndRestoresInTheFullHubGameView`: **1/1 PlayMode PASS** tras ampliar los gestos.
- [ ] Re-run de los ocho casos de `HubCharacterBehaviorTests`: Unity MCP se desconectó sin devolver conteos (**unknown**, no se declara PASS).
- [ ] La corrección se compiló en `HayChoriYPaty-0.2.77-code79-hub-visible-20261030.apk`, pero su instalación/QA en Android permanece **NOT_RUN** porque ADB ya no detecta el teléfono.

## Pausa temporal de movimientos de personajes — 2026-10-30

A pedido del usuario se pausan los reemplazos articulados y todos los movimientos superpuestos de vendedor, policías, almacenero y chicos: el rig se considera visualmente insatisfactorio por ahora. El master ilustrado vuelve a ser la única representación de esos personajes y queda quieto. Se conserva el código/arte auxiliar para un rediseño posterior, pero el runtime no carga ni dibuja esos rigs; también libera una instancia previa si la pausa se activa con el hub abierto. Los efectos ambientales independientes (hojas/humo/brasa/brillos) y los hotspots del hub no cambian. No afecta movimiento de mozos/cocineros durante las partidas.

- [x] El hub no instancia el renderer/rig de personajes y muestra el master estático.
- [ ] PlayMode `NeighborhoodCharactersStayStillDuringAShortHubVisit`, con captura de Game View inicial y tras 6,5 s: **BLOCKED**; Unity MCP perdió la sesión al esperar `command_result` y `editor_state` volvió a informar `ping not answered`.
- [ ] Nueva APK e instalación: **NOT_RUN**; requieren pedido explícito. La APK instalada no incorpora esta pausa de código fuente.

## Restauración y animación de carteles Mercado/Insignias — 2026-11-02

Se recuperan los píxeles de ambos carteles desde el master anterior (`7a7e8b0`) como recortes transparentes en `Assets/Art/Street/Resources/StreetHubSignage/`; el master actual y sus GUID no se reemplazan. `StreetView.HubSignage` los dibuja sobre el hub usando exactamente las áreas de acción ya existentes para Mercado e Insignias (acciones 201/202), sin cambiar hitboxes ni navegación. Solo las dos placas tienen un vaivén/balanceo discreto en desfase: ciclo de3,6s con reloj no escalado, desplazamiento máximo1,25×3,2 unidades lógicas, escala±0,8% y giro±0,55°. El resto de arte/personajes y acciones queda intacto.

**Aceptación focalizada:** recursos importados y legibles; límites de dibujo coinciden con los hotspots; ambos carteles animan durante `Time.timeScale=0`; capturas full Game View antes/después muestran el resultado y no duplican las placas. EditMode `StreetHubSignageTests` y PlayMode `RestoredSignsAnimateInTheFullHubGameView` preparados, pero **BLOCKED / NOT_RUN**: Unity MCP no respondió (`ping not answered`; `instances=0`) después de tres solicitudes acotadas de refresh/compilación, incluidas dos esperas de60s. El proceso Unity conserva abierto el lock del proyecto (PID49011), así que no se abrió una segunda instancia ni se borró el lock. No hay captura de Game View ni resultado de compilación/pruebas para esta entrega. `git diff --check` focalizado sobre fuentes/contrato **PASS**; el chequeo global informa trailing whitespace preexistente en seis `.meta` de `Assets/Resources/ModularKitchen/Art/`, ajenos a este cambio y preservados. Revisión focalizada del diff **PASS**. APK e instalación **NOT_RUN** (no solicitadas).

## Carteles fijos, banderín de Insignias y destellos — 2026-11-02

Este delta reemplaza el vaivén/balanceo del intento anterior: **Mercado queda fijo** en la pared del almacén, arriba de su propio toldo; **Insignias** se coloca como banderín azul con respaldo de tela y dos ataduras visibles sobre la reja de venta. Las placas conservan los recortes originales recuperados en `StreetHubSignage/`; solo se compone el nuevo respaldo del banderín en runtime, no se sustituye el master ni se generan clavos. Las áreas de acción Mercado/Insignias se mueven junto a la señal visible; JUGAR permanece en la perspectiva original del mostrador. Dieciocho destellos pulsantes, animados con reloj no escalado, rodean JUGAR, Mercado e Insignias; los carteles no se desplazan ni rotan.

- [x] Mercado estático encima de la marquesina; banderín Insignias con placa legible, tela y ataduras en la reja; sin duplicar las placas del master.
- [x] Geometría de las acciones 201/202 coincide con sus nuevos bounds; las tres señales reciben múltiples destellos visibles.
- [x] `StreetHubSignageTests`: **3/3 EditMode PASS**. Consola Unity sin errores nuevos; permanecen advertencias existentes de otras partes del proyecto.
- [x] Captura **full Unity Game View/Play Mode real, 1080×1920**, revisada: `Logs/Acceptance/HubSignage-20261102/final-unity-gameview-1080x1920.png`. Esta evidencia no es una captura de dispositivo ni de APK.
- [ ] Comprador junto a JUGAR: **pendiente**; sigue integrado en el master. Se preservó el arte original en vez de ocultarlo con un parche que alteraba el fondo.
- [ ] PlayMode automatizado `RepositionedSignsStayPinnedAndSparkleInTheFullHubGameView`: **BLOCKED / NOT_RUN** en el último intento; el runner no comenzó las pruebas antes del timeout de inicialización. La captura manual sí corresponde a Play Mode real.
- [ ] APK, instalación y teléfono: **NOT_RUN** (no solicitados).

## Integración literal de la referencia completa — reemplazo visual 2026-11-02

Este delta **supersede** las composiciones parciales de recortes, telas y placas descritas arriba. Se usa como fondo la imagen completa entregada por el usuario (841×1870), que ya integra JUGAR pegado en perspectiva al puesto y sin clavos, Mercado sobre el toldo, Insignias como bandera azul atada a la reja, los destellos pintados y los personajes sin el comprador frente al puesto. El PNG del recurso `street-hub-stadium-v1` coincide byte a byte con esa referencia; se conserva su GUID/importador. No se dibujan encima recortes/cutouts del master anterior: esa capa antigua tapaba la bandera y el cartel de Mercado. Los cuatro hotspots continúan invisibles y se remapean a los elementos visibles; los controles de ajustes/audio aprovechan sus iconos integrados. Nueve glints pequeños animan sobre destellos ya pintados sin mover los carteles ni sus hitboxes.

- [x] El recurso del fondo es idéntico al archivo de referencia proporcionado y mantiene GUID `8a11c78f310d45a590d0761af9ed5631`.
- [x] `DrawHubScreen` deja de componer `DrawHubLayeredEnvironment`, evitando volver a superponer camisetas, toldos, lonas y recortes anteriores sobre una ilustración completa.
- [x] Los límites del hotspot quedan documentados por `StreetHubSignageTests`; las comprobaciones de geometría responsive para 320×568 y 360×640 habían pasado antes de este cambio de composición.
- [x] Compilación aislada con Roslyn 8.0.318 del Editor Unity 6000.6.3f1 usando los `.rsp` vigentes: runtime, EditMode y PlayMode; sin errores (warnings existentes anotados en el log local).
- [ ] EditMode `StreetHubSignageTests`: **NOT_RUN** en el reintento (0 tests iniciados; runner no inicializó en 120 s).
- [ ] PlayMode `CompleteReferenceArtworkIsNotCoveredByLegacyLayerCutouts` y captura full Game View 1080×2400: **BLOCKED / NOT_RUN**; el runner no inició ningún test en 120 s y luego el WebSocket MCP Unity se cerró. La captura anterior es previa a quitar la capa y no sirve como evidencia de aceptación de este delta. No declarar aceptación visual hasta capturar y revisar el Game View nuevo.
- [ ] Android/APK y dispositivo: **NOT_RUN** (no solicitados).
