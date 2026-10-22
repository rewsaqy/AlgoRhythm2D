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
using UnityEngine.InputSystem;

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
        private readonly SfxPlayer _sfx;
        private readonly CameraShake _shake;
        private readonly AidaTower _aida;
        private readonly InputRouter _input;
        private readonly RuleRuntime _rules;
        private readonly ShieldController _shield;
        private readonly DemoConfig _config;
        private readonly List<ActiveThreat> _active = new List<ActiveThreat>();
        private readonly ThreatEvent[] _spawnBuffer = new ThreatEvent[8];

        private bool _started;
        private bool _paused;
        private readonly bool _rulesEnabled;
        private int _lastBeat = int.MinValue;
        private int _lastAidaFireBeat = int.MinValue;
        private int _aidaShotCount;
        private int _flavorIndex;
        private bool _clearAnnounced;
        private float _calDebounceUntil;
        private readonly List<AidaShotView> _arrivedShots = new List<AidaShotView>();

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
            SfxPlayer sfx,
            CameraShake shake,
            AidaTower aida,
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
            _sfx = sfx ?? throw new ArgumentNullException(nameof(sfx));
            _shake = shake ?? throw new ArgumentNullException(nameof(shake));
            _aida = aida ?? throw new ArgumentNullException(nameof(aida));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _shield = shield ?? throw new ArgumentNullException(nameof(shield));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            // Rule/energy 3-slot system is temporarily OFF until the final
            // AIDA form (owner decision 2026-10-21). DemoConfig.enableCodePanel
            // is the reversible master switch: panel hidden, no auto-fire,
            // no energy spend/regen. Manual shield stays the only route.
            _rulesEnabled = config.enableCodePanel;
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
            _rules.RuleCommitted += HandleRuleCommitted;
            _director.WaveStarted += OnWaveStarted;
            _director.StartWave(0);
        }

        public void Tick()
        {
            if (!_started)
            {
                return;
            }

            // Calibration stays adjustable even while paused so players can
            // dial it in from the pause overlay. Pure presentation/judge
            // offset; beat authority and judging windows are untouched.
            PollCalibration();

            if (_paused)
            {
                return;
            }

            int beat = _clock.CurrentBeatIndex;
            if (beat != _lastBeat)
            {
                _lastBeat = beat;

                // A3: staged rule commits activate on the next beat. Without
                // this the code-panel commit would stay pending forever and
                // auto-shield could never fire.
                _rules.OnBeat(beat, new BeatContext(beat, _clockSource.NowDsp));
                PushRuleGuards();
                SpawnDue(beat);
                if (_rulesEnabled && beat > 0 && _config.energyRegenEveryBeats > 0
                    && beat % _config.energyRegenEveryBeats == 0)
                {
                    _combat.AddEnergy(1);
                }
            }

            double now = _clockSource.NowDsp;
            double beatPhase = (_clock.NowSeconds / _clock.SecondsPerBeat) % 1.0;
            if (beatPhase < 0.0)
            {
                beatPhase += 1.0;
            }

            if (beat > 0 && _config.aidaFireEveryBeats > 0
                && beat % _config.aidaFireEveryBeats == 0 && beat != _lastAidaFireBeat)
            {
                _lastAidaFireBeat = beat;
                TryAidaFire(beat, now);
            }

            for (int index = _active.Count - 1; index >= 0; index--)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved)
                {
                    _active.RemoveAt(index);
                    continue;
                }

                double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
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
                    (impactDsp - now) * 1000.0,
                    beatPhase);

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

            _aida.UpdateShots(now, _arrivedShots);
            for (int index = 0; index < _arrivedShots.Count; index++)
            {
                AidaShotView shot = _arrivedShots[index];
                int targetId = shot.TargetId;
                bool hit = shot.WillHit;
                shot.Hide();
                if (!hit)
                {
                    continue;
                }

                ActiveThreat target = FindActiveById(targetId);
                if (target == null || target.Resolved)
                {
                    // E006: the shot was aimed true but its target is gone
                    // (resolved or retried away before arrival).
                    _rules.ReportTargetNotFound();
                    continue;
                }

                Resolve(target, TimingGrade.Good, "aida", blockFlash: false);
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
            _sfx.Pause();
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
            _rules.RuleCommitted -= HandleRuleCommitted;
            _director.WaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted(int index)
        {
            _clearAnnounced = false;
            _hud.ShowWaveSummary(string.Empty);
            string waveId = _director.ActiveRunner.Data.WaveId;
            _hud.SetStatus($"WAVE {waveId} START");
            DemoDialogue.LineForWaveStart(index, out string textId, out string line);
            _hud.ShowDialogue(textId, line);
            // WaveDirector restarts the DSP clock origin here; without a
            // resync _nextBeat still points into the old timeline and all
            // future BeatTime() calls land beyond the schedule window,
            // so the metronome goes silent after 1A→1B.
            _metronome.Resync();
        }

        /// <summary>
        /// Z/X nudge the judge calibration in 5 ms steps, clamped to ±150 ms
        /// and persisted to PlayerPrefs. Read directly from the keyboard so no
        /// input-map change is needed; shield and code-panel bindings win on
        /// their own keys and are unaffected.
        /// </summary>
        /// <summary>
        /// Commit confirmation without HUD text: a pew tick, the code-panel
        /// log, and a blinking rule guard in the covered lane(s). Committing
        /// is a player decision, so it must be unmistakable in the arena.
        /// </summary>
        private void HandleRuleCommitted()
        {
            _sfx.PlayPew();
            PushRuleGuards();
        }

        /// <summary>
        /// Pushes active + pending rule coverage to the arena guards so the
        /// player sees where the rules are. Nearest covers all four lanes.
        /// No-op (clears guards) while the rule system is disabled.
        /// </summary>
        private void PushRuleGuards()
        {
            if (!_rulesEnabled)
            {
                _arena.SetRuleGuards(0, 0);
                return;
            }

            _arena.SetRuleGuards(CoverageMask(_rules.ActiveTarget), CoverageMask(_rules.PendingTarget));
        }

        private static int CoverageMask(Target? target)
        {
            if (!target.HasValue)
            {
                return 0;
            }

            if (target.Value == Target.Nearest)
            {
                return 0b1111;
            }

            // Target order mirrors Direction North through West.
            return 1 << (int)target.Value;
        }

        private void PollCalibration()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.unscaledTime < _calDebounceUntil)
            {
                return;
            }

            int step = 0;
            if (keyboard.zKey.wasPressedThisFrame)
            {
                step -= 5;
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                step += 5;
            }

            if (step == 0)
            {
                return;
            }

            _calDebounceUntil = Time.unscaledTime + 0.18f;
            double next = Mathf.Clamp(
                (float)(_judge.CalibrationOffsetMs + step), -150f, 150f);
            _judge.CalibrationOffsetMs = next;
            PlayerPrefs.SetFloat(DemoDialogue.CalibrationPrefKey, (float)next);
            PlayerPrefs.Save();
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
            // Already resolved by direct manual input: no second evaluation,
            // no energy spend, no cooldown. One final result per threat.
            if (active.Resolved)
            {
                return;
            }

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

            // Rule system temporarily OFF: manual shield is the only route.
            // Buffered Miss falls through to the timeout path (Miss).
            if (!_rulesEnabled)
            {
                return;
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
                // Pure repositioning move: no threat in this lane, so no
                // grade text. The shield bar itself moving is the feedback;
                // grade text is reserved for real threat events.
                return;
            }

            // The press always moves the shield; the block lands only when the
            // shield actually covers the lane at press moment. No grade text
            // here either: transit misses are silent, like pure moves.
            if (!_shield.CoversDirection(direction, inputDsp))
            {
                return;
            }

            double impactDsp = _clock.BeatTime(candidate.Threat.ImpactBeat);
            double deltaMs = (inputDsp - impactDsp) * 1000.0;

            if (deltaMs > _judge.GoodWindowMs)
            {
                _arena.ShowInputRejected("TOO LATE");
                return;
            }

            double earliestMs = -(_judge.GoodWindowMs + _config.inputBufferMs);
            if (deltaMs < earliestMs)
            {
                _arena.ShowInputRejected("TOO EARLY");
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
                return;
            }

            TimingGrade grade = _judge.Grade(inputDsp, impactDsp);
            if (candidate.Threat.Kind == ThreatKind.AdwareDecoy && grade != TimingGrade.Miss)
            {
                candidate.Resolved = true;
                _arena.FlashShieldBlock(direction, 1.6f, 0.35f);
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
                _arena.FlashShieldBlock(active.Threat.Direction, 1.6f, 0.35f);
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
                _arena.FlashShieldBlock(active.Threat.Direction, 1.6f, 0.35f);
                ResolveDecoy(active, shielded: true, fromRule: true);
                return;
            }

            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            if (_combat.ResolveThreat(active.Threat, TimingGrade.Good, fromRule: true, damage: 0))
            {
                // Auto block: the lane shield GO pops (Set A independence
                // reads here) alongside the grade text.
                _arena.FlashShieldBlock(active.Threat.Direction, 1.6f, 0.35f);
                _arena.ResolveThreat(active.Threat.Id, TimingGrade.Good, correctDirection: true);
                _sfx.PlayDestroy();
                _shake.AddTrauma(_config.shakeDestroy);
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
            if (shielded)
            {
                _sfx.PlayDestroy();
                _shake.AddTrauma(_config.shakeDestroy);
            }
        }

        /// <summary>
        /// AIDA fires on its cadence at the nearest shootable threat, one
        /// shot per target. Aim is true only on the lucky shot of each hit
        /// cycle (-1000 aura); other shots take an alternating angular error
        /// and always miss harmlessly. Decoys are never targeted. Arrival
        /// meets the threat where it really is at arrival time, not a
        /// fixed ring point.
        /// </summary>
        private void TryAidaFire(int beat, double now)
        {
            CycleAidaFlavor();

            ActiveThreat target = FindAidaTarget(beat, now);
            if (target == null)
            {
                return;
            }

            double impactDsp = _clock.BeatTime(target.Threat.ImpactBeat);
            int hitEvery = Mathf.Max(1, _config.aidaHitEveryShots);
            bool willHit = (_aidaShotCount % hitEvery) == hitEvery - 1;
            float laneAngle = ShieldController.DirectionToAngle(target.Threat.Direction);
            double arrivalDsp = impactDsp - _clock.SecondsPerBeat * 0.25;
            if (arrivalDsp <= now)
            {
                arrivalDsp = now + 0.1;
            }

            Vector3 to = _arena.transform.position
                + _arena.ThreatLocalPosition(target.Threat.Direction, 1f);
            float aimAngle;
            if (willHit)
            {
                // Hit the note where it really is at arrival (same travel
                // math as the threat glide) instead of a fixed ring point,
                // and point the barrel at that spot.
                float arrivalProgress = BeatGlideProgress(
                    target.Threat.SpawnBeat, target.Threat.ImpactBeat, arrivalDsp);
                to = _arena.transform.position
                    + _arena.ThreatLocalPosition(target.Threat.Direction, arrivalProgress);
                Vector3 towerPos = _aida.transform.position;
                aimAngle = Mathf.Atan2(to.x - towerPos.x, to.y - towerPos.y) * Mathf.Rad2Deg;
                _aida.SetFlavor($"Awas {LaneName(target.Threat.Direction)}!");
            }
            else
            {
                aimAngle = laneAngle + ((_aidaShotCount % 2 == 0) ? 25f : -25f);
            }

            _aidaShotCount++;
            _aida.SetAim(aimAngle);

            AidaShotView view = _aida.FireShot(
                _aida.transform.position, to, target.Threat.Id, willHit, arrivalDsp);
            if (view != null)
            {
                _sfx.PlayAidaFire();
            }
        }

        private ActiveThreat FindAidaTarget(int beat, double now)
        {
            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved || active.Threat.Kind != ThreatKind.StraightShooter)
                {
                    continue;
                }

                if (active.Threat.ImpactBeat - beat < 2)
                {
                    continue;
                }

                double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
                double distanceMs = (impactDsp - now) * 1000.0;
                if (distanceMs < bestDistanceMs)
                {
                    best = active;
                    bestDistanceMs = distanceMs;
                }
            }

            return best;
        }

        private ActiveThreat FindActiveById(int threatId)
        {
            for (int index = 0; index < _active.Count; index++)
            {
                if (_active[index].Threat.Id == threatId)
                {
                    return _active[index];
                }
            }

            return null;
        }

        private static string LaneName(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return "Utara";
                case Direction.East:
                    return "Timur";
                case Direction.South:
                    return "Selatan";
                case Direction.West:
                    return "Barat";
                default:
                    return "?";
            }
        }

        private void CycleAidaFlavor()
        {
            string[] lines = _config.aidaFlavorLines;
            if (lines == null || lines.Length == 0)
            {
                _aida.SetFlavor(string.Empty);
                return;
            }

            _aida.SetFlavor(lines[_flavorIndex % lines.Length]);
            _flavorIndex++;
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

        private void Resolve(ActiveThreat active, TimingGrade grade, string reason, bool blockFlash = true)
        {
            _ = reason;
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
            bool success = grade != TimingGrade.Miss;
            if (success && blockFlash)
            {
                // Successful shield: the lane shield GO pops alongside the
                // grade text (Perfect biggest). Arena, sfx, shake and score
                // tick reinforce the read.
                _arena.FlashShieldBlock(
                    active.Threat.Direction,
                    grade == TimingGrade.Perfect ? 2.0f : 1.6f,
                    grade == TimingGrade.Perfect ? 0.45f : 0.35f);
            }

            _arena.ResolveThreat(active.Threat.Id, grade, correctDirection: success);
            if (grade != TimingGrade.Miss)
            {
                _sfx.PlayDestroy();
                _shake.AddTrauma(grade == TimingGrade.Perfect
                    ? _config.shakePerfect
                    : _config.shakeDestroy);
            }
        }

        private void AnnounceWaveClear()
        {
            _clearAnnounced = true;
            if (_rulesEnabled)
            {
                _combat.AddEnergy(1);
            }
            string waveId = _director.ActiveRunner.Data.WaveId;
            bool lastWave = _director.ActiveIndex + 1 >= _director.WaveCount;
            string counts =
                $"P {_combat.PerfectCount}  G {_combat.GoodCount}  M {_combat.MissCount}";
            string energySuffix = _rulesEnabled ? "\n+1 Energy · lanjut..." : "\nlanjut...";
            _hud.ShowWaveSummary(lastWave
                ? $"RUN CLEAR — {waveId} selesai\nSCORE {_combat.Score} · {counts}\n" +
                  $"damage {_combat.DamageTaken} · decoy {_combat.DecoyPenaltyTotal}"
                : $"WAVE CLEAR {waveId} — SCORE {_combat.Score}\n{counts}{energySuffix}");
            _hud.SetStatus(lastWave
                ? "RUN CLEAR — 1A dan 1B selesai"
                : $"WAVE CLEAR {waveId} — semua threat resolved");
            DemoDialogue.LineForWaveClear(waveId, lastWave, out string textId, out string line);
            _hud.ShowDialogue(textId, line);

            _arena.ClearThreats();
            _director.Advance();
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
