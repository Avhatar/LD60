using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Base canvas view of one node, drawn as a header plus a body: the header is the node's
    /// name, the body is whatever the node type says it does — an action's name, a line of
    /// dialogue, the options of a branch. The two read apart: the body is shaded down and parted
    /// from the header by a line, so the tinted header stays a strip of its own. A node whose body
    /// text is empty draws neither, and centers its header instead, so an empty box never looks
    /// like a truncated one. The node's rect
    /// comes from the model (position and author-set size), never from layout, and the
    /// bottom-right corner resizes it. Views are pure presentation — every model change they
    /// trigger goes through the graph's mutation API via the owning window.
    /// </summary>
    public abstract class NodeView
    {
        internal const float ResizeHandleSize = 14f;

        private const float ResizeMarkDotSize = 2f;
        private const float ResizeMarkSpacing = 3f;
        private const int ResizeMarkDots = 3;
        private const float ResizeMarkMargin = 2f;
        private const float BodyPadding = 4f;
        private const float BodySidePadding = 8f;
        private const float BodyTextGap = 3f;

        /// <summary>
        /// How far a rounded corner's arc cuts in along the diagonal, as a fraction of the radius
        /// (1 - 1/sqrt(2)). The resize mark starts past that, so it never crosses the silhouette.
        /// </summary>
        private const float CornerDiagonalInset = 0.2929f;

        private static GUIStyle _bodyStyle;
        private static GUIStyle _emphasizedBodyStyle;

        private static readonly Color _resizeMarkColor = new Color(1f, 1f, 1f, 0.25f);

        public Node Node { get; private set; }

        public GUI.WindowFunction WindowFunction { get; private set; }

        public Rect LastRect { get; private set; }

        private GraphEditorWindow _owner;
        private string _cachedComment;
        private GUIContent _commentContent;
        private string _cachedBodyText;
        private GUIContent _bodyContent;
        private GraphAction _cachedAction;
        private string _actionName;

        internal void Init(Node node, GraphEditorWindow owner)
        {
            Node = node;
            _owner = owner;
            WindowFunction = DrawWindow;
            LastRect = new Rect(node.Position, node.Size);
        }

        internal void SetLastRect(Rect rect)
        {
            LastRect = rect;
        }

        /// <summary>
        /// Corner radius this node draws with. Zero — the default — is the square editor window
        /// style; a positive value swaps in a rounded window and a matching rounded outline.
        /// </summary>
        protected virtual int CornerRadius => 0;

        /// <summary>
        /// What this node does, in words, for the body. Null or empty means the node has nothing
        /// to say and its header is centered instead. Implementations must return a cached string
        /// — this is read every repaint.
        /// </summary>
        protected virtual string GetBodyText()
        {
            RefreshHostedAction();
            return _actionName;
        }

        /// <summary>True when the body is the name of a hosted action, which is drawn in bold.</summary>
        protected virtual bool IsBodyEmphasized => Node is IActionHost;

        internal int WindowCornerRadius => Mathf.Max(0, CornerRadius);

        internal GUIStyle WindowStyle => CanvasShapes.WindowStyle(WindowCornerRadius, !HasBody);

        internal bool HasBody => !string.IsNullOrEmpty(GetBodyText());

        public virtual string GetOutConnectionLabel(int index) => null;

        public virtual int GetOutConnectionSlot(int connectionIndex) => connectionIndex;

        /// <summary>
        /// True when the flow through this slot is gated by a condition, so the canvas can mark its
        /// port square. False by default — most nodes take every out-connection unconditionally.
        /// </summary>
        public virtual bool IsOutSlotConditional(int slot) => false;

        /// <summary>
        /// True when this view maps out-connections to slots that mean something to the node, so
        /// dragging a line's source end onto another slot of the same node is a real edit rather
        /// than a no-op relink.
        /// </summary>
        public virtual bool CanReassignOutConnectionSlot => false;

        /// <summary>
        /// Moves an existing out-connection to another slot of the same node. Called only when
        /// <see cref="CanReassignOutConnectionSlot"/> is true, and always after the window has
        /// notified the mutation guard.
        /// </summary>
        public virtual void SetOutConnectionSlot(Connection connection, int slot)
        {
        }

        public virtual void OnOutConnectionCreated(Connection connection, int slot)
        {
        }

        public virtual void OnOutConnectionRemoved(Connection connection)
        {
        }

        internal GUIContent GetCommentContent()
        {
            if (_cachedComment != Node.Comment)
            {
                _cachedComment = Node.Comment;
                _commentContent = string.IsNullOrEmpty(_cachedComment) ? null : new GUIContent(_cachedComment);
            }

            return _commentContent;
        }

        private void DrawWindow(int id)
        {
            var e = Event.current;
            var local = new Rect(0f, 0f, LastRect.width, LastRect.height);
            HandleWindowEvents(e);
            if (e.type == EventType.Repaint && HasBody)
                CanvasShapes.DrawNodeBody(BodyRect(local), WindowCornerRadius);
            DrawBody(local);
            DrawResizeHandle(local, e);
            HandleDrag(e);
        }

        /// <summary>
        /// The body area of the node: everything below the header strip, full width. It never
        /// starts above the corner radius, so its straight top edge cannot poke out of a rounded
        /// silhouette.
        /// </summary>
        protected Rect BodyRect(Rect localRect)
        {
            var top = Mathf.Max(CanvasShapes.NodeHeaderHeight, WindowCornerRadius);
            return new Rect(0f, top, localRect.width, localRect.height - top);
        }

        /// <summary>
        /// Draws the body text clipped to the space the author gave the node: growing the node
        /// shows more of it, shrinking shows less. Overridden only by views that draw something
        /// other than text.
        /// </summary>
        protected virtual void DrawBody(Rect localRect)
        {
            var text = GetBodyText();
            if (string.IsNullOrEmpty(text))
                return;

            if (!ReferenceEquals(text, _cachedBodyText))
            {
                _cachedBodyText = text;
                _bodyContent = new GUIContent(text);
            }

            var style = IsBodyEmphasized ? EmphasizedBodyStyle : BodyStyle;
            var body = BodyRect(localRect);
            body.xMin += BodySidePadding;
            body.xMax -= BodySidePadding;
            body.yMin += BodyTextGap;
            body.yMax -= BodyPadding;
            if (body.width <= 0f || body.height <= 0f)
                return;

            GUI.Label(body, _bodyContent, style);
        }

        /// <summary>
        /// The grab area of the resize corner inside a box — the node's local rect while drawing,
        /// the node's canvas rect when the window registers the mouse cursor for it. One definition
        /// so the drawn mark, the hit test and the cursor always agree.
        /// </summary>
        internal static Rect ResizeHandleRect(Rect box)
        {
            return new Rect(box.xMax - ResizeHandleSize, box.yMax - ResizeHandleSize,
                ResizeHandleSize, ResizeHandleSize);
        }

        private void DrawResizeHandle(Rect localRect, Event e)
        {
            var handle = ResizeHandleRect(localRect);

            if (e.type == EventType.Repaint)
            {
                DrawResizeMark(handle);
                return;
            }

            if (!_owner.AllowCanvasClick)
                return;

            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                _owner.BeginNodeResize(Node);
                e.Use();
            }
        }

        /// <summary>
        /// Draws the resize affordance as dots marching along the corner's diagonal. A right-angled
        /// bracket cannot work here: on a rounded node the arc cuts inside the corner and the arms
        /// of the bracket end up outside the silhouette. Dots on the diagonal start past where the
        /// arc cuts — an inset derived from the node's own corner radius — so the mark fits any
        /// radius, including none.
        /// </summary>
        private void DrawResizeMark(Rect handle)
        {
            var inset = WindowCornerRadius * CornerDiagonalInset + ResizeMarkMargin;
            for (var i = 0; i < ResizeMarkDots; i++)
            {
                var offset = inset + i * ResizeMarkSpacing;
                if (offset + ResizeMarkDotSize > ResizeHandleSize)
                    return;

                EditorGUI.DrawRect(new Rect(handle.xMax - offset - ResizeMarkDotSize,
                    handle.yMax - offset - ResizeMarkDotSize, ResizeMarkDotSize, ResizeMarkDotSize), _resizeMarkColor);
            }
        }

        private static GUIStyle BodyStyle
        {
            get
            {
                return _bodyStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    clipping = TextClipping.Clip,
                    richText = false,
                };
            }
        }

        private static GUIStyle EmphasizedBodyStyle
        {
            get
            {
                return _emphasizedBodyStyle ??= new GUIStyle(BodyStyle) { fontStyle = FontStyle.Bold };
            }
        }

        private void RefreshHostedAction()
        {
            var action = Node is IActionHost host ? host.Action : null;
            if (ReferenceEquals(action, _cachedAction))
                return;

            _cachedAction = action;
            _actionName = action != null ? NodeTypeInfo.For(action.GetType()).Name : null;
        }

        private void HandleWindowEvents(Event e)
        {
            if (!_owner.AllowCanvasClick)
                return;

            if (e.type == EventType.MouseDown && e.button == 0)
                _owner.OnNodeMouseDown(Node, e.control || e.command);
            else if (e.type == EventType.ContextClick || (e.type == EventType.MouseDown && e.button == 1))
            {
                _owner.ShowNodeContextMenu(Node);
                e.Use();
            }
        }

        private void HandleDrag(Event e)
        {
            if (!_owner.AllowCanvasClick || _owner.IsResizingNode(Node))
                return;

            if (e.type == EventType.MouseDrag && e.button == 0 && _owner.TryDragSelection(Node, e.delta))
                return;

            GUI.DragWindow();
        }
    }
}
