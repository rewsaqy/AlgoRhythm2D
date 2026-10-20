using System.Collections.Generic;
using AlgoRhythm.Core.Combat;

namespace AlgoRhythm.Core.Waves
{
    /// <summary>I-001 debug wave: one 1A-style straight-shooter direction per threat.</summary>
    public static class DebugWaveFactory
    {
        private static readonly Direction[] Directions =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        public static WaveData Create1AGreybox(double bpm = 100.0, int baseDamage = 10)
        {
            var threats = new List<ThreatEvent>(12);
            for (int index = 0; index < 12; index++)
            {
                int spawnBeat = 4 + index * 8;
                threats.Add(
                    new ThreatEvent(
                        id: index + 1,
                        kind: ThreatKind.StraightShooter,
                        direction: Directions[index % Directions.Length],
                        spawnBeat: spawnBeat,
                        impactBeat: spawnBeat + 2,
                        baseDamage: baseDamage));
            }

            return new WaveData("W1A_greybox_i001", bpm, threats);
        }
    }
}
