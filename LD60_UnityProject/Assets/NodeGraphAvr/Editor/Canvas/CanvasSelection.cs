using System.Collections.Generic;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// The set of currently selected canvas elements — nodes and connections. Holds
    /// plain references into the working copy, so it must be pruned after undo/redo —
    /// Unity's undo can replace managed instances, leaving stale references behind.
    /// </summary>
    public class CanvasSelection
    {
        private readonly List<Node> _nodes = new List<Node>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly List<NodeGroup> _groups = new List<NodeGroup>();

        public IReadOnlyList<Node> Nodes => _nodes;

        public IReadOnlyList<Connection> Connections => _connections;

        public IReadOnlyList<NodeGroup> Groups => _groups;

        public int Count => _nodes.Count + _connections.Count + _groups.Count;

        public bool Contains(Node node) => _nodes.Contains(node);

        public bool Contains(Connection connection) => _connections.Contains(connection);

        public bool Contains(NodeGroup group) => _groups.Contains(group);

        public void Set(Node node)
        {
            Clear();
            if (node != null)
                _nodes.Add(node);
        }

        public void Set(Connection connection)
        {
            Clear();
            if (connection != null)
                _connections.Add(connection);
        }

        public void Set(NodeGroup group)
        {
            Clear();
            if (group != null)
                _groups.Add(group);
        }

        public void Add(Node node)
        {
            if (node != null && !_nodes.Contains(node))
                _nodes.Add(node);
        }

        public void Toggle(Node node)
        {
            if (node == null)
                return;

            if (!_nodes.Remove(node))
                _nodes.Add(node);
        }

        public void Clear()
        {
            _nodes.Clear();
            _connections.Clear();
            _groups.Clear();
        }

        public void PruneMissing(Graph graph)
        {
            if (graph == null)
            {
                Clear();
                return;
            }

            for (var i = _nodes.Count - 1; i >= 0; i--)
                if (!ContainsNode(graph, _nodes[i]))
                    _nodes.RemoveAt(i);
            for (var i = _connections.Count - 1; i >= 0; i--)
                if (!ContainsConnection(graph, _connections[i]))
                    _connections.RemoveAt(i);
            for (var i = _groups.Count - 1; i >= 0; i--)
                if (!ContainsGroup(graph, _groups[i]))
                    _groups.RemoveAt(i);
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

        private static bool ContainsConnection(Graph graph, Connection connection)
        {
            if (connection == null || connection.Source == null || !ContainsNode(graph, connection.Source))
                return false;

            var outs = connection.Source.OutConnections;
            for (var i = 0; i < outs.Count; i++)
                if (ReferenceEquals(outs[i], connection))
                    return true;
            return false;
        }
    }
}
