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

        [Header("Layout [U]")]
        [Min(8)] [SerializeField] private int maxCharsPerLine = 56;
        [Min(1)] [SerializeField] private int maxLines = 5;

        [Header("Advance prompt [U]")]
        [SerializeField] private TextMesh advancePrompt;
        [SerializeField] private string advancePromptText = "[ENTER]";
        [Min(8)] [SerializeField] private int promptFontSize = 24;
        [Min(0.01f)] [SerializeField] private float promptCharacterSize = 0.07f;

        [Header("Audio SFX [U]")]
        [SerializeField] private AudioClip typewriterClip;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.22f;
        [SerializeField] private float sfxInterval = 0.055f;

        private AudioSource _audioSource;
        private float _lastSfxTime;
        private int _lastCharCount;

        private TextMesh _label;
        private readonly List<DialogueLine> _script = new List<DialogueLine>();
        private int _index;
        private string _wrappedBody = string.Empty;
        private float _shown;
        private bool _playing;
        private bool _autoHideAtEnd = true;
        private float _autoHideAt = -1f;
        private float _autoHideDelay;

        public bool IsPlaying => _playing;

        public int MaxLines
        {
            get => maxLines;
            set => maxLines = Mathf.Max(1, value);
        }

        public int MaxCharsPerLine
        {
            get => maxCharsPerLine;
            set => maxCharsPerLine = Mathf.Max(8, value);
        }

        private Vector3 _originalLocalPosition;
        private TextAnchor _originalAnchor;
        private TextAlignment _originalAlignment;
        private float _originalCharSize;
        private int _originalFontSize;
        private Color _originalColor;
        private bool _hasSavedStyle;

        /// <summary>
        /// Centers the dialogue and sets luminous CRT phosphor styling for outro.
        /// </summary>
        public void ConfigureOutroStyle()
        {
            if (!EnsureLabel())
            {
                return;
            }

            if (!_hasSavedStyle)
            {
                _originalLocalPosition = _label.transform.localPosition;
                _originalAnchor = _label.anchor;
                _originalAlignment = _label.alignment;
                _originalCharSize = _label.characterSize;
                _originalFontSize = _label.fontSize;
                _originalColor = _label.color;
                _hasSavedStyle = true;
            }

            maxLines = 18;
            maxCharsPerLine = 64;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.characterSize = 0.065f;
            _label.fontSize = 28;
            _label.lineSpacing = 1.15f;
            _label.color = UiTypography.Info;
            _label.transform.localPosition = new Vector3(8.288889f, -4.4f, -0.2f);

            if (advancePrompt != null)
            {
                advancePrompt.transform.localPosition = new Vector3(8.288889f, -5.8f, 0f);
                advancePrompt.color = UiTypography.Good;
            }
        }

        /// <summary>Restores default gameplay styling and positioning.</summary>
        public void RestoreDefaultStyle()
        {
            if (_hasSavedStyle && _label != null)
            {
                _label.transform.localPosition = _originalLocalPosition;
                _label.anchor = _originalAnchor;
                _label.alignment = _originalAlignment;
                _label.characterSize = _originalCharSize;
                _label.fontSize = _originalFontSize;
                _label.color = _originalColor;
                maxLines = 5;
                maxCharsPerLine = 56;
                _hasSavedStyle = false;
            }
        }

        private void Awake()
        {
            // NOTE: GameBootstrap.Awake (root) runs before this Awake, and
            // TutorialDriver.Begin can fire before it too — so every public
            // entry re-resolves the label lazily via EnsureLabel().
            EnsureLabel();
            UiTypography.Style(_label, 28, 0.07f, UiTypography.Ink,
                TextAnchor.LowerCenter, TextAlignment.Center, 1.2f);
            EnsurePrompt();
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

        /// <summary>Finds the dedicated advance-prompt label on demand.
        /// The prompt lives in its own scene object so it never shifts
        /// the body layout; when absent the body still works.</summary>
        private bool EnsurePrompt()
        {
            if (advancePrompt == null)
            {
                GameObject prompt = GameObject.Find("AlgoRhythm/HUD/AdvancePrompt");
                if (prompt != null)
                {
                    advancePrompt = prompt.GetComponent<TextMesh>();
                }
            }

            if (advancePrompt == null)
            {
                return false;
            }

            UiTypography.Style(advancePrompt, promptFontSize, promptCharacterSize,
                UiTypography.Ink, TextAnchor.UpperCenter, TextAlignment.Center, 1.1f);

            // A freshly created TextMesh may carry no font material, so a
            // runtime font swap alone leaves the renderer sampling the wrong
            // texture and the prompt stays invisible. Sync the material.
            var meshRenderer = advancePrompt.GetComponent<MeshRenderer>();
            if (meshRenderer != null && advancePrompt.font != null)
            {
                meshRenderer.sharedMaterial = advancePrompt.font.material;
            }

            return true;
        }

        private static string SpeakerPrefix(DialogueLine line)
        {
            return string.IsNullOrEmpty(line.Speaker) ? string.Empty : line.Speaker + " : ";
        }

        private void RewrapCurrent()
        {
            if (_index < 0 || _index >= _script.Count)
            {
                _wrappedBody = string.Empty;
                return;
            }

            DialogueLine line = _script[_index];
            _wrappedBody = DialogueLayout.WrapBody(
                SpeakerPrefix(line), line.Text ?? string.Empty,
                maxCharsPerLine, maxLines);
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
            RewrapCurrent();
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
            RewrapCurrent();
            gameObject.SetActive(true);
            Render();
        }

        public void Stop()
        {
            _playing = false;
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            if (_label != null)
            {
                _label.text = string.Empty;
            }

            SetPromptVisible(false);
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
            RewrapCurrent();
            _shown = _wrappedBody.Length;
            SetPromptVisible(false);
            gameObject.SetActive(true);
            RenderStatic();
        }

        /// <summary>
        /// Displays pre-formatted raw text directly without word-wrap splitting.
        /// Preserves exact indentation, ASCII box drawing, and line breaks.
        /// </summary>
        public void ShowRaw(string text)
        {
            if (!EnsureLabel())
            {
                return;
            }

            _playing = false;
            _autoHideAt = -1f;
            _script.Clear();
            _wrappedBody = text ?? string.Empty;
            _shown = _wrappedBody.Length;
            SetPromptVisible(false);
            gameObject.SetActive(true);
            _label.text = _wrappedBody;
        }

        private void RenderStatic()
        {
            if (_label == null || _script.Count == 0)
            {
                return;
            }

            _label.text = _wrappedBody;
        }

        private void Update()
        {
            if (!_playing || _label == null)
            {
                return;
            }

            if (_shown < _wrappedBody.Length)
            {
                _shown = Math.Min(_wrappedBody.Length, _shown + charsPerSecond * Time.unscaledDeltaTime);
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

            bool touchPressed = false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                touchPressed = true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                touchPressed = true;
            }

            Keyboard keyboard = Keyboard.current;
            bool keyPressed = keyboard != null && (keyboard[advanceKeyA].wasPressedThisFrame
                || keyboard[advanceKeyB].wasPressedThisFrame);

            if (touchPressed || keyPressed)
            {
                Advance();
            }
        }

        /// <summary>Public method to advance dialogue from touch UI buttons.</summary>
        public void AdvanceDialogue()
        {
            Advance();
        }

        private void Advance()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            if (_shown < _wrappedBody.Length)
            {
                _shown = _wrappedBody.Length;
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
                SetPromptVisible(false);
                Finished?.Invoke();
                return;
            }

            _shown = 0f;
            RewrapCurrent();
            Render();
        }

        private void SetPromptVisible(bool visible)
        {
            if (!EnsurePrompt())
            {
                return;
            }

            if (!visible)
            {
                if (!string.IsNullOrEmpty(advancePrompt.text))
                {
                    advancePrompt.text = string.Empty;
                }

                return;
            }

            string blink = (Time.unscaledTime % 1f) < 0.6f ? advancePromptText : string.Empty;
            if (advancePrompt.text != blink)
            {
                advancePrompt.text = blink;
            }
        }

        private void Render()
        {
            if (_label == null || _index < 0 || _index >= _script.Count)
            {
                return;
            }

            int count = Mathf.Clamp(Mathf.FloorToInt(_shown), 0, _wrappedBody.Length);
            if (count > _lastCharCount && count < _wrappedBody.Length)
            {
                PlaySpeakSound(count);
                _lastCharCount = count;
            }
            else if (count >= _wrappedBody.Length)
            {
                // Kalimat selesai terketik: hentikan audio seketika
                if (_audioSource != null && _audioSource.isPlaying)
                {
                    _audioSource.Stop();
                }
            }

            string partial = _wrappedBody.Substring(0, count);
            string speaker = SpeakerPrefix(_script[_index]);
            _label.text = $"{speaker}{partial}";
            SetPromptVisible(count >= _wrappedBody.Length && !_autoHideAtEnd && _playing);
        }

        private void PlaySpeakSound(int charIndex)
        {
            if (typewriterClip == null)
            {
                return;
            }

            if (charIndex > 0 && charIndex <= _wrappedBody.Length)
            {
                char c = _wrappedBody[charIndex - 1];
                if (char.IsWhiteSpace(c) || c == '.' || c == ',' || c == '!' || c == '?')
                {
                    return;
                }
            }

            if (Time.unscaledTime - _lastSfxTime < sfxInterval)
            {
                return;
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f;
            }

            _audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            _audioSource.PlayOneShot(typewriterClip, sfxVolume);
            _lastSfxTime = Time.unscaledTime;
        }
    }
}
