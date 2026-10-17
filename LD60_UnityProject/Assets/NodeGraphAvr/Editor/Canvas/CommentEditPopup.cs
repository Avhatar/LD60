using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Small popup for quick-editing a node's comment from its context menu. The edit
    /// is applied once, on close, through the graph's mutation API — one undo step per
    /// editing session, applied whether the popup is confirmed or clicked away.
    /// </summary>
    public class CommentEditPopup : PopupWindowContent
    {
        private readonly Graph _graph;
        private readonly Node _node;
        private string _text;

        public CommentEditPopup(Graph graph, Node node)
        {
            _graph = graph;
            _node = node;
            _text = node.Comment ?? "";
        }

        public override Vector2 GetWindowSize() => new Vector2(280f, 120f);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Comment", EditorStyles.boldLabel);
            _text = EditorGUILayout.TextArea(_text, GUILayout.ExpandHeight(true));
            if (GUILayout.Button("Apply"))
                editorWindow.Close();
        }

        public override void OnClose()
        {
            if (_text != (_node.Comment ?? ""))
                _graph.SetComment(_node, _text);
        }
    }
}
