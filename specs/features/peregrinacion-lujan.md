# Peregrinación a Luján — Moreno (ID14)

**Contrato nuevo — 2026-11-10. Estado: implementación parcial; asignación multiequipo incompleta y aceptación Unity pendiente.** Delta exclusivo de ID14 sobre el motor [Tower Defense](tower-defense.md), [cocina activa](active-cooks-mozos.md), [Mercado](market-fridge.md) y [persistencia](firebase-player.md). No es una copia del serpenteo de los otros TD ni un segundo motor. La compilación no demuestra aceptación.

## Identidad, campaña y compatibilidad

- Nombre exacto `Peregrinación a Luján — Moreno`, ID estable **14**, posición visible **10**. Se verificó La Renga ID13 incorporada en código/config; la campaña pasa a quince posiciones: `0,1,9,2,12,3,11,10,13,14,4,5,6,7,8`.
- Ganar13 desbloquea14; ganar14 desbloquea4 por el cierre común idempotente. Preservar significado y unlocks de todos los IDs anteriores. Camioneros conserva120/.20475, Los Redondos y La Renga60/.1575; no retocar tradicionales/Huracán.
- Guardado vivo **v8→v9**, unlocks/estadísticas15 entradas copiando índices0–13 sin reordenar. Nuevos perfiles14cerrado. Migración abre14 si ya estaba abierto cualquier ID4–8 o hay victoria13 no-legacy; no inferirlo de la frontera `unlockedLevel` legacy0–10. Conservar victoria pendiente y premios deduplicados. Repetir carga/migración no cambia estado.
- Envelope local/remoto conserva schema1; snapshots antiguos conservan su propia forma (v7=13,v8=14,v9=15). No migrar/rehashear bytes de operaciones ya encoladas: UUID/hash/revisión/payload son inmutables. No perder stock, tickets, saldo, recibos, partidas pendientes o registros. Validación de match admite14 sin despliegue remoto.

## Geometría y construcción: requisito excluyente

- Configuración opt-in horizontal de **una fila**, dos extremos, `x` estrictamente creciente y `y` constante. Entrada progresiva por borde izquierdo; salida por borde derecho; nunca retorno, conector, giro, segunda fila ni circuito. Reusar follower/espaciado/fin de ola del TD.
- **Única valla modular horizontal debajo del camino**, de extremo a extremo, usando prefabs existentes. Ni borde superior vallado ni valla junto al panel. Collider y dibujo deben coincidir; tolerar solapamiento de cuadros transparentes sólo para unir visualmente rieles, no paneles duplicados visibles.
- Jerarquía portrait: HUD, mural Moreno, camino, valla, área de trabajo continua, panel azul. Toda la superficie útil inferior hasta el panel es construible mediante grilla invisible. Sin división operativa en las dos áreas heredadas.
- Siete tipos colocables, con sprites existentes: parrilla normal, parrilla premium, mesa normal, mesa premium, mesa Fernet, barril Coca y barril cerveza. Seleccionar tarjeta y tocar posición; mostrar preview válido/inválido y motivo. No consumir monedas ni crear objetos por intento rechazado. Compra y alta del objeto son una operación atómica.
- Footprints completos y espacios de acceso no invaden valla, peregrinos, límites o HUD. Comprobar muebles, trabajadores y **conectividad** de accesos/puntos de servicio; una colocación no puede aislar trabajadores ni bloquear estaciones existentes. Workers sólo en área inferior, rutas válidas evitando muebles y reserva/espera de paso que evita superposiciones. No reemplazar movimiento real por teletransportación o workers fijos que producen a distancia.
- Reflow responsive transforma anchors/ocupación manteniendo validez y progreso; no recrea caja, unidades, empleados ni pedidos.

## Producción y servicio: siete IDs existentes

IDs exactos: **0 Chori,1 Paty,2 Bondiola,3 Vacío,4 Coca,5 Fernet con Coca,6 cerveza**. El orden textual del pedido no renumera5/6. Precios oficiales, recetas, capacidades/tiempos base y sprites continúan siendo autoridad del proyecto.

