// Editor-only diagnostic. No production balance, PlayerPrefs, Firebase or assets are mutated.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HayChoriYPaty;

public static class GameplayBalanceAudit
{
    public const string DirectoryName = "Audits/GameplayBalance-20261020";
    const float Dt = .1f;
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly MethodInfo AddWorker = typeof(StreetSimulation).GetMethod("AddWorker", Private, null, new[]{typeof(StreetWorkerRole)}, null);
    static readonly PropertyInfo Speed = typeof(StreetSimulation).GetProperty("SpeedLevel");
    static Queue<Action> jobs;
    static readonly object Sync = new object();
    static string phase, identity;
    static int done, total;
    static StreetBalance effective;
    static List<Run> results;
    public static string Output => Path.GetFullPath(DirectoryName);
    [Serializable] public class Plan {
        public string name; public int mode, speedCap=9, staffCap=12;
        public float speedBias=1, mozo=3, normal=1, premium=1, drinks=1, cadence=.5f;
    }
    [Serializable] public class Purchase { public float second; public string action; public int cost, cashBefore, cashAfter; public int[] team; public int speed; }
    [Serializable] public class Run {
        public string config, mode, strategy, variant="current", phase; public int level, seed, speed, initialCoins, earned, spent, cash;
        public float elapsed, remaining, wallSeconds; public bool won, goalReached, detailedMetrics;
        public int[] goals=new int[7], delivered=new int[7], produced=new int[7], deposited=new int[7], picked=new int[7], initialTable=new int[7], finalTable=new int[7], team=new int[5];
        public float[] roleStates=new float[60], roleVisual=new float[5], roleDistance=new float[5], emptyFoodIdle=new float[5], tableFullSeconds=new float[7], grillFullSeconds=new float[7];
        public int[] ingredientsBefore, ingredientsAfter;
        public List<Purchase> purchases=new List<Purchase>();
    }
    [Serializable] public class Plans { public Plan[] items; }
    [Serializable] public class Level {
        public int level, totalGoal, initialCoins; public string club; public bool active;
        public float duration, workerSpeed, customerSpeed, pickupSeconds, cookSeconds, drinkSeconds, demandMultiplier, demandFraction;
        public int[] products, goals, initialTeam, tables, grills, initialStock, requiredIngredients, prices, speedCosts, firstHireCosts;
        public string[] roles; public string hireCurve="x1, x2.5, x5, x9, x14, x20... rounded upward to multiples of 5; free first normal cook";
    }
    [Serializable] public class Levels { public Level[] items; }
    [Serializable] public class Campaign { public int seed, completed, finalist; public string outcome; public List<Transition> levels=new List<Transition>(); }
    [Serializable] public class Transition { public int level; public long marketBefore, restock, reward, progression, marketAfter; public int[] stockBefore, stockAfter; public string result; public float elapsed; }
    [Serializable] public class Status { public string phase, state, error; public int completed,total; }
    static StreetBalance Clone(StreetBalance b) => JsonUtility.FromJson<StreetBalance>(JsonUtility.ToJson(b));
    static string Hash(string text) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
    static void Save(string name, object value) => File.WriteAllText(Path.Combine(Output,name),JsonUtility.ToJson(value,true));
    static void Append(string file, object value) => File.AppendAllText(Path.Combine(Output,file),JsonUtility.ToJson(value)+"\n");
    public static StreetBalance LoadEffective()
    {
        var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
        try { foreach(var root in preview.GetRootGameObjects()) { var g=root.GetComponentInChildren<StreetGame>(true); if(g!=null) { var games=preview.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<StreetGame>(true)).ToArray(); if(games.Length!=1)throw new Exception("Expected exactly one StreetGame in Main"); return Clone(g.Balance); } } throw new Exception("Main has no StreetGame"); }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
    static string Fingerprint() {
        var files=System.IO.Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories).Concat(new[]{"Assets/Scenes/Main.unity"}).OrderBy(x=>x).ToArray();
        return Hash(string.Join("\n",files.Select(x=>x+":"+Hash(File.ReadAllText(x)))));
    }
    public static string Start(string stage)
    {
        if(jobs!=null) throw new Exception("Audit already running");
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Editor must be idle");
        if(StreetGame.AllowEditorTestInitialization) throw new Exception("Unsafe PlayerPrefs test initialization is enabled");
        effective=LoadEffective(); identity=Fingerprint(); System.IO.Directory.CreateDirectory(Output);
        string existing=Path.Combine(Output,"source-fingerprint.txt");
        if(File.Exists(existing) && File.ReadAllText(existing)!=identity) throw new Exception("Runtime source changed; use a new audit directory");
        File.WriteAllText(existing,identity); File.WriteAllText(Path.Combine(Output,"configuration.json"),JsonUtility.ToJson(effective,true));
        File.WriteAllText(Path.Combine(Output,"configuration.sha256"),Hash(JsonUtility.ToJson(effective)));
        phase=stage; done=0; jobs=new Queue<Action>(); results=new List<Run>();
        string stagePath=Path.Combine(Output,stage+".jsonl");
        var completedKeys=new HashSet<string>();
        if(stage=="explore" && File.Exists(stagePath))foreach(var line in File.ReadAllLines(stagePath))if(!string.IsNullOrEmpty(line)){var old=JsonUtility.FromJson<Run>(line);completedKeys.Add(old.level+":"+old.strategy+":"+old.seed);done++;}
        if(stage=="explore") {
            ExportLevels(); var plans=ExplorationPlans(); Save("plans.json",new Plans{items=plans.ToArray()});
            foreach(int level in Enumerable.Range(0,11)) foreach(var p in plans) foreach(int seed in new[]{101}) {
                int l=level,s=seed; var plan=p; if(!completedKeys.Contains((l+1)+":"+p.name+":"+s))jobs.Enqueue(()=>Record(Isolated(l,s,plan),"explore.jsonl"));
            }
        } else if(stage=="finalists") {
            SelectFinalists(); var plans=JsonUtility.FromJson<Plans>(File.ReadAllText(Path.Combine(Output,"finalists.json"))).items;
            for(int l=0;l<11;l++) for(int f=0;f<2;f++) foreach(int seed in Enumerable.Range(1001,30)) { int level=l, s=seed; var p=plans[l*2+f]; jobs.Enqueue(()=>Record(Isolated(level,s,p),"finalists.jsonl")); }
        } else if(stage=="capacity") {
            foreach(int level in Enumerable.Range(0,11)) {
                int l=level;
                for(int speed=0;speed<=9;speed++){int sp=speed; jobs.Enqueue(()=>Record(Capacity(l,501,new[]{6,4,6,4,6},sp),"capacity.jsonl"));}
                for(int role=0;role<5;role++) {
                    if(!new StreetSimulation(effective,l).HasRole((StreetWorkerRole)role))continue;
                    int max=(role==1||role==3)?4:6;
                    for(int n=1;n<=max;n++){var team=new[]{6,4,6,4,6};team[role]=n;jobs.Enqueue(()=>Record(Capacity(l,501,team,9),"capacity.jsonl"));}
                }
                if(l>=5) foreach(int n in new[]{8,10,12}){int count=n;jobs.Enqueue(()=>Record(Capacity(l,501,new[]{count,6,count,6,count},9),"capacity.jsonl"));}
            }
        } else if(stage=="campaign") {
            for(int finalist=0;finalist<2;finalist++)foreach(int seed in Enumerable.Range(1001,30)){int s=seed,f=finalist;jobs.Enqueue(()=>RunCampaign(s,f));}
        } else if(stage=="alternatives") {
            var plans=JsonUtility.FromJson<Plans>(File.ReadAllText(Path.Combine(Output,"finalists.json"))).items;
            foreach(int level in Enumerable.Range(0,11)) foreach(string v in new[]{"duration+30","movement+20pct","activeCook1.6","hire-20pct"}) foreach(int seed in new[]{1001,1002,1003}) {
                int l=level,s=seed; string variant=v; var p=plans[l*2];jobs.Enqueue(()=>Record(Isolated(l,s,p,variant),"alternatives.jsonl"));
            }
        } else throw new ArgumentException("Unknown stage "+stage);
        total=jobs.Count+done; if(stage!="explore" || done==0)File.WriteAllText(Path.Combine(Output,stage+".jsonl"),"");
        if(stage=="campaign")File.WriteAllText(Path.Combine(Output,"campaign-runs.jsonl"),"");
        Save("status.json",new Status{phase=phase,state="running",total=total});
        if(stage=="campaign")EditorApplication.update+=Pump; else System.Threading.Tasks.Task.Run((Action)PumpAll);
        return stage+": queued "+total+" runs, config "+identity;
    }
    static void PumpAll()
    {
        var queue=jobs;
        try {
            System.Threading.Tasks.Parallel.For(0,1,i=>{
                while(true){Action job;lock(Sync){if(queue.Count==0)break;job=queue.Dequeue();}job();lock(Sync){done++;Save("status.json",new Status{phase=phase,state=done==total?"finished":"running",completed=done,total=total});}}
            });
            jobs=null;
        } catch(Exception e){lock(Sync){Save("status.json",new Status{phase=phase,state="failed",completed=done,total=total,error=e.ToString()});}jobs=null;}
    }
    static void Pump()
    {
        try {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            while(jobs.Count>0 && clock.Elapsed.TotalSeconds<.25){jobs.Dequeue()();done++;}
            Save("status.json",new Status{phase=phase,state=jobs.Count==0?"finished":"running",completed=done,total=total});
            if(jobs.Count==0){EditorApplication.update-=Pump;jobs=null;Debug.Log("Balance audit complete: "+phase+" "+done);}
        } catch(Exception e){Save("status.json",new Status{phase=phase,state="failed",completed=done,total=total,error=e.ToString()});EditorApplication.update-=Pump;jobs=null;Debug.LogException(e);}
    }
    static void Record(Run r,string file){lock(Sync){results.Add(r);Append(file,r);}}
    public static List<Plan> ExplorationPlans()
    {
        var ps=new List<Plan>{new Plan{name="no_purchases",mode=9}, new Plan{name="speed_first",mode=1}, new Plan{name="staff_first",mode=2,staffCap=6},
            new Plan{name="balanced"},new Plan{name="goal_priority",mozo=2,speedBias=.5f},new Plan{name="adaptive_lag",mode=3},
            new Plan{name="income_priority",mode=4},new Plan{name="missing_roles",speedBias=.7f},new Plan{name="early_mozos",mozo=5},new Plan{name="fast_speed",speedBias=3}};
        for(int speed=0;speed<=9;speed++)ps.Add(new Plan{name="speed_cap_"+speed,speedCap=speed});
        for(int n=1;n<=6;n++)ps.Add(new Plan{name="staff_cap_"+n,staffCap=n});
        foreach(float m in new[]{1f,2f,4f})foreach(float s in new[]{.4f,1.5f})ps.Add(new Plan{name="search_m"+m+"_s"+s,mozo=m,speedBias=s});
        return ps;
    }
    static void SelectFinalists()
    {
        var all=File.ReadAllLines(Path.Combine(Output,"explore.jsonl")).Where(x=>x.Length>0).Select(x=>JsonUtility.FromJson<Run>(x)).ToList();
        var plans=JsonUtility.FromJson<Plans>(File.ReadAllText(Path.Combine(Output,"plans.json"))).items;
        var final=new List<Plan>();
        for(int l=0;l<11;l++) {
            var ranked=all.Where(x=>x.level==l+1).GroupBy(x=>x.strategy).OrderByDescending(g=>g.Count(r=>r.won)/(double)g.Count()).ThenByDescending(g=>g.Average(Fraction)).ThenBy(g=>g.Average(r=>r.elapsed)).ToArray();
            var signatures=new HashSet<string>();int count=0;foreach(var g in ranked){var signature=string.Join(";",g.OrderBy(r=>r.seed).Select(r=>r.elapsed+":"+string.Join(",",r.team)+":"+r.speed));if(!signatures.Add(signature))continue;final.Add(plans.First(p=>p.name==g.Key));if(++count==2)break;}if(count!=2)throw new Exception("Need two distinct finalist trajectories");
        }
        Save("finalists.json",new Plans{items=final.ToArray()});
    }
    static double Fraction(Run r){double f=0;int count=0;for(int p=0;p<7;p++)if(r.goals[p]>0){f+=Math.Min(1,(double)r.delivered[p]/r.goals[p]);count++;}return f/Math.Max(1,count);}
    static Run Isolated(int level,int seed,Plan p,string variant="current")
    {
        var b=Clone(effective);b.randomSeed=seed;
        if(variant=="duration+30")b.levelDurations[level]+=30;
        if(variant=="movement+20pct")b.workerSpeed*=1.2f;
        if(variant=="activeCook1.6")b.kitchen.activeCookSeconds=1.6f;
        if(variant=="hire-20pct")for(int i=0;i<b.levelBalances[level].hireBaseCostsByRole.Length;i++)b.levelBalances[level].hireBaseCostsByRole[i]=Mathf.CeilToInt(b.levelBalances[level].hireBaseCostsByRole[i]*.8f/5)*5;
        var sim=new StreetSimulation(b,level);
        var market=new StreetMarket(new StreetMarketData(),b);
        if(level==0)market.InitializeNewGameKit();
        else {market.Data.stockUnits=market.RequiredUnits(sim);market.Data.kitGranted=true;market.Data.initializedUnlockedLevel=level;}
        if(!market.BeginAttempt(sim))throw new Exception("Minimum-stock fixture failed");
        sim.BindIngredientStock(market);sim.StartRound();
        var r=RunSimulation(sim,p,null,"conditional_minimum_stock",market);r.variant=variant;return r;
    }
    static Run Capacity(int level,int seed,int[] team,int speed)
    {
        var b=Clone(effective);b.randomSeed=seed;var sim=new StreetSimulation(b,level);sim.StartRound();
        for(int role=0;role<5;role++)if(sim.HasRole((StreetWorkerRole)role))while(sim.WorkerCount((StreetWorkerRole)role)<team[role])AddWorker.Invoke(sim,new object[]{(StreetWorkerRole)role});
        Speed.SetValue(sim,speed);
        var r=RunSimulation(sim,new Plan{name="capacity_"+string.Join("_",team)+"_s"+speed,mode=9},null,"capacity_preconfigured_unlimited_stock",null);
        return r;
    }
    static Run RunSimulation(StreetSimulation sim,Plan plan,StreetGame game,string mode,StreetMarket market)
    {
        bool detailed=(phase!="explore"&&phase!="capacity")||plan.mode==3;
        var sw=System.Diagnostics.Stopwatch.StartNew();var r=new Run{detailedMetrics=detailed,config=identity,level=sim.LevelIndex+1,seed=GetBalance(sim).randomSeed,strategy=plan.name,mode=mode,initialCoins=sim.Coins};
        for(int p=0;p<7;p++){r.goals[p]=sim.GetProductGoal(p);r.initialTable[p]=sim.Kitchen.TableCount(p);}
        if(market!=null)r.ingredientsBefore=(int[])market.Data.stockUnits.Clone();
        var cooked=new HashSet<int>();var deposited=new HashSet<int>();var picked=new HashSet<int>();
        foreach(var u in sim.Kitchen.Units)if(u.State==StreetFoodState.Cooked){cooked.Add(u.Id);deposited.Add(u.Id);}
        var roleById=new Dictionary<int,StreetWorkerRole>();var tables=new int[7];var grills=new int[7];
        float nextBuy=0;
        while(sim.Phase==RoundPhase.Playing && sim.Elapsed<1000) {
            if(sim.Elapsed+.001f>=nextBuy){for(int buy=0;buy<32;buy++)if(!Buy(sim,plan,r,game))break;nextBuy=sim.Elapsed+plan.cadence;}
            var before=sim.Elapsed;sim.Step(Dt);float dt=sim.Elapsed-before;
            if(detailed){
            Array.Clear(tables,0,7);Array.Clear(grills,0,7);foreach(var u in sim.Kitchen.Units){if(u.Location==StreetFoodLocation.Table&&u.State==StreetFoodState.Cooked)tables[u.Product]++;if(u.Location==StreetFoodLocation.Grill)grills[u.Product]++;}
            foreach(var w in sim.Workers) {
                roleById[w.Id]=w.Role;
                int role=(int)w.Role;r.roleStates[role*12+(int)w.State]+=dt;r.roleDistance[role]+=w.StepDistance*2; // sampled distance is superseded by final TravelDistance below
                if(w.VisualCooking)r.roleVisual[role]+=dt;
                if(w.State==StreetWorkerState.Idle && w.Role==StreetWorkerRole.Mozo && tables[0]+tables[1]+tables[2]+tables[3]==0)r.emptyFoodIdle[role]+=dt;
            }
            for(int p=0;p<7;p++){int cap=sim.Kitchen.TableCapacity(p);if(cap>0&&tables[p]>=cap)r.tableFullSeconds[p]+=dt;int gc=sim.Kitchen.GrillCapacity(p);if(gc>0&&grills[p]>=gc)r.grillFullSeconds[p]+=dt;}
            foreach(var u in sim.Kitchen.Units)if(u.State==StreetFoodState.Cooked){
                if(cooked.Add(u.Id))r.produced[u.Product]++;
                bool deliveryCarry=u.Location==StreetFoodLocation.Carried && roleById.ContainsKey(u.WorkerId)&&StreetSpecialties.IsDeliveryResponsible(roleById[u.WorkerId],u.Product,sim.UsesActiveCooks);
                if((u.Location==StreetFoodLocation.Table||deliveryCarry)&&deposited.Add(u.Id))r.deposited[u.Product]++;
                if(deliveryCarry&&picked.Add(u.Id))r.picked[u.Product]++;
            }
            }
        }
        r.roleDistance=new float[5];foreach(var w in sim.Workers)r.roleDistance[(int)w.Role]+=w.TravelDistance;
        r.won=sim.Phase==RoundPhase.Won;r.phase=sim.Phase.ToString();r.goalReached=sim.GoalReached;r.elapsed=sim.Elapsed;r.remaining=sim.TimeRemaining;r.earned=sim.CoinsEarned;r.cash=sim.Coins;r.speed=sim.SpeedLevel;
        for(int p=0;p<7;p++){r.delivered[p]=sim.GetProductDelivered(p);r.finalTable[p]=sim.Kitchen.TableCount(p);if(r.won&&r.delivered[p]<r.goals[p])throw new Exception("False victory");if(detailed&&r.delivered[p]>r.initialTable[p]+r.deposited[p])throw new Exception("Delivery without deposit p"+p);}
        for(int role=0;role<5;role++)r.team[role]=sim.WorkerCount((StreetWorkerRole)role);
        if(mode!="capacity_preconfigured_unlimited_stock" && r.initialCoins+r.earned-r.spent!=r.cash)throw new Exception("Cash conservation");
        if(market!=null){r.ingredientsAfter=(int[])market.Data.stockUnits.Clone();if(r.ingredientsAfter.Any(x=>x<0))throw new Exception("Negative ingredients");}
        r.wallSeconds=(float)sw.Elapsed.TotalSeconds;return r;
    }
    static StreetBalance GetBalance(StreetSimulation sim)=>(StreetBalance)typeof(StreetSimulation).GetField("balance",Private).GetValue(sim);
    static bool Buy(StreetSimulation sim,Plan p,Run run,StreetGame game)
    {
        if(p.mode==9)return false;
        float[] work=new float[5];
        for(int prod=0;prod<7;prod++)if(sim.IsProductAvailable(prod)) {
            float load=sim.GetProductRemaining(prod);if(p.mode==4)load*=sim.GetProductPrice(prod)/5f;
            int producer=(int)StreetSpecialties.GetProductionRole(prod), deliver=(int)StreetSpecialties.GetDeliveryRole(prod,sim.UsesActiveCooks);
            work[producer]+=load;if(producer!=deliver)work[deliver]+=load;
        }
        work[0]*=p.normal;work[1]*=p.drinks;work[2]*=p.premium;work[3]*=p.drinks;work[4]*=p.mozo;
        if(p.mode==3&&sim.Elapsed>20)for(int role=0;role<5;role++) {
            int output=0;for(int prod=0;prod<7;prod++)if((int)StreetSpecialties.GetDeliveryRole(prod,sim.UsesActiveCooks)==role)output+=sim.GetProductDelivered(prod);
            if(sim.UsesActiveCooks&&(role==0||role==2)){output=0;for(int prod=role==0?0:2;prod<(role==0?2:4);prod++)output+=run.deposited[prod];}
            work[role]*=Mathf.Clamp(sim.Elapsed/Mathf.Max(10,output),.5f,4f);
        }
        int choice=-2;float best=-1;
        for(int role=0;role<5;role++)if(sim.HasRole((StreetWorkerRole)role)&&sim.WorkerCount((StreetWorkerRole)role)==0&&work[role]>0){float score=work[role]/Mathf.Max(1,sim.HireCostForRole((StreetWorkerRole)role));if(score>best){best=score;choice=role;}}
        if(choice==-2){best=-1;float speedBenefit=0;
            for(int role=0;role<5;role++)if(work[role]>0&&sim.HasRole((StreetWorkerRole)role)){
                int n=Math.Max(1,sim.WorkerCount((StreetWorkerRole)role));float rate=role==0||role==2?sim.CookWorkRate:sim.WorkRate;
                float score=work[role]/(n*(n+1)*rate*Mathf.Max(1,sim.HireCostForRole((StreetWorkerRole)role)));
                if(n<p.staffCap&&score>best){best=score;choice=role;}
                float inc=role==0||role==2?.06f:GetBalance(sim).speedIncrease;
                speedBenefit+=work[role]/n*(1f/rate-1f/(rate+inc));
            }
            float speedScore=speedBenefit*p.speedBias/Mathf.Max(1,sim.SpeedCost);
            if(p.mode==1)speedScore=1000000;
            if(p.mode==2&&choice>=0)speedScore=-1;
            if(sim.SpeedLevel<p.speedCap&&speedScore>best){choice=-1;best=speedScore;}
        }
        if(choice==-2)return false;
        int cost=choice==-1?sim.SpeedCost:sim.HireCostForRole((StreetWorkerRole)choice);
        if(cost<=0||cost>sim.Coins)return false;
        var purchase=new Purchase{second=sim.Elapsed,action=choice==-1?"speed":((StreetWorkerRole)choice).ToString(),cost=cost,cashBefore=sim.Coins};
        bool ok=choice==-1?(game==null?sim.TryUpgradeSpeed():game.TryUpgradeSpeed()):(game==null?sim.TryHire((StreetWorkerRole)choice):game.TryHire((StreetWorkerRole)choice));
        if(!ok)throw new Exception("Rejected planned affordable enabled purchase");
        purchase.cashAfter=sim.Coins;purchase.team=Enumerable.Range(0,5).Select(i=>sim.WorkerCount((StreetWorkerRole)i)).ToArray();purchase.speed=sim.SpeedLevel;run.purchases.Add(purchase);run.spent+=cost;return true;
    }
    static void RunCampaign(int seed,int finalist)
    {
        var plans=JsonUtility.FromJson<Plans>(File.ReadAllText(Path.Combine(Output,"finalists.json"))).items;
        var campaign=new Campaign{seed=seed,finalist=finalist,outcome="completed"};var host=new GameObject("Balance audit memory-only player"){hideFlags=HideFlags.HideAndDontSave};host.SetActive(false);
        var game=host.AddComponent<StreetGame>();var b=Clone(effective);b.randomSeed=seed;typeof(StreetGame).GetField("balance",Private).SetValue(game,b);
        string memory=null;
        try{
            if(!game.ActivatePlayer(null,(json,record)=>{memory=json;return true;}))throw new Exception("Memory player activation failed");
            for(int l=0;l<11;l++){
                if(game.Sim.LevelIndex!=l||game.UnlockedLevel!=l)throw new Exception("Illegal unlock");
                var t=new Transition{level=l+1,marketBefore=game.Market.BalanceCents,stockBefore=(int[])game.Market.Data.stockUnits.Clone()};
                if(l>0)for(int id=0;id<10;id++){int packs=game.Market.MissingPacks(game.Sim,id);if(packs>0&&!game.TryBuyIngredient(id,packs,game.Market.NextPurchaseId)){t.result="restock_blocked";break;}}
                t.restock=t.marketBefore-game.Market.BalanceCents;
                if(!game.TryStartRound()){t.result="start_blocked";campaign.outcome=t.result;campaign.levels.Add(t);break;}
                var r=RunSimulation(game.Sim,plans[l*2+finalist],game,"campaign",game.Market);Append("campaign-runs.jsonl",r);
                if(!game.ProcessRoundResult())throw new Exception("Settlement failed");
                var receipt=game.Market.LastResult;t.result=r.phase;t.elapsed=r.elapsed;t.reward=receipt.earnedCents;t.progression=receipt.progressionBonusCents;t.marketAfter=game.Market.BalanceCents;t.stockAfter=(int[])game.Market.Data.stockUnits.Clone();
                if(t.marketBefore-t.restock+t.reward!=t.marketAfter)throw new Exception("Market cash conservation");
                campaign.levels.Add(t);if(!r.won){campaign.outcome="lost_level_"+(l+1);break;}campaign.completed++;
                if(l==10)break;
                if(!game.NextLevel())throw new Exception("NextLevel failed");
                // Real v4 JSON round-trip, same production ActivatePlayer path; no PlayerPrefs or cloud.
                UnityEngine.Object.DestroyImmediate(host);host=new GameObject("Balance audit reloaded player"){hideFlags=HideFlags.HideAndDontSave};host.SetActive(false);game=host.AddComponent<StreetGame>();typeof(StreetGame).GetField("balance",Private).SetValue(game,b);
                if(!game.ActivatePlayer(memory,(json,record)=>{memory=json;return true;})||!game.SelectLevel(l+1))throw new Exception("Legal reload failed");
            }
        } finally{UnityEngine.Object.DestroyImmediate(host);}
        Append("campaign.jsonl",campaign);
    }
    static void ExportLevels()
    {
        var ls=new List<Level>();
        for(int l=0;l<11;l++){
            var s=new StreetSimulation(effective,l);s.StartRound();var row=new Level{level=l+1,club=StreetSimulation.LevelDisplayNames[l],active=s.UsesActiveCooks,totalGoal=s.Goal,duration=s.TimeRemaining,initialCoins=s.Coins,workerSpeed=effective.workerSpeed,customerSpeed=effective.customerSpeed,pickupSeconds=effective.pickupSeconds,cookSeconds=s.UsesActiveCooks?effective.kitchen.activeCookSeconds:effective.kitchen.cookSeconds,drinkSeconds=effective.kitchen.drinkPrepareSeconds,demandMultiplier=effective.levelDemandMultipliers[l],demandFraction=s.DemandFraction};
            row.products=Enumerable.Range(0,s.ProductCount).Select(s.GetAvailableProduct).ToArray();row.goals=Enumerable.Range(0,7).Select(s.GetProductGoal).ToArray();row.initialTeam=Enumerable.Range(0,5).Select(i=>s.WorkerCount((StreetWorkerRole)i)).ToArray();row.roles=Enumerable.Range(0,5).Where(i=>s.HasRole((StreetWorkerRole)i)).Select(i=>((StreetWorkerRole)i).ToString()).ToArray();row.tables=Enumerable.Range(0,7).Select(s.Kitchen.TableCapacity).ToArray();row.grills=Enumerable.Range(0,7).Select(s.Kitchen.GrillCapacity).ToArray();row.initialStock=Enumerable.Range(0,7).Select(s.Kitchen.TableCount).ToArray();row.prices=Enumerable.Range(0,7).Select(i=>(int)s.GetProductPrice(i)).ToArray();row.firstHireCosts=Enumerable.Range(0,5).Select(i=>s.HireCostForRole((StreetWorkerRole)i)).ToArray();row.speedCosts=new int[9];for(int speed=0;speed<9;speed++){Speed.SetValue(s,speed);row.speedCosts[speed]=s.SpeedCost;}row.requiredIngredients=new StreetMarket(new StreetMarketData(),effective).RequiredUnits(s);ls.Add(row);
        }
        Save("levels.json",new Levels{items=ls.ToArray()});File.WriteAllText(Path.Combine(Output,"runtime-states.json"),"{\"roles\":[\"Parrillero\",\"Cocacolero\",\"ParrilleroPremium\",\"Fernetero\",\"Mozo\"],\"states\":["+string.Join(",",Enum.GetNames(typeof(StreetWorkerState)).Select(x=>"\""+x+"\""))+"]}");
    }
    public static string ReplayCheck()
    {
        effective=LoadEffective();identity=Fingerprint();var old=File.ReadAllLines(Path.Combine(Output,"explore.jsonl")).Select(x=>JsonUtility.FromJson<Run>(x)).First(r=>r.level==3&&r.strategy=="balanced");
        var plan=ExplorationPlans().First(p=>p.name==old.strategy);string previousPhase=phase;phase="replay";var fresh=Isolated(2,old.seed,plan);phase=previousPhase;old.detailedMetrics=true;old.wallSeconds=0;fresh.wallSeconds=0;
        if(JsonUtility.ToJson(old)!=JsonUtility.ToJson(fresh))throw new Exception("Instrumentation optimization changed measured outcome");
        return "PASS exact replay across observer optimization: level3 balanced seed"+old.seed;
    }
    public static string SelfCheck()
    {
        var b=LoadEffective();var before=JsonUtility.ToJson(b);var s=new StreetSimulation(b,5);s.StartRound();var r=RunSimulation(s,new Plan{name="check",mode=9},null,"selfcheck_unlimited_stock",null);
        int[] expectedTeam = { 1, 0, 1, 0, 1 };
        if(r.won||!r.phase.Equals("Lost")||r.spent!=0||!r.team.SequenceEqual(expectedTeam)||r.elapsed<b.levelDurations[5]-.1f)throw new Exception("Selfcheck legality/terminal condition failed");
        if(before!=JsonUtility.ToJson(b))throw new Exception("Balance mutated");
        return "PASS: no-purchase true terminal loss, real goal predicate, zero extra hires/money, unchanged balance; "+r.elapsed+"s";
    }
}
