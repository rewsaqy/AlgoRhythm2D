using System;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Game.Rhythm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Input
{
    /// <summary>
    /// Reads the project-wide Rhythm action map. Input is timestamped exactly
    /// once, using DSP time, and is never derived from a frame or animation.
    /// </summary>
    public sealed class InputRouter : MonoBehaviour
    {
        private DspClockSource _clock;
        private InputActionMap _rhythmMap;
        private InputAction _shieldNorth;
        private InputAction _shieldEast;
        private InputAction _shieldSouth;
        private InputAction _shieldWest;
        private InputAction _pause;
        private bool _bound;

        public event Action<Direction, double> ShieldPressed;

        public event Action PausePressed;

        public bool IsBound => _bound;

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

            _shieldNorth = _rhythmMap.FindAction("ShieldNorth", throwIfNotFound: true);
            _shieldEast = _rhythmMap.FindAction("ShieldEast", throwIfNotFound: true);
            _shieldSouth = _rhythmMap.FindAction("ShieldSouth", throwIfNotFound: true);
            _shieldWest = _rhythmMap.FindAction("ShieldWest", throwIfNotFound: true);
            _pause = _rhythmMap.FindAction("Pause", throwIfNotFound: true);

            _shieldNorth.performed += OnShieldNorth;
            _shieldEast.performed += OnShieldEast;
            _shieldSouth.performed += OnShieldSouth;
            _shieldWest.performed += OnShieldWest;
            _pause.performed += OnPause;

            _rhythmMap.Enable();
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            _shieldNorth.performed -= OnShieldNorth;
            _shieldEast.performed -= OnShieldEast;
            _shieldSouth.performed -= OnShieldSouth;
            _shieldWest.performed -= OnShieldWest;
            _pause.performed -= OnPause;
            _rhythmMap.Disable();
            _bound = false;
        }

        private void OnShieldNorth(InputAction.CallbackContext context) => Fire(Direction.North);

        private void OnShieldEast(InputAction.CallbackContext context) => Fire(Direction.East);

        private void OnShieldSouth(InputAction.CallbackContext context) => Fire(Direction.South);

        private void OnShieldWest(InputAction.CallbackContext context) => Fire(Direction.West);

        private void OnPause(InputAction.CallbackContext context) => PausePressed?.Invoke();

        private void Fire(Direction direction)
        {
            if (_clock == null)
            {
                return;
            }

            double inputDsp = _clock.NowDsp;
            ShieldPressed?.Invoke(direction, inputDsp);
        }
    }
}
