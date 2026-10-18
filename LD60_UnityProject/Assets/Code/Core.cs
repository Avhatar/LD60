using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LD60.Dialogue;
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
        private static bool _menuPause;
        private static bool _vinilPause;

        private static readonly HashSet<SpacecraftSO> _contactedSpacecraft = new HashSet<SpacecraftSO>();

        public static bool IsPaused
        {
            get { return _manualPause || _heldPause || _dialoguePause || _introPause || _menuPause || _vinilPause; }
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

        public static bool IsMenuMode
        {
            get { return Mode == GameMode.Menu; }
        }

        public static bool IsVinilMode
        {
            get { return Mode == GameMode.Vinil; }
        }

        public static bool IsSpacecraftOnRecord(SpacecraftSO spacecraft)
        {
            if (spacecraft == null)
                return false;

            return spacecraft.IsPlayer || _contactedSpacecraft.Contains(spacecraft);
        }

        public static void RegisterSpacecraftContact(SpacecraftSO spacecraft)
        {
            if (spacecraft == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(spacecraft)} is null in {nameof(RegisterSpacecraftContact)}. " +
                               "Required to remember for the rest of the run which craft has already been talked to, " +
                               "because the record keeps its slots across acts while the act's sender list does not. " +
                               $"Fix: assign a Spacecraft asset to the Spacecraft slot of the {nameof(LD60.Space.ActSpacecraftSO)} that just answered.");
                return;
            }

            _contactedSpacecraft.Add(spacecraft);
        }

        public static double DaysSinceJ2000 { get; private set; }

        public static double DeltaDays { get; private set; }

        public static int ActIndex { get; private set; }

        public static int Score { get; private set; }

        public static event Action<int> ScoreChanged;

        public static void AddScore(int amount)
        {
            if (amount <= 0)
                return;

            Score += amount;

            if (ScoreChanged != null)
                ScoreChanged.Invoke(amount);
        }

        public static float Watts { get; private set; }

        public static event Action<float> WattsChanged;

        public static float WattsCapacity
        {
            get
            {
                if (Config == null || Config.Watt == null)
                    return 0f;

                return Config.Watt.MaxWatts;
            }
        }

        public static float WattsFraction
        {
            get
            {
                float capacity = WattsCapacity;

                if (capacity <= 0f)
                    return 0f;

                return Mathf.Clamp01(Watts / capacity);
            }
        }

        public static bool TrySpendWatts(float amount)
        {
            if (amount <= 0f)
                return true;

            if (Watts < amount)
                return false;

            Watts -= amount;

            if (WattsChanged != null)
                WattsChanged.Invoke(-amount);

            return true;
        }

        public static void AddWatts(float amount)
        {
            if (amount <= 0f)
                return;

            if (Config == null || Config.Watt == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(CoreSO.Watt)} is null in {nameof(AddWatts)}. " +
                               "Required to know the transmitter maximum the pool is clamped to. " +
                               "Fix: assign a Watt asset to the Watt slot on the Core asset.");
                return;
            }

            float granted = Mathf.Min(amount, Config.Watt.MaxWatts - Watts);

            if (granted <= 0f)
                return;

            Watts += granted;

            if (WattsChanged != null)
                WattsChanged.Invoke(granted);
        }

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
            AddWatts(act.BudgetWatts);
        }

        public static void SetMode(GameMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            _dialoguePause = mode == GameMode.Dialogue;
            _introPause = mode == GameMode.Intro;
            _menuPause = mode == GameMode.Menu;
            _vinilPause = mode == GameMode.Vinil;
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
            _menuPause = false;
            _vinilPause = false;
            _contactedSpacecraft.Clear();
            Time.timeScale = 1f;

            ActIndex = 0;
            DaysSinceJ2000 = 0d;
            DeltaDays = 0d;
            Watts = 0f;
            Score = 0;

            if (_config.Watt == null)
            {
                Debug.LogError($"{nameof(Core)}: {nameof(CoreSO.Watt)} is null in {nameof(Awake)}. " +
                               "Required to know how many watts the transmitter holds, what a dot costs and how the bar reads. " +
                               "Fix: assign a Watt asset to the Watt slot on the Core asset.");
            }

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

            if (IsMenuMode)
                return;

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
