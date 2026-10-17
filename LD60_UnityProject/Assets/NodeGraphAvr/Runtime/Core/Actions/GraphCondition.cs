using System;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// One authored yes/no check a graph can ask before doing something: parameters are
    /// its serialized fields, the answer is synchronous. Conditions never take time —
    /// anything that has to wait is an action. Like actions they are plain serializable
    /// data stored inside their graph and know nothing of any concrete graph kind.
    /// </summary>
    [Serializable]
    public abstract class GraphCondition
    {
        [Tooltip("Inverts the result of this check, so the condition passes exactly when the check itself fails.")]
        [SerializeField] private bool _invert;

        [NonSerialized] private ActionContext _context;
        [NonSerialized] private AgentResolver _agentResolver;

        public bool Invert
        {
            get { return _invert; }
            internal set { _invert = value; }
        }

        public virtual Type AgentType => null;

        public virtual string Summary => null;

        public bool Evaluate(in ActionContext context)
        {
            _context = context;

            if (!_agentResolver.Resolve(AgentType, context, GetType().Name))
                return false;

            var result = OnEvaluate();
            return _invert ? !result : result;
        }

        public virtual string Validate() => null;

        protected ActionContext Context => _context;

        protected GameObject AgentObject => _context.Agent;

        protected Component AgentComponent => _agentResolver.Component;

        protected abstract bool OnEvaluate();
    }

    /// <summary>
    /// Condition that requires a specific component (or interface) on its agent. A
    /// missing component is a logged error and a failed check.
    /// </summary>
    public abstract class GraphCondition<T> : GraphCondition where T : class
    {
        public sealed override Type AgentType => typeof(T);

        protected T Agent => (T)(object)AgentComponent;
    }
}
