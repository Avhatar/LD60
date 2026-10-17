using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// One authored pair telling a dialogue run which scene object plays a given actor.
    /// Exposed as a serialized list on whatever component starts the dialogue, so a test
    /// scene can wire participants by hand; a real game usually computes the same pairs
    /// at runtime and hands them over instead.
    /// </summary>
    [System.Serializable]
    public class ActorAgentBinding
    {
        [Tooltip("The dialogue actor this binding is for — the same asset that is picked on the graph's nodes.")]
        [SerializeField] private ActorData _actor;

        [Tooltip("Scene object that plays this actor. Actions and conditions of nodes naming this actor run on it.")]
        [SerializeField] private GameObject _agent;

        public ActorData Actor
        {
            get { return _actor; }
            internal set { _actor = value; }
        }

        public GameObject Agent
        {
            get { return _agent; }
            internal set { _agent = value; }
        }

        public ActorAgentBinding() { }

        public ActorAgentBinding(ActorData actor, GameObject agent)
        {
            _actor = actor;
            _agent = agent;
        }
    }
}
