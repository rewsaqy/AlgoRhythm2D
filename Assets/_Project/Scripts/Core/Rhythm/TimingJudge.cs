using System;

namespace AlgoRhythm.Core.Rhythm
{
    /// <summary>
    /// Grades a DSP input timestamp against a DSP impact timestamp.
    /// Boundaries are inclusive: <= 70 ms is Perfect, <= 130 ms is Good.
    /// </summary>
    public sealed class TimingJudge
    {
        // Absorbs binary floating-point error at exact millisecond boundaries
        // such as Impact + 0.070 seconds.
        private const double BoundaryEpsilonMs = 1e-6;

        private double _calibrationOffsetMs;

        public TimingJudge(double perfectWindowMs = 70.0, double goodWindowMs = 130.0, double maxCalibrationOffsetMs = 150.0)
        {
            if (perfectWindowMs < 0 || double.IsNaN(perfectWindowMs))
            {
                throw new ArgumentOutOfRangeException(nameof(perfectWindowMs));
            }

            if (goodWindowMs < perfectWindowMs || double.IsNaN(goodWindowMs))
            {
                throw new ArgumentOutOfRangeException(nameof(goodWindowMs));
            }

            if (maxCalibrationOffsetMs < 0 || double.IsNaN(maxCalibrationOffsetMs))
            {
                throw new ArgumentOutOfRangeException(nameof(maxCalibrationOffsetMs));
            }

            PerfectWindowMs = perfectWindowMs;
            GoodWindowMs = goodWindowMs;
            MaxCalibrationOffsetMs = maxCalibrationOffsetMs;
        }

        public double PerfectWindowMs { get; }

        public double GoodWindowMs { get; }

        public double MaxCalibrationOffsetMs { get; }

        /// <summary>
        /// Player calibration offset. Positive values make late input count as
        /// on-time. Clamped to +/-MaxCalibrationOffsetMs.
        /// </summary>
        public double CalibrationOffsetMs
        {
            get => _calibrationOffsetMs;
            set => _calibrationOffsetMs = Math.Max(-MaxCalibrationOffsetMs, Math.Min(MaxCalibrationOffsetMs, value));
        }

        public double ErrorMs(double inputDsp, double expectedImpactDsp)
        {
            return (inputDsp - expectedImpactDsp) * 1000.0 - _calibrationOffsetMs;
        }

        public TimingGrade Grade(double inputDsp, double expectedImpactDsp)
        {
            double absoluteErrorMs = Math.Abs(ErrorMs(inputDsp, expectedImpactDsp));
            if (absoluteErrorMs <= PerfectWindowMs + BoundaryEpsilonMs)
            {
                return TimingGrade.Perfect;
            }

            if (absoluteErrorMs <= GoodWindowMs + BoundaryEpsilonMs)
            {
                return TimingGrade.Good;
            }

            return TimingGrade.Miss;
        }
    }
}
