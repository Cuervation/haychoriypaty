# Auditoría profunda y limpieza de assets — 2026-11-06

## Resultado y alcance

**Limpieza segura ejecutada; validación global NO completamente aprobada.** Se eliminó un recurso B con su meta, sin editar gameplay, escenas, prefabs, importadores ni configuración. Persisten 14 fallos EditMode preexistentes. Los previews visuales no sustituyen partidas completas ni certifican victoria natural de Defense. Sin commit, push, APK ni instalación.

Checkout `/Users/celestino/HayChoriYPaty`, rama `codex/street-automation`, HEAD `bf777e6d3b8b55cd1806136d60eb30356c71ad55`; Unity **6000.6.3f1**. Se usó el Editor existente y Unity MCP; al terminar `Assets/Scenes/Main.unity` está abierta, limpia y fuera de Play. Se respetó el trabajo previo y la copia de contexto no se modificó.

## 1. Resumen cuantitativo

| Medida | Antes | Después |
|---|---:|---:|
| Archivos de asset, sin metas | 472 | 471 |
| Metas, incluidas carpetas | 552 | 551 |
| Archivos totales de Assets | 1024 | 1022 |
| Bytes de Assets | 477.168.331 | 474.674.811 |
| Entradas Unity importadas del grafo | 467 | 466 |

**Recuperado: 2.493.520 bytes (2,49 MB decimales / 2,38 MiB): 2.489.479 PNG + 4.041 meta.** Esto mide disco fuente, NO reducción de APK. APK antes/después y Android Development: **NOT_RUN**; no se hizo una compilación opcional ni se presupone ahorro de tamaño comprimido.

Clasificación de los **472 assets de la línea base**: **333 A**, **1 B**, **30 C**, **108 D**. El [inventario completo](asset-audit-inventory-20261106.csv) da ruta, categoría, bytes, GUID y evidencia para cada uno. A incluye dependencias del proyecto, tests y herramientas; no significa que todo esté visible simultáneamente en Main. C incluye SDK/build inputs y archivos locales cuya falta de uso no se certifica. D identifica histórico/fuentes/evidencia, no uso runtime. Los metas se contabilizan aparte, sin inflar el total de candidatos.

### Reconciliación disco / AssetDatabase

La diferencia neta de cinco no son cinco assets perdidos: Unity agrupa `Assets/Plugins/Android/FirebaseApp.androidlib` como una entrada importada; el disco contiene sus tres archivos internos (`AndroidManifest.xml`, `project.properties`, `res/values/google-services.xml`) y tres `.gitkeep` de Animations/Audio/Prefabs que no son entradas importadas. 472 − 6 + 1 = 467. Esos inputs nativos se conservan.

## 2. Baseline y mapa real

- Git status completo previo, branch/HEAD/root, tamaños y SHA-256 del trabajo local previo se guardaron antes de editar. Las huellas de cambios y assets locales preexistentes coinciden; solo se añaden enlaces de esta auditoría a documentación vigente, conservando su contenido anterior.
- `EditorBuildSettings` mantiene **Main y SampleScene habilitadas**. Aunque launchers Android históricos usaron Main-only, SampleScene sigue siendo raíz de configuración; no se eliminó ni se alteró silenciosamente.
- Se exportó `AssetDatabase.GetDependencies(path, false)` + GUID/tipo de todas las entradas importadas; se unió con referencias GUID de escenas, prefabs, assets, importadores/settings y fuentes. Se revisaron C#/Editor/tests/Tools, Resources.Load/LoadAll/LoadAsync, constantes, arrays, rutas construidas y reflection. La ausencia de una coincidencia textual sola nunca autorizó borrados.
- Se verificaron `ClubVisualTheme`, `StreetView`, `StreetKitchenRenderer`, `StreetKitchenPrefabBuilder`, `TowerDefenseAssetBuilder`, `PrototypeView`, guards Android, tests de arte y pipeline de hub. Familias dinámicas de estados de carne/atlas/partes no se trataron como huérfanas. Código runtime/Editor/tests compilado se conserva; no se demostró ningún script removible con seguridad.
- No se encontró catálogo Addressables, SpriteAtlas ni carga custom AssetBundle en Assets; disponer de módulos de Unity en Packages no prueba que existan tales catálogos. Plugins/Firebase/GeneratedLocalRepo/StreamingAssets se preservan: el grafo del juego no certifica sus dependencias internas.
- Cobertura de carpetas: Art, Resources, Scenes, Prefabs, Scripts, Animations, Audio, Editor, Tests, Settings, Plugins, GeneratedLocalRepo, StreamingAssets, Screenshots, TutorialInfo y `_Recovery`. Animations/Audio incluyen placeholders; no se inventaron animaciones inexistentes. Dos WAV y dos fuentes fueron incluidos en el inventario/SHA, sin candidatos B demostrados.

