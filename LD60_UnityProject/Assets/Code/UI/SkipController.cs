using LD60.Intro;
using LD60.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60.UI
{
    public class SkipController : MonoBehaviour
    {
        private const float MaxStepDeltaSeconds = 0.1f;

        [SerializeField] private GameObject _skipTextRoot;
        [SerializeField] private LocalizedText _skipLabel;
        [SerializeField] private IntroSequence _intro;
        [SerializeField] private DialogueUi _dialogueUi;

        private float _held;
        private bool _shown;
        private bool _labelApplied;
        private bool _firstHintShown;
        private float _hintDelayLeft = -1f;

        private void Awake()
        {
            if (_skipTextRoot == null)
            {
                Debug.LogError($"{nameof(SkipController)}: {nameof(_skipTextRoot)} is null in {nameof(Awake)} on '{name}'. " +
                               "Required to tell the player that a cutscene can be held through. " +
                               "Fix: assign the SkipText object to the Skip Text Root slot on this object.", this);
                enabled = false;
                return;
            }

            if (_dialogueUi == null)
            {
                Debug.LogError($"{nameof(SkipController)}: {nameof(_dialogueUi)} is null in {nameof(Awake)} on '{name}'. " +
                               "Required to know when a conversation is on screen and to cut it short. " +
                               "Fix: assign the DialogueUi object to the Dialogue Ui slot on this object.", this);
                enabled = false;
                return;
            }

            _shown = false;
            _skipTextRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsSkippable())
            {
                _held = 0f;
                SetShown(false);
                return;
            }

            ApplyLabel();
            SetShown(HintWanted());

            if (Keyboard.current == null || !Keyboard.current.qKey.isPressed)
            {
                _held = 0f;
                return;
            }

            _held += StepDelta();

            if (_held < HoldSeconds)
                return;

            _held = 0f;
            Skip();
        }

        private bool HintWanted()
        {
            if (_firstHintShown)
                return true;

            if (_hintDelayLeft < 0f)
                _hintDelayLeft = FirstHintDelaySeconds;

            _hintDelayLeft -= StepDelta();

            if (_hintDelayLeft > 0f)
                return false;

            _firstHintShown = true;
            return true;
        }

        private void SetShown(bool shown)
        {
            if (shown == _shown)
                return;

            _shown = shown;
            _skipTextRoot.SetActive(shown);
        }

        private static float StepDelta()
        {
            return Mathf.Min(Time.unscaledDeltaTime, MaxStepDeltaSeconds);
        }

        private bool IsSkippable()
        {
            if (_intro != null && _intro.IsRunning)
                return true;

            return _dialogueUi.IsRunning;
        }

        private void Skip()
        {
            if (_intro != null && _intro.IsRunning)
            {
                _intro.Finish();
                return;
            }

            if (_dialogueUi.IsRunning)
                _dialogueUi.Stop();
        }

        private void ApplyLabel()
        {
            if (_labelApplied || _skipLabel == null)
                return;

            if (Core.Config == null || Core.Config.UiText == null)
                return;

            _skipLabel.SetText(Core.Config.UiText.SkipHint);
            _labelApplied = true;
        }

        private static float FirstHintDelaySeconds
        {
            get
            {
                if (Core.Config == null)
                    return 0f;

                return Core.Config.SkipHintFirstDelaySeconds;
            }
        }

        private static float HoldSeconds
        {
            get
            {
                if (Core.Config == null || Core.Config.SkipHoldSeconds <= 0f)
                    return 3f;

                return Core.Config.SkipHoldSeconds;
            }
        }
    }
}
