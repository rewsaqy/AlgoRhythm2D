using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Scheduled metronome using manually authored AudioSource children. Every
    /// click is placed on the absolute DSP timeline with PlayScheduled; no
    /// frame-time judgement is involved and no components are added at runtime.
    /// </summary>
    public sealed class MetronomePlayer : MonoBehaviour
    {
        private const double ScheduleAheadBeats = 4.0;

        [SerializeField] private AudioClip clickClip;

        private AudioSource[] _sources;
        private BeatClock _clock;
        private int _nextBeat;
        private int _sourceCursor;
        private bool _paused;
        private bool _audioAvailable;

        public void Initialize(BeatClock clock)
        {
            _clock = clock;
            _sources = GetComponentsInChildren<AudioSource>(true);
            _audioAvailable = clickClip != null && _sources.Length > 0;
            _nextBeat = 0;
            _sourceCursor = 0;
            _paused = false;

            for (int index = 0; index < _sources.Length; index++)
            {
                AudioSource source = _sources[index];
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0.0f;
                source.volume = 0.35f;
                source.clip = clickClip;
            }
        }

        /// <summary>
        /// Re-anchors scheduling to a restarted clock origin (wave change or
        /// retry). Clears stale PlayScheduled clicks so the old timeline
        /// never overlaps the new lead-in, then schedules from the new origin.
        /// </summary>
        public void Resync()
        {
            StopAllSources();
            _paused = false;
            if (_clock != null && _clock.IsStarted)
            {
                _nextBeat = Mathf.Max(0, _clock.CurrentBeatIndex + 1);
            }
            else
            {
                _nextBeat = 0;
            }
        }

        public void Pause()
        {
            _paused = true;
            StopAllSources();
        }

        public void Resume()
        {
            _paused = false;
            if (_clock != null && _clock.IsStarted)
            {
                _nextBeat = Mathf.Max(0, _clock.CurrentBeatIndex + 1);
            }
        }

        private void Update()
        {
            if (!_audioAvailable || _clock == null || !_clock.IsStarted || _paused || _clock.IsPaused)
            {
                return;
            }

            double now = AudioSettings.dspTime;
            double scheduleUntil = now + _clock.SecondsPerBeat * ScheduleAheadBeats;

            while (true)
            {
                double beatTime = _clock.BeatTime(_nextBeat);
                if (beatTime > scheduleUntil)
                {
                    break;
                }

                if (beatTime >= now - 0.02)
                {
                    AudioSource source = _sources[_sourceCursor];
                    _sourceCursor = (_sourceCursor + 1) % _sources.Length;
                    source.Stop();
                    source.PlayScheduled(beatTime);
                }

                _nextBeat++;
            }
        }

        private void StopAllSources()
        {
            if (_sources == null)
            {
                return;
            }

            for (int index = 0; index < _sources.Length; index++)
            {
                if (_sources[index] != null)
                {
                    _sources[index].Stop();
                }
            }
        }
    }
}
