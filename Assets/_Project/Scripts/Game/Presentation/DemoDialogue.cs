using System.Collections.Generic;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Demo-only dialogue lines [A]. Every line is addressed by text_id; the
    /// drivers show them through DialogueView (typewriter), so no text is
    /// dumped all at once. Run order is Tutorial (W1T) - Lagu 1 (WS1) -
    /// Lagu 2 (WS2) - teaser. Every new thing gets its own tutorial script:
    /// W1T drills paddle + Perfect, WS1 teaches song + PTS + shop, WS2
    /// teaches the new threat roster. Rule/energy system is temporarily OFF
    /// (manual-shield only until the final AIDA form); dialogue therefore
    /// never mentions Energy or slots.
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

        /// <summary>
        /// Blocking pre-wave tutorial. Every new thing is taught here before
        /// its wave starts: WS1 teaches song + PTS + shop, WS2 teaches the
        /// new threat roster. Returns null for waves with nothing new.
        /// </summary>
        public static IReadOnlyList<DialogueLine> SongIntroScript(string waveId)
        {
            switch (waveId)
            {
                case "WS1":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Dengerin. Lagu pertama mau nyala. Notnya jalan ngikutin beat, jadi arahin paddle dan tap ngikutin dentingnya."),
                        new DialogueLine("A.I.D.A",
                            "Tiap Perfect kamu dapat 2 PTS, Good dapat 1. Kumpulin yang banyak. Habis lagu ada SHOP: beli REGEN biar tiap wave pulih, atau upgrade aku biar tembakanku makin rajin."),
                        new DialogueLine("A.I.D.A",
                            "Aku cuma bantu dari bawah, ya. Yang jaga ritme tetap kamu. Tekan ENTER kalau siap."),
                    };
                case "WS2":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Oke, lagu kedua. Isinya beda-beda, kenalan dulu biar nggak kaget."),
                        new DialogueLine("A.I.D.A",
                            "Yang badannya tebel itu Heavy, tap sampai pecahnya. Yang jalan baris itu Marcher. Yang tidur itu Sleeper, dia nge-dash pas beat terakhir."),
                        new DialogueLine("A.I.D.A",
                            "Ada juga note panjang, Holder. Tekan dan TAHAN sampai ekornya habis. Terus ada umpan Adware, itu cuekin aja, jangan di-tap."),
                        new DialogueLine("A.I.D.A",
                            "Siap? Tekan ENTER, lagunya mulai."),
                    };
                default:
                    return null;
            }
        }

        public static void WaveStartLine(string waveId, out string textId, out string line)
        {
            switch (waveId)
            {
                case "W1T":
                    textId = string.Empty;
                    line = string.Empty;
                    return;
                case "WS1":
                    textId = "SONG_01";
                    line = "Lagu 1 mulai. Ikutin beatnya, kumpulin PTS!";
                    return;
                case "WS2":
                    textId = "SONG_02";
                    line = "Lagu 2 mulai. Ingat bekal tadi, gas!";
                    return;
            }

            textId = "SONG_XX";
            line = "Jaga ritmemu!";
        }
    }
}
