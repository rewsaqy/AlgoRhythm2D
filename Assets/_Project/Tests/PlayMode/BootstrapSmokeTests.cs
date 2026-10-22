using System.Collections;
using AlgoRhythm.Game.Bootstrap;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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

            // Beatblock input binding: Aim + TapZ/TapX/TapClick + Pause harus terikat.
            InputRouter router = Object.FindFirstObjectByType<InputRouter>();
            Assert.IsNotNull(router, "InputRouter was not authored in SampleScene.");
            Assert.IsTrue(router.IsBound, "Rhythm map Aim/Tap actions are not bound.");
            InputActionMap rhythm = InputSystem.actions?.FindActionMap("Rhythm");
            Assert.IsNotNull(rhythm, "Input action map 'Rhythm' is missing.");
            Assert.IsNotNull(rhythm.FindAction("Aim", throwIfNotFound: false), "Rhythm action 'Aim' is missing.");
            Assert.IsNotNull(rhythm.FindAction("TapZ", throwIfNotFound: false), "Rhythm action 'TapZ' is missing.");
            Assert.IsNotNull(rhythm.FindAction("TapX", throwIfNotFound: false), "Rhythm action 'TapX' is missing.");
            Assert.IsNotNull(rhythm.FindAction("TapClick", throwIfNotFound: false), "Rhythm action 'TapClick' is missing.");

            // First threat spawns around 3.6 s after startup at 100 BPM;
            // wait through its impact so the smoke covers spawn and timeout.
            yield return new WaitForSecondsRealtime(6.0f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
