using System.Linq;
using AlgoRhythm.Game.Presentation;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class OutroDialogueTests
    {
        [Test]
        public void BlackoutLine_IsKamuLightsOut()
        {
            DialogueLine line = OutroDialogue.BlackoutLine();

            Assert.AreEqual("KAMU", line.Speaker);
            Assert.IsTrue(line.Text.Contains("mati lampu"));
        }

        [Test]
        public void Monologue_BreachTerminalThenStop()
        {
            var lines = OutroDialogue.Monologue();

            Assert.IsTrue(lines.Count >= 4);
            Assert.IsTrue(lines.Any(l => l.Text.Contains("DATA TELAH DIJEBOL")));
            Assert.IsTrue(lines.Any(l => l.Speaker == "TERMINAL"));
            Assert.IsTrue(lines.All(l => !string.IsNullOrEmpty(l.Speaker)));
            Assert.IsTrue(lines[lines.Count - 1].Text.Contains("TO BE CONTINUED"));
        }

        [Test]
        public void BlackoutLine_HasNoDashAsHyphen()
        {
            string text = OutroDialogue.BlackoutLine().Text;

            Assert.IsFalse(string.IsNullOrEmpty(text));
            Assert.IsFalse(text.Contains("--"));
            Assert.IsFalse(text.Contains("—"));
        }
    }
}
