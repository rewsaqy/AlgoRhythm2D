using AlgoRhythm.Core.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// The single orbiting shield. It parks at the controller's angle and
    /// pulses on a successful block; it never hides on a timer. The bar
    /// itself is the cue; the debug text label stays hidden.
    /// </summary>
    public sealed class ShieldView : MonoBehaviour
    {
        private TextMesh _label;
        private Vector3 _baseScale;
        private float _pulseUntil;

        public void Initialize()
        {
            Transform label = transform.Find("Label");
            _label = label != null ? label.GetComponent<TextMesh>() : null;
            if (_label != null)
            {
                _label.text = string.Empty;
                _label.gameObject.SetActive(false);
            }

            _baseScale = transform.localScale;
            gameObject.SetActive(true);
        }

        public void Park(Vector3 localPosition, float angleDeg, string slotCode)
        {
            _ = slotCode;
            gameObject.SetActive(true);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        public void Pulse()
        {
            _pulseUntil = Time.unscaledTime + 0.28f;
            transform.localScale = _baseScale * 1.45f;
        }

        private void Update()
        {
            if (_pulseUntil > 0f)
            {
                if (Time.unscaledTime >= _pulseUntil)
                {
                    _pulseUntil = 0f;
                    transform.localScale = _baseScale;
                }
                else
                {
                    float t = 1f - (_pulseUntil - Time.unscaledTime) / 0.28f;
                    float scale = Mathf.Lerp(1.45f, 1.0f, t * t);
                    transform.localScale = _baseScale * scale;
                }
            }
        }
    }
}
