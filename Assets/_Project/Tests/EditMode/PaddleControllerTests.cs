using AlgoRhythm.Core.Combat;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    /// <summary>Port 1:1 dari ShieldControllerTests ke model paddle kontinu.</summary>
    public sealed class PaddleControllerTests
    {
        [Test]
        public void InitialState_ParkedSouth()
        {
            var paddle = new PaddleController(180f, 0f);
            Assert.AreEqual(180f, paddle.CurrentAngle, 1e-4);
            Assert.IsTrue(paddle.Covers(180f, 10f, 35f));
            Assert.IsFalse(paddle.Covers(0f, 10f, 35f));
        }

        [Test]
        public void SetTarget_Unlimited_Snaps()
        {
            var paddle = new PaddleController(180f, 0f);
            paddle.SetTarget(90f);
            paddle.Update(10.18, 10.0);
            Assert.AreEqual(90f, paddle.CurrentAngle, 1e-4);
        }

        [Test]
        public void MidTransit_WithLimit_CoversNoLane()
        {
            var paddle = new PaddleController(180f, 180f);
            paddle.SetTarget(90f);
            paddle.Update(5.25, 5.0);
            // 180 → 90 shortest = −90; 180°/s * 0.25 = 45° → 135°.
            Assert.AreEqual(135.0, paddle.CurrentAngle, 0.5);
            Assert.IsFalse(paddle.Covers(180f, 10f, 30f));
            Assert.IsFalse(paddle.Covers(90f, 10f, 30f));
        }

        [Test]
        public void RetargetMidTransit_KeepsPositionContinuous()
        {
            var paddle = new PaddleController(180f, 180f);
            paddle.SetTarget(90f);
            paddle.Update(5.05, 5.0);
            float before = paddle.CurrentAngle;
            paddle.SetTarget(0f);
            paddle.Update(5.05, 5.05);
            Assert.AreEqual(before, paddle.CurrentAngle, 1e-6);
        }

        [Test]
        public void TakesShortestPath()
        {
            var paddle = new PaddleController(0f, 0f);
            paddle.SetTarget(270f);
            paddle.Update(1.0, 0.0);
            Assert.AreEqual(270f, paddle.CurrentAngle, 1e-4);
            Assert.IsTrue(paddle.Covers(270f, 10f, 35f));
        }

        [Test]
        public void SameTarget_IsNoOp()
        {
            var paddle = new PaddleController(180f, 0f);
            paddle.SetTarget(180f);
            paddle.Update(7.0, 7.0);
            Assert.AreEqual(180f, paddle.CurrentAngle, 1e-4);
            paddle.Update(8.0, 7.0);
            Assert.AreEqual(180f, paddle.CurrentAngle, 1e-4);
        }

        [Test]
        public void Diagonal_DoesNotCoverCardinalNotes()
        {
            // Regresi STRICT: paddle di NE (45°) dengan half-arc 12° tidak boleh
            // menutupi note N (0°) maupun E (90°); jarak pusat 45° > 12°.
            // Jam 8 (240°) vs West (270°): delta 30° > 12° → tidak kena.
            var paddle = new PaddleController(45f, 0f);
            Assert.IsFalse(paddle.Covers(0f, 10f, 12f));
            Assert.IsFalse(paddle.Covers(90f, 10f, 12f));
            Assert.IsTrue(paddle.Covers(45f, 10f, 12f));

            var nearWest = new PaddleController(240f, 0f);
            Assert.IsFalse(nearWest.Covers(270f, 10f, 12f));
            var onWest = new PaddleController(270f, 0f);
            Assert.IsTrue(onWest.Covers(270f, 10f, 12f));
        }
    }
}
