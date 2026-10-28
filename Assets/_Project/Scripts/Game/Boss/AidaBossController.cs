using System;
using System.Collections;
using UnityEngine;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Boss A.I.D.A. Pong Controller.
    /// Defends the top goal with a responsive cyber shield, tracks the incoming data fragment,
    /// deflects bank shots, and exhibits visual damage reactions and rage phases.
    /// </summary>
    public sealed class AidaBossController : MonoBehaviour
    {
        public event Action<int, int> HealthChanged; // (current, max)
        public event Action Defeated;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 8;
        [SerializeField] private float baseAiSpeed = 7.5f;
        [SerializeField] private float enragedAiSpeed = 11.0f;
        [SerializeField] private float shieldWidth = 2.4f;
        [SerializeField] private float predictionAccuracy = 0.65f;
        [SerializeField] private float minX = -5.5f;
        [SerializeField] private float maxX = 5.5f;

        [Header("References")]
        [SerializeField] private Transform barrelPivot;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer barrelRenderer;
        [SerializeField] private Transform shieldVisual;

        private int _currentHealth;
        private bool _isDefeated;
        private bool _isInvulnerable;
        private Transform _playerTarget;
        private BossDataBlock _activeBall;
        private Vector3 _basePosition;
        private Vector3 _baseShieldScale = new Vector3(1.8f, 0.22f, 0.2f);
        private float _targetX;
        private Coroutine _flashCoroutine;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDefeated => _isDefeated;
        public float ShieldWidth => shieldWidth;
        public float HalfShieldWidth => shieldWidth * 0.5f;
        public float BaseAiSpeed => baseAiSpeed;
        public float EnragedAiSpeed => enragedAiSpeed;
        public float PredictionAccuracy => predictionAccuracy;

        /// <summary>
        /// Scales boss attributes based on AIDA upgrades purchased in the shop.
        /// Levels 0 and 1 are intentionally very easy (slow tracking, narrow shield, low HP, low prediction).
        /// Difficulty scales progressively up to Level 5 (overclock nightmare).
        /// </summary>
        public void ConfigureDifficulty(int aidaLevel)
        {
            int level = Mathf.Clamp(aidaLevel, 0, 5);

            switch (level)
            {
                case 0:
                    maxHealth = 3;
                    baseAiSpeed = 3.5f;
                    enragedAiSpeed = 5.0f;
                    shieldWidth = 1.5f;
                    predictionAccuracy = 0.15f;
                    break;
                case 1:
                    maxHealth = 4;
                    baseAiSpeed = 4.2f;
                    enragedAiSpeed = 6.2f;
                    shieldWidth = 1.8f;
                    predictionAccuracy = 0.30f;
                    break;
                case 2:
                    maxHealth = 6;
                    baseAiSpeed = 6.0f;
                    enragedAiSpeed = 8.5f;
                    shieldWidth = 2.2f;
                    predictionAccuracy = 0.50f;
                    break;
                case 3:
                    maxHealth = 8;
                    baseAiSpeed = 8.0f;
                    enragedAiSpeed = 11.5f;
                    shieldWidth = 2.7f;
                    predictionAccuracy = 0.70f;
                    break;
                case 4:
                    maxHealth = 11;
                    baseAiSpeed = 10.2f;
                    enragedAiSpeed = 14.5f;
                    shieldWidth = 3.2f;
                    predictionAccuracy = 0.85f;
                    break;
                case 5:
                default:
                    maxHealth = 15;
                    baseAiSpeed = 12.5f;
                    enragedAiSpeed = 18.0f;
                    shieldWidth = 3.8f;
                    predictionAccuracy = 0.98f;
                    break;
            }

            _currentHealth = maxHealth;
            if (shieldVisual != null)
            {
                _baseShieldScale = new Vector3(1.8f * (shieldWidth / 2.4f), _baseShieldScale.y, _baseShieldScale.z);
                shieldVisual.localScale = _baseShieldScale;
            }

            HealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Initialize(Transform playerTarget, BossDataBlock ball)
        {
            _playerTarget = playerTarget;
            _activeBall = ball;
            _currentHealth = maxHealth;
            _isDefeated = false;
            _isInvulnerable = false;
            _basePosition = transform.position;
            _targetX = _basePosition.x;

            if (shieldVisual != null)
            {
                _baseShieldScale = shieldVisual.localScale;
            }

            if (barrelPivot == null)
            {
                barrelPivot = transform.Find("BarrelPivot");
            }

            HealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void Update()
        {
            if (_isDefeated) return;

            UpdateAiMovement();
            UpdateAim();
            CheckDeflection();
        }

        private void UpdateAiMovement()
        {
            if (_activeBall == null) return;

            // Tracking speed scales with damage taken (Enraged Phase)
            float hpPct = (float)_currentHealth / maxHealth;
            float currentSpeed = Mathf.Lerp(enragedAiSpeed, baseAiSpeed, hpPct);

            Vector3 ballPos = _activeBall.transform.position;
            Vector3 ballVel = _activeBall.Velocity;

            // Only intensely track when ball is heading towards boss
            if (ballVel.y > 0f)
            {
                // Trajectory prediction scaled by prediction accuracy
                float timeToReach = Mathf.Max(0.01f, (transform.position.y - ballPos.y) / Mathf.Max(0.1f, ballVel.y));
                float predictedX = ballPos.x + ballVel.x * (timeToReach * predictionAccuracy);
                _targetX = Mathf.Clamp(predictedX, minX, maxX);
            }
            else
            {
                // Idle return toward center
                _targetX = Mathf.Lerp(_targetX, 0f, Time.deltaTime * 2f);
            }

            // Move smoothly towards target X
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, _targetX, currentSpeed * Time.deltaTime);

            // Subtle floating oscillation
            pos.y = _basePosition.y + Mathf.Sin(Time.time * 3f) * 0.08f;
            transform.position = pos;
        }

        private void UpdateAim()
        {
            if (barrelPivot == null) return;

            Vector3 target = _activeBall != null ? _activeBall.transform.position : (_playerTarget != null ? _playerTarget.position : Vector3.zero);
            Vector3 diff = target - transform.position;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg - 90f;
            barrelPivot.rotation = Quaternion.Lerp(barrelPivot.rotation, Quaternion.Euler(0f, 0f, angle), Time.deltaTime * 10f);
        }

        private void CheckDeflection()
        {
            if (_activeBall == null || _isDefeated) return;

            Vector3 ballPos = _activeBall.transform.position;
            Vector3 ballVel = _activeBall.Velocity;

            // Ball must be moving upwards toward boss
            if (ballVel.y <= 0f) return;

            float bossY = transform.position.y - 0.7f; // Shield line
            if (Mathf.Abs(ballPos.y - bossY) <= 0.45f)
            {
                float diffX = ballPos.x - transform.position.x;
                if (Mathf.Abs(diffX) <= HalfShieldWidth + 0.35f)
                {
                    // Deflect downwards!
                    float hitOffset = Mathf.Clamp(diffX / HalfShieldWidth, -1f, 1f);
                    Vector2 dir = new Vector2(hitOffset * 0.95f, -1.0f).normalized;
                    _activeBall.OnDeflectedByBoss(dir);

                    // Recoil punch on shield
                    if (shieldVisual != null)
                    {
                        shieldVisual.localScale = new Vector3(_baseShieldScale.x * 1.2f, _baseShieldScale.y * 0.7f, _baseShieldScale.z);
                    }
                }
            }

            if (shieldVisual != null)
            {
                shieldVisual.localScale = Vector3.Lerp(shieldVisual.localScale, _baseShieldScale, Time.deltaTime * 10f);
            }
        }

        public void TakeDamage(int damage = 1)
        {
            if (_isDefeated || _isInvulnerable) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            HealthChanged?.Invoke(_currentHealth, maxHealth);

            // Spawn procedural cyber explosion only when playing
            if (Application.isPlaying)
            {
                BossExplosionVfx.Spawn(transform.position, new Color(0f, 1f, 0.8f), 18, 9f);
            }

            // Safe color flash
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                _flashCoroutine = StartCoroutine(DamageFlashRoutine());
            }

            if (_currentHealth <= 0)
            {
                _isDefeated = true;
                Defeated?.Invoke();
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            _isInvulnerable = true;

            // Recoil kickback
            Vector3 originalPos = transform.position;
            transform.position = originalPos + Vector3.up * 0.35f;

            Color origColor = GetColorSafe(bodyRenderer, Color.green);

            SetColorSafe(bodyRenderer, Color.white);
            if (barrelRenderer != null) SetColorSafe(barrelRenderer, Color.white);

            yield return new WaitForSeconds(0.12f);

            SetColorSafe(bodyRenderer, new Color(1f, 0.2f, 0.4f)); // Damage red
            if (barrelRenderer != null) SetColorSafe(barrelRenderer, new Color(1f, 0.2f, 0.4f));

            yield return new WaitForSeconds(0.12f);

            SetColorSafe(bodyRenderer, origColor);
            if (barrelRenderer != null) SetColorSafe(barrelRenderer, origColor);

            transform.position = originalPos;
            _isInvulnerable = false;
        }

        private IEnumerator DefeatSequence()
        {
            // Chain explosions
            for (int i = 0; i < 5; i++)
            {
                Vector3 randOffset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-0.8f, 0.8f), 0f);
                BossExplosionVfx.Spawn(transform.position + randOffset, new Color(0f, 1f, 0.9f), 14, 7f);
                yield return new WaitForSeconds(0.18f);
            }

            Defeated?.Invoke();
        }

        private static void SetColorSafe(Renderer r, Color col)
        {
            if (r == null || r.material == null) return;
            if (r.material.HasProperty("_Color")) r.material.color = col;
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", col);
        }

        private static Color GetColorSafe(Renderer r, Color defaultCol)
        {
            if (r == null || r.material == null) return defaultCol;
            if (r.material.HasProperty("_Color")) return r.material.color;
            if (r.material.HasProperty("_BaseColor")) return r.material.GetColor("_BaseColor");
            return defaultCol;
        }

        public void StopBehavior()
        {
            _isDefeated = true;
        }
    }
}
