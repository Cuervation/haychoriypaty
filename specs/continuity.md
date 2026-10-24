## 2026-10-24 — Current Vacío 4-piece comparison

Latest user request supersedes prior six-piece/+15%-width visual preview. Shared premium grill now has four real Vacío units in a broad 2×2 right-half layout; Bondiola remains six on the left, solo Vacío remains7. Depth stays 1.10× Bondiola; only the shared-grill Vacío X scale is widened to target aspect3.4, with a 1-unit seam. Whole-bondiola art/geometry EditMode36/36, production1/1, bounds1/1, separation1/1; Level6 GameView PlayMode1/1 and ambient-slot integration1/1 passed in an isolated Unity6000.6.3f1 copy. Full-screen/detail evidence: `Logs/Acceptance/VacioFour-20261024/`. APK 0.2.66/code68 built from runtime commit b1c1426 in an isolated Unity6000.6.3f1 copy; artifact /private/tmp/HayChoriYPaty-0.2.66-code68-vacio-four.apk, 384458497bytes, SHA256 9ff3f511f948717b51983334ac51a0085bb3e58b6f8aee6d9323ac636afa81c9. APK metadata/ZIP/alignment/signature/Main bindings verified. No phone install; keep full-campaign Level6 loss as the existing balance limitation and do not modify economy.

## 2026-10-24 — Vacío enlarged 10%

The user reduced the enlargement request to 10%. Shared-grill Vacío now renders at 1.10× Bondiola's width and depth. Six cuts remain in the 3×2 right-half layout and inside the grill; the tighter fit allows up to4.1 logical units of horizontal and1.7 of row-depth overlap. Focused EditMode `WholeBondiolaArtTests` passed36/36; the two affected presentation checks passed1/1 each; Level6 PlayMode visual check passed1/1 in an isolated Unity 6000.6.3f1 copy. Full GameView and grill-detail captures are `Logs/Acceptance/VacioFillHalf-20261024/vacio-plus10-level-6-1080x1920.png` and `vacio-plus10-detail-level-6.png`. `git diff --check` passes. No APK, install, commit, or push.

## 2026-10-24 — Vacío half-grill completed

Selected second Vacío reference implemented as whole transparent Raw/Cooking/Cooked sprites; shared premium grill holds six Vacíos in a tight 3×2 layout on the right, matching Bondiola’s rendered footprint and retaining six Bondiolas left. Sprite/geometry 36/36, production 11/11, relevant presentation checks 2/2, and Level 6 PlayMode 1/1 passed in isolated Unity 6000.6.3f1. Presentation suite has three unrelated existing expectation/resource mismatches (24 vs48 initial table count, Fernet size, missing table plank art). Full Game View and grill-detail captures: `Logs/Acceptance/VacioFillHalf-20261024/`. No APK/build/install/push.

## 2026-10-23 — Entrega de insignias Android0.2.65/code67

Nueva APK con21insignias de ventas500/2500/5000 para7productos; flags obtenidos y ventas viajan juntos en snapshotFirebase porUID. Fuente197c3a3 publicada;32EditMode+1PlayMode previamenteaprobados reutilizados sin cambios y67fuentes runtime/escena comparadas. Build Main-only Development IL2CPP/ARM64 en copiaaislada:0errores/1warning,447,19s. APK `/private/tmp/HayChoriYPaty-0.2.65-code67-insignias.apk`,354.357.269bytes,SHA256 `98ca84f89eeb6e12635c6ad034234e907309642f7b3f9d1172c232e9b11170b0`. Metadatos/ZIP/alineación/firmaV2/debug/bindingsMain verificados e independientemente revisados. No instalada; el Motorola conserva0.2.64/code66. RoundtripFirebase real de insignias pendiente. Sin cambiosglobales/trust ni trabajoajeno incluido. Recibo en [Android delivery](features/android-prototype.md).

## 2026-10-23 — APK login/UI regression repair

