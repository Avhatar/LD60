using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using LD60.Settings;
using LD60.Signals;

namespace LD60.UI
{
    public class SignalAimReadout : MonoBehaviour
    {
        [SerializeField] private SignalLauncher _launcher;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Vector2 _pointerOffset = new Vector2(18f, -18f);

        private UiTextSO _text;

        private void Start()
        {
            if (_launcher == null)
            {
                Debug.LogError($"{nameof(SignalAimReadout)}: {nameof(_launcher)} is null in {nameof(Start)}. " +
                               "Required to read the current aim width, speed and cost. " +
                               "Fix: assign the Signal Launcher object to the Launcher slot on this object.");
                enabled = false;
                return;
            }

            if (_label == null)
            {
                Debug.LogError($"{nameof(SignalAimReadout)}: {nameof(_label)} is null in {nameof(Start)}. " +
                               "Required to draw the readout text. " +
                               "Fix: assign a TextMeshPro text object to the Label slot on this object.");
                enabled = false;
                return;
            }

            _text = Core.Config == null ? null : Core.Config.UiText;

            if (_text == null)
            {
                Debug.LogError($"{nameof(SignalAimReadout)}: {nameof(_text)} is null in {nameof(Start)}. " +
                               "Required to read the localized readout lines. " +
                               "Fix: assign the UiText asset to the Ui Text slot on the Core asset.");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!Core.IsGameMode)
            {
                if (_label.enabled)
                    _label.enabled = false;

                return;
            }

            if (!_label.enabled)
                _label.enabled = true;

            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            _label.rectTransform.position = (Vector3)(mouse.position.ReadValue() + _pointerOffset);
            _label.text = BuildText();
        }

        private string BuildText()
        {
            if (!_launcher.IsReady)
                return _text.NoCarrier.Value;

            string pause = Core.IsPaused ? "\n" + _text.Paused.Value : string.Empty;

            if (!_launcher.IsAiming)
            {
                string hit = _launcher.LastHitName == null || _launcher.LastHitName.IsEmpty
                    ? string.Empty
                    : "\n" + _text.HitPrefix.Value + _launcher.LastHitName.Value;

                return _text.AimHint.Value + pause + hit;
            }

            return string.Format(_text.AimReadoutFormat.Value,
                _launcher.WidthDegrees,
                _launcher.Speed,
                _launcher.CurrentCost,
                _launcher.PowerDots) + pause;
        }
    }
}
