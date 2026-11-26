using UnityEngine;
using UnityEngine.Rendering;

namespace HayChoriYPaty
{
    /// <summary>
    /// The passive campaign-kitchen Defense levels (Camioneros, Los Redondos, La Renga) draw their crew with the campaign kitchen bodies (torso over legs, eight gait directions,
    /// carry, pickup reach) sampled from <see cref="StreetWorkerProjection"/>, so walking towards
    /// the fence shows the worker's back instead of a front pose sliding upwards. Presentation only.
    /// </summary>
    public sealed partial class TowerDefenseWorldPresentation
    {
        private StreetWorkerProjection campaignCrewProjection;

        private bool UsesCampaignCrewBodies => UsesKitchenGroundDepth;

        private StreetWorkerProjection CampaignCrewProjection()
        {
            if (campaignCrewProjection == null && view != null)
                campaignCrewProjection = view.CreateTowerDefenseCrewProjection(visualLevelId);
            return campaignCrewProjection;
        }

        private void DisposeCampaignCrewProjection()
        {
            if (campaignCrewProjection == null) return;
            campaignCrewProjection.Dispose();
            campaignCrewProjection = null;
        }

        private WorkerView CreateCampaignCrewView(TowerDefenseWorkerState worker)
        {
            if (CampaignCrewProjection() == null) return null;
            var obj = new GameObject("TD worker " + worker.Role + " " + worker.Id);
            obj.layer = presentationLayer;
            obj.transform.SetParent(presentationRoot.transform, true);
            var group = obj.AddComponent<SortingGroup>();
            group.sortingLayerName = "Default";
            var view = new WorkerView
            {
                GameObject = obj,
                Group = group,
                LeftLeg = CreateCampaignCrewPart(obj.transform, "LeftLeg", 0),
                RightLeg = CreateCampaignCrewPart(obj.transform, "RightLeg", 1),
                Torso = CreateCampaignCrewPart(obj.transform, "Torso", 2),
                Facing = StreetFacing.Down,
                LastPosition = worker.Position
            };
            view.Renderer = view.Torso;
            Sprite[] handoffFrames = this.view != null && (worker.Role == StreetWorkerRole.Parrillero || worker.Role == StreetWorkerRole.ParrilleroPremium)
                ? this.view.CreateTowerDefenseHandoffFrames(worker.Role) : null; // the Premium keeps its own outfit while cooking (2026-11-26)
            if (handoffFrames != null) generatedWorkerSprites.AddRange(handoffFrames);
            view.HandoffFrames = handoffFrames;
            view.Frames = System.Array.Empty<Sprite>();
            SetLayerRecursively(obj, presentationLayer);
            return view;
        }

        private static SpriteRenderer CreateCampaignCrewPart(Transform root, string name, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(root, false);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = order;
            return renderer;
        }

