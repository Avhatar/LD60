using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Dialogue node that only does something: it runs one action on the selected actor's
    /// agent and then continues along its single outgoing line. It is the only place an
    /// action is authored, so every side effect of a dialogue is a node one can see; several
    /// things in a row are several of these nodes in a row. A failing action stops the whole
    /// dialogue as failed — running it is the point of the node.
    /// </summary>
    [Name("Action")]
    [Description("Runs one action, then continues. If the action fails the dialogue stops as failed. The assigned action's own color tints this node.")]
    [GraphTypeId("dlg.action")]
    [System.Serializable]
    public class ActionNode : DialogueNode, IActionHost
    {
        [Tooltip("Whose agent the action acts on. Leave empty to act on whoever started the dialogue.")]
        [ActorPicker]
        [SerializeField] private ActorData _actor;

        [Tooltip("The action this node runs when entered. The dialogue waits for it before moving on.")]
        [SerializeReference] private GraphAction _action;

        public ActorData Actor
        {
            get { return _actor; }
            internal set { _actor = value; }
        }

        public GraphAction Action
        {
            get { return _action; }
            internal set { _action = value; }
        }

        protected internal override ActorData ActionActor => _actor;

        internal override void Enter(DialogueRunner runner)
        {
            runner.ContinueAfterAction(_action, 0);
        }
    }
}
