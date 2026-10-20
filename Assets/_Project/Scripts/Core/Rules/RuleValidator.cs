using System;

namespace AlgoRhythm.Core.Rules
{
    /// <summary>Validator inputs: spendable energy and remaining cooldown.</summary>
    public readonly struct RuleValidationContext
    {
        public RuleValidationContext(int energy, int cooldownRemainingMs)
        {
            Energy = energy;
            CooldownRemainingMs = cooldownRemainingMs;
        }

        public int Energy { get; }

        public int CooldownRemainingMs { get; }
    }

    /// <summary>Validator output: one specific error plus a deterministic preview key.</summary>
    public readonly struct RuleValidationResult
    {
        public RuleValidationResult(RuleError error, string previewKey)
        {
            Error = error;
            PreviewKey = previewKey;
        }

        public RuleError Error { get; }

        public string PreviewKey { get; }

        public bool IsValid => Error == RuleError.None;
    }

    /// <summary>
    /// Validates drafts against the whitelist, energy, and cooldown.
    /// Checks run in error-number order: E001, E002, E003, E004, then E005.
    /// E006 is a runtime error and is never returned here.
    /// </summary>
    public sealed class RuleValidator
    {
        private readonly RuleWhitelist _whitelist;
        private readonly int _energyCost;
        private readonly int _cooldownMs;

        public RuleValidator(RuleWhitelist whitelist, int energyCost, int cooldownMs)
        {
            _whitelist = whitelist ?? throw new ArgumentNullException(nameof(whitelist));
            if (energyCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(energyCost));
            }

            if (cooldownMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownMs));
            }

            _energyCost = energyCost;
            _cooldownMs = cooldownMs;
        }

        public static string PreviewKeyFor(Condition cond, Target tgt, RuleAction act)
        {
            return $"{ConditionName(cond)}_{tgt.ToString().ToUpperInvariant()}_{ActionName(act)}";
        }

        private static string ConditionName(Condition cond)
        {
            switch (cond)
            {
                case Condition.WhenBeat:
                    return "WHEN_BEAT";
                default:
                    return "WHEN_PROJECTILE";
            }
        }

        private static string ActionName(RuleAction act)
        {
            switch (act)
            {
                default:
                    return "RAISE_SHIELD";
            }
        }

        /// <summary>Tuned cost, shared so Game builds matching RuleDefinitions.</summary>
        public int EnergyCost => _energyCost;

        /// <summary>Tuned cooldown, shared so Game builds matching RuleDefinitions.</summary>
        public int CooldownMs => _cooldownMs;

        public RuleValidationResult Validate(RuleDraft draft, in RuleValidationContext context)
        {
            if (!draft.Cond.HasValue || !draft.Tgt.HasValue || !draft.Act.HasValue)
            {
                return new RuleValidationResult(RuleError.E001_EmptySlot, string.Empty);
            }

            Condition cond = draft.Cond.Value;
            Target tgt = draft.Tgt.Value;
            RuleAction act = draft.Act.Value;

            // Nearest is a dynamic target category: it needs a projectile
            // condition and is wrong for a beat condition.
            if (cond == Condition.WhenBeat && tgt == Target.Nearest)
            {
                return new RuleValidationResult(RuleError.E002_WrongCategory, string.Empty);
            }

            if (!_whitelist.Allows(cond, tgt, act))
            {
                return new RuleValidationResult(RuleError.E003_Unsupported, string.Empty);
            }

            if (context.Energy < _energyCost)
            {
                return new RuleValidationResult(RuleError.E004_NotEnoughEnergy, string.Empty);
            }

            if (context.CooldownRemainingMs > 0)
            {
                return new RuleValidationResult(RuleError.E005_Cooldown, string.Empty);
            }

            return new RuleValidationResult(RuleError.None, PreviewKeyFor(cond, tgt, act));
        }
    }
}
