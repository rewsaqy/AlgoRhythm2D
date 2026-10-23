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
    }
}
