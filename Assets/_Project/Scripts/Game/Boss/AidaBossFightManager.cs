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
        private Coroutine _serveCoroutine;

        private void Awake()
        {
            Time.timeScale = 1f; // Ensure time is never paused/frozen
        }

        private void Start()
        {
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
                boss.HealthChanged += OnBossHealthChanged;
                boss.Defeated += OnBossDefeated;
                boss.Initialize(playerPaddle != null ? playerPaddle.transform : transform, ball);
            }

            if (ball != null)
            {
                ball.PlayerScored += OnPlayerScored;
                ball.BossScored += OnBossScored;
                ball.WallBounced += OnWallBounced;
                ball.BossDeflected += OnBossDeflected;
            }

            if (bannerText != null)
            {
                bannerText.text = "PANTULKAN BALOK DATA KE A.I.D.A!\n[MOUSE / ARROW KEYS: GERAKKAN PADDLE]";
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
                ball.Serve(towardsPlayer);
            }
        }

        private void OnBossHealthChanged(int current, int max)
        {
            UpdateDisplay();
        }

        private void OnBossDefeated()
        {
            _isVictory = true;
            if (ball != null) ball.Stop();
            if (bgmSource != null) bgmSource.Stop();
            PlaySound(victorySfx, 1.0f);

            if (bannerText != null)
            {
                bannerText.text = "VICTORY! A.I.D.A BERHASIL DIKALAHKAN!\nCORE DATABASE BERHASIL DIREBUT KEMBALI!\n[ENTER] KEMBALI KE MAIN MENU";
                bannerText.color = new Color(0f, 1f, 0.5f); // Emerald Neon
            }
        }

        private void TriggerDefeat()
        {
            _isGameOver = true;
            if (ball != null) ball.Stop();
            if (boss != null) boss.StopBehavior();
            if (bgmSource != null) bgmSource.Stop();
            PlaySound(defeatSfx, 1.0f);

            if (bannerText != null)
            {
                bannerText.text = "SYSTEM COMPROMISED!\nCORE DATABASE BERHASIL DIJUAL KE DARK WEB!\n[R] COBA LAGI   ·   [ESC] MENU UTAMA";
                bannerText.color = new Color(1f, 0.2f, 0.2f); // Red Alert
            }
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
                string phase = hp <= 4 ? "[OVERCLOCK RAGE]" : "[DEFENSE CORE]";
                bossHpText.text = $"A.I.D.A v5.0 {phase}: [{hpBar}] {hp}/{max} HP";
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

            if (_isVictory)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
            else if (_isGameOver)
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

            // Cheat F10: Kurangi HP Boss instan (-2 HP per tekan)
            if (keyboard.f10Key.wasPressedThisFrame && boss != null && !_isGameOver && !_isVictory)
            {
                boss.TakeDamage(2);
            }

            // Cheat F9: Instant Victory
            if (keyboard.f9Key.wasPressedThisFrame && boss != null && !_isGameOver && !_isVictory)
            {
                boss.TakeDamage(boss.CurrentHealth);
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
