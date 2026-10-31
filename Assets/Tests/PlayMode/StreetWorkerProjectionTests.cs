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
        private static void SetField(object obj,string name,object value) => obj.GetType().GetField(name,Instance).SetValue(obj,value);
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
        private static Vector2 PresentedPosition(object simulation, object worker)
        {
            MethodInfo method = T("StreetWorkerProjection").GetMethod("PresentationPosition",
                BindingFlags.Static | BindingFlags.NonPublic);
            return (Vector2)method.Invoke(null, new[] { simulation, worker });
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
        public IEnumerator SixCooksPerSpecialtyRenderWaitersAtTheirOwnGrill()
        {
            var root = new GameObject("Separate normal and Premium cook waiting queues");
            Texture2D premium = null, premiumDiagonal = null;
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[]
                    { Activator.CreateInstance(T("StreetBalance")), 8, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                Set(sim, "Coins", 100000);
                foreach (string roleName in new[] { "Parrillero", "ParrilleroPremium" })
                {
                    object role = Enum.Parse(T("StreetWorkerRole"), roleName);
                    while ((int)Call(sim, "WorkerCount", role) < 6)
                        Assert.IsTrue((bool)Call(sim, "TryHire", role));
                }

                IList workers = (IList)Get(sim, "Workers");
                var cooks = new List<object>();
                foreach (object worker in workers)
                    if (Get(worker, "Role").ToString().StartsWith("Parrillero")) cooks.Add(worker);
                workers.Clear();
                foreach (object worker in cooks) workers.Add(worker);
                Assert.AreEqual(12, workers.Count);
                object layout = Get(sim, "KitchenLayout");
                var waiting = new List<object>();
                foreach (string roleName in new[] { "Parrillero", "ParrilleroPremium" })
                {
                    int product = roleName == "Parrillero" ? 0 : 2;
                    int index = 0;
                    foreach (object worker in cooks)
                    {
                        if (Get(worker, "Role").ToString() != roleName) continue;
                        bool queued = index >= 4;
                        Vector2 point = queued ? (Vector2)Call(layout, "CookQueuePosition", product, index - 4, 2)
                            : (Vector2)Call(layout, "GrillWorkPosition", product, index, 4);
                        Set(worker, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                        Set(worker, "CookStationIndex", queued ? -1 : index);
                        Set(worker, "Position", point); Set(worker, "Target", point); Set(worker, "WaitPosition", point);
                        Set(worker, "VisualCooking", !queued); Set(worker, "VisualCookingProduct", queued ? -1 : product);
                        Set(worker, "CookingProgress", .5f); Set(worker, "CookingCycleProgress", .5f);
                        if (queued) waiting.Add(worker);
                        index++;
                    }
                }

                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Call(renderer, "Prepare", sim, 1f, 960f);
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                const BindingFlags staticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
                MethodInfo variant = T("StreetWorkerAppearance").GetMethod("CreateVariant", BindingFlags.Static | BindingFlags.Public);
                object premiumRole = Enum.Parse(T("StreetWorkerRole"), "ParrilleroPremium");
                premium = (Texture2D)variant.Invoke(null, new object[] { normal, premiumRole,
                    T("StreetView").GetField("ParrilleroPoses", staticPrivate).GetValue(null), new Vector2(1315, 1197) });
                premiumDiagonal = (Texture2D)variant.Invoke(null, new object[] { diagonal, premiumRole,
                    T("StreetView").GetField("ParrilleroDiagonalPoses", staticPrivate).GetValue(null), new Vector2(1536, 1024) });
                Assert.NotNull(premium); Assert.NotNull(premiumDiagonal);
                Call(renderer, "PrepareWorkers", normal, diagonal, premium, premiumDiagonal, beverage, beverage, true);
                IDictionary views = (IDictionary)Field(Field(renderer, "workerProjection"), "workers");
                Assert.AreEqual(12, views.Count, "Both six-cook specialties remain visible.");
                Assert.AreEqual(4, waiting.Count);
                foreach (object worker in waiting)
                {
                    int product = Get(worker, "Role").ToString() == "Parrillero" ? 0 : 2;
                    Rect grill = (Rect)Call(layout, "GrillBoundsForProduct", product);
                    Transform workerRoot = (Transform)Field(views[(int)Get(worker, "Id")], "Root");
                    float renderedX = workerRoot.localPosition.x / .01f + 270f;
                    Assert.AreEqual(((Vector2)Get(worker, "Position")).x, renderedX, .001f);
                    Assert.GreaterOrEqual(renderedX - 30f, grill.xMin - .01f);
                    Assert.LessOrEqual(renderedX + 30f, grill.xMax + .01f,
                        "Each waiting cook must render at its own grill, not its specialty table or the other grill.");
                }
                yield return null;
                yield return null;
                SaveProjectionCapture(renderer, "cook-queues-own-grills-960");
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                if (premium != null) UnityEngine.Object.Destroy(premium);
                if (premiumDiagonal != null) UnityEngine.Object.Destroy(premiumDiagonal);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MozoShowsAngryCueOnlyWhenPendingSandwichDemandHasNoReadyPickup()
        {
            var root = new GameObject("Angry Mozo cue acceptance");
            try
            {
                object balance = Activator.CreateInstance(T("StreetBalance"));
                T("StreetBalance").GetField("customerArrivalSeconds", Instance).SetValue(balance, 10000f);
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { balance, 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                IList workers = (IList)Get(sim, "Workers");
                object normalCook = null, premiumCook = null, mozo = null;
                foreach (object worker in workers)
                {
                    string role = Get(worker, "Role").ToString();
                    if (role == "Parrillero") normalCook = worker;
                    else if (role == "ParrilleroPremium") premiumCook = worker;
                    else if (role == "Mozo") mozo = worker;
                }
                Assert.IsNotNull(normalCook);
                Assert.IsNotNull(premiumCook);
                Assert.IsNotNull(mozo);
                Assert.That(Vector2.Distance((Vector2)Get(mozo, "Position"), (Vector2)Get(mozo, "WaitPosition")), Is.LessThan(.01f));

                MethodInfo needsCue = T("StreetWorkerProjection").GetMethod("NeedsMozoWaitingForSandwichAngryCue",
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo grillFacing = T("StreetWorkerProjection").GetMethod("MozoWaitingGrillFacing",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(needsCue);
                Assert.IsNotNull(grillFacing);
                object kitchen = Get(sim, "Kitchen");
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, mozo }),
                    "No angry cue is shown when no customer is waiting for a sandwich.");
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, normalCook }), "Cooks never get the Mozo's waiting cue.");
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, premiumCook }), "Premium cooks never get the Mozo's waiting cue.");

                Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
                object customer = ((IList)Get(sim, "Customers"))[0];
                Set(customer, "Position", Get(customer, "Target"));
                Set(customer, "State", Enum.Parse(T("StreetCustomerState"), "Waiting"));
                Assert.IsTrue((bool)needsCue.Invoke(null, new[] { sim, mozo }));

                Call(kitchen, "Add", 1, Enum.Parse(T("StreetFoodState"), "Cooked"),
                    Enum.Parse(T("StreetFoodLocation"), "Table"), 0);
                Assert.IsTrue((bool)needsCue.Invoke(null, new[] { sim, mozo }),
                    "A ready Paty does not satisfy a waiting Chori order or clear this Mozo's cue.");

                Set(mozo, "State", Enum.Parse(T("StreetWorkerState"), "ToStation"));
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, mozo }), "The cue is hidden while the Mozo is working/transiting.");
                Set(mozo, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                SetField(mozo, "ReturningToWaitPosition", true);
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, mozo }), "The cue is hidden on the return trip to the wait post.");
                SetField(mozo, "ReturningToWaitPosition", false);
                Set(mozo, "State", Enum.Parse(T("StreetWorkerState"), "ToCounter"));
                Set(mozo, "CarriedItemId", 999);
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, mozo }), "A Mozo carrying a unit never shows the empty-supply cue.");
                Set(mozo, "State", Enum.Parse(T("StreetWorkerState"), "Idle"));
                Set(mozo, "CarriedItemId", 0);

                object layout = Get(sim, "KitchenLayout");
                Vector2 normalGrill = ((Rect)Call(layout, "BoundsForProduct", 0)).center;
                Vector2 premiumGrill = ((Rect)Call(layout, "BoundsForProduct", 2)).center;
                Vector2 targetGrill = Vector2.Distance((Vector2)Get(mozo, "Position"), normalGrill) <=
                    Vector2.Distance((Vector2)Get(mozo, "Position"), premiumGrill) ? normalGrill : premiumGrill;
                MethodInfo resolveFacing = T("StreetWorkerAnimation").GetMethod("ResolveFacing", BindingFlags.Static | BindingFlags.Public);
                object expectedFacing = resolveFacing.Invoke(null, new object[] { targetGrill - (Vector2)Get(mozo, "Position"),
                    Enum.Parse(T("StreetFacing"), "Down") });
                Assert.AreEqual(expectedFacing, grillFacing.Invoke(null, new[] { sim, mozo, Enum.Parse(T("StreetFacing"), "Down") }),
                    "While frustrated, the Mozo's rendered idle pose looks toward the nearest grill.");

                Component renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Call(renderer, "Prepare", sim, 1f, 960f);
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                Assert.IsTrue((bool)Get(renderer, "WorkersReady"));
                object projection = Field(renderer, "workerProjection");
                IDictionary visuals = (IDictionary)Field(projection, "workers");
                SpriteRenderer normalCue = (SpriteRenderer)Field(visuals[(int)Get(normalCook, "Id")], "AngryCue");
                SpriteRenderer premiumCue = (SpriteRenderer)Field(visuals[(int)Get(premiumCook, "Id")], "AngryCue");
                SpriteRenderer mozoCue = (SpriteRenderer)Field(visuals[(int)Get(mozo, "Id")], "AngryCue");
                Assert.IsFalse(normalCue.enabled);
                Assert.IsFalse(premiumCue.enabled);
                Assert.IsTrue(mozoCue.enabled);
                Assert.IsNotNull(mozoCue.sprite);
                Assert.AreEqual("MozoEmptySandwichAngryCue", mozoCue.gameObject.name);
                Assert.That(mozoCue.transform.localPosition.y, Is.GreaterThan(1f), "The emoji sits above the Mozo's head.");
                Assert.That(mozoCue.sortingOrder, Is.GreaterThan(((SpriteRenderer)Field(visuals[(int)Get(mozo, "Id")], "Torso")).sortingOrder));
                yield return null;
                yield return null;
                SaveProjectionCapture(renderer, "mozo-anger-no-matching-pickup-960");

                Call(kitchen, "Add", 0, Enum.Parse(T("StreetFoodState"), "Cooked"),
                    Enum.Parse(T("StreetFoodLocation"), "Table"), 0);
                Assert.IsFalse((bool)needsCue.Invoke(null, new[] { sim, mozo }),
                    "A ready sandwich matching the waiting order can be picked up.");
                Call(sim, "Step", .05f);
                Assert.AreEqual("ToStation", Get(mozo, "State").ToString(),
                    "The available order unit dispatches this Mozo toward its table slot.");
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                Assert.IsFalse(mozoCue.enabled, "The cue disappears as soon as this Mozo starts toward its pickup.");
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
                            if (product == 4)
                            {
                                Assert.AreSame(Resources.Load<Texture2D>("street-worker-direction-extension-v1"), torso.sprite.texture,
                                    "Restore the upright, empty-handed frontal pickup pose.");
                                Transform props = ((GameObject)Field(renderer,"propsRoot")).transform;
                                var barrelRenderer = props.Find("station:Station_CocaBarrel").GetComponentInChildren<SpriteRenderer>(true);
                                Assert.Less(torso.sortingOrder, barrelRenderer.sortingOrder, "The barrel occludes the lower body.");
                                Assert.Less(((SpriteRenderer)Field(workerVisual,"LeftLeg")).sortingOrder, barrelRenderer.sortingOrder);
                                Rect barrelBounds = (Rect)Call(Get(sim,"KitchenLayout"),"BoundsForProduct",product);
                                Transform workerRoot = (Transform)Field(workerVisual,"Root");
                                float footX = workerRoot.localPosition.x * 100f + 270f;
                                float footY = (height * .5f - workerRoot.localPosition.y * 100f) / scale;
                                Assert.That(footX, Is.EqualTo(pickup.x).Within(.01f), "No sideways pickup shift.");
                                Assert.That(footY, Is.EqualTo(barrelBounds.yMax - 24f).Within(.01f), "Feet reference the barrel base, not its rim.");
                                Assert.IsFalse(torso.flipX, "Keep the front-facing authored pose.");
                            }
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
                            float expectedReach = product == 4 ? -.0784f : .196f;
                            Assert.That(idleReachY-torso.transform.localPosition.y, Is.EqualTo(expectedReach).Within(.002f),
                                "At peak dwell, the empty-handed torso reaches toward the barrel.");
                            Vector2 reachingPalm=(Vector2)Field(workerVisual,"Palm");
                            Assert.That(reachingPalm.y-idlePalm.y, Is.EqualTo(product == 4 ? -7.84f : 19.6f).Within(.02f),
                                "The hand anchor follows the visible reach instead of staying at the carry pose.");
                            if (product == 4)
                            {
                                yield return null; yield return null;
                                SaveProjectionCapture(renderer, "cocacolero-barrel-pickup");
                            }
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
        public IEnumerator CocacoleroPickupIsFrontalAndGroundedAtBothBarrels()
        {
            var root = new GameObject("Cocacolero Coca and beer barrel pickup preview");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[]
                    { Activator.CreateInstance(T("StreetBalance")), 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                Set(sim, "Coins", 10000);
                Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), "Cocacolero")));
                IList workers = (IList)Get(sim, "Workers");
                object cocaWorker = null;
                foreach (object candidate in workers)
                    if (Get(candidate, "Role").ToString() == "Cocacolero") { cocaWorker = candidate; break; }
                Assert.IsNotNull(cocaWorker, "The selected catalog must include its hired Cocacolero.");

                var workerType = T("StreetWorker");
                Vector2 routeStart = (Vector2)Get(cocaWorker, "Position");
                Vector2 routeTarget = routeStart + Vector2.right * 24f;
                MethodInfo setRoute = sim.GetType().GetMethod("SetWorkerRoute", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(setRoute);
                Assert.IsTrue((bool)setRoute.Invoke(sim, new object[] { cocaWorker, routeTarget }));
                Vector2[] measuredRoute = (Vector2[])workerType.GetField("Route", Instance).GetValue(cocaWorker);
                float expectedRouteDistance = 0f;
                Vector2 previousRoutePoint = routeStart;
                if (measuredRoute == null || measuredRoute.Length == 0)
                    expectedRouteDistance = Vector2.Distance(routeStart, routeTarget);
                else
                    foreach (Vector2 point in measuredRoute)
                    {
                        expectedRouteDistance += Vector2.Distance(previousRoutePoint, point);
                        previousRoutePoint = point;
                    }
                Assert.That((float)Get(cocaWorker, "RouteDistance"), Is.EqualTo(expectedRouteDistance).Within(.01f),
                    "The presentation blend must use the actual assigned route length.");

                object layout = Get(sim, "KitchenLayout");
                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Call(renderer, "Prepare", sim, 1f, 960f);
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                Assert.IsTrue((bool)Get(renderer, "WorkersReady"));
                IDictionary views = (IDictionary)Field(Field(renderer, "workerProjection"), "workers");
                foreach (int product in new[] { 4, 6 })
                {
                    Rect barrel = (Rect)Call(layout, "BoundsForProduct", product);
                    Assert.That(barrel.width, Is.GreaterThan(0f), "The selected level must have the actual Coca/beer barrel.");
                    Set(cocaWorker, "Product", product);
                    Vector2 pickup = (Vector2)Call(sim, "PickupPositionForWorker", cocaWorker);
                    Vector2 centeredSafePickup = (Vector2)Call(layout, "PickupPosition", product);
                    Assert.That(pickup.x, Is.EqualTo(centeredSafePickup.x).Within(.01f),
                        "A single Cocacolero must use the centered safe barrel anchor, not lane 0 of a fixed three-lane spread.");
                    float fullOffsetMagnitude = Mathf.Max(0f, barrel.height - 24f);
                    Set(cocaWorker, "State", Enum.Parse(T("StreetWorkerState"), "Pickup"));
                    Set(cocaWorker, "Position", pickup);
                    Set(cocaWorker, "Target", pickup);
                    Set(cocaWorker, "StepDistance", 0f);
                    Set(cocaWorker, "CarriedItemId", 0);
                    Set(cocaWorker, "PickupProgress", .5f);
                    workerType.GetField("Route", Instance).SetValue(cocaWorker, Array.Empty<Vector2>());
                    workerType.GetField("RouteIndex", Instance).SetValue(cocaWorker, 0);
                    Set(cocaWorker, "RouteDistance", 0f);
                    Vector2 heldPosition = PresentedPosition(sim, cocaWorker);
                    MethodInfo bodyLiftMethod = T("StreetWorkerProjection").GetMethod("BarrelDrinkPickupBodyLift",
                        BindingFlags.Static | BindingFlags.NonPublic);
                    Set(cocaWorker, "State", Enum.Parse(T("StreetWorkerState"), "ToStation"));
                    workerType.GetField("Route", Instance).SetValue(cocaWorker, new[] { pickup });
                    Set(cocaWorker, "RouteDistance", 64f);
                    Set(cocaWorker, "Position", pickup + Vector2.left * 48f);
                    Vector2 startApproachPosition = PresentedPosition(sim, cocaWorker);
                    Set(cocaWorker, "Position", pickup + Vector2.left * 12f);
                    Vector2 approachPosition = PresentedPosition(sim, cocaWorker);
                    Set(cocaWorker, "Position", pickup);
                    Vector2 arrivedPosition = PresentedPosition(sim, cocaWorker);
                    Set(cocaWorker, "State", Enum.Parse(T("StreetWorkerState"), "ToCounter"));
                    workerType.GetField("Route", Instance).SetValue(cocaWorker, new[] { pickup + Vector2.right * 64f });
                    Set(cocaWorker, "RouteDistance", 64f);
                    Set(cocaWorker, "Target", pickup + Vector2.right * 64f);
                    Set(cocaWorker, "Position", pickup + Vector2.right * 12f);
                    Vector2 departurePosition = PresentedPosition(sim, cocaWorker);
                    Assert.That(heldPosition.y, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.That(startApproachPosition.y, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.That(approachPosition.y, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.That(arrivedPosition.y, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.That(departurePosition.y, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.That(approachPosition.x, Is.EqualTo(pickup.x - 12f).Within(.01f));
                    Assert.That(departurePosition.x, Is.EqualTo(pickup.x + 12f).Within(.01f));
                    Assert.That((float)bodyLiftMethod.Invoke(null, new[] { sim, cocaWorker }), Is.EqualTo(0f).Within(.01f),
                        "The torso lift must be absent from every moving state.");
                    // Both products keep the same frontal facing while their root stays on the actual route.
                    Set(cocaWorker, "State", Enum.Parse(T("StreetWorkerState"), "ToStation"));
                    workerType.GetField("Route", Instance).SetValue(cocaWorker, new[] { pickup });
                    workerType.GetField("RouteIndex", Instance).SetValue(cocaWorker, 0);
                    Set(cocaWorker, "RouteDistance", 64f);
                    Set(cocaWorker, "Position", pickup + Vector2.left * 12f);
                    Set(cocaWorker, "Target", pickup);
                    Set(cocaWorker, "StepDistance", 1f);
                    Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                    object visual = views[(int)Get(cocaWorker, "Id")];
                    var torso = (SpriteRenderer)Field(visual, "Torso");
                    Transform workerRoot = (Transform)Field(visual, "Root");
                    Assert.IsFalse(torso.flipX, "The final approach must face forward for both barrels.");
                    float approachFootX = workerRoot.localPosition.x * 100f + 270f;
                    float approachFootY = (480f - workerRoot.localPosition.y * 100f);
                    Assert.That(approachFootX, Is.EqualTo(pickup.x - 12f).Within(.01f));
                    Assert.That(approachFootY, Is.EqualTo(pickup.y).Within(.02f),
                        "The rendered root must follow the exact route position during approach.");

                    Set(cocaWorker, "State", Enum.Parse(T("StreetWorkerState"), "Pickup"));
                    Set(cocaWorker, "Position", pickup);
                    Set(cocaWorker, "StepDistance", 0f);
                    Set(cocaWorker, "PickupProgress", 0f);
                    workerType.GetField("Route", Instance).SetValue(cocaWorker, Array.Empty<Vector2>());
                    workerType.GetField("RouteIndex", Instance).SetValue(cocaWorker, 0);
                    Set(cocaWorker, "RouteDistance", 0f);
                    Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                    Assert.IsTrue((bool)Get(renderer, "WorkersReady"));
                    Assert.AreSame(Resources.Load<Texture2D>("street-worker-direction-extension-v1"), torso.sprite.texture,
                        "Coca and beer must share the frontal reach art.");
                    float restTorsoY = torso.transform.localPosition.y;
                    Vector2 idlePalm = (Vector2)Field(visual, "Palm");
                    Set(cocaWorker, "PickupProgress", .5f);
                    Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                    float footX = workerRoot.localPosition.x * 100f + 270f;
                    float footY = 480f - workerRoot.localPosition.y * 100f;
                    Assert.That(footX, Is.EqualTo(pickup.x).Within(.01f));
                    Assert.That(footY, Is.EqualTo(pickup.y).Within(.01f));
                    Assert.IsFalse(torso.flipX);
                    Assert.That(torso.transform.localPosition.y - restTorsoY,
                        Is.EqualTo(.0784f - fullOffsetMagnitude * .01f).Within(.003f),
                        "Only the upper-body reach moves over the barrel; the root and feet stay grounded.");
                    Vector2 reachingPalm = (Vector2)Field(visual, "Palm");
                    Assert.That(reachingPalm.y - idlePalm.y, Is.EqualTo(-7.84f).Within(.02f),
                        "Both barrel pickups must animate the same empty-hand reach.");
                    Transform propsRoot = ((GameObject)Field(renderer, "propsRoot")).transform;
                    string stationName = product == 4 ? "Station_CocaBarrel" : "Station_BeerBarrel";
                    Transform barrelRoot = propsRoot.Find("station:" + stationName);
                    Assert.NotNull(barrelRoot);
                    Assert.Less(((SpriteRenderer)Field(visual,"LeftLeg")).sortingOrder,
                        barrelRoot.GetComponentInChildren<SpriteRenderer>(true).sortingOrder,
                        "The lower body remains behind either barrel front while the upper reach stays visible.");
                    Assert.Less(torso.sortingOrder, barrelRoot.GetComponentInChildren<SpriteRenderer>(true).sortingOrder,
                        "Either barrel hides the body instead of drawing the worker through its front.");

                    yield return null;
                    yield return null;
                    SaveProjectionCapture(renderer, product == 4
                        ? "cocacolero-coca-barrel-pickup-960" : "cocacolero-beer-barrel-pickup-960");
                }
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CocacoleroBarrelAccessLanesMatchTheActualHiredWorkerCount()
        {
            var root = new GameObject("Cocacolero barrel access lane count");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[]
                    { Activator.CreateInstance(T("StreetBalance")), 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                Set(sim, "Coins", 10000);
                Type roleType = T("StreetWorkerRole");
                object role = Enum.Parse(roleType, "Cocacolero");
                Assert.IsTrue((bool)Call(sim, "TryHire", role));
                Assert.IsTrue((bool)Call(sim, "TryHire", role));

                IList workers = (IList)Get(sim, "Workers");
                var cocacoleros = new List<object>();
                foreach (object worker in workers)
                    if (Get(worker, "Role").ToString() == "Cocacolero") cocacoleros.Add(worker);
                Assert.AreEqual(2, cocacoleros.Count);
                object layout = Get(sim, "KitchenLayout");
                object customer = Activator.CreateInstance(T("StreetCustomer"));
                foreach (object worker in cocacoleros)
                {
                    SetField(worker, "Customer", customer);
                    Set(worker, "State", Enum.Parse(T("StreetWorkerState"), "ToStation"));
                }

                // If the workers serve different beverages, each is alone at its own barrel.
                Set(cocacoleros[0], "Product", 4);
                Set(cocacoleros[1], "Product", 6);
                foreach (object worker in cocacoleros)
                {
                    int product = (int)Get(worker, "Product");
                    Vector2 actual = (Vector2)Call(sim, "PickupPositionForWorker", worker);
                    Vector2 centered = (Vector2)Call(layout, "PickupPosition", product);
                    Assert.That(actual.x, Is.EqualTo(centered.x).Within(.01f),
                        "Each barrel with just one assigned worker keeps that worker on its centered safe anchor.");
                }

                foreach (int product in new[] { 4, 6 })
                {
                    Set(cocacoleros[0], "Product", product);
                    Set(cocacoleros[1], "Product", product);
                    Vector2 first = (Vector2)Call(sim, "PickupPositionForWorker", cocacoleros[0]);
                    Vector2 second = (Vector2)Call(sim, "PickupPositionForWorker", cocacoleros[1]);
                    Vector2 expectedFirst = (Vector2)Call(layout, "PickupPosition", product, 0, 2);
                    Vector2 expectedSecond = (Vector2)Call(layout, "PickupPosition", product, 1, 2);
                    Assert.That(first.x, Is.EqualTo(expectedFirst.x).Within(.01f),
                        "The first hired worker should use the left point of the two-lane spread.");
                    Assert.That(second.x, Is.EqualTo(expectedSecond.x).Within(.01f),
                        "The second hired worker should use the right point of the two-lane spread.");
                    Assert.AreNotEqual(first.x, second.x, "Concurrent workers must not share a barrel anchor.");
                }
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GrillContactPresentationBlendsAcrossRealApproachAndTableDeparture()
        {
            var root = new GameObject("Cook contact transition projection test");
            try
            {
                object balance = Activator.CreateInstance(T("StreetBalance"));
                T("StreetBalance").GetField("customerArrivalSeconds", Instance).SetValue(balance, 10000f);
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[] { balance, 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                Set(sim, "Coins", 100000);
                IList workers = (IList)Get(sim, "Workers");
                object worker = workers[0];
                object layout = Get(sim, "KitchenLayout");
                Vector2 post = (Vector2)Call(layout, "GrillWorkPosition", 0, 0, 1);
                Vector2 start = post + Vector2.right * 160f;
                Set(worker, "Position", start); Set(worker, "Target", start);

                MethodInfo presentationPosition = T("StreetWorkerProjection").GetMethod("PresentationPosition",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(presentationPosition);
                Vector2 initialPresentation = (Vector2)presentationPosition.Invoke(null, new[] { sim, worker });
                Assert.AreEqual(0f, initialPresentation.y - ((Vector2)Get(worker, "Position")).y, .01f,
                    "A cook outside the grill approach does not receive the contact correction prematurely.");

                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                float targetOffset = (float)Call(layout, "GrillVisibleTopInset", 0) +
                    (float)Call(layout, "GrillVisualContactOffset", 0);
                float previousOffset = 0f, largestOffsetStep = 0f;
                bool sawApproach = false, capturedApproach = false, sawCooking = false, capturedCooking = false;
                bool sawDeparture = false, capturedDeparture = false, sawFadedDeparture = false;
                bool capturedTableInteraction = false;

                for (int tick = 0; tick < 700; tick++)
                {
                    Call(sim, "Step", .01f);
                    string state = Get(worker, "State").ToString();
                    Vector2 logical = (Vector2)Get(worker, "Position");
                    Vector2 presented = (Vector2)presentationPosition.Invoke(null, new[] { sim, worker });
                    float offset = presented.y - logical.y;

                    if (state == "ToGrill" || state == "Cooking" || state == "ToTable" || state == "Deposit")
                    {
                        float offsetStep = Mathf.Abs(offset - previousOffset);
                        largestOffsetStep = Mathf.Max(largestOffsetStep, offsetStep);
                        Assert.That(offsetStep, Is.LessThanOrEqualTo(2.6f),
                            "The grill-contact correction must change continuously at a 0.01-second simulation step.");
                    }
                    previousOffset = offset;

                    if (state == "ToGrill" && (int)Get(worker, "CookingItemId") > 0)
                    {
                        sawApproach = true;
                        Vector2 workPosition = (Vector2)Field(worker, "CookingWorkPosition");
                        if (!capturedApproach && Vector2.Distance(logical, workPosition) <= 18f)
                        {
                            Call(renderer, "Prepare", sim, 1f, 960f);
                            Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                            yield return null; yield return null;
                            SaveProjectionCapture(renderer, "cook-approach-after");
                            capturedApproach = true;
                        }
                    }
                    if (state == "Cooking")
                    {
                        sawCooking = true;
                        Assert.AreEqual(targetOffset, offset, .05f, "Cooking retains the final grill-contact anchor.");
                        if (!capturedCooking)
                        {
                            Call(renderer, "Prepare", sim, 1f, 960f);
                            Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                            yield return null; yield return null;
                            SaveProjectionCapture(renderer, "cook-animation-after");
                            capturedCooking = true;
                        }
                    }
                    if (state == "ToTable")
                    {
                        sawDeparture = true;
                        if (!capturedDeparture)
                        {
                            Assert.AreEqual(targetOffset, offset, .05f, "Leaving the cook pose starts from the same visible foot anchor.");
                            Call(renderer, "Prepare", sim, 1f, 960f);
                            Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                            yield return null; yield return null;
                            SaveProjectionCapture(renderer, "cook-table-departure-after");
                            capturedDeparture = true;
                        }
                        if (offset < 1f) sawFadedDeparture = true;
                    }
                    if (state == "Deposit" && sawDeparture)
                    {
                        Call(renderer, "Prepare", sim, 1f, 960f);
                        Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);
                        yield return null; yield return null;
                        SaveProjectionCapture(renderer, "food-table-worker-deposit-20261020");
                        capturedTableInteraction = true;
                        break;
                    }
                    yield return null;
                }

                Assert.IsTrue(sawApproach && capturedApproach, "The worker must approach the final slot and capture that transition.");
                Assert.IsTrue(sawCooking && capturedCooking, "The worker must enter real cooking and capture its integrated pose.");
                Assert.IsTrue(sawDeparture && capturedDeparture && sawFadedDeparture,
                    "The worker must leave continuously, fade the contact offset, and reach the table.");
                Assert.IsTrue(capturedTableInteraction,
                    "Capture the compact food table with the real cook arriving to deposit a real item.");
                Assert.That(largestOffsetStep, Is.LessThanOrEqualTo(2.6f));

                object kitchen = Get(sim, "Kitchen");
                for (int tick = 0; tick < 100 && (int)Call(kitchen, "TableCount", 0) == 0; tick++)
                {
                    Call(sim, "Step", .01f);
                    yield return null;
                }
                Assert.AreEqual(1, (int)Call(kitchen, "TableCount", 0), "The real cooked unit still deposits on its table.");
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

        [UnityTest]
        public IEnumerator ActiveCarryDepthKeepsRealFoodInFrontOfItsOwnBodyButBehindTheWorkerAhead()
        {
            var root = new GameObject("Active sandwich carry depth ordering");
            try
            {
                object sim = Activator.CreateInstance(T("StreetSimulation"), new object[]
                    { Activator.CreateInstance(T("StreetBalance")), 5, 5f, 0, 1, 0 });
                Call(sim, "StartRound");
                Set(sim, "Coins", 100000);
                Assert.IsTrue((bool)Get(sim, "UsesActiveCooks"), "Level 6 is the first active-cook layout.");
                Assert.IsTrue((bool)Call(sim, "TryHire", Enum.Parse(T("StreetWorkerRole"), "Parrillero")));
                IList workers = (IList)Get(sim, "Workers");
                object rearWorker = workers[0], frontWorker = workers[1];
                int rearId = (int)Get(rearWorker, "Id"), frontId = (int)Get(frontWorker, "Id");
                object kitchen = Get(sim, "Kitchen");
                object unit = Call(kitchen, "ReserveCook", 0, rearId);
                Assert.IsNotNull(unit, "Create a real production unit reserved to the rear worker.");
                int unitId = (int)Get(unit, "Id");
                Assert.IsTrue((bool)Call(kitchen, "AdvanceCook", unitId, rearId, 10f, 1f));
                Assert.IsTrue((bool)Call(kitchen, "TakeCooked", unitId, rearId),
                    "A Parrillero carries its own finished unit directly from grill to table.");

                Set(rearWorker, "Product", 0);
                Set(rearWorker, "CarriedItemId", unitId);
                Set(rearWorker, "State", Enum.Parse(T("StreetWorkerState"), "ToTable"));
                Set(rearWorker, "FacingVector", new Vector2(0f, 1f));
                Set(rearWorker, "Position", new Vector2(235f, 400f));
                Set(rearWorker, "Target", new Vector2(220f, 500f));
                Set(frontWorker, "State", Enum.Parse(T("StreetWorkerState"), "ToTable"));
                Set(frontWorker, "FacingVector", new Vector2(0f, 1f));
                Set(frontWorker, "Position", new Vector2(350f, 420f));
                Set(frontWorker, "Target", new Vector2(360f, 500f));

                var renderer = root.AddComponent(T("StreetKitchenRenderer"));
                Texture2D normal = Resources.Load<Texture2D>("street-parrillero");
                Texture2D diagonal = Resources.Load<Texture2D>("street-parrillero-diagonal-v1");
                Texture2D beverage = Resources.Load<Texture2D>("street-cocacolero-levels3-5-v1");
                Call(renderer, "Prepare", sim, 1f, 960f);
                Call(renderer, "PrepareWorkers", normal, diagonal, normal, diagonal, beverage, beverage, true);

                IDictionary views = (IDictionary)Field(Field(renderer, "workerProjection"), "workers");
                var rearView = views[rearId];
                var frontView = views[frontId];
                var rearTorso = (SpriteRenderer)Field(rearView, "Torso");
                var rearFingers = (SpriteRenderer)Field(rearView, "Fingers");
                var frontTorso = (SpriteRenderer)Field(frontView, "Torso");
                IDictionary foodObjects = (IDictionary)Field(renderer, "foodObjects");
                var physicalFood = (GameObject)foodObjects[unitId];
                Assert.IsNotNull(physicalFood, "Renderer must project the real carried unit.");
                var foodRenderer = physicalFood.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(foodRenderer);

                Assert.Greater(foodRenderer.sortingOrder, rearTorso.sortingOrder,
                    "Facing down, the carried sandwich must layer in front of its own torso.");
                Assert.Greater(rearFingers.sortingOrder, foodRenderer.sortingOrder,
                    "The supporting fingers remain in front of the carried sandwich.");
                Assert.Less(foodRenderer.sortingOrder, frontTorso.sortingOrder,
                    "The rear worker's carried sandwich must not draw over a worker 20 canvas points ahead.");
                foreach (int order in new[] { rearTorso.sortingOrder, rearFingers.sortingOrder,
                    foodRenderer.sortingOrder, frontTorso.sortingOrder })
                    Assert.That(order, Is.InRange(short.MinValue, short.MaxValue),
                        "Sorting orders use Unity's signed 16-bit range.");

                yield return null;
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
