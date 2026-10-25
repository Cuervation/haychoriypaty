using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetSpecialtyPresentationTests
    {
        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty."+name+", Assembly-CSharp", true);
        private static object Simulation(int level) => Activator.CreateInstance(Runtime("StreetSimulation"),new object[]{Activator.CreateInstance(Runtime("StreetBalance")),level,5f,0,1,0});
        private static object Layout(string name, params object[] args)
        {
            foreach (var m in Runtime("StreetWorkstationLayout").GetMethods(BindingFlags.Static|BindingFlags.Public))
                if (m.Name==name && m.GetParameters().Length==args.Length) return m.Invoke(null,args);
            throw new MissingMethodException(name);
        }
        [TestCase(234f,120f)] [TestCase(234f,104f)] [TestCase(156f,120f)] [TestCase(156f,100f)]
        public void UpgradePortraitsAndSpeedIconAreCenteredInsideCircularMedallionWithoutDistortion(float width,float height)
        {
            Type view=Runtime("StreetView"); const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;
            Rect card=new Rect(24,740,width,height);
            Rect oval=(Rect)view.GetMethod("UpgradeOvalBounds",flags).Invoke(null,new object[]{card});
            foreach(Vector2 source in new[]{new Vector2(1269,1188),new Vector2(266,303),new Vector2(235,328),new Vector2(1174,662)})
            {
                Rect fitted=(Rect)view.GetMethod("UpgradeOvalContentBounds",flags).Invoke(null,new object[]{card,source});
                Assert.That(Vector2.Distance(oval.center,fitted.center),Is.LessThan(.001f));
                Assert.AreEqual(source.x/source.y,fitted.width/fitted.height,.001f);
                foreach(Vector2 corner in new[]{fitted.min,fitted.max,new Vector2(fitted.xMin,fitted.yMax),new Vector2(fitted.xMax,fitted.yMin)})
                {
                    Assert.LessOrEqual(corner.x, oval.xMax - oval.width * .10f + .001f);
                    Assert.GreaterOrEqual(corner.x, oval.xMin + oval.width * .10f - .001f);
                    Assert.LessOrEqual(corner.y, oval.yMax - oval.height * .10f + .001f);
                    Assert.GreaterOrEqual(corner.y, oval.yMin + oval.height * .10f - .001f,
                        "Portrait/speed art stays inside the circular medallion with a clear rim.");
                }
            }
            Rect status=(Rect)view.GetMethod("UpgradeSpeedStatusBounds",flags).Invoke(null,new object[]{card});
            Rect price=(Rect)view.GetMethod("UpgradePriceTextBounds",flags).Invoke(null,new object[]{card});
            Assert.LessOrEqual(status.yMax,price.yMin-3f,"Speed multiplier and +10% must not overlap the price.");
        }
        [TestCase(0,2)] [TestCase(2,3)] [TestCase(3,4)] [TestCase(4,5)] [TestCase(10,6)]
        public void CatalogHudHasOnlyApplicableCardsWithoutOverlap(int level,int count)
        {
            Type view=Runtime("StreetView"); object sim=Simulation(level);
            int[] actions=(int[])view.GetMethod("CatalogUpgradeActions",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{sim});
            Assert.AreEqual(count,actions.Length);
            MethodInfo bounds=view.GetMethod("CatalogUpgradeCardBounds",BindingFlags.NonPublic|BindingFlags.Static);
            for (int i=0;i<actions.Length;i++)
            {
                Rect card=(Rect)bounds.Invoke(null,new object[]{sim,actions[i]});
                Assert.GreaterOrEqual(card.xMin,0);Assert.LessOrEqual(card.xMax,540);
                Assert.GreaterOrEqual(card.yMin,710);Assert.LessOrEqual(card.yMax,960);
                Assert.GreaterOrEqual(card.width,156);Assert.GreaterOrEqual(card.height,100);
                foreach (string slotName in new[]{"UpgradeTitleBounds","UpgradeStatusBounds","UpgradeCoinBounds","UpgradePriceTextBounds"})
                {
                    Rect slot=(Rect)view.GetMethod(slotName,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{card});
                    Assert.IsTrue(card.Contains(slot.min),slotName);Assert.IsTrue(card.Contains(slot.max-Vector2.one*.01f),slotName);
                }
                for(int j=0;j<i;j++) Assert.IsFalse(card.Overlaps((Rect)bounds.Invoke(null,new object[]{sim,actions[j]})));
            }
            if(level==3){Assert.Contains(20,actions);Assert.IsFalse(Array.Exists(actions,a=>a==21));}
            if(level>=4){Assert.Contains(20,actions);Assert.Contains(21,actions);}
        }
        [Test]
        public void ExpandedStationsAndEveryFootRouteStaySeparate()
        {
            Type layout=Runtime("StreetWorkstationLayout");
            Rect normal=(Rect)layout.GetProperty("GrillBounds").GetValue(null);
            Rect premium=(Rect)layout.GetProperty("PremiumGrillBounds").GetValue(null);
            Assert.AreEqual(normal.size,premium.size);Assert.AreEqual(new Vector2(282,94),normal.size);
            Rect[] occupied={normal,premium,(Rect)Layout("BoundsForProduct",0,true),(Rect)Layout("BoundsForProduct",4,true),(Rect)Layout("BoundsForProduct",5,true),(Rect)Layout("BoundsForProduct",6,true)};
            for(int i=0;i<occupied.Length;i++)for(int j=0;j<i;j++)
                Assert.IsTrue((bool)Layout("HasStationClearance",occupied[i],occupied[j]),i+" / "+j);
            Assert.AreNotEqual(Layout("PickupPosition",0,true),Layout("PickupPosition",2,true));
            Assert.AreNotEqual(Layout("ApproachPosition",0,true),Layout("ApproachPosition",2,true));
            for(int product=0;product<7;product++)
            {
                Vector2[] route=(Vector2[])Layout("ApproachRoute",product,true);
                Assert.AreEqual((Vector2)Layout("PickupPosition",product,true),route[route.Length-1]);
                for(int column=0;column<7;column++)
                {
                    Vector2 from=new Vector2(58+column*70,400);
                    foreach(Vector2 to in route)
                    {
                        for(int sample=0;sample<=100;sample++)
                        {
                            Rect feet=(Rect)Layout("WorkerFootBounds",Vector2.Lerp(from,to,sample/100f));
                            foreach(Rect station in occupied)Assert.IsFalse(feet.Overlaps(station),"Product "+product+" feet "+feet+" prop "+station);
                        }
                        from=to;
                    }
                }
            }
        }

        [Test]
        public void ActiveKitchenKeepsFurnitureScaleAndProvidesConnectedRoutesAndSixResponsiveCards()
        {
            int[] products={0,1,2,3,4,5,6};
            Type layoutType=Runtime("StreetKitchenLayout");
            object active=Activator.CreateInstance(layoutType,new object[]{products,true});
            object legacy=Activator.CreateInstance(layoutType,new object[]{products,false});
            Rect normal=(Rect)layoutType.GetProperty("NormalTableBounds").GetValue(active);
            Rect premium=(Rect)layoutType.GetProperty("PremiumTableBounds").GetValue(active);
            Rect legacyNormal=(Rect)layoutType.GetProperty("NormalTableBounds").GetValue(legacy);
            Rect legacyPremium=(Rect)layoutType.GetProperty("PremiumTableBounds").GetValue(legacy);
            Assert.AreEqual(legacyNormal.size,normal.size);Assert.AreEqual(legacyPremium.size,premium.size);
            Assert.AreEqual(legacyNormal.position,normal.position);Assert.AreEqual(legacyPremium.position,premium.position);
            Assert.AreEqual((float)layoutType.GetField("ActivePlayfieldBottom").GetValue(null),
                (float)layoutType.GetProperty("PlayfieldBottom").GetValue(active));
            Rect grill=(Rect)layoutType.GetMethod("GrillBoundsForProduct").Invoke(active,new object[]{0});
            Assert.AreEqual(70f,grill.yMin-normal.yMax,.01f,"Expanded walk-through gap");

            Vector2 home=new Vector2(433,(float)Runtime("StreetSceneLayout").GetField("WorkerServiceY").GetRawConstantValue());
            MethodInfo route=layoutType.GetMethod("FindShortestSafeRoute");
            MethodInfo work=layoutType.GetMethod("GrillWorkPosition");
            MethodInfo deposit=layoutType.GetMethod("DepositPosition");
            // The restored original prep row separates production from service. Use the real
            // hired cooks' grill-side starts, not the historical counter-side delivery home.
            object cookSim=Simulation(7);
            cookSim.GetType().GetMethod("StartRound").Invoke(cookSim,null);
            Type roleType=Runtime("StreetWorkerRole");
            Assert.IsTrue((bool)cookSim.GetType().GetMethod("TryHire",new[]{roleType}).Invoke(cookSim,new[]{Enum.Parse(roleType,"ParrilleroPremium")}));
            var cooks=(System.Collections.IList)cookSim.GetType().GetProperty("Workers").GetValue(cookSim);
            Vector2 normalHome=(Vector2)cooks[0].GetType().GetProperty("Position").GetValue(cooks[0]);
            Vector2 premiumHome=(Vector2)cooks[1].GetType().GetProperty("Position").GetValue(cooks[1]);
            for(int product=0;product<4;product++)
            foreach(int workerCount in new[]{1,2,3,4})
            for(int workerIndex=0;workerIndex<workerCount;workerIndex++)
            {
                Vector2 grillFeet=(Vector2)work.Invoke(active,new object[]{product,workerIndex,workerCount});
                Vector2 tableFeet=(Vector2)deposit.Invoke(active,new object[]{product});
                Assert.NotNull(route.Invoke(active,new object[]{product<2?normalHome:premiumHome,grillFeet}),"Home→grill product "+product+" worker "+workerIndex+"/"+workerCount);
                Assert.NotNull(route.Invoke(active,new object[]{grillFeet,tableFeet}),"Grill→deposit product "+product+" worker "+workerIndex+"/"+workerCount);
                Assert.NotNull(route.Invoke(active,new object[]{tableFeet,grillFeet}),"Deposit→grill product "+product+" worker "+workerIndex+"/"+workerCount);
            }
            for(int product=0;product<7;product++)
            {
                Vector2 pickup=(Vector2)layoutType.GetMethod("PickupPosition").Invoke(active,new object[]{product});
                Assert.NotNull(route.Invoke(active,new object[]{home,pickup}),"Home→pickup product "+product);
            }

            Type view=Runtime("StreetView");
            object activeSim=Simulation(10);
            MethodInfo responsive=view.GetMethod("BuildResponsiveFooterLayout",BindingFlags.NonPublic|BindingFlags.Static);
            foreach(float height in new[]{700f,810f,960f,1200f})
            {
                object footer=responsive.Invoke(null,new object[]{activeSim,height,true,false});
                Rect[] cards=(Rect[])footer.GetType().GetField("UpgradeBounds").GetValue(footer);
                Assert.AreEqual(6,cards.Length);
                float compact=Mathf.Min(1f,height/960f);
                Rect canvas=new Rect(0,0,540,height);
                Rect[] displayed=new Rect[cards.Length];
                for(int i=0;i<cards.Length;i++)
                {
                    displayed[i]=new Rect(cards[i].x+(cards[i].width*(1f-compact))*.5f,cards[i].y*(height/960f),cards[i].width*compact,cards[i].height*compact);
                    Assert.IsTrue(canvas.Contains(displayed[i].min),"height "+height+" card "+i+" min "+displayed[i]);
                    Assert.IsTrue(canvas.Contains(displayed[i].max-Vector2.one*.01f),"height "+height+" card "+i+" max "+displayed[i]);
                    for(int j=0;j<i;j++)Assert.IsFalse(displayed[i].Overlaps(displayed[j]),"height "+height+" cards "+i+"/"+j);
                }
            }
            MethodInfo withInset=view.GetMethod("BuildResponsiveFooterLayoutWithBottomBleed",BindingFlags.NonPublic|BindingFlags.Static);
            float safeHeight=810f,bottomBleed=42f,scale=Mathf.Min(1f,safeHeight/960f);
            object insetFooter=withInset.Invoke(null,new object[]{activeSim,safeHeight,true,false,bottomBleed});
            Rect[] insetCards=(Rect[])insetFooter.GetType().GetField("UpgradeBounds").GetValue(insetFooter);
            Rect insetCanvas=new Rect(0,0,540,safeHeight+bottomBleed);
            foreach(Rect card in insetCards)
            {
                Rect displayed=new Rect(card.x+(card.width*(1f-scale))*.5f,card.y*(safeHeight/960f),card.width*scale,card.height*scale);
                Assert.IsTrue(insetCanvas.Contains(displayed.min));
                Assert.IsTrue(insetCanvas.Contains(displayed.max-Vector2.one*.01f));
            }
        }
        [Test]
        public void CostumePalettePreservesFacesTransparencyAndNonClothPixels()
        {
            Type roles=Runtime("StreetWorkerRole");
            MethodInfo color=Runtime("StreetWorkerAppearance").GetMethod("OutfitColor");
            object premium=Enum.Parse(roles,"ParrilleroPremium"),fernet=Enum.Parse(roles,"Fernetero"),mozo=Enum.Parse(roles,"Mozo");
            Color white=new Color(.8f,.8f,.8f,1),red=new Color(.8f,.1f,.1f,1);
            Assert.AreNotEqual(white,color.Invoke(null,new object[]{white,premium,.6f}));
            Color warmApron=new Color(.9f,.84f,.74f,1f);
            Assert.AreNotEqual(warmApron,color.Invoke(null,new object[]{warmApron,premium,.6f}),"Warm off-white apron must remain visibly Premium blue");
            Assert.AreNotEqual(red,color.Invoke(null,new object[]{red,fernet,.6f}));
            Assert.AreEqual(white,color.Invoke(null,new object[]{white,premium,.15f}));
            Assert.AreEqual(Color.clear,color.Invoke(null,new object[]{Color.clear,premium,.6f}));
            Color skin=new Color(.8f,.55f,.33f,1);
            Assert.AreEqual(skin,color.Invoke(null,new object[]{skin,premium,.6f}));
            Assert.AreNotEqual(white,color.Invoke(null,new object[]{white,mozo,.6f}),"Mozo apron should use the adapted green outfit");
            Assert.AreEqual(skin,color.Invoke(null,new object[]{skin,mozo,.6f}),"Mozo palette must preserve face/skin pixels");
            Assert.AreEqual(Color.clear,color.Invoke(null,new object[]{Color.clear,mozo,.6f}),"Mozo palette must preserve transparent atlas padding");
        }
        [Test]
        public void PremiumGrillAssetIsTransparentAndRetainsFullResolution()
        {
            Texture2D art=Resources.Load<Texture2D>("street-grill-premium-v1");
            Assert.NotNull(art);Assert.AreEqual(2172,art.width);Assert.AreEqual(724,art.height);
            var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Art/Street/Resources/street-grill-premium-v1.png");
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());Assert.IsTrue(importer.alphaIsTransparency);Assert.IsFalse(importer.mipmapEnabled);
        }
    }
}
