# Firebase player — contrato de integración

Fecha: 2026-10-16. **Estado: implementación local preparada; integración real bloqueada y aceptación incompleta.** SDK oficial incorporado como paquetes locales, todavía sin resolución UPM confirmada; proveedor, reglas desplegadas, conexión real y Android pendientes. Referencias: [Mercado/Heladera](market-fridge.md), [arquitectura](../architecture.md).

## Alcance e identidad

Destino único `haychoriypaty`; Android con application ID/certificados reales. No crear otro proyecto, habilitar facturación, servicios con cargos o elegir región permanente sin autorización. SDK/Auth Google oficiales mantenidos compatibles con Unity requieren verificación de documentación vigente. No cambiar economía, objetivos, tiempos, recetas, reservas físicas, recuperación ni siete productos; no implementar 5%.

Gate antes de inicializar `StreetGame`: inicializando → ingreso/recuperación → listo online/offline o error/conflicto. Mercado, navegación jugable y comandos requieren identidad válida, no solo ocultar botones. Primer login Google exige conexión real; sesión Firebase previamente recuperable habilita offline únicamente para su UID y cache. Sin invitado/anónimo; email no es clave. Cancelación/error tienen timeout y reintento; no bloquear hilo Unity.

Logout explícito cierra sesión, bloquea acceso y conserva cache/outbox separados del UID. No reutilizar un bool propio como autenticación. Cambio de cuenta desmonta simulación/referencias anteriores; callbacks asíncronos verifican UID y generación de sesión. No transferir datos entre cuentas. Una ronda activa debe cerrarse explícitamente o esperarse antes de logout, no abandonarla silenciosamente.

## Persistencia local y migración v4

Mantener v4 como payload de dominio compatible dentro de un envelope versionado por UID: `schemaVersion`, `uid`, `acknowledgedRevision`, `snapshot`, `pending`, `legacySourceHash`, `lastAcknowledgedMatchId`, `migrationCommitted`; wrapper con SHA256. Estadísticas y `activeAttemptSummary` viven dentro del snapshot v4. No se implementó installationId/branchId/localSequence: UUID de operación + CAS identifica cada cadena. Snapshot y operación/contadores relacionados se escriben en **una unidad local duradera** antes de confirmar éxito. Archivo temporal+flush+reemplazo y copia buena previa son una estrategia admisible. No separar saldo y outbox en dos claves que puedan quedar descoordinadas. Error de escritura conserva copia previa e impide confirmar comando.

1. Capturar bytes/fingerprint del PlayerPrefs `HayChoriYPaty.StreetGame.v1` antes de `OnEnable` actual, que inicializa/repara y guarda inmediatamente. No atribuirlo a UID desconocido.
2. Migrar v1–v4 sobre copia con lógica vigente; guardar manifiesto sourceHash, versión, UID destino y pending/committed. Original intacto como backup; corrupción se conserva en cuarentena, no se convierte silenciosamente en perfil nuevo.
3. `RecoverAbandonedAttempt` restaura attemptStock como hoy; no resume ni inventa resultado de ronda incompleta. Conservar reparaciones legacy, flags y watermarks, aplicadas una sola vez.
4. Si remoto no existe, create-only transaccional importa snapshot y recibo de migración; solo después del ACK marca completado. Si existe, cargar remoto y proteger v4 como rama recuperable: **no sumar ni reemplazar remoto**. Vinculación de legado sin propietario al primer UID exige confirmación clara única, no transferible a otra cuenta.
5. Cuenta nueva: kit y estado inicial en un único create-only con recibo. Reinstalación lee remoto, nunca otorga kit por falta de PlayerPrefs. Mantener aparte el piso vigente de nivel1 (`EnsureLevelOneStartingStock`), que no es un kit nuevo.

Datos reconstruibles: unlock, precios, StreetMarketData completo, saldo, stocks/porciones, IDs compra/intento, grants/reparaciones, watermarks y último recibo. `cumulativePurchaseUnits`/`cumulativePurchaseSpendCents` son exactos pero no tienen desglose por ingrediente. No existe historia completa: no reconstruir ventas, clientes, contrataciones o speed de metas/unlocks/workers legacy. Nuevos contadores comienzan desde instrumentationStartedAt, cobertura parcial registrada, totales legacy conocidos separados. Último recibo puede exhibirse como legado sin inventar fecha, UUID o ventas por producto.

