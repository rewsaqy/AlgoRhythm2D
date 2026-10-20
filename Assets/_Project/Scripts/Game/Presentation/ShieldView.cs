using AlgoRhythm.Core.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// The single orbiting shield. It parks at the controller's angle and
    /// pulses on a successful block; it never hides on a timer.
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
            _baseScale = transform.localScale;
            gameObject.SetActive(true);
        }

        public void Park(Vector3 localPosition, float angleDeg, string slotCode)
        {
            gameObject.SetActive(true);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            if (_label != null)
            {
                // Counter-rotate so the text stays upright while the bar lies
                // tangent to the ring.
                _label.transform.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
                _label.text = $"SHIELD\n{slotCode}";
            }
        }

        public void Pulse()
        {
            _pulseUntil = Time.unscaledTime + 0.18f;
            transform.localScale = _baseScale * 1.25f;
        }

        private void Update()
        {
            if (_pulseUntil > 0f && Time.unscaledTime >= _pulseUntil)
            {
                _pulseUntil = 0f;
                transform.localScale = _baseScale;
            }
        }
    }
}