Evidencia de trabajo local: `Logs/Acceptance/AssetCleanup20261106/` contiene `baseline-files.json`, `git-status-before.z`, `preexisting-fingerprints.json`, `reference-inventory.json`, ambos grafos Unity y revisión independiente `dynamic-roots-review.md`. Logs está ignorado; no se pretende versionarlo como runtime.

## 3. Eliminación definitiva completa

| Ruta | Tipo | Bytes | Motivo y evidencia |
|---|---|---:|---|
| `Assets/Art/Street/Resources/street-cover-loading-v1.png` | PNG | 2.489.479 | B: cero referencias entrantes Unity, GUID y consumidores runtime/Editor/tests/Tools/dinámicos; sin contrato de fuente/retención encontrado. Runtime usa portada real y pruebas actuales rechazan la pantalla genérica. |
| `Assets/Art/Street/Resources/street-cover-loading-v1.png.meta` | Meta | 4.041 | Meta del único recurso B eliminado; GUID sin referencias supervivientes. |

GUID retirado: `e7b74f7ec8514e1aa133b75535c75cd8`. SHA-256 PNG: `01b23676d3ae777ff88c482fbbe27217854f96074335e72675211cb0fcd85b2c`; meta: `45898781af29eebee9f33b603b3fce24f86a9d6243a42e80a834641d10c480b2`. Ambos estaban tracked y sin cambios; reversión posible desde HEAD.

El borrado se realizó con AssetDatabase y Refresh; no se creó Assets/Unused. El diff final solo elimina ese par. Una respuesta MCP devolvió `success:false` sin detalle después de ejecutar: se comprobó el resultado en disco, manifiesto y grafo en vez de asumir que había fallado.

### Corrección de clasificación durante la revisión

La propuesta preliminar de diez **no era un permiso de borrado**. Revisión independiente detectó contratos de conservación. Se conservaron cuatro atlases Riot y parrilla v3. Un grupo provisional de cinco se llegó a retirar; cuatro piezas del hub se restauraron byte a byte desde HEAD tras detectar conservación/procedencia explícita. Sus PNG y metas coinciden con SHA inicial. El resultado final B es **solo la portada genérica**, no diez ni cinco recursos. `retention-correction.json` documenta esa corrección; `deletion-results.json` conserva el historial provisional y **no es la lista final de bajas**. La lista definitiva es esta tabla y `deletion-proposal.json` final.

## 4. Recursos conservados y caso Camioneros

