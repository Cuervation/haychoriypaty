# Flujo Git y validación — Hay Chori y Paty

Guía única del flujo Git/GitHub para personas, Claude Code y Codex. Los **permisos** (qué puede hacer un agente sin pedir) viven en [AGENTS.md](AGENTS.md); la coordinación entre agentes en [.agents/README.md](.agents/README.md). Este archivo define el *cómo*. Auditoría y migración pendiente: [GIT_AUDIT_2026-11-17](docs/reports/GIT_AUDIT_2026-11-17.md).

## 1. Ramas (GitHub Flow simplificado)

| Rama | Uso |
|---|---|
| `main` | Única rama permanente: integrada y validada. Solo cambia por Pull Request. Protegida por el ruleset `main-protection`: sin borrado, sin force push, PR obligatorio con el check `Repo structure (no Unity)`. |
| `feature/<tema>` | Funcionalidad nueva |
| `fix/<tema>` | Corrección de error |
| `refactor/<tema>` | Reestructura sin cambio de comportamiento |
| `chore/<tema>` | Mantenimiento, optimización, herramientas, CI |
| `docs/<tema>` | Solo documentación, cuando necesite aislarse |

- Nombre en kebab-case y corto (`fix/lujan-access-anchor`). El agente no va en el nombre; va en el trailer `Co-Authored-By` del commit.
- **Crear rama** al empezar un objetivo publicable nuevo, siempre desde `main` actualizado.
- **Seguir en una rama existente** solo si es el mismo objetivo, no está mergeada y nadie más publica en ella.
- Una rama vive días, no semanas. Si `main` avanzó, actualizarla con merge de `main` (no rebase de ramas ya publicadas).

## 2. Commits (Conventional Commits)

Formato: `tipo(ámbito): resumen en imperativo` — tipos `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `ci`, `build`, `revert`. Ámbitos frecuentes: `hub`, `market`, `kitchen`, `td`, `lujan`, `renga`, `huracan`, `firebase`, `android`, `tests`, `agents`, `sdd`, `assets`.

```text
fix(lujan): keep a reachable access anchor when a new obstacle blocks it

