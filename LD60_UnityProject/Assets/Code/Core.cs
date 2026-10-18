using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Settings;

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
        private static bool _introPause;

        public static bool IsPaused
        {
            get { return _manualPause || _heldPause || _dialoguePause || _introPause; }
        }

        public static bool IsGameMode
        {
            get { return Mode == GameMode.Game; }
        }

        public static bool IsDialogueMode
        {
            get { return Mode == GameMode.Dialogue; }
        }

        public static bool IsIntroMode
        {
            get { return Mode == GameMode.Intro; }
        }

        public static double DaysSinceJ2000 { get; private set; }

        public static double DeltaDays { get; private set; }

        public static int ActIndex { get; private set; }

        public static DateTime CurrentDate
        {
            get { return GameDate.FromDaysSinceJ2000(DaysSinceJ2000); }
        }

        public static ActPeriod CurrentAct
        {
            get
            {
                if (Config == null || Config.TimeScale == null)
                    return null;

                return Config.TimeScale.GetAct(ActIndex);
            }
        }

        public static float ActProgress
        {
            get
            {
                ActPeriod act = CurrentAct;

                if (act == null || act.SpanDays == 0d)
                    return 0f;

                return Mathf.Clamp01((float)((DaysSinceJ2000 - act.StartDaysSinceJ2000) / act.SpanDays));
            }
        }

        public static void SetAct(int index)
        {
            if (Config == null || Config.TimeScale == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(CoreSO.TimeScale)} is null in {nameof(SetAct)}. " +
                               $"Required to read the start and end dates of act {index}. " +
                               "Fix: assign the TimeScale asset to the Time Scale slot on the Core asset.");
                return;
            }

            ActPeriod act = Config.TimeScale.GetAct(index);

            if (act == null)
            {
                Debug.LogError($"{nameof(Core)}: act {index} is missing in {nameof(SetAct)}. " +
                               "Required to know when the run starts and when it ends, so the date on screen can run. " +
                               "Fix: add the act to the Acts list on the TimeScale asset, " +
                               "or point Start Act Index at an act that exists.");
                return;
            }

            ActIndex = index;
            DaysSinceJ2000 = act.StartDaysSinceJ2000;
            DeltaDays = 0d;
        }

        public static void SetMode(GameMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            _dialoguePause = mode == GameMode.Dialogue;
            _introPause = mode == GameMode.Intro;
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
            _introPause = false;
            Time.timeScale = 1f;

            ActIndex = 0;
            DaysSinceJ2000 = 0d;
            DeltaDays = 0d;

            if (_config.TimeScale == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(CoreSO.TimeScale)} is null in {nameof(Awake)}. " +
                               "Required to know which act the run starts in and how fast the date advances. " +
                               "Fix: assign the TimeScale asset to the Time Scale slot on the Core asset.");
                return;
            }

            SetAct(_config.TimeScale.StartActIndex);
        }

        private void Update()
        {
            AdvanceClock();

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

        private static void AdvanceClock()
        {
            DeltaDays = 0d;

            ActPeriod act = CurrentAct;

            if (act == null)
                return;

            double step = Time.deltaTime * act.DaysPerSecond;

            if (step == 0d)
                return;

            double end = act.EndDaysSinceJ2000;
            double next = DaysSinceJ2000 + step;

            if (step > 0d && next > end)
                next = end;
            else if (step < 0d && next < end)
                next = end;

            DeltaDays = next - DaysSinceJ2000;
            DaysSinceJ2000 = next;
        }

        private static bool LanguageToggleRequested()
        {
            return Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame;
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
