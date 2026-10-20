using System.Collections;
using AlgoRhythm.Game.Bootstrap;
using AlgoRhythm.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AlgoRhythm.Tests.PlayMode
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator SampleScene_BootstrapsManualGreyboxWithoutUnexpectedErrors()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            Assert.IsNotNull(bootstrap, "GameBootstrap was not authored in SampleScene.");
            Assert.IsNotNull(Object.FindFirstObjectByType<HudView>(), "HUD was not authored in SampleScene.");
            Assert.IsNotNull(Object.FindFirstObjectByType<ArenaPresenter>(), "Arena was not authored in SampleScene.");
            Assert.IsNotNull(Object.FindFirstObjectByType<MetronomePlayer>(), "Metronome was not authored in SampleScene.");

            // First threat spawns around 3.6 s after startup at 100 BPM;
            // wait through its impact so the smoke covers spawn and timeout.
            yield return new WaitForSecondsRealtime(6.0f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
