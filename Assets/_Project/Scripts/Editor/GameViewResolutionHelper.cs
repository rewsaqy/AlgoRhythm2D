using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AlgoRhythm.Editor
{
    /// <summary>
    /// Editor utility to enforce 1920x1080 (16:9) Game View resolution for Unity Remote 5.
    /// Also attaches MobileCameraFramer to active camera in Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewResolutionHelper
    {
        static GameViewResolutionHelper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Force1080pGameView();
                AlgoRhythm.Game.Presentation.MobileCameraFramer.AttachToCamera();
            }
        }

        [MenuItem("AlgoRhythm/Display/Force 1920x1080 GameView (16:9)")]
        public static void Force1080pGameView()
        {
            try
            {
                var gvWndType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                var gvWnd = EditorWindow.GetWindow(gvWndType);
                if (gvWnd == null) return;

                var sizeSelectionCallback = gvWndType.GetMethod(
                    "SizeSelectionCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                // In standard Unity Android editor profile, index 6 is 1920x1080 Landscape
                if (sizeSelectionCallback != null)
                {
                    sizeSelectionCallback.Invoke(gvWnd, new object[] { 6, null });
                    Debug.Log("[GameViewResolutionHelper] GameView resolution forced to 1920x1080 Landscape (index 6).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameViewResolutionHelper] Could not set GameView size via reflection: {ex.Message}");
            }
        }
    }
}
