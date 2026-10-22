namespace AlgoRhythm.Core.Combat
{
    /// <summary>
    /// Riwayat sudut paddle berkapasitas tetap, tanpa alokasi saat baca.
    /// Dipakai untuk menilai tap terhadap posisi paddle pada timestamp DSP input,
    /// bukan posisi frame saat ini.
    /// </summary>
    public sealed class PaddleHistory
    {
        private readonly double[] _dsp;
        private readonly float[] _angle;
        private readonly int _capacity;
        private int _count;
        private int _head;

        public PaddleHistory(int capacity = 256)
        {
            if (capacity < 2) capacity = 2;
            _capacity = capacity;
            _dsp = new double[capacity];
            _angle = new float[capacity];
            _count = 0;
            _head = 0;
        }

        public int Count => _count;

        /// <summary>Rekam sudut target paddle pada timestamp DSP. Sudut dinormalisasi.</summary>
        public void Record(double dsp, float angleDeg)
        {
            float a = AngleMath.Normalize360(angleDeg);
            _dsp[_head] = dsp;
            _angle[_head] = a;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }

        /// <summary>
        /// Sudut pada timestamp DSP: interpolasi linear lewat arc terpendek
        /// antara dua sampel terdekat. Di luar rentang: sampel terdekat.
        /// </summary>
        public float AngleAt(double dsp)
        {
            if (_count == 0) return 0f;
            if (_count == 1) return _angle[OldestIndex()];

            // Sampel disimpan melingkar; cari dua sampel yang mengapit dsp.
            // Karena Record dipanggil monoton naik, array logis terurut.
            int oldest = OldestIndex();
            double firstDsp = _dsp[oldest];
            if (dsp <= firstDsp) return _angle[oldest];

            int newest = (_head - 1 + _capacity) % _capacity;
            double lastDsp = _dsp[newest];
            if (dsp >= lastDsp) return _angle[newest];

            for (int i = 0; i < _count - 1; i++)
            {
                int a = (oldest + i) % _capacity;
                int b = (oldest + i + 1) % _capacity;
                double da = _dsp[a];
                double db = _dsp[b];
                if (dsp >= da && dsp <= db)
                {
                    if (db <= da) return _angle[b];
                    double t = (dsp - da) / (db - da);
                    double delta = AngleMath.ShortestDelta(_angle[a], _angle[b]);
                    return AngleMath.Normalize360((float)(_angle[a] + delta * t));
                }
            }

            return _angle[newest];
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
        }

        private int OldestIndex()
        {
            return (_head - _count + _capacity * 2) % _capacity;
        }
    }
}
