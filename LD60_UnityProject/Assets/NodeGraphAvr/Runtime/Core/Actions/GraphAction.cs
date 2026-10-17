using System;
using System.Collections;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// One authored unit of behaviour a graph can run: parameters are its serialized
    /// fields, the result is a single success flag. Execution is callback-driven, not
    /// tick-driven — an instant action completes synchronously inside OnExecute, one that
    /// takes time starts a routine on the context's coroutine host and calls EndAction
    /// later. Actions are plain serializable data stored inside their graph; they hold no
    /// drawing code and know nothing of any concrete graph kind.
    /// </summary>
    [Serializable]
    public abstract class GraphAction
    {
        [NonSerialized] private ActionContext _context;
        [NonSerialized] private IActionCompletion _completion;
        [NonSerialized] private IEnumerator _routine;
        [NonSerialized] private AgentResolver _agentResolver;
        [NonSerialized] private bool _isRunning;

        public bool IsRunning => _isRunning;

        public virtual Type AgentType => null;

        public virtual string Summary => null;

        public void Execute(in ActionContext context, IActionCompletion completion)
        {
            if (_isRunning)
            {
                Debug.LogError($"{GetType().Name}: {nameof(Execute)} was called while the action is already running. " +
                               "The second call is refused and reported as failure. " +
                               $"Fix: wait for the completion callback, or call {nameof(Interrupt)} before running it again.");
                completion?.OnActionFinished(false);
                return;
            }

            _context = context;

            if (!_agentResolver.Resolve(AgentType, context, GetType().Name))
            {
                completion?.OnActionFinished(false);
                return;
            }

            _completion = completion;
            _isRunning = true;
            OnExecute();
        }

        public void Interrupt()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _completion = null;
            StopRoutineIfAny();
            OnStop(true);
        }

        public virtual string Validate() => null;

        protected ActionContext Context => _context;

        protected GameObject AgentObject => _context.Agent;

        protected Component AgentComponent => _agentResolver.Component;

        protected void EndAction(bool success)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            var completion = _completion;
            _completion = null;
            StopRoutineIfAny();
            OnStop(false);
            completion?.OnActionFinished(success);
        }

        protected void StartRoutine(IEnumerator routine)
        {
            if (routine == null)
            {
                Debug.LogError($"{GetType().Name}: {nameof(StartRoutine)} was given a null routine, so the action could never finish. " +
                               "It reports failure instead. Fix: return a real IEnumerator from the action's routine method.");
                EndAction(false);
                return;
            }

            var host = _context.CoroutineHost;
            if (host == null)
            {
                Debug.LogError($"{GetType().Name}: the action needs a coroutine host to run over time, but the context carries none. " +
                               "It reports failure immediately. Fix: pass an ICoroutineHost into the ActionContext, " +
                               "e.g. new MonoBehaviourCoroutineHost(this) from the component that starts the graph.");
                EndAction(false);
                return;
            }

            _routine = routine;
            host.StartRoutine(routine);
        }

        protected abstract void OnExecute();

        protected virtual void OnStop(bool interrupted)
        {
        }

        private void StopRoutineIfAny()
        {
            if (_routine == null)
                return;

            var routine = _routine;
            _routine = null;
            var host = _context.CoroutineHost;
            if (host != null)
                host.StopRoutine(routine);
        }
    }

    /// <summary>
    /// Action that requires a specific component (or interface) on its agent. The
    /// component is resolved once per agent and cached; a missing one is a logged error
    /// and a failed action rather than a silent no-op.
    /// </summary>
    public abstract class GraphAction<T> : GraphAction where T : class
    {
        public sealed override Type AgentType => typeof(T);

        protected T Agent => (T)(object)AgentComponent;
    }
}
