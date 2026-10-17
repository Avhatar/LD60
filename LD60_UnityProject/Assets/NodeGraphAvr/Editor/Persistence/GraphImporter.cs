using System.IO;
using UnityEditor.AssetImporters;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Base importer turning a graph JSON file into an imported Graph object. The file
    /// on disk is the source of truth; the imported object is the read-only artifact
    /// the rest of the project references. Each graph system subclasses this and
    /// declares its own file extension with [ScriptedImporter] — the tool itself
    /// claims none. Malformed files fail the import with an actionable error instead
    /// of producing a half-built graph.
    /// </summary>
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