**Do not deliver 0.2.61/code63 or 0.2.62/code64 again:** isolated compiler caches retained another snapshot's absolute path and exported Main with invalid script bindings. Regenerating compiler caches only in the isolated copy restored all five script classes. New FirebaseAndroidBuildGuard blocks invalid MonoScript types and missing/inactive Main components; three unique focused EditMode checks pass. No auth bypass, save rewrite, scene/GUID change or gameplay modification.

Repair APK **0.2.63/code65**: `/private/tmp/HayChoriYPaty-0.2.63-code65-login-repair.apk`, 345,503,205 bytes, SHA256 `fc7b3109deffc36f5c6a49261d37e51cc57169462efe3ae7bfa5ae4bc6490958`; build succeeded0 errors/1 warning506.16s. Direct artifact parsing independently confirms StreetGame/View/KitchenRenderer active and valid. Installed on Motorola Edge60Fusion ZY22MBNWRB via `adb install --no-streaming -r` after streaming USB failed. PackageManager confirms0.2.63/code65 and unchanged firstInstallTime2026-10-03 18:49:24. Physical capture shows authenticated session recovered and Racing Mercado with standard styled buttons. No logout/new Google picker was exercised; cloud online sync is not certified (DNS warnings observed). No data clearing. Device evidence: `/private/tmp/hcyp-login-repair-device-awake.png` (full1220×2712 screen). See [Android repair](features/android-prototype.md).

## 2026-10-23 — APK Android 0.2.62/code64

Se generó en copia aislada con Unity 6000.6.3f1, Main-only Development IL2CPP/ARM64: **Succeeded, 0 errores/6 warnings, 89,67 s**. Incluye la extensión final del pergamino Chulengo que tapa el borde verde del CTA legado. APK `/private/tmp/HayChoriYPaty-0.2.62-code64.apk`, paquete `com.haychoriypaty.game`, versión0.2.62/code64, 345.505.605 bytes, SHA-256 `74b9700a5693cf5e228e534adbdf5443842b614eb45f402b1a3b0493bc9271e8`. Metadatos, integridad ZIP, alineación, firma v2 y flags Firebase Analytics desactivado/GA AD ID desactivado comprobados. Firma Android Debug, no Play Store. No se instaló ni se hizo QA físico; similitud ≥95% de Chulengo no certificada. La APK queda fuera de Git; el contrato completo está en [android-prototype.md](features/android-prototype.md).

## 2026-10-23 — Reanudación de validación en Unity

Se confirmó por Unity MCP que la única instancia activa es `HayChoriYPaty` en `/Users/celestino/HayChoriYPaty`, Unity 6000.6.3f1. El ping de `editor/state` quedó sin respuesta, aunque los jobs focalizados del Test Runner sí terminaron. Pasaron **40 casos EditMode únicos** y **4 casos PlayMode únicos** focalizados para Chulengo, geometría/handoff y compactación de sándwiches/Mozo. PlayMode incluyó capturas Unity del estado Chulengo de Racing y de mesas compactas/transporte del Mozo, en `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/`.

Se corrigió un test táctil de victoria que esperaba navegación directa obsoleta: ahora cobra el premio pendiente antes de abrir niveles. La primera captura Game View reveló una franja verde de un CTA legado debajo de `MULTIPLICAR`; se extendió el pergamino en `StreetView` para cubrirla y la captura posterior confirma que ya no asoma. La composición aún **no está certificada ≥95%** respecto de la referencia.

Se inició también una ejecución EditMode completa sin filtros, pero la consulta del job devolvió `TimeoutError` y el Editor volvió a no responder al ping; no se cuenta como aprobada ni como corrida completa. La suite PlayMode completa, QA visual/device, el gameplay real de nivel6 y los asuntos externos iOS/Firebase/dispositivo siguen pendientes. La meta de partidas reales sigue **0/200**: tests/capturas sintéticas no cuentan como intentos. No se guardó/modificó la escena, ni se generó APK, instaló, confirmó ni publicó nada. Evidencia visual: `/var/folders/bf/q6_2cmvn6ls9pc5h5g7thj8r0000gn/T/hay-chori-chulengo-pending-1080x1920.png`.

