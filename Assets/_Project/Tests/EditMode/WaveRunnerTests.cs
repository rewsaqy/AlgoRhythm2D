using System;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class WaveRunnerTests
    {
        private sealed class FakeClock : IClockSource
        {
            public double NowDsp { get; set; }
        }

        private static WaveData BuildWave(params ThreatEvent[] threats)
        {
            return new WaveData("test", 120.0, threats);
        }

        private static ThreatEvent Threat(int id, int spawnBeat)
        {
            return new ThreatEvent(
                id,
                ThreatKind.StraightShooter,
                Direction.North,
                spawnBeat,
                spawnBeat + 2,
                10);
        }

        [Test]
        public void DueDoesNotRepeatThreats()
        {
            var clock = new BeatClock(new FakeClock(), 120.0);
            clock.Start(0.0);
            var runner = new WaveRunner(BuildWave(Threat(1, 1)), clock);
            var buffer = new ThreatEvent[1];

            Assert.AreEqual(0, runner.Due(0, buffer));
            Assert.AreEqual(1, runner.Due(1, buffer));
            Assert.AreEqual(1, buffer[0].Id);
            Assert.AreEqual(0, runner.Due(1, buffer));
            Assert.IsTrue(runner.AllSpawned);
        }

        [Test]
        public void DueLeavesOverflowForNextCall()
        {
            var clock = new BeatClock(new FakeClock(), 120.0);
            clock.Start(0.0);
            var runner = new WaveRunner(
                BuildWave(Threat(1, 1), Threat(2, 1)),
                clock);
            var buffer = new ThreatEvent[1];

            Assert.AreEqual(1, runner.Due(1, buffer));
            Assert.AreEqual(1, buffer[0].Id);
            Assert.AreEqual(1, runner.Due(1, buffer));
            Assert.AreEqual(2, buffer[0].Id);
            Assert.AreEqual(0, runner.Due(1, buffer));
        }

        [Test]
        public void DueCatchesUpAfterSkippedBeats()
        {
            var clock = new BeatClock(new FakeClock(), 120.0);
            clock.Start(0.0);
            var runner = new WaveRunner(
                BuildWave(Threat(1, 1), Threat(2, 2), Threat(3, 3)),
                clock);
            var buffer = new ThreatEvent[4];

            Assert.AreEqual(3, runner.Due(10, buffer));
            Assert.AreEqual(1, buffer[0].Id);
            Assert.AreEqual(2, buffer[1].Id);
            Assert.AreEqual(3, buffer[2].Id);
        }

        [Test]
        public void IsClearedRequiresSpawnAndResolution()
        {
            var clock = new BeatClock(new FakeClock(), 120.0);
            clock.Start(0.0);
            var runner = new WaveRunner(BuildWave(Threat(1, 0), Threat(2, 0)), clock);
            var buffer = new ThreatEvent[2];

            Assert.IsFalse(runner.IsCleared);
            Assert.AreEqual(2, runner.Due(0, buffer));
            Assert.IsTrue(runner.AllSpawned);
            Assert.IsFalse(runner.IsCleared);

            runner.MarkResolved(1);
            Assert.IsFalse(runner.IsCleared);
            runner.MarkResolved(2);
            Assert.IsTrue(runner.IsCleared);
        }

        [Test]
        public void WaveDataSortsBySpawnBeatThenId()
        {
            var wave = BuildWave(Threat(3, 2), Threat(2, 1), Threat(1, 1));

            Assert.AreEqual(1, wave.Threats[0].Id);
            Assert.AreEqual(2, wave.Threats[1].Id);
            Assert.AreEqual(3, wave.Threats[2].Id);
        }
    }
}
