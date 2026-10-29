using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AlgoRhythm.Editor
{
    /// <summary>
    /// Automated build script for Android (APK and AAB).
    /// Callable from Unity Editor Menu or Command Line / Batchmode:
    /// Unity -batchmode -quit -projectPath . -executeMethod AlgoRhythm.Editor.AndroidBuildScript.BuildApk
    /// </summary>
    public static class AndroidBuildScript
    {
        private const string OutputDirectory = "Builds/Android";
        private const string ApkFileName = "AlgoRhythm2D.apk";
        private const string AabFileName = "AlgoRhythm2D.aab";

        [MenuItem("AlgoRhythm/Build/Android APK (Release)")]
        public static void BuildReleaseApk()
        {
            BuildAndroid(buildAppBundle: false, development: false);
        }

        [MenuItem("AlgoRhythm/Build/Android APK (Debug/Device)")]
        public static void BuildApk()
        {
            BuildAndroid(buildAppBundle: false, development: false);
        }

        [MenuItem("AlgoRhythm/Build/Android AAB (Google Play)")]
        public static void BuildAab()
        {
            BuildAndroid(buildAppBundle: true, development: false);
        }

        [MenuItem("AlgoRhythm/Build/Windows 64-bit Player")]
        public static void BuildWindowsPlayer()
        {
            BuildWindows(development: false);
        }

        [MenuItem("AlgoRhythm/Build/Windows 64-bit Player (Development)")]
        public static void BuildWindowsDevPlayer()
        {
            BuildWindows(development: true);
        }

        public static void BuildWindows(bool development = false)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("BuildScript: No enabled scenes found in EditorBuildSettings!");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            string outputDir = Path.GetFullPath("Builds/Windows");
            const string exeName = "AlgoRhythm2D.exe";
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, exeName);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = (development ? BuildOptions.Development : BuildOptions.None)
            };

            Debug.Log($"[BuildScript] Starting Windows build to: {outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildScript] summary.outputPath: '{summary.outputPath}', files: {report.GetFiles().Length}, totalSize: {summary.totalSize}");
            for (int i = 0; i < Mathf.Min(5, report.GetFiles().Length); i++)
            {
                Debug.Log($"[BuildScript] File {i}: {report.GetFiles()[i].path} ({report.GetFiles()[i].role})");
            }

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Windows Build SUCCESS! Output: {outputPath} ({summary.totalSize / (1024 * 1024):F1} MB)");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildScript] Windows Build FAILED! Total errors: {summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroid(bool buildAppBundle, bool development = false)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("AndroidBuildScript: No enabled scenes found in EditorBuildSettings!");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(OutputDirectory);
            string fileName = buildAppBundle ? AabFileName : ApkFileName;
            string outputPath = Path.Combine(OutputDirectory, fileName);
            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }

            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // Ensure Android target settings
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = (development ? BuildOptions.Development : BuildOptions.None) | BuildOptions.CleanBuildCache
            };

            Debug.Log($"[AndroidBuildScript] Starting build to: {outputPath} (AppBundle: {buildAppBundle})");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuildScript] Build SUCCESS! Output: {outputPath} ({summary.totalSize / (1024 * 1024):F1} MB)");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[AndroidBuildScript] Build FAILED! Total errors: {summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
