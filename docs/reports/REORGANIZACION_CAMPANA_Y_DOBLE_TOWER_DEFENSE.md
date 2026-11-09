# Reorganización de campaña y doble Tower Defense

**Estado de trabajo:** implementación y validación en curso. Los estados `NOT_RUN` no acreditan comportamiento.

## Alcance y criterios obligatorios

- Orden anterior por ID: `0,1,2,3,4,11,5,6,7,8,9,10,12`.
- Orden solicitado por ID estable: `0,1,9,2,12,3,11,10,4,5,6,7,8` (13 posiciones).
- Camioneros (9) y Los Redondos (10) comparten el motor TD, con identidad visual separada. El usuario aclaró expresamente **30 hinchas en cada uno**, reemplazando el valor 10 del documento adjunto.
- Se conservan balance y modalidad de los demás niveles, lanzamiento, estaciones existentes, progresión persistente y datos históricos. No se autoriza reiniciar datos, publicar Firebase, generar APK ni hacer commit/push.

| Criterio obligatorio | Estado | Evidencia / límite |
|---|---|---|
| Orden, selector y transiciones de las 13 posiciones, sin renumerar IDs | `NOT_RUN` | Pendiente de implementación y pruebas. |
| SaveData y migración idempotente conservan desbloqueos, estadísticas, intentos, tickets y economía | `NOT_RUN` | Pendiente; no se tocarán datos reales. |
| Motor común: equivalencia jugable de ambos TD y aislamiento de la identidad de Los Redondos | `NOT_RUN` | Pendiente de implementación y pruebas. |
| Huracán mantiene cinco decisiones y duración y avanza a Los Redondos | `NOT_RUN` | Pendiente de prueba. |
| Entregas físicas y victoria/derrota de ambos TD; ola natural sin ajustar balance | `NOT_RUN` | La prueba previa con 30 hinchas solo cubre Camioneros, no valida el nuevo modo ni la reorganización. |
| Game View real de ambos TD y tres resoluciones verticales | `NOT_RUN` | Pendiente de capturas y revisión. |
| APK y dispositivo de esta revisión | `NOT_RUN` | No solicitados; el APK instalado precede a esta revisión. |

## Evidencia histórica relevante

La ola anterior de 10 hinchas a `fanSpeed=0.45` falló con 35/36 unidades y una fuga. Con el ajuste previo autorizado a `0.315`, la ejecución histórica de 10 pasó; posteriormente Camioneros con 30 pasó una ola natural (109/109 unidades, 30/30 atendidos, cero fugas). Ninguna de estas ejecuciones certifica Los Redondos ni el nuevo orden.

## Implementación y trazabilidad

1. **Resumen y orden:** orden anterior y nuevo figuran arriba; los 13 IDs estables se conservan. Hay diez modos tradicionales, dos TD y un Bonus Track.
2. **Archivos principales:** `Assets/Scripts/Core/StreetSimulation.cs`, `StreetGame.cs`, `StreetMarket.cs`, `StreetPlayerStatistics.cs`, `Assets/Scripts/Cloud/PlayerLocalRepository.cs`, `Assets/Scripts/TowerDefense/`, `Assets/Scripts/UI/StreetView.TowerDefense.cs`, `StreetView.cs`, `Assets/Editor/TowerDefenseAssetBuilder.cs`, configuraciones en `Assets/Resources/TowerDefense/`, tests EditMode/PlayMode y documentos SDD/tabla de balance. El checkout contiene otros cambios locales preexistentes que no deben atribuirse íntegramente a esta tarea.
3. **Motor:** `IsTowerDefenseLevel` identifica IDs 9 y 10; ambos usan `TowerDefenseGameMode`, presentación común y configuraciones separadas. La economía compartida se fija en `TowerDefenseEconomyProfile` para aislarla de cambios de balance tradicional.
4. **Configuración:** 30 hinchas, velocidad 0.315, Chori/Coca, $500 iniciales, cero trabajadores, primeras contrataciones $60, hasta tres de cada tipo por área, dos áreas, sin reloj/cuotas, entrega por lanzamiento; la identidad no se comparte.
5. **Arte ID10:** se reutilizan tema/mural, hinchas y escudo existentes de Los Redondos a través de `ClubVisualTheme` y el asset `LosRedondos-Tandil.asset`. La inspección visual de Unity no está aprobada hasta revisar capturas.
6. **Persistencia:** SaveData v7 registra desbloqueos por ID; conserva compatibilidad con el frontier y flags anteriores, separa estadísticas históricas tradicionales de ID10 y trata su resultado pendiente con el significado anterior. Firestore de producción y partidas reales no fueron modificados.
7. **Selector/progresión:** el selector usa las posiciones del orden de campaña; el sucesor se calcula por ese orden, mientras tickets, catálogo y estadísticas conservan identidad estable. La cobertura completa de interacciones queda pendiente.
8. **Compilación Unity:** tras corregir errores de asmdef/reflexión, la consola no mostró errores de compilación, aunque registró advertencias. Un test inicial de comparación de configuración falló por inspeccionar campos como propiedades; tras corregir el helper, pasó en la tanda identificada abajo.
9. **EditMode, evidencia directa:** job `0c12a935224949e6ba8a7b11bc37b426`, `PASS` 3/3: orden/IDs, resolución de victoria antigua ID10 y comparación de configuraciones visuales/jugables. Job `ca50d42170aa47ddb9782af5e40562f9`, `FAIL` 2/9: reflexión ambigua en Mercado y expectativa de desbloqueo legacy; investigación/corrección en curso. El estado del job completo no certifica otros casos.
10. **PlayMode:** `NOT_RUN` para esta integración hasta obtener resultados de Unity con ambos IDs. La victoria natural histórica de Camioneros no reemplaza esta comprobación.
11. **Capturas Game View:** `NOT_RUN` para 540×960, 1080×1920 y 720×1280 en ambos niveles hasta producir y examinar las seis imágenes reales.
12. **Regresión natural:** fallo previo 35/36 y posterior victoria de Camioneros con velocidad reducida constan en la sección histórica; queda pendiente comprobar ambos con economía y movimiento normales.
13. **Fallos y limitaciones:** los criterios de migración, transiciones completas, victoria/derrota, entrega, Bonus Track y visual no quedan aprobados por una inspección estática ni por tres tests puntuales. El puente MCP ha tenido desconexiones y un timeout intermitentes; no se atribuyen resultados sin job verificable.
14. **Build/dispositivo:** `NOT_RUN`; no se solicitó APK ni instalación en esta tarea. No se hizo commit ni push.
