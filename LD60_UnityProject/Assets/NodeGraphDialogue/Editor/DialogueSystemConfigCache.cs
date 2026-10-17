using UnityEditor;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Locates the project's single DialogueSystemConfig asset once per domain and
    /// caches it for the inspector panel's speaker dropdown. A config created after
    /// the first lookup is picked up on the next domain reload — acceptable for an
    /// asset that is created once per project.
    /// </summary>
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
