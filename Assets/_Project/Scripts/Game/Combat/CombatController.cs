using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Presentation;
using UnityEngine;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// I-001 combat driver. Input is graded against the threat's DSP impact
    /// time; a threat is resolved exactly once, by input or by timeout.
    /// </summary>
    public sealed class CombatController : IDisposable
    {
        private readonly IClockSource _clockSource;
        private readonly BeatClock _clock;
        private readonly TimingJudge _judge;
        private readonly WaveRunner _wave;
        private readonly CombatState _combat;
        private readonly ArenaPresenter _arena;
        private readonly HudView _hud;
        private readonly MetronomePlayer _metronome;
        private readonly InputRouter _input;
        private readonly DemoConfig _config;
        private readonly List<ActiveThreat> _active = new List<ActiveThreat>();
        private readonly ThreatEvent[] _spawnBuffer = new ThreatEvent[8];

        private bool _started;
        private bool _paused;
        private int _lastBeat = int.MinValue;

        private sealed class ActiveThreat
        {
            public ThreatEvent Threat;
            public bool Resolved;
            public double? BufferedInputDsp;
        }

        public CombatController(
            IClockSource clockSource,
            BeatClock clock,
            TimingJudge judge,
            WaveRunner wave,
            CombatState combat,
            ArenaPresenter arena,
            HudView hud,
            MetronomePlayer metronome,
            InputRouter input,
            DemoConfig config)
        {
            _clockSource = clockSource ?? throw new ArgumentNullException(nameof(clockSource));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _judge = judge ?? throw new ArgumentNullException(nameof(judge));
            _wave = wave ?? throw new ArgumentNullException(nameof(wave));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _metronome = metronome ?? throw new ArgumentNullException(nameof(metronome));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsPaused => _paused;

        public void Start()
        {
            if (_started)
            {
                return;
            }

            double startDsp = _clockSource.NowDsp
                              + _config.startLeadInBeats * _clock.SecondsPerBeat;
            _clock.Start(startDsp);
            _started = true;
            _lastBeat = int.MinValue;

            _input.ShieldPressed += HandleShieldPressed;
            _input.PausePressed += TogglePause;
            _hud.SetStatus("I-001: straight-shooter greybox");
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
                float progress = impactDsp > spawnDsp
                    ? (float)((now - spawnDsp) / (impactDsp - spawnDsp))
                    : 1.0f;
                _arena.UpdateThreat(active.Threat, progress, active.BufferedInputDsp.HasValue);

                if (active.BufferedInputDsp.HasValue && now >= impactDsp)
                {
                    TimingGrade bufferedGrade = _judge.Grade(active.BufferedInputDsp.Value, impactDsp);
                    Resolve(active, bufferedGrade, "buffered input");
                    continue;
                }

                if (now > impactDsp + _judge.GoodWindowMs / 1000.0)
                {
                    Resolve(active, TimingGrade.Miss, "timeout");
                }
            }
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
        }

        private void SpawnDue(int beat)
        {
            int count = _wave.Due(beat, _spawnBuffer);
            for (int index = 0; index < count; index++)
            {
                ThreatEvent threat = _spawnBuffer[index];
                _active.Add(new ActiveThreat { Threat = threat });
                _arena.SpawnThreat(threat);
                _hud.SetStatus(
                    $"Threat {threat.Id}: {threat.Direction} telegraph — impact beat {threat.ImpactBeat}");
            }

            if (_wave.AllSpawned && _wave.IsCleared)
            {
                _hud.SetStatus("WAVE CLEAR — all threats resolved");
            }
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
            if (candidate == null)
            {
                _arena.ShowInputRejected($"WRONG DIRECTION ({direction})");
                _hud.SetStatus($"No {direction} threat in the input buffer");
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

                _arena.ShowShield(direction, _judge.Grade(inputDsp, impactDsp));
                _hud.SetStatus($"Buffered {direction} input — judged at impact");
                return;
            }

            TimingGrade grade = _judge.Grade(inputDsp, impactDsp);
            _arena.ShowShield(direction, grade);
            Resolve(candidate, grade, "input");
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
            _wave.MarkResolved(active.Threat.Id);
            _arena.ResolveThreat(active.Threat.Id, grade, correctDirection: grade != TimingGrade.Miss);
            _hud.SetStatus($"Threat {active.Threat.Id}: {grade} ({reason})");

            if (_wave.IsCleared)
            {
                _hud.SetStatus("WAVE CLEAR — all threats resolved");
            }
        }
    }
}
