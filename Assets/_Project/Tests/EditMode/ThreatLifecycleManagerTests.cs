using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Config;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    [TestFixture]
    public sealed class ThreatLifecycleManagerTests
    {
        private sealed class FakeClockSource : IClockSource
        {
            public double NowDsp { get; set; }
        }

        private DemoConfig _config;
        private FakeClockSource _source;
        private BeatClock _clock;
        private TimingJudge _judge;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DemoConfig>();
            _config.paddleHalfArcDeg = 15f;
            _config.heavyArmorHits = 3;

            _source = new FakeClockSource { NowDsp = 10.0 };
            _clock = new BeatClock(_source, 100.0); // 0.6s per beat
            _clock.Start(10.0);
            _judge = new TimingJudge(70.0, 130.0);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void ArmorFor_ReturnsCorrectHitsPerArchetype()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);

            Assert.AreEqual(3, manager.ArmorFor(ThreatKind.Heavy));
            Assert.AreEqual(1, manager.ArmorFor(ThreatKind.StraightShooter));
            Assert.AreEqual(1, manager.ArmorFor(ThreatKind.AdwareDecoy));
        }

        [Test]
        public void FindBestTapCandidate_OnlyReturnsWhenPaddleOverlaps()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);
            // Spawn a wave with 2 threats
            var threats = new List<ThreatEvent>
            {
                new ThreatEvent(1, ThreatKind.StraightShooter, Direction.North, angleDeg: 0f, halfWidthDeg: 10f, spawnBeat: 0, impactBeat: 2, baseDamage: 10),
                new ThreatEvent(2, ThreatKind.StraightShooter, Direction.East, angleDeg: 90f, halfWidthDeg: 10f, spawnBeat: 0, impactBeat: 2, baseDamage: 10)
            };
            var wave = new WaveRunner(new WaveData("TEST", 100, threats), _clock);
            manager.SpawnDue(wave, 0, null);

            // Paddle at North (0 deg) with half-arc 15 deg covers threat 1 (0 deg, half-width 10)
            double impactDsp = _clock.BeatTime(2);
            ActiveThreat candidateNorth = manager.FindBestTapCandidate(0f, impactDsp);
            Assert.IsNotNull(candidateNorth);
            Assert.AreEqual(1, candidateNorth.Threat.Id);

            // Paddle at South (180 deg) covers neither
            ActiveThreat candidateSouth = manager.FindBestTapCandidate(180f, impactDsp);
            Assert.IsNull(candidateSouth);

            // Paddle at East (90 deg) covers threat 2
            ActiveThreat candidateEast = manager.FindBestTapCandidate(90f, impactDsp);
            Assert.IsNotNull(candidateEast);
            Assert.AreEqual(2, candidateEast.Threat.Id);
        }

        [Test]
        public void FindBestTapCandidate_SelectsClosestTimingWhenMultipleOverlap()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);
            var threats = new List<ThreatEvent>
            {
                new ThreatEvent(1, ThreatKind.StraightShooter, Direction.North, angleDeg: 0f, halfWidthDeg: 10f, spawnBeat: 0, impactBeat: 2, baseDamage: 10),
                new ThreatEvent(2, ThreatKind.StraightShooter, Direction.North, angleDeg: 5f, halfWidthDeg: 10f, spawnBeat: 0, impactBeat: 4, baseDamage: 10)
            };
            var wave = new WaveRunner(new WaveData("TEST", 100, threats), _clock);
            manager.SpawnDue(wave, 0, null);

            // Paddle at 0 deg covers both; input closest to beat 2
            double inputDsp = _clock.BeatTime(2) + 0.02;
            ActiveThreat candidate = manager.FindBestTapCandidate(0f, inputDsp);

            Assert.IsNotNull(candidate);
            Assert.AreEqual(1, candidate.Threat.Id);
        }

        [Test]
        public void FindActiveById_FindsExistingAndReturnsNullForMissing()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);
            var threats = new List<ThreatEvent>
            {
                new ThreatEvent(42, ThreatKind.StraightShooter, Direction.North, 0, 2, 10)
            };
            var wave = new WaveRunner(new WaveData("TEST", 100, threats), _clock);
            manager.SpawnDue(wave, 0, null);

            Assert.IsNotNull(manager.FindActiveById(42));
            Assert.IsNull(manager.FindActiveById(999));
        }

        [Test]
        public void FindAidaTarget_IgnoresDecoysAndTooCloseNotes()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);
            var threats = new List<ThreatEvent>
            {
                // Threat 1: Decoy -> ignored
                new ThreatEvent(1, ThreatKind.AdwareDecoy, Direction.North, 0, 4, 0),
                // Threat 2: Impact beat 2 when current beat is 1 (diff = 1 < 2) -> ignored (too close)
                new ThreatEvent(2, ThreatKind.StraightShooter, Direction.East, 0, 2, 10),
                // Threat 3: Impact beat 5 when current beat is 1 (diff = 4 >= 2) -> selected!
                new ThreatEvent(3, ThreatKind.StraightShooter, Direction.South, 0, 5, 10)
            };
            var wave = new WaveRunner(new WaveData("TEST", 100, threats), _clock);
            manager.SpawnDue(wave, 0, null);

            ActiveThreat target = manager.FindAidaTarget(beat: 1, now: 10.0);

            Assert.IsNotNull(target);
            Assert.AreEqual(3, target.Threat.Id);
        }

        [Test]
        public void Clear_EmptiesActiveQueue()
        {
            var manager = new ThreatLifecycleManager(null, _clock, _judge, _config);
            var threats = new List<ThreatEvent>
            {
                new ThreatEvent(1, ThreatKind.StraightShooter, Direction.North, 0, 2, 10)
            };
            var wave = new WaveRunner(new WaveData("TEST", 100, threats), _clock);
            manager.SpawnDue(wave, 0, null);
            Assert.AreEqual(1, manager.ActiveCount);

            manager.Clear();
            Assert.AreEqual(0, manager.ActiveCount);
        }
    }
}
