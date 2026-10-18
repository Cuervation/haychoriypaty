# Firebase player — contrato de integración

Fecha: 2026-10-16. **Estado: backend configurado y APK generada; aceptación del cliente real incompleta.** SDK oficial importado y compilado en Unity6000.6.3f1; Google y reglas ya desplegados; Firestore Standard nativo en São Paulo. Prueba IAM administrativa real aprobada; login de jugador, Google de jugador en Android todavía pendientes. Referencias: [Mercado/Heladera](market-fridge.md), [arquitectura](../architecture.md).

## Alcance e identidad

Destino único `haychoriypaty`; Android con application ID/certificados reales. No crear otro proyecto, habilitar facturación, servicios con cargos o elegir región permanente sin autorización. SDK/Auth Google oficiales mantenidos compatibles con Unity requieren verificación de documentación vigente. No cambiar economía, objetivos, tiempos, recetas, reservas físicas, recuperación ni siete productos; no implementar 5%.

Gate antes de inicializar `StreetGame`: inicializando → ingreso/recuperación → listo online/offline o error/conflicto. Mercado, navegación jugable y comandos requieren identidad válida, no solo ocultar botones. Primer login Google exige conexión real; sesión Firebase previamente recuperable habilita offline únicamente para su UID y cache. Sin invitado/anónimo; email no es clave. Cancelación/error tienen timeout y reintento; no bloquear hilo Unity.

Pantalla de acceso: el ingreso obligatorio usa el mismo botón azul estándar “JUGAR” y tipografía de menú del juego, con la marca G multicolor a la izquierda y el texto “JUGAR CON GOOGLE”; tocarlo conserva el mismo flujo de autenticación y no inicia una partida.

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

1. **Conexión:** CLI oficial autenticada con checelesti@gmail.com tras consentimiento explícito; acceso API confirmado a `haychoriypaty`. La consola muestra errores transitorios: ya no se interpreta como falta de permisos. El intento de agregar Firebase respondió que ya estaba habilitado.
2. **Google:** `firebase deploy --only auth --project haychoriypaty` completado; GET proveedor verifica Google enabled y clientId real. Marca Hay Chori y Paty, correo de soporte checelesti@gmail.com; no invitado/anónimo/email configurados. Primera creación/importación exige ACK; sesión recuperada confirmada permite offline. Login real del jugador aún pendiente.
3. **Firestore:** base `(default)` creada en región **southamerica-east1 (São Paulo)** elegida explícitamente por usuario, Standard/FIRESTORE_NATIVE, protección de borrado habilitada, PITR desactivado; API devuelve freeTier=true. Billing consultado read-only: **billingEnabled=false**. No se vinculó cuenta de facturación.
4. **Android:** app registrada y ACTIVE `com.haychoriypaty.game`, appId `1:800088676868:android:6b4b55811e38665dd57351`; se verificó ausencia antes de crear. SHA1/SHA256 debug registrados. `Assets/google-services.json` y `Resources/player-firebase-config.json` públicos reales descargados y concordantes con WebOAuth, sin clave privada/secret de cliente/tokens.
5. **SDK/archivos:** Firebase Unity14.0.0 App/Auth/Firestore y EDM1.2.190 oficiales en `Packages/FirebaseSDK`; CredentialManager1.6.0/googleid1.2.1, puente Java y marca Google oficial. No hay service account, tokens ni configuración inventada. `packages-lock.json` generado por Unity; cuatro paquetes registrados y compilación del Editor sin errores C#. EDM completó resolución Android y generó templates. Telemetría opcional de plugins EDM/Firebase desactivada; build guard desactiva Analytics nativo/AD ID (Auth trae Analytics transitivo).
6. **Modelo:** agregado `state/current`, recibos `operations` y `matches` separados, implementados en repositorio; todavía no creados en producción.
7. **Reglas:** deny-default, Google+UID, esquema/tipos/límites/CAS y atomicidad; **desplegadas en producción**; compilador remoto aprobado. Se consultaron releases y no había reglas previas; no se reemplazaron contratos ajenos.
8. **Sincronización:** código para v4 completo (saldo, diez stocks, desbloqueo, flags, recibos), estadísticas nuevas y partidos. No se subió progreso de jugadores. Prueba IAM aislada en `__integration_checks/{uuid}` creó/leyó/actualizó y limpió únicamente su documento nuevo con precondición de versión; ausencia posterior verificada.
9. **Migración:** backup exacto previo, consentimiento de asociación, propietario único, original PlayerPrefs intacto; remoto existente no recibe suma de legado. Cobertura histórica parcial con fecha de instrumentación y gasto/unidades legacy conocidos, sin historia fabricada.
10. **Offline:** archivo por UID con temp/flush/reemplazo/checksum, cola ordenada durable y ACK; cap512 bloquea sin descartar. Conflicto congela rama, conserva archivo y permite adoptar remoto explícitamente. Desinstalar puede perder operaciones nunca subidas; no se promete recuperación imposible.
11. **Pruebas ejecutadas:** emulador Firestore1.22.0 **187/187 comprobaciones únicas**; compilación estática Roslyn de runtime Editor/Android, herramientas Editor y assemblies EditMode/PlayMode; javac del puente contra AAR oficiales y Android36. XML/JS/diff revisados. Pruebas Unity ejecutadas tras importar SDK: **46/47 EditMode**, con una falla en `ActualHandoffsAndAdaptivePurchasesFundAllElevenFirstUnlockTransitions` (pierde nivel6); las regresiones nuevas de estadísticas/repositorio/gate y lifecycle pasan. **4/4 PlayMode** Mercado/input. Se corrigió ausencia de matches: JsonUtility reconstruye null inline como objeto vacío; normalizar por IDs evita resultados fantasma al reiniciar/copy/ACK. Además, roundtrip Firestore real autenticado con Google OAuth IAM administrativo y rechazo sin sesión aprobados. **No equivale a login Firebase del jugador ni valida reglas usando su token**. Gate manual en PlayMode normal: LoginRequired, IsPlayerActive=false, TryStartRound=false, seam de tests=false, cero errores. Importador de G oficial corregido a Texture2D (antes Cubemap por .meta incompleto); fuente cargada. Hook de privacidad probado dos veces sin duplicar metadata.
12. **Pendientes:** APK Development **0.2.43/code45** IL2CPP/ARM64 generada en el Editor6000.6.3f1: build final **Succeeded,0 errores/1 warning existente** de símbolos de diagnóstico,23,03s incremental. Sin warning de código sin compilar ni del importador de fuente. Archivo `Builds/Android/HayChoriYPaty-0.2.43-Firebase.apk`,421586297bytes, SHA-256 `9365406962aa2a3542be344053673b13cf019bd6b3b9f3e8560043d168a4cdc8`. ZIP/firma debug SHA registrada, package/code45, ARM64, librerías Firebase, clases CredentialManager/puente y flags de privacidad del manifest verificados. Launcher versionado espera Editor idle y Main limpio antes de compilar fuera del lock MCP. Google real en cliente Android, roundtrip con UID Firebase, reapertura/dispositivo y conflictos de dos clientes reales pendientes. Falla de campaña nivel6 abierta: no se cambió economía para ocultarla. Usuario pulsó Reload: MCP confirma Editor idle y Main sin cambios pendientes. AppleScript autorizado pero macOS había rechazado Accesibilidad; no se operó el diálogo por script. UPM y compilación completados, tests ejecutados según punto11. No se instaló/desinstaló nada en el dispositivo.
13. **Límites:** cliente manipulable; reglas no pueden interpretar snapshot JSON ni demostrar una victoria legítima. CAS evita doble gasto concurrente pero no fusiona dos ramas offline. Solo primera cadena válida confirma; historial inmutable. No servidor antitrampas ni restauración automática de backup corrupto.
14. **Costos:** no se habilitó facturación ni servicios pagos. Plan/cuotas reales del proyecto sin verificar. Transacción normal lee recibo+estado (y match cuando corresponde) y escribe estado+recibo(+match); hay lecturas adicionales/reintentos. No red por cocción/venta. Consultar cuotas y precios oficiales antes de producción: https://firebase.google.com/docs/firestore/quotas y https://firebase.google.com/pricing. No garantía de costo cero.
15. **Pendientes operativos:** APK0.2.44/code46 Development IL2CPP/ARM64 generada y verificada; evidencia completa en [android-prototype.md](android-prototype.md). `StreetMarketViewTests` PlayMode9/9. Login Google real del jugador y transacción/UID Firebase en dispositivo siguen pendientes; la prueba IAM administrativa no los sustituye. La campaña EditMode conserva falla de economía/nivel6. APK 0.2.44 instalada en Motorola Edge 60 Fusion con autorización, sin abrirla.


