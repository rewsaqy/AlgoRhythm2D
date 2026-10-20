using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Rhythm;

namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Owns integrity, energy, scoring, and the one-final-result guard for
    /// resolved threats. It has no Unity dependency and is safe to test in
    /// EditMode.
    /// </summary>
    public sealed class CombatState
    {
        public const int DefaultMaxIntegrity = 100;
        public const int DefaultStartingEnergy = 6;
        public const int DecoyShieldPenalty = 5;

        private readonly HashSet<int> _resolvedThreatIds = new HashSet<int>();
        private int _damageTaken;
        private int _decoyPenaltyTotal;

        public CombatState(
            int maxIntegrity = DefaultMaxIntegrity,
            int startingEnergy = DefaultStartingEnergy,
            int ruleBonus = 0)
        {
            if (maxIntegrity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntegrity));
            }

            if (startingEnergy < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingEnergy));
            }

            if (ruleBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleBonus));
            }

            MaxIntegrity = maxIntegrity;
            Integrity = maxIntegrity;
            Energy = startingEnergy;
            RuleBonus = ruleBonus;
        }

        public int MaxIntegrity { get; private set; }

        public int Integrity { get; private set; }

        public int Energy { get; private set; }

        public int RuleBonus { get; }

        public int PerfectCount { get; private set; }

        public int GoodCount { get; private set; }

        public int MissCount { get; private set; }

        public int DamageTaken => _damageTaken;

        public int DecoyPenaltyTotal => _decoyPenaltyTotal;

        public int Score
        {
            get
            {
                int raw = PerfectCount * 100
                          + GoodCount * 60
                          - MissCount * 40
                          - _damageTaken * 25
                          - _decoyPenaltyTotal
                          + RuleBonus;
                return Math.Max(0, raw);
            }
        }

        public event Action<ThreatResolved> ThreatResolved;

        public event Action<int> IntegrityChanged;

        public bool IsResolved(int threatId)
        {
            return _resolvedThreatIds.Contains(threatId);
        }

        /// <summary>
        /// Resolves a threat exactly once. Damage is only applied for Miss.
        /// Returns false when the id was already resolved.
        /// </summary>
        public bool ResolveThreat(in ThreatEvent threat, TimingGrade grade, bool fromRule, int damage)
        {
            if (_resolvedThreatIds.Contains(threat.Id))
            {
                return false;
            }

            _resolvedThreatIds.Add(threat.Id);
            int scoreBefore = Score;

            if (grade == TimingGrade.Perfect)
            {
                PerfectCount++;
            }
            else if (grade == TimingGrade.Good)
            {
                GoodCount++;
            }
            else
            {
                MissCount++;
            }

            int actualDamage = 0;
            if (grade == TimingGrade.Miss)
            {
                actualDamage = Math.Max(0, damage);
                actualDamage = Math.Min(actualDamage, Integrity);
                if (actualDamage > 0)
                {
                    Integrity -= actualDamage;
                    _damageTaken += actualDamage;
                    IntegrityChanged?.Invoke(Integrity);
                }
            }

            int scoreDelta = Score - scoreBefore;
            ThreatResolved?.Invoke(
                new ThreatResolved(threat.Id, grade, fromRule, actualDamage, scoreDelta));
            return true;
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Energy < amount)
            {
                return false;
            }

            Energy -= amount;
            return true;
        }

        /// <summary>
        /// Resolves a decoy exactly once. A shielded decoy costs −5 with no
        /// grade counters and no damage; an ignored decoy expires neutrally.
        /// Shares the one-final-result guard with <see cref="ResolveThreat"/>.
        /// </summary>
        public bool ResolveDecoy(in ThreatEvent threat, bool shielded, bool fromRule, out int scoreDelta)
        {
            scoreDelta = 0;
            if (threat.Kind != ThreatKind.AdwareDecoy || _resolvedThreatIds.Contains(threat.Id))
            {
                return false;
            }

            _resolvedThreatIds.Add(threat.Id);
            if (!shielded)
            {
                return true;
            }

            int scoreBefore = Score;
            _decoyPenaltyTotal += DecoyShieldPenalty;
            scoreDelta = Score - scoreBefore;
            return true;
        }

        /// <summary>
        /// Fresh sub-wave attempt (A5 retry). Clears the result guard,
        /// counters, damage, and penalties; restores Integrity and Energy.
        /// </summary>
        public void Reset(int maxIntegrity, int startingEnergy)
        {
            if (maxIntegrity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntegrity));
            }

            if (startingEnergy < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingEnergy));
            }

            _resolvedThreatIds.Clear();
            _damageTaken = 0;
            _decoyPenaltyTotal = 0;
            MaxIntegrity = maxIntegrity;
            Integrity = maxIntegrity;
            Energy = startingEnergy;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
        }

        public void AddEnergy(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Energy += amount;
        }
    }
}
