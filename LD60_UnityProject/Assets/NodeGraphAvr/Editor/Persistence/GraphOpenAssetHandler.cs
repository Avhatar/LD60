using UnityEditor;
using UnityEditor.Callbacks;

namespace NodeGraphAvr.Editor
{
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
