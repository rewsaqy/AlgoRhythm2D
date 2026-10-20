namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// One telegraphed threat. SpawnBeat is the beat on which the cue appears;
    /// ImpactBeat is the beat on which the player must shield.
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
        {
            Id = id;
            Kind = kind;
            Direction = direction;
            SpawnBeat = spawnBeat;
            ImpactBeat = impactBeat;
            BaseDamage = baseDamage;
        }

        public int Id { get; }

        public ThreatKind Kind { get; }

        public Direction Direction { get; }

        public int SpawnBeat { get; }

        public int ImpactBeat { get; }

        public int BaseDamage { get; }
    }
}
