using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Run-clear outro: black fade in, KAMU blackout line, 4 black-white
    /// flicker cycles, black fade in again, breach monologue (AIDA confused,
    /// DATA TELAH DIJEBOL + siren, read-only terminal, TO BE CONTINUED),
    /// then back to the main menu where the demo stops. Pure presentation
    /// over the idle run-clear state; judging already ended. Lines confirm
    /// with ENTER like the tutorial.
    /// </summary>
    public sealed class OutroDriver : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Delay,
            BlackIn,
            Line,
            Flicker,
            BlackInAgain,
            Monologue,
            BlackOut,
            Done
        }

    /// <summary>Dialogue stays readable: blackout stops at near-black.</summary>
        private const float ReadableBlackoutAlpha = 0.88f;

        [Header("Timing [U]")]
        [Min(0f)] [SerializeField] private float startDelaySeconds = 1.5f;
        [Min(1)] [SerializeField] private int flickerCycles = 4;
        [Min(0.05f)] [SerializeField] private float flickerHalfPeriod = 0.25f;

        private WaveDirector _director;
        private DialogueView _dialogue;
        private BlackoutView _blackout;
        private SfxPlayer _sfx;
        private BeatClock _clock;
        private Phase _phase = Phase.Idle;
        private float _phaseAt;
        private float _blackTarget = 0f;
        private bool _dialogueDone;
        private readonly System.Collections.Generic.List<GameObject> _keepOff =
            new System.Collections.Generic.List<GameObject>();

        public bool IsActive => _phase != Phase.Idle && _phase != Phase.Done;

        public void Initialize(
            WaveDirector director,
            DialogueView dialogue,
            BlackoutView blackout,
            BeatClock clock,
            SfxPlayer sfx = null)
        {
            _director = director;
            _dialogue = dialogue;
            _blackout = blackout;
            _clock = clock;
            _sfx = sfx;
            if (_dialogue != null)
            {
                _dialogue.Finished -= OnDialogueFinished;
                _dialogue.Finished += OnDialogueFinished;
            }
        }

        private void OnDestroy()
        {
            if (_dialogue != null)
            {
                _dialogue.Finished -= OnDialogueFinished;
            }
        }

        /// <summary>Begins the outro once, right after RUN CLEAR.</summary>
        public void Begin()
        {
            if (_phase != Phase.Idle)
            {
                return;
            }

            _phase = Phase.Delay;
            _phaseAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_phase == Phase.Idle || _phase == Phase.Done)
            {
                return;
            }

            bool paused = _clock != null && _clock.IsPaused;
            if (_blackout != null)
            {
                _blackout.Paused = paused;
            }

            if (paused)
            {
                return;
            }

            switch (_phase)
            {
                case Phase.Delay:
                    if (Time.unscaledTime - _phaseAt >= startDelaySeconds)
                    {
                        EnterBlackIn();
                    }

                    break;
                case Phase.BlackIn:
                    if (!IsBlackoutBusy() && BlackoutAlpha() >= _blackTarget - 0.02f)
                    {
                        EnterLine();
                    }

                    break;
                case Phase.Line:
                    if (_dialogueDone)
                    {
                        EnterFlicker();
                    }

                    break;
                case Phase.Flicker:
                    if (!IsBlackoutBusy())
                    {
                        EnterBlackInAgain();
                    }

                    break;
                case Phase.BlackInAgain:
                    if (!IsBlackoutBusy() && BlackoutAlpha() >= _blackTarget - 0.02f)
                    {
                        EnterMonologue();
                    }

                    break;
                case Phase.Monologue:
                    if (_dialogueDone)
                    {
                        EnterBlackOut();
                    }

                    break;
                case Phase.BlackOut:
                    if (!IsBlackoutBusy() && BlackoutAlpha() <= 0.01f)
                    {
                        if (_dialogue != null)
                        {
                            _dialogue.Stop();
                        }

                        FinishToMenu();
                    }

                    break;
            }
        }

        private void EnterBlackIn()
        {
            _phase = Phase.BlackIn;
            _blackTarget = ReadableBlackoutAlpha;
            SetSceneVisible(false);
            if (_blackout != null)
            {
                _blackout.FadeTo(ReadableBlackoutAlpha);
            }
            else
            {
                EnterLine();
            }
        }

        private void EnterLine()
        {
            _phase = Phase.Line;
            _dialogueDone = false;
            if (_dialogue != null)
            {
                _dialogue.Play(new System.Collections.Generic.List<DialogueLine>
                {
                    OutroDialogue.BlackoutLine(),
                });
            }
            else
            {
                _dialogueDone = true;
            }
        }

        private void EnterFlicker()
        {
            _phase = Phase.Flicker;
            if (_blackout != null)
            {
                _blackout.Flicker(flickerCycles, flickerHalfPeriod);
            }
        }

        private void EnterBlackInAgain()
        {
            _phase = Phase.BlackInAgain;
            _blackTarget = ReadableBlackoutAlpha;
            if (_blackout != null)
            {
                _blackout.FadeTo(ReadableBlackoutAlpha);
            }
            else
            {
                EnterMonologue();
            }
        }

        private void EnterMonologue()
        {
            _phase = Phase.Monologue;
            _dialogueDone = false;
            if (_sfx != null)
            {
                _sfx.PlaySiren();
            }

            if (_dialogue != null)
            {
                _dialogue.Play(OutroDialogue.Monologue());
            }
            else
            {
                _dialogueDone = true;
            }
        }

        private void EnterBlackOut()
        {
            _phase = Phase.BlackOut;
            if (_blackout != null)
            {
                _blackout.FadeTo(0f);
            }
            else
            {
                FinishToMenu();
            }
        }

        /// <summary>Demo stops here: back to the main menu.</summary>
        private void FinishToMenu()
        {
            _phase = Phase.Done;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDialogueFinished()
        {
            _dialogueDone = true;
        }

        /// <summary>
        /// Hides every gameplay visual so only the black panel and AIDA text
        /// remain. Dialogue labels (Controls, AdvancePrompt) and this driver
        /// stay on. Objects that were already off are recorded and left off
        /// when the scene is restored.
        /// </summary>
        private void SetSceneVisible(bool visible)
        {
            if (!visible)
            {
                _keepOff.Clear();
            }

            GameObject root = GameObject.Find("AlgoRhythm");
            if (root != null)
            {
                foreach (Transform child in root.transform)
                {
                    ApplySceneVisibility(child.gameObject, visible);
                }
            }

            GameObject cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                foreach (Transform child in cam.transform)
                {
                    if (child.name == "Blackout")
                    {
                        continue;
                    }

                    ApplySceneVisibility(child.gameObject, visible);
                }
            }
        }

        private void ApplySceneVisibility(GameObject target, bool visible)
        {
            if (target == null)
            {
                return;
            }

            if (target.name == "Tutorial")
            {
                return;
            }

            if (target.name == "HUD")
            {
                foreach (Transform hudChild in target.transform)
                {
                    if (hudChild.name == "Controls" || hudChild.name == "AdvancePrompt")
                    {
                        continue;
                    }

                    ApplySceneVisibility(hudChild.gameObject, visible);
                }

                return;
            }

            if (!visible && !target.activeSelf)
            {
                _keepOff.Add(target);
                return;
            }

            if (visible && _keepOff.Contains(target))
            {
                return;
            }

            target.SetActive(visible);
        }

        private bool IsBlackoutBusy()
        {
            return _blackout != null && _blackout.IsBusy;
        }

        private float BlackoutAlpha()
        {
            return _blackout != null ? _blackout.Alpha : 0f;
        }
    }
}