## Esquema remoto (schemaVersion 1)

| Ruta | Contenido |
|---|---|
| players/{uid} | sin documento raíz requerido; namespace del propietario |
| players/{uid}/state/current | `schemaVersion`, `uid`, `revision`, `operationId`, `payloadHash`, `snapshot` v4 JSON completo acotado, `unlockedLevel`, `balanceCents`, `stockUnits`, `updatedAt` servidor |
| players/{uid}/matches/{matchId} | resultado inmutable de intento finalizado o abandono explícito confirmado |
| players/{uid}/operations/{operationId} | `schemaVersion`, `uid`, `operationId`, `payloadHash`, `previousRevision`, `revision`, `createdAt` servidor, `matchId` opcional |

Un agregado acotado de estado (7 productos/10 ingredientes/11 niveles) simplifica CAS y evita leer documentos de revisiones distintas. Historial y recibos paginados fuera del agregado, sin arrays ilimitados. Si se divide estado en futuro, revisión compartida y transacción única.

Dinero: signed64 en centavos, nunca float; convertir monedas enteras de partido por100 y rechazar overflow. Stocks enteros no negativos; ingredientes8/9 ya son porciones, pack4: no redondear a botellas ni crear stock duplicado. Fechas locales no confiables separadas de committedAt servidor; offline no promete fecha servidor del juego.

Productos IDs:0 Chori,1 Paty,2 Bondiola,3 Vacío,4 Coca,5 Fernet,6 Cerveza. Ingredientes IDs:0 Chorizo,1 Pan común,2 Paty,3 Pan Paty,4 Bondiola,5 Vacío,6 Coca,7 Cerveza,8 Fernet,9 Coca grande.

## Outbox y transacción idempotente

UUID de operación y UUID de intento generados una vez y persistidos antes del efecto; preservar IDs enteros v4 para deduplicación de dominio, **no son únicos entre celulares**. No usar timestamp como ID. Operación local contiene UUID, snapshot, hash estable, expectedRevision y match opcional; el repositorio del UID fija el propietario.

Flush online en orden, un único coordinador por UID, backoff acotado (2–120 segundos, sin jitter):

1. Leer recibo y estado en transacción. Recibo existente+hash igual implica ACK sin aplicar otra vez aunque revisión avanzó; hash distinto implica error.
2. Sin recibo exigir remoteRevision==baseRevision; validar consistencia y límites.
3. Estado revision+1, recibo create-only y match/estadísticas relacionados se escriben en **la misma transacción**. Unlock y bonus forman parte del cierre, no otro cobro independiente.
4. Retirar outbox solo después de persistir ACK. Timeout tras commit reconsulta recibo con mismo UUID; nunca inventa otro.

No usar escrituras offline ciegas del SDK para economía: outbox controla orden y transacciones solo online. No sincronizar por frame/cocción/entrega. No podar recibos sin protocolo de compactación que mantenga deduplicación; inicialmente conservarlos. Reinstalación recupera lo confirmado, no puede recuperar outbox nunca subida y borrada por desinstalación sin backup externo.

### Restricción segura para dos celulares offline

Ambos pueden jugar localmente, pero su progreso offline es **pendiente**. Solo la primera cadena que satisface CAS se incorpora al estado compartido. No es posible aceptar dos ramas que gastan el mismo saldo/stock sin autoridad adicional o cambiar reglas.

Revisión ajena: parar flush y nuevos comandos económicos de esa rama, conservar íntegros snapshot/resultados/outbox en archivo de conflicto. No rebase automático, no reenviar victoria contra otra revisión, no sumar snapshots, no last-write-wins. Mostrar conflicto y opción segura **continuar con estado sincronizado conservando copia local**: no importa esa rama ni elimina su copia exportable/recuperable. Operaciones ya aceptadas se identifican por recibos. Advertir esta limitación antes de uso offline multidispositivo y distinguir pendiente/sincronizado/conflicto. No prometer fusión eventual de ambas ramas. Aceptar todas las ramas requeriría autoridad/escrow/servidor específico, fuera de esta entrega y sin costo habilitado por defecto.

## Estadísticas e historial

