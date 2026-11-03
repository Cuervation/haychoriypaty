using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Advances one identified fan along a fixed, ordered route centerline.
    /// Progress is measured in world units from the first waypoint.
    /// </summary>
    public sealed class TowerDefenseFanRouteFollower
    {
        private readonly Vector2[] _waypoints;
        private readonly float[] _cumulativeDistances;
        private int _segmentIndex;
        private float _speed;

        /// <summary>Stable identity of the fan represented by this follower.</summary>
        public int FanId { get; }

        /// <summary>Current world position, constrained to the configured polyline.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Normalized direction of travel for the current route segment.</summary>
        public Vector2 Facing { get; private set; }

        /// <summary>Distance traveled from the first waypoint, in world units.</summary>
        public float Progress { get; private set; }

        /// <summary>Total length of the configured centerline, in world units.</summary>
        public float TotalDistance { get; }

        /// <summary>Movement speed in world units per second. Zero pauses movement.</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = ValidateSpeed(value);
        }

        /// <summary>True once the fan has reached the final waypoint.</summary>
        public bool IsComplete { get; private set; }

        /// <summary>True once the fan has reached the route exit.</summary>
        public bool HasExited => IsComplete;

        /// <summary>
        /// Creates a follower starting at the first point of a copied route.
        /// Consecutive duplicate waypoints are allowed and skipped; the route
        /// must contain at least one non-zero-length segment.
        /// </summary>
        /// <param name="fanId">Stable identity to retain for this fan.</param>
        /// <param name="waypoints">Ordered route centerline in world coordinates.</param>
        /// <param name="speed">Movement speed in world units per second.</param>
        public TowerDefenseFanRouteFollower(int fanId, IReadOnlyList<Vector2> waypoints, float speed)
        {
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));
            if (waypoints.Count < 2)
                throw new ArgumentException("A route needs at least two waypoints.", nameof(waypoints));

            FanId = fanId;
            _speed = ValidateSpeed(speed);
            _waypoints = new Vector2[waypoints.Count];
            _cumulativeDistances = new float[waypoints.Count];

            float totalDistance = 0f;
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector2 point = waypoints[i];
                if (!IsFinite(point.x) || !IsFinite(point.y))
                    throw new ArgumentException("Route waypoints must have finite coordinates.", nameof(waypoints));

                _waypoints[i] = point;
                if (i == 0)
                {
                    _cumulativeDistances[i] = 0f;
                    continue;
                }

                float segmentLength = Vector2.Distance(_waypoints[i - 1], point);
                if (!IsFinite(segmentLength))
                    throw new ArgumentException("Route segment lengths must be finite.", nameof(waypoints));

                totalDistance += segmentLength;
                if (!IsFinite(totalDistance))
                    throw new ArgumentException("Route length must be finite.", nameof(waypoints));
                _cumulativeDistances[i] = totalDistance;
            }

            if (totalDistance <= 0f)
                throw new ArgumentException("A route must have a non-zero total length.", nameof(waypoints));

            TotalDistance = totalDistance;
            Position = _waypoints[0];
            _segmentIndex = FindFirstNonZeroSegment();
            Facing = DirectionForSegment(_segmentIndex);
        }

        /// <summary>
        /// Advances by speed × deltaTime. Large steps can cross any number of
        /// waypoints; movement is clamped to the exit and allocates no memory.
        /// Calling this after exit leaves the terminal state unchanged.
        /// </summary>
        /// <param name="deltaTime">Elapsed seconds; must be finite and non-negative.</param>
        public void Advance(float deltaTime)
        {
            if (!IsFinite(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must be finite and non-negative.");
            if (IsComplete || deltaTime == 0f || _speed == 0f)
                return;

            double distanceToMove = (double)_speed * deltaTime;
            double remainingDistance = TotalDistance - Progress;
            if (distanceToMove >= remainingDistance)
            {
                CompleteAtExit();
                return;
            }

            Progress += (float)distanceToMove;
            if (Progress > TotalDistance)
                Progress = TotalDistance;

            UpdatePositionAndFacing();
        }

        private int FindFirstNonZeroSegment()
        {
            for (int segment = 0; segment < _waypoints.Length - 1; segment++)
                if (_cumulativeDistances[segment + 1] > _cumulativeDistances[segment])
                    return segment;
            return _waypoints.Length - 2;
        }

        private int FindLastNonZeroSegment()
        {
            for (int segment = _waypoints.Length - 2; segment >= 0; segment--)
                if (_cumulativeDistances[segment + 1] > _cumulativeDistances[segment])
                    return segment;
            return 0;
        }

        private Vector2 DirectionForSegment(int segment)
        {
            Vector2 direction = _waypoints[segment + 1] - _waypoints[segment];
            float length = _cumulativeDistances[segment + 1] - _cumulativeDistances[segment];
            return length > 0f ? direction / length : Vector2.zero;
        }

        private void UpdatePositionAndFacing()
        {
            while (_segmentIndex < _waypoints.Length - 2 &&
                   Progress >= _cumulativeDistances[_segmentIndex + 1])
                _segmentIndex++;

            while (_segmentIndex < _waypoints.Length - 1 &&
                   _cumulativeDistances[_segmentIndex + 1] <= _cumulativeDistances[_segmentIndex])
                _segmentIndex++;

            if (_segmentIndex >= _waypoints.Length - 1)
            {
                CompleteAtExit();
                return;
            }

            float segmentStart = _cumulativeDistances[_segmentIndex];
            float segmentLength = _cumulativeDistances[_segmentIndex + 1] - segmentStart;
            float segmentProgress = Mathf.Clamp01((Progress - segmentStart) / segmentLength);
            Vector2 start = _waypoints[_segmentIndex];
            Vector2 end = _waypoints[_segmentIndex + 1];
            Position = start + (end - start) * segmentProgress;
            Facing = DirectionForSegment(_segmentIndex);
        }

        private void CompleteAtExit()
        {
            Progress = TotalDistance;
            Position = _waypoints[_waypoints.Length - 1];
            Facing = DirectionForSegment(FindLastNonZeroSegment());
            IsComplete = true;
        }

        private static float ValidateSpeed(float speed)
        {
            if (!IsFinite(speed) || speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be finite and non-negative.");
            return speed;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