| Rol disponible | Producción / servicio |
|---|---|
| Parrillero | Cocina0/1 en parrilla normal, transporta y deposita en mesa normal |
| Parrillero Premium | Cocina2/3 y deposita en mesa premium |
| Mozo | Reserva y retira sandwiches0–3 de su mesa, camina al punto inferior de atención y lanza por encima de valla |
| Cocacolero | Prepara/retira Coca4 y cerveza6 en sus barriles, camina y lanza |
| Fernetero | Prepara/retira Fernet5 en mesa existente, camina y lanza |

- Reusar **una** `StreetKitchenProduction` y unidades `StreetFoodUnit` con IDs únicos. Cocina activa con separación cocinero/Mozo; no crear producción, inventario o cobro paralelos. Cocinar exige estación y acceso reales; beber exige estación/rol real. Estación ausente impide preparar/retirar su producto.
- APIs existentes: `ReserveCook→AdvanceCook→TakeCooked→DepositCooked`; Mozo `ReserveTableUnit→TryTake`; entrega válida `Consume` una sola vez. Cancelar/abandonar libera reservas o devuelve unidad mediante APIs existentes. Las animaciones sin ID físico no generan stock, entregas ni ventas.
- Cada recibo físico producto/unidad/trabajador/peregrino origina un lanzamiento visible y una sola actualización de pedido, monedas, contadores y estadísticas. Prioridad por avance/urgencia y reservas evitan doble retiro/cobro. Recepción/salida existentes liberan al atendido; llegada con pedido incompleto causa derrota inmediata.
- Catálogo0–6, pedidos simples o mixtos, iconos/cantidades/globos largos existentes. Globo unos px sobre bounds reales de cabeza sin taparla. Todo producto puede aparecer en la ola completa; onboarding gradual no debe permitir ganar evitando para siempre un rol o producto.
- Sin deadline, cuotas tradicionales ni paciencia. Victoria sólo tras atender la ola finita y finalizar salidas, no por reloj.

## Economía inicial y Mercado

Perfil exclusivo ID14: no mutar `TowerDefenseEconomyProfile` compartido de9/10/13. Cobros usan `OfficialProductPrice`, contrataciones usan fórmula común con parámetros específicos. Máximos, costos por rol/equipo, caja, velocidad, intervalo y demanda deben quedar en config legible, no en arte/UI.

**Configuración inicial implementada, NO balance validado:** 60 peregrinos; factor velocidad `.035` (con base300 son10.5px lógicos/s), separación96px y mínimo9s entre entradas;1200monedas y0trabajadores; primera unidad de cada uno de los siete equipos gratuita, adicionales80; primer empleado60 por rol, máximo3 por rol. Demanda: primeros6 pedidos1unidad de0/4, siguientes12 de0/1/4/6, restantes todos0–6 con hasta2líneas de1unidad. Objetivo: tiempo inicial para construir y ausencia de softlock de estación indispensable. Mantener estos valores como perfil provisional hasta contrastar simulaciones legales; registrar sólo ajustes efectivamente realizados con motivo/evidencia en informe de balance.

- Sigue la política TD actual: sin gate/consumo de ingredientes Mercado durante el intento, pero sí preparación, capacidades, unidades y handoff físicos. No ligar simultáneamente `IStreetKitchenStock` y contabilización externa para el mismo alimento. No cambiar reglas de stock de los otros modos. Coca/cerveza/Fernet preservan su carácter inagotable cuando habilitados; no inventar receta ni octavo producto.
- Mercado, reserva/liquidación de tickets, resultado, derrota y recuperación de abandono usan la vía común de `StreetGame`. Sólo un settlement y un premio por intento/operación; el gasto de equipamiento debe reconocerse de forma explícita en la caja final sin duplicarlo como contratación.
- No importar empleados persistentes gratis, ni añadir monedas de debug. Las simulaciones de balance compran/colocan/contratan legalmente desde caja real sin alterar reloj, delta, velocidad o movimiento. Deben ejercitar los cinco roles y siete productos.

