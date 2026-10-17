using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
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
