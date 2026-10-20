using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Waves;

namespace AlgoRhythm.Game.Data
{
    /// <summary>Converts an authored wave asset into immutable Core data.</summary>
    public static class WaveMapper
    {
        public static WaveData ToWaveData(WaveDefinitionSO definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            WaveThreatEntry[] rows = definition.spawnSequence ?? Array.Empty<WaveThreatEntry>();
            var threats = new List<ThreatEvent>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                WaveThreatEntry row = rows[index];
                threats.Add(new ThreatEvent(
                    definition.idOffset + index + 1,
                    row.kind,
                    row.direction,
                    row.spawnBeat,
                    row.impactBeat,
                    row.baseDamage));
            }

            return new WaveData(definition.waveId, definition.tempoBpm, threats);
        }
    }
}
