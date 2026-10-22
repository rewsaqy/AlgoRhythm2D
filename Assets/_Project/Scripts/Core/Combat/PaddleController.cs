using System;

namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Paddle sudut kontinu (Beatblock): target sudut + batas kecepatan opsional.
    /// Ukuran paddle dalam derajat (setengah busur). Tanpa Unity, tanpa frame.
    /// </summary>
    public sealed class PaddleController
    {
        private float _current;
        private float _target;
        private readonly float _maxDegPerSec;

        /// <param name="initialAngleDeg">Sudut awal (Selatan = 180, parkir lawas).</param>
        /// <param name="maxDegPerSec">Batas slew; &lt;= 0 berarti tak terbatas.</param>
        public PaddleController(float initialAngleDeg = 180f, float maxDegPerSec = 0f)
        {
            _current = AngleMath.Normalize360(initialAngleDeg);
            _target = _current;
            _maxDegPerSec = maxDegPerSec;
        }

        public float CurrentAngle => _current;

        public float TargetAngle => _target;

        /// <summary>Set target sudut; gerak aktual dihitung di Update.</summary>
        public void SetTarget(float angleDeg)
        {
            _target = AngleMath.Normalize360(angleDeg);
        }

        /// <summary>Maju simulasi ke nowDsp dengan batas kecepatan (bila ada).</summary>
        public void Update(double nowDsp, double lastDsp)
        {
            double dt = nowDsp - lastDsp;
            if (dt <= 0.0)
            {
                // Tetap snap bila tak terbatas agar SetTarget langsung terasa.
                if (_maxDegPerSec <= 0f) _current = _target;
                return;
            }

            if (_maxDegPerSec <= 0f)
            {
                _current = _target;
                return;
            }

            double delta = AngleMath.ShortestDelta(_current, _target);
            double maxStep = _maxDegPerSec * dt;
            if (Math.Abs(delta) <= maxStep)
            {
                _current = _target;
            }
            else
            {
                _current = AngleMath.Normalize360((float)(_current + Math.Sign(delta) * maxStep));
            }
        }

        /// <summary>Langsung snap ke target (dipakai saat tak ada smoothing).</summary>
        public void SnapToTarget()
        {
            _current = _target;
        }

        /// <summary>
        /// True bila paddle menutupi note pada sudutnya.
        /// Center-only: hanya pusat note vs setengah busur paddle.
        /// noteHalfWidthDeg hanya visual; tidak ikut judging agar diagonal
        /// 45° tidak menutupi N (0°) dan E (90°) sekaligus (35+10=45 bocor).
        /// </summary>
        public bool Covers(float noteAngleDeg, float noteHalfWidthDeg, float paddleHalfArcDeg)
        {
            _ = noteHalfWidthDeg;
            return AngleMath.Overlaps(noteAngleDeg, 0f, _current, paddleHalfArcDeg);
        }
    }
}
