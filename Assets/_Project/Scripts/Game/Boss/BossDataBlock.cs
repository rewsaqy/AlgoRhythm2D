using System;
using UnityEngine;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Ping-Pong Data Core Fragment / Ball.
    /// Traverses the Cyber Pong arena, ricocheting off neon boundary walls
    /// and player/boss paddles with increasing speed and dynamic trail colors.
    /// </summary>
    public sealed class BossDataBlock : MonoBehaviour
    {
        public event Action<Vector3> WallBounced;
        public event Action<float> PlayerDeflected; // (speed)
        public event Action<float> BossDeflected;   // (speed)
        public event Action BossScored;             // Ball passed player (damage player / upload penalty)
        public event Action PlayerScored;           // Ball passed boss (damage boss)

        [Header("Physics Settings")]
        [SerializeField] private float baseSpeed = 8.5f;
        [SerializeField] private float maxSpeed = 22f;
        [SerializeField] private float speedIncrementPerBounce = 1.055f;
        [SerializeField] private float arenaLeft = -6.2f;
        [SerializeField] private float arenaRight = 6.2f;
        [SerializeField] private float goalTop = 4.75f;
        [SerializeField] private float goalBottom = -4.75f;

        [Header("Components")]
        [SerializeField] private Renderer blockRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        private Vector3 _velocity;
        private float _currentSpeed;
        private bool _isDeflectedByPlayer;
        private bool _isActive = true;
        private int _rallyCount;

        public bool IsDeflectedByPlayer => _isDeflectedByPlayer;
        public int RallyCount => _rallyCount;
        public float CurrentSpeed => _currentSpeed;
        public Vector3 Velocity => _velocity;

        private void Awake()
        {
            if (blockRenderer == null) blockRenderer = GetComponent<Renderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
        }

        public void Serve(bool towardsPlayer, float initialSpeedMultiplier = 1.0f)
        {
            _isActive = true;
            _rallyCount = 0;
            _currentSpeed = baseSpeed * initialSpeedMultiplier;
            transform.position = new Vector3(0f, 0.5f, 0f);

            float randomAngle = UnityEngine.Random.Range(-35f, 35f);
            float dirY = towardsPlayer ? -1f : 1f;
            Vector2 dir = Quaternion.Euler(0f, 0f, randomAngle) * new Vector2(0f, dirY);

            _velocity = dir.normalized * _currentSpeed;
            _isDeflectedByPlayer = !towardsPlayer;

            UpdateVisualColor(towardsPlayer ? new Color(1f, 0.35f, 0.1f) : new Color(0f, 1f, 0.85f));
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            // Move ball
            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, 240f * Time.deltaTime);

            Vector3 pos = transform.position;

            // Bounce off Left Wall
            if (pos.x <= arenaLeft && _velocity.x < 0f)
            {
                pos.x = arenaLeft;
                _velocity.x = -_velocity.x;
                WallBounced?.Invoke(pos);
            }
            // Bounce off Right Wall
            else if (pos.x >= arenaRight && _velocity.x > 0f)
            {
                pos.x = arenaRight;
                _velocity.x = -_velocity.x;
                WallBounced?.Invoke(pos);
            }

            transform.position = pos;

            // Check Boss Goal (Player Scored)
            if (pos.y >= goalTop && _velocity.y > 0f)
            {
                _isActive = false;
                PlayerScored?.Invoke();
                return;
            }

            // Check Player Goal (Boss Scored / Miss)
            if (pos.y <= goalBottom && _velocity.y < 0f)
            {
                _isActive = false;
                BossScored?.Invoke();
                return;
            }
        }

        public void OnDeflectedByPlayer(Vector3 newVelocity)
        {
            _rallyCount++;
            _isDeflectedByPlayer = true;
            _currentSpeed = Mathf.Min(maxSpeed, _currentSpeed * speedIncrementPerBounce);
            _velocity = newVelocity.normalized * _currentSpeed;

            UpdateVisualColor(new Color(0f, 1f, 0.9f)); // Neon Cyan
            PlayerDeflected?.Invoke(_currentSpeed);
        }

        public void OnDeflectedByBoss(Vector3 newVelocity)
        {
            _rallyCount++;
            _isDeflectedByPlayer = false;
            _currentSpeed = Mathf.Min(maxSpeed, _currentSpeed * speedIncrementPerBounce);
            _velocity = newVelocity.normalized * _currentSpeed;

            UpdateVisualColor(new Color(1f, 0.25f, 0.15f)); // Neon Orange-Red
            BossDeflected?.Invoke(_currentSpeed);
        }

        public void Stop()
        {
            _isActive = false;
            _velocity = Vector3.zero;
        }

        private void UpdateVisualColor(Color color)
        {
            if (blockRenderer != null && blockRenderer.material != null)
            {
                var mat = blockRenderer.material;
                if (mat.HasProperty("_Color")) mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            }

            if (trailRenderer != null)
            {
                trailRenderer.startColor = color;
                trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }
    }
}
