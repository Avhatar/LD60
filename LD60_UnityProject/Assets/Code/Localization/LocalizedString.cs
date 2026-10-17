using System;
using UnityEngine;

namespace LD60
{
    [Serializable]
    public class LocalizedString
    {
        [SerializeField] private string _ru = "";
        [SerializeField] private string _en = "";

        public LocalizedString()
        {
        }

        public LocalizedString(string ru, string en)
        {
            _ru = ru == null ? "" : ru;
            _en = en == null ? "" : en;
        }

        public string Value
        {
            get { return Get(Localization.Current); }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(_ru) && string.IsNullOrEmpty(_en); }
        }

        public string Get(Language language)
        {
            if (language == Language.En)
                return string.IsNullOrEmpty(_en) ? _ru : _en;

            return string.IsNullOrEmpty(_ru) ? _en : _ru;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
