using System.Collections;
using System.Collections.Generic;
using System.Text;
using LD60;
using LD60.Settings;
using LD60.Sound;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueReplicUi : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private RectTransform replicRootTransform;

    [SerializeField]
    private TMP_Text replicText;

    [SerializeField]
    private Image portraitImage;

    private readonly StringBuilder _options = new StringBuilder();

    private DialogueUi _owner;
    private Image _caret;
    private Coroutine _typeRoutine;
    private Coroutine _blinkRoutine;
    private bool _showsOptions;
    private int _characterCount;
    private LocalizedString _line;
    private IReadOnlyList<LocalizedString> _optionLines;
    private Sprite _portrait;

    public RectTransform RectTransform
    {
        get { return (RectTransform)transform; }
    }

    public bool IsTyping { get; private set; }

    public void Show(Sprite portrait, LocalizedString text)
    {
        _owner = null;
        _showsOptions = false;
        _optionLines = null;
        _line = text;
        _portrait = portrait;
        Draw(portrait, text == null ? "" : text.Value);
    }

    public void ShowOptions(DialogueUi owner, IReadOnlyList<LocalizedString> options, Sprite portrait)
    {
        if (owner == null || options == null)
        {
            Debug.LogError($"{nameof(DialogueReplicUi)}: {nameof(owner)} or {nameof(options)} is null in {nameof(ShowOptions)} on '{name}'. " +
                           "Required to draw the offered options and report the pick back to the dialogue. " +
                           "Fix: pass the DialogueUi that runs the conversation and its current option list.", this);
            return;
        }

        _owner = owner;
        _showsOptions = true;
        _line = null;
        _optionLines = options;
        _portrait = portrait;
        Draw(portrait, BuildOptions(options));
    }

    public void Hide()
    {
        HideCaret();
        StopAllCoroutines();
        _typeRoutine = null;
        IsTyping = false;
        _owner = null;
        _showsOptions = false;
        _line = null;
        _optionLines = null;
        gameObject.SetActive(false);
    }

    public void HideCaret()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        if (_caret != null)
            _caret.enabled = false;
    }

    public void BeginTyping()
    {
        if (replicText == null)
            return;

        DialogueSO settings = Core.Config == null ? null : Core.Config.Dialogue;
        if (settings == null)
        {
            Debug.LogError($"{nameof(DialogueReplicUi)}: dialogue settings are unavailable in {nameof(BeginTyping)} on '{name}'. " +
                           "Required to know how fast characters are revealed and which sound they click with; " +
                           "the line is shown at once instead. " +
                           "Fix: assign a DialogueSO asset to the Dialogue slot on the CoreSO asset.", this);
            return;
        }

        replicText.ForceMeshUpdate();
        _characterCount = replicText.textInfo.characterCount;

        if (_characterCount <= 0)
        {
            if (string.IsNullOrEmpty(replicText.text))
                return;

            replicText.maxVisibleCharacters = 0;
            IsTyping = true;
            _typeRoutine = StartCoroutine(TypeAfterLayout(settings));
            return;
        }

        replicText.maxVisibleCharacters = 0;
        IsTyping = true;
        _typeRoutine = StartCoroutine(TypeRoutine(settings));
    }

    private IEnumerator TypeAfterLayout(DialogueSO settings)
    {
        yield return null;

        replicText.ForceMeshUpdate();
        _characterCount = replicText.textInfo.characterCount;

        if (_characterCount <= 0)
        {
            IsTyping = false;
            _typeRoutine = null;
            replicText.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        yield return TypeRoutine(settings);
    }

    public void CompleteTyping()
    {
        if (!IsTyping)
            return;

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        IsTyping = false;

        if (replicText != null)
            replicText.maxVisibleCharacters = int.MaxValue;

        ShowCaret();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner == null || !_showsOptions || replicText == null)
            return;

        int index = TMP_TextUtilities.FindIntersectingLink(replicText, eventData.position, OptionCamera());
        if (index < 0)
            return;

        _owner.SelectOption(index);
    }

    private void OnEnable()
    {
        Localization.Changed += ApplyLanguage;
    }

    private void OnDisable()
    {
        Localization.Changed -= ApplyLanguage;
    }

    private void ApplyLanguage()
    {
        if (replicText == null)
            return;

        if (_showsOptions)
        {
            if (_optionLines == null)
                return;

            Draw(_portrait, BuildOptions(_optionLines));
            return;
        }

        if (_line == null)
            return;

        bool wasTyping = IsTyping;
        Draw(_portrait, _line.Value);

        if (wasTyping)
            BeginTyping();
        else
            ShowCaret();
    }

    private string BuildOptions(IReadOnlyList<LocalizedString> options)
    {
        _options.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            if (i > 0)
                _options.Append('\n');

            _options.Append("<link=\"").Append(i).Append("\">").Append(i + 1).Append(". ")
                    .Append(options[i] == null ? "" : options[i].Value).Append("</link>");
        }

        return _options.ToString();
    }

    private void Draw(Sprite portrait, string text)
    {
        HideCaret();
        StopAllCoroutines();
        _typeRoutine = null;
        IsTyping = false;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (replicText == null)
        {
            Debug.LogError($"{nameof(DialogueReplicUi)}: {nameof(replicText)} is null in {nameof(Draw)} on '{name}'. " +
                           "Required to draw the line this replic was given. " +
                           "Fix: assign the TextMeshPro object to the Replic Text slot on the replic prefab.", this);
            return;
        }

        replicText.text = text;
        replicText.maxVisibleCharacters = int.MaxValue;

        if (portraitImage == null)
            return;

        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
    }

    private Camera OptionCamera()
    {
        Canvas canvas = replicText.canvas;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private IEnumerator TypeRoutine(DialogueSO settings)
    {
        TMP_TextInfo textInfo = replicText.textInfo;
        SoundContainerSO typewriterSound = settings.TypewriterSound;
        float charactersPerSecond = settings.TypewriterCharactersPerSecond;

        float revealed = 0f;
        int shown = 0;

        while (shown < _characterCount)
        {
            revealed += charactersPerSecond * Time.unscaledDeltaTime;

            int target = Mathf.Min(_characterCount, Mathf.FloorToInt(revealed));
            bool clicks = false;
            while (shown < target)
            {
                if (!char.IsWhiteSpace(textInfo.characterInfo[shown].character))
                    clicks = true;

                shown++;
            }

            replicText.maxVisibleCharacters = shown;

            if (clicks && typewriterSound != null && AudioController.Instance != null)
                AudioController.Instance.Play2D(typewriterSound);

            if (shown >= _characterCount)
                break;

            yield return null;
        }

        IsTyping = false;
        _typeRoutine = null;
        ShowCaret();
    }

    private void ShowCaret()
    {
        if (replicText == null || _showsOptions)
            return;

        DialogueSO settings = Core.Config == null ? null : Core.Config.Dialogue;
        if (settings == null)
            return;

        EnsureCaret();
        if (!PlaceCaret(settings))
            return;

        _caret.enabled = true;

        if (_blinkRoutine != null)
            StopCoroutine(_blinkRoutine);

        _blinkRoutine = StartCoroutine(BlinkRoutine(Mathf.Max(0.05f, settings.CaretBlinkSeconds)));
    }

    private void EnsureCaret()
    {
        if (_caret != null)
        {
            _caret.color = replicText.color;
            return;
        }

        GameObject caretObject = new GameObject("Caret");
        RectTransform rect = caretObject.AddComponent<RectTransform>();
        rect.SetParent(replicText.rectTransform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;

        _caret = caretObject.AddComponent<Image>();
        _caret.color = replicText.color;
        _caret.raycastTarget = false;
    }

    private bool PlaceCaret(DialogueSO settings)
    {
        replicText.ForceMeshUpdate();
        TMP_TextInfo textInfo = replicText.textInfo;
        if (textInfo.characterCount <= 0)
        {
            _caret.enabled = false;
            return false;
        }

        TMP_CharacterInfo last = textInfo.characterInfo[textInfo.characterCount - 1];
        Rect textRect = replicText.rectTransform.rect;

        RectTransform rect = _caret.rectTransform;
        rect.sizeDelta = settings.CaretSize;
        rect.anchoredPosition = new Vector2(last.topRight.x - textRect.xMin + settings.CaretOffset.x,
                                            last.baseLine - textRect.yMin + settings.CaretOffset.y);
        return true;
    }

    private IEnumerator BlinkRoutine(float blinkSeconds)
    {
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= blinkSeconds)
            {
                elapsed = 0f;
                _caret.enabled = !_caret.enabled;
            }

            yield return null;
        }
    }
}
