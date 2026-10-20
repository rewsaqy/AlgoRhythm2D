using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Presentation;
using AlgoRhythm.Game.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Bootstrap
{
    /// <summary>
    /// I-001 composition root. The scene hierarchy, visuals, labels, audio
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
        [SerializeField] private InputRouter input;

        private CombatController _controller;

        private void Awake()
        {
            if (config == null || arena == null || hud == null || metronome == null || input == null)
            {
                Debug.LogError(
                    "GameBootstrap requires its DemoConfig and manually authored scene references; " +
                    "no runtime fallback is created.",
                    this);
                enabled = false;
                return;
            }

            var clockSource = new DspClockSource();
            var clock = new BeatClock(clockSource, config.bpm);
            var judge = new TimingJudge(config.perfectWindowMs, config.goodWindowMs)
            {
                CalibrationOffsetMs = config.calibrationOffsetMs
            };
            var wave = new WaveRunner(
                DebugWaveFactory.Create1AGreybox(config.bpm, config.straightShooterDamage),
                clock);
            var combat = new CombatState(config.maxIntegrity, config.startingEnergy);

            arena.Initialize();
            hud.Initialize(combat, clock, wave);
            metronome.Initialize(clock);
            input.Initialize(clockSource);

            _controller = new CombatController(
                clockSource,
                clock,
                judge,
                wave,
                combat,
                arena,
                hud,
                metronome,
                input,
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
        }
    }
}
