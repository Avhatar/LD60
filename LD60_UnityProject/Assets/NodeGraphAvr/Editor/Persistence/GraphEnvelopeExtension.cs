namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Extension point letting a graph system persist kind-specific top-level data in
    /// the JSON envelope without the serializer knowing the kind. WriteExtras runs
    /// right after the "view" block; ReadExtras receives the whole document root and
    /// reads its own keys. Extensions must keep the determinism rules: fixed key
    /// order, optional keys omitted at defaults.
    /// </summary>
    public abstract class GraphEnvelopeExtension
    {
        public abstract void WriteExtras(JsonWriter writer, Graph graph);

        public abstract void ReadExtras(JsonValue root, Graph graph);
    }
}
