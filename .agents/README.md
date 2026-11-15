# Agentes compartidos: Codex + Claude Code

Arquitectura para que Codex y Claude Code trabajen con el mismo SDD, los mismos roles y las mismas skills. Las reglas viven solo en [AGENTS.md](../AGENTS.md); este archivo describe **dónde está cada pieza y cómo coordinarse**, sin repetir reglas.

## Fuente única por pieza

| Pieza | Fuente de verdad (editar acá) | Codex la lee desde | Claude Code la lee desde |
|---|---|---|---|
| Reglas operativas y Git | `AGENTS.md` | `AGENTS.md` | `CLAUDE.md` → `@AGENTS.md` (import, sin copia) |
| SDD / contratos | `specs/` (índice `specs/README.md`) | igual | igual |
| Roles `architecture`, `implementation`, `validation` | `developer_instructions` en `.codex/agents/<rol>.toml` | el TOML | `.claude/agents/<rol>.md`: adaptador que ordena leer el TOML |
| Modelo/esfuerzo por rol | TOML (Codex) | TOML | frontmatter: `model: inherit` y `effort` igual al TOML |
| Skills | `.agents/skills/<skill>/SKILL.md` | `.agents/skills/` | `.claude/skills` → symlink a `../.agents/skills` |
| Límite de subagentes | `.codex/config.toml` (2 + principal) | config | `CLAUDE.md` lo recuerda; misma regla en `AGENTS.md` |
| Guardas de permisos | — | aprobaciones propias de Codex | `.claude/settings.json` (`ask` para ramas, reset/clean/stash, worktrees, `gh`, deploy Firebase) |
| Unity MCP | servidor ya abierto (`127.0.0.1:8080`) | config MCP de Codex | `.mcp.json` local (sin versionar) |

Reglas de mantenimiento:
- Cambiar una regla → `AGENTS.md`. Cambiar un rol → su TOML (el adaptador Claude no se toca salvo que cambie el nombre o el esfuerzo del rol). Nueva skill → `.agents/skills/<nombre>/SKILL.md`; Claude la ve por el symlink.
- No agregar instrucciones de proyecto en `CLAUDE.md`, en los adaptadores ni en archivos por herramienta. Si una herramienta necesita algo propio, limitarlo a “dónde está” o a su sintaxis.

## Misma carpeta, en paralelo (modo actual, sin worktrees)

Codex y Claude comparten `/Users/celestino/HayChoriYPaty` y la misma rama. Funciona solo con propiedad exclusiva de archivos y un único dueño de Unity a la vez.

1. **Asignación.** El usuario indica en cada pedido qué archivos o carpetas toca cada agente. Sin asignación explícita, un agente no edita archivos que aparezcan modificados en `git status`/`git diff` y que no haya cambiado él en su sesión.
2. **Al empezar y antes de cada tanda de ediciones:** `git status --short`. Si un archivo propio cambió desde fuera, parar y avisar; no sobrescribir.
3. **Archivos de alta colisión** (`specs/current-state.md`, `specs/status.md`, `specs/decisions.md`, `docs/BALANCE_ACTUAL_NIVELES.md`, `AGENTS.md`, escenas `.unity`, prefabs, `ProjectSettings/`): un solo agente por vez, por asignación del usuario. En los historiales, solo agregar entradas fechadas; nunca reescribir las ajenas.
4. **Unity: un solo dueño a la vez.** Las lecturas sin efecto (Console, `editor_state`, búsquedas de objetos, leer archivos) son libres. Requieren el lock de abajo: tests EditMode/PlayMode, entrar a Play Mode, guardar o modificar escenas/prefabs/assets por MCP, refresh/reimport, ítems de menú con efectos, builds, y cualquier acción que escriba saves. Mientras otro tenga el lock, no escribir en `Assets/`, `Packages/` ni `ProjectSettings/` (provocaría recompilación o reimport en medio de su ejecución).
5. **Saves compartidos.** El Editor guarda progreso en PlayerPrefs/`persistentDataPath` (`DefaultCompany/Hay Chori y Paty`), común a todo checkout y worktree de esta Mac. Nunca limpiar progreso; restaurar cualquier estado temporal de QA al terminar.
6. **Commits, push e integración:** solo el usuario, según `AGENTS.md`.

