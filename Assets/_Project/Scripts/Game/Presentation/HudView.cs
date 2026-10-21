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
        private const int BarWidth = 10;
        private const float StatusVisibleSeconds = 4f;

        private CombatState _combat;
        private BeatClock _clock;
        private WaveDirector _director;
        private TimingJudge _judge;
        private TextMesh _stats;
        private TextMesh _status;
        private TextMesh _dialogue;
        private TextMesh _pause;
        private TextMesh _clear;
        private string _statusText = string.Empty;
        private float _statusVisibleUntil;
        private string _clearSummary = string.Empty;
        private string _dialogueLine = string.Empty;
        private bool _showEnergy = true;

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
            _dialogue = FindText("Controls");
            _pause = FindText("Pause");
            _clear = FindText("Clear");

            UiTypography.Style(_stats, 34, 0.08f, UiTypography.Ink,
                TextAnchor.UpperLeft, TextAlignment.Left, 1.15f);
            UiTypography.Style(_status, 30, 0.08f, UiTypography.Status,
                TextAnchor.UpperLeft, TextAlignment.Left, 1.15f);
            UiTypography.Style(_dialogue, 26, 0.07f, UiTypography.Ink,
                TextAnchor.LowerCenter, TextAlignment.Center, 1.2f);
            UiTypography.Style(_pause, 52, 0.085f, UiTypography.Ink,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.25f);
            UiTypography.Style(_clear, 40, 0.08f, UiTypography.Good,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.25f);

            if (_dialogue != null)
            {
                // Authored active in the scene; position is fixed there too.
                _dialogue.text = string.Empty;
            }

            if (_status != null)
            {
                _status.text = string.Empty;
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

        /// <summary>
        /// Shows a dialogue subtitle addressed by text_id. Routing stays on
        /// the id; only the line is shown (no debug id on screen).
        /// </summary>
        public void ShowDialogue(string textId, string line)
        {
            _ = textId;
            _dialogueLine = line ?? string.Empty;
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
                int filled = Mathf.Clamp(
                    Mathf.RoundToInt((float)_combat.Integrity / max * BarWidth), 0, BarWidth);
                string bar = new string('#', filled) + new string('-', BarWidth - filled);
                string waveId = _director != null && _director.ActiveRunner != null
                    ? _director.ActiveRunner.Data.WaveId
                    : "--";
                int beat = _clock.IsStarted ? _clock.CurrentBeatIndex : 0;
                double calibration = _judge != null ? _judge.CalibrationOffsetMs : 0.0;
                string calSign = calibration >= 0 ? "+" : "-";
                string energySegment = _showEnergy
                    ? $"ENERGY {_combat.Energy}   "
                    : "MANUAL ONLY   ";
                _stats.text =
                    $"INTEGRITY [{bar}] {_combat.Integrity}/{_combat.MaxIntegrity}\n" +
                    $"{energySegment}SCORE {_combat.Score}\n" +
                    $"P {_combat.PerfectCount}  G {_combat.GoodCount}  M {_combat.MissCount}   " +
                    $"BEAT {beat} · {waveId} · {(int)_clock.Bpm} BPM\n" +
                    $"CAL {calSign}{Mathf.Abs((float)calibration):0}ms [Z/X]"
                    + (_combat.Integrity <= max * 30 / 100 ? "\n!! LOW INTEGRITY" : string.Empty);
                _stats.color = _combat.Integrity <= max * 30 / 100
                    ? UiTypography.Warn
                    : UiTypography.Ink;
            }

            if (_status != null)
            {
                _status.text = !string.IsNullOrEmpty(_statusText)
                    && Time.unscaledTime <= _statusVisibleUntil
                    ? _statusText
                    : string.Empty;
            }

            if (_dialogue != null)
            {
                _dialogue.text = _dialogueLine;
            }

            if (_pause != null)
            {
                _pause.text = _clock.IsPaused
                    ? "II PAUSED\nESC / P lanjutkan\nWASD shield · Z/X kalibrasi\n1/2/3 slot · Q/E ganti · ENTER pasang · R reset"
                    : string.Empty;
            }

            if (_clear != null)
            {
                if (_clearSummary.Length > 0)
                {
                    _clear.text = _clearSummary;
                }
                else
                {
                    bool cleared = _director != null
                                   && _director.ActiveRunner != null
                                   && _director.ActiveRunner.IsCleared;
                    _clear.text = cleared ? "WAVE CLEAR" : string.Empty;
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
