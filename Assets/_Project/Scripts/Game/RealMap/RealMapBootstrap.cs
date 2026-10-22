using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Core.Waves;
using AlgoRhythm.Game.Data;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Input;
using AlgoRhythm.Game.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.RealMap
{
    /// <summary>
    /// REAL map full-mechanics wiring. Same gameplay systems as the SampleScene
    /// prototype (DSP beat clock, DSP-timestamped input, positional shield,
    /// W1A/W1B waves, decoy/energy/retry, one-final-result guard) but with no
    /// UI: no HUD, labels, dialogue, results, metronome or effects. Feedback
    /// is purely spatial — cubes advance to Core, the shield orbits Core, and
    /// blocked/hit cubes hide. Decoys read as a 45-degree diamond, never by
    /// colour alone. Rule composition needs the code-panel UI so the rule
    /// runtime stays dormant here, exactly as in SampleScene while
    /// enableCodePanel is false. AIDA/audio have no scene objects here and
    /// are omitted. Tuning defaults mirror DemoConfig [A]. When the final
    /// wave clears, the loop restarts at wave 0 [A] for continuous play.
    /// </summary>
    public sealed class RealMapBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform core;
        [SerializeField] private Transform shield;
        [SerializeField] private Transform[] enemies = new Transform[3];
        [SerializeField] private InputRouter input;
        [SerializeField] private WaveDefinitionSO[] waveDefinitions;

        [Header("Tuning [A] — mirrors DemoConfig defaults")]
        [SerializeField] private double bpm = 100.0;
        [SerializeField] private double perfectWindowMs = 70.0;
        [SerializeField] private double goodWindowMs = 130.0;
        [SerializeField] private int inputBufferMs = 120;
        [SerializeField] private int startLeadInBeats = 2;
        [SerializeField] private int energyRegenEveryBeats = 4;
        [SerializeField] private float spawnDistance = 4f;
        [SerializeField] private float orbitRadius = 1f;

        private DspClockSource _clockSource;
        private BeatClock _clock;
        private TimingJudge _judge;
        private CombatState _combat;
        private PaddleController _paddle;
        private PaddleHistory _history;
        private WaveDirector _director;
        private readonly List<ActiveThreat> _active = new List<ActiveThreat>();
        private readonly ThreatEvent[] _spawnBuffer = new ThreatEvent[8];
        private bool _started;
        private bool _paused;
        private int _lastBeat = int.MinValue;

        private sealed class ActiveThreat
        {
            public ThreatEvent Threat;
            public bool Resolved;
            public bool ImpactEvaluated;
            public double? BufferedInputDsp;
        }

        private void Awake()
        {
            if (core == null || shield == null || input == null
                || enemies == null || enemies.Length == 0
                || waveDefinitions == null || waveDefinitions.Length == 0)
            {
                Debug.LogError(
                    "RealMapBootstrap requires Core, Shield, at least one enemy " +
                    "transform, the InputRouter reference and wave definitions; " +
                    "no runtime fallback is created.",
                    this);
                enabled = false;
                return;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                {
                    Debug.LogError($"RealMapBootstrap enemy slot {i} is missing.", this);
                    enabled = false;
                    return;
                }
            }

            _clockSource = new DspClockSource();
            _clock = new BeatClock(_clockSource, bpm);
            _judge = new TimingJudge(perfectWindowMs, goodWindowMs);
            _combat = new CombatState();
            _paddle = new PaddleController(180f, 0f);
            _history = new PaddleHistory(256);

            var waves = new List<WaveData>(waveDefinitions.Length);
            foreach (WaveDefinitionSO definition in waveDefinitions)
            {
                if (definition == null)
                {
                    Debug.LogError("RealMapBootstrap wave definition is missing.", this);
                    enabled = false;
                    return;
                }

                waves.Add(WaveMapper.ToWaveData(definition));
            }

            _director = new WaveDirector(waves, _clock, _clockSource, startLeadInBeats);

            input.Initialize(_clockSource);
            input.TapPressed += HandleTapPressed;
            input.PausePressed += TogglePause;
            _director.StartWave(0);
            HideAllEnemies();
            _started = true;
        }

        private void Update()
        {
            if (!_started || _paused)
            {
                return;
            }

            Tick();
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.TapPressed -= HandleTapPressed;
                input.PausePressed -= TogglePause;
            }
        }

        private void Tick()
        {
            int beat = _clock.CurrentBeatIndex;
            if (beat != _lastBeat)
            {
                _lastBeat = beat;
                SpawnDue(beat);
                if (beat > 0 && energyRegenEveryBeats > 0
                    && beat % energyRegenEveryBeats == 0)
                {
                    _combat.AddEnergy(1);
                }
            }

            double now = _clockSource.NowDsp;
            Vector3 corePos = core.position;

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
                Transform enemy = enemies[EnemyIndex(active.Threat.Id)];
                enemy.position = Vector3.Lerp(
                    SpawnPosition(active.Threat.AngleDeg, corePos),
                    corePos,
                    ProgressAt(spawnDsp, impactDsp, now));

                if (now >= impactDsp && !active.ImpactEvaluated)
                {
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
                RetryCurrent();
                return;
            }

            if (_director.ActiveRunner.IsCleared)
            {
                _combat.AddEnergy(1);
                if (!_director.Advance())
                {
                    _director.StartWave(0);
                }

                _active.Clear();
                HideAllEnemies();
            }

            float angle = _paddle.CurrentAngle;
            double nowPaddle = now;
            UpdatePaddleAim(nowPaddle);
            angle = _paddle.CurrentAngle;
            float radians = angle * Mathf.Deg2Rad;
            shield.position = corePos + new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * orbitRadius;
            shield.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdatePaddleAim(double nowDsp)
        {
            Vector2 cursor = input != null ? input.AimPosition : Vector2.zero;
            if (cursor == Vector2.zero)
            {
                cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f + 100f);
            }
            Camera cam = Camera.main;
            Vector3 coreScreen = cam != null ? cam.WorldToScreenPoint(core.position) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f);
            float dx = cursor.x - coreScreen.x;
            float dy = cursor.y - coreScreen.y;
            if (Mathf.Sqrt(dx * dx + dy * dy) >= 40f)
            {
                _paddle.SetTarget(AngleMath.AngleFromVector(dx, dy));
            }
            _paddle.SnapToTarget();
            _history.Record(nowDsp, _paddle.CurrentAngle);
        }

        private void SpawnDue(int beat)
        {
            int count = _director.ActiveRunner.Due(beat, _spawnBuffer);
            for (int i = 0; i < count; i++)
            {
                ThreatEvent threat = _spawnBuffer[i];
                _active.Add(new ActiveThreat { Threat = threat });
                Transform enemy = enemies[EnemyIndex(threat.Id)];
                enemy.gameObject.SetActive(true);
                // Decoy reads as a diamond, never by colour alone.
                enemy.rotation = threat.Kind == ThreatKind.AdwareDecoy
                    ? Quaternion.Euler(0f, 0f, 45f)
                    : Quaternion.identity;
            }
        }

        private void ResolveAtImpact(ActiveThreat active)
        {
            double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
            if (active.BufferedInputDsp.HasValue)
            {
                TimingGrade buffered = _judge.Grade(active.BufferedInputDsp.Value, impactDsp);
                if (buffered != TimingGrade.Miss)
                {
                    ResolveManual(active, buffered);
                }
            }
        }

        private void ResolveTimeout(ActiveThreat active)
        {
            if (active.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                ResolveDecoy(active, shielded: false);
                return;
            }

            Resolve(active, TimingGrade.Miss, active.Threat.BaseDamage);
        }

        private void HandleTapPressed(TapSource source, double inputDsp)
        {
            if (!_started || _paused)
            {
                return;
            }

            float paddleAngle = PaddleAngleAt(inputDsp, source);
            ActiveThreat candidate = FindBest(paddleAngle, inputDsp);
            if (candidate == null)
            {
                return;
            }

            double impactDsp = _clock.BeatTime(candidate.Threat.ImpactBeat);
            double deltaMs = (inputDsp - impactDsp) * 1000.0;
            if (deltaMs > _judge.GoodWindowMs)
            {
                return;
            }

            double earliestMs = -(_judge.GoodWindowMs + inputBufferMs);
            if (deltaMs < earliestMs)
            {
                return;
            }

            if (deltaMs < -_judge.GoodWindowMs)
            {
                if (!candidate.BufferedInputDsp.HasValue
                    || Math.Abs(deltaMs) < Math.Abs((candidate.BufferedInputDsp.Value - impactDsp) * 1000.0))
                {
                    candidate.BufferedInputDsp = inputDsp;
                }

                return;
            }

            TimingGrade grade = _judge.Grade(inputDsp, impactDsp);
            if (candidate.Threat.Kind == ThreatKind.AdwareDecoy)
            {
                if (grade != TimingGrade.Miss)
                {
                    candidate.Resolved = true;
                    ResolveDecoy(candidate, shielded: true);
                }

                return;
            }

            if (grade != TimingGrade.Miss)
            {
                ResolveManual(candidate, grade);
            }
        }

        private void ResolveManual(ActiveThreat active, TimingGrade grade)
        {
            Resolve(active, grade, damage: 0);
        }

        private void Resolve(ActiveThreat active, TimingGrade grade, int damage)
        {
            if (active.Resolved)
            {
                return;
            }

            if (!_combat.ResolveThreat(active.Threat, grade, fromRule: false, damage: damage))
            {
                return;
            }

            active.Resolved = true;
            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            enemies[EnemyIndex(active.Threat.Id)].gameObject.SetActive(false);
        }

        private void ResolveDecoy(ActiveThreat active, bool shielded)
        {
            active.Resolved = true;
            _director.ActiveRunner.MarkResolved(active.Threat.Id);
            if (!_combat.ResolveDecoy(active.Threat, shielded, fromRule: false, out _))
            {
                return;
            }

            enemies[EnemyIndex(active.Threat.Id)].gameObject.SetActive(false);
        }

        private void RetryCurrent()
        {
            _active.Clear();
            _combat.Reset(CombatState.DefaultMaxIntegrity, CombatState.DefaultStartingEnergy);
            _director.RetryCurrent();
            HideAllEnemies();
        }

        private void TogglePause()
        {
            if (!_started)
            {
                return;
            }

            _paused = !_paused;
            if (_paused)
            {
                _clock.Pause();
                for (int i = 0; i < _active.Count; i++)
                {
                    _active[i].BufferedInputDsp = null;
                }
            }
            else
            {
                _clock.Resume();
            }
        }

        private float PaddleAngleAt(double inputDsp, TapSource source)
        {
            // Sama seperti CombatController: konsisten dengan
            // TimingJudge.ErrorMs (offset positif = sampel lebih awal).
            double query = inputDsp - _judge.CalibrationOffsetMs / 1000.0;
            if (source == TapSource.Click)
            {
                query -= 10.0 / 1000.0;
            }
            return _history.AngleAt(query);
        }

        private ActiveThreat FindBest(float paddleAngleDeg, double inputDsp)
        {
            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                ActiveThreat active = _active[i];
                if (active.Resolved)
                {
                    continue;
                }

                // Center-only STRICT: hanya pusat note vs busur paddle 12° agar
                // paddle harus tepat di jalur note (jam 8 tidak shield West).
                if (!AngleMath.Overlaps(active.Threat.AngleDeg, 0f, paddleAngleDeg, 12f))
                {
                    continue;
                }

                double distanceMs = Math.Abs((inputDsp - _clock.BeatTime(active.Threat.ImpactBeat)) * 1000.0);
                if (distanceMs < bestDistanceMs)
                {
                    bestDistanceMs = distanceMs;
                    best = active;
                }
            }

            return best;
        }

        /// <summary>
        /// Fixed pool mapping, validated against current W1A/W1B spacing: same
        /// pool-slot threats never overlap in time, so a telegraph never jumps
        /// cubes mid-flight. Judging never depends on this mapping.
        /// </summary>
        private int EnemyIndex(int threatId)
        {
            return Math.Abs(threatId) % enemies.Length;
        }

        private void HideAllEnemies()
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].gameObject.SetActive(false);
            }
        }

        private Vector3 SpawnPosition(float angleDeg, Vector3 corePos)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return corePos + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f) * spawnDistance;
        }

        private static float ProgressAt(double spawnDsp, double impactDsp, double nowDsp)
        {
            if (impactDsp <= spawnDsp)
            {
                return 1f;
            }

            if (nowDsp <= spawnDsp)
            {
                return 0f;
            }

            if (nowDsp >= impactDsp)
            {
                return 1f;
            }

            return (float)((nowDsp - spawnDsp) / (impactDsp - spawnDsp));
        }
    }
}
