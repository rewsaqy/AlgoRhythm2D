namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Demo-only dialogue lines [A]. Every line is addressed by text_id;
    /// routing stays on the id, the view shows only the line. A.I.D.A. 1.0
    /// teaches the tutorial, 2.0 appears after 1B clear — a demo compression,
    /// not production canon.
    /// Rule/energy system is temporarily OFF (manual-shield only until the
    /// final AIDA form); dialogue therefore never mentions Energy or slots.
    /// </summary>
    public static class DemoDialogue
    {
        public const string CalibrationPrefKey = "AlgoRhythm.CalibrationMs";

        public static void LineForWaveStart(int waveIndex, out string textId, out string line)
        {
            if (waveIndex <= 0)
            {
                textId = "TUTO_01";
                line = "A.I.D.A: Hallo, selamat datang di Vierway! Aku AIDA, asisten barumu yang ditugaskan melindungi Data Base ini.\n"
                    + "A.I.D.A: Ini latihan menembak — arahkan Shield ke ATAS / BAWAH / KANAN / KIRI sesuai arah datangnya, tekan sekali lagi untuk menghancurkan malware dan virus yang masuk.\n"
                    + "A.I.D.A: Semangat...!";
                return;
            }

            textId = "AIDA2_01";
            line = "A.I.D.A 2.0: bagus. Sekarang lebih cepat dan ada umpan — abaikan decoy, fokus arah yang benar!";
        }

        public static void LineForWaveClear(string waveId, bool runDone, out string textId, out string line)
        {
            if (runDone)
            {
                textId = "RUN_CLEAR";
                line = "A.I.D.A 2.0: run selesai — 1A dan 1B clear. Bagus!";
                return;
            }

            textId = "CLEAR_" + waveId;
            line = string.Format(
                "A.I.D.A: wave {0} clear — lanjut ke berikutnya!",
                waveId);
        }
    }
}
