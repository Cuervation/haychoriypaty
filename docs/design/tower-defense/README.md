# Tower Defense — Nivel TD 3: Camioneros (propuesta para elegir)

**Estado del documento:** análisis y previsualizaciones previos a implementación; propuesta histórica, no contrato vigente.

> **Supersedido — 2026-11-03:** las recomendaciones de mantener Tower Defense independiente y fuera de campaña ya no son operativas. Camioneros es la posición visible 11 (ID de campaña estable 9) y su única modalidad jugable es Tower Defense. El runtime de ruta conserva su configuración específica; economía, tickets, guardado, resultado y progresión usan el intento normal de campaña. Se conserva el resto del documento como antecedente. Ver [contrato vigente](../../../specs/features/tower-defense.md) e [informe de integración](../../reports/INTEGRACION_CAMIONEROS_TOWER_DEFENSE.md).

## Hallazgo que condiciona las imágenes

La revisión del checkout activo confirma el tema Camioneros de campaña en `Assets/Scripts/UI/ClubVisualTheme.cs` (índice tradicional 9, nombre `Sindicato de Camioneros / Plaza de Mayo`). Su `MuralResource` apunta a `street-background-camioneros-v1` y su recorte a `(0, .10, 1, .42)`, pero el PNG es una ilustración panorámica de Plaza de Mayo —Casa Rosada, obelisco y banderas—, **no un mural de pared de Camioneros**. No encontré un recurso de mural de Camioneros separado ni un atlas de hinchas de Camioneros. El mural real activo `street-mural-real-v5.png` representa Floresta / All Boys, por lo que no lo reutilicé ni lo hice pasar por el de Camioneros.

Las cinco composiciones usan el recorte temático Camioneros existente, el escudo del club y los hinchas que el tema actual realmente configura (atlas de Ferro). No satisfacen literalmente la petición de un mural original de Camioneros: ese recurso no está disponible en el proyecto. Para resolverla sin falsear la identidad visual hace falta que el usuario provea/identifique el mural, o autorice un recurso nuevo. No se generó ninguno.

## Qué se verificó en código activo

| Área | Evidencia y límite actual |
|---|---|
| Campaña | `StreetSimulation` controla clientes, pedidos, rutas de workers, producción, handoff, ventas y cuotas. Camioneros es `LevelIndex 9` (décimo nivel visible); no debe usarse como ID del nuevo Nivel TD 3. No existe runtime de oleadas, colocación espacial, cobertura, targeting o colisiones de TD. |
| Venta e inventario | `StreetKitchenProduction` conserva unidades físicas con identidad, estado y ubicación. Cocineros producen y trasladan; el Mozo recoge/entrega; la venta se acredita en un handoff válido. Es una referencia conceptual valiosa, no autorización para convertir `StreetSimulation` en autoridad del modo nuevo. |
| Workers | `StreetView.cs`, `StreetWorkerProjection.cs`, `StreetWorkerAppearance.cs` y `StreetWorkerAnimation.cs` dibujan/proyectan workers por ID/estado/posición, con movimiento, cocción, toma y transporte. Se cargan `street-parrillero.png`, `street-parrillero-diagonal-v1.png`, `street-cocacolero-v1.png` y `street-cocacolero-levels3-5-v1.png`; aunque este último nombre dice “levels3-5”, el runtime lo selecciona desde `LevelIndex >= 1` (incluido Camioneros). Premium/Mozo/Fernetero son variantes recoloreadas en memoria, no nuevos personajes. |
| Hinchas | Los atlas de caminar activos incluyen All Boys, Chicago, Vélez, Ferro, Independiente, Racing, San Lorenzo, River y Boca. Camioneros está configurado actualmente con los atlas front/walk/riot de Ferro; no hay atlas de hinchas Camioneros separado. Los PNG reutilizados se mantienen sin redibujar. |
| Policías | `Assets/Art/Street/Resources/StreetHubCharacters/Police/PartsAtlas.png` y `PartsAtlasBounds.json` ofrecen tres composiciones `whole_*` y partes cabeza/torso/piernas. `StreetView.HubCharacterAnimations.cs` y `HubCharacterBehavior.cs` las componen en el hub con miradas/cambios de pose, no con una caminata de patrulla. No hay ciclo de marcha ni rutas policiales listas. |
| Muebles y comida | La cocina modular activa incluye `Station_NormalGrill`, `Station_PremiumGrill`, mesas normal/premium/Fernet y barriles Coca/cerveza; usa sprites de `Assets/Resources/ModularKitchen/Art/` para parrilla vacía, mesa, barriles, bebidas, cortes y sándwiches. `StreetKitchenLayout` calcula ubicaciones predefinidas a partir del catálogo/estado de cocineros (Racing+); no es una cuadrícula libre de torres. |
| Escena y perspectiva | `StreetSceneLayout` y `StreetView` representan una calle frontal levemente elevada en 540×960 lógico; el render arma capas de sprites/UI. `Main.unity` contiene la vista tradicional; esta fase no lo editó. El fondo base de campaña también tiene mostrador; las imágenes son un montaje de concepto, no una escena lista para producción. |
| Recursos históricos | `street-grill-premium-v1`, `street-grill-four-zones-v1`, `street-beer-barrel-v1`, `street-beverage-barrel-v1` y `street-fernet-table-v1` permanecen en la vista legacy, sin callers activos para el render actual de campaña. La propuesta prioriza prefabs/sprites modulares activos, no todo PNG presente en el repo. |

