using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Paddle Beatblock: busur orbit di sekitar Core, lebar sesuai derajat.
    /// Park di sudut target (snap, tanpa transisi); Pulse saat tap berhasil.
    /// Tidak membuat GO baru. Skala dasar stabil: SetHalfArc idempoten dan
    /// tidak pernah membaca balik scale yang sedang di-pulse.
    /// </summary>
    public sealed class PaddleView : MonoBehaviour
    {
        private Vector3 _authoredScale = Vector3.one;
        private float _halfArcDeg = 35f;
        private float _appliedHalfArcDeg = -1f;
        private float _pulseUntil;

        public void Initialize()
        {
            _authoredScale = transform.localScale;
            _appliedHalfArcDeg = -1f;
            _pulseUntil = 0f;
            ApplyHalfArc();
            gameObject.SetActive(true);
        }

        /// <summary>Setengah busur paddle (derajat) untuk visual arc.</summary>
        public void SetHalfArc(float halfArcDeg)
        {
            _halfArcDeg = halfArcDeg;
            if (_pulseUntil <= 0f)
            {
                ApplyHalfArc();
            }
        }

        public void Park(Vector3 localPosition, float angleDeg)
        {
            gameObject.SetActive(true);
            transform.localPosition = localPosition;
            // Sudut kompas searah jarum jam; rotasi Z Unity berlawanan arah,
            // jadi negasi agar sumbu panjang (X lokal) tetap tangen di semua sudut.
            transform.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
        }

        public void Pulse()
        {
            _pulseUntil = Time.unscaledTime + 0.28f;
            transform.localScale = BaseScale() * 1.45f;
        }

        private void Update()
        {
            if (_pulseUntil > 0f)
            {
                if (Time.unscaledTime >= _pulseUntil)
                {
                    _pulseUntil = 0f;
                    ApplyHalfArc();
                }
                else
                {
                    float t = 1f - (_pulseUntil - Time.unscaledTime) / 0.28f;
                    float scale = Mathf.Lerp(1.45f, 1.0f, t * t);
                    transform.localScale = BaseScale() * scale;
                }
            }
            else if (!Mathf.Approximately(_appliedHalfArcDeg, _halfArcDeg))
            {
                ApplyHalfArc();
            }
        }

        private Vector3 BaseScale()
        {
            // Greybox: lebarkan bar proporsional terhadap 35° acuan.
            float w = Mathf.Max(0.2f, _halfArcDeg / 35f);
            return new Vector3(_authoredScale.x * w, _authoredScale.y, 1f);
        }

        private void ApplyHalfArc()
        {
            transform.localScale = BaseScale();
            _appliedHalfArcDeg = _halfArcDeg;
        }
    }
}
