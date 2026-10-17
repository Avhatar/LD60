using System.Collections;

namespace NodeGraphAvr
{
    /// <summary>
    /// Host that runs the routine of an action which takes time to finish. Declared as
    /// an interface so the core never reaches for a global coroutine service and so
    /// timed actions can be driven deterministically outside the player loop in tests.
    /// MonoBehaviourCoroutineHost is the shipped implementation.
    /// </summary>
    public interface ICoroutineHost
    {
        void StartRoutine(IEnumerator routine);

        void StopRoutine(IEnumerator routine);
    }
}
