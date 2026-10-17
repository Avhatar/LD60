using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// One selectable option of a ChoiceNode. Owns the authored option text, an explicit
    /// link to its outgoing line (by connection uid) so any option can be wired in any
    /// order, and an optional condition deciding whether the option is offered at all.
    /// Whatever should happen when the option is picked is authored as Action nodes on its
    /// line, so it stays visible in the graph; the condition cannot be, because visibility
    /// is decided before the option is shown.
    /// </summary>
    [System.Serializable]
    public class ChoiceOption
    {
        [Tooltip("Text shown to the player for this option, in every supported language.")]
        [SerializeField] private LD60.LocalizedString _text = new LD60.LocalizedString();

        [Tooltip("Uid of the out-connection carrying this option's flow. Empty means the option has no line and selecting it ends the dialogue in success.")]
        [HideInInspector]
        [SerializeField] private string _lineUid = "";

        [Tooltip("Optional check deciding whether this option is offered at all. No condition means the option is always offered.")]
        [SerializeReference] private GraphCondition _visibleIf;

        public string Text
        {
            get { return _text.Value; }
        }

        public LD60.LocalizedString Line
        {
            get { return _text; }
        }

        public string LineUid
        {
            get { return _lineUid; }
            internal set { _lineUid = value; }
        }

        public GraphCondition VisibleIf
        {
            get { return _visibleIf; }
            internal set { _visibleIf = value; }
        }

        public ChoiceOption() { }

        public ChoiceOption(string text)
        {
            _text = new LD60.LocalizedString(text, text);
        }
    }
}
