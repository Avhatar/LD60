using UnityEditor;
using UnityEditor.Callbacks;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Routes double-clicking an imported graph asset to the graph editor window.
    /// </summary>
    public static class GraphOpenAssetHandler
    {
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is Graph graph)
            {
                GraphEditorWindow.Open(graph);
                return true;
            }

            return false;
        }
    }
}
