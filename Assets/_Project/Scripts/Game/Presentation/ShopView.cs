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

        private TextMesh _title;
        private TextMesh _bits;
        private TextMesh _hint;
        private readonly TextMesh[] _rows = new TextMesh[4];
        private readonly string[] _lastRows = new string[4];
        private int _lastBits;
        private int _selected;
        private bool _open;

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
            }
            UiTypography.Style(_hint, 28, 0.065f, UiTypography.Dim,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
            if (_title != null) _title.text = "== SHOP PHASE ==";
            if (_hint != null) _hint.text = "ATAS/BAWAH pilih · ENTER beli / lanjut";
            gameObject.SetActive(false);
            _open = false;
        }

        public void Show()
        {
            _open = true;
            _selected = 0;
            gameObject.SetActive(true);
            Repaint();
        }

        public void Hide()
        {
            _open = false;
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                _selected = (_selected + _rows.Length - 1) % _rows.Length;
                Repaint();
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                _selected = (_selected + 1) % _rows.Length;
                Repaint();
            }
            else if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Confirmed?.Invoke(_selected);
            }
        }

        private void Repaint()
        {
            if (_bits != null)
            {
                _bits.text = $"PTS {_lastBits}";
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i] == null)
                {
                    continue;
                }

                _rows[i].text = (i == _selected ? "▶ " : "  ") + _lastRows[i];
                _rows[i].color = i == _selected ? UiTypography.Info : UiTypography.Ink;
            }
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }
    }
}
