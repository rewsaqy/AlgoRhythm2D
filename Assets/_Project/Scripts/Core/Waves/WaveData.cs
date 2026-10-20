using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;

namespace AlgoRhythm.Core.Waves
{
    /// <summary>Temporary I-001 fixture. Production waves become ScriptableObjects on Day 2.</summary>
    public sealed class WaveData
    {
        private readonly ThreatEvent[] _threats;

        public WaveData(string waveId, double bpm, IEnumerable<ThreatEvent> threats)
        {
            if (string.IsNullOrWhiteSpace(waveId))
            {
                throw new ArgumentException("Wave id is required.", nameof(waveId));
            }

            if (bpm <= 0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            if (threats == null)
            {
                throw new ArgumentNullException(nameof(threats));
            }

            WaveId = waveId;
            Bpm = bpm;

            var copy = new List<ThreatEvent>(threats);
            _threats = copy.ToArray();
            Array.Sort(_threats, CompareThreats);
        }

        public string WaveId { get; }

        public double Bpm { get; }

        public IReadOnlyList<ThreatEvent> Threats => _threats;

        public int Count => _threats.Length;

        private static int CompareThreats(ThreatEvent left, ThreatEvent right)
        {
            int spawnComparison = left.SpawnBeat.CompareTo(right.SpawnBeat);
            return spawnComparison != 0
                ? spawnComparison
                : left.Id.CompareTo(right.Id);
        }
    }
}