        private void SyncCampaignCrewBody(TowerDefenseGameMode gameMode, TowerDefenseWorkerState worker, WorkerView workerView)
        {
            StreetWorkerProjection projection = CampaignCrewProjection();
            float pixel = gameMode.WorldUnitsPerLogicalPixel;
            bool serving = worker.ServeAnimationSeconds > 0f;
            bool carrying = worker.ServiceStage == 2 && worker.ServiceUnitId > 0;
            bool dwelling = worker.ServiceDwelling && !worker.IsMoving;
            bool pickingUp = dwelling && (worker.ServiceStage == 1 || worker.ServiceStage == 3);
            TowerDefensePlacedStation source = worker.PendingProduct >= 0
                ? FindCampaignCrewStation(gameMode, TowerDefenseGameMode.StationForProduct(worker.PendingProduct)) : null;

            // Face like the campaign crew: the walking direction, the fan while carrying or
            // handing over, the station while picking up; otherwise keep the last facing.
            Vector2 look = Vector2.zero;
            if (worker.IsMoving)
            {
                if (worker.TransferRoute != null && worker.TransferIndex < worker.TransferRoute.Length)
                    look = worker.TransferRoute[worker.TransferIndex] - worker.Position;
                if (look.sqrMagnitude < 1e-10f) look = worker.Position - workerView.LastPosition;
            }
            // Handing over: face the fan once on arrival and hold it, as the campaign faces its customer at the counter.
            else if ((serving || carrying) && !workerView.WasMoving) { }
            else if (serving) look = worker.LastServiceTarget - worker.Position;
            else if (carrying)
            {
                TowerDefenseFanState fan = FindCampaignCrewFan(gameMode, worker.PendingFanId);
                if (fan != null) look = fan.RouteFollower.Position - worker.Position;
            }
            else if (pickingUp && source != null) look = source.Bounds.center - worker.Position;
            else if (worker.ServiceStage == 0 && worker.Role == StreetWorkerRole.Cocacolero)
            {
                // A Cocacolero waiting at its post stands beside its barrel and faces it.
                TowerDefensePlacedStation barrel = FindCampaignCrewStation(gameMode, TowerDefenseStationKind.CocaBarrel)
                    ?? FindCampaignCrewStation(gameMode, TowerDefenseStationKind.BeerBarrel);
                if (barrel != null && Vector2.Distance(worker.Position, worker.PostPosition) <= 2f * pixel)
                    look = new Vector2(barrel.Bounds.center.x - worker.Position.x, 0f);
                // La Renga's C (a pedido): settled at a waiting spot away from the barrel it looks ahead, at the first fence, like the cooks.
                else if (gameMode.IsCShapedLevel) look = Vector2.up;
            }
            // La Renga's C (a pedido): a crew member with nothing to deliver waits looking at the first fence, where the fans queue.
            else if (!worker.IsMoving && !serving && !carrying && !pickingUp && worker.ServiceStage == 0 && gameMode.IsCShapedLevel &&
                     worker.Role != StreetWorkerRole.Cocacolero)
                look = Vector2.up;
            // World Y is up; the campaign facing uses canvas coordinates (Y down).
            if (look.sqrMagnitude > 1e-10f)
                workerView.Facing = StreetWorkerAnimation.ResolveFacing(new Vector2(look.x, -look.y), workerView.Facing);
            // Barrel drinks: the campaign reaches into the barrel from the front; La Renga's barrels are taken from their
            // side, so there the Cocacolero faces the barrel in profile while grabbing.
            if (pickingUp && source != null && worker.Role == StreetWorkerRole.Cocacolero &&
                (worker.PendingProduct == 4 || worker.PendingProduct == 6))
                workerView.Facing = !gameMode.BarrelsFromTheSide ? StreetFacing.Down
                    : source.Bounds.center.x < worker.Position.x ? StreetFacing.Left : StreetFacing.Right;
            workerView.LastPosition = worker.Position;
            workerView.WasMoving = worker.IsMoving;

            int frame = worker.IsMoving ? StreetWorkerAnimation.WalkFrame(worker.TravelDistance / Mathf.Max(1e-6f, pixel)) : 0;
            float bob = worker.IsMoving && (frame & 1) == 1 ? -.8f : 0f;
            // An idle cook breathes, as in the campaign kitchen (StreetWorkerAnimation.Sample).
            if (!worker.IsMoving && !serving && !carrying && !pickingUp &&
                (worker.Role == StreetWorkerRole.Parrillero || worker.Role == StreetWorkerRole.ParrilleroPremium))
                bob = Mathf.Sin(worker.AnimationTime * 1.6f + worker.Id * .83f) * .55f;
            bool barrelPickup = pickingUp && worker.Role == StreetWorkerRole.Cocacolero &&
                (worker.PendingProduct == 4 || worker.PendingProduct == 6);
            bool pickupFlip = source != null && worker.Position.x < source.Bounds.center.x;
            float dwellTotal = gameMode.ChicagoDwellSeconds(worker.Role);
            float pickupProgress = pickingUp && dwellTotal > 0f ? 1f - worker.ServiceDwellSeconds / dwellTotal : 0f;
            // A sale keeps the arm out with the product until the hand-over cue leaves it.
            bool carryPose = carrying || serving;

            // Same visual height as the rest of the Tower Defense crew.
            float unit = pixel * TowerDefenseGameMode.WorkerVisualHeightLogical / StreetWorkerProjection.Height;
            Vector3 root = new Vector3(worker.Position.x, worker.Position.y, 0f);
            workerView.GameObject.transform.position = root;
            workerView.GameObject.transform.localRotation = Quaternion.identity;
            workerView.GameObject.transform.localScale = Vector3.one;
            workerView.Group.sortingOrder = KitchenGroundSortingOrder(worker.Position.y) + 1;

            bool cooking = gameMode.FreeConstruction && worker.IsCooking && workerView.HandoffFrames != null && workerView.HandoffFrames.Length > 0;
            StreetWorkerProjection.BodyPart torso, leftLeg, rightLeg;
            Vector2 palm;
            bool rear;
            if (cooking || projection == null || !projection.TrySampleBody(worker.Role, workerView.Facing, worker.IsMoving, frame,
                    carryPose, pickingUp && !carryPose, pickupFlip, barrelPickup, pickupProgress, bob,
                    out torso, out leftLeg, out rightLeg, out palm, out rear))
            {
                // The campaign's integrated grill pose (same cooking atlas as the kitchen levels).
                Sprite cook = cooking ? workerView.HandoffFrames[Mathf.FloorToInt(worker.AnimationTime * 4f) % workerView.HandoffFrames.Length] : null;
                workerView.Torso.sprite = cook;
                workerView.Torso.flipX = false;
                workerView.Torso.transform.localPosition = Vector3.zero;
                float height = TowerDefenseGameMode.WorkerVisualHeightLogical * pixel;
                workerView.Torso.transform.localScale = Vector3.one * (cook != null ? height / Mathf.Max(.001f, cook.bounds.size.y) : 1f);
                workerView.Torso.enabled = cook != null;
                workerView.LeftLeg.enabled = workerView.RightLeg.enabled = false;
                workerView.PalmWorld = (Vector2)root + new Vector2(18f, 40f) * pixel;
                workerView.Rear = false;
                return;
            }
            ApplyCampaignCrewPart(workerView.Torso, torso, unit);
            ApplyCampaignCrewPart(workerView.LeftLeg, leftLeg, unit);
            ApplyCampaignCrewPart(workerView.RightLeg, rightLeg, unit);
            workerView.PalmWorld = (Vector2)root + palm * unit;
            workerView.Rear = rear;
        }

        private static void ApplyCampaignCrewPart(SpriteRenderer renderer, StreetWorkerProjection.BodyPart part, float unit)
        {
            renderer.sprite = part.Sprite;
            renderer.flipX = part.Flip;
            renderer.transform.localPosition = new Vector3(part.Offset.x * unit, part.Offset.y * unit, 0f);
            // Campaign cutouts are sliced at 100 pixels per unit.
            renderer.transform.localScale = Vector3.one * (part.Scale * unit * 100f);
            renderer.enabled = part.Sprite != null;
        }

        private static TowerDefensePlacedStation FindCampaignCrewStation(TowerDefenseGameMode gameMode, TowerDefenseStationKind kind)
        {
            var stations = gameMode.PlacedStations;
            for (int i = 0; i < stations.Count; i++) if (stations[i].Kind == kind) return stations[i];
            return null;
        }

        private static TowerDefenseFanState FindCampaignCrewFan(TowerDefenseGameMode gameMode, int fanId)
        {
            if (fanId <= 0) return null;
            var fans = gameMode.Fans;
            for (int i = 0; i < fans.Count; i++) if (fans[i].Id == fanId) return fans[i];
            return null;
        }
    }
}
