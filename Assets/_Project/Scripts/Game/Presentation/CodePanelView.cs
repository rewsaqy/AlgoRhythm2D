using System;
using System.Collections.Generic;
using AlgoRhythm.Core.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Code-panel adapter. All visuals are manually authored TextMesh labels;
    /// this component only binds the CodePanel action map, edits a draft,
    /// previews validation, commits to the runtime, and shows its log.
    /// Keys: 1/2/3 select slot, Q/E cycle token, Enter commit, R reset.
    /// </summary>
    public sealed class CodePanelView : MonoBehaviour
    {
        private const int MaxLogLines = 5;

        private RuleValidator _validator;
        private RuleRuntime _runtime;
        private Func<RuleValidationContext> _contextProvider;

        private readonly List<Condition> _condOptions = new List<Condition>(2);
        private readonly List<Target> _tgtOptions = new List<Target>(5);
        private readonly List<RuleAction> _actOptions = new List<RuleAction>(1);

        private Condition? _cond;
        private Target? _tgt;
        private RuleAction? _act;
        private int _selectedSlot;
        private int _errorSlot = -1;
        private RuleError _lastError = RuleError.None;
        private string _lastPreviewKey = string.Empty;

        private TextMesh _slotCondition;
        private TextMesh _slotTarget;
        private TextMesh _slotAction;
        private TextMesh _preview;
        private TextMesh _error;
        private TextMesh _log;
        private TextMesh _hint;

        private InputActionMap _codePanelMap;
        private InputAction _selectSlot1;
        private InputAction _selectSlot2;
        private InputAction _selectSlot3;
        private InputAction _cyclePrev;
        private InputAction _cycleNext;
        private InputAction _commitRule;
        private InputAction _resetSafe;
        private bool _bound;
        private int _lastEnergy = int.MinValue;
        private int _lastCooldown = int.MinValue;
        private float _revalidateAt;
        private readonly List<string> _logLines = new List<string>(MaxLogLines + 1);

        public void Initialize(
            RuleValidator validator,
            RuleRuntime runtime,
            Func<RuleValidationContext> contextProvider)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _contextProvider =
                contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));

            _slotCondition = FindText("SlotCondition");
            _slotTarget = FindText("SlotTarget");
            _slotAction = FindText("SlotAction");
            _preview = FindText("Preview");
            _error = FindText("Error");
            _log = FindText("Log");
            _hint = FindText("Hint");

            if (_hint != null)
            {
                _hint.text = "1/2/3 slot · Q/E cycle · ENTER commit · R reset";
                _hint.color = UiTypography.Faint;
            }

            if (_slotCondition != null)
            {
                _slotCondition.color = UiTypography.Ink;
            }

            if (_slotTarget != null)
            {
                _slotTarget.color = UiTypography.Ink;
            }

            if (_slotAction != null)
            {
                _slotAction.color = UiTypography.Ink;
            }

            if (_preview != null)
            {
                _preview.color = UiTypography.Info;
            }

            if (_error != null)
            {
                _error.color = UiTypography.Warn;
            }

            if (_log != null)
            {
                _log.color = UiTypography.Dim;
            }

            _runtime.RuleFired += OnRuleFired;
            _runtime.RuleLog += OnRuleLog;

            TryBind();
            RefreshLabels();
        }

        /// <summary>
        /// Applies a wave's token unlocks. Single-option slots prefill and
        /// lock; the target slot resets to empty (the one slot that changes).
        /// </summary>
        public void SetUnlocks(string[] unlocks)
        {
            _condOptions.Clear();
            _tgtOptions.Clear();
            _actOptions.Clear();

            if (unlocks != null)
            {
                foreach (string token in unlocks)
                {
                    if (TryParseCondition(token, out Condition cond) && !_condOptions.Contains(cond))
                    {
                        _condOptions.Add(cond);
                    }
                    else if (TryParseTarget(token, out Target tgt) && !_tgtOptions.Contains(tgt))
                    {
                        _tgtOptions.Add(tgt);
                    }
                    else if (TryParseAction(token, out RuleAction act) && !_actOptions.Contains(act))
                    {
                        _actOptions.Add(act);
                    }
                }
            }

            if (_condOptions.Count == 0)
            {
                _condOptions.Add(Condition.WhenProjectile);
            }

            if (_tgtOptions.Count == 0)
            {
                _tgtOptions.Add(Target.North);
            }

            if (_actOptions.Count == 0)
            {
                _actOptions.Add(RuleAction.RaiseShield);
            }

            _cond = _condOptions.Count == 1 ? _condOptions[0] : (Condition?)null;
            _tgt = null;
            _act = _actOptions.Count == 1 ? _actOptions[0] : (RuleAction?)null;
            _selectedSlot = 1;
            _errorSlot = -1;
            Revalidate(force: true);
            RefreshLabels();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
            if (_runtime != null)
            {
                _runtime.RuleFired -= OnRuleFired;
                _runtime.RuleLog -= OnRuleLog;
            }
        }

        private void Update()
        {
            // No camera anchoring: the panel sits at its scene-authored
            // position, so Scene view and runtime match exactly.
            if (_validator == null || Time.unscaledTime < _revalidateAt)
            {
                return;
            }

            _revalidateAt = Time.unscaledTime + 0.25f;
            RuleValidationContext context = _contextProvider();
            if (context.Energy != _lastEnergy || context.CooldownRemainingMs != _lastCooldown)
            {
                Revalidate(force: true);
                RefreshLabels();
            }
        }

        private void TryBind()
        {
            if (_bound || _validator == null)
            {
                return;
            }

            InputActionAsset asset = InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError("AlgoRhythm: project-wide Input System actions are not available.");
                return;
            }

            _codePanelMap = asset.FindActionMap("CodePanel", throwIfNotFound: false);
            if (_codePanelMap == null)
            {
                Debug.LogError("AlgoRhythm: input action map 'CodePanel' is missing.");
                return;
            }

            _selectSlot1 = _codePanelMap.FindAction("SelectSlot1", throwIfNotFound: true);
            _selectSlot2 = _codePanelMap.FindAction("SelectSlot2", throwIfNotFound: true);
            _selectSlot3 = _codePanelMap.FindAction("SelectSlot3", throwIfNotFound: true);
            _cyclePrev = _codePanelMap.FindAction("CyclePrev", throwIfNotFound: true);
            _cycleNext = _codePanelMap.FindAction("CycleNext", throwIfNotFound: true);
            _commitRule = _codePanelMap.FindAction("CommitRule", throwIfNotFound: true);
            _resetSafe = _codePanelMap.FindAction("ResetSafe", throwIfNotFound: true);

            _selectSlot1.performed += OnSelectSlot1;
            _selectSlot2.performed += OnSelectSlot2;
            _selectSlot3.performed += OnSelectSlot3;
            _cyclePrev.performed += OnCyclePrev;
            _cycleNext.performed += OnCycleNext;
            _commitRule.performed += OnCommitRule;
            _resetSafe.performed += OnResetSafe;
            _codePanelMap.Enable();
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            _selectSlot1.performed -= OnSelectSlot1;
            _selectSlot2.performed -= OnSelectSlot2;
            _selectSlot3.performed -= OnSelectSlot3;
            _cyclePrev.performed -= OnCyclePrev;
            _cycleNext.performed -= OnCycleNext;
            _commitRule.performed -= OnCommitRule;
            _resetSafe.performed -= OnResetSafe;
            _codePanelMap.Disable();
            _bound = false;
        }

        private void OnSelectSlot1(InputAction.CallbackContext context) => SelectSlot(0);

        private void OnSelectSlot2(InputAction.CallbackContext context) => SelectSlot(1);

        private void OnSelectSlot3(InputAction.CallbackContext context) => SelectSlot(2);

        private void OnCyclePrev(InputAction.CallbackContext context) => Cycle(-1);

        private void OnCycleNext(InputAction.CallbackContext context) => Cycle(1);

        private void OnCommitRule(InputAction.CallbackContext context) => Commit();

        private void OnResetSafe(InputAction.CallbackContext context) => ResetSafe();

        private void SelectSlot(int slot)
        {
            _selectedSlot = Mathf.Clamp(slot, 0, 2);
            RefreshLabels();
        }

        private void Cycle(int direction)
        {
            switch (_selectedSlot)
            {
                case 0:
                    _cond = CycleOption(_condOptions, _cond, direction);
                    break;
                case 1:
                    _tgt = CycleOption(_tgtOptions, _tgt, direction);
                    break;
                default:
                    _act = CycleOption(_actOptions, _act, direction);
                    break;
            }

            Revalidate(force: true);
            RefreshLabels();
        }

        private static T? CycleOption<T>(List<T> options, T? current, int direction) where T : struct
        {
            if (options.Count <= 1)
            {
                return options.Count == 1 ? options[0] : current;
            }

            int index = 0;
            if (current.HasValue)
            {
                index = (options.IndexOf(current.Value) + direction + options.Count) % options.Count;
            }
            else if (direction < 0)
            {
                index = options.Count - 1;
            }

            return options[index];
        }

        private void Commit()
        {
            Revalidate(force: true);
            if (_lastError != RuleError.None)
            {
                RefreshLabels();
                return;
            }

            _runtime.Commit(new RuleDefinition(
                _cond.Value, _tgt.Value, _act.Value,
                _validator.EnergyCost, _validator.CooldownMs, priority: 0));
            AppendLog($"COMMIT {_lastPreviewKey}");
            RefreshLabels();
        }

        private void ResetSafe()
        {
            _cond = _condOptions.Count == 1 ? _condOptions[0] : (Condition?)null;
            _tgt = null;
            _act = _actOptions.Count == 1 ? _actOptions[0] : (RuleAction?)null;
            AppendLog("RESET");
            Revalidate(force: true);
            RefreshLabels();
        }

        private void OnRuleFired(RuleFired fired)
        {
            AppendLog(
                $"FIRE #{fired.RuleId} threat {fired.ThreatId} -{fired.EnergySpent}E" +
                (fired.WasDecoy ? " DECOY" : string.Empty));
        }

        private void OnRuleLog(string key)
        {
            AppendLog(key.ToUpperInvariant());
        }

        private void AppendLog(string line)
        {
            _logLines.Add(line);
            while (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveAt(0);
            }

            if (_log != null)
            {
                _log.text = string.Join("\n", _logLines.ToArray());
            }
        }

        private void Revalidate(bool force)
        {
            RuleValidationContext context = _contextProvider();
            _lastEnergy = context.Energy;
            _lastCooldown = context.CooldownRemainingMs;

            var draft = new RuleDraft(_cond, _tgt, _act);
            RuleValidationResult result = _validator.Validate(draft, context);
            _lastError = result.Error;
            _lastPreviewKey = result.PreviewKey;
            _errorSlot = ErrorSlot(result.Error, draft);
        }

        private static int ErrorSlot(RuleError error, RuleDraft draft)
        {
            switch (error)
            {
                case RuleError.E001_EmptySlot:
                    if (!draft.Cond.HasValue)
                    {
                        return 0;
                    }

                    return !draft.Tgt.HasValue ? 1 : 2;
                case RuleError.E002_WrongCategory:
                case RuleError.E003_Unsupported:
                    return 1;
                default:
                    return -1;
            }
        }

        private void RefreshLabels()
        {
            SetSlot(_slotCondition, 0, "WHEN", _cond.HasValue ? ConditionName(_cond.Value) : "___");
            SetSlot(_slotTarget, 1, "TARGET", _tgt?.ToString().ToUpperInvariant() ?? "___");
            SetSlot(_slotAction, 2, "DO", _act.HasValue ? "RAISE_SHIELD" : "___");

            if (_preview != null)
            {
                // Coverage now reads on the arena rule guards, so the
                // code-panel preview carries only the draft key — no
                // ACTIVE/COMMITTED/NO-RULE state line.
                _preview.text = _lastError == RuleError.None && _lastPreviewKey.Length > 0
                    ? _lastPreviewKey
                    : "NO PREVIEW";
                _preview.color = _lastError == RuleError.None && _lastPreviewKey.Length > 0
                    ? UiTypography.Info
                    : UiTypography.Dim;
            }

            if (_error != null)
            {
                _error.text = ErrorText(_lastError);
            }
        }

        private void SetSlot(TextMesh label, int slot, string name, string value)
        {
            if (label == null)
            {
                return;
            }

            string marker = slot == _selectedSlot ? ">" : " ";
            string flag = slot == _errorSlot ? "  !!" : string.Empty;
            label.text = $"{marker}[{slot + 1}] {name}: {value}{flag}";
        }

        private static string ErrorText(RuleError error)
        {
            switch (error)
            {
                case RuleError.None:
                    return string.Empty;
                case RuleError.E001_EmptySlot:
                    return "!! E001: slot kosong — pilih token dengan Q/E";
                case RuleError.E002_WrongCategory:
                    return "!! E002: NEAREST butuh WHEN_PROJECTILE";
                case RuleError.E003_Unsupported:
                    return "!! E003: kombinasi tidak didukung";
                case RuleError.E004_NotEnoughEnergy:
                    return "!! E004: Energy kurang";
                case RuleError.E005_Cooldown:
                    return "!! E005: cooldown berjalan";
                default:
                    return error.ToString().ToUpperInvariant();
            }
        }

        private static string ConditionName(Condition cond)
        {
            return cond == Condition.WhenBeat ? "WHEN_BEAT" : "WHEN_PROJECTILE";
        }

        private static bool TryParseCondition(string token, out Condition cond)
        {
            cond = Condition.WhenProjectile;
            if (string.Equals(token, "WHEN_BEAT", StringComparison.OrdinalIgnoreCase))
            {
                cond = Condition.WhenBeat;
                return true;
            }

            return string.Equals(token, "WHEN_PROJECTILE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseTarget(string token, out Target tgt)
        {
            tgt = Target.North;
            if (string.Equals(token, "NORTH", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(token, "EAST", StringComparison.OrdinalIgnoreCase))
            {
                tgt = Target.East;
                return true;
            }

            if (string.Equals(token, "SOUTH", StringComparison.OrdinalIgnoreCase))
            {
                tgt = Target.South;
                return true;
            }

            if (string.Equals(token, "WEST", StringComparison.OrdinalIgnoreCase))
            {
                tgt = Target.West;
                return true;
            }

            if (string.Equals(token, "NEAREST", StringComparison.OrdinalIgnoreCase))
            {
                tgt = Target.Nearest;
                return true;
            }

            return false;
        }

        private static bool TryParseAction(string token, out RuleAction act)
        {
            act = RuleAction.RaiseShield;
            return string.Equals(token, "RAISE_SHIELD", StringComparison.OrdinalIgnoreCase);
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }
    }
}
