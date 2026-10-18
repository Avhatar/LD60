using LD60.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LD60.Intro
{
    public class IntroSequence : MonoBehaviour
    {
        [SerializeField] private GameObject _introCanvas;
        [SerializeField] private GameObject _fadeCanvas;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private Image _pictureImage;
        [SerializeField] private GameObject _pictureRoot;
        [SerializeField] private DialogueUi _dialogueUi;
        [SerializeField] private Key _restartKey = Key.F3;

        private int _stepIndex = -1;
        private float _stepSeconds;
        private float _fadeFrom;
        private bool _running;
        private bool _waitingForDialogue;
        private int _ignoreClickFrame = -1;

        public bool IsRunning
        {
            get { return _running; }
        }

        public void Play()
        {
            if (!HasReferences())
                return;

            if (Settings == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(CoreSO.Intro)} is null in {nameof(Play)} on '{name}'. " +
                               "Required as the list of steps the opening plays before the first act. " +
                               "Fix: assign the Intro asset to the Intro slot on the Core asset.", this);
                return;
            }

            if (_waitingForDialogue && _dialogueUi != null)
                _dialogueUi.Stop();

            _waitingForDialogue = false;
            _running = true;
            _stepIndex = -1;
            _ignoreClickFrame = Time.frameCount;

            Core.SetMode(GameMode.Intro);
            ResetAct();
            _introCanvas.SetActive(true);
            _fadeCanvas.SetActive(true);
            SetFade(1f);
            ShowPicture(null);
            EnterStep(0);
        }

        public void Finish()
        {
            _running = false;
            _waitingForDialogue = false;
            _stepIndex = -1;

            if (_dialogueUi != null && _dialogueUi.IsRunning)
                _dialogueUi.Stop();

            ShowPicture(null);
            SetFade(0f);
            _introCanvas.SetActive(false);
            _fadeCanvas.SetActive(false);
            Core.SetMode(GameMode.Game);
        }

        private void Awake()
        {
            if (!HasReferences())
            {
                enabled = false;
                return;
            }

            _fadeImage.raycastTarget = false;
            _pictureImage.raycastTarget = false;
            _introCanvas.SetActive(false);
            _fadeCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            if (_dialogueUi != null)
                _dialogueUi.Finished += OnDialogueFinished;
        }

        private void OnDisable()
        {
            if (_dialogueUi != null)
                _dialogueUi.Finished -= OnDialogueFinished;
        }

        private void Start()
        {
            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(Start)} on '{name}'. " +
                               $"Required to read whether {nameof(CoreSO.IntroEnabled)} lets the opening play at all. " +
                               "Fix: assign the CoreSO asset to the Config slot on the Core object in the scene.", this);
                Finish();
                return;
            }

            if (!Core.Config.IntroEnabled)
            {
                Finish();
                return;
            }

            Play();
        }

        private void Update()
        {
            if (RestartRequested())
            {
                Play();
                return;
            }

            if (!_running || _waitingForDialogue)
                return;

            Advance();
        }

        private void Advance()
        {
            IntroSO settings = Settings;
            IntroStep step = settings == null ? null : settings.GetStep(_stepIndex);

            if (step == null)
            {
                Finish();
                return;
            }

            _stepSeconds += Time.unscaledDeltaTime;

            if (step.Kind == IntroStepKind.FadeIn)
            {
                if (!TickFade(step, 0f))
                    return;
            }
            else if (step.Kind == IntroStepKind.FadeOut)
            {
                if (!TickFade(step, 1f))
                    return;
            }
            else if (step.Kind == IntroStepKind.Wait)
            {
                if (_stepSeconds < step.Seconds)
                    return;
            }
            else if (step.Kind == IntroStepKind.WaitClick)
            {
                if (!ClickRequested())
                    return;

                _ignoreClickFrame = Time.frameCount;
            }

            EnterStep(_stepIndex + 1);
        }

        private void EnterStep(int index)
        {
            IntroSO settings = Settings;

            if (settings == null)
            {
                Finish();
                return;
            }

            while (true)
            {
                if (index >= settings.StepCount)
                {
                    Finish();
                    return;
                }

                IntroStep step = settings.GetStep(index);

                if (step == null)
                {
                    index++;
                    continue;
                }

                _stepIndex = index;
                _stepSeconds = 0f;
                _fadeFrom = _fadeImage.color.a;

                if (step.Kind == IntroStepKind.ShowPicture)
                {
                    ShowPicture(step.Picture);
                    index++;
                    continue;
                }

                if (step.Kind == IntroStepKind.HidePicture)
                {
                    ShowPicture(null);
                    index++;
                    continue;
                }

                if (step.Kind == IntroStepKind.Dialogue)
                {
                    if (StartDialogue(step))
                        return;

                    index++;
                    continue;
                }

                if (step.Seconds <= 0f && step.Kind != IntroStepKind.WaitClick)
                {
                    if (step.Kind == IntroStepKind.FadeIn)
                        SetFade(0f);
                    else if (step.Kind == IntroStepKind.FadeOut)
                        SetFade(1f);

                    index++;
                    continue;
                }

                return;
            }
        }

        private bool StartDialogue(IntroStep step)
        {
            if (step.Dialogue == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: intro step {_stepIndex} is a {nameof(IntroStepKind.Dialogue)} step " +
                               $"with an empty {nameof(IntroStep.Dialogue)} slot, so the opening would stall with nothing to say. " +
                               "Fix: assign a .ngdlg asset to that step on the Intro asset, or change the step kind.", this);
                return false;
            }

            _waitingForDialogue = true;
            _dialogueUi.Play(step.Dialogue);
            return true;
        }

        private void OnDialogueFinished(bool success)
        {
            if (!_running || !_waitingForDialogue)
                return;

            _waitingForDialogue = false;
            _ignoreClickFrame = Time.frameCount;
            EnterStep(_stepIndex + 1);
        }

        private bool TickFade(IntroStep step, float target)
        {
            if (step.Seconds <= 0f)
            {
                SetFade(target);
                return true;
            }

            float t = Mathf.Clamp01(_stepSeconds / step.Seconds);
            SetFade(Mathf.Lerp(_fadeFrom, target, t));
            return t >= 1f;
        }

        private void SetFade(float alpha)
        {
            float clamped = Mathf.Clamp01(alpha);
            Color color = _fadeImage.color;
            color.a = clamped;
            _fadeImage.color = color;
            _fadeImage.enabled = clamped > 0f;
        }

        private void ShowPicture(Sprite picture)
        {
            _pictureImage.sprite = picture;
            _pictureRoot.SetActive(picture != null);
        }

        private void ResetAct()
        {
            if (Core.Config == null || Core.Config.TimeScale == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(CoreSO.TimeScale)} is null in {nameof(ResetAct)} on '{name}'. " +
                               "Required to rewind the clock to the first act before the opening plays. " +
                               "Fix: assign the TimeScale asset to the Time Scale slot on the Core asset.", this);
                return;
            }

            Core.SetAct(Core.Config.TimeScale.StartActIndex);
        }

        private bool RestartRequested()
        {
            if (_restartKey == Key.None || Keyboard.current == null)
                return false;

            return Keyboard.current[_restartKey].wasPressedThisFrame;
        }

        private bool ClickRequested()
        {
            if (Time.frameCount == _ignoreClickFrame)
                return false;

            Mouse mouse = Mouse.current;

            if (mouse == null)
                return false;

            return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
        }

        private bool HasReferences()
        {
            if (_introCanvas == null || _fadeCanvas == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_introCanvas)} or {nameof(_fadeCanvas)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required as the two canvases the opening switches on while it runs and leaves off the rest of the game. " +
                               "Fix: assign the IntroCanvas and FadeCanvas objects to the slots on this object.", this);
                return false;
            }

            if (_fadeImage == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_fadeImage)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required as the black full-screen image the opening fades through. " +
                               "Fix: assign the FadeImage object to the Fade Image slot on this object.", this);
                return false;
            }

            if (_pictureImage == null || _pictureRoot == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_pictureImage)} or {nameof(_pictureRoot)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required to show and hide the opening pictures over their backdrop. " +
                               "Fix: assign the IntroPicture object and its root to the slots on this object.", this);
                return false;
            }

            if (_dialogueUi == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_dialogueUi)} is null in {nameof(HasReferences)} on '{name}'. " +
                               "Required to run the conversations the opening steps call for. " +
                               "Fix: assign the DialogueUi object to the Dialogue Ui slot on this object.", this);
                return false;
            }

            return true;
        }

        private static IntroSO Settings
        {
            get { return Core.Config == null ? null : Core.Config.Intro; }
        }
    }
}
