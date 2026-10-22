namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Threat archetypes. StraightShooter is the basic note; Marcher behaves
    /// identically in combat but telegraphs as a line; Heavy has armor and
    /// needs repeated hits; Sleeper idles then dashes on its impact beat;
    /// Holder is a hold note resolved by keeping the shield held; AdwareDecoy
    /// is harmless bait (-5 if shielded). New kinds are always additive —
    /// existing waves and tests keep working.
    /// </summary>
    public enum ThreatKind
    {
        StraightShooter,
        AdwareDecoy,
        Marcher,
        Heavy,
        Sleeper,
        Holder
    }
}
