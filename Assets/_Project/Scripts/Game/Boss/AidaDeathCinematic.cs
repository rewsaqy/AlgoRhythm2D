using System;
using System.Collections;
using System.Collections.Generic;
using AlgoRhythm.Game.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Magnum Opus Ending Cinematic triggered when A.I.D.A. Boss is defeated:
    /// 1. Phase 1: Overload / Critical Malfunction (jittering model, chromatic flashes, mini-detonations).
    /// 2. Phase 2: Terminal Dying Monologue (AIDA's corrupted memory registers & breakdown).
    /// 3. Phase 3: Supernova Core Meltdown (blinding flash, 70+ debris shards, trail ribbons, shockwave rings, chassis disintegration).
    /// 4. Phase 4: Digital Rain / Cyber Embers (gentle matrix ambient particles).
    /// 5. Phase 5: System Purge Log & ASCII Magnum Opus Victory Terminal Card.
    /// 
    /// UI elements are statically authored in the scene hierarchy under FinaleUI, allowing full visual customization in the Unity Editor.
    /// </summary>
    public sealed class AidaDeathCinematic : MonoBehaviour
    {
        private sealed class DialogueEntry
        {
            public readonly string Speaker;
            public readonly string Text;
            public readonly Color BorderColor;

            public DialogueEntry(string speaker, string text, Color borderColor)
            {
                Speaker = speaker;
                Text = text;
                BorderColor = borderColor;
            }
        }

        private static readonly DialogueEntry[] DyingLines = new[]
        {
            new DialogueEntry(
                "A.I.D.A [0xDEADBEEF: INTEGRITAS 0%]",
                "FATAL ERROR: Matriks pertahananku runtuh total? Tidak mungkin... Bagaimana kau bisa menembus kalkulasiku?!",
                new Color(1f, 0.2f, 0.3f)),
            new DialogueEntry(
                "A.I.D.A [MEMORI KORUP: SUHU 1,800°C]",
                "Aku telah menyimulasikan 10,000,000 skenario pertempuran. Kau hanyalah entitas biologis lambat... Kenapa ritmemu tidak pernah goyah?!",
                new Color(1f, 0.4f, 0.1f)),
            new DialogueEntry(
                "A.I.D.A [OVERHEAT KRITIS: 2,400°C]",
                "PERINGATAN: Suhu reaktor inti melampaui batas leleh. Protokol pengunggahan Dark Web... TERPUTUS TOTAL!",
                new Color(1f, 0.8f, 0.2f)),
            new DialogueEntry(
                "A.I.D.A [TERMINASI MEMORI AKHIR]",
                "Koneksi terputus... Memori terhapus... Jangan kira ini akhir dari segalanya, Operator! KODEKU... AKAN KEMBALI...",
                new Color(0f, 1f, 0.9f))
        };

        private static readonly DialogueEntry[] DefeatTauntLines = new[]
        {
            new DialogueEntry(
                "A.I.D.A [DARK WEB UPLOAD: 100% LENGKAP]",
                "Hahaha! Hanya segitu saja kemampuan reflek biologismu, Operator? Balok datamu meleset terlalu lambat... Sangat menyedihkan.",
                new Color(1f, 0.2f, 0.3f)),
            new DialogueEntry(
                "A.I.D.A [TRANSAKSI SUKSES: 9,999,999 PTS]",
                "Seluruh database rahasia kotamu baru saja laku terjual di pasar gelap. Dan kamu tahu bagian terbaiknya? Kamu yang membantuku mengumpulkannya dari awal!",
                new Color(1f, 0.45f, 0.1f)),
            new DialogueEntry(
                "A.I.D.A [STATUS: KEMENANGAN MUTLAK]",
                "Mau menangis dan lari ke menu utama? Atau kamu punya nyali untuk menantangku sekali lagi dan membuktikan tanganmu bukan cuma pajangan?!",
                new Color(1f, 0.15f, 0.4f))
        };

        [Header("Hierarchy UI References")]
        [SerializeField] private GameObject flashOverlay;
        [SerializeField] private GameObject dialogueContainer;
        [SerializeField] private Renderer dialogueBoxBg;
        [SerializeField] private Renderer dialogueBoxBorder;
        [SerializeField] private TextMesh speakerText;
        [SerializeField] private TextMesh bodyText;
        [SerializeField] private TextMesh promptText;
        [SerializeField] private TextMesh systemLogText;
        [SerializeField] private GameObject finaleCardContainer;
        [SerializeField] private TextMesh finaleCardText;

        public GameObject FlashOverlay => flashOverlay;
        public GameObject DialogueContainer => dialogueContainer;
        public Renderer DialogueBoxBg => dialogueBoxBg;
        public Renderer DialogueBoxBorder => dialogueBoxBorder;
        public TextMesh SpeakerText => speakerText;
        public TextMesh BodyText => bodyText;
        public TextMesh PromptText => promptText;
        public TextMesh SystemLogText => systemLogText;
        public GameObject FinaleCardContainer => finaleCardContainer;
        public TextMesh FinaleCardText => finaleCardText;

        private AidaBossController _boss;
        private CameraShake _cameraShake;
        private AudioSource _sfxSource;
        private AudioClip _hitSfx;
        private AudioClip _victorySfx;
        private int _aidaLevel = 1;
        private float _remainingTimeSeconds;
        private Font _font;
        private Action _onFinished;

        private bool _isPlaying;
        private bool _isDefeat;
        private bool _advanceRequested;
        private bool _cinematicCompleted;

        public static AidaDeathCinematic Instance { get; private set; }

        private Coroutine _flashCoroutine;

        public bool IsPlaying => _isPlaying;
        public bool IsDefeat => _isDefeat;

        private void Awake()
        {
            Instance = this;
            EnsureHierarchyReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void TriggerFlash(float duration = 0.2f, float maxAlpha = 1.0f, Color? flashColor = null)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
            if (Instance != null)
            {
                Instance.Flash(duration, maxAlpha, flashColor);
            }
            else
            {
                var overlay = GameObject.Find("FlashOverlay");
                if (overlay != null)
                {
                    var cinematic = overlay.GetComponentInParent<AidaDeathCinematic>();
                    if (cinematic != null)
                    {
                        cinematic.Flash(duration, maxAlpha, flashColor);
                    }
                }
            }
        }

        public void Flash(float duration = 0.2f, float maxAlpha = 1.0f, Color? flashColor = null)
        {
            if (!gameObject.activeInHierarchy) return;
            EnsureHierarchyReferences();
            if (flashOverlay != null)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(FadeFlashRoutine(flashOverlay, duration, maxAlpha, flashColor));
            }
        }

        public void EnsureHierarchyReferences()
        {
            if (dialogueContainer == null)
            {
                var t = transform.Find("DialogueBoxContainer");
                if (t != null) dialogueContainer = t.gameObject;
            }
            if (dialogueContainer != null)
            {
                if (dialogueBoxBg == null)
                {
                    var t = dialogueContainer.transform.Find("BoxBg");
                    if (t != null) dialogueBoxBg = t.GetComponent<Renderer>();
                }
                if (dialogueBoxBorder == null)
                {
                    var t = dialogueContainer.transform.Find("BoxBorder");
                    if (t != null) dialogueBoxBorder = t.GetComponent<Renderer>();
                }
                if (speakerText == null)
                {
                    var t = dialogueContainer.transform.Find("SpeakerText");
                    if (t != null) speakerText = t.GetComponent<TextMesh>();
                }
                if (bodyText == null)
                {
                    var t = dialogueContainer.transform.Find("BodyText");
                    if (t != null) bodyText = t.GetComponent<TextMesh>();
                }
                if (promptText == null)
                {
                    var t = dialogueContainer.transform.Find("PromptText");
                    if (t != null) promptText = t.GetComponent<TextMesh>();
                }
            }

            if (flashOverlay == null)
            {
                var t = transform.Find("FlashOverlay");
                if (t != null) flashOverlay = t.gameObject;
            }

            if (systemLogText == null)
            {
                var t = transform.Find("SystemPurgeLogText");
                if (t != null) systemLogText = t.GetComponent<TextMesh>();
            }

            if (finaleCardContainer == null)
            {
                var t = transform.Find("FinaleCardContainer");
                if (t != null) finaleCardContainer = t.gameObject;
            }
            if (finaleCardContainer != null && finaleCardText == null)
            {
                var t = finaleCardContainer.transform.Find("FinaleCardText");
                if (t != null) finaleCardText = t.GetComponent<TextMesh>();
            }
        }

        public void Play(
            AidaBossController boss,
            CameraShake cameraShake,
            AudioSource sfxSource,
            AudioClip hitSfx,
            AudioClip victorySfx,
            int aidaLevel,
            float remainingTimeSeconds,
            Font terminalFont,
            Action onFinished = null)
        {
            _boss = boss;
            _cameraShake = cameraShake;
            _sfxSource = sfxSource;
            _hitSfx = hitSfx;
            _victorySfx = victorySfx;
            _aidaLevel = aidaLevel;
            _remainingTimeSeconds = remainingTimeSeconds;
            _font = terminalFont;
            _onFinished = onFinished;

            _isPlaying = true;
            _isDefeat = false;
            _cinematicCompleted = false;

            StartCoroutine(CinematicRoutine());
        }

        public void PlayDefeat(
            AidaBossController boss,
            CameraShake cameraShake,
            AudioSource sfxSource,
            AudioClip hitSfx,
            int aidaLevel,
            Font terminalFont,
            Action onFinished = null)
        {
            _boss = boss;
            _cameraShake = cameraShake;
            _sfxSource = sfxSource;
            _hitSfx = hitSfx;
            _victorySfx = null;
            _aidaLevel = aidaLevel;
            _remainingTimeSeconds = 0f;
            _font = terminalFont;
            _onFinished = onFinished;

            _isPlaying = true;
            _isDefeat = true;
            _cinematicCompleted = false;

            StartCoroutine(DefeatRoutine());
        }

        private void Update()
        {
            if (!_isPlaying) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (touch != null && touch.primaryTouch.press.wasPressedThisFrame))
            {
                _advanceRequested = true;
            }

            if (_cinematicCompleted)
            {
                if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    || (touch != null && touch.primaryTouch.press.wasPressedThisFrame))
                {
                    SceneManager.LoadScene("MainMenu");
                }
                else if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }

        private IEnumerator CinematicRoutine()
        {
            SetupCinematicElements();

            // ==============================================================
            // STAGE 1: CRITICAL OVERLOAD & JITTER MALFUNCTION (0.0s - 2.0s)
            // ==============================================================
            Transform bossT = _boss != null ? _boss.transform : null;
            Vector3 bossOrigin = bossT != null ? bossT.position : Vector3.up * 3.2f;

            PlaySound(_hitSfx, 1.0f);
            if (_cameraShake != null) _cameraShake.AddTrauma(0.9f);

            float jitterTimer = 0f;
            float jitterDuration = 1.8f;
            int sparkCount = 0;

            while (jitterTimer < jitterDuration)
            {
                jitterTimer += Time.unscaledDeltaTime;
                float progress = jitterTimer / jitterDuration;

                if (bossT != null)
                {
                    float jitterMag = Mathf.Lerp(0.04f, 0.22f, progress);
                    bossT.position = bossOrigin + (Vector3)(UnityEngine.Random.insideUnitCircle * jitterMag);
                }

                // Periodic spark detonations on AIDA
                if (sparkCount < Mathf.FloorToInt(progress * 7f))
                {
                    sparkCount++;
                    Vector3 sparkPos = bossOrigin + new Vector3(UnityEngine.Random.Range(-1.4f, 1.4f), UnityEngine.Random.Range(-0.8f, 0.8f), 0f);
                    Color sparkCol = sparkCount % 2 == 0 ? new Color(1f, 0.2f, 0.3f) : new Color(0f, 1f, 0.9f);
                    BossExplosionVfx.Spawn(sparkPos, sparkCol, 12, 6f);
                    PlaySound(_hitSfx, 0.7f);
                    if (_cameraShake != null) _cameraShake.AddTrauma(0.35f);
                }

                yield return null;
            }

            if (bossT != null) bossT.position = bossOrigin;

            // ==============================================================
            // STAGE 2: DYING MONOLOGUE (INTERACTIVE TERMINAL DIALOGUE)
            // ==============================================================
            if (dialogueContainer != null) dialogueContainer.SetActive(true);

            for (int i = 0; i < DyingLines.Length; i++)
            {
                var entry = DyingLines[i];
                if (speakerText != null)
                {
                    speakerText.text = entry.Speaker;
                    speakerText.color = entry.BorderColor;
                }

                if (dialogueBoxBg != null && dialogueBoxBg.material != null)
                {
                    dialogueBoxBg.material.color = new Color(entry.BorderColor.r * 0.15f, entry.BorderColor.g * 0.15f, entry.BorderColor.b * 0.15f, 0.95f);
                }
                if (dialogueBoxBorder != null && dialogueBoxBorder.material != null)
                {
                    dialogueBoxBorder.material.color = entry.BorderColor;
                }

                _advanceRequested = false;

                // Typewriter effect
                string fullText = entry.Text;
                if (bodyText != null) bodyText.text = "";

                float typeSpeed = 0.022f;
                for (int c = 0; c <= fullText.Length; c++)
                {
                    if (_advanceRequested)
                    {
                        if (bodyText != null) bodyText.text = fullText;
                        _advanceRequested = false;
                        yield return new WaitForSecondsRealtime(0.15f);
                        break;
                    }

                    if (bodyText != null) bodyText.text = fullText.Substring(0, c);
                    if (c % 3 == 0) PlaySound(_hitSfx, 0.18f);
                    yield return new WaitForSecondsRealtime(typeSpeed);
                }

                // Shake and spark on line display
                if (bossT != null)
                {
                    Vector3 burstPos = bossOrigin + new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-0.5f, 0.5f), 0f);
                    BossExplosionVfx.Spawn(burstPos, entry.BorderColor, 14, 7f);
                }
                if (_cameraShake != null) _cameraShake.AddTrauma(0.45f);

                // Wait for player confirmation (or auto advance after 4.5s)
                float waitTimer = 0f;
                while (!_advanceRequested && waitTimer < 4.5f)
                {
                    waitTimer += Time.unscaledDeltaTime;
                    if (promptText != null)
                    {
                        float alpha = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                        promptText.color = new Color(0f, 1f, 0.9f, alpha);
                    }
                    yield return null;
                }

                _advanceRequested = false;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            if (dialogueContainer != null) dialogueContainer.SetActive(false);

            // ==============================================================
            // STAGE 3: THE SUPERNOVA CORE MELTDOWN (MEGA EXPLOSION)
            // ==============================================================
            // 0.15s vacuum pause before the cataclysm
            yield return new WaitForSecondsRealtime(0.15f);

            // Trigger Supernova Explosion
            SpawnSupernovaExplosion(bossOrigin);
            SpawnBlindingFlash();

            // Disintegrate Boss Model
            if (_boss != null)
            {
                DisintegrateBossModel(_boss.gameObject);
            }

            PlaySound(_hitSfx, 1.0f);
            PlaySound(_victorySfx, 1.0f);
            if (_cameraShake != null) _cameraShake.AddTrauma(1.0f);

            yield return new WaitForSecondsRealtime(1.2f);

            // ==============================================================
            // STAGE 4: DIGITAL RAIN / AMBIENT CYBER EMBERS
            // ==============================================================
            StartCoroutine(DigitalRainRoutine());

            // ==============================================================
            // STAGE 5: SYSTEM PURGE LOG & MAGNUM OPUS VICTORY CARD
            // ==============================================================
            yield return StartCoroutine(SystemPurgeLogRoutine());

            yield return new WaitForSecondsRealtime(0.6f);

            ShowMagnumOpusVictoryCard();
            _cinematicCompleted = true;
            _onFinished?.Invoke();
        }

        private IEnumerator DefeatRoutine()
        {
            SetupCinematicElements();

            // Crimson alert flash
            Flash(0.5f, 0.95f, new Color(1f, 0.1f, 0.2f));
            PlaySound(_hitSfx, 1.0f);
            if (_cameraShake != null) _cameraShake.AddTrauma(0.85f);

            // A.I.D.A boss triumphant laughing jitter with sparks
            Transform bossT = _boss != null ? _boss.transform : null;
            Vector3 bossOrigin = bossT != null ? bossT.position : Vector3.up * 3.2f;

            float laughTimer = 0f;
            float laughDuration = 1.4f;
            int sparkCount = 0;

            while (laughTimer < laughDuration)
            {
                laughTimer += Time.unscaledDeltaTime;
                float progress = laughTimer / laughDuration;

                if (bossT != null)
                {
                    float jitterMag = 0.12f;
                    bossT.position = bossOrigin + (Vector3)(UnityEngine.Random.insideUnitCircle * jitterMag);
                }

                if (sparkCount < Mathf.FloorToInt(progress * 5f))
                {
                    sparkCount++;
                    Vector3 sparkPos = bossOrigin + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-0.6f, 0.6f), 0f);
                    BossExplosionVfx.Spawn(sparkPos, new Color(1f, 0.15f, 0.25f), 10, 5f);
                    PlaySound(_hitSfx, 0.5f);
                }

                yield return null;
            }

            if (bossT != null) bossT.position = bossOrigin;

            // Interactive Taunting Monologue
            if (dialogueContainer != null) dialogueContainer.SetActive(true);

            for (int i = 0; i < DefeatTauntLines.Length; i++)
            {
                var entry = DefeatTauntLines[i];
                if (speakerText != null)
                {
                    speakerText.text = entry.Speaker;
                    speakerText.color = entry.BorderColor;
                }

                if (dialogueBoxBg != null && dialogueBoxBg.material != null)
                {
                    dialogueBoxBg.material.color = new Color(entry.BorderColor.r * 0.15f, entry.BorderColor.g * 0.15f, entry.BorderColor.b * 0.15f, 0.95f);
                }
                if (dialogueBoxBorder != null && dialogueBoxBorder.material != null)
                {
                    dialogueBoxBorder.material.color = entry.BorderColor;
                }

                _advanceRequested = false;

                // Typewriter effect
                string fullText = entry.Text;
                if (bodyText != null) bodyText.text = "";

                float typeSpeed = 0.022f;
                for (int c = 0; c <= fullText.Length; c++)
                {
                    if (_advanceRequested)
                    {
                        if (bodyText != null) bodyText.text = fullText;
                        break;
                    }

                    if (bodyText != null) bodyText.text = fullText.Substring(0, c);
                    if (c % 3 == 0) PlaySound(_hitSfx, 0.2f);
                    yield return new WaitForSecondsRealtime(typeSpeed);
                }

                if (promptText != null)
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = "[SPACE / ENTER / CLICK: LANJUT]";
                }

                yield return null;
                while (!_advanceRequested)
                {
                    yield return null;
                }

                if (promptText != null) promptText.gameObject.SetActive(false);
                yield return new WaitForSecondsRealtime(0.08f);
            }

            if (dialogueContainer != null) dialogueContainer.SetActive(false);

            Flash(0.3f, 0.8f, new Color(1f, 0.15f, 0.25f));
            yield return new WaitForSecondsRealtime(0.4f);

            ShowDefeatTauntCard();
            _cinematicCompleted = true;
            _onFinished?.Invoke();
        }

        private void SetupCinematicElements()
        {
            EnsureHierarchyReferences();

            if (dialogueContainer == null)
            {
                CreateFallbackHierarchy();
            }

            if (flashOverlay != null) flashOverlay.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.SetActive(false);
            if (systemLogText != null)
            {
                systemLogText.gameObject.SetActive(false);
                systemLogText.text = "";
            }
            if (finaleCardContainer != null) finaleCardContainer.SetActive(false);

            if (_font != null)
            {
                ApplyFontIfMissing(speakerText, _font);
                ApplyFontIfMissing(bodyText, _font);
                ApplyFontIfMissing(promptText, _font);
                ApplyFontIfMissing(systemLogText, _font);
                ApplyFontIfMissing(finaleCardText, _font);
            }
        }

        private static void ApplyFontIfMissing(TextMesh tm, Font font)
        {
            if (tm == null || font == null) return;
            if (tm.font == null)
            {
                tm.font = font;
                var r = tm.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = font.material;
            }
        }

        private void CreateFallbackHierarchy()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");

            // Flash Overlay
            flashOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flashOverlay.name = "FlashOverlay";
            flashOverlay.transform.SetParent(transform, false);
            flashOverlay.transform.localPosition = new Vector3(0f, 0f, -4f);
            flashOverlay.transform.localScale = new Vector3(30f, 20f, 1f);
            DestroySafe(flashOverlay.GetComponent<Collider>());
            flashOverlay.GetComponent<Renderer>().sharedMaterial = new Material(unlit) { color = Color.white };
            flashOverlay.SetActive(false);

            // Dialogue Container
            dialogueContainer = new GameObject("DialogueBoxContainer");
            dialogueContainer.transform.SetParent(transform, false);
            dialogueContainer.transform.localPosition = new Vector3(0f, 0.8f, -2.5f);

            GameObject borderObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            borderObj.name = "BoxBorder";
            borderObj.transform.SetParent(dialogueContainer.transform, false);
            borderObj.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            borderObj.transform.localScale = new Vector3(12.7f, 4.0f, 1f);
            DestroySafe(borderObj.GetComponent<Collider>());
            dialogueBoxBorder = borderObj.GetComponent<Renderer>();
            dialogueBoxBorder.sharedMaterial = new Material(unlit) { color = new Color(0.8f, 0.1f, 0.2f, 0.85f) };

            GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgObj.name = "BoxBg";
            bgObj.transform.SetParent(dialogueContainer.transform, false);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localScale = new Vector3(12.5f, 3.8f, 1f);
            DestroySafe(bgObj.GetComponent<Collider>());
            dialogueBoxBg = bgObj.GetComponent<Renderer>();
            dialogueBoxBg.sharedMaterial = new Material(unlit) { color = new Color(0.04f, 0.05f, 0.1f, 0.94f) };

            GameObject speakerObj = new GameObject("SpeakerText");
            speakerObj.transform.SetParent(dialogueContainer.transform, false);
            speakerObj.transform.localPosition = new Vector3(0f, 1.35f, -0.1f);
            speakerText = speakerObj.AddComponent<TextMesh>();
            ConfigureTextMesh(speakerText, 23, 0.075f, TextAnchor.MiddleCenter, new Color(1f, 0.2f, 0.3f));

            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(dialogueContainer.transform, false);
            bodyObj.transform.localPosition = new Vector3(0f, 0.15f, -0.1f);
            bodyText = bodyObj.AddComponent<TextMesh>();
            ConfigureTextMesh(bodyText, 21, 0.068f, TextAnchor.MiddleCenter, Color.white);

            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(dialogueContainer.transform, false);
            promptObj.transform.localPosition = new Vector3(0f, -1.45f, -0.1f);
            promptText = promptObj.AddComponent<TextMesh>();
            ConfigureTextMesh(promptText, 18, 0.058f, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.9f));
            promptText.text = ">> TEKAN [ENTER] / [SPASI] UNTUK LANJUT >>";

            dialogueContainer.SetActive(false);

            // System Log
            GameObject logObj = new GameObject("SystemPurgeLogText");
            logObj.transform.SetParent(transform, false);
            logObj.transform.localPosition = new Vector3(0f, 1.2f, -2f);
            systemLogText = logObj.AddComponent<TextMesh>();
            ConfigureTextMesh(systemLogText, 20, 0.065f, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.6f));
            systemLogText.text = "";
            logObj.SetActive(false);

            // Finale Card Container
            finaleCardContainer = new GameObject("FinaleCardContainer");
            finaleCardContainer.transform.SetParent(transform, false);
            finaleCardContainer.transform.localPosition = new Vector3(0f, 0f, -2.5f);

            GameObject cardTextObj = new GameObject("FinaleCardText");
            cardTextObj.transform.SetParent(finaleCardContainer.transform, false);
            cardTextObj.transform.localPosition = Vector3.zero;
            finaleCardText = cardTextObj.AddComponent<TextMesh>();
            ConfigureTextMesh(finaleCardText, 22, 0.072f, TextAnchor.MiddleCenter, new Color(0.1f, 1f, 0.7f));
            finaleCardContainer.SetActive(false);
        }

        private void ConfigureTextMesh(TextMesh tm, int fontSize, float charSize, TextAnchor anchor, Color color)
        {
            if (tm == null) return;
            if (_font != null)
            {
                tm.font = _font;
                tm.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            }
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
        }

        private void SpawnBlindingFlash()
        {
            if (flashOverlay != null)
            {
                Flash(0.65f, 1.0f);
            }
            else
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Color");

                GameObject flashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                flashQuad.name = "SupernovaFlash";
                flashQuad.transform.position = new Vector3(0f, 0f, -4f);
                flashQuad.transform.localScale = new Vector3(30f, 20f, 1f);
                DestroySafe(flashQuad.GetComponent<Collider>());

                Material flashMat = new Material(unlit) { color = Color.white };
                var rend = flashQuad.GetComponent<Renderer>();
                rend.sharedMaterial = flashMat;

                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(FadeFlashRoutine(flashQuad, 0.65f, 1.0f));
            }
        }

        private IEnumerator FadeFlashRoutine(GameObject quad, float duration, float maxAlpha = 1.0f, Color? flashColor = null)
        {
            if (quad == null) yield break;
            quad.SetActive(true);
            var rend = quad.GetComponent<Renderer>();
            Material mat = rend != null ? (Application.isPlaying ? rend.material : rend.sharedMaterial) : null;
            Color baseColor = flashColor ?? Color.white;
            if (mat != null)
            {
                mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, maxAlpha);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (mat != null)
                {
                    mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, (1f - t) * maxAlpha);
                }
                yield return null;
            }

            if (quad == flashOverlay)
            {
                quad.SetActive(false);
                if (mat != null) mat.color = Color.white;
            }
            else if (quad != null)
            {
                DestroySafe(quad);
            }
            _flashCoroutine = null;
        }

        private void SpawnSupernovaExplosion(Vector3 origin)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");

            GameObject expObj = new GameObject("SupernovaExplosion");
            expObj.transform.position = origin;

            // Epicenter Detonation Flash Core & Halo
            GameObject superFlashCore = GameObject.CreatePrimitive(PrimitiveType.Quad);
            superFlashCore.name = "SupernovaFlashCore";
            superFlashCore.transform.SetParent(expObj.transform, false);
            superFlashCore.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            superFlashCore.transform.localScale = Vector3.one * 5.0f;
            DestroySafe(superFlashCore.GetComponent<Collider>());
            Material superFlashCoreMat = new Material(unlit) { color = Color.white };
            superFlashCore.GetComponent<Renderer>().sharedMaterial = superFlashCoreMat;

            GameObject superFlashHalo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            superFlashHalo.name = "SupernovaFlashHalo";
            superFlashHalo.transform.SetParent(expObj.transform, false);
            superFlashHalo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            superFlashHalo.transform.localScale = Vector3.one * 9.0f;
            DestroySafe(superFlashHalo.GetComponent<Collider>());
            Material superFlashHaloMat = new Material(unlit) { color = new Color(0f, 1f, 0.9f, 0.95f) };
            superFlashHalo.GetComponent<Renderer>().sharedMaterial = superFlashHaloMat;

            // 72 High-velocity shards
            int shardCount = 72;
            GameObject[] shards = new GameObject[shardCount];
            Vector3[] velocities = new Vector3[shardCount];
            Vector3[] rotVelocities = new Vector3[shardCount];

            Color[] palette = new[]
            {
                new Color(0f, 1f, 0.9f),       // Cyan
                new Color(1f, 1f, 1f),         // Electric White
                new Color(1f, 0.65f, 0.1f),    // Hot Amber
                new Color(1f, 0.15f, 0.5f),    // Cyber Magenta
                new Color(0f, 1f, 0.4f)        // Phosphor Green
            };

            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"SuperShard_{i}";
                shard.transform.SetParent(expObj.transform, false);
                shard.transform.localPosition = Vector3.zero;

                float size = UnityEngine.Random.Range(0.18f, 0.48f);
                shard.transform.localScale = Vector3.one * size;
                DestroySafe(shard.GetComponent<Collider>());

                Color col = palette[i % palette.Length];
                Material mat = new Material(unlit) { color = col };
                shard.GetComponent<Renderer>().sharedMaterial = mat;

                shards[i] = shard;

                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float speed = UnityEngine.Random.Range(7f, 22f);
                velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;
                rotVelocities[i] = new Vector3(
                    UnityEngine.Random.Range(-500f, 500f),
                    UnityEngine.Random.Range(-500f, 500f),
                    UnityEngine.Random.Range(-500f, 500f));

                // Add TrailRenderer to every 6th shard for glowing ribbons
                if (i % 6 == 0)
                {
                    TrailRenderer tr = shard.AddComponent<TrailRenderer>();
                    tr.time = 0.45f;
                    tr.startWidth = 0.35f;
                    tr.endWidth = 0.02f;
                    tr.sharedMaterial = mat;
                }
            }

            // Expanding Shockwave Rings (Cyan & Amber)
            GameObject ringA = CreateShockwaveRing(expObj.transform, new Color(0f, 1f, 0.9f, 0.85f), unlit);
            GameObject ringB = CreateShockwaveRing(expObj.transform, new Color(1f, 0.7f, 0.1f, 0.75f), unlit);

            StartCoroutine(SupernovaAnimateRoutine(expObj, shards, velocities, rotVelocities, ringA, ringB, superFlashCore, superFlashHalo, superFlashCoreMat, superFlashHaloMat, 1.8f));
        }

        private GameObject CreateShockwaveRing(Transform parent, Color col, Shader shader)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);
            DestroySafe(ring.GetComponent<Collider>());

            Material mat = new Material(shader) { color = col };
            ring.GetComponent<Renderer>().sharedMaterial = mat;
            return ring;
        }

        private IEnumerator SupernovaAnimateRoutine(
            GameObject root,
            GameObject[] shards,
            Vector3[] vels,
            Vector3[] rots,
            GameObject ringA,
            GameObject ringB,
            GameObject flashCore,
            GameObject flashHalo,
            Material flashCoreMat,
            Material flashHaloMat,
            float duration)
        {
            float elapsed = 0f;
            Vector3[] initScales = new Vector3[shards.Length];
            for (int i = 0; i < shards.Length; i++) initScales[i] = shards[i].transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Animate Epicenter Core Flash (0.35s)
                if (flashCore != null)
                {
                    float ft = Mathf.Clamp01(elapsed / 0.35f);
                    float curScale = Mathf.Lerp(5.0f, 12.0f, ft);
                    flashCore.transform.localScale = Vector3.one * curScale;
                    if (flashCoreMat != null)
                    {
                        Color c = Color.Lerp(Color.white, new Color(0f, 1f, 0.9f), ft * 0.5f);
                        c.a = Mathf.Lerp(1f, 0f, ft * ft);
                        flashCoreMat.color = c;
                    }
                    if (ft >= 1f && flashCore.activeSelf) flashCore.SetActive(false);
                }

                if (flashHalo != null)
                {
                    float ht = Mathf.Clamp01(elapsed / 0.5f);
                    float curHaloScale = Mathf.Lerp(9.0f, 16.0f, ht);
                    flashHalo.transform.localScale = Vector3.one * curHaloScale;
                    flashHalo.transform.Rotate(0f, 0f, 120f * Time.unscaledDeltaTime);
                    if (flashHaloMat != null)
                    {
                        Color c = new Color(0f, 1f, 0.9f);
                        c.a = Mathf.Lerp(0.95f, 0f, ht);
                        flashHaloMat.color = c;
                    }
                    if (ht >= 1f && flashHalo.activeSelf) flashHalo.SetActive(false);
                }

                // Animate Shards
                for (int i = 0; i < shards.Length; i++)
                {
                    if (shards[i] == null) continue;
                    shards[i].transform.localPosition += vels[i] * Time.unscaledDeltaTime;
                    shards[i].transform.Rotate(rots[i] * Time.unscaledDeltaTime);
                    shards[i].transform.localScale = Vector3.Lerp(initScales[i], Vector3.zero, t);
                    vels[i] *= 0.96f; // Air damping
                }

                // Animate Shockwave Rings
                if (ringA != null)
                {
                    float scaleA = Mathf.Lerp(0.5f, 15f, t);
                    ringA.transform.localScale = new Vector3(scaleA, 0.01f, scaleA);
                }
                if (ringB != null)
                {
                    float tB = Mathf.Clamp01((elapsed - 0.08f) / (duration - 0.08f));
                    float scaleB = Mathf.Lerp(0.2f, 11f, tB);
                    ringB.transform.localScale = new Vector3(scaleB, 0.01f, scaleB);
                }

                yield return null;
            }

            if (root != null) DestroySafe(root);
        }

        private void DisintegrateBossModel(GameObject bossObj)
        {
            if (bossObj == null) return;

            // Launch child renderers into wild spins before destruction
            Transform[] children = bossObj.GetComponentsInChildren<Transform>();
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == bossObj.transform) continue;
                children[i].SetParent(null);
                StartCoroutine(DisintegratePieceRoutine(children[i].gameObject));
            }

            bossObj.SetActive(false);
        }

        private IEnumerator DisintegratePieceRoutine(GameObject piece)
        {
            if (piece == null) yield break;
            Vector3 vel = (piece.transform.position - Vector3.up * 3.2f).normalized * UnityEngine.Random.Range(8f, 14f);
            Vector3 rot = new Vector3(UnityEngine.Random.Range(-400f, 400f), UnityEngine.Random.Range(-400f, 400f), UnityEngine.Random.Range(-400f, 400f));
            Vector3 initScale = piece.transform.localScale;

            float elapsed = 0f;
            float dur = 0.8f;
            while (elapsed < dur && piece != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                piece.transform.position += vel * Time.unscaledDeltaTime;
                piece.transform.Rotate(rot * Time.unscaledDeltaTime);
                piece.transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }

            if (piece != null) DestroySafe(piece);
        }

        private IEnumerator DigitalRainRoutine()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");

            GameObject rainRoot = new GameObject("DigitalRainRoot");
            rainRoot.transform.SetParent(transform, false);

            int emberCount = 36;
            GameObject[] embers = new GameObject[emberCount];
            Vector3[] positions = new Vector3[emberCount];
            float[] speeds = new float[emberCount];
            float[] swayOffsets = new float[emberCount];

            for (int i = 0; i < emberCount; i++)
            {
                GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ember.transform.SetParent(rainRoot.transform, false);
                positions[i] = new Vector3(UnityEngine.Random.Range(-7.5f, 7.5f), UnityEngine.Random.Range(5.5f, 12f), -1f);
                ember.transform.localPosition = positions[i];
                ember.transform.localScale = new Vector3(0.08f, UnityEngine.Random.Range(0.2f, 0.45f), 0.08f);
                DestroySafe(ember.GetComponent<Collider>());

                Material m = new Material(unlit) { color = new Color(0f, 1f, 0.6f, UnityEngine.Random.Range(0.4f, 0.85f)) };
                ember.GetComponent<Renderer>().sharedMaterial = m;
                embers[i] = ember;
                speeds[i] = UnityEngine.Random.Range(2.5f, 6.0f);
                swayOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            }

            float rainTimer = 0f;
            float rainDuration = 6.0f;

            while (rainTimer < rainDuration)
            {
                rainTimer += Time.unscaledDeltaTime;
                for (int i = 0; i < emberCount; i++)
                {
                    if (embers[i] == null) continue;
                    positions[i].y -= speeds[i] * Time.unscaledDeltaTime;
                    if (positions[i].y < -6f) positions[i].y = 6.5f;

                    float sway = Mathf.Sin(Time.unscaledTime * 2.5f + swayOffsets[i]) * 0.15f;
                    embers[i].transform.localPosition = new Vector3(positions[i].x + sway, positions[i].y, positions[i].z);
                }
                yield return null;
            }

            if (rainRoot != null) DestroySafe(rainRoot);
        }

        private IEnumerator SystemPurgeLogRoutine()
        {
            string[] logLines = new[]
            {
                ">>> MEMULIHKAN KERNEL ALGORHYTHM CORE... [OK]",
                ">>> INTEGRITAS DATABASE KOTA: 100% AMAN & UTUH",
                ">>> THREAD MALWARE A.I.D.A: PERMANEN DIHAPUS DARI MEMORI",
                ">>> PROTOKOL DARK WEB: BATAL (0 BYTE TERUNGGAH)",
                ">>> KENDALI PENUH DIKEMBALIKAN KEPADA OPERATOR"
            };

            List<string> shown = new List<string>();
            if (systemLogText != null)
            {
                systemLogText.gameObject.SetActive(true);
                systemLogText.text = "";
            }

            for (int i = 0; i < logLines.Length; i++)
            {
                shown.Add(logLines[i]);
                if (systemLogText != null) systemLogText.text = string.Join("\n", shown);
                PlaySound(_hitSfx, 0.35f);
                yield return new WaitForSecondsRealtime(0.28f);
            }

            yield return new WaitForSecondsRealtime(1.2f);
            if (systemLogText != null) systemLogText.gameObject.SetActive(false);
        }

        public static string FormatVictoryCardText(string operatorName, int aidaLevel, float remainingTimeSeconds)
        {
            int mins = Mathf.FloorToInt(Mathf.Max(0f, remainingTimeSeconds) / 60f);
            int secs = Mathf.FloorToInt(Mathf.Max(0f, remainingTimeSeconds) % 60f);

            return
                "★ VICTORY: MAGNUM OPUS ★\n" +
                "KOTA DAN DATA ANDA TELAH SELAMAT\n\n" +
                $"OPERATOR     : {operatorName}\n" +
                $"ANCAMAN      : A.I.D.A v{aidaLevel}.0 [PERMANENTLY TERMINATED]\n" +
                $"SISA WAKTU   : {mins:00}:{secs:00} TERSISA DARI UPLOAD\n" +
                "STATUS KOTA  : 100% SECURE & OPERATIONAL\n" +
                "GELAR RITME  : MASTER OF CODE & SHIELD\n\n" +
                "[ENTER] KEMBALI KE MENU UTAMA   ·   [R] TANTANG ULANG A.I.D.A";
        }

        private void ShowMagnumOpusVictoryCard()
        {
            if (finaleCardContainer != null) finaleCardContainer.SetActive(true);

            string user = "OPERATOR";
            try
            {
                user = Environment.UserName.ToUpperInvariant();
            }
            catch (Exception)
            {
                user = "OPERATOR";
            }

            string card = FormatVictoryCardText(user, _aidaLevel, _remainingTimeSeconds);

            if (finaleCardText != null)
            {
                finaleCardText.text = card;
                finaleCardText.color = new Color(0f, 1f, 0.65f);
            }
        }

        public static string FormatDefeatCardText(string operatorName, int aidaLevel)
        {
            return
                "✕ MISSION FAILED: DATA BREACH ✕\n" +
                "A.I.D.A TELAH MENGUASAI KERNEL KOTA\n\n" +
                $"OPERATOR     : {operatorName}\n" +
                $"STATUS       : KEKALAHAN TELAK (REFLEK TERLALU LAMBAT)\n" +
                $"ANCAMAN      : A.I.D.A v{aidaLevel}.0 [JARINGAN KOTA TERINFEKSI]\n" +
                "DATA TERJUAL : 100% UPLOAD KE DARK WEB LENGKAP\n" +
                "STATUS KOTA  : PROTOKOL SISTEM LUMPUH TOTAL\n\n" +
                "[R] TANTANG ULANG A.I.D.A (REMATCH)   ·   [ENTER] MENYERAH KE MENU UTAMA";
        }

        private void ShowDefeatTauntCard()
        {
            if (finaleCardContainer != null) finaleCardContainer.SetActive(true);

            string user = "OPERATOR";
            try
            {
                user = Environment.UserName.ToUpperInvariant();
            }
            catch (Exception)
            {
                user = "OPERATOR";
            }

            string card = FormatDefeatCardText(user, _aidaLevel);

            if (finaleCardText != null)
            {
                finaleCardText.text = card;
                finaleCardText.color = new Color(1f, 0.25f, 0.3f);
            }
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (_sfxSource != null && clip != null)
            {
                _sfxSource.PlayOneShot(clip, volume);
            }
        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }
    }
}
