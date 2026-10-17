using System;
using UnityEngine;

namespace LD60
{
    public static class Localization
    {
        private const string StoredLanguageKey = "LD60.Language";

        private static Language _current = ReadStoredLanguage();

        public static event Action Changed;

        public static Language Current
        {
            get { return _current; }
        }

        public static void SetLanguage(Language language)
        {
            if (_current == language)
                return;

            _current = language;
            PlayerPrefs.SetInt(StoredLanguageKey, (int)language);
            PlayerPrefs.Save();

            if (Changed != null)
                Changed.Invoke();
        }

        public static void Toggle()
        {
            SetLanguage(_current == Language.Ru ? Language.En : Language.Ru);
        }

        private static Language ReadStoredLanguage()
        {
            return PlayerPrefs.GetInt(StoredLanguageKey, (int)Language.Ru) == (int)Language.En
                ? Language.En
                : Language.Ru;
        }
    }
}
