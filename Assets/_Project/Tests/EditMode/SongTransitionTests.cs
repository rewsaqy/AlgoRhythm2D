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

        [Test]
        public void SongPlayer_PlayWaveSong_SetsPitchCorrectly()
        {
            var go = new GameObject("TestSongPlayerPitch");
            try
            {
                var player = go.AddComponent<SongPlayer>();
                player.Initialize();

                AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
                try
                {
                    player.PlayWaveSong(testClip, false, AudioSettings.dspTime + 1.0, isRetry: false, pitch: 1.25f);
                    AudioSource[] sources = go.GetComponents<AudioSource>();
                    bool pitchSet = false;
                    foreach (var s in sources)
                    {
                        if (Mathf.Approximately(s.pitch, 1.25f))
                        {
                            pitchSet = true;
                            break;
                        }
                    }
                    Assert.IsTrue(pitchSet, "SongPlayer should configure AudioSource pitch according to the pitch argument.");
                }
                finally
                {
                    Object.DestroyImmediate(testClip);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void Waves_WS3_WS4_WS5_UseWave1AndWave2Clips_WithMatchingBeatsAndElevatedTempo()
        {
            var ws1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AlgoRhythm.Game.Data.WaveDefinitionSO>("Assets/_Project/Data/Waves/WS1.asset");
            var ws2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AlgoRhythm.Game.Data.WaveDefinitionSO>("Assets/_Project/Data/Waves/WS2.asset");
            var ws3 = UnityEditor.AssetDatabase.LoadAssetAtPath<AlgoRhythm.Game.Data.WaveDefinitionSO>("Assets/_Project/Data/Waves/WS3.asset");
            var ws4 = UnityEditor.AssetDatabase.LoadAssetAtPath<AlgoRhythm.Game.Data.WaveDefinitionSO>("Assets/_Project/Data/Waves/WS4.asset");
            var ws5 = UnityEditor.AssetDatabase.LoadAssetAtPath<AlgoRhythm.Game.Data.WaveDefinitionSO>("Assets/_Project/Data/Waves/WS5.asset");

            Assert.IsNotNull(ws1, "WS1 asset missing");
            Assert.IsNotNull(ws2, "WS2 asset missing");
            Assert.IsNotNull(ws3, "WS3 asset missing");
            Assert.IsNotNull(ws4, "WS4 asset missing");
            Assert.IsNotNull(ws5, "WS5 asset missing");

            // WS3 checks: uses Wave_1 clip, faster BPM (120 vs 100), identical impact beat rhythm
            Assert.IsNotNull(ws3.songClip);
            StringAssert.Contains("wave_1", ws3.songClip.name.ToLowerInvariant(), "WS3 must use Wave 1 song");
            Assert.AreEqual(ws1.songClip, ws3.songClip, "WS3 song clip must be identical to WS1 song clip");
            Assert.Greater(ws3.tempoBpm, ws1.tempoBpm, "WS3 tempo must be faster than WS1");
            Assert.AreEqual(120.0, ws3.tempoBpm);
            Assert.AreEqual(ws1.spawnSequence.Length, ws3.spawnSequence.Length, "WS3 must have matching number of notes to WS1");
            for (int i = 0; i < ws1.spawnSequence.Length; i++)
            {
                Assert.AreEqual(ws1.spawnSequence[i].impactBeat, ws3.spawnSequence[i].impactBeat, $"WS3 note {i} impact beat must match WS1");
            }

            // WS4 checks: uses WAVE_2 clip, faster BPM (125 vs 115), identical impact beat rhythm
            Assert.IsNotNull(ws4.songClip);
            StringAssert.Contains("wave_2", ws4.songClip.name.ToLowerInvariant(), "WS4 must use Wave 2 song");
            Assert.AreEqual(ws2.songClip, ws4.songClip, "WS4 song clip must be identical to WS2 song clip");
            Assert.Greater(ws4.tempoBpm, ws2.tempoBpm, "WS4 tempo must be faster than WS2");
            Assert.AreEqual(125.0, ws4.tempoBpm);
            Assert.AreEqual(ws2.spawnSequence.Length, ws4.spawnSequence.Length, "WS4 must have matching number of notes to WS2");
            for (int i = 0; i < ws2.spawnSequence.Length; i++)
            {
                Assert.AreEqual(ws2.spawnSequence[i].impactBeat, ws4.spawnSequence[i].impactBeat, $"WS4 note {i} impact beat must match WS2");
            }

            // WS5 checks: uses WAVE_2 clip, faster BPM (130 vs 125), identical impact beat rhythm
            Assert.IsNotNull(ws5.songClip);
            StringAssert.Contains("wave_2", ws5.songClip.name.ToLowerInvariant(), "WS5 must use Wave 2 song");
            Assert.AreEqual(ws2.songClip, ws5.songClip, "WS5 song clip must be identical to WS2 song clip");
            Assert.Greater(ws5.tempoBpm, ws4.tempoBpm, "WS5 tempo must be faster than WS4");
            Assert.AreEqual(130.0, ws5.tempoBpm);
            Assert.AreEqual(ws2.spawnSequence.Length, ws5.spawnSequence.Length, "WS5 must have matching number of notes to WS2");
            for (int i = 0; i < ws2.spawnSequence.Length; i++)
            {
                Assert.AreEqual(ws2.spawnSequence[i].impactBeat, ws5.spawnSequence[i].impactBeat, $"WS5 note {i} impact beat must match WS2");
            }
        }
#endif
    }
}
