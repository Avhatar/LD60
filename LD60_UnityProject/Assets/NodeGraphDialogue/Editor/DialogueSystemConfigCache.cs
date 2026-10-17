using UnityEditor;

namespace NodeGraphAvr.Dialogue.Editor
{
    public static class DialogueSystemConfigCache
    {
        private static DialogueSystemConfig _config;
        private static bool _searched;

        public static DialogueSystemConfig Config
        {
            get
            {
                if (_searched)
                    return _config;

                _searched = true;
                var guids = AssetDatabase.FindAssets("t:" + nameof(DialogueSystemConfig));
                if (guids.Length > 0)
                    _config = AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
                return _config;
            }
        }
    }
}
