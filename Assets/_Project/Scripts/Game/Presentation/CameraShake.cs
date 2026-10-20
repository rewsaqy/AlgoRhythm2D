using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Trauma-based camera shake, presentation only. Gameplay adds trauma on
    /// destroys (bigger on Perfect); offset decays to zero on its own. Uses
    /// unscaled time so pause menus never inherit shake. The component lives
    /// on Main Camera and is authored in the scene.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        private float _trauma;
        private float _decayPerSecond = 2.5f;
        private float _maxOffset = 0.2f;
        private Vector3 _basePosition;
        private bool _initialized;

        public void Initialize(float decayPerSecond, float maxOffset)
        {
            _decayPerSecond = Mathf.Max(0.01f, decayPerSecond);
            _maxOffset = Mathf.Max(0f, maxOffset);
            _basePosition = transform.position;
            _initialized = true;
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (_trauma > 0f)
            {
                _trauma = Mathf.Max(0f, _trauma - _decayPerSecond * Time.unscaledDeltaTime);
            }

            if (_trauma <= 0f)
            {
                transform.position = _basePosition;
                return;
            }

            float amplitude = _maxOffset * _trauma * _trauma;
            float time = Time.unscaledTime * 47.0f;
            float x = amplitude * (Mathf.Sin(time * 1.1f) + 0.5f * Mathf.Sin(time * 2.3f));
            float y = amplitude * (Mathf.Cos(time * 1.3f) + 0.5f * Mathf.Sin(time * 2.9f));
            transform.position = _basePosition + new Vector3(x, y, 0f);
        }
    }
}
