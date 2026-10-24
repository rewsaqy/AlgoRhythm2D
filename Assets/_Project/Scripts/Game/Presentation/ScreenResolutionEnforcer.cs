using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Enforces standard 1920x1080 (16:9) screen resolution lock.
    /// Prevents UI clipping or stretching on 4K / Ultrawide displays.
    /// </summary>
    public static class ScreenResolutionEnforcer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Enforce1080p()
        {
            // Maintain 1920x1080 resolution
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        }
    }
}
