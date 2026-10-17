using NodeGraphAvr.Editor;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Edits the choice option carried by a selected out-connection of a ChoiceNode: its
    /// text and the condition deciding whether it is offered. Everything is written through
    /// the node's options array at the property path of the option owning the line, so the
    /// panel's apply/flush cycle records undo and dirties the session. What happens when
    /// the option is picked is authored as Action nodes on its line, not here.
    /// </summary>
    [CustomConnectionInspector(typeof(ChoiceNode))]
    public class ChoiceConnectionInspector : ConnectionInspector
    {
        private const float TextMinHeight = 48f;
        private static GUIStyle _textAreaStyle;

        private static readonly GUIContent _visibleIfLabel = new GUIContent("Visible If",
            "Optional check deciding whether this option is offered at all.");

        public override void OnGUI(SerializedObject graphObject, Connection connection)
        {
            var choice = (ChoiceNode)connection.Source;
            var graph = (Graph)graphObject.targetObject;

            var optionIndex = choice.FindOptionIndexForLine(connection);
            var nodeIndex = -1;
            for (var i = 0; i < graph.Nodes.Count; i++)
                if (ReferenceEquals(graph.Nodes[i], choice))
                    nodeIndex = i;
            if (optionIndex < 0 || optionIndex >= choice.Options.Count || nodeIndex < 0)
                return;

            _textAreaStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            var optionProperty = graphObject.FindProperty("_nodes").GetArrayElementAtIndex(nodeIndex)
                .FindPropertyRelative("_options").GetArrayElementAtIndex(optionIndex);
            var textProperty = optionProperty.FindPropertyRelative("_text");

            GUILayout.Label($"Option {optionIndex + 1} (of {choice.Name})", EditorStyles.miniBoldLabel);
            textProperty.stringValue = EditorGUILayout.TextArea(textProperty.stringValue, _textAreaStyle, GUILayout.MinHeight(TextMinHeight));

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(optionProperty.FindPropertyRelative("_visibleIf"), _visibleIfLabel, true);
        }
    }
}
