using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
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

        protected virtual int CornerRadius => 0;

        protected virtual string GetBodyText()
        {
            RefreshHostedAction();
            return _actionName;
        }

        protected virtual bool IsBodyEmphasized => Node is IActionHost;

        internal int WindowCornerRadius => Mathf.Max(0, CornerRadius);

        internal GUIStyle WindowStyle => CanvasShapes.WindowStyle(WindowCornerRadius, !HasBody);

        internal bool HasBody => !string.IsNullOrEmpty(GetBodyText());

        public virtual string GetOutConnectionLabel(int index) => null;

        public virtual int GetOutConnectionSlot(int connectionIndex) => connectionIndex;

        public virtual bool IsOutSlotConditional(int slot) => false;

        public virtual bool CanReassignOutConnectionSlot => false;

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

        protected Rect BodyRect(Rect localRect)
        {
            var top = Mathf.Max(CanvasShapes.NodeHeaderHeight, WindowCornerRadius);
            return new Rect(0f, top, localRect.width, localRect.height - top);
        }

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
