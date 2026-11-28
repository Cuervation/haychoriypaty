using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    // Arte y poses de trabajadores. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        // The bubble lasts one second and uses the match clock, so its fade also pauses.
        private const float CrewChantSeconds = 1f, CrewChantFadeSeconds = .4f;
        // The speech art's tail sits at the bottom right; its tip is this far in from the right edge (mirrored: from the left edge).
        private const float CrewChantTailTipInsetLogical = 22f, CrewChantSidePaddingLogical = 12f, CrewChantLineHeightLogical = 15f, CrewChantBaseHeightLogical = 22f;
        // Body of the speech art (Items[11], 225 px tall, drawn with 15 px top/bottom slices of 65 px): opaque from source y≈10 to y≈196,
        // the tail below. In logical pixels the body starts 10/65×15 below the top and ends (225−196)/65×15 above the bottom.
        private const float CrewChantBodyTopInsetLogical = 2.3f, CrewChantBodyBottomInsetLogical = 6.7f;
        // Presentation only: ordinary attempts have their own clock and worker IDs, separate from Tower Defense.
        private StreetSimulation campaignCrewChantSimulation;
        private float campaignCrewChantLastTime;
        private readonly HashSet<int> campaignCrewChantWorked = new HashSet<int>();
        private readonly Dictionary<int, float> campaignCrewChantStart = new Dictionary<int, float>();
        private GUIStyle crewChantStyle;

        private static string CampaignCrewChantTextFor(StreetSimulation sim, StreetWorkerRole role)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero when !sim.UsesActiveCooks:
                case StreetWorkerRole.Mozo when sim.UsesActiveCooks:
                    if (!sim.IsProductAvailable(0)) return sim.IsProductAvailable(1) ? "¡Hay Paty!" : null;
                    return sim.IsProductAvailable(1) ? "¡Hay\nchori y Paty!" : "¡Hay chori!";
                case StreetWorkerRole.ParrilleroPremium:
                    if (!sim.IsProductAvailable(2)) return sim.IsProductAvailable(3) ? "¡Hay Vacío!" : null;
                    return sim.IsProductAvailable(3) ? "¡Hay Bondio\ny Vacío!" : "¡Hay Bondio!";
                case StreetWorkerRole.Cocacolero:
                    if (!sim.IsProductAvailable(4)) return sim.IsProductAvailable(6) ? "¡Hay birra!" : null;
                    return sim.IsProductAvailable(6) ? "¡Hay coca\ny birra!" : "¡Hay coca!";
                case StreetWorkerRole.Fernetero:
                    return sim.IsProductAvailable(5) ? "¡Hay Fernet!" : null;
                default:
                    return null;
            }
        }

        private bool TryGetCampaignCrewChantAge(StreetWorker worker, float now, out float age)
        {
            age = 0f;
            // A new hire, a cook walking to a grill, or a worker whose home position changes is not a completed trip.
            if (worker.CarriedItemId > 0 && (worker.State == StreetWorkerState.ToCounter || worker.State == StreetWorkerState.Handoff ||
                worker.State == StreetWorkerState.ToTable || worker.State == StreetWorkerState.Deposit))
                campaignCrewChantWorked.Add(worker.Id);
            bool atPost = worker.State == StreetWorkerState.Idle && !worker.ReturningToWaitPosition && !worker.VisualCooking &&
                worker.CarriedItemId <= 0 && worker.CookingItemId <= 0 && (worker.Position - worker.WaitPosition).sqrMagnitude <= 1f;
            if (!atPost)
            {
                campaignCrewChantStart.Remove(worker.Id);
                return false;
            }
            if (campaignCrewChantWorked.Remove(worker.Id)) campaignCrewChantStart[worker.Id] = now;
            if (!campaignCrewChantStart.TryGetValue(worker.Id, out float start)) return false;
            age = now - start;
            if (age >= 0f && age < CrewChantSeconds) return true;
            campaignCrewChantStart.Remove(worker.Id);
            return false;
        }

        private void DrawCampaignCrewChants(StreetSimulation sim)
        {
            float now = sim.Elapsed;
            if (!ReferenceEquals(sim, campaignCrewChantSimulation) || now < campaignCrewChantLastTime || sim.Phase != RoundPhase.Playing)
            {
                campaignCrewChantSimulation = sim;
                campaignCrewChantWorked.Clear();
                campaignCrewChantStart.Clear();
            }
            campaignCrewChantLastTime = now;
            if (sim.Phase != RoundPhase.Playing || items == null || text == null) return;

            foreach (StreetWorker worker in sim.Workers)
            {
                string chant = CampaignCrewChantTextFor(sim, worker.Role);
                if (chant == null || !TryGetCampaignCrewChantAge(worker, now, out float age) ||
                    StreetWorkerProjection.IsObscuredIdleWorker(sim.Workers, worker, layoutVerticalScale, sim.UsesActiveCooks) ||
                    StreetWorkerProjection.TryGetMozoWaitingSandwichTable(sim, worker, out _)) continue;
                Vector2 foot = StreetWorkerProjection.PresentationPosition(sim, worker);
                float height = StreetWorkerProjection.Height * StreetWorkerProjection.PresentationScale(sim, worker);
                DrawCrewChantBubble(chant, foot.x, foot.y * layoutVerticalScale - height, age);
            }
        }

        private void DrawCrewChantBubble(string chant, float headX, float headY, float age)
        {
            if (crewChantStyle == null) crewChantStyle = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            crewChantStyle.fontSize = 11;
            float alpha = age < CrewChantSeconds - CrewChantFadeSeconds ? 1f : (CrewChantSeconds - age) / CrewChantFadeSeconds;
            Color previousColor = GUI.color;
            // As narrow as its longest line allows (same font size): the bubble hugs the text on both sides.
            string[] lines = chant.Split('\n');
            float chantWidth = 0f;
            foreach (string line in lines) chantWidth = Mathf.Max(chantWidth, crewChantStyle.CalcSize(new GUIContent(line)).x);
            chantWidth = Mathf.Max(56f, Mathf.Ceil(chantWidth) + 2f * CrewChantSidePaddingLogical);
            float chantHeight = CrewChantBaseHeightLogical + lines.Length * CrewChantLineHeightLogical;
            float rise = (1f - alpha) * 6f; // it drifts up a little while it dissolves
            // The tail must point at its worker: bubbles of workers in the left half mirror the art (tail on the left, bubble to the right).
            bool tailLeft = headX < W * .5f;
            float bubbleX = tailLeft ? headX - CrewChantTailTipInsetLogical : headX - (chantWidth - CrewChantTailTipInsetLogical);
            var bubble = new Rect(Mathf.Clamp(bubbleX, 4f, W - chantWidth - 4f), headY - chantHeight - 3f - rise, chantWidth, chantHeight);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Matrix4x4 previousMatrix = GUI.matrix;
            if (tailLeft)
                GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(bubble.center.x, 0f, 0f), Quaternion.identity, new Vector3(-1f, 1f, 1f))
                    * Matrix4x4.TRS(new Vector3(-bubble.center.x, 0f, 0f), Quaternion.identity, Vector3.one);
            DrawOrderBubble(bubble, 1f);
            GUI.matrix = previousMatrix;
            // Centre the text on the bubble's body, not on the body plus tail (2026-11-26, a pedido: "¡Hay Bondio!" sat too high).
            var label = new Rect(bubble.x, bubble.y + CrewChantBodyTopInsetLogical, bubble.width,
                bubble.height - CrewChantBodyTopInsetLogical - CrewChantBodyBottomInsetLogical);
            GUI.Label(label, chant, crewChantStyle);
            GUI.color = previousColor;
        }

        private static int SpecialistUpgradePortraitSourceLevel(int levelIndex)
        {
            // Keep Cocacolero/Fernetero upgrade-card portraits visually consistent across levels.
            return levelIndex >= 0 && levelIndex < StreetSimulation.LevelNames.Length ? 1 : 0;
        }
        private static bool UsesMatchedCocacoleroArt(int levelIndex)
        {
            // Chicago and later specialist levels share the same worker style and proportions.
            return levelIndex >= 1 && levelIndex < StreetSimulation.LevelNames.Length;
        }
        private Texture2D CocacoleroArtForLevel(int levelIndex)
        {
            return UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null ? laterCocacolero : cocacolero;
        }
        private Rect CocacoleroPoseForLevel(int levelIndex, int frame)
        {
            if (UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null)
            {
                return MatchedCocacoleroPose(frame, new Vector2(laterCocacolero.width, laterCocacolero.height));
            }
            return ChicagoCocacoleroPoses[Mathf.Clamp(frame, 0, ChicagoCocacoleroPoses.Length - 1)];
        }
        private static Rect MatchedCocacoleroPose(int frame, Vector2 importedSize)
        {
            Rect pose = MatchedCocacoleroPoses[Mathf.Clamp(frame, 0, MatchedCocacoleroPoses.Length - 1)];
            Vector2 scale = new Vector2(importedSize.x / 1247f, importedSize.y / 1261f);
            return new Rect(pose.x * scale.x, pose.y * scale.y, pose.width * scale.x, pose.height * scale.y);
        }
        internal static Rect[] WorkerFoodPoseBounds => ParrilleroPoses;
        internal static Rect[] WorkerDiagonalPoseBounds => ParrilleroDiagonalPoses;
        internal static Rect[] WorkerDrinkPoseBounds(bool matched) => matched ? MatchedCocacoleroPoses : ChicagoCocacoleroPoses;

        private void DrawWorker(StreetWorker w)
        {
            if (StreetWorkerProjection.IsObscuredIdleWorker(game.Sim.Workers, w, layoutVerticalScale, game.Sim.UsesActiveCooks)) return;
            bool moving=w.State==StreetWorkerState.ToStation||w.State==StreetWorkerState.ToCounter||w.State==StreetWorkerState.ToGrill||w.State==StreetWorkerState.ToTable;
            int levelIndex = game.Sim.LevelIndex;
            Texture2D cocacoleroArt = CocacoleroArtForLevel(levelIndex);
            bool beverageRole = w.Role == StreetWorkerRole.Cocacolero || w.Role == StreetWorkerRole.Fernetero;
            bool isCocacolero = beverageRole && cocacoleroArt != null;
            if (w.Role == StreetWorkerRole.Fernetero)
                cocacoleroArt = UsesMatchedCocacoleroArt(levelIndex) && fernetWorkerLater != null ? fernetWorkerLater : fernetWorker;
            Vector2 movement=w.Target-w.Position;
            StreetWorkerPose workerPose = StreetWorkerAnimation.Sample(w);
            if (StreetWorkerProjection.IsFinalBarrelDrinkApproach(game.Sim, w))
                movement = new Vector2(0f, 1f);
            moving = workerPose.Moving;
            int frame=moving?SelectParrilleroWalkFrame(movement,workerPose.Frame):0;
            if(w.State==StreetWorkerState.Pickup||w.State==StreetWorkerState.Deposit)frame=beverageRole?0:11;
            if(w.State==StreetWorkerState.Handoff)frame=beverageRole?0:3;
            float bob=workerPose.Bob*1.875f;
            // Preserve the original90×98worker shape while anchoring its feet to the
            // same vertically adapted logical position as the native kitchen projection.
            float stationOffset = 98f * (layoutVerticalScale - 1f) / Mathf.Max(.01f, layoutVerticalScale);
            bob += stationOffset;
            Vector2 displayPosition = StreetWorkerProjection.PresentationPosition(game.Sim, w);
            if (w.State == StreetWorkerState.Pickup)
                displayPosition.y += StreetWorkerProjection.BarrelDrinkPickupBodyLift(game.Sim, w);
            float presentationScale = StreetWorkerProjection.PresentationScale(game.Sim, w);
            Rect workerRect=new Rect(displayPosition.x-45*presentationScale,displayPosition.y-98*presentationScale+bob*presentationScale,90*presentationScale,98*presentationScale);
            if(isCocacolero)
            {
                if(frame>=16)
                {
                    int phase=workerPose.Frame&1;
                    float x=Mathf.Abs(movement.x),y=Mathf.Abs(movement.y);
                    frame=x>y?9+phase:(movement.y>0?1+phase:5+phase);
                }
                bool flip=game.Sim.LevelIndex==1
                    ? (w.State == StreetWorkerState.Pickup ? ((w.Product == 4 || w.Product == 6)
                        ? false
                        : !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium))
                        : ChicagoWorkerFlip(true,frame,movement,false))
                    : w.State == StreetWorkerState.Pickup ? ((w.Product == 4 || w.Product == 6)
                        ? false
                        : !StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium))
                        : moving && Mathf.Abs(movement.x)>Mathf.Abs(movement.y) && movement.x<0;
                bool laterStyle = UsesMatchedCocacoleroArt(levelIndex) && laterCocacolero != null;
                Rect pose = CocacoleroPoseForLevel(levelIndex, frame);
                Draw(workerRect,cocacoleroArt,pose,laterStyle,flip);
            }
            else if (w.Role == StreetWorkerRole.ParrilleroPremium)
            {
                bool flip = w.State == StreetWorkerState.Pickup ? StreetWorkstationLayout.PickupReachesRight(w.Product, true)
                    : moving && Mathf.Abs(movement.x) > Mathf.Abs(movement.y) && movement.x > 0;
                Texture2D art = frame >= 16 ? premiumDiagonal : premiumWorker;
                Rect pose = frame >= 16 ? ParrilleroDiagonalPoses[frame-16] : ParrilleroPoses[frame];
                if (art != null) Draw(workerRect, art, pose, true, flip);
            }
            else if(frame>=16)ParrilleroDiagonal(workerRect,frame-16);
            else Parrillero(workerRect,frame,w.State == StreetWorkerState.Pickup
                ? StreetWorkstationLayout.PickupReachesRight(w.Product, game.Sim.HasParrilleroPremium)
                : game.Sim.LevelIndex == 1 ? ChicagoWorkerFlip(false,frame,movement,false)
                : moving && Mathf.Abs(movement.x) > Mathf.Abs(movement.y) && movement.x > 0);
            // Carried food is a native independent object keyed by CarriedItemId, never a cosmetic icon.
        }
        private static bool ChicagoWorkerFlip(bool cocacolero, int frame, Vector2 movement, bool pickup)
        {
            // Existing pickup/reach poses are mirrored: food reaches right, Coca reaches left.
            // Cardinal walks retain their facing even during the final few pixels of the trip.
            return pickup || ((frame == 9 || frame == 10) && (cocacolero ? movement.x < 0 : movement.x > 0));
        }
        private static int SelectParrilleroWalkFrame(Vector2 movement,int gaitFrame)
        {
            int phase=gaitFrame&1;
            float x=Mathf.Abs(movement.x),y=Mathf.Abs(movement.y);
            if(x>0.01f&&y>0.01f&&Mathf.Min(x,y)>=Mathf.Max(x,y)*.30f)
            {
                int direction=movement.y>0?(movement.x<0?0:1):(movement.x<0?2:3);
                return 16+direction*2+phase;
            }
            if(x>y)return 9+phase;
            return movement.y<0?5+phase:1+phase;
        }
    }
}
