#!/usr/bin/env python3
"""Aggregate measured Unity NDJSON. Standard library only; never changes game inputs."""
import json, csv, statistics as st, math, collections as co, pathlib, hashlib, argparse
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Audits/GameplayBalance-20261020'
NAMES=['Chori','Paty','Bondiola','Vacío','Coca','Fernet','Cerveza']
ROLES=['Parrillero','Cocacolero','Premium','Fernetero','Mozo']
def rows(name):
 p=OUT/(name+'.jsonl')
 return [json.loads(l) for l in p.read_text().splitlines() if l.strip()] if p.exists() else []
def avg(a): return st.mean(a) if a else 0
def f(n): return f'{n:.1f}'
def cash(n): return f'${n/100:.2f}'
def wilson(w,n):
 if not n:return '—'
 z=1.96;p=w/n;d=1+z*z/n;c=(p+z*z/(2*n))/d;a=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/d
 return f'{100*(c-a):.1f}–{100*(c+a):.1f}%'
def fraction(r):return avg([min(1,d/g) for d,g in zip(r['delivered'],r['goals']) if g])
def ranked(groups):
 return sorted(groups,key=lambda k:(-sum(r['won'] for r in groups[k])/len(groups[k]),-avg([fraction(r) for r in groups[k]]),avg([r['elapsed'] for r in groups[k]])))
def teamstr(team):return '/'.join(str(round(x)) for x in team)
def table(headers,rs):return '\n'.join(['| '+' | '.join(headers)+' |','|'+'|'.join(['---']*len(headers))+'|']+['| '+' | '.join(map(str,r))+' |' for r in rs])+'\n'
def writecsv(name, headers, data):
 with (OUT/name).open('w') as f:
  w=csv.writer(f);w.writerow(headers);w.writerows(data)
