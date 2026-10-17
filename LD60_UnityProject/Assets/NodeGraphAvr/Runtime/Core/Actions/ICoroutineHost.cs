using System.Collections;

namespace NodeGraphAvr
{
    public interface ICoroutineHost
    {
        void StartRoutine(IEnumerator routine);

        void StopRoutine(IEnumerator routine);
    }
}
