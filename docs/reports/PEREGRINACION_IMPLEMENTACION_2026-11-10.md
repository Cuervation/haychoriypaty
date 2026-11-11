# Peregrinación a Luján — Moreno: implementación y validación pendiente

2026-11-10, checkout `/Users/celestino/HayChoriYPaty`, Unity6000.6.3f1. **Trabajo incorporado en desarrollo; no aceptado como nivel completo.** Ningún commit/push, APK, deploy Firebase ni perfil real modificado. Conservado trabajo local previo.

## Identidad y campaña

La Renga ID13 existe en el repositorio, por lo que se asignó el nuevo **ID14**, posición10, después de13 y antes de Independiente4. Orden15: `0,1,9,2,12,3,11,10,13,14,4,5,6,7,8`. Nombre exacto `Peregrinación a Luján — Moreno`. IDs anteriores conservados; Camioneros120/.20475, Redondos60/.1575 y La Renga60/.1575 no se rebalancearon.

SaveData9 migra8 copiando bits0–13 y abre14 por niveles posteriores4–8 ya abiertos o victoria/pending13. Las versiones1–7 atraviesan sus migraciones previas. Frontera legacy0–10 no cambia. Estadísticas/Mercado15buckets; validator admite snapshotv7=13/v8=14/v9=15, envelope1. Outbox anterior no se reescribe. Equipamiento tiene gasto propio, no cuenta como venta ni hire. Reglas locales admiten14/campo equipment opcional para compatibilidad; sin desplegar.

## Variante TD y presentación

Extensión opt-in del mismo `TowerDefenseGameMode`, no motor/escena/cocina nuevos. Ruta horizontal de dos extremos y dirección izquierda→derecha; generador reutiliza piezas del vallado pero sólo construye el riel inferior y su collider. Config propia `TowerDefense/Peregrinacion-LujanMoreno`. El runtime anterior de dos áreas sigue exclusivo de TD serpenteantes.

Superficie construible debajo del riel, grilla lógica invisible, preview verde/rojo y rechazo/motivo de posiciones. UI dentro del bloque inferior azul original: pestañas trabajadores/equipamiento, páginas de tres cards, pausa/salida. Siete elementos independientes reutilizan prefabs tradicionales: parrilla normal/Premium, mesa normal/Premium, Fernet, Coca, cerveza. Cinco roles y catálogo7 con IDs estables (5Fernet,6cerveza). Se reutilizan Kitchen/reservas/unidades reales, frames y productos anteriores; colocación no crea ventas ni stock. Globo ID14 anclado al límite superior real del sprite con6px libres.

## Arte propio

[Procedencia y prompts exactos](../design/peregrinacion/ART_PROVENANCE.md), manifest SHA-256 adyacente. Cuatro PNG nuevos, sin reemplazar arte previo:

- `street-background-peregrinacion-moreno-v1`:2172×724, ilustración original a partir de la foto autorizada; avenida/multitud/postes/árboles/puestos/cartelLUJÁN, titularPEREGRINACIÓN LUJÁN y cartelMORENO secundario. La Basílica no se trasladó al paisaje de Moreno.
- `street-peregrinacion-crest-v1`:1254², ilustración original de la Basílica de Luján.
- `street-peregrinacion-fans-walk-v1` y `front-v1`:1536×1024, grilla6×4, doce identidades en pares; caminata derecha con dos pasos, espera/recepción y salida compartida. Mochilas, gorras, botellas, ropa deportiva diversa; no camisetas de fútbol recicladas.

Inspección directa PNG: alpha cero50.67% lateral y51.26% frontal, esquinas transparentes (frontal una esquinaalpha1). Esto no certifica importación/pivote ni composición Game View. Texturas finales y metas propios en `Assets/Art/Street/Resources/`.

## Balance inicial provisional — NO certificado ganable

60 peregrinos, velocidad.035 (base300→10.5logicalpx/s), separación96px, intervalo mínimo9s, caja1200, cero empleados, primera unidad de cada equipo gratis y adicionales80; primer hire60 porrol, máximo3porrol. Demanda gradual: primeros6 básicos0/4, siguientes12 básicos0/1/4/6, después catálogo completo con hasta dos líneas de una unidad.

No se copió la velocidad serpenteante: la ruta corta exige más tiempo por peregrino. Estaciones iniciales gratis evitan imposibilidad económica de adquirir equipamiento indispensable; la caja permite contratar los cinco roles. La grilla y footprints se ajustaron para la anchura lógica real540 y el pivote de pie. Estos razonamientos no sustituyen simulaciones legales completas ni certifican dificultad. **No afirmar ganabilidad hasta ejecutar y revisar los casos automáticos.**

## Archivos principales

