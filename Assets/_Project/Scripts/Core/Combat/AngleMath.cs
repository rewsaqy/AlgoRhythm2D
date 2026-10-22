namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Sudut kompas dalam derajat: 0 = Utara, 90 = Timur, 180 = Selatan, 270 = Barat.
    /// Semua operasi murni matematika, tanpa Unity, aman untuk EditMode.
    /// </summary>
    public static class AngleMath
    {
        /// <summary>Normalisasi ke [0, 360).</summary>
        public static float Normalize360(float angleDeg)
        {
            float a = angleDeg % 360f;
            if (a < 0f)
            {
                a += 360f;
            }

            return a;
        }

        /// <summary>Delta terpendek dari `from` ke `to`, rentang (-180, 180].</summary>
        public static double ShortestDelta(double fromDeg, double toDeg)
        {
            double delta = (toDeg - fromDeg) % 360.0;
            if (delta > 180.0)
            {
                delta -= 360.0;
            }
            else if (delta <= -180.0)
            {
                delta += 360.0;
            }

            return delta;
        }

        /// <summary>
        /// True bila dua busur sudut tumpang tindih (inklusif di tepi).
        /// centerA/halfA = note, centerB/halfB = paddle.
        /// </summary>
        public static bool Overlaps(float centerA, float halfA, float centerB, float halfB)
        {
            double delta = ShortestDelta(centerB, centerA);
            return System.Math.Abs(delta) <= (double)halfA + (double)halfB + 1e-6;
        }

        /// <summary>Pemetaan Direction lama ke sudut, identik dengan ShieldController.</summary>
        public static float DirectionToAngle(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return 0f;
                case Direction.East: return 90f;
                case Direction.South: return 180f;
                default: return 270f;
            }
        }

        /// <summary>
        /// Sudut kompas dari vektor layar (dx, dy): atan2(dx, dy).
        /// 0 = Utara, searah jarum jam. Sama dengan konvensi ShieldController.
        /// </summary>
        public static float AngleFromVector(float dx, float dy)
        {
            return Normalize360((float)(System.Math.Atan2(dx, dy) * (180.0 / System.Math.PI)));
        }
    }
}
