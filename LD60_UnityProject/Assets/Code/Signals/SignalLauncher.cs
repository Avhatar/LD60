using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Space;

namespace LD60.Signals
{
    public class SignalLauncher : MonoBehaviour
    {
        [SerializeField] private Orrery _orrery;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _dotMaterial;

        private SignalSO _settings;
        private Camera _camera;
        private LineRenderer _preview;
        private GameObject _previewDotsObject;
        private SignalDotMesh _previewDots;
        private Vector2[] _previewPositions;
        private bool[] _previewAlive;
        private MaterialPropertyBlock _propertyBlock;
        private Transform _origin;
        private Vector2 _aimDirection = Vector2.right;
        private float _chargeSeconds;
        private bool _awaitingReleaseOfEntryClick;
        private bool _dataDirty;
        private Signal _lastSignal;

        public float WidthDegrees { get; private set; }
        public float Speed { get; private set; }
        public int PowerDots { get; private set; }
        public string LastHitName { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsCharging { get; private set; }

        public float CurrentCost
        {
            get { return _settings == null ? 0f : _settings.CostFor(WidthDegrees, Speed, PowerDots); }
        }

        public bool IsReady
        {
            get { return _settings != null && _origin != null; }
        }

        private void OnEnable()
        {
            GameDataSO.Changed += OnDataChanged;
        }

        private void OnDisable()
        {
            GameDataSO.Changed -= OnDataChanged;
        }

        private void OnDataChanged()
        {
            _dataDirty = true;
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
                               "Required to know speed mapping, width limits, dot counts and costs. " +
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
            Speed = _settings.MinSpeed;
            PowerDots = _settings.MinDots;

            CreatePreview();

            _origin = _orrery.PlayerAnchor;

            if (_origin == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: player anchor is null in {nameof(Start)}. " +
                               "Required as the point every signal is transmitted from. " +
                               "Fix: add a spacecraft asset with Is Player enabled to the Free Flyers list on the SolarSystem asset.");
            }
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(SyncData)}. " +
                               "Required to re-read the signal settings after a data edit. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.");
                return;
            }

            if (Core.Config.Signal == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(SignalSO)} is null in {nameof(SyncData)}. " +
                               "Required to know speed mapping, width limits, dot counts and costs. " +
                               "Fix: assign a Signal asset to the Signal slot on the CoreSO asset.");
                return;
            }

            _settings = Core.Config.Signal;
            WidthDegrees = _settings.ClampWidth(WidthDegrees);
            ApplyPreviewLook();
        }

        private void Update()
        {
            if (_dataDirty)
                SyncData();

            if (_origin == null)
                _origin = _orrery.PlayerAnchor;

            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            if (_origin == null)
            {
                if (IsAiming)
                    EndAiming();

                return;
            }

            if (!IsAiming)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    BeginAiming();

                return;
            }

            ReadWidth(mouse);

            Vector2 origin = _origin.position;
            Vector2 aim = PointerWorldPosition(mouse) - origin;

            if (aim.sqrMagnitude > 0.0001f)
                _aimDirection = aim.normalized;

            Speed = _settings.SpeedForPointerDistance(aim.magnitude);

            if (mouse.rightButton.wasPressedThisFrame)
            {
                EndAiming();
                return;
            }

            ReadCharge(mouse);
            DrawPreview(origin);

            if (IsCharging && mouse.leftButton.wasReleasedThisFrame)
            {
                Launch(origin, _aimDirection);
                EndAiming();
            }
        }

        private void ReadCharge(Mouse mouse)
        {
            if (_awaitingReleaseOfEntryClick)
            {
                if (mouse.leftButton.wasReleasedThisFrame)
                    _awaitingReleaseOfEntryClick = false;

                PowerDots = _settings.MinDots;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                IsCharging = true;
                _chargeSeconds = 0f;
            }

            if (!IsCharging)
            {
                PowerDots = _settings.MinDots;
                return;
            }

            _chargeSeconds += Time.unscaledDeltaTime;
            PowerDots = _settings.DotsForHoldSeconds(_chargeSeconds);
        }

        private void BeginAiming()
        {
            IsAiming = true;
            IsCharging = false;
            _chargeSeconds = 0f;
            _awaitingReleaseOfEntryClick = true;
            PowerDots = _settings.MinDots;
            _preview.enabled = true;
            _previewDotsObject.SetActive(true);

            if (_settings.PauseWhileAiming)
                Core.SetPauseHold(true);
        }

        private void EndAiming()
        {
            IsAiming = false;
            IsCharging = false;
            _chargeSeconds = 0f;
            _preview.enabled = false;
            _previewDotsObject.SetActive(false);

            if (_settings.PauseWhileAiming)
                Core.SetPauseHold(false);
        }

        private void ReadWidth(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;

            if (Mathf.Approximately(scroll, 0f))
                return;

            WidthDegrees = _settings.ClampWidth(WidthDegrees + Mathf.Sign(scroll) * _settings.WidthScrollStepDegrees);
        }

        private void DrawPreview(Vector2 origin)
        {
            SignalWedge.BuildFilledWedge(_preview, origin, _aimDirection, WidthDegrees * 0.5f,
                _settings.MaxTravelRadius, _settings.ArcSegments);

            int dots = Mathf.Clamp(PowerDots, 1, _previewPositions.Length);
            float baseAngle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
            float halfWidth = WidthDegrees * 0.5f;

            for (int i = 0; i < dots; i++)
            {
                float t = dots == 1 ? 0.5f : i / (float)(dots - 1);
                float angle = (baseAngle - halfWidth + WidthDegrees * t) * Mathf.Deg2Rad;
                _previewPositions[i] = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _settings.PreviewArcRadius;
                _previewAlive[i] = true;
            }

            _previewDots.Rebuild(_previewPositions, _previewAlive, dots, _settings.DotSize);
        }

        private Vector2 PointerWorldPosition(Mouse mouse)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        private void Launch(Vector2 origin, Vector2 direction)
        {
            if (_lastSignal != null)
                Destroy(_lastSignal.gameObject);

            GameObject spawned = new GameObject("Signal");
            spawned.transform.SetParent(transform, false);

            _lastSignal = spawned.AddComponent<Signal>();
            _lastSignal.Launch(_settings, _orrery, this, _dotMaterial, origin, direction, WidthDegrees, Speed, PowerDots);
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
            _preview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _preview.receiveShadows = false;
            _preview.enabled = false;

            _propertyBlock = new MaterialPropertyBlock();
            ApplyPreviewLook();

            _previewDotsObject = new GameObject("Aim Dots");
            _previewDotsObject.transform.SetParent(transform, false);
            _previewDots = new SignalDotMesh(_previewDotsObject, _dotMaterial, _settings.SignalColor, _settings.MaxDots);
            _previewPositions = new Vector2[_settings.MaxDots];
            _previewAlive = new bool[_settings.MaxDots];
            _previewDotsObject.SetActive(false);
        }

        private void ApplyPreviewLook()
        {
            if (_preview == null)
                return;

            _preview.widthMultiplier = _settings.PreviewLineWidth;
            _preview.startColor = _settings.PreviewColor;
            _preview.endColor = _settings.PreviewColor;

            if (_lineMaterial == null)
                return;

            _preview.sharedMaterial = _lineMaterial;
            _propertyBlock.Clear();
            _propertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), _settings.PreviewColor);
            _preview.SetPropertyBlock(_propertyBlock);
        }
    }
}
