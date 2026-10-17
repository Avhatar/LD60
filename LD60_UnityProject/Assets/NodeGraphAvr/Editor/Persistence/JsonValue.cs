using System.Collections.Generic;

namespace NodeGraphAvr.Editor
{
    public enum JsonKind
    {
        Object,
        Array,
        String,
        Number,
        Bool,
        Null
    }

    /// <summary>
    /// Minimal JSON DOM node produced by JsonParser: a tagged union of object, array,
    /// string, number, bool, and null. Read-side counterpart of JsonWriter; carries
    /// small typed accessors used by the graph deserializer.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonKind Kind;
        public Dictionary<string, JsonValue> Object;
        public List<JsonValue> Array;
        public string String;
        public double Number;
        public bool Bool;

        public JsonValue Get(string key)
        {
            if (Kind != JsonKind.Object)
                return null;
            return Object.TryGetValue(key, out var value) ? value : null;
        }

        public string GetString(string key, string fallback = null)
        {
            var value = Get(key);
            return value != null && value.Kind == JsonKind.String ? value.String : fallback;
        }

        public double GetNumber(string key, double fallback = 0)
        {
            var value = Get(key);
            return value != null && value.Kind == JsonKind.Number ? value.Number : fallback;
        }

        public List<JsonValue> GetArray(string key)
        {
            var value = Get(key);
            return value != null && value.Kind == JsonKind.Array ? value.Array : null;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var value = Get(key);
            return value != null && value.Kind == JsonKind.Bool ? value.Bool : fallback;
        }
    }
}
