using UnityEngine;

namespace HayChoriYPaty
{
    // Canvas coordinates: positive Y is down, not Unity world up.
    public enum StreetFacing { Down, DownLeft, Left, UpLeft, Up, UpRight, Right, DownRight }
    public struct StreetWorkerPose
    {
        public StreetFacing Facing;
        public int Frame;
        public bool Moving, Carrying;
        public float Bob;
    }
    /// <summary>Presentation only: distance-driven gait; never advances service or creates food.</summary>
    public static class StreetWorkerAnimation
    {
        public const float StrideLength = 48f;
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
        public static StreetWorkerPose Sample(StreetWorker worker)
        {
            bool moving = worker.StepDistance > .0001f &&
                (worker.State == StreetWorkerState.ToStation || worker.State == StreetWorkerState.ToCounter);
            int frame = WalkFrame(worker.TravelDistance);
            return new StreetWorkerPose { Facing = ResolveFacing(worker.FacingVector, StreetFacing.Down),
                Frame = frame, Moving = moving, Carrying = worker.CarriedItemId > 0,
                Bob = moving && (frame & 1) == 1 ? -.8f : 0f };
        }
    }
}
