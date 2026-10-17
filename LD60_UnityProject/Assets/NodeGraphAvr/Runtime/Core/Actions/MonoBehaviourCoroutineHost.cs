using System.Collections;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// Shipped coroutine host: runs action routines on a MonoBehaviour the caller owns.
    /// The caller keeps ownership — when that component is destroyed its routines die
    /// with it, which is exactly the desired lifetime for a graph run it started.
    /// </summary>
    public class MonoBehaviourCoroutineHost : ICoroutineHost
    {
        private readonly MonoBehaviour _owner;

        public MonoBehaviourCoroutineHost(MonoBehaviour owner)
        {
            if (owner == null)
            {
                Debug.LogError($"{nameof(MonoBehaviourCoroutineHost)}: {nameof(owner)} is null in the constructor. " +
                               "The host needs a live MonoBehaviour to run action routines on. " +
                               "Fix: pass the component that starts the graph, e.g. new MonoBehaviourCoroutineHost(this).");
                return;
            }

            _owner = owner;
        }

        public void StartRoutine(IEnumerator routine)
        {
            if (_owner == null || routine == null)
            {
                Debug.LogError($"{nameof(MonoBehaviourCoroutineHost)}: cannot start a routine — " +
                               $"{(_owner == null ? "the owning MonoBehaviour is null or destroyed" : "the routine is null")}. " +
                               "The action that requested it will never finish. " +
                               "Fix: keep the owner alive for the duration of the graph run and pass a valid routine.");
                return;
            }

            _owner.StartCoroutine(routine);
        }

        public void StopRoutine(IEnumerator routine)
        {
            if (_owner == null || routine == null)
                return;

            _owner.StopCoroutine(routine);
        }
    }
}
