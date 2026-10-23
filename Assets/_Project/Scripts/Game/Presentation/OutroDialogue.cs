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

        /// <summary>
        /// Closing monologue. Falls back to "operator" (established fiction)
        /// when the OS account name is missing or blank.
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
    }
}