## 2026-10-23 — APK Chulengo 0.2.61/code63

APK generada desde una copia aislada con Unity 6000.6.3f1, Main-only Development IL2CPP/ARM64. BuildPipeline: Succeeded, 0 errores/1 warning de símbolos de diagnóstico, 448,59 s. 12/12 tests EditMode focalizados en Chulengo pasan; se corrigió en `StreetArtTests` el unboxing reflection de `Color` a `Color32`. Metadatos, ZIP, alineación, firma v2 y flags Firebase Analytics desactivado/GA AD ID desactivado verificados. APK 345.506.161 bytes, SHA-256 `31e2f830b814dfd7399d8ba821bd4e3be81035a17632b311af9abbc859b938ec`, artefacto local `/private/tmp/HayChoriYPaty-0.2.61-code63.apk`; debug-signed, no instalada ni subida a Git.

El runtime incluye la recoloración de banderas Chulengo por colores del nivel. No se pudo capturar/inspeccionar el Game View de Unity ni comparar contra la imagen; la similitud ≥95% queda **sin certificar** y sigue pendiente de QA visual real. Unity MCP no respondió al ping; no se tocó el Editor activo. Ver evidencia completa en [android-prototype.md](features/android-prototype.md).

## 2026-10-22 — Chulengo referencia de victoria (iteración en curso)

Se ajustó `StreetView` para acercar el marco Chulengo a la composición vertical adjunta: marco con márgenes/posición más alineados, título y premio reordenados, texto de instrucción/multiplicador/advertencia sin colisiones, y controles inferiores escalables. Las telas de las dos banderas superiores ahora toman los colores del `ClubVisualTheme` del nivel; el arte fuente permanece sin modificación y los banderines inferiores continúan siendo temáticos.

Se agregó cobertura focalizada en `StreetArtTests` y se cambió `isReadable` del popup para permitir el recolor en runtime. El primer filtro de pruebas por nombres cortos devolvió 0 tests; el segundo, con nombres completos, falló al inicializarse por desconexión del Editor antes de iniciar el runner. La captura de cámara disponible con `InitTestScenee16e6289-69f9-4473-8d22-310334403ecf` fuera de Play solo mostró cielo/suelo, no el juego ni Chulengo; el PNG temporal se eliminó y no sirve para QA. El objetivo visual ≥95% queda abierto y no se afirma como logrado. No se cambió escena ni Play Mode; preservar los cambios locales ajenos existentes.

## 2026-10-22 — Auditoría iOS: preflight detenido por toolchain/firma ausentes