### Lock de Unity

Archivo ignorado por Git: `.agents/locks/unity.lock`. Crear de forma atómica (falla si ya existe):

```bash
mkdir -p .agents/locks && ( set -o noclobber; printf 'owner=%s\ntask=%s\nsince=%s\n' "claude" "tests PlayMode Lujan" "$(date -u +%FT%TZ)" > .agents/locks/unity.lock )
```

- Si falla: leer el lock, no ejecutar la acción y reportarla `BLOCKED` (con dueño y hora). No borrar un lock ajeno aunque parezca viejo: preguntar al usuario.
- Al terminar (también tras error o timeout), revisar Console/diálogos según `AGENTS.md` y liberar solo el propio:

```bash
grep -q '^owner=claude$' .agents/locks/unity.lock && rm .agents/locks/unity.lock
```

(Codex usa `owner=codex`.)

## Worktrees (futuro; no habilitado)

No crear worktrees hasta que el usuario lo decida. Cuando lo haga, el objetivo es que cada agente tenga su propia carpeta y rama para editar los mismos archivos sin pisarse; la integración sigue siendo del usuario.

**Creación (la hace o autoriza el usuario):**

```bash
git worktree add ../HayChoriYPaty-codex -b codex/<tarea>
```

```bash
git worktree add ../HayChoriYPaty-claude -b claude/<tarea>
```

- Usar carpetas hermanas fuera del repo (no `.claude/worktrees/` ni dentro de `Assets/`), para que Unity, búsquedas y `git status` del checkout principal no las vean.
- Cada worktree tiene su propia copia de `AGENTS.md`, `specs/`, `.codex/` y `.agents/` en su rama: las reglas siguen siendo una sola fuente por rama. `.claude/skills` es un symlink relativo y funciona igual.
- Copiar a mano lo no versionado que haga falta, por ejemplo `.mcp.json`. No copiar `UserSettings/`, `Library/` ni credenciales salvo decisión del usuario.

**Unity por worktree:**
- Cada worktree es otro proyecto Unity: necesita su propio Editor (misma versión de `ProjectVersion.txt`) y su propia `Library/` (la primera importación es larga). Nunca abrir la misma carpeta en dos Editores.
- Unity MCP admite varias instancias en el mismo servidor: listar `mcpforunity://instances` y fijar la instancia correcta (`set_active_instance` con `Nombre@hash`) al inicio de cada sesión; verificar `projectRoot` contra la carpeta del worktree.
- Los saves siguen siendo compartidos (punto 5): no correr Play Mode que escriba saves en dos worktrees a la vez; el lock de Unity pasa a ser por carpeta, pero coordinar igual las pruebas que toquen progreso.
- Las builds/APK siguen requiriendo pedido explícito y se generan solo en el worktree indicado.

**Abrir los agentes:** apuntar la sesión de Claude Code o Codex a la carpeta del worktree. No usar la creación automática de worktrees de cada herramienta (por ejemplo `EnterWorktree` en Claude, que `.claude/settings.json` deja en `ask`) salvo que el usuario lo pida.

**SDD con ramas paralelas:**
- Un contrato de feature pertenece a una sola rama a la vez.
- `current-state.md`, `status.md` y `decisions.md` generan conflictos: cada rama agrega solo entradas fechadas al final de su sección, o deja la actualización para la integración.
- Antes de integrar, cada agente informa rama, archivos cambiados, pruebas (`PASS`/`FAIL`/`BLOCKED`/`NOT_RUN`) y pendientes. El merge/rebase y la limpieza (`git worktree remove`) los hace el usuario.
