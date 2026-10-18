using LD60.Settings;
using LD60.Space;
using LD60.UI;
using NodeGraphAvr.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace LD60.Ending
{
    public class EndingSequence : MonoBehaviour
    {
        private const float MaxStepDeltaSeconds = 0.1f;

        [SerializeField] private GameObject _fadeCanvas;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private DialogueUi _dialogueUi;
        [SerializeField] private VinilRoot _vinilRoot;
        [SerializeField] private LocalizedText _finishLabel;
        [SerializeField] private GameObject[] _hiddenDuringEnding = new GameObject[0];

        private EndingPhase _phase = EndingPhase.Watching;
        private float _fadeSeconds;
        private float _fadeElapsed;
        private float _fadeFrom;
        private float _fadeTarget;

        public bool IsRunning
        {
            get { return _phase != EndingPhase.Watching && _phase != EndingPhase.Done; }
        }

        private void OnEnable()
        {
            if (_dialogueUi != null)
                _dialogueUi.Finished += OnDialogueFinished;

            if (_vinilRoot != null)
                _vinilRoot.FinishRequested += OnFinishRequested;
        }

        private void OnDisable()
        {
            if (_dialogueUi != null)
                _dialogueUi.Finished -= OnDialogueFinished;

            if (_vinilRoot != null)
                _vinilRoot.FinishRequested -= OnFinishRequested;
        }

        private void Update()
        {
            if (_phase == EndingPhase.Watching)
            {
                WatchForOutcome();
                return;
            }

            if (_phase == EndingPhase.FadeToBlack || _phase == EndingPhase.FadeToRecord || _phase == EndingPhase.FadeToEnd)
                TickFade();
        }

        private void WatchForOutcome()
        {
            if (!Core.IsGameMode)
                return;

            if (!HasReferences())
            {
                enabled = false;
                return;
            }

            DialogueGraph outcome;

            if (!TryResolveOutcome(out outcome))
                return;

            Core.SetMode(GameMode.Ending);

            if (outcome == null)
            {
                Debug.LogError($"{nameof(EndingSequence)}: the dialogue for the outcome this run reached is null in {nameof(WatchForOutcome)} on '{name}'. " +
                               "Required to tell the player how the run ended before the final record is shown. " +
                               "Fix: assign all three outcome dialogues on the Ending asset.", this);
                BeginFadeToBlack();
                return;
            }

            _phase = EndingPhase.OutcomeTalk;
            _dialogueUi.Play(outcome);
        }

        private bool TryResolveOutcome(out DialogueGraph dialogue)
        {
            dialogue = null;

            EndingSO settings = Settings;

            if (settings == null)
                return false;

            ActPeriod act = Core.CurrentAct;

            if (act == null)
                return false;

            if (AllSendersOnRecord(act))
            {
                dialogue = settings.SuccessDialogue;
                return true;
            }

            if (Core.Watts <= 0f)
            {
                dialogue = settings.PowerFailureDialogue;
                return true;
            }

            if (Core.ActProgress >= 1f)
            {
                dialogue = settings.TimeFailureDialogue;
                return true;
            }

            return false;
        }

        private static bool AllSendersOnRecord(ActPeriod act)
        {
            ActSpacecraftSO[] senders = act.Spacecraft;

            if (senders == null || senders.Length == 0)
                return false;

            for (int i = 0; i < senders.Length; i++)
            {
                if (senders[i] == null || senders[i].Spacecraft == null)
                    continue;

                if (!Core.IsSpacecraftOnRecord(senders[i].Spacecraft))
                    return false;
            }

            return true;
        }

        private void OnDialogueFinished(bool success)
        {
            if (_phase == EndingPhase.OutcomeTalk)
            {
                BeginFadeToBlack();
                return;
            }

            if (_phase == EndingPhase.Farewell)
                BeginFade(EndingPhase.FadeToEnd, 1f, FadeOutSeconds);
        }

        private void OnFinishRequested()
        {
            if (_phase != EndingPhase.Record)
                return;

            EndingSO settings = Settings;
            DialogueGraph farewell = settings == null ? null : settings.FarewellDialogue;

            if (farewell == null)
            {
                BeginFade(EndingPhase.FadeToEnd, 1f, FadeOutSeconds);
                return;
            }

            _phase = EndingPhase.Farewell;
            _dialogueUi.Play(farewell, settings.FarewellSpeaker);
        }

        private void BeginFadeToBlack()
        {
            _dialogueUi.Clear();
            BeginFade(EndingPhase.FadeToBlack, 1f, FadeOutSeconds);
        }

        private void BeginFade(EndingPhase phase, float target, float seconds)
        {
            _phase = phase;
            _fadeTarget = target;
            _fadeSeconds = seconds;
            _fadeElapsed = 0f;
            _fadeCanvas.SetActive(true);
            _fadeFrom = _fadeImage.color.a;

            if (seconds <= 0f)
                FinishFade();
        }

        private void TickFade()
        {
            _fadeElapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepDeltaSeconds);

            if (_fadeSeconds <= 0f)
            {
                FinishFade();
                return;
            }

            float t = Mathf.Clamp01(_fadeElapsed / _fadeSeconds);
            EndingSO settings = Settings;
            AnimationCurve curve = settings == null ? null : settings.FadeCurve;
            float shaped = curve == null || curve.length == 0 ? t : Mathf.Clamp01(curve.Evaluate(t));

            SetFade(Mathf.Lerp(_fadeFrom, _fadeTarget, shaped));

            if (t >= 1f)
                FinishFade();
        }

        private void FinishFade()
        {
            SetFade(_fadeTarget);

            if (_phase == EndingPhase.FadeToBlack)
            {
                ShowFinalRecord();
                BeginFade(EndingPhase.FadeToRecord, 0f, FadeInSeconds);
                return;
            }

            if (_phase == EndingPhase.FadeToRecord)
            {
                _phase = EndingPhase.Record;
                return;
            }

            if (_phase == EndingPhase.FadeToEnd)
            {
                _dialogueUi.Clear();
                _vinilRoot.CloseFinal();
                _phase = EndingPhase.Done;
            }
        }

        private void ShowFinalRecord()
        {
            SetHudHidden(true);
            ApplyFinishLabel();
            _vinilRoot.OpenFinal();
        }

        private void ApplyFinishLabel()
        {
            if (_finishLabel == null)
                return;

            if (Core.Config == null || Core.Config.UiText == null)
            {
                Debug.LogError($"{nameof(EndingSequence)}: {nameof(CoreSO.UiText)} is null in {nameof(ApplyFinishLabel)} on '{name}'. " +
                               "Required to put a caption on the button that closes the final record. " +
                               "Fix: assign the UiText asset to the Ui Text slot on the Core asset.", this);
                return;
            }

            _finishLabel.SetText(Core.Config.UiText.FinishLabel);
        }

        private void SetHudHidden(bool hidden)
        {
            if (_hiddenDuringEnding == null)
                return;

            for (int i = 0; i < _hiddenDuringEnding.Length; i++)
            {
                if (_hiddenDuringEnding[i] == null)
                {
                    Debug.LogError($"{nameof(EndingSequence)}: {nameof(_hiddenDuringEnding)}[{i}] is null in {nameof(SetHudHidden)} on '{name}'. " +
                                   "Required to take that piece of the HUD off screen once the run is over. " +
                                   "Fix: drop the missing object into that element, or shorten the list.", this);
                    continue;
                }

                _hiddenDuringEnding[i].SetActive(!hidden);
            }
        }

        private void SetFade(float alpha)
        {
            float clamped = Mathf.Clamp01(alpha);
            Color color = _fadeImage.color;
            color.a = clamped;
            _fadeImage.color = color;
            _fadeImage.enabled = clamped > 0f;
        }

        private bool HasReferences()
        {
            if (_fadeCanvas == null || _fadeImage == null)
            {
                Debug.LogError($"{nameof(EndingSequence)}: {nameof(_fadeCanvas)} or {nameof(_fadeImage)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required to take the screen to black between the last conversation and the final record. " +
                               "Fix: assign the FadeCanvas object and its FadeImage to the slots on this object.", this);
                return false;
            }

            if (_dialogueUi == null)
            {
                Debug.LogError($"{nameof(EndingSequence)}: {nameof(_dialogueUi)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required to say how the run ended and to say goodbye afterwards. " +
                               "Fix: assign the DialogueUi object to the Dialogue Ui slot on this object.", this);
                return false;
            }

            if (_vinilRoot == null)
            {
                Debug.LogError($"{nameof(EndingSequence)}: {nameof(_vinilRoot)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required to show the finished record the whole run was spent filling. " +
                               "Fix: assign the VinilRoot object to the Vinil Root slot on this object.", this);
                return false;
            }

            return true;
        }

        private float FadeOutSeconds
        {
            get
            {
                EndingSO settings = Settings;
                return settings == null ? 0f : settings.FadeOutSeconds;
            }
        }

        private float FadeInSeconds
        {
            get
            {
                EndingSO settings = Settings;
                return settings == null ? 0f : settings.FadeInSeconds;
            }
        }

        private static EndingSO Settings
        {
            get { return Core.Config == null ? null : Core.Config.Ending; }
        }
    }
}
