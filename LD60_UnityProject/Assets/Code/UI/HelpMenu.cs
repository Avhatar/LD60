using LD60.Settings;
using LD60.Sound;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LD60.UI
{
    public class HelpMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _menuRoot;
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _musicValue;
        [SerializeField] private Button _musicMinus;
        [SerializeField] private Button _musicPlus;
        [SerializeField] private TMP_Text _soundLabel;
        [SerializeField] private TMP_Text _soundValue;
        [SerializeField] private Button _soundMinus;
        [SerializeField] private Button _soundPlus;
        [SerializeField] private TMP_Text _helpText;

        private UiTextSO _text;

        private void Awake()
        {
            if (_menuRoot != null)
                _menuRoot.SetActive(false);

            bool wired = Wired(_menuRoot, nameof(_menuRoot), "to show and hide the menu on Q")
                         & Wired(_musicLabel, nameof(_musicLabel), "to draw the localized caption of the music row")
                         & Wired(_musicValue, nameof(_musicValue), "to draw the current music volume")
                         & Wired(_musicMinus, nameof(_musicMinus), "to lower the music volume")
                         & Wired(_musicPlus, nameof(_musicPlus), "to raise the music volume")
                         & Wired(_soundLabel, nameof(_soundLabel), "to draw the localized caption of the overall sound row")
                         & Wired(_soundValue, nameof(_soundValue), "to draw the current overall sound volume")
                         & Wired(_soundMinus, nameof(_soundMinus), "to lower the overall sound volume")
                         & Wired(_soundPlus, nameof(_soundPlus), "to raise the overall sound volume")
                         & Wired(_helpText, nameof(_helpText), "to draw the localized controls help");

            if (!wired)
            {
                enabled = false;
                return;
            }

            _musicMinus.onClick.AddListener(LowerMusic);
            _musicPlus.onClick.AddListener(RaiseMusic);
            _soundMinus.onClick.AddListener(LowerSound);
            _soundPlus.onClick.AddListener(RaiseSound);
        }

        private void Start()
        {
            _text = Core.Config == null ? null : Core.Config.UiText;

            if (_text == null)
            {
                Debug.LogError($"{nameof(HelpMenu)}: {nameof(UiTextSO)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to read the localized row captions and the controls help. " +
                               "Fix: assign the UiText asset to the Ui Text slot on the Core asset.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!ToggleRequested())
                return;

            if (Core.IsMenuMode)
                Close();
            else if (Core.IsGameMode)
                Open();
        }

        private void Open()
        {
            _menuRoot.SetActive(true);
            _musicLabel.text = _text.MusicVolumeLabel.Value;
            _soundLabel.text = _text.SoundVolumeLabel.Value;
            _helpText.text = _text.HelpText.Value;
            RefreshVolumes();
            Core.SetMode(GameMode.Menu);
        }

        private void Close()
        {
            _menuRoot.SetActive(false);
            Core.SetMode(GameMode.Game);
        }

        private void LowerMusic()
        {
            ChangeMusic(-1);
        }

        private void RaiseMusic()
        {
            ChangeMusic(1);
        }

        private void LowerSound()
        {
            ChangeSound(-1);
        }

        private void RaiseSound()
        {
            ChangeSound(1);
        }

        private void ChangeMusic(int direction)
        {
            AudioController audio = ResolveAudio(nameof(ChangeMusic));

            if (audio == null)
                return;

            audio.AdjustMusicVolume(direction);
            RefreshVolumes();
        }

        private void ChangeSound(int direction)
        {
            AudioController audio = ResolveAudio(nameof(ChangeSound));

            if (audio == null)
                return;

            audio.AdjustMasterVolume(direction);
            RefreshVolumes();
        }

        private void RefreshVolumes()
        {
            AudioController audio = ResolveAudio(nameof(RefreshVolumes));

            if (audio == null)
                return;

            string format = _text.VolumeValueFormat.Value;
            _musicValue.text = string.Format(format, audio.MusicVolumePercent);
            _soundValue.text = string.Format(format, audio.MasterVolumePercent);
        }

        private AudioController ResolveAudio(string callerName)
        {
            AudioController audio = AudioController.Instance;

            if (audio == null)
            {
                Debug.LogError($"{nameof(HelpMenu)}: {nameof(AudioController)}.{nameof(AudioController.Instance)} is null in {callerName} on '{name}'. " +
                               "Required to read and change the stored volumes. " +
                               "Fix: add an AudioController object to the scene and keep its execution order ahead of the UI.", this);
            }

            return audio;
        }

        private bool Wired(UnityEngine.Object reference, string fieldName, string purpose)
        {
            if (reference != null)
                return true;

            Debug.LogError($"{nameof(HelpMenu)}: {fieldName} is null in {nameof(Awake)} on '{name}'. " +
                           $"Required {purpose}. " +
                           "Fix: assign the matching child of HelpMenuRoot to that slot on this object.", this);
            return false;
        }

        private static bool ToggleRequested()
        {
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        }
    }
}
