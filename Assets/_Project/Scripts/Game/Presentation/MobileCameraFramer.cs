using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Strictly locks the game viewport and aspect ratio to 1920x1080 (16:9).
    /// On wider displays (e.g. 2400x1080 mobile phones), it applies solid black pillarbox bars
    /// on the left and right, ensuring the playable area is EXACTLY 1920x1080.
    /// Auto-attaches to Camera.main in all scenes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class MobileCameraFramer : MonoBehaviour
    {
        public const float TargetWidth = 1920f;
        public const float TargetHeight = 1080f;
        public const float TargetAspect = TargetWidth / TargetHeight; // 1.7777778f
        private const float BaseOrthoSize = 5f;

        private Camera _cam;
        private int _lastWidth;
        private int _lastHeight;

        private GameObject _overlayGo;
        private RectTransform _leftBar;
        private RectTransform _rightBar;
        private RectTransform _topBar;
        private RectTransform _bottomBar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            AttachToCamera();
            SceneManager.sceneLoaded += (scene, mode) => AttachToCamera();
        }

        public static void AttachToCamera()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MobileCameraFramer>() == null)
            {
                cam.gameObject.AddComponent<MobileCameraFramer>();
            }
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            Apply1080pFraming();
        }

        private void OnEnable()
        {
            Apply1080pFraming();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                Apply1080pFraming();
            }
        }

        public void Apply1080pFraming()
        {
            if (_cam == null)
            {
                _cam = GetComponent<Camera>();
            }

            if (_cam == null)
            {
                return;
            }

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            float currentAspect = (float)_lastWidth / Mathf.Max(1, _lastHeight);
            float variance = currentAspect / TargetAspect;

            _cam.orthographic = true;
            _cam.orthographicSize = BaseOrthoSize;

            if (Mathf.Abs(currentAspect - TargetAspect) < 0.005f)
            {
                // Exact 16:9 (1920x1080)
                _cam.rect = new Rect(0f, 0f, 1f, 1f);
                SetBarsActive(false, false, false, false);
            }
            else if (variance > 1.0f)
            {
                // Wider screen (e.g. 2400x1080 phone -> 20:9): Pillarbox
                // Normalized width of 1920 within 2400 is 1920 / 2400 = 0.8
                float insetWidth = 1.0f / variance;
                float x = (1.0f - insetWidth) * 0.5f;
                _cam.rect = new Rect(x, 0f, insetWidth, 1f);

                EnsureOverlay();
                UpdatePillarboxBars(x);
            }
            else
            {
                // Taller screen (e.g. 4:3 tablet): Letterbox
                float insetHeight = variance;
                float y = (1.0f - insetHeight) * 0.5f;
                _cam.rect = new Rect(0f, y, 1f, insetHeight);

                EnsureOverlay();
                UpdateLetterboxBars(y);
            }
        }

        private void EnsureOverlay()
        {
            if (_overlayGo != null)
            {
                return;
            }

            _overlayGo = GameObject.Find("Resolution1080p_PillarboxOverlay");
            if (_overlayGo == null)
            {
                _overlayGo = new GameObject("Resolution1080p_PillarboxOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                _overlayGo.hideFlags = HideFlags.DontSave;
            }

            Canvas canvas = _overlayGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760; // Top-most layer

            CanvasScaler scaler = _overlayGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(TargetWidth, TargetHeight);

            // Left Bar
            _leftBar = CreateBar("LeftPillarboxBar", _overlayGo.transform);
            // Right Bar
            _rightBar = CreateBar("RightPillarboxBar", _overlayGo.transform);
            // Top Bar
            _topBar = CreateBar("TopLetterboxBar", _overlayGo.transform);
            // Bottom Bar
            _bottomBar = CreateBar("BottomLetterboxBar", _overlayGo.transform);
        }

        private RectTransform CreateBar(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            GameObject barGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(parent, false);

            Image img = barGo.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false; // Does not block inputs

            return barGo.GetComponent<RectTransform>();
        }

        private void UpdatePillarboxBars(float normalizedMarginX)
        {
            if (_leftBar == null || _rightBar == null)
            {
                return;
            }

            _leftBar.gameObject.SetActive(true);
            _leftBar.anchorMin = new Vector2(0f, 0f);
            _leftBar.anchorMax = new Vector2(normalizedMarginX, 1f);
            _leftBar.offsetMin = Vector2.zero;
            _leftBar.offsetMax = Vector2.zero;

            _rightBar.gameObject.SetActive(true);
            _rightBar.anchorMin = new Vector2(1f - normalizedMarginX, 0f);
            _rightBar.anchorMax = new Vector2(1f, 1f);
            _rightBar.offsetMin = Vector2.zero;
            _rightBar.offsetMax = Vector2.zero;

            if (_topBar != null) _topBar.gameObject.SetActive(false);
            if (_bottomBar != null) _bottomBar.gameObject.SetActive(false);
        }

        private void UpdateLetterboxBars(float normalizedMarginY)
        {
            if (_topBar == null || _bottomBar == null)
            {
                return;
            }

            _bottomBar.gameObject.SetActive(true);
            _bottomBar.anchorMin = new Vector2(0f, 0f);
            _bottomBar.anchorMax = new Vector2(1f, normalizedMarginY);
            _bottomBar.offsetMin = Vector2.zero;
            _bottomBar.offsetMax = Vector2.zero;

            _topBar.gameObject.SetActive(true);
            _topBar.anchorMin = new Vector2(0f, 1f - normalizedMarginY);
            _topBar.anchorMax = new Vector2(1f, 1f);
            _topBar.offsetMin = Vector2.zero;
            _topBar.offsetMax = Vector2.zero;

            if (_leftBar != null) _leftBar.gameObject.SetActive(false);
            if (_rightBar != null) _rightBar.gameObject.SetActive(false);
        }

        private void SetBarsActive(bool left, bool right, bool top, bool bottom)
        {
            if (_leftBar != null) _leftBar.gameObject.SetActive(left);
            if (_rightBar != null) _rightBar.gameObject.SetActive(right);
            if (_topBar != null) _topBar.gameObject.SetActive(top);
            if (_bottomBar != null) _bottomBar.gameObject.SetActive(bottom);
        }

        private void OnDestroy()
        {
            if (_overlayGo != null)
            {
                DestroyImmediate(_overlayGo);
            }
        }
    }
}
