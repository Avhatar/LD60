using UnityEngine;
using LD60.Space;

namespace LD60.Signals
{
    public class Signal : MonoBehaviour
    {
        private SignalSO _settings;
        private Orrery _orrery;
        private SignalLauncher _launcher;
        private SignalDotMesh _dots;
        private Vector2 _origin;
        private Vector2[] _directions;
        private Vector2[] _positions;
        private Vector2[] _landedOffsets;
        private Transform[] _landedOn;
        private bool[] _inFlight;
        private float[] _weights;
        private float[] _altBlends;
        private int _count;
        private int _inFlightCount;
        private float _speed;
        private float _travelled;
        private float _age;
        private Transform _ignored;

        public int DotCount
        {
            get { return _count; }
        }

        public Vector2 DotPosition(int index)
        {
            return _positions[index];
        }

        public Transform DotLandedOn(int index)
        {
            return _landedOn[index];
        }

        public bool IsDotInFlight(int index)
        {
            return _inFlight[index];
        }

        public void MarkDotSpent(int index)
        {
            if (!_inFlight[index])
                return;

            _inFlight[index] = false;
            _inFlightCount--;
            _altBlends[index] = 1f;
        }

        public void Launch(SignalSO settings, Orrery orrery, SignalLauncher launcher, Material dotMaterial,
            Vector2 origin, Vector2 direction, float widthDegrees, float speed, int dotCount, Transform ignored)
        {
            _ignored = ignored;
            _settings = settings;
            _orrery = orrery;
            _launcher = launcher;
            _origin = origin;
            _speed = speed;
            _travelled = 0f;
            _count = Mathf.Max(1, dotCount);
            _inFlightCount = _count;

            _directions = new Vector2[_count];
            _positions = new Vector2[_count];
            _landedOffsets = new Vector2[_count];
            _landedOn = new Transform[_count];
            _inFlight = new bool[_count];
            _weights = new float[_count];
            _altBlends = new float[_count];

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float halfWidth = widthDegrees * 0.5f;

            for (int i = 0; i < _count; i++)
            {
                float t = _count == 1 ? 0.5f : i / (float)(_count - 1);
                float angle = (baseAngle - halfWidth + widthDegrees * t) * Mathf.Deg2Rad;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _positions[i] = origin;
                _inFlight[i] = true;
                _weights[i] = 1f;
            }

            _dots = new SignalDotMesh(gameObject, dotMaterial, _count);
            _dots.SetLook(settings.SignalColor, settings.DotEmission, settings.DotEdgeSoftness);
            _dots.SetAltLook(settings.ArrivedColor, settings.ArrivedDotEmission);
            _dots.SetFade(settings.FadeForAge(0f));
            _dots.Rebuild(_positions, _weights, _altBlends, _count, settings.DotSize);
        }

        private void Update()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(Signal)}: {nameof(_settings)} is null in {nameof(Update)}. " +
                               "Required because the signal cannot advance without its speed and dot settings. " +
                               $"Fix: spawn signals through {nameof(SignalLauncher)}, which calls {nameof(Launch)}.");
                enabled = false;
                return;
            }

            _age += Time.deltaTime;

            if (_age >= _settings.DotLifetimeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            if (_inFlightCount > 0)
            {
                _travelled += _speed * Time.deltaTime;
                AdvanceDots();
            }

            FollowLandedBodies();
            _dots.SetFade(_settings.FadeForAge(_age));
            _dots.Rebuild(_positions, _weights, _altBlends, _count, _settings.DotSize);
        }

        private void AdvanceDots()
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_inFlight[i])
                    continue;

                _positions[i] = _origin + _directions[i] * _travelled;

                float hitRadius;
                int bodyIndex;
                Transform hit = _orrery.FindHitTarget(_positions[i], _ignored, out hitRadius, out bodyIndex);

                if (hit != null)
                {
                    _inFlight[i] = false;
                    _inFlightCount--;
                    _landedOn[i] = hit;
                    _landedOffsets[i] = LandedOffsetFor(i, hit, hitRadius);
                    _positions[i] = (Vector2)hit.position + _landedOffsets[i];

                    if (bodyIndex >= 0)
                    {
                        _orrery.FlashBody(bodyIndex, _settings.HitFlashColor, _settings.HitFlashSeconds);
                        _launcher.ReportHit(_orrery.GetBody(bodyIndex).LocalizedName);
                    }
                }
            }
        }

        private Vector2 LandedOffsetFor(int dot, Transform hit, float hitRadius)
        {
            Vector2 outward = _positions[dot] - (Vector2)hit.position;

            if (outward.sqrMagnitude < 0.000001f)
                outward = -_directions[dot];

            return outward.normalized * (hitRadius + _settings.LandedSurfaceOffset);
        }

        private void FollowLandedBodies()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_inFlight[i] || _landedOn[i] == null)
                    continue;

                _positions[i] = (Vector2)_landedOn[i].position + _landedOffsets[i];
            }
        }

    }
}
