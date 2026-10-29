using System;
using AlgoRhythm.Game.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AlgoRhythm.Game.Input
{
    /// <summary>
    /// Virtual touch controls overlay for Android mobile devices.
    /// Provides:
    /// - Invisible virtual thumb steering on left-half screen to steer the Beatblock paddle 360 degrees.
    /// - Right-side dual tap buttons (Tap 1 = Z, Tap 2 = X) for alternating rhythm taps and streams.
    /// - Top-right pause button ([II]).
    /// - Shop Phase navigation arrows (▲ UP and ▼ DOWN) displayed only during SHOP PHASE.
    /// - Confirming shop selection via right-side TAP buttons during SHOP PHASE.
    /// - Android hardware back-button handling.
    /// </summary>
    public sealed class TouchControlsOverlay : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Force display overlay in the Unity Editor for debugging or Unity Remote 5.")]
        [SerializeField] private bool forceShowInEditor = true;
        [SerializeField] private float aimSensitivity = 1f;

        private InputRouter _inputRouter;
        private Camera _mainCamera;
        private Transform _arenaTransform;
        private ShopView _shopView;
        private bool _isInitialized;
        private Canvas _canvas;

        private GameObject _shopNavContainer;

        public void Initialize(InputRouter inputRouter, Transform arenaTransform, ShopView shopView = null)
        {
            _inputRouter = inputRouter ?? throw new ArgumentNullException(nameof(inputRouter));
            _arenaTransform = arenaTransform;
            _shopView = shopView ?? FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            _mainCamera = Camera.main;
            _isInitialized = true;

            bool isMobile = Application.isMobilePlatform 
                || SystemInfo.deviceType == DeviceType.Handheld
                || Touchscreen.current != null
                || forceShowInEditor;

#if UNITY_EDITOR
            // Automatically show in Editor when target platform is Android (ideal for Unity Remote 5)
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            {
                isMobile = true;
            }
#endif

            if (!isMobile)
            {
                gameObject.SetActive(false);
                return;
            }

            _inputRouter.LockPointerAim = true;
            BuildRuntimeOverlayIfNeeded();
        }

        private void Update()
        {
            if (!_isInitialized || _inputRouter == null)
            {
                return;
            }

            // Sync Shop Phase Navigation UI visibility
            if (_shopView == null)
            {
                _shopView = FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            }

            bool isShopOpen = _shopView != null && _shopView.IsOpen;
            if (_shopNavContainer != null && _shopNavContainer.activeSelf != isShopOpen)
            {
                _shopNavContainer.SetActive(isShopOpen);
            }

            // Android Hardware Back Button check (Escape key in New Input System)
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                _inputRouter.TriggerVirtualPause();
            }
        }

        /// <summary>Constructs runtime canvas and touch buttons if not pre-authored.</summary>
        private void BuildRuntimeOverlayIfNeeded()
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas != null)
            {
                return;
            }

            // Create Canvas
            GameObject canvasGo = new GameObject("TouchControlsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 1. Aim Touch Zone (Left screen half: 0% to 55%) - INVISIBLE, zero visual clutter
            GameObject aimZoneGo = new GameObject("AimZone", typeof(RectTransform), typeof(Image), typeof(TouchAimZone));
            aimZoneGo.transform.SetParent(canvasGo.transform, false);
            RectTransform aimRt = aimZoneGo.GetComponent<RectTransform>();
            aimRt.anchorMin = new Vector2(0f, 0f);
            aimRt.anchorMax = new Vector2(0.55f, 1f);
            aimRt.offsetMin = Vector2.zero;
            aimRt.offsetMax = Vector2.zero;

            Image aimImg = aimZoneGo.GetComponent<Image>();
            aimImg.color = new Color(0f, 0f, 0f, 0.001f); // Transparent raycast target

            TouchAimZone aimZone = aimZoneGo.GetComponent<TouchAimZone>();
            aimZone.Setup(this);

            // 2. Button Tap 1 (Primary, Z)
            CreateVirtualButton(canvasGo.transform, "TapButton_A", "TAP 1", new Vector2(1480, 220), new Vector2(210, 210), TapSource.Z);

            // 3. Button Tap 2 (Secondary stream, X)
            CreateVirtualButton(canvasGo.transform, "TapButton_B", "TAP 2", new Vector2(1730, 360), new Vector2(210, 210), TapSource.X);

            // 4. Pause Button (Top Right corner)
            CreatePauseButton(canvasGo.transform, new Vector2(1820, 1000), new Vector2(100, 70));

            // 5. Shop Phase Navigation Container (Arrow Up & Arrow Down, shown ONLY during shop phase)
            BuildShopNavigationUI(canvasGo.transform);
        }

        private void BuildShopNavigationUI(Transform parent)
        {
            _shopNavContainer = new GameObject("ShopNavContainer", typeof(RectTransform));
            _shopNavContainer.transform.SetParent(parent, false);
            RectTransform navRt = _shopNavContainer.GetComponent<RectTransform>();
            navRt.anchorMin = Vector2.zero;
            navRt.anchorMax = Vector2.one;
            navRt.offsetMin = Vector2.zero;
            navRt.offsetMax = Vector2.zero;

            // Arrow UP (▲) button — placed on the right side above TAP 1 & 2 with generous spacing
            CreateShopArrowButton(_shopNavContainer.transform, "ShopArrowUp", "▲", new Vector2(1620, 750), new Vector2(230, 130), () => _shopView?.MoveSelectionUp());

            // Arrow DOWN (▼) button — placed below UP arrow with 60px gap and comfortable reach
            CreateShopArrowButton(_shopNavContainer.transform, "ShopArrowDown", "▼", new Vector2(1620, 560), new Vector2(230, 130), () => _shopView?.MoveSelectionDown());

            _shopNavContainer.SetActive(false);
        }

        private void CreateShopArrowButton(Transform parent, string name, string glyph, Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.28f, 0.42f, 0.85f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.08f, 0.28f, 0.42f, 0.85f);
            colors.highlightedColor = new Color(0.14f, 0.42f, 0.62f, 0.95f);
            colors.pressedColor = new Color(0.22f, 0.65f, 0.95f, 1f);
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Text txt = labelGo.GetComponent<Text>();
            txt.text = glyph;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 56;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.4f, 0.95f, 1f, 0.95f);
        }

        private void CreateVirtualButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, TapSource source)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VirtualTapButton));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.28f, 0.38f, 0.55f);

            VirtualTapButton btn = btnGo.GetComponent<VirtualTapButton>();
            btn.Setup(this, source);

            // Label
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Text txt = labelGo.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 32;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.4f, 0.9f, 1f, 0.85f);
        }

        private void CreatePauseButton(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            GameObject btnGo = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.6f);

            Button btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(OnPauseClicked);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Text txt = labelGo.GetComponent<Text>();
            txt.text = "II";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.85f, 0.85f, 0.9f, 0.85f);
        }

        public void HandleAimDelta(Vector2 delta)
        {
            if (!_isInitialized || _inputRouter == null)
            {
                return;
            }

            Vector3 coreScreen = GetCoreScreenPosition();
            // Delta in screen space: delta.x > 0 is East, delta.y > 0 is North
            // Passes directly to InputRouter.SetAimPosition relative to core with sensitivity multiplier
            _inputRouter.SetAimPosition(new Vector2(coreScreen.x + delta.x * 10f * aimSensitivity, coreScreen.y + delta.y * 10f * aimSensitivity));
        }

        public void TriggerTap(TapSource source, bool pressed)
        {
            if (_shopView == null)
            {
                _shopView = FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            }

            if (_shopView != null && _shopView.IsOpen)
            {
                if (pressed && !_shopView.IsInGracePeriod)
                {
                    _shopView.ConfirmCurrentSelection();
                }
                return;
            }

            _inputRouter?.TriggerVirtualTap(source, pressed);
        }

        public void OnPauseClicked()
        {
            _inputRouter?.TriggerVirtualPause();
        }

        private Vector3 GetCoreScreenPosition()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null && _arenaTransform != null)
            {
                return _mainCamera.WorldToScreenPoint(_arenaTransform.position);
            }

            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        /// <summary>Sub-component for handling Aim Zone touches with invisible thumb steering.</summary>
        private sealed class TouchAimZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            private TouchControlsOverlay _owner;
            private Vector2 _anchor;
            private int _activePointerId = -1;
            private const float DeadZone = 8f;
            private const float DeadZoneSqr = DeadZone * DeadZone;
            private const float MaxRadius = 110f;

            public void Setup(TouchControlsOverlay owner)
            {
                _owner = owner;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (_activePointerId != -1) return; // Ignore secondary touches in aim zone

                _activePointerId = eventData.pointerId;
                _anchor = eventData.position;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData.pointerId != _activePointerId) return;

                Vector2 delta = eventData.position - _anchor;
                if (delta.sqrMagnitude >= DeadZoneSqr)
                {
                    _owner?.HandleAimDelta(delta);
                }

                if (delta.magnitude > MaxRadius)
                {
                    _anchor = eventData.position - delta.normalized * MaxRadius;
                }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (eventData.pointerId == _activePointerId)
                {
                    _activePointerId = -1;
                }
            }
        }

        /// <summary>Sub-component for handling Tap Button press & release with visual feedback.</summary>
        private sealed class VirtualTapButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            private TouchControlsOverlay _owner;
            private TapSource _source;
            private Image _image;
            private Color _normalColor;
            private Color _pressedColor;

            public void Setup(TouchControlsOverlay owner, TapSource source)
            {
                _owner = owner;
                _source = source;
                _image = GetComponent<Image>();
                if (_image != null)
                {
                    _normalColor = _image.color;
                    _pressedColor = new Color(0.18f, 0.55f, 0.8f, 0.85f);
                }
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (_image != null) _image.color = _pressedColor;
                _owner?.TriggerTap(_source, true);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (_image != null) _image.color = _normalColor;
                _owner?.TriggerTap(_source, false);
            }
        }
    }
}
