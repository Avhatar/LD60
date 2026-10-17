using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Space;

namespace LD60.Signals
{
    public class SignalLauncher : MonoBehaviour
    {
        [SerializeField] private Orrery _orrery;
        [SerializeField] private Material _lineMaterial;

        private SignalSO _settings;
        private Camera _camera;
        private LineRenderer _preview;
        private Transform _origin;

        public float WidthDegrees { get; private set; }
        public float SpeedMultiplier { get; private set; }
        public string LastHitName { get; private set; }

        public float CurrentCost
        {
            get { return _settings == null ? 0f : _settings.CostFor(WidthDegrees, SpeedMultiplier); }
        }

        public bool IsReady
        {
            get { return _settings != null && _origin != null; }
        }

        private void Start()
        {
            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(Start)}. " +
                               "Required to read the signal settings. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.");
                enabled = false;
                return;
            }

            _settings = Core.Config.Signal;

            if (_settings == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(SignalSO)} is null in {nameof(Start)}. " +
                               "Required to know base speed, width limits and costs. " +
                               "Fix: assign a Signal asset to the Signal slot on the CoreSO asset.");
                enabled = false;
                return;
            }

            if (_orrery == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(_orrery)} is null in {nameof(Start)}. " +
                               "Required to find the player spacecraft and to test what the signal passes through. " +
                               "Fix: assign the Orrery object to the Orrery slot on the SignalLauncher object.");
                enabled = false;
                return;
            }

            _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(Camera)}.{nameof(Camera.main)} is null in {nameof(Start)}. " +
                               "Required to turn the pointer position into an aim direction. " +
                               "Fix: tag the scene camera as MainCamera.");
                enabled = false;
                return;
            }

            WidthDegrees = _settings.ClampWidth(_settings.DefaultWidthDegrees);
            SpeedMultiplier = _settings.ClampSpeedMultiplier(_settings.DefaultSpeedMultiplier);

            _origin = _orrery.PlayerAnchor;

            if (_origin == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: player anchor is null in {nameof(Start)}. " +
                               "Required as the point every signal is transmitted from. " +
                               "Fix: add a spacecraft asset with Is Player enabled to the Free Flyers list on the SolarSystem asset.");
                enabled = false;
                return;
            }

            CreatePreview();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            ReadWidth(mouse);
            ReadSpeed(mouse);

            Vector2 origin = _origin.position;
            Vector2 aim = PointerWorldPosition(mouse) - origin;

            if (aim.sqrMagnitude < 0.0001f)
                return;

            Vector2 direction = aim.normalized;
            SignalWedge.BuildFilledWedge(_preview, origin, direction, WidthDegrees * 0.5f,
                _settings.MaxTravelRadius, _settings.ArcSegments);

            if (mouse.leftButton.wasPressedThisFrame)
                Launch(origin, direction);
        }

        private void ReadWidth(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;

            if (Mathf.Approximately(scroll, 0f))
                return;

            WidthDegrees = _settings.ClampWidth(WidthDegrees + Mathf.Sign(scroll) * _settings.WidthScrollStepDegrees);
        }

        private void ReadSpeed(Mouse mouse)
        {
            if (!mouse.rightButton.isPressed)
                return;

            float drag = mouse.delta.ReadValue().y;
            SpeedMultiplier = _settings.ClampSpeedMultiplier(SpeedMultiplier + drag * _settings.SpeedDragSensitivity);
        }

        private Vector2 PointerWorldPosition(Mouse mouse)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        private void Launch(Vector2 origin, Vector2 direction)
        {
            GameObject spawned = new GameObject("Signal");
            spawned.transform.SetParent(transform, false);

            Signal signal = spawned.AddComponent<Signal>();
            signal.Launch(_settings, _orrery, this, _lineMaterial, origin, direction, WidthDegrees, SpeedMultiplier);
        }

        public void ReportHit(string bodyName)
        {
            LastHitName = bodyName;
        }

        private void CreatePreview()
        {
            GameObject preview = new GameObject("Aim Preview");
            preview.transform.SetParent(transform, false);

            _preview = preview.AddComponent<LineRenderer>();
            _preview.useWorldSpace = true;
            _preview.widthMultiplier = _settings.PreviewLineWidth;
            _preview.startColor = _settings.PreviewColor;
            _preview.endColor = _settings.PreviewColor;
            _preview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _preview.receiveShadows = false;

            if (_lineMaterial != null)
            {
                _preview.sharedMaterial = _lineMaterial;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(Shader.PropertyToID("_BaseColor"), _settings.PreviewColor);
                _preview.SetPropertyBlock(block);
            }
        }
    }
}
