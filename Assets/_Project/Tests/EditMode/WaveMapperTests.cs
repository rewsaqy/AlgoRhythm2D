using AlgoRhythm.Core.Combat;
using AlgoRhythm.Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    public sealed class WaveMapperTests
    {
        private static WaveDefinitionSO Definition()
        {
            var definition = ScriptableObject.CreateInstance<WaveDefinitionSO>();
            definition.waveId = "W1B";
            definition.tempoBpm = 100.0;
            definition.idOffset = 100;
            definition.spawnSequence = new[]
            {
                new WaveThreatEntry
                {
                    kind = ThreatKind.StraightShooter,
                    direction = Direction.East,
                    spawnBeat = 4,
                    impactBeat = 6,
                    baseDamage = 10
                },
                new WaveThreatEntry
                {
                    kind = ThreatKind.AdwareDecoy,
                    direction = Direction.West,
                    spawnBeat = 12,
                    impactBeat = 14,
                    baseDamage = 0
                }
            };
            return definition;
        }

        [Test]
        public void MapsRows_WithOffsetIds_PreservingKindAndBeats()
        {
            Core.Waves.WaveData data = WaveMapper.ToWaveData(Definition());

            Assert.AreEqual("W1B", data.WaveId);
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual(101, data.Threats[0].Id);
            Assert.AreEqual(ThreatKind.StraightShooter, data.Threats[0].Kind);
            Assert.AreEqual(Direction.East, data.Threats[0].Direction);
            Assert.AreEqual(102, data.Threats[1].Id);
            Assert.AreEqual(ThreatKind.AdwareDecoy, data.Threats[1].Kind);
            Assert.AreEqual(14, data.Threats[1].ImpactBeat);
        }

        [Test]
        public void NullDefinition_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => WaveMapper.ToWaveData(null));
        }
    }
}
