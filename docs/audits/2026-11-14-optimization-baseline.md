# Línea base de optimización — Fase A (parcial, estática)

Fecha: 2026-11-14. Rama `chore/fase3-codigo-prototipo`. Solo lectura; no se modificó código ni assets.

## Estado de la medición
- **Estática: hecha** (abajo).
- **Dinámica: PARCIAL.** Muestra puntual en Play Mode del Editor (escena `Main`, cerca del arranque, profiler encendido; ver abajo). Pendiente: hub, partida y Tower Defense en juego, tiempo de `OnGUI` por función y dispositivo Android.

## Métricas estáticas
| Métrica | Valor |
|---|---|
| Líneas `Assets/Scripts` | ~27.800 (111 `.cs` incl. Editor) |
| Líneas de tests | ~28.400 |
| `StreetView.cs` | 7.579 líneas |
| `Assets/Art/Street` | 221 MB |
| `Assets/Resources/ModularKitchen` | 50 MB |
| `Assets/Resources/Audio` | 5,3 MB |
| `Resources.Load` (Scripts) / en `StreetView.cs` | 60 / 6 |
| `new Texture2D` (Scripts) / en `StreetView.cs` | 41 / 24 |
| `new GUIStyle` (Scripts) / en `StreetView.cs` | 16 / 10 |
| `new GUIContent` en `StreetView.cs` | 15 |
| `.ToString(` / `.ToArray()` en `StreetView.cs` | 23 / 5 |
| `FindObjectOfType`, `GameObject.Find`, LINQ en rutas calientes | 0 |

## Texturas grandes (>1,5 MB, 60 muestreadas)
- Fondos y murales de `Art/Street/Resources` de 2,8–3,5 MB cada uno.
- Import: 37 con `maxTextureSize 2048` y `textureCompression 0`; 16 con 2048 y compresión 1; 5 con 4096 y compresión 0; 2 con `isReadable` activo.
- Caveat: es el ajuste por defecto de cada `.meta`; puede haber overrides de plataforma Android que no se leyeron.
- `Assets/Screenshots/*.png` (≈3 MB c/u) está dentro de `Assets`; confirmar si se incluye en el build.

## Candidatos para fases siguientes (sin medir aún)
1. B: cachear `GUIStyle`/`GUIContent`/strings dentro de `OnGUI` (10 + 15 + 23 ocurrencias en `StreetView.cs`).
2. C: revisar compresión Android y los 4096 y `isReadable`; auditar destrucción de los `Texture2D` creados.
3. D: capturas dentro de `Assets`; posibles duplicados Art/Resources.

## Muestra dinámica parcial (Editor, Play Mode, escena `Main`, 2026-11-14)
Unity 6000.6.3f1. Valores del Editor con profiler activo: sobrestiman el rendimiento en Android.

| Métrica | Valor |
|---|---|
| Memoria de texturas | ~1,56–1,63 GB (1.558–1.580 texturas) |
| Sprites / Materiales / Meshes | 562 / 482 / 14 |
| GC usado | ~270–327 MB |
| Asignación GC por frame | 735 KB (435 allocs) en el primer frame; ~26 KB (453 allocs) en reposo |
| CPU por frame | 77 ms (primer frame); ~8 ms (estable) |
| GPU por frame | ~5–7 ms |

Lectura: la memoria de texturas es lo más pesado (refuerza candidatos C y D). El ~26 KB/frame en reposo sugiere asignaciones en `OnGUI` (candidato B), pero no se aisló por función. No medido: hub, partida, Tower Defense, costo de `OnGUI`, Android.
