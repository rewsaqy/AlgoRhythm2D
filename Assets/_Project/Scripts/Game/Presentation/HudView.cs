using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Grouped greybox HUD. Every label is a manually authored TextMesh in the
    /// scene; this component only styles and updates text and keeps the HUD
    /// anchored to the camera. Text carries meaning, never colour alone:
    /// grades, counts, wave ids and reasons are always spelled out.
    /// Layout: stats group top-left, transient status under it, dialogue
    /// subtitle bottom-center, pause/clear overlays centered.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float StatusVisibleSeconds = 4f;

        private CombatState _combat;
        private BeatClock _clock;
        private WaveDirector _director;
        private TimingJudge _judge;
        private TextMesh _stats;
        private TextMesh _status;
        private TextMesh _pause;
        private TextMesh _clear;
        private string _statusText = string.Empty;
        private float _statusVisibleUntil;
        private string _clearSummary = string.Empty;
        private bool _showEnergy = true;
        private IntegrityBarView _bar;

        // Dirty-checking cache to eliminate per-frame GC allocations and TextMesh rebuilds
        private int _cachedScore = -1;
        private int _cachedPts = -1;
        private int _cachedP = -1;
        private int _cachedG = -1;
        private int _cachedM = -1;
        private int _cachedBeat = -1;
        private int _cachedEnergy = -1;
        private int _cachedIntegrity = -1;
        private string _cachedWaveId = string.Empty;
        private double _cachedCalibration = double.NaN;
        private bool _cachedShowEnergy;
        private bool _cachedLowIntegrity;
        private string _cachedStatusString = null;
        private bool _cachedPaused;
        private string _cachedClearString = null;

        /// <summary>
        /// Called by the bootstrap from DemoConfig.enableCodePanel. When the
        /// rule/energy system is off, ENERGY is hidden (manual-shield only).
        /// Reversible: set back to true when the final-AIDA rule system lands.
        /// </summary>
        public void SetRuleSystemEnabled(bool enabled)
        {
            _showEnergy = enabled;
        }

        public void Initialize(CombatState combat, BeatClock clock, WaveDirector director, TimingJudge judge = null)
        {
            _combat = combat;
            _clock = clock;
            _director = director;
            _judge = judge;
            _stats = FindText("Stats");
            _status = FindText("Status");
            _pause = FindText("Pause");
            _clear = FindText("Clear");
            _bar = GetComponentInChildren<IntegrityBarView>(true);

            UiTypography.Style(_stats, 34, 0.08f, UiTypography.Ink,
                TextAnchor.UpperLeft, TextAlignment.Left, 1.15f);
            UiTypography.Style(_status, 30, 0.08f, UiTypography.Status,
                TextAnchor.UpperLeft, TextAlignment.Left, 1.15f);
            UiTypography.Style(_pause, 30, 0.08f, UiTypography.Ink,
                TextAnchor.MiddleRight, TextAlignment.Right, 1.25f);
            UiTypography.Style(_clear, 40, 0.08f, UiTypography.Good,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.25f);

            if (_status != null)
            {
                _status.text = string.Empty;
            }

            if (_stats != null)
            {
                // Clear the baked placeholder so stale scene text never
                // shows on the first frame; LateUpdate rewrites it anyway.
                _stats.text = string.Empty;
            }
        }

        public void SetStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                _statusText = string.Empty;
                return;
            }

            // Player-facing only: pause, resume, wave banners, clear, retry,
            // run clear. Per-frame debug spam (telegraph, buffered, per-threat
            // grade) is ignored; grades already show on ThreatView/Grade.
            if (status.Contains("PAUSED")
                || status.Contains("RESUMED")
                || status.Contains("WAVE ")
                || status.Contains("RULE ")
                || status.Contains("RUN CLEAR")
                || status.Contains("RETRY"))
            {
                _statusText = status;
                // Transient toast: wave banners and run updates fade on
                // their own; the wave id stays on the stats line.
                _statusVisibleUntil = Time.unscaledTime + StatusVisibleSeconds;
            }
        }

        public void ShowWaveSummary(string summary)
        {
            _clearSummary = summary ?? string.Empty;
        }

        private void LateUpdate()
        {
            if (_combat == null || _clock == null)
            {
                return;
            }

            // No camera anchoring: every label sits at its scene-authored
            // position, so Scene view and runtime match exactly.
            UpdateText();
        }

        private void UpdateText()
        {
            if (_stats != null)
            {
                int max = Mathf.Max(1, _combat.MaxIntegrity);
                int integrity = _combat.Integrity;
                if (integrity != _cachedIntegrity)
                {
                    _cachedIntegrity = integrity;
                    float fraction = (float)integrity / max;
                    if (_bar != null)
                    {
                        _bar.SetFraction(fraction);
                    }
                }

                string waveId = _director != null && _director.ActiveRunner != null
                    ? _director.ActiveRunner.Data.WaveId
                    : "--";
                int beat = _clock.IsStarted ? _clock.CurrentBeatIndex : 0;
                double calibration = _judge != null ? _judge.CalibrationOffsetMs : 0.0;
                int score = _combat.Score;
                int pts = _combat.CurrencyPts;
                int p = _combat.PerfectCount;
                int g = _combat.GoodCount;
                int m = _combat.MissCount;
                int energy = _combat.Energy;
                bool lowIntegrity = integrity <= max * 30 / 100;

                // Dirty check: only format string and rebuild TextMesh when values change
                if (score != _cachedScore
                    || pts != _cachedPts
                    || p != _cachedP
                    || g != _cachedG
                    || m != _cachedM
                    || beat != _cachedBeat
                    || energy != _cachedEnergy
                    || lowIntegrity != _cachedLowIntegrity
                    || _showEnergy != _cachedShowEnergy
                    || waveId != _cachedWaveId
                    || calibration != _cachedCalibration)
                {
                    _cachedScore = score;
                    _cachedPts = pts;
                    _cachedP = p;
                    _cachedG = g;
                    _cachedM = m;
                    _cachedBeat = beat;
                    _cachedEnergy = energy;
                    _cachedLowIntegrity = lowIntegrity;
                    _cachedShowEnergy = _showEnergy;
                    _cachedWaveId = waveId;
                    _cachedCalibration = calibration;

                    string calSign = calibration >= 0 ? "+" : "-";
                    string energySegment = _showEnergy
                        ? $"ENERGY {energy}   "
                        : "MANUAL ONLY   ";

                    _stats.text =
                        $"{energySegment}SCORE {score}  PTS {pts}\n" +
                        $"P {p}  G {g}  M {m}   " +
                        $"BEAT {beat} · {waveId} · {(int)_clock.Bpm} BPM\n" +
                        $"CAL {calSign}{Mathf.Abs((float)calibration):0}ms [ ]"
                        + (lowIntegrity ? "\n!! LOW INTEGRITY" : string.Empty);

                    _stats.color = lowIntegrity
                        ? UiTypography.Warn
                        : UiTypography.Ink;
                }
            }

            if (_status != null)
            {
                string targetStatus = !string.IsNullOrEmpty(_statusText) && Time.unscaledTime <= _statusVisibleUntil
                    ? _statusText
                    : string.Empty;

                if (targetStatus != _cachedStatusString)
                {
                    _cachedStatusString = targetStatus;
                    _status.text = targetStatus;
                }
            }

            if (_pause != null)
            {
                bool isPaused = _clock.IsPaused;
                if (isPaused != _cachedPaused)
                {
                    _cachedPaused = isPaused;
                    _pause.text = isPaused ? "II PAUSED · ESC" : string.Empty;
                }
            }

            if (_clear != null)
            {
                string targetClear;
                if (_clearSummary.Length > 0)
                {
                    targetClear = _clearSummary;
                }
                else
                {
                    bool cleared = _director != null
                                   && _director.ActiveRunner != null
                                   && _director.ActiveRunner.IsCleared;
                    targetClear = cleared ? "WAVE CLEAR" : string.Empty;
                }

                if (targetClear != _cachedClearString)
                {
                    _cachedClearString = targetClear;
                    _clear.text = targetClear;
                }
            }
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }
    }
}