Contadores instrumentados separados de saldo y grants gratuitos; sumar una vez por operación aceptada. Cache muestra pendientes sin confundirlos con estado remoto de otra rama.

- Compras: unidades/gasto por ingrediente[10] y totales existentes, solo éxito confirmado localmente.
- Ventas: entregas por producto[7] y facturación real, nunca cocción/reserva/cancelación. Cliente atendido=al menos una entrega; pedido completo=todas las líneas entregadas; impaciencia solo salida por paciencia, no sold-out/cancelación.
- Intentos iniciados solo BeginAttempt+guardado exitoso; finalizados=won+lost. Abandono solo evento explícito seguro; crash tiene recuperación técnica, no resultado inventado.
- Contrataciones pagas por rol[5], costo previo al débito; excluir Parrillero inicial. Speed compras/gasto previo al incremento. Equipos/speed/monedas temporales nunca se restauran como siguiente plantilla.
- Records por nivel: menor tiempo de victoria, mayor facturación finalizada, más unidades vendidas; empates conservan previo. Cobertura legacy parcial.
- Rewards de liquidación, bonus de progresión/reparación, grants iniciales y ventas separados; nunca contar dos veces bonus ya incluido en recibo.
- Consumo físico y débito neto de inventario distintos: derrota tiene entregas reales pero stock restaurado. No restar consumo otra vez al subir.

Match: UID, UUID intento/match, nivel/club, outcome, elapsed, fecha local/servidor, ventas[7], facturación, gastos workers/speed, monedas iniciales/sobrantes, componentes reward, consumo[10], stock restante[10], contrataciones pagas[5], clientes/pedidos, records. Crear después de liquidación+progresión final guardada; no guardar movimientos físicos.

## Puntos exactos de integración

| Código vigente | Extensión mínima |
|---|---|
| StreetGame.OnEnable/Load/Save | gate antes de inicialización v4, repositorio por UID, envelope atómico snapshot+outbox |
| StreetGame.TryStartRound | UUID global/started con snapshot guardado antes de sim.StartRound; rollback si falla |
| StreetGame.TryBuyIngredient | compra/estadística en mismo guardado; conservar rollback existente |
| StreetGame.HireRole/TryUpgradeSpeed | medir precio previo/éxito; checkpoint y rollback ante persistencia fallida |
| StreetGame.ProcessRoundResult | operación terminal única después de SettleAttempt+bonus/unlock; reintentar Save no recontabiliza |
| StreetGame.ReturnToLevels/FinishOrAbort | distinguir cierre explícito de lifecycle incierto; preservar AbortAttempt |
| StreetMarket.BeginAttempt/SettleAttempt/AbortAttempt/RecoverAbandonedAttempt | conservar snapshots/IDs/flags; exponer recibo/consumo, sin red ni recálculo económico |
| StreetSimulation.Deliver | registrar tras Kitchen.Consume válido; Delivered/GetProductDelivered/CoinsEarned ya existen |
| StreetSimulation.AdvanceCustomers/SetLeaving | motivo de impaciencia explícito y contado una vez, sin alterar comportamiento |
| StreetKitchenProduction/IStreetKitchenStock | sin red por unidad ni persistir reservas/movimientos físicos |
| StreetView | acceso Google, logout, error/sync/conflicto y gate de acciones, sin rediseñar gameplay |

`OrderCompleted` actual sirve sonido y no sustituye ventas: verificar sus early-return antes de reutilizarlo para métricas; instrumentar finalización real de pedido una vez. Nuevos contadores guardados localmente en checkpoints y pausa, sin resumir ronda en carga; no depender de quit, que puede no ejecutarse. En guardados existentes durante Playing se captura un checkpoint de métricas reales: al recuperar un intento interrumpido coincidente se aplica una sola vez, sin match, outcome, premio, victoria, derrota ni abandono. La cola terminal borra ese checkpoint. No se añadió guardado por frame/producto ni timer periódico.

## Seguridad, operación y límites

Reglas deny por defecto, auth UID propio en todas las subrutas; Google como proveedor conforme identidad válida; sin listado global/datos ajenos. Campos permitidos, tipos/tamaños/no negativos/schema, revision+1 y vínculo transaccional estado-recibo verificables. Matches/recibos create-only, sin actualización/eliminación cliente. Nunca desplegar reglas abiertas ni presentar uid==uid como antitrampas.

