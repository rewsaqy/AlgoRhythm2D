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
        private ShieldView _shield;
        private TextMesh _gradeLabel;
        private float _gradeHideAt;
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

            if (_gradeLabel != null)
            {
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
            double msToImpact)
        {
            if (!_activeThreats.TryGetValue(threat.Id, out ThreatView view))
            {
                return;
            }

            float clamped = Mathf.Clamp01(progress);
            Vector3 target = (Vector3)(DirectionVector(threat.Direction) * DirectionRadius);
            Vector3 spawn = (Vector3)(DirectionVector(threat.Direction) * SpawnDistance(threat.Direction));
            view.transform.localPosition = Vector3.Lerp(spawn, target, clamped);

            float pulse = 1.0f + 0.06f * Mathf.Sin(Time.unscaledTime * 16.0f);
            view.transform.localScale = new Vector3(pulse, pulse, 1.0f);
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

        public void ShowInputRejected(string reason)
        {
            if (_gradeLabel == null)
            {
                return;
            }

            _gradeLabel.text = $"NO SHIELD\n{reason}";
            _gradeLabel.color = new Color(1f, 0.85f, 0.25f, 1f);
            _gradeHideAt = Time.unscaledTime + 0.7f;
        }

        public void ShowGrade(TimingGrade grade, string detail)
        {
            if (_gradeLabel == null)
            {
                return;
            }

            _gradeLabel.text = string.IsNullOrEmpty(detail)
                ? grade.ToString().ToUpperInvariant()
                : $"{grade.ToString().ToUpperInvariant()}\n{detail.ToUpperInvariant()}";
            _gradeLabel.color = GradeColor(grade);
            _gradeHideAt = Time.unscaledTime + 0.7f;
        }

        private void Update()
        {
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
                    return new Color(0.4f, 1f, 0.55f, 1f);
                case TimingGrade.Good:
                    return new Color(1f, 0.95f, 0.35f, 1f);
                default:
                    return new Color(1f, 0.35f, 0.35f, 1f);
            }
        }
    }
}
