using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Scheduled song playback on the DSP timeline, one AudioSource.
    /// One-shot songs start exactly at wave beat 0 (after the lead-in);
    /// loop songs restart on every wave start/retry. Pause freezes the
    /// playback position and resume continues it, so the song stays
    /// aligned with the beat clock (which also freezes on pause).
    /// Null clip = silent; the metronome keeps running regardless.
    /// </summary>
    public sealed class SongPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [Range(0f, 1f)] [SerializeField] private float songVolume = 0.8f;

        private bool _wasPlaying;

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

        /// <summary>Starts the wave song at an absolute DSP time.</summary>
        public void PlayWaveSong(AudioClip clip, bool loop, double dspTime)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            _wasPlaying = false;
            if (clip == null)
            {
                return;
            }

            source.clip = clip;
            source.loop = loop;
            source.volume = songVolume;
            if (dspTime > AudioSettings.dspTime + 0.03)
            {
                source.PlayScheduled(dspTime);
            }
            else
            {
                source.Play();
            }

            _wasPlaying = true;
        }

        public void StopSong()
        {
            _wasPlaying = false;
            if (source != null)
            {
                source.Stop();
            }
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
