using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Pooled native cutout animation in the existing kitchen camera, not a second simulation.</summary>
    internal sealed class StreetWorkerProjection : IDisposable
    {
        internal const float Height = 98f, Width = 90f, Waist = .76f;
        private const float WaistOverlap = .025f;
        private struct Art
        {
            public Texture2D Texture;
            public Rect Bounds;
            public bool Flip;
            public Vector2 Palm;
        }
        private sealed class Visual
        {
            public Transform Root;
            public SpriteRenderer Torso, LeftLeg, RightLeg, Fingers, CookingPose;
            public Vector2 Palm;
            public int ProductOrder;
        }
        private readonly Transform parent;
        private readonly Material material;
        private readonly Dictionary<int, Visual> workers = new Dictionary<int, Visual>();
        private struct SpriteKey : IEquatable<SpriteKey>
        {
            public Texture2D Texture;
            public Rect Bounds;
            public bool Equals(SpriteKey other) => Texture == other.Texture && Bounds == other.Bounds;
            public override bool Equals(object obj) => obj is SpriteKey && Equals((SpriteKey)obj);
            public override int GetHashCode() => unchecked(Texture.GetHashCode()*397 ^ Bounds.GetHashCode());
        }
        private readonly Dictionary<SpriteKey, Sprite> sprites = new Dictionary<SpriteKey, Sprite>();
        private readonly HashSet<int> liveIds = new HashSet<int>();
        private readonly List<int> stale = new List<int>();
        private readonly Texture2D extension, premiumExtension, fernetExtension;
        private readonly Texture2D cookingPoses;
        private Texture2D premiumCookingPoses;
        private Texture2D normal, diagonal, premium, premiumDiagonal, drink, fernet, mozo, mozoDiagonal, mozoExtension;
        private Texture2D mozoSource, mozoDiagonalSource;
        private bool matched;
        // Tight full-body frame bounds in the source PNG. Each cooking pose keeps the shoulders,
        // arms, hands, tongs, torso and feet together in one anchored cutout.
        private static readonly Rect[] CookingPoseBounds = {
            new Rect(112,29,454,632), new Rect(587,29,518,633),
            new Rect(73,669,493,662), new Rect(587,669,518,662)
        };
        private static readonly Vector2 CookingPoseAtlasSize = new Vector2(1175f,1338f);
        private static readonly Rect[] ExtensionBounds = { new Rect(28,11,206,279),new Rect(298,11,195,278),new Rect(562,11,213,277),new Rect(848,11,209,277),new Rect(34,299,226,280),new Rect(296,299,208,280),new Rect(557,299,217,280),new Rect(831,299,223,280),new Rect(41,588,218,281),new Rect(290,588,222,281),new Rect(564,588,217,281),new Rect(835,588,212,281),new Rect(38,876,173,272),new Rect(306,881,195,265),new Rect(598,883,176,268),new Rect(887,878,173,272),new Rect(37,1158,180,272),new Rect(326,1158,187,269),new Rect(567,1158,191,269),new Rect(866,1158,186,267) };
        public bool Ready => normal != null && diagonal != null && drink != null && extension != null;
        internal StreetWorkerProjection(Transform parent, Material material)
        {
            this.parent = parent; this.material = material;
            extension = Resources.Load<Texture2D>("street-worker-direction-extension-v1");
            cookingPoses = Resources.Load<Texture2D>("street-parrillero-cooking-arms-v1");
            if (cookingPoses != null)
                premiumCookingPoses = StreetWorkerAppearance.CreateVariant(cookingPoses, StreetWorkerRole.ParrilleroPremium,
                    CookingPoseBounds, CookingPoseAtlasSize);
            if (extension != null)
            {
                Rect[] food = new Rect[4], beverage = new Rect[16];
                Array.Copy(ExtensionBounds, 0, food, 0, 4); Array.Copy(ExtensionBounds, 4, beverage, 0, 16);
                var size = new Vector2(extension.width, extension.height);
                premiumExtension = StreetWorkerAppearance.CreateVariant(extension, StreetWorkerRole.ParrilleroPremium, food, size);
                fernetExtension = StreetWorkerAppearance.CreateVariant(extension, StreetWorkerRole.Fernetero, beverage, size);
            }
        }
        public void SetArt(Texture2D normal, Texture2D diagonal, Texture2D premium, Texture2D premiumDiagonal,
            Texture2D drink, Texture2D fernet, bool matched)
        {
            this.normal = normal; this.diagonal = diagonal; this.premium = premium; this.premiumDiagonal = premiumDiagonal;
            this.drink = drink; this.fernet = fernet; this.matched = matched;
            if (mozoSource != normal && normal != null)
            {
                DestroyMozoVariants();
                mozoSource = normal; mozo = StreetWorkerAppearance.CreateVariant(normal, StreetWorkerRole.Mozo, StreetView.WorkerFoodPoseBounds, new Vector2(1315,1197));
            }
            if (mozoDiagonalSource != diagonal && diagonal != null)
            {
                if (mozoDiagonal != null) UnityEngine.Object.Destroy(mozoDiagonal);
                mozoDiagonalSource = diagonal; mozoDiagonal = StreetWorkerAppearance.CreateVariant(diagonal, StreetWorkerRole.Mozo, StreetView.WorkerDiagonalPoseBounds, new Vector2(1536,1024));
            }
            if (extension != null && mozoExtension == null)
                mozoExtension = StreetWorkerAppearance.CreateVariant(extension, StreetWorkerRole.Mozo, new[] { ExtensionBounds[0], ExtensionBounds[1], ExtensionBounds[2], ExtensionBounds[3] }, new Vector2(extension.width, extension.height));
        }
        private void DestroyMozoVariants()
        {
            if (mozo != null) UnityEngine.Object.Destroy(mozo);
            if (mozoDiagonal != null) UnityEngine.Object.Destroy(mozoDiagonal);
            if (mozoExtension != null) UnityEngine.Object.Destroy(mozoExtension);
            mozo = mozoDiagonal = mozoExtension = null;
        }
        private static bool Beverage(StreetWorker w) => w.Role == StreetWorkerRole.Cocacolero || w.Role == StreetWorkerRole.Fernetero;
        internal static bool IsObscuredIdleWorker(IReadOnlyList<StreetWorker> allWorkers, StreetWorker candidate, float verticalScale, bool activeCooks = false)
        {
            if (candidate == null || candidate.State != StreetWorkerState.Idle || candidate.Role == StreetWorkerRole.Mozo) return false;
            if (activeCooks && IsCook(candidate.Role)) return false;
            // Only suppress genuine legacy duplicates sharing precisely one idle anchor.
            // Bounding-box overlap is expected for a dense horizontal cook row and must not hide staff.
            for (int i = 0; i < allWorkers.Count; i++)
            {
                StreetWorker other = allWorkers[i];
                if (other == null || other == candidate || other.Id >= candidate.Id || other.State != StreetWorkerState.Idle) continue;
                if (Vector2.Distance(other.Position, candidate.Position) <= .5f) return true;
            }
            return false;
        }
        private static bool IsCook(StreetWorkerRole role) => role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium;
        private static bool IsIdleAtGrill(StreetSimulation simulation, StreetWorker worker)
        {
            if (simulation == null || worker == null || worker.State != StreetWorkerState.Idle || !IsCook(worker.Role)) return false;
            int product = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
            if (!simulation.UsesActiveCooks)
            {
                Rect legacyGrill = simulation.KitchenLayout.GrillBoundsForProduct(product);
                Vector2 legacyAnchor = simulation.KitchenLayout.GrillWorkPosition(product, 0, 1);
                return legacyGrill.width > 0f && Mathf.Abs(worker.Position.y - legacyAnchor.y) <= 1f &&
                    worker.Position.x >= legacyGrill.xMin - 1f && worker.Position.x <= legacyGrill.xMax + 1f;
            }
            if (worker.CookStationIndex < 0) return false;
            int count = Mathf.Min(StreetKitchenLayout.MaxCookStationsPerGrill, simulation.WorkerCount(worker.Role));
            Vector2 anchor = simulation.KitchenLayout.GrillWorkPosition(product, worker.CookStationIndex, count);
            return Vector2.Distance(worker.Position, anchor) <= 1f;
        }
        private static bool TryCookingPresentationTarget(StreetSimulation simulation, StreetWorker worker,
            out int product, out int slot, out Vector2 target)
        {
            product = -1;
            slot = 0;
            target = Vector2.zero;
            if (simulation == null || simulation.Kitchen == null || worker == null) return false;
            if (worker.CookingItemId > 0)
            {
                StreetFoodUnit unit = simulation.Kitchen.Find(worker.CookingItemId);
                if (unit == null || unit.Location != StreetFoodLocation.Grill || unit.Product < 0 || unit.Product > 3) return false;
                product = unit.Product;
                slot = unit.Slot;
            }
            else if (worker.VisualCooking && worker.VisualCookingProduct >= 0 && worker.VisualCookingProduct < 4)
            {
                product = worker.VisualCookingProduct;
                int firstProduct = worker.Role == StreetWorkerRole.Parrillero ? 0 : 2;
                if (product < firstProduct || product > firstProduct + 1) return false;
            }
            else return false;

            bool companionAvailable = simulation.IsProductAvailable(product ^ 1);
            target = simulation.KitchenLayout.GrillSlotPosition(product, slot, companionAvailable,
                simulation.Kitchen.GrillCapacity(product));
            return true;
        }
        internal static Vector2 PresentationPosition(StreetSimulation simulation, StreetWorker worker)
        {
            return worker == null ? Vector2.zero : worker.Position;
        }
        internal static float PresentationScale(StreetSimulation simulation, StreetWorker worker)
        {
            // Staff density is solved by assigning only four cooks to physical posts; sprites
            // retain their original responsive scale regardless of the waiting queue length.
            return 1f;
        }
        private static int RowSortBias(StreetSimulation simulation, StreetWorker worker, Vector2 displayPosition)
        {
            if (simulation == null || worker == null || !simulation.UsesActiveCooks || !IsCook(worker.Role))
                return Mathf.Clamp(Mathf.RoundToInt(displayPosition.x / StreetSceneLayout.Width * 4f), 0, 4);
            int index = 0, count = 0;
            foreach (StreetWorker other in simulation.Workers)
            {
                if (other.Role != worker.Role) continue;
                if (other.Id == worker.Id) index = count;
                count++;
            }
            return count <= 1 ? 0 : Mathf.RoundToInt(Mathf.Lerp(-4f, 4f, index / (float)(count - 1)));
        }
        private Texture2D BaseTexture(StreetWorker w) => w.Role == StreetWorkerRole.Mozo && mozo != null ? mozo : Beverage(w) ? (w.Role == StreetWorkerRole.Fernetero ? fernet : drink)
            : w.Role == StreetWorkerRole.ParrilleroPremium ? premium : normal;
        private Texture2D Extension(StreetWorker w) => w.Role == StreetWorkerRole.Mozo && mozoExtension != null ? mozoExtension : w.Role == StreetWorkerRole.ParrilleroPremium ? premiumExtension
            : w.Role == StreetWorkerRole.Fernetero ? fernetExtension : extension;
        private static bool Diagonal(StreetFacing f) => ((int)f & 1) == 1;
        private static int Quarter(StreetFacing f) => f == StreetFacing.DownLeft ? 0 : f == StreetFacing.DownRight ? 1 : f == StreetFacing.UpLeft ? 2 : 3;
        private Art Original(StreetWorker w, int frame, bool flip)
        {
            Texture2D texture = BaseTexture(w);
            Rect source = Beverage(w) ? StreetView.WorkerDrinkPoseBounds(matched)[frame] : StreetView.WorkerFoodPoseBounds[frame];
            Vector2 authored = Beverage(w) ? new Vector2(1247,1261) : new Vector2(1315,1197);
            source = new Rect(source.x * texture.width / authored.x, source.y * texture.height / authored.y,
                source.width * texture.width / authored.x, source.height * texture.height / authored.y);
            return new Art { Texture = texture, Bounds = source, Flip = flip, Palm = new Vector2(.5f,.58f) };
        }
        private Art Supplemental(StreetWorker w, int index, bool flip, Vector2 palm)
            => new Art { Texture = Extension(w), Bounds = ExtensionBounds[index], Flip = flip, Palm = palm };
        private Art CookPoseArt(StreetWorker worker, int frame)
        {
            Texture2D texture = worker.Role == StreetWorkerRole.ParrilleroPremium && premiumCookingPoses != null
                ? premiumCookingPoses : cookingPoses;
            Rect source = CookingPoseBounds[Mathf.Clamp(frame, 0, CookingPoseBounds.Length - 1)];
            Vector2 scale = new Vector2(texture.width / CookingPoseAtlasSize.x, texture.height / CookingPoseAtlasSize.y);
            Rect bounds = new Rect(source.x * scale.x, source.y * scale.y, source.width * scale.x, source.height * scale.y);
            return new Art { Texture = texture, Bounds = bounds, Palm = new Vector2(.5f, 1f) };
        }
        private Art Walk(StreetWorker w, StreetFacing facing, int contact, bool moving)
        {
            if (Diagonal(facing))
            {
                int quarter = Quarter(facing);
                if (Beverage(w)) return Supplemental(w, 4 + quarter * 2 + contact, false, Vector2.zero);
                Texture2D texture = w.Role == StreetWorkerRole.Mozo && mozoDiagonal != null ? mozoDiagonal : w.Role == StreetWorkerRole.ParrilleroPremium ? premiumDiagonal : diagonal;
                return new Art { Texture = texture, Bounds = StreetView.WorkerDiagonalPoseBounds[quarter * 2 + contact] };
            }
            int first = facing == StreetFacing.Down ? 0 : facing == StreetFacing.Up ? 4 : 8;
            bool flip = first == 8 && (Beverage(w) ? facing == StreetFacing.Left : facing == StreetFacing.Right);
            return Original(w, first + (moving ? 1 + contact : 0), flip);
        }
        private Art Carry(StreetWorker w, StreetFacing facing)
        {
            if (!Beverage(w))
            {
                if (!Diagonal(facing))
                {
                    Art a = Original(w, facing == StreetFacing.Down ? 12 : facing == StreetFacing.Up ? 13 : 7, facing == StreetFacing.Right);
                    a.Palm = facing == StreetFacing.Down ? new Vector2(.5f,.68f) : facing == StreetFacing.Up ? new Vector2(.68f,.65f) : new Vector2(.23f,.67f);
                    // Keep the real unit beside the hand/forearm at waist height, not near the face.
                    if (facing == StreetFacing.Up && (w.Role == StreetWorkerRole.Mozo || w.State == StreetWorkerState.ToTable))
                        a.Palm = new Vector2(.98f, .65f);
                    return a;
                }
                int q = Quarter(facing), index = q < 2 ? 1 - q : q;
                return Supplemental(w, index, false, new Vector2(q < 2 ? .36f : q == 2 ? .21f : .81f, q < 2 ? .68f : .65f));
            }
            if (Diagonal(facing))
            {
                int q = Quarter(facing);
                Vector2 palm = new Vector2(q == 0 ? .75f : q == 1 ? .69f : q == 2 ? .22f : .81f, q < 2 ? .68f : .65f);
                return Supplemental(w, 16 + q, q == 0, palm);
            }
            int cell = facing == StreetFacing.Down ? 12 : facing == StreetFacing.Up ? 13 : facing == StreetFacing.Left ? 14 : 15;
            return Supplemental(w, cell, facing == StreetFacing.Left,
                new Vector2(facing == StreetFacing.Down ? .25f : facing == StreetFacing.Up ? .83f : .80f, facing == StreetFacing.Up ? .65f : .68f));
        }
        private Sprite Slice(Texture2D texture, Rect bounds)
        {
            // Value-type keys: no key/texture/pixel allocation in steady-state gait.
            var key = new SpriteKey { Texture = texture, Bounds = bounds };
            Sprite sprite;
            if (sprites.TryGetValue(key, out sprite)) return sprite;
            Rect uv = new Rect(bounds.x, texture.height - bounds.yMax, bounds.width, bounds.height);
            sprite = Sprite.Create(texture, uv, new Vector2(.5f,0f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name + "_cutout"; sprite.hideFlags = HideFlags.DontSave;
            sprites.Add(key, sprite); return sprite;
        }
        private SpriteRenderer Part(Transform root, string name)
        {
            var go = new GameObject(name); go.hideFlags = HideFlags.DontSave; go.layer = 30;
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sharedMaterial = material; return sr;
        }
        private Visual Create(int id)
        {
            var go = new GameObject("Worker_" + id); go.hideFlags = HideFlags.DontSave; go.layer = 30;
            go.transform.SetParent(parent, false);
            var v = new Visual { Root = go.transform };
            v.Torso = Part(v.Root,"Torso"); v.LeftLeg = Part(v.Root,"LeftLeg"); v.RightLeg = Part(v.Root,"RightLeg"); v.Fingers = Part(v.Root,"GripFingers");
            v.CookingPose = Part(v.Root,"CookingPose"); v.CookingPose.enabled = false;
            workers.Add(id,v); return v;
        }
        private float Scale(Art art) => Mathf.Min(Width / art.Bounds.width, Height / art.Bounds.height);
        private void DrawPart(SpriteRenderer renderer, Art art, Rect source, Vector2 local, int order, float scaleMultiplier = 1f)
        {
            renderer.sprite = Slice(art.Texture, source); renderer.flipX = art.Flip;
            renderer.transform.localPosition = new Vector3(local.x*.01f, local.y*.01f,0);
            renderer.transform.localScale = Vector3.one * (Scale(art) * scaleMultiplier);
            renderer.sortingOrder = order; renderer.enabled = true;
        }
        public void Sync(StreetSimulation sim, float verticalScale, float canvasHeight)
        {
            if (!Ready) return;
            liveIds.Clear();
            foreach (StreetWorker w in sim.Workers)
            {
                liveIds.Add(w.Id); Visual v;
                if (!workers.TryGetValue(w.Id,out v)) v = Create(w.Id);
                if (IsObscuredIdleWorker(sim.Workers, w, verticalScale, sim.UsesActiveCooks))
                {
                    if (v.Root.gameObject.activeSelf) v.Root.gameObject.SetActive(false);
                    continue;
                }
                if (!v.Root.gameObject.activeSelf) v.Root.gameObject.SetActive(true);
                Vector2 displayPosition = PresentationPosition(sim, w);
                float presentationScale = PresentationScale(sim, w);
                StreetWorkerPose pose = StreetWorkerAnimation.Sample(w, IsIdleAtGrill(sim, w));
                int contact = pose.Frame >= 2 ? 1 : 0;
                bool hasCookingPose = pose.Cooking && cookingPoses != null &&
                    TryCookingPresentationTarget(sim, w, out _, out _, out _);
                Art upper = pose.Carrying ? Carry(w,pose.Facing) : Walk(w,pose.Facing,contact,pose.Moving);
                if (pose.Cooking) upper = Original(w, 0, false);
                if (pose.PickingUp)
                    upper = Beverage(w) ? Carry(w,pose.Facing)
                        : Original(w,11, w.Position.x < sim.KitchenLayout.BoundsForProduct(w.Product).center.x);
                bool passing = !pose.Moving || (pose.Frame & 1) == 1;
                StreetFacing legFacing = Diagonal(pose.Facing) ? (pose.Facing == StreetFacing.UpLeft || pose.Facing == StreetFacing.UpRight ? StreetFacing.Up : StreetFacing.Down) : pose.Facing;
                Art legs = passing ? Walk(w,legFacing,0,false) : Walk(w,pose.Facing,contact,true);
                float upperScale = Scale(upper) * presentationScale, legScale = Scale(legs) * presentationScale;
                // Overlap the paired cutouts slightly; the upper layer hides the atlas seam
                // without leaving a gap when the cook transitions back to gait/carry poses.
                float split = upper.Bounds.height * (Waist + WaistOverlap);
                float reach = pose.PickingUp ? Mathf.Sin(pose.PickupProgress * Mathf.PI) * Height * .20f : 0f;
                float upperBottom = upper.Bounds.height * (1f-Waist-WaistOverlap) * upperScale - pose.Bob * presentationScale - reach * presentationScale;
                // A tiny x-based tie-break makes same-row workers deterministic without overriding y-depth.
                int depth = -5000 + Mathf.RoundToInt(displayPosition.y * verticalScale) + RowSortBias(sim, w, displayPosition);
                v.Root.localPosition = new Vector3((displayPosition.x-270f)*.01f, (canvasHeight*.5f-displayPosition.y*verticalScale)*.01f,0);
                if (hasCookingPose)
                {
                    v.Torso.enabled = false;
                    v.LeftLeg.enabled = false;
                    v.RightLeg.enabled = false;
                }
                else
                {
                    DrawPart(v.Torso,upper,new Rect(upper.Bounds.x,upper.Bounds.y,upper.Bounds.width,split),new Vector2(0,upperBottom),depth+2,presentationScale);
                    float legHeight = legs.Bounds.height * (1f-Waist+WaistOverlap), half = legs.Bounds.width*.5f;
                    // Passing keyframes articulate alternate legs; at least one foot stays planted.
                    float leftLift = pose.Moving && pose.Frame == 1 ? 2f : 0f;
                    float rightLift = pose.Moving && pose.Frame == 3 ? 2f : 0f;
                    float quarterWidth = legs.Bounds.width * legScale * .25f;
                    if (legs.Flip) { float t=leftLift; leftLift=rightLift; rightLift=t; }
                    DrawPart(v.LeftLeg,legs,new Rect(legs.Bounds.x,legs.Bounds.yMax-legHeight,half,legHeight),new Vector2(legs.Flip ? quarterWidth : -quarterWidth,leftLift),depth,presentationScale);
                    DrawPart(v.RightLeg,legs,new Rect(legs.Bounds.x+half,legs.Bounds.yMax-legHeight,half,legHeight),new Vector2(legs.Flip ? -quarterWidth : quarterWidth,rightLift),depth+1,presentationScale);
                }
                v.Fingers.enabled = pose.Carrying;
                v.CookingPose.enabled = false;
                if (hasCookingPose)
                {
                    Art cooked = CookPoseArt(w, pose.CookingFrame);
                    // One complete sprite prevents arm/leg seams. Sorting by the worker's foot
                    // depth keeps the grill in front where the two silhouettes naturally overlap.
                    DrawPart(v.CookingPose, cooked, cooked.Bounds, Vector2.zero, depth+2, presentationScale);
                }
                float handX = (upper.Palm.x-.5f)*upper.Bounds.width*upperScale;
                if (upper.Flip) handX = -handX;
                float handY = -(1f-upper.Palm.y)*upper.Bounds.height*upperScale + pose.Bob * presentationScale + (pose.PickingUp ? reach * presentationScale : 0f);
                v.Palm = new Vector2(displayPosition.x+handX,displayPosition.y*verticalScale+handY);
                bool rear = pose.Facing == StreetFacing.Up || pose.Facing == StreetFacing.UpLeft || pose.Facing == StreetFacing.UpRight;
                v.ProductOrder = rear ? depth-1 : depth+10000;
                if (pose.Carrying)
                {
                    Rect thumb = new Rect(upper.Bounds.x + upper.Bounds.width*(upper.Palm.x-.065f),
                        upper.Bounds.y + upper.Bounds.height*(upper.Palm.y-.065f), upper.Bounds.width*.13f,upper.Bounds.height*.065f);
                    DrawPart(v.Fingers,upper,thumb,new Vector2(handX,-handY),v.ProductOrder+1,presentationScale);
                }
            }
            stale.Clear(); foreach (int id in workers.Keys) if (!liveIds.Contains(id)) stale.Add(id);
            foreach (int id in stale) { UnityEngine.Object.Destroy(workers[id].Root.gameObject); workers.Remove(id); }
        }
        public bool Grip(int workerId, out Vector2 palm, out int order)
        {
            Visual v; if (workers.TryGetValue(workerId,out v)) { palm=v.Palm; order=v.ProductOrder; return true; }
            palm=Vector2.zero;order=0;return false;
        }
        public void Dispose()
        {
            foreach (Visual v in workers.Values) if (v.Root != null) UnityEngine.Object.Destroy(v.Root.gameObject);
            foreach (Sprite sprite in sprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            if (premiumExtension != null) UnityEngine.Object.Destroy(premiumExtension);
            if (premiumCookingPoses != null) UnityEngine.Object.Destroy(premiumCookingPoses);
            if (fernetExtension != null) UnityEngine.Object.Destroy(fernetExtension);
            DestroyMozoVariants();
            workers.Clear();sprites.Clear();
        }
    }
}
