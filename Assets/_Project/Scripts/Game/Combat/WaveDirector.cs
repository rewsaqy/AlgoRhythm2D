using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Rhythm;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Sequences sub-waves on one shared beat clock. Each wave owns a fresh
    /// runner over its own relative beat space; starting (or retrying) a wave
    /// restarts the clock origin. Combat state carries across waves and is
    /// only reset on retry — that reset is owned by the caller.
    /// </summary>
    public sealed class WaveDirector
    {
        private readonly List<WaveRunner> _runners = new List<WaveRunner>();
        private readonly BeatClock _clock;
        private readonly DspClockSource _clockSource;
        private readonly int _leadInBeats;
        private int _activeIndex;

        public WaveDirector(
            IReadOnlyList<WaveData> waves,
            BeatClock clock,
            DspClockSource clockSource,
            int leadInBeats)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            if (waves.Count == 0)
            {
                throw new ArgumentException("At least one wave is required.", nameof(waves));
            }

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _clockSource = clockSource ?? throw new ArgumentNullException(nameof(clockSource));
            _leadInBeats = leadInBeats;

            foreach (WaveData wave in waves)
            {
                _runners.Add(new WaveRunner(wave, clock));
            }
        }

        public event Action<int> WaveStarted;

        public event Action<int> WaveCleared;

        public WaveRunner ActiveRunner => _runners[_activeIndex];

        public int ActiveIndex => _activeIndex;

        public int WaveCount => _runners.Count;

        public bool AllWavesCleared
        {
            get
            {
                for (int index = 0; index < _runners.Count; index++)
                {
                    if (!_runners[index].IsCleared)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void StartWave(int index)
        {
            if (index < 0 || index >= _runners.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _activeIndex = index;
            _runners[index].Reset();
            // Per-wave BPM (demo decision: W1 100, W2 115, W3 130). Retune
            // before the lead-in offset so it uses the new spacing.
            _clock.SetBpm(_runners[index].Data.Bpm);
            _clock.Start(_clockSource.NowDsp + _leadInBeats * _clock.SecondsPerBeat);
            WaveStarted?.Invoke(index);
        }

        public void RetryCurrent()
        {
            StartWave(_activeIndex);
        }

        /// <summary>
        /// Advances past a cleared wave. Returns false when the run is done.
        /// </summary>
        public bool Advance()
        {
            WaveCleared?.Invoke(_activeIndex);
            if (_activeIndex + 1 >= _runners.Count)
            {
                return false;
            }

            StartWave(_activeIndex + 1);
            return true;
        }
    }
}
