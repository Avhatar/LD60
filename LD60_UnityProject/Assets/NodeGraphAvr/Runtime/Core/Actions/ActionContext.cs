using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// Everything an action or a condition receives from whoever runs the graph: the
    /// agent it acts on, the host that runs timed routines, and the object that started
    /// the run for log context. A value type, rebuilt per execution, so dispatching an
    /// action allocates nothing. Deciding who the agent is belongs to the caller — the
    /// core only ever sees an already resolved GameObject.
    /// </summary>
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
