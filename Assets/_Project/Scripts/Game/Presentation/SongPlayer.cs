using System;
using System.Collections;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Scheduled song playback on the DSP timeline with dual-source seamless crossfading.
    /// Supports uninterrupted transitions between songs (e.g. Tutorial into Wave 1),
    /// beat-aligned phase synchronization, smooth crossfading, and clean restarts on retry.
    /// Loop songs continue playing across waves with the same clip.
    /// Null clip = silent; the metronome keeps running regardless.
    /// </summary>
    public sealed class SongPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [Range(0f, 1f)] [SerializeField] private float songVolume = 0.8f;
        [SerializeField] private float fadeInDuration = 1.0f;
        [SerializeField] private float fadeOutDuration = 1.0f;
        [SerializeField] private float crossfadeDuration = 0.6f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _activeSource;
        private AudioSource _standbySource;
        private bool _wasPlaying;
        private Coroutine _fadeCoroutine;

        public bool IsPlaying => _activeSource != null && _activeSource.isPlaying;

        public AudioClip CurrentClip => _activeSource != null ? _activeSource.clip : null;

        public float CurrentPlaybackTime => _activeSource != null ? _activeSource.time : 0f;

        public float SongVolume => songVolume;

        public void Initialize()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
                if (source == null)
                {
                    source = gameObject.AddComponent<AudioSource>();
                }
            }

            ConfigureSource(source);
            _sourceA = source;

            // Find an existing secondary AudioSource or create one at runtime
            AudioSource[] sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != _sourceA)
                {
                    _sourceB = sources[i];
                    break;
                }
            }

            if (_sourceB == null)
            {
                _sourceB = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSource(_sourceB);
            _sourceB.volume = 0f;

            _activeSource = _sourceA;
            _standbySource = _sourceB;
        }

        private void ConfigureSource(AudioSource src)
        {
            if (src == null)
            {
                return;
            }

            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = songVolume;
            src.pitch = 1.0f;
        }

        /// <summary>
        /// Calculates the next DSP time for beat 0 aligned to the beat/bar grid of the currently playing song.
        /// If no song is playing, returns AudioSettings.dspTime + leadInSeconds.
        /// </summary>
        public double GetNextBeatAlignedDspTime(double leadInSeconds, double secondsPerBeat, int quantizeBeats = 4)
        {
            if (_activeSource == null || !_activeSource.isPlaying || secondsPerBeat <= 0.0)
            {
                return AudioSettings.dspTime + leadInSeconds;
            }

            double pitch = (_activeSource != null && _activeSource.pitch > 0.01f) ? _activeSource.pitch : 1.0;
            double songTime = _activeSource.time;
            double elapsedDspSeconds = songTime / pitch;
            double currentBeat = elapsedDspSeconds / secondsPerBeat;
            double leadInBeats = leadInSeconds / secondsPerBeat;
            double minTargetBeat = currentBeat + leadInBeats;
            double targetBeat = Math.Ceiling(minTargetBeat / quantizeBeats) * quantizeBeats;

            if ((targetBeat - currentBeat) * secondsPerBeat < leadInSeconds + 0.05)
            {
                targetBeat += quantizeBeats;
            }

            double timeUntilTarget = (targetBeat - currentBeat) * secondsPerBeat;
            return AudioSettings.dspTime + timeUntilTarget;
        }

        /// <summary>
        /// Starts the wave song at an absolute DSP time. If another song is already playing and this is not a retry,
        /// performs a seamless crossfade at dspTime without interrupting the current track during lead-in.
        /// </summary>
        public void PlayWaveSong(AudioClip clip, bool loop, double dspTime, bool isRetry = false, float pitch = 1.0f)
        {
            if (_activeSource == null)
            {
                Initialize();
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (clip == null)
            {
                StopSong(true);
                return;
            }

            // Case A: Retry — cleanly stop both sources and schedule from zero
            if (isRetry)
            {
                StopBothImmediate();
                _activeSource.clip = clip;
                _activeSource.loop = loop;
                _activeSource.volume = 0f;
                _activeSource.pitch = pitch;

                ScheduleOrPlay(_activeSource, dspTime);
                _fadeCoroutine = StartCoroutine(FadeInAfterDelay(_activeSource, (float)Mathf.Max(0f, (float)(dspTime - AudioSettings.dspTime))));
                _wasPlaying = true;
                return;
            }

            // Case B: Same clip already playing seamlessly (e.g. loops or consecutive boss phases)
            if (_activeSource.isPlaying && _activeSource.clip == clip)
            {
                _activeSource.loop = loop;
                _activeSource.volume = songVolume;
                _activeSource.pitch = pitch;
                _wasPlaying = true;
                return;
            }

            // Case C: Initial playback (nothing currently playing)
            if (!_activeSource.isPlaying)
            {
                _activeSource.clip = clip;
                _activeSource.loop = loop;
                _activeSource.volume = songVolume;
                _activeSource.pitch = pitch;

                ScheduleOrPlay(_activeSource, dspTime);
                _wasPlaying = true;
                return;
            }

            // Case D: Seamless crossfade transition between different clips (e.g. Tutorial -> Wave 1)
            _standbySource.Stop();
            _standbySource.clip = clip;
            _standbySource.loop = loop;
            _standbySource.volume = 0f;
            _standbySource.pitch = pitch;

            if (dspTime > AudioSettings.dspTime + 0.03)
            {
                _standbySource.PlayScheduled(dspTime);
            }
            else
            {
                _standbySource.Play();
                dspTime = AudioSettings.dspTime;
            }

            _fadeCoroutine = StartCoroutine(CrossfadeRoutine(_activeSource, _standbySource, dspTime, crossfadeDuration));
            _wasPlaying = true;
        }

        private void ScheduleOrPlay(AudioSource src, double dspTime)
        {
            if (dspTime > AudioSettings.dspTime + 0.03)
            {
                src.PlayScheduled(dspTime);
            }
            else
            {
                src.Play();
            }
        }

        private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, double dspStart, float duration)
        {
            // 1. Keep outgoing playing at full volume until dspStart
            while (AudioSettings.dspTime < dspStart)
            {
                outgoing.volume = songVolume;
                incoming.volume = 0f;
                yield return null;
            }

            // 2. Linear crossfade across duration
            if (duration > 0f)
            {
                double crossfadeStartDsp = AudioSettings.dspTime;
                while (AudioSettings.dspTime < crossfadeStartDsp + duration)
                {
                    float t = Mathf.Clamp01((float)((AudioSettings.dspTime - crossfadeStartDsp) / duration));
                    outgoing.volume = songVolume * (1f - t);
                    incoming.volume = songVolume * t;
                    yield return null;
                }
            }

            // 3. Complete transition
            outgoing.volume = 0f;
            outgoing.Stop();
            incoming.volume = songVolume;

            // 4. Swap active and standby sources
            _activeSource = incoming;
            _standbySource = outgoing;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeInAfterDelay(AudioSource src, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return FadeRoutine(src, 0f, songVolume, fadeInDuration);
            _fadeCoroutine = null;
        }

        public void StopSong(bool fadeOut = true)
        {
            _wasPlaying = false;
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            bool anyPlaying = (_activeSource != null && _activeSource.isPlaying) ||
                              (_standbySource != null && _standbySource.isPlaying);

            if (fadeOut && gameObject.activeInHierarchy && anyPlaying && fadeOutDuration > 0f)
            {
                _fadeCoroutine = StartCoroutine(FadeOutAndStopBoth(fadeOutDuration));
            }
            else
            {
                StopBothImmediate();
            }
        }

        private IEnumerator FadeOutAndStopBoth(float duration)
        {
            float startVolA = _activeSource != null ? _activeSource.volume : 0f;
            float startVolB = _standbySource != null ? _standbySource.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_activeSource != null) _activeSource.volume = Mathf.Lerp(startVolA, 0f, t);
                if (_standbySource != null) _standbySource.volume = Mathf.Lerp(startVolB, 0f, t);
                yield return null;
            }

            StopBothImmediate();
            _fadeCoroutine = null;
        }

        private void StopBothImmediate()
        {
            if (_activeSource != null)
            {
                _activeSource.Stop();
                _activeSource.volume = songVolume;
                _activeSource.pitch = 1.0f;
            }

            if (_standbySource != null)
            {
                _standbySource.Stop();
                _standbySource.volume = songVolume;
                _standbySource.pitch = 1.0f;
            }
        }

        private IEnumerator FadeRoutine(AudioSource src, float from, float to, float duration)
        {
            if (src == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                src.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                src.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }
            src.volume = to;
        }

        public void Pause()
        {
            _wasPlaying = false;
            if (_activeSource != null && _activeSource.isPlaying)
            {
                _wasPlaying = true;
                _activeSource.Pause();
            }

            if (_standbySource != null && _standbySource.isPlaying)
            {
                _wasPlaying = true;
                _standbySource.Pause();
            }
        }

        public void Resume()
        {
            if (!_wasPlaying)
            {
                return;
            }

            if (_activeSource != null && _activeSource.clip != null)
            {
                _activeSource.UnPause();
            }

            if (_standbySource != null && _standbySource.clip != null)
            {
                _standbySource.UnPause();
            }
        }

        public void FastForward(float seconds)
        {
            if (_activeSource != null && _activeSource.clip != null)
            {
                float target = Mathf.Clamp(_activeSource.time + seconds, 0f, Mathf.Max(0f, _activeSource.clip.length - 0.1f));
                _activeSource.time = target;
            }
        }
    }
}
