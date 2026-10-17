using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Shared helper for graph systems' Project-window creation menus: writes a
    /// template file produced by the real serializer, so template and format can
    /// never drift apart. The tool declares no menus of its own — each graph system
    /// adds a [MenuItem] that calls this with its graph type and file extension.
    /// </summary>
    public static class GraphAssetCreation
    {
        public static void CreateGraphAsset<T>(string fileName) where T : Graph
        {
            var graph = ScriptableObject.CreateInstance<T>();
            var json = GraphJsonSerializer.Serialize(graph);
            Object.DestroyImmediate(graph);
            ProjectWindowUtil.CreateAssetWithContent(fileName, json);
        }
    }
}
