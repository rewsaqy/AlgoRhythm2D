using AlgoRhythm.Core.Rhythm;

namespace AlgoRhythm.Core.Combat
{
    public readonly struct ThreatResolved
    {
        public ThreatResolved(
            int threatId,
            TimingGrade grade,
            bool fromRule,
            int damage,
            int scoreDelta)
        {
            ThreatId = threatId;
            Grade = grade;
            FromRule = fromRule;
            Damage = damage;
            ScoreDelta = scoreDelta;
        }

        public int ThreatId { get; }

        public TimingGrade Grade { get; }

        public bool FromRule { get; }

        public int Damage { get; }

        public int ScoreDelta { get; }
    }
}
