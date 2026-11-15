using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Arte y poses de trabajadores. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
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
