using System;

namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Positional shield over four lane slots. WASD requests a slot; the
    /// shield travels there with smoothstep easing over the tuned transit
    /// time and stays parked. Coverage follows the actual interpolated
    /// position, so mid-transit the shield covers no lane. All motion is a
    /// deterministic function of DSP timestamps — no frames involved.
    /// Slots: North 0°, East 90°, South 180°, West 270° (compass).
    /// </summary>
    public sealed class ShieldController
    {
        /// <summary>
        /// A lane counts as covered inside this half-angle. Below half the
        /// 90° slot spacing, so mid-transit covers nothing. Demo tuning [U].
        /// </summary>
        public const float CoverageHalfAngleDeg = 30f;

        private readonly double _transitMs;
        private double _legFromAngle;
        private double _legToAngle;
        private double _legStartDsp;

        public ShieldController(double transitMs = 180.0)
        {
            if (transitMs < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(transitMs));
            }

            _transitMs = transitMs;
            _legFromAngle = DirectionToAngle(Direction.South);
            _legToAngle = _legFromAngle;
            _legStartDsp = double.NegativeInfinity;
        }

        /// <summary>Parked slot at construction: South.</summary>
        public Direction ParkedSlot => Direction.South;

        /// <summary>Move target; the shield keeps travelling until it arrives.</summary>
        public void RequestDirection(Direction direction, double nowDsp)
        {
            double current = CurrentAngle(nowDsp);
            double target = DirectionToAngle(direction);
            if (Math.Abs(ShortestDelta(current, target)) < 1e-9)
            {
                return;
            }

            _legFromAngle = current;
            _legToAngle = target;
            _legStartDsp = nowDsp;
        }

        public float CurrentAngle(double nowDsp)
        {
            if (_transitMs <= 0.0 || double.IsNegativeInfinity(_legStartDsp))
            {
                return (float)_legToAngle;
            }

            double elapsedMs = (nowDsp - _legStartDsp) * 1000.0;
            if (elapsedMs <= 0.0)
            {
                return (float)_legFromAngle;
            }

            double t = elapsedMs >= _transitMs ? 1.0 : elapsedMs / _transitMs;
            double eased = t * t * (3.0 - 2.0 * t);
            return (float)(_legFromAngle + ShortestDelta(_legFromAngle, _legToAngle) * eased);
        }

        public bool CoversDirection(Direction direction, double nowDsp)
        {
            return Math.Abs(ShortestDelta(CurrentAngle(nowDsp), DirectionToAngle(direction)))
                   <= CoverageHalfAngleDeg;
        }

        public Direction NearestSlot(double nowDsp)
        {
            float angle = CurrentAngle(nowDsp);
            Direction best = Direction.North;
            double bestDistance = double.MaxValue;

            foreach (Direction slot in new[]
                     {
                         Direction.North, Direction.East, Direction.South, Direction.West
                     })
            {
                double distance = Math.Abs(ShortestDelta(angle, DirectionToAngle(slot)));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = slot;
                }
            }

            return best;
        }

        public static float DirectionToAngle(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return 0f;
                case Direction.East:
                    return 90f;
                case Direction.South:
                    return 180f;
                default:
                    return 270f;
            }
        }

        private static double ShortestDelta(double from, double to)
        {
            double delta = (to - from) % 360.0;
            if (delta > 180.0)
            {
                delta -= 360.0;
            }
            else if (delta < -180.0)
            {
                delta += 360.0;
            }

            return delta;
        }
    }
}
