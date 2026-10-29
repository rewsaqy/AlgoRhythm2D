using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Forces standard 1920x1080 (16:9) resolution and orientation across all platforms.
    /// Prevents display stretching, UI clipping, and keeps rhythm visuals locked to 16:9.
    /// </summary>
    public static class ScreenResolutionEnforcer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Enforce1080p()
        {
            // Force 1920x1080 resolution
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

            // Keep screen awake during gameplay
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Lock orientation strictly to Landscape
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;

            // Frame pacing and performance optimization
            if (Application.isMobilePlatform)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 1;
                QualitySettings.maxQueuedFrames = 2;
                QualitySettings.shadows = ShadowQuality.Disable;
                Application.targetFrameRate = 60;
            }
            else
            {
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
                QualitySettings.maxQueuedFrames = 2;
            }
        }
    }
}
