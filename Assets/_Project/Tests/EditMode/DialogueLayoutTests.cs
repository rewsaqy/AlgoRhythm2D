using AlgoRhythm.Game.Presentation;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class DialogueLayoutTests
    {
        [Test]
        public void Wrap_ShortText_ReturnsUnchanged()
        {
            Assert.AreEqual("Halo.", DialogueLayout.Wrap("Halo.", 52, 4));
        }

        [Test]
        public void Wrap_LongTutorialLine_StaysWithinBounds()
        {
            const string body = "Oke oke, pemanasan dulu biar nggak kaku. Liat lingkaran di tengah itu? Itu Core kita. Caranya gampang: gerakin MOUSE buat ngarahin paddle ke ancaman, terus tekan Z / X / KLIK pas not-nya nempel ring.";
            string wrapped = DialogueLayout.Wrap(body, 56, 5);

            string[] lines = wrapped.Split('\n');
            Assert.LessOrEqual(lines.Length, 5);
            foreach (string line in lines)
            {
                Assert.LessOrEqual(line.Length, 56);
            }

            Assert.AreEqual(body, wrapped.Replace("\n", " "));
        }

        [Test]
        public void WrapBody_KeepsSpeakerBudgetOnFirstLine()
        {
            string wrapped = DialogueLayout.WrapBody("A.I.D.A : ", "Nih aku siapin satu malware jinak dari UTARA.", 20, 4);

            string[] lines = wrapped.Split('\n');
            Assert.GreaterOrEqual(lines.Length, 2);
            Assert.AreEqual("Nih aku", lines[0]);
            foreach (string line in lines)
            {
                Assert.LessOrEqual(line.Length, 20);
            }
        }

        [Test]
        public void Wrap_RespectsMaxLines()
        {
            string wrapped = DialogueLayout.Wrap(
                "satu dua tiga empat lima enam tujuh delapan sembilan sepuluh sebelas dua belas",
                10, 2);

            Assert.AreEqual(2, wrapped.Split('\n').Length);
        }

        [Test]
        public void Wrap_EmptyInput_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, DialogueLayout.Wrap(string.Empty, 52, 4));
            Assert.AreEqual(string.Empty, DialogueLayout.WrapBody("A.I.D.A : ", string.Empty, 52, 4));
        }
    }
}
