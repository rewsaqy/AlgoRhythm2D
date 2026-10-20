using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Rhythm
{
    /// <summary>The only Unity clock adapter used by gameplay.</summary>
    public sealed class DspClockSource : IClockSource
    {
        public double NowDsp => AudioSettings.dspTime;
    }
}
