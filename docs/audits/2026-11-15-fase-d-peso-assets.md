# Fase D — peso de assets y build (auditoría, sin cambios de código)

Fecha: 2026-11-15. Solo lectura; no se construyó APK (requiere pedido explícito).

## Peso en disco (Assets, fuente sin comprimir)
| Carpeta | MB | Va al build |
|---|---|---|
| Art/Street (todo bajo `Resources`) | ~227 | Sí, siempre (Resources) |
| Resources/ModularKitchen | ~52 | Sí |
| GeneratedLocalRepo (Firebase .aar 22 MB) | ~24 | Es el repo Maven local del resolver; el .aar se empaqueta |
| Resources/Audio | ~5,5 | Sí |
| Art/Placeholder, Editor, Reference, TowerDefense | ~15 | Solo si se referencian |
| Screenshots | ~6 | No (sin referencias en Scripts) |

## Hallazgos
- Texturas: en Android 143/184 ya usan ETC2 (ver Fase C); el peso real del APK es una fracción del de disco. No hay ganancia adicional sin tocar calidad visual.
- Audio: el loop de 5,5 MB WAV ya importa con Vorbis (compressionFormat 1, calidad 0,7); en el build no pesa 5,5 MB.
- Backend IL2CPP, ARM64 únicamente, `stripEngineCode` activo. `managedStrippingLevel` está en el valor por defecto.
- Escenas en build: `Main` y `SampleScene`. `SampleScene` solo tiene cámara, luz y volumen (candidata a quitar de Build Settings, ganancia mínima).
- `Screenshots` (6 MB) no entra al build; solo pesa en el repo.

## Palancas descartadas sin build de prueba
- Subir `managedStrippingLevel` a Medium/High: riesgo de romper Firebase/reflexión; exige APK y prueba en dispositivo.
- Reducir resolución de fondos 3+ MB (PNG de ~2K): cambia arte; fuera de alcance sin pedido.
- Quitar `SampleScene`: ganancia de KB; no se hizo para no tocar Build Settings sin necesidad.

## Pendiente (requiere autorización)
Construir un APK (o `BuildReport`) para obtener el desglose real por categoría y decidir con datos.
