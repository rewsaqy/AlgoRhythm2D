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

        [Header("Paddle (Beatblock) [U]")]
        [Tooltip("Setengah busur paddle dalam derajat; penuh = 2x nilai ini. STRICT: kecil agar paddle harus tepat di jalur note (jam 8 tidak ikut nge-shield West).")]
        [Range(5f, 90f)] public float paddleHalfArcDeg = 12f;
        [Tooltip("Setengah lebar sudut note dalam derajat.")]
        [Range(1f, 45f)] public float noteHalfWidthDeg = 10f;
        [Tooltip("SpawnBeat = ImpactBeat - approachBeats untuk angleMode Free.")]
        [Min(1)] public int approachBeats = 8;
        [Tooltip("0 = Linear mulus, 1 = Stepped per-beat (default: diam-gerak sesuai beat).")]
        public int noteMotionMode = 1;
        [Tooltip("Radius tahan sudut di sekitar Core (px layar).")]
        [Min(0f)] public float aimDeadZonePx = 40f;
        [Tooltip("Lookback aim untuk klik kiri (ms).")]
        [Min(0f)] public float clickAimLookbackMs = 10f;
        [Tooltip("Batas kecepatan paddle derajat/detik; 0 = tak terbatas (snap instan).")]
        [Min(0f)] public float paddleMaxDegPerSec = 0f;
        [Tooltip("Radius spawn radial (world) untuk mode paddle; kecepatan sama semua sudut.")]
        [Min(1f)] public float radialSpawnDistance = 8.2f;

        [Header("Code Panel")]
        public bool enableCodePanel = false;

        [Header("Shop [U] — popup 2-track, Lv1-5 (owner 2026-10-23)")]
        [Tooltip("Biaya PTS untuk naik ke level berikutnya. Index = level saat ini (regen 0-4, AIDA 0-4 sebagai level-1).")]
        public int[] shopUpgradeCosts = { 30, 55, 85, 120, 160 };
        [Tooltip("Heal tiap wave clear per level regen 0-5. Lv0 = tidak regen.")]
        public int[] shopRegenPerClear = { 0, 10, 15, 20, 25, 40 };
        [Tooltip("Akurasi AIDA per level 1-5: tiap N tembakan kena 1.")]
        public int[] shopAidaHitEvery = { 8, 8, 7, 6, 5, 4 };
        [Tooltip("Tempo AIDA per level 1-5: tembak tiap N beat.")]
        public int[] shopAidaFireEvery = { 4, 4, 4, 3, 3, 2 };
        [Tooltip("Level maksimum tiap track.")]
        [Min(1)] public int shopMaxLevel = 5;

        [Header("New archetypes [U] — owner decision 2026-10-22")]
        [Tooltip("Hold-note sustain length in beats (Holder).")]
        [Min(1)] public int holdBeats = 4;
        [Tooltip("Successful hits to crack a Heavy (first cracks armor, last resolves).")]
        [Min(2)] public int heavyArmorHits = 2;

        [Header("Camera Shake")]
        [Min(0.0f)] public float shakePerfect = 1.0f;
        [Min(0.0f)] public float shakeDestroy = 0.5f;
        [Min(0.0f)] public float shakeDamage = 0.8f;
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