Certificado debug actual SHA1: `BF:98:58:16:6F:75:5A:AC:91:A3:3D:A7:40:90:32:B1:DA:F1:FC:61`; SHA256: `D2:FC:25:71:0A:B2:23:85:B7:C6:97:91:59:C0:EF:64:7E:91:67:2A:A9:F6:A8:67:29:16:E5:2D:F2:E2:8B:5E`. Son certificados debug registrados, no certificado release. Evidencia real administrativa: `/private/tmp/hcyp-firebase-live-admin-roundtrip.json` (sin tokens).

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

### Decisiones de importación y privacidad

Conservar GUID al reserializar importadores de imagen/fuente; un .meta mínimo no configura correctamente una Texture2D moderna. La dependencia Android firebase-analytics es transitiva del SDK oficial Auth; no se añadió Unity Analytics ni se habilitó Google Analytics del proyecto. El hook de build fija `firebase_analytics_collection_deactivated=true` y `google_analytics_adid_collection_enabled=false` conforme [documentación oficial](https://firebase.google.com/docs/analytics/android/configure-data-collection). No alterar tiempos/objetivos para conseguir un test de campaña verde.


### Entorno administrativo autorizado de esta tarea

La sesión de CLI se mantiene fuera del repositorio y no se versiona. En este equipo, para los comandos anteriores:

```sh
export XDG_CONFIG_HOME=/private/tmp/hcyp-firebase-auth
export FIREBASE_CLI_ENTRY=/private/tmp/hcyp-firebase-tools/node_modules/firebase-tools/lib/bin/firebase.js
export PATH=/Users/celestino/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin:$PATH
```

La autenticación oficial por localhost se completó con consentimiento explícito. No usar `login:ci`, no copiar tokens/códigos de callback al chat, no imprimir `signIn.hashConfig` de las APIs administrativas. No conceder regiones/billing a partir de mensajes de error de la consola. La prueba IAM solo utilizó un documento nuevo UUID, borrado condicionalmente tras validar ownership/updateTime; ningún usuario ni progreso existente fue alterado.