Validation: EditMode LujanStations… 3/3 PASS; PlayMode NOT_RUN.
Co-Authored-By: …
```

- Un commit = una unidad coherente que compila (asset + su `.meta` siempre juntos). Cuerpo opcional con la evidencia de validación (`PASS/FAIL/BLOCKED/NOT_RUN`).
- Checkpoints en la rama de trabajo están bien (antes de una operación riesgosa, al terminar un subpaso validado, al cerrar la sesión); el squash los colapsa al integrar.
- Nunca `git add -A`/`git add .` en un checkout compartido: agregar rutas explícitas propias y revisar `git diff --cached --stat`.

## 3. Push, Pull Request y merge

- **Push** solo a ramas de trabajo: al cerrar una sesión, para respaldo o para correr CI. Nunca a `main`, nunca `--force` ni borrado de ramas sin aprobación explícita.
- **PR** cuando la unidad está lista para integrar (o como borrador para correr CI). Título = mensaje Conventional Commit final; cuerpo con la [plantilla](.github/pull_request_template.md).
- **Squash merge** por defecto (un commit por PR en `main`). **Merge commit** solo si la historia interna de la rama importa (p. ej. la integración inicial de `chore/fase3-codigo-prototipo`, 42 commits documentados). Sin rebase-merge.
- **Eliminar** la rama remota al mergear (ajuste de GitHub “Automatically delete head branches”). Una rama que se descarta sin mergear se conserva antes como tag `archive/<rama>`.

## 4. Versiones y tags

- Versión estable = commit de `main` con CI verde + validación Unity registrada en el SDD + `bundleVersion`/`versionCode` de Player Settings.
- Tag anotado `v<bundleVersion>-code<versionCode>` (p. ej. `v0.2.83-code85`) **solo** cuando una APK sale del entorno de desarrollo (dispositivo de prueba, testers, tienda), sobre el commit exacto de la build. La APK nunca se versiona; su SHA-256 va en el mensaje del tag y en `specs/features/android-prototype.md`.

## 5. Validación proporcional

| Nivel | Cuándo | Qué |
|---|---|---|
| **A. Rápida** (sin Unity, ~1–2 s local) | Todo commit/PR | `bash tools/git/check-repo.sh --staged` (o `--range origin/main...HEAD`): rutas prohibidas/credenciales, tamaño (aviso ≥10 MB, bloqueo ≥50 MB), pares `.meta`, GUID duplicados/cambiados/borrados con referencias, colisiones de mayúsculas, marcadores de conflicto, sintaxis JSON/TOML/YAML/shell, integridad agentes/skills. Imprime la validación Unity sugerida según rutas. CI la repite en cada PR ([workflow](.github/workflows/fast-checks.yml)). |
| **B. Unity** (Editor local existente) | Solo si cambian `Assets/`, `Packages/`, `ProjectSettings/` | Según [targeted-testing](.agents/skills/targeted-testing/SKILL.md): compilación y Console, EditMode/PlayMode **focalizados** en el sistema tocado, captura Game View si cambia lo visual. Lock de Unity según `.agents/README.md`. No corre en GitHub Actions (requiere licencia Unity e infraestructura: decisión pendiente). |
| **C. Versión candidata** | Antes de una APK fuera de desarrollo, con pedido explícito | Regresión pertinente, build Android, tamaño de APK frente a la anterior, Console de build, verificación visual, prueba en dispositivo y progreso/saves preservados (nunca limpiar). Luego tag. |

Hooks opcionales (no se instalan solos; cada clon decide): `git config core.hooksPath tools/git/hooks`. `pre-commit` corre el nivel A sobre lo staged y bloquea commits en `main`; `pre-push` valida solo los commits nuevos y bloquea borrado de ramas, force push y push a `main` (excepción consciente: `HCYP_ALLOW_DANGEROUS_PUSH=1` / `HCYP_ALLOW_MAIN_PUSH=1`). Protegen igual a humanos, Claude y Codex porque actúan sobre `git`.

## 6. Unity y archivos grandes

- Serialización Force Text; `.gitattributes` fija LF en YAML/código y marca binarios. Mac y Windows producen los mismos bytes.
- Mover/renombrar assets desde el Editor (o mover asset + `.meta` juntos). Nunca borrar o regenerar un `.meta` a mano: cambia el GUID y rompe referencias.
- Escenas `.unity`, prefabs y `ProjectSettings/`: un solo dueño por vez; los conflictos se resuelven a mano (o con UnityYAMLMerge configurado localmente), nunca “tomar ambos”.
- No versionar `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, APK/AAB, keystores, `.mcp.json`, `_transfer/`, `InitTestScene*` ni `_Recovery/` (ver `.gitignore`). Capturas de evidencia en `docs/evidence/` o en `Logs/` ignorado, no en `Assets/`.
- **Git LFS: no por ahora.** El repo pesa ~0,6 GB de historia (PNG 442 MB, SDK Firebase 77 MB) y los binarios casi no se reescriben (versiones nuevas con sufijo `-vN`). La cuota gratuita de LFS (1 GiB almacenamiento/ancho de banda mensual) se agotaría con pocos clones y migrar exige reescribir historia y forzar re-clones en Mac y Windows. Reevaluar si un binario >50 MB empieza a cambiar seguido o la historia supera ~2 GB.

## 7. Revertir y recuperar

- Deshacer algo ya integrado: rama `revert/<tema>` + `git revert <sha>` (un solo commit si fue squash) + PR. Nunca `reset` en ramas publicadas. `revert` no toca cambios locales sin commit de nadie.
- Archivo equivocado publicado en una rama de trabajo: commit que lo quita (`git rm --cached` + regla en `.gitignore`). Si era una credencial, **rotarla primero**: borrarla de la historia requiere reescritura y aprobación explícita.
- Commit publicado en la rama equivocada: llevarlo a la correcta (`cherry-pick` con aprobación) y revertirlo en la equivocada.
- Force push accidental: el SHA anterior está en el reflog local (`git reflog show origin/<rama>`) o en la actividad de la rama en GitHub; restaurarlo es otro force push y requiere aprobación.
