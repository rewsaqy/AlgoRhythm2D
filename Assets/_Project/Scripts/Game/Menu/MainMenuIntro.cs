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
    /// Retro opening for the main menu. A fullscreen wipe sweeps away in
    /// discrete steps (intentionally choppy, not smooth), the title types
    /// on character-by-character, then floats with a quantized sine bob.
    /// Runs on unscaled time; the menu owns no beat clock. Any key skips.
    /// If this component is missing, the scene simply shows the title.
    /// </summary>
    public sealed class MainMenuIntro : MonoBehaviour
    {
        [SerializeField] private RectTransform wipe;
        [SerializeField] private TextMeshProUGUI title;

        [Header("Wipe [U] — stepped sweep")]
        [Min(0.1f)] [SerializeField] private float wipeDuration = 2.6f;
        [Min(2)] [SerializeField] private int wipeSteps = 6;

        [Header("Title [U] — typewriter then float")]
        [Min(0f)] [SerializeField] private float revealStartDelay = 0.8f;
        [Min(0.01f)] [SerializeField] private float secondsPerCharacter = 0.14f;
        [SerializeField] private float floatAmplitudePx = 6f;
        [Min(0.1f)] [SerializeField] private float floatCyclesPerSecond = 0.5f;
        [Min(1)] [SerializeField] private int floatQuantizeSteps = 3;
        [SerializeField] private float labelStaggerDelay = 0.3f;

        [Header("Buttons [U] — stepped slide-in from the sides")]
        [Min(0.1f)] [SerializeField] private float slideDuration = 0.7f;
        [Min(2)] [SerializeField] private int slideSteps = 5;

        private readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>();
        private readonly List<Vector2> _labelBase = new List<Vector2>();
        private readonly List<Button> _gatedButtons = new List<Button>();
        private bool _skipped;

        private void Awake()
        {
            if (wipe != null)
            {
                // Cover the screen instantly; the sweep reveals from here.
                wipe.offsetMin = Vector2.zero;
                wipe.offsetMax = Vector2.zero;
                wipe.gameObject.SetActive(true);
            }

            _labels.Clear();
            _labelBase.Clear();
            _gatedButtons.Clear();
            if (title != null)
            {
                RegisterLabel(title, null);
            }

            // Button labels animate exactly like the title and unlock their
            // button only once fully typed. Clicks are hooked in code so the
            // scene needs no persistent UnityEvent wiring.
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TextMeshProUGUI label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && label != title)
                {
                    RegisterLabel(label, buttons[i]);
                    buttons[i].interactable = false;
                }

                if (buttons[i].name == "Play")
                {
                    buttons[i].onClick.AddListener(LoadGame);
                }
                else if (buttons[i].name == "Exit")
                {
                    buttons[i].onClick.AddListener(ExitGame);
                }
            }
        }

        private void Start()
        {
            StartCoroutine(PlayIntro());
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                _skipped = true;
            }
        }

        private IEnumerator PlayIntro()
        {
            // Phase 1: stepped wipe sweep, left edge travels off-screen right.
            if (wipe != null)
            {
                float width = Screen.width;
                float elapsed = 0f;
                while (elapsed < wipeDuration && !_skipped)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Min(1f, elapsed / wipeDuration);
                    // Quantize: N discrete jumps instead of a smooth slide.
                    float stepped = Mathf.Floor(t * wipeSteps) / wipeSteps;
                    float x = stepped * (width + 64f);
                    wipe.offsetMin = new Vector2(x, 0f);
                    wipe.offsetMax = new Vector2(x, 0f);
                    yield return null;
                }

                wipe.gameObject.SetActive(false);
            }

            // Phase 2: title types on, then buttons wipe-slide in from
            // alternating sides (first from the left, next from the right).
            if (_labels.Count > 0)
            {
                if (revealStartDelay > 0f && !_skipped)
                {
                    yield return new WaitForSecondsRealtime(revealStartDelay * 0.5f);
                }

                if (!_skipped)
                {
                    yield return TypeLabel(_labels[0]);
                    _labels[0].maxVisibleCharacters = 9999;
                }
            }

            for (int li = 1; li < _labels.Count && !_skipped; li++)
            {
                if (labelStaggerDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(labelStaggerDelay);
                    if (_skipped)
                    {
                        break;
                    }
                }

                _labels[li].maxVisibleCharacters = 9999;
                yield return SlideIn(li);
                SetLabelButton(li, true);
            }

            for (int i = 0; i < _labels.Count; i++)
            {
                _labels[i].maxVisibleCharacters = 9999;
            }

            for (int i = 0; i < _gatedButtons.Count; i++)
            {
                SetGatedButton(_gatedButtons[i], true);
            }

            // Phase 3: quantized float loop, fully synchronized — every line
            // rises and falls together (retro judder, not smooth bob).
            while (true)
            {
                float s = Mathf.Sin(Time.unscaledTime * floatCyclesPerSecond * Mathf.PI * 2f);
                float quantized = Mathf.Round(s * floatQuantizeSteps) / floatQuantizeSteps;
                Vector2 offset = new Vector2(0f, quantized * floatAmplitudePx);
                for (int i = 0; i < _labels.Count; i++)
                {
                    _labels[i].rectTransform.anchoredPosition = _labelBase[i] + offset;
                }

                yield return null;
            }
        }

        private void RegisterLabel(TextMeshProUGUI label, Button gate)
        {
            _labels.Add(label);
            _labelBase.Add(label.rectTransform.anchoredPosition);
            label.maxVisibleCharacters = 0;
            if (gate != null && !_gatedButtons.Contains(gate))
            {
                _gatedButtons.Add(gate);
            }
        }

        private IEnumerator TypeLabel(TextMeshProUGUI label)
        {
            int total = label.textInfo.characterCount;
            if (total <= 0)
            {
                label.ForceMeshUpdate();
                total = label.textInfo.characterCount;
            }

            int shown = 0;
            while (shown < total && !_skipped)
            {
                shown++;
                label.maxVisibleCharacters = shown;
                yield return new WaitForSecondsRealtime(secondsPerCharacter);
            }
        }

        private IEnumerator SlideIn(int labelIndex)
        {
            RectTransform rt = _labels[labelIndex].rectTransform;
            Vector2 home = _labelBase[labelIndex];
            // Alternate sides: first button from the left, next from right.
            float side = (labelIndex % 2 == 1) ? -1f : 1f;
            float offscreenX = side * (Screen.width * 0.5f + 256f);

            float elapsed = 0f;
            while (elapsed < slideDuration && !_skipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Min(1f, elapsed / slideDuration);
                float stepped = Mathf.Floor(t * slideSteps) / slideSteps;
                rt.anchoredPosition = new Vector2(
                    Mathf.Lerp(home.x + offscreenX, home.x, stepped),
                    home.y);
                yield return null;
            }

            rt.anchoredPosition = home;
        }

        private void SetLabelButton(int labelIndex, bool enabled)
        {
            // Button i gates label i+1 (label 0 is the title, ungated).
            int gateIndex = labelIndex - 1;
            if (gateIndex >= 0 && gateIndex < _gatedButtons.Count)
            {
                SetGatedButton(_gatedButtons[gateIndex], enabled);
            }
        }

        private void SetGatedButton(Button button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        private void LoadGame()
        {
            SceneManager.LoadScene("Opening_1");
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
