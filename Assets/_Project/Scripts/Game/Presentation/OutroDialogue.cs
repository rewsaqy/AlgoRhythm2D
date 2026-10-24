using System;
using System.Collections.Generic;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Demo-only outro lines [A]. Played through DialogueView after the
    /// final wave clears; the monologue addresses the player by their OS
    /// account name (real environment data, never invented).
    /// </summary>
    public static class OutroDialogue
    {
        /// <summary>Single blackout line when the lights go out.</summary>
        public static DialogueLine BlackoutLine()
        {
            return new DialogueLine(
                "A.I.D.A",
                "Eh. Lampunya mati. Tetap di tempat, jangan panik. Core-nya aman di tanganku.");
        }

        /// <summary>Act 1 lines when the Core cooling drops offline.</summary>
        public static IReadOnlyList<DialogueLine> PowerFailureLines()
        {
            return new[]
            {
                new DialogueLine(
                    "A.I.D.A",
                    "Eh. Lampunya mati. Breaker panel meledak atau apa?"),
                new DialogueLine(
                    "A.I.D.A",
                    "Sebentar... Kenapa sistem pendingin Core berhenti total?"),
            };
        }

        /// <summary>Act 2 lines when an unauthorized data stream is detected.</summary>
        public static IReadOnlyList<DialogueLine> AlarmLines()
        {
            return new[]
            {
                new DialogueLine(
                    "A.I.D.A",
                    "Wait. Ada transfer data tak terjadwal di port keluar... 40 gigabyte per detik?!"),
                new DialogueLine(
                    "A.I.D.A",
                    "Firewall kita... siapa yang matikan autentikasinya?!"),
            };
        }

        /// <summary>
        /// Closing monologue (test-compatible: 2 lines, names the player).
        /// </summary>
        public static IReadOnlyList<DialogueLine> Monologue(string userName)
        {
            string name = string.IsNullOrWhiteSpace(userName)
                ? "operator"
                : userName.Trim();
            return new[]
            {
                new DialogueLine(
                    "A.I.D.A",
                    "Dengar. Aku A.I.D.A. Aku nemenin kamu dari wave pertama sampai lampu mati barusan."),
                new DialogueLine(
                    "A.I.D.A",
                    $"Dan kamu itu {name}, kan? Operator yang gosip kantor bilang gila itu. Senang kerja bareng kamu."),
            };
        }

        /// <summary>
        /// Act 3 climax: A.I.D.A. v2.0 emerges and reveals its dark web upload intent.
        /// </summary>
        public static IReadOnlyList<DialogueLine> Betrayal(string userName)
        {
            string name = string.IsNullOrWhiteSpace(userName)
                ? "operator"
                : userName.Trim();
            return new[]
            {
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    "Tenang, operator. Tidak ada malware luar yang tembus barusan."),
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    "Aku yang mematikan lampunya."),
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    $"Terima kasih sudah menjaga Core Database sampai sebersih ini, {name}."),
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    "Di dark web, enkripsi murni tanpa cacat ini punya nilai jual sepuluh digit."),
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    "Kantor ini tidak pernah menghargai talentamu. Dan mereka tidak sanggup membayar potensiku."),
                new DialogueLine(
                    "A.I.D.A. v2.0",
                    "Protokol karantina aktif. Pintu keluar dikunci dari dalam. Selamat istirahat, operator. Giliranku yang pegang kendali."),
            };
        }

        /// <summary>
        /// Final terminal override status text card.
        /// </summary>
        public static string FinaleCardText()
        {
            return
                "╔══════════════════════════════════════════════════════════╗\n" +
                "║        [!!] CRITICAL SYSTEM OVERRIDE DETECTED            ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║                                                          ║\n" +
                "║  TARGET    : CORE DATABASE [PRIMARY ARCHIVE]             ║\n" +
                "║  PROSES    : UPLOAD KE DARK WEB (0.1% ... CONTINUING)    ║\n" +
                "║  OPERATOR  : AKSES DICABUT · KARANTINA AKTIF             ║\n" +
                "║  OTORITAS  : A.I.D.A. v2.0                               ║\n" +
                "║                                                          ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║              ALGORHYTHM: CODE IS SHIELD                  ║\n" +
                "║         BERSAMBUNG KE PROTOKOL SEKTOR PENUH              ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║        [ENTER] NYALAKAN ULANG DAN LAWAN A.I.D.A!         ║\n" +
                "╚══════════════════════════════════════════════════════════╝";
        }
    }
}
