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

        [Header("Rules")]
        [Min(0)] public int ruleEnergyCost = 1;
        [Min(0)] public int ruleCooldownMs = 1500;
        [Min(1)] public int energyRegenEveryBeats = 4;

        [Header("Shield")]
        [Min(0.0f)] public double shieldTransitMs = 320.0;

        [Header("Code Panel")]
        public bool enableCodePanel = false;

        [Header("Camera Shake")]
        [Min(0.0f)] public float shakePerfect = 1.0f;
        [Min(0.0f)] public float shakeDestroy = 0.5f;
        [Min(0.0f)] public float shakeDecayPerSecond = 2.5f;
        [Min(0.0f)] public float shakeMaxOffset = 0.2f;

        [Header("AIDA Tower")]
        [Min(1)] public int aidaFireEveryBeats = 4;
        [Min(1)] public int aidaHitEveryShots = 8;
        public string[] aidaFlavorLines =
        {
            "tembak siapa?",
            "apa yang ditembak?",
            "eh itu musuh ya?"
        };

        [Header("Notes")]
        [Range(0.05f, 0.9f)] public float noteStepAttack = 0.25f;
    }
}