def main():
 levels=json.loads((OUT/'levels.json').read_text())['items']; finals=rows('finalists'); explore=rows('explore'); cap=rows('capacity'); camps=rows('campaign'); cruns=rows('campaign-runs'); alt=rows('alternatives')
 chosen={}; grouped={}
 for l in range(1,12):
  g=co.defaultdict(list)
  for r in finals:
   if r['level']==l:g[r['strategy']].append(r)
  if not g: raise ValueError(f'Finalists absent for level{l}')
  chosen[l]=ranked(g)[0];grouped[l]=g
 summary=[];rec=[];bottles=[];details=[];timings=[];prods=[]
 for level in levels:
  l=level['level'];name=chosen[l];rs=grouped[l][name];wins=[r for r in rs if r['won']];loss=[r for r in rs if not r['won']];w=len(wins);n=len(rs)
  margin=avg([r['remaining'] for r in wins]);rate=w/n
  if not w:difficulty='Sin victoria encontrada; imposibilidad no probada'
  elif rate<.2:difficulty='Extremadamente exigente'
  elif rate<.8:difficulty='Extremadamente exigente/inconsistente'
  elif margin<35:difficulty='Exigente'
  elif margin<60:difficulty='Normal'
  else:difficulty='Fácil para esta política'
  summary.append([l,level['club'],f(level['duration']),level['totalGoal'],f(min([r['elapsed'] for r in wins])) if wins else '—',f'{w}/{n} ({rate*100:.1f}%)',difficulty,'Ganable demostrado' if w else 'No demostrado'])
  mean_team=[st.median([r['team'][i] for r in wins or rs]) for i in range(5)];mean_speed=st.median([r['speed'] for r in wins or rs])
  rec.append([l,round(mean_team[0]),round(mean_team[2]),round(mean_team[1]),round(mean_team[3]),round(mean_team[4]),f(mean_speed)])
  deficit=[avg([max(0,r['goals'][p]-r['delivered'][p]) for r in loss or rs]) for p in range(7)]
  slow=max(range(7),key=lambda p:deficit[p]);mozo_idle=avg([r['emptyFoodIdle'][4] for r in rs]);full=avg([sum(r['tableFullSeconds']) for r in rs])
  bottles.append([l,NAMES[slow]+' pendiente' if deficit[slow]>0 else 'Margen/estrategia de inversión',f'{NAMES[slow]} faltante medio={f(deficit[slow])}; Mozo sin comida={f(mozo_idle)} trabajador-s; mesas llenas={f(full)} producto-s','Ver ensayos en memoria; no aplicado'])
  allwins=[r for r in explore+finals if r['level']==l and r['won']]
  minimum=min(allwins,key=lambda r:(sum(r['team']),r['spent'])) if allwins else None
  representative=min(wins or rs,key=lambda r:abs(r['elapsed']-avg([x['elapsed'] for x in wins or rs])))
  details += [f'## Nivel {l}: {level["club"]}',f'Política recomendada **{name}**; resultados sobre las mismas semillas reservadas 1001–1030. Stock mínimo aislado es una precondición, no una concesión obtenida en campaña. Roles: {", ".join(level["roles"])}.',f'Victorias **{w}/{n}**, IC95% Wilson **{wilson(w,n)}**. Tiempo victoria promedio **{f(avg([r["elapsed"] for r in wins])) if wins else "—"} s**; mejor/peor **{f(min([r["elapsed"] for r in wins])) if wins else "—"}/{f(max([r["elapsed"] for r in wins])) if wins else "—"} s**; sobrante promedio **{f(margin) if wins else "—"} s**.',f'Plantel mínimo **observado** (P/C/Premium/F/M): **{teamstr(minimum["team"]) if minimum else "sin victoria"}**, velocidad {minimum["speed"] if minimum else "—"}; no es mínimo matemático. Plantel recomendado tabla2 es la mediana redondeada de finales ganadores (no contratación inicial).',f'Monedas por partida: generadas **{f(avg([r["earned"] for r in rs]))}**, gastadas **{f(avg([r["spent"] for r in rs]))}**, restantes **{f(avg([r["cash"] for r in rs]))}**. Primer contrato medio **{f(avg([next((b["second"] for b in r["purchases"] if b["action"]!="speed"),r["elapsed"]) for r in rs]))} s**; número de ventas antes depende de la semilla.', '### Comparación de finalistas',table(['Política','N','Victoria','IC95%','Tiempo ganador medio','Margen medio'],[[k,len(g),f'{sum(r["won"] for r in g)}/{len(g)}',wilson(sum(r['won'] for r in g),len(g)),f(avg([r['elapsed'] for r in g if r['won']])) if any(r['won'] for r in g) else '—',f(avg([r['remaining'] for r in g if r['won']])) if any(r['won'] for r in g) else '—'] for k,g in grouped[l].items()])]
  pt=[]
  for p,g in enumerate(level['goals']):
   if not g:continue
   deposited=avg([r['deposited'][p]*60/r['elapsed'] for r in rs]);produced=avg([r['produced'][p]*60/r['elapsed'] for r in rs]);sales=avg([r['delivered'][p]*60/r['elapsed'] for r in rs]);taken=avg([r['picked'][p]*60/r['elapsed'] for r in rs]);missing=avg([max(0,r['goals'][p]-r['delivered'][p]) for r in rs])
   pt.append([NAMES[p],g,f(g*60/level['duration']),f(sales),f(missing),'Cumplido en todas' if all(r['delivered'][p]>=g for r in rs) else 'Variable/faltante'])
   prods.append([l,NAMES[p],g,g*60/level['duration'],produced,deposited,taken,sales,missing,avg([r['initialTable'][p] for r in rs])])
  details+=['### Tabla 3 — Productos y producción',table(['Producto','Objetivo','Ventas/min necesarias','Ventas/min reales','Faltantes medios','Estado'],pt),'Las tasas reales se calculan por tiempo efectivamente jugado, no por la duración máxima. Producción nueva, depósito y retiro por separado en `production.csv`; las mesas de niveles1–5 empiezan con stock ya cocido, excluido de producción nueva.','### Cronología de compras (partida real de referencia, semilla '+str(representative['seed'])+')',table(['Segundo','Compra','Costo','Saldo posterior'],[[f(x['second']),x['action'],x['cost'],x['cashAfter']] for x in representative['purchases']]),'Una referencia no es un guion fijo para toda semilla. `purchase-timing.csv` contiene la mediana y rango observado de cada compra ordinal; los JSON guardan cada segundo exacto.','### Tiempos de trabajadores (media trabajador-segundos, no segundos de reloj)']
  states=['Idle','ToStation','Pickup','ToCounter','Handoff','ToGrill','Cooking','ToTable','Deposit','ToCookQueue']
  actualstates=json.loads((OUT/'runtime-states.json').read_text())['states']
  statrows=[]
  for role in range(5):
   if not any(r['team'][role] for r in rs):continue
   statevals=[avg([r['roleStates'][role*12+i] for r in rs]) for i in range(len(actualstates))]
   statrows.append([ROLES[role]]+[f(v) for v in statevals]+[f(avg([r['roleVisual'][role] for r in rs]))])
  details+=[table(['Rol']+actualstates+['Visual(simulada)'],statrows),'El estado Idle puede incluir retorno al punto de espera; visual es una etiqueta concurrente, no tiempo adicional. Muestreo a100ms, integración del motor en pasos de hasta50ms. Saturación observada no demuestra por sí sola ventas causalmente perdidas.']
  purchases=co.defaultdict(list)
  for r in wins or rs:
   counts=co.Counter()
   for b in r['purchases']:
    counts[b['action']]+=1;purchases[(b['action'],counts[b['action']])].append(b['second'])
  for (a,k),times in purchases.items():timings.append([l,name,a,k,len(times),st.median(times),min(times),max(times)])
 # CSVs raw flattened / full records remain NDJSON
 writecsv('production.csv',['level','product','goal','required_per_min','new_cooked_per_min','deposited_per_min','picked_per_min','sold_per_min','mean_missing','initial_cooked_stock'],prods)
 writecsv('purchase-timing.csv',['level','strategy','action','ordinal','observed_n','median_second','min_second','max_second'],timings)
 writecsv('runs.csv',['mode','variant','level','seed','strategy','won','elapsed','remaining','earned','spent','cash','speed','team','delivered','produced','deposited','picked'],[[('NOT_SAMPLED' if k in ['produced','deposited','picked'] and not r.get('detailedMetrics',True) else r.get(k,'')) if k not in ['team','delivered','produced','deposited','picked'] else json.dumps(r[k]) for k in ['mode','variant','level','seed','strategy','won','elapsed','remaining','earned','spent','cash','speed','team','delivered','produced','deposited','picked']] for r in explore+finals+cap+cruns+alt])
 complete=sum(c['completed']==11 for c in camps);failed=co.Counter(c['outcome'] for c in camps if c['completed']<11)
 campaignrows=[]
 for l in range(1,12):
  ts=[t for c in camps for t in c['levels'] if t['level']==l]
  if ts:campaignrows.append([l,len(ts),sum(t['result']=='Won' for t in ts),cash(avg([t['marketBefore'] for t in ts])),cash(avg([t['restock'] for t in ts])),cash(avg([t['reward'] for t in ts])),cash(avg([t['progression'] for t in ts])),cash(avg([t['marketAfter'] for t in ts]))])
 altrows=[]
 for l in range(1,12):
  for v in sorted(set(r['variant'] for r in alt)):
   rs=[r for r in alt if r['level']==l and r['variant']==v]
   if rs:altrows.append([l,v,len(rs),f'{sum(r["won"] for r in rs)}/{len(rs)}',f(avg([r['remaining'] for r in rs if r['won']])) if any(r['won'] for r in rs) else '—',f(avg([fraction(r)*100 for r in rs]))])
 # Capacity details and measured per-role cycles
 caprows=[]
 for l in range(1,12):
  rs=[r for r in cap if r['level']==l];best=max(rs,key=lambda r:(r['won'],fraction(r),-r['elapsed'])) if rs else None
  if best:caprows.append([l,len(rs),best['strategy'],best['phase'],f(best['elapsed']),str(best['delivered'])])
 baseline=(OUT/'source-fingerprint.txt').read_text().strip()
 runtime_files=sorted(list((ROOT/'Assets/Scripts').rglob('*.cs'))+[ROOT/'Assets/Scenes/Main.unity'],key=lambda p:p.relative_to(ROOT).as_posix())
 current=hashlib.sha256('\n'.join(p.relative_to(ROOT).as_posix()+':'+hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime_files).encode()).hexdigest()
 if baseline!=current:raise ValueError('Runtime source/scene fingerprint changed!')
 manifest={'source_fingerprint':baseline,'current_fingerprint':current,'runtime_unchanged':True,'unity':'6000.6.3f1','configuration_sha256':(OUT/'configuration.sha256').read_text(),'runs':{k:len(v) for k,v in [('explore',explore),('finalists',finals),('capacity',cap),('campaigns',camps),('campaign_levels',cruns),('alternatives',alt)]},'best_strategy_by_level':chosen}
 (OUT/'summary.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
 report=['# Auditoría integral de jugabilidad, balance y progresión — 20/10/2026','## Dictamen y alcance',f'Configuración efectiva de **Main.unity**, Unity **6000.6.3f1**, versión0.2.54/code56, fuentes identificadas por SHA-256 `{baseline}`. Verificación final: las fuentes productivas y la escena **no cambiaron**. **No se aplicaron ajustes de balance.**',f'Se ejecutaron **{len(explore)}** exploraciones, **{len(finals)}** partidas estadísticas finalistas, **{len(cap)}** pruebas preconfiguradas de capacidad, **{len(camps)}** campañas y **{len(alt)}** alternativas en clones en memoria. La observación detallada de estados/producción se conserva en finalistas, campañas y alternativas; se omite en exploraciones/capacidad marcadas detailedMetrics=false para acelerar (sus ceros en esos campos no son mediciones). Partidas individuales de campaña adicionales: **{len(cruns)}**.',f'Campañas completas: **{complete}/{len(camps)} ({100*complete/len(camps):.1f}%)**.' if camps else 'Campañas no ejecutadas.',f'Bloqueos/derrotas de campaña: `{dict(failed)}`.','**No victoria encontrada no equivale a imposibilidad matemática**: la búsqueda es finita y el juego no tiene un límite fijo de contratación. Las categorías de dificultad se condicionan a la política automática medida, no a una garantía para cualquier jugador humano.','## Método y seguridad','1. `LoadEffective()` abre Main como preview, lee el balance deserializado por Unity y lo clona. Se exportan valores efectivos (no la antigua tabla aggregate `levelGoals`) y constantes del motor. No se modifica ni se guarda esa escena.','2. El motor real integra en pasos máximos50ms. Se avanza sin renderizado ni espera real, muestreo100ms. Órdenes aleatorias propias del motor; no se inyectan clientes ni ventas. El juego termina exclusivamente por su condición real: cuotas individuales o reloj.','3. **Nivel aislado condicional:** ingredientes mínimos legales como precondición de entrada, sin inyectar monedas de partido. Esto prueba jugabilidad una vez abastecido, NO que esos ingredientes sean financiables desde una campaña fresca. **Capacidad A:** planteles/velocidad preconfigurados e ingredientes ilimitados en un fixture separado; no se confunden con victorias legales.','4. **Campaña B:** perfil nuevo, API StreetGame y Mercado cash-only, saldo real, premios reales, compras de todos los faltantes asequibles, stock transferido, nueva plantilla/velocidad en cada nivel y JSON de guardado recargado. Primera derrota o falta de fondos detiene esa campaña. Nunca se escribe PlayerPrefs/Firebase ni se limpia el teléfono.','5. Exploración de32políticas con semilla guía101 (y202/303 en niveles iniciales); incluye velocidad primero, plantel primero, objetivos, atrasos, ingresos, roles faltantes, mozos anticipados, límites1–6 y todos los niveles de velocidad0–9. Dos candidatas por nivel se evalúan con30semillas comunes1001–1030; la selección basada en exploración no demuestra óptimo global.','6. El margen ideal20–30s es una referencia de diseño, no una condición implementada. Política reevalúa cada0.5s y puede comprar varias mejoras legales simultáneamente; una persona puede ser menos precisa.','7. Tasas son unidades reales; stock inicial separado. Tiempos de estado y esperas se miden como exposición, NO como ventas causalmente perdidas. No hay evasión dinámica entre empleados ni barra/pérdida por paciencia en esta versión.','## Configuración y diferencias entre etapas',table(['Nivel','Duración','Metas [Ch/Pa/Bo/Va/Co/Fe/Ce]','Monedas iniciales','Sistema','Costos base [P/C/Prem/F/M]','Costos9 velocidades'],[[x['level'],f(x['duration']),x['goals'],x['initialCoins'],'Activo + Mozo' if x['active'] else 'Auto + entrega por rol',x['firstHireCosts'],x['speedCosts']]for x in levels]),'Racing (nivel6) habilita cocina activa y Mozo. Parrillero/Premium: ×(1+0.06×nivel), máximo×1.54, aplicado a movimiento/cocción; demás×(1+0.10×nivel), máximo×1.90. Parrilla normal18slots, Premium7slots en modo anterior; cocina activa limita cuatro puestos simultáneos por parrilla. Mesas48unidades **por producto** de comida; Fernet45, barriles12. Plantilla inicial un Parrillero, velocidad0 (×1). Sin tope fijo de empleados: costo progresivo redondeado a múltiplos de5.','Antes de Racing, cocina automática4s y entrega por especialidad; desde Racing, cocción activa2s más traslados/depósito y entrega separada por Mozo. La mejora normal aumenta la cocción automática con el multiplicador de cocineros. Bebidas se reponen cada0.28/WorkRate, no por cada trabajador: contratar más incrementa capacidad de entrega, no multiplica la reposición compartida.','Los campos serializados `minOrderQuantity/maxOrderQuantity=999` no rigen la generación corriente: `SpawnQuotaBalancedOrder()` usa1–4unidades y hasta5productos; máximo21clientes,7columnas. La presión de demanda crece por nivel.','## Tabla1 — Viabilidad general',table(['Nivel','Club','Tiempo(s)','Objetivo','Mejor tiempo(s)','Victorias','Dificultad','Estado'],summary),'## Tabla2 — Plantel recomendado',table(['Nivel','Parrilleros','Premium','Cocacoleros','Ferneteros','Mozos','Velocidad final'],rec),'La tabla es descriptiva de finales observados, no una compra inicial financiable ni garantía de mínimo. Detalles/orden/segundos en cada nivel y CSV.','## Tabla4 — Cuellos de botella',table(['Nivel','Principal señal','Impacto observado','Ajuste recomendado'],bottles),'## Tabla5 — Balance propuesto (ensayos, NO aplicado)',table(['Ámbito','Parámetro','Actual','Propuesto en clon','Interpretación/riesgo'],[['Cada nivel','Duración','120–315s','actual+30s','Puede suavizar todos; comparar resultados abajo'],['Global','Movimiento base','300','360 (+20%)','No altera cocción; afecta todos los roles/rutas y niveles'],['Cocina activa','Cocción','2s','1.6s','Solo Racing+; puede trasladar cuello al Mozo'],['Cada nivel','Costo base de contratación','ver tabla','80%, redondeado arriba a$5','Cambios de economía; posibles niveles iniciales demasiado fáciles']]),table(['Nivel','Alternativa en memoria','N','Victorias','Margen ganador(s)','Cuotas cubiertas(%)'],altrows),'Las alternativas son exploratorias con3semillas comunes por nivel; no sustituyen una confirmación estadística de30. No se recomienda adoptar globalmente un parámetro sin contrastar la progresión completa y una prueba de jugador humano.','## Tabla6 — Campaña completa',table(['Nivel','Campañas que llegan','Victorias','Mercado antes','Compra stock','Recompensa','De ella: progresión','Mercado después'],campaignrows),'La recompensa real por victoria es50%de ventas+2×sobrante de monedas de partido. El primer desbloqueo agrega únicamente el déficit exacto del siguiente stock mínimo; es una recompensa del código existente, no dinero de prueba. El saldo de partido se reinicia y es distinto del dinero Mercado. La derrota detiene el recorrido sin reintentos.','Los ingredientes de los cuatro sándwiches no se consumen durante cocina activa, pero Mercado sigue exigiendo su mínimo antes de entrar. Bebidas sí consumen ingredientes. Los JSON de campaña conservan stock antes/después y todos los importes; no inferir una falla de financiación si solo hubo una derrota operativa.','## Capacidad física preconfigurada (NO campaña legal)',table(['Nivel','Pruebas','Mejor fixture observado','Resultado','Tiempo(s)','Entregas reales'],caprows),'Barrido de velocidad0–9 y un rol a la vez1–6 (bebidas1–4), con otros roles de soporte altos; en niveles activos también8/10/12cocineros/mozos y6bebidas para comprobar que1–6no es un límite artificial. Máximos simultáneos de cuatro cocineros, resto rota. Es capacidad alcanzada, no cota superior matemática.','## Detalle medido por nivel']+details+['## Reproducción y archivos','1. Editor Unity6000.6.3f1 con este proyecto, fuera de Play, sin pruebas en curso. Nunca activar AllowEditorTestInitialization.','2. Desde ExecuteCode/reflection invocar `GameplayBalanceAudit.Start("explore")`; esperar `status.json: finished`. Repetir `finalists`, `capacity`, `campaign`, `alternatives`. Cada etapa reemplaza solo su archivo; el fingerprint rechaza mezclar fuentes diferentes.','3. Ejecutar `python3 Tools/BalanceAudit/report.py`. No requiere dependencias. Ejecutar categoría AuditUnit separada de los barridos.','4. `configuration.json`, `levels.json`, `source-fingerprint.txt`, `summary.json`, `plans.json`, `finalists.json`; NDJSON con cada semilla, compras, costes, estados, ingredientes y entregas. CSVs resumen: `runs.csv`, `production.csv`, `purchase-timing.csv`.','## Limitaciones explícitas','- No hay observación visual/manual de partida completa ni pruebas Android/Firebase: se solicita diagnóstico lógico sin render; no se construyó APK ni se hizo push.','- No se demuestra optimalidad global ni imposibilidad matemática con un conjunto finito. No se estima el tiempo que necesita una persona para decidir.','- Las tasas promedio de producción de equipos enteros no equivalen al máximo individual de cada empleado; duración de fases/estado incluye rotación/espera. No todos los clientes generan la misma distancia ni orden.','- El retorno exacto de inversión no tiene una atribución única entre empleados que producen y entregan conjuntamente; se entregan costos, cronología, ventas y saldos, no ROI causal inventado.','- Si la campaña se detiene antes del nivel11, niveles posteriores solo tienen evidencia aislada; no se inventan campañas completas ni se desbloquean niveles artificialmente.']
 (OUT/'INFORME.md').write_text('\n\n'.join(report)+'\n')
 print(json.dumps(manifest,indent=2));print('Report:',OUT/'INFORME.md')
if __name__=='__main__':main()
