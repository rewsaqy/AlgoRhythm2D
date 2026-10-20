using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>Serialized four-direction shield flash. No runtime object creation.</summary>
    public sealed class ShieldView : MonoBehaviour
    {
        [SerializeField] private Direction direction;

        private TextMesh _label;
        private float _hideAt;

        public Direction Direction => direction;

        public void Initialize()
        {
            _label = FindText("Label");
            Hide();
        }

        public void Show(TimingGrade grade)
        {
            gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + 0.35f;

            if (_label == null)
            {
                return;
            }

            _label.text = "SHIELD";
            _label.color = ArenaPresenter.GradeColor(grade);
        }

        public void Hide()
        {
            _hideAt = 0f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
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
