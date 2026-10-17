using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Dialogue terminal node: explicitly ends the dialogue with an authored success flag.
    /// Reaching a childless node also ends a dialogue (in success); this node exists for
    /// endings that must be explicit or must report failure.
    /// </summary>
    [Name("Finish")]
    [Description("Ends the dialogue with the authored success flag. Reaching a node without children ends it in success anyway — use this node to end in failure or to make the ending explicit.")]
    [NodeColor("C05C5C")]
    [GraphTypeId("dlg.finish")]
    [System.Serializable]
    public class FinishNode : DialogueNode
    {
        [Tooltip("Whether reaching this ending counts as a successful dialogue outcome.")]
        [SerializeField] private bool _success = true;

        public bool Success
        {
            get { return _success; }
            internal set { _success = value; }
        }

        public override int MaxOutConnections => 0;

        internal override void Enter(DialogueRunner runner)
        {
            runner.FinishFromNode(_success);
        }
    }
}
