using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// One-shot combat SFX. Destroy stingers play immediately on resolve
    /// (responsive) on the absolute DSP timeline with PlayScheduled; pitch
    /// follows the beat index on a pentatonic ladder so rapid destroys sound
    /// rhythmic without quantize latency. The AIDA pew uses the same shared
    /// voice pool with a fixed pitch. Uses manually authored AudioSource
    /// children, round-robin; no components are added at runtime.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        // Pentatonic ladder [U] demo tuning: unison, major 2nd, major 3rd,
        // fifth, major 6th, octave.
        private static readonly float[] PitchLadder =
        {
            1.0f, 1.125f, 1.25f, 1.5f, 1.6667f, 2.0f
        };

        [SerializeField] private AudioClip destroyClip;
        [SerializeField] private AudioClip pewClip;
        [Header("AIDA [U] — own voice so shots never read as UI/commit feedback.")]
        [SerializeField] private AudioClip aidaFireClip;
        [Range(0f, 1f)] [SerializeField] private float destroyVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float pewVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float aidaFireVolume = 0.35f;

        private AudioSource[] _sources;
        private BeatClock _clock;
        private int _sourceCursor;
        private bool _audioAvailable;

        public void Initialize(BeatClock clock)
        {
            _clock = clock;
            _sources = GetComponentsInChildren<AudioSource>(true);
            _audioAvailable = destroyClip != null && _sources.Length > 0;
            for (int index = 0; index < _sources.Length; index++)
            {
                AudioSource source = _sources[index];
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0.0f;
                source.volume = 0.5f;
                source.clip = destroyClip;
            }

            if (!_audioAvailable)
            {
                Debug.LogWarning(
                    "SfxPlayer has no destroy clip or AudioSource children; destroy SFX is silent.",
                    this);
            }
        }

        public void PlayDestroy()
        {
            if (!_audioAvailable || _clock == null || !_clock.IsStarted)
            {
                return;
            }

            int step = _clock.CurrentBeatIndex % PitchLadder.Length;
            if (step < 0)
            {
                step += PitchLadder.Length;
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = destroyClip;
            source.volume = destroyVolume;
            source.pitch = PitchLadder[step];
            source.PlayScheduled(AudioSettings.dspTime);
        }

        public void PlayPew()
        {
            if (!_audioAvailable || _clock == null || !_clock.IsStarted || pewClip == null)
            {
                return;
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = pewClip;
            source.volume = pewVolume;
            source.pitch = 1.15f;
            source.PlayScheduled(AudioSettings.dspTime);
        }

        /// <summary>
        /// AIDA tower shot: its own clip and a lower volume so the frequent
        /// every-4-beat fire never masks the metronome (the rhythm authority,
        /// §11). Falls back to the commit pew when no clip is wired, so a
        /// missing reference degrades to the old sound instead of silence.
        /// </summary>
        public void PlayAidaFire()
        {
            if (!_audioAvailable || _clock == null || !_clock.IsStarted)
            {
                return;
            }

            AudioClip clip = aidaFireClip != null ? aidaFireClip : pewClip;
            if (clip == null)
            {
                return;
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = clip;
            source.volume = aidaFireClip != null ? aidaFireVolume : pewVolume;
            source.pitch = 1.0f;
            source.PlayScheduled(AudioSettings.dspTime);
        }

        /// <summary>
        /// Outro/cutscene stinger. Plays immediately on unscaled time without
        /// requiring BeatClock to be running, at an adjustable pitch.
        /// </summary>
        public void PlayStinger(float pitch = 1.0f, float volume = 0.7f)
        {
            if (_sources == null || _sources.Length == 0 || destroyClip == null)
            {
                return;
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = destroyClip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.2f, 3.0f);
            source.Play();
        }

        /// <summary>
        /// Outro/cutscene glitch zap. Plays the retro zap sound at an adjustable pitch.
        /// </summary>
        public void PlayGlitchZap(float pitch = 1.0f, float volume = 0.6f)
        {
            if (_sources == null || _sources.Length == 0)
            {
                return;
            }

            AudioClip clip = aidaFireClip != null ? aidaFireClip : (pewClip != null ? pewClip : destroyClip);
            if (clip == null)
            {
                return;
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.2f, 3.0f);
            source.Play();
        }

        public void Pause()
        {
            StopAllSources();
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
