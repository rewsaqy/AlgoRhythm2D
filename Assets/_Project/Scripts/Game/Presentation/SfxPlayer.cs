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
        private AudioClip _sirenClip;

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

        public void Pause()
        {
            StopAllSources();
        }

        /// <summary>
        /// Ending-teaser siren: procedural two-tone wail, generated at
        /// runtime so no audio asset or scene change is needed. One voice
        /// from the shared pool; safe to call when the clock never started.
        /// </summary>
        public void PlaySiren(float seconds = 5f)
        {
            if (_sources == null || _sources.Length == 0)
            {
                return;
            }

            if (_sirenClip == null)
            {
                _sirenClip = MakeSirenClip(Mathf.Max(1f, seconds));
                if (_sirenClip == null)
                {
                    return;
                }
            }

            AudioSource source = _sources[_sourceCursor];
            _sourceCursor = (_sourceCursor + 1) % _sources.Length;
            source.Stop();
            source.clip = _sirenClip;
            source.volume = 0.4f;
            source.pitch = 1f;
            source.PlayScheduled(AudioSettings.dspTime);
        }

        private static AudioClip MakeSirenClip(float seconds)
        {
            const int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
            float[] data = new float[samples];
            // Two-tone wail: 660 Hz / 880 Hz alternating every half second,
            // short fades at both ends so it never clicks.
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float freq = ((int)(t * 2f) % 2 == 0) ? 660f : 880f;
                float env = Mathf.Min(1f, t / 0.15f, (seconds - t) / 0.3f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * Mathf.Max(0f, env);
            }

            AudioClip clip = AudioClip.Create("TeaserSiren", samples, 1, rate, false);
            if (clip == null)
            {
                return null;
            }

            clip.SetData(data, 0);
            return clip;
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
