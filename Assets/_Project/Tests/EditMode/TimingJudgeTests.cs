using AlgoRhythm.Core.Rhythm;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class TimingJudgeTests
    {
        private const double Impact = 10.0;

        [Test]
        public void PerfectWindowBoundariesAreInclusive()
        {
            var judge = new TimingJudge(70.0, 130.0);

            Assert.AreEqual(TimingGrade.Perfect, judge.Grade(Impact + 0.070, Impact));
            Assert.AreEqual(TimingGrade.Perfect, judge.Grade(Impact - 0.070, Impact));
            Assert.AreEqual(TimingGrade.Good, judge.Grade(Impact + 0.0701, Impact));
            Assert.AreEqual(TimingGrade.Good, judge.Grade(Impact - 0.0701, Impact));
        }

        [Test]
        public void GoodWindowBoundariesAreInclusive()
        {
            var judge = new TimingJudge(70.0, 130.0);

            Assert.AreEqual(TimingGrade.Good, judge.Grade(Impact + 0.130, Impact));
            Assert.AreEqual(TimingGrade.Good, judge.Grade(Impact - 0.130, Impact));
            Assert.AreEqual(TimingGrade.Miss, judge.Grade(Impact + 0.131, Impact));
            Assert.AreEqual(TimingGrade.Miss, judge.Grade(Impact - 0.131, Impact));
        }

        [Test]
        public void CalibrationShiftsTheJudgedError()
        {
            var judge = new TimingJudge(70.0, 130.0)
            {
                CalibrationOffsetMs = 150.0
            };

            Assert.AreEqual(0.0, judge.ErrorMs(Impact + 0.150, Impact), 1e-9);
            Assert.AreEqual(TimingGrade.Perfect, judge.Grade(Impact + 0.150, Impact));
        }

        [Test]
        public void CalibrationIsClampedToPlusMinus150Ms()
        {
            var judge = new TimingJudge();

            judge.CalibrationOffsetMs = 999.0;
            Assert.AreEqual(150.0, judge.CalibrationOffsetMs);

            judge.CalibrationOffsetMs = -999.0;
            Assert.AreEqual(-150.0, judge.CalibrationOffsetMs);
        }
    }
}
