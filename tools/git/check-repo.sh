#!/usr/bin/env bash
# Hay Chori y Paty — validación rápida de Git/estructura Unity (sin abrir Unity).
# Compatible con bash 3.2 (macOS), Git Bash (Windows) y Ubuntu (GitHub Actions).
# Solo necesita git + awk + grep + sed; python3 es opcional (JSON/TOML/YAML).
#
# Uso:
#   tools/git/check-repo.sh --staged          # lo que está en el índice (pre-commit)
#   tools/git/check-repo.sh --range BASE[..HEAD]  # cambios BASE...HEAD (pre-push / CI)
#   tools/git/check-repo.sh --tree [REV]      # árbol completo de REV (default HEAD)
# Variables: HCYP_WARN_MB (10), HCYP_MAX_MB (50).
# Salida: PASS/WARN/FAIL/SKIP por control; código 1 si hay algún FAIL. No modifica nada.

set -u
LC_ALL=C; export LC_ALL
GIT="git -c core.quotepath=off"

mode="tree"; base=""; head="HEAD"
case "${1:-}" in
  --staged) mode="staged" ;;
  --range)  mode="range"; spec="${2:-}"; [ -n "$spec" ] || { echo "uso: --range BASE[..HEAD]" >&2; exit 2; }
            case "$spec" in *...*) base="${spec%%...*}"; head="${spec##*...}" ;; *..*) base="${spec%%..*}"; head="${spec##*..}" ;; *) base="$spec" ;; esac
            [ -n "$head" ] || head="HEAD" ;;
  --tree|"") mode="tree"; head="${2:-HEAD}" ;;
  -h|--help) sed -n '2,11p' "$0"; exit 0 ;;
  *) echo "opción desconocida: $1 (ver --help)" >&2; exit 2 ;;
esac

root=$($GIT rev-parse --show-toplevel 2>/dev/null) || { echo "no es un repositorio git" >&2; exit 2; }
cd "$root" || exit 2
WARN_MB="${HCYP_WARN_MB:-10}"; MAX_MB="${HCYP_MAX_MB:-50}"
tmp=$(mktemp -d 2>/dev/null || mktemp -d -t hcyp) || exit 2
trap 'rm -rf "$tmp"' EXIT
fails=0; warns=0

pass() { printf 'PASS  %s\n' "$1"; }
warn() { printf 'WARN  %s\n' "$1"; warns=$((warns+1)); }
fail() { printf 'FAIL  %s\n' "$1"; fails=$((fails+1)); }
skip() { printf 'SKIP  %s\n' "$1"; }
detail() { sed 's/^/        /' "$1" | head -n "${2:-15}"; n=$(wc -l < "$1" | tr -d ' '); [ "$n" -gt "${2:-15}" ] && echo "        … ($n en total)"; return 0; }

# --- Árbol objetivo: "sha<TAB>path" (y modo) -------------------------------
if [ "$mode" = "staged" ]; then
  $GIT ls-files -s | awk '{sha=$2; mode=$1; sub(/^[^\t]*\t/,""); print mode"\t"sha"\t"$0}' > "$tmp/tree"
  label="índice (staged)"
else
  $GIT rev-parse --verify -q "$head^{commit}" >/dev/null || { echo "revisión inválida: $head" >&2; exit 2; }
  $GIT ls-tree -r "$head" | awk '{mode=$1; sha=$3; sub(/^[^\t]*\t/,""); print mode"\t"sha"\t"$0}' > "$tmp/tree"
  label="$head"
fi
cut -f3- "$tmp/tree" > "$tmp/paths"

# --- Cambios (status<TAB>oldsha<TAB>newsha<TAB>path) -------------------------
if [ "$mode" = "staged" ]; then
  $GIT diff --cached --raw --no-renames --no-abbrev > "$tmp/raw"
  diffargs="--cached"
elif [ "$mode" = "range" ]; then
  $GIT rev-parse --verify -q "$base^{commit}" >/dev/null || { echo "base inválida: $base (¿falta git fetch?)" >&2; exit 2; }
  mb=$($GIT merge-base "$base" "$head") || { echo "sin merge-base entre $base y $head" >&2; exit 2; }
  $GIT diff --raw --no-renames --no-abbrev "$mb" "$head" > "$tmp/raw"
  diffargs="$mb $head"
