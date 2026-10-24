using System.Collections;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Scheduled song playback on the DSP timeline, one AudioSource.
    /// One-shot songs start exactly at wave beat 0 (after the lead-in);
    /// loop songs restart on every wave start/retry. Pause freezes the
    /// playback position and resume continues it, so the song stays
    /// aligned with the beat clock (which also freezes on pause).
    /// Includes smooth fade-in on play and fade-out on stop.
    /// Null clip = silent; the metronome keeps running regardless.
    /// </summary>
    public sealed class SongPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [Range(0f, 1f)] [SerializeField] private float songVolume = 0.8f;
        [SerializeField] private float fadeInDuration = 1.0f;
        [SerializeField] private float fadeOutDuration = 1.0f;

        private bool _wasPlaying;
        private Coroutine _fadeCoroutine;

        public void Initialize()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (source != null)
            {
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = songVolume;
            }
        }

        /// <summary>Starts the wave song at an absolute DSP time with smooth fade-in.</summary>
        public void PlayWaveSong(AudioClip clip, bool loop, double dspTime)
        {
            if (source == null)
            {
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            source.Stop();
            _wasPlaying = false;
            if (clip == null)
            {
                return;
            }

            source.clip = clip;
            source.loop = loop;
            source.volume = 0f;

            if (dspTime > AudioSettings.dspTime + 0.03)
            {
                source.PlayScheduled(dspTime);
                _fadeCoroutine = StartCoroutine(FadeInAfterDelay((float)(dspTime - AudioSettings.dspTime)));
            }
            else
            {
                source.Play();
                _fadeCoroutine = StartCoroutine(FadeRoutine(0f, songVolume, fadeInDuration));
            }

            _wasPlaying = true;
        }

        private IEnumerator FadeInAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return FadeRoutine(0f, songVolume, fadeInDuration);
        }

        public void StopSong(bool fadeOut = true)
        {
            _wasPlaying = false;
            if (source == null)
            {
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (fadeOut && gameObject.activeInHierarchy && source.isPlaying && fadeOutDuration > 0f)
            {
                _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOutDuration));
            }
            else
            {
                source.Stop();
                source.volume = songVolume;
            }
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            float startVol = source.volume;
            yield return FadeRoutine(startVol, 0f, duration);
            source.Stop();
            source.volume = songVolume;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                source.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }
            source.volume = to;
        }

        public void Pause()
        {
            if (source != null && source.isPlaying)
            {
                _wasPlaying = true;
                source.Pause();
            }
            else
            {
                _wasPlaying = false;
            }
        }

        public void Resume()
        {
            if (_wasPlaying && source != null && source.clip != null)
            {
                source.UnPause();
            }
        }
    }
}