Rutas de referencia: `Assets/Scripts/Core/StreetSimulation.cs`, `StreetKitchenLayout.cs`, `StreetSceneLayout.cs`; `Assets/Scripts/UI/StreetView.cs`, `StreetKitchenRenderer.cs`, `StreetWorkerProjection.cs`, `StreetWorkerAppearance.cs`, `StreetWorkerAnimation.cs`, `ClubVisualTheme.cs`; `Assets/Resources/ModularKitchen/Prefabs/` y `Assets/Resources/ModularKitchen/Art/`.

## Propuesta de arquitectura (no implementada)

1. Crear un dominio/mode nuevo con ID propio de misión, por ejemplo `TowerDefenseMissionId.Camioneros03`, independiente de `StreetSimulation.LevelIndex` y de los guardados/progreso de campaña. Mantener la campaña tradicional intacta.
2. Mantener una definición de mapa con nodos/segmentos de ruta dirigidos, ingreso/salida y zonas de colocación; validar alcance, límites de pantalla, solapamiento con vallas, camino, puestos, otros workers, mural e interfaz. La geometría no se debe derivar de `StreetKitchenLayout`.
3. Mantener pedidos por cliente con hasta dos iconos visibles más contador de productos restantes. Reservar artículos por identidad y tipo; usar producción física y traslado como guía. Los workers siguen moviéndose, preparando, recogiendo y entregando: no se convierten en torres inmóviles que lanzan comida.
4. Separar abastecimiento/estaciones del loop de campaña. El Parrillero produce chori/paty, Premium produce bondiola/vacío, Cocacolero abastece Coca/cerveza, Fernetero prepara Fernet con Coca y Mozo retira producto preparado y lo entrega al hincha. Cada producto entregado se asocia al pedido correcto; el resultado de la venta se evalúa al completar la orden en salida. Fórmula de penalización, premios, vidas, victoria y balance quedan pendientes.
5. Mostrar 3–5 policías ambientales con rutas pequeñas, pausas y orientación. Sin impacto en clientes, pedidos, velocidad, monedas, vidas ni ventas. La composición de partes actual puede ser base visual, pero la caminata y sus poses deben diseñarse/validarse como trabajo nuevo.
6. Mantener alcance semitransparente de bajo contraste y selección/colocación, controles para alcance/velocidad/capacidad, oleadas y HUD propios. Precios actuales solo como referencia; no escribir economía tradicional.
7. Antes de implementar: acordar fuente de mural Camioneros y hinchas Camioneros, elección visual, interacción de placement, persistencia/progreso separado, política de pedidos incompletos y objetivos. Después actualizar el contrato SDD pertinente y dividir validación de unidad/render/placement/performance.

## Cinco previsualizaciones

