using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    public class ChoiceRequest
    {
        private readonly DialogueRunner _runner;
        private readonly int _runVersion;
        private readonly int[] _lineIndices;
        private bool _consumed;

        public IReadOnlyList<LD60.LocalizedString> Options { get; }

        internal ChoiceRequest(DialogueRunner runner, LD60.LocalizedString[] options, int[] lineIndices)
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
