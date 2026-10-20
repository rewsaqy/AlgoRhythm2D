namespace AlgoRhythm.Core.Rules
{
    /// <summary>Slot 1: when the rule fires. Demo set only.</summary>
    public enum Condition
    {
        WhenBeat,
        WhenProjectile
    }

    /// <summary>
    /// Slot 2: which direction the shield rises on. Order mirrors
    /// <see cref="Combat.Direction"/> for North through West; Nearest is a
    /// dynamic target and is only valid with <see cref="Condition.WhenProjectile"/>.
    /// </summary>
    public enum Target
    {
        North,
        East,
        South,
        West,
        Nearest
    }

    /// <summary>Slot 3: what the rule does. Demo set only.</summary>
    public enum RuleAction
    {
        RaiseShield
    }
}