Todas las imágenes son **montajes externos estáticos** hechos mediante composición directa de PNG originales del checkout más polígonos/líneas/texto de propuesta para ruta, vallas, círculos, señalización e interfaz. No son capturas Unity, no contienen sprites de personajes generados y no prueban animación ni comportamiento. La policía se muestra con poses originales del atlas y trazos/flechas punteados: esos trazos comunican una patrulla futura; no existe movimiento en el PNG.

| Opción | Recorrido y uso del espacio | Policías / puestos | Previsualización 1080×1920 | 1080×2400 |
|---|---|---|---|---|
| 1 — Camino en S | Tres pasadas sencillas con retorno lateral; prioriza lectura y cobertura repetida. | 3 policías en bordes/retorno; puestos agrupados en dos filas abajo. | [PNG](proposals/camioneros-td-opcion-01-1080x1920.png) | [PNG](proposals/camioneros-td-opcion-01-1080x2400.png) |
| 2 — Laberinto | Cinco pasillos paralelos y retornos cortos; mejor cruce de radios, pero muchas vallas/oclusiones. | 3 policías externos; parrillas y bebidas en línea inferior. | [PNG](proposals/camioneros-td-opcion-02-1080x1920.png) | [PNG](proposals/camioneros-td-opcion-02-1080x2400.png) |
| 3 — Perimetral | Un circuito rodea el mapa y deja el centro para puestos; favorece coberturas cruzadas. | 3 policías en accesos/perímetro; cinco instalaciones en el centro. | [PNG](proposals/camioneros-td-opcion-03-1080x1920.png) | [PNG](proposals/camioneros-td-opcion-03-1080x2400.png) |
| 4 — Dos sectores | Dos zonas principales conectadas por el recorrido y estaciones agrupadas por función. | 3 policías en conectores/bordes; Parrillero, Premium y Mozo a la izquierda; Coca y Fernet a la derecha. | [PNG](proposals/camioneros-td-opcion-04-1080x1920.png) | [PNG](proposals/camioneros-td-opcion-04-1080x2400.png) |
| 5 — Cruces y atajos | Pasillos de distinta longitud y cambios de dirección; alta variedad y complejidad de lectura. | 3 policías fuera de la ruta principal; cinco estaciones en distribución mixta. | [PNG](proposals/camioneros-td-opcion-05-1080x1920.png) | [PNG](proposals/camioneros-td-opcion-05-1080x2400.png) |

## Evaluación comparativa (1–10)

Los valores son estimaciones de diseño técnico, no mediciones de FPS ni pruebas de aceptación. En rendimiento, 10 significa menor costo esperado. En complejidad, 10 significa implementación más simple.

| Opción | Identidad visual | Jugabilidad | Alcance | Policías | Visibilidad | Espacio | Mobiliario | Rendimiento Android | Complejidad | Rejugabilidad | Media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 — S | 9 | 7 | 8 | 8 | 9 | 7 | 8 | 9 | 8 | 6 | **7,9** |
| 2 — Laberinto | 9 | 9 | 10 | 8 | 6 | 5 | 6 | 7 | 4 | 9 | **7,3** |
| 3 — Perimetral | 9 | 7 | 7 | 8 | 9 | 9 | 8 | 9 | 8 | 7 | **8,1** |
| 4 — Sectores | 9 | 8 | 8 | 9 | 8 | 8 | 8 | 8 | 6 | 8 | **8,0** |
| 5 — Avanzado | 9 | 10 | 10 | 8 | 6 | 7 | 7 | 7 | 4 | 10 | **7,8** |

### Recomendación

**Opción 3 — Circuito perimetral** es la recomendación para seguir diseñando: tiene la media más alta, mantiene lectura clara y ofrece espacio central genuino para instalaciones sin poner a todos los trabajadores sobre el recorrido. Su desventaja es que la primera versión exige validar acceso al centro, balance de coberturas y orden visual de cinco radios superpuestos. Para reducir riesgo, tomaría de la opción 1 la simplicidad de señalización/flujo y de la 4 la agrupación semántica comida/bebida. No recomiendo empezar con la 2 o 5 como primer mapa: ambas maximizan estrategia, pero aumentan coste de navegación, oclusión, pruebas de colocación y dificultad de leer pedidos en móvil.

