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
        [SerializeField] private WaveDefinitionSO[] waveDefinitions;

        private CombatController _controller;
        private WaveDirector _director;
        private RuleRuntime _rules;
        private CombatState _combat;
        private DspClockSource _clockSource;

        private void Awake()
        {
            if (config == null || arena == null || hud == null || metronome == null
                || sfx == null || cameraShake == null || aidaTower == null
                || input == null || codePanel == null
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
            var clock = new BeatClock(_clockSource, config.bpm);
            var judge = new TimingJudge(config.perfectWindowMs, config.goodWindowMs)
            {
                CalibrationOffsetMs = config.calibrationOffsetMs
            };

            var waves = new List<WaveData>(waveDefinitions.Length);
            foreach (WaveDefinitionSO definition in waveDefinitions)
            {
                waves.Add(WaveMapper.ToWaveData(definition));
            }

            _director = new WaveDirector(waves, clock, _clockSource, config.startLeadInBeats);
            _combat = new CombatState(config.maxIntegrity, config.startingEnergy);
            _rules = new RuleRuntime();
            var shield = new ShieldController(config.shieldTransitMs);
            var validator = new RuleValidator(
                RuleWhitelist.Demo, config.ruleEnergyCost, config.ruleCooldownMs);

            arena.Initialize(config.perfectWindowMs, config.goodWindowMs);
            hud.Initialize(_combat, clock, _director);
            metronome.Initialize(clock);
            sfx.Initialize(clock);
            cameraShake.Initialize(config.shakeDecayPerSecond, config.shakeMaxOffset);
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
                shield,
                config);
            _controller.Start();
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
            }
        }

        private void OnWaveStarted(int index)
        {
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

        private RuleValidationContext BuildValidationContext()
        {
            return new RuleValidationContext(
                _combat.Energy,
                _rules.GetCooldownRemainingMs(_clockSource.NowDsp));
        }
    }
}
