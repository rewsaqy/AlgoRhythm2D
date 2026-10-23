using UnityEngine;
using UnityEngine.UI;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Fullscreen black overlay for the run-clear outro (lights-out beat).
    /// A Screen Space Overlay canvas with a plain color Image, no textures
    /// or shaders; alpha is driven on unscaled time so it works while the
    /// beat clock runs. Attach next to a Canvas + Image; the script forces
    /// the black transparent starting state, so no scene tuning is needed.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class BlackoutView : MonoBehaviour
    {
        [Header("Timing [U]")]
        [Min(0.05f)] [SerializeField] private float fadeSeconds = 0.8f;

        private Image _panel;
        private float _from;
        private float _to = 0f;
        private float _startAt;
        private float _duration;
        private bool _fading;
        private int _flickerHalves;
        private float _flickerPeriod = 0.25f;
        private float _nextFlickerAt;
        private bool _flickerOn;

        public bool Paused { get; set; }

        public bool IsBusy => _fading || _flickerHalves > 0;

        /// <summary>Current alpha, for driver sequencing.</summary>
        public float Alpha => _panel != null ? _panel.color.a : 0f;

        private void Awake()
        {
            _panel = GetComponent<Image>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect != null)
                {
                    canvasRect.anchorMin = Vector2.zero;
                    canvasRect.anchorMax = Vector2.one;
                    canvasRect.offsetMin = Vector2.zero;
                    canvasRect.offsetMax = Vector2.zero;
                    canvasRect.sizeDelta = new Vector2(Screen.width, Screen.height);
                }
            }

            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                // Canvas overlay must stretch fullscreen; a freshly added
                // Canvas keeps a zero-area rect that would render nothing.
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            if (_panel != null)
            {
                // UGUI Image with no sprite draws nothing. Build a 1x1 white
                // sprite in memory (no assets, no GameObjects) so the black
                // fade always renders on any Unity version.
                if (_panel.sprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _panel.sprite = Sprite.Create(
                        tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                }

                _panel.raycastTarget = false;
                Color color = Color.black;
                color.a = 0f;
                _panel.color = color;
            }
        }

        /// <summary>Glide toward an alpha over fadeSeconds.</summary>
        public void FadeTo(float targetAlpha)
        {
            if (_panel == null)
            {
                return;
            }

            _flickerHalves = 0;
            _from = _panel.color.a;
            _to = Mathf.Clamp01(targetAlpha);
            _startAt = Time.unscaledTime;
            _duration = Mathf.Max(0.01f, fadeSeconds);
            _fading = !Mathf.Approximately(_from, _to);
            if (!_fading)
            {
                SetAlpha(_to);
            }
        }

        /// <summary>
        /// Black-white strobe: alternate full black and clear, cycles times.
        /// Ends clear. Each half lasts halfPeriod seconds.
        /// </summary>
        public void Flicker(int cycles, float halfPeriod)
        {
            if (_panel == null)
            {
                return;
            }

            _fading = false;
            _flickerHalves = Mathf.Max(0, cycles) * 2;
            _flickerPeriod = Mathf.Max(0.05f, halfPeriod);
            _flickerOn = false;
            _nextFlickerAt = Time.unscaledTime + _flickerPeriod;
            SetAlpha(0f);
        }

        private void Update()
        {
            if (_panel == null || Paused)
            {
                return;
            }

            if (_flickerHalves > 0)
            {
                if (Time.unscaledTime >= _nextFlickerAt)
                {
                    _flickerOn = !_flickerOn;
                    SetAlpha(_flickerOn ? 1f : 0f);
                    _flickerHalves--;
                    _nextFlickerAt = Time.unscaledTime + _flickerPeriod;
                }

                return;
            }

            if (!_fading)
            {
                return;
            }

            float progress = (Time.unscaledTime - _startAt) / _duration;
            if (progress >= 1f)
            {
                _fading = false;
                SetAlpha(_to);
                return;
            }

            SetAlpha(Mathf.Lerp(_from, _to, progress * progress));
        }

        private void SetAlpha(float alpha)
        {
            if (_panel == null)
            {
                return;
            }

            Color color = Color.black;
            color.a = Mathf.Clamp01(alpha);
            _panel.color = color;
        }
    }
}
