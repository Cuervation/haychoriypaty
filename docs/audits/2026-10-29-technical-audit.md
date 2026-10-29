# Auditoría técnica y funcional — 2026-10-29

Estado: **incompleta**. Ganar niveles no es criterio de esta auditoría; no se modificó el balance para obtener victorias. Preservar los cambios locales preexistentes y el progreso del jugador.

## Cambios realizados

- Confirmación antes de que el menú histórico regenere `Main.unity` y sobrescriba ajustes; advertencia en README.
- Corrección de expectativas obsoletas de roster de San Lorenzo y suministro inagotable de Vélez. La estrategia de test contempla producción y entrega de cinco roles, sin retirar sus aserciones históricas de victoria.
- Fixture de movimiento en reposo aislado y `System.Type` corregido en prueba de música.
- Capturas del renderer de cocina dirigidas a carpetas únicas, evitando sobrescribir evidencias históricas.
- Smoke de UI y gameplay con progreso y presupuesto sintéticos en memoria; no exige victoria y omite la portada para no borrar el marcador real del tutorial.

## Evidencia ejecutada

- Unity 6000.6.3f1: compilación sin errores en la revisión; `StreetWorkerAnimationTests` 22/22, pruebas focalizadas de música 2/2, renderer de cocina PlayMode 21/21 tras corrección, smoke operacional UI 3/3 (niveles 1, 6 y 11). El smoke ejercita selección, tutorial o Mercado, inicio, primeros segundos y captura; usa valores sintéticos y no certifica campaña humana ni duración completa.
- `AndroidViewportTests` 4/4 y tanda de dominio 75/75. La tanda de 292 pruebas tuvo seis fallos iniciales; uno era roster obsoleto corregido, los demás son exigencias de estrategias ganadoras fuera del criterio solicitado. Con estrategia actualizada permanecen derrotas; no se reinterpretan como éxito ni como imposibilidad.
- `Main` cargó y el validador de escena informó cero Missing Scripts/prefabs rotos; el escaneo estático de GUID de diez escenas resolvió referencias frente a Assets y paquetes, sin certificar fileID ni carga runtime.
- Build Android Development Main-only, Unity 6000.6.3f1: `Builds/Android/HayChoriYPaty-audit-20261029-final.apk`, 409208735 bytes, SHA-256 `2192b1f48002349894c604eb744dff976f8f0056654ff5c5f0dedf5ded7c9370`; BuildPipeline Success en 300.66 s, 0 errores / 7 advertencias. ZIP, `zipalign -c -v 4`, firma APK v2 y metadata PASS: `com.haychoriypaty.game`, 0.2.77/code79, min API26/target36, ARM64. Firma Android Debug; no se instaló ni ejecutó. El artefacto previo `HayChoriYPaty-audit-20261029.apk` se preservó.

## Hallazgos y pendientes

- **FAIL visual:** en capturas Game View 1080×1920 de Racing y Los Redondos, la cuarta tarjeta de cuota sólida izquierda tapa parte de una burbuja de pedido. Evidencia: `Logs/Acceptance/TestRuns/e713ffad302040a38c5e4c5d5d773de5/operational-level-6.png` y `Logs/Acceptance/TestRuns/abecdce4cf2c4dddaf9df0c6260f4c06/operational-level-11.png`. Corregir con diseño que preserve HUD apilado, dos filas de burbujas y legibilidad en varias resoluciones; no basta cambiar z-order. No corregido.
- El arranque serializa 66 cargas; el checkpoint registra 99,03 s en AVD. Perfilado/optimización y medición posterior pendientes.
- Siete GUID no localizados en ajustes PC son campos URP obsoletos; otros cuatro aparecen también en el perfil estándar de URP. Falta verificar impacto del perfil global con AssetDatabase/runtime; no eliminarlos por búsqueda textual.
- Cinco fallos de estrategias de victoria permanecen históricos/fuera del objetivo actual; la prueba operacional no exige victoria. Quedan por comprobar transiciones terminales, más niveles, integración real Firebase/OAuth, Game Views adicionales, safe areas, ejecución del APK y dispositivo.
- El puente MCP se desconecta intermitentemente; un timeout no implica cancelación. Verificar XML/estado antes de reintentar. En esta entrega, el primer BuildPipeline detectó un CS0117 `Is.AnyOf` en un assembly de PlayMode obsoleto, aunque esa aserción ya no estaba en el archivo actual; forcé refresco/compilación, el test aislado terminó y el segundo build pasó. Un intento de prueba terminal anterior no inicializó; no se presenta como aprobado.
- Algunos tests modifican temporalmente PlayerPrefs y dependen de teardown; antes de ejecutarlos aislar o proteger progreso y rutas de evidencia.

## Incidente de evidencia

La suite de renderer escribió capturas en rutas históricas fijas durante esta auditoría; pudieron sobrescribirse imágenes no versionadas y no se dispone aquí de respaldo anterior verificable. El helper fue corregido para generar rutas únicas en futuras ejecuciones. La auditoría no debe declararse completa hasta resolver o aceptar explícitamente el solapamiento visual y completar la validación pendiente.
