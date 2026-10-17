using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Serializes a Graph to the deterministic on-disk JSON format and back.
    /// Envelope: version, kind id, view state, then nodes in list order — each node
    /// carrying uid, type id, rounded position, optional name/comment/data, and its
    /// out-connections inline for diff locality. Deserialization failures are logged
    /// with actionable detail and yield null, never a half-built graph.
    /// </summary>
    public static class GraphJsonSerializer
    {
        public const int CurrentFormatVersion = 1;

        public static string Serialize(Graph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph), $"{nameof(GraphJsonSerializer)}: cannot serialize a null graph.");

            var writer = new JsonWriter();
            writer.BeginObject();
            writer.Property("version", (long)CurrentFormatVersion);
            writer.Property("kind", GraphTypeRegistry.GetIdOrThrow(graph.GetType()));
            writer.BeginObject("view");
            writer.PropertyInlineInts("pan", Mathf.RoundToInt(graph.Translation.x), Mathf.RoundToInt(graph.Translation.y));
            writer.Property("zoom", Math.Round(graph.ZoomFactor, 2));
            writer.EndObject();
            GraphEnvelopeExtensionRegistry.Resolve(graph.GetType())?.WriteExtras(writer, graph);

            if (graph.Groups.Count > 0)
            {
                writer.BeginArray("groups");
                for (var i = 0; i < graph.Groups.Count; i++)
                {
                    var group = graph.Groups[i];
                    writer.BeginObject();
                    writer.Property("uid", group.Uid);
                    writer.Property("title", group.Title ?? "");
                    if (!string.IsNullOrEmpty(group.Comment))
                        writer.Property("comment", group.Comment);
                    if (group.CommentPlacement != GroupCommentPlacement.None)
                        writer.Property("commentPlacement", group.CommentPlacement.ToString());
                    if (group.CommentColor != NodeGroup.DefaultCommentColor)
                        writer.Property("commentColor", "#" + ColorUtility.ToHtmlStringRGBA(group.CommentColor));
                    if (group.Color != NodeGroup.DefaultColor)
                        writer.Property("color", "#" + ColorUtility.ToHtmlStringRGBA(group.Color));
                    writer.PropertyInlineInts("rect",
                        Mathf.RoundToInt(group.Rect.x), Mathf.RoundToInt(group.Rect.y),
                        Mathf.RoundToInt(group.Rect.width), Mathf.RoundToInt(group.Rect.height));
                    writer.EndObject();
                }

                writer.EndArray();
            }

            writer.BeginArray("nodes");
            for (var i = 0; i < graph.Nodes.Count; i++)
                WriteNode(writer, graph.Nodes[i]);
            writer.EndArray();
            writer.EndObject();
            return writer.ToString();
        }

        internal static void WriteNode(JsonWriter writer, Node node)
        {
            writer.BeginObject();
            writer.Property("uid", node.Uid);
            writer.Property("type", GraphTypeRegistry.GetIdOrThrow(node.GetType()));
            writer.PropertyInlineInts("position", Mathf.RoundToInt(node.Position.x), Mathf.RoundToInt(node.Position.y));
            if (node.Size != Node.DefaultSize)
                writer.PropertyInlineInts("size", Mathf.RoundToInt(node.Size.x), Mathf.RoundToInt(node.Size.y));
            if (!string.IsNullOrEmpty(node.CustomName))
                writer.Property("name", node.CustomName);
            if (!string.IsNullOrEmpty(node.Comment))
                writer.Property("comment", node.Comment);
            if (GraphFieldMapper.HasMappedFields(node.GetType()))
            {
                writer.BeginObject("data");
                GraphFieldMapper.WriteFields(writer, node);
                writer.EndObject();
            }

            if (node.OutConnections.Count > 0)
            {
                writer.BeginArray("out");
                for (var i = 0; i < node.OutConnections.Count; i++)
                {
                    var connection = node.OutConnections[i];
                    writer.BeginObject();
                    writer.Property("uid", connection.Uid);
                    writer.Property("target", connection.Target.Uid);
                    if (connection.Waypoints.Count > 0)
                    {
                        writer.BeginArray("points");
                        for (var j = 0; j < connection.Waypoints.Count; j++)
                            writer.ValueInlineInts(Mathf.RoundToInt(connection.Waypoints[j].x), Mathf.RoundToInt(connection.Waypoints[j].y));
                        writer.EndArray();
                    }

                    if (connection.SharpBends)
                        writer.Property("sharp", true);
                    writer.EndObject();
                }

                writer.EndArray();
            }

            writer.EndObject();
        }

        public static Graph Deserialize(string json, Graph target = null)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (JsonParseException exception)
            {
                Debug.LogError($"{nameof(GraphJsonSerializer)}: malformed graph JSON — {exception.Message}");
                return null;
            }

            if (root.Kind != JsonKind.Object)
            {
                Debug.LogError($"{nameof(GraphJsonSerializer)}: graph JSON root must be an object.");
                return null;
            }

            var kindId = root.GetString("kind");
            if (!GraphTypeRegistry.TryGetType(kindId, out var graphType) || !typeof(Graph).IsAssignableFrom(graphType))
            {
                Debug.LogError($"{nameof(GraphJsonSerializer)}: unknown graph kind id '{kindId}'. " +
                               "The graph type may have been removed or its [GraphTypeId] changed. Fix: restore the type or update the file.");
                return null;
            }

            Graph graph;
            var created = false;
            if (target != null)
            {
                if (target.GetType() != graphType)
                {
                    Debug.LogError($"{nameof(GraphJsonSerializer)}: kind id '{kindId}' maps to '{graphType.Name}' but the provided target is '{target.GetType().Name}'. " +
                                   "Fix: pass a target of the matching graph type or null.", target);
                    return null;
                }

                graph = target;
            }
            else
            {
                graph = (Graph)ScriptableObject.CreateInstance(graphType);
                created = true;
            }

            try
            {
                ReadInto(graph, root, kindId);
                return graph;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(GraphJsonSerializer)}: failed to deserialize graph — {exception.Message}");
                if (created)
                    UnityEngine.Object.DestroyImmediate(graph);
                return null;
            }
        }

        private static void ReadInto(Graph graph, JsonValue root, string kindId)
        {
            graph.FormatVersion = (int)root.GetNumber("version", CurrentFormatVersion);
            var view = root.Get("view");
            if (view != null)
            {
                var pan = view.GetArray("pan");
                if (pan != null && pan.Count == 2)
                    graph.Translation = new Vector2((float)pan[0].Number, (float)pan[1].Number);
                graph.ZoomFactor = (float)view.GetNumber("zoom", 1);
            }

            GraphEnvelopeExtensionRegistry.Resolve(graph.GetType())?.ReadExtras(root, graph);

            var groups = graph.GroupListInternal;
            groups.Clear();
            var groupsJson = root.GetArray("groups");
            if (groupsJson != null)
            {
                foreach (var groupJson in groupsJson)
                {
                    var rectJson = groupJson.GetArray("rect");
                    var rect = rectJson != null && rectJson.Count == 4
                        ? new Rect((float)rectJson[0].Number, (float)rectJson[1].Number, (float)rectJson[2].Number, (float)rectJson[3].Number)
                        : new Rect(0, 0, 200, 150);
                    var color = NodeGroup.DefaultColor;
                    var colorText = groupJson.GetString("color");
                    if (!string.IsNullOrEmpty(colorText) && !ColorUtility.TryParseHtmlString(colorText, out color))
                        color = NodeGroup.DefaultColor;
                    var placement = GroupCommentPlacement.None;
                    var placementText = groupJson.GetString("commentPlacement");
                    if (!string.IsNullOrEmpty(placementText) && !Enum.TryParse(placementText, out placement))
                        placement = GroupCommentPlacement.None;
                    var commentColor = NodeGroup.DefaultCommentColor;
                    var commentColorText = groupJson.GetString("commentColor");
                    if (!string.IsNullOrEmpty(commentColorText) && !ColorUtility.TryParseHtmlString(commentColorText, out commentColor))
                        commentColor = NodeGroup.DefaultCommentColor;
                    var group = new NodeGroup();
                    group.RestorePersistentState(groupJson.GetString("uid"), groupJson.GetString("title", ""),
                        groupJson.GetString("comment", ""), color, rect, placement, commentColor);
                    groups.Add(group);
                }
            }

            var nodesJson = root.GetArray("nodes") ?? new List<JsonValue>();
            var nodes = graph.NodeListInternal;
            nodes.Clear();
            var byUid = new Dictionary<string, Node>();

            foreach (var nodeJson in nodesJson)
            {
                var typeId = nodeJson.GetString("type");
                if (!GraphTypeRegistry.TryGetType(typeId, out var nodeType) || nodeType.IsAbstract || !graph.BaseNodeType.IsAssignableFrom(nodeType))
                    throw new FormatException($"unknown node type id '{typeId}' for graph kind '{kindId}'. " +
                                              "The node type may have been removed or its [GraphTypeId] changed. Fix: restore the type or update the file.");

                var uid = nodeJson.GetString("uid");
                if (string.IsNullOrEmpty(uid) || byUid.ContainsKey(uid))
                    throw new FormatException($"missing or duplicate node uid '{uid}'.");

                var node = (Node)Activator.CreateInstance(nodeType);
                var positionJson = nodeJson.GetArray("position");
                var position = positionJson != null && positionJson.Count == 2
                    ? new Vector2((float)positionJson[0].Number, (float)positionJson[1].Number)
                    : Vector2.zero;
                var sizeJson = nodeJson.GetArray("size");
                var size = sizeJson != null && sizeJson.Count == 2
                    ? new Vector2((float)sizeJson[0].Number, (float)sizeJson[1].Number)
                    : Node.DefaultSize;
                node.RestorePersistentState(uid, position, size, nodeJson.GetString("comment", ""), nodeJson.GetString("name", ""));
                node.Graph = graph;
                var data = nodeJson.Get("data");
                if (data != null)
                    GraphFieldMapper.ReadFields(data, node);
                nodes.Add(node);
                byUid.Add(uid, node);
            }

            for (var i = 0; i < nodesJson.Count; i++)
            {
                var node = nodes[i];
                var outsJson = nodesJson[i].GetArray("out");
                if (outsJson == null)
                    continue;
                foreach (var connectionJson in outsJson)
                {
                    var targetUid = connectionJson.GetString("target");
                    if (string.IsNullOrEmpty(targetUid) || !byUid.TryGetValue(targetUid, out var targetNode))
                        throw new FormatException($"connection on node '{node.Uid}' points at unknown target uid '{targetUid}'.");

                    var connection = (Connection)Activator.CreateInstance(node.OutConnectionType);
                    connection.RestoreUid(connectionJson.GetString("uid"));
                    connection.Source = node;
                    connection.Target = targetNode;
                    var pointsJson = connectionJson.GetArray("points");
                    if (pointsJson != null)
                        foreach (var pointJson in pointsJson)
                            if (pointJson.Kind == JsonKind.Array && pointJson.Array.Count == 2)
                                connection.WaypointList.Add(new Vector2((float)pointJson.Array[0].Number, (float)pointJson.Array[1].Number));
                    connection.SharpBends = connectionJson.GetBool("sharp");
                    node.OutList.Add(connection);
                }
            }

            graph.RebuildRuntimeState();
        }
    }
}