## Presentación e identidad visual

- Referencia suministrada `codex-clipboard-10544268-1a56-48ff-9515-3c76e45acddc.png`: caminata popular sobre avenida de Moreno, árboles/postes/comercios/puestos y cartelería hacia Luján. Crear mural2D original coherente con arte actual, no pegar foto ni representar la llegada a la Basílica. Corrección visual del usuario: titular `PEREGRINACIÓN LUJÁN`, perspectiva de avenida, caminantes y flecha hacia la derecha; Moreno sigue siendo la ubicación del paisaje. Mantener puesto de comidas, vereda/calle y perspectiva del juego.
- Logo/insignia propio representa **Basílica de Luján**; su motivo no convierte el paisaje de Moreno en la plaza de llegada.
- Atlas propios transparentes siguiendo grilla/pivote/proporción actuales; variedad de peregrinos con mochilas, gorras, botellas, pañuelos, pequeñas banderas y ropa deportiva no exclusivamente futbolera. Identidades coherentes en caminata lateral derecha, espera, recepción y salida; no fallback silencioso a hinchas ajenos. Respeto cultural/religioso.
- Conservar bloque inferior azul, cards, botones, tipografías, iconografía, monedas, pausa/navegación/resultados. Pestañas `Personal`/`Equipamiento` dentro del panel existente si hace falta; no ampliar panel sobre área de trabajo. Mínimos touch cómodos y preview no oculta coste/validez.

## Extensiones mínimas / límites técnicos

1. `TowerDefenseLevelConfig`, `TowerDefenseRouteGeometry`, `TowerDefenseResponsiveLayout`: selector explícito de ruta/construcción opt-in; validar1fila sólo en variante horizontal y mantener3–9impares anterior. Evitar división por `RowCount-1`. Exponer bounds construibles/centrolinea y anclas derivados de viewport.
2. `TowerDefenseModularFenceGenerator`: variante una sola frontera inferior; preservar dos fronteras anteriores.
3. `TowerDefenseGameMode`: retirar hardcodes0/4 sólo para14; catálogo/demanda, todosroles, equipamiento/ocupación/rutas y actividad física de cada estación. Mantener runtime/phase/follower/receipts/results comunes. Exponer estado de objetos colocados (ID/tipo/bounds/anchors), preview/motivo, compra/colocación/contratación atómicas y worker path/foodID para vista y tests.
4. `StreetKitchenProduction` ya conoce7productos/cinco roles; reusar sus APIs y `StreetSpecialties` como autoridad. `StreetKitchenLayout` tradicional tiene anchors absolutos: reutilizar dimensiones/footprints/slots, no imponer su disposición fija a construcción libre. Extraer una transformación pequeña de anchors/obstáculos si se necesita, sin segundo motor de cocina.
5. `TowerDefenseWorldPresentation` hoy limita estaciones/displays/receipts a0/4; ampliar por catálogo/objetos colocados sólo14. `StreetView.TowerDefense` hoy sólo2cards/doszonas; adaptar input/preview/pestañas para14 preservando9/10/13. Reusar frames existentes de cinco roles y productos; `ClubVisualTheme` sólo nuevos recursos.
6. Campaña, `StreetGame`, estadísticas, local repository, `Main.unity`, mercado y reglas Firebase: extensiones aditivas por ID, arrays y migraciones; ninguna escena paralela ni renumeración.

## Aceptación obligatoria

Cada fila es obligatoria. Estados permitidos: `PASS`, `FAIL`, `BLOCKED`, `NOT_RUN`, `NOT_APPLICABLE` con motivo. Prueba escrita o compilación estática no es una ejecución Unity ni evidencia visual.

