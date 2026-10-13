using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace HayChoriYPaty.Tests
{
    // Runtime stays in Assembly-CSharp; EditMode tests bind by reflection.
    public sealed class StreetMarketTests
    {
        const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;
        Type simType, balanceType, marketType, dataType;
        [SetUp] public void Setup()
        {
            simType=Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp",true);
            balanceType=Type.GetType("HayChoriYPaty.StreetBalance, Assembly-CSharp",true);
            marketType=Type.GetType("HayChoriYPaty.StreetMarket, Assembly-CSharp",true);
            dataType=Type.GetType("HayChoriYPaty.StreetMarketData, Assembly-CSharp",true);
        }
        object Sim(int level=0) => Activator.CreateInstance(simType,new object[]{Activator.CreateInstance(balanceType),level,5f,0,1,0});
        object Market(object data=null,object balance=null) => Activator.CreateInstance(marketType,new[]{data??Activator.CreateInstance(dataType),balance??Activator.CreateInstance(balanceType)});
        static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name,Public).Invoke(target,args);
        static object Get(object target,string name) => target.GetType().GetProperty(name,Public).GetValue(target,null);
        [Test] public void JsonKeepsCentsAndNormalizesExistingStock()
        {
            object data=Activator.CreateInstance(dataType); dataType.GetField("balanceCents").SetValue(data,257L);
            dataType.GetField("stockUnits").SetValue(data,new[]{-3,2});
            object restored=JsonUtility.FromJson(JsonUtility.ToJson(data),dataType); object market=Market(restored);
            Assert.AreEqual(257L,Get(market,"BalanceCents")); Assert.AreEqual(0,Call(market,"Units",0)); Assert.AreEqual(2,Call(market,"Units",1));
            Assert.AreEqual(1600,Call(market,"PackPriceCents",8));
            object shortBalance=Activator.CreateInstance(balanceType);
            object marketConfig=balanceType.GetField("market",Public).GetValue(shortBalance);
            marketConfig.GetType().GetField("packPricesCents",Public).SetValue(marketConfig,new[]{999});
            Assert.AreEqual(1600,Call(Market(null,shortBalance),"PackPriceCents",8));
        }
        [Test] public void RecipesUseActualCatalogGoalsAndBreadIsShared()
        {
            object market=Market(), sim=Sim(4); int[] req=(int[])Call(market,"RequiredUnits",sim);
            Assert.AreEqual((int)Call(sim,"GetProductGoal",0)+(int)Call(sim,"GetProductGoal",2)+(int)Call(sim,"GetProductGoal",3),req[1]);
            Assert.AreEqual(Call(sim,"GetProductGoal",1),req[3]);
            Assert.AreEqual(req[1],Call(market,"MissingUnits",sim,1));
            Assert.AreEqual((req[8]+3)/4,Call(market,"MissingPacks",sim,8));
            Assert.IsFalse((bool)Call(market,"IsIngredientUnlocked",8,0));
            Assert.IsTrue((bool)Call(market,"IsIngredientUnlocked",8,4));
        }
        [Test] public void PurchaseRejectsDuplicateFutureIngredientAndNumericOverflow()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), sim=Sim(); Call(market,"Initialize",0);
            dataType.GetField("balanceCents").SetValue(Get(market,"Data"),100000L);
            int before=(int)Call(market,"Units",0);
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,8,1,false,0,1L));
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,0,int.MaxValue,false,0,1L));
            Assert.IsTrue((bool)Call(market,"TryBuy",sim,0,1,false,0,1L));
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,0,1,false,0,1L));
            Assert.AreEqual(before+1,Call(market,"Units",0));
        }
        [Test] public void ReservationsRequireAttemptAndReleaseOnAbort()
        {
            object market=Market(), sim=Sim(); Call(market,"Initialize",0);
            Assert.IsFalse((bool)Call(market,"TryReserveFood",11,0));
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim)); int before=(int)Call(market,"Units",0);
            Assert.IsTrue((bool)Call(market,"TryReserveFood",11,0)); Assert.IsFalse((bool)Call(market,"TryReserveFood",11,0));
            Assert.IsFalse((bool)Call(market,"ConsumeReservedFood",11,1));
            Assert.IsTrue((bool)Call(market,"ConsumeReservedFood",11,0)); Assert.IsFalse((bool)Call(market,"ConsumeReservedFood",11,0));
            Call(market,"AbortAttempt"); Assert.AreEqual(before,Call(market,"Units",0));
        }
        static void Set(object target,string field,object value)
        {
            FieldInfo backing=target.GetType().GetField("<"+field+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(backing,field+" backing field"); backing.SetValue(target,value);
        }
        [Test] public void WrongStateCannotSettleAndLossCreditsOnlyNewHalfSales()
        {
            object market=Market(), sim=Sim(); Call(market,"Initialize",0);
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim));
            Set(sim,"CoinsEarned",40); Set(sim,"Coins",3); Set(sim,"Phase",Enum.Parse(Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp",true),"Lost"));
            object result=Call(market,"SettleAttempt",sim);
            Assert.AreEqual(2000L,result.GetType().GetField("recognizedSalesCents",Public).GetValue(result));
            Assert.AreEqual(0L,result.GetType().GetField("bonusCents",Public).GetValue(result));
            Assert.AreSame(result,Call(market,"SettleAttempt",sim));
            object persisted=JsonUtility.FromJson(JsonUtility.ToJson(Get(market,"Data")),dataType);
            Assert.IsNotNull(dataType.GetField("lastResult").GetValue(persisted));
            object replay=Sim(); Assert.IsTrue((bool)Call(market,"BeginAttempt",replay));
            Set(replay,"CoinsEarned",60); Set(replay,"Coins",4);
            Set(replay,"Phase",Enum.Parse(Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp",true),"Won"));
            object win=Call(market,"SettleAttempt",replay);
            Assert.AreEqual(1000L,win.GetType().GetField("recognizedSalesCents",Public).GetValue(win));
            Assert.AreEqual(800L,win.GetType().GetField("bonusCents",Public).GetValue(win));
            Assert.AreSame(win,Call(market,"SettleAttempt",replay));
            object later=Sim(); Assert.IsTrue((bool)Call(market,"BeginAttempt",later));
            Set(later,"CoinsEarned",80); Set(later,"Coins",9);
            Set(later,"Phase",Enum.Parse(Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp",true),"Won"));
            object nextWin=Call(market,"SettleAttempt",later);
            Assert.AreEqual(1000L,nextWin.GetType().GetField("recognizedSalesCents",Public).GetValue(nextWin));
            Assert.AreEqual(0L,nextWin.GetType().GetField("bonusCents",Public).GetValue(nextWin));
        }
        [Test] public void AttemptsRequireReadyAndAllElevenCostsStayFinite()
        {
            object market=Market();
            for(int level=0;level<11;level++)
            {
                object sim=Sim(level); long cost=(long)Call(market,"MinimumPurchaseCostCents",sim);
                Assert.Greater(cost,0L); Assert.Less(cost,long.MaxValue);
            }
            object ready=Sim(); Call(market,"Initialize",0); Assert.IsTrue((bool)Call(market,"BeginAttempt",ready));
            Assert.IsFalse((bool)Call(market,"BeginAttempt",ready)); Call(market,"AbortAttempt");
        }
        [Test] public void ReadyCannotSettleAndDebtOrOperationOverflowRejectsAtomically()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), sim=Sim(); Call(market,"Initialize",0);
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim)); Assert.IsNull(Call(market,"SettleAttempt",sim));
            Assert.IsTrue((bool)dataType.GetField("attemptActive").GetValue(Get(market,"Data"))); Call(market,"AbortAttempt");
            market=Market(); Call(market,"Initialize",1); sim=Sim(1);
            int[] stock=(int[])dataType.GetField("stockUnits").GetValue(Get(market,"Data")); stock[4]=0;
            dataType.GetField("debtCents").SetValue(Get(market,"Data"),long.MaxValue);
            dataType.GetField("balanceCents").SetValue(Get(market,"Data"),0L);
            long op=(long)Get(market,"NextPurchaseId");
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,4,1,true,1,op));
            Assert.AreEqual(long.MaxValue,Get(market,"DebtCents")); Assert.AreEqual(0,Call(market,"Units",4));
        }
        [Test] public void ReservationsCannotSpendOtherProductsSharedBreadQuota()
        {
            object balance=Activator.CreateInstance(balanceType);
            Array levels=(Array)balanceType.GetField("levelBalances",Public).GetValue(balance);
            object level=levels.GetValue(3); int[] goals=(int[])level.GetType().GetField("goalsByProductId",Public).GetValue(level);
            Array.Clear(goals,0,goals.Length);
            object sim=Activator.CreateInstance(simType,new object[]{balance,3,5f,0,1,0});
            int count=(int)simType.GetProperty("ProductCount",Public).GetValue(sim,null);
            for(int slot=0;slot<count;slot++){int product=(int)simType.GetMethod("GetAvailableProduct",Public).Invoke(sim,new object[]{slot});goals[product]=1;}
            sim=Activator.CreateInstance(simType,new object[]{balance,3,5f,0,1,0});
            object data=Activator.CreateInstance(dataType), market=Market(data,balance);
            int[] required=(int[])Call(market,"RequiredUnits",sim); dataType.GetField("stockUnits").SetValue(data,required);
            market=Market(data,balance); Assert.IsTrue((bool)Call(market,"BeginAttempt",sim));
            Assert.IsTrue((bool)Call(market,"TryReserveFood",51,0));
            Assert.IsTrue((bool)Call(market,"TryReserveFood",52,2)); // Bondiola still has its shared pan portion.
            Assert.IsFalse((bool)Call(market,"TryReserveFood",53,0)); // No surplus pan can be tied up by extra Chori.
            ((int[])dataType.GetField("stockUnits").GetValue(Get(market,"Data")))[0]++;
            ((int[])dataType.GetField("stockUnits").GetValue(Get(market,"Data")))[1]++;
            Assert.IsTrue((bool)Call(market,"TryReserveFood",53,0)); // Extra sales are allowed once other quotas are protected.
            Assert.IsFalse((bool)Call(market,"TryReserveFood",54,0));
        }
        [Test] public void BottleShortagesAreIndependentAndPartialPortionsRestoreExactly()
        {
            object sim=Sim(4), market=Market(); Call(market,"Initialize",4);
            object data=Get(market,"Data"); int[] stock=(int[])dataType.GetField("stockUnits").GetValue(data);
            int required=((int[])Call(market,"RequiredUnits",sim))[8];
            stock[8]=required; stock[9]=required-1;
            Assert.AreEqual(0,Call(market,"MissingPacks",sim,8));
            Assert.AreEqual(1,Call(market,"MissingUnits",sim,9));
            Assert.AreEqual(1,Call(market,"MissingPacks",sim,9));
            Assert.IsTrue((bool)Call(market,"TryBuy",sim,9,1,true,4,1L));
            Assert.AreEqual(required+3,Call(market,"Units",9));
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim));
            Assert.IsTrue((bool)Call(market,"TryReserveFood",91,5));
            Assert.IsTrue((bool)Call(market,"ConsumeReservedFood",91,5));
            Assert.AreEqual(required-1,Call(market,"Units",8));
            Assert.AreEqual(required+2,Call(market,"Units",9));
            object restored=Market(JsonUtility.FromJson(JsonUtility.ToJson(Get(market,"Data")),dataType));
            Assert.IsTrue((bool)Call(restored,"RecoverAbandonedAttempt"));
            Assert.AreEqual(required,Call(restored,"Units",8));
            Assert.AreEqual(required+3,Call(restored,"Units",9));
            Assert.IsFalse((bool)Call(restored,"RecoverAbandonedAttempt"));
        }

        [Test] public void LegacyMigrationUsesHighestUnlockAndGrantsKitOnlyOnce()
        {
            object market=Market(); Call(market,"Initialize",3); object data=Get(market,"Data");
            long watermark=(long)((Array)dataType.GetField("highestRecognizedSalesByLevel").GetValue(data)).GetValue(0);
            Assert.Greater(watermark,0L);
            object highest=Sim(3); int[] required=(int[])Call(market,"RequiredUnits",highest);
            for(int i=0;i<10;i++) Assert.GreaterOrEqual((int)Call(market,"Units",i),required[i]);
            int before=(int)Call(market,"Units",0); ((int[])dataType.GetField("stockUnits").GetValue(data))[0]=0;
            Call(market,"Initialize",3); Assert.AreEqual(0,Call(market,"Units",0)); Assert.Greater(before,0);
        }
        [Test] public void ElevenLevelMinimumOnlyFiadoChainSettlesWithoutUnpayableDebt()
        {
            object market=Market(); Call(market,"Initialize",0); int foodId=1000;
            for(int level=0;level<11;level++)
            {
                object sim=Sim(level);
                for(int ingredient=0;ingredient<10;ingredient++)
                {
                    int packs=(int)Call(market,"MissingPacks",sim,ingredient);
                    if(packs<=0) continue;
                    long operation=(long)Get(market,"NextPurchaseId");
                    Assert.IsTrue((bool)Call(market,"TryBuy",sim,ingredient,packs,true,level,operation),"level "+level+", ingredient "+ingredient);
                }
                Assert.IsTrue((bool)Call(market,"BeginAttempt",sim),"stocked level "+level);
                int grossCoins=0; Array delivered=(Array)simType.GetField("deliveredByProduct",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sim);
                int count=(int)simType.GetProperty("ProductCount",Public).GetValue(sim,null);
                for(int slot=0;slot<count;slot++)
                {
                    int product=(int)simType.GetMethod("GetAvailableProduct",Public).Invoke(sim,new object[]{slot});
                    int goal=(int)simType.GetMethod("GetProductGoal",Public).Invoke(sim,new object[]{product});
                    int each=Mathf.RoundToInt((float)simType.GetMethod("GetProductPrice",Public).Invoke(sim,new object[]{product}));
                    for(int unit=0;unit<goal;unit++)
                    {
                        Assert.IsTrue((bool)Call(market,"TryReserveFood",foodId,product),"reserve level "+level+" product "+product+" portion "+unit);
                        Assert.IsTrue((bool)Call(market,"ConsumeReservedFood",foodId,product)); foodId++;
                        delivered.SetValue(unit+1,product); grossCoins+=each;
                    }
                }
                Set(sim,"CoinsEarned",grossCoins); Set(sim,"Coins",0);
                Set(sim,"Phase",Enum.Parse(Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp",true),"Won"));
                Assert.IsNotNull(Call(market,"SettleAttempt",sim)); Assert.AreEqual(0L,Get(market,"DebtCents"),"debt after level "+level);
            }
        }
    }
}
