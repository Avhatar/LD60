using LD60;
using TMPro;
using UnityEngine;

public class UiRoot : MonoBehaviour
{
    [SerializeField]
    private DialogueUi dialogueUi;

    [SerializeField]
    private DateTimeUi dateTimeUi;

    [SerializeField]
    private TMP_Text pauseText;

    [SerializeField]
    private GameObject changeLanguageText;

    private bool _pauseShown;
    private bool _languageHintDone;
    private float _languageHintLeft = -1f;

    public DialogueUi DialogueUi
    {
        get { return dialogueUi; }
    }

    private void Awake()
    {
        if (dialogueUi == null)
        {
            Debug.LogError($"{nameof(UiRoot)}: {nameof(dialogueUi)} is null in {nameof(Awake)} on '{name}'. " +
                           "Required so whatever establishes contact can have the conversation shown. " +
                           "Fix: assign the DialogueUi object to the Dialogue Ui slot on this object.", this);
        }

        if (pauseText == null)
        {
            Debug.LogError($"{nameof(UiRoot)}: {nameof(pauseText)} is null in {nameof(Awake)} on '{name}'. " +
                           "Required to tell the player the clock is frozen while the orrery stands still. " +
                           "Fix: assign the PauseText object to the Pause Text slot on this object.", this);
            return;
        }

        _pauseShown = PauseTextWanted();
        pauseText.enabled = _pauseShown;
    }

    private void Start()
    {
        if (changeLanguageText == null)
        {
            Debug.LogError($"{nameof(UiRoot)}: {nameof(changeLanguageText)} is null in {nameof(Start)} on '{name}'. " +
                           "Required to tell the player once, at the very start, which key switches the language. " +
                           "Fix: assign the ChangeLanguageText object to the Change Language Text slot on this object.", this);
            _languageHintDone = true;
            return;
        }

        _languageHintLeft = LanguageHintSeconds;

        if (_languageHintLeft <= 0f)
        {
            _languageHintDone = true;
            changeLanguageText.SetActive(false);
            return;
        }

        changeLanguageText.SetActive(true);
    }

    private void Update()
    {
        UpdateLanguageHint();

        if (pauseText == null)
            return;

        bool wanted = PauseTextWanted();

        if (wanted == _pauseShown)
            return;

        _pauseShown = wanted;
        pauseText.enabled = wanted;
    }

    private void UpdateLanguageHint()
    {
        if (_languageHintDone || changeLanguageText == null)
            return;

        _languageHintLeft -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        if (_languageHintLeft > 0f)
            return;

        _languageHintDone = true;
        changeLanguageText.SetActive(false);
    }

    private static float LanguageHintSeconds
    {
        get { return Core.Config == null ? 0f : Core.Config.LanguageHintSeconds; }
    }

    private bool PauseTextWanted()
    {
        return Core.IsPaused && Core.IsGameMode;
    }
}
