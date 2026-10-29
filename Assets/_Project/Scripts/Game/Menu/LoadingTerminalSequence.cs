using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AlgoRhythm.Game.Menu
{
    /// <summary>
    /// Retro Terminal BIOS Boot & Emergency Recovery Screen.
    /// Features animated typewriter system logs, retro loading progress bar,
    /// CRT scanlines aesthetic, and stepped pixel fade into the target scene.
    /// </summary>
    public sealed class LoadingTerminalSequence : MonoBehaviour
    {
        [Header("Mode Configuration")]
        [SerializeField] private bool isEmergencyReboot = false;
        [SerializeField] private string nextSceneName = "SampleScene";

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private TextMeshProUGUI progressBarText;
        [SerializeField] private Image fadeOverlay;

        [Header("Timing")]
        [SerializeField] private float duration = 3.5f;

        private bool _skipped;
        private readonly List<string> _lines = new List<string>();

        private static readonly string[] NormalBootLines = new[]
        {
            "ALGORHYTHM KERNEL v4.19 [x86_64] — INITIALIZING SYSTEM",
            "> CHECKING MEMORY BUS (64MB RAM) ............ [OK]",
            "> MOUNTING DSP AUDIO CLOCK SUBSYSTEM ........ [OK]",
            "> SYNCHRONIZING REAL-TIME THREAT RADAR ...... [OK]",
            "> COMPILING 3-SLOT DEFENSE RULE ENGINE ...... [OK]",
            "> CONNECTING OPERATOR INTERFACE ............. [READY]",
            "SYSTEM STATUS: ONLINE. LAUNCHING DEFENSE PROTOCOL..."
        };

        private static readonly string[] EmergencyRebootLines = new[]
        {
            "!! EMERGENCY REBOOT DETECTED !! POWER RESTORED",
            "> WARNING: CORE INTEGRITY OFFLINE",
            "> ERROR: A.I.D.A THREAD DISCONNECTED (HOSTILE TAKEOVER)",
            "> TRACING DATA EXFILTRATION: PORT 8088 -> DARK WEB",
            "> DEPLOYING COUNTER-MEASURE SHELL REFLECTOR... [OK]",
            "> ENGAGING MANUAL EMERGENCY PROTOCOL......... [ARMED]",
            "STATUS: EMERGENCY BOSS FIGHT INITIATED. RECOVER THE CORE!"
        };

        private void Start()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.color = Color.black;
                fadeOverlay.gameObject.SetActive(true);
            }

            StartCoroutine(RunSequence());
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) ||
                (touch != null && touch.primaryTouch.press.wasPressedThisFrame))
            {
                _skipped = true;
            }
        }

        private IEnumerator RunSequence()
        {
            // Fade in from black
            if (fadeOverlay != null)
            {
                for (float t = 1f; t >= 0f; t -= Time.unscaledDeltaTime * 2.5f)
                {
                    fadeOverlay.color = new Color(0f, 0f, 0f, t);
                    yield return null;
                }
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            }

            string[] activeLines = isEmergencyReboot ? EmergencyRebootLines : NormalBootLines;
            _lines.Clear();

            float elapsed = 0f;
            int lineIdx = 0;

            while (elapsed < duration && !_skipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Add lines progressively
                int targetLines = Mathf.FloorToInt(progress * activeLines.Length);
                while (lineIdx < targetLines && lineIdx < activeLines.Length)
                {
                    _lines.Add(activeLines[lineIdx]);
                    lineIdx++;
                    if (logText != null)
                    {
                        logText.text = string.Join("\n", _lines);
                    }
                }

                // Update animated progress bar
                if (progressBarText != null)
                {
                    int pct = Mathf.RoundToInt(progress * 100f);
                    int barLength = 24;
                    int filled = Mathf.RoundToInt((progress) * barLength);
                    string bar = new string('█', filled) + new string('░', barLength - filled);
                    string label = isEmergencyReboot ? "REBOOTING RECOVERY PROTOCOL" : "BOOTING ALGORHYTHM OS";
                    progressBarText.text = $"{label}\n[{bar}] {pct}%";
                }

                yield return null;
            }

            // Render all lines if finished or skipped
            if (logText != null)
            {
                logText.text = string.Join("\n", activeLines);
            }

            if (progressBarText != null)
            {
                progressBarText.text = (isEmergencyReboot ? "REBOOTING RECOVERY PROTOCOL" : "BOOTING ALGORHYTHM OS") + "\n[████████████████████████] 100% [COMPLETED]";
            }

            yield return new WaitForSecondsRealtime(0.4f);

            // Fade out to black
            if (fadeOverlay != null)
            {
                for (float t = 0f; t <= 1f; t += Time.unscaledDeltaTime * 3f)
                {
                    fadeOverlay.color = new Color(0f, 0f, 0f, t);
                    yield return null;
                }
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