En el checkout activo se derivó el Bundle ID iOS `com.haychoriypaty.game` del Android existente y se fijó el primer build number iOS a1; se conservaron bundle version0.2.60, orientación vertical, mínimo iOS15.0, el AndroidBundleVersionCode62 y el ajuste local preexistente `runInBackground:1`. Se actualizó [firebase-player.md](features/firebase-player.md#auditoría-ios--2026-10-22) con el estado y aceptación pendiente.

No se pudo generar Xcode project/archive/IPA: macOS26.7.1 x86_64, Unity6000.6.3f1 y Unity Hub presentes; **iOS Build Support se instaló y quedó verificado (`ios ok` en `unity editors verify`, 2.7G en disco)**. Xcode completo, iPhoneOS SDK/simctl y CocoaPods no están disponibles, `xcode-select` apunta a Command Line Tools. Sin identidades válidas, profiles, Team ID ni dispositivo enumerado; no se intentó firma/instalación. Tras instalar, una consulta Unity MCP expiró; no cerré/reinicié ni refresqué el Editor. Firebase `GoogleService-Info.plist` no existe y el proveedor de credenciales Google actual es Android-only, por lo que iOS Auth todavía no es funcional. No había Firebase CLI instalada/sesionada para verificar o registrar app iOS. No se tocaron gameplay, economía ni Android; no se modificó/reinició el Editor, no se limpiaron datos y no se hizo commit/push.

**Reanudación:** instalar/inicializar Xcode compatible con este Mac Intel, habilitar el módulo iOS de Unity y autorizar la identidad/cuenta Apple desde Xcode (no compartir credenciales en chat). Luego completar/validar configuración iOS de Firebase+Google Sign-In, automatizar archive/export, firmar y verificar la IPA. El criterio sigue sin completarse hasta validar IPA firmada y login real en dispositivo.

## 2026-10-22 — APK 0.2.60/code62 y correcciones de Racing/Chulengo

Se generó en una copia aislada con Unity 6000.6.3f1 (Main-only Development IL2CPP/ARM64), sin tocar/cerrar el Editor en vivo: build exitosa, 601,17 s, 0 errores/2 advertencias. Incluye el Fernetero inicial de Racing a $200 (curva $500/$1.000/$1.800/$2.800), Chulengo más cercano a la referencia, compactación/liberación de celdas de sándwich y handoff en la columna exacta del cliente. Validación focalizada: 35/35 EditMode y 19/19 PlayMode; no se ejecutó la suite completa, no se hizo inspección actual de Game View ni prueba en teléfono. La meta visual ≥90% queda pendiente. APK 345.499.705 bytes, SHA-256 `5f866d282f501d3a4cb14a1dbfd3e9a3a9e351145d7556a12abc765d7ce756fb`, metadatos/zip/alineación/firma/flags Firebase verificados. Debug Development, sin instalar; artefacto local fuera de Git en `/private/tmp/HayChoriYPaty-0.2.60-code62.apk`. Fuente y contratos preparados para publicarse en `codex/street-automation`.

## 2026-10-21 — Última entrega vigente (supersede el estado de entrega del 18/10)

APK Android Development **0.2.58/code60**, Main-only IL2CPP/ARM64, Unity6000.6.3f1: 0 errores/1 advertencia, 344.979.035bytes, SHA-256 `e695784e59eb3de0945616e93249fa79e866efd3fbeacb51dc827a5aebeeb9e9`. Incluye Chulengo y el Premium gratis inicial únicamente en Racing. Se instaló con `adb install -r` en Motorola Edge60Fusion (PackageManager confirma code60); datos preservados, no se abrió. Firma debug, no PlayStore. La build aislada partió de base70ce8b0 y aplicó los cinco archivos de runtime actuales listados en [android-prototype.md](features/android-prototype.md); el overlay de código y los contratos están ahora en Git. La APK permanece fuera del repo en `/private/tmp/HayChoriYPaty-0.2.58-code60.apk` porque `Builds/` está ignorado y la APK pesa329MiB. Evidencia completa en [android-prototype.md](features/android-prototype.md).

## 2026-10-18 — Último estado de entrega (supersede las versiones anteriores abajo)

Android Development APK actual: **0.2.48/code50**, Main-only IL2CPP/ARM64, built from an isolated copy with Unity 6000.6.3f1. Build succeeded (0 errors/1 warning); APK is **348,924,731 bytes**, SHA-256 e06e52d1fb3b433d33ad08834633bd5b02ef82e7e955de8c2f4fff91b3000e29. Direct employee movement and current worker/kitchen updates are included alongside ETC2 Android texture compression. Debug-signed, not Play Store release-signed; not installed. Prior focused movement tests: 10/10; full fixture: 52/54 with two known safe-wait-point failures. No Game View, PlayMode or phone QA. Unity MCP endpoint was unavailable; the live Editor was left open and the batch build isolated. Details in specs/features/android-prototype.md. Source pushed on codex/street-automation.

## 2026-10-16 — Backend Firebase configurado, validación del cliente pendiente

Firebase real configurado: Google habilitado, Android com.haychoriypaty.game con SHA debug, Firestore Standard en São Paulo y reglas seguras desplegadas; Billing=false. Roundtrip administrativo IAM real aprobado, no progreso de jugadores. SDK importado y compila; 46/47 EditMode (falla campaña nivel6) y4/4 PlayMode, gate manual sin sesión verificado. APK0.2.44/code46 IL2CPP/ARM64 generada (0 errores/2 warnings), sin instalación. Google real del cliente y reapertura/dispositivo pendientes. Detalle: [firebase-player.md](features/firebase-player.md#informe-verificable-de-entrega--2026-10-16).

# Continuidad — estado vigente de Hay Chori y Paty

**Nota 2026-10-24:** La variante actual solicitada para inspección visual de Vacío es +15% de ancho sobre la configuración +10% previa, con profundidad sin cambio y desborde exterior permitido. Geometría 2/2 y PlayMode Game View 1/1 pasan en copia aislada; evidencia 1080×1920 y detalle en `Logs/Acceptance/VacioWidth15-20261024/`. En esta disposición el corte más externo aún no cruza el límite de parrilla, aunque el layout ya no comprime el ancho para obligarlo a caber. Checkout con cambios locales ajenos preexistentes: preservar todo lo no relacionado.

## 1. Último estado confirmado — 2026-10-16

Juego Unity Android de 11 niveles. Escenario original restaurado con más circulación desde Racing, sin adelantar Fernet ni agregar mesa lateral de chimi; se mantienen cocineros activos y Mozos. Personajes/flechas centrados en óvalos y ×1/+10% separados del precio. La APK0.2.42/code44, con cocción activa de 2 s, quedó instalada en el Motorola Edge60Fusion sin borrar datos. No se abrió ni se hizo QA de gameplay en el teléfono. El ajuste posterior de proximidad (Parrillero 8 puntos y Cocacolero 2 puntos más cerca) está solo en el checkout; aún no se compiló ni se instaló. La solicitud siguiente separó puestos de Parrilleros: 55/55 EditMode en copia aislada; tampoco está incluida en la APK.

**APK0.2.42/code44 instalada** con `adb install -r` sobre la0.2.41/code43; PackageManager confirma versión44. No se desinstaló ni se borraron datos. Cada victoria real cobra50%ventas+2×sobrante, por intento; recupera$3570del último recibo del teléfono (test compra$825faltantes de3y deja$2745). 32regresiones de economía aprobadas, más19/19EditMode de cocineros/Mozos. Campaña fresca ganó1–9, perdió10por13Chori en mejor política; no se declara recorrido1–11 completo. Evidencia: [status.md](status.md) y [android-prototype.md](features/android-prototype.md).

## 2. Rama, versión y entorno de referencia

- **Checkout real:** `/Users/celestino/HayChoriYPaty`; rama `codex/street-automation`, la rama de release codex/street-automation está actualizada en origin; commit anterior de instalación f077324; remoto `https://github.com/Cuervation/haychoriypaty.git`. Push verificado.
- La carpeta `/Users/celestino/Documents/ChatGPT/Hay Chori y Paty` es una copia inicial sin commits, no el juego activo. No implementar sobre ella ni sincronizar sus assets/código.
- Unity **6000.6.3f1**, Main, Android; source/Development APK **0.2.48/code50**. Firma debug, no release Play Store. El Motorola conserva la instalación anterior **0.2.44/code46**; no se instaló esta build. Artefactos y hashes en [android-prototype.md](features/android-prototype.md).
- App instalada **26.1002.52244/build13536**, CLI **0.162.0-alpha.2**. Roles conservados: architecture Sol/medium; implementation Luna/medium; validation Luna/low. `.codex/config.toml`: dos subagentes más el principal. Config global leída, no modificada: `gpt-6.1-sol`/`xhigh`; no certifica un override de modelo/esfuerzo de este chat.
- **Bloqueo de configuración:** `config/read` del CLI marca inactiva la capa `.codex` del checkout real por falta de trust. `skills/list` sí descubre los cuatro skills. Los tres roles están expuestos en este chat existente; esto no prueba que una nueva sesión CLI cargue la configuración del checkout real. Seleccionar ese proyecto y autorizar su trust es un paso pendiente del usuario: la tarea prohíbe cambiar configuración global.
- MCP identificó la instancia `HayChoriYPaty@1baadce415f1081d`, pero durante la generación de0.2.41 su ping no respondió. El APK se compiló en una copia aislada del checkout; la conectividad/estado live del Editor y una revisión visual quedan pendientes. La instancia histórica `hcyp-visual-test` no es evidencia del checkout real.

## 3. Funcionalidades implementadas

- Once clubes con catálogo progresivo de siete productos, cuotas independientes y FIFO; los clientes no expiran por espera y los ingresos solo ocurren al entregar físicamente.
- Contratación sin tope de gameplay, cinco roles disponibles según catálogo/modo, velocidad limitada, reinicio del equipo/velocidad en cada partida.
- Cocina modular y stock físico; desde nivel 6 cocineros activos y Mozo exclusivo para sándwiches. Bebidas entregadas por sus roles.
- Mercado/Heladera en una sola pantalla IMGUI, saldo e inventario persistentes, recetas/reservas y compras cash-only; restauración de stock al perder y liquidación por intento de cada victoria real. Recuperación exacta del último recibo antiguo válido, reparaciones legacy y premio acotado al primer desbloqueo.
- Portada/selector, HUD por productos, mejoras, animación y trifulcas integradas. Conservar aceptación abierta de cada spec: implementación no equivale a QA universal.

## 4. Arquitectura actual

`StreetGame` coordina lifecycle/save v4; `StreetSimulation` y `StreetKitchenProduction` son el dominio. `StreetView` es IMGUI/input; renderers/proyección de workers y prefabs SpriteRenderer generan capas transparentes en vivo. `StreetSceneLayout` fija escena raíz; `StreetKitchenLayout` fija geometría/rutas. `StreetWorkstationLayout` y el prototipo anterior son compatibilidad histórica. Detalle: [architecture.md](architecture.md).

## 5. Mecánicas vigentes

- Un Parrillero gratis, resto de roles en cero y velocidad ×1,00 al iniciar/cargar/seleccionar nueva partida; saldo inicial de intento según tabla, no compras previas. Progreso de niveles y Mercado sí persisten.
- Niveles 1–5: producción y entrega base. Desde Racing (índice5): Parrillero/Premium → parrilla → mesa → Mozo → cliente; cocción activa configurable **2 / WorkRate s** (2 s a ×1; EditMode `StreetActiveCooksTests` 19/19 aprobado, sin PlayMode ni QA visual/dispositivo). Coca/Cerveza → Cocacolero; Fernet → Fernetero. Cocinar/depositar no genera dinero.
- Desde Racing: seis cards en dos filas de tres, sin banderines inferiores, área de piso/circulación ampliada con muebles originales; Fernet conserva su fila. Geometría y restauración: [active-cooks-mozos.md](features/active-cooks-mozos.md).
- Contratación ilimitada por rol habilitado y saldo; curva sucesiva. Nueve mejoras +10 puntos porcentuales, máximo **×1,90**. Valores exactos, precios de venta, cuotas y tiempos: [street-automation.md](features/street-automation.md#current-per-product-level-balance--2026-10-08). Los Redondos son **315 s**, corroborados en fuente y Main, no el antiguo 300 s.
- Sin préstamos/crédito. Liquidación Mercado: cada victoria real paga50%ventas completas+2×caja sobrante del intento, sin bloqueo por nivel; derrota mantiene reconocimiento incremental previo; premio de reposición solo para el déficit del primer desbloqueo. No es una ganancia persistente del 5%. Detalle: [market-fridge.md](features/market-fridge.md).

## 6. Pendientes conocidos

- Balance: auditoría posterior a restauración ganó1–9desde cero; varias políticas fallaron10, mejor final13Chori pendientes a300s, con saldo para reposición.11ganó en dos auditorías separadas desde el progreso auténtico ya desbloqueado, no desde una campaña fresca completa. No demuestra imposibilidad de10ni balance humano. Hipótesis histórica320/335/335s **no aplicada ni autorizada**.
- ARMÁ TU PLANTEL, ganancia persistente del 5% y desbloqueos permanentes de plantilla/mejoras no aparecen implementados; no confundirlos con la progresión de niveles ya persistente.
- QA Android visual/táctil/cutout/FPS/térmica, aceptación visual abierta en [worker-animation.md](features/worker-animation.md), audio/pulido y publicación release según alcance pendiente de cada spec.
- Nomenclatura Fernet: runtime1L frente a vaso visual800cc. Se conserva sin reinterpretar ni modificar; falta decisión si se desea armonizar producto/presentación.
- Trust del checkout real sigue pendiente; no alterar configuración global para resolverlo silenciosamente.

## 7. Validaciones realizadas (históricas y delimitadas)

- Cocineros/Mozos: 71/71 EditMode + 7/7 PlayMode; rechecks parciales 35/35 y 20/20, no totales únicos adicionales. Restauración posterior: 35/35 EditMode y capturas River1080×1920/Racing1440×2960. Óvalos: 13/13 EditMode y capturas compactas/amplias.
- Pickup del Mozo por slot: `StreetActiveCooksTests` **19/19 EditMode passed** el2026-10-16; los cuatro productos apuntan a su slot reservado. No valida apariencia/movimiento en vivo.
- Proximidad Parrillero/Cocacolero: suite focalizada **42/42 EditMode passed** el2026-10-16 en copia aislada; no valida la apariencia en vivo. La APK0.2.42 instalada conserva posiciones anteriores.
- Parrilleros simultáneos: **55/55 EditMode passed** el2026-10-16; dos trabajadores usan estaciones diferentes y se probaron rutas de hasta9 anchors (evidencia histórica, anterior al límite de cuatro). El cambio local del18/10 asigna cuatro puestos por grill y espera/rota el resto sin escalar. Runtime y EditMode compilaron; el job del assembly se desconectó antes de informar resultados; las capturas de cinco Normal/Premium siguen pendientes. No APK, instalación ni push.
- Economía2026-10-16:32/32regresiones únicas aprobadas (15dominio+9integración+8payout), más2/2auditorías separadas de11. Benchmarks de campaña fresca pierden10; conservar ese límite explícito. Resultados/ledgers en `Logs/Acceptance/EconomyAudit-20261015/`; no sumar reruns como casos nuevos. La evidencia histórica previa a cocina activa no certifica balance actual.
- Build 0.2.41: éxito, 0errores/1advertencia; ZIP, metadatos, ARM64 y firma v2 verificados. Instalación `-r` y versionCode43 confirmados, sin uninstall/data clear.
- Auditoría de contexto: TOML/frontmatter/modelos, descubrimiento de skills, enlaces/rutas y diff; detalle en [status.md](status.md). Sin Unity tests, PlayMode ni nueva APK en la auditoría.

## 8. Validaciones todavía no realizadas

Reintento PlayMode posterior a restauración falló internamente antes de casos; no declararlo aprobado. No hubo revisión física del gameplay ni PlayMode nuevo para el pickup o la cocción2s. La build0.2.42 sí se instaló, pero no se abrió para probar gameplay. No prometer certificación universal de resoluciones/rendimiento.

## 9. Referencias y antecedentes

Usar [README.md](README.md) para elegir un contrato; [status.md](status.md) y [decisions.md](decisions.md) para evidencia puntual por fecha/tema. El checkpoint anterior íntegro está en [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md); sus versiones/reglas/sin-probar no son estado actual. Builds, capturas y evidencia privada quedan ignorados en `Builds/`/`Logs/`, no en Git.

## 10. Próximos pasos autorizados

La solicitud vigente de APK0.2.42/code44 y su instalación quedó completada; no se hizo commit/push. Falta revisión visual/PlayMode del pickup y duración2s; también resolver/aceptar el balance de10y repetir una campaña fresca ganadora1–11. No cambiar tiempos/precios ni campaña pendiente para forzar11/11.


## Chulengo — 2026-10-21 follow-up

Implemented optional post-victory CHULENGO with MULTIPLICAR and COBRAR, original sprites and deterministic unscaled needle. Exact sectors ×0/×1.5/×1/×2/×1/×1.5/×0; zero loses the captured whole turn reward (including its once-calculated stock grant), never prior bank/unlock. New victories escrow until one durable claim; legacy receipts retain old behavior. Final immutable match/statistics are emitted once after claim through existing owner-specific persistence. See [market-fridge.md](features/market-fridge.md#chulengo--authorized-2026-10-21) for updated contract/acceptance.37 unique focused EditMode cases passed; actual ×1 Level1 win, GameView portrait review and pointer ×0/reload verified. Original save/editor restored. Android/Firebase end-to-end/full automated PlayMode NOT_RUN; no APK/install/commit/push. Existing unrelated local settings/assets preserved.


## Premium inicial gratuito en Racing — 2026-10-21

Se implementó localmente, solo en nivel6, un Parrillero Premium gratis en cada partida/reintento junto al Normal; no consume el tier de primer hire pagado. No afecta niveles1–5 ni7–11. Cinco rondas reales en Main a ×1, seed1337, acciones por StreetView: todas perdidas. Mejor resultado197Chori, faltante13, frente al máximo anterior191/faltante19; déficit promedio24,4 frente a51,04 previo. No es victoria ni prueba de imposibilidad; compras variaron adaptativamente y se usó el perfilQA legado, no campaña nueva. `OnlyRacingStartsWithFreePremiumAndItsNextHireUsesBaseCostAcrossRetries` y guardia `RacingWithoutMozoCanCookButCannotSellOrWin`:2/2. La suite `StreetActiveCooksTests` ejecutó82,78 pasaron/4 fallaron en expectativas separadas. Perfil PlayerPrefs restaurado exactamente3439 caracteres; Editor parado, escena init original limpia, scale0, testInitfalse. Sin APK/instalación/commit/push. Informe y ledgers locales: `/Users/celestino/.codex/visualizations/2026/10/21/01a15bdf-e29b-7253-8456-9b392d7045a3/racing-default-premium/INFORME.md`.


## Distancia y orientación de entrega del Mozo — 2026-10-22

Ajuste local en el checkout activo: se redujo a la mitad el hueco vertical anterior entre la fila frontal y la línea de servicio, sin mover la columna del cliente. Con las coordenadas actuales, pasó de 76 a 38 unidades lógicas (Y=324→362, manteniendo Y=400 como origen/línea anterior). Al llegar, el Mozo queda quieto, gira para mirar hacia el cliente y conserva esa orientación durante el tiempo de entrega ya existente. El popup de monedas ahora aparece donde está el Mozo al concretar la venta. Contrato y tests EditMode relacionados se actualizaron; no hubo cambios de economía ni de compra.

**Validación de este ajuste:** `git diff --check` pasó. Unity MCP no respondió al ping para `editor_state` ni `read_console`. Se lanzó una ejecución EditMode filtrada por las fixtures `StreetSimulationTests`, `StreetActiveCooksTests` y `StreetKitchenPresentationTests`, pero la sesión se desconectó mientras se esperaba el resultado; no contarla como aprobada. El intento anterior por nombres cortos devolvió 0 tests y tampoco cuenta. Compilación/runtime en Unity y captura/revisión visual quedan pendientes. No se hizo commit ni push; se preservaron las modificaciones y archivos locales preexistentes ajenos a este cambio.
