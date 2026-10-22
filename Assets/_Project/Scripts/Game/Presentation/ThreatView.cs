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
        private bool _shattering;
        private float _shatterUntil;

        public bool IsAvailable => !gameObject.activeSelf;

        public void Initialize()
        {
            _label = FindText("Label");
            Transform ring = transform.Find("Ring");
            _ring = ring != null ? ring.GetComponent<SpriteRenderer>() : null;
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

            SetRing(_travelMs, 70.0, 130.0);
            SetBuffered(false);
        }

        public void SetBuffered(bool buffered)
        {
            if (_label == null)
            {
                return;
            }

            if (_threat.Kind == ThreatKind.AdwareDecoy)
            {
                _label.text = buffered ? "? DECOY\n+BUFFER" : "? DECOY";
                _label.color = UiTypography.Decoy;
                return;
            }

            string code = ArenaPresenter.DirectionCode(_threat.Direction);
            string glyph = ArenaPresenter.DirectionGlyph(_threat.Direction);
            _label.text = buffered ? $"{glyph} {code}\n+BUFFER" : $"{glyph} {code}";
            _label.color = buffered ? UiTypography.Info : UiTypography.Ink;
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
            _dashFrom = transform.localPosition;
            _dashTarget = coreLocalPosition;
            _dashUntil = Time.unscaledTime + Mathf.Max(0.01f, dashSeconds);
            _dashing = true;
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
                float scale = Mathf.Lerp(1f, shatterPeakScale, eased);
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else if (_dashing)
            {
                float remaining = _dashUntil - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    _dashing = false;
                    transform.localPosition = _dashTarget;
                    transform.localScale = Vector3.one * dashEndScale;
                }
                else
                {
                    // Ease-in dive: lambat di awal, menghujam ke Core.
                    float progress = 1f - remaining / Mathf.Max(0.01f, dashSeconds);
                    float eased = progress * progress;
                    transform.localPosition = Vector3.Lerp(_dashFrom, _dashTarget, eased);
                    float scale = Mathf.Lerp(1f, dashEndScale, eased);
                    transform.localScale = new Vector3(scale, scale, 1f);
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
