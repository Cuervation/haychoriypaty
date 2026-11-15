# Línea base de optimización — Fase A (parcial, estática)

Fecha: 2026-11-14. Rama `chore/fase3-codigo-prototipo`. Solo lectura; no se modificó código ni assets.

## Estado de la medición
- **Estática: hecha** (abajo).
- **Dinámica: CERRADA (parcial en Android y Tower Defense).** Muestra puntual en Play Mode del Editor (escena `Main`, cerca del arranque, profiler encendido; ver abajo). Pendiente: hub, partida y Tower Defense en juego, tiempo de `OnGUI` por función y dispositivo Android.

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


## Medición posterior (2026-11-15, Editor, Play Mode, escena `Main`, tras Fases B y E)
Misma metodología que la muestra anterior (profiler en Editor; sobrestima Android).

| Métrica | Antes | Después |
|---|---|---|
| Asignación GC por frame en reposo | ~26 KB / 453 allocs | ~17,5 KB / ~331 allocs (−33 % bytes, −27 % allocs) |
| Asignación GC por frame en partida (Nivel 1) | referencia previa | −24 % bytes, −22 % allocs (medido el 2026-11-14 tras `9839c75`) |
| CPU por frame (estable) | ~8 ms | ~6,5 ms (hilo principal ~1,7 ms) |
| GPU por frame | ~5–7 ms | ~3,3 ms |
| Memoria de texturas | ~1,56–1,63 GB | ~1,63 GB (sin cambios; Fase C no modificó texturas) |
| Consola | n/d | 0 errores al entrar en Play |

Lecturas:
- La reducción de GC en reposo proviene de las cachés de estilos de botón, fuentes ajustadas, acciones del catálogo y estaciones de cocina (commits `842259d`, `9839c75`). La partición de `StreetView.cs` (Fase E) no cambia comportamiento y no debería afectar estos valores.
- Una sola muestra por métrica: sirve como orden de magnitud, no como benchmark estadístico. El CPU/GPU del Editor varía con el foco de ventana.
- **Sin medir:** hub/selector con interacción, Tower Defense (bloqueado), Android real, peso de APK. Siguen pendientes y requieren un APK/dispositivo con autorización.
- Pendiente de atribuir: ~57 KB/frame en partida (renderer de cocina `LateUpdate`, proyección de trabajadores, simulación, overhead del Editor).


## Atribución del GC en partida (2026-11-15, Nivel 1, ~28 clientes, Editor, 120 frames)
Método: prueba temporal de PlayMode con `ProfilerRecorder` ("GC Allocated In Frame") apagando subsistemas; luego retirada (no se versiona).

| Configuración | Asignación por frame |
|---|---|
| Partida completa | ~49 KB / ~1.225 allocs |
| Sin `StreetKitchenRenderer` | ~46 KB / ~1.155 allocs (el renderer aporta ~2,7 KB) |
| `StreetView` deshabilitado (sin OnGUI/Update/LateUpdate) | ~16 KB / ~360 allocs |
| Piso (juego y vista deshabilitados) | ~15,6 KB / ~355 allocs (overhead del Editor/cámara) |

Lectura: `StreetView` explica ~33 KB y ~870 allocs por frame; la cocina, ~2,7 KB; el piso del Editor, ~16 KB. Un intento de atribuir por sección con `GC.GetTotalMemory` dio ~0 B por sección (el código propio ya casi no asigna: contadores, cuotas, mejoras, cocina y trabajadores medidos individualmente cerca de 0). Eso indica que las asignaciones restantes ocurren dentro de las llamadas IMGUI de Unity (`GUI.Label`/`DrawTexture`/`GUIClip`) y se multiplican porque `OnGUI` corre en dos eventos por frame (Layout y Repaint). Reducirlas más exigiría sacar IMGUI del camino de partida (renderizado por sprites) o evitar el pase Layout, y el juego depende del pase Layout en Android (`NativeAction`), por lo que no se tocó sin un dispositivo para verificarlo.
Conclusión: el GC por frame de código propio ya está en el piso razonable; el resto es costo inherente de IMGUI.
