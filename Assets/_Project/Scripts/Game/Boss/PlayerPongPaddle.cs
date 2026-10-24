using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Player Deflector Paddle for the Cyber Pong Boss Fight.
    /// Follows mouse position seamlessly with keyboard arrow/A-D support.
    /// Calculates angled ricochet vectors for precision pong deflection.
    /// </summary>
    public sealed class PlayerPongPaddle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float minX = -5.8f;
        [SerializeField] private float maxX = 5.8f;
        [SerializeField] private float keyboardSpeed = 16f;
        [SerializeField] private float paddleWidth = 2.2f;

        [Header("Visuals")]
        [SerializeField] private Transform visualTransform;

        private Camera _mainCamera;
        private Vector3 _targetPosition;
        private Vector3 _baseScale = new Vector3(2.2f, 0.28f, 0.2f);

        public float PaddleWidth => paddleWidth;
        public float HalfWidth => paddleWidth * 0.5f;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _targetPosition = transform.position;
            if (visualTransform != null)
            {
                _baseScale = visualTransform.localScale;
            }
        }

        private void Update()
        {
            float targetX = transform.position.x;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            bool usedMouse = false;
            if (mouse != null && _mainCamera != null)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                if (mouseDelta.sqrMagnitude > 0.05f || mouse.leftButton.isPressed)
                {
                    Vector3 mouseScreen = mouse.position.ReadValue();
                    Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
                    targetX = Mathf.Clamp(mouseWorld.x, minX, maxX);
                    usedMouse = true;
                }
            }

            if (!usedMouse && keyboard != null)
            {
                float moveInput = 0f;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveInput -= 1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveInput += 1f;

                if (Mathf.Abs(moveInput) > 0.01f)
                {
                    targetX = Mathf.Clamp(transform.position.x + moveInput * keyboardSpeed * Time.deltaTime, minX, maxX);
                }
            }

            // Smooth interpolation
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * 32f);
            transform.position = pos;

            // Tilt paddle slightly based on velocity
            if (visualTransform != null)
            {
                float vx = (targetX - pos.x) / Mathf.Max(0.001f, Time.deltaTime);
                float tilt = Mathf.Clamp(-vx * 0.8f, -12f, 12f);
                visualTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
        }

        /// <summary>
        /// Checks if ball hits player paddle and deflects it.
        /// </summary>
        public bool TryDeflect(Vector3 ballPos, Vector3 ballVel, out Vector3 newVel, out float hitOffset)
        {
            newVel = ballVel;
            hitOffset = 0f;

            // Ball must be moving downwards towards player
            if (ballVel.y >= 0f) return false;

            float paddleY = transform.position.y;
            float paddleX = transform.position.x;

            // Check Y proximity
            if (Mathf.Abs(ballPos.y - paddleY) <= 0.45f)
            {
                // Check X overlap
                float diffX = ballPos.x - paddleX;
                if (Mathf.Abs(diffX) <= HalfWidth + 0.35f)
                {
                    hitOffset = Mathf.Clamp(diffX / HalfWidth, -1f, 1f);

                    // Angle calculation: center sends straight up (0, 1), edges send at 45-60 degree angles
                    Vector2 dir = new Vector2(hitOffset * 0.95f, 1.0f).normalized;
                    float speed = ballVel.magnitude;
                    newVel = new Vector3(dir.x, dir.y, 0f) * speed;

                    // Slight punch punch effect on paddle
                    Punch();
                    return true;
                }
            }

            return false;
        }

        public void Punch()
        {
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(_baseScale.x * 1.15f, _baseScale.y * 0.75f, _baseScale.z);
            }
        }

        private void LateUpdate()
        {
            if (visualTransform != null)
            {
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, _baseScale, Time.deltaTime * 12f);
            }
        }
    }
}
