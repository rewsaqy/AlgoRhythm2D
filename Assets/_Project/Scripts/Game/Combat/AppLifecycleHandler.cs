using System;
using UnityEngine;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Handles mobile application lifecycle events (app minimizing, phone calls, backgrounding).
    /// Ensures DSP time and gameplay immediately pause so the player is not penalized while away.
    /// </summary>
    public sealed class AppLifecycleHandler : MonoBehaviour
    {
        private CombatController _combatController;

        public void Initialize(CombatController combatController)
        {
            _combatController = combatController ?? throw new ArgumentNullException(nameof(combatController));
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TriggerPause();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Handled primarily via OnApplicationPause to avoid accidental pauses on Samsung One UI system popups
        }

        private void TriggerPause()
        {
            // Ignore during early startup initialization (first 3 seconds) when Android window focus fluctuates
            if (Time.realtimeSinceStartup < 3.0f)
            {
                return;
            }

            if (_combatController != null && !_combatController.IsPaused)
            {
                _combatController.Pause();
            }
        }
    }
}