else
  # Modo árbol: todo el contenido cuenta como "agregado".
  awk -F'\t' '{print "A\t0000000000000000000000000000000000000000\t"$2"\t"$3}' "$tmp/tree" > "$tmp/changes"
  diffargs=""
fi
if [ "$mode" != "tree" ]; then
  awk '{split($0,a,"\t"); split(a[1],m," "); print m[5]"\t"m[3]"\t"m[4]"\t"a[2]}' "$tmp/raw" > "$tmp/changes"
fi
awk -F'\t' '$1!="D"{print $4}' "$tmp/changes" > "$tmp/added"   # agregados/modificados
nchanged=$(wc -l < "$tmp/changes" | tr -d ' ')
echo "Hay Chori y Paty · check-repo · modo=$mode · objetivo=$label · cambios=$nchanged"

# 1. Rutas prohibidas o sensibles ---------------------------------------------
grep -Ei '^(Library|Temp|Obj|Logs|Build|Builds|UserSettings|MemoryCaptures|Recordings)/|(^|/)(\.DS_Store|Thumbs\.db)$|\.(apk|aab|apks|ipa|obb|keystore|jks|p12|pfx|pem|key|csproj|sln|suo|userprefs)$|^\.mcp\.json$|^\.claude/settings\.local\.json$|^\.claude/worktrees/|^\.agents/locks/|^_transfer/|^Assets/InitTestScene[^/]*\.unity(\.meta)?$|^Assets/_Recovery(/|\.meta$)|(^|/)\.env(\.[^/]*)?$|(^|/)[^/]*(service[-_]?account|adminsdk)[^/]*\.json$|(^|/)(firebase|firestore|ui)-debug\.log$|^tools/firebase/node_modules/' "$tmp/added" > "$tmp/forbidden"
if [ -s "$tmp/forbidden" ]; then fail "rutas prohibidas/sensibles (generados, builds, credenciales, ajustes locales)"; detail "$tmp/forbidden"; else pass "sin rutas prohibidas ni sensibles"; fi
grep -E '\.log$|^Assets/Screenshots/' "$tmp/added" > "$tmp/softpaths"
if [ -s "$tmp/softpaths" ]; then
  if [ "$mode" = "tree" ]; then skip "logs/capturas ya versionados: $(wc -l < "$tmp/softpaths" | tr -d ' ') (revisar en la limpieza, no bloquea)"; else warn "logs o capturas nuevas: confirmar que son evidencia intencional"; detail "$tmp/softpaths" 8; fi
fi

# 2. Tamaño de archivos ----------------------------------------------------------
awk -F'\t' '$1!="D"{print $3}' "$tmp/changes" | $GIT cat-file --batch-check='%(objectname) %(objectsize)' > "$tmp/sizes" 2>/dev/null
awk -F'\t' '$1!="D"{print $3"\t"$4}' "$tmp/changes" > "$tmp/shapath"
awk -v W="$WARN_MB" -v M="$MAX_MB" 'NR==FNR{s[$1]=$2; next} {split($0,a,"\t"); sz=s[a[1]]+0; mb=sz/1048576;
  allow=(a[2] ~ /^Packages\/FirebaseSDK\/[^\/]*\.tgz$/);
  if (mb>=M && !allow) printf "FAILSZ %.1f MB %s\n", mb, a[2]; else if (mb>=W) printf "WARNSZ %.1f MB %s%s\n", mb, a[2], (allow?" (permitido: SDK Firebase)":"") }' "$tmp/sizes" "$tmp/shapath" > "$tmp/big"
grep '^FAILSZ' "$tmp/big" | sed 's/^FAILSZ //' > "$tmp/bigfail"; grep '^WARNSZ' "$tmp/big" | sed 's/^WARNSZ //' > "$tmp/bigwarn"
if [ -s "$tmp/bigfail" ]; then fail "archivos >= ${MAX_MB} MB (GitHub avisa a 50 MB y rechaza a 100 MB)"; detail "$tmp/bigfail"; else pass "ningún archivo nuevo/modificado >= ${MAX_MB} MB"; fi
if [ -s "$tmp/bigwarn" ]; then
  if [ "$mode" = "tree" ] && ! grep -qv 'permitido' "$tmp/bigwarn"; then skip "solo archivos grandes permitidos (SDK Firebase)"; else warn "archivos >= ${WARN_MB} MB: confirmar que son necesarios"; detail "$tmp/bigwarn"; fi
