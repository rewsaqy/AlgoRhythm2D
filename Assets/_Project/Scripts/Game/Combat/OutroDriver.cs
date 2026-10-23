using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Run-clear outro: dramatic power failure, emergency glitch flickers,
    /// A.I.D.A. v2.0 revelation monologue addressing the real OS account,
    /// final system override stinger, and interactive cinematic card.
    /// Pure presentation over the idle run-clear state; judging ended.
    /// </summary>
    public sealed class OutroDriver : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Delay,
            PowerCutImpact,
            BlackIn,
            Act1_PowerFailure,
            GlitchStrobe,
            Act2_Alarm,
            TakeoverImpact,
            Act3_Betrayal,
            OverrideClimax,
            FinaleCard,
            Done
        }

        /// <summary>Dialogue stays readable: blackout stops at near-black.</summary>
        private const float ReadableBlackoutAlpha = 0.88f;

        [Header("Timing [U]")]
        [Min(0f)] [SerializeField] private float startDelaySeconds = 1.0f;
        [Min(1)] [SerializeField] private int flickerCycles = 5;
        [Min(0.05f)] [SerializeField] private float flickerHalfPeriod = 0.12f;

        private WaveDirector _director;
        private DialogueView _dialogue;
        private BlackoutView _blackout;
        private BeatClock _clock;
        private CameraShake _shake;
        private SfxPlayer _sfx;
        private DamageFlashView _damageFlash;

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
            CameraShake cameraShake = null,
            SfxPlayer sfx = null,
            DamageFlashView damageFlash = null)
        {
            _director = director;
            _dialogue = dialogue;
            _blackout = blackout;
            _clock = clock;
            _shake = cameraShake;
            _sfx = sfx;
            _damageFlash = damageFlash;

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
                _dialogue.RestoreDefaultStyle();
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
                        EnterPowerCutImpact();
                    }
                    break;

                case Phase.PowerCutImpact:
                    EnterBlackIn();
                    break;

                case Phase.BlackIn:
                    if (!IsBlackoutBusy() && BlackoutAlpha() >= _blackTarget - 0.02f)
                    {
                        EnterAct1_PowerFailure();
                    }
                    break;

                case Phase.Act1_PowerFailure:
                    if (_dialogueDone)
                    {
                        EnterGlitchStrobe();
                    }
                    break;

                case Phase.GlitchStrobe:
                    if (!IsBlackoutBusy())
                    {
                        EnterAct2_Alarm();
                    }
                    break;

                case Phase.Act2_Alarm:
                    if (_dialogueDone)
                    {
                        EnterTakeoverImpact();
                    }
                    break;

                case Phase.TakeoverImpact:
                    if (Time.unscaledTime - _phaseAt >= 0.8f)
                    {
                        EnterAct3_Betrayal();
                    }
                    break;

                case Phase.Act3_Betrayal:
                    if (_dialogueDone)
                    {
                        EnterOverrideClimax();
                    }
                    break;

                case Phase.OverrideClimax:
                    if (Time.unscaledTime - _phaseAt >= 0.6f)
                    {
                        EnterFinaleCard();
                    }
                    break;

                case Phase.FinaleCard:
                    UpdateFinaleInput();
                    break;
            }
        }

        private void EnterPowerCutImpact()
        {
            _phase = Phase.PowerCutImpact;
            _dialogue?.ConfigureOutroStyle();
            _sfx?.PlayGlitchZap(0.5f, 0.8f);
            _sfx?.PlayStinger(0.6f, 0.85f);
            _shake?.AddTrauma(1.0f);
            _damageFlash?.Flash(0.9f);
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
                EnterAct1_PowerFailure();
            }
        }

        private void EnterAct1_PowerFailure()
        {
            _phase = Phase.Act1_PowerFailure;
            _dialogueDone = false;
            if (_dialogue != null)
            {
                _dialogue.Play(OutroDialogue.PowerFailureLines());
            }
            else
            {
                _dialogueDone = true;
            }
        }

        private void EnterGlitchStrobe()
        {
            _phase = Phase.GlitchStrobe;
            _sfx?.PlayGlitchZap(1.3f, 0.7f);
            _damageFlash?.Flash(0.75f);
            _shake?.AddTrauma(0.7f);

            if (_blackout != null)
            {
                _blackout.Flicker(flickerCycles, flickerHalfPeriod);
            }
        }

        private void EnterAct2_Alarm()
        {
            _phase = Phase.Act2_Alarm;
            _dialogueDone = false;
            _blackTarget = ReadableBlackoutAlpha;
            _blackout?.FadeTo(ReadableBlackoutAlpha);

            if (_dialogue != null)
            {
                _dialogue.Play(OutroDialogue.AlarmLines());
            }
            else
            {
                _dialogueDone = true;
            }
        }

        private void EnterTakeoverImpact()
        {
            _phase = Phase.TakeoverImpact;
            _phaseAt = Time.unscaledTime;
            _sfx?.PlayStinger(0.45f, 0.9f);
            _shake?.AddTrauma(0.85f);
            _damageFlash?.Flash(0.5f);
            _blackout?.FadeTo(0.92f);
        }

        private void EnterAct3_Betrayal()
        {
            _phase = Phase.Act3_Betrayal;
            _dialogueDone = false;
            if (_dialogue != null)
            {
                string userName = null;
                try
                {
                    userName = System.Environment.UserName;
                }
                catch (System.Exception)
                {
                    userName = null;
                }

                _dialogue.Play(OutroDialogue.Betrayal(userName));
            }
            else
            {
                _dialogueDone = true;
            }
        }

        private void EnterOverrideClimax()
        {
            _phase = Phase.OverrideClimax;
            _phaseAt = Time.unscaledTime;
            _sfx?.PlayGlitchZap(0.4f, 0.85f);
            _sfx?.PlayStinger(0.35f, 1.0f);
            _shake?.AddTrauma(1.0f);
            _damageFlash?.Flash(1.0f);
            _blackout?.FadeTo(0.95f);
        }

        private void EnterFinaleCard()
        {
            _phase = Phase.FinaleCard;
            if (_dialogue != null)
            {
                _dialogue.ShowRaw(OutroDialogue.FinaleCardText());
            }
        }

        private void UpdateFinaleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[Key.Enter].wasPressedThisFrame
                || keyboard[Key.NumpadEnter].wasPressedThisFrame
                || keyboard[Key.Escape].wasPressedThisFrame)
            {
                SceneManager.LoadScene("MainMenu");
            }
            else if (keyboard[Key.R].wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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
