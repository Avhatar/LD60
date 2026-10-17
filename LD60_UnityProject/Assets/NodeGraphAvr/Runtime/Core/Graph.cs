using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// Base class of all graph kinds: owns the node list, declares per-kind policy,
    /// and exposes the only sanctioned mutation API. Every mutation notifies
    /// MutationGuard before changing state so the editor can record undo. The start
    /// node is the first node in the list by convention. In-memory representation
    /// only — persistence to disk is handled by the editor assembly.
    /// </summary>
    public abstract class Graph : ScriptableObject
    {
        [Tooltip("All nodes of the graph. The first node is the start node by convention.")]
        [SerializeReference] private List<Node> _nodes = new List<Node>();

        [Tooltip("Titled rectangular frames on the canvas. Purely spatial authoring data — no node membership is stored.")]
        [SerializeField] private List<NodeGroup> _groups = new List<NodeGroup>();

        [Tooltip("Canvas pan offset. Authoring view state, persisted so the graph reopens where the author left it.")]
        [SerializeField] private Vector2 _translation;

        [Tooltip("Canvas zoom factor. Authoring view state, persisted with the graph.")]
        [SerializeField] private float _zoomFactor = 1f;

        [Tooltip("Version of the on-disk format this graph was loaded from. Migration hook for future format changes.")]
        [SerializeField] private int _formatVersion = 1;

        private static readonly Comparison<Connection> _compareByTargetX = CompareByTargetX;
        private static readonly Comparison<Connection> _compareByTargetY = CompareByTargetY;

        public abstract Type BaseNodeType { get; }

        public abstract bool IsTree { get; }

        public abstract FlowDirection FlowDirection { get; }

        public IReadOnlyList<Node> Nodes => _nodes;

        internal List<Node> NodeListInternal => _nodes;

        public IReadOnlyList<NodeGroup> Groups => _groups;

        internal List<NodeGroup> GroupListInternal => _groups;

        public Node StartNode
        {
            get
            {
                if (_nodes.Count == 0)
                    return null;
                var first = _nodes[0];
                return first != null && first.AllowAsStart ? first : null;
            }
        }

        public Vector2 Translation
        {
            get { return _translation; }
            set { _translation = value; }
        }

        public float ZoomFactor
        {
            get { return _zoomFactor; }
            set { _zoomFactor = value; }
        }

        public int FormatVersion
        {
            get { return _formatVersion; }
            internal set { _formatVersion = value; }
        }

        public T AddNode<T>(Vector2 position) where T : Node => (T)AddNode(typeof(T), position);

        public Node AddNode(Type nodeType, Vector2 position)
        {
            if (nodeType == null || nodeType.IsAbstract || !BaseNodeType.IsAssignableFrom(nodeType))
            {
                Debug.LogError($"{name}: Refused node of type '{(nodeType != null ? nodeType.Name : "null")}' in {nameof(AddNode)}. " +
                               $"This graph kind accepts non-abstract types derived from {BaseNodeType.Name}. " +
                               "Fix: create nodes through the graph's own node menu.", this);
                return null;
            }

            MutationGuard.Notify(this, "Add Node");
            var node = (Node)Activator.CreateInstance(nodeType);
            node.Graph = this;
            node.Position = position;
            _nodes.Add(node);
            return node;
        }

        public void RemoveNode(Node node)
        {
            if (node == null || node.Graph != this || !_nodes.Contains(node))
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(RemoveNode)}. " +
                               "Only nodes previously added to this graph can be removed. Fix: pass a node obtained from this graph.", this);
                return;
            }

            MutationGuard.Notify(this, "Remove Node");
            for (var i = node.InList.Count - 1; i >= 0; i--)
                RemoveConnectionInternal(node.InList[i]);
            for (var i = node.OutList.Count - 1; i >= 0; i--)
                RemoveConnectionInternal(node.OutList[i]);
            _nodes.Remove(node);
            node.Graph = null;
        }

        public Connection Connect(Node source, Node target, int sourceIndex = -1)
        {
            if (!IsNewConnectionAllowed(source, target))
                return null;

            if (source.Graph != this || target.Graph != this)
            {
                Debug.LogError($"{name}: Refused connection in {nameof(Connect)} — source or target does not belong to this graph. " +
                               "Connections can only link nodes of the same graph. Fix: pass nodes obtained from this graph.", this);
                return null;
            }

            if (!typeof(Connection).IsAssignableFrom(source.OutConnectionType))
            {
                Debug.LogError($"{name}: Refused connection in {nameof(Connect)} — '{source.Name}' declares {nameof(Node.OutConnectionType)} " +
                               $"'{source.OutConnectionType?.Name}', which is not a {nameof(Connection)}. Fix: correct the node type's policy.", this);
                return null;
            }

            MutationGuard.Notify(this, "Connect Nodes");
            var connection = (Connection)Activator.CreateInstance(source.OutConnectionType);
            connection.Source = source;
            connection.Target = target;
            if (sourceIndex < 0 || sourceIndex > source.OutList.Count)
                source.OutList.Add(connection);
            else
                source.OutList.Insert(sourceIndex, connection);
            target.InList.Add(connection);
            return connection;
        }

        public void Disconnect(Connection connection)
        {
            if (connection == null || connection.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(connection)} is null or not part of this graph in {nameof(Disconnect)}. " +
                               "Fix: pass a connection created by this graph's Connect.", this);
                return;
            }

            MutationGuard.Notify(this, "Disconnect Nodes");
            RemoveConnectionInternal(connection);
        }

        public bool Relink(Connection connection, Node newEnd, bool sourceEnd)
        {
            if (connection == null || connection.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(connection)} is null or not part of this graph in {nameof(Relink)}. " +
                               "Fix: pass a connection created by this graph's Connect.", this);
                return false;
            }

            if (newEnd == null || newEnd.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(newEnd)} is null or not part of this graph in {nameof(Relink)}. " +
                               "Connections can only link nodes of the same graph. Fix: pass a node obtained from this graph.", this);
                return false;
            }

            var currentEnd = sourceEnd ? connection.Source : connection.Target;
            if (currentEnd == newEnd)
                return true;

            var newSource = sourceEnd ? newEnd : connection.Source;
            var newTarget = sourceEnd ? connection.Target : newEnd;
            if (!IsNewConnectionAllowed(newSource, newTarget, connection))
                return false;

            MutationGuard.Notify(this, "Relink Connection");
            if (sourceEnd)
            {
                connection.Source.OutList.Remove(connection);
                connection.Source = newEnd;
                newEnd.OutList.Add(connection);
            }
            else
            {
                connection.Target.InList.Remove(connection);
                connection.Target = newEnd;
                newEnd.InList.Add(connection);
            }

            return true;
        }

        public NodeGroup AddGroup(string title, Rect rect)
        {
            MutationGuard.Notify(this, "Add Group");
            var group = new NodeGroup
            {
                Title = title,
                Rect = rect
            };
            _groups.Add(group);
            return group;
        }

        public bool RemoveGroup(NodeGroup group)
        {
            if (!ValidateGroupArg(group, nameof(RemoveGroup)))
                return false;

            MutationGuard.Notify(this, "Remove Group");
            _groups.Remove(group);
            return true;
        }

        public bool MoveGroup(NodeGroup group, Vector2 delta)
        {
            if (!ValidateGroupArg(group, nameof(MoveGroup)))
                return false;

            MutationGuard.Notify(this, "Move Group");
            var rect = group.Rect;
            rect.position += delta;
            group.Rect = rect;
            return true;
        }

        public bool ResizeGroup(NodeGroup group, Rect rect)
        {
            if (!ValidateGroupArg(group, nameof(ResizeGroup)))
                return false;

            MutationGuard.Notify(this, "Resize Group");
            group.Rect = rect;
            return true;
        }

        public bool RenameGroup(NodeGroup group, string title)
        {
            if (!ValidateGroupArg(group, nameof(RenameGroup)))
                return false;

            MutationGuard.Notify(this, "Rename Group");
            group.Title = title;
            return true;
        }

        public bool SetGroupComment(NodeGroup group, string comment)
        {
            if (!ValidateGroupArg(group, nameof(SetGroupComment)))
                return false;

            MutationGuard.Notify(this, "Set Group Comment");
            group.Comment = comment;
            return true;
        }

        public bool SetGroupCommentPlacement(NodeGroup group, GroupCommentPlacement placement)
        {
            if (!ValidateGroupArg(group, nameof(SetGroupCommentPlacement)))
                return false;

            MutationGuard.Notify(this, "Set Group Comment Placement");
            group.CommentPlacement = placement;
            return true;
        }

        public bool SetGroupCommentColor(NodeGroup group, Color color)
        {
            if (!ValidateGroupArg(group, nameof(SetGroupCommentColor)))
                return false;

            MutationGuard.Notify(this, "Set Group Comment Color");
            group.CommentColor = color;
            return true;
        }

        public bool SetGroupColor(NodeGroup group, Color color)
        {
            if (!ValidateGroupArg(group, nameof(SetGroupColor)))
                return false;

            MutationGuard.Notify(this, "Set Group Color");
            group.Color = color;
            return true;
        }

        private bool ValidateGroupArg(NodeGroup group, string operation)
        {
            if (group == null || !_groups.Contains(group))
            {
                Debug.LogError($"{name}: {nameof(group)} is null or not part of this graph in {operation}. " +
                               "Fix: pass a group obtained from this graph's Groups.", this);
                return false;
            }

            return true;
        }

        public bool AddConnectionPoint(Connection connection, int index, Vector2 position)
        {
            if (!ValidateConnectionPointArgs(connection, index, connection?.WaypointList.Count ?? 0, nameof(AddConnectionPoint)))
                return false;

            MutationGuard.Notify(this, "Add Line Point");
            connection.WaypointList.Insert(index, position);
            return true;
        }

        public bool MoveConnectionPoint(Connection connection, int index, Vector2 position)
        {
            if (!ValidateConnectionPointArgs(connection, index, (connection?.WaypointList.Count ?? 0) - 1, nameof(MoveConnectionPoint)))
                return false;

            MutationGuard.Notify(this, "Move Line Point");
            connection.WaypointList[index] = position;
            return true;
        }

        public bool RemoveConnectionPoint(Connection connection, int index)
        {
            if (!ValidateConnectionPointArgs(connection, index, (connection?.WaypointList.Count ?? 0) - 1, nameof(RemoveConnectionPoint)))
                return false;

            MutationGuard.Notify(this, "Remove Line Point");
            connection.WaypointList.RemoveAt(index);
            return true;
        }

        public void SetSharpBends(Connection connection, bool sharpBends)
        {
            if (connection == null || connection.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(connection)} is null or not part of this graph in {nameof(SetSharpBends)}. " +
                               "Fix: pass a connection created by this graph's Connect.", this);
                return;
            }

            if (connection.SharpBends == sharpBends)
                return;

            MutationGuard.Notify(this, "Set Line Style");
            connection.SharpBends = sharpBends;
        }

        private bool ValidateConnectionPointArgs(Connection connection, int index, int maxIndex, string operation)
        {
            if (connection == null || connection.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(connection)} is null or not part of this graph in {operation}. " +
                               "Fix: pass a connection created by this graph's Connect.", this);
                return false;
            }

            if (index < 0 || index > maxIndex)
            {
                Debug.LogError($"{name}: point index {index} is out of range (0..{maxIndex}) in {operation}. " +
                               "Fix: pass an index obtained from the connection's current waypoint list.", this);
                return false;
            }

            return true;
        }

        public void MoveNode(Node node, Vector2 position)
        {
            if (node == null || node.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(MoveNode)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            MutationGuard.Notify(this, "Move Node");
            node.Position = position;
        }

        public void ResizeNode(Node node, Vector2 size)
        {
            if (node == null || node.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(ResizeNode)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            var clamped = new Vector2(Mathf.Max(Node.MinSize.x, size.x), Mathf.Max(Node.MinSize.y, size.y));
            if (clamped == node.Size)
                return;

            MutationGuard.Notify(this, "Resize Node");
            node.Size = clamped;
        }

        public void SetComment(Node node, string comment)
        {
            if (node == null || node.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(SetComment)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            MutationGuard.Notify(this, "Set Comment");
            node.Comment = comment;
        }

        public void SetCustomName(Node node, string customName)
        {
            if (node == null || node.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(SetCustomName)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            MutationGuard.Notify(this, "Rename Node");
            node.CustomName = customName;
        }

        public void SetStartNode(Node node)
        {
            if (node == null || !_nodes.Contains(node))
            {
                Debug.LogError($"{name}: {nameof(node)} is null or not part of this graph in {nameof(SetStartNode)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            if (!node.AllowAsStart)
            {
                Debug.LogWarning($"{name}: Refused start node — '{node.Name}' is not allowed as a start node.", this);
                return;
            }

            if (_nodes[0] == node)
                return;

            MutationGuard.Notify(this, "Set Start Node");
            _nodes.Remove(node);
            _nodes.Insert(0, node);
        }

        public void SortChildrenByPosition(Node parent)
        {
            if (parent == null || parent.Graph != this)
            {
                Debug.LogError($"{name}: {nameof(parent)} is null or not part of this graph in {nameof(SortChildrenByPosition)}. " +
                               "Fix: pass a node obtained from this graph.", this);
                return;
            }

            MutationGuard.Notify(this, "Sort Children");
            var outList = parent.OutList;
            if (outList.Count < 2)
                return;

            if (FlowDirection == FlowDirection.Vertical)
                outList.Sort(_compareByTargetX);
            else
                outList.Sort(_compareByTargetY);
        }

        public static bool IsNewConnectionAllowed(Node source, Node target, Connection existing = null)
        {
            if (source == null || target == null)
            {
                Debug.LogError($"{nameof(Graph)}: Refused connection — source or target is null in {nameof(IsNewConnectionAllowed)}. " +
                               "Both endpoints are required. Fix: pass valid nodes.");
                return false;
            }

            if (source == target)
            {
                Debug.LogWarning($"Refused connection: '{source.Name}' cannot connect to itself.");
                return false;
            }

            if (existing == null || existing.Source != source)
            {
                var maxOut = source.MaxOutConnections;
                if (maxOut != -1 && source.OutConnections.Count >= maxOut)
                {
                    Debug.LogWarning($"Refused connection: '{source.Name}' has no free out slots ({maxOut} max).");
                    return false;
                }
            }

            if (existing == null || existing.Target != target)
            {
                var maxIn = target.MaxInConnections;
                if (maxIn != -1 && target.InConnections.Count >= maxIn)
                {
                    Debug.LogWarning($"Refused connection: '{target.Name}' has no free in slots ({maxIn} max).");
                    return false;
                }
            }

            if (!source.CanConnectTo(target))
            {
                Debug.LogWarning($"Refused connection: '{source.Name}' refuses to connect to '{target.Name}'.");
                return false;
            }

            if (!target.CanConnectFrom(source))
            {
                Debug.LogWarning($"Refused connection: '{target.Name}' refuses connections from '{source.Name}'.");
                return false;
            }

            return true;
        }

        public void RebuildRuntimeState()
        {
            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node == null)
                {
                    Debug.LogError($"{name}: node at index {i} is null in {nameof(RebuildRuntimeState)}. " +
                                   "The graph data is corrupted, likely by a missing node type. Fix: repair or remove the entry in the graph file.", this);
                    continue;
                }

                node.Graph = this;
                node.InList.Clear();
            }

            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node == null)
                    continue;
                var outs = node.OutList;
                for (var j = 0; j < outs.Count; j++)
                {
                    var connection = outs[j];
                    if (connection == null || connection.Target == null)
                    {
                        Debug.LogError($"{name}: '{node.Name}' has a null or target-less out-connection at index {j} in {nameof(RebuildRuntimeState)}. " +
                                       "The graph data is corrupted. Fix: repair or remove the connection entry in the graph file.", this);
                        continue;
                    }

                    connection.Source = node;
                    connection.Target.InList.Add(connection);
                }
            }

            for (var i = 0; i < _nodes.Count; i++)
                if (_nodes[i] != null)
                    _nodes[i].OnRuntimeStateRebuilt();
        }

        private static void RemoveConnectionInternal(Connection connection)
        {
            connection.Source?.OutList.Remove(connection);
            connection.Target?.InList.Remove(connection);
        }

        private static int CompareByTargetX(Connection a, Connection b) => a.Target.Position.x.CompareTo(b.Target.Position.x);

        private static int CompareByTargetY(Connection a, Connection b) => a.Target.Position.y.CompareTo(b.Target.Position.y);
    }
}
