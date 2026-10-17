using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Dialogue statement node: one actor says one line of text, then the dialogue
    /// continues to the single next node once the consumer confirms the line was shown.
    /// It runs nothing itself — "say this, then do that" is an Action node placed before or
    /// after it, so every side effect stays visible in the graph.
    /// </summary>
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

        [Tooltip("The line of text this node says when entered.")]
        [SerializeField] private string _text = "";

        public ActorData Actor
        {
            get { return _actor; }
            internal set { _actor = value; }
        }

        public string Text
        {
            get { return _text; }
            internal set { _text = value; }
        }

        internal override void Enter(DialogueRunner runner)
        {
            runner.RaiseSay(new SayRequest(runner, _actor, _text));
        }
    }
}
