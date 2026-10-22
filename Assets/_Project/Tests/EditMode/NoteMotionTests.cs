using AlgoRhythm.Core.Combat;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class NoteMotionTests
    {
        [Test]
        public void Linear_MidpointIsHalf()
        {
            Assert.AreEqual(0.5f, NoteMotion.Progress(10.0, 12.0, 11.0, 0.6, NoteMotionMode.Linear, 0.25f), 1e-6);
        }

        [Test]
        public void Stepped_HoldsThenSteps()
        {
            // travel 2 beat (10→12), now 0.5 beat setelah spawn: masih tile 0.
            float early = NoteMotion.Progress(10.0, 11.2, 10.3, 0.6, NoteMotionMode.Stepped, 0.25f);
            Assert.LessOrEqual(early, 0.25f);
            // Tepat sebelum impact: ~1.
            float late = NoteMotion.Progress(10.0, 11.2, 11.19, 0.6, NoteMotionMode.Stepped, 0.25f);
            Assert.Greater(late, 0.9f);
        }

        [Test]
        public void Clamps_OutsideRange()
        {
            Assert.AreEqual(0f, NoteMotion.Progress(10.0, 12.0, 9.0, 0.6, NoteMotionMode.Linear, 0.25f));
            Assert.AreEqual(1f, NoteMotion.Progress(10.0, 12.0, 13.0, 0.6, NoteMotionMode.Linear, 0.25f));
            Assert.AreEqual(1f, NoteMotion.Progress(12.0, 12.0, 12.0, 0.6, NoteMotionMode.Linear, 0.25f));
        }

        [Test]
        public void Stepped_SkippedBeat_JumpsTwoStepsAtOnce()
        {
            // Travel 4 beat @100BPM (0.6s/beat): 1 beat = 1 step = 0.25.
            // Posisi murni fungsi DSP: loncat dari spawn langsung +2 beat
            // harus sama dengan 2 step, tanpa perlu melihat beat tengah.
            double spawn = 10.0;
            double beatLen = 0.6;
            double impact = spawn + 4 * beatLen;
            float jumped = NoteMotion.Progress(spawn, impact, spawn + 2 * beatLen, beatLen, NoteMotionMode.Stepped, 0.25f);
            Assert.AreEqual(0.5f, jumped, 1e-6);
            float one = NoteMotion.Progress(spawn, impact, spawn + beatLen, beatLen, NoteMotionMode.Stepped, 0.25f);
            Assert.AreEqual(0.25f, one, 1e-6);
            Assert.AreEqual(2 * one, jumped, 1e-6);
        }
    }
}
