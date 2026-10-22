using AlgoRhythm.Core.Combat;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// The single orbiting shield. It parks at the controller's angle and
    /// pulses on a successful block; it never hides on a timer. The bar
    /// itself is the cue; the debug text label stays hidden.
    /// The same component also drives the four directional block flashes
    /// (ShieldBlock_*): FlashBlock shows a real shield GO in the pressed
    /// lane and auto-hides, so shield feedback reads in the arena instead
    /// of as HUD text. Blocks never Pulse; the orbiter never FlashBlocks.
    /// </summary>
    public sealed class ShieldView : MonoBehaviour
    {
        private const float BlockFadeSeconds = 0.15f;

        private TextMesh _label;
        private Vector3 _baseScale;
        private float _pulseUntil;
        private float _blockHideAt;
        private float _blockHoldTotal = 0.3f;
        private float _blockPunch = 1f;

        public void Initialize()
        {
            Transform label = transform.Find("Label");
            _label = label != null ? label.GetComponent<TextMesh>() : null;
            if (_label != null)
            {
                _label.text = string.Empty;
                _label.gameObject.SetActive(false);
            }

            _baseScale = transform.localScale;
            gameObject.SetActive(true);
        }

        public void Park(Vector3 localPosition, float angleDeg, string slotCode)
        {
            _ = slotCode;
            gameObject.SetActive(true);
            transform.localPosition = localPosition;
            // Sama seperti PaddleView: negasi agar sumbu panjang tetap tangen.
            transform.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
        }

        public void Pulse()
        {
            if (_blockHideAt > 0f)
            {
                return;
            }

            _pulseUntil = Time.unscaledTime + 0.28f;
            transform.localScale = _baseScale * 1.45f;
        }

        /// <summary>
        /// Shows this GO as a shield block in its authored lane: pops to
        /// punch scale, then eases back and auto-hides after holdSeconds.
        /// </summary>
        public void FlashBlock(float punch, float holdSeconds)
        {
            _blockPunch = Mathf.Max(1f, punch);
            _blockHoldTotal = Mathf.Max(0.05f, holdSeconds);
            _blockHideAt = Time.unscaledTime + _blockHoldTotal;
            _pulseUntil = 0f;
            gameObject.SetActive(true);
            transform.localScale = _baseScale * _blockPunch;
        }

        private void Update()
        {
            if (_blockHideAt > 0f)
            {
                float remaining = _blockHideAt - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    _blockHideAt = 0f;
                    transform.localScale = _baseScale;
                    gameObject.SetActive(false);
                }
                else
                {
                    // Two-phase read: punch eases back to 1 over the hold,
                    // then the block shrinks smoothly to 0 instead of
                    // popping out, so fast lane switches don't leave a
                    // hard disappearing edge behind.
                    float fade = Mathf.Min(BlockFadeSeconds, _blockHoldTotal * 0.5f);
                    if (remaining <= fade && fade > 0f)
                    {
                        float s = Mathf.SmoothStep(0f, 1f, remaining / fade);
                        transform.localScale = _baseScale * s;
                    }
                    else
                    {
                        float body = Mathf.Max(_blockHoldTotal - fade, Mathf.Epsilon);
                        float p = Mathf.Clamp01((_blockHoldTotal - remaining) / body);
                        transform.localScale = _baseScale * Mathf.Lerp(_blockPunch, 1f, p * p);
                    }
                }

                return;
            }

            if (_pulseUntil > 0f)
            {
                if (Time.unscaledTime >= _pulseUntil)
                {
                    _pulseUntil = 0f;
                    transform.localScale = _baseScale;
                }
                else
                {
                    float t = 1f - (_pulseUntil - Time.unscaledTime) / 0.28f;
                    float scale = Mathf.Lerp(1.45f, 1.0f, t * t);
                    transform.localScale = _baseScale * scale;
                }
            }
        }
    }
}
