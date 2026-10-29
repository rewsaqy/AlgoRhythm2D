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
    /// Beatblock combat driver. Paddle sudut kontinu mengikuti mouse;
    /// tap (Z/X/klik) dinilai bila paddle menutupi note saat tap.
    /// Satu hasil final per threat, oleh input atau timeout.
    /// Integrasi Shop & ThreatLifecycle didelegasikan ke ShopController dan ThreatLifecycleManager.
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
        private readonly SongPlayer _song;
        private readonly CameraShake _shake;
        private readonly AidaTower _aida;
        private readonly InputRouter _input;
        private readonly RuleRuntime _rules;
        private readonly PaddleController _paddle;
        private readonly PaddleHistory _history;
        private readonly PaddleView _paddleView;
        private readonly DemoConfig _config;
        private readonly ShopController _shopController;
        private readonly ThreatLifecycleManager _threats;

        private bool _started;
        private bool _paused;
        private readonly bool _rulesEnabled;
        private int _lastBeat = int.MinValue;
        private int _lastAidaFireBeat = int.MinValue;
        private int _aidaShotCount;
        private int _flavorIndex;
        private bool _clearAnnounced;
        private float _calDebounceUntil;
        private double _lastPaddleDsp = double.NegativeInfinity;
        private readonly List<AidaShotView> _arrivedShots = new List<AidaShotView>();
        private TutorialDriver _tutorial;

        public int AidaLevel => _shopController != null ? _shopController.AidaLevel : 1;

        public int RegenLevel => _shopController != null ? _shopController.RegenLevel : 0;

        public bool IsPaused => _paused;

        public ShopController Shop => _shopController;

        public ThreatLifecycleManager Threats => _threats;

        /// <summary>When set, the tutorial owns boot + W1T (no auto StartWave).</summary>
        public TutorialDriver Tutorial
        {
            get => _tutorial;
            set
            {
                _tutorial = value;
                if (_shopController != null)
                {
                    _shopController.Tutorial = value;
                }
            }
        }

        /// <summary>When true, Start() subscribes but waits for an external StartWave.</summary>
        public bool DeferFirstWave { get; set; }

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
            PaddleController paddle,
            PaddleHistory history,
            PaddleView paddleView,
            DemoConfig config,
            ShopController shopController,
            ThreatLifecycleManager threatManager,
            SongPlayer song = null)
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
            _paddle = paddle ?? throw new ArgumentNullException(nameof(paddle));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _paddleView = paddleView;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _shopController = shopController ?? throw new ArgumentNullException(nameof(shopController));
            _threats = threatManager ?? throw new ArgumentNullException(nameof(threatManager));
            _song = song;
            _rulesEnabled = config.enableCodePanel;
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _lastBeat = int.MinValue;
            _aida?.SetLevel(AidaLevel, animate: false);

            _input.TapPressed += HandleTapPressed;
            _input.TapReleased += HandleTapReleased;
            _input.PausePressed += TogglePause;
            _rules.RuleCommitted += HandleRuleCommitted;
            _director.WaveStarted += OnWaveStarted;

            _threats.ImpactReached += ResolveAtImpact;
            _threats.TimeoutReached += ResolveTimeout;
            _threats.HoldCompleted += OnHoldCompleted;

            _paddleView?.Initialize();
            try { Cursor.lockState = CursorLockMode.Confined; } catch { }
            if (!DeferFirstWave)
            {
                _director.StartWave(0);
            }
        }

        public void Tick()
        {
            if (!_started)
            {
                return;
            }

            PollCalibration();

            if (_paused)
            {
                return;
            }

            if (Keyboard.current != null)
            {
                // Cheat F6: Pulih HP dan Energy penuh
                if (Keyboard.current.f6Key.wasPressedThisFrame)
                {
                    DevFullHeal();
                    return;
                }

                // Cheat F7 atau U: Tambah 999 PTS instan
                if (Keyboard.current.f7Key.wasPressedThisFrame || Keyboard.current.uKey.wasPressedThisFrame)
                {
                    DevAddPts(999);
                    return;
                }

                // Cheat F8: Skip 1 wave + tambah 999 PTS
                if (Keyboard.current.f8Key.wasPressedThisFrame)
                {
                    DevSkipSingleWave(999);
                    return;
                }

                // Cheat F9: Skip semua wave sampai Outro
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    DevFinishAllWaves();
                    return;
                }

                // Cheat F10 atau B: Langsung lompat ke Boss Fight AIDA
                if (Keyboard.current.f10Key.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame)
                {
                    DevJumpToBossFight();
                    return;
                }

                // Cheat F11: Fast forward linimasa musik (+16 Beat)
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    DevFastForward(16);
                    return;
                }

                // Cheat F12: Skip langsung ke Magnum Opus Ending AIDA
                if (Keyboard.current.f12Key.wasPressedThisFrame)
                {
                    DevSkipToMagnumOpusEnding();
                    return;
                }
            }

            UpdatePaddleAim();

            int beat = _clock.CurrentBeatIndex;
            if (beat != _lastBeat)
            {
                _lastBeat = beat;
                _rules.OnBeat(beat, new BeatContext(beat, _clockSource.NowDsp));
                PushRuleGuards();
                _threats.SpawnDue(_director.ActiveRunner, beat, _hud);
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

            int aidaFireEvery = _shopController.AidaFireEvery;
            if (beat > 0 && aidaFireEvery > 0
                && beat % aidaFireEvery == 0 && beat != _lastAidaFireBeat)
            {
                _lastAidaFireBeat = beat;
                TryAidaFire(beat, now);
            }

            _threats.UpdateThreats(now, beatPhase, beat);

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

                ActiveThreat target = _threats.FindActiveById(targetId);
                if (target == null || target.Resolved)
                {
                    _rules.ReportTargetNotFound();
                    continue;
                }

                ApplyHit(target, TimingGrade.Good, "aida", blockFlash: false);
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

            _paddleView?.Park(
                ArenaPresenter.AngleToLocal(_paddle.CurrentAngle, _arena.RingRadius),
                _paddle.CurrentAngle);
            _arena.SetBeatPulse(beatPhase);
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
            _song?.Pause();
            _paused = true;

            _threats.ClearBufferedInput();
            _hud.SetStatus(Application.isMobilePlatform ? "II PAUSED: TAP II TO RESUME" : "PAUSED: press ESC to resume");
        }

        public void Resume()
        {
            if (!_started || !_paused)
            {
                return;
            }

            _clock.Resume();
            _metronome.Resume();
            _song?.Resume();
            _paused = false;
            _hud.SetStatus("RESUMED");
        }

        public void Dispose()
        {
            _input.TapPressed -= HandleTapPressed;
            _input.TapReleased -= HandleTapReleased;
            _input.PausePressed -= TogglePause;
            _rules.RuleCommitted -= HandleRuleCommitted;
            _director.WaveStarted -= OnWaveStarted;

            _threats.ImpactReached -= ResolveAtImpact;
            _threats.TimeoutReached -= ResolveTimeout;
            _threats.HoldCompleted -= OnHoldCompleted;

            _shopController.Dispose();
        }

        private void OnWaveStarted(int index)
        {
            _clearAnnounced = false;
            _aida?.ClearShots();
            _hud.ShowWaveSummary(string.Empty);
            string waveId = _director.ActiveRunner.Data.WaveId;
            _hud.SetStatus($"WAVE {waveId} START");
            _metronome.Resync();

            int need = ArenaPresenter.MaxConcurrentThreats(_director.ActiveRunner.Data.Threats);
            int have = _arena.PooledViewCount;
            if (need > have)
            {
#if UNITY_EDITOR
                Debug.LogError($"AlgoRhythm: wave {waveId} butuh {need} ThreatView bersamaan, pool hanya {have}. Tambah view di SampleScene.");
#else
                Debug.LogWarning($"AlgoRhythm: wave {waveId} butuh {need} ThreatView bersamaan, pool hanya {have}.");
#endif
            }
        }

        private void HandleRuleCommitted()
        {
            _sfx.PlayPew();
            PushRuleGuards();
        }

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

            return 1 << (int)target.Value;
        }

        private void PollCalibration()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.unscaledTime < _calDebounceUntil)
            {
                return;
            }

            bool dec = keyboard.leftBracketKey.isPressed;
            bool inc = keyboard.rightBracketKey.isPressed;
            if (!dec && !inc)
            {
                return;
            }

            double current = _judge.CalibrationOffsetMs;
            double next = dec ? current - 5.0 : current + 5.0;
            _judge.CalibrationOffsetMs = next;
            _calDebounceUntil = Time.unscaledTime + 0.12f;

            try
            {
                PlayerPrefs.SetFloat("AlgoRhythm.CalibrationOffsetMs", (float)_judge.CalibrationOffsetMs);
                PlayerPrefs.Save();
            }
            catch { }

            _hud.SetStatus($"OFFSET: {_judge.CalibrationOffsetMs:+0.0;-0.0;0.0} ms");
        }

        private void UpdatePaddleAim()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector2 cursor = _input.AimPosition;
            Vector3 coreScreen = cam.WorldToScreenPoint(_arena.transform.position);
            double now = _clockSource.NowDsp;

            float dx = cursor.x - coreScreen.x;
            float dy = cursor.y - coreScreen.y;
            float distPx = Mathf.Sqrt(dx * dx + dy * dy);
            if (distPx >= _config.aimDeadZonePx)
            {
                float angle = AngleMath.AngleFromVector(dx, dy);
                _paddle.SetTarget(angle);
            }

            _paddle.Update(now, double.IsNegativeInfinity(_lastPaddleDsp) ? now : _lastPaddleDsp);
            _lastPaddleDsp = now;
            _history.Record(now, _paddle.CurrentAngle);
            if (_paddleView != null)
            {
                _paddleView.SetHalfArc(_config.paddleHalfArcDeg);
            }
        }

        private float PaddleAngleAt(double dsp, TapSource source)
        {
            double query = dsp - _judge.CalibrationOffsetMs / 1000.0;
            if (source == TapSource.Click && _config.clickAimLookbackMs > 0f)
            {
                query -= _config.clickAimLookbackMs / 1000.0;
            }
            return _history.AngleAt(query);
        }

        private void ResolveAtImpact(ActiveThreat active)
        {
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

        private void HandleTapPressed(TapSource source, double inputDsp)
        {
            if (!_started)
            {
                return;
            }

            if (_paused)
            {
                _hud.SetStatus("PAUSED: press ESC to resume");
                return;
            }

            float paddleAngle = PaddleAngleAt(inputDsp, source);
            ActiveThreat candidate = _threats.FindBestTapCandidate(paddleAngle, inputDsp);
            if (candidate == null)
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
                    candidate.BufferedSource = source;
                }

                _paddleView?.Pulse();
                return;
            }

            TimingGrade grade = _judge.Grade(inputDsp, impactDsp);
            if (candidate.Threat.Kind == ThreatKind.AdwareDecoy && grade != TimingGrade.Miss)
            {
                candidate.Resolved = true;
                _arena.FlashAtAngle(candidate.Threat.AngleDeg, 1.6f, 0.35f);
                ResolveDecoy(candidate, shielded: true, fromRule: false);
                return;
            }

            if (candidate.Threat.Kind == ThreatKind.Holder && grade != TimingGrade.Miss)
            {
                StartHold(candidate, grade, inputDsp, source);
                return;
            }

            _paddleView?.Pulse();
            ApplyHit(candidate, grade, "input", blockFlash: true);
        }

        private void HandleTapReleased(TapSource source, double inputDsp)
        {
            if (!_started || _paused)
            {
                return;
            }

            _threats.HandleTapReleased(source, inputDsp, active => Resolve(active, TimingGrade.Miss, "released"));
        }

        private void OnHoldCompleted(ActiveThreat active, TimingGrade grade)
        {
            Resolve(active, grade, "hold");
        }

        private void ApplyHit(ActiveThreat active, TimingGrade grade, string reason, bool blockFlash)
        {
            if (active.Resolved)
            {
                return;
            }

            int needed = _threats.ArmorFor(active.Threat.Kind);
            if (grade != TimingGrade.Miss && active.HitsTaken + 1 < needed)
            {
                active.HitsTaken++;
                int remaining = needed - active.HitsTaken;
                _arena.SetArmorVisual(active.Threat.Id, remaining, needed);
                if (blockFlash)
                {
                    _arena.FlashAtAngle(active.Threat.AngleDeg, 1.2f, 0.25f);
                    _sfx.PlayPew();
                }
                return;
            }

            Resolve(active, grade, reason, blockFlash);
        }

        private void StartHold(ActiveThreat active, TimingGrade grade, double inputDsp, TapSource source)
        {
            active.Holding = true;
            active.HoldGrade = grade;
            active.HoldStartDsp = inputDsp;
            active.HoldSource = source;
            _paddleView?.Pulse();
            _arena.FlashAtAngle(active.Threat.AngleDeg, 1.2f, 0.25f);
        }

        private void ResolveManual(ActiveThreat active, TimingGrade grade)
        {
            if (_rulesEnabled)
            {
                _rules.TryLogManualPrecedence(active.Threat);
            }

            TapSource source = active.BufferedSource ?? TapSource.Z;
            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                active.Resolved = true;
                _arena.FlashAtAngle(active.Threat.AngleDeg, 1.6f, 0.35f);
                ResolveDecoy(active, shielded: true, fromRule: false);
                return;
            }

            if (active.Threat.Kind == ThreatKind.Holder && grade != TimingGrade.Miss)
            {
                StartHold(active, grade, _clockSource.NowDsp, source);
                return;
            }

            _paddleView?.Pulse();
            ApplyHit(active, grade, "input", blockFlash: true);
        }

        private void ResolveAuto(ActiveThreat active, RuleFired fired)
        {
            active.Resolved = true;
            _paddle.SetTarget(active.Threat.AngleDeg);
            _paddleView?.Pulse();

            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                _arena.FlashAtAngle(active.Threat.AngleDeg, 1.6f, 0.35f);
                ResolveDecoy(active, shielded: true, fromRule: true);
                return;
            }

            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            int armor = _threats.ArmorFor(active.Threat.Kind);
            if (active.HitsTaken + 1 < armor)
            {
                active.HitsTaken++;
                _arena.SetArmorVisual(active.Threat.Id, armor - active.HitsTaken, armor);
                _paddleView?.Pulse();
                return;
            }

            active.HitsTaken++;
            if (_combat.ResolveThreat(active.Threat, TimingGrade.Good, fromRule: true, damage: 0))
            {
                _arena.FlashAtAngle(active.Threat.AngleDeg, 1.6f, 0.35f);
                _arena.ResolveThreat(active.Threat.Id, TimingGrade.Good, correctDirection: true, destroyed: true);
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

            _arena.ResolveDecoyView(active.Threat.Id, shielded, destroyed: shielded);
            if (shielded)
            {
                _sfx.PlayDestroy();
                _shake.AddTrauma(_config.shakeDestroy);
            }
        }

        private void TryAidaFire(int beat, double now)
        {
            if (Tutorial != null && Tutorial.OwnsWave(_director.ActiveRunner.Data.WaveId))
            {
                return;
            }

            CycleAidaFlavor();

            ActiveThreat target = _threats.FindAidaTarget(beat, now);
            if (target == null)
            {
                return;
            }

            double impactDsp = _clock.BeatTime(target.Threat.ImpactBeat);
            int hitEvery = Mathf.Max(1, _shopController.AidaHitEvery);
            bool willHit = (_aidaShotCount % hitEvery) == hitEvery - 1;
            float laneAngle = target.Threat.AngleDeg;
            double arrivalDsp = impactDsp - _clock.SecondsPerBeat * 0.25;
            if (arrivalDsp <= now)
            {
                arrivalDsp = now + 0.1;
            }

            var motionMode = _config.noteMotionMode == 1 ? NoteMotionMode.Stepped : NoteMotionMode.Linear;
            Vector3 towerPos = _aida.transform.position;
            double spawnDsp = _clock.BeatTime(target.Threat.SpawnBeat);
            float arrivalProgress = NoteMotion.Progress(
                spawnDsp, impactDsp, arrivalDsp,
                _clock.SecondsPerBeat, motionMode, _config.noteStepAttack);
            Vector3 meet = _arena.transform.position
                + _arena.ThreatLocalPosition(target.Threat.AngleDeg, arrivalProgress);
            float aimAngle;
            Vector3 to;
            if (willHit)
            {
                to = meet;
                aimAngle = Mathf.Atan2(to.x - towerPos.x, to.y - towerPos.y) * Mathf.Rad2Deg;
                _aida.SetFlavor($"Awas {LaneName(target.Threat.AngleDeg)}!");
            }
            else
            {
                aimAngle = laneAngle + ((_aidaShotCount % 2 == 0) ? 25f : -25f);
                float rad = aimAngle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
                float dist = (meet - towerPos).magnitude;
                if (dist <= 0.01f)
                {
                    dist = (_arena.transform.position - towerPos).magnitude;
                }
                to = towerPos + dir * dist;
            }

            _aidaShotCount++;
            _aida.SetAim(aimAngle);

            AidaShotView view = _aida.FireShot(
                _aida.MuzzleWorldPosition(), to, target.Threat.Id, willHit, arrivalDsp);
            if (view != null)
            {
                _sfx.PlayAidaFire();
            }
        }

        private static string LaneName(float angleDeg)
        {
            float norm = Mathf.Repeat(angleDeg, 360f);
            if (norm >= 315f || norm < 45f) return "UTARA";
            if (norm >= 45f && norm < 135f) return "TIMUR";
            if (norm >= 135f && norm < 225f) return "SELATAN";
            return "BARAT";
        }

        private void CycleAidaFlavor()
        {
            string[] lines = _config.aidaFlavorLines;
            if (lines == null || lines.Length == 0)
            {
                return;
            }

            _aida.SetFlavor(lines[_flavorIndex]);
            _flavorIndex = (_flavorIndex + 1) % lines.Length;
        }

        private void Resolve(ActiveThreat active, TimingGrade grade, string reason, bool blockFlash = true)
        {
            _ = reason;
            if (active.Resolved)
            {
                return;
            }

            int damage = 0;
            if (grade == TimingGrade.Miss && active.Threat.Kind != ThreatKind.AdwareDecoy)
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
            if (success)
            {
                if (_rulesEnabled && grade == TimingGrade.Perfect)
                {
                    _combat.AddEnergy(1);
                }

                if (blockFlash)
                {
                    _arena.FlashAtAngle(
                        active.Threat.AngleDeg,
                        grade == TimingGrade.Perfect ? 2.0f : 1.6f,
                        grade == TimingGrade.Perfect ? 0.45f : 0.35f);
                }
            }

            _arena.ResolveThreat(active.Threat.Id, grade, correctDirection: success, destroyed: grade != TimingGrade.Miss);
            if (grade != TimingGrade.Miss)
            {
                _sfx.PlayDestroy();
                _shake.AddTrauma(grade == TimingGrade.Perfect
                    ? _config.shakePerfect
                    : _config.shakeDestroy);
            }
            else if (damage > 0)
            {
                _shake.AddTrauma(_config.shakeDamage);
            }
        }

        /// <summary>
        /// Dev cheat: Full heal & energy restore.
        /// </summary>
        public void DevFullHeal()
        {
            if (_combat == null) return;
            _combat.Heal(_combat.MaxIntegrity);
            _combat.AddEnergy(10);
            _hud?.SetStatus($"DEV: PULIH PENUH (HP: {_combat.Integrity}/{_combat.MaxIntegrity}, ENERGY: {_combat.Energy})");
        }

        /// <summary>
        /// Dev cheat: skips exactly 1 wave and grants bonus PTS.
        /// If in Tutorial, graduates and advances. If in wave, resolves threats and opens shop.
        /// If in Shop, grants PTS, advances to next wave, and clears it.
        /// </summary>
        public void DevSkipSingleWave(int bonusPts = 999)
        {
            if (!_started || _director.AllWavesCleared)
            {
                return;
            }

            if (_paused)
            {
                Resume();
            }

            _combat.AddPts(bonusPts);

            if (_shopController.InShop)
            {
                _shopController.CloseShop();
                _director.Advance();
                if (!_director.AllWavesCleared)
                {
                    DevClearCurrentWave();
                    AnnounceWaveClear();
                }
                _hud?.SetStatus($"DEV: NEXT WAVE SKIPPED + {bonusPts} PTS (TOTAL: {_combat.CurrencyPts})");
                return;
            }

            string waveId = _director.ActiveRunner.Data.WaveId;
            if (Tutorial != null && Tutorial.OwnsWave(waveId))
            {
                Tutorial.DevGraduate();
                _hud?.SetStatus($"DEV: TUTORIAL SKIPPED + {bonusPts} PTS (TOTAL: {_combat.CurrencyPts})");
                return;
            }

            DevClearCurrentWave();
            AnnounceWaveClear();
            _hud?.SetStatus($"DEV: WAVE {waveId} SKIPPED + {bonusPts} PTS (TOTAL: {_combat.CurrencyPts})");
        }

        /// <summary>
        /// Dev cheat: grants bonus PTS directly to combat state and refreshes shop if open.
        /// </summary>
        public void DevAddPts(int amount = 999)
        {
            if (_combat == null) return;
            _combat.AddPts(amount);
            if (_shopController != null && _shopController.InShop)
            {
                _shopController.RenderShop();
            }
            _hud?.SetStatus($"DEV: +{amount} PTS GRANTED (TOTAL: {_combat.CurrencyPts})");
        }

        /// <summary>
        /// Dev cheat: skips straight to the Magnum Opus ending sequence by loading BossFightScene with auto-trigger.
        /// </summary>
        public void DevSkipToMagnumOpusEnding()
        {
            PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerEnding", 1);
            UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
        }

        /// <summary>
        /// Dev cheat: jumps straight to the Cyber Pong Boss Fight scene.
        /// </summary>
        public void DevJumpToBossFight()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("BossFightScene");
        }

        /// <summary>
        /// Dev cheat: fast-forwards the beat timeline and music playback by the given number of beats.
        /// </summary>
        public void DevFastForward(int beats = 16)
        {
            if (!_started) return;
            _clock.FastForward(beats);
            if (_song != null)
            {
                _song.FastForward((float)(beats * _clock.SecondsPerBeat));
            }
            _hud?.SetStatus($"DEV: PERCEPAT +{beats} BEAT (BEAT AKTIF: {_clock.CurrentBeatIndex})");
        }

        public void DevFinishAllWaves()
        {
            if (!_started)
            {
                return;
            }

            if (_paused)
            {
                Resume();
            }

            int guard = 0;
            while (!_director.AllWavesCleared && guard++ < 32)
            {
                if (_shopController.InShop)
                {
                    _shopController.CloseShop();
                    _director.Advance();
                    continue;
                }

                if (Tutorial != null
                    && Tutorial.OwnsWave(_director.ActiveRunner.Data.WaveId))
                {
                    Tutorial.DevGraduate();
                    continue;
                }

                DevClearCurrentWave();
                AnnounceWaveClear();
            }
        }

        private void DevClearCurrentWave()
        {
            WaveRunner runner = _director.ActiveRunner;
            IReadOnlyList<ActiveThreat> activeList = _threats.ActiveThreats;
            for (int index = activeList.Count - 1; index >= 0; index--)
            {
                ActiveThreat active = activeList[index];
                if (!active.Resolved)
                {
                    Resolve(active, TimingGrade.Perfect, "devskip", blockFlash: false);
                }
            }

            foreach (ThreatEvent threat in runner.Data.Threats)
            {
                if (runner.IsResolved(threat.Id) || threat.Kind == ThreatKind.AdwareDecoy)
                {
                    continue;
                }

                _combat.ResolveThreat(threat, TimingGrade.Perfect, fromRule: false, damage: 0);
            }

            runner.ForceResolveAll();
        }

        private void AnnounceWaveClear()
        {
            _clearAnnounced = true;
            if (_rulesEnabled)
            {
                _combat.AddEnergy(1);
            }

            _shopController.ApplyWaveClearRegen();

            string waveId = _director.ActiveRunner.Data.WaveId;
            bool lastWave = _director.ActiveIndex + 1 >= _director.WaveCount;
            string counts =
                $"P {_combat.PerfectCount}  G {_combat.GoodCount}  M {_combat.MissCount}";
            _hud.ShowWaveSummary(lastWave
                ? $"RUN CLEAR: {waveId} selesai\nSCORE {_combat.Score} · {counts}\n" +
                  $"damage {_combat.DamageTaken} · decoy {_combat.DecoyPenaltyTotal}"
                : string.Empty);
            _hud.SetStatus(lastWave
                ? "RUN CLEAR: semua wave selesai"
                : $"WAVE CLEAR {waveId}: semua threat resolved");

            _arena.ClearThreats();
            _aida?.ClearShots();
            if (Tutorial != null && Tutorial.OwnsWave(waveId))
            {
                Tutorial.OnWaveCleared();
                return;
            }

            if (lastWave)
            {
                _director.Advance();
                return;
            }

            _shopController.OpenShop();
        }

        private void RetryCurrentWave()
        {
            _arena.ClearThreats();
            _threats.Clear();
            _combat.Reset(_config.maxIntegrity, _config.startingEnergy);
            _metronome.Pause();
            _director.RetryCurrent();
            _metronome.Resume();
            _hud.SetStatus("RETRY: wave restarted");
        }
    }
}
