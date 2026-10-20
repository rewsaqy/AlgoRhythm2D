using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Rules;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Presentation;
using UnityEngine;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// I-002 combat driver. Manual shield keeps I-001 behavior; at each
    /// impact the controller then offers the threat to the rule runtime
    /// (auto-shield, max Good, Set A edge rules). One final result per
    /// threat, by input, by rule, or by timeout. Integrity 0 retries the
    /// current sub-wave (A5); clearing a wave advances the director (1A→1B).
    /// </summary>
    public sealed class CombatController : IDisposable
    {
        private readonly IClockSource _clockSource;
        private readonly BeatClock _clock;
        private readonly TimingJudge _judge;
        private readonly WaveDirector _director;
        private readonly CombatState _combat;
        private readonly ArenaPresenter _arena;
        private readonly HudView _hud;
        private readonly MetronomePlayer _metronome;
        private readonly InputRouter _input;
        private readonly RuleRuntime _rules;
        private readonly ShieldController _shield;
        private readonly DemoConfig _config;
        private readonly List<ActiveThreat> _active = new List<ActiveThreat>();
        private readonly ThreatEvent[] _spawnBuffer = new ThreatEvent[8];

        private bool _started;
        private bool _paused;
        private int _lastBeat = int.MinValue;
        private bool _clearAnnounced;

        private sealed class ActiveThreat
        {
            public ThreatEvent Threat;
            public bool Resolved;
            public bool ImpactEvaluated;
            public double? BufferedInputDsp;
        }

        public CombatController(
            IClockSource clockSource,
            BeatClock clock,
            TimingJudge judge,
            WaveDirector director,
            CombatState combat,
            ArenaPresenter arena,
            HudView hud,
            MetronomePlayer metronome,
            InputRouter input,
            RuleRuntime rules,
            ShieldController shield,
            DemoConfig config)
        {
            _clockSource = clockSource ?? throw new ArgumentNullException(nameof(clockSource));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _judge = judge ?? throw new ArgumentNullException(nameof(judge));
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _metronome = metronome ?? throw new ArgumentNullException(nameof(metronome));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _shield = shield ?? throw new ArgumentNullException(nameof(shield));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsPaused => _paused;

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _lastBeat = int.MinValue;

            _input.ShieldPressed += HandleShieldPressed;
            _input.PausePressed += TogglePause;
            _director.WaveStarted += OnWaveStarted;
            _director.StartWave(0);
            _hud.SetStatus("I-002: rules greybox — 1/2/3, Q/E, ENTER, R");
        }

        public void Tick()
        {
            if (!_started || _paused)
            {
                return;
            }

            int beat = _clock.CurrentBeatIndex;
            if (beat != _lastBeat)
            {
                _lastBeat = beat;
                SpawnDue(beat);
                if (beat > 0 && _config.energyRegenEveryBeats > 0
                    && beat % _config.energyRegenEveryBeats == 0)
                {
                    _combat.AddEnergy(1);
                }
            }

            double now = _clockSource.NowDsp;
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved)
                {
                    _active.RemoveAt(index);
                    continue;
                }

                double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
                double spawnDsp = _clock.BeatTime(active.Threat.SpawnBeat);
                // Beat-quantized glide: one tile per beat, smoothstepped
                // inside the beat, frozen between beats and on pause. Pure
                // DSP function; judging is untouched.
                float progress = BeatGlideProgress(
                    active.Threat.SpawnBeat,
                    active.Threat.ImpactBeat,
                    now);
                _arena.UpdateThreat(
                    active.Threat,
                    progress,
                    active.BufferedInputDsp.HasValue,
                    (impactDsp - now) * 1000.0);

                if (now >= impactDsp && !active.ImpactEvaluated)
                {
                    // The auto-shield decision runs exactly once, at impact;
                    // later manual presses use the immediate input path.
                    active.ImpactEvaluated = true;
                    ResolveAtImpact(active);
                }

                if (!active.Resolved && now > impactDsp + _judge.GoodWindowMs / 1000.0)
                {
                    ResolveTimeout(active);
                }
            }

            if (_combat.Integrity <= 0)
            {
                RetryCurrentWave();
                return;
            }

            if (!_clearAnnounced && _director.ActiveRunner.IsCleared)
            {
                AnnounceWaveClear();
            }

            _arena.SetShieldPose(
                _shield.CurrentAngle(now),
                ArenaPresenter.DirectionCode(_shield.NearestSlot(now)));
        }

        public void TogglePause()
        {
            if (!_started)
            {
                return;
            }

            if (_paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (!_started || _paused)
            {
                return;
            }

            _clock.Pause();
            _metronome.Pause();
            _paused = true;
            for (int index = 0; index < _active.Count; index++)
            {
                _active[index].BufferedInputDsp = null;
            }

            _hud.SetStatus("PAUSED — press ESC to resume");
        }

        public void Resume()
        {
            if (!_started || !_paused)
            {
                return;
            }

            _clock.Resume();
            _metronome.Resume();
            _paused = false;
            _hud.SetStatus("RESUMED");
        }

        public void Dispose()
        {
            _input.ShieldPressed -= HandleShieldPressed;
            _input.PausePressed -= TogglePause;
            _director.WaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted(int index)
        {
            _clearAnnounced = false;
            _hud.ShowWaveSummary(string.Empty);
        }

        /// <summary>
        /// Maps DSP time to stepped travel progress: the threat holds still
        /// for most of each beat, then steps quickly in the last attack
        /// fraction and lands exactly on the beat. Frozen on pause. Pure DSP
        /// function; judging is untouched.
        /// </summary>
        private float BeatGlideProgress(int spawnBeat, int impactBeat, double nowDsp)
        {
            int travelBeats = impactBeat - spawnBeat;
            if (travelBeats <= 0)
            {
                return 1.0f;
            }

            double beatsSinceSpawn = (nowDsp - _clock.BeatTime(spawnBeat)) / _clock.SecondsPerBeat;
            if (beatsSinceSpawn <= 0.0)
            {
                return 0.0f;
            }

            float attack = Mathf.Clamp(_config.noteStepAttack, 0.05f, 0.9f);
            double tile = Math.Floor(beatsSinceSpawn);
            double frac = beatsSinceSpawn - tile;
            double eased = tile;
            if (frac >= 1.0 - attack)
            {
                double u = (frac - (1.0 - attack)) / attack;
                eased = tile + u * u * (3.0 - 2.0 * u);
            }

            return (float)(eased / travelBeats);
        }

        private void SpawnDue(int beat)
        {
            WaveRunner wave = _director.ActiveRunner;
            int count = wave.Due(beat, _spawnBuffer);
            for (int index = 0; index < count; index++)
            {
                ThreatEvent threat = _spawnBuffer[index];
                _active.Add(new ActiveThreat { Threat = threat });
                float travelMs = (float)((_clock.BeatTime(threat.ImpactBeat)
                                          - _clock.BeatTime(threat.SpawnBeat)) * 1000.0);
                _arena.SpawnThreat(threat, travelMs);
                _hud.SetStatus(
                    $"Threat {threat.Id}: {threat.Direction} telegraph — impact beat {threat.ImpactBeat}");
            }

            if (wave.AllSpawned && wave.IsCleared)
            {
                _hud.SetStatus("WAVE CLEAR — all threats resolved");
            }
        }

        /// <summary>
        /// Impact order: buffered manual non-Miss wins (precedence); a
        /// buffered Miss never blocks the rule; otherwise the rule may fire.
        /// </summary>
        private void ResolveAtImpact(ActiveThreat active)
        {
            double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);

            if (active.BufferedInputDsp.HasValue)
            {
                TimingGrade bufferedGrade = _judge.Grade(active.BufferedInputDsp.Value, impactDsp);
                if (bufferedGrade != TimingGrade.Miss)
                {
                    ResolveManual(active, bufferedGrade);
                    return;
                }
            }

            RuleFireDecision decision = _rules.EvaluateThreat(
                active.Threat,
                _clockSource.NowDsp,
                _combat.Energy,
                manualNonMissResolved: false,
                out RuleFired fired);

            if (decision == RuleFireDecision.Fire && _combat.TrySpendEnergy(fired.EnergySpent))
            {
                ResolveAuto(active, fired);
            }
        }

        private void ResolveTimeout(ActiveThreat active)
        {
            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                ResolveDecoy(active, shielded: false, fromRule: false);
                return;
            }

            Resolve(active, TimingGrade.Miss, "timeout");
        }

        private void HandleShieldPressed(Direction direction, double inputDsp)
        {
            if (!_started)
            {
                return;
            }

            if (_paused)
            {
                _hud.SetStatus("PAUSED — press ESC to resume");
                return;
            }

            ActiveThreat candidate = FindNearestThreat(direction, inputDsp);
            _shield.RequestDirection(direction, inputDsp);
            if (candidate == null)
            {
                _arena.ShowInputRejected($"WRONG DIRECTION ({direction})");
                _hud.SetStatus($"No {direction} threat in the input buffer");
                return;
            }

            // The press always moves the shield; the block lands only when the
            // shield actually covers the lane at press moment.
            if (!_shield.CoversDirection(direction, inputDsp))
            {
                _arena.ShowInputRejected("SHIELD NOT IN LANE");
                _hud.SetStatus($"Shield en route — {direction} not covered yet");
                return;
            }

            double impactDsp = _clock.BeatTime(candidate.Threat.ImpactBeat);
            double deltaMs = (inputDsp - impactDsp) * 1000.0;

            if (deltaMs > _judge.GoodWindowMs)
            {
                _arena.ShowInputRejected("TOO LATE");
                _hud.SetStatus("Late input — this threat will Miss");
                return;
            }

            double earliestMs = -(_judge.GoodWindowMs + _config.inputBufferMs);
            if (deltaMs < earliestMs)
            {
                _arena.ShowInputRejected("TOO EARLY");
                _hud.SetStatus("Input was outside the buffer window");
                return;
            }

            if (deltaMs < -_judge.GoodWindowMs)
            {
                if (!candidate.BufferedInputDsp.HasValue
                    || Math.Abs(deltaMs) < Math.Abs((candidate.BufferedInputDsp.Value - impactDsp) * 1000.0))
                {
                    candidate.BufferedInputDsp = inputDsp;
                }

                _arena.PulseShield();
                _hud.SetStatus($"Buffered {direction} input — judged at impact");
                return;
            }

            TimingGrade grade = _judge.Grade(inputDsp, impactDsp);
            if (candidate.Threat.Kind == ThreatKind.AdwareDecoy && grade != TimingGrade.Miss)
            {
                candidate.Resolved = true;
                ResolveDecoy(candidate, shielded: true, fromRule: false);
                return;
            }

            _arena.PulseShield();
            Resolve(candidate, grade, "input");
        }

        private void ResolveManual(ActiveThreat active, TimingGrade grade)
        {
            _rules.TryLogManualPrecedence(active.Threat);
            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                active.Resolved = true;
                ResolveDecoy(active, shielded: true, fromRule: false);
                return;
            }

            _arena.PulseShield();
            Resolve(active, grade, "input");
        }

        private void ResolveAuto(ActiveThreat active, RuleFired fired)
        {
            active.Resolved = true;
            // The auto-shield drives the shield itself: it visibly swooshes to
            // the lane as the block lands (Set A independence).
            _shield.RequestDirection(active.Threat.Direction, _clockSource.NowDsp);
            _arena.PulseShield();

            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                ResolveDecoy(active, shielded: true, fromRule: true);
                return;
            }

            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            if (_combat.ResolveThreat(active.Threat, TimingGrade.Good, fromRule: true, damage: 0))
            {
                _arena.ResolveThreat(active.Threat.Id, TimingGrade.Good, correctDirection: true);
                _hud.SetStatus($"Threat {active.Threat.Id}: Good (auto-shield)");
            }
        }

        private void ResolveDecoy(ActiveThreat active, bool shielded, bool fromRule)
        {
            active.Resolved = true;
            _director.ActiveRunner.MarkResolved(active.Threat.Id);

            if (!_combat.ResolveDecoy(active.Threat, shielded, fromRule, out int scoreDelta))
            {
                return;
            }

            _arena.ResolveDecoyView(active.Threat.Id, shielded);
            _hud.SetStatus(shielded
                ? $"Threat {active.Threat.Id}: DECOY −5 ({(fromRule ? "auto-shield" : "manual")})"
                : $"Threat {active.Threat.Id}: decoy ignored");
        }

        private ActiveThreat FindNearestThreat(Direction direction, double inputDsp)
        {
            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved || active.Threat.Direction != direction)
                {
                    continue;
                }

                double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
                double distanceMs = Math.Abs((inputDsp - impactDsp) * 1000.0);
                if (distanceMs < bestDistanceMs)
                {
                    best = active;
                    bestDistanceMs = distanceMs;
                }
            }

            return best;
        }

        private void Resolve(ActiveThreat active, TimingGrade grade, string reason)
        {
            if (active.Resolved)
            {
                return;
            }

            int damage = 0;
            if (grade == TimingGrade.Miss && active.Threat.Kind == ThreatKind.StraightShooter)
            {
                damage = active.Threat.BaseDamage;
            }

            if (!_combat.ResolveThreat(active.Threat, grade, fromRule: false, damage: damage))
            {
                return;
            }

            active.Resolved = true;
            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            _arena.ResolveThreat(active.Threat.Id, grade, correctDirection: grade != TimingGrade.Miss);
            _hud.SetStatus($"Threat {active.Threat.Id}: {grade} ({reason})");
        }

        private void AnnounceWaveClear()
        {
            _clearAnnounced = true;
            _combat.AddEnergy(1);
            _hud.ShowWaveSummary(
                $"WAVE CLEAR {_director.ActiveRunner.Data.WaveId} — " +
                $"P {_combat.PerfectCount}  G {_combat.GoodCount}  M {_combat.MissCount}  " +
                $"DMG {_combat.DamageTaken}  SCORE {_combat.Score}");
            _hud.SetStatus("WAVE CLEAR — all threats resolved");

            _arena.ClearThreats();
            if (!_director.Advance())
            {
                _hud.SetStatus("RUN CLEAR — 1A and 1B done");
            }
        }

        private void RetryCurrentWave()
        {
            _arena.ClearThreats();
            _active.Clear();
            _combat.Reset(_config.maxIntegrity, _config.startingEnergy);
            _metronome.Pause();
            _director.RetryCurrent();
            _metronome.Resume();
            _hud.SetStatus("RETRY — wave restarted");
        }
    }
}
