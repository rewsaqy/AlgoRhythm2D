using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;

namespace AlgoRhythm.Core.Waves
{
    /// <summary>
    /// Spawns threats on their due beat and tracks resolution. Due() is
    /// allocation-free and leaves overflow threats for the next call when the
    /// caller's buffer is full.
    /// </summary>
    public sealed class WaveRunner
    {
        private readonly WaveData _data;
        private readonly HashSet<int> _resolvedThreatIds = new HashSet<int>();
        private int _nextIndex;

        public WaveRunner(WaveData data, BeatClock clock)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public BeatClock Clock { get; }

        public WaveData Data => _data;

        public int SpawnedCount => _nextIndex;

        public bool AllSpawned => _nextIndex >= _data.Count;

        public bool IsCleared => AllSpawned && _resolvedThreatIds.Count >= _data.Count;

        public int Due(int currentBeat, Span<ThreatEvent> buffer)
        {
            int written = 0;
            while (_nextIndex < _data.Count && written < buffer.Length)
            {
                ThreatEvent threat = _data.Threats[_nextIndex];
                if (threat.SpawnBeat > currentBeat)
                {
                    break;
                }

                buffer[written] = threat;
                written++;
                _nextIndex++;
            }

            return written;
        }

        public void MarkResolved(int threatId)
        {
            _resolvedThreatIds.Add(threatId);
        }

        public bool IsResolved(int threatId)
        {
            return _resolvedThreatIds.Contains(threatId);
        }

        public void Reset()
        {
            _nextIndex = 0;
            _resolvedThreatIds.Clear();
        }
    }
}
