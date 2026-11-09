# Mercado — permisos y contribuciones

Fecha: 2026-11-06
Proyecto: checkout activo `/Users/celestino/HayChoriYPaty`
Unity previsto: 6000.6.3f1

## Resumen

Se agregó una pestaña **PERMISOS** al Mercado existente, con compras individuales de **Habilitación Municipal — $500** y **Contribución Policial — $500**. Son artículos temporales de preparación, no desbloqueos permanentes ni ingredientes; no afectan por ahora jugabilidad, stock o estadísticas. El precio se centraliza en `StreetMarketPreparationCatalog.ItemPriceCents` (50.000 centavos).

## Arte

Dos PNG transparentes originales, con importador configurado como Sprite (`spriteMode: 1`, `textureType: 8`):

![Sello original de habilitación municipal](../../Assets/Resources/ModularKitchen/Art/market-municipal-permit-v1.png)

![Insignia original de contribución policial](../../Assets/Resources/ModularKitchen/Art/market-police-contribution-v1.png)

Los metadatos tienen GUID distintos y las dimensiones de rect declaradas corresponden a las imágenes. La importación real del Asset Database aún no se ha validado en Unity.

## Cambios

- `Assets/Scripts/Core/StreetMarket.cs`: enum/catálogo separado; compra valida artículo, nivel de preparación, saldo y duplicado; descuenta del saldo general y solo cambia los flags correspondientes. No usa el contador acumulado de compras de ingredientes.
- `Assets/Scripts/Core/StreetGame.cs`: wrapper guarda la compra o restaura el snapshot si falla el guardado; las banderas se enlazan a ID estable; cambiar de nivel limpia la preparación anterior sin devolución; se consumen con el guardado de inicio exitoso. Normal/TD usan la transacción existente y Bonus tiene snapshot/rollback explícito.
- `Assets/Scripts/UI/StreetView.cs`: tercera pestaña, iconos en las tarjetas, botones, saldo insuficiente/COMPRADO, debounce, hit-test de tarjetas y footer que explicita que no hay efecto jugable aún. Las tarjetas comparten viewport y scroll del Mercado.
- `Assets/Tests/EditMode/StreetMarketTests.cs`: añadidos seis casos de dominio para compra individual, precio/compra doble, saldo insuficiente, persistencia/cambio de preparación, consumo y carga de un guardado viejo sin estos campos.
- `specs/features/market-fridge.md`: contrato aditivo, estados y ciclo de vida.

## Aceptación

| Criterio | Estado | Evidencia / límite |
|---|---|---|
| Precio único central de $500 por artículo; ambas compras descuentan del saldo general | PASS (estático) | `StreetMarketPreparationCatalog.ItemPriceCents = 50000L`; `TryBuyPreparationPermit` descuenta solo `balanceCents`. La prueba automatizada añadida aún no se ejecutó. |
| Compra duplicada o saldo menor a $500 no debita | PASS (estático) | Guardas de compra y debounce UI en `StreetView`; pruebas EditMode agregadas, ejecución pendiente. |
| Flags aislados de inventario/estadísticas de ingredientes | PASS (estático) | Solo dos bools y `preparationLevelId`; no se modifica stock ni contador de compras de mercadería. |
| Salir/reentrar y recargar conserva la misma preparación | PASS (estático) | Back de Mercado oculta el panel; flags serializados en Market v7 y el nivel se recupera desde `preparationLevelId`. Falta prueba de integración ejecutada. |
| Cambio de preparación limpia estados sin reembolso y revierte si falla guardado | PASS (estático) | `StreetMarket.SelectPreparationLevel`; `StreetGame.SelectLevel` restaura SaveData, flags, selección y simulación ante `Save() == false` (sin prueba de fallo ejecutada). |
| Inicio normal/TD/Bonus consume solo al iniciar durablemente; fallo conserva mediante rollback | PASS (estático) | Snapshot de mercado en `TryStartRoundCore`; snapshot explícito en `TryStartBonusRound`. Falta ejecutar pruebas de fallo/partida real. |
| UI de tercera pestaña, tarjetas, botones e iconos integrada al Mercado | PASS (estático) | Ramas de dibujo/input en `StreetView`; falta compilación y captura real. |
| PNG y metadatos de Sprite existen | PASS (estático) | Imágenes 512×512 y 512×492; PNGs y `.meta` presentes, alpha/transparencia y GUID distintos revisables localmente. Unity AssetDatabase no confirmó importación. |
| EditMode: compras A–D, persistencia, consumo y compatibilidad previa | BLOCKED | Seis pruebas añadidas en `StreetMarketTests.cs`; no ejecutadas: Unity MCP reporta `instance_count: 0` y `editor/state` devuelve `no_unity_session`. |
| PlayMode: debounce, navegación/retorno, dos resoluciones | BLOCKED | Unity MCP no tiene sesión; sin ejecución PlayMode. |
| Game View: dos artículos disponibles, cada uno comprado, ambos comprados, saldo insuficiente, 540×960 y 1080×1920 | BLOCKED | No hay sesión de Unity ni capturas reales; no se presenta una imagen mock como Game View. |
| Regresión de compras de mercadería/objetivos de niveles | NOT_RUN | La lógica existente no se modificó intencionalmente; la suite no pudo ejecutarse. |

## Verificaciones realizadas

- Inspección visual local de los dos PNG originales.
- Inspección de los `.meta`: textura importada como Sprite individual, rect y GUIDs diferenciados.
- `git diff --check` en todo el checkout reporta espacios finales en archivos `.meta` de carnes que ya estaban modificados por trabajo local ajeno a esta tarea. Esos archivos no se alteraron para no tocar cambios ajenos. Ejecutar la misma comprobación limitada a los archivos de permisos después de revisión final.
- Unity: `mcpforunity://instances` reportó cero instancias; `mcpforunity://editor/state` devolvió `no_unity_session`. No se abrió otro Editor ni se sustituyó la prueba visual por un render aproximado.
- No se construyó APK, no se instaló en dispositivo y no se modificó Firebase.

## Pendiente

1. Abrir el proyecto activo en Unity y confirmar importación/compilación.
2. Ejecutar los cinco casos `StreetMarketTests` y regresiones focalizadas de compras/stock.
3. Añadir o ejecutar PlayMode para compra doble, persistencia al salir/entrar, cambios de preparación y responsive.
4. Capturar y revisar Game View a 540×960 y 1080×1920 en los cinco estados pedidos; actualizar esta tabla con resultados y rutas de capturas reales.
