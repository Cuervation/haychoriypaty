# Feature: Portada y hub de día de partido

## Alcance vigente — 2026-10-23

La portada reproduce la ilustración proporcionada por el usuario, a pantalla completa y sin botones. Tras autenticar/cargar el perfil obligatorio y mostrarla durante cuatro segundos, el juego avanza automáticamente al hub; la barra de carga es compacta (118×6 unidades lógicas), dibujada por el juego y no forma parte del arte fuente. El cronómetro no consume tiempo mientras la sesión bloquea el juego.

El hub es una escena ilustrada vertical, vista elevada de una calle de partido: se ve parte del estadio, viviendas/vereda, hinchas y un control policial donde se revisan DNI. Los puestos ilustrados funcionan como hotspots, dentro de los límites de su arte y respetando el área segura del canvas:

| Elemento del escenario | Acción | Destino / límite |
|---|---|---|
| Puesto de choripán | JUGAR | Selector de clubes existente; el jugador sigue el flujo de nivel y Mercado actual. No salta la selección ni inicia una partida sin la preparación vigente. |
| Almacén/supermercado | MERCADO | Pantalla Mercado existente; comprar sigue sujeto a saldo/stock/gates actuales. |
| Vendedor ambulante de camisetas | INSIGNIAS | Página de colección provisional, tematizada como el puesto de cancha con camisetas colgadas y en el suelo. La colección/estado se deriva de unlocks y victorias por club ya guardadas (`winsByLevel`); este hub no inventa persistencia de insignias ni premios. |
| Tablero del estadio | RANKING | Muestra canchas desbloqueadas y canchas ya ganadas en el perfil propio, y aclara que el ranking global está pendiente. No consulta ni expone una lista pública de otros jugadores: el contrato Firebase actual no autoriza ese acceso. |
| Avatar/engranaje | CUENTA | Reutiliza el estado/modal Firebase/Google existente para iniciar sesión o cerrar sesión; no modifica la obligación de autenticación. |

Las páginas nuevas vuelven al hub; se reutiliza IMGUI y las rutas/mercado existentes, no se agrega un segundo framework de UI ni servicios. El arte del hub es 941×1672 y sus hitboxes se mapean de la composición full-bleed a la safe area con coordenadas GUI top-origin.

## Aceptación

- [x] Portada sin botones superpuestos y barra de progreso pequeña; pasa automáticamente al hub.
- [x] Hub con hotspots para jugar, Mercado, Insignias, Ranking y Cuenta; entrada/salida preserva la autenticación y flujos actuales.
- [x] Insignias muestran colección temporal derivada de desbloqueos/victorias ya guardados, sin reclamar persistencia o premios nuevos; la última también puede obtenerse al ganar Los Redondos. Ranking no presenta datos globales ficticios.
- [x] Mapeo de hotspots cubierto por prueba geométrica con insets superiores/inferiores asimétricos.
- [ ] Compilar, ejecutar pruebas Unity afectadas y revisar el Game View/dispositivo: pendiente porque el Editor del checkout activo está abierto y no hay Unity MCP disponible; no iniciar batch Unity en paralelo.
- [ ] Generar y verificar APK Android versionName 0.2.64 / versionCode 66 una vez que la validación/build puedan ejecutarse con seguridad.

La aceptación marcada se refiere a implementación/evidencia estática, no certifica ejecución ni apariencia runtime.
