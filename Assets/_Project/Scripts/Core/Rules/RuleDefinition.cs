namespace AlgoRhythm.Core.Rules
{
    /// <summary>
    /// A committed, immutable rule. Costs come from config, never from code.
    /// </summary>
    public readonly struct RuleDefinition
    {
        public RuleDefinition(
            Condition cond,
            Target tgt,
            RuleAction act,
            int energyCost,
            int cooldownMs,
            int priority)
        {
            Cond = cond;
            Tgt = tgt;
            Act = act;
            EnergyCost = energyCost;
            CooldownMs = cooldownMs;
            Priority = priority;
        }

        public Condition Cond { get; }

        public Target Tgt { get; }

        public RuleAction Act { get; }

        public int EnergyCost { get; }

        public int CooldownMs { get; }

        public int Priority { get; }
    }

    /// <summary>
    /// An uncommitted draft from the code panel. Any slot may be empty.
    /// </summary>
    public readonly struct RuleDraft
    {
        public RuleDraft(Condition? cond, Target? tgt, RuleAction? act)
        {
            Cond = cond;
            Tgt = tgt;
            Act = act;
        }

        public Condition? Cond { get; }

        public Target? Tgt { get; }

        public RuleAction? Act { get; }
    }
}