fi

# 3. Pares .meta en Assets/ (Unity ignora ocultos y nombres terminados en ~) ------
awk -F'\t' '{print $3}' "$tmp/tree" | awk '
  function hidden(p,  n,i,c){ n=split(p,c,"/"); for(i=1;i<=n;i++){ if(c[i] ~ /^\./ || c[i] ~ /~$/ || tolower(c[i])=="cvs" || c[i] ~ /\.tmp$/) return 1 } return 0 }
  /^Assets\// { p=$0; while (sub(/\/[^\/]*$/,"",p) && p!="Assets") if (!hidden(p)) dir[p]=1
    if (hidden($0)) next; have[$0]=1; if ($0 !~ /\.meta$/) asset[$0]=1; else meta[$0]=1 }
  END { for (a in asset) if (!((a".meta") in have)) print "falta .meta: " a;
        for (d in dir) if (!((d".meta") in have)) print "falta .meta de carpeta: " d;
        for (m in meta) { t=substr(m,1,length(m)-5); if (!(t in asset) && !(t in dir)) print ".meta huérfano: " m } }' | sort > "$tmp/meta"
if [ -s "$tmp/meta" ]; then fail "pares .meta incompletos en Assets/ (Unity regeneraría GUID y rompería referencias)"; detail "$tmp/meta"; else pass "cada asset/carpeta de Assets/ tiene su .meta y no hay .meta huérfanos"; fi

# 4. GUID duplicados o mal formados ---------------------------------------------
if [ "$mode" = "staged" ]; then $GIT grep --cached -e '^guid: ' -- 'Assets/*.meta' 'Packages/*.meta' > "$tmp/guids" 2>/dev/null
else $GIT grep -e '^guid: ' "$head" -- 'Assets/*.meta' 'Packages/*.meta' 2>/dev/null | sed "s|^$head:||" > "$tmp/guids"; fi
tr -d '\r' < "$tmp/guids" | awk '{i=index($0,":guid:"); g=substr($0,i+6); sub(/^ +/,"",g); sub(/ +$/,"",g); print substr($0,1,i-1)"\t"g}' > "$tmp/guidmap"
awk -F'\t' '{g=$2; if (length(g)!=32 || g !~ /^[0-9a-f]+$/) print "GUID inválido: " $1 " (" g ")"; c[g]++; f[g]=f[g] " " $1} END{for(g in c) if(c[g]>1) print "GUID duplicado " g ":" f[g]}' "$tmp/guidmap" | sort > "$tmp/guiddup"
if [ -s "$tmp/guiddup" ]; then fail "GUID duplicados o inválidos"; detail "$tmp/guiddup"; else pass "GUID únicos y válidos ($(wc -l < "$tmp/guidmap" | tr -d ' ') .meta)"; fi

