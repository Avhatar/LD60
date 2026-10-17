using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public class GraphInspectorPanel
    {
        public const float DefaultWidth = 260f;
        public const float MinWidth = 180f;

        private const float MinLabelWidth = 80f;
        private const float MaxLabelWidth = 240f;
        private const float LabelWidthFraction = 0.45f;
        private const float CommentMinHeight = 48f;
        private static GUIStyle _commentAreaStyle;

        private static GUIStyle CommentAreaStyle =>
            _commentAreaStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };

        private static readonly Color _backgroundColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color _borderColor = new Color(0f, 0f, 0f, 0.5f);

        private SerializedObject _serializedObject;
        private Vector2 _scroll;

        public void OnGUI(Rect rect, Graph graph, CanvasSelection selection)
        {
            EditorGUI.DrawRect(rect, _backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), _borderColor);

            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 14f, rect.height - 12f));
            _scroll = GUILayout.BeginScrollView(_scroll);
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp((rect.width - 14f) * LabelWidthFraction, MinLabelWidth, MaxLabelWidth);

            if (selection.Nodes.Count == 1 && selection.Connections.Count == 0 && selection.Groups.Count == 0)
                DrawNode(graph, selection.Nodes[0]);
            else if (selection.Connections.Count == 1 && selection.Nodes.Count == 0 && selection.Groups.Count == 0)
                DrawConnection(graph, selection.Connections[0]);
            else if (selection.Groups.Count == 1 && selection.Nodes.Count == 0 && selection.Connections.Count == 0)
                DrawGroup(graph, selection.Groups[0]);
            else if (selection.Count > 1)
                GUILayout.Label(selection.Count + " elements selected", EditorStyles.centeredGreyMiniLabel);
            else
                DrawGraph(graph);

            EditorGUIUtility.labelWidth = previousLabelWidth;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void Dispose()
        {
            if (_serializedObject != null)
                _serializedObject.Dispose();
            _serializedObject = null;
        }

        private void DrawNode(Graph graph, Node node)
        {
            var index = IndexOf(graph, node);
            if (index < 0)
                return;

            GUILayout.Label(node.Name, EditorStyles.boldLabel);
            GUILayout.Label(node.GetType().Name, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            EnsureSerializedObject(graph);
            _serializedObject.Update();
            var nodeProperty = _serializedObject.FindProperty("_nodes").GetArrayElementAtIndex(index);
            if (nodeProperty.managedReferenceValue == null)
                return;

            var nameProperty = nodeProperty.FindPropertyRelative("_customName");
            EditorGUILayout.PropertyField(nameProperty, _nameLabel);

            var commentProperty = nodeProperty.FindPropertyRelative("_comment");
            GUILayout.Label("Comment", EditorStyles.miniBoldLabel);
            commentProperty.stringValue = EditorGUILayout.TextArea(commentProperty.stringValue, CommentAreaStyle, GUILayout.MinHeight(CommentMinHeight));

            DrawNodeDataFields(nodeProperty);
            if (_serializedObject.ApplyModifiedProperties())
                Undo.FlushUndoRecordObjects();
        }

        private static void DrawNodeDataFields(SerializedProperty nodeProperty)
        {
            var drewHeader = false;
            var iterator = nodeProperty.Copy();
            var end = iterator.GetEndProperty();
            if (!iterator.NextVisible(true))
                return;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                if (!IsHiddenNodeField(iterator.name))
                {
                    if (!drewHeader)
                    {
                        EditorGUILayout.Space(6f);
                        GUILayout.Label("Fields", EditorStyles.miniBoldLabel);
                        drewHeader = true;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }

                if (!iterator.NextVisible(false))
                    break;
            }
        }

        private static bool IsHiddenNodeField(string fieldName)
        {
            return fieldName == "_uid" || fieldName == "_position" || fieldName == "_outConnections" ||
                   fieldName == "_customName" || fieldName == "_comment";
        }

        private void DrawGroup(Graph graph, NodeGroup group)
        {
            var index = IndexOf(graph, group);
            if (index < 0)
                return;

            GUILayout.Label(string.IsNullOrEmpty(group.Title) ? "Group" : group.Title, EditorStyles.boldLabel);
            GUILayout.Label("Group", EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            EnsureSerializedObject(graph);
            _serializedObject.Update();
            var groupProperty = _serializedObject.FindProperty("_groups").GetArrayElementAtIndex(index);

            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("_title"), _titleLabel);
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("_color"), _colorLabel);
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("_commentPlacement"), _commentPlacementLabel);
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative("_commentColor"), _commentColorLabel);

            var commentProperty = groupProperty.FindPropertyRelative("_comment");
            GUILayout.Label("Comment", EditorStyles.miniBoldLabel);
            commentProperty.stringValue = EditorGUILayout.TextArea(commentProperty.stringValue, CommentAreaStyle, GUILayout.MinHeight(CommentMinHeight));

            if (_serializedObject.ApplyModifiedProperties())
                Undo.FlushUndoRecordObjects();
        }

        private static int IndexOf(Graph graph, NodeGroup group)
        {
            var groups = graph.Groups;
            for (var i = 0; i < groups.Count; i++)
                if (ReferenceEquals(groups[i], group))
                    return i;
            return -1;
        }

        private void DrawConnection(Graph graph, Connection connection)
        {
            GUILayout.Label("Connection", EditorStyles.boldLabel);
            GUILayout.Label(connection.GetType().Name, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
            DrawConnectionExtras(graph, connection);
            EditorGUILayout.LabelField("Source", connection.Source != null ? connection.Source.Name : "<none>");
            EditorGUILayout.LabelField("Target", connection.Target != null ? connection.Target.Name : "<none>");
            EditorGUILayout.LabelField("Uid", connection.Uid, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
            GUILayout.Label("Delete removes this connection.", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawConnectionExtras(Graph graph, Connection connection)
        {
            var inspector = ConnectionInspectorRegistry.Resolve(connection.Source?.GetType());
            if (inspector == null)
                return;

            EnsureSerializedObject(graph);
            _serializedObject.Update();
            inspector.OnGUI(_serializedObject, connection);
            if (_serializedObject.ApplyModifiedProperties())
                Undo.FlushUndoRecordObjects();
            EditorGUILayout.Space(4f);
        }

        private void DrawGraph(Graph graph)
        {
            GUILayout.Label(graph.name, EditorStyles.boldLabel);
            GUILayout.Label(graph.GetType().Name, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Nodes", graph.Nodes.Count.ToString());
            EditorGUILayout.LabelField("Format", graph.FormatVersion.ToString());
            DrawGraphDataFields(graph);
            EditorGUILayout.Space(4f);
            GUILayout.Label("Select a node or a connection.", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawGraphDataFields(Graph graph)
        {
            EnsureSerializedObject(graph);
            _serializedObject.Update();
            var drewHeader = false;
            var iterator = _serializedObject.GetIterator();
            if (!iterator.NextVisible(true))
                return;

            do
            {
                if (IsHiddenGraphField(iterator.name))
                    continue;

                if (!drewHeader)
                {
                    EditorGUILayout.Space(6f);
                    drewHeader = true;
                }

                EditorGUILayout.PropertyField(iterator, true);
            } while (iterator.NextVisible(false));

            if (_serializedObject.ApplyModifiedProperties())
                Undo.FlushUndoRecordObjects();
        }

        private static bool IsHiddenGraphField(string fieldName)
        {
            return fieldName == "m_Script" || fieldName == "_nodes" || fieldName == "_groups" ||
                   fieldName == "_translation" || fieldName == "_zoomFactor" || fieldName == "_formatVersion";
        }

        private void EnsureSerializedObject(Graph graph)
        {
            if (_serializedObject != null && _serializedObject.targetObject == graph)
                return;

            Dispose();
            _serializedObject = new SerializedObject(graph);
        }

        private static int IndexOf(Graph graph, Node node)
        {
            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], node))
                    return i;
            return -1;
        }

        private static readonly GUIContent _nameLabel = new GUIContent("Name", "Optional display name overriding the type-derived one.");
        private static readonly GUIContent _titleLabel = new GUIContent("Title", "Title shown in the group's header on the canvas.");
        private static readonly GUIContent _colorLabel = new GUIContent("Color", "Tint of the group frame. Alpha scales the frame's overall opacity.");
        private static readonly GUIContent _commentPlacementLabel = new GUIContent("Comment Placement",
            "Where the comment is rendered next to the frame on the canvas. None keeps it as a hover tooltip only.");
        private static readonly GUIContent _commentColorLabel = new GUIContent("Comment Color",
            "Text color of the comment when it is rendered next to the frame.");
    }
}
