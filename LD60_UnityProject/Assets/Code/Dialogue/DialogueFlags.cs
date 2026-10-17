using System;
using System.Collections.Generic;
using UnityEngine;

namespace LD60.Dialogue
{
    [Serializable]
    public class DialogueFlagSeed
    {
        [field: Tooltip("Name of the flag, the same string dialogue nodes read and write.")]
        [field: SerializeField] public string Key { get; private set; } = "";

        [field: Tooltip("Value this flag starts the run with.")]
        [field: SerializeField] public int Value { get; private set; }
    }

    public class DialogueFlags : MonoBehaviour
    {
        [SerializeField] private DialogueFlagSeed[] _initialFlags = new DialogueFlagSeed[0];

        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
        private readonly System.Text.StringBuilder _report = new System.Text.StringBuilder();

        private void Awake()
        {
            ResetToInitial();
        }

        public void ResetToInitial()
        {
            _values.Clear();
            for (int i = 0; i < _initialFlags.Length; i++)
            {
                DialogueFlagSeed seed = _initialFlags[i];
                if (seed == null || string.IsNullOrEmpty(seed.Key))
                    continue;

                _values[seed.Key] = seed.Value;
            }
        }

        public int Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{nameof(DialogueFlags)}: {nameof(key)} is empty in {nameof(Get)} on '{name}'. " +
                               "Flags are addressed by name, so an unnamed one can never be read. " +
                               "Fix: name the flag on the dialogue node that reads it.", this);
                return 0;
            }

            int value;
            if (_values.TryGetValue(key, out value))
                return value;

            return 0;
        }

        public void Set(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{nameof(DialogueFlags)}: {nameof(key)} is empty in {nameof(Set)} on '{name}'. " +
                               "Flags are addressed by name, so an unnamed one would be unreadable. " +
                               "Fix: name the flag on the dialogue node that writes it.", this);
                return;
            }

            _values[key] = value;
        }

        public void Add(string key, int delta)
        {
            Set(key, Get(key) + delta);
        }

        public string Describe()
        {
            if (_values.Count == 0)
                return "<none>";

            _report.Clear();
            foreach (KeyValuePair<string, int> pair in _values)
            {
                if (_report.Length > 0)
                    _report.Append(", ");

                _report.Append(pair.Key).Append('=').Append(pair.Value);
            }

            return _report.ToString();
        }
    }
}
