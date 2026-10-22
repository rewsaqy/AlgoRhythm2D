namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// One telegraphed threat. SpawnBeat is the beat on which the cue appears;
    /// ImpactBeat is the beat on which the player must shield.
    /// AngleDeg is the free paddle angle (compass, 0 = North); HalfWidthDeg is
    /// the angular half-size of the note. Direction is kept for legacy waves
    /// (W1A–W1D) and maps N/E/S/W to 0/90/180/270.
    /// </summary>
    public readonly struct ThreatEvent
    {
        public ThreatEvent(
            int id,
            ThreatKind kind,
            Direction direction,
            int spawnBeat,
            int impactBeat,
            int baseDamage)
            : this(id, kind, direction, AngleMath.DirectionToAngle(direction), 10f,
                spawnBeat, impactBeat, baseDamage)
        {
        }

        public ThreatEvent(
            int id,
            ThreatKind kind,
            Direction direction,
            float angleDeg,
            float halfWidthDeg,
            int spawnBeat,
            int impactBeat,
            int baseDamage)
        {
            Id = id;
            Kind = kind;
            Direction = direction;
            AngleDeg = Normalize(angleDeg);
            HalfWidthDeg = halfWidthDeg < 0f ? 0f : halfWidthDeg;
            SpawnBeat = spawnBeat;
            ImpactBeat = impactBeat;
            BaseDamage = baseDamage;
        }

        public int Id { get; }

        public ThreatKind Kind { get; }

        public Direction Direction { get; }

        /// <summary>Free angle in degrees [0, 360).</summary>
        public float AngleDeg { get; }

        /// <summary>Angular half-width in degrees.</summary>
        public float HalfWidthDeg { get; }

        public int SpawnBeat { get; }

        public int ImpactBeat { get; }

        public int BaseDamage { get; }

        private static float Normalize(float angleDeg)
        {
            float a = angleDeg % 360f;
            if (a < 0f) a += 360f;
            return a;
        }
    }
}
