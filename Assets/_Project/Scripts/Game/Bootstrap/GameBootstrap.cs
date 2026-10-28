using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Rules;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Data;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Presentation;
using AlgoRhythm.Game.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Bootstrap
{
    /// <summary>
    /// I-002 composition root. The scene hierarchy, visuals, labels, audio
    /// sources and component wiring are authored manually in SampleScene. This
    /// component only creates the pure-C# runtime services and connects them to
    /// the existing serialized references.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private DemoConfig config;
        [SerializeField] private ArenaPresenter arena;
        [SerializeField] private HudView hud;
        [SerializeField] private MetronomePlayer metronome;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private AidaTower aidaTower;
        [SerializeField] private InputRouter input;
        [SerializeField] private CodePanelView codePanel;
        [SerializeField] private ShopView shop;
        [SerializeField] private SongPlayer songPlayer;
        [SerializeField] private WaveDefinitionSO[] waveDefinitions;
        [Header("Typography [U] — nerd mono display font untuk semua label")]
        [SerializeField] private Font displayFont;

        private CombatController _controller;
        public CombatController Controller => _controller;
        private WaveDirector _director;
        private BeatClock _clock;
        private RuleRuntime _rules;
        private CombatState _combat;
        private DspClockSource _clockSource;

        private void Awake()
        {
            if (config == null || arena == null || hud == null || metronome == null
                || sfx == null || cameraShake == null || aidaTower == null
                || input == null || codePanel == null || shop == null
                || waveDefinitions == null || waveDefinitions.Length == 0)
            {
                Debug.LogError(
                    "GameBootstrap requires its DemoConfig, wave definitions, and manually " +
                    "authored scene references; no runtime fallback is created.",
                    this);
                enabled = false;
                return;
            }

            _clockSource = new DspClockSource();
            UiTypography.DisplayFont = displayFont;
            var clock = new BeatClock(_clockSource, config.bpm);
            _clock = clock;
            var judge = new TimingJudge(config.perfectWindowMs, config.goodWindowMs);
            float savedCalibration = PlayerPrefs.GetFloat(
                DemoDialogue.CalibrationPrefKey, (float)config.calibrationOffsetMs);
            judge.CalibrationOffsetMs = Mathf.Clamp(savedCalibration, -150f, 150f);

#if UNITY_EDITOR
            if (waveDefinitions == null || waveDefinitions.Length < 6)
            {
                var defs = new List<WaveDefinitionSO>();
                foreach (string id in new[] { "W1T", "WS1", "WS2", "WS3", "WS4", "WS5" })
                {
                    var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveDefinitionSO>($"Assets/_Project/Data/Waves/{id}.asset");
                    if (loaded != null) defs.Add(loaded);
                }
                if (defs.Count >= 6)
                {
                    waveDefinitions = defs.ToArray();
                }
            }
#endif

            var waves = new List<WaveData>(waveDefinitions.Length);
            foreach (WaveDefinitionSO definition in waveDefinitions)
            {
                waves.Add(WaveMapper.ToWaveData(definition, config.approachBeats));
            }

            _director = new WaveDirector(waves, clock, _clockSource, config.startLeadInBeats);
            _combat = new CombatState(config.maxIntegrity, config.startingEnergy);
            _rules = new RuleRuntime();
            var paddle = new PaddleController(180f, config.paddleMaxDegPerSec);
            var history = new PaddleHistory(256);
            PaddleView paddleView = arena.GetComponentInChildren<PaddleView>(true);
            if (paddleView == null)
            {
                Debug.LogError(
                    "GameBootstrap: PaddleView missing under Arena — paddle model will not move.",
                    arena);
            }
            var validator = new RuleValidator(
                RuleWhitelist.Demo, config.ruleEnergyCost, config.ruleCooldownMs);

            arena.Initialize(config.perfectWindowMs, config.goodWindowMs);
            hud.Initialize(_combat, clock, _director, judge);
            hud.SetRuleSystemEnabled(config.enableCodePanel);
            metronome.Initialize(clock);
            sfx.Initialize(clock);
            if (songPlayer != null)
            {
                songPlayer.Initialize();
            }
            cameraShake.Initialize(config.shakeDecayPerSecond, config.shakeMaxOffset);
            DamageFlashView damageFlash = cameraShake.GetComponentInChildren<DamageFlashView>(true);
            if (damageFlash != null)
            {
                damageFlash.Initialize(_combat);
            }
            else
            {
                Debug.LogWarning(
                    "DamageFlashView missing on Main Camera; damage flash is silent.",
                    cameraShake);
            }
            aidaTower.Initialize(clock, _clockSource);
            input.Initialize(_clockSource);
            if (config.enableCodePanel)
            {
                codePanel.Initialize(validator, _rules, BuildValidationContext);
            }
            else
            {
                codePanel.gameObject.SetActive(false);
            }
            _director.WaveStarted += OnWaveStarted;
            _director.WaveCleared += OnWaveCleared;
            _director.BeatTimeAligner = (leadInBeats, secondsPerBeat) =>
            {
                if (songPlayer != null && songPlayer.IsPlaying)
                {
                    return songPlayer.GetNextBeatAlignedDspTime(leadInBeats * secondsPerBeat, secondsPerBeat, 4);
                }
                return _clockSource.NowDsp + leadInBeats * secondsPerBeat;
            };
            shop.Initialize();

            var shopController = new ShopController(
                _combat,
                config,
                shop,
                hud,
                sfx,
                aidaTower,
                _director,
                songPlayer);

            var threatManager = new ThreatLifecycleManager(
                arena,
                clock,
                judge,
                config);

            _controller = new CombatController(
                _clockSource,
                clock,
                judge,
                _director,
                _combat,
                arena,
                hud,
                metronome,
                sfx,
                cameraShake,
                aidaTower,
                input,
                _rules,
                paddle,
                history,
                paddleView,
                config,
                shopController,
                threatManager,
                songPlayer);
            TutorialDriver tutorial = GetComponentInChildren<TutorialDriver>(true);
            if (tutorial != null)
            {
                tutorial.Initialize(
                    _director, _combat, config.maxIntegrity, config.startingEnergy);
                _controller.Tutorial = tutorial;
                _controller.DeferFirstWave = true;
            }

            OutroDriver outroDriver = GetComponentInChildren<OutroDriver>(true);
            if (outroDriver != null)
            {
                BlackoutView blackout = cameraShake != null
                    ? cameraShake.GetComponentInChildren<BlackoutView>(true)
                    : null;
                if (blackout == null)
                {
                    Debug.LogWarning(
                        "BlackoutView missing under Main Camera; outro runs dialogue-only.",
                        cameraShake);
                }

                DialogueView outroDialogue = tutorial != null
                    ? tutorial.GetComponent<DialogueView>()
                    : GetComponentInChildren<DialogueView>(true);
                outroDriver.Initialize(
                    _director, outroDialogue, blackout, clock,
                    cameraShake, sfx, damageFlash);
            }
            _controller.Start();
            if (tutorial != null)
            {
                tutorial.Begin();
            }
        }

        private void Update()
        {
            _controller?.Tick();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
            if (_director != null)
            {
                _director.WaveStarted -= OnWaveStarted;
                _director.WaveCleared -= OnWaveCleared;
            }
        }

        private void OnWaveStarted(int index)
        {
            PlayWaveSong(index);
            if (!config.enableCodePanel)
            {
                return;
            }

            if (index >= 0 && index < waveDefinitions.Length
                && waveDefinitions[index] != null)
            {
                codePanel.SetUnlocks(waveDefinitions[index].ruleUnlocks);
            }
        }

        /// <summary>Starts the wave song at beat 0 (after the lead-in).</summary>
        private void PlayWaveSong(int index)
        {
            if (songPlayer == null || _clock == null)
            {
                return;
            }

            if (index < 0 || index >= waveDefinitions.Length
                || waveDefinitions[index] == null)
            {
                songPlayer.StopSong();
                return;
            }

            WaveDefinitionSO definition = waveDefinitions[index];
            bool isRetry = _director != null && _director.IsRetrying;

            float pitch = 1.0f;
            if (definition.songPitch > 0.01f && Mathf.Abs(definition.songPitch - 1.0f) > 0.001f)
            {
                pitch = definition.songPitch;
            }
            else if (definition.songClip != null)
            {
                string clipName = definition.songClip.name.ToLowerInvariant();
                if (clipName.Contains("wave_1") && definition.tempoBpm > 10.0)
                {
                    pitch = (float)(definition.tempoBpm / 100.0);
                }
                else if (clipName.Contains("wave_2") && definition.tempoBpm > 10.0)
                {
                    pitch = (float)(definition.tempoBpm / 115.0);
                }
            }

            songPlayer.PlayWaveSong(definition.songClip, definition.songLoop, _clock.BeatTime(0), isRetry, pitch);
        }

        private void OnWaveCleared(int index)
        {
            if (_director != null && _director.AllWavesCleared)
            {
                if (songPlayer != null)
                {
                    songPlayer.StopSong();
                }

                OutroDriver outroDriver = GetComponentInChildren<OutroDriver>(true);
                if (outroDriver != null)
                {
                    outroDriver.Begin();
                }
            }
        }

        private RuleValidationContext BuildValidationContext()
        {
            return new RuleValidationContext(
                _combat.Energy,
                _rules.GetCooldownRemainingMs(_clockSource.NowDsp));
        }
    }
}
