using System.Collections.Generic;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Demo-only outro lines [A]. Played through DialogueView after the
    /// final wave clears: blackout, breach, read-only terminal, stop.
    /// </summary>
    public static class OutroDialogue
    {
        /// <summary>Single blackout line when the lights go out.</summary>
        public static DialogueLine BlackoutLine()
        {
            return new DialogueLine(
                "KAMU",
                "Aneh, biasanya kantor nggak mati lampu gini.");
        }

        /// <summary>
        /// Closing beats: AIDA is confused too, the breach banner drops with
        /// the siren, then the read-only terminal. Last line stops the demo.
        /// </summary>
        public static IReadOnlyList<DialogueLine> Monologue()
        {
            return new[]
            {
                new DialogueLine(
                    "A.I.D.A",
                    "Core-ku ikut padam. Operator, ini bukan mati lampu biasa."),
                new DialogueLine(
                    "SISTEM",
                    "DATA TELAH DIJEBOL"),
                new DialogueLine(
                    "TERMINAL",
                    "> breach sektor 2. jejak upload asing terdeteksi. koneksi operator terputus."),
                new DialogueLine(
                    "A.I.D.A",
                    "Itu bukan aku. Ada yang lain di dalam sistem ini."),
                new DialogueLine(
                    "SISTEM",
                    "TO BE CONTINUED. Terima kasih sudah main demo ini."),
            };
        }
    }
}
