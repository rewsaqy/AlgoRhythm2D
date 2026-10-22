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

        /// <summary>Tutorial script: greeting, small talk, tap drill, then practice.</summary>
        public static IReadOnlyList<DialogueLine> TutorialScript()
        {
            return new[]
            {
                new DialogueLine("A.I.D.A",
                    "Eh, halo?! Kamu operator barunya ya? Akhirnya datang juga. Aku A.I.D.A, asisten yang bakal nemenin kamu di sini."),
                new DialogueLine("A.I.D.A",
                    "Eh sori, aku excited. Soalnya kantor heboh dari kemarin. Katanya yang ditugasin jaga Data Base ini anak baru yang kemampuannya gila banget. Itu kamu?!"),
                new DialogueLine("A.I.D.A",
                    "Oke oke, pemanasan dulu biar nggak kaku. Liat lingkaran di tengah itu? Itu Core kita. Caranya gampang: gerakin MOUSE buat ngarahin paddle ke ancaman, terus tekan Z / X / KLIK pas not-nya nempel ring."),
                new DialogueLine("A.I.D.A",
                    "Nih aku siapin satu malware jinak dari UTARA. Kasih aku satu PERFECT ya, biar aku liat sendiri gosip kantor itu beneran."),
            };
        }

        public static string PracticeHint()
        {
            return "Arahkan paddle ke UTARA (atas). Tap TEPAT pas note nempel ring!";
        }

        public static string PracticePraise()
        {
            return "NAH ITU DIA. Perfect! Oke, gosip kantor valid. Kamu beneran jago. Tekan ENTER, misi beneran dimulai.";
        }

        public static string PracticeRetry()
        {
            return "Yah, belum pas. Santai aja, ikutin dentingnya... tekan TEPAT pas nempel. Sekali lagi, bisa yuk!";
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
                    line = "Wave 1A mulai. Blokir virusnya ikut irama. Paddle siap, tap yang pas!";
                    return;
                case "W1B":
                    textId = "AIDA2_01";
                    line = "Buset, bagus. Oke naik level. Lebih cepat, plus ada umpan. Yang decoy cuekin aja!";
                    return;
                case "W1C":
                    textId = "AIDA2_C";
                    line = "Nih kenalin pasukan barunya: ada yang baris, badannya tebel, tukang tidur, sama note yang panjang. Seru kan?";
                    return;
                case "W1D":
                    textId = "AIDA2_D";
                    line = "Gauntlet! Semua yang kau pelajari, keluar bareng. Gas!";
                    return;
            }

            if (!string.IsNullOrEmpty(waveId) && waveId.StartsWith("W2"))
            {
                textId = "AIDA2_W2";
                line = "Gelombang 2. Tempo naik ke 115. Jangan kasih kendor!";
                return;
            }

            if (!string.IsNullOrEmpty(waveId) && waveId.StartsWith("W3"))
            {
                textId = "AIDA2_W3";
                line = "Gelombang 3. 130 BPM. Ini ujian beneran. Tahan!";
                return;
            }

            textId = "AIDA2_01";
            line = "Naikin tempo dikit. Jaga ritmemu!";
        }
    }
}
