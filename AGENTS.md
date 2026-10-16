# Hay Chori y Paty — reglas operativas

## Alcance y fuentes
- Trabajar en el checkout Unity activo; confirmar raíz Git y `projectRoot` de MCP si difieren. El checkpoint y los límites pendientes están en [specs/continuity.md](specs/continuity.md).
- [specs/README.md](specs/README.md) es el índice: leer solo el contrato de la tarea. `status.md` y `decisions.md` son historiales, no reglas completas vigentes. Ante contradicción, contrastar el contrato más reciente con código y valores serializados; informar lo irresoluble, no inventar gameplay ni adaptar código a reglas obsoletas.
- Hacer el cambio mínimo autorizado. No implementar pendientes ni ampliar alcance; preservar trabajo local ajeno. Cambiar comportamiento exige actualizar su contrato; un retoque cosmético sin impacto contractual no exige reescribir SDD.

## Trabajo proporcional
- SIMPLE: principal solo, sin plan extenso, subagentes ni suites globales. MEDIA: principal y delegación opcional con beneficio concreto. COMPLEJA: delegar únicamente tareas independientes; ver [token-optimization](.agents/skills/token-optimization/SKILL.md).
- Conservar tres roles de `.codex/agents/`: `architecture`, `implementation`, `validation`. Usarlos solo cuando aporten; asignar archivos/responsabilidad exclusivos y advertir que hay otros trabajando. Límite configurado: dos subagentes simultáneos, además del principal.
- Conservar modelos/esfuerzos de los TOML salvo decisión justificada y autorizada. No cambiar silenciosamente modelo/esfuerzo del chat principal. No instalar paquetes, servicios o dependencias ni modificar configuración global sin autorización específica; no introducir rutas API pagas.
- Activar solo skills relevantes: `sdd-workflow` para contratos, `unity-2d` para Unity, `targeted-testing` para comprobaciones, `token-optimization` para dimensionar trabajo. Buscar antes de leer y reutilizar evidencia; evitar barridos/handoffs/pruebas repetidos.

## Unity y producto
- Usar la versión de `ProjectSettings/ProjectVersion.txt`. Preferir el editor ya abierto mediante Unity MCP: consultas de solo lectura primero, seleccionar el checkout correcto entre varias instancias y comprobar resultados. No duplicar endpoint, editor ni servidor; no cerrar/reiniciar procesos sin autorización.
- Preservar assets, GUIDs y todos los `.meta`; no versionar `Library`, `Temp`, `Logs`, `Obj`, builds o ajustes de usuario. Sin dependencias nuevas por conveniencia.
- Mantener siete productos y puesto callejero argentino sin mesas para sentarse: [product.md](specs/product.md). No modificar economía, objetivos, tiempos, inventario ni pendientes fuera del pedido.
- Consultar [visual-reference.md](specs/visual-reference.md) al cambiar arte/UI. Reutilizar arte original y `StreetView.DrawStandardButton` para botones comunes; cards de mejoras y selector mural son excepciones existentes. La geometría activa es `StreetKitchenLayout`/cocina modular, no los tamaños históricos de `StreetWorkstationLayout`.
- Pruebas proporcionales según `targeted-testing`: estáticas para docs/config; Unity solo para sistemas afectados. Distinguir aprobado, fallido y no ejecutado. APK e instalación únicamente con pedido/autorización; nunca limpiar progreso para validar.
