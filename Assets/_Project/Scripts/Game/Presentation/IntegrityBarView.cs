using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Blood-frame integrity bar. Shows one DARAH sprite frame selected by
    /// integrity fraction: frame 0 = full (100%), last frame = empty.
    /// Pure presentation — HudView keeps the numeric INTEGRITY x/y text so
    /// the value never reads by image alone. Frames are Inspector-wired;
    /// nothing is created at runtime.
    /// </summary>
    public sealed class IntegrityBarView : MonoBehaviour
    {
        [Tooltip("DARAH_0 (full) .. DARAH_12 (empty), in order.")]
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();

        private SpriteRenderer _renderer;
        private int _shown = -1;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public int FrameCount => frames != null ? frames.Length : 0;

        /// <summary>fraction 0..1 → last frame..frame 0. No-op when unwired.</summary>
        public void SetFraction(float fraction)
        {
            if (_renderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fraction);
            int index = (frames.Length - 1) - Mathf.FloorToInt(clamped * (frames.Length - 1));
            if (index == _shown)
            {
                return;
            }

            _shown = index;
            _renderer.sprite = frames[index];
        }
    }
}
