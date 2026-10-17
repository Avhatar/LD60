using TMPro;
using UnityEngine;

namespace LD60.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private LocalizedString _text = new LocalizedString();

        private TMP_Text _label;

        public void SetText(LocalizedString text)
        {
            if (text == null)
            {
                Debug.LogError($"{nameof(LocalizedText)}: {nameof(text)} is null in {nameof(SetText)} on '{name}'. " +
                               "Required to know what this label should show. " +
                               "Fix: pass a localized string from the SO that owns this line of text.", this);
                return;
            }

            _text = text;
            Apply();
        }

        private void OnEnable()
        {
            Localization.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Localization.Changed -= Apply;
        }

        private void Apply()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();

            if (_label == null)
            {
                Debug.LogError($"{nameof(LocalizedText)}: {nameof(_label)} is null in {nameof(Apply)} on '{name}'. " +
                               "Required to draw the localized line. " +
                               "Fix: add a TextMeshPro text component to this object.", this);
                return;
            }

            string value = _text.Value;
            if (_label.text != value)
                _label.text = value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif
    }
}
