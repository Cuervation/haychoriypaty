using UnityEngine;

namespace HayChoriYPaty
{
    // Canvas coordinates: positive Y is down, not Unity world up.
    public enum StreetFacing { Down, DownLeft, Left, UpLeft, Up, UpRight, Right, DownRight }
    public struct StreetWorkerPose
    {
        public StreetFacing Facing;
        public int Frame;
        public bool Moving, Carrying, PickingUp;
        public float Bob, PickupProgress;
        public int CookingFrame;
        public bool Cooking, WorkingAtGrill;
    }
    /// <summary>Presentation only: distance-driven gait; never advances service or creates food.</summary>
    public static class StreetWorkerAnimation
    {
        // A full four-sample cycle now spans 80 logical units: 60% of the previous cadence.
        public const float StrideLength = 80f;
        public static int CookingFrameForProgress(float progress, int workerId = 0)
        {
            // Two complete, distinct hand-tool cycles span the real unit-cooking progress.
            int step = Mathf.FloorToInt(Mathf.Clamp01(progress) * 8f);
            return (step + (workerId & 3)) & 3;
        }
        public static StreetFacing ResolveFacing(Vector2 movement, StreetFacing fallback)
        {
            if (movement.sqrMagnitude < .000001f) return fallback;
            float x = Mathf.Abs(movement.x), y = Mathf.Abs(movement.y);
            const float tangent = .41421356f;
            if (x < y * tangent) return movement.y < 0 ? StreetFacing.Up : StreetFacing.Down;
            if (y < x * tangent) return movement.x < 0 ? StreetFacing.Left : StreetFacing.Right;
            if (movement.x < 0) return movement.y < 0 ? StreetFacing.UpLeft : StreetFacing.DownLeft;
            return movement.y < 0 ? StreetFacing.UpRight : StreetFacing.DownRight;
        }
        public static int WalkFrame(float distance) => Mathf.FloorToInt(Mathf.Max(0f, distance) / (StrideLength / 4f)) % 4;
        public static StreetWorkerPose Sample(StreetWorker worker, bool idleAtGrill = false)
        {
            bool moving = worker.StepDistance > .0001f &&
                (worker.State == StreetWorkerState.ToStation || worker.State == StreetWorkerState.ToCounter ||
                 worker.State == StreetWorkerState.ToGrill || worker.State == StreetWorkerState.ToTable ||
                 worker.State == StreetWorkerState.ToCookQueue);
            int frame = WalkFrame(worker.TravelDistance);
            bool pickingUp = (worker.State == StreetWorkerState.Pickup || worker.State == StreetWorkerState.Deposit) && worker.CarriedItemId <= 0;
            bool cook = worker.Role == StreetWorkerRole.Parrillero || worker.Role == StreetWorkerRole.ParrilleroPremium;
            bool cooking = cook && (worker.State == StreetWorkerState.Cooking || worker.VisualCooking);
            bool workingAtGrill = cook && (cooking || worker.State == StreetWorkerState.Idle && idleAtGrill);
            StreetFacing facing = worker.Role == StreetWorkerRole.Cocacolero && pickingUp
                ? StreetFacing.Down : workingAtGrill ? StreetFacing.Down : ResolveFacing(worker.FacingVector, StreetFacing.Down);
            float bob = moving && (frame & 1) == 1 ? -.8f : 0f;
            if (workingAtGrill)
                bob = Mathf.Sin(worker.AnimationTime * 2.4f + worker.Id * .83f) * 1.2f;
            else if (cook && worker.State == StreetWorkerState.Idle)
                bob = Mathf.Sin(worker.AnimationTime * 1.6f + worker.Id * .83f) * .55f;
            return new StreetWorkerPose { Facing = facing,
                Frame = frame, Moving = moving, Carrying = worker.CarriedItemId > 0,
                PickingUp = pickingUp, PickupProgress = pickingUp ? Mathf.Clamp01(worker.PickupProgress) : 0f, Bob = bob,
                Cooking = cooking, WorkingAtGrill = workingAtGrill,
                CookingFrame = cooking ? CookingFrameForProgress(worker.CookingCycleProgress, worker.Id) : 0 };
        }
    }
}
