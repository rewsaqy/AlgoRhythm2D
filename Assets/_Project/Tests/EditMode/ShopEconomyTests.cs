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

        [Test]
        public void AddPts_IncreasesCurrencyPts()
        {
            var state = new CombatState();
            Assert.AreEqual(10, state.CurrencyPts);

            state.AddPts(500);
            Assert.AreEqual(510, state.CurrencyPts);

            state.AddPts(999);
            Assert.AreEqual(1509, state.CurrencyPts);
        }

        [Test]
        public void AddPts_NegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var state = new CombatState();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => state.AddPts(-1));
        }

        [Test]
        public void EconomyProgression_FlawlessPlayerCanAffordAidaMax()
        {
            var state = new CombatState();
            state.AddPts(296); // 10 initial + 296 = 306 PTS (flawless up to Shop 4)
            Assert.AreEqual(306, state.CurrencyPts);

            // Cost ladder: 35, 65, 85, 95
            Assert.IsTrue(state.TrySpendPts(35)); // Lv2
            Assert.IsTrue(state.TrySpendPts(65)); // Lv3
            Assert.IsTrue(state.TrySpendPts(85)); // Lv4
            Assert.IsTrue(state.TrySpendPts(95)); // Lv5 MAX

            Assert.AreEqual(26, state.CurrencyPts); // 26 PTS spare
        }

        [Test]
        public void EconomyProgression_DamagedPlayerPrioritizesLevel3AndRegen()
        {
            var state = new CombatState();
            state.AddPts(222); // 10 initial + 222 = 232 PTS (~75% efficiency)
            Assert.AreEqual(232, state.CurrencyPts);

            // Player takes damage and invests in Regen Lv1 and Lv2
            Assert.IsTrue(state.TrySpendPts(35)); // Regen Lv1
            Assert.IsTrue(state.TrySpendPts(60)); // Regen Lv2
            Assert.AreEqual(137, state.CurrencyPts);

            // Upgrades AIDA up to Level 3
            Assert.IsTrue(state.TrySpendPts(35)); // AIDA Lv2
            Assert.IsTrue(state.TrySpendPts(65)); // AIDA Lv3
            Assert.AreEqual(37, state.CurrencyPts);

            // Cannot afford AIDA Lv4 (costs 85 PTS)
            Assert.IsFalse(state.TrySpendPts(85));
            Assert.AreEqual(37, state.CurrencyPts);
        }
    }
}
