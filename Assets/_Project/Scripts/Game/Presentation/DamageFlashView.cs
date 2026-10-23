using System;
using AlgoRhythm.Core.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Red damage vignette. A big ring SpriteRenderer centered on the camera
    /// flashes on integrity loss (CombatState.IntegrityChanged) and fades
    /// out, so telegraphs stay readable while the hit still lands. Pure
    /// presentation; no gameplay authority. Attach to a SpriteRenderer child
    /// of Main Camera; all tuning is Inspector-editable [U].
    /// </summary>
    public sealed class DamageFlashView : MonoBehaviour
    {
        [Header("Flash [U] — merah sesaat, bukan overlay permanen")]
        [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);
        [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.75f;
        [Min(0.05f)] [SerializeField] private float holdSeconds = 0.12f;
        [Min(0.05f)] [SerializeField] private float fadeSeconds = 0.5f;

        private SpriteRenderer _renderer;
        private CombatState _combat;
        private float _flashUntil;
        private float _fadeUntil;

        public void Initialize(CombatState combat)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogWarning(
                    "DamageFlashView needs a SpriteRenderer on the same GameObject.",
                    this);
                return;
            }

            SetAlpha(0f);
            _combat.IntegrityChanged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (_combat != null)
            {
                _combat.IntegrityChanged -= OnDamaged;
            }
        }

        private void OnDamaged(int _)
        {
            Flash();
        }

        /// <summary>
        /// Explicit flash trigger for dramatic events or outro cinematics.
        /// </summary>
        public void Flash(float customAlpha = -1f)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            float target = customAlpha > 0f ? Mathf.Clamp01(customAlpha) : maxAlpha;
            float now = Time.unscaledTime;
            _flashUntil = now + holdSeconds;
            _fadeUntil = _flashUntil + fadeSeconds;
            SetAlpha(target);
        }

        private void Update()
        {
            if (_renderer == null || _fadeUntil <= 0f)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _flashUntil)
            {
                SetAlpha(maxAlpha);
            }
            else if (now < _fadeUntil)
            {
                float u = 1f - (now - _flashUntil) / Mathf.Max(0.01f, fadeSeconds);
                SetAlpha(maxAlpha * u * u);
            }
            else
            {
                _fadeUntil = 0f;
                SetAlpha(0f);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_renderer == null)
            {
                return;
            }

            Color color = flashColor;
            color.a = Mathf.Clamp01(alpha);
            _renderer.color = color;
        }
    }
}
