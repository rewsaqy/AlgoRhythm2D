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
    /// Runtime state of an active threat traveling through the arena.
    /// Tracks hit count, hold sustain, buffered input, and resolution state.
    /// </summary>
    public sealed class ActiveThreat
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

    /// <summary>
    /// Manages the active threat queue, spawning, progress updates,
    /// archetype visual synchronization, timeout detection, and candidate lookup.
    /// Decoupled from CombatController to eliminate god-object coupling.
    /// </summary>
    public sealed class ThreatLifecycleManager
    {
        private readonly List<ActiveThreat> _active = new List<ActiveThreat>();
        private readonly ThreatEvent[] _spawnBuffer = new ThreatEvent[64];
        private readonly ArenaPresenter _arena;
        private readonly BeatClock _clock;
        private readonly TimingJudge _judge;
        private readonly DemoConfig _config;

        /// <summary>Invoked when an active threat reaches its impact DSP time.</summary>
        public event Action<ActiveThreat> ImpactReached;

        /// <summary>Invoked when an active threat exceeds its Good timeout window without being resolved.</summary>
        public event Action<ActiveThreat> TimeoutReached;

        /// <summary>Invoked when a holding note reaches its tail successfully.</summary>
        public event Action<ActiveThreat, TimingGrade> HoldCompleted;

        public IReadOnlyList<ActiveThreat> ActiveThreats => _active;

        public int ActiveCount => _active.Count;

        public ThreatLifecycleManager(
            ArenaPresenter arena,
            BeatClock clock,
            TimingJudge judge,
            DemoConfig config)
        {
            _arena = arena;
            _clock = clock;
            _judge = judge;
            _config = config;
        }

        public int ArmorFor(ThreatKind kind)
        {
            return kind == ThreatKind.Heavy
                ? Mathf.Max(2, _config != null ? _config.heavyArmorHits : 2)
                : 1;
        }

        /// <summary>
        /// Spawns threats due on the given beat from the active wave runner into the arena.
        /// Zero GC allocations: uses a preallocated buffer.
        /// </summary>
        public int SpawnDue(WaveRunner wave, int beat, HudView hud)
        {
            if (wave == null || _clock == null)
            {
                return 0;
            }

            int count = wave.Due(beat, _spawnBuffer);
            for (int index = 0; index < count; index++)
            {
                ThreatEvent threat = _spawnBuffer[index];
                float travelMs = (float)((_clock.BeatTime(threat.ImpactBeat)
                                          - _clock.BeatTime(threat.SpawnBeat)) * 1000.0);

                if (_arena != null && !_arena.SpawnThreat(threat, travelMs))
                {
                    continue;
                }

                _active.Add(new ActiveThreat { Threat = threat });
                if (threat.Kind == ThreatKind.Heavy && _arena != null)
                {
                    int armor = ArmorFor(threat.Kind);
                    _arena.SetArmorVisual(threat.Id, armor, armor);
                }
            }

            if (wave.AllSpawned && wave.IsCleared && hud != null)
            {
                hud.SetStatus("WAVE CLEAR: all threats resolved");
            }

            return count;
        }

        /// <summary>
        /// Updates positions, ring sizes, archetype visuals, and evaluates impact & timeouts.
        /// </summary>
        public void UpdateThreats(double now, double beatPhase, int beat)
        {
            if (_clock == null)
            {
                return;
            }

            var motionMode = (_config != null && _config.noteMotionMode == 1)
                ? NoteMotionMode.Stepped
                : NoteMotionMode.Linear;
            float stepAttack = _config != null ? _config.noteStepAttack : 0.25f;

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
                    _clock.SecondsPerBeat, motionMode, stepAttack);

                if (_arena != null)
                {
                    _arena.UpdateThreat(
                        active.Threat,
                        progress,
                        active.BufferedInputDsp.HasValue,
                        (impactDsp - now) * 1000.0,
                        beatPhase);
                }

                if (now >= impactDsp && !active.ImpactEvaluated)
                {
                    active.ImpactEvaluated = true;
                    if (active.Threat.Kind != ThreatKind.Holder)
                    {
                        ImpactReached?.Invoke(active);
                    }
                }

                SyncArchetypeVisual(active, now, beat);

                double timeoutDsp = impactDsp + (_judge != null ? _judge.GoodWindowMs : 130.0) / 1000.0;
                if (active.Threat.Kind == ThreatKind.Holder)
                {
                    int holdBeats = _config != null ? Mathf.Max(1, _config.holdBeats) : 4;
                    timeoutDsp = _clock.BeatTime(active.Threat.ImpactBeat + holdBeats);
                }

                if (!active.Resolved && now > timeoutDsp)
                {
                    TimeoutReached?.Invoke(active);
                }
            }
        }

        private void SyncArchetypeVisual(ActiveThreat active, double now, int beat)
        {
            if (active.Resolved || _arena == null || _clock == null)
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
                int holdBeats = _config != null ? Mathf.Max(1, _config.holdBeats) : 4;
                int tailBeat = active.Threat.ImpactBeat + holdBeats;
                double tailDsp = _clock.BeatTime(tailBeat);
                double span = Math.Max(0.01, tailDsp - active.HoldStartDsp);
                float progress = Mathf.Clamp01((float)((now - active.HoldStartDsp) / span));
                _arena.SetHoldVisual(active.Threat.Id, true, progress);
                if (now >= tailDsp)
                {
                    active.Holding = false;
                    HoldCompleted?.Invoke(active, active.HoldGrade);
                }
            }
        }

        /// <summary>
        /// Finds the active threat whose angular arc overlaps the paddle and has the lowest |input - impact| timing error.
        /// </summary>
        public ActiveThreat FindBestTapCandidate(float paddleAngleDeg, double inputDsp)
        {
            if (_clock == null)
            {
                return null;
            }

            float paddleHalfArc = _config != null ? _config.paddleHalfArcDeg : 12f;
            ActiveThreat best = null;
            double bestError = double.MaxValue;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved)
                {
                    continue;
                }

                bool covered = AngleMath.Overlaps(
                    active.Threat.AngleDeg,
                    active.Threat.HalfWidthDeg,
                    paddleAngleDeg,
                    paddleHalfArc);

                if (!covered)
                {
                    continue;
                }

                double impactDsp = _clock.BeatTime(active.Threat.ImpactBeat);
                double error = Math.Abs(inputDsp - impactDsp);
                if (error < bestError)
                {
                    bestError = error;
                    best = active;
                }
            }

            return best;
        }

        public ActiveThreat FindActiveById(int threatId)
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

        /// <summary>
        /// Finds an incoming threat candidate for the AIDA helper tower to shoot at.
        /// </summary>
        public ActiveThreat FindAidaTarget(int beat, double now)
        {
            if (_clock == null)
            {
                return null;
            }

            ActiveThreat best = null;
            double bestDistanceMs = double.MaxValue;

            for (int index = 0; index < _active.Count; index++)
            {
                ActiveThreat active = _active[index];
                if (active.Resolved || active.Threat.Kind == ThreatKind.AdwareDecoy)
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

        public void ClearBufferedInput()
        {
            for (int index = 0; index < _active.Count; index++)
            {
                _active[index].BufferedInputDsp = null;
                _active[index].BufferedSource = null;
            }
        }

        public void Clear()
        {
            _active.Clear();
        }

        public void HandleTapReleased(TapSource source, double inputDsp, Action<ActiveThreat> onMissRelease)
        {
            if (_clock == null)
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
                onMissRelease?.Invoke(active);
                return;
            }
        }
    }
}
