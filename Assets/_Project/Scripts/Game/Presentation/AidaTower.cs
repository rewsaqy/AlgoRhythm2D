using System.Collections.Generic;
using AlgoRhythm.Core.Rhythm;
using UnityEngine;

namespace AlgoRhythm.Game.Presentation
{
    /// <summary>
    /// A.I.D.A. v1 helper tower (demo ally [U], not production canon). Greybox
    /// Diep.io-style tank parked at the scene-authored position: round body,
    /// one rotating barrel, name + confusion flavor labels. Owns a small pool
    /// of shot views; fire schedule, aim error and resolution stay with the
    /// combat controller. Never creates GameObjects at runtime.
    /// </summary>
    public sealed class AidaTower : MonoBehaviour
    {
        private readonly List<AidaShotView> _shots = new List<AidaShotView>();
        private Transform _barrelPivot;
        private TextMesh _flavor;
        private BeatClock _clock;
        private IClockSource _source;

        public void Initialize(BeatClock clock, IClockSource source)
        {
            _clock = clock;
            _source = source;
            _barrelPivot = transform.Find("BarrelPivot");

            Transform flavor = transform.Find("Flavor");
            _flavor = flavor != null ? flavor.GetComponent<TextMesh>() : null;

            Transform nameplate = transform.Find("Nameplate");
            TextMesh nameText = nameplate != null ? nameplate.GetComponent<TextMesh>() : null;
            if (nameText != null)
            {
                nameText.text = "A.I.D.A v1.0";
            }

            Transform pool = transform.Find("Shots");
            if (pool != null)
            {
                foreach (AidaShotView view in pool.GetComponentsInChildren<AidaShotView>(true))
                {
                    view.Initialize();
                    _shots.Add(view);
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
            for (int index = 0; index < _shots.Count; index++)
            {
                if (_shots[index].IsAvailable)
                {
                    return _shots[index];
                }
            }

            return null;
        }
    }
}
