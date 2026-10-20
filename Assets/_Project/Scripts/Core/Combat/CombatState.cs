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

        private readonly HashSet<int> _resolvedThreatIds = new HashSet<int>();
        private int _damageTaken;

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

        public int MaxIntegrity { get; }

        public int Integrity { get; private set; }

        public int Energy { get; private set; }

        public int RuleBonus { get; }

        public int PerfectCount { get; private set; }

        public int GoodCount { get; private set; }

        public int MissCount { get; private set; }

        public int DamageTaken => _damageTaken;

        public int Score
        {
            get
            {
                int raw = PerfectCount * 100
                          + GoodCount * 60
                          - MissCount * 40
                          - _damageTaken * 25
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
