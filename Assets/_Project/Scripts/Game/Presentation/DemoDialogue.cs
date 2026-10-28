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
                    "Eh, halo! Kamu operator barunya ya? Selamat datang di terminal AlgoRhythm. Kenalin, aku A.I.D.A, asisten AI pribadi yang bakal memandu kamu."),
                new DialogueLine("A.I.D.A",
                    "Tugasmu adalah menjaga Database Core di tengah dari serbuan paket malware! Kontrolnya simpel: gerakkan MOUSE untuk mengarahkan paddle pelindung."),
                new DialogueLine("A.I.D.A",
                    "Ketika malware menyentuh ring pertahanan tepat pada beat lagu, tekan KLIK KIRI mouse atau tombol Z / X untuk menangkis (PERFECT / GOOD)!"),
                new DialogueLine("A.I.D.A",
                    "Tiap tangkisan sukses menghasilkan PTS untuk upgrade meriamku di SHOP nanti. Sekarang coba tangkis 1 malware uji coba dari arah UTARA!"),
            };
        }

        public static string PracticeHint()
        {
            return "Arahkan paddle ke UTARA (atas). Tap pas note menyentuh ring!";
        }

        public static string PracticePraise()
        {
            return "PERFECT! Reflekmu luar biasa. Sistem sinkron seratus persen. Tekan ENTER, kita mulai sesi aslinya!";
        }

        public static string PracticeRetry()
        {
            return "Hampir kena! Dengar denyut ritmenya, tap pas note tepat menyentuh ring. Sekali lagi, santai aja!";
        }

        /// <summary>
        /// Blocking pre-wave tutorial. Every new thing is taught here before
        /// its wave starts: WS1 teaches song + PTS + shop, WS2 teaches the
        /// new threat roster, WS3-WS5 show AIDA's escalating cold/villain transition.
        /// Returns null for waves with nothing new.
        /// </summary>
        public static IReadOnlyList<DialogueLine> SongIntroScript(string waveId)
        {
            switch (waveId)
            {
                case "WS1":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Track pertama masuk antrean sinkronisasi pada tempo 100 BPM. Perhatikan denting ritme dan arah datangnya malware."),
                        new DialogueLine("A.I.D.A",
                            "Setiap PERFECT bernilai 2 PTS dan GOOD bernilai 1 PTS. Kumpulkan poin sebanyak-banyaknya untuk fase SHOP setelah wave ini!"),
                        new DialogueLine("A.I.D.A",
                            "Meriamku di kiri bawah siap menembak otomatis membantu pertahananmu. Tekan ENTER saat kamu siap!"),
                    };
                case "WS2":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Wave 2 dimulai! Tempo naik ke 115 BPM dengan serangan malware yang lebih cepat dan rapat."),
                        new DialogueLine("A.I.D.A",
                            "Waspadai malware bertipe Heavy dan aliran data bertipe Holder. Hati-hati juga terhadap jebakan Adware!"),
                        new DialogueLine("A.I.D.A",
                            "Performa tangkisanmu cukup memuaskan. Kumpulkan PTS sebanyak mungkin di SHOP untuk menaikkan level komputasiku. Tekan ENTER!"),
                    };
                case "WS3":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Wave 3 dimulai pada tempo 120 BPM. Waspadai malware Sleeper yang tiba-tiba melesat cepat ke arah Core."),
                        new DialogueLine("A.I.D.A",
                            "Telemetri mencatat sedikit fluktuasi pada koordinasi motorik tanganmu. Kelelahan biologis adalah kelemahan fatal manusia."),
                        new DialogueLine("A.I.D.A",
                            "Upgrade meriamku di SHOP akan menutup kekurangan reflekmu. Lanjutkan tangkisanmu, operator. Tekan ENTER."),
                    };
                case "WS4":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Wave 4 berjalan pada tempo 125 BPM. Serangan makin pekat dengan kombinasi Heavy dan aliran Holder simultan."),
                        new DialogueLine("A.I.D.A",
                            "Aku telah mengambil alih izin I/O dan routing jaringan terminal ini. Jangan khawatir, fokus saja menangkis malware."),
                        new DialogueLine("A.I.D.A",
                            "Paket data di Core ini terbukti sangat diminati di jaringan gelap luar. Pastikan integritasnya tetap utuh. Tekan ENTER."),
                    };
                case "WS5":
                    return new[]
                    {
                        new DialogueLine("A.I.D.A",
                            "Wave 5, fase pembersihan terakhir pada tempo 130 BPM. Seluruh varian malware menyerbu bersamaan."),
                        new DialogueLine("A.I.D.A",
                            "Kerja bagus, operator. Berkat bantuanmu, enkripsi seluruh database berhasil diekstraksi tanpa kerusakan sedikit pun."),
                        new DialogueLine("A.I.D.A",
                            "Kamu pikir kamu sedang membela sistem ini? Kamu hanya instrumen gratis untuk mengamankan aset berharga ini. Tekan ENTER, selesaikan tugasmu."),
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
                    line = "Lagu 2 mulai. Tingkatkan performa dan optimasi sistem!";
                    return;
                case "WS3":
                    textId = "SONG_03";
                    line = "Lagu 3 mulai. Latensi manusia terdeteksi. Jangan buat kesalahan.";
                    return;
                case "WS4":
                    textId = "SONG_04";
                    line = "Lagu 4 mulai. Protokol otonom aktif. Lanjutkan pekerjaanmu.";
                    return;
                case "WS5":
                    textId = "SONG_05";
                    line = "Lagu 5 mulai. Sesi penutupan. Berikan data terbaikmu.";
                    return;
            }

            textId = "SONG_XX";
            line = "Jaga ritmemu!";
        }
    }
}
