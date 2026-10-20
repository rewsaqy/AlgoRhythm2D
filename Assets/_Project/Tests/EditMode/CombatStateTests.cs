using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class CombatStateTests
    {
        private static ThreatEvent Threat(int id = 1, int damage = 10)
        {
            return new ThreatEvent(
                id,
                ThreatKind.StraightShooter,
                Direction.North,
                spawnBeat: 0,
                impactBeat: 1,
                baseDamage: damage);
        }

        [Test]
        public void ResolveThreat_IsIdempotent()
        {
            var state = new CombatState();
            int eventCount = 0;
            state.ThreatResolved += _ => eventCount++;

            Assert.IsTrue(state.ResolveThreat(Threat(), TimingGrade.Perfect, false, 0));
            Assert.IsFalse(state.ResolveThreat(Threat(), TimingGrade.Perfect, false, 0));

            Assert.AreEqual(1, state.PerfectCount);
            Assert.AreEqual(1, eventCount);
            Assert.IsTrue(state.IsResolved(1));
        }

        [Test]
        public void MissAppliesDamageAndClampsToRemainingIntegrity()
        {
            var state = new CombatState(maxIntegrity: 5, startingEnergy: 6);

            Assert.IsTrue(state.ResolveThreat(Threat(damage: 10), TimingGrade.Miss, false, 10));

            Assert.AreEqual(0, state.Integrity);
            Assert.AreEqual(5, state.DamageTaken);
            Assert.AreEqual(1, state.MissCount);
        }

        [Test]
        public void DamageIsIgnoredForSuccessfulShield()
        {
            var state = new CombatState();

            Assert.IsTrue(state.ResolveThreat(Threat(), TimingGrade.Good, false, 10));

            Assert.AreEqual(100, state.Integrity);
            Assert.AreEqual(0, state.DamageTaken);
            Assert.AreEqual(1, state.GoodCount);
        }

        [Test]
        public void ScoreUsesPerfectGoodMissAndDamageFormula()
        {
            var state = new CombatState();

            state.ResolveThreat(Threat(1), TimingGrade.Perfect, false, 0);
            state.ResolveThreat(Threat(2), TimingGrade.Perfect, false, 0);
            state.ResolveThreat(Threat(3), TimingGrade.Perfect, false, 0);
            Assert.AreEqual(300, state.Score);

            state.ResolveThreat(Threat(4), TimingGrade.Good, false, 0);
            Assert.AreEqual(360, state.Score);

            state.ResolveThreat(Threat(5), TimingGrade.Miss, false, 10);
            Assert.AreEqual(70, state.Score);
        }

        [Test]
        public void EnergySpendFailsWithoutCostWhenInsufficient()
        {
            var state = new CombatState(startingEnergy: 1);

            Assert.IsTrue(state.TrySpendEnergy(1));
            Assert.AreEqual(0, state.Energy);
            Assert.IsFalse(state.TrySpendEnergy(1));
            Assert.AreEqual(0, state.Energy);

            state.AddEnergy(2);
            Assert.AreEqual(2, state.Energy);
        }
    }
}
