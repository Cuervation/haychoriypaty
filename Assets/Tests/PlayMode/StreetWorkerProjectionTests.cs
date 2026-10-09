using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetWorkerProjectionTests
    {
        private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Type T(string name) => Type.GetType("HayChoriYPaty."+name+", Assembly-CSharp",true);
        private static object Get(object obj,string name) => obj.GetType().GetProperty(name,Instance).GetValue(obj);
        private static object Field(object obj,string name) => obj.GetType().GetField(name,Instance).GetValue(obj);
        private static void Set(object obj,string name,object value) => obj.GetType().GetProperty(name,Instance).GetSetMethod(true).Invoke(obj,new[]{value});
        private static object Call(object obj,string name,params object[] args)
        {
            foreach (MethodInfo method in obj.GetType().GetMethods(Instance))
                if (method.Name==name && method.GetParameters().Length==args.Length) return method.Invoke(obj,args);
            throw new MissingMethodException(name);
        }
        [UnityTest]
        public IEnumerator NativeWorkersHaveFourGaitsAndPhysicallyHoldEveryProductAtBothPortraitScales()
        {
            var root=new GameObject("Native worker acceptance");
            try
            {
                object sim=Activator.CreateInstance(T("StreetSimulation"),new object[]{Activator.CreateInstance(T("StreetBalance")),4,5f,0,1,0});
                Call(sim,"StartRound");Set(sim,"Coins",10000);
                foreach(string role in new[]{"Parrillero","ParrilleroPremium","Cocacolero","Fernetero"})
                    Assert.IsTrue((bool)Call(sim,"TryHire",Enum.Parse(T("StreetWorkerRole"),role)));
                var workers=(IList)Get(sim,"Workers");
                Component renderer=root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal=Resources.Load<Texture2D>("street-parrillero"), diagonal=Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage=Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                object kitchen=Get(sim,"Kitchen");
                var objects=(IDictionary)Field(renderer,"foodObjects");
                Vector2[] directions={new Vector2(0,1),new Vector2(-1,1),new Vector2(-1,0),new Vector2(-1,-1),new Vector2(0,-1),new Vector2(1,-1),new Vector2(1,0),new Vector2(1,1)};
                foreach(float scale in new[]{1f,1.25f})
                {
                    float height=960f*scale;
                    Call(renderer,"Prepare",sim,scale,height);
                    Call(renderer,"PrepareWorkers",normal,diagonal,normal,diagonal,beverage,beverage,true);
                    Assert.IsTrue((bool)Get(renderer,"WorkersReady"));
                    Assert.IsNull(Get(renderer,"CarriedTexture"),"Do not draw a duplicate floating overlay");
                    Assert.IsFalse(((Camera)Field(renderer,"carryCamera")).enabled,"Reuse the existing kitchen pass; no extra worker camera");
                    object projection=Field(renderer,"workerProjection");
                    var views=(IDictionary)Field(projection,"workers");
                    Assert.AreEqual(workers.Count,views.Count);
                    for(int product=0;product<7;product++)
                    {
                        string role=product<2?"Parrillero":product<4?"ParrilleroPremium":product==5?"Fernetero":"Cocacolero";
                        object worker=null;foreach(object candidate in workers)if(Get(candidate,"Role").ToString()==role){worker=candidate;break;}
                        int workerId=(int)Get(worker,"Id");
                        object unit=Call(kitchen,"TryTake",product,Get(worker,"Role"),workerId);Assert.NotNull(unit);
                        int itemId=(int)Get(unit,"Id");
                        Set(worker,"CarriedItemId",itemId);Set(worker,"Product",product);Set(worker,"State",Enum.Parse(T("StreetWorkerState"),"ToCounter"));
                        Set(worker,"Position",new Vector2(250,420));Set(worker,"StepDistance",1f);
                        object view=views[workerId];
                        GameObject physicalItem=(GameObject)objects[itemId];
                        for(int direction=0;direction<8;direction++)
                        {
                            var distinct=new HashSet<string>();
                            Set(worker,"FacingVector",directions[direction]);
                            for(int phase=0;phase<4;phase++)
                            {
                                Set(worker,"TravelDistance",phase*12f);
                                Call(renderer,"PrepareWorkers",normal,diagonal,normal,diagonal,beverage,beverage,true);
                                GameObject item=(GameObject)objects[itemId];
                                var sr=item.GetComponentInChildren<SpriteRenderer>();
                                Vector2 palm=(Vector2)Field(view,"Palm");
                                Assert.AreEqual((palm.x-270f)*.01f,sr.bounds.center.x,.0002f);
                                Assert.AreEqual((height*.5f-palm.y)*.01f,sr.bounds.min.y,.0002f,"The physical product bottom must touch its supporting hand");
                                Assert.AreEqual(30,item.layer);
                                var torso=(SpriteRenderer)Field(view,"Torso");var left=(SpriteRenderer)Field(view,"LeftLeg");var right=(SpriteRenderer)Field(view,"RightLeg");
                                Assert.Greater(torso.transform.localScale.x,0f);Assert.AreEqual(torso.transform.localScale.x,torso.transform.localScale.y,.0001f,"Never distort body proportions");
                                distinct.Add(left.sprite.GetHashCode()+":"+left.transform.localPosition.ToString("F4")+":"+right.sprite.GetHashCode()+":"+right.transform.localPosition.ToString("F4"));
                                bool rear=direction==3||direction==4||direction==5;
                                Assert.IsTrue(rear?sr.sortingOrder<torso.sortingOrder:sr.sortingOrder>torso.sortingOrder);
                                if(!rear)Assert.Greater(((SpriteRenderer)Field(view,"Fingers")).sortingOrder,sr.sortingOrder);
                                Assert.AreSame(physicalItem,item,"Direction/frame changes must not replace the real carried unit object");
                            }
                            Assert.AreEqual(4,distinct.Count,"Each direction needs four distinct contact/passing gait samples");
                        }
                        GameObject consumed=(GameObject)objects[itemId];
                        Assert.IsTrue((bool)Call(kitchen,"Consume",itemId,product,workerId));Set(worker,"CarriedItemId",0);
                        Call(renderer,"Prepare",sim,scale,height);
                        Assert.IsFalse(objects.Contains(itemId));yield return null;Assert.IsTrue(consumed==null);
                    }
                    // Stock is ample for one consumed unit of each product per portrait scale.
                }
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlappingIdleWorkersAreHiddenUntilTheyBecomeActive()
        {
            var root = new GameObject("Idle worker overlap acceptance");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { Activator.CreateInstance(T("StreetBalance")), 4, 5f, 0, 1, 0 });
                Call(sim, "StartRound"); Set(sim, "Coins", 100000);
                foreach (string role in new[] { "Parrillero", "Cocacolero", "ParrilleroPremium", "Fernetero" })
                    Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), role)));
                var workers = (IList)Get(sim, "Workers");
                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Call(renderer, "Prepare", sim, 1f, 960f);
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                Assert.IsTrue((bool)Get(renderer, "WorkersReady"));
                object projection = Field(renderer, "workerProjection");
                var views = (IDictionary)Field(projection, "workers");
                Assert.AreEqual(5, views.Count, "All purchased workers remain represented by independent visual objects.");
                Assert.AreEqual(1, CountActive(views), "Only one identical idle pose should be visible at the shared home point.");

                object dispatched = workers[1];
                Set(dispatched, "State", Enum.Parse(T("StreetWorkerState"), "ToStation"));
                Set(dispatched, "Position", new Vector2(300f, 450f));
                Set(dispatched, "Target", new Vector2(330f, 430f));
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                Assert.AreEqual(2, CountActive(views), "A dispatched worker becomes visible without shifting its route.");
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        private static int CountActive(IDictionary views)
        {
            int count = 0;
            foreach (DictionaryEntry entry in views)
            {
                Transform root = (Transform)entry.Value.GetType().GetField("Root", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(entry.Value);
                if (root.gameObject.activeSelf) count++;
            }
            return count;
        }
    }
}
