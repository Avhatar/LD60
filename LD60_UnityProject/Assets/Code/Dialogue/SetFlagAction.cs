using System;
using NodeGraphAvr;
using UnityEngine;

namespace LD60.Dialogue
{
    public enum FlagWrite
    {
        Set,
        Add
    }

    [Name("Set Flag")]
    [Category("LD60")]
    [Description("Writes a named flag on the dialogue host and continues at once.")]
    [NodeColor("C9A227")]
    [GraphTypeId("ld60.act.flag")]
    [Serializable]
    public class SetFlagAction : GraphAction<DialogueFlags>
    {
        [Tooltip("Name of the flag to write, e.g. act, met.pioneer10, track.taken.")]
        [SerializeField] private string _key = "";

        [Tooltip("Whether the value replaces the flag or is added to it.")]
        [SerializeField] private FlagWrite _mode = FlagWrite.Set;

        [Tooltip("Value written to the flag.")]
        [SerializeField] private int _value = 1;

        [NonSerialized] private string _summary;
        [NonSerialized] private string _summaryKey;
        [NonSerialized] private FlagWrite _summaryMode;
        [NonSerialized] private int _summaryValue;

        public override string Summary
        {
            get
            {
                if (_summary == null || _summaryKey != _key || _summaryMode != _mode || _summaryValue != _value)
                {
                    _summaryKey = _key;
                    _summaryMode = _mode;
                    _summaryValue = _value;
                    _summary = (string.IsNullOrEmpty(_key) ? "?" : _key) + (_mode == FlagWrite.Add ? " += " : " = ") + _value;
                }

                return _summary;
            }
        }

        public override string Validate()
        {
            if (string.IsNullOrEmpty(_key))
                return "The flag name is empty, so this action would write a flag nothing can read.";

            return null;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_key))
            {
                Debug.LogError($"{nameof(SetFlagAction)}: the flag name is empty in {nameof(OnExecute)}, " +
                               "so the action reports failure and the dialogue stops. " +
                               "Fix: name the flag on this node in the inspector panel.");
                EndAction(false);
                return;
            }

            if (_mode == FlagWrite.Add)
                Agent.Add(_key, _value);
            else
                Agent.Set(_key, _value);

            EndAction(true);
        }
    }
}