| Recurso/familia | Clasificación / causa |
|---|---|
| `street-parrilla-large.png` | A por test `StreetArtTests.cs:1679–1680`, aunque ya no se dibuje en TD. No cumple la condición del usuario de ausencia de tests necesarios. |
| `street-parrilla-large-v2`, `-v4`, `-velez-v1` | A: All Boys, Chicago y niveles Paty; cargas de StreetView. No se sustituyeron ni tocaron importadores. |
| `street-parrilla-large-v3` | D: `specs/status.md:1348` exige retener PNG y meta supersedidos. |
| TD vigente | Mesa tradicional con choris y barril azul con Coca; captura `after/nivel-09-defense.png` confirma ausencia de parrillas visibles. Producción/servicio no se cambió en esta limpieza. |
| Riot San Lorenzo/Racing/Boca v1 e Independiente v2 | D: `GenerationPrompts.md:5` conserva versiones anteriores como históricas; oficiales actuales v2/v3 quedan A. |
| LegacyKitchen, cuatro imágenes | A/D: tests verifican SHA/GUID y builder utiliza referencia Android; ya fuera de Resources, no se movió. |
| Floresta placeholder original | D: README exige preservar originales; variantes v2 y GameController/PrototypeView siguen usadas por tests, builder y guard Android. |
| Fondos Ferro/Independiente anteriores | A por `StreetArtTests:3078–3105`; no equivalen a basura por haber murales master nuevos. |
| Portadas/escenarios históricos y FanWardrobe/ChicagoWardrobe | D por referencias artísticas, prompts/README; no se restauraron como runtime ni se borraron. |
| Hub characters / wave arm y clean plate | D: contrato de pausa conserva arte/código auxiliar para rediseño. No se reactivaron rigs. |
| Mercado/Insignias recortados | D: procedencia recuperada del master documentada en `home-hub.md:233,239`; no se confunden con carteles actuales. |
| Screenshots, `_Recovery`, InitTestScene previos | D/C y huellas preservadas: evidencia/trabajo local ajeno, no basura disponible para este pedido. |
| Google/Firebase/native/config, dependencias y link.xml | A/C: raíces SDK/build o comportamiento vendor no descartable; no se podan por falta de referencias desde Main. |

Los C están enumerados individualmente en CSV: destacan `street-background.png`, `parrillero-poses.json`, Readme/layout tutorial, archivos locales de runner y dependencias nativas/vendor sin demostración completa de consumidores. **No se autoriza borrarlos.** No se movieron D fuera de Resources porque eso puede romper rutas o contratos; una política de archivado separada sería un cambio posterior.

## 5. Duplicados exactos y similares

SHA-256 sobre PNG/audio/fuentes encontró **tres grupos exactos**, todos capturas históricas locales, no arte runtime:

- `Assets/Screenshots/grill-distance-before-2026-10-18.png` / `Assets/Screenshots/parrilleros-racing-after-2026-10-18.png`: conservar ambas como evidencia local; no consolidar ni modificar trabajo previo. SHA `78f1b221b24a908a25aad83370217b5f0f426243f1d963e9490e1015b8a794c6`.
- `Assets/Screenshots/parrilleros-racing-contact-seam-fixed-2026-10-18-1.png` / `Assets/Screenshots/parrilleros-racing-contact-seam-fixed-2026-10-18.png`: conservar ambas como evidencia local; no consolidar ni modificar trabajo previo. SHA `8fe899a8702010d0dca77f5af55456a805357e10b867381e2b37cf2a4da893ef`.
- `Assets/Screenshots/parrilleros-distance-after-unity-final-2026-10-18.png` / `Assets/Screenshots/parrilleros-distance-after-unity-final-2026-10-18-1.png`: conservar ambas como evidencia local; no consolidar ni modificar trabajo previo. SHA `e60b2b0d91739c93c127b6b07f6d51e3b032fb7fe9de31c0d37e04b61097a571`.

Comparación visual adicional: `similar-variants-review.png` y `similar-variants-imports.json` muestran cinco parrillas (base/v2/v3/v4/Vélez) y Boca Riot v1/v2. Las parrillas tienen cambios reales de contenido y dimensiones (2172×724, 2206×713, 2170×725); pivote común 0,5/0,5 no las hace equivalentes. Son Sprite/alpha/Bilinear/max4096. Boca v1/v2 comparten 1536×1024 pero cambian poses, escala corporal/gutters y maxSize2048/4096: no son intercambiables. No hubo consolidación ni recompress/importer changes. La búsqueda SHA fue exhaustiva sobre los tipos indicados; la comparación visual de similares fue focalizada, no un detector perceptual exhaustivo de las 240 PNG.

## 6. Validación ejecutada

