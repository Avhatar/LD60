using LD60.Dialogue;
using LD60.Settings;
using NodeGraphAvr.Dialogue;
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
        [SerializeField] private GameObject _vinilBaseSide;
        [SerializeField] private GameObject _vinilSpacecraftSide;
        [SerializeField] private GameObject[] _hiddenDuringIntro = new GameObject[0];
        [SerializeField] private DialogueUi _dialogueUi;
        [SerializeField] private DialogueGraph _tutorialDialogue;
        [SerializeField] private Key _restartKey = Key.F3;

        private const float MaxStepDeltaSeconds = 0.1f;

        private int _stepIndex = -1;
        private float _stepSeconds;
        private bool _waitingToStart;
        private float _startDelayLeft;
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

            if (_dialogueUi != null && _dialogueUi.IsRunning)
                _dialogueUi.Stop();

            _waitingForDialogue = false;
            _waitingToStart = false;
            _running = true;
            _stepIndex = -1;
            _ignoreClickFrame = Time.frameCount;

            Core.SetMode(GameMode.Intro);
            ResetAct();
            _introCanvas.SetActive(true);
            _fadeCanvas.SetActive(true);
            SetFade(1f);
            ShowPicture(null);
            HideVinil();
            SetHudHidden(true);
            EnterStep(0);
        }

        public void Finish()
        {
            bool played = _running;

            _running = false;
            _waitingForDialogue = false;
            _waitingToStart = false;
            _stepIndex = -1;

            if (_dialogueUi != null)
            {
                _dialogueUi.Stop();
                _dialogueUi.Clear();
            }

            ShowPicture(null);
            HideVinil();
            SetHudHidden(false);
            SetFade(0f);
            _introCanvas.SetActive(false);
            _fadeCanvas.SetActive(false);
            Core.SetMode(GameMode.Game);

            if (played)
                StartTutorial();
        }

        private void StartTutorial()
        {
            if (_tutorialDialogue == null)
                return;

            if (_dialogueUi == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_dialogueUi)} is null in {nameof(StartTutorial)} on '{name}'. " +
                               "Required to run the tutorial conversation the opening hands the player over to. " +
                               "Fix: assign the DialogueUi object to the Dialogue Ui slot on this object.", this);
                return;
            }

            _dialogueUi.Play(_tutorialDialogue);
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
            _fadeCanvas.SetActive(true);
            SetFade(1f);
            HideVinil();
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

            IntroSO settings = Settings;

            Core.SetMode(GameMode.Intro);
            _introCanvas.SetActive(false);
            _fadeCanvas.SetActive(true);
            SetFade(1f);
            ShowPicture(null);
            HideVinil();
            SetHudHidden(true);

            _waitingToStart = true;
            _startDelayLeft = settings == null ? 0f : settings.StartDelaySeconds;
        }

        private void Update()
        {
            if (RestartRequested())
            {
                Play();
                return;
            }

            if (_waitingToStart)
            {
                _startDelayLeft -= StepDelta();

                if (_startDelayLeft > 0f)
                    return;

                _waitingToStart = false;
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

            _stepSeconds += StepDelta();

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

                if (step.Kind == IntroStepKind.ShowVinilBase)
                {
                    ShowVinil(true);
                    index++;
                    continue;
                }

                if (step.Kind == IntroStepKind.ShowVinilSpacecraft)
                {
                    ShowVinil(false);
                    index++;
                    continue;
                }

                if (step.Kind == IntroStepKind.HideVinil)
                {
                    HideVinil();
                    index++;
                    continue;
                }

                if (step.Kind == IntroStepKind.Line)
                {
                    if (StartLine(step))
                        return;

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

        private bool StartLine(IntroStep step)
        {
            string text = step.Line == null ? null : step.Line.Value;

            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError($"{nameof(IntroSequence)}: intro step {_stepIndex} is a {nameof(IntroStepKind.Line)} step " +
                               $"with an empty {nameof(IntroStep.Line)}, so the narrator would open his mouth and say nothing. " +
                               "Fix: fill the line on that step of the Intro asset, or change the step kind.", this);
                return false;
            }

            IntroSO settings = Settings;

            _waitingForDialogue = true;
            _dialogueUi.PlayLine(settings == null ? null : settings.Narrator, text);
            return true;
        }

        private void ShowVinil(bool baseSide)
        {
            if (_vinilBaseSide == null || _vinilSpacecraftSide == null)
            {
                Debug.LogError($"{nameof(IntroSequence)}: {nameof(_vinilBaseSide)} or {nameof(_vinilSpacecraftSide)} is null in {nameof(ShowVinil)} on '{name}'. " +
                               "Required to turn the record to the side the opening talks about. " +
                               "Fix: assign the BaseVinil and SpacecraftVinil objects to the slots on this object.", this);
                return;
            }

            _vinilBaseSide.SetActive(baseSide);
            _vinilSpacecraftSide.SetActive(!baseSide);
        }

        private void SetHudHidden(bool hidden)
        {
            if (_hiddenDuringIntro == null)
                return;

            for (int i = 0; i < _hiddenDuringIntro.Length; i++)
            {
                if (_hiddenDuringIntro[i] == null)
                {
                    Debug.LogError($"{nameof(IntroSequence)}: {nameof(_hiddenDuringIntro)}[{i}] is null in {nameof(SetHudHidden)} on '{name}'. " +
                                   "Required to take that piece of the HUD off screen while the opening plays. " +
                                   "Fix: drop the missing object into that element, or shorten the list.", this);
                    continue;
                }

                _hiddenDuringIntro[i].SetActive(!hidden);
            }
        }

        private void HideVinil()
        {
            if (_vinilBaseSide != null)
                _vinilBaseSide.SetActive(false);

            if (_vinilSpacecraftSide != null)
                _vinilSpacecraftSide.SetActive(false);
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
            IntroSO settings = Settings;
            AnimationCurve curve = settings == null ? null : settings.FadeCurve;
            float shaped = curve == null || curve.length == 0 ? t : Mathf.Clamp01(curve.Evaluate(t));

            SetFade(Mathf.Lerp(_fadeFrom, target, shaped));
            return t >= 1f;
        }

        private static float StepDelta()
        {
            return Mathf.Min(Time.unscaledDeltaTime, MaxStepDeltaSeconds);
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
