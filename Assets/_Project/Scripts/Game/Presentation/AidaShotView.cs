using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// One pooled AIDA projectile. Flies straight from the tower to the aim
    /// point locked at fire time (no homing, Diep.io-style); the controller
    /// decides hit or fizzle on arrival. Triangle-ish diamond marker plus an
    /// "AIDA" label keep it distinct from enemy threats (never colour alone).
    /// </summary>
    public sealed class AidaShotView : MonoBehaviour
    {
        private TextMesh _label;

        public bool IsAvailable => !gameObject.activeSelf;
        public int TargetId { get; private set; } = -1;
        public bool WillHit { get; private set; }
        public double ArrivalDsp { get; private set; }

        private Vector3 _from;
        private Vector3 _to;
        private double _startDsp;
        private double _durationSec;

        public void Initialize()
        {
            Transform label = transform.Find("Label");
            _label = label != null ? label.GetComponent<TextMesh>() : null;
            if (_label != null)
            {
                // Shots stay text-free: the diamond marker already reads as
                // AIDA (never colour alone). Keeps projectiles quiet.
                _label.gameObject.SetActive(false);
            }

            Hide();
        }

        public void Fire(
            Vector3 from,
            Vector3 to,
            int targetId,
            bool willHit,
            double startDsp,
            double arrivalDsp)
        {
            TargetId = targetId;
            WillHit = willHit;
            ArrivalDsp = arrivalDsp;
            _from = from;
            _to = to;
            _startDsp = startDsp;
            _durationSec = Mathf.Max(0.01f, (float)(arrivalDsp - startDsp));
            transform.localPosition = from;
            if (_label != null)
            {
                _label.text = "AIDA";
            }

            gameObject.SetActive(true);
        }

        public float ProgressAt(double nowDsp)
        {
            if (_durationSec <= 0.0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)((nowDsp - _startDsp) / _durationSec));
        }

        public void PlaceAt(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            transform.localPosition = Vector3.Lerp(_from, _to, clamped);
        }

        public void Hide()
        {
            TargetId = -1;
            WillHit = false;
            gameObject.SetActive(false);
        }
    }
}
