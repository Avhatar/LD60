using System;

namespace LD60
{
    public static class Localization
    {
        private static Language _current = Language.En;

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

            if (Changed != null)
                Changed.Invoke();
        }

        public static void Toggle()
        {
            SetLanguage(_current == Language.Ru ? Language.En : Language.Ru);
        }
    }
}
