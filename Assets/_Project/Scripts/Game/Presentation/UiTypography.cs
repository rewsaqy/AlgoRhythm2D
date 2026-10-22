using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Shared visual tokens for all greybox UI. One palette, one set of text
    /// styles; every view styles its manually authored TextMesh labels through
    /// <see cref="Style"/> so hierarchy and contrast stay consistent.
    /// No shaders, no new materials, no GameObject creation.
    /// </summary>
    public static class UiTypography
    {
        public static readonly Color Ink = new Color(0.95f, 0.97f, 1.0f, 1.0f);
        public static readonly Color Dim = new Color(0.72f, 0.80f, 0.90f, 0.62f);
        public static readonly Color Faint = new Color(0.72f, 0.80f, 0.90f, 0.42f);
        public static readonly Color Perfect = new Color(0.45f, 1.0f, 0.60f, 1.0f);
        public static readonly Color Good = new Color(1.0f, 0.93f, 0.45f, 1.0f);
        public static readonly Color Miss = new Color(1.0f, 0.52f, 0.52f, 1.0f);
        public static readonly Color Decoy = new Color(0.85f, 0.76f, 1.0f, 1.0f);
        public static readonly Color Info = new Color(0.55f, 0.90f, 1.0f, 1.0f);
        public static readonly Color Warn = new Color(1.0f, 0.76f, 0.32f, 1.0f);
        public static readonly Color Status = new Color(1.0f, 0.93f, 0.70f, 1.0f);

        /// <summary>
        /// Display font for every scene TextMesh. Assigned by GameBootstrap
        /// from a serialized Inspector reference; when null, labels keep
        /// their baked font. Set once at boot, read by <see cref="Style"/>.
        /// </summary>
        public static Font DisplayFont { get; set; }

        public static void Style(
            TextMesh label,
            int fontSize,
            float characterSize,
            Color color,
            TextAnchor anchor,
            TextAlignment alignment,
            float lineSpacing = 1.0f)
        {
            if (label == null)
            {
                return;
            }

            if (DisplayFont != null)
            {
                label.font = DisplayFont;
            }

            label.fontSize = fontSize;
            label.characterSize = characterSize;
            label.color = color;
            label.anchor = anchor;
            label.alignment = alignment;
            label.lineSpacing = lineSpacing;
            label.richText = true;
        }
    }
}
