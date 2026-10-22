using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    /// <summary>Shop economy: PTS awards per grade, heal clamp, spend guard.</summary>
    public sealed class ShopEconomyTests
    {
        private static ThreatEvent Threat(int id = 1, int damage = 10, ThreatKind kind = ThreatKind.StraightShooter)
        {
            return new ThreatEvent(
                id,
                kind,
                Direction.North,
                spawnBeat: 0,
                impactBeat: 1,
                baseDamage: damage);
        }

        [Test]
        public void FreshRun_StartsWithTenPts()
        {
            var state = new CombatState();

            Assert.AreEqual(10, state.CurrencyPts);
        }

        [Test]
        public void PerfectAwardsTwoPts_GoodAwardsOne()
        {
            var state = new CombatState();

            Assert.IsTrue(state.ResolveThreat(Threat(1), TimingGrade.Perfect, false, 0));
            Assert.AreEqual(12, state.CurrencyPts);

            Assert.IsTrue(state.ResolveThreat(Threat(2), TimingGrade.Good, false, 10));
            Assert.AreEqual(13, state.CurrencyPts);
        }

        [Test]
        public void MissAndDecoyAwardNothing()
        {
            var state = new CombatState();

            Assert.IsTrue(state.ResolveThreat(Threat(1, damage: 10), TimingGrade.Miss, false, 10));
            Assert.AreEqual(10, state.CurrencyPts);

            int before = state.CurrencyPts;
            Assert.IsTrue(state.ResolveDecoy(Threat(2, damage: 0, kind: ThreatKind.AdwareDecoy), shielded: true, fromRule: false, out _));
            Assert.AreEqual(before, state.CurrencyPts);
        }

        [Test]
        public void HealClampsToMax_AndFiresIntegrityChanged()
        {
            var state = new CombatState(maxIntegrity: 100, startingEnergy: 6);
            state.ResolveThreat(Threat(1, damage: 30), TimingGrade.Miss, false, 30);

            int events = 0;
            state.IntegrityChanged += _ => events++;

            state.Heal(25);
            Assert.AreEqual(95, state.Integrity);
            Assert.AreEqual(1, events);

            state.Heal(25);
            Assert.AreEqual(100, state.Integrity);
        }

        [Test]
        public void TrySpendPts_GuardsInsufficientFunds()
        {
            var state = new CombatState();
            state.ResolveThreat(Threat(1), TimingGrade.Perfect, false, 0);

            Assert.IsFalse(state.TrySpendPts(13));
            Assert.AreEqual(12, state.CurrencyPts);
            Assert.IsTrue(state.TrySpendPts(12));
            Assert.AreEqual(0, state.CurrencyPts);
        }

        [Test]
        public void ResetKeepsPts_AcrossRetry()
        {
            var state = new CombatState();
            state.ResolveThreat(Threat(1), TimingGrade.Perfect, false, 0);

            state.Reset(CombatState.DefaultMaxIntegrity, CombatState.DefaultStartingEnergy);

            Assert.AreEqual(12, state.CurrencyPts);
            Assert.AreEqual(0, state.PerfectCount);
        }
    }
}
