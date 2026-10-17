using UnityEngine;

namespace NodeGraphAvr
{
    public readonly struct ActionContext
    {
        public ActionContext(GameObject agent, ICoroutineHost coroutineHost = null, object sender = null)
        {
            Agent = agent;
            CoroutineHost = coroutineHost;
            Sender = sender;
        }

        public GameObject Agent { get; }

        public ICoroutineHost CoroutineHost { get; }

        public object Sender { get; }

        public bool HasAgent => Agent != null;
    }
}
