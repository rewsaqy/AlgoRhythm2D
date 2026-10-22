using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rules;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class RuleRuntimeTests
    {
        private static ThreatEvent Threat(
            int id = 1,
            Direction direction = Direction.East,
            ThreatKind kind = ThreatKind.StraightShooter)
        {
            return new ThreatEvent(id, kind, direction, spawnBeat: 0, impactBeat: 1, baseDamage: 10);
        }

        private static RuleDefinition EastRule(int cost = 1, int cooldownMs = 1500)
        {
            return new RuleDefinition(
                Condition.WhenProjectile, Target.East, RuleAction.RaiseShield, cost, cooldownMs, priority: 0);
        }

        private static RuleRuntime CommittedAtBeat(RuleDefinition rule, int beat = 5, double dsp = 3.0)
        {
            var runtime = new RuleRuntime();
            runtime.Commit(rule);
            runtime.OnBeat(beat, new BeatContext(beat, dsp));
            return runtime;
        }

        [Test]
        public void Commit_StaysPendingUntilNextBeat()
        {
            var runtime = new RuleRuntime();
            runtime.Commit(EastRule());

            Assert.IsFalse(runtime.HasActiveRule);

            runtime.OnBeat(6, new BeatContext(6, 3.6));

            Assert.IsTrue(runtime.HasActiveRule);
            Assert.AreEqual(1, runtime.ActiveRuleId);
        }

        [Test]
        public void SecondCommit_ReplacesPendingRule()
        {
            var runtime = new RuleRuntime();
            runtime.Commit(EastRule());
            runtime.Commit(new RuleDefinition(
                Condition.WhenProjectile, Target.West, RuleAction.RaiseShield, 1, 1500, 0));
            runtime.OnBeat(6, new BeatContext(6, 3.6));

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(direction: Direction.West), 3.6, 6, false, out RuleFired fired);

            Assert.AreEqual(RuleFireDecision.Fire, decision);
            Assert.AreEqual(2, fired.RuleId);
        }

        [Test]
        public void EvaluateWithoutRule_ReturnsNoRule()
        {
            var runtime = new RuleRuntime();

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(), 3.6, 6, false, out _);

            Assert.AreEqual(RuleFireDecision.NoRule, decision);
        }

        [Test]
        public void WrongDirection_ReturnsNoTargetAndLogs()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule());
            var logs = new List<string>();
            runtime.RuleLog += logs.Add;

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(direction: Direction.West), 3.6, 6, false, out _);

            Assert.AreEqual(RuleFireDecision.NoTarget, decision);
            Assert.Contains(RuleRuntime.LogNoTarget, logs);
        }

        [Test]
        public void NearestTarget_MatchesAnyDirection()
        {
            var runtime = new RuleRuntime();
            runtime.Commit(new RuleDefinition(
                Condition.WhenProjectile, Target.Nearest, RuleAction.RaiseShield, 1, 1500, 0));
            runtime.OnBeat(6, new BeatContext(6, 3.6));

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(direction: Direction.South), 3.6, 6, false, out RuleFired fired);

            Assert.AreEqual(RuleFireDecision.Fire, decision);
            Assert.AreEqual(1, fired.ThreatId);
        }

        [Test]
        public void ManualNonMiss_SuppressesWithoutCost()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule());
            var logs = new List<string>();
            runtime.RuleLog += logs.Add;

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(), 3.6, 6, true, out _);

            Assert.AreEqual(RuleFireDecision.SuppressManualPrecedence, decision);
            Assert.Contains(RuleRuntime.LogManualPrecedence, logs);
            Assert.AreEqual(0, runtime.GetCooldownRemainingMs(3.6));
        }

        [Test]
        public void NoEnergy_SuppressesWithoutStartingCooldown()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule());
            var logs = new List<string>();
            runtime.RuleLog += logs.Add;

            RuleFireDecision suppressed = runtime.EvaluateThreat(
                Threat(), 3.6, 0, false, out _);
            RuleFireDecision afterRefill = runtime.EvaluateThreat(
                Threat(), 3.6, 6, false, out _);

            Assert.AreEqual(RuleFireDecision.SuppressNoEnergy, suppressed);
            Assert.Contains(RuleRuntime.LogSuppressedNoEnergy, logs);
            Assert.AreEqual(RuleFireDecision.Fire, afterRefill);
        }

        [Test]
        public void Fire_StartsCooldown_AndReportsIt()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule(cooldownMs: 1500), dsp: 3.0);

            RuleFireDecision first = runtime.EvaluateThreat(
                Threat(id: 1), 3.0, 6, false, out RuleFired fired);
            RuleFireDecision second = runtime.EvaluateThreat(
                Threat(id: 2), 3.5, 6, false, out _);

            Assert.AreEqual(RuleFireDecision.Fire, first);
            Assert.AreEqual(1, fired.EnergySpent);
            Assert.IsFalse(fired.WasDecoy);
            Assert.AreEqual(RuleFireDecision.SuppressCooldown, second);
            Assert.AreEqual(1000, runtime.GetCooldownRemainingMs(3.5));
            Assert.AreEqual(0, runtime.GetCooldownRemainingMs(4.5));
        }

        [Test]
        public void Fire_RaisesRuleFiredEventOnce_WithBeatAndIds()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule(), beat: 7, dsp: 4.2);
            var firedEvents = new List<RuleFired>();
            runtime.RuleFired += firedEvents.Add;

            runtime.EvaluateThreat(Threat(id: 9), 4.2, 6, false, out _);

            Assert.AreEqual(1, firedEvents.Count);
            Assert.AreEqual(1, firedEvents[0].RuleId);
            Assert.AreEqual(9, firedEvents[0].ThreatId);
            Assert.AreEqual(7, firedEvents[0].BeatIndex);
        }

        [Test]
        public void DecoyFire_MarksWasDecoy()
        {
            RuleRuntime runtime = CommittedAtBeat(EastRule());

            RuleFireDecision decision = runtime.EvaluateThreat(
                Threat(kind: ThreatKind.AdwareDecoy), 3.6, 6, false, out RuleFired fired);

            Assert.AreEqual(RuleFireDecision.Fire, decision);
            Assert.IsTrue(fired.WasDecoy);
        }

        [Test]
        public void ReportTargetNotFound_LogsE006Key()
        {
            var runtime = new RuleRuntime();
            var logged = new List<string>();
            runtime.RuleLog += logged.Add;

            runtime.ReportTargetNotFound();

            Assert.AreEqual(1, logged.Count);
            Assert.AreEqual(RuleRuntime.LogTargetNotFound, logged[0]);
        }

        [Test]
        public void Commit_SetsPendingUntilNextBeat_AndRaisesCommitted()
        {
            var runtime = new RuleRuntime();
            int committedCount = 0;
            runtime.RuleCommitted += () => committedCount++;

            runtime.Commit(EastRule());

            Assert.IsTrue(runtime.HasPendingRule);
            Assert.IsFalse(runtime.HasActiveRule);
            Assert.AreEqual(1, committedCount);

            runtime.OnBeat(6, new BeatContext(6, 3.6));

            Assert.IsFalse(runtime.HasPendingRule);
            Assert.IsTrue(runtime.HasActiveRule);
        }

        [Test]
        public void NewDangerousKinds_CountAsProjectiles()
        {
            var runtime = CommittedAtBeat(EastRule());
            var kinds = new[]
            {
                ThreatKind.Marcher, ThreatKind.Heavy,
                ThreatKind.Sleeper, ThreatKind.Holder,
            };

            foreach (ThreatKind kind in kinds)
            {
                // Fresh runtime per kind: a fire starts the cooldown clock.
                var fresh = CommittedAtBeat(EastRule());
                RuleFireDecision decision = fresh.EvaluateThreat(
                    Threat(id: (int)kind + 10, kind: kind), 3.6, 6, false, out RuleFired fired);
                Assert.AreEqual(RuleFireDecision.Fire, decision, kind.ToString());
            }
        }
    }
}
