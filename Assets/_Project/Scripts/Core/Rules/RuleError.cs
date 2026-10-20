namespace AlgoRhythm.Core.Rules
{
    /// <summary>
    /// Rule error taxonomy. E001–E005 are returned by the validator;
    /// E006 is a runtime target-resolution error, never a validation error.
    /// </summary>
    public enum RuleError
    {
        None,
        E001_EmptySlot,
        E002_WrongCategory,
        E003_Unsupported,
        E004_NotEnoughEnergy,
        E005_Cooldown,
        E006_TargetNotFound
    }
}
