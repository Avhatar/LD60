using UnityEngine;
using LD60.Space;

namespace LD60.Signals
{
    public class Signal : MonoBehaviour
    {
        private SignalSO _settings;
        private Orrery _orrery;
        private LineRenderer _line;
        private Vector2 _origin;
        private Vector2 _direction;
        private float _halfAngleDegrees;
        private float _speed;
        private float _travelled;
        private bool[] _alreadyHit;
        private SignalLauncher _launcher;

        public void Launch(SignalSO settings, Orrery orrery, SignalLauncher launcher, Material lineMaterial,
            Vector2 origin, Vector2 direction, float widthDegrees, float speedMultiplier)
        {
            _settings = settings;
            _orrery = orrery;
            _launcher = launcher;
            _origin = origin;
            _direction = direction.normalized;
            _halfAngleDegrees = widthDegrees * 0.5f;
            _speed = settings.BaseSpeed * speedMultiplier;
            _travelled = 0f;
            _alreadyHit = new bool[orrery.BodyCount];

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthMultiplier = settings.SignalLineWidth;
            _line.startColor = settings.SignalColor;
            _line.endColor = settings.SignalColor;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            if (lineMaterial != null)
            {
                _line.sharedMaterial = lineMaterial;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(Shader.PropertyToID("_BaseColor"), settings.SignalColor);
                _line.SetPropertyBlock(block);
            }
        }

        private void Update()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(Signal)}: {nameof(_settings)} is null in {nameof(Update)}. " +
                               "Required because the signal cannot advance without its speed and width settings. " +
                               $"Fix: spawn signals through {nameof(SignalLauncher)}, which calls {nameof(Launch)}.");
                enabled = false;
                return;
            }

            _travelled += _speed * Time.deltaTime;
            TestBodies();

            SignalWedge.BuildFrontArc(_line, _origin, _direction, _halfAngleDegrees, _travelled, _settings.ArcSegments);

            if (_travelled >= _settings.MaxTravelRadius)
                Destroy(gameObject);
        }

        private void TestBodies()
        {
            for (int i = 0; i < _alreadyHit.Length; i++)
            {
                if (_alreadyHit[i])
                    continue;

                Transform body = _orrery.GetBodyTransform(i);
                if (body == null)
                    continue;

                Vector2 toBody = (Vector2)body.position - _origin;
                float distance = toBody.magnitude;

                if (distance <= 0.0001f || distance > _travelled)
                    continue;

                float bodyRadius = _orrery.GetBody(i).DisplayDiameter * 0.5f;
                float tolerance = Mathf.Atan2(bodyRadius, distance) * Mathf.Rad2Deg;

                if (Vector2.Angle(_direction, toBody) > _halfAngleDegrees + tolerance)
                    continue;

                _alreadyHit[i] = true;
                _orrery.FlashBody(i, _settings.HitFlashColor, _settings.HitFlashSeconds);
                _launcher.ReportHit(_orrery.GetBody(i).DisplayName);
            }
        }
    }
}
