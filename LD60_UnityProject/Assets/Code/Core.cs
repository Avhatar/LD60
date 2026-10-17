using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60
{
    [DefaultExecutionOrder(-1000)]
    public class Core : MonoBehaviour
    {
        [SerializeField] private CoreSO _config;

        public static CoreSO Config { get; private set; }

        private static bool _manualPause;
        private static bool _heldPause;

        public static bool IsPaused
        {
            get { return _manualPause || _heldPause; }
        }

        public static void SetPauseHold(bool held)
        {
            _heldPause = held;
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(_config)} is null in {nameof(Awake)}. " +
                               "Required as the root configuration every system reads through Core.Config. " +
                               "Fix: assign the CoreSO asset to the Config slot on the Core object in the scene.");
                enabled = false;
                return;
            }

            Config = _config;
            _manualPause = false;
            _heldPause = false;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (LanguageToggleRequested())
                Localization.Toggle();

            if (!PauseToggleRequested())
                return;

            _manualPause = !_manualPause;
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying || _config == null)
                return;

            Config = _config;
            GameDataSO.NotifyChanged();
        }
#endif

        private static bool LanguageToggleRequested()
        {
            return Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
        }

        private static bool PauseToggleRequested()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;

            if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
                return true;

            return false;
        }
    }
}
