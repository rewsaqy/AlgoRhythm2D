using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Presentation;
using AlgoRhythm.Game.Rhythm;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    public sealed class SongTransitionTests
    {
        private sealed class FakeClockSource : IClockSource
        {
            public double NowDsp { get; set; } = 100.0;
        }

        [Test]
        public void WaveDirector_UsesBeatTimeAligner_WhenAvailable()
        {
            var clockSource = new DspClockSource();
            var clock = new BeatClock(new FakeClockSource(), 100.0);
            var director = new WaveDirector(
                new[] { new WaveData("W1T", 100.0, new ThreatEvent[0]) },
                clock,
                clockSource,
                2);

            bool alignerCalled = false;
            director.BeatTimeAligner = (leadInBeats, spb) =>
            {
                alignerCalled = true;
                Assert.AreEqual(2, leadInBeats);
                Assert.AreEqual(0.6, spb, 1e-6);
                return 500.0;
            };

            director.StartWave(0);
            Assert.IsTrue(alignerCalled);
            Assert.AreEqual(500.0, clock.OriginDsp, 1e-6);
        }

        [Test]
        public void WaveDirector_BypassesBeatTimeAligner_OnRetry()
        {
            var clockSource = new DspClockSource();
            var fakeSource = new FakeClockSource { NowDsp = 200.0 };
            var clock = new BeatClock(fakeSource, 100.0);
            var director = new WaveDirector(
                new[] { new WaveData("W1T", 100.0, new ThreatEvent[0]) },
                clock,
                clockSource,
                2);

            int alignerCalls = 0;
            director.BeatTimeAligner = (leadInBeats, spb) =>
            {
                alignerCalls++;
                return 999.0;
            };

            // First start: uses aligner
            director.StartWave(0);
            Assert.AreEqual(1, alignerCalls);
            Assert.AreEqual(999.0, clock.OriginDsp, 1e-6);

            // Retry: bypasses aligner
            director.RetryCurrent();
            Assert.AreEqual(1, alignerCalls); // Not incremented!
            Assert.IsFalse(director.IsRetrying);
            // OriginDsp was set from clockSource.NowDsp + 2 * 0.6
            Assert.AreNotEqual(999.0, clock.OriginDsp);
        }

        [Test]
        public void SongPlayer_GetNextBeatAlignedDspTime_ReturnsUnaligned_WhenNotPlaying()
        {
            var go = new GameObject("TestSongPlayer");
            try
            {
                var player = go.AddComponent<SongPlayer>();
                player.Initialize();

                Assert.IsFalse(player.IsPlaying);
                double leadIn = 1.2;
                double spb = 0.6;
                double alignedDsp = player.GetNextBeatAlignedDspTime(leadIn, spb, 4);

                // When not playing, should return AudioSettings.dspTime + leadIn
                Assert.AreEqual(AudioSettings.dspTime + leadIn, alignedDsp, 0.05);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SongPlayer_Initialize_ConfiguresDualAudioSources()
        {
            var go = new GameObject("TestSongPlayer");
            try
            {
                var player = go.AddComponent<SongPlayer>();
                player.Initialize();

                AudioSource[] sources = go.GetComponents<AudioSource>();
                Assert.GreaterOrEqual(sources.Length, 2, "SongPlayer should ensure at least 2 AudioSources exist for crossfading.");

                for (int i = 0; i < sources.Length; i++)
                {
                    Assert.IsFalse(sources[i].playOnAwake);
                    Assert.AreEqual(0f, sources[i].spatialBlend);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
