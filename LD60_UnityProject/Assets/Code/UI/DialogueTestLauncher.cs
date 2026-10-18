using LD60.Dialogue;
using NodeGraphAvr.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60.UI
{
    public class DialogueTestLauncher : MonoBehaviour
    {
        [SerializeField] private DialogueUi _ui;
        [SerializeField] private DialogueGraph _dialogue;
        [SerializeField] private DialogueFlags _flags;
        [SerializeField] private Key _startKey = Key.F1;
        [SerializeField] private bool _resetFlagsOnStart = true;
        [SerializeField] private bool _startOnPlay;

        private void Start()
        {
            if (_startOnPlay)
                StartDialogue();
        }

        private void OnEnable()
        {
            if (_ui != null)
                _ui.Finished += OnFinished;
        }

        private void OnDisable()
        {
            if (_ui != null)
                _ui.Finished -= OnFinished;
        }

        private void Update()
        {
            if (!Application.isEditor)
                return;

            if (Core.IsMenuMode)
                return;

            if (_startKey == Key.None || Keyboard.current == null)
                return;

            if (!Keyboard.current[_startKey].wasPressedThisFrame)
                return;

            StartDialogue();
        }

        public void StartDialogue()
        {
            if (_ui == null)
            {
                Debug.LogError($"{nameof(DialogueTestLauncher)}: {nameof(_ui)} is null in {nameof(StartDialogue)} on '{name}'. " +
                               "Required to show the test conversation. " +
                               "Fix: assign the DialogueUi object to the Ui slot on this object.", this);
                return;
            }

            if (_dialogue == null)
            {
                Debug.LogError($"{nameof(DialogueTestLauncher)}: {nameof(_dialogue)} is null in {nameof(StartDialogue)} on '{name}'. " +
                               "Required to know which conversation the test key starts. " +
                               "Fix: assign a .ngdlg asset to the Dialogue slot on this object.", this);
                return;
            }

            if (_resetFlagsOnStart)
            {
                if (_flags == null)
                {
                    Debug.LogError($"{nameof(DialogueTestLauncher)}: {nameof(_flags)} is null in {nameof(StartDialogue)} on '{name}' " +
                                   "while flags are set to reset on start, so every run would inherit the previous run's state. " +
                                   "Fix: assign the object carrying DialogueFlags, or turn Reset Flags On Start off.", this);
                    return;
                }

                _flags.ResetToInitial();
            }

            Debug.Log($"{nameof(DialogueTestLauncher)}: starting '{_dialogue.name}'" +
                      (_flags == null ? "." : $" with flags {_flags.Describe()}."), this);

            _ui.Play(_dialogue);
        }

        private void OnFinished(bool success)
        {
            Debug.Log($"{nameof(DialogueTestLauncher)}: '{_dialogue.name}' finished, success={success}" +
                      (_flags == null ? "." : $", flags {_flags.Describe()}."), this);
        }
    }
}
