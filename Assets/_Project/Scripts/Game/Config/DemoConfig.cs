using UnityEngine;

namespace AlgoRhythm.Game.Config
{
    /// <summary>
    /// I-001 greybox tuning. No values are scattered through gameplay code.
    /// Production config will be upgraded on Day 2 when wave assets exist.
    /// </summary>
    [CreateAssetMenu(menuName = "AlgoRhythm/Demo Config", fileName = "DemoConfig")]
    public sealed class DemoConfig : ScriptableObject
    {
        [Header("Rhythm")]
        [Min(1.0f)] public double bpm = 100.0;
        [Min(0.0f)] public double perfectWindowMs = 70.0;
        [Min(0.0f)] public double goodWindowMs = 130.0;
        [Min(0.0f)] public int inputBufferMs = 120;
        [Range(-150.0f, 150.0f)] public double calibrationOffsetMs = 0.0;
        [Min(0)] public int startLeadInBeats = 2;

        [Header("Combat")]
        [Min(0)] public int maxIntegrity = 100;
        [Min(0)] public int startingEnergy = 6;
        [Min(0)] public int straightShooterDamage = 10;
    }
}
