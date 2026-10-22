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
        [Header("Layout [U] — editable map. Ubah di Inspector/MCP, tanpa sentuh kode.")]
        [Tooltip("Jarak Core ke tiap lane (satuan world).")]
        [Min(0.1f)] [SerializeField] private float directionRadius = 1.4f;
        [Tooltip("Jarak spawn horizontal (East/West) — lebar layar.")]
        [Min(1f)] [SerializeField] private float spawnDistanceEastWest = 8.2f;
        [Tooltip("Jarak spawn vertikal (North/South).")]
        [Min(1f)] [SerializeField] private float spawnDistanceNorthSouth = 4.6f;
        [Header("Radial (Beatblock) [U] — spawn lingkaran, kecepatan sama semua sudut.")]
        [Tooltip("Radius spawn radial untuk note sudut bebas.")]
        [Min(1f)] [SerializeField] private float radialSpawnDistance = 8.2f;

        [Header("Feel [U] — hentakan beat per threat")]
        [Tooltip("Amplitudo pop saat beat; makin dekat ring makin besar.")]
        [Range(0f, 1f)] [SerializeField] private float popBase = 0.06f;
        [Tooltip("Penguat pop saat threat dekat ring.")]
        [Range(0f, 1f)] [SerializeField] private float popNearRing = 0.24f;

        private readonly List<ThreatView> _views = new List<ThreatView>();
        private readonly Dictionary<int, ThreatView> _activeThreats = new Dictionary<int, ThreatView>();
        private readonly Dictionary<Direction, ShieldView> _blocks = new Dictionary<Direction, ShieldView>();
        private readonly Dictionary<Direction, GameObject> _ruleGuards = new Dictionary<Direction, GameObject>();
        private int _ruleActiveMask;
        private int _rulePendingMask;
        private TextMesh _gradeLabel;
        private float _gradeHideAt;
        private float _gradePopUntil;
        private double _perfectMs = 70.0;
        private double _goodMs = 130.0;

        [Header("Beat pulse [U] — Core breathes on the DSP beat")]
        [Tooltip("Skala denyut Core tepat di beat; 0 = mati.")]
        [Range(0f, 0.2f)] [SerializeField] private float beatPulseAmp = 0.04f;

        private Transform _core;
        private Vector3 _coreBaseScale = Vector3.one;

        public void Initialize(double perfectMs, double goodMs)
        {
            _perfectMs = perfectMs;
            _goodMs = goodMs;
            _gradeLabel = FindText("Grade");

            Transform core = transform.Find("Core");
            if (core != null)
            {
                _core = core;
                _coreBaseScale = core.localScale;
            }

            Transform threatsRoot = transform.Find("Threats");
            if (threatsRoot != null)
            {
                foreach (ThreatView view in threatsRoot.GetComponentsInChildren<ThreatView>(true))
                {
                    view.Initialize();
                    _views.Add(view);
                }
            }

            Transform shieldsRoot = transform.Find("Shields");
            if (shieldsRoot != null)
            {
                foreach (ShieldView block in shieldsRoot.GetComponentsInChildren<ShieldView>(true))
                {
                    if (block == null)
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

        public bool SpawnThreat(ThreatEvent threat, float travelMs)
        {
            if (_activeThreats.ContainsKey(threat.Id))
            {
                return true;
            }

            ThreatView view = FindAvailableView();
            if (view == null)
            {
                Debug.LogWarning($"AlgoRhythm: ThreatView pool habis untuk threat {threat.Id}; note ditahan, tidak dinilai.");
                return false;
            }

            view.Show(threat, travelMs);
            view.transform.localPosition = ThreatLocalPosition(threat.AngleDeg, 0f);
            _activeThreats[threat.Id] = view;
            return true;
        }

        /// <summary>Jumlah view pool (untuk validasi wave).</summary>
        public int PooledViewCount => _views.Count;

        /// <summary>
        /// Validasi pool: hitung maksimum note aktif bersamaan dari interval
        /// spawn..impact. Fail keras di Editor bila pool kurang.
        /// </summary>
        public static int MaxConcurrentThreats(System.Collections.Generic.IReadOnlyList<ThreatEvent> threats)
        {
            int max = 0;
            for (int i = 0; i < threats.Count; i++)
            {
                int overlap = 0;
                for (int j = 0; j < threats.Count; j++)
                {
                    if (threats[j].SpawnBeat <= threats[i].ImpactBeat
                        && threats[i].SpawnBeat <= threats[j].ImpactBeat)
                    {
                        overlap++;
                    }
                }
                if (overlap > max) max = overlap;
            }
            return max;
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
            Vector3 target = AngleToLocal(threat.AngleDeg, directionRadius);
            Vector3 spawn = AngleToLocal(threat.AngleDeg, radialSpawnDistance);
            view.transform.localPosition = Vector3.Lerp(spawn, target, clamped);

            // Beat-locked hentakan: the body pops on every DSP beat and the
            // pop grows as the threat nears the ring, so late beats feel
            // heavier. Position and ring stay the timing authority.
            float tension = 1f - clamped;
            float amp = popBase + popNearRing * tension * tension;
            float pop = Mathf.Exp(-6f * (float)beatPhase);
            float body = 1f + amp * pop;
            view.transform.localScale = new Vector3(body, body, 1f);
            view.SetBuffered(buffered);
            view.SetRing(msToImpact, _perfectMs, _goodMs);
        }

        public void ResolveThreat(int threatId, TimingGrade grade, bool correctDirection, bool destroyed)
        {
            if (!_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            _activeThreats.Remove(threatId);
            view.transform.localScale = Vector3.one;
            view.Resolve(grade, CoreLocalPosition(), destroyed);
            ShowGrade(grade, null);
        }

        public void ResolveDecoyView(int threatId, bool shielded, bool destroyed)
        {
            if (!_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            _activeThreats.Remove(threatId);
            view.ResolveDecoy(shielded, CoreLocalPosition(), destroyed);
            // Notes carry no text, so the decoy outcome reads here instead.
            ShowDecoyFeedback(shielded);
        }

        /// <summary>Core position in Arena space; dash target on resolve.</summary>
        private Vector3 CoreLocalPosition()
        {
            return _core != null ? _core.localPosition : Vector3.zero;
        }

        /// <summary>Radius ring Core (world) untuk parkir paddle.</summary>
        public float RingRadius => directionRadius;

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
        /// Shows a hit flash GO at the contacted angle: instant block flash
        /// that reads in the arena, not as HUD text. Perfect lands biggest,
        /// Good/auto medium, plain presses small. Lane GOs are flash-only;
        /// the moving model is the single Paddle.
        /// </summary>
        public void FlashShieldBlock(Direction direction, float punch, float holdSeconds = 0.3f)
        {
            if (_blocks.TryGetValue(direction, out ShieldView block) && block != null)
            {
                block.FlashBlock(punch, holdSeconds);
            }
        }

        /// <summary>Efek hit di sudut kontak bebas (Beatblock); fallback ke lane terdekat.</summary>
        public void FlashAtAngle(float angleDeg, float punch, float holdSeconds = 0.3f)
        {
            Direction nearest = AngleToDirection(angleDeg);
            FlashShieldBlock(nearest, punch, holdSeconds);
        }

        /// <summary>
        /// Armor feedback: cracked Heavy shrinks to normal size with a CRACK
        /// callout. No-op for unknown or resolved ids.
        /// </summary>
        public void SetArmorVisual(int threatId, bool cracked)
        {
            if (!cracked || !_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            view.SetArmorCracked();
        }

        /// <summary>
        /// Hold-note sustain readout, driven every tick while held.
        /// </summary>
        public void SetHoldVisual(int threatId, bool holding, float progress)
        {
            if (!holding || !_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            view.SetHolding(progress);
        }

        /// <summary>
        /// Sleeper telegraph state; shape + text change, never colour alone.
        /// </summary>
        public void SetSleeperVisual(int threatId, bool armed)
        {
            if (!_activeThreats.TryGetValue(threatId, out ThreatView view))
            {
                return;
            }

            view.SetSleeperArmed(armed);
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

        /// <summary>
        /// Global beat pulse: the Core breathes exactly on the DSP beat
        /// (same decay math as the threat approach pop). Called every tick
        /// with the DSP beat phase; frozen while paused. Pure presentation.
        /// </summary>
        public void SetBeatPulse(double beatPhase)
        {
            if (_core == null || beatPulseAmp <= 0f)
            {
                return;
            }

            float pop = Mathf.Exp(-6f * Mathf.Clamp01((float)beatPhase));
            float scale = 1f + beatPulseAmp * pop;
            _core.localScale = _coreBaseScale * scale;
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
        /// Editable in Inspector (spawnDistanceEastWest/NorthSouth).
        /// </summary>
        internal float SpawnDistance(Direction direction)
        {
            return direction == Direction.East || direction == Direction.West
                ? spawnDistanceEastWest
                : spawnDistanceNorthSouth;
        }

        /// <summary>
        /// Local threat position at a travel progress, mirroring
        /// UpdateThreat's linear spawn-to-ring blend. Used to aim A.I.D.A.
        /// shots at the note's real spot instead of a fixed ring point.
        /// </summary>
        internal Vector3 ThreatLocalPosition(Direction direction, float progress)
        {
            return ThreatLocalPosition(AngleMath.DirectionToAngle(direction), progress);
        }

        /// <summary>Versi sudut bebas (Beatblock): radial konstan.</summary>
        internal Vector3 ThreatLocalPosition(float angleDeg, float progress)
        {
            Vector3 target = AngleToLocal(angleDeg, directionRadius);
            Vector3 spawn = AngleToLocal(angleDeg, radialSpawnDistance);
            return Vector3.Lerp(spawn, target, Mathf.Clamp01(progress));
        }

        internal static Vector3 AngleToLocal(float angleDeg, float radius)
        {
            float radians = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f) * radius;
        }

        internal static Direction AngleToDirection(float angleDeg)
        {
            float a = AngleMath.Normalize360(angleDeg);
            if (a < 45f || a >= 315f) return Direction.North;
            if (a < 135f) return Direction.East;
            if (a < 225f) return Direction.South;
            return Direction.West;
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
