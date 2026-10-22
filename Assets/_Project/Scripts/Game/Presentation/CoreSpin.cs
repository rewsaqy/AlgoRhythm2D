using System.Collections.Generic;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// Ambient planet-style spin for the Core visual. Pure presentation:
    /// rotates the Core's mesh layers slowly in place. No gameplay, timing,
    /// or scoring effect — rhythm authority stays on DSP time.
    /// Even-indexed mesh children use the outer settings, odd-indexed the
    /// inner settings, so the layers counter-rotate like a planet system.
    /// Attach to Arena/Core; discovers mesh children at Awake, creates
    /// nothing at runtime. All tuning is Inspector-editable [U].
    /// </summary>
    public sealed class CoreSpin : MonoBehaviour
    {
        [Header("Outer layer [U] — derajat per detik")]
        [SerializeField] private Vector3 outerAxis = Vector3.up;
        [SerializeField] private float outerDegPerSecond = 12f;

        [Header("Inner layer [U] — counter-rotate")]
        [SerializeField] private Vector3 innerAxis = Vector3.up;
        [SerializeField] private float innerDegPerSecond = -20f;

        private Transform[] _layers = System.Array.Empty<Transform>();

        private void Awake()
        {
            var found = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null)
                {
                    found.Add(child);
                }
            }

            _layers = found.ToArray();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int index = 0; index < _layers.Length; index++)
            {
                Transform layer = _layers[index];
                if (layer == null)
                {
                    continue;
                }

                bool outer = index % 2 == 0;
                Vector3 axis = outer ? outerAxis : innerAxis;
                float speed = outer ? outerDegPerSecond : innerDegPerSecond;
                if (axis.sqrMagnitude > 0.0001f && speed != 0f)
                {
                    layer.Rotate(axis.normalized, speed * dt, Space.Self);
                }
            }
        }
    }
}
