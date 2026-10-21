# Validación independiente — continuación Camioneros / Los Redondos

Revisión read-only de la evidencia terminada. No controlé Unity, no inicié ni simulé rondas y no edité el checkout.

## Resultado comprobado

- `results.tsv`: 46 rondas nuevas, todas `Lost`: Camioneros nivel10 intentos5–25 (21 rondas; sus intentos1–4 previos completan el tope de25) y Los Redondos nivel11 intentos1–25. `DONE.txt` confirma corte por cap; ambos llegaron a25 sin victoria.
- CSV consolidado: 155 filas, 5 `Won` y150 `Lost`. Desglose por nivel: 1–5 una victoria cada uno; 6–11 veinticinco derrotas cada uno. Fuentes: 99 ampliación previa, 46 continuación, 8 Racing TSV previo y2 informe previo en vivo. Así se excluye correctamente el viejo Camioneros5 interrumpido: no se cuenta como derrota ni como ronda completa. El intento nuevo Camioneros5 tiene carpeta/evidencia propia y sí entra en los21.
- Mejores filas observadas coinciden entre resultados y CSV: Camioneros21 vende `213,171,121,112,188,103,121` contra metas `310,220,135,125,255,110,145` (faltante271); Redondos10 `278,229,150,138,255,120,165` contra `345,245,150,140,285,120,165` (faltante115). Equipos/velocidad/mesada/caja también concuerdan con la tabla del informe. Son partidas concretas, no una combinación de máximos.
- Hay46 archivos de inicio,46 finales y2,881 muestras de estado; todas las filas medidas llevan `scale=1`. El intento final indicado por DONE es nivel11 intento25 perdido; no hay incongruencia de intento censurado en este tramo.

## Fidelidad y preservación

- El `controller.cs.txt` revisado opera con `StreetView.DispatchAction`, verifica Play, pausa y `timeScale == 1`, navega por selección normal, paga compras con caja real y abre partidas por Mercado. No contiene `StreetSimulation.Step` ni asignaciones de dinero, stock, seed, metas o equipo. Los TSV de compras documentan UTC, tiempo de juego y cash antes/después; la tabla del Mercado registra acciones normales.
- La semántica corregida de roles está respaldada por `Assets/Scripts/Core/StreetSpecialties.cs:14–17`: 0/1 Parrillero, 2/3 Premium, 4/6 Cocacolero, 5 Fernetero. La nueva política dirige tanto Coca (4) como Cerveza (6) a Cocacolero (acción16) y Fernet (5) a Fernetero (acción21). El informe identifica apropiadamente la clasificación anterior errónea como limitación del auditor, no del juego; por tanto las estrategias previas son resultados reales, pero no una búsqueda óptima.
- El consolidado declara explícitamente que las recomendaciones son experimentos no implementados/no validados y que ni las derrotas prueban imposibilidad ni las mejores trazas garantizan ganar.
- Comparé manifiestos de hashes normalizando su orden: los 28 hashes de C# productivo antes/después son idénticos. El hash de Main es idéntico; `git-before.txt` y `git-after.txt` son idénticos. `preservation.txt` coincide. `restoration.txt` informa Play=false, paused=false, escena original limpia, prefs exactas (3439 chars), escala0, testInit=false y background=true. Restauración leída del log, no reinspeccionada desde el Editor.

## Alcance / límites

La prueba es de Editor en perfil local QA legado desbloqueado; no es Android, campaña fresca, ensayo humano, ni validación de solvencia de campaña nueva. La inspección es de trazas guardadas y capturas seleccionadas, no reproducción visual continua. En consecuencia, el cap sin victoria no establece imposibilidad ni estrategia óptima. No se observaron discrepancias materiales entre el informe, las tablas, las trazas finales y los archivos de preservación/restauración.

## Comprobaciones efectuadas

Parseo independiente de `results.tsv`, `TODAS_LAS_PARTIDAS_COMPLETAS.csv`, estados, compras, iniciales, `policy-overrides.tsv`, capturas presentes, `DONE.txt`, `preservation.txt`, manifiestos SHA-256, `git-before.txt`, `git-after.txt` y `restoration.txt`; contraste estático del controlador y de la asignación de roles en `StreetSpecialties.cs`. No ejecuté suites, Unity ni comandos que modifiquen el proyecto.