# 5. GUID cambiados o eliminados que siguen referenciados (una sola pasada) -------
# Solo cuentan referencias en archivos serializados de Unity (YAML), no menciones en docs.
yaml_specs="Assets/*.unity Assets/*.prefab Assets/*.asset Assets/*.mat Assets/*.anim Assets/*.controller Assets/*.overrideController Assets/*.spriteatlas Assets/*.spriteatlasv2 Assets/*.preset Assets/*.playable Assets/*.mixer Assets/*.guiskin Assets/*.fontsettings Assets/*.renderTexture Assets/*.lighting Assets/*.asmdef Assets/*.asmref Assets/*.inputactions Assets/*.physicsMaterial2D Assets/*.terrainlayer ProjectSettings/*.asset"
: > "$tmp/guidchg"; : > "$tmp/delguids"
if [ "$mode" != "tree" ]; then
  awk -F'\t' '$4 ~ /\.meta$/ && ($1=="M" || $1=="D") {print $1"\t"$2"\t"$4}' "$tmp/changes" > "$tmp/metachg"
  awk -F'\t' '{print $2}' "$tmp/metachg" | $GIT cat-file --batch 2>/dev/null | tr -d '\r' | awk 'NF==3 && $2=="blob" && length($1)==40 {sha=$1; next} /^guid: /{print sha"\t"$2}' > "$tmp/oldguid"
  awk -F'\t' 'FNR==NR{og[$1]=$2; next} FILENAME==ARGV[2]{cur[$2]=$1; byp[$1]=$2; next}
    { g=og[$2]; if (g=="") next;
      if ($1=="M") { if (byp[$3]!=g) print "CHG\tGUID cambiado en " $3 ": " g " -> " (byp[$3]==""?"?":byp[$3]) }
      else if (!(g in cur)) print "DEL\t" g "\t" $3 }' "$tmp/oldguid" "$tmp/guidmap" "$tmp/metachg" > "$tmp/guidev"
  grep '^CHG' "$tmp/guidev" | cut -f2- > "$tmp/guidchg"
  grep '^DEL' "$tmp/guidev" | cut -f2,3 > "$tmp/delguids"
  if [ -s "$tmp/delguids" ]; then
    cut -f1 "$tmp/delguids" > "$tmp/delpat"
    # shellcheck disable=SC2086
    if [ "$mode" = "staged" ]; then $GIT grep --cached -o -F -f "$tmp/delpat" -- $yaml_specs 2>/dev/null > "$tmp/refs"
    else $GIT grep -o -F -f "$tmp/delpat" "$head" -- $yaml_specs 2>/dev/null | sed "s|^$head:||" > "$tmp/refs"; fi
    awk -F'\t' 'FNR==NR{m[$1]=$2; next} { n=split($0,a,":"); g=a[n]; f=a[1]; if (!((g SUBSEP f) in seen)) { seen[g SUBSEP f]=1; r[g]=r[g] " " f } }
      END{for (g in r) print "GUID eliminado (" m[g] ") aún referenciado en:" r[g]}' "$tmp/delguids" "$tmp/refs" >> "$tmp/guidchg"
  fi
fi
if [ "$mode" = "tree" ]; then skip "cambios de GUID (solo aplica a --staged/--range)"
elif [ -s "$tmp/guidchg" ]; then fail "GUID modificados o eliminados con referencias vivas"; detail "$tmp/guidchg"
else pass "ningún GUID existente cambió ni quedó referenciado tras borrarse"; fi

# 6. Colisiones de mayúsculas/minúsculas (Mac/Windows no las distinguen) ----------
awk -F'\t' '{print $3}' "$tmp/tree" | awk '{ p=$0; while (p!="") { l=tolower(p); if ((l in seen) && seen[l]!=p) col[seen[l]" <-> "p]=1; else seen[l]=p; if (!sub(/\/[^\/]*$/,"",p)) break } }
  END{for (c in col) print c}' | sort > "$tmp/case"
if [ -s "$tmp/case" ]; then fail "rutas que solo difieren en mayúsculas (rompen checkouts en Mac/Windows)"; detail "$tmp/case"; else pass "sin colisiones de mayúsculas/minúsculas"; fi

# 7. Secretos y marcadores de conflicto en líneas agregadas -----------------------
secret_re='-----BEGIN [A-Z ]*PRIVATE KEY-----|AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{20,}|xox[abprs]-[A-Za-z0-9-]{10,}|sk-ant-[A-Za-z0-9_-]{20,}|sk-(proj-)?[A-Za-z0-9]{32,}|"private_key" *:|AccountKey=[A-Za-z0-9+/=]{20,}'
if [ "$mode" = "tree" ]; then
  $GIT grep -nIE -e "$secret_re" "$head" -- . ':(exclude)*.png' ':(exclude)*.jpg' ':(exclude)*.tgz' ':(exclude)*.wav' ':(exclude)*.ttf' ':(exclude)*.otf' ':(exclude)*.psd' 2>/dev/null | sed "s|^$head:||" | cut -d: -f1,2 | sed 's/$/: (contenido oculto)/' > "$tmp/secrets"
  $GIT grep -nIE -e '^(<<<<<<<|>>>>>>>) ' "$head" -- . ':(exclude)*.png' ':(exclude)*.jpg' ':(exclude)*.tgz' ':(exclude)*.wav' ':(exclude)*.ttf' ':(exclude)*.otf' ':(exclude)*.psd' 2>/dev/null | sed "s|^$head:||" | cut -c1-120 > "$tmp/markers"
