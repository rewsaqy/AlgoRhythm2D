using System.Collections.Generic;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Greybox arena presentation. Every visual is authored in the scene under
    /// this object; the presenter only moves, labels, activates and deactivates
    /// existing views. It never creates or destroys GameObjects at runtime.
    /// Direction is always communicated with text and a glyph, never colour
    /// alone. Presentation contains no timing or damage authority.
    /// </summary>
    public sealed class ArenaPresenter : MonoBehaviour
    {
        private const float DirectionRadius = 1.4f;

        private readonly List<ThreatView> _views = new List<ThreatView>();
        private readonly Dictionary<int, ThreatView> _activeThreats = new Dictionary<int, ThreatView>();
        private readonly Dictionary<Direction, ShieldView> _blocks = new Dictionary<Direction, ShieldView>();
        private readonly Dictionary<Direction, GameObject> _ruleGuards = new Dictionary<Direction, GameObject>();
        private ShieldView _shield;
        private int _ruleActiveMask;
        private int _rulePendingMask;
        private TextMesh _gradeLabel;
        private float _gradeHideAt;
        private float _gradePopUntil;
        private double _perfectMs = 70.0;
        private double _goodMs = 130.0;

        public void Initialize(double perfectMs, double goodMs)
        {
            _perfectMs = perfectMs;
            _goodMs = goodMs;
            _gradeLabel = FindText("Grade");

            Transform threatsRoot = transform.Find("Threats");
            if (threatsRoot != null)
            {
                foreach (ThreatView view in threatsRoot.GetComponentsInChildren<ThreatView>(true))
                {
                    view.Initialize();
                    _views.Add(view);
                }
            }

            Transform shieldObject = transform.Find("Shields/Shield");
            if (shieldObject != null)
            {
                _shield = shieldObject.GetComponent<ShieldView>();
                if (_shield != null)
                {
                    _shield.Initialize();
                }
            }

            Transform shieldsRoot = transform.Find("Shields");
            if (shieldsRoot != null)
            {
                foreach (ShieldView block in shieldsRoot.GetComponentsInChildren<ShieldView>(true))
                {
                    if (block == null || block == _shield)
                    {
                        continue;
                    }

                    if (!TryBlockDirection(block.name, out Direction slot))
                    {
                        continue;
                    }

                    block.Initialize();
                    // Blocks rest hidden; FlashShieldBlock shows them.
                    // Initialize activates, so park them back off.
                    block.gameObject.SetActive(false);
                    _blocks[slot] = block;
                }
            }

            Transform guardsRoot = transform.Find("RuleGuards");
            if (guardsRoot != null)
            {
                foreach (Transform guard in guardsRoot)
                {
                    if (guard == null || !TryBlockDirection(guard.name, out Direction slot))
                    {
                        continue;
                    }

                    guard.gameObject.SetActive(false);
                    _ruleGuards[slot] = guard.gameObject;
                }
            }

            if (_gradeLabel != null)
            {
                UiTypography.Style(_gradeLabel, 52, 0.085f, UiTypography.Ink,
                    TextAnchor.MiddleCenter, TextAlignment.Center, 1.2f);
                _gradeLabel.text = string.Empty;
            }
        }

        public void SpawnThreat(ThreatEvent threat, float travelMs)
        {
            if (_activeThreats.ContainsKey(threat.Id))
            {
                return;
            }

            ThreatView view = FindAvailableView();
            if (view == null)
            {
                return;
            }

            view.Show(threat, travelMs);
            view.transform.localPosition = (Vector3)(DirectionVector(threat.Direction) * SpawnDistance(threat.Direction));
            view.transform.localScale = Vector3.one;
            _activeThreats[threat.Id] = view;
        }

        public void UpdateThreat(
            ThreatEvent threat,
            float progress,
            bool buffered,
            double msToImpact,
            double beatPhase)
        {
            if (!_activeThreats.TryGetValue(threat.Id, out ThreatView view))
            {
                return;
            }

            float clamped = Mathf.Clamp01(progress);
            Vector3 target = (Vector3)(DirectionVector(threat.Direction) * DirectionRadius);
            Vector3 spawn = (Vector3)(DirectionVector(threat.Direction) * SpawnDistance(threat.Direction));
            view.transform.localPosition = Vector3.Lerp(spawn, target, clamped);

            // Beat-locked hentakan: the body pops on every DSP beat and the
            // pop grows as the threat nears the ring, so late beats feel
            // heavier. Position and ring stay the timing authority.
            float tension = 1f - clamped;
            float amp = 0.06f + 0.24f * tension * tension;
            float pop = Mathf.Exp(-6f * (float)beatPhase);
            float body = 1f + amp * pop;
            view.transform.localScale = new Vector3(body, body, 1f);
            view.SetBuffered(buffered);
            view.SetRing(msToImpact, _perfectMs, _goodMs);
        }

        public void ResolveThreat(int threatId, TimingGrade grade, bool correctDirection)
        {
            if (!_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            _activeThreats.Remove(threatId);
            view.transform.localScale = Vector3.one;
            view.Resolve(grade);
            ShowGrade(grade, correctDirection ? "shield" : "wrong direction");
        }

        public void ResolveDecoyView(int threatId, bool shielded)
        {
            if (!_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            _activeThreats.Remove(threatId);
            view.transform.localScale = Vector3.one;
            view.ResolveDecoy(shielded);
            // Notes carry no text, so the decoy outcome reads here instead.
            ShowDecoyFeedback(shielded);
        }

        /// <summary>Hides every pooled view. Used on wave change and retry.</summary>
        public void ClearThreats()
        {
            _activeThreats.Clear();
            for (int index = 0; index < _views.Count; index++)
            {
                _views[index].Hide();
            }
        }

        /// <summary>
        /// Parks the single orbiting shield on the ring. Angle uses compass
        /// degrees (North 0°, East 90°); the bar lies tangent to the ring.
        /// </summary>
        public void SetShieldPose(float angleDeg, string slotCode)
        {
            if (_shield == null)
            {
                return;
            }

            float radians = angleDeg * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f)
                               * DirectionRadius;
            _shield.Park(position, angleDeg, slotCode);
        }

        public void PulseShield()
        {
            if (_shield != null)
            {
                _shield.Pulse();
            }
        }

        /// <summary>
        /// Shows a real shield GO in the pressed lane: instant block flash
        /// that reads in the arena, not as HUD text. Perfect lands biggest,
        /// Good/auto medium, plain presses small.
        /// </summary>
        public void FlashShieldBlock(Direction direction, float punch, float holdSeconds = 0.3f)
        {
            if (_blocks.TryGetValue(direction, out ShieldView block) && block != null)
            {
                block.FlashBlock(punch, holdSeconds);
            }
        }

        /// <summary>
        /// Persistent rule-coverage readout: a ring guard stays lit in every
        /// lane an active rule covers, so the player sees where the rules
        /// are without reading the code panel. Bit per lane: N=1, E=2, S=4,
        /// W=8. Pending (committed, live next beat) lanes blink instead.
        /// </summary>
        public void SetRuleGuards(int activeMask, int pendingMask)
        {
            _ruleActiveMask = activeMask;
            _rulePendingMask = pendingMask;
            ApplyRuleGuards(Time.unscaledTime);
        }

        public void ShowInputRejected(string reason)
        {
            if (_gradeLabel == null)
            {
                return;
            }

            // Two lines only when there is something to explain; the reason
            // is always text, never colour alone.
            _gradeLabel.text = $"NO SHIELD\n{reason}";
            _gradeLabel.color = UiTypography.Warn;
            PunchGrade();
        }

        public void ShowDecoyFeedback(bool shielded)
        {
            if (_gradeLabel == null)
            {
                return;
            }

            _gradeLabel.text = shielded ? "DECOY -5" : "IGNORED";
            _gradeLabel.color = shielded ? UiTypography.Warn : UiTypography.Dim;
            PunchGrade();
        }

        public void ShowGrade(TimingGrade grade, string detail)
        {
            if (_gradeLabel == null)
            {
                return;
            }

            // Clean hits read as one word; misses and wrong-direction hits
            // keep their reason on a second line.
            _gradeLabel.text = grade == TimingGrade.Perfect || grade == TimingGrade.Good
                ? grade.ToString().ToUpperInvariant()
                : string.IsNullOrEmpty(detail)
                    ? grade.ToString().ToUpperInvariant()
                    : $"{grade.ToString().ToUpperInvariant()}\n{detail.ToUpperInvariant()}";
            _gradeLabel.color = GradeColor(grade);
            PunchGrade();
        }

        private void PunchGrade()
        {
            _gradeHideAt = Time.unscaledTime + 0.7f;
            _gradePopUntil = Time.unscaledTime + 0.25f;
            _gradeLabel.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
        }

        private void Update()
        {
            if (_rulePendingMask != 0)
            {
                ApplyRuleGuards(Time.unscaledTime);
            }

            if (_gradeLabel != null && _gradePopUntil > 0f)
            {
                if (Time.unscaledTime >= _gradePopUntil)
                {
                    _gradePopUntil = 0f;
                    _gradeLabel.transform.localScale = Vector3.one;
                }
                else
                {
                    float t = 1f - (_gradePopUntil - Time.unscaledTime) / 0.25f;
                    float scale = Mathf.Lerp(1.25f, 1.0f, t * t);
                    _gradeLabel.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            if (_gradeLabel != null && _gradeHideAt > 0f && Time.unscaledTime >= _gradeHideAt)
            {
                _gradeLabel.text = string.Empty;
                _gradeHideAt = 0f;
            }
        }

        private ThreatView FindAvailableView()
        {
            for (int index = 0; index < _views.Count; index++)
            {
                if (_views[index].IsAvailable)
                {
                    return _views[index];
                }
            }

            return null;
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }

        /// <summary>
        /// Spawn distance per lane: east/west use the wide screen edges,
        /// north/south the near vertical edges. Same travel speed both axes.
        /// </summary>
        internal static float SpawnDistance(Direction direction)
        {
            return direction == Direction.East || direction == Direction.West ? 8.2f : 4.6f;
        }

        /// <summary>
        /// Local threat position at a travel progress, mirroring
        /// UpdateThreat's linear spawn-to-ring blend. Used to aim A.I.D.A.
        /// shots at the note's real spot instead of a fixed ring point.
        /// </summary>
        internal static Vector3 ThreatLocalPosition(Direction direction, float progress)
        {
            Vector3 target = (Vector3)(DirectionVector(direction) * DirectionRadius);
            Vector3 spawn = (Vector3)(DirectionVector(direction) * SpawnDistance(direction));
            return Vector3.Lerp(spawn, target, Mathf.Clamp01(progress));
        }

        internal static Vector2 DirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector2.up;
                case Direction.East:
                    return Vector2.right;
                case Direction.South:
                    return Vector2.down;
                case Direction.West:
                    return Vector2.left;
                default:
                    return Vector2.zero;
            }
        }

        internal static string DirectionCode(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return "N";
                case Direction.East:
                    return "E";
                case Direction.South:
                    return "S";
                case Direction.West:
                    return "W";
                default:
                    return "?";
            }
        }

        internal static string DirectionGlyph(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return "^";
                case Direction.East:
                    return ">";
                case Direction.South:
                    return "v";
                case Direction.West:
                    return "<";
                default:
                    return "?";
            }
        }

        internal static Color GradeColor(TimingGrade grade)
        {
            switch (grade)
            {
                case TimingGrade.Perfect:
                    return UiTypography.Perfect;
                case TimingGrade.Good:
                    return UiTypography.Good;
                default:
                    return UiTypography.Miss;
            }
        }

        /// <summary>One bit per lane: N=1, E=2, S=4, W=8.</summary>
        private static int GuardBit(Direction direction)
        {
            // Direction order is North=0..West=3; one bit per lane.
            return 1 << (int)direction;
        }

        private void ApplyRuleGuards(float now)
        {
            bool blinkOn = (now * 2f) % 1f < 0.5f;
            foreach (KeyValuePair<Direction, GameObject> pair in _ruleGuards)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                int bit = GuardBit(pair.Key);
                bool active = (_ruleActiveMask & bit) != 0;
                bool pending = (_rulePendingMask & bit) != 0 && !active;
                pair.Value.SetActive(active || (pending && blinkOn));
            }
        }

        /// <summary>
        /// Maps a block GO name (ShieldBlock_N/E/S/W suffix) to its lane.
        /// Unknown names are ignored so stray ShieldViews never break init.
        /// </summary>
        private static bool TryBlockDirection(string objectName, out Direction direction)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                if (objectName.EndsWith("_N"))
                {
                    direction = Direction.North;
                    return true;
                }

                if (objectName.EndsWith("_E"))
                {
                    direction = Direction.East;
                    return true;
                }

                if (objectName.EndsWith("_S"))
                {
                    direction = Direction.South;
                    return true;
                }

                if (objectName.EndsWith("_W"))
                {
                    direction = Direction.West;
                    return true;
                }
            }

            direction = Direction.South;
            return false;
        }
    }
}
