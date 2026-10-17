using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
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
