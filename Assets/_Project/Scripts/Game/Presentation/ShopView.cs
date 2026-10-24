using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Between-wave shop (Beatblock-greybox, Bahasa campuran). Keyboard only:
    /// Up/Down arrows move the cursor, Enter buys (or continues on LANJUT).
    /// No mouse, no runtime GameObjects — all labels are authored in scene.
    /// Rendering is dumb by design: the controller owns prices, stacks and
    /// currency, this view only shows rows and reports the confirmed slot.
    /// Slots: 0 = heal, 1 = AIDA accuracy, 2 = AIDA fire rate, 3 = continue.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        public event Action<int> Confirmed;

        /// <summary>Slot ids: regen track, AIDA track, continue.</summary>
        public const int SlotRegen = 0;
        public const int SlotAida = 1;
        public const int SlotContinue = 2;
        public const int SlotCount = 3;

        private TextMesh _title;
        private TextMesh _bits;
        private TextMesh _hint;
        private readonly TextMesh[] _rows = new TextMesh[4];
        private readonly string[] _lastRows = new string[4];
        private readonly Vector3[] _basePos = new Vector3[4];
        private int _lastBits;
        private int _selected;
        private bool _open;
        private float _animStart;
        private bool _animating;

        public bool IsOpen => _open;

        public void Initialize()
        {
            _title = FindText("Title");
            _bits = FindText("Bits");
            _hint = FindText("Hint");
            _rows[0] = FindText("Item0");
            _rows[1] = FindText("Item1");
            _rows[2] = FindText("Item2");
            _rows[3] = FindText("Item3");
            UiTypography.Style(_title, 44, 0.085f, UiTypography.Status,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
            UiTypography.Style(_bits, 40, 0.08f, UiTypography.Good,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
            for (int i = 0; i < _rows.Length; i++)
            {
                UiTypography.Style(_rows[i], 36, 0.075f, UiTypography.Ink,
                    TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
                if (_rows[i] != null)
                {
                    _basePos[i] = _rows[i].transform.localPosition;
                    // Slot 3 is unused in the 2-track + LANJUT layout.
                    _rows[i].gameObject.SetActive(i < SlotCount);
                }
            }
            UiTypography.Style(_hint, 28, 0.065f, UiTypography.Dim,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
            if (_title != null) _title.text = "SHOP PHASE";
            if (_hint != null) _hint.text = "ATAS/BAWAH pilih · ENTER beli / lanjut";
            SetupBackdrop();
            gameObject.SetActive(false);
            _open = false;
            _animating = false;
        }

        public void Show()
        {
            _open = true;
            _selected = 0;

            // Ensure shop is neatly situated on the open left side
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(-4.3f, -0.35f);
            }
            else
            {
                transform.localPosition = new Vector3(-4.3f, -0.35f, 0f);
            }

            gameObject.SetActive(true);
            _animStart = Time.unscaledTime;
            _animating = true;
            Repaint();
        }

        public void Hide()
        {
            _open = false;
            _animating = false;
            gameObject.SetActive(false);
        }

        public void Render(int bits, string[] rows)
        {
            _lastBits = bits;
            for (int i = 0; i < _lastRows.Length; i++)
            {
                _lastRows[i] = rows != null && i < rows.Length ? rows[i] ?? string.Empty : string.Empty;
            }
            Repaint();
        }

        private void Update()
        {
            if (!_open)
            {
                return;
            }

            UpdateEntrance();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                _selected = (_selected + SlotCount - 1) % SlotCount;
                Repaint();
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                _selected = (_selected + 1) % SlotCount;
                Repaint();
            }
            else if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Confirmed?.Invoke(_selected);
            }
        }

        private static readonly string[] SlotIcons = { "[+]", "(O)", "[→]" };

        /// <summary>
        /// Stepped slide-in per row (retro judder like the main menu):
        /// odd rows sweep from the left, even from the right, staggered.
        /// </summary>
        private void UpdateEntrance()
        {
            if (!_animating)
            {
                return;
            }

            const float slideSeconds = 0.35f;
            const int steps = 5;
            const float stagger = 0.07f;
            const float travel = 2.0f;

            bool done = true;
            float now = Time.unscaledTime;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i] == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01((now - _animStart - i * stagger) / slideSeconds);
                if (t < 1f)
                {
                    done = false;
                }

                float stepped = Mathf.Floor(t * steps) / steps;
                float side = (i % 2 == 0) ? -1f : 1f;
                Vector3 basePos = _basePos[i];
                basePos.x += side * (1f - stepped) * travel;
                _rows[i].transform.localPosition = basePos;
            }

            if (done)
            {
                _animating = false;
                for (int i = 0; i < _rows.Length; i++)
                {
                    if (_rows[i] != null)
                    {
                        _rows[i].transform.localPosition = _basePos[i];
                    }
                }
            }
        }

        private void Repaint()
        {
            if (_bits != null)
            {
                _bits.text = $"PTS {_lastBits}";
            }

            for (int i = 0; i < SlotCount && i < _rows.Length; i++)
            {
                if (_rows[i] == null)
                {
                    continue;
                }

                _rows[i].text = (i == _selected ? "▶ " : "  ") + SlotIcons[i] + " " + _lastRows[i];
                _rows[i].color = i == _selected ? UiTypography.Info : UiTypography.Ink;
            }
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }

        private void SetupBackdrop()
        {
            Transform existing = transform.Find("ShopBackdrop");
            GameObject backdrop;
            if (existing != null)
            {
                backdrop = existing.gameObject;
            }
            else
            {
                backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = "ShopBackdrop";
                backdrop.transform.SetParent(transform, false);
                backdrop.transform.localPosition = new Vector3(0f, -0.05f, 0.05f);
                backdrop.transform.localScale = new Vector3(6.6f, 4.2f, 1f);

                Collider col = backdrop.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(col);
                    }
                    else
                    {
                        DestroyImmediate(col);
                    }
                }

                Renderer rend = backdrop.GetComponent<Renderer>();
                if (rend != null)
                {
                    Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Unlit/Color");
                    if (s != null)
                    {
                        Material m = new Material(s);
                        m.color = new Color(0.03f, 0.04f, 0.08f, 0.96f);
                        rend.material = m;
                    }
                }
            }

            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                var img = panel.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = new Color(0.03f, 0.04f, 0.08f, 0.95f);
                }
            }
        }
    }
}