| Criterio | Resultado y evidencia |
|---|---|
| Baseline/Git/root/grafo/dinámicos y clasificación completa | PASS: inventario de 472 filas + revisión independiente; clasificación final conservadora. |
| B eliminado con meta, sin modificar C/D o lógica | PASS: un par eliminado; otros nueve retenidos; huellas previas preservadas. |
| Importación y compilación | PASS: Refresh, `isCompiling=false`, consola 0 errores; scripts runtime intactos. |
| Referencias causadas por limpieza | PASS: cero referencias supervivientes al GUID retirado; grafo difiere únicamente en ese nodo. |
| Missing en Main/prefabs | PASS: 86 componentes inspeccionados, cero scripts/referencias rotas; `missing-references-after.json`. No equivale a recorrer toda escena histórica en Play. |
| EditMode StreetArtTests, antes y después | **FAIL preexistente**, 296 PASS / 14 FAIL de 310 en ambos; conjunto completo de nombre/estado/mensaje idéntico. **PASS delta: cero nuevos fallos.** |
| Probe PlayMode de 17 superficies | PASS antes y después: 1 caso por corrida, todos los recursos listos y captura Full Game View a540×960. |
| StartupShowsTheRealCoverWhileOtherAssetsLoad | PASS después de limpieza. |
| StartupPresentationAndHubRenderBeforeFirebaseActivatesThePlayer | PASS después de limpieza. |
| Partidas completas de todos los clubes y animaciones de todas las acciones | NOT_RUN: previews controlados no prueban ese alcance. |
| Victoria natural TD | FAIL previamente documentado, fuera de alcance de limpieza: 9 atendidos y 1 escape; no se arregló balance para ocultarlo. |
| Android Development/APK antes/después | NOT_RUN, compilación opcional no ejecutada; no se instaló nada. |

Jobs: baseline EditMode `e820543149d94d65b41b184fd1e89fe2`; después `a4d2cf1ecab341e5a83bb72728be8181`. Probe antes `32d3e6e2ea9f4d34ac86d26b205b60e7`, después `778b00bc8dd941dd8b798b376a491ca4`. Startup después `be90d10ceb174014a529c0d6a0326d5a` (2 PASS). Conteos únicos después: **310 EditMode + 3 PlayMode**; no sumar corridas repetidas como cobertura distinta. Los 310 callbacks reales se exportaron en `EditMode-baseline-results.json` y `EditMode-after-results.json`, y los dos startup en `PlayMode-startup-results.json`.

Los 14 fallos base son: música encima de hotspot hub a1536×2048 (1); cuotas ID9 esperadas7 frente2 (1); footer Huracán ID11 con bloque de mejoras esperado por test viejo (12 resoluciones). No se alteró lógica ni assertions para ponerlos verdes. La falla natural TD y los fallos anteriores impiden presentar el proyecto como totalmente validado.

### Comparación visual real

Se guardaron **34 capturas: 17 antes y 17 después**, en `before/` y `after/`. Son Full Game View desde PlayMode a540×960, no mockups ni dispositivo físico. El harness usa perfil aislado en memoria, sin cloud ni progreso real; se retiró su .cs/meta temporal y se conserva una copia solo en Logs. Niveles tradicionales: Ready previews con un hincha controlado, no partidas completas. TD: arranque del modo real con un pedido controlado y cero trabajadores, mesas/barriles presentes. Captura permite comprobar arte, mobiliario y HUD; variación de poses/humo se observa entre frames, no certifica todas las animaciones ni entregas.

