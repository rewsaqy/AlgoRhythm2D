using System.Collections;
using System.Collections.Generic;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// A.I.D.A. helper tower with visual evolution and shot management.
    /// Diep.io-style cyber tank that evolves visually across shop upgrade levels:
    /// body scales up, barrel expands, and nameplate/glow color shifts.
    /// Manages pooled shot views cleanly with zero ghosting or stuck bullets.
    /// </summary>
    public sealed class AidaTower : MonoBehaviour
    {
        private readonly List<AidaShotView> _shots = new List<AidaShotView>();
        private Transform _body;
        private Transform _barrelPivot;
        private Transform _barrel;
        private TextMesh _nameText;
        private TextMesh _flavor;
        private IClockSource _source;
        private Renderer _bodyRenderer;
        private Renderer _barrelRenderer;
        private Vector3 _baseBodyScale = Vector3.one;
        private Vector3 _baseBarrelScale = Vector3.one;
        private int _currentLevel = 1;
        private Coroutine _upgradePunchCoroutine;

        public void Initialize(BeatClock clock, IClockSource source)
        {
            _ = clock;
            _source = source;

            _body = transform.Find("Body");
            if (_body != null)
            {
                _baseBodyScale = _body.localScale;
                _bodyRenderer = _body.GetComponent<Renderer>();
            }

            _barrelPivot = transform.Find("BarrelPivot");
            _barrel = _barrelPivot != null ? _barrelPivot.Find("Barrel") : null;
            if (_barrel != null)
            {
                _baseBarrelScale = _barrel.localScale;
                _barrelRenderer = _barrel.GetComponent<Renderer>();
            }

            Transform flavor = transform.Find("Flavor");
            if (flavor != null)
            {
                flavor.gameObject.SetActive(false);
            }
            _flavor = null;

            Transform nameplate = transform.Find("Nameplate");
            if (nameplate != null)
            {
                nameplate.gameObject.SetActive(false);
            }
            _nameText = null;

            Transform pool = transform.Find("Shots");
            if (pool != null)
            {
                foreach (AidaShotView view in pool.GetComponentsInChildren<AidaShotView>(true))
                {
                    view.Initialize();
                    _shots.Add(view);
                }
            }

            SetLevel(1, animate: false);
        }

        /// <summary>
        /// Evolves AIDA's visual appearance and title according to the current shop tier.
        /// </summary>
        public void SetLevel(int level, bool animate = true)
        {
            _currentLevel = Mathf.Clamp(level, 1, 5);

            string title;
            Color themeColor;
            float bodyMult;
            Vector3 barrelMult;

            switch (_currentLevel)
            {
                case 1:
                    title = "A.I.D.A v1.0 [SCOUT]";
                    themeColor = new Color(0f, 0.9f, 1f, 1f); // Neon Cyan
                    bodyMult = 1.0f;
                    barrelMult = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case 2:
                    title = "A.I.D.A v2.0 [SENTINEL]";
                    themeColor = new Color(0f, 1f, 0.78f, 1f); // Cyber Teal
                    bodyMult = 1.15f;
                    barrelMult = new Vector3(1.25f, 1.3f, 1.0f);
                    break;
                case 3:
                    title = "A.I.D.A v3.0 [ASSAULT CORE]";
                    themeColor = new Color(1f, 0.84f, 0f, 1f); // Golden Amber
                    bodyMult = 1.3f;
                    barrelMult = new Vector3(1.5f, 1.6f, 1.0f);
                    break;
                case 4:
                    title = "A.I.D.A v4.0 [OVERCLOCK]";
                    themeColor = new Color(1f, 0.1f, 0.6f, 1f); // Hyper Magenta
                    bodyMult = 1.45f;
                    barrelMult = new Vector3(1.75f, 1.9f, 1.0f);
                    break;
                default:
                    title = "A.I.D.A v5.0 [MAGNUM OPUS]";
                    themeColor = new Color(0.85f, 0.98f, 1f, 1f); // Luminous Diamond
                    bodyMult = 1.65f;
                    barrelMult = new Vector3(2.1f, 2.3f, 1.0f);
                    break;
            }

            if (_nameText != null)
            {
                _nameText.text = title;
                _nameText.color = themeColor;
            }

            // Apply color to body and barrel materials
            if (_bodyRenderer != null && _bodyRenderer.material != null)
            {
                _bodyRenderer.material.color = themeColor;
            }

            if (_barrelRenderer != null && _barrelRenderer.material != null)
            {
                _barrelRenderer.material.color = Color.Lerp(themeColor, Color.white, 0.3f);
            }

            Vector3 targetBodyScale = _baseBodyScale * bodyMult;
            Vector3 targetBarrelScale = Vector3.Scale(_baseBarrelScale, barrelMult);

            if (animate && gameObject.activeInHierarchy)
            {
                if (_upgradePunchCoroutine != null)
                {
                    StopCoroutine(_upgradePunchCoroutine);
                }
                _upgradePunchCoroutine = StartCoroutine(AnimateUpgradePunch(targetBodyScale, targetBarrelScale));
            }
            else
            {
                if (_body != null) _body.localScale = targetBodyScale;
                if (_barrel != null) _barrel.localScale = targetBarrelScale;
            }
        }

        private IEnumerator AnimateUpgradePunch(Vector3 targetBody, Vector3 targetBarrel)
        {
            float duration = 0.4f;
            float elapsed = 0f;
            Vector3 punchBody = targetBody * 1.35f;
            Vector3 punchBarrel = targetBarrel * 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Elastic overshoot curve
                float s = Mathf.Sin(t * Mathf.PI);
                if (_body != null) _body.localScale = Vector3.Lerp(targetBody, punchBody, s);
                if (_barrel != null) _barrel.localScale = Vector3.Lerp(targetBarrel, punchBarrel, s);
                yield return null;
            }

            if (_body != null) _body.localScale = targetBody;
            if (_barrel != null) _barrel.localScale = targetBarrel;
            _upgradePunchCoroutine = null;
        }

        /// <summary>
        /// Clears all active shots instantly. Guarantees no stuck ghost shells on restart or clear.
        /// </summary>
        public void ClearShots()
        {
            for (int i = 0; i < _shots.Count; i++)
            {
                if (_shots[i] != null)
                {
                    _shots[i].Hide();
                }
            }
        }

        /// <summary>Aims the barrel along a lane angle (compass degrees).</summary>
        public void SetAim(float angleDeg)
        {
            if (_barrelPivot == null)
            {
                return;
            }

            // The barrel mesh points +Y; rotating -angle lands it on (sin, cos).
            _barrelPivot.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
        }

        /// <summary>
        /// World position of the barrel tip (muzzle), computed through the
        /// pivot so shots emerge from the barrel mouth instead of the tower
        /// center. Falls back to the tower center when the rig is missing.
        /// </summary>
        public Vector3 MuzzleWorldPosition()
        {
            if (_barrelPivot == null)
            {
                return transform.position;
            }

            Vector3 tipLocal = Vector3.up * 0.75f;
            if (_barrel != null)
            {
                tipLocal = _barrel.localPosition + Vector3.up * (_barrel.localScale.y * 0.5f);
            }

            return _barrelPivot.TransformPoint(tipLocal);
        }

        public void SetFlavor(string line)
        {
            if (_flavor != null)
            {
                _flavor.text = line ?? string.Empty;
            }
        }

        public AidaShotView FireShot(
            Vector3 fromWorld,
            Vector3 toWorld,
            int targetId,
            bool willHit,
            double arrivalDsp)
        {
            AidaShotView view = FindAvailableShot();
            if (view == null || _source == null)
            {
                return null;
            }

            view.Fire(
                transform.InverseTransformPoint(fromWorld),
                transform.InverseTransformPoint(toWorld),
                targetId,
                willHit,
                _source.NowDsp,
                arrivalDsp);
            return view;
        }

        /// <summary>Moves live shots; returns the ones that arrived this tick.</summary>
        public List<AidaShotView> UpdateShots(double nowDsp, List<AidaShotView> arrived)
        {
            arrived.Clear();
            for (int index = 0; index < _shots.Count; index++)
            {
                AidaShotView view = _shots[index];
                if (view.IsAvailable)
                {
                    continue;
                }

                float progress = view.ProgressAt(nowDsp);
                view.PlaceAt(progress);
                if (progress >= 1f)
                {
                    arrived.Add(view);
                }
            }

            return arrived;
        }

        private AidaShotView FindAvailableShot()
        {
            // 1. Return first idle shot
            for (int index = 0; index < _shots.Count; index++)
            {
                if (_shots[index].IsAvailable)
                {
                    return _shots[index];
                }
            }

            // 2. Recycle oldest shot if pool is saturated (prevents weapon freeze/jamming)
            AidaShotView oldest = null;
            float maxProgress = -1f;
            double now = _source != null ? _source.NowDsp : 0.0;
            for (int index = 0; index < _shots.Count; index++)
            {
                float p = _shots[index].ProgressAt(now);
                if (p > maxProgress)
                {
                    maxProgress = p;
                    oldest = _shots[index];
                }
            }

            if (oldest != null)
            {
                oldest.Hide();
                return oldest;
            }

            return null;
        }
    }
}
