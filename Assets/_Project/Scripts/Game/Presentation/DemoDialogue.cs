using System.Collections.Generic;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Demo-only dialogue lines [A]. Every line is addressed by text_id; the
    /// TutorialDriver shows them through DialogueView (typewriter), so no
    /// text is dumped all at once. A.I.D.A. 1.0 teaches the tutorial, 2.0
    /// appears after 1B clear — a demo compression, not production canon.
    /// Rule/energy system is temporarily OFF (manual-shield only until the
    /// final AIDA form); dialogue therefore never mentions Energy or slots.
    /// </summary>
    public static class DemoDialogue
    {
        public const string CalibrationPrefKey = "AlgoRhythm.CalibrationMs";

        /// <summary>Tutorial script: greeting, aim, tap, then practice.</summary>
        public static IReadOnlyList<DialogueLine> TutorialScript()
        {
            return new[]
            {
                new DialogueLine("A.I.D.A",
                    "Halo, Operator. Aku A.I.D.A — asisten yang ditugaskan melindungimu."),
                new DialogueLine("A.I.D.A",
                    "Gerakkan MOUSE untuk mengarahkan paddle ke arah datangnya ancaman."),
                new DialogueLine("A.I.D.A",
                    "Lalu tekan Z / X / KLIK tepat saat note menyentuh ring. Latihan dulu: SATU malware jinak datang dari UTARA. Beri aku satu PERFECT."),
            };
        }

        public static string PracticeHint()
        {
            return "Arahkan paddle ke UTARA (atas). Tekan Z / X / klik TEPAT saat note menyentuh ring!";
        }

        public static string PracticePraise()
        {
            return "SEMPURNA. Itu namanya PERFECT — ingat rasanya. Tekan ENTER, misi dimulai.";
        }

        public static string PracticeRetry()
        {
            return "Belum pas. Coba lagi — tekan TEPAT saat note menyentuh ring.";
        }

        public static void WaveStartLine(string waveId, out string textId, out string line)
        {
            switch (waveId)
            {
                case "W1T":
                    textId = string.Empty;
                    line = string.Empty;
                    return;
                case "W1A":
                    textId = "TUTO_01";
                    line = "A.I.D.A: Wave 1A — blokir virus ikut irama! Arahkan paddle, tap tepat beat!";
                    return;
                case "W1B":
                    textId = "AIDA2_01";
                    line = "A.I.D.A 2.0: bagus. Sekarang lebih cepat dan ada umpan — abaikan decoy, fokus arah yang benar!";
                    return;
                case "W1C":
                    textId = "AIDA2_C";
                    line = "A.I.D.A 2.0: kenalkan pasukan baru — baris, armor, penyusup, dan note panjang!";
                    return;
                case "W1D":
                    textId = "AIDA2_D";
                    line = "A.I.D.A 2.0: gauntlet penuh. Semua yang kau pelajari, sekarang!";
                    return;
                default:
                    textId = "AIDA2_01";
                    line = "A.I.D.A 2.0: bagus. Sekarang lebih cepat — jaga ritmemu!";
                    return;
            }
        }
    }
}
