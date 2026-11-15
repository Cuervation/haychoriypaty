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

## Segunda pasada: victoria, derrota, Tower Defense (2026-11-15)
Misma metodología (prueba temporal, retirada) en 540x960, 720x1280, 720x1600, 1080x2400 y 1200x1600. Capturas locales en `Logs/Acceptance/Screens/` (no versionadas).
- **Victoria (Chulengo):** panel, medidor, "COBRAR" y "MULTIPLICAR" caben y se leen en todos los tamaños revisados (720x1600 y referencia 540x960 comparados). La mitad inferior vacía del pergamino es igual en la referencia, no depende de la resolución.
- **Derrota (Game Over):** escena, texto y "VOLVER A NIVELES" completos en 720x1600.
- **Tower Defense (Camioneros):** mapa, vallas, mesas, barriles y las dos tarjetas de contratación completas en 720x1600 y 1200x1600; sin recortes.
- **Sin resultado:** los **tutoriales** no se pudieron mostrar (la prueba terminó en la partida normal) y quedan sin verificar; tampoco se cubrió el notch (requiere dispositivo).
- No se encontraron problemas nuevos de layout en estas pantallas.

## Tutoriales (2026-11-15)
Los seis tutoriales de nivel se forzaron activando su bandera con el nivel seleccionado y se capturaron en 540x960, 720x1600, 1080x2400 y 1200x1600 (24 capturas locales en `Logs/Acceptance/Tutorials/`; revisadas a mano: Nivel 1, 2, 5 y 6 en varios tamaños). Prueba temporal retirada.
- **Teléfonos (540x960, 720x1600, 1080x2400):** burbuja, cola apuntando al elemento correcto, SIGUIENTE y SALTAR caben y se leen. Sin problemas.
- **Tablet 3:4 (1200x1600), tutorial del Nivel 6 (6 tarjetas):** el pie de cartas queda apretado. Se recortan los títulos de las tarjetas ("PARRILLERC", ":OCACOLERC" por cortarse la O/C del borde) y el precio de VELOCIDAD pisa el texto "+10%". Es el único problema de layout hallado en esta revisión; corregido más abajo.
- **Observación menor:** SALTAR es un rectángulo blanco pequeño (~170x55 px en 1080x2400); aceptable pero chico como zona de toque en pantallas muy densas.

## Corrección del pie de mejoras en tablet (2026-11-15, a pedido)
Causa: en canvases cortos (`layoutVerticalScale < 1`, p. ej. 3:4) `ScaleCardBounds` achica el ancho y el alto de cada tarjeta, pero su contenido interno (título, estado, precio, medallón) usa offsets y tipografías fijos; con el rectángulo reducido se recortaban los títulos y el precio pisaba el estado.
Arreglo (`StreetView.Upgrades.cs`, `Upgrade`): el contenido se dibuja con el tamaño original de la tarjeta (ancho/alto divididos por la escala) bajo una matriz que lo reduce en bloque; el área táctil sigue siendo el rectángulo real. En escala 1 (teléfonos) no hay cambio.
Verificación: capturas a 1200x1600 (Nivel 6, 6 tarjetas) con títulos y precios completos; 720x1600 sin cambios; PlayMode `StreetPointerTests`+`StreetActiveCooksPointerTests`+`TowerDefensePlacementViewTests`+`StreetHubViewTests`: 78 pasan, 0 fallan, 34 ignorados.
