using System;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// A titled rectangular frame on the graph canvas. Purely spatial authoring data:
    /// it stores no node membership — the editor moves whatever nodes are inside the
    /// rect when the frame is dragged. Persisted as part of its Graph.
    /// </summary>
    [Serializable]
    public class NodeGroup
    {
        public static readonly Color DefaultColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        public static readonly Color DefaultCommentColor = new Color(0.75f, 0.75f, 0.6f, 1f);

        [Tooltip("Stable identity of the group inside its graph file.")]
        [SerializeField] private string _uid;

        [Tooltip("Title shown in the group's header on the canvas.")]
        [SerializeField] private string _title;

        [Tooltip("Author comment for the group. Shown as a tooltip when hovering the frame on the canvas.")]
        [SerializeField] private string _comment;

        [Tooltip("Where the comment is rendered next to the frame. None keeps it as a hover tooltip only.")]
        [SerializeField] private GroupCommentPlacement _commentPlacement = GroupCommentPlacement.None;

        [Tooltip("Text color of the comment when it is rendered next to the frame.")]
        [SerializeField] private Color _commentColor = DefaultCommentColor;

        [Tooltip("Tint of the group frame. Alpha scales the frame's overall opacity.")]
        [SerializeField] private Color _color = DefaultColor;

        [Tooltip("Canvas rectangle of the group frame. Authoring data only.")]
        [SerializeField] private Rect _rect;

        public string Uid => string.IsNullOrEmpty(_uid) ? _uid = Guid.NewGuid().ToString("N") : _uid;

        public string Title
        {
            get { return _title; }
            internal set { _title = value; }
        }

        public string Comment
        {
            get { return _comment; }
            internal set { _comment = value; }
        }

        public GroupCommentPlacement CommentPlacement
        {
            get { return _commentPlacement; }
            internal set { _commentPlacement = value; }
        }

        public Color CommentColor
        {
            get { return _commentColor; }
            internal set { _commentColor = value; }
        }

        public Color Color
        {
            get { return _color; }
            internal set { _color = value; }
        }

        public Rect Rect
        {
            get { return _rect; }
            internal set { _rect = value; }
        }

        internal void RestorePersistentState(string uid, string title, string comment, Color color, Rect rect,
            GroupCommentPlacement commentPlacement, Color commentColor)
        {
            _uid = uid;
            _title = title;
            _comment = comment;
            _color = color;
            _rect = rect;
            _commentPlacement = commentPlacement;
            _commentColor = commentColor;
        }
    }
}
