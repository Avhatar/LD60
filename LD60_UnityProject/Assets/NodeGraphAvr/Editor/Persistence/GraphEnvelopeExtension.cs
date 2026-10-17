namespace NodeGraphAvr.Editor
{
    public abstract class GraphEnvelopeExtension
    {
        public abstract void WriteExtras(JsonWriter writer, Graph graph);

        public abstract void ReadExtras(JsonValue root, Graph graph);
    }
}
