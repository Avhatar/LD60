using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Raised when a SayNode is entered: one actor says one line. The consumer shows
    /// the line and calls Advance() when it is done; the continuation is an instance
    /// method, so no closure is allocated. A request outlives its usefulness when the
    /// dialogue stops or moves on — a stale or repeated Advance is ignored with a
    /// warning.
    /// </summary>
    public class SayRequest
    {
        private readonly DialogueRunner _runner;
        private readonly int _runVersion;
        private bool _consumed;

        public ActorData Actor { get; }

        public string Text { get; }

        internal SayRequest(DialogueRunner runner, ActorData actor, string text)
        {
            _runner = runner;
            _runVersion = runner.RunVersion;
            Actor = actor;
            Text = text;
        }

        public void Advance()
        {
            if (_consumed || _runVersion != _runner.RunVersion)
            {
                Debug.LogWarning($"{nameof(SayRequest)}: {nameof(Advance)} ignored — the request was already consumed " +
                                 "or the dialogue has moved on (stopped, finished, or restarted). " +
                                 "Fix: call Advance exactly once per request and drop requests after Finished.");
                return;
            }

            _consumed = true;
            _runner.ContinueTo(0);
        }
    }
}