| ID | Criterio verificable | Estado inicial |
|---|---|---|
| PL1 | Unity compila; selector15posiciones/IDs/nombres/catálogos;13→14→4; settlement/replay no duplica premios/tickets | `BLOCKED` — Editor/MCP sin respuesta |
| PL2 | EditMode migra v8→v9 y cargas viejas, preserva unlocks0–13/economía/stock/tickets/estadísticas;14apertura correcta; segunda carga idéntica; outbox antiguo byte/hashestable; reabrir/recover | `NOT_RUN` |
| PL3 | Geometría sólo2extremos con xcreciente/yconstante; único collider/riel inferior modular unido; entradaizquierda/salidaderecha; ninguna segunda valla/giro/retorno;9/10/13 geometría intacta | `NOT_RUN` |
| PL4 | Input real selecciona/preview/coloca cada7tipo; rechaza solape, valla, HUD, fuera de bounds y cierre de accesos sin cobrar; múltiples equipos y empleados coexisten con rutas válidas | `FAIL` — extras comprables sin asignación operativa; resto dinámico no ejecutado |
| PL5 | Cinco roles contratables con costos/límites/caja reales; workers sólo abajo, rodean obstáculos sin solaparse; parrilla→mesa→Mozo y bebidas→servicio recorren distancias reales | `FAIL` — asignación siempre al primer equipo del tipo; movimiento multiworker no ejecutado |
| PL6 | Siete productos y pedidos mixtos con IDs/precios/recetas intactos; cook/reserva/retirada/consume/recepción trazables por unidad única; rechazo/pausa/cancelación sin duplicación/pérdida | `NOT_RUN` |
| PL7 | Partidas completas automáticas con compra/colocación/contratación progresiva legal y todosroles/productos;60servidos/victoria; estrategia insuficiente derrota en salida; ninguna moneda/velocidad/reloj artificial; ajustes documentados | `NOT_RUN` |
| PL8 | Mercado→intento→resultado→salir/reingresar, pausa/continuar, derrota/abandono, tickets/estadísticas/guardado local integrados; política stock TD sin consumo doble; reglas localesID14 | `NOT_RUN` |
| PL9 | Game Views reales540×960,1080×1920,720×1280 con peregrinos circulando, múltiples equipos/workers/lanzamientos; muralMoreno/logoBasílica/atlaspropios; HUDtáctil intacto, globos no tapan y cero solapes | `BLOCKED` — ninguna captura real disponible |
| PL10 | Regresión focalizada mantiene balances/producción/tutorial/settlement de9/10/13 y tradicional/Huracán representativos; ninguna modificación global no autorizada | `NOT_RUN` |

Evidencia independiente debe identificar nombres/jobs/resultados/capturas y distinguir estático/dinámico. Ante bloqueo Unity, registrarlo sin relabelar pruebas como aprobadas. Límite de dos ciclos de corrección; no reiniciar/cerrar Editor ni procesos sin autorización. Sin commit, push, APK, publicación o despliegue Firebase sin permiso. Ver [revisión independiente](../../docs/reports/PEREGRINACION_REVISION_INDEPENDIENTE_2026-11-10.md) para alcance y límites estáticos. No declarar implementación completa mientras una fila obligatoria sea `FAIL`, `BLOCKED` o `NOT_RUN`.

## Checkpoint de entrega parcial

Mural corregido a petición posterior: titularPEREGRINACIÓN LUJÁN, perspectiva y caminantes hacia señal derecha. Compilación Roslyn de cuatro assemblies PASS estático y diff check focalizado PASS; cero pruebas Unity/capturas nuevas confirmadas. Transporte MCP desconectado. Asignación de varias estaciones iguales todavía prioriza la primera; congestión multiworker y ganabilidad natural requieren resolver/verificar en Unity. No declarar implementación completa. [Informe](../../docs/reports/PEREGRINACION_IMPLEMENTACION_2026-11-10.md) y [revisión independiente](../../docs/reports/PEREGRINACION_REVISION_INDEPENDIENTE_2026-11-10.md).