Cliente+Firestore protege aislamiento/estructura/concurrencia, **no prueba victoria legítima** frente APK alterada. Sin servidor verificador no prometer economía competitiva autoritativa. No secretos, service-account keys ni tokens en Git/logs; google-services.json es configuración pública, no credencial admin, aun así revisar antes de versionar. Reglas/índices/config de destino versionados. Verificar cuotas actuales antes de despliegue; no prometer costo cero ni activar Functions/facturación/servicios con cargos. Firestore inexistente+región no elegida requiere decisión del propietario, sin frenar trabajo local independiente.

## Aceptación (pendiente hasta evidencia)

- [ ] Google real obligatorio, errores/cancelación/reintento, offline recuperado, logout/cambio UID y comandos sin sesión bloqueados.
- [ ] Roundtrip/migración v4 preservan stocks/porciones/saldo/flags/watermarks/precios/unlock; backups y conflicto remoto; import idempotente y reinstalación sin kit nuevo.
- [ ] Compra sin saldo/overflow rechazada, Save fallido revierte, mismo UUID no duplica, hash distinto rechazado; liquidación/política derrota/grants intactos.
- [ ] Resultado+stats+unlock+inventario atómicos, terminal reintentado/reiniciado no duplica, crash no inventa resultado.
- [ ] Ventas[7], ingredientes[10], workers[5], speed/clientes/pedidos/records desde eventos reales; cobertura legacy parcial visible.
- [ ] Outbox reinicio, timeout antes/después commit, ACK perdido, orden alterado y duplicados probados.
- [ ] Dos dispositivos misma base: solo uno confirma; otro conflicto visible/copia íntegra, sin rebase/merge; cambio UID no envía operaciones ajenas.
- [ ] Reglas UID propio/ajeno/anónimo, documento inválido, revisión omitida/recibo reescrito probadas.
- [ ] Lectura/escritura autenticada **real** en haychoriypaty con datos aislados autorizados, no progreso real destructivo; mocks/emulador no sustituyen evidencia remota.
- [ ] Unity compila; Android IL2CPP/ARM64 dependencias resueltas y Google real/persistencia tras reapertura en dispositivo autorizado. No instalar/desinstalar sin permiso.

Validación documental: coherencia con código, vínculos y diff; no exige Unity. Informe final separa implementado, probado local/remoto, desplegado, bloqueado por permisos/región y no ejecutado; ninguna casilla se aprueba por existir este diseño.


## Informe verificable de entrega — 2026-10-16

