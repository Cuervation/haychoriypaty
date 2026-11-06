using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    // Fast structural/invariant tests only. Full balance searches are manual Editor diagnostic stages.
    [Category("AuditUnit")]
    public sealed class GameplayBalanceAuditTests
    {
        static Type Runner => Type.GetType("GameplayBalanceAudit, Assembly-CSharp-Editor",true);
        static object Invoke(string method,params object[] args) => Runner.GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
        static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        [Test] public void SerializedSceneHasCurrentActiveCookingAndRoleRates()
        {
            object b=Invoke("LoadEffective");Type sim=Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp",true);
            for(int level=0;level<11;level++){
                object s=Activator.CreateInstance(sim,new object[]{b,level,5f,0,1,0});
                Assert.AreEqual(level>=5,sim.GetProperty("UsesActiveCooks").GetValue(s));
                Assert.AreEqual(1f,sim.GetProperty("CookWorkRate").GetValue(s));
                Assert.AreEqual(1f,sim.GetProperty("WorkRate").GetValue(s));
            }
        }
        [Test] public void InvariantCheckUsesTrueTerminalPhaseAndPreservesBalance()
        { StringAssert.StartsWith("PASS",(string)Invoke("SelfCheck")); }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)]
        public void EachLevelDiagnosticHonorsPhysicalGoalsAndRealMoney(int level)
        {
            object b=Invoke("LoadEffective");Runner.GetField("effective",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,b);
            var plan=Activator.CreateInstance(Runner.GetNestedType("Plan"));plan.GetType().GetField("name").SetValue(plan,"unit_no_purchases");plan.GetType().GetField("mode").SetValue(plan,9);
            object r=Invoke("Isolated",level,1001,plan,"current");
            Assert.Contains((string)Field(r,"phase"),new[]{"Won","Lost"});
            Assert.AreEqual((int)Field(r,"initialCoins")+(int)Field(r,"earned")-(int)Field(r,"spent"),Field(r,"cash"));
            Assert.AreEqual(0,Field(r,"spent"));Assert.AreEqual(0,Field(r,"speed"));
            int[] goals=(int[])Field(r,"goals"),sold=(int[])Field(r,"delivered"),initial=(int[])Field(r,"initialTable"),deposited=(int[])Field(r,"deposited");
            bool all=true;for(int p=0;p<7;p++){Assert.LessOrEqual(sold[p],initial[p]+deposited[p]);if(sold[p]<goals[p])all=false;}
            Assert.AreEqual(all,Field(r,"goalReached"));Assert.AreEqual(all,Field(r,"won"));
            foreach(int stock in (int[])Field(r,"ingredientsAfter"))Assert.GreaterOrEqual(stock,0);
        }
    }
}
