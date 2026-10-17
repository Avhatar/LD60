namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Base node of the dialogue graph kind: any number of incoming lines converge
    /// into a node, and by default a node continues to exactly one next node.
    /// Branching nodes override the out capacity. A node may also name the actor its
    /// actions and conditions act on; nodes that have no actor at all leave it null and
    /// their actions run on whoever started the dialogue.
    /// </summary>
    [System.Serializable]
    public abstract class DialogueNode : Node
    {
        public override int MaxInConnections => -1;
        public override int MaxOutConnections => 1;

        protected internal virtual ActorData ActionActor => null;

        internal abstract void Enter(DialogueRunner runner);
    }
}
