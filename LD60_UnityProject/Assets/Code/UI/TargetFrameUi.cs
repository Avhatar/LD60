using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Dialogue;
using LD60.Settings;
using LD60.Space;

namespace LD60.UI
{
    public class TargetFrameUi : MonoBehaviour
    {
        public CelestialBodySO HoveredBody { get; private set; }

        [SerializeField] private Orrery _orrery;
        [SerializeField] private TargetFrame _playerFrame;
        [SerializeField] private TargetFrame _hoverFrame;

        private Camera _camera;
        private TargetFrameSO _settings;
        private UiTextSO _text;
        private bool _dataDirty;

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
            if (_orrery == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(_orrery)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to find our own spacecraft and the body under the pointer. " +
                               "Fix: assign the Orrery object to the Orrery slot on this object.", this);
                enabled = false;
                return;
            }

            if (_playerFrame == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(_playerFrame)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to draw the frame that marks our own spacecraft. " +
                               "Fix: assign the Player Frame object to the Player Frame slot on this object.", this);
                enabled = false;
                return;
            }

            if (_hoverFrame == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(_hoverFrame)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to draw the frame that follows the pointer onto a body. " +
                               "Fix: assign the Hover Frame object to the Hover Frame slot on this object.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(Camera)}.{nameof(Camera.main)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to project world positions onto the HUD and to read what the pointer is over. " +
                               "Fix: tag the scene camera as MainCamera.", this);
                enabled = false;
                return;
            }

            SyncData();
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to read the frame settings and the label text. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.", this);
                enabled = false;
                return;
            }

            _settings = Core.Config.TargetFrame;

            if (_settings == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(TargetFrameSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required for the frame colours, padding and animation. " +
                               "Fix: assign the TargetFrame asset to the Target Frame slot on the Core asset.", this);
                enabled = false;
                return;
            }

            _text = Core.Config.UiText;

            if (_text == null)
            {
                Debug.LogError($"{nameof(TargetFrameUi)}: {nameof(UiTextSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to read the localized label drawn over our own spacecraft. " +
                               "Fix: assign the UiText asset to the Ui Text slot on the Core asset.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_dataDirty)
                SyncData();

            if (!enabled)
                return;

            if (!Core.IsGameMode)
            {
                _playerFrame.Release();
                _hoverFrame.Release();
                return;
            }

            TrackPlayer();
            TrackHovered();
        }

        private void TrackPlayer()
        {
            Transform anchor = _orrery.PlayerAnchor;
            SpacecraftSO player = _orrery.PlayerSpacecraft;

            if (anchor == null || player == null)
            {
                _playerFrame.Release();
                return;
            }

            _playerFrame.Track(_camera, anchor.position, player.DisplayDiameter * 0.5f,
                _text.SelfFrameLabel.Value, _settings.PlayerColor);
        }

        private void TrackHovered()
        {
            Mouse mouse = Mouse.current;

            HoveredBody = null;

            if (mouse == null)
            {
                _hoverFrame.Release();
                return;
            }

            Vector2 pointer = PointerWorldPosition(mouse);
            int hovered = -1;
            float hoveredRadius = float.MaxValue;

            for (int i = 0; i < _orrery.BodyCount; i++)
            {
                if (!_orrery.IsHitAt(i, pointer))
                    continue;

                float radius = _orrery.GetHitRadius(i);

                if (radius >= hoveredRadius)
                    continue;

                hoveredRadius = radius;
                hovered = i;
            }

            if (hovered < 0)
            {
                _hoverFrame.Release();
                return;
            }

            Transform body = _orrery.GetBodyTransform(hovered);
            CelestialBodySO data = _orrery.GetBody(hovered);

            if (body == null || data == null)
            {
                _hoverFrame.Release();
                return;
            }

            HoveredBody = data;
            _hoverFrame.Track(_camera, body.position, hoveredRadius, data.LocalizedName.Value, _settings.HoverColor);
        }

        private Vector2 PointerWorldPosition(Mouse mouse)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }
    }
}
