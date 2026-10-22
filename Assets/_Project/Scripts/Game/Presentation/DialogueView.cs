using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>One RPG dialogue line: who speaks, what is said.</summary>
    [Serializable]
    public struct DialogueLine
    {
        public string Speaker;
        public string Text;

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// RPG-style dialogue box with a typewriter effect. ENTER/SPACE shows the
    /// full line first, then advances; the Finished event fires after the
    /// last line is confirmed. Pure presentation: the TutorialDriver owns
    /// flow, this view only types and reports. Runs on unscaled time so it
    /// works before the beat clock starts and while paused.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour
    {
        public event Action Finished;

        [Header("Typewriter [U]")]
        [Min(1f)] [SerializeField] private float charsPerSecond = 45f;

        [Header("Advance keys [U]")]
        [SerializeField] private Key advanceKeyA = Key.Enter;
        [SerializeField] private Key advanceKeyB = Key.Space;

        private TextMesh _label;
        private readonly List<DialogueLine> _script = new List<DialogueLine>();
        private int _index;
        private float _shown;
        private bool _playing;
        private bool _autoHideAtEnd = true;
        private float _autoHideAt = -1f;
        private float _autoHideDelay;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            // NOTE: GameBootstrap.Awake (root) runs before this Awake, and
            // TutorialDriver.Begin can fire before it too — so every public
            // entry re-resolves the label lazily via EnsureLabel().
            EnsureLabel();
            UiTypography.Style(_label, 30, 0.075f, UiTypography.Ink,
                TextAnchor.LowerCenter, TextAlignment.Center, 1.25f);
        }

        /// <summary>Finds the Controls label on demand; the scene bake may
        /// still hold stale text, which the first Render overwrites.</summary>
        private bool EnsureLabel()
        {
            if (_label == null)
            {
                _label = GetComponent<TextMesh>();
            }

            if (_label == null)
            {
                GameObject controls = GameObject.Find("AlgoRhythm/HUD/Controls");
                if (controls != null)
                {
                    _label = controls.GetComponent<TextMesh>();
                }
            }

            if (_label == null)
            {
                Debug.LogError("DialogueView: Controls label not found; dialogue is silent.", this);
                return false;
            }

            return true;
        }

        /// <summary>Plays a manual script; confirm each line to continue.</summary>
        public void Play(IReadOnlyList<DialogueLine> script)
        {
            if (!EnsureLabel())
            {
                return;
            }

            _script.Clear();
            if (script != null)
            {
                _script.AddRange(script);
            }

            _index = 0;
            _shown = 0f;
            _playing = _script.Count > 0;
            _autoHideAtEnd = false;
            gameObject.SetActive(true);
            Render();
        }

        /// <summary>One-liner that dismisses itself after a few seconds.</summary>
        public void ShowLine(string speaker, string text, float hideAfterSeconds = 4f)
        {
            if (!EnsureLabel())
            {
                return;
            }

            _script.Clear();
            _script.Add(new DialogueLine(speaker, text));
            _index = 0;
            _shown = 0f;
            _playing = true;
            _autoHideAtEnd = true;
            _autoHideDelay = Mathf.Max(0.5f, hideAfterSeconds);
            _autoHideAt = -1f;
            gameObject.SetActive(true);
            Render();
        }

        public void Stop()
        {
            _playing = false;
            if (_label != null)
            {
                _label.text = string.Empty;
            }
        }

        /// <summary>
        /// Persistent hint with no advance handling; the driver owns flow.
        /// Replaced by the next Play/ShowLine call.
        /// </summary>
        public void ShowStatic(string text)
        {
            if (!EnsureLabel())
            {
                return;
            }

            _playing = false;
            _autoHideAt = -1f;
            _script.Clear();
            _script.Add(new DialogueLine(string.Empty, text ?? string.Empty));
            _index = 0;
            _shown = (text ?? string.Empty).Length;
            gameObject.SetActive(true);
            RenderStatic();
        }

        private void RenderStatic()
        {
            if (_label == null || _script.Count == 0)
            {
                return;
            }

            _label.text = _script[0].Text;
        }

        private void Update()
        {
            if (!_playing || _label == null)
            {
                return;
            }

            string full = CurrentText();
            if (_shown < full.Length)
            {
                _shown = Math.Min(full.Length, _shown + charsPerSecond * Time.unscaledDeltaTime);
                Render();
            }
            else if (_autoHideAtEnd)
            {
                if (_autoHideAt < 0f)
                {
                    _autoHideAt = Time.unscaledTime + _autoHideDelay;
                }
                else if (Time.unscaledTime >= _autoHideAt)
                {
                    Stop();
                    return;
                }
            }
            else
            {
                // Full line shown, waiting for confirm: refresh the blink.
                Render();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[advanceKeyA].wasPressedThisFrame
                || keyboard[advanceKeyB].wasPressedThisFrame)
            {
                Advance();
            }
        }

        private void Advance()
        {
            string full = CurrentText();
            if (_shown < full.Length)
            {
                _shown = full.Length;
                Render();
                return;
            }

            if (_autoHideAtEnd)
            {
                Stop();
                Finished?.Invoke();
                return;
            }

            _index++;
            if (_index >= _script.Count)
            {
                _playing = false;
                Finished?.Invoke();
                return;
            }

            _shown = 0f;
            Render();
        }

        private string CurrentText()
        {
            if (_index < 0 || _index >= _script.Count)
            {
                return string.Empty;
            }

            return _script[_index].Text ?? string.Empty;
        }

        private void Render()
        {
            if (_label == null || _index < 0 || _index >= _script.Count)
            {
                return;
            }

            DialogueLine line = _script[_index];
            int count = Mathf.Clamp(Mathf.FloorToInt(_shown), 0, line.Text.Length);
            string partial = line.Text.Substring(0, count);
            // Blinking advance hint so players never stare at a still screen.
            string hint = (Time.unscaledTime % 1f) < 0.6f ? "\n<color=#9fb2ccff>[ENTER]</color>" : string.Empty;
            string speaker = string.IsNullOrEmpty(line.Speaker) ? string.Empty : line.Speaker + " ▸ ";
            _label.text = $"{speaker}{partial}{hint}";
        }
    }
}
