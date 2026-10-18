using UnityEngine;
using TMPro;
using LD60.Settings;
using LD60.Signals;
using LD60.Space;

namespace LD60.UI
{
    public class CooldownReadout : MonoBehaviour
    {
        [SerializeField] private SignalLauncher _launcher;
        [SerializeField] private Orrery _orrery;
        [SerializeField] private RectTransform _root;

        private TargetFrameSO _settings;
        private Camera _camera;
        private TMP_Text _label;
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
            if (_launcher == null)
            {
                Debug.LogError($"{nameof(CooldownReadout)}: {nameof(_launcher)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to read how much cooldown is left. " +
                               "Fix: assign the Signal Launcher object to the Launcher slot on this object.", this);
                enabled = false;
                return;
            }

            if (_orrery == null || _root == null)
            {
                Debug.LogError($"{nameof(CooldownReadout)}: {nameof(_orrery)} or {nameof(_root)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to place the counter under our own spacecraft. " +
                               "Fix: assign the Orrery object and a Canvas RectTransform on this object.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError($"{nameof(CooldownReadout)}: {nameof(Camera)}.{nameof(Camera.main)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to turn our spacecraft position into a screen position. " +
                               "Fix: tag the scene camera as MainCamera.", this);
                enabled = false;
                return;
            }

            CreateLabel();
            SyncData();
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null || Core.Config.TargetFrame == null)
            {
                Debug.LogError($"{nameof(CooldownReadout)}: {nameof(TargetFrameSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to know the counter font, size, colour and offset. " +
                               "Fix: assign a TargetFrame asset to the Target Frame slot on the CoreSO asset.", this);
                return;
            }

            _settings = Core.Config.TargetFrame;
            ApplyLook();
        }

        private void LateUpdate()
        {
            if (_dataDirty)
                SyncData();

            if (_settings == null || _label == null)
                return;

            Transform anchor = _orrery.PlayerAnchor;

            if (!Core.IsGameMode || !_launcher.IsCoolingDown || anchor == null)
            {
                if (_label.enabled)
                    _label.enabled = false;

                return;
            }

            if (!_label.enabled)
                _label.enabled = true;

            _label.text = (Mathf.Ceil(_launcher.CooldownLeft * 10f) * 0.1f).ToString("F1");
            _label.rectTransform.position = (Vector3)((Vector2)_camera.WorldToScreenPoint(anchor.position)
                                           - new Vector2(0f, _settings.CooldownOffset));
        }

        private void CreateLabel()
        {
            GameObject label = new GameObject("Cooldown");
            label.transform.SetParent(_root, false);

            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(120f, 28f);
            text.enabled = false;
            _label = text;
        }

        private void ApplyLook()
        {
            if (_label == null)
                return;

            _label.font = _settings.CooldownFont;
            _label.fontSize = _settings.CooldownFontSize;
            _label.color = _settings.CooldownColor;
        }
    }
}
