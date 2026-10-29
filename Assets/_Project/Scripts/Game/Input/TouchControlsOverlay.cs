using System;
using AlgoRhythm.Game.Combat;
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
    /// - Secret mobile cheat menu triggered via 5x TAP 1 then 5x TAP 2 while game is paused.
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
        private CombatController _combatController;
        private bool _isInitialized;
        private Canvas _canvas;

        private GameObject _shopNavContainer;

        // Secret mobile cheat menu fields
        private int _secretTap1Count;
        private int _secretTap2Count;
        private GameObject _cheatMenuModal;
        private Text _cheatStatusText;

        public void Initialize(InputRouter inputRouter, Transform arenaTransform, ShopView shopView = null, CombatController combatController = null)
        {
            _inputRouter = inputRouter ?? throw new ArgumentNullException(nameof(inputRouter));
            _arenaTransform = arenaTransform;
            _shopView = shopView ?? FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            _combatController = combatController;
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

            // Reset secret cheat combo if unpaused
            if (!IsGamePaused())
            {
                _secretTap1Count = 0;
                _secretTap2Count = 0;
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

            // Secret Mobile Cheat Trigger:
            // When game is paused, track 5x TAP 1 followed by 5x TAP 2
            if (pressed && IsGamePaused())
            {
                ProcessSecretCheatTap(source);
                return;
            }

            _inputRouter?.TriggerVirtualTap(source, pressed);
        }

        private void ProcessSecretCheatTap(TapSource source)
        {
            if (!IsMobileAllowed())
            {
                return;
            }

            if (_cheatMenuModal != null && _cheatMenuModal.activeSelf)
            {
                return;
            }

            if (source == TapSource.Z) // TAP 1
            {
                if (_secretTap2Count > 0)
                {
                    // TAP 2 had already started, reset and count as 1st tap of TAP 1
                    _secretTap1Count = 1;
                    _secretTap2Count = 0;
                }
                else
                {
                    _secretTap1Count++;
                }
            }
            else if (source == TapSource.X) // TAP 2
            {
                if (_secretTap1Count >= 5)
                {
                    _secretTap2Count++;
                    if (_secretTap2Count >= 5)
                    {
                        _secretTap1Count = 0;
                        _secretTap2Count = 0;
                        OpenCheatMenu();
                    }
                }
                else
                {
                    // TAP 2 tapped prematurely before reaching 5x TAP 1
                    _secretTap1Count = 0;
                    _secretTap2Count = 0;
                }
            }
        }

        private bool IsMobileAllowed()
        {
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                return true;
            }

#if UNITY_EDITOR
            if (forceShowInEditor)
            {
                return true;
            }
#endif

            return false;
        }

        private bool IsGamePaused()
        {
            var controller = GetCombatController();
            return controller != null && controller.IsPaused;
        }

        private CombatController GetCombatController()
        {
            if (_combatController != null)
            {
                return _combatController;
            }

            var bootstrap = FindAnyObjectByType<AlgoRhythm.Game.Bootstrap.GameBootstrap>(FindObjectsInactive.Include);
            if (bootstrap != null && bootstrap.Controller != null)
            {
                _combatController = bootstrap.Controller;
            }

            return _combatController;
        }

        public void OpenCheatMenu()
        {
            if (!IsMobileAllowed())
            {
                return;
            }

            if (_cheatMenuModal == null)
            {
                BuildCheatMenuUI(_canvas != null ? _canvas.transform : transform);
            }

            if (_cheatMenuModal != null)
            {
                _cheatMenuModal.SetActive(true);
                SetCheatStatus("Pilih perintah debugging sistem:");
            }
        }

        public void CloseCheatMenu()
        {
            if (_cheatMenuModal != null)
            {
                _cheatMenuModal.SetActive(false);
            }
        }

        private void SetCheatStatus(string message, bool isSuccess = false)
        {
            if (_cheatStatusText != null)
            {
                _cheatStatusText.text = message;
                _cheatStatusText.color = isSuccess ? UiTypography.Good : UiTypography.Info;
            }
        }

        private void BuildCheatMenuUI(Transform parent)
        {
            // Modal full-screen background
            _cheatMenuModal = new GameObject("SecretCheatMenuModal", typeof(RectTransform), typeof(Image));
            _cheatMenuModal.transform.SetParent(parent, false);

            RectTransform modalRt = _cheatMenuModal.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;

            Image modalBg = _cheatMenuModal.GetComponent<Image>();
            modalBg.color = new Color(0.02f, 0.05f, 0.08f, 0.88f);

            // Center Panel
            GameObject panelGo = new GameObject("CheatPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelGo.transform.SetParent(_cheatMenuModal.transform, false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(1080f, 680f);

            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.09f, 0.15f, 0.96f);

            Outline panelOutline = panelGo.GetComponent<Outline>();
            panelOutline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.85f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -42f);
            titleRt.sizeDelta = new Vector2(1000f, 44f);

            Text titleTxt = titleGo.GetComponent<Text>();
            titleTxt.text = "// SISTEM OVERRIDE: CHEAT MENU //";
            titleTxt.font = UiTypography.DisplayFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.0f, 0.94f, 1.0f, 1.0f);

            // Subtitle / Status text
            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(panelGo.transform, false);
            RectTransform statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -85f);
            statusRt.sizeDelta = new Vector2(1000f, 32f);

            _cheatStatusText = statusGo.GetComponent<Text>();
            _cheatStatusText.font = UiTypography.DisplayFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _cheatStatusText.fontSize = 20;
            _cheatStatusText.alignment = TextAnchor.MiddleCenter;
            _cheatStatusText.color = UiTypography.Info;
            _cheatStatusText.text = "Pilih perintah debugging sistem:";

            // 8 Cheat Buttons (2 columns x 4 rows)
            Vector2 btnSize = new Vector2(470f, 80f);
            float colLeftX = -250f;
            float colRightX = 250f;
            float row0Y = -155f;
            float row1Y = -250f;
            float row2Y = -345f;
            float row3Y = -440f;

            Color standardBg = new Color(0.08f, 0.22f, 0.35f, 0.92f);
            Color accentBg = new Color(0.12f, 0.26f, 0.38f, 0.92f);
            Color specialBg = new Color(0.28f, 0.12f, 0.22f, 0.92f);
            Color closeBg = new Color(0.24f, 0.10f, 0.10f, 0.92f);

            // Column Left
            CreateCheatButton(panelGo.transform, "Btn_F6", "[F6] PULIH PENUH (HP & ENERGY)", new Vector2(colLeftX, row0Y), btnSize, standardBg, () =>
            {
                GetCombatController()?.DevFullHeal();
                SetCheatStatus("F6: Integritas dan Energy berhasil dipulihkan penuh!", true);
            });

            CreateCheatButton(panelGo.transform, "Btn_F7", "[F7] TAMBAH 999 PTS", new Vector2(colLeftX, row1Y), btnSize, standardBg, () =>
            {
                GetCombatController()?.DevAddPts(999);
                SetCheatStatus("F7: +999 PTS berhasil ditambahkan ke saldo!", true);
            });

            CreateCheatButton(panelGo.transform, "Btn_F8", "[F8] LEWATI 1 WAVE (+999 PTS)", new Vector2(colLeftX, row2Y), btnSize, accentBg, () =>
            {
                CloseCheatMenu();
                GetCombatController()?.DevSkipSingleWave(999);
            });

            CreateCheatButton(panelGo.transform, "Btn_F9", "[F9] LEWATI SEMUA WAVE (OUTRO)", new Vector2(colLeftX, row3Y), btnSize, specialBg, () =>
            {
                CloseCheatMenu();
                GetCombatController()?.DevFinishAllWaves();
            });

            // Column Right
            CreateCheatButton(panelGo.transform, "Btn_F10", "[F10] BOSS FIGHT (AIDA)", new Vector2(colRightX, row0Y), btnSize, standardBg, () =>
            {
                CloseCheatMenu();
                if (GetCombatController() != null) GetCombatController().DevJumpToBossFight();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
            });

            CreateCheatButton(panelGo.transform, "Btn_F11", "[F11] PERCEPAT RITME (+16 BEAT)", new Vector2(colRightX, row1Y), btnSize, standardBg, () =>
            {
                GetCombatController()?.DevFastForward(16);
                SetCheatStatus("F11: Linimasa musik maju +16 Beat!", true);
            });

            CreateCheatButton(panelGo.transform, "Btn_F12", "[F12] MAGNUM OPUS ENDING", new Vector2(colRightX, row2Y), btnSize, specialBg, () =>
            {
                CloseCheatMenu();
                if (GetCombatController() != null) GetCombatController().DevSkipToMagnumOpusEnding();
                else
                {
                    PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerEnding", 1);
                    UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
                }
            });

            CreateCheatButton(panelGo.transform, "Btn_Close", "[X] TUTUP MENU", new Vector2(colRightX, row3Y), btnSize, closeBg, () =>
            {
                CloseCheatMenu();
            });

            // Footer tip text
            GameObject tipGo = new GameObject("FooterTip", typeof(RectTransform), typeof(Text));
            tipGo.transform.SetParent(panelGo.transform, false);
            RectTransform tipRt = tipGo.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0.5f, 0f);
            tipRt.anchorMax = new Vector2(0.5f, 0f);
            tipRt.anchoredPosition = new Vector2(0f, 38f);
            tipRt.sizeDelta = new Vector2(1000f, 30f);

            Text tipTxt = tipGo.GetComponent<Text>();
            tipTxt.text = "Tap tombol [II] di pojok kanan atas untuk melanjutkan ritme";
            tipTxt.font = UiTypography.DisplayFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            tipTxt.fontSize = 18;
            tipTxt.alignment = TextAnchor.MiddleCenter;
            tipTxt.color = UiTypography.Dim;
        }

        private void CreateCheatButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color bgColor, Action onClick)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            Outline outline = btnGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.85f, 0.95f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(Mathf.Min(1f, bgColor.r + 0.15f), Mathf.Min(1f, bgColor.g + 0.15f), Mathf.Min(1f, bgColor.b + 0.15f), 1f);
            colors.pressedColor = new Color(0.18f, 0.60f, 0.90f, 1f);
            colors.selectedColor = bgColor;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(12f, 4f);
            lrt.offsetMax = new Vector2(-12f, -4f);

            Text txt = labelGo.GetComponent<Text>();
            txt.text = label;
            txt.font = UiTypography.DisplayFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = UiTypography.Ink;
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
