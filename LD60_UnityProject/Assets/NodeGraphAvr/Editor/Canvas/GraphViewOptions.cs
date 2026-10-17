using UnityEditor;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Canvas view toggles of the graph window — what the canvas draws, not what the
    /// graph holds. Values live in EditorPrefs and are cached in memory, because the
    /// draw path reads them on every repaint.
    /// </summary>
    public static class GraphViewOptions
    {
        private const string ShowConnectionLabelsKey = "NodeGraphAvr.View.ShowConnectionLabels";
        private const bool ShowConnectionLabelsDefault = true;

        private static bool _loaded;
        private static bool _showConnectionLabels = ShowConnectionLabelsDefault;

        public static bool ShowConnectionLabels
        {
            get
            {
                Load();
                return _showConnectionLabels;
            }
            set
            {
                Load();
                if (_showConnectionLabels == value)
                    return;

                _showConnectionLabels = value;
                EditorPrefs.SetBool(ShowConnectionLabelsKey, value);
            }
        }

        internal static void Reload()
        {
            _loaded = false;
        }

        private static void Load()
        {
            if (_loaded)
                return;

            _loaded = true;
            _showConnectionLabels = EditorPrefs.GetBool(ShowConnectionLabelsKey, ShowConnectionLabelsDefault);
        }
    }
}