else
  # shellcheck disable=SC2086
  $GIT diff $diffargs -U0 --no-color --no-ext-diff | awk '/^\+\+\+ /{f=substr($0,7); next} /^\+/{print f": "substr($0,2)}' > "$tmp/addlines"
  grep -E -e "$secret_re" "$tmp/addlines" | sed 's/: .*$/: (contenido oculto)/' > "$tmp/secrets"
  grep -E ': (<<<<<<<|>>>>>>>) ' "$tmp/addlines" | cut -c1-120 > "$tmp/markers"
fi
if [ -s "$tmp/secrets" ]; then fail "posibles credenciales (claves privadas/tokens). Las API keys Firebase AIza… son públicas por diseño y no se marcan"; detail "$tmp/secrets"; else pass "sin patrones de credenciales"; fi
if [ -s "$tmp/markers" ]; then fail "marcadores de conflicto sin resolver"; detail "$tmp/markers"; else pass "sin marcadores de conflicto"; fi

# 8. Integridad de agentes/skills compartidos (fuente única Codex + Claude) --------
cat_t() { # contenido de un path en el objetivo
  if [ "$mode" = "staged" ]; then $GIT show ":$1" 2>/dev/null; else $GIT show "$head:$1" 2>/dev/null; fi; }
: > "$tmp/agents"
if grep -q '^AGENTS\.md$' "$tmp/paths"; then
  cat_t CLAUDE.md | grep -q '^@AGENTS\.md' || echo "CLAUDE.md no importa @AGENTS.md" >> "$tmp/agents"
  sl=$(awk -F'\t' '$3==".claude/skills"{print $1" "$2}' "$tmp/tree")
  case "$sl" in 120000*) [ "$($GIT cat-file -p "${sl#* }")" = "../.agents/skills" ] || echo ".claude/skills no apunta a ../.agents/skills" >> "$tmp/agents" ;;
    *) echo ".claude/skills no es un symlink versionado" >> "$tmp/agents" ;; esac
  grep -E '^\.claude/agents/[^/]+\.md$' "$tmp/paths" | while read -r a; do r=$(basename "$a" .md)
    grep -qx ".codex/agents/$r.toml" "$tmp/paths" || echo "$a sin .codex/agents/$r.toml"; cat_t "$a" | grep -q ".codex/agents/$r.toml" || echo "$a no remite a su TOML"; done >> "$tmp/agents"
  grep -E '^\.agents/skills/[^/]+/SKILL\.md$' "$tmp/paths" | while read -r s; do d=$(basename "$(dirname "$s")")
    cat_t "$s" | sed -n '1,5p' | grep -q "^name: $d\$" || echo "$s: frontmatter name distinto de '$d'"; done >> "$tmp/agents"
  if [ -s "$tmp/agents" ]; then fail "arquitectura compartida de agentes/skills"; detail "$tmp/agents"; else pass "CLAUDE.md→AGENTS.md, adaptadores→TOML y skills consistentes"; fi
else skip "AGENTS.md ausente en el objetivo"; fi

# 9. Sintaxis de configuraciones y scripts cambiados (un solo proceso Python) ---
py=""; for c in python3 python; do if command -v "$c" >/dev/null 2>&1 && "$c" -c 'import sys' >/dev/null 2>&1; then py="$c"; break; fi; done
grep -E '\.(json|toml|ya?ml|sh)$|^tools/git/hooks/[^/.]+$' "$tmp/added" > "$tmp/cfg"
mkdir "$tmp/cfgf"; : > "$tmp/cfgmap"; : > "$tmp/syntax"; i=0; nsh=0
while read -r f; do
  [ -n "$f" ] || continue; i=$((i+1))
  cat_t "$f" > "$tmp/cfgf/$i" 2>/dev/null || continue
  case "$f" in
    *.sh|tools/git/hooks/*) nsh=$((nsh+1)); bash -n "$tmp/cfgf/$i" 2>"$tmp/err" || echo "$f: $(head -n1 "$tmp/err")" >> "$tmp/syntax" ;;
    *) printf '%s\t%s\n' "$tmp/cfgf/$i" "$f" >> "$tmp/cfgmap" ;;
  esac
done < "$tmp/cfg"
nstruct=0; notrun=""
if [ -s "$tmp/cfgmap" ]; then
  if [ -n "$py" ]; then
    "$py" - "$tmp/cfgmap" >> "$tmp/syntax" 2>&1 <<'PY'
import json, sys
try: import tomllib
except ImportError: tomllib = None
try: import yaml
except ImportError: yaml = None
ok = 0; skipped = set()
for line in open(sys.argv[1], encoding="utf-8"):
    tmpf, name = line.rstrip("\n").split("\t", 1)
    try:
        if name.endswith(".json"): json.load(open(tmpf, encoding="utf-8-sig")); ok += 1
        elif name.endswith(".toml"):
            if tomllib is None: skipped.add("toml"); continue
            tomllib.load(open(tmpf, "rb")); ok += 1
        else:
            if yaml is None: skipped.add("yaml"); continue
            yaml.safe_load(open(tmpf, encoding="utf-8")); ok += 1
    except Exception as e:
        print(f"{name}: {str(e).splitlines()[0][:140]}")
print(f"#OK {ok}"); print("#SKIP " + " ".join(sorted(skipped)))
PY
    nstruct=$(sed -n 's/^#OK //p' "$tmp/syntax"); notrun=$(sed -n 's/^#SKIP //p' "$tmp/syntax")
    grep -v '^#' "$tmp/syntax" > "$tmp/syntax2"; mv "$tmp/syntax2" "$tmp/syntax"
    case " $notrun " in *" yaml "*) if command -v ruby >/dev/null 2>&1; then notrun=$(echo "$notrun" | sed 's/yaml//')
      awk -F'\t' '$2 ~ /\.ya?ml$/' "$tmp/cfgmap" | while IFS="$(printf '\t')" read -r t n; do ruby -ryaml -e 'YAML.load_file(ARGV[0])' "$t" 2>"$tmp/err" || echo "$n: $(head -n1 "$tmp/err")"; done >> "$tmp/syntax"
      nstruct=$((nstruct + $(awk -F'\t' '$2 ~ /\.ya?ml$/' "$tmp/cfgmap" | wc -l))); fi ;; esac
  else notrun="json/toml/yaml"; fi
fi
checked=$((nsh + ${nstruct:-0}))
if [ -s "$tmp/syntax" ]; then fail "sintaxis de configuración/scripts"; detail "$tmp/syntax"
elif [ "$checked" -gt 0 ]; then pass "sintaxis válida en $checked archivo(s) de configuración/script"; else skip "sin configuraciones/scripts cambiados"; fi
notrun=$(echo "$notrun" | tr -s ' ' | sed 's/^ //; s/ $//')
[ -n "$notrun" ] && skip "parser no disponible para: $notrun (lo valida CI)"

# 10. Sugerencia de validación proporcional (targeted-testing) -----------------
if [ "$mode" != "tree" ] && [ -s "$tmp/changes" ]; then
  awk -F'\t' '{print $4}' "$tmp/changes" | awk '
    /^Assets\/Scripts\/.*\.cs$/ {s["compilar Unity + EditMode focalizado del sistema tocado"]=1; next}
    /^Assets\/Tests\// {s["ejecutar solo los tests modificados (EditMode/PlayMode)"]=1; next}
    /\.(unity|prefab)$/ {s["escena/prefab: Console + PlayMode focalizado + captura Game View"]=1; next}
    /^Assets\/.*\.(png|jpg|psd|wav|ttf|mat|anim|controller)(\.meta)?$/ {s["arte/audio: import + captura visual del elemento"]=1; next}
    /^Assets\/.*\.meta$/ {s["solo .meta: revisar import settings (StreetArtTests si es textura)"]=1; next}
    /^(ProjectSettings|Packages)\// {s["ProjectSettings/Packages: compilar + smoke PlayMode; riesgo de build Android"]=1; next}
    /^firebase\/|^firebase\.json|^\.firebaserc/ {s["Firebase: revisar reglas; deploy solo con autorización"]=1; next}
    /^Assets\// {s["Assets: compilar y revisar Console"]=1; next}
    {s["docs/config/agentes: solo controles estáticos (este script)"]=1}
    END{for (k in s) print k}' | sort > "$tmp/suggest"
  echo "INFO  validación sugerida según rutas (ver .agents/skills/targeted-testing):"; sed 's/^/        - /' "$tmp/suggest"
fi

echo "Resultado: $fails FAIL, $warns WARN"
[ "$fails" -eq 0 ]
