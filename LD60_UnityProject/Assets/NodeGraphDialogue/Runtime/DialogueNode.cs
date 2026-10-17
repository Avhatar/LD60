namespace NodeGraphAvr.Dialogue
{
    [System.Serializable]
    public abstract class DialogueNode : Node
    {
        public override int MaxInConnections => -1;
        public override int MaxOutConnections => 1;

        protected internal virtual ActorData ActionActor => null;

        internal abstract void Enter(DialogueRunner runner);
    }
}
