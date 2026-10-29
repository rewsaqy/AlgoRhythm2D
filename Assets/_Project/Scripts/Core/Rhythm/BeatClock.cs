using System;

namespace AlgoRhythm.Core.Rhythm
{
    /// <summary>
    /// Maps musical beats onto absolute DSP time. Pausing removes elapsed DSP
    /// time from the beat origin without advancing or reordering the beat map.
    /// </summary>
    public sealed class BeatClock
    {
        private readonly IClockSource _source;
        private double _secondsPerBeat;
        private double _originDsp;
        private double _pauseStartedDsp;
        private bool _started;
        private bool _paused;

        public BeatClock(IClockSource source, double bpm)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (bpm <= 0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            _source = source;
            Bpm = bpm;
            _secondsPerBeat = 60.0 / bpm;
        }

        public double Bpm { get; private set; }

        public double SecondsPerBeat => _secondsPerBeat;

        /// <summary>
        /// Retunes the clock mid-run (per-wave BPM). Safe to call before
        /// Start; never call mid-wave (beat map would shear).
        /// </summary>
        public void SetBpm(double bpm)
        {
            if (bpm <= 0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            Bpm = bpm;
            _secondsPerBeat = 60.0 / bpm;
        }

        public double OriginDsp => _originDsp;

        public bool IsStarted => _started;

        public bool IsPaused => _paused;

        public void Start(double dspStart)
        {
            _originDsp = dspStart;
            _pauseStartedDsp = dspStart;
            _started = true;
            _paused = false;
        }

        public void Pause()
        {
            if (!_started || _paused)
            {
                return;
            }

            _pauseStartedDsp = _source.NowDsp;
            _paused = true;
        }

        public void Resume()
        {
            if (!_started || !_paused)
            {
                return;
            }

            _originDsp += _source.NowDsp - _pauseStartedDsp;
            _paused = false;
        }

        /// <summary>
        /// Fast-forwards the beat timeline by advancing origin DSP backward by the given beat count.
        /// </summary>
        public void FastForward(int beats)
        {
            _originDsp -= beats * _secondsPerBeat;
        }

        public double BeatTime(int beatIndex)
        {
            return _originDsp + beatIndex * _secondsPerBeat;
        }

        public int BeatIndexAt(double dspTime)
        {
            if (!_started)
            {
                return 0;
            }

            return (int)Math.Floor((dspTime - _originDsp) / _secondsPerBeat);
        }

        public int CurrentBeatIndex => BeatIndexAt(_paused ? _pauseStartedDsp : _source.NowDsp);

        /// <summary>Seconds since beat zero, frozen while paused.</summary>
        public double NowSeconds
        {
            get
            {
                if (!_started)
                {
                    return 0.0;
                }

                double now = _paused ? _pauseStartedDsp : _source.NowDsp;
                return now - _originDsp;
            }
        }
    }
}
