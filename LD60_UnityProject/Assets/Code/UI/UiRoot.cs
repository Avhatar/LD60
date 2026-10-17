using UnityEngine;

public class UiRoot : MonoBehaviour
{
    [SerializeField]
    private DialogueUi dialogueUi;

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
    }
}
