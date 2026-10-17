using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Copy/paste buffer for canvas nodes: serializes a node selection to a small
    /// JSON payload (same node-block format as the on-disk files) and pastes it back
    /// as fresh clones — new uids, fields and comments preserved, connections kept
    /// only between copied nodes, waypoints translated with the nodes. Every paste
    /// mutation goes through the graph's mutation API, so one paste = one undo step.
    /// </summary>
    public static class GraphClipboard
    {
        private const string Marker = "clipboard";

        public static string Copy(Graph graph, IReadOnlyList<Node> nodes)
        {
            if (graph == null || nodes == null || nodes.Count == 0)
            {
                Debug.LogError($"{nameof(GraphClipboard)}: nothing to copy in {nameof(Copy)} — a non-null graph and a non-empty node list are required. " +
                               "Fix: select at least one node before copying.");
                return null;
            }

            var writer = new JsonWriter();
            writer.BeginObject();
            writer.Property(Marker, 1L);
            writer.Property("kind", GraphTypeRegistry.GetIdOrThrow(graph.GetType()));
            writer.BeginArray("nodes");
            for (var i = 0; i < nodes.Count; i++)
                if (nodes[i] != null)
                    GraphJsonSerializer.WriteNode(writer, nodes[i]);
            writer.EndArray();
            writer.EndObject();
            return writer.ToString();
        }

        public static bool LooksLikeClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var trimmed = text.TrimStart();
            return trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.Contains("\"" + Marker + "\"");
        }

        public static List<Node> Paste(Graph graph, string json, Vector2 anchor)
        {
            var pasted = new List<Node>();
            if (graph == null)
            {
                Debug.LogError($"{nameof(GraphClipboard)}: {nameof(graph)} is null in {nameof(Paste)}. " +
                               "A paste target graph is required. Fix: open a graph in the window before pasting.");
                return pasted;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (JsonParseException exception)
            {
                Debug.LogError($"{nameof(GraphClipboard)}: malformed clipboard JSON — {exception.Message}");
                return pasted;
            }

            if (root.Kind != JsonKind.Object || root.GetNumber(Marker, 0) == 0)
            {
                Debug.LogError($"{nameof(GraphClipboard)}: the clipboard text is not a node clipboard payload. " +
                               "Fix: copy nodes from a graph window first.");
                return pasted;
            }

            var graphKind = GraphTypeRegistry.GetIdOrThrow(graph.GetType());
            var payloadKind = root.GetString("kind");
            if (payloadKind != graphKind)
            {
                Debug.LogError($"{nameof(GraphClipboard)}: the clipboard holds nodes of graph kind '{payloadKind}' but the target graph is '{graphKind}'. " +
                               "Fix: paste into a graph of the same kind the nodes were copied from.");
                return pasted;
            }

            var nodesJson = root.GetArray("nodes");
            if (nodesJson == null || nodesJson.Count == 0)
                return pasted;

            var nodeTypes = new Type[nodesJson.Count];
            for (var i = 0; i < nodesJson.Count; i++)
            {
                var typeId = nodesJson[i].GetString("type");
                if (!GraphTypeRegistry.TryGetType(typeId, out var nodeType) || nodeType.IsAbstract || !graph.BaseNodeType.IsAssignableFrom(nodeType))
                {
                    Debug.LogError($"{nameof(GraphClipboard)}: unknown or incompatible node type id '{typeId}' for graph kind '{graphKind}'. " +
                                   "Nothing was pasted. Fix: copy nodes from a graph of the same kind.");
                    return pasted;
                }

                nodeTypes[i] = nodeType;
            }

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < nodesJson.Count; i++)
            {
                var position = ReadPosition(nodesJson[i]);
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var offset = anchor - (min + max) * 0.5f;

            var byOldUid = new Dictionary<string, Node>();
            for (var i = 0; i < nodesJson.Count; i++)
            {
                var nodeJson = nodesJson[i];
                var node = graph.AddNode(nodeTypes[i], ReadPosition(nodeJson) + offset);
                if (node == null)
                    continue;

                node.RestorePersistentState(node.Uid, node.Position, ReadSize(nodeJson),
                    nodeJson.GetString("comment", ""), nodeJson.GetString("name", ""));
                var data = nodeJson.Get("data");
                if (data != null)
                    GraphFieldMapper.ReadFields(data, node);
                var oldUid = nodeJson.GetString("uid");
                if (!string.IsNullOrEmpty(oldUid) && !byOldUid.ContainsKey(oldUid))
                    byOldUid.Add(oldUid, node);
                pasted.Add(node);
            }

            for (var i = 0; i < nodesJson.Count; i++)
            {
                var outsJson = nodesJson[i].GetArray("out");
                if (outsJson == null || !byOldUid.TryGetValue(nodesJson[i].GetString("uid"), out var source))
                    continue;

                foreach (var connectionJson in outsJson)
                {
                    var targetUid = connectionJson.GetString("target");
                    if (string.IsNullOrEmpty(targetUid) || !byOldUid.TryGetValue(targetUid, out var target))
                        continue;

                    var connection = graph.Connect(source, target);
                    if (connection == null)
                        continue;

                    var pointsJson = connectionJson.GetArray("points");
                    if (pointsJson != null)
                        for (var p = 0; p < pointsJson.Count; p++)
                            if (pointsJson[p].Kind == JsonKind.Array && pointsJson[p].Array.Count == 2)
                                graph.AddConnectionPoint(connection, p,
                                    new Vector2((float)pointsJson[p].Array[0].Number, (float)pointsJson[p].Array[1].Number) + offset);
                    if (connectionJson.GetBool("sharp"))
                        graph.SetSharpBends(connection, true);
                }
            }

            return pasted;
        }

        private static Vector2 ReadPosition(JsonValue nodeJson)
        {
            var positionJson = nodeJson.GetArray("position");
            return positionJson != null && positionJson.Count == 2
                ? new Vector2((float)positionJson[0].Number, (float)positionJson[1].Number)
                : Vector2.zero;
        }

        private static Vector2 ReadSize(JsonValue nodeJson)
        {
            var sizeJson = nodeJson.GetArray("size");
            return sizeJson != null && sizeJson.Count == 2
                ? new Vector2((float)sizeJson[0].Number, (float)sizeJson[1].Number)
                : Node.DefaultSize;
        }
    }
}
