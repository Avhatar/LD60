using System.IO;
using UnityEditor.AssetImporters;

namespace NodeGraphAvr.Editor
{
    public abstract class GraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var json = File.ReadAllText(ctx.assetPath);
            var graph = GraphJsonSerializer.Deserialize(json);
            if (graph == null)
            {
                ctx.LogImportError($"Failed to import graph '{ctx.assetPath}'. " +
                                   "See the preceding error for the exact reason (malformed JSON or unknown type ids). " +
                                   "Fix the file content or restore the missing types, then reimport.");
                return;
            }

            graph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("graph", graph);
            ctx.SetMainObject(graph);
        }
    }
}
