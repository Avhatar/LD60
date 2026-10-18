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
        private Transform[] _landedOn;
        private bool[] _inFlight;
        private float[] _weights;
        private float[] _trippedBlends;
        private float[] _lifeLeft;
        private Camera _camera;
        private int _count;
        private int _inFlightCount;
        private float _speed;
        private float _travelled;
        private float _endRadius;
        private Transform _ignored;

        public ActSpacecraftSO Source { get; private set; }

        public void ApplyBaseLook(bool studied, bool exhausted)
        {
            if (_dots == null || _settings == null)
                return;

            if (exhausted)
            {
                _dots.SetLook(_settings.ExhaustedColor, _settings.ExhaustedDotEmission, _settings.DotEdgeSoftness);
                return;
            }

            _dots.SetLook(studied ? _settings.StudiedColor : _settings.ForeignColor,
                          studied ? _settings.StudiedDotEmission : _settings.ForeignDotEmission,
                          _settings.DotEdgeSoftness);
        }

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

            GroundDot(index);
            StartFadeOut(index);
        }

        public void TripDot(int index)
        {
            if (!_inFlight[index])
                return;

            GroundDot(index);
            StartFadeOut(index);
            _trippedBlends[index] = 1f;
        }

        private void GroundDot(int index)
        {
            if (!_inFlight[index])
                return;

            _inFlight[index] = false;
            _inFlightCount--;
        }

        private void StartFadeOut(int index)
        {
            if (_lifeLeft[index] > _settings.DotFadeOutSeconds)
                _lifeLeft[index] = _settings.DotFadeOutSeconds;
        }

        public void Launch(SignalSO settings, ActSpacecraftSO source, Orrery orrery, Material dotMaterial,
            Vector2 origin, Vector2 aimPoint, Transform ignored, float clearance, bool studied, bool exhausted)
        {
            _settings = settings;
            _orrery = orrery;
            _ignored = ignored;
            _camera = Camera.main;
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
            _landedOn = new Transform[_count];
            _inFlight = new bool[_count];
            _weights = new float[_count];
            _trippedBlends = new float[_count];
            _lifeLeft = new float[_count];

            for (int i = 0; i < _count; i++)
            {
                float t = _count == 1 ? 0.5f : i / (float)(_count - 1);
                float angle = (baseAngle - halfWidth + source.ArcWidthDegrees * t) * Mathf.Deg2Rad;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _positions[i] = origin + _directions[i] * _travelled;
                _inFlight[i] = true;
                _weights[i] = 1f;
                _lifeLeft[i] = settings.DotFlightLifetimeSeconds;
            }

            _dots = new SignalDotMesh(gameObject, dotMaterial, _count);
            ApplyBaseLook(studied, exhausted);
            _dots.SetAltLook(settings.TrippedColor, settings.TrippedDotEmission);
            _dots.SetFade(1f);
            _dots.Rebuild(_positions, _weights, _trippedBlends, _count, settings.ForeignDotSize);
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

            if (_inFlightCount > 0)
            {
                _travelled += _speed * Time.deltaTime;
                AdvanceDots();
            }

            FollowLandedBodies();

            if (!AgeDots())
            {
                Destroy(gameObject);
                return;
            }

            _dots.Rebuild(_positions, _weights, _trippedBlends, _count, _settings.ForeignDotSize);
        }

        private bool AgeDots()
        {
            bool anyAlive = false;

            for (int i = 0; i < _count; i++)
            {
                if (_lifeLeft[i] <= 0f)
                    continue;

                if (ViewBounds.IsOutside(_camera, _positions[i], _settings.OffscreenMargin))
                {
                    GroundDot(i);
                    StartFadeOut(i);
                }

                _lifeLeft[i] -= Time.deltaTime;
                _weights[i] = _settings.FadeForRemaining(_lifeLeft[i]);

                if (_lifeLeft[i] > 0f)
                    anyAlive = true;
            }

            return anyAlive;
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
                    _lifeLeft[i] = _settings.DotLifetimeSeconds;
                    _landedOffsets[i] = LandedOffsetFor(i, hit, hitRadius);
                    _positions[i] = (Vector2)hit.position + _landedOffsets[i];

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
