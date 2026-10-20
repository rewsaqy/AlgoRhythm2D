using System;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Serialized greybox threat visual. The view is authored in the scene and
    /// pooled by activation; it never creates GameObjects at runtime.
    /// </summary>
    public sealed class ThreatView : MonoBehaviour
    {
        private const float RingFullScale = 1.8f;
        private const float RingMinScale = 0.45f;

        private TextMesh _label;
        private SpriteRenderer _ring;
        private ThreatEvent _threat;
        private bool _visible;
        private float _hideAt;
        private float _travelMs = 1f;

        public bool IsAvailable => !gameObject.activeSelf;

        public void Initialize()
        {
            _label = FindText("Label");
            Transform ring = transform.Find("Ring");
            _ring = ring != null ? ring.GetComponent<SpriteRenderer>() : null;
            Hide();
        }

        public void Show(ThreatEvent threat, float travelMs)
        {
            _threat = threat;
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
                _label.text = buffered ? "?\nDECOY+\nBUFFER" : "?\nDECOY";
                _label.color = Color.white;
                return;
            }

            string code = ArenaPresenter.DirectionCode(_threat.Direction);
            string glyph = ArenaPresenter.DirectionGlyph(_threat.Direction);
            _label.text = buffered
                ? $"{code}\n{glyph}+\nBUFFER"
                : $"{code}\n{glyph}";
            _label.color = Color.white;
        }

        public void Resolve(TimingGrade grade)
        {
            _visible = true;
            _hideAt = Time.unscaledTime + 0.45f;

            if (_label == null)
            {
                return;
            }

            _label.text = grade == TimingGrade.Miss
                ? "MISS"
                : $"SHIELD\n{grade}";
            _label.color = ArenaPresenter.GradeColor(grade);
        }

        public void ResolveDecoy(bool shielded)
        {
            _visible = true;
            _hideAt = Time.unscaledTime + 0.45f;

            if (_label == null)
            {
                return;
            }

            _label.text = shielded ? "DECOY\n-5" : "DECOY\nIGNORED";
            _label.color = shielded
                ? new Color(1f, 0.95f, 0.35f, 1f)
                : new Color(0.75f, 0.75f, 0.75f, 1f);
        }

        public void Hide()
        {
            _visible = false;
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
            float scale = Mathf.Lerp(RingMinScale, RingFullScale, clamped);
            _ring.transform.localScale = new Vector3(scale, scale, 1f);

            double abs = Math.Abs(msToImpact);
            _ring.color = abs <= perfectMs
                ? new Color(0.4f, 1f, 0.55f, 0.9f)
                : abs <= goodMs
                    ? new Color(1f, 0.95f, 0.35f, 0.9f)
                    : new Color(0.85f, 0.9f, 1f, 0.55f);
        }

        private void Update()
        {
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