1. **Conexión:** consola abierta en `haychoriypaty` con checelesti@gmail.com responde proyecto inexistente/sin permiso. CLI sin sesión autorizada; flujo remoto oficial bloqueado por el navegador. No se verificó acceso real.
2. **Google:** código obligatorio implementado, sin invitado; proveedor/OAuth reales no inspeccionados ni habilitados por falta de acceso. Primera creación/importación exige ACK Firestore antes de jugar; offline solo con Firebase Google recuperado y perfil previamente confirmado.
3. **Firestore:** existencia/región/plan no verificados. No se creó base ni se eligió región.
4. **Android:** destino `com.haychoriypaty.game`; registro existente aún desconocido. `admin.mjs android-config` inspecciona paquetes antes de crear para evitar duplicados.
5. **SDK/archivos:** Firebase Unity14.0.0 App/Auth/Firestore y EDM1.2.190 oficiales en `Packages/FirebaseSDK`; CredentialManager1.6.0/googleid1.2.1, puente Java y marca Google oficial. No hay service account, tokens ni configuración inventada. `packages-lock.json` pendiente de Unity.
6. **Modelo:** agregado `state/current`, recibos `operations` y `matches` separados, implementados en repositorio; todavía no creados en producción.
7. **Reglas:** deny-default, Google+UID, esquema/tipos/límites/CAS y atomicidad; **no desplegadas**. Revisar reglas existentes antes de reemplazar.
8. **Sincronización:** código para v4 completo (saldo, diez stocks, desbloqueo, flags, recibos), estadísticas nuevas y partidos. Ningún dato real subido durante esta tarea.
9. **Migración:** backup exacto previo, consentimiento de asociación, propietario único, original PlayerPrefs intacto; remoto existente no recibe suma de legado. Cobertura histórica parcial con fecha de instrumentación y gasto/unidades legacy conocidos, sin historia fabricada.
10. **Offline:** archivo por UID con temp/flush/reemplazo/checksum, cola ordenada durable y ACK; cap512 bloquea sin descartar. Conflicto congela rama, conserva archivo y permite adoptar remoto explícitamente. Desinstalar puede perder operaciones nunca subidas; no se promete recuperación imposible.
11. **Pruebas ejecutadas:** emulador Firestore1.22.0 **187/187 comprobaciones únicas**; compilación estática Roslyn de runtime Editor/Android, herramientas Editor y assemblies EditMode/PlayMode; javac del puente contra AAR oficiales y Android36. XML/JS/diff revisados. No equivalen a tests Unity ejecutados.
12. **Pendientes:** importar/resolver UPM, ejecutar tests Unity focalizados (estadísticas/repo/economía/gate), revisar login/UI en GameView, Gradle IL2CPP/ARM64, Google real, roundtrip autenticado aislado, reapertura/dispositivo y conflictos de dos clientes reales. Editor detenido en diálogo de cambio externo de Main.unity; no se reinició ni descartó escena. No se generó APK ni se instaló/desinstaló nada.
13. **Límites:** cliente manipulable; reglas no pueden interpretar snapshot JSON ni demostrar una victoria legítima. CAS evita doble gasto concurrente pero no fusiona dos ramas offline. Solo primera cadena válida confirma; historial inmutable. No servidor antitrampas ni restauración automática de backup corrupto.
14. **Costos:** no se habilitó facturación ni servicios pagos. Plan/cuotas reales del proyecto sin verificar. Transacción normal lee recibo+estado (y match cuando corresponde) y escribe estado+recibo(+match); hay lecturas adicionales/reintentos. No red por cocción/venta. Consultar cuotas y precios oficiales antes de producción: https://firebase.google.com/docs/firestore/quotas y https://firebase.google.com/pricing. No garantía de costo cero.
15. **Acciones imprescindibles:** en la pestaña Firebase, avatar superior derecho → cambiar a cuenta propietaria/administradora con acceso a `haychoriypaty` (no enviar contraseñas/tokens). Alternativa: propietario en Configuración del proyecto → Usuarios y permisos concede el acceso mínimo necesario a la cuenta elegida. Cerrar el diálogo de Unity conservando la escena abierta, sin descartar ni guardar encima del cambio externo, para reanudar MCP. Una vez autorizada la CLI oficial, ejecutar preflight; comprobar Authentication → Sign-in method → Google y registrar los certificados Android; solo después generar configuración y hacer prueba real. Si Firestore no existe, solicitar región antes de crearlo.

Certificado debug actual SHA1: `BF:98:58:16:6F:75:5A:AC:91:A3:3D:A7:40:90:32:B1:DA:F1:FC:61`; SHA256: `D2:FC:25:71:0A:B2:23:85:B7:C6:97:91:59:C0:EF:64:7E:91:67:2A:A9:F6:A8:67:29:16:E5:2D:F2:E2:8B:5E`. No son certificado release ni se registraron aún.

### Comandos reproducibles

Usar Node instalado y Firebase CLI oficial, JDK21 para emulador (no cambiar JDK de Unity):

```sh
cd /Users/celestino/HayChoriYPaty
node tools/firebase/admin.mjs preflight
# Solo después de proveedor/OAuth y permisos reales:
node tools/firebase/admin.mjs android-config
# Dependencias de prueba declaradas en tools/firebase/package.json:
cd tools/firebase
pnpm install
pnpm run test:rules
```

`FIREBASE_CLI_ENTRY` permite indicar la ruta de la CLI oficial. Para autorizar una sesión usar `firebase login` en la cuenta propietaria (flujo oficial), nunca tokens pegados al chat. Revisar el proyecto exacto y reglas existentes antes de `firebase deploy --only firestore:rules --project haychoriypaty`. No ejecutar deploy a ciegas ni afirmar que estos comandos ya modificaron producción. Evidencia local temporal: `/private/tmp/hcyp-firebase-rules-final.log`; no versionar logs.
