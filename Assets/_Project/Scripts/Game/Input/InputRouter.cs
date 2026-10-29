using System;
using AlgoRhythm.Game.Rhythm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Input
{
    /// <summary>Sumber tap Beatblock: Z, X, atau klik kiri.</summary>
    public enum TapSource
    {
        Z = 0,
        X = 1,
        Click = 2
    }

    /// <summary>
    /// Beatblock input: Aim kontinu (posisi pointer) + tiga tap terpisah.
    /// Timestamp tepat sekali memakai DSP, tak pernah dari frame/animasi.
    /// Tiga action tap terpisah (bukan satu action multi-binding) agar Z lalu X
    /// bergantian cepat tidak saling memblokir conflict resolution.
    /// </summary>
    public sealed class InputRouter : MonoBehaviour
    {
        private DspClockSource _clock;
        private InputActionMap _rhythmMap;
        private InputAction _aim;
        private InputAction _tapZ;
        private InputAction _tapX;
        private InputAction _tapClick;
        private InputAction _pause;
        private bool _bound;

        public event Action<TapSource, double> TapPressed;

        /// <summary>Fired on release; hanya release dari sumber yang sama mengakhiri hold.</summary>
        public event Action<TapSource, double> TapReleased;

        public event Action PausePressed;

        public bool IsBound => _bound;

        /// <summary>Posisi pointer terakhir (piksel layar) untuk aim paddle.</summary>
        public Vector2 AimPosition { get; private set; }

        /// <summary>
        /// Ketika true, AimPosition hanya diperbarui via SetAimPosition (Touch overlay),
        /// mengabaikan pointer generik agar tap tombol tidak memindahkan paddle.
        /// </summary>
        public bool LockPointerAim { get; set; }

        /// <summary>Memperbarui AimPosition dari input eksternal (mis. virtual touch pad).</summary>
        public void SetAimPosition(Vector2 screenPosition)
        {
            AimPosition = screenPosition;
        }

        /// <summary>Memicu tap dari tombol virtual touch (Tap A / Tap B / Click).</summary>
        public void TriggerVirtualTap(TapSource source, bool pressed)
        {
            if (pressed)
            {
                FirePressed(source);
            }
            else
            {
                FireReleased(source);
            }
        }

        /// <summary>Memicu pause dari tombol virtual atau gesture Android.</summary>
        public void TriggerVirtualPause()
        {
            PausePressed?.Invoke();
        }

        public void Initialize(DspClockSource clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            TryBind();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void TryBind()
        {
            if (_bound || _clock == null)
            {
                return;
            }

            InputActionAsset asset = InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError("AlgoRhythm: project-wide Input System actions are not available.");
                return;
            }

            _rhythmMap = asset.FindActionMap("Rhythm", throwIfNotFound: false);
            if (_rhythmMap == null)
            {
                Debug.LogError("AlgoRhythm: input action map 'Rhythm' is missing.");
                return;
            }

            _aim = _rhythmMap.FindAction("Aim", throwIfNotFound: false);
            _tapZ = _rhythmMap.FindAction("TapZ", throwIfNotFound: false);
            _tapX = _rhythmMap.FindAction("TapX", throwIfNotFound: false);
            _tapClick = _rhythmMap.FindAction("TapClick", throwIfNotFound: false);
            _pause = _rhythmMap.FindAction("Pause", throwIfNotFound: false);

            // Fallback: bila asset .inputactions belum dimigrasi (mis. scene lama),
            // buat action runtime agar game tetap jalan tanpa edit asset manual.
            if (_aim == null || _tapZ == null || _tapX == null || _tapClick == null)
            {
                CreateRuntimeTapActions();
            }
            else
            {
                _aim.performed += OnAim;
                _aim.canceled += OnAim;
                _tapZ.performed += OnTapZPressed;
                _tapX.performed += OnTapXPressed;
                _tapClick.performed += OnTapClickPressed;
                _tapZ.canceled += OnTapZReleased;
                _tapX.canceled += OnTapXReleased;
                _tapClick.canceled += OnTapClickReleased;
            }

            if (_pause != null)
            {
                _pause.performed += OnPause;
            }

            _rhythmMap.Enable();
            _bound = true;
        }

        private void CreateRuntimeTapActions()
        {
            // Runtime fallback tanpa menyentuh asset: Z/X/klik + aim pointer.
            _tapZ = new InputAction("TapZ", InputActionType.Button, "<Keyboard>/z");
            _tapX = new InputAction("TapX", InputActionType.Button, "<Keyboard>/x");
            _tapClick = new InputAction("TapClick", InputActionType.Button, "<Mouse>/leftButton");
            _aim = new InputAction("Aim", InputActionType.Value, "<Pointer>/position");
            _tapZ.performed += OnTapZPressed;
            _tapX.performed += OnTapXPressed;
            _tapClick.performed += OnTapClickPressed;
            _tapZ.canceled += OnTapZReleased;
            _tapX.canceled += OnTapXReleased;
            _tapClick.canceled += OnTapClickReleased;
            _aim.performed += OnAim;
            _aim.canceled += OnAim;
            _tapZ.Enable();
            _tapX.Enable();
            _tapClick.Enable();
            _aim.Enable();
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            if (_aim != null)
            {
                _aim.performed -= OnAim;
                _aim.canceled -= OnAim;
            }
            if (_tapZ != null)
            {
                _tapZ.performed -= OnTapZPressed;
                _tapZ.canceled -= OnTapZReleased;
            }
            if (_tapX != null)
            {
                _tapX.performed -= OnTapXPressed;
                _tapX.canceled -= OnTapXReleased;
            }
            if (_tapClick != null)
            {
                _tapClick.performed -= OnTapClickPressed;
                _tapClick.canceled -= OnTapClickReleased;
            }
            if (_pause != null)
            {
                _pause.performed -= OnPause;
            }
            if (_rhythmMap != null)
            {
                _rhythmMap.Disable();
            }
            _bound = false;
        }

        private void OnAim(InputAction.CallbackContext context)
        {
            if (LockPointerAim || Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                return;
            }

            AimPosition = context.ReadValue<Vector2>();
        }

        private void OnTapZPressed(InputAction.CallbackContext context) => FirePressed(TapSource.Z);
        private void OnTapXPressed(InputAction.CallbackContext context) => FirePressed(TapSource.X);
        private void OnTapClickPressed(InputAction.CallbackContext context) => FirePressed(TapSource.Click);

        private void OnTapZReleased(InputAction.CallbackContext context) => FireReleased(TapSource.Z);
        private void OnTapXReleased(InputAction.CallbackContext context) => FireReleased(TapSource.X);
        private void OnTapClickReleased(InputAction.CallbackContext context) => FireReleased(TapSource.Click);

        private void OnPause(InputAction.CallbackContext context) => PausePressed?.Invoke();

        private void FirePressed(TapSource source)
        {
            if (_clock == null)
            {
                return;
            }

            double inputDsp = _clock.NowDsp;
            TapPressed?.Invoke(source, inputDsp);
        }

        private void FireReleased(TapSource source)
        {
            if (_clock == null)
            {
                return;
            }

            double inputDsp = _clock.NowDsp;
            TapReleased?.Invoke(source, inputDsp);
        }
    }
}
