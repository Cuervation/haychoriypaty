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
- (Resuelto) La fragilidad de Level03/04 y del test de Hub a 1080 era el tamaño del Game view; ver diagnóstico.

## Diagnóstico de tests frágiles (corregido 2026-11-15)
**Causa raíz confirmada:** los tests PlayMode de UI dependen del tamaño del Game view del Editor. Con "Free Aspect" (lo que queda tras reabrir Unity o un layout distinto) `Screen.width` es 1601 o 480 en vez de 1080: fallan `AffordableUpgradeCards...` (1080 vs 1601), el test de Hub a 1080 y las inyecciones de drag del selector (`Level01`, `Level02`, `OperationalSmoke_AllBoys`). Los fallos de Level03/04 en la suite completa eran del mismo tipo de entorno. No era un efecto de la Fase E.
**Verificación:** con el Game view en `1920x1080 Portrait`, `StreetAutonomousCampaignUiTests` + `StreetPointerTests` + `StreetHubViewTests`: 96 tests, 57 pasan, 0 fallan, 39 ignorados (los documentados).
**Corrección aplicada (solo tests; el juego no cambia y debe funcionar en cualquier resolución):** nuevo helper `Assets/Tests/PlayMode/PortraitGameView.cs` que fija el Game view en un portrait fijo de **720x1280** (más chico que la referencia 1080x1920), lo enfoca, espera a que `Screen` lo refleje y lo restaura al terminar. Lo usan `StreetAutonomousCampaignUiTests` (reemplaza `Screen.SetResolution`, que no actúa en el Editor), `StreetHubViewTests` (default 1080x1920 → 720x1280) y `AffordableUpgradeCards...` (ahora compara con el tamaño real del Game view en vez de 1080x1920).
**Verificación en el peor caso (Game view en Free Aspect):** 96 tests de campaña/puntero/hub: solo fallaba el test de capturas, que ya no depende de 1080 y pasa aislado; el resto 0 fallos.
Descartado: el robo de `Mouse.current` por un mouse físico (se probó deshabilitar otros mouse y no cambió el resultado).

## Limpieza de código muerto (2026-11-15)
Se eliminaron ~300 líneas sin referencias en todo `Scripts`/`Tests`/`Editor`: funciones `Draw*Fan` y `*RiotFanFrameSource` por club, `DrawLevelSelectArtwork`, `DrawGrill`, `DrawStation`, `QuitGame`, `ScaleCardChild`, `ScreenBounds`, `CatalogLevelBadge`, `ServingTableBounds`, `MaximumResponsiveFooterPanelHeight`, `FormatBottleStock`, `ChulengoSectorAtPosition`, `HubArtworkActionScreenBounds`, 25 constantes `*FansResource` duplicadas (las texturas siguen cargándose vía `ClubVisualTheme`), campos `*RiotFans` y `MenuPlay/MenuQuit`. No se tocaron los ganchos `*ForTesting` ni `DrawRankingShine`/`DrawPlaySweep`/`DrawHubLayeredEnvironment`/`HubAmbientLayoutBounds`. `StreetView.cs`: 1.871 → 1.791 líneas. Verificación: 322 EditMode OK; 88 PlayMode con solo el fallo de entorno 1080 vs 1601 preexistente.

## Bug del intro (IndexOutOfRangeException en `DrawIntroLogo`)
Evidencia: `Logs/Editor-prev.log` registró 3.680 excepciones `DrawIntroLogo` → `introLogoShines[frame]` en una corrida de tests con "Reload Domain disabled". Con `Mathf.Clamp(x, 0, Length-1)` y `Length == 0`, el índice resulta −1. El array existía pero vacío (estado persistente con la recarga de dominio desactivada); `PrepareIntroLogoShines` lo daba por preparado con solo `!= null`.
Corrección mínima (`StreetView.Intro.cs`): tratar el array vacío como no preparado, exigir `Length > 0` antes de indexar y omitir texturas destruidas. No se reprodujo el fallo antes del cambio en esta sesión, por lo que la causa del array vacío es una inferencia a partir del código y del log; verificado: compila, `StreetHubViewTests` 10/10 y sin errores en consola.
