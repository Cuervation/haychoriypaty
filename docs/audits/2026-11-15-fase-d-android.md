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
