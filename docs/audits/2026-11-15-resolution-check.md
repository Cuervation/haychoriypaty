# Revisión de resoluciones portrait (2026-11-15)

Método: prueba temporal de PlayMode (retirada, no versionada) que fija el Game view con `PortraitGameView` y captura hub, selector, Mercado y partida (Nivel 1) en 540x960, 720x1280, 1080x2400, 720x1600, 1080x1600 y 1200x1600. Capturas locales en `Logs/Acceptance/Resolutions/` (no versionadas). Revisadas a mano: 1200x1600 (tablet 3:4), 720x1600 (angosto/alto) y 1080x2400 (hub), más las demás por archivo.

## Resultado
- **Partida:** HUD, mostrador, cocina, tarjetas de mejoras y banderines caben y se leen en todos los tamaños revisados; en 720x1600 queda más aire entre las tarjetas y los banderines (aceptable).
- **Mercado:** todos los bloques (cabecera, objetivos, saldo, pestañas, lista, costo, JUGAR PARTIDO) visibles y sin recortes en 1200x1600.
- **Selector:** grilla de 2 columnas con VOLVER visible en 720x1600.
- **Hub:** la ilustración se recorta tipo "cover" según el aspecto, pero JUGAR, RANKING, INSIGNIAS y MERCADO quedan visibles y tocables en 3:4, 9:20 y 9:22.

## Hallazgo (corregido: ver al final)
Los controles superiores del hub (ajustes, música y el botón de cuenta) tienen tamaño fijo en **píxeles de pantalla** (`HubSettingsButtonWidth = 48`, `HubTopMusicButtonSize = 44`, cuenta 110x32; `StreetView.cs`) y no escalan con el canvas (`ancho/540`). En 1080x2400 se ven y tocan a ~3 % del ancho (el botón de cuenta ~70x20 px visibles); en 720x1600 aún más chicos que en 1200x1600. En un teléfono real de 1080 px de ancho eso equivale a ~16 dp, por debajo de los 48 dp recomendados para tocar. El resto de la UI sí escala.
Corrección posible: multiplicar esas medidas por `Screen.width / 540` (con tope) y actualizar el contrato del hub. Cambia tamaños de UI y tests de límites del hub, por eso no se aplicó sin confirmar.

## No cubierto
- Notch/recortes: en el Editor `Screen.safeArea` es toda la pantalla; requiere dispositivo o simulador.
- Pantallas de victoria/derrota, Tower Defense y tutoriales no se capturaron en esta pasada.

## Corrección aplicada (2026-11-15, a pedido)
`StreetView.HubScreen.cs`: nuevo `HubControlScale(screenSize) = clamp(ancho/540, 1, 3)`. Escalan con él el botón de cuenta (rect completo), los mínimos de toque de ajustes (48) y música (44), y los márgenes/huecos del fallback de música. A 540 px o menos no cambia nada; en 1080 px mide el doble; el tope es 3x. La ilustración del hub (engranaje y altavoz horneados en el arte) no se tocó: solo crecen las zonas táctiles y el botón de cuenta.
Pruebas: nuevo `StreetHubControlScaleTests` (EditMode: escala, límites en pantalla y no solapamiento a 540x960, 720x1600 y 1080x2400); `StreetHubSignageTests` 14/14 con él; PlayMode `StreetHubViewTests`+`HubTouchAnimationViewTests`+`StreetPointerTests`: 47 pasan, 0 fallan, 28 ignorados.
Sin verificar visualmente el botón de cuenta (el arnés de pruebas lo desactiva) ni en dispositivo.
