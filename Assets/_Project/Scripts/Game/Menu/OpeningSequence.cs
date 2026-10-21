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
    /// Opening_1 retro boot terminal. Code lines type themselves in, the
    /// finished log scrolls up terminal-style (top lines drop in steps),
    /// then a stepped pixel-read fade carries into the gameplay scene.
    /// Any key skips straight to the fade. If references are missing the
    /// scene still forwards instead of soft-locking.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI codeText;
        [SerializeField] private Image fade;
        [SerializeField] private string nextScene = "SampleScene";

        [Header("Timing [U]")]
        [Min(0f)] [SerializeField] private float startDelay = 0.5f;
        [Min(0.005f)] [SerializeField] private float secondsPerCharacter = 0.03f;
        [Min(0f)] [SerializeField] private float doneHoldSeconds = 1.2f;
        [Min(0.05f)] [SerializeField] private float scrollStepSeconds = 0.12f;
        [Min(1)] [SerializeField] private int keepLastLines = 4;
        [Min(2)] [SerializeField] private int fadeSteps = 8;
        [Min(0.02f)] [SerializeField] private float fadeStepSeconds = 0.1f;

        [Header("Boot log [U] — generic terminal flavor")]
        [SerializeField]
        private string[] bootLines =
        {
            "ALGORHYTHM/OS v1.0 -- cold boot",
            "> mount /core/database ............ OK",
            "> load shield.sys ................ OK",
            "> lock timing.dll [dsp] .......... OK",
            "> scan sectors [1..5] ............ 1 ONLINE",
            "> trace intruder ................. ADWARE x12",
            "> compile defense [3 slots] ...... READY",
            "> awaiting operator _",
        };

        private readonly List<string> _visible = new List<string>();
        private bool _skipped;
        private bool _leaving;

        private void Awake()
        {
            _visible.Clear();
            if (codeText != null)
            {
                codeText.text = string.Empty;
                codeText.maxVisibleCharacters = 9999;
            }

            if (fade != null)
            {
                Color c = fade.color;
                c.a = 0f;
                fade.color = c;
                fade.raycastTarget = false;
                fade.gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            StartCoroutine(PlayBoot());
        }

        private void Update()
        {
            if (_leaving)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                _skipped = true;
            }
        }

        private IEnumerator PlayBoot()
        {
            if (startDelay > 0f && !_skipped)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            if (codeText != null && bootLines != null)
            {
                foreach (string line in bootLines)
                {
                    if (_skipped)
                    {
                        break;
                    }

                    yield return TypeLine(line ?? string.Empty);
                }

                RenderAll();
            }

            if (!_skipped && doneHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(doneHoldSeconds);
            }

            // Terminal-style scroll: finished lines drop off the top in
            // discrete steps until only the tail remains.
            while (!_skipped && _visible.Count > keepLastLines)
            {
                _visible.RemoveAt(0);
                RenderAll();
                yield return new WaitForSecondsRealtime(scrollStepSeconds);
            }

            RenderAll();
            if (!_skipped)
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }

            yield return FadeOut();
            SceneManager.LoadScene(nextScene);
        }

        private IEnumerator TypeLine(string line)
        {
            _visible.Add(string.Empty);
            for (int i = 1; i <= line.Length && !_skipped; i++)
            {
                _visible[_visible.Count - 1] = line.Substring(0, i);
                RenderAll();
                yield return new WaitForSecondsRealtime(secondsPerCharacter);
            }

            if (!_skipped)
            {
                _visible[_visible.Count - 1] = line;
                RenderAll();
            }
        }

        private void RenderAll()
        {
            if (codeText == null)
            {
                return;
            }

            codeText.text = string.Join("\n", _visible.ToArray());
        }

        private IEnumerator FadeOut()
        {
            _leaving = true;
            if (fade != null)
            {
                for (int i = 1; i <= fadeSteps; i++)
                {
                    Color c = fade.color;
                    c.a = (float)i / fadeSteps;
                    fade.color = c;
                    yield return new WaitForSecondsRealtime(fadeStepSeconds);
                }
            }
        }
    }
}
