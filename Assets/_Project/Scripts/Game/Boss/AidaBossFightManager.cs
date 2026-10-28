using System;
using System.Collections;
using AlgoRhythm.Game.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Master Manager for the Cyber Pong A.I.D.A. Boss Fight.
    /// Manages Dark Web Upload timer (5-minute countdown), Pong deflection physics,
    /// ball scoring, CRT HUD updates, developer cheats (F9/F10/F11), and victory/defeat states.
    /// </summary>
    public sealed class AidaBossFightManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float totalUploadTimeSeconds = 300f; // 5 Menit
        [SerializeField] private float missPenaltySeconds = 15f;      // Penalti jika balok lewat

        [Header("Scene References")]
        [SerializeField] private AidaBossController boss;
        [SerializeField] private PlayerPongPaddle playerPaddle;
        [SerializeField] private BossDataBlock ball;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private BossBootSequence bootSequence;
        [SerializeField] private AidaDeathCinematic deathCinematic;

        [Header("UI Text Displays")]
        [SerializeField] private TextMesh timerText;
        [SerializeField] private TextMesh bossHpText;
        [SerializeField] private TextMesh bannerText;
        [SerializeField] private TextMesh rallyText;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip deflectSfx;
        [SerializeField] private AudioClip hitBossSfx;
        [SerializeField] private AudioClip wallBounceSfx;
        [SerializeField] private AudioClip missSfx;
        [SerializeField] private AudioClip victorySfx;
        [SerializeField] private AudioClip defeatSfx;

        private float _elapsedTime;
        private bool _isGameOver;
        private bool _isVictory;
        private bool _battleStarted;
        private int _aidaLevel = 1;
        private Coroutine _serveCoroutine;

        public int AidaLevel => _aidaLevel;

        private void Awake()
        {
            Time.timeScale = 1f; // Ensure time is never paused/frozen
        }

        private void Start()
        {
            _aidaLevel = Mathf.Clamp(PlayerPrefs.GetInt("AlgoRhythm.AidaLevel", 1), 0, 5);

            switch (_aidaLevel)
            {
                case 0:
                    totalUploadTimeSeconds = 360f;
                    missPenaltySeconds = 8f;
                    break;
                case 1:
                    totalUploadTimeSeconds = 330f;
                    missPenaltySeconds = 10f;
                    break;
                case 2:
                    totalUploadTimeSeconds = 300f;
                    missPenaltySeconds = 13f;
                    break;
                case 3:
                    totalUploadTimeSeconds = 270f;
                    missPenaltySeconds = 16f;
                    break;
                case 4:
                    totalUploadTimeSeconds = 220f;
                    missPenaltySeconds = 20f;
                    break;
                case 5:
                default:
                    totalUploadTimeSeconds = 180f;
                    missPenaltySeconds = 25f;
                    break;
            }

            if (cameraShake == null && Camera.main != null)
            {
                cameraShake = Camera.main.GetComponent<CameraShake>();
            }

            if (bootSequence != null)
            {
                bootSequence.BootCompleted += OnBootCompleted;
                _battleStarted = false;
                SetCombatElementsActive(false);
            }
            else
            {
                StartBattle();
            }

            if (boss != null)
            {
                boss.ConfigureDifficulty(_aidaLevel);
                boss.HealthChanged += OnBossHealthChanged;
                boss.Defeated += OnBossDefeated;
                boss.Initialize(playerPaddle != null ? playerPaddle.transform : transform, ball);
            }

            if (PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerEnding", 0) == 1)
            {
                PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerEnding");
                if (bootSequence != null)
                {
                    bootSequence.gameObject.SetActive(false);
                }
                StartCoroutine(AutoTriggerEndingRoutine());
                return;
            }

            if (PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerDefeat", 0) == 1)
            {
                PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerDefeat");
                if (bootSequence != null)
                {
                    bootSequence.gameObject.SetActive(false);
                }
                StartCoroutine(AutoTriggerDefeatRoutine());
                return;
            }

            if (ball != null)
            {
                float baseSpeed = _aidaLevel <= 0 ? 6.5f : (_aidaLevel == 1 ? 7.0f : (_aidaLevel == 2 ? 8.0f : (_aidaLevel == 3 ? 9.0f : (_aidaLevel == 4 ? 10.2f : 11.5f))));
                float increment = _aidaLevel <= 0 ? 1.025f : (_aidaLevel == 1 ? 1.030f : (_aidaLevel == 2 ? 1.042f : (_aidaLevel == 3 ? 1.055f : (_aidaLevel == 4 ? 1.065f : 1.075f))));
                ball.ConfigureDifficulty(baseSpeed, increment);

                ball.PlayerScored += OnPlayerScored;
                ball.BossScored += OnBossScored;
                ball.WallBounced += OnWallBounced;
                ball.BossDeflected += OnBossDeflected;
            }

            if (bannerText != null)
            {
                bannerText.text = $"PANTULKAN BALOK DATA KE A.I.D.A v{_aidaLevel}.0!\n[MOUSE / ARROW KEYS: GERAKKAN PADDLE]";
            }

            UpdateDisplay();
        }

        private void OnBootCompleted()
        {
            SetCombatElementsActive(true);
            StartBattle();
        }

        private void SetCombatElementsActive(bool active)
        {
            if (timerText != null) timerText.gameObject.SetActive(active);
            if (bossHpText != null) bossHpText.gameObject.SetActive(active);
            if (rallyText != null) rallyText.gameObject.SetActive(active);
            if (bannerText != null) bannerText.gameObject.SetActive(active);
            if (boss != null) boss.gameObject.SetActive(active);
            if (playerPaddle != null) playerPaddle.gameObject.SetActive(active);
            if (ball != null) ball.gameObject.SetActive(active);
        }

        private void StartBattle()
        {
            _battleStarted = true;
            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            ServeBall(towardsPlayer: true, delay: 0.5f);
        }

        private void OnDestroy()
        {
            if (bootSequence != null) bootSequence.BootCompleted -= OnBootCompleted;
            if (boss != null)
            {
                boss.HealthChanged -= OnBossHealthChanged;
                boss.Defeated -= OnBossDefeated;
            }
            if (ball != null)
            {
                ball.PlayerScored -= OnPlayerScored;
                ball.BossScored -= OnBossScored;
                ball.WallBounced -= OnWallBounced;
                ball.BossDeflected -= OnBossDeflected;
            }
        }

        private void Update()
        {
            HandleCheats();

            if (!_battleStarted) return;

            if (_isGameOver || _isVictory)
            {
                HandleEndGameInput();
                return;
            }

            // Dark Web Upload Timer
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= totalUploadTimeSeconds)
            {
                TriggerDefeat();
                return;
            }

            // Check Player Deflection
            if (playerPaddle != null && ball != null && ball.gameObject.activeInHierarchy)
            {
                if (playerPaddle.TryDeflect(ball.transform.position, ball.Velocity, out Vector3 newVel, out float offset))
                {
                    ball.OnDeflectedByPlayer(newVel);
                    PlaySound(deflectSfx, 0.9f);
                    if (cameraShake != null) cameraShake.AddTrauma(0.12f);
                }
            }

            UpdateDisplay();
        }

        private void OnPlayerScored()
        {
            if (_isVictory || _isGameOver) return;

            if (boss != null)
            {
                boss.TakeDamage(1);
            }

            PlaySound(hitBossSfx, 1.0f);
            if (cameraShake != null) cameraShake.AddTrauma(0.6f);

            if (bannerText != null)
            {
                bannerText.text = ">> DIRECT HIT! A.I.D.A INTEGRITY DAMAGED! <<";
                bannerText.color = new Color(0f, 1f, 0.6f);
            }

            if (!_isVictory)
            {
                ServeBall(towardsPlayer: false, delay: 0.9f);
            }
        }

        private void OnBossScored()
        {
            if (_isVictory || _isGameOver) return;

            _elapsedTime = Mathf.Min(totalUploadTimeSeconds, _elapsedTime + missPenaltySeconds);
            PlaySound(missSfx != null ? missSfx : deflectSfx, 1.0f);
            if (cameraShake != null) cameraShake.AddTrauma(0.35f);

            if (bannerText != null)
            {
                bannerText.text = $">> PACKET MISSED! DARK WEB UPLOAD +{missPenaltySeconds:0}s! <<";
                bannerText.color = new Color(1f, 0.3f, 0.2f);
            }

            if (_elapsedTime >= totalUploadTimeSeconds)
            {
                TriggerDefeat();
            }
            else
            {
                ServeBall(towardsPlayer: true, delay: 0.9f);
            }
        }

        private void OnWallBounced(Vector3 bouncePos)
        {
            PlaySound(wallBounceSfx, 0.65f);
        }

        private void OnBossDeflected(float speed)
        {
            PlaySound(deflectSfx, 0.8f);
        }

        private void ServeBall(bool towardsPlayer, float delay)
        {
            if (_serveCoroutine != null) StopCoroutine(_serveCoroutine);
            _serveCoroutine = StartCoroutine(ServeRoutine(towardsPlayer, delay));
        }

        private IEnumerator ServeRoutine(bool towardsPlayer, float delay)
        {
            if (ball != null) ball.Stop();
            yield return new WaitForSeconds(delay);

            if (_isGameOver || _isVictory) yield break;

            if (ball != null)
            {
                ball.Serve(towardsPlayer, 1.0f);
            }
        }

        private void OnBossHealthChanged(int current, int max)
        {
            UpdateDisplay();
        }

        private IEnumerator AutoTriggerEndingRoutine()
        {
            yield return null;
            if (boss != null)
            {
                boss.TakeDamage(boss.CurrentHealth);
            }
            else
            {
                OnBossDefeated();
            }
        }

        private IEnumerator AutoTriggerDefeatRoutine()
        {
            yield return null;
            TriggerDefeat();
        }

        private void OnBossDefeated()
        {
            if (_isVictory) return;
            _isVictory = true;
            if (ball != null) ball.Stop();
            if (bgmSource != null) bgmSource.Stop();

            // Sembunyikan HUD pertempuran normal agar cinematic mendapatkan panggung penuh
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (bossHpText != null) bossHpText.gameObject.SetActive(false);
            if (rallyText != null) rallyText.gameObject.SetActive(false);
            if (bannerText != null) bannerText.gameObject.SetActive(false);

            Font fontToUse = timerText != null ? timerText.font : (bannerText != null ? bannerText.font : null);

            var cinematic = deathCinematic != null ? deathCinematic : GetComponentInChildren<AidaDeathCinematic>(true);
            if (cinematic == null)
            {
                cinematic = gameObject.AddComponent<AidaDeathCinematic>();
            }

            cinematic.Play(
                boss,
                cameraShake,
                sfxSource,
                hitBossSfx != null ? hitBossSfx : deflectSfx,
                victorySfx,
                _aidaLevel,
                Mathf.Max(0f, totalUploadTimeSeconds - _elapsedTime),
                fontToUse
            );
        }

        private void TriggerDefeat()
        {
            if (_isGameOver || _isVictory) return;
            _isGameOver = true;
            if (ball != null) ball.Stop();
            if (boss != null) boss.StopBehavior();
            if (bgmSource != null) bgmSource.Stop();
            PlaySound(defeatSfx, 1.0f);

            // Sembunyikan HUD pertempuran normal agar cinematic mendapatkan panggung penuh
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (bossHpText != null) bossHpText.gameObject.SetActive(false);
            if (rallyText != null) rallyText.gameObject.SetActive(false);
            if (bannerText != null) bannerText.gameObject.SetActive(false);

            Font fontToUse = timerText != null ? timerText.font : (bannerText != null ? bannerText.font : null);

            var cinematic = deathCinematic != null ? deathCinematic : GetComponentInChildren<AidaDeathCinematic>(true);
            if (cinematic == null)
            {
                cinematic = gameObject.AddComponent<AidaDeathCinematic>();
            }

            cinematic.PlayDefeat(
                boss,
                cameraShake,
                sfxSource,
                hitBossSfx != null ? hitBossSfx : deflectSfx,
                _aidaLevel,
                fontToUse
            );
        }

        public void DevTriggerDefeat()
        {
            TriggerDefeat();
        }

        private void UpdateDisplay()
        {
            // Update Timer Text
            if (timerText != null)
            {
                float remaining = Mathf.Max(0f, totalUploadTimeSeconds - _elapsedTime);
                int mins = Mathf.FloorToInt(remaining / 60f);
                int secs = Mathf.FloorToInt(remaining % 60f);
                float progress = Mathf.Clamp01(_elapsedTime / totalUploadTimeSeconds) * 100f;

                int barLength = 20;
                int filled = Mathf.RoundToInt((progress / 100f) * barLength);
                string bar = new string('█', filled) + new string('░', barLength - filled);

                timerText.text = $"DARK WEB UPLOAD: [{bar}] {progress:0}%\nWAKTU TERSISA: {mins:00}:{secs:00}";
            }

            // Update Boss HP Text
            if (bossHpText != null && boss != null)
            {
                int hp = boss.CurrentHealth;
                int max = boss.MaxHealth;
                string hpBar = new string('■', hp) + new string('□', max - hp);
                string phase = hp <= (max / 2) ? "[OVERCLOCK RAGE]" : (_aidaLevel <= 1 ? "[EASY MODE]" : "[DEFENSE CORE]");
                bossHpText.text = $"A.I.D.A v{_aidaLevel}.0 {phase}: [{hpBar}] {hp}/{max} HP";
            }

            // Update Rally / Speed Text
            if (rallyText != null && ball != null)
            {
                rallyText.text = $"RALLY: {ball.RallyCount}x   ·   SPEED: {ball.CurrentSpeed:0.0} m/s";
            }
        }

        private void HandleEndGameInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Navigasi saat victory maupun defeat dikendalikan penuh oleh AidaDeathCinematic jika aktif
            var cinematic = deathCinematic != null ? deathCinematic : GetComponentInChildren<AidaDeathCinematic>(true);
            if (cinematic != null && cinematic.IsPlaying)
            {
                return;
            }

            if (_isGameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        private void HandleCheats()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Cheat F8: Instant Defeat / Trigger Defeat Ending
            if (keyboard.f8Key.wasPressedThisFrame && !_isGameOver && !_isVictory)
            {
                TriggerDefeat();
            }

            // Cheat F10: Kurangi HP Boss instan (-2 HP per tekan)
            if (keyboard.f10Key.wasPressedThisFrame && boss != null && !_isGameOver && !_isVictory)
            {
                boss.TakeDamage(2);
            }

            // Cheat F9 / F12: Instant Victory / Trigger Magnum Opus Ending
            if ((keyboard.f9Key.wasPressedThisFrame || keyboard.f12Key.wasPressedThisFrame) && !_isGameOver && !_isVictory)
            {
                if (boss != null)
                {
                    boss.TakeDamage(boss.CurrentHealth);
                }
                else
                {
                    OnBossDefeated();
                }
            }

            // Cheat F11: Percepat waktu (+60 detik per tekan)
            if (keyboard.f11Key.wasPressedThisFrame && !_isGameOver && !_isVictory)
            {
                _elapsedTime = Mathf.Min(totalUploadTimeSeconds, _elapsedTime + 60f);
            }
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
