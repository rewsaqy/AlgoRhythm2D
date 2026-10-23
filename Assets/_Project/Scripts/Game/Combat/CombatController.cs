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
    /// Satu hasil final per threat, oleh input atau timeout. Integrity 0
    /// retry sub-wave (A5); clear wave memajukan director (1A→1B).
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
        private readonly ShopView _shop;
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
        private double _lastPaddleDsp = double.NegativeInfinity;
        private readonly List<AidaShotView> _arrivedShots = new List<AidaShotView>();

        // Shop run state: two tracks, Lv1-5 (regen 0-5). PERSISTENT for the
        // whole run including tutorial/retry; reset only on full restart.
        // _aidaLevel doubles as the hidden dependency marker the future
        // boss reads to scale its rhythm (harder if you fed AIDA a lot).
        private bool _inShop;
        private int _regenLevel;
        private int _aidaLevel = 1;
        private int _aidaHitEvery;
        private int _aidaFireEvery;

        /// <summary>Hidden dependency marker for the future AIDA boss.</summary>
        public int AidaLevel => _aidaLevel;

        /// <summary>Player regen level (0 = none).</summary>
        public int RegenLevel => _regenLevel;

        private sealed class ActiveThreat
        {
            public ThreatEvent Threat;
            public bool Resolved;
            public bool ImpactEvaluated;
            public double? BufferedInputDsp;
            public TapSource? BufferedSource;
            public int HitsTaken;
            public bool Holding;
            public TimingGrade HoldGrade;
            public double HoldStartDsp;
            public TapSource HoldSource;
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
            PaddleController paddle,
            PaddleHistory history,
            PaddleView paddleView,
            ShopView shop,
            DemoConfig config,
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
            _shop = shop;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _song = song;
            _rulesEnabled = config.enableCodePanel;
        }

        public bool IsPaused => _paused;

        /// <summary>When set, the tutorial owns boot + W1T (no auto StartWave).</summary>
        public TutorialDriver Tutorial { get; set; }

        /// <summary>When true, Start() subscribes but waits for an external StartWave.</summary>
        public bool DeferFirstWave { get; set; }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _lastBeat = int.MinValue;
            _aidaHitEvery = AidaTable(_config.shopAidaHitEvery, 1, 8);
            _aidaFireEvery = AidaTable(_config.shopAidaFireEvery, 1, 4);

            _input.TapPressed += HandleTapPressed;
            _input.TapReleased += HandleTapReleased;
            _input.PausePressed += TogglePause;
            _rules.RuleCommitted += HandleRuleCommitted;
            _director.WaveStarted += OnWaveStarted;
            if (_shop != null)
            {
                _shop.Confirmed += OnShopConfirmed;
                _shop.Hide();
            }
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

#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                DevFinishAllWaves();
                return;
            }
