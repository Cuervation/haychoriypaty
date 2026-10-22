## 2026-10-22 — APK 0.2.60/code62 y correcciones de Racing/Chulengo

Se generó en una copia aislada con Unity 6000.6.3f1 (Main-only Development IL2CPP/ARM64), sin tocar/cerrar el Editor en vivo: build exitosa, 601,17 s, 0 errores/2 advertencias. Incluye el Fernetero inicial de Racing a $200 (curva $500/$1.000/$1.800/$2.800), Chulengo más cercano a la referencia, compactación/liberación de celdas de sándwich y handoff en la columna exacta del cliente. Validación focalizada: 35/35 EditMode y 19/19 PlayMode; no se ejecutó la suite completa, no se hizo inspección actual de Game View ni prueba en teléfono. La meta visual ≥90% queda pendiente. APK 345.499.705 bytes, SHA-256 `5f866d282f501d3a4cb14a1dbfd3e9a3a9e351145d7556a12abc765d7ce756fb`, metadatos/zip/alineación/firma/flags Firebase verificados. Debug Development, sin instalar; artefacto local fuera de Git en `/private/tmp/HayChoriYPaty-0.2.60-code62.apk`. Fuente y contratos preparados para publicarse en `codex/street-automation`.

## 2026-10-21 — Última entrega vigente (supersede el estado de entrega del 18/10)

APK Android Development **0.2.58/code60**, Main-only IL2CPP/ARM64, Unity6000.6.3f1: 0 errores/1 advertencia, 344.979.035bytes, SHA-256 `e695784e59eb3de0945616e93249fa79e866efd3fbeacb51dc827a5aebeeb9e9`. Incluye Chulengo y el Premium gratis inicial únicamente en Racing. Se instaló con `adb install -r` en Motorola Edge60Fusion (PackageManager confirma code60); datos preservados, no se abrió. Firma debug, no PlayStore. La build aislada partió de base70ce8b0 y aplicó los cinco archivos de runtime actuales listados en [android-prototype.md](features/android-prototype.md); el overlay de código y los contratos están ahora en Git. La APK permanece fuera del repo en `/private/tmp/HayChoriYPaty-0.2.58-code60.apk` porque `Builds/` está ignorado y la APK pesa329MiB. Evidencia completa en [android-prototype.md](features/android-prototype.md).

## 2026-10-18 — Último estado de entrega (supersede las versiones anteriores abajo)

Android Development APK actual: **0.2.48/code50**, Main-only IL2CPP/ARM64, built from an isolated copy with Unity 6000.6.3f1. Build succeeded (0 errors/1 warning); APK is **348,924,731 bytes**, SHA-256 e06e52d1fb3b433d33ad08834633bd5b02ef82e7e955de8c2f4fff91b3000e29. Direct employee movement and current worker/kitchen updates are included alongside ETC2 Android texture compression. Debug-signed, not Play Store release-signed; not installed. Prior focused movement tests: 10/10; full fixture: 52/54 with two known safe-wait-point failures. No Game View, PlayMode or phone QA. Unity MCP endpoint was unavailable; the live Editor was left open and the batch build isolated. Details in specs/features/android-prototype.md. Source pushed on codex/street-automation.

## 2026-10-16 — Backend Firebase configurado, validación del cliente pendiente

Firebase real configurado: Google habilitado, Android com.haychoriypaty.game con SHA debug, Firestore Standard en São Paulo y reglas seguras desplegadas; Billing=false. Roundtrip administrativo IAM real aprobado, no progreso de jugadores. SDK importado y compila; 46/47 EditMode (falla campaña nivel6) y4/4 PlayMode, gate manual sin sesión verificado. APK0.2.44/code46 IL2CPP/ARM64 generada (0 errores/2 warnings), sin instalación. Google real del cliente y reapertura/dispositivo pendientes. Detalle: [firebase-player.md](features/firebase-player.md#informe-verificable-de-entrega--2026-10-16).

# Continuidad — estado vigente de Hay Chori y Paty

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
