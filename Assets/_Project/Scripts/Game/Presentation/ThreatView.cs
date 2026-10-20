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
        private TextMesh _label;
        private ThreatEvent _threat;
        private bool _visible;
        private float _hideAt;

        public bool IsAvailable => !gameObject.activeSelf;

        public void Initialize()
        {
            _label = FindText("Label");
            Hide();
        }

        public void Show(ThreatEvent threat)
        {
            _threat = threat;
            _visible = true;
            _hideAt = 0f;
            gameObject.SetActive(true);
            SetBuffered(false);
        }

        public void SetBuffered(bool buffered)
        {
            if (_label == null)
            {
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

        public void Hide()
        {
            _visible = false;
            _hideAt = 0f;
            gameObject.SetActive(false);
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
