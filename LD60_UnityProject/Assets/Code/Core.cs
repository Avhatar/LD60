using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60
{
    [DefaultExecutionOrder(-1000)]
    public class Core : MonoBehaviour
    {
        [SerializeField] private CoreSO _config;

        public static CoreSO Config { get; private set; }

        public static GameMode Mode { get; private set; }

        public static event Action<GameMode> ModeChanged;

        private static bool _manualPause;
        private static bool _heldPause;
        private static bool _dialoguePause;

        public static bool IsPaused
        {
            get { return _manualPause || _heldPause || _dialoguePause; }
        }

        public static bool IsGameMode
        {
            get { return Mode == GameMode.Game; }
        }

        public static bool IsDialogueMode
        {
            get { return Mode == GameMode.Dialogue; }
        }

        public static void SetMode(GameMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            _dialoguePause = mode == GameMode.Dialogue;
            ApplyTimeScale();

            if (ModeChanged != null)
                ModeChanged.Invoke(mode);
        }

        public static void SetPauseHold(bool held)
        {
            _heldPause = held;
            ApplyTimeScale();
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
            Mode = GameMode.Game;
            _manualPause = false;
            _heldPause = false;
            _dialoguePause = false;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (LanguageToggleRequested())
                Localization.Toggle();

            if (!IsGameMode)
                return;

            if (!PauseToggleRequested())
                return;

            _manualPause = !_manualPause;
            ApplyTimeScale();
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

        private static void ApplyTimeScale()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }

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
