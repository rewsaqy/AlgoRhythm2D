using AlgoRhythm.Game.Presentation;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class OutroDialogueTests
    {
        [Test]
        public void Monologue_NamesThePlayer()
        {
            var lines = OutroDialogue.Monologue("zxwyvern");

            Assert.AreEqual(2, lines.Count);
            Assert.IsTrue(lines[1].Text.Contains("zxwyvern"));
        }

        [Test]
        public void Monologue_BlankName_FallsBackToOperator()
        {
            foreach (string blank in new[] { null, "", "   " })
            {
                var lines = OutroDialogue.Monologue(blank);

                Assert.IsTrue(lines[1].Text.Contains("operator"));
                Assert.IsFalse(lines[1].Text.Contains("  "));
            }
        }

        [Test]
        public void BlackoutLine_HasNoDashAsHyphen()
        {
            string text = OutroDialogue.BlackoutLine().Text;

            Assert.IsFalse(string.IsNullOrEmpty(text));
            Assert.IsFalse(text.Contains("--"));
            Assert.IsFalse(text.Contains("—"));
        }

        [Test]
        public void Betrayal_NamesThePlayerAndSpeakerIsAidaV2()
        {
            var lines = OutroDialogue.Betrayal("zxwyvern");

            Assert.GreaterOrEqual(lines.Count, 4);
            Assert.AreEqual("A.I.D.A. v2.0", lines[0].Speaker);
            bool foundName = false;
            foreach (var line in lines)
            {
                Assert.IsFalse(line.Text.Contains("--"));
                Assert.IsFalse(line.Text.Contains("—"));
                if (line.Text.Contains("zxwyvern"))
                {
                    foundName = true;
                }
            }
            Assert.IsTrue(foundName);
        }

        [Test]
        public void PowerFailureAndAlarmLines_HaveNoDashes()
        {
            foreach (var line in OutroDialogue.PowerFailureLines())
            {
                Assert.IsFalse(line.Text.Contains("--"));
                Assert.IsFalse(line.Text.Contains("—"));
            }

            foreach (var line in OutroDialogue.AlarmLines())
            {
                Assert.IsFalse(line.Text.Contains("--"));
                Assert.IsFalse(line.Text.Contains("—"));
            }
        }

        [Test]
        public void FinaleCardText_HasNoDashesAndFitsLines()
        {
            string card = OutroDialogue.FinaleCardText();
            Assert.IsFalse(card.Contains("--"));
            Assert.IsFalse(card.Contains("—"));
            string[] lines = card.Split('\n');
            Assert.AreEqual(15, lines.Length);
            foreach (string line in lines)
            {
                Assert.AreEqual(60, line.Length);
            }
        }
    }
}
