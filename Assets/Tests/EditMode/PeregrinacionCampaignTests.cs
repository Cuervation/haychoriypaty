using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class PeregrinacionCampaignTests
    {
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Type T(string n) => Type.GetType("HayChoriYPaty." + n + ", Assembly-CSharp", true);
        static object Field(object x, string n) => x.GetType().GetField(n, I).GetValue(x);
        static void Set(object x, string n, object v) => x.GetType().GetField(n, I).SetValue(x, v);
        static object Migrate(object save) => T("StreetGame").GetMethod("MigrateSaveData", S).Invoke(null, new[] {save});
        static object V8(bool later = false)
        {
            object s = Activator.CreateInstance(T("StreetGame").GetNestedType("SaveData"));
            Set(s, "version", 8); var bits = new bool[14]; bits[0] = bits[13] = true; bits[4] = later;
            Set(s, "unlockedLevelIds", bits); Set(s, "unlockedLevel", 10);
            return s;
        }
        [Test]
        public void CampaignHasFifteenStableIdsAndThirteenFourteenFourSuccessors()
        {
            CollectionAssert.AreEqual(new[] {0,1,9,2,12,3,11,10,13,14,4,5,6,7,8},
                (int[])T("StreetSimulation").GetField("CampaignOrder", S).GetValue(null));
            foreach (var pair in new[] {new[] {13,14}, new[] {14,4}})
                Assert.AreEqual(pair[1], T("StreetSimulation").GetMethod("NextCampaignLevelId", S).Invoke(null, new object[] {pair[0]}));
            string[] names = (string[])T("StreetSimulation").GetField("LevelNames", S).GetValue(null);
            Assert.AreEqual(15, names.Length); Assert.AreEqual("Peregrinación a Luján — Moreno", names[14]);
        }
        [Test]
        public void V8MigrationPreservesEveryBitAndDoesNotInferWinFromLegacyFrontier()
        {
            object old = V8(); bool[] before = (bool[])((bool[])Field(old,"unlockedLevelIds")).Clone();
            Set(old,"price",13f); Set(old,"activeAttemptId","retain-active-attempt");
            object result = Migrate(old); var after = (bool[])Field(result,"unlockedLevelIds");
            Assert.AreEqual(9, Field(result,"version")); Assert.AreEqual(15,after.Length);
            for (int i=0;i<14;i++) Assert.AreEqual(before[i],after[i]);
            Assert.IsFalse(after[14],"Access to La Renga or legacy frontier10 is not proof of its victory.");
            Assert.AreEqual(13f,Field(result,"price")); Assert.AreEqual("retain-active-attempt",Field(result,"activeAttemptId"));
            Assert.AreSame(result,Migrate(result));
            Assert.IsTrue(((bool[])Field(Migrate(V8(true)),"unlockedLevelIds"))[14]);
        }
        [Test]
        public void V8MigrationRecognizesRengaVictoryAndPreservesStatistics()
        {
            object old = V8(); object stats = Activator.CreateInstance(T("StreetPlayerStatistics"));
            long[] wins = new long[14]; wins[13]=3; wins[2]=7; Set(stats,"winsByLevel",wins); Set(old,"statistics",stats);
            object result=Migrate(old); Assert.IsTrue(((bool[])Field(result,"unlockedLevelIds"))[14]);
            CollectionAssert.AreEqual(wins,(long[])Field(Field(result,"statistics"),"winsByLevel"));
        }
        [TestCase(7,13)] [TestCase(8,14)] [TestCase(9,15)]
        public void SnapshotShapeIsVersionedNotCurrentCampaignLength(int version,int count)
        {
            object save=V8(); Set(save,"version",version);var bits=new bool[count];bits[0]=true;Set(save,"unlockedLevelIds",bits);
            MethodInfo validate=T("Cloud.PlayerLocalRepository").GetMethod("ValidateSnapshot",S);
            Assert.DoesNotThrow(()=>validate.Invoke(null,new object[]{JsonUtility.ToJson(save)}));
            Set(save,"unlockedLevelIds",new bool[count+1]);
            Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{JsonUtility.ToJson(save)}));
        }
        [Test]
        public void PeregrinacionThemeAndConfigOwnTheirIdentityAndSevenProducts()
        {
            object config=Resources.Load("TowerDefense/Peregrinacion-LujanMoreno",T("TowerDefenseLevelConfig"));
            Assert.NotNull(config);
            Func<string,object> property=n=>config.GetType().GetProperty(n,I).GetValue(config);
            Assert.AreEqual(14,property("SourceCampaignLevelId")); Assert.AreEqual(true,property("FreeConstruction"));
            Assert.AreEqual(8,property("EconomicBalanceCampaignLevelId")); Assert.AreEqual(true,property("UseCampaignEconomyPricing"));
            Assert.AreEqual(1200,property("StartingCoins"),"The level keeps its own opening cash.");
            Assert.AreEqual(false,property("EquipmentPlacementAllowed"),"Every Luján station is preinstalled and equipment cannot be placed or purchased.");
            CollectionAssert.AreEqual(new[]{1,0,0,0,1},(int[])property("StartingWorkersByRole"));
            Array startingStations=(Array)property("StartingStations");
            Assert.AreEqual(7,startingStations.Length,"Luján starts with one real instance of every station type.");
            CollectionAssert.AreEqual(new[]{0,1,2,3,4,5,6},startingStations.Cast<object>()
                .Select(station=>Convert.ToInt32(station.GetType().GetField("Kind").GetValue(station))).ToArray());
            MethodInfo hireCost=config.GetType().GetMethod("HireCostForRole",I);
            CollectionAssert.AreEqual(new[]{55,55,70,65,65},new[]{0,1,2,3,4}.Select(i=>(int)hireCost.Invoke(config,new[]{Enum.ToObject(T("StreetWorkerRole"),i)})).ToArray());
            CollectionAssert.AreEquivalent(new[]{0,1,2,3,4,5,6},(IEnumerable)property("AvailableProducts"));
            Assert.AreEqual("street-background-peregrinacion-moreno-v4",property("BackgroundResource"));
            Assert.NotNull(Resources.Load<Texture2D>((string)property("BackgroundResource")));
            object theme=T("ClubVisualTheme").GetMethod("ForLevel",S).Invoke(null,new object[]{14});
            Assert.AreEqual(property("BackgroundResource"),theme.GetType().GetProperty("MuralResource").GetValue(theme));
            Assert.NotNull(Resources.Load<Texture2D>("street-peregrinacion-fans-walk-v1"));
            Assert.NotNull(Resources.Load<Texture2D>("street-peregrinacion-fans-front-v1"));
            Assert.NotNull(Resources.Load<Texture2D>("street-peregrinacion-crest-v1"));
        }
    }
}
