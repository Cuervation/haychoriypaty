# Fase E — partir StreetView.cs (cerrada)

Fecha: 2026-11-15. Movimiento mecánico a archivos `partial class StreetView`; sin cambios de lógica.

| Bloque | Archivo nuevo | Líneas | Commit |
|---|---|---|---|
| Mercado | `StreetView.Market.cs` | ~730 | 8116674 |
| Riot / resultado | `StreetView.Riot.cs` | ~340 | 362379f |
| Pantalla Hub | `StreetView.HubScreen.cs` | ~390 | 48d46c6 |
| HUD de partida | `StreetView.Hud.cs` | ~430 | (ver git log) |

`StreetView.cs`: 7.622 → 5.761 líneas (−1.861) tras agregar HUD; HUD verificado con 30 tests EditMode (StreetSpecialtyPresentation, TowerDefenseScreenLayout).

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
