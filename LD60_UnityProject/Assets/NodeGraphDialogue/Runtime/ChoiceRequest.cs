using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Raised when a ChoiceNode is entered. The consumer shows the option texts and
    /// calls Select(index) with the player's pick; the dialogue continues along the
    /// out-connection bound to that option (an option without a line ends the dialogue in
    /// success). Options hidden by their condition are absent from this request, so indices
    /// here are positions among the offered options, not in the node's authored list.
    /// A stale or repeated Select is ignored with a warning.
    /// </summary>
    public class ChoiceRequest
    {
        private readonly DialogueRunner _runner;
        private readonly int _runVersion;
        private readonly int[] _lineIndices;
        private bool _consumed;

        public IReadOnlyList<string> Options { get; }

        internal ChoiceRequest(DialogueRunner runner, string[] options, int[] lineIndices)
        {
            _runner = runner;
            _runVersion = runner.RunVersion;
            _lineIndices = lineIndices;
            Options = options;
        }

        public void Select(int index)
        {
            if (_consumed || _runVersion != _runner.RunVersion)
            {
                Debug.LogWarning($"{nameof(ChoiceRequest)}: {nameof(Select)} ignored — the request was already consumed " +
                                 "or the dialogue has moved on (stopped, finished, or restarted). " +
                                 "Fix: call Select exactly once per request and drop requests after Finished.");
                return;
            }

            if (index < 0 || index >= Options.Count)
            {
                Debug.LogWarning($"{nameof(ChoiceRequest)}: {nameof(Select)}({index}) ignored — the request has only " +
                                 $"{Options.Count} option(s). Fix: pass an index shown to the player.");
                return;
            }

            _consumed = true;
            _runner.ContinueTo(_lineIndices[index]);
        }
    }
}
