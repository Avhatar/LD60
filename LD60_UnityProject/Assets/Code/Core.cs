using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60
{
    [DefaultExecutionOrder(-1000)]
    public class Core : MonoBehaviour
    {
        [SerializeField] private CoreSO _config;

        public static CoreSO Config { get; private set; }
        public static bool IsPaused { get; private set; }

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
            IsPaused = false;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!PauseToggleRequested())
                return;

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
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
