namespace AlgoRhythm.Core.Combat
{
    /// <summary>Mode gerak note: Linear mulus atau Stepped per-beat (lawas).</summary>
    public enum NoteMotionMode
    {
        Linear = 0,
        Stepped = 1
    }

    /// <summary>
    /// Progres travel note sebagai fungsi DSP murni (0 = spawn, 1 = impact).
    /// Dipindah dari CombatController.BeatGlideProgress agar bisa di-test EditMode.
    /// </summary>
    public static class NoteMotion
    {
        public static float Progress(
            double spawnDsp,
            double impactDsp,
            double nowDsp,
            double secondsPerBeat,
            NoteMotionMode mode,
            float attack)
        {
            if (impactDsp <= spawnDsp)
            {
                return 1f;
            }

            if (nowDsp <= spawnDsp)
            {
                return 0f;
            }

            if (nowDsp >= impactDsp)
            {
                return 1f;
            }

            if (mode == NoteMotionMode.Linear)
            {
                double t = (nowDsp - spawnDsp) / (impactDsp - spawnDsp);
                return (float)t;
            }

            // Stepped: threat menahan sebagian besar tiap beat, lalu melangkah
            // cepat di fraksi attack terakhir, smoothstep di dalam langkah.
            if (secondsPerBeat <= 0.0)
            {
                double t = (nowDsp - spawnDsp) / (impactDsp - spawnDsp);
                return (float)t;
            }

            double beatsSinceSpawn = (nowDsp - spawnDsp) / secondsPerBeat;
            double travelBeats = (impactDsp - spawnDsp) / secondsPerBeat;
            if (travelBeats <= 0.0)
            {
                return 1f;
            }

            float clampedAttack = attack < 0.05f ? 0.05f : (attack > 0.9f ? 0.9f : attack);
            double tile = System.Math.Floor(beatsSinceSpawn);
            double frac = beatsSinceSpawn - tile;
            double eased = tile;
            if (frac >= 1.0 - clampedAttack)
            {
                double u = (frac - (1.0 - clampedAttack)) / clampedAttack;
                eased = tile + u * u * (3.0 - 2.0 * u);
            }

            double progress = eased / travelBeats;
            if (progress < 0.0) progress = 0.0;
            if (progress > 1.0) progress = 1.0;
            return (float)progress;
        }
    }
}
