using AlgoRhythm.Core.Rhythm;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class BeatClockTests
    {
        private sealed class FakeClock : IClockSource
        {
            public double NowDsp { get; set; }
        }

        [Test]
        public void BeatTime_UsesBpmAndOrigin()
        {
            var source = new FakeClock { NowDsp = 10.0 };
            var clock = new BeatClock(source, 120.0);
            clock.Start(10.0);

            Assert.AreEqual(10.0, clock.BeatTime(0), 1e-9);
            Assert.AreEqual(10.5, clock.BeatTime(1), 1e-9);
            Assert.AreEqual(0.5, clock.SecondsPerBeat, 1e-9);
        }

        [Test]
        public void CurrentBeatIndex_AdvancesWithDspTime()
        {
            var source = new FakeClock { NowDsp = 10.0 };
            var clock = new BeatClock(source, 120.0);
            clock.Start(10.0);

            source.NowDsp = 10.49;
            Assert.AreEqual(0, clock.CurrentBeatIndex);

            source.NowDsp = 10.5;
            Assert.AreEqual(1, clock.CurrentBeatIndex);

            source.NowDsp = 11.49;
            Assert.AreEqual(2, clock.CurrentBeatIndex);
        }

        [Test]
        public void PauseFreezesNowSecondsAndBeatIndex()
        {
            var source = new FakeClock { NowDsp = 10.0 };
            var clock = new BeatClock(source, 120.0);
            clock.Start(10.0);

            source.NowDsp = 10.5;
            clock.Pause();
            source.NowDsp = 60.0;

            Assert.AreEqual(0.5, clock.NowSeconds, 1e-9);
            Assert.AreEqual(1, clock.CurrentBeatIndex);
            Assert.IsTrue(clock.IsPaused);
        }

        [Test]
        public void ResumeShiftsOriginByPauseDuration()
        {
            var source = new FakeClock { NowDsp = 10.0 };
            var clock = new BeatClock(source, 120.0);
            clock.Start(10.0);

            source.NowDsp = 10.5;
            clock.Pause();
            source.NowDsp = 12.5;
            clock.Resume();

            Assert.AreEqual(12.0, clock.OriginDsp, 1e-9);
            Assert.AreEqual(0.5, clock.NowSeconds, 1e-9);
            Assert.AreEqual(12.5, clock.BeatTime(1), 1e-9);
            Assert.IsFalse(clock.IsPaused);
        }

        [Test]
        public void BeatIndexAt_BeforeOrigin_IsNegative()
        {
            var source = new FakeClock { NowDsp = 9.5 };
            var clock = new BeatClock(source, 120.0);
            clock.Start(10.0);

            Assert.AreEqual(-1, clock.BeatIndexAt(9.5));
        }

        [Test]
        public void SetBpm_RetunesSecondsPerBeat_AndRejectsInvalid()
        {
            var source = new FakeClock { NowDsp = 10.0 };
            var clock = new BeatClock(source, 100.0);
            clock.Start(10.0);

            Assert.AreEqual(0.6, clock.SecondsPerBeat, 1e-9);

            clock.SetBpm(120.0);
            Assert.AreEqual(120.0, clock.Bpm);
            Assert.AreEqual(0.5, clock.SecondsPerBeat, 1e-9);
            Assert.AreEqual(10.5, clock.BeatTime(1), 1e-9);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.SetBpm(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => clock.SetBpm(double.NaN));
        }
    }
}
