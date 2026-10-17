using System;
using NodeGraphAvr;
using UnityEngine;

namespace LD60.Dialogue
{
    public enum FlagComparison
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    }

    [Name("Flag")]
    [Category("LD60")]
    [Description("Compares a named flag of the dialogue host against a value. A flag that was never written reads as zero.")]
    [NodeColor("5FA98A")]
    [GraphTypeId("ld60.cond.flag")]
    [Serializable]
    public class FlagCondition : GraphCondition<DialogueFlags>
    {
        [Tooltip("Name of the flag to read, e.g. act, met.pioneer10, track.taken.")]
        [SerializeField] private string _key = "";

        [Tooltip("How the flag is compared against the value below.")]
        [SerializeField] private FlagComparison _comparison = FlagComparison.Equal;

        [Tooltip("Value the flag is compared against.")]
        [SerializeField] private int _value = 1;

        [NonSerialized] private string _summary;
        [NonSerialized] private string _summaryKey;
        [NonSerialized] private FlagComparison _summaryComparison;
        [NonSerialized] private int _summaryValue;

        public override string Summary
        {
            get
            {
                if (_summary == null || _summaryKey != _key || _summaryComparison != _comparison || _summaryValue != _value)
                {
                    _summaryKey = _key;
                    _summaryComparison = _comparison;
                    _summaryValue = _value;
                    _summary = (string.IsNullOrEmpty(_key) ? "?" : _key) + " " + OperatorText(_comparison) + " " + _value;
                }

                return _summary;
            }
        }

        public override string Validate()
        {
            if (string.IsNullOrEmpty(_key))
                return "The flag name is empty, so this check reads nothing and always compares against zero.";

            return null;
        }

        protected override bool OnEvaluate()
        {
            int current = Agent.Get(_key);
            switch (_comparison)
            {
                case FlagComparison.Equal:
                    return current == _value;
                case FlagComparison.NotEqual:
                    return current != _value;
                case FlagComparison.Less:
                    return current < _value;
                case FlagComparison.LessOrEqual:
                    return current <= _value;
                case FlagComparison.Greater:
                    return current > _value;
                case FlagComparison.GreaterOrEqual:
                    return current >= _value;
            }

            Debug.LogError($"{nameof(FlagCondition)}: unhandled comparison '{_comparison}' in {nameof(OnEvaluate)} for flag '{_key}'. " +
                           "The check fails so the option stays hidden. " +
                           $"Fix: handle the new value in {nameof(FlagComparison)} here.");
            return false;
        }

        private static string OperatorText(FlagComparison comparison)
        {
            switch (comparison)
            {
                case FlagComparison.NotEqual:
                    return "!=";
                case FlagComparison.Less:
                    return "<";
                case FlagComparison.LessOrEqual:
                    return "<=";
                case FlagComparison.Greater:
                    return ">";
                case FlagComparison.GreaterOrEqual:
                    return ">=";
            }

            return "==";
        }
    }
}
