# Continuidad — estado vigente de Hay Chori y Paty

## 1. Último estado confirmado — 2026-10-16

Juego Unity Android de 11 niveles. Escenario original restaurado con más circulación desde Racing, sin adelantar Fernet ni agregar mesa lateral de chimi; se mantienen cocineros activos y Mozos. Personajes/flechas centrados en óvalos y ×1/+10% separados del precio. La última instalada sigue siendo0.2.39/code41 en el Motorola; PackageManager había confirmado esos valores. Se generó0.2.40/code42 con el fix y no se instaló ni abrió. No equivale a QA del nuevo runtime en teléfono.

**APK0.2.40/code42 generada** con el cobro corregido; build verificado, aún no instalado. Cada victoria real cobra50%ventas+2×sobrante, por intento; recupera$3570del último recibo del teléfono (test compra$825faltantes de3y deja$2745). 32regresiones aprobadas, más dos auditorías11desde progreso existente. Campaña fresca ganó1–9, perdió10por13Chori en mejor política; no se declara recorrido1–11 completo. Evidencia: [status.md](status.md).

## 2. Rama, versión y entorno de referencia

- **Checkout real:** `/Users/celestino/HayChoriYPaty`; rama `codex/street-automation`, HEAD `1f83e6c` (`Fix Mercado victory payout and ship Android 0.2.40`), también `origin/codex/street-automation`; commit anterior de instalación `f077324`; remoto `https://github.com/Cuervation/haychoriypaty.git`. Push verificado.
- La carpeta `/Users/celestino/Documents/ChatGPT/Hay Chori y Paty` es una copia inicial sin commits, no el juego activo. No implementar sobre ella ni sincronizar sus assets/código.
- Unity **6000.6.3f1**, Main, Android; PlayerSettings **0.2.40/code42** (APK generada, no instalada). Build Development IL2CPP/ARM64, firma debug (no release Play Store). Artefacto y hash en [android-prototype.md](features/android-prototype.md).
- App instalada **26.1002.52244/build13536**, CLI **0.162.0-alpha.2**. Roles conservados: architecture Sol/medium; implementation Luna/medium; validation Luna/low. `.codex/config.toml`: dos subagentes más el principal. Config global leída, no modificada: `gpt-6.1-sol`/`xhigh`; no certifica un override de modelo/esfuerzo de este chat.
- **Bloqueo de configuración:** `config/read` del CLI marca inactiva la capa `.codex` del checkout real por falta de trust. `skills/list` sí descubre los cuatro skills. Los tres roles están expuestos en este chat existente; esto no prueba que una nueva sesión CLI cargue la configuración del checkout real. Seleccionar ese proyecto y autorizar su trust es un paso pendiente del usuario: la tarea prohíbe cambiar configuración global.
- MCP recuperado y comprobado el2026-10-16: instancia `HayChoriYPaty@1baadce415f1081d`, raíz correcta, Main, editor detenido/idle, sin compilación ni tests. La instancia histórica `hcyp-visual-test` no es evidencia del checkout real; al reconectar solo se detectó el principal. Revalidar conexión al retomar.

## 3. Funcionalidades implementadas

- Once clubes con catálogo progresivo de siete productos, cuotas independientes, FIFO y paciencia por cliente; ingresos solo al entregar físicamente.
- Contratación sin tope de gameplay, cinco roles disponibles según catálogo/modo, velocidad limitada, reinicio del equipo/velocidad en cada partida.
- Cocina modular y stock físico; desde nivel 6 cocineros activos y Mozo exclusivo para sándwiches. Bebidas entregadas por sus roles.
- Mercado/Heladera en una sola pantalla IMGUI, saldo e inventario persistentes, recetas/reservas y compras cash-only; restauración de stock al perder y liquidación por intento de cada victoria real. Recuperación exacta del último recibo antiguo válido, reparaciones legacy y premio acotado al primer desbloqueo.
- Portada/selector, HUD por productos, mejoras, animación y trifulcas integradas. Conservar aceptación abierta de cada spec: implementación no equivale a QA universal.

## 4. Arquitectura actual

`StreetGame` coordina lifecycle/save v4; `StreetSimulation` y `StreetKitchenProduction` son el dominio. `StreetView` es IMGUI/input; renderers/proyección de workers y prefabs SpriteRenderer generan capas transparentes en vivo. `StreetSceneLayout` fija escena raíz; `StreetKitchenLayout` fija geometría/rutas. `StreetWorkstationLayout` y el prototipo anterior son compatibilidad histórica. Detalle: [architecture.md](architecture.md).

## 5. Mecánicas vigentes

- Un Parrillero gratis, resto de roles en cero y velocidad ×1,00 al iniciar/cargar/seleccionar nueva partida; saldo inicial de intento según tabla, no compras previas. Progreso de niveles y Mercado sí persisten.
- Niveles 1–5: producción y entrega base. Desde Racing (índice5): Parrillero/Premium → parrilla → mesa → Mozo → cliente; cocción configurable **0,5 / WorkRate s**. Coca/Cerveza → Cocacolero; Fernet → Fernetero. Cocinar/depositar no genera dinero.
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
- Economía2026-10-16:32/32regresiones únicas aprobadas (15dominio+9integración+8payout), más2/2auditorías separadas de11. Benchmarks de campaña fresca pierden10; conservar ese límite explícito. Resultados/ledgers en `Logs/Acceptance/EconomyAudit-20261015/`; no sumar reruns como casos nuevos. La evidencia histórica previa a cocina activa no certifica balance actual.
- Build 0.2.39: éxito en516,95s, 0errores/2advertencias; ZIP, metadatos, ARM64 y firma v2 verificados. Instalación `-r` Success y versión verificada, sin uninstall/data clear.
- Auditoría de contexto: TOML/frontmatter/modelos, descubrimiento de skills, enlaces/rutas y diff; detalle en [status.md](status.md). Sin Unity tests, PlayMode ni nueva APK en la auditoría.

## 8. Validaciones todavía no realizadas

Reintento PlayMode posterior a restauración falló internamente antes de casos; el job EditMode de empaquetado no inició por timeout. No declararlos aprobados. La nueva campaña se ejecutó con motor real en EditMode tras restaurar geometría; no hubo revisión física del gameplay ni PlayMode nuevo. La corrección está empaquetada en0.2.40, aún no instalada ni validada en dispositivo. No prometer certificación universal de resoluciones/rendimiento.

## 9. Referencias y antecedentes

Usar [README.md](README.md) para elegir un contrato; [status.md](status.md) y [decisions.md](decisions.md) para evidencia puntual por fecha/tema. El checkpoint anterior íntegro está en [history/continuity-before-20261015-audit.md](history/continuity-before-20261015-audit.md); sus versiones/reglas/sin-probar no son estado actual. Builds, capturas y evidencia privada quedan ignorados en `Builds/`/`Logs/`, no en Git.

## 10. Próximos pasos autorizados

La solicitud vigente autoriza investigar economía desde el inicio, corregir el cobro de **cada victoria real**, probar y recuperar autónomamente la conexión Unity; preservar progreso. Falta resolver/aceptar el balance de10y repetir una campaña fresca ganadora1–11. No se autorizaron nuevos precios/metas/tiempos, configuración global/trust ni instalación en esta tarea. El pedido posterior autorizó build y push: APK0.2.40 generada y commit1f83e6c publicado. Sin pedido de instalación, el teléfono conserva0.2.39. No cambiar tiempos/precios ni campaña pendiente para forzar11/11.
