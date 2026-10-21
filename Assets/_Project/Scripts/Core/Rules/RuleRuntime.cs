using System;
using AlgoRhythm.Core.Combat;

namespace AlgoRhythm.Core.Rules
{
    /// <summary>Beat-scoped clock reading for the runtime.</summary>
    public readonly struct BeatContext
    {
        public BeatContext(int beatIndex, double dspTime)
        {
            BeatIndex = beatIndex;
            DspTime = dspTime;
        }

        public int BeatIndex { get; }

        public double DspTime { get; }
    }

    /// <summary>One auto-shield fire. Logged by the code panel.</summary>
    public readonly struct RuleFired
    {
        public RuleFired(int ruleId, int threatId, int beatIndex, int energySpent, bool wasDecoy)
        {
            RuleId = ruleId;
            ThreatId = threatId;
            BeatIndex = beatIndex;
            EnergySpent = energySpent;
            WasDecoy = wasDecoy;
        }

        public int RuleId { get; }

        public int ThreatId { get; }

        public int BeatIndex { get; }

        public int EnergySpent { get; }

        public bool WasDecoy { get; }
    }

    /// <summary>Per-threat auto-shield decision. Only Fire spends and resolves.</summary>
    public enum RuleFireDecision
    {
        NoRule,
        NoTarget,
        SuppressManualPrecedence,
        SuppressNoEnergy,
        SuppressCooldown,
        Fire
    }

    /// <summary>
    /// Owns the committed rule, its activation beat, and its cooldown clock.
    /// A commit is staged and becomes active on the next <see cref="OnBeat"/>
    /// (A3). Per-threat firing is decided by <see cref="EvaluateThreat"/>;
    /// the caller applies energy spend and resolution. Suppressions cost
    /// nothing and never start the cooldown (Set A).
    /// </summary>
    public sealed class RuleRuntime
    {
        public const string LogSuppressedNoEnergy = "suppressed_no_energy";
        public const string LogManualPrecedence = "manual_precedence";
        public const string LogCooldown = "cooldown";
        public const string LogNoTarget = "no_target";
        public const string LogTargetNotFound = "e006_target_not_found";

        private RuleDefinition? _pending;
        private RuleDefinition? _active;
        private int _lastRuleId;
        private int _activeRuleId;
        private int _lastBeatIndex;
        private double _lastFireDsp = double.NaN;

        /// <summary>Raised once per fire, for feedback and the code-panel log.</summary>
        public event Action<RuleFired> RuleFired;

        /// <summary>Gameplay-free log keys for every suppression path.</summary>
        public event Action<string> RuleLog;

        public bool HasActiveRule => _active.HasValue;

        public int ActiveRuleId => _activeRuleId;

        /// <summary>
        /// Reports an E006 target-resolution failure: a fired rule arrived at
        /// a target that no longer exists (resolved or retried away). Log-only;
        /// no energy is refunded or spent here.
        /// </summary>
        public void ReportTargetNotFound()
        {
            RuleLog?.Invoke(LogTargetNotFound);
        }

        public void Commit(RuleDefinition rule)
        {
            _lastRuleId++;
            _pending = rule;
        }

        public void OnBeat(int beatIndex, in BeatContext ctx)
        {
            _lastBeatIndex = beatIndex;
            if (_pending.HasValue)
            {
                _active = _pending;
                _pending = null;
                _activeRuleId = _lastRuleId;
            }
        }

        public int GetCooldownRemainingMs(double nowDsp)
        {
            if (!_active.HasValue || double.IsNaN(_lastFireDsp))
            {
                return 0;
            }

            double remaining = _active.Value.CooldownMs - (nowDsp - _lastFireDsp) * 1000.0;
            return remaining > 0.0 ? (int)Math.Ceiling(remaining) : 0;
        }

        /// <summary>
        /// Logs manual precedence when a manual non-Miss resolve beats the
        /// active rule to a threat it targets. Returns false (and logs
        /// nothing) when no active rule targets the threat.
        /// </summary>
        public bool TryLogManualPrecedence(in ThreatEvent threat)
        {
            if (!_active.HasValue)
            {
                return false;
            }

            RuleDefinition rule = _active.Value;
            if (!MatchesTarget(rule, threat) || !MatchesCondition(rule, threat))
            {
                return false;
            }

            RuleLog?.Invoke(LogManualPrecedence);
            return true;
        }

        public RuleFireDecision EvaluateThreat(
            in ThreatEvent threat,
            double nowDsp,
            int energy,
            bool manualNonMissResolved,
            out RuleFired fired)
        {
            fired = default;
            if (!_active.HasValue)
            {
                return RuleFireDecision.NoRule;
            }

            RuleDefinition rule = _active.Value;
            if (!MatchesTarget(rule, threat) || !MatchesCondition(rule, threat))
            {
                RuleLog?.Invoke(LogNoTarget);
                return RuleFireDecision.NoTarget;
            }

            if (manualNonMissResolved)
            {
                RuleLog?.Invoke(LogManualPrecedence);
                return RuleFireDecision.SuppressManualPrecedence;
            }

            if (energy < rule.EnergyCost)
            {
                RuleLog?.Invoke(LogSuppressedNoEnergy);
                return RuleFireDecision.SuppressNoEnergy;
            }

            if (GetCooldownRemainingMs(nowDsp) > 0)
            {
                RuleLog?.Invoke(LogCooldown);
                return RuleFireDecision.SuppressCooldown;
            }

            _lastFireDsp = nowDsp;
            fired = new RuleFired(
                _activeRuleId,
                threat.Id,
                _lastBeatIndex,
                rule.EnergyCost,
                threat.Kind == ThreatKind.AdwareDecoy);
            RuleFired?.Invoke(fired);
            return RuleFireDecision.Fire;
        }

        private static bool MatchesTarget(RuleDefinition rule, in ThreatEvent threat)
        {
            if (rule.Tgt == Target.Nearest)
            {
                return true;
            }

            return threat.Direction == (Direction)rule.Tgt;
        }

        private static bool MatchesCondition(RuleDefinition rule, in ThreatEvent threat)
        {
            switch (rule.Cond)
            {
                case Condition.WhenProjectile:
                    // Anything flying counts as a projectile, which is exactly
                    // why a decoy fools the rule (Set A).
                    return threat.Kind == ThreatKind.StraightShooter
                           || threat.Kind == ThreatKind.AdwareDecoy;
                case Condition.WhenBeat:
                    return true;
                default:
                    return false;
            }
        }
    }
}
