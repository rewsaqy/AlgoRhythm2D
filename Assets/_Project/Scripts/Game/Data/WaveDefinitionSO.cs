using System;
using UnityEngine;

namespace AlgoRhythm.Game.Data
{
    /// <summary>One wave row authored in the inspector. Mapped to Core WaveData.</summary>
    [Serializable]
    public struct WaveThreatEntry
    {
        public AlgoRhythm.Core.Combat.ThreatKind kind;
        public AlgoRhythm.Core.Combat.Direction direction;
        [Min(0)] public int spawnBeat;
        [Min(0)] public int impactBeat;
        [Min(0)] public int baseDamage;
    }

    /// <summary>
    /// Wave definition asset. Threat ids are assigned as idOffset + row + 1 so
    /// waves never share ids (the result guard is per run, not per wave).
    /// </summary>
    [CreateAssetMenu(menuName = "AlgoRhythm/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string waveId = "W1A";
        [Min(1.0f)] public double tempoBpm = 100.0;
        public string difficultyTier = "Tutorial";
        [Min(0)] public int idOffset;

        [Header("Content")]
        public WaveThreatEntry[] spawnSequence = Array.Empty<WaveThreatEntry>();

        [Header("Rules")]
        [Tooltip("Token names pre-filled in the code panel, e.g. WHEN_PROJECTILE, RAISE_SHIELD.")]
        public string[] ruleUnlocks = Array.Empty<string>();

        [Header("Flow")]
        public string dialogueTrigger = "OnStart";
    }
}