- `Assets/Scripts/TowerDefense/TowerDefenseGameMode{,.Peregrinacion}.cs`: extensión dominio/colocación/servicio; config, geometría y responsive del mismo módulo; generador del vallado.
- `Assets/Scripts/TowerDefense/TowerDefenseWorldPresentation{,.Peregrinacion}.cs`: muebles colocados, productos físicos/carga, poses y lanzamientos de siete productos.
- `Assets/Scripts/UI/StreetView{,.TowerDefense,.Peregrinacion,.LevelSelectorPresentation}.cs`, `ClubVisualTheme.cs`: UI/identidad sin rediseño global.
- Core `StreetSimulation`, `StreetGame`, `StreetPlayerStatistics`, `StreetMarket`; `Cloud/PlayerLocalRepository`; `Main.unity` arrays15; reglas locales.
- `Assets/Tests/EditMode/PeregrinacionCampaignTests.cs`, `PeregrinacionGameModeTests.cs`, regresiones de conteo/sucesor actualizadas; nuevos casos Peregrinacion en `Assets/Tests/PlayMode/TowerDefensePlacementViewTests.cs`.
- [Contrato PL1–PL10](../../specs/features/peregrinacion-lujan.md), [revisión independiente](PEREGRINACION_REVISION_INDEPENDIENTE_2026-11-10.md), [balance maestro](../BALANCE_ACTUAL_NIVELES.md).

Los archivos compartidos tenían modificaciones previas; no se atribuye todo su diff a este pedido. No se limpiaron escenas de prueba/metas ajenos ni preferencias globales.

## Evidencia y bloqueo

- Compilación estática de cuatro assemblies mediante Roslyn del SDK Unity y referencias Bee: salida temporal `/private/tmp/hcyp-peregrinacion-static/`. No abre otro Editor ni escribe Library. Los logs finales se conservan en `Logs/Acceptance/Peregrinacion-20261110/` (ignorados por Git).
- Pruebas escritas de orden/migración/shapes/identidad y PlayMode de construcción, ola natural, derrota/pausa y capturas; **ejecución Unity NOT_RUN**.
- Nueva consulta inicial sólo lectura `project/info` devolvió `Unity plugin session ... disconnected while awaiting command_result`; `editor/state` falló `ping not answered`. La sesión anterior ya agotó tres reconexiones acotadas sin recuperar Unity. No se repitieron ciclos, no se reinició/cerró Editor, no se alteró trust ni se abrió otro proceso Unity.
- Importación/compilación del Editor **BLOCKED**; Game View real540×960/1080×1920/720×1280 **BLOCKED**. No existen capturas nuevas de este nivel y no se sustituyen por imágenes generadas.
- Firebase real, APK/dispositivo: **NOT_RUN**, sin publicación autorizada.

La matriz independiente enumera criterios pendientes y defectos concretos si los hay; no se declara aceptación completa por éxito Roslyn. Recuperar Editor/MCP (reinicio sólo con autorización), importar assets finales, ejecutar EditMode/PlayMode focalizados, partidas naturales legales y las tres Game Views. Comprobar unidad por unidad, navegación/obstáculos, contabilidad/settlement, save/reopen y regresiones previas. Corregir únicamente criterios demostrados y repetir sus pruebas.

## Continuación: corrección visual y cierre parcial

- Petición posterior atendida: mural **PEREGRINACIÓN LUJÁN**, avenida con fuga hacia el horizonte derecho y todos los caminantes hacia el destino/señalLUJÁN→. MORENO sólo queda como cartel de ubicación secundaria. Se reemplazó el PNG a pedido manteniendo GUID; [prompt de revisión](../design/peregrinacion/MURAL_DIRECTION_REVISION_PROMPT.md). Inspección visual directa del PNG, no Game View.
- Ciclos de corrección estática: cocinero ahora lleva el ID físico a la mesa antes de depositar; BFS valida extremos exactos y barrido del cuerpo; compra recalcula anclas; resize conserva offsets lógicos/unidades y rechaza layouts que no caben, pausando. La vista distribuye unidades por producto/capacidad, no en cuatro posiciones superpuestas.
- Cuatro assemblies finales compilan mediante Roslyn **PASS estático**; `git diff --check` focalizado **PASS**. No tests Unity confirmados. La consulta nueva a MCP ahora falla a nivel transporte HTTP `127.0.0.1:8080/mcp`, antes de poder consultar proyecto/console. Se pidió permiso explícito para cerrar/reabrir únicamente el Editor si es necesario; **no se reinició**.

### Trabajo funcional todavía pendiente — implementación NO completa

La segunda estación de un mismo tipo puede colocarse/comprarse, pero la selección `FindPlacedStation`/`RoleStation` y la proyección del stock todavía priorizan la primera; falta asignación operativa efectiva de múltiples estaciones. También falta demostrar y, si corresponde, resolver congestión entre trabajadores que comparten accesos. El pack propuesto y la estrategia automática legal no se ejecutaron: **no está certificado ganable ni apto para entrega**. Las pruebas por rol individual no demuestran coexistencia de los cinco; ésta pertenece a PlayMode.

Tras dos ciclos acotados no se prolongó un bucle de cambios sin evidencia dinámica. Se conserva el desarrollo local y se declara el pendiente; no se publicó. Próxima sesión: resolver asignación de estaciones múltiples, recuperar Unity/MCP con autorización donde corresponda, ejecutar los casos focalizados y corregir los fallos observados, incluyendo cocina→mesa→Mozo, ola60, pérdida, pausa/reflow y las tres Game Views. Ninguna captura generada sustituye esas pruebas.
