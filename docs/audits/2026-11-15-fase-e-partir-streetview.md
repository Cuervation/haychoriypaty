# Fase E — partir StreetView.cs (cerrada)

Fecha: 2026-11-15. Movimiento mecánico a archivos `partial class StreetView`; sin cambios de lógica.

| Bloque | Archivo nuevo | Líneas | Commit |
|---|---|---|---|
| Mercado | `StreetView.Market.cs` | ~730 | 8116674 |
| Riot / resultado | `StreetView.Riot.cs` | ~340 | 362379f |
| Pantalla Hub | `StreetView.HubScreen.cs` | ~390 | 48d46c6 |
| HUD de partida | `StreetView.Hud.cs` | ~430 | dba28aa |
| Medallas, ranking, selector | `StreetView.LevelSelector.cs` | ~230 | 77f9d4a |
| Clientes, pedidos, hinchas | `StreetView.Customers.cs` | ~470 | 520446a |
| Victoria, Chulengo, recibo | `StreetView.Victory.cs` | ~780 | (lote 2) |
| Tutoriales de nivel | `StreetView.Tutorials.cs` | ~540 | (lote 2) |
| Tarjetas de mejoras | `StreetView.Upgrades.cs` | ~280 | (lote 2) |
| Introducción | `StreetView.Intro.cs` | ~200 | 6d9ce3b |
| Entrada de puntero y acciones | `StreetView.Input.cs` | ~800 | (lote 3) |
| Fondos, murales, banderines | `StreetView.Backdrop.cs` | ~310 | (lote 3) |
| Botones estándar | `StreetView.StandardButtons.cs` | ~245 | (lote 3) |
| Trabajadores (poses) | `StreetView.Workers.cs` | ~125 | (lote 3) |

`StreetView.cs`: 7.622 → 1.871 líneas (−5.751, −75 %) tras el lote 3; HUD verificado con 30 tests EditMode (StreetSpecialtyPresentation, TowerDefenseScreenLayout).

## Verificación
- Compila sin errores CS tras cada paso.
- PlayMode completo tras Riot: 179/181; Level03_Velez y Level04_Ferro fallaron solo en la corrida completa y pasan aislados, con y sin el cambio (dependen del orden de ejecución).
- Hub: `StreetHubViewTests` 10/10 aislado. `SignsPulseInsideStableHotspotsInTheFullHubGameView` falló en una corrida combinada (1080 vs 480) y pasa aislado con y sin el cambio.
- No se volvió a correr la suite completa tras Hub ni EditMode (esa corrida dejó Unity cerrado).

## Pendiente (fuera de esta fase)
- Más bloques candidatos: HUD (contadores/chips), selector de niveles, upgrades.
- Investigar la fragilidad por orden de ejecución de Level03/04 y del test de Hub a 1080.

## Diagnóstico de tests frágiles (2026-11-15)
- `StreetAutonomousCampaignUiTests` corrida sola: fallaron Level01, Level02 y OperationalSmoke_AllBoys ("Dragging the selector ... scroll position" = 0), con `editor_is_focused: false`. En la suite completa habían fallado otros (Level03/04, "authentic kit or Mercado"). Los fallos cambian de una corrida a otra y todos pasan aislados.
- Causa probable: estos tests inyectan mouse real en el Game view y dependen de foco del Editor, resolución del Game view (1080x1920 vs `Screen.SetResolution(540, 960)`, que no aplica en Editor) y del orden/timing de frames. No es un efecto de los movimientos de código de la Fase E (se reprodujeron con y sin ellos).
- No se modificó ningún test. Una solución real exige rediseñar la inyección de input (p. ej. invocar acciones sin mouse) o fijar el Game view; queda como trabajo aparte.

- Lote 2: `StreetPointerTests.AffordableUpgradeCardsCaptureDoubleWhiteBorderOnlyWithEnoughCoins` falla (1080 vs 1601, tamaño del Game view) con y sin los cambios; es el mismo tipo de fallo de entorno de los otros tests frágiles.

## Cierre
`StreetView.cs` conserva campos/constantes, OnEnable/carga de recursos, música del hub, `DrawGameGui` y helpers pequeños. Seguir partiendo da rendimientos decrecientes: lo restante es mayormente estado compartido (campos) y métodos pequeños acoplados. Verificación del lote 3: compila, 45 EditMode OK, PlayMode (StreetPointer/HubView/MarketView/KitchenRenderer/ActiveCooksPointer) 115 tests sin fallos nuevos (solo el fallo de entorno de 1080 vs 1601, preexistente).

## Limpieza de código muerto (2026-11-15)
Se eliminaron ~300 líneas sin referencias en todo `Scripts`/`Tests`/`Editor`: funciones `Draw*Fan` y `*RiotFanFrameSource` por club, `DrawLevelSelectArtwork`, `DrawGrill`, `DrawStation`, `QuitGame`, `ScaleCardChild`, `ScreenBounds`, `CatalogLevelBadge`, `ServingTableBounds`, `MaximumResponsiveFooterPanelHeight`, `FormatBottleStock`, `ChulengoSectorAtPosition`, `HubArtworkActionScreenBounds`, 25 constantes `*FansResource` duplicadas (las texturas siguen cargándose vía `ClubVisualTheme`), campos `*RiotFans` y `MenuPlay/MenuQuit`. No se tocaron los ganchos `*ForTesting` ni `DrawRankingShine`/`DrawPlaySweep`/`DrawHubLayeredEnvironment`/`HubAmbientLayoutBounds`. `StreetView.cs`: 1.871 → 1.791 líneas. Verificación: 322 EditMode OK; 88 PlayMode con solo el fallo de entorno 1080 vs 1601 preexistente.
