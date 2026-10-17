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
        private int[] _landedBodies;
        private bool[] _inFlight;
        private bool[] _visible;
        private int _count;
        private int _inFlightCount;
        private float _speed;
        private float _travelled;

        public void Launch(SignalSO settings, Orrery orrery, SignalLauncher launcher, Material dotMaterial,
            Vector2 origin, Vector2 direction, float widthDegrees, float speed, int dotCount)
        {
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
            _landedBodies = new int[_count];
            _inFlight = new bool[_count];
            _visible = new bool[_count];

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float halfWidth = widthDegrees * 0.5f;

            for (int i = 0; i < _count; i++)
            {
                float t = _count == 1 ? 0.5f : i / (float)(_count - 1);
                float angle = (baseAngle - halfWidth + widthDegrees * t) * Mathf.Deg2Rad;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _positions[i] = origin;
                _landedBodies[i] = -1;
                _inFlight[i] = true;
                _visible[i] = true;
            }

            _dots = new SignalDotMesh(gameObject, dotMaterial, settings.SignalColor, _count);
            _dots.Rebuild(_positions, _visible, _count, settings.DotSize);
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

            if (_inFlightCount > 0)
            {
                _travelled += _speed * Time.deltaTime;
                AdvanceDots();
            }

            FollowLandedBodies();
            _dots.Rebuild(_positions, _visible, _count, _settings.DotSize);
        }

        private void AdvanceDots()
        {
            bool beyondRange = _travelled >= _settings.MaxTravelRadius;

            for (int i = 0; i < _count; i++)
            {
                if (!_inFlight[i])
                    continue;

                _positions[i] = _origin + _directions[i] * _travelled;

                int body = FindBodyUnder(i);

                if (body >= 0)
                {
                    _inFlight[i] = false;
                    _inFlightCount--;
                    _landedBodies[i] = body;
                    _landedOffsets[i] = LandedOffsetFor(i, body);
                    _positions[i] = (Vector2)_orrery.GetBodyTransform(body).position + _landedOffsets[i];
                    _orrery.FlashBody(body, _settings.HitFlashColor, _settings.HitFlashSeconds);
                    _launcher.ReportHit(_orrery.GetBody(body).DisplayName);
                    continue;
                }

                if (!beyondRange)
                    continue;

                _inFlight[i] = false;
                _inFlightCount--;
                _visible[i] = false;
            }
        }

        private Vector2 LandedOffsetFor(int dot, int body)
        {
            Vector2 outward = _positions[dot] - (Vector2)_orrery.GetBodyTransform(body).position;

            if (outward.sqrMagnitude < 0.000001f)
                outward = -_directions[dot];

            return outward.normalized * (_orrery.GetHitRadius(body) + _settings.LandedSurfaceOffset);
        }

        private void FollowLandedBodies()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_inFlight[i] || _landedBodies[i] < 0)
                    continue;

                Transform body = _orrery.GetBodyTransform(_landedBodies[i]);

                if (body == null)
                    continue;

                _positions[i] = (Vector2)body.position + _landedOffsets[i];
            }
        }

        private int FindBodyUnder(int dot)
        {
            for (int body = 0; body < _orrery.BodyCount; body++)
            {
                if (_orrery.IsHitAt(body, _positions[dot]))
                    return body;
            }

            return -1;
        }
    }
}
