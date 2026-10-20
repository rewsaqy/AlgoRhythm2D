using AlgoRhythm.Core.Combat;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class ShieldControllerTests
    {
        [Test]
        public void InitialState_ParkedSouthCoveringSouthOnly()
        {
            var shield = new ShieldController(transitMs: 180.0);

            Assert.AreEqual(Direction.South, shield.ParkedSlot);
            Assert.IsTrue(shield.CoversDirection(Direction.South, 100.0));
            Assert.IsFalse(shield.CoversDirection(Direction.North, 100.0));
            Assert.IsFalse(shield.CoversDirection(Direction.East, 100.0));
            Assert.IsFalse(shield.CoversDirection(Direction.West, 100.0));
        }

        [Test]
        public void RequestArrivesAfterTransit_CoveringTarget()
        {
            var shield = new ShieldController(transitMs: 180.0);
            shield.RequestDirection(Direction.East, 10.0);

            Assert.IsFalse(shield.CoversDirection(Direction.East, 10.0));
            Assert.AreEqual(Direction.South, shield.NearestSlot(10.0));

            Assert.IsTrue(shield.CoversDirection(Direction.East, 10.0 + 0.18));
            Assert.AreEqual(Direction.East, shield.NearestSlot(10.0 + 0.18));
        }

        [Test]
        public void MidTransit_CoversNoLane()
        {
            // South (180°) to East (90°): smoothstep(0.5) = 0.5 → 135°.
            var shield = new ShieldController(transitMs: 100.0);
            shield.RequestDirection(Direction.East, 5.0);

            float angle = shield.CurrentAngle(5.0 + 0.05);

            Assert.AreEqual(135.0, angle, 0.001);
            Assert.IsFalse(shield.CoversDirection(Direction.South, 5.0 + 0.05));
            Assert.IsFalse(shield.CoversDirection(Direction.East, 5.0 + 0.05));
            Assert.IsFalse(shield.CoversDirection(Direction.North, 5.0 + 0.05));
            Assert.IsFalse(shield.CoversDirection(Direction.West, 5.0 + 0.05));
        }

        [Test]
        public void RetargetMidTransit_KeepsPositionContinuous()
        {
            var shield = new ShieldController(transitMs: 100.0);
            shield.RequestDirection(Direction.East, 5.0);

            float before = shield.CurrentAngle(5.0 + 0.05);
            shield.RequestDirection(Direction.North, 5.0 + 0.05);
            float after = shield.CurrentAngle(5.0 + 0.05);

            Assert.AreEqual(before, after, 1e-6);
        }

        [Test]
        public void TakesShortestPath()
        {
            // North (0°) to West (270°): shortest delta is −90°.
            var shield = new ShieldController(transitMs: 100.0);
            shield.RequestDirection(Direction.North, 0.0);
            shield.RequestDirection(Direction.West, 1.0);

            float mid = shield.CurrentAngle(1.0 + 0.05);

            Assert.AreEqual(-45.0, mid, 0.001);
            Assert.IsTrue(shield.CoversDirection(Direction.West, 1.0 + 0.1));
        }

        [Test]
        public void SameSlotRequest_IsNoOp()
        {
            var shield = new ShieldController(transitMs: 180.0);
            shield.RequestDirection(Direction.South, 7.0);

            Assert.IsTrue(shield.CoversDirection(Direction.South, 7.0));
            Assert.IsTrue(shield.CoversDirection(Direction.South, 7.0 + 1.0));
        }
    }
}