| Superficie | Captura por fase | Píxeles distintos / 518400 | Resultado visual |
|---|---|---:|---|
| extras | `extras.png` | 0 | PASS: mismos recursos, sin pérdida observable |
| hub | `hub.png` | 2611 | PASS: mismos recursos, sin pérdida observable |
| mercado | `mercado.png` | 0 | PASS: mismos recursos, sin pérdida observable |
| All Boys | `nivel-00.png` | 309 | PASS: mismos recursos, sin pérdida observable |
| Chicago | `nivel-01.png` | 320 | PASS: mismos recursos, sin pérdida observable |
| Vélez | `nivel-02.png` | 313 | PASS: mismos recursos, sin pérdida observable |
| Ferro | `nivel-03.png` | 682 | PASS: mismos recursos, sin pérdida observable |
| Independiente | `nivel-04.png` | 665 | PASS: mismos recursos, sin pérdida observable |
| Racing (avanzado) | `nivel-05.png` | 1519 | PASS: mismos recursos, sin pérdida observable |
| San Lorenzo | `nivel-06.png` | 2303 | PASS: mismos recursos, sin pérdida observable |
| River | `nivel-07.png` | 3425 | PASS: mismos recursos, sin pérdida observable |
| Boca | `nivel-08.png` | 3179 | PASS: mismos recursos, sin pérdida observable |
| nivel-09-defense | `nivel-09-defense.png` | 819 | PASS: mismos recursos, sin pérdida observable |
| Los Redondos | `nivel-10.png` | 2960 | PASS: mismos recursos, sin pérdida observable |
| Huracán bonus | `nivel-11.png` | 556 | PASS: mismos recursos, sin pérdida observable |
| Argentinos Juniors | `nivel-12.png` | 1104 | PASS: mismos recursos, sin pérdida observable |
| selector | `selector.png` | 0 | PASS: mismos recursos, sin pérdida observable |

`visual-comparison.png` reúne pares antes/ después en orden alfabético, cuatro pares por fila, izquierda antes/derecha después. Mercado, Extras y Selector coinciden píxel a píxel; las otras diferencias son pequeñas y se inspeccionaron visualmente (poses/efectos/reloj no escalado). **No se afirma identidad píxel a píxel de las 17 vistas.** Los 13 niveles se ampliaron respecto del mínimo; Selector muestra su viewport, no todos los scrolls/pantallas simultáneos.

Las capturas posteriores se realizaron durante la retirada provisional de cinco imágenes; la restauración byte-identical de los cuatro auxiliares no cambia recursos activos ni código, y los tests de inicio posteriores y grafo final se ejecutaron sobre el conjunto definitivo. No se presenta esa restauración como un rediseño.

## 7. Incidencias, límites y próximos pasos

1. Revisión independiente corrigió una propuesta demasiado amplia: se preservó histórico explícito y se restauró lo provisional. Solo portada genérica cumple B al cierre. No hay objetivo artificial de megabytes.
2. El helper de inspección no compiló inicialmente porque Unity6000.6 marca obsoleto con error `objectReferenceInstanceIDValue`; se utilizó `objectReferenceEntityIdValue` mediante reflexión, sin modificar código del juego. No fue error de compilación runtime.
3. Las respuestas MCP ambiguas se contrastaron con archivos reales y callbacks; timeout/false nunca se convirtieron en PASS por sí solos.
4. No hay cambio funcional que requiera reescribir balance/SDD; se enlaza este informe desde estado/índice y se preserva historia. No había referencias vigentes por nombre a la portada retirada que reemplazar.
5. Pendiente fuera de esta limpieza: corregir fallos de hub/layout/tests legacy en su tarea propia, resolver y validar victoria natural TD, ejecutar partidas completas/acciones animadas y, si se solicita, medir APK Development comparable. Los 30 C y108 D no son autorización para nuevos borrados.

**Cierre:** mapa e inventario cubiertos, limpieza B aplicada y sin regresiones nuevas en checks ejecutados; **no se declara finalización integral de todas las validaciones pedidas**, por fallos previos y cobertura funcional completa no ejecutada. No commit ni push.

### Revisión independiente de cierre

El validador contrastó el reporte con CSV/JSON, ambos grafos, callbacks y capturas: cantidades y categorías coinciden; el único nodo retirado es cover-loading y las 466 filas restantes no cambiaron; las cuatro restauraciones coinciden con sus SHA iniciales. Se verificaron **172 huellas previas sin desviaciones**, con los dos documentos intencionalmente ampliados solo por append y prefijo original intacto. Aprobado el alcance cuantitativo y la distinción entre evidencia y pendientes, **no una aprobación global del producto**.
