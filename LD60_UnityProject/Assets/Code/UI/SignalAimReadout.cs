using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using LD60.Signals;

namespace LD60.UI
{
    public class SignalAimReadout : MonoBehaviour
    {
        [SerializeField] private SignalLauncher _launcher;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Vector2 _pointerOffset = new Vector2(18f, -18f);

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
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            _label.rectTransform.position = (Vector3)(mouse.position.ReadValue() + _pointerOffset);

            if (!_launcher.IsReady)
            {
                _label.text = "NO CARRIER";
                return;
            }

            _label.text = string.Format("{0:0.0}°  ×{1:0.00}  {2:0.0}W{3}{4}",
                _launcher.WidthDegrees,
                _launcher.SpeedMultiplier,
                _launcher.CurrentCost,
                Core.IsPaused ? "\nPAUSED" : string.Empty,
                string.IsNullOrEmpty(_launcher.LastHitName) ? string.Empty : "\nHIT: " + _launcher.LastHitName);
        }
    }
}
