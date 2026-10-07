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
        [TestCase(0,2)] [TestCase(2,3)] [TestCase(3,4)] [TestCase(4,5)] [TestCase(10,5)]
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
        public void CostumePalettePreservesFacesTransparencyAndNonClothPixels()
        {
            Type roles=Runtime("StreetWorkerRole");
            MethodInfo color=Runtime("StreetWorkerAppearance").GetMethod("OutfitColor");
            object premium=Enum.Parse(roles,"ParrilleroPremium"),fernet=Enum.Parse(roles,"Fernetero");
            Color white=new Color(.8f,.8f,.8f,1),red=new Color(.8f,.1f,.1f,1);
            Assert.AreNotEqual(white,color.Invoke(null,new object[]{white,premium,.6f}));
            Assert.AreNotEqual(red,color.Invoke(null,new object[]{red,fernet,.6f}));
            Assert.AreEqual(white,color.Invoke(null,new object[]{white,premium,.15f}));
            Assert.AreEqual(Color.clear,color.Invoke(null,new object[]{Color.clear,premium,.6f}));
            Color skin=new Color(.8f,.55f,.33f,1);
            Assert.AreEqual(skin,color.Invoke(null,new object[]{skin,premium,.6f}));
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
