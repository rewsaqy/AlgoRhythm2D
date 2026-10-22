using System;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Serialized greybox threat visual. The view is authored in the scene and
    /// pooled by activation; it never creates GameObjects at runtime.
    /// Direction always reads as glyph + code text (never colour alone);
    /// decoys read as a diamond marker plus "?" text.
    /// </summary>
    public sealed class ThreatView : MonoBehaviour
    {
        [Header("Visual [U] — editable musuh. Ubah di Inspector/MCP.")]
        [Tooltip("Skala ring saat spawn (jauh).")]
        [Min(0.1f)] [SerializeField] private float ringFullScale = 1.8f;
        [Tooltip("Skala ring saat tepat di Core (dekat).")]
        [Min(0.05f)] [SerializeField] private float ringMinScale = 0.45f;
        [Tooltip("Skala pop saat grade muncul (Perfect/Good/Miss).")]
        [Min(0.1f)] [SerializeField] private float resolveHideSeconds = 0.45f;

        private TextMesh _label;
        private SpriteRenderer _ring;
        private MeshFilter _markerMesh;
        private static readonly System.Collections.Generic.Dictionary<string, Mesh> MeshCache =
            new System.Collections.Generic.Dictionary<string, Mesh>();
        private ThreatEvent _threat;
        private bool _visible;
        private float _hideAt;
        private float _travelMs = 1f;

        [Header("Kamikaze [U] — nyeruduk ke Core saat lolos")]
        [Tooltip("Durasi dash ke Core saat threat TIDAK dihancurkan (Miss).")]
        [Min(0.05f)] [SerializeField] private float dashSeconds = 0.28f;
        [Tooltip("Skala akhir saat menabrak Core (pecahan dari skala awal).")]
        [Range(0.1f, 1f)] [SerializeField] private float dashEndScale = 0.35f;

        [Header("Shatter [U] — pecah di tempat saat dihancurkan")]
        [Tooltip("Durasi pop membesar saat threat dihancurkan pemain/AIDA.")]
        [Min(0.05f)] [SerializeField] private float shatterSeconds = 0.22f;
        [Tooltip("Skala puncak pop shatter.")]
        [Min(1f)] [SerializeField] private float shatterPeakScale = 1.55f;

        private bool _dashing;
        private float _dashUntil;
        private Vector3 _dashFrom;
        private Vector3 _dashTarget;
        private Vector3 _dashFromScale;
        private bool _shattering;
        private float _shatterUntil;
        private Vector3 _shatterFrom;
        private string _banner = string.Empty;
        private float _bannerUntil;
        private bool _sleeperArmed;

        public bool IsAvailable => !gameObject.activeSelf;

        public void Initialize()
        {
            _label = FindText("Label");
            Transform ring = transform.Find("Ring");
            _ring = ring != null ? ring.GetComponent<SpriteRenderer>() : null;
            Transform markerRoot = transform.Find("Marker");
            _markerMesh = markerRoot != null ? markerRoot.GetComponent<MeshFilter>() : null;
            UiTypography.Style(_label, 40, 0.08f, UiTypography.Ink,
                TextAnchor.MiddleCenter, TextAlignment.Center, 1.1f);
            if (_label != null)
            {
                // Notes stay text-free: lane, marker shape and the approach
                // ring carry direction/kind/timing. Grade and decoy feedback
                // still read as text on the arena grade label.
                _label.gameObject.SetActive(false);
            }

            Hide();
        }

        public void Show(ThreatEvent threat, float travelMs)
        {
            _threat = threat;
            _dashing = false;
            _shattering = false;
            _travelMs = Mathf.Max(1f, travelMs);
            _visible = true;
            _hideAt = 0f;
            gameObject.SetActive(true);

            Transform marker = transform.Find("Marker");
            if (marker != null)
            {
                // Decoys read as a diamond, never by colour alone.
                marker.localRotation = threat.Kind == ThreatKind.AdwareDecoy
                    ? Quaternion.Euler(0f, 0f, 45f)
                    : Quaternion.identity;
            }

            ApplyKindPose();
            SetRing(_travelMs, 70.0, 130.0);
            SetBuffered(false);
        }

        /// <summary>Base pose per archetype; pooled views reset here.</summary>
        private void ApplyKindPose()
        {
            // Distinct silhouettes from built-in meshes (no new assets):
            // shooter = flat quad, marcher = capsule trooper, heavy = big
            // cube, sleeper = sphere pod, holder = long cube bar. Decoy stays
            // a rotated diamond quad; labels always spell the kind too.
            if (_markerMesh != null)
            {
                string meshName = _threat.Kind switch
                {
                    ThreatKind.Marcher => "Capsule.fbx",
                    ThreatKind.Heavy => "Cube.fbx",
                    ThreatKind.Sleeper => "Sphere.fbx",
                    ThreatKind.Holder => "Cube.fbx",
                    _ => "Quad.fbx",
                };
                Mesh mesh;
                if (!MeshCache.TryGetValue(meshName, out mesh) || mesh == null)
                {
                    mesh = Resources.GetBuiltinResource<Mesh>(meshName);
                    MeshCache[meshName] = mesh;
                }

                if (mesh != null)
                {
                    _markerMesh.sharedMesh = mesh;
                }
            }

            switch (_threat.Kind)
            {
                case ThreatKind.Heavy:
                    transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                    break;
                case ThreatKind.Holder:
                    transform.localScale = new Vector3(2.2f, 0.8f, 1f);
                    break;
                case ThreatKind.Sleeper:
                    transform.localScale = Vector3.one * 0.9f;
                    SetSleeperArmed(false);
                    break;
                case ThreatKind.Marcher:
                    transform.localScale = new Vector3(0.7f, 1.3f, 1f);
                    break;
                default:
                    transform.localScale = Vector3.one;
                    break;
            }
        }

        public void SetBuffered(bool buffered)
        {
            if (_label == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_banner) && Time.unscaledTime < _bannerUntil)
            {
                _label.text = _banner;
                _label.color = UiTypography.Info;
                return;
            }

            _banner = string.Empty;

            if (_threat.Kind == ThreatKind.AdwareDecoy)
            {
                _label.text = buffered ? "? DECOY\n+BUFFER" : "? DECOY";
                _label.color = UiTypography.Decoy;
                return;
            }

            if (_threat.Kind == ThreatKind.Sleeper)
            {
                // Shape + text differ per state, never colour alone.
                Transform marker = transform.Find("Marker");
                if (marker != null)
                {
                    marker.localRotation = _sleeperArmed
                        ? Quaternion.Euler(0f, 0f, 45f)
                        : Quaternion.identity;
                }

                _label.text = _sleeperArmed
                    ? (buffered ? "▲ !! DASH\n+BUFFER" : "▲ !! DASH")
                    : (buffered ? "○ Zzz\n+BUFFER" : "○ Zzz");
                _label.color = _sleeperArmed ? UiTypography.Warn : UiTypography.Dim;
                return;
            }

            string code = ArenaPresenter.DirectionCode(_threat.Direction);
            string glyph = ArenaPresenter.DirectionGlyph(_threat.Direction);
            string prefix = _threat.Kind switch
            {
                ThreatKind.Marcher => "≡",
                ThreatKind.Heavy => "◆",
                ThreatKind.Holder => "▬",
                _ => string.Empty,
            };
            string head = string.IsNullOrEmpty(prefix) ? $"{glyph} {code}" : $"{prefix} {glyph} {code}";
            _label.text = buffered ? $"{head}\n+BUFFER" : head;
            _label.color = buffered ? UiTypography.Info : UiTypography.Ink;
        }

        /// <summary>Transient banner (CRACK/HOLD) that wins over the label.</summary>
        public void SetBanner(string text, float seconds)
        {
            _banner = text ?? string.Empty;
            _bannerUntil = Time.unscaledTime + Mathf.Max(0.01f, seconds);
        }

        /// <summary>Armor cracked: shrink to normal size + CRACK callout.</summary>
        public void SetArmorCracked()
        {
            transform.localScale = Vector3.one;
            SetBanner("CRACK", 0.8f);
        }

        /// <summary>Hold-note sustain readout, called every tick while held.</summary>
        public void SetHolding(float progress)
        {
            float p = Mathf.Clamp01(progress);
            transform.localScale = new Vector3(Mathf.Lerp(2.2f, 1f, p), 0.8f, 1f);
            SetBanner($"HOLD {(int)(p * 100f)}%", 0.3f);
        }

        /// <summary>Sleeper telegraph state; shape + text change with colour.</summary>
        public void SetSleeperArmed(bool armed)
        {
            _sleeperArmed = armed;
        }

        public void Resolve(TimingGrade grade, Vector3 coreLocalPosition, bool destroyed)
        {
            _visible = true;
            _hideAt = Time.unscaledTime + resolveHideSeconds;
            if (destroyed)
            {
                StartShatter();
            }
            else
            {
                StartDash(coreLocalPosition);
            }

            if (_label == null)
            {
                return;
            }

            // Single line: the grade is the feedback, colour only reinforces.
            _label.text = grade.ToString().ToUpperInvariant();
            _label.color = ArenaPresenter.GradeColor(grade);
        }

        public void ResolveDecoy(bool shielded, Vector3 coreLocalPosition, bool destroyed)
        {
            _visible = true;
            _hideAt = Time.unscaledTime + resolveHideSeconds;
            if (destroyed)
            {
                StartShatter();
            }
            // Ignored decoy: correct play, just expires in place.

            if (_label == null)
            {
                return;
            }

            _label.text = shielded ? "DECOY -5" : "IGNORED";
            _label.color = shielded ? UiTypography.Warn : UiTypography.Dim;
        }

        public void Hide()
        {
            _visible = false;
            _dashing = false;
            _shattering = false;
            _hideAt = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Drives the approach ring from DSP time. The ring shrinks from full
        /// to minimum across the travel and shifts colour inside the Good and
        /// Perfect windows, so the shrink — not colour — is the primary cue.
        /// </summary>
        public void SetRing(double msToImpact, double perfectMs, double goodMs)
        {
            if (_ring == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01((float)(msToImpact / _travelMs));
            float scale = Mathf.Lerp(ringMinScale, ringFullScale, clamped);
            _ring.transform.localScale = new Vector3(scale, scale, 1f);

            double abs = Math.Abs(msToImpact);
            _ring.color = abs <= perfectMs
                ? new Color(0.45f, 1f, 0.60f, 0.9f)
                : abs <= goodMs
                    ? new Color(1f, 0.93f, 0.45f, 0.9f)
                    : new Color(0.85f, 0.9f, 1f, 0.55f);
        }

        /// <summary>
        /// Kamikaze dive into the Core on resolve: eases from the impact
        /// spot to the Core while shrinking, then lingers the grade text
        /// until Hide. Pure presentation; judging already happened.
        /// </summary>
        private void StartDash(Vector3 coreLocalPosition)
        {
            _dashing = true;
            _shattering = false;
            _dashFrom = transform.localPosition;
            _dashFromScale = transform.localScale;
            _dashTarget = coreLocalPosition;
            _dashUntil = Time.unscaledTime + Mathf.Max(0.01f, dashSeconds);
        }

        /// <summary>
        /// Shatter pop in place when the player (or AIDA) destroys the
        /// threat: quick ease-out expand, then Hide cuts it. The grade text
        /// stays on the arena label. Pure presentation.
        /// </summary>
        private void StartShatter()
        {
            _dashing = false;
            _shattering = true;
            _shatterFrom = transform.localScale;
            _shatterUntil = Time.unscaledTime + Mathf.Max(0.01f, shatterSeconds);
        }

        private void Update()
        {
            if (_shattering)
            {
                float remaining = _shatterUntil - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    Hide();
                    return;
                }

                float progress = 1f - remaining / Mathf.Max(0.01f, shatterSeconds);
                float eased = 1f - (1f - progress) * (1f - progress);
                transform.localScale = _shatterFrom * Mathf.Lerp(1f, shatterPeakScale, eased);
            }
            else if (_dashing)
            {
                float remaining = _dashUntil - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    _dashing = false;
                    transform.localPosition = _dashTarget;
                    transform.localScale = _dashFromScale * dashEndScale;
                }
                else
                {
                    // Ease-in dive: lambat di awal, menghujam ke Core.
                    float progress = 1f - remaining / Mathf.Max(0.01f, dashSeconds);
                    float eased = progress * progress;
                    transform.localPosition = Vector3.Lerp(_dashFrom, _dashTarget, eased);
                    float scale = Mathf.Lerp(1f, dashEndScale, eased);
                    transform.localScale = _dashFromScale * scale;
                }
            }

            if (_visible && _hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                Hide();
            }
        }

        private TextMesh FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }
    }
}
