using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Minimal greybox HUD. Every label is a manually authored TextMesh in the
    /// scene; this component only updates text and keeps the HUD anchored to
    /// the camera. Text is the source of truth, not colour.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private CombatState _combat;
        private BeatClock _clock;
        private WaveDirector _director;
        private Camera _camera;
        private TextMesh _stats;
        private TextMesh _status;
        private TextMesh _controls;
        private TextMesh _pause;
        private TextMesh _clear;
        private string _statusText = string.Empty;
        private string _clearSummary = string.Empty;

        public void Initialize(CombatState combat, BeatClock clock, WaveDirector director)
        {
            _combat = combat;
            _clock = clock;
            _director = director;
            _camera = Camera.main;
            _stats = FindText("Stats");
            _status = FindText("Status");
            _controls = FindText("Controls");
            _pause = FindText("Pause");
            _clear = FindText("Clear");
            if (_controls != null)
            {
                _controls.text = string.Empty;
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

            // Player-facing only: pause, resume, clear, retry, run clear.
            // Per-frame debug spam (telegraph, buffered, per-threat grade)
            // is ignored; grades already show on ThreatView/Grade.
            if (status.Contains("PAUSED")
                || status.Contains("RESUMED")
                || status.Contains("WAVE CLEAR")
                || status.Contains("RUN CLEAR")
                || status.Contains("RETRY"))
            {
                _statusText = status;
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

            UpdateText();
            AnchorToCamera();
        }

        private void UpdateText()
        {
            if (_stats != null)
            {
                _stats.text =
                    $"INTEGRITY {_combat.Integrity}/{_combat.MaxIntegrity}    ENERGY {_combat.Energy}\n" +
                    $"SCORE {_combat.Score}";
            }

            if (_status != null)
            {
                _status.text = _statusText;
            }

            if (_pause != null)
            {
                _pause.text = _clock.IsPaused ? "PAUSED\nESC to resume" : string.Empty;
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

        private void AnchorToCamera()
        {
            Camera camera = _camera != null ? _camera : Camera.main;
            if (camera == null)
            {
                return;
            }

            const float depth = 10.0f;
            Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
            transform.position = new Vector3(topLeft.x + 0.35f, topLeft.y - 0.35f, 0f);

            Vector3 center = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            if (_pause != null)
            {
                _pause.transform.position = new Vector3(center.x, center.y + 0.7f, -0.2f);
            }

            if (_clear != null)
            {
                _clear.transform.position = new Vector3(center.x, center.y - 1.0f, -0.2f);
            }
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }
    }
}
