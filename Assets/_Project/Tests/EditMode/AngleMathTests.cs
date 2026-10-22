using AlgoRhythm.Core.Combat;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class AngleMathTests
    {
        [Test]
        public void ShortestDelta_Wraps359To1_ViaShortArc()
        {
            Assert.AreEqual(2.0, AngleMath.ShortestDelta(359f, 1f), 1e-6);
            Assert.AreEqual(-2.0, AngleMath.ShortestDelta(1f, 359f), 1e-6);
        }

        [Test]
        public void Overlaps_EdgeInclusive()
        {
            // Tepi tepat: |10 - 0| = 10 = halfA(0) + halfB(10) → overlap.
            Assert.IsTrue(AngleMath.Overlaps(10f, 0f, 0f, 10f));
            Assert.IsFalse(AngleMath.Overlaps(10.1f, 0f, 0f, 10f));
        }

        [Test]
        public void Overlaps_WrapsAroundNorth()
        {
            Assert.IsTrue(AngleMath.Overlaps(359f, 10f, 1f, 35f));
        }

        [Test]
        public void AngleFromVector_CompassConvention()
        {
            Assert.AreEqual(0f, AngleMath.AngleFromVector(0f, 1f), 1e-4);
            Assert.AreEqual(90f, AngleMath.AngleFromVector(1f, 0f), 1e-4);
            Assert.AreEqual(180f, AngleMath.AngleFromVector(0f, -1f), 1e-4);
            Assert.AreEqual(270f, AngleMath.AngleFromVector(-1f, 0f), 1e-4);
        }
    }
}
