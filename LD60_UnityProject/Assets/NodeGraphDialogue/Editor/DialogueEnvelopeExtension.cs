using NodeGraphAvr.Editor;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Persists the dialogue graph's declared actor list as the optional top-level
    /// "actors" array of guid+name objects, omitted when empty so files without
    /// actors stay byte-identical.
    /// </summary>
    [GraphEnvelopeExtension(typeof(DialogueGraph))]
    public class DialogueEnvelopeExtension : GraphEnvelopeExtension
    {
        public override void WriteExtras(JsonWriter writer, Graph graph)
        {
            var dialogueGraph = (DialogueGraph)graph;
            if (dialogueGraph.Actors.Count == 0)
                return;

            writer.BeginArray("actors");
            for (var i = 0; i < dialogueGraph.Actors.Count; i++)
            {
                var actor = dialogueGraph.Actors[i];
                if (actor == null)
                    writer.ValueNull();
                else
                    GraphFieldMapper.WriteAssetReferenceValue(writer, actor, $"actor {i} on '{graph.name}'");
            }

            writer.EndArray();
        }

        public override void ReadExtras(JsonValue root, Graph graph)
        {
            var actorList = ((DialogueGraph)graph).ActorListInternal;
            actorList.Clear();
            var actorsJson = root.GetArray("actors");
            if (actorsJson == null)
                return;

            for (var i = 0; i < actorsJson.Count; i++)
                actorList.Add((ActorData)GraphFieldMapper.ReadAssetReferenceValue(typeof(ActorData), actorsJson[i],
                    $"actors entry {i}"));
        }
    }
}
