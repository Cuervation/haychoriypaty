# Fase E — partir StreetView.cs (cerrada)

Fecha: 2026-11-15. Movimiento mecánico a archivos `partial class StreetView`; sin cambios de lógica.

| Bloque | Archivo nuevo | Líneas | Commit |
|---|---|---|---|
| Mercado | `StreetView.Market.cs` | ~730 | 8116674 |
| Riot / resultado | `StreetView.Riot.cs` | ~340 | 362379f |
| Pantalla Hub | `StreetView.HubScreen.cs` | ~390 | 48d46c6 |

`StreetView.cs`: 7.622 → 6.190 líneas (−1.432).

## Verificación
- Compila sin errores CS tras cada paso.
- PlayMode completo tras Riot: 179/181; Level03_Velez y Level04_Ferro fallaron solo en la corrida completa y pasan aislados, con y sin el cambio (dependen del orden de ejecución).
- Hub: `StreetHubViewTests` 10/10 aislado. `SignsPulseInsideStableHotspotsInTheFullHubGameView` falló en una corrida combinada (1080 vs 480) y pasa aislado con y sin el cambio.
- No se volvió a correr la suite completa tras Hub ni EditMode (esa corrida dejó Unity cerrado).

## Pendiente (fuera de esta fase)
- Más bloques candidatos: HUD (contadores/chips), selector de niveles, upgrades.
- Investigar la fragilidad por orden de ejecución de Level03/04 y del test de Hub a 1080.
