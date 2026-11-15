# Fase D — mediciones en dispositivo (2026-11-15)

Dispositivo: Motorola Edge 60 Fusion, Android 16, arm64-v8a, 1220x2712 a 450 dpi (con recorte de pantalla). Build: release (no development), IL2CPP, ARM64, versión 0.2.83. Instalado con `adb install -r` sobre la app existente (conserva el progreso); no se jugó ninguna partida para no tocar el guardado real.

## Peso del APK
| APK | Tamaño |
|---|---|
| Instalado antes (13/11) | 510 MB |
| Build del 14/11 (`Firebase.apk`) | 370 MB |
| Build de hoy (`opt-test`, tras Fases B/E y limpieza) | 345 MB |

Composición del de 370 MB: `assets/bin/Data` 323 MB comprimidos (786 MB sin comprimir) = texturas ETC2 RGBA8 (8 bpp); `libunity` 16,6 MB; `libil2cpp` 15,4 MB; dex 9,8 MB; resto <5 MB. El código es ~10 % del APK; el peso es casi todo arte.

## Hub en dispositivo (primer APK)
- Arranque: actividad visible en 0,4 s; hub listo en pocos segundos.
- Memoria: PSS total ~1,0 GB (Graphics 614 MB, Private Other 248 MB, Code 77 MB, Native 42 MB).
- CPU con el hub quieto: 75–81 % de un núcleo.
- Los botones del hub escalados no chocan con el recorte de pantalla.
- No se obtuvo fps/janks: `gfxinfo` y `SurfaceFlinger --latency` no devuelven datos para este renderer en Android 16.

## Ajuste hecho tras ver el dispositivo
El botón CUENTA escalado dejaba asomar la pastilla horneada en la ilustración. Se mantuvo el desplazamiento superior original (solo crecen tamaño e inset horizontal) y quedó cubierta salvo un borde mínimo (`opt-test2`, verificado en el teléfono).

## Palancas pendientes (requieren decisión)
- **ASTC 6x6 en fondos grandes** (≈3,6 bpp vs 8 bpp de ETC2 RGBA8): reduciría las texturas más de la mitad (peso y 614 MB de memoria gráfica) con pérdida visual leve. ASTC 8x8 (2 bpp) aún más agresivo. Cambia arte; hay que revisar visualmente.
- ETC2 RGB (sin alfa) en fondos opacos (4 bpp).
- 46 texturas de Resources (87 MB de PNG) mantienen mipmaps; probablemente innecesarios en IMGUI.
- CPU alto en el hub quieto (IMGUI con ambientación animada): candidato a medir con el profiler en un build de desarrollo.

## Prueba ASTC 6x6 (2026-11-15, aprobada por el usuario)
Cambio: el override de Android de **136 texturas de `Resources` con lado ≥ 1024 px** pasó de ETC2 RGBA8 (formato 47) a ASTC 6x6 (formato 50), solo en sus `.meta`; el arte fuente no se modificó. Las menores a 1024 px, los escudos RGBA32 y las demás quedaron igual. Para volver atrás: revertir ese commit (ETC2 es 47 en el mismo campo).

| Métrica (mismo teléfono, hub) | ETC2 (`opt-test`) | ASTC 6x6 (`opt-astc`) |
|---|---|---|
| Tamaño del APK | 345 MB | **219 MB (−37 %)** |
| Memoria gráfica | 614 MB | **428 MB (−30 %)** |
| PSS total | 1,00 GB | **0,80 GB (−20 %)** |
| CPU hub quieto | 75–81 % de un núcleo | 59–82 % (igual dentro del ruido) |

Calidad: comparado a 1:1 el cartel "JUGAR" del hub, no se distingue diferencia visible (ASTC incluso se ve algo más suave que los bloques de ETC2). **No se revisaron aún las demás pantallas en el dispositivo** (partida, Mercado, victoria, selector): conviene mirar una partida con personajes y comida en el teléfono antes de darlo por definitivo.
Tests: `StreetArtTests` aceptaba solo ETC2_RGBA8; ahora acepta ETC2_RGBA8 o ASTC_6x6 (el criterio sigue siendo "override comprimido con alfa"). 353 tests de arte/tema/cocina en verde. El Editor sigue usando la configuración de Standalone, así que no ve diferencia.