## Inventario gráfico de la propuesta

**PNG originales reutilizados:** escudo `street-camioneros-crest-v1`; ambiente `street-background-open-street-v4` y recurso temático activo `street-background-camioneros-v1`; atlas walk `street-allboys-fans-walk-v1`, `street-chicago-fans-walk-v1`, `street-velez-fans-walk-v1`, `street-ferro-fans-walk-v2`, `street-independiente-fans-walk-v2`, `street-racing-fans-paired-v1`, `street-sanlorenzo-fans-paired-v1`, `street-river-fans-paired-v1` y `street-boca-fans-paired-v1` (Camioneros usa el Ferro configurado por runtime); workers `street-parrillero.png`, `street-cocacolero-levels3-5-v1.png` y policía `PartsAtlas.png`; arte modular `empty-grill.png`, `empty-table.png`, `food-table-folding-v2.png`, `blue-barrel.png`, `coca.png`, `beer.png`, cortes y `sandwich-chori.png`/`sandwich-paty.png`. Premium/Mozo/Fernetero se muestran como variantes conceptuales de la apariencia que el runtime recolorea en memoria; no se agregaron personajes nuevos. Las propuestas muestran cortes, bebidas y sándwiches sobre el borde de los puestos para que se entienda su función.

**Arte vectorial nuevo en el montaje, no guardado como recursos del juego:** railes y postes de valla, carril/guía de recorrido, flechas, chips ingreso/salida, radios de alcance, selección, botones de mejora y HUD TD. Para producción se necesitaría convertir solo los elementos aprobados a elementos de UI/prefab con el estilo y escala existentes. La caminata/patrulla policial, ubicaciones válidas y animaciones no están resueltas por esta lámina.

## Riesgos y decisiones pendientes

- Falta el mural original específico de Camioneros; la panorámica activa de Plaza de Mayo no es equivalente. No sustituirla silenciosamente por el mural All Boys ni crear arte sin aprobación.
- No existe atlas Camioneros propio: la campaña muestra hinchas de Ferro para ese nivel. Confirmar si se acepta conservar esa configuración para el modo nuevo.
- El atlas policial permite composición/poses, no caminar. Cada escena requiere animación y lógica nuevas, con policías fuera de los caminos como ambiente solamente.
- Las previews heredan el fondo/mostrador de la lámina base; el layout de calle del modo nuevo tendría que definir qué elementos del fondo permanecen y dónde cabe el puesto, sin tapar mural, pedidos ni entrada/salida.
- Aún no se definieron: oleadas, frecuencia/velocidades, capacidad/reserva, política y penalización por pedidos parciales, vidas/fin de partida, precios/recompensas, colocación/celdas, guardado independiente, tutorial y HUD final.
- Verificar en dispositivo Android la legibilidad, overdraw/radios, cantidad simultánea de personajes/atlas, draw calls y carga de texturas antes de comprometer rendimiento. Los puntajes no sustituyen esa prueba.

## Validación realizada

- Revisión estática del checkout vigente, contratos pertinentes y fuentes de assets activos; Unity `6000.6.3f1`.
- Diez PNG compuestos/exportados, con dimensiones comprobables `1080×1920` y `1080×2400`; inspección visual de las cinco variantes.
- El Unity MCP respondió al estado del proyecto con timeout tras intentos razonables; no se reinició ni se tocó el editor. Por eso los resultados no se describen como capturas Unity ni hubo validación de runtime.
- Los únicos archivos agregados por esta fase son esta documentación y `proposals/*.png`. El worktree ya contenía cambios locales previos (entre ellos `Main.unity`, `StreetSimulation.cs` y configuración); se preservaron y no se atribuyen a Tower Defense. Esta fase no los editó ni cambió scripts, escenas, assets del juego, economía, guardados o estado de features SDD.

**Estado final de esta fase: detenerse y esperar elección. No implementar hasta aprobación del usuario y resolver el mural Camioneros.**
