using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Presentation;
using UnityEngine;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Boot tutorial + song intros + ending teaser. Owns W1T end-to-end
    /// (CombatController defers the first StartWave and the W1T clear
    /// transition to this driver; graduation needs a Perfect, anything else
    /// retries the drill with zero damage). After graduation it plays the
    /// blocking pre-wave tutorial for each song (WS1: song + PTS + shop,
    /// WS2: new threat roster) and the blackout teaser after the last wave.
    /// Pure flow — judging stays in TimingJudge/CombatState, visuals in
    /// DialogueView/ArenaPresenter.
    /// </summary>
    public sealed class TutorialDriver : MonoBehaviour
    {
        public const string TutorialWaveId = "W1T";

        private enum Phase
        {
            Idle,
            Intro,
            Practice,
            Praise,
            SongIntro,
            Done
        }

        private WaveDirector _director;
        private CombatState _combat;
        private DialogueView _dialogue;
        private int _maxIntegrity;
        private int _startingEnergy;
        private Phase _phase = Phase.Idle;
        private TimingGrade _lastGrade = TimingGrade.Miss;
        private bool _lastResolved;

        public bool IsActive => _phase != Phase.Idle && _phase != Phase.Done;

        public bool OwnsWave(string waveId)
        {
            return IsActive && waveId == TutorialWaveId;
        }

        public void Initialize(
            WaveDirector director, CombatState combat, int maxIntegrity, int startingEnergy)
        {
            _director = director;
            _combat = combat;
            _maxIntegrity = maxIntegrity;
            _startingEnergy = startingEnergy;
            _dialogue = GetComponent<DialogueView>();
            if (_dialogue == null)
            {
                Debug.LogError("TutorialDriver needs a DialogueView on the same GameObject.", this);
                enabled = false;
                return;
            }

            _director.WaveStarted += OnWaveStarted;
            _combat.ThreatResolved += OnThreatResolved;
        }

        private void OnDestroy()
        {
            if (_director != null)
            {
                _director.WaveStarted -= OnWaveStarted;
            }

            if (_combat != null)
            {
                _combat.ThreatResolved -= OnThreatResolved;
            }

            if (_dialogue != null)
            {
                _dialogue.Finished -= OnDialogueFinished;
            }
        }

        /// <summary>Begins the greeting; wave WS1 waits until graduation.</summary>
        public void Begin()
        {
            if (_dialogue == null)
            {
                _director?.StartWave(0);
                _phase = Phase.Done;
                return;
            }

            _phase = Phase.Intro;
            _dialogue.Finished += OnDialogueFinished;
            _dialogue.Play(DemoDialogue.TutorialScript());
        }

        private void OnWaveStarted(int index)
        {
            if (_director == null || _dialogue == null)
            {
                return;
            }

            string waveId = _director.ActiveRunner != null
                ? _director.ActiveRunner.Data.WaveId
                : string.Empty;

            // The practice wave shows a persistent hint, not a one-liner.
            if (waveId == TutorialWaveId)
            {
                _phase = Phase.Practice;
                _lastResolved = false;
                _dialogue.ShowStatic(DemoDialogue.PracticeHint());
                return;
            }

            if (_phase == Phase.Done)
            {
                DemoDialogue.WaveStartLine(waveId, out string _, out string line);
                if (!string.IsNullOrEmpty(line))
                {
                    _dialogue.ShowLine("A.I.D.A", line);
                }
            }
        }

        private void OnThreatResolved(ThreatResolved resolved)
        {
            if (_phase != Phase.Practice)
            {
                return;
            }

            _lastGrade = resolved.Grade;
            _lastResolved = true;
        }

        /// <summary>Called by CombatController instead of advancing W1T.</summary>
        public void OnWaveCleared()
        {
            if (_phase != Phase.Practice)
            {
                return;
            }

            if (_lastResolved && _lastGrade == TimingGrade.Perfect)
            {
                _phase = Phase.Praise;
                _dialogue.Play(new List<DialogueLine>
                {
                    new DialogueLine("A.I.D.A", DemoDialogue.PracticePraise()),
                });
            }
            else
            {
                _dialogue.Play(new List<DialogueLine>
                {
                    new DialogueLine("A.I.D.A", DemoDialogue.PracticeRetry()),
                });
            }

            _lastResolved = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Dev-only: graduates the tutorial instantly without confirmations
        /// (used by the F9 finish-all dev shortcut). Mirrors the Praise
        /// branch of OnDialogueFinished.
        /// </summary>
        public void DevGraduate()
        {
            if (_phase == Phase.Done)
            {
                return;
            }

            _phase = Phase.Done;
            if (_dialogue != null)
            {
                _dialogue.Finished -= OnDialogueFinished;
                _dialogue.Stop();
            }

            if (_director != null)
            {
                // The drill threat is skipped, not played: seal the runner
                // so AllWavesCleared (outro trigger) still holds at RUN CLEAR.
                _director.ActiveRunner.ForceResolveAll();
                _director.Advance();
            }
        }
#endif

        /// <summary>
        /// Shop LANJUT hook: when the next wave has its own tutorial, play
        /// it blocking here and advance after. Returns true when consumed
        /// (the caller must NOT advance); false means advance directly.
        /// </summary>
        public bool TryConsumeShopContinue()
        {
            if (_phase != Phase.Done || _director == null || _dialogue == null)
            {
                return false;
            }

            string nextId = _director.WaveIdAt(_director.ActiveIndex + 1);
            IReadOnlyList<DialogueLine> script = DemoDialogue.SongIntroScript(nextId);
            if (script == null || script.Count == 0)
            {
                return false;
            }

            _phase = Phase.SongIntro;
            _dialogue.Finished += OnDialogueFinished;
            _dialogue.Play(script);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Dev-only: graduates the tutorial instantly without confirmations
        /// (used by the F9 finish-all dev shortcut). Mirrors the Praise
        /// branch of OnDialogueFinished.
        /// </summary>
        public void DevGraduate()
        {
            if (_phase == Phase.Done)
            {
                return;
            }

            _phase = Phase.Done;
            if (_dialogue != null)
            {
                _dialogue.Finished -= OnDialogueFinished;
                _dialogue.Stop();
            }

            if (_director != null)
            {
                // The drill threat is skipped, not played: seal the runner
                // so AllWavesCleared (outro trigger) still holds at RUN CLEAR.
                _director.ActiveRunner.ForceResolveAll();
                _director.Advance();
            }
        }
#endif

        private void OnDialogueFinished()
        {
            switch (_phase)
            {
                case Phase.Intro:
                    // Intro script done: spawn the single practice threat.
                    _director.StartWave(0);
                    break;
                case Phase.Praise:
                    // Graduated: teach the first song, then start it.
                    PlaySongIntroOrAdvance();
                    break;
                case Phase.SongIntro:
                    // Pre-wave tutorial done: start the wave, stay Done so
                    // the wave-start one-liner still shows.
                    _phase = Phase.Done;
                    _dialogue.Finished -= OnDialogueFinished;
                    _director.Advance();
                    break;
                default:
                    // Practice retry: confirm the hint, then rerun the drill
                    // with clean counters (no damage was ever taken).
                    _combat.Reset(_maxIntegrity, _startingEnergy);
                    _director.RetryCurrent();
                    break;
            }
        }

        private void PlaySongIntroOrAdvance()
        {
            IReadOnlyList<DialogueLine> script = DemoDialogue.SongIntroScript(
                _director != null ? _director.WaveIdAt(_director.ActiveIndex + 1) : string.Empty);
            if (script != null && script.Count > 0)
            {
                _phase = Phase.SongIntro;
                _dialogue.Play(script);
                return;
            }

            _phase = Phase.Done;
            _dialogue.Finished -= OnDialogueFinished;
            _director.Advance();
        }
    }
}