#endif

            UpdatePaddleAim();

            int beat = _clock.CurrentBeatIndex;
            if (beat != _lastBeat)
            {
                _lastBeat = beat;
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

            if (beat > 0 && _aidaFireEvery > 0
                && beat % _aidaFireEvery == 0 && beat != _lastAidaFireBeat)
            {
                _lastAidaFireBeat = beat;
                TryAidaFire(beat, now);
            }

            var motionMode = _config.noteMotionMode == 1 ? NoteMotionMode.Stepped : NoteMotionMode.Linear;
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
                float progress = NoteMotion.Progress(
                    spawnDsp, impactDsp, now,
                    _clock.SecondsPerBeat, motionMode, _config.noteStepAttack);
                _arena.UpdateThreat(
                    active.Threat,
                    progress,
                    active.BufferedInputDsp.HasValue,
                    (impactDsp - now) * 1000.0,
                    beatPhase);

                if (now >= impactDsp && !active.ImpactEvaluated)
                {
                    active.ImpactEvaluated = true;
                    if (active.Threat.Kind != ThreatKind.Holder)
                    {
                        ResolveAtImpact(active);
                    }
                }

                SyncArchetypeVisual(active, now, beat);

                double timeoutDsp = impactDsp + _judge.GoodWindowMs / 1000.0;
                if (active.Threat.Kind == ThreatKind.Holder)
                {
                    timeoutDsp = _clock.BeatTime(
                        active.Threat.ImpactBeat + Mathf.Max(1, _config.holdBeats));
                }

                if (!active.Resolved && now > timeoutDsp)
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

            // Satu-satunya model: Paddle berputar 360° mengikuti sudut aim.
            // Shields/Shield orbiter lawas sudah dihapus dari scene.
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
            _sfx.Pause();
            _song?.Pause();
            _paused = true;
            for (int index = 0; index < _active.Count; index++)
            {
                _active[index].BufferedInputDsp = null;
                _active[index].BufferedSource = null;
            }

            _hud.SetStatus("PAUSED: press ESC to resume");
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
            if (_shop != null)
            {
                _shop.Confirmed -= OnShopConfirmed;
            }
        }

        private void OnWaveStarted(int index)
        {
            _clearAnnounced = false;
            _hud.ShowWaveSummary(string.Empty);
            string waveId = _director.ActiveRunner.Data.WaveId;
            _hud.SetStatus($"WAVE {waveId} START");
            // Dialogue (typewriter panel + one-liners) is owned by the
            // TutorialDriver through DialogueView; nothing to show here.
            _metronome.Resync();

            // Fase 0: validasi pool — fail keras di Editor bila note bisa
            // aktif bersamaan melebihi view pool.
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

        /// <summary>
        /// Kalibrasi di [ dan ] (5 ms, clamp ±150, PlayerPrefs).
        /// Dipindah dari Z/X agar tap tidak menggeser offset.
        /// </summary>
        private void PollCalibration()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.unscaledTime < _calDebounceUntil)
            {
                return;
            }

            int step = 0;
            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                step -= 5;
            }

            if (keyboard.rightBracketKey.wasPressedThisFrame)
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
        /// Aim paddle dari posisi kursor → sudut terhadap Core.
        /// Dead zone di sekitar Core menahan sudut terakhir (anti jitter atan2).
        /// </summary>
        private void UpdatePaddleAim()
        {
            double now = _clockSource.NowDsp;
            Vector2 cursor = _input.AimPosition;
            if (cursor == Vector2.zero)
            {
                cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f + 100f);
            }

            Camera cam = Camera.main;
            Vector3 coreWorld = _arena.transform.position;
            Vector3 coreScreen = cam != null ? cam.WorldToScreenPoint(coreWorld) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f);
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
            // Konsisten dengan TimingJudge.ErrorMs = (input-impact)*1000 - offset:
            // offset positif menganggap input terjadi lebih awal, jadi sampel
            // paddle juga digeser lebih awal dengan -offset.
            double query = dsp - _judge.CalibrationOffsetMs / 1000.0;
            if (source == TapSource.Click)
            {
                query -= _config.clickAimLookbackMs / 1000.0;
            }
            return _history.AngleAt(query);
        }

        private void SpawnDue(int beat)
        {
            WaveRunner wave = _director.ActiveRunner;
            int count = wave.Due(beat, _spawnBuffer);
            for (int index = 0; index < count; index++)
            {
                ThreatEvent threat = _spawnBuffer[index];
                float travelMs = (float)((_clock.BeatTime(threat.ImpactBeat)
                                          - _clock.BeatTime(threat.SpawnBeat)) * 1000.0);
                // Perbaiki urutan: note hanya masuk _active bila ada view.
                if (!_arena.SpawnThreat(threat, travelMs))
                {
                    continue;
                }
                _active.Add(new ActiveThreat { Threat = threat });
                if (threat.Kind == ThreatKind.Heavy)
                {
                    int armor = ArmorFor(threat.Kind);
                    _arena.SetArmorVisual(threat.Id, armor, armor);
                }
            }

            if (wave.AllSpawned && wave.IsCleared)
            {
                _hud.SetStatus("WAVE CLEAR: all threats resolved");
            }
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
            ActiveThreat candidate = FindBestTapCandidate(paddleAngle, inputDsp);
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

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved || !active.Holding
                    || active.HoldSource != source)
                {
                    continue;
                }

                active.Holding = false;
                Resolve(active, TimingGrade.Miss, "released");
                return;
            }
        }

        private void ApplyHit(ActiveThreat active, TimingGrade grade, string reason, bool blockFlash)
        {
            int armor = ArmorFor(active.Threat.Kind);
            active.HitsTaken++;
            if (active.HitsTaken < armor)
            {
                _arena.SetArmorVisual(active.Threat.Id, armor - active.HitsTaken, armor);
                _paddleView?.Pulse();
                return;
            }

            Resolve(active, grade, reason, blockFlash);
        }

        /// <summary>Hits to resolve a threat: armor for Heavies, 1 otherwise.</summary>
        private int ArmorFor(ThreatKind kind)
        {
            return kind == ThreatKind.Heavy ? Mathf.Max(2, _config.heavyArmorHits) : 1;
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

        private void SyncArchetypeVisual(ActiveThreat active, double now, int beat)
        {
            if (active.Resolved)
            {
                return;
            }

            if (active.Threat.Kind == ThreatKind.Sleeper)
            {
                bool armed = active.Threat.ImpactBeat - beat <= 2;
                _arena.SetSleeperVisual(active.Threat.Id, armed);
                return;
            }

            if (active.Threat.Kind == ThreatKind.Holder && active.Holding)
            {
                int tailBeat = active.Threat.ImpactBeat + Mathf.Max(1, _config.holdBeats);
                double tailDsp = _clock.BeatTime(tailBeat);
                double span = Math.Max(0.01, tailDsp - active.HoldStartDsp);
                float progress = Mathf.Clamp01((float)((now - active.HoldStartDsp) / span));
                _arena.SetHoldVisual(active.Threat.Id, true, progress);
                if (now >= tailDsp)
                {
                    active.Holding = false;
                    Resolve(active, active.HoldGrade, "hold");
                }
            }
        }

        private void ResolveManual(ActiveThreat active, TimingGrade grade)
        {
            _rules.TryLogManualPrecedence(active.Threat);
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
            int armor = ArmorFor(active.Threat.Kind);
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
            // The tutorial drill belongs to the player: AIDA holds fire
            // while the driver owns the active wave.
            if (Tutorial != null && Tutorial.OwnsWave(_director.ActiveRunner.Data.WaveId))
            {
                return;
            }

            CycleAidaFlavor();

            ActiveThreat target = FindAidaTarget(beat, now);
            if (target == null)
            {
                return;
            }

            double impactDsp = _clock.BeatTime(target.Threat.ImpactBeat);
            int hitEvery = Mathf.Max(1, _aidaHitEvery);
            bool willHit = (_aidaShotCount % hitEvery) == hitEvery - 1;
            float laneAngle = target.Threat.AngleDeg;
            double arrivalDsp = impactDsp - _clock.SecondsPerBeat * 0.25;
            if (arrivalDsp <= now)
            {
                arrivalDsp = now + 0.1;
            }

            var motionMode = _config.noteMotionMode == 1 ? NoteMotionMode.Stepped : NoteMotionMode.Linear;
            Vector3 towerPos = _aida.transform.position;
            // Titik temu bila tembakan tepat: posisi real note saat arrival.
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
                // Peluru ikut arah laras: error sudut yang sama dengan aim,
                // jarak tempuh sama dengan skenario kena (balistik identik).
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

        private ActiveThreat FindAidaTarget(int beat, double now)
        {
            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved
                    || active.Threat.Kind == ThreatKind.AdwareDecoy)
                {
                    continue;
                }

                if (active.Threat.ImpactBeat - beat < 2)
                {
                    continue;
                }

                double distanceMs = (_clock.BeatTime(active.Threat.ImpactBeat) - now) * 1000.0;
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

        private static string LaneName(float angleDeg)
        {
            Direction dir = ArenaPresenter.AngleToDirection(angleDeg);
            switch (dir)
            {
                case Direction.North: return "Utara";
                case Direction.East: return "Timur";
                case Direction.South: return "Selatan";
                case Direction.West: return "Barat";
                default: return "?";
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

        /// <summary>
        /// Kandidat tap: note aktif yang ditutupi paddle, dipilih yang
        /// |inputDsp − impactDsp| paling kecil. Menggantikan FindNearestThreat per-lane.
        /// </summary>
        private ActiveThreat FindBestTapCandidate(float paddleAngleDeg, double inputDsp)
        {
            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;
            float paddleHalf = _config.paddleHalfArcDeg;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved)
                {
                    continue;
                }

                // Center-only: note tertutup bila pusat note masuk busur paddle.
                // noteHalfWidthDeg hanya visual; tidak ikut judging agar
                // diagonal 45° (mis. NE) tidak menutupi N dan E sekaligus.
                if (!AngleMath.Overlaps(active.Threat.AngleDeg, 0f, paddleAngleDeg, paddleHalf))
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
            if (success && blockFlash)
            {
                _arena.FlashAtAngle(
                    active.Threat.AngleDeg,
                    grade == TimingGrade.Perfect ? 2.0f : 1.6f,
                    grade == TimingGrade.Perfect ? 0.45f : 0.35f);
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

#if UNITY_EDITOR
        /// <summary>
        /// Dev-only (F9): instantly finishes the whole run through the
        /// natural clear path — tutorial graduates, each wave resolves all
        /// threats as Perfect, shop LANJUT auto-confirms. Stripped from
        /// builds; demo acceptance runs never see it.
        /// </summary>
        public void DevFinishAllWaves()
        {
            if (!_started || _paused)
            {
                return;
            }

            int guard = 0;
            while (!_director.AllWavesCleared && guard++ < 32)
            {
                if (_inShop)
                {
                    _inShop = false;
                    if (_shop != null)
                    {
                        _shop.Hide();
                    }

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

        /// <summary>
        /// Dev-only: resolves every threat of the active wave as Perfect —
        /// spawned ones through the normal Resolve path, not-yet-spawned
        /// ones directly in combat state. The runner cursor is drained too,
        /// so no resolved threat can spawn later as a stuck ghost.
        /// </summary>
        private void DevClearCurrentWave()
        {
            WaveRunner runner = _director.ActiveRunner;
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                ActiveThreat active = _active[index];
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
#endif

        private void AnnounceWaveClear()
        {
            _clearAnnounced = true;
            if (_rulesEnabled)
            {
                _combat.AddEnergy(1);
            }

            // Regen track: heal on every wave clear (Lv0 = none).
            int regen = RegenForLevel(_regenLevel);
            if (regen > 0)
            {
                _combat.Heal(regen);
            }
            string waveId = _director.ActiveRunner.Data.WaveId;
            bool lastWave = _director.ActiveIndex + 1 >= _director.WaveCount;
            string counts =
                $"P {_combat.PerfectCount}  G {_combat.GoodCount}  M {_combat.MissCount}";
            // Ringkasan skor hanya di wave paling terakhir; wave tengah
            // langsung lanjut ke shop tanpa panel ringkasan.
            _hud.ShowWaveSummary(lastWave
                ? $"RUN CLEAR: {waveId} selesai\nSCORE {_combat.Score} · {counts}\n" +
                  $"damage {_combat.DamageTaken} · decoy {_combat.DecoyPenaltyTotal}"
                : string.Empty);
            _hud.SetStatus(lastWave
                ? "RUN CLEAR: semua wave selesai"
                : $"WAVE CLEAR {waveId}: semua threat resolved");
            // Dialogue (typewriter panel) is owned by the TutorialDriver.

            _arena.ClearThreats();
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

            OpenShop();
        }

        /// <summary>
        /// Between-wave shop: game idles on the cleared wave (clock keeps
        /// running, metronome keeps playing, but nothing can spawn) until
        /// the player confirms LANJUT. Stackable items, keyboard-only.
        /// </summary>
        private void OpenShop()
        {
            if (_shop == null)
            {
                _director.Advance();
                return;
            }

            _inShop = true;
            if (_song != null)
            {
                _song.StopSong();
            }

            RenderShop();
            _shop.Show();
        }

        private void OnShopConfirmed(int slot)
        {
            if (!_inShop)
            {
                return;
            }

            if (slot == ShopView.SlotContinue)
            {
                _inShop = false;
                _shop.Hide();
                _director.Advance();
                return;
            }

            BuyShopItem(slot);
        }

        /// <summary>
        /// Two upgrade tracks, Lv1-5 each. Regen heals on wave clear;
        /// AIDA gains accuracy + fire rate (and secretly raises the
        /// dependency marker the future boss will read).
        /// </summary>
        private void BuyShopItem(int slot)
        {
            int maxLevel = Mathf.Max(1, _config.shopMaxLevel);
            int cost;
            bool maxed;
            if (slot == ShopView.SlotRegen)
            {
                cost = UpgradeCost(_regenLevel);
                maxed = _regenLevel >= maxLevel;
            }
            else if (slot == ShopView.SlotAida)
            {
                cost = UpgradeCost(_aidaLevel - 1);
                maxed = _aidaLevel >= maxLevel;
            }
            else
            {
                return;
            }

            if (maxed)
            {
                _hud.SetStatus("Sudah MAX");
                RenderShop();
                return;
            }

            if (!_combat.TrySpendPts(cost))
            {
                _hud.SetStatus("PTS tidak cukup");
                RenderShop();
                return;
            }

            if (slot == ShopView.SlotRegen)
            {
                _regenLevel = Mathf.Min(maxLevel, _regenLevel + 1);
                _sfx.PlayDestroy();
            }
            else
            {
                _aidaLevel = Mathf.Min(maxLevel, _aidaLevel + 1);
                _aidaHitEvery = AidaTable(_config.shopAidaHitEvery, _aidaLevel, 8);
                _aidaFireEvery = AidaTable(_config.shopAidaFireEvery, _aidaLevel, 4);
                _sfx.PlayPew();
            }

            RenderShop();
        }

        private int UpgradeCost(int currentLevel)
        {
            int[] costs = _config.shopUpgradeCosts;
            if (costs == null || costs.Length == 0)
            {
                return int.MaxValue;
            }

            return Mathf.Max(0, costs[Mathf.Clamp(currentLevel, 0, costs.Length - 1)]);
        }

        private int AidaTable(int[] table, int level, int fallback)
        {
            if (table == null || table.Length == 0)
            {
                return fallback;
            }

            return Mathf.Max(1, table[Mathf.Clamp(level - 1, 0, table.Length - 1)]);
        }

        private int RegenForLevel(int level)
        {
            int[] table = _config.shopRegenPerClear;
            if (table == null || table.Length == 0)
            {
                return 0;
            }

            return Mathf.Max(0, table[Mathf.Clamp(level, 0, table.Length - 1)]);
        }

        private void RenderShop()
        {
            if (_shop == null)
            {
                return;
            }

            int maxLevel = Mathf.Max(1, _config.shopMaxLevel);
            string regenRow = _regenLevel >= maxLevel
                ? $"REGEN Lv{maxLevel} (MAX)"
                : $"REGEN Lv{_regenLevel + 1} +{RegenForLevel(_regenLevel + 1)}HP/clear: {UpgradeCost(_regenLevel)} PTS";
            int nextAida = Mathf.Min(maxLevel, _aidaLevel + 1);
            string aidaRow = _aidaLevel >= maxLevel
                ? $"AIDA Lv{maxLevel} (MAX)"
                : $"AIDA Lv{nextAida} {AidaTable(_config.shopAidaHitEvery, nextAida, 8)}r/{AidaTable(_config.shopAidaFireEvery, nextAida, 4)}b: {UpgradeCost(_aidaLevel - 1)} PTS";
            _shop.Render(_combat.CurrencyPts, new[]
            {
                regenRow,
                aidaRow,
                "LANJUT →",
            });
        }

        private void RetryCurrentWave()
        {
            _arena.ClearThreats();
            _active.Clear();
            _combat.Reset(_config.maxIntegrity, _config.startingEnergy);
            _metronome.Pause();
            _director.RetryCurrent();
            _metronome.Resume();
            _hud.SetStatus("RETRY: wave restarted");
        }
    }
}
