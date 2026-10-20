namespace AlgoRhythm.Core.Rhythm
{
    /// <summary>
    /// Absolute DSP-time source. Gameplay timing must never use frame time.
    /// </summary>
    public interface IClockSource
    {
        double NowDsp { get; }
    }
}
