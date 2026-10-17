using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public class GraphEditorWindow : EditorWindow
    {
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 1f;
        private const float ZoomNotchFactor = 1.15f;
        private const float ZoomNotchFactorFine = 1.07f;
        private const float GridSpacing = 20f;
        private const float GridCoarseZoomThreshold = 0.5f;
        private const float GridVeryCoarseZoomThreshold = 0.2f;
        private const float GridCoarseFactor = 5f;
        private const int GridMajorEvery = 5;
        private const float CullingMargin = 100f;
        private const float RectSelectThreshold = 4f;

        private static readonly Color _backgroundColor = new Color(0.16f, 0.16f, 0.16f);
        private static readonly Color _gridMinorColor = new Color(0f, 0f, 0f, 0.18f);
        private static readonly Color _gridMajorColor = new Color(0f, 0f, 0f, 0.38f);
        private static readonly Color _selectionColor = new Color(0.35f, 0.65f, 1f, 0.9f);
        private static readonly Color _rectSelectFillColor = new Color(0.35f, 0.65f, 1f, 0.08f);
        private static readonly Color _rectHighlightColor = new Color(0.35f, 0.65f, 1f, 0.5f);
        private static readonly Color _portEmptyColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color _portConnectedColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color _portStripColor = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color _portConditionColor = new Color(1f, 0.82f, 0.35f, 0.95f);
        private static readonly Color _previewConnectionColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);
        private const float PortSize = 12f;
        private const float PortConditionRingWidth = 2f;
        private const float PortOffset = 6f;
        private const float PortSpacing = 20f;
        private const float PortEdgeMargin = 10f;
        private const float NodeTintStrength = 0.55f;
        private const float GroupHeaderHeight = 22f;
        private const float GroupResizeCorner = 16f;
        private const float GroupMinWidth = 120f;
        private const float GroupMinHeight = 80f;
        private static readonly Vector2 _newGroupSize = new Vector2(300f, 200f);
        private static readonly Vector2 _duplicateOffset = new Vector2(24f, 24f);
        private static readonly Color _groupBorderColor = new Color(1f, 1f, 1f, 0.25f);
        private static GUIStyle _groupTitleStyle;
        private static GUIStyle _tooltipStyle;
        private static GUIStyle _groupCommentStyle;
        private const float GroupCommentSideWidth = 180f;
        private const float GroupCommentGap = 4f;
        private static readonly GUIContent _groupCommentContent = new GUIContent();
        private static readonly GUIContent _viewMenuContent = new GUIContent("View");
        private static readonly GUIContent _showConnectionLabelsContent = new GUIContent("Show text on link lines");
        private const float TooltipMaxWidth = 240f;
        private const float PanelSplitterGrabHalfWidth = 3f;
        private const float CanvasMinVisibleWidth = 150f;
        private const double TooltipDelaySeconds = 0.6;
        private static readonly Vector2 _tooltipCursorOffset = new Vector2(16f, 20f);
        private static readonly GUIContent _tooltipContent = new GUIContent();
        private NodeGroup _hoveredCommentGroup;
        private bool _tooltipShown;
        private Vector2 _tooltipPosition;
        private Vector2 _lastWindowMousePosition;
        private double _lastPointerActivityTime;

        private static GUIStyle _commentStyle;
        private static GUIStyle _startTagStyle;

        [SerializeField] private string _assetGuid;
        [SerializeField] private string _stashedJson;
        [SerializeField] private bool _stashedDirty;
        [SerializeField] private bool _showInspector = true;
        [SerializeField] private float _inspectorWidth = GraphInspectorPanel.DefaultWidth;

        private GraphWindowSession _session;
        private readonly Dictionary<Node, NodeView> _views = new Dictionary<Node, NodeView>();
        private readonly Dictionary<Connection, ConnectionView> _connectionViews = new Dictionary<Connection, ConnectionView>();
        private readonly CanvasSelection _selection = new CanvasSelection();
        private readonly GraphInspectorPanel _inspectorPanel = new GraphInspectorPanel();
        private Rect _panelRect;
        private Node _portDragSource;
        private int _portDragSlot = -1;
        private bool _nodeDragMoved;
        private Node _resizingNode;
        private NodeGroup _draggingGroup;
        private NodeGroup _resizingGroup;
        private NodeGroup _renamingGroup;
        private string _renameBuffer;
        private readonly List<Node> _groupDragNodes = new List<Node>();
        private readonly List<GroupDragPoint> _groupDragPoints = new List<GroupDragPoint>();

        private readonly struct GroupDragPoint
        {
            public readonly Connection Connection;
            public readonly int Index;

            public GroupDragPoint(Connection connection, int index)
            {
                Connection = connection;
                Index = index;
            }
        }

        internal bool AllowCanvasClick { get; private set; } = true;

        internal bool ConnectionGestureActive { get; set; }
        private Rect _canvasRect;
        private Vector2 _guiGroupOrigin;
        private Matrix4x4 _previousGuiMatrix;
        private string _statusLabel = "";
        private string _zoomLabel = "100%";
        private int _zoomLabelPercent = 100;
        private string _nodeCountLabel = "";
        private int _drawnNodeCount = -1;
        private int _totalNodeCount = -1;
        private bool _rectSelectPending;
        private bool _isRectSelecting;
        private Vector2 _rectSelectStart;
        private Vector2 _rectSelectEnd;
        private Node _pendingCommentNode;
        private GenericMenu _pendingContextMenu;
        private Vector2? _pendingBrowserWindowPosition;
        private Vector2 _pendingBrowserCanvasPosition;
        private bool _isPanMouseDown;
        private bool _fullDrawPass = true;

        private float CurrentZoom => Mathf.Clamp(_session.WorkingCopy.ZoomFactor, MinZoom, MaxZoom);

        [MenuItem("Window/NodeGraphAvr/Graph Editor")]
        private static void OpenEmpty()
        {
            GetGraphWindow();
        }

        public static void Open(Graph graph)
        {
            var path = AssetDatabase.GetAssetPath(graph);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{nameof(GraphEditorWindow)}: {nameof(graph)} '{graph.name}' has no asset path in {nameof(Open)}. " +
                               "Only imported graph assets can be opened. Fix: open a .ngbt/.ngdlg asset from the Project window.", graph);
                return;
            }

            GetGraphWindow().SetTarget(AssetDatabase.AssetPathToGUID(path));
        }

        private static GraphEditorWindow GetGraphWindow()
        {
            var window = GetWindow<GraphEditorWindow>();
            window.UpdateChrome();
            return window;
        }

        private void OnEnable()
        {
            saveChangesMessage = "The graph has unsaved changes.";
            wantsMouseMove = true;
            _lastPointerActivityTime = EditorApplication.timeSinceStartup;
            AssemblyReloadEvents.beforeAssemblyReload += StashSession;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += OnEditorUpdate;
            UpdateChrome();
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= StashSession;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Application.logMessageReceived -= OnLogMessageReceived;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_tooltipShown || _hoveredCommentGroup == null || _session == null)
                return;

            if (mouseOverWindow != this)
            {
                _hoveredCommentGroup = null;
                return;
            }

            if (EditorApplication.timeSinceStartup - _lastPointerActivityTime < TooltipDelaySeconds)
                return;

            _tooltipShown = true;
            _tooltipPosition = _lastWindowMousePosition + _tooltipCursorOffset;
            Repaint();
        }

        private void OnUndoRedoPerformed()
        {
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            _portDragSource = null;
            _portDragSlot = -1;
            _nodeDragMoved = false;
            _resizingNode = null;
            ConnectionGestureActive = false;
            ResetGroupGestures();
            _fullDrawPass = true;
            Repaint();
        }

        private void ResetGroupGestures()
        {
            _draggingGroup = null;
            _resizingGroup = null;
            _renamingGroup = null;
            _groupDragNodes.Clear();
            _groupDragPoints.Clear();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Warning || _session == null)
                return;

            if (condition.IndexOf("Refused", StringComparison.Ordinal) >= 0)
                ShowNotification(new GUIContent(condition));
        }

        private void OnDestroy()
        {
            DisposeSession();
        }

        public override void SaveChanges()
        {
            if (_session != null && !_session.Save())
                throw new InvalidOperationException($"{nameof(GraphEditorWindow)}: saving the graph failed — see the Console for the cause.");
            base.SaveChanges();
        }

        private void OnGUI()
        {
            EnsureSession();
            _canvasRect = new Rect(0f, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);
            DrawToolbar();

            if (_session == null || _session.WorkingCopy == null)
            {
                GUI.Label(_canvasRect, _statusLabel, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var e = Event.current;
            var panelWidth = ClampInspectorWidth(_inspectorWidth);
            _panelRect = new Rect(position.width - panelWidth, _canvasRect.y, panelWidth, _canvasRect.height);
            if (_showInspector)
            {
                HandlePanelResize(e);
                _canvasRect.width -= panelWidth;
            }

            AllowCanvasClick = !_showInspector || !(_panelRect.Contains(e.mousePosition) || PanelSplitterRect.Contains(e.mousePosition));
            StampPointerActivity(e);
            if ((e.alt || _isPanMouseDown) && AllowCanvasClick)
                EditorGUIUtility.AddCursorRect(_canvasRect, MouseCursor.Pan);
            if (_showInspector)
                _inspectorPanel.OnGUI(_panelRect, _session.WorkingCopy, _selection);
            HandleSaveShortcut(e);
            HandleCanvasInput(e);
            DrawCanvas();
            UpdateGroupTooltip(e);
            DrawGroupTooltip(e);
            HandlePostCanvasEvents(e);
            if (e.type == EventType.Repaint)
                _fullDrawPass = false;
            ShowPendingContextMenu();
            ShowPendingCommentPopup();
            ShowPendingCreationBrowser();
        }

        private Rect PanelSplitterRect =>
            new Rect(_panelRect.x - PanelSplitterGrabHalfWidth, _panelRect.y, PanelSplitterGrabHalfWidth * 2f, _panelRect.height);

        private void HandlePanelResize(Event e)
        {
            var splitterRect = PanelSplitterRect;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var cursorRect = GUIUtility.hotControl == controlId
                ? new Rect(0f, 0f, position.width, position.height)
                : splitterRect;
            EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.ResizeHorizontal);
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && splitterRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        GUIUtility.keyboardControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        _inspectorWidth = ClampInspectorWidth(position.width - e.mousePosition.x);
                        _panelRect = new Rect(position.width - _inspectorWidth, _canvasRect.y, _inspectorWidth, _canvasRect.height);
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        private float ClampInspectorWidth(float width)
        {
            var max = Mathf.Max(GraphInspectorPanel.MinWidth, position.width - CanvasMinVisibleWidth);
            return Mathf.Clamp(width, GraphInspectorPanel.MinWidth, max);
        }

        private void SetTarget(string assetGuid)
        {
            if (_assetGuid == assetGuid && _session != null)
                return;

            if (_session != null && _session.IsDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "Unsaved graph changes",
                    $"'{_session.WorkingCopy.name}' has unsaved changes. Save them before switching graphs?",
                    "Save", "Cancel", "Discard");
                if (choice == 1)
                    return;
                if (choice == 0 && !_session.Save())
                    return;
            }

            DisposeSession();
            _assetGuid = assetGuid;
            _stashedJson = null;
            _stashedDirty = false;
            EnsureSession();
            Repaint();
        }

        private void EnsureSession()
        {
            if (_session != null || string.IsNullOrEmpty(_assetGuid))
                return;

            _session = GraphWindowSession.Load(_assetGuid, _stashedJson, _stashedDirty);
            _stashedJson = null;
            _stashedDirty = false;
            if (_session == null)
            {
                _assetGuid = null;
                UpdateChrome();
                return;
            }

            _session.Changed += OnSessionChanged;
            _fullDrawPass = true;
            UpdateChrome();
            OnSessionChanged();
        }

        private void DisposeSession()
        {
            if (_session == null)
                return;

            _session.Changed -= OnSessionChanged;
            _session.Dispose();
            _session = null;
            _views.Clear();
            _connectionViews.Clear();
            _selection.Clear();
            _portDragSource = null;
            _portDragSlot = -1;
            ResetGroupGestures();
            _inspectorPanel.Dispose();
        }

        private void StashSession()
        {
            if (_session == null)
                return;

            if (_session.WorkingCopy != null)
            {
                try
                {
                    _stashedJson = GraphJsonSerializer.Serialize(_session.WorkingCopy);
                    _stashedDirty = _session.IsDirty;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{nameof(GraphEditorWindow)}: could not stash the working copy before domain reload — {exception.Message}. " +
                                   "Unsaved changes will be lost; the graph reloads from its file.");
                    _stashedJson = null;
                    _stashedDirty = false;
                }
            }

            DisposeSession();
        }

        private void OnSessionChanged()
        {
            hasUnsavedChanges = _session != null && _session.IsDirty;
            if (_session != null && _session.WorkingCopy != null)
            {
                _selection.PruneMissing(_session.WorkingCopy);
                PruneStaleViews(_session.WorkingCopy);
                PruneStaleConnectionViews(_session.WorkingCopy);
                PruneStaleGestures(_session.WorkingCopy);
            }

            Repaint();
        }

        private void PruneStaleGestures(Graph graph)
        {
            if (_draggingGroup != null && !ContainsGroup(graph, _draggingGroup))
            {
                _draggingGroup = null;
                _groupDragNodes.Clear();
                _groupDragPoints.Clear();
            }

            if (_resizingGroup != null && !ContainsGroup(graph, _resizingGroup))
                _resizingGroup = null;
            if (_renamingGroup != null && !ContainsGroup(graph, _renamingGroup))
                _renamingGroup = null;
            if (_portDragSource != null && !ContainsNode(graph, _portDragSource))
            {
                _portDragSource = null;
                _portDragSlot = -1;
            }

            if (_resizingNode != null && !ContainsNode(graph, _resizingNode))
                _resizingNode = null;
        }

        private static bool ContainsGroup(Graph graph, NodeGroup group)
        {
            var groups = graph.Groups;
            for (var i = 0; i < groups.Count; i++)
                if (ReferenceEquals(groups[i], group))
                    return true;
            return false;
        }

        private static bool ContainsNode(Graph graph, Node node)
        {
            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], node))
                    return true;
            return false;
        }

        private void PruneStaleViews(Graph graph)
        {
            if (_views.Count == graph.Nodes.Count)
                return;

            var stale = new List<Node>();
            foreach (var pair in _views)
                if (!IsInGraph(graph, pair.Key))
                    stale.Add(pair.Key);
            for (var i = 0; i < stale.Count; i++)
                _views.Remove(stale[i]);
        }

        private void PruneStaleConnectionViews(Graph graph)
        {
            List<Connection> stale = null;
            foreach (var pair in _connectionViews)
            {
                var connection = pair.Key;
                var alive = connection.Source != null && IsInGraph(graph, connection.Source) &&
                            ContainsConnection(connection.Source, connection);
                if (!alive)
                    (stale ??= new List<Connection>()).Add(connection);
            }

            if (stale == null)
                return;
            for (var i = 0; i < stale.Count; i++)
                _connectionViews.Remove(stale[i]);
        }

        private static bool ContainsConnection(Node source, Connection connection)
        {
            var outs = source.OutConnections;
            for (var i = 0; i < outs.Count; i++)
                if (ReferenceEquals(outs[i], connection))
                    return true;
            return false;
        }

        private static bool IsInGraph(Graph graph, Node node)
        {
            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], node))
                    return true;
            return false;
        }

        private void UpdateChrome()
        {
            var path = string.IsNullOrEmpty(_assetGuid) ? null : AssetDatabase.GUIDToAssetPath(_assetGuid);
            if (string.IsNullOrEmpty(path))
            {
                titleContent = new GUIContent("Graph Editor");
                _statusLabel = "No graph loaded. Double-click a .ngbt/.ngdlg asset to open it.";
                return;
            }

            titleContent = new GUIContent(Path.GetFileNameWithoutExtension(path));
            _statusLabel = path;
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            using (new EditorGUI.DisabledScope(_session == null))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    _session.Save();
            }

            var viewRect = GUILayoutUtility.GetRect(_viewMenuContent, EditorStyles.toolbarDropDown);
            if (GUI.Button(viewRect, _viewMenuContent, EditorStyles.toolbarDropDown))
                ShowViewMenu(viewRect);

            GUILayout.FlexibleSpace();
            GUILayout.Label(_statusLabel, EditorStyles.miniLabel);
            if (_session != null && _session.WorkingCopy != null)
            {
                GUILayout.Label(NodeCountLabel(), EditorStyles.miniLabel, GUILayout.Width(70f));
                GUILayout.Label(ZoomLabel(), EditorStyles.miniLabel, GUILayout.Width(40f));
            }

            _showInspector = GUILayout.Toggle(_showInspector, "Inspector", EditorStyles.toolbarButton);
            GUILayout.EndHorizontal();
        }

        private void ShowViewMenu(Rect activator)
        {
            var menu = new GenericMenu();
            menu.AddItem(_showConnectionLabelsContent, GraphViewOptions.ShowConnectionLabels, ToggleConnectionLabels);
            menu.DropDown(activator);
        }

        private void ToggleConnectionLabels()
        {
            GraphViewOptions.ShowConnectionLabels = !GraphViewOptions.ShowConnectionLabels;
            Repaint();
        }

        private string ZoomLabel()
        {
            var percent = Mathf.RoundToInt(CurrentZoom * 100f);
            if (percent != _zoomLabelPercent)
            {
                _zoomLabelPercent = percent;
                _zoomLabel = percent + "%";
            }

            return _zoomLabel;
        }

        private string NodeCountLabel()
        {
            var total = _session.WorkingCopy.Nodes.Count;
            if (_drawnNodeCount == -1)
                _drawnNodeCount = total;
            if (total != _totalNodeCount || _nodeCountLabel.Length == 0)
            {
                _totalNodeCount = total;
                _nodeCountLabel = "nodes " + _drawnNodeCount + "/" + total;
            }

            return _nodeCountLabel;
        }

        private void HandleSaveShortcut(Event e)
        {
            if (e.type != EventType.KeyDown || e.keyCode != KeyCode.S || !(e.control || e.command))
                return;

            _session.Save();
            e.Use();
        }

        private void HandleCanvasInput(Event e)
        {
            if (e.type == EventType.MouseDown && e.button == 2)
                _isPanMouseDown = true;
            else if (e.rawType == EventType.MouseUp && e.button == 2)
                _isPanMouseDown = false;

            var overCanvas = _canvasRect.Contains(e.mousePosition) && AllowCanvasClick;
            if (e.type == EventType.ScrollWheel && overCanvas)
            {
                var factor = e.shift ? ZoomNotchFactorFine : ZoomNotchFactor;
                ZoomAround(e.mousePosition - _canvasRect.position, e.delta.y < 0f ? factor : 1f / factor);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && (e.button == 2 || (e.button == 0 && e.alt)) && overCanvas && !_isRectSelecting)
            {
                _rectSelectPending = false;
                _session.WorkingCopy.Translation = RoundPan(_session.WorkingCopy.Translation + e.delta / CurrentZoom);
                e.Use();
                Repaint();
            }
        }

        private void HandlePostCanvasEvents(Event e)
        {
            if (e.rawType == EventType.MouseUp && e.button == 0 && _nodeDragMoved)
            {
                _nodeDragMoved = false;
                SortMovedNodeParents();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && _canvasRect.Contains(e.mousePosition) && AllowCanvasClick)
            {
                GUIUtility.keyboardControl = 0;
                _selection.Clear();
                _rectSelectPending = true;
                _rectSelectStart = MouseToCanvas(e.mousePosition);
                _rectSelectEnd = _rectSelectStart;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && _rectSelectPending)
            {
                _rectSelectEnd = MouseToCanvas(e.mousePosition);
                if (!_isRectSelecting && (_rectSelectEnd - _rectSelectStart).magnitude * CurrentZoom > RectSelectThreshold)
                    _isRectSelecting = true;
                e.Use();
                Repaint();
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0 && _rectSelectPending)
            {
                if (_isRectSelecting)
                    ApplyRectSelection();
                _rectSelectPending = false;
                _isRectSelecting = false;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.ContextClick && _canvasRect.Contains(e.mousePosition) && AllowCanvasClick)
            {
                ShowCreateNodeMenu(MouseToCanvas(e.mousePosition), e.mousePosition);
                e.Use();
            }
            else if (GUIUtility.keyboardControl == 0)
            {
                if (e.type == EventType.ValidateCommand && (e.commandName == "SoftDelete" || e.commandName == "Delete"))
                {
                    e.Use();
                }
                else if (e.type == EventType.ExecuteCommand && (e.commandName == "SoftDelete" || e.commandName == "Delete"))
                {
                    if (_selection.Count > 0)
                        DeleteSelection();
                    e.Use();
                }
                else if (e.type == EventType.ValidateCommand && IsClipboardCommand(e.commandName))
                {
                    if (CanExecuteClipboardCommand(e.commandName))
                        e.Use();
                }
                else if (e.type == EventType.ExecuteCommand && IsClipboardCommand(e.commandName))
                {
                    if (CanExecuteClipboardCommand(e.commandName))
                        ExecuteClipboardCommand(e.commandName);
                    e.Use();
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
                {
                    FrameSelection();
                    e.Use();
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space && !e.shift && _canvasRect.Contains(e.mousePosition) && AllowCanvasClick)
                {
                    ShowCreateNodeMenu(MouseToCanvas(e.mousePosition), e.mousePosition);
                    e.Use();
                }
            }
        }

        private static bool IsClipboardCommand(string commandName)
        {
            return commandName == "Copy" || commandName == "Paste" || commandName == "Duplicate";
        }

        private bool CanExecuteClipboardCommand(string commandName)
        {
            if (commandName == "Paste")
                return GraphClipboard.LooksLikeClipboard(EditorGUIUtility.systemCopyBuffer);
            return _selection.Nodes.Count > 0;
        }

        private void ExecuteClipboardCommand(string commandName)
        {
            var graph = _session.WorkingCopy;
            switch (commandName)
            {
                case "Copy":
                {
                    var json = GraphClipboard.Copy(graph, _selection.Nodes);
                    if (json != null)
                        EditorGUIUtility.systemCopyBuffer = json;
                    break;
                }
                case "Paste":
                {
                    SelectPasted(GraphClipboard.Paste(graph, EditorGUIUtility.systemCopyBuffer, PasteAnchor()));
                    break;
                }
                case "Duplicate":
                {
                    var json = GraphClipboard.Copy(graph, _selection.Nodes);
                    if (json != null)
                        SelectPasted(GraphClipboard.Paste(graph, json, NodesPositionCenter(_selection.Nodes) + _duplicateOffset));
                    break;
                }
            }
        }

        private void SelectPasted(List<Node> pasted)
        {
            if (pasted.Count == 0)
                return;

            _selection.Clear();
            for (var i = 0; i < pasted.Count; i++)
                _selection.Add(pasted[i]);
            Repaint();
        }

        private Vector2 PasteAnchor()
        {
            var pointer = _lastWindowMousePosition;
            var overPanel = _showInspector && _panelRect.Contains(pointer);
            if (!overPanel && _canvasRect.Contains(pointer))
                return MouseToCanvas(pointer);
            return MouseToCanvas(_canvasRect.center);
        }

        private static Vector2 NodesPositionCenter(IReadOnlyList<Node> nodes)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    continue;
                min = Vector2.Min(min, nodes[i].Position);
                max = Vector2.Max(max, nodes[i].Position);
            }

            return (min + max) * 0.5f;
        }

        private void SortMovedNodeParents()
        {
            SortParentsOf(_selection.Nodes);
        }

        private void SortParentsOf(IReadOnlyList<Node> movedNodes)
        {
            var graph = _session.WorkingCopy;
            if (!graph.IsTree)
                return;

            List<Node> parents = null;
            for (var i = 0; i < movedNodes.Count; i++)
            {
                var ins = movedNodes[i].InConnections;
                for (var j = 0; j < ins.Count; j++)
                {
                    var parent = ins[j].Source;
                    if (parent == null)
                        continue;
                    parents ??= new List<Node>();
                    if (!parents.Contains(parent))
                        parents.Add(parent);
                }
            }

            if (parents == null)
                return;
            for (var i = 0; i < parents.Count; i++)
                graph.SortChildrenByPosition(parents[i]);
            Repaint();
        }

        private void FrameSelection()
        {
            var graph = _session.WorkingCopy;
            var nodes = _selection.Nodes.Count > 0 ? _selection.Nodes : graph.Nodes;
            if (nodes.Count == 0)
                return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    continue;
                var rect = GetView(nodes[i]).LastRect;
                min = Vector2.Min(min, rect.min);
                max = Vector2.Max(max, rect.max);
            }

            var center = (min + max) * 0.5f;
            graph.Translation = RoundPan(_canvasRect.size * 0.5f / CurrentZoom - center);
            _fullDrawPass = true;
            Repaint();
        }

        private static Vector2 RoundPan(Vector2 pan)
        {
            return new Vector2(Mathf.Round(pan.x), Mathf.Round(pan.y));
        }

        private Vector2 MouseToCanvas(Vector2 windowPoint)
        {
            return ViewToCanvas(windowPoint - _canvasRect.position);
        }

        private Vector2 ViewToCanvas(Vector2 canvasLocalPoint)
        {
            return canvasLocalPoint / CurrentZoom - _session.WorkingCopy.Translation;
        }

        private NodeView GetView(Node node)
        {
            if (!_views.TryGetValue(node, out var view))
            {
                view = NodeViewRegistry.CreateView(node, this);
                _views.Add(node, view);
            }

            return view;
        }

        private void ZoomAround(Vector2 canvasLocalPoint, float zoomMultiplier)
        {
            var graph = _session.WorkingCopy;
            var pointUnderCursor = canvasLocalPoint / CurrentZoom - graph.Translation;
            graph.ZoomFactor = Mathf.Clamp(CurrentZoom * zoomMultiplier, MinZoom, MaxZoom);
            graph.Translation = RoundPan(canvasLocalPoint / CurrentZoom - pointUnderCursor);
            _fullDrawPass = true;
            Repaint();
        }

        internal void OnNodeMouseDown(Node node, bool toggle)
        {
            GUIUtility.keyboardControl = 0;
            if (toggle)
                _selection.Toggle(node);
            else if (!_selection.Contains(node))
                _selection.Set(node);
            Repaint();
        }

        internal void ShowNodeContextMenu(Node node)
        {
            var graph = _session.WorkingCopy;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteNode(node));
            if (!ReferenceEquals(graph.StartNode, node) && node.AllowAsStart)
                menu.AddItem(new GUIContent("Set As Start"), false, () => SetStartNode(node));
            else
                menu.AddDisabledItem(new GUIContent("Set As Start"));
            menu.AddItem(new GUIContent("Edit Comment"), false, () => RequestCommentEdit(node));
            _pendingContextMenu = menu;
        }

        private void DeleteNode(Node node)
        {
            _session.WorkingCopy.RemoveNode(node);
            _selection.PruneMissing(_session.WorkingCopy);
            Repaint();
        }

        private void SetStartNode(Node node)
        {
            _session.WorkingCopy.SetStartNode(node);
            Repaint();
        }

        private void RequestCommentEdit(Node node)
        {
            _pendingCommentNode = node;
            Repaint();
        }

        private void DeleteSelection()
        {
            var graph = _session.WorkingCopy;
            for (var i = _selection.Connections.Count - 1; i >= 0; i--)
            {
                var connection = _selection.Connections[i];
                graph.Disconnect(connection);
                NotifyOutConnectionRemoved(connection);
            }

            var severed = new List<Connection>();
            for (var i = _selection.Nodes.Count - 1; i >= 0; i--)
            {
                var node = _selection.Nodes[i];
                severed.Clear();
                severed.AddRange(node.InConnections);
                graph.RemoveNode(node);
                for (var j = 0; j < severed.Count; j++)
                    NotifyOutConnectionRemoved(severed[j]);
            }

            for (var i = _selection.Groups.Count - 1; i >= 0; i--)
                graph.RemoveGroup(_selection.Groups[i]);
            _selection.Clear();
            Repaint();
        }

        private void NotifyOutConnectionRemoved(Connection connection)
        {
            if (connection?.Source != null)
                GetView(connection.Source).OnOutConnectionRemoved(connection);
        }

        private void ShowCreateNodeMenu(Vector2 canvasPosition, Vector2 windowPosition)
        {
            _pendingBrowserCanvasPosition = canvasPosition;
            _pendingBrowserWindowPosition = windowPosition;
            Repaint();
        }

        private void ShowPendingCreationBrowser()
        {
            if (_pendingBrowserWindowPosition == null)
                return;

            var windowPosition = _pendingBrowserWindowPosition.Value;
            var canvasPosition = _pendingBrowserCanvasPosition;
            _pendingBrowserWindowPosition = null;

            var graph = _session.WorkingCopy;
            var items = new List<NodeMenuTree.Item>();
            var types = TypeCache.GetTypesDerivedFrom(graph.BaseNodeType);
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (type.IsAbstract || type.Assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal))
                    continue;

                var info = NodeTypeInfo.For(type);
                var createdType = type;
                items.Add(new NodeMenuTree.Item
                {
                    Path = info.MenuPath,
                    Description = info.Description,
                    HasColor = true,
                    Color = NodeTypeInfo.GetTypeColor(type),
                    Invoke = () => CreateNode(createdType, canvasPosition),
                });
            }

            items.Add(new NodeMenuTree.Item
            {
                Path = "Group/Create Group",
                Description = "Adds an empty titled frame at this position. Frames are visual only — dragging one carries the nodes inside it.",
                Invoke = () => CreateGroup(canvasPosition),
            });
            if (_selection.Nodes.Count > 0)
                items.Add(new NodeMenuTree.Item
                {
                    Path = "Group/Group Selection",
                    Description = "Adds a frame around the currently selected nodes.",
                    Invoke = GroupSelection,
                });

            var kindId = GraphTypeRegistry.GetIdOrThrow(graph.GetType());
            var activator = new Rect(windowPosition.x, windowPosition.y, 0f, 0f);
            PopupWindow.Show(activator, new NodeCreationBrowser("Create", kindId, items));
        }

        private void CreateNode(Type nodeType, Vector2 canvasPosition)
        {
            var node = _session.WorkingCopy.AddNode(nodeType, canvasPosition);
            if (node == null)
                return;

            _selection.Set(node);
            Repaint();
        }

        internal bool TryDragSelection(Node node, Vector2 delta)
        {
            if (_selection.Count <= 1 || !_selection.Contains(node))
                return false;

            var graph = _session.WorkingCopy;
            for (var i = 0; i < _selection.Nodes.Count; i++)
            {
                var selected = _selection.Nodes[i];
                graph.MoveNode(selected, selected.Position + delta);
                if (_views.TryGetValue(selected, out var view))
                    view.SetLastRect(new Rect(selected.Position, view.LastRect.size));
            }

            _nodeDragMoved = true;
            Repaint();
            return true;
        }

        private void ShowPendingContextMenu()
        {
            if (_pendingContextMenu == null)
                return;

            var menu = _pendingContextMenu;
            _pendingContextMenu = null;
            menu.ShowAsContext();
        }

        private void ShowPendingCommentPopup()
        {
            if (_pendingCommentNode == null)
                return;

            var node = _pendingCommentNode;
            _pendingCommentNode = null;
            var view = GetView(node);
            var zoom = CurrentZoom;
            var pan = _session.WorkingCopy.Translation;
            var viewPosition = (view.LastRect.position + pan) * zoom + _canvasRect.position;
            var activator = new Rect(viewPosition, view.LastRect.size * zoom);
            PopupWindow.Show(activator, new CommentEditPopup(_session.WorkingCopy, node));
        }

        private void DrawCanvas()
        {
            EditorGUI.DrawRect(_canvasRect, _backgroundColor);
            BeginZoomArea();
            DrawGrid();
            DrawGroups();
            DrawConnections();
            DrawNodes();
            DrawOverlays();
            EndZoomArea();
        }

        private void DrawGroups()
        {
            var graph = _session.WorkingCopy;
            var pan = graph.Translation;
            var e = Event.current;
            var visible = VisibleDrawRect();

            HandleGroupGesture(e, graph);

            EnsureGroupStyle();
            var groups = graph.Groups;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var rect = new Rect(group.Rect.position + pan, group.Rect.size);
                if (!_fullDrawPass && !visible.Overlaps(rect) && !IsGroupGestureTarget(group))
                    continue;

                var headerRect = new Rect(rect.x, rect.y, rect.width, GroupHeaderHeight);
                var resizeRect = new Rect(rect.xMax - GroupResizeCorner, rect.yMax - GroupResizeCorner, GroupResizeCorner, GroupResizeCorner);

                var groupColor = group.Color;
                var fillColor = new Color(groupColor.r, groupColor.g, groupColor.b, 0.12f * groupColor.a);
                var headerColor = new Color(groupColor.r, groupColor.g, groupColor.b, 0.45f * groupColor.a);
                var selected = _selection.Contains(group);
                EditorGUI.DrawRect(rect, fillColor);
                EditorGUI.DrawRect(headerRect, headerColor);
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, selected ? _selectionColor : _groupBorderColor);
                Handles.DrawSolidRectangleWithOutline(resizeRect, Color.clear, _groupBorderColor);

                if (_renamingGroup == group)
                    DrawGroupRenameField(e, graph, group, headerRect);
                else
                    GUI.Label(headerRect, group.Title, _groupTitleStyle);

                DrawGroupSideComment(group, rect);

                if (!AllowCanvasClick)
                    continue;

                EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);
                EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpLeft);

                if (e.type == EventType.MouseDown && e.button == 0 && _renamingGroup != group)
                {
                    if (headerRect.Contains(e.mousePosition))
                    {
                        if (e.clickCount >= 2)
                            BeginGroupRename(group);
                        else
                            BeginGroupDrag(group, graph);
                        e.Use();
                    }
                    else if (resizeRect.Contains(e.mousePosition))
                    {
                        _resizingGroup = group;
                        e.Use();
                    }
                }
                else if ((e.type == EventType.ContextClick || (e.type == EventType.MouseDown && e.button == 1)) && headerRect.Contains(e.mousePosition))
                {
                    ShowGroupContextMenu(group);
                    e.Use();
                }
            }
        }

        private static void DrawGroupSideComment(NodeGroup group, Rect frameRect)
        {
            if (group.CommentPlacement == GroupCommentPlacement.None || string.IsNullOrEmpty(group.Comment))
                return;

            _groupCommentContent.text = group.Comment;
            _groupCommentStyle.normal.textColor = group.CommentColor;
            _groupCommentStyle.alignment = group.CommentPlacement == GroupCommentPlacement.Left
                ? TextAnchor.UpperRight
                : TextAnchor.UpperLeft;
            Rect commentRect;
            switch (group.CommentPlacement)
            {
                case GroupCommentPlacement.Top:
                {
                    var height = _groupCommentStyle.CalcHeight(_groupCommentContent, frameRect.width);
                    commentRect = new Rect(frameRect.x, frameRect.y - height - GroupCommentGap, frameRect.width, height);
                    break;
                }
                case GroupCommentPlacement.Bottom:
                {
                    var height = _groupCommentStyle.CalcHeight(_groupCommentContent, frameRect.width);
                    commentRect = new Rect(frameRect.x, frameRect.yMax + GroupCommentGap, frameRect.width, height);
                    break;
                }
                case GroupCommentPlacement.Left:
                {
                    var width = Mathf.Min(GroupCommentSideWidth, _groupCommentStyle.CalcSize(_groupCommentContent).x + 2f);
                    var height = _groupCommentStyle.CalcHeight(_groupCommentContent, width);
                    commentRect = new Rect(frameRect.x - width - GroupCommentGap, frameRect.y, width, height);
                    break;
                }
                default:
                {
                    var height = _groupCommentStyle.CalcHeight(_groupCommentContent, GroupCommentSideWidth);
                    commentRect = new Rect(frameRect.xMax + GroupCommentGap, frameRect.y, GroupCommentSideWidth, height);
                    break;
                }
            }

            GUI.Label(commentRect, _groupCommentContent, _groupCommentStyle);
        }

        private bool IsGroupGestureTarget(NodeGroup group)
        {
            return _draggingGroup == group || _resizingGroup == group || _renamingGroup == group;
        }

        private void HandleGroupGesture(Event e, Graph graph)
        {
            if (_draggingGroup != null)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    graph.MoveGroup(_draggingGroup, e.delta);
                    for (var i = 0; i < _groupDragNodes.Count; i++)
                    {
                        var node = _groupDragNodes[i];
                        graph.MoveNode(node, node.Position + e.delta);
                        if (_views.TryGetValue(node, out var view))
                            view.SetLastRect(new Rect(node.Position, view.LastRect.size));
                    }

                    for (var i = 0; i < _groupDragPoints.Count; i++)
                    {
                        var point = _groupDragPoints[i];
                        graph.MoveConnectionPoint(point.Connection, point.Index, point.Connection.Waypoints[point.Index] + e.delta);
                    }

                    e.Use();
                    Repaint();
                }
                else if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    SortParentsOf(_groupDragNodes);
                    _draggingGroup = null;
                    _groupDragNodes.Clear();
                    _groupDragPoints.Clear();
                    e.Use();
                    Repaint();
                }
            }

            if (_resizingGroup != null)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    var rect = _resizingGroup.Rect;
                    rect.width = Mathf.Max(GroupMinWidth, rect.width + e.delta.x);
                    rect.height = Mathf.Max(GroupMinHeight, rect.height + e.delta.y);
                    graph.ResizeGroup(_resizingGroup, rect);
                    e.Use();
                    Repaint();
                }
                else if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    _resizingGroup = null;
                    e.Use();
                    Repaint();
                }
            }
        }

        private void BeginGroupDrag(NodeGroup group, Graph graph)
        {
            GUIUtility.keyboardControl = 0;
            _selection.Set(group);
            _draggingGroup = group;
            _groupDragNodes.Clear();
            _groupDragPoints.Clear();
            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                    continue;

                var nodeRect = GetView(node).LastRect;
                if (group.Rect.Contains(nodeRect.min) && group.Rect.Contains(nodeRect.max))
                    _groupDragNodes.Add(node);

                var outs = node.OutConnections;
                for (var j = 0; j < outs.Count; j++)
                {
                    var connection = outs[j];
                    if (connection == null)
                        continue;
                    for (var k = 0; k < connection.Waypoints.Count; k++)
                        if (group.Rect.Contains(connection.Waypoints[k]))
                            _groupDragPoints.Add(new GroupDragPoint(connection, k));
                }
            }
        }

        private void BeginGroupRename(NodeGroup group)
        {
            _renamingGroup = group;
            _renameBuffer = group.Title;
            Repaint();
        }

        private void DrawGroupRenameField(Event e, Graph graph, NodeGroup group, Rect headerRect)
        {
            GUI.SetNextControlName("GroupRename");
            _renameBuffer = GUI.TextField(headerRect, _renameBuffer);
            GUI.FocusControl("GroupRename");
            var commit = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape);
            var clickOutside = e.type == EventType.MouseDown && !headerRect.Contains(e.mousePosition);
            if (!commit && !clickOutside)
                return;

            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.Escape || clickOutside)
            {
                if (_renameBuffer != group.Title)
                    graph.RenameGroup(group, _renameBuffer);
            }

            _renamingGroup = null;
            GUIUtility.keyboardControl = 0;
            if (commit)
                e.Use();
            Repaint();
        }

        private void ShowGroupContextMenu(NodeGroup group)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, () => BeginGroupRename(group));
            menu.AddItem(new GUIContent("Delete Group"), false, () =>
            {
                _session.WorkingCopy.RemoveGroup(group);
                Repaint();
            });
            _pendingContextMenu = menu;
        }

        private void CreateGroup(Vector2 canvasPosition)
        {
            _session.WorkingCopy.AddGroup("Group", new Rect(canvasPosition, _newGroupSize));
            Repaint();
        }

        private void GroupSelection()
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < _selection.Nodes.Count; i++)
            {
                var rect = GetView(_selection.Nodes[i]).LastRect;
                min = Vector2.Min(min, rect.min);
                max = Vector2.Max(max, rect.max);
            }

            var bounds = Rect.MinMaxRect(min.x - 20f, min.y - GroupHeaderHeight - 14f, max.x + 20f, max.y + 20f);
            _session.WorkingCopy.AddGroup("Group", bounds);
            Repaint();
        }

        private void StampPointerActivity(Event e)
        {
            if (!e.isMouse && e.type != EventType.ScrollWheel)
                return;

            _lastPointerActivityTime = EditorApplication.timeSinceStartup;
            _lastWindowMousePosition = e.mousePosition;
            if (_tooltipShown)
            {
                _tooltipShown = false;
                Repaint();
            }
        }

        private void UpdateGroupTooltip(Event e)
        {
            _hoveredCommentGroup = FindHoveredCommentGroup(_lastWindowMousePosition);
            if (_hoveredCommentGroup == null)
            {
                _lastPointerActivityTime = EditorApplication.timeSinceStartup;
                _tooltipShown = false;
            }
        }

        private NodeGroup FindHoveredCommentGroup(Vector2 windowPoint)
        {
            if (!AllowCanvasClick || !_canvasRect.Contains(windowPoint) || GUIUtility.hotControl != 0 ||
                _draggingGroup != null || _resizingGroup != null || _resizingNode != null || _portDragSource != null ||
                _isRectSelecting || ConnectionGestureActive)
                return null;

            var canvasPoint = MouseToCanvas(windowPoint);
            var groups = _session.WorkingCopy.Groups;
            for (var i = groups.Count - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(groups[i].Comment) && groups[i].CommentPlacement == GroupCommentPlacement.None &&
                    groups[i].Rect.Contains(canvasPoint))
                    return groups[i];
            return null;
        }

        private void DrawGroupTooltip(Event e)
        {
            if (!_tooltipShown || _hoveredCommentGroup == null || e.type != EventType.Repaint)
                return;

            EnsureGroupStyle();
            _tooltipContent.text = _hoveredCommentGroup.Comment;
            var size = _tooltipStyle.CalcSize(_tooltipContent);
            if (size.x > TooltipMaxWidth)
                size = new Vector2(TooltipMaxWidth, _tooltipStyle.CalcHeight(_tooltipContent, TooltipMaxWidth));
            var tooltipPosition = _tooltipPosition;
            tooltipPosition.x = Mathf.Min(tooltipPosition.x, position.width - size.x - 4f);
            tooltipPosition.y = Mathf.Min(tooltipPosition.y, position.height - size.y - 4f);
            _tooltipStyle.Draw(new Rect(tooltipPosition, size), _tooltipContent, false, false, false, false);
        }

        private static void EnsureGroupStyle()
        {
            if (_groupTitleStyle != null)
                return;

            _groupTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 0, 0)
            };
            _tooltipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                fontSize = 11,
                padding = new RectOffset(6, 6, 4, 4)
            };
            _groupCommentStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                fontSize = 13,
                alignment = TextAnchor.UpperLeft
            };
        }

        private Rect VisibleDrawRect()
        {
            var zoom = CurrentZoom;
            return new Rect(-CullingMargin, -CullingMargin,
                _canvasRect.width / zoom + CullingMargin * 2f, _canvasRect.height / zoom + CullingMargin * 2f);
        }

        private void DrawConnections()
        {
            var graph = _session.WorkingCopy;
            var pan = graph.Translation;
            var e = Event.current;
            var visible = VisibleDrawRect();

            HandlePortDragDrop(e, pan);

            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                    continue;

                DrawNodeOutPorts(node, e, pan, visible);
                var outs = node.OutConnections;
                var slotCount = OutSlotCount(node);
                var nodeView = GetView(node);
                for (var j = 0; j < outs.Count; j++)
                {
                    var connection = outs[j];
                    if (connection == null || connection.Target == null)
                        continue;

                    var view = GetConnectionView(connection);
                    var fromPos = OutPortPosition(node, nodeView.GetOutConnectionSlot(j), slotCount, pan);
                    var toPos = TargetAnchorPosition(connection.Target, pan);
                    if (!_fullDrawPass && !visible.Overlaps(ConnectionBounds(connection, fromPos, toPos, pan)) &&
                        !_selection.Contains(connection) && view.ActiveRelinkEnd == ConnectionView.RelinkEnd.None)
                        continue;

                    var label = GraphViewOptions.ShowConnectionLabels ? nodeView.GetOutConnectionLabel(j) : null;
                    view.Draw(fromPos, toPos, graph.FlowDirection, pan, label);
                }
            }

            DrawPortDragPreview(e, pan);
        }

        private void HandlePortDragDrop(Event e, Vector2 pan)
        {
            if (_portDragSource == null)
                return;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _portDragSource = null;
                _portDragSlot = -1;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag)
                Repaint();
            if (e.rawType != EventType.MouseUp || e.button != 0)
                return;

            var graph = _session.WorkingCopy;
            var source = _portDragSource;
            var slot = _portDragSlot;
            _portDragSource = null;
            _portDragSlot = -1;
            var target = AllowCanvasClick ? NodeUnderMouse(e.mousePosition, pan) : null;
            if (target != null)
            {
                var connection = graph.Connect(source, target);
                if (connection != null)
                {
                    GetView(source).OnOutConnectionCreated(connection, slot);
                    if (graph.IsTree)
                        graph.SortChildrenByPosition(source);
                }
            }

            e.Use();
            Repaint();
        }

        private void DrawNodeOutPorts(Node node, Event e, Vector2 pan, Rect visible)
        {
            if (node.MaxOutConnections == 0)
                return;

            var view = GetView(node);
            var nodeRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
            if (!_fullDrawPass && !visible.Overlaps(nodeRect))
                return;

            var outs = node.OutConnections;
            var canHaveMore = node.MaxOutConnections == -1 || outs.Count < node.MaxOutConnections;
            var slotCount = OutSlotCount(node);
            if (canHaveMore)
                EditorGUI.DrawRect(PortStripRect(nodeRect), _portStripColor);
            for (var s = 0; s < slotCount; s++)
            {
                var square = PortSquareRect(OutPortPosition(node, s, slotCount, pan));
                if (view.IsOutSlotConditional(s))
                    EditorGUI.DrawRect(Grown(square, PortConditionRingWidth), _portConditionColor);
                EditorGUI.DrawRect(square, IsOutSlotOccupied(node, s) ? _portConnectedColor : _portEmptyColor);
            }
            if (!canHaveMore)
                return;

            if (!AllowCanvasClick)
                return;

            var portStrip = PortStripRect(nodeRect);
            EditorGUIUtility.AddCursorRect(portStrip, MouseCursor.ArrowPlus);
            if (e.type == EventType.MouseDown && e.button == 0 && portStrip.Contains(e.mousePosition) &&
                !IsOverOccupiedOutPort(node, slotCount, pan, e.mousePosition))
            {
                _portDragSource = node;
                _portDragSlot = FreeSlotUnderMouse(node, slotCount, pan, e.mousePosition);
                if (_portDragSlot < 0)
                    _portDragSlot = FirstFreeOutSlot(node, slotCount);
                e.Use();
            }
        }

        private bool IsOutSlotOccupied(Node node, int slot)
        {
            var view = GetView(node);
            for (var j = 0; j < node.OutConnections.Count; j++)
                if (view.GetOutConnectionSlot(j) == slot)
                    return true;
            return false;
        }

        private bool IsOverOccupiedOutPort(Node node, int slotCount, Vector2 pan, Vector2 mousePosition)
        {
            var view = GetView(node);
            for (var j = 0; j < node.OutConnections.Count; j++)
                if (PortSquareRect(OutPortPosition(node, view.GetOutConnectionSlot(j), slotCount, pan)).Contains(mousePosition))
                    return true;
            return false;
        }

        private int FreeSlotUnderMouse(Node node, int slotCount, Vector2 pan, Vector2 mousePosition)
        {
            for (var s = 0; s < slotCount; s++)
                if (!IsOutSlotOccupied(node, s) && PortSquareRect(OutPortPosition(node, s, slotCount, pan)).Contains(mousePosition))
                    return s;
            return -1;
        }

        private int FirstFreeOutSlot(Node node, int slotCount)
        {
            for (var s = 0; s < slotCount; s++)
                if (!IsOutSlotOccupied(node, s))
                    return s;
            return slotCount - 1;
        }

        private static int OutSlotCount(Node node)
        {
            var outs = node.OutConnections.Count;
            if (node.MaxOutConnections == -1)
                return outs + 1;
            return Mathf.Max(outs, node.MaxOutConnections);
        }

        private static void DrawNodeOutline(NodeView view, Rect rect, Color color, int radiusGrowth)
        {
            var radius = view.WindowCornerRadius;
            if (radius > 0)
                CanvasShapes.DrawRoundedOutline(rect, color, radius + radiusGrowth);
            else
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, color);
        }

        private Vector2 EffectiveNodeSize(Node node)
        {
            var size = node.Size;
            return new Vector2(Mathf.Max(size.x, NodeMinWidth(node)), Mathf.Max(size.y, Node.MinSize.y));
        }

        private float NodeMinWidth(Node node)
        {
            if (_session.WorkingCopy.FlowDirection != FlowDirection.Vertical)
                return Node.MinSize.x;

            var span = (OutSlotCount(node) - 1) * PortSpacing + PortSize + PortEdgeMargin * 2f;
            return Mathf.Max(Node.MinSize.x, span);
        }

        private Rect PortStripRect(Rect nodeRect)
        {
            return _session.WorkingCopy.FlowDirection == FlowDirection.Vertical
                ? new Rect(nodeRect.x, nodeRect.yMax, nodeRect.width, PortSize)
                : new Rect(nodeRect.xMax, nodeRect.y, PortSize, nodeRect.height);
        }

        private void DrawPortDragPreview(Event e, Vector2 pan)
        {
            if (_portDragSource == null)
                return;

            var graph = _session.WorkingCopy;
            var slotCount = OutSlotCount(_portDragSource);
            var previewSlot = _portDragSlot >= 0 ? _portDragSlot : FirstFreeOutSlot(_portDragSource, slotCount);
            var fromPos = OutPortPosition(_portDragSource, previewSlot, slotCount, pan);
            ConnectionView.ResolveTangents(fromPos, e.mousePosition, graph.FlowDirection, out var fromTangent, out var toTangent);
            Handles.DrawBezier(fromPos, e.mousePosition, fromPos + fromTangent, e.mousePosition + toTangent, _previewConnectionColor, null, 3f);
        }

        private static Rect PortSquareRect(Vector2 center)
        {
            return new Rect(center.x - PortSize * 0.5f, center.y - PortSize * 0.5f, PortSize, PortSize);
        }

        private static Rect Grown(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private Vector2 OutPortPosition(Node node, int slotIndex, int slotCount, Vector2 pan)
        {
            var view = GetView(node);
            var nodeRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
            var offset = slotCount <= 1 ? 0f : (slotIndex - (slotCount - 1) * 0.5f) * SlotSpacing(nodeRect, slotCount);
            return _session.WorkingCopy.FlowDirection == FlowDirection.Vertical
                ? new Vector2(nodeRect.center.x + offset, nodeRect.yMax + PortOffset)
                : new Vector2(nodeRect.xMax + PortOffset, nodeRect.center.y + offset);
        }

        private float SlotSpacing(Rect nodeRect, int slotCount)
        {
            var edgeLength = _session.WorkingCopy.FlowDirection == FlowDirection.Vertical ? nodeRect.width : nodeRect.height;
            return Mathf.Min(PortSpacing, (edgeLength - PortSize) / (slotCount - 1));
        }

        private Vector2 TargetAnchorPosition(Node node, Vector2 pan)
        {
            var view = GetView(node);
            var nodeRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
            return _session.WorkingCopy.FlowDirection == FlowDirection.Vertical
                ? new Vector2(nodeRect.center.x, nodeRect.yMin)
                : new Vector2(nodeRect.xMin, nodeRect.center.y);
        }

        private Node NodeUnderMouse(Vector2 drawSpacePosition, Vector2 pan)
        {
            var nodes = _session.WorkingCopy.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                    continue;
                var view = GetView(node);
                if (new Rect(view.LastRect.position + pan, view.LastRect.size).Contains(drawSpacePosition))
                    return node;
            }

            return null;
        }

        private ConnectionView GetConnectionView(Connection connection)
        {
            if (!_connectionViews.TryGetValue(connection, out var view))
            {
                view = new ConnectionView(connection, this);
                _connectionViews.Add(connection, view);
            }

            return view;
        }

        internal bool IsSelected(Connection connection) => _selection.Contains(connection);

        internal bool IsSelected(Node node) => _selection.Contains(node);

        internal void OnConnectionMouseDown(Connection connection)
        {
            GUIUtility.keyboardControl = 0;
            _selection.Set(connection);
            Repaint();
        }

        private static Rect ConnectionBounds(Connection connection, Vector2 fromPos, Vector2 toPos, Vector2 pan)
        {
            var bounds = FromToRect(fromPos, toPos);
            var waypoints = connection.Waypoints;
            for (var i = 0; i < waypoints.Count; i++)
            {
                var point = waypoints[i] + pan;
                bounds.xMin = Mathf.Min(bounds.xMin, point.x);
                bounds.yMin = Mathf.Min(bounds.yMin, point.y);
                bounds.xMax = Mathf.Max(bounds.xMax, point.x);
                bounds.yMax = Mathf.Max(bounds.yMax, point.y);
            }

            return bounds;
        }

        internal void AddConnectionPoint(Connection connection, int index, Vector2 canvasPoint)
        {
            if (_session.WorkingCopy.AddConnectionPoint(connection, index, canvasPoint))
                _selection.Set(connection);
            Repaint();
        }

        internal void MoveConnectionPoint(Connection connection, int index, Vector2 canvasPoint)
        {
            _session.WorkingCopy.MoveConnectionPoint(connection, index, canvasPoint);
            Repaint();
        }

        internal void ShowConnectionContextMenu(Connection connection, int insertIndex, Vector2 canvasPoint)
        {
            var graph = _session.WorkingCopy;
            var menu = new GenericMenu();
            if (insertIndex >= 0)
                menu.AddItem(new GUIContent("Add Point Here"), false, () => AddConnectionPoint(connection, insertIndex, canvasPoint));
            else
                menu.AddDisabledItem(new GUIContent("Add Point Here"));
            menu.AddItem(new GUIContent("Sharp Bends"), connection.SharpBends, () =>
            {
                graph.SetSharpBends(connection, !connection.SharpBends);
                Repaint();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteConnection(connection));
            _pendingContextMenu = menu;
        }

        internal void ShowConnectionPointContextMenu(Connection connection, int pointIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove Point"), false, () =>
            {
                _session.WorkingCopy.RemoveConnectionPoint(connection, pointIndex);
                Repaint();
            });
            _pendingContextMenu = menu;
        }

        private void DeleteConnection(Connection connection)
        {
            _session.WorkingCopy.Disconnect(connection);
            NotifyOutConnectionRemoved(connection);
            _selection.PruneMissing(_session.WorkingCopy);
            Repaint();
        }

        internal void OnRelinkDrop(Connection connection, ConnectionView.RelinkEnd end, Vector2 drawSpacePosition)
        {
            if (!AllowCanvasClick)
                return;

            var graph = _session.WorkingCopy;
            var sourceEnd = end == ConnectionView.RelinkEnd.Source;
            if (sourceEnd && TryMoveConnectionToSlot(connection, drawSpacePosition, graph))
            {
                Repaint();
                return;
            }

            var target = NodeUnderMouse(drawSpacePosition, graph.Translation);
            if (target == null)
                return;

            var previousSource = connection.Source;
            if (graph.Relink(connection, target, sourceEnd))
            {
                if (sourceEnd && !ReferenceEquals(previousSource, connection.Source))
                {
                    if (previousSource != null)
                        GetView(previousSource).OnOutConnectionRemoved(connection);
                    GetView(connection.Source).OnOutConnectionCreated(connection, -1);
                }

                if (graph.IsTree && connection.Source != null)
                    graph.SortChildrenByPosition(connection.Source);
            }

            Repaint();
        }

        private bool TryMoveConnectionToSlot(Connection connection, Vector2 drawSpacePosition, Graph graph)
        {
            var source = connection.Source;
            if (source == null)
                return false;

            var view = GetView(source);
            if (!view.CanReassignOutConnectionSlot)
                return false;

            var pan = graph.Translation;
            var nodeRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
            if (!nodeRect.Contains(drawSpacePosition) && !PortStripRect(nodeRect).Contains(drawSpacePosition))
                return false;

            var index = IndexOfOutConnection(source, connection);
            if (index < 0)
                return false;

            var slotCount = OutSlotCount(source);
            var slot = NearestOutSlot(source, slotCount, pan, drawSpacePosition);
            if (slot < 0 || slot == view.GetOutConnectionSlot(index))
                return false;

            MutationGuard.Notify(graph, "Move Line To Slot");
            view.SetOutConnectionSlot(connection, slot);
            return true;
        }

        private int NearestOutSlot(Node node, int slotCount, Vector2 pan, Vector2 drawSpacePosition)
        {
            var vertical = _session.WorkingCopy.FlowDirection == FlowDirection.Vertical;
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var s = 0; s < slotCount; s++)
            {
                var center = OutPortPosition(node, s, slotCount, pan);
                var distance = Mathf.Abs(vertical ? center.x - drawSpacePosition.x : center.y - drawSpacePosition.y);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = s;
            }

            return best;
        }

        private static int IndexOfOutConnection(Node node, Connection connection)
        {
            var outs = node.OutConnections;
            for (var i = 0; i < outs.Count; i++)
                if (ReferenceEquals(outs[i], connection))
                    return i;
            return -1;
        }

        private void BeginZoomArea()
        {
            _guiGroupOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            GUI.EndGroup();
            _guiGroupOrigin -= GUIUtility.GUIToScreenPoint(Vector2.zero);
            var zoom = CurrentZoom;
            var clipped = new Rect(_canvasRect.x + _guiGroupOrigin.x, _canvasRect.y + _guiGroupOrigin.y,
                _canvasRect.width / zoom, _canvasRect.height / zoom);
            GUI.BeginGroup(clipped);
            _previousGuiMatrix = GUI.matrix;
            var pivot = new Vector3(clipped.x, clipped.y, 0f);
            var translation = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
            var scale = Matrix4x4.Scale(new Vector3(zoom, zoom, 1f));
            GUI.matrix = translation * scale * translation.inverse * GUI.matrix;
        }

        private void EndZoomArea()
        {
            GUI.matrix = _previousGuiMatrix;
            GUI.EndGroup();
            GUI.BeginGroup(new Rect(_guiGroupOrigin.x, _guiGroupOrigin.y, position.width, position.height));
        }

        private void DrawNodes()
        {
            var graph = _session.WorkingCopy;
            var pan = graph.Translation;
            var zoom = CurrentZoom;
            var visible = new Rect(-pan.x - CullingMargin, -pan.y - CullingMargin,
                _canvasRect.width / zoom + CullingMargin * 2f, _canvasRect.height / zoom + CullingMargin * 2f);

            var drawn = 0;
            BeginWindows();
            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                    continue;

                var view = GetView(node);
                var size = EffectiveNodeSize(node);
                view.SetLastRect(new Rect(node.Position, size));
                if (!_fullDrawPass && !visible.Overlaps(view.LastRect) && !_selection.Contains(node))
                    continue;

                var passedRect = new Rect(node.Position + pan, size);
                GUI.backgroundColor = Color.Lerp(Color.white, NodeTypeInfo.GetColor(node), NodeTintStrength);
                var returnedRect = GUI.Window(i, passedRect, view.WindowFunction, node.Name, view.WindowStyle);
                GUI.backgroundColor = Color.white;
                var newPosition = returnedRect.position - pan;
                if (newPosition != node.Position)
                {
                    graph.MoveNode(node, newPosition);
                    _nodeDragMoved = true;
                }

                view.SetLastRect(new Rect(node.Position, size));
                drawn++;
            }

            EndWindows();
            HandleNodeResizeGesture(Event.current, graph);
            if (drawn != _drawnNodeCount)
            {
                _drawnNodeCount = drawn;
                _totalNodeCount = -1;
            }
        }

        internal void BeginNodeResize(Node node)
        {
            GUIUtility.keyboardControl = 0;
            _resizingNode = node;
        }

        internal bool IsResizingNode(Node node)
        {
            return _resizingNode != null && ReferenceEquals(_resizingNode, node);
        }

        private void HandleNodeResizeGesture(Event e, Graph graph)
        {
            if (_resizingNode == null)
                return;

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                var size = EffectiveNodeSize(_resizingNode) + e.delta;
                graph.ResizeNode(_resizingNode, new Vector2(Mathf.Max(size.x, NodeMinWidth(_resizingNode)), size.y));
                e.Use();
                Repaint();
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                _resizingNode = null;
                e.Use();
                Repaint();
            }
        }

        private void DrawOverlays()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EnsureStyles();
            var graph = _session.WorkingCopy;
            var pan = graph.Translation;
            var zoom = CurrentZoom;
            var visible = new Rect(-pan.x - CullingMargin, -pan.y - CullingMargin,
                _canvasRect.width / zoom + CullingMargin * 2f, _canvasRect.height / zoom + CullingMargin * 2f);

            var colored = graph.Nodes;
            for (var i = 0; i < colored.Count; i++)
            {
                var node = colored[i];
                if (node == null)
                    continue;

                var view = GetView(node);
                if (!visible.Overlaps(view.LastRect))
                    continue;

                var nodeRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
                DrawNodeOutline(view, nodeRect, NodeTypeInfo.GetColor(node), 0);
                if (AllowCanvasClick)
                    EditorGUIUtility.AddCursorRect(NodeView.ResizeHandleRect(nodeRect), MouseCursor.ResizeUpLeft);
            }

            for (var i = 0; i < _selection.Nodes.Count; i++)
            {
                var view = GetView(_selection.Nodes[i]);
                var rect = new Rect(view.LastRect.position + pan, view.LastRect.size);
                rect.xMin -= 2f;
                rect.yMin -= 2f;
                rect.xMax += 2f;
                rect.yMax += 2f;
                DrawNodeOutline(view, rect, _selectionColor, 2);
            }

            var startNode = graph.StartNode;
            if (startNode != null)
            {
                var view = GetView(startNode);
                GUI.Label(new Rect(view.LastRect.x + pan.x, view.LastRect.y + pan.y - 18f, 80f, 16f), "START", _startTagStyle);
            }

            var nodes = graph.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    continue;
                var view = GetView(nodes[i]);
                var content = view.GetCommentContent();
                if (content == null)
                    continue;
                var rect = new Rect(view.LastRect.position + pan, view.LastRect.size);
                var height = _commentStyle.CalcHeight(content, rect.width);
                GUI.Label(new Rect(rect.x, rect.yMax + 2f, rect.width, height), content, _commentStyle);
            }

            if (_isRectSelecting)
            {
                var canvasSelectRect = FromToRect(_rectSelectStart, _rectSelectEnd);
                for (var i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] == null)
                        continue;
                    var view = GetView(nodes[i]);
                    if (!canvasSelectRect.Overlaps(view.LastRect))
                        continue;
                    var highlightRect = new Rect(view.LastRect.position + pan, view.LastRect.size);
                    Handles.DrawSolidRectangleWithOutline(highlightRect, Color.clear, _rectHighlightColor);
                }

                var marqueeRect = FromToRect(_rectSelectStart + pan, _rectSelectEnd + pan);
                Handles.DrawSolidRectangleWithOutline(marqueeRect, _rectSelectFillColor, _selectionColor);
            }
        }

        private void ApplyRectSelection()
        {
            _selection.Clear();
            var rect = FromToRect(_rectSelectStart, _rectSelectEnd);
            var nodes = _session.WorkingCopy.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    continue;
                if (rect.Overlaps(GetView(nodes[i]).LastRect))
                    _selection.Add(nodes[i]);
            }
        }

        private static Rect FromToRect(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static void EnsureStyles()
        {
            if (_commentStyle != null)
                return;

            _commentStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.75f, 0.75f, 0.6f) }
            };
            _startTagStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(0.4f, 0.85f, 0.4f) }
            };
        }

        private void DrawGrid()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var zoom = CurrentZoom;
            var pan = _session.WorkingCopy.Translation;
            var width = _canvasRect.width / zoom;
            var height = _canvasRect.height / zoom;
            var spacing = GridSpacing;
            if (zoom <= GridVeryCoarseZoomThreshold)
                spacing *= GridCoarseFactor * GridCoarseFactor;
            else if (zoom <= GridCoarseZoomThreshold)
                spacing *= GridCoarseFactor;

            var firstColumn = Mathf.FloorToInt(-pan.x / spacing);
            var lastColumn = Mathf.CeilToInt((-pan.x + width) / spacing);
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                Handles.color = column % GridMajorEvery == 0 ? _gridMajorColor : _gridMinorColor;
                var x = column * spacing + pan.x;
                Handles.DrawLine(new Vector3(x, 0f), new Vector3(x, height));
            }

            var firstRow = Mathf.FloorToInt(-pan.y / spacing);
            var lastRow = Mathf.CeilToInt((-pan.y + height) / spacing);
            for (var row = firstRow; row <= lastRow; row++)
            {
                Handles.color = row % GridMajorEvery == 0 ? _gridMajorColor : _gridMinorColor;
                var y = row * spacing + pan.y;
                Handles.DrawLine(new Vector3(0f, y), new Vector3(width, y));
            }

            Handles.color = Color.white;
        }
    }
}
