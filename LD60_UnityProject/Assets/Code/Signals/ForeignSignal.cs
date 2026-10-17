using UnityEngine;
using LD60.Space;

namespace LD60.Signals
{
    public class ForeignSignal : MonoBehaviour
    {
        private SignalSO _settings;
        private Orrery _orrery;
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
        private float _endRadius;
        private CelestialBodySO _ignoredBody;
        private SignalDotMesh _arrivedDots;
        private bool[] _arrivedVisible;
        private float _age;

        public ActSpacecraftSO Source { get; private set; }

        public int DotCount
        {
            get { return _count; }
        }

        public Vector2 DotPosition(int index)
        {
            return _positions[index];
        }

        public bool IsDotCapturable(int index)
        {
            return _inFlight[index];
        }

        public void ConsumeDot(int index)
        {
            if (!_inFlight[index])
                return;

            _inFlight[index] = false;
            _inFlightCount--;
            _visible[index] = false;
        }

        public void Launch(SignalSO settings, ActSpacecraftSO source, Orrery orrery, Material dotMaterial,
            Vector2 origin, Vector2 aimPoint, CelestialBodySO ignoredBody, float clearance)
        {
            _settings = settings;
            _orrery = orrery;
            _ignoredBody = ignoredBody;
            Source = source;
            _origin = origin;
            _speed = source.Speed;
            _count = Mathf.Max(1, source.DotCount);
            _inFlightCount = _count;

            Vector2 toTarget = aimPoint - origin;
            _endRadius = toTarget.magnitude;
            _travelled = Mathf.Max(_endRadius * settings.ForeignSpawnFraction, clearance);

            Vector2 direction = _endRadius <= 0.0001f ? Vector2.right : toTarget / _endRadius;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float halfWidth = source.ArcWidthDegrees * 0.5f;

            _directions = new Vector2[_count];
            _positions = new Vector2[_count];
            _landedOffsets = new Vector2[_count];
            _landedBodies = new int[_count];
            _inFlight = new bool[_count];
            _visible = new bool[_count];

            for (int i = 0; i < _count; i++)
            {
                float t = _count == 1 ? 0.5f : i / (float)(_count - 1);
                float angle = (baseAngle - halfWidth + source.ArcWidthDegrees * t) * Mathf.Deg2Rad;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _positions[i] = origin + _directions[i] * _travelled;
                _landedBodies[i] = -1;
                _inFlight[i] = true;
                _visible[i] = true;
            }

            _arrivedVisible = new bool[_count];

            _dots = new SignalDotMesh(gameObject, dotMaterial, settings.ForeignColor, _count);
            _dots.Rebuild(_positions, _visible, _count, settings.ForeignDotSize);

            GameObject arrived = new GameObject("Arrived");
            arrived.transform.SetParent(transform, false);
            _arrivedDots = new SignalDotMesh(arrived, dotMaterial, settings.ArrivedColor, _count);
            _arrivedDots.Rebuild(_positions, _arrivedVisible, _count, settings.ForeignDotSize);
        }

        private void Update()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(ForeignSignal)}: {nameof(_settings)} is null in {nameof(Update)}. " +
                               "Required because the transmission cannot advance without its speed and dot settings. " +
                               $"Fix: spawn foreign transmissions through {nameof(ForeignSignalDirector)}, which calls {nameof(Launch)}.");
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
            _dots.Rebuild(_positions, _visible, _count, _settings.ForeignDotSize);
            _arrivedDots.Rebuild(_positions, _arrivedVisible, _count, _settings.ForeignDotSize);
        }

        private void AdvanceDots()
        {
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

                    if (_orrery.GetBody(body) != Source.TransmitTo)
                        continue;

                    _visible[i] = false;
                    _arrivedVisible[i] = true;
                }
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
                if (_orrery.GetBody(body) == _ignoredBody)
                    continue;

                if (_orrery.IsHitAt(body, _positions[dot]))
                    return body;
            }

            return -1;
        }
    }
}
