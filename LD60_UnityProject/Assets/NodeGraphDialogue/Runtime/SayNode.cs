using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    [Name("Say")]
    [Description("One actor says one line. Continues along its single outgoing line when the consumer advances the request.")]
    [NodeColor("5B8FB9")]
    [GraphTypeId("dlg.say")]
    [System.Serializable]
    public class SayNode : DialogueNode
    {
        [Tooltip("Who says this line. Pick from the actors declared on the graph or in the dialogue system config.")]
        [ActorPicker]
        [SerializeField] private ActorData _actor;

        [Tooltip("The line of text this node says when entered, in every supported language.")]
        [SerializeField] private LD60.LocalizedString _text = new LD60.LocalizedString();

        public ActorData Actor
        {
            get { return _actor; }
            internal set { _actor = value; }
        }

        public string Text
        {
            get { return _text.Value; }
        }

        public LD60.LocalizedString Line
        {
            get { return _text; }
        }

        internal override void Enter(DialogueRunner runner)
        {
            runner.RaiseSay(new SayRequest(runner, _actor, _text));
        }
    }
}
