using System.Collections.Generic;
using System.Linq;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Presentation;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    /// <summary>
    /// Demo run contract: Tutorial - Lagu 1 - Lagu 2 - teaser. Every new
    /// thing has its own blocking tutorial, and no visible line uses an
    /// em/en dash (antislop R-02).
    /// </summary>
    public sealed class DemoDialogueTests
    {
        private sealed class FakeClock : IClockSource
        {
            public double NowDsp { get; set; }
        }

        private static IEnumerable<string> AllLines()
        {
            foreach (DialogueLine line in DemoDialogue.TutorialScript())
            {
                yield return line.Speaker + " : " + line.Text;
            }

            yield return DemoDialogue.PracticeHint();
            yield return DemoDialogue.PracticePraise();
            yield return DemoDialogue.PracticeRetry();
            foreach (string waveId in new[] { "WS1", "WS2" })
            {
                foreach (DialogueLine line in DemoDialogue.SongIntroScript(waveId) ?? new DialogueLine[0])
                {
                    yield return line.Speaker + " : " + line.Text;
                }

                DemoDialogue.WaveStartLine(waveId, out string textId, out string lineText);
                yield return textId + " " + lineText;
            }

            foreach (DialogueLine line in new[] { OutroDialogue.BlackoutLine() })
            {
                yield return line.Speaker + " : " + line.Text;
            }

            foreach (DialogueLine line in OutroDialogue.Monologue())
            {
                yield return line.Speaker + " : " + line.Text;
            }
        }

        [Test]
        public void TutorialScript_HasSpeakerOnEveryLine()
        {
            IReadOnlyList<DialogueLine> script = DemoDialogue.TutorialScript();
            Assert.IsTrue(script.Count >= 4);
            foreach (DialogueLine line in script)
            {
                Assert.IsFalse(string.IsNullOrEmpty(line.Speaker), "speaker missing");
                Assert.IsFalse(string.IsNullOrEmpty(line.Text), "text missing");
            }
        }

        [Test]
        public void SongIntros_TeachWhatsNew()
        {
            IReadOnlyList<DialogueLine> ws1 = DemoDialogue.SongIntroScript("WS1");
            IReadOnlyList<DialogueLine> ws2 = DemoDialogue.SongIntroScript("WS2");
            Assert.IsNotNull(ws1);
            Assert.IsNotNull(ws2);
            Assert.IsTrue(ws1.Count >= 2, "WS1 must teach song + PTS + shop");
            Assert.IsTrue(ws2.Count >= 3, "WS2 must teach the new roster");
            string ws1Text = string.Join(" ", ws1.Select(l => l.Text));
            StringAssert.Contains("PTS", ws1Text);
            StringAssert.Contains("SHOP", ws1Text);
            string ws2Text = string.Join(" ", ws2.Select(l => l.Text));
            StringAssert.Contains("Heavy", ws2Text);
            StringAssert.Contains("Holder", ws2Text);
            StringAssert.Contains("Adware", ws2Text);
        }

        [Test]
        public void SongIntro_UnknownWave_ReturnsNull()
        {
            Assert.IsNull(DemoDialogue.SongIntroScript("W9X"));
            DemoDialogue.WaveStartLine("W1T", out string _, out string line);
            Assert.IsTrue(string.IsNullOrEmpty(line));
        }

        [Test]
        public void WaveStartLines_ExistForBothSongs()
        {
            DemoDialogue.WaveStartLine("WS1", out string id1, out string line1);
            DemoDialogue.WaveStartLine("WS2", out string id2, out string line2);
            Assert.IsFalse(string.IsNullOrEmpty(id1));
            Assert.IsFalse(string.IsNullOrEmpty(line1));
            Assert.IsFalse(string.IsNullOrEmpty(id2));
            Assert.IsFalse(string.IsNullOrEmpty(line2));
        }

        [Test]
        public void Outro_BlackoutThenTerminal()
        {
            DialogueLine blackout = OutroDialogue.BlackoutLine();
            Assert.AreEqual("KAMU", blackout.Speaker);
            IReadOnlyList<DialogueLine> mono = OutroDialogue.Monologue();
            Assert.IsTrue(mono.Count >= 4);
            Assert.IsTrue(mono.Any(l => l.Text.Contains("DATA TELAH DIJEBOL")));
            Assert.IsTrue(mono.Any(l => l.Speaker == "TERMINAL"));
            Assert.IsTrue(mono[mono.Count - 1].Text.Contains("TO BE CONTINUED"));
        }

        [Test]
        public void NoEmOrEnDashInDemoCopy()
        {
            foreach (string text in AllLines())
            {
                Assert.IsFalse(text.Contains("—"), "em dash in: " + text);
                Assert.IsFalse(text.Contains("–"), "en dash in: " + text);
            }
        }

        [Test]
        public void WaveDirector_WaveIdAt_ReturnsIdsAndEmptyOutOfRange()
        {
            var clock = new BeatClock(new FakeClock(), 100.0);
            var director = new WaveDirector(
                new[]
                {
                    new WaveData("W1T", 100.0, new ThreatEvent[0]),
                    new WaveData("WS1", 100.0, new ThreatEvent[0]),
                },
                clock,
                new AlgoRhythm.Game.Rhythm.DspClockSource(),
                2);
            Assert.AreEqual("W1T", director.WaveIdAt(0));
            Assert.AreEqual("WS1", director.WaveIdAt(1));
            Assert.AreEqual(string.Empty, director.WaveIdAt(2));
            Assert.AreEqual(string.Empty, director.WaveIdAt(-1));
        }
    }
}
