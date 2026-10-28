#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AlgoRhythm.Game.Bootstrap;
using AlgoRhythm.Game.Combat;

namespace AlgoRhythm.EditorTools
{
    public static class DevCheatsMenu
    {
        [MenuItem("Tools/Cheats/Skip 1 Wave + 999 PTS (F8)", false, 1)]
        public static void SkipSingleWaveWithPts()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null && bootstrap.Controller != null)
            {
                bootstrap.Controller.DevSkipSingleWave(999);
                Debug.Log("[DevCheats] Skip 1 wave + 999 PTS berhasil dijalankan!");
            }
            else
            {
                Debug.LogWarning("[DevCheats] GameBootstrap atau Controller tidak ditemukan di scene saat ini.");
            }
        }

        [MenuItem("Tools/Cheats/Tambah 999 PTS (Uang Banyak) (F7)", false, 2)]
        public static void AddBonusPts()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null && bootstrap.Controller != null)
            {
                bootstrap.Controller.DevAddPts(999);
                Debug.Log("[DevCheats] +999 PTS berhasil ditambahkan!");
            }
            else
            {
                Debug.LogWarning("[DevCheats] GameBootstrap atau Controller tidak ditemukan di scene saat ini.");
            }
        }

        [MenuItem("Tools/Cheats/Skip Semua Waves (Ke Outro) (F9)", false, 3)]
        public static void SkipAllWaves()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null && bootstrap.Controller != null)
            {
                bootstrap.Controller.DevFinishAllWaves();
                Debug.Log("[DevCheats] Seluruh wave berhasil di-skip menuju Outro!");
            }
            else
            {
                Debug.LogWarning("[DevCheats] GameBootstrap atau Controller tidak ditemukan di scene saat ini.");
            }
        }

        [MenuItem("Tools/Cheats/Lompat Ke Boss Fight AIDA (F10)", false, 4)]
        public static void JumpToBoss()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
        }

        [MenuItem("Tools/Cheats/Langsung Skip Ke Magnum Opus Ending (F12)", false, 0)]
        public static void TriggerMagnumOpusEnding()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            var boss = Object.FindFirstObjectByType<AlgoRhythm.Game.Boss.AidaBossController>();
            if (boss != null)
            {
                boss.TakeDamage(boss.CurrentHealth);
                Debug.Log("[DevCheats] AIDA berhasil dikalahkan, Magnum Opus Ending dimulai!");
                return;
            }

            // Jika di SampleScene atau scene lain, set auto-trigger dan muat BossFightScene
            PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerEnding", 1);
            UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
            Debug.Log("[DevCheats] Memuat BossFightScene dan langsung memicu Magnum Opus Ending!");
        }

        [MenuItem("Tools/Cheats/Picu Ending Kalah A.I.D.A (Defeat Taunt) (F8 di BossFightScene)", false, 5)]
        public static void TriggerDefeatEnding()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DevCheats] Cheat hanya dapat dijalankan saat Play Mode aktif!");
                return;
            }

            var bossManager = Object.FindFirstObjectByType<AlgoRhythm.Game.Boss.AidaBossFightManager>();
            if (bossManager != null)
            {
                bossManager.DevTriggerDefeat();
                Debug.Log("[DevCheats] Ending kalah A.I.D.A berhasil dipicu!");
                return;
            }

            // Jika di SampleScene atau scene lain, set auto-trigger defeat dan muat BossFightScene
            PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerDefeat", 1);
            UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
            Debug.Log("[DevCheats] Memuat BossFightScene dan langsung memicu ending kalah A.I.D.A!");
        }
    }
}
#endif
