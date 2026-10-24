using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Emergency Terminal Reboot & Loading Screen before Boss Fight.
    /// Runs a retro CRT computer boot sequence after the power outage cutscene:
    /// Typewriter emergency recovery logs, animated ASCII progress bar, and stepped fade
    /// into the boss fight arena. Allows quick skip with any key.
    /// </summary>
    public sealed class BossBootSequence : MonoBehaviour
    {
        public event Action BootCompleted;

        [Header("UI References")]
        [SerializeField] private TextMesh logTextMesh;
        [SerializeField] private TextMesh progressBarMesh;
        [SerializeField] private Renderer fadeOverlayRenderer;

        [Header("Configuration")]
        [SerializeField] private float duration = 3.2f;

        private bool _skipped;
        private bool _completed;
        private bool _awaitingConfirm;
        private readonly List<string> _visibleLines = new List<string>();

        private static readonly string[] EmergencyBootLines = new[]
        {
            "ALGORHYTHM/OS v1.0: EMERGENCY REBOOT DETECTED",
            "> POWER SOURCE: AUXILIARY GENERATOR RESTORED",
            "> WARNING: CORE DATABASE OFFLINE",
            "> HOSTILE EVENT: A.I.D.A THREAD DISCONNECTED (HIJACKED)",
            "> EXFILTRATION DETECTED: PORT 8088 -> DARK WEB",
            "> DEPLOYING COUNTER-MEASURE DEFLECTOR PADDLE... [READY]",
            "> STATUS: OVERRIDE MANUAL PROTOCOL ARMED",
            "INITIALIZING COMBAT SUBSYSTEM... RECOVER THE CORE!"
        };

        private void Start()
        {
            StartCoroutine(RunBootSequence());
        }

        private void Update()
        {
            if (_completed || _awaitingConfirm) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)))
            {
                _skipped = true;
            }
        }

        private IEnumerator RunBootSequence()
        {
            _visibleLines.Clear();
            float elapsed = 0f;
            int lineIdx = 0;

            while (elapsed < duration && !_skipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Add log lines progressively
                int targetLines = Mathf.FloorToInt(progress * EmergencyBootLines.Length);
                while (lineIdx < targetLines && lineIdx < EmergencyBootLines.Length)
                {
                    _visibleLines.Add(EmergencyBootLines[lineIdx]);
                    lineIdx++;
                    if (logTextMesh != null)
                    {
                        logTextMesh.text = string.Join("\n", _visibleLines);
                    }
                }

                // Update animated progress bar
                if (progressBarMesh != null)
                {
                    int pct = Mathf.RoundToInt(progress * 100f);
                    int barLength = 24;
                    int filled = Mathf.RoundToInt(progress * barLength);
                    string bar = new string('█', filled) + new string('░', barLength - filled);
                    progressBarMesh.text = $"REBOOTING RECOVERY PROTOCOL\n[{bar}] {pct}%";
                }

                yield return null;
            }

            // Show all lines
            if (logTextMesh != null)
            {
                logTextMesh.text =
                    "TUTORIAL BOSS FIGHT:\n\n" +
                    "\"Masukan Bola Malware Ke Gawang Aida Untuk Hentikan Aida\n" +
                    "Dari Menjual Data Set kita, ayo kita hancurkan!\"\n\n" +
                    "[TEKAN ENTER UNTUK MULAI]";
                logTextMesh.color = new Color(0f, 1f, 0.85f);
            }

            if (progressBarMesh != null)
            {
                progressBarMesh.text = "STATUS: SIAP TEMPUR · PROTOKOL OVERRIDE AKTIF";
                progressBarMesh.color = new Color(0f, 1f, 0.5f);
            }

            // Wait for player to press Enter/Space/Click
            _awaitingConfirm = true;
            bool confirmed = false;
            while (!confirmed)
            {
                var kb = Keyboard.current;
                var ms = Mouse.current;
                if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) ||
                    (ms != null && ms.leftButton.wasPressedThisFrame))
                {
                    confirmed = true;
                }
                yield return null;
            }
            _awaitingConfirm = false;

            yield return new WaitForSecondsRealtime(0.1f);

            // Stepped fade out to reveal arena
            if (fadeOverlayRenderer != null && fadeOverlayRenderer.material != null)
            {
                Color c = fadeOverlayRenderer.material.color;
                for (int step = 6; step >= 0; step--)
                {
                    c.a = step / 6f;
                    fadeOverlayRenderer.material.color = c;
                    yield return new WaitForSecondsRealtime(0.06f);
                }
            }

            gameObject.SetActive(false);
            _completed = true;
            BootCompleted?.Invoke();
        }
    }
}
