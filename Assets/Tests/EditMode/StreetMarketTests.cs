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
        static object Call(object target,string name,params object[] args)
        {
            var methods = target.GetType().GetMethods(Public);
            for (int i = 0; i < methods.Length; i++)
            {
                var parameters = methods[i].GetParameters();
                if (methods[i].Name != name || parameters.Length != args.Length) continue;
                bool compatible = true;
                for (int p = 0; p < parameters.Length; p++)
                    if (args[p] != null && !parameters[p].ParameterType.IsInstanceOfType(args[p])) { compatible = false; break; }
                if (compatible) return methods[i].Invoke(target, args);
            }
            throw new MissingMethodException(target.GetType().FullName, name);
        }
        static object CallStatic(Type target,string name,params object[] args) => target.GetMethod(name,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
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
        [Test] public void MunicipalPermitPurchaseChargesExactlyOnceAndIsNotAnIngredient()
        {
            object data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,100000L);
            dataType.GetField("preparationLevelId").SetValue(data,0);
            object market=Market(data);

            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",0,0));
            Assert.IsFalse((bool)Call(market,"TryBuyPreparationPermit",0,0),"A repeated tap must not charge twice.");
            Assert.AreEqual(50000L,Get(market,"BalanceCents"));
            object saved=Get(market,"Data");
            Assert.IsTrue((bool)dataType.GetField("municipalPermitPurchased").GetValue(saved));
            Assert.IsFalse((bool)dataType.GetField("policeContributionPurchased").GetValue(saved));
            Assert.AreEqual(0L,(long)dataType.GetField("cumulativePurchaseSpendCents").GetValue(saved));
        }
        [Test] public void PolicePermitAndBothPurchasesUseCentralPrice()
        {
            object data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,100000L);
            dataType.GetField("preparationLevelId").SetValue(data,0);
            object market=Market(data);

            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",1,0));
            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",0,0));
            Assert.IsFalse((bool)Call(market,"TryBuyPreparationPermit",1,0));
            Assert.AreEqual(0L,Get(market,"BalanceCents"));
            object saved=Get(market,"Data");
            Assert.IsTrue((bool)dataType.GetField("municipalPermitPurchased").GetValue(saved));
            Assert.IsTrue((bool)dataType.GetField("policeContributionPurchased").GetValue(saved));
            Type catalog=Type.GetType("HayChoriYPaty.StreetMarketPreparationCatalog, Assembly-CSharp",true);
            Assert.AreEqual(50000L,(long)catalog.GetField("ItemPriceCents",BindingFlags.Public|BindingFlags.Static).GetRawConstantValue());
        }
        [Test] public void InsufficientBalanceRejectsPermitWithoutMutation()
        {
            object data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,49999L);
            dataType.GetField("preparationLevelId").SetValue(data,0);
            object market=Market(data);

            Assert.IsFalse((bool)Call(market,"TryBuyPreparationPermit",0,0));
            Assert.AreEqual(49999L,Get(market,"BalanceCents"));
            Assert.IsFalse((bool)dataType.GetField("municipalPermitPurchased").GetValue(Get(market,"Data")));
        }
        [Test] public void PermitPurchasesSurviveSerializationAndClearOnlyForANewPreparation()
        {
            object data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,100000L);
            dataType.GetField("preparationLevelId").SetValue(data,0);
            object market=Market(data);
            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",0,0));
            object loadedData=JsonUtility.FromJson(JsonUtility.ToJson(Get(market,"Data")),dataType);
            object restored=Market(loadedData);
            Assert.IsTrue((bool)Call(restored,"IsPreparationPermitPurchased",0,0));
            Assert.IsFalse((bool)Call(restored,"SelectPreparationLevel",0));
            Assert.IsTrue((bool)Call(restored,"IsPreparationPermitPurchased",0,0));
            Assert.IsTrue((bool)Call(restored,"SelectPreparationLevel",1));
            Assert.IsFalse((bool)Call(restored,"IsPreparationPermitPurchased",0,1));
            Assert.AreEqual(50000L,Get(restored,"BalanceCents"),"Changing the preparation clears flags without refunding the purchase.");
        }
        [Test] public void ConsumingSuccessfulPreparationClearsFlagsWithoutChangingEconomy()
        {
            object data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,100000L);
            dataType.GetField("preparationLevelId").SetValue(data,0);
            object market=Market(data);
            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",0,0));
            Assert.IsTrue((bool)Call(market,"TryBuyPreparationPermit",1,0));
            Call(market,"ConsumePreparationPermits",0);
            Assert.IsFalse((bool)Call(market,"IsPreparationPermitPurchased",0,0));
            Assert.IsFalse((bool)Call(market,"IsPreparationPermitPurchased",1,0));
            Assert.AreEqual(0L,Get(market,"BalanceCents"));
        }
        [Test] public void LegacyMarketSaveWithoutPreparationFieldsLoadsWithoutPermits()
        {
            object legacy=JsonUtility.FromJson("{\"balanceCents\":12345}",dataType);
            object market=Market(legacy);
            object restored=Get(market,"Data");

            Assert.AreEqual(12345L,Get(market,"BalanceCents"));
            Assert.IsFalse((bool)dataType.GetField("municipalPermitPurchased").GetValue(restored));
            Assert.IsFalse((bool)dataType.GetField("policeContributionPurchased").GetValue(restored));
            Assert.AreEqual(0,Call(market,"Units",0));
        }
        [Test] public void MarketWatermarksKeepHuracanAndArgentinosInSeparateStableSlots()
        {
            object data = Activator.CreateInstance(dataType);
            object market = Market(data);
            long[] sales = (long[])dataType.GetField("highestRecognizedSalesByLevel").GetValue(Get(market, "Data"));
            bool[] paid = (bool[])dataType.GetField("bonusPaidByLevel").GetValue(Get(market, "Data"));
            Assert.AreEqual(15, sales.Length); Assert.AreEqual(15, paid.Length);
            sales[11] = 1100; sales[12] = 1200; paid[11] = true;
            object restored = Market(JsonUtility.FromJson(JsonUtility.ToJson(Get(market, "Data")), dataType));
            sales = (long[])dataType.GetField("highestRecognizedSalesByLevel").GetValue(Get(restored, "Data"));
            paid = (bool[])dataType.GetField("bonusPaidByLevel").GetValue(Get(restored, "Data"));
            Assert.AreEqual(1100L, sales[11]); Assert.AreEqual(1200L, sales[12]);
            Assert.IsTrue(paid[11]); Assert.IsFalse(paid[12]);
        }
        [Test] public void ArgentinosLegacyProgressionRepairUsesLosRedondosAsItsPredecessor()
        {
            object balance = Activator.CreateInstance(balanceType), data = Activator.CreateInstance(dataType);
            long[] sales = (long[])dataType.GetField("highestRecognizedSalesByLevel").GetValue(data);
            bool[] paid = (bool[])dataType.GetField("bonusPaidByLevel").GetValue(data);
            sales[10] = long.MaxValue; paid[10] = true;
            object market = Market(data, balance), argentinos = Sim(12);
            long expected = (long)Call(market, "MinimumPurchaseCostCents", argentinos);

            Assert.Greater(expected, 0L);
            Assert.AreEqual(expected, Call(market, "ReconcileLegacyProgressionBalance", argentinos, 12));
            Assert.AreEqual(expected, Get(market, "BalanceCents"));
            Assert.IsTrue((bool)dataType.GetField("legacyTransitionRepairApplied").GetValue(Get(market, "Data")));
        }
        [Test] public void RecipesUseActualCatalogGoalsAndBreadIsShared()
        {
            object market=Market(), sim=Sim(4); int[] req=(int[])Call(market,"RequiredUnits",sim);
            Assert.AreEqual((int)Call(sim,"GetProductGoal",0)+(int)Call(sim,"GetProductGoal",2)+(int)Call(sim,"GetProductGoal",3),req[1]);
            Assert.AreEqual(Call(sim,"GetProductGoal",1),req[3]);
            Assert.AreEqual(req[1],Call(market,"MissingUnits",sim,1));
            Assert.AreEqual(0,req[8],"Fernet ingredients are optional and never required to begin a round.");
            Assert.AreEqual(0,req[9],"Large Coca ingredients are optional and never required to begin a round.");
            Assert.AreEqual(0,Call(market,"MissingPacks",sim,8));
            Assert.AreEqual(0,Call(market,"MissingPacks",sim,9));
            Assert.IsFalse((bool)Call(market,"IsIngredientUnlocked",8,0));
            Assert.IsTrue((bool)Call(market,"IsIngredientUnlocked",8,4));
        }
        [Test] public void CampaignMarketGateUsesExplicitStableIdUnlocksInsteadOfLegacyFrontier()
        {
            object market = Market();
            bool[] unlocked = new bool[13]; unlocked[0] = true; unlocked[1] = true;
            Assert.IsTrue((bool)Call(market, "IsIngredientUnlocked", 0, unlocked, 10));
            Assert.IsFalse((bool)Call(market, "IsIngredientUnlocked", 8, unlocked, 10),
                "A high legacy frontier must not unlock Fernet ingredients after campaign reorder.");
            Assert.IsFalse((bool)Call(market, "IsIngredientUnlocked", 2, unlocked, 10));
            unlocked[12] = true;
            Assert.IsTrue((bool)Call(market, "IsIngredientUnlocked", 8, unlocked, 0),
                "An explicitly unlocked Argentinos ID exposes only its configured catalog.");
        }
        [Test] public void BothDefenseLevelsStartWithoutLegacyQuotaStockWhileTraditionalGateStaysIntact()
        {
            for (int level = 9; level <= 10; level++)
            {
                object data = Activator.CreateInstance(dataType), market = Market(data), defense = Sim(level);
                CollectionAssert.AreEqual(new int[10], (int[])Call(market, "RequiredUnits", defense));
                Assert.IsTrue((bool)Call(market, "HasRequiredStock", defense));
                Assert.IsTrue((bool)Call(market, "BeginAttempt", defense));
                Assert.AreEqual(0, Call(defense, "GetProductGoal", 0));
                Assert.AreEqual(0, Call(defense, "GetProductGoal", 4));
                Assert.AreEqual(0f, Get(defense, "TimeRemaining"));
                Assert.AreEqual(2, Get(defense, "ProductCount"));
                Assert.AreEqual(500, CallStatic(simType, "GetStartingCoinsForLevel", Activator.CreateInstance(balanceType), level));
            }
            object ordinaryMarket = Market(), ordinary = Sim(8);
            Assert.IsFalse((bool)Call(ordinaryMarket, "HasRequiredStock", ordinary));
            Assert.Greater((float)Get(ordinary, "TimeRemaining"), 0f);
            Assert.Greater((int)Call(ordinary, "GetProductGoal", 0), 0);
        }

        [Test] public void VelezDoesNotRequireSandwichIngredientsBeforeStarting()
        {
            object sim = Sim(2), data = Activator.CreateInstance(dataType), market = Market(data);
            int[] required = (int[])Call(market, "RequiredUnits", sim);
            for (int ingredient = 0; ingredient <= 3; ingredient++)
                Assert.AreEqual(0, required[ingredient], "Vélez's inexhaustible Chori/Paty do not need Market stock.");
            Assert.Greater(required[6], 0, "The exception is limited to sandwiches; the other unlocked drink retains its existing gate.");
            dataType.GetField("stockUnits").SetValue(data, (int[])required.Clone());
            market = Market(data);
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim), "No sandwich ingredient stock is required to start Vélez.");
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
        }
        [Test] public void FernetIngredientsAreNotRequiredWhenItsProductIsUnlocked()
        {
            object sim = Sim(4), data = Activator.CreateInstance(dataType), market = Market(data);
            for (int level = 0; level < 11; level++)
            {
                int[] levelRequirements = (int[])Call(market, "RequiredUnits", Sim(level));
                Assert.AreEqual(0, levelRequirements[8], "Fernet ingredient is never required at level " + (level + 1) + ".");
                Assert.AreEqual(0, levelRequirements[9], "Large Coca ingredient is never required at level " + (level + 1) + ".");
            }
            int[] required = (int[])Call(market, "RequiredUnits", sim);
            Assert.Greater(required[6], 0, "Other available catalog products retain their ingredient requirements.");
            dataType.GetField("stockUnits").SetValue(data, (int[])required.Clone());
            market = Market(data);
            Assert.IsTrue((bool)Call(market, "HasRequiredStock", sim), "Zero Fernet ingredients do not block the market gate.");
            Assert.IsTrue((bool)Call(market, "BeginAttempt", sim));
        }
        [Test] public void NewGameKitUsesExactLevelOneRecipeGoalsAndAddsOnlyRequiredIngredients()
        {
            object balance=Activator.CreateInstance(balanceType), data=Activator.CreateInstance(dataType), market=Market(data,balance);
            Call(market,"InitializeNewGameKit");
            Assert.AreEqual(200,Call(market,"Units",0)); Assert.AreEqual(200,Call(market,"Units",1));
            for(int i=2;i<10;i++) Assert.AreEqual(0,Call(market,"Units",i),"No optional ingredient should be included.");
            // The first-level recipe goal remains the authority if its configured amount changes.
            Array levels=(Array)balanceType.GetField("levelBalances",Public).GetValue(balance);
            object first=levels.GetValue(0); int[] goals=(int[])first.GetType().GetField("goalsByProductId",Public).GetValue(first); goals[0]=201;
            data=Activator.CreateInstance(dataType); market=Market(data,balance); Call(market,"InitializeNewGameKit");
            Assert.AreEqual(201,Call(market,"Units",0)); Assert.AreEqual(201,Call(market,"Units",1));
        }

        [Test] public void LevelOneStockFloorFillsOnlyShortagesAndNeverAppliesToLaterLevels()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), levelOne=Sim(0);
            int[] stock=(int[])dataType.GetField("stockUnits").GetValue(data); stock[0]=145; stock[1]=435;
            Assert.IsTrue((bool)Call(market,"CanMeetRequiredStockWithLevelOneFloor",levelOne));
            Assert.IsFalse((bool)Call(market,"HasRequiredStock",levelOne));
            Assert.IsTrue((bool)Call(market,"EnsureLevelOneStartingStock",levelOne));
            Assert.AreEqual(200,Call(market,"Units",0)); Assert.AreEqual(435,Call(market,"Units",1));
            Assert.IsFalse((bool)Call(market,"EnsureLevelOneStartingStock",levelOne),"A repeat entry must not grant beyond the floor.");
            stock[0]=199; stock[1]=50;
            Assert.IsTrue((bool)Call(market,"EnsureLevelOneStartingStock",levelOne));
            Assert.AreEqual(200,Call(market,"Units",0)); Assert.AreEqual(200,Call(market,"Units",1));
            object levelTwo=Sim(1); stock[0]=145; stock[1]=150;
            Assert.IsFalse((bool)Call(market,"CanMeetRequiredStockWithLevelOneFloor",levelTwo));
            Assert.IsFalse((bool)Call(market,"EnsureLevelOneStartingStock",levelTwo));
            Assert.AreEqual(145,Call(market,"Units",0)); Assert.AreEqual(150,Call(market,"Units",1));
        }
        [Test] public void PurchaseRejectsDuplicateFutureIngredientAndNumericOverflow()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), sim=Sim(); Call(market,"Initialize",0);
            dataType.GetField("balanceCents").SetValue(Get(market,"Data"),100000L);
            int before=(int)Call(market,"Units",0);
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,8,1,0,1L));
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,0,int.MaxValue,0,1L));
            Assert.IsTrue((bool)Call(market,"TryBuy",sim,0,1,0,1L));
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,0,1,0,1L));
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
        [Test] public void LossKeepsIncrementalSalesAndEveryVictoryPaysFullFormula()
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
            Assert.AreEqual(3000L,win.GetType().GetField("recognizedSalesCents",Public).GetValue(win));
            Assert.AreEqual(800L,win.GetType().GetField("bonusCents",Public).GetValue(win));
            Assert.AreSame(win,Call(market,"SettleAttempt",replay));
            object later=Sim(); Assert.IsTrue((bool)Call(market,"BeginAttempt",later));
            Set(later,"CoinsEarned",80); Set(later,"Coins",9);
            Set(later,"Phase",Enum.Parse(Type.GetType("HayChoriYPaty.RoundPhase, Assembly-CSharp",true),"Won"));
            object nextWin=Call(market,"SettleAttempt",later);
            Assert.AreEqual(4000L,nextWin.GetType().GetField("recognizedSalesCents",Public).GetValue(nextWin));
            Assert.AreEqual(1800L,nextWin.GetType().GetField("bonusCents",Public).GetValue(nextWin));
        }
        [Test] public void AttemptsRequireReadyAndAllElevenCostsStayFinite()
        {
            object market=Market();
            for(int level=0;level<11;level++)
            {
                object sim=Sim(level); long cost=(long)Call(market,"MinimumPurchaseCostCents",sim);
                if (level == 9) Assert.AreEqual(0L, cost, "Defense has no legacy quota stock requirement.");
                else Assert.Greater(cost,0L, "Traditional level " + level);
                Assert.Less(cost,long.MaxValue);
            }
            object ready=Sim(); Call(market,"Initialize",0); Assert.IsTrue((bool)Call(market,"BeginAttempt",ready));
            Assert.IsFalse((bool)Call(market,"BeginAttempt",ready)); Call(market,"AbortAttempt");
        }
        [Test] public void ReadyCannotSettleAndUnaffordablePurchaseRejectsAtomically()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), sim=Sim(); Call(market,"Initialize",0);
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim)); Assert.IsNull(Call(market,"SettleAttempt",sim));
            Assert.IsTrue((bool)dataType.GetField("attemptActive").GetValue(Get(market,"Data"))); Call(market,"AbortAttempt");
            market=Market(); Call(market,"Initialize",1); sim=Sim(1);
            int[] stock=(int[])dataType.GetField("stockUnits").GetValue(Get(market,"Data")); Array.Clear(stock,0,stock.Length);
            dataType.GetField("balanceCents").SetValue(Get(market,"Data"),0L);
            long op=(long)Get(market,"NextPurchaseId");
            Assert.IsFalse((bool)Call(market,"TryBuy",sim,0,1,1,op));
            Assert.AreEqual(0L,Get(market,"BalanceCents")); Assert.AreEqual(0,Call(market,"Units",0));
            Assert.AreEqual(op,Get(market,"NextPurchaseId"));
            Assert.IsFalse((bool)Call(market,"HasRequiredStock",sim));
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
        [Test] public void FernetIngredientsAreOptionalPurchasableSurplusAndAbortRestoresFiniteStock()
        {
            object sim=Sim(4), market=Market(); Call(market,"Initialize",4);
            object data=Get(market,"Data"); dataType.GetField("balanceCents").SetValue(data,800L);
            int[] stock=(int[])dataType.GetField("stockUnits").GetValue(data);
            int[] required=(int[])Call(market,"RequiredUnits",sim);
            Assert.AreEqual(0,required[8]); Assert.AreEqual(0,required[9]);
            stock[8]=3; stock[9]=2;
            Assert.AreEqual(0,Call(market,"MissingPacks",sim,8));
            Assert.AreEqual(0,Call(market,"MissingUnits",sim,9));
            Assert.AreEqual(0,Call(market,"MissingPacks",sim,9));
            Assert.IsTrue((bool)Call(market,"TryBuy",sim,9,1,4,1L));
            Assert.AreEqual(6,Call(market,"Units",9),"Manual surplus purchases still work even though Fernet does not use these ingredients.");
            stock=(int[])dataType.GetField("stockUnits").GetValue(Get(market,"Data"));
            stock[0]++; stock[1]++; // one actual finite Chori serving beyond its protected minimum
            int stock0Before=stock[0], stock1Before=stock[1];
            Assert.IsTrue((bool)Call(market,"BeginAttempt",sim));
            Assert.IsTrue((bool)Call(market,"TryReserveFood",91,0));
            Assert.IsTrue((bool)Call(market,"ConsumeReservedFood",91,0));
            Assert.AreEqual(stock0Before-1,Call(market,"Units",0));
            Assert.AreEqual(stock1Before-1,Call(market,"Units",1));
            Assert.AreEqual(3,Call(market,"Units",8));
            Assert.AreEqual(6,Call(market,"Units",9));
            object restored=Market(JsonUtility.FromJson(JsonUtility.ToJson(Get(market,"Data")),dataType));
            Assert.IsTrue((bool)Call(restored,"RecoverAbandonedAttempt"));
            Assert.AreEqual(stock0Before,Call(restored,"Units",0));
            Assert.AreEqual(stock1Before,Call(restored,"Units",1));
            Assert.AreEqual(3,Call(restored,"Units",8));
            Assert.AreEqual(6,Call(restored,"Units",9));
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
        [Test] public void LegacyProgressionRepairPaysOnlyTheExactNextLevelStockShortageOnce()
        {
            object balance=Activator.CreateInstance(balanceType), data=Activator.CreateInstance(dataType);
            dataType.GetField("balanceCents").SetValue(data,0L);
            dataType.GetField("cumulativePurchaseSpendCents").SetValue(data,0L);
            dataType.GetField("kitGranted").SetValue(data,true);
            dataType.GetField("initializedUnlockedLevel").SetValue(data,1);
            dataType.GetField("stockUnits").SetValue(data,new[]{0,235,0,0,0,0,85,0,0,0});
            ((long[])dataType.GetField("highestRecognizedSalesByLevel").GetValue(data))[0]=50000L;
            ((bool[])dataType.GetField("bonusPaidByLevel").GetValue(data))[0]=true;
            object market=Market(data,balance), levelTwo=Sim(1);

            Assert.AreEqual(58750L,Call(market,"ReconcileLegacyProgressionBalance",levelTwo,1));
            Assert.AreEqual(58750L,Get(market,"BalanceCents"));
            Assert.AreEqual(0,Call(market,"Units",0)); Assert.AreEqual(235,Call(market,"Units",1)); Assert.AreEqual(85,Call(market,"Units",6));
            Assert.AreEqual(0L,Call(market,"ReconcileLegacyProgressionBalance",levelTwo,1),"Persisted migration marker must prevent a second grant.");
            Assert.AreEqual(58750L,Get(market,"BalanceCents"));
            Assert.IsTrue((bool)Call(market,"TryBuy",levelTwo,0,200,1,1L));
            Assert.IsTrue((bool)Call(market,"TryBuy",levelTwo,6,115,1,2L));
            Assert.AreEqual(0L,Get(market,"BalanceCents")); Assert.IsTrue((bool)Call(market,"HasRequiredStock",levelTwo));
            object persisted=JsonUtility.FromJson(JsonUtility.ToJson(Get(market,"Data")),dataType);
            Assert.IsTrue((bool)dataType.GetField("legacyTransitionRepairApplied").GetValue(persisted));
        }

        [Test] public void LegacyProgressionRepairSkipsCurrentCashAndAnyPriorMarketSpend()
        {
            object data=Activator.CreateInstance(dataType), market=Market(data), levelTwo=Sim(1);
            dataType.GetField("kitGranted").SetValue(data,true); dataType.GetField("initializedUnlockedLevel").SetValue(data,1);
            ((long[])dataType.GetField("highestRecognizedSalesByLevel").GetValue(data))[0]=50000L;
            ((bool[])dataType.GetField("bonusPaidByLevel").GetValue(data))[0]=true;
            dataType.GetField("balanceCents").SetValue(data,1L);
            Assert.AreEqual(0L,Call(market,"ReconcileLegacyProgressionBalance",levelTwo,1));
            dataType.GetField("balanceCents").SetValue(data,0L);
            dataType.GetField("cumulativePurchaseSpendCents").SetValue(data,150L);
            Assert.AreEqual(0L,Call(market,"ReconcileLegacyProgressionBalance",levelTwo,1));
            Assert.IsFalse((bool)dataType.GetField("legacyTransitionRepairApplied").GetValue(data));
        }

        [Test] public void LegacyDebtJsonIsIgnoredAndNotWrittenBack()
        {
            string legacy="{\"balanceCents\":1234,\"debtCents\":987654,\"stockUnits\":[0,0,0,0,0,0,0,0,0,0]}";
            object data=JsonUtility.FromJson(legacy,dataType); object market=Market(data);
            Assert.AreEqual(1234L,Get(market,"BalanceCents"));
            Assert.IsNull(dataType.GetField("debtCents"));
            Assert.IsNull(marketType.GetProperty("DebtCents",Public));
            Assert.IsNull(marketType.GetMethod("FinancingNeededCents",Public));
            Assert.IsFalse(JsonUtility.ToJson(Get(market,"Data")).Contains("debtCents"));
        }
    }
}
