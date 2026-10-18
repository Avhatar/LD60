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
    
    private bool _pauseShown;
    private bool _dateShown = true;

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

    private void Update()
    {
        UpdateDate();

        if (pauseText == null)
            return;

        bool wanted = PauseTextWanted();

        if (wanted == _pauseShown)
            return;

        _pauseShown = wanted;
        pauseText.enabled = wanted;
    }

    private void UpdateDate()
    {
        if (dateTimeUi == null)
            return;

        bool wanted = !Core.IsIntroMode;

        if (wanted == _dateShown)
            return;

        _dateShown = wanted;
        dateTimeUi.gameObject.SetActive(wanted);
    }

    private bool PauseTextWanted()
    {
        return Core.IsPaused && Core.IsGameMode;
    }
}
