# Revisión independiente — La Renga (2026-11-10)

Checkout revisado: `/Users/celestino/HayChoriYPaty`. Alcance: core, progresión/migración, snapshots locales/outbox, configuración y vínculos de identidad/atlas. Sin llamadas Unity ni modificación de runtime/tests. Los logs Roslyn de `/private/tmp/hcyp-larenga-static` fueron proporcionados por el agente principal; exit 0 en compilaciones de runtime, editor, EditMode y PlayMode; esto **no equivale** a importar/compilar/ejecutar en Unity. La prueba solicitada se agotó sin job Unity. `git diff --check` presenta trailing whitespace preexistente/externo en los seis `.meta` de ModularKitchen modificados; no altera este alcance.

## Matriz de aceptación LR1–LR10

| Criterio | Estado independiente | Evidencia / límite |
|---|---|---|
| LR1 | **BLOCKED** | La inspección estática confirma IDs/orden exactos en `StreetSimulation.CampaignOrder` y `LevelNames` (14 IDs, `0,1,9,2,12,3,11,10,13,4,5,6,7,8`). Roslyn compiló las cuatro assemblies (logs indicados arriba), pero la compilación/import real de Unity, la escena/selector y errores de índice/NullReference no se ejecutaron. |
| LR2 | **NOT_RUN** | `LaRengaCampaignTests.CompletingRedondosThenLaRengaUnlocksSuccessorsAndRetriesCannotDuplicateRewards` cubre 10→13, 13→4 y retries/tickets en código; no se ejecutó el test en Unity. |
| LR3 | **NOT_RUN** | `LaRengaCampaignTests.V7MigrationPreservesProgressAndEconomyAndDoesNotInferUnlockFromLegacyFrontierAlone` y `V7MigrationCopiesOldBitsAndUnlocksLaRengaForLaterCampaignAccessOrRedondosVictory` expresan los límites; no se ejecutaron. Inspección directa confirma v7 shape=13 y v8=14, copia estable de 0–12 y `unlockedLevel` legacy limitado a 0–10. |
| LR4 | **NOT_RUN** | `LaRengaCampaignTests.V7OutboxOperationIsByteStableAcrossV8LiveSnapshotAndRestart` cubre UUID/snapshot/hash/revisión del outbox; `LocalSnapshotValidatorAcceptsV7AndV8CampaignArrayShapesWithoutChangingEnvelope` cubre shapes. No se ejecutaron ni se validó recuperación en Unity/Firebase real. No hubo escritura/deploy real. |
| LR5 | **NOT_RUN** | Inspección serializada de `Camioneros-PlazaDeMayo.asset`=120/0.20475, `LosRedondos-Tandil.asset`=60/0.1575 y `LaRenga-AutodromoBuenosAires.asset`=60/0.1575/96; los tres apuntan al motor/config TD existente y perfil económico común. La prueba `LaRengaCampaignTests.LaRengaConfigUsesSharedTwoProductTowerDefenseDefaults` no se ejecutó; sin comprobación runtime de tradicionales/Huracán. |
| LR6 | **NOT_RUN** | `TowerDefensePlacementViewTests.LaRengaAutomaticSixtyFansCanWinWithNormalMovementAndCoinHires` es la cobertura natural de recorrido/victoria; sin ejecución. Doble valla y salida/derrota tampoco verificadas dinámicamente. |
| LR7 | **NOT_RUN** | `TowerDefensePlacementViewTests.LaRengaMixedOrderUsesPhysicalProductsAndCommonCampaignProjection` cubre producción y ambos productos; sin ejecución real de PlayMode. |
| LR8 | **BLOCKED** | Existe familia exclusiva y vínculos estáticos en `ClubVisualTheme.LaRengaFans()` (12 variantes, atlas lateral/frontal/descendente, dos poses laterales, sin fallback de otros clubes). `LaRengaVisualTests` cubre recursos, dimensiones 1536×1024, alpha/importer, rectángulos por pose, recepción/gait y preservación ID9/10; no se ejecutó. La inspección posterior confirma corregidos los metadatos de las cinco texturas: los tres atlas y el mural quedaron como `TextureImporter` (spriteMode 0) sin nombres subasset heredados; el crest conserva spriteMode 1 con subasset `street-larenga-crest-v1_0`. Los cinco GUIDs observados permanecen estables. Resta importación y prueba real de Unity; no se aprueba aceptación visual solo por inspección YAML. |
| LR9 | **BLOCKED** | `TowerDefensePlacementViewTests.LaRengaGameViewShowsBothStaffedAreasFansAndPhysicalThrowsAtThreeResolutions` prepara capturas 540×960, 1080×1920 y 720×1280. No se ejecutó ni hay capturas Game View de La Renga disponibles en `Assets/Screenshots`; no se verifica composición/oclusiones/globos en runtime. |
| LR10 | **NOT_RUN** | PlayMode `LaRengaAutomaticSixtyFansCanWinWithNormalMovementAndCoinHires` afirma que el tutorial La Renga no se activa; no se ejecutó la entrada/salida integrada selector→Mercado→resultado→reingreso. |

## Evidencia puntual

- Contrato aplicable: `specs/features/la-renga.md`, tabla obligatoria LR1–LR10.
- Core inspeccionado: `Assets/Scripts/Core/StreetSimulation.cs`, `StreetGame.cs`, `StreetPlayerStatistics.cs`, `StreetMarket.cs`, `Assets/Scripts/Cloud/PlayerLocalRepository.cs`, `TowerDefenseLevelConfig.cs` y configuración serializada TD.
- Configs ID9/10 no cambiaron velocidad; Camioneros ahora serializa 120/0.20475 conforme al contrato; La Renga 60/0.1575 y spacing 96.
- La migración v7→v8 preserva unlock bits 0–12; abre 13 por accesos previos IDs4–8 o victoria TD no legacy ID10 y no deduce esa victoria de `unlockedLevel==10`.
- El atlas/fondo/escudo PNG están bajo `Assets/Art/Street/Resources/`; tamaños observados con `sips`: mural 2172×724, fan atlases 1536×1024, escudo 1254×1254. Dimensiones no sustituyen validación Unity de importación, transparencia y pivotes.
- Pruebas EditMode nuevas: `LaRengaCampaignTests`, `LaRengaVisualTests`; PlayMode: `TowerDefensePlacementViewTests` (casos LaRenga). Compilación Roslyn de assemblies no prueba que Unity cargó recursos/metas ni ejecutó los casos.

**Conclusión:** no declarar aceptación completa. Hay evidencia estática favorable para core y configuración, pero ninguna aceptación dinámica Unity de LR1–LR10. LR8 requiere importación y prueba de recursos en Unity; LR9 exige capturas reales de las tres resoluciones.
