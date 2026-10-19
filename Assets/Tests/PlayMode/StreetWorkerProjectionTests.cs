using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private static void SaveProjectionCapture(object renderer, string name)
        {
            RenderTexture target = (RenderTexture)Get(renderer, "BackgroundTexture");
            Assert.IsNotNull(target, "The native kitchen camera must have a render target.");
            Assert.IsTrue(target.IsCreated(), "The native kitchen camera target must be created.");
            RenderTexture previous = RenderTexture.active;
            var capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                capture.Apply(false, false);
                string path = Path.Combine(Path.GetTempPath(), "hay-chori-" + name + ".png");
                File.WriteAllBytes(path, capture.EncodeToPNG());
                Debug.Log("Native Unity kitchen-camera capture saved to " + path + " (" + target.width + "x" + target.height + ")");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(capture);
            }
        }
        private static object Call(object obj,string name,params object[] args)
        {
            foreach (MethodInfo method in obj.GetType().GetMethods(Instance))
                if (method.Name==name && method.GetParameters().Length==args.Length) return method.Invoke(obj,args);
            throw new MissingMethodException(name);
        }
        [UnityTest]
        public IEnumerator RealCooksApproachTheirReservedFoodWithoutMovingItOrOverlappingPosts()
        {
            var root = new GameObject("Real cook slot reach acceptance");
            try
            {
                object balance = Activator.CreateInstance(T("StreetBalance"));
                T("StreetBalance").GetField("customerArrivalSeconds", Instance).SetValue(balance, 10000f);
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { balance, 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound"); Set(sim, "Coins", 100000);
                foreach (string role in new[] { "Parrillero", "Parrillero", "ParrilleroPremium" })
                    Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), role)));

                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                object kitchen = Get(sim, "Kitchen");
                object layout = Get(sim, "KitchenLayout");
                var seenRoles = new HashSet<string>();
                for (int tick = 0; tick < 160 && seenRoles.Count < 2; tick++)
                {
                    Call(sim, "Step", .05f);
                    IList workers = (IList)Get(sim, "Workers");
                    var workingPosts = new List<Vector2>();
                    foreach (object worker in workers)
                    {
                        if (Get(worker, "State").ToString() != "Cooking") continue;
                        int itemId = (int)Get(worker, "CookingItemId");
                        object unit = Call(kitchen, "Find", itemId);
                        Assert.IsNotNull(unit, "A cooking worker must own one real grill unit.");
                        Assert.AreEqual("Grill", Get(unit, "Location").ToString());
                        int product = (int)Get(unit, "Product"), slot = (int)Get(unit, "Slot");
                        int capacity = (int)Call(kitchen, "GrillCapacity", product);
                        bool companion = (bool)Call(sim, "IsProductAvailable", product ^ 1);
                        Vector2 food = (Vector2)Call(layout, "GrillSlotPosition", product, slot, companion, capacity);
                        Vector2 expectedPost = (Vector2)Call(layout, "GrillSlotWorkPosition", product, food);
                        Vector2 actualPost = (Vector2)Get(worker, "Position");
                        Assert.That(Vector2.Distance(expectedPost, actualPost), Is.LessThan(.01f), "The cook should stand at the post aligned with its own reserved item.");
                        Assert.That(Vector2.Distance(actualPost, food), Is.LessThan(40f), "The meat must be within the integrated pose's natural reach.");
                        Assert.IsFalse((bool)Call(kitchen, "IsGrillSlotFree", product, slot), "The physical item stays reserved in its original grill slot.");
                        foreach (Vector2 otherPost in workingPosts)
                            Assert.That(Vector2.Distance(actualPost, otherPost), Is.GreaterThanOrEqualTo(84f), "Concurrent full-body cooking poses need a clear horizontal gap.");
                        workingPosts.Add(actualPost);
                        seenRoles.Add(Get(worker, "Role").ToString());
                    }
                    if (seenRoles.Contains("Parrillero") && seenRoles.Contains("ParrilleroPremium"))
                    {
                        Call(renderer, "Prepare", sim, 1f, 960f);
                        Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                        object projection = Field(renderer, "workerProjection");
                        var views = (IDictionary)Field(projection, "workers");
                        var cookingBounds = new List<Bounds>();
                        var cookingLabels = new List<string>();
                        foreach (object worker in (IList)Get(sim, "Workers"))
                        {
                            if (Get(worker, "State").ToString() != "Cooking") continue;
                            var view = views[(int)Get(worker, "Id")];
                            var pose = (SpriteRenderer)Field(view, "CookingPose");
                            cookingBounds.Add(pose.bounds);
                            cookingLabels.Add(Get(worker, "Role") + "#" + Get(worker, "Id"));
                        }
                        for (int i = 0; i < cookingBounds.Count; i++)
                            for (int j = i + 1; j < cookingBounds.Count; j++)
                                Assert.IsFalse(cookingBounds[i].Intersects(cookingBounds[j]),
                                    cookingLabels[i] + " and " + cookingLabels[j] + " must not overlap as rendered full-body cooking poses.");
                        yield return null;
                        yield return null;
                        SaveProjectionCapture(renderer, "parrillero-real-cooking-slots-960");
                        break;
                    }
                    yield return null;
                }
                Assert.IsTrue(seenRoles.Contains("Parrillero"), "A normal cook should resume real cooking.");
                Assert.IsTrue(seenRoles.Contains("ParrilleroPremium"), "A Premium cook should resume real cooking.");
            }
            finally { UnityEngine.Object.Destroy(root); }
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
                        if(product==4||product==6)
                        {
                            Vector2 pickup=(Vector2)Call(Get(sim,"KitchenLayout"),"PickupPosition",product);
                            Assert.AreEqual(pickup,Call(Get(sim,"KitchenLayout"),"BarrelPickupPosition",product));
                            Set(worker,"Product",product);Set(worker,"State",Enum.Parse(T("StreetWorkerState"),"Pickup"));
                            Set(worker,"Position",pickup);Set(worker,"Target",pickup);Set(worker,"StepDistance",0f);
                            Set(worker,"CarriedItemId",0);Set(worker,"PickupProgress",0f);
                            object workerVisual=views[workerId];
                            var torso=(SpriteRenderer)Field(workerVisual,"Torso");
                            Call(renderer,"PrepareWorkers",normal,diagonal,normal,diagonal,beverage,beverage,true);
                            float idleReachY=torso.transform.localPosition.y;
                            Vector2 idlePalm=(Vector2)Field(workerVisual,"Palm");
                            GameObject carryRoot=(GameObject)Field(renderer,"carryRoot");
                            foreach(DictionaryEntry entry in objects)
                            {
                                GameObject existing=(GameObject)entry.Value;
                                Assert.IsFalse(existing!=null&&existing.transform.parent==carryRoot.transform,
                                    "A barrel drink must remain at its station until TryTake succeeds.");
                            }
                            Set(worker,"PickupProgress",.5f);
                            Call(renderer,"PrepareWorkers",normal,diagonal,normal,diagonal,beverage,beverage,true);
                            Assert.That(idleReachY-torso.transform.localPosition.y, Is.EqualTo(0.196f).Within(.002f),
                                "At peak dwell, the empty-handed torso must reach 20% of the authored worker height toward the barrel.");
                            Vector2 reachingPalm=(Vector2)Field(workerVisual,"Palm");
                            Assert.That(reachingPalm.y-idlePalm.y, Is.EqualTo(19.6f).Within(.02f),
                                "The hand anchor must follow the visible reach instead of staying at the carry pose.");
                        }
                        object unit=Call(kitchen,"TryTake",product,Get(worker,"Role"),workerId);Assert.NotNull(unit);
                        int itemId=(int)Get(unit,"Id");
                        Set(worker,"CarriedItemId",itemId);Set(worker,"Product",product);Set(worker,"State",Enum.Parse(T("StreetWorkerState"),"ToCounter"));
                        Set(worker,"Position",new Vector2(250,420));Set(worker,"StepDistance",1f);
                        object view=views[workerId];
                        GameObject physicalItem=(GameObject)objects[itemId];
                        if (product == 0 && scale == 1f)
                        {
                            yield return null;
                            yield return null;
                            SaveProjectionCapture(renderer, "parrillero-real-carry-960");
                        }
                        for(int direction=0;direction<8;direction++)
                        {
                            var distinct=new HashSet<string>();
                            Set(worker,"FacingVector",directions[direction]);
                            for(int phase=0;phase<4;phase++)
                            {
                                Set(worker,"TravelDistance",phase*20f);
                                Call(renderer,"PrepareWorkers",normal,diagonal,normal,diagonal,beverage,beverage,true);
                                GameObject item=(GameObject)objects[itemId];
                                Assert.AreSame(((GameObject)Field(renderer,"carryRoot")).transform,item.transform.parent,
                                    "The real unit enters the hand/carry projection only after station withdrawal.");
                                var sr=item.GetComponentInChildren<SpriteRenderer>();
                                Vector2 palm=(Vector2)Field(view,"Palm");
                                Assert.AreEqual((palm.x-270f)*.01f,sr.bounds.center.x,.0002f);
                                Assert.AreEqual((height*.5f-palm.y)*.01f,sr.bounds.min.y,.0002f,"The physical product bottom must touch its supporting hand");
                                Assert.That(palm.y - (((Vector2)Get(worker, "Position")).y * scale), Is.InRange(-37f, -24f),
                                    "Every carried sandwich stays around hand/waist height instead of the face.");
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
        public IEnumerator CookingPosesReplaceOriginalBodyAndStayAnchoredForOneToFourCooksAtTwoPortraitScales()
        {
            var root = new GameObject("Integrated cooking upper-pose projection test");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { Activator.CreateInstance(T("StreetBalance")), 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound"); Set(sim, "Coins", 100000);
                string[] hireRoles = { "Parrillero", "ParrilleroPremium", "Parrillero", "ParrilleroPremium" };
                foreach (string hireRole in hireRoles)
                    Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), hireRole)));
                IList workers = (IList)Get(sim, "Workers");
                var allWorkers = new List<object>();
                foreach (object worker in workers) allWorkers.Add(worker);
                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Texture2D sourceCookingAtlas = Resources.Load<Texture2D>("street-parrillero-cooking-arms-v1");
                Assert.IsNotNull(sourceCookingAtlas, "The integrated four-pose cooking atlas must load from Resources.");
                object layout = Get(sim, "KitchenLayout");
                MethodInfo cookingFrameForProgress = T("StreetWorkerAnimation")
                    .GetMethod("CookingFrameForProgress", BindingFlags.Public | BindingFlags.Static);
                MethodInfo cookingPoseFootInset = T("StreetWorkerProjection")
                    .GetMethod("CookingPoseFootInset", BindingFlags.NonPublic | BindingFlags.Static);

                foreach (float scale in new[] { 1f, 1.25f })
                {
                    float canvasHeight = 960f * scale;
                    Call(renderer, "Prepare", sim, scale, canvasHeight);
                    for (int cookCount = 1; cookCount <= 4; cookCount++)
                    {
                        workers.Clear();
                        for (int i = 0; i < cookCount; i++) workers.Add(allWorkers[i]);
                        int normalCount = 0, premiumCount = 0;
                        foreach (object worker in workers)
                            if (Get(worker, "Role").ToString() == "ParrilleroPremium") premiumCount++; else normalCount++;
                        int normalIndex = 0, premiumIndex = 0;
                        foreach (object worker in workers)
                        {
                            bool isPremium = Get(worker, "Role").ToString() == "ParrilleroPremium";
                            int product = isPremium ? 2 : 0;
                            int station = isPremium ? premiumIndex++ : normalIndex++;
                            int teamCount = isPremium ? premiumCount : normalCount;
                            Vector2 post = (Vector2)Call(layout, "GrillWorkPosition", product, station, teamCount);
                            Set(worker, "CookStationIndex", station);
                            Set(worker, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                            Set(worker, "Position", post); Set(worker, "Target", post);
                            Set(worker, "VisualCooking", true); Set(worker, "VisualCookingProduct", product);
                            Set(worker, "CookingProgress", .5f); Set(worker, "CookingCycleProgress", .5f);
                        }

                        Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                        object projection = Field(renderer, "workerProjection");
                        var views = (IDictionary)Field(projection, "workers");
                        Assert.AreEqual(cookCount, views.Count, "Projection must retain every cook from one to four.");
                        Texture2D regularCookingAtlas = (Texture2D)Field(projection, "cookingPoses");
                        Texture2D premiumCookingAtlas = (Texture2D)Field(projection, "premiumCookingPoses");
                        Assert.AreSame(sourceCookingAtlas, regularCookingAtlas);
                        Assert.IsNotNull(premiumCookingAtlas, "Premium gets one cached palette variant of the same cooking poses.");
                        Assert.AreNotSame(regularCookingAtlas, premiumCookingAtlas);

                        foreach (object worker in workers)
                        {
                            int id = (int)Get(worker, "Id");
                            object visual = views[id];
                            var torso = (SpriteRenderer)Field(visual, "Torso");
                            var leftLeg = (SpriteRenderer)Field(visual, "LeftLeg");
                            var rightLeg = (SpriteRenderer)Field(visual, "RightLeg");
                            var pose = (SpriteRenderer)Field(visual, "CookingPose");
                            Assert.IsTrue(pose.enabled, "Cooking must use the integrated full-body pose.");
                            Assert.IsFalse(torso.enabled, "The original torso (and its original arms) must not be double-drawn.");
                            Assert.IsFalse(leftLeg.enabled || rightLeg.enabled, "The original legs are hidden while the integrated full-body cooking cutout is drawn.");
                            Texture2D expected = Get(worker, "Role").ToString() == "ParrilleroPremium" ? premiumCookingAtlas : regularCookingAtlas;
                            Assert.AreSame(expected, pose.sprite.texture);
                            Assert.Greater(pose.sprite.rect.height, 600f, "Use one complete cook sprite so no hand/tool is cut at the torso seam.");
                            Assert.AreEqual(0f, pose.transform.localPosition.x, .0001f, "Cooking art stays on the worker axis; grill target must not translate it.");
                            Assert.AreEqual(0f, pose.transform.localPosition.y, .0001f, "The full cooking sprite is anchored at the worker's feet.");
                            Vector2 position = (Vector2)Get(worker, "Position");
                            Transform workerRoot = (Transform)Field(visual, "Root");
                            Assert.AreEqual((position.x - 270f) * .01f, workerRoot.localPosition.x, .0001f,
                                "The cooking pose root follows the worker post rather than the reserved food slot.");
                            int grillProduct = Get(worker, "Role").ToString() == "ParrilleroPremium" ? 2 : 0;
                            int cookingFrame = (int)cookingFrameForProgress.Invoke(null,
                                new object[] { (float)Get(worker, "CookingCycleProgress"), id });
                            float grillInset = (float)Call(layout, "GrillVisibleTopInset", grillProduct);
                            float footInset = (float)cookingPoseFootInset.Invoke(null, new object[] { cookingFrame });
                            float contactOffset = (float)Call(layout, "GrillVisualContactOffset", grillProduct);
                            float visualPositionY = position.y + grillInset + contactOffset + footInset / scale;
                            Assert.AreEqual((canvasHeight * .5f - visualPositionY * scale) * .01f,
                                workerRoot.localPosition.y, .0001f,
                                "Aspect-fit grill padding and each cooking frame's transparent foot margin are compensated without moving the logical post.");
                            Rect grillBounds = (Rect)Call(layout, "GrillBoundsForProduct", grillProduct);
                            float visibleGrillHeight = grillBounds.height - 2f * grillInset;
                            float visibleFootY = visualPositionY - footInset / scale;
                            float footDepth = (visibleFootY - grillBounds.yMin - grillInset) / visibleGrillHeight;
                            Assert.That(footDepth, Is.EqualTo(.30f).Within(.005f),
                                "Normal and Premium visible feet align near the cook-side edge while remaining readable at every tested aspect ratio.");
                        }

                        yield return null;
                        yield return null; // Batchmode does not advance WaitForEndOfFrame; the camera has rendered by the next frame.
                        SaveProjectionCapture(renderer, "parrillero-cooks-" + cookCount + "-" + (int)canvasHeight);
                    }

                    // Capture each authored hand/tool phase from the actual Unity kitchen camera.
                    // Keep all four phases on one character so no phase is hidden by a neighbor.
                    workers.Clear();
                    object phaseWorker = allWorkers[0];
                    workers.Add(phaseWorker);
                    bool phaseWorkerPremium = Get(phaseWorker, "Role").ToString() == "ParrilleroPremium";
                    int phaseProduct = phaseWorkerPremium ? 2 : 0;
                    Vector2 phasePost = (Vector2)Call(layout, "GrillWorkPosition", phaseProduct, 0, 1);
                    int phaseWorkerId = (int)Get(phaseWorker, "Id");
                    Set(phaseWorker, "CookStationIndex", 0);
                    Set(phaseWorker, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                    Set(phaseWorker, "Position", phasePost); Set(phaseWorker, "Target", phasePost);
                    Set(phaseWorker, "VisualCooking", true); Set(phaseWorker, "VisualCookingProduct", phaseProduct);
                    var observedPhaseRects = new HashSet<string>();
                    for (int phase = 0; phase < 4; phase++)
                    {
                        int progressStep = (phase - (phaseWorkerId & 3) + 4) & 3;
                        float cycleProgress = (progressStep + .25f) / 8f;
                        Set(phaseWorker, "CookingProgress", .5f);
                        Set(phaseWorker, "CookingCycleProgress", cycleProgress);
                        MethodInfo frameForProgress = T("StreetWorkerAnimation").GetMethod("CookingFrameForProgress", BindingFlags.Public | BindingFlags.Static);
                        Assert.AreEqual(phase, (int)frameForProgress.Invoke(null, new object[] { cycleProgress, phaseWorkerId }));
                        Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                        var phaseViews = (IDictionary)Field(Field(renderer, "workerProjection"), "workers");
                        var phasePose = (SpriteRenderer)Field(phaseViews[phaseWorkerId], "CookingPose");
                        observedPhaseRects.Add(phasePose.sprite.rect.ToString());
                        yield return null;
                        yield return null;
                        SaveProjectionCapture(renderer, "parrillero-phase-" + (phase + 1) + "-" + (int)canvasHeight);
                    }
                    Assert.AreEqual(4, observedPhaseRects.Count, "All four real cooking progress phases must render different authored poses.");

                    if (scale > 1f)
                    {
                        workers.Clear();
                        for (int i = 0; i < allWorkers.Count; i++) workers.Add(allWorkers[i]);
                        object first = workers[0];
                        Set(first, "VisualCooking", false); Set(first, "VisualCookingProduct", -1);
                        Set(first, "Position", new Vector2(270f, 500f)); Set(first, "Target", new Vector2(270f, 500f));
                        Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                        var views = (IDictionary)Field(Field(renderer, "workerProjection"), "workers");
                        object returned = views[(int)Get(first, "Id")];
                        Assert.IsFalse(((SpriteRenderer)Field(returned, "CookingPose")).enabled, "Leaving Cooking removes the integrated pose without a floating overlay.");
                        Assert.IsTrue(((SpriteRenderer)Field(returned, "Torso")).enabled, "The normal body returns for the next state.");
                        yield return null;
                        yield return null;
                        SaveProjectionCapture(renderer, "parrillero-transition-" + (int)canvasHeight);
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActiveCookTeamsStayVisibleOnOneRowAndSpreadAfterDepositing()
        {
            var root = new GameObject("Active cook single-row projection test");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { Activator.CreateInstance(T("StreetBalance")), 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound"); Set(sim, "Coins", 100000);
                Assert.IsTrue((bool)Get(sim, "UsesActiveCooks"));
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), "Parrillero")));
                    Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), "ParrilleroPremium")));
                }
                var workers = (IList)Get(sim, "Workers");
                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Call(renderer, "Prepare", sim, 1f, 960f);
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                object projection = Field(renderer, "workerProjection");
                var views = (IDictionary)Field(projection, "workers");
                var xPositions = new HashSet<float>();
                var rowSortOrders = new Dictionary<string, HashSet<int>>();
                float rowY = float.NaN;
                foreach (object worker in workers)
                {
                    object view = views[(int)Get(worker, "Id")];
                    Transform visual = (Transform)Field(view, "Root");
                    Assert.IsTrue(visual.gameObject.activeSelf, "Overlapping bounding boxes must not hide a distinct active cooker.");
                    Assert.IsTrue(xPositions.Add(visual.localPosition.x), "Cookers need distinct horizontal anchors.");
                    string role = Get(worker, "Role").ToString();
                    if (!rowSortOrders.ContainsKey(role)) rowSortOrders.Add(role, new HashSet<int>());
                    var torso = (SpriteRenderer)Field(view, "Torso");
                    Assert.IsTrue(rowSortOrders[role].Add(torso.sortingOrder), "Same-row cookers have stable distinct draw order within each team.");
                    if (float.IsNaN(rowY)) rowY = visual.localPosition.y;
                    Assert.AreEqual(rowY, visual.localPosition.y, .001f, "All active cooks stay on one horizontal line.");
                }
                Assert.AreEqual(workers.Count, xPositions.Count);
                Assert.AreEqual(5, rowSortOrders["Parrillero"].Count);
                Assert.AreEqual(4, rowSortOrders["ParrilleroPremium"].Count);

                object layout = Get(sim, "KitchenLayout");
                foreach (object worker in workers)
                {
                    int product = Get(worker, "Role").ToString() == "ParrilleroPremium" ? 2 : 0;
                    Vector2 deposit = (Vector2)Call(layout, "DepositPosition", product);
                    Set(worker, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                    Set(worker, "Position", deposit); Set(worker, "Target", deposit);
                }
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                xPositions.Clear();
                foreach (object worker in workers)
                {
                    object view = views[(int)Get(worker, "Id")];
                    Transform visual = (Transform)Field(view, "Root");
                    Assert.IsTrue(visual.gameObject.activeSelf, "Idle cooks sharing the table deposit must remain visible.");
                    Assert.IsTrue(xPositions.Add(visual.localPosition.x), "Idle cooks sharing one deposit anchor are visually spread.");
                }
                Assert.AreEqual(workers.Count, xPositions.Count);
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
