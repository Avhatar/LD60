using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Settings;
using LD60.Space;

namespace LD60.Signals
{
    public class SignalLauncher : MonoBehaviour
    {
        [SerializeField] private Orrery _orrery;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _dotMaterial;

        private SignalSO _settings;
        private WattSO _watts;
        private Camera _camera;
        private LineRenderer _preview;
        private GameObject _previewDotsObject;
        private SignalDotMesh _previewDots;
        private Vector2[] _previewPositions;
        private float[] _previewWeights;
        private float[] _previewAltBlends;
        private MaterialPropertyBlock _propertyBlock;
        private Transform _origin;
        private Vector2 _aimDirection = Vector2.right;
        private float _cooldownLeft;
        private float _cooldownTotal;
        private bool _dataDirty;
        private Signal[] _live = new Signal[8];
        private int _liveCount;

        public float WidthDegrees { get; private set; }
        public float Speed { get; private set; }
        public int PowerDots { get; private set; }
        public LocalizedString LastHitName { get; private set; }
        public bool IsAiming { get; private set; }
        public float CooldownLeft
        {
            get { return _cooldownLeft; }
        }

        public float CooldownTotal
        {
            get { return _cooldownTotal; }
        }

        public bool IsCoolingDown
        {
            get { return _cooldownLeft > 0f; }
        }

        public float CurrentCost
        {
            get { return _watts == null ? 0f : PowerDots * _watts.WattsPerDot; }
        }

        public bool HasPowerForShot
        {
            get { return Core.Watts >= CurrentCost; }
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

            _watts = Core.Config.Watt;

            if (_watts == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(WattSO)} is null in {nameof(Start)}. " +
                               "Required to know what one dot of a volley costs, so the shot can be charged for. " +
                               "Fix: assign a Watt asset to the Watt slot on the CoreSO asset.");
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
            ApplyWidth(_settings.ClampWidth(_settings.DefaultWidthDegrees));

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

            if (Core.Config.Watt == null)
            {
                Debug.LogError($"{nameof(SignalLauncher)}: {nameof(WattSO)} is null in {nameof(SyncData)}. " +
                               "Required to re-read what one dot of a volley costs after a data edit. " +
                               "Fix: assign a Watt asset to the Watt slot on the CoreSO asset.");
                return;
            }

            _settings = Core.Config.Signal;
            _watts = Core.Config.Watt;
            ApplyWidth(_settings.ClampWidth(WidthDegrees));
            ApplyPreviewLook();
        }

        private void Update()
        {
            if (_dataDirty)
                SyncData();

            PruneLiveSignals();

            if (!Core.IsGameMode)
            {
                if (IsAiming)
                    EndAiming();

                return;
            }

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

            if (_cooldownLeft > 0f)
            {
                _cooldownLeft -= Time.deltaTime;

                if (_cooldownLeft <= 0f)
                    _cooldownLeft = 0f;
            }

            if (!IsAiming)
            {
                if (mouse.leftButton.wasPressedThisFrame && !IsCoolingDown)
                    BeginAiming();

                return;
            }

            ReadWidth(mouse);

            Vector2 origin = _origin.position;
            Vector2 aim = PointerWorldPosition(mouse) - origin;

            if (aim.sqrMagnitude > 0.0001f)
                _aimDirection = aim.normalized;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                EndAiming();
                return;
            }

            DrawPreview(origin);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!Core.TrySpendWatts(CurrentCost))
                    return;

                Launch(origin, _aimDirection);
                StartCooldown();
                EndAiming();
            }
        }

        private void ApplyWidth(float widthDegrees)
        {
            WidthDegrees = widthDegrees;
            PowerDots = _settings.DotsForWidth(WidthDegrees);
            Speed = _settings.SpeedForWidth(WidthDegrees);
        }

        private void StartCooldown()
        {
            _cooldownTotal = _settings.CooldownForDots(PowerDots);
            _cooldownLeft = _cooldownTotal;
        }

        private void BeginAiming()
        {
            IsAiming = true;
            _preview.enabled = true;
            _previewDotsObject.SetActive(true);

            if (_settings.PauseWhileAiming)
                Core.SetPauseHold(true);
        }

        private void EndAiming()
        {
            IsAiming = false;
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

            ApplyWidth(_settings.ClampWidth(WidthDegrees + Mathf.Sign(scroll) * _settings.WidthScrollStepDegrees));
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
                _previewWeights[i] = 1f;
            }

            _previewDots.Rebuild(_previewPositions, _previewWeights, _previewAltBlends, dots, _settings.DotSize);
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
            signal.Launch(_settings, _orrery, this, _dotMaterial, origin, direction, WidthDegrees, Speed, PowerDots, _origin);
            AddLiveSignal(signal);

            PlaySound(_settings.OursSound);
        }

        private void AddLiveSignal(Signal signal)
        {
            if (_liveCount == _live.Length)
            {
                Signal[] grown = new Signal[_live.Length * 2];

                for (int i = 0; i < _liveCount; i++)
                    grown[i] = _live[i];

                _live = grown;
            }

            _live[_liveCount] = signal;
            _liveCount++;
        }

        private void PruneLiveSignals()
        {
            int kept = 0;

            for (int i = 0; i < _liveCount; i++)
            {
                if (_live[i] == null)
                    continue;

                _live[kept] = _live[i];
                kept++;
            }

            for (int i = kept; i < _liveCount; i++)
                _live[i] = null;

            _liveCount = kept;
        }

        private static void PlaySound(SoundContainerSO sound)
        {
            if (sound == null || LD60.Sound.AudioController.Instance == null)
                return;

            LD60.Sound.AudioController.Instance.Play2D(sound);
        }

        public int LiveSignalCount
        {
            get { return _liveCount; }
        }

        public Signal LiveSignal(int index)
        {
            return _live[index];
        }

        public void ReportHit(LocalizedString bodyName)
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
            _previewDots = new SignalDotMesh(_previewDotsObject, _dotMaterial, _settings.MaxDots);
            _previewDots.SetLook(_settings.SignalColor, _settings.PreviewDotEmission, _settings.DotEdgeSoftness);
            _previewDots.SetFade(1f);
            _previewPositions = new Vector2[_settings.MaxDots];
            _previewWeights = new float[_settings.MaxDots];
            _previewAltBlends = new float[_settings.MaxDots];
            _previewDotsObject.SetActive(false);
        }

        private void ApplyPreviewLook()
        {
            if (_preview == null)
                return;

            _preview.widthMultiplier = _settings.PreviewLineWidth;
            _preview.startColor = _settings.PreviewColor;
            _preview.endColor = _settings.PreviewColor;

            if (_previewDots != null)
                _previewDots.SetLook(_settings.SignalColor, _settings.PreviewDotEmission, _settings.DotEdgeSoftness);

            if (_lineMaterial == null)
                return;

            _preview.sharedMaterial = _lineMaterial;
            _propertyBlock.Clear();
            _propertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), _settings.PreviewColor);
            _preview.SetPropertyBlock(_propertyBlock);
        }
    }
}
