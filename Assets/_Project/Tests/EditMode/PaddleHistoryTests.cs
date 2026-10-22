using AlgoRhythm.Core.Combat;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class PaddleHistoryTests
    {
        [Test]
        public void AngleAt_InterpolatesViaShortestArc()
        {
            var h = new PaddleHistory(8);
            h.Record(10.0, 359f);
            h.Record(10.1, 1f);
            // Tengah: 359 → 1 lewat 0° = 0°.
            Assert.AreEqual(0f, h.AngleAt(10.05), 0.5);
        }

        [Test]
        public void AngleAt_ClampsOutsideRange()
        {
            var h = new PaddleHistory(8);
            h.Record(10.0, 90f);
            h.Record(10.1, 180f);
            Assert.AreEqual(90f, h.AngleAt(9.0), 1e-4);
            Assert.AreEqual(180f, h.AngleAt(11.0), 1e-4);
        }

        [Test]
        public void RingBuffer_FullCapacity_KeepsNewest()
        {
            var h = new PaddleHistory(4);
            h.Record(1.0, 0f);
            h.Record(2.0, 90f);
            h.Record(3.0, 180f);
            h.Record(4.0, 270f);
            h.Record(5.0, 45f);
            Assert.AreEqual(4, h.Count);
            Assert.AreEqual(45f, h.AngleAt(5.0), 1e-4);
            Assert.AreEqual(90f, h.AngleAt(2.0), 1e-4);
        }
    }
}
