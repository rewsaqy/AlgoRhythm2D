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
            int nativeHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            if (nativeHz < 50)
            {
                nativeHz = 60;
            }

            if (Application.isMobilePlatform)
            {
                QualitySettings.antiAliasing = 0;
                // vSyncCount = 0 allows Android Choreographer & SurfaceFlinger to pace frames at targetFrameRate
                // without the 3:2 pull-down stutter caused by 60 FPS on 90Hz/120Hz displays.
                QualitySettings.vSyncCount = 0;
                QualitySettings.maxQueuedFrames = 2;
                QualitySettings.shadows = ShadowQuality.Disable;
                Application.targetFrameRate = Mathf.Clamp(nativeHz, 60, 120);
            }
            else
            {
                Application.targetFrameRate = Mathf.Clamp(nativeHz, 60, 240);
                QualitySettings.vSyncCount = 0;
                QualitySettings.maxQueuedFrames = 2;
            }
        }

        /// <summary>Target refresh rate detected from hardware display.</summary>
        public static int DetectedRefreshRate => Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);

        /// <summary>Configures runtime target FPS (e.g. 60 battery saver, 90, or 120).</summary>
        public static void SetTargetFps(int fps)
        {
            Application.targetFrameRate = Mathf.Clamp(fps, 30, 240);
        }
    }
}
